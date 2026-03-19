# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0 – v5.0** - Phases 1–172 (shipped 2026-03-16)
- ⚠️ **v6.0 Deployment Preparation** - Phases 173–174 (closed 2026-03-16, no work executed)
- ✅ **v7.0 Assessment Terminology Fix** - Phase 175 (shipped 2026-03-16)
- ✅ **v7.1 Export & Import Data** - Phases 176–180 (shipped 2026-03-16)
- ✅ **v7.2 PDF Evidence Report Enhancement** - Phase 181 (shipped 2026-03-17)
- ✅ **v7.2 (loose)** - Phase 182 (shipped 2026-03-17)
- ✅ **v7.3 Elemen Teknis Shuffle & Rename** - Phases 183–184 (shipped 2026-03-17)
- ✅ **v7.4 Certification Management** - Phases 185–190 (shipped 2026-03-18)
- ✅ **v7.5 Assessment Form Revamp & Certificate Enhancement** - Phases 190–195 (shipped 2026-03-18)
- ✅ **v7.6 Code Deduplication & Shared Services** - Phases 196–199 (shipped 2026-03-18)
- 🚧 **v7.7 Renewal Certificate & Certificate History** - Phases 200–204 (in progress)

## Phases

<details>
<summary>✅ v1.0–v5.0 (Phases 1–172) - SHIPPED 2026-03-16</summary>

Phases 1–172 shipped across milestones v1.0–v5.0. See MILESTONES.md for details.

</details>

<details>
<summary>⚠️ v6.0 Deployment Preparation (Phases 173–174) - CLOSED 2026-03-16, no work executed</summary>

Phases 173–174 defined but never executed. Deferred indefinitely.

</details>

<details>
<summary>✅ v7.0 Assessment Terminology Fix (Phase 175) - SHIPPED 2026-03-16</summary>

#### Phase 175: Terminology Rename

**Goal**: All user-facing assessment UI shows "Elemen Teknis" instead of "Sub Kompetensi"
**Requirements**: TERM-01, TERM-02, TERM-03, TERM-04, TERM-05, TERM-06, TERM-07
**Plans**: 1 plan (complete)

</details>

<details>
<summary>✅ v7.1 Export & Import Data (Phases 176–180) — SHIPPED 2026-03-16</summary>

- [x] Phase 176: Export Records & RecordsTeam (1/1 plans) — completed 2026-03-16
- [x] Phase 177: Import CoachCoacheeMapping (1/1 plans) — completed 2026-03-16
- [x] Phase 178: Export AuditLog (1/1 plans) — completed 2026-03-16
- [x] Phase 179: Export & Import Silabus Proton (1/1 plans) — completed 2026-03-16
- [x] Phase 180: Import Training & Export HistoriProton (1/1 plans) — completed 2026-03-16

</details>

<details>
<summary>✅ v7.2 PDF Evidence Report Enhancement (Phase 181) — SHIPPED 2026-03-17</summary>

#### Phase 181: PDF Header Coachee Info
**Goal**: The PDF Evidence Report header displays coachee identity (Nama, Unit, Track) above Tanggal Coaching
**Requirements**: PDF-01, PDF-02, PDF-03
**Plans**: 1 plan (complete)

</details>

<details>
<summary>✅ v7.2 (loose) — Phase 182 — SHIPPED 2026-03-17</summary>

#### Phase 182: CDP/CoachingProton Evidence Column Clarification

**Goal**: Fix Evidence column to derive display from Status field instead of EvidencePath
**Requirements**: None (loose phase)
**Plans**: 1 plan (complete)

</details>

<details>
<summary>✅ v7.3 Elemen Teknis Shuffle & Rename (Phases 183–184) — SHIPPED 2026-03-17</summary>

#### Phase 183: Internal Rename SubCompetency → ElemenTeknis
**Goal**: All internal C# code, DB column, and ViewModels use ElemenTeknis instead of SubCompetency
**Requirements**: RENAME-01, RENAME-02, RENAME-03
**Plans**: 1 plan (complete)

#### Phase 184: Shuffle Algorithm — Guaranteed Elemen Teknis Distribution
**Goal**: Cross-package and single-package shuffle guarantees at least one question per Elemen Teknis group, and reshuffles preserve that distribution
**Requirements**: SHUF-01, SHUF-02, SHUF-03
**Plans**: 3 plans (complete)

