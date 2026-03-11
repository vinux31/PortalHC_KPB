# Requirements: Portal HC KPB — v3.19 Assessment Certificate Toggle

**Defined:** 2026-03-11
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.19 Requirements

### Certificate Toggle

- [ ] **CERT-01**: HC can enable/disable certificate generation when creating an assessment (toggle "Terbitkan Sertifikat", default OFF) — *user decision overrides initial spec; new assessments default to GenerateCertificate = false*
- [ ] **CERT-02**: HC can edit the certificate toggle on existing assessments via EditAssessment
- [ ] **CERT-03**: Results page hides "View Certificate" button when GenerateCertificate is false, even if worker passed
- [ ] **CERT-04**: Certificate action returns 404 when GenerateCertificate is false (server-side guard)
- [ ] **CERT-05**: All existing assessments retain certificate access (migration default = true)
- [ ] **CERT-06**: Training Records views hide certificate link/column (show dash) when GenerateCertificate is false for assessment rows

## Out of Scope

| Feature | Reason |
|---------|--------|
| PDF certificate file storage | Current HTML-based on-demand generation is sufficient |
| Certificate template customization | Not requested, future enhancement |
| Per-worker certificate override | Toggle is per-assessment, not per-worker |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CERT-01 | Phase 150 | Pending |
| CERT-02 | Phase 150 | Pending |
| CERT-03 | Phase 150 | Pending |
| CERT-04 | Phase 150 | Pending |
| CERT-05 | Phase 150 | Pending |
| CERT-06 | Phase 150 | Pending |

**Coverage:**
- v3.19 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0

---
*Requirements defined: 2026-03-11*
*CERT-01 default updated 2026-03-11: user decision (discuss-phase) overrides initial spec — new assessments default OFF*
*CERT-06 added 2026-03-11: Training Records flag respect identified during planning review*
