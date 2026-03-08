# Roadmap: Portal HC KPB

## Shipped Milestones

<details>
<summary>v1.0 through v2.7 (Phases 1-81) — See milestones/ for details</summary>

- v1.0 CMP Assessment Completion (Phases 1-3, shipped 2026-02-17)
- v1.1 CDP Coaching Management (Phases 4-8, shipped 2026-02-18)
- v1.2 UX Consolidation (Phases 9-12, shipped 2026-02-19)
- v1.3 Assessment Management UX (Phases 13-15, shipped 2026-02-19)
- v1.4 Assessment Monitoring (Phase 16, shipped 2026-02-19)
- v1.5 Question and Exam UX (Phase 17, shipped 2026-02-19)
- v1.6 Training Records Management (Phases 18-20, shipped 2026-02-20)
- v1.7 Assessment System Integrity (Phases 21-26, shipped 2026-02-21)
- v1.8 Assessment Polish (Phases 27-32, shipped 2026-02-23)
- v1.9 Proton Catalog Management (Phases 33-37, shipped 2026-02-24)
- v2.0 Assessment Management & Training History (Phases 38-40, shipped 2026-02-24)
- v2.1 Assessment Resilience & Real-Time Monitoring (Phases 41-45, shipped 2026-02-25)
- v2.2 Attempt History (Phase 46, shipped 2026-02-26)
- v2.3 Admin Portal (Phases 47-53, 59, shipped 2026-03-01)
- v2.4 CDP Progress (Phases 61-64, shipped 2026-03-01)
- v2.5 User Infrastructure & AD Readiness (Phases 65-72, shipped 2026-03-01)
- v2.6 Codebase Cleanup (Phases 73-78, shipped 2026-03-01)
- v2.7 Assessment Monitoring (Phases 79-81, shipped 2026-03-01)

</details>

<details>
<summary>v3.0 through v3.8 (Phases 82-112) — shipped 2026-03-02 to 2026-03-07</summary>

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)
- **v3.6 Histori Proton** — Phases 107-108 (shipped 2026-03-06)
- **v3.7 Role Access & Filter Audit** — Phases 109-111 (shipped 2026-03-07)
- **v3.8 CoachingProton UI Redesign** — Phase 112 (shipped 2026-03-07)

</details>

---

## v3.9 ProtonData Enhancement

**Milestone Goal:** Enhance ProtonData/Index with Target column, Status completeness tab, and hard delete with consumer audit.

## Phases

- [x] **Phase 113: Target Column** - Add Target text column to silabus table with edit/save support (completed 2026-03-07)
- [x] **Phase 114: Status Tab** - New first tab showing silabus and guidance completeness tree (completed 2026-03-07)
- [x] **Phase 115: Hard Delete + Consumer Audit** - Kompetensi hard delete with safety checks and consumer verification (completed 2026-03-07)

## Phase Details

### Phase 113: Target Column
**Goal**: Admin/HC can see and edit a Target column on the silabus table
**Depends on**: Nothing (first phase in v3.9)
**Requirements**: TGT-01, TGT-02
**Success Criteria** (what must be TRUE):
  1. Silabus table in view mode shows a Target column between SubKompetensi and Deliverable
  2. In edit mode, user can type a target value into the Target field and save it via SilabusSave
  3. Existing silabus rows display correctly with empty/null Target values
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [x] 113-01: Migration, model, DTO, save logic, and UI for Target column

### Phase 114: Status Tab
**Goal**: Admin/HC can see completeness status of silabus and guidance across all tracks at a glance
**Depends on**: Phase 113
**Requirements**: STAT-01, STAT-02, STAT-03, STAT-04
**Success Criteria** (what must be TRUE):
  1. ProtonData/Index opens with Status as the first (default) tab
  2. Status tab displays a tree of Bagian > Unit > Track nodes that can be expanded and collapsed
  3. Each Track node shows a green checkmark in the Silabus column when at least 1 active Kompetensi exists
  4. Each Track node shows a green checkmark in the Guidance column when at least 1 guidance file exists
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [x] 114-01: StatusData endpoint and Status tab UI

