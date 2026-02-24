# Research Summary: Exam Resilience & Live Monitoring (v2.1)

**Domain:** Online assessment/exam systems, resilience features, real-time monitoring
**Researched:** February 24, 2026
**Overall confidence:** MEDIUM (HIGH on patterns, MEDIUM on edge cases)

---

## Executive Summary

Online assessment platforms follow well-established patterns for exam resilience. The v2.1 feature set—auto-save, session resume, exam invalidation, and HC monitoring—are interdependent and collectively enable the Close Early feature (already built) to work reliably.

**Key insight:** Auto-save is the foundation. Every other feature depends on it. If auto-save is wrong, everything else fails. Resume requires accurate auto-save state. Invalidation assumes answers are saved before force-close. HC monitoring accuracy depends on knowing what's actually saved.

**Stack recommendation:** Use 60-second polling + per-click saves (JavaScript on answer change) + server-side debouncing to avoid race conditions. Standard patterns from Moodle, Canvas, and Exam.net.

**Table stakes vs differentiators:**
- **Must build:** Auto-save with toast feedback, session resume modal, soft-modal invalidation, basic HC monitoring (6 core columns)
- **Nice-to-have:** Per-question animated indicators, persistent state timestamps, real-time WebSocket updates, time-extension UI
- **Skip:** Keystroke-level saves, tab-switch blocking, silent redirects, 30+ monitoring columns

---

## Key Findings by Question

### 1. Auto-Save: Table Stakes Behavior

**Selected option states + page position + timestamp** are the non-negotiable persistence targets. All three are required for both auto-save and later resume to work.

**Visual feedback pattern:** Immediate click feedback (instant selection) + "Saving..." (1-2s) + "Answer saved" toast (2-3s auto-dismiss) + persistent "Saved X min ago" badge. This pattern is verified across design systems (Carbon, Pajamas) and exam platforms (Moodle 2-min interval default, Canvas per-click + per-page).

**Error handling:** Network drop doesn't discard the answer. Keep it selected locally, retry save every 5-10s, show "reconnecting" status. This allows graceful recovery when connection returns.

**Mitigation:** Debounce rapid clicks (ignore same question within 500ms), disable page navigation until save ACK, use server timestamps (never trust client clock for resume time calculation).

### 2. Session Resume: UX Clarity Over Surprise

**Recommendation: Explicit modal (Option A)** — "Welcome back, Alice! You left off on Question 23 (Page 3 of 10). Time remaining: 42 minutes. [Resume]"

Why this wins over silent resume or restart-from-Q1:
- Worker is disoriented after disconnect; explicit confirmation prevents panic
- Shows exact position + remaining time (critical context)
- Single clear action button prevents accidental restart

**Time recalculation is critical:** Resume must calculate remaining = (assigned_time) - (now - exam_start_timestamp). Never use client clock or assume timer hasn't advanced.

