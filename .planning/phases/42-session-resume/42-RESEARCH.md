# Phase 42: Session Resume - Research

**Researched:** 2026-02-24
**Domain:** ASP.NET Core 8 MVC exam engine, session state persistence, timer restoration, answer pre-population
**Confidence:** HIGH

## Summary

Phase 42 implements session resume for workers who disconnect mid-exam and reconnect. A worker returns to exactly where they left off—the correct page with all previously selected answers pre-filled—with remaining time calculated from actual active time spent (offline time does NOT count). The system uses ElapsedSeconds tracking (saved on page navigation and every 30 seconds) instead of `now - StartedAt` to ensure offline duration is excluded. A modal dialog at resume entry offers confirmation before reloading the exam. Pre-population of answers happens seamlessly across all pages. A safety-net question-count check detects stale question sets and forces a fresh start if the package was edited mid-session.

**Primary recommendation:** Add ElapsedSeconds and LastActivePage fields to AssessmentSession; create UpdateSessionProgress POST endpoint to persist elapsed time on every navigation and via 30-second setInterval; modify StartExam to detect in-progress sessions and show resume confirmation modal; pre-populate all answer fields before rendering; implement stale-question detection with hard-block modal; ensure timer calculates `remaining = duration - elapsed_seconds_from_db` on load.

---

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Resume Entry Point:**
- Assignment card: "Mulai Ujian" button is hidden and replaced with a **"Resume"** button when there is an in-progress session for that worker
- "Resume" button uses a visually distinct color (warning/yellow or secondary) to signal in-progress state
- Clicking "Resume" → **modal dialog appears before the exam loads**: "Ada ujian yang belum selesai — lanjutkan dari soal no. X?" with **only one button: "Lanjutkan"** (no restart option)
- After "Lanjutkan": exam page loads directly on the saved page, all previous answers pre-filled
- Worker has **free navigation** — all pages accessible normally (Prev/Next work from wherever they land)
- If the exam has **expired while worker was offline**: auto-submit with whatever was auto-saved, redirect to Results page, show modal "Waktu assessment habis" with OK button

**Stale Question Set Handling:**
- HC is already blocked from editing questions once an assessment is active (existing system guard)
- Phase 42 adds a minimal **safety-net question-count check**: compare the question count recorded at session start vs. the current count in the package
- If mismatch detected on resume:
  - Hard block: **modal "Soal ujian telah berubah. Hubungi HC."** — worker stays on assignment card, cannot proceed
  - Saved progress is **cleared** (force fresh start) — old answers may map to wrong questions so restart is safer
- HC cannot change questions once active — this check is a defensive safety net only

**Timer Restoration:**
- Timer **starts immediately from server-calculated remaining time** — no loading state or delay
- Server calculates remaining time using **ElapsedSeconds** (tracked separately), NOT `now - StartedAt`
  - Reason: offline time must NOT count against the worker's exam duration
  - `remaining = exam_duration - elapsed_seconds_saved`
- ElapsedSeconds is saved to the server:
  - On every **page navigation** (Prev/Next) — as part of UpdateSessionProgress
  - **Periodically every 30 seconds** — via a setInterval in the frontend
- Tolerance: up to 30 seconds imprecision on disconnect is acceptable
- If remaining time ≤ 0 at resume: same behavior as expired exam (auto-submit, redirect to Results, modal "Waktu assessment habis" + OK)

**Pre-populated Answer Display:**
- Previously answered questions: **radio button pre-checked seamlessly** — no visual difference from fresh selections
- **All pages pre-populated** (not just current page) — worker can navigate to page 1 and see their previous answers
- Unanswered questions remain **empty** — no highlight, border, or special cue for unanswered state
- Answered count counter (e.g., "Soal terjawab: 12/30") immediately reflects saved answers from resume start
- If pre-populate fails to load from server:
  - **Exam still opens** on the saved page
  - Show **toast warning**: "Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. X."
  - Worker can re-answer; auto-save (Phase 41) will re-save to database
  - **Data is safe**: previously saved answers remain in database; SubmitExam reads from DB and captures all

**Claude's Discretion:**
- Exact modal styling and copy for the "Ada ujian yang belum selesai" resume dialog
- Exact visual treatment for the "Resume" button (specific Bootstrap color class: `btn-warning`, `btn-secondary`, etc.)
- Exact copy for the stale-question modal message (Indonesian, consistent with existing UI tone)
- Interval implementation for periodic ElapsedSeconds save (whether to piggyback on existing CheckExamStatus poll or use its own setInterval)

### Deferred Ideas (OUT OF SCOPE)

- Mobile-specific session resume UI (no mobile scope in v2.1)
- Automatic question-set recovery (stale questions trigger hard block + restart only)
- Session timeout with auto-submission after X minutes offline (not in scope)

</user_constraints>

