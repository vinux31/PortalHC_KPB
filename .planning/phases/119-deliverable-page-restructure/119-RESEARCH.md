# Phase 119: Deliverable Page Restructure - Research

**Researched:** 2026-03-08
**Domain:** ASP.NET Razor view restructure (Bootstrap 5 layout)
**Confidence:** HIGH

## Summary

This phase is a pure view-layer restructure of `Views/CDP/Deliverable.cshtml`. All data, models, and controller logic already exist. The work is rearranging existing HTML into 4 Bootstrap cards with a new layout (2 side-by-side on top, 2 full-width below), removing redundant elements (alert banners, upload form), and simplifying the breadcrumb.

The existing view is 485 lines with clearly identifiable sections that map 1:1 to the target cards. The ViewModel (`DeliverableViewModel`) already exposes all needed data. ViewBag provides `CoachingSessions`, `StatusHistories`, `ApproverNames`, and `CoachNames`. No model or controller changes are needed unless we want to add Kompetensi/SubKompetensi names to the card (they are already available via `Model.Deliverable.ProtonSubKompetensi.ProtonKompetensi`).

**Primary recommendation:** Single plan, single task: rewrite Deliverable.cshtml with the 4-card layout. No backend changes needed.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- 4 separate Bootstrap cards (not tabs or single-card-with-dividers)
- Desktop: Detail Coachee & Approval Chain side-by-side (row with 2 columns), Evidence Coach and Riwayat Status full-width below
- Mobile: all cards stack vertically
- Card 1 "Detail Coachee & Kompetensi": Coachee name, Track, Kompetensi, SubKompetensi, Deliverable name. Always visible.
- Card 2 "Approval Chain": Badge status in header corner, vertical stepper SrSpv > SH > HC, rejection reason inside card (not alert banner), approve/reject/HC review buttons at bottom of card. Always visible.
- Card 3 "Evidence Coach": File evidence + download + date, then coaching session data (Coach name, Tanggal, Catatan Coach, Kesimpulan, Result). Hidden if no evidence/session.
- Card 4 "Riwayat Status": Timeline from DeliverableStatusHistory. Hidden if no history.
- Remove: alert banners (Rejected/Submitted/Approved), upload evidence form, upload ulang after rejection
- Breadcrumb simplified to: `Coaching Proton > Deliverable` (or `IDP Plan > Deliverable`)

### Claude's Discretion
- Exact spacing, padding, font sizes
- Responsive breakpoint for side-by-side vs stacked
- Column width ratio for Detail vs Approval Chain (e.g., col-md-6/col-md-6 or col-md-7/col-md-5)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PAGE-01 | Halaman Deliverable detail dibagi menjadi sections: Detail Coachee & Kompetensi, Evidence Coach, Approval Chain, Riwayat Status | 4 Bootstrap cards with existing data sources; all section content already in current view |
| PAGE-02 | Section Riwayat Status menampilkan timeline kronologis dari DeliverableStatusHistory | Existing timeline code (lines 404-467) moves into Card 4; ViewBag.StatusHistories already populated |
| PAGE-03 | Section Evidence Coach menampilkan coaching session data dan file evidence dengan download | Evidence display (lines 96-115) + Coaching Reports (lines 363-402) merge into Card 3; ViewBag.CoachingSessions already populated |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | 5.x (already in project) | Card layout, grid, badges, responsive | Already used throughout portal |
| Bootstrap Icons | (already in project) | Icons for cards | Already used |

No new libraries needed. This is purely rearranging existing Razor/HTML.

## Architecture Patterns

### Target Page Structure
```
Deliverable.cshtml
|-- Breadcrumb (simplified: 2 levels)
|-- TempData alerts (Success/Error only - keep these)
|-- IsAccessible check (keep as-is)
|-- Row (side-by-side on md+)
|   |-- Col: Card 1 - Detail Coachee & Kompetensi
|   |-- Col: Card 2 - Approval Chain (with badge, stepper, actions)
|-- Card 3 - Evidence Coach (conditional)
|-- Card 4 - Riwayat Status (conditional)
|-- Back button
```

### Layout Pattern
```html
<div class="row g-3 mb-3">
    <div class="col-md-7"><!-- Card 1: Detail --></div>
    <div class="col-md-5"><!-- Card 2: Approval Chain --></div>
</div>
<!-- Card 3: Evidence Coach (conditional) -->
<!-- Card 4: Riwayat Status (conditional) -->
```

**Recommendation:** col-md-7 for Detail (more text content), col-md-5 for Approval Chain (stepper is narrow). The `g-3` gutter provides consistent spacing.

