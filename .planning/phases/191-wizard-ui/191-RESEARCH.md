# Phase 191: Wizard UI — Research

**Researched:** 2026-03-17
**Domain:** ASP.NET MVC Razor / Bootstrap 5 multi-step wizard (client-side show/hide)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Step 1 — Kategori:** Category dropdown + Title field. If Assessment Proton selected: also show Track dropdown
- **Step 2 — Users:** Multi-select user checkboxes with Section filter + Search. For Assessment Proton: show eligible coachees instead of normal user list
- **Step 3 — Settings:** Schedule (date+time), Duration, Status, Token toggle, PassPercentage, shuffle options, ExamWindowCloseDate, ValidUntil datepicker
- **Step 4 — Konfirmasi:** Read-only summary with grouped cards, Submit button
- Title moved from Settings to Step 1 (alongside Category)
- Progress indicator: Bootstrap nav-pills horizontal bar at top (✓ selesai hijau, ● aktif biru, ○ belum abu-abu)
- Clickable steps: only already-visited steps can be clicked (prevents skipping validation)
- Per-step inline validation: required fields marked red with error message, Selanjutnya button disabled until valid
- Step 2 minimum: at least 1 user must be selected to proceed
- Konfirmasi: 3 grouped cards (Kategori & Judul, Peserta, Settings), each with "Edit" link
- After editing from Step 4, user gets "Kembali ke Konfirmasi" button to return directly to Step 4
- Peserta card: show first 5 names + "...dan N lainnya" if >5, expandable
- Submit button: green btn-success "✓ Buat Assessment"
- ValidUntil: optional, min=today, standard `input[type=date]`, label "Tanggal Expired Sertifikat (opsional)"
- If Assessment Proton category changes back to non-Proton (or vice versa), Step 2 selections are reset
- All categories always have 4 steps — Step 2 content changes based on category
- Wizard is all client-side CSS show/hide inside a single `<form>` — POST action unchanged, no server round-trips

### Claude's Discretion

- Exact CSS classes and spacing for step indicator
- Animation/transition between steps (fade, slide, or instant)
- Exact layout of fields within Step 3 (row/column arrangement)
- Error message wording for validation failures
- How shuffle options and ExamWindowCloseDate are laid out in Step 3

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FORM-01 | Admin/HC can create assessment melalui wizard step-based (Kategori → Users → Settings → Konfirmasi) | Covered by wizard JS architecture, Bootstrap 5 nav-pills step indicator, per-step validation, Konfirmasi summary, ValidUntil model addition |
</phase_requirements>

---

## Summary

Phase 191 restructures the existing `CreateAssessment.cshtml` single-page form into a 4-step wizard using Bootstrap 5 nav-pills as a progress indicator, pure JavaScript show/hide of step panels, and per-step client-side validation. No new server routes are needed — the existing POST action signature (`CreateAssessment(AssessmentSession model, List<string> UserIds)`) stays completely unchanged.

The implementation is a Razor view rewrite plus new JavaScript. All existing behavioral JS (passPercentageManuallySet flag, token toggle, schedule combiner, Proton AJAX for eligible coachees, section filter + text search) must be preserved and adapted to work within the step structure. The success modal and toast after POST redirect also survive unchanged.

One model addition is required: `ValidUntil` (`DateTime?`) must be added to `AssessmentSession` and a new EF migration applied. This property does not yet exist. The POST action's `ModelState.Remove("ExamWindowCloseDate")` pattern should be replicated for `ValidUntil` since it is optional.

**Primary recommendation:** Write one clean rewrite of `CreateAssessment.cshtml` that (a) keeps all existing field `asp-for` bindings intact, (b) wraps each step in `<div id="step-N" class="step-panel">` divs, (c) replaces the submit button with Selanjutnya/Sebelumnya buttons except on Step 4, and (d) adds a step controller JS module at the bottom.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | 5.x (project CDN) | nav-pills, d-none, card, form-*, btn-* | Already loaded in project layout |
| Bootstrap Icons | current (project bundle) | bi-check-circle-fill, bi-circle, bi-arrow-right, etc. | Already loaded in project layout |
| Vanilla JS (ES5/ES6) | browser | Wizard controller, step validation, summary population | No build pipeline — project uses inline `<script>` blocks in Razor views |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ASP.NET MVC model binding | .NET 8 | `asp-for`, `asp-validation-for` on all fields | All form fields must retain server-side binding |
| jQuery Validation (if loaded by layout) | per layout | Not used for wizard validation — wizard uses custom JS | Only relevant for server-side re-render path |

