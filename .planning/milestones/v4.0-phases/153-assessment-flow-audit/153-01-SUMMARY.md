---
phase: 153-assessment-flow-audit
plan: 01
subsystem: assessment
tags: [assessment, admin, import, excel, validation, bug-fix]

requires: []
provides:
  - "Audit report for ASSESS-01 (assessment creation) and ASSESS-02 (question import)"
  - "Fixed DeleteQuestion FK crash when question has user responses"
  - "Fixed ImportPackageQuestions N+1 SaveChangesAsync (batched to single transaction)"
  - "Fixed AddQuestion non-atomic save with rollback guard"
  - "Fixed EditAssessment POST missing server-side validation"
  - "Fixed CreateAssessment view missing Warning alert display"
  - "Added 5MB file size guard to ImportPackageQuestions"
affects: [153-02, 153-03, 153-04]

tech-stack:
  added: []
  patterns:
    - "Batch SaveChangesAsync: collect all entities before saving — avoids N+1 round-trips"
    - "Pre-delete Restrict FK children before removing parent to avoid FK constraint crash"
    - "EditAssessment validation mirrors CreateAssessment — same rules for create and edit"

key-files:
  created:
    - ".planning/phases/153-assessment-flow-audit/153-01-AUDIT-REPORT.md"
  modified:
    - "Controllers/AdminController.cs"
    - "Views/Admin/CreateAssessment.cshtml"

key-decisions:
  - "EditAssessment validation added inline (TempData redirect pattern) to match existing controller style — no ModelState return needed since view has no edit form model"
  - "ImportPackageQuestions batch refactor: use PackageQuestion.Options navigation collection so EF resolves FK automatically — single SaveChangesAsync per import"
  - "5MB file size limit for Excel imports — matches reasonable import size, prevents OOM on large files"

patterns-established:
  - "Audit format: Code Review produces AUDIT-REPORT.md with finding severity table + fix log"

requirements-completed: [ASSESS-01, ASSESS-02]

duration: 30min
completed: 2026-03-11
---

# Phase 153 Plan 01: Assessment Creation and Question Import Audit Summary

**Code audit of assessment creation (ASSESS-01) and question import (ASSESS-02) flows — found and fixed 6 bugs/edge-cases including a FK crash in DeleteQuestion and N+1 import loop**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-03-11T00:00:00Z
- **Completed:** 2026-03-11T00:30:00Z
- **Tasks:** 1 of 2 (Task 2 is checkpoint:human-verify, pending approval)
- **Files modified:** 2 (+ 1 audit report created)

## Accomplishments
- Produced comprehensive audit report covering 10 findings across ASSESS-01 and ASSESS-02
- Fixed DeleteQuestion crash: UserResponse rows with Restrict FK must be removed before question
- Fixed ImportPackageQuestions: N+1 SaveChangesAsync inside per-row loop replaced with single batched transaction
- Fixed EditAssessment POST: added server-side validation (schedule date, duration, pass%, token)
- Fixed CreateAssessment view: added Warning alert for duplicate detection display
- Added 5MB file size guard to ImportPackageQuestions to prevent OOM

## Task Commits

1. **Task 1: Code review — Assessment creation and question import** - `e952fec` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` — DeleteQuestion fix, AddQuestion atomic save, ImportPackageQuestions batch + size limit, EditAssessment validation
- `Views/Admin/CreateAssessment.cshtml` — Warning alert block added
- `.planning/phases/153-assessment-flow-audit/153-01-AUDIT-REPORT.md` — Full audit findings

## Decisions Made
- EditAssessment validation added using TempData["Error"] + redirect pattern (not ModelState return) since the edit view doesn't have a form model that re-renders on error
- Import batch refactor uses EF navigation collection (PackageQuestion.Options) so FK assignment is automatic — cleaner than two SaveChanges calls
- 5MB file size limit chosen — large enough for 100+ question imports, small enough to prevent abuse

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] DeleteQuestion crashes with FK constraint when question has user responses**
- **Found during:** Task 1 (code review)
- **Issue:** UserResponse.AssessmentQuestionId configured as `OnDelete(Restrict)` — removing question without first removing responses throws FK violation
- **Fix:** Added query to load and delete UserResponse rows before AssessmentQuestion.Remove
- **Files modified:** Controllers/AdminController.cs (~4896)
- **Committed in:** e952fec

**2. [Rule 1 - Bug] ImportPackageQuestions: 2x SaveChangesAsync per question (N+1 pattern)**
- **Found during:** Task 1 (code review)
- **Issue:** Per-question loop called SaveChangesAsync twice (question then options) — 80 DB round-trips for 40 questions
- **Fix:** Refactored to add options via PackageQuestion.Options navigation collection, single SaveChangesAsync + transaction
- **Files modified:** Controllers/AdminController.cs (~5302)
- **Committed in:** e952fec

**3. [Rule 2 - Missing Critical] EditAssessment POST has no server-side validation**
- **Found during:** Task 1 (code review)
- **Issue:** EditAssessment POST accepted any schedule date, any DurationMinutes, any PassPercentage — could save invalid data
- **Fix:** Added validation block matching CreateAssessment rules
- **Files modified:** Controllers/AdminController.cs (~1130)
- **Committed in:** e952fec

**4. [Rule 2 - Missing Critical] ImportPackageQuestions has no file size limit**
- **Found during:** Task 1 (code review)
- **Issue:** Could upload arbitrarily large files causing OOM
- **Fix:** Added 5MB guard at POST entry
- **Files modified:** Controllers/AdminController.cs (~5161)
- **Committed in:** e952fec

**5. [Rule 1 - Bug] CreateAssessment view: Warning alert not rendered (duplicate detection lost)**
- **Found during:** Task 1 (code review)
- **Issue:** Controller sets TempData["Warning"] for duplicates but view only renders TempData["Error"] and TempData["SuccessMessage"]
- **Fix:** Added warning alert block in view
- **Files modified:** Views/Admin/CreateAssessment.cshtml
- **Committed in:** e952fec

---

**Total deviations:** 5 auto-fixed (2 bugs, 2 missing critical, 1 bug+cosmetic)
**Impact on plan:** All fixes necessary for correctness and security. No scope creep.

## Issues Encountered
- Build fails with MSB3492 / MSB3021 file-lock error because app is already running — code compiles clean (no C# errors), just can't overwrite the running .exe

## Next Phase Readiness
- ASSESS-01 and ASSESS-02 code review complete, bugs fixed
- Task 2 (checkpoint:human-verify) pending user browser verification of audit findings
- Ready for browser UAT: Admin > ManageAssessment > create/edit/import flows

---
*Phase: 153-assessment-flow-audit*
*Completed: 2026-03-11*
