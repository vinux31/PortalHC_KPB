# Phase 99: Remove Deliverable Card from CDP Index - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core MVC UI cleanup (Bootstrap 5 card removal)
**Confidence:** HIGH

## Summary

This is a straightforward UI cleanup task: remove a navigation card from the CDP Index page that incorrectly links to a detail page without required parameters. The Deliverable page (`CDP/Deliverable?id={x}`) requires an `id` parameter, but the navigation card links to `/CDP/Deliverable` without it, causing a 404 error. The correct workflow is: CDP Index → Coaching Proton → "Lihat Detail" button.

**Primary recommendation:** Simple HTML removal task — delete lines 79-98 from `Views/CDP/Index.cshtml`, no controller, model, or database changes needed. Bootstrap grid will auto-adjust from 4 cards to 3 cards per row using existing `col-md-6 col-lg-3` responsive breakpoints.

## User Constraints

### Locked Decisions
- Remove the "Deliverable & Evidence" card from `Views/CDP/Index.cshtml` (lines 79-98)
- Do not create a replacement page or redirect
- Users access deliverable details via: CDP Index → Coaching Proton → "Lihat Detail" button
- This is a UI cleanup fix, NOT a workflow change
- Coaching Proton page remains the primary entry point for deliverable management
- Deliverable detail page (`CDP/Deliverable?id={x}`) continues to work as before

### Claude's Discretion
- Bootstrap grid adjustment after card removal (whether to expand other cards or leave gap)
- No other changes to CDP Index page unless user explicitly requests

### Deferred Ideas (OUT OF SCOPE)
None

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| None | UI cleanup fix only — no requirement IDs assigned | N/A |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0.0 | Web framework | Project target framework |
| Bootstrap CSS | 5.3.0 | Responsive grid and cards | Already in use via CDN |
| Bootstrap Icons | 1.10.0 | Icon set (bi-*) | Already in use via CDN |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| None | — | This is pure HTML removal task | No additional libraries needed |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Simple card removal | Replace with redirect page | Unnecessary complexity — removal is cleaner |
| Simple card removal | Add id parameter logic | Wrong layer — Deliverable is detail view, not list view |

**Installation:**
No installation needed — using existing project stack.

## Architecture Patterns

### Recommended Project Structure
```
Views/
└── CDP/
    ├── Index.cshtml          # ← MODIFY: Remove Deliverable card (lines 79-98)
    ├── PlanIdp.cshtml        # Unchanged
    ├── CoachingProton.cshtml # Unchanged (correct workflow entry point)
    ├── Dashboard.cshtml      # Unchanged
    └── Deliverable.cshtml    # Unchanged (detail page remains functional)
```

### Pattern 1: Bootstrap Card Grid in ASP.NET Core MVC Views
**What:** Self-contained Bootstrap 5 cards in responsive grid layout using `row` + `col-*` classes
**When to use:** Navigation hub pages with multiple entry points
**Example:**
```html
<!-- Current CDP Index structure -->
<div class="row g-4">
    <!-- Card 1 -->
    <div class="col-12 col-md-6 col-lg-3">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-body">
                <!-- Card content -->
            </div>
        </div>
    </div>
    <!-- Card 2, 3, 4... -->
</div>
```

**Key insight:** Bootstrap 5 responsive grid (`col-md-6 col-lg-3`) auto-adjusts:
- 4 cards = 4 per row on lg (≥992px), 2 per row on md (≥768px), 1 per row on xs
- 3 cards = 3 per row on lg, 2 per row on md (with gap), 1 per row on xs
- No explicit grid recalculation needed — Bootstrap handles layout automatically

### Pattern 2: ASP.NET Core MVC Action Link Generation
**What:** Using `Url.Action()` helper to generate controller action URLs
**When to use:** Link generation in Razor views to avoid hardcoded paths
**Example:**
```cshtml
<!-- CORRECT: Action link with required parameter -->
<a href="@Url.Action("Deliverable", "CDP", new { id = Model.ProgressId })">
    View Deliverable
</a>

<!-- INCORRECT: Action link without required parameter (current bug) -->
<a href="@Url.Action("Deliverable", "CDP")">
    View Deliverables
</a>
```

**Current bug analysis:**
```cshtml
<!-- Line 93 in Views/CDP/Index.cshtml -->
<a href="@Url.Action("Deliverable", "CDP")" class="btn btn-success w-100">
```

