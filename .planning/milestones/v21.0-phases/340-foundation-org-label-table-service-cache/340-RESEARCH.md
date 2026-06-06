# Phase 340: P1 Foundation — Tabel + Service + Cache — Research

**Researched:** 2026-06-02
**Domain:** ASP.NET Core 8 + EF Core 8 SQL Server (data layer + service + IMemoryCache + JSON endpoint)
**Confidence:** HIGH (stack proven, semua pola sudah ada di repo, hanya wiring komponen baru)

---

## Summary

Phase 340 adalah foundation milestone v21.0 — bikin tabel `OrganizationLevelLabels` + service `IOrgLabelService` dengan in-memory cache + endpoint `GET /Admin/GetLevelLabels`. Semua decision sudah locked di `340-CONTEXT.md` (11 keputusan D-01..D-11). Stack ASP.NET Core 8 + EF Core 8 SQL Server sudah established di codebase (`HcPortal.csproj` L17-18); semua pola yang dibutuhkan (idempotent seed, IMemoryCache, AuditLogService injection, controller route convention, EF Fluent API) sudah dipraktekkan di kode existing dan tinggal direplikasi.

Risiko utama bukan stack tapi **disiplin scope** — service expose 7 method tapi 3 mutation method (`Add/Update/Delete`) tidak dipanggil di Phase 340 (D-10), jadi planner harus pastikan mutation diimplementasi penuh tapi test minimal saja (TEST-01 cover `GetLabel` happy + fallback). Mutation full test geser ke Phase 344.

**Primary recommendation:** Replikasi pola `AssessmentAdminController.cs:223` IMemoryCache untuk read path (tapi tanpa TTL per D-02), pola `Data/SeedData.cs:75` `SeedOrganizationUnitsAsync` untuk seed runtime idempotent, dan pola `Controllers/OrganizationController.cs:11` route `[Route("Admin/[action]")]` untuk endpoint. Bikin migration **CreateTable only** (no `INSERT`/`HasData` per D-01) supaya HC custom label (mis. "Bagian"→"Direktorat") tidak ke-overwrite saat re-apply.

---

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 Seed Strategy:** Seed 3 baris default via `Data/SeedData.cs` runtime check (`if (!Any())`). Migration body **HANYA `CreateTable`**, NO `INSERT` SQL, NO `HasData`. Match `SeedData.cs:26` pattern.
- **D-02 Cache:** `IMemoryCache` singleton, key `"OrgLabels:All"`, value `IReadOnlyDictionary<int, string>`. **No TTL, manual invalidate only** (`_cache.Remove("OrgLabels:All")` tiap mutation).
- **D-03 Endpoint Auth:** `[Authorize]` (authenticated user, no role filter) — label = public display info.
- **D-04 Audit Log:** Reuse `AuditLogService.LogAsync` (`Services/AuditLogService.cs:21`). ActionType `"OrgLabel-Add"`/`"OrgLabel-Update"`/`"OrgLabel-Delete"`. TargetType `"OrganizationLevelLabel"`. TargetId = level (int).
- **D-05 Migration:** Name `AddOrganizationLevelLabel` (singular). Schema: `Level int PK (ValueGeneratedNever)`, `Label nvarchar(50) NOT NULL`, `UpdatedAt datetime2 NOT NULL UTC`, `UpdatedBy nvarchar(450) NOT NULL`. Unique index `IX_OrganizationLevelLabels_Label` via Fluent `HasIndex(e => e.Label).IsUnique()`.
- **D-06 Lifetime:** `OrgLabelService` = Scoped (DI captive dependency safety — inject ApplicationDbContext Scoped). IMemoryCache tetap Singleton.
- **D-07 Fallback:** `GetLabel(int level)` fallback `$"Level {level}"` bila level tidak ada di cache/tabel.
- **D-08 Max Methods:** Service expose dua method terpisah — `GetMaxConfiguredLevel()` (dari cache `MAX(OrganizationLevelLabels.Level)`) + `GetMaxUsedLevel()` (live query `MAX(OrganizationUnits.Level)`, no cache).
- **D-09 DB_HANDOFF_IT:** Phase 340 deliverable include `docs/DB_HANDOFF_IT_2026-06-XX.html` dari template. MIGRATION_LIST = 1 migration additive, AFFECTED_TABLES = NEW table.
- **D-10 Service Scope:** Phase 340 implement **FULL** `IOrgLabelService` (7 method). Mutation method belum dipanggil di Phase 340 (Phase 341 controller consume). Test Phase 340 = TEST-01 saja (GetLabel happy + fallback). Mutation test full di Phase 344.
- **D-11 Model:** `Models/OrganizationLevelLabel.cs` (singular). DbSet plural `OrganizationLevelLabels`.

### Claude's Discretion

- Service file location: `Services/IOrgLabelService.cs` + `Services/OrgLabelService.cs` flat (sama dengan `INotificationService.cs` + `NotificationService.cs`).
- XML doc verbosity: standar pattern `INotificationService.cs` — `<summary>` + `<param>` + `<returns>` per method.
- Test coverage Phase 340: pure unit test minimal, basic `GetLabel` happy + fallback. Integration test full di Phase 344.
- Controller class: NEW `Controllers/OrgLabelController.cs` (Phase 340 mount `GetLevelLabels`). Phase 341 reuse untuk page CRUD endpoints. Hindari clutter `OrganizationController.cs`.

### Deferred Ideas (OUT OF SCOPE Phase 340)

- WebSocket push label refresh ke tab lain.
- i18n multi-language label.
- Multi-server cache invalidation strategy (single-server Portal HC KPB).
- Tom Select integration dropdown induk.
- Page UI `/Admin/ManageOrgLevelLabels` (Phase 341).
- Page CRUD POST endpoints Add/Update/Delete (Phase 341).
- `@inject IOrgLabelService` di view CMP/CDP/etc. (Phase 343).
- Fix bug ManageOrganization tree (Phase 342).
- Full Playwright/integration test (Phase 344).

