# Phase 149: Homepage View & Controller Redesign - Context

**Gathered:** 2026-03-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Replace the current Homepage with a clean hero greeting + Quick Access cards only. Remove glass cards (IDP Status, Pending Assessment, Mandatory Training), Recent Activity timeline, and Upcoming Deadlines section from both View and Controller. Clean up unused CSS, ViewModel properties, and controller queries.

</domain>

<decisions>
## Implementation Decisions

### Hero Section Styling
- Keep gradient hero as-is (purple gradient background, avatar circle, badge pills, date)
- Phase 148 already removed pseudo-element decorations — no further CSS changes needed for hero
- HERO-01 satisfied by keeping clean gradient without glassmorphism pseudo-elements

### Quick Access Cards
- Keep 3 cards, rename to match destinations: CDP, Assessment, CMP
- Switch to Bootstrap `card border-0 shadow-sm` pattern (matching CMP/CDP Index cards)
- Uniform Bootstrap-toned icons (no colorful gradient icon boxes)
- Icon + name only — no subtitles or descriptions
- Links stay: /CDP/Index, /CMP/Assessment, /CMP/Index

### ViewModel & Controller Cleanup
- Full cleanup: remove all unused properties from DashboardHomeViewModel
- Remove: IdpTotalCount, IdpCompletedCount, IdpProgressPercentage, PendingAssessmentCount, HasUrgentAssessments, MandatoryTrainingStatus, RecentActivities, UpcomingDeadlines
- Remove controller methods: GetRecentActivities, GetUpcomingDeadlines, GetMandatoryTrainingStatus
- Remove related helper types if only used by Homepage: RecentActivityItem, DeadlineItem, TrainingStatusInfo
- Keep: CurrentUser, Greeting (still used by hero)

### CSS Cleanup
- Remove all unused CSS rules: circular-progress, gradient-text, badge-gradient, section-header, hero-stats
- Keep only: hero-section rules, quick-access rules (restyled to Bootstrap pattern), responsive rules for hero
- Quick Access cards should use Bootstrap classes directly, reducing need for custom CSS

### Page Layout
- Full width layout, same container width as current
- Page ends cleanly after Quick Access section — no empty space fillers

### Claude's Discretion
- Exact Bootstrap icon color/shade for Quick Access cards
- Whether to keep or simplify quick-access-card hover effects
- How to handle the ViewModel class if it becomes too thin (merge into simpler model or keep)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- CMP/CDP Index pattern: `card border-0 shadow-sm h-100` + `card-body` — use this exact pattern for Quick Access
- Hero section CSS already cleaned (Phase 148) — base rule intact at home.css lines 23-32

### Established Patterns
- Bootstrap card pattern used consistently across CMP/CDP Index pages
- HomeController uses UserManager + DbContext pattern for data fetching
- DashboardHomeViewModel is the single ViewModel for Homepage

### Integration Points
- Views/Home/Index.cshtml — main file to rewrite
- Controllers/HomeController.cs — Index action to simplify
- Models/DashboardHomeViewModel.cs — trim unused properties
- wwwroot/css/home.css — remove unused rules
- Models for RecentActivityItem, DeadlineItem, TrainingStatusInfo — check if used elsewhere before deleting

</code_context>

<specifics>
## Specific Ideas

- Quick Access card names should literally be "CDP", "Assessment", "CMP" — matching the destination pages
- Icon style should match the muted/professional look of CMP/CDP Index cards (not colorful gradients)

</specifics>

<deferred>
## Deferred Ideas

- Role-based Quick Access differentiation — future milestone
- Personalized shortcut reordering — future milestone

</deferred>

---

*Phase: 149-homepage-view-controller-redesign*
*Context gathered: 2026-03-10*
