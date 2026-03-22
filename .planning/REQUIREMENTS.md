# Requirements: Portal HC KPB

**Defined:** 2026-03-21
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.1 Requirements

Requirements for RenewalCertificate Page Audit milestone. Audit menyeluruh renewal ecosystem — logic, UI, cross-page integration, edge cases.

### Logic & Data

- [ ] **LDAT-01**: Renewal chain FK 4 kombinasi (AS→AS, AS→TR, TR→TR, TR→AS) semua set dengan benar saat renew
- [ ] **LDAT-02**: Badge count Admin/Index sinkron dengan BuildRenewalRowsAsync (single source of truth)
- [ ] **LDAT-03**: DeriveCertificateStatus handle semua edge case (null ValidUntil, Permanent, expired, akan expired)
- [ ] **LDAT-04**: Grouping by Judul case-insensitive dan karakter khusus URL-safe
- [ ] **LDAT-05**: MapKategori konsisten dengan AssessmentCategories naming

### UI & UX

- [ ] **UIUX-01**: Grouped view RenewalCertificate tampil benar dengan data aktual
- [ ] **UIUX-02**: Filter cascade Bagian/Unit/Kategori/Tipe berfungsi dan saling terhubung
- [ ] **UIUX-03**: Renewal method modal (single + bulk) menampilkan pilihan yang benar berdasarkan tipe
- [ ] **UIUX-04**: Certificate history modal menampilkan chain grouping yang akurat

### Cross-Page Integration

- [ ] **XPAG-01**: CreateAssessment renewal pre-fill (judul, kategori, peserta) dari RenewalCertificate berfungsi
- [ ] **XPAG-02**: AddTraining renewal mode (pre-fill + FK) dari RenewalCertificate berfungsi
- [ ] **XPAG-03**: CDP Certification Management menyembunyikan renewed certs dengan toggle
- [ ] **XPAG-04**: Admin/Index badge count mencerminkan jumlah renewal yang pending

### Edge Cases

- [ ] **EDGE-01**: Bulk renew mixed-type (campuran Assessment + Training) validasi dan flow benar
- [ ] **EDGE-02**: Double renewal prevention — sertifikat yang sudah di-renew tidak bisa di-renew lagi
- [ ] **EDGE-03**: Empty state handling saat tidak ada sertifikat yang perlu di-renew

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
| New renewal UI redesign | Audit saja, bukan redesign |
| Mobile app | Web-first |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LDAT-01 | TBD | Pending |
| LDAT-02 | TBD | Pending |
| LDAT-03 | TBD | Pending |
| LDAT-04 | TBD | Pending |
| LDAT-05 | TBD | Pending |
| UIUX-01 | TBD | Pending |
| UIUX-02 | TBD | Pending |
| UIUX-03 | TBD | Pending |
| UIUX-04 | TBD | Pending |
| XPAG-01 | TBD | Pending |
| XPAG-02 | TBD | Pending |
| XPAG-03 | TBD | Pending |
| XPAG-04 | TBD | Pending |
| EDGE-01 | TBD | Pending |
| EDGE-02 | TBD | Pending |
| EDGE-03 | TBD | Pending |

**Coverage:**
- v8.1 requirements: 16 total
- Mapped to phases: 0
- Unmapped: 16 ⚠️

---
*Requirements defined: 2026-03-22*
*Last updated: 2026-03-22 after v8.1 milestone definition*
