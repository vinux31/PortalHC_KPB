# Phase 399: Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 16 (new/modified)
**Analogs found:** 16 / 16 (all have a repo analog — ~80% reuse; only `UserUnit` model, the migration, `SyncUserUnitsAsync`, and the JS multi-cascade variant are genuinely new shapes, and each still copies an existing pattern)

> All excerpts below are copy-paste-ready with `file:line`. Stack: ASP.NET Core 8 MVC + EF Core 8 (SQL Server / SQLEXPRESS), Razor server-rendered, Bootstrap 5 + Bootstrap Icons, vanilla JS, ClosedXML. Brownfield — reuse idioms, do NOT introduce new libs (D-03 forbids multiselect/chip libs).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/UserUnit.cs` (NEW) | model (EF entity) | CRUD / junction | `Models/CoachCoacheeMapping.cs:7-51` | exact (junction NAME-string + IsActive) |
| `Data/ApplicationDbContext.cs` (DbSet + config) | DbContext config | schema | `ApplicationDbContext.cs:32` DbSet + `:327-335` CoachCoacheeMapping config + `:225-229` filtered-unique | exact (filtered-unique index 2× precedent) |
| `Migrations/<ts>_AddUserUnitsTable.cs` (NEW) | migration | schema + data migration | `Migrations/20260603012335_AddOrganizationLevelLabel.cs:12-41` (CreateTable+Index) + `:225-229` filter idiom | exact (add-table+index); backfill SQL = new but `migrationBuilder.Sql` standard |
| `Controllers/WorkerController.cs` — `SyncUserUnitsAsync` helper (NEW) | controller-action (private helper) | CRUD write-through (tx) | `CoachMappingController.cs:959-979` (BeginTransaction→SaveChanges→Commit) | role-match (tx pattern) |
| `Controllers/WorkerController.cs` — `CreateWorker` POST (`:212-309`) | controller-action | request-response / CRUD | itself (`:261-275` user create + `:284-297` audit) | exact (in-place modify) |
| `Controllers/WorkerController.cs` — `EditWorker` POST (`:346-481`) | controller-action | request-response / CRUD | itself (`:400-413` audit set-diff seed + `:437` UpdateAsync + `:464-477` audit) | exact (in-place modify) |
| `Controllers/WorkerController.cs` — `Import` (`:915-1040+`) | controller-action / excel | file-I/O / batch | itself (`:937-983` parse+validate loop) | exact (in-place extend) |
| `Controllers/WorkerController.cs` — `Export` (`:180-187`) | controller-action / excel-export | file-I/O / transform | itself (`:186-187` ClosedXML cell write) | exact (in-place modify) |
| `Controllers/AccountController.cs` — `Profile`/`Settings` GET (`:140-206`) | controller-action | request-response (VM populate) | itself (`:149-167`, `:183-203`) | exact (in-place modify) |
| `Models/ManageUserViewModel.cs` (`:36-37`) | viewmodel | — | `ManageUserViewModel.cs:33-37` scalar Section/Unit | exact (extend scalar→+List) |
| `Models/ProfileViewModel.cs` (`:15`) | viewmodel | — | `ProfileViewModel.cs:14-15` | exact |
| `Models/SettingsViewModel.cs` (`:19-21`) | viewmodel | — | `SettingsViewModel.cs:19-21` | exact |
| `Models/PSignViewModel.cs` (`:11`) | viewmodel | — | `PSignViewModel.cs:10-12` | exact |
| `Views/Admin/CreateWorker.cshtml` + `EditWorker.cshtml` | razor-view | — | `CreateWorker.cshtml:119-124` (`<select>`) + `:196-203` cascade init | exact (replace unit select) |
| `Views/Admin/WorkerDetail.cshtml` + `ManageWorkers.cshtml` + `Views/Account/Profile.cshtml` + `Settings.cshtml` + `Views/Home/Index.cshtml` | razor-view (display) | — | `WorkerDetail.cshtml:91-98` semantic badge | exact (badge loop) |
| `Views/Shared/_PSign.cshtml` (`:40-43`) | razor-view (print) | — | `_PSign.cshtml:40-43` plain `.psign-label` | exact |
| `wwwroot/js/shared-cascade.js` — `initSectionUnitMultiCascade` (NEW fn) | js | event-driven (DOM) | `shared-cascade.js:13-49` `initSectionUnitCascade` | exact (extend, same file) |

---

## Pattern Assignments

### `Models/UserUnit.cs` (NEW — model, junction)

**Analog:** `Models/CoachCoacheeMapping.cs:7-51` (int Id, string FK-id, string Unit-name, bool IsActive — exact shape).

**Analog excerpt** (`Models/CoachCoacheeMapping.cs:7-44`):
```csharp
public class CoachCoacheeMapping
{
    public int Id { get; set; }
    public string CoachId { get; set; } = "";
    public string CoacheeId { get; set; } = "";
    public bool IsActive { get; set; } = true;
    // ...
    public string? AssignmentUnit { get; set; }   // NAME-string unit — same convention UserUnit.Unit follows
}
```

**Code to write** (copy shape, add `IsPrimary` + nav per spec §4.1):
```csharp
namespace HcPortal.Models
{
    public class UserUnit
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";   // FK → AspNetUsers.Id (Users.Id), ON DELETE CASCADE
        public string Unit { get; set; } = "";       // NAME-string, child of worker's Section
        public bool IsPrimary { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public ApplicationUser? User { get; set; }
    }
}
```
> If installing the optional `(UserId, Unit)` unique index (recommended, Claude's Discretion D), add `[MaxLength(200)]` on `Unit` — SQL Server rejects `nvarchar(max)` as an index key (Pitfall 2). `UserId` is already `nvarchar(450)` (Identity key).

---

### `Data/ApplicationDbContext.cs` (DbSet + OnModelCreating config)

**Analog A — DbSet declaration** (`ApplicationDbContext.cs:32`):
```csharp
public DbSet<CoachCoacheeMapping> CoachCoacheeMappings { get; set; }
```
**Add (sibling line):**
```csharp
public DbSet<UserUnit> UserUnits { get; set; }
```

**Analog B — filtered-unique index, TWO precedents to copy verbatim:**

`CoachCoacheeMapping` config (`ApplicationDbContext.cs:327-335`):
```csharp
builder.Entity<CoachCoacheeMapping>(entity =>
{
    entity.HasIndex(m => m.CoachId);
    entity.HasIndex(m => m.CoacheeId)
        .HasFilter("[IsActive] = 1")
        .IsUnique()
        .HasDatabaseName("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique");
    entity.HasIndex(m => new { m.CoachId, m.CoacheeId });
});
```
NomorSertifikat filtered-unique (`ApplicationDbContext.cs:225-229`):
```csharp
entity.HasIndex(a => a.NomorSertifikat)
    .IsUnique()
    .HasFilter("[NomorSertifikat] IS NOT NULL")
    .HasDatabaseName("IX_AssessmentSessions_NomorSertifikat_Unique");
