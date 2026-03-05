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
- **Per halaman** — ManageWorkers fixes → satu commit, CoachCoacheeMapping fixes → satu commit
- **Cross-cutting concerns → commit terpisah** — Validation fixes → satu commit, Role gate fixes → satu commit
- Total expected: 4-5 commit
- This matches Phase 94's by-flow approach and keeps changes organized by feature area
- Cross-cutting concerns yang mempengaruhi banyak halaman dipisah agar lebih mudah track apa yang diperbaiki

### Testing Approach
- **Smoke test only** — quick verification that pages load and obvious bugs are fixed
- Don't test every role combination exhaustively
- Pattern: Code review → identify bugs → fix → browser verify (same as Phases 93/94)
- Focus on verifying the specific bug that was fixed
- Browser testing only when code review is unclear or requires runtime verification

### Test Data Approach
- **Pakai existing seed data** — Workers dari Phase 83 (Master Data QA), Coach-coachee mappings dari Phase 85
- **Tambah test data hanya saat diperlukan** — selama code review, kalau butuh worker dengan role spesifik atau mapping status tertentu, baru tambah
- Untuk Import Workers: pakai template existing (DownloadImportTemplate), isi dengan sample data
- Test file Excel — Claude tentukan berdasarkan code review findings
- Pragmatic approach: hanya tambah test data yang benar-benar diperlukan

### Role Testing Coverage
- **HC & Admin roles saja** — dua role yang memang punya akses ke Admin pages
- **Verify role gates via code review** — cek `[Authorize(Roles = "Admin, HC")]` attribute di controller
- Tidak perlu test semua intermediate role (Coach, Spv, SectionHead) untuk save time
- **Test role-based filtering kalau ada di code** — kalau code review menemukan .Where(u => u.Unit == user.Unit) atau similar, perlu test
- Ini adalah smoke test level — verify role gates exist lewat code review, bukan exhaustive permission testing

### Validation Depth
- **All Admin forms** — check validation error handling on all Admin CRUD forms
- ManageWorkers: Create, Edit forms
- CoachCoacheeMapping: Assign form
- Import form: File upload validation
- Check: Required fields, data type validation, error messages via TempData (not raw exceptions)

### Import/Export Depth
- **Smoke test untuk Import Workers** — upload valid file → verify processed → check data ada di DB
- **Export — Claude tentukan** — tergantung complexity code review. Kalau export logic kompleks (formatting, calculations), test. Kalau simple data dump, smoke test atau skip.
- **Smoke test validation** — test satu invalid file type untuk verify validation exists. Tidak test semua scenarios, cukup verify validation works.
- Focus: verify basic functionality works, edge cases hanya kalau code review reveals potential issues

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
- Untuk Import Excel test files: buatberapa tergantung findings
- Untuk Export: test atau skip tergantung code complexity

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 93/94 audit pattern: Code review → Identify bugs → Fix → Smoke test
- Commit style: `fix(admin): [description]` with Co-Authored-By footer
- Use Indonesian culture (id-ID) for all date formatting, matching Phase 92/93/94 fixes
- Preserve existing functionality — bug fixes only, no behavior changes
- Focus on pages NOT yet audited: ManageWorkers, CoachCoacheeMapping (KKJ, Assessments, ProtonData already covered in Phase 88/90)
- "Secara menyeluruh dan detail" — thoroughness is the priority, not speed (from Phase 90)

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
