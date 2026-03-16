# Requirements: Portal HC KPB

**Defined:** 2026-03-16
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.0 Requirements

### Terminology

- [x] **TERM-01**: Assessment Results page shows "Analisis Elemen Teknis" as section title (was "Analisis Sub Kompetensi")
- [x] **TERM-02**: Assessment Results table header shows "Elemen Teknis" (was "Sub Kompetensi")
- [x] **TERM-03**: Import template Excel header shows "Elemen Teknis" (was "Sub Kompetensi")
- [x] **TERM-04**: Import template example row shows "Elemen Teknis x.x" (was "Sub Kompetensi x.x")
- [x] **TERM-05**: Import template help text shows "Kolom Elemen Teknis" (was "Kolom Sub Kompetensi")
- [x] **TERM-06**: Import page hint shows "Elemen Teknis (opsional)" (was "Sub Kompetensi (opsional)")
- [x] **TERM-07**: Cross-package warning message shows "Elemen Teknis" (was "Sub Kompetensi")

## v7.1 Requirements

### Export Data

- [x] **EXP-01**: User dapat export riwayat pelatihan pribadi (Records) ke Excel
- [x] **EXP-02**: Atasan/HC/Admin dapat export riwayat pelatihan tim (RecordsTeam) ke Excel
- [x] **EXP-03**: Admin/HC dapat export AuditLog ke Excel dengan filter tanggal
- [ ] **EXP-04**: Admin/HC dapat export data Silabus Proton ke Excel
- [ ] **EXP-05**: Coach/HC/Admin dapat export data Histori Proton ke Excel

### Import Data

- [x] **IMP-01**: Admin/HC dapat download template Excel untuk CoachCoacheeMapping
- [x] **IMP-02**: Admin/HC dapat import CoachCoacheeMapping dari file Excel
- [ ] **IMP-03**: Admin/HC dapat download template Excel untuk Silabus Proton
- [ ] **IMP-04**: Admin/HC dapat import Silabus Proton dari file Excel
- [ ] **IMP-05**: Admin/HC dapat download template Excel untuk Training record
- [ ] **IMP-06**: Admin/HC dapat import Training record dari file Excel

## Out of Scope

| Feature | Reason |
|---------|--------|
| Export PDF untuk Records/AuditLog | Excel sudah cukup untuk data tabular |
| Import Workers (sudah ada) | Sudah implemented di ManageWorkers |
| Import Questions (sudah ada) | Sudah implemented di ImportPackageQuestions |
| Export CoachCoacheeMapping (sudah ada) | Sudah implemented |
| Export Assessment Results (sudah ada) | Sudah implemented |
| Export CoachingProton Excel/PDF (sudah ada) | Sudah implemented |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TERM-01 | Phase 175 | Complete |
| TERM-02 | Phase 175 | Complete |
| TERM-03 | Phase 175 | Complete |
| TERM-04 | Phase 175 | Complete |
| TERM-05 | Phase 175 | Complete |
| TERM-06 | Phase 175 | Complete |
| TERM-07 | Phase 175 | Complete |
| EXP-01 | Phase 176 | Complete |
| EXP-02 | Phase 176 | Complete |
| EXP-03 | Phase 178 | Complete |
| EXP-04 | Phase 179 | Pending |
| EXP-05 | Phase 180 | Pending |
| IMP-01 | Phase 177 | Complete |
| IMP-02 | Phase 177 | Complete |
| IMP-03 | Phase 179 | Pending |
| IMP-04 | Phase 179 | Pending |
| IMP-05 | Phase 180 | Pending |
| IMP-06 | Phase 180 | Pending |

**Coverage:**
- v7.0 requirements: 7 total (all complete)
- v7.1 requirements: 11 total
- Mapped to phases: 11/11
- Unmapped: 0

---
*Requirements defined: 2026-03-16*
*Last updated: 2026-03-16 after v7.1 roadmap creation*
