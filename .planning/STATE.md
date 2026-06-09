---
gsd_state_version: 1.0
milestone: v24.0
milestone_name: Gambar di Soal Assessment (Manage Package)
status: executing
last_updated: "2026-06-09T02:14:20.737Z"
last_activity: 2026-06-09
progress:
  total_phases: 20
  completed_phases: 3
  total_plans: 13
  completed_plans: 12
  percent: 92
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-06-06)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 355 — Test & UAT

## Current Position

Milestone: v24.0 — Gambar di Soal Assessment (Manage Package) — Phase 352 ✅ SHIPPED LOCAL; Phase 353 ⏸ PAUSED mid-discuss 2026-06-06
Phase: 355 (Test & UAT) — EXECUTING
Plan: 3 of 3
Status: Ready to execute
Last activity: 2026-06-09

Phase 352 ✅ SHIPPED LOCAL: commit 40a8fc2f (feat) + bfcd6c48 (verif) + 8e13fefa (state). Entity 4 prop nullable + AllowedImageExtensions{jpg,jpeg,png}+MaxImageFileSizeBytes(5MB) + ValidateImageFile + migration AddImageToPackageQuestionAndOption applied lokal HcPortalDB_Dev + 120/120 test. Migration flag=TRUE IT-notify. D-03 override 5MB.

⚠️ Frontmatter total_phases/status disentuh sesi paralel (Phase 356 Coach×Coachee addon) — jangan andalkan angka frontmatter; v24.0 inti = 4 phase 352-355.

Scope: gambar pada soal + opsi (MC/MA/Essay), render 6 layar, sinkron Pre→Post shared-file, hapus file atomic (Phase 333). 1 migration (Phase 352, 4 kolom). Phase numbering lanjut dari 351 → 352-355.

Predecessor: v23.0 ✅ CLOSED 2026-06-06 (7/7 REQ SF-01..07, tag v23.0 lokal, archived). v22.0 ✅ CLOSED 2026-06-05. Bundle v19-v23 NOT PUSHED pending IT.

## v24.0 Phase Map (REVISED 4 phase)

| Phase | Goal | REQ | Migration | Depends on | UI hint |
|-------|------|-----|-----------|-----------|---------|
| **352** Data Foundation + Image-Only Upload | Migration 4 kolom + entity (PackageQuestion/PackageOption ImagePath+ImageAlt) + FileUploadHelper mode image-only (magic-byte, ≤2MB) + folder `/uploads/questions/{packageId}/` | IMG-04 | **true** (4 kolom nullable) | — (fase pertama) | no |
| **353** Admin Backend Gambar (CRUD + Sync + Atomic Delete) | Form upload/alt/replace/remove per soal+opsi + Create/Edit/Delete wiring + JSON prefill edit (Gap 3) + preview admin render (Gap 5) + SyncPackagesToPost shared-file (Gap 1) + hapus file atomic pola Phase 333 (DeleteQuestion/replace). **MERGED old 353+354** — sengaja lebih besar tapi kohesif (satu file `AssessmentAdminController.cs`). 7 SC. | IMG-01, IMG-02, IMG-03, IMG-05, IMG-06, IMG-07, RND-04, SYN-01, SYN-02 | false | Phase 352 | yes |
| **354** Render Gambar di 6 Layar | 4 ViewModel bawa gambar (Gap 2) + render img-fluid+lazy+alt di StartExam/ExamSummary/Results/_PreviewQuestion/AssessmentMonitoringDetail/EditPesertaAnswers | RND-01, RND-02, RND-03, RND-05, RND-06, RND-07 | false | Phase 353 (shared-file path final) | yes |
| **355** Test & UAT | xUnit konsolidasi (upload valid/invalid + sync copy + delete file) + Playwright UAT end-to-end admin→peserta StartExam→Results | TST-01, TST-02 | false | Phase 354, 353 | yes |

**Roadmap shaping notes (revised):**

