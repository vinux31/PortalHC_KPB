---
phase: 208-grouped-view-structure
verified: 2026-03-20T00:00:00Z
status: passed
score: 5/5 must-haves verified
gaps:
  - truth: "Tabel dalam group hanya menampilkan kolom: Checkbox, Nama, Kategori, Sub Kategori, Valid Until, Status, Aksi (tanpa No dan Judul Sertifikat)"
    status: partial
    reason: "Implementasi sudah benar (7 kolom tanpa No dan Judul Sertifikat), tetapi REQUIREMENTS.md GRP-04 mendefinisikan hanya 5 kolom (Checkbox, Nama, Valid Until, Status, Aksi) tanpa Kategori dan Sub Kategori. Ada konflik antara PLAN dan REQUIREMENTS.md yang perlu diklarifikasi."
    artifacts:
      - path: "Views/Admin/Shared/_RenewalGroupTablePartial.cshtml"
        issue: "Memiliki 7 kolom (Kategori + Sub Kategori ada), sedangkan GRP-04 di REQUIREMENTS.md menyebut 5 kolom saja"
    missing:
      - "Klarifikasi: apakah GRP-04 harus diupdate ke 7 kolom (sesuai PLAN), atau implementasi harus dikurangi ke 5 kolom (sesuai REQUIREMENTS)"
  - truth: "Setiap group bisa di-collapse/expand dengan klik header, default semua collapsed"
    status: partial
    reason: "PLAN menyatakan 'default semua collapsed' dan implementasi memang menggunakan aria-expanded=false (collapsed). Namun REQUIREMENTS.md GRP-03 menyatakan 'default: expanded' — konflik antara dokumen requirements dan implementasi aktual."
    artifacts:
      - path: "Views/Admin/Shared/_RenewalGroupedPartial.cshtml"
        issue: "aria-expanded=false (collapsed by default). REQUIREMENTS.md GRP-03 menyebutkan default: expanded."
      - path: ".planning/REQUIREMENTS.md"
        issue: "GRP-03 tulis 'default: expanded' tapi PLAN dan implementasi = collapsed"
    missing:
      - "Klarifikasi dan sinkronisasi: update REQUIREMENTS.md GRP-03 agar sesuai dengan keputusan yang diimplementasikan (collapsed), atau ubah implementasi ke expanded jika itu yang dimaksud user"
human_verification:
  - test: "Buka /Admin/RenewalCertificate — verifikasi semua accordion collapsed saat pertama dibuka"
    expected: "Semua card group dalam kondisi collapsed, user perlu klik untuk expand"
    why_human: "Behavior default collapse/expand tidak bisa diverifikasi via grep"
  - test: "Klik header card satu group — verifikasi expand/collapse berfungsi dan chevron berubah arah"
    expected: "Card expand menampilkan tabel pekerja, icon chevron berubah dari right ke down"
    why_human: "Interaksi Bootstrap collapse memerlukan browser"
  - test: "Verifikasi kolom di tabel dalam group"
    expected: "Tentukan apakah 7 kolom (dengan Kategori, Sub Kategori) atau 5 kolom (tanpa keduanya) yang benar — sesuaikan dengan keputusan user"
    why_human: "Ini adalah pertanyaan keputusan desain yang perlu konfirmasi user"
---

# Phase 208: Grouped View Structure — Verification Report

**Phase Goal:** Admin dapat melihat daftar renewal certificate yang dikelompokkan per nama sertifikat, bukan flat list per orang
**Verified:** 2026-03-20
**Status:** gaps_found — konflik dokumen (PLAN vs REQUIREMENTS.md) pada GRP-03 dan GRP-04
**Re-verification:** Tidak — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Halaman RenewalCertificate menampilkan accordion cards per judul sertifikat, bukan flat table | VERIFIED | `_RenewalGroupedPartial.cshtml` ada, container dikosongkan + di-load via AJAX `refreshTable(1)`. `_RenewalCertificateTablePartial` tidak lagi direferensikan di `RenewalCertificate.cshtml` |
| 2 | Setiap group header menampilkan judul, kategori/sub-kategori, dan badge count (total, expired, akan expired) | VERIFIED | `_RenewalGroupedPartial.cshtml` baris 31-41: chevron + Judul + badge `bg-secondary` (total), `bg-danger` (expired), `bg-warning` (akan expired), + kategori/sub-kategori di kanan |
| 3 | Setiap group bisa di-collapse/expand dengan klik header, default semua collapsed | PARTIAL | Collapse/expand terwire (`data-bs-toggle="collapse"`, `aria-expanded="false"`). Implementasi = collapsed. Namun **REQUIREMENTS.md GRP-03 menyebut "default: expanded"** — konflik dokumen |
| 4 | Tabel dalam group hanya menampilkan 7 kolom (tanpa No dan Judul Sertifikat) | PARTIAL | Tidak ada `<th>No</th>` dan `<th>Judul Sertifikat</th>` — benar. Namun **REQUIREMENTS.md GRP-04 menyebut 5 kolom** (tanpa Kategori dan Sub Kategori), sedangkan PLAN dan implementasi ada 7 kolom |
| 5 | Pagination per group berfungsi via AJAX tanpa reload seluruh halaman | VERIFIED | `refreshGroupTable()` function ada di `RenewalCertificate.cshtml` (baris 341), memanggil `FilterRenewalCertificateGroup`, memperbarui `group-{key}-table`. `wirePagination()` detect `groupKey` + `groupJudul` untuk routing ke per-group. |

