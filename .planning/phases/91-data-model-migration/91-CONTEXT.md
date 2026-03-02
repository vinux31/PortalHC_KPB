# Phase 91: Data Model & Migration - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Create CpdpFile entity (mirroring KkjFile pattern from Phase 90) with EF Core migration, and export all existing CpdpItem rows to an Excel backup file before any table changes. CpdpFile reuses KkjBagian as its container entity (sections RFCC/GAST/NGP/DHT are shared).

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion (user delegated all decisions)

User confirmed: "Pattern sudah jelas dari KkjFile, biarkan Claude ikuti pattern yang sama persis."

All implementation details follow the established KkjFile pattern:

- **Entity design**: Mirror KkjFile exactly — same fields (BagianId, FileName, FilePath, FileSizeBytes, FileType, Keterangan, UploadedAt, UploaderName, IsArchived). FK to KkjBagian with cascade delete.
- **Storage path**: `/uploads/cpdp/{bagianId}/{timestamp}_{safeName}.{ext}` — mirrors KkjFile's `/uploads/kkj/` structure
- **Excel backup**: One-time export of all CpdpItem rows to Excel file saved to disk before migration. Can be a controller action or console script — Claude decides approach.
- **Migration**: Single EF Core migration that adds CpdpFiles table. CpdpItem table NOT dropped in this phase (that's Phase 93).

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 90 pattern exactly — KkjFile entity is the reference implementation
- CpdpFile lives in Models/KkjModels.cs alongside KkjFile and CpdpItem
- DbSet<CpdpFile> added to ApplicationDbContext next to DbSet<KkjFile>

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- KkjFile model (Models/KkjModels.cs:15-28): Exact template for CpdpFile
- KkjBagian entity: Already exists with RFCC/GAST/NGP/DHT sections — reuse as-is
- ApplicationDbContext: Already has KkjFiles entity config pattern to copy

### Established Patterns
- EF Core migration: project uses `dotnet ef migrations add` + `dotnet ef database update`
- File entity FK: KkjFile → KkjBagian with cascade delete (OnDelete(DeleteBehavior.Cascade))
- Soft-delete: IsArchived bool flag pattern

### Integration Points
- Models/KkjModels.cs: Add CpdpFile class after KkjFile (line ~28)
- Data/ApplicationDbContext.cs: Add DbSet<CpdpFile> after DbSet<KkjFile> (line ~29)
- Entity config: Add CpdpFile config block after KkjFile config in OnModelCreating

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 91-data-model-migration*
*Context gathered: 2026-03-03*
