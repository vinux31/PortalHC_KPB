# Roadmap: v5.0 Guide Page Overhaul

**Created:** 2026-03-16
**Milestone:** v5.0
**Phases:** 4 (171-174)
**Requirements:** 12

## Phase Overview

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 171 | Guide Content Cleanup | Remove/simplify redundant accordion guides and fix card counts | GUIDE-01, GUIDE-02, GUIDE-03, GUIDE-04 | 4 |
| 172 | FAQ Reorder & Expand/Collapse | Reorganize FAQ with expand/collapse all and logical reordering | FAQ-01, FAQ-02, FAQ-03 | 3 |
| 173 | UI Consistency | Unify badge, button, and step styling across Guide system | UI-01, UI-02, UI-03 | 3 |
| 174 | Navigation Improvements | Add back-to-top button and improve breadcrumbs | NAV-01, NAV-02 | 2 |

## Phase Details

### Phase 171: Guide Content Cleanup

**Goal:** Remove redundant accordion guides already covered by PDF tutorials and fix guide counts.

**Requirements:** GUIDE-01, GUIDE-02, GUIDE-03, GUIDE-04

**Success Criteria:**
1. CMP GuideDetail: accordion items covered by PDF Assessment tutorial are simplified (kept as short summaries pointing to PDF)
2. CDP GuideDetail: accordion items covered by PDF Coaching Proton tutorial are simplified similarly
3. Guide card counts dynamically match actual visible guides per user role (not hardcoded)
4. Tutorial PDF cards styled via guide.css classes, not inline styles

---

### Phase 172: FAQ Reorder & Expand/Collapse

**Goal:** Improve FAQ usability with expand/collapse all button and logical reordering.

**Requirements:** FAQ-01, FAQ-02, FAQ-03

**Success Criteria:**
1. "Expand All" / "Collapse All" toggle button visible above FAQ section
2. FAQ items reordered: most common questions (login, assessment, coaching) appear first
3. FAQ categories cleaned up — items that purely duplicate GuideDetail content are removed or consolidated

---

### Phase 173: UI Consistency

**Goal:** Standardize visual styling across the Guide system.

**Requirements:** UI-01, UI-02, UI-03

**Success Criteria:**
1. Role badges (Admin/HC, Coach/Atasan) use same CSS class everywhere (no inline bg-success overrides)
2. FAQ collapse buttons and GuideDetail accordion buttons share unified base styling
3. CMP step badges use blue variant matching CMP module icon (not default gradient)

---

### Phase 174: Navigation Improvements

**Goal:** Add back-to-top and improve breadcrumb navigation.

**Requirements:** NAV-01, NAV-02

**Success Criteria:**
1. Floating back-to-top button appears after scrolling 300px, smooth-scrolls to top on click
2. GuideDetail breadcrumb shows: Home > Panduan > [Module Name] with clickable links

---
*Roadmap created: 2026-03-16*
*Last updated: 2026-03-16*
