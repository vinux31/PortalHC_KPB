# Phase 149: Homepage View & Controller Redesign - Research

**Researched:** 2026-03-10
**Domain:** ASP.NET Core MVC — View (Razor), ViewModel, Controller data fetching
**Confidence:** HIGH

## Summary

Phase 149 replaces the current Homepage with a **clean hero greeting + Quick Access cards only**. The three glassmorphism cards (IDP Status, Pending Assessment, Mandatory Training) are removed, along with the Recent Activity timeline and Upcoming Deadlines sections. The controller stops fetching unused data (activities, deadlines, training status), and the ViewModel is trimmed to only properties the view actually uses.

The hero section keeps its purple gradient background and clean styling (Phase 148 already removed pseudo-element decorations). Quick Access cards are restyled to match the Bootstrap card pattern used by CMP/CDP Index pages (border-0, shadow-sm, h-100).

**Primary recommendation:** Restructure View to show only hero + Quick Access using Bootstrap card classes, delete helper types (RecentActivityItem, DeadlineItem, TrainingStatusInfo) from ViewModel, remove data fetching methods from Controller, and update CSS rules to reflect the new layout.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Keep gradient hero as-is (purple gradient background, avatar circle, badge pills, date) — Phase 148 already removed pseudo-element decorations
- Keep 3 Quick Access cards, rename to match destinations: CDP, Assessment, CMP
- Switch to Bootstrap `card border-0 shadow-sm` pattern (matching CMP/CDP Index cards)
- Uniform Bootstrap-toned icons (no colorful gradient icon boxes)
- Icon + name only — no subtitles or descriptions
- Links stay: /CDP/Index, /CMP/Assessment, /CMP/Index
- Full cleanup: remove all unused properties from DashboardHomeViewModel
- Remove: IdpTotalCount, IdpCompletedCount, IdpProgressPercentage, PendingAssessmentCount, HasUrgentAssessments, MandatoryTrainingStatus, RecentActivities, UpcomingDeadlines
- Remove controller methods: GetRecentActivities, GetUpcomingDeadlines, GetMandatoryTrainingStatus
- Remove related helper types if only used by Homepage: RecentActivityItem, DeadlineItem, TrainingStatusInfo
- Keep: CurrentUser, Greeting (still used by hero)
- Remove all unused CSS rules: circular-progress, gradient-text, badge-gradient, section-header, hero-stats
- Keep only: hero-section rules, quick-access rules (restyled to Bootstrap pattern), responsive rules for hero
- Full width layout, same container width as current
- Page ends cleanly after Quick Access section — no empty space fillers

### Claude's Discretion
- Exact Bootstrap icon color/shade for Quick Access cards
- Whether to keep or simplify quick-access-card hover effects
- How to handle the ViewModel class if it becomes too thin (merge into simpler model or keep)

### Deferred Ideas (OUT OF SCOPE)
- Role-based Quick Access differentiation
- Personalized shortcut reordering

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HOME-01 | Homepage does not show IDP Status, Pending Assessment, or Mandatory Training cards for any role | Controller currently fetches IdpTotalCount, IdpCompletedCount, IdpProgressPercentage, PendingAssessmentCount, HasUrgentAssessments, MandatoryTrainingStatus. View renders 3 glass-card divs. MUST remove both. |
| HOME-02 | Homepage does not show Recent Activity timeline section | Controller.GetRecentActivities() populates RecentActivities list (lines 152-210). View renders .timeline section (lines 207-240). MUST remove both. |
| HOME-03 | Homepage does not show Upcoming Deadlines section | Controller.GetUpcomingDeadlines() populates UpcomingDeadlines list (lines 212-286). View renders .deadline-card sections (lines 243-298). MUST remove both. |
| HOME-04 | Controller/ViewModel no longer fetch data that's not used (activities, deadlines) | Remove GetRecentActivities, GetUpcomingDeadlines, GetMandatoryTrainingStatus from HomeController. Remove corresponding properties from DashboardHomeViewModel. Keep CurrentUser, Greeting. |
| HERO-01 | Hero section uses styling clean without glassmorphism/gradient pseudo-elements | Phase 148 removed pseudo-elements. Hero base styling (.hero-section gradient, padding, border-radius, box-shadow) stays. Badges still use rgba backgrounds, NOT backdrop-filter blur. |
| HERO-02 | Hero section displays greeting, name, position, unit, tanggal with no glassmorphism/gradient pseudo-elements | View currently renders greeting (line 20), FullName (line 20), Position (line 25), Unit (line 29), date (lines 36-37). Keep all, remove pseudo-elements. |
| QUICK-01 | Quick Access cards use Bootstrap card pattern (shadow-sm, border-0) matching CMP/CDP | Current Quick Access uses custom .quick-access-card class with gradient backgrounds. MUST switch to Bootstrap: `card border-0 shadow-sm h-100` with simple icon boxes (bg-opacity-10 + text-primary pattern). |

