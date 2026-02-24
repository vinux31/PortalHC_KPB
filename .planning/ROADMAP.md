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
- ✅ **v1.8 Assessment Polish** — Phases 27-32 (shipped 2026-02-23)
- 🚧 **v1.9 Proton Catalog Management** — Phases 33-37 (in progress)
- 🔜 **v2.0 Assessment Management & Training History** — Phases 38-40 (planned)

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

### 🚧 v1.9 Proton Catalog Management (Phases 33-37)

**Milestone Goal:** HC/Admin can manage the full Proton program catalog (Tracks → Kompetensi → SubKompetensi → Deliverable) through a single web page with no database access needed — full CRUD, drag-and-drop reorder, and delete guards with active coachee impact counts.

---

#### Phase 33: ProtonTrack Schema
**Goal:** The ProtonTrack entity exists as a first-class table and ProtonKompetensi references it via FK — no code or data depends on the old TrackType+TahunKe strings anymore
**Depends on:** Phase 32 (v1.8 complete)
**Requirements:** SCHEMA-01
**Success Criteria** (what must be TRUE):
  1. A `ProtonTrack` table exists in the database with TrackType, TahunKe, and DisplayName columns
  2. `ProtonKompetensi` rows each have a non-null `ProtonTrackId` FK pointing to a row in `ProtonTrack`
  3. All existing seeded Proton data (Panelman / Operator tracks, Tahun 1/2/3) is present and intact after migration
  4. The AssignTrack workflow resolves tracks by ProtonTrackId — no code reads TrackType+TahunKe strings from ProtonKompetensi directly
**Plans:** 2 plans

Plans:
- [x] 33-01-PLAN.md — ProtonTrack model, DbContext registration, EF migration; data migration populating ProtonTrack rows from distinct TrackType+TahunKe values in existing ProtonKompetensi; add ProtonTrackId FK column and backfill; drop old string columns
- [x] 33-02-PLAN.md — Update SeedProtonData.cs to seed via ProtonTrack FKs; update AssignTrack and any other CDPController reads that filter by TrackType+TahunKe to use ProtonTrackId

---

#### Phase 34: Catalog Page
**Goal:** HC/Admin can open the Proton Catalog Manager from navigation and view the complete Kompetensi → SubKompetensi → Deliverable tree for any track, with expand/collapse per row and a working track dropdown
**Depends on:** Phase 33
**Requirements:** CAT-01, CAT-02, CAT-09
**Success Criteria** (what must be TRUE):
  1. HC/Admin sees a "Proton Catalog" link in the navigation that takes them to the catalog manager page
  2. HC/Admin selects a track from the dropdown — the page shows all Kompetensi for that track as top-level rows
  3. HC/Admin expands a Kompetensi row — SubKompetensi rows appear beneath it; expanding a SubKompetensi row reveals its Deliverables
  4. HC/Admin submits the "Add Track" modal with TrackType, TahunKe, and DisplayName — the new track appears in the dropdown immediately without a full page reload
  5. Page is read-only in this phase (no add/edit/delete/reorder controls yet)
**Plans:** 2 plans

Plans:
- [x] 34-01-PLAN.md — ProtonCatalogController with Index GET (load tracks, load catalog for selected track), GetCatalogTree GET (AJAX partial), AddTrack POST (JSON); ProtonCatalogViewModel appended to ProtonViewModels.cs
- [x] 34-02-PLAN.md — Views/ProtonCatalog/Index.cshtml (track dropdown, AJAX tree reload, Add Track modal + JS), Views/ProtonCatalog/_CatalogTree.cshtml (Bootstrap collapse tree partial), _Layout.cshtml CDP nav dropdown with Proton Catalog link (HC/Admin only)

---

#### Phase 35: CRUD Add and Edit
**Goal:** HC/Admin can add Kompetensi, SubKompetensi, and Deliverables inline and rename any item in-place — all without leaving the page
**Depends on:** Phase 34
**Requirements:** CAT-03, CAT-04, CAT-05, CAT-06
**Success Criteria** (what must be TRUE):
  1. HC/Admin clicks "Add Kompetensi" for the selected track — an inline input appears; submitting it adds the new Kompetensi to the tree without a page reload
  2. HC/Admin clicks "Add SubKompetensi" under a Kompetensi row — an inline input appears in the expanded section; submitting adds the SubKompetensi under the correct parent
  3. HC/Admin clicks "Add Deliverable" under a SubKompetensi row — an inline input appears and submission adds the Deliverable under the correct parent
  4. HC/Admin clicks the name of any Kompetensi, SubKompetensi, or Deliverable — it becomes an editable field; saving commits the new name via AJAX and the row updates in-place
