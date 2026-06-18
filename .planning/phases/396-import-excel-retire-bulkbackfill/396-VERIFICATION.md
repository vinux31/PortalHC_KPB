---
phase: 396-import-excel-retire-bulkbackfill
verified: 2026-06-18T00:00:00Z
status: passed
score: 5/5 must-haves verified
overrides_applied: 0
---

# Phase 396: Import Excel + Retire BulkBackfill — Laporan Verifikasi

**Phase Goal:** HC dapat meng-inject jawaban/skor batch via Excel matrix dengan template yang ter-generate dari paket soal, validasi atomic, dan tool lama BulkBackfill dipensiunkan/diarahkan sehingga tidak ada dua tool duplikat.
**Verified:** 2026-06-18
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (Success Criteria ROADMAP)

| #  | Truth (dari ROADMAP Phase 396)                                                                                                                                  | Status     | Bukti                                                                                                      |
|----|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|------------|------------------------------------------------------------------------------------------------------------|
| 1  | HC dapat men-download template Excel yang ter-generate dari paket soal inject (baris = NIP, kolom = soal), mengisi jawaban, dan meng-upload untuk inject batch. | ✓ VERIFIED | `DownloadInjectTemplate` POST + `UploadInjectExcel` POST ada di controller. Template 2-sheet (Jawaban + Legenda) dikonfirmasi UAT 5/5. |
| 2  | Parser memvalidasi NIP wajib ada di sistem dan huruf opsi valid; bila ada baris invalid, seluruh import di-rollback dan HC mendapat pesan error jelas.           | ✓ VERIFIED | `ParseMatrix` NIP-not-in-picker → `InjectRowError` (D-02). Invalid letter "E" → error per-sel (D-09). Atomic: `UploadInjectExcel` dengan `errors.Count>0` → `Ok=false`, 0 DB write. Integration test `ExcelPath_AnyError_AtomicRollback` 3/3 PASSED. |
| 3  | Import Excel menghasilkan sesi identik dengan jalur form (lewat `InjectAssessmentService` yang sama) — diverifikasi hasil di `/CMP/Records` + `/CMP/Results`.  | ✓ VERIFIED | `BuildExcelPreviews` reuse `MapToInMemory` + `AssessmentScoreAggregator.Compute` (engine sama dengan commit). Integration test `ExcelPath_ProducesSameScore_AsFormPath` (preview==commit) 3/3 PASSED. UAT: DB session Score=100, IsPassed=1, IsManualEntry=1, muncul di `/CMP/Results` per-soal. |
| 4  | Tool lama BulkBackfill dipensiunkan — tidak ada lagi dua entry-point yang melakukan inject hasil assessment.                                                    | ✓ VERIFIED | `grep BulkBackfill Controllers/ Views/` → 0 (hanya 2 komentar kosmetik non-routing). `BulkBackfill.cshtml` dihapus. `Index.cshtml` + `_AssessmentGroupsTab.cshtml` tanpa link BulkBackfill. Route `/Admin/BulkBackfill` → 404 (Playwright Scenario 6 PASS). |
| 5  | `dotnet build` 0 error + `dotnet test` (parser/validasi/rollback) + `dotnet run` + Playwright e2e hijau; BulkBackfill lama tak lagi dapat dipakai.              | ✓ VERIFIED | Build: 0 error. Fast unit: 389/389. Integration InjectExcelImport: 3/3. Playwright 6 scenarios green. UAT live browser 5/5. 0 migration. |

**Score: 5/5 truths verified**

---

### Required Artifacts