- **Kompresi 5 → 4 phase (pilihan user):** Old Phase 353 (Admin CRUD) + old Phase 354 (Sync + Cleanup) di-merge → Phase 353 tunggal "Admin Backend Gambar". Alasan: keduanya menulis file yang sama (`AssessmentAdminController.cs`: CRUD ~L6067-6377, JSON prefill L6214, SyncPackagesToPost L5337, DeleteQuestion L6377) dan sudah sequential-strict — tidak ada keuntungan paralel dari memisahnya. Phase 353 kini memegang 9 REQ + 7 success criteria (CRUD + sync + atomicity). Sengaja lebih besar tetapi kohesif.
- **Backbone spec §12 (A→E) tetap dihormati**: A=352 (Data&Upload), B+C=353 (Admin CRUD + Sync&Cleanup digabung), D=354 (Render), E=355 (Test). RND-04 (preview admin) tetap di Phase 353 (cohere ke form CRUD via 1 view `_PreviewQuestion`, Gap 5 di alur create/edit).
- **File-overlap sequencing:** Phase 353 = seluruh sisi backend `AssessmentAdminController.cs` dalam satu fase. Phase 354 menulis `CMPController.cs` (StartExam L1055, Results L2300) + ViewModels + 6 view — jalur berbeda, dijadwalkan setelah 353 agar shared-file path final & menghindari rework render.
- **1 migration (Phase 352 only):** 4 kolom nullable `PackageQuestions.ImagePath`/`ImageAlt` + `PackageOptions.ImagePath`/`ImageAlt`. Flag migration IT-notify saat shipped. Semua phase lain migration=false.
- **Tests: hybrid** — logic-bearing test di-fold incremental per phase (352 helper image-only, 353 sync/delete, pola v22/v23), **plus** Phase 355 dedicated mengkonsolidasi TST-01 (xUnit suite final) + TST-02 (Playwright UAT lintas-stack admin upload → peserta StartExam → Results).
- **Verifikasi lokal wajib (CLAUDE.md Develop Workflow):** tiap phase `dotnet build` + `dotnet run` localhost:5277 + Playwright (bila UI) sebelum commit. ❌ tidak ada edit di Dev/Prod.

## Next Action

1. **`/gsd-plan-phase 352`** — Data Foundation + Image-Only Upload. Input: spec §4 (data design 4 kolom) + §6 (upload, Gap 4 helper image-only) + §10 (keamanan). Effort S-M. **Migration** (4 kolom nullable) — Seed Workflow tidak relevan (kolom, bukan seed data); snapshot DB lokal sebelum apply migration per kebiasaan. Verifikasi `dotnet build` + `dotnet ef database update` lokal + xUnit helper image-only.
2. **Carry-over IT promo v19.0+v20.0+v21.0+v22.0+v23.0** — push bundle ~163+ commit lokal + Dev migration coordination tetap pending. v24.0 Phase 352 akan menambah 1 migration baru ke batch (flag IT-notify saat shipped).
3. **(Opsional) `/gsd-verify-work 350`** — tutup HUMAN-UAT item visual v23.0 (XLSX content) saat dev/IT sempat; non-blocking.
4. **(Backlog housekeeping non-blocker):** v16.0+v17.0+v18.0 MILESTONES.md entries belum ditambah (defer batch retro).

## Deferred Items

### v15.0 Deferred (carry-over)

| REQ | Item | Status | Due |
|-----|------|--------|-----|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | menunggu user verifikasi save/load Rubrik | 2026-05-12 |

### Carry-over dari v14.0 close (2026-04-24)

| Category | Item | Status | Source |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | paused-at-checkpoint | HANDOFF.json (2026-04-10) |
| UAT | Phase 235 — 5 items butuh human verification via browser | pending | STATE.md (prior) |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notification) | pending | STATE.md (prior) |
| Research gap | Phase 297 Pre-Post Renewal behavior — keputusan 2 sesi baru otomatis | undecided | v14.0 planning |
| Research gap | Phase 298 essay max character limit — nvarchar(max) vs nvarchar(2000) | undecided | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | undecided | v13.0 carry-over |
| v11.2 paused | Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page) | paused | MILESTONES.md v11.2 |

### v22.0 Tech Debt (acknowledged at close 2026-06-05)

| Item | Status |
|------|--------|
| Push batch v19+v20+v21+v22 (~127 commit leg) pending IT availability | pending |
| CMP06R-03 PDF env-blocked lokal (QuestPDF 204, Phase 327 known) — code-verified | needs Dev/Prod render-confirm |
| 348/349 tanpa VERIFICATION.md (human-verify + UAT substantif) | accepted |

### v23.0 Tech Debt (acknowledged at close 2026-06-06)

