# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- 🚧 **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–206 (in progress)

## Phases

<details>
<summary>✅ v1.0–v7.7 (Phases 1–204) - SHIPPED 2026-03-19</summary>

43 milestones shipped. See MILESTONES.md for full history.

</details>

### 🚧 v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu (In Progress)

**Milestone Goal:** Gabung 2 menu terpisah di CMP Index (KKJ dan Alignment KKJ & IDP) menjadi 1 halaman dengan 2 tab, menampilkan semua bagian langsung tanpa dropdown.

## Phase Details

### Phase 205: Halaman Gabungan KKJ & Alignment
**Goal**: Pengguna dapat melihat dokumen KKJ dan Alignment KKJ/IDP dalam satu halaman dengan 2 tab, tiap tab menampilkan semua bagian beserta file-nya sesuai role
**Depends on**: Phase 204
**Requirements**: CMP-02, CMP-03, CMP-04, CMP-05
**Success Criteria** (what must be TRUE):
  1. Pengguna membuka halaman gabungan dan melihat 2 tab: "Kebutuhan Kompetensi Jabatan" dan "Alignment KKJ & IDP"
  2. Tab KKJ menampilkan semua bagian beserta daftar file-nya langsung tanpa perlu memilih bagian dari dropdown
  3. Tab Alignment menampilkan semua bagian beserta daftar file-nya langsung tanpa dropdown
  4. Pengguna L5-L6 hanya melihat data bagiannya sendiri di kedua tab; pengguna L1-L4 melihat semua bagian
**Plans**: 1 plan

Plans:
- [ ] 205-01-PLAN.md — Action DokumenKkj + view gabungan 2 tab

### Phase 206: Update CMP Hub & Backward Compat
**Goal**: Card di CMP Index digabung menjadi 1 dan action/view lama dihapus
**Depends on**: Phase 205
**Requirements**: CMP-01, CMP-06
**Success Criteria** (what must be TRUE):
  1. CMP Index menampilkan 1 card "Dokumen KKJ & Alignment KKJ/IDP" (bukan 2 card terpisah)
  2. Action `/CMP/Kkj` dan `/CMP/Mapping` dihapus (tidak redirect, langsung 404)
  3. File view lama Kkj.cshtml dan Mapping.cshtml dihapus
**Plans**: 1 plan

Plans:
- [ ] 206-01-PLAN.md — Gabung card CMP Index + hapus action/view lama

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 205. Halaman Gabungan KKJ & Alignment | 1/1 | Complete   | 2026-03-20 | - |
| 206. Update CMP Hub & Backward Compat | v7.8 | 0/1 | Not started | - |
