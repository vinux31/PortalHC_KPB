# Feature Research: v2.1 Assessment Resilience & Real-Time Monitoring

**Domain:** Web-based exam/assessment system with live HC monitoring (ASP.NET Core 8 MVC, Razor server-side rendering, polling-based architecture)
**Researched:** 2026-02-24
**Confidence:** MEDIUM (patterns verified with multiple exam platforms; specific ASP.NET MVC implementation requires phase-specific research)

## Executive Summary

This milestone adds 5 resilience and monitoring features to Portal HC KPB's existing exam system. Three are **table stakes** (auto-save on interaction, resume on disconnect, worker polling for closure) expected by workers in modern online exam systems. Two are **differentiators** (auto-save before navigation, live HC monitoring dashboard). All 5 depend on existing infrastructure (SaveAnswer endpoint, CheckExamStatus, session state) and require careful handling of timing, state consistency, and edge cases.

---

## Feature Landscape

### Table Stakes (Workers Expect These)

Features users assume exist in modern online exams. Missing these = product feels incomplete or frustrating.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Auto-save on click** (radio button selection) | Workers in modern LMS platforms (Moodle, Blackboard, Pearson) expect answers to save automatically on selection, not requiring manual save button | MEDIUM | Debounced or immediate firing of SaveAnswer AJAX POST; needs feedback to worker (e.g., save indicator); edge case: rapid clicks before first save completes |
| **Auto-save before page navigation** (Prev/Next buttons) | Standardized across LMS platforms (Moodle, Blackboard, SurveyMonkey); prevents data loss when worker navigates away | MEDIUM | Page navigation blocked until current page answers saved; requires SaveAnswer to complete before Prev/Next buttons become active; risk: worker perceives lag if SaveAnswer is slow |
| **Session resume after disconnect** | Pearson, edX, Gallup, Scrum.org all support this; worker expectations set by consumer-grade apps (Gmail auto-recover drafts) | MEDIUM-HIGH | Exam resumes on last page with accurate remaining timer, not page 1; server tracks last answered question; requires exam window close check on resume |
| **Worker polling for session closure** (exam-page polls every 10s) | Industry standard for non-WebSocket systems; workers need to know immediately when HC has closed exam to avoid wasted effort answering | LOW-MEDIUM | Lightweight GET CheckExamStatus every 10s; on CloseEarly status, redirect to Results page; edge case: race condition if worker answers while poll detects closure |

### Differentiators (Competitive Advantage)

