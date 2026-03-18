# Phase 188: AJAX Filter Bar - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Filter bar untuk CertificationManagement page — Bagian/Unit cascade, status, tipe (Training/Assessment), free-text search. Semua filter update tabel + summary cards via AJAX tanpa reload.

</domain>

<decisions>
## Implementation Decisions

### Filter Bar Layout & Controls
- Filter bar diposisikan di atas tabel, dalam card yang sama (konsisten dengan Dashboard Proton)
- Bagian/Unit menggunakan cascade dropdown — reuse GetCascadeOptions pattern dari CDPController
- Search field instant dengan debounce 300ms (keyup)
- Tombol Reset yang clear semua filter + reload data

### AJAX Behavior & Summary Cards
- Summary cards (Total, Aktif, Akan Expired, Expired) di-update dari filtered dataset — AJAX response include counts
- Pagination reset ke page 1 setiap kali filter berubah
- Loading indicator: opacity 0.5 + spinner pada container tabel (pattern Dashboard)
- URL tidak di-update (no pushState) — filter state hanya di JS

### Claude's Discretion
- Detail implementasi partial view structure
- Exact debounce implementation approach

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CDPController.GetCascadeOptions(section)` — returns units for section + categories (reusable for Bagian/Unit cascade)
- `CDPController.FilterCoachingProton()` — AJAX pattern: returns PartialView with filtered data
- `Dashboard.cshtml` JS — fetch + AbortController pattern, cascade wiring, loading class toggle
- `PaginationHelper` — server-side pagination already used in CertificationManagement action
- `OrganizationStructure.GetUnitsForSection()` — cascade data source

### Established Patterns
- AJAX filter: controller action returns `PartialView("_PartialName", model)`
- JS: `fetch('/CDP/Action?' + params)` → replace container innerHTML
- AbortController for cancelling in-flight requests
- Loading state: add/remove CSS class on container

### Integration Points
- `CDPController.CertificationManagement` — existing action to refactor (add filter params)
- `BuildSertifikatRowsAsync()` — existing helper returns all rows, needs filter params
- `CertificationManagementViewModel` — needs filter property additions for active filter state
- `Views/CDP/CertificationManagement.cshtml` — split table into partial view

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches following existing Dashboard AJAX pattern.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>
