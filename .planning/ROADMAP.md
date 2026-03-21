# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v7.7** - Phases 1–204 (shipped 2026-03-19)
- ✅ **v7.8 Dokumen KKJ & Alignment KKJ/IDP — Combine Menu** - Phases 205–207 (shipped 2026-03-20)
- ✅ **v7.9 Renewal Certificate Grouped View** - Phases 208–209 (shipped 2026-03-20)
- ✅ **v7.10 RenewalCertificate Bug Fixes & Enhancement** - Phases 210–212 (shipped 2026-03-21)
- ✅ **v7.11 CMP Records Bug Fixes & Enhancement** - Phases 213–218 (shipped 2026-03-21)
- ✅ **v7.12 Struktur Organisasi CRUD** - Phases 219–222 (shipped 2026-03-21)
- 📋 **v8.0 Assessment & Training System Audit** - Phases 223–227 (planned)

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

<details>
<summary>✅ v7.12 Struktur Organisasi CRUD (Phases 219–222) - SHIPPED 2026-03-21</summary>

- [x] Phase 219: DB Model & Migration (1/1 plans) — completed 2026-03-21
- [x] Phase 220: CRUD Page Kelola Data (2/2 plans) — completed 2026-03-21
- [x] Phase 221: Integrasi Codebase (3/3 plans) — completed 2026-03-21
- [x] Phase 222: Cleanup & Finalisasi (1/1 plan) — completed 2026-03-21

</details>

### 📋 v8.0 Assessment & Training System Audit (Planned)

**Milestone Goal:** Audit dan perkuat fondasi sistem assessment dan training — integritas data exam, analytics untuk HC, compliance tracking training, notifikasi email sertifikat, question bank terpisah, dan cleanup legacy debt.

- [ ] **Phase 223: Assessment Quick Wins** - Persist ET score, deteksi tab-switch, status lifecycle, timestamp UserResponse, dokumentasi AccessToken
- [ ] **Phase 224: Analytics Dashboard HC** - Visualisasi fail rate, trend assessment, breakdown ET, ringkasan sertifikat akan expired
- [ ] **Phase 225: Training Compliance Matrix** - CRUD matriks training wajib per jabatan, kalkulasi compliance, summary per section
- [ ] **Phase 226: Email Notification Sertifikat Expired** - Reminder otomatis 90/30/7 hari, duplikat guard via NotificationSentLog
- [ ] **Phase 227: Major Refactors** - Question Bank CRUD + import, pemilihan soal dari bank, migrasi legacy path, cleanup orphan tables, NomorSertifikat timing fix

## Phase Details

### Phase 223: Assessment Quick Wins
**Goal**: Integritas data assessment terperkuat — skor ET tersimpan per session, perilaku tab-switch tercatat dan termonitor, lifecycle status TrainingRecord terdefinisi, timestamp UserResponse terisi, dan keputusan AccessToken terdokumentasi
**Depends on**: Phase 222
**Requirements**: AINT-01, AINT-02, AINT-03, AINT-04, CLEN-01, CLEN-05
**Success Criteria** (what must be TRUE):
  1. Setelah peserta submit exam, skor per ElemenTeknis dapat dilihat di database tabel SessionElemenTeknisScore (bukan hanya skor total)
  2. Ketika peserta beralih tab atau kehilangan focus saat ujian berlangsung, event focus_lost/focus_returned tercatat di ExamActivityLog
  3. HC dapat membuka halaman AssessmentMonitoringDetail dan melihat indikator berapa kali peserta melakukan tab-switch
  4. Setelah SaveLegacyAnswer dipanggil, field SubmittedAt pada UserResponse terisi dengan waktu submit
  5. Kode mengandung komentar dokumentasi yang menjelaskan alasan AccessToken dibiarkan shared (common exam room pattern)
**Plans**: TBD

### Phase 224: Analytics Dashboard HC
**Goal**: HC memiliki halaman Analytics Dashboard dengan visualisasi data assessment dan sertifikat yang actionable
**Depends on**: Phase 223
**Requirements**: ANLT-01, ANLT-02, ANLT-03, ANLT-04
**Success Criteria** (what must be TRUE):
  1. HC dapat mengakses halaman Analytics Dashboard dan melihat grafik fail rate dikelompokkan per section dan per category assessment
  2. HC dapat memilih rentang periode waktu dan melihat grafik trend jumlah assessment yang passed vs failed dalam periode tersebut
  3. HC dapat melihat tabel breakdown skor ElemenTeknis aggregate (rata-rata, distribusi) dikelompokkan per kategori assessment
  4. HC dapat melihat daftar sertifikat yang akan expired dalam 30, 60, dan 90 hari ke depan dalam satu ringkasan
