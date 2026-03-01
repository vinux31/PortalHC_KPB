# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 CMP Assessment Completion** — Phases 1-3 (shipped 2026-02-17)
- ✅ **v1.1 CDP Coaching Management** — Phases 4-8 (shipped 2026-02-18)
- ✅ **Post-v1.1 Fix: Admin Role Switcher** — Phase 8 (shipped 2026-02-18)
- ✅ **v1.2 UX Consolidation** — Phases 9-12 (shipped 2026-02-19)
- ✅ **v1.3 Assessment Management UX** — Phases 13-15 (shipped 2026-02-19)
- ✅ **v1.4 Assessment Monitoring** — Phase 16 (shipped 2026-02-19)
- ✅ **v1.5 Question and Exam UX** — Phase 17 (shipped 2026-02-19)
- ✅ **v1.6 Training Records Management** — Phases 18-20 (shipped 2026-02-20)
- ✅ **v1.7 Assessment System Integrity** — Phases 21-26 (shipped 2026-02-21)
- ✅ **v1.8 Assessment Polish** — Phases 27-32 (shipped 2026-02-23)
- ✅ **v1.9 Proton Catalog Management** — Phases 33-37 (shipped 2026-02-24)
- ✅ **v2.0 Assessment Management & Training History** — Phases 38-40 (shipped 2026-02-24)
- ✅ **v2.1 Assessment Resilience & Real-Time Monitoring** — Phases 41-45 (shipped 2026-02-25)
- ✅ **v2.2 Attempt History** — Phase 46 (shipped 2026-02-26)
- 🚧 **v2.3 Admin Portal** — Phases 47-62 (in progress)
- 🚧 **v2.4 CDP Progress** — Phases 63-66 (in progress)
- 📋 **v2.5 User Infrastructure & AD Readiness** — Phases 67-73 (planned)

## Phases

<details>
<summary>✅ v1.0 CMP Assessment Completion (Phases 1-3) — SHIPPED 2026-02-17</summary>

### Phase 1: Assessment Results & Configuration
**Goal:** Users can see their assessment results with pass/fail status and review answers, HC can configure pass thresholds and answer review visibility per assessment

- [x] 01-01: Database schema changes (PassPercentage, AllowAnswerReview, IsPassed, CompletedAt)
- [x] 01-02: Assessment configuration UI (Create/Edit form enhancements)
- [x] 01-03: Results page, SubmitExam redirect, and lobby links

**Completed:** 2026-02-14

---

### Phase 2: HC Reports Dashboard
**Goal:** HC staff can view, analyze, and export assessment results across all users with filtering and performance analytics

- [x] 02-01: Reports dashboard foundation (ViewModels, controller, view with filters, stats, and paginated table)
- [x] 02-02: Excel export with ClosedXML and individual user assessment history
- [x] 02-03: Performance analytics charts (Chart.js pass rate by category, score distribution)

**Completed:** 2026-02-14

---

### Phase 3: KKJ/CPDP Integration
**Goal:** Assessment results automatically inform competency tracking and generate personalized development recommendations

- [x] 03-01: Data foundation (competency models, DbContext, position helper, migration)
- [x] 03-02: Auto-update competency on assessment completion + seed data
- [x] 03-03: Gap analysis dashboard with radar chart and IDP suggestions
- [x] 03-04: CPDP progress tracking with assessment evidence + visual verification

**Completed:** 2026-02-14

---

**Milestone Summary:**
- 3 phases, 10 plans completed
- 6/6 functional requirements satisfied
- Full assessment workflow with results, analytics, and competency integration
- See `.planning/milestones/v1.0-ROADMAP.md` for full details

</details>

<details>
<summary>✅ v1.1 CDP Coaching Management (Phases 4-8) — SHIPPED 2026-02-18</summary>

### Phase 4: Foundation & Coaching Sessions
**Goal:** Coaches can log sessions and action items against a stable data model, with users able to view their full coaching history
**Depends on:** Phase 3 (v1.0 complete)
**Requirements:** COACH-01, COACH-02, COACH-03
**Success Criteria** (what must be TRUE):
  1. Coach can create a coaching session with domain-specific fields (Kompetensi, SubKompetensi, Deliverable, CoacheeCompetencies, CatatanCoach, Kesimpulan, Result) for a coachee
  2. Coach can add action items with due dates to a coaching session
  3. User can view their coaching session history with date and status filtering
  4. All existing v1.0 features remain functional after schema migration (broken CoachingLog FK fixed)
**Plans:** 3 plans

Plans:
- [x] 04-01-PLAN.md — Data foundation: models, DbContext, CoachingLog cleanup, migration
- [x] 04-02-PLAN.md — Controller actions and view: coaching CRUD with filtering
- [x] 04-03-PLAN.md — Gap closure: replace Topic/Notes with domain-specific coaching fields

#### Phase 5: Proton Deliverable Tracking
**Goal:** Coachee can track assigned deliverables in a structured Kompetensi hierarchy, with coaches able to upload and revise evidence files sequentially
**Depends on:** Phase 4
**Requirements:** PROTN-01, PROTN-02, PROTN-03, PROTN-04, PROTN-05
**Success Criteria** (what must be TRUE):
  1. Coach or SrSpv can assign a coachee to a Proton track (Panelman or Operator, Tahun 1/2/3) from the Proton Main page
  2. Coachee can view their full deliverable list on the IDP Plan page organized by Kompetensi > Sub Kompetensi > Deliverable (read-only, no status, no navigation links)
  3. Coachee can only access the next deliverable after the current one is approved — sequential lock is enforced
  4. Coach can upload evidence files for an active deliverable on the Deliverable page
  5. Coach can revise evidence and resubmit a rejected deliverable
**Plans:** 3 plans

Plans:
- [x] 05-01-PLAN.md — Data foundation: Proton models, DbContext, migration, seed data
- [x] 05-02-PLAN.md — ProtonMain track assignment page and PlanIdp hybrid Coachee view
- [x] 05-03-PLAN.md — Deliverable page with sequential lock, evidence upload, and resubmit