| Artifact                                        | Expected                                                                                        | Status     | Detail                                                                                                |
|-------------------------------------------------|-------------------------------------------------------------------------------------------------|------------|-------------------------------------------------------------------------------------------------------|
| `Helpers/InjectExcelHelper.cs`                  | Static EF-free `GenerateTemplate` + `ParseMatrix` menggunakan ClosedXML                        | ✓ VERIFIED | 282 baris. Berisi `public static class InjectExcelHelper`. EF-free: 0 `ApplicationDbContext`. ONE comparator `OrderBy(q=>q.Order).ThenBy(q=>q.TempId)` dipakai di KEDUANYA (baris 48 + 170). |
| `HcPortal.Tests/InjectExcelHelperTests.cs`      | 8 fact unit test (round-trip, A=Options[0], blank=omit, MA koma, essay teks-opsional, validasi) | ✓ VERIFIED | 298 baris. 8 `[Fact]`. Tidak ada `[Trait("Category","Integration")]`. Semua GREEN (fast suite 389/389). |
| `HcPortal.Tests/InjectExcelImportTests.cs`      | 3 fact integration: Excel==form, atomic rollback, essay text-optional                           | ✓ VERIFIED | 322 baris. `[Trait("Category","Integration")]`. 3 `[Fact]`: preview==commit + rollback + EssayTextRequired both-direction. 3/3 PASSED. |
| `Controllers/InjectAssessmentController.cs`     | `DownloadInjectTemplate` GET + `UploadInjectExcel` POST (RBAC+CSRF+10MB+whitelist)              | ✓ VERIFIED | Kedua endpoint ada. `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` pada keduanya. `[RequestFormLimits(10MB)]` + `.xlsx`/`.xls` whitelist pada upload. `AssessmentScoreAggregator.Compute` dipakai di `BuildExcelPreviews`. `EssayTextRequired=false` di-set pada jalur Excel. |
| `Models/InjectAssessmentDtos.cs`                | `InjectExcelUploadResult` + `InjectExcelPreviewRow` + `EssayTextRequired` flag                 | ✓ VERIFIED | `class InjectExcelUploadResult` ada. `class InjectExcelPreviewRow` ada. `EssayTextRequired { get; set; } = true` ada (default true = form path preserved). |
| `ViewModels/InjectAssessmentViewModel.cs`       | `Step5Method` flag untuk toggle Form/Excel                                                      | ✓ VERIFIED | `public string? Step5Method { get; set; }` ada.                                                       |
| `Views/Admin/InjectAssessment.cshtml`           | Step-5 N1 toggle + N2 Excel panel + N3 preview table + N4 error list (Bahasa Indonesia)        | ✓ VERIFIED | `name="step5Method"` radio (MethodForm/MethodExcel). `#step5FormPath` wrapper. `#step5ExcelPanel`. `#btnDownloadTemplate`, `#step5ExcelFile`, `#btnUploadExcel`. `#step5ExcelErrors` + `#step5ExcelPreview`. Semua Bahasa Indonesia. XSS-safe: `.textContent`. |
| `Services/InjectAssessmentService.cs`           | `EssayTextRequired`-guarded text-required rule (D-05 form-only)                                | ✓ VERIFIED | Baris 397: `if (req.EssayTextRequired && ans.EssayScore.HasValue && ...)` — default true = form path tidak berubah; false = Excel path boleh essay tanpa teks. |
| `Controllers/TrainingAdminController.cs`        | BulkBackfill GET+POST dihapus; `CleanupAttemptHistory` + `ClosedXML` + `ManualDuplicatePredicate` dipertahankan | ✓ VERIFIED | `grep BulkBackfill TrainingAdminController.cs` → 0 match. `CleanupAttemptHistory` + `CleanupAttemptHistoryExecute` masih ada. `using ClosedXML.Excel` baris 8 ada. `new XLWorkbook` ≥3 hits (training template/import paths intact). |
| `Views/Admin/BulkBackfill.cshtml`               | File dihapus                                                                                    | ✓ VERIFIED | File tidak ada di filesystem (Glob → no match).                                                       |
| `Views/Admin/Index.cshtml`                      | Section D BulkBackfill card dihapus                                                             | ✓ VERIFIED | `grep BulkBackfill Views/Admin/Index.cshtml` → 0 match.                                              |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml`| BulkBackfill dropdown-item + orphan divider dihapus                                             | ✓ VERIFIED | `grep BulkBackfill _AssessmentGroupsTab.cshtml` → 0 match.                                           |
| `tests/e2e/inject-excel-396.spec.ts`            | Playwright: toggle + download + upload-success + upload-error + commit + Scenario 6 route-404  | ✓ VERIFIED | File ada. 6 skenario (5 INJ-10 + 1 INJ-11). `mode: 'serial'`. DB snapshot/restore. Semua green.      |

---

### Key Link Verification

| From                                    | To                                         | Via                                               | Status     | Detail                                                                                                  |
|-----------------------------------------|--------------------------------------------|---------------------------------------------------|------------|---------------------------------------------------------------------------------------------------------|
| `Views/Admin/InjectAssessment.cshtml`   | `POST UploadInjectExcel`                   | fetch dengan FormData + antiforgery               | ✓ WIRED    | `data-upload-url="@Url.Action("UploadInjectExcel")"` pada tombol. JS fetch di IIFE Phase 396 membangun FormData + token antiforgery. |
| `Views/Admin/InjectAssessment.cshtml`   | `#AnswersJson`                             | submit listener menulis `excelAnswersCache` saat method=excel | ✓ WIRED | Baris 1088-1090: `isExcel ? (window.injExcelAnswersCache \|\| '[]') : buildWorkerAnswersPayload()`. Cache di-set HANYA setelah upload 0-error. |
| `Controllers/InjectAssessmentController`| `Helpers/InjectExcelHelper`                | `GenerateTemplate` + `ParseMatrix` calls           | ✓ WIRED    | Baris 192: `using var wb = InjectExcelHelper.GenerateTemplate(...)`. Baris 253: `(workers, errors, skippedBlank) = InjectExcelHelper.ParseMatrix(...)`. |
| `Controllers/InjectAssessmentController`| `Helpers/AssessmentScoreAggregator`        | `BuildExcelPreviews` reuse `MapToInMemory` + `Compute` | ✓ WIRED | Baris 295: `var agg = AssessmentScoreAggregator.Compute(...)`. Sama dengan commit path → preview==commit. |
| `Helpers/InjectExcelHelper`             | `ViewModels/InjectAssessmentViewModel`     | emits `List<InjectWorkerAnswersVM>` Mode="manual" | ✓ WIRED    | `InjectAssessmentViewModel.InjectWorkerAnswersVM` dipakai sebagai output `ParseMatrix`.                 |
| `tests/e2e/inject-excel-396.spec.ts`   | `/Admin/BulkBackfill` route                | navigation expects 404                            | ✓ WIRED    | Baris 351-353: `page.goto('/Admin/BulkBackfill')` → `expect(status).toBe(404)`. Scenario 6 green.      |
| `Services/InjectAssessmentService.cs`  | `EssayTextRequired` flag                   | `req.EssayTextRequired` guard di `PreflightValidateAsync` | ✓ WIRED | Baris 397: guard dikondisikan oleh flag. Controller set `false` saat `vm.Step5Method == "excel"`. |