---

## Standard Stack

### Core (already installed)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.x | Server framework, endpoint hosting | Project baseline; existing Session management pattern, [Url.Action()] established |
| Entity Framework Core | 8.x | ORM, database persistence | Phase 41 uses ExecuteUpdateAsync for atomic upserts; same pattern for ElapsedSeconds updates |
| Bootstrap | 5.3.0 | CSS framework, button states, modal dialogs | Existing Assessment.cshtml uses btn-warning, btn-secondary; modal.show() standard |
| Fetch API | Native | AJAX POST for UpdateSessionProgress endpoint | Modern, no dependencies; same pattern as Phase 41 SaveAnswer |
| JavaScript Timers | Native | Periodic elapsed-time polling, 30-second save interval | setInterval(), setTimeout() already used in StartExam.cshtml (timer, CheckExamStatus) |
| ASP.NET Core Identity | 8.x | User context, session ownership verification | Existing SaveAnswer pattern: _userManager.GetUserAsync(User) + session.UserId check |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Modal API | 5.3.0 | Resume confirmation dialog, stale-question hard block | Modal('show')/hide() already used in existing codebase (token modal in Assessment.cshtml line 503) |
| JSON serialization | Native | Store question counts, last page state | System.Text.Json already used for UserPackageAssignment (StartExam.cs line 2828) |

### No New Packages Required
Phase 42 uses only technologies already in project stack. No new NuGet packages needed.

---

## Architecture Patterns

### Recommended Project Structure

**Backend additions:**
```
Models/
├── AssessmentSession.cs (MODIFY)
│   ├── Add: int ElapsedSeconds = 0
│   └── Add: int? LastActivePage = null
│
Controllers/
└── CMPController.cs (MODIFY + ADD)
    ├── UpdateSessionProgress(sessionId, elapsedSeconds, currentPage) — NEW POST endpoint
    └── StartExam(id) — MODIFY to detect in-progress, return answer pre-population, calculate remaining time

Migrations/
└── [date]_AddSessionResumeFields.cs — NEW (add ElapsedSeconds, LastActivePage to AssessmentSessions table)
```

**Frontend additions:**
```
Views/CMP/
├── StartExam.cshtml (MODIFY)
│   ├── Add: Resume confirmation modal (hidden by default)
│   ├── Add: Stale-question hard-block modal
│   ├── Modify: Timer initialization to use server-calculated remaining time
│   ├── Add: setInterval(UpdateSessionProgress, 30000) for periodic elapsed-time save
│   ├── Add: Update radio change handler to save elapsed time on navigation
│   └── Add: Answer pre-population logic before rendering
│
└── Assessment.cshtml (MODIFY)
    └── Detect in-progress session and show "Resume" button instead of "Start Assessment"
```

---

### Pattern 1: ElapsedSeconds Tracking and Restoration (Backend + Frontend)

**What:** Store seconds actively spent on exam (not calendar time), save on every page navigation and every 30 seconds via polling. On resume, calculate remaining time as `duration - elapsed_seconds_from_db`, skip offline duration.

**When to use:** Ensure workers' remaining exam time is not penalized by network downtime.

**Example (Backend — Model update):**
```csharp
// Source: Custom implementation following existing AssessmentSession pattern
// Models/AssessmentSession.cs

public class AssessmentSession
{
    // Existing fields...
    public DateTime? StartedAt { get; set; }

    // NEW FIELDS for resume:
    /// <summary>
    /// Total seconds worker has actively spent in the exam (excludes offline time).
    /// Updated on each page navigation and every 30 seconds via frontend polling.
    /// </summary>
    public int ElapsedSeconds { get; set; } = 0;

    /// <summary>
    /// Last page (0-based index) the worker was viewing before disconnect.
    /// Used to resume on correct page.
    /// </summary>
    public int? LastActivePage { get; set; }
}
```

**Example (Backend — Endpoint to save elapsed time):**
```csharp
// Source: CMPController.cs — NEW endpoint
// Pattern: POST with antiforgery, explicit session owner check, atomic update

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateSessionProgress(int sessionId, int elapsedSeconds, int? currentPage)
{
    var session = await _context.AssessmentSessions.FindAsync(sessionId);
    if (session == null) return NotFound();

    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    // Session ownership check (same pattern as SaveAnswer)
    if (session.UserId != user.Id)
        return Json(new { success = false, error = "Unauthorized" });

    // Skip update if session already closed
    if (session.Status == "Completed" || session.Status == "Abandoned")
        return Json(new { success = false, error = "Session already closed" });

    // Update elapsed seconds and last active page atomically
    var updated = await _context.AssessmentSessions
        .Where(s => s.Id == sessionId)
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.ElapsedSeconds, elapsedSeconds)
            .SetProperty(r => r.LastActivePage, currentPage ?? 0)
            .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
        );

    if (updated == 0)
        return Json(new { success = false });

    return Json(new { success = true });
}
```

