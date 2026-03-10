# Architecture Patterns — Homepage Minimalist Redesign

**Project:** Portal HC KPB v3.18
**Researched:** 2026-03-10
**Milestone:** Homepage Minimalist Redesign (v3.18)

## Current Architecture Overview

The homepage currently follows a **premium/glassmorphism design pattern** that contrasts sharply with the rest of the application (CMP, CDP, Admin pages use clean Bootstrap card design). The redesign goal is alignment: move homepage to the same simple, professional card-based design language used throughout the portal.

### Design Inconsistency Gap

| Aspect | Homepage (Current) | CMP/CDP Pages | Target |
|--------|-------------------|---------------|--------|
| Card style | `.glass-card` (glassmorphism, blurred bg) | `.card.border-0.shadow-sm` (Bootstrap native) | Align to CMP/CDP |
| Color scheme | Gradient backgrounds + backdrop-filter | Solid white + icon-box bg-opacity-10 | Simplify to solid |
| Animation | AOS (Animate On Scroll) via data-aos | No animations | Remove from homepage |
| Typography | Premium spacing, shadows, effects | Standard Bootstrap spacing | Simplify |
| Hero section | Large gradient + pseudo-elements | Not present elsewhere | Reduce decoration |

## Recommended Architecture for v3.18

### 1. Hero Section Simplification

**Current:** Large gradient background, pseudo-element blur circles, excess styling

**Target:** Clean, simple header with user greeting

```html
<!-- CURRENT (bloated) -->
<div class="hero-section" data-aos="fade-down">
  <div class="hero-content">
    <h1 class="hero-greeting">Selamat Pagi, [Name]</h1>
    <!-- pseudos: ::before blur circles, ::after more blur circles -->
  </div>
</div>

<!-- TARGET (minimal) -->
<div class="container py-4 mb-4">
  <h2 class="mb-1">
    <i class="bi bi-house-door me-2"></i>Dashboard
  </h2>
  <p class="text-muted">Selamat @Model.Greeting, @Model.CurrentUser.FullName</p>
</div>
```

**CSS to Remove:**
- `.hero-section` (entire rule block)
- `.hero-avatar`, `.hero-greeting`, `.hero-subtitle`, `.hero-badge`, `.hero-stats`
- `.hero-section::before`, `.hero-section::after` (pseudo-element blur circles)

**Lines affected:** ~127 lines (23-149 in home.css)

### 2. Dashboard Cards Transformation

**Current:** Three glass cards with individual styling (IDP Status, Pending Assessment, Mandatory Training)

**Target:** Standard Bootstrap cards with icon-box pattern (matches CMP/CDP)

```html
<!-- CURRENT (glassmorphism) -->
<div class="glass-card card-primary h-100">
  <div class="card-icon-wrapper icon-primary">
    <i class="fas fa-chart-line"></i>
  </div>
  <h5 class="card-title fw-bold mb-3">My IDP Status</h5>
  <!-- circular progress SVG with gradient -->
</div>

<!-- TARGET (Bootstrap simple) -->
<div class="card border-0 shadow-sm h-100">
  <div class="card-body">
    <div class="d-flex align-items-center mb-3">
      <div class="icon-box bg-primary bg-opacity-10 text-primary rounded-3 p-3 me-3">
        <i class="bi bi-graph-up-arrow fs-3"></i>
      </div>
      <div>
        <h5 class="mb-0">IDP Status</h5>
        <small class="text-muted">Progress</small>
      </div>
    </div>
    <p class="text-muted mb-3">[Summary of progress]</p>
    <a href="#" class="btn btn-primary w-100">View Details</a>
  </div>
</div>
```

**CSS to Remove:**
- `.glass-card` (entire rule block including hover states)
- `.glass-card.card-primary`, `.glass-card.card-warning`, `.glass-card.card-success`
- `.glass-card::before` (gradient top border on hover)
- `.glass-card:hover` (translate/shadow effects)
- `.card-icon-wrapper`, `.card-icon-wrapper.icon-primary`, etc.

**Lines affected:** ~85 lines (131-215 in home.css)

### 3. Quick Access Section

**Current:** Uses custom `.quick-access-card` with animation, gradient icons

**Target:** Keep the concept but use Bootstrap cards + AOS removal

**Note:** Quick Access section is intentionally KEPT in simplified form because it's highly functional (3 quick links to main features). Only remove animations and glassmorphism styling.

