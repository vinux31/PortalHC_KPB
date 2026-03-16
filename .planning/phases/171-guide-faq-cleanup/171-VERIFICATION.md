---
phase: 171-guide-faq-cleanup
verified: 2026-03-16T00:00:00Z
status: passed
score: 9/9 must-haves verified
re_verification: false
---

# Phase 171: Guide FAQ Cleanup Verification Report

**Phase Goal:** Simplify Guide detail pages, clean FAQ section, add expand/collapse, dynamic counts
**Verified:** 2026-03-16
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
|----|-------|--------|----------|
| 1  | CMP GuideDetail workers see 4 items (3 accordion + 1 tutorial) | VERIFIED | cmpCollapse1, 2, 6 visible to all; cmpCollapse7 Admin/HC only; tutorial card present; Guide.cshtml cmpCount = isAdminOrHc ? 5 : 4 |
| 2  | CDP GuideDetail has redundant items removed (covered by PDF Coaching Proton) | VERIFIED | cdpCollapse2 (Coaching Progress), 3 (Deliverable), 4 (Upload Evidence) absent; only cdpCollapse1, 5, 6, 7 remain |
| 3  | CDP 5 (Approve/Reject Deliverable) visible to Admin/HC only | VERIFIED | `@if (userRole == "Admin" || userRole == "HC")` wraps cdpCollapse5 at line 242 |
| 4  | Tutorial PDF cards use CSS classes instead of inline styles | VERIFIED | guide-tutorial-card--cmp (line 66), guide-tutorial-card--cdp (line 90), guide-tutorial-card--admin (line 116); no inline style attributes on tutorial card divs |
| 5  | AD tutorial card appears in admin module for Admin/HC only | VERIFIED | `@else if (module == "admin")` block with `@if (userRole == "Admin" || userRole == "HC")` gate at line 114; links to `/documents/guides/ActiveDirectory-Guide.html` |
| 6  | Guide card counts dynamically reflect actual guide count per role | VERIFIED | Razor vars: cmpCount (5/4), cdpCount (5/3), accountCount (4), dataCount (3), adminCount (13); all 5 cards use `@xxxCount panduan tersedia` |
| 7  | FAQ has expand all / collapse all toggle button | VERIFIED | `<button id="faqToggleAll" onclick="toggleAllFaq()">` at line 168; `toggleAllFaq()` function at line 472 using Bootstrap Collapse API |
| 8  | FAQ categories reordered: Akun & Login > Assessment > CDP & Coaching > Umum > KKJ & CPDP > Admin & Kelola Data | VERIFIED | Category titles appear at lines 175, 230, 265, 311, 366, 413 in exact specified order |
| 9  | FAQ items duplicating PDF tutorial step-by-step content removed | VERIFIED | Assessment and CDP step-by-step flow items removed; categories reduced; IDs renumbered sequentially (confirmed by commits 1c1f87f) |