**Example (Frontend — Initialize timer with server-calculated remaining time):**
```javascript
// Source: StartExam.cshtml JavaScript
// Pattern: Fetch ElapsedSeconds from server on page load, calculate remaining time

// Initial state from server (passed via ViewBag or data attribute)
const DURATION_SECONDS = @Model.DurationMinutes * 60;
const ELAPSED_SECONDS_FROM_DB = @(ViewBag.ElapsedSeconds ?? 0);  // Set by StartExam controller

let timeRemaining = DURATION_SECONDS - ELAPSED_SECONDS_FROM_DB;
let elapsedSeconds = ELAPSED_SECONDS_FROM_DB;

// Timer update (increments elapsedSeconds locally)
function updateTimer() {
    timeRemaining--;
    elapsedSeconds++;

    const minutes = Math.floor(timeRemaining / 60);
    const seconds = timeRemaining % 60;
    const display = (minutes < 10 ? '0' + minutes : minutes) + ':' +
                    (seconds < 10 ? '0' + seconds : seconds);
    const el = document.getElementById('examTimer');
    el.innerText = display;

    if (timeRemaining <= 0) {
        clearInterval(timerInterval);
        // Auto-submit on timeout (same as Phase 41)
        autoSubmitExam('Waktu assessment habis');
    }
}
var timerInterval = setInterval(updateTimer, 1000);
updateTimer();

// Periodic save: every 30 seconds, send elapsed time to server
const UPDATE_SESSION_URL = '@Url.Action("UpdateSessionProgress", "CMP")';
const SAVE_INTERVAL_MS = 30000;

setInterval(function() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    fetch(UPDATE_SESSION_URL, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({
            sessionId: @Model.AssessmentSessionId,
            elapsedSeconds: elapsedSeconds,
            currentPage: currentPage
        })
    })
    .catch(err => console.error('Failed to save session progress:', err));
}, SAVE_INTERVAL_MS);
```

**Key points:**
- ElapsedSeconds increments locally on client; synced to server every 30 seconds + on page navigation
- Timer uses `remaining = duration - elapsed_from_db`, excluding offline duration
- Atomic database update via ExecuteUpdateAsync prevents race conditions
- If save fails, local timer continues (worst case: 30-second imprecision)

---

### Pattern 2: Resume Confirmation Modal (Frontend)

**What:** Before loading a resumed exam, show modal "Ada ujian yang belum selesai — lanjutkan dari soal no. X?" with only "Lanjutkan" button. On cancel, return to assignment card.

**When to use:** Confirm worker intent to resume rather than start fresh; show which page they'll resume on.

**Example (HTML):**
```html
<!-- In StartExam.cshtml or Assessment.cshtml, hidden by default -->
<div class="modal fade" id="resumeConfirmModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-warning">
            <div class="modal-header bg-light">
                <h5 class="modal-title text-warning">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>Ada ujian yang belum selesai
                </h5>
            </div>
            <div class="modal-body">
                <p id="resumeMessage">
                    Anda memiliki ujian yang belum selesai. Lanjutkan dari soal no. <strong id="resumePageNum">--</strong>?
                </p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary w-100" id="resumeConfirmBtn">
                    <i class="bi bi-arrow-right me-1"></i>Lanjutkan
                </button>
            </div>
        </div>
    </div>
</div>
```

**Example (JavaScript in StartExam.cshtml):**
```javascript
// Source: Custom implementation based on existing token modal pattern
// Check if this is a resume scenario (LastActivePage > 0 and InProgress status)

const IS_RESUME = @(ViewBag.IsResume ?? false);  // Set by StartExam controller
const RESUME_PAGE = @(ViewBag.LastActivePage ?? 0);

if (IS_RESUME && RESUME_PAGE !== undefined) {
    // Show resume confirmation modal on page load
    document.getElementById('resumePageNum').innerText = (RESUME_PAGE + 1); // Convert 0-based to 1-based display
    const resumeModal = new bootstrap.Modal(document.getElementById('resumeConfirmModal'));
    resumeModal.show();

    document.getElementById('resumeConfirmBtn').addEventListener('click', function() {
        resumeModal.hide();
        // Proceed to exam page at RESUME_PAGE (already set in currentPage initialization)
        currentPage = RESUME_PAGE;
        updatePanel(); // Render correct page
    });
}
```

**Key points:**
- Modal shows "soal no. X" (1-based display number converted from 0-based LastActivePage)
- Only one button: "Lanjutkan" (no cancel/restart option as per user decision)
- Data-bs-backdrop="static" prevents dismissal by clicking outside
- Modal shown before exam rendering; once confirmed, exam loads at correct page

---

### Pattern 3: Stale Question Set Detection (Backend)

