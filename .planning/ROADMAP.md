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
- ✅ **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- ✅ **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 (shipped 2026-03-03)
- ✅ **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- 🚧 **v3.3 Basic Notifications** — Phases 99-103 (IN PLANNING)
- 📋 **v3.5 User Guide** — Phases 105-106 (PLANNED)

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

### ✅ v3.0 Full QA & Feature Completion (Shipped 2026-03-05)

**Milestone Goal:** Comprehensive end-to-end QA of all portal flows organized by use-case (not page-by-page), code cleanup to remove orphaned/duplicate paths, UI rename from "Proton Progress" to "Coaching Proton" throughout, and Plan IDP new feature development.

## Phase Checklist

- [x] **Phase 82: Cleanup & Rename** - Remove orphaned pages, duplicate CMP paths, add AuditLog card, rename "Proton Progress" to "Coaching Proton" (completed 2026-03-02)
- [x] **Phase 83: Master Data QA** - Verify all Kelola Data hub CRUD and export features work correctly for Admin/HC (completed 2026-03-03)
- [x] **Phase 84: Assessment Flow QA** - End-to-end QA of the full assessment lifecycle from creation to history (completed 2026-03-04)
- [x] **Phase 85: Coaching Proton Flow QA** - End-to-end QA of the full coaching workflow from mapping to export (completed 2026-03-04)
- [~] **Phase 86: Plan IDP Development** — Superseded by Phase 89 (PlanIDP 2-Tab Redesign)
- [x] **Phase 87: Dashboard & Navigation QA** - Verify all dashboards, login flow, role-based navigation, and audit log page (completed 2026-03-05)
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
- [x] 87-01: QA Home/Index dashboard per role and CDP Dashboard both tabs (DASH-01, DASH-02, DASH-03)
- [x] 87-02: QA login flow and role-based navigation visibility (DASH-04, DASH-05)
- [x] 87-03: QA section selectors, AccessDenied page, and AuditLog page (DASH-06, DASH-07, DASH-08)

### Phase 89: PlanIDP Silabus and Coaching Guidance Tabs Improvement

**Goal:** Redesign CDP/PlanIdp from its current dual-path layout (Coachee deliverable table + Admin/HC PDF view) into a unified 2-tab layout (Silabus + Coaching Guidance) for all roles — read-only consumer view aligned with the finalized ProtonData/Index admin page
**Requirements**: PLANIDP-01, PLANIDP-02, PLANIDP-03, PLANIDP-04, PLANIDP-05
**Depends on:** Phase 88
**Plans:** 4/3 plans complete

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

---

### ✅ v3.2 Bug Hunting & Quality Audit (Shipped 2026-03-05)

**Milestone Goal:** Comprehensive audit of all portal sections — Homepage, CMP, CDP, Admin Portal, Account pages, Authentication/Authorization, and Data Integrity — to identify and fix bugs across UI, navigation, localization, authorization, soft-delete cascades, and audit logging.

## Phase Checklist

- [x] **Phase 92: Homepage Audit** - Audit Homepage for bugs and fix all issues (completed 2026-03-05)
- [x] **Phase 93: CMP Section Audit** - Audit CMP pages for bugs (completed 2026-03-05)
- [x] **Phase 94: CDP Section Audit** - Audit CDP pages for bugs (completed 2026-03-05)
- [x] **Phase 95: Admin Portal Audit** - Audit Kelola Data pages for bugs (completed 2026-03-05)
- [x] **Phase 96: Account Pages Audit** - Audit Account (Profile & Settings) pages for bugs (completed 2026-03-05)
- [x] **Phase 97: Authentication & Authorization Audit** - Audit authentication and authorization for bugs (completed 2026-03-05)
- [x] **Phase 98: Data Integrity Audit** - Audit data integrity patterns for bugs (completed 2026-03-05)

## Phase Details

### Phase 92: Homepage Audit ✅
**Goal**: Audit Homepage for bugs and fix all issues
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: HOME-01, HOME-02, HOME-03, HOME-04, HOME-05
**Status**: ✅ COMPLETE — 5 bugs fixed (deadline links, pluralization, localization, query consistency, negative days)

**Success Criteria** (what must be TRUE):
1. Homepage renders without errors for all authenticated user roles
2. All dashboard cards display accurate data (IDP stats, assessments, training status)
3. Recent activities show correct Indonesian time formatting
4. Deadline cards are clickable and navigate to correct pages
5. All dates use Indonesian locale (day names, month names)

