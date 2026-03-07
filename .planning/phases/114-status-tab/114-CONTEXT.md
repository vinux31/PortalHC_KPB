# Phase 114: Status Tab - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

New first (default) tab on ProtonData/Index showing silabus and guidance completeness across all Bagian > Unit > Track combinations. Read-only overview — no editing on this tab.

</domain>

<decisions>
## Implementation Decisions

### Tree layout
- Flat indented table, always fully visible — no expand/collapse mechanics
- Three indentation levels: Bagian (top), Unit (indented), Track (further indented)
- Bagian rows: bold text with light gray background (table-light)
- Unit rows: semi-bold, normal background
- Track rows: normal weight, most indented
- Columns: Level name | Silabus | Guidance
- No filter dropdowns — show all data unfiltered (the point is a full overview)
- Bagian and Unit rows show no status indicators — only Track (leaf) rows show checkmarks

### Completeness criteria
- **Silabus complete:** Every active (IsActive == true) ProtonKompetensi for that Bagian+Unit+Track has at least 1 SubKompetensi, and each SubKompetensi has at least 1 Deliverable
- **Guidance complete:** At least 1 CoachingGuidance record exists for that Bagian+Unit+Track
- Incomplete shows just the icon — no tooltip or detail about what's missing

### Data loading
- Single API endpoint returns all status data on tab show (AJAX call)
- Re-fetch data every time user clicks the Status tab (auto-refresh on tab switch)
- Data is lightweight (~102 leaf nodes max: 4 Bagian x 17 Units x 6 Tracks)

### Visual indicators
- Complete: green checkmark
- Incomplete: yellow warning triangle
- No summary counts on Bagian/Unit rows
- No total summary row at bottom

### Tab ordering
- Status tab becomes the first (default active) tab
- Existing Silabus and Guidance tabs shift to positions 2 and 3

### Claude's Discretion
- Exact indentation spacing (padding-left values)
- Loading spinner/skeleton while fetching status data
- Table responsive wrapper styling

</decisions>

<specifics>
## Specific Ideas

No specific requirements — standard indented table following existing ProtonData/Index patterns.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrganizationStructure.SectionUnits` (OrganizationStructure.cs): Static Bagian->Units mapping for generating rows
- `ProtonTrack` (ProtonModels.cs:6): 6 tracks with DisplayName and Urutan for ordering
- Bootstrap `nav-tabs` pattern already in ProtonData/Index.cshtml (line 25)

### Established Patterns
- ProtonData/Index uses AJAX to load tab content (Silabus and Guidance tabs fetch data on filter change)
- Tables use `table table-bordered table-sm` with `table-light` headers
- ProtonKompetensi has Bagian, Unit, ProtonTrackId fields for grouping
- CoachingGuidance has Bagian, Unit, ProtonTrackId fields for grouping

### Integration Points
- `ProtonDataController.cs`: New StatusData action returning JSON
- `Views/ProtonData/Index.cshtml`: New tab button + tab pane, reorder existing tabs
- Query joins: ProtonKompetensi -> SubKompetensi -> Deliverable for silabus completeness
- Query: CoachingGuidance existence check per Bagian+Unit+Track

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 114-status-tab*
*Context gathered: 2026-03-07*
