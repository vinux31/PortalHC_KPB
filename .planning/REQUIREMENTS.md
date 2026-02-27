# Requirements: Portal HC KPB

**Defined:** 2026-02-26
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v2.3 Requirements — Admin Portal

### Category A: Master Data Managers (seed-only → full UI)

- [x] **MDAT-01**: Admin can view, create, edit, and delete KKJ Matrix items (KkjMatrixItem) through a dedicated management page — no database/code change required
- [x] **MDAT-02**: Admin can view, create, edit, and delete CPDP Items (CpdpItem) with section filter through a dedicated management page
- [x] **MDAT-03**: Admin can view, create, edit, and delete Assessment Competency Maps (AssessmentCompetencyMap) — mapping assessment categories to KKJ items

### Category B: Operational Admin (no admin override existed)

- [ ] **OPER-01**: Admin can view, create, edit, and delete Coach-Coachee Mappings (CoachCoacheeMapping) — assign and unassign coaches to coachees
- [ ] **OPER-02**: Admin can view, create, edit, and delete Proton Track Assignments (ProtonTrackAssignment) — assign workers to Proton tracks and manage active/inactive state
- [ ] **OPER-03**: Admin can view and override ProtonDeliverableProgress status — correct stuck or erroneous deliverable records
- [ ] **OPER-04**: Admin can view, approve, reject, and edit ProtonFinalAssessment records — admin-level management of final assessments
- [ ] **OPER-05**: Admin can view all CoachingSession and ActionItem records and perform override edits or deletions

### Category C: CRUD Completions (partial CRUD → full)

- [ ] **CRUD-01**: Admin/HC can edit existing AssessmentQuestion text and options (Edit was missing — only Add/Delete existed)
- [ ] **CRUD-02**: Admin/HC can edit and delete individual PackageQuestion and PackageOption records (currently import-only, no inline edit/delete)
- [ ] **CRUD-03**: Admin can edit and delete ProtonTrack records (Create existed, Edit/Delete were missing)
- [ ] **CRUD-04**: Admin can reset a worker's password from a standalone action without going through the full EditWorker form

## Future Requirements

*(None captured yet — all gaps included in v2.3)*

## Out of Scope

| Feature | Reason |
|---------|--------|
| Notifications manager | System-generated, low admin value for now |
| UserCompetencyLevel admin override | System-calculated — manual override risks data integrity |
| AssessmentAttemptHistory admin CRUD | Append-only audit trail by design |
| Role management page (add/remove IdentityRoles) | 9 roles are fixed by design, no need to add/remove |
| User activity summary (last login, sessions) | Not requested, out of scope |
| AuditLog edit/delete | Append-only by design |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| MDAT-01 | Phase 47 | Complete |
| MDAT-02 | Phase 48 | Complete |
| MDAT-03 | Phase 49 | Complete |
| OPER-01 | Phase 50 | Pending |
| OPER-02 | Phase 51 | Pending |
| OPER-03 | Phase 52 | Pending |
| OPER-04 | Phase 53 | Pending |
| OPER-05 | Phase 54 | Pending |
| CRUD-01 | Phase 55 | Pending |
| CRUD-02 | Phase 56 | Pending |
| CRUD-03 | Phase 57 | Pending |
| CRUD-04 | Phase 58 | Pending |

**Coverage:**
- v2.3 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-26*
*Last updated: 2026-02-26 after initial definition*