#### Phase 6: Approval Workflow & Completion
**Goal:** Deliverables move through the SrSpv/SectionHead approval chain to completion, with HC completing final approvals before creating a final Proton Assessment that updates competency levels
**Depends on:** Phase 5
**Requirements:** APPRV-01, APPRV-02, APPRV-03, APPRV-04, APPRV-05, APPRV-06, PROTN-06, PROTN-07, PROTN-08
**Success Criteria** (what must be TRUE):
  1. Coach can submit a deliverable for approval
  2. SrSpv or SectionHead can approve or reject a submitted deliverable — either approver alone is sufficient for the coachee to proceed
  3. Approver can reject with a written reason; both coach and coachee can see rejection status and reason
  4. HC receives notification when a coachee completes all deliverables; HC approval is non-blocking per deliverable but HC must complete all pending approvals before creating a final Proton Assessment
  5. Coachee's Proton view shows final assessment status and resulting competency level update
**Plans:** 3 plans

Plans:
- [x] 06-01-PLAN.md — Data foundation: extend models, add ProtonNotification/ProtonFinalAssessment, migration
- [x] 06-02-PLAN.md — Approve/Reject actions with rejection reasons and sequential unlock
- [x] 06-03-PLAN.md — HC workflow: HCApprovals queue, final assessment, PlanIdp completion card

**Completed:** 2026-02-18

#### Phase 7: Development Dashboard
**Goal:** Supervisors and HC can monitor team competency progress, deliverable status, and pending approvals from a role-scoped dashboard with trend charts
**Depends on:** Phase 6
**Requirements:** DASH-01, DASH-02, DASH-03, DASH-04
**Success Criteria** (what must be TRUE):
  1. Dashboard is accessible to Spv, SrSpv, SectionHead, HC, and Admin — coachees have no access
  2. Dashboard data is scoped by role: Spv sees their unit only; SrSpv and SectionHead see their section; HC and Admin see all sections
  3. Dashboard shows each team member's deliverable progress, pending approvals, and competency status
  4. Dashboard includes Chart.js charts showing competency level changes over time
**Plans:** 2 plans

Plans:
- [x] 07-01-PLAN.md — ViewModel + CDPController.DevDashboard GET action with role-scoped queries and chart data
- [x] 07-02-PLAN.md — DevDashboard.cshtml view with charts and coachee table, plus _Layout.cshtml nav link

### Phase 8: Fix Admin Role Switcher
**Goal:** Admin can switch between all role views (HC, Atasan, Coach, Coachee, Admin) with each simulated view granting the correct access to controller actions and showing accurate data
**Depends on:** Phase 7
**Plans:** 2 plans

Plans:
- [x] 08-01-PLAN.md — Enable Admin view: add "Admin" to allowedViews, _Layout dropdown, and SeedData default
- [x] 08-02-PLAN.md — Fix CDPController gates: HC-gated actions, Atasan-gated actions, null-Section coachee lists, CreateSession Coachee block

**Completed:** 2026-02-18

</details>

<details>
<summary>✅ v1.2 UX Consolidation (Phases 9-12) — SHIPPED 2026-02-19</summary>

- [x] Phase 9: Gap Analysis Removal (1/1 plans) — completed 2026-02-18
- [x] Phase 10: Unified Training Records (2/2 plans) — completed 2026-02-18
- [x] Phase 11: Assessment Page Role Filter (2/2 plans) — completed 2026-02-18
- [x] Phase 12: Dashboard Consolidation (3/3 plans) — completed 2026-02-19

See `.planning/milestones/v1.2-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v1.3 Assessment Management UX (Phases 13-15) — SHIPPED 2026-02-19</summary>

- [x] Phase 13: Navigation & Creation Flow (1/1 plans) — completed 2026-02-19
- [x] Phase 14: Bulk Assign (1/1 plans) — completed 2026-02-19
- [~] Phase 15: Quick Edit — Cancelled (feature reverted; Edit page used instead)

See `.planning/milestones/v1.3-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v1.4 Assessment Monitoring (Phase 16) — SHIPPED 2026-02-19</summary>

- [x] Phase 16: Grouped Monitoring View (3/3 plans) — completed 2026-02-19

See `.planning/milestones/v1.5-REQUIREMENTS.md` for MON requirement traceability.

</details>

<details>
<summary>✅ v1.5 Question and Exam UX (Phase 17) — SHIPPED 2026-02-19</summary>

- [x] Phase 17: Question and Exam UX improvements (7/7 plans) — completed 2026-02-19

See `.planning/milestones/v1.5-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v1.6 Training Records Management (Phases 18-20) — SHIPPED 2026-02-20</summary>

- [x] Phase 18: Data Foundation (1/1 plans) — completed 2026-02-20
- [x] Phase 19: HC Create Training Record + Certificate Upload (1/1 plans) — completed 2026-02-20
- [x] Phase 20: Edit, Delete, and RecordsWorkerList Wiring (1/1 plans) — completed 2026-02-20

See `.planning/milestones/v1.6-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v1.7 Assessment System Integrity (Phases 21-26) — SHIPPED 2026-02-21</summary>

- [x] Phase 21: Exam State Foundation (1/1 plan) — completed 2026-02-20
- [x] Phase 22: Exam Lifecycle Actions (4/4 plans) — completed 2026-02-20
- [x] Phase 23: Package Answer Integrity (3/3 plans) — completed 2026-02-21
- [x] Phase 24: HC Audit Log (2/2 plans) — completed 2026-02-21
- [x] Phase 25: Worker UX Enhancements (2/2 plans) — completed 2026-02-21
- [x] Phase 26: Data Integrity Safeguards (2/2 plans) — completed 2026-02-21

