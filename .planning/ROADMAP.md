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
- 🚧 **v3.0 Full QA & Feature Completion** — Phases 82-87 (in progress)

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

- [ ] **Phase 82: Cleanup & Rename** - Remove orphaned pages, duplicate CMP paths, add AuditLog card, rename "Proton Progress" to "Coaching Proton"
- [ ] **Phase 83: Master Data QA** - Verify all Kelola Data hub CRUD and export features work correctly for Admin/HC
- [ ] **Phase 84: Assessment Flow QA** - End-to-end QA of the full assessment lifecycle from creation to history
- [ ] **Phase 85: Coaching Proton Flow QA** - End-to-end QA of the full coaching workflow from mapping to export
- [ ] **Phase 86: Plan IDP Development** - Build the new Plan IDP page where coachees view silabus and download guidance docs
- [ ] **Phase 87: Dashboard & Navigation QA** - Verify all dashboards, login flow, role-based navigation, and audit log page

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
**Plans:** 2/3 plans executed

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
**Plans**: TBD

Plans:
- [ ] 83-01: QA KKJ Matrix editor (CRUD, bulk save, bagian management, CMP/Kkj link) (DATA-01)
- [ ] 83-02: QA KKJ-IDP Mapping editor (CRUD, bulk save, export, CMP/Mapping link) (DATA-02)
- [ ] 83-03: QA Silabus CRUD and verify data links to Plan IDP and Coaching Proton (DATA-03)
- [ ] 83-04: QA Coaching Guidance file management (upload, download, replace, delete, Plan IDP links) (DATA-04)
- [ ] 83-05: QA Worker management CRUD, import from Excel template, and export with filters (DATA-05, DATA-06, DATA-07)

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
**Plans**: TBD

Plans:
- [ ] 85-01: QA coach-coachee mapping CRUD and Excel export (COACH-01, COACH-02)
- [ ] 85-02: QA coachee progress view and coach evidence upload flow (COACH-03, COACH-04)
- [ ] 85-03: QA approval chain and deliverable detail page (COACH-05, COACH-06)
- [ ] 85-04: QA Override tab and progress exports (COACH-07, COACH-08)

### Phase 86: Plan IDP Development
**Goal**: Coachees can view the silabus items assigned to their track and download the relevant coaching guidance documents
**Depends on**: Phase 83
**Requirements**: IDP-01, IDP-02, IDP-03
**Success Criteria** (what must be TRUE):
  1. A coachee logging in sees a Plan IDP page that lists the silabus items matching their assigned Operator Tahun, Unit, and Bagian
  2. Each silabus item with a linked coaching guidance file shows a download button; clicking it downloads the correct file
  3. The Plan IDP page supports filtering by Bagian, Unit, and Level so coachees (and HC reviewing) can narrow displayed silabus items
**Plans**: TBD

Plans:
- [ ] 86-01: Build Plan IDP controller action and ViewModel (query silabus by track assignment, support Bagian/Unit/Level filters) (IDP-01, IDP-03)
- [ ] 86-02: Build Plan IDP Razor view (silabus list, filter bar, guidance file download links) and wire Coaching Guidance downloads (IDP-02)

### Phase 87: Dashboard & Navigation QA
**Goal**: All dashboards show correct role-scoped data, login and navigation work without errors, and authorization boundaries are enforced
**Depends on**: Phase 82, Phase 86
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

## Progress

**Execution Order:**
Phases execute in numeric order: 82 → 83 → 84 → 85 → 86 → 87

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 82. Cleanup & Rename | 2/3 | In Progress|  | - |
| 83. Master Data QA | v3.0 | 0/5 | Not started | - |
| 84. Assessment Flow QA | v3.0 | 0/6 | Not started | - |
| 85. Coaching Proton Flow QA | v3.0 | 0/4 | Not started | - |
| 86. Plan IDP Development | v3.0 | 0/2 | Not started | - |
| 87. Dashboard & Navigation QA | v3.0 | 0/3 | Not started | - |
