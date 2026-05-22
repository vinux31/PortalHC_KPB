---
gsd_state_version: 1.0
milestone: null
milestone_name: null
status: milestone-closed
last_updated: "2026-05-23T00:00:00.000Z"
last_activity: 2026-05-23 -- v17.0 milestone ARCHIVED (Phase 320+321+322 SHIPPED, tag v17.0 created, REQUIREMENTS.md cleared for next milestone)
progress:
  total_phases: 0
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-23)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** No active milestone — v17.0 closed 2026-05-23. Next: `/gsd-new-milestone` untuk start v18.0 atau promote backlog item.

## Current Position

Phase: — (no active milestone)
Status: v17.0 milestone CLOSED + archived ke `.planning/milestones/v17.0-ROADMAP.md`
Last activity: 2026-05-23 -- v17.0 archived (3/3 phase SHIPPED, 24/24 REQ delivered, 2 post-shipping CSS dead-code fix applied, tag v17.0)

## Next Action

1. **User action:** Notify IT — commit hash `202ce331` (push 2026-05-22) + 3 follow-up commit `b0b4049b` + `3cdccfb4` + `13046757` (post-shipping CSS dead-code fix + UAT amend) + tag `v17.0` (milestone close). NO migration flag. Draft di `322-UAT.md` Handoff section.
2. **Push milestone close commits:** `git push origin main && git push origin v17.0` (tag created during milestone close).
3. **Next milestone:** `/gsd-new-milestone` untuk start v18.0 — atau promote backlog item dulu (EPRV-01 v15.0 + 7 carry-over v14.0).
4. **Backlog housekeeping (non-blocker):** v16.0 milestone (Phases 315-319, shipped 2026-05-12) sudah di-archive di `milestones/v16.0-ROADMAP.md` tapi belum punya entry di `MILESTONES.md` log. Optional — tambah saat sempat.

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
- Phase 322 added (2026-05-22): filter-scope-per-tab-manage-assessment — fix double filter di tab Assessment Groups (dev) + filter contamination antar tab (Phase 311 Plan 02 shared filter rollback). Per-tab native filter: Tab 1 (search+kategori+status), Tab 2 (bagian+unit+kategori-training+status+nama/nopeg), Tab 3 sub-tab History masing-masing punya filter client-side (Riwayat Assessment + Riwayat Training).

## Session Continuity

Last activity: 2026-05-22 — Phase 322 SHIPPED via /gsd-execute-phase 322 --interactive (Playwright automated UAT). 13 commit total: 6 feat (322-01..06) + 2 fix critical post-UAT discovery (`6ecb7a50` ViewBag null coalesce + `773c970c` wrapper hx-vals → URL migration — HTMX hx-vals inheritance gotcha) + 5 docs (UAT + 3 SUMMARY). UAT 11/12 PASS + 1 N/A (Step 3 pagination — DB lokal 1 grup insufficient; bonus fix verified via code review).

Phase 322 deliverables:
- Bug 1 (double filter Tab 1): FIXED via shell shared form delete
- Bug 2 (cross-tab contamination): PREVENTED by-design via D-21 Strategy D Hybrid (URL query string per-wrapper, NOT hx-vals which inherits to descendants)
- Bug 3 (pagination filter state): bonus fix via hx-include=#filterFormAssessment
- Riwayat Training filter NEW (parity sama Riwayat Assessment)
- D-10 URL bookmark backward compat preserved

Next action: User confirm finishing actions (tag v17.0-p322-complete + push origin/main). v17.0 milestone 3/3 phases SHIPPED — ready /gsd-complete-milestone untuk prep v18.0.

Key learning untuk depan: HTMX hx-vals attribute INHERITS dari ancestor ke descendant HTMX triggers (override descendant form data). Solution wrapper-only params: bake ke hx-get URL query string. Solution user-driven params (form/dropdown): descendant pakai hx-include="closest form" tanpa ancestor hx-vals.
