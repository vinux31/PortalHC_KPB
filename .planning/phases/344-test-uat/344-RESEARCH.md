# Phase 344: Test + UAT - Research

**Researched:** 2026-06-04
**Domain:** Test infrastructure (xUnit + EF Core 8 + Playwright/TS) for verifying shipped code (Phase 340-343), milestone v21.0
**Confidence:** HIGH (all SUT/test-infra claims verified by direct codebase read; EF Core integration pattern CITED from docs)

> RESEARCH.md is agent-facing (consumed by `gsd-planner`) — written in technical English per orchestrator instruction. Project user-facing language remains Bahasa Indonesia (CLAUDE.md).

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Unit TEST-01..04):** Fill gaps only. Keep the 23 existing [Fact] (OrgLabelService 13 + OrgLabelController 10) verbatim — DO NOT rewrite/duplicate. Add only:
  - TEST-02: permission denial non-Admin/non-HC (403) on `OrgLabelController` (validation side already ✅).
  - TEST-03: pre-order DFS sort correctness for 3-level + multi-root tree — verify first whether already in `OrganizationControllerTests`; add if missing.
  - TEST-04: dup-name per-parent — verify first in `OrganizationControllerTests`; add only if missing.
  - Planner MUST inspect `OrganizationControllerTests.cs` before writing new tests to avoid duplication.
- **D-02 (Integration TEST-05):** Disposable separate SQL Server LocalDB / test DB (`HcPortalDB_Test` or LocalDB instance). EF InMemory does NOT validate real migration SQL. Migrate fresh + seed default + service first read + assert, then **drop per run**. MUST NOT touch `HcPortalDB_Dev` → no SEED_WORKFLOW snapshot/restore needed (clean isolation via separate DB). Validates: migration apply succeeds + default OrgLabel seed rows present + `OrgLabelService` first read returns configured label (not fallback).
- **D-03 (Playwright TEST-06):** NEW spec file `tests/e2e/manage-org-label.spec.ts` using existing `global.setup.ts`/`global.teardown.ts` + `helpers/` patterns (consistent with 9 other specs). DO NOT mix into another domain spec. 5 scenarios: tree load + legend visible; dropdown pre-order + inactive parent shown; cascade warning modal count accurate; label CRUD live rename → tree updated; new label visible in 2+ integration pages (CMP + Worker form).
- **D-04 (Manual UAT vs Automation):** Maximize Playwright (automate 4), thin manual (1 visual). Automate: (1) HC rename label → check 7 areas, (2) Admin add Bagian → dynamic title, (3) "Operations" in 2 different Bagian OK, (4) deactivate parent → edit child reparent OK. Manual thin: (5) Edit large Bagian → cascade warning with **correct count** (visual count-accuracy judgment) + **5 regression smoke** (tree drag-reorder, toggle active, delete unit, add unit existing flow) per ORG-INTEG-03. Manual doc = thin `344-HUMAN-UAT.md` (only non-automated checklist items), user executes at `http://localhost:5277`.

### Claude's Discretion
- Test method naming, arrange/act/assert structure, internal fixture/helper.
- Disposable test-DB provisioning mechanism (xUnit fixture / `IClassFixture` / test connection-string config) — as long as it drops per run and does not touch dev DB.
- Seed SQL detail for Playwright (via existing global.setup pattern).

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within Phase 344 scope (test + UAT v21.0).
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TEST-01 | xUnit `OrgLabelService.GetLabel` happy + fallback `"Level N"` | **ALREADY DONE** — `OrgLabelServiceTests.GetLabel_KnownLevel_ReturnsConfiguredLabel` + `GetLabel_UnknownLevel_ReturnsFallback`. No new work. Verify-only. |
| TEST-02 | xUnit `OrgLabelController` permission denial non-Admin/non-HC + validation reject | **PARTIAL.** Validation reject (empty/dup/>50) DONE (7 [Fact]). Permission denial = GAP. SUT uses `[Authorize(Roles="Admin, HC")]` attribute — see Q2: NOT exercisable by direct controller unit test. Recommend reflection-based attribute assertion (unit) + Playwright 403 (E2E). |
| TEST-03 | xUnit pre-order DFS sort correctness 3-level + multi-root | **GAP + ARCHITECTURE RISK.** Pre-order DFS (`flattenTreePreOrder`) lives in **client-side JS** (`wwwroot/js/orgTree.js:308-317`), NOT C#. No C# SUT exists. See Q1 for the three viable options (recommend: server-side helper extraction OR Playwright DOM-order assertion). |
| TEST-04 | xUnit dup-name per-parent (same name OK different parent, rejected same parent) | **ALREADY DONE** — `OrganizationControllerTests` has `AddOrganizationUnit_SameNameDifferentParent_Accepted` + `SameNameSameParent_Rejected` + `EditOrganizationUnit_SameNameSameParent_Rejected`. No new work. Verify-only. |
| TEST-05 | Integration test migration apply + seed default + service first read | **GAP.** Needs real SQL Server (D-02). See Q3 — `Database.Migrate()` + `SeedData.SeedOrganizationLevelLabelsAsync` + `OrgLabelService.GetLabel(0)=="Bagian"`. Tests project currently only references EF InMemory provider. |
| TEST-06 | Playwright E2E 5 scenarios | **GAP.** New spec `manage-org-label.spec.ts`. See Q4 — routes/login/seed patterns established. |
| ORG-INTEG-03 | No regression on existing functions (tree CRUD, drag-reorder, toggle active, integrated pages) | Covered by D-04 manual regression smoke (5 items) + the 4 automated Playwright scenarios. See Validation Architecture. |
</phase_requirements>

---

## Summary

Phase 344 is a **test-gap-closure + UAT** phase — no feature code. The orchestrator's grounding is confirmed: 23 existing [Fact] cover TEST-01 (GetLabel happy+fallback ✅) and TEST-04 (dup-name per-parent ✅) **completely** — those two requirements are verify-only, not write-new. The real work is four gaps: TEST-02 permission denial, TEST-03 DFS sort, TEST-05 real-DB integration, TEST-06 Playwright spec.

