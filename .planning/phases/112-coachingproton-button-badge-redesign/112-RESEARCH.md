# Phase 112: CoachingProton Button & Badge Redesign - Research

**Researched:** 2026-03-07
**Domain:** Bootstrap 5 CSS styling, single-file Razor view refactor
**Confidence:** HIGH

## Summary

This phase is a pure visual redesign of `Views/CDP/CoachingProton.cshtml` -- converting clickable badge elements into proper buttons and establishing consistent styling conventions. The file is ~1500 lines, self-contained (no external CSS files), using Bootstrap 5.3.0 utility classes exclusively.

The critical technical risk is in three JavaScript innerHTML assignments (lines 1102, 1138, 1179, 1418, 1424, 1428) that dynamically render badges after AJAX operations. These must be updated to match the new styling conventions, otherwise approved/rejected states will revert to old styling.

**Primary recommendation:** Change elements in two passes: (1) Razor template HTML for server-rendered elements, (2) JavaScript innerHTML strings for AJAX-updated elements. Verify each JS innerHTML matches the new convention.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Pending "Tinjau" badges in SrSpv/SH columns become `btn btn-sm btn-outline-warning` buttons
- Button text: "Tinjau" (not "Pending") -- action verb communicates what clicking does
- No icon on Tinjau button -- outline style is sufficient to signal interactivity
- Non-clickable Pending badge stays `badge bg-secondary` (gray) -- clearly passive
- After approval, button transforms into status badge (e.g., green "Approved") -- not removed
- When both SrSpv and SH columns show Pending, independent buttons -- no row grouping
- No icons on any status badges -- text + color + font weight differentiation only
- Resolved statuses (Approved, Rejected, Reviewed) get bold text + solid border for visual weight
- Unresolved statuses (Pending, Submitted) stay normal weight, no border
- Color mapping unchanged: Approved=green, Rejected=red, Pending=gray, Reviewed=green, Submitted=blue
- Evidence badges follow same pattern: Sudah Upload=bold green+border, Belum Upload=normal gray
- All clickable elements use `btn` classes (border, hover effect)
- All read-only status elements use `badge` classes (flat, no hover)
- Submit Evidence stays filled `btn-primary` (most prominent action for Coach)
- HC Review uses `btn-outline-success` in both table and Antrian Review panel -- same style
- Navigation: `btn-outline-secondary` -- Lihat Detail, Kembali, Reset
- Export: `btn-outline-success` -- both Excel and PDF (PDF changes from outline-danger to outline-success)
- Approval: `btn-outline-warning` -- Tinjau
- Review: `btn-outline-success` -- HC Review
- Primary action: `btn-primary` filled -- Submit Evidence
- Keep existing icons on action buttons: Export Excel (bi-file-earmark-excel), Export PDF (bi-file-earmark-pdf), Kembali (bi-arrow-left), Reset (bi-arrow-counterclockwise)
- No icons on status badges
- Must preserve `.btnTinjau`, `.btnSubmitEvidence`, `.btnHcReview`, `.btnHcReviewPanel` CSS class hooks for JS
- Must preserve `data-bs-toggle="modal"` + `data-bs-target="#tinjaModal"` on Tinjau elements

### Claude's Discretion
- Button sizing (btn-sm vs regular) for top-level vs table-inline buttons
- Exact border color/style for bold status badges
- Spacing and alignment adjustments
- Any minor CSS tweaks needed for visual polish

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| BTN-01 | Clickable Pending badge di kolom SrSpv berubah menjadi proper button | Change `<span class="badge bg-warning...btnTinjau">Pending</span>` to `<button class="btn btn-sm btn-outline-warning btnTinjau"...>Tinjau</button>` (lines 440-448, 556-564) |
| BTN-02 | Clickable Pending badge di kolom SH berubah menjadi proper button | Same conversion for SH column (lines 462-471, 579-588) |
| BTN-03 | Semua status badges menampilkan icon yang sesuai | CONTEXT.md overrides: NO icons on badges. Instead, resolved statuses get bold text + border. Update `GetApprovalBadge` and `GetApprovalBadgeWithTooltip` helper functions (lines 920-956) |
| CONS-01 | Evidence column style konsisten | Evidence "Sudah Upload" badge gets bold+border; "Belum Upload" stays normal gray. Update 4 locations (lines 429, 433, 545, 549) plus JS innerHTML (lines 1417-1419) |
| CONS-02 | "Lihat Detail" button lebih standout | Already `btn btn-sm btn-outline-secondary` -- keep as-is per convention (lines 500-501, 616-617) |
| CONS-03 | HC Review button style konsisten | Already `btn btn-sm btn-outline-success` in both main table (line 495) and panel (line 785). Verify identical styling |
| CONS-04 | Export, Reset, Kembali buttons polished dan konsisten | Export PDF changes from `btn-outline-danger` to `btn-outline-success` (line 294). Reset and Kembali already correct |
| TECH-01 | JS event handlers tetap berfungsi | Preserve `.btnTinjau`, `.btnSubmitEvidence`, `.btnHcReview`, `.btnHcReviewPanel` classes. Change `<span>` to `<button>` for Tinjau -- JS uses class selector, not tag |
| TECH-02 | Modal triggers tetap bekerja | Keep `data-bs-toggle="modal"` and `data-bs-target="#tinjaModal"` on new `<button>` elements. Bootstrap modal works with both `<span>` and `<button>` triggers |
| TECH-03 | AJAX innerHTML updates konsisten | Update 6 JS innerHTML assignments: approval badge (line 1102), HC review badge (lines 1138, 1179), evidence badges (lines 1417-1419, 1424, 1428) |
</phase_requirements>

