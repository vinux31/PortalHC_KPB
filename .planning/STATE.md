---
gsd_state_version: 1.0
milestone: v22.0
milestone_name: CMP-06 Residual Fix + CMP/Records + ManageAssessment/Monitoring Audit
status: Phase 345 SHIPPED LOCAL 4/4 — ready Phase 346
last_updated: "2026-06-04T06:44:26.251Z"
last_activity: 2026-06-04 -- Phase 345 complete (CMP06R-01..05; verifier 5/5 SC + 1 human-UAT pending PDF; code-review 0 critical)
progress:
  total_phases: 13
  completed_phases: 7
  total_plans: 26
  completed_plans: 25
  percent: 96
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-02)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 346 — cmp-records-detail-search-logic (Phase 345 COMPLETE)

## Current Position

Milestone: v22.0 🚀 ACTIVE (phases 345-349; spec 346/347 + 348/349)
Phase: 346 (cmp-records-detail-search-logic) — next (Phase 345 COMPLETE)
Plan: Not started (346 plans belum di-generate)
Status: Phase 345 SHIPPED LOCAL 4/4 — ready Phase 346
Last activity: 2026-06-04 -- Phase 345 complete (CMP06R-01..05; verifier 5/5 SC + 1 human-UAT pending PDF; code-review 0 critical)

Sumber: verifikasi Playwright + code sweep 2026-06-04 — bug Phase 337 CMP-06 (assessment Completed+IsPassed-null salah label "Fail") belum tuntas di 3 surface (RecordsWorkerDetail + UserAssessmentHistory + BulkExportPdf). Memory: project_cmp06_residual_recordsworkerdetail. Keputusan: label "Menunggu Penilaian" unified, passRate exclude pending.

Paralel outstanding: v21.0 Phase 344 (test+UAT) belum di-plan; v21.0 343 SHIPPED. v19.0+v20.0+v21.0 bundle pending push origin/main + IT promo Dev.

Predecessor: v20.0 ✅ SHIPPED LOCAL + ARCHIVED 2026-06-02 (39/39 REQ + 4/4 phase).

## Phase 340 Closure Snapshot

- **REQ delivered:** ORG-LABEL-01 (tabel + migration + seed) / ORG-LABEL-02 (service + cache) / ORG-LABEL-03 (controller endpoint) / ORG-LABEL-07 (tests + handoff)
- **Commits (10):** `e31db3c5` entity / `26ecd48a` DbSet+migration / `7e575f38` seed + D-12 fix / `92e20fbe` interface / `3a3aa3b9` service+DI+controller / `a6ba3514` plan02 summary / `43e94655` xUnit tests / `10ba5c5e` IT handoff HTML / `d631a686` plan01+03 summaries
- **Tests:** `dotnet test HcPortal.Tests` → 20/20 PASS (18 existing + 2 new TEST-01)
- **Live endpoint:** `GET /Admin/GetLevelLabels` → 200 `{"0":"Bagian","1":"Unit","2":"Sub-unit"}`
- **IT handoff:** `docs/DB_HANDOFF_IT_2026-06-03.html` ready forward Team IT (commit target now `d631a686`, migration flag YES)

## Next Action

1. **`/gsd-execute-phase 345 --interactive`** — eksekusi 4 plan v22.0 (CMP-06 residual fix); pre-plan artifacts (CONTEXT+RESEARCH+VALIDATION+UI-SPEC+PATTERNS) lengkap, checker PASS.
2. **Carry-over IT promo v19.0+v20.0+v21.0/340** — push bundle + Dev migration coordination tetap pending (paralel jalur).
3. **(Backlog housekeeping non-blocker):** Pre-existing Tom Select UX regression dari v20.0 audit defer ke v21.0 backlog (lihat v20.0-MILESTONE-AUDIT.md tech_debt).
4. **(Backlog housekeeping non-blocker):** v16.0+v17.0+v18.0 MILESTONES.md entries belum ditambah (defer batch retro).

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

## Quick Tasks Completed

