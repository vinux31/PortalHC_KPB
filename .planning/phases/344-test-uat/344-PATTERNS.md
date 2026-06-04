# Phase 344: Test + UAT - Pattern Map

**Mapped:** 2026-06-04
**Files analyzed:** 6 (4 new + 1 modify + 1 csproj edit; 1 thin UAT doc)
**Analogs found:** 6 / 6 (all exact or strong role-match)

> This is a test-gap-closure phase. RESEARCH.md already holds the deep mechanics (EF `Migrate()`, reflection auth, fixture lifecycle). PATTERNS.md is the tight **file → analog → concrete-excerpt** mapping the planner copies from. Every analog below was read directly 2026-06-04; line numbers are current.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `HcPortal.Tests/HcPortal.Tests.csproj` (MODIFY: add SqlServer pkg) | config | build | `HcPortal.csproj:18` (SqlServer 8.0.0 ref) | exact |
| `HcPortal.Tests/OrgLabelControllerTests.cs` (MODIFY: +5 reflection [Fact]) | test (unit) | request-response (attr assertion) | same file, existing 7 [Fact] | exact (in-file) |
| `Helpers/OrgTreePreOrder.cs` (NEW production helper `BuildPreOrder`) | utility | transform (tree DFS) | `Helpers/SheetNameSanitizer.cs` | exact (pure static helper) |
| `HcPortal.Tests/OrganizationControllerTests.cs` (MODIFY: + pre-order [Fact]) OR new `PreOrderSortTests.cs` | test (unit) | transform | `OrganizationControllerTests.cs` [Fact] structure | exact |
| `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` (NEW) | test (integration) | CRUD / file-I/O (real SQL DDL) | `OrgLabelServiceTests.cs` ctor/seed + RESEARCH Pattern 3 | role-match (no IClassFixture exists yet) |
| `tests/e2e/manage-org-label.spec.ts` (NEW, 5+1 scenarios) | test (e2e) | request-response (browser→SSR→DB) | `tests/e2e/manage-assessment-filter.spec.ts` | exact |
| `.planning/phases/344-test-uat/344-HUMAN-UAT.md` (NEW, thin) | doc | — | `.planning/phases/343-integrasi-app-wide/343-HUMAN-UAT.md` | exact |

---

## Pattern Assignments

### `HcPortal.Tests/HcPortal.Tests.csproj` (config — add provider for TEST-05)

**Analog:** `HcPortal.Tests.csproj:11-23` (existing `<ItemGroup>` PackageReference block). Add one line alongside the existing InMemory provider; match version 8.0.0 to the app (`HcPortal.csproj:18`).

**Add to the PackageReference ItemGroup (after line 12):**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```
**Command (idiomatic):** `dotnet add HcPortal.Tests/HcPortal.Tests.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0` then `dotnet restore`.

---

### `HcPortal.Tests/OrgLabelControllerTests.cs` (test, unit — TEST-02b reflection attr)

**Analog:** SAME file. The existing 7 [Fact] (validation rejects) are **verify-only — DO NOT touch** (D-01). Add 5 NEW reflection-attribute [Fact] in the same file, same `namespace HcPortal.Tests`, same `[Fact]` style. Reflection tests need NO controller instance (skip `MakeControllerWithCtx`).

**Existing import block to extend** (lines 6-14) — add `using Microsoft.AspNetCore.Authorization;`:
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;
// ADD:
using Microsoft.AspNetCore.Authorization;
```

**Verified auth contract to assert** (from `Controllers/OrgLabelController.cs`):
- Class-level `[Authorize]` (any authenticated) — line 16
- `ManageOrgLevelLabels` `[Authorize(Roles = "Admin, HC")]` — line 52
- `UpdateLevelLabel` — line 105
- `AddLevelLabel` — line 140
- `DeleteLevelLabel` — line 181
- `GetLevelLabels` — line 42-43 is `[HttpGet]` only (NO `Roles`) → assert it does NOT carry `Roles="Admin, HC"` (locks ORG-LABEL-03 contract).

