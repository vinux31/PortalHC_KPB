---
phase: 154-coaching-proton-flow-audit
plan: "02"
subsystem: coaching-proton
tags: [audit, security, authorization, evidence-upload, coaching-sessions, approval-chain]
requirements: [PROTON-02, PROTON-03, PROTON-04]

dependency_graph:
  requires: []
  provides: [154-02-AUDIT-REPORT.md]
  affects: [Controllers/CDPController.cs]

tech_stack:
  added: []
  patterns: [coach-mapping-auth, idempotency-guard]

key_files:
  created:
    - .planning/phases/154-coaching-proton-flow-audit/154-02-AUDIT-REPORT.md
  modified:
    - Controllers/CDPController.cs

decisions:
  - UploadEvidence IDOR fixed with CoachCoacheeMappings check (same pattern as SubmitEvidenceWithCoaching)
  - DownloadEvidence coach scope changed from section-equality to mapping-based (supports cross-section coaching)
  - HCReviewFromProgress idempotency guard added to match HCReviewDeliverable behavior

metrics:
  duration: "~20 minutes"
  completed_date: "2026-03-11"
  tasks_completed: 1
  files_changed: 1
---

# Phase 154 Plan 02: Evidence Upload, Coaching Sessions & Approval Chain Audit Summary

**One-liner:** Fixed Coach IDOR in UploadEvidence (missing CoachCoacheeMappings check) and added HCReviewFromProgress idempotency guard; PROTON-02/03/04 all pass.

## What Was Done

Performed a full code review of the Coaching Proton execution loop covering evidence upload (PROTON-02), coaching session creation (PROTON-03), and the L4 approval chain (PROTON-04).

## Findings and Fixes

### PROTON-02 — Evidence Upload

**Pass** with 2 fixes applied.

**Security fix (UploadEvidence IDOR):** `UploadEvidence()` verified the Coach role but did NOT verify the coach was mapped to the coachee. Any Coach could POST to `/CDP/UploadEvidence?progressId=X` for any other coachee's deliverable. Fixed by adding a `CoachCoacheeMappings` check after the role check.

**Edge-case fix (DownloadEvidence):** For coach requesters, the access check used `coachee.Section == user.Section` — a section-equality test. This would allow a coach to download evidence for any coachee in their section, not just their mapped coachees. Replaced with a `CoachCoacheeMappings.AnyAsync()` check, consistent with `Deliverable()` and `SubmitEvidenceWithCoaching()`.

Other checks are solid: file type/size validation, path traversal prevention, approval status reset on re-upload.

### PROTON-03 — Coaching Sessions

**Pass** with 1 edge-case fix.

**Edge-case fix (HCReviewFromProgress):** The AJAX endpoint lacked the idempotency guard present in `HCReviewDeliverable()`. Without it, two rapid clicks could create duplicate StatusHistory entries. Added `if (HCApprovalStatus != "Pending") return Json(error)` guard.

Session creation is Coach-only (roleLevel == 5 check), correctly links to `CoacheeId` and `ProtonDeliverableProgressId`, and always creates a new session (multiple sessions per deliverable is by design). PDF export includes all session fields (CatatanCoach, Kesimpulan, Result).

### PROTON-04 — Approval Chain

**Pass** — no issues found.

L4 role scoping enforced on all approve/reject endpoints via `HasSectionAccess()`. Section check on coachee confirmed in both AJAX and non-AJAX paths. `FilterCoachingProton()` server-side enforces `section = user.Section` for L4 users, cannot be bypassed via URL params. Rejection reason stored in `RejectionReason` field and rendered to coachee on Deliverable view. HC review is a distinct action with its own status field (`HCApprovalStatus = "Reviewed"`).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Security] UploadEvidence IDOR — missing coach-coachee mapping check**
- **Found during:** Task 1 code review
- **Issue:** Any Coach role could upload evidence for any coachee's deliverable without being mapped to them
- **Fix:** Added `CoachCoacheeMappings.AnyAsync()` check after role check in `UploadEvidence()`
- **Files modified:** `Controllers/CDPController.cs`
- **Commit:** 02897c0

**2. [Rule 2 - Security/Auth] DownloadEvidence coach access scope**
- **Found during:** Task 1 code review
- **Issue:** Coach download used section-equality check; should use mapping check for consistency and correctness
- **Fix:** Replaced section check with `CoachCoacheeMappings.AnyAsync()` for `isCoach` branch
- **Files modified:** `Controllers/CDPController.cs`
- **Commit:** 02897c0

**3. [Rule 1 - Bug] HCReviewFromProgress missing idempotency guard**
- **Found during:** Task 1 code review
- **Issue:** AJAX HC review endpoint allowed duplicate review actions; non-AJAX version had the guard
- **Fix:** Added `HCApprovalStatus != "Pending"` guard to `HCReviewFromProgress()`
- **Files modified:** `Controllers/CDPController.cs`
- **Commit:** 02897c0

## Self-Check: PASSED

- [x] `154-02-AUDIT-REPORT.md` created with PROTON-02, PROTON-03, PROTON-04 sections (7 references)
- [x] Commit 02897c0 exists (security + edge-case fixes)
- [x] Build: warnings only (pre-existing), zero errors
