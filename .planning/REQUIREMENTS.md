# Requirements: Portal HC KPB

**Defined:** 2026-03-23
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.4 Requirements

Requirements for milestone v8.4: Alarm Sertifikat Expired.

### Banner Alert

- [ ] **ALRT-01**: HC/Admin melihat alert banner di Home/Index yang menampilkan jumlah sertifikat Expired dan Akan Expired (≤30 hari)
- [ ] **ALRT-02**: Banner menampilkan count Expired (merah) dan count Akan Expired (kuning) terpisah
- [ ] **ALRT-03**: Banner memiliki link "Lihat Detail" yang mengarah ke RenewalCertificate
- [ ] **ALRT-04**: Banner tidak tampil jika tidak ada sertifikat expired maupun akan expired

### Bell Notification

- [ ] **NOTF-01**: Saat HC/Admin buka Home/Index, sistem generate UserNotification tipe CERT_EXPIRED untuk sertifikat expired yang belum punya notifikasi
- [ ] **NOTF-02**: Notifikasi CERT_EXPIRED dikirim ke semua user dengan role HC atau Admin
- [ ] **NOTF-03**: Notifikasi CERT_EXPIRED muncul di bell dropdown dengan nama pekerja dan judul sertifikat

## Validated Requirements (Previous Milestones)

### v8.3 — Date Range Filter Team View Records
- [x] FILT-01..06, EXP-01..02 — All complete (Phase 239)

## Future Requirements

None for this milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| CERT_EXPIRING_SOON notification | User memilih hanya CERT_EXPIRED di bell, akan expired cukup di banner saja |
| Background job / scheduler | Notifikasi di-generate on page load, tidak perlu background service |
| Email notification | Scope hanya in-app (banner + bell) |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ALRT-01 | TBD | Pending |
| ALRT-02 | TBD | Pending |
| ALRT-03 | TBD | Pending |
| ALRT-04 | TBD | Pending |
| NOTF-01 | TBD | Pending |
| NOTF-02 | TBD | Pending |
| NOTF-03 | TBD | Pending |

**Coverage:**
- v8.4 requirements: 7 total
- Mapped to phases: 0
- Unmapped: 7

---
*Requirements defined: 2026-03-23*
*Last updated: 2026-03-23*
