---
phase: 396-import-excel-retire-bulkbackfill
reviewed: 2026-06-18T00:00:00Z
depth: standard
files_reviewed: 12
files_reviewed_list:
  - Controllers/InjectAssessmentController.cs
  - Controllers/TrainingAdminController.cs
  - HcPortal.Tests/InjectExcelHelperTests.cs
  - HcPortal.Tests/InjectExcelImportTests.cs
  - Helpers/InjectExcelHelper.cs
  - Models/InjectAssessmentDtos.cs
  - Services/InjectAssessmentService.cs
  - ViewModels/InjectAssessmentViewModel.cs
  - Views/Admin/Index.cshtml
  - Views/Admin/InjectAssessment.cshtml
  - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
  - tests/e2e/inject-excel-396.spec.ts
findings:
  critical: 0
  warning: 3
  info: 5
  total: 8
status: issues_found
---

# Phase 396: Code Review Report

**Reviewed:** 2026-06-18
**Depth:** standard
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Phase 396 adds an Excel batch-import path to the inject-assessment wizard (`InjectExcelHelper` template generator + matrix parser, two controller endpoints `DownloadInjectTemplate`/`UploadInjectExcel`, a Step-5 toggle/panel/preview/error-list in the Razor view) and hard-removes the legacy BulkBackfill tool.

The seven critical invariants called out in the prompt are all satisfied:

1. **Preview == commit** — Both `UploadInjectExcel.BuildExcelPreviews` and the commit path (`InjectAssessmentService` step f, and `MapToInMemory`/`PreviewInjectScore`) route through the SAME `AssessmentScoreAggregator.Compute`. No separate grading branch was introduced. The integration test `ExcelPath_ProducesSameScore_AsFormPath` asserts DB `Score == previewPct` and the e2e test asserts `committedScore == 100`.
2. **Atomic all-or-nothing** — `UploadInjectExcel` returns the FULL error list on `errors.Count > 0` with `Ok=false` and zero DB write; the commit-side `PreflightValidateAsync` also reject-alls. Verified by `ExcelPath_AnyError_AtomicRollback` and e2e Scenario 4.
3. **EssayTextRequired scoped to form only (D-05)** — Controller sets `req.EssayTextRequired = false` only when `vm.Step5Method == "excel"`; default stays `true`. `InjectExcelImportTests.ExcelPath_EssayScoreNoText_NotRejected` proves both directions.
4. **Anti silent-grade-0** — Submit listener fills `#AnswersJson` from `injExcelAnswersCache`, which is only populated after a 0-error upload (invalid upload sets it to `'[]'`). e2e asserts cache `!= '[]'` before commit and `Score == 100` after.
5. **XSS-safe rendering** — All Excel-path DOM rendering uses `.textContent` / `createElement` (error list, preview rows, blank warning). No `innerHTML` with server/user data in the new Excel code.
6. **RBAC + antiforgery + 10MB + .xlsx whitelist** — Both new endpoints carry `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`; `UploadInjectExcel` adds `[RequestFormLimits(MultipartBodyLengthLimit = 10MB)]` + extension whitelist.
7. **BulkBackfill removal** — `Admin/BulkBackfill` + `Admin/BulkBackfillAssessment` actions and the `BulkBackfill.cshtml` view are gone; Index.cshtml entry-point removed (Section D now System-only). `CleanupAttemptHistory`, `ManualDuplicatePredicate`, and ClosedXML import are preserved as required. e2e Scenario 6 asserts 404 + missing entry-point.

No critical issues found. The findings below are correctness edge cases and quality notes worth addressing before sign-off.

## Warnings

### WR-01: Duplicate NIP in worker picker throws unhandled `ArgumentException` (caught as opaque "Gagal memproses file")

**File:** `Controllers/InjectAssessmentController.cs:244-245`
**Issue:** `UploadInjectExcel` builds three dictionaries keyed on NIP:
```csharp
var nipToUserId = picker.ToDictionary(p => p.Nip, p => p.Id);
var nipToName = picker.ToDictionary(p => p.Nip, p => p.Name);
```
NIP is not unique-constrained in `Users` (other controllers explicitly `GroupBy(u => u.NIP)` to tolerate shared NIPs, e.g. `CoachMappingController.cs:237`). If the selected audience contains two active users sharing one NIP, `ToDictionary` throws `ArgumentException: An item with the same key has already been added`. The `catch (Exception)` at line 271 swallows it into the generic "Gagal memproses file Excel" message — which is misleading (the file is fine; the picker selection is the problem) and gives the operator no path to recovery.

Note: the commit-side `InjectAssessmentService.PreflightValidateAsync:348-350` has the identical latent issue (`ToDictionaryAsync(u => u.NIP!)`), so this is a pre-existing pattern — but Phase 396 newly exercises it from the upload endpoint with a duplicate-tolerant audience picker.

**Fix:** Group by NIP and take the first (matching the rest of the codebase), or surface a friendly error naming the conflicting NIP:
```csharp
var byNip = picker
    .GroupBy(p => p.Nip)
    .ToDictionary(g => g.Key, g => g.First());
var nipToUserId = byNip.ToDictionary(kv => kv.Key, kv => kv.Value.Id);
var nipToName   = byNip.ToDictionary(kv => kv.Key, kv => kv.Value.Name);
// allowedNips = byNip.Keys.ToHashSet();
```
(Or detect duplicates explicitly and return `Ok=false` with `"NIP {x} dipakai >1 pekerja terpilih — perbaiki pilihan pekerja di Langkah 2."`.)

### WR-02: Fractional essay score in Excel is silently truncated, not rejected

