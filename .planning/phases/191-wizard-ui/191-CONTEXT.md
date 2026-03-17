# Phase 191: Wizard UI - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Restructure the existing single-page CreateAssessment form into a 4-step wizard (Kategori → Users → Settings → Konfirmasi) with per-step client-side validation, a summary confirm step, and a ValidUntil date picker. All client-side — the existing POST action stays unchanged, no new server round-trips between steps.

</domain>

<decisions>
## Implementation Decisions

### Step Content & Grouping
- **Step 1 — Kategori:** Category dropdown + Title field. If Assessment Proton selected: also show Track dropdown
- **Step 2 — Users:** Multi-select user checkboxes with Section filter + Search. For Assessment Proton: show eligible coachees instead of normal user list
- **Step 3 — Settings:** Schedule (date+time), Duration, Status, Token toggle, PassPercentage, shuffle options, ExamWindowCloseDate, ValidUntil datepicker
- **Step 4 — Konfirmasi:** Read-only summary with grouped cards, Submit button
- Title moved from Settings to Step 1 (alongside Category)

### Assessment Proton Flow
- Step 1: Category + Track selection (eligible coachee list moves to Step 2)
- Step 2: Shows eligible coachees (not normal user list) when Proton is selected
- All categories always have 4 steps — Step 2 content changes based on category
- If user changes category (Proton ↔ non-Proton), Step 2 selections are reset

### Step Navigation & Validation
- Progress indicator: Bootstrap nav-pills horizontal bar at top (✓ selesai hijau, ● aktif biru, ○ belum abu-abu)
- Clickable steps: only already-visited steps can be clicked (prevents skipping validation)
- Per-step inline validation: required fields marked red with error message, Selanjutnya button disabled until valid
- Step 2 minimum: at least 1 user must be selected to proceed

### Konfirmasi Step Layout
- 3 grouped cards: Kategori & Judul, Peserta, Settings
- Each card has an "Edit" link that jumps to that step
- After editing from Step 4, user gets a "Kembali ke Konfirmasi" button to return directly to Step 4
- Peserta card: show first 5 names + "...dan N lainnya" if >5, expandable
- Submit button: green (btn-success) "✓ Buat Assessment"

### ValidUntil Datepicker
- Located in Step 3 (Settings) alongside other assessment settings
- Optional field — if not set, certificate has no expiry
- Minimum date constraint: must be >= today (future dates only)
- Standard HTML `input type="date"` — consistent with existing Schedule date field

### Claude's Discretion
- Exact CSS classes and spacing for step indicator
- Animation/transition between steps (fade, slide, or instant)
- Exact layout of fields within Step 3 (row/column arrangement)
- Error message wording for validation failures
- How shuffle options and ExamWindowCloseDate are laid out in Step 3

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment Form (primary file being modified)
- `Views/Admin/CreateAssessment.cshtml` — Current single-page form; all fields, JS logic for category change, Proton fields toggle, token toggle, pass percentage auto-set
- `Controllers/AdminController.cs` — CreateAssessment GET (ViewBag setup) and POST (form processing) — POST action must NOT change

### Phase 190 artifacts (just completed — DB categories)
- `Models/AssessmentCategory.cs` — Category entity with DefaultPassPercentage
- `.planning/phases/190-db-categories-foundation/190-RESEARCH.md` — Documents current form structure, ViewBag patterns, passPercentageManuallySet flag behavior

### UI conventions
- `Views/Admin/ManageCategories.cshtml` — Recent Bootstrap 5 page pattern (breadcrumb, cards, forms)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Bootstrap 5 nav-pills: already available in project for step indicator
- `form-select`, `form-control`, `form-check` classes: all existing form patterns
- Category `data-pass-percentage` attributes: Phase 190 already added these to `<option>` elements

### Established Patterns
- `needs-validation` + `novalidate` on form: existing client-side validation bootstrap pattern
- ViewBag dropdown pattern: Categories, Sections, ProtonTracks all loaded via ViewBag in GET action
- Proton eligible coachee AJAX: existing JS fetches eligible coachees when Track is selected

### Integration Points
- Form wraps all steps in a single `<form>` — wizard is purely CSS show/hide of step panels
- Existing POST action receives the same form fields — wizard restructure is transparent to server
- `passPercentageManuallySet` flag and category change handler must survive the restructure
- Token toggle JS, Schedule date+time combiner JS must work within wizard steps

</code_context>

<specifics>
## Specific Ideas

- Wizard layout similar to the preview mockups discussed — horizontal nav-pills at top, card-based content per step
- Konfirmasi step uses 3 grouped Bootstrap cards with Edit links per card
- "Kembali ke Konfirmasi" shortcut button when editing from Step 4

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 191-wizard-ui*
*Context gathered: 2026-03-17*
