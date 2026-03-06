# Phase 107: Backend & Worker List Page - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Build CDPController actions (HistoriProton list + HistoriProtonDetail) and the worker list page with search/filter. Role-scoped access: Coachee redirects to own detail, Coach sees mapped coachees, SrSpv/SH sees section workers, HC/Admin sees all. Covers HIST-01 through HIST-08.

Timeline detail page UI is Phase 108 — this phase builds the backend + list page only.

</domain>

<decisions>
## Implementation Decisions

### Worker List Layout
- Table layout (consistent with ManageWorkers, Records patterns)
- Columns: No, Nama, NIP, Unit, Jalur (Panelman/Operator), Progress Proton, Status, Aksi
- One row per worker (latest assignment determines Jalur shown; detail in timeline)
- Pagination (consistent with existing list pages)
- Default sort: Nama A-Z
- Action: Single "Lihat Riwayat" button per row linking to timeline detail

### Progress Proton Column
- Renamed from "Tahun Proton Terakhir" to "Progress Proton"
- Visual step indicator: filled/empty circles connected by lines
- Example: ●─●─○ means Tahun 1 done, Tahun 2 done, Tahun 3 not yet
- Shows current position in the 3-year Proton journey at a glance

### Status Badges
- Colored badges (consistent with Assessment monitoring)
- Lulus = green, Dalam Proses = yellow, Belum Mulai = gray
- Assignment without ProtonFinalAssessment = "Dalam Proses"

### Role Scoping
- Coachee: Auto-redirect to HistoriProtonDetail/{userId} — never sees list
- Coach: Sees coachees via CoachCoacheeMapping
- SrSupervisor / SectionHead: Sees all workers in same Section (ApplicationUser.Section)
- HC / Admin: Sees all workers
- Dual role: Use widest scope (SrSpv scope > Coach scope)

### Search & Filter UX
- Inline above table: search bar + 4 filter dropdowns
- Search: by nama or NIP (real-time/auto-apply)
- Filters: Section, Unit, Jalur (Panelman/Operator), Status (Lulus/Dalam Proses/Belum Mulai)
- All filters auto-apply on change (no submit button)
- Reset button to clear all filters + search at once
- Empty state: "Tidak ada data yang sesuai dengan pencarian."

### Navbar / Navigation
- New card in CDP Hub (Index.cshtml), NOT a navbar dropdown
- Card position: after Coaching Proton (3rd card)
- Order: Plan IDP, Coaching Proton, Histori Proton, Deliverable, Dashboard
- Icon: bi-clock-history, warna info (biru muda)
- Description: "Lihat riwayat perjalanan Proton per pekerja"

### Page Chrome
- Feature name: "Histori Proton" (not Riwayat, not Jejak)
- Breadcrumb: CDP > Histori Proton
- Browser tab title: "Histori Proton - CDP"

### Data Edge Cases
- Only workers with at least 1 ProtonTrackAssignment appear in list (HIST-05)
- No assignment = worker not shown
- Assignment without ProtonFinalAssessment = status "Dalam Proses"

### Claude's Discretion
- Client-side vs server-side filtering implementation
- Filter cascade behavior (Section -> Unit dependent dropdown or independent)
- Exact pagination size (10/15/20 per page)
- Step indicator CSS implementation details

</decisions>

<specifics>
## Specific Ideas

- Step indicator (●─●─○) for Progress Proton column — compact visual showing 3-year journey
- "Histori Proton" as canonical feature name throughout UI
- Card placement in CDP hub after Coaching Proton (related Proton features grouped)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CDPController.cs: Existing actions (PlanIdp, CoachingProton, Deliverable, Dashboard) — pattern for new action
- ProtonTrackAssignment model: CoacheeId, ProtonTrackId, IsActive, AssignedAt
- ProtonFinalAssessment model: CompetencyLevelGranted, Status, CompletedAt, linked via ProtonTrackAssignmentId
- ProtonTrack model: TrackType (Panelman/Operator), TahunKe (Tahun 1/2/3), Urutan
- CoachCoacheeMapping model: CoachId, CoacheeId — for Coach role scoping
- ApplicationUser: Section, Unit fields — for SrSpv/SH scoping
- Views/CDP/Index.cshtml: Hub page with card grid — add new card here

### Established Patterns
- CDPController has class-level [Authorize] — per-action role checks not needed for basic auth
- No FK constraints on CoacheeId in ProtonTrackAssignment (string userId pattern)
- Hub-based navigation (card grid) not dropdown navbar

### Integration Points
- CDPController: Add HistoriProton() and HistoriProtonDetail(string userId) actions
- Views/CDP/Index.cshtml: Add Histori Proton card after Coaching Proton card
- ApplicationDbContext: ProtonTrackAssignment, ProtonFinalAssessment, ProtonTrack DbSets already exist

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 107-backend-worker-list-page*
*Context gathered: 2026-03-06*