The single most important finding for planning: **the pre-order DFS sort (TEST-03) is client-side JavaScript** (`wwwroot/js/orgTree.js`, function `flattenTreePreOrder`, lines 308-317), not a C# method. There is no server-side ordering SUT — `GetOrganizationTree` returns a flat list ordered by `Level, DisplayOrder, Name`, and the tree/dropdown pre-order assembly happens entirely in the browser (`buildTree` + `flattenTreePreOrder`). xUnit cannot test JS. The planner must choose one of three approaches (Q1) — recommended: extract a tiny pure C# pre-order helper and unit-test it, OR assert dropdown `<option>` DOM order via Playwright (lower-cost, validates the actual shipped path).

The second key finding: **TEST-02 authorization cannot be exercised by direct controller unit tests.** `OrgLabelController` enforces auth via `[Authorize(Roles="Admin, HC")]` attributes (class + per-action), which the ASP.NET pipeline evaluates — a directly-instantiated controller (the existing test pattern) bypasses the pipeline entirely. Recommended: assert attribute presence via reflection (cheap, deterministic, xUnit) AND cover the live 403 via Playwright (real pipeline).

**Primary recommendation:** Treat TEST-01/TEST-04 as verify-only (run existing suite, confirm green). Add: (a) reflection attribute test for TEST-02 + Playwright 403; (b) a pure C# pre-order helper for TEST-03 (or Playwright DOM-order assertion); (c) one `IClassFixture` real-SQL-Server integration test for TEST-05 marked `[Trait("Category","Integration")]` (skippable when SQL Server absent — DEV_WORKFLOW guarantees local SQLEXPRESS); (d) `manage-org-label.spec.ts` reusing `accounts`/`loginAny`/`global.setup`. Manual UAT doc thin per D-04.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| GetLabel happy/fallback (TEST-01) | API/Service (`OrgLabelService`) | — | Pure service method, in-memory testable. Already covered. |
| Permission denial (TEST-02) | ASP.NET auth pipeline (attribute) | E2E (browser) | `[Authorize(Roles)]` evaluated by middleware, not in controller body → not unit-testable in isolation; reflection + Playwright. |
| Pre-order DFS sort (TEST-03) | **Browser/Client (JS)** | API (if helper extracted) | `flattenTreePreOrder` runs in browser; flat data comes from API. SUT is JS unless a C# helper is extracted. |
| Dup-name per-parent (TEST-04) | API/Controller (`OrganizationController`) | — | Server-side `AnyAsync(Name && ParentId)` check, InMemory testable. Already covered. |
| Migration + seed + first read (TEST-05) | Database/Storage + Service | — | Validates real SQL Server schema + idempotent seed + cached service read. Requires real provider. |
| 5 E2E flows (TEST-06) | Browser → SSR → API → DB (full stack) | — | End-to-end; Playwright against running app on :5277. |
| Regression smoke (ORG-INTEG-03) | Full stack | — | Manual + automated; exercises tree CRUD/reorder/toggle. |

## Standard Stack

### Core (already present — DO NOT change versions without reason)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| xunit | 2.9.3 | Unit + integration test framework | [VERIFIED: HcPortal.Tests.csproj:14] existing |
| xunit.runner.visualstudio | 3.0.1 | Test runner | [VERIFIED: csproj:15] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test host | [VERIFIED: csproj:13] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Fast unit-test DB (TEST-01..04) | [VERIFIED: csproj:12] existing pattern |
| @playwright/test | (tests/) | E2E (TypeScript) | [VERIFIED: tests/playwright.config.ts] existing, 9 specs |
| coverlet.collector | 6.0.4 | Coverage | [VERIFIED: csproj:19] |

### Supporting (must ADD for TEST-05)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real SQL Server provider for integration test | **ADD to HcPortal.Tests.csproj.** Main project HcPortal.csproj already references it [VERIFIED: HcPortal.csproj:18] and it flows transitively via ProjectReference, but add explicitly to the Tests csproj so the integration test's `UseSqlServer(...)` resolves unambiguously and intent is clear. Match version 8.0.0 to avoid mismatch. [ASSUMED: transitive availability is sufficient — but explicit add is the safe, idiomatic choice.] |

**Installation (TEST-05 only):**
```bash
dotnet add HcPortal.Tests/HcPortal.Tests.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
```

**Version verification note:** Versions above are read directly from the repo csproj files (current as of 2026-06-04). No registry lookup needed — match the existing 8.0.0 EF Core line to stay consistent with the app.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Real SQL Server (D-02) | EF InMemory | InMemory does NOT execute migration DDL or validate the real schema → would not satisfy TEST-05's intent. D-02 locks real DB. |
| Real SQL Server (D-02) | SQLite in-memory | Closer to relational than InMemory but still not the production engine; migration DDL (nvarchar(50), unique index) wouldn't be validated against actual SQL Server. D-02 locks SQL Server. |
| LocalDB | SQLEXPRESS `HcPortalDB_Test` | Both acceptable per D-02. SQLEXPRESS is already installed (dbSnapshot.ts uses `localhost\SQLEXPRESS`), so a transient `HcPortalDB_Test_<guid>` on SQLEXPRESS is the lowest-friction choice (no LocalDB install dependency). Recommend SQLEXPRESS. |

## Architecture Patterns

### System Architecture Diagram (test flows)

```
                          PHASE 344 TEST FLOWS

[xUnit unit, EF InMemory]                 [xUnit integration, real SQL Server]
  OrgLabelServiceTests  ───┐               provision HcPortalDB_Test_<guid>
  OrgLabelControllerTests ─┤                       │
  OrganizationControllerTests ┤            ApplicationDbContext(UseSqlServer)
  (+NEW: attr-reflection,    │                     │ Database.Migrate()  ◄── validates DDL
   pre-order helper)         │                     │ SeedData.SeedOrganizationLevelLabelsAsync()
        │                    │                     │ new OrgLabelService(ctx, cache, audit)
        ▼                    │                     │ svc.GetLabel(0) == "Bagian"  (NOT fallback)
   assert (in-proc, ms)      │                     │ assert
                             │                     ▼
                             │              DROP DATABASE (IAsyncLifetime.DisposeAsync)
                             │
[Playwright E2E, TS]  ───────┘
  global.setup.ts: app-check + BACKUP HcPortalDB_Dev + seed SQL + Layer1 validate
        │
   manage-org-label.spec.ts (NEW)
        │  loginAny(page,'admin'|'hc')  →  POST /Account/Login  → redirect
        ├─ GET /Admin/ManageOrganization   (tree, legend, dropdown pre-order, cascade modal)
        ├─ GET /Admin/ManageOrgLevelLabels (label CRUD rename)
        ├─ GET /CMP/Records                (integration label — Team View tab)
        └─ GET /Admin/CreateWorker         (integration label — Bagian/Unit selects)
        │
   global.teardown.ts: flush report + RESTORE HcPortalDB_Dev + Layer4 validate (0 rows)
```

