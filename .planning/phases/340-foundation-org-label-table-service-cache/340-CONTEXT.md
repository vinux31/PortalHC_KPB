# Phase 340: Foundation — Tabel + Service + Cache — Context

**Gathered:** 2026-06-02
**Status:** Ready for planning
**Milestone:** v21.0 ManageOrganization Overhaul + Level Label CRUD
**Spec:** `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` (commit `b1f4013b`)

<domain>
## Phase Boundary

Phase 340 = P1 Foundation. Lapisan dasar yang dipakai phase 341 (CRUD page), 342 (ManageOrganization fix), 343 (integrasi app-wide). Deliverables:

1. **Tabel** `OrganizationLevelLabels` via EF migration (CREATE TABLE only, NO seed di migration body).
2. **Service** `IOrgLabelService` + `OrgLabelService` dengan in-memory cache, fallback string, audit log hook.
3. **API endpoint** `GET /Admin/GetLevelLabels` untuk konsumsi JS.
4. **Auto-detect depth** via `MAX(OrganizationUnits.Level)` + buffer 1.
5. **Seed default** 3 baris (Bagian/Unit/Sub-unit) via `Data/SeedData.cs` runtime check (idempotent).
6. **DB_HANDOFF_IT** dokumen siap diserahkan ke IT untuk apply migration di Dev.

**Out of scope phase ini:** Page CRUD UI (Phase 341), integrasi view (Phase 343), fix tree page (Phase 342), test E2E (Phase 344).

REQ coverage: ORG-LABEL-01, 02, 03, 07.

</domain>

<decisions>
## Implementation Decisions

### Seed Strategy

- **D-01:** Seed 3 baris default (`Level 0='Bagian'`, `Level 1='Unit'`, `Level 2='Sub-unit'`) ditambah via **`Data/SeedData.cs` runtime check** pattern existing (`if (!context.OrganizationLevelLabels.Any())`). Migration body HANYA `CreateTable`, NO `INSERT` SQL, NO `HasData`.
  - **Why:** Aman dari overwrite data HC custom (mis: "Bagian" → "Direktorat") saat IT re-apply migration / fresh dev / rollback restore `.bak`. Match pola existing `SeedOrganizationUnitsAsync` di `SeedData.cs:26`. Migration transactional safety (CREATE TABLE saja, low fail rate vs CREATE+INSERT bundle).
  - **How to apply:** Tambah method `SeedOrganizationLevelLabelsAsync(context)` di `Data/SeedData.cs` setelah `SeedOrganizationUnitsAsync`. Call dari `SeedData.InitializeAsync` chain. Method sign: `private static async Task SeedOrganizationLevelLabelsAsync(ApplicationDbContext context)`.
  - **Reference:** Lihat `Data/SeedData.cs:26` `SeedOrganizationUnitsAsync` sebagai template idempotent pattern.

### Cache Strategy

- **D-02:** `IMemoryCache` singleton (existing registered via `services.AddMemoryCache()` di Program.cs). Cache key `"OrgLabels:All"` menyimpan `IReadOnlyDictionary<int, string>`. **No TTL, manual invalidate only** (`_cache.Remove("OrgLabels:All")` setiap `UpdateAsync`/`AddAsync`/`DeleteAsync`).
  - **Why:** Spec §4.2 lock decision. Single-server Portal HC KPB → in-memory cache cukup. Label jarang berubah (mungkin 0-2x setahun) → no need TTL fallback. Memory footprint trivial (3-5 entry).
  - **How to apply:** `OrgLabelService` inject `IMemoryCache _cache` + `ApplicationDbContext _context`. `GetAll()` pakai `_cache.GetOrCreate("OrgLabels:All", entry => { ... })`. `UpdateAsync`/`AddAsync`/`DeleteAsync` panggil `_cache.Remove("OrgLabels:All")` sebelum return.

### API Endpoint Auth

- **D-03:** `GET /Admin/GetLevelLabels` auth = `[Authorize]` (authenticated user, no role filter).
  - **Why:** Label = public display info, dipakai JS di banyak page (CMP/CDP/Worker dst) untuk render label dynamic. Strict role filter akan force SSR-only rendering — friction tinggi. Spec §4.5 confirm.
  - **How to apply:** Method signature `[Authorize] [HttpGet] public IActionResult GetLevelLabels()` di `OrgLabelController` atau attach di `OrganizationController` (decision Phase 341).

### Audit Log

