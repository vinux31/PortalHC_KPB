# Requirements: Portal HC KPB

**Defined:** 2026-03-21
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.11 Requirements

Requirements for CMP Records Bug Fixes & Enhancement.

### Filter Fixes

- [x] **FLT-01**: Team View Category+Status filter menghitung status per-kategori yang dipilih (bukan global semua kategori) — client-side JS logic harus mencocokkan status dengan kategori yang difilter
- [x] **FLT-02**: hasTraining (view) dan CompletedTrainings (service) menggunakan set status yang sama — tambah "Permanent" ke CompletedTrainings count di WorkerDataService
- [x] **FLT-03**: NIP data attribute di Team View di-lowercase agar konsisten dengan search filter logic
- [ ] **FLT-04**: Tambah dropdown filter Sub Category di Team View, dependent pada category yang dipilih

### Data Model

- [ ] **MDL-01**: Tambah field SubKategori di TrainingRecord model dengan migrasi database

### Export Fixes

- [ ] **EXP-01**: ExportRecordsTeamAssessment menerima parameter category dan menggunakannya untuk filter (saat ini pass null)
- [ ] **EXP-02**: Team training export tambah kolom Kategori, Status, ValidUntil, Kota, NomorSertifikat (sejajar dengan personal export)
- [ ] **EXP-03**: Assessment export (personal & team) tambah kolom Kategori

### Display Enhancement

- [ ] **DSP-01**: My Records menampilkan badge IsExpiringSoon (warning 30 hari) untuk training yang akan expired — saat ini hanya tampil badge Expired

## Out of Scope

| Feature | Reason |
|---------|--------|
| Server-side filtering for Team View | Client-side filtering cukup untuk jumlah worker saat ini |
| Status filter default edge case (LOW) | Tidak ada path untuk value kosong di dropdown |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FLT-01 | Phase 213 | Complete |
| FLT-02 | Phase 213 | Complete |
| FLT-03 | Phase 213 | Complete |
| FLT-04 | Phase 214 | Pending |
| MDL-01 | Phase 214 | Pending |
| EXP-01 | Phase 215 | Pending |
| EXP-02 | Phase 215 | Pending |
| EXP-03 | Phase 215 | Pending |
| DSP-01 | Phase 215 | Pending |

**Coverage:**
- v7.11 requirements: 9 total
- Mapped to phases: 9
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-21*
*Last updated: 2026-03-21 — traceability mapped after roadmap creation*
