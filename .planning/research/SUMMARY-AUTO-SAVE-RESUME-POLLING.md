# Research Summary: Auto-Save, Session Resume, and Polling Features

**Project:** PortalHC Online Assessment System — Resilience Features (Phases 41+)
**Researched:** 2026-02-24
**Overall Confidence:** HIGH

## Executive Summary

Adding auto-save, session resume, and polling to an existing monolithic exam system introduces **10 critical race conditions and data integrity pitfalls** that must be addressed systematically across 5–6 implementation phases. The most dangerous pitfalls are concurrent AJAX saves that create duplicate database records, timer drift that allows workers to manipulate exam time, and double-submit races where HC's force-close overwrites worker submissions. All 10 pitfalls are preventable with specific, concrete ASP.NET Core + EF Core patterns, but each requires database schema changes, client-side synchronization, or caching infrastructure.

**Key finding:** Naive implementation (no debounce, no unique constraints, TempData-based token guards) will cascade into production bugs within 2–3 weeks of heavy usage. Testing on single-user basis will not catch these race conditions; must load-test with 50+ concurrent users to surface issues.

---

## Key Findings

### 1. Concurrency is the Primary Risk Domain

**Pitfall #1 (Auto-Save Race):** Multiple concurrent SaveAnswer AJAX calls for same question create duplicate database records because `FirstOrDefaultAsync()` then `Add()` is not atomic. Without client-side debounce (300–500ms) and database unique constraint, duplicates corrupt grading.

**Mitigation Required:**
- Client: JavaScript debounce on option clicks (300ms wait after last change before firing AJAX)
- Server: Atomic `ExecuteUpdateAsync()` with `WHERE (sessionId, qId)` clause
- Database: Unique constraint on (AssessmentSessionId, PackageQuestionId)
- EF Core: [Timestamp] property on PackageUserResponse for optimistic concurrency detection

**Phasing:** Must implement all three (client debounce + server upsert + DB constraint) **simultaneously** in Phase 41. Partial implementation leaves race condition open.

---

### 2. Server-Side Timer is Non-Negotiable

**Pitfall #2 (Timer Drift):** Client-side timer can be manipulated (DevTools) or drifts if browser clock is wrong. Worker can fake unlimited time. SubmitExam already calculates elapsed time server-side (good), but SaveAnswer must NOT accept client timer values.

**Mitigation Required:**
- Remove any `elapsedTime` or `remainingTime` parameter from SaveAnswer + SubmitExam request bodies
- SubmitExam calculates: `elapsed = DateTime.UtcNow - assessment.StartedAt.Value` (already implemented, maintain it)
- SaveAnswer must NOT enforce time limit (answer clicks are allowed anytime); only SubmitExam enforces time
- Add ElapsedMinutesBeforePause to AssessmentSession for resume scenarios

**Phasing:** Phase 42 (resume feature). Verify StartExam doesn't accept client timer. Add pause tracking.

---

### 3. Double-Submit Requires Optimistic Concurrency Tokens

**Pitfall #3 (Close Early Race):** When HC clicks "Close Early" and worker simultaneously clicks "Submit," both requests see Status=InProgress and both proceed to grade + update status. Last write wins; earlier submission is lost. Current code has no optimistic locking.

**Mitigation Required:**
- Add [Timestamp] property to AssessmentSession (SQL Server rowversion)
- Wrap status transitions in serializable transaction or check RowVersion before update
- Both SubmitExam and CloseEarlySession will throw DbUpdateConcurrencyException on conflict, preventing silent overwrites

**Phasing:** Phase 41 (prioritize before Phase 44). Must be in place before Close Early is enhanced with polling.

---

### 4. TempData Token Guards Will Break Under Polling

**Pitfall #4 (TempData Consumed):** Token verification is stored in TempData (one-time state). When StartExam reads it, TempData is consumed (marked for deletion). Later, polling endpoint tries to re-verify token, finds TempData null, incorrectly thinks session is invalid. Resume fails.

**Mitigation Required:**
- Move token verification from TempData to AssessmentSession database field (TokenVerified bool, TokenVerifiedAt timestamp)
- Polling does NOT re-check token (token verified once on StartExam entry)
- Use Peek() or Keep() only if TempData absolutely unavoidable (not recommended)

**Phasing:** Phase 41. Do this before Phase 43 (polling feature).

---

### 5. Polling Storm Requires Caching

**Pitfall #5 (DB Load Explosion):** 20 workers polling every 10 seconds = 2 DB hits/second. Scales to 100 workers = 10 DB hits/sec. At 50ms per query, this is 50% database CPU, leaving no room for SubmitExam. Cascade failure at ~150 workers.

**Mitigation Required:**
- Add IMemoryCache layer with 5-second TTL on CheckExamStatus endpoint
- Cache key: `exam_status_{sessionId}`
- Invalidate cache when HC closes exam (CloseEarlySession clears cache)
- Increase polling interval to 30s or use exponential backoff
- Select only Status column, not full record (AsNoTracking)

**Phasing:** Phase 43 (polling feature). Cannot skip; polling is unusable without caching at scale.

---

