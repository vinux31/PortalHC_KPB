# Plan 325-01 SUMMARY — Bootstrap xUnit + 6 stub test

**Status:** COMPLETE
**Wave:** 0 (foundation)
**Commits:** `7069ead2` (Task 1) + `3255b9b4` (Task 2)
**Date:** 2026-05-27

## Files Created

| File | Purpose |
|------|---------|
| `HcPortal.Tests/HcPortal.Tests.csproj` | xUnit v2 project net8.0, `Microsoft.NET.Sdk` (BUKAN Web), ProjectReference ke `..\HcPortal.csproj` |
| `HcPortal.Tests/FileUploadHelperTests.cs` | 7 test (5 GREEN + 2 SKIP TODO Plan 02) |

## Files Modified

| File | Change |
|------|--------|
| `HcPortal.sln` | Tambah project entry `HcPortal.Tests.csproj` (1 → 2 project) |
| `HcPortal.csproj` | `<DefaultItemExcludes>$(DefaultItemExcludes);HcPortal.Tests\**</DefaultItemExcludes>` cegah root Web SDK glob compile sibling test files |

## xUnit Package Pinning

| Package | Version |
|---------|---------|
| `Microsoft.NET.Test.Sdk` | 17.13.0 |
| `xunit` | 2.9.3 |
| `xunit.runner.visualstudio` | 3.0.1 |
| `coverlet.collector` | 6.0.4 |

## Test Inventory

**5 GREEN (existing helper API):**
- `ValidateCertificateFile_NullFile_ReturnsValid`
- `ValidateCertificateFile_ValidPdf_ReturnsValid`
- `ValidateCertificateFile_ValidJpg_ReturnsValid`
- `ValidateCertificateFile_ValidPng_ReturnsValid`
- `ValidateCertificateFile_UnsupportedExtension_ReturnsInvalid`

**2 SKIP (TODO Plan 02 D-09):**
- `ValidateCertificateFile_ExeRenamedPdf_ReturnsInvalidMagicByte` — await magic byte gate di `ValidateCertificateFile`
- `MatchesMagicByte_JpegAliasMatchesJpg` — await `AssessmentConstants.FileValidation.MatchesMagicByte` helper

## Verification

```
dotnet build HcPortal.sln  → 0 error, 23 pre-existing warning
dotnet test HcPortal.Tests → Passed: 5, Skipped: 2, Failed: 0, Total: 7, Duration: 700ms
```

## Deviation From Plan

- Task 1 acceptance asked `.gitignore` patch — skipped karena `.gitignore` line 42-43 sudah wildcard `[Bb]in/` + `[Oo]bj/` (covers HcPortal.Tests/).
- Tambah patch `HcPortal.csproj` `<DefaultItemExcludes>` — NOT planned tapi REQUIRED untuk fix glob conflict (root Web SDK include sibling test files menyebabkan CS0246). Minimal 2-line change, in-scope (test infra bootstrap mandate).

## Handoff Plan 02

Untuk flip 2 SKIP → GREEN, Plan 02 wajib:
1. Tambah `MagicBytes` dict + `MatchesMagicByte` helper di `AssessmentConstants.FileValidation` (D-09).
2. Tambah magic byte gate di `FileUploadHelper.ValidateCertificateFile` (D-02 + D-03 stream reset).
3. Edit `HcPortal.Tests/FileUploadHelperTests.cs`:
   - Hapus 2 `[Fact(Skip = "...")]` attribute → ganti `[Fact]`
   - Uncomment body 2 test (saat ini block komentar `// TODO test logic Plan 02`)
4. `dotnet test` → expect `Passed: 7, Skipped: 0, Failed: 0`.

## Success Criteria Met

- **SC-6 Foundation:** ✓ 6 test skeleton ada (1 helper `MakeFile` + 7 `[Fact]`). 4 GREEN + 2 SKIP → ready Plan 02 flip.
