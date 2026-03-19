# Phase 202: Renewal Certificate Page (Kelola Data) - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/Admin memiliki halaman khusus di Kelola Data untuk melihat dan mengelola semua sertifikat expired/akan expired yang belum di-renew. Halaman menampilkan daftar dengan filter, aksi renew satuan, dan bulk renew untuk sertifikat dengan kategori sama. Card entry point di Section C Kelola Data hub.

</domain>

<decisions>
## Implementation Decisions

### Tampilan Daftar
- Kolom tabel ringkas: Nama, Judul Sertifikat, Kategori, Sub Kategori, Valid Until, Status, Aksi
- Sorting default: Expired di atas, lalu Akan Expired. Dalam grup status sama, sort by ValidUntil ascending (paling dekat expired di atas)
- Pagination 20 baris per halaman (konsisten dengan CertificationManagement)
- Badge count summary di atas tabel: Expired (N), Akan Expired (N)

### Filter
- 4 dropdown filter: Bagian, Unit, Kategori, Status (Expired/Akan Expired)
- Filter menggunakan AJAX (tabel update tanpa reload halaman), konsisten dengan CertificationManagement
- Default state: semua filter kosong = tampilkan semua sertifikat expired/akan expired

### Bulk Select & Renew
- Checkbox per baris. Setelah checkbox pertama dicentang, checkbox sertifikat dengan kategori berbeda otomatis disabled + tooltip "Hanya sertifikat kategori sama"
- Tombol "Renew Selected" muncul/aktif di atas tabel setelah minimal 1 checkbox dicentang
- Klik Renew Selected langsung redirect ke CreateAssessment tanpa confirm dialog (user review di form CreateAssessment)
- Passing data via query string multi-param: ?renewSessionId=1&renewSessionId=2&... atau campuran renewSessionId+renewTrainingId

### Card di Kelola Data
- Card "Renewal Sertifikat" di Section C dengan badge count (jumlah total sertifikat perlu renew)
- Icon bi-arrow-repeat, warna text-warning (orange) — menarik perhatian karena action item
- Role: Admin dan HC (konsisten dengan card Section C lainnya)

### Navigasi
- Breadcrumb: Kelola Data > Renewal Sertifikat (tanpa tombol kembali eksplisit)

### Claude's Discretion
- Empty state halaman jika tidak ada sertifikat perlu renew
- Exact styling badge status (Expired vs Akan Expired)
- Apakah Sub Kategori juga perlu filter dropdown atau cukup di kolom tabel saja
- Handling jika sertifikat dari TrainingRecord (tidak punya Kategori/SubKategori)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Data Model & Query
- `Models/CertificationManagementViewModel.cs` — SertifikatRow model dengan IsRenewed flag, CertificateStatus enum, DeriveCertificateStatus helper
- `Controllers/CDPController.cs` line 3187 — BuildSertifikatRowsAsync: query yang menghasilkan SertifikatRow, bisa di-reuse/adapt untuk halaman renewal
- `Models/AssessmentSession.cs` — RenewsSessionId, RenewsTrainingId FK columns (Phase 200)

### CreateAssessment Integration (Phase 201)
- `Controllers/AdminController.cs` lines 947-1095 — CreateAssessment GET/POST, sudah support renewSessionId/renewTrainingId query param
- `Views/Admin/CreateAssessment.cshtml` — Form wizard dengan renewal banner

### Kelola Data Hub
- `Views/Admin/Index.cshtml` line 126 — Section C: Assessment & Training, tempat card baru

### Filter Pattern
- `Views/CDP/CertificationManagement.cshtml` — AJAX filter pattern yang sudah ada
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — Partial view pattern untuk AJAX table refresh

### Requirements
- `.planning/REQUIREMENTS.md` — RNPAGE-01 through RNPAGE-05

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SertifikatRow` model: Sudah ada dengan semua field yang dibutuhkan (Nama, Judul, Kategori, SubKategori, ValidUntil, Status, IsRenewed)
- `BuildSertifikatRowsAsync` (CDPController): Query lengkap yang menghasilkan SertifikatRow dari AssessmentSession + TrainingRecord. Bisa di-copy/adapt ke AdminController dengan filter IsRenewed=false dan Status in (Expired, AkanExpired)
- `CertificationManagement` AJAX pattern: Filter dropdown + partial view refresh pattern yang sudah proven
- `DeriveCertificateStatus`: Static helper untuk menghitung status dari ValidUntil

### Established Patterns
- AJAX filter: CDPController CertificationManagement menggunakan partial view + jQuery AJAX untuk filter tanpa reload
- ViewBag dropdown population: Pattern standar di AdminController untuk Bagian, Unit, Kategori
- Pagination: PaginationHelper sudah ada (Phase 199)

### Integration Points
- `AdminController`: Tambah action RenewalCertificate (GET) dan RenewalCertificateFilter (POST/GET untuk AJAX)
- `Views/Admin/Index.cshtml` Section C: Tambah card baru setelah card yang ada
- CreateAssessment: Sudah siap menerima renewSessionId/renewTrainingId dari redirect

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 202-renewal-certificate-page-kelola-data*
*Context gathered: 2026-03-19*
