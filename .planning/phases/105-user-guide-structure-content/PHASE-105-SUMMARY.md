# Phase 105: User Guide Structure & Content - Planning Summary

**Phase:** 105 - User Guide Structure & Content
**Status:** Ready for Execution
**Date:** 2026-03-06
**Plans Created:** 5

## Overview

Phase 105 completes the User Guide feature by fixing bugs, adding missing content, and polishing the user experience. The core infrastructure already exists (HomeController, views, CSS, navbar integration), but content gaps and CSS issues need resolution.

## Current State (Already Built)

### ✅ Completed Features
- HomeController.Guide() and HomeController.GuideDetail(string module) methods
- Views/Home/Guide.cshtml - Hero section, search bar, 5 module cards, FAQ with 32 questions
- Views/Home/GuideDetail.cshtml - Detail page with accordion guides for each module
- wwwroot/css/guide.css - Complete styling with gradients, animations, responsive design
- _Layout.cshtml - "Panduan" navbar link with icon (line 66)
- Role-based access control working (Admin/HC see Admin and Data modules)
- AOS animations integrated
- Client-side search functionality working

### ⚠️ Issues Identified

**Content Gaps:**
1. **Account module:** Card says "4 panduan" but only 3 guides exist
   - Missing: Logout & Role Information guide

2. **CDP module:** Card says "7 panduan" but only 6 guides exist
   - Missing: Deliverable List Overview guide

3. **Admin module:** Card says "12 panduan" but only 5 guides exist
   - Missing 7 guides: Manage Questions, Manage Assessments, Assessment Monitoring, Manage Units, Manage Positions, Notifications, Audit Log detail
   - Current guides combine multiple topics (e.g., "Kelola Ujian" covers 3 features)

**CSS Bugs:**
1. Classes `btn-cdp`, `btn-account`, `btn-data`, `btn-admin` used in HTML but undefined in guide.css
2. Accordion buttons have inconsistent class usage
3. No visual impact currently, but creates undefined class references

**UX Improvements Needed:**
1. Search doesn't highlight matching terms
2. No breadcrumb navigation on home Guide page
3. Print CSS could be enhanced

## Plans Breakdown

### Wave 1 (Can Run in Parallel)

**PLAN-01: Fix CSS Bugs and Class Inconsistencies**
- Add definitions for missing button classes (`btn-cdp`, `btn-account`, `btn-data`, `btn-admin`)
- Align classes with existing gradient system
- Files: `wwwroot/css/guide.css`
- Requirements: GUIDE-STYLE-01, GUIDE-STYLE-03, GUIDE-STYLE-06
- Estimated time: 15 minutes

**PLAN-02: Add Missing Account Module Guide Content**
- Add 4th Account guide: "Cara Logout & Memahami Role System"
- Update card count verification
- Files: `Views/Home/GuideDetail.cshtml`, `Views/Home/Guide.cshtml`
- Requirements: GUIDE-CONTENT-01, GUIDE-CONTENT-02, GUIDE-ACCESS-06
- Estimated time: 20 minutes

**PLAN-03: Add Missing CDP Module Guide Content**
- Add 7th CDP guide: "Cara Melihat Daftar Deliverable & Status Progress"
- Verify card count accuracy
- Files: `Views/Home/GuideDetail.cshtml`, `Views/Home/Guide.cshtml`
- Requirements: GUIDE-CONTENT-01, GUIDE-CONTENT-02, GUIDE-ACCESS-05
- Estimated time: 20 minutes

### Wave 2 (Depends on Wave 1 Content Structure)

**PLAN-04: Add Missing Admin Module Guide Content**
- Split combined guides into focused, feature-specific guides
- Add 7 new Admin guides (Bank Soal, Create Assessment, Monitoring, Units, Positions, Notifications, Audit Log)
- Reorganize existing content for better navigation
- Files: `Views/Home/GuideDetail.cshtml`, `Views/Home/Guide.cshtml`
- Requirements: GUIDE-CONTENT-01, GUIDE-CONTENT-02, GUIDE-ACCESS-07
- Estimated time: 60-90 minutes (most content-intensive plan)

**PLAN-05: Add Minor UX Features**
- Implement search term highlighting
- Add breadcrumb navigation to home Guide page
- Enhance print CSS for better PDF export
- Files: `Views/Home/Guide.cshtml`, `wwwroot/css/guide.css`
- Requirements: GUIDE-CONTENT-03, GUIDE-STYLE-07, GUIDE-NAV-01
- Estimated time: 45 minutes

## Requirements Coverage

### Phase 105 Requirements (All Covered)

**Navigation (GUIDE-NAV-01 to GUIDE-NAV-05):**
- ✅ GUIDE-NAV-01: Navbar "Panduan" link exists (line 66 in _Layout.cshtml)
- ✅ GUIDE-NAV-02: Hero section with gradient styling (exists in Guide.cshtml)
- ✅ GUIDE-NAV-03: 5 tab/module cards (CMP, CDP, Account, Data, Admin)
- ✅ GUIDE-NAV-04: Tab switching works (via GuideDetail navigation)
- ✅ GUIDE-NAV-05: Admin Panel tab hidden for non-Admin/HC (role check exists)