| Date | Slug | Description |
|------|------|-------------|
| 2026-05-26 | cdp-portal-platform-rename | Rename CDP label "Competency Development Portal" → "Platform" (parity dgn CMP). 4 edit di Views/CDP/Index.cshtml + Views/Home/Index.cshtml. |

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
- Phase 323 added (2026-05-26): Fix cascade bug AssessmentEditLogs di 3 endpoint delete assessment — Phase 321 oversight di Phase 312 cascade. `AssessmentEditLog` (Phase 321) punya FK Restrict ke `AssessmentSession`, tapi `DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup` (Phase 312) tidak cascade hapus. Repro Dev: AssessmentSession Id 1 (0 edit logs) sukses; Id 2+5 (ada edit logs) gagal "Gagal menghapus assessment". Scope: tambah `RemoveRange(AssessmentEditLogs)` block sebelum cascade existing di 3 endpoint di `Controllers/AssessmentAdminController.cs`. Tidak ubah schema/model/migration.
- Phase 324 added (2026-05-26): Fix duplicate TrainingRecord auto-create on assessment completion — regression dari commit `766011b6` (2026-04-10) yang re-introduce auto-create TrainingRecord di `GradingService.GradeAndCompleteAsync` setelah sebelumnya di-remove oleh `79284609` (2026-03-18) karena "caused duplicate entries in RecordWorkerDetail unified view". Worker submit 1 assessment biasa (non-essay/non-PreTest) → DB store 1 AssessmentSession + 1 TrainingRecord (Judul=`Assessment: {Title}`). `WorkerDataService.GetUnifiedRecords` query keduanya → user lihat 2 row untuk 1 event. Scope: hapus auto-create di `GradingService.cs:255-285`, `AssessmentAdminController.cs:3404-3421` (FinalizeEssayGrading), `GradingService.cs:483-562` (RegradeAfterEditAsync TR cascade); cleanup TR legacy `Judul LIKE 'Assessment:%'` antara 2026-04-10..hari ini (backup DB lokal dulu per SEED_WORKFLOW).
- Phase 329 added (2026-05-28): fix-cascade-deleteassessmentgroup-deleteprepostgroup-renewal-precheck — Pasang pre-check renewal chain (RenewsSessionId) di DeleteAssessmentGroup (AssessmentAdminController.cs:2199) + DeletePrePostGroup (AssessmentAdminController.cs:2359) sebelum BeginTransactionAsync, paralel pola Phase 325 P05 DeleteAssessment L2040-2052. Source Phase 328 RESEARCH.md §4.4 + §4.5 (HIGH D5 fail). Severity HIGH. Effort S (~40 LoC delta 1 controller, no migration). Depends on Phase 328 (audit deliverable).
- Phase 330 added (2026-05-28): fix-cascade-med-bundle-delete-category-package-question-orgunit-notification — Fix MED cascade safety: DeleteCategory + DeletePackage + DeleteQuestion + DeleteOrganizationUnit (TrainingAdminController) + NotificationService.DeleteAsync. Pre-check FK renewal/child referrer sebelum tx scope. Source Phase 328 RESEARCH.md §9 proposal #7. Severity MED. Effort S-M (~60-80 LoC delta 2 file, no migration). Depends on Phase 329.
- Phase 328 added (2026-05-27): Cascade Audit Sweep — Delete* Endpoints (Audit-Only). Post-Phase-323 deferred follow-up per `323-CONTEXT.md:122`. Enumerate semua `Delete*` method di `Controllers/*.cs` + `Services/*.cs`, audit terhadap 7-dim cascade-safety checklist (FK risk, file-DB atomicity, audit log, role check, renewal chain null-clear, error handling, transaction wrap). Severity tag per row (HIGH/MED/LOW). Deliverable `328-RESEARCH.md` only. No code change, no fix phase spawn (separate user decision post-audit). Pre-audit HIGH finding confirmed: renewal chain bug di `DeleteTraining` (`TrainingAdminController.cs:527-548`) + `DeleteManualAssessment` (`:736-756`). Spec: `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` commit `02f620be`.
- Phase 331 added (2026-05-28): fix-cascade-deletetraining-deletemanualassessment-atomicity — DeleteTraining + DeleteManualAssessment (TrainingAdminController.cs:559 + :793): wrap BeginTransactionAsync + move File.Delete POST commit (D2+D7). D5 sudah Phase 325 P05 covered. Source Phase 328 RESEARCH.md §4.1 + §4.2 + §9 proposal #1. Severity HIGH. Effort S-M (~80 LoC delta 1 controller, no migration). Depends on Phase 330.
- Phase 332 added (2026-05-28): fix-cascade-deletebagian-file-atomicity — DeleteBagian (DocumentAdminController.cs:283): wrap BeginTransactionAsync + move File.Delete L327+L343 POST SaveChanges + try/catch DbUpdateException (D2+D6+D7). Pertahankan pre-check active files BLOCK L289-302. Source Phase 328 RESEARCH.md §4.7 + §9 proposal #3. Severity HIGH. Effort S-M (~50 LoC delta 1 controller, no migration). Depends on Phase 331.
- Phase 333 added (2026-05-28): fix-cascade-deletecoachingsession-file-atomicity — DeleteCoachingSession (CDPController.cs:2433): move File.Delete loop L2490-2503 (evidence EvidencePath + history) POST tx.CommitAsync L2538 + refactor catch L2540 jangan raw 500 throw (D2 fix + D6 polish). Pertahankan progress state revert logic L2505-2517 + active-mapping guard L2441-2453. Source Phase 328 RESEARCH.md §4.6 + §9 proposal #4. Severity HIGH. Effort M. Depends on Phase 332.
- Phase 334 added (2026-05-28): fix-cascade-deletekompetensi-orphan-evidence-files — DeleteKompetensi (ProtonDataController.cs:1516): iterate nested SubKompetensi → Deliverable → Progress tree, collect EvidencePath + History (JsonDocument parse), File.Delete POST CommitAsync L1576 + refactor catch L1584 jangan return ex.Message ke client (D2 fix + D6 info leak fix). Pertahankan BeginTransactionAsync L1529. Source Phase 328 RESEARCH.md §4.8 + §9 proposal #5. Severity HIGH. Effort M. Depends on Phase 333.
- Phase 335 added (2026-05-28): fix-cascade-deleteworker-renewal-files-tx — DeleteWorker (WorkerController.cs:487): triple-fix D2+D5+D7. (D2) Loop TR/AS + Proton progress milik user, collect file paths SertifikatUrl/ManualSertifikatUrl/EvidencePath, File.Delete POST commit. (D5) Pre-check TR/AS user di-referensi sebagai RenewsTrainingId/RenewsSessionId source untuk worker LAIN — cross-user pattern (BUKAN same-user Phase 325 P05) → block atau null-clear. (D7) BeginTransactionAsync wrap full 9-step RemoveRange cascade INCLUDING UserManager.DeleteAsync interaction (Identity store separate SaveChanges path). Solo phase, BUKAN bundle. Manual UAT 5+ scenario. Source Phase 328 RESEARCH.md §4.3 + §9 proposal #6. Severity HIGH. Effort L (~200-300 LoC). Depends on Phase 334.

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