---

### Data-Flow Trace (Level 4)

| Artifact                        | Data Variable        | Source                                                   | Produces Real Data                          | Status    |
|---------------------------------|----------------------|----------------------------------------------------------|---------------------------------------------|-----------|
| `InjectExcelHelper.ParseMatrix` | `workers` (output)   | Baca sel sheet "Jawaban"; validasi vs `allowedNips`      | Ya — sel dari file upload yang diunggah HC  | ✓ FLOWING |
| `UploadInjectExcel` endpoint    | `result.AnswersJson` | `InjectExcelHelper.ParseMatrix` → serialized workers JSON | Ya — dari parse matrix nyata                | ✓ FLOWING |
| `BuildExcelPreviews`            | preview rows          | `AssessmentScoreAggregator.Compute` pada worker yang di-parse | Ya — skor dihitung oleh engine grading | ✓ FLOWING |
| View Step-5 Excel preview table | DOM rows              | Fetch `UploadInjectExcel` → JSON `Previews`              | Ya — dari engine grading                    | ✓ FLOWING |
| Commit via `#btnInject`         | `#AnswersJson`        | `window.injExcelAnswersCache` (di-set dari `result.answersJson`) | Ya — set hanya setelah upload 0-error | ✓ FLOWING |

---

### Behavioral Spot-Checks

| Behavior                                                    | Hasil Verifikasi                     | Status  |
|-------------------------------------------------------------|--------------------------------------|---------|
| Unit tests InjectExcelHelperTests (8 fact)                  | 389/389 fast suite GREEN             | ✓ PASS  |
| Integration InjectExcelImportTests (3 fact real SQL)        | 3/3 PASSED                           | ✓ PASS  |
| Playwright e2e inject-excel-396 (6 skenario)                | 6/6 GREEN (MAIN tree, AD-off)        | ✓ PASS  |
| Live browser UAT orchestrator-driven (5 skenario)           | 5/5 APPROVED                         | ✓ PASS  |
| Route `/Admin/BulkBackfill` → 404                           | Playwright Scenario 6 PASS + curl konfirmasi SUMMARY | ✓ PASS |
| Route `/Admin/BulkBackfillAssessment` (POST) → 404          | Playwright Scenario 6 PASS           | ✓ PASS  |
| Entry-point UI BulkBackfill (kartu + dropdown) hilang       | Playwright Scenario 6 PASS           | ✓ PASS  |
| `CleanupAttemptHistory` + `ClosedXML` + `ManualDuplicatePredicate` tetap ada | DuplicateGuardTests 9/9 PASSED | ✓ PASS |

---

### Requirements Coverage

| REQ-ID | Source Plan | Deskripsi                                                                                                  | Status     | Bukti                                                                                            |
|--------|-------------|-----------------------------------------------------------------------------------------------------------|------------|--------------------------------------------------------------------------------------------------|
| INJ-10 | 396-01..04  | Import jawaban/skor batch via Excel — template ter-generate + matrix parser (baris=NIP, kolom=soal) atomic via `InjectAssessmentService` | ✓ SATISFIED | `InjectExcelHelper` (gen+parse, 282 baris), endpoints `DownloadInjectTemplate`/`UploadInjectExcel`, view Step-5 toggle+panel+preview+errors, unit 8/8, integration 3/3, e2e 5 skenario, UAT 5/5. |
| INJ-11 | 396-05      | Retire BulkBackfill — tidak ada dua entry-point duplikat                                                  | ✓ SATISFIED | BulkBackfill GET+POST dihapus dari `TrainingAdminController.cs`. `BulkBackfill.cshtml` dihapus. Section D card + dropdown-item hilang. Route 404 diverifikasi runtime. `CleanupAttemptHistory` + ClosedXML + `ManualDuplicatePredicate` dipertahankan. |

