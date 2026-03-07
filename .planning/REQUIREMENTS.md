# Requirements: Portal HC KPB

**Defined:** 2026-03-06
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.7 Requirements

Requirements for v3.7 Role Access & Filter Audit. Each maps to roadmap phases.

### Role-Scoped View Content

- [x] **ROLE-01**: CMP Records page shows correct data per role (L1-3 all, L4 section-only, L5-6 own only)
- [x] **ROLE-02**: CMP RecordsTeam page correctly scopes to section for L4 and forbids L5-6
- [ ] **ROLE-03**: CDP CoachingProton page shows correct coachee list per role (L1-3 all, L4 section, L5 mapped, L6 self)
- [x] **ROLE-04**: CDP PlanIdp page scopes content correctly per role
- [x] **ROLE-05**: CDP Deliverable page enforces section check for L4 and coach-coachee check for L5
- [x] **ROLE-07**: CDP HistoriProton page scopes worker list correctly per role

### SectionHead Level 4

- [ ] **SH-01**: SectionHead at level 4 has same section-scoped access as SrSupervisor across all pages
- [ ] **SH-02**: Navigation menu items show/hide correctly for SectionHead level 4
- [ ] **SH-03**: Approval workflow (SrSpv/SH chain) works correctly with SH at level 4

### Filter Consistency

- [x] **FILT-01**: CMP Records filters use OrganizationStructure instead of data-driven queries
- [x] **FILT-02**: CMP RecordsTeam section/unit filters use OrganizationStructure
- [x] **FILT-03**: CDP CoachingProton Bagian/Unit filters use OrganizationStructure (verify)
- [ ] **FILT-04**: Admin ManageWorkers section filter uses OrganizationStructure
- [ ] **FILT-05**: All unit dropdowns cascade correctly from selected Bagian

### Empty States

- [x] **UX-01**: CMP Records shows "Data belum ada" when filtered results are empty
- [x] **UX-02**: CMP RecordsTeam shows "Data belum ada" when filtered results are empty
- [x] **UX-03**: CDP CoachingProton shows "Data belum ada" when filtered results are empty
- [x] **UX-04**: CDP PlanIdp shows "Data belum ada" when filtered results are empty

## Future Requirements

None deferred for this milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Home Dashboard role scoping | Dashboard stats already working, low priority |
| OrganizationStructure migration to DB | Static dict sufficient for current org size |
| New role additions | Not in scope for audit milestone |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ROLE-01 | Phase 109 | Complete |
| ROLE-02 | Phase 109 | Complete |
| ROLE-03 | Phase 110 | Pending |
| ROLE-04 | Phase 110 | Complete |
| ROLE-05 | Phase 110 | Complete |
| ROLE-07 | Phase 110 | Complete |
| SH-01 | Phase 111 | Pending |
| SH-02 | Phase 111 | Pending |
| SH-03 | Phase 111 | Pending |
| FILT-01 | Phase 109 | Complete |
| FILT-02 | Phase 109 | Complete |
| FILT-03 | Phase 110 | Complete |
| FILT-04 | Phase 111 | Pending |
| FILT-05 | Phase 111 | Pending |
| UX-01 | Phase 109 | Complete |
| UX-02 | Phase 109 | Complete |
| UX-03 | Phase 110 | Complete |
| UX-04 | Phase 110 | Complete |

**Coverage:**
- v3.7 requirements: 18 total
- Mapped to phases: 18
- Unmapped: 0

---
*Requirements defined: 2026-03-06*
*Last updated: 2026-03-06 after roadmap creation*
