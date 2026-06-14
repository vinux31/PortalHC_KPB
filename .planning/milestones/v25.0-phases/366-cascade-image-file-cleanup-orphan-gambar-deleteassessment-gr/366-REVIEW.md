---
phase: 366-cascade-image-file-cleanup-orphan-gambar-deleteassessment-gr
reviewed: 2026-06-12T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Helpers/ImageFileCleanup.cs
  - Controllers/AssessmentAdminController.cs
  - HcPortal.Tests/ImageCleanupIntegrationTests.cs
  - HcPortal.Tests/PackageImageDeleteTests.cs
findings:
  critical: 0
  warning: 1
  info: 2
  total: 3
status: issues_found
---

# Phase 366: Code Review Report

**Reviewed:** 2026-06-12T00:00:00Z
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Phase 366 mengekstrak helper statis `ImageFileCleanup.DeleteUnreferencedAsync` dari 3 inline loop yang byte-identik di controller, lalu menginstalnya di 3 cascade-delete method (`DeleteAssessment`, `DeleteAssessmentGroup`, `DeletePrePostGroup`) menggunakan pola Phase 333 (collect-before-RemoveRange, call-after-CommitAsync).

**Penilaian keseluruhan:** Refactor ini benar secara arsitektur. Semua titik kritis terpenuhi: (1) collect ImagePath dilakukan _sebelum_ RemoveRange di ketiga call-site; (2) helper dipanggil _setelah_ `tx.CommitAsync`; (3) penggunaan `logger` (lokal) vs `_logger` (field) konsisten dengan pre-existing pattern di masing-masing method cascade; (4) 2 swapped call-site di `DeletePackage`/`DeleteQuestion`/method-edit tetap menggunakan `_logger` (field) yang benar. Satu warning ditemukan pada integration test fixture, dua info item bersifat minor.

## Warnings

### WR-01: `ImageCleanupFixture` menggunakan `IClassFixture` — DB disposable di-share antar test dalam kelas yang sama

**File:** `HcPortal.Tests/ImageCleanupIntegrationTests.cs:70`

**Issue:** `ImageCleanupIntegrationTests` di-dekorasi `IClassFixture<ImageCleanupFixture>`, artinya seluruh instance kelas berbagi satu DB disposable yang sama. Kedua `[Fact]` berjalan berurutan dalam proses xUnit yang sama, dan keduanya memanggil `SeedUserAsync` lalu melakukan `ctx.AssessmentSessions.Remove(session)` di akhir. Selama test berjalan, sisa seed dari Fact-1 (user row di `AspNetUsers`) masih ada di DB ketika Fact-2 mulai. Ini tidak menyebabkan kegagalan sekarang karena kedua test memakai GUID unik untuk UserName — namun jika test ke depannya membutuhkan state DB bersih (misalnya count-assertion), shared fixture akan menjadi sumber non-determinisme.

Lebih spesifik: `DisposeAsync` hanya dipanggil sekali setelah _seluruh_ kelas selesai, bukan antar `[Fact]`. Jika Fact-1 gagal tengah jalan (assertion throw sebelum `ctx.SaveChanges` final), sisa entity yang sudah ter-commit bisa mempengaruhi Fact-2 yang mengandalkan `AnyAsync` post-commit sebagai assertion utama.

**Fix:** Paling minimal, tambahkan komentar eksplisit bahwa kedua Fact saling independen via GUID-keying dan tidak ada count-assertion lintas-test. Jika ingin aman penuh, gunakan `IAsyncLifetime` per-test (bukan `IClassFixture`) sehingga DB di-drop dan di-recreate antara setiap `[Fact]` — meski ini jauh lebih lambat (dua kali MigrateAsync per run). Alternatif pragmatis: `ICollectionFixture` dengan explicit `[Collection]` agar xUnit tidak menjalankan Fact-1 dan Fact-2 secara paralel.

```csharp
// Opsi minimal — tambahkan komentar di kelas:
// WARNING: kedua [Fact] share DB yang sama (IClassFixture). Test saling-independen
// HANYA karena pakai GUID unik per UserName. Jangan tambah assertion berbasis DB-count
// lintas-test tanpa mengubah ke IAsyncLifetime per-test.
```

## Info

### IN-01: `Path.Combine(webRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))` — path traversal dari DB-only input, tapi tidak ada guard eksplisit post-combine

**File:** `Helpers/ImageFileCleanup.cs:32`

**Issue:** Helper menggunakan `Path.Combine` + `TrimStart('/')` untuk mengonstruksi path fisik dari `relUrl` yang berasal dari kolom DB `ImagePath`. Komentar XML-doc menyatakan "path hanya dari kolom DB ImagePath (upload flow tervalidasi)" — ini benar untuk call-site production. Namun helper tidak melakukan validasi bahwa path hasil combine berada di bawah `webRootPath` (misal via `Path.GetFullPath` + `StartsWith` check). Ini bukan kerentanan aktif karena input dikontrol upload flow, tapi jika di masa depan helper dipanggil dari konteks lain dengan input tidak tepercaya, risikonya menjadi nyata.

**Fix:** Opsional untuk sekarang. Jika ingin defense-in-depth, tambahkan guard sebelum `File.Delete`:

```csharp
var fullWebRoot = Path.GetFullPath(webRootPath);
var physical = Path.GetFullPath(Path.Combine(webRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
if (!physical.StartsWith(fullWebRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
{
    logger.LogWarning("File.Delete ditolak — path keluar dari webRoot ({Source}): {Path}", source, relUrl);
    continue;
}
```

### IN-02: Komentar `logger lokal (:2565)` di `DeletePrePostGroup` merujuk nomor baris yang bisa berubah

**File:** `Controllers/AssessmentAdminController.cs` (baris ~2730 post-Phase 366)

**Issue:** Komentar inline `// logger lokal (:2565)` mencantumkan nomor baris absolut. Nomor ini akan geser setiap kali ada penambahan kode di atas. Komentar serupa di `DeleteAssessment` (`:2346`) dan `DeleteAssessmentGroup` (`:2542`) menggunakan frasa `"logger lokal method ini"` tanpa nomor baris — lebih robust.

**Fix:** Ganti `:2565` dengan penjelasan konseptual:

```csharp
// Phase 366 / SC#3: logger adalah parameter lokal method DeletePrePostGroup
// (bukan _logger field controller) — konsisten dengan method Delete* lain di atas.
await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, logger, imagePaths, "DeletePrePostGroup image");
```

---

_Reviewed: 2026-06-12T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
