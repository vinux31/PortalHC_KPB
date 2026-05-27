# Plan 325-02 SUMMARY — P01 path traversal + P02 magic byte + ILogger? opsional

**Status:** COMPLETE
**Wave:** 1
**Commits:** `524da7eb` (Task 1 constants) + `1920e709` (Task 2 helper) + `[Task 3 flip tests]`
**Date:** 2026-05-27

## Decisions Implemented

| ID | Decision | Where |
|----|----------|-------|
| D-01 | Path traversal fix via `Path.GetFileName()` | `FileUploadHelper.SaveFileAsync` |
| D-02 | Magic byte hardcoded switch (3 format) | `AssessmentConstants.FileValidation.MagicBytes` dict |
| D-03 | Read 8 byte + `stream.Position = 0` reset | `ValidateCertificateFile` |
| D-06 | Error message strings locked verbatim | `"Isi file tidak cocok dengan ekstensi (magic byte mismatch)."` |
| D-09 | MagicBytes Dictionary + MatchesMagicByte helper | `AssessmentConstants.cs` |
| D-10 | LogWarning kalau filename mengandung path separator | `SaveFileAsync` audit trail |

## Files Modified

| File | Change |
|------|--------|
| `Models/AssessmentConstants.cs` | +33 line: `MagicBytes` dict (.pdf/.jpg/.jpeg/.png) + `MatchesMagicByte(ext, header)` static helper |
| `Helpers/FileUploadHelper.cs` | ValidateCertificateFile: tambah magic byte gate (read 8 byte, reset pos, reject `read<3`, reject `!MatchesMagicByte`). SaveFileAsync: optional `ILogger? logger = null` param, `Path.GetFileName` strip, `LogWarning` D-10 audit. Tambah `using Microsoft.Extensions.Logging;`. |
| `HcPortal.Tests/FileUploadHelperTests.cs` | Hapus 2 `[Fact(Skip=...)]` attribute, uncomment body 2 test |

## Verification

```
dotnet build HcPortal.sln    → 0 error, 23 pre-existing warning
dotnet test HcPortal.Tests   → Passed: 7, Skipped: 0, Failed: 0, Total: 7, Duration: 298ms
```

## Backward Compat

Caller existing `SaveFileAsync(file, env.WebRootPath, "uploads/certificates")` di:
- `TrainingAdminController.cs:581` (AddManualAssessment)
- `TrainingAdminController.cs:681` (EditManualAssessment)

Compile tanpa update karena `ILogger? logger = null` default. No breaking change.

## Threat Mitigation

| Threat | Severity | Status |
|--------|----------|--------|
| T-325-01 path traversal | HIGH | MITIGATED — `Path.GetFileName` strip + `LogWarning` audit |
| T-325-02 MIME spoof | MED | MITIGATED — magic byte signature centralized di constants |

## Success Criteria

- **SC-2** (`.exe→.pdf` reject): ✓ unit test `ExeRenamedPdf_ReturnsInvalidMagicByte`
- **SC-3** (PDF/JPG/PNG valid lolos): ✓ 3 unit test `ValidPdf` + `ValidJpg` + `ValidPng`
- **SC-6** (foundation test pass): ✓ 7/7 pass
- **SC-1** (path traversal manual Postman test): pending Plan 05 UAT batch

## Handoff Plan 03

Refactor 3 inline duplicate file validation sites di `TrainingAdminController.cs` (line 206-215, 221-233, 459-471) call `FileUploadHelper.ValidateCertificateFile` — else P02 magic byte gate BYPASS-able di endpoint Add/Edit Training (RESEARCH §Pitfall 1).
