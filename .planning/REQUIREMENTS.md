# Requirements: Portal HC KPB

**Defined:** 2026-03-23
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.5 Requirements

Requirements untuk UAT end-to-end sistem Assessment (reguler + Proton). Milestone murni UAT + bug fix — semua fitur sudah terbangun di v1.0–v8.4.

### Seed Data

- [x] **SEED-01**: Seed Coach-Coachee mapping (Rustam→Rino) aktif saat startup Development
- [x] **SEED-02**: Seed sub-kategori "OJT Operasi Kilang" dengan parent OJT
- [x] **SEED-03**: Seed assessment reguler "OJT Proses Alkylation Q1-2026" untuk Rino + Iwan (token required, 30 menit, generate certificate)
- [x] **SEED-04**: Seed paket soal "Paket A" + 15 soal dengan 4 opsi + Elemen Teknis, assigned ke assessment SEED-03
- [x] **SEED-05**: Seed assessment Proton Tahun 1 "Operator - Tahun 1" untuk Rino + paket soal + 15 soal
- [x] **SEED-06**: Seed assessment Proton Tahun 3 "Operator - Tahun 3" (interview, durasi=0) untuk Rino
- [x] **SEED-07**: Seed 1 assessment completed dengan skor + sertifikat untuk Rino (data analytics/records/renewal)

### Setup

- [x] **SETUP-01**: Admin dapat membuat sub-kategori assessment dengan parent hierarchy dan verifikasi tampilan indent
- [x] **SETUP-02**: Admin/HC dapat membuat assessment multi-user dengan token, jadwal, durasi, dan sertifikat
- [x] **SETUP-03**: Admin dapat membuat paket soal dan import 15 soal via paste Excel dengan Elemen Teknis
- [x] **SETUP-04**: Admin dapat melihat ET coverage matrix pada paket soal dan preview soal

### Exam Flow

- [x] **EXAM-01**: Worker dapat melihat assessment yang ditugaskan, input token, dan mulai ujian
- [x] **EXAM-02**: Worker dapat mengerjakan ujian dengan soal acak, opsi acak, pagination, dan auto-save
- [x] **EXAM-03**: Timer countdown wall-clock berfungsi dengan warning ≤5 menit dan auto-submit saat habis
- [x] **EXAM-04**: Worker dapat resume ujian setelah disconnect dengan sisa waktu, jawaban, dan page terakhir intact
- [x] **EXAM-05**: Worker dapat review jawaban di ExamSummary dan submit untuk auto-grading dengan skor ET
- [x] **EXAM-06**: Worker dapat melihat hasil assessment dengan radar chart ET dan tinjauan jawaban (highlight benar/salah)
- [x] **EXAM-07**: Worker yang lulus mendapat sertifikat dengan nomor otomatis KPB/SEQ/BULAN/TAHUN dan dapat print/PDF

### Monitoring

- [x] **MON-01**: HC dapat memonitor ujian real-time via SignalR dengan stat cards dan status per-user
- [x] **MON-02**: HC dapat manage token (copy, regenerate) dan force close/reset ujian
- [x] **MON-03**: HC dapat export hasil ujian ke Excel
- [x] **MON-04**: Analytics dashboard menampilkan fail rate, trend, ET breakdown, dan expiring soon dengan cascading filter

### Proton

- [x] **PROT-01**: Admin dapat membuat assessment Proton Tahun 1/2 (online exam) dengan track selection dan flow ujian berjalan normal
- [x] **PROT-02**: Admin dapat membuat assessment Proton Tahun 3 (interview, durasi=0) tanpa paket soal
- [x] **PROT-03**: HC dapat input hasil interview Tahun 3 (5 aspek penilaian skor 1-5, judges, catatan, IsPassed manual)
- [x] **PROT-04**: ProtonFinalAssessment auto-created saat interview Tahun 3 lulus, sertifikat di-generate

### Edge Cases

- [x] **EDGE-01**: Token salah ditolak dengan pesan error, token expired/invalid tidak bisa digunakan
- [x] **EDGE-02**: HC force close mengakhiri ujian worker secara real-time, HC reset memungkinkan ujian ulang
- [x] **EDGE-03**: HC regenerate token menghasilkan token baru dan token lama invalid
- [x] **EDGE-04**: Renewal sertifikat expired berfungsi end-to-end dari alarm hingga perpanjangan

### Records

- [x] **REC-01**: Worker dapat melihat riwayat assessment di My Records dengan kolom lengkap dan export Excel
- [x] **REC-02**: HC dapat melihat data seluruh pekerja di Team View dengan date range filter dan export

### Bug Fix

- [x] **FIX-01**: Semua bug yang ditemukan selama simulasi UAT diperbaiki dan diverifikasi

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

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEED-01 | Phase 241 | Complete |
| SEED-02 | Phase 241 | Complete |
| SEED-03 | Phase 241 | Complete |
| SEED-04 | Phase 241 | Complete |
| SEED-05 | Phase 241 | Complete |
| SEED-06 | Phase 241 | Complete |
| SEED-07 | Phase 241 | Complete |
| SETUP-01 | Phase 242 | Complete |
| SETUP-02 | Phase 242 | Complete |
| SETUP-03 | Phase 242 | Complete |
| SETUP-04 | Phase 242 | Complete |
| EXAM-01 | Phase 243 | Complete |
| EXAM-02 | Phase 243 | Complete |
| EXAM-03 | Phase 243 | Complete |
| EXAM-04 | Phase 243 | Complete |
| EXAM-05 | Phase 243 | Complete |
| EXAM-06 | Phase 243 | Complete |
| EXAM-07 | Phase 243 | Complete |
| MON-01 | Phase 244 | Complete |
| MON-02 | Phase 244 | Complete |
| MON-03 | Phase 244 | Complete |
| MON-04 | Phase 244 | Complete |
| PROT-01 | Phase 245 | Complete |
| PROT-02 | Phase 245 | Complete |
| PROT-03 | Phase 245 | Complete |
| PROT-04 | Phase 245 | Complete |
| EDGE-01 | Phase 246 | Complete |
| EDGE-02 | Phase 246 | Complete |
| EDGE-03 | Phase 246 | Complete |
| EDGE-04 | Phase 246 | Complete |
| REC-01 | Phase 246 | Complete |
| REC-02 | Phase 246 | Complete |
| FIX-01 | Phase 247 | Complete |

**Coverage:**
- v8.5 requirements: 27 total
- Mapped to phases: 27
- Unmapped: 0 ✓
- Complete (SEED): 7/7

---
*Requirements defined: 2026-03-23*
*Last updated: 2026-03-24 — Restored from commit 2f19b5cf, SEED requirements marked complete*
