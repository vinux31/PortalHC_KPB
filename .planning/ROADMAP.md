# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- 🚧 **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (in progress)

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

### 🚧 v7.10 RenewalCertificate Bug Fixes & Enhancement (In Progress)

**Milestone Goal:** Perbaikan semua bug renewal chain (bulk renew, badge count, FK logic) + data/display bugs (ValidUntil=null, category prefill, grouping) + tambah filter tipe + renewal flow berdasarkan tipe (Assessment vs Training) + AddTraining renewal mode.

#### Phase 210: Critical Renewal Chain Fixes
**Goal**: Renewal chain berfungsi benar — semua user yang dipilih mendapat FK renewal, badge count Admin/Index sinkron, dan filter TrainingRecord hanya hitungan yang IsPassed
**Depends on**: Phase 209
**Requirements**: FIX-01, FIX-02, FIX-03
**Success Criteria** (what must be TRUE):
  1. Bulk renew pada N pekerja menghasilkan N AssessmentSession/TrainingRecord baru yang masing-masing memiliki RenewsSessionId/RenewsTrainingId terisi (bukan hanya record pertama)
  2. Badge count di kartu Section C Admin/Index menampilkan angka yang identik dengan jumlah baris pada halaman RenewalCertificate (termasuk TR→AS dan TR→TR)
  3. Sertifikat yang berasal dari TrainingRecord yang tidak IsPassed tidak muncul sebagai kandidat renewal di renewedByTrSessionIds
**Plans**: 1 plan

Plans:
- [x] 210-01: Critical renewal chain fixes — bulk FK assignment, badge count sync, IsPassed filter

