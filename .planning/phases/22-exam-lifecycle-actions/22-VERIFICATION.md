---
phase: 22-exam-lifecycle-actions
verified: 2026-02-20T14:30:00Z
status: passed
score: 6/6 must-haves verified
gaps: []
human_verification:
  - test: Click Keluar Ujian on an active exam and confirm the dialog
    expected: Browser confirm dialog shows; clicking OK submits abandon form; session shows Abandoned in HC monitoring
    why_human: Confirm dialog and form submission require browser interaction
  - test: Start exam; manipulate StartedAt to exceed DurationMinutes+2; then submit
    expected: Server rejects with error and redirects to StartExam
    why_human: Requires database manipulation and live browser to test the timing branch
  - test: Set ExamWindowCloseDate to past date; navigate to StartExam for that session
    expected: Error Ujian sudah ditutup. shown; redirected to Assessment lobby
    why_human: Requires live database state and browser navigation
---

# Phase 22: Exam Lifecycle Actions Verification Report

**Phase Goal:** Workers can intentionally exit an in-progress exam, HC can force-close or reset sessions for management, and the system enforces both server-side timer limits and configurable exam window close dates.

**Verified:** 2026-02-20T14:30:00Z
**Status:** passed
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Worker sees Keluar Ujian button; clicking with confirmation marks session Abandoned and redirects to lobby | VERIFIED | StartExam.cshtml lines 18-22 (button), 31-34 (form), 273-284 (confirmAbandon JS); AbandonExam POST lines 1832-1860 sets Status=Abandoned, redirects to Assessment |
| 2 | SubmitExam POST rejects when elapsed > DurationMinutes + 2min grace; redirects to StartExam with error | VERIFIED | CMPController.cs lines 2120-2128: elapsed check, allowedMinutes = DurationMinutes + 2, TempData error, RedirectToAction StartExam |
| 3 | HC sees Reset button on Completed AND Abandoned rows; Reset clears Score/IsPassed/CompletedAt/StartedAt; deletes UserResponse + UserPackageAssignment; Status to Open | VERIFIED | AssessmentMonitoringDetail.cshtml line 177 covers Completed and Abandoned; ResetAssessment lines 408-430 deletes both types; resets all fields |
| 4 | HC sees Force Close on InProgress and Not started rows; Force Close sets Status=Completed, Score=0, IsPassed=false, CompletedAt=now | VERIFIED | AssessmentMonitoringDetail.cshtml line 189 covers InProgress and Not started; ForceCloseAssessment lines 466-471; DB guard allows Open + InProgress |
| 5 | ExamWindowCloseDate enforced in StartExam GET -- workers redirected with Ujian sudah ditutup if UtcNow > ExamWindowCloseDate | VERIFIED | CMPController.cs lines 1662-1666: HasValue + UtcNow > value; error begins Ujian sudah ditutup.; redirects to Assessment lobby |
| 6 | Abandoned sessions show UserStatus=Abandoned (not InProgress) -- Abandoned branch before StartedAt check | VERIFIED | CMPController.cs lines 342-350: Completed (343), Abandoned (345), InProgress via StartedAt (347) -- correct ordering |

