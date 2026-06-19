---
phase: 395-mode-jawaban-input-asli-auto-generate
verified: 2026-06-18T05:04:56Z
status: passed
score: 13/13
overrides_applied: 0
---

# Phase 395: Mode Jawaban (Input Asli + Auto-Generate) — Laporan Verifikasi

**Phase Goal:** HC dapat menentukan jawaban tiap pekerja dengan dua cara — menginput jawaban asli per soal, atau auto-generate pola jawaban dari skor target — dan melihat skor final aktual (memperhitungkan pembulatan) sebelum commit.
**Verified:** 2026-06-18T05:04:56Z
**Status:** PASSED
**Re-verification:** Tidak — verifikasi awal.

---

## Goal Achievement

### Observable Truths

Merged dari 5 Success Criteria ROADMAP + 13 must-have truths di 3 PLAN frontmatter (deduplicated).

#### Success Criteria ROADMAP (non-negotiable)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| SC1 | HC dapat membuka form jawaban per pekerja, memilih opsi MC (1 opsi) / MA (>=1) dan mengisi teks + skor Essay; saat di-inject skor dihitung pipeline grading (bukan diketik sebagai persen final). *(INJ-08)* | VERIFIED | Step-5 sub-komponen IIFE `InjectAssessment.cshtml:1079+` render form MC radio / MA checkbox / Essay textarea+skor; `InjectBatchAsync` dipanggil di POST commit `:77`, pipeline grading berjalan. E2e `inject-assessment-395.spec.ts` membuktikan Score=100 bukan 0 untuk worker yang pilih opsi benar. |
| SC2 | HC dapat memilih mode auto-generate dengan skor target; sistem membentuk pola benar/salah MC/MA konsisten dengan target. *(INJ-09)* | VERIFIED | `BuildAutoGenAnswers` di `InjectAssessmentService.cs:540` menghasilkan subset soal "benar" via greedy+seed+re-cek `floor()`. `ComputeAutoGenSeed` SHA-256 `:520`. 30 unit test `BuildAutoGenAnswersTests.cs` hijau termasuk hit-target, mixed-weight, ceiling-essay, seed reproducible. |
| SC3 | Sebelum commit, sistem menampilkan skor final aktual hasil pipeline grading untuk auto-generate (memperhitungkan pembulatan) agar HC tahu. *(INJ-09)* | VERIFIED | `PreviewInjectScore` di `InjectAssessmentController.cs:106` — endpoint dry-run `[HttpPost][Authorize Admin,HC][ValidateAntiForgeryToken]`, memanggil `AssessmentScoreAggregator.Compute` identik commit `:134`, return `Json(result)` tanpa `CertNumberHelper`/`SaveChanges`. UI step-5 menampilkan `percentage`, badge Lulus/Tidak, overshoot note. |
| SC4 | Jawaban auto-generate tetap menghasilkan sesi `IsManualEntry=true` + AuditLog; skor yang tampil di hasil == skor preview. *(INJ-08, INJ-09)* | VERIFIED | `InjectBatchAsync` existing (393) menangani `IsManualEntry` + AuditLog. Integration test `InjectPreviewEqualsCommitTests.cs` — 4 fact real-SQL: `PreviewEqualsCommit_InputAsli_MixedAnswers`, `PreviewEqualsCommit_AutoGen_HitsTargetAndMatches`, `SkipOmit_UnansweredGradedZero_NotRejectAll`, `TextAnswerRequired_EssayScoreWithoutText_Rejects` — semuanya hijau. Preview==commit dibuktikan via seed deterministik SHA-256. |
| SC5 | `dotnet build` 0 error + `dotnet test` + Playwright e2e: input asli → skor benar; auto-gen → preview skor final tampil → commit → hasil match. *(INJ-08, INJ-09)* | VERIFIED | Summary Plan 01-03 mencatat: build 0 error, fast suite 381/381 green, integration 4/4 green, e2e `inject-assessment-395.spec.ts` 5/5 green (termasuk D-04 regression `fix(395)` @`50e7eb27`). Semua commit terverifikasi ada di git history. |

