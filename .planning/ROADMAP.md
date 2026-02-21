# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 CMP Assessment Completion** — Phases 1-3 (shipped 2026-02-17)
- ✅ **v1.1 CDP Coaching Management** — Phases 4-7 (shipped 2026-02-18)
- ✅ **Post-v1.1 Fix: Admin Role Switcher** — Phase 8 (shipped 2026-02-18)
- ✅ **v1.2 UX Consolidation** — Phases 9-12 (shipped 2026-02-19)
- ✅ **v1.3 Assessment Management UX** — Phases 13-15 (shipped 2026-02-19)
- ✅ **v1.4 Assessment Monitoring** — Phase 16 (shipped 2026-02-19)
- ✅ **v1.5 Question and Exam UX** — Phase 17 (shipped 2026-02-19)
- ✅ **v1.6 Training Records Management** — Phases 18-20 (shipped 2026-02-20)
- ✅ **v1.7 Assessment System Integrity** — Phases 21-26 (shipped 2026-02-21)
- 🚧 **v1.8 Assessment Polish** — Phases 27-31 (in progress)

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

### 🚧 v1.8 Assessment Polish (Phases 27-31)

**Milestone Goal:** Close known gaps in assessment monitoring accuracy, package management flexibility, scheduling automation, import quality, and HC reporting — leaving the assessment system complete for routine operational use.

---

#### Phase 27: Monitoring Status Fix
**Goal:** HC sees accurate session status in the monitoring card summary — Abandoned and InProgress sessions are never shown as "Not started"
**Depends on:** Phase 26 (v1.7 complete)
**Requirements:** MON-01
**Success Criteria** (what must be TRUE):
  1. HC opens the monitoring tab for an assessment — a worker who has started but not submitted shows "In Progress", not "Not started"
  2. HC opens the monitoring tab for an assessment — a worker who abandoned their session shows "Abandoned", not "Not started"
  3. A worker who has not started still shows "Not started"
  4. A worker who submitted shows "Completed"
**Plans:** 1 plan

Plans:
- [x] 27-01-PLAN.md — Update GetMonitorData UserStatus projection from 2-state to 4-state matching AssessmentMonitoringDetail logic

---

#### Phase 28: Package Reshuffle (Re-assign dropped)
**Goal:** HC can reshuffle a worker's package assignment (single or bulk) from the AssessmentMonitoringDetail page to re-randomize packages as a recovery action
**Depends on:** Phase 27
**Requirements:** PKG-02 (reshuffle only — PKG-01 re-assign dropped per user decision)
**Success Criteria** (what must be TRUE):
  1. HC clicks a "Reshuffle" button for a Pending worker — the system assigns a new random package, different from the current one if possible
  2. HC clicks "Reshuffle All" — all Pending workers get reshuffled, with a result modal showing each worker's outcome
  3. Reshuffle is only available for Pending (Not started) workers — InProgress/Completed/Abandoned are ineligible (buttons disabled)
  4. Reshuffle controls only appear for package-mode assessments — question-mode assessments show no reshuffle UI
  5. Per-worker reshuffle updates in-place with toast; Reshuffle All shows result modal
**Plans:** 2 plans

Plans:
- [x] 28-01-PLAN.md — Backend: ReshufflePackage + ReshuffleAll controller actions, extend view models with IsPackageMode/PackageName
- [x] 28-02-PLAN.md — Frontend: Per-worker reshuffle button, Reshuffle All button, AJAX calls, toast, confirmation dialog, result modal

---

#### Phase 29: Auto-transition Upcoming to Open
**Goal:** Assessment sessions with status Upcoming automatically become Open when their scheduled date arrives, so HC does not need to manually open each assessment
**Depends on:** Phase 26 (v1.7 complete)
**Requirements:** SCHED-01
**Success Criteria** (what must be TRUE):
  1. An assessment with status Upcoming and a scheduled date of today or earlier is shown as Open to workers and HC without any manual action
  2. An assessment with a future scheduled date remains Upcoming and workers cannot start it
  3. The transition happens on the next page load or AJAX call after the scheduled date passes — no stale Upcoming state is served
**Plans:** 3 plans

Plans:
- [x] 29-01-PLAN.md — Apply Upcoming→Open auto-transition at three CMPController.cs read sites: GetMonitorData (display-only re-projection), worker assessment list (in-memory foreach), StartExam (persist before status checks)
- [ ] 29-02-PLAN.md — Upgrade three auto-transition comparisons from date-only to time-based WIB (Schedule <= UtcNow.AddHours(7)); add StartExam time gate blocking future-scheduled access
- [ ] 29-03-PLAN.md — Add Schedule time picker to CreateAssessment and EditAssessment forms; display "Opens DD MMM YYYY, HH:mm WIB" for Upcoming assessments in worker list

---

#### Phase 30: Import Deduplication
**Goal:** Importing questions (Excel or paste) into a package skips any row whose question text already exists in that package, preventing duplicate questions
**Depends on:** Phase 26 (v1.7 complete)
**Requirements:** IMP-01
**Success Criteria** (what must be TRUE):
  1. HC imports an Excel file containing 10 questions where 3 already exist in the package — exactly 7 questions are added and the import result indicates 3 were skipped
  2. HC pastes question text containing a duplicate — the duplicate row is silently skipped and only new questions are saved
  3. A package with zero prior questions accepts all imported rows with no skips
**Plans:** 1 plan

Plans:
- [ ] 30-01-PLAN.md — Add duplicate-text guard in ImportPackageQuestions action: load existing question texts for the package, skip rows where QuestionText matches (case-insensitive), report skip count in result message

---

#### Phase 31: HC Reporting Actions
**Goal:** HC can download a full Excel results report for an assessment and bulk-close all open sessions from the monitoring detail page
**Depends on:** Phase 27
**Requirements:** RPT-01, RPT-02
**Success Criteria** (what must be TRUE):
  1. HC clicks "Export Results" on the monitoring detail page — an Excel file downloads with one row per worker showing name, package, score, pass/fail, and completion time
  2. The exported file includes all workers assigned to the assessment, including those who have not completed
  3. HC clicks "Force Close All" on the monitoring view — all sessions with status Open or InProgress are transitioned to Abandoned, and the page reflects the updated statuses immediately
  4. Force Close All is a single click with a confirmation prompt — no per-session action required
**Plans:** 2 plans

Plans:
- [ ] 31-01-PLAN.md — Excel results export: ExportAssessmentResults action, ClosedXML workbook with worker result rows, download link on monitoring detail page
- [ ] 31-02-PLAN.md — Bulk force-close: ForceCloseAll controller action (POST), update all Open/InProgress sessions to Abandoned, audit log entry, UI button with confirm prompt on monitoring view

### Phase 32: Fix legacy Question path in StartExam — sibling session lookup so HC-created questions work for all workers

**Goal:** Legacy question path in StartExam uses sibling session lookup so HC-created questions appear for all workers in the assessment batch, not just the representative session owner
**Depends on:** None (independent bug fix)
**Plans:** 1 plan

Plans:
- [x] 32-01-PLAN.md — Fix legacy path query to search sibling sessions for questions instead of only the worker's own session

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
| 28. Package Reshuffle (Re-assign dropped) | v1.8 | 2/2 | Complete | 2026-02-21 |
| 29. Auto-transition Upcoming to Open | v1.8 | 1/1 | Complete | 2026-02-21 |
| 30. Import Deduplication | v1.8 | 0/1 | Pending | — |
| 31. HC Reporting Actions | v1.8 | 0/2 | Pending | — |
| 32. Fix legacy Question path | v1.8 | 1/1 | Complete | 2026-02-21 |
