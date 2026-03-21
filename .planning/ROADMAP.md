# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- 🚧 **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–217 (in progress)

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

### 🚧 v7.11 CMP Records Bug Fixes & Enhancement (In Progress)

**Milestone Goal:** Perbaikan filter bug di Team View, penambahan SubCategory model dan filter, perbaikan konsistensi export, dan penambahan badge IsExpiringSoon di My Records.

- [x] **Phase 213: Filter & Status Fixes** - Perbaiki 3 filter bug inti di Team View (completed 2026-03-21)
- [x] **Phase 214: SubCategory Model + CRUD** - Tambah kolom SubKategori di TrainingRecord, dropdown Kategori/SubKategori dari AssessmentCategories di AddTraining/EditTraining/ImportTraining (completed 2026-03-21)
- [x] **Phase 215: Team View Filter Enhancement** - Assessment records masuk data filterable, dropdown Sub Category dependent di Team View (completed 2026-03-21)
- [ ] **Phase 216: Export Fixes & Display Enhancement** - Sejajarkan team export dengan personal export dan tampilkan badge expiring soon
- [x] **Phase 217: Fix Category Dropdown RecordsTeam** - Dropdown Category dari master AssessmentCategories, bukan union string records (completed 2026-03-21)

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
| 213. Filter & Status Fixes | v7.11 | 1/1 | Complete    | 2026-03-21 |
| 214. SubCategory Model + CRUD | v7.11 | 2/2 | Complete    | 2026-03-21 |
| 215. Team View Filter Enhancement | v7.11 | 1/1 | Complete    | 2026-03-21 |
| 216. Export Fixes & Display Enhancement | v7.11 | 0/? | Not started | - |
| 217. Fix Category Dropdown RecordsTeam | v7.11 | 1/1 | Complete   | 2026-03-21 |
