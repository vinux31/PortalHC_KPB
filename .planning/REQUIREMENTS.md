# Requirements: Portal HC KPB — v7.5 Assessment Form Revamp & Certificate Enhancement

**Defined:** 2026-03-17
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.5 Requirements

Requirements for assessment form revamp and certificate enhancement. Each maps to roadmap phases.

### Assessment Form UX

- [ ] **FORM-01**: Admin/HC can create assessment melalui wizard step-based (Kategori → Users → Settings → Konfirmasi)
- [ ] **FORM-02**: Admin dapat mengelola kategori assessment dari database (CRUD) tanpa perlu edit code
- [ ] **FORM-03**: Admin/HC dapat membuat assessment baru dari duplikasi assessment yang sudah ada (clone)

### Certificate Enhancement

- [ ] **CERT-01**: Admin/HC dapat mengatur tanggal expired (ValidUntil) pada sertifikat assessment online
- [ ] **CERT-02**: Sistem men-generate nomor sertifikat otomatis saat sertifikat terbit (format: CERT-{TAHUN}-{SEQ})
- [ ] **CERT-03**: User dapat download sertifikat sebagai file PDF (server-side via QuestPDF)

## Future Requirements

### Assessment Workflow (v7.6+)

- **TMPL-01**: Admin/HC dapat menyimpan assessment template/preset untuk konfigurasi yang sering dipakai
- **SCHED-01**: Admin/HC dapat membuat recurring assessment dengan jadwal berulang otomatis

### Certificate Advanced (v7.6+)

- **QRCODE-01**: Sertifikat memiliki QR code untuk verifikasi keaslian
- **RENEW-01**: Workflow renewal sertifikat expired (notifikasi → upload baru → approval)
- **NOTIF-01**: Notifikasi otomatis X hari sebelum sertifikat expired

## Out of Scope

| Feature | Reason |
|---------|--------|
| Assessment template/preset | Terlalu kompleks untuk 1 milestone, defer ke v7.6+ |
| Recurring scheduling | Butuh infrastruktur scheduler baru, milestone sendiri |
| QR code verifikasi | Nice-to-have, bukan prioritas saat ini |
| Expiry notification | Butuh infrastruktur notifikasi baru |
| Certificate revocation | Workflow terlalu kompleks, belum ada kebutuhan |
| Multi-page certificate layout | Satu halaman sertifikat sudah cukup |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| FORM-01 | — | Pending |
| FORM-02 | — | Pending |
| FORM-03 | — | Pending |
| CERT-01 | — | Pending |
| CERT-02 | — | Pending |
| CERT-03 | — | Pending |

**Coverage:**
- v7.5 requirements: 6 total
- Mapped to phases: 0
- Unmapped: 6 ⚠️

---
*Requirements defined: 2026-03-17*
*Last updated: 2026-03-17 after initial definition*
