---
phase: 423-certificate-issuance-consistency
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - Helpers/CertIssuanceRules.cs
  - Helpers/CertNumberHelper.cs
  - Services/GradingService.cs
  - Controllers/AssessmentAdminController.cs
  - Controllers/TrainingAdminController.cs
  - Views/Admin/EssayGrading.cshtml
  - Views/Admin/AssessmentMonitoringDetail.cshtml
  - HcPortal.Tests/CertIssuanceRulesTests.cs
  - HcPortal.Tests/CertIssuanceIntegrationTests.cs
findings:
  critical: 1
  warning: 2
  info: 1
  total: 4
status: issues_found
---

# Phase 423: Code Review Report

**Reviewed:** 2026-06-24
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Ringkasan

Review fase 423 (Certificate Issuance Consistency) mencakup helper murni `CertIssuanceRules`, hardening seq `TryAssignNextSeqAsync`, penyambungan gate di 4 site grading, guard anti-dup, penolakan namespace manual, dan badge umur PendingGrading.

Secara keseluruhan arsitektur sudah solid: gate tunggal `ShouldIssueCertificate` benar secara logika, retry+jitter di `TryAssignNextSeqAsync` sudah lebih baik dari sebelumnya, `DeriveValidUntil` sesuai D-04/D-05/D-10, dan validasi Razor sudah auto-escape (tidak ada `Html.Raw` pada data pengguna). RBAC `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` terjaga di semua endpoint POST yang dimodifikasi. `migration=FALSE` dihormati.

Namun ditemukan **satu bug kritis** (sinkronisasi in-memory terlewat di SITE 3) dan **dua warning** (logika `updated==0` di retry loop, dan error message memuat `UserId` raw yang mungkin bukan NIP).

---

## Critical Issues

### CR-01: SITE 3 FinalizeEssayGrading — session.IsPassed tidak di-sync setelah ExecuteUpdateAsync, cert tidak pernah terbit

**File:** `Controllers/AssessmentAdminController.cs:3869-3910`

**Issue:** Di SITE 1 (`GradingService.GradeAndCompleteAsync`) dan SITE 2 (`RegradeAfterEditAsync`), fase ini menambahkan sinkronisasi in-memory `session.IsPassed = isPassed` setelah `ExecuteUpdateAsync` (komentar "Rule 1 - Bug fix"). SITE 3 (`FinalizeEssayGrading`) melakukan `ExecuteUpdateAsync` yang sama di baris 3869-3875 tetapi **tidak memiliki sinkronisasi in-memory yang setara**. Hasilnya, `session.IsPassed` masih `null` (nilai PendingGrading) ketika `CertIssuanceRules.ShouldIssueCertificate(session)` dipanggil di baris 3910. Gate membaca `session.IsPassed == true` — karena nilainya `null`, gate selalu mengembalikan `false` → cert tidak pernah diterbitkan dari jalur FinalizeEssayGrading, walau peserta lulus.

`session.CompletedAt` juga tidak di-sync, tetapi ini kurang kritis karena nilai lama dari PendingGrading masih mewakili tanggal peserta selesai ujian (sesuai D-05). Namun `session.IsPassed` adalah blocker nyata.

**Fix:**
```csharp
// Tambahkan SETELAH if (rowsAffected == 0) { ... } di FinalizeEssayGrading (setelah baris ~3900)
// Sinkron in-memory ke nilai yang baru ditulis ke DB (paritas GradingService SITE 1/2).
session.IsPassed = isPassed;
session.Status   = AssessmentConstants.AssessmentStatus.Completed;
// session.CompletedAt sudah ada dari PendingGrading path (GradingService line 224), tidak perlu di-update.
```

---

## Warnings

### WR-01: TryAssignNextSeqAsync — logika updated==0 anggap sukses tanpa cek apakah NomorSertifikat memang sudah terisi untuk session ini

**File:** `Helpers/CertNumberHelper.cs:63-68`

**Issue:**
```csharp
var updated = await context.AssessmentSessions
    .Where(s => s.Id == sessionId && s.NomorSertifikat == null)
    .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, nomor));
if (updated > 0) return true;
// updated == 0 -> sudah terisi oleh proses lain (idempotent). Anggap sukses.
return true;
```

`updated == 0` bisa terjadi karena dua alasan berbeda: (a) NomorSertifikat sesi ini sudah terisi (idempotent, sukses), atau (b) sesi dengan `sessionId` tidak ditemukan sama sekali (bug data, juga return true). Komentar "anggap sukses" hanya valid untuk kasus (a). Kasus (b) menyebabkan `return true` palsu — caller menganggap cert tersimpan padahal sesi tidak ada atau sudah dihapus. Ini terutama relevan saat ada lag antara validasi dan penyimpanan.

