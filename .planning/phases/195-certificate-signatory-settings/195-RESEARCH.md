# Phase 195: Sub-Categories & Signatory Settings - Research

**Researched:** 2026-03-18
**Domain:** ASP.NET Core MVC — EF Core self-referencing FK, Bootstrap 5 UI hierarchy, QuestPDF PDF layout, signatory resolution chain
**Confidence:** HIGH

## Summary

Phase 195 extends `AssessmentCategory` with two new nullable columns: a self-referencing `ParentId` (int?) FK and a `SignatoryUserId` (string?) FK pointing to `AspNetUsers`. Up to three levels of hierarchy (grandchild) are supported but the parent dropdown filters to depth < 2. Admin ManageCategories gets an indented tree table, Parent Category dropdown, and a searchable Signatory dropdown with live P-Sign preview. The CreateAssessment and EditAssessment wizard category dropdowns become `<optgroup>`-grouped selects. Both the HTML Certificate.cshtml and QuestPDF CertificatePdf are updated to Design A2: Pertamina logo header, compact no-border P-Sign footer replacing the static "Authorized Sig." block, no QR code.

The signatory resolution chain is: category's own SignatoryUserId → parent's SignatoryUserId → static fallback (Position = "HC Manager", FullName = ""). This chain must be resolved in the controller before passing data to both Certificate and CertificatePdf views.

**Primary recommendation:** Add EF Core self-referencing relationship with `DeleteBehavior.Restrict` on the ParentId FK (prevents cascade delete, enforcing the "delete children first" rule at the database level). Resolve signatory at the controller level and pass a populated `PSignViewModel` to both views.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Sub-category Hierarchy**
- Two levels deep maximum: Parent > Child > Grandchild
- Self-referencing nullable ParentId FK on AssessmentCategory
- Add form gets optional "Parent Category" dropdown (shows only categories where depth < 2)
- Block delete on categories that have children — must delete children first

**Admin ManageCategories UI**
- Indented list in existing table: children shown indented under parent row
- Add/Edit form gains: Parent Category dropdown + Signatory user dropdown
- Signatory dropdown shows all users (no role filter), searchable
- Mini P-Sign preview appears below signatory dropdown after selection (shows how it will look on certificate)

**Wizard & Edit Dropdown**
- CreateAssessment wizard: HTML `<optgroup>` with parent as group label, children as options
- EditAssessment: same optgroup-style dropdown
- Any level selectable — both parents and leaf categories can be assigned to assessments

**Signatory Configuration**
- New SignatoryUserId nullable FK on AssessmentCategory → ApplicationUser
- Admin selects a user account; system reads their FullName + Position for P-Sign
- Inheritance: sub-category with no signatory falls back to parent's signatory
- Global fallback: if no signatory on category or parent, certificate shows old static "Authorized Sig." + line + "HC Manager"

**Certificate Design (A2)**
- Header: Pertamina logo image (reuse psign-pertamina.png) + "HC PORTAL KPB" text + "Human Capital Development Portal" subtitle — replaces old text-only icon header
- Footer layout: Date section (left) — P-Sign (right, shifted slightly left with margin-right)
- No QR code
- P-Sign style on certificate: no border, no padding, no "KPB" line — compact (logo + position + name only)
- P-Sign sized larger than Settings version: logo ~48px, font 0.85-0.9rem
- Score badge stays as-is (bottom-right circle)
- Both HTML Certificate.cshtml and QuestPDF CertificatePdf updated to Design A2
- Settings page _PSign.cshtml partial unchanged (keeps border + KPB line)

**Migration & Seed Data**
- Add ParentId (nullable int, self-ref FK) and SignatoryUserId (nullable string FK to AspNetUsers) columns to AssessmentCategory
- Existing categories get null for both new columns — admin sets values manually
- No default signatory assigned during migration

### Claude's Discretion
- Exact spacing/typography adjustments in certificate layout
- Searchable dropdown implementation (Select2, tom-select, or native)
- P-Sign preview rendering approach on ManageCategories (partial view, JS template, etc.)

