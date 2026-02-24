# Feature Landscape: Exam Resilience & Live Monitoring

**Domain:** Online assessment/exam systems (ASP.NET Core 8.0 MVC, Portal HC KPB v2.1)
**Existing context:** Paged exams (10q/page), countdown timer, per-user shuffled packages, Close Early feature, 4-state worker status
**Researched:** February 24, 2026
**Confidence:** MEDIUM (ecosystem verified via multiple platforms; some edge cases from WebSearch only)

---

## Executive Summary

Online assessment platforms follow converging patterns for resilience and monitoring. Auto-save is table stakes (every 30-120s with visual confirmation), session resume is expected UX (preserve state + time remaining), exam invalidation is a silent or soft-interrupt (not alarming), and live progress monitoring emphasizes clarity over data density. The features are interdependent: auto-save enables resume, resume requires live invalidation detection, and HC monitoring feeds Close Early decisions.

---

## Feature 1: Auto-Save Answers

### Table Stakes Behavior

**What persists on every save cycle (30-120s intervals):**

| State | Why Critical | Implementation Notes |
|-------|-------------|----------------------|
| **Selected option per question** | Losing answers = test failure | Save on click (immediate) + periodic (background) |
| **Current page position** | Multi-page tests require position memory | Required for resume from correct page |
| **Question sequence order** | Shuffled packages need ordering preserved | Essential for resume consistency |
| **Timestamp of last save** | Verify data freshness, detect stale state | Use server timestamp (not client) |
| **Answer state (draft vs submitted)** | Distinguish "I picked this" from "I haven't chosen yet" | Per-question status tracking |

**NOT table stakes (but useful):**
- Full browser state snapshots
- Keystroke-level granularity
- Every keystroke in essay fields (cost-benefit poor)

### Visual Feedback to Worker

**Best-practice pattern from UX research:**

1. **Immediate feedback on user action:**
   - Click an option → instant visual change (radio selected, checkbox marked)
   - No wait, no modal

2. **Background save indicator (non-intrusive):**
   - **While saving:** Small inline indicator near question (e.g., "Saving..." in grey text, 1-2 seconds)
   - **After save:** Brief confirmation toast (2-3s auto-dismiss)
     - Text: "Answer saved" (positive, not negative "unsaved")
     - Color: Green (success semantic)
     - Position: Top-right or bottom-right (avoid blocking question)
   - **Persistent state:** "Saved just now" / "Saved 2 min ago" timestamp visible below submit button or in question header

3. **Error state (rare but critical):**
   - Network failure: Show warning toast "Unable to save answer - reconnecting..."
   - Persist state locally + retry server save every 5-10s
   - Do NOT discard unsaved answer (can recover on reconnect)

4. **Per-page save (form-level):**
   - When clicking "Next Page": Show toast "Page saved" before transitioning
   - Reassures worker that multi-part pages are atomic

### Answer State Persistence Requirements

**Fields to save per question:**

```json
{
  "question_id": "UUID",
  "selected_option_id": "UUID | null",
  "answer_text": "string",
  "answer_timestamp": "ISO8601",
  "page_number": "int",
  "position_on_page": "int"
}
```

**Backend save strategy:**
- Per-click immediate save (AJAX POST, non-blocking)
- Debounced periodic save every 30-60s (catches any missed clicks)
- Moodle/Canvas standard: 30-120s intervals (use 60s as default)

### Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| **Network drops, answer unsaved** | Keep answer selected locally, queue save retry, show "reconnecting" state | Don't lose work; recover silently when online |
| **Page refresh** | Restore page position + answers from server state (session resume) | Worker shouldn't lose place |
| **Browser tab close** | Answers auto-saved to server are preserved; unsaved answers lost (expected) | Accept loss, but minimize window |
| **Offline mode** | If offline > 30s, show warning "You are offline - answers may not save"; local cache helps but not guaranteed | Honest about offline risk |
| **Rapid option changes** | Only save final selection (debounce successive clicks) | Avoid save spam for nervous test-takers |

### Dependency on Existing Features

- **Countdown timer:** Must coordinate with save timestamps to preserve correct remaining time
- **Shuffled package assignment:** Must save original shuffle order + position to resume consistently

