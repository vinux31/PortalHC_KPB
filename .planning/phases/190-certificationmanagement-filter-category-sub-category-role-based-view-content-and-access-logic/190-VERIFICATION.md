---
phase: 190-certificationmanagement-filter-category-sub-category-role-based-view-content-and-access-logic
verified: 2026-03-18T00:00:00Z
status: human_needed
score: 13/13 automated must-haves verified
human_verification:
  - test: "Login sebagai L5 — cek hanya data diri sendiri (bukan coachee), summary cards tidak tampil, kolom Nama/Bagian/Unit tidak tampil, filter Bagian dan Unit disabled"
    expected: "L5 hanya melihat sertifikat dirinya sendiri. Tidak ada summary cards. Tabel tidak menampilkan kolom Nama, Bagian, Unit. Filter Bagian dan Unit dalam keadaan disabled."
    why_human: "Butuh login L5 untuk memverifikasi scope override dan conditional rendering"
  - test: "Login sebagai L4 — cek summary cards tampil, filter Bagian disabled dan pre-filled dengan bagian sendiri, Unit auto-load"
    expected: "Summary cards muncul. Filter Bagian disabled dan berisi bagian user. Unit auto-load via cascade. Kolom Nama/Bagian/Unit tampil."
    why_human: "Butuh login L4 untuk memverifikasi conditional state"
  - test: "Login sebagai Admin/L1-3 — pilih Category dari dropdown, Sub-Category harus populate otomatis via AJAX"
    expected: "Sub-Category dropdown terisi setelah memilih Category. Refresh tabel AJAX membawa parameter category dan subCategory."
    why_human: "Behavior AJAX cascade tidak bisa diverifikasi secara statis"
  - test: "Export Excel — cek ada kolom 'Sub Kategori' setelah 'Kategori' (total 13 kolom)"
    expected: "File Excel berhasil diunduh dengan header: No, Nama, Bagian, Unit, Judul, Kategori, Sub Kategori, Nomor Sertifikat, Tgl Terbit, Valid Until, Tipe, Status, Sertifikat URL"
    why_human: "Butuh download aktual untuk verifikasi output Excel"
---

# Phase 190: CertificationManagement Filter & Role-Based View Verification Report

**Phase Goal:** Filter cascade Category/Sub-Category, role-based view content (summary cards, kolom tabel, filter disabled state), dan L5 scope override ke own-data-only
**Verified:** 2026-03-18
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | SertifikatRow memiliki property SubKategori | VERIFIED | `Models/CertificationManagementViewModel.cs:34` — `public string? SubKategori { get; set; }` |
| 2 | CertificationManagementViewModel memiliki property RoleLevel | VERIFIED | `Models/CertificationManagementViewModel.cs:72` — `public int RoleLevel { get; set; } = 1;` |
| 3 | BuildSertifikatRowsAsync menerima parameter l5OwnDataOnly dan override scope L5 | VERIFIED | `CDPController.cs:3201` — signature tuple, `3222` — `if (l5OwnDataOnly)` branching |
| 4 | GetSubCategories endpoint mengembalikan children AssessmentCategory berdasarkan parent name | VERIFIED | `CDPController.cs:300` — endpoint terdaftar dengan query parent |
| 5 | FilterCertificationManagement menerima parameter category dan subCategory | VERIFIED | `CDPController.cs:3085-3086` — kedua parameter hadir |
| 6 | ExportSertifikatExcel menerima parameter category dan subCategory serta menambah kolom Sub Kategori | VERIFIED | `CDPController.cs:3142-3143,3174,3188` — parameter + header + cell mapping |
| 7 | CertificationManagement dan FilterCertificationManagement set vm.RoleLevel dari GetCurrentUserRoleLevelAsync | VERIFIED | 2 occurrences `vm.RoleLevel = roleLevel` di `CDPController.cs:3060,3124` |
| 8 | L5/L6 tidak melihat summary cards | VERIFIED | `CertificationManagement.cshtml:30` — `@if (Model.RoleLevel <= 4)` wrap |
| 9 | L5/L6 tidak melihat kolom Nama, Bagian, Unit di tabel | VERIFIED | `_CertificationManagementTablePartial.cshtml:16,51` — `@if (Model.RoleLevel <= 4)` pada header dan body |
| 10 | L4 melihat filter Bagian disabled dan pre-filled dengan bagian sendiri | VERIFIED (static) | `CertificationManagement.cshtml:82-85` — `@(Model.RoleLevel >= 4 ? "disabled" : "")` + pre-fill L4 |
| 11 | L5/L6 melihat filter Bagian dan Unit disabled | VERIFIED (static) | `CertificationManagement.cshtml:82,103` — conditional disabled attributes |
| 12 | Kolom Sub Kategori tampil di tabel untuk semua role | VERIFIED | `_CertificationManagementTablePartial.cshtml:24,59` — header dan data cell hadir |
| 13 | Filter Category/Sub-Category cascade dikirim ke AJAX refresh dan export | VERIFIED | `CertificationManagement.cshtml:271-272,348-349` — `params.set('category')` dan `params.set('subCategory')` di kedua fungsi |

