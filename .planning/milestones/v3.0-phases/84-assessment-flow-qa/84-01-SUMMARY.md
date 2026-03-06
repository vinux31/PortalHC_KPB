---
phase: 84-assessment-flow-qa
plan: 01
subsystem: ui
tags: [excel, xlsx, closedxml, import, assessment, questions]

# Dependency graph
requires: []
provides:
  - GET /Admin/DownloadQuestionTemplate endpoint returning question_import_template.xlsx
  - Download Template button in ImportPackageQuestions.cshtml format reference card
affects: [84-02-smoke-test]

# Tech tracking
tech-stack:
  added: []
  patterns: [DownloadTemplate action pattern — green headers, italic example row, dark red instruction, AdjustToContents (mirrors DownloadImportTemplate)]

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/ImportPackageQuestions.cshtml

key-decisions:
  - "DownloadQuestionTemplate placed between PreviewPackage and ImportPackageQuestions GET for logical grouping"
  - "Column order in template (Q, OptA-D, Correct) matches ImportPackageQuestions Excel parser cells[0..5] exactly"

patterns-established:
  - "DownloadTemplate pattern: XLWorkbook, green headers (#16A34A/white), italic gray example row, dark red instruction row, AdjustToContents, File() return"

requirements-completed: [ASSESS-08]

# Metrics
duration: 7min
completed: 2026-03-04
---

# Phase 84 Plan 01: DownloadQuestionTemplate Action Summary

**GET /Admin/DownloadQuestionTemplate returning 6-column xlsx with green headers and italic example row, linked from ImportPackageQuestions format card**

## Performance

- **Duration:** 7 min
- **Started:** 2026-03-04T02:42:17Z
- **Completed:** 2026-03-04T02:49:17Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Added DownloadQuestionTemplate GET action to AdminController with [Authorize(Roles="Admin, HC")]
- Template has 6 columns (Question, Option A, Option B, Option C, Option D, Correct) with green bold headers, italic gray example row (RFCC question), dark red instruction row
- Column order matches the ImportPackageQuestions Excel parser exactly (cells[0..5])
- Added Download Template button inline with the format reference card in ImportPackageQuestions.cshtml

## Task Commits

Each task was committed atomically:

1. **Task 1: Add DownloadQuestionTemplate action to AdminController** - `65621ea` (feat) — combined with Task 2
2. **Task 2: Add Download Template button to ImportPackageQuestions view** - `65621ea` (feat)

## Files Created/Modified
- `Controllers/AdminController.cs` - Added DownloadQuestionTemplate GET action (~50 lines) between PreviewPackage and ImportPackageQuestions
- `Views/Admin/ImportPackageQuestions.cshtml` - Updated format reference card to include Download Template button

## Decisions Made
- DownloadQuestionTemplate placed between PreviewPackage and ImportPackageQuestions GET for logical package-management action grouping
- Column order in template (Question, Option A-D, Correct) matches ImportPackageQuestions parser cells[0..5] exactly to enable round-trip verification

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build reported MSB3027/MSB3021 file-lock errors because HcPortal.exe (PID 12580) was running during build. These are file-copy errors, not C# compile errors. Confirmed zero `error CS` compile errors in output. Code compiles correctly.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Plan 84-02 smoke test can now verify the full round-trip: download template → fill questions → import via Excel upload
- DownloadQuestionTemplate is accessible at GET /Admin/DownloadQuestionTemplate for HC/Admin roles

---
*Phase: 84-assessment-flow-qa*
*Completed: 2026-03-04*
