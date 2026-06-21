# Phase 399: Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server / SQLEXPRESS) — junction table + filtered-unique index + write-through mirror + Razor multi-select UI + ClosedXML import
**Confidence:** HIGH (semua klaim ditambatkan ke file:line di codebase ini; pola yang dipakai sudah ada di repo)

## Summary

Phase 399 menambah satu tabel junction `UserUnits` (model + DbSet + index filtered-unique primary + migration `AddUserUnitsTable` + backfill) lalu memasang kontrak **write-through primary-mirror** di `WorkerController` (Create/Edit/Import) sehingga `ApplicationUser.Unit` selalu = baris `UserUnits.IsPrimary`. Semua surface display (Profile, Settings, WorkerDetail, ManageWorkers, Excel export, Home dashboard, `_PSign`) diperluas menampilkan seluruh unit pekerja dengan primary ditandai. Ditambah validasi tiap junction-write `Unit ∈ unit-Bagian`, audit set-diff, dan guard hapus-unit (MU-07) asimetris.

Kabar baik untuk planner: **hampir semua pola yang dibutuhkan sudah ada di codebase**. Filtered-unique index sudah dipakai 2x (`IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`, `IX_AssessmentSessions_NomorSertifikat_Unique`) — tinggal disalin. Junction NAME-string + `IsActive` sudah jadi bentuk `CoachCoacheeMapping`. `GetSectionUnitsDictAsync()`/`GetUnitsForSectionAsync()` sudah jadi primitif validasi+cascade. `shared-cascade.js` sudah ada (tinggal ditambah varian checkbox-list). Auto-deactivate mapping (MU-07 jalur coach) sudah persis ada di `CoachCoacheeMappingDeactivate` (`CoachMappingController.cs:947-982`). Authz Section (`IsResultsAuthorized` `CMPController.cs:2503-2511`) **dikonfirmasi 100% scalar** — 0 perubahan (de-risk terbesar tervalidasi).

**Primary recommendation:** Ikuti pola junction+index `CoachCoacheeMapping` byte-demi-byte untuk `UserUnit`; pusatkan write-through di satu helper privat `SyncUserUnitsAsync(user, units, primaryUnit)` di `WorkerController` yang dipanggil Create/Edit/Import; backfill via migration `Up` raw SQL idempotent (`INSERT...SELECT WHERE NOT EXISTS`); MU-07 via server round-trip re-prompt flag. ⚠️ **Blocker lingkungan yang HARUS diatasi planner:** `dotnet ef` CLI global = **v10.0.3** sementara project = **net8.0 / EF Core 8.0.0** — versi mismatch dapat menggagalkan `dotnet ef migrations add` (lihat Environment Availability + Pitfall 1).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Penyimpanan multi-unit (`UserUnits`) | Database / Storage | API/Backend | Junction table + filtered-unique index = DB enforce 1-primary; backfill = data migration |
| Write-through primary-mirror | API/Backend (`WorkerController`) | Database | Kontrak transaksional: tulis junction + mirror scalar dalam 1 SaveChanges/tx |
| Validasi `Unit ∈ Bagian` | API/Backend | Database | `GetUnitsForSectionAsync` query DB; enforce di setiap write |
| Multi-select widget + primary radio | Browser/Client (`shared-cascade.js`) | Frontend Server (Razor render) | Cascade client-side dari `ViewBag.SectionUnitsJson` (no AJAX, D-01) |
| Display semua unit | Frontend Server (Razor) | API/Backend (VM populate) | Render badge/chip; VM diisi controller dari `UserUnits` |
| Import parse pipe-delimited | API/Backend (ClosedXML) | — | Server-side split+validate per baris |
| Guard hapus-unit (MU-07) | API/Backend | Database | Query `CoachCoacheeMapping`/`ProtonTrackAssignment` aktif; tx auto-deactivate atau hard-block |

## Standard Stack

### Core (sudah terpasang — JANGAN tambah dependency baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 `[VERIFIED: HcPortal.csproj:18]` | ORM + migration + filtered index | Sudah dipakai seluruh data layer |
| Microsoft.EntityFrameworkCore.Tools / Design | 8.0.0 `[VERIFIED: HcPortal.csproj:19,23]` | `dotnet ef migrations add` | Design-time migration scaffold |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 `[VERIFIED: HcPortal.csproj:16]` | `ApplicationUser : IdentityUser` (FK `UserId → AspNetUsers.Id`) | User store |
| ClosedXML | 0.105.0 `[VERIFIED: HcPortal.csproj:14]` | Import/Export Excel (`row.Cell(n).GetString()`) | Sudah dipakai Import+Export Worker |
| Bootstrap 5 + Bootstrap Icons | (CDN/static) `[VERIFIED: CreateWorker.cshtml `bi bi-*` classes]` | Badge/chip styling, form controls | Idiom UI seragam |

### Supporting (test only)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xunit | 2.9.3 `[VERIFIED: HcPortal.Tests.csproj:15]` | Unit/integration test | Validation Architecture tests |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 `[VERIFIED: HcPortal.Tests.csproj:12]` | Unit test DB | Write-through/set-diff/parse logic (⚠️ TIDAK enforce filtered-unique-index) |
| Microsoft.EntityFrameworkCore.SqlServer (test) | 8.0.0 `[VERIFIED: HcPortal.Tests.csproj:13]` | Real-SQL integration | Filtered-unique enforce — **deferred ke Phase 404** (QA-01) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Junction NAME-string (`UserUnits.Unit`) | FK ke `OrganizationUnit.Id` | Spec §2 LOCKED NAME-string (konsisten `AssignmentUnit`/`ProtonKompetensi.Unit`, cascade minimal). JANGAN re-decide. |
| Checkbox-list + primary radio | `<select multiple>` / chip-tag input | D-03 LOCKED checkbox-list (reuse dict, primary eksplisit, no lib). JANGAN re-decide. |
| Backfill migration `Up` raw SQL | `SeedData.cs` idempotent | Claude's Discretion — **lean Up** (lihat Pattern 3). |

**Installation:** Tidak ada — semua dependency sudah terpasang.

**Version verification:**
`[VERIFIED: HcPortal.csproj]` — EF Core 8.0.0, ClosedXML 0.105.0, Identity.EF 8.0.0, TargetFramework `net8.0`.
`[VERIFIED: dotnet --version → 8.0.418]`, `[VERIFIED: dotnet ef --version → 10.0.3]` ⚠️ — global EF CLI mismatch (lihat Pitfall 1).

## Architecture Patterns