---

## Feature 2: Session Resume

### Ideal UX Pattern

**When worker returns after disconnect:**

Choose one approach (recommend Option A for clarity):

#### **Option A: Explicit Welcome-Back Modal (RECOMMENDED)**

**Trigger:** On login after disconnect, detect session exists with InProgress status.

**Modal content:**
```
┌─────────────────────────────────────┐
│  Welcome back, [Worker Name]!       │
├─────────────────────────────────────┤
│  You left off on Question 23        │
│  (Page 3 of 10)                     │
│                                     │
│  Time remaining: 42 minutes         │
│                                     │
│  [Resume Exam] [View Score]         │
│  (if already force-closed)          │
└─────────────────────────────────────┘
```

**Why this:**
- Explicit and trustworthy (shows worker where they were)
- Avoids confusion ("Did I already submit this?")
- Clear action button prevents accidental restart
- Time remaining is critical context (workers need to know if panic-worthy)

#### **Option B: Silent Resume (Less Recommended)**

- Redirect automatically to last page + question
- Auto-scroll to last answered question
- Assumes worker recognizes they're mid-exam
- Risk: Worker thinks they've restarted and panics

#### **Option C: Resume from Q1 with Saved Answers**

- Start at first question but show all previous answers
- Worker must click through to reach their stopping point
- Adds friction; only use if compliance/audit needs full re-review

### Resume Requirements

**What must be restored:**

