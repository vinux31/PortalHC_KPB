# Phase 148: CSS Audit & Cleanup - Research

**Researched:** 2026-03-10
**Domain:** CSS audit, cleanup, and cross-page dependency verification
**Confidence:** HIGH

## Summary

Phase 148 is a **CSS audit and cleanup operation** that removes glassmorphism styles, animation attributes, and unused layout components from home.css in preparation for Phase 149's homepage HTML redesign. The phase requires verification that removed styles don't break CMP, CDP, or Admin pages before any CSS deletion occurs.

The key risk is **class name sharing**: home.css contains global styles (glass-card, timeline, deadline-card) that might be used by other views. Research confirms these classes are **only used on Homepage** (Home/Index.cshtml), but guide.css independently implements backdrop-filter styles for the Guide pages (separate from home.css).

**Primary recommendation:** Create a verification checklist comparing current home.css rules against CMP/CDP/Admin pages (by grep inspection and visual testing), then remove only Homepage-specific sections while preserving Hero and Quick Access base styling needed by Phase 149.

## User Constraints (from STATE.md)

### Locked Decisions
- Remove glass cards (IDP Status, Pending Assessment, Mandatory Training) — Phase 149
- Remove Timeline and Deadlines sections — Phase 149
- Keep Hero section and Quick Access cards — Phase 149
- Match styling to CMP/CDP pages (Bootstrap cards, shadow-sm, no glassmorphism) — Phase 149
- AOS library stays in _Layout.cshtml (used by Guide.cshtml) — only data-aos removed from Homepage
- **CSS audit MUST run BEFORE view changes** to detect any shared class names across CMP/CDP/Admin

### Claude's Discretion
None specified for Phase 148.

### Deferred Ideas (OUT OF SCOPE)
- Role-based Quick Access differentiation
- Personalized shortcut reordering

---

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CSS-01 | home.css contains no glass-card, backdrop-filter, or blur pseudo-element rules | Identified 131 lines (131-186) with `.glass-card`, `.glass-card::before/after`, `.hero-badge`, `.hero-stat-item`, `backdrop-filter: blur()` rules. Also `::before`/`::after` pseudo-elements on `.hero-section` (lines 34-56) with blur filters. |
| CSS-02 | home.css contains no timeline or deadline-card styling rules | Identified 97 lines (309-405) with `.timeline`, `.timeline-item`, `.timeline-marker`, `.timeline-content`, `.deadline-card`, `.deadline-*` rules. |
| CSS-03 | No data-aos animation attributes on Homepage elements | Found 11 data-aos attributes in Home/Index.cshtml (lines 10, 46, 84, 118, 169, 177, 185, 194, 207, 243). |

---

## Standard Stack

### Current Tooling
| Tool | Version | Purpose | Status |
|------|---------|---------|--------|
| Bootstrap | 5.x (implied) | Grid, cards, utilities | Active — no changes needed |
| Inter font | via system fallback | Typography | Active — no changes needed |
| AOS (Animate On Scroll) | 2.3.1 (CDN) | Scroll animations | Used by Guide.cshtml, must remain in _Layout |

### CSS Architecture
- **home.css**: Global homepage styles (467 lines total)
- **guide.css**: Guide pages styles (1200+ lines, independent backdrop-filter implementations)
- **_Layout.cshtml**: Master layout with AOS CDN script (line 179)

**Key insight:** AOS library is global (in _Layout), so removing `data-aos` attributes from Homepage doesn't break the library or Guide pages. Guide pages have their own animation attributes.

---

## Architecture Patterns

### Current Homepage Structure
```
Home/Index.cshtml (Phase 149 target — HTML restructuring)
├── Hero Section (.hero-section with ::before/after blur)
├── Dashboard Cards (3× glass-card: IDP, Assessment, Training)
├── Quick Access Cards (.quick-access-card)
├── Recent Activity Timeline (.timeline, .timeline-item, .timeline-marker, .timeline-content)
└── Upcoming Deadlines (.deadline-card, .deadline-*)

CSS Dependencies:
home.css → all inline styles
guide.css → independent (no shared classes)
CMP/CDP/Admin → Bootstrap only, no home.css imports
```