| Item | Status |
|------|--------|
| Phase 350 VERIFICATION human_needed — XLSX export content (archived vs current per-Category) belum di-eyeball lokal:5277 | code + Playwright href/counter verified; needs visual confirm |
| Phase 351 code review 3 INFO opsional (data-type konvensi 2 surface, deserialize null-coalesce, comparer culture) | accepted |
| Nyquist artifact-only partial — VALIDATION.md 350/351 frontmatter draft tak di-update post-exec (Wave 0 Playwright hijau) | optional `/gsd-validate-phase 350\|351` sync |
| Push bundle v19+v20+v21+v22+v23 (~163+ commit) pending IT availability | pending |

### Dropped (v23.0 scope decision)

| Item | Reason | Date |
|------|--------|------|
| Phase 999.1 Realtime Assessment SignalR | Tidak diprioritaskan user | 2026-06-05 |
| Phase 999.2 Team View search → Assessment title | PROMOTED → v23.0 Phase 350 (SF-01/02/06) | 2026-06-05 |

## Quick Tasks Completed

| Date | Slug | Description |
|------|------|-------------|
| 2026-05-26 | cdp-portal-platform-rename | Rename CDP label "Competency Development Portal" → "Platform" (parity dgn CMP). 4 edit di Views/CDP/Index.cshtml + Views/Home/Index.cshtml. |

## Accumulated Context

### Decisions (persist across milestones)

