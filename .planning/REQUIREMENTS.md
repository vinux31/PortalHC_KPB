# Requirements: Portal HC KPB v3.0

**Defined:** 2026-03-01
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.0 Requirements

Comprehensive end-to-end QA of all portal features organized by use-case flows, code cleanup, rename, and Plan IDP development. Each QA requirement = "verify feature works correctly for all applicable roles, fix any bugs found."

### Cleanup & Rename

- [x] **CLN-01**: Rename "Proton Progress" → "Coaching Proton" in all views, nav entries, hub cards, breadcrumbs, and page titles
- [x] **CLN-02**: Remove orphaned CMP/CpdpProgress page (action + view)
- [x] **CLN-03**: Remove duplicate CMP/CreateTrainingRecord (action + view — Admin/AddTraining is canonical)
- [x] **CLN-04**: Remove duplicate CMP/ManageQuestions (action + view — Admin/ManageQuestions is canonical)
- [x] **CLN-05**: Add AuditLog card to Kelola Data hub with proper role gating
- [x] **CLN-06**: Analyze Override Silabus & Coaching Guidance tabs — remove if unnecessary, document if keeping

### Assessment Flow (End-to-End)

- [ ] **ASSESS-01**: HC/Admin can create assessment with all fields (title, category, schedule, threshold, answer review toggle) and assign workers
- [ ] **ASSESS-02**: HC/Admin can edit and delete assessments with proper warnings (schedule change with packages, cascade cleanup)
- [ ] **ASSESS-03**: Worker can verify token, start exam, auto-save answers per click, and resume from exact page with accurate remaining time
- [ ] **ASSESS-04**: Worker can submit exam and view results with score, pass/fail, conditional answer review, and earned competencies
- [ ] **ASSESS-05**: Worker can view certificate after passing assessment
- [ ] **ASSESS-06**: HC can monitor assessment in real-time (live progress, status, score, countdown per worker)
- [ ] **ASSESS-07**: HC can execute monitoring actions: force close, reset, bulk close, close early, regenerate token, reshuffle packages
- [ ] **ASSESS-08**: HC/Admin can create packages, import questions from Excel/paste, preview packages, and cross-package shuffle works correctly
- [ ] **ASSESS-09**: Training Records page shows correct personal assessment + training history with filters
- [ ] **ASSESS-10**: Admin/ManageAssessment 3-tab view works correctly (Assessment Group, Training Records, Assessment Monitoring entry)

### Coaching Proton Flow (End-to-End)

- [ ] **COACH-01**: Admin/HC can assign, edit, deactivate, reactivate coach-coachee mappings with proper validation
- [ ] **COACH-02**: Coach-Coachee mapping export to Excel works correctly
- [ ] **COACH-03**: Coachee can view their coaching progress with deliverable statuses, evidence uploads, and approval states
- [ ] **COACH-04**: Coach can select coachee, upload evidence with coaching log, and view approval statuses
- [ ] **COACH-05**: SrSpv/SectionHead/HC can approve or reject deliverables with proper role-scoping in approval chain
- [ ] **COACH-06**: Individual deliverable detail page shows complete info (status, evidence, coaching report, approval history)
- [ ] **COACH-07**: HC/Admin can override stuck deliverable progress via Coaching Proton Override tab
- [ ] **COACH-08**: Progress export (Excel + PDF) works correctly for authorized roles

### Master Data Management

- [ ] **DATA-01**: KKJ Matrix spreadsheet editor works (CRUD, bulk save, bagian management) and data links correctly to CMP/Kkj view
- [x] **DATA-02**: KKJ-IDP Mapping editor works (CRUD, bulk save, export) and data links correctly to CMP/Mapping view
- [x] **DATA-03**: Silabus CRUD works and data links correctly to Plan IDP and Coaching Proton pages
- [ ] **DATA-04**: Coaching Guidance file management works (upload, download, replace, delete) and files link correctly to Plan IDP
- [ ] **DATA-05**: Worker management full CRUD works (create, edit, delete, detail view)
- [ ] **DATA-06**: Worker import from Excel template works (download template, upload, process, validation errors)
- [ ] **DATA-07**: Worker export to Excel works correctly with filters applied

