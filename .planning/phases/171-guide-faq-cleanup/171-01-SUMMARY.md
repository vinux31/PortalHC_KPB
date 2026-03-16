---
phase: 171-guide-faq-cleanup
plan: "01"
subsystem: guide-ui
tags: [guide, css, accordion, tutorial-cards, role-gating]
dependency_graph:
  requires: []
  provides: [simplified-guide-accordions, tutorial-card-css-variants, ad-tutorial-card]
  affects: [Views/Home/GuideDetail.cshtml, wwwroot/css/guide.css]
tech_stack:
  added: []
  patterns: [CSS variant classes replacing inline styles, role-gated content]
key_files:
  created:
    - wwwroot/documents/guides/ActiveDirectory-Guide.html
  modified:
    - Views/Home/GuideDetail.cshtml
    - wwwroot/css/guide.css
decisions:
  - CMP accordion reduced to 4 items (5 for Admin/HC) by removing assessment/results/certificate items covered by PDF tutorial
  - CDP accordion reduced to 2-3 items by removing coaching/deliverable/upload items covered by PDF tutorial
  - CDP 5 (Approve/Reject) gated to Admin/HC only
  - Tutorial card CSS classes use variant modifier pattern (guide-tutorial-card--cmp/cdp/admin)
  - AD guide file served from wwwroot/documents/guides/ (original in docs/ retained)
metrics:
  duration: "~15 min"
  completed: "2026-03-16"
  tasks_completed: 2
  tasks_total: 2
  files_modified: 3
  files_created: 1
---

# Phase 171 Plan 01: GuideDetail Accordion Simplification and Tutorial Card CSS Refactor Summary

CSS variant classes for tutorial cards (CMP purple, CDP green, Admin pink) replacing inline styles, with CMP/CDP accordions stripped of PDF-covered items and AD guide integrated for admin module.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Add tutorial card CSS classes and move AD guide file | 3af3fa2 | wwwroot/css/guide.css, wwwroot/documents/guides/ActiveDirectory-Guide.html |
| 2 | Simplify GuideDetail accordions, fix tutorial cards, add AD card, role-gate CDP 5 | 027ec2f | Views/Home/GuideDetail.cshtml |

## What Was Built

### CSS Variant Classes (guide.css)

Added three tutorial card variant modifier classes at the end of guide.css:

- `.guide-tutorial-card--cmp`: purple theme (`#5c35cc`), lavender gradient background
- `.guide-tutorial-card--cdp`: green theme (`#2e7d32`), mint gradient background
- `.guide-tutorial-card--admin`: pink theme (`#c2185b`), rose gradient background

Each variant targets `.guide-tutorial-inner` (background/border/radius), `.guide-tutorial-icon` (48x48 colored box), and `.guide-tutorial-title` (heading color). Shared base classes `.guide-tutorial-inner` (padding) and `.guide-tutorial-icon i` (white icon color) are also added.

### GuideDetail.cshtml Changes

**CMP module:**
- Tutorial card converted from inline styles to `guide-tutorial-card--cmp` CSS classes
- CMP 3 (Mengerjakan Assessment), CMP 4 (Melihat Hasil Assessment), CMP 5 (Download Sertifikat) removed — all covered by Panduan-Lengkap-Assessment.html PDF tutorial
- Remaining: CMP 1 (Library KKJ), CMP 2 (Mapping KKJ-CPDP), CMP 3→Training Records, CMP 4→Monitoring Records Tim (Admin/HC)

**CDP module:**
- Tutorial card converted from inline styles to `guide-tutorial-card--cdp` CSS classes
- CDP 2 (Coaching Progress), CDP 3 (Membuat Deliverable), CDP 4 (Upload Evidence) removed — covered by Panduan-Lengkap-Coaching-Proton.html PDF tutorial
- CDP 5 (Approve/Reject Deliverable) wrapped in `@if (userRole == "Admin" || userRole == "HC")` role gate
- Remaining: CDP 1 (Plan IDP/Silabus), CDP 5 (Admin/HC only), CDP 6 Dashboard (Admin/HC only), CDP 7 (Daftar Deliverable)

**Admin module:**
- New `@else if (module == "admin")` tutorial card block added
- Role-gated to Admin/HC only (consistent with module access)
- Uses `guide-tutorial-card--admin` CSS variant with pink theme
- Links to `/documents/guides/ActiveDirectory-Guide.html`
- Buttons: Lihat (btn-outline-danger) and Download (btn-danger)

**AD guide file:**
- `docs/ActiveDirectory-Guide.html` copied to `wwwroot/documents/guides/ActiveDirectory-Guide.html`
- Original in `docs/` retained

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- `dotnet build` compiles without C# errors (only MSBuild copy lock due to running process — not a code error)
- CMP 3/4/5 accordion IDs absent from GuideDetail.cshtml: confirmed 0 matches
- Tutorial card CSS variant classes: 3 instances in GuideDetail.cshtml (cmp, cdp, admin)
- AD guide referenced in GuideDetail.cshtml: 2 instances (Lihat + Download links)
- 9 CSS rule blocks added to guide.css for the three variants

## Self-Check: PASSED

- `Views/Home/GuideDetail.cshtml` — modified, committed 027ec2f
- `wwwroot/css/guide.css` — modified, committed 3af3fa2
- `wwwroot/documents/guides/ActiveDirectory-Guide.html` — created, committed 3af3fa2
- Commits 3af3fa2 and 027ec2f confirmed in git log
