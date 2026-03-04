# Phase 84: Assessment Flow QA (Slimmed) - Context

**Gathered:** 2026-03-04
**Updated:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

QA the remaining assessment flows NOT already covered by Phases 90 and 91. Specifically: package management (create/delete/preview), question import (Excel + paste), cross-package shuffle validation, question import template, and edge-case stress testing. Does NOT re-test Admin assessment CRUD (Phase 90) or worker exam flow (Phase 91).

**Already verified by Phase 90 (11 flows PASS):** ManageAssessment CRUD, worker assignment, AssessmentMonitoring, HC actions (force close, reset, bulk close, regenerate token)
**Already verified by Phase 91 (9 flows PASS):** Token verification, exam start/resume/submit, auto-save, results, certificate, records, shuffle, option shuffle, CSRF

</domain>

<decisions>
## Implementation Decisions

### Scope Reduction
- Phase 84 slimmed to cover only ASSESS-08 (package management + question import) since Phases 90/91 already covered ASSESS-01 through ASSESS-07, ASSESS-09, ASSESS-10
- ASSESS-05 (earned competencies): leave orphaned models and UI as-is — competency auto-update was removed when KKJ tables dropped in Phase 90. Results.cshtml "Kompetensi Diperoleh" section only renders if data exists (currently never). Future phase can re-wire.
- Additionally include edge-case stress testing: 3+ packages, unusual paste formatting, mismatched question counts, duplicate import attempts

### QA Approach: Template + Smoke Test
- Primary deliverable: implement downloadable question import template (DownloadQuestionTemplate action)
- Smoke test the import flow (Excel + paste) with the new template — verify it round-trips
- Skip exhaustive package CRUD testing since Phase 90 already verified assessment-level CRUD
- Fix bugs found inline during smoke test — no pre-known issues

### Edge Cases to Stress-Test
- Cross-package with 3+ packages: verify equal count validation and shuffle distribution
- Paste import with unusual formatting: extra whitespace, mixed tabs/spaces, header row variants
- Duplicate import: verify fingerprint dedup correctly skips already-imported questions
- Mismatched question counts across sibling packages: verify error message is clear

### Question Import Template
- Add downloadable Excel template for question import (DownloadQuestionTemplate action)
- Template has headers: Question, Option A, Option B, Option C, Option D, Correct — with 1 example row
- Matches ImportWorkers pattern (download template button + file upload + process)

### Claude's Discretion
- Template styling (match ImportWorkers green headers or simpler) and button placement in ImportPackageQuestions.cshtml
- Exact template content, example row, and instruction text
- Bug fix approach for any issues found during smoke test
- Test data seeding strategy for edge-case verification

</decisions>

<code_context>
## Existing Code Insights

### Key Files
- Controllers/AdminController.cs: ManagePackages (line 4763), CreatePackage (line 4794), DeletePackage (line 4824), PreviewPackage (line 4879), ImportPackageQuestions (line 4900/4917)
- Controllers/AdminController.cs: DownloadImportTemplate (line 3881) — reference pattern for question template
- Views/Admin/ManagePackages.cshtml: Package list with status badges (OK/Warning/Kosong)
- Views/Admin/ImportPackageQuestions.cshtml: Two tabs (Excel upload + paste) with format reference card
- Views/Admin/PreviewPackage.cshtml: Read-only question display with IsCorrect badge
- Controllers/CMPController.cs: BuildCrossPackageAssignment (line 1155), Fisher-Yates shuffle (line 1139)

### Established Patterns
- ImportWorkers DownloadImportTemplate: ClosedXML workbook, green headers (#16A34A), italic example row, auto-width columns, returns FileStream
- Excel import uses ClosedXML library (already in project)
- Cross-package count validation: all sibling packages (same title + category + date) must have equal question counts
- Deduplication: fingerprint hash on normalized (question + optionA + optionB + optionC + optionD) text
- Correct letter parsing: handles "A", "A.", "OPTION A", case-insensitive

### Integration Points
- ManagePackages accessed from ManageAssessment detail view
- ImportPackageQuestions accessed from ManagePackages page
- Cross-package shuffle triggered at CMP/StartExam when worker begins exam
- Single-package mode: Fisher-Yates shuffle of question order
- Multi-package mode: slot-based even distribution across packages, then Fisher-Yates shuffle

</code_context>

<specifics>
## Specific Ideas

- Reference implementation for template: AdminController.DownloadImportTemplate (ImportWorkers pattern)
- User loves the Excel import system with downloadable template pattern
- ImportPackageQuestions view already has format reference card — template download enhances this

</specifics>

<deferred>
## Deferred Ideas

- Re-implement competency mapping with new data model (KKJ tables dropped) — future phase
- Token brute-force rate limiting — future security phase

</deferred>

---

*Phase: 84-assessment-flow-qa*
*Context updated: 2026-03-04*
