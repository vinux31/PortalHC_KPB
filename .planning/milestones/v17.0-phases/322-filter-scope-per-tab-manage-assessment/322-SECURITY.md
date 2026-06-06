---
phase: 322
slug: filter-scope-per-tab-manage-assessment
status: secured
threats_open: 0
asvs_level: 1
created: 2026-05-22
---

# Phase 322 — Security

> Per-phase security contract. Phase 322 SHIPPED + tag `v17.0-p322-complete`. PLAN.md no explicit `<threat_model>` block (view-only refactor scope). Threat register reconstructed from artifacts via STRIDE applied to surface area.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser → ASP.NET MVC | Admin user filter input via HTMX inline trigger → controller partial action | Filter query params (search/category/statusFilter/section/unit), no sensitive data |
| MVC → SQL Server | Controller partial action `ManageAssessmentTab_*` → EF Core LINQ query | Filter values mapped ke EF parameterized query (no raw SQL) |
| Razor → Browser | Server-rendered partial HTML response → HTMX innerHTML swap | User-controlled filter values re-rendered (`value="@searchTerm"`, `<option selected="selected">`) |
| Wrapper hx-get URL → Partial action | Initial page load + tab activation pass filter params via URL query string (D-21 Strategy D Hybrid post-UAT migration) | Same as Browser → MVC |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-322-01 | Information Disclosure (Cross-tab leak) | Wrapper hx-vals attribute → descendant inheritance | mitigate | D-21 Strategy D Hybrid (Tab 2 wrapper inject section+unit+page ONLY, drop overlap); post-UAT fix `773c970c` migrate hx-vals → URL query string (URL params NOT inherit ke descendant, prevent ancestor override descendant form data). Bug 2 prevention by-design. | closed |
| T-322-02 | Tampering (XSS) | Filter input `value="@searchTerm"` re-render after URL bookmark | mitigate | Razor `@` auto-encode (HTML entity escape default); ViewBag null coalesce fix `6ecb7a50` (`search ?? ""`) prevents literal `"null"` string injection. Verified runtime spec REGRESSION-A. | closed |
| T-322-03 | Elevation of Privilege (Unauthorized partial endpoint access) | `ManageAssessmentTab_Assessment` / `_Training` / `_History` actions | mitigate | `[Authorize(Roles = "Admin, HC")]` attribute preserved on all 3 partial actions (Phase 311 baseline, not modified Phase 322). Verified grep `Controllers/AssessmentAdminController.cs`. | closed |
| T-322-04 | Tampering (CSRF) | HTMX GET filter requests to partial endpoints | accept | GET request idempotent (filter is read-only, no state mutation). No CSRF token required for GET per OWASP. POST mutating endpoints (Add/Edit/DeleteCategory) retain `[ValidateAntiForgeryToken]` (Phase 311 baseline). | closed |
| T-322-05 | DoS (Filter param injection) | Controller param binding `string? search, category, section, unit, statusFilter` | mitigate | Strong typing (`string?` nullable, default `null`); EF Core parameterized query (no raw SQL); category bounded by ViewBag.Categories list (DB Distinct via `_cache`) untuk Tab 1 + hardcoded 8-enum (`OJT/IHT/Training Licencor/OTS/MANDATORY/Proton/ISS/OSS`) untuk Tab 2 partial. No user input flow ke raw SQL. | closed |
| T-322-06 | Information Disclosure (Debug leak via JSON serialize) | Wrapper hx-vals `@Json.Serialize(...)` produces JSON in HTML attribute | mitigate | Post-UAT fix `773c970c` REMOVED hx-vals from wrapper entirely. Migrated to hx-get URL query string (no JSON serialization in response). REGRESSION-B spec asserts wrapper TIDAK punya hx-vals attribute. | closed |
| T-322-07 | Tampering (HTMX hx-vals inheritance bypass) | Pre-fix wrapper hx-vals override descendant form data — could allow attacker manipulate URL bookmark to suppress user filter selection | mitigate | Same fix as T-322-06: hx-vals removed from wrapper. Descendant form data (filter dropdown/input) now sole source for HTMX request param. Defense-in-depth: form fields server-side validated via controller signature anyway. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| R-322-01 | T-322-04 | HTMX GET filter request CSRF acceptable — GET idempotent read-only, no state mutation. OWASP CSRF cheat sheet: GET endpoints don't require token. POST mutating endpoints retain `[ValidateAntiForgeryToken]`. | gsd-secure-phase audit (2026-05-22) | 2026-05-22 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-05-22 | 7 | 7 | 0 | gsd-secure-phase 322 (orchestrator inline — no auditor agent spawn karena threats_open: 0 ditemukan langsung dari artifacts) |

