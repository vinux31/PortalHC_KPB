---
phase: 346-cmp-records-detail-search-logic
plan: 05
subsystem: CMP/Records (logic + label)
tags: [ef-where, pending-grading, date-validation, label, i18n]
requires: [346-04]
provides: ["PendingGrading included in My Records + team history", "inverted date-range warning", "Assessment Lulus header"]
affects: ["Services/WorkerDataService.cs", "Views/CMP/RecordsTeam.cshtml"]
tech-stack:
  added: []
  patterns: ["constant over literal (AssessmentStatus.PendingGrading)", "ISO date string lexicographic compare"]
key-files:
  created: []
  modified: ["Services/WorkerDataService.cs", "Views/CMP/RecordsTeam.cshtml"]
key-decisions:
  - "WHERE uses AssessmentConstants.AssessmentStatus.PendingGrading constant (literal 'PendingGrading' = 0 rows)"
  - "Phase 345 label switch untouched -> pending sessions auto-render Menunggu Penilaian (IsPassed null)"
  - "REC-09 view-only header relabel; CompletedAssessments field NOT renamed (D-08)"
requirements-completed: [REC-07, REC-08, REC-09]
duration: 7 min
completed: 2026-06-04
---

# Phase 346 Plan 05: REC-07/08/09 — PendingGrading + Date-warning + Relabel Summary

Tiga perbaikan kecil: (REC-07) include sesi PendingGrading di My Records + export team history, (REC-08) warning date-range terbalik, (REC-09) perjelas header kolom Team View.

**Tasks:** 2 | **Files:** 2 | **Commits:** 2 (`4afd8af6` service, `220571b3` view)

## What was built

- **Task 1** (`4afd8af6`): `GetUnifiedRecords` (L33) + `GetAllWorkersHistory` (L136) WHERE diperluas `|| a.Status == AssessmentConstants.AssessmentStatus.PendingGrading`. Pakai konstanta (= "Menunggu Penilaian"), bukan literal. Switch label Phase 345 (L56 `null => PendingGrading`) tak disentuh → sesi pending (IsPassed null) otomatis berlabel "Menunggu Penilaian".
- **Task 2** (`220571b3`): `updateDateHint` +cabang inverted-range paling atas (early-return) `if (state.dateFrom && state.dateTo && state.dateFrom > state.dateTo)` → "Tanggal Awal lebih besar dari Tanggal Akhir — perbaiki rentang." (perbandingan string ISO valid). Header `<th>` "Assessment" → "Assessment Lulus" (view-only).

## Verification

- `dotnet build` → Build succeeded, 0 Error (2×).
- grep: konstanta PendingGrading di 2 WHERE (total 3 di file incl. switch L56) ✓ · NO literal `"PendingGrading"` ✓ · switch L56 intact ✓ · `state.dateFrom > state.dateTo` branch + warning text ✓ · `>Assessment Lulus<` ✓ · `_RecordsTeamBody.cshtml:29` masih `@worker.CompletedAssessments` (field tak di-rename) ✓.

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- 2 files modified exist ✓ · 2 commits (`git log --grep="346-05"`) ✓ · all acceptance_criteria re-run PASS ✓ · build green ✓ · L433 sibling-completion WHERE (NotifyIfGroupCompleted) correctly NOT touched ✓.

## Notes

- Excel `ExportRecordsTeamAssessment` (L694) + Personal Excel `ExportRecords` (CMPController L603, konsumsi GetUnifiedRecords) otomatis ikut include pending — tak perlu edit (sudah tolerate null IsPassed dari Phase 345).
- UAT: sesi essay pending tampil "Menunggu Penilaian" di My Records + warning interaktif date-range → 346-06 (Playwright).
- Ready for 346-06 (Wave 4, tests + UAT checkpoint — depends all). **This is the final implementation plan; 346-06 is test/UAT only.**
