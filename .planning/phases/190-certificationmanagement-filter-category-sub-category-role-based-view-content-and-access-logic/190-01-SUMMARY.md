---
phase: 190-certificationmanagement-filter-category-sub-category-role-based-view-content-and-access-logic
plan: 01
subsystem: CertificationManagement
tags: [backend, model, controller, role-based, filter, export]
dependency_graph:
  requires: []
  provides: [SubKategori field, RoleLevel property, GetSubCategories endpoint, l5OwnDataOnly scoping, category/subCategory filter params]
  affects: [Views/Shared/_CertificationManagementTablePartial, Views/CDP/CertificationManagement]
tech_stack:
  added: []
  patterns: [tuple return from async helper, l5OwnDataOnly parameter for scope override]
key_files:
  created: []
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/CDPController.cs
decisions:
  - "L5 scope override via l5OwnDataOnly bool param — CertificationManagement page shows only own data for L5, not coachees"
  - "SubKategori set to null for both Training and Assessment rows — AssessmentSession tidak menyimpan sub-category"
  - "BuildSertifikatRowsAsync returns tuple (rows, roleLevel) — propagates roleLevel ke ViewModel untuk view layer"
metrics:
  duration: 8min
  completed_date: "2026-03-18"
  tasks_completed: 2
  files_modified: 2
---

# Phase 190 Plan 01: Backend Model & Controller Updates Summary

Backend CertificationManagement siap untuk role-based view layer: SubKategori + RoleLevel di model, L5 scope override, GetSubCategories AJAX endpoint, category/subCategory filter dan export params, serta kolom Sub Kategori di Excel export.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Update Model — SubKategori dan RoleLevel | d69d628 | Models/CertificationManagementViewModel.cs |
| 2 | Update CDPController — L5 override, GetSubCategories, filter params, RoleLevel propagation, export column | ba48151 | Controllers/CDPController.cs |

## What Was Built

### Task 1: Model Changes
- `SertifikatRow.SubKategori` — nullable string property setelah `Kategori`
- `CertificationManagementViewModel.RoleLevel` — int dengan default value 1

### Task 2: Controller Changes
- **BuildSertifikatRowsAsync** — signature baru return tuple `(List<SertifikatRow> rows, int roleLevel)` dengan parameter `bool l5OwnDataOnly = false`. L5 branching: saat `l5OwnDataOnly=true` hanya lihat data diri sendiri; saat false tetap coachee+self.
- **CertificationManagement action** — call dengan `l5OwnDataOnly: true`, set `vm.RoleLevel`, tambah `ViewBag.AllCategories` dan `ViewBag.UserBagian`
- **FilterCertificationManagement** — parameter `category` dan `subCategory` ditambah, filter diterapkan setelah existing filters, `vm.RoleLevel` di-set
- **ExportSertifikatExcel** — parameter `category` dan `subCategory` ditambah, filter yang sama diterapkan, header "Sub Kategori" ditambah setelah "Kategori", kolom data digeser (kolom 7-13)
- **GetSubCategories** — endpoint baru untuk cascade AJAX, return list nama AssessmentCategory anak berdasarkan nama parent

## Deviations from Plan

None — plan dieksekusi tepat sesuai spesifikasi.

Satu auto-fix: Nama property tuple `GetCurrentUserRoleLevelAsync()` adalah `User` (kapital) bukan `user` — diperbaiki saat build error pertama.

## Self-Check: PASSED

- Models/CertificationManagementViewModel.cs — SubKategori dan RoleLevel ada
- Controllers/CDPController.cs — semua acceptance criteria terpenuhi
- Build sukses tanpa error kompilasi C# (file-lock MSB error karena app berjalan, bukan error kode)
- Commit d69d628 — Task 1
- Commit ba48151 — Task 2
