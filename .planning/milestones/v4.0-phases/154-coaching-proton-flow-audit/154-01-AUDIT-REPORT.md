# 154-01 Audit Report: Coach-Coachee Mapping Flow (PROTON-01)

**Date:** 2026-03-11
**Auditor:** Claude (automated code review)
**Scope:** PROTON-01 — HC creates coach-coachee mapping with correct section/unit assignment; coachee sees deliverables immediately.

---

## PROTON-01: Coach-Coachee Mapping Setup

### Authorization

| Action | Attribute | Assessment |
|--------|-----------|------------|
| GET CoachCoacheeMapping | `[Authorize(Roles = "Admin, HC")]` | PASS |
| POST CoachCoacheeMappingAssign | `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` | PASS |
| POST CoachCoacheeMappingEdit | `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` | PASS |
| POST CoachCoacheeMappingDeactivate | `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` | PASS |
| POST CoachCoacheeMappingReactivate | `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` | PASS |
| GET CoachCoacheeMappingActiveAssignmentCount | `[Authorize(Roles = "Admin, HC")]` | PASS |
| POST CoachCoacheeMappingGetSessionCount | `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` | PASS |

**Worker role access:** The class-level `[Authorize]` on AdminController requires authentication. The per-action `[Authorize(Roles = "Admin, HC")]` blocks Worker/Coachee/Coach roles — they will receive a 403 redirect to AccessDenied. PASS.

### CSRF Protection (AJAX + [FromBody])

The Assign and Edit endpoints use `[FromBody]` JSON deserialization alongside `[ValidateAntiForgeryToken]`. The view passes the token as the `RequestVerificationToken` request header (line 512, 573, 599, 638, 659 of the view). ASP.NET Core's anti-forgery middleware reads this header by default. This is the correct pattern. PASS.

### Duplicate Mapping Prevention

- **Assign:** Checks `CoachCoacheeMappings` for existing active mapping for any of the submitted `CoacheeIds` before creating. Returns error listing names of already-mapped coachees. PASS.
- **Edit:** Checks for duplicate only when `CoachId` changes (correct — no need to check same-coach). PASS.
- **Reactivate:** Checks for other active mapping for same coachee before reactivating. PASS.
- **Self-mapping guard:** Assign checks `req.CoacheeIds.Contains(req.CoachId)`. Edit checks `req.CoachId == mapping.CoacheeId`. PASS.

### AssignmentSection / AssignmentUnit Handling

- **Assign:** Both fields are required (null/whitespace check at line 3014). Stored trimmed. ProtonTrack progress is auto-created immediately via `AutoCreateProgressForAssignment`. PASS.
- **Edit:** Fields are stored trimmed. If `AssignmentUnit` changes and no ProtonTrack change was requested, existing progress is deleted and rebuilt for the new unit (Phase 129 logic). PASS.
- **Multi-unit user handling:** `AssignmentSection`/`AssignmentUnit` are stored explicitly on the mapping, decoupled from the user's `Section`/`Unit` profile fields. In `CDPController.CoachingProton()` the Phase 129 defensive filter resolves the unit for each active assignment from the mapping's `AssignmentUnit` (falling back to the user's `Unit` only if the mapping field is empty). This correctly handles multi-unit users. PASS.

### Coachee Visibility After Mapping

- `CoachingProton()` (CDPController) scopes deliverable data via active `ProtonTrackAssignments` → `ProtonDeliverableProgresses`.
- When a mapping is created and `ProtonTrackId` is supplied, `AutoCreateProgressForAssignment` is called immediately. The coachee will see deliverables on next page load without any re-seeding step. PASS.
- If `ProtonTrackId` is NOT supplied at mapping creation time, no `ProtonTrackAssignment` is created, and the coachee will see nothing — but this is expected (no track assigned yet). PASS (by design).

### Deactivation Cascade

`CoachCoacheeMappingDeactivate` sets `IsActive = false` on the mapping AND on all active `ProtonTrackAssignments` for that coachee (line 3283-3287). The coachee will no longer see deliverables because `CoachingProton()` filters on `a.IsActive`. PASS (cascade is correct).

`ProtonDeliverableProgresses` are NOT deleted on deactivation — progress records are preserved. This is the correct design (history is kept).

---

## Findings

### FINDING-01 — BUG: Reactivate does not restore ProtonTrackAssignments

**Severity:** Bug
**File:** `Controllers/AdminController.cs`, `CoachCoacheeMappingReactivate()` (~line 3316)
**Description:**
When a mapping is deactivated, `CoachCoacheeMappingDeactivate` cascades to set all `ProtonTrackAssignments.IsActive = false`. When reactivated via `CoachCoacheeMappingReactivate`, only the mapping itself is set to `IsActive = true`. The `ProtonTrackAssignments` remain inactive. The coachee reappears in the mapping list but sees no deliverables in `CoachingProton()` because their track assignment is still inactive.

The current response sends `showAssignPrompt = true` with a link to `PlanIdp` (not `CoachingProton`) — this is the wrong URL and the prompt doesn't guide the HC to re-assign a ProtonTrack.

**Fix applied:**
1. Added cascade reactivation of `ProtonTrackAssignments` in `CoachCoacheeMappingReactivate`.
2. Fixed `assignUrl` to point to `CoachCoacheeMapping` (where HC can re-assign a ProtonTrack in the edit modal).

### FINDING-02 — Edge Case: Section filter uses coach OR coachee section (may show unexpected results)

**Severity:** Edge case (cosmetic / UX)
**File:** `Controllers/AdminController.cs`, `CoachCoacheeMapping()` (~line 2917)
**Description:**
The section filter matches rows where `Coach.Section == section OR Coachee.Section == section`. If a coach is in Seksi A but coaching someone from Seksi B, and an HC filters by Seksi B, they see the Seksi A coach's mapping group. This may be intentional (cross-section coaching) but could be confusing.

**No fix applied** — this is a UX edge case, not a bug. The behavior is defensible.

### FINDING-03 — Edge Case: No SearchUsers endpoint (plan reference was stale)

**Severity:** Informational
**Description:**
The plan referenced `SearchUsers()` (~line 583). No such action exists in AdminController. The line range corresponds to `ManageAssessment()`. There is no user search API that could leak cross-section data. This reference was stale; no issue.

---

## Fixes Applied

### Fix for FINDING-01
**File:** `Controllers/AdminController.cs`

In `CoachCoacheeMappingReactivate()`:
- Added cascade reactivation of `ProtonTrackAssignments` for the coachee.
- Corrected `assignUrl` from `PlanIdp/CDP` to `CoachCoacheeMapping/Admin`.

---

## Summary

| Requirement | Result | Findings |
|-------------|--------|----------|
| PROTON-01 (Coach-Coachee Mapping) | CONDITIONAL PASS | 1 bug (fixed), 1 edge case (accepted), 1 informational |

**Total findings:** 3
**Bugs fixed:** 1 (FINDING-01: Reactivate cascade)
**Edge cases accepted:** 1 (FINDING-02: section filter OR logic)
**Informational:** 1 (FINDING-03: stale plan reference)

**PROTON-01 verdict:** PASS after fix. HC can create mappings with correct section/unit. Coachee sees deliverables immediately when a ProtonTrack is assigned. Multi-unit users handled via AssignmentUnit field. Authorization gates are solid (Admin/HC only, CSRF protected).
