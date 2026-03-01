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
- 🚧 **v2.4 CDP Progress** — Phases 61-64 (in progress)
- 🚧 **v2.5 User Infrastructure & AD Readiness** — Phases 65-72 (in progress)

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

## v2.4 CDP Progress

### Phases

- [x] **Phase 61: Data Source Fix** — DATA-01, DATA-02, DATA-03, DATA-04 (completed)
- [x] **Phase 62: Functional Filters** — FILT-01, FILT-02, FILT-03, FILT-04, UI-01, UI-03 (completed)
- [x] **Phase 63: Actions** — ACTN-01, ACTN-02, ACTN-03, ACTN-04, ACTN-05 (completed)
- [x] **Phase 64: UI Polish** — UI-02, UI-04 (completed)

### Phase Details

### Phase 61: Data Source Fix
**Goal:** Progress page queries ProtonDeliverableProgress + ProtonTrackAssignment (not IdpItems), displays real coachee list from CoachCoacheeMapping, and computes correct summary stats — the data foundation is accurate
**Depends on:** Phase 53 (v2.3 complete)
**Requirements:** DATA-01, DATA-02, DATA-03, DATA-04
**Plans:** 2/2 plans complete

### Phase 62: Functional Filters
**Goal:** Every filter on the Progress page (Bagian/Unit, Coachee, Track, Tahun, Search) genuinely narrows the data returned — parameters are wired to queries and roles scope what users can see
**Depends on:** Phase 61
**Requirements:** FILT-01, FILT-02, FILT-03, FILT-04, UI-01, UI-03
**Plans:** 2/2 plans complete

### Phase 63: Actions
**Goal:** Approve, reject, coaching report, evidence, and export actions all persist to the database — no more console.log stubs or missing onclick handlers
**Depends on:** Phase 62
**Requirements:** ACTN-01, ACTN-02, ACTN-03, ACTN-04, ACTN-05
**Plans:** 3/3 plans complete

### Phase 64: UI Polish
**Goal:** Progress page handles edge cases gracefully — empty states communicate clearly, and large datasets do not load all rows at once
**Depends on:** Phase 63
**Requirements:** UI-02, UI-04
**Plans:** 2/2 plans complete

## v2.5 User Infrastructure & AD Readiness

### Phases

- [x] **Phase 65: Dynamic Profile Page** — PROF-01, PROF-02, PROF-03 (completed 2026-02-27)
- [x] **Phase 66: Functional Settings Page** — PROF-04, PROF-05, PROF-06 (completed 2026-02-27)
- [x] **Phase 67: ManageWorkers Migration to Admin** — USR-01, USR-02, USR-03, USTR-02 (completed 2026-02-28)
- [x] **Phase 68: Kelola Data Hub Reorganization** — USR-04 (completed 2026-02-28)
- [x] **Phase 69: LDAP Auth Service Foundation** — AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-08, USTR-01 (completed 2026-02-28)
- [x] **Phase 70: Dual Auth Login Flow** — AUTH-05, AUTH-06, AUTH-07 (completed 2026-02-28)
- [x] **Phase 71: User Structure Polish** — USTR-02 completion (completed 2026-02-28)
- [x] **Phase 72: Hybrid Auth & Role Restructuring** — AUTH-HYBRID (completed 2026-02-28)

### Phase Details

### Phase 65: Dynamic Profile Page
**Goal:** Profile page menampilkan data real user login — no more hardcoded placeholders
**Depends on:** Phase 64 (v2.4 complete), or can start in parallel
**Requirements:** PROF-01, PROF-02, PROF-03
**Plans:** 1/1 plans complete

### Phase 66: Functional Settings Page
**Goal:** Settings page functional — change password works, edit profile fields, cleanup non-functional items
**Depends on:** Phase 65
**Requirements:** PROF-04, PROF-05, PROF-06
**Plans:** 2/2 plans complete

### Phase 67: ManageWorkers Migration to Admin
**Goal:** Pindahkan seluruh fitur ManageWorkers (CRUD, import, export, detail) dari CMPController ke AdminController — clean break tanpa redirect, akses via Kelola Data hub card, GetDefaultView() helper extraction
**Depends on:** Phase 66
**Requirements:** USR-01, USR-02, USR-03, USTR-02
**Plans:** 2/2 plans complete

### Phase 68: Kelola Data Hub Reorganization
**Goal:** Admin/Index.cshtml restructured — ManageWorkers prominent, stale items cleaned up
**Depends on:** Phase 67
**Requirements:** USR-04
**Plans:** 1/1 plans complete

### Phase 69: LDAP Auth Service Foundation
**Goal:** Infrastructure dual auth — NuGet, service interface, implementations, config toggle, AuthSource field. Login flow belum diubah.
**Depends on:** Phase 68
**Requirements:** AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-08, USTR-01
**Plans:** 2/2 plans complete

### Phase 70: Dual Auth Login Flow
**Goal:** Login flow pakai IAuthService — global config routing (no per-user AuthSource), profile sync FullName/Email, ManageWorkers + import adaptation for AD mode
**Depends on:** Phase 69
**Requirements:** AUTH-05, AUTH-06, AUTH-07
**Plans:** 3/3 plans complete

### Phase 71: User Structure Polish
**Goal:** Finalize — consistent SelectedView mapping, SeedData cleanup, documentation
**Depends on:** Phase 70
**Requirements:** USTR-02 (completion)
**Plans:** 1/1 plans complete

### Phase 72: Hybrid Auth & Role Restructuring
**Goal:** Enable hybrid authentication (AD fallback to local) so dedicated Admin KPB user works in production AD mode, plus role/access fixes
**Depends on:** Phase 71
**Requirements:** AUTH-HYBRID (new)
**Plans:** 2/2 plans complete

## Progress

| Phase | Milestone | Plans | Status | Completed |
|-------|-----------|-------|--------|-----------|
| 1-3 | v1.0 | 10/10 | Complete | 2026-02-14 |
| 4-8 | v1.1 | 13/13 | Complete | 2026-02-18 |
| 9-12 | v1.2 | 8/8 | Complete | 2026-02-19 |
| 13-15 | v1.3 | 2/2 | Complete | 2026-02-19 |
| 16 | v1.4 | 3/3 | Complete | 2026-02-19 |
| 17 | v1.5 | 7/7 | Complete | 2026-02-19 |
| 18-20 | v1.6 | 3/3 | Complete | 2026-02-20 |
| 21-26 | v1.7 | 14/14 | Complete | 2026-02-21 |
| 27-32 | v1.8 | 10/10 | Complete | 2026-02-23 |
| 33-37 | v1.9 | 8/8 | Complete | 2026-02-24 |
| 38-40 | v2.0 | 5/5 | Complete | 2026-02-24 |
| 41-45 | v2.1 | 13/13 | Complete | 2026-02-25 |
| 46 | v2.2 | 2/2 | Complete | 2026-02-26 |
| 47-53, 59 | v2.3 | 29/29 | Complete | 2026-03-01 |
| 61-64 | v2.4 | 9/9 | Complete | 2026-02-28 |
| 65-72 | v2.5 | 14/14 | Complete | 2026-02-28 |