### Phase 115: Hard Delete + Consumer Audit
**Goal**: Admin/HC can permanently remove incorrectly entered Kompetensi master data while all silabus consumers remain intact
**Depends on**: Phase 114
**Requirements**: DEL-01, DEL-02, DEL-03, AUD-01
**Success Criteria** (what must be TRUE):
  1. View mode shows a Delete button on each Kompetensi row
  2. Clicking Delete shows a confirmation dialog listing the count of SubKompetensi and Deliverable that will be deleted
  3. Delete is blocked with a message when ProtonDeliverableProgress records reference deliverables under that Kompetensi
  4. After successful delete, PlanIdp and CoachingProton pages still function correctly (consumer audit verified)
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 115-01-PLAN.md — Backend cascade delete endpoints + frontend delete button and confirmation modal

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 113. Target Column | 1/1 | Complete    | 2026-03-07 |
| 114. Status Tab | 1/1 | Complete    | 2026-03-07 |
| 115. Hard Delete + Consumer Audit | 1/1 | Complete    | 2026-03-07 |

### Phase 121: CDP Dashboard Filter & Assessment Analytics Redesign

**Goal**: Both CDP Dashboard tabs have cascade filter dropdowns with AJAX refresh, role-based filter locking, and aligned layout
**Depends on:** Phase 120
**Requirements**: FILT-01, FILT-02, FILT-03, FILT-04, FILT-05, FILT-06, FILT-07, FILT-08
**Success Criteria** (what must be TRUE):
  1. Coaching Proton tab has 4 cascade filters (Section -> Unit -> Category -> Track) with immediate AJAX refresh
  2. Assessment Analytics tab has 3 cascade filters (Section -> Unit -> Category) with AJAX refresh, no page reload
  3. Level 4 users see Section pre-filled and locked; Level 5 users see Section+Unit locked
  4. Both tabs follow identical layout: Filter bar -> KPI cards -> Charts -> Table
  5. Excel export respects active filter selections
  6. Assessment Analytics filter change no longer redirects to Coaching Proton tab
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 121-01-PLAN.md — Backend AJAX endpoints + Coaching Proton tab cascade filters
- [ ] 121-02-PLAN.md — Assessment Analytics tab redesign with cascade filters + export fix

### Phase 122: Remove Assessment Analytics Tab from CDP Dashboard

**Goal**: CDP Dashboard is simplified to a single-section Coaching Proton Dashboard with all analytics code removed
**Depends on:** Phase 121
**Requirements**: REM-01, REM-02, REM-03, REM-04, REM-05
**Success Criteria** (what must be TRUE):
  1. Assessment Analytics tab, partials, controller actions, and view model classes are fully removed
  2. Dashboard renders as a single-section "Coaching Proton Dashboard" page with no tab UI
  3. Navbar, hub card, and guide pages reference "Coaching Proton Dashboard"
  4. Build succeeds with no errors
  5. Old analytics URLs land on Dashboard without errors
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 122-01-PLAN.md — Remove analytics backend/frontend code and simplify Dashboard to single-section

---

## v3.10 Evidence Coaching & Deliverable Redesign

**Milestone Goal:** Redesign evidence coaching flow — cleanup modal, add status history tracking, restructure Deliverable detail page, build P-Sign infrastructure, and auto-generate PDF evidence coaching forms.

## Phases

- [x] **Phase 116: Modal Cleanup** - Remove Kompetensi Coachee field from evidence modal and backend (completed 2026-03-07)
- [x] **Phase 117: Status History** - New DeliverableStatusHistory table tracking all status changes with rejection persistence (completed 2026-03-07)
- [x] **Phase 118: P-Sign Infrastructure** - User P-Sign data model and renderable badge component (completed 2026-03-07)
- [x] **Phase 119: Deliverable Page Restructure** - Split Deliverable detail into sectioned layout with status timeline (completed 2026-03-08)
- [x] **Phase 120: PDF Evidence** - Auto-generate PDF evidence coaching form with P-Sign (completed 2026-03-08)

