# Phase 292: Backend AJAX Endpoints - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

OrganizationController siap melayani AJAX — endpoint GetOrganizationTree baru tersedia dan semua CRUD action sudah dual-response (JSON jika AJAX, redirect jika form POST). Tidak ada perubahan UI/view di phase ini — hanya backend + JS utility file.

</domain>

<decisions>
## Implementation Decisions

### Struktur JSON Tree
- **D-01:** GetOrganizationTree mengembalikan **flat array** — client-side JS yang membangun tree hierarchy
- **D-02:** Fields: `Id`, `Name`, `ParentId`, `Level`, `DisplayOrder`, `IsActive` — 6 field saja, tidak ada tambahan

### Dual-response Strategy
- **D-03:** Mekanisme deteksi AJAX vs form POST — **Claude's Discretion** (pilih antara helper method di AdminBaseController atau extension method, yang paling sesuai dengan codebase)

### CSRF & ajaxPost Utility
- **D-04:** File utility di `wwwroot/js/orgTree.js` — file JS khusus untuk fitur organization tree
- **D-05:** CSRF token diambil dari hidden input `@Html.AntiForgeryToken()` — standard ASP.NET pattern yang sudah dipakai di seluruh project

### Error Response Format
- **D-06:** Format JSON konsisten `{success: bool, message: string}` untuk semua response (sukses dan gagal) — cukup untuk toast notification

### Claude's Discretion
- Dual-response detection mechanism (D-03): Claude bebas pilih antara helper di base controller atau extension method

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Code
- `Controllers/OrganizationController.cs` — Current controller with 5 POST actions (Add, Edit, Toggle, Delete, Reorder), semua PRG pattern
- `Controllers/AdminBaseController.cs` — Base class yang di-inherit OrganizationController
- `Views/Admin/ManageOrganization.cshtml` — Current 519-line view (tidak diubah di phase ini, tapi perlu dipahami)

### Model
- `Models/OrganizationUnit.cs` — Entity dengan Id, Name, ParentId, Level, DisplayOrder, IsActive, Children

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminBaseController` — base class dengan `_context`, `_userManager`, `_auditLog`, `_env`. OrganizationController sudah inherit
- `[ValidateAntiForgeryToken]` sudah ada di semua POST actions
- `fetch()` pattern sudah dipakai di 20+ views — bukan pattern baru

### Established Patterns
- PRG (Post-Redirect-Get) pattern di semua CRUD actions — harus tetap jalan untuk non-AJAX
- TempData["Success"]/TempData["Error"] untuk feedback — AJAX response harus mirror message yang sama
- Routing: `[Route("Admin")]` + `[Route("Admin/[action]")]`

### Integration Points
- GetOrganizationTree akan diakses oleh Phase 293 (tree rendering) dan Phase 294 (CRUD refresh)
- `orgTree.js` akan dipakai oleh Phase 293-295

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 292-backend-ajax-endpoints*
*Context gathered: 2026-04-02*
