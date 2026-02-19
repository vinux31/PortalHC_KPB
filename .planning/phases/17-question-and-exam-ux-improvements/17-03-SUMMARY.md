---
phase: 17-question-and-exam-ux-improvements
plan: "03"
subsystem: ui

tags: [asp-net-core, mvc, razor, closedxml, excel-import, packages, questions]

# Dependency graph
requires:
  - phase: 17-02
    provides: ManagePackages action + view, PackageQuestions/PackageOptions DbSets

provides:
  - ImportPackageQuestions GET action (loads package info into ViewBag for two-tab form)
  - ImportPackageQuestions POST action (parses .xlsx via ClosedXML or TSV paste, validates rows, persists PackageQuestion + PackageOption records)
  - Views/CMP/ImportPackageQuestions.cshtml (two Bootstrap tabs: Upload Excel File + Paste from Excel, format reference card)

affects:
  - 17-04-PLAN.md (exam-taking UI, workers get assigned packages with imported questions)
  - 17-05-PLAN.md (grading uses PackageOption.Id from imported options)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ClosedXML already imported via 'using ClosedXML.Excel;' in CMPController.cs — reused for xlsx parsing"
    - "TSV paste header auto-detection: if last cell of first line is 'correct' (case-insensitive), skip that line"
    - "Row-level validation: empty question text, empty options, invalid Correct letter each produce per-row error messages"
    - "Per-question SaveChangesAsync flush to get generated PackageQuestion.Id before inserting PackageOptions"

key-files:
  created:
    - Views/CMP/ImportPackageQuestions.cshtml
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "using ClosedXML.Excel was already present at line 7 of CMPController.cs — no duplicate using directive added"
  - "Both tabs use @Html.AntiForgeryToken() explicitly since file upload form uses enctype=multipart/form-data (asp-action tag helper generates the token automatically for the paste form too)"
  - "On partial import with errors: TempData[Warning] — shows count imported + up to 5 row-level error messages"
  - "On success: redirect to ManagePackages (not ImportPackageQuestions) so HC sees the updated question count"

# Metrics
duration: 1min
completed: 2026-02-19
---

# Phase 17 Plan 03: Excel Import for Package Questions Summary

**ImportPackageQuestions GET + POST with ClosedXML xlsx parsing and TSV paste tab; row-level validation with per-row error reporting; redirects to ManagePackages on completion**

## Performance

- **Duration:** ~1 min
- **Started:** 2026-02-19T14:14:47Z
- **Completed:** 2026-02-19T14:16:08Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added `ImportPackageQuestions` GET action to `#region Package Management` in CMPController.cs: loads package with Questions, sets ViewBag (PackageId, PackageName, AssessmentId, CurrentQuestionCount), returns View()
- Added `ImportPackageQuestions` POST action: dual-path parser — if `excelFile` present, reads .xlsx with `XLWorkbook`, skips header row, collects 6 columns per row; if `pasteText` present, splits on `\n`, auto-detects and skips header, splits on `\t`; validates each row (non-empty question, non-empty options, valid Correct letter A/B/C/D); persists `PackageQuestion` then 4 `PackageOption` records per valid row; redirects to `ManagePackages` with success or warning TempData
- Created `Views/CMP/ImportPackageQuestions.cshtml` with Bootstrap tab switcher (two tabs), format reference card (`Question | Option A | Option B | Option C | Option D | Correct`), file upload form with `enctype=multipart/form-data`, paste textarea form, AntiForgeryToken in both forms

## Task Commits

1. **Task 1 + Task 2: ImportPackageQuestions GET/POST actions + ImportPackageQuestions.cshtml view** - `aa005a0` (feat)

## Files Created/Modified

- `Controllers/CMPController.cs` - Added ImportPackageQuestions GET and POST actions inside `#region Package Management`, before `#endregion`
- `Views/CMP/ImportPackageQuestions.cshtml` - Two-tab import form with format reference card, file upload tab, paste tab

## Decisions Made

- `using ClosedXML.Excel;` was already present at line 7 of CMPController.cs — no duplicate needed; used `new XLWorkbook(stream)` (not the fully-qualified `new ClosedXML.Excel.XLWorkbook(stream)` as in plan — both compile identically given the using directive)
- `@Html.AntiForgeryToken()` added explicitly in the file upload form because `enctype="multipart/form-data"` on a `<form>` tag with `asp-action` still generates the hidden token field, but explicit call makes it unambiguous for both forms
- Per-question `SaveChangesAsync()` flush used to obtain `newQ.Id` before inserting `PackageOption` records — consistent with `AddQuestion` action pattern already in this controller

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- ImportPackageQuestions GET/POST actions are live at `/CMP/ImportPackageQuestions?packageId=N`
- ManagePackages "Import Questions" button from 17-02 already links to this action — fully wired now
- Imported questions are stored as `PackageQuestion` + `PackageOption` records in DB, ready for 17-04 exam-taking UI
- Correct answer mapping (A=0, B=1, C=2, D=3 index) is persisted as `IsCorrect=true` on the corresponding `PackageOption`

## Self-Check: PASSED

- Controllers/CMPController.cs: FOUND (ImportPackageQuestions GET + POST added)
- Views/CMP/ImportPackageQuestions.cshtml: FOUND
- Commit aa005a0 (Task 1+2): FOUND
- dotnet build: 0 CS errors, Build succeeded

---
*Phase: 17-question-and-exam-ux-improvements*
*Completed: 2026-02-19*