```html
<!-- KEEP structure but simplify CSS -->
<div class="row g-4 mb-5">
  <div class="col-md-4 col-6">  <!-- Remove data-aos -->
    <a asp-controller="CDP" asp-action="Index" class="card border-0 shadow-sm h-100 text-decoration-none">
      <div class="card-body text-center">
        <div class="icon-box bg-primary bg-opacity-10 text-primary rounded-3 p-3 mx-auto mb-3" style="width:fit-content;">
          <i class="bi bi-calendar-alt fs-3"></i>
        </div>
        <h6 class="card-title">My IDP</h6>
      </div>
    </a>
  </div>
</div>
```

**CSS to Remove:**
- `.quick-access-card` (replace with standard `.card` from Bootstrap)
- `.quick-access-icon` (replace with `.icon-box` pattern)
- `.quick-access-card:hover` transforms and scales
- `.quick-access-card:hover .quick-access-icon` animation

**CSS to Keep:**
- The card link pattern itself (good UX)
- The 3-column layout on desktop, 2-column on mobile

**Lines affected:** ~45 lines (261-305 in home.css)

### 4. Timeline & Deadline Sections — REMOVE

**Current:** "Recent Activity Timeline" and "Upcoming Deadlines" sections with custom styling

**Target:** REMOVE entirely

**Rationale:**
- Data fetching adds complexity to HomeController
- These sections are often empty for new users
- Information is already available in CMP/CDP dashboards
- Simplification goal = less content on homepage

**Code Impact:**

*HomeController.cs:*
- Remove method: `GetRecentActivities()` (~60 lines)
- Remove method: `GetUpcomingDeadlines()` (~75 lines)
- Remove property setup in `Index()` action (lines 60-63)
- Remove urgency check logic (lines 71-75)

*DashboardHomeViewModel.cs:*
- Remove property: `RecentActivities`
- Remove property: `UpcomingDeadlines`

*Views/Home/Index.cshtml:*
- Remove: Recent Activity Timeline section (lines 204-240)
- Remove: Upcoming Deadlines section (lines 242-299)
- Remove: data-aos attributes from timeline/deadline markup

*home.css:*
- Remove: `.timeline` and all `.timeline-*` classes (~65 lines)
- Remove: `.deadline-card` and all `.deadline-*` classes (~65 lines)
- Remove: `.section-header` (used only by removed sections)
- Remove: `.section-icon` (used only by removed sections)

**Lines affected:** ~130 lines (309-442 in home.css) + controller methods

### 5. AOS Animation Library

**Decision:** REMOVE from layout for homepage only, keep globally available

**Rationale:**
- Guide.cshtml still uses AOS extensively (data-aos on guide cards, breadcrumbs, hero)
- Other pages may use it in the future
- AOS has minimal performance impact (24KB minified, loaded once globally)
- Removing from `_Layout.cshtml` would break Guide page

**Implementation:**
- Keep `<script src="https://unpkg.com/aos@2.3.1/dist/aos.js">` in `_Layout.cshtml`
- Remove ALL `data-aos` attributes from `Views/Home/Index.cshtml`
- Remove AOS initialization code from home.css or skip initialization if no data-aos elements exist

**No code removal needed** — just remove HTML attributes (data-aos, data-aos-delay)

### 6. CSS Cleanup

**home.css file structure after cleanup:**

```
KEEP:
- :root variables (gradients, shadows) — LOW impact, useful for future
- Quick Access cards styling (simplified)
- Button styles
- Responsive design rules

REMOVE:
- Hero section (entire ~27 rule block)
- Glassmorphism cards (glass-card, glass-card.card-*, .card-icon-wrapper)
- Timeline and deadline components
- Section header/icon styling (only used by removed sections)
- Premium shadows and animation utilities
- Pseudo-element blur effects

EXPECTED SIZE REDUCTION:
- Current: 11,622 bytes (513 lines)
- Target: ~4,000 bytes (180 lines) — 65% reduction
```

**Lines removed:**
- Hero: 127 lines (23-149)
- Glassmorphism: 85 lines (131-215)
- Circular progress: 38 lines (219-256)
- Quick Access: 45 lines (261-305)
- Timeline: 65 lines (309-373)
- Deadlines: 65 lines (378-442)
- **Total: ~425 lines** (but some utility spacing survives)

## Integration Points with Existing Architecture

### 1. View Model (DashboardHomeViewModel.cs)

**SAFE TO REMOVE:**
```csharp
public List<RecentActivityItem> RecentActivities { get; set; } = new();
public List<DeadlineItem> UpcomingDeadlines { get; set; } = new();
```