#### Must-Haves Plan 01 (Fondasi server-side)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| P01-T1 | Auto-gen menghasilkan pola jawaban MC/MA yang, setelah di-grade pipeline, mencapai skor terkecil yang >= target | VERIFIED | `BuildAutoGenAnswers` `:540` dengan greedy+smallest-such+re-cek `(int)((double)total/max*100)`. Test `HitTarget_EqualWeight_SmallestSuch` + `HitTarget_MixedWeight_BoundaryOffByOne` dalam `BuildAutoGenAnswersTests.cs`. |
| P01-T2 | Bila target melebihi ceiling MC/MA, `TargetReachable=false` dilaporkan (tidak di-cap diam-diam) | VERIFIED | `InjectAssessmentService.cs:570-576`: `if (targetPercent > ceilingPercent) return new AutoGenResult(allCorrect, ceilingPercent, maxScore, TargetReachable: false)`. Test `Ceiling_EssayHeavy_TargetUnreachable`. Controller guard `FindBlockedAutoGenNips` `:263` menolak commit bila unreachable. |
| P01-T3 | Pola auto-gen reproducible: input sama -> pola identik; room berbeda -> pola berbeda | VERIFIED | `ComputeAutoGenSeed` SHA-256 `:510-521` pakai string kanonik `nip+title+category+date+target` (unit-separator U+001F). Test `Seed_SameInput_SameInt` + `Seed_DifferentNip_DifferentInt`. Integration test `PreviewEqualsCommit_AutoGen` seed identik dipakai KEDUA preview & commit. |
| P01-T4 | Essay mode input-asli yang berisi skor tetapi teks kosong ditolak (D-04), tetapi essay auto-gen yang di-omit tidak terblokir | VERIFIED | `PreflightValidateAsync` `:396-397`: `if (ans.EssayScore.HasValue && string.IsNullOrWhiteSpace(ans.TextAnswer)) errors.Add(...)`. Guard `EssayScore.HasValue` memastikan auto-gen essay (tak di-emit) tidak terblokir. Test `TextAnswerRequired_EssayScoreWithoutText_Rejects`. |

#### Must-Haves Plan 02 (Controller commit + preview)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| P02-T1 | POST /Admin/InjectAssessment me-commit aktual via `InjectBatchAsync` (commit pertama milestone) | VERIFIED | `InjectAssessmentController.cs:53-98`: POST action memanggil `_injectService.InjectBatchAsync(req, actorUserId, actorName)` `:77`. Tidak lagi ada blok `TempData["Info"]` no-commit 394. Commit `b7335135`. |
| P02-T2 | Endpoint POST /Admin/PreviewInjectScore mengembalikan skor final + Lulus/Tidak + ceiling/overshoot/blocking TANPA nomor sertifikat dan TANPA write DB | VERIFIED | `PreviewInjectScore` `:100-152`: `[ValidateAntiForgeryToken]`, `[FromBody]`, return `Json(result)`. Komentar eksplisit "NO CertNumberHelper (D-09), NO SaveChanges" `:136`. Dikonfirmasi grep `CertNumberHelper` = HANYA di comment, bukan pemanggilan. |
| P02-T3 | Skor preview == skor commit (engine `AssessmentScoreAggregator.Compute` identik di kedua jalur) | VERIFIED | `PreviewInjectScore` `:134` memanggil `AssessmentScoreAggregator.Compute` identik. `MapToInMemory` helper `:283` memetakan TempId ke in-memory POCO. Integration test 4/4 membuktikan `session.Score == previewPct`. |
| P02-T4 | Worker auto-gen di-resolve server-authoritative: `BuildAutoGenAnswers` dipanggil di controller, mode/target tak pernah masuk DTO/service | VERIFIED | `ResolveWorkerAnswers` `:225` di controller: mode `auto` memanggil `BuildAutoGenAnswers(seed)` server-side. `InjectRequest`/`InjectAnswerSpec`/`InjectWorkerSpec` DTO 393 tidak berubah — tidak ada field Mode/Target. |