</phase_requirements>

---

## Standard Stack

### Core Technologies
| Technology | Version | Purpose | Status |
|------------|---------|---------|--------|
| ASP.NET Core | 8.0 (implied) | Web framework, MVC routing | Active — no changes |
| Entity Framework Core | Latest (implicit) | Database queries | Active — only removing unused queries |
| Bootstrap | 5.x | Grid, card utilities, responsive | Primary for redesign |
| C# | 12 (implied) | Backend logic | Active — trimming code |

### Architecture Patterns (Current)
- **HomeController.cs** (322 lines):
  - `Index()` action fetches user + 6 separate data queries (IDP, assessments, training, activities, deadlines)
  - Helper methods: `GetTimeBasedGreeting()`, `GetMandatoryTrainingStatus()`, `GetRecentActivities()`, `GetUpcomingDeadlines()`, `GetTimeAgo()`
  - Class-level `[Authorize]` attribute (authenticated users only)

- **DashboardHomeViewModel.cs** (65 lines):
  - 12 properties total
  - **Keep (2):** CurrentUser, Greeting
  - **Remove (10):** IdpTotalCount, IdpCompletedCount, IdpProgressPercentage, PendingAssessmentCount, HasUrgentAssessments, MandatoryTrainingStatus, RecentActivities, UpcomingDeadlines, TrainingStatusInfo, RecentActivityItem, DeadlineItem

- **Views/Home/Index.cshtml** (299 lines):
  - Hero section (lines 10-41): greeting, avatar, badges, date — **KEEP**
  - Dashboard glass cards (lines 43-166): IDP, Assessment, Training — **REMOVE**
  - Quick Access cards (lines 169-202): CDP, Assessment, CMP — **RESTRUCTURE**
  - Recent Activity timeline (lines 207-240): — **REMOVE**
  - Upcoming Deadlines (lines 243-298): — **REMOVE**

- **wwwroot/css/home.css** (467 lines, after Phase 148):
  - Hero section styling (lines 23-101) — **KEEP base, pseudo-elements removed by Phase 148**
  - Circular progress (lines 106-143) — **REMOVE (used only by IDP Status card)**
  - Quick Access cards (lines 148-191) — **RESTRUCTURE to Bootstrap**
  - Section headers (lines 197-220) — **REMOVE (not used after removing sections)**
  - Utility classes (lines 225-236) — **REVIEW: gradient-text, badge-gradient may be unused after cleanup**
  - Responsive rules (lines 241-263) — **KEEP for hero, update for new layout**

### CMP/CDP Index Pattern (Reference)
```html
<!-- Bootstrap card pattern from CMP/CDP Index pages -->
<div class="card border-0 shadow-sm h-100">
    <div class="card-body">
        <div class="d-flex align-items-center mb-3">
            <div class="icon-box bg-primary bg-opacity-10 text-primary rounded-3 p-3 me-3">
                <i class="bi bi-grid-3x3-gap fs-3"></i>
            </div>
            <div>
                <h5 class="mb-0">Title</h5>
                <small class="text-muted">Subtitle</small>
            </div>
        </div>
        <p class="text-muted mb-3">Description</p>
        <a href="#" class="btn btn-primary w-100">Action</a>
    </div>
</div>
```

**For Quick Access, simplified (no subtitle/description):**
```html
<a asp-controller="..." asp-action="..." class="card border-0 shadow-sm h-100 text-decoration-none">
    <div class="card-body d-flex flex-column align-items-center">
        <div class="icon-box bg-primary bg-opacity-10 text-primary rounded-3 p-3 mb-3">
            <i class="bi bi-... fs-3"></i>
        </div>
        <h6 class="text-center">CDP</h6>
    </div>
</a>
```

