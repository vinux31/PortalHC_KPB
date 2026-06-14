---
phase: 354-render-gambar-di-6-layar
plan: 04
subsystem: assessment-admin-controller
tags: [populate, viewmodel-mapping, image-data, admin-surface]
requires: [02]
provides:
  - essay grading populate gambar soal (soal saja)
  - EditPesertaAnswers populate gambar soal+opsi
affects:
  - Views/Admin/AssessmentMonitoringDetail.cshtml (Plan 06 render)
  - Views/Admin/EditPesertaAnswers.cshtml (Plan 06 render)
tech-stack:
  added: []
  patterns: [entity-to-vm-scalar-mapping]
key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "EssayGradingItemViewModel populate gambar SOAL SAJA (essay tak punya opsi, RND-05/L-01)"
  - "essayQs query full-entity (no projection) → ImagePath/ImageAlt scalar auto-load; EditPesertaAnswers Include(q.Options) confirmed L2963"
requirements-completed: []
duration: 4 min
completed: 2026-06-09
---

# Phase 354 Plan 04: Populate Gambar 2 Loop AssessmentAdminController Summary

Mengisi data gambar di 2 populate loop `AssessmentAdminController.cs` — essay grading (soal saja) + EditPesertaAnswers (soal + opsi). View admin (Plan 06) kini punya data render.

**Tasks:** 2 | **Files:** 1 modified | **Duration:** ~4 min

## What was built

- **Essay grading (~L3397)** — `EssayGradingItemViewModel` dapat `ImagePath/ImageAlt` soal saja (RND-05). Tidak tambah field opsi.
- **EditPesertaAnswers (~L2986)** — `EditQuestionRow` (q) soal + `EditOptionRow` (o) opsi dapat ImagePath/ImageAlt (RND-06).

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- Grep AssessmentAdminController: `ImagePath = q.ImagePath`≥2 (essay + EditQuestionRow), `ImagePath = o.ImagePath`≥1 (EditOptionRow).
- Include(q.Options) confirmed L2963 (EditPesertaAnswers); essayQs full-entity scalar auto-load.
- `dotnet build` → **Build succeeded** (0 error).

## Issues Encountered

None.

## Next Phase Readiness

Data gambar admin lengkap di VM. Requirements RND-05/06 di-mark complete saat render Plan 06. Ready for Wave 3 (Plan 06 render view admin — checkpoint Playwright).

## Self-Check: PASSED
