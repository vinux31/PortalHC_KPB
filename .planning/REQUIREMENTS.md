# Requirements: Portal HC KPB

**Defined:** 2026-03-01
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v2.6 Requirements

Requirements for codebase cleanup milestone. Prioritized: Critical → Dead Code → Stubs → Role Fixes → Broken Links.

### Critical Fixes

- [x] **CRIT-01**: User sees a proper "Access Denied" page instead of a runtime error when authorization fails
- [x] **CRIT-02**: Dead `CMPController.WorkerDetail` action removed (missing view causes runtime exception; all UI already routes through Admin/WorkerDetail)

### Dead View Cleanup

- [x] **VIEW-01**: Orphaned `Views/CMP/CreateAssessment.cshtml` deleted (migrated to Admin)
- [x] **VIEW-02**: Orphaned `Views/CMP/EditAssessment.cshtml` deleted (migrated to Admin)
- [x] **VIEW-03**: Orphaned `Views/CMP/UserAssessmentHistory.cshtml` deleted (migrated to Admin)
- [x] **VIEW-04**: Orphaned `Views/CMP/AuditLog.cshtml` deleted (migrated to Admin)
- [x] **VIEW-05**: Orphaned `Views/CMP/AssessmentMonitoringDetail.cshtml` deleted (migrated to Admin)
- [x] **VIEW-06**: Orphaned `Views/CDP/Progress.cshtml` deleted (controller redirects, view never rendered)

### Dead Action Cleanup

- [x] **ACTN-01**: `CMPController.GetMonitorData` action removed (zero references, replaced by Admin/GetMonitoringProgress)
- [x] **ACTN-02**: `CDPController.Progress` redirect stub removed (no inbound links)

### Placeholder Cleanup

- [x] **STUB-01**: BP navbar link and placeholder page removed (or hidden until module is built)
- [x] **STUB-02**: Admin hub "Coaching Session Override" stub card removed
- [x] **STUB-03**: Admin hub "Final Assessment Manager" stub card removed
- [x] **STUB-04**: Settings page disabled items (2FA, Notifikasi, Bahasa) removed
- [x] **STUB-05**: `Views/Home/Privacy.cshtml` and `HomeController.Privacy` action removed

### Dead Files

- [x] **FILE-01**: `wwwroot/css/site.css` deleted (unreferenced by any view)
- [x] **FILE-02**: `wwwroot/js/site.js` deleted (unreferenced by any view)

### HC Role Fix

- [x] **ROLE-01**: Admin hub cards hidden for HC users when the backing action is Admin-only (KKJ Matrix, KKJ-IDP Mapping, Coach-Coachee Mapping, Manage Assessments)
- [x] **ROLE-02**: "Kelola Data" navbar visibility uses Identity role check, not just SelectedView field

### Broken Link Fix

- [x] **LINK-01**: Admin hub "Deliverable Progress Override" card activates correct Bootstrap tab on ProtonData page

### Training Record Redirect Fix

- [x] **REDIR-01**: `EditTrainingRecord` and `DeleteTrainingRecord` redirect to `CMP/Records` instead of `Admin/WorkerDetail` (which shows no training data)

### Navigation Improvement

- [ ] **NAV-01**: Kelola Data hub shows a "Training Records" card for HC and Admin users linking to `CMP/Records`

## Future Requirements

### Page Migration (deferred)

- **MIG-01**: CMP/Records full migration to Admin controller (currently linked from Kelola Data hub but still served by CMPController)
- **MIG-02**: CDP/ProtonMain page migrated to Kelola Data Hub or Admin

## Out of Scope

| Feature | Reason |
|---------|--------|
| Full CMP/Records controller migration | v2.6 adds nav shortcut + fixes redirects; full migration deferred |
| CDP/ProtonMain migration | Serves supervisor workflow, not admin — different audience |
| New feature development | v2.6 is cleanup-only |
| BP module implementation | BP is explicitly deferred (not in scope for any current milestone) |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CRIT-01 | Phase 73 | Complete |
| CRIT-02 | Phase 73 | Complete |
| VIEW-01 | Phase 74 | Complete |
| VIEW-02 | Phase 74 | Complete |
| VIEW-03 | Phase 74 | Complete |
| VIEW-04 | Phase 74 | Complete |
| VIEW-05 | Phase 74 | Complete |
| VIEW-06 | Phase 74 | Complete |
| ACTN-01 | Phase 74 | Complete |
| ACTN-02 | Phase 74 | Complete |
| STUB-01 | Phase 75 | Complete |
| STUB-02 | Phase 75 | Complete |
| STUB-03 | Phase 75 | Complete |
| STUB-04 | Phase 75 | Complete |
| STUB-05 | Phase 75 | Complete |
| FILE-01 | Phase 74 | Complete |
| FILE-02 | Phase 74 | Complete |
| ROLE-01 | Phase 76 | Complete |
| ROLE-02 | Phase 76 | Complete |
| LINK-01 | Phase 76 | Complete |
| REDIR-01 | Phase 77 | Planned |
| NAV-01 | Phase 78 | Planned |

**Coverage:**
- v2.6 requirements: 22 total
- Mapped to phases: 22
- Unmapped: 0

---
*Requirements defined: 2026-03-01*
*Last updated: 2026-03-01 after codebase audit*
