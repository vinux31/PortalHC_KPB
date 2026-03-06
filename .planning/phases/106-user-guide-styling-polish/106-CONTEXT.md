# Phase 106: User Guide Styling & Polish - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Visual polish, UX enhancements, and accessibility improvements for the existing User Guide. Structure and content are complete from Phase 105. This phase focuses on refinement: enhanced animations, mobile polish, accessibility improvements, and visual consistency optimizations. No new features or content changes.
</domain>

<decisions>
## Implementation Decisions

### Scope (from Phase 105)
- User Guide infrastructure already built: Guide.cshtml, GuideDetail.cshtml, guide.css (771 lines)
- Content complete: 33 guides across 4 modules (CMP, CDP, Account, Admin Panel)
- Basic styling in place: Bootstrap 5, AOS animations, glassmorphism cards, CSS variables
- Phase 106 = polish and refinement only

### Polish Focus Areas
Based on project needs and user requirements:

1. **Animation & Micro-interactions**
   - Enhance beyond basic AOS entrance animations
   - Add hover effects, transitions, and interaction feedback
   - Improve FAQ accordion animations
   - Polish search input interactions

2. **Mobile & Responsive Polish**
   - Enhance touch targets for mobile devices
   - Optimize layouts for small screens (phones, tablets)
   - Improve mobile navigation and spacing
   - Test responsive breakpoints

3. **Accessibility Enhancements**
   - Improve focus indicators for keyboard navigation
   - Enhance ARIA labels and roles
   - Optimize for screen readers
   - Ensure WCAG compliance where possible

4. **Visual Consistency & Performance**
   - Align styling with dashboard/home page patterns
   - Optimize CSS for performance
   - Ensure consistent spacing and typography

### Claude's Discretion
- Specific animation timing and easing functions
- Exact shadow/color values for hover states
- Mobile breakpoint values (use Bootstrap defaults or refine)
- Which ARIA attributes to add (follow WCAG 2.1 AA guidelines)
- Performance optimization techniques (lazy loading, code splitting, etc.)
- Print CSS enhancements (Phase 105 added basics, may refine)

</decisions>

<specifics>
## Specific Ideas

No specific user preferences provided beyond the 4 focus areas above. Use portal's existing design patterns as reference (dashboard cards, navigation, buttons).
</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- **guide.css** (wwwroot/css/guide.css): 771 lines of complete styling
  - CSS variables: --gradient-primary, --gradient-success, --gradient-warning, --gradient-info, --gradient-orange
  - Shadow variables: --shadow-sm, --shadow-md, --shadow-lg, --shadow-hover
  - Hero section with glassmorphism effect
  - Role badge styling
  - Search input styling
  - Module card styling (card-cmp, card-cdp, card-account, card-data, card-admin)
  - FAQ accordion styling
  - Responsive breakpoints already defined
  - Print CSS @media query (lines 539-560)

- **Guide.cshtml** (Views/Home/Guide.cshtml):
  - Hero section with AOS fade-down animation
  - Breadcrumb navigation
  - Search bar with icon
  - Module cards grid (5 cards total, 4 visible to non-admin users)
  - FAQ section with 32 questions
  - Client-side search JavaScript (lines 500-541)
  - AOS animations: fade-down, fade-up, fade-right with delays

- **GuideDetail.cshtml** (Views/Home/GuideDetail.cshtml):
  - Back button with breadcrumb
  - Module-specific hero section
  - Bootstrap accordion for step-by-step instructions
  - 33 total guides across modules
  - Step cards with numbered badges and icons
  - Search term highlighting (added in Phase 105)

- **home.css** (wwwroot/css/home.css):
  - Inter font family (line 17)
  - CSS variables that guide.css extends
  - Glassmorphism card patterns
  - Consistent spacing and shadow values

### Established Patterns
- **Bootstrap 5.3.0**: Framework for grid, cards, accordions, alerts
- **Bootstrap Icons**: `<i class="bi bi-icon-name"></i>` syntax
- **AOS Library**: `data-aos="fade-up"`, `data-aos-delay="100"` pattern
- **Role-based UI**: `@if (User.IsInRole("Admin") || User.IsInRole("HC"))` pattern
- **Gradient styling**: `background: var(--gradient-primary)` for badges/icons
- **Card hover**: `.card:hover { transform: translateY(-4px); box-shadow: var(--shadow-hover); }` pattern (from home.css)

### Integration Points
- **CSS**: Extend existing guide.css - no new files needed
- **JavaScript**: Enhance existing search script in Guide.cshtml (lines 500-541)
- **AOS**: Already loaded in _Layout.cshtml, just add/update data-aos attributes
- **Responsive**: Use Bootstrap breakpoints (sm, md, lg, xl) - already in place

### Current Styling Patterns to Extend
```css
/* Module cards (current basic styling) */
.guide-module-card {
    background: white;
    border-radius: 16px;
    box-shadow: var(--shadow-md);
    transition: all 0.3s ease;
}

/* FAQ accordions (current) */
.faq-question {
    background: white;
    border: none;
    padding: 1rem 1.25rem;
    /* Can add hover effects here */
}
```

</code_context>

<deferred>
## Deferred Ideas

None - Phase 106 focuses on polish only. All new features are out of scope.
</deferred>

---

*Phase: 106-user-guide-styling-polish*
*Context gathered: 2026-03-06*
*Approach: Focus on animation, mobile, accessibility, and visual polish refinements*