## Phase Details

### Phase 116: Modal Cleanup
**Goal**: Evidence modal only captures coaching-relevant fields, with no unused Kompetensi Coachee textarea
**Depends on**: Nothing (independent)
**Requirements**: MOD-01, MOD-02
**Success Criteria** (what must be TRUE):
  1. Evidence modal in CoachingProton no longer shows the "Kompetensi Coachee" textarea
  2. Submitting evidence succeeds without sending koacheeCompetencies data
  3. CoachingSession records created after this change have no koacheeCompetencies value stored
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [x] 116-01-PLAN.md — Remove CoacheeCompetencies from modal, controller, model, views + data-clearing migration

### Phase 117: Status History
**Goal**: Every deliverable status change is permanently recorded with actor, timestamp, and reason
**Depends on**: Nothing (independent)
**Requirements**: HIST-01, HIST-02, HIST-03, HIST-04
**Success Criteria** (what must be TRUE):
  1. When a deliverable status changes (submit, approve, reject, review, re-submit), a new row appears in DeliverableStatusHistory
  2. After a rejection followed by re-submit, the original rejection entry (with reason) is still present in the history table
  3. Each approval role (SrSpv, SH, HC) creates a separate history entry when they approve
  4. Re-submitting evidence after rejection creates a "Re-submitted" history entry
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter
nPlans:
- [ ] 117-01-PLAN.md — DeliverableStatusHistory model, migration, and CDPController wiring

### Phase 118: P-Sign Infrastructure
**Goal**: Any user's P-Sign badge can be rendered as a visual component for use in pages and PDFs
**Depends on**: Nothing (independent)
**Requirements**: PSIGN-01, PSIGN-02, PSIGN-03
**Success Criteria** (what must be TRUE):
  1. ApplicationUser has Position/Role text and Unit fields that populate the P-Sign
  2. P-Sign badge renders with Logo Pertamina, Role + Unit, and full name
  3. P-Sign can be rendered as an embeddable HTML component and as an image for PDF embedding
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 118-01-PLAN.md — PSignViewModel, _PSign.cshtml partial view, and Settings page preview

### Phase 119: Deliverable Page Restructure
**Goal**: Deliverable detail page presents coaching data, approval status, and history in clearly separated sections
**Depends on**: Phase 117 (needs DeliverableStatusHistory data)
**Requirements**: PAGE-01, PAGE-02, PAGE-03
**Success Criteria** (what must be TRUE):
  1. Deliverable detail page shows four distinct sections: Detail Coachee & Kompetensi, Evidence Coach, Approval Chain, Riwayat Status
  2. Riwayat Status section displays a chronological timeline of all status changes from DeliverableStatusHistory
  3. Evidence Coach section shows Catatan Coach, Kesimpulan, Result, and a download button for the evidence file
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter
nPlans:
- [ ] 119-01-PLAN.md — Rewrite Deliverable.cshtml with 4-card sectioned layout

### Phase 120: PDF Evidence
**Goal**: Coach can download a professional PDF evidence form after submitting coaching evidence
**Depends on**: Phase 116 (modal fields define PDF content), Phase 118 (P-Sign component for PDF)
**Requirements**: PDF-01, PDF-02, PDF-03, PDF-04
**Success Criteria** (what must be TRUE):
  1. After coach submits evidence, a PDF is auto-generated containing the coaching session data
  2. PDF contains: Coachee info, Track, Kompetensi, SubKompetensi, Deliverable, Tanggal, Catatan Coach, Kesimpulan, Result
  3. PDF displays the Coach's P-Sign badge in the bottom-right corner
  4. Deliverable detail page has a "Download PDF" button that downloads the generated PDF
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 120-01-PLAN.md — DownloadEvidencePdf action + download button in Deliverable Card 3

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 116. Modal Cleanup | 1/1 | Complete    | 2026-03-07 |
| 117. Status History | 1/1 | Complete    | 2026-03-07 |
| 118. P-Sign Infrastructure | 1/1 | Complete    | 2026-03-07 |
| 119. Deliverable Page Restructure | 1/1 | Complete    | 2026-03-08 |
| 120. PDF Evidence | 1/1 | Complete    | 2026-03-08 |