### Deferred Ideas (OUT OF SCOPE)
- QR code verification on certificate — discussed and explicitly removed from scope
- Public certificate verification page — not needed
- Training hours/duration on certificate — not in scope for this phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| R195-1 | AssessmentCategory gains nullable ParentId (self-ref FK) for parent→children hierarchy | EF Core self-referencing FK pattern documented below; `DeleteBehavior.Restrict` prevents orphan delete |
| R195-2 | Admin Manage Categories UI shows parent categories with expandable sub-categories | Indented tree table via ps-4/ps-5 + arrow icon; parent badge; delete block pattern documented |
| R195-3 | CreateAssessment wizard category dropdown shows grouped options (parent > sub) | `<optgroup>` HTML pattern; ViewBag.ParentCategories DTO structure documented |
| R195-4 | Per-category SignatoryName field stored in AssessmentCategory, displayed on certificate | SignatoryUserId FK + inheritance chain resolver; Certificate.cshtml + CertificatePdf Design A2 patterns documented |
</phase_requirements>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core (Microsoft.EntityFrameworkCore.SqlServer) | Already in project | Self-referencing FK, migration | Project ORM — no alternative |
| ASP.NET Core MVC | Already in project | Controller actions, Razor views | Project framework |
| QuestPDF | Already in project (wwwroot/fonts/ present) | PDF certificate generation | Established in Phase 194 |
| Bootstrap 5 | Project-wide | UI components, utility classes | Project design system |
| Bootstrap Icons (bi-*) | Project-wide | Action icons in table | Project icon system |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| tom-select | v2.x (CDN or wwwroot/lib) | Searchable signatory dropdown | Claude's discretion; native `<select>` is acceptable fallback |
| Playfair Display + Lato | Google Fonts (Certificate.cshtml only) | Certificate typography | Already loaded in Certificate.cshtml |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| tom-select | Select2 | Both are fine; tom-select is lighter, no jQuery dependency |
| tom-select | Native `<select size="N">` | Native avoids JS library entirely — acceptable per CONTEXT.md |

**Installation (tom-select if chosen):**
```bash
# Option A: CDN (add to ManageCategories.cshtml head section)
# https://cdn.jsdelivr.net/npm/tom-select@2/dist/css/tom-select.css
# https://cdn.jsdelivr.net/npm/tom-select@2/dist/js/tom-select.complete.min.js
# Option B: copy dist files to wwwroot/lib/tom-select/
```

---

## Architecture Patterns

### Self-Referencing FK in EF Core

**Model pattern:**
```csharp
// Source: EF Core official docs — self-referencing relationships
public class AssessmentCategory
{
    public int Id { get; set; }
    [Required][MaxLength(100)]
    public string Name { get; set; } = "";
    public int DefaultPassPercentage { get; set; } = 70;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    // New in Phase 195
    public int? ParentId { get; set; }
    public string? SignatoryUserId { get; set; }

    // Navigation properties
    public AssessmentCategory? Parent { get; set; }
    public ICollection<AssessmentCategory> Children { get; set; } = new List<AssessmentCategory>();
    public ApplicationUser? Signatory { get; set; }
}
```

**DbContext Fluent API (add to existing AssessmentCategory entity block):**
```csharp
// In ApplicationDbContext.OnModelCreating
builder.Entity<AssessmentCategory>(entity =>
{
    entity.ToTable("AssessmentCategories");
    entity.HasIndex(c => c.Name).IsUnique();
    entity.HasIndex(c => c.SortOrder);
    entity.Property(c => c.IsActive).HasDefaultValue(true);
    entity.Property(c => c.DefaultPassPercentage).HasDefaultValue(70);

    // Self-referencing FK — Phase 195
    entity.HasOne(c => c.Parent)
          .WithMany(c => c.Children)
          .HasForeignKey(c => c.ParentId)
          .OnDelete(DeleteBehavior.Restrict); // blocks delete when children exist

    // Signatory FK — Phase 195
    entity.HasOne(c => c.Signatory)
          .WithMany()
          .HasForeignKey(c => c.SignatoryUserId)
          .OnDelete(DeleteBehavior.SetNull); // if user deleted, signatory clears
});
```