**Installation:** No new packages. No npm installs. Everything is already available.

---

## Architecture Patterns

### Wizard Structure (Single Form)

```
<form id="createAssessmentForm" method="post" novalidate>
  <!-- Step Indicator Nav -->
  <nav class="mb-4">
    <ul class="nav nav-pills nav-fill gap-2" id="wizardStepNav">
      <li class="nav-item"><button type="button" id="pill-1">1. Kategori</button></li>
      <li class="nav-item"><button type="button" id="pill-2">2. Peserta</button></li>
      <li class="nav-item"><button type="button" id="pill-3">3. Settings</button></li>
      <li class="nav-item"><button type="button" id="pill-4">4. Konfirmasi</button></li>
    </ul>
  </nav>

  <!-- Step Panels (all in DOM, shown/hidden via d-none) -->
  <div id="step-1" class="step-panel">...</div>
  <div id="step-2" class="step-panel d-none">...</div>
  <div id="step-3" class="step-panel d-none">...</div>
  <div id="step-4" class="step-panel d-none">...</div>
</form>
```

Key invariant: the entire form is one `<form>` tag. Step panels are siblings. JS toggles `d-none` on transition. No iframes, no AJAX, no partial views.

### Pattern 1: Step Controller JS Module

**What:** A self-contained IIFE at the bottom of `@section Scripts` that owns all wizard navigation, validation, and summary rendering.

**Structure:**
```javascript
(function WizardController() {
    var currentStep = 1;
    var visitedSteps = new Set([1]);
    var returnToConfirm = false; // set true when editing from Step 4

    function goToStep(n) { ... }         // show/hide panels, update pills
    function validateStep(n) { ... }     // returns bool, adds is-invalid classes
    function populateSummary() { ... }   // builds Step 4 card content from form values
    function updatePills() { ... }       // sets pill CSS states (completed/active/pending)

    // Button event listeners
    // ...

    // Init: set Step 1 as active
    goToStep(1);
})();
```

**When to use:** Any phase with JS-only UI orchestration in a Razor view. Encapsulation prevents conflicts with the other JS blocks (Proton AJAX, token toggle, passPercentage, schedule combiner) that already exist in the page.

### Pattern 2: Per-Step Validation

**What:** On "Selanjutnya" click, validate only the fields in the current step panel. Do not use HTML5 `form.checkValidity()` globally — this would fire errors on all steps at once.

**Implementation:**
```javascript
function validateStep(n) {
    var panel = document.getElementById('step-' + n);
    var valid = true;

    // Clear previous errors in this panel
    panel.querySelectorAll('.is-invalid').forEach(function(el) {
        el.classList.remove('is-invalid');
    });

    if (n === 1) {
        var cat = panel.querySelector('#Category');
        var title = panel.querySelector('input[name="Title"]');
        if (!cat.value) { cat.classList.add('is-invalid'); valid = false; }
        if (!title.value.trim()) { title.classList.add('is-invalid'); valid = false; }
    }
    // ... steps 2, 3 similarly

    return valid;
}
```

**When to use:** Any multi-step wizard where native browser validation is insufficient.

### Pattern 3: Summary Population (Step 4)

**What:** On advancing to Step 4, read values from form fields and write them into the summary cards. Avoids any server round-trip.

```javascript
function populateSummary() {
    // Category & Title card
    document.getElementById('summary-category').textContent =
        document.getElementById('Category').options[document.getElementById('Category').selectedIndex].text;
    document.getElementById('summary-title').textContent =
        document.querySelector('input[name="Title"]').value;

    // Peserta card — collect checked boxes
    var checked = Array.from(document.querySelectorAll('.user-checkbox:checked'));
    // ... show first 5 + expand link

    // Settings card — read schedule date/time, duration, status, etc.
    // ...
}
```

### Pattern 4: "Edit from Step 4" Flow

**What:** Each summary card has an "Edit" link that calls `goToStep(n)` AND sets `returnToConfirm = true`. Within steps 1–3, when `returnToConfirm` is true, a "Kembali ke Konfirmasi" button is shown. Clicking it calls `goToStep(4)` and resets `returnToConfirm`.

