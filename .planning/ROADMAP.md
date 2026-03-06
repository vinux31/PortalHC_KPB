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

- **v3.0 Full QA & Feature Completion** — Phases 82-91 (shipped 2026-03-05)
- **v3.1 CPDP Mapping File-Based Rewrite** — Phase 88 CPDP (shipped 2026-03-03)
- **v3.2 Bug Hunting & Quality Audit** — Phases 92-98 (shipped 2026-03-05)
- **v3.5 User Guide** — Phases 105-106 (shipped 2026-03-06)
- **v3.6 Histori Proton** — Phases 107-108 (shipped 2026-03-06)

---

## v3.7 Role Access & Filter Audit

**Milestone Goal:** Audit and fix role-level access, view content, and filters across all CMP/CDP pages so every level sees correctly scoped data, filters use OrganizationStructure, and empty results show proper messages.

## Phases

- [x] **Phase 109: CMP Role Access & Filters** - Fix role scoping, OrganizationStructure filters, and empty states on Records and RecordsTeam (completed 2026-03-06)
- [ ] **Phase 110: CDP Role Access & Filters** - Fix role scoping, filters, and empty states on CoachingProton, PlanIdp, Deliverable, and HistoriProton
- [ ] **Phase 111: SectionHead & Filter Infrastructure** - SectionHead level 4 consistency across all pages, ManageWorkers filter, and cascade wiring

## Phase Details

### Phase 109: CMP Role Access & Filters
**Goal**: Every role sees correctly scoped data on CMP Records and RecordsTeam, with OrganizationStructure-based filters and empty states
**Depends on**: Nothing (first phase)
**Requirements**: ROLE-01, ROLE-02, FILT-01, FILT-02, UX-01, UX-02
**Success Criteria** (what must be TRUE):
  1. L1-3 user on CMP Records sees all workers; L4 sees section-only; L5-6 see own records only
  2. L4 user on CMP RecordsTeam sees only their section's workers; L5-6 are denied access
  3. Bagian and Unit filter dropdowns on Records and RecordsTeam are populated from OrganizationStructure (not from existing data queries)
  4. Selecting a filter combination that returns no data shows "Data belum ada" message instead of empty table
**Plans:** 1/1 plans complete

Plans:
- [ ] 109-01-PLAN.md — OrganizationStructure filters, cascade, role scoping verification, empty states

### Phase 110: CDP Role Access & Filters
**Goal**: Every role sees correctly scoped data on all CDP pages, with consistent filters and empty states
**Depends on**: Phase 109
**Requirements**: ROLE-03, ROLE-04, ROLE-05, ROLE-07, FILT-03, UX-03, UX-04
**Success Criteria** (what must be TRUE):
  1. CoachingProton shows correct coachee list per role (L1-3 all, L4 section, L5 mapped coachees, L6 self only)
  2. PlanIdp scopes content correctly per role level
  3. Deliverable page enforces section check for L4 and coach-coachee mapping check for L5
  4. HistoriProton worker list is scoped correctly per role level
  5. CoachingProton and PlanIdp show "Data belum ada" when filtered results are empty
**Plans**: TBD

Plans:
- [ ] 110-01: TBD
- [ ] 110-02: TBD

### Phase 111: SectionHead & Filter Infrastructure
**Goal**: SectionHead at level 4 has consistent access everywhere, ManageWorkers filter fixed, and all unit dropdowns cascade correctly
**Depends on**: Phase 110
**Requirements**: SH-01, SH-02, SH-03, FILT-04, FILT-05
**Success Criteria** (what must be TRUE):
  1. SectionHead at level 4 has identical section-scoped access as SrSupervisor on every CMP/CDP page
  2. Navigation menu items show/hide correctly for SectionHead level 4 (same visibility as SrSupervisor)
  3. SrSpv/SH approval chain works correctly when SH is at level 4
  4. Admin ManageWorkers section filter uses OrganizationStructure
  5. Selecting a Bagian in any filter dropdown cascades to show only that Bagian's units
**Plans**: TBD

Plans:
- [ ] 111-01: TBD
- [ ] 111-02: TBD

## Progress

**Execution Order:** 109 → 110 → 111

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 109. CMP Role Access & Filters | 1/1 | Complete    | 2026-03-06 |
| 110. CDP Role Access & Filters | 0/? | Not started | - |
| 111. SectionHead & Filter Infrastructure | 0/? | Not started | - |

---
*Roadmap created: 2026-03-06*
*Last updated: 2026-03-06 after Phase 109 planning*
