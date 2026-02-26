# Requirements: Portal HC KPB

**Defined:** 2026-02-26
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v2.2 Requirements

### Attempt History

- [ ] **HIST-01**: When HC resets an assessment session, the current attempt data (score, pass/fail, started_at, completed_at, status) is archived as a historical record before the session is cleared
- [ ] **HIST-02**: HC and Admin can view all historical attempts per worker per assessment in the History tab at /CMP/Records, with an Attempt # column showing sequential attempt number per worker per assessment title
- [ ] **HIST-03**: The upgraded History tab displays columns: Nama Pekerja, NIP, Assessment Title, Attempt #, Score, Pass/Fail, Tanggal — showing both archived attempts and current completed sessions

## Future Requirements

*(None captured yet)*

## Out of Scope

| Feature | Reason |
|---------|--------|
| ForceClose creates attempt history | ForceClose already sets final Completed state; only Reset flow needs archiving |
| Worker-visible attempt history | Workers see their current Records; attempt history is HC/Admin audit view |
| Analytics/charts on attempt data | User explicitly deferred — table view only for this milestone |
| Notifications | Deferred — no server connection yet |
| Certificate tab | Deferred — descoped from this milestone |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| HIST-01 | Phase 46 | Pending |
| HIST-02 | Phase 46 | Pending |
| HIST-03 | Phase 46 | Pending |

**Coverage:**
- v2.2 requirements: 3 total
- Mapped to phases: 3
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-26*
*Last updated: 2026-02-26 — traceability confirmed after roadmap creation*
