---
phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
reviewed: 2026-06-17T00:00:00+07:00
depth: standard
files_reviewed: 2
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - HcPortal.Tests/FlexibleParticipantAddTests.cs
findings:
  critical: 0
  warning: 2
  info: 1
  total: 3
status: issues_found
---

# Phase 391: Code Review Report

**Reviewed:** 2026-06-17
**Depth:** standard
**Files Reviewed:** 2
**Status:** issues_found

## Summary

Phase 391 memperkenalkan tiga keputusan logika utama: (1) `DeriveReadyStatus` helper yang mirror `CMPController.StartExam` untuk memberi status siap-mulai pada sesi peserta baru, (2) guard `hasAddition` yang melonggarkan blokir `Completed` selama ada penambahan peserta, dan (3) skip in-progress pada sibling edit loop dan Pre-Post per-phase loops untuk melindungi timer peserta yang sedang ujian.

Secara keseluruhan, logika inti sudah benar dan konvensi proyek diikuti dengan baik: `AssessmentConstants` digunakan secara konsisten, WIB = `DateTime.UtcNow.AddHours(7)` yang konsisten dengan d844c552 fix, IDOR tidak terbuka (controller terlindungi `[Authorize(Roles = "Admin, HC")]` + `AdminBaseController [Authorize]`), dan tes disposable DB telah ditulis.

Dua **Warning** ditemukan: satu logika bug potensial pada guard `Completed` yang tidak mencakup status `PendingGrading`, satu race condition minor pada `savedAssessment` re-load. Satu **Info** untuk parameter tak terpakai `examWindowCloseDate` di signature helper.

---

## Warnings

### WR-01: Guard `Completed` Tidak Mencakup `PendingGrading` — Edit Murni Bisa Lolos pada Sesi Essay

**File:** `Controllers/AssessmentAdminController.cs:1997-2001`

**Issue:** Guard di D-02 hanya memeriksa `assessment.Status == AssessmentConstants.AssessmentStatus.Completed`. Namun sesi representatif yang memiliki soal essay setelah selesai dikerjakan berstatus `PendingGrading` ("Menunggu Penilaian"), bukan `Completed` — karena penilaian essay belum selesai. Artinya, jika sesi representatif bertatus `PendingGrading` dan `NewUserIds` kosong (edit murni tanpa penambahan peserta), guard **tidak akan memblokir edit tersebut**. Admin bisa mengubah `Title`, `Schedule`, `DurationMinutes`, atau bahkan `Status` pada sesi yang sudah dalam antrian penilaian essay, karena kondisi bypass `!hasAddition` tidak dievaluasi sama sekali — cek `assessment.Status == Completed` langsung gagal dan guard dilewati.

Kondisi sebelum Phase 391 menggunakan string literal `"Completed"` sehingga secara tidak langsung sudah menderita masalah yang sama. Phase ini tidak memperburuk, tetapi juga tidak memperbaiki.

**Konteks:** `AssessmentConstants.IsFinished(status)` di `Models/AssessmentConstants.cs:89` sudah menyediakan helper yang mencakup keduanya: `status == Completed || status == PendingGrading`.

**Fix:**
```csharp
// Ganti:
if (assessment.Status == AssessmentConstants.AssessmentStatus.Completed && !hasAddition)

// Menjadi:
if (AssessmentConstants.AssessmentStatus.IsFinished(assessment.Status) && !hasAddition)
```

Ini memastikan edit murni (tanpa penambahan peserta) pada sesi `PendingGrading` juga diblokir, konsisten dengan intent asli guard.

---

### WR-02: `savedAssessment` Re-load Bisa Membaca Data Lama Karena EF Change Tracker

**File:** `Controllers/AssessmentAdminController.cs:2133-2136`

**Issue:** Setelah `SaveChangesAsync()` pada sibling update, BULK ASSIGN melakukan `_context.AssessmentSessions.FindAsync(id)` untuk mendapatkan `savedAssessment` — field yang dipakai untuk menyalin konfigurasi ke sesi baru. Karena EF Core `FindAsync` menggunakan **identity map / change tracker**, jika session dengan `id` tersebut sudah ada di tracker (sibling dengan `id` sama sudah dimuat di loop), `FindAsync` mengembalikan **entitas yang sudah ada di memory**, termasuk nilai-nilai yang baru diubah di loop sibling sebelum `SaveChangesAsync`. Ini sebenarnya menguntungkan — sesi baru akan mendapat nilai terbaru.

