# Requirements: Portal HC KPB — v7.4 Certification Management

**Defined:** 2026-03-17
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v7.4 Requirements

Requirements for certificate monitoring milestone. Each maps to roadmap phases.

### Monitoring Dashboard

- [ ] **DASH-01**: User can see summary cards showing Total Sertifikat, Aktif, Akan Expired, and Expired counts
- [ ] **DASH-02**: User can see a table listing all certificates with worker name, NIP, certificate name, type, status badge, and expiry date
- [ ] **DASH-03**: Expired and expiring-soon rows are visually highlighted with distinct colors
- [ ] **DASH-04**: User can access Certification Management from a new card on CDP/Index

### Role Scoping

- [ ] **ROLE-01**: Admin and HC can view certificates for all workers across all units
- [ ] **ROLE-02**: SectionHead and Sr. Supervisor can view certificates for workers in their section only
- [ ] **ROLE-03**: Coach and Coachee can view only their own certificates

### Data Integration

- [ ] **DATA-01**: Table includes certificates from TrainingRecord (manual training with uploaded certificates)
- [ ] **DATA-02**: Table includes certificates from AssessmentSession (online assessment, displayed as Permanent status)

### Filtering

- [ ] **FILT-01**: User can filter by Bagian and Unit via cascade dropdown
- [ ] **FILT-02**: User can filter by certificate status (Aktif / Akan Expired / Expired / Permanent)
- [ ] **FILT-03**: User can filter by certificate type (Annual / 3-Year / Permanent)
- [ ] **FILT-04**: User can search by worker name or NIP

### Actions

- [ ] **ACT-01**: User can view a certificate (online cert opens CMP/Certificate page, manual cert opens file)
- [ ] **ACT-02**: User can download a certificate file
- [ ] **ACT-03**: User can export the filtered certificate list to Excel

## Future Requirements

### Notifications

- **NOTF-01**: User receives notification when certificate is expiring within 30 days
- **NOTF-02**: HC receives weekly summary of expiring certificates

### Advanced

- **ADV-01**: Certificate renewal workflow with upload and approval
- **ADV-02**: Expiry countdown column showing days until expiration

## Out of Scope

| Feature | Reason |
|---------|--------|
| Email alerts for expiring certificates | High complexity, requires email infrastructure not yet built |
| Inline editing of certificate data | This is a monitoring page, edits happen in TrainingRecord/Assessment management |
| Certificate generation/creation | Certificates are created via existing TrainingRecord or Assessment flows |
| Mobile app notifications | Web-first, no mobile infrastructure |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DASH-01 | Phase 187 | Pending |
| DASH-02 | Phase 187 | Pending |
| DASH-03 | Phase 187 | Pending |
| DASH-04 | Phase 187 | Pending |
| ROLE-01 | Phase 186 | Pending |
| ROLE-02 | Phase 186 | Pending |
| ROLE-03 | Phase 186 | Pending |
| DATA-01 | Phase 185 | Pending |
| DATA-02 | Phase 185 | Pending |
| FILT-01 | Phase 188 | Pending |
| FILT-02 | Phase 188 | Pending |
| FILT-03 | Phase 188 | Pending |
| FILT-04 | Phase 188 | Pending |
| ACT-01 | Phase 189 | Pending |
| ACT-02 | Phase 189 | Pending |
| ACT-03 | Phase 189 | Pending |

**Coverage:**
- v7.4 requirements: 16 total
- Mapped to phases: 16
- Unmapped: 0

---
*Requirements defined: 2026-03-17*
*Last updated: 2026-03-17 — traceability updated after roadmap creation*
