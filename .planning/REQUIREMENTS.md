# Requirements: Portal HC KPB

**Defined:** 2026-02-19
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v1.3 Requirements

Requirements for Assessment Management UX milestone. Phases continue from 12 → start at 13.

### Navigation (NAV)

- [ ] **NAV-01**: HC/Admin sees a dedicated "Manage Assessments" card on CMP Index (workers do not see it)
- [ ] **NAV-02**: CMP Index "Manage Assessments" card links directly to manage view (`/CMP/Assessment?view=manage`)
- [ ] **NAV-03**: Embedded Create Assessment form removed from CMP Index (revert)

### Assessment Creation (CRT)

- [ ] **CRT-01**: "Create Assessment" green button on manage view links to dedicated `/CMP/CreateAssessment` page
- [ ] **CRT-02**: After successful creation, HC is redirected back to manage view (not CMP Index)

### Bulk Assign (BLK)

- [ ] **BLK-01**: HC can add more users to an existing assessment from the manage view
- [ ] **BLK-02**: Bulk assign shows current assigned users and lets HC select additional ones

### Quick Edit (QED)

- [ ] **QED-01**: HC can change an assessment's status directly from the manage view card (no full Edit page needed)
- [ ] **QED-02**: HC can reschedule an assessment directly from the manage view card

## Future Requirements

*(Nothing deferred yet)*

## Out of Scope

| Feature | Reason |
|---------|--------|
| Bulk delete assessments | High risk, not requested |
| Assessment templates | Future milestone |
| Notification on new assignment | Notifications feature not yet built |

## Traceability

*(Populated during roadmap creation)*

| Requirement | Phase | Status |
|-------------|-------|--------|
| NAV-01 | — | Pending |
| NAV-02 | — | Pending |
| NAV-03 | — | Pending |
| CRT-01 | — | Pending |
| CRT-02 | — | Pending |
| BLK-01 | — | Pending |
| BLK-02 | — | Pending |
| QED-01 | — | Pending |
| QED-02 | — | Pending |

**Coverage:**
- v1.3 requirements: 9 total
- Mapped to phases: 0
- Unmapped: 9 ⚠️

---
*Requirements defined: 2026-02-19*
*Last updated: 2026-02-19 after initial definition*
