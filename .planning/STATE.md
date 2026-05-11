---
gsd_state_version: 1.0
milestone: v16.0
milestone_name: QA Test Coverage
status: v16.0 defined, REQUIREMENTS.md fresh, ROADMAP.md updated, Phase 315 ready untuk discuss/plan
last_updated: "2026-05-11T01:39:47.558Z"
last_activity: 2026-05-11 — v16.0 milestone definition (streamlined, skip research). Spec lengkap di docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md (commit 94bacecf) sudah ada — akan jadi input CONTEXT.md
progress:
  total_phases: 1
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-11)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v16.0 QA Test Coverage — Phase 315 Assessment Matrix Test (PLANNING)

## Current Position

Phase: 315 (Assessment Matrix Test) — PLANNING
Plan: — (not yet generated, see Next Action)
Status: v16.0 defined, REQUIREMENTS.md fresh, ROADMAP.md updated, Phase 315 ready untuk discuss/plan
Last activity: 2026-05-11 — v16.0 milestone definition (streamlined, skip research). Spec lengkap di docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md (commit 94bacecf) sudah ada — akan jadi input CONTEXT.md
Resume file: .planning/phases/315-assessment-matrix-test/315-CONTEXT.md

## Next Action

1. **Recommended:** `/gsd-discuss-phase 315` — gather context Phase 315 (akan baca spec di `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` dan resolve 5 open questions: MA save flow, Essay save flow, Notes field, ID collision check, URL encoding)
2. **Alternatively:** `/gsd-plan-phase 315` — skip discuss, plan langsung (kalau yakin spec sudah lengkap, pakai sebagai CONTEXT.md langsung)
3. After plan: `/gsd-execute-phase 315` — eksekusi atomic per plan

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

## Session Continuity

Last activity: 2026-05-11 — v15.0 milestone closed via `/gsd-complete-milestone`. ROADMAP.md collapsed v15.0 ke shipped section, MILESTONES.md entry diperbaiki dari accomplishments dirty hasil parser failure, phase dirs (304-314 + 313.1) dipindah ke `.planning/milestones/v15.0-phases/`. PROJECT.md Current Milestone reset ke "(none — defining next)". STATE.md di-rewrite untuk between-milestones state.

Next action: Jalankan `/gsd-new-milestone v16.0 QA Test Coverage` (streamlined — skip research). Phase pertama akan pakai spec `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` sebagai input CONTEXT.md.