**Score:** 13/13 truths verified (automated checks)

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/CertificationManagementViewModel.cs` | SubKategori di SertifikatRow, RoleLevel di ViewModel | VERIFIED | Kedua property hadir pada baris 34 dan 72 |
| `Controllers/CDPController.cs` | GetSubCategories, l5OwnDataOnly, category/subCategory params, RoleLevel propagation, export kolom | VERIFIED | Semua 10 acceptance criteria dari PLAN terpenuhi |
| `Views/CDP/CertificationManagement.cshtml` | filter-category, filter-subcategory, GetSubCategories JS, role-based disabled, ViewBag.AllCategories | VERIFIED | Semua 9 acceptance criteria dari PLAN terpenuhi |
| `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` | Sub Kategori di thead, row.SubKategori di tbody, RoleLevel <= 4 conditional (2x), colCount | VERIFIED | Semua 4 acceptance criteria dari PLAN terpenuhi |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `CertificationManagement.cshtml` | `CDPController.GetSubCategories` | JS fetch `/CDP/GetSubCategories?category=` | WIRED | `cshtml:216` — fetch call hadir |
| `CertificationManagement.cshtml` | `CDPController.FilterCertificationManagement` | AJAX params category + subCategory | WIRED | `cshtml:271-272` — params.set hadir di refreshTable |
| `CertificationManagement.cshtml` | `CDPController.ExportSertifikatExcel` | params category + subCategory di export | WIRED | `cshtml:348-349` — params.set hadir di exportExcel |
| `CDPController.cs` | `CertificationManagementViewModel.RoleLevel` | `vm.RoleLevel = roleLevel` | WIRED | 2 assignments: baris 3060 dan 3124 |
| `_CertificationManagementTablePartial.cshtml` | `SertifikatRow.SubKategori` | `row.SubKategori` | WIRED | `partial:59` — hadir di tbody |

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|-------------|-----------|--------|----------|
| ROLE-SCOPE | 190-01 | L5 hanya melihat data diri sendiri (bukan coachee) | VERIFIED* | `CDPController.cs:3222` — `if (l5OwnDataOnly)` dengan `scopedUserIds = new List { user.Id }` |
| CATEGORY-FILTER | 190-01 | Filter berdasarkan kategori sertifikat | VERIFIED | `CDPController.cs:3085,3109` — parameter + filter query hadir |
| SUBCATEGORY-COL | 190-01 | Kolom SubKategori di model | VERIFIED | `CertificationManagementViewModel.cs:34` |
| EXPORT-SUBCOL | 190-01 | Kolom Sub Kategori di Excel export | VERIFIED | `CDPController.cs:3174,3188` — header dan cell mapping |
| ROLE-VIEW | 190-02 | Conditional rendering summary cards dan kolom tabel berdasarkan role | VERIFIED (static) | `CertificationManagement.cshtml:30`, `_TablePartial.cshtml:16,51` |
| FILTER-CASCADE | 190-02 | Cascade Category -> Sub-Category via AJAX | VERIFIED (static) | `CertificationManagement.cshtml:216` — fetch ke GetSubCategories |
| SUBCATEGORY-DISPLAY | 190-02 | Kolom Sub Kategori tampil di tabel | VERIFIED | `_CertificationManagementTablePartial.cshtml:24,59` |

*ROLE-SCOPE memerlukan verifikasi runtime untuk konfirmasi behavior L5

---

### Anti-Patterns Found

Tidak ditemukan anti-pattern blocker. Implementasi lengkap dan substantif.

---

### Human Verification Required

#### 1. L5 Scope Override — Own Data Only

**Test:** Login sebagai user L5. Buka CDP > Certification Management.
**Expected:** Hanya sertifikat milik user sendiri yang tampil (bukan sertifikat coachee). Summary cards tidak muncul. Kolom Nama, Bagian, Unit tidak tampil di tabel. Filter Bagian dan Unit dalam keadaan disabled.
**Why human:** Butuh login aktif L5 dan data sertifikat terisi untuk memverifikasi scope override runtime.

#### 2. L4 Pre-fill dan Unit Auto-load

**Test:** Login sebagai user L4. Buka CDP > Certification Management.
**Expected:** Filter Bagian disabled dan menampilkan bagian user yang sedang login. Filter Unit auto-load berdasarkan bagian tersebut. Summary cards tetap tampil. Semua kolom (termasuk Nama/Bagian/Unit) tampil.
**Why human:** Pre-fill dan cascade auto-load hanya bisa diverifikasi dengan login aktif dan data user yang sesuai.

#### 3. Category -> Sub-Category Cascade AJAX

**Test:** Login sebagai Admin/L1-3. Buka halaman. Pilih salah satu nilai dari dropdown "Kategori".
**Expected:** Dropdown "Sub Kategori" terisi otomatis dengan children dari kategori yang dipilih. Mengubah Sub Kategori lalu menekan filter me-refresh tabel via AJAX dengan parameter category dan subCategory.
**Why human:** AJAX runtime behavior tidak bisa diverifikasi secara statis.

#### 4. Export Excel dengan Kolom Sub Kategori

**Test:** Pilih filter tertentu lalu klik tombol Export Excel.
**Expected:** File Excel berhasil diunduh dengan 13 kolom: No, Nama, Bagian, Unit, Judul, Kategori, Sub Kategori, Nomor Sertifikat, Tgl Terbit, Valid Until, Tipe, Status, Sertifikat URL.
**Why human:** Output file Excel hanya bisa diverifikasi dengan download aktual.

---

## Gaps Summary

Tidak ada gap yang teridentifikasi dari pemeriksaan otomatis. Semua 13 truths terverifikasi secara statis — model, controller, dan view sudah lengkap dan terhubung dengan benar. Status `human_needed` karena ada 4 item yang membutuhkan verifikasi runtime: L5 scope override behavior, L4 pre-fill cascade, Category/Sub-Category AJAX cascade, dan output Excel.

---

_Verified: 2026-03-18_
_Verifier: Claude (gsd-verifier)_
