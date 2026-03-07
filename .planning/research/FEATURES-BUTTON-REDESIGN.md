# Feature Landscape: CoachingProton Button & UI Redesign

**Domain:** Enterprise approval dashboard — button/indicator consistency
**Researched:** 2026-03-07
**Confidence:** HIGH (established UX patterns + direct codebase analysis)

---

## Current State Analysis

The CoachingProton page has three interaction patterns that are inconsistently styled:

1. **Badge-as-button** — `<span class="badge bg-warning btnTinjau" role="button">Pending</span>` for SrSpv/SH approval columns. Clickable badge that opens a modal. Only affordance: `cursor:pointer` and a title tooltip.
2. **Actual buttons** — `btn btn-sm btn-primary` (Submit Evidence), `btn-outline-success` (HC Review), `btn-outline-secondary` (Lihat Detail).
3. **Static badges** — `badge bg-success/bg-secondary/bg-danger` for status display (Sudah Upload, Belum Upload, Approved, Rejected).

**Core problem:** Clickable badges and static badges look nearly identical. Users cannot tell at a glance what is actionable vs informational.

---

## Table Stakes

Features users expect. Missing = page feels broken or confusing.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Visual separation of status vs action | Users must instantly distinguish "shows state" from "click to act" | Low | Badges = status only. Buttons = actions only. Never mix. |
| Consistent button hierarchy | Primary/secondary/tertiary actions need clear visual weight | Low | One primary per row max. Current page already does this by role visibility. |
| Hover states on all clickable elements | Feedback that something is interactive | Low | Present on `btn-*`, missing on badge-as-button |
| Icon + label on action buttons | Small table buttons need both for scannability | Low | HC "Review" has icon. SrSpv/SH "Pending" badge-button has none. |
| Icons on status badges | Accessibility — colorblind users need more than color | Low | check-circle for Approved, x-circle for Rejected, hourglass for Pending |

---

## Differentiators

Features that elevate from functional to polished.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Status + action stacked in approval cells | Show current state (badge) above available action (button) in same cell | Low | HC column already does this pattern. Extend to SrSpv/SH. |
| Contextual button colors for approval actions | Tinjau = outline-warning, Review = outline-success. Matches action semantics. | Low | Replaces ambiguous yellow badge-button |
| Muted "not your turn" indicators | Pending items outside your role show as light/muted badge with border | Low | Distinguishes "waiting for someone else" from "waiting for you" |
| Brief fade/highlight on AJAX success | Row briefly highlights green when approved, red when rejected | Low | Immediate feedback that action succeeded |
| Consistent sizing across all table buttons | All actions use `btn-sm` uniformly | Low | Current page already mostly does this |

---

## Anti-Features

Features to explicitly NOT build.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Badge-as-button pattern | Violates affordance — badges communicate state, buttons communicate action. Users miss clickable badges. | Replace all clickable badges with proper `btn btn-sm` elements. |
| Inline approve/reject buttons directly in table cells | Too many buttons per row = visual noise, accidental clicks on mobile | Keep modal pattern (tinjaModal) — forces deliberate action, allows comments. |
| Color-only status differentiation | WCAG accessibility failure for colorblind users | Add icons to every badge: check-circle, x-circle, hourglass, dash-circle. |
| Different button styles per role | Maintenance burden, inconsistent experience | Same visual language for all roles; show/hide by permission, never restyle. |
| Dropdown menus in table cells | Adds click depth, hides actions, poor mobile UX | Single clear button per cell, modal for multi-step flows. |
| Tooltip-only affordance for clickable elements | Title tooltips are invisible on mobile/touch devices | Use visible button styling. Tooltips are supplementary, never primary. |

---

## Specific Redesign Recommendations

### 1. Approval Columns (SrSpv, SH, HC) — The Core Fix

**Current (broken):**
- Actionable for current role: `<span class="badge bg-warning btnTinjau">Pending</span>` (looks like status)
- Not actionable: `<span class="badge bg-secondary">Pending</span>` (looks identical minus color)

**Recommended:**

| State | Element | Style |
|-------|---------|-------|
| Actionable (your turn) | `<button>` | `btn btn-sm btn-outline-warning text-dark` with icon `bi-eye` + "Tinjau" |
| Approved (done) | `<span>` badge | `badge bg-success` with icon `bi-check-circle` |
| Rejected (done) | `<span>` badge | `badge bg-danger` with icon `bi-x-circle` |
| Pending (not your role) | `<span>` badge | `badge bg-light text-muted border` with icon `bi-hourglass` |
| Not yet reached in chain | plain text | `<span class="text-muted">` em-dash |

### 2. Evidence Column — Already Correct

Current pattern is fine: `btn-primary` for Submit Evidence action, `badge` for upload status. No changes needed.

### 3. Detail Column — Already Correct

`btn-outline-secondary` for Lihat Detail is correct tertiary styling. No changes needed.

### 4. HC Review Panel — Minor Polish

Current: `btn-outline-success btnHcReviewPanel` with icon. Already good. Just ensure consistent sizing with main table buttons.

### 5. Export Buttons — Already Correct

`btn-outline-success` (Excel) and `btn-outline-danger` (PDF) follow standard conventions. No changes needed.

---

## Button Hierarchy (per row)

| Priority | Action | Style | When Visible |
|----------|--------|-------|-------------|
| Primary | Submit Evidence | `btn-primary` | Coach, when status = Pending/Rejected |
| Secondary | Tinjau / Review | `btn-outline-warning` or `btn-outline-success` | Approver, when item awaits their approval |
| Tertiary | Lihat Detail | `btn-outline-secondary` | Always (all roles) |

Only one primary action visible per row per role — already naturally enforced by role-based rendering.

---

## Feature Dependencies

```
Badge-to-button conversion (SrSpv/SH columns)
  └─> Update JS handlers: .btnTinjau selector still works if class kept on new <button>
  └─> Update AJAX response handlers that replace cell innerHTML

Icon addition to all badges
  └─> Already using Bootstrap Icons (bi-*), no new dependency

Muted "not your turn" styling
  └─> No dependency, pure CSS class change

AJAX success highlight
  └─> Depends on existing AJAX handlers already in page JS
```

---

## MVP Recommendation

**Priority order (do all in one phase — total complexity is Low):**

1. **Replace badge-as-button with actual buttons** in SrSpv and SH columns — highest impact fix
2. **Add icons to all status badges** — accessibility + scannability
3. **Muted styling for "not your turn" pending states** — clarity
4. **Verify JS handlers still work** with new button elements (class selectors)

**Defer (nice-to-have, separate phase if desired):**
- AJAX success animation/highlight
- HC Review Panel visual alignment with main table

---

## Complexity Assessment

| Change | Complexity | Reason |
|--------|------------|--------|
| Badge-to-button conversion | Low | HTML element swap, keep same classes/data attributes |
| Icon addition to badges | Low | Add `<i class="bi bi-*">` inside existing badge spans |
| Muted pending styling | Low | CSS class change: `bg-secondary` to `bg-light text-muted border` |
| JS handler verification | Low | `.btnTinjau` class can stay on new `<button>` elements |
| GetApprovalBadge helper update | Low | Two Razor helper functions at bottom of file |

**Overall: Single-phase, Low complexity milestone.**

---

## Sources

- Direct codebase analysis: `Views/CDP/CoachingProton.cshtml` (lines 417-620, badge-as-button pattern)
- Bootstrap 5 component guidelines: buttons for actions, badges for status labels
- WCAG 2.1 SC 1.4.1: color should not be the only visual means of conveying information
- Nielsen Norman Group affordance principles: interactive elements must look interactive