| Element | Source | Behavior |
|---------|--------|----------|
| **Page number** | Last saved position | Land on page X (not Q1) |
| **Question position** | Last saved position | Scroll to last-answered question |
| **All saved answers** | Answer table | Display selected options/text as read-only or editable (platform choice) |
| **Remaining time** | Timer at last save + (now - last_save_timestamp) | Recalculate, show updated countdown |
| **Question order** | Original shuffle seed | Restore exact same sequence (don't re-shuffle) |

**What NOT to restore:**
- Page scroll position (user will re-read answers)
- Keystroke history (only final answers matter)

### Connection Timeout Behavior

**Auto-reconnect pattern (from exam platform standards):**
- Detect disconnect after 5s of no server contact
- Show overlay: "Reconnecting..." with spinner
- Retry every 5s for up to 2 minutes
- If reconnect fails after 2 min: Show modal "Connection failed - Refresh page to resume"
- Worker clicks refresh → back to resume modal

### Dependency on Existing Features

- **Auto-save:** Must have saved state to resume from
- **Countdown timer:** Timer must persist across disconnect/resume
- **Worker status:** InProgress must be retrievable to trigger resume logic

---

## Feature 3: Exam Invalidation Polling (Worker Side)

### Detection & UX Pattern

**What triggers invalidation:** HC closes exam while worker is mid-exam (Close Early feature).

**Worker experience (recommended UX):**

#### **Approach: Soft Modal + Auto-Redirect**

1. **Polling mechanism:** Worker polls exam status every 5-10s (or WebSocket push if available)
2. **On invalidation detected:**
   ```
   ┌──────────────────────────────────┐
   │  Exam Closed                     │
   │                                  │
   │  This exam has been closed       │
   │  by your instructor.             │
   │                                  │
   │  Your answers have been saved    │
   │  and will be scored.             │
   │                                  │
   │  Redirecting in 3 seconds...     │
   │  [View Results Now]              │
   └──────────────────────────────────┘
   ```
3. **Auto-redirect timeline:**
   - Show modal for 3-5 seconds (gives worker time to read)
   - Auto-redirect to results page OR
   - Auto-refresh to show score (if score-on-submit enabled)

#### **Why NOT immediate redirect:**
- Disorienting (worker is mid-question)
- Doesn't explain what happened
- Workers think exam crashed

#### **Why NOT just toast:**
- Too easy to miss/dismiss
- Exam close is a major event, not a minor notification
- Requires acknowledgment

### Polling Implementation Details

**Frequency:** 5-10s intervals (balance responsiveness vs server load)
- Start at 10s, reduce to 5s if worker idle (reduce noise)
- Consider WebSocket or Server-Sent Events (SSE) if available for real-time

**Fallback if no polling:** Worker sees result page is now accessible when they finish or click Submit

**Edge case:** If worker submits answers during Close Early window:
- Let submission complete (few seconds of race possible)
- Server-side logic: Score submitted answers regardless of Close Early timestamp
- Worker sees their scored result

### Dependency on Existing Features

- **Close Early (already built):** This feature depends on Close Early existing
- **Answer auto-save:** Ensures answers are persisted before invalidation arrives
- **Worker status transitions:** Must support InProgress → Completed transition without explicit submission

---

## Feature 4: Live Progress Monitoring (HC Dashboard)

### Standard Monitoring Columns

**Core columns (table stakes) — refreshed every 5-10s:**

| Column | Data | Why Valuable | Format |
|--------|------|-------------|--------|
| **Worker Name** | Assigned worker ID/name | Identify who you're monitoring | Text |
| **Status** | InProgress / Abandoned / Completed / Force-Closed | Quick health check | Status badge (color-coded) |
| **Progress %** | Questions answered / Total questions | "Are they done yet?" | 45/100 (45%) or bar chart |
| **Current Question** | Q number (e.g., "Q23") or "Q18 (Page 3/10)" | Where they are now | Text or page indicator |
| **Last Activity** | Timestamp of last answer save (e.g., "2 min ago") | Detect stalled workers (AFK/disconnected) | Relative time |
| **Remaining Time** | Countdown (e.g., "15:42") or "--:--" if over | Know if time pressure is real | MM:SS format |
| **Online Status** | Online (green dot) / Offline (red dot) | Distinguish "stuck" from "disconnected" | Icon + color |

**Optional columns (nice-to-have, add if space allows):**

| Column | When Use |
|--------|----------|
| **Device/Browser** | For troubleshooting (e.g., "Chrome on Windows") |
| **Session Duration** | How long they've been taking exam |
| **Connection Quality** | Signal strength indicator (if monitoring network) |

### HC Actions from Dashboard

**What HC typically wants to do while monitoring:**

1. **Identify struggling worker** → Click worker row → See detail panel or open chat
2. **Send message** → "Are you OK? Having issues?" → Worker sees inline notification
3. **Force close this worker's exam** → Click "Close" button on row → Immediate invalidation signal
4. **Extend time** (if supported) → Modal to add minutes → Timer updates live
5. **Export progress** → Download CSV after exam closes

### Refresh Frequency

- **Default:** 5-10s polling (balance UX responsiveness vs server load)
- **Rationale:**
  - Too fast (< 2s): Server overhead, browser CPU spike
  - Too slow (> 15s): HC sees stale data, "last activity" is meaningless
  - 5-10s is sweet spot (matches web poll standard, user doesn't notice 5s lag)

### Real-time vs Polled

**Polling (simpler, recommended for v2.1):**
- HC page refreshes data every 5-10s via AJAX
- Simple implementation, no WebSocket infrastructure
- HC must refresh manually or accept 5-10s latency

**Real-time (WebSocket/SSE, future enhancement):**
- Server pushes updates to HC as workers answer
- Lower latency, but requires bidirectional connection
- Defer to v2.2

### Layout Recommendation

```
┌─────────────────────────────────────────────────────┐
│ Exam Progress Monitor [Auto-refresh: 5s]            │
├─────────────────────────────────────────────────────┤
│ Worker          Status      Progress    Current    │
│ Name                        %/Total     Q#  Time   │
├─────────────────────────────────────────────────────┤
│ Alice Smith     InProgress  45/100 45%  Q23 14:32  │
│ Bob Johnson     InProgress  30/100 30%  Q15 23:10  │
│ Carol Davis     Completed   100/100     --  --     │
│ Dan Wilson      Abandoned   12/100      Q4  --     │
│ Eve Martinez    InProgress  70/100 70%  Q34  8:15  │
└─────────────────────────────────────────────────────┘
```

### Dependency on Existing Features

- **Auto-save:** Must persist answers so progress % is accurate
- **Countdown timer:** Must sync with worker session timer
- **Worker status:** Requires accurate status state transitions
- **Close Early:** HC uses monitoring data to decide when to close

---

## Feature Dependencies & Sequencing

```
Auto-Save (foundation)
    ↓
Session Resume (depends on auto-save state)
    ↓
Exam Invalidation Polling (depends on auto-save + resume)
    ↓
Live Progress Monitoring (depends on all three + auto-save accuracy)
```

**Why this order:**
1. **Auto-save first:** No other features work without persistent answer state
2. **Resume second:** Requires auto-save data to restore
3. **Invalidation third:** Needs auto-save to preserve answers before Close Early
4. **Monitoring last:** Can be added independently but becomes valuable only after first three exist

---

## Table Stakes vs Differentiators

### Table Stakes (Must-Have)

| Feature | Why Mandatory |
|---------|---------------|
| **Auto-save with visual feedback** | Worker expects no answer loss; exam platform baseline standard |
| **Session resume with time recalculation** | Disconnect is inevitable; workers expect to continue, not restart |
| **Soft invalidation modal (not silent)** | Clear communication on exam close (compliance + UX) |
| **HC monitoring dashboard with progress %** | Core supervision requirement; HC needs visibility to manage exam |
| **Last activity timestamp** | Detect stalled/offline workers; diagnostic essential |

### Differentiators (Nice-to-Have)

| Feature | Competitive Value |
|---------|-------------------|
| **Per-question save indicator (animated checkmark)** | Reassures anxious test-takers; Moodle/Canvas don't do this elegantly |
| **"Saved 2 min ago" persistent badge** | Trust-builder; shows save continuity |
| **Color-coded online/offline status** | At-a-glance HC awareness; requires extra connection tracking |
| **Auto-extend time on network reconnect** (add back lost seconds) | Fairness feature; expensive to implement correctly |
| **Real-time WebSocket updates** | Reduce monitoring latency from 5-10s to < 500ms |

### Anti-Features (Explicitly Don't Build)

| Feature | Why Avoid |
|---------|-----------|
| **Keystroke-level auto-save** | Performance cost > benefit; only final answers matter |
| **Save on every keystroke in essay** | Server spam; 99% of platforms use per-paragraph or per-field |
| **Browser tab-switch detection/blocking** | Creepy; creates compliance/privacy issues; Respondus does this (not table stakes) |
| **Immediate silent redirect on exam close** | Disorienting; worse UX than modal + delay |
| **30+ columns in HC monitoring** | Info overload; diminishing returns after 6-8 core columns |
| **Manual per-worker time extension UI (mid-exam)** | Scope creep; edge case; defer to manual admin action post-exam |

---

## MVP Recommendations

### Phase 2.1 Scope (Auto-Save + Resume)

**Prioritize:**
1. Auto-save answers (selected option + page position) every 60s + on-click
2. Toast visual feedback ("Answer saved" 2-3s auto-dismiss)
3. Session resume modal with time recalculation
4. Exam invalidation polling (5-10s, soft modal on close detect)

**Why this order:**
- Auto-save + Resume are interdependent; must ship together
- Invalidation polling is lightweight addition (builds on auto-save)
- These three provide solid resilience foundation

### Phase 2.2 Scope (HC Monitoring)

**Prioritize:**
1. Live progress monitoring dashboard (6 core columns)
2. 5-10s polling refresh
3. Worker detail panel on click

**Why separate:**
- Doesn't block worker features
- Can be added after resilience features are stable
- Gives time to verify auto-save accuracy before exposing to HC

### Deferred (v2.3+)

- Real-time WebSocket updates
- Per-question animated save indicators
- Time-extension UI
- Connection quality indicators

---

## Implementation Edge Cases & Gotchas

### High-Risk Pitfalls

| Pitfall | Why Costly | Mitigation |
|---------|-----------|-----------|
| **Timestamp mismatch (client vs server)** | Resume calculates wrong remaining time | Always use server timestamp for "now", never client clock |
| **Shuffle seed not saved/restored** | Resume shows questions in different order = confusion + wrong answers | Must save shuffle seed with session state |
| **Auto-save on page navigation loses answers** | Worker clicks "Next" before save completes = answer lost | Queue unsaved answers, don't allow navigation until server ACKs save |
| **Duplicate saves on fast clicks** | Race condition: two saves of same Q in quick succession | Debounce saves (ignore duplicate ID within 500ms window) |
| **Invalidation polling doesn't detect Close Early** | Worker keeps testing unaware = admin confused | Polling must check status every 5-10s; no exceptions |
| **HC dashboard shows stale data** | Monitor sees 15-min-old progress (worker already done) | Max 10s refresh window, auto-refresh if HC page is open |

### Moderate Issues

| Issue | Risk | Mitigation |
|-------|------|-----------|
| **Essay answer save takes 2s on slow network** | Worker thinks answer was lost, clicks again | Show "Saving..." state for > 1s; disable click during save |
| **Resume logic forgets last page** | Worker lands on Q1 instead of Q45 | Store page_number in session state, not just answer count |
| **"Saved" toast overlaps with question text** | Hard to read | Position toast in bottom-right or use CSS z-index management |
| **No indication worker is offline until next save attempt** | Worker doesn't know connection dropped | Show offline banner at top of page if no server contact > 30s |

---

## Conformance to Existing Patterns

### Alignment with Close Early Feature (Already Built)

- **Close Early scores from saved answers:** Auto-save makes this accurate
- **Worker gets scored immediately:** Resume feature makes it clear when exam is closed
- **HC sees worker status:** Monitoring dashboard feeds Close Early decision-making

### Alignment with Paged Exams (Already Built)

- **10 questions per page:** Auto-save must track page position
- **Multi-page navigation:** Session resume must restore to correct page
- **Shuffled per-user:** Resume must restore original shuffle order

---

## Visual Reference: Mockup Descriptions

### Auto-Save Toast (Success State)
```
┌──────────────────┐
│ ✓ Answer saved   │  (green, 2-3s auto-dismiss)
└──────────────────┘
```

### Session Resume Modal
```
┌──────────────────────────────┐
│  Welcome back, Alice Smith!  │
│                              │
│  You left off on:            │
│  Question 23 (Page 3 of 10)  │
│                              │
│  Time remaining: 42:15       │
│                              │
│  [Resume] [View Results]     │
└──────────────────────────────┘
```

### Exam Close Modal
```
┌──────────────────────────────┐
│  Exam Closed                 │
│                              │
│  This exam has been closed   │
│  by your instructor.         │
│                              │
│  Your answers have been      │
│  saved and will be scored.   │
│                              │
│  Redirecting in 3 seconds... │
│  [View Results Now]          │
└──────────────────────────────┘
```

### HC Monitoring Dashboard
```
Worker              Status          Progress    Current   Remaining
─────────────────────────────────────────────────────────────────
Alice Smith         ● InProgress    45/100      Q23       14:32
Bob Johnson         ● InProgress    30/100      Q15       23:10
Carol Davis         ✓ Completed     100/100     --        --
Dan Wilson          ⊗ Abandoned     12/100      Q4        --
Eve Martinez        ● InProgress    70/100      Q34       8:15

(● = Online indicator, ✓ = Completed, ⊗ = Abandoned)
Auto-refresh: 5s
```

---

## Confidence Assessment

| Area | Level | Notes |
|------|-------|-------|
| **Auto-save requirements** | HIGH | Moodle/Canvas/exam.net all follow same pattern; verified across multiple sources |
| **Session resume UX** | MEDIUM | Standards are clear; specific modal wording varies by platform (recommendations are composite) |
| **Invalidation polling** | MEDIUM | Polling mechanism is standard; exact timing (5-10s) is inference from proctoring tools, not verified in docs |
| **HC monitoring columns** | MEDIUM | Core columns verified (AssessPrep, ProgressLearning docs); optional columns are inferred |
| **Visual feedback patterns** | HIGH | Toast/indicator UX is verified from design system standards (Carbon, Pajamas); auto-save patterns confirmed |

---

## Research Gaps & Phase-Specific Questions

**For Phase 2.1 Requirements Writing:**
- Will you support pause/resume (or only disconnect/resume)? Affects timer behavior.
- Should incomplete answers be autosaved, or only submitted answers? (Recommendation: all selections, even partial)
- What's the max acceptable latency for answer save? (Recommendation: < 2s)
- Should "Saved X min ago" timestamp be visible continuously or only on hover?

**For Phase 2.2 Requirements Writing:**
- Should HC be able to add time mid-exam, or only view remaining time?
- Should worker see a notification when HC sends a chat message, or silent?
- Do you want per-question mastery indicators in HC monitoring (e.g., % correct per category)?

---

## Sources

### Auto-Save Functionality & Patterns
- [Auto-Save in CBT Test Software for Better Online Exams](https://thinkexam.com/blog/how-auto-save-features-in-cbt-test-software-enhance-exam-integrity-and-user-experience/)
- [Modern Online Examination System Features for 2026 | MeritTrac](https://merittrac.com/blogs/10-must-have-features-of-a-modern-online-examination-system-in-2026/)
- [Moodle Quiz Settings - Quiz Auto Save](https://docs.moodle.org/501/en/Quiz_settings)

### Session Resume & Reconnection UX
- [My Assessment Session was Disconnected. How Can I Resume? | Scrum.org](https://www.scrum.org/support/my-assessment-session-was-disconnected-how-can-i-resume)
- [Canvas for Students: Resume a Quiz that I Already Started Taking | IT@UMN](https://it.umn.edu/services-technologies/how-tos/canvas-students-resume-quiz-i-already)
- [Auto client reconnect policy settings | Citrix Reference](https://docs.citrix.com/en-us/xenapp-and-xendesktop/7-15-ltsr/policies/reference/ica-policy-settings/auto-client-reconnect-policy-settings.html)

### Live Progress Monitoring
- [Monitoring Students using the Live Invigilation Dashboard – AssessPrep](https://assessprep.zendesk.com/hc/en-us/articles/4841247686033-Monitoring-Students-using-the-Live-Invigilation-Dashboard)
- [How do I use Live Monitoring? - Progress Learning](https://help.progresslearning.com/article/r64o44dacy-how-do-i-use-live-monitoring)
- [Live Monitoring Feature - Code.org](https://support.code.org/hc/en-us/articles/115000693231-Viewing-student-progress)

### Visual Feedback & Notifications
- [Indicators, Validations, and Notifications: Pick the Correct Communication Option - NN/G](https://www.nngroup.com/articles/indicators-validations-notifications/)
- [Toast UI Design: Best practices, Design variants & Examples | Mobbin](https://mobbin.com/glossary/toast)
- [What is a toast notification? Best practices for UX - LogRocket Blog](https://blog.logrocket.com/ux-design/toast-notifications/)
- [Autosave design pattern - UI Patterns](https://ui-patterns.com/patterns/autosave)
- [Saving and feedback | Pajamas Design System](https://design.gitlab.com/patterns/saving-and-feedback/)

### Exam Close & Proctoring Behavior
- [Best Exam Software with Real-Time Monitoring 2026 | GetApp](https://www.getapp.com/education-childcare-software/exam/f/real-time-monitoring/)
- [Exam Monitoring Software – Ensure safe exams | Digiexam](https://www.digiexam.com/exam-monitoring-system)
- [Respondus LockDown Browser - Exiting and Resuming a Quiz/Exam](https://c4e.zendesk.com/hc/en-us/articles/360053598811-Respondus-LockDown-Browser-Exiting-and-Resuming-a-Quiz-Exam)

### Network Disconnection Handling
- [Network Connection Lost (Autosave Failed) - Online Exams Common Issues](https://it.ajman.ac.ae/docs/network-connection-lost-autosave-failed)
- [If the Internet connection to the LMS server is lost during an exam, how does that affect the video? – Respondus Support](https://support.respondus.com/hc/en-us/articles/4409607196059-If-the-Internet-connection-to-the-LMS-server-is-lost-during-an-exam-how-does-that-affect-the-video)
- [Exam Troubleshooting Guide – ExamSoft](https://support.examsoft.com/hc/en-us/articles/12155838507277-Exam-Troubleshooting-Guide)

### Activity Tracking & Timestamps
- [How does exam monitoring software work? | Digiexam](https://www.digiexam.com/exam-monitoring-software)
- [Tracing Online Exam Proctoring System Workflows with OpenTelemetry](https://oneuptime.com/blog/post/2026-02-06-trace-exam-proctoring-workflows-opentelemetry/view)
