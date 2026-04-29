---
gsd_state_version: 1.0
milestone: v15.0
milestone_name: Audit Findings 27 April 2026
status: paused-at-checkpoint
last_updated: "2026-04-29T01:42:00.000Z"
last_activity: 2026-04-29 -- Phase 307 Plan 02 Wave 1 Tasks 1-3 complete; paused at Task 4 checkpoint:human-verify (manual UAT 5-step)
progress:
  total_phases: 4
  completed_phases: 4
  total_plans: 8
  completed_plans: 8
  percent: 100
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-28)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 307 — selected-participants-inline-view

## Current Position

Phase: 307 (selected-participants-inline-view) — PAUSED AT CHECKPOINT
Plan: 2 of 2 (Wave 1 Tasks 1-3 complete, Task 4 checkpoint:human-verify pending)
Status: Awaiting manual UAT 5-step Bahasa Indonesia
Last activity: 2026-04-29 -- Phase 307 Plan 02 Tasks 1-3 implemented (3 commits a4b90ff5, ad7fa210, 7d81eecf); paused at Task 4
Resume file: .planning/phases/307-selected-participants-inline-view/307-02-SUMMARY.md

## Next Action

Phase 307 Plan 02 Tasks 1-3 selesai (panel "Peserta Terpilih" Step 2 implemented — markup + 3 top-level helpers + populateSummary refactor + Proton listener replace + AJAX hydrate + reset handler edit). 3 commits atomic, 0 build errors, all grep-based acceptance criteria pass.

**Task 4 PENDING — checkpoint:human-verify gate:blocking:** User/orchestrator perlu jalankan manual UAT 5-step Bahasa Indonesia di `.planning/phases/307-selected-participants-inline-view/307-UAT.md` dengan dev server running di `localhost:5277`. Required evidence: performance delta < 200ms screenshot (Step 4) + Step 2/Step 4 side-by-side parity screenshot (Step 5) + filled sign-off section di UAT.md.

**Pre-UAT smoke E2E:** `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --reporter=list` — 4 tests Phase 307 sebelumnya RED, expect transisi ke GREEN. FLOW 1 test 1.2 (`'2 terpilih'` post Wave 0 rot fix) tetap PASS.

Setelah UAT PASS, lanjut ke `/gsd-verify-work` untuk close Phase 307.

## v15.0 Phase Roadmap (lihat ROADMAP.md untuk detail success criteria)

| Phase | Goal | REQ | Wave |
|-------|------|-----|------|
| 304 | UI Label Polish (Login + WIB) | AUTH-01, WIZ-02, WIZ-03 | 1 (Low risk) |
| 305 | Question Type Naming Clarity | LBL-01 | 1 (Low+docs) |
| 306 | Score Editable per Question Type | QSCR-01 | 2 (Medium) |
| 307 | Selected Participants Inline View | WIZ-01 | 2 (Low) |
| 308 | PrePost Wizard Validation Fix | WIZ-04 | 2 (Medium) |
| 309 | Worker Certificate Defensive Fix + Submitted Status Handling | WCRT-01, SUB-01 | 3 (Med-High, parallel w/310) |
| 310 | Essay Finalize Idempotency | ESCG-01 | 3 (Med-High, parallel w/309) |
| 311 | ManageAssessment Performance | PERF-01 | 4 (Med, measurement-driven) |
| 312 | Admin Full-Delete Assessment Room | DEL-01 | 5 (Med, parallel-eligible) |
| 313 | Block Manual Submit Saat Waktu Habis | TMR-01 | 5 (Med-High, parallel-eligible) |
| 314 | Fix Regenerate Token Upcoming | TKN-01 | 5 (Low-Med, investigative) |

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
- [v15.0 / Phase 306 / Plan 02]: Score input default-enabled untuk semua tipe MC/MA/Essay; JS dynamic disabled/reset/help-text logic dihapus — single source of truth = Razor static `Range 1–100` (D-03, D-04, D-05)
- [v15.0 / Phase 306 / Plan 02]: Modal `editScoreWarningModal` trigger condition = editMode AND scoreChange AND affectedSessions > 0 (informational only); confirm button class `btn-warning btn-sm` per CD-D-08 UI-SPEC override (D-06, D-08, D-09, CD-02)
- [v15.0 / Phase 306 / Plan 02]: Header daftar soal format `(N soal • Total X poin)` dengan bullet U+2022; computed via `@questions.Sum(q => q.ScoreValue)` (D-17, CD-03)
- [v15.0 / Phase 306 / Plan 02]: D-19 verified via UAT — Stored AssessmentSessions.Score di Completed sessions TIDAK auto-recalculate setelah admin edit; Score di-persist saat SubmitExam, modal warning informasional only
- [v15.0 / Phase 307 / Plan 01]: Selectors helper module placed di `tests/e2e/helpers/wizardSelectors.ts` (NEW folder), bukan `tests/helpers/`, untuk separation e2e-specific selectors vs shared utilities (login/utils/accounts)
- [v15.0 / Phase 307 / Plan 01]: Performance budget #4 + Step 2/Step 4 visual parity di-defer ke manual UAT karena E2E flaky di CI runner — Step 4 punya `performance.mark/measure` script copy-paste-able ke Console
- [v15.0 / Phase 307 / Plan 01]: Opportunistic rot fix line 45 (`'2 selected'` → `'2 terpilih'`) applied di Wave 0 — match production text Bahasa Indonesia di `CreateAssessment.cshtml` line 289 default `'0 terpilih'` dan `updateSelectedCount` line 1446 format `count + ' terpilih'`

### Open Blockers/Concerns

- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level, unit Level 2+ tidak muncul di dropdown ManageWorkers secara diam-diam (keputusan masih tertunda)

## Session Continuity

Last activity: 2026-04-29 — Phase 307 Plan 01 (Wave 0 test scaffold) complete. 3 commits (37c4bb90 selector helper, a3bdd04f assessment.spec.ts extension + rot fix, e88879ce manual UAT 5-step). 4 new Playwright tests (7.1-7.4) ready RED state untuk Wave 1 GREEN cycle.
Next action: Jalankan `/gsd-execute-phase 307` Plan 02 (Wave 1 implementasi actual panel "Peserta Terpilih" — markup, helper render function, reset handler edit, Proton mode switch).
