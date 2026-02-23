# Phase 33: ProtonTrack Schema - Context

**Gathered:** 2026-02-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Create `ProtonTrack` as a dedicated entity/table. Migrate `ProtonKompetensi` rows to reference it via `ProtonTrackId` FK. Migrate `ProtonTrackAssignment` rows to use `ProtonTrackId` FK. Drop old `TrackType`+`TahunKe` string columns from both tables. Update `SeedProtonData.cs` and the `AssignTrack` workflow to use the new FK. No UI is built in this phase.

</domain>

<decisions>
## Implementation Decisions

### Track DisplayName format
- Format: `"Panelman - Tahun 1"` (TrackType + ` - ` + Tahun label in Indonesian)
- Language: Indonesian — `Tahun`, not `Year`
- Not editable by HC/Admin — DisplayName is auto-generated from TrackType + TahunKe at seed/migration time
- Storage: Claude's discretion — store as a real DB column (set at seed time) since Phase 34 needs it in a dropdown query

### ProtonTrackAssignment scope
- Phase 33 migrates **both** `ProtonKompetensi` AND `ProtonTrackAssignment` to use `ProtonTrackId` FK
- Old `TrackType`+`TahunKe` string columns dropped from both tables in the same migration (no backup columns)
- Rationale: Phase 33's goal is eliminating all string dependencies; leaving `ProtonTrackAssignment` as strings defeats the purpose
- `AssignTrack` action will receive `ProtonTrackId` directly (selected from dropdown), not TrackType+TahunKe strings — no internal lookup needed

### Track data completeness
- Exactly 6 tracks exist: Panelman×3 (Tahun 1/2/3) + Operator×3 (Tahun 1/2/3)
- Migration uses **defensive** approach: reads distinct TrackType+TahunKe combinations from existing `ProtonKompetensi` rows to build `ProtonTrack` rows dynamically (safe if data ever drifted)
- After Phase 33, `SeedProtonData.cs` seeds **ProtonTrack rows only** — Kompetensi/SubKompetensi/Deliverable catalog items are managed via the Phase 35 UI going forward
- Existing production data is preserved by the data migration (backfills `ProtonTrackId` on all existing rows)
- Fresh dev installs: only ProtonTrack rows exist after seed; catalog items must be added via the Phase 35 catalog UI

### Claude's Discretion
- Whether DisplayName is stored as a DB column or computed at runtime — recommendation: store in DB for query efficiency (Phase 34's dropdown queries it directly)
- EF migration structure — one migration or split (add table + backfill + drop cols)
- ProtonTrackAssignment FK nullability during migration (nullable during backfill, then made non-null)

</decisions>

<specifics>
## Specific Ideas

- DisplayName example: `"Panelman - Tahun 1"`, `"Panelman - Tahun 2"`, `"Operator - Tahun 3"`
- Navigation placement for the catalog page is a Phase 34 concern (CAT-09) — deferred

</specifics>

<deferred>
## Deferred Ideas

- Navigation placement for Proton Catalog Manager (HC/Admin nav link) — Phase 34 (CAT-09)
- Ability for HC to customize DisplayName when creating a new track — Phase 34's Add Track modal can accept it as an input; user may want this later

</deferred>

---

*Phase: 33-protontrack-schema*
*Context gathered: 2026-02-23*
