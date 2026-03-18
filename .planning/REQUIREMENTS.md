# Requirements: Portal HC KPB — v7.6 Code Deduplication & Shared Services

**Defined:** 2026-03-18
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.6 Requirements

Requirements for milestone v7.6 Code Deduplication & Shared Services. Each maps to roadmap phases.

### Shared Services

- [ ] **SVC-01**: GetUnifiedRecords() di-extract dari AdminController + CMPController ke shared service class
- [ ] **SVC-02**: GetAllWorkersHistory() di-extract ke shared service (duplikat ~80 baris di Admin + CMP)
- [ ] **SVC-03**: GetWorkersInSection() di-extract ke shared service (duplikat ~100 baris di Admin + CMP)
- [ ] **SVC-04**: NotifyIfGroupCompleted() di-extract ke shared service — logic divergence antara Admin (izinkan Cancelled) dan CMP (hanya Completed) diperbaiki jadi satu versi konsisten
- [ ] **SVC-05**: Common Excel export helper di-extract dari 4 controller (Admin, CMP, CDP, ProtonData) — shared header setup, data population, dan formatting

### CRUD Consolidation

- [ ] **CRUD-01**: Training Record edit/hapus di CMPController dihapus — Admin/EditTraining dan Admin/DeleteTraining jadi satu-satunya entry point
- [ ] **CRUD-02**: Training Import dipindahkan atau di-link dari Admin (saat ini hanya bisa diakses dari CMP/ImportTraining)
- [ ] **CRUD-03**: Worker Detail di Admin (/Admin/WorkerDetail) dan CMP (/CMP/RecordsWorkerDetail) dibedakan tujuannya secara jelas — Admin fokus profil/edit data pekerja, CMP fokus rekaman training & assessment

### Code Patterns

- [ ] **PAT-01**: File upload logic untuk KKJ dan CPDP di AdminController di-extract ke FileUploadHelper class (validasi, safe filename, save file, audit log — 90% identik)
- [ ] **PAT-02**: Role-scoping enforcement logic yang berulang di CMPController (3+ tempat) di-extract ke private helper method
- [ ] **PAT-03**: Pagination logic yang berulang di 5+ tempat (Admin, CMP, CDP) di-extract ke helper atau extension method

## Future Requirements

### Deferred

- **FUT-01**: Certificate file validation helper (CMPController + CDPController) — low severity, defer ke milestone berikutnya
- **FUT-02**: TrainingRecord creation factory/mapper untuk import — low severity

## Out of Scope

| Feature | Reason |
|---------|--------|
| Menambah fitur baru | Milestone ini murni refactoring/cleanup |
| Mengubah UI/UX yang ada | Hanya perubahan internal, user tidak merasakan perbedaan |
| Database migration | Tidak ada perubahan schema |
| Menghapus Worker Detail salah satu | Keputusan: tetap dua, dibedakan tujuannya |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SVC-01 | TBD | Pending |
| SVC-02 | TBD | Pending |
| SVC-03 | TBD | Pending |
| SVC-04 | TBD | Pending |
| SVC-05 | TBD | Pending |
| CRUD-01 | TBD | Pending |
| CRUD-02 | TBD | Pending |
| CRUD-03 | TBD | Pending |
| PAT-01 | TBD | Pending |
| PAT-02 | TBD | Pending |
| PAT-03 | TBD | Pending |

**Coverage:**
- v7.6 requirements: 11 total
- Mapped to phases: 0
- Unmapped: 11 ⚠️

---
*Requirements defined: 2026-03-18*
*Last updated: 2026-03-18 after initial definition*
