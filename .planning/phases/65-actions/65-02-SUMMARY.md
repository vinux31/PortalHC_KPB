---
phase: 65-actions
plan: 02
subsystem: cdp-proton-evidence
tags: [evidence, coaching, modal, ajax, multipart, file-upload, batch-select, ef-core]
dependency_graph:
  requires:
    - phase: 65-01
      provides: per-role approval schema, HCReviewFromProgress endpoint, tinjaModal, actionToast, showToast(), AntiForgeryToken
  provides:
    - SubmitEvidenceWithCoaching multipart AJAX endpoint
    - Combined evidence+coaching modal with batch deliverable selector
    - File upload to wwwroot/uploads/evidence/
    - CoachingSession records linked to ProtonDeliverableProgress
  affects: [ProtonDeliverableProgress, CoachingSession, CDPController, ProtonProgress view]
tech_stack:
  added: []
  patterns: [multipart-formdata-ajax, batch-checkbox-selector, in-place-row-update, stat-card-recalculation]
key_files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/ProtonProgress.cshtml
key-decisions:
  - "Do NOT set Content-Type header in fetch() — browser sets multipart boundary automatically (Pitfall 4 from RESEARCH)"
  - "File saved with timestamp prefix to avoid name collisions: {timestamp}_{originalname}"
  - "Resubmission resets SrSpvApprovalStatus + ShApprovalStatus to Pending, clearing approver fields"
  - "If no new file uploaded but EvidencePath already set, existing path is kept (coaching-only resubmission)"
  - "updateStatCards() recounts from DOM rather than server round-trip for instant feedback"
patterns-established:
  - "Batch modal pattern: JS scans table for data attributes, builds selector, pre-checks clicked item"
  - "FormData multipart: append fields individually, use 'RequestVerificationToken' header (not __RequestVerificationToken in header)"
requirements-completed: [ACTN-03, ACTN-04]
duration: ~20min
completed: 2026-02-27
---

# Phase 65 Plan 02: Actions — Evidence + Coaching Modal Summary

**Combined evidence+coaching modal with batch deliverable selector, SubmitEvidenceWithCoaching multipart endpoint creating one CoachingSession per selected deliverable with optional PDF/JPG/PNG file upload to wwwroot/uploads/evidence/.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-02-27T10:30:00Z
- **Completed:** 2026-02-27T10:50:00Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- SubmitEvidenceWithCoaching POST endpoint: validates Coach role, authorizes via CoachCoacheeMapping, creates CoachingSession per deliverable, sets Status=Submitted, resets approval columns on resubmission
- Combined evidence+coaching modal (#evidenceModal) with 7 fields: date, koacheeCompetencies, catatanCoach, kesimpulan, result, evidenceFile, plus batch deliverable selector
- Batch selector populates from table rows, groups by Kompetensi > SubKompetensi > Deliverable with nested checkboxes
- File upload: validates .pdf/.jpg/.jpeg/.png extension and 10MB size, saves to wwwroot/uploads/evidence/{firstProgressId}/{timestamp}_{filename}
- Submit Evidence buttons on Coach Pending/Rejected rows now have data attributes for modal wiring
- After submit: evidence cell updates to Sudah Upload badge, SrSpv/SH cells reset to Pending, stat cards recalculate

## Task Commits

Each task was committed atomically:

1. **Task 1: SubmitEvidenceWithCoaching endpoint + combined modal + batch selector** - `02e6c24` (feat)

## Files Created/Modified

- `Controllers/CDPController.cs` - Added SubmitEvidenceWithCoaching multipart AJAX endpoint (143 lines)
- `Views/CDP/ProtonProgress.cshtml` - Updated Submit Evidence buttons with data attrs, added #evidenceModal HTML, added all JavaScript (modal show, batch selector, submit handler, updateStatCards)

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| No Content-Type header in fetch | Browser must set multipart boundary automatically; setting it manually breaks the boundary |
| Timestamp prefix on filename | Prevents collisions when same file is uploaded multiple times |
| Keep existing EvidencePath if no new file | Coach can resubmit coaching notes without re-uploading the same evidence file |
| Approval resets on resubmission | Fresh review cycle required per CONTEXT.md — rejection should be re-evaluated with new submission |
| updateStatCards from DOM | Avoids a server round-trip; accurate enough for immediate feedback after AJAX action |

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Plan 02 complete: SubmitEvidenceWithCoaching endpoint and combined modal are fully wired
- Phase 65 Plan 03 (export functionality) can proceed — all evidence and approval data is now stored
- wwwroot/uploads/evidence/ directory is created on first upload via Directory.CreateDirectory()

## Self-Check

- [x] SubmitEvidenceWithCoaching endpoint exists in CDPController.cs with [HttpPost][ValidateAntiForgeryToken]
- [x] evidenceModal HTML in ProtonProgress.cshtml with deliverableSelector, all 7 fields
- [x] btnSubmitEvidence buttons have data-progress-id, data-deliverable, data-kompetensi, data-sub-kompetensi, data-status
- [x] File upload validates .pdf/.jpg/.jpeg/.png extension and 10MB size
- [x] FormData fetch does NOT set Content-Type header
- [x] CoachingSession created per deliverable via _context.CoachingSessions.Add
- [x] SrSpvApprovalStatus and ShApprovalStatus reset to Pending on resubmission
- [x] updateStatCards() recalculates from table DOM state
- [x] Build passes 0 errors

---
*Phase: 65-actions*
*Completed: 2026-02-27*