```
Cascade FK idiom (`ApplicationDbContext.cs:188-193`, AssessmentSession→User):
```csharp
entity.HasOne(a => a.User).WithMany()
      .HasForeignKey(a => a.UserId)
      .OnDelete(DeleteBehavior.Cascade);
```

**Code to write** (place in `OnModelCreating`, after the CoachCoacheeMapping block ~`:335`):
```csharp
builder.Entity<UserUnit>(entity =>
{
    entity.HasOne(uu => uu.User).WithMany()
          .HasForeignKey(uu => uu.UserId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(uu => uu.UserId);

    // Exactly 1 primary per user (invariant #3) — identical idiom to IX_CoachCoacheeMappings_CoacheeId_ActiveUnique
    entity.HasIndex(uu => uu.UserId)
          .IsUnique()
          .HasFilter("[IsPrimary] = 1")
          .HasDatabaseName("IX_UserUnits_UserId_PrimaryUnique");

    // Optional (recommended) — prevent duplicate unit per user
    entity.HasIndex(uu => new { uu.UserId, uu.Unit })
          .IsUnique()
          .HasDatabaseName("IX_UserUnits_UserId_Unit_Unique");
});
```
> Filter literal uses SQL-Server bracket+bit syntax: `"[IsPrimary] = 1"` mirrors `"[IsActive] = 1"` (`:331`) and `"[NomorSertifikat] IS NOT NULL"` (`:228`). InMemory does NOT enforce this filter (Pitfall 3 → SQL-real enforce deferred to Phase 404).

---

### `Migrations/<ts>_AddUserUnitsTable.cs` (NEW — migration)

**Analog:** `Migrations/20260603012335_AddOrganizationLevelLabel.cs:12-41` (CreateTable + CreateIndex unique).

**Analog excerpt** (`AddOrganizationLevelLabel.cs:14-40`):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "OrganizationLevelLabels",
        columns: table => new
        {
            Level = table.Column<int>(type: "int", nullable: false),
            Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            // ...
        },
        constraints: table => { table.PrimaryKey("PK_OrganizationLevelLabels", x => x.Level); });

    migrationBuilder.CreateIndex(
        name: "IX_OrganizationLevelLabels_Label",
        table: "OrganizationLevelLabels",
        column: "Label",
        unique: true);
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "OrganizationLevelLabels");
}
```

**Code to write** (CreateTable + filtered CreateIndex + idempotent backfill SQL):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "UserUnits",
        columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            UserId   = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Unit     = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false), // 200 so (UserId,Unit) is indexable
            IsPrimary= table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_UserUnits", x => x.Id);
            table.ForeignKey("FK_UserUnits_Users_UserId", x => x.UserId,
                "Users", "Id", onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateIndex("IX_UserUnits_UserId", "UserUnits", "UserId");

    migrationBuilder.CreateIndex(
        name: "IX_UserUnits_UserId_PrimaryUnique",
        table: "UserUnits", column: "UserId",
        unique: true, filter: "[IsPrimary] = 1");

    migrationBuilder.CreateIndex(
        name: "IX_UserUnits_UserId_Unit_Unique",
        table: "UserUnits", columns: new[] { "UserId", "Unit" }, unique: true);

    // BACKFILL (Claude's Discretion → migration Up, idempotent). 1 primary-row per worker w/ non-null Unit.
    migrationBuilder.Sql(@"
        INSERT INTO UserUnits (UserId, Unit, IsPrimary, IsActive)
        SELECT u.Id, u.Unit, 1, 1
        FROM Users u
        WHERE u.Unit IS NOT NULL AND u.Unit <> ''
          AND NOT EXISTS (SELECT 1 FROM UserUnits uu WHERE uu.UserId = u.Id)
    ");
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "UserUnits");
}
```
> **migration=TRUE.** Notify IT with commit hash + flag. `dotnet ef` CLI is v10.0.3 vs project EF 8.0.0 (Pitfall 1) — planner's FIRST task: try `dotnet ef migrations add AddUserUnitsTable`; if it fails, pin a local v8 tool-manifest OR hand-write this file + update `ApplicationDbContextModelSnapshot.cs`. CLAUDE.md gate (`dotnet build` + `dotnet run` localhost:5277 + check local DB) is mandatory before commit.

---

### `Controllers/WorkerController.cs` — `SyncUserUnitsAsync` helper (NEW private method)

**Analog (transaction):** `CoachMappingController.cs:959-979`:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    mapping.IsActive = false;
    mapping.EndDate = DateTime.UtcNow;
    // ... cascade ...
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    // ...
}
```
**DI already available** (`WorkerController.cs:20-31`): `_context` (ApplicationDbContext), `_userManager`, `_auditLog`, `_config`, `_logger`. No new injection needed.