**Resume requirements:** Page position + all saved answers + question order (don't re-shuffle). Exception: Don't restore scroll position; let user re-read.

### 3. Exam Invalidation: Soft Signal, Not Shock

**When HC closes exam (Close Early), worker should:** Poll status every 5-10s, detect `exam_status == "Closed"`, show modal "Exam Closed — Your answers have been saved and will be scored. Redirecting in 3 seconds. [View Results Now]", then redirect.

**Why soft modal vs immediate redirect:**
- Immediate redirect = disorientation ("What happened? Did I crash?")
- Silent invalidation = worker keeps taking exam unaware
- Modal + 3-5s delay = respectful UX, clear explanation, gives worker agency ("View Now" button)

**Polling frequency:** 5-10s standard from proctoring tools. Too fast (< 2s) wastes server resources; too slow (> 15s) defeats the purpose.

### 4. HC Monitoring: Core Columns for Supervision

**Essential 6 columns** (verified across AssessPrep, ProgressLearning, Code.org):
1. Worker Name — identify who
2. Status (InProgress/Completed/Abandoned/Closed) — health check
3. Progress % (45/100) — completion estimate
4. Current Question (Q23 or "Q23, Page 3") — precise position
5. Last Activity ("2 min ago") — detect stalled/offline workers
6. Remaining Time ("14:32") — time pressure visibility

**Optional columns** (add if UI space allows): Device/Browser (troubleshoot), Online Status (green/red dot), Session Duration.

**Avoid:** 30+ columns (info overload), per-question scores (belongs in detail view, not summary), predicted completion time (too speculative).

**Refresh frequency:** 5-10s polling for v2.1 (simple, no infrastructure). Real-time WebSocket is v2.2 enhancement.

---

## Feature Dependencies & Sequence

```
Phase 2.1: Foundation Resilience
├─ Auto-save (60s polling + per-click) — MUST ship first
│  └ Enables all downstream features
├─ Session Resume (modal, time recalc) — MUST ship with auto-save
│  └ Requires auto-save state to restore from
├─ Exam Invalidation Polling (5-10s check) — LIGHTWEIGHT addition
│  └ Builds on auto-save + resume infrastructure
└─ HC Monitoring (6-column dashboard) — Can ship Phase 2.2
   └ Useful only after auto-save/resume are stable

Verification: Before ship, test auto-save accuracy under:
- Network disconnect > 2 min, then reconnect
- Multi-page navigation (answers on page 1 survive navigate-to-page-3-and-back)
- Timer drift (auto-save timestamp ≠ countdown timer)
```

---

## High-Risk Edge Cases

| Risk | Consequence | Mitigation |
|------|-----------|-----------|
| **Timestamp mismatch (client clock vs server)** | Resume shows wrong remaining time; worker panics or gets unfair additional time | Always use server timestamp for "now". Never trust client clock. Recalculate: remaining = assigned_duration - (server_now - exam_start_timestamp) |
| **Shuffle seed not saved/restored** | Resume loads different question order; worker's "Q5 = option C" answer lands on different question | Must save shuffle_seed with exam_session_start. Restore exact shuffle on resume using same seed. |
| **Auto-save on page navigation blocks forever** | Worker clicks "Next Page" but save fails; page transition hangs, worker stuck | Queue answer + allow navigation immediately. Use optimistic UI: show new page while save completes in background. Show "still saving..." if save > 2s. |
| **Invalidation polling misses Close Early signal** | Worker finishes exam unaware it was closed; submits answers thinking they're taking a live exam | Polling interval must be 5-10s maximum. No exceptions. Log each poll attempt for audit. |
| **HC monitoring shows stale progress** | HC thinks worker is on Q15 but worker already submitted; HC closes exam thinking it's still active | Auto-refresh if HC page is open. Max 10s latency. Consider real-time push (v2.2) if < 10s becomes unacceptable. |

---

## Implications for Roadmap

### Recommended Phase 2.1 Scope

**In scope:** Auto-save + Session Resume + Invalidation Polling
- These three form a coherent unit: save → detect disconnect → resume → detect invalidation
- Covers worker resilience (answers aren't lost)
- Enables Close Early feature to work reliably
- Estimated effort: 3-4 weeks engineering

**Out of scope for 2.1:** HC Monitoring Dashboard
- Can follow in 2.2 without blocking workers
- Depends on auto-save being proven reliable first
- Gives time for UX design + QA sign-off on resilience before exposing HC to live data

### Phase Structure Recommendation

```
Phase 2.1a: Auto-Save Foundation (1 week)
├─ Database: answer_saves table (question_id, selected_option_id, answer_timestamp, page_number)
├─ Backend: Save endpoint, debouncing, server-side timestamp
├─ Frontend: Per-click AJAX + 60s polling, toast feedback
└─ Testing: Network disconnect scenarios, race conditions, clock skew

Phase 2.1b: Session Resume (1 week)
├─ Backend: Resume endpoint (fetch last saved state, recalculate timer)
├─ Frontend: Resume modal on login-during-InProgress detection
├─ Integration: Ensure auto-saved answers restore correctly
└─ Testing: Disconnect/reconnect flow, time recalculation, shuffle seed preservation

Phase 2.1c: Invalidation + Close Early Integration (1 week)
├─ Worker polling: 5-10s status check, modal on close detection
├─ Close Early integration: Set exam_status = "Closed", worker detects via poll
├─ HC feature enablement: Close Early button now knows answers are saved
└─ Testing: Concurrent worker submissions during Close Early window

Phase 2.2: HC Monitoring (2 weeks)
├─ Backend: Monitoring endpoint (workers' current question, progress %, last activity)
├─ Frontend: Dashboard (6 core columns, 5-10s auto-refresh, click-for-detail)
├─ Actions: "Close this worker" button integration with Phase 2.1c close mechanism
└─ Testing: Multi-user monitoring, stale data scenarios, large cohort performance
```

### Why This Sequence

1. **Auto-save is foundation:** Nothing works without it. High risk if done wrong (exam invalidation = data loss horror). Start here.
2. **Resume is dependent:** Meaningless without auto-save state. But unlocks the key UX win: "You lost connection; you can continue without restarting."
3. **Invalidation ties into Close Early:** Makes Close Early safe. You know answers are saved before forcing completion.
4. **Monitoring is independent** but valuable only after 1-3 are proven. Better to ship resilience first, then surveillance.

---

## Confidence Assessment

| Area | Level | Reason |
|------|-------|--------|
| **Auto-save requirements** | HIGH | Pattern verified across Moodle (official docs), Canvas (official docs), Exam.net, exam platform best practices. All converge on same approach. |
| **Session resume UX** | MEDIUM | Best practices are clear from proctoring/exam platforms. Specific wording varies. Used modal approach (Option A) as composite recommendation from strongest sources. |
| **Invalidation polling timing** | MEDIUM | 5-10s interval is standard from Respondus + enterprise exam monitoring. Exact choice is engineering trade-off (responsiveness vs server load). Not verified from single authoritative source. |
| **HC monitoring columns** | MEDIUM | Core 6 columns verified from AssessPrep + ProgressLearning docs. Optional columns are inferred from general HR dashboard patterns (not exam-specific). |
| **Visual feedback patterns** | HIGH | Toast/indicator/persistent-state feedback verified from design system standards (Carbon, Pajamas, NN Group). Auto-save feedback patterns match deployed systems. |

---

## Gaps & Phase-Specific Questions

**For Phase 2.1 requirements spec:**
1. Will you support pause/resume (teacher can pause active exam) or only disconnect/resume (network dropped)? Affects timer logic.
2. Should partial (unanswered) questions be saved, or only answered questions? Recommendation: save all selections, even partial/draft.
3. Maximum acceptable latency for auto-save? (Recommendation: < 2s on save completion, show "Saving..." if > 1s)
4. Should "Saved 2 min ago" timestamp be always visible, or only on hover/question hover? Recommendation: badge below submit button (always visible, reduces anxiety).
5. When resume modal appears, should worker see their saved answers on the last page, or start fresh? Recommendation: show saved answers read-only, let worker edit before continuing.

**For Phase 2.2 monitoring spec:**
1. Should HC be able to extend a worker's time mid-exam? (Recommendation: defer; implement as manual post-exam review + manual redo if needed)
2. Should worker see in-exam chat messages from HC? If yes, should they interrupt the exam? (Recommendation: yes to chat, no to interrupt; show notification in corner, worker reads after question)
3. Do you want per-question mastery indicators ("3/5 correct in reading section")? (Recommendation: defer; belongs in post-exam results, not live monitoring)

**For Phase 2.3+ planning:**
1. Real-time monitoring via WebSocket / Server-Sent Events? (Recommendation: evaluate after 2.2 ships; current 5-10s polling is good MVP)
2. Per-question animated save checkmarks? (Recommendation: nice-to-have; canvas timeout on slow networks)

---

## Key Sources

**Exam Platform Standards:**
- Moodle Quiz Settings documentation (official): https://docs.moodle.org/501/en/Quiz_settings
- Canvas Quiz Auto-Save & Resume (official): https://it.umn.edu/services-technologies/how-tos/canvas-students-resume-quiz-i-already
- Exam.net Session Resume (official): https://support.exam.net/s/article/conduct-exams-over-several-sessions-resume-exam

**Real-World Implementations:**
- AssessPrep Live Invigilation Dashboard: https://assessprep.zendesk.com/hc/en-us/articles/4841247686033
- ProgressLearning Live Monitoring: https://help.progresslearning.com/article/r64o44dacy-how-do-i-use-live-monitoring
- Digiexam Monitoring System: https://www.digiexam.com/exam-monitoring-software

**UX Standards:**
- NN Group Indicators/Validations/Notifications: https://www.nngroup.com/articles/indicators-validations-notifications/
- Toast Notifications Best Practices (LogRocket): https://blog.logrocket.com/ux-design/toast-notifications/
- Pajamas Design System Saving/Feedback: https://design.gitlab.com/patterns/saving-and-feedback/

**Network Resilience:**
- Citrix Auto-Reconnect Policy: https://docs.citrix.com/en-us/xenapp-and-xendesktop/7-15-ltsr/policies/reference/ica-policy-settings/
- ExamSoft Troubleshooting: https://support.examsoft.com/hc/en-us/articles/12155838507277-Exam-Troubleshooting-Guide

---

## Next Steps

1. **Requirements writing (Phase 2.1a):** Use EXAM-RESILIENCE-FEATURES.md as baseline. Specify auto-save interval, answer states, save strategy, error handling.
2. **Architecture review:** Validate timestamp handling, shuffle seed storage, auto-save debouncing strategy. Flag clock-skew risks early.
3. **QA planning:** Define disconnect/reconnect test scenarios (Fiddler proxy, network emulation, concurrent saves). Plan load testing for 5-10s polling under 1000+ concurrent users (HC monitoring).
4. **Phase 2.2 prep:** Begin UX wireframes for HC dashboard tabs. Validate 6-column layout. Consider real-time push architecture (optional, for later evaluation).

---

*Research completed for Portal HC KPB v2.1 Exam Resilience Features*
*Confidence: MEDIUM overall (HIGH on patterns, MEDIUM on edge cases & timing)*
*Recommend: All findings actionable; edge cases require engineering validation during architecture review*
