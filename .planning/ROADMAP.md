# Roadmap: Portal HC KBP

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
<summary>v3.0 through v3.14 (Phases 82-137) — shipped 2026-03-02 to 2026-03-09</summary>

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)
- **v3.6 Histori Proton** — Phases 107-108 (shipped 2026-03-06)
- **v3.7 Role Access & Filter Audit** — Phases 109-111 (shipped 2026-03-07)
- **v3.8 CoachingProton UI Redesign** — Phase 112 (shipped 2026-03-07)
- **v3.9 ProtonData Enhancement** — Phases 113-115 (shipped 2026-03-07)
- **v3.10 Evidence Coaching & Deliverable Redesign** — Phases 116-120 (shipped 2026-03-08)
- **v3.11 CoachCoacheeMapping Overhaul** — Phases 123-125 (shipped 2026-03-08)
- **v3.12 Progress Unit Scoping** — Phases 128-129 (shipped 2026-03-08)
- **v3.13 In-App Notifications** — Phases 130-132 (shipped 2026-03-09)
- **v3.14 Bug Hunting Per Case** — Phases 133-137 (in progress)

</details>

<details>
<summary>v3.15 Assessment Real Time Test (Phases 138-142)</summary>

- Phase 138: Assessment Setup & Monitoring Overview
- Phase 139: Worker Exam Lifecycle
- Phase 140: HC Real-Time Monitoring & Actions
- Phase 141: Post-Exam & Records Validation
- Phase 142: Edge Cases & Integration

</details>

<details>
<summary>v3.16 Form Coaching GAST Redesign (Phases 143-144) — shipped 2026-03-09</summary>

- Phase 143: Modal Form Evidence Acuan (completed 2026-03-09)
- Phase 144: Export PDF Form GAST (completed 2026-03-09)

</details>

---

## v3.17 Assessment Sub-Competency Analysis

**Milestone Goal:** Tambah identitas sub-kompetensi pada soal assessment dan tampilkan analisa spider web (radar chart) + tabel summary di Results page.

## Phases

- [x] **Phase 145: Data Model & Migration** - Add SubCompetency nullable string field to PackageQuestion with DB migration and ViewModel class (completed 2026-03-10)
- [x] **Phase 146: Excel Import Update** - Update import template and parsing logic to support optional Sub Kompetensi column (completed 2026-03-10)
- [x] **Phase 147: Scoring & Results UI** - Calculate per-sub-competency scores and render radar chart + summary table on Results page (completed 2026-03-10)

## Phase Details

### Phase 145: Data Model & Migration
**Goal**: PackageQuestion has a SubCompetency field that persists to the database
**Depends on**: Nothing (first phase)
**Requirements**: SUBTAG-02
**Success Criteria** (what must be TRUE):
  1. PackageQuestion model has a nullable string SubCompetency property
  2. EF Core migration runs successfully and adds the column to the database
  3. Existing PackageQuestion rows retain NULL SubCompetency without errors (backward compatible)
**Plans:** 1/1 plans complete

Plans:
- [ ] 145-01-PLAN.md — Add SubCompetency property + EF Core migration

### Phase 146: Excel Import Update
**Goal**: HC can import questions with optional Sub Kompetensi column via Excel template
**Depends on**: Phase 145
**Requirements**: SUBTAG-01, SUBTAG-03
**Success Criteria** (what must be TRUE):
  1. Download template includes a "Sub Kompetensi" column header
  2. Import with Sub Kompetensi values saves normalized (trimmed, consistent casing) SubCompetency per question
  3. Import with old template (no Sub Kompetensi column) still works without errors — backward compatible
  4. Questions with different casing of the same sub-competency name are normalized to a single canonical form
**Plans:** 1/1 plans complete

Plans:
- [ ] 146-01-PLAN.md — Add Sub Kompetensi column to template, extend import parsing with normalization

### Phase 147: Scoring & Results UI
**Goal**: Results page displays per-sub-competency analysis with radar chart and summary table
**Depends on**: Phase 146
**Requirements**: ANAL-01, ANAL-02, ANAL-03, ANAL-04
**Success Criteria** (what must be TRUE):
  1. After exam submission, system calculates correct percentage per sub-competency via GroupBy on SubCompetency
  2. Results page displays a Chart.js radar chart with one axis per sub-competency showing the score percentage
  3. Results page displays a summary table with columns: Sub Kompetensi, Benar, Total, Persentase
  4. Radar chart and summary table are hidden when questions have no SubCompetency data (graceful degradation for legacy assessments)
  5. Radar chart renders correctly for 3-8 sub-competency axes; below 3 only the table is shown
**Plans:** 1/1 plans complete

Plans:
- [ ] 147-01-PLAN.md — Sub-competency scoring, radar chart, and summary table on Results page

## Progress

**Execution Order:** 145 → 146 → 147

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 145. Data Model & Migration | 1/1 | Complete    | 2026-03-10 |
| 146. Excel Import Update | 1/1 | Complete    | 2026-03-10 |
| 147. Scoring & Results UI | 1/1 | Complete    | 2026-03-10 |