- **D-04:** Reuse existing **`AuditLogService`** (`Services/AuditLogService.cs:21` `LogAsync`) instead of direct `_context.AuditLogs.Add`. Inject `AuditLogService` ke `OrgLabelService` constructor. Field mapping ke `AuditLog` schema (`Models/AuditLog.cs`):
  - `ActorUserId` = current user identity ID
  - `ActorName` = NIP + FullName (resolved upstream by controller, passed in)
  - `ActionType` = `"OrgLabel-Add"` / `"OrgLabel-Update"` / `"OrgLabel-Delete"` (`MaxLength(50)` safe, 15-16 char)
  - `Description` = `"Level {N}: '{oldLabel}' → '{newLabel}'"` (Update) / `"Level {N}: '{newLabel}' created"` (Add) / `"Level {N}: '{oldLabel}' deleted"` (Delete)
  - `TargetId` = level (int)
  - `TargetType` = `"OrganizationLevelLabel"` (`MaxLength(100)` safe)
  - **Why:** `AuditLogService.LogAsync` sudah encapsulate insert + `SaveChangesAsync`. Konsisten dgn pattern existing (CMPController/AssessmentAdminController via DI). Tidak duplicate logic.
  - **How to apply:** Phase 340 implement `OrgLabelService` mutation methods (`UpdateAsync`/`AddAsync`/`DeleteAsync`) yang call `_auditLog.LogAsync(...)` setelah DB save + cache invalidate. Endpoint `GET /Admin/GetLevelLabels` (Phase 340 deliverable) sendiri read-only — tidak audit. Phase 341 controller forward `actorUserId` + `actorName` ke service method.

### Migration

- **D-05:** Migration name = `AddOrganizationLevelLabel` (singular). Body = `CreateTable` only. Schema:
  - `Level int PRIMARY KEY` (no identity — value is the level number itself)
  - `Label nvarchar(50) NOT NULL`
  - `UpdatedAt datetime2 NOT NULL` (UTC)
  - `UpdatedBy nvarchar(450) NOT NULL`
  - **Unique index:** `IX_OrganizationLevelLabels_Label` UNIQUE — via Fluent API `entity.HasIndex(e => e.Label).IsUnique()`.
  - **Why:** Spec §4.1 + post-brainstorm fix. Level int PK natural (level numerik adalah identifier alami). Label unique mencegah HC bikin 2 level dgn label sama.
  - **How to apply:** `dotnet ef migrations add AddOrganizationLevelLabel` setelah model + DbContext config. Verify `.Designer.cs` generated. Snapshot lokal sebelum apply.

### Service Lifetime

- **D-06:** `OrgLabelService` registered **Scoped** (`services.AddScoped<IOrgLabelService, OrgLabelService>()`). `IMemoryCache` tetap Singleton (existing).
  - **Why:** Service inject `ApplicationDbContext` (Scoped) → service harus Scoped untuk avoid captive dependency. Cache singleton OK karena di-share via `IMemoryCache`.
  - **How to apply:** Tambah di `Program.cs` setelah `AddMemoryCache()`.

### Fallback Format

- **D-07:** `GetLabel(int level)` fallback bila level tidak ada di tabel/cache = string `$"Level {level}"` (e.g., level 99 → `"Level 99"`).
  - **Why:** Spec §4.2. Tidak null (avoid NRE downstream). Predictable + debuggable.

### GetMaxLevel Methods

- **D-08:** Service expose 2 method:
  - `int GetMaxConfiguredLevel()` — `MAX(OrganizationLevelLabels.Level)` cached dgn dict.
  - `int GetMaxUsedLevel()` — `MAX(OrganizationUnits.Level)` live DB query (no cache, dipanggil jarang dari Phase 341 page render).
  - **Why:** Phase 341 page UI butuh `max(used, configured) + 1` untuk render row count. Pisah method jaga semantik jelas.

### Service Scope (Phase 340 vs Phase 341)

- **D-10:** Phase 340 implement **FULL** `IOrgLabelService` interface — semua 7 method (`GetLabel`, `GetAll`, `UpdateAsync`, `AddAsync`, `DeleteAsync`, `GetMaxConfiguredLevel`, `GetMaxUsedLevel`). Mutation method (`UpdateAsync`/`AddAsync`/`DeleteAsync`) **belum dipanggil dari mana-mana di Phase 340** — Phase 341 controller akan consume.
  - **Why:** Boundary phase clean — Phase 340 = data + service layer complete. Phase 341 = UI consumer + endpoint binding. Bila split mutation di 341, service split antara 2 phase → reasoning lebih sulit. Spec §4.2 list 7 method tanpa pembagian phase.
  - **How to apply:** Phase 340 implementation include test minimal untuk `GetLabel` happy path + fallback (TEST-01 dari REQ list). Mutation method tested formally di Phase 344. Phase 341 controller tinggal panggil.

