# Technology Stack: v3.8 CoachingProton UI Redesign

**Project:** PortalHC KPB
**Researched:** 2026-03-07
**Overall confidence:** HIGH

## Verdict: No New Libraries Needed

Bootstrap 5 + Bootstrap Icons + vanilla JS is fully sufficient. The problems on CoachingProton are CSS consistency issues (badge-as-button, washed-out colors, inconsistent sizing), not missing capabilities.

## Current Stack (No Changes)

| Technology | Version | Purpose | Status |
|------------|---------|---------|--------|
| Bootstrap 5 | 5.3.0 | Buttons, badges, utilities, spacing | KEEP |
| Bootstrap Icons | 1.10.0 | Icons in buttons | KEEP |
| Vanilla JS (ES6+) | - | Click handlers, AJAX, modals | KEEP |
| ASP.NET Core Razor | .NET 8.0 | Server-side views | KEEP |

## Why Bootstrap 5 Is Sufficient

Every redesign target maps directly to existing Bootstrap classes:

| Problem | Current Code Pattern | Fix Using Bootstrap |
|---------|---------------------|---------------------|
| Badge used as clickable button | `<span class="badge bg-warning" onclick="...">` | `<button class="btn btn-warning btn-sm">` |
| "Lihat Detail" washed out | `btn btn-outline-info btn-sm` | `btn btn-info btn-sm text-white` or custom class |
| Evidence submit vs status inconsistent | Mix of badges and buttons | Buttons for actions, badges for read-only status |
| HC Review button inconsistency | Different styles in table vs panel | Single `.btn-review` class composing Bootstrap utilities |
| Kembali/Export/Reset lack polish | Generic outline buttons | Proper icon + label combos with `btn-outline-secondary` |

## Custom CSS Needed (Minimal)

A small `<style>` block or additions to the view's existing styles. No external CSS files or preprocessors.

```css
/* Clickable status elements -- cursor + hover feedback */
.btn-status { font-size: 0.75rem; padding: 0.25rem 0.5rem; }
.btn-status:hover { filter: brightness(0.9); }

/* Consistent evidence action button */
.btn-evidence { min-width: 100px; }
```

Estimated: under 30 lines of custom CSS.

## What NOT to Add

| Library | Why Not |
|---------|---------|
| Tailwind CSS | Conflicts with Bootstrap; massive migration for zero gain |
| Animate.css | No animation requirements |
| Custom icon fonts (FontAwesome, etc.) | Bootstrap Icons already covers all needed icons |
| SASS/LESS build tooling | Not worth the build complexity for 30 lines of CSS |
| Any CSS-in-JS | Wrong ecosystem (Razor, not React/Vue) |
| jQuery plugins | Vanilla JS is already used, no jQuery dependency needed |

## Integration Points

All changes are scoped to one file: `Views/CDP/CoachingProton.cshtml` (1800+ lines).

- **Inline `<style>` block** (already exists in the view around line 50) -- add custom classes there
- **No layout changes** -- `_Layout.cshtml` untouched
- **No new partials** -- button markup changes are in-place replacements
- **No JS changes expected** -- existing `onclick` handlers and AJAX calls stay the same, only the HTML elements and CSS classes change

## Sources

- Verified in codebase: `Views/CDP/CoachingProton.cshtml` uses Bootstrap 5 classes throughout
- Verified in codebase: `Views/Shared/_Layout.cshtml` loads Bootstrap 5.3.0 and Bootstrap Icons 1.10.0
- Bootstrap 5 button documentation (HIGH confidence -- stable, well-documented API)
