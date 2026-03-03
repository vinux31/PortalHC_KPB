# Phase 89: PlanIDP Silabus and Coaching Guidance Tabs Improvement - Context

**Gathered:** 2026-03-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Redesign CDP/PlanIdp page from its current dual-path layout (Coachee deliverable table + Admin/HC PDF view) into a unified 2-tab layout (Silabus + Coaching Guidance) for all roles. Read-only consumer view aligned with the finalized ProtonData/Index admin page. Remove the old PDF-based view entirely.

</domain>

<decisions>
## Implementation Decisions

### Target Audience & Role Paths
- All roles see the same 2-tab layout (Silabus + Coaching Guidance)
- Replace BOTH current paths: the Coachee deliverable hierarchy AND the Admin/HC PDF view
- Coachee-specific elements (deliverable progress link, final assessment card) are REMOVED from PlanIdp — they are already accessible from the Coaching Proton page

### Silabus Tab
- Hierarchical read-only table with rowspan merge (Kompetensi > SubKompetensi > Deliverable)
- Only show active silabus items (IsActive == true)
- Cascading filter: Bagian → Unit → Track → Muat Data (same pattern as ProtonData/Index)
- No edit/delete/soft-delete buttons — purely read-only

### Coaching Guidance Tab
- Accordion drill-down structure with 4 levels: Bagian > Unit > TrackType (Operator/Panelman) > TahunKe (Tahun 1/2/3)
- Each level is collapsible
- Files displayed at the deepest level (TahunKe) with Download buttons
- Data source: CoachingGuidanceFile table JOIN ProtonTrack (for TrackType and TahunKe)
- No upload/replace/delete — read-only, download only

### Navigation & Filtering
- Coachee: auto-filter by their ProtonTrackAssignment (Bagian/Unit/Track pre-filled), but can browse all via "Lihat Semua" option
- Admin/HC: see all data, no auto-filter, manual cascading dropdowns
- Silabus tab: cascading filter (Bagian > Unit > Track) consistent with ProtonData/Index
- Coaching Guidance tab: accordion loads all data grouped hierarchically (no filter needed, just expand/collapse)

### PDF Legacy Cleanup
- Remove the old PDF-based IDP view entirely (physical PDF files on disk with naming `{Bagian}_{Unit}_{Level}_Kompetensi_*.pdf`)
- All coaching guidance documents are now served from CoachingGuidanceFile table
- Remove related JS for PDF section selection grid and PDF filter cascade

### Empty States
- Simple text messages:
  - No silabus data: "Tidak ada data silabus untuk filter ini"
  - No guidance files: "Belum ada file coaching guidance"
  - Coachee without assignment: "Anda belum memiliki penugasan Proton"

### Claude's Discretion
- Exact Bootstrap tab styling and responsive behavior
- Accordion expand/collapse animation
- Loading indicator while fetching data
- Error handling for failed API calls

</decisions>

<specifics>
## Specific Ideas

- Silabus tab should mirror the same visual structure as ProtonData/Index silabus table (rowspan merge) but without action buttons
- Coaching Guidance accordion should feel like a file explorer / tree view
- Coachee auto-filter should pre-select their Bagian/Unit/Track from ProtonTrackAssignment on page load
- Download buttons reuse the existing ProtonData/GuidanceDownload endpoint

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ProtonDataController.cs` — GuidanceList, GuidanceDownload endpoints already exist and can be reused for read-only access
- `ProtonData/Index.cshtml` — Silabus hierarchical rendering JS (rowspan merge logic) can be adapted for read-only version
- `CoachingGuidanceFile` model — Already stores Bagian, Unit, ProtonTrackId with file metadata
- `OrganizationStructure.SectionUnits` — Hardcoded org structure for cascading dropdown population
- `ProtonTrack` model — Has TrackType and TahunKe for accordion hierarchy mapping

### Established Patterns
- Cascading filter (Bagian → Unit → Track) used in ProtonData/Index — reuse same JS pattern
- CoachingGuidanceFile CRUD in ProtonDataController — download endpoint already [Authorize] without role restriction
- ProtonTrackAssignment for coachee auto-detection — already used in current PlanIdp coachee path

### Integration Points
- CDPController.PlanIdp action — complete rewrite of this action
- Views/CDP/PlanIdp.cshtml — complete rewrite of this view
- ProtonDataController.GuidanceList — new read-only endpoint or reuse existing with role check
- ProtonDataController.GuidanceDownload — reuse as-is for file downloads

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 89-planidp-silabus-and-coaching-guidance-tabs-improvement*
*Context gathered: 2026-03-03*
