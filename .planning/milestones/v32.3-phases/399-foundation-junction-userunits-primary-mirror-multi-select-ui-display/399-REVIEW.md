---
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
reviewed: 2026-06-18T00:00:00Z
depth: deep
files_reviewed: 23
files_reviewed_list:
  - Models/UserUnit.cs
  - Data/ApplicationDbContext.cs
  - Migrations/20260618045427_AddUserUnitsTable.cs
  - Migrations/20260618045427_AddUserUnitsTable.Designer.cs
  - Migrations/ApplicationDbContextModelSnapshot.cs
  - Controllers/WorkerController.cs
  - Controllers/AccountController.cs
  - Controllers/HomeController.cs
  - Models/ManageUserViewModel.cs
  - Models/ProfileViewModel.cs
  - Models/SettingsViewModel.cs
  - Models/PSignViewModel.cs
  - Models/DashboardHomeViewModel.cs
  - wwwroot/js/shared-cascade.js
  - Views/Admin/CreateWorker.cshtml
  - Views/Admin/EditWorker.cshtml
  - Views/Admin/WorkerDetail.cshtml
  - Views/Admin/ManageWorkers.cshtml
  - Views/Account/Profile.cshtml
  - Views/Account/Settings.cshtml
  - Views/Shared/_PSign.cshtml
  - Views/Home/Index.cshtml
  - HcPortal.Tests (UserUnit/PrimaryMirror/AuditDiff/ImportMultiUnit/UnitInSection/RemoveUnitGuard/Backfill)
findings:
  critical: 0
  warning: 2
  info: 4
  total: 6
status: issues_found
---

# Phase 399: Code Review Report

**Reviewed:** 2026-06-18
**Depth:** deep (cross-file: controller ↔ VM ↔ view ↔ JS ↔ migration ↔ tests)
**Files Reviewed:** 23
**Status:** issues_found (2 Warning, 4 Info — none blocking)

## Summary

Phase 399 lays a clean foundation for multi-unit-within-Bagian. The core design holds up well under
adversarial review:

- **Mass-assignment / over-posting is properly defended.** Both `CreateWorker` and `EditWorker` POST
  re-fetch the valid unit set server-side via `GetUnitsForSectionAsync(model.Section)` and validate each
  submitted unit against it (`WorkerController.cs:397`, `:548`), never trusting the client checkbox list.
  `PrimaryUnit` is likewise re-validated to be a member of the checked set, both client-side
  (`shared-cascade.js`) and server-side (`ValidateUnitsInSection` + `SyncUserUnitsAsync` line 95 re-derives
  primary from the set, ignoring an out-of-set `primaryUnit` arg).
- **XSS is mitigated everywhere.** All badge rendering uses Razor auto-encoded `@u` / `@string.Join(...)`
  (no `Html.Raw` on user/unit data). The JS checkbox-list HTML-escapes unit names via `esc()` before
  `innerHTML` (`shared-cascade.js:85-89`, applied at `:159/:167-172`). The `@Html.Raw(SectionUnitsJson)`
  in Create/Edit views is safe because `System.Text.Json` default encoder escapes `<`, `>`, `&` to
  `\uXXXX` (HTML-safe in `<script>` context).
- **Authorization intact.** Every new/changed POST carries `[Authorize(Roles = "Admin, HC")]` +
  `[ValidateAntiForgeryToken]` (CreateWorker, EditWorker, ImportWorkers).
- **Backfill SQL injection-free** — static literal `INSERT...SELECT...WHERE NOT EXISTS`, no interpolation;
  idempotent via `NOT EXISTS`; null/empty `Unit` → 0 rows.
- **Filtered-unique index syntax** (`[IsPrimary] = 1`) matches SQL Server and is consistent across
  migration, `Designer`, `ModelSnapshot`, and `ApplicationDbContext`.
- **Primary-recompute correctness** is good: promote-on-removal and clear-all→null are covered by
  `PrimaryMirrorTests` and implemented deterministically in `SyncUserUnitsAsync` and the JS
  `onCheckChange`/`syncPrimaryDefault`.
- **MU-07 asymmetric guard** is correct and well-tested: PTA hard-block resolves the PROTON unit via
  `activeMapping.AssignmentUnit ?? oldPrimary` (Pitfall 4), and the coach-mapping auto-deactivate runs
  inside the EditWorker transaction and is audited.
