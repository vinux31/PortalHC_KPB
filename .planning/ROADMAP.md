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
- 🚧 **v1.7 Assessment System Integrity** — Phases 21-26 (in progress)

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

### 🚧 v1.7 Assessment System Integrity (Phases 21-26)

**Milestone Goal:** Close 12 critical gaps in the assessment system — exam lifecycle management, answer review for package exams, token enforcement, audit logging, worker history, and data integrity safeguards.

---

#### Phase 21: Exam State Foundation
**Goal:** The assessment session model tracks real exam state — workers who load an exam are immediately recorded as InProgress with a start timestamp, and the exam window close date can be configured per assessment
**Depends on:** Phase 20 (v1.6 complete)
**Requirements:** LIFE-01
**Success Criteria** (what must be TRUE):
  1. When a worker loads the StartExam page for the first time, their session status changes from `Open` to `InProgress` and a `StartedAt` timestamp is recorded
  2. The `InProgress` status is visible in the monitoring detail view alongside existing `Completed` and `Open` statuses
  3. Reloading the exam page does not reset `StartedAt` — the timestamp is only written once on first load
**Plans:** 1 plan

Plans:
- [x] 21-01-PLAN.md — Schema migration (StartedAt nullable DateTime on AssessmentSession) + idempotent InProgress write in StartExam GET + monitoring detail updated for three-state display

---

#### Phase 22: Exam Lifecycle Actions
**Goal:** Workers can intentionally exit an in-progress exam, HC can force-close or reset sessions for management, and the system enforces both server-side timer limits and configurable exam window close dates
**Depends on:** Phase 21
**Requirements:** LIFE-02, LIFE-03, LIFE-04, LIFE-05, DATA-03
**Success Criteria** (what must be TRUE):
  1. Worker sees a "Keluar Ujian" button on the exam page; clicking it (with confirmation) marks their session `Abandoned` and returns them to the assessment lobby
  2. `SubmitExam` rejects a submission if the elapsed time since `StartedAt` exceeds `DurationMinutes`, returning the worker to the exam page with an expiry message
  3. HC can click a "Reset" action on a `Completed` session in the monitoring detail view; the session reverts to `Open` with score and answers cleared, ready for retake
  4. HC can click a "Force Close" action on any `Open` or `InProgress` session in the monitoring detail view; the session is marked `Completed` with a system score of 0
  5. HC can set an `ExamWindowCloseDate` on an assessment; workers attempting to start the exam after that date see a clear "Ujian sudah ditutup" message instead of the exam
**Plans:** 4 plans

Plans:
- [x] 22-01-PLAN.md — ExamWindowCloseDate migration + Create/Edit form binding + StartExam GET close-date lockout + Abandoned re-entry guard
- [x] 22-02-PLAN.md — AbandonExam POST action + "Keluar Ujian" button with confirmation in StartExam view
- [x] 22-03-PLAN.md — SubmitExam POST elapsed-time check (DurationMinutes + 2 min grace); redirect to StartExam on expiry
- [x] 22-04-PLAN.md — ResetAssessment + ForceCloseAssessment POST actions + 4-state UserStatus projection + MonitoringDetail view buttons

---

#### Phase 23: Package Answer Integrity
**Goal:** Package-based exam answers are persisted to a dedicated table on submission, enabling answer review for package exams to work identically to the legacy path; token-protected exams enforce token entry before any exam content is shown
**Depends on:** Phase 21
**Requirements:** ANSR-01, ANSR-02, SEC-01
**Success Criteria** (what must be TRUE):
  1. When a worker submits a package-based exam, one `PackageUserResponse` row is inserted per question answered, recording the selected `PackageOptionId`
  2. When `AllowAnswerReview` is enabled, a worker who completed a package-based exam can view each question with their selected answer highlighted and correct/incorrect feedback — matching the existing legacy answer review behavior
  3. A worker attempting to start a token-protected exam without entering the correct token sees a token entry prompt and cannot access exam content; a direct URL to the exam without a valid token is blocked and redirected to the prompt
