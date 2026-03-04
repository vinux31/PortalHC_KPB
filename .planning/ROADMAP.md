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
- ✅ **v2.3 Admin Portal** — Phases 47-53, 59 (shipped 2026-03-01)
- ✅ **v2.4 CDP Progress** — Phases 61-64 (shipped 2026-03-01)
- ✅ **v2.5 User Infrastructure & AD Readiness** — Phases 65-72 (shipped 2026-03-01)
- ✅ **v2.6 Codebase Cleanup** — Phases 73-78 (shipped 2026-03-01)
- ✅ **v2.7 Assessment Monitoring** — Phases 79-81 (shipped 2026-03-01)
- 🚧 **v3.0 Full QA & Feature Completion** — Phases 82-91 (in progress)
- ✅ **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 (shipped 2026-03-03)

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

- [x] Phase 4: Foundation & Coaching Sessions (3/3 plans) — completed 2026-02-18
- [x] Phase 5: Proton Deliverable Tracking (3/3 plans) — completed 2026-02-18
- [x] Phase 6: Approval Workflow & Completion (3/3 plans) — completed 2026-02-18
- [x] Phase 7: Development Dashboard (2/2 plans) — completed 2026-02-18
- [x] Phase 8: Fix Admin Role Switcher (2/2 plans) — completed 2026-02-18

See `.planning/milestones/` for full details.

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

<details>
<summary>✅ v2.3 Admin Portal (Phases 47-53, 59) — SHIPPED 2026-03-01</summary>

- [x] Phase 47: KKJ Matrix Manager (9/9 plans) — completed 2026-02-26
- [x] Phase 48: CPDP Items Manager (4/4 plans) — completed 2026-02-26
- [x] Phase 49: Assessment Management Migration (5/5 plans) — completed 2026-02-27
- [x] Phase 50: Coach-Coachee Mapping Manager (2/2 plans) — completed 2026-02-27
- [x] Phase 51: Proton Silabus & Coaching Guidance Manager (3/3 plans) — completed 2026-02-27
- [x] Phase 52: DeliverableProgress Override (2/2 plans) — completed 2026-02-27
- [x] Phase 53: Final Assessment Manager (3/3 plans) — completed 2026-03-01
- [x] Phase 59: Hapus Page ProtonCatalog (1/1 plans) — completed 2026-03-01

See `.planning/milestones/v2.3-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v2.4 CDP Progress (Phases 61-64) — SHIPPED 2026-03-01</summary>

- [x] Phase 61: Data Source Fix (2/2 plans) — completed 2026-02-27
- [x] Phase 62: Functional Filters (2/2 plans) — completed 2026-02-27
- [x] Phase 63: Actions (3/3 plans) — completed 2026-02-27
- [x] Phase 64: UI Polish (2/2 plans) — completed 2026-02-28

See `.planning/milestones/v2.4-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v2.5 User Infrastructure & AD Readiness (Phases 65-72) — SHIPPED 2026-03-01</summary>

- [x] Phase 65: Dynamic Profile Page (1/1 plans) — completed 2026-02-27
- [x] Phase 66: Functional Settings Page (2/2 plans) — completed 2026-02-27
- [x] Phase 67: ManageWorkers Migration to Admin (2/2 plans) — completed 2026-02-28
- [x] Phase 68: Kelola Data Hub Reorganization (1/1 plans) — completed 2026-02-28
- [x] Phase 69: LDAP Auth Service Foundation (2/2 plans) — completed 2026-02-28
- [x] Phase 70: Dual Auth Login Flow (3/3 plans) — completed 2026-02-28
- [x] Phase 71: User Structure Polish (1/1 plans) — completed 2026-02-28
- [x] Phase 72: Hybrid Auth & Role Restructuring (2/2 plans) — completed 2026-02-28

See `.planning/milestones/v2.5-ROADMAP.md` for full details.

</details>

<details>
<summary>✅ v2.6 Codebase Cleanup (Phases 73-78) — SHIPPED 2026-03-01</summary>

