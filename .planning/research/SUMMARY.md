# Project Research Summary

**Project:** Portal HC KPB - v2.1 Milestone (Assessment Resilience & Real-Time Monitoring)
**Domain:** Web-based exam/assessment system with live HC monitoring (ASP.NET Core 8 MVC)
**Researched:** 2026-02-24
**Confidence:** HIGH

## Executive Summary

v2.1 adds four interdependent resilience and monitoring features to Portal HC's exam system: **auto-save on answer selection**, **session resume after disconnect**, **worker polling for HC-initiated closure**, and **live HC monitoring dashboard**. These are table-stakes expectations in modern online assessment platforms (Moodle, Blackboard, Pearson) and are achievable **without new external dependencies** — the stack extends existing ASP.NET Core endpoints and uses native browser APIs (Fetch, sessionStorage, setInterval).

The research reveals a **high-confidence, low-risk implementation path**: all four features layer cleanly onto the existing monolithic CMPController architecture (SaveAnswer already exists; CheckExamStatus already exists; session state table is extensible). The primary challenge is not technology selection but **concurrency handling** — auto-save, page navigation, and final submission can overlap in subtle ways that corrupt audit trails or create race conditions if not protected with atomic upserts and debouncing.

**Recommended approach:** Implement in strict dependency order (Phase 41 → 45). Phase 41 hardens SaveAnswer with atomic upserts + concurrency tokens. Phase 42 adds session resume capability. Phase 43 introduces memory caching to reduce database load from polling. Phases 44-45 are optional hardening and HC feature polish. This ordering eliminates integration surprises and ensures each phase's verification is clean before the next begins.

## Key Findings

### Recommended Stack

v2.1 uses **zero new NuGet packages**. All technology already exists in the codebase:

- **ASP.NET Core 8 MVC** — Extend existing SaveAnswer, CheckExamStatus endpoints; add UpdateSessionProgress, GetMonitoringProgress endpoints
- **Entity Framework Core 8** — Use ExecuteUpdateAsync for atomic upserts; add RowVersion [Timestamp] for concurrency detection
- **SQL Server/SQLite** — Two new columns to AssessmentSession (LastPageIndex, ElapsedSeconds); unique constraint on (SessionId, QuestionId) in PackageUserResponse
- **Fetch API (native ES6)** — AJAX for auto-save, polling, monitoring refresh; already used in codebase
- **sessionStorage (HTML5)** — Client-side ephemeral state for exam page/time (NEW, <100 bytes per session)
- **setInterval (ES3)** — Polling timer (already used for countdown timer)
- **jQuery 3.7.1** — Keep for backward compatibility; new code uses fetch()
- **Bootstrap 5** — UI framework unchanged

**Migration effort:** Add 1 EF Core migration with two columns and one unique constraint. Existing endpoints need parameter extensions but no signature changes.

**Load baseline (100 concurrent workers):**
- Auto-save: ~1.6 req/sec (debounced, fire-and-forget)
- Polling: ~10 req/sec (every 10-30s)
- Monitoring: <1 req/sec (every 5-10s for HC)
- **Total: ~12 req/sec** on 500-1000 MHz server (negligible)

**With Phase 43 memory caching (5-10s TTL):** Database load reduces 3-6x; even at 300 concurrent workers, system stays sustainable.

### Expected Features

#### Must Have (Table Stakes)

These features workers expect in modern online exams. Missing = product feels incomplete:

1. **Auto-save on click** — Worker selects radio button → immediate AJAX POST to SaveAnswer (debounced 300-500ms). Server uses atomic upsert pattern (ExecuteUpdateAsync) to prevent duplicates on concurrent requests.

2. **Auto-save before page navigation** — Clicking Prev/Next blocks navigation until all SaveAnswer requests on current page complete. Shows loading indicator ("Saving... please wait").

3. **Session resume after disconnect** — Browser close or network outage; worker logs back in and clicks "Resume Exam" → loads last-answered page with correct remaining timer (server-calculated from StartedAt + ExamWindowCloseDate).

4. **Worker polling for HC closure** — Exam page polls CheckExamStatus every 10-30 seconds. If HC closes exam (status changes), worker redirects to Results page immediately without manual refresh.