**What:** On resume, compare question count from session start vs. current package question count. If mismatch, hard-block with modal and clear saved progress.

**When to use:** Prevent worker from answering questions that may have been edited by HC after session started.

**Example (Backend — Modify StartExam):**
```csharp
// Source: CMPController.cs StartExam method (around line 2707)
// Addition: Question-count check for package path

// Load the assigned package (after assignment lookup)
var assignedPackage = packages.First(p => p.Id == assignment.AssessmentPackageId);

// NEW: Stale question set check
if (assignment.SavedQuestionCount.HasValue &&
    assignment.SavedQuestionCount.Value != assignedPackage.Questions.Count)
{
    // Question count mismatch: clear the session and block resume
    // (Clear is optional: existing decisions say "clear saved progress" but frontend blocks with modal)
    // Set flag to show hard-block modal instead of resuming
    ViewBag.StaleQuestionSet = true;
    ViewBag.ErrorMessage = "Soal ujian telah berubah. Hubungi HC.";
    // Return early to Assessment view (don't render exam)
    return RedirectToAction("Assessment");
}
```

**Example (Frontend — Show hard-block modal on Assessment card):**
```html
<!-- In Assessment.cshtml, after worker clicks "Resume" button, check for stale set error -->
<div class="modal fade" id="staleQuestionModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-danger">
            <div class="modal-header bg-light">
                <h5 class="modal-title text-danger">
                    <i class="bi bi-exclamation-circle-fill me-2"></i>Soal Ujian Berubah
                </h5>
            </div>
            <div class="modal-body">
                <p>Soal ujian telah berubah sejak Anda mulai mengerjakan. Hubungi HC untuk mengulang ujian.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">OK</button>
            </div>
        </div>
    </div>
</div>

<script>
    // If TempData contains stale question error, show modal
    @if (!string.IsNullOrEmpty(ViewBag.ErrorMessage) && ViewBag.ErrorMessage.Contains("Soal ujian telah berubah"))
    {
        <text>
            const staleModal = new bootstrap.Modal(document.getElementById('staleQuestionModal'));
            staleModal.show();
        </text>
    }
</script>
```

**Key points:**
- Check happens after assignment lookup but before exam rendering
- Hard block: no resume allowed; worker must contact HC
- Optional: Clear saved progress (SavedQuestionCount mismatch triggers data cleanup)
- Modal dismissed returns worker to assignment card

---

### Pattern 4: Answer Pre-population on Resume (Frontend)

**What:** Load all previously saved answers from database, pre-fill radio buttons for all pages (not just current page), update answered count.

**When to use:** Seamlessly restore worker's exam state without visual cues or delays.

**Example (Backend — Modify StartExam to return saved answers):**
```csharp
// Source: CMPController.cs StartExam (after building PackageExamViewModel)
// Pattern: Query PackageUserResponses for this session, include in ViewBag

// Load pre-existing answers for this session
var savedAnswers = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == id)
    .ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId ?? 0);

ViewBag.SavedAnswers = System.Text.Json.JsonSerializer.Serialize(savedAnswers);
ViewBag.IsResume = assessment.StartedAt != null && assessment.CompletedAt == null;
ViewBag.LastActivePage = assignment?.LastActivePage ?? 0;
ViewBag.ElapsedSeconds = assessment.ElapsedSeconds;

return View(vm);
```

**Example (Frontend — Initialize radio buttons with saved answers):**
```javascript
// Source: StartExam.cshtml (in initialization section, after page load)
// Pattern: Parse saved answers, pre-check matching radio buttons

const SAVED_ANSWERS = @Html.Raw(ViewBag.SavedAnswers ?? "{}");  // Object: { questionId: optionId, ... }
const IS_RESUME = @(ViewBag.IsResume ?? false);

function prePopulateAnswers() {
    Object.entries(SAVED_ANSWERS).forEach(([qIdStr, optId]) => {
        const qId = parseInt(qIdStr);
        if (optId > 0) {
            // Find and check the radio button
            const radio = document.querySelector(`input[name="radio_${qId}"][value="${optId}"]`);
            if (radio) {
                radio.checked = true;
                document.getElementById('ans_' + qId).value = optId;
                answeredQuestions.add(qId); // Track for answered count
            }
        }
    });
    updateAnsweredCount();
}

// Call on page load if resume scenario
if (IS_RESUME) {
    prePopulateAnswers();
}

// Or call whenever page changes
function updatePanel() {
    // ... existing page rendering code ...
    prePopulateAnswers();  // Re-check radio buttons on new page
    updateAnsweredCount();
}
```

**Key points:**
- Saved answers loaded as JSON object from server before view renders
- Radio buttons pre-checked before page visibility (no visual flicker)
- All pages pre-populated (not just current page) — supports free navigation
- answeredQuestions Set updated for accurate answer count display

