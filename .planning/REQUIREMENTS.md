# Requirements: Portal HC KPB — v7.5 Assessment Form Revamp & Certificate Enhancement

**Defined:** 2026-03-17
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.5 Requirements

Requirements for assessment form revamp and certificate enhancement. Each maps to roadmap phases.

### Assessment Form UX

- [x] **FORM-01**: Admin/HC can create assessment melalui wizard step-based (Kategori → Users → Settings → Konfirmasi)
- [x] **FORM-02**: Admin dapat mengelola kategori assessment dari database (CRUD) tanpa perlu edit code
- [ ] **FORM-03**: Admin/HC dapat membuat assessment baru dari duplikasi assessment yang sudah ada (clone)

### Certificate Enhancement

- [x] **CERT-01**: Admin/HC dapat mengatur tanggal expired (ValidUntil) pada sertifikat assessment online
- [x] **CERT-02**: Sistem men-generate nomor sertifikat otomatis saat sertifikat terbit (format: CERT-{TAHUN}-{SEQ})
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
| FORM-01 | Phase 191 | Complete |
| FORM-02 | Phase 190 | Complete |
| FORM-03 | Phase 193 | Pending |
| CERT-01 | Phase 192 | Complete |
| CERT-02 | Phase 192 | Complete |
| CERT-03 | Phase 194 | Pending |

**Coverage:**
- v7.5 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-17*
*Last updated: 2026-03-17 after roadmap creation*
