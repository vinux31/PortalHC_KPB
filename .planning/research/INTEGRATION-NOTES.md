# Integration Notes: Homepage Minimalist Redesign

**For Phase Planning:** Reference this during Phase 1, 2, and 3 planning

## Key Files to Update

### Phase 1: CSS Refactor
**File:** `wwwroot/css/home.css`

**Lines to REMOVE:**
- Lines 1–14: `:root` gradient variable definitions
- Lines 23–56: `.hero-section`, `.hero-section::before`, `.hero-section::after`, `.hero-content`, `.hero-avatar`, `.hero-greeting`, `.hero-subtitle`, `.hero-badge`, `.hero-stats`, `.hero-stat-item`, `.hero-stat-value`, `.hero-stat-label`
- Lines 131–215: All `.glass-card*` and `.card-icon-wrapper*` variants
- Lines 219–256: `.circular-progress*` classes
- Lines 309–373: `.timeline*` classes (timeline section is removed)
- Lines 378–442: `.deadline-card*` classes (deadline section is removed)
- Lines 474–486: `.gradient-text` and `.badge-gradient` utility classes

**Lines to KEEP:**
- Lines 261–304: `.quick-access-card*` (adapt to new simpler pattern)
- Lines 446–469: `.section-header*` and `.section-icon*` (adapt)
- Lines 490–512: `@media` responsive queries (simplify)

**NEW CSS to add:**
```css
.icon-box {
    width: 60px;
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.card {
    transition: transform 0.2s, box-shadow 0.2s;
}

.card:hover {
    transform: translateY(-5px);
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
}
```

### Phase 2: HTML Markup Update
**File:** `Views/Home/Index.cshtml`

**Lines to MODIFY:**

| Line Range | Current | Change To |
|------------|---------|-----------|
| 10 | `<div class="hero-section" data-aos="fade-down">` | `<div class="hero-section bg-primary text-white py-5">` |
| 46–80 | `<div class="col-md-4" data-aos="fade-up" data-aos-delay="100">` | `<div class="col-md-4">` (remove data-aos) |
| 46–80 | `<div class="glass-card card-primary h-100">` | `<div class="card border-0 shadow-sm h-100">` |
| 46–79 | Entire circular progress SVG block | `<div class="text-center mb-3"><h2 class="display-4">@Model.IdpProgressPercentage%</h2></div>` |
| 46–79 | `<div class="card-icon-wrapper icon-primary">` | `<div class="bg-primary bg-opacity-10 text-primary rounded-3 p-3">` |
| 85–114 | Same glass-card refactor for Pending Assessment | Use `.card.border-0.shadow-sm` pattern |
| 119–164 | Same glass-card refactor for Mandatory Training | Use `.card.border-0.shadow-sm` pattern |
| 177–201 | Quick Access cards refactor | Match CMP/CDP card pattern exactly |
| 205–298 | **REMOVE ENTIRE SECTION** — Recent Activity & Upcoming Deadlines | Delete lines 205–298 entirely |

### Phase 3: Visual Testing
No files to modify, but verify:
- Responsive breakpoints match CMP/CDP (768px, 992px)
- Icon sizing consistent across all cards (60px boxes)
- Shadow depth matches: `shadow-sm` everywhere, no custom shadows
- Hover effects: translateY(-5px) only, no scale or color changes

## Validation Checkpoints

| Checkpoint | How to Verify | Pass Criteria |
|------------|---------------|---------------|
| home.css shrinkage | `wc -l home.css` before/after | ~200-250 lines remain (was 512) |
| No breaking deps | Check `_Layout.cshtml` | AOS still loads, but no `data-aos` attrs on homepage |
| Bootstrap consistency | Compare Home/Index with CMP/Index | Same card pattern, icon sizing, spacing |
| Responsive grid | Test on mobile (375px), tablet (768px), desktop (1920px) | Cards stack 1 column mobile, 2–3 cols desktop |
| Accessibility | Run axe DevTools or WAVE | No contrast issues, semantic HTML preserved |

## Conflict Prevention

**Watch out for:**
1. **Legacy styles bleeding through** — If old `.glass-card` CSS remains in home.css, it will conflict with new `.card` styles. Use `git diff` to verify complete removal.
2. **AOS library still running** — Removing `data-aos` attributes is sufficient; AOS won't break if it can't find elements, but test that page loads without JS errors.
3. **Custom font sizing** — Hero section currently has `font-size: 2.5rem`. Simplify to standard Bootstrap heading classes (`h2`, `h3`).
4. **Mobile viewport** — Quick Access cards are `col-md-4 col-6`. Verify they don't wrap awkwardly on small screens.

## Performance Impact

- **Positive:** Removing `backdrop-filter: blur` eliminates GPU-heavy rendering; page paint time should improve ~5–10%
- **Positive:** Fewer CSS animations (AOS removed) = faster initial page load
- **Neutral:** Bootstrap CDN remains unchanged; no bundle size impact
- **Neutral:** JavaScript execution minimal (remove AOS event listeners on homepage only)

## Rollback Plan

If Phase 1 CSS breaks something:
1. Revert `home.css` from git
2. Home/Index.cshtml will still reference old classes, no problem
3. Page will render with old styles
4. Restart phase with clearer scope

If Phase 2 HTML update breaks alignment:
1. Revert `Home/Index.cshtml` from git
2. Old markup will work with old CSS (if Phase 1 wasn't fully deployed)
3. Or temporarily comment out Phase 2 changes per card

## References

- **CMP/CDP pattern examples:**
  - `Views/CMP/Index.cshtml` lines 15–35: Icon box + card pattern
  - `Views/CDP/Index.cshtml` lines 15–35: Same pattern
  - Inline CSS in both files (lines 102–119) provides `.icon-box` and `.card` hover styles

- **Bootstrap 5.3 utilities:**
  - `border-0` removes card borders
  - `shadow-sm` applies light shadow (alternative: `shadow` for heavier shadow)
  - `bg-opacity-10` makes background color 10% opaque
  - `rounded-3` = `border-radius: 0.375rem` (Bootstrap's spacing scale)

---

**Integration note date:** 2026-03-10
**Update if:** Bootstrap version changes, new AOS features required, or design guidance shifts
