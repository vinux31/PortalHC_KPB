# Requirements: Portal HC KPB — v4.0 E2E Use-Case Audit

**Defined:** 2026-03-11
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Audit Format:** Hybrid (Code Review + Browser UAT)

## v4.0 Requirements

### Case 1: Assessment Flow

- [x] **ASSESS-01**: HC can create assessment with all fields (title, category, schedule, duration, pass%, packages, users, token, certificate toggle)
- [x] **ASSESS-02**: HC can import questions via Excel template into assessment packages
- [x] **ASSESS-03**: Worker can view available assessments filtered by status (Open/Upcoming/Completed)
- [x] **ASSESS-04**: Worker can start exam with token verification and complete exam with auto-save
- [x] **ASSESS-05**: Worker can submit exam, view results (score, pass/fail), and review answers if allowed
- [x] **ASSESS-06**: Worker can view/download certificate when GenerateCertificate=true and IsPassed=true
- [x] **ASSESS-07**: HC can monitor live exam progress, reset/force-close/regenerate token
- [x] **ASSESS-08**: Assessment completion creates TrainingRecord and updates competency level

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
| ASSESS-01 | Phase 153 | Complete |
| ASSESS-02 | Phase 153 | Complete |
| ASSESS-03 | Phase 153 | Complete |
| ASSESS-04 | Phase 153 | Complete |
| ASSESS-05 | Phase 153 | Complete |
| ASSESS-06 | Phase 153 | Complete |
| ASSESS-07 | Phase 153 | Complete |
| ASSESS-08 | Phase 153 | Complete |
| PROTON-01 | Phase 154 | Pending |
| PROTON-02 | Phase 154 | Pending |
| PROTON-03 | Phase 154 | Pending |
| PROTON-04 | Phase 154 | Pending |
| PROTON-05 | Phase 154 | Pending |
| PROTON-06 | Phase 154 | Pending |
| PROTON-07 | Phase 154 | Pending |
| ADMIN-01 | Phase 155 | Pending |
| ADMIN-02 | Phase 155 | Pending |
| ADMIN-03 | Phase 155 | Pending |
| ADMIN-04 | Phase 155 | Pending |
| ADMIN-05 | Phase 155 | Pending |
| ADMIN-06 | Phase 155 | Pending |
| CDP-01 | Phase 156 | Pending |
| CDP-02 | Phase 156 | Pending |
| CDP-03 | Phase 156 | Pending |
| CDP-04 | Phase 156 | Pending |
| AUTH-01 | Phase 157 | Pending |
| AUTH-02 | Phase 157 | Pending |
| AUTH-03 | Phase 157 | Pending |
| AUTH-04 | Phase 157 | Pending |
| NAV-01 | Phase 158 | Pending |
| NAV-02 | Phase 158 | Pending |
| NAV-03 | Phase 158 | Pending |
| NAV-04 | Phase 158 | Pending |

**Coverage:**
- v4.0 requirements: 33 total
- Mapped to phases: 33
- Unmapped: 0

---
*Requirements defined: 2026-03-11*
*Last updated: 2026-03-11 after roadmap creation — all 33 requirements mapped to Phases 153-158*
