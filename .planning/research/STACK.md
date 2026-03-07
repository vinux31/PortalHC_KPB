# Technology Stack

**Project:** PortalHC KPB — v3.9 ProtonData Enhancement
**Researched:** 2026-03-07

## Verdict: No New Packages Needed

The existing stack handles all v3.9 requirements. No NuGet additions required.

## Current Stack (unchanged)

| Technology | Version | Purpose |
|------------|---------|---------|
| ASP.NET Core | .NET 8.0 | Web framework |
| EF Core | 8.0.0 | ORM + migrations |
| SQL Server | — | Database |
| Bootstrap 5 | — | UI framework |
| Bootstrap Icons | — | Icon set |
| Vanilla JS | — | Client-side logic |
| Razor Views | — | Server-side rendering |
| ClosedXML | 0.105.0 | Excel import/export |
| QuestPDF | 2026.2.2 | PDF generation |

## Feature-by-Feature Stack Mapping

### 1. Target Column in Silabus Table

**Need:** Add `string Target` property to Silabus model, EF Core migration.

**Stack:** EF Core migrations (already in use). Standard `dotnet ef migrations add AddSilabusTarget` workflow. No new packages.

**Migration approach:**
```csharp
// Nullable string column, no breaking change
migrationBuilder.AddColumn<string>(
    name: "Target",
    table: "Silabus",  // verify actual table name
    nullable: true);
```

### 2. Tree Checklist UI (Status Tab)

**Need:** Hierarchical display: Bagian > Unit > Track with completeness indicators.

**Stack:** Bootstrap 5 accordion (nested) + Bootstrap Icons for check/uncheck states. No third-party tree library needed.

**Why not a tree library:**
- Only 3 levels deep (Bagian > Unit > Track) — accordion handles this natively
- Bootstrap accordion is already loaded and used in the project
- Adding a JS tree library (jstree, etc.) would be overhead for a read-only status display
- Checkmarks are display-only, not interactive tree-select

**Pattern:**
```html
<!-- Outer accordion: Bagian -->
<div class="accordion" id="statusTree">
  <div class="accordion-item">
    <h2 class="accordion-header">
      <button class="accordion-button">
        <i class="bi bi-check-circle-fill text-success"></i> Bagian Name
      </button>
    </h2>
    <div class="accordion-collapse">
      <!-- Inner accordion: Unit > Track rows -->
    </div>
  </div>
</div>
```

**Icons:** `bi-check-circle-fill` (complete), `bi-circle` (incomplete), `bi-dash-circle` (partial). Already available via Bootstrap Icons.

### 3. Cascade Delete at Kompetensi Level

**Need:** Hard delete Kompetensi + all child records (SubKompetensi, Silabus, etc.).

**Stack:** EF Core cascade delete behavior, configured via `OnDelete(DeleteBehavior.Cascade)` in DbContext or handled explicitly in controller with `Include()` + `RemoveRange()`.

**Recommendation:** Explicit removal in controller (load children, RemoveRange) rather than relying on DB cascade — gives more control and clearer audit trail. This is the safer pattern for a brownfield app where FK constraints may not all have cascade configured.

### 4. Audit Silabus Table Connections

**Need:** Trace all FK references to Silabus from PlanIdp, CoachingProton, etc.

**Stack:** No tooling needed. Code grep + EF Core model inspection. Pure analysis task.

## Alternatives Considered

| Need | Considered | Verdict |
|------|-----------|---------|
| Tree UI | jstree, Treeview.js | Overkill for 3-level read-only display |
| Tree UI | Custom CSS tree | Bootstrap accordion already does this |
| Cascade delete | DB-level CASCADE | Explicit code removal is safer in brownfield |

## Confidence: HIGH

All features use patterns already established in the codebase. Zero new dependencies.