This generates `/CDP/Deliverable` without `id` parameter, but controller action requires it:
```csharp
// Line 718 in Controllers/CDPController.cs
public async Task<IActionResult> Deliverable(int id)
```

Result: 404 error or model binding failure depending on routing configuration.

### Anti-Patterns to Avoid
- **Hand-coded URL paths:** Don't use `<a href="/CDP/Deliverable">` — breaks if routing changes. Always use `Url.Action()`
- **Partial card removal:** Don't remove only card body — must remove entire `col-*` wrapper div to avoid grid breakage
- **Leaving orphaned CSS:** Card-specific styles (lines 105-122) are generic, no cleanup needed

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Navigation fix | Custom redirect logic | Remove broken link entirely | Deliverable is detail view, not list — redirect would be wrong UX |
| Grid layout adjustment | Manual width calculations | Bootstrap auto-layout | Grid system designed for this — 3 cards work fine with existing `col-lg-3` |
| URL validation | Custom route constraints | Use existing model binding | `int id` parameter already enforces requirement at framework level |

**Key insight:** This is purely subtractive — remove broken UI, don't add replacement complexity. The Coaching Proton page already provides correct navigation flow.

## Common Pitfalls

### Pitfall 1: Incomplete Card Removal
**What goes wrong:** Removing only card content (`<div class="card-body">...</div>`) but leaving the `col-*` wrapper div
**Why it happens:** Misunderstanding Bootstrap grid structure — cards live inside column wrappers
**How to avoid:** Remove entire block from opening `<div class="col-12 col-md-6 col-lg-3">` to closing `</div>` (lines 79-98 inclusive)
**Warning signs:** Empty space in grid layout, cards not aligning properly after removal

### Pitfall 2: Accidentally Removing Working Cards
**What goes wrong:** Misidentifying line numbers and deleting wrong card (Plan IDP, Coaching Proton, or Dashboard)
**Why it happens:** Similar card structure — all use same Bootstrap classes and layout
**How to avoid:** Verify card by content: Deliverable card has title "Deliverable" (line 88), icon `bi-file-earmark-check` (line 85), green theme (`bg-success` on line 84)
**Warning signs:** Other navigation links broken after change

### Pitfall 3: Modifying Controller or Routes
**What goes wrong:** "Fixing" the Deliverable action to handle missing `id` parameter instead of removing broken link
**Why it happens:** Treating symptom (404 error) instead of root cause (incorrect navigation UX)
**How to avoid:** Remember user decision — Deliverable is detail page accessed via Coaching Proton, not via Index card
**Warning signs:** Adding `[HttpGet("Deliverable")]` overload without parameters, adding default `id = 0` optional parameter

### Pitfall 4: Leaving Orphaned CSS References
**What goes wrong:** Removing card but leaving card-specific CSS classes that are no longer used
**Why it happens:** Over-cleaning — removing styles that might be generic
**How to avoid:** Check if CSS classes (`.icon-box`, `.card:hover`) are used by other cards before removal
**Warning signs:** Other cards losing hover effects or icon styling

**Current CSS analysis (lines 105-122):**
```css
.icon-box { /* Used by all 3 remaining cards */ }
.card { /* Generic hover effect for all cards */ }
.card:hover { /* Generic hover effect for all cards */ }
```
**Verdict:** No CSS cleanup needed — styles are shared across all cards.

## Code Examples

Verified patterns from codebase:

### Card Removal Pattern
```html
<!-- BEFORE: 4 cards in grid -->
<div class="row g-4">
    <!-- Plan IDP card (lines 17-35) -->
    <!-- Coaching Proton card (lines 38-56) -->
    <!-- Dashboard Monitoring card (lines 59-77) -->
    <!-- Deliverable card (lines 79-98) ← DELETE THIS BLOCK -->
</div>

<!-- AFTER: 3 cards in grid -->
<div class="row g-4">
    <!-- Plan IDP card (unchanged) -->
    <!-- Coaching Proton card (unchanged) -->
    <!-- Dashboard Monitoring card (unchanged) -->
    <!-- Deliverable card REMOVED -->
</div>
```

### Deliverable Action Signature (for reference)
```csharp
// Source: Controllers/CDPController.cs, line 718
public async Task<IActionResult> Deliverable(int id)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    // ... authorization and data loading logic

    return View(progress);
}
```

