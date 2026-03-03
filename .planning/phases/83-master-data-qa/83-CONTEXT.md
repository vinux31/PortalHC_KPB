# Phase 83: Master Data QA - Context

**Gathered:** 2026-03-02 (updated 2026-03-03)
**Status:** Ready for planning

<domain>
## Phase Boundary

Verify all master data management features in the Kelola Data hub work correctly end-to-end for Admin and HC roles. Fix bugs found during QA. Features: KKJ Matrix editor, KKJ-IDP Mapping editor, Silabus CRUD, Coaching Guidance file management, Worker management (CRUD + import/export).

</domain>

<decisions>
## Implementation Decisions

### QA Depth & Scope
- Happy path + validation: test normal CRUD flows plus input validation (empty fields, duplicates, invalid data)
- Code review first: Claude reviews controller/view code, identifies potential bugs, fixes them proactively, THEN user verifies in browser
- 3 known high-priority bugs to fix (discovered during QA):
  1. **DeleteWorker ProtonFinalAssessment** — Worker deletion fails due to FK constraint on ProtonFinalAssessment table
  2. **SilabusDelete FK guard** — Silabus deletion fails due to FK constraint (no guard/cascade)
  3. **KkjBagianDelete archived count** — KKJ Bagian deletion doesn't account for archived records in count
- Filter behavior: test bagian/unit filter switching to verify data loads correctly and saves don't cross-contaminate

### Bug Fix Approach
- Fix inline: fix bugs immediately as part of the QA plan (review code → fix bugs → commit → user verifies in browser)
- Big bugs: fix anything that's a localized change (under ~100 lines). Only flag truly architectural issues for discussion
- Verification: manual browser test after each plan. Use /gsd:verify-work 83 at the end for a formal pass

### Test Data Setup
- Database has production-like data across most features — test against existing data
- Worker import: Claude creates a small test Excel file with 5-10 workers (valid + invalid rows) for testing
- Coaching Guidance: Claude checks code for allowed file types and tests accordingly

### Cross-Feature Links
- Full round-trip verification: edit data in Admin → verify it appears correctly in user-facing views (CMP/Kkj, CMP/Mapping, CDP/CoachingProton)
- Silabus: verify dropdown options actually populate from Silabus data in Plan IDP and Coaching Proton
- Export: Claude decides based on code review whether export contents need manual verification vs just download works

### Claude's Discretion
- Exact test scenarios per feature (number of test rows, specific validation cases)
- Coaching Guidance file type testing scope
- Export content verification depth
- Loading skeleton or UX improvements if encountered during QA

</decisions>

<specifics>
## Specific Ideas

- User prefers testing organized by use-case flows (not page-by-page or role-by-role)
- Pattern: Claude analyzes code → user verifies in browser → Claude fixes bugs
- Reference: Worker import/export pattern from AdminController.cs (ImportWorkers + DownloadImportTemplate)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController.cs`: KKJ Matrix (lines 48-260), KKJ-IDP Mapping (lines 266-450+), Worker CRUD (lines 2972-3700+), AuditLog (lines 2135+)
- `ProtonDataController.cs`: Silabus (lines 108-300+), CoachingGuidance (lines 338-500+)
- `AuditLogService`: All admin actions already log via `_auditLog.LogAsync()` — can verify audit trail during QA
- ClosedXML: Used for all Excel import/export (Worker import, KKJ-IDP Mapping export, Coach-Coachee export)

### Established Patterns
- Spreadsheet editors: `[FromBody] List<T>` for bulk save, JSON serialized to ViewBag for initial load
- CRUD pattern: GET loads view, POST actions return `Json(new { success, message })` for AJAX
- Role gating: `[Authorize(Roles = "Admin, HC")]` on all admin CRUD actions
- File uploads: `IFormFile` parameter, stored in wwwroot or database

### Integration Points
- KKJ Matrix → CMP/Kkj view (same `KkjMatrixItem` model, same DbContext)
- KKJ-IDP Mapping → CMP/Mapping view (same `KkjIdpMapping` model)
- Silabus → Plan IDP and Coaching Proton pages (queried for dropdown/selection options)
- Coaching Guidance → Plan IDP (file download links)
- Worker data → used across all features (assessment, coaching, IDP)

</code_context>

<deferred>
## Deferred Ideas

- Package question management feature (CMP has ImportPackageQuestions.cshtml, ManagePackages.cshtml, PreviewPackage.cshtml) — user noted this during Phase 82 UAT, consider for future phase
- **PlanIdp Bagian/Unit filter bug** — deferred to Phase 86 (PlanIdp development scope)
- **Coachee guidance download bug** — deferred to Phase 86 (PlanIdp development scope)

</deferred>

---

*Phase: 83-master-data-qa*
*Context gathered: 2026-03-02*