### Model Class

- **D-11:** Create `Models/OrganizationLevelLabel.cs` (singular class name, plural table name `OrganizationLevelLabels`). Schema match D-05 + EF Fluent API config (D-05 list HasKey/ValueGeneratedNever/Property MaxLength/HasIndex IsUnique).
  - **Why:** Convention existing — model class singular (lihat `OrganizationUnit.cs` + `AuditLog.cs`), `DbSet<>` property plural.
  - **How to apply:** Tambah `public class OrganizationLevelLabel { int Level; string Label; DateTime UpdatedAt; string UpdatedBy; }` di `Models/`. Tambah `public DbSet<OrganizationLevelLabel> OrganizationLevelLabels { get; set; }` di `ApplicationDbContext`. Tambah Fluent config di `OnModelCreating`.

### DB_HANDOFF_IT

- **D-09:** Phase 340 deliverable include `docs/DB_HANDOFF_IT_2026-06-XX.html` generated dari template `docs/templates/DB_HANDOFF_IT.template.md`. Isian:
  - `MIGRATION_LIST`: 1 migration `AddOrganizationLevelLabel.cs` (additive: CreateTable, no destructive)
  - `AFFECTED_TABLES`: NEW table `OrganizationLevelLabels` (zero existing data touched)
  - Backup `.bak` tetap WAJIB per SOP (Section 1 mandatory)
  - **Why:** Per CLAUDE.md Dev Workflow + root cause Cilacap incident Phase 336. Migration handoff harus formal supaya IT tidak skip backup.
  - **How to apply:** Generate file saat plan task implementation done, file commit bersama task akhir Phase 340.

### Claude's Discretion

- Internal namespace organization (Services subfolder vs root): Claude pilih konsisten dgn existing — `Services/IOrgLabelService.cs` + `Services/OrgLabelService.cs` flat (sama dengan `INotificationService.cs` + `NotificationService.cs`).
- XML doc verbosity: standard pattern (like `INotificationService.cs`), summary per method.
- Test coverage approach: pure unit test di Phase 340 minimal (basic happy path GetLabel). Integration test full di Phase 344.
- Controller class: NEW `OrgLabelController.cs` (Phase 340 mount `GetLevelLabels` endpoint). Phase 341 reuse controller untuk page CRUD endpoints. Avoid clutter `OrganizationController.cs`.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Requirements
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` — Full design spec (600 baris). Sections paling relevant untuk Phase 340: §3 Arsitektur, §4.1 Tabel, §4.2 Service, §4.5 API endpoint, §5 DI Registration, §8 Migration & Deployment.
- `.planning/milestones/v21.0-REQUIREMENTS.md` — ORG-LABEL-01..07 + dependency map.
- `.planning/milestones/v21.0-ROADMAP.md` — Phase 340 success criteria (5 item).

### Existing Code Patterns (read SEBELUM implement)
- `Data/SeedData.cs:26` `SeedOrganizationUnitsAsync` — template idempotent runtime seed pattern (replicate untuk OrganizationLevelLabels).
- `Services/INotificationService.cs` — interface XML doc + method sign pattern.
- `Services/NotificationService.cs` — impl pattern.
- `Models/AuditLog.cs` — schema audit log existing (reuse, jangan bikin baru).
- `Controllers/AssessmentAdminController.cs:221` — `IMemoryCache.GetOrCreateAsync` pattern (referensi cache usage; Phase 340 pakai sync `GetOrCreate` karena dict kecil + no async DB di hot path setelah first read).

### Workflow & SOP
- `CLAUDE.md` — Project workflow rules (Dev → Prod gating, IT-managed deploy).
- `docs/DEV_WORKFLOW.md` — Lokal verify steps (`dotnet build` + `dotnet run http://localhost:5277` + DB lokal).
- `docs/SEED_WORKFLOW.md` — Seed klasifikasi permanent vs temporary. `OrganizationLevelLabels` = **permanent + prod-required**.
- `docs/templates/DB_HANDOFF_IT.template.md` — Template HTML untuk handoff IT (output Phase 340).

### Predecessor Context
- `.planning/milestones/v20.0-phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-ROOT_CAUSE.md` — Root cause kenapa DB_HANDOFF_IT MANDATORY untuk semua migration.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`IMemoryCache` already registered** di `Program.cs` (existing `services.AddMemoryCache()` via standard ASP.NET pipeline). Inject langsung di `OrgLabelService` constructor, tidak perlu register ulang.
- **`Data/SeedData.cs`** sudah punya `InitializeAsync` chain. Tambah `await SeedOrganizationLevelLabelsAsync(context)` setelah existing `SeedOrganizationUnitsAsync`.
- **`Services/AuditLogService.cs:21`** `LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` — inject + reuse. JANGAN tulis `_context.AuditLogs.Add` langsung.
- **`Models/AuditLog.cs`** schema fields: `ActorUserId` + `ActorName` + `ActionType` (MaxLength 50) + `Description` + `TargetId?` + `TargetType?` (MaxLength 100) + `CreatedAt`.
- **`ApplicationDbContext`** sudah punya banyak `DbSet<>` properties. Tambah `public DbSet<OrganizationLevelLabel> OrganizationLevelLabels { get; set; }`.