See `.planning/milestones/v1.7-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v1.8 Assessment Polish (Phases 27-32) — SHIPPED 2026-02-23</summary>

- [x] Phase 27: Monitoring Status Fix (1/1 plans) — completed 2026-02-21
- [x] Phase 28: Package Reshuffle (2/2 plans) — completed 2026-02-21
- [x] Phase 29: Auto-transition Upcoming to Open (3/3 plans) — completed 2026-02-21
- [x] Phase 30: Import Deduplication (1/1 plans) — completed 2026-02-23
- [x] Phase 31: HC Reporting Actions (2/2 plans) — completed 2026-02-23
- [x] Phase 32: Fix Legacy Question Path (1/1 plans) — completed 2026-02-21

</details>

<details>
<summary>✅ v1.9 Proton Catalog Management (Phases 33-37) — SHIPPED 2026-02-24</summary>

- [x] Phase 33: ProtonTrack Schema (2/2 plans) — completed 2026-02-23
- [x] Phase 34: Catalog Page (2/2 plans) — completed 2026-02-23
- [x] Phase 35: CRUD Add and Edit (2/2 plans) — completed 2026-02-24
- [x] Phase 36: Delete Guards (2/2 plans) — completed 2026-02-24
- [~] Phase 37: Drag-and-Drop Reorder — Cancelled (SortableJS incompatible with nested-table tree; collapse-state preservation shipped instead)

See `.planning/milestones/v1.9-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v2.0 Assessment Management & Training History (Phases 38-40) — SHIPPED 2026-02-24</summary>

- [x] Phase 38: Auto-Hide Filter (1/1 plans) — completed 2026-02-24
- [x] Phase 39: Close Early (2/2 plans) — completed 2026-02-24
- [x] Phase 40: Training Records History Tab (2/2 plans) — completed 2026-02-24

See `.planning/milestones/v2.0-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v2.1 Assessment Resilience & Real-Time Monitoring (Phases 41-45) — SHIPPED 2026-02-25</summary>

- [x] Phase 41: Auto-Save (2/2 plans) — completed 2026-02-24
- [x] Phase 42: Session Resume (4/4 plans) — completed 2026-02-24
- [x] Phase 43: Worker Polling (2/2 plans) — completed 2026-02-25
- [x] Phase 44: Real-Time Monitoring (2/2 plans) — completed 2026-02-25
- [x] Phase 45: Cross-Package Per-Position Shuffle (3/3 plans) — completed 2026-02-25

See `.planning/milestones/v2.1-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v2.2 Attempt History (Phase 46) — SHIPPED 2026-02-26</summary>

- [x] Phase 46: Attempt History (2/2 plans) — completed 2026-02-26

See `.planning/milestones/v2.2-ROADMAP.md` for full details.

</details>

## v2.3 Admin Portal

### Phases

- [x] **Phase 47: KKJ Matrix Manager** — MDAT-01 (complete 2026-02-26)
- [x] **Phase 48: CPDP Items Manager** — MDAT-02 (complete 2026-02-26)
- [x] **Phase 49: Assessment Management Migration** — MDAT-03 (planned) (completed 2026-02-27)
- [x] **Phase 50: Coach-Coachee Mapping Manager** — OPER-01 (planned) (completed 2026-02-27)
- [x] **Phase 51: Proton Silabus & Coaching Guidance Manager** — (planned) (completed 2026-02-27)
- [x] **Phase 52: DeliverableProgress Override** — OPER-03 (planned) (completed 2026-02-27)
- [x] **Phase 53: Final Assessment Manager** — OPER-04 (planned) (completed 2026-03-01)
- [ ] **Phase 54: Coaching Session Override** — OPER-05 (planned)
- [x] ~~**Phase 55: Question Bank Edit**~~ — REMOVED (covered by Phase 61 Assessment Management consolidation)
- [x] ~~**Phase 56: Package Question Edit/Delete**~~ — REMOVED (covered by Phase 61 Assessment Management consolidation)
- [x] ~~**Phase 57: ProtonTrack Edit/Delete**~~ — REMOVED (covered by Phase 60 Proton Catalog consolidation)
- [x] ~~**Phase 58: Password Reset Standalone**~~ — REMOVED (covered by Phase 59 Kelola Pekerja consolidation)
- [~] **Phase 59: Konsolidasi Kelola Pekerja** — ~~CONS-01~~ Superseded by v2.5 Phase 69
- [ ] **Phase 60: Konsolidasi Proton Catalog** — CONS-02 (planned)
- [ ] **Phase 61: Konsolidasi Assessment Management** — CONS-03 (planned)
- [~] **Phase 62: Update Kelola Data Hub** — ~~CONS-04~~ Partially superseded by v2.5 Phase 70

### Phase Details

### Phase 47: KKJ Matrix Manager
**Goal:** Admin can view, create, edit, and delete KKJ Matrix items (KkjMatrixItem) through a dedicated management page — no database or code change required to manage master data
**Depends on:** Phase 46 (v2.2 complete)
**Requirements:** MDAT-01
**Success Criteria** (what must be TRUE):
  1. Admin can navigate to a KKJ Matrix management page that lists all KkjMatrixItem records
  2. Admin can create a new KkjMatrixItem with all required fields (KKJ code, Kompetensi, SubKompetensi, Level) via an inline or modal form
  3. Admin can edit an existing KkjMatrixItem's fields inline or via modal
  4. Admin can delete a KkjMatrixItem with appropriate guard (show usage count or warn if in use)
  5. All CRUD operations persist immediately to SQL Server and reflect in the list without full page reload
**Plans:** 4/5 plans executed

Plans:
- [x] 47-01-PLAN.md — Admin Portal infrastructure: AdminController, /Admin/Index hub page, /Admin/KkjMatrix read-mode table, Kelola Data nav link
- [x] 47-02-PLAN.md — KKJ Matrix write operations: spreadsheet edit mode (all 20 cols), bulk-save POST, delete-with-guard, clipboard paste
- [ ] 47-03-PLAN.md — GAP: Per-bagian tables + editable headers (KkjBagian entity, EF migration, grouped view, KkjBagianSave)
- [ ] 47-04-PLAN.md — GAP: Full 21-column read-mode table + per-row insert/delete in edit mode
- [ ] 47-05-PLAN.md — GAP: Excel multi-cell selection (drag, Ctrl+C/V, Delete range) + save toast