```javascript
function goToStep(n, fromConfirm) {
    currentStep = n;
    visitedSteps.add(n);
    if (fromConfirm) returnToConfirm = true;
    // show/hide panels, update pills
    updateBackToConfirmButton();
}

function updateBackToConfirmButton() {
    var btn = document.getElementById('btnBackToConfirm-' + currentStep);
    if (btn) {
        if (returnToConfirm) btn.classList.remove('d-none');
        else btn.classList.add('d-none');
    }
}
```

Each step panel (1–3) needs its own `btnBackToConfirm` button (or a shared one rendered once per panel).

### Pattern 5: Proton Mode Step 2 Content Switch

**What:** Category change handler in Step 1 must also reset Step 2 selections and swap which user container is shown. Existing Proton AJAX logic (fetches eligible coachees via `GetEligibleCoachees`) is preserved.

**Integration point:** The existing `applyProtonMode(isProton)` function in the Proton IIFE already handles show/hide of `protonEligibleSection` vs the normal user list. In the wizard, this function must also:
1. Clear all checked boxes in whichever container is being hidden
2. Be called from the Step 1 category change listener inside the wizard controller

### Anti-Patterns to Avoid

- **Using HTML5 `form.reportValidity()` globally:** Fires errors on hidden step panels. Use per-panel querySelector.
- **Relying on `asp-validation-for` spans for wizard errors:** These only populate on server-side re-render. Use custom `invalid-feedback` divs adjacent to inputs.
- **Generating summary content server-side via AJAX:** The design decision is all client-side — no new controller actions.
- **Multiple `<form>` tags for steps:** All fields must submit in a single form post. Splitting forms breaks POST binding.
- **Using `disabled` on hidden input fields:** Hidden inputs inside `d-none` panels still post their values. Do not disable them unless intentional.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Step indicator visual | Custom CSS progress stepper from scratch | Bootstrap 5 `nav nav-pills nav-fill` | Already in project; consistent look |
| Date picker with min constraint | Custom date validation widget | Native `<input type="date" min="...">` attribute | Consistent with existing `ExamWindowCloseDate` and `ScheduleDate` fields |
| User search/filter | Custom typeahead/autocomplete | Existing `applyFilters()` JS function (section + text filter) | Already works; just wire to Step 2 |
| Token generation | New library | Existing `generateToken()` function | Already implemented |

---

## Common Pitfalls

### Pitfall 1: Schedule Hidden Field Combiner Fires Too Early

**What goes wrong:** The existing schedule combiner (`schedHidden.value = date + 'T' + time + ':00'`) runs on form submit. In the wizard, the submit button is on Step 4. If the combiner only runs inside the old `form.addEventListener('submit', ...)` it still works — but must not be removed when refactoring the submit handler.

**How to avoid:** Keep the schedule combiner logic inside the form's `submit` event listener. The wizard's "Selanjutnya" button does NOT submit the form — it only navigates. The form submit fires only when Step 4's "Buat Assessment" button is clicked, at which point the combiner logic runs normally.

### Pitfall 2: ValidUntil Model Property Missing

**What goes wrong:** `AssessmentSession` does not have a `ValidUntil` property yet. Binding `asp-for="ValidUntil"` will throw a compile error or silently not bind.

**How to avoid:** Add `public DateTime? ValidUntil { get; set; }` to `AssessmentSession.cs` and generate a new EF Core migration before the view is used. Also add `ModelState.Remove("ValidUntil")` in the POST action (same pattern as `ExamWindowCloseDate`) since it is optional. The POST action processes the value naturally if the property exists.

**Warning signs:** Razor compilation error referencing `ValidUntil` on `AssessmentSession`; or field silently not saving.

### Pitfall 3: passPercentageManuallySet Flag Reset Between Steps

**What goes wrong:** The `passPercentageManuallySet` variable is declared inside the `DOMContentLoaded` closure. In the wizard, the Category field is in Step 1 and PassPercentage is in Step 3. Navigating between steps does not re-initialize page JS — the flag persists correctly. But if the Proton IIFE and the DOMContentLoaded block both attach `change` listeners to `#Category`, one may shadow the other.

**How to avoid:** Consolidate the category change handler into one place (the wizard controller), or use `dispatchEvent` to trigger the existing handler. Do NOT attach two separate `change` listeners to `#Category`.

### Pitfall 4: Proton Eligible Coachees Section Visibility

**What goes wrong:** The existing code hides/shows `#protonEligibleSection` and `#protonUserCheckboxContainer` relative to their container structure. After restructuring into Step 2, if the container IDs change or move, the Proton IIFE's DOM references break.

