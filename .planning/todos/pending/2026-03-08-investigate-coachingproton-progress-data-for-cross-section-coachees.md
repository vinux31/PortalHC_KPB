---
created: 2026-03-08T08:15:35.353Z
title: Investigate CoachingProton progress data for cross-section coachees
area: ui
status: investigated
files:
  - Controllers/CDPController.cs:1265-1276
  - Controllers/AdminController.cs:2977-2985
---

## Problem

On the CDP/CoachingProton page, when a coach views a newly assigned cross-section coachee, the progress table already shows existing progress data even though the coachee is from a different unit and was just assigned.

## Investigation Result

**Not a bug — expected behavior.** ProtonDeliverableProgress records belong to the coachee, not the coach-coachee mapping. The query at CDPController.cs:1265-1276 filters by:
1. Active ProtonTrackAssignment for the coachee
2. Existing ProtonDeliverableProgress records for that coachee

If the coachee had progress data from a previous coach or assignment, it carries over when reassigned to a new coach. ProtonDeliverableProgress records are never created programmatically — they exist from prior workflow usage.

## Decision Needed

This is a data architecture question for the user:
- **Option A (current):** Progress follows the coachee — any coach assigned to them sees their full history
- **Option B:** Scope progress to the specific ProtonTrackAssignment, showing only deliverables for tracks assigned through the current mapping (would require adding a ProtonTrackAssignmentId FK to ProtonDeliverableProgress)

Recommend discussing with user before changing.
