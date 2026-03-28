# Requirements: Portal HC KPB

**Defined:** 2026-03-27
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v10.0 Requirements

Requirements for UAT Assessment OJT di Server Development.

### Assessment Setup (Admin)

- [ ] **SETUP-01**: Admin dapat membuat assessment baru dengan kategori OJT
- [ ] **SETUP-02**: Admin dapat download template soal
- [ ] **SETUP-03**: Admin dapat import/upload soal ke assessment
- [ ] **SETUP-04**: Admin dapat assign worker ke assessment

### Exam Flow (Worker)

- [ ] **EXAM-01**: Worker dapat melihat daftar assessment (status badge, jadwal)
- [ ] **EXAM-02**: Token verification berfungsi (jika assessment pakai token)
- [ ] **EXAM-03**: Worker dapat memulai ujian, soal ditampilkan dengan benar
- [ ] **EXAM-04**: Timer berjalan akurat, format tampilan benar (MM:SS/HH:MM:SS)
- [ ] **EXAM-05**: Jawaban auto-save saat worker memilih opsi
- [ ] **EXAM-06**: Navigasi antar halaman soal berfungsi (10 soal/halaman)
- [ ] **EXAM-07**: Network status indicator tampil di sticky header
- [ ] **EXAM-08**: Tombol "Keluar Ujian" (abandon) berfungsi dengan benar

### Review & Submit

- [ ] **SUBMIT-01**: Summary jawaban ditampilkan per soal
- [x] **SUBMIT-02**: Warning ditampilkan untuk soal belum dijawab
- [ ] **SUBMIT-03**: Submit berhasil, grading otomatis benar

### Results & Certificate

- [ ] **RESULT-01**: Skor dan status pass/fail ditampilkan
- [ ] **RESULT-02**: Review jawaban per-soal (jawaban benar vs dipilih)
- [ ] **RESULT-03**: Analisa Elemen Teknis ditampilkan (jika ada)
- [x] **CERT-01**: Sertifikat preview & download PDF (jika lulus)

### Resilience & Edge Cases

- [x] **EDGE-01**: Lost connection — warning/retry, jawaban tidak hilang
- [x] **EDGE-02**: Tab tertutup & resume — kembali ke halaman soal terakhir
- [x] **EDGE-03**: Resume — timer lanjut dari sisa waktu (tidak reset)
- [x] **EDGE-04**: Resume — jawaban yang sudah dipilih masih tercentang
- [x] **EDGE-05**: Resume — progress counter akurat, indikasi "lanjutkan"
- [x] **EDGE-06**: Browser refresh — jawaban, posisi, timer tetap benar
- [x] **EDGE-07**: Timer habis — behavior sesuai (auto-submit/block/pesan)

### Monitoring (Admin/HC)

- [x] **MON-01**: Progress real-time (x/total soal terjawab)
- [x] **MON-02**: Status lifecycle (Open → InProgress → Completed)
- [x] **MON-03**: Timer/elapsed akurat dan sinkron dengan worker
- [x] **MON-04**: Result menampilkan skor & pass/fail setelah submit

## Out of Scope

| Feature | Reason |
|---------|--------|
| Test kategori selain OJT (IHT, OTS, dll) | Milestone ini fokus OJT saja, kategori lain flow-nya identik |
| Assessment Proton (Tahun 3 interview) | Flow berbeda, bukan multiple choice |
| Fitur baru / enhancement | Milestone ini murni UAT, bukan development |
| Production deployment | Testing di server development saja |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SETUP-01 | Phase 264 | Pending |
| SETUP-02 | Phase 264 | Pending |
| SETUP-03 | Phase 264 | Pending |
| SETUP-04 | Phase 264 | Pending |
| EXAM-01 | Phase 265 | Pending |
| EXAM-02 | Phase 265 | Pending |
| EXAM-03 | Phase 265 | Pending |
| EXAM-04 | Phase 265 | Pending |
| EXAM-05 | Phase 265 | Pending |
| EXAM-06 | Phase 265 | Pending |
| EXAM-07 | Phase 265 | Pending |
| EXAM-08 | Phase 265 | Pending |
| SUBMIT-01 | Phase 266 | Pending |
| SUBMIT-02 | Phase 266 | Complete |
| SUBMIT-03 | Phase 266 | Pending |
| RESULT-01 | Phase 266 | Pending |
| RESULT-02 | Phase 266 | Pending |
| RESULT-03 | Phase 266 | Pending |
| CERT-01 | Phase 266 | Complete |
| EDGE-01 | Phase 267 | Complete |
| EDGE-02 | Phase 267 | Complete |
| EDGE-03 | Phase 267 | Complete |
| EDGE-04 | Phase 267 | Complete |
| EDGE-05 | Phase 267 | Complete |
| EDGE-06 | Phase 267 | Complete |
| EDGE-07 | Phase 267 | Complete |
| MON-01 | Phase 268 | Complete |
| MON-02 | Phase 268 | Complete |
| MON-03 | Phase 268 | Complete |
| MON-04 | Phase 268 | Complete |

**Coverage:**
- v10.0 requirements: 30 total
- Mapped to phases: 30
- Unmapped: 0

---
*Requirements defined: 2026-03-27*
*Last updated: 2026-03-27 after roadmap creation*
