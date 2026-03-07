# Architecture: CoachingProton UI Button Redesign

**Researched:** 2026-03-07
**Confidence:** HIGH (pure frontend, no unknowns)

## Current State

CoachingProton.cshtml is a single ~1500-line Razor view containing:
- 18 inline `style=` attributes (mostly `cursor:pointer`, `width:auto`, `min-width`)
- ~40 button/badge elements using Bootstrap 5 classes (`btn-*`, `badge bg-*`)
- Vanilla JS at bottom (~400 lines) that manipulates badge/button HTML via `innerHTML`
- No `@section Styles` exists in `_Layout.cshtml` -- only `@section Scripts`
- No `site.css` is referenced in layout; project uses CDN Bootstrap 5.3 + Bootstrap Icons + Font Awesome

### Key Problem Patterns

1. **Badge-as-button:** `<span class="badge bg-warning" role="button" style="cursor:pointer" ...>` -- looks like a status badge but is clickable (the `btnTinjau` elements)
2. **Duplicated markup:** Coach view (lines ~400-500) and coachee view (lines ~530-620) have near-identical button/badge blocks
3. **JS rebuilds HTML strings:** JS event handlers construct badge HTML with inline class strings (lines 1096-1200, 1418-1428), so CSS classes used must match what JS generates

## Recommendation: Inline `<style>` Block in the View

**Use a `<style>` block at the top of CoachingProton.cshtml.** Do NOT create a separate CSS file.

### Rationale

| Approach | Verdict | Why |
|----------|---------|-----|
| Separate CSS file (e.g., `coaching-proton.css`) | NO | No `site.css` pattern exists in the project. Layout has no CSS section. Adding a new file requires layout changes or a hardcoded `<link>` -- overengineered for one page. |
| Scoped CSS (`CoachingProton.cshtml.css`) | NO | ASP.NET scoped CSS uses attribute isolation which does NOT work with dynamically generated HTML from JS `innerHTML`. The JS-generated badges would not get the scoped attribute. |
| Inline styles on elements | NO | Already the current problem -- 18 scattered `style=` attributes make changes error-prone and inconsistent. |
| `<style>` block in the view | YES | Self-contained, no infrastructure changes, works with JS-generated HTML, easy to maintain for a single page. Follows KISS. |

### Implementation Structure

```html
@* At top of CoachingProton.cshtml, after the @{ } block *@
<style>
    /* CoachingProton button overrides */
    .cp-btn-evidence { /* Submit Evidence button */ }
    .cp-btn-detail { /* Lihat Detail -- needs more contrast */ }
    .cp-btn-review { /* HC Review button -- consistent in table + panel */ }
    .cp-badge-clickable { /* Replaces badge-as-button pattern */ }
    .cp-btn-action { /* Kembali, Export, Reset -- unified secondary style */ }
</style>
```

### Naming Convention

Prefix all custom classes with `cp-` (CoachingProton) to avoid collisions with Bootstrap or other views.

## Component Boundaries

| Component | What Changes | Touches JS? |
|-----------|-------------|-------------|
| Evidence column | Replace badge-as-button with proper `<button>`, add `cp-badge-clickable` | YES -- JS rebuilds evidence cell HTML |
| Lihat Detail | Change from `btn-outline-secondary` to a more visible class | No |
| HC Review button | Unify style between main table and review panel | No |
| Status badges (non-clickable) | No change -- these are display-only | No |
| Kembali / Export / Reset | Minor class updates | No |
| btnTinjau (Pending Approval) | Replace `<span badge>` with `<button>` or add `cp-badge-clickable` | YES -- click handlers target these |

## Data Flow Impact

None. This is purely CSS/HTML. No controller, model, or API changes.

## Build Order

1. **Add `<style>` block** with all `cp-*` class definitions
2. **Fix badge-as-button pattern** (btnTinjau elements) -- this is the riskiest change because JS event delegation must still find these elements
3. **Restyle Evidence column** buttons and badges
4. **Restyle Lihat Detail** button
5. **Unify HC Review** button styling (table + panel)
6. **Polish secondary buttons** (Kembali, Export, Reset)
7. **Remove all inline `style=` attributes** replaced by classes
8. **Update JS `innerHTML` strings** to use new class names

### Why This Order

- Step 1 is prerequisite for everything
- Steps 2-3 are highest risk (JS interaction) -- do early to catch issues
- Steps 4-6 are safe class swaps
- Steps 7-8 are cleanup after all styles are proven working

## Risk: JS innerHTML Coupling

The main risk is that JS code constructs HTML strings with hardcoded Bootstrap classes. When changing classes on server-rendered elements, the corresponding JS strings (lines ~1096-1200, ~1418-1428) MUST be updated in the same phase to avoid visual inconsistency after AJAX operations.

**Mitigation:** Search for every badge/button class string in the JS section and update them alongside the HTML changes. Do not split HTML and JS updates across phases.
