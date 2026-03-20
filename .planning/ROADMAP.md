# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- 🚧 **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (in progress)

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

### 🚧 v7.9 Renewal Certificate Grouped View (In Progress)

**Milestone Goal:** Redesign halaman RenewalCertificate dari flat table menjadi grouped-by-sertifikat dengan collapsible sections, badge count per group, dan bulk renew per group.

#### Phase 208: Grouped View Structure
**Goal**: Admin dapat melihat daftar renewal certificate yang dikelompokkan per nama sertifikat, bukan flat list per orang
**Depends on**: Nothing (first phase of milestone)
**Requirements**: GRP-01, GRP-02, GRP-03, GRP-04
**Success Criteria** (what must be TRUE):
  1. Halaman RenewalCertificate menampilkan data dalam group terpisah per judul sertifikat (bukan satu tabel panjang)
  2. Setiap group header menampilkan judul sertifikat, kategori/sub-kategori, dan badge count (N expired, N akan expired)
  3. Setiap group dapat di-collapse dan di-expand dengan klik pada header (default expanded)
  4. Tabel di dalam setiap group hanya menampilkan kolom: Checkbox, Nama, Valid Until, Status, Aksi
**Plans**: 1 plan

Plans:
- [ ] 208-01: Grouped view — ViewModel grouping + partial view redesign

#### Phase 209: Bulk Renew & Filter Compatibility
**Goal**: Admin dapat melakukan bulk renew per group sertifikat, dan semua filter existing tetap berfungsi pada tampilan grouped
**Depends on**: Phase 208
**Requirements**: BULK-01, BULK-02, FILT-01, FILT-02
**Success Criteria** (what must be TRUE):
  1. Admin dapat mencentang checkbox "select all" per group untuk memilih semua pekerja dalam satu sertifikat
  2. Tombol "Renew N Pekerja" muncul per group saat ada checkbox tercentang, dan menghilang saat tidak ada yang tercentang
  3. Filter Bagian/Unit/Kategori/Sub Kategori/Status menghasilkan tampilan grouped yang benar (group yang semua anggotanya terfilter keluar tidak muncul)
  4. Summary cards (Expired count, Akan Expired count) tetap tampil dan nilainya update sesuai filter aktif
**Plans**: TBD

Plans:
- [ ] 209-01: Bulk renew per group + filter compatibility

## Phase Details

### Phase 208: Grouped View Structure
**Goal**: Admin dapat melihat daftar renewal certificate yang dikelompokkan per nama sertifikat, bukan flat list per orang
**Depends on**: Nothing (first phase of milestone)
**Requirements**: GRP-01, GRP-02, GRP-03, GRP-04
**Success Criteria** (what must be TRUE):
  1. Halaman RenewalCertificate menampilkan data dalam group terpisah per judul sertifikat (bukan satu tabel panjang)
  2. Setiap group header menampilkan judul sertifikat, kategori/sub-kategori, dan badge count (N expired, N akan expired)
  3. Setiap group dapat di-collapse dan di-expand dengan klik pada header (default expanded)
  4. Tabel di dalam setiap group hanya menampilkan kolom: Checkbox, Nama, Valid Until, Status, Aksi
**Plans**: 1 plan

### Phase 209: Bulk Renew & Filter Compatibility
**Goal**: Admin dapat melakukan bulk renew per group sertifikat, dan semua filter existing tetap berfungsi pada tampilan grouped
**Depends on**: Phase 208
**Requirements**: BULK-01, BULK-02, FILT-01, FILT-02
**Success Criteria** (what must be TRUE):
  1. Admin dapat mencentang checkbox "select all" per group untuk memilih semua pekerja dalam satu sertifikat
  2. Tombol "Renew N Pekerja" muncul per group saat ada checkbox tercentang, dan menghilang saat tidak ada yang tercentang
  3. Filter Bagian/Unit/Kategori/Sub Kategori/Status menghasilkan tampilan grouped yang benar (group yang semua anggotanya terfilter keluar tidak muncul)
  4. Summary cards (Expired count, Akan Expired count) tetap tampil dan nilainya update sesuai filter aktif
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 205. Halaman Gabungan KKJ & Alignment | v7.8 | 1/1 | Complete | 2026-03-20 |
| 206. Update CMP Hub & Backward Compat | v7.8 | 1/1 | Complete | 2026-03-20 |
| 207. Perbaikan Desain Tabel DokumenKkj | v7.8 | 1/1 | Complete | 2026-03-20 |
| 208. Grouped View Structure | 1/1 | Complete   | 2026-03-20 | - |
| 209. Bulk Renew & Filter Compatibility | v7.9 | 0/1 | Not started | - |
