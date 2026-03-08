# Roadmap: Portal HC KPB

## Shipped Milestones

<details>
<summary>v1.0 through v2.7 (Phases 1-81) — See milestones/ for details</summary>

- v1.0 CMP Assessment Completion (Phases 1-3, shipped 2026-02-17)
- v1.1 CDP Coaching Management (Phases 4-8, shipped 2026-02-18)
- v1.2 UX Consolidation (Phases 9-12, shipped 2026-02-19)
- v1.3 Assessment Management UX (Phases 13-15, shipped 2026-02-19)
- v1.4 Assessment Monitoring (Phase 16, shipped 2026-02-19)
- v1.5 Question and Exam UX (Phase 17, shipped 2026-02-19)
- v1.6 Training Records Management (Phases 18-20, shipped 2026-02-20)
- v1.7 Assessment System Integrity (Phases 21-26, shipped 2026-02-21)
- v1.8 Assessment Polish (Phases 27-32, shipped 2026-02-23)
- v1.9 Proton Catalog Management (Phases 33-37, shipped 2026-02-24)
- v2.0 Assessment Management & Training History (Phases 38-40, shipped 2026-02-24)
- v2.1 Assessment Resilience & Real-Time Monitoring (Phases 41-45, shipped 2026-02-25)
- v2.2 Attempt History (Phase 46, shipped 2026-02-26)
- v2.3 Admin Portal (Phases 47-53, 59, shipped 2026-03-01)
- v2.4 CDP Progress (Phases 61-64, shipped 2026-03-01)
- v2.5 User Infrastructure & AD Readiness (Phases 65-72, shipped 2026-03-01)
- v2.6 Codebase Cleanup (Phases 73-78, shipped 2026-03-01)
- v2.7 Assessment Monitoring (Phases 79-81, shipped 2026-03-01)

</details>

<details>
<summary>v3.0 through v3.8 (Phases 82-112) — shipped 2026-03-02 to 2026-03-07</summary>

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)
- **v3.6 Histori Proton** — Phases 107-108 (shipped 2026-03-06)
- **v3.7 Role Access & Filter Audit** — Phases 109-111 (shipped 2026-03-07)
- **v3.8 CoachingProton UI Redesign** — Phase 112 (shipped 2026-03-07)

</details>

---

## v3.9 ProtonData Enhancement

**Milestone Goal:** Enhance ProtonData/Index with Target column, Status completeness tab, and hard delete with consumer audit.

## Phases

- [x] **Phase 113: Target Column** - Add Target text column to silabus table with edit/save support (completed 2026-03-07)
- [x] **Phase 114: Status Tab** - New first tab showing silabus and guidance completeness tree (completed 2026-03-07)
- [x] **Phase 115: Hard Delete + Consumer Audit** - Kompetensi hard delete with safety checks and consumer verification (completed 2026-03-07)

## Phase Details

### Phase 113: Target Column
**Goal**: Admin/HC can see and edit a Target column on the silabus table
**Depends on**: Nothing (first phase in v3.9)
**Requirements**: TGT-01, TGT-02
**Success Criteria** (what must be TRUE):
  1. Silabus table in view mode shows a Target column between SubKompetensi and Deliverable
  2. In edit mode, user can type a target value into the Target field and save it via SilabusSave
  3. Existing silabus rows display correctly with empty/null Target values
**Plans**: 1 plan

Plans:
- [x] 113-01: Migration, model, DTO, save logic, and UI for Target column

### Phase 114: Status Tab
**Goal**: Admin/HC can see completeness status of silabus and guidance across all tracks at a glance
**Depends on**: Phase 113
**Requirements**: STAT-01, STAT-02, STAT-03, STAT-04
**Success Criteria** (what must be TRUE):
  1. ProtonData/Index opens with Status as the first (default) tab
  2. Status tab displays a tree of Bagian > Unit > Track nodes that can be expanded and collapsed
  3. Each Track node shows a green checkmark in the Silabus column when at least 1 active Kompetensi exists
  4. Each Track node shows a green checkmark in the Guidance column when at least 1 guidance file exists
**Plans**: 1 plan

Plans:
- [x] 114-01: StatusData endpoint and Status tab UI

### Phase 115: Hard Delete + Consumer Audit
**Goal**: Admin/HC can permanently remove incorrectly entered Kompetensi master data while all silabus consumers remain intact
**Depends on**: Phase 114
**Requirements**: DEL-01, DEL-02, DEL-03, AUD-01
**Success Criteria** (what must be TRUE):
  1. View mode shows a Delete button on each Kompetensi row
  2. Clicking Delete shows a confirmation dialog listing the count of SubKompetensi and Deliverable that will be deleted
  3. Delete is blocked with a message when ProtonDeliverableProgress records reference deliverables under that Kompetensi
  4. After successful delete, PlanIdp and CoachingProton pages still function correctly (consumer audit verified)
**Plans**: 1 plan

Plans:
- [ ] 115-01-PLAN.md — Backend cascade delete endpoints + frontend delete button and confirmation modal

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 113. Target Column | 1/1 | Complete    | 2026-03-07 |
| 114. Status Tab | 1/1 | Complete    | 2026-03-07 |
| 115. Hard Delete + Consumer Audit | 1/1 | Complete    | 2026-03-07 |

---