#### Must-Haves Plan 03 (View Step-5 + UI)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| P03-T1 | HC membuka Langkah 5: navigasi 1-pekerja-per-layar (Prev/Next + roster), per-pekerja pilih mode input-asli/auto-generate | VERIFIED | `InjectAssessment.cshtml:1084+` IIFE ber-state `step5State`/`step5Idx`, fungsi `step5Rebuild()`/`step5RenderWorker()`/`step5RenderRoster()`. Tombol `step5PrevWorker`/`step5NextWorker` `:1472-1476`. Roster tabel `role=status aria-live=polite`. UAT live browser PASSED. |
| P03-T2 | Input-asli: render soal authored (MC radio, MA checkbox, Essay textarea+skor); skip=omit (tak push answer) | VERIFIED | `step5RenderAnswerForm()` `:1197+` render MC radio / MA checkbox / Essay textarea+skor per soal. Skip-checkbox `step5WorkerAnswers` `:1366`: `if (!auto && ans.skipped) return` (OMIT, D-05). |
| P03-T3 | Auto-generate: input target -> tombol Pratinjau -> skor final aktual + Lulus/Tidak; overshoot/BLOCKING ditampilkan | VERIFIED | Handler Pratinjau `:1412+` fetch POST `PreviewInjectScore` dengan CSRF token. Render `step5RenderPreviewSurface` `:1411` + `step5RenderBlocking` `:1434`. D-04 validasi inline `step5ValidateCurrentEssays` `:1449` abort bila essay teks kosong + skor diisi (fix `50e7eb27`). |
| P03-T4 | `#AnswersJson` terisi saat submit di listener yang SAMA dengan `#QuestionsJson` -> POST commit menghasilkan skor benar (bukan grade 0 silent) | VERIFIED | `InjectAssessment.cshtml:988-995`: satu `addEventListener('submit')` berisi KEDUANYA `#QuestionsJson` `:990` dan `#AnswersJson` `:993-994`. E2e `page.evaluate(() => document.getElementById('AnswersJson').value)` non-empty + Score DB != 0. |
| P03-T5 | Label tipe soal = Single Answer/Multiple Answer (LBL-02), bukan Pilihan Ganda/Pilihan Majemuk | VERIFIED | `injTypeLabel()` `:852-857`: return `'Single Answer'`/`'Multiple Answer'`. Grep "Pilihan Ganda"/"Pilihan Majemuk" di file = 0 match. Pesan validasi `:949-950` sudah pakai label baru. |

**Score: 13/13 truths verified**

---

### Required Artifacts

| Artifact | Provides | Status | Detail |
|----------|----------|--------|--------|
| `Services/InjectAssessmentService.cs` | `BuildAutoGenAnswers` + `ComputeAutoGenSeed` + `AutoGenResult` + rule D-04 TextAnswer-wajib | VERIFIED | `BuildAutoGenAnswers` `:540`, `ComputeAutoGenSeed` `:510`, `AutoGenResult` record `:490-494`, rule D-04 `:396-397`. SHA-256 BCL `:520`. |
| `HcPortal.Tests/BuildAutoGenAnswersTests.cs` | Unit pure: hit-target, boundary, ceiling-essay, seed reproducible, degenerate (30 tests) | VERIFIED | File ada. Kelas `BuildAutoGenAnswersTests` tanpa `[Trait("Category","Integration")]`. Hijau 30/30. |
| `Controllers/InjectAssessmentController.cs` | `PreviewInjectScore` + `ParseAnswerVms` + `MapToRequest` isi Answers + wire `InjectBatchAsync` | VERIFIED | Semua method ada: `PreviewInjectScore` `:106`, `ParseAnswerVms` `:327`, `ResolveWorkerAnswers` `:225`, `FindBlockedAutoGenNips` `:263`, `MapToInMemory` `:283`. `InjectBatchAsync` dipanggil `:77`. |
| `ViewModels/InjectAssessmentViewModel.cs` | `AnswersJson` + `InjectAnswerVM` + `InjectWorkerAnswersVM` | VERIFIED | `AnswersJson` `:38`, `InjectAnswerVM` `:62`, `InjectWorkerAnswersVM` `:75`. |
| `Models/InjectAssessmentDtos.cs` | `InjectPreviewRequest` + `InjectPreviewResult` DTO | VERIFIED | `InjectPreviewRequest` `:87`, `InjectPreviewResult` `:104`. DTO 393 (`InjectRequest`/`InjectAnswerSpec`) tidak berubah. |
| `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs` | Integration real-SQL: preview==commit, skip=omit grade 0, TextAnswer-wajib reject | VERIFIED | File ada. `[Trait("Category","Integration")]`, 4 fact hijau real-SQL. |
| `Views/Admin/InjectAssessment.cshtml` | Step-5 sub-komponen IIFE + `#AnswersJson` hidden + preview JS + LBL-02 | VERIFIED | `#AnswersJson` `:314`, IIFE step5 `:1079+`, `buildWorkerAnswersPayload` `:1377`, serialize di submit-listener `:993-994`, `injTypeLabel` `:852`. |
| `tests/e2e/inject-assessment-395.spec.ts` | E2e: input-asli + auto-gen + Pratinjau + commit + `#AnswersJson` terisi | VERIFIED | File ada, 5 test (3 core + 1 setup + 1 D-04 regression `50e7eb27`). |

