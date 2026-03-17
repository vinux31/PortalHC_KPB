---
phase: 182-page-cdp-coachingproton-kolom-evidence-jelaskan-status-yang-ada-di-kolom-ini
plan: "01"
subsystem: CDP / CoachingProton
tags: [bugfix, evidence-column, coaching-proton, badge]
dependency_graph:
  requires: []
  provides: [correct-evidence-badge-display]
  affects: [Views/CDP/CoachingProton.cshtml, Controllers/CDPController.cs]
tech_stack:
  added: []
  patterns: [status-driven badge rendering]
key_files:
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/CoachingProton.cshtml
decisions:
  - EvidenceStatus now carries the actual workflow Status value (Pending/Submitted/Approved/Rejected) rather than a derived binary from EvidencePath presence
metrics:
  duration: ~5 minutes
  completed: 2026-03-17
  tasks_completed: 1
  files_modified: 2
---

# Phase 182 Plan 01: Evidence Column Status Badge Fix Summary

**One-liner:** Evidence column in CoachingProton now shows four status-based badges (Pending/Submitted/Approved/Rejected) derived from the Status field instead of EvidencePath presence.

## What Was Done

Fixed a display bug where the Evidence column showed "Belum Upload" even after a coach submitted evidence without attaching a file. The root cause was that `EvidenceStatus` was computed as `p.EvidencePath != null ? "Uploaded" : "Pending"` — ignoring the actual workflow Status field.

### Changes

**Controllers/CDPController.cs (line ~1483)**
- Changed `EvidenceStatus = p.EvidencePath != null ? "Uploaded" : "Pending"` to `EvidenceStatus = p.Status`
- Now carries actual workflow status through to the view

**Views/CDP/CoachingProton.cshtml (two evidence column blocks)**
- Replaced binary `Uploaded`/else pattern with four-branch status pattern
- Submitted -> "Sudah Upload" (green badge)
- Approved -> "Approved" (green bold with border)
- Rejected -> "Rejected" (red badge)
- Pending/else -> "Belum Upload" (grey badge)
- Both multi-coachee table (~line 430) and single-coachee table (~line 546) updated identically

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- `dotnet build` passed with 0 errors (69 pre-existing warnings)
- `EvidenceStatus = p.Status` confirmed in CDPController.cs
- Two occurrences each of `EvidenceStatus == "Submitted"`, `EvidenceStatus == "Approved"`, `EvidenceStatus == "Rejected"` in CoachingProton.cshtml
- `badge bg-danger` present in both evidence column contexts

## Commits

- `5e58c34` fix(182-01): derive Evidence column from Status field not EvidencePath

## Self-Check: PASSED

- Controllers/CDPController.cs: modified and committed
- Views/CDP/CoachingProton.cshtml: modified and committed
- Commit 5e58c34 exists
