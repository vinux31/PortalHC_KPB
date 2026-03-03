# Phase 93: Worker View & Cleanup - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Rewrite CMP/Mapping as a file download page where workers download CPDP documents per bagian, with role-based section filtering (L1-L4 all, L5-L6 own unit). Then remove the CpdpItem table, model, seed data, and all old spreadsheet CRUD code.

</domain>

<decisions>
## Implementation Decisions

### Page Layout & Presentation
- Tabbed by bagian (mirror Admin/CpdpFiles pattern) but read-only — download only, no upload/archive/delete controls
- File info per row: Nama file, Tipe (PDF/Excel badge), Keterangan, Tanggal upload, Download button
- Empty bagian tab shows "Belum ada dokumen CPDP untuk bagian ini." message with inbox icon
- Page title stays "Mapping KKJ - IDP (CPDP)" — unchanged
- Breadcrumb navigation: Beranda > CMP > Mapping KKJ-IDP (CPDP)
- Print Kurikulum button removed (was non-functional in old view, not relevant for file download)

### Role-Based Filtering
- L1-L4: See all bagian tabs
- L5-L6: See only their own unit's bagian tab — other tabs hidden completely (not greyed out)
- Matching: user.Section string matched to KkjBagian.Name
- Fallback: If user.Section doesn't match any KkjBagian, show all bagian tabs (safe fallback — better too much access than none)
- Admin/HC opening CMP/Mapping see all bagian tabs (same as L1-L4)

### CpdpItem Removal — Total Cleanup
- Delete CpdpItem model class from KkjModels.cs
- Delete GapAnalysisItem class from KkjModels.cs (if unused — verify no references)
- Remove DbSet<CpdpItem> from ApplicationDbContext.cs
- Remove CpdpItem seed data from SeedMasterData.cs
- Delete old Mapping.cshtml (spreadsheet table view)
- Delete MappingSectionSelect.cshtml (bagian selector page — replaced by tabs)
- Remove old CMPController.Mapping action that queries CpdpItems
- Create EF migration to drop CpdpItems table
- Remove any CpdpItem-related OnModelCreating configurations

### Navigation & Entry Point
- URL stays /CMP/Mapping — no change
- Navbar label stays "Mapping KKJ-IDP" — content is file downloads of mapping documents
- MappingSectionSelect.cshtml removed — tabbed layout replaces it, workers land directly on tabs
- No section query parameter needed — tabs handle navigation

### Claude's Discretion
- Tab ordering and default active tab logic
- Exact breadcrumb implementation
- Migration naming convention
- Order of cleanup operations

</decisions>

<specifics>
## Specific Ideas

- "Tetap Mapping KKJ-IDP, karena dokumen filenya memang tentang Mapping KKJ-IDP" — user confirms the mapping terminology is correct for the content
- Admin/CpdpFiles (Phase 92) is the reference implementation for the tabbed pattern — worker view is a stripped-down read-only version

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/Admin/CpdpFiles.cshtml`: Proven tabbed-by-bagian layout with file listing — mirror this for worker view minus admin controls
- `CpdpFile` model (KkjModels.cs): Entity with BagianId, FileName, FilePath, FileType, Keterangan, UploadedAt, IsArchived
- `KkjBagian` model: Shared bagian table used by both KKJ and CPDP features
- `AdminController.CpdpFileDownload`: Existing download action serves files — worker view can call the same endpoint (already [Authorize] not role-restricted)

### Established Patterns
- Tabbed Bootstrap layout: `nav-tabs` + `tab-content` + `tab-pane` pattern established in KkjMatrix and CpdpFiles
- File download uses `PhysicalFile()` with correct content types (PDF, Excel)
- Soft-delete pattern with IsArchived flag — worker view only shows `IsArchived == false` files

### Integration Points
- `CMPController.Mapping` action needs rewrite — currently queries CpdpItems, needs to query CpdpFiles + KkjBagians
- New Mapping.cshtml replaces old one — same filename, different content
- `ApplicationDbContext.CpdpItems` DbSet removal requires migration
- `SeedMasterData.cs` has CpdpItem seeding code to remove
- `_Layout.cshtml` navbar already has CMP > Mapping link — no nav changes needed

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 93-worker-view-cleanup*
*Context gathered: 2026-03-03*
