# Roadmap: v5.0 Guide Page Overhaul

**Created:** 2026-03-16
**Milestone:** v5.0
**Phases:** 2 (171-172)
**Requirements:** 12

## Phase Overview

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 171 | 2/2 | Complete    | 2026-03-16 | 7 |
| 172 | 2/2 | Complete    | 2026-03-16 | 5 |

## Phase Details

### Phase 171: Guide & FAQ Cleanup

**Goal:** Remove redundant accordion guides covered by PDF tutorials, fix card counts, and improve FAQ with reorder + expand/collapse all.

**Requirements:** GUIDE-01, GUIDE-02, GUIDE-03, GUIDE-04, FAQ-01, FAQ-02, FAQ-03

**Plans:** 2/2 plans complete

Plans:
- [x] 171-01-PLAN.md — Simplify GuideDetail accordions, fix tutorial card styles, add AD tutorial card
- [x] 171-02-PLAN.md — Dynamic card counts, FAQ reorder/cleanup, expand/collapse toggle

**Success Criteria:**
1. CMP GuideDetail accordion items covered by PDF tutorial simplified to short summaries pointing to PDF
2. CDP GuideDetail accordion items covered by PDF tutorial simplified similarly
3. Guide card counts dynamically reflect actual visible guides per user role
4. Tutorial PDF cards styled via guide.css classes, not inline styles
5. Expand All / Collapse All toggle button above FAQ section
6. FAQ items reordered logically (most common first: login, assessment, coaching)
7. FAQ categories cleaned up — items duplicating GuideDetail content removed or consolidated

---

### Phase 172: UI & Navigation Polish

**Goal:** Standardize visual styling and improve navigation across Guide system.

**Requirements:** UI-01, UI-02, UI-03, NAV-01, NAV-02

**Plans:** 2/2 plans complete

Plans:
- [ ] 172-01-PLAN.md — Unify role badges, step badge colors, accordion base styles
- [ ] 172-02-PLAN.md — Back-to-top button, GuideDetail breadcrumb

**Success Criteria:**
1. Role badges consistent across Guide.cshtml and GuideDetail.cshtml (no inline overrides)
2. FAQ collapse buttons and GuideDetail accordion buttons share unified base styling
3. CMP step badges use blue variant matching module icon
4. Floating back-to-top button appears after scrolling 300px
5. GuideDetail breadcrumb shows: Home > Panduan > [Module Name]

---
*Roadmap created: 2026-03-16*
*Last updated: 2026-03-16 — phase 172 planned (2 plans)*
