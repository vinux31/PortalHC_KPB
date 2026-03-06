# Phase 105: User Guide Structure & Content - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Build a comprehensive User Guide page at `/Home/Guide` with tab-based navigation covering all 5 portal modules (Dashboard, CMP, CDP, Account, Admin Panel). Content includes step-by-step instructions with numbered step cards, FAQ section with accordion behavior, and role-based access control. Styling and animations are Phase 106.
</domain>

<decisions>
## Implementation Decisions

### Tab Content Structure
- **5 tabs total**: Dashboard, CMP, CDP, Account, Admin Panel (Admin/HC only)
- **Content depth**: 8-12 detailed steps per tab - covers main workflows without being overwhelming
- **Step granularity**: Each step is one clear action (e.g., "Click tombol 'Mulai Ujian'", "Upload file evidence")
- **Scope within boundary**: Each tab covers only what users can SEE/DO in that module. Implementation details (Phase 106 styling, backend logic) are out of scope.

### Dashboard Tab Content
- Login flow (quick reference)
- View personal greeting and role badge
- Understand 4 dashboard cards (IDP Progress, Pending Assessments, Mandatory Training, Recent Activities)
- Check upcoming deadlines
- Navigate to other modules

### CMP Tab Content
- Access Assessment page
- View available/open assessments
- Start exam and answer questions
- Submit and view results
- View competency score
- Check training history

### CDP Tab Content
- Access CDP page
- View IDP items
- Open coaching progress
- Track deliverable status
- Submit evidence with coaching report
- View approval workflow (SrSpv → SH → HC)

### Account Tab Content
- View profile (Nama, NIP, Email, Position, Role)
- Change password
- Edit full name and position
- Understand role badge display

### Admin Panel Tab Content (Admin/HC only)
- Access Kelola Data hub
- Manage Workers (CRUD, import, export)
- Manage Assessments (create, edit, monitoring)
- View KKJ Matrix and CPDP items
- Access Proton Data (Silabus, Coaching Guidance, Deliverable Override)

### Instruction Format
- **Hero section** at top with gradient background matching dashboard style ("Panduan Pengguna" title, subtitle)
- **Bootstrap 5 Tabs** for 5-module navigation (click switches content without page refresh)
- **Step cards** per instruction:
  - Icon (Bootstrap Icons) on left (bi-arrow-right-circle, bi-file-earmark-check, etc.)
  - Gradient badge number (1, 2, 3...) using --gradient-primary
  - Bold title (what the step is)
  - Description (how to do it)
- **Bootstrap Cards** with shadow-md for grouping related steps
- **Alert boxes** (bootstrap alert-info) for tips/catatan penting

### FAQ Content Scope
- **12 comprehensive FAQs** covering common questions:
  1. Bagaimana cara login ke portal?
  2. Lupa password, apa yang harus dilakukan?
  3. Apa bedanya CMP dan CDP?
  4. Bagaimana cara mengikuti assessment?
  5. Bagaimana cara upload evidence untuk deliverable?
  6. Bagaimana alur approval coaching session?
  7. Bagaimana melihat progress coaching proton?
  8. Apa yang harus dilakukan jika assessment tidak bisa dibuka?
  9. Bagaimana cara mengubah profil?
  10. Bagaimana HC memantau progress pekerja?
  11. Apa itu KKJ Matrix?
  12. Bagaimana cara mencapai kompetensi tertinggi?
- **Accordion behavior** using Bootstrap collapse (one open at a time or multiple allowed)

### Controller Structure
- **Add to existing HomeController**: Create `Guide()` action method
- **Route**: `/Home/Guide` (follows existing pattern)
- **View**: `Views/Home/Guide.cshtml`
- **Authorization**: Uses `[Authorize]` class-level attribute from HomeController (non-logged users auto-redirect to login)
- **No backend logic needed**: Static content only, no database queries
- **Role-based tab visibility**: Use `@if (User.IsInRole("Admin") || User.IsInRole("HC"))` in view to hide Admin Panel tab

### Navbar Integration
- Add "Panduan" link to navbar after "CDP" link, before "Kelola Data"
- Icon: `<i class="bi bi-question-circle-fill me-1"></i>` or `<i class="fa-solid fa-circle-question me-1"></i>`
- Position: In main nav, visible to all authenticated users
- Follows existing pattern: `<a class="nav-link text-dark" asp-controller="Home" asp-action="Guide">`

### Role Indicator Badge
- Display user's current role at top of Guide page (below hero, before tabs)
- Format: `<span class="badge bg-primary">Admin</span>` or similar
- Help text: "Masuk sebagai: [Role]"