**Plans:** 3 plans

Plans:
- [x] 23-01-PLAN.md — PackageUserResponse entity + EF migration + insert rows in SubmitExam package path + delete in ResetAssessment
- [x] 23-02-PLAN.md — Package answer review in Results action (load PackageUserResponse + build QuestionReviewItem for package path)
- [x] 23-03-PLAN.md — Server-side token enforcement in StartExam GET (TempData flag set by VerifyToken, checked before exam content)

**Completed:** 2026-02-21

---

#### Phase 24: HC Audit Log
**Goal:** HC and Admin can see a full audit trail of assessment management actions — every create, edit, delete, and assign operation is logged with actor, timestamp, and description
**Depends on:** Phase 21
**Requirements:** SEC-02
**Success Criteria** (what must be TRUE):
  1. All HC assessment management actions (create assessment, edit assessment, delete assessment, assign users) write a row to the `AuditLog` table with actor NIP/name, action type, target description, and timestamp
  2. HC and Admin can navigate to an Audit Log page and view the full log sorted by most recent, with actor name, action, and timestamp visible per row
  3. Audit log entries are read-only — no edit or delete controls exist on the audit log view
**Plans:** 2 plans

Plans:
- [x] 24-01-PLAN.md — AuditLog entity, EF migration, AuditLogService DI service, instrument all 7 HC assessment management actions in CMPController
- [x] 24-02-PLAN.md — AuditLog page (HC/Admin only) with paginated table sorted by CreatedAt DESC; nav link in Assessment manage view header

**Completed:** 2026-02-21

---

#### Phase 25: Worker UX Enhancements
**Goal:** Workers can see their completed assessment history from their assessment page and understand which competencies they earned on the results page — closing the feedback loop between assessment and competency development
**Depends on:** Phase 21
**Requirements:** WRK-01, WRK-02
**Success Criteria** (what must be TRUE):
  1. Worker's assessment page shows a "Riwayat Ujian" section listing all their completed assessments with title, category, date, score, and pass/fail status
  2. The riwayat section is visible only to the worker viewing their own page — HC viewing a worker's data is unaffected
  3. After passing an assessment, the results page shows a "Kompetensi Diperoleh" section listing each competency name and the new level the worker has reached
  4. The competency section only appears when competencies were actually updated (IsPassed = true and AssessmentCompetencyMap entries exist for that assessment category)
**Plans:** 2 plans

Plans:
- [ ] 25-01-PLAN.md — Worker assessment history: completed sessions query in Assessment() worker branch; Riwayat Ujian table in Assessment.cshtml
- [ ] 25-02-PLAN.md — Competency display on results: AssessmentCompetencyMap lookup in Results action; "Kompetensi Diperoleh" card on Results.cshtml

---

#### Phase 26: Data Integrity Safeguards
**Goal:** HC is protected from accidental data loss — deleting a package with active assignments or changing an assessment schedule when packages are attached both require explicit confirmation before proceeding
**Depends on:** Phase 22
**Requirements:** DATA-01, DATA-02
**Success Criteria** (what must be TRUE):
  1. When HC clicks "Delete" on a package that has one or more `UserPackageAssignment` records, a confirmation dialog shows the number of affected assignments before the delete proceeds
  2. When HC submits an edit to an assessment's schedule date and packages are attached to that assessment, a confirmation warning is shown before the change is saved
  3. If HC cancels either confirmation, no data is changed — the package and assignment records remain intact
**Plans:** TBD

Plans:
- [ ] 26-01: Delete package warning — DeletePackage action checks UserPackageAssignment count; if > 0, returns confirmation view showing count; second POST with confirmed=true proceeds with delete
- [ ] 26-02: Schedule change warning — EditAssessment POST detects date change + attached packages; returns confirmation view; second POST with confirmed=true saves the change

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
| 25. Worker UX Enhancements | v1.7 | 0/TBD | Not started | - |
| 26. Data Integrity Safeguards | v1.7 | 0/TBD | Not started | - |