---

### Key Link Verification

| From | To | Via | Status | Detail |
|------|-----|-----|--------|--------|
| `BuildAutoGenAnswers` | `AssessmentScoreAggregator.Compute` formula | re-cek `floor((int)((double)total/max*100))` setelah seleksi | WIRED | Formula direplika identik di `BuildAutoGenAnswers` + `PreviewInjectScore`; dikunci unit test smallest-such + integration preview==commit. |
| `ComputeAutoGenSeed` | `System.Security.Cryptography.SHA256` | `ComputeHash(UTF8(canonical)) → BitConverter.ToInt32 & 0x7FFFFFFF` | WIRED | `:520-521` pakai `SHA256.Create()`. Grep `SHA256` ada, `GetHashCode` hanya di doc-comment. |
| `Controllers/InjectAssessmentController.cs MapToRequest` | `ParseAnswerVms → InjectWorkerSpec.Answers` | match per-worker by UserId; auto → `BuildAutoGenAnswers(seed)` | WIRED | `ResolveWorkerAnswers` `:225` dipanggil dari `MapToRequest` `:208`. Auto path memanggil `BuildAutoGenAnswers` + `ComputeAutoGenSeed`. |
| `PreviewInjectScore` | `AssessmentScoreAggregator.Compute` | map pola → in-memory via `MapToInMemory` (TempId=Id sintetis) | WIRED | `:131-134` di `PreviewInjectScore`. |
| `POST InjectAssessment` | `_injectService.InjectBatchAsync` | ganti blok no-commit 394 dengan commit aktual | WIRED | `:77` memanggil `InjectBatchAsync`. Guard BLOCKING `:66-72` sebelum commit. |
| `submit listener #injectAssessmentForm` | `#AnswersJson` value | `JSON.stringify(buildWorkerAnswersPayload())` di listener SAMA dgn `#QuestionsJson` | WIRED | `:988-995` satu listener berisi keduanya. `window.injBuildWorkerAnswers` `:1388` exposed untuk e2e assert. |
| `tombol Pratinjau Skor` | `POST /Admin/PreviewInjectScore` | fetch JSON dengan anti-forgery token; render skor + badge | WIRED | Handler `:1446+` `step5ValidateCurrentEssays` + fetch endpoint. `data-preview-url` `:505`. |
| `render data pekerja/soal` | `.textContent` (XSS-safe) | tidak innerHTML untuk data user | WIRED | Semua render teks soal/opsi/nama via `.textContent`: `:880`, `:1093`, `:1162`, `:1214`, `:1260`. |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `InjectAssessment.cshtml` Step-5 | `step5State[userId].answers` | `step5RenderAnswerForm()` → event listener per soal (radio/checkbox/textarea) | Ya — UI capture dari input user; non-hardcoded | FLOWING |
| `PreviewInjectScore` (controller) | `agg.Percentage`, `agg.IsPassed` | `AssessmentScoreAggregator.Compute(qInMem, respInMem, passPercentage)` `:134` | Ya — dihitung dari pola usulan, bukan hardcoded | FLOWING |
| `InjectBatchAsync` | `AssessmentSession.Score` | `GradingService` pipeline (Phase 393) | Ya — engine grading identik online | FLOWING |
| `buildWorkerAnswersPayload()` | `answers` per worker | `step5WorkerAnswers(ws)` iterasi `injQuestions` | Ya — answers real dari state, skip=OMIT diterapkan | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command/Evidence | Result | Status |
|----------|-----------------|--------|--------|
| `BuildAutoGenAnswers` menghasilkan pola >= target | 30 unit test `BuildAutoGenAnswersTests.cs` hijau | Passed 30/30 | PASS |
| `ComputeAutoGenSeed` deterministik lintas-proses | Test `Seed_SameInput_SameInt` + SHA-256 non-`GetHashCode` | Passed | PASS |
| Preview == Commit (real-SQL) | `InjectPreviewEqualsCommitTests.cs` 4/4 integration | Passed 4/4 | PASS |
| E2e Score DB tidak 0 setelah commit | `inject-assessment-395.spec.ts` test INJ-08: Score=100 | Passed 5/5 | PASS |
| `#AnswersJson` terisi non-empty saat submit | `page.evaluate(() => document.getElementById('AnswersJson').value)` non-empty | Passed | PASS |
| LBL-02 grep "Pilihan Ganda" = 0 match | `grep "Pilihan Ganda" InjectAssessment.cshtml` | 0 match | PASS |
| BLOCKING guard server-side | `FindBlockedAutoGenNips` + `TempData["Error"]` sebelum `InjectBatchAsync` | Wired `:66-72` | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|-------------|-----------|--------|----------|
| INJ-08 | Plan 01, 02, 03 | HC dapat menginput jawaban asli tiap pekerja per soal via form — MC/MA pilih opsi, Essay teks+skor — lalu sistem hitung skor via pipeline grading | SATISFIED | Form Step-5 input-asli + `InjectBatchAsync` commit aktual + integration test + e2e Score==preview bukan 0 |
| INJ-09 | Plan 01, 02, 03 | HC dapat auto-generate pola jawaban dari skor target + skor final aktual ditampilkan sebelum commit | SATISFIED | `BuildAutoGenAnswers`/`ComputeAutoGenSeed` + `PreviewInjectScore` endpoint + `step5RenderPreviewSurface` UI + integration preview==commit |