**Completed**: 2026-03-05

---

### Phase 93: CMP Section Audit ✅
**Goal**: Audit CMP (Competency Management Platform) pages for bugs
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: CMP-01, CMP-02, CMP-03, CMP-04, CMP-05, CMP-06
**Status**: ✅ COMPLETE

**Pages to Audit**:
- /CMP/Index (Assessment hub)
- /CMP/Assessment (Assessment list)
- /CMP/Records (Assessment + Training history)
- /CMP/Mapping (KKJ Matrix view)
- /CMP/Monitoring (Assessment monitoring detail)

**Success Criteria** (what must be TRUE):
1. All CMP pages load without errors for Worker, HC, Admin roles
2. Assessment monitoring shows real-time data correctly
3. Records pagination works correctly with filters
4. KKJ Matrix section filtering works per user RoleLevel
5. All forms handle validation errors gracefully (no raw exceptions)
6. CMP navigation flows work end-to-end (Create → Edit → Delete → Monitor)

**Plans**:
- [x] 93-01: Code review — CMPController, Assessment models, view files
- [x] 93-02: Fix localization bugs — Add Indonesian date formatting to all CMP views
- [x] 93-03: Fix validation bugs — Add parameter validation to CMP POST actions
- [x] 93-04: Browser verification — Smoke test all CMP flows (all 5 tasks PASS)

---

### Phase 94: CDP Section Audit ✅
**Goal**: Audit CDP (Competency Development Platform) pages for bugs
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: CDP-01, CDP-02, CDP-03, CDP-04, CDP-05, CDP-06
**Status**: ✅ COMPLETE — 6/6 plans complete

**Pages to Audit**:
- /CDP/Index (Plan IDP)
- /CDP/CoachingProton (Coaching workflow)
- /CDP/Progress (Progress tracking)
- /CDP/Deliverable (Deliverable detail)
- /CDP/PlanIdp (2-tab Silabus + Guidance)

**Success Criteria** (what must be TRUE):
1. All CDP pages load without errors for Worker, Coach, Spv, HC, Admin roles
2. Coaching Proton shows correct coachee lists scoped by role
3. Progress approval workflows work correctly per role (SrSpv, SH, HC)
4. Evidence upload/download works without errors
5. Coaching session submission and approval flows complete end-to-end
6. All CDP forms handle validation errors gracefully

**Plans**:
- [x] 94-01: Code review — CDPController, Proton models, view files
- [x] 94-02: Browser verification — Test all CDP flows with different roles
- [x] 94-03: Fix identified bugs
- [x] 94-04: Regression test — Verify fixes don't break existing functionality
- [x] 94-05: Navigation fixes — Fix broken navbar links to CDP pages
- [x] 94-06: Edge case fixes — Fix auth issues and navigation gaps

---

### Phase 95: Admin Portal Audit ✅
**Goal**: Audit Kelola Data (Admin Portal) pages for bugs
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: ADMIN-01, ADMIN-02, ADMIN-03, ADMIN-04, ADMIN-05, ADMIN-06, ADMIN-07, ADMIN-08
**Status**: ✅ COMPLETE — 4/4 plans complete

**Pages to Audit**:
- /Admin/Index (Kelola Data hub)
- /Admin/ManageWorkers
- /Admin/KkjMatrix (Manage Silabus)
- /Admin/ManageAssessment
- /Admin/AssessmentMonitoring
- /Admin/CoachCoacheeMapping
- /Admin/ProtonData (Silabus + Guidance tabs)

**Success Criteria** (what must be TRUE):
1. All Admin pages load without errors for Admin and HC roles
2. ManageWorkers filters and pagination work correctly
3. KkjMatrix file operations (upload, download, archive) work correctly
4. Assessment monitoring displays real-time participant data
5. Coach-Coachee mapping operations complete successfully
6. ProtonData tabs display correctly with file management
7. All forms handle validation errors gracefully
8. Role gates work correctly (HC-only vs Admin-only actions)

**Plans**:
- [x] 95-01: Code review — AdminController, Admin models, view files
- [x] 95-02: Browser verification — Test all Admin flows with HC and Admin roles
- [x] 95-03: Fix identified bugs
- [x] 95-04: Regression test — Verify fixes don't break existing functionality

---

### Phase 96: Account Pages Audit ✅
**Goal**: Audit Account (Profile & Settings) pages for bugs
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: ACCT-01, ACCT-02, ACCT-03, ACCT-04
**Status**: ✅ COMPLETE — 3/3 plans complete (+ 1 SUMMARY)