---

## Architecture Patterns

### Recommended View Structure (Phase 149)
```html
@model DashboardHomeViewModel

<!-- Hero Section -->
<div class="hero-section">
    <!-- greeting, name, position, unit, date -->
</div>

<!-- Quick Access Cards -->
<div class="row g-4">
    <!-- CDP Card (Bootstrap pattern) -->
    <!-- Assessment Card (Bootstrap pattern) -->
    <!-- CMP Card (Bootstrap pattern) -->
</div>

<!-- Page ends -->
```

### ViewModel Simplification
```csharp
// AFTER Phase 149
public class DashboardHomeViewModel
{
    public ApplicationUser CurrentUser { get; set; } = null!;
    public string Greeting { get; set; } = string.Empty;
}

// REMOVE:
// - IdpTotalCount, IdpCompletedCount, IdpProgressPercentage
// - PendingAssessmentCount, HasUrgentAssessments
// - MandatoryTrainingStatus
// - RecentActivities, UpcomingDeadlines
// - TrainingStatusInfo, RecentActivityItem, DeadlineItem (helper types)
```

### Controller Simplification
```csharp
// AFTER Phase 149
public async Task<IActionResult> Index()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var viewModel = new DashboardHomeViewModel
    {
        CurrentUser = user,
        Greeting = GetTimeBasedGreeting()
    };

    return View(viewModel);
}

// REMOVE methods:
// - GetMandatoryTrainingStatus()
// - GetRecentActivities()
// - GetUpcomingDeadlines()
// - GetTimeAgo()
// - Also remove: GetTimeBasedGreeting() if not used elsewhere (check Guide)
```

**Note:** `GetTimeBasedGreeting()` is used ONLY by Homepage (checked via grep). Safe to remove after Phase 149, but can be kept if desired for minimal cleanup.

