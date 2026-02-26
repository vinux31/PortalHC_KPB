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
- 🚧 **v2.3 Admin Portal** — Phases 47-58 (in progress)

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

- [~] **Phase 47: KKJ Matrix Manager** — MDAT-01 (8/8 plans complete, 1 gap closure plan pending)
- [ ] **Phase 48: CPDP Items Manager** — MDAT-02 (planned)
- [ ] **Phase 49: Assessment Competency Map Manager** — MDAT-03 (planned)
- [ ] **Phase 50: Coach-Coachee Mapping Manager** — OPER-01 (planned)
- [ ] **Phase 51: Proton Track Assignment Manager** — OPER-02 (planned)
- [ ] **Phase 52: DeliverableProgress Override** — OPER-03 (planned)
- [ ] **Phase 53: Final Assessment Manager** — OPER-04 (planned)
- [ ] **Phase 54: Coaching Session Override** — OPER-05 (planned)
- [ ] **Phase 55: Question Bank Edit** — CRUD-01 (planned)
- [ ] **Phase 56: Package Question Edit/Delete** — CRUD-02 (planned)
- [ ] **Phase 57: ProtonTrack Edit/Delete** — CRUD-03 (planned)
- [ ] **Phase 58: Password Reset Standalone** — CRUD-04 (planned)

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
**Plans:** 9/9 plans complete

Plans:
- [x] 47-01-PLAN.md — Admin Portal infrastructure: AdminController, /Admin/Index hub page, /Admin/KkjMatrix read-mode table, Kelola Data nav link
- [x] 47-02-PLAN.md — KKJ Matrix write operations: spreadsheet edit mode (all 20 cols), bulk-save POST, delete-with-guard, clipboard paste
- [x] 47-03-PLAN.md — GAP: Per-bagian tables + editable headers (KkjBagian entity, EF migration, grouped view, KkjBagianSave)
- [x] 47-04-PLAN.md — GAP: Full 21-column read-mode table + per-row insert/delete in edit mode
- [x] 47-05-PLAN.md — GAP: Excel multi-cell selection (drag, Ctrl+C/V, Delete range) + save toast
- [x] 47-06-PLAN.md — GAP CLOSURE: renderEditRows() orphan items fix + btnSave empty-rows guard
- [x] 47-07-PLAN.md — GAP CLOSURE: Read-mode dropdown filter + bagian CRUD toolbar + KkjBagianDelete controller action
- [x] 47-08-PLAN.md — GAP CLOSURE: Fix mousedown INPUT guard (drag selection) + edit-mode single-bagian dropdown filter
- [ ] 47-09-PLAN.md — GAP CLOSURE: Fix Ctrl+V paste dari Excel (navigator.clipboard.readText + selectedCells anchor)

### Phase 48: CPDP Items Manager
**Goal:** Admin can view, create, edit, and delete CPDP Items (CpdpItem) with section filter through a dedicated management page
**Depends on:** Phase 47
**Requirements:** MDAT-02
**Success Criteria** (what must be TRUE):
  1. Admin can navigate to a CPDP Items management page that lists all CpdpItem records, filterable by section
  2. Admin can create, edit, and delete CpdpItem records through the management UI
**Plans:** TBD

### Phase 49: Assessment Competency Map Manager
**Goal:** Admin can view, create, edit, and delete Assessment Competency Maps (AssessmentCompetencyMap) — mapping assessment categories to KKJ items
**Depends on:** Phase 48
**Requirements:** MDAT-03
**Success Criteria** (what must be TRUE):
  1. Admin can view all AssessmentCompetencyMap records and their linked assessment category → KKJ item mappings
  2. Admin can create, edit, and delete mappings through the management UI
**Plans:** TBD

### Phase 50: Coach-Coachee Mapping Manager
**Goal:** Admin can view, create, edit, and delete Coach-Coachee Mappings (CoachCoacheeMapping) — assign and unassign coaches to coachees
**Depends on:** Phase 49
**Requirements:** OPER-01
**Plans:** TBD

### Phase 51: Proton Track Assignment Manager
**Goal:** Admin can view, create, edit, and delete Proton Track Assignments (ProtonTrackAssignment) — assign workers to Proton tracks and manage active/inactive state
**Depends on:** Phase 50
**Requirements:** OPER-02
**Plans:** TBD

### Phase 52: DeliverableProgress Override
**Goal:** Admin can view and override ProtonDeliverableProgress status — correct stuck or erroneous deliverable records
**Depends on:** Phase 51
**Requirements:** OPER-03
**Plans:** TBD

### Phase 53: Final Assessment Manager
**Goal:** Admin can view, approve, reject, and edit ProtonFinalAssessment records — admin-level management of final assessments
**Depends on:** Phase 52
**Requirements:** OPER-04
**Plans:** TBD

### Phase 54: Coaching Session Override
**Goal:** Admin can view all CoachingSession and ActionItem records and perform override edits or deletions
**Depends on:** Phase 53
**Requirements:** OPER-05
**Plans:** TBD

### Phase 55: Question Bank Edit
**Goal:** Admin/HC can edit existing AssessmentQuestion text and options (Edit was missing — only Add/Delete existed)
**Depends on:** Phase 54
**Requirements:** CRUD-01
**Plans:** TBD

### Phase 56: Package Question Edit/Delete
**Goal:** Admin/HC can edit and delete individual PackageQuestion and PackageOption records (currently import-only, no inline edit/delete)
**Depends on:** Phase 55
**Requirements:** CRUD-02
**Plans:** TBD

### Phase 57: ProtonTrack Edit/Delete
**Goal:** Admin can edit and delete ProtonTrack records (Create existed, Edit/Delete were missing)
**Depends on:** Phase 56
**Requirements:** CRUD-03
**Plans:** TBD

### Phase 58: Password Reset Standalone
**Goal:** Admin can reset a worker's password from a standalone action without going through the full EditWorker form
**Depends on:** Phase 57
**Requirements:** CRUD-04
**Plans:** TBD

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
| 47. KKJ Matrix Manager | 9/9 | Complete    | 2026-02-26 | - |
| 48. CPDP Items Manager | v2.3 | 0/? | Not started | - |
| 49. Assessment Competency Map Manager | v2.3 | 0/? | Not started | - |
| 50. Coach-Coachee Mapping Manager | v2.3 | 0/? | Not started | - |
| 51. Proton Track Assignment Manager | v2.3 | 0/? | Not started | - |
| 52. DeliverableProgress Override | v2.3 | 0/? | Not started | - |
| 53. Final Assessment Manager | v2.3 | 0/? | Not started | - |
| 54. Coaching Session Override | v2.3 | 0/? | Not started | - |
| 55. Question Bank Edit | v2.3 | 0/? | Not started | - |
| 56. Package Question Edit/Delete | v2.3 | 0/? | Not started | - |
| 57. ProtonTrack Edit/Delete | v2.3 | 0/? | Not started | - |
| 58. Password Reset Standalone | v2.3 | 0/? | Not started | - |
