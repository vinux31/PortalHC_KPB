# Phase 118: P-Sign Infrastructure - Research

**Researched:** 2026-03-07
**Domain:** Razor partial view component, CSS styling, ASP.NET Core ViewModels
**Confidence:** HIGH

## Summary

This phase is straightforward infrastructure work: create a reusable P-Sign (digital initial badge) as a Razor partial view with a ViewModel and inline CSS. No external libraries needed -- everything uses existing ASP.NET Core patterns already established in the codebase.

ApplicationUser already has all required fields (Position, Unit, FullName). The logo file already exists at `wwwroot/images/psign-pertamina.png`. The only deliverables are: PSignViewModel class, _PSign.cshtml partial view, and a preview section on the Settings page.

**Primary recommendation:** Build as a self-contained partial view with inline styles. Follow existing ViewModel conventions in Models/ directory.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Vertical layout: Logo on top, then Position, then Unit, then Full Name
- Border box with rounded corners, ~180px width, grey border/black text
- Nama bold, Role+Unit regular weight; no NIP, no disclaimer, no date
- Data from ApplicationUser.Position, .Unit, .FullName (not role system)
- Fallback: hide rows with null/empty values
- Razor Partial View: `Views/Shared/_PSign.cshtml`
- ViewModel: `PSignViewModel` with LogoUrl, Position, Unit, FullName
- Inline styles within `<style>` tag inside partial (self-contained)
- Logo: `wwwroot/images/psign-pertamina.png`, height ~40px
- Show P-Sign preview on Account/Settings page
- No separate image generation endpoint
- Phase 120 handles PDF conversion

### Claude's Discretion
- Exact spacing, padding, font sizes within the badge
- Settings page preview placement
- ViewModel location (Models/ or ViewModels/ -- follow existing convention)

### Deferred Ideas (OUT OF SCOPE)
None.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PSIGN-01 | ApplicationUser has Position/Role text and Unit fields for P-Sign rendering | Already satisfied -- ApplicationUser has Position (string?), Unit (string?), FullName (string) fields. No schema changes needed. |
| PSIGN-02 | P-Sign badge contains Logo Pertamina, Role + Unit, and full name | PSignViewModel + _PSign.cshtml partial view with inline CSS. Logo at wwwroot/images/psign-pertamina.png already exists. |
| PSIGN-03 | P-Sign renderable as embeddable component for PDF and web | Razor partial via `@await Html.PartialAsync("_PSign", model)`. Phase 120 will render the HTML page containing P-Sign to PDF -- no image generation needed in this phase. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | (existing) | Razor partial views, ViewModels | Already the project framework |
| Bootstrap 5 | (existing) | Utility classes if needed | Already loaded in _Layout |

No additional packages needed.

## Architecture Patterns

### New Files
```
Models/PSignViewModel.cs          # Simple ViewModel (4 properties)
Views/Shared/_PSign.cshtml        # Self-contained partial with inline <style>
```

### Modified Files
```
Views/Account/Settings.cshtml     # Add P-Sign preview section
Controllers/AccountController.cs  # Populate PSignViewModel in Settings GET action
Models/SettingsViewModel.cs       # Add PSign property
```

### Pattern: Self-Contained Partial View
**What:** Partial view with inline `<style>` block so it renders correctly anywhere (web page, PDF HTML)
**When to use:** When the component must render in multiple contexts without external CSS dependencies
**Example:**
```csharp
// PSignViewModel.cs
public class PSignViewModel
{
    public string LogoUrl { get; set; } = "/images/psign-pertamina.png";
    public string? Position { get; set; }
    public string? Unit { get; set; }
    public string FullName { get; set; } = string.Empty;
}
```

```html
<!-- _PSign.cshtml -->
@model HcPortal.Models.PSignViewModel
<style>
    .psign-badge { /* inline styles */ }
</style>
<div class="psign-badge">
    <img src="@Model.LogoUrl" alt="Pertamina" />
    @if (!string.IsNullOrEmpty(Model.Position)) { <div>@Model.Position</div> }
    @if (!string.IsNullOrEmpty(Model.Unit)) { <div>@Model.Unit</div> }
    <div class="psign-name">@Model.FullName</div>
</div>
```

### Pattern: ViewModel Composition for Settings Page
**What:** Add PSignViewModel as a property on SettingsViewModel
**Example:**
```csharp
// In SettingsViewModel.cs, add:
public PSignViewModel? PSign { get; set; }
```

```html
<!-- In Settings.cshtml, after profile form, before password section -->
<hr class="my-4">
<p class="text-uppercase fw-bold small text-muted mb-3">Preview P-Sign</p>
@if (Model.PSign != null)
{
    @await Html.PartialAsync("_PSign", Model.PSign)
}
```

### Anti-Patterns to Avoid
- **External CSS file for P-Sign:** Breaks self-containment for PDF rendering in Phase 120
- **Using role system names instead of Position field:** CONTEXT.md explicitly says use ApplicationUser.Position
- **Rendering empty badge when all fields null:** Should still render with FullName (which is required/non-null)

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Image generation | Server-side image rendering | HTML partial (Phase 120 does HTML-to-PDF) | User explicitly deferred image generation |

## Common Pitfalls

### Pitfall 1: CSS Class Name Collisions
**What goes wrong:** Generic class names like `.badge` conflict with Bootstrap
**How to avoid:** Prefix all classes with `psign-` (e.g., `.psign-badge`, `.psign-name`)

### Pitfall 2: Logo Path in PDF Context
**What goes wrong:** Relative paths like `/images/...` won't resolve in PDF generation
**How to avoid:** Use LogoUrl property in ViewModel so Phase 120 can pass absolute URL or base64 if needed

### Pitfall 3: Multiple Partial Renders on Same Page
**What goes wrong:** `<style>` block duplicated if two P-Signs on same page
**How to avoid:** Use scoped/unique CSS class names so duplicate styles are harmless. This is acceptable for a small component.

## Code Examples

### Populating PSignViewModel from AccountController
```csharp
// In Settings GET action, after loading user:
var psign = new PSignViewModel
{
    Position = user.Position,
    Unit = user.Unit,
    FullName = user.FullName
};
// Add to SettingsViewModel
model.PSign = psign;
```

### Consuming _PSign from any view
```html
@await Html.PartialAsync("_PSign", new PSignViewModel
{
    Position = someUser.Position,
    Unit = someUser.Unit,
    FullName = someUser.FullName
})
```

## Open Questions

None -- all decisions are locked and the implementation is straightforward.

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection: ApplicationUser.cs, Settings.cshtml, SettingsViewModel.cs
- CONTEXT.md with locked decisions from user discussion
- wwwroot/images/psign-pertamina.png confirmed to exist

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, all existing patterns
- Architecture: HIGH - simple partial view, well-established ASP.NET Core pattern
- Pitfalls: HIGH - minor CSS concerns, straightforward mitigations

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable, no moving parts)
