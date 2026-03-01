# Phase 64: Functional Filters - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Wire every filter on the Progress page (Bagian/Unit, Coachee, Track, Tahun, Search) to actual database queries so they genuinely narrow returned data. Enforce role-based data scoping server-side (Spv=unit, SrSpv/SectionHead=section, HC/Admin=all). Fix dropdown selected attribute behavior on reload.

</domain>

<decisions>
## Implementation Decisions

### Filter combination & reset
- All filters combine with AND logic — selecting Bagian=X AND Track=Panelman shows only Panelman rows in Bagian X
- Each dropdown has a "Semua [Category]" default option to clear individually, plus an inline Reset button that clears all filters
- Reset button sits inline at the end of the filter row (same visual level as dropdowns)
- Auto-submit on dropdown change — each change triggers a full GET page reload
- Cascading dropdowns: selecting Bagian narrows the Unit dropdown to that Bagian's units; selecting Track narrows the Coach's Coachee dropdown to coachees on that track
- Default state: no filters active, user sees all data their role allows, all dropdowns show "Semua [Category]"
- Empty result: table stays visible with empty body + message "Tidak ada data yang sesuai filter"
- Display result count: "Menampilkan X dari Y data" above or below the table
- No active filter visual indicator needed — dropdown selected values are sufficient

### Role-filter visibility
- **Spv:** Bagian/Unit dropdowns hidden entirely (implicit scope to their unit). See Track, Tahun, Search only
- **SrSpv/SectionHead:** Unit dropdown visible (populated with units in their section only). Bagian dropdown hidden. See Unit, Track, Tahun, Search
- **Coach:** Coachee dropdown only (populated from CoachCoacheeMapping). Bagian/Unit hidden. See Coachee, Track, Tahun, Search
- **HC/Admin:** All dropdowns visible — Bagian, Unit, Coachee, Track, Tahun, Search
- Server-side enforcement always applies: query-level role scope regardless of URL params. A Spv passing ?bagian=other still only gets their unit's data

### Search behavior
- Client-side only — filters competency/deliverable name column, not other columns
- Debounced at 300ms after last keystroke
- Case-insensitive matching
- Clear (X) button inside the search input when text is present
- Placeholder text: "Cari kompetensi..."
- No-results message: "Tidak ditemukan kompetensi untuk '[query]'" (search-specific, references user's query)
- Search clears on page reload (does not persist via URL)
- No highlight of matching text — just show/hide rows

### Filter state on URL
- Server-side filters reflected as GET query parameters (e.g., ?bagian=2&track=panelman)
- Strip empty/default params from URL — only include non-default values
- Out-of-scope params silently ignored (server-side enforcement overrides)
- Natural browser back/forward history — each filter change is a new history entry via GET reload

### Filter bar layout
- Single horizontal row on desktop: Bagian -> Unit -> Coachee -> Track -> Tahun -> Search -> Reset
- Stack vertically (full-width) on mobile/small screens
- Search box visually distinct: magnifying glass icon prefix, slightly wider than dropdowns
- Filter order follows organizational hierarchy first, then content filters, then search

### Loading feedback
- Browser default loading indicator only (tab spinner) — no custom loading UI
- No safeguards for slow loads (no dropdown disabling during reload)

### Dropdown option labels
- Bagian dropdown: name only (e.g., "Produksi", "Maintenance")
- Track dropdown: full name "Panelman" / "Operator"
- Tahun dropdown: "Tahun 1" / "Tahun 2" / "Tahun 3"
- Default option for all dropdowns: "Semua [Category]" (e.g., "Semua Bagian", "Semua Track", "Semua Tahun", "Semua Coachee")

### Claude's Discretion
- Cascade + auto-submit double reload handling (server-side reset vs AJAX cascade)
- Exact dropdown sizing and spacing
- Table column widths when filter results change
- Error handling for malformed URL params

</decisions>

<specifics>
## Specific Ideas

- Filter auto-submits via GET form so URL params happen naturally with standard HTML form behavior
- Cascade parent->child: Bagian->Unit is the key cascade pair; Track->Coachee cascade applies only for Coach role
- "Menampilkan X dari Y data" count gives users confidence that filters are working

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 64-functional-filters*
*Context gathered: 2026-02-27*
