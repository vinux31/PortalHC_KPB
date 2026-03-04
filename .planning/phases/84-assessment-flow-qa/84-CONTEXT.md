# Phase 84: Assessment Flow QA (Slimmed) - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

QA the remaining assessment flows NOT already covered by Phases 90 and 91. Specifically: package management (create/delete/preview), question import (Excel + paste), cross-package shuffle validation, and question import template. Does NOT re-test Admin assessment CRUD (Phase 90) or worker exam flow (Phase 91).

**Already verified by Phase 90 (11 flows PASS):** ManageAssessment CRUD, worker assignment, AssessmentMonitoring, HC actions (force close, reset, bulk close, regenerate token)
**Already verified by Phase 91 (9 flows PASS):** Token verification, exam start/resume/submit, auto-save, results, certificate, records, shuffle, option shuffle, CSRF

</domain>

<decisions>
## Implementation Decisions

### Scope Reduction
- Phase 84 slimmed to cover only ASSESS-08 (package management + question import) since Phases 90/91 already covered ASSESS-01 through ASSESS-07, ASSESS-09, ASSESS-10
- ASSESS-05 (earned competencies): leave orphaned models and UI as-is — competency auto-update was removed when KKJ tables dropped in Phase 90. Results.cshtml "Kompetensi Diperoleh" section only renders if data exists (currently never). Future phase can re-wire.

### Package Management QA
- Pure QA approach: verify all flows work (create package, delete package, preview, import via Excel, import via paste, cross-package count validation, deduplication)
- Fix bugs found inline during QA — no pre-known issues
- Verify cross-package shuffle distributes questions correctly across workers

### Question Import Template
- Add downloadable Excel template for question import (DownloadQuestionTemplate action)
- Template has headers: Question, Option A, Option B, Option C, Option D, Correct — with 1 example row
- Matches ImportWorkers pattern (download template button + file upload + process)
- Add download button to ImportPackageQuestions.cshtml

### Claude's Discretion
- Exact template content and example row
- Bug fix approach for any issues found during QA
- Test data seeding strategy for package/question verification

</decisions>

<code_context>
## Existing Code Insights

### Key Files
- Controllers/AdminController.cs: ManagePackages (line 4763), CreatePackage (line 4794), DeletePackage (line 4824), PreviewPackage (line 4879), ImportPackageQuestions (line 4900/4917)
- Views/Admin/ManagePackages.cshtml: Package list with status badges
- Views/Admin/ImportPackageQuestions.cshtml: Two tabs (Excel upload + paste)
- Controllers/CMPController.cs: BuildCrossPackageAssignment (line 1148), Fisher-Yates shuffle (line 1138)

### Established Patterns
- ImportWorkers has DownloadImportTemplate action (line 3884) — reuse same pattern for question template
- Excel import uses EPPlus library (already in project)
- Cross-package count validation: all sibling packages must have equal question counts
- Deduplication: MD5 fingerprint of normalized question+options text

### Integration Points
- ManagePackages accessed from ManageAssessment detail view
- ImportPackageQuestions accessed from ManagePackages page
- Cross-package shuffle triggered at CMP/StartExam when worker begins exam

</code_context>

<specifics>
## Specific Ideas

- Reference implementation for template: AdminController.DownloadImportTemplate (ImportWorkers pattern)
- User loves the Excel import system with downloadable template pattern

</specifics>

<deferred>
## Deferred Ideas

- Re-implement competency mapping with new data model (KKJ tables dropped) — future phase
- Token brute-force rate limiting — future security phase

</deferred>

---

*Phase: 84-assessment-flow-qa*
*Context gathered: 2026-03-04*