**Pattern to copy (reflection [Fact]):**
```csharp
[Fact]
public void UpdateLevelLabel_RequiresAdminOrHcRole()
{
    var m = typeof(OrgLabelController).GetMethod(nameof(OrgLabelController.UpdateLevelLabel))!;
    var attr = m.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
    Assert.NotNull(attr);
    Assert.Equal("Admin, HC", attr!.Roles);   // exact string per OrgLabelController.cs:52,105,140,181
}
```
**Note:** `Assert.Equal("Admin, HC", ...)` — the role string includes the space exactly as declared. Repeat per protected action; one [Fact] asserting `GetLevelLabels` carries `[Authorize]` WITHOUT `Roles`.

---

### `Helpers/OrgTreePreOrder.cs` (utility — NEW production helper, D-05 / TEST-03)

**Analog:** `Helpers/SheetNameSanitizer.cs` — the canonical pure static helper in this repo. Mirror exactly: `namespace HcPortal.Helpers`, `public static class`, XML-doc summary, single public static method, private static walker. Place the new file in `Helpers/`.

**Convention to copy (file shape from SheetNameSanitizer.cs:1-4):**
```csharp
namespace HcPortal.Helpers
{
    public static class SheetNameSanitizer   // → public static class OrgTreePreOrder
    {
        // single public static method + private static helpers, XML-doc above public method
```

**Algorithm source = JS to port** (`wwwroot/js/orgTree.js`):
- `buildTree` (lines 62-75): groups by `parentId`, roots = `parentId === null`.
- `flattenTreePreOrder` (lines 308-317): `walk(node, depth)` pushes node then recurses children in order; roots iterated first.
- The flat data the JS receives is ordered by EF `Level, DisplayOrder, Name` (`GetOrganizationTree`, `OrganizationController.cs:61-67`) — replicate that sibling ordering (`.OrderBy(DisplayOrder).ThenBy(Name)`) so the C# helper matches the shipped DOM order.

**C# port (from RESEARCH Code Examples — keep pure + deterministic):**
```csharp
internal static List<(int Id, int Depth)> BuildPreOrder(
    IReadOnlyList<(int Id, int? ParentId, int DisplayOrder, string Name)> flat)
{
    var byParent = flat
        .OrderBy(n => n.DisplayOrder).ThenBy(n => n.Name)   // mirror EF + JS sibling order
        .GroupBy(n => n.ParentId)
        .ToDictionary(g => g.Key, g => g.ToList());
    var outList = new List<(int, int)>();
    void Walk(int? parentId, int depth)
    {
        if (!byParent.TryGetValue(parentId, out var kids)) return;
        foreach (var k in kids) { outList.Add((k.Id, depth)); Walk(k.Id, depth + 1); }
    }
    Walk(null, 0);   // roots first, each followed by ALL descendants before next sibling
    return outList;
}
```
**Scope guard (D-05):** helper stands alone; DO NOT refactor `GetOrganizationTree` to call it (rejected scope creep). Signature/visibility is Claude's discretion — make it testable (public or `internal` + `InternalsVisibleTo`, or public static).

---

### `HcPortal.Tests/OrganizationControllerTests.cs` (test, unit — TEST-03 pre-order [Fact])

**Analog:** SAME file's existing [Fact] arrange/act/assert shape. The existing 6 [Fact] (dup-name + PreviewEditCascade) are **verify-only — DO NOT touch** (D-01, TEST-04 already ✅). Add the pre-order [Fact] here (or a new `PreOrderSortTests.cs` — Claude's discretion). Since `BuildPreOrder` is a pure helper, this test needs NO `MakeController()` / DbContext — just call the helper directly.