#### Should Have (Competitive Differentiators)

Features that improve operational visibility and worker confidence:

5. **Live HC monitoring dashboard** — HC's monitoring page auto-refreshes progress every 5-10s via AJAX (not full page reload). Shows "X/Y answered" per worker without scrolling reset or filter loss. Uses dedicated GetMonitoringProgress endpoint returning minimal JSON payload (~100 bytes).

**Optional UX enhancers (defer to v2.2+):**
- Visual feedback ("Saved" badges) during auto-save
- Graceful closure warning (grace period before hard redirect)
- Auto-extend exam window if worker actively answering

#### Anti-Features (Explicitly Avoid)

1. **Save on every keystroke** — Generates excessive AJAX traffic (100+ workers × 10+ saves/sec = 1000+ DB writes/sec overload). Mitigation: debounce 500ms+ minimum; save-on-blur preferred for text fields.

2. **Immediate redirect on closure without warning** — Removes worker agency; if closure is unintended, worker loses answers. Mitigation: redirect with "Exam closed by HC" message + brief grace period.

3. **Live timer countdown in HC dashboard** — Creates perception of lag (HC sees 4:32, worker sees 4:28). Mitigation: show only "time at last save"; client timer is authoritative.

4. **Full-page polling refresh for monitoring** — Destroys HC UX (scrolling resets, filter state lost). Mitigation: AJAX refresh only progress cells; preserve scroll and state.

### Architecture Approach

v2.1's architecture is **enhancement of existing CMPController**, not refactoring. All four features integrate via:

1. **Phase 41 (Auto-Save Foundation):** Harden SaveAnswer endpoint with ExecuteUpdateAsync (atomic upsert) + add RowVersion [Timestamp] for concurrency detection + enforce unique constraint on (SessionId, QuestionId). Add debounce(300ms) and request-blocking on Exam.cshtml. **Critical path item; blocks all others.**

2. **Phase 42 (Session Resume):** Add LastPageIndex + ElapsedSeconds columns to AssessmentSession. New UpdateSessionProgress endpoint saves page/time during navigation. Enhanced StartExam detects resume, populates ViewBag with stored values for client. Exam.cshtml updates sessionStorage every 5s and adds page-tracking to navigation.

3. **Phase 43 (Polling + Caching):** Add IMemoryCache to CheckExamStatus endpoint (5s TTL). Reduce polling interval from 30s to 10s. Cache invalidation on CloseEarlySession. **Reduces database load 3-6x at scale.**

4. **Phase 44 (Close Early Hardening, optional):** Add DbUpdateConcurrencyException handling. Verify RowVersion prevents race between SubmitExam and CloseEarlySession.

5. **Phase 45 (HC Monitoring):** New GetMonitoringProgress endpoint mirrors CheckExamStatus caching strategy. AssessmentMonitoringDetail.cshtml adds Progress column + 5-10s AJAX refresh loop.

**Data flow transformation:**

```
Current: Click → Manual Save → Submit (all answers collected at end)
New:     Click → Auto-Save (incremental) + Page Track + Polling → Submit (grades from pre-saved answers)
```

**Key patterns:**
- **Atomic upsert:** Prevents duplicate rows on concurrent saves
- **Server-side timer:** Client cannot manipulate exam duration (DevTools proof)
- **Memory cache with TTL:** Reduces DB load; invalidated on state changes
- **Optimistic concurrency (RowVersion):** Detects conflicts; allows safe concurrent updates

### Critical Pitfalls

1. **Race condition between auto-save and page navigation (SEVERITY: HIGH)**
   - Problem: Worker clicks radio → auto-save fires AJAX (non-blocking) → worker immediately clicks Next before AJAX completes → overlapping requests corrupt SubmittedAt or duplicate PackageOptionId
   - Impact: Audit trail shows wrong timestamps; LastPageIndex resume off-by-one; answered count incorrect in monitoring
   - Prevention: Debounce auto-save (max 1 request in-flight); block Prev/Next until all pending saves complete; use ExecuteUpdateAsync for atomic upsert; add RowVersion for concurrency detection
   - Detection: HC dashboard shows duplicate SubmittedAt for same question; session resume lands on wrong page
   - Phase: Phase 41 — must be addressed in auto-save feature itself, not later