**CRITICAL — `DeleteBehavior.Restrict`:** This makes the database refuse to delete a category that has children. The controller's `DeleteCategory` POST action must check for children BEFORE calling `SaveChanges` and return an error message (currently `TempData["Error"]`), since EF Core will throw `DbUpdateException` on restrict violation. Pre-check in code = better UX than catching DB exception.

### Migration Pattern

```csharp
// New migration: AddParentAndSignatoryToAssessmentCategory
migrationBuilder.AddColumn<int?>(
    name: "ParentId",
    table: "AssessmentCategories",
    nullable: true);

migrationBuilder.AddColumn<string?>(
    name: "SignatoryUserId",
    table: "AssessmentCategories",
    nullable: true,
    maxLength: 450); // AspNetUsers.Id is nvarchar(450)

migrationBuilder.AddForeignKey(
    name: "FK_AssessmentCategories_AssessmentCategories_ParentId",
    table: "AssessmentCategories",
    column: "ParentId",
    principalTable: "AssessmentCategories",
    principalColumn: "Id",
    onDelete: ReferentialAction.Restrict);

migrationBuilder.AddForeignKey(
    name: "FK_AssessmentCategories_AspNetUsers_SignatoryUserId",
    table: "AssessmentCategories",
    column: "SignatoryUserId",
    principalTable: "AspNetUsers",
    principalColumn: "Id",
    onDelete: ReferentialAction.SetNull);
```

Existing rows have both new columns as NULL — no data migration needed.

### Signatory Resolution Chain (Controller)

Both `Certificate` and `CertificatePdf` actions must resolve the signatory. Pattern:

```csharp
// Resolve signatory from AssessmentSession.CategoryId (if FK exists)
// OR from AssessmentSession.AssessmentCategory string via lookup
// Apply inheritance chain: own → parent → static fallback

private async Task<PSignViewModel> ResolvePSign(int? categoryId)
{
    if (categoryId.HasValue)
    {
        var cat = await _context.AssessmentCategories
            .Include(c => c.Signatory)
            .Include(c => c.Parent).ThenInclude(p => p.Signatory)
            .FirstOrDefaultAsync(c => c.Id == categoryId.Value);

        if (cat?.Signatory != null)
            return new PSignViewModel { FullName = cat.Signatory.FullName, Position = cat.Signatory.Position };

        if (cat?.Parent?.Signatory != null)
            return new PSignViewModel { FullName = cat.Parent.Signatory.FullName, Position = cat.Parent.Signatory.Position };
    }
    // Static fallback
    return new PSignViewModel { FullName = "", Position = "HC Manager" };
}
```

**IMPORTANT:** `AssessmentSession` currently stores category as a string field (`AssessmentCategory`), NOT an FK. The Certificate actions will need to look up the category by name to resolve the signatory. The v7.5 roadmap decision states categories remain as strings in AssessmentSession — confirm whether `CategoryId` FK is being added in this phase or if lookup by name is required.

Looking at the codebase: `AssessmentSession.AssessmentCategory` is a string. Phase 195 does NOT add a CategoryId FK to AssessmentSession (not mentioned in CONTEXT.md). Resolution must look up `AssessmentCategory` where `Name == session.AssessmentCategory`.

### ViewBag Pattern for Hierarchical Categories

Existing pattern: `ViewBag.Categories` passes `IEnumerable<AssessmentCategory>`. New pattern needed for optgroup requires parent-child structure. Use a DTO or grouped LINQ query:

```csharp
// In AdminController — ManageCategories + CreateAssessment/EditAssessment
var allCategories = await _context.AssessmentCategories
    .Include(c => c.Children)
    .Include(c => c.Signatory)
    .Where(c => c.ParentId == null)   // only roots with children loaded
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .ToListAsync();

ViewBag.ParentCategories = allCategories; // List<AssessmentCategory> with Children populated

// For ManageCategories tree table — need all categories ordered:
var flatOrdered = await _context.AssessmentCategories
    .Include(c => c.Children)
    .Include(c => c.Signatory)
    .Where(c => c.ParentId == null)
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .ToListAsync();
// Then in Razor: foreach parent, render parent row, then foreach parent.Children...
```

### Certificate Header — Design A2

Current `Certificate.cshtml` header (before the `header-title` div) needs a `logo-section` block added. Per UI-SPEC:

```html
<div class="logo-section" style="display:flex; align-items:center; justify-content:center; gap:16px; margin-bottom:24px;">
  <img src="/images/psign-pertamina.png" alt="Pertamina" style="height:55px; width:auto;"
       onerror="this.style.display='none'" />
  <div style="font-family:'Playfair Display',serif; font-size:18px; font-weight:bold; color:#1a4a8d; letter-spacing:2px; text-align:left; line-height:1.3;">
    HC PORTAL KPB
    <small style="display:block; font-size:12px; font-weight:400; letter-spacing:0.5px; color:#666;">Human Capital Development Portal</small>
  </div>
</div>
```

### Certificate Footer — Design A2

Replace `signature-section` div (Certificate.cshtml lines 280–287):

```html
<!-- Dynamic P-Sign — compact, no border -->
<div class="psign-badge" style="text-align:center; background:transparent; margin-right:32px;">
    <img src="/images/psign-pertamina.png" alt="Logo Pertamina"
         style="height:48px; margin-bottom:8px;"
         onerror="this.style.display='none'" />
    <div style="font-size:0.85rem; color:#333; line-height:1.3;">@Model.PSign.Position</div>
    <div style="font-size:0.9rem; font-weight:700; color:#000; line-height:1.3; margin-top:4px;">@Model.PSign.FullName</div>
</div>
```

`Certificate.cshtml` currently uses `AssessmentSession` as its model. The PSign data must be passed via ViewBag or by converting to a ViewModel. Given existing pattern (action returns `View(assessment)`), use `ViewBag.PSign` as a `PSignViewModel`.

### QuestPDF CertificatePdf — Design A2 changes

**Header change:** Replace existing `col.Item().AlignCenter().PaddingBottom(20).Text("HC PORTAL KPB")...` block with a Row containing the Pertamina logo image + text column:

```csharp
// Logo header row (replaces existing "HC PORTAL KPB" text-only header)
col.Item().AlignCenter().PaddingBottom(18).Row(headerRow =>
{
    headerRow.AutoItem()
        .Image(Path.Combine(_env.WebRootPath, "images", "psign-pertamina.png"))
        .FitHeight().WithCompressionQuality(ImageCompressionQuality.High);

    headerRow.ConstantItem(12); // gap

    headerRow.AutoItem().AlignMiddle().Column(logoText =>
    {
        logoText.Item()
            .Text("HC PORTAL KPB")
            .FontFamily("Playfair Display").FontSize(18).Bold()
            .LetterSpacing(0.05f).FontColor("#1a4a8d");
        logoText.Item()
            .Text("Human Capital Development Portal")
            .FontFamily("Lato").FontSize(12).FontColor("#666666");
    });
});
```

**Footer change:** Replace static "Authorized Sig." / "HC Manager" text in the right column with resolved PSign:

```csharp
// Right: P-Sign (compact — logo + position + name)
row.AutoItem().AlignRight().AlignBottom().PaddingRight(30).Column(right =>
{
    right.Item().AlignCenter()
        .Image(Path.Combine(_env.WebRootPath, "images", "psign-pertamina.png"))
        .FitHeight(); // constrain height via width of container

    right.Item().AlignCenter().PaddingTop(4)
        .Text(pSign.Position ?? "HC Manager")
        .FontFamily("Lato").FontSize(10).FontColor("#333333");

    right.Item().AlignCenter().PaddingTop(2)
        .Text(pSign.FullName)
        .FontFamily("Lato").FontSize(11).Bold().FontColor("#000000");
});
```

