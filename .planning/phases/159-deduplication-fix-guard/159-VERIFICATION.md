---
phase: 159-deduplication-fix-guard
verified: 2026-03-12T08:00:00Z
status: passed
score: 5/5 must-haves verified
re_verification: false
---

# Phase 159: Deduplication Fix & Guard Verification Report

**Phase Goal:** CoachingProton shows no duplicate deliverable rows for any coachee — reactivate cascade is safe, assign flow is idempotent, existing dirty data is cleaned, and the query itself tolerates any surviving bad data
**Verified:** 2026-03-12T08:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Reactivating a CoachCoacheeMapping only restores ProtonTrackAssignments deactivated with that mapping (timestamp correlation) | VERIFIED | `AdminController.cs` line 3479-3487: filters `DeactivatedAt` within ±5s of `originalEndDate` via `EF.Functions.DateDiffSecond`; falls back to `DeactivatedAt == null` for legacy mappings |
| 2 | Assigning a coachee to a track they were previously assigned to reuses the existing inactive ProtonTrackAssignment | VERIFIED | `AdminController.cs` lines 3167-3176: queries for existing inactive assignment by `CoacheeId + ProtonTrackId`, reactivates it (`IsActive = true, DeactivatedAt = null`) instead of inserting a new row |
| 3 | Reused assignments retain existing ProtonDeliverableProgress rows | VERIFIED | Assign path never calls `Add` for reactivated assignments — only sets `IsActive` and clears `DeactivatedAt`; FK-linked progress rows automatically become visible |
| 4 | No coachee+track combination has more than one active ProtonTrackAssignment after cleanup runs | VERIFIED | `SeedData.DeduplicateProtonTrackAssignments` (line 32-70): groups active assignments by `(CoacheeId, ProtonTrackId)`, keeps latest by `AssignedAt` + `Id` tiebreaker, deactivates remainder; called from `InitializeAsync` line 25 |
| 5 | CoachingProton page shows each deliverable row exactly once per coachee even if duplicate assignments exist | VERIFIED | `CDPController.cs` line 1422-1426: `GroupBy(a => new { a.CoacheeId, a.ProtonTrackId }).Select(g => g.OrderByDescending(a => a.Id).First().Id)` before progress query |