- [x] Phase 73: Critical Fixes (2/2 plans) — completed 2026-03-01
- [x] Phase 74: Dead Code Removal (2/2 plans) — completed 2026-03-01
- [x] Phase 75: Placeholder Cleanup (2/2 plans) — completed 2026-03-01
- [x] Phase 76: Role Fixes & Broken Link (2/2 plans) — completed 2026-03-01
- [x] Phase 77: Merge Training Records into Manage Assessment & Training (3/3 plans) — completed 2026-03-01
- [x] Phase 78: Deduplicate CMP page (1/1 plans) — completed 2026-03-01

</details>


<details>
<summary>✅ v2.7 Assessment Monitoring (Phases 79-81) — SHIPPED 2026-03-01</summary>

- [x] Phase 79: Assessment Monitoring Page — Group List (1/1 plans) — completed 2026-03-01
- [x] Phase 80: Per-Participant Monitoring Detail & HC Actions (1/1 plans) — completed 2026-03-01
- [x] Phase 81: Cleanup — Remove Old Entry Points (2/2 plans) — completed 2026-03-01

See `.planning/milestones/v2.7-ROADMAP.md` for full details.

</details>

---

### 🚧 v3.0 Full QA & Feature Completion (In Progress)

**Milestone Goal:** Comprehensive end-to-end QA of all portal flows organized by use-case (not page-by-page), code cleanup to remove orphaned/duplicate paths, UI rename from "Proton Progress" to "Coaching Proton" throughout, and Plan IDP new feature development.

## Phase Checklist

- [x] **Phase 82: Cleanup & Rename** - Remove orphaned pages, duplicate CMP paths, add AuditLog card, rename "Proton Progress" to "Coaching Proton" (completed 2026-03-02)
- [x] **Phase 83: Master Data QA** - Verify all Kelola Data hub CRUD and export features work correctly for Admin/HC (completed 2026-03-03)
- [x] **Phase 84: Assessment Flow QA** - End-to-end QA of the full assessment lifecycle from creation to history (completed 2026-03-04)
- [x] **Phase 85: Coaching Proton Flow QA** - End-to-end QA of the full coaching workflow from mapping to export (completed 2026-03-04)
- [~] **Phase 86: Plan IDP Development** — Superseded by Phase 89 (PlanIDP 2-Tab Redesign)
- [ ] **Phase 87: Dashboard & Navigation QA** - Verify all dashboards, login flow, role-based navigation, and audit log page
- [x] **Phase 88: KKJ Matrix Full Rewrite** - Redesign KKJ Matrix to document-based file management with dynamic columns (completed 2026-03-03)
- [x] **Phase 89: PlanIDP 2-Tab Redesign** - Unified Silabus + Coaching Guidance tabs for all roles (completed 2026-03-04)
- [x] **Phase 90: Audit Admin Assessment Pages** - Audit & fix ManageAssessment + AssessmentMonitoring — all 11 flows verified (completed 2026-03-04)
- [x] **Phase 91: Audit CMP Assessment Pages** - Audit & fix Assessment + Records — all 9 flows verified (completed 2026-03-04)

## Phase Details

### Phase 82: Cleanup & Rename
**Goal**: The portal is free of orphaned/duplicate pages and "Coaching Proton" is the consistent terminology everywhere
**Depends on**: Nothing (first phase of v3.0)
**Requirements**: CLN-01, CLN-02, CLN-03, CLN-04, CLN-05, CLN-06
**Success Criteria** (what must be TRUE):
  1. Every page title, nav entry, hub card, breadcrumb, and button that previously said "Proton Progress" now says "Coaching Proton"
  2. Navigating to CMP/CpdpProgress, CMP/CreateTrainingRecord, or CMP/ManageQuestions returns 404 or redirects correctly — no orphaned views remain
  3. The Kelola Data hub shows an AuditLog card visible to Admin and HC only; Worker role does not see it
  4. A decision is documented for Override Silabus and Coaching Guidance tabs (either removed with justification or kept with rationale recorded in PROJECT.md)
**Plans:** 3/3 plans complete

Plans:
- [ ] 82-01-PLAN.md — Rename "Proton Progress" → "Coaching Proton" across CDPController, views, and exports (CLN-01)
- [ ] 82-02-PLAN.md — Remove orphaned CMP actions and views, fix dead hub card and broken ManageQuestions links (CLN-02, CLN-03, CLN-04)
- [ ] 82-03-PLAN.md — Add AuditLog card to Kelola Data hub Section C and document CLN-06 decision (CLN-05, CLN-06)