#### Phase 211: Data & Display Fixes
**Goal**: Semua data dan tampilan RenewalCertificate akurat — ValidUntil=null ditangani benar, category pre-fill dari TR berfungsi, grouping konsisten dan aman
**Depends on**: Phase 210
**Requirements**: FIX-05, FIX-06, FIX-07, FIX-08, FIX-09, FIX-10
**Success Criteria** (what must be TRUE):
  1. Sertifikat dengan ValidUntil=null dan CertificateType bukan "Permanent" muncul di renewal list (tidak hilang karena salah dianggap Permanent)
  2. Saat Admin klik "Renew" dari baris TrainingRecord, form CreateAssessment ter-prefill Category sesuai sertifikat asal
  3. Group header menampilkan nama kategori yang konsisten dengan AssessmentCategories (tanpa variasi typo/case)
  4. Group-by judul berfungsi case-insensitive sehingga "MIGAS" dan "Migas" masuk ke group yang sama
  5. Judul sertifikat dengan karakter khusus (/, &, #) tidak menyebabkan URL mismatch atau group yang terpisah
**Plans**: 1 plan

Plans:
- [x] 211-01: Data & display fixes — ValidUntil null handling, category prefill, MapKategori, grouping case/URL

#### Phase 212: Tipe Filter, Renewal Flow, AddTraining Renewal
**Goal**: Admin dapat memfilter renewal list berdasarkan tipe (Assessment/Training), alur renewal berbeda sesuai tipe sumber, dan AddTraining mendukung mode renewal dengan pre-fill + FK
**Depends on**: Phase 211
**Requirements**: ENH-01, ENH-02, ENH-03, ENH-04, FIX-04
**Success Criteria** (what must be TRUE):
  1. Dropdown filter "Tipe" pada halaman RenewalCertificate menampilkan pilihan Assessment / Training / Semua, dan memfilter tabel sesuai tipe sumber sertifikat
  2. Klik "Renew" pada baris bertipe Training menampilkan popup pilihan: "Renew via Assessment" atau "Renew via Training Record baru" — bukan langsung ke CreateAssessment
  3. Bulk renew yang mencakup campuran tipe Assessment dan Training menampilkan konfirmasi atau memisahkan alur sehingga Training items tidak langsung dikirim ke CreateAssessment
  4. Halaman AddTraining dapat dibuka dalam mode renewal (dari link renewal) dengan field Title/Category/Peserta ter-prefill dan RenewsTrainingId/RenewsSessionId tersimpan saat submit
**Plans**: 2 plans

Plans:
- [x] 212-01: Tipe filter + renewal method modal + bulk mixed-type validation
- [ ] 212-02: AddTraining renewal mode (prefill + FK + bulk multi-user)

## Phase Details

### Phase 210: Critical Renewal Chain Fixes
**Goal**: Renewal chain berfungsi benar — semua user yang dipilih mendapat FK renewal, badge count Admin/Index sinkron, dan filter TrainingRecord hanya hitungan yang IsPassed
**Depends on**: Phase 209
**Requirements**: FIX-01, FIX-02, FIX-03
**Success Criteria** (what must be TRUE):
  1. Bulk renew pada N pekerja menghasilkan N AssessmentSession/TrainingRecord baru yang masing-masing memiliki RenewsSessionId/RenewsTrainingId terisi (bukan hanya record pertama)
  2. Badge count di kartu Section C Admin/Index menampilkan angka yang identik dengan jumlah baris pada halaman RenewalCertificate (termasuk TR→AS dan TR→TR)
  3. Sertifikat yang berasal dari TrainingRecord yang tidak IsPassed tidak muncul sebagai kandidat renewal di renewedByTrSessionIds
**Plans**: 1 plan

### Phase 211: Data & Display Fixes
**Goal**: Semua data dan tampilan RenewalCertificate akurat — ValidUntil=null ditangani benar, category pre-fill dari TR berfungsi, grouping konsisten dan aman
**Depends on**: Phase 210
**Requirements**: FIX-05, FIX-06, FIX-07, FIX-08, FIX-09, FIX-10
**Success Criteria** (what must be TRUE):
  1. Sertifikat dengan ValidUntil=null dan CertificateType bukan "Permanent" muncul di renewal list (tidak hilang karena salah dianggap Permanent)
  2. Saat Admin klik "Renew" dari baris TrainingRecord, form CreateAssessment ter-prefill Category sesuai sertifikat asal
  3. Group header menampilkan nama kategori yang konsisten dengan AssessmentCategories (tanpa variasi typo/case)
  4. Group-by judul berfungsi case-insensitive sehingga "MIGAS" dan "Migas" masuk ke group yang sama
  5. Judul sertifikat dengan karakter khusus (/, &, #) tidak menyebabkan URL mismatch atau group yang terpisah
**Plans**: 1 plan

### Phase 212: Tipe Filter, Renewal Flow, AddTraining Renewal
**Goal**: Admin dapat memfilter renewal list berdasarkan tipe (Assessment/Training), alur renewal berbeda sesuai tipe sumber, dan AddTraining mendukung mode renewal dengan pre-fill + FK
**Depends on**: Phase 211
**Requirements**: ENH-01, ENH-02, ENH-03, ENH-04, FIX-04
**Success Criteria** (what must be TRUE):
  1. Dropdown filter "Tipe" pada halaman RenewalCertificate menampilkan pilihan Assessment / Training / Semua, dan memfilter tabel sesuai tipe sumber sertifikat
  2. Klik "Renew" pada baris bertipe Training menampilkan popup pilihan: "Renew via Assessment" atau "Renew via Training Record baru" — bukan langsung ke CreateAssessment
  3. Bulk renew yang mencakup campuran tipe Assessment dan Training menampilkan konfirmasi atau memisahkan alur sehingga Training items tidak langsung dikirim ke CreateAssessment
  4. Halaman AddTraining dapat dibuka dalam mode renewal (dari link renewal) dengan field Title/Category/Peserta ter-prefill dan RenewsTrainingId/RenewsSessionId tersimpan saat submit
**Plans**: 1 plan

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 205. Halaman Gabungan KKJ & Alignment | v7.8 | 1/1 | Complete | 2026-03-20 |
| 206. Update CMP Hub & Backward Compat | v7.8 | 1/1 | Complete | 2026-03-20 |
| 207. Perbaikan Desain Tabel DokumenKkj | v7.8 | 1/1 | Complete | 2026-03-20 |
| 208. Grouped View Structure | v7.9 | 1/1 | Complete | 2026-03-20 |
| 209. Bulk Renew & Filter Compatibility | v7.9 | 1/1 | Complete | 2026-03-20 |
| 210. Critical Renewal Chain Fixes | v7.10 | 2/2 | Complete    | 2026-03-21 |
| 211. Data & Display Fixes | v7.10 | 1/1 | Complete    | 2026-03-21 |
| 212. Tipe Filter, Renewal Flow, AddTraining Renewal | v7.10 | 1/2 | In Progress|  |