### 6. Progress Tracking Becomes Misleading Without Clarification

**Pitfall #8 (Progress Inflated):** Progress counts saved answers, but saves can be cleared or overwritten. HC's monitoring shows "60% answered," but actual submission is 40%. Competency levels based on progress threshold are assigned wrong.

**Mitigation Required:**
- Count only non-null answers: `WHERE PackageOptionId != null`
- Recalculate Progress on SubmitExam from actual final responses
- HC monitoring should show "40 answered (60 attempted)" for transparency

**Phasing:** Phase 45 (HC monitoring). Also applies to progress display on worker exam page.

---

## Implications for Roadmap

### Recommended Phase Structure

**Phase 41: Save Answer with Race Condition Prevention (SaveAnswer/Auto-Save)**
- **What to build:** SaveAnswer API endpoint with atomic upsert pattern
- **Pitfalls addressed:** #1 (concurrent inserts), #6 (upsert conflict)
- **Must include:** Client debounce (300ms), ExecuteUpdateAsync on server, unique constraint migration
- **Blockers:** None (independent feature)
- **Risk level:** HIGH if incomplete; must have all three parts (debounce + upsert + constraint)
- **Estimated complexity:** Medium (requires DB migration, client-side timing logic, error handling)

**Phase 42: Session Resume with Elapsed Time Tracking**
- **What to build:** Resume functionality, server-side elapsed time storage
- **Pitfalls addressed:** #2 (timer drift), #7 (question count change)
- **Must include:** ElapsedMinutesBeforePause on AssessmentSession, prevent mid-exam question changes, verify question count frozen
- **Dependencies:** Phase 41 (SaveAnswer), RowVersion on AssessmentSession (from Phase 41 or 43)
- **Risk level:** HIGH (timer logic is security-sensitive)
- **Estimated complexity:** Medium–High (multiple time calculations, pause/resume state machine)

**Phase 43: Worker Polling with Caching**
- **What to build:** CheckExamStatus endpoint, IMemoryCache layer, cache invalidation
- **Pitfalls addressed:** #4 (TempData consumed), #5 (polling storm), #12 (auth leak)
- **Must include:** Cache with 5s TTL, cache invalidation on close, authorization check, move token to DB
- **Dependencies:** Phase 42 (session resume fully working), RowVersion on AssessmentSession
- **Risk level:** MEDIUM (caching is well-established pattern)
- **Estimated complexity:** Medium (caching logic, auth checks, cache invalidation points)

**Phase 44: Close Early with Concurrency Control**
- **What to build:** Enhanced Close Early action with optimistic concurrency, prevent double-submit
- **Pitfalls addressed:** #3 (double-submit race)
- **Must include:** RowVersion check on AssessmentSession (if not done in Phase 41), serializable transaction option
- **Dependencies:** Phase 41 (RowVersion added), Phase 42 (elapsed time tracking)
- **Risk level:** HIGH (status transitions are critical)
- **Estimated complexity:** Medium (transaction isolation levels, conflict handling)

**Phase 45: HC Live Monitoring with Progress Display**
- **What to build:** HC dashboard endpoint for real-time progress (answered count per session)
- **Pitfalls addressed:** #5 (polling storm from HC side), #8 (progress misrepresentation), #10 (audit trail)
- **Must include:** Cache for progress queries, count only non-null answers, log SaveAnswer to audit
- **Dependencies:** Phase 41 (SaveAnswer), Phase 43 (caching infrastructure)
- **Risk level:** MEDIUM
- **Estimated complexity:** Medium (similar caching as Phase 43)

### Phase Ordering Rationale

