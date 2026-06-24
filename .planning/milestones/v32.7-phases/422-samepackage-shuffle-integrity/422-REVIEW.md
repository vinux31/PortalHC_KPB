---
phase: 422-samepackage-shuffle-integrity
reviewed: 2026-06-23T13:30:00Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Helpers/PackageSizeAnalysis.cs
  - Helpers/SessionEditLockRules.cs
  - Helpers/ShuffleToggleRules.cs
  - Data/ApplicationDbContext.cs
  - Migrations/20260623103224_AddPackageNumberUniqueIndex.cs
  - Views/Admin/ManagePackages.cshtml
  - Views/Admin/ManagePackageQuestions.cshtml
findings:
  critical: 0
  warning: 2
  info: 3
  total: 5
status: issues_found
---

# Phase 422: Code Review Report

**Reviewed:** 2026-06-23T13:30:00Z
**Depth:** standard
**Files Reviewed:** 8
**Status:** issues_found

## Summary

Reviewed the Phase 422 (SamePackage & Shuffle Integrity, SHFX-01..07) additions across the
controller, three pure helpers, the DbContext config, the unique-index migration, and the two
Razor views. The implementation is clean, well-commented, and consistent with established
codebase patterns. Security posture on the new surfaces is solid:

- **RBAC + antiforgery** on `ToggleSamePackage`, `CreatePackage`, `DeletePackage`, `UpdateShuffleSettings`
  (`[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`), plus class-level `[Authorize]`
  on `AdminBaseController`.
- **Server-authoritative lock** — `SessionEditLockRules.IsSessionEditLocked` guards all 5 POST
  edit endpoints + Import at the top, before any write; view friendly-disable is correctly framed
  as a UX layer only (not the security boundary).
- **Toggle `anyStarted` guard** evaluated server-side over the Pre+Post pair before mutating.
- **No XSS** in the new warning/toast/lock-banner strings — all are static Razor literals (auto-encoded);
  the confirm-before JS uses single-quoted string literals (no quote-break, no interpolated user data).
- **Migration is injection-safe** (static SQL literal, no user input), idempotent (ROW_NUMBER renumber
  is stable on re-run), and non-destructive on Down (DropIndex only). EF wraps Up() (dedup + CreateIndex)
  in a single migration transaction by default, so the dedup-then-index sequence is atomic.
- **Pure helpers** (`PackageSizeAnalysis`, `SessionEditLockRules`, `ShuffleToggleRules`) are EF-free,
  single-source, and correctly kill the prior view↔controller compute drift.

Two Warning-level latent issues relate to transaction integrity / dangling-FK edge cases in the
sync path. Three Info items are minor (one pre-existing structural oddity now hosting Phase 422 code,
two cosmetic). None are blocking.

## Warnings

### WR-01: `ToggleSamePackage` ON-path performs 3 sequential SaveChanges without an enclosing transaction

**File:** `Controllers/AssessmentAdminController.cs:6017-6035`
**Issue:** The ON-path executes three independent commits:
(1) set `SamePackage=true` + SaveChanges (`:6019`),
(2) remove stale Post `UserPackageAssignment` + SaveChanges (`:6033`),
(3) `SyncToLinkedPostIfSamePackageAsync` → `SyncPackagesToPost` deletes Post packages + clones from Pre + SaveChanges (`:5962`).
If the process fails between (1) and (3), the DB is left in an inconsistent state: `SamePackage=true`
(locked) but Post packages not yet (re)synced from Pre. This controller already uses explicit
transactions for comparable multi-step writes (e.g. Import at `:6617`,
`using var importTx = await _context.Database.BeginTransactionAsync()`), so this path diverges from
the established pattern. The `anyStarted` guard reduces blast radius (no participant mid-exam), and
re-running toggle ON would self-heal, so impact is low — but a partial commit can surface a locked
Post with stale/empty packages until the next sync trigger.
**Fix:** Wrap the ON-path mutation in a transaction so flag-set + UPA-clear + package-sync commit atomically:
```csharp
if (samePackage)
{
    using var tx = await _context.Database.BeginTransactionAsync();
    var stalePostAssignments = await _context.UserPackageAssignments
        .Where(a => a.AssessmentSessionId == post.Id)
        .ToListAsync();
    if (stalePostAssignments.Any())
        _context.UserPackageAssignments.RemoveRange(stalePostAssignments);
    await _context.SaveChangesAsync();
    await SyncToLinkedPostIfSamePackageAsync(post.LinkedSessionId.Value);
    await tx.CommitAsync();
    TempData["Success"] = "...";
}
```
(Note: `post.SamePackage=true` + SaveChanges at `:6017-6019` must run before the helper since the helper
reads `post.SamePackage`; either move it inside the same transaction or keep it before `BeginTransactionAsync`
and accept that only the destructive sync steps are wrapped.)

### WR-02: 6 non-toggle sync sites can hit a Restrict-FK `DbUpdateException` if a Post `UserPackageAssignment` survives

