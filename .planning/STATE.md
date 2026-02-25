# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-24)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v2.1 SHIPPED — ready for next milestone

## Current Position

**Milestone:** v2.1 Assessment Resilience & Real-Time Monitoring — COMPLETE ✅
**Phase:** 44 of 44 — COMPLETE
**Current Plan:** None — all plans complete, milestone shipped.
**Next action:** Define next milestone
**Status:** v2.1 fully shipped 2026-02-25. All 4 phases (41–44) complete.
**Last activity:** 2026-02-25 — Phase 44 complete, v2.1 milestone shipped

Progress: [████████████████████] 100% (v2.1 — Phase 41 ✓, Phase 42 ✓, Phase 43 ✓, Phase 44 ✓)

## Performance Metrics

**Velocity (v1.0–v2.0):**
- Total plans completed: 57+
- Average duration: ~4 min/plan

*Updated after each plan completion*

| Phase | Duration | Notes |
|-------|----------|-------|
| Phase 38-auto-hide-filter P01 | 3min | 2 tasks, 1 file |
| Phase 39-close-early P01 | 5min | 1 task, 1 file |
| Phase 39-close-early P02 | ~25min | 2 tasks + 3 fixes, 3 files |
| Phase 40-history-tab P01 | 8min | 2 tasks, 3 files |
| Phase 40-history-tab P02 | ~5min | 2 tasks + 1 checkpoint, 1 file |
| Phase 41-auto-save P01 | 2min | 2 tasks, 5 files |
| Phase 41-auto-save P02 | ~12min | 3 tasks + human checkpoint, 2 files |
| Phase 42-session-resume P01 | 2min | 2 tasks | 5 files |
| Phase 42-session-resume P02 | 3min | 2 tasks, 1 file |
| Phase 42-session-resume P03 | 2min | 2 tasks, 2 files |
| Phase 42-session-resume P04 | ~40min | 1 checkpoint + 4 bug fixes, 3 files |
| Phase 43-worker-polling P01 | 2min | 2 tasks, 2 files |
| Phase 43-worker-polling P02 | ~10min | 2 tasks + 1 bug fix, 2 files |
| Phase 44-real-time-monitoring P01 | ~5min | 1 task, 1 file |
| Phase 44-real-time-monitoring P02 | ~15min | 2 tasks + human checkpoint, 1 file |
| Phase 44 verification fixes | ~30min | Actions remap (3 files), resume modal formula, failure toast logic/position |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