- **No leftover scalar audit anti-pattern** — the only `user.Unit != model.Unit` text is in an explanatory
  comment (`WorkerController.cs:584`); actual auditing uses the set-diff from `SyncUserUnitsAsync`.

The two Warnings concern transaction atomicity on the **Create/Import** paths (the mirror+junction
desync window the phase explicitly guarded against in Edit, but not in Create/Import) and the fact that
the real-DB enforcement of the replace-set strategy against the filtered-unique index is still deferred
to Phase 404 (test stubs are `Skip`-ped). Both are defensible given the phase scope, but worth recording.

## Warnings

### WR-01: Create/Import paths lack the transaction that Edit uses — mirror+junction can desync on partial failure

**File:** `Controllers/WorkerController.cs:453-455` (CreateWorker), `:1286-1288` (ImportWorkers)
**Issue:**
`EditWorker` deliberately wraps the write-through + mirror in one transaction (`:650 BeginTransactionAsync`
… `:673 CommitAsync`) precisely to keep `UserUnits` rows and the `ApplicationUser.Unit` mirror in sync
(Open Q3 atomicity). The Create and Import paths do **not** do this:

```csharp
// CreateWorker (no tx)
await SyncUserUnitsAsync(_context, user, model.Units ?? new(), model.PrimaryUnit); // queues junction + sets user.Unit
await _context.SaveChangesAsync();        // commits junction rows + (queued) mirror on shared context
await _userManager.UpdateAsync(user);     // separate SaveChanges → persists mirror to Identity store
```

If `_userManager.UpdateAsync(user)` (line 455 / 1288) fails *after* `SaveChangesAsync()` (line 454 / 1287)
already committed the junction rows, the worker ends up with `UserUnits` rows whose primary does not
match the persisted `ApplicationUser.Unit` mirror — the exact invariant-#3 desync the Edit path was
hardened against. In Create the user already exists (from `CreateAsync`), so this is a real partial-state
window, not an all-or-nothing create.

Note: because Identity's store is the same `ApplicationDbContext` (`Program.cs:47`
`AddEntityFrameworkStores<ApplicationDbContext>`), `UpdateAsync`'s save and `_context.SaveChangesAsync`
actually share one connection — so the practical failure probability is low — but the code reads as
inconsistent with the (correct) Edit path and offers no atomicity guarantee on the Import loop in
particular (per-row `SaveChanges`, no per-row tx).

**Fix:** Mirror the Edit pattern — wrap each write-through + `UpdateAsync` in a transaction:

```csharp
using var uuTx = await _context.Database.BeginTransactionAsync();
await SyncUserUnitsAsync(_context, user, model.Units ?? new(), model.PrimaryUnit);
var upd = await _userManager.UpdateAsync(user);   // persists mirror
if (!upd.Succeeded) { /* surface errors, return — tx disposes/rolls back */ }
await _context.SaveChangesAsync();
await uuTx.CommitAsync();
```

For `ImportWorkers`, wrap the per-row create+sync+update so a row either fully succeeds or is reported
as an error without leaving an orphaned junction set.

### WR-02: Replace-set vs filtered-unique index verified only at unit level + one manual scenario — change-primary case not gated by a real-DB test

**File:** `Controllers/WorkerController.cs:78-108` (SyncUserUnitsAsync replace-set);
`HcPortal.Tests/UserUnitsWriteThroughTests.cs:6-7`; `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs:72-91`
**Issue:**
`SyncUserUnitsAsync` relies on EF Core emitting all `RemoveRange` DELETEs before the `Add` INSERTs within
a single `SaveChanges`, so the filtered-unique index `IX_UserUnits_UserId_PrimaryUnique` never sees a
transient two-primary state (comment at `:78-79`). EF Core 8 does reorder same-table commands to satisfy
unique-index conflicts, so the claim is most likely correct — **but it is not gated by an automated
real-DB test**:

- All `SyncUserUnitsAsync` unit tests use InMemory, which (by the file's own admission,
  `UserUnitsWriteThroughTests.cs:6-7`) does **not** enforce the filtered-unique index.
- The SQL-real backfill/index tests are still `[Fact(Skip = ...)]` Wave-0 scaffolds deferred to Phase 404
  (`UserUnitsBackfillIntegrationTests.cs:72-91`).