**How to avoid:** Preserve the existing IDs: `protonFieldsSection`, `protonEligibleSection`, `protonUserCheckboxContainer`, `protonEligibleStatus` must remain on the same elements they are today. Only their parent wrapper changes (now inside `#step-2` instead of the main form card).

### Pitfall 5: Step 4 Peserta Count Shows Wrong Value

**What goes wrong:** If Proton mode is active, the selected users come from `protonUserCheckboxContainer` checkboxes, not `userCheckboxContainer`. The summary population code must detect which container is active and collect from the correct one.

**How to avoid:** Detect Proton mode by checking `document.getElementById('Category').value === 'Assessment Proton'` at summary population time, then query the appropriate container.

### Pitfall 6: Success Modal After POST Redirect

**What goes wrong:** The POST action redirects to `CreateAssessment` (GET) with `TempData["CreatedAssessment"]`. The GET action passes this to `ViewBag.CreatedAssessment`, which is then written to `<script type="application/json" id="createdAssessmentData">`. The existing JS on DOMContentLoaded reads this JSON and shows the success modal. After the wizard rewrite, the modal and its JS must still be present.

**How to avoid:** Keep the success modal HTML and its JS initialization code unchanged in the rewritten view. The wizard restructure only affects the form area, not the modal.

### Pitfall 7: Visited Steps Set — Pill Clickability

**What goes wrong:** If a user goes forward, then backward (via Sebelumnya), the visited set must contain all visited steps so they remain clickable. Steps the user has never reached must not be clickable.

**How to avoid:** Add step number to `visitedSteps` in `goToStep()`. Pill click handlers check `visitedSteps.has(n)` before navigating. Initial state: `visitedSteps = new Set([1])` (Step 1 is always visited on page load).

---

## Code Examples

Verified patterns from existing codebase:

### Existing Schedule Date+Time Combiner (must survive rewrite)
```javascript
// Source: CreateAssessment.cshtml line 720-723
if (schedDateInput && schedDateInput.value && schedTimeInput && schedHidden) {
    schedHidden.value = schedDateInput.value + 'T' + (schedTimeInput.value || '08:00') + ':00';
}
```
This must remain in the form `submit` event handler, not in step navigation.

### Existing Proton Mode Toggle (must survive rewrite)
```javascript
// Source: CreateAssessment.cshtml ~line 791-797
function applyProtonMode(isProton) {
    if (isProton) {
        protonSection.classList.remove('d-none');
        standardUserCol.style.display = 'none';
    } else {
        protonSection.classList.add('d-none');
        standardUserCol.style.display = '';
        // Restore duration + passPercentage
    }
}
```
In the wizard, `standardUserCol` becomes the Step 2 normal-user container. The reference must be updated.

### Bootstrap 5 Nav-Pills Step Indicator
```html
<!-- Source: Bootstrap 5 docs + UI-SPEC.md -->
<nav class="mb-4">
  <ul class="nav nav-pills nav-fill gap-2" id="wizardStepNav">
    <li class="nav-item">
      <button type="button" class="nav-link bg-primary text-white active" id="pill-1">
        <i class="bi bi-circle-fill me-1"></i>1. Kategori
      </button>
    </li>
    <li class="nav-item">
      <button type="button" class="nav-link text-muted border" id="pill-2" disabled>
        <i class="bi bi-circle me-1"></i>2. Peserta
      </button>
    </li>
    <!-- x4 -->
  </ul>
</nav>
```

### ValidUntil Model Property Addition
```csharp
// Add to Models/AssessmentSession.cs
/// <summary>
/// Optional certificate expiry date. Null = certificate has no expiry.
/// Only relevant when GenerateCertificate = true.
/// </summary>
public DateTime? ValidUntil { get; set; }
```

### ValidUntil POST Action Guard (AdminController.cs)
```csharp
// Add after line 1017 (ModelState.Remove("ExamWindowCloseDate"))
ModelState.Remove("ValidUntil"); // optional field
```

