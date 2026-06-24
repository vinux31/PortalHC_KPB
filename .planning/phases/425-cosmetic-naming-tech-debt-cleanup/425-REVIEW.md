---
phase: 425-cosmetic-naming-tech-debt-cleanup
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 13
files_reviewed_list:
  - Controllers/TrainingAdminController.cs
  - Controllers/AssessmentAdminController.cs
  - Controllers/CMPController.cs
  - Helpers/ManualEntryRules.cs
  - Helpers/ControllerGuards.cs
  - Models/AssessmentSession.cs
  - Models/UserPackageAssignment.cs
  - Views/Admin/ManageAssessment.cshtml
  - Views/Admin/CreateAssessment.cshtml
  - Views/Admin/EditAssessment.cshtml
  - HcPortal.Tests/ManualEntryRulesTests.cs
  - HcPortal.Tests/ControllerGuardsTests.cs
  - HcPortal.Tests/ExamTimeRulesTests.cs
findings:
  critical: 0
  warning: 0
  info: 1
  total: 1
status: issues_found
---

# Phase 425: Code Review Report

**Reviewed:** 2026-06-24
**Depth:** standard
**Files Reviewed:** 13
**Status:** issues_found (1 Info, non-blocking)

## Ringkasan

Phase 425 (v32.7 final) adalah fase pembersihan cosmetic/tech-debt risiko-rendah: CLN-01 (label/XML-doc/komentar), CLN-02 (peringatan cross-validate non-blocking entri manual), CLN-03 (RESERVED `AssessmentPhase` via XML-doc), CLN-04 (konsolidasi formula timer ke `ExamTimeRules.AllowedExamSeconds`), CLN-05 (helper `ControllerGuards.JsonFail` selektif ke cluster `SubmitEssayScore`).

Saya memverifikasi keempat fokus risiko yang diminta. Semua LULUS:

- **(a) Parity timer (CLN-04):** Keempat situs CMPController (:1191, :1564, :1642 int-detik; :4661 menit→double) sekarang memanggil `ExamTimeRules.AllowedExamSeconds(DurationMinutes, ExtraTimeMinutes)` yang mengembalikan `(durationMinutes + (extraTimeMinutes ?? 0)) * 60` — numerik identik dengan formula inline lama. Tidak ada off-by-one maupun truncation: helper tetap int (`* 60`), dan pada situs :4661 hasil int di-assign implicit ke `double allowedSec`, identik dengan `allowedMinutes * 60.0` lama. `int allowedMinutes` di :4661 SENGAJA dipertahankan (masih dipakai `WriteSubmitBlockedAuditAsync` untuk audit satuan menit, bukan dead code). Test parity baru `ExamTimeRulesTests` mengonfirmasi (termasuk situs double :4661).

- **(b) CLN-02 null-safety + non-blocking + XSS-safe:** `ManualEntryRules.PassStatusMismatch(int? score, int passPercentage, bool isPassed)` melakukan `score.HasValue &&` lebih dulu → `null` ⇒ `false` (tidak ada NRE/short-circuit aman). Signature cocok persis dengan tipe model (`Score` = `int?`, `PassPercentage` = `int`, `IsPassed` = `bool`). Wiring di `AddManualAssessment` HANYA men-set `TempData["Warning"]` tanpa `ModelState.AddModelError`, tanpa `return`, dan TIDAK memutasi `model.Score`/`IsPassed` → benar-benar non-blocking, tidak auto-override (eksekusi lanjut ke build session + `SaveChanges`). Pesan hanya berisi nilai numerik (`Score`, `PassPercentage`) + teks statis; di-render via `@TempData["Warning"]` (Razor auto-encode) → XSS-safe. Redirect tujuan `ManageAssessment` (AssessmentAdmin) memang merender blok Warning → peringatan tampil ke user.

