# Requirements: Portal HC KPB

**Defined:** 2026-02-24
**Milestone:** v2.0 Assessment Management & Training History
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v2.0 Requirements

### Assessment Management (ASSESS)

- [ ] **ASSESS-01**: HC can close an active assessment group early from AssessmentMonitoringDetail — clicking "Tutup Lebih Awal" sets ExamWindowCloseDate to now, Open sessions are blocked from starting, and InProgress sessions are force-completed with score calculated from their actual submitted answers (not Score=0)
- [ ] **ASSESS-02**: The Monitoring tab automatically hides assessment groups 7 days after their ExamWindowCloseDate (falls back to Schedule date if ExamWindowCloseDate is null)
- [ ] **ASSESS-03**: The Management tab applies the same 7-day auto-hide filter as ASSESS-02 — assessment groups disappear from Management tab 7 days after close

### Training History (HIST)

- [ ] **HIST-01**: The RecordsWorkerList page has a second "History" tab showing all workers' manual training records and assessment online completions combined in a single table, sorted by tanggal mulai descending

## Future Requirements (deferred)

### Notifications (NOTIF)
- **NOTIF-01**: HC receives in-app notification when all workers in an assessment group have submitted
- **NOTIF-02**: HC receives in-app notification when ExamWindowCloseDate expires
- **NOTIF-03**: Worker receives in-app notification after submitting — confirms result is recorded

## Out of Scope

| Feature | Reason |
|---------|--------|
| Email notifications | No SMTP infrastructure; deferred to dedicated Notification milestone |
| Per-worker close early | ASSESS-01 is group-level only; individual ForceClose already exists |
| Drag-and-drop reorder in catalog | Removed in v1.9 — nested table structure incompatible with SortableJS |
| BP Module | Talent profiles, eligibility, point system — postponed indefinitely |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ASSESS-01 | Phase 39 | Pending |
| ASSESS-02 | Phase 38 | Pending |
| ASSESS-03 | Phase 38 | Pending |
| HIST-01 | Phase 40 | Pending |

**Coverage:**
- v2.0 requirements: 4 total
- Mapped to phases: 4
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-24*
*Last updated: 2026-02-24 after milestone v2.0 kickoff*
