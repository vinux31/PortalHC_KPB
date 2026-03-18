---
phase: 186-role-scoped-data-query-helper
plan: 01
subsystem: api
tags: [cdp, certificates, role-scoping, query-helper]

# Dependency graph
requires:
  - phase: 185-certification-management-viewmodel
    provides: SertifikatRow, DeriveCertificateStatus, CertificationManagementViewModel
provides:
  - GetCurrentUserRoleLevelAsync private method in CDPController
  - BuildSertifikatRowsAsync private method in CDPController returning merged List<SertifikatRow>
affects: [187-certification-management-action]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Role-scoped query helper: scopedUserIds null=full-access, List=filtered; applied to both TrainingRecord and AssessmentSession queries
    - Post-materialization status derivation: project anonymous type via ToListAsync, then map to SertifikatRow with DeriveCertificateStatus to avoid EF translation issues

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "Post-materialization pattern for DeriveCertificateStatus — project to anonymous type first, then .Select to SertifikatRow after ToListAsync to avoid EF Core translation issues"
  - "L5 scoping includes coach own ID in coacheeIds list so coach sees own certificates alongside mapped coachees"

patterns-established:
  - "BuildSertifikatRowsAsync pattern: scopedUserIds null-or-list -> apply to both queries -> merge results"

requirements-completed: [ROLE-01, ROLE-02, ROLE-03]

# Metrics
duration: 8min
completed: 2026-03-18
---

# Phase 186 Plan 01: Role-Scoped Data Query Helper Summary

**Role-scoped certificate data helpers in CDPController — BuildSertifikatRowsAsync queries TrainingRecord + AssessmentSession with L1-6 visibility rules, returns merged List<SertifikatRow>**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-18T07:40:00Z
- **Completed:** 2026-03-18T07:48:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- GetCurrentUserRoleLevelAsync ditambahkan ke CDPController (pola identik dengan CMPController)
- BuildSertifikatRowsAsync mengquery TrainingRecord (SertifikatUrl != null) dan AssessmentSession (GenerateCertificate && IsPassed)
- Role scoping lengkap: L1-3 full access, L4 section, L5 coach mappings + self, L6 self only
- DeriveCertificateStatus dipanggil post-materialization untuk menghindari masalah translasi EF Core

## Task Commits

1. **Task 1: Add GetCurrentUserRoleLevelAsync and BuildSertifikatRowsAsync** - `dda7683` (feat)

**Plan metadata:** (docs commit menyusul)

## Files Created/Modified
- `Controllers/CDPController.cs` - Ditambahkan 132 baris: 2 private methods di akhir class CDPController

## Decisions Made
- Post-materialization pattern digunakan untuk DeriveCertificateStatus — EF Core tidak dapat mentranslasi static method dalam Select projection, jadi project ke anonymous type dulu lalu map setelah ToListAsync
- L5 (Coach): user.Id ditambahkan ke coacheeIds agar coach juga melihat sertifikat milik diri sendiri

## Deviations from Plan

None - plan executed exactly as written. Satu penyesuaian minor: anonymous type projection sebelum materialization (sudah diantisipasi di plan sebagai "if EF cannot translate" path).

## Issues Encountered
- Build warning MSB3027 (file terkunci karena aplikasi sedang berjalan) — bukan error kompilasi C#, tidak ada CS error sama sekali

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- BuildSertifikatRowsAsync siap dipanggil dari action CertificationManagement di Phase 187
- Signature: `private async Task<List<SertifikatRow>> BuildSertifikatRowsAsync()`
- Tidak ada blocker

---
*Phase: 186-role-scoped-data-query-helper*
*Completed: 2026-03-18*