### Phase 48: CPDP Items Manager (KKJ-IDP Mapping Editor)
**Goal:** Admin can view, create, edit, and delete CPDP Items (CpdpItem) with section filter through a dedicated management page — spreadsheet-style inline editing, bulk-save, delete guard, multi-cell clipboard, and Excel export
**Depends on:** Phase 47
**Requirements:** MDAT-02
**Success Criteria** (what must be TRUE):
  1. Admin can navigate to a CPDP Items management page that lists all CpdpItem records, filterable by section dropdown (RFCC, GAST, NGP, DHT)
  2. Admin can create, edit, and delete CpdpItem records through spreadsheet-style inline editing with bulk-save and no reference guard blocking deletion
  3. Admin can copy-paste data from Excel using multi-cell clipboard operations
  4. Admin can export filtered data to Excel
  5. CMP/Mapping section select page updated to use dropdown instead of card selection
**Plans:** 4/4 plans complete

Plans:
- [x] 48-01-PLAN.md — GET action + read-mode table + section dropdown + Admin/Index link update
- [x] 48-02-PLAN.md — Edit mode table + CpdpItemsSave POST + CpdpItemDelete POST + CMP/Mapping dropdown update
- [x] 48-03-PLAN.md — Multi-cell selection (Ctrl+C/V, Delete range) + Excel export endpoint
- [ ] 48-04-PLAN.md — GAP: Read-mode 6 columns, remove delete guard, fix Delete-key operator precedence

### Phase 49: Assessment Management Migration
**Goal:** Move Manage Assessments from CMP to Kelola Data (/Admin) — migrate all manage actions (Create, Edit, Delete, Reset, Force Close, Export, Monitoring, History) from CMPController to AdminController, move AuditLog to Admin, clean up CMP/Assessment to pure personal view
**Depends on:** Phase 48
**Requirements:** MDAT-03
**Success Criteria** (what must be TRUE):
  1. HC/Admin can access Manage Assessments from /Admin/ManageAssessment with all existing functionality (Create, Edit, Delete, Reset, Force Close, Export, Monitoring Detail, User History)
  2. AuditLog page accessible at /Admin/AuditLog showing all global audit entries
  3. CMP/Assessment is pure personal view — no manage toggle, no manage-related UI elements
  4. Card 'Assessment Competency Map' in Admin/Index replaced with 'Manage Assessments' linking to /Admin/ManageAssessment
  5. Card 'Manage Assessments' removed from CMP Index, card 'Assessment Lobby' renamed to 'My Assessments'
  6. All manage-related actions removed from CMPController, AuditLog removed from CMPController
**Plans:** 5/5 plans complete

Plans:
- [x] 49-01-PLAN.md — AdminController ManageAssessment GET + ManageAssessment.cshtml + Admin/Index card update
- [x] 49-02-PLAN.md — Create/Edit/Delete/RegenerateToken actions + CreateAssessment.cshtml + EditAssessment.cshtml
- [x] 49-03-PLAN.md — Monitoring, Reset, ForceClose, Export, UserHistory actions + view files
- [x] 49-04-PLAN.md — AuditLog migration + CMPController cleanup + CMP/Assessment personal-only + CMP/Index card updates
- [ ] 49-05-PLAN.md — GAP: Fix success modal, composite key migration, UserAssessmentHistory link, token guard

**Completed:** 2026-02-27

### Phase 50: Coach-Coachee Mapping Manager
**Goal:** Admin can view, create, edit, and delete Coach-Coachee Mappings (CoachCoacheeMapping) through a grouped-by-coach management page with bulk assign, soft-delete, optional ProtonTrack assignment, section filter, Excel export, and AuditLog integration
**Depends on:** Phase 49
**Requirements:** OPER-01
**Plans:** 2/2 plans complete

Plans:
- [ ] 50-01-PLAN.md — GET scaffold + grouped-by-coach view + filters/pagination + modal skeletons + Admin/Index card activation
- [ ] 50-02-PLAN.md — Write endpoints (Assign/Edit/Deactivate/Reactivate) + ProtonTrack side-effect + AuditLog + Excel export + modal JS wiring

### Phase 51: Proton Silabus & Coaching Guidance Manager
**Goal:** Admin/HC can manage Proton silabus data (Bagian > Unit > Track > Kompetensi > SubKompetensi > Deliverable) and coaching guidance files through /Admin/ProtonData with two tabs — replaces ProtonCatalog page
**Depends on:** Phase 50
**Requirements:** —
**Plans:** 3/3 plans complete

### Phase 52: DeliverableProgress Override
**Goal:** Admin/HC can view all ProtonDeliverableProgress records in a third /ProtonData tab and override stuck or erroneous statuses; sequential lock removed — all deliverables Active on assignment
**Depends on:** Phase 51
**Requirements:** OPER-03
**Plans:** 2/2 plans complete

Plans:
- [ ] 52-01-PLAN.md — Override tab UI + OverrideList/OverrideDetail/OverrideSave endpoints + badge table + override modal + AuditLog
- [ ] 52-02-PLAN.md — Lock removal: CDPController AssignTrack/Deliverable/ApproveDeliverable cleanup + CoacheeProgressRow.Locked removal + EF data migration

### Phase 53: Final Assessment Manager
**Goal:** Add "Assessment Proton" exam category to the assessment system — HC can create Proton exams (Tahun 1-2 online, Tahun 3 interview), with eligibility-gated coachee picker, Tahun 3 interview result input in MonitoringDetail; legacy HCApprovals and CreateFinalAssessment pages removed
**Depends on:** Phase 52
**Requirements:** OPER-04
**Plans:** 3/3 plans complete

Plans:
- [ ] 53-01-PLAN.md — Data model extension: AssessmentSession ProtonTrackId/TahunKe/InterviewResultsJson fields + InterviewResultsDto POCO + EF migration
- [ ] 53-02-PLAN.md — Adaptive CreateAssessment form + GetEligibleCoachees AJAX endpoint + badge display in ManageAssessment/CMP/Assessment
- [ ] 53-03-PLAN.md — Tahun 3 interview form in MonitoringDetail + SubmitInterviewResults POST + delete HCApprovals/CreateFinalAssessment + legacy data cleanup

