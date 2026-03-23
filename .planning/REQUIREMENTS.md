# Requirements: Portal HC KPB

**Defined:** 2026-03-23
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.5 Requirements

Requirements untuk UAT end-to-end sistem Assessment (reguler + Proton). Milestone murni UAT + bug fix — semua fitur sudah terbangun di v1.0–v8.4.

### Seed Data

- [ ] **SEED-01**: Seed Coach-Coachee mapping (Rustam→Rino) aktif saat startup Development
- [ ] **SEED-02**: Seed sub-kategori "OJT Operasi Kilang" dengan parent OJT
- [ ] **SEED-03**: Seed assessment reguler "OJT Proses Alkylation Q1-2026" untuk Rino + Iwan (token required, 30 menit, generate certificate)
- [ ] **SEED-04**: Seed paket soal "Paket A" + 15 soal dengan 4 opsi + Elemen Teknis, assigned ke assessment SEED-03
- [ ] **SEED-05**: Seed assessment Proton Tahun 1 "Operator - Tahun 1" untuk Rino + paket soal + 15 soal
- [ ] **SEED-06**: Seed assessment Proton Tahun 3 "Operator - Tahun 3" (interview, durasi=0) untuk Rino
- [ ] **SEED-07**: Seed 1 assessment completed dengan skor + sertifikat untuk Rino (data analytics/records/renewal)

### Setup

- [ ] **SETUP-01**: Admin dapat membuat sub-kategori assessment dengan parent hierarchy dan verifikasi tampilan indent
- [ ] **SETUP-02**: Admin/HC dapat membuat assessment multi-user dengan token, jadwal, durasi, dan sertifikat
- [ ] **SETUP-03**: Admin dapat membuat paket soal dan import 15 soal via paste Excel dengan Elemen Teknis
- [ ] **SETUP-04**: Admin dapat melihat ET coverage matrix pada paket soal dan preview soal

### Exam Flow

- [ ] **EXAM-01**: Worker dapat melihat assessment yang ditugaskan, input token, dan mulai ujian
- [ ] **EXAM-02**: Worker dapat mengerjakan ujian dengan soal acak, opsi acak, pagination, dan auto-save
- [ ] **EXAM-03**: Timer countdown wall-clock berfungsi dengan warning ≤5 menit dan auto-submit saat habis
- [ ] **EXAM-04**: Worker dapat resume ujian setelah disconnect dengan sisa waktu, jawaban, dan page terakhir intact
- [ ] **EXAM-05**: Worker dapat review jawaban di ExamSummary dan submit untuk auto-grading dengan skor ET
- [ ] **EXAM-06**: Worker dapat melihat hasil assessment dengan radar chart ET dan tinjauan jawaban (highlight benar/salah)
- [ ] **EXAM-07**: Worker yang lulus mendapat sertifikat dengan nomor otomatis KPB/SEQ/BULAN/TAHUN dan dapat print/PDF

### Monitoring

- [ ] **MON-01**: HC dapat memonitor ujian real-time via SignalR dengan stat cards dan status per-user
- [ ] **MON-02**: HC dapat manage token (copy, regenerate) dan force close/reset ujian
- [ ] **MON-03**: HC dapat export hasil ujian ke Excel
- [ ] **MON-04**: Analytics dashboard menampilkan fail rate, trend, ET breakdown, dan expiring soon dengan cascading filter

### Proton

- [ ] **PROT-01**: Admin dapat membuat assessment Proton Tahun 1/2 (online exam) dengan track selection dan flow ujian berjalan normal
- [ ] **PROT-02**: Admin dapat membuat assessment Proton Tahun 3 (interview, durasi=0) tanpa paket soal
- [ ] **PROT-03**: HC dapat input hasil interview Tahun 3 (5 aspek penilaian skor 1-5, judges, catatan, IsPassed manual)
- [ ] **PROT-04**: ProtonFinalAssessment auto-created saat interview Tahun 3 lulus, sertifikat di-generate

### Edge Cases

- [ ] **EDGE-01**: Token salah ditolak dengan pesan error, token expired/invalid tidak bisa digunakan
- [ ] **EDGE-02**: HC force close mengakhiri ujian worker secara real-time, HC reset memungkinkan ujian ulang
- [ ] **EDGE-03**: HC regenerate token menghasilkan token baru dan token lama invalid
- [ ] **EDGE-04**: Renewal sertifikat expired berfungsi end-to-end dari alarm hingga perpanjangan

### Records

- [ ] **REC-01**: Worker dapat melihat riwayat assessment di My Records dengan kolom lengkap dan export Excel
- [ ] **REC-02**: HC dapat melihat data seluruh pekerja di Team View dengan date range filter dan export

### Bug Fix

- [ ] **FIX-01**: Semua bug yang ditemukan selama simulasi UAT diperbaiki dan diverifikasi

## Validated Requirements (Previous Milestones)

### v8.4 — Alarm Sertifikat Expired
- [x] ALRT-01..04, NOTF-01..03 — All complete (Phase 240)

### v8.3 — Date Range Filter Team View Records
- [x] FILT-01..06, EXP-01..02 — All complete (Phase 239)

## Future Requirements

### Next Milestone Goals

- **NEXT-01**: Competency gap heatmap (worker x kompetensi matrix)
- **NEXT-02**: Scheduling integration / calendar untuk coaching sessions
- **NEXT-03**: AI-generated coaching session summaries
- **NEXT-04**: SLA/escalation otomatis untuk approval yang terlalu lama
- **NEXT-05**: Predicted completion date berdasarkan historical pace

## Out of Scope

| Feature | Reason |
|---------|--------|
| Fitur baru assessment | v8.5 murni UAT + bug fix, bukan development fitur baru |
| Automated browser testing | UAT dilakukan manual via browser, bukan Playwright/Selenium |
| Performance/load testing | Scope terbatas pada functional correctness |
| Tab-switch detection (AINT-02/03) | Deferred dari v8.0, tidak dalam scope UAT |

## Traceability

Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEED-01 | — | Pending |
| SEED-02 | — | Pending |
| SEED-03 | — | Pending |
| SEED-04 | — | Pending |
| SEED-05 | — | Pending |
| SEED-06 | — | Pending |
| SEED-07 | — | Pending |
| SETUP-01 | — | Pending |
| SETUP-02 | — | Pending |
| SETUP-03 | — | Pending |
| SETUP-04 | — | Pending |
| EXAM-01 | — | Pending |
| EXAM-02 | — | Pending |
| EXAM-03 | — | Pending |
| EXAM-04 | — | Pending |
| EXAM-05 | — | Pending |
| EXAM-06 | — | Pending |
| EXAM-07 | — | Pending |
| MON-01 | — | Pending |
| MON-02 | — | Pending |
| MON-03 | — | Pending |
| MON-04 | — | Pending |
| PROT-01 | — | Pending |
| PROT-02 | — | Pending |
| PROT-03 | — | Pending |
| PROT-04 | — | Pending |
| EDGE-01 | — | Pending |
| EDGE-02 | — | Pending |
| EDGE-03 | — | Pending |
| EDGE-04 | — | Pending |
| REC-01 | — | Pending |
| REC-02 | — | Pending |
| FIX-01 | — | Pending |

**Coverage:**
- v8.5 requirements: 27 total
- Mapped to phases: 0
- Unmapped: 27 ⚠️

---
*Requirements defined: 2026-03-23*
*Last updated: 2026-03-23 after initial definition*