### Plan IDP Development

- [ ] **IDP-01**: Coachee can view silabus items for their assigned track (Operator Tahun X, Unit Y, Bagian Z)
- [ ] **IDP-02**: Coachee can download coaching guidance files relevant to their assignment
- [ ] **IDP-03**: Plan IDP page supports filtering by Bagian, Unit, and Level to show relevant silabus

### Dashboard & Navigation

- [ ] **DASH-01**: Home/Index dashboard shows correct stats per role (IDP progress, assessment summary, training completion)
- [ ] **DASH-02**: CDP Dashboard Coaching Proton tab shows correct progress data across all bagian/unit
- [ ] **DASH-03**: CDP Dashboard Assessment Analytics tab shows correct assessment and training data with export
- [ ] **DASH-04**: Login flow works correctly (local auth + LDAP if configured)
- [ ] **DASH-05**: Role-based navigation visibility is correct (Kelola Data for Admin/HC only, CMP/CDP for all)
- [ ] **DASH-06**: Section selectors (KkjSectionSelect, MappingSectionSelect) work correctly for Admin/HC
- [ ] **DASH-07**: AccessDenied page displays properly when unauthorized user attempts restricted action
- [ ] **DASH-08**: AuditLog page displays assessment management audit trail correctly

## Future Requirements (v3.1+)

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
| Automated UI tests (Selenium/Playwright) | Manual QA with code analysis more effective for v3.0; defer automation to v3.1 |
| WebSocket real-time monitoring | Current polling approach (10s interval) works; defer to v3.1 if needed |
| Soft delete for any entity | Hard delete + AuditLog sufficient for current needs |
| New role additions | Current 6-role structure sufficient |
| LDAP/AD production testing | Requires live AD server; test local auth path only |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CLN-01 | Phase 82 | Complete |
| CLN-02 | Phase 82 | Complete |
| CLN-03 | Phase 82 | Complete |
| CLN-04 | Phase 82 | Complete |
| CLN-05 | Phase 82 | Complete |
| CLN-06 | Phase 82 | Complete |
| ASSESS-01 | Phase 84 | Pending |
| ASSESS-02 | Phase 84 | Pending |
| ASSESS-03 | Phase 84 | Pending |
| ASSESS-04 | Phase 84 | Pending |
| ASSESS-05 | Phase 84 | Pending |
| ASSESS-06 | Phase 84 | Pending |
| ASSESS-07 | Phase 84 | Pending |
| ASSESS-08 | Phase 84 | Pending |
| ASSESS-09 | Phase 84 | Pending |
| ASSESS-10 | Phase 84 | Pending |
| COACH-01 | Phase 85 | Pending |
| COACH-02 | Phase 85 | Pending |
| COACH-03 | Phase 85 | Pending |
| COACH-04 | Phase 85 | Pending |
| COACH-05 | Phase 85 | Pending |
| COACH-06 | Phase 85 | Pending |
| COACH-07 | Phase 85 | Pending |
| COACH-08 | Phase 85 | Pending |
| DATA-01 | Phase 83 | Pending |
| DATA-02 | Phase 83 | Complete |
| DATA-03 | Phase 83 | Complete |
| DATA-04 | Phase 83 | Pending |
| DATA-05 | Phase 83 | Pending |
| DATA-06 | Phase 83 | Pending |
| DATA-07 | Phase 83 | Pending |
| IDP-01 | Phase 86 | Pending |
| IDP-02 | Phase 86 | Pending |
| IDP-03 | Phase 86 | Pending |
| DASH-01 | Phase 87 | Pending |
| DASH-02 | Phase 87 | Pending |
| DASH-03 | Phase 87 | Pending |
| DASH-04 | Phase 87 | Pending |
| DASH-05 | Phase 87 | Pending |
| DASH-06 | Phase 87 | Pending |
| DASH-07 | Phase 87 | Pending |
| DASH-08 | Phase 87 | Pending |

**Coverage:**
- v3.0 requirements: 42 total
- Mapped to phases: 42
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-01*
*Last updated: 2026-03-01 — Traceability complete after roadmap creation*
