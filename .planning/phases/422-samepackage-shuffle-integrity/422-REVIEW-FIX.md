---
phase: 422-samepackage-shuffle-integrity
fixed_at: 2026-06-23T12:48:48Z
review_path: .planning/phases/422-samepackage-shuffle-integrity/422-REVIEW.md
iteration: 1
findings_in_scope: 4
fixed: 4
skipped: 0
status: all_fixed
---

# Phase 422: Code Review Fix Report

**Fixed at:** 2026-06-23T12:48:48Z
**Source review:** .planning/phases/422-samepackage-shuffle-integrity/422-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 4 (WR-02, WR-01, IN-03, IN-01 — IN-02 explicitly excluded as harmless dead-defensive branch)
- Fixed: 4
- Skipped: 0
- Build after all fixes: `dotnet build` → 0 errors (25 pre-existing warnings)
- Tests: 422 filter (SamePackage/SessionEditLock/PackageNumber/SiblingTypeAware/ShuffleToggleRules/PackageSizeAnalysis) **57/57 passed**; `RetakeThenPassCert` **1/1 passed**

## Fixed Issues

### WR-02: 6 non-toggle sync sites can hit a Restrict-FK `DbUpdateException` if a Post `UserPackageAssignment` survives

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `bea2a75a`
**Applied fix:** Added a centralized stale-Post-UPA cleanup INSIDE `SyncPackagesToPost`, immediately before `RemoveRange(existingPostPkgs)`. It collects the IDs of the Post packages about to be deleted (`existingPostPkgs.Select(p => p.Id)`) and removes any `UserPackageAssignment` whose `AssessmentPackageId` (the `OnDelete(Restrict)` FK) points at them. Because all 7 sync callers route through `SyncPackagesToPost`, the toggle path plus CreatePackage/DeletePackage/CreateQuestion/EditQuestion/DeleteQuestion/Import now all get defense-in-depth against a Restrict-FK `DbUpdateException` (legacy / out-of-band Post UPA). Scoped by `AssessmentPackageId` rather than `AssessmentSessionId` because the Restrict FK that actually throws is on the package, not the session. Did NOT touch the deep-clone or OFF-path keep-clone behavior.

### WR-01: `ToggleSamePackage` ON-path performs sequential SaveChanges without an enclosing transaction

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `788153eb`
**Applied fix:** Wrapped the ON-path multi-step write in an explicit EF transaction mirroring the Import pattern (`await using var tx = await _context.Database.BeginTransactionAsync();` + try/commit/catch-rollback-rethrow). The flag-set (`SamePackage = true` + SaveChanges, which must precede the helper that reads `post.SamePackage`) and the `SyncToLinkedPostIfSamePackageAsync` package-sync now commit atomically — a mid-operation failure rolls back rather than leaving `SamePackage=true` with un-synced Post packages. As the WR-02 follow-up, removed the now-redundant inline Post-UPA clear from the ON-path (the cleanup is centralized in `SyncPackagesToPost`), keeping a single source of truth. The OFF-path was left as a single `SaveChanges` with no transaction (no destructive multi-step), and the keep-clone behavior is unchanged. Method header comment updated to reflect the new structure.

### IN-03: Duplicated `anyStarted`-in-group computation between `ToggleSamePackage` and the GET ViewBag

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `d71411ca`
**Applied fix:** Extracted the Pre+Post pair "any participant started" predicate into one private async helper `AnyStartedInPairAsync(int postId, int? preId)` (returns false when `preId` is null; otherwise checks `StartedAt != null || Status=="InProgress" || Status=="Completed"` over `{postId, preId}`). Both call sites now use it: the GET `ViewBag.AnyStartedInGroup` (UX-disable) and the POST `ToggleSamePackage` guard (server-reject). This guarantees the friendly-disable and the server-authoritative reject can never diverge, matching the project's `SessionEditLockRules`/`ShuffleToggleRules` kill-drift pattern.

### IN-01: Phase 422 lock guard lives inside `CreateQuestion`, whose routing attributes are misapplied to `TruncateAlt`

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `24e4ea7d`
**Applied fix:** Relocated the private static `TruncateAlt` helper to sit ABOVE the attribute trio, so `[HttpPost]` / `[Authorize(Roles="Admin, HC")]` / `[ValidateAntiForgeryToken]` now bind directly to `CreateQuestion`. Verified post-edit: `CreateQuestion` has all three attributes; `TruncateAlt` has none. This closes the CSRF gap (and restores method-level role + verb enforcement) on the question-create endpoint, which had been relying only on class-level `[Authorize]`.

## Skipped Issues

### IN-02: `PackageSizeAnalysis.Compute` null-vs-empty `Questions` doc note

**File:** `Helpers/PackageSizeAnalysis.cs:30`
**Reason:** Excluded from scope by the fix instructions — harmless dead-defensive branch (`Questions != null && Any()`), no behavioral change required. The review itself rated it "None required (defensive-by-design)". No action taken.

---

_Fixed: 2026-06-23T12:48:48Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