**Coverage: 2/2 REQ fase 396 (INJ-10 + INJ-11) SATISFIED.**

INJ-12 (Phase 397) dan INJ-13 (Phase 398) adalah REQ milestone berikutnya — bukan scope Phase 396, tidak di-verifikasi di sini.

---

### Anti-Patterns Found

| File                                    | Pola                                                                           | Severity | Impact                                                                                                                    |
|-----------------------------------------|--------------------------------------------------------------------------------|----------|---------------------------------------------------------------------------------------------------------------------------|
| `Controllers/InjectAssessmentController.cs:244-245` | `ToDictionary(p => p.Nip)` tanpa duplikat-guard (WR-01)            | ⚠️ Warning | Throw `ArgumentException` bila 2 pekerja ber-NIP sama di picker. Pre-existing di service juga. Advisory; tidak memblokir INJ-10/11 karena NIP-sharing jarang dan dibatasi oleh picker. |
| `Helpers/InjectExcelHelper.cs:200-204`  | Skor essay `double` di-cast ke `int` tanpa cek fraksi (WR-02)                 | ⚠️ Warning | `8.7` → `8` diam-diam. Inkonsisten dengan text path (`int.TryParse` tolak non-integer). Correctness concern untuk skor sertifikasi. Advisory — tidak memblokir. |
| `Views/Admin/InjectAssessment.cshtml:1737-1739` | `tmp.submit(); tmp.removeChild(tmp)` synchronous (WR-03)           | ⚠️ Warning | Fragile di sebagian browser (race condition). Lulus e2e Chromium. Advisory.                                               |
| `Controllers/InjectAssessmentController.cs:172-177` | `DownloadInjectTemplate` error path → `View()` full re-render (IN-01) | ℹ️ Info | Kehilangan client state bila path empty-questions/0-workers. Sulit dicapai (UI sudah gate). Info only. |
| `Controllers/InjectAssessmentController.cs:217` | Whitelist `.xls` tapi ClosedXML hanya baca `.xlsx` (IN-04) | ℹ️ Info | Operator dikelirukan bila upload `.xls` asli. Error message ramah sudah ada. Info only. |

Tidak ada anti-pattern blocker (STUB, MISSING, atau ORPHANED yang menghalangi goal). WR-01..03 adalah advisory/non-blocking sesuai code review yang sudah disetujui.

---

### Human Verification Required

Semua verifikasi sudah diselesaikan secara otomatis maupun via UAT live browser yang dilakukan oleh orchestrator dengan Playwright MCP (5/5 APPROVED sebelum phase ini selesai). Tidak ada item yang memerlukan verifikasi manusia tambahan.

---

## Gaps Summary

Tidak ada gap. Phase 396 mencapai goal-nya sepenuhnya:

1. **INJ-10 (Import Excel):** Template 2-sheet ter-generate dari paket soal dengan format matrix (baris=NIP, kolom=soal). Validasi atomic: NIP wajib di picker, huruf opsi valid, rollback total bila ada error. Import menghasilkan sesi identik jalur form (engine grading sama). UI Step-5 toggle Form/Excel, preview batch (skor/lulus/terjawab, tanpa cert#), error list lengkap, blank-cell warn-but-allow. 8 unit fact + 3 integration fact + 5 e2e skenario + UAT 5/5 semua hijau.

2. **INJ-11 (Retire BulkBackfill):** Dua action `BulkBackfill`/`BulkBackfillAssessment` dihapus dari `TrainingAdminController.cs`. View `BulkBackfill.cshtml` dihapus. Section D card dan dropdown-item hilang dari UI. Route 404 diverifikasi runtime. `CleanupAttemptHistory`, `ManualDuplicatePredicate`, dan `using ClosedXML.Excel` dipertahankan. `DuplicateGuardTests` 9/9 tetap hijau.

3 Warning dari code review (WR-01 dup-NIP, WR-02 fraksi essay, WR-03 hidden-form race) bersifat advisory dan tidak memblokir pencapaian goal — sesuai keputusan code review 2026-06-18 (0 Critical).

**0 migration** dikonfirmasi (tidak ada perubahan entity/DbContext/Migrations).

---

_Verified: 2026-06-18_
_Verifier: Claude (gsd-verifier)_