### CSS Cleanup Strategy
```css
/* KEEP (with minor updates) */
.hero-section { ... }           /* Already clean, pseudo-elements removed Phase 148 */
.hero-content { ... }           /* Needed for hero layout */
.hero-avatar { ... }            /* Needed for avatar */
.hero-greeting { ... }          /* Needed for greeting text */
.hero-subtitle { ... }          /* Needed for position/unit badges */
.hero-badge { ... }             /* Needed (no backdrop-filter after Phase 148) */

/* REMOVE (unused after card removal) */
.circular-progress { ... }      /* Only used by IDP Status card */
.circular-progress-text { ... }
.circle-bg { ... }
.circle-progress { ... }
.gradient-text { ... }          /* Only used by Pending Assessment count */
.badge-gradient { ... }         /* May be unused after glassmorphism removal */

/* REMOVE (section-specific) */
.section-header { ... }         /* Section headers for Timeline/Deadlines */
.section-icon { ... }

/* RESTRUCTURE */
.quick-access-card { ... }      /* Refactor to Bootstrap card classes OR keep lightweight for hover effects */
.quick-access-icon { ... }
.quick-access-title { ... }
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Card styling & responsiveness | Custom grid + flexbox for cards | Bootstrap `card`, `border-0`, `shadow-sm`, `g-4` utilities | Already proven in CMP/CDP, built for this use case, responsive out-of-the-box |
| Icon box styling | Custom rounded boxes with gradients | Bootstrap `bg-opacity-10`, `text-primary`, `rounded-3` | Simpler, lighter CSS, consistent with project patterns |
| Circular progress chart | SVG + custom stroke-dasharray for IDP % | Remove entirely — page shows concept only | Unnecessary visual complexity; data not used by new design |
| Activity timeline | Custom CSS timeline with ::before/after markers | Remove entirely — section deleted | Phase 149 eliminates the need entirely |
| Deadline urgency badges | Custom gradient badges | Bootstrap badge utilities | Reduces CSS surface area |

**Key insight:** Phase 149 isn't about replacing complex components with simpler ones — it's about **removing unused sections entirely** and using proven Bootstrap patterns for what remains.

---

## Common Pitfalls

### Pitfall 1: Incomplete ViewModel Property Removal
**What goes wrong:** Remove ViewModel properties but forget to remove Controller assignments → controller tries to set properties that don't exist → NullReferenceException at runtime.

**Why it happens:** Mass-delete ViewModel properties without checking all places they're assigned in Controller.

**How to avoid:**
- For each property marked "REMOVE," search Controller for assignments: `IdpTotalCount =`, `PendingAssessmentCount =`, etc.
- Delete assignment line AND property definition together
- Verify no View `@Model.PropertyName` references remain (grep search)

**Warning signs:**
- "Property does not exist on type DashboardHomeViewModel" compile error
- Runtime exception when Index() tries to assign removed property

**Current status:** Controller lines 42-75 assign all removable properties. All 10 must be deleted.

### Pitfall 2: Missing Bootstrap Classes on Quick Access Cards
**What goes wrong:** Remove custom `.quick-access-card` class and add Bootstrap `card` class, but forget `border-0 shadow-sm h-100` → cards display with visible borders and/or don't match CMP/CDP styling.

**Why it happens:** Add Bootstrap card class without understanding the full pattern (border-0, shadow, height flex).

**How to avoid:**
- Reference CMP/CDP Index.cshtml exact pattern: `<div class="card border-0 shadow-sm h-100">`
- Include ALL utility classes: border-0 (remove default border), shadow-sm (standard shadow), h-100 (full height for grid alignment)
- Use `bg-opacity-10` + `text-primary` for icon boxes (not gradient backgrounds)

**Warning signs:**
- Quick Access cards have visible gray borders
- Card heights don't align in responsive view
- Icons don't match CMP/CDP styling (colorful gradients instead of muted tones)

**Current status:** Current HTML uses inline `style="background: linear-gradient(...)"` on icon divs. MUST replace with Bootstrap `bg-primary bg-opacity-10 text-primary rounded-3`.

### Pitfall 3: Removing Helper Methods but Leaving References
**What goes wrong:** Delete `GetRecentActivities()` method from Controller, but View still tries to render `@foreach (var activity in Model.RecentActivities)` → Model doesn't have property → rendering error.

**Why it happens:** Delete method first, forget to also delete View section using its output.

**How to avoid:**
- Before deleting Controller method, verify View doesn't reference the ViewModel property
- Delete View rendering section FIRST (lines 207-240 for activities, 243-298 for deadlines)
- Then delete Controller method
- Then delete ViewModel property

**Warning signs:**
- View rendering error: "RecentActivities does not exist"
- Compiler error if property removed but View uses it

**Current status:** View references both RecentActivities (line 216) and UpcomingDeadlines (line 251). Must remove both sections.

### Pitfall 4: CSS Rules Not Fully Cleaned Up
**What goes wrong:** Remove `.circular-progress` class from View, but forget to remove CSS rules in home.css → unused CSS rules bloat file and confuse future maintainers.

**Why it happens:** Focus on HTML/View cleanup, forget CSS file has corresponding rules.

**How to avoid:**
- After removing View elements, search home.css for removed class names: `grep "circular-progress\|gradient-text\|section-header" home.css`
- Document line numbers in PLAN.md
- Delete CSS blocks entirely (including @media queries if applicable)

**Warning signs:**
- home.css file size doesn't shrink
- Grep shows removed classes still present in CSS

**Current status:** Phase 148 removed glassmorphism. Phase 149 must remove: circular-progress (lines 106-143), section-header (lines 197-220), gradient-text/badge-gradient usage if present.

### Pitfall 5: Hero Styling Broken by Overly Aggressive CSS Cleanup
**What goes wrong:** Remove `.hero-section` rules thinking they're unused, but Phase 149 needs hero container → hero loses gradient, padding, border-radius → breaks entire page layout.

**Why it happens:** Assume "glassmorphism cleanup" means remove all hero CSS.

**How to avoid:**
- Hero-section BASE rules (background, padding, border-radius, box-shadow) are REQUIRED
- Only pseudo-elements (::before, ::after) and blur filters were removed by Phase 148
- Verify Phase 148 RESEARCH.md: "Only remove: `.hero-section::before/after` pseudo-elements"

**Warning signs:**
- Hero section displays with no background color or padding
- Date/badge positioning breaks

**Current status:** Phase 148 confirmed hero-section base rules are safe and required. Phase 149 keeps them.

### Pitfall 6: Icon Reference Mismatch (Font Awesome vs Bootstrap Icons)
**What goes wrong:** Quick Access cards use Font Awesome icons (`fas fa-calendar-alt`, `fas fa-edit`, `fas fa-book`) but CMP/CDP use Bootstrap icons (`bi bi-grid-3x3-gap`, etc.) → mixed icon sets look inconsistent.

**Why it happens:** Copy CMP/CDP pattern but forget to change icon class names.

**How to avoid:**
- Verify icon library consistency: CMP/CDP use `bi bi-*` (Bootstrap Icons), not `fas fa-*` (Font Awesome)
- Update Quick Access icons to match: `bi bi-calendar` (or similar) instead of `fas fa-calendar-alt`
- Test in browser to ensure icons render correctly

**Warning signs:**
- Quick Access icons don't display (broken icon references)
- Icon style doesn't match other pages (different icon set)

**Current status:** Current Quick Access uses Font Awesome. May need to switch to Bootstrap Icons for consistency (Claude's Discretion on exact choice).

---

## Code Examples

### Example 1: Hero Section (Keep As-Is)
```html
<!-- Source: Views/Home/Index.cshtml lines 9-41 -->
<!-- KEEP THIS SECTION UNCHANGED (after Phase 148 cleanup) -->
<div class="hero-section">
    <div class="hero-content">
        <div class="row align-items-center">
            <div class="col-md-9">
                <div class="d-flex align-items-center gap-4">
                    <div class="hero-avatar">
                        <i class="fas fa-user"></i>
                    </div>
                    <div>
                        <h1 class="hero-greeting mb-2">
                            @Model.Greeting, @Model.CurrentUser.FullName!
                        </h1>
                        <p class="hero-subtitle mb-0">
                            <span class="hero-badge">
                                <i class="fas fa-briefcase"></i>
                                @(Model.CurrentUser.Position ?? "Staff")
                            </span>
                            <span class="hero-badge ms-2">
                                <i class="fas fa-building"></i>
                                @(Model.CurrentUser.Unit ?? "N/A")
                            </span>
                        </p>
                    </div>
                </div>
            </div>
            <div class="col-md-3 text-end">
                <p class="mb-0 fs-5 fw-semibold">@DateTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("id-ID"))</p>
                <p class="mb-0 fs-6 opacity-75">@DateTime.Now.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"))</p>
            </div>
        </div>
    </div>