Namun ada satu skenario bug: jika sesi representatif (dengan `id`) berstatus `InProgress` (StartedAt set, CompletedAt null), maka ia di-**skip** di sibling loop sehingga field-nya **tidak diperbarui di memory** meski sibling lain sudah diperbarui. Ketika BULK ASSIGN membaca `savedAssessment` via `FindAsync(id)`, ia akan mendapat field lama (Schedule, DurationMinutes, dll. dari DB) dari sesi yang berstatus InProgress — yang mungkin berbeda dengan apa yang di-submit di form. Sesi peserta baru akan mewarisi konfigurasi sesi InProgress, bukan konfigurasi yang baru diupdate admin.

**Skenario konkret:** Admin membuka EditAssessment sesi `id=5` (yang sedang InProgress), mengubah `Schedule` dan `DurationMinutes`, lalu menambahkan peserta baru. Sesi baru akan mendapat `Schedule` dan `DurationMinutes` lama karena sesi `id=5` tidak diupdate di loop sibling.

**Fix (dua opsi):**

Opsi A — Baca `savedAssessment` dari sibling non-InProgress yang sudah diperbarui:
```csharp
// Ambil satu sibling yang BUKAN sedang berjalan untuk dijadikan template
var templateSession = await _context.AssessmentSessions
    .Where(a => a.Title == origTitle && a.Category == origCategory
             && a.Schedule.Date == origScheduleDate
             && !(a.StartedAt != null && a.CompletedAt == null))
    .FirstOrDefaultAsync();
// Fallback ke savedAssessment jika semua InProgress
if (templateSession == null) templateSession = savedAssessment;
```

Opsi B (lebih sederhana) — Gunakan `model` langsung untuk field yang diupdate:
```csharp
Status = DeriveReadyStatus(model.Schedule, model.ExamWindowCloseDate),
Schedule = model.Schedule,
DurationMinutes = model.DurationMinutes,
// dst.
```

Opsi B lebih aman dan tidak bergantung pada re-load.

---

## Info

### IN-01: Parameter `examWindowCloseDate` di `DeriveReadyStatus` Tidak Digunakan — Nama Misleading

**File:** `Controllers/AssessmentAdminController.cs:2243-2250`

**Issue:** Signature `DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)` menerima `examWindowCloseDate` tetapi tidak menggunakannya dalam implementasi. Komentar di header sudah menjelaskan ini intentional (status hanya bergantung pada `schedule`), dan mirror `CMPController.StartExam:915` juga hanya memeriksa jadwal untuk transisi Upcoming→Open.

Meski intentional, parameter yang tidak dipakai di signature publik (atau private static) bisa menyesatkan reviewer ke depan dan menyebabkan compiler warning `CS1998`-style (bukan error di C#, tetapi parameter tak terpakai bisa ditandai oleh analisis statis/Roslyn). Tes di `FlexibleParticipantAddTests.cs:91` pun mereplikasi signature yang sama dengan parameter tak terpakai.

**Fix:** Hapus parameter `examWindowCloseDate` dari signature jika memang tidak akan dipakai, atau beri `_` prefix untuk menegaskan intentional:
```csharp
// Opsi: hapus parameter tak terpakai
private static string DeriveReadyStatus(DateTime schedule)
{
    var nowWib = DateTime.UtcNow.AddHours(7);
    return schedule <= nowWib
        ? AssessmentConstants.AssessmentStatus.Open
        : AssessmentConstants.AssessmentStatus.Upcoming;
}
```
Update panggilan di L2171 dan test mirror di `FlexibleParticipantAddTests.cs:91` sesuai.

---

## Catatan Positif (tidak menghasilkan finding, tercatat untuk konteks)

- **WIB correctness:** `DateTime.UtcNow.AddHours(7)` konsisten dengan `CMPController.StartExam:915` dan fix d844c552. Tidak ada regresi.
- **Konvensi constants:** Guard baru menggunakan `AssessmentConstants.AssessmentStatus.Completed` menggantikan string literal `"Completed"` — peningkatan positif.
- **Tes disposable DB:** Fixture `FlexibleParticipantAddFixture` membuat DB `HcPortalDB_Test_{guid}` unik per-run dan `DisposeAsync` memanggil `EnsureDeletedAsync`. Cleanup terjamin, tidak ada risiko polusi `HcPortalDB_Dev`.
- **Fact (c) membuktikan filter selektif, bukan no-op:** Tes memverifikasi bahwa sesi non-InProgress JUSTRU berubah (bukan hanya memeriksa sesi InProgress tidak berubah), ini membuktikan logika skip benar-benar selektif.
- **Keamanan:** Tidak ada IDOR baru. `EditAssessment POST` terlindungi `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` + `AdminBaseController [Authorize]`. Rate limit 50 user dipertahankan.
- **Pre-Post in-progress skip:** Dua guard identik di `preGroup` / `postGroup` loop (L1852 dan L1863) konsisten dengan guard di sibling loop standar.

---

_Reviewed: 2026-06-17_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