### Phase 83: Master Data QA
**Goal**: All master data management features in the Kelola Data hub work correctly end-to-end for Admin and HC roles
**Depends on**: Phase 82
**Requirements**: DATA-01, DATA-02, DATA-03, DATA-04, DATA-05, DATA-06, DATA-07
**Success Criteria** (what must be TRUE):
  1. Admin/HC can create, edit, and delete KKJ Matrix rows via the spreadsheet editor and see changes reflected in the CMP/Kkj view
  2. Admin/HC can create, edit, and delete KKJ-IDP Mapping entries, export them to Excel, and see changes reflected in the CMP/Mapping view
  3. Admin/HC can create, edit, and delete Silabus entries; new Silabus items appear as options in Plan IDP and Coaching Proton pages
  4. Admin/HC can upload, replace, and delete Coaching Guidance files; download links function correctly
  5. Admin/HC can create, edit, delete, and view Worker details; Worker import with Excel template succeeds and validation errors are shown; Worker export with active filters produces correct Excel output
**Plans**: 9 plans (4 complete, 5 gap closure)

Plans:
- [x] 83-01: QA KKJ Matrix editor — cross-link to CMP/Kkj added (DATA-01)
- [x] 83-02: QA KKJ-IDP Mapping editor — reference guard added (DATA-02)
- [x] 83-03: QA Silabus CRUD — orphan cleanup bugs fixed (DATA-03)
- [x] 83-04: KKJ Bagian delete guard — active-only guard + two-phase delete (DATA-04)
- [ ] 83-05-PLAN.md — Add IsActive to ApplicationUser + ProtonKompetensi + EF migration (DATA-05, DATA-03)
- [ ] 83-06-PLAN.md — Worker soft delete backend: DeactivateWorker, ReactivateWorker, login block (DATA-05, DATA-07)
- [ ] 83-07-PLAN.md — Silabus soft delete backend: SilabusDeactivate, SilabusReactivate, CDPController filter (DATA-03)
- [ ] 83-08-PLAN.md — Worker soft delete UI + ExportWorkers inactive + ImportWorkers inactive match (DATA-05, DATA-06)
- [ ] 83-09-PLAN.md — Silabus soft delete UI in ProtonData/Index + browser verify all flows (DATA-01..DATA-07)

### Phase 84: Assessment Flow QA
**Goal**: The complete assessment lifecycle works correctly for all applicable roles — from HC creating an assessment to workers taking the exam and seeing results
**Depends on**: Phase 83
**Requirements**: ASSESS-01, ASSESS-02, ASSESS-03, ASSESS-04, ASSESS-05, ASSESS-06, ASSESS-07, ASSESS-08, ASSESS-09, ASSESS-10
**Success Criteria** (what must be TRUE):
  1. HC/Admin can create an assessment with all fields, edit it with schedule-change warnings, delete it with cascade cleanup, and assign workers — all without errors
  2. HC/Admin can create packages, import questions via Excel or paste, and preview; cross-package shuffle distributes questions correctly across workers
  3. Worker can verify token, start exam, see answers auto-saved per click, submit, and resume from the exact page with accurate time remaining after a disconnect
  4. Worker sees results with correct score, pass/fail status, conditional answer review, earned competencies, and certificate link after passing
  5. HC sees live per-participant progress in the monitoring view and can execute all actions (force close, reset, bulk close, regenerate token, reshuffle)
  6. Training Records page shows the worker's correct personal assessment and training history with working filters; Admin 3-tab ManageAssessment view renders all tabs correctly
**Plans**: TBD

Plans:
- [ ] 84-01: QA assessment creation, edit, delete, and worker assignment flows (ASSESS-01, ASSESS-02)
- [ ] 84-02: QA package management and question import (ASSESS-08)
- [ ] 84-03: QA worker exam flow: token verification, start, auto-save, resume, submit (ASSESS-03)
- [ ] 84-04: QA results page, certificate, and earned competencies display (ASSESS-04, ASSESS-05)
- [ ] 84-05: QA HC real-time monitoring and all HC action buttons (ASSESS-06, ASSESS-07)
- [ ] 84-06: QA Training Records page and Admin 3-tab ManageAssessment view (ASSESS-09, ASSESS-10)