**Code to write** (centralize write-through; returns set-diff for the caller's audit):
```csharp
// Called by CreateWorker / EditWorker / Import. Replace-set strategy: DELETE old + INSERT new in
// one SaveChanges → EF emits DELETE before INSERT, so no 2-primary window vs filtered-unique index.
private async Task<List<string>> SyncUserUnitsAsync(
    ApplicationUser user, List<string> units, string? primaryUnit)
{
    var changes = new List<string>();
    var existing = await _context.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
    var oldSet = existing.Select(e => e.Unit).ToHashSet();
    var newSet = units.Distinct().ToHashSet();

    foreach (var added   in newSet.Except(oldSet))  changes.Add($"Unit +'{added}'");
    foreach (var removed in oldSet.Except(newSet))  changes.Add($"Unit -'{removed}'");

    var primary = (primaryUnit != null && newSet.Contains(primaryUnit))
        ? primaryUnit : units.FirstOrDefault();                       // D-02 deterministic
    var oldPrimary = existing.FirstOrDefault(e => e.IsPrimary)?.Unit;
    if (oldPrimary != primary) changes.Add($"Primary: '{oldPrimary}' → '{primary}'");

    _context.UserUnits.RemoveRange(existing);
    foreach (var u in newSet)
        _context.UserUnits.Add(new UserUnit { UserId = user.Id, Unit = u,
                                              IsPrimary = (u == primary), IsActive = true });

    user.Unit = primary;     // MIRROR (invariant #3); null when 0 units
    return changes;
}
```
> **Open Q3 (atomicity):** Edit uses `_userManager.UpdateAsync(user)` (`:437`, own SaveChanges) while `_context` is separate. Set `user.Unit` BEFORE `UpdateAsync`, then call `SyncUserUnitsAsync` + `_context.SaveChangesAsync()` after — OR wrap both in `BeginTransactionAsync`. Planner decides; integrity test required.

---

### `Controllers/WorkerController.cs` — `CreateWorker` POST (`:212-309`)

**Analog:** the method itself — user-create at `:261-275`, audit at `:284-297`.

**Current code to modify** (`WorkerController.cs:261-275`):
```csharp
var user = new ApplicationUser
{
    // ...
    Section = model.Section,
    Unit = model.Unit,    // ← ANTI-PATTERN: scalar set without junction (research §Anti-Patterns)
    // ...
};
```
**After:** keep scalar mirror via the helper. After `CreateAsync` succeeds (`:280`), call `await SyncUserUnitsAsync(user, model.Units ?? new(), model.PrimaryUnit);` then `await _userManager.UpdateAsync(user)` (to persist mirror) + `_context.SaveChangesAsync()`. Add per-unit validation `u ∈ GetUnitsForSectionAsync(Section)` (loop over `model.Units`) before `ModelState.IsValid` check — extend the existing single-unit validation at `:229-236`.

**Validation analog to extend** (`WorkerController.cs:229-236`):
```csharp
else if (!string.IsNullOrEmpty(model.Unit))
{
    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
    if (!validUnits.Contains(model.Unit))
        ModelState.AddModelError("Unit", $"Unit '{model.Unit}' tidak valid untuk bagian '{model.Section}'");
}
```
> ViewBag re-populate on error already correct (`:243-244`). Keep `ViewBag.SectionUnitsJson`.

---

### `Controllers/WorkerController.cs` — `EditWorker` POST (`:346-481`)

**Analog:** itself — scalar audit at `:400-413` (THE line to replace), `UpdateAsync` at `:437`, audit emit at `:464-477`.

**Anti-pattern to replace** (`WorkerController.cs:405-413`):
```csharp
if (user.Section != model.Section) changes.Add($"Section: '{user.Section}' → '{model.Section}'");
if (user.Unit != model.Unit) changes.Add($"Unit: '{user.Unit}' → '{model.Unit}'");  // ← D-12: REPLACE w/ set-diff
// ...
user.Section = model.Section;
user.Unit = model.Unit;   // ← never set Unit directly; go through SyncUserUnitsAsync
```
**After:** keep `Section` scalar line; remove the `Unit` audit + assignment lines; instead, after `UpdateAsync` (`:437`) merge `SyncUserUnitsAsync`'s returned diff into `changes`. The audit emit (`:464-477`) already logs `string.Join("; ", changes)` — no change needed there.

**Audit emit analog (unchanged)** (`WorkerController.cs:469-475`):
```csharp
await _auditLog.LogAsync(actor?.Id ?? "", actorName, "EditWorker",
    $"Updated user '{model.FullName}' ({model.Email}). Changes: {(changes.Any() ? string.Join("; ", changes) : "none")}",
    null, "ApplicationUser");
```

**MU-07 guard — insert before SyncUserUnitsAsync.** Reuse the deactivate pattern below; query active PTA per `CDPController.cs:75`/`:398` (`a.CoacheeId == user.Id && a.IsActive`) and active mapping per `CDPController.cs:944` (`m.CoacheeId == ... && m.IsActive`).
```csharp
var oldUnits = await _context.UserUnits.Where(uu => uu.UserId == user.Id).Select(uu => uu.Unit).ToListAsync();
var removed  = oldUnits.Except(model.Units ?? new()).ToList();
if (removed.Any())
{
    var activeMapping = await _context.CoachCoacheeMappings
        .FirstOrDefaultAsync(m => m.CoacheeId == user.Id && m.IsActive);          // CDPController.cs:944 idiom
    var hasActivePta  = await _context.ProtonTrackAssignments
        .AnyAsync(a => a.CoacheeId == user.Id && a.IsActive);                     // CDPController.cs:75,398 idiom

    // HARD-BLOCK (D-11): PROTON active + the removed unit is the resolved PROTON unit
    //   (a) AssignmentUnit != null && ∈ removed; OR (b) AssignmentUnit == null && oldPrimary ∈ removed (fallback)
    // ... ModelState.AddModelError("", "Tidak bisa menghapus Unit ... PROTON tahun-berjalan aktif ...") + return View(model);

    // AUTO-DEACTIVATE-after-confirm (D-10): active mapping AssignmentUnit ∈ removed, no related PTA
    if (activeMapping?.AssignmentUnit != null && removed.Contains(activeMapping.AssignmentUnit))
    {
        if (!model.ConfirmedDeactivate)
        {
            model.ImpactedMappings = new() { $"Mapping coach aktif unit '{activeMapping.AssignmentUnit}' akan dinonaktifkan" };
            ViewBag.NeedConfirm = true;        // view shows modal + hidden ConfirmedDeactivate=true
            return View(model);
        }
        // confirmed → reuse CoachCoacheeMappingDeactivate logic IN the same tx
        activeMapping.IsActive = false; activeMapping.EndDate = DateTime.UtcNow;
    }
}
```
> Add `bool ConfirmedDeactivate` + `List<string> ImpactedMappings` to `ManageUserViewModel`. Open Q1/Q2: guard BOTH PROTON resolution branches AND the "0 units" case before allowing empty set.

---

### `Controllers/WorkerController.cs` — MU-07 auto-deactivate (reuse `CoachCoacheeMappingDeactivate`)

**Analog (the canonical deactivate, tx + cascade PTA + audit):** `CoachMappingController.cs:959-982`:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    mapping.IsActive = false;
    mapping.EndDate = DateTime.UtcNow;
    var deactivationTime = mapping.EndDate.Value;
    var activeAssignments = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive).ToListAsync();
    foreach (var a in activeAssignments) { a.IsActive = false; a.DeactivatedAt = deactivationTime; }
    int cascadeCount = activeAssignments.Count;
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    await _auditLog.LogAsync(actor.Id, actor.FullName, "Deactivate",
        $"Deactivated coach-coachee mapping #{id} — {cascadeCount} ProtonTrackAssignment(s) also deactivated",
        targetId: id, targetType: "CoachCoacheeMapping");
}
```
> For MU-07 D-10 path, replicate the IsActive/EndDate set + audit INSIDE the EditWorker transaction (do NOT cascade PTA here — D-11 hard-blocks the PTA case, so only the no-PTA mapping reaches auto-deactivate).

---

### `Controllers/WorkerController.cs` — `Import` (`:915-1040+`)

**Analog:** itself — parse at `:944`, validation at `:976-983`, user-create at `:1018-1037`.

**Current parse+validate** (`WorkerController.cs:944, 976-983`):
```csharp
var unit = (row.Cell(6).GetString() ?? "").Trim();
// ...
if (!string.IsNullOrWhiteSpace(unit))
{
    if (string.IsNullOrWhiteSpace(bagian))
        errors.Add("Unit tidak boleh diisi tanpa Section");
    else if (sectionUnitsDict.TryGetValue(bagian, out var validUnits) && !validUnits.Contains(unit))
        errors.Add($"Unit '{unit}' bukan child dari Section '{bagian}'");
}
```
**After (D-04/D-05 pipe-split, first=primary, backward-compat):**
```csharp
var unitCell = (row.Cell(6).GetString() ?? "").Trim();   // Cell(6) position UNCHANGED (D-04)
var unitList = unitCell.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Distinct(StringComparer.OrdinalIgnoreCase).ToList();   // first = primary
if (unitList.Any())
{
    if (string.IsNullOrWhiteSpace(bagian))
        errors.Add("Unit tidak boleh diisi tanpa Section");
    else if (sectionUnitsDict.TryGetValue(bagian, out var validUnits))
    {
        var invalid = unitList.Where(u => !validUnits.Contains(u)).ToList();   // PER-unit validation (MU-05)
        if (invalid.Any())
            errors.Add($"Unit tidak valid untuk '{bagian}': {string.Join(", ", invalid)}");
    }
}
// after CreateAsync succeeds (:1035): await SyncUserUnitsAsync(newUser, unitList, unitList.FirstOrDefault());
// keep newUser.Unit mirror = unitList.FirstOrDefault() at :1027 (or let SyncUserUnitsAsync set it)
```
> "UnitA" with no pipe → 1 element = primary (D-05 backward-compat). Update the import template/help text (D-06).

---

### `Controllers/WorkerController.cs` — `Export` (`:180-187`, excel-export)

**Analog:** itself — `WorkerController.cs:185-187`:
```csharp
ws.Cell(i + 2, 6).Value = u.Section ?? "-";
ws.Cell(i + 2, 7).Value = u.Unit ?? "-";    // ← replace w/ primary-first comma-join of all units
ws.Cell(i + 2, 8).Value = u.IsActive ? "Active" : "Inactive";
```
**After (primary-first comma text, SAME column 7, D-08):**
```csharp
string unitsText = (u.Units == null || !u.Units.Any())
    ? "-"
    : string.Join(", ", u.Units.OrderByDescending(x => x == u.PrimaryUnit).ThenBy(x => x));
