# Phase 92: Admin CPDP File Management - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can manage CPDP document files per section — uploading, downloading, archiving, and viewing file history — with the ability to add or remove section tabs. This mirrors the existing KkjMatrix file management pattern exactly, but for CPDP documents stored in CpdpFiles table (created in Phase 91).

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion (user delegated all decisions)

User confirmed: follow the KkjMatrix pattern exactly. All implementation details mirror the established KKJ file management system:

- **Page structure**: New page at `/Admin/CpdpFiles` with tabbed sections (KkjBagian records). The existing `/Admin/CpdpItems` spreadsheet editor stays untouched at its current URL — it remains the KKJ-IDP Mapping Editor. The new CpdpFiles page is a separate entry point for file-based CPDP document management.
- **Tab layout**: Bootstrap nav-tabs per KkjBagian (RFCC, GAST, NGP, DHT), identical to KkjMatrix.cshtml tab structure. Each tab shows active (non-archived) CpdpFile records for that bagian.
- **Upload flow**: Separate upload page `/Admin/CpdpUpload?bagianId={id}` mirroring KkjUpload.cshtml — drag-drop zone, bagian selector, keterangan field, PDF/Excel only, 10MB max.
- **Download**: `GET /Admin/CpdpFileDownload/{id}` — serves file from `/uploads/cpdp/{bagianId}/` path, mirrors KkjFileDownload.
- **Archive (soft-delete)**: `POST /Admin/CpdpFileArchive` — sets `IsArchived = true`, file moves to history view. Mirrors KkjFileDelete.
- **History view**: `/Admin/CpdpFileHistory/{bagianId}` — shows archived files with download option. Mirrors KkjFileHistory.cshtml.
- **Bagian management**: Add/Delete bagian controls on the CPDP page, sharing KkjBagian records. Adding a bagian on CPDP also shows on KKJ (they share the same table). Delete only allowed if bagian has zero files in both KKJ and CPDP.
- **Storage path**: `/uploads/cpdp/{bagianId}/{timestamp}_{safeName}.{ext}` — mirrors KKJ's `/uploads/kkj/` structure.
- **Authorization**: All actions use `[Authorize(Roles = "Admin, HC")]` except download which uses `[Authorize]` (any authenticated user), same as KKJ pattern.

</decisions>

<specifics>
## Specific Ideas

- Follow KkjMatrix.cshtml as the exact template for CpdpFiles.cshtml (tabs, file table, upload/history buttons, bagian management bar)
- Follow KkjUpload.cshtml as the exact template for CpdpUpload.cshtml (drag-drop zone, file validation, bagian selector)
- Follow KkjFileHistory.cshtml as the exact template for CpdpFileHistory.cshtml (archived files table with download)
- Success criteria #5 requires bagian changes to reflect on both admin page AND worker CMP/Mapping page — this is already the case since both use KkjBagian records

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- KkjMatrix.cshtml (Views/Admin/): Complete reference template for tabbed file management page
- KkjUpload.cshtml (Views/Admin/): Upload page with drag-drop, validation, bagian selector
- KkjFileHistory.cshtml (Views/Admin/): Archived files listing with download
- AdminController KkjUpload/KkjFileDownload/KkjFileDelete/KkjFileHistory actions: Exact controller pattern to copy
- CpdpFile entity (Models/KkjModels.cs:31-44): Already created in Phase 91
- DbSet<CpdpFile> in ApplicationDbContext: Already registered in Phase 91

### Established Patterns
- File upload: IFormFile + physical save to wwwroot/uploads/{type}/{bagianId}/ + DB record
- Soft-delete archive: IsArchived bool flag, not physical deletion
- Tab navigation: Bootstrap nav-tabs with KkjBagian-driven tabs, selectedBagianId for active tab
- Add/Delete bagian: AJAX POST with antiforgery token, JSON responses
- File download: Read bytes from disk, serve with correct content-type

### Integration Points
- AdminController.cs: Add 6 new actions (CpdpFiles, CpdpUpload GET/POST, CpdpFileDownload, CpdpFileArchive, CpdpFileHistory)
- Views/Admin/: Add 3 new views (CpdpFiles.cshtml, CpdpUpload.cshtml, CpdpFileHistory.cshtml)
- Admin/Index hub: Add card/link to access the new CpdpFiles page
- KkjBagian add/delete: Already exists on KkjMatrix — CPDP page reuses same backend endpoints or adds parallel ones

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 92-admin-cpdp-file-management*
*Context gathered: 2026-03-03*
