# Requirements: Portal HC KPB v3.1

**Defined:** 2026-03-03
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.1 Requirements

Rewrite Admin/CpdpItems spreadsheet editor and CMP/Mapping read-only table into file-based document management system, following the Phase 90 KKJ Matrix pattern (KkjBagian + KkjFile).

### CPDP File Management (Admin)

- [ ] **CPDP-01**: Admin/HC can upload CPDP document files (PDF, XLSX, XLS) per section with optional description
- [ ] **CPDP-02**: Admin/HC can download and soft-delete (archive) CPDP files, with file history view per section
- [ ] **CPDP-03**: Admin/HC can manage sections (add/delete bagian tabs) on the CPDP admin page

### CPDP Mapping View (Worker)

- [ ] **CPDP-04**: All authenticated users can view and download CPDP files per section on CMP/Mapping page
- [ ] **CPDP-05**: CMP/Mapping page supports role-based section filtering (L1-L4 see all sections, L5-L6 see own unit only)

### Data Migration

- [x] **CPDP-06**: Existing CpdpItem data exported to Excel backup file before migration
- [ ] **CPDP-07**: CpdpItem table and related spreadsheet CRUD actions removed after file-based system is verified

## Future Requirements (v3.2+)

- **PERF-01**: Performance baseline and load testing for concurrent exam sessions
- **AUTO-01**: Automated test suite (xUnit + WebApplicationFactory) for regression
- **MOBILE-01**: Mobile-responsive assessment forms
- **NOTIF-01**: Email notifications for assessment assignments and coaching approvals
- **ESCAL-01**: Approval auto-escalation after timeout period
- **ACCT-01**: QA Account/Profile page (view and edit FullName, Position)
- **ACCT-02**: QA Account/Settings page (change password)

## Out of Scope

| Feature | Reason |
|---------|--------|
| IdpItem table changes | IdpItem.Kompetensi kept as standalone string — no FK to CpdpItem |
| Plan IDP page changes | Plan IDP uses ProtonTrack/Silabus, not CpdpItem |
| Admin/ProtonData changes | Silabus & Coaching Guidance are separate from CPDP |
| CpdpItem import feature | Replacing with file-based; no need for spreadsheet import |
| MappingSectionSelect redesign | Section selection folded into new CMP/Mapping dropdown |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CPDP-01 | Phase 92 | Pending |
| CPDP-02 | Phase 92 | Pending |
| CPDP-03 | Phase 92 | Pending |
| CPDP-04 | Phase 93 | Pending |
| CPDP-05 | Phase 93 | Pending |
| CPDP-06 | Phase 91 | Complete |
| CPDP-07 | Phase 93 | Pending |

**Coverage:**
- v3.1 requirements: 7 total
- Mapped to phases: 7 (100%)
- Unmapped: 0

---
*Requirements defined: 2026-03-03*
*Last updated: 2026-03-03 — traceability updated after roadmap creation*