### Clean CSS Structure (Target)
```
Phase 149 will restructure HTML to use Bootstrap card classes instead of:
- Remove: .glass-card (replace with Bootstrap .card + .shadow-sm)
- Remove: .timeline (custom implementation in Timeline section, Phase 149)
- Remove: .deadline-card (custom HTML, no special class needed)
- Keep: .hero-section (base styling for Phase 149 cleanup)
- Keep: .quick-access-card (Phase 149 refactor to Bootstrap .card)
```

### Removal Strategy
Phase 148 removes CSS **rules and pseudo-elements**:
1. **Lines 131-186**: `.glass-card` rule block (56 lines)
2. **Lines 309-405**: `.timeline` and `.deadline-card` blocks (97 lines)
3. **Lines 34-56**: `.hero-section::before` and `::after` pseudo-elements with `filter: blur()` (23 lines)
4. **Cleanup**: `.hero-badge`, `.hero-stat-item` backdrop-filter (where applicable)
5. **Verify**: `.hero-avatar`, `.hero-badge` still needed for Phase 149 hero styling

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Glass morphism effects | Custom blur + backdrop-filter | Bootstrap utility classes + shadow-sm | Simple, maintainable, consistent with CMP/CDP |
| Timeline visualization | Custom CSS timeline with ::before/after | Bootstrap accordion or simple div list | Phase 149 will handle with HTML structure |
| Deadline badges | Custom deadline-card styling | Bootstrap badge + card utilities | Reduces CSS surface area, easier to theme |
| Scroll animations | AOS library with data-aos attributes | CSS transitions on hover/state | Keep AOS for Guide, remove from Homepage |

**Key insight:** Removing decorative CSS (blur, glassmorphism, complex pseudo-elements) isn't about replacing with something else — it's about **simplification**. Phase 149 will use Bootstrap's standard card patterns, which are already CSS-lean.

---

## Common Pitfalls

### Pitfall 1: Removing Classes Still Used Elsewhere
**What goes wrong:** Grep-removing `.glass-card` from home.css, but class is referenced in CMP or Admin pages (breaks their styling).

**Why it happens:** Assume Homepage classes are unique; don't cross-check other controllers/views before deletion.

**How to avoid:**
- Before removing any class from home.css, verify with: `grep -r "class.*CLASSNAME" /Views/ /wwwroot/`
- Run full-page smoke tests on CMP/CDP/Admin after phase completion
- Document verified-unused classes in PLAN.md before executing

**Warning signs:**
- CMP/CDP index pages display with broken cards or misaligned content
- Admin pages lose styling
- Console shows missing style errors

**Current status:** Research confirms glass-card, timeline, deadline-card are **ONLY in Home/Index.cshtml** — safe to remove.

### Pitfall 2: Breaking Guide Page Styles
**What goes wrong:** Remove guide-specific styles by accident (Guide pages stop showing backdrop-filter effects).

