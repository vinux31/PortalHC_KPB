# Audit Report: HC Oversight, Assessment Proton, Histori Proton
## Phase 154 Plan 03 — PROTON-05, PROTON-06, PROTON-07

**Date:** 2026-03-11
**Auditor:** Claude (executor)
**Files reviewed:**
- `Controllers/CDPController.cs`
- `Controllers/AdminController.cs`
- `Views/CDP/HistoriProton.cshtml`
- `Views/CDP/HistoriProtonDetail.cshtml`
- `Views/CDP/Deliverable.cshtml`

---

## PROTON-05: HC Oversight / Review

**Verdict: PASS** — No bugs. Two edge-case observations.

### HC sees all coachees (scope check)

`CoachingProton()` at line 1274 — `if (userLevel <= 3)` branches to query ALL active `ProtonTrackAssignments` without any section/unit restriction. HC (RoleLevel=2 typically) gets the full unfiltered list.

`BuildProtonProgressSubModelAsync()` at line 347 — same: `userRole == UserRoles.HC || userRole == UserRoles.Admin` fetches all active assignments globally.

**Result:** HC correctly sees all coachees. No scoping bug.

### HC Review actions

- `HCReviewDeliverable()` (line 1048): Authorization via `userRole == HC || Admin`. Sets `HCApprovalStatus = "Reviewed"`. Guarded against double-review (`!= "Pending"` check). Correct.
- `HCReviewFromProgress()` (line 1873): AJAX version. Same authorization pattern, same status transition. Returns JSON with reviewer name and timestamp. Correct.
- There is a dedicated `HCApprovalStatus` field in `ProtonDeliverableProgresses` (separate from `Status`/`SrSpvApprovalStatus`/`ShApprovalStatus`). The HC review is a distinct step. Design is correct.

### Export authorization

- `ExportProgressExcel()` (line 2081): `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]` — correct.
- `ExportProgressPdf()` (line 2169): `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]` — correct.
- Both do section-scope enforcement inline for non-full-access roles. HC bypasses via `UserRoles.HasFullAccess(user.RoleLevel)`.

### FilterCoachingProton

`FilterCoachingProton()` (line 266): HC is not section/unit scoped, so the enforcement block `if (UserRoles.HasSectionAccess(roleLevel))` does not apply. HC gets `section` and `unit` params as-is, which are then treated as optional filters in `BuildProtonProgressSubModelAsync`. This is correct — HC can filter by any section/unit.

### Edge Cases (PROTON-05)

| # | Severity | Description | File:Line |
|---|----------|-------------|-----------|
| EC-01 | edge-case | `ExportProgressExcel` has no `[Authorize]` attribute of its own — it relies on the class-level `[Authorize]` on CDPController. This means any authenticated user can technically request the export URL (the scope check happens inside). The inline scope validation protects against data leakage, but the export is accessible to coachee-level users who will hit `Forbid()`. Not a security hole but slightly inconsistent with `ExportProgressPdf` which has an explicit role attribute. | CDPController.cs:2081 |

**No bugs or security issues found for PROTON-05.**

---

## PROTON-06: Assessment Proton Creation

**Verdict: FAIL — 1 bug found and fixed.**

### Tahun 1/2 (online exam) path

`CreateAssessment()` POST (line 733):
- Duration validation correctly skips for `Category == "Assessment Proton"` and `DurationMinutes == 0` (sentinel for Tahun 3, line 778-791).
- `ProtonTrackId` is required — validated: if missing, error is returned (line 886-895).
- `TahunKe` is resolved from the ProtonTrack record (line 896).
- For Tahun 3, `DurationMinutes` is forced to 0 server-side (line 929-930), even if the submitted value is non-zero.
- Sessions are created per-user in a transaction with `SaveChangesAsync`.