**Score:** 3/5 truths VERIFIED, 2/5 PARTIAL (konflik dokumen)

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/CertificationManagementViewModel.cs` | `RenewalGroup` dan `RenewalGroupViewModel` classes | VERIFIED | Baris 97 dan 113 — kedua class ada |
| `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` | Accordion grouped view partial | VERIFIED | Exists, mengandung `data-bs-toggle`, chevron, badge count, collapse body |
| `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` | Single group table partial untuk pagination | VERIFIED | Exists, `@model HcPortal.Models.RenewalGroup`, pagination dengan `data-group-key` |
| `Controllers/AdminController.cs` | `FilterRenewalCertificateGroup` endpoint | VERIFIED | Baris 7025 — endpoint ada dengan parameter groupKey, judul, page, filter params |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `RenewalCertificate.cshtml` | `/Admin/FilterRenewalCertificate` | AJAX `refreshTable()` | VERIFIED | `refreshTable(1)` dipanggil saat DOMContentLoaded, baris 397 |
| `RenewalCertificate.cshtml` | `/Admin/FilterRenewalCertificateGroup` | AJAX `refreshGroupTable()` | VERIFIED | `fetch('/Admin/FilterRenewalCertificateGroup?' + params)` baris 356 |
| `Controllers/AdminController.cs` | `Shared/_RenewalGroupedPartial` | PartialView return dari `FilterRenewalCertificate` | VERIFIED | `return PartialView("Shared/_RenewalGroupedPartial", gvm)` baris 7020 |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| GRP-01 | 208-01-PLAN | Data dikelompokkan per judul sertifikat (bukan flat list) | SATISFIED | GroupBy Judul di AdminController baris 6984, accordion cards di `_RenewalGroupedPartial` |
| GRP-02 | 208-01-PLAN | Group header: judul, kategori/sub-kategori, badge count | SATISFIED | Header card dengan chevron, Judul, badge `bg-secondary/danger/warning`, kategori di kanan |
| GRP-03 | 208-01-PLAN | Collapse/expand per group, default: collapsed (PLAN) vs expanded (REQUIREMENTS.md) | PARTIAL — konflik dokumen | Implementasi = collapsed (`aria-expanded="false"`). PLAN = collapsed. REQUIREMENTS.md = expanded. Perlu keputusan mana yang benar. |
| GRP-04 | 208-01-PLAN | Kolom tabel: PLAN = 7 kolom, REQUIREMENTS.md = 5 kolom | PARTIAL — konflik dokumen | Implementasi = 7 kolom (Checkbox, Nama, Kategori, Sub Kategori, Valid Until, Status, Aksi). REQUIREMENTS.md menyebutkan 5 kolom (tanpa Kategori dan Sub Kategori). |

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Tidak ada | — | — | — | Tidak ada TODO/FIXME/placeholder/empty return di file yang dimodifikasi |

---

## Human Verification Required

### 1. Default State Accordion

**Test:** Buka `/Admin/RenewalCertificate` di browser tanpa menyentuh apapun.
**Expected:** Semua accordion card tampil dalam kondisi collapsed (hanya header yang terlihat, body tersembunyi).
**Why human:** Behavior visual default tidak bisa diverifikasi via grep. Juga penting untuk mengonfirmasi apakah user menginginkan collapsed (sesuai PLAN dan implementasi) atau expanded (sesuai REQUIREMENTS.md GRP-03).

### 2. Chevron Animasi Collapse/Expand

**Test:** Klik header salah satu card group. Klik lagi untuk collapse.
**Expected:** Icon chevron berubah dari kanan (`bi-chevron-right`) ke bawah (`bi-chevron-down`) saat expand, kembali ke kanan saat collapse.
**Why human:** Bootstrap collapse event listener (`show.bs.collapse`, `hide.bs.collapse`) memerlukan browser untuk diverifikasi.

### 3. Konfirmasi Kolom GRP-04

**Test:** Buka tabel dalam salah satu group — hitung kolom yang tampil.
**Expected:** Konfirmasi apakah 7 kolom (Checkbox, Nama, Kategori, Sub Kategori, Valid Until, Status, Aksi) atau 5 kolom (tanpa Kategori dan Sub Kategori) yang diinginkan. Implementasi saat ini = 7 kolom.
**Why human:** Ini adalah keputusan desain — perlu konfirmasi user sebelum REQUIREMENTS.md atau implementasi disesuaikan.

---

## Gaps Summary

**Build:** Berhasil tanpa error (0 errors, 72 warnings — semua warnings pre-existing).

**Inti masalah:** Semua artifact tersedia, terwire, dan fungsional. Namun ada **konflik antara PLAN (208-01-PLAN.md) dan REQUIREMENTS.md** pada dua poin:

1. **GRP-03 — Default state accordion:** PLAN menyebut "default semua collapsed" dan implementasi menggunakan `aria-expanded="false"` (collapsed). Namun REQUIREMENTS.md menulis "default: expanded". Salah satu dokumen perlu dikoreksi untuk sinkronisasi.

2. **GRP-04 — Jumlah kolom tabel:** PLAN menyebut 7 kolom (termasuk Kategori dan Sub Kategori) dan implementasi memiliki 7 kolom. Namun REQUIREMENTS.md menyebut 5 kolom (tanpa Kategori dan Sub Kategori). Perbedaan ini perlu keputusan: apakah REQUIREMENTS.md harus diupdate mengikuti PLAN, atau implementasi perlu dikurangi kolomnya.

**Rekomendasi:** Minta konfirmasi user untuk kedua poin di atas, lalu update REQUIREMENTS.md agar sinkron dengan keputusan yang diambil. Tidak ada perubahan kode yang diperlukan jika user setuju dengan implementasi 7-kolom dan default-collapsed.

---

_Verified: 2026-03-20_
_Verifier: Claude (gsd-verifier)_