---

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ORG-LABEL-01 | Tabel `OrganizationLevelLabels` dibuat via EF migration (Level PK, Label, UpdatedAt, UpdatedBy). Seed default 3 baris masuk `Data/SeedData.cs` klasifikasi permanent+prod-required. | Migration pattern terbukti di `Migrations/*` (lihat InitialSqlServer + AddAuditLog). Seed pattern `Data/SeedData.cs:75`. Fluent API config di `Data/ApplicationDbContext.cs:131` `OnModelCreating`. |
| ORG-LABEL-02 | Service `IOrgLabelService` + `OrgLabelService` dengan in-memory cache, key `OrgLabels:All`, manual invalidate, fallback `"Level {N}"`. | IMemoryCache injection pattern di `AssessmentAdminController.cs:22, 44`. Service interface+impl pattern di `INotificationService.cs` + `NotificationService.cs`. DI Scoped registration `Program.cs:61`. |
| ORG-LABEL-03 | Endpoint `GET /Admin/GetLevelLabels` JSON dict `{ "0": "Bagian", ... }`, auth: authenticated user. | Controller route convention `[Route("Admin/[action]")]` (10 controller pakai pola ini, lihat `OrganizationController.cs:11`). `Json(...)` return pattern di `OrganizationController.cs:67`. |
| ORG-LABEL-07 | Auto-detect max level via `MAX(OrganizationUnits.Level)` + buffer 1. | `OrganizationUnit.Level int` field exists (`Models/OrganizationUnit.cs:8`). Linq `MaxAsync` standar EF Core 8 API. |

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Tabel `OrganizationLevelLabels` schema | Database / Storage | — | Persisted state, EF Core managed via migration |
| Seed default 3 baris | API / Backend (startup) | Database | Runtime seed via `SeedData.InitializeAsync` di `Program.cs:134` — bukan migration body (D-01) |
| Cache 3-5 entry label dict | API / Backend (in-memory) | — | Singleton `IMemoryCache` di-share antar request, single-server scope |
| `GetLabel(level)` read API | API / Backend (service) | — | Pure business logic, no UI/storage tier |
| `GET /Admin/GetLevelLabels` JSON | API / Backend (controller) | Browser / Client (JS consumer Phase 342+343) | Endpoint mendukung JS fetch di view consumer (downstream phase) |
| Audit log mutation | API / Backend | Database | `AuditLogService` write ke `AuditLogs` tabel existing |
| DB_HANDOFF_IT doc | Documentation / Process | — | Bukan tier kode, tapi deliverable formal — bagian dari "build artifact" untuk IT |

**Konfirmasi:** Tidak ada misassignment ke browser tier — semua read+cache+endpoint murni backend. Browser consumer baru ada di Phase 342 (orgTree.js) dan Phase 343 (view integration).

---

## Stack Considerations

### IMemoryCache Lifetime & Captive Dependency

