---
phase: 400-membership-listing-set-aware-rollup-dedup
fixed_at: 2026-06-18T00:00:00Z
review_path: .planning/phases/400-membership-listing-set-aware-rollup-dedup/400-REVIEW.md
iteration: 2
findings_in_scope: 5
fixed: 3
already_fixed: 2
skipped: 0
status: all_fixed
---

# Phase 400: Laporan Perbaikan Code Review

**Diperbaiki pada:** 2026-06-18T00:00:00Z
**Sumber review:** .planning/phases/400-membership-listing-set-aware-rollup-dedup/400-REVIEW.md
**Iterasi:** 2

**Ringkasan:**
- Temuan dalam scope: 5 (WR-01, WR-02, IN-01, IN-02, IN-03 — scope = all)
- Diperbaiki di iterasi ini: 3 (IN-01, IN-02, IN-03)
- Sudah diperbaiki di iterasi 1: 2 (WR-01, WR-02)
- Dilewati: 0

Verifikasi: `dotnet build` lulus dengan **0 error** setelah setiap perubahan (28 warning, semuanya pre-existing & tidak terkait perubahan ini — Tier 2 PASSED untuk tiap fix).

## Isu yang Diperbaiki

### WR-01: `unitsByUser` Di-load Setelah Filter `IsActive` — Coupling Implisit Tak Terdokumentasi

**File yang dimodifikasi:** `Services/WorkerDataService.cs`
**Commit:** `dae42f0a`
**Status:** already_fixed (iterasi 1)
**Perbaikan yang diterapkan:** Komentar inline `NOTE (WR-01)` ditambahkan tepat sebelum batch-load `unitsByUser`, mendokumentasikan bahwa `userIds` berasal dari `users` yang sudah difilter `u.IsActive` sehingga filter `.IsActive` tambahan pada `ApplicationUser` tidak diperlukan, plus peringatan jika caller berubah. Comment-only, tanpa perubahan perilaku. Tidak diulang di iterasi 2.

### WR-02: `ExportWorkers` Batch-Load `exportUnitsByUser` Tidak Memfilter `IsActive`

**File yang dimodifikasi:** `Controllers/WorkerController.cs`
**Commit:** `a0b9468d`
**Status:** already_fixed (iterasi 1, requires human verification)
**Perbaikan yang diterapkan:** Menambahkan `&& uu.IsActive` pada klausa `Where` batch-load `exportUnitsByUser` sehingga unit non-aktif tidak lagi muncul di kolom "Unit" file Excel. Menyamakan perilaku ekspor dengan predikat filter unit `ExportWorkers` dan dengan `GetWorkersInSection`. Mengubah hasil data ekspor — semantiknya (unit non-aktif memang disembunyikan, bukan dipertahankan sebagai audit trail) perlu dikonfirmasi developer via UAT browser. Tidak diulang di iterasi 2.

### IN-01: `ManageWorkers` Batch-Load `userUnitsDict` Juga Tidak Memfilter `IsActive`

**File yang dimodifikasi:** `Controllers/WorkerController.cs`
**Commit:** `764397c6`
**Status:** fixed: requires human verification
**Perbaikan yang diterapkan:** Menambahkan `&& uu.IsActive` pada klausa `Where` batch-load `userUnitsDict` (input ke `ViewBag.UserUnitsDict`), plus komentar `IN-01` yang menjelaskan tujuannya. Ini menjadikan badge "semua unit" di halaman Manage Workers hanya menampilkan unit aktif — konsisten dengan `GetWorkersInSection` (`unitsByUser`) dan dengan predikat filter unit aktif.

**Alasan menerapkan (bukan skip):** Guardrail mengonfirmasi keputusan proyek D-07 mengatur "semua unit vs primary-only", BUKAN "active vs inactive". Memfilter ke active-only TIDAK bertentangan dengan D-07 dan justru menyelaraskan dengan `GetWorkersInSection`. Karena itu fix diterapkan untuk konsistensi.

