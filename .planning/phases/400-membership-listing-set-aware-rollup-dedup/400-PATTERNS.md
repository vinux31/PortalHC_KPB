# Phase 400: Membership Listing Set-Aware + Rollup Dedup (MU-06) - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 4 (3 modified source + 1 test)
**Analogs found:** 4 / 4 (all exact or role-match — every mechanic already exists in-repo)

> Brownfield ASP.NET Core 8 MVC + EF Core 8. All 4 files already exist on branch `ITHandoff`. Phase 400 = rewire 3 query predicates + 1 contextual column + ~7 unit tests. **0 migration.** **0 markup change** (view cell is value-driven). All copy Bahasa Indonesia; Unit/Bagian labels via `@OrgLabels.GetLabel(1/0)` (not relevant here — no new strings).

---

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Services/WorkerDataService.cs` (`GetWorkersInSection` :244-355) | service (data layer) | CRUD/read-filter | self (CMP-25 batch-load `:283/:295`) + `Controllers/WorkerController.cs:224-232` (UserUnitsDict) + `TrainingAdminController.cs:802` (correlated-subquery form) | exact |
| `Controllers/WorkerController.cs` (`ManageWorkers` :202-204, `ExportWorkers` :300-301) | controller | request-response/read-filter | self — adjacent scalar predicate replaced with same subquery form as `WorkerDataService`; `TrainingAdminController.cs:802` (subquery form) | exact |
| `Views/CMP/_RecordsTeamBody.cshtml` (cell :27, attr :18) | view (Razor partial) | render-only | self — **markup unchanged**; only `worker.Unit` value differs (computed server-side) | exact (no edit) |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` (+~7 tests) | test (unit, EF InMemory) | request-response (assert) | self — `MakeService`/`User` helpers `:19-45`, existing facts `:49-93`; `OrphanCleanupTests.cs:34` (InMemory correlated-subquery) | exact |

**Key cross-cutting fact:** No production correlated-subquery against `_context.UserUnits` exists yet (verified — `grep _context.UserUnits.Any` finds only planning docs). The **form** analog is `TrainingAdminController.cs:802` (`.Where(h => _context.X.Any(s => s.Id == h.SessionId))`). The **batch-load + primary-first ordering** analogs are `WorkerController.cs:224-232`, `AccountController.cs:155-163`, `HomeController.cs:61-69`. Planner must combine: subquery *form* from TrainingAdmin + UserUnits *content* + ordering from Phase 399.

---

## Pattern Assignments

### `Services/WorkerDataService.cs` — `GetWorkersInSection` (service, read-filter)

This file owns BOTH changes: (A) the set-aware predicate, and (B) the contextual `WorkerTrainingStatus.Unit` column (D-02/D-04/D-05). It is the structural template for itself — the new code slots into the existing CMP-25 batch-load + foreach-hydrate shape.

**Analogs:** self (`:283`, `:347`), `WorkerController.cs:224-232` (dict shape), `TrainingAdminController.cs:802` (subquery form), `AccountController.cs:155-163` (primary-first ordering).

---

**(A) Predicate to REPLACE — current scalar (lines 254-255):**
```csharp
if (!string.IsNullOrEmpty(unitFilter))
    usersQuery = usersQuery.Where(u => u.Unit == unitFilter);
```