### Phase 54: Coaching Session Override
**Goal:** Admin can view all CoachingSession and ActionItem records and perform override edits or deletions
**Depends on:** Phase 53
**Requirements:** OPER-05
**Plans:** TBD

### ~~Phase 55: Question Bank Edit~~ — REMOVED
Covered by Phase 61 (Assessment Management consolidation). Question edit will be added as enhancement to ManageQuestions within the consolidated Assessment Management page.

### ~~Phase 56: Package Question Edit/Delete~~ — REMOVED
Covered by Phase 61 (Assessment Management consolidation). Package question edit/delete will be added as enhancement to ManagePackages within the consolidated Assessment Management page.

### ~~Phase 57: ProtonTrack Edit/Delete~~ — REMOVED
Covered by Phase 60 (Proton Catalog consolidation). ProtonTrack already has full CRUD in /ProtonCatalog — Edit/Delete are already implemented.

### ~~Phase 58: Password Reset Standalone~~ — REMOVED
Covered by Phase 59 (Kelola Pekerja consolidation). EditWorker already has password change fields — no standalone page needed.

### ~~Phase 59: Konsolidasi Kelola Pekerja~~ — Superseded by v2.5 Phase 69
**Status:** Superseded — ManageWorkers migration to Admin is now covered by v2.5 Phase 69 (ManageWorkers Migration to Admin) with expanded scope including USTR-02 (GetDefaultView extraction).
**Original Requirements:** CONS-01 → replaced by USR-01, USR-02, USR-03, USTR-02

### Phase 60: Konsolidasi Proton Catalog
**Goal:** Move Proton Catalog (master data Track/Kompetensi/SubKompetensi/Deliverable CRUD) from standalone /ProtonCatalog into Kelola Data Section A — all master data tables in one hub
**Depends on:** Phase 59
**Requirements:** CONS-02
**Success Criteria** (what must be TRUE):
  1. Admin/HC can access Proton Catalog from Kelola Data hub page (Section A card)
  2. All Proton Catalog CRUD functionality works from Admin section (tree view, add/edit/delete at all 4 levels)
  3. Old /ProtonCatalog URL redirects to new location
**Plans:** TBD

### Phase 61: Konsolidasi Assessment Management
**Goal:** Move Assessment Management (CRUD assessment, kelola soal, kelola paket, monitoring, export) from CMP into Kelola Data Section B — HC manages all assessment admin from one hub. Personal assessment view (take exam, results) stays in CMP.
**Depends on:** Phase 60
**Requirements:** CONS-03
**Success Criteria** (what must be TRUE):
  1. Admin/HC can access Assessment Management from Kelola Data hub page (Section B card)
  2. All assessment admin functions work from Admin section (create/edit/delete assessment, manage questions, manage packages, monitoring, export, audit log)
  3. CMP/Assessment personal view (worker taking exams, viewing results) remains accessible and unchanged
  4. Old /CMP/Assessment?view=manage URL redirects to new location
**Plans:** TBD

