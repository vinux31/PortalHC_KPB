# Requirements: Portal HC KPB

**Defined:** 2026-03-21
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.12 Requirements

Requirements for Milestone v7.12 — Struktur Organisasi CRUD.

### Model & Database

- [x] **DB-01**: Entity OrganizationUnit (Id, Name, ParentId, Level, DisplayOrder, IsActive) — self-referential Adjacency List
- [x] **DB-02**: Migrasi data dari static OrganizationStructure (4 Bagian, 19 Unit) ke tabel OrganizationUnits
- [x] **DB-03**: KkjFile/CpdpFile FK BagianId → ganti ke OrganizationUnitId, hapus entity KkjBagian

### CRUD Page

- [x] **CRUD-01**: Halaman Struktur Organisasi di Kelola Data Section A — indented table view
- [x] **CRUD-02**: Tambah node baru di level manapun (root, bagian, unit, sub-unit)
- [x] **CRUD-03**: Edit nama node
- [x] **CRUD-04**: Pindahkan node ke parent lain (children ikut pindah, validasi anti-circular reference)
- [x] **CRUD-05**: Soft-delete node (block jika punya children aktif atau user ter-assign)
- [x] **CRUD-06**: Reorder node dalam parent yang sama

### Integrasi Codebase

- [x] **INT-01**: Semua filter dropdown Bagian/Unit di seluruh app ambil dari database OrganizationUnits
- [x] **INT-02**: Cascade dropdown tetap berfungsi — data dari database
- [x] **INT-03**: ApplicationUser.Section/Unit validasi terhadap OrganizationUnit saat create/edit worker
- [x] **INT-04**: Role-based section locking (L4/L5) tetap berfungsi
- [x] **INT-05**: KkjFile/CpdpFile grouping di DokumenKkj menggunakan OrganizationUnit
- [x] **INT-06**: ProtonKompetensi.Bagian/Unit dan CoachingGuidanceFile.Bagian/Unit tersinkron dengan OrganizationUnit
- [ ] **INT-07**: Hapus static class OrganizationStructure.cs setelah semua referensi diganti

### Cleanup

- [ ] **CLN-01**: Seed data menggunakan OrganizationUnit
- [ ] **CLN-02**: ImportWorkers validasi Section/Unit terhadap OrganizationUnit database

## Out of Scope

| Feature | Reason |
|---------|--------|
| Drag-drop tree UI (jsTree) | Overkill untuk 4-5 level; indented table cukup. Bisa ditambah nanti |
| Org chart visual | Read-only visualization, bukan prioritas CRUD |
| Multi-unit user assignment | User tetap 1 Section + 1 Unit (existing pattern) |
| Full FK migration ApplicationUser → OrganizationUnit | Risiko tinggi, user bisa punya multiple units. Validasi string cukup |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DB-01 | Phase 219 | Complete |
| DB-02 | Phase 219 | Complete |
| DB-03 | Phase 219 | Complete |
| CRUD-01 | Phase 220 | Complete |
| CRUD-02 | Phase 220 | Complete |
| CRUD-03 | Phase 220 | Complete |
| CRUD-04 | Phase 220 | Complete |
| CRUD-05 | Phase 220 | Complete |
| CRUD-06 | Phase 220 | Complete |
| INT-01 | Phase 221 | Complete |
| INT-02 | Phase 221 | Complete |
| INT-03 | Phase 221 | Complete |
| INT-04 | Phase 221 | Complete |
| INT-05 | Phase 221 | Complete |
| INT-06 | Phase 221 | Complete |
| INT-07 | Phase 222 | Pending |
| CLN-01 | Phase 222 | Pending |
| CLN-02 | Phase 222 | Pending |

**Coverage:**
- v7.12 requirements: 18 total
- Mapped to phases: 18
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-21*
*Last updated: 2026-03-21 after roadmap v7.12 created*