### Content Language
- All content in **Indonesian language**
- Technical terms kept in English (Dashboard, CMP, CDP, Assessment, IDP, KKJ Matrix)
- Consistent terminology with existing portal UI

### Claude's Discretion
- Exact wording for each step description (as long as it's clear and in Indonesian)
- Which Bootstrap Icons to use for each step
- Exact placement of tips/catatan within content flow
- Whether FAQ allows multiple open accordions or one-at-a-time
- Hero section subtitle text

</decisions>

<specifics>
## Specific Ideas

- "Step cards should look similar to dashboard cards - same shadow, same gradient badge style"
- "FAQ should use the same accordion pattern we use elsewhere in portal"
- "Tips should use alert-info box with icon bi-lightbulb"
- "Number badges should use --gradient-primary CSS variable"

## Content References

All content should reference actual existing portal features:
- "Dashboard" refers to /Home/Index with 4 cards
- "CMP" refers to /CMP/Index with assessment list
- "CDP" refers to /CDP/Index with IDP items
- "Account" refers to /Account/Profile and /Account/Settings
- "Admin Panel" refers to /Admin/Index with 3 sections

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- **HomeController** (Controllers/HomeController.cs): Has `[Authorize]` class-level attribute, add `Guide()` method following `Index()` pattern
- **_Layout.cshtml** (Views/Shared/_Layout.cshtml):
  - Lines 37-117: Navbar structure - add "Panduan" link after line 62 (CDP link)
  - Uses `@if (User.IsInRole("Admin") || User.IsInRole("HC"))` pattern (line 64) for role-based nav
  - Bootstrap Icons already loaded (line 22)
  - Font Awesome already loaded (line 23)
- **home.css** (wwwroot/css/home.css):
  - CSS variables available: --gradient-primary, --shadow-sm/md/lg/hover (lines 5-13)
  - Inter font family loaded (line 17)
  - Glassmorphism card styles exist (can reuse)
- **AOS Library** (line 25 in _Layout): Already loaded `<link href="https://unpkg.com/aos@2.3.1/dist/aos.css" rel="stylesheet">`

### Established Patterns
- **Controller actions**: Public async Task<IActionResult> ActionName() → return View(viewModel)
- **Role-based UI**: `@if (User.IsInRole("Admin") || User.IsInRole("HC")) { ... }` pattern in views
- **Bootstrap 5.3.0**: Already loaded via CDN (line 21 in _Layout)
- **Bootstrap Icons**: Use `<i class="bi bi-icon-name"></i>` syntax
- **Alert boxes**: `<div class="alert alert-info"><i class="bi bi-info-circle me-2"></i><strong>Info:</strong> message</div>` (lines 121-149 in _Layout)

### Integration Points
- **Navbar**: Add "Panduan" link in `<ul class="navbar-nav me-auto">` section (after CDP link, around line 62)
- **HomeController**: Add new `public IActionResult Guide()` method (no async needed, no DB queries)
- **View**: Create `Views/Home/Guide.cshtml` - follows same structure as `Views/Home/Index.cshtml`
- **Role check**: Use `@User.IsInRole()` in view to conditionally render Admin Panel tab
- **CSS**: Can extend `home.css` or create `guide.css` (import home.css variables)

### View Structure Pattern (from Index.cshtml)
```cshtml
@{
    ViewData["Title"] = "Panduan Pengguna";
}
<div class="container-fluid px-4">
    <!-- Hero section -->
    <div class="hero-section">
        <!-- Content -->
    </div>
    <!-- Tabs and content -->
</div>
```

</code_context>

<deferred>
## Deferred Ideas

- Video tutorials for each module — defer to v2+ (GUIDE-INTER-01)
- Interactive walkthrough tours — defer to v2+ (GUIDE-INTER-02)
- Search functionality — defer to v2+ (GUIDE-INTER-03)
- Screenshots with annotations — defer to v2+ (GUIDE-INTER-04)
- Context-sensitive help from each page — defer to v2+ (GUIDE-INTER-05)
- Admin/HC-specific detailed guides — defer to v2+ (GUIDE-ADV-01)
- Printable PDF versions — defer to v2+ (GUIDE-ADV-02)
- Troubleshooting guides — defer to v2+ (GUIDE-ADV-03)
- Best practices per role — defer to v2+ (GUIDE-ADV-04)

</deferred>

---

*Phase: 105-user-guide-structure-content*
*Context gathered: 2026-03-06*