**KEEP (still needed for Quick Access + info display):**
```csharp
public ApplicationUser CurrentUser { get; set; } = null!;
public string Greeting { get; set; } = string.Empty;
public int IdpTotalCount { get; set; }
public int IdpCompletedCount { get; set; }
public int IdpProgressPercentage { get; set; }
public int PendingAssessmentCount { get; set; }
public bool HasUrgentAssessments { get; set; }
public TrainingStatusInfo MandatoryTrainingStatus { get; set; } = new();
```

**Note:** With dashboard cards removed, some ViewModel properties (IdpProgressPercentage, MandatoryTrainingStatus, etc.) become unused. Can remove in a future cleanup phase, but safe to leave for now.

### 2. HomeController.cs

**SAFE TO REMOVE (complex LINQ logic):**
- `GetRecentActivities()` — fetches AssessmentSessions, IdpItems, CoachingLogs
- `GetUpcomingDeadlines()` — calculates deadlines, urgency, date formatting
- Related setup in `Index()` action (lines 60-63, 71-75)

**KEEP (minimal logic):**
- `Index()` action setup for greeting, basic counts (can optimize later)
- `Guide()` and `GuideDetail()` actions (unchanged)
- Helper: `GetTimeBasedGreeting()` (still needed)
- `Error()` action (unchanged)

**Performance benefit:** Removes 4 database queries from homepage load:
1. AssessmentSessions query (up to 2 recent)
2. IdpItems query (up to 2 recent)
3. CoachingLogs query (up to 2 recent)
4. TrainingRecords query for urgency check

### 3. _Layout.cshtml (Shared)

**NO CHANGES NEEDED** — AOS stays in layout (used by Guide.cshtml). Removing data-aos attributes from Index.cshtml is sufficient to prevent unnecessary initialization.

**Option to optimize later:** Add conditional check in AOS initialization:
```javascript
// Only init AOS if page has data-aos elements
if (document.querySelector('[data-aos]')) {
  AOS.init({ ... });
}
```

### 4. Navigation & Routing

**NO IMPACT** — All links (CDP, CMP, Admin) still work. Quick Access buttons remain functional.

## Data Flow — Before vs After

### BEFORE (Current)
```
HomeController.Index()
  ├─ Get user + roles
  ├─ Calculate IDP stats (DB query 1)
  ├─ Count pending assessments (DB query 2)
  ├─ Get mandatory training status (DB query 3)
  ├─ GetRecentActivities()    (DB queries 4-6)  ← REMOVE
  ├─ GetUpcomingDeadlines()   (DB queries 7-9)  ← REMOVE
  └─ GetUrgentAssessments()   (DB query 10)     ← REMOVE

  → DashboardHomeViewModel
    → Index.cshtml renders:
       - Hero section
       - 3 Glass cards (IDP, Assessment, Training)
       - Quick Access (3 links)
       - Timeline section (from RecentActivities)
       - Deadlines section (from UpcomingDeadlines)
       - All with AOS animations
```

### AFTER (Simplified)
```
HomeController.Index()
  ├─ Get user + roles
  ├─ Calculate IDP stats (DB query 1)
  ├─ Count pending assessments (DB query 2)
  ├─ Get mandatory training status (DB query 3)

  → DashboardHomeViewModel (simpler)
    → Index.cshtml renders:
       - Simple header greeting
       - Quick Access (3 links) — cleaned up styling
       - REMOVED: 3 glass cards
       - REMOVED: Timeline section
       - REMOVED: Deadlines section
       - REMOVED: All AOS animations
```

**Database query reduction:** 10 → 3 queries

## Component Boundaries & Dependencies

### Current Components

| Component | Type | Dependencies | Status |
|-----------|------|--------------|--------|
| Hero Section | View + CSS | home.css only | Remove |
| Dashboard Cards (3) | View + CSS + ViewModel | home.css + IDP/Assessment/Training data | Remove or simplify |
| Quick Access | View + CSS | home.css + route links | Keep (simplify CSS) |
| Timeline | View + CSS + Controller method | home.css + GetRecentActivities() | Remove |
| Deadlines | View + CSS + Controller method | home.css + GetUpcomingDeadlines() | Remove |
| AOS Library | JavaScript | Global _Layout.cshtml | Keep in layout, remove from Index.cshtml |

### New Components (Post-v3.18)

None — this is a removal/simplification phase, not a feature addition.

## Build Order & Dependencies