**Score:** 5/5 truths verified

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/ProtonModels.cs` | `DeactivatedAt` nullable DateTime on ProtonTrackAssignment | VERIFIED | Line 81: `public DateTime? DeactivatedAt { get; set; }` |
| `Controllers/AdminController.cs` | Fixed Reactivate and Assign logic; CleanupDuplicateAssignments endpoint | VERIFIED | Reactivate at line 3441, Assign FIX-02 at line 3157, Cleanup at line 5767 |
| `Data/SeedData.cs` | `DeduplicateProtonTrackAssignments` method called from `InitializeAsync` | VERIFIED | Method at line 32, called from `InitializeAsync` at line 25 |
| `Controllers/CDPController.cs` | Defensive GroupBy guard by coachee+track | VERIFIED | Lines 1422-1426 with DEF-01 comment |
| `Migrations/20260312072019_AddProtonTrackAssignmentDeactivatedAt.cs` | EF migration for DeactivatedAt column | VERIFIED | File exists in Migrations directory |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `AdminController.cs CoachCoacheeMappingDeactivate` | `ProtonTrackAssignment.DeactivatedAt` | Sets `DeactivatedAt = deactivationTime` on cascade | WIRED | Line 3406: `a.DeactivatedAt = deactivationTime;` (where `deactivationTime = mapping.EndDate.Value`) |
| `AdminController.cs CoachCoacheeMappingReactivate` | `ProtonTrackAssignment` timestamp correlation | `EF.Functions.DateDiffSecond` ±5s filter on `DeactivatedAt` | WIRED | Lines 3484-3486: `DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) >= -5 && <= 5` |
| `AdminController.cs CoachCoacheeMappingAssign` | `ProtonTrackAssignment` reuse | Check existing inactive by `CoacheeId + ProtonTrackId` before create | WIRED | Lines 3167-3176: `FirstOrDefaultAsync` + `existing.IsActive = true` branch |
| `CDPController.cs CoachingProton` | `ProtonTrackAssignment` grouping | `GroupBy(CoacheeId, ProtonTrackId)` + `OrderByDescending(Id).First().Id` | WIRED | Lines 1422-1426 with `// DEF-01` comment |
| `SeedData.cs DeduplicateProtonTrackAssignments` | `ProtonTrackAssignment` deactivation | `OrderByDescending(AssignedAt).ThenByDescending(Id)` keeps latest, deactivates rest | WIRED | Lines 37-68; called from `InitializeAsync` line 25 |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| FIX-01 | 159-01 | DeactivatedAt timestamp on cascade-deactivate; Reactivate correlates by timestamp | SATISFIED | Deactivate stamps `DeactivatedAt = mapping.EndDate.Value`; Reactivate uses `EF.Functions.DateDiffSecond` ±5s filter with `originalEndDate` from EF OriginalValues |
| FIX-02 | 159-01 | Assign flow reuses existing inactive assignments for same coachee+track | SATISFIED | `CoachCoacheeMappingAssign` queries for existing inactive by `CoacheeId + ProtonTrackId`, reactivates if found, only creates new otherwise |
| CLN-01 | 159-02 | One-time cleanup deactivates duplicate active assignments per coachee+track | SATISFIED | `SeedData.DeduplicateProtonTrackAssignments` deactivates all but latest per group; `AdminController.CleanupDuplicateAssignments` provides manual trigger returning `{ cleaned: N }` |
| DEF-01 | 159-02 | CoachingProton query deduplicates by selecting only progress rows from latest active assignment per coachee+track | SATISFIED | `CDPController.cs` lines 1422-1426: GroupBy + OrderByDescending(Id).First() before progress query |

No orphaned requirements — all four IDs declared in PLAN frontmatter map to Phase 159 in REQUIREMENTS.md and are marked `[x]` complete.

---

## Anti-Patterns Found

No anti-patterns found in phase-modified files.

- No TODO/FIXME/placeholder comments in new code paths
- No stub returns or empty implementations
- No console.log-only handlers
- Build: 0 errors, 69 pre-existing warnings (all in `LdapAuthService.cs`, unrelated to this phase)

---

## Human Verification Required

### 1. Full reactivate cycle end-to-end

**Test:** In Admin, map a coach to a coachee, assign a ProtonTrack. Deactivate the mapping. Reactivate the mapping. Verify on the CoachingProton page that deliverable rows appear exactly once (not doubled).
**Expected:** Exactly one set of deliverable rows visible, same as before deactivation.
**Why human:** Requires live database with a coach-coachee pair; runtime behavior of the ±5s window cannot be verified statically.

### 2. Re-assign same track idempotency

**Test:** Assign a coachee to Track A, then deactivate the mapping, then assign the same coachee to Track A again via a new mapping. Verify the CoachingProton page shows no duplicated rows and the existing progress percentages are preserved.
**Expected:** Progress retained, no duplicate deliverable rows.
**Why human:** Requires pre-existing data state and browser verification of rendered rows.

### 3. Startup cleanup log

**Test:** Restart the application with a known duplicate in the database (or check application startup log). Verify the CLN-01 console line appears and reports the correct count.
**Expected:** `CLN-01: Deactivated N duplicate ProtonTrackAssignment(s).` or `CLN-01: No duplicate active ProtonTrackAssignments found.`
**Why human:** Requires access to application startup output.

---

## Gaps Summary

No gaps. All five observable truths are fully implemented and wired. All four requirement IDs (FIX-01, FIX-02, CLN-01, DEF-01) are satisfied with substantive code — not stubs. The EF migration exists and was applied. Build is clean at 0 errors.

---

_Verified: 2026-03-12T08:00:00Z_
_Verifier: Claude (gsd-verifier)_