### ~~Phase 62: Update Kelola Data Hub~~ — Partially superseded by v2.5 Phase 70
**Status:** Partially superseded — Kelola Data hub reorganization (ManageWorkers card prominent, stale cleanup) is now covered by v2.5 Phase 70. Remaining consolidation items (Proton Catalog, Assessment Management cards) stay in v2.3 scope if Phase 60/61 are executed.
**Original Requirements:** CONS-04 → partially replaced by USR-04

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Assessment Results & Configuration | v1.0 | 3/3 | Complete | 2026-02-14 |
| 2. HC Reports Dashboard | v1.0 | 3/3 | Complete | 2026-02-14 |
| 3. KKJ/CPDP Integration | v1.0 | 4/4 | Complete | 2026-02-14 |
| 4. Foundation & Coaching Sessions | v1.1 | 3/3 | Complete | 2026-02-17 |
| 5. Proton Deliverable Tracking | v1.1 | 3/3 | Complete | 2026-02-17 |
| 6. Approval Workflow & Completion | v1.1 | 3/3 | Complete | 2026-02-18 |
| 7. Development Dashboard | v1.1 | 2/2 | Complete | 2026-02-18 |
| 8. Fix Admin Role Switcher | post-v1.1 | 2/2 | Complete | 2026-02-18 |
| 9. Gap Analysis Removal | v1.2 | 1/1 | Complete | 2026-02-18 |
| 10. Unified Training Records | v1.2 | 2/2 | Complete | 2026-02-18 |
| 11. Assessment Page Role Filter | v1.2 | 2/2 | Complete | 2026-02-18 |
| 12. Dashboard Consolidation | v1.2 | 3/3 | Complete | 2026-02-19 |
| 13. Navigation & Creation Flow | v1.3 | 1/1 | Complete | 2026-02-19 |
| 14. Bulk Assign | v1.3 | 1/1 | Complete | 2026-02-19 |
| 15. Quick Edit | v1.3 | 0/1 | Cancelled | 2026-02-19 |
| 16. Grouped Monitoring View | v1.4 | 3/3 | Complete | 2026-02-19 |
| 17. Question and Exam UX | v1.5 | 7/7 | Complete | 2026-02-19 |
| 18. Data Foundation | v1.6 | 1/1 | Complete | 2026-02-20 |
| 19. HC Create Training Record + Certificate Upload | v1.6 | 1/1 | Complete | 2026-02-20 |
| 20. Edit, Delete, and RecordsWorkerList Wiring | v1.6 | 1/1 | Complete | 2026-02-20 |
| 21. Exam State Foundation | v1.7 | 1/1 | Complete | 2026-02-20 |
| 22. Exam Lifecycle Actions | v1.7 | 4/4 | Complete | 2026-02-20 |
| 23. Package Answer Integrity | v1.7 | 3/3 | Complete | 2026-02-21 |
| 24. HC Audit Log | v1.7 | 2/2 | Complete | 2026-02-21 |
| 25. Worker UX Enhancements | v1.7 | 2/2 | Complete | 2026-02-21 |
| 26. Data Integrity Safeguards | v1.7 | 2/2 | Complete | 2026-02-21 |
| 27. Monitoring Status Fix | v1.8 | 1/1 | Complete | 2026-02-21 |
| 28. Package Reshuffle | v1.8 | 2/2 | Complete | 2026-02-21 |
| 29. Auto-transition Upcoming to Open | v1.8 | 3/3 | Complete | 2026-02-21 |
| 30. Import Deduplication | v1.8 | 1/1 | Complete | 2026-02-23 |
| 31. HC Reporting Actions | v1.8 | 2/2 | Complete | 2026-02-23 |
| 32. Fix Legacy Question Path | v1.8 | 1/1 | Complete | 2026-02-21 |
| 33. ProtonTrack Schema | v1.9 | 2/2 | Complete | 2026-02-23 |
| 34. Catalog Page | v1.9 | 2/2 | Complete | 2026-02-23 |
| 35. CRUD Add and Edit | v1.9 | 2/2 | Complete | 2026-02-24 |
| 36. Delete Guards | v1.9 | 2/2 | Complete | 2026-02-24 |
| 37. Drag-and-Drop Reorder | v1.9 | 0/2 | Cancelled | 2026-02-24 |
| 38. Auto-Hide Filter | v2.0 | 1/1 | Complete | 2026-02-24 |
| 39. Close Early | v2.0 | 2/2 | Complete | 2026-02-24 |
| 40. Training Records History Tab | v2.0 | 2/2 | Complete | 2026-02-24 |
| 41. Auto-Save | v2.1 | 2/2 | Complete | 2026-02-24 |
| 42. Session Resume | v2.1 | 4/4 | Complete | 2026-02-24 |
| 43. Worker Polling | v2.1 | 2/2 | Complete | 2026-02-25 |
| 44. Real-Time Monitoring | v2.1 | 2/2 | Complete | 2026-02-25 |
| 45. Cross-Package Per-Position Shuffle | v2.1 | 3/3 | Complete | 2026-02-25 |
| 46. Attempt History | v2.2 | 2/2 | Complete | 2026-02-26 |
| 47. KKJ Matrix Manager | v2.3 | 9/9 | Complete | 2026-02-26 |
| 48. CPDP Items Manager (KKJ-IDP Mapping Editor) | v2.3 | 4/4 | Complete | 2026-02-26 |
| 49. Assessment Management Migration | 5/5 | Complete    | 2026-02-27 | - |
| 50. Coach-Coachee Mapping Manager | 2/2 | Complete    | 2026-02-27 | - |
| 51. Proton Track Assignment Manager | 3/3 | Complete    | 2026-02-27 | - |
| 52. DeliverableProgress Override | 2/2 | Complete    | 2026-02-27 | - |
| 53. Final Assessment Manager | 3/3 | Complete    | 2026-03-01 | - |
| 54. Coaching Session Override | v2.3 | 0/? | Not started | - |
| 55. ~~Question Bank Edit~~ | v2.3 | - | Removed | - |
| 56. ~~Package Question Edit/Delete~~ | v2.3 | - | Removed | - |
| 57. ~~ProtonTrack Edit/Delete~~ | v2.3 | - | Removed | - |
| 58. ~~Password Reset Standalone~~ | v2.3 | - | Removed | - |
| 59. ~~Konsolidasi Kelola Pekerja~~ | v2.3 | - | Superseded (v2.5 P69) | - |
| 60. Konsolidasi Proton Catalog | v2.3 | 0/? | Not started | - |
| 61. Konsolidasi Assessment Management | v2.3 | 0/? | Not started | - |
| 62. ~~Update Kelola Data Hub~~ | v2.3 | - | Partially superseded (v2.5 P70) | - |
| 63. Data Source Fix | v2.4 | 2/2 | Complete | 2026-02-27 |
| 64. Functional Filters | v2.4 | 2/2 | Complete | 2026-02-27 |
| 65. Actions | v2.4 | 0/? | Not started | - |
| 66. UI Polish | 2/2 | Complete    | 2026-02-28 | - |
| 67. Dynamic Profile Page | 1/1 | Complete    | 2026-02-27 | - |
| 68. Functional Settings Page | 2/2 | Complete    | 2026-02-27 | - |
| 69. ManageWorkers Migration to Admin | 2/2 | Complete    | 2026-02-28 | - |
| 70. Kelola Data Hub Reorganization | 1/1 | Complete    | 2026-02-28 | - |
| 71. LDAP Auth Service Foundation | 2/2 | Complete    | 2026-02-28 | - |
| 72. Dual Auth Login Flow | 3/3 | Complete    | 2026-02-28 | - |
| 73. User Structure Polish | 1/1 | Complete    | 2026-02-28 | - |

## v2.4 CDP Progress

### Phases

- [x] **Phase 63: Data Source Fix** — DATA-01, DATA-02, DATA-03, DATA-04 (completed 2026-02-27)
- [ ] **Phase 64: Functional Filters** — FILT-01, FILT-02, FILT-03, FILT-04, UI-01, UI-03
- [ ] **Phase 65: Actions** — ACTN-01, ACTN-02, ACTN-03, ACTN-04, ACTN-05
- [x] **Phase 66: UI Polish** — UI-02, UI-04 (completed 2026-02-28)

### Phase Details

### Phase 63: Data Source Fix
**Goal:** Progress page queries ProtonDeliverableProgress + ProtonTrackAssignment (not IdpItems), displays real coachee list from CoachCoacheeMapping, and computes correct summary stats — the data foundation is accurate
**Depends on:** Phase 62 (v2.3 complete) — runs in parallel, independent of v2.3 progress
**Requirements:** DATA-01, DATA-02, DATA-03, DATA-04
**Success Criteria** (what must be TRUE):
  1. Progress page table rows come from ProtonDeliverableProgress joined with ProtonTrackAssignment — no IdpItems data appears
  2. Coach sees their real coachees in the dropdown, populated from CoachCoacheeMapping, not hardcoded mock values
  3. Summary stats (progress %, pending actions, pending approvals) match the actual ProtonDeliverableProgress records in the database
  4. Approving or updating evidence on the Deliverable page is immediately reflected on the Progress page with no stale cache
