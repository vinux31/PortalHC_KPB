# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- ✅ **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–218 (shipped 2026-03-21)
- 🚧 **v7.12 Struktur Organisasi CRUD** - Phases 219–222 (in progress)

## Phases

<details>
<summary>✅ v1.0–v7.7 (Phases 1–204) - SHIPPED 2026-03-19</summary>

43 milestones shipped. See MILESTONES.md for full history.

</details>

<details>
<summary>✅ v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu (Phases 205–207) - SHIPPED 2026-03-20</summary>

- [x] Phase 205: Halaman Gabungan KKJ & Alignment (1/1 plans) — completed 2026-03-20
- [x] Phase 206: Update CMP Hub & Backward Compat (1/1 plans) — completed 2026-03-20
- [x] Phase 207: Perbaikan Desain Tabel DokumenKkj (1/1 plans) — completed 2026-03-20

</details>

<details>
<summary>✅ v7.9 Renewal Certificate Grouped View (Phases 208–209) - SHIPPED 2026-03-20</summary>

- [x] Phase 208: Grouped View Structure (1/1 plans) — completed 2026-03-20
- [x] Phase 209: Bulk Renew & Filter Compatibility (1/1 plans) — completed 2026-03-20

</details>

<details>
<summary>✅ v7.10 RenewalCertificate Bug Fixes & Enhancement (Phases 210–212) - SHIPPED 2026-03-21</summary>

- [x] Phase 210: Critical Renewal Chain Fixes (2/2 plans) — completed 2026-03-21
- [x] Phase 211: Data & Display Fixes (1/1 plans) — completed 2026-03-21
- [x] Phase 212: Tipe Filter, Renewal Flow, AddTraining Renewal (2/2 plans) — completed 2026-03-21

</details>

<details>
<summary>✅ v7.11 CMP Records Bug Fixes & Enhancement (Phases 213–218) - SHIPPED 2026-03-21</summary>

- [x] Phase 213: Filter & Status Fixes (1/1 plans) — completed 2026-03-21
- [x] Phase 214: SubCategory Model + CRUD (2/2 plans) — completed 2026-03-21
- [x] Phase 215: Team View Filter Enhancement (1/1 plans) — completed 2026-03-21
- [x] Phase 216: Export Fixes & Display Enhancement (0/? plans) — skipped/deferred
- [x] Phase 217: Fix Category Dropdown RecordsTeam (1/1 plans) — completed 2026-03-21
- [x] Phase 218: RecordsWorkerDetail Redesign & ImportTraining Update (2/2 plans) — completed 2026-03-21

</details>

### 🚧 v7.12 Struktur Organisasi CRUD (In Progress)

**Milestone Goal:** Mengganti static class OrganizationStructure dengan database-driven CRUD — Admin dapat mengelola struktur organisasi (Bagian dan Unit) secara dinamis, dan seluruh dropdown Bagian/Unit di portal mengambil data dari database.

- [x] **Phase 219: DB Model & Migration** - Entity OrganizationUnit, migrasi data 4 Bagian/19 Unit, konsolidasi KkjBagian (completed 2026-03-21)
- [x] **Phase 220: CRUD Page Kelola Data** - Halaman Struktur Organisasi di Kelola Data: indented table, tambah/edit/pindah/hapus/reorder node (completed 2026-03-21)
- [x] **Phase 221: Integrasi Codebase** - Semua controller dan view ganti ke OrganizationUnit — filter dropdown, cascade, worker create/edit, DokumenKkj, ProtonData (completed 2026-03-21)
- [ ] **Phase 222: Cleanup & Finalisasi** - Hapus static OrganizationStructure.cs, update seed data, ImportWorkers validasi terhadap database

## Phase Details

