# Phase 83: Master Data QA - Context

**Gathered:** 2026-03-02 (updated 2026-03-03)
**Status:** Ready for planning

<domain>
## Phase Boundary

Verify all master data management features in the Kelola Data hub work correctly end-to-end for Admin and HC roles. Fix bugs found during QA. Features: KKJ Matrix editor, CPDP File Management (Admin/CpdpFiles), Silabus CRUD, Coaching Guidance file management, Worker management (CRUD + import/export).

</domain>

<decisions>
## Implementation Decisions

### QA Depth & Scope
- Happy path + validation: test normal CRUD flows plus input validation (empty fields, duplicates, invalid data)
- Code review first: Claude reviews controller/view code, identifies potential bugs, fixes them proactively, THEN user verifies in browser
- 3 known high-priority bugs to fix (discovered during QA):
  1. **DeleteWorker ProtonFinalAssessment** — Worker deletion fails due to FK constraint on ProtonFinalAssessment table
  2. **SilabusDelete FK guard** — Silabus deletion fails due to FK constraint (no guard/cascade)
  3. **KkjBagianDelete archived count** — KKJ Bagian deletion doesn't account for archived records in count
- Filter behavior: test bagian/unit filter switching to verify data loads correctly and saves don't cross-contaminate

### Bug Fix Approach
- Fix inline: fix bugs immediately as part of the QA plan (review code → fix bugs → commit → user verifies in browser)
- Big bugs: fix anything that's a localized change (under ~100 lines). Only flag truly architectural issues for discussion
- Verification: manual browser test after each plan. Use /gsd:verify-work 83 at the end for a formal pass

### FK Deletion Strategy
- **Worker**: Soft delete — set `IsActive = false`, NOT hard delete
  - Label tombol berubah dari "Hapus" → "Nonaktifkan"
  - Worker inactive disembunyikan dari list default
  - Toggle checkbox "Tampilkan Inactive" untuk melihat inactive workers
  - Tombol "Aktifkan Kembali" tersedia di list inactive
  - Data worker inactive (assessment, coaching, IDP) disembunyikan dari report/dashboard
- **Silabus**: Soft delete — set `IsActive = false`, sama pattern dengan Worker
  - Label tombol "Nonaktifkan"
  - Filter toggle + reactivate tersedia
  - Silabus inactive disembunyikan dari dropdown pilihan di Plan IDP dan Coaching Proton
  - Plan IDP yang sudah referensikan Silabus inactive: item disembunyikan dari tampilan
- **CpdpFiles**: Archive pattern sudah ada (`IsArchived`), tidak perlu hard delete tambahan
- **KKJ Bagian/Column**: Hard delete dengan guard — hanya active records yang block deletion

### Archived Record Logic (KKJ)
- Archived records TIDAK mencegah penghapusan Bagian/Column
- Hanya active records yang block deletion
- Archived records ikut cascade delete bersama parent
- Konfirmasi dialog menyebutkan jumlah archived records yang akan ikut terhapus: "Hapus Bagian X? 3 archived records akan ikut terhapus. Tindakan ini tidak bisa dibatalkan."
- Pesan block spesifik dengan jumlah active records: "Bagian ini memiliki 5 KKJ Matrix aktif. Nonaktifkan atau hapus data KKJ terkait terlebih dahulu."
- AuditLog detail: log cascaded archived records count
- Pattern konsisten untuk semua KKJ delete operations (Bagian, Column, row)
- Claude cek kode CMP/Kkj untuk filter behavior (active only vs all) — keputusan based on findings

### Inactive Worker Impact on Active Data
- Nonaktifkan Worker → coaching assignment auto-close, assessment auto-cancel
- Konfirmasi detail menyebutkan dampak: "Nonaktifkan Worker [nama]? 2 coaching aktif akan ditutup, 1 assessment akan dibatalkan."
- Login di-disable: Worker inactive tidak bisa login, pesan "Akun Anda tidak aktif, hubungi Admin."

### Delete Error UX
- Sukses nonaktifkan: Toast/alert "Worker [nama] berhasil dinonaktifkan." (TempData pattern)
- Error block delete: Modal/dialog popup dengan detail jumlah records
- Konfirmasi: Dialog Ya/Tidak (tidak perlu ketik nama)
- Bahasa: Semua pesan dalam **Bahasa Indonesia**

### Worker Export & Inactive
- Export Excel mengikuti filter halaman — kalau toggle "Tampilkan Inactive" aktif, inactive workers ikut ter-export
- Export includes kolom Status jika inactive workers termasuk

