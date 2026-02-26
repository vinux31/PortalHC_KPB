---
phase: 13-add-bagian-selection-page-for-cmp-kkjidp
plan: 13
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CMP/MappingSectionSelect.cshtml
  - Controllers/CMPController.cs
autonomous: true
requirements:
  - add-bagian-selection-cmp-mapping
must_haves:
  truths:
    - "Clicking 'CPDP Mapping' from CMP Index shows a bagian selection page"
    - "User can click RFCC, GAST, NGP, or DHT/HMU card to enter the mapping view for that bagian"
    - "Mapping view URL includes the section param: /CMP/Mapping?section=GAST"
    - "Mapping view header reflects the selected bagian"
    - "Navigating to /CMP/Mapping with no section redirects to the selection page"
  artifacts:
    - path: "Views/CMP/MappingSectionSelect.cshtml"
      provides: "Bagian selection UI for CPDP Mapping — 4 cards for RFCC, GAST, NGP, DHT"
    - path: "Controllers/CMPController.cs"
      provides: "Updated Mapping() action accepting optional section param, redirects to MappingSectionSelect when empty"
  key_links:
    - from: "Views/CMP/Index.cshtml (CPDP Mapping card)"
      to: "/CMP/Mapping"
      via: "Url.Action(\"Mapping\", \"CMP\") — unchanged"
    - from: "CMPController.Mapping()"
      to: "Views/CMP/MappingSectionSelect"
      via: "return View(\"MappingSectionSelect\") when section is null/empty"
    - from: "MappingSectionSelect.cshtml card links"
      to: "/CMP/Mapping?section=RFCC|GAST|NGP|DHT"
      via: "Url.Action(\"Mapping\", \"CMP\", new { section = \"RFCC\" })"
---

<objective>
Add a bagian (department) selection page for CMP CPDP Mapping, mirroring the existing pattern used by KKJ Matrix. When a user clicks "CPDP Mapping" from CMP/Index, they first land on a selection page with 4 department cards (RFCC, GAST, NGP, DHT). Selecting a card takes them to the Mapping view filtered to that section.

Purpose: Consistent navigation UX between KKJ Matrix and CPDP Mapping. Both are per-bagian views that need a gateway selection step.
Output: MappingSectionSelect.cshtml view + updated Mapping() controller action.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md

<interfaces>
<!-- Existing patterns the executor uses directly. No codebase exploration needed. -->

From Controllers/CMPController.cs — existing Kkj() action (the exact pattern to replicate for Mapping):
```csharp
public async Task<IActionResult> Kkj(string? section)
{
    var user = await _userManager.GetUserAsync(User);
    var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
    var userRole = userRoles.FirstOrDefault();
    int userLevel = user?.RoleLevel ?? 6;

    ViewBag.UserRole = userRole;
    ViewBag.UserLevel = userLevel;
    ViewBag.SelectedSection = section;

    // If Level 1-3 (Admin, HC, Management) and no section selected, show selection page
    if (UserRoles.HasFullAccess(userLevel) && string.IsNullOrEmpty(section))
    {
        return View("KkjSectionSelect");
    }

    var matrixData = await _context.KkjMatrices
        .OrderBy(k => k.No)
        .ToListAsync();

    return View(matrixData);
}
```

Current Mapping() action (no section param yet):
```csharp
public async Task<IActionResult> Mapping()
{
    var cpdpData = await _context.CpdpItems
        .OrderBy(c => c.No)
        .ToListAsync();

    return View(cpdpData);
}
```