### Phase 126: Scope CoachingProton progress data to ProtonTrackAssignment context

**Goal:** Absorbed into Phase 127
**Requirements**: TBD
**Depends on:** Phase 125
**Plans:** 0 plans (absorbed into Phase 127)

Plans:
- Absorbed into Phase 127

### Phase 127: Audit & fix CoachingProton progress table data source and assignment scoping

**Goal:** Link ProtonDeliverableProgress to ProtonTrackAssignment, auto-create progress on assign, rewrite all CDP scoping to assignment-based, and add silabus sync/cascade cleanup
**Requirements**: TBD
**Depends on:** Phase 125
**Plans:** 3/3 plans complete

Plans:
- [x] 127-01-PLAN.md — Migration + model FK + auto-create/cleanup in Assign/Edit
- [x] 127-02-PLAN.md — Rewrite Dashboard + CoachingProton + HistoriProton scoping to assignment-based
- [x] 127-03-PLAN.md — SaveSilabus auto-sync + DeleteKompetensi cascade cleanup

---

## v3.11 CoachCoacheeMapping Overhaul

**Milestone Goal:** Perbaiki sistem CoachCoacheeMapping agar mendukung cross-section assignment, tambah field penugasan, perbaiki CDP access check, tambah database constraint, dan cleanup ProtonTrackAssignment lifecycle.

## Phases

- [x] **Phase 123: Data Model & Migration** - Add AssignmentUnit/AssignmentSection fields and unique constraint to CoachCoacheeMapping (completed 2026-03-08)
- [x] **Phase 124: CDP Access & Lifecycle** - Rewrite all CDP scope queries to mapping-based access and wire ProtonTrackAssignment cleanup on deactivate (completed 2026-03-08)
- [x] **Phase 125: Mapping UI** - Display assignment columns, update assign modal, and include in Excel export (completed 2026-03-08)

## Phase Details