- **(c) CLN-05 JSON shape byte-identik:** Frontend `wwwroot/js/essay-grading.js:56-68` membaca `data.success` (bool) dan `data.message` (string) pada kegagalan. `JsonFail` mengembalikan `new JsonResult(new { success = false, message })`. `Program.cs` TIDAK punya `AddJsonOptions`/`AddNewtonsoftJson` kustom → default System.Text.Json camelCase. `ControllerBase.Json(obj)` dan `new JsonResult(obj)` keduanya lewat `JsonResultExecutor` dengan `JsonOptions` MVC yang sama → output byte-identik `{"success":false,"message":"..."}`. Test `ControllerGuardsTests` mengonfirmasi parity + byte-exact, termasuk pesan dinamis interpolasi. Jalur sukses (`success=true, pendingCount, allGraded`) sengaja TIDAK dimigrasi — tetap konsisten dengan apa yang dibaca frontend.

- **(d) Tidak ada pelemahan [Authorize]/[ValidateAntiForgeryToken]:** `SubmitEssayScore` tetap `[HttpPost] [Authorize(Roles = "Admin, HC")] [ValidateAntiForgeryToken]`. `AddManualAssessment` tetap `[HttpPost] [ValidateAntiForgeryToken] [Authorize(Roles = "Admin, HC")]`. Diff hanya mengganti body return/menambah blok TempData; tidak ada atribut keamanan yang disentuh.

Keputusan scope yang SENGAJA (RESERVED `AssessmentPhase`, subset selektif CLN-05) TIDAK saya flag sebagai defect — XML-doc/komentar yang mendokumentasikan keduanya akurat (sudah saya verifikasi silang: `AssessmentPhase` 0 referensi baca/tulis; `JsonFail` hanya diterapkan ke cluster `SubmitEssayScore`).

Semua perubahan dokumentasi (XML-doc FK app-level PA-04, sentinel `AssessmentPackageId` PA-05, komentar 7-nilai `Status`, label "Berlaku Sampai") konsisten dan tidak mengubah perilaku. Saya verifikasi label cshtml ("Berlaku Sampai") kini selaras dengan `[Display(Name = "Berlaku Sampai")]` pada `ValidUntil`.

Tidak ditemukan isu Critical maupun Warning. Satu observasi Info di bawah.

## Info

### IN-01: Peringatan `TempData["Warning"]` ter-render ganda di halaman ManageAssessment

**File:** `Views/Admin/ManageAssessment.cshtml:41-48` (interaksi dengan `Views/Shared/_Layout.cshtml:190-199`)

**Issue:** `ManageAssessment.cshtml` memakai `_Layout` (via `Views/_ViewStart.cshtml`). `_Layout.cshtml` sudah merender `TempData["Warning"]` secara global di baris 190 (sebelum `@RenderBody()` di baris 233). Blok Warning baru yang ditambahkan CLN-02 di `ManageAssessment.cshtml:41` ada di dalam body (di-render via `@RenderBody`). Read indexer `TempData["Warning"]` tidak meng-clear nilai dalam request yang sama, sehingga setelah `AddManualAssessment` redirect ke `ManageAssessment` dengan Warning ter-set, alert akan tampil DUA kali pada halaman yang sama (satu dari layout, satu dari body).

Catatan konteks (menurunkan severity ke Info): pola double-render ini SUDAH ADA sebelum Phase 425 untuk `TempData["Success"]` dan `TempData["Error"]` — `ManageAssessment.cshtml` memang punya blok Success/Error sendiri (baris 33 & 48) yang juga digandakan oleh `_Layout`. Blok Warning baru hanya mengikuti konvensi (yang sudah tidak ideal) di view yang sama, bukan anomali baru yang berdiri sendiri. Murni cosmetic — tidak ada dampak fungsional, keamanan, atau data.

**Fix (opsional, jika ingin menghilangkan duplikasi):** Hapus blok Warning lokal di `ManageAssessment.cshtml:41-48` dan andalkan render global `_Layout` (sekaligus konsisten dengan menghilangkan duplikasi Success/Error lama). Alternatif: jika styling alert lokal disengaja (ikon `bi-exclamation-triangle` tanpa prefix "Warning:"), gunakan `TempData.Peek`/key terpisah agar tidak bentrok dengan render layout. Mengingat ini hanya mirror pola eksisting dan risiko-rendah, dapat juga dibiarkan untuk konsistensi visual dengan Success/Error di view yang sama.

---

_Reviewed: 2026-06-24_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
