---
phase: 05-proton-deliverable-tracking
plan: 03
subsystem: ui
tags: [asp-net-core, razor, iformfile, file-upload, sequential-lock, proton]

# Dependency graph
requires:
  - phase: 05-01
    provides: ProtonDeliverableProgress model with EvidencePath/EvidenceFileName/Status fields
  - phase: 05-02
    provides: IWebHostEnvironment injected in CDPController, DeliverableViewModel defined in ProtonViewModels.cs
provides:
  - Deliverable GET action with sequential lock enforcement (PROTN-03)
  - UploadEvidence POST action with IFormFile handling (PROTN-04)
  - Resubmit support for rejected deliverables (PROTN-05)
  - Views/CDP/Deliverable.cshtml — evidence display, upload form, status UI
affects: [06-proton-approvals, file-upload-pattern]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IFormFile file upload: validate extension/size, Path.GetFileName for sanitization, write to wwwroot/uploads/{entity}/{id}/"
    - "Sequential lock in-memory: load all progress in one query, order by hierarchy urutan, check previous status"
    - "Resubmit pattern: same POST action handles both first upload and resubmit (Status Active or Rejected both allowed)"

key-files:
  created:
    - Views/CDP/Deliverable.cshtml
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "UploadEvidence handles both Active and Rejected status — single action covers PROTN-04 and PROTN-05"
  - "Old files kept on disk on resubmit (audit trail) — new file overwrites EvidencePath reference only"
  - "CanUpload requires RoleLevel <= 5 (coach/supervisor) — coachees cannot self-upload"
  - "Path.GetFileName strips directory separators from filename — prevents path traversal"

patterns-established:
  - "File upload to wwwroot: Path.Combine(_env.WebRootPath, dir, id, safeFileName)"
  - "Timestamp prefix on filename: DateTime.UtcNow:yyyyMMddHHmmss_ + Path.GetFileName(original)"

# Metrics
duration: 7min
completed: 2026-02-17
---

# Phase 5 Plan 03: Deliverable Detail Page Summary

**Deliverable detail page with server-side sequential lock (PROTN-03), IFormFile evidence upload (PROTN-04), and rejected-deliverable resubmit flow (PROTN-05) via single UploadEvidence POST action**

## Performance

- **Duration:** 7 min
- **Started:** 2026-02-17T~09:00Z
- **Completed:** 2026-02-17
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Sequential lock enforced server-side: Deliverable GET loads all coachee progress in one query, orders by hierarchy urutan, checks previous deliverable Approved status before granting access
- Evidence file upload: validates extension (.pdf/.jpg/.jpeg/.png) and size (10MB), sanitizes filename with timestamp prefix and Path.GetFileName, stores to wwwroot/uploads/evidence/{progressId}/
- Resubmit for rejected deliverables: same UploadEvidence POST handles both first upload (Active) and resubmit (Rejected), status returns to Submitted and RejectedAt is cleared

## Task Commits

Each task was committed atomically:

1. **Task 1: Deliverable GET with sequential lock + UploadEvidence POST** - `cb5fcd6` (feat)
2. **Task 2: Views/CDP/Deliverable.cshtml with evidence upload form** - `3d57709` (feat)

## Files Created/Modified

- `Controllers/CDPController.cs` — Added Deliverable GET (sequential lock, builds DeliverableViewModel) and UploadEvidence POST (file validation, disk write, status update)
- `Views/CDP/Deliverable.cshtml` — Deliverable detail page: breadcrumb, TempData alerts, locked/accessible states, evidence display with download, upload form (enctype=multipart/form-data), status-specific notices

## Decisions Made

- UploadEvidence handles both Active and Rejected status in a single action — covers PROTN-04 and PROTN-05 without code duplication
- Old evidence files kept on disk on resubmit — new filename stored in EvidencePath, old file remains as audit trail
- CanUpload requires RoleLevel <= 5 (coach/supervisor) — matches PROTN-04 "Coach can upload" requirement; coachees do not self-upload
- Path.GetFileName strips directory separators from the uploaded filename — prevents path traversal attacks

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- PROTN-03, PROTN-04, PROTN-05 complete — coach can upload evidence, sequential lock blocks access to future deliverables
- Phase 06 (Proton Approvals) can now implement the approval/rejection flow: approve sets Status=Approved and unlocks next deliverable; reject sets Status=Rejected and RejectedAt, allowing resubmit via existing UploadEvidence action
- wwwroot/uploads/evidence/ directory will be created automatically on first upload (Directory.CreateDirectory is idempotent)

---
*Phase: 05-proton-deliverable-tracking*
*Completed: 2026-02-17*

## Self-Check: PASSED

- FOUND: Controllers/CDPController.cs
- FOUND: Views/CDP/Deliverable.cshtml
- FOUND: 05-03-SUMMARY.md
- FOUND commit: cb5fcd6 (Deliverable GET + UploadEvidence POST)
- FOUND commit: 3d57709 (Deliverable.cshtml)
