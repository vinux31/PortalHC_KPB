# Phase 190: DB Categories Foundation - Research

**Researched:** 2026-03-17
**Domain:** ASP.NET Core MVC — EF Core model + migration + Admin CRUD + Razor view update
**Confidence:** HIGH

## Summary

This phase moves six hardcoded assessment category strings out of Razor views and into a SQL table (`AssessmentCategories`). The table carries `DefaultPassPercentage` so the JS `categoryDefaults` object in CreateAssessment.cshtml can be eliminated — each `<option>` will carry a `data-pass-percentage` attribute instead. A new ManageCategories page (Admin/HC access) provides full CRUD; the Admin/Index hub gets a new card in Section C.

The implementation is entirely within existing patterns: `ApplicationDbContext` gets one new `DbSet`, a standard EF migration creates the table with seed data via `migrationBuilder.Sql` + MERGE, the two assessment forms switch from a Razor-built `SelectListItem` list to `ViewBag.Categories` loaded in GET actions, and the controller branches on `model.Category == "Assessment Proton"` remain unchanged because the string value is preserved.

No new packages are required. No FK from `AssessmentSession.Category` is created — the column stays `nvarchar` — so existing session records are never broken.

**Primary recommendation:** Follow the ProtonTrack migration MERGE pattern for seed data; follow the ManageWorkers CRUD pattern for ManageCategories; use `data-pass-percentage` on `<option>` elements to replace the JS `categoryDefaults` map.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Dedicated page at Admin/ManageCategories — not inline or tab-based
- New card in Admin/Index hub Section C (Assessment & Training) where ManageAssessment already lives
- Access: Admin + HC roles (same as ManageAssessment)
- Fields: `Id`, `Name` (string), `DefaultPassPercentage` (int), `IsActive` (bool), `SortOrder` (int)
- Categories stay as plain strings on AssessmentSession.Category — no FK relationship (protects historical data)
- 6 seed rows: OJT (70%), IHT (70%), Training Licencor (80%), OTS (70%), Mandatory HSSE Training (100%), Assessment Proton (70%)
- Allow delete even if sessions reference the category — string stays on existing sessions
- IsActive toggle available as softer alternative to deletion
- Both CreateAssessment.cshtml AND EditAssessment.cshtml updated in this phase
- All hardcoded category lists removed from views — zero hardcoded categories remain after this phase
- `categoryDefaults` JS object in CreateAssessment replaced with `data-pass-percentage` attributes on `<option>` elements
- EditAssessment category dropdown also loads from DB via ViewBag

### Claude's Discretion
- ManageCategories page layout and table styling
- Exact SortOrder default values for seed data
- Validation messages and error handling UX

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FORM-02 | Admin dapat mengelola kategori assessment dari database (CRUD) tanpa perlu edit code | AssessmentCategory model + migration + ManageCategories CRUD page + ViewBag.Categories in form GET actions eliminates all hardcoded categories |
</phase_requirements>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | Already in project | ORM + migration | Already in use throughout the project |
| ASP.NET Core MVC | Already in project | Controller + Razor views | Entire project uses this stack |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| AuditLogService | Project-internal | Log all admin CRUD actions | Required by project convention for all admin mutations |

No new NuGet packages required.

**Installation:** None needed.

---

## Architecture Patterns

### Recommended Project Structure

New files for this phase:
```
Models/
└── AssessmentCategory.cs          # New entity model

Views/Admin/
└── ManageCategories.cshtml        # New CRUD page

Migrations/
└── YYYYMMDDHHMMSS_AddAssessmentCategoriesTable.cs  # New migration
```

Modified files:
```
Data/ApplicationDbContext.cs       # Add DbSet<AssessmentCategory> + OnModelCreating config
Controllers/AdminController.cs     # Add ManageCategories + AddCategory + EditCategory + DeleteCategory + ToggleCategory actions; update CreateAssessment GET + EditAssessment GET
Views/Admin/CreateAssessment.cshtml  # Replace hardcoded list + categoryDefaults JS
Views/Admin/EditAssessment.cshtml    # Replace hardcoded list
Views/Admin/Index.cshtml             # Add card to Section C
```

