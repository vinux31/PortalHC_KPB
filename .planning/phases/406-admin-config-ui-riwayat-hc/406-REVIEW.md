---
phase: 406-admin-config-ui-riwayat-hc
reviewed: 2026-06-21T00:00:00Z
depth: standard
files_reviewed: 10
files_reviewed_list:
  - Helpers/RiwayatUnifier.cs
  - Models/RiwayatAttemptViewModel.cs
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/_RiwayatPercobaan.cshtml
  - Views/Admin/ManagePackages.cshtml
  - Views/Admin/CreateAssessment.cshtml
  - Views/Admin/EditAssessment.cshtml
  - Views/Admin/AssessmentMonitoringDetail.cshtml
  - tests/e2e/retake-config-406.spec.ts
  - tests/e2e/riwayat-hc-406.spec.ts
findings:
  critical: 0
  warning: 1
  info: 3
  total: 4
status: issues_found
---

# Phase 406: Code Review Report

**Reviewed:** 2026-06-21
**Depth:** standard
**Files Reviewed:** 10
**Status:** issues_found

## Summary

Phase 406 menambahkan UI admin untuk fitur Ujian Ulang (v32.4): helper murni `RiwayatUnifier`, ViewModel `RiwayatAttemptViewModel`, endpoint GET `RiwayatPercobaan`, partial `_RiwayatPercobaan`, kartu konfigurasi retake di `ManagePackages.cshtml`, binding `asp-for` di Create/Edit, serta modal lazy-fetch di `AssessmentMonitoringDetail.cshtml`.

Penilaian keseluruhan: **kuat pada poros keamanan yang menjadi fokus fase ini.** Empat concern utama yang diminta diteliti telah terverifikasi BERSIH:

- **XSS (PRIMARY): PASS.** Seluruh konten peserta (`QuestionText`, `AnswerText`) dirender via Razor `@` (auto HTML-escape). NOL `Html.Raw`. Judul modal di-set via `.textContent` (bukan `innerHTML`). Body modal diisi `innerHTML` dari partial yang sudah `@`-encoded di server. Test `riwayat-hc-406.spec.ts` (skenario `xss`) memverifikasi runtime payload `<script>` inert (sentinel `__riwayatXss406` undefined).
- **No answer-KEY leak: PASS.** Model `AssessmentAttemptResponseArchive` tidak memiliki field jawaban-benar/answer-key (hanya `QuestionText`, `AnswerText` peserta, `IsCorrect`, `AwardedScore`). `RetakeArchiveBuilder` hanya menulis jawaban peserta + verdict. Partial tidak pernah mengakses `PackageOption.IsCorrect`/teks opsi benar. Tidak ada kebocoran kunci jawaban.
- **RBAC: PASS.** `RiwayatPercobaan` di-`[Authorize(Roles = "Admin, HC")]` — identik dengan `AssessmentMonitoringDetail`. Tidak ada eskalasi role.
- **RiwayatUnifier correctness: PASS.** Grouping STRICT by `AttemptHistoryId` (anti salah-attach), tri-state `IsCorrect` (`bool?`) dipertahankan, `IsCurrent` benar, ordering `AttemptNumber` DESC, null-safety lengkap (semua input `?? new List`). Tipe sejajar (`int? Score → int? ScorePercent`). Tercakup 6 unit test.

Concern minor yang ditemukan: satu inkonsistensi tampilan tri-state pada badge header accordion (Warning) dan tiga catatan Info (IDOR by-design, kebijakan tampilan skor pending, efisiensi query). Tidak ada isu Critical.

## Warnings

### WR-01: Badge header accordion menampilkan "Gagal" untuk attempt pending (IsPassed null)

