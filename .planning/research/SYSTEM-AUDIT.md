# Assessment System Audit

**System:** Portal HC KPB — CMP Assessment Module (ASP.NET Core 8.0 MVC)
**Date:** 2026-02-24
**Scope:** Full assessment lifecycle from HC creation through worker completion; interactions with v2.1 features (auto-save, session resume, early close)
**Analyzed Files:**
- Controllers/CMPController.cs (4,963 lines)
- Views/CMP/StartExam.cshtml
- Views/CMP/AssessmentMonitoringDetail.cshtml
- Models: AssessmentSession, PackageUserResponse, UserPackageAssignment

---

## Executive Summary

The assessment system is structurally sound but exhibits **critical concurrency and timing bugs** that will surface during v2.1 (auto-save + polling). The system has two divergent grading paths (legacy AssessmentQuestion + package-based), creating complexity and inconsistency. Timer enforcement is purely client-side client-only in the UI, with inadequate server-side recovery from network disconnection. The most severe issue is a **race condition between CloseEarly and SubmitExam** that can cause duplicate scoring or data loss.

**Key findings:**
- **P0 (Critical):** Race condition on concurrent SubmitExam + CloseEarly; timer enforcement gaps; session state machine inconsistencies
- **P1 (High):** Missing network recovery; insufficient progress tracking; incomplete monitoring visibility
- **P2 (Medium):** UX confusion on token gates; missing validation on timer restart

---

## Flow Analysis

### Flow 1: HC Creates Assessment → Assigns Workers → Sets Schedule + Close Date

**Works:**
- Assessment creation persists to DB with Title, Category, Schedule, DurationMinutes
- ExamWindowCloseDate is optional; null = no hard cutoff (relies on Schedule date only)
- Workers are implicitly "assigned" by creating AssessmentSession records (one session per worker per assessment group)
- Assessment appears in worker's personal "Assessment" view filtered by UserId
- Status auto-transitions: Upcoming → Open when Schedule <= WIB time

**Bugs/Edge Cases:**
1. **No explicit close date requirement:** HC can leave ExamWindowCloseDate null, relying only on Schedule date. If schedule time is not enforced server-side for re-entry, workers could re-open exam after schedule time if they navigate directly via URL + cached token.
   - **Root cause:** StartExam checks `ExamWindowCloseDate` (line 2715) but allows Upcoming → Open auto-transition regardless of whether window has closed (line 2681).
   - **Severity:** LOW — token guard and status check mitigate, but timing logic is fragile.

2. **No validation on close date < schedule:** HC can set ExamWindowCloseDate *before* Schedule date. If a worker starts after Schedule but before the window close, the window might already be past. No warning to HC about this misconfiguration.
   - **Root cause:** No server-side validation when saving assessment parameters.
   - **Severity:** LOW — affects only misconfigured assessments; easily fixed by HC re-editing.

3. **Assessment group identification relies on exact Title + Category + Schedule.Date match:**
   - If HC creates two assessments with same Title/Category on same day but different hours, they are treated as one group for package assignments, competency updates, and CloseEarly logic.
   - **Root cause:** No explicit "assessment group ID"; grouping is string-based in multiple places (StartExam line 2739, CloseEarly line 792).
   - **Severity:** MEDIUM — data inconsistency if duplicate assessments exist; manifests as wrong packages or wrong competency mappings.

**Missing:**
- No HC dashboard showing upcoming close dates (only visible in monitoring detail view)
- No reminder/warning if ExamWindowCloseDate is not set
- No audit trail showing when HC set or modified ExamWindowCloseDate
- No worker notification when ExamWindowCloseDate changes

---

### Flow 2: Worker Starts Exam → Answers Questions → Submits

**Works:**
- StartExam validates token (if required), checks ExamWindowCloseDate, marks session as InProgress
- Questions are loaded with Fisher-Yates shuffle (per-user, per-option)
- Client-side timer counts down from DurationMinutes * 60
- JavaScript tracks answered questions in Set
- SaveAnswer fires async (fire-and-forget) on each radio change, upserting PackageUserResponse (lines 1050-1067)
- SubmitExam re-validates timer (2-minute grace, line 3191), grades all answers, calculates percentage, sets IsPassed
- Competency levels auto-update on pass
- Results page displays score + pass/fail status
- Session status persists as Completed with CompletedAt timestamp

**Bugs/Edge Cases:**

1. **Timer is purely client-side:**
   - JavaScript startExam.cshtml line 204 runs a 1-second interval decrementing a local variable
   - Server-side enforcement only happens at **submit time** (SubmitExam line 3190-3196)
   - **Attack vector:** Worker opens DevTools, sets `timeRemaining = 9999`, bypasses auto-submit on 0
   - **Network issue:** Worker loses connection at T+30min (exam duration 60 min). JavaScript timer keeps running. Worker reconnects at T+31 min. Timer shows 29 min remaining (incorrect). Worker submits. Server validates elapsed time since StartedAt and rejects (line 3192-3196). Worker sees error and no answers saved. **Data loss.**
   - **Severity:** HIGH — affects both security (timer bypass) and reliability (disconnection recovery).

2. **SaveAnswer is fire-and-forget with silent failure:**
   - Line 233 in StartExam.cshtml: `.catch(function() { /* ignore network errors */ })`
   - If network fails, answer is NOT saved to database but IS stored in hidden form input for later submit
   - If HC runs CloseEarly while SaveAnswer fails, that answer is lost (never scored)
   - **Root cause:** SaveAnswer doesn't retry or notify worker of failure
   - **Severity:** HIGH — answers can be silently lost; CloseEarly won't score them.

