---
phase: 212-tipe-filter-renewal-flow-addtraining-renewal
plan: 02
subsystem: ui
tags: [renewal, training, cmp, addtraining, fk-chain]

requires:
  - phase: 212-01
    provides: Popup pilihan renewal metode (Assessment vs Training) + tipe filter di RenewalCertificate

provides:
  - AddTraining GET menerima renewTrainingId dan renewSessionId query params
  - Prefill Judul, Kategori, UserId dari sertifikat asal (single dan bulk)
  - Banner kuning Mode Renewal di AddTraining view
  - Hidden FK inputs RenewsTrainingId/RenewsSessionId dikirim ke POST
  - POST menyimpan FK ke TrainingRecord (single dan bulk)
  - Bulk renewal: loop per-user dengan per-user FK map via JSON hidden field

affects: [RenewalCertificate, TrainingRecord, ManageAssessment]

tech-stack:
  added: []
  patterns:
    - Renewal mode via query params prefill model + ViewBag pattern (sama dengan CreateAssessment)
    - Bulk FK map via JSON hidden input, deserialized di POST
    - JS DOM manipulation untuk prefill select dan sembunyikan field saat bulk mode

key-files:
  created: []
  modified:
    - Models/CreateTrainingRecordViewModel.cs
    - Controllers/AdminController.cs
    - Views/Admin/AddTraining.cshtml

key-decisions:
  - "Prefill Peserta via JS (bukan server-side select) karena model binding select menggunakan asp-for"
  - "Bulk mode menyembunyikan field UserId tunggal dan menampilkan info jumlah peserta"
  - "ModelState invalid tetap restore ViewBag IsRenewalMode agar banner tidak hilang saat validasi gagal"

patterns-established:
  - "AddTraining renewal mode: query params → DB query → prefill model + ViewBag → View"
  - "Bulk renewal POST: parse JSON UserIds + fkMap → loop per user → SaveChangesAsync"

requirements-completed: [ENH-04, FIX-04]

duration: 15min
completed: 2026-03-21
---

# Phase 212 Plan 02: AddTraining Renewal Mode Summary

**AddTraining mendukung single dan bulk renewal mode dengan prefill Judul/Kategori/Peserta, banner kuning, dan FK RenewsTrainingId/RenewsSessionId tersimpan ke TrainingRecord.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-21T06:25:00Z
- **Completed:** 2026-03-21T06:40:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- CreateTrainingRecordViewModel mendapat field RenewsTrainingId dan RenewsSessionId
- AddTraining GET menerima renewTrainingId/renewSessionId params, query DB, prefill model dan ViewBag untuk single dan bulk
- View menampilkan banner kuning Mode Renewal dengan sumber title dan user name
- Hidden FK inputs dikirim via form POST, POST handler menyimpan FK ke TrainingRecord
- Bulk renewal: POST membuat N TrainingRecord dalam satu loop dengan FK per-user dari JSON map

## Task Commits

1. **Task 1: ViewModel FK fields + AddTraining GET renewal mode** - `2e837d9` (feat)
2. **Task 2: AddTraining view renewal UI + POST FK assignment + bulk multi-user** - `e0b4b05` (feat)

## Files Created/Modified
- `Models/CreateTrainingRecordViewModel.cs` - Tambah RenewsTrainingId dan RenewsSessionId
- `Controllers/AdminController.cs` - AddTraining GET renewal mode + POST bulk loop + FK assignment
- `Views/Admin/AddTraining.cshtml` - Banner kuning, hidden FK inputs, JS prefill peserta

## Decisions Made
- Prefill Peserta via JS DOM manipulation (bukan server-side model binding) karena asp-for select tidak bisa di-override dari ViewBag
- Bulk mode menyembunyikan field UserId tunggal dan menampilkan info jumlah peserta
- ModelState invalid handler me-restore ViewBag IsRenewalMode dan RenewalSource agar banner tidak hilang

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Renewal chain via AddTraining lengkap: RenewalCertificate → popup pilihan → AddTraining renewal mode → TrainingRecord dengan FK
- Phase 212 selesai — semua requirements v7.10 (ENH-01/02/03/04 + FIX-04) terpenuhi

---
*Phase: 212-tipe-filter-renewal-flow-addtraining-renewal*
*Completed: 2026-03-21*
