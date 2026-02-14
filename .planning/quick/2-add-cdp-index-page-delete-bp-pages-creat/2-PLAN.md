---
phase: quick-2
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CDP/Index.cshtml
  - Views/Shared/_Layout.cshtml
  - Views/BP/Index.cshtml
  - Views/BP/Simulation.cshtml
  - Views/BP/Historical.cshtml
  - Views/BP/EligibilityValidator.cshtml
  - Views/BP/TalentProfile.cshtml
  - Views/BP/PointSystem.cshtml
  - Controllers/BPController.cs
autonomous: true
must_haves:
  truths:
    - "CDP navbar link goes directly to CDP/Index without dropdown"
    - "CDP/Index shows card-based hub with links to all CDP features (Plan IDP, Coaching, Progress, Dashboard)"
    - "BP navbar link goes directly to BP/Index without dropdown"
    - "BP/Index shows 'under development' placeholder page"
    - "All old BP feature pages (Simulation, Historical, EligibilityValidator, TalentProfile, PointSystem) are deleted"
    - "BPController only has Index action returning simple view"
  artifacts:
    - path: "Views/CDP/Index.cshtml"
      provides: "CDP hub page with feature cards"
      contains: "Career Development Portal"
    - path: "Views/BP/Index.cshtml"
      provides: "Under development placeholder"
      contains: "under development"
    - path: "Views/Shared/_Layout.cshtml"
      provides: "Updated navbar with direct links for CDP and BP"
    - path: "Controllers/BPController.cs"
      provides: "Simplified controller with only Index action"
  key_links:
    - from: "Views/Shared/_Layout.cshtml"
      to: "CDP/Index"
      via: "direct nav-link (no dropdown)"
      pattern: "asp-controller=\"CDP\" asp-action=\"Index\""
    - from: "Views/Shared/_Layout.cshtml"
      to: "BP/Index"
      via: "direct nav-link (no dropdown)"
      pattern: "asp-controller=\"BP\" asp-action=\"Index\""
---

<objective>
Add a CDP/Index hub page (card-based layout like CMP/Index but without the section dropdown), delete all BP feature pages, and replace BP/Index with an "under development" placeholder. Update navbar to use direct links (no dropdowns) for both CDP and BP.

Purpose: CDP needs a proper landing/hub page like CMP has. BP features are placeholder/dummy data and should be hidden behind an "under development" page until they are properly built.
Output: CDP hub page, simplified BP placeholder, cleaned-up navbar
</objective>

<context>
@Views/CMP/Index.cshtml (reference: card layout pattern to follow for CDP)
@Views/CDP/Index.cshtml (current: IDP Proton PDF viewer page to be replaced)
@Views/Shared/_Layout.cshtml (navbar with dropdown menus to simplify)
@Controllers/BPController.cs (current: has TalentProfile, PointSystem, EligibilityValidator actions to remove)
@Controllers/CDPController.cs (current: Index action serves PDF viewer, keep action but view changes)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create CDP/Index hub page and update navbar for CDP and BP</name>
  <files>
    Views/CDP/Index.cshtml
    Views/Shared/_Layout.cshtml
  </files>
  <action>
**CDP/Index.cshtml** - Replace the entire file content with a card-based hub page modeled after CMP/Index.cshtml. The page should:

- Title: "CDP - Career Development Portal"
- Header: icon `bi-person-workspace`, title "Career Development Portal", subtitle "Manage individual development plans, coaching, and progress tracking"
- 4 feature cards in a responsive grid (col-12 col-md-6 col-lg-3):
  1. **Plan IDP (Silabus)** - icon: `bi-file-earmark-pdf`, color: primary, description: "View IDP Proton curriculum and development plan documents", links to `Url.Action("Index", "CDP")` with query param `?bagian=show` (Note: this needs a separate approach - see below)
  2. **Laporan Coaching** - icon: `bi-chat-dots`, color: success, description: "View and manage coaching session logs and records", links to `Url.Action("Coaching", "CDP")`
  3. **Progress & Tracking** - icon: `bi-graph-up-arrow`, color: warning, description: "Track IDP completion progress and approval status", links to `Url.Action("Progress", "CDP")`
  4. **Dashboard Monitoring** - icon: `bi-speedometer2`, color: info, description: "View CDP analytics, completion rates, and unit compliance", links to `Url.Action("Dashboard", "CDP")`

IMPORTANT: For card 1 (Plan IDP), since CDP/Index is now the hub page, the IDP Proton PDF viewer needs a new action. Add a new action `PlanIdp` to CDPController.cs that contains the exact same logic as the current `Index` action (copy the body). Then update the existing `Index` action to simply `return View();` (no parameters needed). The Plan IDP card should link to `Url.Action("PlanIdp", "CDP")`.

Also create the view `Views/CDP/PlanIdp.cshtml` by renaming/copying the current `Views/CDP/Index.cshtml` content BEFORE overwriting it. The PlanIdp.cshtml should be identical to the current Index.cshtml content.

Include the same `.icon-box` and `.card` hover styles from CMP/Index.cshtml at the bottom.

**Controllers/CDPController.cs** changes:
- Copy the current `Index` method body into a new method `public async Task<IActionResult> PlanIdp(string? bagian = null, string? unit = null, string? level = null)`
- Simplify `Index` to just `public IActionResult Index() { return View(); }`

**Views/Shared/_Layout.cshtml** - Update the navbar:
1. Replace CDP dropdown (lines 59-68) with a simple nav-item (same pattern as CMP on line 55-57):
   ```
   <li class="nav-item">
       <a class="nav-link text-dark" asp-controller="CDP" asp-action="Index">CDP</a>
   </li>
   ```