**Key insight:** Action requires `id` parameter — no overload without it, no default value. This is intentional: Deliverable is a detail view, not a list view.

### Correct Navigation Flow (unchanged)
```cshtml
<!-- In CoachingProton.cshtml, "Lihat Detail" button -->
<a href="@Url.Action("Deliverable", "CDP", new { id = item.ProgressId })"
   class="btn btn-sm btn-primary">
    <i class="bi bi-eye me-1"></i>Lihat Detail
</a>
```

**Pattern:** Coaching Proton page provides `progressId` context, generates correct URL with parameter. Index card cannot provide this context.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded URLs | `Url.Action()` helper | Project inception | Routing-aware link generation |
| Manual CSS grid | Bootstrap 5 flexbox grid | Project inception | Responsive layout without custom CSS |
| Inline styles | External CSS blocks | Project inception | Separation of concerns, maintainability |

**Deprecated/outdated:**
- Bootstrap 4.x syntax — Project uses Bootstrap 5.3.0 (verified in `_Layout.cshtml`)
- jQuery-based grid manipulation — Not needed with Bootstrap 5's native flexbox

## Open Questions

1. **Bootstrap grid behavior with 3 cards**
   - What we know: Bootstrap 5 `col-lg-3` creates 4-column grid, auto-wraps to 3 cards with 1 empty column slot
   - What's unclear: Whether visual gap is acceptable or if user prefers cards expand to fill width
   - Recommendation: Default to auto-layout (leave gap) — maintain consistent card sizing. If user requests, change `col-lg-3` → `col-lg-4` for 3 cards to fill row evenly

2. **Card removal impact on user workflow**
   - What we know: Coaching Proton page already has "Lihat Detail" buttons for each deliverable
   - What's unclear: Whether users were using the broken Index card as a shortcut before discovering it was broken
   - Recommendation: Monitor user feedback — if users complain about missing shortcut, clarify correct workflow (Coaching Proton → Lihat Detail)

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None (manual testing only) |
| Config file | N/A |
| Quick run command | N/A — manual browser verification |
| Full suite command | N/A — manual browser verification |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| None | UI cleanup fix — no functional requirements | Manual only | N/A | N/A |

### Sampling Rate
- **Per task commit:** Manual browser verification — navigate to `/CDP/Index`, confirm Deliverable card removed
- **Per wave merge:** N/A — single-task phase
- **Phase gate:** Manual browser verification — confirm grid layout correct, other cards still functional

### Wave 0 Gaps
- **None** — This is pure UI removal task with no functional requirements. No automated test infrastructure exists in project, and manual verification is sufficient for HTML removal.

### Verification Steps
1. Navigate to `/CDP/Index`
2. Confirm only 3 cards displayed (Plan IDP, Coaching Proton, Dashboard Monitoring)
3. Confirm "Deliverable" card no longer visible
4. Confirm grid layout adjusts correctly (3 cards on lg screens, 2 on md with gap, 1 on xs)
5. Verify other 3 cards still navigate correctly to respective pages
6. Verify Coaching Proton → "Lihat Detail" button still navigates to Deliverable detail page

## Sources

### Primary (HIGH confidence)
- **Views/CDP/Index.cshtml** - Current card grid structure, lines 79-98 identified for removal
- **Controllers/CDPController.cs** - Deliverable action signature requiring `id` parameter (line 718)
- **HcPortal.csproj** - Project target framework .NET 8.0, no test frameworks referenced
- **Views/Shared/_Layout.cshtml** - Bootstrap 5.3.0 and Bootstrap Icons 1.10.0 CDN links

### Secondary (MEDIUM confidence)
- **Bootstrap 5.3 Documentation** - Grid system auto-layout behavior with `col-*` classes
- **ASP.NET Core MVC Documentation** - `Url.Action()` helper usage and parameter binding

### Tertiary (LOW confidence)
- None — all findings verified directly from codebase or official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Verified from project files (HcPortal.csproj, _Layout.cshtml)
- Architecture: HIGH - Code review of existing Views/CDP/Index.cshtml and Controllers/CDPController.cs
- Pitfalls: HIGH - Common Bootstrap grid and Razor view mistakes well-documented

**Research date:** 2026-03-05
**Valid until:** 30 days (stable domain — HTML/CSS removal pattern doesn't change)

---

*Research complete. Ready for planner to create task breakdown.*
