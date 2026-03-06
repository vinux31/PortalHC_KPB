# Phase 108: Timeline Detail Page & Styling - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Build the HistoriProtonDetail page: vertical timeline showing a worker's Proton journey (Tahun 1→2→3) with per-node details, worker info header card, and responsive styling. Replace the current placeholder view. Covers HIST-09 through HIST-17.

Worker list page and backend actions already built in Phase 107. This phase builds the detail view only.

</domain>

<decisions>
## Implementation Decisions

### Timeline Visual Style
- Left-aligned vertical timeline: line on the left, content cards to the right
- Filled circles with color: green = Lulus, yellow = Dalam Proses, gray outline = Belum Mulai
- Each node's content in a Bootstrap card (shadow-sm, rounded)
- Connector line: solid between completed nodes, dashed leading to incomplete nodes
- No animation on page load — render everything immediately
- Timeline width: narrower centered column (col-lg-8) for readability

### Node Content & Info
- Summary (collapsed): Tahun label + Jalur + Status badge
- Expanded: Unit, Coach name, Competency Level, Dates (start/end)
- Click/toggle to expand — not all visible at once
- "Belum Mulai" nodes (no assignment) do NOT appear in timeline at all
- Only nodes with actual ProtonTrackAssignment appear
- Worker can only have 1 track (Panelman OR Operator, not both)

### Worker Header Section
- Info card above timeline: Nama, NIP, Unit, Section, Jalur
- No step indicator in header (timeline itself shows progress)
- Back navigation via breadcrumb only: CDP > Histori Proton > Detail
- No separate back button
- Page title: generic "Detail Histori Proton - CDP"

### Edge States
- Worker with only 1 assignment: show just 1 node
- Coachee with only "Dalam Proses": show the in-progress node (not empty state)
- No multi-track scenario — 1 worker = 1 jalur only
- No print/export needed for now

### Mobile Responsive
- Same left-aligned layout on mobile, cards go full width
- Vertical timeline is naturally responsive — no special mobile treatment needed

### Claude's Discretion
- Coach data source (CoachCoacheeMapping vs ProtonTrackAssignment — pick most accurate)
- Expand/collapse animation style (accordion, collapse, etc.)
- Exact card spacing and typography
- Color shades for status badges (consistent with list page)
- Connector line thickness and styling details

</decisions>

<specifics>
## Specific Ideas

- Summary + expandable pattern: collapsed shows "Tahun 1 - Panelman [Lulus]", expand reveals full details
- Left-aligned timeline with cards — similar to the Phase 107 preview mockup
- "Histori Proton" canonical name throughout (from Phase 107)
- Status badge colors: Lulus (green), Dalam Proses (yellow), Belum Mulai (gray) — carried from Phase 107

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HistoriProtonViewModel.cs`: Has worker row model with Tahun1Done/Tahun2Done/Tahun3Done booleans
- `CDPController.HistoriProtonDetail(userId)`: Action exists with full auth checks, currently returns empty View()
- `Views/CDP/HistoriProtonDetail.cshtml`: Placeholder view to replace
- Status badge pattern from `HistoriProton.cshtml` list page (green/yellow/gray)
- Step indicator CSS from list page (reusable for timeline nodes)

### Established Patterns
- Bootstrap 5 cards with shadow-sm used throughout portal
- Breadcrumb pattern: Controller > Feature > Page
- ViewBag for passing role/filter data to views
- Client-side expand/collapse using Bootstrap Collapse component

### Integration Points
- CDPController.HistoriProtonDetail: Needs ViewModel with timeline data, query ProtonTrackAssignment + ProtonFinalAssessment + CoachCoacheeMapping
- Views/CDP/HistoriProtonDetail.cshtml: Replace placeholder with full timeline view
- May need new ViewModel class for detail page (HistoriProtonDetailViewModel)

</code_context>

<deferred>
## Deferred Ideas

- Print/export PDF of worker Proton history — future phase if needed
- Link from timeline node to CoachingProton page for that specific Tahun

</deferred>

---

*Phase: 108-timeline-detail-page-styling*
*Context gathered: 2026-03-06*
