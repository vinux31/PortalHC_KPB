---
phase: 202-renewal-certificate-page-kelola-data
plan: 01
subsystem: admin-renewal-certificate
tags: [controller, view, ajax, pagination, bulk-select, certificate-renewal]
dependency_graph:
  requires: [200-02, 201-01]
  provides: [renewal-certificate-page]
  affects: [AdminController, Views/Admin]
tech_stack:
  added: []
  patterns: [BuildRenewalRowsAsync-pattern, AJAX-filter-partial, checkbox-category-lock]
key_files:
  created:
    - Views/Admin/RenewalCertificate.cshtml
    - Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml
  modified:
    - Controllers/AdminController.cs
decisions:
  - "BuildRenewalRowsAsync di AdminController tidak menggunakan role scoping — Admin/HC punya akses penuh ke semua data"
  - "Filter Status hanya menawarkan Expired dan AkanExpired (bukan Aktif/Permanent) karena POST-FILTER sudah menyaring di BuildRenewalRowsAsync"
  - "Direktori Views/Admin/Shared dibuat baru untuk menampung partial view ini"
metrics:
  duration: ~35min
  completed: 2026-03-19
  tasks_completed: 2
  files_modified: 3
---

# Phase 202 Plan 01: Renewal Certificate Page Summary

**One-liner:** Halaman /Admin/RenewalCertificate dengan AJAX filter, checkbox bulk select category-lock, dan tombol Renew ke CreateAssessment.

## What Was Built

### Task 1: Controller Actions (AdminController.cs)

Tiga member baru ditambahkan ke akhir AdminController dalam region `#region Renewal Certificate`:

- **`BuildRenewalRowsAsync()`** — Private async method tanpa role scoping. Menjalankan batch renewal lookup 4-set identik dengan CDPController, lalu POST-FILTER hanya baris `!IsRenewed && (Expired || AkanExpired)`. Sort: Expired dahulu, lalu ValidUntil ascending.
- **`RenewalCertificate(int page = 1)`** — GET action `[Authorize(Roles = "Admin, HC")]`. Membangun CertificationManagementViewModel, mengisi ViewBag.AllBagian dan ViewBag.AllCategories, ViewBag.SelectedView = "RenewalCertificate".
- **`FilterRenewalCertificate(...)`** — GET action untuk AJAX. Menerapkan filter in-memory, paginate, kembalikan PartialView `Shared/_RenewalCertificateTablePartial`.

### Task 2: Views

**RenewalCertificate.cshtml:**
- Layout: container-fluid py-4, header h4 dengan bi-arrow-repeat text-warning, breadcrumb Kelola Data > Renewal Sertifikat
- Summary: 2 card (Expired text-danger, Akan Expired text-warning) dengan id count-expired dan count-akan-expired
- Toolbar: btn btn-warning d-none id btn-renew-selected dengan onclick="renewSelected()"
- Filter bar: Bagian, Unit (cascade via /CDP/GetCascadeOptions), Kategori, Status (hanya Expired/AkanExpired), Reset
- Table container: div#cert-table-container merender partial
- JavaScript: refreshTable(), wireCheckboxes() dengan category-lock logic (selectedKategori), renewSelected() membangun multi-param URL ke CreateAssessment, wirePagination(), updateSummaryCards()

**_RenewalCertificateTablePartial.cshtml:**
- Hidden spans untuk update summary cards setelah AJAX
- Tabel 9 kolom: Checkbox (cb-select dengan data-kategori/sourceid/recordtype), No, Nama, Judul, Kategori (d-none d-md), Sub Kategori (d-none d-md), Valid Until, Status badge, Aksi Renew
- Empty state: bi-patch-check-fill fs-1 text-success, teks, link Kembali ke Kelola Data
- Pagination: nav ul.pagination data-page
- Select-all header checkbox dengan category-aware logic

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check

- [x] Views/Admin/RenewalCertificate.cshtml exists
- [x] Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml exists
- [x] Controllers/AdminController.cs modified with 3 new members
- [x] dotnet build succeeds with 0 errors
- [x] Commit 7f2888d (Task 1 controller)
- [x] Commit baf197a (Task 2 views)

## Self-Check: PASSED