### Anti-Patterns to Avoid
- **Cascade delete on self-referencing FK:** Using `DeleteBehavior.Cascade` would silently delete all children when a parent is deleted. Use `Restrict` and pre-check in controller.
- **Passing CategoryId as new FK on AssessmentSession:** Phase 195 does NOT add CategoryId to AssessmentSession — lookup signatory by matching `AssessmentCategory` string name.
- **Modifying `_PSign.cshtml` partial:** Per CONTEXT.md, the Settings-page P-Sign partial is unchanged. Certificate uses inline CSS matching Design A2 spec.
- **Using `ViewBag.Categories` (flat list) for optgroup rendering:** The wizard views need `ViewBag.ParentCategories` (list of parent categories with Children loaded), not the flat list. Both ViewBag keys may coexist if needed for backward compatibility.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Searchable dropdown | Custom JS search filter | tom-select or native | tom-select is production-grade; native is acceptable |
| Self-ref FK relationship | Custom parent-child tracking logic | EF Core `HasOne/WithMany` self-ref | EF handles join/include/cascade correctly |
| Certificate PDF image loading | Base64 embed in code | `QuestPDF Image(filePath)` | QuestPDF has built-in image API with compression |

---

## Common Pitfalls

### Pitfall 1: Depth Calculation for Parent Dropdown Filter

**What goes wrong:** The "Parent Category" dropdown must only show categories at depth 0 or 1 (i.e., cannot select a grandchild as a parent). A category at depth 2 would create depth-3 nodes.

**How to avoid:** When building the parent dropdown list, load all categories that have `ParentId == null` (depth 0) OR `ParentId != null AND Parent.ParentId == null` (depth 1). This is a two-level check and can be done with a single query using `.Include(c => c.Parent)`.

**Warning signs:** If the parent dropdown is built from a simple "all non-null ParentId categories" filter, it will incorrectly allow depth-3 assignments.

### Pitfall 2: Unique Name Constraint With Parent-Child

**What goes wrong:** The existing `HasIndex(c => c.Name).IsUnique()` makes category names globally unique. This means "Gas Tester" under "HSSE" and "Gas Tester" under "Safety" would conflict.

**How to handle:** Per CONTEXT.md, this is not mentioned as requiring change. Keep the global unique name constraint — the admin must use distinct names. This is a known limitation to document to the user, not a bug.

### Pitfall 3: AssessmentSession stores Category as string, not FK

**What goes wrong:** The `Certificate` and `CertificatePdf` actions receive an `AssessmentSession` with `AssessmentCategory` as a string. To resolve the signatory, a database lookup by name is needed. If the category name was later edited, the lookup may fail.

**How to avoid:** Lookup with `.FirstOrDefaultAsync(c => c.Name == assessment.AssessmentCategory)`. If null, fall back to static P-Sign. Do NOT throw — silent fallback is correct behavior.

### Pitfall 4: QuestPDF Image Loading

**What goes wrong:** `_env.WebRootPath` may differ between development and production. If the image file is missing, QuestPDF throws at render time.

**How to avoid:** Use `try/catch` around the image load, or check `File.Exists()` before adding the Image element. The watermark already has a `try/catch` pattern in the existing code.

### Pitfall 5: ManageCategories Controller — ViewBag must be set on all POST re-render paths

**What goes wrong:** Phase 190 decision notes "ViewBag.Categories must be set in all POST re-render paths to prevent NullReferenceException on form re-render." The new `ParentCategories` ViewBag must follow the same pattern.

**How to avoid:** Create a private helper method `SetCategoriesViewBag()` that sets both ViewBag keys and call it from all ManageCategories action paths.

### Pitfall 6: Delete action does not pre-check for children

**What goes wrong:** Current `DeleteCategory` POST does `_context.AssessmentCategories.Remove(category)` then `SaveChanges()`. With `DeleteBehavior.Restrict`, this will throw `DbUpdateException` if the category has children.

