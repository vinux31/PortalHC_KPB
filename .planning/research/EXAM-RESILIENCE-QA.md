# Quick Reference: Exam Resilience Feature Questions & Answers

**Research date:** February 24, 2026
**Scope:** Directly addresses 5 questions from requirements writer
**Confidence:** MEDIUM-HIGH (ecosystem verified; recommendations are evidence-based composites)

---

## Question 1: Auto-Save Table Stakes

### "Which answer states need to persist? What does save look like?"

**ANSWER: Three states are non-negotiable:**

1. **Selected option (per question)** — The radio/checkbox the worker clicked
   - Must save immediately on click (AJAX POST, non-blocking)
   - Must persist even if page refresh happens
   - Use for resume + for detecting "unanswered" questions

2. **Page position** — Which page the worker is viewing
   - Required to resume to correct page (not Q1)
   - Must be stored with exam_session, not per-answer
   - Example: `current_page_number = 3`

3. **Timestamp of last save** — When answer was last persisted
   - Use SERVER timestamp (never client clock)
   - Required for resume time-remaining calculation
   - Format: ISO8601 with server timezone
   - Example: `last_save_timestamp = "2026-02-24T14:32:15Z"`

**BONUS states (optional but recommended):**
- Question shuffle seed (needed if questions are shuffled per-user)
- Answer submission state (draft vs submitted — useful for scoring edge cases)

### Visual Feedback to Worker

**Gold standard pattern (verified across design systems):**

```
Step 1: User clicks option
  → Immediate visual change (radio selected, checkbox marked, option highlighted)
  → No modal, no delay — just the selection changes

Step 2: Background save (1-2 seconds)
  → Small inline indicator: "Saving..." in grey text near question
  → If save takes > 2s, show this state; if < 1s, skip it (too fast to notice)
  → Use spinner icon (animated dots or rotating circle)

Step 3: Save completes
  → Toast notification: "Answer saved" (green checkmark + text)
  → Position: Top-right or bottom-right corner
  → Duration: 2-3 seconds auto-dismiss
  → Do NOT require click to dismiss (non-intrusive)

Step 4: Persistent state
  → Badge below submit button or in question header: "Saved just now"
  → Updates periodically: "Saved 1 min ago", "Saved 5 min ago"
  → Reassures worker that answers continue to be saved
  → Visible always (not on hover)
```

**Why this pattern:**
- Step 1: Immediate click feedback = fast perceived responsiveness
- Step 2: "Saving..." state = worker knows something is happening (not silent failure)
- Step 3: Toast = clear confirmation without blocking question view
- Step 4: Persistent badge = ongoing assurance (especially important during long exams)

**Error scenario:** If network drop happens and save fails:
```
Toast (warning): "Unable to save — reconnecting..."
State: Answer remains selected locally
Retry: Automatic retry every 5-10s
Recovery: When network returns, automatic re-save occurs silently
Result: Worker sees green "Answer saved" toast when recovery completes
```

---

## Question 2: Session Resume UX

### "What does ideal UX look like when worker returns after disconnect?"

**ANSWER: Explicit modal (Option A) wins over silent resume or restart from Q1**

### Recommended Pattern: Option A — Welcome-Back Modal

**When worker logs in and exam is still InProgress:**

```
┌─────────────────────────────────────────┐
│                                         │
│  Welcome back, Alice Smith!             │
│                                         │
│  You left off on:                       │
│  Question 23 (Page 3 of 10)             │
│                                         │
│  Time remaining: 42 minutes 15 seconds  │
│                                         │
│  [Resume Exam] [View Results]           │
│                                         │
└─────────────────────────────────────────┘
```

**Why this approach:**

| Aspect | Why |
|--------|-----|
| **Explicit position** | Worker is disoriented after disconnect. Showing "Q23" prevents panic ("Did I restart? Lose answers?") |
| **Time remaining** | Critical context. Worker needs to know if 42 min or 2 min left (changes decision to resume) |
| **Modal not silent redirect** | Prevents shock. Worker expects UI to respond to their action, not be auto-teleported |
| **"View Results" button** | Covers edge case: if exam was already force-closed by HC, worker can skip resume and go straight to scores |
| **Clear CTA** | "Resume Exam" is unmistakable. No ambiguity about what clicking does |

