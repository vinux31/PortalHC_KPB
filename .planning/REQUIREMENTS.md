# Requirements: Portal HC KPB

**Defined:** 2026-03-21
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.0 Requirements

Requirements for Assessment & Training System Audit milestone. Each maps to roadmap phases.

### Assessment Integrity

- [x] **AINT-01**: Skor ElemenTeknis per session dipersist ke database (tabel SessionElemenTeknisScore) saat SubmitExam dan GradeFromSavedAnswers
- [x] **AINT-02**: Tab-switch/focus-loss saat ujian terdeteksi dan tercatat di ExamActivityLog sebagai event focus_lost/focus_returned
- [x] **AINT-03**: HC dapat melihat indikator tab-switch per peserta di halaman AssessmentMonitoringDetail
- [x] **AINT-04**: UserResponse (legacy path) memiliki field SubmittedAt timestamp yang terisi saat SaveLegacyAnswer

### Analytics

- [ ] **ANLT-01**: HC dapat melihat halaman Analytics Dashboard dengan visualisasi fail rate per section dan per category
- [ ] **ANLT-02**: HC dapat melihat trend assessment (pass/fail) dalam periode waktu tertentu
- [ ] **ANLT-03**: HC dapat melihat breakdown skor ElemenTeknis aggregate per kategori assessment
- [ ] **ANLT-04**: HC dapat melihat ringkasan sertifikat yang akan expired dalam 30/60/90 hari

### Training Compliance

- [ ] **COMP-01**: Admin dapat mengelola (CRUD) matriks training wajib per jabatan (tabel RequiredTraining: PositionTitle × SubKategori)
- [ ] **COMP-02**: Compliance percentage per worker dihitung dari training completed vs training required (bukan training di DB)
- [ ] **COMP-03**: HC dapat melihat compliance summary per section/unit di team view

### Notification

- [ ] **NOTF-01**: Sistem mengirim email reminder otomatis 90 hari sebelum sertifikat expired
- [ ] **NOTF-02**: Sistem mengirim email reminder otomatis 30 hari sebelum sertifikat expired
- [ ] **NOTF-03**: Sistem mengirim email reminder otomatis 7 hari sebelum sertifikat expired
- [ ] **NOTF-04**: NotificationSentLog mencegah email duplikat saat service restart

### Question Bank

- [ ] **QBNK-01**: Admin/HC dapat mengelola (CRUD) Question Bank — library soal terpisah dari assessment session
- [ ] **QBNK-02**: Admin/HC dapat import soal dari Excel ke Question Bank
- [ ] **QBNK-03**: Saat buat assessment, Admin/HC dapat memilih soal dari Question Bank (copy-by-value ke PackageQuestion)

### Legacy Cleanup

- [x] **CLEN-01**: TrainingRecord.Status lifecycle terdefinisi jelas — hapus ambiguitas Passed/Valid, transisi terdokumentasi
- [ ] **CLEN-02**: Legacy question path (AssessmentQuestion/AssessmentOption/UserResponse) deprecated — existing sessions dimigrasi ke package format
- [ ] **CLEN-03**: AssessmentCompetencyMap dan UserCompetencyLevel (orphaned tables) dibersihkan dari database
- [ ] **CLEN-04**: NomorSertifikat di-generate saat SubmitExam + IsPassed (bukan saat CreateAssessment)
- [x] **CLEN-05**: Shared AccessToken tetap as-is (documented decision — common exam room pattern)

## Future Requirements

### Item Analysis
- **ITEM-01**: Difficulty index dan discrimination index per soal dari data historis
- **ITEM-02**: HC dapat melihat item analysis report per assessment

## Out of Scope

| Feature | Reason |
|---------|--------|
| SCORM/xAPI integration | Terlalu enterprise, ini internal portal |
| Per-user unique token | Shared token sudah cukup untuk exam room setting (CLEN-05) |
| Proton/Coaching audit | Explicitly excluded dari scope v8.0 |
| Mobile app | Web-first, tidak dalam scope |
| Multi-tenant | Single organization (Pertamina KPB) |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| AINT-01 | Phase 223 | Complete |
| AINT-02 | Phase 223 | Complete |
| AINT-03 | Phase 223 | Complete |
| AINT-04 | Phase 223 | Complete |
| CLEN-01 | Phase 223 | Complete |
| ANLT-01 | Phase 224 | Pending |
| ANLT-02 | Phase 224 | Pending |
| ANLT-03 | Phase 224 | Pending |
| ANLT-04 | Phase 224 | Pending |
| COMP-01 | Phase 225 | Pending |
| COMP-02 | Phase 225 | Pending |
| COMP-03 | Phase 225 | Pending |
| NOTF-01 | Phase 226 | Pending |
| NOTF-02 | Phase 226 | Pending |
| NOTF-03 | Phase 226 | Pending |
| NOTF-04 | Phase 226 | Pending |
| QBNK-01 | Phase 227 | Pending |
| QBNK-02 | Phase 227 | Pending |
| QBNK-03 | Phase 227 | Pending |
| CLEN-02 | Phase 227 | Pending |
| CLEN-03 | Phase 227 | Pending |
| CLEN-04 | Phase 227 | Pending |
| CLEN-05 | Phase 223 | Complete |

**Coverage:**
- v8.0 requirements: 23 total
- Mapped to phases: 23
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-21*
*Last updated: 2026-03-21 after initial definition*