### ValidUntil Field in Step 3
```html
<div class="col-md-6">
  <label asp-for="ValidUntil" class="form-label fw-bold">
    Tanggal Expired Sertifikat (opsional)
  </label>
  <input asp-for="ValidUntil" type="date" class="form-control"
         id="ValidUntil" min="@DateTime.Today.ToString("yyyy-MM-dd")" />
  <div class="form-text">Kosongkan jika sertifikat tidak memiliki batas waktu.</div>
</div>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single flat form for CreateAssessment | 4-step wizard with progress indicator | Phase 191 | Improved UX for complex form |
| Alert-based validation (browser `alert()`) | Inline `is-invalid` + `invalid-feedback` div per field | Phase 191 | Consistent with Bootstrap form patterns |
| Hard-coded category strings in form | DB-driven categories with `data-pass-percentage` | Phase 190 | Category options now come from `AssessmentCategories` table |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework detected for Razor views) |
| Config file | none |
| Quick run command | `dotnet run` + navigate to `/Admin/CreateAssessment` |
| Full suite command | Manual checklist per use-case flow |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FORM-01 | 4-step wizard navigation with per-step validation | manual browser | n/a | ❌ Wave 0 (no test file) |
| FORM-01 | Back preserves state (selections intact) | manual browser | n/a | ❌ Wave 0 |
| FORM-01 | Multi-user selection intact after nav | manual browser | n/a | ❌ Wave 0 |
| FORM-01 | Konfirmasi summary matches entered data | manual browser | n/a | ❌ Wave 0 |
| FORM-01 | Submit calls existing POST unchanged | manual browser / server log | n/a | ❌ Wave 0 |
| FORM-01 | ValidUntil saves to DB when set | manual browser / DB query | n/a | ❌ Wave 0 |
| FORM-01 | Proton mode: Step 2 shows coachees not users | manual browser | n/a | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet build` — zero compile errors
- **Per wave merge:** Full manual browser test checklist
- **Phase gate:** All FORM-01 manual test cases pass before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] No automated test files — this phase is entirely manual browser verification
- [ ] Verify `dotnet build` passes after `ValidUntil` model addition and migration

---

## Open Questions

1. **Does ValidUntil belong to Phase 191 or Phase 192?**
   - What we know: CONTEXT.md Step 3 lists ValidUntil as a wizard field; REQUIREMENTS.md maps CERT-01 to Phase 192; `AssessmentSession` has no `ValidUntil` property today
   - What's unclear: Whether the planner should add the model property in Phase 191 (needed for form binding) or leave a placeholder input that Phase 192 wires up
   - Recommendation: Add `ValidUntil` to `AssessmentSession` in Phase 191 (it is required for the form to compile). Phase 192 then uses the already-stored value for certificate generation. This is the cleanest split.

2. **Token field layout in Step 3**
   - What we know: Token toggle + conditional AccessToken input is currently in the main form; UI-SPEC puts it in Step 3 col-md-6
   - What's unclear: The token input can expand (shows an extra div). Ensure the conditional `d-none` toggle still works within the two-column layout without layout shift.
   - Recommendation: Keep token toggle and its conditional container in a single `col-12` row inside Step 3 (same as current layout), placed after the PassPercentage row.

---

## Sources

### Primary (HIGH confidence)

- `Views/Admin/CreateAssessment.cshtml` — Full current form: all field IDs, existing JS logic, Proton AJAX, schedule combiner, token toggle
- `Controllers/AdminController.cs` lines 904–1033 — GET and POST `CreateAssessment` actions: ViewBag setup, ModelState.Remove patterns, re-render path
- `Models/AssessmentSession.cs` — Current model: confirms `ValidUntil` is absent, `ExamWindowCloseDate` pattern to replicate
- `.planning/phases/191-wizard-ui/191-CONTEXT.md` — Locked decisions on step grouping, Proton flow, navigation, ValidUntil
- `.planning/phases/191-wizard-ui/191-UI-SPEC.md` — Visual contract: pill states, field layout per step, copy, spacing, color

### Secondary (MEDIUM confidence)

- `.planning/REQUIREMENTS.md` — FORM-01 + CERT-01 requirement-to-phase mapping
- Bootstrap 5 docs (nav-pills, d-none, form validation classes) — well-known, stable API

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — Bootstrap 5 + Vanilla JS already in use; no new dependencies
- Architecture: HIGH — single-form show/hide is confirmed locked decision; all patterns derive from existing code
- Pitfalls: HIGH — identified from direct code reading of existing JS, model state, and field IDs
- ValidUntil gap: HIGH — confirmed by reading AssessmentSession.cs; property is absent

**Research date:** 2026-03-17
**Valid until:** 2026-04-17 (stable Bootstrap 5 + .NET 8 stack)
