---
phase: 357-standarisasi-istilah-tipe-soal-single-answer-multiple-answer-essay
reviewed: 2026-06-09T00:00:00Z
depth: standard
files_reviewed: 8
files_reviewed_list:
  - Models/QuestionTypeLabels.cs
  - HcPortal.Tests/QuestionTypeLabelsTests.cs
  - Views/Admin/ManagePackageQuestions.cshtml
  - Views/Admin/EditPesertaAnswers.cshtml
  - Views/Admin/ImportPackageQuestions.cshtml
  - Controllers/AssessmentAdminController.cs
  - Controllers/CMPController.cs
  - Services/GuideContentProvider.cs
findings:
  critical: 0
  warning: 0
  info: 2
  total: 2
status: clean
---

# Phase 357: Code Review Report

**Reviewed:** 2026-06-09
**Depth:** standard
**Files Reviewed:** 8
**Status:** clean (2 non-blocking info items)

## Summary

Phase 357 is a pure label re-wording + dead-code removal phase. The review focused on the
five risk areas the workflow flagged: DB enum switch keys / value attributes / route keys /
JS-read values, the CMPController `TrueFalse` removal, Razor binding correctness, over-replace
risk, and the Excel ternary keying. I reviewed the actual Phase 357 diff (commits `59dd71e1`,
`e6a32424`, `739b92eb`, `76828e11`) against the 8 source files, not pre-existing code.

**No correctness issues found.** Every load-bearing identifier is preserved:

- **DB enum switch keys** (`QuestionTypeLabels.cs`): only the return *strings* changed
  (`Single Choice/Multiple Answers` -> `Single Answer/Multiple Answer`). The `switch` keys
  (`"MultipleChoice"`, `"MultipleAnswer"`, `"Essay"`) and `BadgeClass()` are byte-for-byte
  unchanged. Fallback (`_`) follows new wording and is locked by the `[InlineData(null, ...)]`
  test cases.
- **Value attributes** (`ManagePackageQuestions.cshtml`): `<option value="MultipleChoice/MultipleAnswer/Essay">`
  preserved verbatim; only the visible text moved to `@QuestionTypeLabels.Long(...)` binding.
  The JS handler reads `this.value` (`'Essay'`, `'MultipleAnswer'`) — untouched.
- **Route keys** (`ImportPackageQuestions.cshtml`): `new { type = "MC" }` / `type = "MA"` /
  `type = "Essay"` / `type = "Universal"` preserved; only button label text changed.
- **Excel ternary** (`AssessmentAdminController.cs:4550`): condition still
  `tipe == "MultipleChoice" ? "SA" : "MA"` — keyed on the enum value, not a label. Only the
  output abbreviation `"MC"` -> `"SA"` changed. The adjacent branch at line 4530
  (`else if (tipe == "MultipleChoice")`) that drives grading is untouched and consistent.
- **CMPController `TrueFalse` removal** (lines 3389, 3624): see IN-01 below — confirmed
  behavior-preserving for the 3 valid types.
- **Razor binding**: `@using HcPortal.Models` is global in `Views/_ViewImports.cshtml:2`, so
  `@QuestionTypeLabels.Long/Short/BadgeClass` resolve in all views. Confirmed by `dotnet build`
  (0 errors, 0 warnings).
- **No over-replacement**: grep for `Multiple Answers`, `Single Choice`, `Answer Answer`,
  `Answers (` across all `.cs`/`.cshtml` returns zero matches. The GuideContentProvider reword
  correctly left the already-new "Multiple Answer (MA)" step untouched (no double-replace).

`dotnet build` passes clean. Test file `QuestionTypeLabelsTests.cs` correctly locks both the new
wording and the fallback for `null`.

## Info

### IN-01: CMPController `TrueFalse` removal is behavior-safe, but the "unreachable" guarantee rests on DB data, not the runtime normalizer

**File:** `Controllers/CMPController.cs:3389`, `Controllers/CMPController.cs:3624`
**Issue:** Both branches changed from `questionType == "MultipleChoice" || questionType == "TrueFalse"`
(and the `!= ... && != "TrueFalse"` guard) to plain `MultipleChoice`. The `MultipleChoice`
condition is preserved verbatim, so behavior for the 3 valid types is identical — this is correct.

One nuance worth recording: in this analytics path `questionType` is sourced directly from the
raw DB value (`question.QuestionType ?? "MultipleChoice"` at lines 3373 and 3623), **not** routed
through `NormalizeQuestionType` (which lives in `AssessmentAdminController.cs:5725` and coerces
unknown values to `MultipleChoice` only on the import write-path). So the "unreachable" claim
holds because `"TrueFalse"` cannot exist in the `QuestionType` column (True/False was dropped in
Phase 298 and stored as `MultipleChoice` — see `.planning/v14.0-VERIFY-WORK.md:73`), not because
a runtime guard would re-map it.

Net effect: for any real data, removing the dead `TrueFalse` term changes nothing — distractor
analysis was already gated to `MultipleChoice`, and a hypothetical legacy `"TrueFalse"` row would
have produced distractor output under the old code but now would not. Given the data contract,
this is dead-code removal, not a behavior change. No action required; noted only so a future
reader does not mistake the normalizer for the safety net.
**Fix:** None required. If extra confidence is desired, a one-line SQL check
(`SELECT COUNT(*) FROM PackageQuestions WHERE QuestionType = 'TrueFalse'` -> expect 0) on Dev/Prod
before promotion would conclusively confirm the data assumption.

### IN-02: GuideContentProvider dropped legacy search keywords "multiple choice" / "mc"

**File:** `Services/GuideContentProvider.cs:188`
**Issue:** The keyword array for the `cmp-tipe-package-question` guide item changed
`{ ..., "multiple choice", ..., "mc", ... }` to `{ ..., "single answer", ..., "sa", ... }`.
An admin who searches the guide using the old term "multiple choice" or "mc" will no longer find
this item. This is intentional per the rebrand, but since the underlying DB enum is still literally
`MultipleChoice`, an admin familiar with the stored value or older docs may search the legacy term.
**Fix:** Optional — if backward search discoverability matters, append the legacy aliases without
showing them in UI text:
```csharp
Keywords: new[] { "tipe soal", "single answer", "multiple answer", "essay", "rubrik",
                  "package question", "sa", "ma", "multiple choice", "mc" }
```
This is a UX-discoverability nicety, not a correctness issue.

---

_Reviewed: 2026-06-09_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
