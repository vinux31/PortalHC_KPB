# Phase 214: SubCategory Model + CRUD - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Tambah kolom SubKategori di TrainingRecord, dropdown Kategori dan SubKategori di AddTraining/EditTraining mengambil data dari AssessmentCategories (ManageCategories), dan tambah kolom SubKategori di ImportTraining Excel.

</domain>

<decisions>
## Implementation Decisions

### Data Model (MDL-01)
- **D-01:** Tambah kolom `SubKategori` (string, nullable) di tabel TrainingRecords — migrasi database
- **D-02:** SubKategori bersifat opsional — tidak semua training harus punya sub kategori

### Dropdown Kategori + SubKategori di AddTraining/EditTraining
- **D-03:** Dropdown Kategori di AddTraining/EditTraining **tidak lagi hardcode** — diambil dari AssessmentCategories (parent categories, IsActive)
- **D-04:** Dropdown SubKategori muncul setelah Kategori, **dependent** — hanya menampilkan child categories dari parent yang dipilih
- **D-05:** SubKategori di-disable saat Kategori belum dipilih
- **D-06:** Sumber data: AssessmentCategories parent-child hierarchy (ManageCategories page)

### ImportTraining
- **D-07:** Template Excel ImportTraining ditambah kolom SubKategori (opsional)
- **D-08:** Import logic menerima SubKategori dari Excel dan menyimpan ke TrainingRecord

### Scope Clarification
- **D-09:** Phase ini TIDAK mengubah Team View filter — itu Phase 215
- **D-10:** Phase ini TIDAK menambahkan assessment records ke data filterable — itu Phase 215

### Claude's Discretion
- Migrasi naming convention
- JS approach untuk dependent dropdown (inline atau fetch API)
- Validasi SubKategori terhadap AssessmentCategories atau simpan as-is

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Model & Database
- `Models/TrainingRecord.cs` — Model yang akan ditambah field SubKategori
- `Data/ApplicationDbContext.cs` — DbContext, TrainingRecord registration (line 19, config lines 97-119)
- `Models/AssessmentCategory.cs` — Parent-child hierarchy model (ParentId, IsActive, Name)

### Forms
- `Views/Admin/AddTraining.cshtml` — Form tambah training, dropdown Kategori hardcode (lines 90-103)
- `Views/Admin/EditTraining.cshtml` — Form edit training, dropdown Kategori
- `Controllers/AdminController.cs` — AddTraining/EditTraining actions

### Import
- `Views/Admin/ImportTraining.cshtml` — Import form + template documentation (line 181: Kategori column)
- `Controllers/AdminController.cs` — ImportTraining + DownloadImportTemplate actions

### Reference Pattern
- `Views/Admin/ManageCategories.cshtml` — CRUD kategori dengan parent-child hierarchy
- `Controllers/AdminController.cs` — ManageCategories, AddCategory actions — pattern untuk query parent/child

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AssessmentCategories sudah punya parent-child hierarchy (ParentId) — tinggal query untuk populate dropdown
- ManageCategories view sudah render parent → child → grandchild — pattern query bisa di-reuse
- ImportTraining sudah punya pattern download template + process Excel

### Established Patterns
- Dropdown Kategori di AddTraining: `<select asp-for="Kategori">` dengan hardcode options (OJT, IHT, MANDATORY, PROTON, SERTIFIKASI, ISS, OSS)
- Import Excel pattern: column mapping di AdminController ImportTraining action
- Tom-select library sudah di-load di ManageCategories — bisa dipakai untuk searchable dropdown

### Integration Points
- AddTraining action perlu ViewBag/ViewData untuk pass AssessmentCategories ke view
- EditTraining action perlu sama + pre-select current Kategori & SubKategori
- ImportTraining action perlu validasi SubKategori (opsional)

</code_context>

<specifics>
## Specific Ideas

- Dropdown Kategori populate dari AssessmentCategories where ParentId == null && IsActive
- Dropdown SubKategori populate dari AssessmentCategories where ParentId == selectedCategory && IsActive
- Dependent dropdown bisa pakai JS change event pada Kategori → filter SubKategori options
- Alternatif: render semua subcategories sebagai data attributes, filter client-side (menghindari API call)

</specifics>

<deferred>
## Deferred Ideas

- **Phase 215**: Team View filter Sub Category + assessment records masuk data filterable
- **Phase 216**: Export Fixes & Display Enhancement (badge IsExpiringSoon, export alignment)

</deferred>

---

*Phase: 214-subcategory-model-crud*
*Context gathered: 2026-03-21*