### System Architecture Diagram (alur data write-through)

```
┌─────────────── CREATE / EDIT WORKER (form) ──────────────┐
│  Browser: shared-cascade.js (checkbox-list + primary radio)│
│   pilih Bagian → render unit checkboxes dari               │
│   ViewBag.SectionUnitsJson  →  centang unit + 1 primary     │
└──────────────────────┬─────────────────────────────────────┘
                       │ POST: model.Units[], model.PrimaryUnit
                       ▼
┌──────────── WorkerController.CreateWorker/EditWorker ──────┐
│ 1. Validasi: tiap u ∈ GetUnitsForSectionAsync(Section)     │
│ 2. [EDIT only] MU-07 guard: hitung removedUnits            │
│      ├─ ada ProtonTrackAssignment aktif di unit? → BLOCK    │
│      └─ ada CoachCoacheeMapping.AssignmentUnit aktif?       │
│           → confirmedDeactivate flag? ──no──► re-prompt     │
│                                        └─yes─► deactivate   │
│ 3. SyncUserUnitsAsync(user, units, primary):  (TX)         │
│      - hapus baris UserUnits lama, tulis baru (1 IsPrimary) │
│      - user.Unit = primaryUnit   (MIRROR — invariant #3)    │
│      - audit set-diff (added/removed/primary-changed)       │
└──────────────────────┬─────────────────────────────────────┘
                       ▼
        ┌───── DB: UserUnits (filtered-unique WHERE IsPrimary=1) ─────┐
        │      Users.Unit (mirror, denormalized)                       │
        └──────────────────────────────────────────────────────────────┘
                       │
                       ▼  (READ — semua surface jalan via mirror SAMPAI fase 400+)
   Profile / Settings / WorkerDetail / ManageWorkers / Excel / Home / _PSign
   → Phase 399 ubah supaya baca UserUnits (semua unit, primary ditandai)
```

