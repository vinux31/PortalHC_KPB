# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** - Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** - Sub-path deployment fixes (shipped 2026-03-27)
- 🚧 **v10.0 UAT Assessment OJT di Server Development** - Phases 264-277 (in progress)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v9.1, Phases 1-263) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

### 🚧 v10.0 UAT Assessment OJT di Server Development

**Milestone Goal:** Simulasi test end-to-end assessment kategori OJT di server development, temukan bug/issue, perbaiki di project lokal.
**Verification approach:** Manual — user test langsung di browser server dev, lapor temuan, Claude fix bug di kode lokal.

- [x] **Phase 264: Admin Setup Assessment OJT** - Admin buat assessment, upload soal, assign worker (completed 2026-03-27)
- [x] **Phase 265: Worker Exam Flow** - Worker mulai ujian, jawab soal, navigasi halaman (completed 2026-03-27)
- [x] **Phase 266: Review, Submit & Hasil** - Review jawaban, submit, grading, sertifikat
- [x] **Phase 267: Resilience & Edge Cases** - Offline, resume, refresh, timeout behavior
- [x] **Phase 268: Monitoring Dashboard** - Admin/HC pantau progress real-time (completed 2026-03-28)
- [x] **Phase 269: Loading overlay SignalR** - Loading overlay saat koneksi belum ready
- [x] **Phase 270: Perbaiki resume exam** - Sederhanakan modal resume + redirect ke page 0
- [x] **Phase 271: Fix timer ujian** - Server-authoritative timer dengan wall-clock cross-check
- [x] **Phase 272: Block submit jika belum semua soal terisi** - Frontend disable + backend guard
- [x] **Phase 274: Hilangkan score di sertifikat** - Remove skor dari sertifikat
- [x] **Phase 275: Warning create assessment** - Pre test tidak bisa create certificate
- [x] **Phase 276: Navigasi soal di StartExam** - Tampilkan seluruh nomor soal dengan fitur klik langsung ke lokasi (completed 2026-04-01)
- [ ] **Phase 277: Delete Peserta Assessment di EditAssessment** - Hapus peserta assessment satu-per-satu dari halaman EditAssessment

## Phase Details

### Phase 264: Admin Setup Assessment OJT
**Goal**: Admin dapat membuat assessment OJT lengkap dengan soal dan peserta yang siap dikerjakan
**Depends on**: Nothing (first phase)
**Requirements**: SETUP-01, SETUP-02, SETUP-03, SETUP-04
**Success Criteria** (what must be TRUE):
  1. Admin dapat login dan membuat assessment baru dengan kategori OJT, judul, dan konfigurasi lengkap
  2. Admin dapat download template soal Excel dan mengimport soal ke assessment yang sudah dibuat
  3. Admin dapat assign worker ke assessment dan worker muncul di daftar peserta
  4. Assessment berstatus Open dan siap dikerjakan oleh worker yang di-assign
**Plans**: 1 plan
**UI hint**: yes

Plans:
- [x] 264-01: TBD

### Phase 265: Worker Exam Flow
**Goal**: Worker dapat mengerjakan ujian dengan pengalaman yang lancar — soal tampil, timer berjalan, jawaban tersimpan, navigasi berfungsi
**Depends on**: Phase 264
**Requirements**: EXAM-01, EXAM-02, EXAM-03, EXAM-04, EXAM-05, EXAM-06, EXAM-07, EXAM-08
**Success Criteria** (what must be TRUE):
  1. Worker dapat melihat daftar assessment dengan status badge dan jadwal yang benar
  2. Worker dapat memulai ujian (dengan token verification jika aktif) dan soal ditampilkan dengan benar
  3. Timer berjalan akurat dengan format tampilan yang benar, dan network status indicator tampil di sticky header
  4. Jawaban auto-save saat worker memilih opsi, navigasi antar halaman soal (10 soal/halaman) berfungsi
  5. Tombol "Keluar Ujian" (abandon) berfungsi dengan konfirmasi dan redirect yang benar
**Plans**: 1 plan
**UI hint**: yes

Plans:
- [x] 265-01: UAT worker exam flow — 3 skenario (token, non-token, abandon)

### Phase 266: Review, Submit & Hasil
**Goal**: Worker dapat me-review jawaban, submit ujian, melihat hasil dan sertifikat
**Depends on**: Phase 265
**Requirements**: SUBMIT-01, SUBMIT-02, SUBMIT-03, RESULT-01, RESULT-02, RESULT-03, CERT-01
**Success Criteria** (what must be TRUE):
  1. Summary jawaban ditampilkan per soal dengan warning untuk soal yang belum dijawab
  2. Submit berhasil dan grading otomatis menghasilkan skor yang benar
  3. Halaman hasil menampilkan skor, status pass/fail, dan review jawaban per-soal (jawaban benar vs dipilih)
  4. Analisa Elemen Teknis ditampilkan jika assessment memiliki data ET
  5. Sertifikat dapat di-preview dan di-download sebagai PDF jika worker lulus
**Plans**: 2 plans
**UI hint**: yes

Plans:
- [x] 266-01: UAT review, submit, grading, hasil, sertifikat — 2 skenario (rino happy path + arsyad partial)
- [x] 266-02: Gap closure — fix ExamSummary warning logic + CertificatePdf 204

### Phase 267: Resilience & Edge Cases
**Goal**: Ujian tahan terhadap gangguan — koneksi putus, tab tertutup, browser refresh, dan timer habis ditangani dengan benar
**Depends on**: Phase 265
**Requirements**: EDGE-01, EDGE-02, EDGE-03, EDGE-04, EDGE-05, EDGE-06, EDGE-07
**Success Criteria** (what must be TRUE):
  1. Saat koneksi putus, warning/retry muncul dan jawaban yang sudah dipilih tidak hilang
  2. Setelah tab tertutup dan resume, worker kembali ke halaman soal terakhir dengan jawaban tetap tercentang
  3. Setelah resume, timer lanjut dari sisa waktu (tidak reset) dan progress counter akurat
  4. Browser refresh tidak menghilangkan jawaban, posisi halaman, atau timer
  5. Saat timer habis, behavior sesuai konfigurasi (auto-submit/block/pesan)
**Plans**: 3 plans

Plans:
- [x] 267-01: UAT resilience Regan — koneksi putus, tab close/resume, browser refresh (EDGE-01 sampai EDGE-06)
- [x] 267-02: UAT timer habis — EDGE-07 PASS, modal + auto-submit berjalan benar
- [x] 267-03: Gap closure — fix timer display on resume + fix blocker submit incomplete saat timer expired

### Phase 268: Monitoring Dashboard
**Goal**: Admin/HC dapat memantau progress ujian secara real-time dan melihat hasil setelah selesai
**Depends on**: Phase 265
**Requirements**: MON-01, MON-02, MON-03, MON-04
**Success Criteria** (what must be TRUE):
  1. Dashboard menampilkan progress real-time (x/total soal terjawab) per peserta
  2. Status lifecycle (Open, InProgress, Completed) berubah sesuai aktivitas worker
  3. Timer/elapsed yang ditampilkan akurat dan sinkron dengan sisa waktu worker
  4. Setelah worker submit, hasil menampilkan skor dan status pass/fail
**Plans**: 1 plan
**UI hint**: yes

Plans:
- [x] 268-01: UAT monitoring dashboard — analisa kode, browser test dua bersamaan, batch fix bug

## Progress

**Execution Order:**
Phases execute in numeric order: 264 → 265 → 266 → 267 → 268

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 264. Admin Setup Assessment OJT | 1/1 | Complete    | 2026-03-27 |
| 265. Worker Exam Flow | 1/1 | Complete    | 2026-03-27 |
| 266. Review, Submit & Hasil | 2/2 | Complete   | 2026-03-28 |
| 267. Resilience & Edge Cases | 3/3 | Complete    | 2026-03-29 |
| 268. Monitoring Dashboard | 1/1 | Complete    | 2026-03-28 |
| 269. Loading overlay SignalR | 3/3 | Complete    | 2026-03-31 |
| 270. Perbaiki resume exam | 1/1 | Complete    | 2026-03-31 |
| 271. Fix timer ujian | 1/1 | Complete    | 2026-03-31 |
| 272. Block submit belum terisi | 1/1 | Complete    | 2026-03-31 |
| 274. Hilangkan score di sertifikat | 1/1 | Complete    | 2026-03-28 |
| 275. Warning create assessment | 1/1 | Complete    | 2026-03-28 |
| 276. Navigasi soal di StartExam | 1/1 | Complete    | 2026-04-01 |
| 277. Delete Peserta Assessment | 0/1 | In Progress | — |

### Phase 269: Loading overlay saat koneksi SignalR belum ready di StartExam

**Goal:** Menambahkan loading overlay di StartExam yang memblokir interaksi user selama SignalR hub belum connected, dengan error state jika gagal
**Requirements**: OVL-01, OVL-02, OVL-03, OVL-04, OVL-05, OVL-06, OVL-07
**Depends on:** Phase 268
**Plans:** 3/3 plans complete

Plans:
- [x] 269-01-PLAN.md — Loading overlay HTML, CSS, JavaScript di StartExam.cshtml

### Phase 270: Perbaiki resume exam: notif lanjutkan mengerjakan dan redirect ke page 1

**Goal:** Sederhanakan modal resume exam (hapus info nomor soal) dan ubah redirect agar selalu ke halaman 1
**Requirements**: RESUME-01, RESUME-02
**Depends on:** Phase 269
**Plans:** 1/1 plans complete

Plans:
- [x] 270-01-PLAN.md — Sederhanakan modal resume + redirect ke page 0

### Phase 271: Fix timer ujian — monitoring salah baca, resume timer bertambah, dan logic timer lainnya

**Goal:** Perbaiki semua bug terkait timer ujian: (1) monitoring terbaca waktu habis padahal peserta sedang mengerjakan, (2) resume exam timer malah bertambah (misal 25:30 → 25:35), (3) audit seluruh logic timer (start, resume, countdown, sinkronisasi server-client)
**Requirements**: TIMER-01, TIMER-02
**Depends on:** Phase 270
**Plans:** 1/1 plans complete

Plans:
- [x] 271-01-PLAN.md — Server-authoritative timer: wall-clock cross-check + clamp validation

### Phase 272: Block submit jika belum semua soal terisi

**Goal:** Cegah submit ujian jika masih ada soal kosong — disable tombol di frontend, validasi di backend, exception untuk auto-submit saat waktu habis
**Requirements**: BLOCK-01, BLOCK-02, BLOCK-03
**Depends on:** Phase 271
**Plans:** 1/1 plans complete

Plans:
- [x] 272-01-PLAN.md — Frontend disable + backend guard + auto-submit exception

### Phase 274: Hilangkan score di sertifikat pojok kanan bawah

**Goal:** [To be planned]
**Requirements**: TBD
**Depends on:** Phase 273
**Plans:** 1/1 plans complete

Plans:
- [x] TBD (run /gsd:plan-phase 274 to break down) (completed 2026-03-28)

### Phase 275: Warning create assessment: pre test tidak bisa create certificate, hanya post test

**Goal:** [To be planned]
**Requirements**: TBD
**Depends on:** Phase 274
**Plans:** 1/1 plans complete

Plans:
- [x] TBD (run /gsd:plan-phase 275 to break down) (completed 2026-03-28)

### Phase 276: Navigasi soal di StartExam

**Goal:** Menampilkan seluruh nomor soal (bukan hanya soal di halaman saat ini) dengan fitur klik untuk langsung loncat ke lokasi soal tertentu. Panel navigasi menampilkan semua nomor soal dari 1 sampai N dalam grid layout dengan indikator status jawaban.
**Requirements**: TBD
**Depends on:** Phase 275
**Plans:** 1/1 plans complete

Plans:
- [x] 276-01-PLAN.md — Tampilkan seluruh nomor soal dengan fitur klik langsung ke lokasi soal

### Phase 277: Delete Peserta Assessment di EditAssessment

**Goal:** Menambahkan fitur hapus peserta assessment satu-per-satu dari halaman EditAssessment, dengan kolom status assessment, guard logic, dan redirect sesuai konteks
**Requirements**: TBD
**Depends on:** Phase 276
**Plans:** 1 plan

Plans:
- [ ] 277-01-PLAN.md — Backend action + frontend tabel peserta dengan kolom status dan tombol hapus

### Phase 278: Cari Bug, Block, Error, Miss di Website

**Goal:** Scan kode area Assessment/Exam (CMPController) dan Admin/HC (AdminController), verifikasi temuan, dan hasilkan laporan terstruktur untuk user review dan prioritas fix
**Requirements**: D-04, D-06, D-07, D-08, D-09, D-10, D-11
**Depends on:** Phase 277
**Plans:** 1 plan

Plans:
- [ ] 278-01-PLAN.md — Verifikasi temuan audit dan hasilkan FINDINGS report untuk user review

### Phase 279: Tambah komponen waktu (jam:menit) pada tanggal tutup ujian (ExamWindowCloseDate)

**Goal:** [To be planned]
**Requirements**: TBD
**Depends on:** Phase 278
**Plans:** 0 plans

Plans:
- [ ] TBD (run /gsd:plan-phase 279 to break down)
