---
phase: 400-membership-listing-set-aware-rollup-dedup
reviewed: 2026-06-18T00:00:00Z
depth: standard
files_reviewed: 3
files_reviewed_list:
  - Services/WorkerDataService.cs
  - Controllers/WorkerController.cs
  - HcPortal.Tests/WorkerDataServiceSearchTests.cs
findings:
  critical: 0
  warning: 2
  info: 3
  total: 5
status: issues_found
---

# Phase 400: Code Review Report

**Reviewed:** 2026-06-18T00:00:00Z
**Depth:** standard
**Files Reviewed:** 3
**Status:** issues_found

## Summary

Phase 400 mengimplementasikan filter unit SET-AWARE di tiga titik (`GetWorkersInSection`, `ManageWorkers`, `ExportWorkers`): predikat scalar `u.Unit == unitFilter` diganti correlated subquery `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)`, dan kolom `WorkerTrainingStatus.Unit` dibuat kontekstual (filtered → `unitFilter`; unfiltered → semua unit aktif primary-first comma-join dengan fallback `user.Unit`).

Secara keseluruhan implementasi solid: pola correlated subquery EF Core 8 dapat ditranslasikan ke SQL `EXISTS` dengan benar, batch-load `unitsByUser` menghindari N+1, dedup by-construction (1 baris/pekerja), dan test suite MU-06 mencakup edge case krusial (inactive unit, zero-unit fallback, primary-first ordering, filtered vs. unfiltered column). Dua warning ditemukan yang berpotensi menghasilkan behavior tak terduga di edge case tertentu; tiga info untuk peningkatan kejelasan kode.

## Warnings

### WR-01: `unitsByUser` Di-load Setelah Filter `IsActive` — Pekerja Tidak Aktif Tidak Akan Masuk, Tetapi Batch Query Tidak Memfilter `IsActive`

**File:** `Services/WorkerDataService.cs:279`

**Issue:** Query batch-load `unitsByUser` (baris 279) mengambil semua baris `UserUnits` di mana `userIds.Contains(uu.UserId) && uu.IsActive`. `userIds` dibangun dari `users` yang sudah difilter `u.IsActive == true` (baris 247–274). Ini berarti pekerja tidak aktif tidak akan pernah masuk ke `users`, sehingga `userIds`-nya kosong — aman dalam konteks `GetWorkersInSection`.

Namun, jika `userIds` secara hipotetis mengandung ID pekerja tidak aktif (misalnya karena caller melewati `usersQuery` tanpa filter `IsActive`), query `unitsByUser` tetap akan memuat unit mereka. Ini bukan bug sekarang, tetapi coupling implisit antara filter `IsActive` di baris 247 dan batch-load di baris 279 tidak terdokumentasi, sehingga refactoring di masa depan berpotensi merusak invariant ini secara diam-diam.

**Fix:** Tambahkan komentar inline di batch-load `unitsByUser` yang secara eksplisit menyatakan ketergantungan ini:

```csharp
// NOTE: userIds berasal dari users yang sudah difilter IsActive (baris 247).
// Oleh karena itu tidak perlu menambahkan filter .IsActive pada ApplicationUser di sini.
// Jika caller berubah (misal: hilangkan filter IsActive di usersQuery), perhatikan bahwa
// unit milik pekerja tidak aktif bisa ikut ter-load.
var unitsByUser = (await _context.UserUnits
        .Where(uu => userIds.Contains(uu.UserId) && uu.IsActive)
        .ToListAsync())
    ...
```

---

### WR-02: `ExportWorkers` Batch-Load `exportUnitsByUser` Tidak Memfilter `IsActive` — Unit Tidak Aktif Ikut Terbawa di Ekspor

**File:** `Controllers/WorkerController.cs:327`

**Issue:** Di `ExportWorkers`, batch-load unit untuk ekspor Excel (baris 327) tidak menyertakan filter `IsActive`:

```csharp
var exportUnitsByUser = (await _context.UserUnits
        .Where(uu => exportUserIds.Contains(uu.UserId))  // ← tidak ada && uu.IsActive
        .ToListAsync())
    .GroupBy(uu => uu.UserId)
    .ToDictionary(g => g.Key, g => g.ToList());
```

Akibatnya, unit yang sudah dinonaktifkan (misalnya via MU-07 setelah pekerja pindah unit) akan tetap muncul di kolom "Unit" pada file Excel yang diunduh. Ini inkonsisten dengan:
1. `GetWorkersInSection` (baris 279–286) yang batch-load dengan `&& uu.IsActive`.
2. `ManageWorkers` (baris 227–235) yang juga batch-load dengan implicit `UserUnits` untuk `ViewBag.UserUnitsDict` — meskipun di sana juga tidak ada filter `IsActive` (lihat baris 227), kedua tempat ini konsisten tidak filter; namun `GetWorkersInSection` lebih ketat.
3. Predikat filter unit di `ExportWorkers` sendiri (baris 306) yang pakai `&& uu.IsActive`.

