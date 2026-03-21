---
phase: 212-tipe-filter-renewal-flow-addtraining-renewal
plan: 01
subsystem: ui
tags: [renewal, filter, modal, assessment, training, bulk-renew]

# Dependency graph
requires:
  - phase: 211-perbaikan-data-display-renewal
    provides: BuildRenewalRowsAsync dengan RecordType per row, grouped view dengan checkbox
provides:
  - Filter dropdown Tipe (Assessment/Training) di RenewalCertificate view
  - Modal pilihan metode renewal untuk single renew (renewMethodModal)
  - Validasi mixed-type untuk bulk renew dengan pesan error di modal
  - Bulk method buttons (via Assessment / via Training) di bulkRenewConfirmModal
affects: [212-02, AddTraining renewal mode]

# Tech tracking
tech-stack:
  added: []
  patterns: [event-delegation untuk btn-renew-single dari partial, modal chaining via JS]

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/RenewalCertificate.cshtml
    - Views/Admin/Shared/_RenewalGroupTablePartial.cshtml

key-decisions:
  - "Single Renew tidak lagi langsung redirect ke CreateAssessment — selalu tampil modal pilihan metode"
  - "Bulk renew mixed-type diblokir di UI dengan pesan error, tombol Lanjutkan lama disembunyikan"
  - "JS event delegation digunakan untuk btn-renew-single karena button berasal dari AJAX partial"
  - "Tipe filter dikirim sebagai query param string? tipe ke kedua action FilterRenewalCertificate dan FilterRenewalCertificateGroup"

patterns-established:
  - "Modal pilihan metode: single param (renewSessionId atau renewTrainingId) tergantung recordType"
  - "Bulk renew: deteksi uniqueTypes dari checked.map(cb => cb.dataset.recordtype)"

requirements-completed: [ENH-01, ENH-02, ENH-03]

# Metrics
duration: 25min
completed: 2026-03-21
---

# Phase 212 Plan 01: Tipe Filter + Renewal Flow Summary

**Filter tipe Assessment/Training dan modal pilihan metode renewal (single + bulk) dengan blokir mixed-type di RenewalCertificate**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-21T06:20:00Z
- **Completed:** 2026-03-21T06:45:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Dropdown filter Tipe (Semua Tipe / Assessment / Training) ditambah sebelum Status di filter bar — memfilter flat dan grouped view termasuk summary cards
- Tombol Renew di _RenewalGroupTablePartial diganti dari anchor href langsung menjadi button.btn-renew-single dengan data attributes
- Modal renewMethodModal ditambah dengan dua tombol: Renew via Assessment dan Renew via Training
- Bulk renew mendeteksi mixed-type dan menampilkan alert error, serta method choice buttons untuk tipe seragam

## Task Commits

1. **Task 1: Tipe filter di controller + view + JS** - `667918c` (feat)
2. **Task 2: Modal pilihan metode renewal (single + bulk)** - `c49be7e` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` - Parameter string? tipe ditambah ke FilterRenewalCertificate dan FilterRenewalCertificateGroup, filter Enum.TryParse<RecordType>, IsFiltered check diupdate
- `Views/Admin/RenewalCertificate.cshtml` - Dropdown filter-tipe, renewMethodModal HTML, JS single-renew handler, updateBulkModalState, bulk method buttons, tipe param di refreshTable/refreshGroupTable/reset
- `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` - Anchor Renew diganti dengan button.btn-renew-single berisi data-sourceid, data-recordtype, data-judul, data-namaworker

## Decisions Made
- Single Renew tidak langsung redirect — selalu menampilkan modal pilihan metode agar admin dapat memilih renewal via Assessment atau Training tanpa tergantung RecordType sumber
- Tombol Lanjutkan lama di bulkRenewConfirmModal disembunyikan (d-none) karena digantikan oleh method buttons
- JS event delegation pada document untuk btn-renew-single karena button di-inject via AJAX partial

## Deviations from Plan

None - plan dieksekusi persis sesuai yang ditulis.

## Issues Encountered
None

## User Setup Required
None - tidak ada konfigurasi eksternal yang diperlukan.

## Next Phase Readiness
- Filter tipe dan modal pilihan metode siap digunakan
- Phase 212-02 dapat mengimplementasikan AddTraining renewal mode (pre-fill form + set FK RenewsSessionId/RenewsTrainingId)
- URL /Admin/AddTraining?renewSessionId=X dan /Admin/AddTraining?renewTrainingId=X sudah di-generate dari modal, tinggal AddTraining action menangani parameter tersebut

---
*Phase: 212-tipe-filter-renewal-flow-addtraining-renewal*
*Completed: 2026-03-21*