```
┌──────────── IMPORT WORKER (Excel, Cell(6)=Unit) ───────────┐
│ row.Cell(6).GetString() = "UnitA|UnitB|UnitC"              │
│   split('|') → trim → dedup → first = primary               │
│   tiap unit validasi ∈ sectionUnitsDict[bagian]            │
│   → SyncUserUnitsAsync(newUser, units, units[0])           │
└────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure (file yang disentuh)
```
Models/
├── UserUnit.cs                # BARU — entity junction (per spec §4.1)
├── ManageUserViewModel.cs     # + List<string> Units, string? PrimaryUnit  (:33-37)
├── ProfileViewModel.cs        # + List<string> Units, string? PrimaryUnit  (:14-15)
├── SettingsViewModel.cs       # + List<string> Units, string? PrimaryUnit  (:19-21)
└── PSignViewModel.cs          # + List<string>? Units (primary-first join)  (:11)
Data/
├── ApplicationDbContext.cs    # + DbSet<UserUnit> (:38-area) + config (:335-area)
└── SeedData.cs                # (opsi backfill — TIDAK dipilih; lean migration Up)
Migrations/
└── <ts>_AddUserUnitsTable.cs  # BARU — CreateTable + filtered index + backfill SQL
Controllers/
├── WorkerController.cs        # Create:269 / Edit:405-413 / Import:1026-1027 / Export:186-187 / ManageWorkers / WorkerDetail
└── AccountController.cs       # Profile:149-167 / Settings:183-203 (populate Units)
Views/Admin/
├── CreateWorker.cshtml        # Unit single-select → checkbox-list + primary radio (:119-124,196-203)
├── EditWorker.cshtml          # idem (:124-129)
├── WorkerDetail.cshtml        # display semua unit (:101-104)
└── ManageWorkers.cshtml       # display semua unit (:262)
Views/Account/
├── Profile.cshtml             # display semua unit (:83-88) + _PSign (:126)
└── Settings.cshtml            # display semua unit (:112-120) + _PSign (:133)
Views/Shared/
└── _PSign.cshtml              # primary-first comma-join (:40-43)
Views/Home/
└── Index.cshtml               # display semua unit (:39)
wwwroot/js/
└── shared-cascade.js          # + initSectionUnitMultiCascade(opts) varian (:13-49)
```

### Pattern 1: Entity junction NAME-string + IsActive (salin dari `CoachCoacheeMapping`)
**What:** `UserUnit` mengikuti bentuk `CoachCoacheeMapping` (int Id, string UserId, string Unit-name, bool IsActive) `[VERIFIED: Models/CoachCoacheeMapping.cs:7-51]`. Spec §4.1 menambah `IsPrimary` + nav `User`.
**When to use:** Definisi model baru.
**Example:**
```csharp
// Source: spec §4.1 + pola Models/CoachCoacheeMapping.cs:7-24
namespace HcPortal.Models
{
    public class UserUnit
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";   // FK → AspNetUsers.Id, ON DELETE CASCADE
        public string Unit { get; set; } = "";       // NAME string, anak dari Section pekerja
        public bool IsPrimary { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public ApplicationUser? User { get; set; }
    }
}
```
DbSet ditambah di `[VERIFIED: Data/ApplicationDbContext.cs:32]` area (sejajar `CoachCoacheeMappings`):
```csharp
public DbSet<UserUnit> UserUnits { get; set; }
```

### Pattern 2: Filtered-unique index (salin PERSIS dari pola existing)
**What:** `OnModelCreating` config dengan `HasIndex(...).IsUnique().HasFilter(...)`. Dua preseden persis ada.
**When to use:** Enforce 1 primary/user di DB (invariant #3).
**Example (model config):**
```csharp
// Source: VERIFIED Data/ApplicationDbContext.cs:327-335 (CoachCoacheeMapping) + :225-229 (NomorSertifikat)
builder.Entity<UserUnit>(entity =>
{
    entity.HasOne(uu => uu.User)
          .WithMany()                          // tidak ada nav collection di ApplicationUser (boleh begitu)
          .HasForeignKey(uu => uu.UserId)
          .OnDelete(DeleteBehavior.Cascade);   // ON DELETE CASCADE (spec §4.1)

    entity.HasIndex(uu => uu.UserId);

    // Tepat 1 primary per user (filtered-unique) — pola identik IX_CoachCoacheeMappings_CoacheeId_ActiveUnique
    entity.HasIndex(uu => uu.UserId)
          .IsUnique()
          .HasFilter("[IsPrimary] = 1")
          .HasDatabaseName("IX_UserUnits_UserId_PrimaryUnique");

    // (Opsional, rekomendasi pasang — Claude's Discretion D) cegah duplikat unit/user
    entity.HasIndex(uu => new { uu.UserId, uu.Unit })
          .IsUnique()
          .HasDatabaseName("IX_UserUnits_UserId_Unit_Unique");
});
```
⚠️ **Catatan filter syntax SQL Server:** filter string pakai bracket kolom dan literal SQL — `"[IsPrimary] = 1"` (bool→bit), persis seperti `"[IsActive] = 1"` `[VERIFIED: ApplicationDbContext.cs:331]` dan `"[NomorSertifikat] IS NOT NULL"` `[VERIFIED: ApplicationDbContext.cs:228]`.

### Pattern 3: Migration add-table + filtered index + backfill (raw SQL `Up`)
**What:** `CreateTable` + `CreateIndex(unique, filter)` + `migrationBuilder.Sql(...)` backfill idempotent. Semua tiga sudah ada presedennya.
**When to use:** Migration `AddUserUnitsTable`.
**Example:**
```csharp
// Source: VERIFIED Migrations/20260603012335_AddOrganizationLevelLabel.cs:12-40 (CreateTable+Index)
//       + VERIFIED Migrations/20260308065109_AddAssignmentFieldsAndUniqueConstraint.cs:14-48 (Sql + filtered CreateIndex)
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "UserUnits",
        columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
            IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
            IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_UserUnits", x => x.Id);
            table.ForeignKey("FK_UserUnits_Users_UserId", x => x.UserId,
                "Users", "Id", onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateIndex("IX_UserUnits_UserId", "UserUnits", "UserId");

    // Filtered-unique primary — UserId harus NVARCHAR(450) (bukan MAX) agar bisa di-index.
    migrationBuilder.CreateIndex(
        name: "IX_UserUnits_UserId_PrimaryUnique",
        table: "UserUnits",
        column: "UserId",
        unique: true,
        filter: "[IsPrimary] = 1");

    // (opsional) cegah duplikat unit/user — Unit harus di-cap maxLength agar bisa di-index (≤450)
    // migrationBuilder.CreateIndex("IX_UserUnits_UserId_Unit_Unique", "UserUnits",
    //     new[] { "UserId", "Unit" }, unique: true);

    // BACKFILL idempotent (Claude's Discretion → lean Up). 1 primary-row/pekerja Unit non-null.
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
⚠️ **`Unit` column type:** untuk index opsional `(UserId, Unit)`, kolom `Unit` TIDAK boleh `nvarchar(max)` (SQL Server tolak index pada MAX). Jika pasang index opsional, set `Unit` `maxLength: 200` (`type: "nvarchar(200)"`). `UserId` sudah `nvarchar(450)` (default Identity key) — index-able. `[CITED: SQL Server index key column ≤ 1700 bytes; nvarchar(max) tak boleh jadi index key]`

### Pattern 4: Write-through helper terpusat (BARU — kill desync)
**What:** Satu helper privat di `WorkerController` dipanggil Create/Edit/Import; tx-wrapped seperti `DeleteWorker` `[VERIFIED: WorkerController.cs:563]` dan `CoachCoacheeMappingDeactivate` `[VERIFIED: CoachMappingController.cs:959-979]`.
**When to use:** Setiap mutasi multi-unit.
**Example:**
```csharp
// Pola tx: VERIFIED CoachMappingController.cs:959-979 (BeginTransactionAsync → SaveChanges → CommitAsync)
// Catatan: UserManager.CreateAsync/UpdateAsync TIDAK ikut transaksi DbContext kita secara otomatis;
//          untuk Edit, set user.Unit DULU sebelum UpdateAsync; untuk UserUnits pakai _context + tx terpisah,
//          ATAU panggil SyncUserUnitsAsync SETELAH UserManager sukses (sederhana, sesuai pola Create existing).
private async Task<List<string>> SyncUserUnitsAsync(
    ApplicationUser user, List<string> units, string? primaryUnit)
{
    var changes = new List<string>();                 // set-diff (D-12)
    var existing = await _context.UserUnits
        .Where(uu => uu.UserId == user.Id).ToListAsync();
    var oldSet = existing.Select(e => e.Unit).ToHashSet();
    var newSet = units.ToHashSet();

    foreach (var added in newSet.Except(oldSet))   changes.Add($"Unit +'{added}'");
    foreach (var removed in oldSet.Except(newSet)) changes.Add($"Unit -'{removed}'");

    // primary deterministik: jika primaryUnit kosong/invalid → first checked (D-02)
    var primary = (primaryUnit != null && newSet.Contains(primaryUnit))
        ? primaryUnit : units.FirstOrDefault();
    var oldPrimary = existing.FirstOrDefault(e => e.IsPrimary)?.Unit;
    if (oldPrimary != primary) changes.Add($"Primary: '{oldPrimary}' → '{primary}'");

    _context.UserUnits.RemoveRange(existing);          // replace-set (sederhana, dedup natural)
    foreach (var u in newSet)
        _context.UserUnits.Add(new UserUnit {
            UserId = user.Id, Unit = u, IsPrimary = (u == primary), IsActive = true });

    user.Unit = primary;                               // MIRROR (invariant #3); null bila 0 unit
    return changes;
}
```
⚠️ **Urutan SaveChanges vs filtered-unique:** karena replace-set (`RemoveRange` lama lalu `Add` baru) dilakukan dalam satu `SaveChangesAsync`, EF mengirim DELETE sebelum INSERT → tidak ada window 2-primary. Tetapi **InMemory tidak enforce filter** — test SQL-riil (Phase 404) wajib (Pitfall 4).

### Pattern 5: Multi-select cascade varian (extend `shared-cascade.js`)
**What:** Tambah fungsi `initSectionUnitMultiCascade(opts)` di samping `initSectionUnitCascade` existing `[VERIFIED: shared-cascade.js:13-49]`. Section TETAP single-select (`<select>`), Unit jadi container checkbox+radio yang di-render ulang saat Bagian berubah.
**When to use:** CreateWorker/EditWorker form.
**Example (struktur DOM yang disarankan — idiom Bootstrap 5):**
```html
<!-- Section tetap single (tidak berubah) -->
<select asp-for="Section" class="form-select" id="sectionSelect">
    <option value="">-- Pilih @OrgLabels.GetLabel(0) --</option>
</select>

<!-- Unit jadi container multi (ganti <select id="unitSelect">) -->
<div id="unitMultiContainer" class="border rounded p-2">
    <span class="text-muted small">Pilih Bagian dahulu</span>
</div>
```
```javascript
// Source: extend pola shared-cascade.js:28-44 (updateUnits)
function initSectionUnitMultiCascade(opts) {
    var sectionSelect = document.getElementById(opts.sectionId);
    var container = document.getElementById(opts.containerId);
    var sectionUnits = opts.sectionUnits || {};
    var selected = opts.selectedUnits || [];          // List<string> dari model
    var primary  = opts.primaryUnit || '';

    function render(section) {
        container.innerHTML = '';
        var units = sectionUnits[section] || [];
        units.forEach(function (unit, i) {
            var checked = selected.indexOf(unit) >= 0;
            var isPrim  = unit === primary;
            var row = document.createElement('div');
            row.className = 'd-flex align-items-center gap-2 mb-1';
            row.innerHTML =
              '<input type="checkbox" name="Units" value="'+unit+'" class="form-check-input uu-check"'+(checked?' checked':'')+'>' +
              '<input type="radio" name="PrimaryUnit" value="'+unit+'" class="form-check-input uu-primary"'+(isPrim?' checked':'')+(checked?'':' disabled')+'>' +
              '<label class="form-check-label small">'+unit+'</label>';
            container.appendChild(row);
        });
        // wire: checkbox uncheck → disable+clear radio; ≥1 checked & no primary → default first (D-02)
    }
    if (opts.currentSection) { sectionSelect.value = opts.currentSection; render(opts.currentSection); }
    sectionSelect.addEventListener('change', function () { render(this.value); });
}
```
**Catatan binding:** `name="Units"` (banyak checkbox) → MVC bind ke `List<string> Units`. `name="PrimaryUnit"` (radio, 1 value) → bind ke `string? PrimaryUnit`. Ini pola form-binding standar MVC, tidak butuh JS submit khusus. `[ASSUMED: model-binding List<string> dari checkbox bernama sama — standar ASP.NET Core MVC]`

### Anti-Patterns to Avoid
- **Scalar audit `if user.Unit != model.Unit`** `[VERIFIED: WorkerController.cs:406]` — ganti set-diff (D-12). Baris ini WAJIB dihapus/diganti.
- **Set `user.Unit = model.Unit` langsung tanpa sync junction** `[VERIFIED: WorkerController.cs:270,413,1027]` — selalu lewat `SyncUserUnitsAsync` (kill desync).
- **Native `<select multiple>` atau chip-input** — D-03 LOCKED checkbox-list.
- **Revert `_PSign` ke primary-only** — D-07 LOCKED tampil semua unit (primary-first join).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Validasi `Unit ∈ Bagian` | Manual loop OrganizationUnits | `_context.GetUnitsForSectionAsync(section)` `[VERIFIED: ApplicationDbContext.cs:106-116]` | Sudah handle ParentId hierarchy + IsActive + order |
| Cascade Bagian→Unit dict | Query baru per-Bagian | `_context.GetSectionUnitsDictAsync()` `[VERIFIED: ApplicationDbContext.cs:118-133]` + `ViewBag.SectionUnitsJson` `[VERIFIED: WorkerController.cs:203-204,337-338]` | Single dict, client-side, no AJAX (D-01) |
| Filtered-unique index | Trigger/check constraint manual | `HasIndex().IsUnique().HasFilter("[IsPrimary]=1")` `[VERIFIED: ApplicationDbContext.cs:330-333]` | SQL Server filtered index native |
| Auto-deactivate mapping (MU-07 coach) | Logika deactivate baru | Reuse pola `CoachCoacheeMappingDeactivate` `[VERIFIED: CoachMappingController.cs:962-982]` (IsActive=false + EndDate + cascade PTA + audit, tx) | Sudah atomic + audit + cascade PTA |
| Audit write | INSERT manual | `_auditLog.LogAsync(actorId, actorName, actionType, desc, targetId, targetType)` `[VERIFIED: Services/AuditLogService.cs:21-42]` | SaveChanges internal, pola dipakai semua controller |
| Excel cell read/write | Parsing manual | `row.Cell(n).GetString()` / `ws.Cell(r,c).Value` `[VERIFIED: WorkerController.cs:944,186]` (ClosedXML) | Sudah dipakai import+export |
| Transaksi atomic | try/catch manual rollback | `using var tx = await _context.Database.BeginTransactionAsync(); ... await tx.CommitAsync();` `[VERIFIED: WorkerController.cs:563; CoachMappingController.cs:959-979]` | Pola tx standar repo |

**Key insight:** Phase 399 ~80% reuse. Komponen genuinely baru hanya: model `UserUnit`, migration, `SyncUserUnitsAsync` helper, varian JS multi-cascade, dan render-loop display. Sisanya menyalin pola yang sudah terbukti di repo.

## Runtime State Inventory

> Phase 399 = menambah tabel + write-through + UI. Migration mengubah skema + backfill data. Inventory difokuskan ke apa yang harus dimigrasi vs apa yang aman.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `Users.Unit` (scalar) existing untuk setiap pekerja `[VERIFIED: ApplicationUser.cs:33]` — sumber backfill. ~N baris (1 per pekerja Unit non-null). | **Data migration:** backfill 1 baris `UserUnits{IsPrimary=1}` per pekerja Unit non-null (migration `Up` SQL, idempotent `WHERE NOT EXISTS`). Pekerja Unit null → 0 baris. |
| Live service config | Tidak ada — Phase 399 tidak menyentuh n8n/Datadog/external service. | None — verified by scope (spec §5 Fase 399 = Models/Controller/View/Migration only). |
| OS-registered state | Tidak ada — tidak ada Task Scheduler/pm2/service rename. | None — verified by scope. |
| Secrets/env vars | `Authentication:UseActiveDirectory` config dibaca `[VERIFIED: WorkerController.cs:215,448,847,922]` — TIDAK berubah (read-only, tidak di-rename). | None — verified by code (hanya dibaca, bukan di-rename). |
| Build artifacts | EF model snapshot (`Migrations/ApplicationDbContextModelSnapshot.cs`) AUTO-regenerate saat `dotnet ef migrations add`. `Designer.cs` baru di-generate. | **Auto** via `dotnet ef migrations add AddUserUnitsTable` (lihat Environment Availability — versi CLI mismatch). |

**Mirror desync risk:** baris `Users.Unit` lama TETAP dipertahankan sebagai mirror (spec §4.2, invariant #3). Backfill TIDAK mengosongkan `Users.Unit`. Pembaca scalar lama (~30+ site) jalan terus via mirror tanpa diubah di Phase 399 — migrasi pembaca = Phase 400+ (deferred).

## Common Pitfalls

### Pitfall 1: `dotnet ef` CLI versi 10.0.3 vs project EF Core 8.0.0
**What goes wrong:** `dotnet ef migrations add AddUserUnitsTable` bisa gagal/warn karena global tool `[VERIFIED: dotnet ef --version → 10.0.3]` lebih baru dari runtime assembly project (`net8.0` / EF Core 8.0.0 `[VERIFIED: HcPortal.csproj:4,18]`). EF tools umumnya kompatibel mundur untuk scaffold sederhana, tapi mismatch major (10 vs 8) berisiko.
**Why it happens:** Global `dotnet-ef` di-update ke v10; project belum.
**How to avoid:** Planner siapkan langkah verifikasi: (a) coba `dotnet ef migrations add AddUserUnitsTable` dari root project; bila gagal, (b) pin tool lokal `dotnet new tool-manifest && dotnet tool install dotnet-ef --version 8.0.* && dotnet tool run dotnet-ef migrations add ...`. **Alternatif aman:** tulis file migration `<ts>_AddUserUnitsTable.cs` + update `ApplicationDbContextModelSnapshot.cs` secara manual (pola Pattern 3 + salin entri index dari snapshot existing) — menghindari CLI sama sekali. CLAUDE.md gate `dotnet build` + `dotnet run` + cek DB lokal tetap WAJIB sebelum commit.
**Warning signs:** Error "The Entity Framework tools version 'X' is older/newer than..." atau snapshot tidak ter-update.

### Pitfall 2: nvarchar(max) tidak bisa jadi index key
**What goes wrong:** Index opsional `(UserId, Unit)` gagal apply bila `Unit` = `nvarchar(max)`.
**Why it happens:** SQL Server menolak index key column bertipe MAX (>1700 bytes).
**How to avoid:** Bila pasang index opsional, set `Unit` `maxLength: 200` di model (`[MaxLength(200)]`) + migration `type: "nvarchar(200)"`. `UserId` aman (Identity key = nvarchar(450)).
**Warning signs:** Migration apply error "Column ... in table ... is of a type that is invalid for use as a key column in an index."

### Pitfall 3: EF-InMemory tidak enforce filtered-unique index
**What goes wrong:** Test InMemory bisa lolos walau ada 2 baris `IsPrimary=1` per user → false green.
**Why it happens:** InMemory provider mengabaikan `HasFilter` `[CITED: docs.microsoft.com/ef-core — InMemory has no relational constraint enforcement]`.
**How to avoid:** Phase 399 test InMemory untuk LOGIC (set-diff, parse, mirror assignment, primary recompute). Test enforce filtered-unique = SQL-riil **deferred ke Phase 404** (QA-01) menggunakan fixture `OrgLabelMigrationFixture`-style `[VERIFIED: HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-48 — disposable HcPortalDB_Test_<guid> on localhost\SQLEXPRESS + MigrateAsync]`.
**Warning signs:** Test "tidak boleh 2 primary" hijau di InMemory tapi belum pernah dijalankan di SQL.

### Pitfall 4: ProtonTrackAssignment TIDAK punya kolom Unit (MU-07 hard-block resolution)
**What goes wrong:** MU-07 D-11 minta hard-block bila unit yang dihapus masih dirujuk `ProtonTrackAssignment` aktif — tapi `ProtonTrackAssignment` punya 7 kolom, **0 di antaranya Unit** `[VERIFIED: Models/ProtonModels.cs:71-87 — CoacheeId, AssignedById, ProtonTrackId, IsActive, AssignedAt, DeactivatedAt, Origin]`.
**Why it happens:** Unit PROTON di-resolve, tidak disimpan (spec §3): `AssignmentUnit (active CoachCoacheeMapping) ?? User.Unit`.
**How to avoid:** Untuk MU-07 hard-block, "unit PROTON aktif pekerja" = `AssignmentUnit` dari `CoachCoacheeMapping` aktif coachee **bila** ada `ProtonTrackAssignment` aktif. Query: ada `ProtonTrackAssignment` aktif (`CoacheeId==user.Id && IsActive` `[VERIFIED: CDPController.cs:75,398]`) DAN active mapping `AssignmentUnit ∈ removedUnits` → BLOCK. Jika tidak ada active mapping, unit PROTON jatuh ke primary `User.Unit` (fallback yang baru dibuang di Phase 401 — di Phase 399 fallback masih ada). **Planner harus eksplisitkan resolusi ini** karena tidak ada kolom langsung. Lihat Open Question 1.
**Warning signs:** Mencari `ProtonTrackAssignment.Unit` (tidak ada) → null-ref atau logika salah.

### Pitfall 5: Override `View()` di WorkerController butuh path absolut
**What goes wrong:** `WorkerController` override `View()` ke folder `~/Views/Admin/` `[VERIFIED: WorkerController.cs:34-37]`. PartialAsync dari controller folder-override butuh path absolut (lesson Phase 394).
**Why it happens:** Resolusi view custom.
**How to avoid:** Untuk render `_PSign` atau partial baru, gunakan path absolut `~/Views/Shared/_PSign.cshtml` bila dipanggil dari konteks WorkerController. Profile/Settings (AccountController) pakai resolusi normal.
**Warning signs:** "The view '_PSign' was not found."

## Code Examples

### Display semua unit (badge HTML) — surface Profile/WorkerDetail/ManageWorkers
```razor
@* Source: pola badge VERIFIED WorkerDetail.cshtml:91-98 (Section badge) + fallback :103 *@
@if (Model.Units != null && Model.Units.Any())
{
    @foreach (var u in Model.Units)
    {
        var isPrim = u == Model.PrimaryUnit;
        <span class="badge @(isPrim ? "bg-success" : "bg-secondary bg-opacity-25 text-dark") me-1">
            @u@(isPrim ? " ★" : "")
        </span>
    }
}
else
{
    <span class="text-muted fst-italic">Belum diisi</span>   @* fallback existing (D-09) *@
}
```

### Display semua unit (primary-first comma-join) — `_PSign` + Excel
```csharp
// Source: pola comma-join sederhana; primary-first ordering
// VM helper atau inline:
string UnitsText(List<string>? units, string? primary) =>
    units == null || !units.Any() ? "" :
    string.Join(", ", units.OrderByDescending(u => u == primary).ThenBy(u => u));
// Excel: ws.Cell(i+2, 7).Value = UnitsText(u.Units, u.PrimaryUnit) (ganti WorkerController.cs:186)
```
```razor
@* _PSign.cshtml ganti :40-43 *@
@if (Model.Units != null && Model.Units.Any())
{
    <div class="psign-label">@string.Join(", ", Model.Units.OrderByDescending(u => u == Model.PrimaryUnit))</div>
}
```

### Import parse pipe-delimited (extend WorkerController.cs:944,976-983)
```csharp
// Source: extend VERIFIED WorkerController.cs:944 (Cell(6)) + :976-983 (validasi Unit ∈ Bagian)
var unitCell = (row.Cell(6).GetString() ?? "").Trim();
var unitList = unitCell.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Distinct(StringComparer.OrdinalIgnoreCase).ToList();  // dedup, first=primary (D-04)
if (unitList.Any())
{
    if (string.IsNullOrWhiteSpace(bagian))
        errors.Add("Unit tidak boleh diisi tanpa Section");
    else if (sectionUnitsDict.TryGetValue(bagian, out var validUnits))
    {
        var invalid = unitList.Where(u => !validUnits.Contains(u)).ToList();   // validasi PER unit
        if (invalid.Any())
            errors.Add($"Unit tidak valid untuk '{bagian}': {string.Join(", ", invalid)}");
    }
}
// backward-compat: "UnitA" (no pipe) → 1 elemen = primary (D-05). Template lama tetap valid.
// setelah CreateAsync sukses: await SyncUserUnitsAsync(newUser, unitList, unitList.FirstOrDefault());
```

### MU-07 server round-trip re-prompt (Edit POST, lean Claude's Discretion)
```csharp
// Pola: tambah ManageUserViewModel.ConfirmedDeactivate (bool) + ImpactedMappings (List<string>) display
// Di EditWorker POST setelah validasi unit, sebelum SyncUserUnitsAsync:
var oldUnits = await _context.UserUnits.Where(uu => uu.UserId == user.Id).Select(uu => uu.Unit).ToListAsync();
var removed = oldUnits.Except(model.Units ?? new()).ToList();
if (removed.Any())
{
    // HARD-BLOCK (D-11): ada ProtonTrackAssignment aktif + active mapping AssignmentUnit ∈ removed
    var activeMapping = await _context.CoachCoacheeMappings
        .FirstOrDefaultAsync(m => m.CoacheeId == user.Id && m.IsActive);          // VERIFIED pola CDPController.cs:944
    var hasActivePta = await _context.ProtonTrackAssignments
        .AnyAsync(a => a.CoacheeId == user.Id && a.IsActive);                     // VERIFIED CDPController.cs:75,398
    if (hasActivePta && activeMapping?.AssignmentUnit != null && removed.Contains(activeMapping.AssignmentUnit))
    {
        ModelState.AddModelError("", $"Tidak bisa hapus unit '{activeMapping.AssignmentUnit}': masih ada PROTON tahun-berjalan aktif. Tutup/bypass PROTON dulu via surface PROTON.");
        /* re-render form */ return View(model);
    }
    // AUTO-DEACTIVATE-after-confirm (D-10): mapping aktif AssignmentUnit ∈ removed (tanpa PTA aktif terkait)
    if (activeMapping?.AssignmentUnit != null && removed.Contains(activeMapping.AssignmentUnit))
    {
        if (!model.ConfirmedDeactivate)
        {
            model.ImpactedMappings = new() { $"Coach mapping aktif unit '{activeMapping.AssignmentUnit}' akan dinonaktifkan" };
            ViewBag.NeedConfirm = true;                  // view tampilkan modal konfirmasi + hidden ConfirmedDeactivate=true
            /* re-render form */ return View(model);
        }
        // confirmed → reuse pola CoachCoacheeMappingDeactivate (VERIFIED CoachMappingController.cs:962-979) DALAM tx yang sama
        activeMapping.IsActive = false; activeMapping.EndDate = DateTime.UtcNow;
        // (+ cascade PTA deactivate + audit, sesuai pola)
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `ApplicationUser.Unit` scalar 1-unit | `UserUnits` junction multi + mirror | Phase 399 (this) | Pekerja >1 unit; scalar jadi mirror primary |
| Scalar audit `if user.Unit != model.Unit` `[VERIFIED:WorkerController.cs:406]` | Set-diff (added/removed/primary) | Phase 399 (D-12) | Audit akurat untuk multi |
| Unit single `<select>` `[VERIFIED:CreateWorker.cshtml:121]` | Checkbox-list + primary radio | Phase 399 (D-01) | UX multi-pilih + primary eksplisit |
| 1-unit import cell `[VERIFIED:WorkerController.cs:944]` | Pipe-delimited `"A|B|C"` | Phase 399 (D-04) | Multi-unit per baris, backward-compat |

**Deprecated/outdated:** Tidak ada deprecation di Phase 399 — `ApplicationUser.Section`/`Unit` SENGAJA dipertahankan (spec §9, mirror permanen fase ini).

## Project Constraints (from CLAUDE.md)

- **Develop Workflow:** Lokal → Dev (10.55.3.3) → Prod. Verifikasi lokal WAJIB sebelum commit: `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + **cek DB lokal** + Playwright bila ada UI. ❌ Jangan edit kode/DB di Dev/Prod. Promosi ke Dev = tanggung jawab IT.
- **Migration notify IT:** Phase 399 **migration=TRUE** (`AddUserUnitsTable` + backfill) → commit SERTAKAN file migration; notify IT dengan commit hash + flag migration=TRUE.
- **Seed Data Workflow:** Bila butuh seed test multi-unit lokal: klasifikasi (temporary local-only vs permanent) → snapshot DB (`sqlcmd BACKUP DATABASE`) → catat `docs/SEED_JOURNAL.md` → restore setelah test. Untuk test SQL-riil otomatis, pola `OrgLabelMigrationFixture` pakai disposable `HcPortalDB_Test_<guid>` (tidak sentuh DB dev, tidak butuh snapshot/restore) — preferred untuk Phase 404.
- **Always respond in Bahasa Indonesia.**

## Validation Architecture

> Nyquist validation aktif (config tidak set false). Section ini menurunkan VALIDATION.md.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 `[VERIFIED: HcPortal.Tests.csproj:15]` |
| Config file | none (xUnit auto-discover); project `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| Full suite command | `dotnet test HcPortal.Tests` |
| InMemory DB pattern | `new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())` `[VERIFIED: WorkerDataServiceSearchTests.cs:21-24]` |
| Real-SQL pattern | `OrgLabelMigrationFixture` disposable `HcPortalDB_Test_<guid>` localhost\SQLEXPRESS + `MigrateAsync()` `[VERIFIED: OrgLabelMigrationIntegrationTests.cs:24-48]`; `[Trait("Category","Integration")]` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MU-01 | Create/Edit set >1 unit dalam 1 Bagian; validasi `Unit ∈ Bagian` | unit (InMemory) | `dotnet test HcPortal.Tests --filter UserUnitsWriteThroughTests` | ❌ Wave 0 |
| MU-02 | Write-through: `user.Unit` == baris IsPrimary; recompute primary saat primary dihapus (promote/blok); 0 unit → Unit=null & 0 IsPrimary | unit (InMemory) | `dotnet test ... --filter PrimaryMirrorTests` | ❌ Wave 0 |
| MU-02 | Audit set-diff (added/removed/primary-changed), bukan scalar | unit (InMemory) | `dotnet test ... --filter UserUnitsAuditDiffTests` | ❌ Wave 0 |
| MU-03 | VM (`Profile/Settings/ManageUser/PSign`) memuat semua unit + primary marker | unit (VM populate) | `dotnet test ... --filter UnitsViewModelTests` | ❌ Wave 0 |
| MU-04 | Import parse pipe `"A|B|C"` → split+trim+dedup, first=primary; backward-compat 1-unit | unit (InMemory) | `dotnet test ... --filter ImportMultiUnitParseTests` | ❌ Wave 0 |
| MU-05 | Tiap junction-write validasi `Unit ∈ unit-Bagian`; tolak unit asing | unit (InMemory) | `dotnet test ... --filter UnitInSectionValidationTests` | ❌ Wave 0 |
| MU-05 | Backfill: pekerja Unit non-null → 1 baris IsPrimary; Unit null → 0 baris; idempotent (re-run no dup) | **integration (SQL-riil)** | `dotnet test ... --filter "Category=Integration&UserUnitsBackfill"` | ❌ Wave 0 (deferred enforce ke 404) |
| MU-07 | Hapus unit dgn PTA aktif → BLOCK; dgn mapping aktif (no PTA) → confirm→auto-deactivate (1 tx) | unit (InMemory) | `dotnet test ... --filter RemoveUnitGuardTests` | ❌ Wave 0 |
| (invariant #3) | Tepat 1 IsPrimary/user di SQL (filtered-unique) | **integration (SQL-riil)** | deferred Phase 404 QA-01 | ❌ Phase 404 |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test HcPortal.Tests --filter "Category!=Integration"` (logic tests cepat).
- **Per wave merge:** `dotnet test HcPortal.Tests` (full, termasuk Integration bila SQLEXPRESS tersedia) + `dotnet run` cek `localhost:5277` + cek DB lokal (CLAUDE.md gate).
- **Phase gate:** full suite green + UAT lokal (build+run+DB) sebelum `/gsd-verify-work`. Filtered-unique enforce SQL-riil resmi di Phase 404 (QA-01) — di Phase 399 minimal 1 integration test backfill idempotent direkomendasikan.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/UserUnitsWriteThroughTests.cs` — MU-01/MU-02 (write set + mirror)
- [ ] `HcPortal.Tests/PrimaryMirrorTests.cs` — MU-02 recompute (promote/blok/null)
- [ ] `HcPortal.Tests/UserUnitsAuditDiffTests.cs` — MU-02 set-diff
- [ ] `HcPortal.Tests/ImportMultiUnitParseTests.cs` — MU-04 parse + backward-compat
- [ ] `HcPortal.Tests/UnitInSectionValidationTests.cs` — MU-05 validasi
- [ ] `HcPortal.Tests/RemoveUnitGuardTests.cs` — MU-07 block vs auto-deactivate
- [ ] `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs` — MU-05 backfill idempotent (`[Trait("Category","Integration")]`, pola `OrgLabelMigrationFixture`)
- [ ] (opsional) `UnitsViewModelTests.cs` — MU-03 VM populate
- Framework install: tidak perlu (xUnit + InMemory + SqlServer sudah di csproj test).

## Security Domain

> `security_enforcement` aktif (tidak set false). Phase 399 = data layer + admin CRUD form.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak ubah auth; AD/local config dibaca read-only `[VERIFIED: WorkerController.cs:215]` |
| V3 Session Management | no | Tidak tersentuh |
| V4 Access Control | yes | Create/Edit/Import/Export Worker = `[Authorize(Roles="Admin, HC")]` `[VERIFIED: WorkerController.cs:196,210,344,896,129]`. Pertahankan di semua endpoint baru. **Authz Section (`IsResultsAuthorized`) 100% scalar — 0 perubahan** `[VERIFIED: CMPController.cs:2503-2511]`. |
| V5 Input Validation | yes | Validasi `Unit ∈ Bagian` setiap junction-write (MU-05); `[ValidateAntiForgeryToken]` di POST `[VERIFIED: WorkerController.cs:211,345,897]`; pipe-parse server-side (jangan trust client). |
| V6 Cryptography | no | Tidak ada crypto baru (password helper existing tak berubah `[VERIFIED: WorkerController.cs:1075-1084]`) |

### Known Threat Patterns for ASP.NET Core MVC + EF Core
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Mass-assignment via `List<string> Units` (inject unit asing/cross-Bagian) | Tampering / EoP | Validasi server `u ∈ GetUnitsForSectionAsync(Section)` SETIAP write (MU-05); tolak primary ∉ set |
| SQL injection di backfill raw SQL | Tampering | Backfill `migrationBuilder.Sql` literal statik (no user input) — aman; EF query parametrized |
| CSRF di Create/Edit/Import POST | Tampering | `[ValidateAntiForgeryToken]` sudah ada — pertahankan |
| Broken access (non-Admin/HC ubah unit) | EoP | `[Authorize(Roles="Admin, HC")]` di semua endpoint mutasi |
| Mirror desync (Unit ≠ IsPrimary) | Tampering (integrity) | Write-through terpusat `SyncUserUnitsAsync` + tx + test (invariant #3) |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Model-binding `List<string> Units` dari multiple checkbox bernama sama = standar MVC, tanpa JS submit khusus | Pattern 5 | Bila gagal bind, butuh JSON hidden field + JS serialize; planner sediakan fallback |
| A2 | `dotnet ef` 10.0.3 dapat scaffold migration EF Core 8 (kompat mundur) ATAU manual-write migration viable | Pitfall 1 / Env | Bila CLI gagal & manual error, blok migration; planner siapkan kedua jalur |
| A3 | MU-07 hard-block resolution: "PROTON unit aktif" = `AssignmentUnit` active mapping bila ada `ProtonTrackAssignment` aktif (karena PTA tak punya kolom Unit) | Pitfall 4 / Open Q1 | Bila resolusi unit PROTON berbeda, guard bisa over/under-block; butuh konfirmasi |
| A4 | Backfill via migration `Up` (Claude's Discretion lean) — bukan SeedData | Pattern 3 | Bila IT prefer SeedData, pindahkan; low-risk (keduanya diizinkan spec §9) |
| A5 | Index opsional `(UserId, Unit)` dipasang (rekomendasi) → `Unit` butuh maxLength ≤450 | Pattern 2-3 | Bila tak dipasang, `Unit` boleh nvarchar(max); keputusan planner |

## Open Questions

1. **MU-07 hard-block: bagaimana persis menentukan "unit PROTON aktif" yang dihapus?**
   - What we know: `ProtonTrackAssignment` TIDAK punya kolom Unit `[VERIFIED: ProtonModels.cs:71-87]`; unit di-resolve `AssignmentUnit (active mapping) ?? User.Unit` (spec §3). Active mapping single per coachee `[VERIFIED: filtered-unique IX_CoachCoacheeMappings_CoacheeId_ActiveUnique]`.
   - What's unclear: Di Phase 399 fallback `User.Unit` masih ada (dibuang baru di 401). Jika pekerja punya PTA aktif tapi `AssignmentUnit` mapping = null (fallback ke primary), menghapus PRIMARY unit = menghapus unit PROTON aktif → harus block. Apakah guard cek `AssignmentUnit ∈ removed` SAJA, atau juga `primary ∈ removed && hasActivePta && AssignmentUnit==null`?
   - Recommendation: Guard cek KEDUA: (a) `AssignmentUnit != null && AssignmentUnit ∈ removed && hasActivePta` → block; (b) `AssignmentUnit == null && hasActivePta && oldPrimary ∈ removed` → block (PROTON jatuh ke primary). Planner konfirmasi cakupan; ini A3.

2. **0 unit dipilih saat Edit pekerja yang punya PTA/mapping aktif — block atau cascade?**
   - What we know: D-02 izinkan 0 unit (`Unit=null`). MU-07 block bila unit aktif dihapus.
   - What's unclear: Mengosongkan SEMUA unit = menghapus unit primary yang mungkin = unit PROTON aktif.
   - Recommendation: Terapkan guard MU-07 sebelum mengizinkan 0-unit; bila ada PTA aktif di unit yang dikosongkan → block (konsisten D-11). Planner eksplisitkan.

3. **`UserManager.UpdateAsync` vs DbContext transaction untuk Edit write-through.**
   - What we know: Edit pakai `_userManager.UpdateAsync(user)` `[VERIFIED: WorkerController.cs:437]` (Identity store, SaveChanges sendiri). `SyncUserUnitsAsync` pakai `_context`.
   - What's unclear: Atomicity lintas UserManager + _context. Bila `UpdateAsync` sukses tapi UserUnits gagal → mirror desync.
   - Recommendation: Set `user.Unit = primary` SEBELUM `UpdateAsync`, lalu `SyncUserUnitsAsync` (yang juga set Unit) commit setelahnya; ATAU wrap keduanya dalam satu `BeginTransactionAsync` (UpdateAsync + _context.SaveChanges share connection via DI scope). Planner pilih; test integrity wajib.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/run/test | ✓ | 8.0.418 `[VERIFIED: dotnet --version]` | — |
| dotnet ef CLI | `migrations add AddUserUnitsTable` | ⚠️ versi mismatch | 10.0.3 `[VERIFIED: dotnet ef --version]` (project=8.0.0) | Pin tool lokal v8 ATAU tulis migration manual (Pitfall 1) |
| SQL Server / SQLEXPRESS | DB lokal + test SQL-riil | ✓ (per CLAUDE.md + OrgLabelMigrationFixture localhost\SQLEXPRESS) `[VERIFIED: OrgLabelMigrationIntegrationTests.cs:36]` | — | — |
| ClosedXML | Import/Export | ✓ | 0.105.0 `[VERIFIED: HcPortal.csproj:14]` | — |
| xUnit + EF InMemory/SqlServer (test) | Validation | ✓ | 2.9.3 / 8.0.0 `[VERIFIED: HcPortal.Tests.csproj]` | — |

**Missing dependencies with no fallback:** Tidak ada.
**Missing dependencies with fallback:** `dotnet ef` versi mismatch (10.0.3 vs 8.0.0) — fallback: pin tool lokal v8 atau manual-write migration + snapshot. **Planner WAJIB rencanakan langkah ini sebagai task pertama** (migration adalah fondasi seluruh milestone).

## Sources

### Primary (HIGH confidence)
- Codebase file:line refs (semua `[VERIFIED: ...]`) — `ApplicationDbContext.cs`, `WorkerController.cs`, `CoachMappingController.cs`, `CMPController.cs`, `CDPController.cs`, `Models/*`, `Views/*`, `Migrations/*`, `HcPortal.csproj`, `HcPortal.Tests.csproj`
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` (§2,§4,§5,§7,§9) — design AUTHORITATIVE
- `.planning/phases/399-.../399-CONTEXT.md` — keputusan D-01..D-12 (locked)
- `.planning/REQUIREMENTS.md` — MU-01..07 acceptance
- `CLAUDE.md` — Develop + Seed Data Workflow
- `dotnet --version` → 8.0.418; `dotnet ef --version` → 10.0.3 (verified this session)

### Secondary (MEDIUM confidence)
- SQL Server filtered index + index key column constraints (nvarchar(max) tak boleh key) — pengetahuan EF Core/SQL Server umum, konsisten dgn pola repo

### Tertiary (LOW confidence)
- A1 model-binding checkbox→List<string> (standar MVC, tidak diverifikasi runtime sesi ini — direkomendasikan Playwright/manual UAT)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency terverifikasi di csproj, semua pola ada di repo
- Architecture/patterns: HIGH — filtered-index, junction, tx, auto-deactivate, cascade dict semua punya preseden file:line
- Pitfalls: HIGH — versi mismatch & InMemory-no-enforce & PTA-no-Unit-column terverifikasi langsung
- MU-07 resolusi unit PROTON: MEDIUM — logika benar tapi butuh konfirmasi cakupan (Open Q1/Q2)
- Model-binding checkbox UI: MEDIUM — standar MVC tapi belum runtime-verified (A1)

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stack stabil; re-verify `dotnet ef` versi bila CLI di-update)