**How to avoid:** Add pre-check before remove:
```csharp
if (await _context.AssessmentCategories.AnyAsync(c => c.ParentId == id))
{
    TempData["Error"] = "Hapus sub-kategori terlebih dahulu.";
    return RedirectToAction("ManageCategories");
}
```

---

## Code Examples

### Signatory Lookup by Category Name (Certificate actions)

```csharp
// Source: CONTEXT.md pattern + existing AssessmentSession string-category convention
var pSign = new PSignViewModel { Position = "HC Manager", FullName = "" }; // static fallback

var category = await _context.AssessmentCategories
    .Include(c => c.Signatory)
    .Include(c => c.Parent).ThenInclude(p => p!.Signatory)
    .FirstOrDefaultAsync(c => c.Name == assessment.AssessmentCategory);

if (category?.Signatory != null)
    pSign = new PSignViewModel { FullName = category.Signatory.FullName ?? "", Position = category.Signatory.Position };
else if (category?.Parent?.Signatory != null)
    pSign = new PSignViewModel { FullName = category.Parent.Signatory.FullName ?? "", Position = category.Parent.Signatory.Position };

ViewBag.PSign = pSign;
```

### Optgroup Razor rendering (CreateAssessment / EditAssessment)

```html
<!-- Source: 195-UI-SPEC.md Component 7 -->
<select name="categoryId" class="form-select" required>
    <option value="">-- Pilih Kategori --</option>
    @{
        var parents = (IEnumerable<HcPortal.Models.AssessmentCategory>)ViewBag.ParentCategories;
        var standalone = parents.Where(p => !p.Children.Any());
        var withChildren = parents.Where(p => p.Children.Any());
    }
    @foreach (var parent in standalone)
    {
        <option value="@parent.Id" @(Model.CategoryId == parent.Id ? "selected" : "")>@parent.Name</option>
    }
    @foreach (var parent in withChildren)
    {
        <optgroup label="@parent.Name">
            <option value="@parent.Id" @(Model.CategoryId == parent.Id ? "selected" : "")>@parent.Name (Induk)</option>
            @foreach (var child in parent.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
            {
                <option value="@child.Id" @(Model.CategoryId == child.Id ? "selected" : "")>&nbsp;&nbsp;@child.Name</option>
            }
        </optgroup>
    }
</select>
```

Note: `Model.CategoryId` does not yet exist on `AssessmentSession` — `CreateAssessment.cshtml` binds to `name="categoryId"` as a separate form field matched in the controller POST action. Check how the current flat dropdown passes the selected category.

### ManageCategories tree table rendering (Razor)

