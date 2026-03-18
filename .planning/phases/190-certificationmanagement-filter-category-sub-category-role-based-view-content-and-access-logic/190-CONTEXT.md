# Phase 190: CertificationManagement Filter Category/Sub-Category, Role-Based View Content and Access Logic - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Enhance halaman CertificationManagement dengan:
1. Filter cascade Category → Sub-Category menggunakan data dari tabel AssessmentCategory
2. Kolom Sub-Category baru di tabel
3. Konten view yang berbeda berdasarkan role level (kolom visible, summary cards, filter availability)
4. Penyesuaian scope L5 khusus page ini (hanya data sendiri, bukan coachee)

</domain>

<decisions>
## Implementation Decisions

### Filter Category/Sub-Category
- Sumber filter: tabel `AssessmentCategory` (parent/children hierarchy dari Phase 195)
- Cascade AJAX: pilih Category (parent) → fetch Sub-Category (children) — pattern sama dengan Bagian → Unit
- Filter Category/Sub-Category **hanya berlaku untuk Assessment rows**. Training rows yang Kategori-nya tidak ada di AssessmentCategory akan tampil saat "Semua Kategori" tapi hilang saat filter spesifik dipilih
- TrainingRecord.Kategori tetap string bebas — tidak perlu migrasi atau mapping

### Kolom Sub-Category di Tabel
- Tambah kolom "Sub Kategori" di tabel, posisi setelah kolom Kategori
- Hanya terisi untuk Assessment rows yang punya sub-category
- Training rows tampilkan "-" di kolom ini

### Role-Scoped Data Query (Perubahan dari Phase 186)
- **L1-3:** Full access, semua data
- **L4:** Data di bagian (section) user saja
- **L5:** Hanya data **diri sendiri** (BERBEDA dari Phase 186 default yang include coachee)
- **L6:** Hanya data diri sendiri

### Role-Based Filter Bar
- **L1-3:** Semua filter aktif (Bagian, Unit, Status, Tipe, Category, Sub-Category, Search)
- **L4:** Bagian dropdown **disabled** dan pre-filled dengan bagian user. Unit dropdown aktif. Filter lainnya aktif.
- **L5/L6:** Filter Bagian dan Unit **disabled** (grey out). Filter Status, Tipe, Category, Sub-Category, Search tetap aktif.

### Role-Based View Content
- **L1-4:** Lihat semua kolom tabel + summary cards (Total, Aktif, Akan Expired, Expired)
- **L5/L6:** Kolom Nama, Bagian, Unit **disembunyikan**. Summary cards **tidak ditampilkan**.

### Access & Permissions
- Semua authenticated user tetap bisa akses halaman (CDPController class-level [Authorize])
- Export Excel tetap Admin/HC only ([Authorize(Roles = "Admin, HC")])
- Row actions tetap: Lihat/Download sertifikat saja

### Claude's Discretion
- Endpoint name untuk cascade Category → Sub-Category AJAX
- Bagaimana pass roleLevel ke view (ViewBag vs ViewModel property)
- Sub-Category field mapping untuk AssessmentSession rows (lookup AssessmentCategory.Children by parent Category name)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Data Model & Hierarchy
- `Models/AssessmentCategory.cs` — Parent/Children hierarchy, ParentId, SignatoryUserId
- `Models/CertificationManagementViewModel.cs` — SertifikatRow, CertificateStatus enum, ViewModel
- `Models/AssessmentSession.cs` — Category field (string), GenerateCertificate, IsPassed
- `Models/TrainingRecord.cs` — Kategori field (string bebas)

### Existing Implementation (Phase 185-189)
- `Controllers/CDPController.cs` lines 3026-3280 — CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel, BuildSertifikatRowsAsync
- `Views/CDP/CertificationManagement.cshtml` — Full page with filter bar + AJAX JS
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — Table partial view

### Role Scoping Pattern
- `Controllers/CDPController.cs` — GetCurrentUserRoleLevelAsync() method, UserRoles.GetRoleLevel()
- `Controllers/CDPController.cs` line 288 — GetCascadeOptions (Bagian → Unit AJAX pattern)

### Shared Helpers
- `Helpers/PaginationHelper.cs` — Calculate() for pagination
- `Helpers/ExcelExportHelper.cs` — CreateSheet() + ToFileResult()

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GetCascadeOptions` action: Pattern AJAX cascade sudah ada untuk Bagian → Unit, bisa di-replika untuk Category → Sub-Category
- `AssessmentCategory` model: Sudah punya Parent/Children navigation properties (Phase 195)
- `BuildSertifikatRowsAsync()`: Helper yang sudah handle role-scoping, perlu modifikasi untuk L5 scope change

### Established Patterns
- AJAX filter: fetch partial view → replace container innerHTML → update summary cards via data attributes
- Cascade dropdown: parent change event → fetch JSON → populate child dropdown
- Role-scoping: GetCurrentUserRoleLevelAsync() returns (user, roleLevel) tuple

### Integration Points
- `SertifikatRow` model perlu field baru: SubKategori
- `CertificationManagementViewModel` mungkin perlu property RoleLevel untuk conditional rendering di view
- `BuildSertifikatRowsAsync()` perlu parameter atau branching untuk L5 scope override
- Filter action perlu parameter baru: category, subCategory
- AJAX endpoint baru untuk Category → Sub-Category cascade

</code_context>

<specifics>
## Specific Ideas

- Filter Bagian/Unit pattern yang sudah ada (cascade + disabled state) jadi template untuk Category → Sub-Category
- L5 scope override khusus page ini — tidak mengubah BuildSertifikatRowsAsync default, tapi pass parameter atau buat variant
- Summary cards disembunyikan via conditional rendering berdasarkan roleLevel, bukan via CSS class

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 190-certificationmanagement-filter-category-sub-category-role-based-view-content-and-access-logic*
*Context gathered: 2026-03-18*