ws.Cell(i + 2, 7).Value = unitsText;
```
> The export iterates worker DTOs — planner must populate `Units`/`PrimaryUnit` per worker from `UserUnits` (batch-load to avoid N+1; group by `UserId`). No star/"Utama" in Excel — ordering is the only primary signal (D-08).

---

### `Controllers/AccountController.cs` — `Profile`/`Settings` GET (`:140-206`)

**Analog:** itself — `:149-167` (Profile VM) and `:183-203` (Settings VM), both with nested `PSign = new PSignViewModel { ... Unit = user.Unit }`.

**Current populate** (`AccountController.cs:157-166`):
```csharp
Section = user.Section,
Unit = user.Unit,
// ...
PSign = new PSignViewModel { FullName = user.FullName, Position = user.Position, Unit = user.Unit }
```
**After:** load `var userUnits = await _context.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();` then set `Units = userUnits.Select(x => x.Unit).ToList()`, `PrimaryUnit = userUnits.FirstOrDefault(x => x.IsPrimary)?.Unit` on both the page VM and the nested `PSignViewModel`. Keep scalar `Unit = user.Unit` (mirror) for any unmigrated reader.
> `AccountController` does NOT currently inject `ApplicationDbContext` for these actions in the snippet — planner verifies `_context` is available (it is used elsewhere in the controller) or injects it.

---

### ViewModels — `ManageUserViewModel` / `ProfileViewModel` / `SettingsViewModel` / `PSignViewModel`

**Analog (scalar fields to extend):**
- `ManageUserViewModel.cs:33-37`: `public string? Section { get; set; }` / `public string? Unit { get; set; }`
- `ProfileViewModel.cs:14-15`: `public string? Section { get; set; }` / `public string? Unit { get; set; }`
- `SettingsViewModel.cs:19-21`: `Section` / `Directorate` / `Unit` (all `string?`)
- `PSignViewModel.cs:10-12`: `public string? Unit { get; set; }`

**Add to each (Section STAYS scalar — invariant #1):**
```csharp
public List<string> Units { get; set; } = new();
public string? PrimaryUnit { get; set; }
```
**`ManageUserViewModel` additionally needs (MU-07 round-trip, research §MU-07):**
```csharp
public bool ConfirmedDeactivate { get; set; }
public List<string> ImpactedMappings { get; set; } = new();
```
**`PSignViewModel` additionally:** `public List<string>? Units { get; set; }` (keep scalar `Unit` for fallback).

---

### Views — Multi-select widget: `Views/Admin/CreateWorker.cshtml` + `EditWorker.cshtml`

**Analog (single-select to replace):** `CreateWorker.cshtml:119-124`:
```html
<div class="col-md-6">
    <label asp-for="Unit" class="form-label fw-semibold"></label>
    <select asp-for="Unit" class="form-select" id="unitSelect">
        <option value="">-- Pilih @OrgLabels.GetLabel(1) --</option>
    </select>