```html
<!-- Source: 195-UI-SPEC.md Component 1 -->
@foreach (var parent in Model) // Model is IEnumerable<AssessmentCategory> with Children loaded
{
    var hasChildren = parent.Children.Any();
    <tr>
        <td class="p-3 fw-semibold">
            @parent.Name
            @if (hasChildren) {
                <span class="badge bg-light text-secondary border ms-2 small">Parent</span>
            }
        </td>
        <!-- ... other columns ... -->
    </tr>
    @foreach (var child in parent.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
    {
        <tr>
            <td class="p-3 ps-4">
                <i class="bi bi-arrow-return-right text-muted me-2 small"></i>@child.Name
            </td>
        </tr>
        @foreach (var grandchild in child.Children)
        {
            <tr>
                <td class="p-3 ps-5">
                    <i class="bi bi-arrow-return-right text-muted me-2 small"></i>@grandchild.Name
                </td>
            </tr>
        }
    }
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Static "Authorized Sig." + "HC Manager" on certificate | Dynamic P-Sign from category signatory with inheritance fallback | Phase 195 | Certificate now reflects actual signatory per assessment category |
| Text-only "HC PORTAL KPB" header on certificate | Pertamina logo + text header (Design A2) | Phase 195 | Matches approved cert-preview-A2.html design |
| Flat category list in wizard dropdown | optgroup-grouped by parent | Phase 195 | Hierarchical categories navigable in standard HTML select |

---

## Open Questions

1. **CategoryId on AssessmentSession**
   - What we know: AssessmentSession stores category as string `AssessmentCategory`; v7.5 roadmap locked this as string-only
   - What's unclear: When `CreateAssessment` POST runs, does it store the selected `categoryId` as an integer FK OR convert to the category name string? If categories can be renamed, signatory lookup by name will be stale.
   - Recommendation: Check `AdminController.cs` CreateAssessment POST action to confirm how `categoryId` form field is currently processed. If it maps to string, the lookup-by-name approach works but may become stale if category is renamed. This is acceptable per current scope.

2. **ApplicationUser.Position field**
   - What we know: `PSignViewModel.Position` is populated from user's `Position` field
   - What's unclear: Confirm `ApplicationUser` has a `Position` property (not just `FullName`)
   - Recommendation: Grep `ApplicationUser.cs` for `Position` before implementing signatory resolution.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project convention per MEMORY.md) |
| Config file | none |
| Quick run command | `dotnet build` (compilation check) |
| Full suite command | `dotnet build && dotnet run` (smoke test) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| R195-1 | Migration applies: ParentId + SignatoryUserId columns exist in DB | smoke | `dotnet ef database update` | ❌ Wave 0 (migration file) |
| R195-1 | Delete blocked when children exist | manual | n/a — verify in browser | manual-only |
| R195-2 | ManageCategories shows indented child rows | manual | n/a — visual verification | manual-only |
| R195-2 | Add/Edit forms accept Parent + Signatory fields | manual | n/a — form submission test | manual-only |
| R195-3 | Wizard category dropdown renders optgroup groups | manual | n/a — visual/DOM check | manual-only |
| R195-4 | Certificate P-Sign shows category signatory name | manual | n/a — browser verification | manual-only |
| R195-4 | Certificate PDF P-Sign matches HTML version | manual | n/a — download and compare | manual-only |

### Sampling Rate
- **Per task commit:** `dotnet build` — confirms no compile errors
- **Per wave merge:** `dotnet build` + browser smoke test of ManageCategories and Certificate pages
- **Phase gate:** All manual tests pass before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] EF Core migration file: `AddParentAndSignatoryToAssessmentCategory` — generated by `dotnet ef migrations add`
- [ ] Confirm `ApplicationUser.Position` field exists before implementing signatory resolution

*(All test coverage is manual per project convention — no automated test files to create)*

---

## Sources

### Primary (HIGH confidence)
- `Models/AssessmentCategory.cs` — current model shape verified directly
- `Controllers/AdminController.cs` lines 759–880 — ManageCategories CRUD actions verified directly
- `Views/Admin/ManageCategories.cshtml` — full view verified directly
- `Controllers/CMPController.cs` lines 2332–2576 — Certificate + CertificatePdf actions verified directly
- `wwwroot/cert-preview-A2.html` — approved Design A2 mockup verified directly
- `Views/Shared/_PSign.cshtml` — P-Sign partial verified directly
- `Models/PSignViewModel.cs` — PSign view model verified directly
- `.planning/phases/195-certificate-signatory-settings/195-CONTEXT.md` — locked decisions
- `.planning/phases/195-certificate-signatory-settings/195-UI-SPEC.md` — approved UI contract

### Secondary (MEDIUM confidence)
- EF Core self-referencing relationship pattern — standard EF Core documentation pattern, highly stable

### Tertiary (LOW confidence)
- tom-select v2 CDN URLs — should be verified against https://tom-select.js.org/ before use

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project, verified from codebase
- Architecture: HIGH — patterns derived directly from existing code + approved CONTEXT.md + UI-SPEC
- Pitfalls: HIGH — derived from existing code analysis (string-based category, no-FK pattern, existing ViewBag rules from Phase 190)

**Research date:** 2026-03-18
**Valid until:** 2026-04-17 (stable domain)