**Plans**: TBD

### Phase 225: Training Compliance Matrix
**Goal**: Admin dapat mendefinisikan training wajib per jabatan, dan HC dapat melihat compliance percentage pekerja terhadap training yang diwajibkan
**Depends on**: Phase 223
**Requirements**: COMP-01, COMP-02, COMP-03
**Success Criteria** (what must be TRUE):
  1. Admin dapat membuka halaman Kelola Data, mengakses matriks training wajib, dan melakukan CRUD (tambah, edit, hapus) mapping PositionTitle ke SubKategori training yang diwajibkan
  2. Compliance percentage seorang pekerja dihitung berdasarkan perbandingan training yang sudah diselesaikan vs training yang diwajibkan oleh jabatannya (bukan total training di database)
  3. HC dapat melihat halaman team view yang menampilkan compliance summary per section/unit — berapa persen pekerja di tiap unit yang memenuhi training wajib jabatannya
**Plans**: TBD

### Phase 226: Email Notification Sertifikat Expired
**Goal**: Sistem mengirim email reminder otomatis kepada pekerja sebelum sertifikat mereka expired, tanpa duplikat meskipun service restart
**Depends on**: Phase 223
**Requirements**: NOTF-01, NOTF-02, NOTF-03, NOTF-04
**Success Criteria** (what must be TRUE):
  1. Pekerja yang sertifikatnya akan expired dalam 90 hari menerima email reminder secara otomatis dari sistem
  2. Pekerja yang sertifikatnya akan expired dalam 30 hari menerima email reminder otomatis kedua
  3. Pekerja yang sertifikatnya akan expired dalam 7 hari menerima email reminder otomatis ketiga
  4. Jika background service restart atau dijadwalkan ulang, email yang sudah terkirim tidak dikirim lagi — NotificationSentLog mencegah duplikat per sertifikat per threshold
**Plans**: TBD

### Phase 227: Major Refactors
**Goal**: Question Bank terpisah dari assessment session tersedia untuk Admin/HC, legacy question path dimigrasikan ke package format, tabel orphan dibersihkan, dan NomorSertifikat di-generate pada waktu yang tepat
**Depends on**: Phase 224, Phase 225
**Requirements**: QBNK-01, QBNK-02, QBNK-03, CLEN-02, CLEN-03, CLEN-04
**Success Criteria** (what must be TRUE):
  1. Admin/HC dapat mengakses halaman Question Bank, melakukan CRUD soal secara mandiri tanpa harus membuat assessment session terlebih dahulu
  2. Admin/HC dapat menggunakan fitur Import untuk mengunggah soal dari file Excel ke Question Bank
  3. Saat membuat assessment baru, Admin/HC dapat memilih soal dari Question Bank yang kemudian di-copy ke PackageQuestion (bukan hanya buat soal baru)
  4. Session assessment yang masih menggunakan legacy path (AssessmentQuestion/AssessmentOption/UserResponse) telah dimigrasikan ke package format, dan tabel legacy tidak lagi digunakan untuk session baru
  5. Tabel AssessmentCompetencyMap dan UserCompetencyLevel tidak lagi memiliki data aktif dan telah dihapus dari skema database
  6. NomorSertifikat hanya ter-generate saat SubmitExam + IsPassed = true (bukan saat CreateAssessment), sehingga tidak ada NomorSertifikat pada session yang belum lulus
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
| 216. Export Fixes & Display Enhancement | v7.11 | 0/? | Skipped | - |
| 217. Fix Category Dropdown RecordsTeam | v7.11 | 1/1 | Complete | 2026-03-21 |
| 218. RecordsWorkerDetail Redesign & ImportTraining Update | v7.11 | 2/2 | Complete | 2026-03-21 |
| 219. DB Model & Migration | v7.12 | 1/1 | Complete | 2026-03-21 |
| 220. CRUD Page Kelola Data | v7.12 | 2/2 | Complete | 2026-03-21 |
| 221. Integrasi Codebase | v7.12 | 3/3 | Complete | 2026-03-21 |
| 222. Cleanup & Finalisasi | v7.12 | 1/1 | Complete | 2026-03-21 |
| 223. Assessment Quick Wins | v8.0 | 0/TBD | Not started | - |
| 224. Analytics Dashboard HC | v8.0 | 0/TBD | Not started | - |
| 225. Training Compliance Matrix | v8.0 | 0/TBD | Not started | - |
| 226. Email Notification Sertifikat Expired | v8.0 | 0/TBD | Not started | - |
| 227. Major Refactors | v8.0 | 0/TBD | Not started | - |
