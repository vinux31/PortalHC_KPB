# Requirements: Portal HC KPB

**Defined:** 2026-02-21
**Milestone:** v1.8 Assessment Polish
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v1 Requirements

Requirements for v1.8 milestone. Each maps to roadmap phases.

### Monitoring Fix (MON)

- [ ] **MON-01**: HC sees correct status for Abandoned and InProgress sessions in the monitoring tab card summary — not "Not started"

### Package Management (PKG)

- [ ] **PKG-01**: HC can re-assign a different package to a worker who has already been assigned one, replacing their current package
- [ ] **PKG-02**: HC can trigger a reshuffle (new random package) for a worker from the management UI

### Assessment Scheduling (SCHED)

- [ ] **SCHED-01**: Assessment sessions with status `Upcoming` automatically transition to `Open` when the scheduled date arrives, without HC manual action

### Import Quality (IMP)

- [ ] **IMP-01**: When importing questions via Excel or paste, rows with question text identical to an existing question in that package are skipped (not duplicated)

### Reporting (RPT)

- [ ] **RPT-01**: HC can download an Excel file of all worker results for a specific assessment from the monitoring detail page
- [ ] **RPT-02**: HC can force-close all Open and InProgress sessions for an assessment at one click from the monitoring view, without closing one by one

## v2 Requirements

Deferred to v1.9+.

### UX Polish

- **UX-01**: Answer review shows point value per question alongside correct/incorrect indicator
- **UX-03**: Status transition logic — Upcoming sessions auto-transition to Open by scheduled date (deferred if SCHED-01 covers this)

### Performance

- **PERF-01**: Monitoring queries use batch loading (Include/projection) to eliminate N+1 patterns
- **PERF-02**: TempData int/long coercion replaced with robust type-safe serialization

### Worker Features

- **WTRN-01**: Worker can create their own manual training record
- **WTRN-02**: Worker can upload a certificate for their self-created training record

## Out of Scope

| Feature | Reason |
|---------|--------|
| Email notifications for exam events | No email system in project; deferred to future milestone |
| Real-time exam monitoring | High complexity; not required for assessment polish |
| Mobile-optimized exam UI | Web-only per project constraints |
| Automated test suite | Out of scope per project constraints |
| Worker self-add training records | Deferred to v1.9+ (WTRN-01, WTRN-02) |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| MON-01 | Phase 27 | Pending |
| PKG-01 | Phase 28 | Pending |
| PKG-02 | Phase 28 | Pending |
| SCHED-01 | Phase 29 | Pending |
| IMP-01 | Phase 30 | Pending |
| RPT-01 | Phase 31 | Pending |
| RPT-02 | Phase 31 | Pending |

**Coverage:**
- v1 requirements: 7 total
- Mapped to phases: 7
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-21*
*Last updated: 2026-02-21 — initial definition for v1.8*