### Existing test project structure (reuse, don't reinvent)
```
HcPortal.Tests/
├── OrgLabelServiceTests.cs        # 13 [Fact] — TEST-01 ✅ (verify-only)
├── OrgLabelControllerTests.cs     # 7 [Fact]  — TEST-02 validation ✅; ADD perm test here
├── OrganizationControllerTests.cs # 6 [Fact]  — TEST-04 ✅; ADD pre-order helper test (or new file)
├── CertificateStatusTests.cs      # unrelated
└── FileUploadHelperTests.cs       # unrelated
                                    # NEW (TEST-05): OrgLabelMigrationIntegrationTests.cs
tests/
├── playwright.config.ts           # baseURL http://localhost:5277, globalTeardown wired
├── helpers/accounts.ts            # admin + hc + 8 roles, all pwd 123456
├── helpers/dbSnapshot.ts          # sqlcmd wrapper: backup/restore/execScript/queryScalar
└── e2e/
    ├── global.setup.ts            # app-check + BACKUP + seed + Layer1
    ├── global.teardown.ts         # flush + RESTORE + Layer4
    ├── manage-assessment-filter.spec.ts  # representative admin-page spec (login pattern)
    └── manage-org-label.spec.ts   # NEW (TEST-06)
```

### Pattern 1: Direct-instantiation controller unit test (EF InMemory) — existing
**What:** Build `ApplicationDbContext` on `UseInMemoryDatabase(Guid)`, seed rows, instantiate controller with `null!` substitutes for un-dereferenced deps (`UserManager`, `IWebHostEnvironment`), set `ControllerContext` with `DefaultHttpContext`, assert on `JsonResult` via reflection helpers.
**When to use:** TEST-02 validation rejects, TEST-04 dup-name — anything that returns before dereferencing `UserManager` and does NOT depend on the auth pipeline.
**Example:**
```csharp
// Source: HcPortal.Tests/OrganizationControllerTests.cs:18-32 (verified)
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
var ctx = new ApplicationDbContext(options);
var auditLog = new AuditLogService(ctx);
#pragma warning disable CS8625
var ctrl = new OrganizationController(ctx, null!, auditLog, null!);
#pragma warning restore CS8625
var httpContext = new DefaultHttpContext();
httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest"; // → Json response
ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
```

### Pattern 2: Reflection-based authorization-attribute assertion (TEST-02 permission gap)
**What:** Since `[Authorize(Roles=...)]` is enforced by middleware (not in the action body), the directly-instantiated controller does NOT enforce it. The deterministic, dependency-free way to verify in xUnit is to assert the attribute's presence + roles via reflection.
**When to use:** TEST-02 "permission denial non-Admin/non-HC" at the unit level. Pair with Playwright for live 403.
**Example:**
```csharp
// Verifies class-level + action-level [Authorize(Roles="Admin, HC")] declared.
// Source pattern: standard reflection over MethodInfo.GetCustomAttributes (CITED: learn.microsoft.com/aspnet/core/mvc/controllers/testing)
using Microsoft.AspNetCore.Authorization;

[Fact]
public void UpdateLevelLabel_RequiresAdminOrHcRole()
{
    var m = typeof(OrgLabelController).GetMethod(nameof(OrgLabelController.UpdateLevelLabel))!;
    var attr = m.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
    Assert.NotNull(attr);
    Assert.Equal("Admin, HC", attr!.Roles);   // matches OrgLabelController.cs:52,105,140,181
}
```
**Note for planner:** Repeat per protected action (`ManageOrgLevelLabels`, `UpdateLevelLabel`, `AddLevelLabel`, `DeleteLevelLabel`). `GetLevelLabels` is intentionally `[Authorize]` only (any authenticated user) — assert it does NOT carry Roles="Admin, HC" to lock the documented contract (ORG-LABEL-03). The live 403 for a non-Admin/non-HC user is verified in Playwright (Q4 / D-04 scenario set, e.g. login as `coach` → GET `/Admin/ManageOrgLevelLabels` → expect redirect/403).

