---
phase: 345-assessment-pending-grade-display-fix
reviewed: 2026-06-04T00:00:00Z
depth: standard
files_reviewed: 11
files_reviewed_list:
  - Services/WorkerDataService.cs
  - Views/CMP/RecordsWorkerDetail.cshtml
  - Views/CMP/Records.cshtml
  - Controllers/CMPController.cs
  - Models/CDPDashboardViewModel.cs
  - Models/ReportsDashboardViewModel.cs
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/UserAssessmentHistory.cshtml
  - HcPortal.Tests/AssessmentHistoryStatsTests.cs
  - tests/e2e/assessment-pending-grade.spec.ts
  - tests/sql/pending345-seed.sql
findings:
  critical: 0
  warning: 3
  info: 2
  total: 5
status: issues_found
---

# Phase 345: Code Review Report

**Reviewed:** 2026-06-04
**Depth:** standard
**Files Reviewed:** 11
**Status:** issues_found

## Summary

Phase 345 memperbaiki tampilan label "Menunggu Penilaian" di 3 surface (RecordsWorkerDetail, UserAssessmentHistory, BulkExportPdf) + unifikasi label via GetUnifiedRecords + perbaikan stats passRate/averageScore exclude-pending. Perubahan logika inti benar dan unit-tested dengan cakupan baik (7 kasus di AssessmentHistoryStatsTests). Bool→bool? ripple pada AssessmentReportItem sudah tepat dan konsisten dengan semua consumer yang teridentifikasi.

Tiga temuan Warning ditemukan — semuanya bersifat correctness (bukan estetika): satu mismatch antara skenario seed dan kondisi produksi nyata yang membuat UAT tidak merepresentasikan jalur sebenarnya, satu potensi DB tidak ter-restore bila beforeAll gagal sebelum assignment `snapshotPath`, dan satu kondisi edge di view AverageScore. Tidak ada critical (zero crash path, zero security vuln baru). Tidak ada `a.IsPassed` consumer lain yang break akibat bool→bool? yang terlewat di-guard.

---

## Warnings

### WR-01: Seed SQL menyetel `Status='Completed'` padahal sesi pending-grade produksi ber-status `'Menunggu Penilaian'`

**File:** `tests/sql/pending345-seed.sql:38`
**Issue:** Seed sengaja menyetel `Status = 'Completed', IsPassed = NULL` supaya lolos filter `WHERE a.Status == "Completed"` di `WorkerDataService.GetUnifiedRecords` (L33) dan `AssessmentAdminController.UserAssessmentHistory` (L4731). Namun GradingService produksi nyata selalu menulis `Status = AssessmentConstants.AssessmentStatus.PendingGrading` (= `"Menunggu Penilaian"`) dan **tidak pernah** menulis `Status = "Completed"` untuk sesi yang masih pending-grading (lihat `GradingService.cs:199`). Artinya:

1. Kombinasi `Status='Completed', IsPassed=NULL` **tidak bisa terjadi secara organik** via GradingService — ini state yang hanya ada di seed.
2. UAT 3 surface membuktikan badge amber muncul untuk state yang tidak representatif.
3. Sesi `Status='Menunggu Penilaian', IsPassed=NULL` (kondisi produksi nyata) tetap tidak akan muncul di kedua surface tersebut karena filter `Status == "Completed"` — bug utama (REC-07) belum diuji.

CONTEXT.md D-03 memang menyebutkan bahwa sesi `Status="Menunggu Penilaian"` adalah cakupan Phase 346, dan seed ini dirancang untuk menguji kode Phase 345 saja. Tapi seed tidak mencatat keterbatasan ini secara eksplisit dalam komentar SQL, sehingga pembaca dapat menyimpulkan badge akan muncul untuk skenario produksi nyata saat ini — padahal belum.

**Fix:** Tambahkan komentar eksplisit di seed dan/atau di header spec Playwright yang menjelaskan keterbatasan ini:

```sql
-- CATATAN: Status='Completed' + IsPassed=NULL adalah state buatan untuk UAT Phase 345.
-- Di produksi nyata, sesi essay pending-grading ber-Status='Menunggu Penilaian' (bukan 'Completed').
-- Filter GetUnifiedRecords + UserAssessmentHistory masih hanya memuat Status='Completed'.
-- Skenario produksi nyata (Status='Menunggu Penilaian') ditangani Phase 346 REC-07.
-- Tanpa Phase 346, badge "Menunggu Penilaian" TIDAK akan muncul untuk sesi essay yang
-- benar-benar belum dinilai di RecordsWorkerDetail dan UserAssessmentHistory.
```

Ini warning (bukan critical) karena keputusan scope Phase 346 sudah terdokumentasi di CONTEXT.md D-03, namun perlu penanda eksplisit di artefak test agar tim IT/QA tidak salah mengira bug sudah sepenuhnya selesai.

---

### WR-02: `afterAll` restore berada di dalam `try`, bukan `finally` — DB tidak ter-restore bila `db.restore()` sendiri throw

**File:** `tests/e2e/assessment-pending-grade.spec.ts:70-81`
**Issue:** Struktur `afterAll` saat ini:

```typescript
test.afterAll(async () => {
  try {
    if (snapshotPath) await db.restore(snapshotPath);     // (A)
    if (snapshotPath) { /* hapus .bak */ }
  } finally {
    if (snapshotPath) {
      const remaining = await db.queryScalar(...);         // (B)
      expect(remaining, ...).toBe(0);
    }
  }
});
```