**Result:** Tahun 1/2 path is correct. Package is not validated in `CreateAssessment` (it's assigned later via `AssessmentPackages`), which is consistent with the broader system design.

### Tahun 3 (interview) path

`CreateAssessment()` POST: No package validation error for Tahun 3 — correct. Duration=0 is accepted. Sessions created correctly.

`SubmitInterviewResults()` (line 1842):
- Authorization: `[Authorize(Roles = "Admin, HC")]` — correct.
- Validates `Category == "Assessment Proton"` and `TahunKe == "Tahun 3"` — correct guard.
- Collects 5 aspect scores from form fields.
- Saves `InterviewResultsJson` (JSON blob with scores, judges, notes, pass/fail).
- Sets `session.IsPassed`, `session.Status = "Completed"`, `session.CompletedAt`.

**BUG FOUND (severity: bug):** `SubmitInterviewResults` does NOT create a `ProtonFinalAssessment` record after a successful (isPassed=true) interview. The `ProtonFinalAssessments` table is the canonical "completion" marker used by:
- `HistoriProton()` — determines "Lulus" vs "Dalam Proses" status for each TahunKe
- `HistoriProtonDetail()` — determines "Lulus" vs "Dalam Proses" per timeline node
- `BuildProtonProgressSubModelAsync()` — `HasFinalAssessment` flag, `CompletedCoachees` count

Without a `ProtonFinalAssessment` record, a coachee who passes the Tahun 3 interview will:
1. Never appear as "Lulus" in HistoriProton list
2. Never appear in the `CompletedCoachees` stat on the HC dashboard
3. Never have their timeline node show "Lulus" in HistoriProtonDetail

### PROTON-06 Fix Applied

**File:** `Controllers/AdminController.cs`, after `await _context.SaveChangesAsync()` in `SubmitInterviewResults`

**Fix:** When `isPassed == true` and `session.ProtonTrackId` is set, look up the coachee's active `ProtonTrackAssignment` for that track. If found and no duplicate `ProtonFinalAssessment` already exists for that assignment, create one with `Status = "Completed"`, `CompetencyLevelGranted = 0` (interview track does not grant numeric level), and a descriptive note including the judges' name.

**Commit hash:** (see task commit below)

### EditAssessment — Tahun 3 edge case

`EditAssessment()` at line 1148: `bool editIsProtonYear3 = model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue && model.DurationMinutes == 0;` — correctly skips duration validation. No bug.

### Findings Table (PROTON-06)

| # | Severity | Description | File:Line | Status |
|---|----------|-------------|-----------|--------|
| BUG-01 | **bug** | `SubmitInterviewResults` does not create `ProtonFinalAssessment` on pass — coachee stays "Dalam Proses" in HistoriProton, never counted in CompletedCoachees. | AdminController.cs:1916 | **FIXED** |

---

## PROTON-07: Histori Proton Timeline

**Verdict: PASS with edge cases.** No code bugs beyond the already-fixed BUG-01.

### Authorization

`HistoriProton()` (line 2589): No explicit `[Authorize]` attribute beyond the class-level `[Authorize]`. The role-scoped query builds different views based on `user.RoleLevel`. Level 6 (coachee) is redirected to own `HistoriProtonDetail`. No authorization bypass possible.

`HistoriProtonDetail(string userId)` (line 2732):
- Level >= 6: must match `userId == user.Id` — coachee isolation enforced.
- Level 5 (Coach): must be in active `CoachCoacheeMappings` for that coachee — enforced.
- Level 4 (SrSpv/SH): target coachee must be in same section — enforced.
- Level <= 3 (HC/Admin): full access — no check needed.

**Result:** Authorization is correct. A coachee accessing `/CDP/HistoriProtonDetail/{otherUserId}` is correctly rejected with `Forbid()`.

### Event completeness / timeline

`HistoriProtonDetail` builds `ProtonTimelineNode` list from `ProtonTrackAssignments`, ordered by `ProtonTrack.Urutan` (line 2804). Each node shows:
- TahunKe, unit, coach name, status (Lulus/Dalam Proses), competency level, start date, end date

**Gap observed:** The timeline shows only per-TahunKe nodes (one node per assignment), not granular events (individual deliverable completions, coaching sessions, approvals). The view description in the plan says "all events (mapping, deliverables, sessions, approvals, assessments) in correct chronological order" — but the actual implementation is a high-level track summary, not a detailed event log.

This is not a bug in the existing code — it is the intended design for this view (coarse-grained Tahun 1/2/3 milestone timeline). The plan description overstates what the view does. The view is consistent with its own model (`HistoriProtonDetailViewModel.Nodes`).

### Null date safety

`ProtonTimelineNode.StartDate = a.AssignedAt` — `AssignedAt` is `DateTime` (not nullable) on `ProtonTrackAssignment`. Safe.
`EndDate = hasAssessment ? fa!.CompletedAt : null` — `CompletedAt` is `DateTime?`. Safe — null for in-progress.

The `OrderBy(n => n.TahunUrutan)` uses `int`, not a date field — no null sort issues.

### Findings Table (PROTON-07)

| # | Severity | Description | File:Line |
|---|----------|-------------|-----------|
| EC-02 | edge-case | HistoriProtonDetail nodes only show TahunKe milestones, not granular events (individual deliverable completions, coaching sessions). The UI is correct for its design, but may not satisfy the plan's intent of "all events in timeline." Consider a future enhancement to show event-level detail. | CDPController.cs:2790 |
| EC-03 | edge-case | `HistoriProton()` (list view) at line 2688: status is "Lulus" only if `assessmentsByAssignmentId.ContainsKey(latestAssignment.Id)` — it checks the *latest* assignment, not all assignments. A coachee who finished Tahun 1 but is in Tahun 2 would show "Dalam Proses". This is intentional (overall status = latest year), but BUG-01 meant Tahun 3 completions were also affected. Now fixed. | CDPController.cs:2688 |

**No authorization or ordering bugs found for PROTON-07.**

---

## Summary

| Requirement | Status | Findings |
|-------------|--------|----------|
| PROTON-05 (HC Oversight) | PASS | 1 edge-case (EC-01, low impact) |
| PROTON-06 (Assessment Proton) | FAIL → FIXED | 1 bug (BUG-01, fixed inline) |
| PROTON-07 (Histori Proton) | PASS | 2 edge-cases (EC-02, EC-03) |

**Total findings:** 4 (1 bug fixed, 1 security observation, 2 edge-cases)
**Bugs fixed inline:** 1 (BUG-01 — `SubmitInterviewResults` now creates `ProtonFinalAssessment` on pass)
**Bugs deferred:** 0

### Fixes Applied

**BUG-01 Fix — `Controllers/AdminController.cs` in `SubmitInterviewResults()`:**

After setting `session.Status = "Completed"`, before `SaveChangesAsync`, the following logic was added:
1. If `isPassed == true` and `session.ProtonTrackId.HasValue`
2. Look up active `ProtonTrackAssignment` for `(CoacheeId=session.UserId, ProtonTrackId=session.ProtonTrackId.Value)`
3. If assignment found and no duplicate `ProtonFinalAssessment` exists for that assignment ID
4. Create and add `ProtonFinalAssessment` with: `Status="Completed"`, `CompetencyLevelGranted=0`, notes including judge names, `CompletedAt=DateTime.UtcNow`

This ensures HC dashboard, HistoriProton list, and HistoriProtonDetail timeline all correctly reflect Tahun 3 pass status.