**Pattern to copy ([Fact] shape, no DB needed):**
```csharp
[Fact]
public void PreOrder_ThreeLevelMultiRoot_OrdersParentThenDescendantsThenSibling()
{
    // 2 roots; root A has child A1 with grandchild A1a; root B has child B1.
    var flat = new (int, int?, int, string)[]
    {
        (1, null, 1, "A"), (2, 1, 1, "A1"), (3, 2, 1, "A1a"),
        (4, null, 2, "B"), (5, 4, 1, "B1"),
    };
    var order = OrgTreePreOrder.BuildPreOrder(flat).Select(x => x.Id).ToList();
    Assert.Equal(new[] { 1, 2, 3, 4, 5 }, order);   // A, A1, A1a, B, B1
}
```
**Adequacy bar (RESEARCH §Case Design):** ≥2 roots, ≥1 three-level chain, siblings with distinct DisplayOrder. **Pitfall 1:** do NOT assert pre-order against `GetOrganizationTree` output (that endpoint is flat-by-level — would pass for the wrong reason).

---

### `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` (test, integration — TEST-05)

**Analog (closest):** `OrgLabelServiceTests.cs:25-45` for the DbContext + seed + service construction shape (`new OrgLabelService(ctx, new MemoryCache(...), new AuditLogService(ctx))`). The ONLY differences vs the InMemory analog: (a) `UseSqlServer(cs)` instead of `UseInMemoryDatabase`, (b) `IClassFixture` + `IAsyncLifetime` wrapper (no existing fixture in the repo — this is the new shape, per RESEARCH Pattern 3), (c) seed via the **production** method, not inline.

**Construct service identically to the existing analog** (`OrgLabelServiceTests.cs:42-44`):
```csharp
var cache = new MemoryCache(new MemoryCacheOptions());
var auditLog = new AuditLogService(ctx);
var svc = new OrgLabelService(ctx, cache, auditLog);   // same triple-arg ctor
```