2. **Session resume loads stale LastPageIndex if previous page's auto-save in-flight (SEVERITY: MEDIUM)**
   - Problem: Worker on page 5 → clicks Next → SaveAnswer for Q5 is in-flight → UpdateSessionProgress saves LastPageIndex=6 → server crashes → on resume, Q5's answer is lost but session says page 6
   - Impact: Worker resumes, doesn't see their answer to Q5; confusion about whether answer was saved
   - Prevention: Wait for all SaveAnswer requests to complete before calling UpdateSessionProgress; use client-side queue; timeout after 10s + warn worker
   - Detection: Test with network throttling; pause SaveAnswer response, click Next, verify navigation blocks
   - Phase: Phase 42 — critical during implementation; test before shipping

3. **Polling interval tuning creates UX/load trade-off (SEVERITY: MEDIUM)**
   - Problem: At 10s polling interval × 100 workers = 10 DB queries/sec; at 30s = 3.3 DB queries/sec. If cache TTL is 5s and polling is 30s, 25 out of 30 seconds are cache hits (efficient). If polling is 10s, hits are still ~80% but DB load is higher. No caching = collapse at 100+ workers.
   - Impact: Too frequent polling (1-2s) → overload; too infrequent (60s) → worker doesn't know exam is closed for 1 minute
   - Prevention: Default to 10-30s polling; MUST include Phase 43 memory cache (5s TTL). Monitor DB CPU during load test. If >60%, increase cache TTL or polling interval.
   - Detection: Load test 100 workers, monitor DB CPU over 10 min. Without cache, will spike >80%. With cache, should stay <30%.
   - Phase: Phase 43 — test polling + caching together; never deploy Phase 43 polling without Phase 43 caching

4. **EF Core migration timing issues (SEVERITY: MEDIUM)**
   - Problem: Adding LastPageIndex + ElapsedSeconds + RowVersion columns in separate migrations (Phase 41, 42) can create "column not found" errors if code runs mid-migration. If RowVersion added in Phase 41 but SaveAnswer code references it in Phase 42, code breaks until both migrations applied.
   - Impact: Production deployment failure; roll-back needed
   - Prevention: Create all three migrations in Phase 41 (RowVersion + unique constraint) and Phase 42 (LastPageIndex + ElapsedSeconds). Apply all before deploying code changes. Or: Feature-flag new code paths until migration complete.
   - Detection: Test deployment script on staging; verify migrations apply before code runs
   - Phase: Phase 41 & 42 — validate migration script and deployment process before release

5. **Antiforgery token conflicts with stateless GET monitoring requests (SEVERITY: LOW)**
   - Problem: SaveAnswer is POST (requires antiforgery token); GetMonitoringProgress is GET (no token). If code incorrectly expects token on GET, monitoring fails silently.
   - Impact: HC monitoring returns 400 Bad Request; monitoring dashboard blank
   - Prevention: Verify token handling: POSTs use [ValidateAntiForgeryToken], GETs use [AllowAnonymous] or omit token check. Document in code.
   - Detection: Test without antiforgery cookie; verify monitoring still loads
   - Phase: Phase 45 — simple check during implementation

