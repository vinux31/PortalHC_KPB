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
- 🚧 **v2.7 Assessment Monitoring** — Phases 79-81 (in progress)

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

---

### 🚧 v2.7 Assessment Monitoring (Phases 79-81)

**Milestone Goal:** Extract assessment monitoring from the ManageAssessment dropdown into a dedicated, first-class page in Kelola Data hub — giving HC/Admin a purpose-built workspace for live assessment oversight with all monitoring actions available, while cleaning up the hub by removing redundant cards.

#### Phase 79: Assessment Monitoring Page — Group List
**Goal:** HC/Admin can reach a dedicated Assessment Monitoring page from the Kelola Data hub and see all assessment groups with real-time stats and search/filter controls
**Depends on:** Nothing (first phase of milestone)
**Requirements:** MON-01, MON-02, MON-05
**Success Criteria** (what must be TRUE):
  1. An HC or Admin user sees an "Assessment Monitoring" card in Kelola Data hub Section C (Assessment & Training) and clicking it navigates to the new page
  2. The monitoring page lists all assessment groups with real-time stats: participant count, completed count, passed count, and assessment status badge
  3. The user can type in a search box or use a filter to narrow the group list by assessment name, status, or date — the list updates without a full page reload or with a filtered server response
  4. A "Regenerate Token" action is available on the group list page (per group) and functions correctly
**Plans:** 1/1 plans complete

Plans:
- [ ] 79-01-PLAN.md — Extend ViewModel, add AssessmentMonitoring controller action with filters, add hub card, build group list view

---

#### Phase 80: Per-Participant Monitoring Detail & HC Actions
**Goal:** HC/Admin can drill into any assessment group to see per-participant live progress, and can perform all monitoring actions (Reset, Force Close, Bulk Close, Close Early, Regenerate Token) from within the dedicated monitoring page
**Depends on:** Phase 79
**Requirements:** MON-03, MON-04
**Success Criteria** (what must be TRUE):
  1. Clicking a group on the monitoring page navigates to a detail view showing each participant's real-time progress (answers completed / total), status badge, current score, and countdown timer
  2. HC/Admin can Reset an individual participant session from the detail page — the participant's exam state is cleared and they can restart
  3. HC/Admin can Force Close an individual session from the detail page — the participant's exam is immediately terminated and scored
  4. HC/Admin can Bulk Close all active sessions for a group from the detail page — all in-progress sessions are closed in one action
  5. HC/Admin can Close Early for a participant from the detail page — the session closes and results are computed from answers submitted so far
**Plans:** 1/1 plans complete

---

#### Phase 81: Cleanup — Remove Old Entry Points
**Goal:** The monitoring dropdown action is removed from ManageAssessment and the redundant Training Records hub card is removed from Kelola Data, leaving the hub clean with only the new dedicated monitoring card as the monitoring entry point
**Depends on:** Phase 80
**Requirements:** CLN-01, CLN-02
**Success Criteria** (what must be TRUE):
  1. The ManageAssessment Assessment Groups tab no longer shows a "Monitoring" dropdown action — the row action menu contains only non-monitoring actions
  2. The Kelola Data hub Section C no longer shows a "Training Records" card — only the Assessment Monitoring card (and other remaining cards) are visible
  3. Direct navigation to the old monitoring URL (if it existed as a dropdown target) either redirects to the new dedicated page or returns 404 — no orphaned route remains
**Plans:** 2/2 plans complete

---

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 73. Critical Fixes | v2.6 | 2/2 | Complete | 2026-03-01 |
| 74. Dead Code Removal | v2.6 | 2/2 | Complete | 2026-03-01 |
| 75. Placeholder Cleanup | v2.6 | 2/2 | Complete | 2026-03-01 |
| 76. Role Fixes & Broken Link | v2.6 | 2/2 | Complete | 2026-03-01 |
| 77. Merge Training Records | v2.6 | 3/3 | Complete | 2026-03-01 |
| 78. Deduplicate CMP page | v2.6 | 1/1 | Complete | 2026-03-01 |
| 79. Assessment Monitoring Page — Group List | 1/1 | Complete    | 2026-03-01 | - |
| 80. Per-Participant Monitoring Detail & HC Actions | 1/1 | Complete    | 2026-03-01 | - |
| 81. Cleanup — Remove Old Entry Points | 2/2 | Complete   | 2026-03-01 | - |
