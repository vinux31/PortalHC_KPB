# Requirements: Portal HC KPB

**Defined:** 2026-03-21
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.1 Requirements

Requirements for Renewal & Assessment Ecosystem Audit milestone. Riset best practices → audit kode dengan lens best practices → fix + improve.

### Best Practices Research

- [x] **RSCH-01**: Riset best practices certificate renewal UX dari platform sejenis (Coursera, LinkedIn Learning, HR portals)
- [x] **RSCH-02**: Riset best practices assessment/exam management dari platform sejenis (Moodle, Google Forms Quiz, Examly)
- [x] **RSCH-03**: Riset best practices real-time exam monitoring UX dari platform sejenis
- [x] **RSCH-04**: Dokumen perbandingan fitur portal vs best practices dengan rekomendasi improvement per halaman

### Renewal — Logic & Data

- [x] **LDAT-01**: Renewal chain FK 4 kombinasi (AS→AS, AS→TR, TR→TR, TR→AS) semua set dengan benar saat renew
- [x] **LDAT-02**: Badge count Admin/Index sinkron dengan BuildRenewalRowsAsync (single source of truth)
- [x] **LDAT-03**: DeriveCertificateStatus handle semua edge case (null ValidUntil, Permanent, expired, akan expired)
- [x] **LDAT-04**: Grouping by Judul case-insensitive dan karakter khusus URL-safe
- [x] **LDAT-05**: MapKategori konsisten dengan AssessmentCategories naming

### Renewal — UI & UX

- [x] **UIUX-01**: Grouped view RenewalCertificate tampil benar dengan data aktual
- [x] **UIUX-02**: Filter cascade Bagian/Unit/Kategori/Tipe berfungsi dan saling terhubung
- [x] **UIUX-03**: Renewal method modal (single + bulk) menampilkan pilihan yang benar berdasarkan tipe
- [x] **UIUX-04**: Certificate history modal menampilkan chain grouping yang akurat

### Renewal — Cross-Page Integration

- [x] **XPAG-01**: CreateAssessment renewal pre-fill (judul, kategori, peserta) dari RenewalCertificate berfungsi
- [x] **XPAG-02**: AddTraining renewal mode (pre-fill + FK) dari RenewalCertificate berfungsi
- [x] **XPAG-03**: CDP Certification Management menyembunyikan renewed certs dengan toggle
- [x] **XPAG-04**: Admin/Index badge count mencerminkan jumlah renewal yang pending

### Renewal — Edge Cases

- [x] **EDGE-01**: Bulk renew mixed-type (campuran Assessment + Training) validasi dan flow benar
- [x] **EDGE-02**: Double renewal prevention — sertifikat yang sudah di-renew tidak bisa di-renew lagi
- [x] **EDGE-03**: Empty state handling saat tidak ada sertifikat yang perlu di-renew

### Assessment Management

- [x] **AMGT-01**: CreateAssessment form validasi lengkap (judul, kategori, tanggal, peserta, passing grade)
- [x] **AMGT-02**: EditAssessment mempertahankan data existing dan warning jika ada package terkait
- [x] **AMGT-03**: DeleteAssessment cascade cleanup benar (packages, questions, sessions, responses)
- [x] **AMGT-04**: Package assignment ke peserta berfungsi (single + bulk assign)
- [x] **AMGT-05**: ManageAssessment list view filter dan search berfungsi

### Assessment Monitoring

- [x] **AMON-01**: AssessmentMonitoring group list menampilkan stats real-time (participant count, completed, passed, status)
- [x] **AMON-02**: MonitoringDetail per-participant live progress (answered/total, status, score, time remaining)
- [x] **AMON-03**: HC actions berfungsi (Reset, Force Close, Bulk Close, Close Early, Regenerate Token)
- [x] **AMON-04**: Token card dengan copy dan regenerate berfungsi

### Assessment Flow — Worker Side

- [x] **AFLW-01**: Worker melihat daftar assessment (Open/Upcoming) sesuai assignment
- [x] **AFLW-02**: StartExam flow benar (token entry → exam page → timer → auto-save per-click)
- [x] **AFLW-03**: SubmitExam menghasilkan score, IsPassed, NomorSertifikat (jika lulus), competency update
- [x] **AFLW-04**: Session resume berfungsi (ElapsedSeconds, LastActivePage, pre-populated answers)
- [x] **AFLW-05**: Results page menampilkan score, pass/fail, answer review (jika diaktifkan HC)

## v8.0 Requirements (Previous)

### Assessment Integrity

- [x] **AINT-01**: Skor ElemenTeknis per session dipersist ke database (tabel SessionElemenTeknisScore) saat SubmitExam dan GradeFromSavedAnswers
- [ ] **AINT-02**: Tab-switch/focus-loss saat ujian terdeteksi dan tercatat di ExamActivityLog sebagai event focus_lost/focus_returned
- [ ] **AINT-03**: HC dapat melihat indikator tab-switch per peserta di halaman AssessmentMonitoringDetail
- [x] **AINT-04**: UserResponse (legacy path) memiliki field SubmittedAt timestamp yang terisi saat SaveLegacyAnswer