**Pages to Audit**:
- /Account/Profile
- /Account/Settings

**Success Criteria** (what must be TRUE):
1. Profile page displays correct user data (Nama, NIP, Email, Position, Unit)
2. Settings page change password works correctly
3. Profile edit (FullName, Position) saves and persists correctly
4. Avatar initials display correctly from FullName

**Plans**:
- [x] 96-01: Code review — AccountController, ApplicationUser model, view files
- [x] 96-02: Browser verification — Test profile and settings flows
- [x] 96-03: Fix identified bugs
- [x] 96-04: Regression test — Verify fixes don't break existing functionality

### Phase 97: Authentication & Authorization Audit ✅
**Goal**: Audit authentication and authorization for bugs
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05
**Status**: ✅ Complete

**Areas to Audit**:
- Login flow (local and AD modes)
- AccessDenied page
- Role-based navigation visibility
- Return URL redirects

**Success Criteria** (what must be TRUE):
1. Login flow works correctly in both local and AD authentication modes ✅
2. Inactive users are blocked from login (Phase 83 soft-delete) ✅
3. AccessDenied page displays for unauthorized access attempts ✅
4. Role-based navigation visibility works correctly for all 6 roles ✅
5. Return URL redirect after login works correctly and securely (Url.IsLocalUrl check) ✅

**Plans**: 4/4 complete
- [x] 97-01: Authorization Matrix Audit - Exhaustive grep audit of all controllers and views (AUTH-01, AUTH-02)
- [x] 97-02: Browser Verification - Spot-check critical auth flows (AUTH-03, AUTH-04, AUTH-05)
- [x] 97-03: Edge Case Testing and Bug Fixes - Analyze security and functional bugs via code review (AUTH-01 through AUTH-05)
- [x] 97-04: Regression Testing and Phase Summary - Verify fixes, create summary (AUTH-01 through AUTH-05)

**Completed:** 2026-03-05

### Phase 98: Data Integrity Audit ✅
**Goal**: Audit data integrity patterns for bugs
**Milestone**: v3.2 Bug Hunting & Quality Audit
**Requirements**: DATA-01, DATA-02, DATA-03
**Status**: ✅ COMPLETE — 4/4 plans complete, 7 bugs fixed (3 orphan leaks + 4 AuditLog gaps)
**Completed**: 2026-03-05

**Areas to Audit**:
- IsActive filter consistency across all queries
- Soft-delete cascade operations
- Audit logging completeness

**Success Criteria** (what must be TRUE):
1. All IsActive filters are applied consistently (Workers, Silabus, Assessments)
2. Soft-delete operations cascade correctly without orphaned records
3. Audit logging captures all HC/Admin actions with correct actor and timestamp

**Plans**:
- [x] 98-01: Code review — Search for all IsActive usages, soft-delete patterns, AuditLog calls
- [x] 98-02: Database verification — Check for orphaned records and missing filters
- [x] 98-03: Fix identified bugs
- [x] 98-04: Regression test — Verify fixes don't break existing functionality

---

### 🚧 v3.3 Basic Notifications (In Planning)

**Milestone Goal:** Build basic in-app notification system for Assessment and Coaching Proton workflows — assignment notifications, deadline reminders, and approval chain notifications.

**Target notification types:**

**Assessment (2 triggers in v3.3):**
- Worker receives: Assessment assigned, assessment results

**Coaching Proton (6 triggers in v3.3):**
- Coachee receives: Coach assignment, coaching completed
- Coach receives: Evidence rejected notification
- SrSpv receives: Evidence uploaded by coach (for review)
- SectionHead receives: Evidence approved by SrSpv
- HC receives: Evidence approved by SectionHead

**Scope:**
- In-App notification center (bell icon, notification list)
- Read/unread status tracking
- No real-time (SignalR) — refresh-based only
- No notification preferences in v3.3

**Approach:** Database model → Notification service → UI components → Trigger points → Testing

## Phase Checklist

- [x] **Phase 99: Notification Database & Service** - Create Notification + UserNotification tables, NotificationService following AuditLogService pattern, DI registration (0/3 plans) (completed 2026-03-05)
- [ ] **Phase 100: Notification Center UI** - Bell icon with unread badge, dropdown list, mark read functionality, deep linking (0/4 plans)
- [ ] **Phase 101: Assessment Notification Triggers** - Worker receives assessment assigned + results notifications (0/2 plans)
- [ ] **Phase 102: Coaching Notification Triggers** - Full approval chain notifications (6 triggers across 4 roles) (0/3 plans)
- [ ] **Phase 103: Notification Testing & Polish** - Integration tests, manual QA, performance testing, edge cases (0/3 plans)

