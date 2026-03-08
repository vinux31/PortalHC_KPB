# Requirements: Portal HC KPB v3.11

**Defined:** 2026-03-08
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.11 Requirements

Requirements for CoachCoacheeMapping Overhaul milestone.

### Data Model

- [ ] **MODEL-01**: CoachCoacheeMapping memiliki field `AssignmentSection` untuk section penugasan (bisa beda dari section pekerja)
- [ ] **MODEL-02**: CoachCoacheeMapping memiliki field `AssignmentUnit` untuk unit penugasan (bisa beda dari unit pekerja)
- [ ] **MODEL-03**: Database memiliki unique filtered index untuk one-active-coach-per-coachee constraint (`CoacheeId` WHERE `IsActive = 1`)

### CDP Access

- [ ] **ACCESS-01**: Deliverable page menggunakan CoachCoacheeMapping check (bukan section match) untuk validasi akses coach
- [ ] **ACCESS-02**: Coach bisa akses coachee dari section lain jika ada active mapping
- [ ] **ACCESS-03**: Semua CDP scope query (CoachingProton, HistoriProton, GetCoacheeDeliverables, batch submit) konsisten menggunakan mapping-based access

### Lifecycle

- [ ] **LIFE-01**: Deactivate mapping otomatis deactivate ProtonTrackAssignment terkait
- [ ] **LIFE-02**: Reactivate mapping menampilkan opsi untuk re-assign ProtonTrack

### UI

- [ ] **UI-01**: CoachCoacheeMapping page menampilkan kolom "Unit Penugasan" dan "Seksi Penugasan" terpisah dari unit/seksi asal pekerja
- [ ] **UI-02**: Assign modal memiliki field AssignmentSection dan AssignmentUnit
- [ ] **UI-03**: Export Excel menyertakan kolom unit/seksi penugasan vs unit/seksi asal

## Out of Scope

| Feature | Reason |
|---------|--------|
| Tahun field di mapping | Tahun sudah tracked via ProtonTrack.TahunKe di ProtonTrackAssignment |
| Coach self-assign UI | Violates admin-only mapping control |
| Auto-migrate existing mappings | Existing mappings retain null AssignmentSection/Unit (fallback to worker's own) |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| MODEL-01 | Phase 123 | Pending |
| MODEL-02 | Phase 123 | Pending |
| MODEL-03 | Phase 123 | Pending |
| ACCESS-01 | Phase 124 | Pending |
| ACCESS-02 | Phase 124 | Pending |
| ACCESS-03 | Phase 124 | Pending |
| LIFE-01 | Phase 124 | Pending |
| LIFE-02 | Phase 124 | Pending |
| UI-01 | Phase 125 | Pending |
| UI-02 | Phase 125 | Pending |
| UI-03 | Phase 125 | Pending |

**Coverage:**
- v3.11 requirements: 11 total
- Mapped to phases: 11
- Unmapped: 0

---
*Requirements defined: 2026-03-08*
*Last updated: 2026-03-08 after roadmap creation*