**Plans:** 2 plans

Plans:
- [ ] 35-01-PLAN.md — Backend: AddKompetensi, AddSubKompetensi, AddDeliverable POST actions (JSON, return new item id + name); EditCatalogItem POST action (accepts level + id + name, updates correct entity)
- [ ] 35-02-PLAN.md — Frontend: empty-state messages per level, inline Add input rows with Save/Cancel buttons, pencil-icon edit trigger (visible on expand), AJAX wiring for all four endpoints with antiforgery token; human verification checkpoint

---

#### Phase 36: Delete Guards
**Goal:** HC/Admin can delete any catalog item only after seeing how many active coachees are affected and explicitly confirming — deletion cascades to all child items
**Depends on:** Phase 35
**Requirements:** CAT-07
**Success Criteria** (what must be TRUE):
  1. HC/Admin clicks "Delete" on a Kompetensi — a Bootstrap modal appears showing the name of the item and the count of active coachees who have progress on it or any child item
  2. HC/Admin types or clicks a hard confirmation in the modal — the item and all its SubKompetensi and Deliverables are deleted; the tree updates without a page reload
  3. When 0 coachees are affected, the modal still appears but states "No active coachees affected" — deletion still requires confirmation
  4. Delete is available on SubKompetensi and Deliverables with the same guard — each shows its own affected coachee count
  5. Cascade order is enforced: Deliverables deleted before SubKompetensi before Kompetensi (no FK constraint errors)
**Plans:** 2 plans

Plans:
- [ ] 36-01-PLAN.md — Backend: GetDeleteImpact GET action (returns affected coachee count for item + children); DeleteCatalogItem POST action with cascade delete in correct order (Deliverables → SubKompetensi → Kompetensi → Track)
- [ ] 36-02-PLAN.md — Frontend: trash icon in _CatalogTree.cshtml; shared #deleteModal in Index.cshtml; initDeleteGuards() function wired into initCatalogTree() call chain; human verification checkpoint

---

#### Phase 37: Drag-and-Drop Reorder
**Goal:** HC/Admin can drag Kompetensi, SubKompetensi, or Deliverable rows to a new position within their level — the new order persists immediately
**Depends on:** Phase 36
**Requirements:** CAT-08
**Success Criteria** (what must be TRUE):
  1. HC/Admin drags a Kompetensi row to a new position — on drop, the visual order updates and an AJAX call persists the new sort order; page reload confirms order is saved
  2. HC/Admin drags a SubKompetensi row within its Kompetensi — same behavior: visual update + AJAX persist
  3. HC/Admin drags a Deliverable row within its SubKompetensi — same behavior
  4. Reorder is constrained within the same parent — a Kompetensi cannot be dropped into a different track's section; a Deliverable cannot be dropped under a different SubKompetensi
**Plans:** 2 plans

Plans:
- [ ] 37-01-PLAN.md — Backend: ReorderKompetensi, ReorderSubKompetensi, ReorderDeliverable POST actions in ProtonCatalogController; each accepts int[] orderedIds and reassigns Urutan 1..N
- [ ] 37-02-PLAN.md — Frontend: SortableJS 1.15.7 CDN in _Layout.cshtml; grip handle column in _CatalogTree.cshtml; initSortables() + handleDropEnd() + in-flight lock in Index.cshtml; human verification checkpoint

---

### 🔜 v2.0 Assessment Management & Training History (Phases 38-40)

**Milestone Goal:** HC gets better control over active assessments and a unified history view — close assessments early with fair scoring, auto-clean stale entries from Management and Monitoring tabs, and see all training + assessment completions in one place.

---

#### Phase 38: Auto-Hide Filter
**Goal:** Stale assessment groups disappear automatically from both the Management tab and the Monitoring tab 7 days after their exam window closes — HC no longer sees old completed assessments cluttering the active list
**Depends on:** Phase 37 (v1.9 complete)
**Requirements:** ASSESS-02, ASSESS-03
**Success Criteria** (what must be TRUE):
  1. An assessment group whose ExamWindowCloseDate was 8 or more days ago no longer appears in the Monitoring tab — only groups closed within the last 7 days (or still open/upcoming) are shown
  2. The same assessment group is also absent from the Management tab — both tabs apply the identical 7-day cutoff filter
  3. An assessment group with no ExamWindowCloseDate falls back to Schedule date for the 7-day calculation — groups are not permanently pinned just because no close date was set
  4. An assessment group closed exactly 7 days ago is still visible — it disappears on day 8