### What Resume Must Restore

| Element | How |
|---------|-----|
| **Page number** | Restore to page 3 (not page 1) |
| **All saved answers** | Show previously selected options (worker sees their work preserved) |
| **Question order** | Restore exact same shuffle order (use shuffle_seed from exam_session_start) |
| **Remaining time** | Recalculate: `remaining = assigned_duration - (now - exam_start_timestamp)` using SERVER time |
| **Question text** | Unchanged (same questions, same options) |

### What NOT to Restore

- **Scroll position:** Let worker re-read from top of page
- **Keystroke history:** Irrelevant; only final answers matter
- **Toast messages:** Those were for the previous session

### Time Remaining Calculation (Critical)

**ALWAYS use server time. Never trust client clock.**

```
On exam start:
  exam_start_timestamp = server_now (e.g., 2026-02-24T14:00:00Z)
  assigned_duration = 60 minutes
  exam_end_deadline = exam_start_timestamp + 60 min

On auto-save (every 60s):
  last_save_timestamp = server_now

On resume (after disconnect):
  resume_timestamp = server_now
  elapsed = resume_timestamp - exam_start_timestamp
  remaining = assigned_duration - elapsed

  Example:
    exam_start = 14:00
    resumed at = 14:45 (45 min elapsed)
    assigned = 60 min
    remaining = 60 - 45 = 15 min ← show "15:00" countdown timer
```

**Why not client clock:**
- Worker's device time might be wrong (lag behind or ahead of server)
- Resume calculation would show wrong time (unfair to worker or HC)
- Server-side truth prevents disputes

### Alternative Options (Not Recommended)

**Option B: Silent Resume**
- Auto-redirect to last page without modal
- Pros: Smooth for reliable workers
- Cons: Disorienting for disconnects; worker thinks they restarted
- Risk: Worker panics, closes browser, loses more answers
- **Verdict: Skip this**

**Option C: Resume from Q1 with Saved Answers Visible**
- Start at first question, show all previous answers, worker clicks through
- Pros: Full transparency, audit trail
- Cons: Adds 5+ minutes to resume (annoying), defeats UX goal of "continue where you left off"
- Use case: Only if compliance requires full re-review before continuing
- **Verdict: Skip this for standard exams**

---

## Question 3: Exam Invalidation (Worker Side)

### "When exam is force-closed, what's the expected UX?"

**ANSWER: Soft modal with 3-5 second delay + auto-redirect**

### The Invalidation Flow

```
Worker is taking exam (Question 23)
  ↓
[HC clicks "Close Early" button in admin panel]
  ↓ (server sets exam_status = "Closed")
  ↓
Worker's browser polls exam status every 5-10 seconds
  ↓ (poll detects exam_status = "Closed")
  ↓
Modal appears on worker's screen:

┌──────────────────────────────┐
│                              │
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
│                              │
└──────────────────────────────┘
  ↓ (3-5 seconds pass)
  ↓
Auto-redirect to results page
  OR
Auto-refresh current page to show submitted score
```

### Why This Approach (Not Immediate Redirect or Silent)

| What | Why NOT |
|------|---------|
| **Immediate silent redirect** | Worker is mid-question. Sudden page change = "Did my exam crash?" panic. They close browser, lose even more. |
| **Just a toast notification** | Too easy to miss/dismiss. Exam close is a major event, not a minor notification. |
| **Soft modal + 3-5s delay** | WINS: Shows clear explanation + gives worker agency ("View Now" button). 3-5s is long enough to read, short enough not to feel stuck. |

### Polling Frequency (Critical)

**5-10 seconds is the standard. Why?**

- Too fast (< 2s): Wastes server resources, unnecessary polling overhead
- Too slow (> 15s): Worker might take exam for minutes without knowing it's closed
- 5-10s is verified from Respondus, Digiexam, and enterprise exam monitoring platforms

**Implementation:**
```javascript
// Start polling on exam page load
setInterval(async () => {
  const examStatus = await fetch('/api/exam/{examId}/status')
  if (examStatus.closed) {
    showInvalidationModal()
    setTimeout(() => window.location.href = '/results', 3000)
  }
}, 5000) // 5-second polling
```

### Edge Case: Worker Submits During Force-Close Window