</div>
```
**Analog (cascade init to extend):** `CreateWorker.cshtml:196-203`:
```html
<script src="~/js/shared-cascade.js"></script>
<script>
    initSectionUnitCascade({
        sectionUnits: @Html.Raw(ViewBag.SectionUnitsJson ?? "{}"),
        sectionId: 'sectionSelect', unitId: 'unitSelect',
        currentSection: "@(Model.Section ?? "")", currentUnit: "@(Model.Unit ?? "")"
    });
</script>
```
**After (DOM per UI-SPEC §A — Section `<select>` UNCHANGED, Unit → bordered checkbox group):**
```html
<div class="col-md-6">
  <label class="form-label fw-semibold">@OrgLabels.GetLabel(1) Penugasan</label>
  <div id="unitMultiContainer" class="border rounded p-2" style="max-height:220px;overflow-y:auto;"
       role="group" aria-label="Daftar @OrgLabels.GetLabel(1) penugasan">
    <span class="text-muted small">Pilih @OrgLabels.GetLabel(0) dahulu untuk menampilkan daftar @OrgLabels.GetLabel(1).</span>
  </div>
  <div class="form-text">Centang setiap @OrgLabels.GetLabel(1) ... Pilih satu sebagai @OrgLabels.GetLabel(1) Utama.</div>