</div>
```

**What's kept:** Greeting, name, position unit badges (with `hero-badge` styling), date display. All display data comes from `CurrentUser` and `Greeting` (both in simplified ViewModel).

### Example 2: Quick Access Cards (Restructured to Bootstrap)
```html
<!-- Source: Views/Home/Index.cshtml lines 169-202 (RESTRUCTURED) -->
<!-- REPLACE with Bootstrap card pattern matching CMP/CDP Index -->

<div class="row g-4 mb-4">
    <!-- CDP Card -->
    <div class="col-md-4 col-6">
        <a asp-controller="CDP" asp-action="Index" class="card border-0 shadow-sm h-100 text-decoration-none">
            <div class="card-body d-flex flex-column align-items-center justify-content-center">
                <div class="bg-primary bg-opacity-10 text-primary rounded-3 p-3 mb-3">
                    <i class="bi bi-calendar fs-3"></i>
                </div>
                <h6 class="text-center mb-0 text-dark">CDP</h6>
            </div>
        </a>
    </div>

    <!-- Assessment Card -->
    <div class="col-md-4 col-6">
        <a asp-controller="CMP" asp-action="Assessment" class="card border-0 shadow-sm h-100 text-decoration-none">
            <div class="card-body d-flex flex-column align-items-center justify-content-center">
                <div class="bg-success bg-opacity-10 text-success rounded-3 p-3 mb-3">
                    <i class="bi bi-clipboard-check fs-3"></i>
                </div>
                <h6 class="text-center mb-0 text-dark">Assessment</h6>
            </div>
        </a>
    </div>

    <!-- CMP Card -->
    <div class="col-md-4 col-6">
        <a asp-controller="CMP" asp-action="Index" class="card border-0 shadow-sm h-100 text-decoration-none">
            <div class="card-body d-flex flex-column align-items-center justify-content-center">
                <div class="bg-info bg-opacity-10 text-info rounded-3 p-3 mb-3">
                    <i class="bi bi-book fs-3"></i>
                </div>
                <h6 class="text-center mb-0 text-dark">CMP</h6>
            </div>
        </a>
    </div>
