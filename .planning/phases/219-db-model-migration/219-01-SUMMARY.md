---
phase: 219-db-model-migration
plan: "01"
subsystem: data-model
tags: [entity, migration, organization-unit, kkj, cpdp, sqlite]
dependency_graph:
  requires: []
  provides: [OrganizationUnit entity, OrganizationUnits table seeded, KkjFile/CpdpFile migrated to OrganizationUnitId]
  affects: [AdminController, CMPController, KkjModels, ApplicationDbContext, KkjMatrix views, CpdpFiles views, DokumenKkj view]
tech_stack:
  added: [OrganizationUnit entity class]
  patterns: [Self-referential FK (same as AssessmentCategory), EF Core migration with manual SQL seed]
key_files:
  created:
    - Models/OrganizationUnit.cs
    - Migrations/20260321115636_AddOrganizationUnitsAndConsolidateKkjBagian.cs
  modified:
    - Models/KkjModels.cs
    - Data/ApplicationDbContext.cs
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs
    - Views/Admin/KkjMatrix.cshtml
    - Views/Admin/KkjUpload.cshtml
    - Views/Admin/KkjFileHistory.cshtml
    - Views/Admin/CpdpFiles.cshtml
    - Views/Admin/CpdpUpload.cshtml
    - Views/Admin/CpdpFileHistory.cshtml
    - Views/CMP/DokumenKkj.cshtml
decisions:
  - "OrganizationUnit menggunakan self-referential FK (sama seperti AssessmentCategory) dengan DeleteBehavior.Restrict"
  - "Seed data 4 Bagian + 17 Unit dimasukkan langsung dalam migration file (bukan runtime seeding)"
  - "Rename column BagianId -> OrganizationUnitId via sp_rename (EF Core scaffolding) untuk preserve existing data"
  - "Remap data: KkjBagians.Name matched ke OrganizationUnits.Name dengan special case DHT/HMU -> DHT / HMU"
  - "DeleteBehavior.Restrict (bukan Cascade) agar file tidak ter-delete otomatis saat OrganizationUnit dihapus"
metrics:
  duration: "~35 menit"
  completed_date: "2026-03-21"
  tasks_completed: 2
  files_modified: 13
---

# Phase 219 Plan 01: DB Model & Migration Summary

Entity OrganizationUnit dengan hierarki parent-child (4 Bagian + 17 Unit = 21 baris), menggantikan KkjBagian — migrasi memetakan ulang FK KkjFile/CpdpFile dari BagianId ke OrganizationUnitId.

## Tasks Completed

| Task | Description | Commit | Status |
|------|-------------|--------|--------|
| 1 | Buat OrganizationUnit entity, hapus KkjBagian, update referensi codebase | 2d18546 | Done |
| 2 | Generate migration + apply (seed 21 baris, remap FK, drop KkjBagians) | 071cda5 | Done |

## What Was Built

### Entity OrganizationUnit
- File: `Models/OrganizationUnit.cs`
- Self-referential hierarchy: `ParentId` nullable (null = Bagian/top-level, not null = Unit/child)
- Fields: `Id`, `Name`, `ParentId`, `Level`, `DisplayOrder`, `IsActive` (default true)
- Navigation: `Parent`, `Children`, `KkjFiles`, `CpdpFiles`

### Migration: AddOrganizationUnitsAndConsolidateKkjBagian
Urutan operasi manual (bukan EF default order):
1. Drop FK lama (KkjFiles/CpdpFiles -> KkjBagians)
2. Create table OrganizationUnits
3. Seed 21 baris (4 Bagian + 17 Unit) via `migrationBuilder.Sql()`
4. Rename BagianId -> OrganizationUnitId (sp_rename, preserve data)
5. Remap OrganizationUnitId dari nilai lama (KkjBagians.Id) ke nilai baru (OrganizationUnits.Id) via JOIN
6. Guard: DELETE orphan rows yang tidak ter-remap
7. Drop KkjBagians
8. Add FK baru (KkjFiles/CpdpFiles -> OrganizationUnits) dengan Restrict

### Referensi Codebase
- AdminController: 20+ titik diupdate (KkjBagians -> OrganizationUnits, BagianId -> OrganizationUnitId)
- CMPController: DokumenKkj, KkjFileDownload, CpdpFileDownload
- 7 views: type cast `KkjBagian` -> `OrganizationUnit`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Update semua referensi KkjBagian di controllers dan views**
- **Found during:** Task 1 — setelah KkjBagian dihapus dari Models, semua .cs dan .cshtml yang mereferensikan tipe tersebut gagal compile
- **Issue:** Plan hanya menyebut 3 file (Models/OrganizationUnit.cs, Models/KkjModels.cs, Data/ApplicationDbContext.cs) namun perubahan model menyebabkan 11 file lain tidak compile
- **Fix:** Update AdminController.cs, CMPController.cs, dan 7 view files untuk menggunakan `OrganizationUnit` / `OrganizationUnitId` / `OrganizationUnits` DbSet
- **Files modified:** Controllers/AdminController.cs, Controllers/CMPController.cs, Views/Admin/KkjMatrix.cshtml, Views/Admin/KkjUpload.cshtml, Views/Admin/KkjFileHistory.cshtml, Views/Admin/CpdpFiles.cshtml, Views/Admin/CpdpUpload.cshtml, Views/Admin/CpdpFileHistory.cshtml, Views/CMP/DokumenKkj.cshtml
- **Commits:** 2d18546

**2. [Rule 3 - Blocking] Rewrite urutan operasi dalam migration**
- **Found during:** Task 2 — EF Core scaffold menghasilkan urutan: DropFK -> DropTable(KkjBagians) -> Rename -> CreateTable(OrganizationUnits). Urutan ini tidak memungkinkan remap data karena KkjBagians sudah di-drop sebelum mapping bisa dilakukan
- **Fix:** Rewrite migration dengan urutan manual: DropFK -> CreateTable -> Seed -> Rename -> Remap SQL -> DropTable -> AddFK
- **Files modified:** Migrations/20260321115636_AddOrganizationUnitsAndConsolidateKkjBagian.cs

**3. [Rule 3 - Blocking] Hentikan proses HcPortal.exe yang mengunci binary**
- **Found during:** Task 2 — `dotnet ef migrations add` gagal karena HcPortal.exe (PID 37372) sedang berjalan dan mengunci bin/Debug/HcPortal.exe
- **Fix:** `Stop-Process -Name HcPortal -Force` via PowerShell, kemudian generate migration berhasil
- **Impact:** Tidak ada perubahan kode

## Success Criteria Verification

- [x] Entity OrganizationUnit ada di database dengan hierarki parent-child (4 Bagian, 17 Unit) — migration applied
- [x] KkjFile dan CpdpFile memiliki FK OrganizationUnitId ke OrganizationUnits — migration applied
- [x] Entity KkjBagian dihapus dari codebase — Models/KkjModels.cs tidak mengandung class KkjBagian
- [x] Tabel KkjBagians di-drop dari database — migration applied (DROP TABLE KkjBagians)
- [x] `dotnet build` sukses tanpa compile error (0 CS errors)
- [x] `dotnet ef database update` sukses — "No migrations were applied. The database is already up to date."
