---
phase: 191-wizard-ui
plan: 02
subsystem: views
tags: [wizard, bootstrap5, javascript, razor, assessment-form]

# Dependency graph
requires:
  - "191-01 — ValidUntil property on AssessmentSession model"
provides:
  - "4-step wizard UI for CreateAssessment with nav-pills progress indicator"
  - "Per-step inline validation (is-invalid + invalid-feedback)"
  - "Konfirmasi summary step with edit-from-confirm flow"
  - "ValidUntil datepicker in Step 3 Settings"
  - "Proton mode Step 2 coachee list (existing AJAX preserved)"
affects: [191-03, 193-clone]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Multi-step wizard: single <form> + JS show/hide of .step-panel divs via d-none"
    - "WizardController IIFE encapsulating goToStep/validateStep/populateSummary/updatePills"
    - "Per-step validation using panel.querySelectorAll('.is-invalid') — avoids native form.checkValidity()"
    - "Edit-from-confirm: returnToConfirm flag + btnBackToConfirm buttons"
    - "visitedSteps Set controls pill clickability (prevents skipping validation)"

key-files:
  created: []
  modified:
    - Views/Admin/CreateAssessment.cshtml

key-decisions:
  - "ShuffleQuestions/ShuffleOptions removed from view — properties do not exist on AssessmentSession model (Rule 1 auto-fix)"
  - "protonEligibleSection shown by default in Step 2 when Proton mode; hidden when non-Proton"
  - "Schedule combiner and Proton Tahun 3 duration=0 patch in form submit capture phase (runs before wizard JS)"

patterns-established:
  - "WizardController IIFE pattern for multi-step Razor forms — reusable for future wizard phases"

requirements-completed: [FORM-01]

# Metrics
duration: ~5min
completed: 2026-03-17
---

# Phase 191 Plan 02: CreateAssessment Wizard UI Summary

**4-step wizard form for CreateAssessment with nav-pills progress indicator, per-step validation, Konfirmasi summary, and all existing JS preserved**

## Performance

- **Duration:** ~5 min
- **Completed:** 2026-03-17
- **Tasks:** 2 of 2 (all complete)
- **Files modified:** 1

## Accomplishments

- Complete rewrite of `Views/Admin/CreateAssessment.cshtml` (783 lines → 1007 lines)
- Bootstrap 5 nav-pills horizontal step indicator (`#wizardStepNav`) with 4 states: completed (green check), active (blue filled), visited (outline blue, clickable), pending (grey, disabled)
- Step 1 (Kategori): Category dropdown with `data-pass-percentage`, Title input, conditional Proton Track section
- Step 2 (Peserta): Normal user list with section filter + text search + select all/deselect all; Proton mode shows eligible coachees via AJAX
- Step 3 (Settings): Two-column layout — schedule date/time, duration, status, token toggle, PassPercentage, ExamWindowCloseDate, ValidUntil datepicker
- Step 4 (Konfirmasi): Read-only summary cards (Kategori & Judul, Peserta, Settings) each with Edit link; submit button
- `WizardController` IIFE with `goToStep`, `validateStep`, `populateSummary`, `updatePills`, `updateBackToConfirmButton`
- Edit-from-confirm flow: `returnToConfirm` flag + per-step "Kembali ke Konfirmasi" buttons
- `visitedSteps` Set prevents skipping unvisited steps
- All existing JS preserved: Proton AJAX (`GetEligibleCoachees`), token toggle, schedule combiner, section filter, success modal + toast

## Task Commits

1. **Task 1: Rewrite CreateAssessment.cshtml as 4-step wizard** — `9faf074`
   - `Views/Admin/CreateAssessment.cshtml` — complete rewrite
2. **Task 2: Verify wizard in browser** — human-verified (approved 2026-03-17)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed ShuffleQuestions/ShuffleOptions asp-for bindings**
- **Found during:** Task 1 — dotnet build
- **Issue:** Plan spec listed `asp-for="ShuffleQuestions"` and `asp-for="ShuffleOptions"` but these properties do not exist on `AssessmentSession` model, causing compile errors
- **Fix:** Removed both toggle rows from Step 3; not in original view and not in current model
- **Files modified:** Views/Admin/CreateAssessment.cshtml
- **Commit:** 9faf074

## Self-Check

- [x] `Views/Admin/CreateAssessment.cshtml` exists and is 1007 lines
- [x] `dotnet build` exits 0 (0 errors, 69 warnings — pre-existing)
- [x] All acceptance criteria passed (18/18 grep checks)
- [x] Commit 9faf074 exists

## Self-Check: PASSED

## Next Phase Readiness

- CreateAssessment wizard is complete and user-verified
- Phase 193 (Clone) can pre-fill wizard steps by targeting `#step-1`/`#step-2`/`#step-3` panels and calling `goToStep(n)` after DOM load
- EditAssessment wizard (if planned) can reuse the same WizardController IIFE pattern