**File:** `Controllers/AssessmentAdminController.cs:6124, 6209, 6936, 7201, 7296, 6710` (the `SyncToLinkedPostIfSamePackageAsync` wire sites) → `:5919` (`SyncPackagesToPost` `RemoveRange(existingPostPkgs)`)
**Issue:** `SyncPackagesToPost` deletes existing Post packages via `RemoveRange`, but
`UserPackageAssignment.AssessmentPackageId` is configured `OnDelete(DeleteBehavior.Restrict)`
(`Data/ApplicationDbContext.cs:543-546`). If any Post `UserPackageAssignment` still references a Post
package being deleted, SaveChanges throws `DbUpdateException`. The `ToggleSamePackage` ON-path
defensively clears stale Post UPA before syncing (`:6028-6032`, "Open Q2"), but the 6 incremental sync
sites (CreatePackage/DeletePackage/CreateQuestion/EditQuestion/DeleteQuestion/Import) do **not** —
they rely on the locked invariant (Post can't be edited and SamePackage can't be enabled once anyone
started) to guarantee no Post UPA exists. That invariant holds in the normal flow, but there is no
defense-in-depth at these sites: a Post UPA created out-of-band (e.g. legacy data, a future reshuffle
path, or manual assignment) would surface an unhandled `DbUpdateException` bubbling to a 500 rather
than a graceful TempData message. These sites also lack the `DbUpdateException` try/catch that
DeletePackage/DeleteQuestion wrap their own SaveChanges in.
**Fix:** Centralize the stale-Post-UPA cleanup inside `SyncPackagesToPost` (so all 7 callers benefit),
clearing assignments that point to the Post packages about to be removed before `RemoveRange`:
```csharp
var postPkgIds = existingPostPkgs.Select(p => p.Id).ToList();
if (postPkgIds.Any())
{
    var staleUpa = await _context.UserPackageAssignments
        .Where(a => postPkgIds.Contains(a.AssessmentPackageId))
        .ToListAsync();
    if (staleUpa.Any()) _context.UserPackageAssignments.RemoveRange(staleUpa);
}
```
This also lets the `ToggleSamePackage` ON-path drop its now-redundant inline cleanup (`:6028-6032`),
removing the divergence between the toggle path and the 6 wire sites.

## Info

### IN-01: Phase 422 lock guard now lives inside `CreateQuestion`, whose routing attributes are misapplied to the preceding `TruncateAlt` helper

**File:** `Controllers/AssessmentAdminController.cs:6770-6777`
**Issue:** The `[HttpPost]` / `[Authorize(Roles="Admin, HC")]` / `[ValidateAntiForgeryToken]` trio at
`:6770-6772` is followed by the private static method `TruncateAlt` (`:6773-6775`), then `CreateQuestion`
(`:6777`). In C#, attributes bind to the immediately following declaration, so they attach to
`TruncateAlt` (where they are inert) and `CreateQuestion` ends up with **no** method-level routing,
role, or antiforgery attributes. Authentication is still enforced via the class-level `[Authorize]` on
`AdminBaseController`, but `CreateQuestion` is missing both `[ValidateAntiForgeryToken]` (no CSRF
protection) and the `Admin, HC` role restriction, and `[HttpPost]` (it could be reached via GET).
This is **pre-existing** (introduced in Phase 353, commit `18d6693d`, 2026-06-08) and out of strict
Phase 422 scope — but it is flagged here because Phase 422 deliberately placed its server-authoritative
SHFX-03 lock guard inside this exact method (`:6815-6821`), and a reviewer relying on that guard should
know the surrounding method is not role-restricted or CSRF-protected at the method level. Recommend
moving the `TruncateAlt` helper out from between the attributes and `CreateQuestion`.
**Fix:** Relocate `TruncateAlt` above the attribute block (or anywhere not between the attributes and
the action), so the `[HttpPost]/[Authorize]/[ValidateAntiForgeryToken]` trio binds to `CreateQuestion`.

### IN-02: `PackageSizeAnalysis.Compute` doc comment says "ignore packages without questions" but uses `Questions != null && Any()`; defensive but XML doc and behavior should note empty-`Questions` vs null-`Questions`

**File:** `Helpers/PackageSizeAnalysis.cs:30`
**Issue:** `packages.Where(p => p.Questions != null && p.Questions.Any())` correctly filters out
packages with no questions, but `Questions` on a fully-Included EF entity is normally a non-null
(possibly empty) collection, so the `!= null` branch is effectively dead for the documented caller
(GET ViewBag passes Included packages). It is harmless defensive code; the only note is that callers
passing non-Included packages (lazy/null nav) would silently treat all as "without questions" and
return `Result(0, null, false)`. No action required for current callers; just ensure future callers
always pre-Include `Questions`.
**Fix:** None required (defensive-by-design). Optionally add a one-line remark to the XML summary that
the helper assumes `Questions` is eager-loaded.

### IN-03: Duplicated `anyStarted`-in-group computation between `ToggleSamePackage` and the GET ViewBag

**File:** `Controllers/AssessmentAdminController.cs:6007-6010` and `:5881-5884`
**Issue:** The Pre+Post pair "any participant started" check (`StartedAt != null || Status=="InProgress"
|| Status=="Completed"` over `{post.Id, LinkedSessionId}`) is written twice — once in the GET for the
view friendly-disable (`ViewBag.AnyStartedInGroup`) and once in the POST guard. The two must stay in
sync for the UX-disable and the server-reject to agree. This mirrors the project's own kill-drift
philosophy already applied to `ShuffleToggleRules` and `PackageSizeAnalysis` this phase.
**Fix:** Extract a small pure/async predicate (e.g. `AnyStartedInPairAsync(context, postId, preId)` or a
pure `IsParticipantStarted(session)` over the loaded pair) and call it from both sites, matching the
`SessionEditLockRules` / `ShuffleToggleRules` pattern.

---

_Reviewed: 2026-06-23T13:30:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