</details>

<details>
<summary>✅ v7.4 Certification Management (Phases 185–190) — SHIPPED 2026-03-18</summary>

- [x] Phase 185: ViewModel and Data Model Foundation (1/1 plans) — completed 2026-03-18
- [x] Phase 186: Role-Scoped Data Query Helper (1/1 plans) — completed 2026-03-18
- [x] Phase 187: Full-Page Controller Action and Static View (1/1 plans) — completed 2026-03-18
- [x] Phase 188: AJAX Filter Bar (1/1 plans) — completed 2026-03-18
- [x] Phase 189: Certificate Actions and Excel Export (1/1 plans) — completed 2026-03-18
- [x] Phase 190: CertificationManagement filter category/sub-category, role-based view (2/2 plans) — completed 2026-03-18

</details>

<details>
<summary>✅ v7.5 Assessment Form Revamp & Certificate Enhancement (Phases 190–195) — SHIPPED 2026-03-18</summary>

- [x] Phase 190: DB Categories Foundation (2/2 plans) — completed 2026-03-17
- [x] Phase 191: Wizard UI (2/2 plans) — completed 2026-03-17
- [x] Phase 192: ValidUntil & NomorSertifikat (1/1 plans) — completed 2026-03-17
- ~~Phase 193: Clone Assessment~~ — Removed
- [x] Phase 194: PDF Certificate Download (1/1 plans) — completed 2026-03-17
- [x] Phase 195: Sub-Categories & Signatory Settings (3/3 plans) — completed 2026-03-18

</details>

<details>
<summary>✅ v7.6 Code Deduplication & Shared Services (Phases 196–199) — SHIPPED 2026-03-18</summary>

- [x] Phase 196: Shared Service Extraction (2/2 plans) — completed 2026-03-18
- [x] Phase 197: Excel Export Helper (1/1 plans) — completed 2026-03-18
- [x] Phase 198: CRUD Consolidation (1/1 plans) — completed 2026-03-18
- [x] Phase 199: Code Pattern Extraction (2/2 plans) — completed 2026-03-18

</details>

### v7.7 Renewal Certificate & Certificate History (In Progress)

**Milestone Goal:** Certificate renewal workflow untuk HC/Admin (Kelola Data) dan certificate history timeline per pekerja (shared modal). CDP Certification Management ditingkatkan untuk menyembunyikan sertifikat yang sudah di-renew.

## Phase Details

### Phase 200: Renewal Chain Foundation
**Goal**: AssessmentSession memiliki kolom renewal chain dan BuildSertifikatRowsAsync dapat menentukan apakah suatu sertifikat sudah di-renew secara akurat
**Depends on**: Phase 199
**Requirements**: RENEW-01, RENEW-02
**Success Criteria** (what must be TRUE):
  1. AssessmentSession di database memiliki kolom RenewsSessionId dan RenewsTrainingId yang dapat diisi tanpa error
  2. BuildSertifikatRowsAsync mengembalikan flag IsRenewed=true hanya jika ada sesi renewal yang IsPassed==true mengarah ke sertifikat tersebut
  3. Sertifikat tanpa renewal yang lulus tetap tampil sebagai belum di-renew meskipun ada sesi renewal yang gagal
**Plans**: 2 plans

Plans:
- [ ] 200-01-PLAN.md — DB migration RenewsSessionId + RenewsTrainingId + EF model update
- [ ] 200-02-PLAN.md — BuildSertifikatRowsAsync enhancement: resolve renewal chain, set IsRenewed flag

### Phase 201: CreateAssessment Renewal Pre-fill
**Goal**: HC/Admin dapat memulai alur renewal dari sertifikat mana pun dan CreateAssessment otomatis terisi dengan data sertifikat asal
**Depends on**: Phase 200
**Requirements**: RENEW-03
**Success Criteria** (what must be TRUE):
  1. CreateAssessment yang dibuka dengan query param renewSessionId atau renewTrainingId menampilkan Title, Category, dan daftar peserta yang sudah terisi otomatis
  2. Field GenerateCertificate otomatis dicentang dan ValidUntil menjadi wajib diisi saat mode renewal aktif
  3. AssessmentSession yang dibuat dari pre-fill ini menyimpan RenewsSessionId/RenewsTrainingId sesuai param yang dikirim