**v2.1 design decisions (from research):**
- SaveAnswer endpoint already exists — Phase 41 hardens it with atomic upsert (ExecuteUpdateAsync), not a rewrite
- CheckExamStatus endpoint already exists — Phase 43 adds setInterval wiring + memory cache on top of it
- [Phase 43-01]: Cache key is session-scoped (exam-status-{sessionId}), not user-scoped — ownership verified on every cache miss; non-owners short-circuit before cache key computed
- [Phase 43-01]: 5-second TTL collapses ~100 concurrent worker polls to 1 DB hit per 5s per session (~99% DB load reduction); TTL shorter than 10s poll interval ensures at most 1 DB hit per cycle
- [Phase 43-01]: CloseEarly invalidates cache immediately after SaveChangesAsync so next poll reflects closed status within the TTL window
- [Phase 43-02]: 10s poll interval (not 30s) — workers detect HC early close within 10-20s; saveSessionProgress stays at 30s (unchanged)
- [Phase 43-02]: Results page ROW_NUMBER() full scan replaced with separate Questions/Responses loads for legacy path — avoids full scan timeout on large datasets
- All four features use zero new NuGet packages — Fetch API, setInterval, IMemoryCache all already available
- Phase order is strictly dependency-driven: 41 (auto-save) → 42 (resume) → 43 (polling) → 44 (monitoring)
- Phase 44 monitoring uses a single GROUP BY query against PackageUserResponse — not N+1 per session
- Antiforgery: SaveAnswer/UpdateSessionProgress are POST (token required); CheckExamStatus/GetMonitoringProgress are GET (no token)
- [Phase 44-01]: GetMonitoringProgress status priority: Completed (CompletedAt!=null OR Score!=null) > Abandoned > InProgress (StartedAt!=null) > "Not started" — lowercase 's' matches AssessmentMonitoringDetail
- [Phase 44-01]: remainingSeconds = Math.Max(0, (DurationMinutes*60) - ElapsedSeconds) for InProgress only; null for all other statuses
- [Phase 44-01]: result maps IsPassed (bool?) to "Pass"/"Fail"/null
- [Phase 44-01]: Single GROUP BY query for answered counts (not N+1 per session); package mode detection via AssessmentPackages.CountAsync
- [Phase 44-02]: isPackageMode detected client-side via document.getElementById('reshuffleForm') !== null — avoids embedding Razor in unconditional script block
- [Phase 44-02]: #antiforgeryForm (outside all @if blocks) provides token to JS-rendered Reset/ForceClose — #reshuffleForm (package-mode only) not used for monitoring actions
- [Phase 44-02]: Initial Progress cell shows —/N not 0/N — polling fires immediately on page load so answered count updates before user notices
- [Phase 44-02]: tds[1]–tds[7] column mapping fixed by 8-column thead: Name(0), Progress(1), Status(2), Score(3), Result(4), CompletedAt(5), TimeRemaining(6), Actions(7)

**v2.0 design decisions (relevant carry-forward):**
- [Phase 39-02]: SaveAnswer uses explicit session-owner check (Json error), not [Authorize(Roles)]
- [Phase 39-02]: CheckExamStatus is a plain GET with no antiforgery — read-only JSON
- [Phase 39-02]: 30s poll interval shipped in v2.0 — Phase 43 tightens to 10-30s with caching
- [Phase 41-auto-save]: ExecuteUpdateAsync + conditional Add pattern used for atomic upsert — avoids EF change tracking race condition on concurrent auto-saves
- [Phase 41-auto-save]: UNIQUE DB constraint on PackageUserResponse(AssessmentSessionId, PackageQuestionId) as safety net for extreme concurrency
- [Phase 41-auto-save]: SaveLegacyAnswer targets UserResponse (not PackageUserResponse) — consistent with legacy exam scoring path established in Phase 39
- [Phase 42-01]: ElapsedSeconds is non-nullable int with DEFAULT 0 — no null check needed in backend, clean accumulation
- [Phase 42-01]: LastActivePage and SavedQuestionCount are nullable int — null signals pre-Phase-42 session, avoiding data migration of live records
- [Phase 42-02]: isResume = assessment.StartedAt != null — first-load safe because ElapsedSeconds=0 and LastActivePage=null/0 prevent resume modal (modal requires IsResume && LastActivePage > 0)
- [Phase 42-02]: RemainingSeconds = (DurationMinutes * 60) - ElapsedSeconds — offline time is excluded from exam duration by design
- [Phase 42-03]: btn-warning (yellow) for Resume button; <a asp-action> not JS button — resume modal fires on StartExam load, not card click
- [Phase 42-03]: RESUME_PAGE > 0 gates resume modal — page 0 resume is silent (worker was on first page)
- [Phase 42-03]: prePopulateAnswers runs first in init block — answeredCount badge correct before modal renders
- [Phase 42-03]: Failure toast "Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. X." (1-based X) per locked user decision
- [Phase 42-03]: EXAM_EXPIRED path: modal + OK-click submit + 5s fallback; window.onbeforeunload = null before auto-submit

### Roadmap Evolution

- Phase 45 added: Cross-Package Per-Position Shuffle (replaces single-package assignment with per-slot random cross-package selection)

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-25
Stopped at: v2.1 complete. All phases shipped. Awaiting next milestone definition.
Resume file: None.