## Phase Details

### Phase 99: Notification Database & Service
**Goal**: System has persistent notification storage with service layer following AuditLogService pattern
**Depends on**: Nothing (first phase of v3.3)
**Requirements**: INFRA-01, INFRA-02, INFRA-07, INFRA-08, INFRA-09
**Success Criteria** (what must be TRUE):
  1. Notification and UserNotification tables exist in database with proper indexes (UserId, IsRead, CreatedAt DESC)
  2. NotificationService is registered as scoped dependency in Program.cs and can be injected into controllers
  3. NotificationService.SendAsync() creates notifications with audit trail (CreatedBy, CreatedAt, ReadAt, DeliveryStatus)
  4. NotificationService uses try-catch wrapping so failures never crash main workflows
  5. Notification templates provide consistent messaging across all notification types
**Plans**: TBD

Plans:
- [ ] 99-01-PLAN.md — Create Notification + UserNotification models with EF Core migration (INFRA-01, INFRA-07)
- [ ] 99-02-PLAN.md — Build NotificationService with full CRUD operations following AuditLogService pattern (INFRA-02, INFRA-09)
- [ ] 99-03-PLAN.md — Register INotificationService in DI, create notification templates, add unit tests (INFRA-08)

### Phase 100: Notification Center UI
**Goal**: Users can access notifications via bell icon in navbar with unread badge, view list, mark as read, and navigate to related content
**Depends on**: Phase 99
**Requirements**: INFRA-03, INFRA-04, INFRA-05, INFRA-06, INFRA-10
**Success Criteria** (what must be TRUE):
  1. Bell icon appears in navbar with red badge showing unread count for authenticated users
  2. Clicking bell icon opens dropdown showing most recent 20 notifications (most recent first)
  3. Clicking a notification marks it as read and navigates to the relevant page (deep linking)
  4. "Mark all as read" button clears all unread notifications for the current user
  5. AJAX polling updates unread count every 30 seconds without full page refresh
**Plans**: TBD

Plans:
- [ ] 100-01-PLAN.md — Add bell icon to navbar with unread badge (INFRA-03)
- [ ] 100-02-PLAN.md — Build NotificationController with GetNotifications and MarkAsRead JSON endpoints (INFRA-04)
- [ ] 100-03-PLAN.md — Create notification dropdown view with pagination and mark individual/bulk read actions (INFRA-05, INFRA-06)
- [ ] 100-04-PLAN.md — Implement AJAX polling for unread count and deep linking to Assessment/Coaching pages (INFRA-10)

### Phase 101: Assessment Notification Triggers
**Goal**: Workers receive notifications when assessments are assigned and when results are ready
**Depends on**: Phase 100
**Requirements**: ASMT-01, ASMT-02
**Success Criteria** (what must be TRUE):
  1. Worker receives notification when HC assigns them to an assessment (title includes assessment name, deadline)
  2. Worker receives notification when assessment results are ready (includes score, pass/fail status)
  3. Notifications link to /CMP/Assessment (assignment) or /CMP/Results (results)
  4. Bulk assignment to 100 workers completes in under 10 seconds (no blocking loop)
**Plans**: TBD

Plans:
- [ ] 101-01-PLAN.md — Add assessment assignment notification trigger in AdminController.AssignWorkers (ASMT-01)
- [ ] 101-02-PLAN.md — Add assessment results ready notification trigger in CMPController.SubmitExam/GradeExam (ASMT-02)

### Phase 102: Coaching Notification Triggers
**Goal**: All roles in coaching approval chain receive notifications at appropriate workflow stages
**Depends on**: Phase 101
**Requirements**: COACH-01, COACH-02, COACH-03, COACH-04, COACH-05, COACH-06
**Success Criteria** (what must be TRUE):
  1. Coachee receives notification when coach is assigned (includes coach name)
  2. SrSpv receives notification when coach uploads evidence for review (includes deliverable name)
  3. Coach receives notification when evidence is rejected (includes rejection reason)
  4. SectionHead receives notification when evidence is approved by SrSpv (includes deliverable name)
  5. HC receives notification when evidence is approved by SectionHead (includes deliverable name)
  6. Coachee receives notification when coaching session is completed (includes session summary)