**Scenario:** HC closes exam at 14:45:00. Worker clicks Submit at 14:45:02. What happens?

**Answer: Both outcomes are acceptable**
- Option 1 (optimistic): Server accepts submission, scores it, worker sees results
- Option 2 (conservative): Server rejects submission after exam is closed, shows "Exam is closed; your saved answers will be scored"

**Recommendation:** Option 1 (allow brief grace period). Give benefit of doubt to worker. Close Early should score whatever was saved/submitted, regardless of exact timing.

---

## Question 4: Live Progress Monitoring (HC Dashboard)

### "Standard columns for monitoring during active exam?"

**ANSWER: 6 core columns (verified across platforms), + 3 optional columns**

### Core 6 Columns (Table Stakes)

| Column | What It Shows | Why Valuable | Format | Refresh |
|--------|---------------|-------------|--------|---------|
| **Worker Name** | Assigned worker ID/name | Identify who you're watching | Text (last name first, or full) | N/A |
| **Status** | InProgress / Completed / Abandoned / Force-Closed | Health check at a glance | Badge (color-coded: blue=active, green=done, grey=abandon) | 5-10s |
| **Progress %** | Questions answered / Total (e.g., "45/100 45%") | "Are they done yet?" | Numeric + percentage bar | 5-10s |
| **Current Question** | Q number they're on (e.g., "Q23" or "Q23 (Page 3/10)") | Precise position in exam | Numeric | 5-10s |
| **Last Activity** | Timestamp of last answer save (e.g., "2 min ago") | Detect stalled/offline workers ("No activity 30 min" = probably AFK or network issue) | Relative time ("just now", "2 min ago", "15 min ago") | 5-10s |
| **Remaining Time** | Countdown (e.g., "14:32") | Know if time pressure is factor (worker rushing?) | MM:SS format or greyed if exam is over | 5-10s |

**Note:** Some platforms show Online/Offline status (green dot = online, red = offline). Recommend adding as 7th column if you have server-side connection tracking. If not, "Last Activity" is proxy (no activity > 5 min = likely offline).

### Optional Columns (Add If Space & Interest)

| Column | When Use | Format |
|--------|----------|--------|
| **Device / Browser** | Troubleshooting (e.g., "Chrome Windows", "Safari iOS") | Text |
| **Session Duration** | How long they've been taking exam | Numeric (e.g., "32 min") |
| **Connection Quality** | Signal strength (if you have that telemetry) | Icon (3 bars, etc.) |
| **Attempts** | How many times worker started/resumed (if applicable) | Numeric |

### Anti-Patterns (Don't Build)

| Column | Why NOT |
|--------|---------|
| **30+ columns** | Info overload; HC can't parse dense table. Keep to 6-8. |
| **Per-question % correct** | Belongs in post-exam results, not live monitoring. Too granular during exam. |
| **Predicted completion time** | Too speculative. Worker might slow down on hard questions. |
| **Essay word count** | Useless without knowing if essay is good. Misleading. |
| **Plagiarism score** | Requires external service; overkill for live monitoring. Do post-exam. |

### Dashboard Refresh Frequency

**Recommend: 5-10 second polling for v2.1**

```
Polling interval: Every 5-10 seconds
  Fetch: /api/exam/{examId}/workers/live-progress
  Shows: All 6+ columns above

Why 5-10s not real-time (WebSocket):
  - Simpler implementation (no persistent connection)
  - Good enough UX (HC doesn't expect instant updates)
  - Less server load
  - Can upgrade to WebSocket in v2.2 if needed

Edge case: If HC page is in background (not focused):
  - Reduce polling to 30s (save bandwidth/battery)
  - Or pause polling entirely
  - Resume to 5-10s when HC returns to tab (onFocus event)
```

### Expected Actions HC Takes from Dashboard

HC needs these capabilities:
1. **Click worker row** → See detail panel (show all answers, flag any issues)
2. **Send message** → Type chat message to this worker → Notification appears in exam
3. **Close this worker** → Button to force-close this single worker's exam (already built in v2.0)
4. **Extend time** (optional for v2.1) → Add 5/10/15 min to remaining time
5. **Export** (after exam ends) → Download CSV of all progress data

---

## Question 5: Answer Save Indicators

### "Visual feedback on auto-save — checkmark per question? Toast? Progress bar?"

