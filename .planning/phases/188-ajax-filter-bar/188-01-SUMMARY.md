---
phase: 188-ajax-filter-bar
plan: 01
subsystem: CDP / CertificationManagement
tags: [ajax, filter, partial-view, cascade, debounce]
dependency_graph:
  requires: [186-01, 187-01]
  provides: [FilterCertificationManagement AJAX endpoint, _CertificationManagementTablePartial]
  affects: [Views/CDP/CertificationManagement.cshtml, Controllers/CDPController.cs]
tech_stack:
  added: []
  patterns: [fetch + AbortController, data-* attributes for summary sync, JS debounce 300ms, cascade dropdown]
key_files:
  created:
    - Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/CertificationManagement.cshtml
decisions:
  - FilterCertificationManagement melakukan filter in-memory (status adalah derived field, bukan DB column)
  - Pagination menggunakan data-page JS, bukan asp-action links (wired ulang setelah setiap innerHTML replace)
  - Summary cards update via data-* attributes pada root element partial view
  - Filter bar ditempatkan dalam card terpisah antara summary cards dan table container
metrics:
  duration: ~15min
  completed_date: "2026-03-18"
  tasks_completed: 2
  files_changed: 3
---

# Phase 188 Plan 01: AJAX Filter Bar for CertificationManagement Summary

Filter bar interaktif Bagian/Unit cascade + Status + Tipe + free-text search dengan fetch+AbortController, debounce 300ms, dan summary card sync via data-* attributes.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Backend — FilterCertificationManagement action + partial view | ca28205 | CDPController.cs, _CertificationManagementTablePartial.cshtml, CertificationManagement.cshtml |
| 2 | Frontend — Filter bar UI + JS wiring | ca28205 | CertificationManagement.cshtml |

## What Was Built

### FilterCertificationManagement Action (CDPController.cs)
- Action `[HttpGet]` baru dengan 6 parameter: bagian, unit, status, tipe, search, page
- Filter in-memory menggunakan `Enum.TryParse<CertificateStatus>` dan `Enum.TryParse<RecordType>`
- Free-text search pada NamaWorker, Judul, NomorSertifikat
- Returns `PartialView("Shared/_CertificationManagementTablePartial", vm)`
- `ViewBag.AllBagian = OrganizationStructure.GetAllSections()` ditambahkan ke action CertificationManagement yang sudah ada

### _CertificationManagementTablePartial.cshtml (baru)
- Root `<div id="cert-table-content">` dengan data-total, data-aktif, data-akan-expired, data-expired, data-permanent
- Tabel identik dengan versi sebelumnya (thead/tbody dengan badges)
- Pagination menggunakan `data-page` atribut (bukan asp-action links)
- Info text "Menampilkan X - Y dari Z sertifikat"

### CertificationManagement.cshtml (direfaktor)
- Summary cards mendapat IDs: count-total, count-aktif, count-akan-expired, count-expired
- Filter bar: Bagian (cascade ke Unit via GetCascadeOptions), Status, Tipe, Search input, Reset button
- `<div id="cert-table-container">` merender partial untuk initial load
- Loading CSS `.dashboard-loading` dengan spinner animation
- JS wiring: AbortController, debounce 300ms, wirePagination(), updateSummaryCards()

## Deviations from Plan

None — plan dieksekusi tepat seperti ditulis. Task 1 dan Task 2 digabung dalam satu commit karena perubahan CertificationManagement.cshtml mencakup kedua task.

## Self-Check

- [x] _CertificationManagementTablePartial.cshtml dibuat
- [x] CDPController.cs mengandung FilterCertificationManagement
- [x] CertificationManagement.cshtml mengandung filter-bagian, AbortController, updateSummaryCards
- [x] Build: hanya MSB3027 (file lock karena app berjalan) — tidak ada error CS