### Established Patterns

- **Service interface + impl flat** di `Services/` (NotificationService, AuthService, WorkerDataService, ImpersonationService). XML doc per method dgn `<summary>`, `<param>`, `<returns>`.
- **IMemoryCache pattern** dipakai di AssessmentAdminController dengan `GetOrCreateAsync` TTL 5min absolute expiration. Phase 340 beda — no TTL.
- **Idempotent seed** via `if (!await context.X.AnyAsync()) { ... AddRange ... SaveChangesAsync }` di `SeedData.cs`.
- **Audit log insert** via `_context.AuditLogs.Add(new AuditLog { ... })` + `await _context.SaveChangesAsync()` di dalam transaction yang sama dengan operasi business.
- **Migration naming** convention: `{YYYYMMDDHHMMSS}_{PascalCaseDescription}.cs`. Phase 340 = `{TIMESTAMP}_AddOrganizationLevelLabel.cs`.

### Integration Points

- **`Program.cs`**: tambah `services.AddScoped<IOrgLabelService, OrgLabelService>()` setelah `AddMemoryCache()`.
- **`ApplicationDbContext.OnModelCreating`**: tambah Fluent API config untuk `OrganizationLevelLabel`:
  ```csharp
  modelBuilder.Entity<OrganizationLevelLabel>(entity =>
  {
      entity.HasKey(e => e.Level);
      entity.Property(e => e.Level).ValueGeneratedNever(); // Level adalah identifier alami, bukan auto-increment
      entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
      entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(450);
      entity.HasIndex(e => e.Label).IsUnique();
  });
  ```
- **`_ViewImports.cshtml`**: tambah `@inject HcPortal.Services.IOrgLabelService OrgLabels` untuk Phase 343 view integration (Phase 340 prep namespace, tidak edit `_ViewImports` di Phase 340).
- **`SeedData.InitializeAsync` chain**: insert call SETELAH `SeedOrganizationUnitsAsync` (order matters bila depth detection dipanggil saat seed — TIDAK kasus Phase 340; safe order apa pun).

</code_context>

<specifics>
## Specific Ideas

- **Controller naming:** NEW `Controllers/OrgLabelController.cs`. Phase 340 mount `[Authorize] [HttpGet] GetLevelLabels`. Phase 341 nanti reuse untuk page Index + CRUD action. Avoid menumpuk di `OrganizationController.cs` yang sudah besar.
- **Route prefix:** ikuti pattern existing AdminController route `/Admin/X`. Endpoint `GET /Admin/GetLevelLabels` → `OrgLabelController` route via `[Route("Admin/GetLevelLabels")]` attribute atau inherit base. Phase 341 nanti tambah `/Admin/ManageOrgLevelLabels` page route.
- **No `[AutoValidateAntiforgeryToken]` di GET endpoint** — read-only, tidak butuh CSRF. POST endpoints Phase 341 nanti pakai `[ValidateAntiForgeryToken]`.
- **Cache value type:** `IReadOnlyDictionary<int, string>` (immutable view). Service return read-only API supaya consumer tidak modify cache content.

</specifics>

<deferred>
## Deferred Ideas

- **WebSocket push label refresh** ke tab user yang sedang buka page lain — out of scope per spec §2 Non-Goals. Manual refresh acceptable.
- **i18n multi-language label** — single-string per level, out of scope.
- **Multi-server cache invalidation strategy** — Portal HC KPB single-server, irrelevant.
- **Tom Select integration** untuk dropdown induk besar (>100 unit) — defer hingga skala butuh.
- **Search box di tree ManageOrganization** — UX innovation #5 dari brainstorm, defer.
- **Drag-reparent across siblings** — UX innovation #6, defer.
- **Stats breakdown Aktif/Nonaktif** — UX innovation #8, defer.

### Reviewed Todos (not folded)

(`todo match-phase 340` returned 0 matches — no relevant pending todos.)

</deferred>

---

*Phase: 340-foundation-org-label-table-service-cache*
*Context gathered: 2026-06-02*
*Next: /gsd-plan-phase 340*
