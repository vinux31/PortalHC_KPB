# Phase 259: Export Categories (Excel & PDF) + Bug Fix Signatory - Context

**Gathered:** 2026-03-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Halaman ManageCategories (`/Admin/ManageCategories`): tambah fitur export Excel & PDF, dan fix bug kolom Penandatangan yang menampilkan "—" padahal sudah di-set untuk sub-kategori.

</domain>

<decisions>
## Implementation Decisions

### Bug Fix
- **D-01:** Root cause: `SetCategoriesViewBag()` (line 788-794) hanya `.Include(c => c.Signatory)` untuk root. Children/grandchildren tidak include Signatory. Fix: tambah `.Include(c => c.Children).ThenInclude(ch => ch.Signatory)` dan `.Include(c => c.Children).ThenInclude(ch => ch.Children).ThenInclude(gc => gc.Signatory)`.

### Data Scope
- **D-02:** Export sesuai tampilan — saat ini ManageCategories menampilkan semua kategori (aktif + nonaktif), jadi export juga semua. Kolom Status membedakan.

### Hierarchy Display
- **D-03:** Flat rows dengan kolom "Kategori Induk" terpisah (berisi nama parent, kosong jika root). Tidak pakai indent — bersih dan sortable.

### PDF Styling
- **D-04:** Plain table — header bold, border standar, tanpa warna/logo. Konsisten dengan export PDF lain di project.

### Excel Styling
- **D-05:** Header biru muda (light blue) + bold, auto-fit kolom. Sama dengan pattern `ExportWorkers`.

### Authorization
- **D-06:** `[Authorize(Roles = "Admin, HC")]` — sama dengan akses ManageCategories.

### Filename
- **D-07:** Format `KategoriAssessment_yyyyMMdd_HHmmss.xlsx` / `.pdf`.

### Query Strategy
- **D-08:** Export actions query ulang dari DB (bukan ViewBag) agar bisa include Signatory dan Parent dengan benar.

### No New Packages
- **D-09:** ClosedXML dan QuestPDF sudah ada di project.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Export Pattern References
- `Helpers/ExcelExportHelper.cs` — Helper CreateSheet + ToFileResult untuk Excel export
- `Controllers/AdminController.cs` lines 4703-4768 — `ExportWorkers()` sebagai reference implementation Excel
- `Controllers/CDPController.cs` lines 2447-2510 — `ExportProgressPdf()` sebagai reference implementation QuestPDF

### Bug Fix Reference
- `Controllers/AdminController.cs` lines 787-794 — `SetCategoriesViewBag()` query yang perlu di-fix
- `Models/AssessmentCategory.cs` — Model dengan Signatory navigation property

### View Reference
- `Views/Admin/ManageCategories.cshtml` — View yang perlu ditambah tombol export

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExcelExportHelper.CreateSheet()` + `ToFileResult()` — pattern established, tinggal pakai
- QuestPDF sudah configured di project (license, etc.)
- Bootstrap Icons (`bi-*`) untuk icon tombol

### Established Patterns
- Excel: `XLWorkbook` → `CreateSheet` → populate rows → `ToFileResult` (lihat ExportWorkers)
- PDF: `QuestPDF.Fluent.Document.Create` → `container.Page` → table layout (lihat ExportProgressPdf)
- Export button: dropdown `btn-group` dengan `btn-outline-success`

### Integration Points
- `AdminController.cs` — tambah 2 action methods baru
- `ManageCategories.cshtml` header — tambah dropdown button sebelah "Tambah Kategori"

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches following established patterns.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 259-export-categories-bugfix-signatory*
*Context gathered: 2026-03-26*
