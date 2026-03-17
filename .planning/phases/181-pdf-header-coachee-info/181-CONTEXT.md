# Phase 181: PDF Header Coachee Info - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Add coachee identity fields (Nama, Unit, Track) to the PDF Evidence Report header in DownloadEvidencePdf. Fields are positioned top-left above Tanggal Coaching. ExportProgressPdf is out of scope.

</domain>

<decisions>
## Implementation Decisions

### Label formatting
- Full labels with colon separator, matching existing Tanggal Coaching style
- Labels: "Nama Coachee", "Unit Coachee", "Track", "Tanggal Coaching"
- Colons aligned vertically (tabular look) — all labels padded to same width
- Track value uses existing trackDisplay as-is ("{TrackType} {TahunKe}" e.g. "Operator Tahun 2")

### Header layout
- Side-by-side layout: coachee info block on the left, Pertamina logo on the right at same vertical position
- Line order: Nama Coachee, Unit Coachee, Track, Tanggal Coaching (top to bottom)
- Thin horizontal line separator between header block and table content below
- All lines at FontSize 10 — uniform with existing Tanggal Coaching line

### Missing data handling
- Show "-" (dash) for any missing field — applies uniformly to all 3 fields (Nama, Unit, Track)
- Consistent with existing dash convention in the PDF (kompetensi, subKompetensi default to "-")
- Multi-unit coachees: use primary unit (user.Unit field), not all units

### Claude's Discretion
- Exact padding/spacing between header lines
- Column width ratio for side-by-side layout (info vs logo)
- Horizontal line styling (color, thickness)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### PDF generation
- `.planning/REQUIREMENTS.md` — PDF-01, PDF-02, PDF-03 requirements
- `.planning/ROADMAP.md` — Phase 181 success criteria and dependencies

### Existing implementation
- `Controllers/CDPController.cs` lines 2287-2405 — DownloadEvidencePdf action with existing header, data loading, and QuestPDF document structure

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `coacheeName` already loaded at line 2336-2339 (query by progress.CoacheeId, selects FullName)
- `trackDisplay` already computed at line 2351-2352 ("{TrackType} {TahunKe}")
- `Or()` helper at line 2364 for null/whitespace fallback to "-"
- QuestPDF document structure already in place with page setup, header column, and content table

### Established Patterns
- Header uses `page.Header().PaddingBottom(12).Column()` pattern
- Logo loaded as byte array, rendered with `.AlignRight().MaxWidth(140).Image()`
- Tanggal Coaching rendered as `.Text()` with FontSize(10) and Indonesian date format

### Integration Points
- Coachee Unit needs to be added to the query at line 2336-2339 (currently only selects FullName)
- Header Column at line 2377 needs restructuring: current sequential logo→text becomes side-by-side Row with left info column and right logo
- Horizontal line separator added after the info block, before content table

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches within the decisions above.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 181-pdf-header-coachee-info*
*Context gathered: 2026-03-17*
