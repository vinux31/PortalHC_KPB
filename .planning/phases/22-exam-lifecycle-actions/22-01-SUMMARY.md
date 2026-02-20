---
phase: 22-exam-lifecycle-actions
plan: 01
subsystem: assessment
tags: [ef-migration, datetime, csharp, razor, assessment-session, exam-lifecycle]

# Dependency graph
requires:
  - phase: 21-assessment-session-tracking
    provides: StartedAt nullable datetime, InProgress status, StartExam GET action
provides:
  - ExamWindowCloseDate nullable DateTime column on AssessmentSessions table
  - EF migration AddExamWindowCloseDate
  - Close date guard in StartExam GET (before InProgress write)
  - Abandoned session guard in StartExam GET
  - ExamWindowCloseDate binding in CreateAssessment POST and EditAssessment POST
  - Date input field in CreateAssessment.cshtml and EditAssessment.cshtml
affects: [22-exam-lifecycle-actions, 23-package-user-response, StartExam GET]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Guard-before-write: status/date guards inserted before the InProgress StartedAt write in StartExam GET"
    - "ModelState.Remove for optional nullable fields to prevent accidental validation failure"
    - "Bulk-assign propagation: ExamWindowCloseDate copied to sibling sessions in EditAssessment bulk-assign block"

key-files:
  created:
    - Migrations/20260220135244_AddExamWindowCloseDate.cs
    - Migrations/20260220135244_AddExamWindowCloseDate.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Controllers/CMPController.cs
    - Views/CMP/CreateAssessment.cshtml
    - Views/CMP/EditAssessment.cshtml
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "Abandoned guard added alongside ExamWindowCloseDate guard — both placed before InProgress write per LIFE-02 requirement"
  - "ExamWindowCloseDate is nullable (no [Required]) — null means no expiry enforced"
  - "ModelState.Remove('ExamWindowCloseDate') added defensively in CreateAssessment POST to prevent optional field validation failure"
  - "Bulk-assign initializer in EditAssessment POST copies ExamWindowCloseDate from savedAssessment for consistency"

patterns-established:
  - "Guard ordering in StartExam GET: Completed → ExamWindowCloseDate → Abandoned → InProgress write"

# Metrics
duration: 5min
completed: 2026-02-20
---

# Phase 22 Plan 01: Exam Window Close Date Summary

**ExamWindowCloseDate nullable datetime2 column on AssessmentSessions with StartExam GET guard blocking workers after close date and HC form inputs in Create/Edit Assessment**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-20T13:51:35Z
- **Completed:** 2026-02-20T13:55:54Z
- **Tasks:** 2
- **Files modified:** 5 (+ 2 migration files created)

## Accomplishments
- EF migration adds `ExamWindowCloseDate datetime2 NULL` column to AssessmentSessions — applied to database
- StartExam GET guard checks `ExamWindowCloseDate.HasValue && DateTime.UtcNow > value` and redirects with Indonesian-language "Ujian sudah ditutup. Waktu ujian telah berakhir." error before any InProgress write
- Abandoned session guard also added before InProgress write — workers with Abandoned status see "Ujian Anda sebelumnya telah dibatalkan." and cannot reload exam
- Create Assessment and Edit Assessment forms now display "Tanggal Tutup Ujian (Opsional)" date input with help text
- Controller binds ExamWindowCloseDate in CreateAssessment POST (session initializer + ModelState.Remove), EditAssessment POST (field assignment), and bulk-assign initializer

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ExamWindowCloseDate to AssessmentSession model and run EF migration** - `e70ad3c` (feat)
2. **Task 2: Enforce close date in StartExam GET and bind ExamWindowCloseDate in Create/Edit forms + controller** - `f60c5d0` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `Models/AssessmentSession.cs` - Added `public DateTime? ExamWindowCloseDate { get; set; }` with XML doc comment
- `Migrations/20260220135244_AddExamWindowCloseDate.cs` - EF migration adding datetime2 nullable column
- `Migrations/20260220135244_AddExamWindowCloseDate.Designer.cs` - Migration snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated with ExamWindowCloseDate
- `Controllers/CMPController.cs` - Guard in StartExam GET; ExamWindowCloseDate binding in CreateAssessment POST, EditAssessment POST, bulk-assign initializer
- `Views/CMP/CreateAssessment.cshtml` - "Tanggal Tutup Ujian" date input inside Assessment Settings card
- `Views/CMP/EditAssessment.cshtml` - "Tanggal Tutup Ujian" date input inside Assessment Settings card

## Decisions Made
- Abandoned guard added alongside ExamWindowCloseDate guard: both are LIFE-02 concerns and belong together at the same guard point before InProgress write.
- ExamWindowCloseDate intentionally has no [Required] annotation — null = no expiry (per plan).
- ModelState.Remove("ExamWindowCloseDate") added defensively in CreateAssessment POST to prevent optional nullable DateTime fields from causing accidental validation failures.
- Bulk-assign (EditAssessment POST) copies ExamWindowCloseDate from savedAssessment to new sibling sessions for consistency with PassPercentage and AllowAnswerReview propagation pattern.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added Abandoned session guard alongside ExamWindowCloseDate guard**
- **Found during:** Task 2 (StartExam GET enforcement)
- **Issue:** Plan specified ExamWindowCloseDate guard only, but the plan text also described an Abandoned guard block to add at the same location (same LIFE-02 requirement, same insertion point). Abandoned sessions have StartedAt set — without the guard they silently reload the exam.
- **Fix:** Added both guards in sequence after Completed guard, before InProgress write, as described in the plan action block.
- **Files modified:** Controllers/CMPController.cs
- **Verification:** Build passes; guard ordering Completed → ExamWindowCloseDate → Abandoned → InProgress write confirmed by code review.
- **Committed in:** f60c5d0 (Task 2 commit)

---

**Total deviations:** 1 auto-added (per plan action text — Abandoned guard was described in plan body)
**Impact on plan:** Matches plan intent exactly. No scope creep.

## Issues Encountered
None - dotnet build passed 0 errors on both task commits.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- ExamWindowCloseDate column live in database; HC can set it via Create/Edit Assessment forms
- StartExam GET enforces the close date — workers blocked with clear Indonesian-language message
- Phase 22-02 (Keluar Ujian / Abandon) and 22-03/22-04 can proceed — StartExam GET guard ordering is established

---
*Phase: 22-exam-lifecycle-actions*
*Completed: 2026-02-20*
