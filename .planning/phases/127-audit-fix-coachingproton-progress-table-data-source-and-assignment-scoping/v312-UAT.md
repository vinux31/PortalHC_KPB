---
status: complete
phase: v3.12-progress-unit-scoping
source: 127-01-SUMMARY.md, 127-02-SUMMARY.md, 127-03-SUMMARY.md, 128-01-SUMMARY.md, 129-01-SUMMARY.md
started: 2026-03-09T00:00:00Z
updated: 2026-03-09T00:01:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Auto-create progress on coach-coachee assignment
expected: When HC assigns a coach-coachee mapping, progress rows are auto-created for all deliverables in the assigned track. CoachingProton page shows progress with "Belum Mulai" status for each deliverable.
result: pass

### 2. CoachingProton shows only coachees with active assignments
expected: CoachingProton page only shows coachees who have an active ProtonTrackAssignment. Users without assignments do not appear in the coachee list, even if they are Level 6 workers.
result: pass

### 3. Edit mapping to change track — progress rebuilt
expected: When HC edits a coach-coachee mapping to change the track, old progress/sessions are cleaned up and new progress rows are created for the new track's deliverables.
result: pass

### 4. Add deliverable in silabus — progress auto-synced
expected: When Admin adds a new deliverable to a silabus track (SilabusSave), progress rows are automatically created for all active assignments on that track. New deliverable appears in CoachingProton progress table.
result: pass

### 5. Delete deliverable in silabus — cascade cleanup
expected: When Admin deletes a deliverable from silabus, associated progress records and coaching sessions are cleaned up. No FK errors occur. The deleted deliverable disappears from CoachingProton progress.
result: pass

### 6. Unit-scoped progress — only matching unit deliverables
expected: Progress is only created for deliverables whose ProtonKompetensi.Unit matches the assignment's resolved unit (AssignmentUnit or fallback User.Unit). A coachee assigned to "Alkylation" only sees Alkylation deliverables, not RFCC NHT deliverables.
result: pass

### 7. Edit mapping unit change — progress rebuilt for new unit
expected: When HC edits a mapping and changes AssignmentUnit, old progress is cleaned up and new progress is created filtered by the new unit. CoachingProton page reflects the new unit's deliverables only.
result: pass

### 8. Defensive unit filter on CoachingProton/Dashboard
expected: Even if stale cross-unit progress exists in DB, CoachingProton page and CDP Dashboard defensively filter it out. Only unit-matching progress rows are displayed.
result: pass

## Summary

total: 8
passed: 8
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