## v3.10 Evidence Coaching & Deliverable Redesign

**Milestone Goal:** Redesign evidence coaching flow — cleanup modal, add status history tracking, restructure Deliverable detail page, build P-Sign infrastructure, and auto-generate PDF evidence coaching forms.

## Phases

- [x] **Phase 116: Modal Cleanup** - Remove Kompetensi Coachee field from evidence modal and backend (completed 2026-03-07)
- [x] **Phase 117: Status History** - New DeliverableStatusHistory table tracking all status changes with rejection persistence (completed 2026-03-07)
- [x] **Phase 118: P-Sign Infrastructure** - User P-Sign data model and renderable badge component (completed 2026-03-07)
- [x] **Phase 119: Deliverable Page Restructure** - Split Deliverable detail into sectioned layout with status timeline (completed 2026-03-08)
- [x] **Phase 120: PDF Evidence** - Auto-generate PDF evidence coaching form with P-Sign (completed 2026-03-08)

## Phase Details

### Phase 116: Modal Cleanup
**Goal**: Evidence modal only captures coaching-relevant fields, with no unused Kompetensi Coachee textarea
**Depends on**: Nothing (independent)
**Requirements**: MOD-01, MOD-02
**Success Criteria** (what must be TRUE):
  1. Evidence modal in CoachingProton no longer shows the "Kompetensi Coachee" textarea
  2. Submitting evidence succeeds without sending koacheeCompetencies data
  3. CoachingSession records created after this change have no koacheeCompetencies value stored
**Plans**: 1 plan

Plans:
- [x] 116-01-PLAN.md — Remove CoacheeCompetencies from modal, controller, model, views + data-clearing migration

### Phase 117: Status History
**Goal**: Every deliverable status change is permanently recorded with actor, timestamp, and reason
**Depends on**: Nothing (independent)
**Requirements**: HIST-01, HIST-02, HIST-03, HIST-04
**Success Criteria** (what must be TRUE):
  1. When a deliverable status changes (submit, approve, reject, review, re-submit), a new row appears in DeliverableStatusHistory
  2. After a rejection followed by re-submit, the original rejection entry (with reason) is still present in the history table
  3. Each approval role (SrSpv, SH, HC) creates a separate history entry when they approve
  4. Re-submitting evidence after rejection creates a "Re-submitted" history entry
**Plans**: 1 plan
nPlans:
- [ ] 117-01-PLAN.md — DeliverableStatusHistory model, migration, and CDPController wiring

### Phase 118: P-Sign Infrastructure
**Goal**: Any user's P-Sign badge can be rendered as a visual component for use in pages and PDFs
**Depends on**: Nothing (independent)
**Requirements**: PSIGN-01, PSIGN-02, PSIGN-03
**Success Criteria** (what must be TRUE):
  1. ApplicationUser has Position/Role text and Unit fields that populate the P-Sign
  2. P-Sign badge renders with Logo Pertamina, Role + Unit, and full name
  3. P-Sign can be rendered as an embeddable HTML component and as an image for PDF embedding
**Plans**: 1 plan

Plans:
- [ ] 118-01-PLAN.md — PSignViewModel, _PSign.cshtml partial view, and Settings page preview

### Phase 119: Deliverable Page Restructure
**Goal**: Deliverable detail page presents coaching data, approval status, and history in clearly separated sections
**Depends on**: Phase 117 (needs DeliverableStatusHistory data)
**Requirements**: PAGE-01, PAGE-02, PAGE-03
**Success Criteria** (what must be TRUE):
  1. Deliverable detail page shows four distinct sections: Detail Coachee & Kompetensi, Evidence Coach, Approval Chain, Riwayat Status
  2. Riwayat Status section displays a chronological timeline of all status changes from DeliverableStatusHistory
  3. Evidence Coach section shows Catatan Coach, Kesimpulan, Result, and a download button for the evidence file
**Plans**: 1 plan
nPlans:
- [ ] 119-01-PLAN.md — Rewrite Deliverable.cshtml with 4-card sectioned layout

### Phase 120: PDF Evidence
**Goal**: Coach can download a professional PDF evidence form after submitting coaching evidence
**Depends on**: Phase 116 (modal fields define PDF content), Phase 118 (P-Sign component for PDF)
**Requirements**: PDF-01, PDF-02, PDF-03, PDF-04
**Success Criteria** (what must be TRUE):
  1. After coach submits evidence, a PDF is auto-generated containing the coaching session data
  2. PDF contains: Coachee info, Track, Kompetensi, SubKompetensi, Deliverable, Tanggal, Catatan Coach, Kesimpulan, Result
  3. PDF displays the Coach's P-Sign badge in the bottom-right corner
  4. Deliverable detail page has a "Download PDF" button that downloads the generated PDF
**Plans**: 1 plan

Plans:
- [ ] 120-01-PLAN.md — DownloadEvidencePdf action + download button in Deliverable Card 3

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 116. Modal Cleanup | 1/1 | Complete    | 2026-03-07 |
| 117. Status History | 1/1 | Complete    | 2026-03-07 |
| 118. P-Sign Infrastructure | 1/1 | Complete    | 2026-03-07 |
| 119. Deliverable Page Restructure | 1/1 | Complete    | 2026-03-08 |
| 120. PDF Evidence | 1/1 | Complete   | 2026-03-08 |
