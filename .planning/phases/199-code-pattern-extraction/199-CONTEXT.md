# Phase 199: Code Pattern Extraction - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Extract 3 repeated inline code patterns (file upload, role-scoping, pagination) into reusable helper classes/methods. Pure behavior-preserving refactor — no new features, no changed behavior.

</domain>

<decisions>
## Implementation Decisions

### Lokasi & Struktur Helper
- Semua helper baru di `Helpers/` folder — konsisten dengan `ExcelExportHelper.cs` yang sudah ada
- Static class untuk semua helper — tidak perlu DI, langsung panggil
- Satu file per helper: `FileUploadHelper.cs`, `PaginationHelper.cs`, role-scoping helper di `CMPController` sebagai private method

### Scope Pagination Helper
- Hanya kalkulasi: skip, take, totalPages, currentPage clamping
- Return object/tuple dengan hasil kalkulasi
- Controller tetap assign ViewBag sendiri — helper tidak coupled ke ViewBag
- Dipakai di 3 controller: Admin, CMP, CDP (5+ inline implementations)

### Claude's Discretion
- Nama exact untuk return type pagination (class, record, atau tuple)
- Nama method dan parameter untuk FileUploadHelper
- Apakah role-scoping helper di CMPController jadi static Helpers/ class atau private method — tergantung apakah pattern reusable di luar CMP

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in REQUIREMENTS.md (PAT-01, PAT-02, PAT-03) and decisions above.

### Existing patterns
- `Helpers/ExcelExportHelper.cs` — Reference implementation for helper class pattern (static class, Helpers/ folder)
- `Services/WorkerDataService.cs` — Example of shared service extraction from Phase 196

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helpers/ExcelExportHelper.cs`: Established pattern for static helper classes
- `Services/AuditLogService.cs`: Audit logging service already available for FileUploadHelper to reference

### Established Patterns
- Static helper class pattern: public static methods, no DI, utility-focused
- Controllers assign ViewBag for pagination display (ViewBag.CurrentPage, ViewBag.TotalPages, etc.)

### Integration Points
- `AdminController.cs`: File upload inline code (KKJ + CPDP uploads) — 11 file-related occurrences
- `CMPController.cs`: Role-scoping enforcement — 10 role-check occurrences, 3+ repeated blocks
- `AdminController.cs`, `CMPController.cs`, `CDPController.cs`: Pagination inline — 40 total occurrences across 3 controllers

</code_context>

<specifics>
## Specific Ideas

No specific requirements — follow ExcelExportHelper pattern for consistency.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 199-code-pattern-extraction*
*Context gathered: 2026-03-18*