2. Replace BP dropdown (lines 70-79) with a simple nav-item:
   ```
   <li class="nav-item">
       <a class="nav-link text-dark" asp-controller="BP" asp-action="Index">BP</a>
   </li>
   ```
  </action>
  <verify>
    - `dotnet build` compiles successfully
    - Navigate to /CDP/Index shows card hub page with 4 feature cards
    - Navigate to /CDP/PlanIdp shows the original IDP Proton PDF viewer
    - Navbar shows CMP, CDP, BP as simple links (no dropdowns)
  </verify>
  <done>CDP/Index displays a 4-card hub page. CDP/PlanIdp serves the original IDP PDF viewer. Navbar has direct links for all 3 modules.</done>
</task>

<task type="auto">
  <name>Task 2: Delete BP feature pages and simplify BPController to under-development placeholder</name>
  <files>
    Views/BP/Index.cshtml
    Views/BP/Simulation.cshtml (DELETE)
    Views/BP/Historical.cshtml (DELETE)
    Views/BP/EligibilityValidator.cshtml (DELETE)
    Views/BP/TalentProfile.cshtml (DELETE)
    Views/BP/PointSystem.cshtml (DELETE)
    Controllers/BPController.cs
  </files>
  <action>
**Delete these 5 view files** (use `rm` or `Remove-Item`):
- `Views/BP/Simulation.cshtml`
- `Views/BP/Historical.cshtml`
- `Views/BP/EligibilityValidator.cshtml`
- `Views/BP/TalentProfile.cshtml`
- `Views/BP/PointSystem.cshtml`

**Views/BP/Index.cshtml** - Replace the single-line placeholder with a proper "under development" page:
```html
@{
    ViewData["Title"] = "BP - Best People";
}

<div class="container-fluid py-5">
    <div class="row justify-content-center">
        <div class="col-12 col-md-8 col-lg-6 text-center">
            <div class="card border-0 shadow-sm p-5">
                <div class="mb-4">
                    <i class="bi bi-rocket-takeoff text-primary" style="font-size: 5rem;"></i>
                </div>
                <h2 class="fw-bold mb-3">Best People Portal</h2>
                <p class="text-muted fs-5 mb-4">
                    Modul ini sedang dalam tahap pengembangan.
                </p>
                <div class="alert alert-info border-0 bg-info bg-opacity-10">
                    <i class="bi bi-info-circle me-2"></i>
                    Fitur Talent Profile, Point System, Career Simulation, dan Eligibility Validator akan segera tersedia.
                </div>
                <a href="@Url.Action("Index", "Home")" class="btn btn-outline-primary mt-3">
                    <i class="bi bi-arrow-left me-2"></i>Kembali ke Beranda
                </a>
            </div>
        </div>
    </div>
</div>
```

**Controllers/BPController.cs** - Simplify to only the Index action:
- Remove `TalentProfile`, `PointSystem`, `EligibilityValidator` action methods
- Remove ALL ViewModel classes at the bottom of the file (TalentProfileViewModel, PerformanceRecord, CareerHistory, PointSystemViewModel, PointActivity, EligibilityViewModel, EligibilityCriteria)
- Remove unused `using` statements (keep only Microsoft.AspNetCore.Mvc and Microsoft.AspNetCore.Authorization)
- Remove the `_userManager` and `_context` fields and constructor (not needed for a simple view return)
- The controller should be minimal:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HcPortal.Controllers
{
    [Authorize]
    public class BPController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
```

**Check for broken references:** Verify that `CareerHistory` class used by `Views/BP/Historical.cshtml` (which we are deleting) is NOT the same `CareerHistory` used elsewhere. The BPController.cs defines its own `CareerHistory` class in the HcPortal.Controllers namespace. Check if any other file references `HcPortal.Controllers.CareerHistory` - if not, safe to delete. The `CareerCandidate` model in `Models/CareerCandidate.cs` is only used by `Simulation.cshtml` which we are deleting, but do NOT delete the model file itself (out of scope, just note it is now unused).
  </action>
  <verify>
    - `dotnet build` compiles without errors
    - `ls Views/BP/` shows only `Index.cshtml`
    - Navigate to /BP/Index shows the "under development" placeholder
    - Navigate to /BP/TalentProfile returns 404 (action removed)
  </verify>
  <done>All 5 BP feature views deleted. BPController simplified to single Index action. BP/Index shows styled "under development" placeholder in Indonesian. No compilation errors from removed ViewModels.</done>
</task>

</tasks>

<verification>
- `dotnet build` compiles with zero errors
- Navbar shows 3 simple links: CMP, CDP, BP (no dropdowns)
- /CMP/Index shows CMP hub (unchanged)
- /CDP/Index shows new CDP hub with 4 feature cards
- /CDP/PlanIdp shows original IDP Proton PDF viewer
- /CDP/Coaching, /CDP/Progress, /CDP/Dashboard still work
- /BP/Index shows "under development" placeholder
- /BP/TalentProfile, /BP/PointSystem, /BP/EligibilityValidator return 404
</verification>

<success_criteria>
- CDP has a proper card-based hub page consistent with CMP/Index visual style
- All CDP features remain accessible via hub cards and direct URLs
- BP module cleanly shows "under development" with no broken links
- Navbar is simplified with direct links for all 3 modules
- Build compiles with zero errors
</success_criteria>

<output>
After completion, create `.planning/quick/2-add-cdp-index-page-delete-bp-pages-creat/2-SUMMARY.md`
</output>
