---
phase: 400-membership-listing-set-aware-rollup-dedup
fixed_at: 2026-06-18T00:00:00Z
review_path: .planning/phases/400-membership-listing-set-aware-rollup-dedup/400-REVIEW.md
iteration: 1
findings_in_scope: 2
fixed: 2
skipped: 0
status: all_fixed
---

# Phase 400: Laporan Perbaikan Code Review

**Diperbaiki pada:** 2026-06-18T00:00:00Z
**Sumber review:** .planning/phases/400-membership-listing-set-aware-rollup-dedup/400-REVIEW.md
**Iterasi:** 1

**Ringkasan:**
- Temuan dalam scope: 2 (WR-01, WR-02 — kritis + warning saja; info IN-01/IN-02/IN-03 dikecualikan)
- Diperbaiki: 2
- Dilewati: 0

Verifikasi: `dotnet build` lulus dengan 0 error setelah kedua perubahan (Tier 2 PASSED untuk kedua file).

## Isu yang Diperbaiki

### WR-01: `unitsByUser` Di-load Setelah Filter `IsActive` — Coupling Implisit Tak Terdokumentasi

**File yang dimodifikasi:** `Services/WorkerDataService.cs`
**Commit:** `dae42f0a`
**Status:** fixed
**Perbaikan yang diterapkan:** Menambahkan komentar inline (`NOTE (WR-01)`) tepat sebelum batch-load `unitsByUser` yang secara eksplisit mendokumentasikan bahwa `userIds` berasal dari `users` yang sudah difilter `u.IsActive` (baris 247). Komentar ini menjelaskan bahwa tidak perlu menambahkan filter `.IsActive` pada `ApplicationUser` di titik ini, dan memperingatkan bahwa jika caller berubah (menghilangkan filter `IsActive` di `usersQuery`), unit milik pekerja tidak aktif bisa ikut ter-load. Perubahan ini comment-only — tidak ada perubahan perilaku.

### WR-02: `ExportWorkers` Batch-Load `exportUnitsByUser` Tidak Memfilter `IsActive`

**File yang dimodifikasi:** `Controllers/WorkerController.cs`
**Commit:** `a0b9468d`
**Status:** fixed: requires human verification
**Perbaikan yang diterapkan:** Menambahkan `&& uu.IsActive` pada klausa `Where` di batch-load `exportUnitsByUser`, sehingga unit yang sudah dinonaktifkan (mis. via MU-07 setelah pekerja pindah unit) tidak lagi muncul di kolom "Unit" pada file Excel yang diunduh. Perubahan ini menyamakan perilaku ekspor dengan predikat filter unit di `ExportWorkers` (yang sudah pakai `&& uu.IsActive`) dan dengan `GetWorkersInSection`.

**Catatan untuk verifikasi manusia:** Perubahan ini mengubah hasil data ekspor (logic/data-affecting, bukan sekadar syntax). Build lulus, namun semantiknya — bahwa unit non-aktif memang harus disembunyikan dari ekspor Excel dan bukan dipertahankan sebagai audit trail — perlu dikonfirmasi oleh developer melalui UAT browser sesuai Develop Workflow proyek (cek di URL Dev/Lokal). Saran reviewer terkait ordering `primary-first` (`.OrderByDescending(x => x.Unit == primaryUnit)` di ExportWorkers vs `.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Unit)` di GetWorkersInSection) dinilai non-fatal dan TIDAK diubah dalam iterasi ini agar scope fix tetap sempit; jika konsistensi ordering diinginkan, tangani sebagai item terpisah.

## Isu yang Dilewati

Tidak ada — kedua temuan dalam scope berhasil diperbaiki.

## Catatan Line-Number

Nomor baris yang dirujuk di REVIEW.md sedikit bergeser dari kondisi aktual file (WR-02 menyebut baris 327, sedangkan klausa `Where` aktual ada di baris 323). Konteks kode tetap cocok sehingga fix diterapkan dengan benar berdasarkan isi kode, bukan nomor baris literal.

## Catatan Scope

Tiga temuan Info (IN-01, IN-02, IN-03) sengaja dikecualikan karena `fix_scope = critical_warning`. Ringkasannya bila ingin ditindaklanjuti terpisah:
- IN-01: `ManageWorkers` batch-load `userUnitsDict` (WorkerController.cs:227) juga tidak memfilter `IsActive` — konsistensi display.
- IN-02: Komentar `PITFALL #1` diduplikasi di tiga tempat — peluang DRY / nav-property.
- IN-03: `GenerateRandomPassword` Base64 — asumsi `PasswordOptions` tak terverifikasi.

---

_Diperbaiki: 2026-06-18T00:00:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iterasi: 1_
