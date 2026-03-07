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
<summary>v3.0 through v3.7 (Phases 82-111) — shipped 2026-03-02 to 2026-03-07</summary>

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)
- **v3.6 Histori Proton** — Phases 107-108 (shipped 2026-03-06)
- **v3.7 Role Access & Filter Audit** — Phases 109-111 (shipped 2026-03-07)

</details>

---

## v3.8 CoachingProton UI Redesign

**Milestone Goal:** Redesign all buttons and UI elements on CoachingProton page so interactive elements are visually distinguishable from status indicators, with consistent styling across all actions.

## Phases

- [ ] **Phase 112: CoachingProton Button & Badge Redesign** - Replace badge-as-button antipattern with proper buttons, add status icons, unify styling across all interactive elements

## Phase Details

### Phase 112: CoachingProton Button & Badge Redesign
**Goal**: All interactive elements on the CoachingProton page are visually distinguishable from read-only status indicators, with consistent styling across buttons, badges, and approval actions
**Depends on**: Nothing (standalone UI milestone)
**Requirements**: BTN-01, BTN-02, BTN-03, CONS-01, CONS-02, CONS-03, CONS-04, TECH-01, TECH-02, TECH-03
**Success Criteria** (what must be TRUE):
  1. Pending badges in SrSpv and SH columns are clearly clickable buttons with hover/focus states that signal interactivity -- not styled as passive badges
  2. All status badges (Approved, Rejected, Pending, Reviewed) display appropriate icons alongside text, making status distinguishable without relying on color alone
  3. Evidence column submit buttons and status badges have visually distinct, consistent styling -- users can instantly tell which is actionable vs informational
  4. Lihat Detail, Export, Reset, Kembali, and HC Review buttons all have polished, consistent styling that matches across the main table and Antrian Review panel
  5. All existing approval workflows (SrSpv Tinjau modal, SH Tinjau modal, HC Review, Evidence Submit) continue to function after the redesign -- no JS regression
**Plans**: TBD

Plans:
- [ ] 112-01: TBD

## Progress

**Execution Order:** Phase 112 (single phase milestone)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 112. CoachingProton Button & Badge Redesign | 0/? | Not started | - |

---
*Roadmap created: 2026-03-07*