### Analytics

- [x] **ANLT-01**: HC dapat melihat halaman Analytics Dashboard dengan visualisasi fail rate per section dan per category
- [x] **ANLT-02**: HC dapat melihat trend assessment (pass/fail) dalam periode waktu tertentu
- [x] **ANLT-03**: HC dapat melihat breakdown skor ElemenTeknis aggregate per kategori assessment
- [x] **ANLT-04**: HC dapat melihat ringkasan sertifikat yang akan expired dalam 30/60/90 hari

### Legacy Cleanup

- [x] **CLEN-01**: TrainingRecord.Status lifecycle terdefinisi jelas — hapus ambiguitas Passed/Valid, transisi terdokumentasi
- [x] **CLEN-02**: Legacy question path (AssessmentQuestion/AssessmentOption/UserResponse) deprecated — existing sessions dimigrasi ke package format
- [x] **CLEN-03**: AssessmentCompetencyMap dan UserCompetencyLevel (orphaned tables) dibersihkan dari database
- [x] **CLEN-04**: NomorSertifikat di-generate saat SubmitExam + IsPassed (bukan saat CreateAssessment)
- [x] **CLEN-05**: Shared AccessToken tetap as-is (documented decision — common exam room pattern)

## Future Requirements

### Training Compliance
- **COMP-01**: Admin dapat mengelola (CRUD) matriks training wajib per jabatan
- **COMP-02**: Compliance percentage per worker dihitung dari training completed vs training required
- **COMP-03**: HC dapat melihat compliance summary per section/unit di team view

### Notification
- **NOTF-01**: Sistem mengirim email reminder otomatis 90 hari sebelum sertifikat expired
- **NOTF-02**: Sistem mengirim email reminder otomatis 30 hari sebelum sertifikat expired
- **NOTF-03**: Sistem mengirim email reminder otomatis 7 hari sebelum sertifikat expired
- **NOTF-04**: NotificationSentLog mencegah email duplikat saat service restart

### Question Bank
- **QBNK-01**: Admin/HC dapat mengelola (CRUD) Question Bank
- **QBNK-02**: Admin/HC dapat import soal dari Excel ke Question Bank
- **QBNK-03**: Saat buat assessment, Admin/HC dapat memilih soal dari Question Bank

### Item Analysis
- **ITEM-01**: Difficulty index dan discrimination index per soal dari data historis
- **ITEM-02**: HC dapat melihat item analysis report per assessment

## Out of Scope

| Feature | Reason |
|---------|--------|
| Renewal via email notification | Deferred ke future milestone (NOTF-*) |
| Training compliance matrix | Deferred ke future milestone (COMP-*) |
| Question bank management | Deferred ke future milestone (QBNK-*) |
| Full UI redesign | Audit + targeted improvement, bukan redesign total |
| Mobile app | Web-first |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| RSCH-01 | Phase 228 | Complete |
| RSCH-02 | Phase 228 | Complete |
| RSCH-03 | Phase 228 | Complete |
| RSCH-04 | Phase 228 | Complete |
| LDAT-01 | Phase 229 | Complete |
| LDAT-02 | Phase 229 | Complete |
| LDAT-03 | Phase 229 | Complete |
| LDAT-04 | Phase 229 | Complete |
| LDAT-05 | Phase 229 | Complete |
| EDGE-01 | Phase 229 | Complete |
| EDGE-02 | Phase 229 | Complete |
| EDGE-03 | Phase 229 | Complete |
| UIUX-01 | Phase 230 | Complete |
| UIUX-02 | Phase 230 | Complete |
| UIUX-03 | Phase 230 | Complete |
| UIUX-04 | Phase 230 | Complete |
| XPAG-01 | Phase 230 | Complete |
| XPAG-02 | Phase 230 | Complete |
| XPAG-03 | Phase 230 | Complete |
| XPAG-04 | Phase 230 | Complete |
| AMGT-01 | Phase 231 | Complete |
| AMGT-02 | Phase 231 | Complete |
| AMGT-03 | Phase 231 | Complete |
| AMGT-04 | Phase 231 | Complete |
| AMGT-05 | Phase 231 | Complete |
| AMON-01 | Phase 231 | Complete |
| AMON-02 | Phase 231 | Complete |
| AMON-03 | Phase 231 | Complete |
| AMON-04 | Phase 231 | Complete |
| AFLW-01 | Phase 232 | Complete |
| AFLW-02 | Phase 232 | Complete |
| AFLW-03 | Phase 232 | Complete |
| AFLW-04 | Phase 232 | Complete |
| AFLW-05 | Phase 232 | Complete |

**Coverage:**
- v8.1 requirements: 34 total
- Mapped to phases: 34
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-22*
*Last updated: 2026-03-22 after scope expansion (assessment pages + research)*
