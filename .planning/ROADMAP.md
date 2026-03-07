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

- [ ] **Phase 113: Target Column** - Add Target text column to silabus table with edit/save support
- [ ] **Phase 114: Status Tab** - New first tab showing silabus and guidance completeness tree
- [ ] **Phase 115: Hard Delete + Consumer Audit** - Kompetensi hard delete with safety checks and consumer verification

## Phase Details

### Phase 113: Target Column
**Goal**: Admin/HC can see and edit a Target column on the silabus table
**Depends on**: Nothing (first phase in v3.9)
**Requirements**: TGT-01, TGT-02
**Success Criteria** (what must be TRUE):
  1. Silabus table in view mode shows a Target column between SubKompetensi and Deliverable
  2. In edit mode, user can type a target value into the Target field and save it via SilabusSave
  3. Existing silabus rows display correctly with empty/null Target values
**Plans**: TBD

Plans:
- [ ] 113-01: Migration, model, and UI for Target column

### Phase 114: Status Tab
**Goal**: Admin/HC can see completeness status of silabus and guidance across all tracks at a glance
**Depends on**: Phase 113
**Requirements**: STAT-01, STAT-02, STAT-03, STAT-04
**Success Criteria** (what must be TRUE):
  1. ProtonData/Index opens with Status as the first (default) tab
  2. Status tab displays a tree of Bagian > Unit > Track nodes that can be expanded and collapsed
  3. Each Track node shows a green checkmark in the Silabus column when at least 1 active Kompetensi exists
  4. Each Track node shows a green checkmark in the Guidance column when at least 1 guidance file exists
**Plans**: TBD

Plans:
- [ ] 114-01: StatusData endpoint and Status tab UI

### Phase 115: Hard Delete + Consumer Audit
**Goal**: Admin/HC can permanently remove incorrectly entered Kompetensi master data while all silabus consumers remain intact
**Depends on**: Phase 114
**Requirements**: DEL-01, DEL-02, DEL-03, AUD-01
**Success Criteria** (what must be TRUE):
  1. View mode shows a Delete button on each Kompetensi row
  2. Clicking Delete shows a confirmation dialog listing the count of SubKompetensi and Deliverable that will be deleted
  3. Delete is blocked with a message when ProtonDeliverableProgress records reference deliverables under that Kompetensi
  4. After successful delete, PlanIdp and CoachingProton pages still function correctly (consumer audit verified)
**Plans**: TBD

Plans:
- [ ] 115-01: Hard delete endpoint, confirmation dialog, consumer audit

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 113. Target Column | 0/1 | Not started | - |
| 114. Status Tab | 0/1 | Not started | - |
| 115. Hard Delete + Consumer Audit | 0/1 | Not started | - |
