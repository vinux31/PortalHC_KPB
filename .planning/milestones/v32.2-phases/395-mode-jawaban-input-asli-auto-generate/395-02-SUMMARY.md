---
phase: 395-mode-jawaban-input-asli-auto-generate
plan: 02
subsystem: inject-assessment / controller-preview-commit
tags: [inject, preview, dry-run, commit, auto-generate, server-authoritative, integration-test]
requires:
  - "InjectAssessmentService.BuildAutoGenAnswers / ComputeAutoGenSeed / AutoGenResult (Plan 01, Services/InjectAssessmentService.cs)"
  - "InjectAssessmentService.InjectBatchAsync (Phase 393, commit batch grade+persist+cert+audit)"
  - "AssessmentScoreAggregator.Compute (Helpers/AssessmentScoreAggregator.cs:26-60, engine preview==commit)"
  - "InjectAnswerSpec / InjectQuestionSpec / InjectWorkerSpec DTO (Phase 393, TIDAK diubah — D-02)"
provides:
  - "POST /Admin/InjectAssessment commit aktual (InjectBatchAsync) — commit PERTAMA milestone"
  - "POST /Admin/PreviewInjectScore (dry-run skor final pra-persist, NO cert#, NO write — D-09)"
  - "InjectAssessmentController.ParseAnswerVms (deserialize #AnswersJson per-worker, try/catch JsonException)"
  - "InjectAssessmentController.MapToRequest(vm, userIdToNip, workerAnswers) — isi Answers per-worker"
  - "InjectPreviewRequest / InjectPreviewResult DTO (Models/InjectAssessmentDtos.cs)"
  - "AnswersJson + InjectAnswerVM + InjectWorkerAnswersVM (ViewModels/InjectAssessmentViewModel.cs)"
affects:
  - "Phase 395 Plan 03 (view) — konsumsi endpoint PreviewInjectScore + serialize #AnswersJson + UI mode"
  - "Phase 396 (Import Excel) — reuse BuildAutoGenAnswers + jalur commit yang sama"
tech-stack:
  added: []
  patterns:
    - "Server-authoritative pola auto-gen: controller re-derive BuildAutoGenAnswers(seed deterministik) saat commit = pola identik preview (RESEARCH A1)"
    - "Dry-run preview pra-persist: map pola usulan → in-memory PackageQuestion/Response (TempId=Id sintetis) → AssessmentScoreAggregator.Compute (EF-free, no write) → preview==commit"
    - "BLOCKING guard server-side: worker auto-gen TargetReachable=false TIDAK di-commit (D-08.3 integritas sertifikasi, jangan cap diam-diam)"
    - "Skip=OMIT spec (D-05): manual answers di-copy apa adanya; soal di-skip tak ada di Answers → grade 0, bukan reject-all"
key-files:
  created:
    - "HcPortal.Tests/InjectPreviewEqualsCommitTests.cs (integration real-SQL, 4 fact, [Trait Category=Integration])"
  modified:
    - "Controllers/InjectAssessmentController.cs (+PreviewInjectScore +ParseAnswerVms +MapToRequest Answers +ResolveWorkerAnswers +FindBlockedAutoGenNips +MapToInMemory +wire InjectBatchAsync)"
    - "Models/InjectAssessmentDtos.cs (+InjectPreviewRequest +InjectPreviewResult)"
    - "ViewModels/InjectAssessmentViewModel.cs (+AnswersJson +InjectAnswerVM +InjectWorkerAnswersVM)"
    - "HcPortal.Tests/InjectViewModelMapTests.cs (fix 6 call site MapToRequest 3-arg — Rule 3 blocking)"
