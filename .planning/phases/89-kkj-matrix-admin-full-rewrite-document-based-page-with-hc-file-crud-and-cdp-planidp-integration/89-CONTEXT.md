# Phase 90: KKJ Matrix Admin Full Rewrite — Document-Based Page - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Replace the Admin KKJ Matrix table-based editor and CMP KKJ Matrix worker view with a document/file-based system. Admin/HC upload PDF and Excel files per bagian. Workers view and download files. All KKJ DB tables (KkjMatrixItem, KkjTargetValue, KkjColumn, PositionColumnMapping) are dropped. Assessment flow KKJ logic (PositionTargetHelper) is also removed. Phase 88 (Excel Import) is obsoleted and should be removed from roadmap.

**CORRECTION:** CDP PlanIDP is NOT related to KKJ — PlanIDP connects to Admin Silabus & Coaching Guidance. KKJ only connects Admin KKJ Matrix ↔ CMP KKJ Matrix. Remove PlanIDP from phase scope.

</domain>

<decisions>
## Implementation Decisions

### File Format & Limits
- Accepted file types: PDF (.pdf) and Excel (.xlsx, .xls) only
- Max file size: 10MB per file
- Multiple files per bagian (not 1:1)

### File Storage & Versioning
- When admin uploads new file, old files with same purpose move to archive/history
- Active files are the latest uploads; archived files accessible via history view
- Claude's discretion on storage approach (DB model like KkjFile vs filesystem-only)

### Admin Page Layout (Views/Admin/KkjMatrix.cshtml)
- Tab navigation per bagian (dynamic from KkjBagian.DisplayOrder in DB)
- Each tab shows: file list table (name, type, size, upload date, uploader) + actions (download, delete, view history)
- Upload button per tab → navigates to **separate upload page**
- Bagian management inline: rename/delete per tab, "Tambah Bagian" button alongside tabs
- Admin can CRUD bagians (KkjBagian model stays in DB)

### Upload Page (separate page)
- Form fields: file picker, title/keterangan (optional text), bagian selector dropdown
- Validation: PDF/Excel only, max 10MB
- After successful upload → redirect back to KkjMatrix page at the uploaded bagian's tab, with success toast

### CMP/Kkj Worker View (Views/CMP/Kkj.cshtml)
- Rewrite to file list + download (no more competency table)
- Role logic same as Phase 89: L1-L4 see all bagians dropdown, L5-L6 see own bagian only
- Workers can view list of files and download them

### DB Cleanup — Drop All KKJ Tables
- Drop tables via EF migration: KkjMatrices, KkjTargetValues, KkjColumns, PositionColumnMappings
- Remove model classes: KkjMatrixItem, KkjTargetValue, KkjColumn, PositionColumnMapping
- KkjBagian model STAYS (needed for bagian tabs/CRUD)
- Remove PositionTargetHelper entirely
- Remove all GetTargetLevel/GetTargetLevelAsync usage from assessment flow

### Permissions
- Admin + HC: upload, delete, manage files and bagians
- All users: download/view files (role-filtered by bagian)

### Navigation Updates
- Update link descriptions in CMP hub and Kelola Data hub to reflect file-based KKJ

### Phase 88 Impact
- Phase 88 (KKJ Matrix Excel Import) is obsoleted — should be removed from roadmap
- Excel import to DB table no longer relevant since tables are being dropped

### Claude's Discretion
- File storage approach (KkjFile DB model vs filesystem scan)
- KkjSectionSelect.cshtml — keep or delete
- File naming convention on server
- Archive/history UI design
- Cleanup of related code (SeedMasterData KKJ section, etc.)

</decisions>

<specifics>
## Specific Ideas

- "hapus semua code di page KkjMatrix, susun ulang dari awal" — full clean rewrite, no legacy code carried over
- "full dokumen saja tanpa table" — no competency table, document/file management only
- Upload via separate page (not modal or inline), with bagian dropdown in form
- Tab-based bagian navigation on admin page (not cards, not dropdown)
- User wants the upload page pattern similar to ImportWorkers flow (button → separate page → process → redirect back)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- KkjBagian model + CRUD actions (KkjBagianAdd, KkjBagianSave, KkjBagianDelete) — keep for bagian management
- ImportWorkers pattern in AdminController — reference for file upload page flow
- Role-based bagian filtering from CMPController.Kkj() Phase 89 — reuse for CMP/Kkj worker view

### Established Patterns
- File upload: ImportWorkers uses IFormFile, saves to wwwroot/uploads/, processes, redirects
- Tab navigation: Bootstrap 5 nav-tabs pattern used elsewhere in portal
- Toast feedback: success/error toast pattern from KkjMatrix Phase 89

### Integration Points
- AdminController.cs: KkjMatrix region needs full rewrite (remove table CRUD, add file CRUD)
- CMPController.cs: Kkj() action needs rewrite (load files instead of matrix items)
- Views/Admin/KkjMatrix.cshtml: full rewrite to tab + file list
- Views/CMP/Kkj.cshtml: full rewrite to file list + download
- Models/KkjModels.cs: remove KkjMatrixItem, KkjTargetValue, KkjColumn, PositionColumnMapping; keep KkjBagian
- Data/ApplicationDbContext.cs: remove DbSets for dropped models
- Helpers/PositionTargetHelper.cs: delete entirely
- CMP hub and Kelola Data hub views: update link descriptions

### Files to Delete/Modify
- DELETE: Helpers/PositionTargetHelper.cs
- DELETE: Views/CMP/KkjSectionSelect.cshtml (Claude's discretion)
- MODIFY: Models/KkjModels.cs (remove 4 classes, keep KkjBagian)
- MODIFY: Data/ApplicationDbContext.cs (remove 4 DbSets)
- MODIFY: Controllers/AdminController.cs (rewrite KkjMatrix region)
- MODIFY: Controllers/CMPController.cs (rewrite Kkj action)
- REWRITE: Views/Admin/KkjMatrix.cshtml
- REWRITE: Views/CMP/Kkj.cshtml
- NEW: Views/Admin/KkjUpload.cshtml (upload page)
- NEW: EF migration to drop tables + add KkjFile table (if using DB model)

</code_context>

<deferred>
## Deferred Ideas

- CDP PlanIDP connection — user clarified this belongs to Admin Silabus & Coaching Guidance, not KKJ. Separate phase if needed.
- File preview (PDF inline embed) — user chose download-only for now, could add preview later.

</deferred>

---

*Phase: 90-kkj-matrix-admin-full-rewrite*
*Context gathered: 2026-03-02*