3. **SubmitExam race condition with CloseEarly:**
   - Worker submits answers (SubmitExam line 3163 loads session)
   - HC simultaneously clicks "Submit Assessment" (CloseEarly line 789 loads **all** sessions in group)
   - Both methods call SaveChangesAsync() on overlapping AssessmentSession records
   - If CloseEarly sets Status=Completed, Score, CompletedAt **between** SubmitExam's load and its SaveChangesAsync, SubmitExam's SaveChangesAsync will overwrite CloseEarly's score with the worker's final submission score
   - **Root cause:** No optimistic concurrency control (version field), no database-level lock, no transaction isolation
   - **Severity:** CRITICAL — can result in wrong score or data loss. Example: CloseEarly scores InProgress worker at 35%, SubmitExam overwrites with 75%.

4. **Re-entry from Completed sessions is blocked, but Abandoned is also blocked:**
   - StartExam line 2722-2726: if Status == "Abandoned", reject with error "hubungi HC untuk mengulang"
   - But AssessmentMonitoringDetail provides a Reset button for Abandoned sessions
   - If HC hasn't reset, worker cannot re-enter; if HC resets, worker's old answers are deleted
   - UX issue: Worker doesn't know they need HC approval; appears as "access denied" rather than "awaiting reset"
   - **Severity:** LOW — by design, but UX could be clearer.

5. **DurationMinutes zero or negative not validated:**
   - If HC sets DurationMinutes to 0, timer immediately fires auto-submit (line 197-200)
   - If negative, JavaScript uses `Math.floor(negativeValue / 60)` → NaN or unexpected behavior
   - **Root cause:** No validation on AssessmentSession creation/edit
   - **Severity:** LOW — unusual misconfiguration; not exploitable by workers.

**Missing:**
- No progress checkpoint saves to database at intervals (relies entirely on final submit)
- No graceful reconnection: worker must remember their answers and resubmit manually from memory
- No visual indication that SaveAnswer succeeded/failed
- No retry mechanism for failed SaveAnswer calls
- No server-side time validation that timer doesn't exceed DurationMinutes + 2-min grace

---

### Flow 3: Worker Starts Exam → Disconnects Mid-Exam → Reconnects

**Current behavior:**
1. Worker loads StartExam, session marked as InProgress, timer begins
2. Network drops (router failure, browser crash, etc.)
3. Worker's browser still has JavaScript running (timer ticking locally)
4. Worker reconnects after 10 minutes
5. Worker manually refreshes page or navigates back to Assessment lobby

**What happens:**
- If worker navigates back to Assessment lobby: session is still InProgress (no timeout recovery). Assessment appears in "actionable" list. Worker clicks StartExam again.
- StartExam line 2729-2733: checks if StartedAt == null (idempotent check). **StartedAt is NOT null**, so session does NOT get marked InProgress again; code skips to loading packages.
- JavaScript timer restarts from DurationMinutes (full timer value), not remaining time
- Previous SaveAnswer calls may or may not have persisted (depends on if fetch completed before network dropped)

**Bugs:**
1. **Lost progress:** Answers saved before disconnect are preserved in PackageUserResponse, but answers saved *during* the failed SaveAnswer request are lost. Worker will not know which answers were saved.
   - **Root cause:** SaveAnswer is fire-and-forget; worker receives no confirmation.
   - **Severity:** HIGH — worker loses answers without notification.

2. **Timer reset on reload:**
   - Timer is JavaScript state, not persisted to server
   - On page reload, timer resets to full DurationMinutes
   - If worker was at 30 min elapsed, reconnects, and reloads page, timer now shows full time again
   - Worker gets false confidence about remaining time
   - **Root cause:** Timer state not sent to server; no server-side elapsed time calculation on page reload
   - **Severity:** HIGH — timer display is incorrect, can lead to worker running overtime.

3. **No idle timeout or heartbeat:**
   - If worker goes offline for 2 hours, session remains InProgress forever
   - HC cannot distinguish between a worker who is actually in the exam vs. one who walked away with browser open
   - **Root cause:** No server-side session timeout; no heartbeat/keep-alive mechanism
   - **Severity:** MEDIUM — affects HC monitoring accuracy.

4. **ExamWindowCloseDate check happens only at StartExam, not on reload:**
   - If worker starts exam at 14:00, ExamWindowCloseDate is set to 15:00 by HC via CloseEarly at 14:30
   - Worker's JavaScript is still running (timer not reset on server)
   - CheckExamStatus (line 1075) detects ExamWindowCloseDate in the past, returns { closed: true }, redirects worker to Results page
   - Worker's unsaved answers in hidden form inputs are discarded
   - **Root cause:** No confirmation dialog; worker doesn't get a chance to save answers before redirect
   - **Severity:** HIGH — answers in memory are lost if not yet submitted.

**Missing:**
- No session resumption: if worker re-enters mid-exam, timer should resume from correct elapsed time, not reset
- No auto-save checkpoint every N seconds to ensure some answers are persisted
- No persistent timer on server to recover correct remaining time on reload
- No "session state snapshot" to warn worker if they've been idle for X minutes

---

### Flow 4: HC Clicks "Submit Assessment" (CloseEarly) While Workers in Various States

