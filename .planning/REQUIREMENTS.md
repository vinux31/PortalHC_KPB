# Requirements: Portal HC KPB

**Defined:** 2026-03-21
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.12 Requirements

Requirements for Milestone v7.12 — Struktur Organisasi CRUD.

### Model & Database

- [ ] **DB-01**: Entity OrganizationUnit (Id, Name, ParentId, Level, DisplayOrder, IsActive) — self-referential Adjacency List
- [ ] **DB-02**: Migrasi data dari static OrganizationStructure (4 Bagian, 19 Unit) ke tabel OrganizationUnits
- [ ] **DB-03**: KkjFile/CpdpFile FK BagianId → ganti ke OrganizationUnitId, hapus entity KkjBagian

### CRUD Page

- [ ] **CRUD-01**: Halaman Struktur Organisasi di Kelola Data Section A — indented table view
- [ ] **CRUD-02**: Tambah node baru di level manapun (root, bagian, unit, sub-unit)
- [ ] **CRUD-03**: Edit nama node
- [ ] **CRUD-04**: Pindahkan node ke parent lain (children ikut pindah, validasi anti-circular reference)
- [ ] **CRUD-05**: Soft-delete node (block jika punya children aktif atau user ter-assign)
- [ ] **CRUD-06**: Reorder node dalam parent yang sama

### Integrasi Codebase

- [ ] **INT-01**: Semua filter dropdown Bagian/Unit di seluruh app ambil dari database OrganizationUnits
- [ ] **INT-02**: Cascade dropdown tetap berfungsi — data dari database
- [ ] **INT-03**: ApplicationUser.Section/Unit validasi terhadap OrganizationUnit saat create/edit worker
- [ ] **INT-04**: Role-based section locking (L4/L5) tetap berfungsi
- [ ] **INT-05**: KkjFile/CpdpFile grouping di DokumenKkj menggunakan OrganizationUnit
- [ ] **INT-06**: ProtonKompetensi.Bagian/Unit dan CoachingGuidanceFile.Bagian/Unit tersinkron dengan OrganizationUnit
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
| DB-01 | — | Pending |
| DB-02 | — | Pending |
| DB-03 | — | Pending |
| CRUD-01 | — | Pending |
| CRUD-02 | — | Pending |
| CRUD-03 | — | Pending |
| CRUD-04 | — | Pending |
| CRUD-05 | — | Pending |
| CRUD-06 | — | Pending |
| INT-01 | — | Pending |
| INT-02 | — | Pending |
| INT-03 | — | Pending |
| INT-04 | — | Pending |
| INT-05 | — | Pending |
| INT-06 | — | Pending |
| INT-07 | — | Pending |
| CLN-01 | — | Pending |
| CLN-02 | — | Pending |

**Coverage:**
- v7.12 requirements: 18 total
- Mapped to phases: 0
- Unmapped: 18 ⚠️

---
*Requirements defined: 2026-03-21*
*Last updated: 2026-03-21 after milestone v7.12 definition*