</div>
```
Init call swaps to the new variant:
```html
<script>
    initSectionUnitMultiCascade({
        sectionUnits: @Html.Raw(ViewBag.SectionUnitsJson ?? "{}"),
        sectionId: 'sectionSelect', containerId: 'unitMultiContainer',
        currentSection: "@(Model.Section ?? "")",
        selectedUnits: @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.Units ?? new())),
        primaryUnit: "@(Model.PrimaryUnit ?? "")"
    });
</script>
```
> Form-control idiom (`form-select`, `form-label fw-semibold`) preserved (`CreateWorker.cshtml:114-123`). Submit button "Simpan Pekerja" unchanged (`:188`). Lesson Phase 354: this dynamic widget MUST be Playwright-runtime-verified.

---

### Views — Display badges: `WorkerDetail.cshtml` / `ManageWorkers.cshtml` / `Profile.cshtml` / `Settings.cshtml` / `Home/Index.cshtml`

**Analog (semantic badge `bg-*-10 text-*`):** `WorkerDetail.cshtml:91-98`:
```html
@if (!string.IsNullOrEmpty(Model.Section))
{
    <span class="badge bg-primary bg-opacity-10 text-primary">@Model.Section</span>
}
else { <span class="text-muted">-</span> }
```
**Analog (table-cell fallback):** `ManageWorkers.cshtml:262`:
```html
<td class="p-3"><small class="text-muted">@(user.Unit ?? "-")</small></td>
```
**Analog (italic empty fallback):** `Profile.cshtml:80,86` → `<span class="text-muted fst-italic">Belum diisi</span>` (D-09).

**Code to write (per UI-SPEC §B — primary-first, green primary badge + star + "Utama"):**
```razor
@if (Model.Units != null && Model.Units.Any())
{
    @foreach (var u in Model.Units.OrderByDescending(x => x == Model.PrimaryUnit).ThenBy(x => x))
    {
        if (u == Model.PrimaryUnit)
        {
            <span class="badge bg-success bg-opacity-10 text-success me-1">
                <i class="bi bi-star-fill me-1" aria-hidden="true"></i>@u <span class="visually-hidden">(Utama)</span>Utama
            </span>
        }
        else
        {
            <span class="badge bg-secondary bg-opacity-25 text-dark me-1">@u</span>
        }
    }
}
else
{
    <span class="text-muted fst-italic">Belum diisi</span>   @* table cells render "-" instead (D-09) *@
}
```
> WorkerDetail unit row to replace: `:101-104`. ManageWorkers cell: `:262` (render `-` when empty, not the italic panel — match existing density). Accent green reserved for primary ONLY (UI-SPEC Color). Never color-alone: pair with star + "Utama" text.

---

### View — `Views/Shared/_PSign.cshtml` (`:40-43`, print/cert card)

**Analog:** itself — `_PSign.cshtml:40-43`:
```razor
@if (!string.IsNullOrEmpty(Model.Unit))
{
    <div class="psign-label">@Model.Unit</div>
}
```
**After (D-07 — ALL units, primary-first comma-join, NO badges in print):**
```razor
@if (Model.Units != null && Model.Units.Any())
{
    <div class="psign-label">@string.Join(", ", Model.Units.OrderByDescending(u => u == Model.PrimaryUnit).ThenBy(u => u))</div>
}
```
> **D-07 LOCKED: do NOT revert to primary-only.** No row when empty (preserve existing). If rendered from a WorkerController context, use absolute path `~/Views/Shared/_PSign.cshtml` (Pitfall 5 — controller overrides `View()` to `~/Views/Admin/`, `:34-37`).

---

### `wwwroot/js/shared-cascade.js` — `initSectionUnitMultiCascade` (NEW fn)

**Analog:** `shared-cascade.js:13-49` `initSectionUnitCascade` (same file — extend, do NOT replace; `togglePassword` at `:56-66` stays).

**Analog excerpt** (`shared-cascade.js:28-44` `updateUnits`):
```javascript
function updateUnits(section) {
    unitSelect.innerHTML = '<option value="">-- Pilih Unit --</option>';
    if (section && sectionUnits[section]) {
        sectionUnits[section].forEach(function (unit) {
            var opt = document.createElement('option');
            opt.value = unit; opt.textContent = unit;
            if (unit === currentUnit) opt.selected = true;
            unitSelect.appendChild(opt);
        });
    }
}
```
**Code to write (checkbox-list + primary radio per UI-SPEC §A state machine):**
```javascript
function initSectionUnitMultiCascade(opts) {
    var sectionSelect = document.getElementById(opts.sectionId);
    var container = document.getElementById(opts.containerId);
    var sectionUnits = opts.sectionUnits || {};
    var selected = opts.selectedUnits || [];
    var primary  = opts.primaryUnit || '';
    if (!sectionSelect || !container) return;

    function render(section) {
        container.innerHTML = '';
        var units = (sectionUnits[section] || []);
        if (!units.length) {
            container.innerHTML = '<span class="text-muted small">Pilih Bagian dahulu untuk menampilkan daftar Unit.</span>';
            return;
        }
        units.forEach(function (unit, i) {
            var checked = selected.indexOf(unit) >= 0;
            var isPrim  = unit === primary;
            var row = document.createElement('div');
            row.className = 'd-flex align-items-center gap-2 mb-1';
            row.innerHTML =
              '<input type="checkbox" name="Units" value="'+unit+'" id="uu-chk-'+i+'" class="form-check-input uu-check mt-0"'+(checked?' checked':'')+'>' +
              '<input type="radio" name="PrimaryUnit" value="'+unit+'" id="uu-prim-'+i+'" class="form-check-input uu-primary mt-0"'+(isPrim?' checked':'')+(checked?'':' disabled')+'>' +
              '<label for="uu-chk-'+i+'" class="form-check-label small flex-grow-1">'+unit+'</label>' +
              '<label for="uu-prim-'+i+'" class="form-check-label small text-success">Utama</label>';
            container.appendChild(row);
        });
        // wire (UI-SPEC §A): checkbox change → enable/disable+clear its radio; ≥1 checked & no primary → default first
    }
    if (opts.currentSection) { sectionSelect.value = opts.currentSection; render(opts.currentSection); }
    sectionSelect.addEventListener('change', function () { selected = []; primary = ''; render(this.value); });
}
```
> Model-binding: many `name="Units"` checkboxes → `List<string> Units`; single `name="PrimaryUnit"` radio → `string? PrimaryUnit` (standard MVC, A1 — verify with Playwright/manual UAT). Changing Bagian on Edit clears prior selections (units must belong to new Bagian, invariant #1).

---

## Shared Patterns

### Validation: `Unit ∈ Bagian` (every junction-write — MU-05)
**Source:** `ApplicationDbContext.cs:106-116` `GetUnitsForSectionAsync(section)` (+ `GetSectionUnitsDictAsync` `:118-133` for import).
**Apply to:** CreateWorker POST, EditWorker POST, Import (loop). Reject any unit not in the section's child set; reject `PrimaryUnit ∉ checked set`.
```csharp
var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
var invalid = (model.Units ?? new()).Where(u => !validUnits.Contains(u)).ToList();
if (invalid.Any()) ModelState.AddModelError("Units", $"Unit tidak valid untuk '{model.Section}': {string.Join(", ", invalid)}");
```

### Cascade dict population (no AJAX — D-01)
**Source:** `WorkerController.cs:203-204` (Create) / `:337-338` (Edit) → `ViewBag.SectionUnitsJson`.
**Apply to:** Create/Edit GET + error re-render (`:243-244`, `:379-380`). Already present — keep when swapping to the multi-cascade widget.
```csharp
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
```

### Transaction (atomic write-through + MU-07 deactivate)
**Source:** `CoachMappingController.cs:959-979` + `WorkerController.cs:563` (DeleteWorker).
**Apply to:** `SyncUserUnitsAsync` + MU-07 auto-deactivate (one tx).
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try { /* ... */ await _context.SaveChangesAsync(); await transaction.CommitAsync(); }
catch { /* rollback implicit on dispose */ throw; }
```