- The manual `SEED_JOURNAL` gate (2026-06-18, 399-02) verified the index *rejects a 2nd inserted primary*
  and the mirror matches — but it did **not** exercise the change-primary replace (old primary row
  DELETEd + new primary row INSERTed in one `SaveChanges`), which is the scenario that actually depends on
  EF's DELETE-before-INSERT ordering.

**Fix:** Before Phase 404 closes, add (or activate in 404) one SQL-Server integration test that calls
`SyncUserUnitsAsync` to *change* the primary on an existing 2-unit worker and asserts `SaveChanges`
succeeds against the real filtered-unique index (and ends with exactly one primary). This converts the
"EF orders DELETE before INSERT" assumption from a comment into a guarded fact. (Acceptable to defer to
404 per the phase plan, but flag it so it is not lost.)

## Info

### IN-01: `SyncUserUnitsAsync` re-dedups case-sensitively while `ParseUnitCell` and the DB index are case-insensitive

**File:** `Controllers/WorkerController.cs:88` (`(units ?? new()).Distinct()`) vs `:55`
(`ParseUnitCell` uses `StringComparer.OrdinalIgnoreCase`) and the DB index
`IX_UserUnits_UserId_Unit_Unique` (SQL Server default collation = case-insensitive).
**Issue:** If two units differing only by case ever reach `SyncUserUnitsAsync` via the checkbox POST
(e.g. `["UnitA","unita"]`), the case-sensitive `.Distinct()` keeps both, and the case-insensitive unique
index would throw on `SaveChanges`. In practice checkbox values are server-rendered from a single valid
unit list, so a collision is currently impossible — this is a latent inconsistency, not a live bug.
**Fix:** Use `.Distinct(StringComparer.OrdinalIgnoreCase)` in `SyncUserUnitsAsync` to match the parse
helper and the index, so the in-memory dedup can never disagree with the DB constraint.

### IN-02: `currentSection` / `primaryUnit` interpolated as raw JS string literals (defense-in-depth only)

**File:** `Views/Admin/CreateWorker.cshtml:206,208`; `Views/Admin/EditWorker.cshtml:247,249`
**Issue:** `currentSection: "@(Model.Section ?? "")"` and `primaryUnit: "@(Model.PrimaryUnit ?? "")"`
place the value directly inside a double-quoted JS string. Razor HTML-encodes the value (so `"`→`&quot;`,
`<`→`&lt;`), which prevents both string break-out and `</script>` break-out — and these values are
admin-curated section/unit names — so there is **no exploitable XSS**. It is, however, slightly fragile
compared to the sibling `selectedUnits` line, which correctly uses
`@Html.Raw(JsonSerializer.Serialize(...))`.
**Fix (optional):** For consistency and robustness, render scalars the same way:
`primaryUnit: @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.PrimaryUnit ?? ""))`.

### IN-03: `SyncUserUnitsAsync` "Added/Removed" audit diff is unordered (HashSet enumeration)

**File:** `Controllers/WorkerController.cs:91-92`
**Issue:** `newSet.Except(oldSet)` / `oldSet.Except(newSet)` iterate `HashSet<string>` whose enumeration
order is not guaranteed, so the audit string "Unit +'X'; Unit +'Y'" may render in a non-deterministic
order. Purely cosmetic for an audit log; the existing tests assert with `Assert.Contains` so they are not
affected.
**Fix (optional):** `.OrderBy(x => x)` the two `Except` results before adding to `changes` for stable,
diff-friendly audit entries.

### IN-04: Excel export multi-unit ordering recomputed inline instead of reusing the primary-first projection

**File:** `Controllers/WorkerController.cs:341-345` vs the `WorkerUnitsView` projection at `:230-232`
and the identical `OrderByDescending(... == primary).ThenBy(...)` blocks in `AccountController.cs:159-163`,
`:208-212`, `HomeController.cs:65-69`.
**Issue:** The "primary-first, then alphabetical" ordering is duplicated in ~5 places. Not a bug, but a
small DRY opportunity now that several surfaces share the exact same shaping logic.
**Fix (optional):** Extract a single helper, e.g.
`static IEnumerable<string> PrimaryFirst(IEnumerable<UserUnit> rows)`, and reuse it across the export,
the `ManageWorkers` dict, `AccountController`, and `HomeController`.

---

_Reviewed: 2026-06-18_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: deep_
