# Requirements: Portal HC KPB — v3.16 Form Coaching GAST Redesign

**Defined:** 2026-03-09
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.16 Requirements

### Modal Form Evidence (FORM)

- [ ] **FORM-01**: Modal form evidence coaching memiliki bagian Acuan yang grouped (Pedoman, TKO/TKI/TKPA, Best Practice, Dokumen) sebagai textarea
- [ ] **FORM-02**: Data Acuan tersimpan di database (CoachingSession model + migration)
- [ ] **FORM-03**: JS submit handler mengirim 4 field Acuan baru ke controller dan tersimpan

### Export PDF (PDF)

- [ ] **PDF-01**: Export PDF menggunakan layout 3-column table (Acuan / Catatan Coach / Kesimpulan dari Coach) sesuai Form GAST
- [ ] **PDF-02**: Kesimpulan dan Result ditampilkan sebagai checkbox (checked sesuai value)
- [ ] **PDF-03**: TTD Coach + Nopeg di bagian bawah (tanpa TTD Coachee)
- [ ] **PDF-04**: Header (SUB.KOMPETENSI, DELIVERABLES, Tanggal) dan footer branding Pertamina (logo, red wave, www.pertamina.com)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Coachee's Competencies column | Tidak dibutuhkan per user request |
| TTD Coachee di PDF | Hanya TTD Coach yang diperlukan |
| Perubahan approval chain | Fokus hanya pada form dan PDF |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FORM-01 | Phase 143 | Pending |
| FORM-02 | Phase 143 | Pending |
| FORM-03 | Phase 143 | Pending |
| PDF-01 | Phase 144 | Pending |
| PDF-02 | Phase 144 | Pending |
| PDF-03 | Phase 144 | Pending |
| PDF-04 | Phase 144 | Pending |

**Coverage:**
- v3.16 requirements: 7 total
- Mapped to phases: 7
- Unmapped: 0

---
*Requirements defined: 2026-03-09*
*Last updated: 2026-03-09 after initial definition*
