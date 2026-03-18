# Requirements: Portal HC KPB

**Defined:** 2026-03-18
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.7 Requirements

Requirements for Renewal Certificate & Certificate History milestone.

### Renewal Chain

- [ ] **RENEW-01**: AssessmentSession memiliki field RenewsSessionId (FK self) dan RenewsTrainingId (FK ke TrainingRecord) untuk tracking renewal chain
- [ ] **RENEW-02**: Sertifikat dianggap "sudah di-renew" hanya jika ada AssessmentSession dengan RenewsSessionId/RenewsTrainingId yang mengarah ke sertifikat tersebut DAN IsPassed == true
- [ ] **RENEW-03**: CreateAssessment menerima query param renewSessionId/renewTrainingId — pre-fill Title, Category, peserta, GenerateCertificate=true, ValidUntil wajib

### Renewal Certificate Page

- [ ] **RNPAGE-01**: HC/Admin melihat daftar sertifikat Expired dan Akan Expired yang belum di-renew (tidak ada renewal yang lulus)
- [ ] **RNPAGE-02**: HC/Admin dapat filter berdasarkan Bagian, Unit, Kategori
- [ ] **RNPAGE-03**: Klik Renew pada satu sertifikat → redirect ke CreateAssessment dengan data pre-filled
- [ ] **RNPAGE-04**: Checkbox bulk select + Renew Selected untuk sertifikat dengan kategori sama
- [ ] **RNPAGE-05**: Card Renewal Certificate di Kelola Data Section C

### Certificate History

- [ ] **HIST-01**: Modal timeline riwayat sertifikat per pekerja, grouped by renewal chain (terbaru di atas)
- [ ] **HIST-02**: Di Renewal page, modal history menampilkan tombol Renew pada sertifikat expired/akan expired yang belum di-renew
- [ ] **HIST-03**: Di CDP Certification Management, klik nama pekerja membuka modal history read-only

### CDP Enhancement

- [ ] **CDP-01**: Sertifikat yang sudah di-renew (ada renewal lulus) default tersembunyi di tabel
- [ ] **CDP-02**: Toggle "Tampilkan riwayat" untuk show/hide sertifikat yang sudah di-renew
- [ ] **CDP-03**: Summary card Expired hanya menghitung sertifikat yang belum di-renew

## Out of Scope

| Feature | Reason |
|---------|--------|
| Status "Renewed" sebagai enum baru | Tidak perlu — cek relasi renewal chain cukup, menghindari status terlalu banyak |
| Renewal dari Training ke Training | Renewal selalu via assessment baru — TrainingRecord tidak bisa di-renew ke TrainingRecord lain |
| Notifikasi otomatis sertifikat akan expired | Bisa ditambahkan di milestone berikutnya |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| RENEW-01 | Pending | Pending |
| RENEW-02 | Pending | Pending |
| RENEW-03 | Pending | Pending |
| RNPAGE-01 | Pending | Pending |
| RNPAGE-02 | Pending | Pending |
| RNPAGE-03 | Pending | Pending |
| RNPAGE-04 | Pending | Pending |
| RNPAGE-05 | Pending | Pending |
| HIST-01 | Pending | Pending |
| HIST-02 | Pending | Pending |
| HIST-03 | Pending | Pending |
| CDP-01 | Pending | Pending |
| CDP-02 | Pending | Pending |
| CDP-03 | Pending | Pending |

**Coverage:**
- v7.7 requirements: 14 total
- Mapped to phases: 0
- Unmapped: 14 ⚠️

---
*Requirements defined: 2026-03-18*
*Last updated: 2026-03-18 after milestone v7.7 start*
