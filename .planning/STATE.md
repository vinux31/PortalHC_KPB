---
gsd_state_version: 1.0
milestone: v15.0
milestone_name: Audit Findings 27 April 2026
status: executing
last_updated: "2026-04-28T10:19:44.245Z"
last_activity: 2026-04-28
progress:
  total_phases: 3
  completed_phases: 2
  total_plans: 6
  completed_plans: 5
  percent: 83
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-28)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 306 — score-editable-per-question-type

## Current Position

Phase: 306 (score-editable-per-question-type) — EXECUTING
Plan: 2 of 2
Status: Plan 01 complete — Plan 02 ready to execute
Last activity: 2026-04-28 -- Phase 306 Plan 01 complete (server-side validation + audit log)
Resume file: .planning/phases/306-score-editable-per-question-type/306-02-PLAN.md

## Next Action

Jalankan `/gsd-execute-phase 306` untuk eksekusi Plan 02 (View + Modal + UAT) — JSON GET sudah expose `affectedSessions` field, siap dikonsumsi client-side `populateEditForm`.

## v15.0 Phase Roadmap (lihat ROADMAP.md untuk detail success criteria)

| Phase | Goal | REQ | Wave |
|-------|------|-----|------|
| 304 | UI Label Polish (Login + WIB) | AUTH-01, WIZ-02, WIZ-03 | 1 (Low risk) |
| 305 | Question Type Naming Clarity | LBL-01 | 1 (Low+docs) |
| 306 | Score Editable per Question Type | QSCR-01 | 2 (Medium) |
| 307 | Selected Participants Inline View | WIZ-01 | 2 (Low) |
| 308 | PrePost Wizard Validation Fix | WIZ-04 | 2 (Medium) |
| 309 | Worker Certificate Defensive Fix | WCRT-01 | 3 (Med-High, parallel w/310) |
| 310 | Essay Finalize Idempotency | ESCG-01 | 3 (Med-High, parallel w/309) |
| 311 | ManageAssessment Performance | PERF-01 | 4 (Med, measurement-driven) |

## Deferred Items

### v15.0 Deferred (current milestone)

| REQ | Item | Status | Due |
|-----|------|--------|-----|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | menunggu user verifikasi save/load Rubrik | 2026-05-12 |

### Carry-over dari v14.0 close (2026-04-24)

| Category | Item | Status | Source |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | paused-at-checkpoint | HANDOFF.json (2026-04-10) |
| UAT | Phase 235 — 5 items butuh human verification via browser | pending | STATE.md (prior) |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notification) | pending — overlap risk dengan Phase 310 (T9 NotifyIfGroupCompleted) | STATE.md (prior) |
| Research gap | Phase 297 Pre-Post Renewal behavior — keputusan 2 sesi baru otomatis | undecided | v14.0 planning |
| Research gap | Phase 298 essay max character limit — nvarchar(max) vs nvarchar(2000) | undecided | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | undecided | v13.0 carry-over |
| v11.2 paused | Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page) | paused | MILESTONES.md v11.2 |

Total: 7 carry-over deferred items + 1 v15.0 deferred (EPRV-01) = 8 tracked items.

## Accumulated Context

### Decisions (persist across milestones)

- [v14.0 / Phase 296]: GradeFromSavedAnswers dihapus — GradingService adalah satu-satunya source of truth untuk grading
- [v14.0 / Phase 301]: Export endpoints re-query database independen (tidak share state dengan API endpoints)
- [v14.0 / Phase 302]: A11Y-03 (screen reader) & A11Y-04 (font size controls) di-drop per D-18/D-19
- [v14.0 / Phase 303]: Chart.js v4 `indexAxis:'y'` untuk horizontal bar (bukan v2 horizontalBar)
- [v14.0 / Phase 303]: Auto-suggest coach via `data-section` attribute, tanpa server round-trip
- [v13.0]: SortableJS 1.15.7 via CDN; drag-drop sibling-only (group: false); orgTree.js single JS orchestrator
- [v12.0]: AdminController dipecah menjadi 8 controller per domain; URL tetap via [Route] attribute
- [Phase 292]: IsAjaxRequest() sebagai protected method di AdminBaseController; dual-response pattern
- [v15.0 / Phase 306 / Plan 01]: Replace force-override `scoreValue=10` MC/MA dengan inline range check 1-100 server-side (D-12, D-13, D-14) — defense in depth, tahan DevTools bypass
- [v15.0 / Phase 306 / Plan 01]: AuditLog `EditQuestion-ScoreChange` dengan format `oldScore → newScore (N sessions affected)` literal arrow U+2192, dibungkus try/catch dengan _logger.LogWarning fallback (D-10, T-306-02 mitigation)
- [v15.0 / Phase 306 / Plan 01]: AuditLog `CreateQuestion-CustomScore` saat scoreValue != 10 (D-11, CD-05) — informational audit untuk non-default score
- [v15.0 / Phase 306 / Plan 01]: EditQuestion AJAX GET extends JSON dengan `affectedSessions` field (Distinct().CountAsync() per AssessmentSessionId) untuk Plan 02 modal trigger (D-09)

### Open Blockers/Concerns

- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level, unit Level 2+ tidak muncul di dropdown ManageWorkers secara diam-diam (keputusan masih tertunda)

## Session Continuity

Last activity: 2026-04-24 — Closed milestone v14.0 Assessment Enhancement (8 phases, 23 plans shipped)
Next action: Jalankan `/gsd-new-milestone` untuk memulai v15.0 (kandidat: Performance Deep Check, batch UAT closeout, atau reaktivasi v11.2)
