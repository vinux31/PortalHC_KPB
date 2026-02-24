# Phase 42: Session Resume - Context

**Gathered:** 2026-02-24
**Status:** Ready for planning

<domain>
## Phase Boundary

A worker who closes their browser or loses connection mid-exam can return and continue from exactly where they left off — correct page, pre-selected answers, and accurate remaining time calculated from actual active time (offline time does NOT count against their duration). No data is lost on disconnect, and the worker does not need to restart from page 1.

Coverage: both package and legacy exam paths (resume uses the same auto-saved answers from Phase 41).

</domain>

<decisions>
## Implementation Decisions

### Resume Entry Point
- Assignment card: "Mulai Ujian" button is hidden and replaced with a **"Resume"** button when there is an in-progress session for that worker
- "Resume" button uses a visually distinct color (warning/yellow or secondary) to signal in-progress state
- Clicking "Resume" → **modal dialog appears before the exam loads**: "Ada ujian yang belum selesai — lanjutkan dari soal no. X?" with **only one button: "Lanjutkan"** (no restart option)
- After "Lanjutkan": exam page loads directly on the saved page, all previous answers pre-filled
- Worker has **free navigation** — all pages accessible normally (Prev/Next work from wherever they land)
- If the exam has **expired while worker was offline**: auto-submit with whatever was auto-saved, redirect to Results page, show modal "Waktu assessment habis" with OK button

### Stale Question Set Handling
- HC is already blocked from editing questions once an assessment is active (existing system guard)
- Phase 42 adds a minimal **safety-net question-count check**: compare the question count recorded at session start vs. the current count in the package
- If mismatch detected on resume:
  - Hard block: **modal "Soal ujian telah berubah. Hubungi HC."** — worker stays on assignment card, cannot proceed
  - Saved progress is **cleared** (force fresh start) — old answers may map to wrong questions so restart is safer
- HC cannot change questions once active — this check is a defensive safety net only

### Timer Restoration
- Timer **starts immediately from server-calculated remaining time** — no loading state or delay
- Server calculates remaining time using **ElapsedSeconds** (tracked separately), NOT `now - StartedAt`
  - Reason: offline time must NOT count against the worker's exam duration
  - `remaining = exam_duration - elapsed_seconds_saved`
- ElapsedSeconds is saved to the server:
  - On every **page navigation** (Prev/Next) — as part of UpdateSessionProgress
  - **Periodically every 30 seconds** — via a setInterval in the frontend
- Tolerance: up to 30 seconds imprecision on disconnect is acceptable
- If remaining time ≤ 0 at resume: same behavior as expired exam (auto-submit, redirect to Results, modal "Waktu assessment habis" + OK)

### Pre-populated Answer Display
- Previously answered questions: **radio button pre-checked seamlessly** — no visual difference from fresh selections
- **All pages pre-populated** (not just current page) — worker can navigate to page 1 and see their previous answers
- Unanswered questions remain **empty** — no highlight, border, or special cue for unanswered state
- Answered count counter (e.g., "Soal terjawab: 12/30") immediately reflects saved answers from resume start
- If pre-populate fails to load from server:
  - **Exam still opens** on the saved page
  - Show **toast warning**: "Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. X."
  - Worker can re-answer; auto-save (Phase 41) will re-save to database
  - **Data is safe**: previously saved answers remain in database; SubmitExam reads from DB and captures all

### Claude's Discretion
- Exact modal styling and copy for the "Ada ujian yang belum selesai" resume dialog
- Exact visual treatment for the "Resume" button (specific Bootstrap color class: `btn-warning`, `btn-secondary`, etc.)
- Exact copy for the stale-question modal message (Indonesian, consistent with existing UI tone)
- Interval implementation for periodic ElapsedSeconds save (whether to piggyback on existing CheckExamStatus poll or use its own setInterval)

</decisions>

<specifics>
## Specific Ideas

- Worker experience is seamless: they click "Resume", confirm, and land exactly where they left off with all answers visible
- "Lanjutkan dari soal no. X" appears in both the resume modal AND the pre-populate failure toast — consistent framing
- The "Waktu assessment habis" modal at resume (when time has expired) should be distinct from the normal time-up flow during active exam — it's a "you came back but it was already too late" message

</specifics>

<deferred>
## Deferred Ideas

- **HC override for failed sessions**: HC can manually set a worker's "last answered question" and override their resume state — for Phase 44+ or dedicated phase
- **HC visibility into stale-blocked workers**: see which workers hit the staleness block — Phase 44 monitoring concern
- **HC question submit/confirm button**: when HC finishes entering questions, a submit button confirms the question set is finalized, with a toast confirmation — new phase, not Phase 42
- **HC can view worker's saved answer history**: for audit trail in case of disputes — future capability

</deferred>

---

*Phase: 42-session-resume*
*Context gathered: 2026-02-24*