**Plans**: TBD

Plans:
- [ ] 102-01-PLAN.md — Add coach assignment notification trigger in AdminController.AssignCoach (COACH-01)
- [ ] 102-02-PLAN.md — Add evidence upload/reject/approval notification triggers in CDPController (COACH-02, COACH-03, COACH-04, COACH-05)
- [ ] 102-03-PLAN.md — Add coaching completed notification trigger in CDPController.SubmitCoachingSession (COACH-06)

### Phase 103: Notification Testing & Polish
**Goal**: All notification triggers work correctly end-to-end with proper performance and edge case handling
**Depends on**: Phase 102
**Requirements**: All v3.3 requirements (validation phase)
**Success Criteria** (what must be TRUE):
  1. All 8 notification triggers fire correctly at appropriate workflow stages
  2. Unread count badge updates within 30 seconds of notification creation
  3. Deep links navigate to correct pages with proper context (assessment ID, deliverable ID)
  4. Bulk operations (100+ notifications) complete in under 10 seconds
  5. Edge cases handled gracefully (deleted entities, no permissions, duplicate notifications)
**Plans**: TBD

Plans:
- [ ] 103-01-PLAN.md — Create integration tests for all 8 notification triggers
- [ ] 103-02-PLAN.md — Manual QA checklist for each trigger with browser verification
- [ ] 103-03-PLAN.md — Performance testing with 100+ notifications and edge case validation


### 🚧 v3.5 User Guide (Planned)

**Milestone Goal:** Build interactive user guide page with step-by-step instructions for all portal modules, role-specific content, and FAQ section.

#### Phase 105: User Guide Structure & Content — IMPROVEMENTS & GAP COMPLETION
**Goal**: Complete and improve existing User Guide infrastructure with missing content, bug fixes, and UX enhancements
**Depends on**: Phase 104
**Status**: Infrastructure exists (HomeController.Guide, Guide.cshtml, GuideDetail.cshtml, guide.css, navbar link) — Phase 105 adds improvements and gap completion
**Requirements**: GUIDE-NAV-01, GUIDE-NAV-02, GUIDE-NAV-03, GUIDE-NAV-04, GUIDE-NAV-05, GUIDE-CONTENT-01, GUIDE-CONTENT-02, GUIDE-CONTENT-03, GUIDE-CONTENT-04, GUIDE-CONTENT-05, GUIDE-CONTENT-06, GUIDE-ACCESS-01, GUIDE-ACCESS-02, GUIDE-ACCESS-03, GUIDE-ACCESS-04, GUIDE-ACCESS-05, GUIDE-ACCESS-06, GUIDE-ACCESS-07
**Success Criteria** (what must be TRUE):
  1. User can access Guide page via "Panduan" link in navbar after login ✅ (already implemented)
  2. Guide page displays hero section with gradient styling and 4 tab navigation buttons ✅ (Dashboard removed per user request)
  3. User can click tabs to switch between CMP, CDP, Account, Admin Panel content sections without page refresh ✅ (already implemented)
  4. Each tab displays step-by-step instructions with numbered step cards including icon, title, and description ✅ (already implemented, gaps to fill)
  5. Important information displays in alert boxes (tips/catatan) and FAQ section displays at bottom with accordion behavior ✅ (already implemented)
  6. Admin Panel tab is visible only to Admin/HC users (hidden for other roles) ✅ (already implemented)
  7. Non-logged users accessing Guide page are redirected to login page ✅ (already implemented)
**Plans**: 5 improvement plans (fixes, content gaps, UX polish)

Plans:
- [x] 105-01-PLAN.md — Fix CSS bugs (missing btn-cdp, btn-account, btn-data, btn-admin classes)
- [x] 105-02-PLAN.md — Add missing Account module guide (Logout & Role Information)
- [x] 105-03-PLAN.md — Add missing CDP module guide (Deliverable List Overview)
- [x] 105-04-PLAN.md — Split existing Admin guides into focused, detailed guides (3 tasks)
- [x] 105-04B-PLAN.md — Add new Admin module guides (Manage Units, Positions, Notifications, System Settings)
- [x] 105-05-PLAN.md — Add minor UX features (search highlighting, breadcrumb, print CSS)

