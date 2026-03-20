# Requirements: Portal HC KPB

**Defined:** 2026-03-20
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.10 Requirements

### Bug Fixes — Renewal Chain

- [ ] **FIX-01**: Bulk renew menetapkan RenewsSessionId/RenewsTrainingId ke semua user yang dipilih (bukan hanya user[0])
- [ ] **FIX-02**: Badge count di Admin/Index sinkron dengan BuildRenewalRowsAsync (termasuk TR→AS dan TR→TR renewal)
- [ ] **FIX-03**: renewedByTrSessionIds memfilter hanya TrainingRecord yang IsPassed

- [ ] **FIX-04**: AddTraining mendukung renewal chain (set RenewsTrainingId/RenewsSessionId sesuai sumber)

### Bug Fixes — Data & Display

- [ ] **FIX-05**: ValidUntil=null dengan CertificateType bukan "Permanent" tidak salah dianggap Permanent (hilang dari renewal list)
- [ ] **FIX-06**: Renew dari TrainingRecord: Category di-prefill otomatis sesuai sertifikat asal
- [ ] **FIX-07**: MapKategori konsisten dengan AssessmentCategories name
- [ ] **FIX-08**: Grouping by Judul case-insensitive
- [ ] **FIX-09**: Judul dengan karakter khusus aman di URL (encode/decode tidak gagal match)
- [ ] **FIX-10**: ValidUntil=null di renewal mode menampilkan error message informatif

### Enhancement — Tipe Filter & Renewal Flow

- [ ] **ENH-01**: Filter tipe (Assessment / Training) pada halaman RenewalCertificate
- [ ] **ENH-02**: Renewal flow berdasarkan tipe — Training→popup pilihan (renew via assessment ATAU via training record baru)
- [ ] **ENH-03**: Bulk renew aware tipe — tidak langsung ke CreateAssessment kalau ada training items
- [ ] **ENH-04**: AddTraining renewal mode dengan pre-fill data sertifikat asal + set FK

## Out of Scope

| Feature | Reason |
|---------|--------|
| Redesign filter bar | Filter bar existing sudah cukup, hanya tambah filter tipe |
| Redesign grouped view | Grouped view v7.9 sudah shipped, hanya fix bugs |
| Certificate History modal changes | Modal existing sudah berfungsi baik |
| DB migration untuk renewal chain | Kolom FK sudah ada, hanya fix logic yang belum set |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FIX-01 | Phase 210 | Pending |
| FIX-02 | Phase 210 | Pending |
| FIX-03 | Phase 210 | Pending |
| FIX-04 | Phase 212 | Pending |
| FIX-05 | Phase 211 | Pending |
| FIX-06 | Phase 211 | Pending |
| FIX-07 | Phase 211 | Pending |
| FIX-08 | Phase 211 | Pending |
| FIX-09 | Phase 211 | Pending |
| FIX-10 | Phase 211 | Pending |
| ENH-01 | Phase 212 | Pending |
| ENH-02 | Phase 212 | Pending |
| ENH-03 | Phase 212 | Pending |
| ENH-04 | Phase 212 | Pending |

**Coverage:**
- v7.10 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-20*
*Last updated: 2026-03-20 — traceability mapped after roadmap creation*