### Phase 123: Data Model & Migration
**Goal**: CoachCoacheeMapping supports cross-section assignment with database-enforced one-active-coach-per-coachee constraint
**Depends on**: Nothing (foundation phase)
**Requirements**: MODEL-01, MODEL-02, MODEL-03
**Success Criteria** (what must be TRUE):
  1. CoachCoacheeMapping has nullable AssignmentSection and AssignmentUnit string fields that persist to the database
  2. Existing mappings with null AssignmentSection/AssignmentUnit continue to work (fallback to worker's own section/unit)
  3. Attempting to create a second active mapping for the same coachee is rejected by the database unique filtered index
  4. Migration applies cleanly on existing data without data loss
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 123-01-PLAN.md — Model fields, migration with duplicate cleanup, unique index, and assign validation

### Phase 124: CDP Access & Lifecycle
**Goal**: Coaches can access coachees across sections via mapping, and deactivating a mapping cleans up associated ProtonTrackAssignments
**Depends on**: Phase 123 (needs AssignmentSection/AssignmentUnit fields)
**Requirements**: ACCESS-01, ACCESS-02, ACCESS-03, LIFE-01, LIFE-02
**Success Criteria** (what must be TRUE):
  1. Coach can open Deliverable page for a coachee in a different section when an active mapping exists
  2. CoachingProton, HistoriProton, GetCoacheeDeliverables, and batch submit all show coachees based on active mapping (not section match)
  3. Deactivating a mapping automatically deactivates all ProtonTrackAssignment records for that coach-coachee pair
  4. Reactivating a mapping presents the option to re-assign ProtonTrack to the coachee
  5. Coach without an active mapping for a coachee cannot access that coachee's Deliverable page
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:

### Phase 125: Mapping UI
**Goal**: Admin/HC can see and set assignment unit/section when managing coach-coachee mappings, with full export support
**Depends on**: Phase 123 (needs AssignmentSection/AssignmentUnit fields)
**Requirements**: UI-01, UI-02, UI-03
**Success Criteria** (what must be TRUE):
  1. CoachCoacheeMapping list page shows separate columns for worker's home unit/section and assignment unit/section
  2. Assign modal includes AssignmentSection and AssignmentUnit dropdown fields that default to the coachee's own section/unit
  3. Excel export includes both home and assignment unit/section columns with correct data per row
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 125-01-PLAN.md — Table columns, assign/edit modal dropdowns, and Excel export

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 123. Data Model & Migration | 1/1 | Complete    | 2026-03-08 |
| 124. CDP Access & Lifecycle | 2/2 | Complete    | 2026-03-08 |
| 125. Mapping UI | 1/1 | Complete    | 2026-03-08 |

---

## v3.12 Progress Unit Scoping

**Milestone Goal:** Fix progress data agar hanya berisi kompetensi sesuai unit penugasan coachee — AutoCreateProgress filter by AssignmentUnit, clean migration hapus & recreate progress, dan reassignment handler.

## Phases

- [x] **Phase 128: Unit-Filtered Progress & Clean Migration** - AutoCreateProgress filters by AssignmentUnit + migration wipes and recreates all progress data (completed 2026-03-08)
- [x] **Phase 129: Sync, Reassignment & Defensive Query** - SilabusSave unit-aware sync, edit-mapping reassignment handler, and CoachingProton belt-and-suspenders filter (completed 2026-03-08)

## Phase Details

### Phase 128: Unit-Filtered Progress & Clean Migration
**Goal**: Progress data contains only deliverables matching the coachee's assignment unit, with all existing data cleaned and recreated correctly
**Depends on**: Nothing (foundation phase, builds on v3.11 AssignmentUnit field)
**Requirements**: PROG-01, MIG-01, MIG-02
**Success Criteria** (what must be TRUE):
  1. When a ProtonTrackAssignment is created, AutoCreateProgressForAssignment only creates progress rows for deliverables where ProtonKompetensi.Unit matches the coachee's AssignmentUnit from CoachCoacheeMapping
  2. After migration runs, all old ProtonDeliverableProgress, CoachingSessions, and DeliverableStatusHistory rows are deleted
  3. After migration runs, every active ProtonTrackAssignment has fresh progress rows created with the correct unit filter applied
  4. A coachee assigned to unit "Alkylation" sees only Alkylation-scoped kompetensi in their CoachingProton table (no cross-unit leakage)
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

Plans:
- [ ] 128-01-PLAN.md — Unit-filtered AutoCreateProgress + clean migration

### Phase 129: Sync, Reassignment & Defensive Query
**Goal**: All secondary progress-creation paths respect unit scoping, and unit changes trigger automatic progress rebuild
**Depends on**: Phase 128 (needs unit-filtered AutoCreateProgress logic)
**Requirements**: PROG-02, REASSIGN-01, QUERY-01
**Success Criteria** (what must be TRUE):
  1. When HC saves new silabus deliverables via SilabusSave, auto-sync only creates progress for assignments whose AssignmentUnit matches the deliverable's ProtonKompetensi.Unit
  2. When Admin/HC edits a mapping's AssignmentUnit, the coachee's old progress is deleted and new progress is created matching the new unit
  3. CoachingProton query includes a defensive filter ensuring displayed deliverables belong to kompetensi matching the assignment's unit
**Plans**: 1 plan
nPlans:
- [ ] 129-01-PLAN.md — SilabusSave auto-sync + reassignment rebuild + defensive unit filter

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 128. Unit-Filtered Progress & Clean Migration | 1/1 | Complete    | 2026-03-08 |
| 129. Sync, Reassignment & Defensive Query | 1/1 | Complete    | 2026-03-08 |
