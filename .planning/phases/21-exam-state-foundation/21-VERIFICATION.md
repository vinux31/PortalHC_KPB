---
phase: 21-exam-state-foundation
verified: 2026-02-20T12:53:54Z
status: passed
score: 3/3 must-haves verified
re_verification: false
---

# Phase 21: Exam State Foundation Verification Report

**Phase Goal:** The assessment session model tracks real exam state — workers who load an exam are immediately recorded as InProgress with a start timestamp, and the exam window close date can be configured per assessment

**Verified:** 2026-02-20T12:53:54Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | When a worker loads StartExam for the first time, session Status changes to InProgress and StartedAt is recorded | VERIFIED | `CMPController.cs` lines 1555–1559: `if (assessment.StartedAt == null) { assessment.Status = "InProgress"; assessment.StartedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }` |
| 2 | Reloading the exam page does not reset StartedAt — the timestamp is only written when StartedAt is null | VERIFIED | Guard at line 1555 is `assessment.StartedAt == null` (timestamp-authoritative, not status-string). On second load, StartedAt is already set so the block is skipped entirely. |
| 3 | InProgress status is visible as a distinct badge in the monitoring detail view alongside Open and Completed | VERIFIED | `AssessmentMonitoringDetail.cshtml` lines 117–122: three-way switch renders "InProgress" as `bg-warning text-dark` badge; "Completed" as `bg-success`; default as `bg-light text-dark border` |

**Score:** 3/3 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/AssessmentSession.cs` | `DateTime? StartedAt` property | VERIFIED | Line 37: `public DateTime? StartedAt { get; set; }` present after `CompletedAt` |
| `Controllers/CMPController.cs` | Idempotent InProgress state write in StartExam GET | VERIFIED | Lines 1554–1560: `if (assessment.StartedAt == null)` guard with Status+StartedAt assignment and SaveChangesAsync |
| `Models/AssessmentMonitoringViewModel.cs` | InProgress status derivation + StartedAt property on MonitoringSessionViewModel | VERIFIED | Line 20 comment says `"Not started", "InProgress", or "Completed"`; line 24: `public DateTime? StartedAt { get; set; }` |
| `Views/CMP/AssessmentMonitoringDetail.cshtml` | InProgress badge (bg-warning) in per-user status table | VERIFIED | Lines 117–122: switch expression with `"InProgress" => "bg-warning text-dark"` |
| `Migrations/20260220124827_AddExamStateFields.cs` | AddColumn StartedAt datetime2 nullable on AssessmentSessions | VERIFIED | File exists; Up() contains `migrationBuilder.AddColumn<DateTime>(name: "StartedAt", table: "AssessmentSessions", type: "datetime2", nullable: true)` |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `CMPController.cs` StartExam GET | AssessmentSessions table | `assessment.StartedAt == null` guard + `SaveChangesAsync()` | WIRED | Lines 1555–1559 perform the write atomically. Guard is timestamp-based (authoritative), not status-string-based. |
| `CMPController.cs` AssessmentMonitoringDetail | `MonitoringSessionViewModel.UserStatus` | Three-state derivation: CompletedAt/Score → Completed, StartedAt != null → InProgress, else Not started | WIRED | Lines 342–348: correct three-state logic. UserStatus flows through to view via `MonitoringGroupViewModel.Sessions`. |
| `AssessmentMonitoringDetail.cshtml` | `MonitoringSessionViewModel.UserStatus` | Switch expression renders InProgress as bg-warning badge | WIRED | Lines 117–122: `session.UserStatus switch { "Completed" => "bg-success", "InProgress" => "bg-warning text-dark", _ => "bg-light text-dark border" }` |
| `CMPController.cs` GetMonitorData | AssessmentSessions query | `Status == "InProgress"` in Where filter | WIRED (partial) | Line 258: InProgress sessions included in query. Line 295: `hasOpen` boolean accounts for InProgress sessions. However, UserStatus projection inside GetMonitorData (line 288) still uses two-state `isCompleted ? "Completed" : "Not started"` — InProgress workers show as "Not started" in the card-list widget. This does NOT affect the monitoring detail view. See warning below. |

### Requirements Coverage

| Requirement | Status | Notes |
|-------------|--------|-------|
| LIFE-01: System records StartedAt and sets InProgress on first exam load | SATISFIED | StartExam GET idempotent write confirmed |
| Idempotent: Reloading exam does not reset StartedAt | SATISFIED | `StartedAt == null` guard confirmed |
| Monitoring visibility: InProgress badge in AssessmentMonitoringDetail | SATISFIED | Three-way switch in view confirmed |
| No regression: Completed sessions still show Completed | SATISFIED | `CompletedAt != null \|\| Score != null` check is first branch in three-state logic |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Controllers/CMPController.cs` | 288 | `UserStatus = isCompleted ? "Completed" : "Not started"` in GetMonitorData (two-state, not three-state) | WARNING | InProgress workers show as "Not started" in the Assessment monitoring card-list widget (Assessment.cshtml manage tab). Does NOT affect AssessmentMonitoringDetail per-user table. Phase goal targets the detail view — this is a secondary inconsistency. |
| `Models/AssessmentSession.cs` | 20 | `// "Open", "Upcoming", "Completed"` — comment does not list "InProgress" | INFO | Documentation only; runtime behaviour is correct since Status is an unconstrained string. No functional impact. |

No TODO/FIXME/placeholder patterns found in any modified file.

### Human Verification Required

#### 1. End-to-end first-load InProgress write

**Test:** Log in as a worker with an Open assessment. Navigate to the StartExam page. Check the database row for that session (or reload AssessmentMonitoringDetail as HC).
**Expected:** Session Status = 'InProgress', StartedAt = UTC timestamp. AssessmentMonitoringDetail shows "InProgress" badge (yellow) for that worker.
**Why human:** Cannot execute the web request or query the live SQL Server database programmatically from this context.

#### 2. Reload idempotency

**Test:** After the worker's session is InProgress, reload the StartExam page (F5) one or more times.
**Expected:** StartedAt timestamp does not change. Status remains 'InProgress'.
**Why human:** Requires live DB state comparison between two web requests.

#### 3. InProgress vs Completed status priority

**Test:** Submit an exam (SubmitExam POST). Then check AssessmentMonitoringDetail.
**Expected:** Worker's status shows "Completed" (green badge), not "InProgress". Score is visible.
**Why human:** Requires submitting a live exam form.

### Gaps Summary

No gaps found. All three observable truths are verified against actual code. All artifacts exist, are substantive, and are correctly wired. The two warnings noted (GetMonitorData two-state UserStatus and outdated comment) are below the blocker threshold — they do not prevent the phase goal from being achieved.

The GetMonitorData warning is worth noting for Phase 22: if that widget is expected to surface InProgress workers in the card-list summary, its UserStatus projection will need to be updated to three-state logic matching AssessmentMonitoringDetail.

---

_Verified: 2026-02-20T12:53:54Z_
_Verifier: Claude (gsd-verifier)_
