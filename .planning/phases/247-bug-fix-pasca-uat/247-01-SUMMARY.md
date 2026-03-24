---
phase: 247-bug-fix-pasca-uat
plan: 01
subsystem: CMP Assessment
tags: [bug-fix, et-distribution, uat]
dependency_graph:
  requires: []
  provides: [balanced-et-distribution]
  affects: [exam-question-selection, radar-chart-accuracy]
tech_stack:
  added: []
  patterns: [round-robin-distribution, fallback-pool]
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
decisions:
  - "Phase 2 BuildCrossPackageAssignment diganti dari per-package ke per-ET round-robin distribution"
  - "BUG-02 COACH_EVIDENCE_RESUBMITTED verified correct — no fix needed"
metrics:
  duration: 5m
  completed: "2026-03-24T11:36:00Z"
  tasks_completed: 2
  files_modified: 1
---

# Phase 247 Plan 01: Fix ET Distribution + Verify BUG-02 Summary

Round-robin per-ET distribution menggantikan per-package distribution di BuildCrossPackageAssignment Phase 2, memastikan radar chart ET representatif dengan gap max 1 soal antar ET.

## Tasks Completed

### Task 1: Fix ET Distribution Phase 2 (D-01)
- **Commit:** `820018c2` — fix(247-01): fix ET distribution Phase 2
- **Changes:** Replaced per-package slot distribution with per-ET round-robin: `basePerET = remaining / M`, extra soal random ke subset ET, fallback pool untuk NULL-ET
- **Files:** Controllers/CMPController.cs (28 insertions, 39 deletions)
- **Build:** Passed (0 errors)

### Task 2: Verify BUG-02 + REQUIREMENTS.md (D-03, D-06)
- **Commit:** None (verification only, no code changes needed)
- **BUG-02 Result:** VERIFIED CORRECT
  - `resubmitFlags` populated at line 2195 BEFORE status change at line 2207
  - `COACH_EVIDENCE_RESUBMITTED` sent only for previously-Rejected deliverables (line 2267)
  - Notification type string exact match (line 2295)
- **REQUIREMENTS.md:** SETUP-01 and SETUP-02 already `[x]` with `Complete` in traceability table

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED
