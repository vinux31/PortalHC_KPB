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
- ✅ **v8.3 Date Range Filter Team View Records** - Phase 239 (shipped 2026-03-23)
- ✅ **v8.4 Alarm Sertifikat Expired** - Phase 240 (shipped 2026-03-23)
- 🚧 **v8.5 UAT Assessment System End-to-End** - Phases 241–247 (in progress)

---

<details>
<summary>✅ v1.0–v8.4 (Phases 1–240) - SHIPPED</summary>

All prior milestones shipped. See MILESTONES.md for full detail.

Last completed phase: 240 (v8.4 — Alarm Sertifikat Expired)

</details>

---

### 🚧 v8.5 UAT Assessment System End-to-End (In Progress)

**Milestone Goal:** Simulasi dan UAT end-to-end seluruh sistem Assessment (reguler + Proton Tahun 1-3), mencakup setup, ujian, grading, sertifikat, monitoring, analytics, dan edge cases — dengan perbaikan bug yang ditemukan.

## Phases

- [ ] **Phase 241: Seed Data UAT** - Siapkan seluruh data test (coach mapping, kategori, assessment, soal, completed session) agar UAT dapat dijalankan
- [ ] **Phase 242: UAT Setup Flow** - Verifikasi admin dapat membuat kategori hierarchy, assessment multi-user, paket soal, import soal, dan melihat ET coverage
- [ ] **Phase 243: UAT Exam Flow** - Verifikasi worker dapat input token, mengerjakan ujian, resume, submit, melihat hasil, dan mencetak sertifikat
- [ ] **Phase 244: UAT Monitoring & Analytics** - Verifikasi HC dapat monitor real-time, manage token, force close, export, dan melihat analytics dashboard
- [ ] **Phase 245: UAT Proton Assessment** - Verifikasi alur assessment Proton Tahun 1/2 (online) dan Tahun 3 (interview) hingga sertifikat final
- [ ] **Phase 246: UAT Edge Cases & Records** - Verifikasi penanganan token salah, force close, regenerate token, renewal sertifikat expired, dan riwayat records
- [ ] **Phase 247: Bug Fix Pasca-UAT** - Perbaiki dan verifikasi semua bug yang ditemukan selama simulasi UAT

## Phase Details

### Phase 241: Seed Data UAT
**Goal**: Seluruh data prasyarat UAT tersedia di environment Development sehingga semua fase UAT berikutnya dapat dieksekusi tanpa setup manual
**Depends on**: Nothing (first phase)
**Requirements**: SEED-01, SEED-02, SEED-03, SEED-04, SEED-05, SEED-06, SEED-07
**Success Criteria** (what must be TRUE):
  1. Coach-coachee mapping Rustam→Rino aktif dan muncul di halaman admin Coach-Coachee Mapping
  2. Sub-kategori "OJT Operasi Kilang" tampil dengan indentasi di bawah parent OJT pada halaman Kategori Assessment
  3. Assessment reguler "OJT Proses Alkylation Q1-2026" tersedia dengan peserta Rino + Iwan, token required, durasi 30 menit, generate certificate aktif, dan Paket A berisi 15 soal dengan Elemen Teknis ter-assign
  4. Assessment Proton Tahun 1 "Operator - Tahun 1" tersedia untuk Rino lengkap dengan paket soal 15 soal
  5. Assessment Proton Tahun 3 "Operator - Tahun 3" tersedia untuk Rino dengan tipe interview dan durasi 0
  6. Satu assessment completed dengan skor dan sertifikat tersedia untuk Rino (digunakan untuk UAT analytics, records, dan renewal)
**Plans**: TBD

### Phase 242: UAT Setup Flow
**Goal**: Admin dan HC dapat melakukan seluruh alur setup assessment — dari membuat kategori hierarchy hingga melihat ET coverage matrix — tanpa error
**Depends on**: Phase 241
**Requirements**: SETUP-01, SETUP-02, SETUP-03, SETUP-04
**Success Criteria** (what must be TRUE):
  1. Admin membuat sub-kategori baru dengan parent hierarchy dan melihat tampilan indent yang benar di daftar kategori
  2. Admin/HC membuat assessment baru dengan token required, jadwal, durasi, dan generate certificate — assessment muncul di daftar ManageAssessment
  3. Admin membuat paket soal baru dan berhasil import 15 soal via paste Excel dengan kolom Elemen Teknis — semua soal tersimpan
  4. Admin membuka halaman paket soal dan melihat ET coverage matrix yang menampilkan distribusi soal per elemen, serta dapat preview soal individual
**Plans**: TBD

### Phase 243: UAT Exam Flow
**Goal**: Worker dapat menyelesaikan siklus ujian penuh — dari input token hingga mencetak sertifikat — dengan semua mekanisme integritas berfungsi
**Depends on**: Phase 241
**Requirements**: EXAM-01, EXAM-02, EXAM-03, EXAM-04, EXAM-05, EXAM-06, EXAM-07
**Success Criteria** (what must be TRUE):
  1. Worker membuka halaman Assessment, melihat assessment yang ditugaskan, input token yang benar, dan ujian dimulai
  2. Worker mengerjakan ujian dengan soal dan opsi dalam urutan acak, dapat berpindah halaman, dan jawaban tersimpan otomatis per klik
  3. Timer countdown wall-clock berjalan akurat, menampilkan warning visual saat ≤5 menit tersisa, dan ujian auto-submit saat timer habis
  4. Worker disconnect dan kembali — ujian dilanjutkan dari page terakhir dengan sisa waktu akurat dan jawaban sebelumnya masih terpilih
  5. Worker membuka ExamSummary, melihat ringkasan jawaban, submit, dan sistem menampilkan skor beserta radar chart ET
  6. Worker yang lulus dapat melihat sertifikat dengan nomor otomatis format KPB/SEQ/BULAN/TAHUN dan mencetak/download sebagai PDF
