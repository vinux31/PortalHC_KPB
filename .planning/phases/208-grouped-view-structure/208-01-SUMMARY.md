---
phase: 208-grouped-view-structure
plan: 01
subsystem: Admin/RenewalCertificate
tags: [grouped-view, accordion, pagination, ajax, viewmodel]
dependency_graph:
  requires: []
  provides: [RenewalGroup, RenewalGroupViewModel, FilterRenewalCertificateGroup, _RenewalGroupedPartial, _RenewalGroupTablePartial]
  affects: [Views/Admin/RenewalCertificate, Controllers/AdminController, Models/CertificationManagementViewModel]
tech_stack:
  added: []
  patterns: [accordion collapse Bootstrap 5, AJAX partial view pagination per group, ViewModel grouping layer]
key_files:
  created:
    - Views/Admin/Shared/_RenewalGroupedPartial.cshtml
    - Views/Admin/Shared/_RenewalGroupTablePartial.cshtml
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/AdminController.cs
    - Views/Admin/RenewalCertificate.cshtml
decisions:
  - "GroupKey di-encode dengan Base64 URL-safe (replace +_/-=) agar aman sebagai HTML id attribute"
  - "Container cert-table-container dikosongkan, isi di-load via AJAX refreshTable(1) saat DOMContentLoaded"
  - "cb-group-select-all checkbox ada di HTML tapi belum di-wire — scope Phase 209"
metrics:
  duration: 10 minutes
  completed_date: "2026-03-20"
  tasks_completed: 2
  files_changed: 5
---

# Phase 208 Plan 01: Grouped View Structure Summary

Mengubah halaman RenewalCertificate dari flat table menjadi accordion grouped view per judul sertifikat dengan badge count expired/akan-expired dan pagination per group via AJAX.

## What Was Built

### Task 1: ViewModel + Controller

Ditambahkan dua kelas baru ke `Models/CertificationManagementViewModel.cs`:

- `RenewalGroup` — mewakili satu group judul sertifikat: GroupKey (Base64 URL-safe), Judul, Kategori, SubKategori, TotalCount, ExpiredCount, AkanExpiredCount, MinValidUntil, Rows (paginated), CurrentPage, TotalPages, PageSize=10
- `RenewalGroupViewModel` — container: List<RenewalGroup>, TotalExpiredCount, TotalAkanExpiredCount

`FilterRenewalCertificate` diubah: setelah filtering + sorting, data di-group by Judul, diurutkan by MinValidUntil, setiap group di-paginate halaman pertama, lalu return `PartialView("Shared/_RenewalGroupedPartial", gvm)`.

Endpoint baru `FilterRenewalCertificateGroup` ditambah: menerima groupKey, judul, page + filter params, return `PartialView("Shared/_RenewalGroupTablePartial", group)` untuk pagination per group.

### Task 2: Partial Views + JS

`_RenewalGroupedPartial.cshtml` — accordion cards per group: header clickable dengan chevron, badge count (abu-abu total, merah expired, kuning akan expired), kategori/sub-kategori di kanan, collapse body berisi `_RenewalGroupTablePartial`.

`_RenewalGroupTablePartial.cshtml` — tabel 7 kolom: Checkbox, Nama, Kategori, Sub Kategori, Valid Until, Status, Aksi. Tidak ada kolom No dan Judul Sertifikat. Pagination per group dengan data-group-key + data-group-judul.

`RenewalCertificate.cshtml` diubah:
- Container dikosongkan (tidak lagi embed partial langsung)
- Initial load: `refreshTable(1)` via AJAX
- `wirePagination()` detect per-group vs global pagination
- Tambah `refreshGroupTable()` untuk AJAX per group
- Tambah `wireGroupChevrons()` untuk animasi chevron Bootstrap collapse events
- Hapus blok `#cb-select-all` global (per-group select-all = scope Phase 209)

## Deviations from Plan

None — plan dieksekusi sesuai spesifikasi.

## Self-Check

### Files Created/Modified
- [x] Models/CertificationManagementViewModel.cs — RenewalGroup + RenewalGroupViewModel
- [x] Views/Admin/Shared/_RenewalGroupedPartial.cshtml — accordion grouped partial
- [x] Views/Admin/Shared/_RenewalGroupTablePartial.cshtml — single group table partial
- [x] Controllers/AdminController.cs — FilterRenewalCertificateGroup endpoint
- [x] Views/Admin/RenewalCertificate.cshtml — container + JS updated

### Commits
- c5be74b — feat(208-01): ViewModel grouping layer dan endpoint pagination per group
- 013897e — feat(208-01): accordion grouped view + pagination per group + JS wiring

## Self-Check: PASSED