Features that set Portal HC apart. Not required, but valuable for worker UX and HC operational control.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Live HC monitoring dashboard** (MonitoringDetail auto-refresh 5-10s) | HC staff see real-time progress ("X/Y answered") without page refresh; enables reactive monitoring (e.g., spot check slow workers, identify technical issues). Currently missing: HC must manually refresh page to see progress. | MEDIUM | Polling-based AJAX refresh of progress column; only updates MonitoringDetail table rows, not full page reload; requires delta tracking (only fetch changed sessions); caching strategy needed to avoid O(n) queries per poll |
| **Auto-save before page nav with visual feedback** | Workers see confirmation that answer saved before page changes; reduces anxiety about lost data; differentiates from systems with silent saves | LOW | Progress bar or "Saved" badge on current page answers; requires coordination between SaveAnswer response time and Prev/Next button re-enable timing |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems. Explicitly avoid or defer.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Save on every keystroke** (for text/long-answer questions) | Intuitive; "real-time sync" sounds good | Generates excessive AJAX traffic if debounce is too aggressive (< 500ms); server load spikes with 100-500 concurrent workers; creates false sense of safety (network can still drop between saves); harder to debug race conditions | Use 1-2 second debounce minimum; prioritize save-on-blur (when worker leaves input field) and save-before-nav |
| **Immediate redirect on CloseEarly without warning** | Prevents workers from continuing to answer after closure | Removes worker agency; if closure is unintended (HC fat-finger), worker loses answers. Frustration spike. | Redirect with "Exam closed by HC" message + option to view/print answers before leaving; brief grace period (5-10s) for worker to copy notes if needed |
| **Live timer countdown in HC dashboard** | Nice visual; "see exactly what workers see" | Requires per-worker timer sync on every poll interval; creates perception of lag (HC timer shows 4:32 but worker's timer shows 4:28); confuses HC if they don't understand client-side timer is authoritative | Show only "time remaining at last save" in HC dashboard; let worker's client timer be source of truth |
| **Full-page polling refresh for monitoring dashboard** (vs. partial AJAX) | Simpler to implement (reload whole page) | Destroys HC UX: scrolling resets, filter/sort state lost, visible flicker every 5-10s, impossible to read while monitoring | AJAX-refresh specific DOM regions (progress rows, counts); preserve scroll position and UI state |

---

## Feature Dependencies

```
SaveAnswer AJAX endpoint (existing)
    └──requires──> Auto-save on click
    └──requires──> Auto-save before page nav
    └──requires──> Live HC monitoring (reads PackageUserResponse for answered count)

CheckExamStatus endpoint (existing)
    └──requires──> Worker polling for closure
    └──requires──> Session resume (verifies ExamWindowCloseDate still valid)

Session state (StartedAt, CompletedAt, Score, ExamWindowCloseDate, Status)
    └──requires──> Session resume (re-calculates remaining time = ExamWindowCloseDate - now)
    └──requires──> Worker polling for closure (checks Status == CloseEarly)

Existing exam timer (JS setInterval countdown)
    └──enhanced by──> Session resume (reset timer to remaining seconds from server)
    └──conflicts──> Live timer in HC dashboard (client-side timer diverges from server)

Existing Prev/Next button navigation
    └──enhanced by──> Auto-save before page nav (blocks nav until save completes)
    └──enhanced by──> Auto-save click feedback (shows which answers pending save)

PackageUserResponse table (one row per question per user)
    └──queried by──> Live HC monitoring (count answered = WHERE answered IS NOT NULL)
    └──depends on──> Auto-save on click (populates PackageUserResponse.answer)
```

### Dependency Notes

- **SaveAnswer endpoint enables three features:** auto-save on click, auto-save before nav, and HC dashboard monitoring all depend on SaveAnswer working reliably and quickly.
- **Session resume conflicts with page-1-reset assumption:** Current code may assume exam always starts at page 1. Resume requires storing/retrieving last page viewed.
- **Worker polling + ExamWindowCloseDate creates race condition:** If HC closes exam at same moment worker submits answer to page N, poll might not detect closure immediately. Mitigation: next page always checks status server-side before rendering.
- **Live HC monitoring requires efficient queries:** If HC dashboard runs `SELECT COUNT(*) FROM PackageUserResponse WHERE assessment_id=X AND answered IS NOT NULL` every 5s with 100+ sessions, database load grows. Solution: materialized view, Redis cache, or denormalized progress column.

---

## Feature Behaviors & Edge Cases

### 1. Auto-Save on Click (Radio Button Selection)

**Standard behavior across Moodle, Pearson, Blackboard:**
- Worker selects a radio button → SaveAnswer AJAX POST fires (debounced or immediate)
- Server returns 200 OK with timestamp
- Worker sees no visual change (silent save) OR optional progress indicator

**Edge cases to handle:**

| Edge Case | Behavior | Mitigation |
|-----------|----------|-----------|
| Rapid clicks (A → B → C within 500ms) | Which answer gets saved? Last one, or race condition? | Debounce 500-1000ms; on each new click, cancel pending save timer and restart. Ensure last selection is the one saved. |
| SaveAnswer fails (500 error) | Does answer stay selected on client? | YES - keep selection on client. Add retry logic (exponential backoff). Alert worker if 3 retries fail. |
| Worker closes tab before save completes | Answer lost | Accept as unavoidable without service workers. SessionStorage can cache unsaved answers. |
| Worker selects, navigates before SaveAnswer response | Is answer saved or lost? | Block Prev/Next buttons until SaveAnswer completes (show disabled state). Clarify to worker: "Saving answer... please wait" |
| Rapid clicks on different questions across pages | Cross-page state issues | Keep SaveAnswer per-question isolated. Don't assume all answers on page save atomically. |

**Implementation notes for Portal HC:**
- Use `setInterval` debounce (already familiar in codebase for timer)
- SaveAnswer already exists; add debouncing logic on client-side radio button change handler
- Consider adding `data-save-status="pending|saved|error"` attributes to track per-question state
- Return timestamp from SaveAnswer endpoint; store in `data-saved-at` to show "Saved at 14:32:05"

---

### 2. Auto-Save Before Page Navigation (Prev/Next Buttons)

**Standard behavior (Moodle, Blackboard, Pearson):**
- Worker clicks Prev/Next
- Current page answers auto-save (SaveAnswer AJAX for all unsaved answers on current page)
- Wait for all SaveAnswer requests to complete
- Only then navigate to previous/next page
- Worker sees page stay same until save completes, then page changes

**Edge cases to handle:**

| Edge Case | Behavior | Mitigation |
|-----------|----------|-----------|
| Multiple answers on page all fire SaveAnswer simultaneously | Race condition in server? | No; SaveAnswer is idempotent (upsert PackageUserResponse by question ID). Safe to fire in parallel. |
| One SaveAnswer fails (e.g., 503 service unavailable) | Does Prev/Next still happen? | NO - block navigation. Show error message: "Failed to save answer to Q5. Please try again." Retry button. |
| Worker navigates before page finishes rendering | Answer from old page + new page both pending | Disable Prev/Next until page fully rendered and ready state verified. |
| Network latency (SaveAnswer takes 2s) | Worker perceives lag | Accept. Show loading spinner on Prev/Next button. Clear communication: "Saving... please wait". Timeout after 10s and warn worker. |
| Worker is on page 3 of 4, clicks Prev twice rapidly | Both clicks queued or second ignored? | Queue prevention: disable Prev/Next until first navigation completes. Second click ignored. |

**Implementation notes for Portal HC:**
- On Prev/Next click, gather all unsaved answers on current page (query DOM for changed radio selections vs. cached state)
- Fire SaveAnswer AJAX for each; use Promise.all() to wait for all to complete
- Only enable Prev/Next button after all saves succeed
- Add visual feedback: spinner or "Saving before moving..." message

---

### 3. Session Resume After Disconnect

**Standard behavior (Pearson, edX, Gallup, Scrum.org):**
- Worker takes exam, browser closes/disconnects at question 15, page 2
- Worker logs back in, clicks "Resume Exam"
- Exam page loads with question 15 visible (not question 1)
- Timer shows correct remaining time (not full time, not zero)
- All previously saved answers still visible in their respective questions

**Edge cases to handle:**

| Edge Case | Behavior | Mitigation |
|-----------|----------|-----------|
| Worker disconnects, HC closes exam while offline | Worker resumes, page shows "Exam closed" | On resume, CheckExamStatus must verify Status != CloseEarly. If closed, show results page instead of exam page. |
| Exam window close time passed while worker offline | Timer would show negative | Server checks ExamWindowCloseDate vs current time. If expired, treat same as CloseEarly. |
| Worker resumes on different device | Session state must be device-agnostic | Session stored server-side (AssessmentSession table), not in browser storage. OK to resume on any device. |
| Last page answered was partial (e.g., Q28 answered, Q29-30 blank) | Resume position: page with Q28 or page with Q29? | Resume to last page visited (store `LastPageNumber` in AssessmentSession). Preserve "blank answer = unanswered" distinction. |
| SaveAnswer was in-flight when disconnect occurred | Was answer saved? | Worker resumes; sees either saved or unsaved. If unsaved, can re-select. Acceptable; avoid overthinking. |
| 30+ minute gap between disconnect and resume | New session ID/cookies? | Session validity check: if > 30 min, require re-login; treat as new session. This is security/policy, not feature. |

**Implementation notes for Portal HC:**
- Add `LastPageNumber` column to AssessmentSession
- On page navigation (Prev/Next), update `LastPageNumber` in server-side session state
- On exam page load, if returning to existing session, redirect to `?page=LastPageNumber` instead of page 1
- Exam page load must call CheckExamStatus first; if Status==CloseEarly, don't render exam, show results
- Timer on resume: calculate `remaining = ExamWindowCloseDate - NOW()` server-side; pass to client in HTML

---

### 4. Worker Polling for Session Closure (10s poll of CheckExamStatus)

**Standard behavior (required when no WebSockets):**
- Exam page JavaScript runs `setInterval(() => CheckExamStatus(), 10000)`
- CheckExamStatus returns JSON: `{ Status: "InProgress" | "CloseEarly" | "Submitted", ... }`
- If Status == CloseEarly, redirect to `/Results?sessionId=X`
- If Status == Submitted, also redirect

**Edge cases to handle:**

| Edge Case | Behavior | Mitigation |
|-----------|----------|-----------|
| HC closes exam at 14:32:15; worker's poll fires at 14:32:16 | Worker discovers closure 1+ seconds after it happens | Accept. 10s poll interval is intentional trade-off (lower server load). Worker may submit 1-2 more answers before redirection. That's OK; score reflects close time. |
| Worker is on Prev/Next navigation; closure detected mid-save | SaveAnswer in flight, then redirect | Redirect happens after page renders. Potential: answer saved after close time. Acceptable; mark response timestamp and score considers it. |
| Worker closes browser; no more polls | Worker never discovers closure | Acceptable; worker will see "Session closed" when they log back in and try to resume. |
| Poll returns 500 error (server down) | Worker doesn't redirect, assumes exam still open | Retry poll with exponential backoff. After 3 failures, show warning: "Cannot contact exam server. Please refresh page." Don't auto-redirect on poll failure. |
| Multiple redirects in quick succession | Browser history confusing? | Redirect to `/Results` with `replace: true` (history.replaceState) to avoid back-button issues. |
| HC deliberately wants gradual closure (warn, then close) | Polls see "Closing" status vs. "CloseEarly"? | Future feature; out of scope for v2.1. For now, only two states: InProgress or Closed. HC can message workers separately. |

**Implementation notes for Portal HC:**
- Add JavaScript poll loop to exam page: `setInterval(checkStatus, 10000)` where `checkStatus` calls existing `CheckExamStatus` endpoint
- Response must include `Status` field as string
- On Status change to CloseEarly, call `window.location.replace('/Results?sessionId=...')` to redirect
- Poll should gracefully handle network errors; log but don't break exam page
- Clarify in HC UI: "Closing exam is immediate; workers see closure within ~10 seconds"

---

### 5. Live HC Monitoring Dashboard (MonitoringDetail auto-refresh 5-10s)

**Standard behavior (live dashboards across various platforms):**
- HC staff views list of assessment sessions with progress: "15/30 answered"
- Dashboard auto-refreshes every 5-10s without full page reload
- Only the progress column updates; HC's scroll position, filters, sorts are preserved
- Visual indicator shows "last refreshed at 14:32:45"

**Edge cases to handle:**

| Edge Case | Behavior | Mitigation |
|-----------|----------|-----------|
| 100+ concurrent sessions; dashboard queries count(*) for each | Database query explodes; O(n) per refresh | Query once: `SELECT assessment_id, COUNT(*) as answered_count FROM PackageUserResponse WHERE answered IS NOT NULL GROUP BY assessment_id`. Join result with session list. Or use Redis cache invalidated on SaveAnswer. |
| Session just created; no PackageUserResponse rows yet | Shows "0/30 answered" | Correct. Worker hasn't answered yet. No special handling needed. |
| Worker completes exam (200+ answers in 10 rows) during last refresh | Progress jumps 20/30 → 30/30 in one refresh | Correct; no smoothing animation needed. Refresh shows latest state. |
| HC's dashboard still refreshing while they close an exam | Progress column and Status column out of sync | Race condition; acceptable. HC closure is immediate server-side. Next refresh will show Status=CloseEarly. |
| HC opens dashboard, leaves browser tab idle for 1 hour | Poll keeps running, hammering server | Add "pause polling when tab hidden" using Page Visibility API. Resume on tab focus. |
| HC has 200 sessions; full-page reload would take 5s, AJAX refresh takes 100ms | Perceived responsiveness | AJAX refresh is fast; users happy. No special handling needed. |
| New session created while HC dashboard running | Does it appear automatically in list? | No; assume HC refreshes page manually or feature is out of scope. For v2.1, only refresh existing rows. |

**Implementation notes for Portal HC:**
- Create a lightweight GET endpoint: `GetMonitoringProgress?monitoringId=X` returns JSON array of `{ sessionId, answered, total, status }`
- Call every 5-10s from exam monitoring page
- Update only the progress table cells (not full page)
- Use jQuery's `.html()` or `.text()` to update cells with new counts
- Add `data-last-refresh="14:32:45"` to show HC when dashboard last synced
- Optional: Show "3 sessions closed, 2 still in progress" summary line; update on each refresh

---

## MVP Definition

### Phase v2.1 Launch (Minimum Viable for "Resilience & Monitoring")

Must have:

- [x] **Auto-save on click** — Core UX expectation; workers perceive answers lost if not in database. BLOCKING for worker confidence.
- [x] **Auto-save before page nav** — Prevents accidental data loss on Prev/Next. BLOCKING for worker trust.
- [x] **Session resume** — Workers expect to recover exam state after disconnect. BLOCKING for production readiness.
- [x] **Worker polling for closure** — Workers need to know exam is closed; prevents confusion and wasted effort. BLOCKING for HC control.

Should have (high-confidence, add before release):

- [x] **Live HC monitoring dashboard** — HC staff need progress visibility without manual refresh. Enables proactive monitoring. HIGH user value, MEDIUM complexity.

### Add After v2.1 Validation (v2.2+)

- [ ] **Save-before-nav visual feedback** — Enhance UX with progress bars/badges. Nice but not blocking. Can defer.
- [ ] **Graceful closure with worker warning** — Give workers grace period before hard redirect. UX enhancement, adds complexity.
- [ ] **Session timeout auto-extend** — Auto-renew exam window if worker is actively answering. Advanced; defer.

### Future Consideration (v2.5+)

- [ ] **Server-Sent Events (SSE) or WebSockets** — Replace polling for lower latency and server load. Major refactor. Defer until business case (e.g., 1000+ concurrent users).
- [ ] **Real-time answer broadcasting to HC** — Show HC which worker just answered which question. UX differentiation; requires architecture change.
- [ ] **Answer conflict detection** — If worker submits same answer twice (race condition), detect and warn. Out of scope for initial release.

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Phase Dependencies | Priority |
|---------|------------|---------------------|-------------------|----------|
| Auto-save on click | HIGH (core UX) | MEDIUM (debounce logic) | SaveAnswer endpoint | P1 |
| Auto-save before page nav | HIGH (prevents loss) | MEDIUM (promise handling) | SaveAnswer endpoint + Prev/Next button logic | P1 |
| Session resume | HIGH (critical for resilience) | HIGH (timer recalc, state tracking, page nav logic) | AssessmentSession state, LastPageNumber column | P1 |
| Worker polling for closure | HIGH (HC control) | LOW (simple GET loop) | CheckExamStatus endpoint | P1 |
| Live HC monitoring dashboard | HIGH (operational visibility) | MEDIUM (AJAX refresh, query optimization) | PackageUserResponse, new GET endpoint | P1 |

**All 5 features are P1 for v2.1 launch.** None are truly optional; together they form "resilience + monitoring" value proposition.

---

## Competitor Feature Analysis

| Feature | Moodle | Blackboard TestNav | Pearson Assessment | Scrum.org | Our Approach (Portal HC) |
|---------|--------|-------------------|--------------------|-----------|------------------------|
| Auto-save on click | YES; silent | YES; silent with save indicator | YES; auto on selection | YES; auto | Auto-save 500ms debounce + visual indicator |
| Auto-save before nav | YES; required | YES; required | YES; required | YES | Block Prev/Next until SaveAnswer completes; show loading |
| Session resume | YES; up to 30min | YES; on any device | YES; preserves answers + time | YES; server re-opens | Resume to last page, recalc timer from ExamWindowCloseDate |
| Graceful closure | YES; "Quiz closed" warning | YES; "Test completed" redirect | YES; "Exam closed" + results view | YES; session re-open for support | Immediate redirect to Results; future: grace period |
| Live monitoring | Limited (manual refresh) | Dashboard exists but slow | Dashboard with filters | Not applicable | AJAX 5-10s refresh, preserves HC scroll/state |

**Differentiator:** Our monitoring dashboard with fast AJAX refresh + no full-page reload maintains HC UX better than manual refresh. However, not fundamentally different from Blackboard. Competitive parity, not differentiation.

---

## Edge Cases Summary (Risk Matrix)

| Edge Case | Severity | Feature(s) Affected | Mitigation |
|-----------|----------|---------------------|-----------|
| Race condition: answer + closure simultaneous | MEDIUM | Worker polling + auto-save on click | Timestamp check server-side; score includes answered-before-close |
| SaveAnswer timeout (2s) on slow network | MEDIUM | Auto-save on click + before nav | Timeout + retry; alert worker after 3 failures; allow manual submit |
| Timer divergence (HC sees 4:32, worker sees 4:28) | LOW | Session resume + live HC dashboard | Document: client timer is authoritative; HC dashboard shows "time at last save" |
| Poll detects closure mid-navigation | MEDIUM | Worker polling + auto-save before nav | Redirect takes precedence; answer may be saved after close time (acceptable) |
| Resume after > 30 min offline | LOW | Session resume | Require re-login; treat as new session (security policy) |
| Database load on monitoring poll (100+ sessions) | MEDIUM | Live HC monitoring dashboard | Optimize query; use Redis cache or materialized view; monitor performance |

---

## Sources

- [Smart & Secure - Best Web-Based Online Exam Software in 2025 - BlinkExam](https://blinkexam.com/blog/web-based-online-exam-software/)
- [Top 10 Features of Online Examination System in 2026 - SoftwareSuggest](https://www.softwaresuggest.com/blog/features-of-online-examination-software-system/)
- [Modern Online Examination System Features for 2026 - MeritTrac](https://merittrac.com/blogs/10-must-have-features-of-a-modern-online-examination-system-in-2026/)
- [My Assessment Session was Disconnected. How Can I Resume? - Scrum.org](https://www.scrum.org/support/my-assessment-session-was-disconnected-how-can-i-resume)
- [Resume a Test - Pearson Assessment Support](https://support.assessment.pearson.com/display/PAsup/Resume+a+Test)
- [Implementing Efficient AutoSave with JavaScript Debounce Techniques - Medium](https://kannanravi.medium.com/implementing-efficient-autosave-with-javascript-debounce-techniques-463704595a7a)
- [A Guide to Implementing Auto‑Save Functionality on a Form - DhiWise](https://www.dhiwise.com/post/implementing-auto-save-on-forms)
- [WebSocket vs HTTP Polling: Enterprise Comparison - Lightyear](https://lightyear.ai/tips/websocket-versus-http-polling)
- [WebSockets vs Long Polling: From Legacy to Real-Time - WebSocket.org](https://websocket.org/comparisons/long-polling/)
- [How to refresh webpage data without reloading whole page - Daniweb](https://www.daniweb.com/digital-media/ui-ux-design/threads/416175/how-to-refresh-webpage-data-without-reloading-whole-page)
- [Long Polling in Spring MVC - Baeldung](https://www.baeldung.com/spring-mvc-long-polling)
- [Understanding Short Polling, Long Polling, Server Sent Events and Web Sockets - DEV Community](https://dev.to/shameel/understanding-short-polling-long-polling-server-sent-events-and-web-sockets-20kh)

---

**Research completed:** 2026-02-24
**Next phase:** Implement auto-save features (v2.1 Phase 35-36), validate edge cases with real worker/HC testing