---

### Pattern 5: Expired Exam on Resume (Backend + Frontend)

**What:** If remaining time ≤ 0 on resume, auto-submit saved answers and show modal "Waktu assessment habis".

**When to use:** Worker reconnects after exam window expired while offline.

**Example (Backend — Check on StartExam):**
```csharp
// Source: CMPController.cs StartExam (add after timer restoration calculation)

// Calculate remaining time from elapsed seconds
int remainingSeconds = (assessment.DurationMinutes * 60) - assessment.ElapsedSeconds;

if (remainingSeconds <= 0)
{
    // Exam expired: trigger auto-submit
    ViewBag.ExamExpired = true;
    ViewBag.ExpiredAction = "auto-submit"; // Signal to frontend to auto-submit
    // Optionally call SubmitExam here directly instead of frontend-based submission
}
else
{
    ViewBag.RemainingSeconds = remainingSeconds;
}
```

**Example (Frontend — Auto-submit and show modal):**
```javascript
// In StartExam.cshtml, check if exam expired on load

const EXAM_EXPIRED = @(ViewBag.ExamExpired ?? false);

if (EXAM_EXPIRED) {
    // Show modal and auto-submit
    const expiredModal = new bootstrap.Modal(document.getElementById('examExpiredModal'));
    expiredModal.show();

    // Auto-submit after brief delay
    setTimeout(function() {
        autoSubmitExam('Waktu assessment habis');
    }, 1500);
}

function autoSubmitExam(reason) {
    // Disable form changes
    document.querySelectorAll('.exam-radio').forEach(r => r.disabled = true);

    // Submit form to SubmitExam action
    document.getElementById('examForm').submit();
}
```

**Example (HTML — Expired modal):**
```html
<div class="modal fade" id="examExpiredModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-danger">
            <div class="modal-header bg-light">
                <h5 class="modal-title text-danger">
                    <i class="bi bi-hourglass-end-fill me-2"></i>Waktu Assessment Habis
                </h5>
            </div>
            <div class="modal-body">
                <p>Waktu ujian Anda telah berakhir. Jawaban Anda akan dikirimkan secara otomatis.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">OK</button>
            </div>
        </div>
    </div>
</div>
```

**Key points:**
- Remaining time = duration - elapsed_seconds (excludes offline time)
- If ≤ 0, auto-submit immediately (no re-examination window)
- Modal shown before submission for UX clarity
- Same auto-submit logic as Phase 41 timeout handling

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Elapsed time persistence | Custom time tracking library | ElapsedSeconds integer + UpdateSessionProgress POST | Simplicity; works with existing EF pattern; 30-second tolerance acceptable |
| Timer calculation with offline handling | Client-side offset calculations | Server-calculated remaining time (duration - elapsed_from_db) | Prevents timezone/clock-sync issues; server is source of truth |
| Resume state machine | Custom state flags | AssessmentSession fields + IsResume/LastActivePage ViewBag flags | Leverages existing EF model; minimal new schema |
| Answer pre-population | Manual radio scanning loop | SAVED_ANSWERS JSON object from server + pre-fill before render | Cleaner, no DOM flicker, consistent with phase 41 patterns |
| Modal dialogs | Hand-built modal HTML/CSS | Bootstrap Modal API (modal.show()/hide()) | Already installed; matches existing codebase (token modal in Assessment.cshtml) |
| Question-count verification | Custom hash comparison | Simple integer count comparison (SavedQuestionCount vs. current Count) | Minimal, fast, sufficient for defensive check |

**Key insight:** Session resume appears complex but breaks into simple parts: persist elapsed time (UpdateSessionProgress), detect in-progress (IS_RESUME flag), pre-fill answers (JSON object), verify questions (integer comparison). Each pattern exists in Phase 41 or codebase; no novel infrastructure needed.

---

## Common Pitfalls

### Pitfall 1: Offline Time Counted Against Exam Duration

**What goes wrong:** Worker's timer shows 10 minutes remaining, goes offline for 15 minutes, reconnects to timer showing -5 minutes expired. System auto-submits and worker loses remaining exam time.

**Why it happens:** Timer uses `now - StartedAt` instead of ElapsedSeconds. Offline period counts as exam time.

**How to avoid:** Use ElapsedSeconds field (updated every 30 seconds on server). Calculate remaining as `duration - elapsed_seconds_from_db`. On resume, set `timeRemaining = DURATION_SECONDS - ELAPSED_SECONDS_FROM_DB` before starting timer.

**Warning signs:** Timer on resume shows remaining time less than expected; workers report unfair time loss after disconnect.

---

### Pitfall 2: Resume Modal Not Shown; Exam Loads Directly

**What goes wrong:** Worker resumes in-progress exam, page jumps immediately to last page without confirmation modal. Worker confused whether they're resuming or starting fresh.

