# Roadmap: Portal HC KPB

## Current Milestone: v3.6 Histori Proton

### Phase 107: Backend & Worker List Page

**Goal:** Build CDPController actions, role-scoped access, and worker list page with search/filter.

**Requirements:** HIST-01 through HIST-08

**Plans:** 2/2 plans complete

Plans:
- [ ] 107-01-PLAN.md — Backend: ViewModel, CDPController actions with role-scoped access, CDP Hub card
- [ ] 107-02-PLAN.md — Worker list Razor view with table, search, filters, step indicator, status badges

**Success Criteria:**
1. CDPController has HistoriProton (list) and HistoriProtonDetail (timeline) actions
2. CDP navbar shows "Histori Proton" menu item
3. Role-scoped access: Coachee redirects to own detail, Coach/SrSpv/SH sees section, HC/Admin sees all
4. Worker list page displays workers with Proton history (from ProtonTrackAssignment)
5. Search by nama/NIP and filter by unit/section work correctly
6. Each row shows summary: nama, NIP, tahun Proton terakhir, status terakhir

### Phase 108: Timeline Detail Page & Styling

**Goal:** Build vertical timeline detail page with Proton year nodes and responsive styling.

**Requirements:** HIST-09 through HIST-17

**Success Criteria:**
1. Vertical timeline with distinct node per Proton year (filled/empty based on status)
2. Each node displays: Tahun (1/2/3), Unit, Coach name, Status badge, Competency Level, Dates
3. Timeline ordered chronologically (Tahun 1 -> 2 -> 3)
4. Status badges: Lulus (green), Dalam Proses (yellow), Belum Mulai (gray)
5. Design consistent with portal design system (Bootstrap 5, CSS variables)
6. Responsive mobile layout

---

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

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)

---
*Roadmap created: 2026-03-06*
*Last updated: 2026-03-06*