### Pattern 1: Entity Model (follows project convention)

**What:** Plain POCO with `[Required]` / `[MaxLength]` annotations.
**When to use:** Every new table in this project.
**Example:**
```csharp
// New file: Models/AssessmentCategory.cs
namespace HcPortal.Models
{
    public class AssessmentCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        public int DefaultPassPercentage { get; set; } = 70;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }
}
```

### Pattern 2: DbContext Registration (follows project convention)

**What:** Add `DbSet` + `OnModelCreating` config block.
**Example:**
```csharp
// In Data/ApplicationDbContext.cs
public DbSet<AssessmentCategory> AssessmentCategories { get; set; }

// In OnModelCreating:
builder.Entity<AssessmentCategory>(entity =>
{
    entity.ToTable("AssessmentCategories");
    entity.HasIndex(c => c.Name).IsUnique();
    entity.HasIndex(c => c.SortOrder);
    entity.Property(c => c.IsActive).HasDefaultValue(true);
    entity.Property(c => c.DefaultPassPercentage).HasDefaultValue(70);
});
```

### Pattern 3: Migration with Seed Data (MERGE pattern — from ProtonTrack migration)

**What:** Create table + seed rows in a single migration using `migrationBuilder.Sql` MERGE statement. This is the established pattern for this project (see `20260223060707_CreateProtonTrackTable.cs`).
**When to use:** Any new lookup table that needs initial rows.
**Example:**
```csharp
// Up() method
migrationBuilder.CreateTable(
    name: "AssessmentCategories",
    columns: table => new
    {
        Id = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
        DefaultPassPercentage = table.Column<int>(type: "int", nullable: false, defaultValue: 70),
        IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
        SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_AssessmentCategories", x => x.Id);
    });

migrationBuilder.CreateIndex(
    name: "IX_AssessmentCategories_Name",
    table: "AssessmentCategories",
    column: "Name",
    unique: true);

// Seed 6 rows via MERGE (idempotent — safe to re-run)
migrationBuilder.Sql(@"
    WITH Expected AS (
        SELECT 'OJT' AS Name, 70 AS DefaultPassPercentage, 1 AS SortOrder
        UNION ALL SELECT 'IHT', 70, 2
        UNION ALL SELECT 'Training Licencor', 80, 3
        UNION ALL SELECT 'OTS', 70, 4
        UNION ALL SELECT 'Mandatory HSSE Training', 100, 5
        UNION ALL SELECT 'Assessment Proton', 70, 6
    )
    MERGE INTO AssessmentCategories ac
    USING Expected e ON ac.Name = e.Name
    WHEN NOT MATCHED THEN
        INSERT (Name, DefaultPassPercentage, IsActive, SortOrder)
        VALUES (e.Name, e.DefaultPassPercentage, 1, e.SortOrder);
");
```

### Pattern 4: ViewBag.Categories Loading (follows ViewBag.ProtonTracks convention)

**What:** Load active categories ordered by SortOrder in each form GET action.
**Example:**
```csharp
// In AdminController.cs — CreateAssessment GET and EditAssessment GET
ViewBag.Categories = await _context.AssessmentCategories
    .Where(c => c.IsActive)
    .OrderBy(c => c.SortOrder)
    .ThenBy(c => c.Name)
    .ToListAsync();
```

### Pattern 5: data-pass-percentage on Option Elements (replaces JS categoryDefaults)

**What:** Embed `DefaultPassPercentage` as a data attribute on each `<option>` so JS can read it on change without a hardcoded map.
**Example (Razor):**
```html
<!-- In CreateAssessment.cshtml / EditAssessment.cshtml -->
<select id="Category" name="Category" class="form-select" required>
    <option value="">-- Pilih Kategori --</option>
    @foreach (var cat in (IEnumerable<HcPortal.Models.AssessmentCategory>)ViewBag.Categories)
    {
        <option value="@cat.Name"
                data-pass-percentage="@cat.DefaultPassPercentage"
                @(Model.Category == cat.Name ? "selected" : "")>
            @cat.Name
        </option>
    }
</select>
```

