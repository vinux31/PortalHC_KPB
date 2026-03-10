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
- **v3.14 Bug Hunting Per Case** — Phases 133-137 (shipped 2026-03-09)

</details>

<details>
<summary>v3.15 Assessment Real Time Test (Phases 138-142) — shipped 2026-03-09</summary>

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

<details>
<summary>v3.17 Assessment Sub-Competency Analysis (Phases 145-147) — shipped 2026-03-10</summary>

- Phase 145: Data Model & Migration (completed 2026-03-10)
- Phase 146: Excel Import Update (completed 2026-03-10)
- Phase 147: Scoring & Results UI (completed 2026-03-10)

</details>

---

## v3.18 Homepage Minimalist Redesign

**Milestone Goal:** Sederhanakan Homepage agar minimalis dan clean — seragamkan styling dengan halaman CMP/CDP yang sudah ada.

## Phases

- [x] **Phase 148: CSS Audit & Cleanup** - Audit CSS dependencies then strip glassmorphism, animation, timeline, and deadline styles from home.css (completed 2026-03-10)
- [x] **Phase 149: Homepage View & Controller Redesign** - Remove glass cards, timeline, deadlines from view; simplify hero and quick access markup; optimize controller data-fetching (completed 2026-03-10)

## Phase Details

### Phase 148: CSS Audit & Cleanup
**Goal**: home.css contains only styles required by the simplified homepage — glassmorphism, blur effects, animation, and unused section styles are gone
**Depends on**: Nothing (first phase)
: 1 plan

Plans:
- [ ] 148-01-PLAN.md — Remove glassmorphism, timeline, and deadline CSS; strip data-aos from Homepage

### Phase 149: Homepage View & Controller Redesign
**Goal**: Homepage displays a clean hero greeting and Quick Access cards only — no glass cards, no timeline, no deadlines — and the controller fetches only the data the page actually uses
**Depends on**: Phase 148
**Requirements**: HOME-01, HOME-02, HOME-03, HOME-04, HERO-01, HERO-02, QUICK-01
**Success Criteria** (what must be TRUE):
  1. Homepage does not show IDP Status, Pending Assessment, or Mandatory Training cards for any role
  2. Homepage does not show a Recent Activity timeline section
  3. Homepage does not show an Upcoming Deadlines section
  4. Hero section displays user greeting, nama, position, unit, and tanggal with no glassmorphism or gradient pseudo-elements
  5. Quick Access cards use Bootstrap card pattern (border-0, shadow-sm) matching CMP/CDP Index styling
  6. HomeController.Index no longer executes activity or deadline database queries
**Plans**: 1 plan

Plans:
- [ ] 149-01-PLAN.md — Rewrite View (hero + Quick Access only), simplify ViewModel and Controller, strip unused CSS

## Progress

**Execution Order:** 148 → 149

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 148. CSS Audit & Cleanup | 1/1 | Complete    | 2026-03-10 |
| 149. Homepage View & Controller Redesign | 1/1 | Complete    | 2026-03-10 |
