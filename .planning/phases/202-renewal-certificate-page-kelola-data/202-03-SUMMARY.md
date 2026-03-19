---
phase: 202-renewal-certificate-page-kelola-data
plan: 03
subsystem: ui
tags: [filter, cascade, dropdown, renewal-certificate]

requires:
  - phase: 202-01
    provides: RenewalCertificate page dan FilterRenewalCertificate endpoint
provides:
  - Filter Bagian dari OrganizationStructure (semua section)
  - Filter Kategori dari AssessmentCategories DB (parent only)
  - Filter Sub Kategori cascade dari Kategori via GetSubCategories
affects: []

tech-stack:
  added: []
  patterns: [cascade-dropdown-filter]

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/RenewalCertificate.cshtml

key-decisions:
  - "Reuse existing GetSubCategories endpoint dari CDPController untuk cascade Sub Kategori"

patterns-established:
  - "Cascade sub-category filter: sama pattern dengan CertificationManagement"

requirements-completed: [RNPAGE-03]

duration: 2min
completed: 2026-03-19
---

# Phase 202 Plan 03: Fix Filter dan Tambah Sub Kategori Summary

**Fix filter Bagian/Kategori agar populate dari sumber data master (bukan dari data renewal), dan tambah dropdown Sub Kategori cascade dari Kategori**

## What Was Done

### Task 1: Fix ViewBag populasi dan tambah subCategory filter di Controller
- `ViewBag.AllBagian` diganti dari `allRows.Select(r => r.Bagian)` ke `OrganizationStructure.GetAllSections()` — semua section tampil termasuk yang tidak punya data renewal
- `ViewBag.AllCategories` diganti dari `allRows.Select(r => r.Kategori)` ke query DB `AssessmentCategories` (parent only, active, sorted)
- Parameter `subCategory` ditambahkan di `FilterRenewalCertificate` method signature
- Filter `subCategory` ditambahkan setelah filter `category`
- **Commit:** c32dc4b

### Task 2: Tambah dropdown Sub Kategori dan update JS di View
- Dropdown Sub Kategori ditambahkan di filter bar (setelah Kategori, sebelum Status)
- Cascade JS: saat Kategori berubah, fetch `/CDP/GetSubCategories?category=...` dan populate Sub Kategori
- `refreshTable` mengirim `subCategory` param ke server
- Reset button membersihkan Sub Kategori (innerHTML reset + disabled)
- **Commit:** 83effe1

## Deviations from Plan

None - plan executed exactly as written.

## Commits

| # | Hash | Message |
|---|------|---------|
| 1 | c32dc4b | feat(202-03): fix ViewBag populasi filter dan tambah subCategory filter |
| 2 | 83effe1 | feat(202-03): tambah dropdown Sub Kategori dengan cascade JS di view |
