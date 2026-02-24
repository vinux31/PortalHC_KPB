# Domain Pitfalls: Auto-Save, Session Resume, and Polling Features

**Project:** PortalHC Online Assessment System (Exam Engine v2)
**Domain:** Adding resilience features (auto-save, session resume, worker polling, HC live monitoring) to existing ASP.NET Core 8.0 MVC exam system
**Researched:** 2026-02-24
**System Context:**
- CMPController monolith (4963 lines)
- EF Core package-based answer storage (PackageUserResponse table)
- TempData token guards on StartExam
- Client-side timer only
- Phase 39 (Close Early) + Phase 40 (Training Records) complete; building Phases 41+ (resilience)

**Confidence:** HIGH (verified with official ASP.NET Core docs + direct codebase analysis)

---

## Critical Pitfalls

Mistakes that cause data corruption, security breaches, cascading failures, or require architectural rewrites.

---

### Pitfall 1: Race Condition on Concurrent Auto-Save AJAX Calls — Multiple Saves for Same Question

**What goes wrong:**
Worker rapidly clicks through exam options (rapid clicking, mouse double-click, or accidental key repeat). Five AJAX SaveAnswer calls fire in quick succession for the same question (e.g., Question #5) before any request completes. Each request includes `sessionId=101` and `questionId=42`, representing the same question-session pair.

Current SaveAnswer pattern (in CMPController or new SaveAnswerAPI):
```csharp
var existingResponse = await _context.PackageUserResponses
    .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == qId);
if (existingResponse != null) {
    existingResponse.PackageOptionId = selectedOptionId;
    _context.Update(existingResponse);
} else {
    _context.PackageUserResponses.Add(new PackageUserResponse { ... });
}
await _context.SaveChangesAsync();
```

Timeline:
- **T1 (14:55:00.000):** Request A: FirstOrDefaultAsync query → NO record found.
- **T2 (14:55:00.005):** Request B: FirstOrDefaultAsync query → NO record found (A's insert not yet committed).
- **T3 (14:55:00.010):** Request C: FirstOrDefaultAsync query → NO record found.
- **T4 (14:55:00.015):** Request A: SaveChangesAsync() → Inserts new PackageUserResponse (id=1001).
- **T5 (14:55:00.020):** Request B: SaveChangesAsync() → Inserts new PackageUserResponse (id=1002) — DUPLICATE!
- **T6 (14:55:00.025):** Request C: SaveChangesAsync() → Inserts new PackageUserResponse (id=1003) — DUPLICATE!

Result: Three rows with same sessionId=101, qId=42, different option IDs (or same ID, doesn't matter). Database has duplicates. No unique constraint → no error thrown.

**Why it happens:**
- `FirstOrDefaultAsync()` is a **read** operation; it's not atomic with the subsequent **write** (Add + SaveChangesAsync).
- Between the query at T1 and the insert at T4, another request at T2 executes the same query, seeing the same "no record" state.
- EF Core provides no built-in atomicity guarantee for check-then-insert pattern.
- No database-level unique constraint on (AssessmentSessionId, PackageQuestionId) to prevent duplicates.
- No optimistic concurrency token (RowVersion) on PackageUserResponse to detect unexpected concurrent writes.
- Client-side debounce is not implemented, allowing rapid AJAX calls.

**Consequences:**
1. **Data inconsistency:** PackageUserResponses table contains duplicate rows for same question.
2. **Scoring errors:** When SubmitExam iterates over PackageUserResponses to grade:
   ```csharp
   foreach (var q in packageQuestions) {
       var selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : null;
       var existingResponse = await _context.PackageUserResponses
           .FirstOrDefaultAsync(r => r.AssessmentSessionId == id && r.PackageQuestionId == q.Id);
       // If three duplicates exist, FirstOrDefaultAsync returns first one (arbitrary order)
       // Only one answer is graded; two duplicates are ignored
   }
   ```
   Grading uses arbitrary duplicate (first one by database order), not the "latest" answer. Unpredictable scoring.

3. **Progress count inflated:** HC's polling sees `COUNT(PackageUserResponses)` = 3 for one question, so Progress = "3/110 answered" instead of "1/110". HC monitoring shows worker answered 300% of questions (nonsense).

4. **Final submission anomaly:** Worker submits; SubmitExam creates final state. If PackageUserResponses rows for question #5 include duplicates, grading logic may pick wrong one. Worker expects score based on "last answer they gave," but gets score based on arbitrary duplicate.

5. **Database constraint violation on explicit upsert:** If migration later adds unique constraint on (sessionId, qId), historic data with duplicates cannot be cleaned up without special handling. Application breaks on insert.

6. **Answer review displays duplicates:** When worker reviews answers after submission, they see duplicate rows displayed (confusing, "Why is question 5 listed twice?").

**Prevention:**

1. **Client-side debounce (CRITICAL — Must implement):**

   Implement at least 300–500ms debounce on SaveAnswer AJAX calls. After user changes an answer, wait for a quiet period (no new changes for 300ms) before firing the request. Prevents rapid-fire duplicates at the source.

   ```javascript
   // In exam view (JavaScript)
   let saveTimeout;
   document.querySelectorAll('input[name^="option_"]').forEach(el => {
       el.addEventListener('change', function() {
           clearTimeout(saveTimeout);
           saveTimeout = setTimeout(() => {
               const qId = this.dataset.questionId;
               const optId = this.value;
               fetch('/CMP/SaveAnswer', {
                   method: 'POST',
                   body: JSON.stringify({ sessionId, questionId: qId, selectedOptionId: optId }),
                   headers: { 'Content-Type': 'application/json' }
               }).then(r => r.json()).then(d => console.log('Saved:', d));
           }, 300); // 300ms debounce
       });
   });
   ```

2. **Database-level unique constraint (CRITICAL — Must implement):**

   Add unique index on (AssessmentSessionId, PackageQuestionId) in migration:
   ```csharp
   // In migration (e.g., Phase 41 or earlier)
   migrationBuilder.CreateIndex(
       name: "IX_PackageUserResponses_SessionQuestion_Unique",
       table: "PackageUserResponses",
       columns: new[] { "AssessmentSessionId", "PackageQuestionId" },
       unique: true);
   ```

   This constraint enforces: Only one answer record per (session, question) pair. If a duplicate insert is attempted, database throws unique constraint violation.

3. **Server-side atomic upsert pattern (CRITICAL — Must implement):**

   Replace check-then-insert with atomic ExecuteUpdateAsync (EF Core 7+) or raw SQL UPSERT. ExecuteUpdateAsync is atomic at DB level:
   ```csharp
   // SaveAnswer endpoint (new or modified)
   public async Task<IActionResult> SaveAnswer(int sessionId, int questionId, int? selectedOptionId) {
       // Authorization check (existing)
       var assessment = await _context.AssessmentSessions.FindAsync(sessionId);
       var user = await _userManager.GetUserAsync(User);
       if (assessment.UserId != user.Id) return Forbid();
       if (assessment.Status != "InProgress") return BadRequest("Exam not in progress");

       // Atomic upsert: Update if exists, insert if not
       var updated = await _context.PackageUserResponses
           .Where(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId)
           .ExecuteUpdateAsync(s => s
               .SetProperty(r => r.PackageOptionId, selectedOptionId)
               .SetProperty(r => r.SubmittedAt, DateTime.UtcNow));

       if (updated == 0) {
           // No existing record, insert new one
           _context.PackageUserResponses.Add(new PackageUserResponse {
               AssessmentSessionId = sessionId,
               PackageQuestionId = questionId,
               PackageOptionId = selectedOptionId,
               SubmittedAt = DateTime.UtcNow
           });
           await _context.SaveChangesAsync();
       }

       return Ok(new { success = true, savedAt = DateTime.UtcNow });
   }
   ```

4. **Optimistic concurrency with RowVersion (HIGH — Should implement):**

   Add [Timestamp] property to PackageUserResponse. This auto-increments on every update, allowing EF Core to detect unexpected concurrent writes:
   ```csharp
   public class PackageUserResponse {
       public int Id { get; set; }
       public int AssessmentSessionId { get; set; }
       public int PackageQuestionId { get; set; }
       public int? PackageOptionId { get; set; }
       public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

       [Timestamp]
       public byte[] Version { get; set; } // SQL Server: rowversion type
   }
   ```

   On concurrent update, EF Core throws DbUpdateConcurrencyException, allowing graceful handling:
   ```csharp
   try {
       await _context.SaveChangesAsync();
   } catch (DbUpdateConcurrencyException ex) {
       // Concurrent edit detected; reload latest and retry
       await ex.Entries[0].ReloadAsync();
       // Retry logic or return 409 Conflict
       return Conflict(new { message = "Answer changed by another request; reloading." });
   }
   ```

**Detection:**
- **Database query:** Identify duplicates:
  ```sql
  SELECT AssessmentSessionId, PackageQuestionId, COUNT(*) cnt
  FROM PackageUserResponses
  GROUP BY AssessmentSessionId, PackageQuestionId
  HAVING COUNT(*) > 1
  ORDER BY cnt DESC;
  ```
  Any result indicates duplicates (should return no rows).

- **Application Insights / Logs:** Monitor SaveAnswer endpoint for database constraint violation errors or DbUpdateConcurrencyException.

- **Worker feedback:** "My answer appears twice in the review" or "Score doesn't match the answer I see."

- **HC monitoring:** Progress percentage > 100% or "45/110 answered" on exam with only 20 questions.

---

### Pitfall 2: Client-Side Timer Manipulation and Drift on Resume

**What goes wrong:**
Worker starts exam at 14:00 UTC, timer shows "60:00 remaining." After 45 minutes (14:45), worker's internet cuts out. Browser is still open; client timer continues decrementing locally (now "15:00 remaining" at 14:45 UTC). Worker reconnects at 14:55 UTC (10 real minutes later). They resume the exam. What timer is authoritative?

**Scenario A (Client Timer Used):**
- Client timer at 14:55 shows "5:00 remaining" (because countdown continued for 10 min offline, eating into the grace).
- Worker sees "only 5 minutes left" and panics, submits immediately.
- But server's actual elapsed time is 55 minutes (14:00 → 14:55), so only 5 minutes is correct... by accident.

**Scenario B (Client Timer is Stale):**
- HC creates exam with DurationMinutes=60, StartedAt=14:00.
- Worker's browser timer is purely client-side: `remainingTime = 60 * 60 - (now - startTime)`.
- At 14:45 (45 min elapsed), worker's browser shows 15 min remaining. ✓ Correct.
- Network fails. Browser goes to sleep (laptop hibernates). Wall-clock advances to 15:30 (real time).
- Worker wakes laptop, resumes. Browser's `startTime` JS variable is still 14:00 UTC.
- Browser recalculates: `remainingTime = 3600 - (15:30 UTC - 14:00 UTC) = 3600 - 5400 = -1800 sec = -30 minutes` ❌ NEGATIVE.
- Or: If browser's clock is wrong (set to 14:30 instead of 15:30), calculation gives extra time.

**Scenario C (Client Timer Manipulation):**
- Worker opens DevTools console after 45 minutes elapsed.
- Types: `window.examStartTime = Date.now() - 1800000;` (pretend exam started 30 minutes ago instead of 45).
- Timer recalculates: `remainingTime = 60*60 - (now - 1800000) = 3600 - 1800 = 1800 sec = 30 minutes remaining` ❌ INFLATED.
- Worker now has 30 "fake" minutes instead of real 15 minutes.
- Submits and tries to game the time.

**Scenario D (Time Enforcement Inconsistency):**
- SubmitExam has a 2-minute grace period: `if (elapsed > 62 min) reject`.
- SaveAnswer has no grace period: `if (elapsed > 60 min) reject`.
- Worker's SaveAnswer succeeds at 60:30 (within 61-min threshold if no grace), but SubmitExam at 60:30 is rejected (over 60-min strict).
- Or vice versa: inconsistency causes confusion.

**Why it happens:**
1. **Timer is purely client-side.** JavaScript: `remainingTime = DurationMinutes * 60 - (now - startTime)`. Server never validates this value.

2. **Client timer value can be passed to SubmitExam and honored.** If request body includes `{ elapsedTime: 3600 }`, server might trust it: `if (requestBody.elapsedTime > duration) reject`. Client controls the value → security hole.

3. **No server-side elapsed time stored on StartExam or during resume.** Only StartedAt is stored. Elapsed time is calculated fresh on SubmitExam: `DateTime.UtcNow - assessment.StartedAt.Value`. But this is recalculated at submission, not remembered.

4. **Resume doesn't track "time already consumed before disconnect."** If worker paused at 45 min, that 45 min is "spent." On resume, server should know 45 min is gone, leaving only 15 min. But if server only has StartedAt=14:00 and current time=15:30, it calculates 90 min elapsed (wrong; should be 45 + 15 = 60, but with only StartedAt it becomes 90).

5. **Browser's system clock can be manipulated.** Worker opens system settings, changes clock to future, browser's `Date.now()` returns fake time.

**Consequences:**
1. **Unfair assessment:** Worker gains extra time by manipulating timer or clock.
2. **Inflated score:** Worker solves more questions because they have artificial time.
3. **Security violation:** Time limits are not enforced fairly; easy to bypass.
4. **Audit failure:** Examiner cannot trust elapsed time (could be manipulated).
5. **Resume failure:** If server recalculates, it may reject resume as "time exceeded" even though real time allowed.
6. **Score dispute:** Worker says "I submitted in 59 minutes" (clock says so), HC says "You submitted at 90 minutes of elapsed time." No clear truth.

**Prevention:**

1. **Server-side elapsed time calculation (CRITICAL — Must implement):**

   SubmitExam must calculate elapsed time server-side, using DateTime.UtcNow and stored StartedAt:
   ```csharp
   // In SubmitExam (existing code at line ~3188, good practice already in place)
   if (assessment.StartedAt.HasValue) {
       var elapsed = DateTime.UtcNow - assessment.StartedAt.Value; // Server-authoritative
       int allowedMinutes = assessment.DurationMinutes + 2; // 2-minute grace for slow upload
       if (elapsed.TotalMinutes > allowedMinutes) {
           TempData["Error"] = "Exam time exceeded.";
           return RedirectToAction("StartExam", new { id });
       }
   }
   ```

   **Do NOT accept client-supplied elapsed time or remaining time in request body.** Remove any parameters like `{ remainingTime: 540 }` or `{ elapsedMinutes: 120 }` from SaveAnswer or SubmitExam endpoints.

2. **Client timer is UI-only (CRITICAL — Must implement):**

   JavaScript timer is purely for user feedback. Never send its value to server. Never use it for any decisions:
   ```javascript
   // ✅ CORRECT: Client timer for display only
   setInterval(() => {
       const elapsedSec = Math.floor((Date.now() - startTimeMs) / 1000);
       const remainingSec = Math.max(0, durationSec - elapsedSec);
       document.getElementById('timerDisplay').textContent = formatTime(remainingSec);
   }, 1000);

   // ❌ WRONG: Do NOT do this
   fetch('/CMP/SubmitExam', {
       method: 'POST',
       body: JSON.stringify({ ..., elapsedTime: elapsedSec }), // ← Never send timer value
   });
   ```

3. **Add server-side elapsed time tracking on pause/resume (HIGH — Should implement):**

   When worker pauses or disconnects, record how much time has been consumed. On resume, start from that baseline:
   ```csharp
   public class AssessmentSession {
       ...
       public DateTime? StartedAt { get; set; }

       // New fields for resume support
       public DateTime? PausedAt { get; set; }
       public double ElapsedMinutesBeforePause { get; set; } = 0; // Time consumed before last pause
   }

   // On pause (new endpoint or action)
   public async Task<IActionResult> PauseExam(int id) {
       var assessment = await _context.AssessmentSessions.FindAsync(id);
       if (assessment.StartedAt.HasValue && assessment.PausedAt == null) {
           var elapsedSincePause = (DateTime.UtcNow - assessment.StartedAt.Value).TotalMinutes;
           assessment.ElapsedMinutesBeforePause = elapsedSincePause;
           assessment.PausedAt = DateTime.UtcNow;
           await _context.SaveChangesAsync();
       }
       return Ok();
   }

   // On resume (in StartExam)
   public async Task<IActionResult> StartExam(int id) {
       var assessment = await _context.AssessmentSessions.FindAsync(id);
       if (assessment.PausedAt.HasValue) {
           // Resume from paused state; restart the timer
           assessment.StartedAt = DateTime.UtcNow; // Reset start time to now
           assessment.PausedAt = null;
           // Consumed time is now locked at ElapsedMinutesBeforePause
           // Remaining = DurationMinutes - ElapsedMinutesBeforePause
           await _context.SaveChangesAsync();
       }
       return View(...);
   }

   // On SubmitExam, calculate total elapsed
   var totalElapsed = assessment.ElapsedMinutesBeforePause + (DateTime.UtcNow - assessment.StartedAt.Value).TotalMinutes;
   if (totalElapsed > assessment.DurationMinutes + 2) {
       TempData["Error"] = "Exam time exceeded.";
       return RedirectToAction("StartExam", new { id });
   }
   ```

4. **Use UTC consistently (CRITICAL — Already implemented well, maintain it):**

   All time comparisons use `DateTime.UtcNow` (not local time). Already in place in codebase. ✓

5. **Validate client timer on resume, but don't trust it (MEDIUM — Nice-to-have):**

   If client sends its timer value for logging, validate it's reasonable (not negative, not wildly different from server):
   ```csharp
   // In SaveAnswer or CheckExamStatus
   var clientRemainingMinutes = /* from request */;
   var serverRemainingMinutes = assessment.DurationMinutes -
       (DateTime.UtcNow - assessment.StartedAt.Value).TotalMinutes;

   if (Math.Abs(clientRemainingMinutes - serverRemainingMinutes) > 5) {
       // Log discrepancy, but do NOT reject; server time is authoritative
       _auditLog.Log($"Timer drift for session {id}: client={clientRemainingMinutes}, server={serverRemainingMinutes}");
   }
   ```

**Detection:**
- **Submission audit:** Query sessions where (CompletedAt - StartedAt) differs significantly from claimed elapsed time.
- **Comparative check:** `SELECT id, DATEDIFF(MINUTE, StartedAt, CompletedAt) as serverElapsed, ElapsedMinutesReported FROM AssessmentSessions WHERE ABS(serverElapsed - ElapsedMinutesReported) > 5` (if client timer is logged).
- **Worker report:** "I only answered for 30 minutes but system says 60 minutes elapsed."
- **Anomaly:** Worker's clock was set to future time, system grants inflated time.

---

### Pitfall 3: Double-Submit Race Condition — Worker's SubmitExam vs HC's Close Early

**What goes wrong:**
HC clicks "Close Early" button at 14:55:30.000 UTC, intending to close worker's exam and grade incomplete answers. Simultaneously, the worker is submitting their completed answers via SubmitExam POST at 14:55:29.990 UTC (the requests are on different network paths, so there's 10ms of overlap). Both requests reach the database within milliseconds:

**Timeline:**
1. **14:55:29.990:** SubmitExam request begins: Reads assessment session (Status=InProgress).
2. **14:55:30.000:** CloseEarlySession request begins: Reads assessment session (Status=InProgress, same read).
3. **14:55:30.005:** SubmitExam: Grades all PackageUserResponses, sets Score=85, Status=Completed, IsPassed=true, CompletedAt=14:55:30.005.
4. **14:55:30.010:** CloseEarlySession: Grades incomplete PackageUserResponses, sets Score=45 (fewer answered), Status=Completed, IsPassed=false, CompletedAt=14:55:30.010.
5. Both SaveChangesAsync() succeed (no conflict detection).
6. **Final state:** Score=45 (CloseEarlySession's value, the later write), Status=Completed, CompletedAt=14:55:30.010.

**Result:**
- Worker's complete submission (Score=85) is overwritten by HC's incomplete grading (Score=45).
- Worker's IsPassed=true is overwritten to false.
- Worker's CompletedAt is overwritten.
- Competency levels based on Score=45 are granted (or not), not based on actual Score=85.
- Audit trail shows two completion timestamps; confusion about which is real.

**Why it happens:**
1. **No optimistic concurrency token on AssessmentSession.** Status field is not protected by RowVersion or [Timestamp]. Both requests see Status=InProgress, both proceed to update Status=Completed independently.

2. **No explicit status transition guard.** SubmitExam and CloseEarlySession both check `if (Status == "Completed") return Forbid()` to prevent re-submission, but they don't prevent each other. There's no mutual exclusion.

3. **Default transaction isolation level (Read Committed in SQL Server) allows both transactions to see same pre-update state.** Transaction A reads Status=InProgress. Transaction B reads Status=InProgress (before A's update commits). Both proceed. A commits first, B's update overwrites A's data.

4. **No database-level uniqueness constraint on Status or completion state.** Multiple completion records can be created for same session.

5. **Score and final state updates are not atomic.** SubmitExam updates Score, Status, IsPassed, CompletedAt separately. CloseEarlySession does the same. Last write wins; no merge logic.

**Consequences:**
1. **Score corruption:** Worker's real submission is ignored. HC's force-close grading overwrites it. Worker disputes: "I submitted my answers; my score should be based on them, not HC's incomplete grades."

2. **Fairness violation:** If worker was on track to pass, but HC closes and overwrites with incomplete grading, they fail unjustly.

3. **Competency misallocation:** If score determines competency level grant (e.g., "score ≥ 70% → grant Level 3"), the wrong level is assigned based on corrupted score.

4. **Audit trail confusion:** Two CompletedAt timestamps exist (or logs show both SubmitExam and CloseEarlySession executed). Unclear which took precedence.

5. **User experience:** Worker sees one score on Results page; HC sees another (if UI is cached differently). Inconsistency.

**Prevention:**

1. **Add optimistic concurrency token on AssessmentSession (CRITICAL — Must implement):**

   Add [Timestamp] property to auto-increment on every update:
   ```csharp
   public class AssessmentSession {
       public int Id { get; set; }
       ...
       [Timestamp]
       public byte[] Version { get; set; } // SQL Server: rowversion type
   }
   ```

   Now, both SubmitExam and CloseEarlySession will attempt to update with old Version value. The second update will fail (Version mismatch), throwing DbUpdateConcurrencyException. Prevents silent overwrites.

2. **Check Status before allowing SubmitExam (CRITICAL — Already exists, maintain it):**

   In SubmitExam (lines ~3179-3183), check:
   ```csharp
   if (assessment.Status == "Completed") {
       TempData["Error"] = "Assessment already completed.";
       return RedirectToAction("Assessment");
   }
   ```

   This prevents re-submission. But it doesn't prevent concurrent SubmitExam + CloseEarlySession. You need the RowVersion check.

3. **Use explicit transaction with Serializable isolation (HIGH — Should implement):**

   For critical status transitions, wrap in a serializable transaction (highest isolation level). Prevents dirty reads and ensures one transaction completes fully before another reads:
   ```csharp
   using (var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)) {
       try {
           var assessment = await _context.AssessmentSessions.FindAsync(id);

           if (assessment.Status != "InProgress") {
               await transaction.RollbackAsync();
               return BadRequest("Session not in progress");
           }

           // Perform all grading and status updates
           // ... grade answers, calculate score ...
           assessment.Status = "Completed";
           assessment.Score = finalScore;
           assessment.IsPassed = finalScore >= assessment.PassPercentage;
           assessment.CompletedAt = DateTime.UtcNow;

           await _context.SaveChangesAsync();
           await transaction.CommitAsync();

           return RedirectToAction("Results", new { id });
       } catch (DbUpdateException ex) {
           await transaction.RollbackAsync();
           TempData["Error"] = "Exam status changed during submission. Please try again.";
           return RedirectToAction("StartExam", new { id });
       }
   }
   ```

   **Note:** Serializable can cause deadlocks under high concurrency. Use with caution; test thoroughly.

4. **Idempotent completion check (MEDIUM — Nice-to-have):**

   Once Status is set to Completed, any subsequent attempt to update score/status is a no-op or returns success without changing data:
   ```csharp
   if (assessment.Status == "Completed") {
       // Already completed; return success without re-grading
       return RedirectToAction("Results", new { id });
   }
   ```

5. **Graceful conflict handling (MEDIUM — Should implement):**

   When DbUpdateConcurrencyException occurs, inform user and suggest refresh:
   ```csharp
   try {
       await _context.SaveChangesAsync();
   } catch (DbUpdateConcurrencyException ex) {
       TempData["Error"] = "Another admin action completed this exam simultaneously. Refreshing...";
       return RedirectToAction("StartExam", new { id });
   }
   ```

**Detection:**
- **Database audit:** Query AssessmentSessions where CompletedAt has been updated multiple times:
  ```sql
  SELECT id, Score, IsPassed, CompletedAt, UpdatedAt
  FROM AssessmentSessions
  WHERE Status = 'Completed' AND UpdatedAt > CompletedAt
  ORDER BY UpdatedAt DESC;
  ```
  Multiple updates after Completed indicate potential conflict.

- **Logs:** Filter for SubmitExam + CloseEarlySession for same sessionId within 1 second: Indicates race condition.

- **Worker complaint:** "I submitted but my score is wrong. HC closed my exam with incomplete answers."

- **Monitoring:** Track DbUpdateConcurrencyException errors. Non-zero count indicates conflicts.

---

### Pitfall 4: TempData Token Guard Consumed by Polling — Prevents Session Validation

**What goes wrong:**
HC creates token-required assessment: `IsTokenRequired=true`, `AccessToken="ABC123"`. Worker goes to Assessment lobby, enters token "ABC123", which is verified:
```csharp
// In Assessment action (lobby page)
if (tokenInput == "ABC123") {
    TempData[$"TokenVerified_{id}"] = true; // Store verification flag
    return RedirectToAction("StartExam", new { id });
}
```

StartExam checks this TempData:
```csharp
// In StartExam (line ~2706)
if (assessment.IsTokenRequired && assessment.UserId == user.Id && assessment.StartedAt == null) {
    var tokenVerified = TempData[$"TokenVerified_{id}"]; // ← READ (marked for deletion)
    if (tokenVerified == null) {
        TempData["Error"] = "Token required.";
        return RedirectToAction("Assessment");
    }
}
```

**Problem:** When TempData is READ, it's marked for automatic deletion at the end of that request (ASP.NET Core default behavior). Now, 10 seconds later, a polling endpoint CheckExamStatus is called (to check if HC closed the exam):

```csharp
// In CheckExamStatus (new polling endpoint)
public async Task<IActionResult> CheckExamStatus(int sessionId) {
    var assessment = await _context.AssessmentSessions.FindAsync(sessionId);
    // Maybe polling also re-checks token (BUG):
    var tokenVerified = TempData.Peek($"TokenVerified_{id}"); // ← Already deleted!
    if (tokenVerified == null) {
        return Ok(new { valid = false }); // ← Thinks exam is invalid
    }
    return Ok(new { valid = assessment.Status != "Closed" });
}
```

Or, worse, if polling doesn't re-check but the worker's exam page calls a new action that checks TempData again (e.g., GetExamStatus to load page state):

**Timeline:**
1. **14:50:00:** Worker enters token in Assessment lobby → TempData[$"TokenVerified_101"] = true.
2. **14:50:05:** Worker clicks "Start Exam" → StartExam reads TempData → TempData is consumed (marked for deletion).
3. **14:50:06:** Request completes; TempData is deleted (persisted, not available in next request).
4. **14:50:15:** Worker's page polls every 10 seconds to check "is exam still open?" → Calls CheckExamStatus.
5. **14:50:15:** Inside CheckExamStatus, code tries to re-read TempData[$"TokenVerified_101"] → Returns null (already deleted).
6. **14:50:15:** Code thinks "token not verified" and incorrectly marks session as invalid.
7. **14:50:15:** Polling returns `{ valid: false }` → Worker's exam page shows "Session invalid, disconnected."
8. **14:50:16:** Worker is confused, exam is interrupted, session incorrectly marked as invalid.

**Alternative scenario (even worse): TempData shared across sessions**

If TempData is session-based and not properly isolated:
- Worker A verifies token for assessment 101.
- Worker B (different user, same session ID due to bug) calls polling for assessment 102.
- Worker B's polling accidentally reads Worker A's TempData token.
- Worker B gains unauthorized access.

**Why it happens:**
1. **TempData is consumed on first read.** ASP.NET Core MVC design: TempData stores one-time data. When you read a TempData value, it's marked for deletion at the end of the request. On the next request, it's gone.

2. **Code assumes TempData persists across multiple requests.** If multiple actions need to check `TokenVerified`, the second and subsequent reads fail.

3. **Token verification is a one-time check, but polling may need session status multiple times.** Original design assumes "token check once on entry, then done." Polling introduces repeated checks.

4. **TempData isn't the right place for persistent session validation.** TempData is temporary state for **one-time flash messages** (e.g., "Saved successfully!"). It's not for persistent validation flags that need to survive across multiple requests.

5. **Polling endpoint doesn't know not to re-check token.** If it's a generic "GetExamStatus" endpoint, it might check all security conditions, including token, not knowing token is already consumed.

**Consequences:**
1. **Session interruption:** Worker's polling detects "invalid session" (false positive) and breaks exam UX.

2. **Resume failure:** Worker reconnects after network outage. StartExam checks TempData token again (now consumed), finds null, refuses to resume. "Session invalid, cannot resume."

3. **Security bypass:** If TempData is shared across sessions or users (due to session state bugs), unauthorized workers might read another worker's token flag.

4. **Audit trail lost:** TempData consumption is not logged. No record of when token was verified.

5. **Feature doesn't work:** Polling + token-required exams = broken combination. Tests might not catch this (test on same request, not across multiple requests).

**Prevention:**

1. **Never store validation state in TempData (CRITICAL — Must fix):**

   TempData is for one-time messages, not persistent validation. Move token verification to persistent storage: database or a long-lived session variable.

2. **Store token verification in database (CRITICAL — Must implement):**

   Add field to AssessmentSession:
   ```csharp
   public class AssessmentSession {
       ...
       public bool TokenVerified { get; set; } = false;
       public DateTime? TokenVerifiedAt { get; set; }
   }
   ```

   On token verification:
   ```csharp
   // In Assessment action (lobby)
   if (tokenInput == "ABC123") {
       var assessment = await _context.AssessmentSessions.FindAsync(id);
       assessment.TokenVerified = true;
       assessment.TokenVerifiedAt = DateTime.UtcNow;
       await _context.SaveChangesAsync();
       return RedirectToAction("StartExam", new { id });
   }
   ```

   On StartExam (and any polling endpoint):
   ```csharp
   if (assessment.IsTokenRequired && !assessment.TokenVerified) {
       return RedirectToAction("Assessment"); // Token not verified
   }
   ```

   This is persistent, survives across requests, and doesn't depend on TempData consumption.

3. **Use Keep() or Peek() if TempData is unavoidable (MEDIUM — Fallback only):**

   If you must use TempData, preserve it across multiple reads using Keep():
   ```csharp
   var tokenVerified = TempData[$"TokenVerified_{id}"];
   if (tokenVerified != null) {
       TempData.Keep($"TokenVerified_{id}"); // Preserve for next request
   }
   ```

   Or use Peek() to read without consuming:
   ```csharp
   var tokenVerified = TempData.Peek($"TokenVerified_{id}"); // Doesn't mark for deletion
   ```

   But this is a band-aid. Better to use database.

4. **Isolate token verification from polling (MEDIUM — Design consideration):**

   Token is verified once on entry (StartExam). Polling should NOT re-check token. Polling only checks session validity:
   ```csharp
   // ✅ Polling endpoint: NO token re-check
   public async Task<IActionResult> CheckExamStatus(int sessionId) {
       var assessment = await _context.AssessmentSessions.FindAsync(sessionId);
       var user = await _userManager.GetUserAsync(User);

       // Ownership check (security)
       if (assessment.UserId != user.Id && !User.IsInRole("Admin")) {
           return Forbid();
       }

       // NO token check; token was checked once on StartExam entry
       return Ok(new { valid = assessment.Status == "InProgress" });
   }
   ```

5. **Add session state isolation (MEDIUM — Best practice):**

   Ensure each user's session state is isolated. TempData should be per-user, per-session. This is usually automatic in ASP.NET Core (session cookies are user-bound), but verify in session middleware configuration.

**Detection:**
- **Worker report:** "I verified the token, but after a few seconds polling says 'session invalid.'"

- **Logs:** Multiple reads of same TempData key. If Peek() is not used, second read returns null.
  ```
  14:50:05 TempData.TryGetValue("TokenVerified_101") → true
  14:50:15 TempData.TryGetValue("TokenVerified_101") → null (consumed)
  ```

- **Test case:** Write test that verifies token, calls StartExam, then calls polling endpoint 3x. Polling should succeed all 3 times.

---

### Pitfall 5: Polling Storm — Database Load Explosion from 20-100 Concurrent Workers

**What goes wrong:**
20 workers in active exam sessions, each polling CheckExamStatus every 10 seconds to detect if HC closes the exam. That's:
- 20 workers × 1 poll per 10 seconds = 2 requests/second
- Scale to 100 workers = 10 requests/second
- Each poll hits the database: `SELECT Id, Status FROM AssessmentSessions WHERE Id = @id`
- With 100 workers, 10 requests/second × 50ms per query = 500ms of database load per second = **50% CPU utilization**, leaving only 50% for other operations (SubmitExam, SaveAnswer, HC monitoring).

**At scale (500 workers):**
- 500 workers × 1 poll per 10 seconds = 50 requests/second
- 50 requests/sec × 50ms = 2500ms = **250% of database capacity** (if DB can handle 20 req/sec).
- Result: Connection pool exhaustion, query timeouts, cascading failures.

**Cascade failure scenario:**
1. 500 workers polling simultaneously (at 10s mark, thundering herd).
2. 50 concurrent database connections open.
3. Connection pool maxsize = 100 (default for SQL Server via EF Core).
4. All 100 connections occupied by polling queries.
5. New SubmitExam request needs connection → waits for free connection → times out after 30 seconds.
6. Worker's submission times out, returns 504 Gateway Timeout.
7. Worker panics, hits reload, creating more requests.
8. Cascading failure: system becomes unresponsive.

**Why it happens:**
1. **No caching of exam session state.** Every poll goes to database, even if session status hasn't changed (which is 99% of the time).

2. **Polling interval is fixed (10s), not adaptive.** All workers who started exam at roughly the same time poll at the same time → thundering herd effect.

3. **Session state is rarely changed (HC closes exam), but polled frequently.** Hundreds of reads per change. Wastes resources.

4. **No early exit or conditional polling.** Client doesn't check if status has changed before next poll. Blindly polls every interval.

5. **Database is not optimized for read-heavy polling.** No read replicas, no caching layer (Redis), no ETags.

**Consequences:**
1. **Database CPU exhaustion:** Polling queries occupy all connections, starving other operations.

2. **SubmitExam timeout:** Workers can't submit exams because no DB connection is available.

3. **HC monitoring broken:** HC's live progress polling (answered count per session) also queues behind exam status polls, slowing down monitoring.

4. **System unavailability:** At scale, the entire system becomes unresponsive.

5. **Wasted bandwidth and latency:** Polling transfers full AssessmentSession record (all fields) when only Status is needed.

**Prevention:**

1. **Add in-memory cache with TTL (CRITICAL — Must implement):**

   Cache exam session state for 5 seconds. Polling hits memory instead of database:
   ```csharp
   private readonly IMemoryCache _cache;

   public async Task<IActionResult> CheckExamStatus(int sessionId) {
       var cacheKey = $"exam_status_{sessionId}";

       if (!_cache.TryGetValue(cacheKey, out ExamStatus status)) {
           // Cache miss; query database
           var assessment = await _context.AssessmentSessions
               .AsNoTracking() // Faster for read-only
               .Select(a => new ExamStatus { Id = a.Id, Status = a.Status })
               .FirstOrDefaultAsync(a => a.Id == sessionId);

           if (assessment != null) {
               status = new ExamStatus { Id = assessment.Id, Status = assessment.Status };
               _cache.Set(cacheKey, status, TimeSpan.FromSeconds(5)); // Cache for 5 seconds
           }
       }

       var user = await _userManager.GetUserAsync(User);
       var assessment = await _context.AssessmentSessions.FindAsync(sessionId);
       if (assessment.UserId != user.Id && !User.IsInRole("Admin")) {
           return Forbid();
       }

       return Ok(new { valid = status?.Status != "Closed" });
   }
   ```

   **Impact:** 20 polls over 10 seconds = only 2 database queries (at 5s mark). 90% reduction in DB load.

2. **Invalidate cache on state change (CRITICAL — Must implement):**

   When HC closes exam (CloseEarlySession), clear the cache:
   ```csharp
   public async Task<IActionResult> CloseEarlySession(int id) {
       // ... perform grading ...
       assessment.Status = "Closed";
       await _context.SaveChangesAsync();

       // Invalidate cache so next poll sees updated status
       _cache.Remove($"exam_status_{id}");

       return RedirectToAction("...");
   }
   ```

   Now, polling immediately after HC closes will hit DB (cache miss), see Closed status, and inform worker.

3. **Increase polling interval on client (MEDIUM — Can implement):**

   Default polling every 30 seconds instead of 10 seconds. Reduces load by 3x:
   ```javascript
   const POLLING_INTERVAL_MS = 30000; // 30 seconds instead of 10
   setInterval(() => {
       fetch('/CMP/CheckExamStatus?sessionId=' + sessionId)
           .then(r => r.json())
           .then(data => {
               if (!data.valid) {
                   alert('Exam closed by administrator.');
                   window.location.href = '/CMP/Results?id=' + sessionId;
               }
           });
   }, POLLING_INTERVAL_MS);
   ```

4. **Exponential backoff polling (MEDIUM — Nice-to-have):**

   Start polling every 10 seconds; if no change for 5 polls, increase to 30s; if no change for 5 more, increase to 60s. Reset to 10s if exam closes:
   ```javascript
   let pollingInterval = 10000; // Start at 10s
   let noChangeCount = 0;

   setInterval(() => {
       fetch('/CMP/CheckExamStatus?sessionId=' + sessionId)
           .then(r => r.json())
           .then(data => {
               if (!data.valid) {
                   // Exam closed; go to results
                   window.location.href = '/CMP/Results?id=' + sessionId;
               } else {
                   noChangeCount++;
                   if (noChangeCount > 5) {
                       pollingInterval = Math.min(pollingInterval * 1.5, 120000); // Max 2 min
                   }
               }
           });
   }, pollingInterval);
   ```

5. **Optimize polling query (MEDIUM — Should implement):**

   Select only needed columns, not entire AssessmentSession:
   ```csharp
   var status = await _context.AssessmentSessions
       .Where(a => a.Id == sessionId)
       .AsNoTracking()
       .Select(a => new { a.Status, a.UpdatedAt })
       .FirstOrDefaultAsync();
   ```

   Reduces data transfer and memory usage per query.

6. **Implement WebSocket or SignalR for push (OPTIONAL — Ideal long-term):**

   Instead of polling, use bidirectional WebSocket. Server pushes "Exam closed" notification to all workers. Eliminates periodic requests entirely. Complex to implement but eliminates polling load.

   ```csharp
   // In CloseEarlySession
   await _hubContext.Clients.User(/* worker ID */).SendAsync("ExamClosed", id);
   ```

**Detection:**
- **Database monitoring:** Query performance drops, connection pool near max capacity during exams.
  ```sql
  SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE session_id > 50; -- Check active connections
  ```

- **Application Insights:** CheckExamStatus endpoint shows high latency (>100ms) during peak times.

- **Worker complaint:** "Submission timed out, exam closed while submitting."

- **Load test:** Simulate 100 concurrent exam workers; observe DB load.

---

### Pitfall 6: PackageUserResponse Upsert Conflict — Concurrent Inserts via FirstOrDefault Check-Then-Insert

**What goes wrong:**
SaveAnswer uses the check-then-insert pattern (similar to Pitfall #1, but focused on EF Core transaction semantics):

```csharp
var existingResponse = await _context.PackageUserResponses
    .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == qId);
if (existingResponse != null) {
    existingResponse.PackageOptionId = selectedOptionId;
    _context.Update(existingResponse);
} else {
    _context.PackageUserResponses.Add(new PackageUserResponse { ... });
}
await _context.SaveChangesAsync();
```

**Scenario:** Two concurrent requests (A and B) with identical (sessionId=101, qId=42) arrive within milliseconds:

**Request A timeline:**
1. FirstOrDefaultAsync() query → DB returns no row (none exists yet).
2. A continues; prepares to Insert.

**Request B timeline (parallel, before A's SaveChangesAsync):**
1. FirstOrDefaultAsync() query → DB returns no row (A's insert not yet committed).
2. B continues; prepares to Insert.

**Commit phase:**
3. A: SaveChangesAsync() → INSERT new PackageUserResponse (succeeds, row id=1001 created).
4. B: SaveChangesAsync() → INSERT new PackageUserResponse (succeeds, row id=1002 created, **duplicate**).

**Result:** Two rows with same (sessionId, qId). If no unique constraint exists, both rows persist silently. Grading logic picks arbitrary one (first by ID order), other is ignored.

**Why it happens:**
(Same as Pitfall #1, but from different angle)
- FirstOrDefaultAsync is not atomic.
- EF Core does not auto-deduplicate on concurrent inserts.
- Unique constraint may not exist on (sessionId, qId).

**Consequences:**
(Same as Pitfall #1)
- Duplicate answers in database.
- Inconsistent grading.
- Progress count inflated.

**Prevention:**
(Same as Pitfall #1 — atomic upsert + unique constraint)

---

## Moderate Pitfalls

### Pitfall 7: Resume Page Calculation Breaks If Question Count Changes

**What goes wrong:**
Worker answers questions 1–20, gets interrupted at question 5. LastSavedPage or LastSavedQuestionId is stored. Worker disconnects. Meanwhile, HC imports 10 more questions into the same package. Total question count goes from 20 → 30. Worker resumes. If resume logic uses page number (e.g., "go to page 5"), but page size hasn't changed, they now see questions 21–25 instead of 6–10 (the questions that were originally on page 5).

Or: If resume uses question ID (stored as last answered question ID), it works fine because ID is immutable. But if resume uses array index or position, it breaks.

**Why it happens:**
- Question assignment (ShuffledQuestionIds) is immutable per worker. Good.
- But total question count is not frozen. If HC adds questions between start and resume, count changes.
- Resume logic may assume count is stable.

**Consequences:**
- Worker sees wrong question on resume.
- Pagination breaks (page 5 shows different questions).
- Score calculation includes new questions worker didn't answer (e.g., 20 answered out of 30 = 67%, instead of 20/20 = 100%).

**Prevention:**

1. **Freeze question count at exam start (SHOULD implement):**

   ```csharp
   public class UserPackageAssignment {
       ...
       public int QuestionCountAtStart { get; set; } // Frozen count
   }

   // In StartExam
   assignment = new UserPackageAssignment {
       AssessmentSessionId = id,
       AssessmentPackageId = selectedPackage.Id,
       UserId = user.Id,
       ShuffledQuestionIds = JsonSerializer.Serialize(questionIds),
       ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionOrderDict),
       QuestionCountAtStart = selectedPackage.Questions.Count // ← Freeze here
   };
   ```

   On resume, verify count hasn't changed:
   ```csharp
   if (assignedPackage.Questions.Count != assignment.QuestionCountAtStart) {
       return BadRequest("Exam content changed. Contact HC.");
   }
   ```

2. **Prevent mid-exam package modifications (SHOULD implement):**

   Prevent HC from adding/removing questions while exam is InProgress:
   ```csharp
   var activeSessions = await _context.AssessmentSessions
       .Where(a => a.AssessmentPackageId == packageId && a.Status == "InProgress")
       .ToListAsync();
   if (activeSessions.Any()) {
       return BadRequest("Cannot modify package while exams are active.");
   }
   ```

---

### Pitfall 8: Answer Count Progress Tracking is Misleading

**What goes wrong:**
SaveAnswer updates Progress whenever an answer is saved:
```csharp
assessment.Progress = (answeredCount / totalQuestions) * 100;
```

But answeredCount is `COUNT(PackageUserResponses)`, which includes ALL records, even if they're null or overwritten. Worker saves answer #1, clears it (select "No answer"), then saves answer #2. Progress counts all 3 rows (or 2, depending on logic), but only answer #2 is a valid final answer.

HC's monitoring shows "60% answered," but actual submission shows only 40% (many saves were cleared or overwritten).

**Why it happens:**
- Progress counts SaveAnswer saves, not final non-null answers.
- No distinction between "attempted" (saved) and "answered" (non-null final).

**Consequences:**
- HC's monitoring is inaccurate.
- Progress bar is misleading.
- Competency levels based on Progress threshold are assigned incorrectly.

**Prevention:**

1. **Count only non-null answers for Progress (SHOULD implement):**

   ```csharp
   var answeredCount = await _context.PackageUserResponses
       .Where(r => r.AssessmentSessionId == id && r.PackageOptionId != null)
       .CountAsync();
   assessment.Progress = (answeredCount * 100) / totalQuestions;
   ```

2. **Recalculate Progress on SubmitExam (SHOULD implement):**

   Don't trust saved Progress. Recalculate from actual responses:
   ```csharp
   var finalAnsweredCount = packageQuestions
       .Where(q => answers.ContainsKey(q.Id) && answers[q.Id].HasValue)
       .Count();
   assessment.Progress = (finalAnsweredCount * 100) / packageQuestions.Count;
   ```

---

## Minor Pitfalls

### Pitfall 9: SaveAnswer Response Doesn't Return Updated State

Worker clicks option, SaveAnswer saves, returns `{ success: true }`. But client doesn't know if save actually persisted. If network glitch returns 500, client might show "saved" (bug in error handling).

**Prevention:** Return full state (answered count, timestamp, confirmation). Validate HTTP response status in client.

### Pitfall 10: Polling Endpoint Exposes Session State to Wrong User

CheckExamStatus doesn't check authorization. Malicious worker calls `CheckExamStatus(sessionId=999)` for another worker's session and learns their exam status.

**Prevention:** Always authorize (owner or HC/Admin) before returning session data.

---

## Phase-Specific Warnings and Mitigation

| Phase | Feature | Likely Pitfalls | Mitigation | Confidence |
|-------|---------|---|---|---|
| **Phase 41 (SaveAnswer/Auto-Save)** | Auto-save AJAX | Pitfall #1 (concurrent inserts), #6 (upsert conflict) | Client debounce (300ms), atomic ExecuteUpdateAsync, unique constraint | HIGH |
| **Phase 41** | SaveAnswer response | Pitfall #9 (no state returned) | Return answered count + timestamp | HIGH |
| **Phase 42 (Session Resume)** | Resume from disconnect | Pitfall #2 (timer drift), #7 (question count change) | Server-side elapsed time, freeze question count, prevent mid-exam changes | HIGH |
| **Phase 43 (Worker Polling)** | CheckExamStatus polling | Pitfall #4 (TempData consumed), #5 (polling storm), #12 (auth leak) | Store token in DB, add 5s cache + invalidation, authorize before returning state | HIGH |
| **Phase 44 (Close Early)** | HC force-close | Pitfall #3 (double-submit race), #8 (progress mismatch) | Add RowVersion to AssessmentSession, serializable transaction, recalculate final progress | HIGH |
| **Phase 45 (HC Monitoring)** | Progress polling (HC side) | Pitfall #5 (polling storm), #8 (misleading count) | Cache progress endpoint (5-10s TTL), count non-null answers only, invalidate on SaveAnswer | MEDIUM |

---

## Verification Checklist for Implementation

Before marking any phase done, verify:

- [ ] **Pitfall #1 (Concurrent auto-save):** Client has debounce (300ms+) on SaveAnswer AJAX. Database has unique constraint on (sessionId, qId). ExecuteUpdateAsync used for atomic upsert.

- [ ] **Pitfall #2 (Timer drift):** SubmitExam calculates elapsed time server-side (DateTime.UtcNow - StartedAt). Client timer never sent to server. No client-supplied elapsed time in request body.

- [ ] **Pitfall #3 (Double-submit race):** AssessmentSession has [Timestamp] property (RowVersion). Status transitions wrapped in serializable transaction or optimistic concurrency checked. No silent overwrites on concurrent SubmitExam + CloseEarlySession.

- [ ] **Pitfall #4 (TempData consumed):** Token verification stored in AssessmentSession.TokenVerified (database), not TempData. Polling doesn't re-check token. Keep() or Peek() used only if TempData unavoidable.

- [ ] **Pitfall #5 (Polling storm):** CheckExamStatus uses IMemoryCache with 5s TTL. Cache invalidated on status change (CloseEarlySession). Polling interval ≥ 10s (or adaptive backoff). AsNoTracking() used for read-only queries.

- [ ] **Pitfall #6 (Upsert conflict):** Unique constraint on (sessionId, qId). ExecuteUpdateAsync used. No FirstOrDefault + Add pattern.

- [ ] **Pitfall #7 (Question count change):** QuestionCountAtStart stored in UserPackageAssignment. Resume checks count hasn't changed. HC prevented from modifying package during InProgress.

- [ ] **Pitfall #8 (Progress misleading):** Progress counts non-null answers only. Recalculated on SubmitExam from actual responses. Distinction between "attempted" and "answered" clear in UI.

- [ ] **Pitfall #9 (SaveAnswer no state):** SaveAnswer returns `{ success, answeredCount, savedAt, questionId, selectedOptionId }`. Client validates HTTP status.

- [ ] **Pitfall #10 (Auth leak):** CheckExamStatus verifies user owns session (userId check) before returning status. Non-authorized request returns Forbid(403).

---

## Sources

### Official Microsoft Documentation
- [Handling Concurrency Conflicts - EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Tutorial: Handle concurrency - ASP.NET MVC with EF Core](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/concurrency?view=aspnetcore-8.0)
- [Session and state management in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0)

### Concurrency and Race Condition Best Practices
- [Solving Race Conditions With EF Core Optimistic Locking](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking)
- [Race Conditions and Entity Framework Core - KPMG UK Engineering](https://medium.com/kpmg-uk-engineering/race-conditions-and-entity-framework-core-5f4ea8b308f6)
- [Guide to Handling Concurrency Conflicts in ASP.NET Core](https://medium.com/@chris.claude/guide-to-handling-concurrency-conflicts-in-asp-net-core-db26c75a8267)
- [Understanding Transaction Isolation Levels in Entity Framework Core](https://medium.com/@serhatalftkn/understanding-transaction-isolation-levels-in-entity-framework-core-89d8e89f0ec4)
- [A Clever Way To Implement Pessimistic Locking in EF Core](https://www.milanjovanovic.tech/blog/a-clever-way-to-implement-pessimistic-locking-in-ef-core)

### Polling, Caching, and API Design
- [7 best practices for polling API endpoints](https://www.merge.dev/blog/api-polling-best-practices)
- [Caching Best Practices in REST API Design](https://www.speakeasy.com/api-design/caching)
- [Cache optimization: Strategies to cut latency and cloud cost - Redis Blog](https://redis.io/blog/guide-to-cache-optimization-strategies/)

### Session and Time Management
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- [The Exam Engine - DEV Community](https://dev.to/insight105/the-exam-engine-206c)

### Auto-Save and AJAX Patterns
- [Implementing Efficient AutoSave with JavaScript Debounce Techniques](https://kannanravi.medium.com/implementing-efficient-autosave-with-javascript-debounce-techniques-463704595a7a)

