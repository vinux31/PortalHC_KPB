# Phase 95: Admin Portal Audit - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit Admin (Kelola Data) portal pages for bugs — Manage Workers, Coach-Coachee Mapping, and cross-cutting concerns (validation error handling, role gates). Focus is finding and fixing bugs, NOT adding new features or changing functionality.

**Pages to Audit:**
- /Admin/ManageWorkers — Worker CRUD with filters, pagination, import/export
- /Admin/CoachCoacheeMapping — Coach-coachee assignment management
- /Admin/ImportWorkers — Excel bulk import functionality
- Cross-cutting: Validation error handling across all Admin forms
- Cross-cutting: Role gate verification (HC vs Admin access)

**Requirements:** ADMIN-01 through ADMIN-08 (ManageWorkers, KKJ files, Assessments, Monitoring, Coach-Coachee Mapping, Proton Data, validation, role gates)

**Note:** Some Admin pages already audited in prior phases:
- Phase 88: KKJ Matrix, CPDP Files, Proton Data (Silabus + Coaching Guidance tabs)
- Phase 90: ManageAssessment, AssessmentMonitoring, CreateAssessment, EditAssessment

</domain>

<decisions>
## Implementation Decisions

### Audit Organization
- **By page** — Group fixes by Admin page being audited
- ManageWorkers fixes in one commit (or grouped by bug category if many similar bugs)
- CoachCoacheeMapping fixes in separate commit
- Cross-cutting fixes (validation, role gates) as separate commits
- This matches Phase 94's by-flow approach and keeps changes organized by feature area

### Testing Approach
- **Smoke test only** — quick verification that pages load and obvious bugs are fixed
- Don't test every role combination exhaustively
- Pattern: Code review → identify bugs → fix → browser verify (same as Phases 93/94)
- Focus on verifying the specific bug that was fixed
- Browser testing only when code review is unclear or requires runtime verification

### Test Data Approach
- **Use existing seed data** where possible — no need for comprehensive new seed data
- Workers should already exist from Phase 83 (Master Data QA)
- Coach-coachee mappings should already exist from Phase 85
- If specific test scenarios are missing during code review, add them

### Role Testing Coverage
- **HC and Admin roles** — verify both have correct access to Admin pages
- Verify Worker role is blocked from all Admin pages (403 or redirect)
- No need to test every intermediate role (Coach, Spv, SectionHead) unless code review reveals specific concerns
- This is smoke test level — verify role gates exist, not exhaustive permission testing

### Validation Depth
- **All Admin forms** — check validation error handling on all Admin CRUD forms
- ManageWorkers: Create, Edit forms
- CoachCoacheeMapping: Assign form
- Import form: File upload validation
- Check: Required fields, data type validation, error messages via TempData (not raw exceptions)

### Import/Export Depth
- **Smoke test** — verify ImportWorkers and CoachCoacheeMappingExport work end-to-end
- Verify: File upload succeeds, data is processed, export returns correct file
- Don't test edge cases (large files, malformed data) unless code review reveals potential issues
- File validation should be checked (allowed extensions, size limits)

### Bug Priority
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

### Claude's Discretion
- Exact order of bug fixes within each page
- Whether to group fixes by page or by bug category
- Which validation checks are actually needed vs defensive coding
- Whether to refactor any messy code discovered during audit
- How deep to investigate each edge case vs smoke test

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 93/94 audit pattern: Code review → Identify bugs → Fix → Smoke test
- Commit style: `fix(admin): [description]` with Co-Authored-By footer
- Use Indonesian culture (id-ID) for all date formatting, matching Phase 92/93/94 fixes
- Preserve existing functionality — bug fixes only, no behavior changes
- Focus on pages NOT yet audited: ManageWorkers, CoachCoacheeMapping (KKJ, Assessments, ProtonData already covered in Phase 88/90)

</specifics>

<code_context>
## Existing Code Insights

### Key Files
- `Controllers/AdminController.cs` — Large controller (5500+ lines) with all Admin actions
- `Views/Admin/ManageWorkers.cshtml` — Worker list with filters and pagination
- `Views/Admin/CreateWorker.cshtml`, `EditWorker.cshtml` — Worker CRUD forms
- `Views/Admin/ImportWorkers.cshtml` — Excel import with template download
- `Views/Admin/CoachCoacheeMapping.cshtml` — Coach-coachee assignment interface
- `Views/Admin/Index.cshtml` — Admin hub (already audited in Phase 87)

### Established Patterns from Prior Audits
- **Phase 93 (CMP Audit)**: Localization sweep using `CultureInfo.GetCultureInfo("id-ID")`, null checks for DateTime, CSRF token verification
- **Phase 94 (CDP Audit)**: Flow-based organization, role-based filtering, validation error handling via TempData
- **Phase 90 (Admin Assessment)**: Comprehensive CRUD audit with IsActive filters, form validation, cross-page connections

### Integration Points
- ManageWorkers connects to: ApplicationUser table, Role assignments, Unit/Bagian filtering
- CoachCoacheeMapping connects to: ProtonTrackAssignment, ProtonTrack tables
- ImportWorkers uses: ClosedXML.Excel library (same as Phase 83 seed data)
- All Admin pages share: Role gates `[Authorize(Roles = "Admin, HC")]`, TempData error handling

### Reusable Assets
- AuditLogService — already injected into AdminController
- UserManager — for user role operations
- ClosedXML.Excel — for Excel import/export (already used in Phase 83)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 95-admin-portal-audit*
*Context gathered: 2026-03-05*
