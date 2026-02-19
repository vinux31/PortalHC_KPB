---
quick: 5
title: group-manage-view-cards-by-assessment
phase: quick
plan: 5
subsystem: assessment-management
tags: [grouping, manage-view, ux, assessment, pagination]
dependency_graph:
  requires: []
  provides: [grouped-manage-cards, delete-assessment-group-endpoint]
  affects: [Views/CMP/Assessment.cshtml, Controllers/CMPController.cs]
tech_stack:
  added: []
  patterns: [in-memory-groupby, dynamic-viewbag, anonymous-type-grouping]
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Assessment.cshtml
decisions:
  - "In-memory GroupBy after EF query (not DB-level GROUP BY) — avoids EF anonymous-type projection limitations and keeps the grouping logic in C# LINQ"
  - "IEnumerable<dynamic> cast in Razor view for anonymous-type ViewBag data — typed cast would fail at runtime with anonymous types"
  - "representative session = OrderBy(CreatedAt).First() — deterministic selection for Edit/Questions routing"
  - "DeleteAssessmentGroup finds siblings by Title+Category+Schedule.Date — consistent with CreateAssessment duplicate-check and EditAssessment sibling query"
metrics:
  duration: "~3 min"
  completed: "2026-02-19"
  tasks_completed: 2
  files_modified: 2
---

# Quick Task 5: Group Manage View Cards by Assessment — Summary

**One-liner:** In-memory GroupBy(Title, Category, Schedule.Date) collapses per-user sessions into one card per assessment with compact user list and group-delete action.

## What Was Done

### Task 1 — CMPController.cs: Grouped ManagementData + DeleteAssessmentGroup action

**Assessment() action — HC/Admin branch:**

Replaced the flat paginated EF query with a two-step approach:
1. Fetch all matching sessions (no Skip/Take) with `.Include(a => a.User)`
2. Group in-memory with LINQ `GroupBy((Title, Category, Schedule.Date))`
3. For each group, project an anonymous object with: `Title`, `Category`, `Schedule`, `DurationMinutes`, `Status`, `IsTokenRequired`, `AccessToken`, `PassPercentage`, `AllowAnswerReview`, `RepresentativeId` (earliest-created session), `Users` (list of `{FullName, Email}`), `AllIds`
4. Paginate on the grouped list — `TotalCount` now reflects distinct group count
5. Changed `return View(managementData)` to `return View()` — ManagementData is in ViewBag, no typed model needed

**DeleteAssessmentGroup action (new):**
- Loads representative session by `representativeId`
- Queries all siblings matching `Title + Category + Schedule.Date`
- Deletes UserResponses, Options, Questions, and Sessions for every sibling
- Returns JSON `{success, message}` — consistent with existing `DeleteAssessment` pattern

### Task 2 — Assessment.cshtml: Group-aware card loop

- Changed `ViewBag.ManagementData as List<AssessmentSession>` to `IEnumerable<dynamic>` — anonymous types cannot be cast to typed lists
- Badge count: `.Count` property (List-specific) replaced with `.Count()` extension method
- Empty check: `.Count == 0` replaced with `!managementData.Any()`
- Replaced entire `@foreach (var item in managementData)` block with `@foreach (var group in managementData)`:
  - Header shows category badge + "N assigned" pill
  - Compact user display: `string.Join(", ", first 3 names)` + `" +N more"` if needed
  - All dynamic properties accessed with explicit casts `(string)group.Category`, `(bool)group.IsTokenRequired`, `(DateTime)group.Schedule`, `(int)group.DurationMinutes`
  - Edit and Questions buttons use `@group.RepresentativeId`
  - Delete button calls `confirmDeleteGroup(representativeId, title, userCount)`
- Added `confirmDeleteGroup` JavaScript function:
  - Double confirmation dialog mentioning user count
  - AJAX POST to `/CMP/DeleteAssessmentGroup` with `representativeId` and CSRF token
  - Page reload on success, button restore on error

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

### Files exist:
- `Controllers/CMPController.cs` — modified (contains `DeleteAssessmentGroup`)
- `Views/CMP/Assessment.cshtml` — modified (contains `confirmDeleteGroup`)

### Commits exist:
- `c7c7080` — feat(quick-5): group ManagementData in-memory + add DeleteAssessmentGroup action
- `8d0b76a` — feat(quick-5): update manage tab card loop to render grouped assessment data

### Build:
- 0 C# compilation errors (`error CS` — none found)
- 2 MSB file-lock warnings only (running app process holds HcPortal.exe — not compilation errors)

## Self-Check: PASSED