decisions:
  - "Server-otoritas pola (A1): commit re-derive BuildAutoGenAnswers(seed) bukan terima pola dari client → tahan-tamper + preview==commit terjamin via seed deterministik"
  - "PreviewInjectScore return Json (bukan View) → tidak terpengaruh override View() ~/Views/Admin/ (hanya untuk ViewResult)"
  - "BLOCKING auto-gen unreachable di-guard di POST action SEBELUM InjectBatchAsync (re-derive ceiling per worker auto); bukan diam-diam cap (D-08.3)"
  - "MapToRequest signature diperluas (+workerAnswers) bukan overload baru — call site 394 di-fix (Rule 3); Answers per-worker manual=copy / auto=BuildAutoGenAnswers + essay manual gabung (HYBRID D-08.1)"
metrics:
  duration: "~9 menit"
  tasks: 3
  files_created: 1
  files_modified: 4
  tests_added: 4
  completed: 2026-06-18
---

# Phase 395 Plan 02: Wire commit + PreviewInjectScore + ParseAnswerVms + MapToRequest Answers Summary

Lapisan controller yang menjembatani helper auto-gen Plan 01 ke commit aktual + preview dry-run. Tiga capaian: (1) POST `/Admin/InjectAssessment` kini **commit aktual** via `InjectBatchAsync` (commit PERTAMA milestone — 394 berhenti sebelum commit per D-07); (2) endpoint baru `PreviewInjectScore` menghitung skor final pra-persist via `AssessmentScoreAggregator.Compute` yang SAMA dengan commit (preview==commit, NO cert#, NO write — D-09); (3) `MapToRequest` mengisi `Answers` per-worker — input-asli langsung, auto-gen via `BuildAutoGenAnswers(seed)` server-authoritative. Dikunci 4 integration test real-SQL preview==commit + skip=omit + TextAnswer-wajib.

## What Was Built

**Task 1 — DTO preview + VM AnswersJson** (`feat(395-02)` @`b78140bd`):
- `Models/InjectAssessmentDtos.cs`: `InjectPreviewRequest` (PassPercentage/Title/Category/CompletedAt/Nip/Mode/TargetScore/Questions/Answers) + `InjectPreviewResult` (Percentage/IsPassed/TotalScore/MaxScore/CeilingPercent/TargetReachable/Overshoot/Blocked/BlockingMessage).
- `ViewModels/InjectAssessmentViewModel.cs`: `AnswersJson` (hidden-JSON paralel `QuestionsJson`) + nested `InjectAnswerVM` (mirror `InjectAnswerSpec`) + `InjectWorkerAnswersVM` (per-worker `{UserId, Mode, TargetScore, Answers[]}`, key = checkbox value = user.Id).
- DTO 393 stabil (`InjectRequest`/`InjectAnswerSpec`/`InjectWorkerSpec`) TIDAK berubah — Mode/Target = lapisan VM/controller (D-02).

**Task 2 — Controller wire** (`feat(395-02)` @`b7335135`):
- `ParseAnswerVms` — deserialize `#AnswersJson` per-worker, try/catch `JsonException` → fallback `new()` (paralel `ParseQuestionVms`, Security V5 malformed→bukan 500).
- `MapToRequest(vm, userIdToNip, workerAnswers)` — signature diperluas; loop UserId→NIP kini isi `Answers` via `ResolveWorkerAnswers`: mode `auto` → `BuildAutoGenAnswers(req.Questions, target, ComputeAutoGenSeed(nip, title, category, CompletedAt, target))` MC/MA + gabung essay manual (HYBRID D-08.1, hanya soal Essay); mode `manual` → copy spec apa adanya (skip=omit ditangani client, D-05); worker tanpa entry → `Answers` kosong.
- `PreviewInjectScore` action ([HttpPost] [Authorize Admin,HC] [ValidateAntiForgeryToken], `[FromBody] InjectPreviewRequest`, return `Json`): resolve pola (auto=BuildAutoGenAnswers+essay manual / manual=Answers) → `MapToInMemory` (pola → POCO in-memory, TempId=Id sintetis) → `AssessmentScoreAggregator.Compute` (engine identik commit → preview==commit). Auto: isi CeilingPercent/TargetReachable/Blocked/Overshoot/BlockingMessage. **NO `CertNumberHelper`, NO `SaveChanges`** (D-09).
- POST `InjectAssessment` — ganti blok no-commit 394 (`TempData["Info"]`) dengan: parse answers → MapToRequest → **guard BLOCKING auto-gen unreachable** (`FindBlockedAutoGenNips` re-derive ceiling per worker auto; TargetReachable=false → JANGAN commit, `TempData["Error"]`, arahkan switch input-asli, D-08.3) → `_injectService.InjectBatchAsync(req, actorUserId=_userManager.GetUserId(User), actorName)` → surface `TempData["Success"]`/`["Error"]` (+PerRowErrors bila Rejected).

**Task 3 — Integration test real-SQL** (`test(395-02)` @`24cbf353`): `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs` ([Trait Category=Integration], IClassFixture `InjectAssessmentFixture` di-reuse, helper `NewGradingService`/`NewInjectService`/`SeedUserAsync`/`LoadGradedAsync` SALIN VERBATIM). 4 fact:
- `PreviewEqualsCommit_InputAsli_MixedAnswers`: MC benar + MA partial(0) + Essay 7/10 → preview 56% (17/30 truncation) == `AssessmentSession.Score` commit dari DB.
- `PreviewEqualsCommit_AutoGen_HitsTargetAndMatches`: 4 MC, target 70, seed deterministik dipakai KEDUA preview & commit → pola identik; preview>=target, commit Score==preview, IsPassed.
- `SkipOmit_UnansweredGradedZero_NotRejectAll`: 4 MC, jawab 2 (2 di-omit) → batch TIDAK reject-all; Score=50 (2*10/40); preview==commit.
- `TextAnswerRequired_EssayScoreWithoutText_Rejects`: essay EssayScore=5 + TextAnswer whitespace → Rejected (0 tulisan); essay di-omit → TIDAK reject (D-05/D-08 konsisten).

## Verification

- `dotnet build HcPortal.csproj` → **Build succeeded, 0 Error(s)** (warning CS hanya di file pre-existing tak terkait: WorkerController/StartExam/TrainingAdmin/RecordsWorkerDetail — out of scope).
- `dotnet test --filter "Category=Integration&FullyQualifiedName~PreviewEqualsCommit"` → **Passed! 4/4** (real-SQL SQLEXPRESS lokal; disposable DB HcPortalDB_Test_{guid}, DB Dev tak tersentuh).
- `dotnet test --filter "Category!=Integration"` → **Passed! 381/381** (no regression; termasuk 6 InjectViewModelMapTests 394 yang di-fix ke signature 3-arg).
- grep `PreviewInjectScore` ✓ + `[ValidateAntiForgeryToken]` ×2 (kedua POST) + `[Authorize(Roles = "Admin, HC")]` ×3 (GET+2 POST) di controller.
- grep `_injectService.InjectBatchAsync` dipanggil di POST `InjectAssessment` (commit aktual) ✓; `Answers = new()` hardcoded di MapToRequest **TIDAK ada lagi** (diganti `ResolveWorkerAnswers`) ✓.
- grep `CertNumberHelper` di controller = HANYA muncul di comment "NO CertNumberHelper (D-09)" — bukan pemanggilan ✓.
- **0 migration**: tak ada file `Data/ApplicationDbContext`/`Migrations/` tersentuh; `Models/InjectAssessmentDtos.cs` = POCO DTO (bukan EF entity, tak ada DbSet/Key/mapping) → nol model diff.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fix 6 call site `InjectViewModelMapTests` (Phase 394) ke signature `MapToRequest` 3-arg**
- **Found during:** Task 3 (build test project sebelum jalankan integration test).
- **Issue:** Memperluas `MapToRequest(vm, userIdToNip)` → `MapToRequest(vm, userIdToNip, workerAnswers)` (Task 2, sesuai instruksi plan) memecah 6 pemanggilan existing di `HcPortal.Tests/InjectViewModelMapTests.cs` (CS7036 missing argument). Blocking — test project tak compile → integration test tak bisa jalan.
- **Fix:** Tambah helper `NoAnswers()` (list kosong `InjectWorkerAnswersVM`) + lewatkan ke 6 call site. Intent test 394 (mapping scalar/question/cert/NIP TANPA jawaban) terjaga — Answers kosong, assertion `Assert.Empty(w.Answers)` :150 tetap lulus.
- **Files modified:** `HcPortal.Tests/InjectViewModelMapTests.cs`.
- **Commit:** `24cbf353` (digabung dengan Task 3 — keduanya bagian gate test integration).

### Catatan non-deviasi
- **Carry-in 394 cosmetic LBL-02** (`injTypeLabel`/validasi "Pilihan Ganda"→"Single Answer" di `InjectAssessment.cshtml`) **TIDAK** dikerjakan di Plan 02 — file view bukan bagian `files_modified` plan ini; di-bundle ke Plan 03 (view) bersama UI mode auto-gen + serialize `#AnswersJson` (sesuai catatan Plan 01 SUMMARY).
- **Untracked** `docs/395-QUESTIONS.json`, `docs/KPB - Licensor Training ... .xlsx`, `docs/Tipe A.xlsx` = artefak discuss (di luar scope), dibiarkan.
- **Commit phase-396 context** (`6a3af750`/`ba1cb20a`) muncul di `git log` SETELAH commit Task 3 saya — itu sesi discuss phase 396 paralel (orchestrator), bukan bagian Plan 02. 3 commit Plan 02 (`b78140bd`/`b7335135`/`24cbf353`) bersih, menyentuh hanya file yang dimaksud.

## Known Stubs
None — semua jalur (parse answers, resolve per-worker manual/auto, preview dry-run, commit, BLOCKING guard) terimplementasi penuh + terkunci integration test. Konsumsi UI (serialize `#AnswersJson` + tombol Pratinjau + render mode) ada di Plan 03; endpoint server siap dikonsumsi.

## TDD Gate Compliance
Task 3 ber-`tdd="true"`. Karena kode produksi yang dikonsumsi sudah ada (BuildAutoGenAnswers/ComputeAutoGenSeed dari Plan 01 @`c79d27a4`; PreviewInjectScore/MapToRequest/InjectBatchAsync dari Task 1-2 commit di plan ini), test integration LULUS pada penulisan pertama (no RED murni terpisah untuk task ini). Ini bukan pelanggaran: Task 3 adalah **verifikasi integration end-to-end** atas kode yang Task 1-2 bangun (RED/GREEN ditegakkan di Plan 01 untuk algoritma genuine-baru). Gate sequence keseluruhan phase: `test(395-01)` RED @`561944f7` → `feat(395-01)` GREEN @`c79d27a4` → `feat(395-02)` Task 1/2 → `test(395-02)` integration lock @`24cbf353`. Commit `test(395-02)` di-tag `test(...)` sesuai konvensi (test-only changes).

## Self-Check: PASSED
- FOUND: `Controllers/InjectAssessmentController.cs` (berisi `PreviewInjectScore`, `ParseAnswerVms`, `ResolveWorkerAnswers`, `FindBlockedAutoGenNips`, `MapToInMemory`, panggilan `_injectService.InjectBatchAsync`)
- FOUND: `Models/InjectAssessmentDtos.cs` (berisi `InjectPreviewRequest`, `InjectPreviewResult`)
- FOUND: `ViewModels/InjectAssessmentViewModel.cs` (berisi `AnswersJson`, `InjectAnswerVM`, `InjectWorkerAnswersVM`)
- FOUND: `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs`
- FOUND commit: `b78140bd` (feat Task 1)
- FOUND commit: `b7335135` (feat Task 2)
- FOUND commit: `24cbf353` (test Task 3)