**Plans:** 2/2 plans complete

### Phase 64: Functional Filters
**Goal:** Every filter on the Progress page (Bagian/Unit, Coachee, Track, Tahun, Search) genuinely narrows the data returned — parameters are wired to queries and roles scope what users can see
**Depends on:** Phase 63
**Requirements:** FILT-01, FILT-02, FILT-03, FILT-04, UI-01, UI-03
**Success Criteria** (what must be TRUE):
  1. HC/Admin selecting a Bagian or Unit filter receives only deliverable rows for workers in that Bagian/Unit
  2. Coach selecting a coachee from the dropdown sees only that coachee's deliverable rows — other coachees' data disappears
  3. Selecting Proton Track (Panelman/Operator) and/or Tahun (1/2/3) filters rows to matching assignments only
  4. Typing in the search box hides non-matching competency rows client-side without a page reload
  5. Role-scoped data is enforced: Spv sees their unit, SrSpv/SectionHead see their section, HC/Admin see all — not based on filter selection alone
  6. Filter dropdowns show the currently selected value as selected on page reload (no incorrect HTML selected attribute behavior)
**Plans:** 1/2 plans executed

### Phase 65: Actions
**Goal:** Approve, reject, coaching report, evidence, and export actions all persist to the database — no more console.log stubs or missing onclick handlers
**Depends on:** Phase 64
**Requirements:** ACTN-01, ACTN-02, ACTN-03, ACTN-04, ACTN-05
**Success Criteria** (what must be TRUE):
  1. SrSpv or SectionHead clicking Approve on a deliverable row updates ProtonDeliverableProgress.Status to Approved in the database and the row reflects the new status on reload
  2. SrSpv or SectionHead clicking Reject opens a rejection reason input; submitting it saves the reason to ProtonDeliverableProgress and status becomes Rejected
  3. Coach submitting a coaching report modal creates a new CoachingSession record in the database with the entered details
  4. Clicking Upload Evidence on a deliverable row opens the existing Deliverable workflow (or inline upload); the file is saved and viewable from the Progress page
  5. Export Excel and Export PDF buttons generate and download the current filtered data as a file
**Plans:** TBD

### Phase 66: UI Polish
**Goal:** Progress page handles edge cases gracefully — empty states communicate clearly, and large datasets do not load all rows at once
**Depends on:** Phase 65
**Requirements:** UI-02, UI-04
**Success Criteria** (what must be TRUE):
  1. When no deliverable data matches the current filter/role scope, the table shows a descriptive empty-state message instead of a blank table
  2. Large coachee/deliverable datasets are paginated so the page does not load hundreds of rows at once; user can navigate between pages
**Plans:** 2/2 plans complete

## v2.5 User Infrastructure & AD Readiness

### Phases

- [x] **Phase 67: Dynamic Profile Page** — PROF-01, PROF-02, PROF-03 (planned) (completed 2026-02-27)
- [x] **Phase 68: Functional Settings Page** — PROF-04, PROF-05, PROF-06 (planned) (completed 2026-02-27)
- [x] **Phase 69: ManageWorkers Migration to Admin** — USR-01, USR-02, USR-03, USTR-02 (planned) — supersedes v2.3 Phase 59 (completed 2026-02-28)
- [x] **Phase 70: Kelola Data Hub Reorganization** — USR-04 (planned) — partially supersedes v2.3 Phase 62 (completed 2026-02-28)
- [x] **Phase 71: LDAP Auth Service Foundation** — AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-08, USTR-01 (planned) (completed 2026-02-28)
- [x] **Phase 72: Dual Auth Login Flow** — AUTH-05, AUTH-06, AUTH-07 (planned) (completed 2026-02-28)
- [x] **Phase 73: User Structure Polish** — USTR-02 completion (planned) (completed 2026-02-28)

### Phase Details

### Phase 67: Dynamic Profile Page
**Goal:** Profile page menampilkan data real user login — no more hardcoded placeholders
**Depends on:** Phase 66 (v2.4 complete), or can start in parallel
**Requirements:** PROF-01, PROF-02, PROF-03
**Success Criteria** (what must be TRUE):
  1. Profile page displays real user data: Nama, NIP, Email, Position, Section, Unit, Directorate, Role, JoinDate from @Model
  2. Empty/null fields show placeholder "Belum diisi" instead of blank space or error
  3. Avatar initials are dynamically generated from user's FullName (not hardcoded "BS")
**Plans:** 1/1 plans complete
- [ ] 67-01: Rewrite Profile.cshtml with @Model binding, null-safe display, dynamic initials

### Phase 68: Functional Settings Page
**Goal:** Settings page functional — change password works, edit profile fields, cleanup non-functional items
**Depends on:** Phase 67
**Requirements:** PROF-04, PROF-05, PROF-06
**Success Criteria** (what must be TRUE):
  1. User can change password via ChangePasswordAsync with current/new/confirm fields and validation
  2. User can edit FullName and Position fields; NIP, Email, Role, Section are displayed read-only
  3. Non-functional items (2FA, Notifications, Language) are either removed or displayed as "Belum Tersedia" with disabled state
**Plans:** 2/2 plans complete
- [ ] 68-01: Backend — ChangePasswordViewModel, EditProfileViewModel, AccountController POST actions
- [ ] 68-02: Frontend — Rewrite Settings.cshtml, 2 form sections, remove/disable placeholder items

