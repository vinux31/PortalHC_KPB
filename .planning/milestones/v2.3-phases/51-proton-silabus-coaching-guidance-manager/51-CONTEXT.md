# Phase 51: Proton Silabus & Coaching Guidance Manager - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can manage Proton silabus data and coaching guidance files through a new page at /Admin/ProtonData with two tabs. Tab Silabus provides a flat table CRUD for ProtonKompetensi > SubKompetensi > Deliverable data scoped by Bagian > Unit > Track. Tab Coaching Guidance provides file upload/download/delete for learning materials (PDF, Word, Excel, PPT) scoped by Bagian > Unit > Track.

This phase replaces the existing ProtonCatalog page. Coachee-facing views (PlanIdp, ProtonProgress) are out of scope — those will be updated in a separate future phase.

</domain>

<decisions>
## Implementation Decisions

### Phase repurposing
- Phase 51 originally "Proton Track Assignment Manager" — absorbed into Phase 50 (Coach-Coachee Mapping)
- Repurposed to: Proton Silabus & Coaching Guidance Manager
- Roadmap needs rename: "Proton Silabus & Coaching Guidance Manager"
- OPER-02 requirement mapping needs updating

### Tab Silabus — Data structure
- Extend existing ProtonKompetensi model with `Bagian` (string) and `Unit` (string) fields
- Hierarchy: Bagian > Unit > Track (ProtonTrackId) > Kompetensi > SubKompetensi > Deliverable
- Each Unit can have different silabus content for the same track
- Multiple Kompetensi sets allowed per Bagian+Unit+Track combination
- Filter cascade on page: Bagian dropdown → Unit dropdown (filtered by Bagian) → Track dropdown
- No, Kompetensi, SubKompetensi, Deliverable are all string/text columns — No is manual input (flexible: "1", "1.1", "2a")

### Tab Silabus — Display
- Flat table: one row per deliverable (all columns: No, Kompetensi, SubKompetensi, Deliverable)
- Merge/rowspan for Kompetensi and SubKompetensi columns when same value spans multiple deliverables
- In edit mode: rowspan expands — all rows shown individually for easier editing
- View mode shows merged cells, edit mode shows expanded rows

### Tab Silabus — CRUD
- Inline editing: click cell to edit directly in table
- Inline add: "+" button per row to insert new row (not just at bottom — can insert anywhere)
- Inline delete: delete button per row with modal confirmation
- Save All button for batch save (changes held in memory until Save All clicked)
- No per-row highlight for changed rows

### Tab Coaching Guidance — Files
- New database entity: `CoachingGuidanceFile` (Id, Bagian, Unit, ProtonTrackId, FileName, FilePath, FileSize, UploadedAt, UploadedById)
- Filter: Bagian > Unit > Track (independent filter state from Silabus tab)
- Table columns: Nama File, Unit, Ukuran, Tanggal Upload, Actions (Download/Delete)
- Allowed file types: PDF, Word (.doc/.docx), Excel (.xls/.xlsx), PowerPoint (.ppt/.pptx)
- Max file size: 10 MB per file
- Unlimited files per Unit+Track combination
- Upload: standard "Choose File" button, one file at a time
- Replace in-place: edit existing file record, upload replacement file — record stays same
- Storage: server local (wwwroot/uploads/guidance/) — note for future: connect to company server
- Delete: modal confirmation before deletion

### Page layout & navigation
- URL: /Admin/ProtonData
- Admin/Index card: "Silabus & Coaching Guidance" in Section A (Master Data), at end of section
- Tab style: Bootstrap nav-tabs (consistent with app — same as Dashboard, Assessment, etc.)
- Two tabs: Silabus | Coaching Guidance
- Each tab has its own independent filter state (Bagian > Unit > Track)
- Empty state: message "Belum ada data silabus untuk [Unit] - [Track]" + Tambah button

### ProtonCatalog replacement
- Delete ProtonCatalog card from Admin/Index
- ProtonCatalogController: Claude's discretion on delete vs redirect
- All ProtonCatalog functionality replaced by Silabus tab in new page

### Data migration
- Add `Bagian` (string) and `Unit` (string) columns to ProtonKompetensi via EF Core migration
- Create new `CoachingGuidanceFile` table
- Migration cleans up old data: delete all ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonDeliverableProgress records
- ProtonTrack records (6 seeded tracks) kept — still valid
- ProtonTrackAssignment records: Claude's discretion on keep or clean

### Access control
- Admin and HC only (RoleLevel <= 2) — same as current ProtonCatalog
- Both Admin and HC have full CRUD (no difference in permissions)
- Delete actions require modal confirmation

### Audit & logging
- AuditLogService logs every action: create, edit, delete in both tabs (Silabus and Coaching Guidance)

### Claude's Discretion
- ProtonCatalogController: delete entirely vs redirect to new page
- ProtonTrackAssignment records during migration: keep or clean
- Exact modal confirmation design
- Inline editing UX details (contenteditable vs input fields)
- File naming convention for uploaded guidance files (sanitization)
- Table pagination (if needed for large datasets)

</decisions>

<specifics>
## Specific Ideas

- Filter cascade matches OrganizationStructure.cs Bagian > Unit mapping
- Flat table with rowspan merge in view mode, expand in edit mode — similar to spreadsheet feel
- "+" button per row for inserting anywhere in the table, not just appending
- Save All batch pattern (not auto-save per row)
- Coaching Guidance file storage future note: plan to connect to company server later

</specifics>

<deferred>
## Deferred Ideas

- PlanIdp page update: coachee views silabus and coaching guidance for their assigned track (read-only download) — add as new phase at end of roadmap
- ProtonProgress page update: progress data sourced from new Bagian+Unit-scoped silabus — add as new phase at end of roadmap
- PlanIdp needs redevelopment (not final yet) — future phase

</deferred>

---

*Phase: 51-proton-silabus-coaching-guidance-manager*
*Context gathered: 2026-02-27*