</div>
```

**Bootstrap classes used:**
- `card border-0 shadow-sm h-100` — card styling matching CMP/CDP
- `d-flex flex-column align-items-center justify-content-center` — center content
- `bg-{color} bg-opacity-10 text-{color} rounded-3 p-3` — icon boxes (muted, professional)
- `text-decoration-none` — removes link underline

### Example 3: Simplified Controller (After Phase 149)
```csharp
// Source: Controllers/HomeController.cs (SIMPLIFIED)
[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var viewModel = new DashboardHomeViewModel
        {
            CurrentUser = user,
            Greeting = GetTimeBasedGreeting()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Guide() { ... }     // Keep unchanged
    public async Task<IActionResult> GuideDetail(string module) { ... }     // Keep unchanged

    private string GetTimeBasedGreeting()
    {
        var hour = DateTime.Now.Hour;
        return hour < 12 ? "Selamat Pagi"
             : hour < 15 ? "Selamat Siang"
             : hour < 18 ? "Selamat Sore"
             : "Selamat Malam";
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() { ... }     // Keep unchanged

    // REMOVE these methods entirely:
    // - GetMandatoryTrainingStatus()
    // - GetRecentActivities()
    // - GetUpcomingDeadlines()
    // - GetTimeAgo()
}
```

**Removed:** All database queries except user fetch; all complexity is gone.

### Example 4: Simplified ViewModel (After Phase 149)
```csharp
// Source: Models/DashboardHomeViewModel.cs (SIMPLIFIED)
namespace HcPortal.Models
{
    public class DashboardHomeViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public string Greeting { get; set; } = string.Empty;
    }

    // REMOVE these types entirely (if not used elsewhere):
    // - TrainingStatusInfo
    // - RecentActivityItem
    // - DeadlineItem
}
```

**Result:** One simple ViewModel, 6 lines instead of 65.

### Example 5: CSS Cleanup (home.css after Phase 149)
```css
/* KEEP: Hero Section Base Styling */
.hero-section {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-radius: 24px;
    padding: 3rem 2.5rem;
    color: white;
    position: relative;
    overflow: hidden;
    box-shadow: var(--shadow-lg);
    margin-bottom: 2.5rem;
}

.hero-content {
    position: relative;
    z-index: 1;
}

.hero-avatar {
    width: 80px;
    height: 80px;
    background: rgba(255, 255, 255, 0.2);
    border: 3px solid rgba(255, 255, 255, 0.3);
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 2rem;
    font-weight: 700;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
}

.hero-greeting {
    font-size: 2.5rem;
    font-weight: 800;
    margin-bottom: 0.5rem;
    text-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.hero-subtitle {
    font-size: 1.1rem;
    opacity: 0.95;
    font-weight: 400;
}

.hero-badge {
    background: rgba(255, 255, 255, 0.25);
    border: 1px solid rgba(255, 255, 255, 0.3);
    padding: 0.5rem 1rem;
    border-radius: 50px;
    font-weight: 600;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
}

/* Responsive design */
@media (max-width: 768px) {
    .hero-section {
        padding: 2rem 1.5rem;
    }

    .hero-greeting {
        font-size: 1.75rem;
    }
}

/* REMOVE THESE SECTIONS:
   - .circular-progress (lines 106-143)
   - .gradient-text (lines 225-229)
   - .badge-gradient (lines 232-236)
   - .section-header (lines 197-220)
   - .quick-access-card (lines 148-191) if using Bootstrap classes instead
*/
```

---

## State of the Art

### Homepage Evolution: Glassmorphism → Minimalist

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| 3 glassmorphism cards (IDP, Assessment, Training) with circular progress | Hero + 3 simple Quick Access cards | Reduced data fetching, simplified UI, faster page load |
| Recent Activity timeline with custom CSS | Removed entirely | Cleaner design, fewer database queries |
| Upcoming Deadlines with gradient badges | Removed entirely | Reduced visual noise, fewer database queries |
| Custom `.quick-access-card` styling | Bootstrap `card border-0 shadow-sm` pattern | Consistent with CMP/CDP, easier to maintain |
| `GetRecentActivities()`, `GetUpcomingDeadlines()`, `GetMandatoryTrainingStatus()` in Controller | Single `Index()` with one user fetch | Simpler, faster, easier to test |

### Performance Improvements
- **Fewer database queries:** From 6+ (IDP count, completed count, assessment count, activities, deadlines, training) → 1 (user fetch only)
- **Smaller ViewModel:** 65 lines → 6 lines
- **Smaller Controller:** 322 lines → ~50 lines (Index + helper method + error action)
- **Reduced CSS:** ~130 lines removed (circular-progress, timeline, deadline styling)

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UI testing + grep verification |
| Config file | None — this phase is structural refactoring, not feature addition |
| Quick run command | `grep -E "IdpTotalCount\|RecentActivities\|UpcomingDeadlines" Models/DashboardHomeViewModel.cs` (should return 0) |
| Full suite command | Run dev server, navigate to Home/Index, verify: hero displays, Quick Access cards render, no console errors, CMP/CDP pages still work |

### Phase Requirements → Verification Map

| Req ID | Behavior | Verification Type | Automated Check |
|--------|----------|-------------------|-----------------|
| HOME-01 | Homepage does not show glass cards (IDP, Assessment, Training) | Visual inspection + grep | `grep -c "glass-card\|IdpTotalCount\|PendingAssessmentCount" Views/Home/Index.cshtml Models/DashboardHomeViewModel.cs` (should return 0) |
| HOME-02 | Homepage does not show Recent Activity timeline section | Visual inspection + grep | `grep -c "RecentActivities\|timeline-item" Views/Home/Index.cshtml` (should return 0) |
| HOME-03 | Homepage does not show Upcoming Deadlines section | Visual inspection + grep | `grep -c "UpcomingDeadlines\|deadline-card" Views/Home/Index.cshtml` (should return 0) |
| HOME-04 | Controller doesn't fetch unused data | Code review + grep | `grep -c "GetRecentActivities\|GetUpcomingDeadlines\|GetMandatoryTrainingStatus" Controllers/HomeController.cs` (should return 0) |
| HERO-01 | Hero uses clean styling without glassmorphism/pseudo-elements | Visual inspection + grep | Hero section displays with gradient, no blur effects. `grep -c "hero-section::before\|hero-section::after" wwwroot/css/home.css` (should return 0 — Phase 148 already removed) |
| HERO-02 | Hero displays greeting, name, position, unit, date | Visual inspection | Load Home/Index, verify hero shows: "Selamat [Pagi/Siang/Sore/Malam], [Name]" + position badge + unit badge + date |
| QUICK-01 | Quick Access cards use Bootstrap card pattern matching CMP/CDP | Visual inspection + grep | Cards render with `card border-0 shadow-sm` classes, icon boxes use `bg-opacity-10` pattern (not gradient), no colorful gradient backgrounds |

### Sampling Rate
- **Per task commit:** Grep verification: `grep -E "(IdpTotalCount|RecentActivities|glass-card|timeline-item|deadline-card)" Views/Home/Index.cshtml Models/DashboardHomeViewModel.cs Controllers/HomeController.cs` — should return 0 results
- **After code review:** Load dev server, visual inspection of Home/Index page
- **Phase gate:** Full suite: Home/Index displays correctly (hero + Quick Access only), CMP/CDP/Admin pages still work, no console errors

### Wave 0 Gaps
- None — this is pure refactoring/cleanup. No new test infrastructure needed. Existing smoke tests (if any) should still pass.

---

## Open Questions

1. **Quick Access icon library — Font Awesome or Bootstrap Icons?**
   - What we know: Current uses Font Awesome (`fas fa-calendar-alt`, etc.). CMP/CDP Index use Bootstrap Icons (`bi bi-*`)
   - What's unclear: Should Phase 149 switch to Bootstrap Icons for consistency, or keep Font Awesome?
   - Recommendation: **Claude's Discretion.** Either works. Bootstrap Icons preferred for consistency with CMP/CDP (one icon set). Font Awesome is also acceptable (already loaded in _Layout). Choose based on visual preference.

2. **Hover effect on Quick Access cards — keep or remove?**
   - What we know: Current cards have transform and shadow hover effects
   - What's unclear: Does new Bootstrap-based design still need hover effects, or keep them simple?
   - Recommendation: **Claude's Discretion.** Can keep lightweight hover (e.g., `transform: translateY(-2px)`) or remove for minimalist feel. CMP/CDP cards have hover effects in their CSS, so keeping them on Quick Access is acceptable.

3. **ViewModel size — keep DashboardHomeViewModel or merge with simpler model?**
   - What we know: After cleanup, ViewModel has only 2 properties (CurrentUser, Greeting)
   - What's unclear: Is a 2-property ViewModel justified, or should it be merged into a simpler structure?
   - Recommendation: **Claude's Discretion.** Keep DashboardHomeViewModel for consistency (already named, existing pattern). It's still useful as a logical model even if thin. Merging to `HomeViewModel` or using anonymous model is also fine.

---

## Sources

### Primary (HIGH confidence)
- **Controllers/HomeController.cs** (lines 1-322) — Full controller analysis, 6 queryable methods identified
- **Models/DashboardHomeViewModel.cs** (lines 1-65) — Complete ViewModel inspection, 10 removable properties identified
- **Views/Home/Index.cshtml** (lines 1-299) — Full view inspection, sections to remove identified
- **wwwroot/css/home.css** (467 lines, after Phase 148) — CSS architecture, cleanup targets identified
- **Views/CMP/Index.cshtml** (lines 18-34) — Bootstrap card pattern reference (`card border-0 shadow-sm h-100`)
- **Views/CDP/Index.cshtml** (lines 18-34) — Bootstrap card pattern reference
- **149-CONTEXT.md** (2026-03-10) — User decisions and locked constraints
- **REQUIREMENTS.md** (2026-03-10) — Phase requirement definitions (HOME-01 through QUICK-01)
- **148-RESEARCH.md** (2026-03-10) — Phase 148 context, CSS cleanup verification

### Secondary (MEDIUM confidence)
- Pattern matching: CMP/CDP card styling used as reference for Quick Access redesign
- Grep verification: RecentActivityItem, DeadlineItem, TrainingStatusInfo only found in HomeController and DashboardHomeViewModel (safe to remove)

---

## Metadata

**Confidence breakdown:**
- **Standard Stack:** HIGH — ASP.NET Core MVC, Entity Framework, Bootstrap are explicitly used in codebase
- **Architecture Patterns:** HIGH — Code inspection is definitive; files are small and focused
- **Pitfalls:** HIGH — Specific line numbers and removal targets documented with grep verification
- **Validation:** MEDIUM — Visual smoke testing sufficient; no formal unit test framework detected in project

**Research date:** 2026-03-10
**Valid until:** 2026-03-31 (30 days — ASP.NET Core patterns are stable, no framework updates expected)

**Known assumptions:**
- Bootstrap 5.x is active in project (verified in CMP/CDP views)
- Entity Framework Core is used for data access (confirmed in HomeController)
- Font Awesome icons available in _Layout (can switch to Bootstrap Icons if needed)
- UserManager<ApplicationUser> follows standard ASP.NET Core Identity (confirmed in controller)
- AOS library remains in _Layout for Guide pages, not needed for Homepage (verified Phase 148)

---

## File Locations

**Primary research sources:**
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Controllers/HomeController.cs` (322 lines)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Models/DashboardHomeViewModel.cs` (65 lines)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Views/Home/Index.cshtml` (299 lines)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/wwwroot/css/home.css` (467 lines after Phase 148)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Views/CMP/Index.cshtml` (120 lines, pattern reference)
- `/c/Users/Administrator/Desktop/PortalHC_KPB/Views/CDP/Index.cshtml` (127 lines, pattern reference)

**Phase output location:**
- `.planning/phases/149-homepage-view-controller-redesign/149-RESEARCH.md`