#### Phase 106: User Guide Styling & Polish
**Goal**: User Guide displays with premium visual design matching existing portal design system
**Depends on**: Phase 105
**Requirements**: GUIDE-STYLE-01, GUIDE-STYLE-02, GUIDE-STYLE-03, GUIDE-STYLE-04, GUIDE-STYLE-05, GUIDE-STYLE-06, GUIDE-STYLE-07, GUIDE-STYLE-08
**Success Criteria** (what must be TRUE):
  1. Guide page uses CSS variables matching home.css (--gradient-primary, --shadow-*) for visual consistency
  2. Page uses Inter font family throughout all text elements
  3. Cards display with glassmorphism effect matching existing design system
  4. Page content animates smoothly using AOS library on scroll
  5. Step numbers display with gradient badges matching portal styling
  6. Tab navigation styling matches existing design patterns (hover states, active states)
  7. Page displays correctly on mobile devices with responsive breakpoints and adjusted layouts
  8. All content is in Indonesian language
**Plans**: TBD

Plans:
- [ ] 106-01-PLAN.md — Apply CSS styling with home.css variables and glassmorphism effects
- [ ] 106-02-PLAN.md — Implement AOS scroll animations and gradient badges
- [ ] 106-03-PLAN.md — Add responsive design for mobile devices
- [ ] 106-04-PLAN.md — Final polish and Indonesian language verification

## Progress

| Phase | Plans | Status | Completed |
|-------|-------|--------|-----------|
| 99. Notification Database & Service | 6/3 | Complete    | 2026-03-05 |
| 100. Notification Center UI | 0/4 | Not started | - |
| 101. Assessment Notification Triggers | 0/2 | Not started | - |
| 102. Coaching Notification Triggers | 0/3 | Not started | - |
| 103. Notification Testing & Polish | 0/3 | Not started | - |
| 104. CMP Records Team View | 4/4 | Complete | 2026-03-06 |
| 105. User Guide Structure & Content | 7/6 | Complete    | 2026-03-06 |
| 106. User Guide Styling & Polish | 0/4 | Not started | - |

### Phase 104: CMP Records Team View
**Goal:** Close UAT gaps - fix duplicate UI elements, broken filters, and missing back button
**Requirements**: N/A (gap closure from UAT)
**Depends on:** Phase 103
**Plans:** 7/6 plans complete

## Historical Progress

**Execution Order:**
82 → 83 → 88 → 89 → 90 → 91 → 84 → 85 → 87

| Phase | Milestone | Plans | Status | Completed |
|-------|-----------|-------|--------|-----------|
| 82. Cleanup & Rename | v3.0 | 3/3 | ✅ Complete | 2026-03-02 |
| 83. Master Data QA | v3.0 | 9/9 | ✅ Complete | 2026-03-03 |
| 84. Assessment Flow QA | v3.0 | 2/2 | ✅ Complete | 2026-03-04 |
| 85. Coaching Proton Flow QA | v3.0 | 4/4 | ✅ Complete | 2026-03-04 |
| 86. Plan IDP Development | v3.0 | — | ↗️ Superseded by 89 | - |
| 87. Dashboard & Navigation QA | v3.0 | 3/3 | ✅ Complete | 2026-03-05 |
| 88. KKJ Matrix Full Rewrite | v3.0 | 4/4 | ✅ Complete | 2026-03-03 |
| 88. CPDP File Rewrite (3 sub) | v3.1 | 6/6 | ✅ Complete | 2026-03-03 |
| 89. PlanIDP 2-Tab Redesign | v3.0 | 3/3 | ✅ Complete | 2026-03-04 |
| 90. Audit Admin Assessment | v3.0 | 3/3 | ✅ Complete | 2026-03-04 |
| 91. Audit CMP Assessment | v3.0 | 3/3 | ✅ Complete | 2026-03-04 |
| 92. Homepage Audit | v3.2 | — | ✅ Complete | 2026-03-05 |
| 93. CMP Section Audit | v3.2 | 4/4 | ✅ Complete | 2026-03-05 |
| 94. CDP Section Audit | v3.2 | 6/6 | ✅ Complete | 2026-03-05 |
| 95. Admin Portal Audit | v3.2 | 4/4 | ✅ Complete | 2026-03-05 |
| 96. Account Pages Audit | v3.2 | 4/3 | ✅ Complete | 2026-03-05 |
| 97. Auth & Authorization Audit | v3.2 | 4/4 | ✅ Complete | 2026-03-05 |
| 98. Data Integrity Audit | v3.2 | 4/4 | ✅ Complete | 2026-03-05 |
