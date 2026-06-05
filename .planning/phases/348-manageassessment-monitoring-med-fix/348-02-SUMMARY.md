---
phase: 348-manageassessment-monitoring-med-fix
plan: 02
subsystem: assessment-admin
tags: [pending-grading, essay, signalr, executeupdateasync, change-tracker, monitoring-detail, tdd]

# Dependency graph
requires:
  - phase: 345-assessment-pending-grade-display-fix
    provides: "AssessmentConstants.AssessmentStatus.PendingGrading + Menunggu Penilaian label baseline"
  - phase: 348-manageassessment-monitoring-med-fix
    provides: "Plan 01 Tema A (file overlap AssessmentAdminController.cs — sequential predecessor)"
provides:
  - "DeriveUserStatus static helper (PendingGrading checked first) — testable, 7 xUnit"
  - "AssessmentMonitoringDetail CompletedCount excludes ungraded essay (passRate accurate)"
  - "SignalR workerSubmitted push reloads Status from DB, branches PendingGrading (result=— + status pending)"
  - "AssessmentMonitoringDetail.cshtml handler renders pending badge from data.status"
affects: [348-04, 349, assessment-monitoring, grading]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Reload entity Status from DB after ExecuteUpdateAsync (bulk-SQL bypasses EF change-tracker)"
    - "Extract pure derivation logic to static helper for xUnit (CertificateStatus precedent)"

key-files:
  created:
    - HcPortal.Tests/MonitoringUserStatusTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
    - Controllers/CMPController.cs
    - Views/Admin/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "MAM-04 helper diekstrak ke AssessmentAdminController.DeriveUserStatus static (test project sudah ProjectReference HcPortal) — TDD RED→GREEN, 7/7 pass"
  - "MAM-05 reuse event workerSubmitted dengan status override (bukan event baru workerPendingGrading) — minimal-risk D-05"
  - "MAM-05 freshStatus reload via .Where(Id).Select(Status).FirstAsync() — no signature change ke GradeAndCompleteAsync (hindari ripple caller lain)"

patterns-established:
  - "Setelah ExecuteUpdateAsync, JANGAN baca entity tracked — reload kolom dari DB"

requirements-completed: [MAM-04, MAM-05]

# Metrics
duration: ~13 min
completed: 2026-06-05
---

# Phase 348 Plan 02: Tema B Essay PendingGrading Summary

**Essay-pending session kini tampil "Menunggu Penilaian" jujur di Monitoring Detail (badge + live SignalR) dan tak lagi inflate CompletedCount/passRate — root cause ExecuteUpdateAsync bypass change-tracker ditangani via reload status dari DB.**

## Performance

- **Duration:** ~13 min
- **Started:** 2026-06-05T00:03Z
- **Completed:** 2026-06-05T00:16Z
- **Tasks:** 3 (1 TDD RED→GREEN)
- **Files modified:** 3 + 1 test created

## Accomplishments
- **MAM-04:** Ekstrak `DeriveUserStatus(status, completedAt, startedAt)` static helper — `PendingGrading` adalah cabang PERTAMA (essay flow set `Status="Menunggu Penilaian"` + `CompletedAt` BERSAMAAN, jadi cek CompletedAt duluan salah-map "Completed"). `AssessmentMonitoringDetail` panggil helper → `CompletedCount` (`Count UserStatus=="Completed"`) auto-exclude essay-pending → passRate akurat.
- **MAM-05 controller:** `SubmitExam` reload `freshStatus` dari DB setelah `GradeAndCompleteAsync` (karena essay flow pakai `ExecuteUpdateAsync` yang bypass change-tracker — entity `assessment` stale). SignalR `workerSubmitted` push branch: essay-pending → `result="—"` + `status="Menunggu Penilaian"`, non-essay → Pass/Fail + "Completed".
- **MAM-05 view:** Handler `workerSubmitted` render badge dari `data.status` (`bg-warning text-dark` "Menunggu Penilaian" vs `bg-success` "Completed").

## Task Commits

1. **Task 1 RED: failing test DeriveUserStatus** - `faf75351` (test)
2. **Task 1 GREEN: MAM-04 helper + wiring** - `fc1c52f8` (feat)
3. **Task 2: MAM-05 SignalR reload + branch** - `40df284f` (fix)
4. **Task 3: MAM-05 view handler badge** - `08ad7628` (fix)

## Files Created/Modified
- `HcPortal.Tests/MonitoringUserStatusTests.cs` — 7 xUnit (6 Theory + 1 Fact) untuk DeriveUserStatus 6-cabang.
- `Controllers/AssessmentAdminController.cs` — static helper `DeriveUserStatus` + Detail derivation panggil helper.
- `Controllers/CMPController.cs` — reload freshStatus + SignalR push branch PendingGrading.
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — handler status cell render dari data.status.

## Decisions Made
- Helper `DeriveUserStatus` ditaruh static di `AssessmentAdminController` (bukan `AssessmentConstants`) — co-located dengan satu-satunya konsumen, test project akses via `using HcPortal.Controllers` (ProjectReference confirmed). TDD proper: RED (CS0117 helper missing) → GREEN (7/7).
- MAM-05 reuse event `workerSubmitted` + override `status` payload (bukan event baru) = minimal-risk per D-05/Claude's discretion.
- `freshStatus` reload pakai projection `.Select(s => s.Status).FirstAsync()` (lightweight, no entity load) — tak ubah signature `GradeAndCompleteAsync`.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. Build 0-error tiap task; full suite 83/83 pass (76 baseline + 7 baru, no regression).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Tema B (MAM-04/05, risiko tertinggi) tutup tanpa deviation. Pitfall ExecuteUpdateAsync ditangani.
- Ready for Plan 03 (Tema C Tab2 struktural, MAM-06/07/08/09 — empty-state/pagination/delete-filter/status-filter; MAM-06 WAJIB cek 322-UAT.md full-roster parity jangan break).
- UAT live SignalR (worker submit essay → badge real-time) + CompletedCount exclude di-defer ke Plan 05 verify-gate.
- Build hijau, tree clean.

---
*Phase: 348-manageassessment-monitoring-med-fix*
*Completed: 2026-06-05*
