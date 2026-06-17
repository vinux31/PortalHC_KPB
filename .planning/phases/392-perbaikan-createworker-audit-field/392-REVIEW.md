---
phase: 392-perbaikan-createworker-audit-field
reviewed: 2026-06-17T00:00:00+07:00
depth: standard
files_reviewed: 2
files_reviewed_list:
  - Views/Admin/CreateWorker.cshtml
  - tests/e2e/createworker-392.spec.ts
findings:
  critical: 0
  warning: 1
  info: 1
  total: 2
status: issues_found
---

# Phase 392: Code Review Report

**Reviewed:** 2026-06-17
**Depth:** standard
**Files Reviewed:** 2 (Views/Admin/CreateWorker.cshtml, tests/e2e/createworker-392.spec.ts)
**Status:** issues_found (1 Warning, 1 Info — tidak ada Critical)

## Summary

Phase ini adalah **view-only** (CreateWorker.cshtml) plus e2e baru (createworker-392.spec.ts). Tidak ada migration, tidak ada perubahan controller/model/EditWorker — scope lock D-08 terpenuhi dan dikonfirmasi via `git diff --name-only`.

**View correctness:** Penghapusan `readonly`/`bg-light` pada FullName/Email aman. Keduanya adalah display attribute, bukan security control — server-side `[Required]`, `[EmailAddress]`, dan antiforgery token di WorkerController tetap sebagai garis pertahanan utama. Teks info-text yang lebih deskriptif untuk AD mode tidak menambah risiko.

**`type="email"` pada Email input:** Aman. Model `ManageUserViewModel.Email` tidak memiliki `[DataType(DataType.EmailAddress)]` (hanya `[EmailAddress]` untuk validasi), sehingga ASP.NET Core tag helper `asp-for` pada .NET 8 tidak akan meng-emit `type=email` secara otomatis dari attribute validation — tidak ada duplikat `type` yang terjadi. Rendered HTML akan punya satu `type="email"`.

**Span validasi 4 field org baru (Position/Directorate/Section/Unit/Role):** Ditempatkan secara benar di bawah masing-masing input/select, tidak tumpang-tindih dengan 6 span yang sudah ada sebelumnya (FullName, Email, NIP, JoinDate, Password, ConfirmPassword). Total sekarang 11 span — semua terpetakan ke property model yang valid. `Position`, `Directorate`, `Section`, `Unit` adalah nullable (`string?`) di model sehingga tidak ada `[Required]` client-side yang dipicu, namun span tetap diperlukan untuk menampilkan error jika controller menambahkan ModelState error secara eksplisit.

**`@section Scripts` + `_ValidationScriptsPartial` ordering:** Benar. `_Layout.cshtml` line 241 load jQuery dari CDN di dalam `<body>` sebelum `@RenderSectionAsync("Scripts")` di line 267. `_ValidationScriptsPartial` (yang load `jquery.validate` + `jquery.validate.unobtrusive`) di-render pertama di dalam `@section Scripts`, diikuti `shared-cascade.js`, `shared-loading.js`, lalu inline `<script>` inisialisasi. Urutan jQuery → validate → unobtrusive → cascade → loading → init sudah benar.

**Razor interpolation `currentSection`/`currentUnit`:** Terpelihara. Ekspresi `"@(Model.Section ?? "")"` dan `"@(Model.Unit ?? "")"` tidak berubah dari versi sebelumnya; hanya berpindah dari bare `<script>` ke dalam `@section Scripts {}`. Perilaku identik.

**Teardown e2e (DEF-392-01):** Reload halaman antara validasi-rejection assertion dan submit-sukses assertion adalah pemisahan yang sah, bukan masking bug. Bug `initFormLoading` (shared infra) sudah dicatat di `deferred-items.md` dengan severity LOW dan rencana fix di phase tersendiri. Teardown menggunakan `POST /Admin/DeleteWorker` (Identity cascade roles) bukan raw-SQL DELETE — benar.

**Satu Warning ditemukan:** SQL literal interpolation di test teardown (bukan SQL injection risk di produksi, tetapi engineering concern untuk test helper). Satu Info terkait `Section` dan `Unit` yang tidak punya `[Required]` di model namun diberi span validasi.

---

## Warnings

### WR-01: SQL literal interpolation di `queryString` teardown — email value tidak di-escape

**File:** `tests/e2e/createworker-392.spec.ts:104` dan `tests/e2e/createworker-392.spec.ts:113`

**Issue:** `EMAIL` di-interpolasi langsung ke string SQL:
```ts
workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${EMAIL}'`);
```
`EMAIL` dibangun dari `Date.now()` (murni numerik), sehingga dalam praktik tidak ada karakter berbahaya. Namun pola ini melanggar prinsip parameterisasi: jika pola `queryString` dengan template literal dipakai kembali oleh engineer lain dengan email dari input pengguna atau fixture eksternal, hasilnya adalah SQL injection terhadap DB lokal test environment.

**Fix:** Tambahkan helper `queryStringParam` di `tests/helpers/dbSnapshot.ts` yang menerima parameter terpisah, atau setidaknya sanitasi/escape single-quote pada nilai string sebelum interpolasi:
```ts
// Opsi minimal — escape single-quote untuk SQL Server
const safeMail = EMAIL.replace(/'/g, "''");
workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${safeMail}'`);
```
Atau lebih baik, gunakan parameterisasi via `sqlcmd -v` jika `dbSnapshot` mendukungnya.

---

## Info

### IN-01: `asp-validation-for` pada `Section` dan `Unit` tidak akan pernah menampilkan client-side error (field nullable, tanpa `[Required]`)

**File:** `Views/Admin/CreateWorker.cshtml:118` dan `Views/Admin/CreateWorker.cshtml:125`

**Issue:** `ManageUserViewModel.Section` dan `Unit` adalah `string?` tanpa `[Required]` — jQuery unobtrusive validation tidak akan menandai keduanya sebagai wajib isi di sisi klien. Span `asp-validation-for` untuk keduanya hanya berguna jika WorkerController secara eksplisit menambahkan `ModelState.AddModelError("Section", ...)` dari sisi server. Span ini tidak salah, dan berguna sebagai safety net, tetapi reviewer berikutnya mungkin bertanya apakah ada validasi server-side yang menghendaki field ini diisi.

**Fix (opsional):** Jika Section/Unit memang wajib diisi dalam bisnis, tambahkan `[Required]` di model dan controller akan otomatis menolak submission kosong. Jika memang opsional, span sudah benar sebagai safety net — tidak perlu diubah.

---

## Konfirmasi Scope Lock

- `WorkerController.cs` — **TIDAK** ada di diff. Terkonfirmasi.
- `ManageUserViewModel.cs` — **TIDAK** ada di diff. Terkonfirmasi.
- `Views/Admin/EditWorker.cshtml` — **TIDAK** ada di diff. Terkonfirmasi.
- Migration: **TIDAK ADA**. Sesuai klaim phase.

---

_Reviewed: 2026-06-17_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