**Plans:** 1 plan

Plans:
- [x] 38-01-PLAN.md — Backend query change: add 7-day cutoff filter to both GetManageData and GetMonitorData queries using ExamWindowCloseDate ?? Schedule.Date; no frontend changes needed

---

#### Phase 39: Close Early
**Goal:** HC can stop an active assessment group from the MonitoringDetail page — workers already in progress receive a fair score calculated from their actual submitted answers rather than a zero
**Depends on:** Phase 38
**Requirements:** ASSESS-01
**Success Criteria** (what must be TRUE):
  1. HC sees a "Tutup Lebih Awal" button on AssessmentMonitoringDetail for any Open assessment group — the button is not shown for Upcoming or already-closed groups
  2. HC clicks the button and confirms — ExamWindowCloseDate is set to now for all sessions in the group; workers who have not started can no longer access the exam
  3. Workers whose status was InProgress at the time of close are marked Completed with a score calculated from their actual PackageUserResponse answers (not Score=0)
  4. Workers whose status was Not Started remain Not Started — they are locked out but their score stays null
  5. The MonitoringDetail page reflects the updated statuses and scores immediately after the action completes
**Plans:** 2 plans

Plans:
- [x] 39-01-PLAN.md — Backend: CloseEarly POST action in CMPController; sets ExamWindowCloseDate=DateTime.UtcNow on all sibling sessions; for each InProgress session, calculate score from PackageUserResponse answers using same grading logic as SubmitExam package path; mark session Completed+IsPassed; audit log entry
- [x] 39-02-PLAN.md — Frontend: "Tutup Lebih Awal" button in AssessmentMonitoringDetail.cshtml (HC/Admin only, Open groups only); Bootstrap confirmation modal with warning text; POST form to CloseEarly action; human verification checkpoint

---

#### Phase 40: Training Records History Tab
**Goal:** HC can see all workers' training activity — both manual training records and completed assessment online sessions — in one combined chronological list without navigating to individual worker pages
**Depends on:** Phase 38
**Requirements:** HIST-01
**Success Criteria** (what must be TRUE):
  1. The RecordsWorkerList page has a second tab labelled "History" alongside the existing worker list tab — clicking it shows the combined history table
  2. The History tab shows one row per training event across all workers: manual TrainingRecords and completed AssessmentSessions (IsPassed is not null) are both included
  3. Each row displays the worker name, record type (Manual / Assessment Online), title, tanggal mulai, and relevant type-specific detail (penyelenggara for manual; score+pass/fail for assessment)
  4. Rows are sorted by tanggal mulai descending — the most recent activity appears at the top regardless of type
  5. HC can use the History tab to confirm a specific worker completed a training or assessment without opening that worker's individual detail page
**Plans:** 2 plans

Plans:
- [ ] 40-01-PLAN.md — Backend: AllWorkersHistoryViewModel; CMPController action or extension to RecordsWorkerList GET that queries all TrainingRecords + completed AssessmentSessions (Status=="Completed"), merges and sorts by tanggal mulai descending; human verification checkpoint
- [ ] 40-02-PLAN.md — Frontend: second Bootstrap tab in RecordsWorkerList.cshtml; history table with type badge, worker name, title, date, type-specific columns; tab persistence via activeTab hidden input or URL param; human verification checkpoint

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
| 28. Package Reshuffle | v1.8 | 2/2 | Complete | 2026-02-21 |
| 29. Auto-transition Upcoming to Open | v1.8 | 3/3 | Complete | 2026-02-21 |
| 30. Import Deduplication | v1.8 | 1/1 | Complete | 2026-02-23 |
| 31. HC Reporting Actions | v1.8 | 2/2 | Complete | 2026-02-23 |
| 32. Fix Legacy Question Path | v1.8 | 1/1 | Complete | 2026-02-21 |
| 33. ProtonTrack Schema | v1.9 | 2/2 | Complete | 2026-02-23 |
| 34. Catalog Page | v1.9 | 2/2 | Complete | 2026-02-23 |
| 35. CRUD Add and Edit | v1.9 | 2/2 | Complete | 2026-02-24 |
| 36. Delete Guards | v1.9 | 2/2 | Complete | 2026-02-24 |
| 37. Drag-and-Drop Reorder | v1.9 | 0/2 | Not started | — |
| 38. Auto-Hide Filter | v2.0 | 1/1 | Complete | 2026-02-24 |
| 39. Close Early | v2.0 | 2/2 | Complete | 2026-02-24 |
| 40. Training Records History Tab | v2.0 | 2/2 | Complete | 2026-02-24 |