6. **Incomplete question set mid-exam confuses session resume (SEVERITY: MEDIUM)**
   - Problem: Assessment has 30 questions; worker is on Q20. HC updates assessment to 25 questions. Worker resumes, page tries to render Q26 (doesn't exist anymore). Or: question IDs change (Q5 is now a different question entirely).
   - Impact: Worker sees broken page; admin dashboard breaks when grading orphaned answers
   - Prevention: On resume, validate question count unchanged. Store `StoredQuestionCount` on AssessmentSession at start; on UpdateSessionProgress, verify count matches. If not, block resume with "Assessment changed; cannot resume."
   - Detection: Test case: pause exam, change question set, attempt resume
   - Phase: Phase 42 — validation logic must be in UpdateSessionProgress endpoint

7. **Monitoring query O(n) explodes at 100+ sessions (SEVERITY: MEDIUM)**
   - Problem: GetMonitoringProgress runs `SELECT COUNT(*) FROM PackageUserResponses WHERE SessionId = X` for each session. With 100 sessions, 100 subqueries per refresh. At 5s intervals over 10 workers, 200 queries/sec.
   - Impact: Database CPU spike; monitoring dashboard slow to refresh; cascading slowness on worker polling
   - Prevention: Single efficient query: `SELECT SessionId, COUNT(*) FROM PackageUserResponses GROUP BY SessionId`. Materialize the result or cache for 10s. Avoid N+1 queries.
   - Detection: Load test with 100 sessions; monitor query execution time. Should be <100ms with grouping + cache, >5000ms without.
   - Phase: Phase 45 — implement as single efficient query, not loop; validate before shipping

8. **Client timer divergence from server time (SEVERITY: LOW)**
   - Problem: Worker's client-side timer (setInterval countdown) can drift from server's ExamWindowCloseDate by 3-5 seconds due to network latency, clock skew, or browser tab backgrounding. HC monitoring shows "4 min remaining" but worker sees "3:45".
   - Impact: Worker confusion; unlikely to affect scoring (server time is authoritative)
   - Prevention: Document: "Client timer is for UX only; server time is source of truth." On resume, reset client timer from server-calculated remaining time. Don't sync too frequently (adds polling overhead).
   - Detection: Open exam on two devices; compare timer displays. Should be within 5s.
   - Phase: Phase 42 — document in code comment; document in UX guide for HC staff

## Implications for Roadmap

Based on research, Phase v2.1 should be structured as **5 sequential phases** (41-45), with phases 41-43 **required** and phases 44-45 **optional but recommended**.

### Phase 41: Auto-Save Foundation (REQUIRED, BLOCKS ALL)

**Rationale:** Existing SaveAnswer endpoint is used by all features. It has a race condition (overlapping upsert calls corrupt data). Must be hardened first or all downstream features inherit the bug.

**Delivers:**
- ExecuteUpdateAsync atomic upsert on SaveAnswer (prevents duplicate rows)
- Unique constraint on (SessionId, QuestionId) in PackageUserResponse
- RowVersion [Timestamp] on PackageUserResponse for optimistic concurrency
- Debounce(300ms) + request-blocking on Exam.cshtml radio buttons
- Prevents rapid-fire SaveAnswer calls from causing data corruption

**Avoids pitfall:** Race condition between auto-save and page navigation (Pitfall 1)

**Verification checklist:**
- Load test: Single worker, 1000 rapid clicks on same question; verify 0 duplicate rows, 1 final answer saved
- Performance: SaveAnswer latency <10ms per request
- Concurrency: Two workers answering same question simultaneously; verify no duplicates or stale data

**Research flags:** No deep research needed; existing SaveAnswer implementation well-documented. Clear upgrade path.

---

### Phase 42: Session Resume (REQUIRED, MEDIUM PRIORITY)

**Rationale:** Resume capability depends on stable auto-save (Phase 41). Can't resume if saved answers are corrupted.

**Delivers:**
- LastPageIndex + ElapsedSeconds columns on AssessmentSession
- UpdateSessionProgress endpoint (new) to save page/time on navigation
- StartExam enhancement to detect resume and populate ViewBag
- Exam.cshtml page-tracking JS + timer UI reset
- Validation to prevent question-set changes mid-exam

**Uses:** Phase 41 (RowVersion), existing session state table

**Avoids pitfalls:** Session resume stale data (Pitfall 2), incomplete question set (Pitfall 6)

**Verification checklist:**
- Resume test: Pause exam at Q15, page 2; network down 5 min; resume → correct page + correct timer
- Question change test: Pause exam, admin changes question count; resume → blocked with "Assessment changed"
- Timer accuracy test: Resume, compare client timer to server clock; should be within 5s
- Page tracking test: Navigate 5 pages, stop, resume; verify lands on correct page

**Research flags:** Standard pattern; well-documented in research. Clear implementation path.

---

### Phase 43: Polling + Caching Infrastructure (REQUIRED, HIGH IMPACT)

**Rationale:** Without caching, 100 workers polling every 10s = 10 DB queries/sec; grows to 100+ at 300 workers. With 5s memory cache, reduces to 2-3 DB queries/sec. **Essential for scalability.**

**Delivers:**
- IMemoryCache registration in Startup.cs
- CheckExamStatus endpoint enhanced with 5s cache (key: exam_status_{sessionId})
- CloseEarlySession enhanced with cache invalidation
- Exam.cshtml polling loop (10-30s interval)
- GetMonitoringProgress endpoint with 10s cache (key: monitoring_{title}_{category}_{date})
- Polling verification: 100 workers, 10s interval, 10min duration → DB <30% CPU

**Avoids pitfalls:** Polling interval tuning (Pitfall 3), monitoring query O(n) (Pitfall 7)

**Verification checklist:**
- Load test: 100 concurrent workers, 10s polling, 10min exam duration; monitor DB CPU (target <30%)
- Cache hit rate: Should be >80% (5s cache + 10-30s polling interval)
- Cache invalidation: Close Early clears cache; next worker poll fetches fresh data
- Monitoring query: 100 sessions, 5s refresh; query latency <100ms with grouping + cache

**Research flags:** Memory caching is standard ASP.NET Core pattern. No exotic dependencies. Well-documented.

---

### Phase 44: Close Early Hardening (OPTIONAL, LOWER PRIORITY)

**Rationale:** Close Early feature already shipped (Phase 39). Phase 44 hardens it for concurrency. Can defer if schedule tight.

**Delivers:**
- DbUpdateConcurrencyException handling on SubmitExam + CloseEarlySession race
- Verification: Worker submits at T=3599s, HC closes at T=3600s → both requests succeed, final state correct, no data loss

**Uses:** Phase 41 (RowVersion), Phase 43 (caching)

**Avoids pitfalls:** (None new; hardens existing feature)

**Verification checklist:**
- Race test: Simultaneous SubmitExam + CloseEarlySession; verify scores match, no orphaned records
- Concurrency exception: Catch DbUpdateConcurrencyException; retry with fresh data

**Research flags:** Low risk; well-understood concurrency pattern. Can ship later or skip if feature proves stable.

---

### Phase 45: HC Monitoring Dashboard (OPTIONAL, NICE-TO-HAVE)

**Rationale:** HC monitoring is differentiator, not blocker. Workers don't need it. Can be deferred if schedule tight. Reuses Phase 43 caching infrastructure.

**Delivers:**
- GetMonitoringProgress endpoint (new, mirrors CheckExamStatus caching)
- AssessmentMonitoringDetail.cshtml: Progress column + 5-10s AJAX refresh JS
- HC sees real-time "X/Y answered" per worker without full page reload
- Preserves HC scroll position, filter state, sort order

**Uses:** Phase 43 (caching), Phase 41 (stable SaveAnswer)

**Avoids pitfalls:** Monitoring query O(n) (Pitfall 7)

**Verification checklist:**
- Real-time test: HC watches worker; progress updates within 5-10s
- Accuracy test: Answer counts match DB (no off-by-one)
- Unload test: 500 workers, monitoring dashboard; query latency <2s
- UX test: HC refreshes; scroll position + filter state preserved

**Research flags:** Well-documented pattern; efficient single-query design. Standard AJAX refresh technique.

---

## Phase Ordering Rationale

```
Phase 41: Auto-Save (foundation, required)
   ↓
Phase 42: Resume (requires stable auto-save)
   ↓
Phase 43: Polling + Caching (uses stable session state, improves scalability)
   ├─→ Phase 44: Close Early (optional hardening)
   └─→ Phase 45: Monitoring (optional UX enhancement)
```

**Why this order:**
1. Phases 41-43 are **required** for v2.1 launch (worker resilience + basic monitoring)
2. Phase 41 blocks 42 (resume needs working auto-save)
3. Phase 42 should complete before 43 goes live (caching must cache correct session state)
4. Phases 44-45 are **optional** (can ship v2.1 without them, add in v2.2)
5. **Cannot skip phases:** Each enables the next; skipping introduces gaps

**Dependency chain:** 41 → 42 → 43; then 44+45 in parallel (both depend on 43, not on each other)

---

## Research Flags

**Phases likely needing deeper research during planning:**

- **Phase 43 (Polling + Caching):** Load test with 100-300 concurrent workers required. Memory cache tuning (TTL adjustment) depends on production network latency. Recommend `/gsd:research-phase` for load testing script + metrics dashboard.

- **Phase 45 (Monitoring Dashboard):** Query optimization for 100+ sessions requires testing. If monitoring frequently refreshes, may need Redis or materialized view (out of scope for v2.1). Recommend load test during implementation.

**Phases with standard patterns (skip research-phase):**

- **Phase 41 (Auto-Save):** Atomic upsert + debouncing are well-established patterns. SaveAnswer code review sufficient.
- **Phase 42 (Resume):** Server-side timer, session state tracking are standard. EF Core migration straightforward.
- **Phase 44 (Concurrency):** RowVersion + DbUpdateConcurrencyException handling are documented EF Core patterns.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| **Stack** | HIGH | ASP.NET Core 8, EF Core, Fetch, sessionStorage all mature, well-documented. No dependency surprises. |
| **Features** | MEDIUM-HIGH | Core five features (auto-save, resume, polling, monitoring) verified against Moodle/Blackboard/Pearson. Edge cases documented. Confidence limited by lack of phase-specific testing (will clarify during Phase 35 planning). |
| **Architecture** | HIGH | Existing CMPController, SaveAnswer, CheckExamStatus endpoints thoroughly analyzed. Integration points clear. No refactoring surprises. |
| **Pitfalls** | HIGH | Eight critical pitfalls identified via direct codebase analysis. Prevention strategies clear. No unknown gotchas. |
| **Load Estimates** | MEDIUM | Baseline estimates (12 req/sec at 100 workers) are conservative. Phase 43 caching strategy verified. Will require load testing in Phase 43 to confirm 300+ worker scalability. |

**Overall confidence:** HIGH — stack is proven, features are standard, architecture is straightforward, pitfalls are understood. Main uncertainty is load testing at scale; recommend load test script during Phase 43.

### Gaps to Address During Phase Planning

1. **Load testing script** (Phase 43) — Need way to simulate 100-300 concurrent workers. Locust/JMeter recommended.
2. **HC monitoring query optimization** (Phase 45) — Single GROUP BY query designed; requires verification with 100+ sessions to ensure <2s latency.
3. **Deployment validation** (Phase 41-42) — Migration script must apply before code runs. Need deployment checklist.
4. **Feature flag for gradual rollout** (Optional) — If auto-save causes issues, want quick off-switch without redeployment.

---

## Sources

### Stack Research
- Microsoft Learn: EF Core ExecuteUpdateAsync, Concurrency, Memory Cache
- MDN: Fetch API, sessionStorage, setInterval
- Codebase: STACK.md (detailed version requirements, load estimates, browser compatibility)

### Feature Research
- Competitive analysis: Moodle, Blackboard, Pearson, Scrum.org, edX
- Online exam platforms: BlinkExam, MeritTrac, SoftwareSuggest surveys
- Codebase: FEATURES.md (edge cases, dependency analysis, MVP definition)

### Architecture Research
- Direct codebase review: CMPController.cs, Exam.cshtml, StartExam.cshtml
- EF Core patterns: Atomic upserts, RowVersion, cached queries
- ASP.NET Core: IMemoryCache, antiforgery tokens, role-based authorization
- Codebase: ARCHITECTURE-v2.1-FEATURES.md (phase-by-phase integration, data flow, deployment checklist)

### Pitfalls Research
- Concurrency scenarios: Race conditions between auto-save, navigation, submit
- Database migration timing: Column additions, constraint timing
- Polling scalability: Cache tuning, TTL strategy
- Codebase: v2.1-PITFALLS.md (8 critical pitfalls, detection methods, prevention strategies)

---

*Research completed: 2026-02-24*
*Ready for roadmap: YES*
*Next step: `/gsd:roadmap-v2.1` to create detailed phase breakdown*
