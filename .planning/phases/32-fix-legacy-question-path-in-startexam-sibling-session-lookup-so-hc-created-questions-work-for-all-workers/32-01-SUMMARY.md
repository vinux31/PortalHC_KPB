---
phase: 32-fix-legacy-question-path
plan: 01
subsystem: assessment-exam
tags: [legacy-path, sibling-session, assessment-questions, grading]
dependency_graph:
  requires: []
  provides: [legacy-exam-question-lookup-fixed]
  affects: [StartExam, ExamSummary, SubmitExam]
tech_stack:
  added: []
  patterns: [sibling-session-lookup, siblingSessionIds.Contains]
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
decisions:
  - "Sibling lookup pattern applied identically across all three legacy paths: Title + Category + Schedule.Date matching"
  - "questionsForGrading variable name used in SubmitExam to make grading source explicit"
  - "siblingSessionIds computed inline in ExamSummary and SubmitExam else-blocks (not shared scope); reused from outer scope in StartExam"
metrics:
  duration: 4min
  completed: 2026-02-21
  tasks: 2
  files: 1
---

# Phase 32 Plan 01: Fix Legacy Question Path Summary

## One-liner

Applied sibling session lookup (Title + Category + Schedule.Date) to all three legacy paths in CMPController so HC-created questions are found for every worker in the batch, not just the representative session owner.

## What Was Built

HC creates questions via the "Question" button on an assessment card. Those questions are stored on the representative (first) session. Worker sessions in the same batch have zero questions. The legacy path in all three exam actions was querying only `a.Id == id` (the worker's own empty session), causing blank exams, empty review summaries, and zero scores.

The package path already solved this with a `siblingSessionIds.Contains()` lookup. This plan applied the same pattern to the three legacy paths.

### StartExam (Task 1)

**Before:**
```csharp
var assessmentWithQuestions = await _context.AssessmentSessions
    .Include(a => a.Questions)
        .ThenInclude(q => q.Options)
    .FirstOrDefaultAsync(a => a.Id == id);
```

**After:**
```csharp
var sessionWithQuestions = await _context.AssessmentSessions
    .Include(a => a.Questions)
        .ThenInclude(q => q.Options)
    .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any())
    .FirstOrDefaultAsync();
```

`siblingSessionIds` was already computed at line 1864 (for the package path) and is in scope -- no recomputation needed.

### ExamSummary GET (Task 2)

Added sibling session ID lookup inside the legacy else-block, then replaced:
```csharp
.Where(q => q.AssessmentSessionId == id)
```
with:
```csharp
.Where(q => siblingSessionIds.Contains(q.AssessmentSessionId))
```

`assessment.Schedule.Date` is already accessible via the `FindAsync(id)` load -- no Include needed.

### SubmitExam POST (Task 2)

Added sibling session ID lookup + full session load with questions inside the legacy else-block:
```csharp
var siblingWithQuestions = await _context.AssessmentSessions
    .Include(a => a.Questions)
        .ThenInclude(q => q.Options)
    .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any())
    .FirstOrDefaultAsync();

var questionsForGrading = siblingWithQuestions?.Questions?.ToList()
    ?? new List<AssessmentQuestion>();
```

Then changed `foreach (var question in assessment.Questions)` to `foreach (var question in questionsForGrading)`.

UserResponse records still use `AssessmentSessionId = id` (the worker's own session ID) -- this is correct, responses are per-worker.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 48d5d96 | fix(32-01): fix StartExam legacy path to use sibling session lookup |
| 2 | a8ebc42 | fix(32-01): fix ExamSummary and SubmitExam legacy paths to use sibling session lookup |

## Verification

- `dotnet build`: zero CS compiler errors (only MSB file-lock warnings -- app was running)
- Diff reviewed: three legacy else-blocks modified; all three package-path if-blocks untouched
- `siblingSessionIds.Contains` present in all three fixes
- `HasPackages = false` preserved in StartExam legacy ViewModel
- `FindAsync(id)` in ExamSummary load unchanged
- `FirstOrDefaultAsync(a => a.Id == id)` in SubmitExam load unchanged (no `.Include(a => a.Schedule)` added)
- `AssessmentSessionId = id` still used in UserResponse.Add (worker's own session)

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- Controllers/CMPController.cs: modified (3 legacy paths fixed)
- Commits 48d5d96 and a8ebc42: verified via git log