**Updated JS (replaces categoryDefaults block):**
```javascript
// Replace the categoryDefaults object + lookup with:
categorySelect.addEventListener('change', function() {
    var selected = this.options[this.selectedIndex];
    var defaultPct = selected ? parseInt(selected.getAttribute('data-pass-percentage') || '0') : 0;
    if (!passPercentageManuallySet && defaultPct > 0) {
        passPercentageInput.value = defaultPct;
    }
});
```

### Pattern 6: ManageCategories CRUD (follows ManageWorkers pattern)

**What:** Standard full-page CRUD with list table, add form, edit form, and delete action. Same structure as ManageWorkers (`AdminController.cs` lines ~3958+).
**Actions needed:**
- `GET ManageCategories` — list all categories, pass to view
- `POST AddCategory` — create new, redirect back
- `GET EditCategory(int id)` — load for edit form
- `POST EditCategory(int id, ...)` — save changes, redirect
- `POST DeleteCategory(int id)` — delete (no FK check needed per decision), redirect
- `POST ToggleCategoryActive(int id)` — flip IsActive, redirect

**AuditLog calls required in all POST mutations** — project convention logs every admin action via `_auditLog.LogAsync(...)`.

### Pattern 7: Admin/Index Hub Card (follows existing Section C cards)

**What:** Add a new card to Section C (Assessment & Training) using existing Bootstrap card layout from `Views/Admin/Index.cshtml`.
**Where:** After the existing ManageAssessment card (line 134) and AssessmentMonitoring card.
**Example (consistent with existing cards):**
```html
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("ManageCategories", "Admin")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-tags fs-5 text-primary"></i>
                    <span class="fw-bold">Kategori Assessment</span>
                </div>
                <small class="text-muted">Kelola kategori assessment dan nilai lulus default</small>
            </div>
        </div>
    </a>
</div>
}
```

### Anti-Patterns to Avoid
- **HasData in OnModelCreating for seed rows:** This project does NOT use `HasData` for seeding — it uses `migrationBuilder.Sql` MERGE in migration files. Do not use `HasData`.
- **FK from AssessmentSession.Category to AssessmentCategories.Id:** Explicitly rejected — category stays as a plain string column on sessions.
- **Hardcoded fallback list in view:** After this phase, zero hardcoded categories should remain. Do not add a view-side fallback if `ViewBag.Categories` is null — fail loudly so the bug is caught.
- **Filtering out IsActive categories in CreateAssessment GET:** The query should only return `IsActive = true` categories so deactivated ones don't appear in new sessions.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Idempotent seed data | Custom INSERT with IF NOT EXISTS | `migrationBuilder.Sql` MERGE pattern | Already proven in ProtonTrack migration; handles re-run and data drift |
| Audit trail | Custom log table writes | `_auditLog.LogAsync(...)` (AuditLogService) | Service already injected in AdminController; consistent with all other admin actions |
| Category uniqueness | Application-layer duplicate check | `HasIndex(...).IsUnique()` on `Name` column | DB constraint is the only reliable enforcement |

---

## Common Pitfalls

### Pitfall 1: Seed String Mismatch with Controller Branching
**What goes wrong:** If the seed value for "Assessment Proton" doesn't exactly match the string used in `AdminController.cs` controller branching (`model.Category == "Assessment Proton"`), Proton-specific logic (ProtonTrackId assignment, TahunKe, Year 3 interview) silently breaks.
**Why it happens:** The JS `categoryDefaults` map (line 538) actually had BOTH `'Proton': 85` and `'Assessment Proton': 70` — the "Proton" key is orphaned/legacy. The controller exclusively checks `"Assessment Proton"`.
**How to avoid:** Seed row must be `Name = "Assessment Proton"` (not "Proton"). Verify against controller grep results: `model.Category == "Assessment Proton"` appears at lines 839, 948, 986, 1210, 1887, 1926.
**Warning signs:** Proton track dropdown doesn't show when selecting the Proton category on the create form, or TahunKe is null after creating a Proton session.