### Phase 85: Coaching Proton Flow QA
**Goal**: The complete Coaching Proton workflow works correctly for all applicable roles — from coach-coachee mapping through evidence, approval, and export
**Depends on**: Phase 83
**Requirements**: COACH-01, COACH-02, COACH-03, COACH-04, COACH-05, COACH-06, COACH-07, COACH-08
**Success Criteria** (what must be TRUE):
  1. Admin/HC can assign, edit, deactivate, and reactivate coach-coachee mappings with validation; export to Excel produces correct data
  2. Coachee sees their coaching progress page with deliverable statuses, evidence uploads, and approval states correctly displayed
  3. Coach can select a coachee, upload evidence with a coaching log, and view current approval statuses
  4. SrSpv, SectionHead, and HC can each approve or reject deliverables within their role scope; the correct approval chain is enforced
  5. The deliverable detail page shows complete information — status, evidence file, coaching report, and full approval history
  6. HC/Admin can override a stuck deliverable from the Coaching Proton Override tab; Excel and PDF exports work for authorized roles
**Plans**: 4/4 plans complete

Plans:
- [x] 85-01: QA coach-coachee mapping CRUD and Excel export (COACH-01, COACH-02)
- [x] 85-02: QA coachee progress view and coach evidence upload flow (COACH-03, COACH-04)
- [x] 85-03: QA approval chain and deliverable detail page (COACH-05, COACH-06)
- [x] 85-04: QA Override tab and progress exports (COACH-07, COACH-08)

**Completed:** 2026-03-04

### Phase 86: Plan IDP Development — SUPERSEDED
**Status**: Superseded by Phase 89 (PlanIDP 2-Tab Redesign) which delivered a more comprehensive unified layout with Silabus + Coaching Guidance tabs for all roles.

### Phase 87: Dashboard & Navigation QA
**Goal**: All dashboards show correct role-scoped data, login and navigation work without errors, and authorization boundaries are enforced
**Depends on**: Phase 82
**Requirements**: DASH-01, DASH-02, DASH-03, DASH-04, DASH-05, DASH-06, DASH-07, DASH-08
**Success Criteria** (what must be TRUE):
  1. Home/Index dashboard shows correct stats per role (IDP progress for coachees, assessment summary for HC, training completion for workers)
  2. CDP Dashboard Coaching Proton tab shows accurate progress data across all bagian/unit; Assessment Analytics tab shows correct assessment and training data with working export
  3. Login flow completes without error for local auth; role-based navigation shows Kelola Data only to Admin/HC and hides it from other roles
  4. Section selectors (KkjSectionSelect, MappingSectionSelect) function correctly for Admin/HC; the AccessDenied page renders when an unauthorized user attempts a restricted action
  5. The AuditLog page shows the assessment management audit trail with correct entries and is visible to Admin and HC only
**Plans**: TBD

Plans:
- [ ] 87-01: QA Home/Index dashboard per role and CDP Dashboard both tabs (DASH-01, DASH-02, DASH-03)
- [ ] 87-02: QA login flow and role-based navigation visibility (DASH-04, DASH-05)
- [ ] 87-03: QA section selectors, AccessDenied page, and AuditLog page (DASH-06, DASH-07, DASH-08)

### Phase 89: PlanIDP Silabus and Coaching Guidance Tabs Improvement

**Goal:** Redesign CDP/PlanIdp from its current dual-path layout (Coachee deliverable table + Admin/HC PDF view) into a unified 2-tab layout (Silabus + Coaching Guidance) for all roles — read-only consumer view aligned with the finalized ProtonData/Index admin page
**Requirements**: PLANIDP-01, PLANIDP-02, PLANIDP-03, PLANIDP-04, PLANIDP-05
**Depends on:** Phase 88
**Plans:** 4/4 plans complete