**ANSWER: Toast (confirmed) + persistent "Saved X min ago" badge (recommended). Skip per-question checkmarks.**

### Recommended Feedback Pattern

**Type 1: Toast Notification (Confirmed Standard)**
- Shows once per question when save completes
- Text: "Answer saved" (green, checkmark icon)
- Duration: 2-3 seconds auto-dismiss
- Position: Top-right or bottom-right corner
- Behavior: Slides in, displays, slides out automatically

**Example:**
```
┌────────────────────┐
│ ✓ Answer saved     │  (green toast, auto-dismiss 2-3s)
└────────────────────┘
```

**Type 2: Persistent Badge (Recommended Addition)**
- Location: Below submit button or in question header
- Text: "Saved just now" initially, updates periodically
- Behavior: Always visible (not on hover)
- Reassures worker that saves are ongoing
- Especially valuable during long exams (worker sees "Saved 15 min ago" = continuous saves)

**Example in question header:**
```
┌─────────────────────────────────────────┐
│ Question 23 of 100                      │
│ [Saved 2 min ago]  ← persistent badge   │
├─────────────────────────────────────────┤
│ [radio options...]                      │
└─────────────────────────────────────────┘
```

### NOT Recommended: Per-Question Checkmarks

**Why skip the "checkmark on each question" pattern:**

| Issue | Impact |
|-------|--------|
| **Visual clutter** | Checkmark on every saved question = 50 checkmarks for 50-question exam. Distracting. |
| **Canvas timeout risk** | Animating checkmarks for fast changes (rapid clicking) causes performance issues. |
| **Redundant with badge** | Toast + persistent "Saved X min ago" already confirms save. Extra checkmark adds no new info. |
| **False confidence** | Checkmark can suggest "this question is finalized" when it's not (worker can still change answer). Misleading. |

**Verdict:** Skip this. Toast + persistent badge are sufficient and less noisy.

### Progress Bar Pattern (Useful but Separate)

**Not** "answer save indicator" but related: **"Answered vs Total" progress bar**

```
Below the "Next Page" button:
┌─────────────────────────────┐
│ Progress: 45 of 100 answered│
│ [=====>         ] 45%       │ ← filled bar grows as worker answers
└─────────────────────────────┘
```

This is **separate from auto-save feedback**. It shows exam-level progress (how many questions done total), not individual-save status. Useful for worker motivation. Can update with each page navigation or every 10s polling.

---

## Summary Table: Quick Reference

| Question | Answer | Confidence |
|----------|--------|------------|
| **Q1: Auto-save states?** | Option ID + Page # + Server timestamp. Visual: Toast "Answer saved" + persistent "Saved X min ago" | HIGH |
| **Q2: Resume UX?** | Modal: "Welcome back! Q23 (Page 3). Time: 42 min. [Resume]". Never silent redirect. | MEDIUM (good pattern, specific wording may vary) |
| **Q3: Invalidation?** | Modal: "Exam closed. Answers saved. Redirecting in 3s." Poll every 5-10s. Not immediate/silent. | MEDIUM |
| **Q4: Monitoring columns?** | 6 core: Name, Status, Progress%, Current Q, Last Activity, Remaining Time. Refresh 5-10s. | MEDIUM-HIGH |
| **Q5: Save indicators?** | Toast (2-3s) + persistent "Saved X min ago" badge. Skip per-Q checkmarks. | HIGH |

---

## Sources

**All answers grounded in:**
- Moodle Quiz Settings (official docs): https://docs.moodle.org/501/en/Quiz_settings
- Canvas Quizzes Auto-Save: https://it.umn.edu/services-technologies/how-tos/canvas-students-resume-quiz-i-already
- AssessPrep Live Dashboard: https://assessprep.zendesk.com/hc/en-us/articles/4841247686033
- UX Standards (NN Group, Pajamas Design System): https://design.gitlab.com/patterns/saving-and-feedback/
- Exam proctoring platforms (Respondus, Digiexam, ExamSoft docs)

---

*For detailed reasoning, edge cases, and implementation gotchas, see:*
- *EXAM-RESILIENCE-FEATURES.md — Full feature specification*
- *EXAM-RESILIENCE-SUMMARY.md — Executive summary with roadmap implications*
