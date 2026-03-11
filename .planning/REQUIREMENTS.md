# Requirements: Portal HC KPB — v4.0 E2E Use-Case Audit

**Defined:** 2026-03-11
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Audit Format:** Hybrid (Code Review + Browser UAT)

## v4.0 Requirements

### Case 1: Assessment Flow

- [ ] **ASSESS-01**: HC can create assessment with all fields (title, category, schedule, duration, pass%, packages, users, token, certificate toggle)
- [ ] **ASSESS-02**: HC can import questions via Excel template into assessment packages
- [ ] **ASSESS-03**: Worker can view available assessments filtered by status (Open/Upcoming/Completed)
- [ ] **ASSESS-04**: Worker can start exam with token verification and complete exam with auto-save
- [ ] **ASSESS-05**: Worker can submit exam, view results (score, pass/fail), and review answers if allowed
- [ ] **ASSESS-06**: Worker can view/download certificate when GenerateCertificate=true and IsPassed=true
- [ ] **ASSESS-07**: HC can monitor live exam progress, reset/force-close/regenerate token
- [ ] **ASSESS-08**: Assessment completion creates TrainingRecord and updates competency level

### Case 2: Coaching Proton Flow

- [ ] **PROTON-01**: HC can create coach-coachee mapping with section/unit assignment
- [ ] **PROTON-02**: Coachee can view assigned deliverables and upload evidence
- [ ] **PROTON-03**: Coach can create coaching session with notes, conclusion, and action items
- [ ] **PROTON-04**: SrSpv/SectionHead can approve or reject deliverables with reason
- [ ] **PROTON-05**: HC can review deliverables and track overall progress
- [ ] **PROTON-06**: HC can create Assessment Proton (Tahun 1-2 online, Tahun 3 interview)
- [ ] **PROTON-07**: Histori Proton timeline shows complete coachee journey

### Case 3: Admin Kelola Data

- [ ] **ADMIN-01**: HC/Admin can CRUD workers (create, edit, deactivate, delete with cascade)
- [ ] **ADMIN-02**: HC/Admin can bulk import workers via Excel template
- [ ] **ADMIN-03**: HC/Admin can manage KKJ files (upload, download, archive per bagian)
- [ ] **ADMIN-04**: HC/Admin can manage CPDP files (upload, download, archive per bagian)
- [ ] **ADMIN-05**: HC/Admin can manage Proton Data (silabus CRUD, guidance upload, override status)
- [ ] **ADMIN-06**: Audit log records all admin actions with actor, timestamp, details

### Case 4: PlanIDP & CDP Dashboard

- [ ] **CDP-01**: Coachee sees assigned track silabus with deliverable targets
- [ ] **CDP-02**: HC/Admin can browse any section/unit/track silabus
- [ ] **CDP-03**: Coaching guidance files downloadable per bagian/unit/track
- [ ] **CDP-04**: CDP Dashboard shows role-scoped progress metrics and drill-down

### Case 5: Account & Auth

- [ ] **AUTH-01**: User can login (local + AD mode) with inactive user block
- [ ] **AUTH-02**: User can view profile with correct role/section/unit data
- [ ] **AUTH-03**: User can change password and edit profile fields
- [ ] **AUTH-04**: Unauthorized access redirects to AccessDenied page

### Case 6: Homepage & Navigation

- [ ] **NAV-01**: Homepage shows personalized dashboard with progress bars and upcoming events
- [ ] **NAV-02**: Role-scoped navbar shows correct menu items per role
- [ ] **NAV-03**: Guide pages accessible with role-appropriate content
- [ ] **NAV-04**: All navigation links resolve to correct pages (no dead links)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Automated Playwright scripts | Audit is code review + manual browser UAT, not automated testing |
| Performance/load testing | Focus is functional correctness, not performance |
| Database migration audit | Schema is stable, focus on application layer |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ASSESS-01 | — | Pending |
| ASSESS-02 | — | Pending |
| ASSESS-03 | — | Pending |
| ASSESS-04 | — | Pending |
| ASSESS-05 | — | Pending |
| ASSESS-06 | — | Pending |
| ASSESS-07 | — | Pending |
| ASSESS-08 | — | Pending |
| PROTON-01 | — | Pending |
| PROTON-02 | — | Pending |
| PROTON-03 | — | Pending |
| PROTON-04 | — | Pending |
| PROTON-05 | — | Pending |
| PROTON-06 | — | Pending |
| PROTON-07 | — | Pending |
| ADMIN-01 | — | Pending |
| ADMIN-02 | — | Pending |
| ADMIN-03 | — | Pending |
| ADMIN-04 | — | Pending |
| ADMIN-05 | — | Pending |
| ADMIN-06 | — | Pending |
| CDP-01 | — | Pending |
| CDP-02 | — | Pending |
| CDP-03 | — | Pending |
| CDP-04 | — | Pending |
| AUTH-01 | — | Pending |
| AUTH-02 | — | Pending |
| AUTH-03 | — | Pending |
| AUTH-04 | — | Pending |
| NAV-01 | — | Pending |
| NAV-02 | — | Pending |
| NAV-03 | — | Pending |
| NAV-04 | — | Pending |

**Coverage:**
- v4.0 requirements: 27 total
- Mapped to phases: 0
- Unmapped: 27 ⚠️

---
*Requirements defined: 2026-03-11*
*Last updated: 2026-03-11 after initial definition*
