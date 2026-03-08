---
created: 2026-03-08T08:15:35.353Z
title: Investigate CoachingProton progress data for cross-section coachees
area: ui
files:
  - Controllers/CDPController.cs
  - Views/CDP/CoachingProton.cshtml
---

## Problem

On the CDP/CoachingProton page, when a coach views a newly assigned cross-section coachee, the progress table already shows existing progress data even though the coachee is from a different unit and was just assigned. The data source for the progress table may be pulling progress from all coachees rather than scoping correctly to the coach-coachee mapping context.

## Solution

TBD — investigate the CoachingProton action's data query to understand how progress rows are fetched. Check whether progress is tied to ProtonTrackAssignment (which would be empty for new assignments) or to some other source. May need to scope progress display to only show data for tracks assigned through the current coach-coachee mapping.
