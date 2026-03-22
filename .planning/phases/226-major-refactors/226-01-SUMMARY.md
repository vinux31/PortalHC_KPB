---
phase: 227-major-refactors
plan: 01
subsystem: database
tags: [cert-number, ef-migration, cleanup, assessment, cmp-controller]

# Dependency graph
requires:
  - phase: 223
    provides: SubmitExam package path dengan ExecuteUpdateAsync status claim pattern
provides:
  - CertNumberHelper shared static class (Build/ToRomanMonth/GetNextSeqAsync/IsDuplicateKeyException)
  - NomorSertifikat generation dipindah ke SubmitExam (hanya saat IsPassed=true)
  - Bad cert data di-NULL via migration
  - AssessmentCompetencyMaps dan UserCompetencyLevels tables di-drop
affects: [CMP, Admin, assessment-flow]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "CertNumberHelper static class pattern: shared cert logic diextract ke Helpers/ agar bisa digunakan lintas controller"
    - "Status-guarded cert generation: NomorSertifikat hanya di-generate setelah rowsAffected > 0 && IsPassed=true"

key-files:
  created:
    - Helpers/CertNumberHelper.cs
    - Migrations/20260322031900_CleanupCertTimingAndOrphanTables.cs
  modified:
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs
    - Data/ApplicationDbContext.cs
    - Program.cs
  deleted:
    - Data/SeedCompetencyMappings.cs
    - Models/Competency/AssessmentCompetencyMap.cs
    - Models/Competency/UserCompetencyLevel.cs

key-decisions:
  - "NomorSertifikat generation dipindah dari CreateAssessment ke SubmitExam — sessions sekarang start dengan null, cert hanya exist jika lulus"
  - "Retry loop di CreateAssessment dipertahankan tapi cert-specific logic dihapus — hanya reset Id untuk re-insert"
  - "NomorSertifikat generation ditambahkan ke KEDUA path di SubmitExam (package path + legacy path)"
  - "Migration mencakup data cleanup (NULL bad certs) DAN schema cleanup (DropTable) dalam satu operasi"

patterns-established:
  - "CertNumberHelper.Build: format KPB/{seq:D3}/{month}/{year}"
  - "Cert generation pattern: GetNextSeqAsync → Build → ExecuteUpdateAsync → retry on duplicate key"

requirements-completed: [CLEN-04, CLEN-03]

# Metrics
duration: 25min
completed: 2026-03-22
---

# Phase 227 Plan 01: Legacy Cleanup & Cert Timing Fix Summary

**NomorSertifikat dipindah dari CreateAssessment ke SubmitExam+IsPassed via shared CertNumberHelper, dan orphan tables AssessmentCompetencyMaps/UserCompetencyLevels di-drop dengan data cleanup migration**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-22T03:19:00Z
- **Completed:** 2026-03-22T03:44:00Z
- **Tasks:** 2
- **Files modified:** 7 (3 deleted, 2 created, 7 modified)

## Accomplishments
- CertNumberHelper extracted ke Helpers/CertNumberHelper.cs dengan 4 public methods (Build, ToRomanMonth, GetNextSeqAsync, IsDuplicateKeyException)
- AdminController CreateAssessment: sessions start dengan NomorSertifikat = null, private cert helpers dihapus
- CMPController SubmitExam: NomorSertifikat di-generate hanya saat IsPassed=true di KEDUA path (package + legacy)
- EF Migration applied: NULL bad cert data + DropTable AssessmentCompetencyMaps + UserCompetencyLevels
- 3 orphan files dihapus: SeedCompetencyMappings.cs, AssessmentCompetencyMap.cs, UserCompetencyLevel.cs

## Task Commits

1. **Task 1: Extract CertNumberHelper + Move NomorSertifikat timing** - `e55be72` (feat)
2. **Task 2: EF Migration — NULL bad cert data + Drop orphan tables** - `8f35d9e` (feat)

## Files Created/Modified
- `Helpers/CertNumberHelper.cs` - Shared cert number generation logic (Build, ToRomanMonth, GetNextSeqAsync, IsDuplicateKeyException)
- `Controllers/AdminController.cs` - Removed pre-computation block, set NomorSertifikat=null, replaced private methods with CertNumberHelper calls
- `Controllers/CMPController.cs` - Added cert generation block di package path + legacy path dalam SubmitExam
- `Data/ApplicationDbContext.cs` - Removed AssessmentCompetencyMaps/UserCompetencyLevels DbSets dan entity configs
- `Program.cs` - Removed SeedCompetencyMappings.SeedAsync call
- `Migrations/20260322031900_CleanupCertTimingAndOrphanTables.cs` - Migration dengan SQL UPDATE + DropTable

## Decisions Made
- NomorSertifikat generation dipindah ke SubmitExam agar selaras dengan business rule "cert hanya untuk yang lulus"
- Retry loop di CreateAssessment dipertahankan (simplified) untuk antisipasi future unique constraints lain
- NomorSertifikat generation ditambahkan ke legacy path juga karena ada legacy sessions yang masih bisa submit

## Deviations from Plan

None - plan dieksekusi sesuai spesifikasi.

## Issues Encountered
None.

## User Setup Required
None - migration sudah diapply otomatis via `dotnet ef database update`.

## Next Phase Readiness
- Phase 227 Plan 02 siap dieksekusi
- CertNumberHelper tersedia untuk digunakan oleh controller lain jika diperlukan
- Database schema sudah bersih dari orphan tables

---
*Phase: 227-major-refactors*
*Completed: 2026-03-22*
