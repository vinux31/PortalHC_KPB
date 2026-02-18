# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 CMP Assessment Completion** — Phases 1-3 (shipped 2026-02-17)
- ✅ **v1.1 CDP Coaching Management** — Phases 4-7 (shipped 2026-02-18)
- ✅ **Post-v1.1 Fix: Admin Role Switcher** — Phase 8 (shipped 2026-02-18)
- 🚧 **v1.2 UX Consolidation** — Phases 9-12 (in progress)

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

---

### 🚧 v1.2 UX Consolidation (In Progress)

**Milestone Goal:** Refocus core pages to their intended purpose and consolidate overlapping dashboards into a coherent, role-aware experience. Assessment page shows only actionable items per role. Training Records becomes the single unified development history. Gap Analysis page is removed. Three dashboards are merged into one tabbed CDP Dashboard.

#### Phase 9: Gap Analysis Removal
**Goal:** The Gap Analysis page, its nav entry points, and associated controller action are fully removed so the CMP hub and CPDP Progress pages no longer reference a dead route
**Depends on:** Phase 8
**Requirements:** GAPS-01
**Success Criteria** (what must be TRUE):
  1. The Gap Analysis nav card is absent from CMP Index — no link renders for any role
  2. The Gap Analysis cross-link is absent from CPDP Progress — no link renders for any role
  3. Navigating directly to the former Gap Analysis URL returns a redirect or 404, not a working page
  4. The application builds and loads without errors after the removal
**Plans:** TBD

Plans:
- [ ] 09-01-PLAN.md — Remove CompetencyGap action, view, and nav links (CMP/Index.cshtml + CMP/CpdpProgress.cshtml) in one atomic commit

---

#### Phase 10: Unified Training Records
**Goal:** Users can view their complete development history — completed assessments and manual training records — in a single merged table with type-differentiated columns, and HC can see a worker list with completion rates drawn from both sources
**Depends on:** Phase 9
**Requirements:** TREC-01, TREC-02, TREC-03
**Success Criteria** (what must be TRUE):
  1. A worker visiting Training Records sees one merged table containing both completed assessment sessions and manual training records, sorted most-recent-first
  2. Each row is visually distinguishable by type: Assessment Online rows show Score and Pass/Fail; Training Manual rows show Penyelenggara, Tipe Sertifikat, and Berlaku Sampai
  3. Certificate expiry warnings are preserved for Training Manual rows that have a Berlaku Sampai date
  4. HC or Admin visiting Training Records sees a worker list with completion rate calculated from both data sources (completed assessments + valid training records)
**Plans:** TBD

Plans:
- [ ] 10-01-PLAN.md — UnifiedCapabilityRecord ViewModel + BuildUnifiedRecords() helper in CMPController
- [ ] 10-02-PLAN.md — Updated Records.cshtml with merged table, type badge, conditional columns, and HC worker list completion rate

---

#### Phase 11: Assessment Page Role Filter
**Goal:** Workers see only Open and Upcoming assessments on the Assessment page; HC and Admin see a restructured page with dedicated Management and Monitoring tabs
**Depends on:** Phase 10 (Training Records must exist as the history destination before Completed items are filtered out)
**Requirements:** ASMT-01, ASMT-02, ASMT-03
**Success Criteria** (what must be TRUE):
  1. A worker (Spv or below) visiting the Assessment page sees only Open and Upcoming assessments — Completed assessments do not appear in the list
  2. The Assessment page includes a visible link or callout directing workers to Training Records for their completion history
  3. HC or Admin visiting the Assessment page sees a Management tab (assessment CRUD and question management) as a distinct tab
  4. HC or Admin visiting the Assessment page sees a Monitoring tab showing all active and upcoming assessments across the system
  5. Empty states are shown per tab when no assessments match the tab's criteria
**Plans:** TBD

Plans:
- [ ] 11-01-PLAN.md — CMPController Assessment action: status filter for workers, monitor branch for HC/Admin
- [ ] 11-02-PLAN.md — Assessment.cshtml: tab restructure, worker callout to Training Records, empty states

---

#### Phase 12: Dashboard Consolidation
**Goal:** The CDP Dashboard becomes the single unified entry point for all development-related views — HC Reports and the Dev Dashboard are absorbed into role-scoped tabs, and standalone pages are retired after the tabs are verified
**Depends on:** Phase 11
**Requirements:** DASH-01, DASH-02, DASH-03, DASH-04
**Success Criteria** (what must be TRUE):
  1. The CDP Dashboard has two visible tabs: "Proton Progress" (accessible to all roles) and "Assessment Analytics" (visible only to HC and Admin — tab is absent from the DOM for other roles)
  2. The Proton Progress tab shows role-scoped data: Coachee sees their own summary; Spv sees their unit; SrSpv/SectionHead see their section; HC/Admin see all — matching the former Dev Dashboard scope rules
  3. The Assessment Analytics tab displays KPI cards, assessment result filters, and an export-to-Excel action — replacing the standalone HC Reports page for HC and Admin users
  4. The standalone Dev Dashboard nav item is removed from the layout after tab content is verified
**Plans:** TBD

Plans:
- [ ] 12-01-PLAN.md — CDPDashboardViewModel + CDPController.Dashboard() rewrite with role-gated sub-model population
- [ ] 12-02-PLAN.md — Dashboard.cshtml tab structure with _HCReportsPartial and _DevDashboardPartial, plus authorization re-declaration audit
- [ ] 12-03-PLAN.md — Retire standalone Dev Dashboard nav item and audit cross-controller Url.Action references

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
| 9. Gap Analysis Removal | v1.2 | 0/1 | Not started | - |
| 10. Unified Training Records | v1.2 | 0/2 | Not started | - |
| 11. Assessment Page Role Filter | v1.2 | 0/2 | Not started | - |
| 12. Dashboard Consolidation | v1.2 | 0/3 | Not started | - |
