---
phase: 356-audit-fix-assign-coach-coachee
plan: 01
subsystem: coaching-eligibility
tags: [coaching, eligibility, assessment, audit-fix]
requires: []
provides: [CoacheeEligibilityCalculator, GetEligibleCoachees-per-unit]
affects: [Controllers/CoachMappingController.cs]
tech-stack:
  added: []
  patterns: [static-pure-helper, per-unit-eligibility]
key-files:
  created:
    - Helpers/CoacheeEligibilityCalculator.cs
    - HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs
  modified:
    - Controllers/CoachMappingController.cs
key-decisions:
  - "AF-1 fix = eligibility per-unit coachee (expectedCount per unit), bukan total deliverable semua-unit track"
  - "Logic eligibility diekstrak ke static pure helper agar testable tanpa DbContext (D-13)"
  - "Tahun 3 (no deliverable) tetap ditangani di call-site (D-02), bukan di helper"
requirements-completed: [AF-1]
duration: ~10 min
completed: 2026-06-09
---

# Phase 356 Plan 01: AF-1 Eligibility Per-Unit Summary

Static pure `CoacheeEligibilityCalculator.IsEligiblePerUnit(statuses, expectedCount)` + 4 xUnit [Fact], dan refactor `GetEligibleCoachees` agar menghitung **expectedCount per-unit coachee** (mirror filter `.Trim()` dua-sisi dari `AutoCreateProgressForAssignment`) alih-alih membandingkan dengan total deliverable semua-unit track. Memperbaiki bug HIGH: coachee di track multi-unit (track id=4) tak pernah eligible untuk Assessment Proton.

## Tasks
- **Task 1** (`2b2c36b7`): Helper static + 4 [Fact] (full-approved/zero/partial/expectedCount-zero) — 4/4 hijau (27ms).
- **Task 2** (`959df390`): Refactor `GetEligibleCoachees` per-unit; Tahun 3 branch (D-02) dipertahankan verbatim; build 0 error.

## Verification
- `dotnet build HcPortal.csproj` → 0 error (22 warning baseline).
- `dotnet test --filter "FullyQualifiedName~CoacheeEligibilityCalculator"` → 4/4 passed.
- grep: `CoacheeEligibilityCalculator.IsEligiblePerUnit` (1), `Unit!.Trim() == resolvedUnit.Trim()` (2: GetEligibleCoachees + AutoCreateProgress), `if (!trackDeliverableIds.Any())` (1, Tahun 3), `mine.Count == trackDeliverableIds.Count` (0, comparator lama dihapus).

## Deviations from Plan
**[Rule 1 - Bug] Filter test salah di acceptance criteria** — Found during: Task 1 verify. Plan/VALIDATION acceptance memakai `--filter "FullyQualifiedName~IsEligiblePerUnit"`, tapi nama method test = behavior (FullApproved_Eligible dst), bukan mengandung "IsEligiblePerUnit" → 0 match. Fix: jalankan dengan `--filter "FullyQualifiedName~CoacheeEligibilityCalculator"` (nama class) → 4/4 passed. Tidak ada perubahan kode; hanya string filter verifikasi. Files modified: none.

**Total deviations:** 1 (filter string koreksi, non-substantif). **Impact:** nihil — semua 4 [Fact] terbukti hijau.

## Issues Encountered
None.

## Next Phase Readiness
Ready for 356-02 (AF-3/6/4). e2e track id=4 (AF-1 headline) diverifikasi di Plan 05 UAT.
