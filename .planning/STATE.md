# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-24)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v2.1 Assessment Resilience & Real-Time Monitoring — Phase 42 complete, Phase 43 next

## Current Position

**Milestone:** v2.1 Assessment Resilience & Real-Time Monitoring — IN PROGRESS
**Phase:** 43 of 44 (Worker Polling)
**Current Plan:** 1 of 2 complete (Plan 01 complete; Plan 02 at checkpoint — awaiting human verification)
**Next action:** Human verifies end-to-end: worker tab auto-redirects within 10-30s of HC clicking "Tutup Lebih Awal"
**Status:** Plan 01 complete (IMemoryCache registered + CMPController injected). Plan 02 Task 1 complete (interval changed to 10s, build passes). Checkpoint reached.
**Last activity:** 2026-02-25 — Phase 43 Plan 01 complete (IMemoryCache cache-aside); Plan 02 checkpoint:human-verify pending

Progress: [████████░░░░░░░░░░░░] 50% (v2.1 — 2/4 phases complete, Phase 41 ✓, Phase 42 ✓)

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

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

**v2.1 design decisions (from research):**
- SaveAnswer endpoint already exists — Phase 41 hardens it with atomic upsert (ExecuteUpdateAsync), not a rewrite
- CheckExamStatus endpoint already exists — Phase 43 adds setInterval wiring + memory cache on top of it
- [Phase 43-01]: Cache key is session-scoped (exam-status-{sessionId}), not user-scoped — ownership verified on every cache miss; non-owners short-circuit before cache key computed
- [Phase 43-01]: 5-second TTL collapses ~100 concurrent worker polls to 1 DB hit per 5s per session (~99% DB load reduction); TTL shorter than 10s poll interval ensures at most 1 DB hit per cycle
- [Phase 43-01]: CloseEarly invalidates cache immediately after SaveChangesAsync so next poll reflects closed status within the TTL window
- All four features use zero new NuGet packages — Fetch API, setInterval, IMemoryCache all already available
- Phase order is strictly dependency-driven: 41 (auto-save) → 42 (resume) → 43 (polling) → 44 (monitoring)
- Phase 44 monitoring uses a single GROUP BY query against PackageUserResponse — not N+1 per session
- Antiforgery: SaveAnswer/UpdateSessionProgress are POST (token required); CheckExamStatus/GetMonitoringProgress are GET (no token)

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

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-25
Stopped at: Completed 43-01-PLAN.md (Plan 01 complete). Phase 43 Plan 02 checkpoint:human-verify — Task 1 committed (a6f8500), awaiting human verification of end-to-end worker redirect flow.
Resume file: None.
