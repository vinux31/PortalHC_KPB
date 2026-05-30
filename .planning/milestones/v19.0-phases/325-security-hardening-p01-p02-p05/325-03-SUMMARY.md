# Plan 325-03 SUMMARY — Refactor 3 inline file validation di TrainingAdminController

**Status:** COMPLETE
**Wave:** 2
**Commit:** `1df212c6`
**Date:** 2026-05-27

## Files Modified

| File | Change |
|------|--------|
| `Controllers/TrainingAdminController.cs` | 3 inline duplicate validation site → `FileUploadHelper.ValidateCertificateFile` (-36 +16 line net) |

## Sites Refactored

| Site | Endpoint | Pre-Refactor Line | Pattern | Post-Refactor |
|------|----------|---------|---------|------|
| #1 | `AddTraining` POST single | 202-216 | `ModelState.AddModelError("CertificateFile", ...)` | Helper call + preserve `sertifikatUrl` decl |
| #2 | `AddTraining` POST per-worker loop | 218-233 | `ModelState.AddModelError("", "File untuk pekerja {UserId}...")` | Helper call + message context preserve `$"File untuk pekerja {wc.UserId}: {wcErr}"` |
| #3 | `EditTraining` POST single | 444-459 | `TempData["Error"] + RedirectToAction` | Helper call + preserve TempData + RedirectToAction pattern |

## Verification

```
grep -c "allowedExtensions = new[] { \".pdf\"" Controllers/TrainingAdminController.cs  → 0
grep -c "allowedExts = new[]"                  Controllers/TrainingAdminController.cs  → 0
grep -c "10 * 1024 * 1024"                     Controllers/TrainingAdminController.cs  → 0
grep -c "FileUploadHelper.ValidateCertificateFile" Controllers/TrainingAdminController.cs  → 7
dotnet build HcPortal.sln  → 0 error, 23 pre-existing warning
dotnet test HcPortal.Tests → Passed: 7, Skipped: 0, Failed: 0, Duration: 296ms
```

Catatan: `.xlsx` import line 869 `allowedExtensions = new[] { ".xlsx", ".xls" }` BUKAN target refactor (Excel import, beda dari cert upload).

## Threat Mitigation

| Threat | Severity | Status |
|--------|----------|--------|
| T-325-04 (P02 bypass via inline duplicate) | HIGH | MITIGATED — 3 endpoint sekarang reach magic byte gate Plan 02 |
| T-325-02 (transitive root cause) | MED | FULLY MITIGATED — Plan 02 helper + Plan 03 refactor combined |

## Success Criteria

- **SC-2** (`.exe→.pdf` reject) reachable di `/Admin/AddTraining` + `/Admin/EditTraining` — Plan 05 manual UAT verify
- **SC-3** (PDF/JPG/PNG valid lolos) tetap berfungsi via helper

## Handoff Plan 04

P05 FK quick patch — pre-check referencing + try/catch DbUpdateException + TempData["Error"] di 3 endpoint delete:
- `TrainingAdminController.DeleteTraining` (line 527-548)
- `AssessmentAdminController.DeleteAssessmentSession` (target line :744, FK = AssessmentSession not TrainingRecord per RESEARCH CORRECTION 2)
- `AssessmentAdminController.DeleteAssessment` (line 2040-2162, pre-check AWAL endpoint sebelum tx scope per D-11)
