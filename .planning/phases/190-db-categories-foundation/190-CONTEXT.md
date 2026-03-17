# Phase 190: DB Categories Foundation - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Move hardcoded assessment categories to a database table with Admin/HC CRUD management. Update CreateAssessment and EditAssessment forms to load categories from DB. Replace JS `categoryDefaults` object with `data-pass-percentage` attributes on `<option>` elements.

</domain>

<decisions>
## Implementation Decisions

### Category CRUD Placement
- Dedicated page at Admin/ManageCategories — not inline or tab-based
- New card in Admin/Index hub Section B (Kelola Assessment)
- Access: Admin + HC roles (same as ManageAssessment)

### Category Data Model
- Fields: `Id`, `Name` (string), `DefaultPassPercentage` (int), `IsActive` (bool), `SortOrder` (int)
- Categories stay as plain strings on AssessmentSession.Category — no FK relationship (protects historical data)
- 6 seed rows: OJT (70%), IHT (70%), Training Licencor (80%), OTS (70%), Mandatory HSSE Training (100%), Assessment Proton (70%)

### Delete Behavior
- Allow delete even if sessions reference the category — string stays on existing sessions
- No FK constraint means historical data is never broken by category management
- IsActive toggle available as softer alternative to deletion

### Form Sync Scope
- Both CreateAssessment.cshtml AND EditAssessment.cshtml updated in this phase
- All hardcoded category lists removed from views — zero hardcoded categories remain after this phase
- `categoryDefaults` JS object in CreateAssessment replaced with `data-pass-percentage` attributes on `<option>` elements
- EditAssessment category dropdown also loads from DB via ViewBag

### Claude's Discretion
- ManageCategories page layout and table styling
- Exact SortOrder default values for seed data
- Validation messages and error handling UX

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment Form (existing code to modify)
- `Views/Admin/CreateAssessment.cshtml` — Hardcoded categories at lines 7-15, categoryDefaults JS at line 538, passPercentageManuallySet logic
- `Views/Admin/EditAssessment.cshtml` — Hardcoded categories at lines 11-16, same dropdown pattern
- `Controllers/AdminController.cs` — CreateAssessment GET (lines 759-789), POST (lines 795-1105), EditAssessment actions; controller branches on `model.Category == "Assessment Proton"`

### Admin Hub
- `Views/Admin/Index.cshtml` — Section B (Kelola Assessment) where new ManageCategories card goes

### Research
- `.planning/research/STACK.md` — No new packages needed
- `.planning/research/PITFALLS.md` — categoryDefaults/seed string mismatch is #1 pitfall
- `.planning/research/ARCHITECTURE.md` — Integration points and build order

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Admin hub card pattern: existing cards in Admin/Index.cshtml Section B follow consistent Bootstrap card layout
- ViewBag dropdown pattern: `ViewBag.ProtonTracks`, `ViewBag.Sections` already loaded in CreateAssessment GET — same pattern for categories
- ManageWorkers CRUD pattern: full-page CRUD with table list, add/edit/delete — reusable for ManageCategories

### Established Patterns
- Soft-delete via IsActive flag: used on Workers, Silabus — same pattern for categories
- Seed data via DbContext.OnModelCreating or migration HasData
- AuditLog: all admin actions logged — ManageCategories should follow same pattern

### Integration Points
- CreateAssessment GET: add `ViewBag.Categories` from DbContext
- EditAssessment GET: add `ViewBag.Categories` from DbContext
- CreateAssessment.cshtml: replace hardcoded `<option>` list + remove `categoryDefaults` JS
- EditAssessment.cshtml: replace hardcoded `<option>` list
- Admin/Index.cshtml: add card to Section B
- AppDbContext: add `DbSet<AssessmentCategory>`
- EF Migration: new AssessmentCategories table with seed data

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches following existing Admin CRUD patterns.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 190-db-categories-foundation*
*Context gathered: 2026-03-17*