### Phase 1: CSS Cleanup (No-risk)
1. Create backup of `home.css`
2. Remove hero section styling (lines 23-149)
3. Remove glassmorphism card styling (lines 131-215)
4. Remove timeline/deadline styling (lines 309-442)
5. Simplify `.quick-access-card` to use Bootstrap `.card` pattern
6. Remove `.section-header`, `.section-icon` rules
7. Test: Homepage should still load without JS errors

### Phase 2: View Markup Removal (Straightforward)
1. Remove `<div class="row g-4">` for dashboard cards (lines 44-166 in Index.cshtml)
2. Remove Recent Activity section (lines 206-240)
3. Remove Upcoming Deadlines section (lines 242-299)
4. Remove ALL `data-aos` attributes from remaining Index.cshtml markup
5. Replace hero section with simple header (5 lines)
6. Test in browser: Verify Quick Access still works

### Phase 3: Controller & ViewModel Cleanup (Requires Testing)
1. Remove `GetRecentActivities()` method from HomeController
2. Remove `GetUpcomingDeadlines()` method from HomeController
3. Remove collection setup in `Index()` action (lines 60-63, 71-75)
4. Remove properties from DashboardHomeViewModel:
   - `RecentActivities`
   - `UpcomingDeadlines`
5. Remove properties from DashboardHomeViewModel (safe now):
   - `IdpTotalCount`, `IdpCompletedCount`, `IdpProgressPercentage` (if dashboard cards removed)
   - `PendingAssessmentCount`, `HasUrgentAssessments` (if dashboard cards removed)
6. Test: Homepage still loads, no runtime errors

### Phase 4: Cross-Page Testing
1. Verify Quick Access links work (CDP, CMP, Assessment)
2. Verify Guide page still animates with AOS
3. Verify no console errors
4. Check responsive design on mobile
5. Performance test: Measure load time improvement (should be ~50-100ms faster)

## Key Decisions & Rationale

### Why Remove Timeline & Deadlines?
- **Duplication:** Same information in CMP/CDP dashboards
- **Complexity:** Requires 3-4 database queries per page load
- **Often empty:** New users see empty state frequently
- **Goal alignment:** Minimize homepage, maximize performance

### Why Keep Quick Access?
- **Utility:** Users commonly access 3 main features from homepage
- **Performance:** No data fetching needed (just route links)
- **Simplicity:** Single CSS rule block to maintain

### Why Keep AOS Globally?
- **Used elsewhere:** Guide.cshtml heavily uses AOS animations
- **Cost:** Minimal (24KB JS + CSS bundle)
- **Future-proof:** Other pages may adopt animations later
- **Risk:** Removing from layout might break existing Guide functionality

### Why NOT Simplify Dashboard Cards?
- **Data needed:** Still showing IDP progress, pending assessments, training status
- **User expectation:** Users want quick status overview on homepage
- **Decision:** Convert to Bootstrap cards instead of full removal
  - **OR:** Remove cards entirely, keep only Quick Access
  - *Recommended: Keep but simplify to match CMP/CDP style* (current plan)

## Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Breaking Guide page AOS | High | Test Guide.cshtml before/after. Keep AOS in layout, only remove from Index.cshtml |
| Database connection issues during removal | Medium | Controller refactoring should be done carefully. Unit test the simplified Index() |
| Responsive design breaks on mobile | Medium | Test Quick Access cards on viewport sizes 320px, 768px, 1024px |
| Unused ViewModel properties | Low | Mark as [Obsolete] first phase, remove in cleanup phase |
| CSS specificity conflicts | Low | new `.card` styles use Bootstrap native classes (no conflicts) |

## Files Affected Summary

| File | Type | Changes | Lines |
|------|------|---------|-------|
| `wwwroot/css/home.css` | CSS | Remove: hero, cards, timeline, deadlines | -425 |
| `Views/Home/Index.cshtml` | View | Remove: markup, data-aos attrs | -250 |
| `Controllers/HomeController.cs` | Controller | Remove: GetRecentActivities(), GetUpcomingDeadlines() | -135 |
| `Models/DashboardHomeViewModel.cs` | Model | Remove: 2 properties | -4 |
| `Views/Shared/_Layout.cshtml` | Layout | No changes | 0 |

**Total lines affected:** ~814 lines removed

## Sources

- CMP Index.cshtml (Bootstrap card design reference)
- CDP Index.cshtml (icon-box pattern reference)
- Current Index.cshtml and home.css (existing implementation)
- HomeController.cs (data fetching logic)
- DashboardHomeViewModel.cs (view model structure)
- _Layout.cshtml (AOS initialization, global scripts)
