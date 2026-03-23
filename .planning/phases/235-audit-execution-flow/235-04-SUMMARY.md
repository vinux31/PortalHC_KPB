---
phase: 235
plan: "04"
status: complete
started: 2026-03-23T03:00:00Z
completed: 2026-03-23T03:05:00Z
type: gap_closure
---

# Plan 235-04 Summary: Fix COACH_EVIDENCE_RESUBMITTED in SubmitEvidenceWithCoaching

## What Changed

`SubmitEvidenceWithCoaching` (batch endpoint) sekarang mengirim `COACH_EVIDENCE_RESUBMITTED` ke section reviewers ketika deliverable yang disubmit sebelumnya berstatus `Rejected`. Sebelumnya hanya mengirim `COACH_EVIDENCE_SUBMITTED` melalui `NotifyReviewersAsync`.

## Key Changes

1. **Track resubmit state** — `resubmitFlags` dictionary dibuat SEBELUM foreach loop mengubah status ke "Submitted" (line 2152)
2. **Resubmit notification block** — Setelah standard `NotifyReviewersAsync`, loop tambahan mengirim `COACH_EVIDENCE_RESUBMITTED` hanya untuk coachee yang punya deliverable resubmitted (lines 2221-2255)

## Key Files

- `Controllers/CDPController.cs` — lines 2152, 2221-2255

## Deviations

None.

## Self-Check: PASSED

- [x] `COACH_EVIDENCE_RESUBMITTED` appears in SubmitEvidenceWithCoaching (line 2251)
- [x] `resubmitFlags` captured before status overwrite (line 2152)
- [x] Build compiles: 0 CS errors
- [x] Pattern matches UploadEvidence reference implementation (lines 1265-1294)
