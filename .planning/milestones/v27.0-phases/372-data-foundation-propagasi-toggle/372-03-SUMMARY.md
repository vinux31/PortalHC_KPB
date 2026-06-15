---
phase: 372-data-foundation-propagasi-toggle
plan: 03
subsystem: ui
tags: [razor, bootstrap, create-assessment-wizard, shuffle-toggle, form-switch, playwright-uat]

requires:
  - phase: 372-01
    provides: ShuffleQuestions/ShuffleOptions entity props (asp-for bind target)
  - phase: 372-02
    provides: controller persists the form-bound flags
provides:
  - "2 form-switch toggles (Acak Soal + Acak Pilihan Jawaban) in Create wizard Step 3 Group B, default ON from model"
  - "Educational verbatim help-text (incl. grading-safety line) per UI-SPEC"
  - "ON/OFF status rows in Step 4 summary (standard + Pre-Post) + populateSummary mirror in both branches"
affects: [373, 374, 375]

tech-stack:
  added: []
  patterns: ["form-switch toggle bound via asp-for to entity (no ViewModel); default-checked from model, never hardcoded checked"]

key-files:
  modified:
    - Views/Admin/CreateAssessment.cshtml

key-decisions:
  - "Toggles placed in Group B 'Pengaturan Ujian' beside Token (D-01), no new card"
  - "Default ON via asp-for→model (entity = true), no hardcoded checked attribute (D-08)"
  - "1 toggle pair drives both Pre and Post summary (D-04); plain-text ON/OFF status, no colored badge (D-05)"

patterns-established:
  - "Razor dynamic verified at runtime via Playwright DOM eval + screenshot (lesson Phase 354)"

requirements-completed: [SHUF-02]

duration: ~20min
completed: 2026-06-13
---

# Phase 372 Plan 03: Wizard UI Toggles Summary

**2 Bootstrap form-switch shuffle toggles (default ON) with educational Indonesian help-text added to Create Assessment wizard Step 3 Group B, plus ON/OFF status in the Step 4 confirmation summary (both standard and Pre-Post blocks)**

## Performance

- **Duration:** ~20 min
- **Tasks:** 2/2 (Task 1 auto, Task 2 UAT smoke checkpoint)
- **Files modified:** 1

## Accomplishments
- (A) New `col-md-6` in Step 3 Group B after Token: sub-heading "Pengacakan Soal & Jawaban" (`bi-shuffle`) + 2 `form-switch` (`asp-for ShuffleQuestions/ShuffleOptions`, default ON from model) + verbatim educational help-text (incl. mandatory "jawaban benar tetap dinilai dengan benar").
- (B) 2 status rows in BOTH Step 4 summary blocks (standard `summary-shuffle-*` + Pre-Post `summary-ppt-shuffle-*`).
- (C) `populateSummary()` mirror in BOTH branches (standard + isPrePost) → "Aktif (ON)"/"Nonaktif (OFF)" plain text.
- 11/11 grep acceptance checks pass; 0 hardcoded checked; 0 ViewModel; build green.
- **UAT smoke (Playwright @5277, logged in as admin):** both toggles render in Group B with full help-text, **default checked=true** (DOM eval), all 4 summary spans present, page 200 — no RuntimeBinderException. Screenshot captured.

## Task Commits

1. **Task 1: toggles + summary + populateSummary** - `47ba09d4` (feat)
2. **Task 2: UAT smoke** - no code change (browser-verified checkpoint)

## Files Created/Modified
- `Views/Admin/CreateAssessment.cshtml` — 2 toggles (Step 3 Group B) + 4 summary rows (2 blocks) + JS mirror (2 branches)

## Decisions Made
- None beyond plan — copy used verbatim from 372-UI-SPEC.

## Deviations from Plan
None - plan executed as written.

## Issues Encountered
- `populateSummary()` is function-scoped (not global) → could not invoke directly from Playwright to assert reactive Step-4 text. Mitigated: source wiring confirmed by grep (`summSQ.textContent`/`summSQP.textContent`) + summary spans confirmed present in DOM. Full reactive + Pre-Post UAT is Phase 375 scope.

## UAT Evidence
- DOM eval: `ShuffleQuestions`/`ShuffleOptions` exist, `checked === true` (default ON), in `.form-check.form-switch`; labels "Acak Soal"/"Acak Pilihan Jawaban"; help-text textContent contains verbatim grading-safety phrase; 4 summary spans present.
- Screenshot: toggles render in Group B "Pengaturan Ujian" beside Token with full help-text, no layout/runtime error.

## Next Phase Readiness
- Phase 372 (data foundation) complete: columns + migration + controller persistence/propagation + wizard UI.
- Phase 373 (read engine) and 374 (ManagePackages UI/lock) build on this. Full Playwright UAT = Phase 375.

---
*Phase: 372-data-foundation-propagasi-toggle*
*Completed: 2026-06-13*