**Catatan untuk verifikasi manusia:** Ini perubahan perilaku DISPLAY (unit yang dinonaktifkan via MU-07 kini tidak tampil di badge "semua unit" pada halaman Manage Workers). Build lulus, namun konfirmasi UAT browser disarankan untuk memastikan ekspektasi tampilan sesuai (sejalan dengan Develop Workflow: cek di URL Lokal/Dev). Tidak ada audit-trail yang hilang karena data `UserUnits` non-aktif tetap ada di DB; hanya tampilan badge yang berubah.

### IN-02: Komentar `PITFALL #1` Duplikasi di Tiga Tempat — Peluang DRY Komentar

**File yang dimodifikasi:** `Services/WorkerDataService.cs`, `Controllers/WorkerController.cs`
**Commit:** `de6e1dab`
**Status:** fixed
**Perbaikan yang diterapkan:** Konsolidasi komentar menjadi satu **rujukan kanonik** di `WorkerDataService.cs` (`GetWorkersInSection`), yang menjelaskan secara lengkap bahwa relasi `UserUnit→ApplicationUser` dikonfigurasi via `.WithMany()` tanpa argumen sehingga nav-property `ApplicationUser.UserUnits` SENGAJA tidak ada (`u.UserUnits.Any(...)` → CS1061). Dua komentar duplikat verbatim di `WorkerController.cs` (predikat `ManageWorkers` dan `ExportWorkers`) dipersingkat menjadi referensi satu baris ke rujukan kanonik tersebut.

**Catatan opsi yang TIDAK diambil (sesuai guardrail):** Saran reviewer untuk menambahkan nav-property `ApplicationUser.UserUnits` + konfigurasi fluent SENGAJA TIDAK dilakukan karena berisiko mengubah perilaku EF tracking/migration — di luar scope cleanup Info. Perubahan ini murni comment-only, tanpa perubahan perilaku.

### IN-03: `GenerateRandomPassword` Base64 — Asumsi `PasswordOptions` Tak Terverifikasi

**File yang dimodifikasi:** `Controllers/WorkerController.cs`
**Commit:** `37668500`
**Status:** fixed
**Perbaikan yang diterapkan:** Mendokumentasikan asumsi `PasswordOptions` di komentar fungsi `GenerateRandomPassword`. Konfigurasi aktual diverifikasi langsung di `Program.cs` (`AddIdentity`): `RequireDigit=false`, `RequireLowercase=false`, `RequireUppercase=false`, `RequireNonAlphanumeric=false`, `RequiredLength=6`. Karena Base64 dari 12 byte menghasilkan 16 karakter dan tidak ada satu pun syarat komposisi karakter yang aktif, password ini DIJAMIN lolos validasi Identity. Komentar juga memberi peringatan agar generator ditinjau ulang bila `PasswordOptions` kelak diketatkan.

**Catatan (sesuai guardrail):** Generator password TIDAK ditulis ulang — cukup dokumentasi asumsi. Comment-only, tanpa perubahan perilaku. Kekhawatiran probabilistik di REVIEW.md (string Base64 tanpa kelas karakter tertentu) menjadi moot karena konfigurasi proyek tidak mewajibkan kelas karakter apa pun.

## Isu yang Dilewati

Tidak ada — kelima temuan dalam scope (2 warning sudah diperbaiki di iterasi 1, 3 info diperbaiki di iterasi ini) berhasil ditangani.

## Catatan Line-Number

Nomor baris di REVIEW.md sedikit bergeser dari kondisi aktual file (mis. IN-01 menyebut baris 227, klausa `Where` aktual ada di sekitar baris 228; IN-03 menyebut 1344). Konteks kode tetap cocok sehingga fix diterapkan berdasarkan isi kode, bukan nomor baris literal.

---

_Diperbaiki: 2026-06-18T00:00:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iterasi: 2_