### Phase 69: ManageWorkers Migration to Admin
**Goal:** Pindahkan seluruh fitur ManageWorkers (CRUD, import, export, detail) dari CMPController ke AdminController — clean break tanpa redirect, akses via Kelola Data hub card, GetDefaultView() helper extraction
**Depends on:** Phase 68
**Requirements:** USR-01, USR-02, USR-03, USTR-02
**Supersedes:** v2.3 Phase 59 (Konsolidasi Kelola Pekerja)
**Success Criteria** (what must be TRUE):
  1. All ManageWorkers CRUD actions (list, create, edit, delete, import, export, detail) work from /Admin/ManageWorkers
  2. Old /CMP/ManageWorkers deleted — no redirect (clean break per user decision, overrides roadmap SC #2)
  3. Standalone "Kelola Pekerja" button removed from navbar — access only via Kelola Data hub
  4. Role-to-SelectedView mapping extracted to shared helper UserRoles.GetDefaultView()
**Plans:** 2/2 plans complete

Plans:
- [ ] 69-01-PLAN.md — Backend migration: 11 actions to AdminController with HC+Admin auth, GetDefaultView() extraction, 5 views copied to Views/Admin/ with all CMP references updated
- [ ] 69-02-PLAN.md — Cleanup: delete from CMPController, delete old CMP views, remove navbar button, add hub card, update RecordsWorkerList, full stale reference verification

### Phase 70: Kelola Data Hub Reorganization
**Goal:** Admin/Index.cshtml restructured — ManageWorkers prominent, stale items cleaned up
**Depends on:** Phase 69
**Requirements:** USR-04
**Partially supersedes:** v2.3 Phase 62 (Update Kelola Data Hub)
**Success Criteria** (what must be TRUE):
  1. Kelola Data hub page (Admin/Index.cshtml) has ManageWorkers card in prominent position
  2. Stale "Segera" items cleaned up — cards for implemented features show active status
  3. Section C removed or reorganized
**Plans:** 1/1 plans complete

Plans:
- [ ] 70-01-PLAN.md — Rewrite Admin/Index.cshtml (3 domain sections: Data Management, Proton, Assessment & Training), activate Deliverable Progress Override card, remove old Kelengkapan CRUD section, update navbar HC visibility

### Phase 71: LDAP Auth Service Foundation
**Goal:** Infrastructure dual auth — NuGet, service interface, implementations, config toggle, AuthSource field. Login flow belum diubah.
**Depends on:** Phase 70
**Requirements:** AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-08, USTR-01
**Success Criteria** (what must be TRUE):
  1. System.DirectoryServices NuGet package added to csproj
  2. ApplicationUser has AuthSource field ("Local"/"AD") with EF migration
  3. appsettings.json has `Authentication:UseActiveDirectory` toggle (dev=false)
  4. IAuthService interface defined with AuthenticateAsync method
  5. LocalAuthService wraps existing PasswordSignInAsync
  6. LdapAuthService uses DirectoryEntry with Pertamina LDAP path and samaccountname filter
  7. Program.cs registers IAuthService via DI based on config toggle
**Plans:** 2/2 plans complete
- [ ] 71-01: NuGet package, AuthSource migration, config sections, IAuthService interface, LocalAuthService
- [ ] 71-02: LdapAuthService with Pertamina DirectoryEntry code + AD attribute extraction, Program.cs DI registration

### Phase 72: Dual Auth Login Flow
**Goal:** Login flow pakai IAuthService — global config routing (no per-user AuthSource), profile sync FullName/Email, ManageWorkers + import adaptation for AD mode
**Depends on:** Phase 71
**Requirements:** AUTH-05, AUTH-06, AUTH-07
**Success Criteria** (what must be TRUE):
  1. Login POST uses IAuthService.AuthenticateAsync instead of direct PasswordSignInAsync
  2. Global config routing: UseActiveDirectory=true → all users via LDAP, false → all users via local Identity
  3. AuthSource field removed from ApplicationUser (EF migration DROP column) — routing is global, not per-user
  4. Login page identical both modes; AD mode shows hint "Login menggunakan akun Pertamina" below form
  5. User not in DB → rejected with "Akun Anda belum terdaftar. Hubungi HC." (no auto-provisioning)
  6. AD login: sync FullName + Email from AuthResult before SignInAsync; skip null values
  7. ManageWorkers: password field hidden + auto-generate in AD mode; FullName/Email read-only for AD users
  8. Import template dynamic: AD mode → no Password column (auto-generate); local mode → with Password column
**Plans:** 3/3 plans complete
- [ ] 72-01: Backend — Rewrite AccountController.Login POST with IAuthService, profile sync, remove AuthSource migration
- [ ] 72-02: Frontend — Login.cshtml hint, ManageWorkers form adaptation (password hide, read-only fields)
- [ ] 72-03: Import — Dynamic template generation, import logic adaptation for AD mode

### Phase 73: User Structure Polish
**Goal:** Finalize — consistent SelectedView mapping, SeedData cleanup, documentation
**Depends on:** Phase 72
**Requirements:** USTR-02 (completion)
**Success Criteria** (what must be TRUE):
  1. All inline role-to-SelectedView switch/if statements replaced with UserRoles.GetDefaultView()
  2. SeedData cleaned up (AuthSource references removed since field dropped in Phase 72)
  3. ARCHITECTURE.md updated with dual auth documentation
**Plans:** 1/1 plans complete
- [ ] 73-01: Replace all inline role-to-view switches with GetDefaultView(), SeedData AuthSource, ARCHITECTURE.md update

### Phase 74: Hybrid Auth & Role Restructuring
**Goal:** Enable hybrid authentication (AD fallback to local) so dedicated Admin KPB user works in production AD mode, plus role/access fixes
**Depends on:** Phase 73
**Requirements:** AUTH-HYBRID (new)
**Success Criteria** (what must be TRUE):
  1. Login flow tries AD first, falls back to local auth if AD fails — dedicated Admin KPB (admin@pertamina.com) can login in AD mode
  2. _Layout.cshtml and AccountController use SelectedView (or role priority) instead of GetRolesAsync().FirstOrDefault() for role display
  3. RoleLevel restructuring: SectionHead level 3, SrSupervisor stays level 4
  4. Upload evidence access restricted to Coach only (not SrSupervisor)
**Plans:** 2/2 plans complete

Plans:
- [ ] 74-01-PLAN.md — HybridAuthService (AD+local fallback for admin@pertamina.com), Program.cs DI update, UserRoles.cs restructuring (Supervisor role + SectionHead level 3)
- [ ] 74-02-PLAN.md — Role display fix (_Layout.cshtml SelectedView), Coach-only evidence upload (CDPController), Coach-only EligibleCoaches (AdminController), EF migration SectionHead RoleLevel 4→3
