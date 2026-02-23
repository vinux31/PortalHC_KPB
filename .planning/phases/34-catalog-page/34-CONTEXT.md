# Phase 34: Catalog Page - Context

**Gathered:** 2026-02-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Build a read-only Proton Catalog Manager page accessible to HC and Admin. The page shows the complete Kompetensi → SubKompetensi → Deliverable tree for a selected track, with expand/collapse rows and a track dropdown. HC/Admin can add new tracks via a modal. No add/edit/delete/reorder controls for catalog items in this phase — that is Phase 35 (add/edit), 36 (delete), and 37 (reorder).

</domain>

<decisions>
## Implementation Decisions

### Tree expand/collapse behavior
- **Trigger:** Chevron/arrow icon only — clicking the row text does NOT expand; only the icon does
- **Initial state on load:** All Kompetensi rows collapsed — clean starting point, nothing pre-expanded
- **Expand behavior:** Expanding a Kompetensi reveals its SubKompetensi rows in collapsed state (Deliverables hidden until each SubKompetensi is also expanded); two-click workflow to reach Deliverables
- **Animation:** Claude's discretion — standard Bootstrap collapse transition is acceptable

### Track dropdown and page behavior
- **Default on first load:** "Select a track..." placeholder — tree is blank until user picks a track
- **URL reflects track selection:** Yes — URL updates to `/ProtonCatalog?trackId={id}` when track changes
- **Direct URL navigation:** If URL contains `trackId` query param, that track is pre-selected on load (bookmarkable/linkable to specific tracks)
- **Tree reload on track change:** Claude's discretion — AJAX replacing just the tree section is preferred; avoid full page reload if possible
- **Back button behavior:** Claude's discretion — follows standard browser history pattern for the chosen AJAX approach
- **Empty state (no Kompetensi):** Show message: "No Kompetensi yet — add some in the catalog editor"

### Add Track modal
- **TrackType field:** Constrained dropdown — only "Panelman" and "Operator" as options (no free text)
- **TahunKe field:** Constrained dropdown — only "Tahun 1", "Tahun 2", "Tahun 3" as options (no free text)
- **DisplayName field:** Read-only preview that auto-generates from TrackType + TahunKe inputs (e.g., selecting "Panelman" + "Tahun 2" shows "Panelman - Tahun 2" as preview); not editable by user; server generates it on submit
- **Duplicate validation:** If same TrackType + TahunKe already exists, show inline error in modal: "[DisplayName] already exists" (e.g., "Panelman - Tahun 1 already exists")
- **On success:** Modal closes, new track appears in dropdown, dropdown auto-selects the new track (showing its empty catalog with the empty state message)
- **Who can see Add Track button:** Both HC and Admin (both can add tracks)

### Navigation placement
- **Location:** Inside the CDP dropdown menu in the nav, after existing CDP links, separated by a Bootstrap `dropdown-divider`
- **Label:** "Proton Catalog"
- **Visibility:** HC and Admin roles only — all other roles (Spv, SrSpv, Coach, Coachee) do not see the link
- **Role-switcher behavior:** Show based on actual role, not simulated view — if the user's real role is Admin or HC, the link shows even while simulating another role
- **Icon:** Plain text link only — no icon or badge

### Claude's Discretion
- Exact animation behavior for tree expand/collapse (Bootstrap default is fine)
- Whether track dropdown change uses AJAX or full page reload — AJAX preferred
- Browser history/back button behavior for track navigation
- Tree row styling — indentation levels, icons, column widths

</decisions>

<specifics>
## Specific Ideas

- Chevron icon should visually indicate direction — pointing right when collapsed, down when expanded (standard Bootstrap/Bootstrap icons pattern)
- DisplayName preview in modal updates live as user selects TrackType and TahunKe from the dropdowns
- The CDP dropdown separator should use Bootstrap's built-in `<hr class="dropdown-divider">` — consistent with any existing separators in the nav

</specifics>

<deferred>
## Deferred Ideas

- Ability for HC to rename a track's DisplayName — future enhancement (noted as possibility in Phase 33 context)
- Filtering/searching Kompetensi within a track — not in scope for read-only phase

</deferred>

---

*Phase: 34-catalog-page*
*Context gathered: 2026-02-23*
