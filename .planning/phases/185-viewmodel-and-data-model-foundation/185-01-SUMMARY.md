---
phase: 185-viewmodel-and-data-model-foundation
plan: 01
subsystem: database
tags: [viewmodel, enum, certification, training, assessment]

requires: []
provides:
  - CertificateStatus enum (Aktif, AkanExpired, Expired, Permanent)
  - RecordType enum (Training, Assessment)
  - SertifikatRow class with unified fields from TrainingRecord and AssessmentSession
  - DeriveCertificateStatus static method with 30-day threshold
  - CertificationManagementViewModel with Rows list and summary counts + pagination
affects:
  - 186-query-and-data-service
  - 187-list-view-and-filtering
  - 188-pagination-and-export
  - 189-integration-and-polish

tech-stack:
  added: []
  patterns:
    - "File-scoped namespace (namespace HcPortal.Models;) consistent with CDPDashboardViewModel"
    - "POCO classes, no constructor logic, default values via = new() or = 0"
    - "Static DeriveCertificateStatus helper on SertifikatRow — 30-day threshold matches TrainingRecord.IsExpiringSoon"

key-files:
  created:
    - Models/CertificationManagementViewModel.cs
  modified: []

key-decisions:
  - "DeriveCertificateStatus placed as static method on SertifikatRow (not a separate helper class) to keep types co-located"
  - "For AssessmentSession rows certificateType=null — ValidUntil==null yields Permanent per plan spec"

patterns-established:
  - "SertifikatRow.DeriveCertificateStatus: certificateType=='Permanent' OR validUntil==null → Permanent; days<0 → Expired; days<=30 → AkanExpired; else Aktif"

requirements-completed: [DATA-01, DATA-02]

duration: 5min
completed: 2026-03-18
---

# Phase 185 Plan 01: ViewModelAndDataModelFoundation Summary

**CertificateStatus/RecordType enums + SertifikatRow + CertificationManagementViewModel POCO dengan DeriveCertificateStatus 30-hari threshold, siap dipakai Phase 186-189.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-18T07:25:00Z
- **Completed:** 2026-03-18T07:30:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- Dibuat `Models/CertificationManagementViewModel.cs` dengan 4 tipe (2 enum, 2 class)
- `DeriveCertificateStatus` menggunakan threshold 30 hari konsisten dengan `TrainingRecord.IsExpiringSoon`
- Proyek compile tanpa error C# (MSB3021 file-lock bukan error kompilasi)

## Task Commits

Setiap task di-commit secara atomik:

1. **Task 1: Create CertificationManagementViewModel.cs with all types** - `1564317` (feat)

## Files Created/Modified

- `Models/CertificationManagementViewModel.cs` — RecordType, CertificateStatus, SertifikatRow, CertificationManagementViewModel

## Decisions Made

- `DeriveCertificateStatus` diletakkan sebagai static method pada `SertifikatRow` agar semua tipe tetap co-located dalam satu file
- Untuk baris AssessmentSession, `certificateType` di-pass null sehingga `ValidUntil==null` menghasilkan `Permanent` (sesuai spec plan)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

MSB3021 saat build (HcPortal.exe sedang dipakai proses lain) — bukan error kompilasi C#. Tidak ada error CS ditemukan.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Semua tipe tersedia untuk Phase 186 (query builder dan data service)
- `SertifikatRow.DeriveCertificateStatus` siap dipanggil saat mapping dari TrainingRecord/AssessmentSession ke baris viewmodel

---
*Phase: 185-viewmodel-and-data-model-foundation*
*Completed: 2026-03-18*
