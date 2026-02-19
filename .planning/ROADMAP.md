# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 CMP Assessment Completion** — Phases 1-3 (shipped 2026-02-17)
- ✅ **v1.1 CDP Coaching Management** — Phases 4-7 (shipped 2026-02-18)
- ✅ **Post-v1.1 Fix: Admin Role Switcher** — Phase 8 (shipped 2026-02-18)
- ✅ **v1.2 UX Consolidation** — Phases 9-12 (shipped 2026-02-19)
- 🚧 **v1.3 Assessment Management UX** — Phases 13-15 (in progress)

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
- [x] 06-02-PLAN.md — Approve/Reject actions with sequential unlock and Deliverable UI
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

---

### 🚧 v1.3 Assessment Management UX (In Progress)

**Milestone Goal:** Clean up HC/Admin assessment navigation on CMP Index, restore the dedicated create assessment flow, and add bulk assign (user picker on Edit Assessment page) + quick-edit capabilities from the manage view.

#### Phase 13: Navigation & Creation Flow
**Goal:** HC and Admin see a clean CMP Index with a dedicated Manage Assessments card and no embedded form, and the create assessment flow routes correctly through the dedicated page with proper post-redirect
**Depends on:** Phase 12
**Requirements:** NAV-01, NAV-02, NAV-03, CRT-01, CRT-02
**Success Criteria** (what must be TRUE):
  1. HC/Admin visiting CMP Index sees a "Manage Assessments" card that links to `/CMP/Assessment?view=manage`; workers do not see this card
  2. CMP Index no longer contains an embedded Create Assessment form — the page is clean for all roles
  3. The "Create Assessment" button on the manage view links to `/CMP/CreateAssessment` (the dedicated page)
  4. After HC submits a new assessment, they are redirected to the manage view (`/CMP/Assessment?view=manage`), not CMP Index
**Plans:** 1 plan

Plans:
- [x] 13-01-PLAN.md — Remove embedded form from Index, add Manage Assessments card, revert Index() controller, fix CreateAssessment redirect

#### Phase 14: Bulk Assign
**Goal:** HC can see who is already assigned to an assessment and add more users directly from the existing Edit Assessment page, with new AssessmentSessions created on save
**Depends on:** Phase 13
**Requirements:** BLK-01, BLK-02
**Success Criteria** (what must be TRUE):
  1. The Edit Assessment page (`/CMP/EditAssessment`) includes a section showing currently assigned users
  2. HC can select additional users from a picker on the same Edit Assessment page and save
  3. Saving the user additions creates new AssessmentSessions for the newly selected users without removing or altering existing assignees
**Plans:** 1 plan

Plans:
- [x] 14-01-PLAN.md — Extend EditAssessment view and controller: add assigned user list, multi-select picker, bulk AssessmentSession creation on save

#### Phase 15: Quick Edit
**Goal:** HC can change an assessment's status and reschedule it directly from the manage view card without navigating to the full Edit page
**Depends on:** Phase 14
**Requirements:** QED-01, QED-02
**Success Criteria** (what must be TRUE):
  1. HC can change an assessment's status (e.g., Open, Upcoming, Closed) directly from the manage view card
  2. HC can change an assessment's scheduled date/time directly from the manage view card
  3. Both edits take effect immediately without visiting the full Edit page
**Plans:** TBD

Plans:
- [ ] 15-01: QuickEdit action on CMPController + inline modal on manage view for status + schedule fields

---

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
| 15. Quick Edit | v1.3 | 0/1 | Not started | - |