Requirements:
- **PLANIDP-01**: All roles see the same unified 2-tab PlanIdp layout (Silabus + Coaching Guidance); old PDF view and old Coachee deliverable-hierarchy path are removed
- **PLANIDP-02**: Silabus tab shows read-only hierarchical table (Kompetensi > SubKompetensi > Deliverable) with rowspan merge and cascading filter (Bagian > Unit > Track > Muat Data); only IsActive==true items shown
- **PLANIDP-03**: Coaching Guidance tab shows 4-level accordion (Bagian > Unit > TrackType > TahunKe) with Download buttons at the file level; data sourced from CoachingGuidanceFile table
- **PLANIDP-04**: Coachee role auto-pre-fills their Bagian/Unit/Track from ProtonTrackAssignment on page load; "Lihat Semua" resets to manual filter; Coachee without assignment sees informational empty state
- **PLANIDP-05**: CDPController.GuidanceDownload endpoint added (any [Authorize] user); old PDF-based view entirely removed including JS, CSS, and PDF file references

Plans:
- [x] 89-01: Rewrite CDPController.PlanIdp + add CDPController.GuidanceDownload (PLANIDP-01..05)
- [x] 89-02: Rewrite Views/CDP/PlanIdp.cshtml as 2-tab layout (PLANIDP-01..05)
- [x] 89-03: Human verify — browser check all tabs and role behaviors (PLANIDP-01..05)

**Completed:** 2026-03-04

### Phase 90: Audit & fix Admin Assessment pages (ManageAssessment + AssessmentMonitoring)

**Goal:** All Admin assessment management pages work correctly end-to-end — CRUD, monitoring actions, tab navigation, cross-page links, and authorization boundaries are verified and bug-free
**Requirements**: None (audit/fix phase)
**Depends on:** Phase 89
**Plans:** 3/3 plans complete

Plans:
- [x] 90-01-PLAN.md — AdminController assessment actions: IsActive filters, RegenerateToken multi-sibling fix, cascade review
- [x] 90-02-PLAN.md — View-level audit: ManageAssessment header fix, Monitoring cross-link, AssessmentMonitoring detail links, form nav
- [x] 90-03-PLAN.md — Seed test data + browser verification: all 11 flows across ManageAssessment and AssessmentMonitoring

**Completed:** 2026-03-04

### Phase 91: Audit & fix CMP Assessment pages (Assessment + Records)

**Goal:** All CMP assessment pages work correctly end-to-end — exam flow, results, records, certificate, and CSRF protection are verified and bug-free
**Requirements**: None (audit/fix phase)
**Depends on:** Phase 90
**Plans:** 3/3 plans complete

Plans:
- [x] 91-01: Audit CMPController assessment actions — CSRF, HC auth, shuffle fix
- [x] 91-02: View-level audit — Results returnUrl, Records 2-tab redesign, VerifyToken CSRF
- [x] 91-03: Browser verification — all 9 CMP Assessment flows verified PASS

**Completed:** 2026-03-04

---

### ✅ v3.1 CPDP Mapping File-Based Rewrite (Shipped 2026-03-03)

**Milestone Goal:** Replace the Admin/CpdpItems spreadsheet editor and CMP/Mapping data table with a file-based document management system (same pattern as Phase 88 KKJ Matrix rewrite), reusing KkjBagian as the section container and creating a new CpdpFile entity.

## Phase Checklist

- [x] **Phase 88: Data Model & Migration** - Create CpdpFile entity, EF Core migration, export CpdpItem data to Excel backup (completed 2026-03-03)
- [x] **Phase 88: Admin CPDP File Management** - Rewrite Admin/CpdpItems as file upload/download/archive hub with per-section tabs and bagian management (completed 2026-03-03)
- [x] **Phase 88: Worker View & Cleanup** - Rewrite CMP/Mapping as file download page with role-based filtering, then remove CpdpItem table and old CRUD (completed 2026-03-03)

## Phase Details

### Phase 88: Data Model & Migration
**Goal**: The CpdpFile entity exists in the database and all existing CpdpItem data is preserved as an Excel backup before any table changes
**Depends on**: Phase 88 (KkjBagian entity already exists)
**Requirements**: CPDP-06
**Success Criteria** (what must be TRUE):
  1. An Excel file containing all existing CpdpItem rows is saved to disk and readable before migration runs
  2. The CpdpFiles table exists in the database with columns for BagianId, FileName, StoredFileName, Description, UploadedAt, UploadedBy, and IsArchived
  3. EF Core migration applies cleanly with no errors on dotnet ef database update
**Plans**: TBD

Plans:
- [ ] 88-01: Export CpdpItem data to Excel backup via one-time script or controller action (CPDP-06)
- [ ] 88-02: Define CpdpFile model, add DbSet to AppDbContext, create and apply EF Core migration