- **`IMemoryCache` = Singleton** (default ASP.NET Core registration via `services.AddMemoryCache()` — sudah ada di `Program.cs:17`). [VERIFIED: Program.cs:17 baris `builder.Services.AddMemoryCache();`]
- **`OrgLabelService` HARUS Scoped** karena inject `ApplicationDbContext` (Scoped per request) + `AuditLogService` (Scoped per `Program.cs:51`). Bila Singleton, ApplicationDbContext akan jadi captive dependency → DbContext threading bug. [CITED: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#scoped-service-as-singleton]
- **Pola injection:** Constructor `OrgLabelService(ApplicationDbContext context, IMemoryCache cache, AuditLogService auditLog)` — IMemoryCache itu Singleton tapi aman di-inject ke Scoped service (rule reversed: Singleton dipakai oleh Scoped OK; yang dilarang sebaliknya).

### EF Core 8 Specifics

- **`HasKey(e => e.Level)` + `ValueGeneratedNever()`** — Level int adalah natural identifier (bukan auto-increment). EF Core default treat int PK sebagai identity column kalau tidak dispesifikasi `ValueGeneratedNever()`. Tanpa ini, `Level=0` insert akan fail (SQL Server identity tidak boleh insert 0 default) atau ke-override identity. [VERIFIED: https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties]
- **`HasIndex(e => e.Label).IsUnique()`** generate `CREATE UNIQUE INDEX IX_OrganizationLevelLabels_Label ON [OrganizationLevelLabels] ([Label])`. Existing pattern di `ApplicationDbContext.cs:209-210` (NomorSertifikat unique) + `:315` (CoacheeId unique mapping). [VERIFIED: ApplicationDbContext.cs grep output]
- **`Property(e => e.Label).IsRequired().HasMaxLength(50)`** generate `[Label] nvarchar(50) NOT NULL`. EF Core default string property = `nvarchar(max)` kalau tidak dispesifikasi MaxLength.
- **Migration generation:** `dotnet ef migrations add AddOrganizationLevelLabel --context ApplicationDbContext` — `--context` explicit karena single context di project, tapi konsistensi dengan SOP `docs/DEV_WORKFLOW.md` §3 Step 3.

### SQL Server datetime2 / nvarchar / UNIQUE Index

- **`UpdatedAt datetime2 NOT NULL UTC`** — EF Core map `DateTime` C# → `datetime2(7)` SQL Server default (precision 100ns). Pakai `DateTime.UtcNow` di service (jangan `DateTime.Now` — server time zone drift risk, match `AuditLog.cs:51` pattern `CreatedAt = DateTime.UtcNow`).
- **`UpdatedBy nvarchar(450) NOT NULL`** — 450 char max sesuai ASP.NET Identity `IdentityUser.Id` (default GUID string `nvarchar(450)`). [VERIFIED: Models/AuditLog.cs:13 `ActorUserId string` no MaxLength = `nvarchar(max)`, tapi spec D-05 lock 450 untuk consistency dengan UserId column type. Aman karena GUID = 36 char.]
- **Unique constraint Label cross-level:** Pasangan `Level=0 Label='Bagian'` + `Level=2 Label='Bagian'` ditolak DB level via index (defense in depth selain service validation). [CITED: spec §4.1 + D-05]

### ASP.NET Core Authorize Attribute

- **`[Authorize]` tanpa Roles** = authenticated user (apa pun role). [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple]
- **Tidak ada `[ValidateAntiForgeryToken]` di GET endpoint** — CSRF protection irrelevant untuk read-only request (idempotent). Phase 341 POST endpoints baru pakai. [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery]
- **Route attribute:** `[Route("Admin/[action]")]` di controller class → `[HttpGet]` method `GetLevelLabels` jadi `GET /Admin/GetLevelLabels`. Sepuluh controller existing pakai pola identik (`AdminController`, `OrganizationController`, dst — lihat grep result). [VERIFIED: grep Controllers/ multiple files]

---

## Implementation Patterns

### Pattern 1: EF Migration CreateTable-only (D-01 / D-05)

**What:** Migration body hanya `migrationBuilder.CreateTable(...)` + drop index nothing else. No `migrationBuilder.InsertData(...)`. Seed handled by `SeedData.SeedOrganizationLevelLabelsAsync` di runtime.

**Why:** Re-apply migration atau restore `.bak` tidak overwrite HC custom label.

**Steps:**
1. Buat `Models/OrganizationLevelLabel.cs` (entity).
2. Tambah `DbSet<OrganizationLevelLabel> OrganizationLevelLabels` di `ApplicationDbContext.cs:18-89` block.
3. Tambah Fluent config di `ApplicationDbContext.OnModelCreating` (lihat `ApplicationDbContext.cs:131`).
4. Run `dotnet ef migrations add AddOrganizationLevelLabel --context ApplicationDbContext`.
5. Verify generated `Migrations/{TIMESTAMP}_AddOrganizationLevelLabel.cs` body **hanya `CreateTable`** + `CreateIndex` (untuk unique). Bila EF generate `InsertData` (karena `HasData`), HAPUS dan re-add migration.
6. Verify `Migrations/{TIMESTAMP}_AddOrganizationLevelLabel.Designer.cs` + `ApplicationDbContextModelSnapshot.cs` updated.
7. Run `dotnet ef database update --context ApplicationDbContext` di lokal.

**Source:** `Migrations/20260221032754_AddAuditLog.cs` precedent — single tabel CreateTable + Index.

### Pattern 2: Idempotent Runtime Seed (D-01)

**What:** Method static async di `SeedData.cs` check `!await context.X.AnyAsync()` sebelum AddRange + SaveChangesAsync.

**Code template (replikasi `SeedData.cs:75` `SeedOrganizationUnitsAsync`):**

```csharp
// Source: Data/SeedData.cs:71-104 (pola idempotent seed)
public static async Task SeedOrganizationLevelLabelsAsync(ApplicationDbContext context)
{
    if (await context.OrganizationLevelLabels.AnyAsync())
        return;

    var defaults = new[]
    {
        new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
        new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
        new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
    };
    context.OrganizationLevelLabels.AddRange(defaults);
    await context.SaveChangesAsync();
}
```

**Wiring:** Tambah `await SeedOrganizationLevelLabelsAsync(context);` di `SeedData.InitializeAsync` setelah baris `:26` `await SeedOrganizationUnitsAsync(context);`.

**Klasifikasi seed:** `permanent + prod-required` per `docs/SEED_WORKFLOW.md` §3 — tidak butuh journal entry, langsung commit di `Data/SeedData.cs`.

### Pattern 3: IMemoryCache no-TTL manual invalidate (D-02)

**What:** `_cache.Set(key, value)` (no expiration) atau `GetOrCreate` tanpa `entry.AbsoluteExpiration*` set. Invalidate manual via `_cache.Remove(key)` setiap mutation.

**Reference pattern (modify dari `AssessmentAdminController.cs:223`):**

```csharp
// Source: Controllers/AssessmentAdminController.cs:223 (modified — drop AbsoluteExpiration)
// Phase 340 D-02: no TTL, manual invalidate only
private const string LabelsCacheKey = "OrgLabels:All";

public string GetLabel(int level)
{
    var dict = GetAll();
    return dict.TryGetValue(level, out var label) ? label : $"Level {level}";  // D-07
}

public IReadOnlyDictionary<int, string> GetAll()
{
    return _cache.GetOrCreate(LabelsCacheKey, entry =>
    {
        // NO entry.AbsoluteExpirationRelativeToNow set — manual invalidate only
        return (IReadOnlyDictionary<int, string>)_context.OrganizationLevelLabels
            .AsNoTracking()
            .OrderBy(l => l.Level)
            .ToDictionary(l => l.Level, l => l.Label);
    })!;
}
```

**Invalidation:**
```csharp
public async Task UpdateAsync(int level, string label, string userId, string actorName)
{
    var row = await _context.OrganizationLevelLabels.FindAsync(level);
    if (row == null) throw new InvalidOperationException($"Level {level} not configured");
    var oldLabel = row.Label;
    row.Label = label;
    row.UpdatedAt = DateTime.UtcNow;
    row.UpdatedBy = userId;
    await _context.SaveChangesAsync();

    _cache.Remove(LabelsCacheKey);  // D-02 manual invalidate

    await _auditLog.LogAsync(
        actorUserId: userId,
        actorName: actorName,
        actionType: "OrgLabel-Update",          // D-04
        description: $"Level {level}: '{oldLabel}' → '{label}'",
        targetId: level,
        targetType: "OrganizationLevelLabel"
    );
}
```

**Note `GetOrCreate` synchronous:** dict kecil (3-5 entry) + first-read DB query selesai milliseconds — tidak butuh `GetOrCreateAsync`. Konsisten dengan razor view `@inject` consumer di Phase 343 yang prefer sync interface.

### Pattern 4: Controller Route + JSON Endpoint (D-03)

**Code template:**

```csharp
// Source: Controllers/OrganizationController.cs:11, :56-68 (pola route + Json return)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize]                                  // D-03 authenticated, no role filter
    [Route("Admin/[action]")]                    // konsisten 10 controller existing
    public class OrgLabelController : Controller
    {
        private readonly IOrgLabelService _orgLabels;

        public OrgLabelController(IOrgLabelService orgLabels)
        {
            _orgLabels = orgLabels;
        }

        // GET /Admin/GetLevelLabels
        [HttpGet]
        public IActionResult GetLevelLabels()
        {
            var dict = _orgLabels.GetAll();
            // JSON serialization output: { "0": "Bagian", "1": "Unit", "2": "Sub-unit" }
            return Json(dict);
        }
    }
}
```

**Note:** Tidak inherit `AdminBaseController` karena tidak butuh `_context`/`_userManager`/`_auditLog`/`_env` di Phase 340 endpoint (service already encapsulate). Phase 341 nanti boleh refactor inherit kalau page CRUD butuh.

### Pattern 5: Service Registration Program.cs (D-06)

**Where:** `Program.cs` setelah baris `:61` `AddScoped<INotificationService, NotificationService>()`.

**Code:**
```csharp
// Phase 340 ORG-LABEL-02 — registered Scoped (D-06)
builder.Services.AddScoped<HcPortal.Services.IOrgLabelService, HcPortal.Services.OrgLabelService>();
```

**Order:** Setelah `AddMemoryCache()` (`Program.cs:17`) — itu prerequisite. Posisi terbaik setelah baris `:62` `AddScoped<IWorkerDataService, WorkerDataService>()` supaya semua interface-based services kelompokkan.

### Pattern 6: ApplicationDbContext Fluent Config (D-05 / D-11)

**Where:** `ApplicationDbContext.OnModelCreating` block (`ApplicationDbContext.cs:131`). Tambah block baru SETELAH `base.OnModelCreating(builder)` baris `:133`, sebelum entity config existing yang lain.

**Code:**
```csharp
// Phase 340 ORG-LABEL-01 — OrganizationLevelLabel config (D-05, D-11)
builder.Entity<OrganizationLevelLabel>(entity =>
{
    entity.HasKey(e => e.Level);
    entity.Property(e => e.Level).ValueGeneratedNever();  // Level adalah natural identifier
    entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
    entity.Property(e => e.UpdatedAt).IsRequired();
    entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(450);
    entity.HasIndex(e => e.Label).IsUnique();
});
```

**Note:** Letakkan dekat dengan existing `OrganizationUnit` config (kalau ada) supaya semantic grouping. Pattern existing `ApplicationDbContext.cs:209-210` (NomorSertifikat unique) sebagai sanity-check template.

### Pattern 7: Model Class (D-11)

**Code template (singular class, plural DbSet — match `Models/OrganizationUnit.cs`):**

```csharp
// Source pattern: Models/OrganizationUnit.cs, Models/AuditLog.cs
namespace HcPortal.Models
{
    /// <summary>
    /// Label tampilan tier organisasi (Phase 340 — milestone v21.0).
    /// Level adalah natural PK. Label di-CRUD oleh HC/Admin via /Admin/ManageOrgLevelLabels (Phase 341).
    /// </summary>
    public class OrganizationLevelLabel
    {
        public int Level { get; set; }
        public string Label { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = "";
    }
}
```

**No data-annotation `[Required]`/`[MaxLength]`** karena sudah ada di Fluent config (D-05). Project pattern campuran — `AuditLog.cs:13` pakai annotation, `OrganizationUnit.cs:6` tidak. Untuk single source of truth, prefer Fluent (terkonsentrasi di DbContext) konsisten dengan pesan CONTEXT D-05.

---

## Anti-Patterns to Avoid

- **`HasData` di Fluent config / `migrationBuilder.InsertData` di migration body.** Akan generate INSERT di migration → ke-trigger ulang setiap `dotnet ef database update` di env baru → potential UPDATE existing customized HC label balik ke default. **Solusi:** Runtime seed di `SeedData.cs` (D-01).
- **`IMemoryCache` dengan TTL untuk tabel jarang berubah.** TTL bikin cache miss berkala → DB hit unnecessary → cache invalidation jadi 2 mekanisme (TTL + manual) yang membingungkan. **Solusi:** No TTL, manual invalidate saja (D-02).
- **Inject `ApplicationDbContext` ke Singleton service.** EF Core DbContext NOT thread-safe — Singleton service yang inject DbContext = bug magnet. **Solusi:** Service Scoped (D-06).
- **Method async di interface untuk method yang body-nya sync.** `Task<string> GetLabelAsync(int level)` dengan body in-memory dict lookup = boilerplate tanpa benefit (no I/O). **Solusi:** Sync `string GetLabel(int level)` per D-07 spec.
- **Forget `[Authorize]` di controller class atau action.** Default ASP.NET Core 8 anonymous accessible kalau atribut hilang. **Solusi:** `[Authorize]` di class level (D-03), confirm dengan grep test.
- **`DateTime.Now` di service.** Server time zone drift (Windows lokal vs Linux Dev). **Solusi:** `DateTime.UtcNow` (match `AuditLog.cs:51`).
- **Initialize `IReadOnlyDictionary` setiap call tanpa cache.** Bila lupa cache, 7 method call = 7 DB hit. **Solusi:** Cache `GetAll()` result, semua method lain delegate ke `GetAll()`.
- **Audit log target outside MaxLength.** `TargetType = "OrganizationLevelLabel"` 24 char OK (MaxLength 100). `ActionType = "OrgLabel-Update"` 16 char OK (MaxLength 50). Verify saat write.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| In-memory cache 3-5 entry | Custom `Dictionary<int, string>` static field | `IMemoryCache` injection | Thread-safe, ASP.NET-managed lifetime, framework support |
| Audit log insert | Direct `_context.AuditLogs.Add(...)` di service | `AuditLogService.LogAsync` (existing) | Encapsulate insert + SaveChangesAsync + consistent field mapping. Lihat D-04 + `Services/AuditLogService.cs:21` |
| Seed default data | INSERT di migration body | Runtime idempotent seed di `Data/SeedData.cs` | Re-apply safe + HC custom value preserved. Lihat D-01 |
| JSON response serialization | Manual `JsonSerializer.Serialize` + `Content(...)` | `return Json(dict)` controller helper | ASP.NET Core auto-content-type `application/json` + camel case via global config |
| EF identity column override | Raw SQL `SET IDENTITY_INSERT` | `Property(e => e.Level).ValueGeneratedNever()` Fluent API | EF treat as non-identity column → no IDENTITY_INSERT needed |
| DB_HANDOFF_IT format from scratch | Bikin HTML/MD baru | Copy template `docs/templates/DB_HANDOFF_IT.template.md` | Format already battle-tested per Cilacap incident SOP, IT familiar |

---

## Common Pitfalls

### Pitfall 1: HasData rigid seed
**What goes wrong:** EF Fluent `entity.HasData(...)` translate ke `migrationBuilder.InsertData(...)` di migration body. Saat re-apply migration di env yang sudah ada customized label, INSERT akan fail unique constraint atau (kalau dipindah ke Up via SQL idempotent) UPDATE balik ke default.
**Why it happens:** Pattern EF default + dokumentasi tutorial banyak pakai HasData → reflexively dipakai.
**How to avoid:** Eksplisit verify migration generated body — open `Migrations/{TIMESTAMP}_AddOrganizationLevelLabel.cs` dan **pastikan tidak ada `migrationBuilder.InsertData`**. Bila ada → root cause: `HasData` di Fluent config bocor → hapus + re-add migration.
**Warning signs:** `dotnet ef migrations add` output mention `data seeded`. Diff migration body show `InsertData` block.

### Pitfall 2: Captive dependency Scoped-into-Singleton
**What goes wrong:** Bila `OrgLabelService` accidentally registered Singleton (`AddSingleton<IOrgLabelService, ...>()`), `ApplicationDbContext` yang Scoped jadi ter-capture sebagai Singleton lifecycle → DbContext shared antar request → threading bug + memory leak.
**Why it happens:** Developer copy-paste registration line dari Singleton service, lupa ubah.
**How to avoid:** Explicit `AddScoped<IOrgLabelService, OrgLabelService>()` per D-06. Verification: grep `Program.cs` setelah edit confirm "Scoped" untuk IOrgLabelService.
**Warning signs:** ASP.NET Core 8 startup error `Cannot consume scoped service 'ApplicationDbContext' from singleton 'OrgLabelService'` (ASP.NET Core has built-in scope validation in Dev — `EnableValidateOnBuild`).

### Pitfall 3: Cache key collision
**What goes wrong:** `_cache.Set("OrgLabels:All", ...)` tabrakan dengan key lain. Existing repo pakai `CategoriesCacheKey = "assessment_categories_distinct"` (`AssessmentAdminController.cs:24`). Bila `OrgLabels:All` accidentally clashed (e.g., diketik typo "OrgLabels:All " trailing space) bisa silently fail invalidate.
**Why it happens:** No type-safe key registry, string magic.
**How to avoid:** Define `private const string LabelsCacheKey = "OrgLabels:All";` di service class. Reference via constant, bukan literal. Audit existing cache keys via `grep "_cache.Set\|_cache.GetOrCreate\|_cache.Remove" --include="*.cs"` confirm no collision.
**Warning signs:** Update label tidak reflect di GET response — invalidate berhasil tapi GetOrCreate hit stale cache karena beda key.

### Pitfall 4: Audit log foreign key (none — TargetId is int, not FK)
**What goes wrong (hypothetical):** Developer assume `AuditLog.TargetId` adalah FK ke specific table.
**Why it doesn't apply:** `Models/AuditLog.cs:42` `TargetId int?` is **opaque integer**, no FK constraint. Polymorphic identifier — `TargetType` string disambiguate. Untuk Phase 340: `TargetId = level` (0/1/2/...) + `TargetType = "OrganizationLevelLabel"`. Safe.
**How to avoid:** Tidak ada action — just confirm via `Models/AuditLog.cs:42-49` no `[ForeignKey]` annotation.

### Pitfall 5: Migration ordering / ModelSnapshot conflict
**What goes wrong:** Bila ada migration lain pending (uncommit) saat `dotnet ef migrations add AddOrganizationLevelLabel`, `ApplicationDbContextModelSnapshot.cs` akan include perubahan kedua → diff messy + risk merge conflict.
**Why it happens:** Developer multi-task antar fitur tanpa baseline migration.
**How to avoid:** Pre-flight: `git status` confirm working tree clean (atau hanya Phase 340 files staged). `dotnet ef migrations list --context ApplicationDbContext` confirm last migration applied = `20260528064336_ChangeValidUntilToDateOnly` (latest per glob result).
**Warning signs:** Designer.cs include entity changes yang tidak related ke Phase 340.

### Pitfall 6: Program.cs DI registration order
**What goes wrong:** `AddScoped<IOrgLabelService, OrgLabelService>()` SEBELUM `AddMemoryCache()` → ASP.NET Core startup tidak fail (DI resolve at request time), tapi readability suffer.
**Why it happens:** Insertion at wrong file location.
**How to avoid:** Place setelah baris `Program.cs:62` (`AddScoped<IWorkerDataService, WorkerDataService>()`) — group dengan service interface yang lain. Verify dengan grep "AddScoped" + "IOrgLabelService" → exactly 1 match.

### Pitfall 7: JSON dict serialization int key
**What goes wrong:** `Dictionary<int, string>` di-serialize ASP.NET Core 8 default System.Text.Json mengeluarkan `{ "0": "Bagian", ... }` (int key → string conversion). Tapi kalau global config ada PropertyNamingPolicy custom, bisa beda.
**Why it happens:** ASP.NET Core 8 supports `Dictionary<int, string>` serialization out of the box (since .NET 7), beda dari .NET 6 yang throw `NotSupportedException`. [CITED: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-collection-types]
**How to avoid:** Confirm during local test `GET /Admin/GetLevelLabels` response body via browser DevTools. Bila throw or ugly format, return `dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)` explicit (defensive).
**Warning signs:** Response body `{}` (empty), 500 error, or `"$type":"Dictionary[Int32,String]"` polymorphic metadata.

### Pitfall 8: SeedData call order (non-critical Phase 340)
**What goes wrong (potential future):** Bila Phase 343 service consumer (view @inject) dipanggil DURING seed (mis. `SeedOrganizationUnitsAsync` panggil `GetLabel`), order matter. Phase 340 sendiri tidak: `SeedOrganizationLevelLabelsAsync` independent dari `SeedOrganizationUnitsAsync`.
**How to avoid:** Safe order — `await SeedOrganizationLevelLabelsAsync(context);` setelah `SeedOrganizationUnitsAsync(context)` di `InitializeAsync`. Code review: confirm no `IOrgLabelService` reference di `SeedData.cs`.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 [VERIFIED: HcPortal.Tests/HcPortal.Tests.csproj:12-13] |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (sibling project, excluded dari main build via `HcPortal.csproj:10` `DefaultItemExcludes`) |
| Test directory | `HcPortal.Tests/` di root repo |
| Existing test files | `FileUploadHelperTests.cs` (Phase 325 W0 bootstrap) + `CertificateStatusTests.cs` |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelService"` |
| Full suite command | `dotnet test HcPortal.Tests` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| ORG-LABEL-01 (migration + seed) | Migration apply + 3 baris hadir | Manual local (`dotnet ef database update` + SQL query) + integration test Phase 344 | `dotnet ef database update` + `sqlcmd -Q "SELECT COUNT(*) FROM OrganizationLevelLabels"` | ❌ Manual SC#1 |
| ORG-LABEL-02 (service + cache + fallback) | `GetLabel(0)`→"Bagian"; `GetLabel(99)`→"Level 99"; cache invalidate trigger | Unit test xUnit (TEST-01) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelServiceTests"` | ❌ Wave 0 — create `OrgLabelServiceTests.cs` |
| ORG-LABEL-03 (endpoint JSON) | `GET /Admin/GetLevelLabels` return dict 3 entry | Manual local browser/curl + Playwright E2E Phase 344 | `curl http://localhost:5277/Admin/GetLevelLabels` + login cookie | ❌ Manual SC#3 |
| ORG-LABEL-07 (GetMaxUsedLevel) | `MAX(OrganizationUnits.Level)` returned correct | Manual local SQL check + integration Phase 344 | `sqlcmd -Q "SELECT MAX(Level) FROM OrganizationUnits"` vs service output | ❌ Manual SC#5 |

### Sampling Rate

- **Per task commit:** `dotnet build` (no warning baru) + `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelService"` (TEST-01 fast unit test).
- **Per wave merge:** Full `dotnet test HcPortal.Tests` (include existing FileUploadHelperTests + CertificateStatusTests untuk regression smoke).
- **Phase gate:** Full local UAT 5 SC (1=migration apply, 2=GetLabel happy+fallback unit test pass, 3=endpoint browser verify, 4=cache invalidate mock test pass, 5=GetMaxUsedLevel manual SQL verify match service).

### Wave 0 Gaps

- [ ] `HcPortal.Tests/OrgLabelServiceTests.cs` — covers TEST-01 (`GetLabel` happy + fallback)
- [ ] No new test conftest needed — xUnit pakai constructor injection + `[Fact]` attribute, no global setup
- [ ] Existing test infrastructure (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` + `coverlet.collector`) covers all Phase 340 needs — no new package install

### Unit Test Approach (TEST-01)

**Goal:** Verify `GetLabel(int)` happy path (0/1/2 → "Bagian"/"Unit"/"Sub-unit") + fallback (`99` → "Level 99"). Mock `IMemoryCache` + `ApplicationDbContext` via in-memory provider.

**Strategy:** Use EF Core 8 InMemory provider (`Microsoft.EntityFrameworkCore.InMemory` 8.0.0 — need add package to `HcPortal.Tests.csproj`) + real `MemoryCache` instance (lightweight, no mock needed).

**Test sketch:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

public class OrgLabelServiceTests
{
    private static (OrgLabelService service, ApplicationDbContext ctx) MakeService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );
        ctx.SaveChanges();
        var cache = new MemoryCache(new MemoryCacheOptions());
        // AuditLogService also needs the same ctx; pass null actor if mutation tidak ditest
        var auditLog = new AuditLogService(ctx);
        var service = new OrgLabelService(ctx, cache, auditLog);
        return (service, ctx);
    }

    [Fact]
    public void GetLabel_KnownLevel_ReturnsConfiguredLabel()
    {
        var (svc, _) = MakeService();
        Assert.Equal("Bagian",   svc.GetLabel(0));
        Assert.Equal("Unit",     svc.GetLabel(1));
        Assert.Equal("Sub-unit", svc.GetLabel(2));
    }

    [Fact]
    public void GetLabel_UnknownLevel_ReturnsFallback()
    {
        var (svc, _) = MakeService();
        Assert.Equal("Level 99", svc.GetLabel(99));
        Assert.Equal("Level 5",  svc.GetLabel(5));
    }
}
```

**Required package add (Wave 0):**
```bash
cd HcPortal.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
```

**Note:** SC#4 spec "cache invalidation triggered saat label di-update (mock test)" — bisa di-cover di Phase 340 sebagai bonus 3rd test, OR defer ke Phase 344 (mutation full test). Per D-10, TEST-01 minimum = 2 fact above. Planner discretion.

---

## Files to Create/Modify

### Create (NEW)

| Path | Purpose | LoC est |
|------|---------|---------|
| `Models/OrganizationLevelLabel.cs` | Entity class (D-11) | ~20 |
| `Services/IOrgLabelService.cs` | Service interface (7 method per D-10) | ~60 (with XML doc) |
| `Services/OrgLabelService.cs` | Service impl (cache + fallback + audit hook) | ~150 |
| `Controllers/OrgLabelController.cs` | NEW controller — GET endpoint Phase 340, page CRUD Phase 341 | ~40 |
| `Migrations/{TIMESTAMP}_AddOrganizationLevelLabel.cs` | EF migration CreateTable + unique index | ~60 (auto-gen) |
| `Migrations/{TIMESTAMP}_AddOrganizationLevelLabel.Designer.cs` | Auto-gen designer | auto |
| `docs/DB_HANDOFF_IT_2026-06-XX.html` | IT handoff doc (D-09) — derive dari `.template.md`, render markdown→HTML via VS Code preview or pandoc | ~150 |
| `HcPortal.Tests/OrgLabelServiceTests.cs` | TEST-01 unit test (xUnit, 2 facts) | ~50 |

### Modify

| Path | Change |
|------|--------|
| `Data/ApplicationDbContext.cs` | (1) Tambah `DbSet<OrganizationLevelLabel> OrganizationLevelLabels` di L18-89 block; (2) Tambah Fluent config block di `OnModelCreating` (after L133) |
| `Data/SeedData.cs` | (1) Tambah `await SeedOrganizationLevelLabelsAsync(context);` di `InitializeAsync` setelah L26; (2) Tambah method `SeedOrganizationLevelLabelsAsync` di akhir class (after L104) |
| `Program.cs` | Tambah `builder.Services.AddScoped<HcPortal.Services.IOrgLabelService, HcPortal.Services.OrgLabelService>();` setelah L62 |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | Auto-update saat `dotnet ef migrations add` (do not edit manual) |
| `HcPortal.Tests/HcPortal.Tests.csproj` | Tambah `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />` |

### Out of Scope (NOT edited Phase 340)

| Path | Reason |
|------|--------|
| `Views/Admin/ManageOrgLevelLabels.cshtml` | Phase 341 |
| `Views/Shared/_ViewImports.cshtml` | `@inject IOrgLabelService` ditambah Phase 343 |
| `wwwroot/js/orgTree.js` | Phase 342 |
| `Views/CMP/*.cshtml`, `Views/CDP/*.cshtml`, dst | Phase 343 |

---

## DB_HANDOFF_IT Specifics

### File location & naming

- Source template: `docs/templates/DB_HANDOFF_IT.template.md`
- Phase 340 deliverable: `docs/DB_HANDOFF_IT_2026-06-XX.html` (XX = day of commit, mis `2026-06-03`)
- Precedent format: `docs/DB_HANDOFF_IT_2026-05-13.html`, `docs/DB_HANDOFF_IT_2026-05-26.html` — styled HTML (Pertamina red `#e30613` brand color, 950px container)
- Generation: copy `.template.md` → fill placeholder → optional render to `.html` via VS Code preview or pandoc per `docs/DEV_WORKFLOW.md` §Pre-Deploy Backup SOP step 3

### Placeholder fill (Phase 340)

| Placeholder | Value |
|-------------|-------|
| `{DATE}` | Commit date YYYY-MM-DD (when Phase 340 PRs ke main) |
| `{COMMIT_HASH}` | Final commit hash Phase 340 (set saat task akhir) |
| `{BRANCH}` | `main` |
| `{YES_NO}` (Migration Flag) | `YES` |
| `{DEVELOPER_NAME}` | Rino |
| `{DEVELOPER_EMAIL}` | (per existing precedent — sama dengan handoff sebelumnya) |
| `{WINDOW_START}` / `{WINDOW_END}` | IT discretion, default 1-hr window business hour |

### MIGRATION_LIST section value

```
- {TIMESTAMP}_AddOrganizationLevelLabel.cs (additive: CREATE TABLE OrganizationLevelLabels + CREATE UNIQUE INDEX IX_OrganizationLevelLabels_Label, no destructive, no data migration)
```

### AFFECTED_TABLES section value

```
- OrganizationLevelLabels (NEW table — zero existing data touched, no ALTER on existing tables)
```

### Special note Phase 340

Tambah baris keterangan tambahan setelah affected_tables:

```
**Seed default (auto-runtime):** SeedData.SeedOrganizationLevelLabelsAsync di Program.cs:134 akan auto-INSERT 3 baris (Level=0 'Bagian', Level=1 'Unit', Level=2 'Sub-unit') saat app first startup setelah migration applied. Idempotent — bila tabel sudah ada baris, skip. Tidak butuh manual INSERT dari IT.
```

Section 1 (Pre-Deploy Backup) tetap MANDATORY per template (Cilacap SOP). Section 4 step 6 (smoke test) update ke:

```
- [ ] Navigate http://10.55.3.3/KPB-PortalHC/Admin/GetLevelLabels (sign in admin@pertamina.com first) → JSON dict { "0": "Bagian", "1": "Unit", "2": "Sub-unit" } returned
```

---

## Code Examples

### Example 1: Service interface full (D-10 = 7 method)

```csharp
// Source: pola Services/INotificationService.cs (XML doc style)
namespace HcPortal.Services
{
    /// <summary>
    /// Org label service — read + cache label tier organisasi (Bagian/Unit/Sub-unit/...).
    /// Phase 340 — milestone v21.0 ORG-LABEL-02.
    /// </summary>
    public interface IOrgLabelService
    {
        /// <summary>Resolve label untuk level. Fallback "Level {N}" bila tidak ada.</summary>
        string GetLabel(int level);

        /// <summary>Get all configured labels as immutable dict.</summary>
        IReadOnlyDictionary<int, string> GetAll();

        /// <summary>Update label existing level. Throws bila level tidak ada.</summary>
        Task UpdateAsync(int level, string label, string userId, string actorName);

        /// <summary>Add new level + label. Throws bila level sudah ada atau label duplicate.</summary>
        Task AddAsync(int level, string label, string userId, string actorName);

        /// <summary>Delete level (highest only, dengan precondition tidak dipakai unit).</summary>
        Task DeleteAsync(int level, string userId, string actorName);

        /// <summary>MAX(Level) dari OrganizationLevelLabels (cached).</summary>
        int GetMaxConfiguredLevel();

        /// <summary>MAX(Level) dari OrganizationUnits (live query, no cache).</summary>
        Task<int> GetMaxUsedLevelAsync();
    }
}
```

**Note signature change vs CONTEXT D-08:** CONTEXT line "GetMaxUsedLevel int" — research recommend `Task<int> GetMaxUsedLevelAsync()` async karena hit DB. Sync `int` dengan `.Result` block thread → planner discretion. Recommend async pure path supaya tidak introduce sync-over-async deadlock risk.

### Example 2: GetMaxUsedLevel implementation

```csharp
public async Task<int> GetMaxUsedLevelAsync()
{
    // D-08: live query, NO cache. Dipanggil jarang dari Phase 341 page render.
    if (!await _context.OrganizationUnits.AnyAsync())
        return 0;  // safety: tabel kosong → return 0
    return await _context.OrganizationUnits.MaxAsync(u => u.Level);
}
```

### Example 3: Endpoint with explicit dict serialization (defensive)

```csharp
// Defensive: explicit Dictionary<string, string> supaya JSON keys deterministic
[HttpGet]
public IActionResult GetLevelLabels()
{
    var dict = _orgLabels.GetAll();
    var jsonDict = dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
    return Json(jsonDict);
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded label "Bagian"/"Unit" di views + controllers | Service-driven dynamic label via DI | Phase 340 (this) | Foundation only — actual replacement Phase 343 |
| INSERT seed di migration body | Runtime idempotent seed di `SeedData.cs` | Project pattern (sejak `SeedOrganizationUnitsAsync` v13.0 P292) | Re-apply safe + customization preserved |
| Direct `_context.AuditLogs.Add` per controller | `AuditLogService.LogAsync` injection | Phase 24 (`Migrations/20260221032754_AddAuditLog.cs`) | Consistent audit format + single source of truth |
| Manual cache management | `IMemoryCache` injection no-TTL + manual invalidate | Phase 311 Plan 03 (precedent `AssessmentAdminController.cs:223` uses TTL; Phase 340 drop TTL per D-02) | Predictable freshness + zero DB hit after first read |

**Deprecated/outdated:**
- `_context.AuditLogs.Add(...)` direct write — replaced by `AuditLogService.LogAsync` (D-04). Phase 340 service WAJIB pakai service.
- `.Result` blocking sync-over-async — never used; all I/O paths async.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Phase 340 build steps fit in standard CONTEXT planning (no NDA blockers, no env-specific gotcha) | All | Low — pattern terbukti di repo |
| A2 | `IReadOnlyDictionary<int, string>` JSON serialization via System.Text.Json menghasilkan `{ "0": "Bagian", ... }` di ASP.NET Core 8 default config | Pitfall 7, Pattern 4 | Medium — bila global JSON config override, planner perlu add Pattern 3 defensive ToDictionary cast (Example 3) |
| A3 | `dotnet ef migrations add` di working tree dengan latest baseline `20260528064336_ChangeValidUntilToDateOnly` tidak conflict dengan uncommitted changes lain | Pitfall 5 | Low — git status clean adalah precondition standar |
| A4 | `Microsoft.EntityFrameworkCore.InMemory` 8.0.0 compatible dengan test pattern + cocok untuk seed data setup | Validation Architecture | Low — paket official Microsoft, version match other EF packages di `HcPortal.csproj:17-19` |
| A5 | Controller `OrgLabelController` plain `Controller` base class (not `AdminBaseController`) cukup karena endpoint Phase 340 tidak butuh `_context`/`_userManager`/`_auditLog`/`_env` | Pattern 4 | Low — Phase 341 nanti boleh inherit kalau page CRUD butuh; bisa refactor saat itu |
| A6 | `GetMaxUsedLevel` async vs sync — research recommend async, CONTEXT D-08 silent on async/sync | Code Examples | Medium — bila CONTEXT intent sync API (untuk view @inject Phase 343 sync render), planner bisa cancel A6 dan implement sync `int GetMaxUsedLevel()` dengan `.GetAwaiter().GetResult()` (risky sync-over-async) or pre-cache the value. Recommend confirm with user via /gsd-discuss-phase OR planner make ad-hoc decision dengan tradeoff dokumentasi |

---

## Open Questions

1. **`GetMaxUsedLevel` async vs sync signature?**
   - What we know: CONTEXT D-08 list 2 method, naming `GetMaxUsedLevel()`. Phase 341 will consume from Razor view + controller render context.
   - What's unclear: Razor view friendly to sync-only methods; controller can await async. Inconsistent signature awkward.
   - Recommendation: Service expose async `Task<int> GetMaxUsedLevelAsync()` (idiomatic EF), controller resolve via await + pass result ke view as plain int. View tidak panggil langsung. Planner update CONTEXT D-08 atau decide ad-hoc.

2. **DB_HANDOFF_IT format: .md or .html?**
   - What we know: Precedents `docs/DB_HANDOFF_IT_2026-05-13.html` + `2026-05-26.html` adalah HTML rendered. Template adalah `.md`. SOP step 3 mention "Optional: render Markdown→HTML pakai pandoc atau VS Code preview".
   - What's unclear: Apakah Phase 340 deliverable .md saja cukup atau wajib render ke .html?
   - Recommendation: Follow precedent — generate `.html` (consistent dengan 2 file sebelumnya). Pakai VS Code "Markdown PDF" extension atau manual copy template HTML inlining content.

3. **Test framework decision lock = xUnit (already confirmed via `HcPortal.Tests.csproj`).** No question.

4. **Existing tabel `OrganizationUnit.Level` semantic** — `SeedData.cs:90` set `Level = 1` untuk RFCC parent + `Level = 2` untuk child unit. Tapi spec D-01 + ORG-LABEL-01 set `Level=0='Bagian'` (root level convention 0-indexed). Mismatch?
   - What we know: `SeedOrganizationUnitsAsync` insert root at `Level = 1` (1-indexed) + child at `Level = 2`. Tapi label seed: Level=0 Bagian, Level=1 Unit, Level=2 Sub-unit (0-indexed).
   - What's unclear: Bila label seed Level=0 = "Bagian" tapi tabel `OrganizationUnits` root unit di-set `Level=1`, maka `GetLabel(unit.Level)` untuk RFCC root return label Level=1 = "Unit" — SALAH SEMANTIC.
   - Recommendation: **CRITICAL — surface ke user via /gsd-discuss-phase**. Options: (a) Update `SeedOrganizationUnitsAsync` set root `Level = 0`, child `Level = 1` (breaking change existing data lokal Dev — perlu data migration script); (b) Update seed label Level=1='Bagian', Level=2='Unit', Level=3='Sub-unit' (1-indexed convention); (c) Document mismatch as Phase 342 fix scope. Planner WAJIB clarify ini sebelum execute Phase 340 — tidak boleh silent.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Build + test | ✓ (assumed) | 8.0.x | — |
| EF Core CLI (`dotnet ef`) | Migration generation | ✓ (assumed per repo precedent) | 8.0.0 | — |
| SQL Server Express / SQLite lokal | DB migration apply + verify | ✓ (assumed per `docs/SEED_WORKFLOW.md` §1) | local | — |
| `Microsoft.EntityFrameworkCore.InMemory` package | xUnit test (TEST-01) | ✗ — not yet referenced di HcPortal.Tests | 8.0.0 (to install) | `dotnet add package` Wave 0 task |
| pandoc / VS Code Markdown preview | DB_HANDOFF_IT HTML render | ✓ likely (VS Code present per developer setup) | n/a | Manual HTML copy from precedent file `DB_HANDOFF_IT_2026-05-26.html` + replace content |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** `Microsoft.EntityFrameworkCore.InMemory` — install via `dotnet add package` di Wave 0 (cheap install, no behavior risk).

---

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua respons ke user — research file tidak strict (mixed acceptable), tapi commit messages + IT notification + plan title prefer Bahasa Indonesia atau bilingual.
- **Develop Workflow gating:** Local verify wajib (`dotnet build` + `dotnet run` + DB verify) sebelum push. Migration file wajib commit bersama code change.
- **JANGAN edit kode/DB langsung di Dev/Prod** — Phase 340 100% lokal. Promo ke Dev = tanggung jawab Team IT setelah developer push + notify dengan DB_HANDOFF_IT artifact.
- **JANGAN ALTER tabel manual** — selalu pakai EF migration.
- **Seed klasifikasi WAJIB sebelum bikin seed.** Phase 340 seed = `permanent + prod-required` per CONTEXT D-01 + `docs/SEED_WORKFLOW.md` §3 — masuk `Data/SeedData.cs`, no journal entry needed (no snapshot/restore cycle).
- **Pre-commit checklist** (`docs/DEV_WORKFLOW.md` §5): build pass tanpa warning baru, dotnet run + manual verify, DB lokal migration apply OK, migration file di-commit, Team IT notify dengan commit hash + migration flag.

---

## Sources

### Primary (HIGH confidence — Context7 / Official docs / repo grep)
- [VERIFIED: HcPortal.csproj:17-23] EF Core 8 + SqlServer + Sqlite packages
- [VERIFIED: HcPortal.Tests/HcPortal.Tests.csproj:12-13] xUnit 2.9.3 + Test.Sdk 17.13.0
- [VERIFIED: Program.cs:17] `services.AddMemoryCache()` registered
- [VERIFIED: Program.cs:51,61,62] `AddScoped` pattern for service interface+impl
- [VERIFIED: Data/SeedData.cs:75-104] `SeedOrganizationUnitsAsync` idempotent template
- [VERIFIED: Data/ApplicationDbContext.cs:131,209-210,315] OnModelCreating + HasIndex IsUnique precedent
- [VERIFIED: Controllers/OrganizationController.cs:11,30-68] `[Route("Admin/[action]")]` + `[Authorize(Roles=...)]` + `Json(...)` return pattern
- [VERIFIED: Controllers/AssessmentAdminController.cs:22,44,223] IMemoryCache injection + GetOrCreate pattern
- [VERIFIED: Services/AuditLogService.cs:21-42] `LogAsync` signature + UTC timestamp
- [VERIFIED: Models/AuditLog.cs:13-51] AuditLog schema field types
- [VERIFIED: Models/OrganizationUnit.cs] entity pattern
- [VERIFIED: docs/templates/DB_HANDOFF_IT.template.md] handoff template structure
- [VERIFIED: docs/SEED_WORKFLOW.md §3] seed klasifikasi rule
- [VERIFIED: docs/DEV_WORKFLOW.md §3-5] Dev → Prod gating + pre-commit checklist
- [VERIFIED: Migrations/ glob] latest baseline migration `20260528064336_ChangeValidUntilToDateOnly`

### Secondary (MEDIUM — official .NET docs)
- [CITED: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection] Scoped/Singleton lifetime + captive dependency
- [CITED: https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties] `ValueGeneratedNever` Fluent API
- [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple] `[Authorize]` attribute semantics
- [CITED: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery] CSRF protection scope
- [CITED: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-collection-types] Dictionary<int,string> serialization .NET 7+

### Tertiary (LOW — training data, needs validation by planner)
- A6 sync vs async `GetMaxUsedLevel` recommendation — based on idiomatic ASP.NET Core 8 design, not user-confirmed
- Open Question #4 (Level convention 0-indexed vs 1-indexed) — surfaced from repo grep, NOT user-confirmed → MUST surface to user before plan execute

---

## Metadata

**Confidence breakdown:**
- Standard Stack: **HIGH** — ASP.NET Core 8 + EF Core 8 SQL Server proven in repo, all patterns exist
- Architecture: **HIGH** — locked decisions in CONTEXT.md cover all design questions
- Pitfalls: **HIGH** — derived from concrete code grep + .NET official docs (not speculation)
- Validation: **MEDIUM** — xUnit framework confirmed, test pattern reusable from `FileUploadHelperTests.cs`, but InMemory provider not yet installed (Wave 0 install needed)
- Open Question #4 (Level convention): **CRITICAL — block plan execution until clarified**

**Research date:** 2026-06-02
**Valid until:** 2026-07-02 (30 days, stable .NET 8 stack)

---

*Next: planner reads this + CONTEXT.md → generate PLAN.md per task. CRITICAL: Open Question #4 MUST be resolved before implementation.*
