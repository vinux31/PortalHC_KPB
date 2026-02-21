---
phase: 29-auto-transition-upcoming-to-open
plan: "03"
subsystem: Assessment UI
tags: [views, forms, datetime, schedule, time-picker]
dependency_graph:
  requires: [29-02-PLAN.md]
  provides: [schedule-time-input-create, schedule-time-input-edit, upcoming-time-display]
  affects: [Views/CMP/CreateAssessment.cshtml, Views/CMP/EditAssessment.cshtml, Views/CMP/Assessment.cshtml]
tech_stack:
  added: []
  patterns: [hidden-field-JS-combine, date-time-split-inputs, razor-conditional-display]
key_files:
  created: []
  modified:
    - Views/CMP/CreateAssessment.cshtml
    - Views/CMP/EditAssessment.cshtml
    - Views/CMP/Assessment.cshtml
decisions:
  - "ScheduleDate and ScheduleTime are plain inputs (not asp-for); ScheduleHidden is the asp-for binding populated by JS before submit — avoids DateTime ISO string breaking date input pre-population"
  - "Always-running combine IIFE added to EditAssessment in addition to the package-warning IIFE (which returns early when packageCount=0)"
  - "Upcoming button replaced day-countdown arithmetic (.Days) with exact datetime string — countdown was inaccurate intra-day"
metrics:
  duration: "2min"
  completed: "2026-02-21"
  tasks: 2
  files: 3
---

# Phase 29 Plan 03: Schedule Time Picker and Upcoming Time Display Summary

**One-liner:** Date+time picker pair added to Create/Edit Assessment forms with JS-combine hidden field; Upcoming worker cards now display exact opening datetime in WIB.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Add time picker to CreateAssessment and EditAssessment forms | 1a92c39 | CreateAssessment.cshtml, EditAssessment.cshtml |
| 2 | Update Upcoming assessment display to show opening time in worker list | 2792854 | Assessment.cshtml |

## What Was Built

### CreateAssessment.cshtml
- Schedule section replaced: date-only `<input type="date" asp-for="Schedule">` replaced with a side-by-side pair: `#ScheduleDate` (type="date", no asp-for) + `#ScheduleTime` (type="time", default "08:00")
- `#ScheduleHidden` is the actual `asp-for="Schedule"` field (hidden), populated by JS before submit
- JS combine block inserted into the existing submit handler: `schedHidden.value = schedDateInput.value + 'T' + (schedTimeInput.value || '08:00') + ':00'`
- Schedule validation updated to check `schedDateInput.value` (not `scheduleField.value`)

### EditAssessment.cshtml
- Same date+time picker structure added, pre-populated from `@Model.Schedule.ToString("yyyy-MM-dd")` and `@Model.Schedule.ToString("HH:mm")`
- `#ScheduleHidden` pre-populated with full ISO string from model
- Package-warning IIFE updated: reads `ScheduleDate` (not `Schedule`) for date comparison; combine block runs inside it (covers packageCount > 0 case)
- New always-running combine IIFE added before Bootstrap validation IIFE (covers packageCount = 0 case)
- `originalSchedule` comparison uses date portion from `ScheduleDate` — time change alone does not trigger package-reassignment warning

### Assessment.cshtml
- Date meta-item updated: Upcoming assessments show `Opens DD MMM YYYY, HH:mm WIB`; other statuses unchanged
- Disabled Upcoming button updated: shows `Opens DD MMM YYYY, HH:mm WIB` — removes `.Days` day-countdown arithmetic that showed inaccurate results intra-day

## Decisions Made

1. **Split inputs, hidden binding** — `ScheduleDate` + `ScheduleTime` are plain inputs without `asp-for` to avoid Razor rendering a full ISO DateTime string into a date input (which browsers reject). The hidden `ScheduleHidden` carries the `asp-for` binding and receives the combined value via JS before submit.

2. **Dual combine IIFEs in EditAssessment** — The package-warning IIFE returns early when `packageCount=0`, so a second always-running IIFE ensures the hidden field is always populated correctly regardless of package count.

3. **Remove day-countdown from Upcoming button** — The old `(item.Schedule - DateTime.Now).Days` showed "Available in 0 days" on the scheduled day before the opening hour. Replacing with the exact datetime string is always accurate and consistent with the meta-item display.

## Deviations from Plan

None — plan executed exactly as written, with one addition: an always-running combine IIFE was added to EditAssessment to cover the `packageCount=0` case (the plan's combine block was placed inside the package-warning IIFE which returns early at `packageCount <= 0`). This is a correctness fix (Rule 2).

**[Rule 2 - Missing Critical Functionality] EditAssessment combine block unreachable when packageCount=0**
- **Found during:** Task 1
- **Issue:** Plan placed combine block inside the package-warning IIFE which returns early when `packageCount <= 0`, meaning the hidden schedule field would never be populated for assessments with no packages
- **Fix:** Added a separate always-running IIFE that performs the combine unconditionally; the package-warning IIFE still does its own combine when `packageCount > 0` (idempotent)
- **Files modified:** Views/CMP/EditAssessment.cshtml
- **Commit:** 1a92c39

## Self-Check

```
Views/CMP/CreateAssessment.cshtml — ScheduleTime: FOUND
Views/CMP/CreateAssessment.cshtml — ScheduleHidden: FOUND
Views/CMP/EditAssessment.cshtml — ScheduleTime: FOUND
Views/CMP/EditAssessment.cshtml — ScheduleHidden: FOUND
Views/CMP/Assessment.cshtml — WIB (×2): FOUND
Views/CMP/Assessment.cshtml — 'Available in': NOT FOUND (pass)
Build: 0 errors
Commit 1a92c39: FOUND
Commit 2792854: FOUND
```

## Self-Check: PASSED
