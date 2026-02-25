# Phase 41: Auto-Save - Context

**Gathered:** 2026-02-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Worker answers are saved to the server silently on each radio button click and before each page navigation. Workers never lose answers due to disconnect, accidental browser close, or timing issues before final submit. Auto-save is additive — SubmitExam remains unchanged and is the authoritative "close and score" action.

Coverage: BOTH package exam path (SaveAnswer → PackageUserResponse) AND legacy exam path (new endpoint → UserResponse).

</domain>

<decisions>
## Implementation Decisions

### Save Feedback Indicator
- Show a fixed-position indicator at the **bottom-right corner** of the exam page
- While save is in-flight: "Soal no. X, menyimpan..."
- On success: changes to "Soal no. X, saved"
- Displayed for ~2 seconds then fades out
- Each radio click updates the indicator with the correct question number

### Navigation Blocking (Prev / Next / ExamSummary)
- When worker clicks Prev, Next, or the button to navigate to ExamSummary: **block navigation** until all pending SaveAnswer requests for the current page resolve
- Tombol Prev/Next/ExamSummary-nav: greyed out (disabled state) while save is in flight
- If no save is pending when clicking nav: navigate immediately without any delay
- **Timeout:** If save has not responded within 5 seconds, navigation proceeds anyway

### Submit Interaction
- **SubmitExam is not changed** — auto-save is additive; SubmitExam remains the authoritative close-and-score action
- **ExamSummary page**: add a badge or note "Semua jawaban sudah tersimpan" to reassure worker before final submit
- Submit button itself is NOT blocked by pending saves — Submit can fire at any time

### Save Failure Handling
- On AJAX failure (network error / timeout): **1x retry immediately**, then give up
- If single retry also fails:
  - Show "Soal no. X, gagal tersimpan" at the bottom-right indicator
  - Show a **toast warning** "Koneksi bermasalah, cek jaringan" — appears once, fades out after ~5 seconds
- **Safety net:** SubmitExam's own upsert loop still captures any answers present in the browser at submit time, so a single failed auto-save is not catastrophic

### Exam Path Coverage
- Auto-save applies to **both** exam paths:
  - **Package exams:** call existing `SaveAnswer` endpoint (writes PackageUserResponse) on each radio click
  - **Legacy exams:** call a NEW `SaveLegacyAnswer` endpoint (writes UserResponse) — must be created in Phase 41 Plan 01
- Debounce: 300ms on radio click before firing AJAX — prevents duplicate rapid-click saves

### Claude's Discretion
- Exact Bootstrap 5 / CSS styling for the bottom-right indicator (badge, small card, or toast-like element)
- Whether to use a single shared indicator element or per-question elements
- Exact spinner/loading animation during in-flight state (if any)
- Whether "Soal no. X, menyimpan..." text uses Indonesian or "Saving..." English (follow existing UI language)

</decisions>

<specifics>
## Specific Ideas

- Indicator content format: "Soal no. X, saved" (includes question number so worker knows which was just saved)
- Two states of the indicator: "menyimpan..." → "saved" (not optimistic — waits for server response)
- SubmitExam safety net is explicitly relied on as the last-resort fallback for any failed auto-saves

</specifics>

<deferred>
## Deferred Ideas

- Auto-save for workers on mobile (behavior may differ) — no mobile scope in v2.1
- Offline mode / service worker caching — out of scope (requires network)
- Showing worker which questions are unsaved vs saved in the question number panel — could be a future UX enhancement

</deferred>

---

*Phase: 41-auto-save*
*Context gathered: 2026-02-24*