### Pitfall 2: passPercentageManuallySet Logic Broken by JS Refactor
**What goes wrong:** The `passPercentageManuallySet` flag tracks whether the user manually typed a pass percentage. If the `change` event listener is rewritten incorrectly, it may always override the user's manual value or never pre-fill.
**Why it happens:** The existing logic in CreateAssessment.cshtml (lines 530–553) sets `passPercentageManuallySet = true` on `input` events, and only auto-fills if the flag is false. The refactor must preserve this guard.
**How to avoid:** Keep the `input` event listener that sets `passPercentageManuallySet = true`. In the `change` handler, only write to `passPercentageInput.value` when `!passPercentageManuallySet`.

### Pitfall 3: EditAssessment Selected Category Not Matching DB Row
**What goes wrong:** The EditAssessment form pre-selects the current `Model.Category` value. If a category name was edited or deleted after a session was created, no option will be pre-selected (the value is orphaned).
**Why it happens:** No FK, so the session's string value can become stale relative to the categories table.
**How to avoid:** In EditAssessment.cshtml, after the DB-driven option loop, add a fallback: if `Model.Category` is not in the list, render it as a disabled option so the form still submits the existing value correctly. This is defensive — the success criteria don't require it but it prevents invisible data corruption.

### Pitfall 4: ViewBag.Categories Null Reference in View
**What goes wrong:** If `ViewBag.Categories` is not set (e.g., a code path returns the view without loading categories), the Razor foreach throws a NullReferenceException.
**Why it happens:** Both CreateAssessment POST (validation failure re-render) and EditAssessment GET/POST must set ViewBag.Categories. Easy to forget the POST failure re-render path.
**How to avoid:** Add `ViewBag.Categories = await _context.AssessmentCategories...` to every code path that returns the view — including the re-render on ModelState invalid in both CreateAssessment POST and EditAssessment POST.

### Pitfall 5: Migration Unique Index Conflict on Re-Seed
**What goes wrong:** If the migration is run on a database that already has some category names from a previous partial run, the MERGE inserts only missing rows. But if the `Name` unique index already exists and a row with a different casing was inserted manually, the MERGE fails.
**Why it happens:** SQL Server string comparison is case-insensitive by default for most collations, but the unique index enforces the exact stored casing.
**How to avoid:** The MERGE uses `ON ac.Name = e.Name` which is case-insensitive on default collations — this is correct. The seed strings must be the exact production casing (verified from views/controller).

---

## Code Examples

### Loading Categories in Form GET Actions
```csharp
// Source: established ViewBag.ProtonTracks pattern in this codebase
// AdminController.cs — CreateAssessment GET (insert after line 771)
ViewBag.Categories = await _context.AssessmentCategories
    .Where(c => c.IsActive)
    .OrderBy(c => c.SortOrder)
    .ThenBy(c => c.Name)
    .ToListAsync();

// Same block added to EditAssessment GET (after line 1160)
```

### Category Dropdown in Razor (both CreateAssessment and EditAssessment)
```html
<!-- Replaces the Razor @{ var categories = new List<SelectListItem> { ... } } block at top of view -->
<!-- AND replaces the asp-items="@Model(categories)" or manual option loop -->
<select asp-for="Category" class="form-select" id="Category" required>
    <option value="">-- Pilih Kategori --</option>
    @foreach (var cat in (IEnumerable<HcPortal.Models.AssessmentCategory>)ViewBag.Categories)
    {
        <option value="@cat.Name"
                data-pass-percentage="@cat.DefaultPassPercentage"
                selected="@(Model.Category == cat.Name)">
            @cat.Name
        </option>
    }
</select>
```

