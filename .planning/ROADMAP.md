# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- ✅ **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–218 (shipped 2026-03-21)
- ✅ **v7.12 Struktur Organisasi CRUD** - Phases 219–222 (shipped 2026-03-21)
- ✅ **v8.0 Assessment Integrity & Analytics** - Phases 223–227 (shipped 2026-03-22)
- ✅ **v8.1 Renewal & Assessment Ecosystem Audit** - Phases 228–232 (shipped 2026-03-22)
- ✅ **v8.2 Proton Coaching Ecosystem Audit** - Phases 233–238 (shipped 2026-03-23)
- 🚧 **v8.3 Date Range Filter Team View Records** - Phase 239 (in progress)

---

<details>
<summary>✅ v1.0–v8.2 (Phases 1–238) - SHIPPED</summary>

All prior milestones shipped. See MILESTONES.md for full detail.

Last completed phase: 238 (v8.2 — Gap Closure UI Wiring)

</details>

---

### 🚧 v8.3 Date Range Filter Team View Records (In Progress)

**Milestone Goal:** Ganti search nama dengan date range filter pada Team View di CMP/Records agar user dapat memfilter workers berdasarkan rentang tanggal records mereka.

## Phases

- [ ] **Phase 239: Date Range Filter & Export** - Hapus search nama, tambah 2 input date, filter tabel dan count, update export

## Phase Details

### Phase 239: Date Range Filter & Export
**Goal**: User dapat memfilter Team View berdasarkan rentang tanggal records — tabel hanya menampilkan workers yang punya records dalam rentang tersebut, count ikut ter-filter, dan export meneruskan parameter date range ke server
**Depends on**: Nothing (single-phase milestone)
**Requirements**: FILT-01, FILT-02, FILT-03, FILT-04, FILT-05, FILT-06, EXP-01, EXP-02
**Success Criteria** (what must be TRUE):
  1. User melihat 2 input date (Tanggal Awal & Tanggal Akhir) di filter bar Team View, menggantikan textbox Search Nama/NIP yang sebelumnya ada
  2. Saat tanggal diisi, tabel hanya menampilkan workers yang punya minimal 1 record (assessment atau training) dalam rentang tersebut; saat dikosongkan semua workers tampil kembali
  3. Count kolom Assessment dan Training di baris setiap worker hanya menghitung records yang jatuh di dalam rentang tanggal yang dipilih
  4. Filter date bekerja independen bersama filter Bagian, Unit, Category, Sub Category, dan Status — kombinasi apapun menghasilkan filter yang benar
  5. Tombol Reset mengosongkan date range bersama semua filter lain; export Assessment dan export Training meneruskan parameter date range ke server sehingga hasil export konsisten dengan tampilan tabel
**Plans:** 2 plans

Plans:
- [ ] 239-01-PLAN.md — Backend: service date filter + controller AJAX partial + export params
- [ ] 239-02-PLAN.md — Frontend: UI date inputs + JS AJAX refactor + reset/export wiring

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 239. Date Range Filter & Export | v8.3 | 0/2 | Not started | - |