**Fix:** Tambahkan filter `IsActive` pada batch-load ekspor agar konsisten dengan filter predikat dan dengan `GetWorkersInSection`:

```csharp
var exportUnitsByUser = (await _context.UserUnits
        .Where(uu => exportUserIds.Contains(uu.UserId) && uu.IsActive)  // active-only
        .ToListAsync())
    .GroupBy(uu => uu.UserId)
    .ToDictionary(g => g.Key, g => g.ToList());
```

Perlu juga dicek apakah ordering `primary-first` sudah diterapkan — saat ini ordering di `ExportWorkers` menggunakan `.OrderByDescending(x => x.Unit == primaryUnit)` yang berfungsi secara semantik, tetapi tidak identik dengan ordering yang dipakai di `GetWorkersInSection` (`.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Unit)`). Tidak fatal, tetapi berpotensi membingungkan jika `primaryUnit` di-resolve dari data yang sudah stale.

## Info

### IN-01: `ManageWorkers` Batch-Load `userUnitsDict` Juga Tidak Memfilter `IsActive`

**File:** `Controllers/WorkerController.cs:227`

**Issue:** Di `ManageWorkers`, batch-load `userUnitsDict` untuk display (baris 227–235) tidak memfilter `uu.IsActive`:

```csharp
var userUnitsDict = (await _context.UserUnits
        .Where(uu => listUserIds.Contains(uu.UserId))  // ← tidak ada && uu.IsActive
        .ToListAsync())
    ...
```

Berbeda dengan `GetWorkersInSection` yang hanya menampilkan unit aktif (baris 279, filter `&& uu.IsActive`). Ini berarti badge "semua unit" di halaman Manage Workers bisa menampilkan unit yang sudah dinonaktifkan, meskipun filter dropdown unit hanya berlaku untuk unit aktif.

**Fix:** Untuk konsistensi dengan `GetWorkersInSection` dan dengan logika filter unit aktif, pertimbangkan menambahkan `&& uu.IsActive` pada batch-load `userUnitsDict`. Jika perilaku saat ini (tampilkan semua unit termasuk tidak aktif) memang disengaja untuk audit trail, dokumentasikan alasannya dengan komentar.

---

### IN-02: Komentar `PITFALL #1` Duplikasi di Tiga Tempat — Peluang DRY Komentar

**File:** `Services/WorkerDataService.cs:258`, `Controllers/WorkerController.cs:207`, `Controllers/WorkerController.cs:304`

**Issue:** Komentar `// PITFALL #1: pakai _context.UserUnits, BUKAN u.UserUnits (nav prop tak ada → CS1061)` diulang verbatim di tiga lokasi. Meskipun komentar informatif, pengulangan ini menunjukkan bahwa tidak ada nav-property `ApplicationUser.UserUnits` yang sudah dikonfigurasi — artinya developer harus tahu cara mengakses `UserUnits` dengan benar setiap kali menambahkan filter unit baru.

**Fix:** Pertimbangkan menambahkan nav-property `public ICollection<UserUnit> UserUnits { get; set; }` pada model `ApplicationUser` dengan konfigurasi EF yang sesuai (foreign key ke `AspNetUsers.Id`). Ini akan mengeliminasi pitfall sepenuhnya dan membuat kode lebih idiomatis. Jika nav-property tidak diinginkan (misalnya alasan performa/EF tracking), cukup satu komentar di header file atau di kelas `ApplicationUser` sudah mencukupi — tidak perlu diulang di tiga tempat.

---

### IN-03: `GenerateRandomPassword` Menghasilkan String Base64 — Mungkin Gagal Validasi Password Identity Jika Karakter Khusus Diwajibkan

**File:** `Controllers/WorkerController.cs:1344`

**Issue:** `GenerateRandomPassword()` menggunakan `Convert.ToBase64String(bytes)` yang menghasilkan karakter `[A-Za-z0-9+/=]`. Komentar menyebutkan "no special chars that break Identity validation", tetapi jika konfigurasi Identity (`PasswordOptions`) di proyek ini mewajibkan setidaknya satu karakter non-alphanumeric (`RequireNonAlphanumeric = true`, yang merupakan default ASP.NET Core Identity), password `+`, `/`, atau `=` dari Base64 memenuhi syarat ini.

Namun jika konfigurasi proyek menyetel `RequireNonAlphanumeric = false` DAN `RequireUppercase = true` DAN `RequireLowercase = true` DAN `RequireDigit = true`, ada kemungkinan sangat kecil (probabilitas rendah tetapi non-zero) bahwa string Base64 12-byte tidak mengandung uppercase/lowercase/digit secara bersamaan. Probabilitas aktual sangat rendah, tetapi ini adalah asumsi implisit yang tidak terverifikasi.

**Fix:** Dokumentasikan asumsi `PasswordOptions` di komentar fungsi, atau gunakan generator password yang lebih eksplisit menjamin kehadiran uppercase, lowercase, dan digit.

---

_Reviewed: 2026-06-18T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