Jika `db.restore()` di baris (A) throw (misalnya file `.bak` hilang, SQL Server sedang SINGLE_USER dari run sebelumnya yang gagal, atau disk penuh), blok `finally` di (B) tetap dieksekusi — tapi DB **tidak** pernah di-restore. Baris (B) kemudian mengeksekusi query pada DB kotor (masih ada seed). Karena DB kotor, `remaining > 0`, dan `expect(...).toBe(0)` gagal — yang mengacaukan laporan test ("Layer 4 cleanup") padahal masalah sebenarnya adalah restore yang gagal. Pesan error asli dari `db.restore()` tertimpa oleh assertion failure Layer 4.

Selain itu, `snapshotPath` dideklarasikan sebagai `string` (non-nullable TypeScript) di baris 20, tetapi nilainya baru di-assign di dalam `beforeAll` (baris 53). Jika `beforeAll` gagal sebelum assignment (misalnya `db.queryString(...)` di baris 49 throw), `snapshotPath` tetap `undefined` saat runtime meskipun TypeScript menyatakan `string`. Guard `if (snapshotPath)` di `afterAll` akan terpenuhi karena TypeScript menganggap selalu truthy — namun ini tidak akan crash karena JavaScript mengevaluasi `undefined` sebagai falsy di runtime.

**Fix:** Pisahkan restore ke blok `finally` yang sesungguhnya dan tangani error restore secara independen:

```typescript
test.afterAll(async () => {
  let restoreError: unknown = null;
  try {
    if (snapshotPath) await db.restore(snapshotPath);
    if (snapshotPath) {
      const fs = await import('node:fs');
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    }
  } catch (e) {
    restoreError = e;
    console.error('[afterAll] db.restore gagal:', e);
  } finally {
    if (snapshotPath) {
      const remaining = await db.queryScalar(
        "SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%'"
      );
      expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
    }
    if (restoreError) throw restoreError; // re-throw agar test runner melaporkan restore failure
  }
});
```

---

### WR-03: `AverageScore` dirender tanpa guard di Average Score card — menampilkan "0.0" yang menyesatkan saat semua sesi pending

**File:** `Views/Admin/UserAssessmentHistory.cshtml:119`
**Issue:** Pass Rate card sudah mendapat guard `GradedCount > 0 ? ... : "Belum ada penilaian"` (baris 68 dan 101). Namun kartu Average Score di baris 119 tidak mendapat perlakuan yang sama:

```razor
<h2 class="fw-bold mb-0">@Model.AverageScore.ToString("F1")</h2>
```

Ketika semua sesi adalah pending (`GradedCount == 0`), `ComputeHistoryStats` mengembalikan `averageScore = 0` (guard `gradedItems.Count > 0` di L4783). View akan menampilkan `"0.0"` — yang menyesatkan karena terlihat seperti "nilai rata-rata adalah nol" padahal belum ada nilai sama sekali. Inkonsistensi visual ini signifikan karena Pass Rate di kartu sebelah sudah menampilkan "Belum ada penilaian".

Ini tidak menyebabkan crash (tidak ada division by zero di view), tapi memberikan informasi yang salah kepada HC saat membaca halaman UserAssessmentHistory untuk peserta yang belum dinilai.

**Fix:**

```razor
<h2 class="fw-bold mb-0">
    @(Model.GradedCount > 0 ? Model.AverageScore.ToString("F1") : "Belum ada penilaian")
</h2>
```

---

## Info

### IN-01: `graded + pending` dihitung dua kali dengan scan terpisah di `ComputeHistoryStats` — DRY minor

**File:** `Controllers/AssessmentAdminController.cs:4778-4782`
**Issue:** `graded` dan `pending` dihitung dengan dua `Count()` terpisah yang masing-masing iterasi seluruh list, lalu `gradedItems` dibuat dengan `Where().ToList()` yang merupakan iterasi ketiga:

```csharp
var graded = items.Count(a => a.IsPassed != null);   // iterasi 1
var pending = items.Count(a => a.IsPassed == null);  // iterasi 2
// ...
var gradedItems = items.Where(a => a.IsPassed != null).ToList(); // iterasi 3
```

`pending` selalu sama dengan `total - graded`, sehingga iterasi kedua redundan.

**Fix:** (Tidak wajib — helper ini dipanggil sekali per request, list umumnya kecil per user. Sebutkan untuk kelengkapan.)

```csharp
var gradedItems = items.Where(a => a.IsPassed != null).ToList();
var graded = gradedItems.Count;
var pending = total - graded;
```

---

### IN-02: Komentar di baris `Score = a.Score ?? 0` (AssessmentAdminController L4742) tidak mencatat bahwa Score dapat non-null untuk sesi pending-grading produksi

**File:** `Controllers/AssessmentAdminController.cs:4742`
**Issue:** Baris `Score = a.Score ?? 0` tidak memiliki komentar, sehingga tidak jelas apakah `Score = NULL` untuk sesi pending adalah kondisi yang diharapkan atau artefak seed. Di produksi nyata, GradingService menyetel `Score = interimPercentage` (interim MC-only score) untuk sesi PendingGrading — sehingga `a.Score` tidak akan `NULL` untuk sesi pending organik. Dalam scope Phase 345, ini bukan bug karena `UserAssessmentHistory` hanya memuat `Status == "Completed"` (bukan `"Menunggu Penilaian"`), dan sesi `Completed` normalnya memiliki score. Namun tanpa komentar, ini bisa membingungkan developer berikutnya saat Phase 346 memperluas filter status.

**Fix:** Tambahkan komentar inline ringan:

```csharp
Score = a.Score ?? 0, // ?? 0: defensif untuk ManualEntry atau edge case; sesi PendingGrading organik punya interimScore != null
```

---

_Reviewed: 2026-06-04_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