**Score:** 6/6 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|-------|
| Controllers/CMPController.cs | AbandonExam, ResetAssessment, ForceCloseAssessment + timer/window enforcement | VERIFIED | All actions present; timer lines 2120-2128; window lines 1662-1666 |
| Views/CMP/StartExam.cshtml | Keluar Ujian button + hidden abandon form + confirmAbandon JS | VERIFIED | Button lines 18-22, form lines 31-34, JS lines 272-284 |
| Views/CMP/AssessmentMonitoringDetail.cshtml | Reset for Completed/Abandoned; Force Close for InProgress/Not started | VERIFIED | Reset lines 177-187, Force Close lines 189-199 |
| Models/AssessmentSession.cs | ExamWindowCloseDate and StartedAt properties | VERIFIED | ExamWindowCloseDate line 43, StartedAt line 37 |
| Models/AssessmentMonitoringViewModel.cs | MonitoringSessionViewModel with UserStatus field | VERIFIED | UserStatus at line 20; all needed fields present |
| Migrations/20260220135244_AddExamWindowCloseDate.cs | Adds nullable ExamWindowCloseDate column | VERIFIED | Adds nullable datetime2 to AssessmentSessions |
| Migrations/20260220124827_AddExamStateFields.cs | Adds nullable StartedAt column | VERIFIED | Adds nullable datetime2 StartedAt to AssessmentSessions |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|-------|
| StartExam.cshtml Keluar button | AbandonExam POST | asp-action=AbandonExam on hidden form; confirmAbandon() submits | WIRED | Form at line 31; JS at line 282 |
| AssessmentMonitoringDetail Reset form | ResetAssessment POST | asp-action=ResetAssessment | WIRED | Line 180 in view; controller at line 389 |
| AssessmentMonitoringDetail Force Close form | ForceCloseAssessment POST | asp-action=ForceCloseAssessment | WIRED | Line 192 in view; controller at line 446 |
| SubmitExam timer check | StartedAt field | assessment.StartedAt.HasValue + elapsed calculation | WIRED | Lines 2120-2128; StartedAt set at exam start (line 1679) |
| StartExam GET window check | ExamWindowCloseDate field | HasValue AND UtcNow > value | WIRED | Line 1662; field on model (line 43); propagated from Create/Edit |
| Projection logic | Abandoned status badge | Status check order in sessionViewModels.Select | WIRED | Lines 342-350: Abandoned before StartedAt!=null InProgress branch |

### Requirements Coverage

| Requirement | Status | Notes |
|-------------|--------|-----|
| LIFE-02 | SATISFIED | AbandonExam POST sets Status=Abandoned; re-entry blocked at lines 1669-1673 |
| LIFE-03 | SATISFIED | SubmitExam lines 2120-2128: DurationMinutes + 2min grace enforced server-side |
| LIFE-04 | SATISFIED | ResetAssessment clears all state fields; deletes responses and package assignment |
| LIFE-05 | SATISFIED | ForceCloseAssessment sets Status=Completed/Score=0/IsPassed=false |
| DATA-03 | SATISFIED | StartExam GET lines 1662-1666 enforces cutoff before any session state changes |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|------|
| Models/AssessmentMonitoringViewModel.cs | 20 | UserStatus comment omits Abandoned as valid value | Info | Documentation only; runtime behavior is correct |

No blocker or warning-level anti-patterns found.

### Human Verification Required

#### 1. Keluar Ujian confirm dialog and form submission

**Test:** Start an exam as a worker; click the Keluar Ujian button; confirm the browser dialog.
**Expected:** Session status changes to Abandoned in HC AssessmentMonitoringDetail; worker sees info message and lands on Assessment lobby.
**Why human:** Browser confirm dialog and form POST require a live browser session.

#### 2. Server-side timer rejection

**Test:** Start an exam; manipulate StartedAt in the database to exceed DurationMinutes + 2 minutes ago; then submit answers.
**Expected:** Server returns error Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses. and redirects to StartExam.
**Why human:** Requires database manipulation and live browser submit to exercise the timing branch.

#### 3. ExamWindowCloseDate cutoff enforcement

**Test:** Set ExamWindowCloseDate to a past datetime on an Open session; navigate to StartExam as a worker.
**Expected:** Error Ujian sudah ditutup. Waktu ujian telah berakhir. shown; user redirected to Assessment lobby.
**Why human:** Requires a live session with a past close date in the database.

### Gaps Summary

No gaps. All six must-haves are verified against the actual codebase. The implementation is substantive and wired end-to-end.

Key implementation notes:

- Keluar Ujian flow (must-have 1): fully implemented with button, hidden form, JS confirmation, POST handler, and correct Status=Abandoned transition.
- Timer enforcement (must-have 2): server-side with 2-minute grace, not relying solely on client-side countdown.
- Reset (must-have 3): removes both legacy UserResponse records and modern UserPackageAssignment records before resetting all session fields to null/zero/Open.
- Force Close (must-have 4): maps UI Not started to DB Open status -- controller guard accepts both Open and InProgress, matching both display states correctly.
- ExamWindowCloseDate (must-have 5): enforced before any session state changes. Error begins Ujian sudah ditutup. Redirect goes to Assessment lobby, appropriate since the window is permanently closed.
- Abandoned status ordering (must-have 6): projection checks Completed first, then Abandoned, then InProgress via StartedAt -- preventing misclassification even when StartedAt is set.

---

_Verified: 2026-02-20T14:30:00Z_
_Verifier: Claude (gsd-verifier)_