### Card Styling Pattern (existing in project)
```html
<div class="card border-0 shadow-sm mb-3">
    <div class="card-header bg-white border-bottom d-flex justify-content-between align-items-center">
        <h6 class="mb-0 fw-semibold"><i class="bi bi-icon me-2 text-primary"></i>Title</h6>
        <span class="badge bg-xxx">Status</span> <!-- optional -->
    </div>
    <div class="card-body">...</div>
</div>
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Responsive layout | CSS media queries | Bootstrap `col-md-*` grid | Already works, handles stacking |
| Status badges | Custom styling | Existing `bg-secondary/primary/success/danger` classes | Consistent with rest of portal |

## Common Pitfalls

### Pitfall 1: Losing approval action forms
**What goes wrong:** Moving approve/reject buttons into the Approval Chain card but forgetting hidden fields or anti-forgery tokens.
**How to avoid:** Copy the entire form blocks (lines 282-335) into Card 2, keeping all asp-action, hidden inputs, and AntiForgeryToken calls intact.

### Pitfall 2: Breaking conditional visibility
**What goes wrong:** Card 2 stepper currently only shows for Submitted/Approved/Rejected. But CONTEXT says "Selalu tampil (stepper Pending jika belum ada aksi)".
**How to avoid:** Remove the status check gate (line 175) so the stepper always renders. When no approvals exist, all steps show "Pending".

### Pitfall 3: Forgetting the "Created" event in Riwayat Status
**What goes wrong:** The current timeline includes a "Deliverable dibuat" entry from `Progress.CreatedAt`. This must be preserved in Card 4.
**How to avoid:** Keep the CreatedAt entry logic when building timelineEvents.

### Pitfall 4: Evidence Coach card visibility logic
**What goes wrong:** Card 3 should be hidden when no evidence AND no coaching session. Need to check both conditions.
**How to avoid:** `@if (!string.IsNullOrEmpty(Model.Progress?.EvidencePath) || coachingSessions65.Any())`

## Code Examples

### Card 2 Header with Status Badge
```html
<div class="card-header bg-white border-bottom d-flex justify-content-between align-items-center">
    <h6 class="mb-0 fw-semibold">
        <i class="bi bi-diagram-3 me-2 text-primary"></i>Approval Chain
    </h6>
    <span class="badge @badgeClass px-3 py-2">@(Model.Progress?.Status ?? "Pending")</span>
</div>
```

### Rejection Reason Inside Stepper (not separate alert)
The existing code at lines 269-275 already renders rejection reason inside the stepper card. This pattern stays, but the separate alert banner (lines 117-143) is removed.

### Simplified Breadcrumb
```html
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-action="@bcAction">@bcLabel</a></li>
        <li class="breadcrumb-item active" aria-current="page">Deliverable</li>
    </ol>
</nav>
```

## Existing Code Mapping

| Target Card | Source Lines | What Moves |
|-------------|-------------|------------|
| Card 1: Detail Coachee | 77-93 | Info row (Coachee, Track, role badge) + add Kompetensi/SubKompetensi from breadcrumb data |
| Card 2: Approval Chain | 174-335 | Stepper + approval actions + HC review (remove status gate, always show) |
| Card 3: Evidence Coach | 96-115, 363-402 | Evidence display + Coaching Reports merged |
| Card 4: Riwayat Status | 404-467 | Timeline (unchanged) |
| REMOVED | 117-172 | Alert banners (Rejected, Submitted, Approved notices) |
| REMOVED | 337-358 | Upload Evidence form |

## Data Availability

All data is already available in the view without controller changes:

| Data | Source | Available |
|------|--------|-----------|
| Coachee name, Track, TahunKe | `Model.CoacheeName`, `Model.TrackType`, `Model.TahunKe` | Yes |
| Kompetensi | `Model.Deliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi` | Yes (used in current breadcrumb) |
| SubKompetensi | `Model.Deliverable?.ProtonSubKompetensi?.NamaSubKompetensi` | Yes (used in current breadcrumb) |
| Deliverable name | `Model.Deliverable?.NamaDeliverable` | Yes |
| Approval statuses | `Model.Progress.SrSpvApprovalStatus` etc. | Yes |
| Approver names | `ViewBag.ApproverNames` | Yes |
| Evidence file | `Model.Progress.EvidencePath`, `EvidenceFileName` | Yes |
| Coaching sessions | `ViewBag.CoachingSessions` | Yes |
| Coach names | `ViewBag.CoachNames` | Yes |
| Status history | `ViewBag.StatusHistories` | Yes |
| Action flags | `Model.CanApprove`, `Model.CanHCReview` | Yes |

**No ViewModel or controller changes needed.**

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (ASP.NET MVC Razor views) |
| Config file | N/A |
| Quick run command | `dotnet build` (compile check) |
| Full suite command | Manual UAT in browser |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PAGE-01 | 4 distinct card sections visible | manual | `dotnet build` (compile only) | N/A |
| PAGE-02 | Riwayat Status shows timeline from history | manual | `dotnet build` | N/A |
| PAGE-03 | Evidence Coach shows coaching data + download | manual | `dotnet build` | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` to verify no Razor compilation errors
- **Per wave merge:** Manual browser check across roles (Coachee, Coach, SrSpv, SH, HC)
- **Phase gate:** Full UAT with all status states (Pending, Submitted, Approved, Rejected)

### Wave 0 Gaps
None -- this is a view-only change, `dotnet build` confirms compilation.

## Open Questions

None. All data sources exist, all decisions are locked, scope is clear.

## Sources

### Primary (HIGH confidence)
- `Views/CDP/Deliverable.cshtml` - full current view (485 lines), all sections mapped
- `Models/ProtonViewModels.cs` - DeliverableViewModel with all properties confirmed
- `119-CONTEXT.md` - locked layout decisions from user discussion

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, Bootstrap already in use
- Architecture: HIGH - pure view restructure, all code exists and is mapped line-by-line
- Pitfalls: HIGH - identified from reading actual code, clear mitigations

**Research date:** 2026-03-08
**Valid until:** 2026-04-08 (stable - view restructure, no external dependencies)