From Views/CMP/KkjSectionSelect.cshtml — the exact UI pattern to mirror for MappingSectionSelect:
- 4 cards: RFCC (blue #0d6efd), GAST (orange #fd7e14), NGP (green #198754), DHT (purple #6f42c1)
- GAST full name: "GSH Alkylation & Sour Treating" (per Mapping.cshtml header — NOT "Gas Separation & Treatment")
- DHT full name: "Diesel Hydrotreating/Hydrogen Manufacturing Unit" (per task description)
- Links go to Url.Action("Mapping", "CMP", new { section = "RFCC" }) etc.
- Button text: "Lihat Mapping" (not "Lihat KKJ")
- Page title/header: "Mapping KKJ - IDP (CPDP)" with subtitle "Pilih bagian untuk melihat mapping CPDP"

From Views/CMP/Mapping.cshtml — header area to update:
```html
<h2 class="fw-bold">Mapping KKJ - IDP (CPDP)</h2>
<p class="text-muted">Kurikulum Pengembangan Kompetensi: <span class="fw-bold text-dark">Unit GSH & Alkylation Level Operator</span></p>
```
This hardcoded subtitle should reflect the selected section when one is passed.

From Views/CMP/Index.cshtml — CPDP Mapping card link (unchanged, already correct):
```html
<a href="@Url.Action("Mapping", "CMP")" class="btn btn-success w-100">
```
No change needed here — the controller handles the redirect.
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create MappingSectionSelect.cshtml</name>
  <files>Views/CMP/MappingSectionSelect.cshtml</files>
  <action>
Create Views/CMP/MappingSectionSelect.cshtml modeled exactly after Views/CMP/KkjSectionSelect.cshtml with these differences:

1. ViewData["Title"] = "Pilih Bagian - Mapping KKJ-IDP (CPDP)"
2. Header h2: "Mapping KKJ - IDP (CPDP)"
3. Subtitle p: "Pilih bagian untuk melihat mapping CPDP"
4. All 4 card links point to Url.Action("Mapping", "CMP", new { section = "RFCC" }) etc. (not "Kkj")
5. Button text: "Lihat Mapping" (not "Lihat KKJ")
6. GAST description: "GSH Alkylation & Sour Treating" (matches Mapping.cshtml — NOT "Gas Separation & Treatment")
7. DHT description: "Diesel Hydrotreating/Hydrogen Manufacturing Unit"
8. RFCC description: "Residual Fluid Catalytic Cracking"
9. NGP description: "Naphtha Gas Processing"
10. Back button: Url.Action("Index", "CMP") — back to CMP portal (not Home)

Copy all CSS from KkjSectionSelect.cshtml verbatim (section-card, section-icon-wrap, color classes) — no changes needed.

Icons: RFCC = bi-building, GAST = bi-fire, NGP = bi-droplet, DHT = bi-gear-wide-connected (same as KkjSectionSelect).
  </action>
  <verify>File Views/CMP/MappingSectionSelect.cshtml exists and contains "Url.Action("Mapping"" and all four sections (RFCC, GAST, NGP, DHT)</verify>
  <done>MappingSectionSelect.cshtml renders 4 department cards, each linking to /CMP/Mapping?section={BAGIAN}</done>
</task>

<task type="auto">
  <name>Task 2: Update CMPController.Mapping() to accept section param and show selection page</name>
  <files>Controllers/CMPController.cs</files>
  <action>
In Controllers/CMPController.cs, update the Mapping() action (currently at line ~75) to accept an optional section parameter and show the selection page when no section is provided. Mirror the Kkj() action pattern exactly:

Replace:
```csharp
public async Task<IActionResult> Mapping()
{
    var cpdpData = await _context.CpdpItems
        .OrderBy(c => c.No)
        .ToListAsync();

    return View(cpdpData);
}
```

With:
```csharp
public async Task<IActionResult> Mapping(string? section)
{
    // If no section selected, show bagian selection page
    if (string.IsNullOrEmpty(section))
    {
        return View("MappingSectionSelect");
    }

    ViewBag.SelectedSection = section;

    var cpdpData = await _context.CpdpItems
        .OrderBy(c => c.No)
        .ToListAsync();

    return View(cpdpData);
}
```

Note: No UserRoles.HasFullAccess() check needed here — the Mapping page is accessible to all roles (unlike Kkj which has role-based gating). All users selecting a section should see the Mapping view directly.

Also update Views/CMP/Mapping.cshtml: replace the hardcoded subtitle line:
```html
<p class="text-muted">Kurikulum Pengembangan Kompetensi: <span class="fw-bold text-dark">Unit GSH & Alkylation Level Operator</span></p>
```
With a dynamic version using ViewBag.SelectedSection:
```html
<p class="text-muted">Kurikulum Pengembangan Kompetensi: <span class="fw-bold text-dark">
    @(ViewBag.SelectedSection == "RFCC" ? "Unit RFCC Level Operator" :
      ViewBag.SelectedSection == "GAST" ? "Unit GSH & Alkylation Level Operator" :
      ViewBag.SelectedSection == "NGP" ? "Unit Naphtha Gas Processing Level Operator" :
      ViewBag.SelectedSection == "DHT" ? "Unit Diesel Hydrotreating/HMU Level Operator" :
      "Unit GSH & Alkylation Level Operator")
</span></p>
```

Also add a "Ganti Bagian" back button in Mapping.cshtml just below the header div, pointing to Url.Action("Mapping", "CMP") (no section = returns to selection page):
```html
<a href="@Url.Action("Mapping", "CMP")" class="btn btn-outline-secondary btn-sm mb-3">
    <i class="bi bi-arrow-left me-1"></i>Ganti Bagian
</a>
```

Files modified: Controllers/CMPController.cs, Views/CMP/Mapping.cshtml
  </action>
  <verify>dotnet build succeeds with no errors. Navigate to /CMP/Mapping — should show MappingSectionSelect. Navigate to /CMP/Mapping?section=GAST — should show the mapping table with "Unit GSH & Alkylation Level Operator" subtitle and a "Ganti Bagian" button.</verify>
  <done>
- /CMP/Mapping (no param) shows bagian selection page with 4 department cards
- /CMP/Mapping?section=GAST shows the mapping table with correct section subtitle
- "Ganti Bagian" button on Mapping view returns to selection page
- dotnet build: 0 errors
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` from project root — 0 errors, 0 warnings (or only pre-existing warnings)
2. Navigate to /CMP — "CPDP Mapping" card is present, clicking it goes to /CMP/Mapping
3. /CMP/Mapping shows MappingSectionSelect with 4 cards (RFCC, GAST, NGP, DHT)
4. Clicking GAST card navigates to /CMP/Mapping?section=GAST and shows the mapping table
5. "Ganti Bagian" button on the mapping table returns to /CMP/Mapping (selection page)
6. KKJ Matrix flow is unchanged — /CMP/Kkj still works as before
</verification>

<success_criteria>
- Navigating to /CMP/Mapping without a section shows the 4-card bagian selection page
- Each card links correctly to /CMP/Mapping?section={BAGIAN}
- The mapping table header shows a section-appropriate subtitle
- A "Ganti Bagian" back button is visible on the mapping table view
- No regressions: existing /CMP/Kkj and /CMP/Index flows unaffected
- dotnet build passes cleanly
</success_criteria>

<output>
After completion, create `.planning/quick/13-add-bagian-selection-page-for-cmp-kkjidp/13-SUMMARY.md`
</output>
