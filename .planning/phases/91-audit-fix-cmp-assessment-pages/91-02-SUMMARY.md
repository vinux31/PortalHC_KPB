---
phase: 91-audit-fix-cmp-assessment-pages
plan: "02"
subsystem: CMP Assessment Views
tags: [navigation, records, csrf, exam-resilience, option-shuffle, ui]
dependency_graph:
  requires: [91-01]
  provides: [cmp-view-layer-fixes]
  affects: [Views/CMP/Results.cshtml, Views/CMP/Certificate.cshtml, Views/CMP/Records.cshtml, Views/CMP/Assessment.cshtml, Views/CMP/StartExam.cshtml, Controllers/CMPController.cs]
tech_stack:
  added: []
  patterns: [returnUrl query param for back navigation, Bootstrap 2-tab layout, exponential backoff retry, Bootstrap modal for notifications]
key_files:
  created: []
  modified:
    - Views/CMP/Results.cshtml
    - Views/CMP/Certificate.cshtml
    - Views/CMP/Records.cshtml
    - Views/CMP/Assessment.cshtml
    - Views/CMP/StartExam.cshtml
    - Controllers/CMPController.cs
decisions:
  - returnUrl query param chosen for back button navigation (simple, no server-side session changes needed)
  - 3-attempt retry: attempt 0=immediate (first try), attempt 1 retries after 1s, attempt 2 retries after 3s
  - Force-close modal uses 10s auto-redirect fallback if user doesn't click OK
  - ViewBag.OptionShuffle variable renamed from optionShuffleDict to parsedOptionShuffle to avoid CS0136 scope conflict
metrics:
  duration: "4 min"
  completed: "2026-03-04"
  tasks: 3
  files: 6
---

# Phase 91 Plan 02: CMP Assessment View-Layer Fixes Summary

CMP Assessment view-layer bugs fixed: returnUrl back-button navigation for Results/Certificate, Records page redesigned with 2-tab layout and clickable assessment rows, VerifyToken CSRF headers added, auto-save 3-attempt retry with exponential backoff, force-close modal replacing silent banner, and option shuffle rendering in StartExam.

## Tasks Completed

| # | Task | Commit | Files Modified |
|---|------|--------|----------------|
| 1 | Fix Results/Certificate back buttons with returnUrl; add Records breadcrumb | ac031a6 | Results.cshtml, Certificate.cshtml, Records.cshtml |
| 2 | Redesign Records.cshtml — stat cards, 2-tab layout, clickable Assessment rows | 522539f | Records.cshtml |
| 3 | CSRF fix Assessment.cshtml, retry/modal/shuffle in StartExam.cshtml + CMPController | a1edc31 | Assessment.cshtml, StartExam.cshtml, CMPController.cs |

## Files Modified

- `Views/CMP/Results.cshtml` — returnUrl-based back button (2 locations); "Kembali" label
- `Views/CMP/Certificate.cshtml` — certBackUrl from returnUrl query param; "Kembali" label
- `Views/CMP/Records.cshtml` — breadcrumb, new stat cards (Assessment/Training/Total), 2 Bootstrap tabs (Assessment Online + Training Manual), clickable rows linking to CMP/Results
- `Views/CMP/Assessment.cshtml` — RequestVerificationToken header added to both VerifyToken AJAX calls
- `Views/CMP/StartExam.cshtml` — 3-attempt exponential backoff retry, forceCloseModal replacing banner, option shuffle rendering via optShuffle ViewBag
- `Controllers/CMPController.cs` — ViewBag.OptionShuffle populated by deserializing ShuffledOptionIdsPerQuestion

## Key Decisions Made

1. **returnUrl approach** — Query param `?returnUrl=/Admin/ManageAssessment` appended by callers (Admin/UserAssessmentHistory); CMP/Assessment used as fallback. No server-side session storage needed.

2. **3-attempt retry timing** — Attempt 0 is the first fetch (no delay). Attempt 1 retries after 1s. Attempt 2 retries after 3s. After all 3 fail, error indicator + toast shown. Updated call site in saveAnswerWithDebounce from `false` → `0`.

3. **Variable rename** — `optionShuffleDict` already defined in the `if (assignment == null)` block scope; renamed outer variable to `parsedOptionShuffle` to avoid CS0136 compilation error.

4. **Option shuffle rendering** — `optShuffle[q.QuestionId]` gives ordered list of option IDs; LINQ `.FirstOrDefault(o => o.OptionId == oid)` maps back to ExamOptionItem. Null options filtered out. Falls back to original `q.Options` order if no shuffle stored.

5. **Records search/filter** — Old `filterTable()` JS kept but scoped to `.training-row` class which exists in both tab panes. Filter works across both tabs simultaneously (user sees filtered results in whichever tab they're on).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Variable name conflict in CMPController.cs**
- **Found during:** Task 3 build verification
- **Issue:** `optionShuffleDict` already declared in inner `if (assignment == null)` block; adding same name in outer scope caused CS0136
- **Fix:** Renamed outer variable to `parsedOptionShuffle`
- **Files modified:** Controllers/CMPController.cs
- **Commit:** a1edc31

## Self-Check: PASSED

All 6 modified files confirmed present. All 3 task commits (ac031a6, 522539f, a1edc31) confirmed in git log.