**File:** `Views/Admin/_RiwayatPercobaan.cshtml:21,28`
**Issue:** Badge Lulus/Gagal di header accordion dihitung dari `var passSuccess = attempt.IsPassed == true;` lalu dirender `text-bg-@(passSuccess ? "success" : "danger")` dengan teks `@(passSuccess ? "Lulus" : "Gagal")`. Untuk attempt yang `IsPassed == null` (belum dinilai / pending grading — kondisi yang secara eksplisit di-handle tri-state pada tabel per-soal), header malah menampilkan badge merah "Gagal". Ini menimbulkan inkonsistensi dengan prinsip tri-state fase ini (null = "Menunggu", bukan "Gagal") dan berpotensi menyesatkan HC: sebuah percobaan yang masih menunggu penilaian essay akan terlihat seperti sudah dinyatakan gagal di level ringkasan attempt. Skor pada header sudah benar menampilkan "—" saat null (`ScorePercent.HasValue ? ... : "—"`), sehingga ada ketidakselarasan internal (skor "—%" tapi badge "Gagal").
**Fix:** Tangani tri-state di header sama seperti tabel per-soal — mis. ekstrak ke tiga cabang:
```cshtml
@if (attempt.IsPassed == true)
{
    <span class="badge text-bg-success ms-2">Lulus</span>
}
else if (attempt.IsPassed == false)
{
    <span class="badge text-bg-danger ms-2">Gagal</span>
}
else
{
    <span class="badge text-bg-secondary ms-2">Menunggu penilaian</span>
}
```
Catatan: dampak praktis rendah karena attempt LIVE saat ini berasal dari sesi `Completed` (umumnya `IsPassed` non-null) dan attempt ter-arsip umumnya sudah punya verdict; namun arsip dengan essay pending dapat memunculkan `IsPassed == null` sehingga perbaikan tetap dianjurkan untuk konsistensi tri-state.

## Info

### IN-01: RiwayatPercobaan mengizinkan Admin/HC manapun melihat jawaban peserta lain (IDOR by-design)

**File:** `Controllers/AssessmentAdminController.cs:3485-3489`
**Issue:** Endpoint hanya membatasi role (`Admin, HC`) tanpa pengecekan kepemilikan/scope unit — Admin/HC manapun dapat memuat jawaban peserta manapun via `?sessionId=`. Ini KONSISTEN dengan model otorisasi monitoring yang sudah ada (mis. `EssayGrading`, `EditHistoryPartial`, `AssessmentMonitoringDetail` semuanya role-only), jadi diterima by-design untuk fase ini dan BUKAN regresi. Dicatat agar terlacak: jika di masa depan diberlakukan unit-scoping HC (mis. v32.3 multi-unit), endpoint ini perlu ikut difilter UserUnit seperti surface monitoring lainnya.
**Fix:** Tidak ada aksi sekarang (sesuai pola eksisting). Saat unit-scoping HC diaktifkan app-wide, tambahkan guard scope unit yang sama dengan `AssessmentMonitoringDetail`.

### IN-02: Current attempt menampilkan badge "Gagal" + skor "—" bila session.Score null

**File:** `Helpers/RiwayatUnifier.cs:59-62`, `Views/Admin/_RiwayatPercobaan.cshtml:27-28`
**Issue:** Untuk attempt LIVE saat ini, `ScorePercent`/`IsPassed` diambil langsung dari `current?.Score`/`current?.IsPassed`. Endpoint hanya menambahkan current rows saat `session.Status == "Completed"`, namun sesi Completed dengan essay belum difinalisasi bisa memiliki `Score`/`IsPassed` null → header current menampilkan "—%" + badge "Gagal" (turunan dari WR-01). Setelah WR-01 diperbaiki, kasus ini otomatis menjadi "Menunggu penilaian" yang benar.
**Fix:** Tercakup oleh fix WR-01 (tri-state header). Tidak perlu perubahan terpisah di unifier — provenance dari session sudah benar.

### IN-03: Query histories + archive berpotensi besar tanpa paging (skala terbatas)

**File:** `Controllers/AssessmentAdminController.cs:3493-3501`
**Issue:** `histories` dan `archiveRows` di-`ToListAsync()` tanpa batas. Per worker per (Title, Category) jumlah attempt dibatasi domain (`MaxAttempts` 1–5) sehingga volume baris sangat kecil — bukan masalah nyata. Pola query sudah efisien: satu query histories, satu query archive via `histIds.Contains(...)` (tidak ada N+1; current rows di-load dengan `Include(q => q.Options)` sekali). Dicatat hanya sebagai kelengkapan; tidak ada aksi diperlukan. (Catatan: isu performa di luar scope v1, dicantumkan karena relevan dengan robustness endpoint.)
**Fix:** Tidak ada aksi. Volume dibatasi secara domain oleh `MaxAttempts`.

---

_Reviewed: 2026-06-21_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