**Plans**: TBD

### Phase 244: UAT Monitoring & Analytics
**Goal**: HC dapat memantau ujian secara real-time, mengelola token, dan mengakses analytics assessment yang akurat
**Depends on**: Phase 241
**Requirements**: MON-01, MON-02, MON-03, MON-04
**Success Criteria** (what must be TRUE):
  1. HC membuka halaman AssessmentMonitoring saat ujian berlangsung dan melihat stat cards serta status (answered/total, timer, status badge) setiap peserta diperbarui secara real-time via SignalR
  2. HC dapat menyalin token, melakukan regenerate token (token lama invalid), force close ujian peserta, dan reset peserta agar dapat ujian ulang — semua dari halaman monitoring
  3. HC mengekspor hasil ujian ke file Excel yang dapat dibuka dan berisi data peserta, skor, dan status
  4. HC membuka analytics dashboard dan melihat fail rate, tren skor, ET breakdown, dan expiring soon — cascading filter Bagian/Unit/Kategori berfungsi
**Plans**: TBD

### Phase 245: UAT Proton Assessment
**Goal**: Alur assessment Proton Tahun 1/2 (ujian online) dan Tahun 3 (interview) berjalan end-to-end hingga sertifikat Proton dihasilkan
**Depends on**: Phase 241
**Requirements**: PROT-01, PROT-02, PROT-03, PROT-04
**Success Criteria** (what must be TRUE):
  1. Admin membuat assessment Proton Tahun 1 dengan track selection, assign peserta, dan worker dapat mengikuti ujian online dengan flow yang sama seperti assessment reguler
  2. Admin membuat assessment Proton Tahun 3 dengan tipe interview dan durasi 0 — assessment tersimpan tanpa memerlukan paket soal
  3. HC membuka halaman input hasil interview Tahun 3, mengisi 5 aspek penilaian (skor 1-5), nama juri, catatan, dan menandai IsPassed secara manual — data tersimpan
  4. Setelah HC menandai Tahun 3 lulus, ProtonFinalAssessment dibuat otomatis dan sertifikat Proton di-generate serta dapat diakses peserta
**Plans**: TBD

### Phase 246: UAT Edge Cases & Records
**Goal**: Sistem menangani kondisi tidak normal (token salah, force close, regenerate) dengan benar, renewal sertifikat expired berjalan end-to-end, dan worker/HC dapat melihat riwayat lengkap
**Depends on**: Phase 241
**Requirements**: EDGE-01, EDGE-02, EDGE-03, EDGE-04, REC-01, REC-02
**Success Criteria** (what must be TRUE):
  1. Worker yang memasukkan token salah mendapat pesan error yang jelas dan tidak dapat memulai ujian; token expired/invalid juga ditolak
  2. HC force close mengakhiri ujian worker secara langsung (worker di-redirect ke Results) dan HC reset memungkinkan worker mengikuti ujian ulang dari awal
  3. HC regenerate token menghasilkan token baru dan token lama tidak dapat digunakan untuk memulai ujian
  4. Alarm sertifikat expired di Home/Index → klik link → halaman RenewalCertificate → proses renewal → sertifikat baru terbuat end-to-end
  5. Worker membuka My Records dan melihat riwayat assessment pribadi dengan kolom lengkap serta dapat export ke Excel
  6. HC membuka Team View Records, memfilter dengan date range, melihat data seluruh pekerja, dan dapat export ke Excel
**Plans**: TBD

### Phase 247: Bug Fix Pasca-UAT
**Goal**: Semua bug yang ditemukan selama simulasi UAT (Phase 242–246) diperbaiki, diverifikasi, dan tidak ada regresi
**Depends on**: Phase 246
**Requirements**: FIX-01
**Success Criteria** (what must be TRUE):
  1. Setiap bug yang dicatat selama UAT memiliki fix yang dapat diverifikasi ulang di browser
  2. Alur yang sebelumnya gagal dalam UAT kini berjalan tanpa error setelah fix diterapkan
  3. Tidak ada regresi pada fitur yang sebelumnya bekerja normal (fitur di luar scope bug yang ditemukan tetap berfungsi)
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 239. Date Range Filter & Export | v8.3 | 2/2 | Complete | 2026-03-23 |
| 240. Alarm Sertifikat Expired | v8.4 | 2/2 | Complete | 2026-03-23 |
| 241. Seed Data UAT | v8.5 | 0/? | Not started | - |
| 242. UAT Setup Flow | v8.5 | 0/? | Not started | - |
| 243. UAT Exam Flow | v8.5 | 0/? | Not started | - |
| 244. UAT Monitoring & Analytics | v8.5 | 0/? | Not started | - |
| 245. UAT Proton Assessment | v8.5 | 0/? | Not started | - |
| 246. UAT Edge Cases & Records | v8.5 | 0/? | Not started | - |
| 247. Bug Fix Pasca-UAT | v8.5 | 0/? | Not started | - |
