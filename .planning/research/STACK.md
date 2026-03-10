# Stack Research: Homepage Minimalist Redesign

**Domain:** ASP.NET Core MVC Portal — UI Simplification
**Researched:** 2026-03-10
**Confidence:** HIGH

## Summary

Homepage redesign requires **ZERO new library additions**. The existing validated stack (Bootstrap 5.3, Chart.js, jQuery, Font Awesome, Bootstrap Icons, Inter font, AOS) is complete. This is a **pure CSS refactor** — removing glassmorphism, gradients, and animations from `home.css` to match the clean, simple card styling already established in CMP/CDP pages.

## Recommended Stack

### Core Technologies (No Changes)

| Technology | Current Version | Purpose | Status |
|------------|-----------------|---------|--------|
| Bootstrap | 5.3.0 | Card components, grid, responsive layout | Keep as-is (already used in CMP/CDP) |
| Font Awesome | 6.5.1 | Icon library for cards | Keep as-is |
| Bootstrap Icons | 1.10.0 | Icon library for badges/UI | Keep as-is |
| Inter Font | 300–800 weights | Typography system | Keep as-is |
| jQuery | 3.7.1 | Legacy AJAX/DOM manipulation | Keep as-is |

### Supporting Libraries (Reduced Usage on Homepage)

| Library | Current Version | Purpose | Homepage Status |
|---------|-----------------|---------|-----------------|
| AOS (Animate On Scroll) | 2.3.1 | Scroll animations | Remove from homepage; keep for other pages |
| Chart.js | Latest via CDN | Dashboard visualizations | Not used on homepage (keep in _Layout.cshtml) |

## What to Use on Homepage (CMP/CDP Pattern)

Homepage should follow the **exact styling pattern** already proven in CMP/CDP Index views:

```html
<div class="card border-0 shadow-sm h-100">
  <div class="card-body">
    <div class="d-flex align-items-center mb-3">
      <div class="icon-box bg-primary bg-opacity-10 text-primary rounded-3 p-3 me-3">
        <i class="bi bi-[icon] fs-3"></i>
      </div>
      <div>
        <h5 class="mb-0">Card Title</h5>
        <small class="text-muted">Subtitle</small>
      </div>
    </div>
    <p class="text-muted mb-3">Description text</p>
    <a href="#" class="btn btn-primary w-100">Action</a>
  </div>
</div>
```

**Key Bootstrap utility classes to embrace:**
- `border-0` — Remove default card border
- `shadow-sm` — Light, subtle shadow (no glassmorphism)
- `bg-opacity-10` — Subtle background color for icon boxes
- `rounded-3` — Bootstrap's rounded corners (no custom blur/effects)
- `text-muted` — Standard gray text for descriptions
- `g-4` — Gap/spacing between grid items
- `h-100` — Full height cards

## What NOT to Use on Homepage

| Avoid (Remove from home.css) | Why | Use Instead |
|------------------------------|-----|-------------|
| Glassmorphism effects (backdrop-filter: blur) | Creates visual complexity, inconsistent with CMP/CDP | Plain Bootstrap cards with `shadow-sm` |
| Custom gradient backgrounds | Overcomplicated hero section | Simple `bg-primary` or solid color background |
| CSS gradient text (`-webkit-background-clip: text`) | Hard to maintain, accessibility issues | Plain text or `text-primary` classes |
| Circular progress SVG with gradients | Unnecessary complexity for status display | Simple progress bars or text percentage |
| AOS animation library on homepage | Page feels cluttered, inconsistent with CDP simplicity | Bootstrap `fade` animations only (CSS-only, no JS) |
| Custom timeline styling with pseudo-elements | Recent Activity section will be removed | N/A (section removed per project brief) |
| Rounded pill buttons (`rounded-pill`) | Too playful for professional portal | Standard Bootstrap button classes (`btn-primary`, etc.) |
| Hero section with gradient + blur overlays | Excessive visual weight | Simple solid background with clear typography |

## CSS Changes Scope

### Remove from home.css

1. **Hero section glassmorphism**
   - Remove `::before` and `::after` pseudo-elements with blur effects
   - Remove `backdrop-filter: blur(10px)` from `.hero-badge` and `.hero-avatar`
   - Simplify to solid background or Bootstrap's `bg-primary` class

2. **Glass card styling**
   - Remove entire `.glass-card` class and variants
   - Remove `backdrop-filter`, custom borders, gradient overlays
   - Replace with Bootstrap's `.card.border-0.shadow-sm`

3. **Circular progress visualization**
   - Remove SVG-based circular progress
   - Replace with simple text percentage or Bootstrap progress bar if needed

4. **Quick Access card animations**
   - Remove `transform: scale()` and custom transitions
   - Use Bootstrap's standard card styling with `shadow-sm`
   - Keep simple hover effect: `box-shadow` only (no transform)

