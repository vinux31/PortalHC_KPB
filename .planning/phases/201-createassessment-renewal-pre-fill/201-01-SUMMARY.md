---
phase: 201-createassessment-renewal-pre-fill
plan: 01
subsystem: api
tags: [asp.net-core, razor, viewbag, renewal, pre-fill]

requires:
  - phase: 200-renewal-chain-fk
    provides: RenewsSessionId/RenewsTrainingId columns on AssessmentSession and TrainingRecord
provides:
  - CreateAssessment GET accepts renewSessionId/renewTrainingId query params with pre-fill
  - CreateAssessment POST saves renewal FK and validates ValidUntil required
  - Renewal banner with cancel button in view
affects: [202-renew-button-ui, 203-certificate-history]

tech-stack:
  added: []
  patterns: [ViewBag.IsRenewalMode boolean flag for conditional rendering, hidden input FK binding]

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CreateAssessment.cshtml

key-decisions:
  - "Renewal FK assigned only to first session (i==0) in multi-user create — renewal is 1-to-1"
  - "ValidUntil validated via application code after ModelState.Remove — not via data annotation"

patterns-established:
  - "Renewal mode detection: ViewBag.IsRenewalMode boolean controls all conditional UI"
  - "Hidden input pattern for FK POST binding with ViewBag null check"

requirements-completed: [RENEW-03]

duration: 4min
completed: 2026-03-19
---

# Phase 201 Plan 01: CreateAssessment Renewal Pre-fill Summary

**CreateAssessment GET/POST renewal-aware dengan query param pre-fill, banner Mode Renewal, dan ValidUntil required validation**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-19T06:11:54Z
- **Completed:** 2026-03-19T06:15:44Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- CreateAssessment GET menerima renewSessionId/renewTrainingId, pre-fill Title/Category/peserta/GenerateCertificate/ValidUntil
- POST menyimpan RenewsSessionId/RenewsTrainingId ke AssessmentSession dengan XOR validation
- Banner alert-info Mode Renewal dengan tombol Batalkan Renewal
- ValidUntil required marker (*) di renewal mode dengan server-side validation

## Task Commits

Each task was committed atomically:

1. **Task 1: Controller GET/POST renewal-aware logic** - `1191e0f` (feat)
2. **Task 2: View renewal banner, hidden fields, conditional ValidUntil** - `59c9db6` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` - GET query param handling, renewal pre-fill, POST validation, FK save
- `Views/Admin/CreateAssessment.cshtml` - Renewal banner, hidden fields, ValidUntil required marker, SubCategory JS trigger

## Decisions Made
- Renewal FK assigned only to first session (i==0) in multi-user loop — renewal is 1-to-1 relationship
- ValidUntil validation: ModelState.Remove always runs, then custom AddModelError if renewal mode and null — avoids order-of-operations pitfall

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CreateAssessment renewal mode fully functional via manual URL entry
- Ready for Phase 202: Renew button UI on certificate list pages

---
*Phase: 201-createassessment-renewal-pre-fill*
*Completed: 2026-03-19*