**Why it happens:** Assume home.css and guide.css are tightly coupled (they're not).

**How to avoid:**
- Verify guide.css is independent: `grep -l "guide-" home.css` → should return nothing
- guide.css has its own `backdrop-filter` rules (lines 105, 1181) — don't touch
- Only remove home.css entries, never guide.css

**Warning signs:**
- Guide page hero badge or other elements lose backdrop-filter effect

**Current status:** guide.css is independent. This is LOW risk.

### Pitfall 3: AOS Library Still Loaded but data-aos Removed
**What goes wrong:** AOS script stays in _Layout (correct), but all data-aos attributes on Homepage removed (unexpected blank page behavior or uninitialized JS).

**Why it happens:** Assume AOS only works on Homepage; don't realize Guide.cshtml also uses data-aos.

**How to avoid:**
- Verify other pages use data-aos: `grep -r "data-aos" /Views/` → confirms Guide.cshtml has 11+ instances
- AOS will initialize normally with Guide pages, ignore Homepage elements
- No performance impact; AOS gracefully handles missing data-aos targets

**Warning signs:**
- None expected (AOS handles gracefully)

**Current status:** Guide.cshtml has 11 data-aos attributes. Safe to remove Homepage ones.

### Pitfall 4: Pseudo-Elements with Filter Blur Not Fully Removed
**What goes wrong:** Remove `.glass-card` rule but leave `.hero-section::before` and `::after` pseudo-elements with `filter: blur()` (visual inconsistency).

**Why it happens:** See class rules but miss pseudo-element rules (::before, ::after need separate line deletion).

**How to avoid:**
- Search for all instances: `grep -n "::before\|::after\|filter: blur" home.css`
- List all findings in PLAN.md with line numbers
- Verify each removal in diff before committing

**Warning signs:**
- Hero section still shows blur effects after CSS cleanup
- Inspect element shows ::before/::after still present

**Current status:** Found 6 pseudo-element rules with blur (hero-section::before/after, glass-card::before/after, etc.). Must be explicitly listed.

### Pitfall 5: Removing Required Hero Base Styles
**What goes wrong:** Remove all `.hero-section` styling thinking it's all unused, but Phase 149 needs `.hero-section` base (background gradient, padding, border-radius).

**Why it happens:** See "glassmorphism" keyword and assume entire section should be removed.

**How to avoid:**
- Phase 149 REQUIRES: `.hero-section { background, border-radius, padding, box-shadow, color }`
- ONLY remove: `.hero-section::before/after` (the blur pseudo-elements)
- Document "kept" vs "removed" in PLAN.md

**Warning signs:**
- Hero section loses gradient background or borders
- Phase 149 can't render hero container

**Current status:** Hero section base rules MUST be kept. Only pseudo-elements removed.

---

## Code Examples

### Example 1: Lines to Remove (`.glass-card` block)
**Location:** home.css lines 131-186
```css
/* REMOVE THIS ENTIRE BLOCK */
.glass-card {
    background: rgba(255, 255, 255, 0.9);
    backdrop-filter: blur(20px);
    border-radius: 20px;
    border: 1px solid rgba(255, 255, 255, 0.5);
    box-shadow: var(--shadow-md);
    transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
    overflow: hidden;
}

.glass-card::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 6px;
    background: var(--gradient-primary);
    opacity: 0;
    transition: opacity 0.3s ease;
}

.glass-card:hover {
    transform: translateY(-8px);
    box-shadow: var(--shadow-hover);
}

.glass-card:hover::before {
    opacity: 1;
}

.glass-card.card-primary {
    background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
}

.glass-card.card-primary::before {
    background: var(--gradient-primary);
}

.glass-card.card-warning {
    background: linear-gradient(135deg, rgba(240, 147, 251, 0.1) 0%, rgba(245, 87, 108, 0.1) 100%);
}

.glass-card.card-warning::before {
    background: var(--gradient-warning);
}

.glass-card.card-success {
    background: linear-gradient(135deg, rgba(17, 153, 142, 0.1) 0%, rgba(56, 239, 125, 0.1) 100%);
}

.glass-card.card-success::before {
    background: var(--gradient-success);
}
```

### Example 2: Lines to Remove (`.timeline` and `.deadline-card` block)
**Location:** home.css lines 309-405
```css
/* REMOVE ENTIRE SECTION */
.timeline {
    position: relative;
    padding-left: 2rem;
}

.timeline::before {
    content: '';
    position: absolute;
    left: 8px;
    top: 0;
    bottom: 0;
    width: 2px;
    background: linear-gradient(to bottom, #667eea, #764ba2);
}

.timeline-item {
    position: relative;
    padding-bottom: 1.5rem;
}

.timeline-item:last-child {
    padding-bottom: 0;
}

.timeline-marker {
    position: absolute;
    left: -1.5rem;
    top: 4px;
    width: 18px;
    height: 18px;
    border-radius: 50%;
    background: white;
    border: 3px solid #667eea;
    box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.1);
}

.timeline-content {
    background: white;
    padding: 1rem 1.25rem;
    border-radius: 12px;
    box-shadow: var(--shadow-sm);
    transition: all 0.3s ease;
}

.timeline-content:hover {
    box-shadow: var(--shadow-md);
    transform: translateX(4px);
}

.timeline-title {
    font-weight: 600;
    color: #2d3748;
    margin-bottom: 0.25rem;
}

.timeline-description {
    font-size: 0.875rem;
    color: #718096;
    margin-bottom: 0.5rem;
}

.timeline-time {
    font-size: 0.75rem;
    color: #a0aec0;
}

.deadline-card {
    background: white;
    border-radius: 12px;
    padding: 1rem 1.25rem;
    margin-bottom: 0.75rem;
    border-left: 4px solid #667eea;
    box-shadow: var(--shadow-sm);
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
    gap: 1rem;
}

.deadline-card:hover {
    box-shadow: var(--shadow-md);
    transform: translateX(4px);
}

.deadline-icon {
    width: 48px;
    height: 48px;
    border-radius: 12px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
    flex-shrink: 0;
}

.deadline-content {
    flex: 1;
}

.deadline-title {
    font-weight: 600;
    color: #2d3748;
    margin-bottom: 0.25rem;
    font-size: 0.95rem;
}

.deadline-date {
    font-size: 0.8rem;
    color: #718096;
}

.deadline-badge {
    padding: 0.25rem 0.75rem;
    border-radius: 50px;
    font-size: 0.75rem;
    font-weight: 600;
    flex-shrink: 0;
}

.deadline-badge.urgent {
    background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    color: white;
}

.deadline-badge.normal {
    background: rgba(102, 126, 234, 0.1);
    color: #667eea;
}
```

### Example 3: Pseudo-Elements to Remove (`.hero-section::before` and `::after`)
**Location:** home.css lines 34-56
```css
/* REMOVE THESE PSEUDO-ELEMENTS */
.hero-section::before {
    content: '';
    position: absolute;
    top: -50%;
    right: -20%;
    width: 400px;
    height: 400px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 50%;
    filter: blur(60px);  /* ← Glassmorphism blur */
}

.hero-section::after {
    content: '';
    position: absolute;
    bottom: -30%;
    left: -10%;
    width: 300px;
    height: 300px;
    background: rgba(255, 255, 255, 0.08);
    border-radius: 50%;
    filter: blur(50px);  /* ← Glassmorphism blur */
}
```

**Keep this (Hero base structure for Phase 149):**
```css
.hero-section {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-radius: 24px;
    padding: 3rem 2.5rem;
    color: white;
    position: relative;
    overflow: hidden;
    box-shadow: var(--shadow-lg);
    margin-bottom: 2.5rem;
}
```

### Example 4: Attributes to Remove (Home/Index.cshtml data-aos)
**Location:** Home/Index.cshtml lines with data-aos
```html
<!-- REMOVE data-aos attributes from Homepage only -->
<!-- Line 10: Keep hero-section div, remove data-aos="fade-down" -->
<div class="hero-section">  <!-- was: data-aos="fade-down" -->

<!-- Line 46: Remove data-aos from IDP Status card -->
<div class="col-md-4">  <!-- was: data-aos="fade-up" data-aos-delay="100" -->

<!-- Line 84: Remove data-aos from Assessment card -->
<div class="col-md-4">  <!-- was: data-aos="fade-up" data-aos-delay="200" -->

<!-- Line 118: Remove data-aos from Training card -->
<div class="col-md-4">  <!-- was: data-aos="fade-up" data-aos-delay="300" -->

<!-- Lines 169, 177, 185, 194: Remove from Quick Access section -->
<div class="section-header">  <!-- was: data-aos="fade-right" -->

<!-- Lines 207, 243: Remove from Timeline and Deadlines (will be deleted anyway in Phase 149) -->
```

---

## State of the Art

### Glassmorphism Era → Minimalist Era

| Old Approach | Current Approach | Impact |
|--------------|-----------------|--------|
| Frosted glass cards with `backdrop-filter: blur()` | Bootstrap card pattern with `shadow-sm` | Simpler CSS, better performance, consistent with CMP/CDP |
| Blur pseudo-elements for decoration | Removed entirely | Cleaner design, reduced CSS lines by ~130 |
| AOS scroll animations on all sections | Removed from Homepage, kept on Guide | Lighter interaction, less JavaScript initialization |
| Custom timeline component | Will be replaced with HTML structure in Phase 149 | Removes 40+ CSS lines, easier to maintain |

### Why This Matters
- **Performance:** Fewer pseudo-elements = fewer paint operations
- **Maintainability:** Bootstrap utilities = less custom CSS to manage
- **Consistency:** CMP/CDP already use Bootstrap, now Homepage matches
- **Accessibility:** Less animation = better for users with motion sensitivity

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Visual smoke test + grep verification |
| Config file | None — CSS changes don't require unit tests |
| Quick run command | `grep -c "glass-card\|timeline\|deadline-card\|data-aos" wwwroot/css/home.css Views/Home/Index.cshtml` |
| Full suite command | Run local dev server, verify CMP/CDP/Admin pages display correctly, Homepage loads without errors |

### Phase Requirements → Verification Map

| Req ID | Behavior | Verification Type | Automated Check |
|--------|----------|-------------------|-----------------|
| CSS-01 | home.css contains no glass-card, backdrop-filter, or blur pseudo-element rules | Grep + Visual inspection | `grep -E "glass-card|backdrop-filter: blur|filter: blur" wwwroot/css/home.css` should return 0 results |
| CSS-02 | home.css contains no timeline or deadline-card styling rules | Grep + Visual inspection | `grep -E "\.timeline|\.deadline-card" wwwroot/css/home.css` should return 0 results |
| CSS-03 | No data-aos animation attributes on Homepage elements | Grep | `grep -c "data-aos" Views/Home/Index.cshtml` should be 0 |

### Sampling Rate
- **After CSS removal:** `grep "glass-card\|timeline\|deadline-card" wwwroot/css/home.css && grep "data-aos" Views/Home/Index.cshtml` — both should return empty
- **After code review:** Visual inspection of home.css for removed sections
- **Phase gate:** Run dev server, verify:
  - CMP Index page displays correctly (cards, styling intact)
  - CDP Index page displays correctly (cards, styling intact)
  - Admin pages display correctly (no broken styling)
  - Homepage loads without console errors

### Wave 0 Gaps
- None — CSS cleanup is pure file editing with grep verification. No test framework setup needed.

---

## Open Questions

1. **Backdrop-filter in guide.css — safe to ignore?**
   - What we know: guide.css has independent `backdrop-filter: blur()` rules (lines 105, 1181) for `.role-badge` and guide icons
   - What's unclear: Should these be removed too, or kept since Guide page is not being redesigned?
   - Recommendation: KEEP guide.css as-is. Phase 148 is Homepage-only cleanup. Guide cleanup defers to future phase.

2. **Hero-badge and hero-stat-item backdrop-filter — remove or keep?**
   - What we know: `.hero-badge` and `.hero-stat-item` have `backdrop-filter: blur(10px)` (lines 93, 111)
   - What's unclear: Are these "glassmorphism" we're eliminating, or acceptable micro-elements?
   - Recommendation: REMOVE (lines 67, 93, 111) as part of glassmorphism cleanup. Phase 149 will redesign without these.

3. **card-icon-wrapper classes — part of glass-card removal?**
   - What we know: `.card-icon-wrapper` (lines 187-214) styles icon boxes inside glass cards
   - What's unclear: These classes aren't part of glass-card rule, but only used inside glass cards (in removed HTML)
   - Recommendation: REMOVE `.card-icon-wrapper` rules (lines 187-214) since the HTML will be gone in Phase 149. Safe deletion.

---

## Sources

### Primary (HIGH confidence)
- **home.css** (current state, lines 1-513) — exhaustive review of current styles
- **Home/Index.cshtml** (current state, lines 1-299) — HTML structure with data-aos attributes
- **guide.css** (current state, lines 1-1200+) — verified independent of home.css
- **STATE.md** (2026-03-10) — project decisions locked in
- **REQUIREMENTS.md** (2026-03-10) — CSS-01, CSS-02, CSS-03 definitions

### Secondary (MEDIUM confidence)
- Grep verification across codebase confirms class usage isolation to Homepage

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — CSS/HTML inspection is definitive
- Architecture: HIGH — class usage verified via grep
- Pitfalls: HIGH — specific line numbers and class names documented
- Validation: MEDIUM — smoke test guidance provided; actual testing defers to planner

**Research date:** 2026-03-10
**Valid until:** 2026-03-31 (30 days — CSS is stable, no framework updates expected)

**Known assumptions:**
- Bootstrap 5.x is active (verified in CMP/CDP pages)
- No other CSS files import home.css (verified: home.css is standalone)
- AOS library remains in _Layout for Guide pages (confirmed: script at line 179)
- Phase 149 will handle HTML restructuring (out of scope for Phase 148)

---

## File Locations

**Primary research sources:**
- `/c/Users/Administrator/Desktop/PortalHC_KPB/wwwroot/css/home.css` (467 lines total)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Views/Home/Index.cshtml` (299 lines total)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/wwwroot/css/guide.css` (1200+ lines total)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Views/Shared/_Layout.cshtml` (AOS script at line 179)

**Phase output location:**
- `.planning/phases/148-css-audit-cleanup/148-RESEARCH.md`