**Set-aware replacement (correlated subquery — Pitfall #1: nav prop `ApplicationUser.UserUnits` does NOT exist, must use `_context.UserUnits`):**
```csharp
if (!string.IsNullOrEmpty(unitFilter))
    usersQuery = usersQuery.Where(u =>
        _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive));
```

**Form analog this copies** — `Controllers/TrainingAdminController.cs:802` (correlated subquery on outer entity, inside `.Where()`, translates to SQL `EXISTS`):
```csharp
int orphanCount = await _context.AssessmentAttemptHistory
    .Where(h => !_context.AssessmentSessions.Any(s => s.Id == h.SessionId))
    .CountAsync();
```
This is the only in-repo `_context.X.Any(correlated-on-outer)` predicate-in-Where; it proves the shape compiles + translates (it has a passing InMemory test at `OrphanCleanupTests.cs:34`). Phase 400 drops the `!` and adds the multi-clause body `uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive`.

**Anti-pattern (do NOT):** `u.UserUnits.Any(...)` — CS1061, nav property absent (`ApplicationDbContext` maps `.WithMany()` no-arg). Do NOT add `.Distinct()` (no fan-out exists — subquery is boolean, 1 row/user by construction = D-01 dedup gratis).

---

**(B) Batch-load dict to ADD** — insert immediately AFTER `var userIds = users.Select(u => u.Id).ToList();` (line 271).

**Source analog** — `Controllers/WorkerController.cs:224-232` (the `ViewBag.UserUnitsDict` batch-load added in Phase 399), reuse verbatim PLUS add `&& uu.IsActive` filter (D-03) and project to `List<string>`:
```csharp
// WorkerController.cs:224-232 (existing — the dict shape to copy):
var userUnitsDict = (await _context.UserUnits
        .Where(uu => listUserIds.Contains(uu.UserId))
        .ToListAsync())
    .GroupBy(uu => uu.UserId)
    .ToDictionary(
        g => g.Key,
        g => new WorkerUnitsView(
            g.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Unit).Select(x => x.Unit).ToList(),
            g.FirstOrDefault(x => x.IsPrimary)?.Unit));
```

**Adapted for GetWorkersInSection** (project straight to `List<string>`, add `IsActive` per D-03; matches the in-file CMP-25 `trainingsByUser`/`sessionsByUser` shape at `:283`/`:295` — load-then-GroupBy-ToDictionary, no N+1):
```csharp
var unitsByUser = (await _context.UserUnits
        .Where(uu => userIds.Contains(uu.UserId) && uu.IsActive)        // D-03 active-only
        .ToListAsync())
    .GroupBy(uu => uu.UserId)
    .ToDictionary(
        g => g.Key,
        g => g.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Unit)  // D-04 primary-first
              .Select(x => x.Unit).ToList());
```

**In-file N+1 idiom this mirrors** — `WorkerDataService.cs:283` (CMP-25, same load-then-group shape):
```csharp
var trainingsByUser = (await trainingsQuery.ToListAsync())
    .GroupBy(tr => tr.UserId)
    .ToDictionary(g => g.Key, g => g.ToList());
```

---

**(C) Contextual column to CHANGE** — current assignment (line 347, inside the `foreach (var user in users)` hydrate loop at `:340-355`):
```csharp
Unit = user.Unit ?? "",
```

**Contextual replacement (D-02 filtered → unitFilter; unfiltered → all-active primary-first join; D-05 fallback). `WorkerTrainingStatus.Unit` is `string?` [VERIFIED Models/WorkerTrainingStatus.cs:14] so in-place set is type-safe — no model change, view byte-stable:**
```csharp
Unit = !string.IsNullOrEmpty(unitFilter)
    ? unitFilter                                                       // D-02 filtered → matched unit
    : (unitsByUser.TryGetValue(user.Id, out var uList) && uList.Count > 0
        ? string.Join(", ", uList)                                     // D-02 unfiltered → all-active, primary-first
        : (user.Unit ?? "")),                                          // D-05 fallback (legacy/0-unit)
```
The `string.Join(", ", uList)` with `uList` already primary-first-ordered (from the dict) is the same join contract as Phase 399 `_PSign`/Excel export. The view's existing `@(worker.Unit ?? "---")` (`_RecordsTeamBody.cshtml:27`) is the final null guard (D-05 → `---`).

**Ordering MUST match Phase 399** — `AccountController.cs:159-162` / `WorkerController.cs:231`: `OrderByDescending(IsPrimary).ThenBy(Unit)`. (Note: AccountController/HomeController/WorkerController-export use `OrderByDescending(x => x.Unit == primaryUnit)` because they don't carry `IsPrimary` into the projection; the dict here keeps `IsPrimary`, so use `OrderByDescending(x => x.IsPrimary)` directly — semantically identical, cleaner.)

---

### `Controllers/WorkerController.cs` — `ManageWorkers` + `ExportWorkers` (controller, read-filter)

**Analog:** self (the same subquery form as `WorkerDataService` change A; `TrainingAdminController.cs:802` for form). **D-06: predicate-only — do NOT touch display.** `ManageWorkers` already shows all units via `ViewBag.UserUnitsDict` badge (Phase 399); leave it. No batch-load dict change here, no view edit.

**Predicate #2 to REPLACE — `ManageWorkers` (lines 202-204):**
```csharp
// Filter by unit
if (!string.IsNullOrEmpty(unitFilter))
{
    query = query.Where(u => u.Unit == unitFilter);
}
```
→ replace body with the identical set-aware subquery:
```csharp
if (!string.IsNullOrEmpty(unitFilter))
{
    query = query.Where(u =>
        _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive));
}
```

**Predicate #3 to REPLACE — `ExportWorkers` (lines 300-301):**
```csharp
if (!string.IsNullOrEmpty(unitFilter))
    query = query.Where(u => u.Unit == unitFilter);
```
→ same set-aware subquery body.

**Preserve as-is (do NOT change):** the `unitFilter`-vs-section server validation `:171-176`/`:274-279` (security V5, RESEARCH §Security); the existing `ViewBag.UserUnitsDict` batch-load `:224-232`; the export comma-join `:336-351`. These already handle display per Phase 399 (D-06).

---

### `Views/CMP/_RecordsTeamBody.cshtml` — Unit cell (view, render-only)

**Analog:** self. **NO EDIT (verify, do not change).** UI-SPEC + D-02 + RESEARCH all converge: the cell is value-driven; the new value comes from `WorkerDataService` change C. Markup stays byte-stable.

**Cell today (line 27) — keep exactly:**
```html
<td class="p-3">@(worker.Unit ?? "---")</td>
```

**`data-unit` attribute (line 18) — keep exactly (Claude's Discretion; VERIFIED no client-side reader in `RecordsTeam.cshtml` — filter is server-side; tracks contextual value for free):**
```html
data-unit="@worker.Unit"
```

**Planner note:** add a verification step (not an edit) confirming `RecordsTeam.cshtml` does not read `data-unit` for client-side filtering and counts `.worker-row` (1/worker) — already VERIFIED in RESEARCH (`RecordsTeam.cshtml:403/:408`). The empty-state full-row "Data belum ada" (`:3-11`) is unchanged.

---

### `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — ~7 new MU-06 tests (test, unit)

**Analog:** self — reuse `MakeService(out var ctx)` (`:19-28`, EF InMemory Guid-per-test) and the `User(id, name, section)` helper (`:30-35`). Existing facts (`:49-93`) are the structural template: arrange via `ctx.Users.AddRange(...)` + `await ctx.SaveChangesAsync()`, act via `await svc.GetWorkersInSection(...)`, assert with `Assert.Single`/`Assert.Equal`. Seed UserUnits via `ctx.UserUnits.AddRange(new UserUnit { ... })`.

**CRITICAL helper caveat:** `User()` hardcodes `Unit = "U1"` [VERIFIED `:33`]. New MU-06 tests MUST override `u.Unit` (e.g. `var u = User("u1","Budi","A"); u.Unit = "UnitX";`) so the primary mirror matches the seeded primary `UserUnit`, else the D-05 fallback assertion drifts. `UserUnit` fields: `UserId`, `Unit` (string), `IsPrimary` (bool), `IsActive` (bool, default true) [VERIFIED Models/UserUnit.cs].

**Structural template (existing fact to copy — `:49-58`):**
```csharp
[Fact]
public async Task Scope_Nama_FiltersByName()
{
    var svc = MakeService(out var ctx);
    ctx.Users.AddRange(User("u1", "Budi Santoso", "A"), User("u2", "Andi Wijaya", "A"));
    await ctx.SaveChangesAsync();
    var result = await svc.GetWorkersInSection("A", search: "budi", searchScope: "Nama");
    Assert.Single(result);
    Assert.Equal("u1", result[0].WorkerId);
}
```

**InMemory correlated-subquery precedent (proves `.Any(...)`-in-Where runs green on InMemory)** — `OrphanCleanupTests.cs:34`:
```csharp
.Where(h => !ctx.AssessmentSessions.Any(s => s.Id == h.SessionId))
```

**~7 tests to add (per RESEARCH Test Map / Nyquist; full bodies in 400-RESEARCH.md §Code Examples):**
1. `MultiUnitWorker_AppearsInBothUnitFilters_SetAware` — {X,Y} → `Single` for filter X AND filter Y (SC#1).
2. (folded into #1) `GetWorkersInSection("A").Single()` → 1 row no-filter = dedup (SC#2).
3. `InactiveUnit_ExcludedFromFilter_D03` — Y `IsActive=false` → `Empty` for filter Y.
4. `UnfilteredColumn_AllActiveUnits_PrimaryFirst_D02` — primary=Y → `r[0].Unit == "UnitY, UnitX"`.
5. `FilteredColumn_ShowsUnitFilter_D02` — filter Y → `r[0].Unit == "UnitY"`.
6. `ZeroUnit_Fallback_D05` — no active UserUnits, `u.Unit="Legacy"` → `r[0].Unit == "Legacy"`.
7. Regression: existing `Scope_Null_NoFilter_BackwardCompat` (`:86`) stays green = no-drift D1=b (SC#3).

**Do NOT touch `FakeWorkerDataService.cs`** (returns empty; only used by GradingService tests — no controller-level test added).

---

## Shared Patterns

### Set-aware membership predicate (correlated subquery)
**Form source:** `Controllers/TrainingAdminController.cs:802` — `.Where(x => _context.Y.Any(z => z.Fk == x.Id))`
**Apply to:** all 3 predicates — `WorkerDataService.cs:254-255`, `WorkerController.cs:202-204`, `WorkerController.cs:300-301`. Identical body in all three:
```csharp
_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)
```
Consumer #4 `AssessmentAdminController.cs:278` inherits this automatically (passes `unit` to `GetWorkersInSection`) — **no code change, add to verification scope** (RESEARCH Pitfall #2). It is benign + semantically consistent with MU-06.

### Batch-load dict, no N+1
**Source:** `WorkerController.cs:224-232` + in-file `WorkerDataService.cs:283/:295` (CMP-25)
**Apply to:** `GetWorkersInSection` only (D-06 — NOT ManageWorkers). One `_context.UserUnits.Where(userIds.Contains && IsActive).ToList().GroupBy.ToDictionary`.

### Primary-first comma-join (display, plain text)
**Source:** `AccountController.cs:159-162`, `HomeController.cs:65-69`, `WorkerController.cs:231` & export `:342-345`
**Apply to:** the unfiltered branch of the contextual `Unit` column. `OrderByDescending(IsPrimary).ThenBy(Unit)` → `string.Join(", ", ...)`. **No badges/chips** (D-02/D-08 — this cell follows `_PSign`/Excel plain-text convention, not the ManageWorkers/Profile badge convention).

### Dedup by-construction (NOT post-hoc)
**Source:** D-01 — `.Any()` is a boolean subquery → `_context.Users` stays 1 row/user, no JOIN fan-out.
**Apply to:** all paths. **Do NOT add `.Distinct(WorkerId)`** (anti-pattern — masks a fan-out that doesn't exist). Pagination `workerList.Count` (`CMPController.cs:824`, `AssessmentAdminController.cs:280`) stays accurate for free.

### No-drift D1=b (verification, not code)
**Source:** analytics path `CMPController.cs:2581` uses `s.User!.Unit` scalar directly (NOT GetWorkersInSection); Team View `:543` calls GetWorkersInSection with no `unitFilter` → predicate skipped.
**Apply to:** verification — confirm diff touches none of the analytics/renewal/Team-View paths. By-construction 0 drift (SC#3).

---

## No Analog Found

None — every mechanic exists in-repo. The single novelty is *combining* an existing correlated-subquery *form* (`TrainingAdminController.cs:802`) with the `_context.UserUnits` *content* (new only in that no prior code queries UserUnits via subquery; all prior UserUnits access is materialized `.Where(uu => uu.UserId == X).ToList()`). LOW risk — flagged in RESEARCH Assumptions A1 (SQL `EXISTS` translation) with a 2-step materialize fallback if translation fails; verify via `dotnet run` + DB log (Phase 400) and SQL-real (Phase 404).

---

## Metadata

**Analog search scope:** `Services/`, `Controllers/`, `Views/CMP/`, `HcPortal.Tests/`, `Models/`, `Data/`
**Files scanned (read):** `WorkerDataService.cs`, `WorkerController.cs`, `AccountController.cs`, `HomeController.cs`, `TrainingAdminController.cs`, `AssessmentAdminController.cs`, `WorkerDataServiceSearchTests.cs`, `_RecordsTeamBody.cshtml`, `UserUnit.cs`, `WorkerTrainingStatus.cs`
**Greps:** `_context.UserUnits.Any` (0 prod hits — confirms novelty), `.Where(x => _context.Y.Any(...))` (TrainingAdmin:802 form analog)
**Pattern extraction date:** 2026-06-18