## Architecture Patterns

### Single File, All Changes
Everything lives in `Views/CDP/CoachingProton.cshtml`:
- Razor helper functions (`@functions` block, lines 920-957): `GetApprovalBadge`, `GetApprovalBadgeWithTooltip`, `GetStatusBadge`
- Inline HTML in two table rendering loops (multi-coachee view lines 388-507, single-coachee view lines 512-623)
- HC Review Panel (lines 727-804)
- Export buttons (lines 288-298)
- JavaScript innerHTML strings (lines 1086-1218, 1405-1430)

### Change Categories

**Category A -- Razor helper functions (highest leverage):**
Update `GetApprovalBadge` and `GetApprovalBadgeWithTooltip` to add `fw-bold border` for Approved/Rejected/Reviewed. This automatically fixes ALL server-rendered status badges across both table views.

**Category B -- Clickable Tinjau conversion (4 locations):**
Lines 440-448, 462-471, 556-564, 579-588. Change `<span class="badge bg-warning...">Pending</span>` to `<button class="btn btn-sm btn-outline-warning btnTinjau"...>Tinjau</button>`.

**Category C -- Evidence badges (4 HTML + 3 JS locations):**
HTML: lines 429, 433, 545, 549. JS: lines 1417-1419, 1424, 1428. Add `fw-bold border border-success` to "Sudah Upload" badge.

**Category D -- JS innerHTML for post-AJAX badge updates (3 locations):**
Lines 1102, 1138, 1179. Must match new `GetApprovalBadgeWithTooltip` output format.

**Category E -- Export PDF button (1 location):**
Line 294: change `btn-outline-danger` to `btn-outline-success`.

### Recommended Approach
```
1. Update @functions helper methods (fixes all server-rendered badges at once)
2. Convert Tinjau <span> to <button> (4 locations, 2 per table view)
3. Update Evidence badge HTML (4 locations)
4. Update JS innerHTML strings (6 locations)
5. Update Export PDF button class (1 location)
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Bold badge styling | Custom CSS classes | Bootstrap `fw-bold border border-{color}` utilities | Already loaded, zero maintenance |
| Button hover effects | Custom :hover CSS | Bootstrap `btn-outline-*` classes | Built-in hover/focus states |

## Common Pitfalls

### Pitfall 1: JS innerHTML Drift
**What goes wrong:** Server-rendered badges look correct but AJAX-updated badges revert to old styling after approve/reject actions.
**Why it happens:** JS innerHTML strings on lines 1102, 1138, 1179, 1417-1428 are independent from Razor helpers.
**How to avoid:** Grep for every `innerHTML` assignment in the script section. Each one that renders a badge must match the new convention.
**Warning signs:** Badge styling changes after clicking Approve/Reject/Review.

### Pitfall 2: Breaking Modal Trigger on Tag Change
**What goes wrong:** Changing `<span>` to `<button>` for Tinjau breaks modal opening.
**Why it happens:** `data-bs-toggle="modal"` works on any element, but `<button>` inside a `<form>` could trigger form submission.
**How to avoid:** Add `type="button"` to all Tinjau buttons. The Tinjau buttons are inside `<td>` (not a form), so this is low risk but good practice.

### Pitfall 3: Duplicate Code in Two Table Views
**What goes wrong:** Fix multi-coachee view but miss single-coachee view (or vice versa).
**Why it happens:** The same column rendering is duplicated for `showCoacheeColumn` true/false paths.
**How to avoid:** Each change must be applied in BOTH loops. There are exactly 2 Tinjau locations per column (SrSpv + SH), totaling 4 Tinjau conversions.

### Pitfall 4: HC Review Panel Badge Count Badge
**What goes wrong:** The `@pendingCount pending` badge in HC Review Panel header (line 741) uses `badge bg-warning`. This is a counter badge, not a status badge -- leave it as-is.
**How to avoid:** Only change status badges, not counter/notification badges.

## Code Examples

### Updated GetApprovalBadge Helper
```csharp
string GetApprovalBadge(string status)
{
    return status switch
    {
        "Approved" => "<span class=\"badge bg-success fw-bold border border-success\">Approved</span>",
        "Rejected" => "<span class=\"badge bg-danger fw-bold border border-danger\">Rejected</span>",
        "Reviewed" => "<span class=\"badge bg-success fw-bold border border-success\">Reviewed</span>",
        "Pending"  => "<span class=\"badge bg-secondary\">Pending</span>",
        _ => "<span class=\"badge bg-light text-dark\">-</span>",
    };
}
```

### Tinjau Button Conversion (from span badge to button)
```html
<!-- BEFORE -->
<span class="badge bg-warning text-dark btnTinjau" role="button" style="cursor:pointer"
      data-bs-toggle="modal" data-bs-target="#tinjaModal"
      data-progress-id="@item.Id" ...
      title="Klik untuk tinjau">Pending</span>