**Precondition:**
- Assessment group has 10 workers: 2 Not Started, 4 InProgress, 3 Completed, 1 Abandoned

**What should happen:**
- ExamWindowCloseDate set to now (locks out Not Started workers)
- InProgress workers scored from their SaveAnswer records
- Completed workers unaffected
- Abandoned workers unaffected

**What currently happens:**

1. **Not Started workers (status=Open, StartedAt=null):**
   - CloseEarly sets ExamWindowCloseDate = now, skips scoring (line 852: `if (!isInProgress) continue`)
   - Next time worker clicks StartExam, ExamWindowCloseDate is checked (line 2715): DateTime.UtcNow > ExamWindowCloseDate = true
   - Error: "Ujian sudah ditutup. Waktu ujian telah berakhir."
   - **Works correctly.**

2. **InProgress workers (status=InProgress, StartedAt!=null, CompletedAt=null, Score=null):**
   - CloseEarly loads all PackageUserResponses for this session (line 864)
   - Scores from PackageUserResponses (not from form submission or client-side state)
   - Sets Score, Status=Completed, IsPassed, CompletedAt
   - **Works correctly IF SaveAnswer calls have persisted.** Race condition if SaveAnswer is in-flight.

3. **Completed workers (status=Completed, CompletedAt!=null):**
   - Line 852: `if (!isInProgress) continue` — skipped
   - Score, Status, CompletedAt left unchanged
   - **Works correctly.**

4. **Abandoned workers (status=Abandoned, CompletedAt=null):**
   - Line 852: checks `if (!isInProgress)` — Abandoned is not InProgress, so skipped
   - ExamWindowCloseDate still set, but session score not recalculated
   - **Works correctly.**

**Edge Cases:**