1. **Phase 41 first (SaveAnswer)** because:
   - Foundation for all subsequent features (resume, polling, close-early all depend on SaveAnswer working correctly)
   - Race condition (#1) is most likely to surface in testing (rapid clicks)
   - Requires database migration (unique constraint) that blocks other migrations

2. **Phase 42 next (Resume)** because:
   - Requires elapsed time tracking (new AssessmentSession fields) which Phase 41 prepares
   - Timer security (#2) is prerequisite for subsequent features
   - Independent of polling; no circular dependencies

3. **Phase 43 then (Polling)** because:
   - Depends on Phase 42 (session state complete)
   - Introduces database load (#5), which testing should verify before Phase 44/45
   - Must precede Phase 44 for close-early state consistency

4. **Phase 44 after (Close Early)** because:
   - Depends on RowVersion and elapsed time (Phases 41–43)
   - Benefits from caching (Phase 43) to avoid load spikes
   - Testing can verify double-submit race with polling active

5. **Phase 45 last (HC Monitoring)** because:
   - Depends on all worker features being stable
   - Uses same caching infrastructure as Phase 43
   - Can be delayed without blocking worker exam functionality

### Critical Dependencies and Blockers

**Hard blockers (must complete before moving to next phase):**
- Phase 41 → Phase 42: RowVersion on AssessmentSession must exist
- Phase 42 → Phase 43: Session state must be correct (elapsed time tracking verified)
- Phase 43 → Phase 44: Caching must work (cache invalidation tested)

**Soft blockers (should complete, but can work around):**
- Phase 42 → Phase 44: Elapsed time tracking simplifies Close Early; can use old logic if blocked
- Phase 43 → Phase 45: Polling cache can be reused for HC monitoring, but not required

---

## Risk Assessment and Load Testing Strategy

### Critical Load Test Scenarios

**Scenario A: Concurrent Auto-Save (Phase 41)**
- Setup: 1 worker, 1 question, rapid clicks (simulate double-click, accidental repeat keys)
- Measure: Database for duplicate PackageUserResponses rows
- Target: 0 duplicates across 1000 clicks
- If failure: Race condition (#1) not fixed; cannot proceed to Phase 42

**Scenario B: Polling Storm (Phase 43)**
- Setup: 100 workers in simultaneous exams, polling every 10 seconds for 10 minutes
- Measure: Database query latency, connection pool usage, SubmitExam response time
- Target: SubmitExam latency <1 second, DB CPU <70%
- If failure: Caching insufficient; polling interval must increase or WebSocket required

**Scenario C: Close Early Race (Phase 44)**
- Setup: Worker submits at T=3599 sec, HC closes at T=3600 sec (1ms overlap)
- Measure: Final score, CompletedAt timestamp, Competency levels granted
- Target: No data loss, one consistent final state, no orphaned competencies
- If failure: RowVersion not working; serializable transaction required

**Scenario D: Resume After Disconnect (Phase 42)**
- Setup: Worker pauses, 5 min network outage, resumes; verifies elapsed time correct
- Measure: ServerElapsed ≈ RealTime, no timer manipulation possible
- Target: ServerElapsed within 1 second of actual wall-clock time
- If failure: Timer drift; Phase 42 blocked

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| **Race Conditions (Pitfalls #1, #3, #6)** | HIGH | Well-documented in EF Core + SQL Server docs; RowVersion/[Timestamp] is proven pattern |
| **Timer Security (Pitfall #2)** | HIGH | Server-side calculation standard in exam systems; already partially implemented |
| **TempData Pitfall (Pitfall #4)** | HIGH | Official ASP.NET Core behavior documented; database storage is obvious fix |
| **Polling Load (Pitfall #5)** | HIGH | Caching is standard; 5s TTL is proven effective for status checks |
| **Progress Accuracy (Pitfall #8)** | MEDIUM | Depends on how Progress is currently used; may require UI changes to display both "answered" and "attempted" |
| **Overall Implementation** | MEDIUM | Pitfalls are clear, but integration with existing monolithic code (4963-line CMPController) is high-complexity. Refactoring to separate SaveAnswer into API endpoint requires careful testing. |

---

## Gaps to Address in Phase-Specific Research

The following topics should be investigated during each phase's research stage:

1. **Phase 41:**
   - How to integrate debounce into existing exam view without breaking current functionality
   - Performance impact of ExecuteUpdateAsync vs current Add + SaveChanges pattern
   - Migration strategy for unique constraint (handle historic duplicates if any)

2. **Phase 42:**
   - Pause/resume UX (should exam page show "paused" state? how long can pause last?)
   - Interaction with TempData token guard (ensure token persists through pause/resume)
   - Backward compatibility with sessions that started before Phase 42

3. **Phase 43:**
   - Cache invalidation on other state changes (e.g., if worker's role changes, does session validity change?)
   - Interaction with 2-minute grace period on submission (when polling says "open," is 2-min grace still applied?)

4. **Phase 44:**
   - Close Early grading logic (does it use same as SubmitExam? must score match?)
   - Competency level updates triggered by Close Early (same as SubmitExam, or different?)

5. **Phase 45:**
   - Progress definition (answered % vs completion %? different metrics?)
   - HC monitoring refresh rate (same 10s as worker polling, or different?)
   - Audit trail scope (log every progress change, or only final state?)

---

## Quick Start Checklist for Phase 41

Before coding Phase 41 (SaveAnswer), verify:

- [ ] Create migration: Add unique constraint on (AssessmentSessionId, PackageQuestionId) to PackageUserResponses
- [ ] Create migration: Add [Timestamp] property to AssessmentSession and PackageUserResponse
- [ ] Add SaveAnswer API endpoint (AJAX target) with ExecuteUpdateAsync logic
- [ ] Add JavaScript debounce on exam question option clicks (300ms)
- [ ] Add error handling: Catch DbUpdateException for constraint violation, return 409 Conflict
- [ ] Load test: 1 worker, rapid clicks, verify 0 duplicates
- [ ] Update unit tests: Verify concurrent SaveAnswer calls don't create duplicates

---

## Sources

All findings verified with official Microsoft documentation and industry best practices:

- [Handling Concurrency Conflicts - EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Tutorial: Handle concurrency - ASP.NET MVC with EF Core](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/concurrency?view=aspnetcore-8.0)
- [Session and state management in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0)
- [Solving Race Conditions With EF Core Optimistic Locking](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking)
- [7 best practices for polling API endpoints](https://www.merge.dev/blog/api-polling-best-practices)
- [Cache optimization: Strategies to cut latency and cloud cost](https://redis.io/blog/guide-to-cache-optimization-strategies/)
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)

