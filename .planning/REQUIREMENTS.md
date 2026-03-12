# Requirements: Portal HC KPB

**Defined:** 2026-03-12
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v4.1 Requirements

Requirements for Coaching Proton Deduplication milestone.

### Bug Fix

- [x] **FIX-01**: Add `DeactivatedAt` timestamp to ProtonTrackAssignment; Deactivate cascade records the timestamp; Reactivate only restores assignments where DeactivatedAt correlates with the mapping's deactivation
- [x] **FIX-02**: Assign flow checks for existing inactive assignments for same coachee+track and reuses them instead of creating duplicates

### Data Cleanup

- [x] **CLN-01**: One-time migration/seed cleanup deactivates duplicate active ProtonTrackAssignments per coachee+track (keeps latest by AssignedAt)

### Defensive Guard

- [x] **DEF-01**: CoachingProton query deduplicates by selecting only progress rows from the latest active assignment per coachee+track

### Assignment Removal

- [x] **RMV-01**: CoachCoacheeMapping page has "Hapus" button for deactivated mappings that permanently deletes the mapping, its ProtonTrackAssignments, and all associated ProtonDeliverableProgress rows with confirmation dialog
- [x] **RMV-02**: Remove action only available on deactivated (not active) mappings; action logged to AuditLog

## Out of Scope

| Feature | Reason |
|---------|--------|
| Adding FK from ProtonTrackAssignment to CoachCoacheeMapping | High migration risk for existing data; timestamp correlation sufficient |
| Rewriting Deactivate cascade to be mapping-scoped | Current behavior (deactivate all for coachee) is correct since a coachee has one active mapping |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FIX-01 | Phase 159 | Complete |
| FIX-02 | Phase 159 | Complete |
| CLN-01 | Phase 159 | Complete |
| DEF-01 | Phase 159 | Complete |
| RMV-01 | Phase 160 | Complete |
| RMV-02 | Phase 160 | Complete |

**Coverage:**
- v4.1 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-12*
*Last updated: 2026-03-12 — traceability mapped to phases 159-160*