### Phase 88: Admin CPDP File Management
**Goal**: Admin/HC can manage CPDP document files per section — uploading, downloading, archiving, and viewing file history — with the ability to add or remove section tabs
**Depends on**: Phase 88
**Requirements**: CPDP-01, CPDP-02, CPDP-03
**Success Criteria** (what must be TRUE):
  1. Admin/HC navigates to /Admin/CpdpItems and sees tabbed sections (RFCC, GAST, NGP, DHT) matching KkjBagian records; each tab shows active files for that section
  2. Admin/HC uploads a PDF or Excel file with an optional description; the file appears immediately in the correct section tab after upload
  3. Admin/HC clicks Archive on a file and it disappears from the active list but remains visible in the History view for that section
  4. Admin/HC downloads a file from the active list or history and receives the correct file
  5. Admin/HC adds a new bagian tab or deletes an empty bagian tab; the change reflects on both the admin page and the worker CMP/Mapping page
**Plans**: TBD

Plans:
- [ ] 88-01: AdminController actions — CpdpFiles GET, CpdpUpload GET/POST, CpdpFileDownload GET, CpdpFileArchive POST (CPDP-01, CPDP-02)
- [ ] 88-02: AdminController CpdpFileHistory GET + KkjBagianDelete CPDP guard; Views CpdpFiles.cshtml, CpdpUpload.cshtml, CpdpFileHistory.cshtml; Admin/Index hub card (CPDP-03)

### Phase 88: Worker View & Cleanup
**Goal**: All authenticated workers can download CPDP files per section on the CMP/Mapping page with role-based section filtering, and the legacy CpdpItem table and all spreadsheet CRUD code are permanently removed
**Depends on**: Phase 88
**Requirements**: CPDP-04, CPDP-05, CPDP-07
**Success Criteria** (what must be TRUE):
  1. An L1-L4 worker navigating to /CMP/Mapping sees all section tabs and can download files from any section
  2. An L5-L6 worker navigating to /CMP/Mapping sees only the tab(s) matching their own unit and cannot access other section tabs
  3. The CpdpItem table no longer exists in the database after migration; any direct URL access to old CpdpItem CRUD routes returns 404 or redirects
  4. No references to CpdpItem, the old Mapping spreadsheet editor, or MappingSectionSelect remain in controllers or views
**Plans**: TBD

Plans:
- [ ] 88-01: Rewrite CMPController Mapping action and CMP/Mapping.cshtml view — file download per section, role-based tab filtering (L1-L4 all, L5-L6 own unit) (CPDP-04, CPDP-05)
- [ ] 88-02: Remove CpdpItem model, DbSet, migration drop, and all old CRUD controller actions and views; verify no broken references remain (CPDP-07)

## Progress

**Execution Order:**
82 → 83 → 88 → 89 → 90 → 91 → 84 → 85 → 87

| Phase | Milestone | Plans | Status | Completed |
|-------|-----------|-------|--------|-----------|
| 82. Cleanup & Rename | v3.0 | 3/3 | ✅ Complete | 2026-03-02 |
| 83. Master Data QA | v3.0 | 9/9 | ✅ Complete | 2026-03-03 |
| 84. Assessment Flow QA | v3.0 | 2/2 | ✅ Complete | 2026-03-04 |
| 85. Coaching Proton Flow QA | v3.0 | Complete    | 2026-03-04 | 2026-03-04 |
| 86. Plan IDP Development | v3.0 | — | ↗️ Superseded by 89 | - |
| 87. Dashboard & Navigation QA | v3.0 | 0/3 | ⬜ Not started | - |
| 88. KKJ Matrix Full Rewrite | v3.0 | 4/4 | ✅ Complete | 2026-03-03 |
| 89. PlanIDP 2-Tab Redesign | v3.0 | 3/3 | ✅ Complete | 2026-03-04 |
| 90. Audit Admin Assessment | v3.0 | 3/3 | ✅ Complete | 2026-03-04 |
| 91. Audit CMP Assessment | v3.0 | 3/3 | ✅ Complete | 2026-03-04 |
| 88. CPDP File Rewrite (3 sub) | v3.1 | 6/6 | ✅ Complete | 2026-03-03 |