### Audit Notes

**Method:** State B reconstruction. PLAN files no explicit `<threat_model>` block (Phase 322 view-only refactor = low security risk). Threat register built implicit dari STRIDE applied to:
- 5 file modified (3 partial Razor views + 1 shell view + 1 controller action body)
- 0 file created
- 0 new endpoint
- 0 DB migration
- 0 new dependency

**Verification evidence:**
- T-322-01, T-322-06, T-322-07: Spec `tests/e2e/manage-assessment-filter.spec.ts` REGRESSION-B asserts wrapper TIDAK punya `hx-vals` attribute (commit `773c970c` post-UAT fix verified runtime).
- T-322-02: Spec REGRESSION-A asserts textbox value="" NOT literal "null" (commit `6ecb7a50` post-UAT fix verified runtime).
- T-322-03: Grep verified `Controllers/AssessmentAdminController.cs` — `[Authorize(Roles = "Admin, HC")]` decorator preserved pada method `ManageAssessment`, `ManageAssessmentTab_Assessment`, `ManageAssessmentTab_Training`, `ManageAssessmentTab_History`.
- T-322-04: Acceptance rationale per OWASP CSRF cheat sheet (GET idempotent, no token required).
- T-322-05: Controller signature `(string? search, int page = 1, int pageSize = 20, string? tab = null, string? section = null, string? unit = null, string? category = null, string? statusFilter = null, string? isFiltered = null)` — all nullable strings + EF Core LINQ (parameterized). No raw SQL untouched in Phase 322 scope.

**Surface area summary (Phase 322 specific):**
- Wrapper hx-vals → hx-get URL query string migration (post-UAT fix) ELIMINATES inheritance attack surface (T-322-06/07).
- Controller `_cache.GetOrCreateAsync(CategoriesCacheKey)` di partial action `ManageAssessmentTab_Assessment` preserved — partial action fetch sendiri (single cache key, multi-consumer pattern).
- Shell action `ManageAssessment` drop `ViewBag.Categories = await _cache.GetOrCreateAsync(...)` block — reduce surface (one less DB query path).

**No new threat surface introduced.** Phase 322 NET-NEGATIVE surface change:
- ✅ Removed: shell shared `<form id="filter-form">` (1 less form)
- ✅ Removed: cross-tab `htmx:afterSwap` invalidation listener (1 less event handler)
- ✅ Removed: `hx-get` endpoint updater script (1 less DOM mutation path)
- ✅ Removed: shell `ViewBag.Categories` redundant cache fetch (1 less DB query)
- ✅ Added: 3 wrapper hx-get URL query string (server-side render, parameterized)
- ✅ Added: 1 JS function `filterTrainingRows()` (client-side DOM filter, no XHR — minimal attack surface)

**Approval:** secured 2026-05-22.

---

## ASVS Level 1 Coverage Notes

- V1 Architecture: trust boundaries documented
- V2 Authentication: existing AspNet Identity baseline preserved
- V3 Session: cookie auth preserved (Phase 311 baseline)
- V4 Access Control: `[Authorize(Roles)]` attribute pattern (T-322-03)
- V5 Validation: strong typing + Razor encode (T-322-02, T-322-05)
- V13 API: HTMX partial endpoints same access control as parent action (T-322-03)
- V14 Configuration: no new config introduced
