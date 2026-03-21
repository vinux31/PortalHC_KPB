# Phase 218: RecordsWorkerDetail Redesign & ImportTraining Update - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Redesign tabel RecordsWorkerDetail — hapus kolom Score dan Sertifikat, tambah kolom Kategori/SubKategori dan kolom Action (Detail + Download Sertifikat), tambah filter SubCategory cascade, dan update ImportTraining form/logic sesuai perubahan model (urutan kolom diperbaiki, tambah kolom baru).

</domain>

<decisions>
## Implementation Decisions

### Struktur Kolom Tabel RecordsWorkerDetail
- **D-01:** Kolom baru (7 kolom): Tanggal | Nama Kegiatan | Tipe | Kategori | Sub Kategori | Status | Action
- **D-02:** Kolom Score dihapus dari tabel
- **D-03:** Kolom Sertifikat lama dihapus, diganti kolom Action
- **D-04:** Kolom Tipe (badge Assessment/Training) tetap dipertahankan
- **D-05:** Assessment rows — Kategori diambil dari `AssessmentSession.Category`, SubKategori = — (tidak ada di model)
- **D-06:** Training rows — Kategori dari `TrainingRecord.Kategori`, SubKategori dari `TrainingRecord.SubKategori`
- **D-07:** UnifiedTrainingRecord perlu di-update: populate Kategori untuk Assessment rows dari AssessmentSession.Category

### Kolom Action
- **D-08:** Assessment rows — TIDAK ada tombol Detail, hanya Download Sertifikat jika `GenerateCertificate=true`
- **D-09:** Training rows — tombol Detail + Download Sertifikat (jika `SertifikatUrl` ada)
- **D-10:** Tombol Detail Training membuka **modal popup** dengan semua field detail (penyelenggara, kota, tanggal mulai/selesai, nomor sertifikat, dll)
- **D-11:** Download Sertifikat Assessment mengarah ke `CMP/Certificate/{sessionId}` (existing)
- **D-12:** Download Sertifikat Training mengarah ke `SertifikatUrl` (existing file download)

### Filter SubCategory Cascade
- **D-13:** Filter SubCategory cascade **dependent** pada filter Kategori — disabled sampai Kategori dipilih
- **D-14:** Sumber data SubCategory dari **master AssessmentCategories** (bukan dari data rows)
- **D-15:** Perlu pass AssessmentCategories data ke view (ViewBag atau similar) untuk populate SubCategory dropdown

### ImportTraining Update
- **D-16:** Update kedua view: `Views/CMP/ImportTraining.cshtml` dan `Views/Admin/ImportTraining.cshtml`
- **D-17:** Urutan kolom template Excel baru: NIP, Judul, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Penyelenggara, Kota, Status, ValidUntil, NomorSertifikat
- **D-18:** Kolom baru ditambahkan: SubKategori, Kota, TanggalMulai, TanggalSelesai
- **D-19:** Import logic dan DownloadImportTemplate action perlu update sesuai urutan dan kolom baru
- **D-20:** Format notes di view perlu update untuk reflect kolom baru

### Claude's Discretion
- Modal design untuk Training Detail popup
- JS implementation untuk cascade filter (inline vs fetch API)
- Handling jika Kategori filter dipilih tapi tidak ada SubCategory match di master data

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Tabel RecordsWorkerDetail
- `Views/CMP/RecordsWorkerDetail.cshtml` — View tabel yang akan di-redesign (7 kolom baru)
- `Controllers/CMPController.cs` — Action RecordsWorkerDetail (line ~448), perlu pass AssessmentCategories
- `Models/UnifiedTrainingRecord.cs` — ViewModel unified, perlu populate Kategori untuk Assessment rows
- `Services/WorkerDataService.cs` — GetUnifiedRecords, perlu update mapping untuk Assessment Kategori

### Model & Data
- `Models/AssessmentSession.cs` — Punya `Category` field (line 16) untuk Assessment rows
- `Models/TrainingRecord.cs` — Punya `Kategori` dan `SubKategori` fields
- `Models/AssessmentCategory.cs` — Parent-child hierarchy untuk cascade filter

### ImportTraining
- `Views/CMP/ImportTraining.cshtml` — CMP import view, perlu update format notes dan form
- `Views/Admin/ImportTraining.cshtml` — Admin import view, perlu update sama
- `Controllers/AdminController.cs` — ImportTraining + DownloadImportTemplate actions
- `Controllers/CMPController.cs` — CMP-side ImportTraining + DownloadImportTrainingTemplate actions

### Prior Phase Context
- `.planning/phases/214-subcategory-model-crud/214-CONTEXT.md` — Phase 214 decisions (SubKategori model, dropdown pattern, import template)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `UnifiedTrainingRecord` model sudah punya field Kategori, tapi belum populate untuk Assessment rows
- `ExcelExportHelper.CreateSheet()` — bisa reuse untuk template download
- Existing filter bar di RecordsWorkerDetail (search, category, year, type) — extend dengan SubCategory
- AssessmentCategories parent-child hierarchy sudah ada dan dipakai di Phase 214

### Established Patterns
- Client-side filtering via data attributes pada `<tr>` elements (existing di RecordsWorkerDetail)
- Dropdown cascade pattern sudah diimplementasi di AddTraining/EditTraining (Phase 214)
- ImportTraining template download pattern (DownloadImportTemplate / DownloadImportTrainingTemplate)

### Integration Points
- `WorkerDataService.GetUnifiedRecords()` — perlu update untuk populate Kategori dari AssessmentSession.Category
- RecordsWorkerDetail action — perlu query AssessmentCategories dan pass ke view
- Filter JS — perlu extend untuk SubCategory cascade + data-subcategory attribute pada rows

</code_context>

<specifics>
## Specific Ideas

- Modal Training Detail: tampilkan semua field yang tidak terlihat di tabel (penyelenggara, kota, tanggal mulai/selesai, nomor sertifikat, valid until)
- Filter SubCategory cascade disable/enable mirip pattern AddTraining dropdown dari Phase 214
- Assessment rows di tabel: hanya tombol Download Sertifikat di kolom Action (tanpa Detail)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 218-recordsworkerdetail-redesign-importtraining-update*
*Context gathered: 2026-03-21*
