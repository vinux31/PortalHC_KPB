# Phase 123: Data Model & Migration - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Add AssignmentSection and AssignmentUnit fields to CoachCoacheeMapping model, create EF Core migration with duplicate cleanup, and add unique filtered index enforcing one-active-coach-per-coachee at the database level.

</domain>

<decisions>
## Implementation Decisions

### Field Defaults
- Fields are nullable strings (existing mappings will have null values)
- Mapping lama dengan null: tetap berfungsi, tapi muncul warning badge "Assignment belum diisi" di halaman CoachCoacheeMapping (Phase 125 UI)
- Mapping baru (assign setelah migration): wajib isi AssignmentSection dan AssignmentUnit — pre-fill default dari coachee's own Section/Unit tapi bisa diubah
- Validation di application level: assign action reject kalau AssignmentSection/Unit kosong

### Unique Constraint
- Filtered unique index pada CoacheeId WHERE IsActive = 1 (one active coach per coachee)
- Migration auto-deactivate duplikat: kalau ada 2+ active mapping untuk coachee yang sama, keep yang terbaru (by Id), deactivate sisanya (set IsActive=false, EndDate=today)
- Duplikat dibersihkan sebelum index dipasang

### Section/Unit Values
- Tipe data: string (sama seperti ApplicationUser.Section/Unit)
- Dropdown dari distinct existing values di ApplicationUser (bukan free text)
- Cascade: pilih AssignmentSection dulu → AssignmentUnit filter ke unit yang ada di section itu
- Pakai pattern yang sama seperti cascade filter di CoachingProton (Phase 121)

### Edit Mapping
- Edit modal TIDAK include AssignmentSection/Unit — hanya Coach, ProtonTrack, StartDate seperti sekarang
- Kalau mau ubah assignment section/unit: deactivate mapping lama, assign baru dengan assignment baru
- Ini menjaga history yang jelas — setiap assignment change tercatat sebagai deactivate + new assign

### Audit Trail
- AuditLog message saat assign baru include AssignmentSection/Unit info
- Halaman CoachCoacheeMapping menampilkan history section yang filter AuditLog entries terkait mapping
- Tidak perlu tabel log baru — pakai AuditLog existing dengan filter

### Claude's Discretion
- Migration naming convention dan ordering
- Exact duplicate detection query strategy
- Index implementation details (filtered index syntax for SQL Server)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CoachCoacheeMapping` model: Models/CoachCoacheeMapping.cs — 6 fields, no FK constraints
- AuditLog system: already used in all Admin actions, log entries keyed by action description
- Cascade filter pattern: Phase 121 GetCascadeOptions endpoint in CDPController — reusable for Section→Unit cascade

### Established Patterns
- EF Core migrations: `Migrations/` directory, SQL Server, hand-written migration for data-only changes (Phase 116 pattern)
- DbContext config: `ApplicationDbContext.cs` lines 261-267 — existing indexes on CoachId, CoacheeId, composite CoachId+CoacheeId
- String fields for Section/Unit: ApplicationUser uses plain strings, no FK to master table

### Integration Points
- `ApplicationDbContext.OnModelCreating()` — add filtered unique index config
- `AdminController.CoachCoacheeMappingAssign()` — add AssignmentSection/Unit to request DTO and validation
- `AdminController.CoachCoacheeMappingExport()` — include new fields in Excel export
- `Views/Admin/CoachCoacheeMapping.cshtml` — assign modal needs cascade dropdowns (Phase 125)

</code_context>

<specifics>
## Specific Ideas

- Cascade dropdown endpoint: reuse GetCascadeOptions or similar pattern from Phase 121
- Warning badge style: match existing badge patterns in CoachCoacheeMapping page (Aktif/Non-aktif badges)
- AuditLog format: "Assigned coach to {count} coachee(s) [Section: {X}, Unit: {Y}]"

</specifics>

<deferred>
## Deferred Ideas

- Master table untuk Section/Unit — over-engineering untuk sekarang, bisa dipertimbangkan di masa depan jika data inconsistency jadi masalah
- MappingLog tabel khusus — AuditLog cukup untuk sekarang

</deferred>

---

*Phase: 123-data-model-migration*
*Context gathered: 2026-03-08*