**Content (GUIDE-CONTENT-01 to GUIDE-CONTENT-06):**
- ✅ GUIDE-CONTENT-01: Step-by-step instructions with numbered cards (exists, being completed)
- ✅ GUIDE-CONTENT-02: Icons, titles, descriptions in steps (exists, being enhanced)
- ✅ GUIDE-CONTENT-03: Alert boxes for tips (exists in guide.css)
- ✅ GUIDE-CONTENT-04: Accordion/collapse for sub-sections (Bootstrap accordions)
- ✅ GUIDE-CONTENT-05: FAQ section with 32 questions (complete)
- ✅ GUIDE-CONTENT-06: FAQ covers login, password, CMP vs CDP, assessments, evidence, approval, coaching (complete)

**Access Control (GUIDE-ACCESS-01 to GUIDE-ACCESS-07):**
- ✅ GUIDE-ACCESS-01: Authentication required (HomeController has [Authorize])
- ✅ GUIDE-ACCESS-02: Role badge displays user's role (in hero section)
- ✅ GUIDE-ACCESS-03: Dashboard content available (N/A - no Dashboard guide per user decision)
- ✅ GUIDE-ACCESS-04: CMP tab content available (7 guides)
- ✅ GUIDE-ACCESS-05: CDP tab content available (7 guides after PLAN-03)
- ✅ GUIDE-ACCESS-06: Account tab content available (4 guides after PLAN-02)
- ✅ GUIDE-ACCESS-07: Admin Panel tab visible only to Admin/HC (role check exists, 12 guides after PLAN-04)

## Execution Strategy

### Recommended Sequence
1. **Wave 1 (Parallel):** Execute PLAN-01, PLAN-02, PLAN-03 simultaneously
   - Fix CSS bugs first (PLAN-01)
   - Add missing Account and CDP guides (PLAN-02, PLAN-03)
   - These are independent and low-risk

2. **Wave 2 (Sequential):** Execute PLAN-04, then PLAN-05
   - Add all Admin guides (PLAN-04) - most work, depends on Wave 1 patterns
   - Polish with UX features (PLAN-05) - depends on complete content

### Testing Strategy
- After each plan: Verify in browser (Chrome/Edge/Firefox)
- Test with different user roles (Admin, HC, regular user)
- Verify search functionality works with new content
- Print to PDF test for PLAN-05
- Mobile responsive test on all pages

### Risk Mitigation
- **PLAN-04 Risk:** Most content-heavy plan
  - Mitigation: Use existing guide structure as template
  - Break into smaller commits if needed

- **Search Highlight Risk:** Breaking HTML with regex replacement
  - Mitigation: Use TreeWalker API (provided in plan)
  - Test with various search terms

- **Print CSS Risk:** Browser compatibility
  - Mitigation: Test in Chrome, Edge, Firefox print preview
  - Use standard CSS @media print properties

## Success Criteria

### Phase Complete When:
- [ ] All CSS classes defined and rendering correctly
- [ ] Account module: 4/4 guides complete
- [ ] CDP module: 7/7 guides complete
- [ ] Admin module: 12/12 guides complete
- [ ] Card counts match actual guide counts in all modules
- [ ] Search highlighting works for FAQ and cards
- [ ] Breadcrumb navigation functional on both pages
- [ ] Print-to-PDF produces clean, readable documentation
- [ ] Zero browser console errors (CSS or JS)
- [ ] Role-based access control verified (Admin/HC vs regular users)
- [ ] All 18 Phase 105 requirements satisfied

### Final Deliverables
- Updated `Views/Home/Guide.cshtml` with breadcrumb
- Updated `Views/Home/GuideDetail.cshtml` with 23 total guides (CMP: 7, CDP: 7, Account: 4, Data: 3, Admin: 12+)
- Updated `wwwroot/css/guide.css` with button classes, search highlighting, improved print styles
- Updated `Views/Home/Guide.cshtml` card counts (if needed)
- Complete User Guide feature ready for Phase 106 (Styling & Polish)

## Next Steps

1. **Execute Phase 105:**
   ```bash
   /gsd:execute-phase 105
   ```

2. **After Phase 105 Complete:**
   - Phase 106: Styling & Polish (8 requirements - animations, responsive refinements)
   - Or skip to next milestone if styling is satisfactory

3. **Quality Gates:**
   - Browser testing: Chrome, Edge, Firefox
   - Role testing: Admin, HC, Coachee roles
   - Device testing: Desktop, tablet, mobile
   - Print testing: PDF export quality

---

**Planning Complete:** 2026-03-06
**Plans Ready for Execution:** Yes
**Autonomous Execution:** All plans are autonomous (minimal user intervention needed)
**Estimated Total Time:** 3-4 hours for all 5 plans