- [v24.0 / spec §8 Gap 1]: **Sinkron Pre→Post gambar = shared-file (string path copy), BUKAN file fisik digandakan.** SyncPackagesToPost drop-recreate seluruh Post tiap edit → kalau gandakan fisik akan terus orphan. Lifecycle file fisik dimiliki paket pemilik (Pre untuk soal disinkron; Post untuk soal Post-only). Sync TIDAK PERNAH buat/hapus file.
- [v24.0 / spec §9]: **Hapus file gambar pakai pola Phase 333/335** — kumpul path SEBELUM BeginTransactionAsync, File.Delete loop SETELAH CommitAsync, inner try/catch warn-only per file (tidak throw). Berlaku DeleteQuestion + replace gambar via Edit.
- [v24.0 / spec §5+§8]: **Render gambar via atribut `src` ber-encode Razor (img-fluid + loading=lazy + alt), render hanya jika ImagePath != null** — tidak menambah surface XSS. Perbaikan render bare `@QuestionText`/`@OptionText` existing = keputusan terpisah, OUT OF SCOPE v24.0.
- [v23.0 / Phase 350]: REC-06 D-07 invariant LOCKED — search assessment-title filter di level worker (post-load), badge/count per-worker utuh.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` ("Menunggu Penilaian") = single source of truth label lintas 11+ surface; exclude-pending denominator konsisten.
- [v14.0 / Phase 296]: GradeFromSavedAnswers dihapus — GradingService satu-satunya source of truth grading
- [v14.0 / Phase 301]: Export endpoints re-query database independen (tidak share state dengan API endpoints)
- [v13.0]: SortableJS 1.15.7 via CDN; drag-drop sibling-only (group: false); orgTree.js single JS orchestrator
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route] attribute
- [v15.0 / Phase 307]: Selectors helper di `tests/e2e/helpers/wizardSelectors.ts` (NEW folder) untuk separation e2e-specific selectors vs shared utilities
- [v21.0]: Configurable display labels via cached `IOrgLabelService` + global `@inject` di `_ViewImports.cshtml` (real-time propagation via cache-invalidate-on-mutation)

### Open Blockers/Concerns

- [v24.0 / Phase 352] Seed/migration: snapshot DB lokal sebelum `dotnet ef database update` (migration 4 kolom) per kebiasaan; kolom nullable → aman tapi tetap backup.
- Phase 293 `GetSectionUnitsDictAsync` — hardcoded 2-level, unit Level 2+ tidak muncul di dropdown ManageWorkers (keputusan tertunda)

### Roadmap Evolution

- Phase 356 added (2026-06-06): Audit Fix Assign Coach×Coachee — addon OFF-THEME ke v24.0 atas permintaan user. 7 temuan audit AF-1..7 (CoachMappingController.cs). AF-1 HIGH **confirmed** via query DB: track id=4 punya 2 unit → `GetEligibleCoachees` bandingkan progress unit-coachee vs total deliverable semua-unit → coachee track multi-unit tak pernah eligible Assessment Proton. Independen dari 352-355 (jalur file berbeda). Belum di-plan. Pertimbangkan spec audit dulu.
- v24.0 REVISED (2026-06-06): dikompresi 5 → 4 phase atas pilihan user. Old Phase 353 (Admin CRUD) + old Phase 354 (Sync/Cleanup) MERGED → Phase 353 "Admin Backend Gambar" (keduanya menulis `AssessmentAdminController.cs` & sequential-strict). Renumber kontigu: old 355 Render → 354, old 356 Test/UAT → 355. Phase 353 kini memegang 9 REQ (IMG-01/02/03/05/06/07 + RND-04 + SYN-01/02) + 7 success criteria. 17/17 REQ tetap mapped, 0 dropped, 0 orphan. Migration tetap Phase 352 only.
- v24.0 added (2026-06-06): Gambar di Soal Assessment (Manage Package) — awalnya 5 phase 352-356 derived dari spec 2026-06-06-image-in-assessment-questions-design.md (spec-driven, 5 brainstorm decisions + 5 code-verified gaps).
- v23.0 added (2026-06-05): CMP/Records Search & Filter Consistency Audit — 2 phase 350-351 dari audit 3-surface (7 confirmed). Sequential strict (file-overlap WorkerDataService.cs). Tests folded per phase. No migration.

## Session Continuity

Last activity: 2026-06-06 — ROADMAP v24.0 DIREVISI (roadmapper) dari 5 phase ke 4 phase:

- **Phase 352** (Data Foundation + Image-Only Upload) — UNCHANGED: IMG-04. Migration 4 kolom nullable (PackageQuestions/PackageOptions ImagePath+ImageAlt) + entity `Models/AssessmentPackage.cs` + FileUploadHelper mode image-only (Gap 4, magic-byte JPG/PNG reject PDF) + folder `/uploads/questions/{packageId}/` + cap 2MB. **migration=true.**
- **Phase 353** (Admin Backend Gambar — MERGED old 353+354): IMG-01/02/03/05/06/07 + RND-04 + SYN-01/02. Form upload/alt/replace/remove per soal+opsi; CreateQuestion POST ~L6067, EditQuestion GET JSON ~L6196 (+imagePath+imageAlt Gap 3 ~L6214) + POST ~L6241, DeleteQuestion ~L6377; preview admin `_PreviewQuestion.cshtml` L45-63 (Gap 5); SyncPackagesToPost L5337 shared-file copy (Gap 1, +ImagePath+ImageAlt soal L5370 + opsi L5379); hapus file atomic pola Phase 333 (DeleteQuestion + replace). Semua di `AssessmentAdminController.cs`. 7 success criteria. depends 352.
- **Phase 354** (Render 6 layar — was 355): RND-01/02/03/05/06/07. 4 ViewModel bawa gambar (Gap 2: PackageExamViewModel L25/43, AssessmentResultsViewModel L24, AssessmentMonitoringViewModel L73, + ExamSummary/EditPesertaAnswers item) + populate (CMPController StartExam L1055, Results L2300; AssessmentAdminController essay L3401) + render img-fluid+lazy+alt di 6 view. depends 353 (shared-file path final).
- **Phase 355** (Test/UAT — was 356): TST-01 (xUnit konsolidasi) + TST-02 (Playwright UAT end-to-end). depends 354/353.

Kompresi rationale: old 353+354 sama-sama `AssessmentAdminController.cs` (CRUD + Sync L5337 + Delete L6377 + JSON prefill L6214) dan sudah sequential-strict → tidak ada keuntungan paralel; gabung jadi satu fase backend kohesif. Migration hanya Phase 352. Tests hybrid (per-phase folded + dedicated 355).

Files written: ROADMAP.md (v24.0 block diganti dengan versi 4-phase 352-355 — milestone list line + Phases checklist + Phase Details 352-355 dengan UI hint + Progress Table + Coverage Validation 17/17 + footer revisi log; existing v1.0-v23.0 history preserved), REQUIREMENTS.md (traceability 17/17 remapped + grouping rationale revised + revision note), STATE.md (this file — frontmatter total_phases=4, v24.0 Phase Map revised, Session Continuity, Next Action).

Next action: `/gsd-plan-phase 352` — Data Foundation + Image-Only Upload. Spec §4+§6+§10 jadi input CONTEXT.md. Effort S-M. **Migration** (snapshot DB lokal sebelum apply). Verifikasi `dotnet build` + `dotnet ef database update` + `dotnet run` localhost:5277 + xUnit helper image-only sebelum commit per CLAUDE.md Develop Workflow.