### Phase 213: Filter & Status Fixes
**Goal**: Filter Category+Status di Team View bekerja akurat dan konsisten dengan logika status yang digunakan di personal view
**Depends on**: Phase 212
**Requirements**: FLT-01, FLT-02, FLT-03
**Success Criteria** (what must be TRUE):
  1. Memilih kategori tertentu di Team View lalu filter by status hanya menampilkan worker yang statusnya sesuai untuk kategori tersebut (bukan status global semua kategori)
  2. Jumlah training "Completed" di Team View konsisten dengan jumlah di personal view — training berstatus "Permanent" ikut dihitung sebagai completed
  3. Search by NIP di Team View bekerja benar — NIP yang mengandung huruf kapital maupun huruf kecil sama-sama dapat ditemukan
**Plans**: 1 plan
Plans:
- [x] 213-01-PLAN.md — Fix 3 filter bugs: Category+Status per-kategori, Permanent count, NIP lowercase

### Phase 214: SubCategory Model + CRUD
**Goal**: SubKategori tersedia sebagai field di TrainingRecord, dropdown Kategori dan SubKategori di AddTraining/EditTraining mengambil data dari AssessmentCategories, dan ImportTraining mendukung kolom SubKategori
**Depends on**: Phase 213
**Requirements**: MDL-01
**Success Criteria** (what must be TRUE):
  1. Database memiliki kolom SubKategori di tabel TrainingRecord — migrasi berhasil diaplikasikan tanpa error
  2. Dropdown Kategori di AddTraining/EditTraining mengambil data dari AssessmentCategories (bukan hardcode) — sinkron dengan ManageCategories
  3. Dropdown SubKategori muncul di AddTraining/EditTraining, dependent pada Kategori — hanya menampilkan child categories dari parent yang dipilih
  4. SubKategori di-disable saat Kategori belum dipilih
  5. ImportTraining mendukung kolom SubKategori (opsional) — data tersimpan di TrainingRecord
**Plans**: 2 plans
Plans:
- [x] 214-01-PLAN.md — Model + Migration + ViewModel + Controller backend
- [x] 214-02-PLAN.md — View updates: dynamic dropdowns + ImportTraining docs

### Phase 215: Team View Filter Enhancement
**Goal**: Assessment records masuk ke data filterable di Team View dan dropdown Sub Category tersedia sebagai filter dependent
**Depends on**: Phase 214
**Requirements**: FLT-04
**Success Criteria** (what must be TRUE):
  1. Filter Category di Team View memfilter worker berdasarkan training DAN assessment records (bukan hanya training)
  2. Dropdown filter Sub Category muncul di Team View Row 1 setelah Category, dependent pada Category yang dipilih
  3. Sub Category di-disable saat Category belum dipilih
  4. Memilih Sub Category memfilter daftar worker sesuai nilai SubKategori yang dipilih (training + assessment)
**Plans**: 1 plan
Plans:
- [x] 215-01-PLAN.md — Backend AssessmentSessions + Frontend SubCategory dropdown + filter JS

### Phase 216: Export Fixes & Display Enhancement
**Goal**: Export team dan personal menghasilkan data yang lengkap dan konsisten, serta badge IsExpiringSoon tampil di My Records untuk training yang akan segera expired
**Depends on**: Phase 215
**Requirements**: EXP-01, EXP-02, EXP-03, DSP-01
**Success Criteria** (what must be TRUE):
  1. Export team training menghasilkan file Excel dengan kolom Kategori, Status, ValidUntil, Kota, dan NomorSertifikat — sejajar dengan personal export
  2. Filter category yang aktif di Team View ikut diterapkan saat klik Export — hasil export hanya berisi data kategori yang dipilih
  3. Export assessment (personal maupun team) menghasilkan kolom Kategori yang terisi
  4. My Records menampilkan badge kuning "Akan Expired" untuk training yang ValidUntil-nya dalam 30 hari ke depan, bukan hanya badge merah "Expired"
Plans:
- [ ] (belum dibuat)

### Phase 217: Fix Category Dropdown RecordsTeam
**Goal**: Dropdown Category di RecordsTeam mengambil data dari tabel master AssessmentCategories (sinkron dengan ManageCategories), bukan dari union string records
**Depends on**: Phase 215
**Requirements**: CAT-01
**Success Criteria** (what must be TRUE):
  1. Dropdown Category di RecordsTeam menampilkan kategori yang sama persis dengan halaman Admin/ManageCategories
  2. Filter Category, Sub Category, Status, dan Export tetap berfungsi setelah perubahan data source