**Coverage: 2/2 requirements Phase 395 terpenuhi. Orphans: 0.**

---

### Anti-Patterns Found

| File | Baris | Pattern | Severity | Impact |
|------|-------|---------|----------|--------|
| Tidak ada | — | — | — | — |

Pemindaian anti-pattern dilakukan pada: `Services/InjectAssessmentService.cs`, `Controllers/InjectAssessmentController.cs`, `ViewModels/InjectAssessmentViewModel.cs`, `Models/InjectAssessmentDtos.cs`, `Views/Admin/InjectAssessment.cshtml`. Tidak ditemukan TODO/FIXME blocker, stub kosong, atau hardcoded empty yang mengalir ke render. Satu-satunya deviasi minor: D-10 pre-fill grid saat switch BLOCKING→input-asli tidak diimplementasi di klien karena server-otoritas (pola auto-gen MC/MA tidak ter-expose ke klien). Esensi D-10 (tombol "Beralih ke input asli" ada + mode di-set manual) terpenuhi — bukan blocker.

---

### Human Verification Required

Semua item telah diverifikasi via live browser UAT (verification_context): navigasi 1-pekerja-per-layar, toggle mode, input-asli MC/MA/Essay, auto-gen + Pratinjau, BLOCKING target>ceiling, commit "seakan online", DB cross-check preview==commit, LBL-02 label. UAT PASSED semua core path. FINDING-1 (D-04 essay teks kosong tidak diblok di Pratinjau) di-fix `50e7eb27` dan dikunci e2e regression.

**Tidak ada item tersisa yang memerlukan verifikasi manusia tambahan.**

---

### Deferred Items

Tidak ada item yang di-defer ke fase berikutnya dari must-have Phase 395.

*(Catatan: D-10 pre-fill grid BLOCKING→input-asli dilewati secara sengaja karena konflik dengan prinsip server-otoritas; dicatat di Plan 03 SUMMARY sebagai "deviasi minor, nice-to-have". Bukan requirement formal di ROADMAP atau REQUIREMENTS.md.)*

---

### Gaps Summary

Tidak ada gap. Semua 13 must-have truths VERIFIED, semua 8 artifacts VERIFIED + WIRED, semua key links WIRED, semua 2 requirement IDs SATISFIED, tidak ada anti-pattern blocker, tidak ada item human-verification tersisa.

---

_Verified: 2026-06-18T05:04:56Z_
_Verifier: Claude (gsd-verifier)_
