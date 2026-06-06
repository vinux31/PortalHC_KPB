---
phase: 348-manageassessment-monitoring-med-fix
plan: 01
subsystem: assessment-admin
tags: [pre-post, linkedgroupid, regenerate-token, export-excel, bulk-pdf, monitoring-badge, razor]

# Dependency graph
requires:
  - phase: 345-assessment-pending-grade-display-fix
    provides: "IsMenungguPenilaian computed property + Menunggu Penilaian label baseline (MAM-03 badge konsumen)"
provides:
  - "RegenerateToken Pre-Post route-by-LinkedGroupId (PreTest + PostTest token sama walau beda tanggal)"
  - "ExportAssessmentResults + BulkExportPdf optional int? linkedGroupId → query both-half"
  - "_AssessmentGroupsTab dropdown Aksi Pre-Post: Monitoring→list both-half + Export/PDF kirim linkedGroupId"
  - "prePostGroups MonitoringGroupViewModel assign MenungguPenilaianCount dari postSubs (badge pending muncul)"
affects: [348-04, 349, assessment-monitoring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Pre-Post group routing by LinkedGroupId (bukan single Schedule.Date) — PostTest boleh beda tanggal"
    - "Optional int? linkedGroupId param backward-compatible: non-null→both-half, null→fallback Schedule.Date"

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml

key-decisions:
  - "MAM-02 Monitoring link Pre-Post → AssessmentMonitoring list (search=Title) reuse view both-half existing, hindari single-date filter (D-01)"
  - "MAM-02 Export/PDF pakai optional int? linkedGroupId — backward-compatible, grup standard call-site default null"
  - "MAM-01 sibling-selection inline EF query (tak diekstrak helper — tak trivial tanpa DB); xUnit comprehensive deferred ke Plan 05"

patterns-established:
  - "Route Pre-Post by LinkedGroupId untuk semua operasi grup (token/export/monitoring), bukan asumsi same-date"

requirements-completed: [MAM-01, MAM-02, MAM-03]

# Metrics
duration: ~10 min
completed: 2026-06-05
---

# Phase 348 Plan 01: Tema A Pre-Post Group Consistency Summary

**RegenerateToken/Export/PDF/badge untuk grup Pre-Post kini sadar LinkedGroupId — PostTest beda-tanggal tak lagi silently ke-miss; token regenerate ke kedua half, export both-half, badge "X belum dinilai" muncul.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-06-04T23:50Z
- **Completed:** 2026-06-05T00:00Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- **MAM-01:** `RegenerateToken` siblings query route-by-`LinkedGroupId` saat Pre-Post → PreTest + PostTest dapat token baru sama walau jadwal beda tanggal; grup standard fallback `Title+Category+Schedule.Date` (tak berubah).
- **MAM-02:** `ExportAssessmentResults` + `BulkExportPdf` terima `int? linkedGroupId` opsional → query both-half (Pre+Post); view `_AssessmentGroupsTab` wiring: link Monitoring Pre-Post → list both-half, Export/PDF kirim `linkedGroupId` + label "(Pre & Post)".
- **MAM-03:** `prePostGroups` `MonitoringGroupViewModel` assign `MenungguPenilaianCount = postSubs.Count(a => a.IsMenungguPenilaian)` (parity `standardGroups` L2825) → badge "X belum dinilai" muncul untuk SELURUH grup Pre-Post.

## Task Commits

1. **Task 1: MAM-01 RegenerateToken + MAM-03 badge count** - `a1fd1a26` (fix)
2. **Task 2: MAM-02 Export/PDF LinkedGroupId-aware** - `222231a1` (fix)
3. **Task 3: MAM-02 view wiring** - `9ae312ea` (fix)

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` — RegenerateToken siblings LinkedGroupId branch + ExportAssessmentResults/BulkExportPdf `int? linkedGroupId` param + prePostGroups MenungguPenilaianCount.
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — dropdown Aksi: Pre-Post Monitoring→list, Export/PDF kirim linkedGroupId + suffix label.

## Decisions Made
- MAM-02 Monitoring link Pre-Post diarahkan ke `AssessmentMonitoring` **list** (search=Title) yang sudah render both-half (preDetailUrl/postDetailUrl L337-388), bukan ke `AssessmentMonitoringDetail` per-date — menghindari single-date filter yang miss PostTest beda tanggal (D-01).
- Verifikasi `AssessmentMonitoring(string? search,...)` punya param `search` SEBELUM wiring (Url.Action silent-append, build tak nangkap salah-nama route value).
- TDD MAM-01: sibling-selection adalah EF query terhadap DbContext, tak trivial diekstrak ke pure helper tanpa DB — biarkan inline, xUnit comprehensive di Plan 05 (diizinkan plan).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. Build 0-error semua 3 task. Satu commit subject typo (`348-02 wip`) di Task 2 diperbaiki via `git commit --amend` jadi `fix(348-01)` sebelum lanjut (cosmetic, pre-push).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Tema A Pre-Post (MAM-01/02/03) tutup. Ready for Plan 02 (Tema B essay PendingGrading, MAM-04/05 — **ISOLASI, risiko tertinggi**; WAJIB baca 348-PATTERNS.md §MAM-05 ExecuteUpdateAsync pitfall sebelum mulai).
- UAT smoke (token both-half di DB, export Pre+Post, badge) di-defer ke Plan 05 verify-gate.
- Build hijau, tree clean.

---
*Phase: 348-manageassessment-monitoring-med-fix*
*Completed: 2026-06-05*