**File:** `Helpers/InjectExcelHelper.cs:200-204`
**Issue:** When an Essay score cell holds a numeric value, the parser reads it as `double` and casts to `int`:
```csharp
bool hasScore = scoreCell.TryGetValue<double>(out var sd);
...
if (hasScore) { score = (int)sd; }
```
A value like `8.7` is silently truncated to `8`, and `8.9` to `8`. The operator typed a fractional grade and the committed score differs from what they entered, with no warning. The text-path (`int.TryParse`) correctly rejects non-integers, so behavior is inconsistent between a cell stored as a number vs. as text. Because essay scores feed directly into the certification-bearing total, a silent downward rounding is a correctness concern (the "preview == commit" invariant holds, but "input == committed" does not).

**Fix:** Reject non-integral numeric scores instead of truncating:
```csharp
if (hasScore)
{
    if (sd != Math.Floor(sd))
    {
        errors.Add(new InjectRowError { Nip = nip,
            Message = $"Baris {row}, kolom Soal {idx} (Essay): skor harus bilangan bulat." });
        continue;
    }
    score = (int)sd;
}
```

### WR-03: Download-template programmatic form removes itself before browser starts the download

**File:** `Views/Admin/InjectAssessment.cshtml:1737-1739`
**Issue:** The Download Template handler builds a temporary form, submits it, and immediately removes it:
```javascript
document.body.appendChild(tmp);
tmp.submit();
document.body.removeChild(tmp);
```
Synchronously removing the form node right after `submit()` is a known fragile pattern — some browsers begin the navigation/download asynchronously and detaching the submitting form before the request is dispatched can cancel it or drop fields. It currently passes e2e (Chromium), but it is timing-dependent and may intermittently fail on other engines or under load. The full-page form POST also navigates the top-level document; because the response is a file attachment the page normally stays put, but any non-file response (e.g. the `TempData["Error"]` re-render path when questions/workers are empty) will replace the wizard and discard all client state.

**Fix:** Defer removal (e.g. `setTimeout(() => tmp.remove(), 1000)`), and/or target an isolated iframe so an error response cannot clobber the wizard:
```javascript
tmp.target = 'injDlFrame'; // <iframe name="injDlFrame" hidden>
document.body.appendChild(tmp);
tmp.submit();
setTimeout(function () { tmp.remove(); }, 1000);
```

## Info

### IN-01: `DownloadInjectTemplate` empty-state returns wizard view, losing all client-held state

**File:** `Controllers/InjectAssessmentController.cs:172-177, 184-189`
**Issue:** When no questions or no NIP-bearing workers are present, the handler does `return View(nameof(InjectAssessment), vm)`, re-rendering the full wizard from a fresh page load. Because questions/answers live only in client JS state (`injQuestions[]`, `step5State`), this full navigation discards everything the operator authored. In practice the client gates the button (questions exist by Step 3, workers by Step 2), so this path is hard to hit, but it is a foot-gun if reached.
**Fix:** Return a JSON error (like `UploadInjectExcel` does) and surface it inline, or guard the button so the POST cannot fire without questions+workers.

### IN-02: `MapToInMemory` and `BuildExcelPreviews` are duplicated by `InjectExcelImportTests.PreviewPercentage`

**File:** `Controllers/InjectAssessmentController.cs:446-470` and `HcPortal.Tests/InjectExcelImportTests.cs:128-153`
**Issue:** The test re-implements the controller's `MapToInMemory` projection verbatim to predict the preview. This is intentional (the test asserts parity) but creates a drift risk: a future change to `MapToInMemory` would not be caught unless the test copy is updated in lockstep. The controller's `MapToInMemory` is `private static`, so the test cannot reuse it directly.
**Fix:** Consider extracting the in-memory projection into a shared pure helper (e.g. on `AssessmentScoreAggregator` or a small `InjectPreviewHelper`) so production and test reference one source of truth — same kill-drift rationale already used for the aggregator itself.

### IN-03: Extension whitelist is the only upload content check (no magic-byte / sheet validation before parse)

**File:** `Controllers/InjectAssessmentController.cs:216-222`
**Issue:** Upload validation trusts `Path.GetExtension(excel.FileName)`. A renamed non-xlsx payload passes the extension check and reaches `new XLWorkbook(stream)`, which is wrapped in try/catch and degrades to a friendly error — so this is not exploitable (no 500, no parse of untrusted format beyond ClosedXML's own hardening). Noted only as defense-in-depth: the comment says "Security V12" but the actual guarantee is "ClosedXML rejects it gracefully," not "format validated." Acceptable for v1; no action required beyond accuracy of the comment.

### IN-04: `.xls` accepted by whitelist but ClosedXML reads only `.xlsx` (OOXML)

**File:** `Controllers/InjectAssessmentController.cs:217` and `Views/Admin/InjectAssessment.cshtml:570`
**Issue:** The whitelist and the file input `accept=".xlsx,.xls"` both advertise legacy `.xls` (BIFF) support, but ClosedXML only opens OOXML `.xlsx`. A genuine `.xls` file passes the extension gate, then fails inside `new XLWorkbook(stream)` and surfaces "Gagal membaca file Excel. Pastikan file .xlsx valid…". The operator was told `.xls` is allowed, so the failure is confusing.
**Fix:** Drop `.xls` from both the whitelist and the `accept` attribute (template is always `.xlsx`), or message explicitly that only `.xlsx` is supported.

### IN-05: `LetterToIndex` only supports single-letter options (A–Z); 27th option silently invalid

**File:** `Helpers/InjectExcelHelper.cs:29-34`
**Issue:** `LetterToIndex` returns -1 for anything but one A–Z character, and `IndexToLetter` only produces single letters. Questions with more than 26 options cannot be addressed from Excel. The authoring UI caps options at A–D (4), so this is purely theoretical for the current product, but worth a note if option count ever grows. No action needed for v1.

---

_Reviewed: 2026-06-18_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