1. **InProgress but score already calculated (shouldn't exist, but does if CloseEarly runs twice):**
   - CloseEarly line 852 checks `bool isInProgress = session.StartedAt != null && session.CompletedAt == null && session.Score == null`
   - If CloseEarly runs once and scores a worker at 60%, worker's Score is now non-null
   - If CloseEarly runs again (HC accidentally clicks button twice, or idempotent retry), isInProgress will be false (Score is non-null), so worker is skipped
   - **Idempotent behavior — works correctly.**

2. **Worker submits *exactly at the same time* HC clicks CloseEarly:**
   - Worker: SubmitExam loads session (line 3163)
   - HC: CloseEarly loads all sessions (line 791)
   - Both modify session, both call SaveChangesAsync on overlapping records
   - Depending on database transaction isolation level, one write may overwrite the other
   - **RACE CONDITION — CRITICAL.** See Flow 2 analysis.

3. **CloseEarly on legacy (non-package) assessment:**
   - CloseEarly detects package mode (line 805-807)
   - If not package mode, loads legacyQuestions from a sibling session (line 835-840)
   - **Issue:** If no sibling session has questions loaded yet, legacyQuestions is empty
   - InProgress worker is scored at 0% (maxScore=0, finalPercentage = 0)
   - **Severity:** HIGH — incorrect score due to missing sibling data.

4. **CloseEarly and ForceCloseAssessment both trigger on same group:**
   - HC clicks "Submit Assessment" (CloseEarly) — sets ExamWindowCloseDate, scores InProgress
   - User navigates to monitoring detail, sees "Force Close All" button
   - HC clicks "Force Close All" (ForceCloseAll) — forces close all Open/InProgress, sets Score=0
   - Both operations modify same sessions
   - If CloseEarly's SaveChangesAsync hasn't completed, ForceCloseAll might load stale data
   - **RACE CONDITION — less likely than Flow 2, but possible.**

**Missing:**
- No warning to HC showing how many workers will be affected by CloseEarly (e.g., "3 InProgress, 2 Not Started")
- No HC confirmation of which workers are currently InProgress vs. Not Started
- No record of when ExamWindowCloseDate was set (audit log only records CloseEarly action, not timestamp)
- No HC ability to *cancel* CloseEarly after it's been triggered (idempotent re-run is only mitigation)

---

### Flow 5: Worker Tries to Access Exam After ExamWindowCloseDate

**Scenario 1: Not Started worker, ExamWindowCloseDate has passed**
- Worker clicks StartExam
- StartExam line 2715: `if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow > assessment.ExamWindowCloseDate.Value)`
- Error: "Ujian sudah ditutup. Waktu ujian telah berakhir."
- Redirect to Assessment lobby
- **Works correctly.**

**Scenario 2: InProgress worker, ExamWindowCloseDate set by CloseEarly**
- Worker is mid-exam, JavaScript timer running
- HC clicks "Submit Assessment" (CloseEarly)
- CloseEarly sets ExamWindowCloseDate = now
- Worker's JavaScript every 30 seconds calls CheckExamStatus (line 346)
- CheckExamStatus line 1090: `if (session.ExamWindowCloseDate.HasValue && DateTime.UtcNow > assessment.ExamWindowCloseDate.Value) isClosed = true`
- CheckExamStatus returns { closed: true, redirectUrl: "/CMP/Results?id=..." }
- JavaScript (line 324-340) shows banner, clears timers, redirects after 3 seconds
- **Works correctly** — but worker's answers in form inputs are discarded (not submitted).

**Scenario 3: Worker offline, reconnects after ExamWindowCloseDate has passed**
- Worker was InProgress, network dropped
- Worker reconnects after ExamWindowCloseDate has passed
- Worker manually navigates back to StartExam
- StartExam line 2715: checks ExamWindowCloseDate, rejects
- Error page shown
- **Works correctly.**

**Edge Case: Clock skew (server time vs. client time)**
- Worker's system clock is 5 minutes behind server
- Worker thinks timer has 5 min remaining (client shows 5:00)
- Worker submits answers
- Server checks elapsed time: `DateTime.UtcNow - StartedAt > DurationMinutes + 2min grace`
- Depending on server's UTC vs. client's local time, elapsed could be calculated differently
- **Severity:** LOW — rare in practice; UTC handles most skew, but edge cases possible in high-latency networks.

**Missing:**
- No advance warning to worker if ExamWindowCloseDate is approaching
- No countdown displayed to worker showing "Exam closes in 5 minutes"
- No "save answers and exit gracefully" option when window closes (just abrupt redirect)

---

### Flow 6: HC Force-Closes a Session, HC Resets a Session

**Force Close (ForceCloseAssessment):**

**Current behavior:**
- Only callable on Open or InProgress sessions (line 571)
- Sets Status=Completed, Score=0, IsPassed=false immediately (no scoring logic)
- User sees failure message, completion recorded with zero score
- Audit log records action

**Bugs:**
1. **Zero score is implicit failure:**
   - If PassPercentage=70, Score=0 will always result in IsPassed=false
   - But if a worker legitimately answered 5/10 questions correctly (50% score) and HC force-closed after 5 min, the worker gets 0%, which is harsher than their actual score
   - **Root cause:** Force close doesn't calculate score from SaveAnswer records; just sets 0
   - **Severity:** MEDIUM — punishes worker unfairly; should score from available answers instead.

2. **No NotStarted option in UI:**
   - AssessmentMonitoringDetail line 240-251 shows Force Close button only for InProgress or Not Started
   - But ForceCloseAssessment line 571 rejects "Not Started" with error
   - **UX inconsistency:** Button appears but fails
   - **Root cause:** UI condition doesn't match server-side validation
   - **Severity:** LOW — UI bug, easily fixed.

3. **Competency levels not rolled back:**
   - If worker previously passed an assessment and gained competency level, then HC force-closes a new attempt, competency is not revoked
   - CloseEarly updates competencies on pass (line 892-934), but ForceCloseAssessment does not (line 583-610)
   - **Root cause:** ForceCloseAssessment just sets score=0; doesn't implement competency logic
   - **Severity:** MEDIUM — competency record may no longer reflect actual worker capability if old assessment is force-closed.

**Reset (ResetAssessment):**

**Current behavior:**
- Only callable on Completed or Abandoned sessions (line 496)
- Deletes UserResponse + PackageUserResponse + UserPackageAssignment records
- Resets session to Open with Score=null, Status="Open", StartedAt=null, Progress=0
- Audit log records action

**Bugs:**
1. **No competency rollback:**
   - If worker passed and was granted competency level, then HC resets the session, competency is not revoked
   - Worker retakes exam, fails, but competency record still shows passed
   - **Root cause:** ResetAssessment doesn't interact with UserCompetencyLevels table
   - **Severity:** MEDIUM — competency record becomes stale if worker fails after reset.

2. **Reshuffle not triggered on reset:**
   - ResetAssessment deletes UserPackageAssignment, but if HC previously reshuffled the package, next StartExam will assign a *new* random package
   - This is correct (fresh shuffle), but AssessmentMonitoringDetail's "Reshuffle" button (line 218) is disabled for Completed sessions, making it unclear to HC whether the next attempt will get a new shuffle
   - **Root cause:** UI doesn't show that Reset + StartExam = new shuffle
   - **Severity:** LOW — works correctly, just not transparent to HC.

3. **No notification to worker that session was reset:**
   - Worker sees their old Completed assessment disappear from "Riwayat Ujian" (Assessment history)
   - Next time they load Assessment lobby, session is back in "Open" list
   - No email or message explaining why
   - **Root cause:** No notification service; only TempData message for HC
   - **Severity:** LOW — worker discovers on next login, can ask HC if confused.

**Missing:**
- No "undo reset" for HC (soft delete with recovery would help)
- No HC ability to force close with a score other than 0 (e.g., force close with "DNF" marker instead of 0)
- No audit trail showing old vs. new state after reset (only "reset action", not the deleted answers)

---

### Flow 7: Worker Views Results Page After Completion

**Current behavior:**
- SubmitExam redirects to Results action (line 3304)
- Results page loads session with Score, IsPassed, CompletedAt
- If AllowAnswerReview=true, displays answer breakdown
- Competency updates are persisted

**Works:**
- Score and pass/fail status displayed correctly
- Timestamp shows when completed
- Answer review (if enabled) shows correct/incorrect indicators

**Bugs:**
1. **No distinction between submitted and CloseEarly-scored results:**
   - If worker actively submitted, their answer review shows what they selected
   - If HC CloseEarly-scored the worker, their answer review shows what they answered (from PackageUserResponses), which is identical
   - However, the **context** is different: worker intentionally submitted vs. HC force-completed
   - No Results page banner indicating "This assessment was closed early by administrator"
   - **Root cause:** CloseEarly doesn't set a flag on session to mark it as early-closed
   - **Severity:** LOW — affects transparency, not functionality. Worker might think they submitted normally.

2. **No timestamp showing when CloseEarly happened:**
   - If HC CloseEarly-scores a worker, CompletedAt is set to DateTime.UtcNow (the time of CloseEarly)
   - Results page displays this timestamp
   - Worker might think they finished at that time, not realizing HC closed it early
   - **Root cause:** No separate "ClosedEarlyAt" field; using CompletedAt for both scenarios
   - **Severity:** LOW — timestamps are accurate but context is lost.

3. **AllowAnswerReview doesn't apply to CloseEarly-scored workers:**
   - If AllowAnswerReview=false, worker shouldn't see their answers
   - But PackageUserResponses are still stored in DB for CloseEarly scoring
   - Results page always loads them if available (no check for AllowAnswerReview)
   - **Root cause:** No enforcement of AllowAnswerReview on Results view
   - **Severity:** MEDIUM — security/integrity issue if assessment is supposed to be reviewed only by HC.

**Missing:**
- No option for worker to request review/explanation from HC
- No certificate generation or download (mentioned in Views but not checked in Results implementation)
- No comparison with previous attempts (historical trend)

---

## Critical Issues (P0 — Must Fix)

### P0-1: Race Condition Between SubmitExam and CloseEarly

**Description:**
Two concurrent requests can corrupt assessment scoring:
1. Worker submits answers (SubmitExam loads session, calculates score)
2. HC simultaneously runs CloseEarly (loads all group sessions, scores InProgress workers)
3. Both call SaveChangesAsync on overlapping records

**Root Cause:**
- No optimistic concurrency control (EF Core RowVersion/ConcurrencyToken)
- No pessimistic locking (database lock)
- No serialization isolation level enforcement
- AssessmentSession records lack any timestamp or version field

**Manifest As:**
- Worker's final score overwritten with CloseEarly's score (or vice versa)
- CompletedAt timestamp from one operation erased by the other
- Competency level updates duplicated or lost

**Attack Scenario:**
Worker is InProgress with 3 answers saved. Worker submits (hidden form has those 3 answers). CloseEarly fires. Worker's SubmitExam overwrites CloseEarly's score calculation. Audit log shows both actions, but database only reflects one.

**Fix Required:**
- Add EF Core `[ConcurrencyCheck]` attribute to AssessmentSession.UpdatedAt
- Wrap SubmitExam and CloseEarly in explicit transactions with IsolationLevel.Serializable
- Or: implement "early-close flag" that blocks SubmitExam if exam is in closing state

**Affected Methods:**
- SubmitExam (line 3161)
- CloseEarly (line 788)

---

### P0-2: SaveAnswer Fire-and-Forget Silent Failures

**Description:**
Worker selects answer, JavaScript calls SaveAnswer asynchronously. If network fails, SaveAnswer never reaches server. Error is caught and silently ignored (line 233 StartExam.cshtml). Worker has no indication that answer wasn't saved. If HC CloseEarly, that answer is lost.

**Root Cause:**
- No retry logic
- No error callback to UI
- Worker is not informed of network issues
- Fire-and-forget pattern with silent catch

**Manifest As:**
- Answers saved during network hiccup are missing from PackageUserResponses
- Worker's score is lower than expected when CloseEarly runs
- Worker is unaware and cannot attempt to re-save
- Loss of data integrity without worker knowledge

**Fix Required:**
- Implement exponential backoff retry (3 attempts with 500ms, 1s, 2s delays)
- Show toast/banner if SaveAnswer fails after retries
- Optionally: fall back to storing answer in sessionStorage and retry on next page interaction

**Affected Methods:**
- SaveAnswer (line 1033)
- StartExam.cshtml fetch call (line 224-233)

---

### P0-3: Client-Side-Only Timer Enforcement

**Description:**
JavaScript timer counts down from DurationMinutes. Auto-submits when reaches 0. Server only validates elapsed time **at submit time**. If worker is offline or timer is manipulated (DevTools), submission can exceed allocated time.

**Root Cause:**
- Timer state not persisted to server
- No server-side heartbeat to reset/sync timer
- Validation only on submit, not during exam

**Manifest As:**
1. Security: Worker opens DevTools, sets `timeRemaining = 99999`, bypasses auto-submit
2. Reliability: Worker offline for 5 min, reconnects, timer shows wrong remaining time, submits with stale timer state
3. User confusion: Timer is inconsistent across page reloads

**Fix Required:**
- Send timer heartbeat to server every 30 seconds (along with CheckExamStatus)
- Server responds with server-calculated remaining time
- JavaScript timer syncs to server time on each heartbeat
- If time is up, JavaScript immediately shows banner and prevents submit (don't wait for CheckExamStatus poll)

**Affected Methods:**
- StartExam.cshtml timer (line 185-205)
- CheckExamStatus (line 1075) — already polled, just needs to return remaining time

---

### P0-4: Session State Machine Inconsistency

**Description:**
Session has multiple fields that can be independently modified (Status, StartedAt, CompletedAt, Score, ExamWindowCloseDate), creating inconsistent states. Example: Status="InProgress" but StartedAt=null (should never happen, but no constraint prevents it).

**Root Cause:**
- No state machine enforcement in code
- No database check constraints
- Fields updated independently in different methods

**Manifest As:**
1. CloseEarly logic assumes `StartedAt != null && CompletedAt == null && Score == null` identifies InProgress, but this assumption can be violated if code paths diverge
2. ResetAssessment sets StartedAt=null but doesn't re-validate Status=Open
3. ForceCloseAssessment sets Status=Completed without checking if already Completed

**Fix Required:**
- Implement state machine with explicit states: Open → InProgress → Completed | Abandoned
- Use database check constraint: `CHECK ((Status = 'Open' AND StartedAt IS NULL) OR (Status = 'InProgress' AND StartedAt IS NOT NULL) OR (Status IN ('Completed', 'Abandoned')))`
- Wrap all state transitions in a SaveSessionState method that validates transitions

**Affected Tables:**
- AssessmentSession model and database schema

---

## Important Gaps (P1 — Should Fix This Milestone)

### P1-1: No Progress Checkpoint Saves

**Current:** Answers are saved incrementally via SaveAnswer, but if SaveAnswer fails or network is weak, answers are only in form inputs (client-side). If page crashes, all in-flight answers are lost.

**Impact:** Worker loses work if browser crashes before final submit.

**Fix:** Auto-save all form inputs to sessionStorage every 10 seconds; on page reload, restore from sessionStorage and re-submit via SaveAnswer with retry logic.

---

### P1-2: Missing Session Resume Behavior

**Current:** If worker's browser crashes mid-exam, on reload they get a fresh timer (counts from full DurationMinutes). They have no way to know how much time has elapsed server-side.

**Impact:** Worker submits and gets "Time is up" error; loses all in-flight work.

**Fix:** On StartExam page reload (idempotent check line 2729), calculate server-side elapsed time and send it to JavaScript. JavaScript should restore timer to remaining seconds = DurationMinutes*60 - elapsed.

---

### P1-3: CheckExamStatus Doesn't Sync Timer

**Current:** CheckExamStatus (polled every 30s) returns { closed, redirectUrl } but not remaining time.

**Impact:** Client timer drifts from server reality; worker has false confidence about remaining time.

**Fix:** Return `{ closed, remainingSeconds, redirectUrl }` from CheckExamStatus. JavaScript syncs local timer to this value.

---

### P1-4: No Heartbeat to Detect Idle/Offline Workers

**Current:** If worker's browser is open but worker is AFK (away from keyboard), session remains InProgress forever. HC monitoring shows worker as active when they're not.

**Impact:** HC cannot distinguish between a worker actively taking the exam vs. one who left the browser open.

**Fix:** Implement heartbeat in CheckExamStatus. If HC hasn't received a heartbeat in 10 minutes, mark session as "idle" and timeout after 30 minutes of inactivity.

---

### P1-5: No Validation on DurationMinutes

**Current:** HC can set DurationMinutes to 0 or negative values. No server-side validation.

**Impact:** Timer auto-submits immediately or behaves unpredictably.

**Fix:** Add `[Range(1, 999)]` validation to DurationMinutes on AssessmentSession model.

---

### P1-6: Force Close Calculates Score as 0 Instead of From Answers

**Current:** ForceCloseAssessment sets Score=0 regardless of how many questions worker answered.

**Impact:** Worker is penalized even if they answered 50% correctly before being force-closed.

**Fix:** ForceCloseAssessment should calculate score from PackageUserResponses (same logic as CloseEarly) instead of hard-coding 0.

---

### P1-7: CloseEarly on Legacy Assessments Fails if No Sibling Questions Exist

**Current:** CloseEarly line 835-840 loads questions from a sibling session. If no sibling has questions yet (e.g., newly created assessment), legacyQuestions is empty, and all scores are 0%.

**Impact:** Workers on legacy assessments are scored incorrectly if questions haven't been loaded into any session's Questions collection.

**Fix:** Load legacy questions directly from AssessmentQuestion table, not from session.Questions navigation property. Or: validate that legacy assessment has at least one question before CloseEarly runs.

---

### P1-8: AllowAnswerReview Not Enforced on Results Page

**Current:** Results page displays PackageUserResponses without checking AllowAnswerReview flag.

**Impact:** If assessment is supposed to be review-restricted, worker can see their answers by loading Results page.

**Fix:** Results controller should check AllowAnswerReview and hide answer details if false.

---

### P1-9: Competency Updates Not Rolled Back on Reset/ForceClose

**Current:** ResetAssessment and ForceCloseAssessment don't modify UserCompetencyLevels. If worker previously passed and gained levels, then assessment is reset/force-closed, levels remain.

**Impact:** Competency record no longer reflects actual worker capability.

**Fix:** On ResetAssessment/ForceCloseAssessment, delete or revert UserCompetencyLevel records that were created from this assessment (check AssessmentSessionId).

---

### P1-10: No HC Warning Before CloseEarly on Mixed States

**Current:** HC clicks "Submit Assessment" without seeing breakdown of how many workers are in each state (Not Started, InProgress, Completed, Abandoned).

**Impact:** HC might accidentally close early when expecting only a few InProgress workers, but 8 are still Not Started.

**Fix:** CloseEarly confirmation modal should show counts: "3 InProgress workers will be scored, 2 Not Started will be locked, 3 already Completed, 1 Abandoned."

---

### P1-11: No Token Verification on Package Resume

**Current:** StartExam line 2704-2712 checks token requirement, but only if StartedAt==null (first entry). On reload, token is not re-verified.

**Impact:** If token is revoked between the first and second entry, worker can still re-enter on reload without re-verifying token.

**Fix:** Token re-verification on every StartExam call, or persistent token validation via session cookie.

---

### P1-12: Monitoring View Doesn't Show Last Activity Timestamp

**Current:** AssessmentMonitoringDetail shows Status (Completed, InProgress) but not "last heartbeat" or "last activity" timestamp.

**Impact:** HC cannot tell if InProgress worker is AFK or actually taking the exam.

**Fix:** Add LastActivityAt timestamp to AssessmentSession; update on CheckExamStatus calls. Display in monitoring view.

---

## Minor Pitfalls (P2 — Future)

### P2-1: Clock Skew Between Client and Server
If worker's system clock is significantly behind server, timer calculation can be off. Mitigate with UTC and client-side sync on heartbeat (P1-3 fix).

### P2-2: No Explicit Assessment Group ID
Assessment grouping relies on Title + Category + Schedule.Date string matching. If HC creates duplicate assessments on the same day with same title/category, behavior is undefined. Use an explicit AssessmentGroupId.

### P2-3: ExamWindowCloseDate Can Be Misconfigured
HC can set ExamWindowCloseDate before Schedule, or leave it null without realizing there's no cutoff. No validation warning. Low impact but UX improvement needed.

### P2-4: Package Path vs. Legacy Path Inconsistency
Two separate grading paths (AssessmentQuestion + legacy vs. AssessmentPackage) create code duplication and risk of divergent behavior. Consider unifying grading logic.

### P2-5: No "Answer Submitted" Confirmation Toast
SaveAnswer succeeds silently. No visual feedback to worker that answer was persisted. Worker has no confidence in save status.

### P2-6: Reshuffle Button Appears Disabled for Completed Workers
UI shows "disabled" button state, but button doesn't explain why. Tooltip should say "Cannot reshuffle completed assessments."

### P2-7: TempData Messages Not Shown for Async Operations
SaveAnswer and CheckExamStatus use JSON responses, not TempData. HC actions (Reset, Force Close) use TempData, but worker-side async actions don't. Inconsistent UX.

### P2-8: No Rate Limiting on SaveAnswer or CheckExamStatus
Worker's JavaScript polls CheckExamStatus every 30 seconds and calls SaveAnswer on every radio change. No rate limiting; could result in many requests if worker is spamming clicks. Not critical, but consider implementing endpoint rate limiting for v2.2.

---

## v2.1 Feature Impact

### How Auto-Save Addresses Critical Issues

**v2.1 adds:** Regular background saves every 10 seconds, with retry logic and visual feedback

**Helps with:**
- **P0-2 (SaveAnswer silent failures):** Retry logic + visible error banner let worker know if saves are failing
- **P1-1 (No progress checkpoints):** Regular saves ensure answers persist even if network drops
- **P1-2 (No session resume):** Auto-save + resume means worker doesn't lose answers on reload

**Still doesn't address:**
- P0-1 (Race condition): Race condition between auto-save and CloseEarly still exists; requires transaction isolation fix
- P0-3 (Client timer): Auto-save doesn't fix timer desync; requires heartbeat with timer sync

---

### How Session Resume Addresses Critical Issues

**v2.1 adds:** Restoring timer state and PageSessionState on reload

**Helps with:**
- **P0-3 (Client timer):** Server calculates elapsed time, returns to client, client syncs timer
- **P1-2 (No session resume):** Worker resumes exactly where they left off with correct remaining time
- **P1-4 (Idle detection):** LastActivityAt timestamp can be used to detect idle workers

**Still doesn't address:**
- P0-1 (Race condition): Requires transaction isolation fix
- P0-2 (SaveAnswer failures): Requires retry logic (orthogonal to session resume, but v2.1 auto-save helps)

---

### How Exam Invalidation Polling Addresses Critical Issues

**v2.1 adds:** Polling CloseEarly status + auto-redirect on close, improved timer sync

**Helps with:**
- **P0-3 (Client timer):** Heartbeat returns correct remaining time from server
- **P1-3 (CheckExamStatus doesn't sync timer):** Enhanced polling includes remainingSeconds

**Still doesn't address:**
- P0-1 (Race condition): Polling doesn't prevent concurrent writes
- P0-2 (SaveAnswer failures): Orthogonal; auto-save helps but polling doesn't

---

### How Live Progress Monitoring Addresses Critical Issues

**v2.1 adds:** Real-time progress dashboard showing per-worker status

**Helps with:**
- **P1-4 (Idle detection):** LastActivityAt timestamp displayed in monitoring
- **P1-10 (No HC warning):** Real-time counts of Not Started, InProgress, Completed shown

**Still doesn't address:**
- P0-1 (Race condition): Not a monitoring issue; requires database-level fix
- P0-2 (SaveAnswer failures): Not visible to HC monitoring (worker-side issue)

---

## Roadmap Recommendations for v2.1

### Phase 1: Concurrency & Timer Fixes (Week 1-2)
**Priority:** CRITICAL — blocks other work

1. **Add RowVersion concurrency check to AssessmentSession**
   - Add `[ConcurrencyToken] public byte[] RowVersion { get; set; }` to model
   - Add migration to DB schema
   - Wrap SubmitExam + CloseEarly in try-catch for `DbUpdateConcurrencyException`
   - If conflict, return 409 Conflict and ask user to retry

2. **Implement Timer Heartbeat in CheckExamStatus**
   - Calculate server elapsed time: `DateTime.UtcNow - session.StartedAt`
   - Calculate remaining: `DurationMinutes * 60 - elapsed`
   - Return `{ closed, remainingSeconds, redirectUrl }` in JSON
   - JavaScript syncs timer: `timeRemaining = data.remainingSeconds`

3. **Add DurationMinutes Validation**
   - `[Range(1, 999)]` on AssessmentSession model
   - Server-side validation on Create/Edit

### Phase 2: Resilience & Recovery (Week 2-3)

4. **Implement Auto-Save with Retry Logic**
   - Keep SaveAnswer but add exponential backoff retry (3x with 500ms, 1s, 2s)
   - Show toast on failure: "Jawaban gagal disimpan. Akan dicoba ulang..."
   - On success: quiet toast "Jawaban disimpan" (2 sec auto-dismiss)

5. **Session State Machine Validation**
   - Add database check constraint for valid state transitions
   - Add SaveSessionState method that validates before SaveChangesAsync

6. **CloseEarly Score Calculation for Legacy**
   - Load questions from AssessmentQuestion table, not session.Questions
   - Validate that questions exist before scoring

### Phase 3: Monitoring & UX (Week 3-4)

7. **Enhance CheckExamStatus Polling**
   - Add LastActivityAt tracking on heartbeat
   - Return `lastActivitySeconds` to help UI detect idle state

8. **Improve CloseEarly Confirmation Modal**
   - Query session counts before showing modal
   - Display: "3 InProgress, 2 Not Started, 1 Already Completed"
   - Show estimated impact on scores

9. **Force Close Score Calculation**
   - Calculate score from PackageUserResponses instead of hard-coding 0
   - OR: force close with "DNF" status instead of numeric score

10. **Competency Rollback on Reset/Force Close**
    - Delete UserCompetencyLevel records where AssessmentSessionId matches
    - Log action in audit trail

### Phase 4: Transparency & Diagnostics (Week 4+)

11. **Add Early-Close Indicator on Results Page**
    - If ExamWindowCloseDate is set and past, show banner: "This assessment was closed early by administrator"
    - Show actual close time

12. **Token Re-Verification**
    - Check token on every StartExam, not just first entry
    - Or: use session-bound token cookie

13. **Progress Monitoring Real-Time Dashboard**
    - Display LastActivityAt for each InProgress worker
    - Show SaveAnswer failure rate (?)
    - Highlight workers who haven't answered any questions after 5+ minutes

---

## Summary: Critical Path for v2.1

**Must fix before auto-save launch (Week 1-2):**
1. RowVersion concurrency control
2. Timer heartbeat with sync
3. CloseEarly score validation for legacy
4. DurationMinutes validation

**Should fix during auto-save rollout (Week 2-3):**
5. Auto-save retry logic
6. SaveAnswer error toast
7. Session state machine
8. Competency rollback on reset

**Nice to have (Week 3-4):**
9. CloseEarly confirmation modal with counts
10. Force Close score calculation
11. Early-close banner on Results
12. LastActivityAt monitoring

---

## Confidence Assessment

| Area | Level | Reason |
|------|-------|--------|
| **Concurrency bugs** | HIGH | Code inspection shows overlapping SaveChangesAsync calls; no concurrency control |
| **Timer enforcement** | HIGH | Timer is client-only; server validation only at submit |
| **SaveAnswer reliability** | HIGH | Fire-and-forget with silent catch explicitly in code |
| **Session state inconsistency** | MEDIUM | No explicit state machine; assumptions in CloseEarly logic could break |
| **CloseEarly legacy path** | MEDIUM | Depends on sibling questions being loaded; unclear if always happens |
| **Competency rollback** | MEDIUM | Not mentioned in reset/force close; assumed missing |
| **Monitoring transparency** | LOW | Missing features (LastActivityAt, counts) but no critical bugs |

---

## Gaps Requiring Phase-Specific Research

- **Phase 1 (Concurrency):** Verify EF Core RowVersion implementation and error handling in ASP.NET Core 8.0
- **Phase 2 (Timer Sync):** Verify CheckExamStatus polling interval (30s) vs. grace period (2 min) — is there a race between poll and submit?
- **Phase 3 (Monitoring):** Research real-time dashboard tech (SignalR vs. polling) — current polling at 30s may be too slow for HC to react to early closes
- **Phase 4 (Legacy Path):** Verify if AssessmentQuestion.Options are eagerly loaded or lazy-loaded in CloseEarly scoring

---

## Appendix: Code Locations

| Issue | File | Line |
|-------|------|------|
| Timer client-side | StartExam.cshtml | 185-205, 204 |
| SaveAnswer fire-and-forget | StartExam.cshtml | 224-233 |
| SaveAnswer implementation | CMPController.cs | 1033 |
| CheckExamStatus | CMPController.cs | 1075 |
| SubmitExam load | CMPController.cs | 3163 |
| SubmitExam SaveChangesAsync | CMPController.cs | 3302 |
| CloseEarly load | CMPController.cs | 791 |
| CloseEarly SaveChangesAsync | CMPController.cs | 1014 |
| StartExam idempotent check | CMPController.cs | 2729 |
| ExamWindowCloseDate check | CMPController.cs | 2715 |
| InProgress detection in CloseEarly | CMPController.cs | 852 |
| Legacy question load | CMPController.cs | 835-840 |
| ResetAssessment | CMPController.cs | 488 |
| ForceCloseAssessment | CMPController.cs | 563 |
| AssessmentSession model | Models/AssessmentSession.cs | 1-57 |
| UserPackageAssignment model | Models/UserPackageAssignment.cs | 1-87 |
| PackageUserResponse model | Models/PackageUserResponse.cs | 1-25 |