### AuditLog Call Pattern (from AdminController)
```csharp
// Source: AuditLogService usage pattern throughout AdminController.cs
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";

await _auditLog.LogAsync(
    currentUser?.Id ?? "",
    actorName,
    "AddCategory",
    $"Added assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
    category.Id,
    "AssessmentCategory");
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded `SelectListItem` list in Razor view `@{ }` block | DB-driven list loaded in controller GET, passed via ViewBag | This phase | Admin can add/edit categories without code deployment |
| `categoryDefaults` JS object keyed by category name string | `data-pass-percentage` attribute on each `<option>` element | This phase | Default pass % is always in sync with DB; no JS maintenance needed |

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Not detected — no test project found in solution |
| Config file | None |
| Quick run command | Manual browser test |
| Full suite command | Manual browser test |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FORM-02 | Admin navigates to ManageCategories, adds a new category, sees it in list | manual | n/a | n/a |
| FORM-02 | Admin edits existing category name | manual | n/a | n/a |
| FORM-02 | Admin deletes category with no session references | manual | n/a | n/a |
| FORM-02 | CreateAssessment dropdown populated from DB (no hardcoded values) | manual | n/a | n/a |
| FORM-02 | EditAssessment dropdown populated from DB (no hardcoded values) | manual | n/a | n/a |
| FORM-02 | Selecting category in CreateAssessment auto-fills PassPercentage from data attribute | manual | n/a | n/a |
| FORM-02 | All 6 original seed rows present after migration | manual (DB query) | n/a | n/a |

### Sampling Rate
- Per task commit: Manual smoke test of affected form
- Per wave merge: Full FORM-02 acceptance checklist above
- Phase gate: All 7 manual checks pass before `/gsd:verify-work`

### Wave 0 Gaps
None — no automated test infrastructure; all validation is manual browser testing as per project convention.

---

## Open Questions

1. **EditAssessment GET action location**
   - What we know: The GET action populates ViewBag.Users, ViewBag.Sections, ViewBag.ProtonTracks; it returns the assessment model at line 1169.
   - What's unclear: The `EditAssessment GET` signature is at approximately line 1107–1169 — `ViewBag.ProtonTracks` may or may not already be set there. Need to verify before writing the plan task.
   - Recommendation: Implementer reads lines 1107–1170 of AdminController before writing the EditAssessment GET modification to confirm exact insertion point.

2. **ViewBag.Categories in POST re-render paths**
   - What we know: Both CreateAssessment POST (lines 795–1105) and EditAssessment POST (lines 1176+) can re-render the view on validation failure.
   - What's unclear: Exact count of re-render `return View(model)` calls in each POST action.
   - Recommendation: Search for `return View(model)` within each POST action and ensure `ViewBag.Categories` is set before every one of them. Planner should create a dedicated sub-task for this.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` — CreateAssessment GET (lines 759–789), POST branching on "Assessment Proton" (multiple locations), EditAssessment GET (lines ~1107–1170)
- `Views/Admin/CreateAssessment.cshtml` — Hardcoded category list (lines 7–15), `categoryDefaults` JS (lines 538–546)
- `Views/Admin/EditAssessment.cshtml` — Hardcoded category list (lines 9–17)
- `Data/ApplicationDbContext.cs` — DbSet registration pattern, OnModelCreating configuration pattern
- `Migrations/20260223060707_CreateProtonTrackTable.cs` — Authoritative reference for MERGE seed pattern
- `Views/Admin/Index.cshtml` — Section C structure (lines 126–180) for hub card placement

### Secondary (MEDIUM confidence)
- None needed — all findings verified against source code.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, all patterns verified against existing code
- Architecture: HIGH — directly derived from ProtonTrack migration and ManageWorkers controller patterns in this codebase
- Pitfalls: HIGH — pitfall 1 (seed string mismatch) verified by reading exact controller branches; pitfalls 2–5 derived from direct code inspection

**Research date:** 2026-03-17
**Valid until:** 2026-04-17 (stable domain, no external dependencies)