### Worker Import & Inactive Handling
- Import Excel menemukan Worker dengan NIP/email yang sudah inactive → masuk **review list**
- Di halaman hasil import: X berhasil, Y gagal, Z perlu review (inactive matches)
- Tombol "Reactivate" per baris pada review list
- Reactivate → Worker di-aktifkan kembali + data di-update dari Excel (data Excel lebih baru)
- Worker yang di-reactivate mulai fresh — coaching/assessment lama yang auto-closed tetap closed
- Template Excel import tanpa kolom Status — import selalu buat Worker active

### Test Data Setup
- Database has production-like data across most features — test against existing data
- Worker import: Claude creates a small test Excel file with 5-10 workers (valid + invalid rows) for testing
- Coaching Guidance: Claude checks code for allowed file types and tests accordingly

### Cross-Feature Links
- Full round-trip verification: edit data in Admin → verify it appears correctly in user-facing views (CMP/Kkj, CMP/Mapping, CDP/CoachingProton)
- Silabus: verify dropdown options actually populate from Silabus data in Plan IDP and Coaching Proton
- CPDP Files (Admin/CpdpFiles) → CMP/Mapping view: already connected, CMP/Mapping filters `!IsArchived`
- Export: Claude decides based on code review whether export contents need manual verification vs just download works

### Soft Delete Consistency Summary
| Entity | Strategy | Field | Reactivate |
|--------|----------|-------|------------|
| Worker | Soft delete | `IsActive` | Yes, via list + import |
| Silabus | Soft delete | `IsActive` | Yes, via list |
| CpdpFiles | Archive | `IsArchived` (existing) | No (archive is sufficient) |
| KKJ Bagian | Hard delete | N/A (guard check) | N/A |
| KKJ Column | Hard delete | N/A (guard check) | N/A |
| Coaching Guidance | Hard delete | N/A | N/A (file re-uploadable) |

### Claude's Discretion
- Exact test scenarios per feature (number of test rows, specific validation cases)
- Coaching Guidance file type testing scope
- Export content verification depth
- Loading skeleton or UX improvements if encountered during QA
- CMP/Kkj filter behavior (active only vs all) — Claude checks code and decides

</decisions>

<specifics>
## Specific Ideas

- User prefers testing organized by use-case flows (not page-by-page or role-by-role)
- Pattern: Claude analyzes code → user verifies in browser → Claude fixes bugs
- Reference: Worker import/export pattern from AdminController.cs (ImportWorkers + DownloadImportTemplate)
- KKJ-IDP Mapping is now managed via Admin/CpdpFiles page (file upload/archive), connected to CMP/Mapping view

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController.cs`: KKJ Matrix (lines 48-260), CPDP Files (lines 294-468), Worker CRUD (lines 2972-3700+), AuditLog (lines 2135+)
- `ProtonDataController.cs`: Silabus (lines 108-300+), CoachingGuidance (lines 338-500+)
- `AuditLogService`: All admin actions already log via `_auditLog.LogAsync()` — can verify audit trail during QA
- ClosedXML: Used for all Excel import/export (Worker import, KKJ-IDP Mapping export, Coach-Coachee export)
- `CpdpFile` model: Already has `IsArchived` field for soft delete pattern
- `CpdpFileArchive` action: Existing soft delete endpoint (sets `IsArchived = true`)
- `CpdpFileHistory` action: Shows archived files per bagian

### Established Patterns
- Spreadsheet editors: `[FromBody] List<T>` for bulk save, JSON serialized to ViewBag for initial load
- CRUD pattern: GET loads view, POST actions return `Json(new { success, message })` for AJAX
- Role gating: `[Authorize(Roles = "Admin, HC")]` on all admin CRUD actions
- File uploads: `IFormFile` parameter, stored in wwwroot (`/uploads/cpdp/{bagianId}/`)
- Archive pattern: `CpdpFile.IsArchived` — file retained on disk, hidden from active views

### Integration Points
- KKJ Matrix → CMP/Kkj view (same `KkjMatrixItem` model, same DbContext)
- CPDP Files (Admin/CpdpFiles) → CMP/Mapping view (queries `CpdpFiles.Where(!IsArchived)`, grouped by BagianId)
- CMP/Mapping: Role-based filtering (RoleLevel >= 5 sees only own section)
- Silabus → Plan IDP and Coaching Proton pages (queried for dropdown/selection options)
- Coaching Guidance → Plan IDP (file download links)
- Worker data → used across all features (assessment, coaching, IDP)

</code_context>

<deferred>
## Deferred Ideas

- Package question management feature (CMP has ImportPackageQuestions.cshtml, ManagePackages.cshtml, PreviewPackage.cshtml) — user noted this during Phase 82 UAT, consider for future phase
- **PlanIdp Bagian/Unit filter bug** — deferred to Phase 86 (PlanIdp development scope)
- **Coachee guidance download bug** — deferred to Phase 86 (PlanIdp development scope)

</deferred>

---

*Phase: 83-master-data-qa*
*Context gathered: 2026-03-02 (updated 2026-03-03)*