### Pattern 3: Disposable real-SQL-Server integration test (TEST-05)
**What:** `IClassFixture` (or `IAsyncLifetime`) creates a uniquely-named DB on `localhost\SQLEXPRESS`, runs `Database.Migrate()` (NOT `EnsureCreated()`), seeds defaults via the production seed method, exercises the service, then drops the DB in disposal.
**When to use:** TEST-05 only.
**Why `Migrate()` not `EnsureCreated()`:** `EnsureCreated()` builds the schema directly from the model and **skips the migrations pipeline entirely** — it would NOT validate that `20260603012335_AddOrganizationLevelLabel` applies cleanly (the whole point of TEST-05). `Database.Migrate()` runs every migration's `Up()` (real DDL: `nvarchar(50)`, unique index `IX_OrganizationLevelLabels_Label`) and writes `__EFMigrationsHistory`, exactly mirroring what IT will run on Dev/Prod. [CITED: learn.microsoft.com/ef-core/managing-schemas/migrations — "Migrate() applies pending migrations; EnsureCreated bypasses migrations and is incompatible with them."]
**Seed source (CRITICAL):** The migration does **NOT** seed via `HasData` — [VERIFIED: Migrations/20260603012335_AddOrganizationLevelLabel.cs only `CreateTable`+`CreateIndex`, no `InsertData`]. The 3 default rows come from `SeedData.SeedOrganizationLevelLabelsAsync(context)` [VERIFIED: Data/SeedData.cs:114-127, idempotent, inserts Level 0=Bagian/1=Unit/2=Sub-unit]. So the integration test MUST call this method after `Migrate()` — asserting rows appear *only after seeding* is the correct contract (don't expect migration alone to populate).
**Example:**
```csharp
// Source: composed from EF Core 8 docs + repo seed method (verified). NOTE async DB drop in dispose.
public class OrgLabelMigrationFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // localhost-only, Integrated Security — mirrors appsettings.Development.json + dbSnapshot.ts guard.
        var cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;" +
                 "TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;

        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.MigrateAsync();                       // validates real migration DDL
        await SeedData.SeedOrganizationLevelLabelsAsync(ctx);    // production seed path (idempotent)
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();                 // drop per run (D-02)
    }
}

[Trait("Category", "Integration")]   // see Q3b — lets SQL-less CI filter this out
public class OrgLabelMigrationIntegrationTests : IClassFixture<OrgLabelMigrationFixture>
{
    private readonly OrgLabelMigrationFixture _fx;
    public OrgLabelMigrationIntegrationTests(OrgLabelMigrationFixture fx) => _fx = fx;

    [Fact]
    public void Migrate_Seed_FirstRead_ReturnsConfiguredLabel_NotFallback()
    {
        using var ctx = new ApplicationDbContext(_fx.Options);
        var svc = new OrgLabelService(ctx, new MemoryCache(new MemoryCacheOptions()), new AuditLogService(ctx));
        Assert.Equal("Bagian",   svc.GetLabel(0));   // configured, NOT "Level 0"
        Assert.Equal("Unit",     svc.GetLabel(1));
        Assert.Equal("Sub-unit", svc.GetLabel(2));
        Assert.Equal("Level 99", svc.GetLabel(99));  // fallback still works on real DB
        Assert.Equal(3, ctx.OrganizationLevelLabels.Count());
    }
}
```
**Gotcha — `ApplicationDbContext` ctor:** [VERIFIED] tests already construct `new ApplicationDbContext(options)` directly (no Identity-store complications for this read-path), so the SqlServer-backed context constructs identically — only the provider differs. `Database.MigrateAsync()` will create Identity/AspNetUsers tables too (the full schema), which is fine and proves the whole migration chain.
**Gotcha — connection-string isolation:** The unique `Guid` DB name guarantees no collision with `HcPortalDB_Dev` and supports parallel/repeat runs. `EnsureDeletedAsync()` drops it. There is NO appsettings change and NO touch of `HcPortalDB_Dev` (D-02 satisfied; SEED_WORKFLOW snapshot not required).

### Pattern 4: Playwright admin-page spec (TEST-06) — existing conventions
**What:** Import `accounts` + inline `loginAny`, `page.goto(route)`, wait for a known selector, assert DOM. Login = real form POST to `/Account/Login`, wait for redirect away from login. No `storageState` is used — each test logs in fresh.
**When to use:** All 5 TEST-06 scenarios.
**Example:**
```typescript
// Source: tests/e2e/manage-assessment-filter.spec.ts:25-41 (verified)
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

async function loginAny(page: Page, key: AccountKey) {
  const { email, password } = accounts[key];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(u => !u.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}
// baseURL http://localhost:5277 from playwright.config.ts → use relative paths.
```

### Anti-Patterns to Avoid
- **Duplicating TEST-01/TEST-04 coverage** — D-01 forbids it. `GetLabel` happy+fallback and dup-name per-parent are fully covered. Re-asserting them is waste + drift risk.
- **Using `EnsureCreated()` for TEST-05** — bypasses migrations; defeats the requirement (see Pattern 3).
- **Trying to unit-test the JS DFS via xUnit** — `flattenTreePreOrder` is JS; there is no C# SUT (see Q1). Either extract a C# helper or test via Playwright DOM order.
- **Asserting 403 from a directly-instantiated controller** — auth attributes aren't enforced without the pipeline; the call would succeed and the test would be meaningless. Use reflection + Playwright.
- **Touching `HcPortalDB_Dev` in TEST-05** — D-02 mandates a separate disposable DB. Mixing would require SEED_WORKFLOW snapshot/restore and risk dev data.
- **Case-insensitivity assumptions in InMemory dup-name tests** — [VERIFIED note in OrganizationControllerTests.cs:4, Pitfall 5] InMemory is case-sensitive but SQL Server default collation is case-insensitive; keep casing identical in test data so behavior matches across providers.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Seeding default OrgLabel rows in TEST-05 | Inline `AddRange` of 3 rows | `SeedData.SeedOrganizationLevelLabelsAsync(ctx)` | Tests the actual production seed path (idempotent, prod-required). Inline copy drifts from prod. [VERIFIED: SeedData.cs:114] |
| SQL Server backup/restore/scalar in Playwright | New sqlcmd code | `helpers/dbSnapshot.ts` (`backup`/`restore`/`execScript`/`queryScalar`/`queryString`) | Already has localhost-only guard, `-b` error-propagation, default-backup-path resolution. [VERIFIED: dbSnapshot.ts] |
| Login in Playwright | New auth flow | `accounts` + inline `loginAny` | Established across 9 specs; `admin`+`hc` keys present. [VERIFIED: accounts.ts] |
| Migration apply | Hand-run DDL | `ctx.Database.MigrateAsync()` | Runs the real migration chain + history table. [CITED: EF Core docs] |
| Disposable DB lifecycle | Manual create/drop in each test | xUnit `IClassFixture` + `IAsyncLifetime` | Standard xUnit fixture; one provision/teardown per class. [CITED: xunit.net/docs/shared-context] |

**Key insight:** Every primitive this phase needs already exists in the repo (seed method, sqlcmd wrapper, login helper, EF providers). The phase is composition, not construction.

## Common Pitfalls

### Pitfall 1: TEST-03 is JavaScript, not C# — silent scope error
**What goes wrong:** Planner writes an xUnit test for "pre-order DFS" assuming a C# method exists; finds none; either fakes one or asserts against `GetOrganizationTree` (which returns flat `Level/DisplayOrder/Name` order, NOT pre-order).
**Why it happens:** Phase 342 implemented the pre-order in the browser (`orgTree.js:flattenTreePreOrder`); the server only returns flat data.
**How to avoid:** Decide the approach up front (Q1). The flat→pre-order transform is `buildTree` + `flattenTreePreOrder`. Recommended: extract a small pure C# `BuildPreOrder(IEnumerable<flatNode>) -> List` helper (used by a test only, or refactored into the controller) OR assert dropdown `<option>` text order in Playwright. Do NOT assert pre-order against the existing flat endpoint.
**Warning signs:** A test named `PreOrderSort_*` that asserts on `GetOrganizationTree` output (that endpoint is flat-by-level, will pass for wrong reasons).

### Pitfall 2: TEST-05 expects seed from migration alone
**What goes wrong:** Test runs `Migrate()`, immediately counts `OrganizationLevelLabels`, expects 3, gets 0, "fails".
**Why it happens:** Seeding is NOT in the migration (`HasData`); it's in `SeedData.SeedOrganizationLevelLabelsAsync`.
**How to avoid:** Always call the seed method after `Migrate()` (Pattern 3). The contract is "migration creates table, seed populates" — assert in that order.
**Warning signs:** Count==0 after Migrate; fixture missing the seed call.

### Pitfall 3: Integration test pollutes or collides with dev DB
**What goes wrong:** Hardcoded `Database=HcPortalDB_Test` reused across runs leaves residue or collides with a running app/another test run.
**Why it happens:** Static DB name + no drop, or drop fails because a connection is open.
**How to avoid:** Unique `Guid` DB name per fixture instance + `EnsureDeletedAsync()` in `DisposeAsync`. If a drop ever blocks on open connections, the dev pattern (`SET SINGLE_USER WITH ROLLBACK IMMEDIATE`) from dbSnapshot.ts:80-99 is the fallback — but a fresh guid DB rarely has external connections.
**Warning signs:** "Cannot drop database currently in use"; leftover `HcPortalDB_Test_*` DBs in SSMS.

### Pitfall 4: Playwright org-label test mutates dev DB without cleanup
**What goes wrong:** Scenario "rename Bagian → Direktorat" persists to `HcPortalDB_Dev`; subsequent manual UAT / other specs see "Direktorat" instead of "Bagian".
**Why it happens:** OrgLabel CRUD writes to the real dev DB; Playwright runs against the live app.
**How to avoid:** The existing `global.teardown.ts` already does BACKUP (setup) → RESTORE (teardown) of `HcPortalDB_Dev`, which reverts ALL changes including label renames. The new spec inherits this automatically (it's globalTeardown, runs once after all specs). **Confirm** the rename scenario's label is restored by teardown's RESTORE — no per-test undo needed. As a belt-and-suspenders option, the rename scenario can rename back to "Bagian" at test end, but the global RESTORE is the authoritative cleanup. [VERIFIED: global.teardown.ts:64-76 restores HcPortalDB_Dev]
**Warning signs:** Manual UAT after a Playwright run shows renamed labels; SEED_JOURNAL left `active`.

### Pitfall 5: CMP integration label only renders for roleLevel ≤ 4
**What goes wrong:** TEST-06 scenario 5 logs in as a low-privilege user, navigates `/CMP/Records`, can't find the `@OrgLabels.GetLabel(0)` filter label, test fails.
**Why it happens:** [VERIFIED: CMPController.cs:502] the Team View tab (which holds the `OrgLabels` filter labels in `RecordsTeam.cshtml`) only renders for `roleLevel <= 4`. The labels at RecordsTeam.cshtml:20,50,135 live in that tab.
**How to avoid:** Use `admin` (Admin role, level 1) for the CMP integration assertion. The `accounts.admin` key is correct. Worker form `/Admin/CreateWorker` is `[Authorize(Roles="Admin, HC")]` (WorkerController.cs:14 Route + AdminBase auth) — also use `admin` or `hc`.
**Warning signs:** Label not found on `/CMP/Records` when logged in as coachee/coach.

### Pitfall 6: SQL-less CI run fails the integration test
**What goes wrong:** TEST-05 runs in an environment without SQLEXPRESS → connection refused → red build.
**Why it happens:** Real-DB test has a hard runtime dependency on SQL Server.
**How to avoid:** See Q3b — mark with `[Trait("Category","Integration")]` so `dotnet test --filter "Category!=Integration"` excludes it; document that local verification (DEV_WORKFLOW step 3, SQLEXPRESS present) is the gate. Optionally a `Skip`-via-Fact-fixture pattern. Do NOT make the whole suite depend on SQL Server.
**Warning signs:** CI red on connection timeout; `dotnet test` fails on a machine without SQLEXPRESS.

## Runtime State Inventory

> Phase 344 writes test code + one thin UAT doc. It does NOT rename/migrate production data. Most categories are N/A, but two test-runtime items matter:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `HcPortalDB_Dev` (SQLEXPRESS) is mutated transiently by Playwright OrgLabel rename + tree CRUD scenarios, then reverted by `global.teardown.ts` RESTORE. | None — existing teardown reverts. Confirm SEED_JOURNAL flips active→cleaned. |
| Live service config | Running app on `http://localhost:5277` must be up before Playwright (DEV_WORKFLOW step 3). | Planner: document `dotnet run` prerequisite in test run command. |
| OS-registered state | None — no scheduled tasks/services touched. | None — verified by scope (test-only). |
| Secrets/env vars | None — tests use Integrated Security (`-E`) + hardcoded local dev creds (pwd 123456, local-only). | None. |
| Build artifacts | New `Microsoft.EntityFrameworkCore.SqlServer` package added to Tests csproj → `obj/` restore needed; new `HcPortalDB_Test_<guid>` DBs auto-dropped per run. | `dotnet restore` after csproj edit; verify no leftover test DBs in SSMS after a run. |

## Code Examples

### TEST-03 Option A — pure C# pre-order helper (recommended for xUnit coverage)
```csharp
// A test-local mirror of orgTree.js buildTree + flattenTreePreOrder, kept deterministic.
// If extracted into OrganizationController as a private/internal static, even better (then test the real SUT).
// Source semantics: wwwroot/js/orgTree.js:62-75 (buildTree) + 308-317 (flattenTreePreOrder)
internal static List<(int Id, int Depth)> BuildPreOrder(
    IReadOnlyList<(int Id, int? ParentId, int DisplayOrder, string Name)> flat)
{
    var byParent = flat
        .OrderBy(n => n.DisplayOrder).ThenBy(n => n.Name)          // mirrors EF ordering
        .GroupBy(n => n.ParentId)
        .ToDictionary(g => g.Key, g => g.ToList());
    var outList = new List<(int, int)>();
    void Walk(int? parentId, int depth)
    {
        if (!byParent.TryGetValue(parentId, out var kids)) return;
        foreach (var k in kids) { outList.Add((k.Id, depth)); Walk(k.Id, depth + 1); }
    }
    Walk(null, 0);   // roots first, each followed by all descendants (pre-order)
    return outList;
}

[Fact]
public void PreOrder_ThreeLevelMultiRoot_OrdersParentThenDescendantsThenSibling()
{
    // 2 roots; root A has child A1 with grandchild A1a; root B has child B1.
    var flat = new (int, int?, int, string)[]
    {
        (1, null, 1, "A"), (2, 1, 1, "A1"), (3, 2, 1, "A1a"),
        (4, null, 2, "B"), (5, 4, 1, "B1"),
    };
    var order = BuildPreOrder(flat).Select(x => x.Id).ToList();
    Assert.Equal(new[] { 1, 2, 3, 4, 5 }, order);                  // A, A1, A1a, B, B1
}
```

### TEST-06 scenario 5 — integration label visible (CMP + Worker form)
```typescript
// Source: routes verified — /CMP/Records (CMPController.Records→View("Records")) renders RecordsTeam.cshtml
// for admin (roleLevel<=4); /Admin/CreateWorker (WorkerController) renders CreateWorker.cshtml selects.
test('label baru kelihatan di 2+ page integrasi (CMP + Worker form)', async ({ page }) => {
  await loginAny(page, 'admin');

  // Precondition: rename done earlier in the spec (Bagian -> Direktorat) via /Admin/ManageOrgLevelLabels.
  await page.goto('/CMP/Records');
  // Team View tab filter label — RecordsTeam.cshtml:20 renders @OrgLabels.GetLabel(0)
  await expect(page.getByText('Direktorat', { exact: false }).first()).toBeVisible();

  await page.goto('/Admin/CreateWorker');
  // CreateWorker.cshtml:116 "-- Pilih @OrgLabels.GetLabel(0) --"
  await expect(page.getByText('Direktorat', { exact: false }).first()).toBeVisible();
});
```

### Playwright TEST-06 scenario list → real routes (planner reference)
```
1. tree load + legend visible        → GET /Admin/ManageOrganization
                                         assert #org-legend has items; tree rows have .org-tier-badge
2. dropdown pre-order + inactive shown → GET /Admin/ManageOrganization, open Add/Edit modal
                                         assert #unitModalParent <option> order = pre-order;
                                         inactive option has " (nonaktif)" suffix + grey
3. cascade warning modal count accurate→ GET /Admin/ManageOrganization, edit a unit with refs
                                         assert #cascadeConfirmModal shows non-zero counts
                                         (D-04: count VALUE accuracy is the MANUAL item; modal-appears is automatable)
4. label CRUD live rename → tree updated→ GET /Admin/ManageOrgLevelLabels rename → GET /Admin/ManageOrganization
                                         assert .org-tier-badge text == new label
5. label in 2+ integration pages       → GET /CMP/Records  +  GET /Admin/CreateWorker (admin login)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `EnsureCreated()` for test schema | `Database.Migrate()` for migration validation | EF Core long-standing guidance | TEST-05 must use Migrate to validate real DDL. |
| Per-test DB create in `[Fact]` | `IClassFixture` + `IAsyncLifetime` | xUnit standard | One provision/teardown per class; async-safe drop. |
| InMemory for everything | InMemory for unit + real provider for integration | this phase (D-02) | Adds SqlServer provider to Tests project. |

**Deprecated/outdated:** None relevant. EF Core 8 + xUnit 2.9 + Playwright are all current in-repo versions.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Adding `Microsoft.EntityFrameworkCore.SqlServer` explicitly to Tests csproj is needed (vs relying on transitive flow from HcPortal.csproj). | Standard Stack / Supporting | LOW — if transitive flow already works, the explicit add is harmless/redundant. If it doesn't, the add is required. Safe either way. |
| A2 | A `[Trait("Category","Integration")]` filter is the right CI gate (vs conditional skip or `autonomous:false`). | Q3b | LOW — planner may prefer a runtime skip; both satisfy "don't break SQL-less CI." DEV_WORKFLOW guarantees local SQLEXPRESS so the test runs locally regardless. |
| A3 | The global RESTORE in teardown fully reverts the OrgLabel rename done by Playwright. | Pitfall 4 | LOW — verified the teardown restores HcPortalDB_Dev; assumes setup BACKUP captured pre-rename state (it runs before all specs). |

## Open Questions

1. **TEST-03 approach choice (DECISION NEEDED at planning):**
   - What we know: pre-order DFS is JS (`orgTree.js:flattenTreePreOrder`); no C# SUT exists.
   - What's unclear: whether the planner wants (A) a pure C# helper unit test [mirrors logic, fast, but tests a copy unless refactored into the controller], (B) refactor the controller to expose a server-side pre-order + unit-test the real SUT [highest fidelity, but is feature-adjacent code change in a test phase], or (C) Playwright DOM-order assertion on `#unitModalParent` options [tests the actually-shipped JS path, no C# change, but E2E-cost].
   - Recommendation: **Option A** for the xUnit requirement (TEST-03 literally says "Unit test xUnit"), supplemented by **Option C** as the Playwright scenario 2 (which already exists in the TEST-06 list). This satisfies the requirement verbatim AND validates the real path. Avoid Option B (scope creep in a test phase).

2. **Cascade count: automated vs manual (D-04 nuance):**
   - What we know: D-04 puts count-accuracy under MANUAL (visual). Playwright scenario 3 asserts the modal appears with non-zero counts.
   - What's unclear: whether Playwright should also assert the exact count number (it can, since `PreviewEditCascade` count==actual is already proven by `OrganizationControllerTests`).
   - Recommendation: Playwright asserts modal appears + counts are present/non-zero (structural). Exact-number visual confirmation stays manual per D-04. Don't over-automate the visual judgment the user explicitly reserved.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| SQL Server (SQLEXPRESS) | TEST-05 integration; Playwright dev DB | ✓ (per DEV_WORKFLOW + dbSnapshot.ts uses `localhost\SQLEXPRESS`) | local instance | `[Trait]` filter to skip in SQL-less CI |
| .NET 8 SDK / `dotnet test` | All xUnit | ✓ (repo builds on net8.0) | 8.0 | — |
| Running app on :5277 | TEST-06 Playwright | requires `dotnet run` before test | — | start app first (documented prerequisite) |
| Node + @playwright/test | TEST-06 | ✓ (tests/ has playwright.config + 9 specs) | in-repo | — |
| sqlcmd | Playwright BACKUP/RESTORE | ✓ (dbSnapshot.ts spawns it) | system | — |
| `Microsoft.EntityFrameworkCore.SqlServer` in Tests project | TEST-05 | ✗ in Tests csproj (✓ in HcPortal.csproj) | needs 8.0.0 | add package (A1) |

**Missing dependencies with no fallback:** None blocking. Running app on :5277 must be started manually before Playwright (standard DEV_WORKFLOW step 3).

**Missing dependencies with fallback:** SqlServer EF provider not explicitly in Tests csproj → add it (one-line `dotnet add package`).

## Validation Architecture

> nyquist_validation: treated as enabled (no `.planning/config.json` workflow override found disabling it). This section maps each requirement to its test type, case design, and adequacy bar so a VALIDATION.md can be derived.

### Test Framework
| Property | Value |
|----------|-------|
| Unit/Integration framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 [VERIFIED: HcPortal.Tests.csproj] |
| Unit DB | EF Core InMemory 8.0.0 (TEST-01..04, TEST-02-attr, TEST-03-helper) |
| Integration DB | EF Core SqlServer 8.0.0 on `localhost\SQLEXPRESS`, disposable `HcPortalDB_Test_<guid>` (TEST-05) |
| Config file (unit) | none — options built in test factory (`UseInMemoryDatabase(Guid)`) |
| Config file (integration) | none — connection string built in fixture (no appsettings change) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (fast, no SQL) |
| Full suite command | `dotnet test` (includes TEST-05; requires SQLEXPRESS) |
| E2E framework | @playwright/test, baseURL `http://localhost:5277` [VERIFIED: tests/playwright.config.ts] |
| E2E run command | (app running) `cd tests && npx playwright test e2e/manage-org-label.spec.ts` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TEST-01 | GetLabel happy + fallback | unit | `dotnet test --filter "FullyQualifiedName~OrgLabelServiceTests"` | ✅ exists (verify-only) |
| TEST-02a | Validation reject empty/dup/>50 | unit | `dotnet test --filter "FullyQualifiedName~OrgLabelControllerTests"` | ✅ exists (verify-only) |
| TEST-02b | Permission denial non-Admin/non-HC (attribute) | unit (reflection) | `dotnet test --filter "FullyQualifiedName~OrgLabelControllerTests"` | ❌ Wave 0 — add attr-presence [Fact] x5 actions |
| TEST-02c | Permission denial live 403/redirect | e2e | `npx playwright test e2e/manage-org-label.spec.ts -g "403"` | ❌ Wave 0 — add coach→/Admin/ManageOrgLevelLabels |
| TEST-03 | Pre-order DFS 3-level + multi-root | unit (helper) | `dotnet test --filter "FullyQualifiedName~PreOrder"` | ❌ Wave 0 — add BuildPreOrder helper + [Fact] |
| TEST-04 | Dup-name per-parent | unit | `dotnet test --filter "FullyQualifiedName~OrganizationControllerTests"` | ✅ exists (verify-only) |
| TEST-05 | Migration apply + seed + first read | integration | `dotnet test --filter "Category=Integration"` | ❌ Wave 0 — new OrgLabelMigrationIntegrationTests.cs + fixture |
| TEST-06.1 | Tree load + legend visible | e2e | `npx playwright test e2e/manage-org-label.spec.ts` | ❌ Wave 0 — new spec |
| TEST-06.2 | Dropdown pre-order + inactive shown | e2e | (same spec) | ❌ Wave 0 |
| TEST-06.3 | Cascade warning modal (counts present) | e2e | (same spec) | ❌ Wave 0 |
| TEST-06.4 | Label rename → tree badge updated | e2e | (same spec) | ❌ Wave 0 |
| TEST-06.5 | Label in CMP + Worker form | e2e | (same spec) | ❌ Wave 0 |
| ORG-INTEG-03 | No regression (CRUD/reorder/toggle/integrated pages) | e2e (4 auto) + manual (5 smoke) | spec + `344-HUMAN-UAT.md` | ❌ Wave 0 (doc) |

### Case Design / Adequacy Bar
- **TEST-02b (attribute):** assert `[Authorize(Roles="Admin, HC")]` on `ManageOrgLevelLabels`, `UpdateLevelLabel`, `AddLevelLabel`, `DeleteLevelLabel`; assert `GetLevelLabels` is `[Authorize]` WITHOUT Roles (locks ORG-LABEL-03 contract). Adequate = all 5 contracts asserted.
- **TEST-03:** ≥2 roots, ≥1 three-level chain (parent→child→grandchild), siblings with distinct DisplayOrder. Adequate = output ID sequence equals hand-computed pre-order (parent immediately followed by ALL its descendants before next sibling).
- **TEST-05:** assert (a) `Migrate()` throws nothing, (b) seed inserts exactly 3 rows, (c) `GetLabel(0/1/2)` returns configured labels (NOT `"Level N"`), (d) `GetLabel(99)` returns fallback. Adequate = all four on a freshly-migrated real DB, DB dropped after.
- **TEST-06:** 5 scenarios above; each asserts a concrete DOM signal (legend item count, option text order, modal visibility + count text, badge text, integration-page label text). Adequate = all 5 green on :5277 with global BACKUP/RESTORE bracketing.
- **Manual (D-04 #5 + smoke):** count-accuracy visual + 5 regression smoke. Adequate = checklist all ticked by user at :5277.

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (sub-second-ish, no SQL).
- **Per wave merge:** `dotnet test` (full, incl. integration) + `npx playwright test e2e/manage-org-label.spec.ts`.
- **Phase gate:** full `dotnet test` green + Playwright spec green on :5277 + `344-HUMAN-UAT.md` checklist signed off, before `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/HcPortal.Tests.csproj` — add `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0
- [ ] `HcPortal.Tests/OrgLabelControllerTests.cs` — add 5 reflection attribute [Fact] (TEST-02b)
- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` (or new `PreOrderSortTests.cs`) — add BuildPreOrder helper + [Fact] (TEST-03)
- [ ] `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` + fixture — TEST-05 (`[Trait("Category","Integration")]`)
- [ ] `tests/e2e/manage-org-label.spec.ts` — TEST-06 5 scenarios + TEST-02c 403 (reuse `accounts`/`loginAny`)
- [ ] `.planning/phases/344-test-uat/344-HUMAN-UAT.md` — thin manual doc: cascade count visual + 5 regression smoke (D-04)

### `344-HUMAN-UAT.md` content (D-04 — keep thin, non-automated only)
```
Execute at http://localhost:5277 (admin@pertamina.com / 123456). After: confirm DB labels restored or restore snapshot.

[ ] UAT-5  Edit a Bagian with many users → cascade warning modal appears AND the count numbers
           shown match the actual affected Users/Mappings/Kompetensi/Guidance (visual accuracy).
[ ] SMOKE-1 Tree drag-reorder a unit → order persists after reload (ReorderBatch).
[ ] SMOKE-2 Toggle a unit Active/Nonaktif → badge + dropdown suffix update correctly.
[ ] SMOKE-3 Delete a leaf unit → removed from tree, no orphan.
[ ] SMOKE-4 Add a unit under an existing parent → appears in correct pre-order position with dynamic title.
```

## Security Domain

> security_enforcement: treated as enabled (no explicit `false`). This is a test phase — the relevant security surface is the authorization contract being verified, not new attack surface.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no (test-only; reuses existing dev login) | Identity login (existing) |
| V4 Access Control | **yes** | `[Authorize(Roles="Admin, HC")]` on OrgLabel CRUD — TEST-02 verifies this contract (reflection + Playwright 403). |
| V5 Input Validation | yes (already covered) | Server-side label validation (empty/dup/>50) — existing 7 [Fact]. |
| V6 Cryptography | no | — |

### Known Threat Patterns for this stack
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Privilege escalation to OrgLabel CRUD by non-Admin/non-HC | Elevation of Privilege | `[Authorize(Roles="Admin, HC")]` — TEST-02 is the regression guard. |
| Test DB targeting a non-local server | Tampering | dbSnapshot.ts localhost-only guard (existing); TEST-05 connection string hardcoded to `localhost\SQLEXPRESS`. |
| Test mutation leaking into dev DB | Tampering | global BACKUP/RESTORE (Playwright) + disposable guid DB (integration). |

## Sources

### Primary (HIGH confidence — direct codebase read 2026-06-04)
- `wwwroot/js/orgTree.js` — pre-order DFS is JS (`flattenTreePreOrder` 308-317, `buildTree` 62-75, `populateParentDropdown` 319-337)
- `Controllers/OrgLabelController.cs` — auth `[Authorize]` class + `[Authorize(Roles="Admin, HC")]` actions (52,105,140,181); `GetLevelLabels` authenticated-only (42-48)
- `Controllers/OrganizationController.cs` — `GetOrganizationTree` flat order (61-67); dup-name per-parent (86,151); PreviewEditCascade (283)
- `HcPortal.Tests/OrgLabelServiceTests.cs` (13 [Fact]), `OrgLabelControllerTests.cs` (7 [Fact]), `OrganizationControllerTests.cs` (6 [Fact]) — existing coverage
- `Services/OrgLabelService.cs` — GetLabel fallback (31-35), cache (37-46)
- `Data/SeedData.cs:114-127` — `SeedOrganizationLevelLabelsAsync` (seed NOT in migration)
- `Migrations/20260603012335_AddOrganizationLevelLabel.cs` — CreateTable + unique index only, no seed
- `tests/playwright.config.ts` (baseURL :5277, globalTeardown), `tests/helpers/accounts.ts` (admin+hc), `tests/helpers/dbSnapshot.ts`, `tests/e2e/global.setup.ts`/`global.teardown.ts`, `tests/e2e/manage-assessment-filter.spec.ts` (login pattern)
- `HcPortal.csproj:18` (SqlServer 8.0.0 present), `HcPortal.Tests.csproj` (InMemory only), `appsettings.Development.json` (connection string)
- `Controllers/CMPController.cs:479-534` (`/CMP/Records`, Team View roleLevel<=4), `Controllers/WorkerController.cs:14,197` (`/Admin/CreateWorker`)
- `Views/CMP/RecordsTeam.cshtml:20,50,135` + `Views/Admin/CreateWorker.cshtml:116,122` — `@OrgLabels.GetLabel` integration points
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §7 (UAT scenarios), `.planning/milestones/v21.0-REQUIREMENTS.md`/`v21.0-ROADMAP.md`

### Secondary (MEDIUM-HIGH confidence — official docs)
- EF Core docs: `Database.Migrate()` vs `EnsureCreated()` (migrations applied vs schema-only) — learn.microsoft.com/ef-core/managing-schemas/migrations
- xUnit shared context: `IClassFixture` + `IAsyncLifetime` — xunit.net/docs/shared-context
- ASP.NET Core controller testing + auth attributes — learn.microsoft.com/aspnet/core/mvc/controllers/testing

### Tertiary (LOW confidence)
- None — no unverified web-only claims in this research.

## Metadata

**Confidence breakdown:**
- Existing coverage / gaps (TEST-01..06 status): HIGH — every test file and SUT read directly.
- TEST-03 location (JS not C#): HIGH — confirmed by grep + full file read of orgTree.js.
- TEST-05 EF pattern: HIGH (mechanics) / MEDIUM (A1 explicit-package-add necessity — safe either way).
- TEST-06 routes + login: HIGH — routes and login helper read directly.
- Auth enforcement (TEST-02): HIGH — attributes confirmed; "not unit-testable directly" is a verified property of how the existing tests instantiate controllers.

**Research date:** 2026-06-04
**Valid until:** 2026-07-04 (stable — internal repo, no fast-moving external deps; re-verify only if Phase 342/343 code changes the JS pre-order or controller auth).