Risiko praktis rendah (sesi pasti ada jika dipanggil dari grading flow yang baru selesai), tapi logika ambigu.

**Fix:**
```csharp
if (updated > 0) return true;
// updated == 0 -> WHERE NomorSertifikat == null tidak cocok.
// Kemungkinan: (a) sesi ini sudah ber-NomorSertifikat (idempotent sukses),
//              (b) sessionId tidak valid (return false agar caller menangani).
var alreadyHas = await context.AssessmentSessions
    .AnyAsync(s => s.Id == sessionId && s.NomorSertifikat != null);
return alreadyHas; // true = idempotent OK; false = data anomali, caller tandai HC
```

### WR-02: Error message CERT-04 di TrainingAdminController menampilkan UserId mentah (bukan NIP/nama)

**File:** `Controllers/TrainingAdminController.cs:721`

**Issue:**
```csharp
ModelState.AddModelError("",
    $"Nomor sertifikat manual untuk pekerja {wc.UserId} tidak boleh menyerupai format otomatis.");
```

`wc.UserId` adalah GUID string identitas internal ASP.NET Identity, bukan NIP atau nama yang bermakna bagi pengguna HC. Pesan error ditampilkan di view — HC akan melihat GUID acak yang tidak informatif. Pola yang sama juga ada di baris 706 dan 738 (pre-existing), tapi baris 721 adalah tambahan baru fase ini.

**Fix:**
```csharp
// Opsi 1: tampilkan nomor urut pekerja dalam batch
ModelState.AddModelError("",
    $"Nomor sertifikat manual (pekerja ke-{idx + 1}) tidak boleh menyerupai format otomatis (KPB/NNN/ROMAN/TAHUN).");

// Opsi 2 (konsisten dengan pola existing di baris 706/738): gunakan wc.UserId tapi tambah catatan
// bahwa pola ini sudah ada pre-existing dan perlu di-fix secara terpisah.
```

---

## Info

### IN-01: GetNextSeqAsync masih berpotensi scan semua nomor cert tahun berjalan ke memori (pre-existing, dipakai oleh TryAssignNextSeqAsync baru)

**File:** `Helpers/CertNumberHelper.cs:23-35`

**Issue:** `GetNextSeqAsync` memuat semua `NomorSertifikat` yang berakhir dengan `/{year}` ke memori untuk mencari MAX seq via LINQ client-side. Ini pre-existing dan di luar scope fase, tetapi `TryAssignNextSeqAsync` (baru di fase ini) memanggil metode ini di setiap iterasi retry. Jika ada burst simultan dengan banyak cert tahun ini, setiap retry akan memuat ulang daftar lengkap. Tidak ada dampak correctness (non-destruktif), hanya informasi untuk backlog.

**Fix (backlog):** Ubah menjadi query MAX server-side:
```csharp
var max = await context.AssessmentSessions
    .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))
    .MaxAsync(s => (int?)s.NomorSertifikat!.Split('/')[1].ParseIntOrDefault()) ?? 0;
return max + 1;
```
Atau gunakan SQL `MAX(CAST(...))`. Tidak urgent selama volume cert per tahun < ribuan.

---

## Catatan Tambahan

**CERT-05 anti-dup guard:** Guard di `AssessmentAdminController.cs:1034` sudah benar diposisikan di luar cabang `ConfirmDuplicateTitle` (unconditional). Logika `isRenewalModePost` mencakup `RenewsSessionId`, `RenewsTrainingId`, dan `RenewalFkMap` — renewal exemption komplit.

**Razor badge (CERT-07):** Semua output di Razor auto-escaped (`@pCls`, `@pDays`, dll). Tidak ada `Html.Raw` pada data pengguna. Tanggal di-format via `.ToString(...)` — aman dari XSS.

**DeriveValidUntil non-kanonik:** Nilai `null` dikembalikan untuk CertificateType non-kanonik (D-10), dan caller hanya menulis ke DB jika `validUntil != null`. Ini berarti untuk type non-kanonik, `ValidUntil` yang sudah diisi HC di form manual akan tetap digunakan (tidak di-overwrite). Perilaku sudah benar sesuai D-10.

**migration=FALSE:** Tidak ada perubahan schema, migration file, atau `DbSet` baru. Dikonfirmasi.

---

_Reviewed: 2026-06-24_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
