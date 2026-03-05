# Phase 93: CMP Section Audit - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit CMP (Competency Management Platform) pages for bugs — Assessment hub, Assessment list, Records, KKJ Matrix view, and Assessment monitoring detail. Focus is finding and fixing bugs, NOT adding new features or changing functionality.

**Pages to Audit:**
- /CMP/Index (Assessment hub)
- /CMP/Assessment (Assessment list)
- /CMP/Records (Assessment + Training history)
- /CMP/Mapping (KKJ Matrix view)
- /CMP/Monitoring (Assessment monitoring detail)

**Requirements:** CMP-01 through CMP-06 (load without errors, real-time data, pagination, filtering, validation, navigation flows)

</domain>

<decisions>
## Implementation Decisions

### Audit Organization
- Claude's discretion — organize based on code review findings and bug severity
- Group fixes by bug category when practical (localization sweep, null safety sweep, validation sweep)
- One commit per category or grouped by related fixes

### Testing Approach
- Smoke test only — quick verification that pages load and obvious bugs are fixed
- Don't test every role combination exhaustively
- Focus on verifying the specific bug that was fixed
- Browser testing only when code review is unclear or requires runtime verification

### Bug Priority
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

### Known Gaps Investigation
- Yes — investigate "PositionTargetHelper missing for competency display" (ASSESS-04 from v3.0 gaps)
- Document what is broken and where it impacts users
- Fix if within scope of CMP pages being audited
- If fix is large, document as separate issue for future phase

### Claude's Discretion
- Exact order of bug fixes within each category
- Grouping of fixes into commits (per-file vs per-category vs one large commit)
- Which null safety checks are actually needed vs defensive coding
- Whether to refactor code patterns or just fix bugs

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 92 (Homepage Audit) pattern: Code review → Identify bugs → Fix → Smoke test
- Commit style: `fix(cmp): [description]` with Co-Authored-By footer
- Use Indonesian culture (id-ID) for all date formatting, matching Homepage fix
- Preserve existing functionality — bug fixes only, no behavior changes

</specifics>

<code_context>
## Existing Code Insights

### CMPController.cs (1200+ lines)
- Main controller with assessment lifecycle, KKJ Matrix, Records, exam flows
- Role-based filtering patterns using user.RoleLevel
- Real-time monitoring with IMemoryCache
- Assessment attempt history tracking

### CMP Views
- **Index.cshtml** — CMP hub with cards for KKJ Matrix, CPDP Mapping, Assessment, Records
- **Assessment.cshtml** — Assessment list with Open/Upcoming tabs (Worker) vs Management/Monitoring (HC/Admin)
- **Records.cshtml** — Unified assessment + training history with 2-tab layout, client-side filtering
- **Kkj.cshtml** — KKJ Matrix file download page, tabbed by bagian, role-based section filtering
- **Mapping.cshtml** — CPDP file download page, tabbed by bagian, role-based section filtering
- **StartExam.cshtml** — Exam interface with timer, auto-save, session management
- **Results.cshtml** — Assessment results with score, pass/fail, competency display (potentially broken)
- **Certificate.cshtml** — Certificate download with returnUrl support

### Known Bug Patterns (Initial Findings)
- **Localization**: 12+ instances of `.ToString("dd MMM yyyy")` without Indonesian culture:
  - Records.cshtml: lines 142, 188, 194
  - Assessment.cshtml: lines 142, 146, 209, 260, 308
  - Kkj.cshtml: line 105
  - Mapping.cshtml: line 111
  - Certificate.cshtml: line 4
  - Results.cshtml: line 78
- **Null safety**: `.First()` calls without null checks at CMPController lines 80, 85, 927
- **Validation**: POST actions need verification for ModelState error handling

### Established Patterns from Phase 92
- Date localization fix: Add `@using System.Globalization` to view, use `CultureInfo.GetCultureInfo("id-ID")`
- Pluralization: Use ternary for singular/plural (Indonesian: "menit/jam/hari/hari/minggu")
- Null safety: Use `?.` null-conditional and `??` coalescing operators
- Commit pattern: `fix(cmp): [category] - [description]` with detailed commit body

### Models to Review
- AssessmentSession — Status (Upcoming/Open/InProgress/Completed/Abandoned), Schedule, DurationMinutes
- AssessmentPackage — Title, PassPercentage, QuestionCount, AssessmentPackageQuestions
- PackageUserResponse — Auto-save answers, ShuffledOptionIdsPerQuestion
- AssessmentAttemptHistory — Archive of completed sessions with AttemptNumber
- KkjFile — BagianId, FileName, FilePath, FileType, Keterangan, UploadedAt, IsArchived
- KkjBagian — Name, DisplayOrder (shared by KKJ and CPDP features)

### Integration Points
- CMP Index hub — cards link to Kkj, Mapping, Assessment, Records
- Role-based navigation — Worker sees different tabs than HC/Admin
- Assessment Results — potentially broken competency display (PositionTargetHelper missing)
- Real-time monitoring — IMemoryCache with 10s TTL, 10s poll interval
- File downloads — PhysicalFile() with correct content types (PDF, Excel)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 93-cmp-section-audit*
*Context gathered: 2026-03-05*
