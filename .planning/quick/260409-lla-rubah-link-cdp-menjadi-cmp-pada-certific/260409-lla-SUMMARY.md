---
phase: quick
plan: 260409-lla
subsystem: CMP
tags: [routing, certification, controller-migration]
key-files:
  created:
    - Views/CMP/CertificationManagement.cshtml
    - Views/CMP/Shared/_CertificationManagementTablePartial.cshtml
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Index.cshtml
decisions:
  - GetCascadeOptions dan GetSubCategories sudah ada di CMPController sehingga tidak perlu duplikasi
metrics:
  duration: ~10 menit
  completed: 2026-04-09
  tasks: 3
  files: 4
---

# Quick Task 260409-lla: Pindah CertificationManagement dari CDP ke CMP

**One-liner:** Semua endpoint dan view CertificationManagement dipindah dari CDPController ke CMPController sehingga URL menjadi /CMP/CertificationManagement.

## Ringkasan

Fitur Certification Management sebelumnya masih routing ke CDPController meski menu sudah ada di CMP. Task ini memindahkan backend dan frontend ke CMPController secara penuh.

## Tasks Completed

| Task | Nama | Commit | Files |
|------|------|--------|-------|
| 1 | Copy actions + helpers ke CMPController | 4679f05e | Controllers/CMPController.cs |
| 2 | Copy views ke Views/CMP/ dan update fetch URL | a8c35ad9 | Views/CMP/CertificationManagement.cshtml, Views/CMP/Shared/_CertificationManagementTablePartial.cshtml |
| 3 | Update link di CMP/Index.cshtml | f4ade2b4 | Views/CMP/Index.cshtml |

## Deviations from Plan

**1. [Rule 2 - Missing] Perbaiki breadcrumb dan link "Kembali" di view**
- **Found during:** Task 2
- **Issue:** View yang dicopy masih punya breadcrumb menunjuk CDP dan link "Kembali ke CDP"
- **Fix:** Ganti `asp-controller="CDP"` dan teks menjadi CMP
- **Files modified:** Views/CMP/CertificationManagement.cshtml

Selain itu, tidak ada deviasi lain — plan dieksekusi sesuai rencana.

## Self-Check

- [x] Views/CMP/CertificationManagement.cshtml — FOUND
- [x] Views/CMP/Shared/_CertificationManagementTablePartial.cshtml — FOUND
- [x] Controllers/CMPController.cs — FOUND (berisi CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel, BuildSertifikatRowsAsync, MapKategori)
- [x] Views/CMP/Index.cshtml — link mengarah ke CMP bukan CDP

## Self-Check: PASSED