5. **Timeline and Deadline cards**
   - Remove `.timeline` and `.deadline-card` sections entirely (per project spec)
   - Remove custom gradient borders, markers, pseudo-elements

6. **Custom gradient utilities**
   - Remove `:root` CSS variables for gradients
   - Remove `.gradient-text` class
   - Remove `.badge-gradient` class

### Keep from home.css

1. **Responsive design media queries** (adapt to new simpler card structure)
2. **Typography hierarchy** (keep Inter font sizing, but simplify)
3. **Icon box styling** (adapt to Bootstrap's `bg-opacity-10` pattern)
4. **Basic spacing and padding** (Bootstrap grid already handles this)

### Add/Verify in home.css

1. **Simple card hover effect**
   ```css
   .card {
       transition: transform 0.2s, box-shadow 0.2s;
   }
   .card:hover {
       transform: translateY(-5px);
       box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
   }
   ```
   *(Copy from CMP/CDP Index views — already proven pattern)*

2. **Icon box standardization**
   ```css
   .icon-box {
       width: 60px;
       height: 60px;
       display: flex;
       align-items: center;
       justify-content: center;
   }
   ```
   *(Copy from CMP/CDP Index views)*

## Installation / Update

**NO npm packages to install or update.**

Only action required:
1. Edit `wwwroot/css/home.css` to remove glassmorphism/animation styles
2. Update `Views/Home/Index.cshtml` to remove:
   - `data-aos` attributes from all elements
   - Glass card classes → use Bootstrap `.card.border-0.shadow-sm`
   - Circular progress SVG → use simple text percentage
   - Timeline section (Recent Activity) → remove entirely per spec
   - Upcoming Deadlines section → remove entirely per spec
   - Quick Access cards → simplify styling to match CMP/CDP pattern

3. `Views/Shared/_Layout.cshtml`: No changes needed (AOS library stays for other pages)

## Stack Validation Against Existing Pages

Homepage redesign maintains **full compatibility** with CMP/CDP:

| Component | Bootstrap Version | Used On | Notes |
|-----------|------------------|---------|-------|
| Card component | 5.3.0 | CMP Index, CDP Index, Homepage (new) | No version conflicts |
| Icon system | FA 6.5.1 + BI 1.10.0 | All pages | No changes needed |
| Spacing/grid | Bootstrap 5.3 utilities | All pages | Harmonizes design language |
| Shadows | Bootstrap `.shadow-sm` utility | CMP/CDP (new on Homepage) | No custom shadow CSS needed |

## Why This Stack for Minimalist Redesign

1. **Bootstrap 5.3 is proven** — CMP/CDP pages already use it successfully with clean card styling
2. **Zero additional libraries** — avoids bloat, maintains performance
3. **Reduced JavaScript dependency** — removing AOS from homepage reduces JS execution
4. **Consistency with platform** — users see unified design language across all features
5. **Maintainability** — simple Bootstrap utilities are easier to update than custom glassmorphism CSS
6. **Accessibility** — plain text and standard contrasts better than gradient text
7. **Performance** — no `backdrop-filter` (GPU-intensive) = faster paint operations

## Alternatives Considered

| Original Choice | Alternative | Why Not Used |
|-----------------|-------------|--------------|
| Remove glassmorphism entirely | Keep limited glassmorphism on hero only | Dilutes simplification goal; CMP/CDP don't use it anywhere |
| Keep AOS animations on homepage | Remove all AOS from homepage | Homepage should feel fast/direct; animations clash with minimalist theme |
| Create custom "hybrid" CSS | Follow CMP/CDP pattern exactly | Reduces tech debt; easier for future maintainers |
| Use custom progress visualization | Bootstrap progress bar or text-only | Text percentage is clearest for IDP status |

## Compatibility Notes

- **Bootstrap 5.3.0** is compatible with the existing _Layout.cshtml setup — no version upgrades needed
- **Removing AOS from homepage** does NOT break _Layout.cshtml (library loaded globally but won't initialize on removed `data-aos` attributes)
- **Custom home.css** will be much smaller after glassmorphism removal (~40% size reduction estimated)

## Sources

- **Existing codebase validation:**
  - `Views/CMP/Index.cshtml` — Reference pattern for simple card styling
  - `Views/CDP/Index.cshtml` — Reference pattern for simple card styling
  - `Views/Shared/_Layout.cshtml` — Verified Bootstrap 5.3.0 and dependencies
  - `wwwroot/css/home.css` — Current implementation (512 lines of glassmorphism)

- **Bootstrap 5.3 documentation** — Standard card and utility class usage (already proven in use)
- **User preference** — From MEMORY.md: "User loves clean Bootstrap styling (as in CMP/CDP pages)"

---

**Stack Research for:** Homepage Minimalist Redesign — ASP.NET Core MVC Portal
**Researched:** 2026-03-10
**Conclusion:** No new libraries needed. Pure CSS refactor to match proven CMP/CDP styling pattern.
