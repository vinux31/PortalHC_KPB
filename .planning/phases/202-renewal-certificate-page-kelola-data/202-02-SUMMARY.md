---
phase: 202-renewal-certificate-page-kelola-data
plan: "02"
subsystem: ui
tags: [asp.net-core, mvc, razor, admin, badge-count, certificate]

requires:
  - phase: 202-01
    provides: BuildRenewalRowsAsync dan RenewalCertificate GET/Filter actions di AdminController

provides:
  - Card "Renewal Sertifikat" di Section C Kelola Data dengan badge count server-rendered
  - ViewBag.RenewalCount lightweight query di AdminController.Index()

affects:
  - 202-03 (jika ada plan lanjutan renewal certificate page)

tech-stack:
  added: []
  patterns:
    - "Lightweight badge count query di Index action: 4 query DB (renewedSessionIds, renewedTrainingIds, expiredTrainingCount, expiredAssessmentCount)"
    - "Index action diubah ke async untuk mendukung query DB"

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml

key-decisions:
  - "Lightweight query dipakai di Index() — bukan BuildRenewalRowsAsync yang mahal — karena badge hanya perlu count"
  - "Index() diubah dari sync ke async Task<IActionResult> untuk mendukung query DB"
  - "Badge hanya ditampilkan jika RenewalCount > 0 untuk menghindari badge 0 yang tidak informatif"

patterns-established:
  - "Badge count di hub card menggunakan ViewBag dengan lightweight count query, bukan memanggil full data builder"

requirements-completed: [RNPAGE-05]

duration: 10min
completed: 2026-03-19
---

# Phase 202 Plan 02: Renewal Certificate Card di Kelola Data Summary

**Card Renewal Sertifikat dengan server-rendered badge count via 4 query ringan di AdminController.Index(), tampil di Section C Kelola Data**

## Performance

- **Duration:** 10 min
- **Started:** 2026-03-19T08:00:00Z
- **Completed:** 2026-03-19T08:10:00Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- AdminController.Index() diubah dari sync ke async dengan 4 query ringan untuk badge count
- ViewBag.RenewalCount tersedia di Index view dengan nilai jumlah sertifikat expired/akan expired yang belum di-renew
- Card "Renewal Sertifikat" ditambahkan di Section C dengan icon bi-arrow-repeat text-warning dan badge dinamis
- Build berhasil tanpa error (71 warning pre-existing, 0 error baru)

## Task Commits

1. **Task 1: Card Renewal Sertifikat di Section C + badge count query di Index** - `e882294` (feat)

**Plan metadata:** akan dibuat setelah SUMMARY.md

## Files Created/Modified

- `Controllers/AdminController.cs` - Index() diubah ke async, ditambah 4 query lightweight renewal count + ViewBag.RenewalCount
- `Views/Admin/Index.cshtml` - Ditambah card Renewal Sertifikat di Section C dengan badge dan link ke /Admin/RenewalCertificate

## Decisions Made

- Lightweight query (CountAsync saja) dipakai di Index() agar tidak membebani hub page dengan full BuildRenewalRowsAsync
- Index action diubah ke async — diperlukan untuk mendukung await query DB
- Badge hanya muncul jika count > 0 per spesifikasi plan

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Card Renewal Sertifikat sudah tersedia di Kelola Data Section C
- Link ke /Admin/RenewalCertificate sudah terpasang
- Siap untuk pengujian browser manual: navigasi ke /Admin → verifikasi card muncul di Section C

---
*Phase: 202-renewal-certificate-page-kelola-data*
*Completed: 2026-03-19*