**Plans**: 1 plan
Plans:
- [x] 217-01-PLAN.md — Ganti dropdown source dari union strings ke master AssessmentCategories JSON

### Phase 218: RecordsWorkerDetail Redesign & ImportTraining Update
**Goal**: Redesign tabel RecordsWorkerDetail — hapus kolom Score dan Sertifikat, tambah kolom Kategori/SubKategori dan kolom Action (Detail + Download Sertifikat), tambah filter SubCategory cascade, dan update ImportTraining form/logic sesuai perubahan model
**Depends on**: Phase 217
**Requirements**: RWD-01, RWD-02, RWD-03, IMP-01
**Success Criteria** (what must be TRUE):
  1. Tabel RecordsWorkerDetail memiliki kolom Kategori dan Sub Kategori — kolom Score dihapus
  2. Kolom Sertifikat diganti kolom Action berisi 2 tombol: Detail dan Download Sertifikat
  3. Filter Sub Category cascade tersedia di RecordsWorkerDetail, dependent pada Kategori
  4. Halaman ImportTraining form dan logic ter-update sesuai perubahan model (urutan kolom diperbaiki)
**Plans**: 2 plans
Plans:
- [x] 218-01-PLAN.md — Redesign tabel RecordsWorkerDetail: 7 kolom baru, modal detail, cascade filter SubCategory
- [x] 218-02-PLAN.md — Update ImportTraining: template 12 kolom, import logic, format notes kedua view

### Phase 219: DB Model & Migration
**Goal**: Entity OrganizationUnit tersedia di database dengan data yang dimigrasikan dari static class, dan KkjBagian dikonsolidasikan ke OrganizationUnit
**Depends on**: Phase 218
**Requirements**: DB-01, DB-02, DB-03
**Success Criteria** (what must be TRUE):
  1. Tabel OrganizationUnits ada di database dengan kolom Id, Name, ParentId, Level, DisplayOrder, IsActive — migrasi berhasil tanpa error
  2. 4 Bagian dan 19 Unit dari static OrganizationStructure.cs tersedia sebagai rows di tabel OrganizationUnits dengan hierarki parent-child yang benar
  3. KkjFile dan CpdpFile memiliki FK OrganizationUnitId (menggantikan BagianId) — entitas KkjBagian dihapus dari codebase
  4. Aplikasi dapat di-seed ulang (dotnet run dengan SeedData) tanpa error
**Plans**: 1 plan
Plans:
- [x] 219-01-PLAN.md — Model OrganizationUnit, migration seed+remap, drop KkjBagian

### Phase 220: CRUD Page Kelola Data
**Goal**: Admin dapat mengelola struktur organisasi (tambah/edit/pindah/hapus/reorder node) melalui halaman Struktur Organisasi di Kelola Data
**Depends on**: Phase 219
**Requirements**: CRUD-01, CRUD-02, CRUD-03, CRUD-04, CRUD-05, CRUD-06
**Success Criteria** (what must be TRUE):
  1. Halaman Struktur Organisasi tersedia di Kelola Data Section A — menampilkan indented table dengan node Bagian dan Unit
  2. Admin dapat menambah node baru di level manapun (root, bagian, unit) — node langsung tampil di tabel
  3. Admin dapat mengedit nama node — perubahan tersimpan dan tampil tanpa reload penuh
  4. Admin dapat memindahkan node ke parent lain — children ikut pindah, sistem menolak circular reference
  5. Admin dapat soft-delete node — sistem memblok delete jika node punya children aktif atau user ter-assign, konfirmasi ditampilkan
  6. Admin dapat mengubah urutan node dalam parent yang sama menggunakan reorder
**Plans**: 2 plans
Plans:
- [x] 220-01-PLAN.md — Controller actions + card Kelola Data Index
- [x] 220-02-PLAN.md — View ManageOrganization.cshtml collapsible tree table