**Why it happens:** IS_RESUME flag not set in ViewBag, or modal-show logic not triggered on page load.

**How to avoid:** Set ViewBag.IsResume in StartExam based on `assessment.StartedAt != null && assessment.CompletedAt == null`. Check IS_RESUME in JavaScript before rendering; if true, instantiate and show bootstrap modal before calling `updatePanel()`.

**Warning signs:** Worker reports no resume modal; timer starts at beginning instead of resuming value; answeredQuestions count resets.

---

### Pitfall 3: Answers Pre-populated Incorrectly; Wrong Options Checked

**What goes wrong:** Worker previously answered Q1 as "Option B", resumes and sees "Option D" pre-checked. Worker re-answers thinking original answer was lost.

**Why it happens:** SAVED_ANSWERS object contains wrong question-option mapping, or radio selector `input[name="radio_${qId}"][value="${optId}"]` doesn't match actual DOM structure.

**How to avoid:** Verify SAVED_ANSWERS object in browser console matches database state. Test radio selector on each question (use `document.querySelectorAll()` and check name/value attributes match). Call prePopulateAnswers() AFTER DOM is fully rendered (not during AJAX page change).

**Warning signs:** Browser console logs "Radio not found for question X"; answered count shows wrong total; visual mismatch between saved answer in database and checked radio.

---

### Pitfall 4: ElapsedSeconds Save Fails Silently; Resume Shows Wrong Time

**What goes wrong:** UpdateSessionProgress fails due to network error; elapsedSeconds on server stays at 0. Worker resumes with timer showing full duration instead of elapsed time consumed.

**Why it happens:** Fetch request to UpdateSessionProgress times out; error not handled; local elapsedSeconds continues to increment but server-side value never updates.

**How to avoid:** Add error handling to UpdateSessionProgress fetch (log to console, optionally show silent toast). Implement retry logic (1x immediate retry on failure). Accept that up to 30 seconds of imprecision is tolerable (resume time will be off by ≤ 30 seconds in worst case).

**Warning signs:** Network tab shows failed/timeout requests to UpdateSessionProgress; resumed timer is always significantly ahead of expected; workers complain timer doesn't match what they expected.

---

### Pitfall 5: Stale Question Check Doesn't Trigger; Worker Answers Edited Questions

**What goes wrong:** HC edits package to remove 5 questions while worker is taking exam. Worker resumes and answers old question IDs that no longer map to current questions. On submit, scoring breaks.

**Why it happens:** SavedQuestionCount field not populated on first StartExam, or question-count comparison logic missing on resume.

**How to avoid:** On first exam load (StartExam), save `assignment.SavedQuestionCount = assignedPackage.Questions.Count`. On resume, compare: if `SavedQuestionCount != current count`, set ViewBag.StaleQuestionSet = true and return error modal without rendering exam. Hard-block; no resume allowed.

