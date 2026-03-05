# Requirements: Portal HC KPB v3.2

**Defined:** 2026-03-05
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.2 Requirements

Systematically audit all portal pages to identify and fix bugs. Organized by navbar menu for comprehensive coverage.

### Homepage

- [x] **HOME-01**: Homepage renders without errors for all authenticated users
- [x] **HOME-02**: All dashboard cards display correct data (IDP stats, pending assessments, mandatory training)
- [x] **HOME-03**: Recent activities timeline shows correct time ago in Indonesian
- [x] **HOME-04**: Upcoming deadlines are clickable and navigate to correct pages
- [x] **HOME-05**: Date formatting uses Indonesian locale (day names, month names)

### CMP (Competency Management Platform)

- [x] **CMP-01**: Assessment page loads without errors for all roles (Worker, HC, Admin)
- [x] **CMP-02**: Assessment monitoring detail page shows real-time data correctly
- [x] **CMP-03**: Records page displays assessment history and training records with correct pagination
- [x] **CMP-04**: KKJ Matrix page loads correctly with section-based filtering
- [x] **CMP-05**: All CMP forms handle validation errors gracefully (no raw exceptions)
- [x] **CMP-06**: CMP navigation flows work correctly (Create Assessment → Edit → Delete → Monitor)

### CDP (Competency Development Platform)

- [x] **CDP-01**: Plan IDP page loads without errors for all roles (Worker, Coach, Spv, HC, Admin)
- [x] **CDP-02**: Coaching Proton page shows correct coachee lists and deliverable status
- [x] **CDP-03**: Progress page displays correct approval workflows per role
- [ ] **CDP-04**: Evidence upload and download work correctly for deliverables
- [ ] **CDP-05**: Coaching session submission and approval flows work end-to-end
- [ ] **CDP-06**: All CDP forms handle validation errors gracefully

### Kelola Data (Admin Portal)

- [ ] **ADMIN-01**: Manage Workers page loads with correct filters and pagination
- [ ] **ADMIN-02**: Manage Silabus page handles KKJ files correctly (upload, download, archive)
- [ ] **ADMIN-03**: Manage Assessment page shows correct assessment lists and actions
- [ ] **ADMIN-04**: Assessment Monitoring page displays real-time participant data
- [ ] **ADMIN-05**: Coach-Coachee Mapping page works correctly (assign, remove, export)
- [ ] **ADMIN-06**: Proton Data page (Silabus + Coaching Guidance) displays correct tabs
- [ ] **ADMIN-07**: All Admin forms handle validation errors gracefully
- [ ] **ADMIN-08**: Admin role gates work correctly (HC vs Admin access)

### Account (Profile & Settings)

- [x] **ACCT-01**: Profile page displays correct user data (Nama, NIP, Email, Position, Unit)
- [x] **ACCT-02**: Settings page change password works correctly
- [x] **ACCT-03**: Profile edit (FullName, Position) saves correctly
- [x] **ACCT-04**: Avatar initials display correctly from FullName

### Authentication & Authorization

- [x] **AUTH-01**: Login flow works correctly (local and AD modes)
- [x] **AUTH-02**: Inactive users are blocked from login (Phase 83 soft-delete)
- [x] **AUTH-03**: AccessDenied page shows for unauthorized access attempts
- [x] **AUTH-04**: Role-based navigation visibility works correctly
- [x] **AUTH-05**: Return URL redirect after login works correctly and securely

### Data Integrity

- [x] **DATA-01**: All IsActive filters are applied consistently (Workers, Silabus, Assessments)
- [x] **DATA-02**: Soft-delete operations cascade correctly (no orphaned records)
- [x] **DATA-03**: Audit logging captures all HC/Admin actions correctly

## Future Requirements (v3.3+)

Deferred to future milestones. Not in scope for bug hunting.

- **PERF-01**: Performance baseline and load testing for concurrent exam sessions
- **AUTO-01**: Automated test suite (xUnit + WebApplicationFactory) for regression
- **MOBILE-01**: Mobile-responsive assessment forms
- **NOTIF-01**: Email notifications for assessment assignments and coaching approvals
- **ESCAL-01**: Approval auto-escalation after timeout period

## Out of Scope

| Feature | Reason |
|---------|--------|
| New features | This is bug hunting only — no new functionality |
| UI redesign | Fix bugs only, no visual overhauls unless broken |
| Performance optimization | Only fix critical performance bugs, not general optimization |
| Database migrations | Only fix data integrity bugs, no schema changes |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| HOME-01 | Phase 92 | Complete |
| HOME-02 | Phase 92 | Complete |
| HOME-03 | Phase 92 | Complete |
| HOME-04 | Phase 92 | Complete |
| HOME-05 | Phase 92 | Complete |
| CMP-01 | Phase 93 | Complete |
| CMP-02 | Phase 93 | Complete |
| CMP-03 | Phase 93 | Complete |
| CMP-04 | Phase 93 | Complete |
| CMP-05 | Phase 93 | Complete |
| CMP-06 | Phase 93 | Complete |
| CDP-01 | Phase 94 | Complete |
| CDP-02 | Phase 94 | Complete |
| CDP-03 | Phase 94 | Complete |
| CDP-04 | Phase 94 | Pending |
| CDP-05 | Phase 94 | Pending |
| CDP-06 | Phase 94 | Pending |
| ADMIN-01 | Phase 95 | Pending |
| ADMIN-02 | Phase 95 | Pending |
| ADMIN-03 | Phase 95 | Pending |
| ADMIN-04 | Phase 95 | Pending |
| ADMIN-05 | Phase 95 | Pending |
| ADMIN-06 | Phase 95 | Pending |
| ADMIN-07 | Phase 95 | Pending |
| ADMIN-08 | Phase 95 | Pending |
| ACCT-01 | Phase 96 | Complete |
| ACCT-02 | Phase 96 | Complete |
| ACCT-03 | Phase 96 | Complete |
| ACCT-04 | Phase 96 | Complete |
| AUTH-01 | Phase 97 | Complete |
| AUTH-02 | Phase 97 | Complete |
| AUTH-03 | Phase 97 | Complete |
| AUTH-04 | Phase 97 | Complete |
| AUTH-05 | Phase 97 | Complete |
| DATA-01 | Phase 98 | Complete |
| DATA-02 | Phase 98 | Complete |
| DATA-03 | Phase 98 | Complete |

**Coverage:**
- v3.2 requirements: 40 total
- Mapped to phases: 40 (100%)
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-05*
*Last updated: 2026-03-05 — initial v3.2 requirements*