**Fixture + test shape (RESEARCH Pattern 3 — copy verbatim, adjust naming):**
```csharp
public class OrgLabelMigrationFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;" +
                 "TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.MigrateAsync();                    // validates REAL migration DDL (NOT EnsureCreated)
        await SeedData.SeedOrganizationLevelLabelsAsync(ctx); // production seed path (Data/SeedData.cs:114)
    }
    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();              // drop per run (D-02)
    }
}

[Trait("Category", "Integration")]   // lets `dotnet test --filter "Category!=Integration"` skip on SQL-less CI
public class OrgLabelMigrationIntegrationTests : IClassFixture<OrgLabelMigrationFixture>
{
    private readonly OrgLabelMigrationFixture _fx;
    public OrgLabelMigrationIntegrationTests(OrgLabelMigrationFixture fx) => _fx = fx;

    [Fact]
    public void Migrate_Seed_FirstRead_ReturnsConfiguredLabel_NotFallback()
    {
        using var ctx = new ApplicationDbContext(_fx.Options);
        var svc = new OrgLabelService(ctx, new MemoryCache(new MemoryCacheOptions()), new AuditLogService(ctx));
        Assert.Equal("Bagian",   svc.GetLabel(0));
        Assert.Equal("Unit",     svc.GetLabel(1));
        Assert.Equal("Sub-unit", svc.GetLabel(2));
        Assert.Equal("Level 99", svc.GetLabel(99));   // fallback still works
        Assert.Equal(3, ctx.OrganizationLevelLabels.Count());
    }
}
```
**Critical conventions (RESEARCH Pitfall 2 + 3 + Don't-Hand-Roll):**
- **Seed comes from `SeedData.SeedOrganizationLevelLabelsAsync(ctx)`, NOT the migration** (migration `20260603012335` has CreateTable+CreateIndex only, no `InsertData`). Assert rows AFTER seed, never expect `Migrate()` alone to populate.
- Use `MigrateAsync()` NOT `EnsureCreated()` (the whole point of TEST-05 is validating the migration DDL pipeline).
- Unique `Guid` DB name + `EnsureDeletedAsync()` — never touch `HcPortalDB_Dev` (D-02, no SEED_WORKFLOW snapshot needed).
- Imports needed: `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Caching.Memory`, `HcPortal.Data`, `HcPortal.Services`, `Xunit` (same as `OrgLabelServiceTests.cs:1-6`).

---

### `tests/e2e/manage-org-label.spec.ts` (test, e2e — TEST-06 5 scenarios + TEST-02c 403)

**Analog:** `tests/e2e/manage-assessment-filter.spec.ts` — representative admin-page spec. Copy: the header-comment convention, `import { accounts, AccountKey }`, the inline `loginAny` helper, `test.describe(...)` block, relative `page.goto` (baseURL `:5277` from `playwright.config.ts`), DOM-signal assertions.

**Import + login block to copy verbatim** (`manage-assessment-filter.spec.ts:25-41`):
```typescript
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}
```
**Account keys available** (`tests/helpers/accounts.ts`): `admin` (admin@pertamina.com), `hc`, `coach` (rustam.nugroho@pertamina.com) — all pwd `123456`. Use `admin` for all integration-page assertions (RESEARCH Pitfall 5: CMP Team View label only renders for roleLevel ≤ 4). Use `coach` for the TEST-02c 403 scenario.

**Scenario → route map (copy into the spec):**
```
1. tree load + legend visible        → GET /Admin/ManageOrganization
                                        assert #org-legend items; rows have .org-tier-badge
2. dropdown pre-order + inactive shown → /Admin/ManageOrganization → open Add/Edit modal
                                        assert #unitModalParent <option> order = pre-order;
                                        inactive option has " (nonaktif)" suffix + grey (orgTree.js:330,334)
3. cascade warning modal count present → /Admin/ManageOrganization → edit a referenced unit
                                        assert #cascadeConfirmModal visible + counts present/non-zero
                                        (exact-number visual accuracy is the MANUAL item per D-04)
4. label rename → tree badge updated   → /Admin/ManageOrgLevelLabels rename → /Admin/ManageOrganization
                                        assert .org-tier-badge text == new label
5. label in 2+ integration pages       → /CMP/Records (RecordsTeam.cshtml:20 @OrgLabels.GetLabel(0))
                                        + /Admin/CreateWorker (CreateWorker.cshtml:116) — login admin
TEST-02c: 403/redirect                 → loginAny(page,'coach') → GET /Admin/ManageOrgLevelLabels
                                        expect redirect away or 403 (live auth pipeline)
```
**Cleanup convention (NO new teardown code needed — Pitfall 4):** the existing `tests/e2e/global.teardown.ts` already does BACKUP (setup) → RESTORE (teardown) of `HcPortalDB_Dev`, which auto-reverts the scenario-4 label rename. The new spec inherits this globally. As belt-and-suspenders, scenario 4 MAY rename back to "Bagian" at test end, but the global RESTORE is authoritative.
**Run:** (app running on :5277) `cd tests && npx playwright test e2e/manage-org-label.spec.ts`.

---

### `.planning/phases/344-test-uat/344-HUMAN-UAT.md` (doc — thin manual UAT, D-04 / ORG-INTEG-03)

**Analog:** `.planning/phases/343-integrasi-app-wide/343-HUMAN-UAT.md`. Copy its YAML front-matter shape (`status`, `phase`, `source`, `started`, `updated`) + `## Tests` (numbered, each with `expected:` / `result:`) + `## Summary` (total/passed/issues/...) + `## Gaps`. Keep THIN — only the non-automated items (1 cascade-count visual + 4-5 regression smoke per D-04). User executes at `http://localhost:5277` (admin@pertamina.com / 123456), restores DB labels after.

**Content checklist to seed the doc (RESEARCH §`344-HUMAN-UAT.md`):**
```
[ ] UAT-5  Edit a Bagian with many users → cascade warning modal count numbers match actual
           affected Users/Mappings/Kompetensi/Guidance (visual accuracy judgment).
[ ] SMOKE-1 Tree drag-reorder a unit → order persists after reload (ReorderBatch).
[ ] SMOKE-2 Toggle a unit Active/Nonaktif → badge + dropdown suffix update correctly.
[ ] SMOKE-3 Delete a leaf unit → removed from tree, no orphan.
[ ] SMOKE-4 Add a unit under an existing parent → correct pre-order position + dynamic title.
```

---

## Shared Patterns

### InMemory unit-test factory (TEST-02b uses it only indirectly; TEST-03 helper test skips it)
**Source:** `OrgLabelControllerTests.cs:27-64` / `OrganizationControllerTests.cs:18-32` / `OrgLabelServiceTests.cs:25-45`
**Apply to:** any new unit [Fact] that needs a seeded context.
- `new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options`
- `#pragma warning disable CS8625` around `null!` substitutes for un-dereferenced ctor deps (`UserManager`, `IWebHostEnvironment`).
- For AJAX/Json responses: `httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest"` (`OrganizationControllerTests.cs:29`).
- Reflection JSON-prop assertion helpers `GetSuccess/GetMessage/GetInt/GetBool` already exist — reuse, don't redefine.
- **Pitfall 5 (casing):** keep test-data casing IDENTICAL — InMemory is case-sensitive, SQL Server default collation is not.

### Audit + cache + service triple
**Source:** `OrgLabelServiceTests.cs:42-44`
**Apply to:** integration test + any service-construction site. `OrgLabelService(ctx, MemoryCache, AuditLogService(ctx))` — same triple-arg ctor across InMemory and SqlServer providers (only the provider differs).

### Playwright login + DB safety
**Source:** `manage-assessment-filter.spec.ts:32-41` (login) + `global.setup.ts` / `global.teardown.ts` (BACKUP/RESTORE)
**Apply to:** the new e2e spec. Reuse `accounts` + inline `loginAny`; rely on global BACKUP/RESTORE for cleanup (do NOT write a new teardown). Use `dbSnapshot.ts` helpers only if a seed is needed (Claude's discretion).

### Test-category trait for SQL-dependent tests
**Source:** RESEARCH Pattern 3 / Pitfall 6 (new convention — not yet in repo)
**Apply to:** `OrgLabelMigrationIntegrationTests`. `[Trait("Category", "Integration")]` so `dotnet test --filter "Category!=Integration"` runs fast/SQL-less; full `dotnet test` runs it locally (DEV_WORKFLOW guarantees SQLEXPRESS).

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none — all 6 files have a concrete in-repo analog) | — | — | — |

**Closest-to-novel:** `OrgLabelMigrationIntegrationTests.cs` — no `IClassFixture`/`IAsyncLifetime` exists in `HcPortal.Tests` yet (all existing tests use per-test InMemory). Its DbContext+seed+service shape mirrors `OrgLabelServiceTests.cs`; the fixture wrapper + `UseSqlServer` + `MigrateAsync` come from RESEARCH Pattern 3 (CITED EF Core 8 + xUnit docs). Planner should treat RESEARCH Pattern 3 as the authoritative template for the fixture portion.

---

## Metadata

**Analog search scope:** `HcPortal.Tests/`, `Helpers/`, `tests/e2e/`, `tests/helpers/`, `Controllers/OrgLabelController.cs`, `wwwroot/js/orgTree.js`, `.planning/phases/*/`
**Files scanned (read in full or targeted):** OrgLabelControllerTests.cs, OrganizationControllerTests.cs, OrgLabelServiceTests.cs, HcPortal.Tests.csproj, manage-assessment-filter.spec.ts, global.setup.ts, global.teardown.ts, accounts.ts, orgTree.js (buildTree+flattenTreePreOrder), SheetNameSanitizer.cs, OrgLabelController.cs, 343-HUMAN-UAT.md
**Pattern extraction date:** 2026-06-04
