# Phase 189: Certificate Actions and Excel Export - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

View/download sertifikat individual + Excel export filtered list untuk Admin/HC. TrainingRecord redirect ke SertifikatUrl, AssessmentSession redirect ke CMP/CertificatePdf.

</domain>

<decisions>
## Implementation Decisions

### Certificate View/Download Actions
- Link icon di kolom baru "Aksi" di tabel sertifikat
- Training tanpa SertifikatUrl: tampilkan dash "-" tanpa link
- Assessment download: new tab (target="_blank") agar user tetap di halaman
- TrainingRecord → redirect ke SertifikatUrl, AssessmentSession → redirect ke CMP/CertificatePdf

### Excel Export
- Export button di header halaman, sebelah tombol "Kembali ke CDP"
- Export button hanya tampil untuk Admin/HC (role-gated via User.IsInRole)
- Export data mengikuti filter aktif (filtered dataset, bukan semua)
- Kolom Excel: sama dengan tabel + tambah kolom SertifikatUrl
- Nama file: `Sertifikat_Export_{tanggal}.xlsx`
- Gunakan ExcelExportHelper.CreateSheet() + ToFileResult()

### Claude's Discretion
- Detail implementasi filter parameter passing ke export action
- Icon choice untuk view/download link

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExcelExportHelper.CreateSheet(workbook, sheetName, headers)` — creates worksheet with bold headers
- `ExcelExportHelper.ToFileResult(workbook, fileName, controller)` — auto-adjust + return FileContentResult
- `CMPController.CertificatePdf(int id)` — existing action for assessment certificate PDF
- `SertifikatRow.SertifikatUrl` — already has URL field for training records
- `FilterCertificationManagement` action (Phase 188) — same filter params reusable for export

### Established Patterns
- ExcelExportHelper used in CDPController, CMPController, AdminController, ProtonDataController
- Pattern: `using var wb = new XLWorkbook(); var ws = ExcelExportHelper.CreateSheet(wb, name, headers); /* populate rows */; return ExcelExportHelper.ToFileResult(wb, filename, this);`
- Role-gating: `User.IsInRole("Admin") || User.IsInRole("HC")`

### Integration Points
- `_CertificationManagementTablePartial.cshtml` — add Aksi column
- `CertificationManagement.cshtml` — add export button in header
- `CDPController.cs` — add ExportSertifikatExcel action

</code_context>

<specifics>
## Specific Ideas

No specific requirements — follow established ExcelExportHelper pattern and existing table action patterns.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>