### Phase 221: Integrasi Codebase
**Goal**: Seluruh dropdown Bagian/Unit, cascade filter, validasi worker, dan grouping DokumenKkj/ProtonData di portal mengambil data dari tabel OrganizationUnits
**Depends on**: Phase 220
**Requirements**: INT-01, INT-02, INT-03, INT-04, INT-05, INT-06
**Success Criteria** (what must be TRUE):
  1. Semua dropdown filter Bagian dan Unit di seluruh portal (Admin, CMP, CDP, ProtonData) menampilkan data dari database OrganizationUnits — bukan dari static class
  2. Cascade dropdown Bagian → Unit tetap berfungsi dengan data dari database — memilih Bagian memfilter daftar Unit yang relevan
  3. Form create/edit worker memvalidasi Section/Unit terhadap OrganizationUnit yang ada di database
  4. Role-based section locking untuk L4/L5 tetap berfungsi setelah integrasi
  5. DokumenKkj menampilkan grouping berdasarkan OrganizationUnit (Bagian) dari database
  6. ProtonKompetensi dan CoachingGuidanceFile menggunakan Bagian/Unit dari OrganizationUnit — data tersinkron
**Plans**: 2 plans
Plans:
- [x] 221-01-PLAN.md — DbContext helpers + AdminController + RecordsTeam integrasi
- [x] 221-02-PLAN.md — CDPController + ProtonDataController + views integrasi

### Phase 222: Cleanup & Finalisasi
**Goal**: Static class OrganizationStructure.cs dihapus, seed data menggunakan OrganizationUnit, dan ImportWorkers memvalidasi Section/Unit terhadap database
**Depends on**: Phase 221
**Requirements**: INT-07, CLN-01, CLN-02
**Success Criteria** (what must be TRUE):
  1. File OrganizationStructure.cs tidak ada di codebase — tidak ada kompilasi error atau warning yang tersisa
  2. SeedData menggunakan OrganizationUnit dari database (bukan static list) — seed dapat dijalankan ulang dari clean state
  3. ImportWorkers menolak baris dengan Section/Unit yang tidak dikenal di OrganizationUnit — pesan error menyebut nama yang tidak valid
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 205. Halaman Gabungan KKJ & Alignment | v7.8 | 1/1 | Complete | 2026-03-20 |
| 206. Update CMP Hub & Backward Compat | v7.8 | 1/1 | Complete | 2026-03-20 |
| 207. Perbaikan Desain Tabel DokumenKkj | v7.8 | 1/1 | Complete | 2026-03-20 |
| 208. Grouped View Structure | v7.9 | 1/1 | Complete | 2026-03-20 |
| 209. Bulk Renew & Filter Compatibility | v7.9 | 1/1 | Complete | 2026-03-20 |
| 210. Critical Renewal Chain Fixes | v7.10 | 2/2 | Complete | 2026-03-21 |
| 211. Data & Display Fixes | v7.10 | 1/1 | Complete | 2026-03-21 |
| 212. Tipe Filter, Renewal Flow, AddTraining Renewal | v7.10 | 2/2 | Complete | 2026-03-21 |
| 213. Filter & Status Fixes | v7.11 | 1/1 | Complete | 2026-03-21 |
| 214. SubCategory Model + CRUD | v7.11 | 2/2 | Complete | 2026-03-21 |
| 215. Team View Filter Enhancement | v7.11 | 1/1 | Complete | 2026-03-21 |
| 216. Export Fixes & Display Enhancement | v7.11 | 0/? | Not started | - |
| 217. Fix Category Dropdown RecordsTeam | v7.11 | 1/1 | Complete | 2026-03-21 |
| 218. RecordsWorkerDetail Redesign & ImportTraining Update | v7.11 | 2/2 | Complete | 2026-03-21 |
| 219. DB Model & Migration | v7.12 | 1/1 | Complete    | 2026-03-21 |
| 220. CRUD Page Kelola Data | v7.12 | 2/2 | Complete    | 2026-03-21 |
| 221. Integrasi Codebase | v7.12 | 2/2 | Complete   | 2026-03-21 |
| 222. Cleanup & Finalisasi | v7.12 | 0/? | Not started | - |