**Score:** 9/9 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Views/Home/GuideDetail.cshtml` | Simplified accordions, role-gated content, CSS tutorial cards | VERIFIED | CMP 3/4/5 removed, CDP 2/3/4 removed, CDP 5 gated, all 3 tutorial cards use CSS variant classes |
| `wwwroot/css/guide.css` | CSS classes for tutorial card variants (CMP purple, CDP green, Admin pink) | VERIFIED | 11 rule blocks added: `.guide-tutorial-inner`, `.guide-tutorial-icon i`, plus 3 variants each with 3 rules (lines 1124–1191) |
| `wwwroot/documents/guides/ActiveDirectory-Guide.html` | AD guide file in correct location for serving | VERIFIED | File exists at wwwroot/documents/guides/ActiveDirectory-Guide.html |
| `Views/Home/Guide.cshtml` | Dynamic card counts, reordered FAQ, expand/collapse toggle, cleaned FAQ | VERIFIED | All 5 counts use Razor variables, toggle button functional, FAQ order correct |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `Views/Home/GuideDetail.cshtml` | `wwwroot/css/guide.css` | CSS classes guide-tutorial-card--(cmp/cdp/admin) | WIRED | Classes applied at lines 66, 90, 116; CSS rules confirmed in guide.css lines 1133–1191 |
| `Views/Home/Guide.cshtml` | role variable | `isAdminOrHc` ternary for dynamic counts | WIRED | Pattern `isAdminOrHc ? 5 : 4` present; all card `<p>` and `aria-label` use `@cmpCount`, `@cdpCount`, etc. |
| `Views/Home/Guide.cshtml` | Bootstrap Collapse API | `faqToggleAll` / `toggleAllFaq()` | WIRED | Button wired via `onclick="toggleAllFaq()"`, function uses `bootstrap.Collapse.getOrCreateInstance` on `.faq-answer` elements |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| GUIDE-01 | 171-01 | Redundant CMP accordion items removed | SATISFIED | cmpCollapse3/4/5 absent; only 1, 2, 6, 7(gated) remain |
| GUIDE-02 | 171-01 | Redundant CDP accordion items removed | SATISFIED | cdpCollapse2/3/4 absent; only 1, 5(gated), 6(gated), 7 remain |
| GUIDE-03 | 171-02 | Guide card counts dynamically reflect actual visible guides | SATISFIED | All 5 card counts use Razor int variables, role-conditional for CMP and CDP |
| GUIDE-04 | 171-01 | Tutorial PDF cards use consistent CSS classes instead of inline styles | SATISFIED | All 3 tutorial cards use variant modifier classes; no inline style attributes found on tutorial card divs |
| FAQ-01 | 171-02 | Expand all / collapse all FAQ toggle button | SATISFIED | `faqToggleAll` button with `toggleAllFaq()` Bootstrap Collapse toggle at lines 168, 472 |
| FAQ-02 | 171-02 | FAQ items reordered by priority | SATISFIED | Categories reordered; Umum moved before KKJ; within-category ordering by basic-to-advanced |
| FAQ-03 | 171-02 | FAQ categories reorganized to reduce redundancy | SATISFIED | Step-by-step assessment and coaching FAQ items removed; conceptual/policy items retained |

All 7 requirement IDs from both plan frontmatter declarations are satisfied. No orphaned requirements found — REQUIREMENTS.md marks all 7 as complete for Phase 171.

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Views/Home/Guide.cshtml` | 69, 494, 497 | `placeholder=` | None | HTML input placeholder text and JS mobile label — legitimate use, not stub code |

No blocker or warning anti-patterns detected.

---

### Human Verification Required

The following items were verified programmatically but benefit from browser confirmation due to their interactive nature. These are not blockers — automated checks passed.

#### 1. FAQ expand/collapse interaction

**Test:** Visit /Home/Guide, click "Buka Semua" button
**Expected:** All FAQ answers expand simultaneously; button text changes to "Tutup Semua"; clicking again collapses all
**Why human:** Bootstrap Collapse API behavior at runtime cannot be verified statically

#### 2. Dynamic count accuracy in browser

**Test:** Visit /Home/Guide as a worker, then as Admin
**Expected:** CMP card shows "4 panduan tersedia" for worker, "5 panduan tersedia" for Admin; CDP shows "3" vs "5"
**Why human:** Razor rendering depends on ViewBag.UserRole being set correctly by the controller

#### 3. AD tutorial card visibility

**Test:** Visit /Home/GuideDetail?module=admin as Admin; repeat as a regular worker
**Expected:** Admin sees pink AD tutorial card; worker sees no tutorial card in admin module
**Why human:** Role-conditional rendering requires a live session

---

### Commit Verification

All documented commits confirmed in git log:

| Commit | Description |
|--------|-------------|
| `3af3fa2` | feat(171-01): add tutorial card CSS variant classes and AD guide file |
| `027ec2f` | feat(171-01): simplify GuideDetail accordions, fix tutorial cards, add AD card |
| `cede70e` | feat(171-02): dynamic guide card counts and FAQ expand/collapse toggle |
| `1c1f87f` | feat(171-02): reorder FAQ categories, remove redundant items, renumber IDs |

---

## Summary

Phase 171 goal is achieved. All 9 must-have truths verified against the actual codebase:

- GuideDetail.cshtml: CMP reduced from 7 to 3+1 accordion items, CDP from 7 to 2+2(gated) accordion items, CDP 5 role-gated, all 3 tutorial cards converted from inline styles to CSS variant classes, AD tutorial card added for admin module with Admin/HC gate
- guide.css: Three tutorial card variant modifier classes (cmp/cdp/admin) with inner, icon, and title sub-selectors replace all inline styles
- wwwroot/documents/guides/ActiveDirectory-Guide.html: AD guide file in correct location
- Guide.cshtml: All 5 card counts use Razor role-conditional int variables (zero hardcoded count strings), FAQ expand/collapse toggle fully wired to Bootstrap Collapse API, FAQ categories in specified order (Akun & Login > Assessment > CDP & Coaching > Umum > KKJ & CPDP > Admin & Kelola Data), step-by-step FAQ items removed

All 7 requirement IDs (GUIDE-01 through GUIDE-04, FAQ-01 through FAQ-03) are satisfied and confirmed in REQUIREMENTS.md.

---

_Verified: 2026-03-16_
_Verifier: Claude (gsd-verifier)_