**Plans**: 1 plan

Plans:
- [ ] 201-01-PLAN.md — CreateAssessment GET pre-fill from renewSessionId/renewTrainingId + POST saves renewal FK

### Phase 202: Renewal Certificate Page (Kelola Data)
**Goal**: HC/Admin memiliki halaman khusus di Kelola Data untuk melihat dan mengelola semua sertifikat expired/akan expired yang belum di-renew, termasuk aksi renew satuan dan bulk
**Depends on**: Phase 201
**Requirements**: RNPAGE-01, RNPAGE-02, RNPAGE-03, RNPAGE-04, RNPAGE-05
**Success Criteria** (what must be TRUE):
  1. Card "Renewal Sertifikat" muncul di Kelola Data Section C dan dapat diklik untuk membuka halaman daftar
  2. Halaman menampilkan hanya sertifikat dengan status Expired atau Akan Expired yang belum memiliki renewal lulus
  3. Filter Bagian, Unit, dan Kategori mempersempit daftar secara akurat
  4. Klik tombol Renew pada satu baris membuka CreateAssessment dengan data pre-filled
  5. Checkbox bulk select aktif hanya untuk sertifikat dengan kategori sama, dan Renew Selected membuka CreateAssessment multi-peserta
**Plans**: 2 plans

Plans:
- [ ] 202-01-PLAN.md — Controller actions (BuildRenewalRowsAsync, RenewalCertificate, FilterRenewalCertificate) + Views (halaman utama + partial tabel dengan AJAX filter, bulk select, tombol Renew)
- [ ] 202-02-PLAN.md — Card Renewal Sertifikat di Kelola Data Section C dengan badge count

### Phase 203: Certificate History Modal
**Goal**: Setiap sertifikat memiliki tombol/link riwayat yang membuka modal timeline renewal chain per pekerja, dapat digunakan dari dua halaman berbeda
**Depends on**: Phase 202
**Requirements**: HIST-01, HIST-02, HIST-03
**Success Criteria** (what must be TRUE):
  1. Modal history menampilkan timeline sertifikat per pekerja grouped by renewal chain, dengan sertifikat terbaru di atas
  2. Dari halaman Renewal Certificate, modal history menyertakan tombol Renew pada sertifikat yang expired/akan expired dan belum di-renew
  3. Dari CDP Certification Management, klik nama pekerja membuka modal history dalam mode read-only tanpa tombol aksi
**Plans**: TBD

Plans:
- [ ] 203-01-PLAN.md — Shared _CertificateHistoryModal partial + CertificateHistoryAsync controller action
- [ ] 203-02-PLAN.md — Integrasi modal di Renewal page (dengan tombol Renew) dan CDP CertificationManagement (read-only)

### Phase 204: CDP Certification Management Enhancement
**Goal**: Tabel Certification Management di CDP menyembunyikan sertifikat yang sudah di-renew secara default dan summary card Expired mencerminkan jumlah yang akurat
**Depends on**: Phase 203
**Requirements**: CDP-01, CDP-02, CDP-03
**Success Criteria** (what must be TRUE):
  1. Sertifikat yang sudah memiliki renewal lulus tidak tampil di tabel secara default
  2. Toggle "Tampilkan riwayat" menampilkan sertifikat yang sudah di-renew dengan visual berbeda (misalnya baris lebih redup)
  3. Summary card Expired hanya menghitung sertifikat yang belum di-renew, sehingga jumlah tidak membingungkan pengguna
**Plans**: TBD

Plans:
- [ ] 204-01-PLAN.md — CDP CertificationManagement: hide renewed default, toggle show/hide, fix Expired count

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 200. Renewal Chain Foundation | 2/2 | Complete    | 2026-03-19 | - |
| 201. CreateAssessment Renewal Pre-fill | 1/1 | Complete    | 2026-03-19 | - |
| 202. Renewal Certificate Page (Kelola Data) | 2/2 | Complete   | 2026-03-19 | - |
| 203. Certificate History Modal | v7.7 | 0/2 | Not started | - |
| 204. CDP Certification Management Enhancement | v7.7 | 0/1 | Not started | - |
