---
gsd_state_version: 1.0
milestone: v16.0
milestone_name: QA Test Coverage
status: executing
last_updated: "2026-05-11T11:00:00.000Z"
last_activity: 2026-05-11
progress:
  total_phases: 5
  completed_phases: 2
  total_plans: 11
  completed_plans: 11
  percent: 40
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-11)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 317 — fix-surf-316-a-ma-essay-mixed-e2e-via-ui

## Current Position

Phase: 317
Plan: Not started (plan draft di `~/.claude/plans/phase-315-dan-316-streamed-llama.md` — 8 tasks)
Status: Executing Phase 317 (Tasks 1+2 done outside planner; Tasks 3-8 pending)
Last activity: 2026-05-11
Resume file: TBD (Phase 317 belum punya CONTEXT.md formal)

## Next Action

1. **Recommended:** `/gsd-plan-phase 317` — convert plan draft jadi formal phase plans (split Task 3-7 jadi exam-types.spec.ts plans; Task 8 jadi regression smoke plan)
2. **Alternatively:** Lanjut execute langsung tanpa planner — buat `tests/e2e/exam-types.spec.ts` FLOW K (MA full cycle) sebagai first wave
3. **Defer:** Phase 318+319 plan (preview only di ROADMAP) — activate setelah 317 selesai

## Deferred Items

### v15.0 Deferred (carry-over ke v16.0+)

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
- [v15.0 / Phase 306]: Score editable MC/MA/Essay range 1-100 server-side; AuditLog `EditQuestion-ScoreChange` dengan format `oldScore → newScore (N sessions affected)`; modal warning informasional only (Stored AssessmentSessions.Score di Completed sessions TIDAK auto-recalculate)
- [v15.0 / Phase 307]: Selectors helper di `tests/e2e/helpers/wizardSelectors.ts` (NEW folder), bukan `tests/helpers/`, untuk separation e2e-specific selectors vs shared utilities
- [v15.0 / Phase 311]: ManageAssessment performance bottleneck = proxy wifi kantor (bukan backend); HTMX lazy load + AsNoTracking + 2 EF index + IMemoryCache Categories TTL 5min
- [v15.0 / Phase 313.1]: Helper module `tests/e2e/helpers/exam313.ts` (4 function exports flat); Race-tolerant Tier-2 assertion via `Promise.race`; Tier-1 manual reject TRUE end-to-end TIDAK UI-testable (server-side StartExam redirect ExamSummary)

### Open Blockers/Concerns

- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level, unit Level 2+ tidak muncul di dropdown ManageWorkers secara diam-diam (keputusan masih tertunda)

### Roadmap Evolution

- Phase 316 added (2026-05-11): Fix SubmitExam page-closed bug + matrix test infra polish — resolve cascade fail dari Phase 315 yang block sentinel S8/S9/S10 verification
- Phase 317-319 added (2026-05-11): Extend v16.0 dari 2 → 5 phases untuk close exam-type coverage gap (317: MA/Essay/Mixed via HC UI), advanced features (318: PreTest/PostTest, ExamWindowCloseDate, Certificate PDF), admin coverage (319: ManualAssessment, Export, Analytics, CertificationManagement)

## Session Continuity

Last activity: 2026-05-11 — Phase 316 complete (11/11 matrix tests passed). Phase 317 started outside planner: Task 1 fix SURF-316-A done (examMatrix.ts submit selector + 2-step flow), Task 2 matrix smoke validation done (sibling-pool root cause resolved via seed SQL peserta2 packages removed + Layer 1 counts updated). Bonus helper hardening done (essay saveIndicator text check, gradeEssaysAsHc data-session-id targeting, verifyResultPage badge selector).

Next action: Commit current changes (SURF-316-A fix + seed SQL fix + helper hardening + ROADMAP/STATE updates). Then `/gsd-plan-phase 317` untuk formal split Task 3-8 jadi plans, ATAU lanjut execute manual buat `tests/e2e/exam-types.spec.ts` (FLOW K MA full cycle first).