### Audit log (set-diff — D-12)
**Source:** `Services/AuditLogService.cs:21-42` `LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` (SaveChanges internal). Usage idiom: `WorkerController.cs:464-477`.
**Apply to:** Create/Edit/Import — feed `SyncUserUnitsAsync`'s returned `List<string>` diff into the existing `changes` join. Also log the auto-deactivate event (D-12).
```csharp
await _auditLog.LogAsync(actor?.Id ?? "", actorName, "EditWorker",
    $"... Changes: {string.Join("; ", changes)}", null, "ApplicationUser");
```

### Authorization (do NOT relax)
**Source:** `[Authorize(Roles = "Admin, HC")]` on every mutation (`WorkerController.cs:196,210,344,896`) + `[ValidateAntiForgeryToken]` on POST (`:211,345,897`).
**Apply to:** all new/modified endpoints. **Authz Section (`IsResultsAuthorized`, `CMPController.cs:2503-2511`) is 100% scalar — 0 changes** (de-risk: Section stays scalar).

### Active-record query idioms (MU-07 guard)
**Source:** active PTA `CDPController.cs:75,398` (`a.CoacheeId == X && a.IsActive`); active mapping `CDPController.cs:944` (`m.CoacheeId == X && m.IsActive`).
**Apply to:** EditWorker/Import remove-unit guard. Note: `ProtonTrackAssignment` has NO Unit column (Pitfall 4) — resolve PROTON unit via active mapping `AssignmentUnit ?? User.Unit`.

---

## No Analog Found

None. Every file maps to an existing repo pattern. The only genuinely-new *shapes* (`UserUnit` entity, the `AddUserUnitsTable` migration, `SyncUserUnitsAsync`, the JS multi-cascade variant) each copy a verified analog rather than inventing structure. Researcher's "~80% reuse" is corroborated.

---

## Metadata

**Analog search scope:** `Models/`, `Data/`, `Controllers/`, `Views/Admin/`, `Views/Account/`, `Views/Shared/`, `Views/Home/`, `Migrations/`, `wwwroot/js/`, `Services/`
**Files scanned (analogs read):** `CoachCoacheeMapping.cs`, `ApplicationDbContext.cs`, `WorkerController.cs`, `CoachMappingController.cs`, `AccountController.cs`, `CDPController.cs` (grep), `shared-cascade.js`, `AuditLogService.cs`, `ManageUserViewModel.cs`, `ProfileViewModel.cs`, `SettingsViewModel.cs`, `PSignViewModel.cs`, `CreateWorker.cshtml`, `WorkerDetail.cshtml`, `ManageWorkers.cshtml`, `_PSign.cshtml`, `Migrations/20260603012335_AddOrganizationLevelLabel.cs`
**Pattern extraction date:** 2026-06-18
