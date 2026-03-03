# Phase 90: Audit & fix Admin Assessment pages (ManageAssessment + AssessmentMonitoring) - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Comprehensive audit of Admin-side assessment management pages — verify all CRUD operations, monitoring actions, data display, links, cross-page connections, and authorization boundaries work correctly. Fix any bugs found. Covers:
- `/Admin/ManageAssessment` (3-tab hub: Assessment, Training, History)
- `/Admin/AssessmentMonitoring` (group list + detail views)
- Cross-page data consistency with CMP/Assessment and CMP/Records

</domain>

<decisions>
## Implementation Decisions

### ManageAssessment Scope
- Audit ALL 3 tabs: Assessment, Training, History
- All CRUD operations: Create, Edit, Delete (individual + group), Assign workers
- Form validation + edge cases: required fields, date validation, duration > 0, duplicate prevention
- Package management: import questions Excel, preview, package creation, cross-package shuffle
- All navigation: links, back buttons, breadcrumbs, redirects between assessment pages
- Grouping: verify display accuracy (not deep logic audit)

### Monitoring Actions Scope
- Audit ALL HC actions: Reset Assessment, Force Close, Export Excel, Regenerate Token, Bulk Close
- Both view levels: Group List (overview with filters) + MonitoringDetail (per-participant)
- Filter logic + display: search, status (Open/Upcoming/Closed), category — verify filter produces correct data, combinations work, count summary accurate
- Real-time polling: verify polling mechanism, data refresh, status update in MonitoringDetail

### Verification Method
- Pattern: Code review → fix bugs → browser verify (same as Phase 83)
- Seed data required: create comprehensive test data covering ALL assessment statuses (Open, Upcoming, Closed, Completed, In-Progress, Abandoned) + Training Records + History
- User verifies in browser after code fixes

### Cross-page Connections
- Phase 90 verifies Admin pages + connections TO CMP pages (data created in Admin should display correctly in CMP/Assessment and CMP/Records)
- Phase 91 will verify CMP pages + connections back to Admin
- Data consistency verification: counts, status, scores must be consistent across all 4 pages showing the same data
- Authorization boundary check: role guards enforced (Worker blocked from Admin pages, Admin-specific flows restricted properly)

### Claude's Discretion
- Exact seed data structure and content
- Priority ordering of bug fixes
- How deep to investigate each edge case
- Whether to refactor any messy code discovered during audit

</decisions>

<specifics>
## Specific Ideas

- User wants the same testing pattern as Phase 83: Claude analyzes code, fixes bugs, user verifies in browser
- "Secara menyeluruh dan detail" — thoroughness is the priority, not speed
- 4 pages are interconnected — the audit must verify the connections work, not just each page in isolation

</specifics>

<code_context>
## Existing Code Insights

### Key Files
- `Controllers/AdminController.cs` — 7+ assessment actions (ManageAssessment, CreateAssessment, EditAssessment, DeleteAssessment, DeleteAssessmentGroup, AssessmentMonitoring, AssessmentMonitoringDetail, ResetAssessment, ForceCloseAssessment, ExportAssessmentResults, UserAssessmentHistory)
- `Controllers/CMPController.cs` — Assessment, Records, VerifyToken, StartExam, Results, EditTrainingRecord, DeleteTrainingRecord
- `Views/Admin/ManageAssessment.cshtml` — 3-tab hub
- `Views/Admin/AssessmentMonitoring.cshtml` — Group list with filters
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — Per-participant detail
- `Views/Admin/CreateAssessment.cshtml`, `EditAssessment.cshtml`, `UserAssessmentHistory.cshtml`
- `Views/CMP/Assessment.cshtml` — Worker assessment list with token modal
- `Views/CMP/Records.cshtml` — Unified training records

### Models
- `AssessmentSession.cs` — Core assessment entity
- `AssessmentMonitoringViewModel.cs` — MonitoringGroupViewModel + MonitoringSessionViewModel
- `AssessmentResultsViewModel.cs` — Results with question review
- `UnifiedTrainingRecord.cs` — Bridges AssessmentSession + TrainingRecord
- `AssessmentPackage.cs`, `AssessmentQuestion.cs`, `AssessmentAttemptHistory.cs`

### Established Patterns
- All inline JavaScript (no separate .js files)
- Assessment grouping by Title + Category + Schedule.Date
- Authorization: AdminController class-level `[Authorize]`, per-action `[Authorize(Roles = "Admin, HC")]`
- CMPController class-level `[Authorize]`, runtime owner checks for StartExam/Results

### Integration Points
- ManageAssessment redirects: Create/Edit/Delete all → ManageAssessment
- Monitoring redirects: Reset/ForceClose → AssessmentMonitoringDetail
- CMP/Assessment → VerifyToken (AJAX) → StartExam → Results
- CMP/Records uses GetUnifiedRecords helper merging AssessmentSessions + TrainingRecords
- Training tab CRUD (EditTrainingRecord/DeleteTrainingRecord) redirects to ManageAssessment?tab=training

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 90-audit-fix-admin-assessment-pages-manageassessment-assessmentmonitoring*
*Context gathered: 2026-03-04*
