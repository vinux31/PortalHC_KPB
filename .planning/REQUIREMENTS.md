# Requirements: Portal HC KPB v3.9

**Defined:** 2026-03-07
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.9 Requirements

### Status Tab

- [ ] **STAT-01**: Admin/HC dapat melihat tab Status (posisi pertama) di ProtonData/Index
- [ ] **STAT-02**: Tab Status menampilkan tree checklist Bagian > Unit > Track yang bisa di-expand/collapse
- [ ] **STAT-03**: Setiap node Track memiliki kolom Silabus dengan centang hijau jika ada minimal 1 Kompetensi aktif
- [ ] **STAT-04**: Setiap node Track memiliki kolom Guidance dengan centang hijau jika ada minimal 1 file guidance

### Silabus Target Column

- [x] **TGT-01**: Tabel Silabus menampilkan kolom Target (text) setelah SubKompetensi dan sebelum Deliverable
- [x] **TGT-02**: Kolom Target bisa diisi di edit mode dan tersimpan via SilabusSave

### Hard Delete

- [ ] **DEL-01**: Admin/HC dapat hard delete Kompetensi dari view mode dengan tombol Delete
- [ ] **DEL-02**: Delete diblokir jika ada ProtonDeliverableProgress records yang mereferensi deliverable di bawah Kompetensi tersebut
- [ ] **DEL-03**: Konfirmasi dialog menampilkan jumlah SubKompetensi dan Deliverable yang akan ikut terhapus

### Consumer Audit

- [ ] **AUD-01**: Semua consumer tabel Silabus (PlanIdp, CoachingProton, dll) tetap berfungsi setelah perubahan schema

## Out of Scope

| Feature | Reason |
|---------|--------|
| Coaching Guidance CRUD changes | Existing upload/replace/delete already works |
| Override tab changes | Not in scope |
| Other pages (PlanIdp, CoachingProton) | Only ProtonData/Index |
| Silabus edit mode restructuring | Only adding Target column to existing edit flow |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| STAT-01 | Phase 114 | Pending |
| STAT-02 | Phase 114 | Pending |
| STAT-03 | Phase 114 | Pending |
| STAT-04 | Phase 114 | Pending |
| TGT-01 | Phase 113 | Complete |
| TGT-02 | Phase 113 | Complete |
| DEL-01 | Phase 115 | Pending |
| DEL-02 | Phase 115 | Pending |
| DEL-03 | Phase 115 | Pending |
| AUD-01 | Phase 115 | Pending |

**Coverage:**
- v3.9 requirements: 10 total
- Mapped to phases: 10
- Unmapped: 0

---
*Requirements defined: 2026-03-07*
