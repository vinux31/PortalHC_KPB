---
quick_id: 260318-qsh
description: "Hapus fitur search & filter tipe, urutkan by Kategori → SubKategori → Status"
---

# Quick Plan: 260318-qsh

## Task 1: Remove search & tipe filter from UI and controller

### Files
- Views/CDP/CertificationManagement.cshtml
- Controllers/CDPController.cs

### Actions
1. Remove `filter-search` input and `filter-tipe` select from CertificationManagement.cshtml
2. Remove JS references to searchEl, tipeEl, searchTimer, debounce logic
3. Remove `search` and `tipe` params from FilterCertificationManagement action
4. Remove `search` and `tipe` params from ExportSertifikatExcel action
5. Remove search/tipe filter logic from both actions
6. Adjust column layout (redistribute col-md sizes)

## Task 2: Change default sort order

### Files
- Controllers/CDPController.cs

### Actions
1. In CertificationManagement action: change OrderByDescending(TanggalTerbit) → OrderBy(Kategori).ThenBy(SubKategori).ThenBy(Status)
2. In FilterCertificationManagement action: same sort change
