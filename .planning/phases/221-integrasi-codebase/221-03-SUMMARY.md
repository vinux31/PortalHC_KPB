---
phase: 221-integrasi-codebase
plan: 03
subsystem: ui
tags: [asp-net-mvc, viewbag, organization-units, dropdown, server-validation]

# Dependency graph
requires:
  - phase: 221-01
    provides: GetSectionUnitsDictAsync, GetAllSectionsAsync, GetUnitsForSectionAsync helper methods di ApplicationDbContext
  - phase: 221-02
    provides: Pattern ViewBag.SectionUnitsJson dari ProtonDataController

provides:
  - CreateWorker GET/POST terintegrasi DB OrganizationUnits via ViewBag.SectionUnitsJson
  - EditWorker GET/POST terintegrasi DB OrganizationUnits via ViewBag.SectionUnitsJson
  - Server-side validation Section/Unit terhadap OrganizationUnit aktif di CreateWorker dan EditWorker POST
  - Dropdown Bagian/Unit di CreateWorker dan EditWorker di-populate dinamis dari DB (tidak ada hardcoded data)

affects: [222-cleanup-finalisasi, semua fase yang memakai form create/edit worker]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ViewBag.SectionUnitsJson pattern: controller get dict async → serialize → pass ke view sebagai @Html.Raw"
    - "JS dynamic dropdown: Object.keys(sectionUnits).forEach untuk populate select dari ViewBag data"
    - "currentSection restore pattern: set sectionSelect.value lalu call updateUnits untuk pre-select saat validation error return"

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml

key-decisions:
  - "Urutan JS: populate Bagian dulu, lalu set currentSection + call updateUnits — agar options sudah ada sebelum .value diset"
  - "Re-populate ViewBag di error path (CreateWorker POST dan EditWorker POST) agar view tidak crash saat validation fail"

patterns-established:
  - "Dynamic dropdown from DB: ViewBag.SectionUnitsJson → @Html.Raw → Object.keys().forEach"

requirements-completed: [INT-01, INT-02, INT-03, INT-04, INT-05, INT-06]

# Metrics
duration: 15min
completed: 2026-03-21
---

# Phase 221 Plan 03: Integrasi CreateWorker/EditWorker Dropdown + Validasi Server-Side Summary

**Dropdown Bagian/Unit CreateWorker dan EditWorker sepenuhnya dari DB OrganizationUnits via ViewBag.SectionUnitsJson, dengan validasi server-side yang menolak Section/Unit tidak valid**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-21T14:00:00Z
- **Completed:** 2026-03-21T14:15:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- CreateWorker GET diubah async, set ViewBag.SectionUnitsJson dari GetSectionUnitsDictAsync
- EditWorker GET set ViewBag.SectionUnitsJson dari GetSectionUnitsDictAsync
- CreateWorker POST dan EditWorker POST: validasi Section/Unit terhadap OrganizationUnit aktif di DB
- POST error path: re-populate ViewBag agar view tidak crash saat return dengan validation error
- Kedua view: hapus hardcoded RFCC/DHT/NGP/GAST dropdown options, ganti dengan JS populate dari ViewBag

## Task Commits

Setiap task di-commit secara atomik:

1. **Task 1: Controller — ViewBag + Validasi Server-Side** - `c5bde53` (feat)
2. **Task 2: Views — Replace Hardcoded Dropdown dengan DB Data** - `214d30f` (feat)

## Files Created/Modified

- `Controllers/AdminController.cs` - CreateWorker GET async + ViewBag, EditWorker GET ViewBag, CreateWorker/EditWorker POST validasi Section/Unit + re-populate ViewBag di error path
- `Views/Admin/CreateWorker.cshtml` - Hapus hardcoded options, populate dari @Html.Raw(ViewBag.SectionUnitsJson), restore currentSection
- `Views/Admin/EditWorker.cshtml` - Hapus hardcoded options, populate dari @Html.Raw(ViewBag.SectionUnitsJson), restore currentSection

## Decisions Made

- Re-populate ViewBag di POST error path agar view tidak crash — ini critical karena view membaca ViewBag.SectionUnitsJson dan akan throw null reference jika ViewBag kosong
- Urutan JS: populate options dulu, lalu set .value — agar select dapat menemukan option yang cocok saat pre-select

## Deviations from Plan

None — plan dieksekusi persis seperti yang tertulis.

## Issues Encountered

Build menunjukkan MSB3027 file-locking error (bukan compile error) karena app sedang berjalan saat build dijalankan. Tidak ada compile error CS. Semua kode valid.

## Next Phase Readiness

- Phase 222 (cleanup dan finalisasi) dapat berjalan — semua referensi OrganizationStructure di CreateWorker/EditWorker sudah diganti dengan DB
- Tidak ada blocker

---
*Phase: 221-integrasi-codebase*
*Completed: 2026-03-21*