<!-- AFTER -->
<button type="button" class="btn btn-sm btn-outline-warning btnTinjau"
        data-bs-toggle="modal" data-bs-target="#tinjaModal"
        data-progress-id="@item.Id" ...
        title="Klik untuk tinjau">Tinjau</button>
```

### JS innerHTML Update (approval result)
```javascript
// BEFORE (line 1102)
cell.innerHTML = `<span class="badge ${badgeClass}" data-bs-toggle="tooltip" title="${tooltip}">${data.newStatus}</span>`;

// AFTER
cell.innerHTML = `<span class="badge ${badgeClass} fw-bold border border-${data.newStatus === 'Approved' ? 'success' : 'danger'}" data-bs-toggle="tooltip" title="${tooltip}">${data.newStatus}</span>`;
```

### Export PDF Button
```html
<!-- BEFORE -->
<a href="..." class="btn btn-sm btn-outline-danger">
    <i class="bi bi-file-earmark-pdf"></i> Export PDF
</a>

<!-- AFTER -->
<a href="..." class="btn btn-sm btn-outline-success">
    <i class="bi bi-file-earmark-pdf"></i> Export PDF
</a>
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated UI test framework) |
| Config file | none |
| Quick run command | `dotnet build` (compile check only) |
| Full suite command | `dotnet build` + manual browser verification |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| BTN-01 | SrSpv Tinjau button renders as proper button | manual-only | N/A -- visual verification as SrSpv role | N/A |
| BTN-02 | SH Tinjau button renders as proper button | manual-only | N/A -- visual verification as SH role | N/A |
| BTN-03 | Resolved badges show bold+border, no icons | manual-only | N/A -- visual inspection | N/A |
| CONS-01 | Evidence column consistent styling | manual-only | N/A -- visual inspection | N/A |
| CONS-02 | Lihat Detail button standout | manual-only | N/A -- visual inspection | N/A |
| CONS-03 | HC Review button consistent | manual-only | N/A -- compare table vs panel | N/A |
| CONS-04 | Export/Reset/Kembali polished | manual-only | N/A -- visual inspection | N/A |
| TECH-01 | JS event handlers work after redesign | manual-only | N/A -- click Tinjau/Submit/Review buttons | N/A |
| TECH-02 | Modal triggers work | manual-only | N/A -- click Tinjau, verify modal opens | N/A |
| TECH-03 | AJAX innerHTML uses new classes | manual-only | N/A -- approve/reject, verify badge updates | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` (ensure no Razor compilation errors)
- **Per wave merge:** Manual browser test across SrSpv, SH, HC, Coach roles
- **Phase gate:** All 10 requirements visually verified

### Wave 0 Gaps
None -- no automated test infrastructure needed for pure CSS/HTML changes. `dotnet build` is sufficient to catch syntax errors.

## Sources

### Primary (HIGH confidence)
- Direct code inspection of `Views/CDP/CoachingProton.cshtml` (1500+ lines)
- Bootstrap 5.3.0 documentation (loaded via CDN in project)

### Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Bootstrap 5.3.0, no custom CSS, all utility classes
- Architecture: HIGH - single file, all change locations identified with line numbers
- Pitfalls: HIGH - JS innerHTML drift is the primary risk, all 6 locations documented

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable -- no framework changes expected)
