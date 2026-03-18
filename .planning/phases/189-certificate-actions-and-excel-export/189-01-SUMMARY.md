---
phase: 189-certificate-actions-and-excel-export
plan: "01"
subsystem: CDP / Certification Management
tags: [excel-export, table-actions, role-gate, certification]
dependency_graph:
  requires: []
  provides: [ExportSertifikatExcel action, kolom Aksi di tabel sertifikat]
  affects: [Views/CDP/CertificationManagement.cshtml, Views/CDP/Shared/_CertificationManagementTablePartial.cshtml, Controllers/CDPController.cs]
tech_stack:
  added: []
  patterns: [ExcelExportHelper pattern, role-gate dengan User.IsInRole, window.exportExcel expose dari IIFE]
key_files:
  created: []
  modified:
    - Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
    - Views/CDP/CertificationManagement.cshtml
    - Controllers/CDPController.cs
decisions:
  - Fungsi exportExcel didefinisikan di dalam IIFE agar mengakses variabel filter, lalu di-expose via window.exportExcel untuk onclick handler
  - Filter logic di ExportSertifikatExcel diduplikasi dari FilterCertificationManagement (bukan di-extract ke helper) karena kedua action memiliki parameter identik
metrics:
  duration: 8min
  completed_date: "2026-03-18"
  tasks_completed: 2
  files_modified: 3
---

# Phase 189 Plan 01: Certificate Actions and Excel Export Summary

**One-liner:** Kolom Aksi dengan conditional icon links per RecordType dan Export Excel role-gated (Admin/HC) dengan filter aktif menggunakan ExcelExportHelper.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Tambah kolom Aksi di partial + export button dan JS di main view | dc2c249 | _CertificationManagementTablePartial.cshtml, CertificationManagement.cshtml |
| 2 | Tambah ExportSertifikatExcel action di CDPController | 31a9ef4 | CDPController.cs |

## What Was Built

### Kolom Aksi di Tabel Sertifikat
- Kolom baru "Aksi" di posisi terakhir thead dan setiap baris data
- Training dengan SertifikatUrl: icon `bi-eye` → link ke URL di tab baru
- Training tanpa SertifikatUrl: dash teks muted
- Assessment: icon `bi-download` → link ke CMP/CertificatePdf dengan SourceId di tab baru
- colspan di baris "Belum ada data sertifikat" diupdate dari 11 ke 12

### Export Excel (Admin/HC Only)
- Tombol "Export Excel" di header, visible hanya untuk role Admin/HC via `User.IsInRole`
- Fungsi `exportExcel(e)` di dalam IIFE, membaca filter aktif (bagian, unit, status, tipe, search)
- Expose via `window.exportExcel` agar dapat dipanggil dari onclick attribute
- URL: `/CDP/ExportSertifikatExcel?{filter params}`

### ExportSertifikatExcel Action
- Method: GET, `[Authorize(Roles = "Admin, HC")]`
- Filter identik dengan FilterCertificationManagement
- 12 kolom: No, Nama, Bagian, Unit, Judul, Kategori, Nomor Sertifikat, Tgl Terbit, Valid Until, Tipe, Status, Sertifikat URL
- Nama file: `Sertifikat_Export_{yyyy-MM-dd}.xlsx`
- Menggunakan `ExcelExportHelper.CreateSheet` dan `ExcelExportHelper.ToFileResult`

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

- dotnet build: 0 error, 71 warning (pre-existing warnings, tidak terkait perubahan)
- Semua acceptance criteria Task 1 terpenuhi (9/9 grep checks)
- Semua acceptance criteria Task 2 terpenuhi (6/6 grep checks)

## Self-Check: PASSED

- Files exist: partial view, main view, CDPController — all FOUND
- Commits exist: dc2c249, 31a9ef4 — both FOUND
