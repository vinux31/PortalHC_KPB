# Requirements: Portal HC KPB v3.12

**Defined:** 2026-03-08
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.12 Requirements

Requirements for Progress Unit Scoping milestone. Fix progress data agar hanya berisi kompetensi sesuai unit penugasan coachee.

### Progress Creation

- [ ] **PROG-01**: `AutoCreateProgressForAssignment` hanya membuat progress untuk deliverable yang `ProtonKompetensi.Unit` == `CoachCoacheeMapping.AssignmentUnit`
- [ ] **PROG-02**: `SilabusSave` auto-sync hanya sync deliverable baru ke assignments yang Unit-nya match

### Data Migration

- [ ] **MIG-01**: Migration menghapus semua ProtonDeliverableProgress, CoachingSessions, dan DeliverableStatusHistory
- [ ] **MIG-02**: Migration me-recreate progress dari semua active ProtonTrackAssignment dengan filter Unit yang benar

### Reassignment

- [ ] **REASSIGN-01**: Saat AssignmentUnit berubah (edit mapping), progress lama dihapus dan dibuat baru sesuai unit baru

### Query

- [ ] **QUERY-01**: CoachingProton belt-and-suspenders filter tambah validasi `ProtonKompetensi.Unit` == assignment's Unit (defensive)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Filter by Bagian | Coachee hanya ditugaskan di unit dalam bagian sendiri — unit filter cukup |
| Tambah Unit field di ProtonTrackAssignment | Unit info sudah ada di CoachCoacheeMapping.AssignmentUnit |
| UI changes di CoachingProton | Hanya fix data source, tampilan tetap sama |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PROG-01 | Phase 128 | Pending |
| PROG-02 | Phase 129 | Pending |
| MIG-01 | Phase 128 | Pending |
| MIG-02 | Phase 128 | Pending |
| REASSIGN-01 | Phase 129 | Pending |
| QUERY-01 | Phase 129 | Pending |

**Coverage:**
- v3.12 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0

---
*Requirements defined: 2026-03-08*
*Last updated: 2026-03-08 after roadmap creation*
