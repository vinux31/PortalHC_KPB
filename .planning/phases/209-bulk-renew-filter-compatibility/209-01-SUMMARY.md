---
phase: 209-bulk-renew-filter-compatibility
plan: 01
subsystem: renewal-certificate
tags: [bulk-renew, checkbox-lock, select-all, modal-konfirmasi, filter-empty-state]
dependency_graph:
  requires: [208-01]
  provides: [BULK-01, BULK-02, FILT-01]
  affects: [Views/Admin/RenewalCertificate.cshtml, Views/Admin/Shared/_RenewalGroupedPartial.cshtml]
tech_stack:
  added: []
  patterns: [checkbox-lock-per-group, modal-confirmation-before-redirect, isfiltered-empty-state]
key_files:
  created: []
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/AdminController.cs
    - Views/Admin/Shared/_RenewalGroupedPartial.cshtml
    - Views/Admin/RenewalCertificate.cshtml
decisions:
  - Lock checkbox per group-key (bukan per kategori) — sesuai desain grouped view Phase 208
  - Modal konfirmasi bulk renew sebelum redirect agar admin tidak accidental renew
  - IsFiltered property di ViewModel agar empty state partsal bisa membedakan dua kondisi
metrics:
  duration: "~5 menit"
  completed: "2026-03-20T07:44:04Z"
  tasks_completed: 2
  tasks_total: 3
  files_changed: 4
---

# Phase 209 Plan 01: Bulk Renew per Group + Filter Compatibility Summary

**One-liner:** Checkbox lock per group-key dengan select-all, tombol renew per accordion group, modal konfirmasi sebelum redirect CreateAssessment, dan empty state filter berbeda dari empty state no-data.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Server-side tombol renew per group + IsFiltered + empty state filter | 966650d | Models/CertificationManagementViewModel.cs, Controllers/AdminController.cs, Views/Admin/Shared/_RenewalGroupedPartial.cshtml |
| 2 | JS wiring checkbox lock per group, select-all, modal konfirmasi, cleanup | bb25e3e | Views/Admin/RenewalCertificate.cshtml |

## Checkpoint Pending

Task 3 (human-verify) sedang menunggu verifikasi manual di browser.

## What Was Built

### Task 1 — Server-side
- `public bool IsFiltered { get; set; }` ditambah ke `RenewalGroupViewModel`
- `gvm.IsFiltered` di-set di `FilterRenewalCertificate` action berdasarkan filter params yang aktif
- Tombol `btn-renew-group` (hidden default, `d-none`) di-render di setiap card-header accordion dengan `event.stopPropagation()`, `data-group-key`, dan `data-group-judul`
- Empty state filter (`bi-funnel`) dengan tombol Reset Filter ditambah saat `Model.IsFiltered == true`, empty state no-data tetap saat `IsFiltered == false`

### Task 2 — JS wiring
- `selectedKategori` diganti `selectedGroupKey + pendingRenewParams`
- `wireCheckboxes()` di-refactor: lock per `data-group-key` bukan `data-kategori`
- `wireGroupSelectAll()`: mencentang/menghapus semua checkbox di satu group, dengan guard untuk group yang sudah terkunci
- `updateGroupRenewButton(groupKey)`: show/hide tombol di accordion header sesuai jumlah checkbox tercentang
- `renewGroup(groupKey, judul)`: kumpul params, set `pendingRenewParams`, tampilkan modal konfirmasi
- `bulkRenewConfirmModal`: modal HTML dengan count dan judul sertifikat, tombol Batal + Lanjutkan
- `btn-bulk-renew-confirm` redirect ke `/Admin/CreateAssessment?{pendingRenewParams}`
- `refreshTable()` dan `refreshGroupTable()` reset `selectedGroupKey` setelah reload
- Hapus fungsi lama: `updateRenewSelectedButton`, `renewSelected`, `resetKategoriLock`
- Hapus tombol global `#btn-renew-selected` dari HTML

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check

### Files exist:
- [x] Models/CertificationManagementViewModel.cs — FOUND
- [x] Controllers/AdminController.cs — FOUND
- [x] Views/Admin/Shared/_RenewalGroupedPartial.cshtml — FOUND
- [x] Views/Admin/RenewalCertificate.cshtml — FOUND

### Commits exist:
- [x] 966650d — feat(209-01): server-side tombol renew per group + IsFiltered + empty state filter
- [x] bb25e3e — feat(209-01): JS wiring checkbox lock per group, select-all, modal konfirmasi, cleanup

## Self-Check: PASSED