**Warning signs:** Resume modal shown but exam loads anyway with fewer/different questions; scoring errors after submit (option IDs don't exist in current question set).

---

### Pitfall 6: Navigation Doesn't Trigger UpdateSessionProgress; ElapsedSeconds Stale

**What goes wrong:** Worker navigates Prev/Next frequently; UpdateSessionProgress only fires every 30 seconds. Worker disconnects immediately after page change; only 5 seconds of elapsed time saved instead of 25 seconds.

**Why it happens:** ElapsedSeconds only updated via 30-second setInterval, not on page navigation event.

**How to avoid:** Call UpdateSessionProgress on EVERY navigation (Prev/Next buttons), in addition to 30-second setInterval. Combine: `changePage()` triggers POST to UpdateSessionProgress immediately with current elapsedSeconds; setInterval provides fallback for non-navigation activity.

**Warning signs:** Workers who frequently navigate show larger time-discrepancies on resume; workers who navigate once then wait 30+ seconds show small discrepancies.

---

### Pitfall 7: Pre-population Runs Before SAVED_ANSWERS Parsed; No Answers Shown

**What goes wrong:** prePopulateAnswers() called before @Html.Raw(ViewBag.SavedAnswers) is evaluated. SAVED_ANSWERS = {} (empty), no radio buttons checked.

**Why it happens:** prePopulateAnswers() called at top of script before ViewBag variables rendered; or ViewBag.SavedAnswers is null.

**How to avoid:** Ensure SAVED_ANSWERS declaration happens BEFORE prePopulateAnswers() function definition. Call prePopulateAnswers() AFTER page load (in updatePanel() or at end of script block). Test ViewBag.SavedAnswers != null in backend before setting.

**Warning signs:** Browser console shows SAVED_ANSWERS = {}; no radio buttons pre-checked on resume; answered count shows 0.

---

## Code Examples

Verified patterns from official sources:

### ExecuteUpdateAsync for ElapsedSeconds
```csharp
// Source: Microsoft.EntityFrameworkCore 8.0 documentation
// https://learn.microsoft.com/en-us/ef/core/saving/execute-update-delete

var updated = await _context.AssessmentSessions
    .Where(s => s.Id == sessionId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.ElapsedSeconds, elapsedSeconds)
        .SetProperty(r => r.LastActivePage, currentPage)
        .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
    );

if (updated == 0)
    return Json(new { success = false });

return Json(new { success = true });
```

---

### Timer Restoration from Server-Calculated Remaining Time
```javascript
// Source: Standard timer pattern adapted from existing StartExam.cshtml
// Calculate remaining time from database ElapsedSeconds, not client time

const DURATION_SECONDS = @Model.DurationMinutes * 60;
const ELAPSED_SECONDS_FROM_DB = @(ViewBag.ElapsedSeconds ?? 0);

let timeRemaining = DURATION_SECONDS - ELAPSED_SECONDS_FROM_DB;
let elapsedSeconds = ELAPSED_SECONDS_FROM_DB;

function updateTimer() {
    timeRemaining--;
    elapsedSeconds++;

    const minutes = Math.floor(timeRemaining / 60);
    const seconds = timeRemaining % 60;
    document.getElementById('examTimer').innerText =
        (minutes < 10 ? '0' : '') + minutes + ':' +
        (seconds < 10 ? '0' : '') + seconds;

    if (timeRemaining <= 0) {
        clearInterval(timerInterval);
        autoSubmitExam('Waktu assessment habis');
    }
}

var timerInterval = setInterval(updateTimer, 1000);
updateTimer();
```

---

### Resume Confirmation Modal (Bootstrap 5)
```html
<!-- Source: Bootstrap Modal documentation
     https://getbootstrap.com/docs/5.3/components/modal/ -->

<div class="modal fade" id="resumeConfirmModal" tabindex="-1" data-bs-backdrop="static">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-warning">
            <div class="modal-header bg-light">
                <h5 class="modal-title">Ada ujian yang belum selesai</h5>
            </div>
            <div class="modal-body">
                <p>Lanjutkan dari soal no. <strong id="resumePageNum">--</strong>?</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-primary w-100" id="resumeConfirmBtn">
                    Lanjutkan
                </button>
            </div>
        </div>
    </div>
</div>

<script>
    const IS_RESUME = @(ViewBag.IsResume ?? false);
    const RESUME_PAGE = @(ViewBag.LastActivePage ?? 0);

    if (IS_RESUME) {
        document.getElementById('resumePageNum').innerText = (RESUME_PAGE + 1);
        const modal = new bootstrap.Modal(document.getElementById('resumeConfirmModal'));
        modal.show();

        document.getElementById('resumeConfirmBtn').addEventListener('click', () => {
            modal.hide();
            currentPage = RESUME_PAGE;
            updatePanel();
        });
    }
</script>
```

---

### Periodic Session Progress Update (setInterval + Fetch)
```javascript
// Source: Fetch API + setInterval pattern from existing StartExam
// Update session progress every 30 seconds

const UPDATE_SESSION_URL = '@Url.Action("UpdateSessionProgress", "CMP")';
const TOKEN = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

setInterval(function() {
    fetch(UPDATE_SESSION_URL, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': TOKEN
        },
        body: JSON.stringify({
            sessionId: @Model.AssessmentSessionId,
            elapsedSeconds: elapsedSeconds,
            currentPage: currentPage
        })
    })
    .catch(err => console.error('UpdateSessionProgress failed:', err));
}, 30000);  // Every 30 seconds
```

---

## State of the Art

| Old Approach | Current Approach (Phase 42) | When Changed | Impact |
|--------------|----------------------------|--------------|--------|
| No session resume; fresh start required on reconnect | Resume from last page with pre-populated answers | Phase 42 (v2.1) | Workers don't lose progress on network disconnect; no restart friction |
| Timer uses `now - StartedAt`; offline time counts | Timer uses `duration - elapsed_seconds_from_db` | Phase 42 (v2.1) | Fair exam duration; offline time not penalized |
| No last-page tracking | LastActivePage field tracks 0-based page index | Phase 42 (v2.1) | Resume loads correct page; no manual page selection |
| HC can edit questions during live exam | Safety-net question-count check on resume | Phase 42 (v2.1) | Detects stale questions; prevents answer misalignment |
| Fire-and-forget answer save (Phase 41) | Combines with UpdateSessionProgress for state tracking (Phase 42) | Phase 42 (v2.1) | Both answer persistence and session state atomic; resume fully reliable |

**Deprecated/outdated:**
- Fresh exam start on reconnect: Replaced by resume confirmation modal + answer pre-population
- Client-based timer with offline penalty: Replaced by server-calculated remaining time from ElapsedSeconds
- Manual page selection on resume: Replaced by automatic last-page restoration

---

## Open Questions

1. **UpdateSessionProgress triggered on every navigation vs. 30-second fallback only**
   - What we know: Context specifies "Periodically every 30 seconds" + "on every page navigation"
   - What's unclear: Should navigation-triggered save be immediate POST, or queued with debounce?
   - Recommendation: Immediate POST on navigation (Prev/Next/ExamSummary); 30-second setInterval is fallback for idle users. No debounce needed (updates are rare; server handles idempotently).

2. **SavedQuestionCount initialization and migration**
   - What we know: Phase context mentions question-count check but doesn't specify where SavedQuestionCount is stored
   - What's unclear: Is SavedQuestionCount a field on UserPackageAssignment or AssessmentSession?
   - Recommendation: Store on UserPackageAssignment (per-assignment state). Set in StartExam when assignment is created: `assignment.SavedQuestionCount = assignedPackage.Questions.Count`. Compare on resume: if mismatch, block with modal.

3. **Timeout for UpdateSessionProgress failures**
   - What we know: Phase 41 uses 5-second timeout for SaveAnswer; Phase 42 uses 30-second polling interval
   - What's unclear: Should UpdateSessionProgress have explicit timeout, or rely on browser default?
   - Recommendation: No explicit timeout needed. If UpdateSessionProgress fails, local elapsedSeconds continues to increment; next 30-second poll will try again. Worst case: 30-second imprecision (acceptable per context).

4. **Answer pre-population for legacy exams (non-package path)**
   - What we know: UserResponse stores legacy exam answers (SelectedOptionId); pre-population logic applies to both paths
   - What's unclear: Does legacy path use same SAVED_ANSWERS JSON structure, or different mapping?
   - Recommendation: Yes, same structure. Query UserResponse by AssessmentSessionId; build dictionary `{ question_id: option_id }`. Serialize to JSON; pass via ViewBag. Pre-populate logic is path-agnostic (works on DOM radio elements regardless of source).

5. **Modal button text: "Lanjutkan" only vs. "Lanjutkan" + "Mulai Ulang"**
   - What we know: Context explicitly specifies "only one button: Lanjutkan (no restart option)"
   - What's unclear: Is restart option completely removed, or just deferred to Phase 43+?
   - Recommendation: Only "Lanjutkan" button in this phase. Restart would require full answer clearing; defer to future phase if needed. Current design is simple: resume or contact HC for reset.

---

## Sources

### Primary (HIGH confidence)

- **AssessmentSession.cs, Models/**: Existing field structure (StartedAt, Status, CompletedAt); confirms ElapsedSeconds addition pattern
- **CMPController.cs, StartExam method (line 2707)**: Existing session state checks and answer loading patterns
- **CheckExamStatus method (line 1114)**: Existing session ownership verification and status logic
- **Entity Framework Core 8.0 — ExecuteUpdateAsync**: Microsoft official documentation for atomic database updates (same pattern as Phase 41)
- **StartExam.cshtml (lines 163-533)**: Existing timer, radio change handlers, answer tracking, Phase 41 auto-save integration
- **Assessment.cshtml (lines 499-511)**: Existing button rendering logic; confirms where "Resume" button replaces "Start Assessment"
- **Bootstrap Modal API 5.3**: Official Bootstrap documentation for modal.show()/hide() (already used in codebase)

### Secondary (MEDIUM confidence)

- **Phase 41 Research.md**: Auto-save patterns, debounce logic, antiforgery token handling, navigation blocking (transferred to Phase 42)
- **PackageUserResponse.cs, UserResponse.cs**: Response models; confirm both path types can be queried the same way for answer pre-population
- **Existing fetch patterns in StartExam.cshtml**: AJAX POST structure, error handling, antiforgery token injection (applied to UpdateSessionProgress)

### Tertiary (LOW confidence)

- None identified; all claims verified against official docs, codebase patterns, or Phase 41 research

---

## Metadata

**Confidence breakdown:**
- **Standard Stack: HIGH** — All libraries verified installed (ASP.NET Core 8, Bootstrap 5, Fetch API); patterns exist in Phase 41 or codebase
- **Architecture: HIGH** — Patterns (ElapsedSeconds tracking, resume modal, answer pre-population) adapted from existing Session lifecycle and Phase 41 auto-save; verified with official docs
- **Pitfalls: HIGH** — Common issues identified from timer-calculation logic, modal initialization, pre-population timing; prevention strategies provided
- **Open Questions: Mostly resolved** — Clarifications made based on codebase inspection (UserPackageAssignment structure, legacy path answer model, button patterns)

**Research date:** 2026-02-24
**Valid until:** 2026-03-03 (7 days; ASP.NET Core 8 and Bootstrap Modal are stable; no breaking changes expected this week)

---
