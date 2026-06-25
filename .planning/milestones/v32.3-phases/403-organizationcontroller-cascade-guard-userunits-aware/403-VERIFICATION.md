---
phase: 403-organizationcontroller-cascade-guard-userunits-aware
verified: 2026-06-19T14:05:00Z
status: passed
score: 7/7 must-haves verified
overrides_applied: 0
re_verification:
  is_re_verification: false
---

# Phase 403: OrganizationController Cascade/Guard UserUnits-Aware Verification Report

**Phase Goal:** Make OrganizationController UserUnits-aware (junction from Phase 399) — cascade rename to membership (incl. secondary), delete-guard prevents deleting a unit still in use, reparent cross-Bagian hard-blocked so a worker's UserUnits never split into >1 Bagian (Invariant #1).
**Verified:** 2026-06-19T14:05:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
| -- | ----- | ------ | -------- |
| 1 (SC#1) | Rename unit Level>=1 cascades to ALL `UserUnits.Unit==oldName` rows (incl. secondary & IsActive=false) + primary-mirror `ApplicationUser.Unit` stays consistent | VERIFIED | `OrganizationController.cs:230-232` renames all rows (no IsActive filter); `IsPrimary` untouched so mirror at `:224` (Users.Unit) stays consistent. Test `EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows` asserts all 3 rows renamed incl. inactive, non-match "Lain" untouched, primary flag preserved, `Users.Unit` mirror == new name. 14/14 OrgController tests pass. |
| 2 (SC#2 delete) | DeleteOrganizationUnit rejects unit still a pure secondary active membership, message mentions "sekunder" | VERIFIED | `:501-511` scans `_context.UserUnits.AnyAsync(uu => uu.Unit == unit.Name && uu.IsActive)`; specific "sekunder" message when only membership matches. Test `DeleteOrganizationUnit_SecondaryMembershipActive_Rejected` (pure secondary, no scalar) asserts success=false + "sekunder" + unit still present. |
| 3 (SC#2 deactivate) | ToggleOrganizationUnitActive deactivate-branch rejects unit still a secondary active membership | VERIFIED | `:441-451` same `&& uu.IsActive` scan + "sekunder" message. Test `ToggleOrganizationUnitActive_SecondaryMembershipActive_Rejected` asserts success=false + "sekunder" + unit still active. |
| 4 (SC#3 block) | Reparent cross-Bagian hard-BLOCK when a worker's UserUnits would split into >1 Bagian; message names NIP/worker | VERIFIED | `:261-286` split-detect via `GetSectionUnitsDictAsync()`, member set from active rows, other-rows filtered `IsActive && Unit != oldName`, flagged when resolved Section != newSectionName; message includes NIP - FullName. Test `EditOrganizationUnit_ReparentSplitsWorker_Blocked` asserts success=false + msg contains "12345"/"Budi". |
| 5 (SC#3 allow) | Single-unit reparent (no split) still ALLOWED + Section updated (existing behavior preserved) | VERIFIED | Section cascade preserved at `:288-303`. Test `EditOrganizationUnit_ReparentSingleUnitWorker_Allowed` asserts success=true + `Users.Section=="HSC"`. (Green since RED = regression guard, documented in 403-01-SUMMARY.) |
| 6 (SC#4) | PreviewEditCascade emits `affectedUserUnitsCount` == actual rows renamed by EditOrganizationUnit (preview==actual); modal row appears | VERIFIED | `:367-369` `CountAsync(uu => uu.Unit == oldName)` Level>=1, NO IsActive filter = byte-identical predicate to rename `:230` (PARITY RULE). View `:226` `<li><strong id="cascadeUserUnits">0</strong> baris keanggotaan unit`; `orgTree.js:365` populates `.textContent = pv.affectedUserUnitsCount||0`; `:429` adds term to total so modal appears on UserUnits-only impact. Test `PreviewEditCascade_RenameLevel1_UserUnitsCountMatchesActual` proves preview==actual==3 with discriminating IsActive=false row. UAT pure-edge scenario (Wet Gas) proves modal appears with only UU count. |
| 7 (SC#5) | EditOrganizationUnit cascade wrapped in BeginTransactionAsync (atomic); authz/CSRF preserved; 0 migration | VERIFIED | `:182` `using var tx = await _context.Database.BeginTransactionAsync()`, `:308` `await tx.CommitAsync()`; split-block return-early at `:283-284` disposes tx without commit = rollback. Authz: 9× `[Authorize(Roles = "Admin, HC")]`, 7× `[ValidateAntiForgeryToken]` (all 4 critical actions intact). `dotnet build` 0 error. Migration=false (no schema change; verified by inspection — only UserUnits junction queries, no new entity). |

**Score:** 7/7 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Controllers/OrganizationController.cs` | rename UserUnits cascade + split-detect block + 2 guard scan + affectedUserUnitsCount + tx wrap | VERIFIED | All 6 sub-changes present + wired; `_context.UserUnits` correlated (no nav-prop, 0 occurrences of `u.UserUnits`). |
| `HcPortal.Tests/OrganizationControllerTests.cs` | ~6 ORG-01/02 tests + suppress TransactionIgnoredWarning | VERIFIED | 6 new tests present (`TransactionIgnoredWarning` at `:27`, `GetMessage` helper at `:55`); 14/14 pass. |
| `Views/Admin/ManageOrganization.cshtml` | li 5th row cascadeUserUnits in #cascadeConfirmModal | VERIFIED | `:226` literal "baris keanggotaan unit", `<strong>`, no `@OrgLabels.GetLabel`, identical style to siblings. |
| `wwwroot/js/orgTree.js` | populate textContent + add affectedUserUnitsCount to total | VERIFIED | `:365` populate `.textContent` (XSS-safe), `:429` `+ (pv.affectedUserUnitsCount || 0)` in total. |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| EditOrganizationUnit (rename L>=1) | `_context.UserUnits.Where(uu => uu.Unit == oldName)` | rename loop, no IsActive | WIRED | `:230` (grep count 1) |
| PreviewEditCascade | `affectedUserUnitsCount` JSON field | `CountAsync(uu => uu.Unit == oldName)` L>=1 | WIRED | `:369` + JSON `:379` (grep count 1) |
| Delete / Toggle | `_context.UserUnits.AnyAsync(uu => uu.Unit == name && uu.IsActive)` | OR-clause to existing guard | WIRED | `:441`, `:501` (grep count 2) |
| split-detect | `GetSectionUnitsDictAsync()` | unit->Section map | WIRED | `:266` calls method confirmed at `ApplicationDbContext.cs:121` |
| orgTree.js showCascadeConfirm | `#cascadeUserUnits` textContent | `pv.affectedUserUnitsCount \|\| 0` | WIRED | `orgTree.js:365` |
| orgTree.js submitUnitModal total | modal appears on UserUnits-only | `+ (pv.affectedUserUnitsCount \|\| 0)` | WIRED | `orgTree.js:429` |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| cascadeUserUnits modal row | `pv.affectedUserUnitsCount` | server PreviewEditCascade JSON (`CountAsync` against real `UserUnits` DbSet) | Yes — server-authoritative DB count | FLOWING |
| EditOrganizationUnit cascade | `affectedUnitRows` | `_context.UserUnits.Where(...)` real DbSet query | Yes — mutates real junction rows | FLOWING |

UAT (Playwright @5270, DB snapshot→seed→restore) confirmed live: rename "Alkylation Unit" showed "6 baris keanggotaan unit" == 6 actual UU rows; edge "RFCC NHT" 0 user/1 UU renamed 1 row + mirror untouched; pure-edge "Wet Gas" modal appeared with only UU count = 1.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Build compiles | `dotnet build` | 0 Error, 28 pre-existing warnings | PASS |
| OrgController test suite | `dotnet test --filter ~OrganizationController` | Failed: 0, Passed: 14, Skipped: 0 | PASS |
| Rename cascade predicate parity | grep rename vs preview predicate | Both `uu.Unit == oldName` no-IsActive (identical) | PASS |
| Guard IsActive asymmetry | grep `&& uu.IsActive` | 2 guard occurrences + split-detect | PASS |
| Authz preserved | grep Authorize/AntiForgery | 9 Authorize, 7 AntiForgery (>= 4 critical) | PASS |
| split-detect dependency exists | grep GetSectionUnitsDictAsync in DbContext | method present `:121` | PASS |
| 403 commits exist | `git log --grep=403` | RED `02d32d6f`, GREEN `0994c948`, UI `986fedff` | PASS |
| UAT screenshots present | ls 403-uat-screenshots | 4 png (A/B/D/pure-edge) | PASS |

Full suite count (532/0/5) per 403-01/02-SUMMARY not re-run here (cost; 5 skipped are SQLEXPRESS-gated, owned by Phase 404). OrgController-specific 14/14 re-run live = PASS.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| ORG-01 | 403-01 | Rename/reparent cascade to UserUnits.Unit (+ recompute primary-mirror); delete-guard scans UserUnits incl. secondary | SATISFIED | Truths 1, 2, 3 verified; mirror via untouched IsPrimary |
| ORG-02 | 403-01, 403-02 | Reparent cross-Bagian hard-BLOCK on split (Invariant #1); PreviewEditCascade counts UserUnits (preview==actual) | SATISFIED | Truths 4, 5, 6 verified |

Cross-reference: REQUIREMENTS.md maps exactly ORG-01 + ORG-02 to Phase 403 (both marked Complete). PLAN frontmatter declares 403-01:[ORG-01, ORG-02], 403-02:[ORG-02]. No orphaned requirements — all IDs accounted for.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| Controllers/OrganizationController.cs | 306-308 | EditOrganizationUnit cascade not audited (asymmetric with Delete `:545`) | ℹ️ Info (Warning WR-01 in 403-REVIEW) | Traceability gap, not correctness. Pre-existing; phase widens cascade scope. Non-blocking — capture as backlog. |
| Controllers/OrganizationController.cs | 271-272 | split-detect silently skips memberships absent from active Section dict (fail-open) | ℹ️ Info (IN-01) | No effect in current data model (UU names always active units). Defensive comment suggested. |
| Controllers/OrganizationController.cs | 358 | combined rename+reparent preview-parity untested path | ℹ️ Info (IN-02) | De-dup is deliberate; UI changes one field at a time. Low risk. |
| wwwroot/js/orgTree.js | 426-433 | PreviewEditCascade `error` payload not surfaced (falls through to Edit toast) | ℹ️ Info (IN-03) | Error still surfaces via Edit. Minor UX. |

No blocker anti-patterns. No TODO/FIXME/placeholder. No stub/empty-handler. All counts flow from real server-side DB queries.

### Human Verification Required

None outstanding. The plan's checkpoint UAT (403-02 Task 2, gate=blocking) was executed by the executor via Playwright @localhost:5270 with DB snapshot→seed→restore: 5/5 scenarios PASS (rename modal count==actual, UserUnits-only modal appears, pure-edge isolation, delete reject "sekunder", reparent split block names NIP), documented in 403-02-SUMMARY.md with screenshot evidence (403-uat-screenshots/, 4 files confirmed on disk) and seed journal restored (cleaned). Preview==actual was DB-verified during UAT.

### Gaps Summary

No gaps. All 5 ROADMAP success criteria (decomposed into 7 observable truths) are VERIFIED at all levels (exists, substantive, wired, data flowing). Both requirement IDs (ORG-01, ORG-02) SATISFIED. Build 0 error, 14/14 OrgController tests pass, authz/CSRF preserved, 0 migration. The 1 Warning + 3 Info from code review are non-blocking traceability/UX items suitable for backlog. Browser UAT was completed and documented with evidence. Phase goal achieved.

---

_Verified: 2026-06-19T14:05:00Z_
_Verifier: Claude (gsd-verifier)_
