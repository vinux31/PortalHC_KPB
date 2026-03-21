---
phase: 221-integrasi-codebase
plan: "01"
subsystem: OrganizationUnit Integration
tags: [database, helper-methods, admin-controller, cmp-controller, organization-structure]
dependency_graph:
  requires: [219-db-model-migration, 220-crud-page-kelola-data]
  provides: [GetAllSectionsAsync, GetUnitsForSectionAsync, GetSectionUnitsDictAsync]
  affects: [AdminController, CMPController, RecordsTeam]
tech_stack:
  added: []
  patterns: [DbContext helper methods, ViewBag SectionUnitsJson pattern]
key_files:
  created: []
  modified:
    - Data/ApplicationDbContext.cs
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs
    - Views/CMP/RecordsTeam.cshtml
decisions:
  - SectionUnitsJson dikirim sebagai JSON string ke ViewBag agar konsisten dengan pola SubCategoryMapJson yang sudah ada
  - CoachingProton action juga mendapat SectionUnitsJson selain SectionUnits dan Sections untuk dukungan filter cascade
metrics:
  duration: "~20 menit"
  completed_date: "2026-03-21"
  tasks_completed: 2
  files_modified: 4
---

# Phase 221 Plan 01: Helper Methods DbContext + AdminController & RecordsTeam Integrasi Summary

**One-liner:** 3 helper async methods di ApplicationDbContext menggantikan OrganizationStructure static class di AdminController (12 referensi) dan RecordsTeam view.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Helper methods DbContext + AdminController integrasi | d2b4840 | ApplicationDbContext.cs, AdminController.cs |
| 2 | RecordsTeam view + CMPController verifikasi | 686952a | CMPController.cs, RecordsTeam.cshtml |

## What Was Built

### Task 1: Helper Methods + AdminController

Tambah 3 helper async methods ke `ApplicationDbContext`:
- `GetAllSectionsAsync()` — query semua OrganizationUnit dengan ParentId == null dan IsActive, ordered by DisplayOrder
- `GetUnitsForSectionAsync(sectionName)` — query children units untuk bagian tertentu
- `GetSectionUnitsDictAsync()` — query semua bagian dan units, return Dictionary<string, List<string>>

Ganti 12 referensi `OrganizationStructure.*` di AdminController dengan helper methods:
- 5x CreateAssessment (GET + POST error paths) — `GetAllSections()` → `GetAllSectionsAsync()`
- 1x EditAssessment GET — `GetAllSections()` → `GetAllSectionsAsync()`
- 1x CoachingProton — dua referensi (`GetAllSections()` + `SectionUnits`) → `GetSectionUnitsDictAsync()` + SectionUnitsJson
- 2x ManageWorkers — `GetAllSections()` + `GetUnitsForSection()` → async equivalents
- 2x ExportWorkers — `GetUnitsForSection()` → `GetUnitsForSectionAsync()`
- 1x RenewalCertificate — `GetAllSections()` → `GetAllSectionsAsync()`

### Task 2: RecordsTeam View + CMPController

CMPController Records action (partial view trigger):
- Tambah `GetSectionUnitsDictAsync()` call
- Set `ViewBag.SectionUnitsJson` dan `ViewBag.AllSections`

RecordsTeam.cshtml (partial view):
- Ganti `OrganizationStructure.GetAllSections()` + `OrganizationStructure.SectionUnits` dengan `ViewBag.SectionUnitsJson` + `ViewBag.AllSections`

## Deviations from Plan

None — plan executed exactly as written.

## Verification Results

- `grep -c "OrganizationStructure" Controllers/AdminController.cs` → **0**
- `grep -c "OrganizationStructure" Views/CMP/RecordsTeam.cshtml` → **0**
- `grep -c "OrganizationStructure" Controllers/CMPController.cs` → **0**
- `grep -c "GetAllSectionsAsync\|GetUnitsForSectionAsync\|GetSectionUnitsDictAsync" Data/ApplicationDbContext.cs` → **3**
- `dotnet build` → **0 errors, 72 warnings (semua pre-existing CA1416)**

## Self-Check: PASSED

- [x] `Data/ApplicationDbContext.cs` — helper methods ditambahkan
- [x] `Controllers/AdminController.cs` — 0 referensi OrganizationStructure
- [x] `Controllers/CMPController.cs` — 0 referensi OrganizationStructure, SectionUnitsJson dikirim
- [x] `Views/CMP/RecordsTeam.cshtml` — ViewBag.SectionUnitsJson dipakai
- [x] Commit d2b4840 — Task 1
- [x] Commit 686952a — Task 2
- [x] dotnet build sukses
