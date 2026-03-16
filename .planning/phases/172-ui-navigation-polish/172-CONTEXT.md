# Phase 172: UI & Navigation Polish - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Standardize visual styling (role badges, accordion buttons, step badge colors) and improve navigation (back-to-top button, breadcrumb) across Guide.cshtml and GuideDetail.cshtml. No new features — polish and consistency only.

</domain>

<decisions>
## Implementation Decisions

### Role Badge Unification
- Create one unified `.guide-role-badge` class replacing both `.role-badge` (Guide hero) and `.guide-step-badge-role` (GuideDetail accordion headers)
- Same neutral color everywhere — no module-specific badge colors. Remove inline overrides (e.g. `bg-success text-white border-0` on CDP 5)
- Standardize badge text to "Admin / HC" everywhere (replace "Admin & HC", "Atasan / HC" variants)

### Accordion Button Unification
- FAQ `.faq-question` buttons and GuideDetail Bootstrap `.accordion-button` share a unified base style (font-size, padding, hover effect, border-radius)
- Module-specific color accents still allowed via existing variant classes

### Step Badge Colors
- Add `.step-variant-blue` for CMP steps to match CMP module icon color (blue)
- Remove unused `.step-variant-pink` CSS (dead code — Admin Panel uses orange)
- Final color map: CMP = blue, CDP = green, Account = teal, Kelola Data = orange, Admin Panel = orange

### Back-to-Top Button
- Guide pages only (Guide.cshtml and GuideDetail.cshtml) — not site-wide
- Fixed position bottom-right corner
- Circular icon button with chevron-up icon, subtle shadow, smooth fade-in/out
- Appears after scrolling 300px (per success criteria)

### GuideDetail Breadcrumb
- Add breadcrumb to GuideDetail: Home > Panduan > [Module Name]
- "Panduan" links back to /Home/Guide; module name is current page (not clickable)
- Use friendly display names: CMP → 'CMP', CDP → 'CDP', account → 'Akun', kelola-data → 'Kelola Data', admin → 'Admin Panel'
- Reuse Guide.cshtml's responsive breadcrumb CSS (icon-only on mobile)

### Claude's Discretion
- Exact shadow/border-radius values for back-to-top button
- Scroll animation speed (smooth scroll to top)
- Exact unified accordion base style details (font-weight, transition timing)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches that match the existing guide.css aesthetic.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `.guide-breadcrumb` CSS class (guide.css:29-40) with responsive behavior already defined — can be reused for GuideDetail
- `.role-badge` (guide.css:100) and `.guide-step-badge-role` (guide.css:465) — both to be replaced by unified class
- Step variant classes (guide.css:399-442) — pattern to follow for new `.step-variant-blue`
- `.guide-tutorial-card--*` variant modifier pattern from Phase 171 — consistent naming convention

### Established Patterns
- Color variants use BEM-like modifier: `.guide-step-item.step-variant-{color} .guide-step-badge`
- Responsive breakpoints in guide.css handle mobile breadcrumb (icon-only at small screens)
- Bootstrap Collapse API used for both FAQ and GuideDetail accordions

### Integration Points
- GuideDetail.cshtml renders via `?module=` query parameter — breadcrumb needs to read this
- Guide.cshtml hero section contains `.role-badge` — replace with unified class
- GuideDetail accordion headers contain `.guide-step-badge-role` — replace with unified class

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 172-ui-navigation-polish*
*Context gathered: 2026-03-16*
