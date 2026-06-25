# Phase 402: Coaching Cross-Unit Mapping - Pattern Map

**Mapped:** 2026-06-19
**Files analyzed:** 6 (3 source modified + 2 test created + 1 e2e spec created)
**Analogs found:** 6 / 6 (all exact or strong role-match; this is a MODIFY-HEAVY phase — most "new code" = new methods/blocks inside existing files, so the closest analog is usually an adjacent block in the SAME file)

> **Phase shape:** NO new pages/components. 3 source files are *modified* (`CoachMappingController.cs`, `CDPController.cs`, `CoachCoacheeMapping.cshtml` + `_CoachingProtonPartial.cshtml`). The only genuinely *new* files are unit-test + e2e-spec files (Wave 0). Highest-value pattern mapping = the test files (see Pattern Assignments §A) because their analogs are already-green sibling tests in the same project.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `HcPortal.Tests/CrossUnitAssignTests.cs` *(NEW)* | test (xUnit, InMemory) | request-response (logic seam) | `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs` | exact (same helper, same seam) |
| `HcPortal.Tests/CdpCoachUnionScopeTests.cs` *(NEW, or fold)* | test (xUnit, InMemory) | CRUD / scope-query | `HcPortal.Tests/FilterAxisTests.cs` | exact (same dict-projection idiom) |
| `tests/e2e/coaching-crossunit-402.spec.ts` *(NEW)* | test (Playwright e2e) | event-driven (UI) | `tests/e2e/coachcoacheemapping-389.spec.ts` | exact (same modal, same login/skip idiom) |
| `Controllers/CoachMappingController.cs` *(MODIFY)* — assign guard + per-coachee loop + DTO | controller | request-response | adjacent blocks `:530-535` (loop), `:191-194` (batch dict), helper `:52-62` | exact (same file) |
| `Controllers/CDPController.cs` *(MODIFY)* — self-scope 3 sites | controller | CRUD / scope-query | the 3 sites themselves `:305/:326/:647` + base-scope `:465-480` | exact (same file) |
| `Views/Admin/CoachCoacheeMapping.cshtml` + `_CoachingProtonPartial.cshtml` *(MODIFY)* — modal markup + JS + unit dropdown | component (Razor + vanilla JS) | adjacent `:442` coachee-item, `:703-713` filter JS, `_CoachingProtonPartial.cshtml:30-47` | exact (same file/sibling) |

---

## Pattern Assignments

### §A — Unit tests (HIGHEST VALUE) — `HcPortal.Tests/CrossUnitAssignTests.cs` (test, request-response)

**Analog:** `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs` (the Phase-401 helper test, already GREEN — this is the direct sibling for CXU-02/CXU-03 logic).

This analog is gold: it already exercises the exact helper (`ValidateAssignmentUnitInUserUnits`) that CXU-03 reuses per-coachee, and demonstrates the per-coachee batch-reject primitive in `Assign_batch_rejects_when_one_coachee_lacks_unit` (lines 106-117).

**InMemory DbContext factory pattern** (`AssignmentUnitInUserUnitsTests.cs:19-22`) — copy verbatim:
```csharp
private static ApplicationDbContext InMemoryContext() =>
    new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
```
> Guid-per-test DB = isolation, no `IClassFixture`. Identical factory appears in `FilterAxisTests.cs:18-20`, `UnitInSectionValidationTests.cs:21-24`, `ProtonUnitResolveTests.cs:20-23`. This is THE project idiom for DbContext mocking — there is NO `UserManager`/`HttpContext` mock in this codebase (RESEARCH Pattern 1). Use a **static seam** for new logic.

**Seed helper + namespace/usings header** (`AssignmentUnitInUserUnitsTests.cs:7-28`):
```csharp
using System; using System.Threading.Tasks;
using HcPortal.Controllers; using HcPortal.Data; using HcPortal.Models;
using Microsoft.EntityFrameworkCore; using Xunit;
namespace HcPortal.Tests;

private static async Task SeedUnitAsync(ApplicationDbContext ctx, string coacheeId, string unit, bool isActive = true)
{
    ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = unit, IsPrimary = false, IsActive = isActive });
    await ctx.SaveChangesAsync();
}
```

**Arrange/Act/Assert idiom + per-coachee reject** (`AssignmentUnitInUserUnitsTests.cs:106-117`) — direct analog for CXU-03 "one bad unit rejects whole batch":
```csharp
[Fact]
public async Task Assign_batch_rejects_when_one_coachee_lacks_unit()
{
    await using var ctx = InMemoryContext();
    ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitA", IsPrimary = true, IsActive = true });
    ctx.UserUnits.Add(new UserUnit { UserId = "c2", Unit = "UnitB", IsPrimary = true, IsActive = true });
    await ctx.SaveChangesAsync();
    Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UnitA"));
    Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c2", "UnitA"));
}
```

**For CXU-02 (Section-match guard):** prefer extracting a **static seam** analogous to `WorkerController.ValidateUnitsInSection` (tested in `UnitInSectionValidationTests.cs:38-79`) — that test seeds `OrganizationUnits` (Bagian→Unit ParentId hierarchy) and asserts an error-list return. Mirror its seed shape if CXU-02 needs org-tree, OR seed `ApplicationUser` rows with `.Section` (see `FilterAxisTests.cs:27` for `ctx.Users.Add(new ApplicationUser { Id="c1", Unit="UnitX", Section="Bagian1" })`) and assert the compare returns reject when `coachee.Section != coach.Section`.

**Org-tree seed analog** (`UnitInSectionValidationTests.cs:27-36`) — use if CXU-02/CXU-03 needs `GetSectionUnitsDictAsync`/`GetUnitsForSectionAsync`:
```csharp
ctx.OrganizationUnits.AddRange(
    new OrganizationUnit { Id = 1, Name = "Bagian1", Level = 0, ParentId = null, IsActive = true },
    new OrganizationUnit { Id = 2, Name = "UnitA", Level = 1, ParentId = 1, IsActive = true },
    new OrganizationUnit { Id = 3, Name = "UnitB", Level = 1, ParentId = 1, IsActive = true },
    new OrganizationUnit { Id = 4, Name = "Bagian2", Level = 0, ParentId = null, IsActive = true },
    new OrganizationUnit { Id = 5, Name = "UnitZ", Level = 1, ParentId = 4, IsActive = true });
await ctx.SaveChangesAsync();
```

**File-header comment convention** (every test file): top-of-file comment naming the Phase/Plan/Task + requirement ID + strategy note (see `AssignmentUnitInUserUnitsTests.cs:1-6`, `FilterAxisTests.cs:1-5`, `UnitInSectionValidationTests.cs:1-6`). Match this: start `CrossUnitAssignTests.cs` with `// Phase 402 Plan 0X — CXU-02/CXU-03 ...`.

> **Pitfall 5 (RESEARCH):** Do NOT write a 402 InMemory test asserting the single-active unique-index invariant — InMemory ignores `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`. That's Phase 404 (SQL-real). 402 tests target LOGIC only (Section compare, unit-∈-UserUnits, map iteration). See the `[Fact(Skip = "...deferred to Phase 404 QA-01...")]` idiom in `ProtonUnitResolveTests.cs:70-74` for how to mark deferred integration assertions while keeping the suite green.

---

### §B — Unit tests — `HcPortal.Tests/CdpCoachUnionScopeTests.cs` (test, scope-query) *(may fold into §A)*

**Analog:** `HcPortal.Tests/FilterAxisTests.cs` (Phase-401 PSU-02 — the closest analog for CDP coachee-scope logic; it tests the exact `unitByCoachee` dict projection that CXU-05 reasons about).

**Scope-dict projection + union/narrow assertion** (`FilterAxisTests.cs:22-39`) — direct analog for "union when unit=null, narrow when unit set":
```csharp
[Fact]
public async Task FilterAxis_resolves_coachee_to_AssignmentUnit_not_primary()
{
    using var ctx = InMemoryContext();
    ctx.Users.Add(new ApplicationUser { Id = "c1", Unit = "UnitX", Section = "Bagian1" });
    ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitX", IsPrimary = true, IsActive = true });
    ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitY", IsPrimary = false, IsActive = true });
    ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = "c1", CoachId = "coach1",
        IsActive = true, AssignmentUnit = "UnitY", AssignmentSection = "Bagian1" });
    await ctx.SaveChangesAsync();

    var unitByCoachee = (await ctx.CoachCoacheeMappings.Where(m => m.IsActive)
        .Select(m => new { m.CoacheeId, m.AssignmentUnit }).ToListAsync())
        .ToDictionary(m => m.CoacheeId, m => m.AssignmentUnit!.Trim());

    Assert.Equal("UnitY", unitByCoachee["c1"]);
}
```
> For CXU-05, seed a coach with 2 active UserUnits + mappings to coachees in different AssignmentUnits, then assert: (a) `unit=null` → scoped set = union (both coachees); (b) `unit="UnitY"` → post-filter narrows to coachees with `AssignmentUnit=="UnitY"`; (c) foreign `unit` ∉ coach.UserUnits → coerced to null (union). **Do NOT touch CDP post-filter `:490-503`** in production code — it is already AssignmentUnit-aware from Phase 401 (RESEARCH Pitfall 3).

**CDP authz attribute test (optional parity)** analog: `CDPControllerAuthTests.cs:13-26` uses reflection to assert `[Authorize(Roles=...)]` on a CDP action. Reuse this pattern only if 402 changes an attribute (it does not — leave authz untouched; this is here as the role-scope reference).

---

### §C — E2E spec — `tests/e2e/coaching-crossunit-402.spec.ts` (test, UI)

**Analog:** `tests/e2e/coachcoacheemapping-389.spec.ts` (the existing baseline for the SAME modal `#assignModal` on `/Admin/CoachCoacheeMapping`).

**Login helper + describe/beforeEach** (`coachcoacheemapping-389.spec.ts:21-42`):
```typescript
import { test, expect, Page } from '@playwright/test';
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

test.describe('Phase 402 — Coaching Cross-Unit Mapping', () => {
  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CoachCoacheeMapping');
    await expect(page.locator('h2', { hasText: 'Coach-Coachee Mapping' })).toBeVisible();
  });
});
```

**Data-guard skip idiom** (`coachcoacheemapping-389.spec.ts:48`) — REQUIRED so the spec stays green when the local DB lacks a multi-unit fixture:
```typescript
test.skip(cardCount === 0, 'no coach group data — ... (RED pra-Plan 0X)');
```

**Run-instructions header convention** (`coachcoacheemapping-389.spec.ts:1-19`): top-of-file comment naming requirements (CXU-01/03/04/05), TEST-FIRST note, and run command. **For branch ITHandoff the app runs on `localhost:5270`, NOT 5277** — document `E2E_BASE_URL=http://localhost:5270 npx playwright test coaching-crossunit-402 --workers=1` (`--workers=1` WAJIB — NTLM/shared-memory SQL; RESEARCH §Environment + reference_local_e2e_sql_env_fix).

> **Lesson Phase 354 (UI-SPEC line 212, WAJIB):** dynamic Razor (JS-rendered per-row `<select>`, JS checklist filter) MUST be verified at runtime via Playwright/browser — grep+build is NOT sufficient. Multi-unit fixture = temporary/local-only; snapshot→restore DB per CLAUDE.md Seed Workflow.

---

### §D — `Controllers/CoachMappingController.cs` (controller, request-response) *(MODIFY)*

**Analog:** adjacent blocks in the SAME file (this is reshape, not greenfield).

**Per-coachee batch-loop analog** (`:530-535`, the loop to convert from single `req.AssignmentUnit` → per-coachee `map[cid]`):
```csharp
// CURRENT (single unit for all) — CXU-03 reshapes this loop to use map[cid]
foreach (var cid in req.CoacheeIds)
{
    if (!await ValidateAssignmentUnitInUserUnits(_context, cid, req.AssignmentUnit))
        return Json(new { success = false, message = $"Unit '{req.AssignmentUnit}' bukan anggota Unit aktif coachee terpilih — tambahkan unit ke coachee dulu." });
}
```

**Batch-dict load analog (Pitfall 1 — ApplicationUser has NO UserUnits nav)** (`:191-194`, the Phase-401 `unitsByCoachee` pattern — reuse VERBATIM to build coacheeId→active-units for exposing to client / per-coachee validation):
```csharp
var unitsByCoachee = (await _context.UserUnits
    .Where(uu => checkCoacheeIds.Contains(uu.UserId) && uu.IsActive)
    .Select(uu => new { uu.UserId, uu.Unit }).ToListAsync())
    .GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.Unit.Trim()).ToList());
```
> For the client dropdown default (D-02 primary-first), add `.IsPrimary` to the projection and `OrderByDescending(x => x.IsPrimary)` before `Select(x => x.Unit)` (RESEARCH Pattern 2). This is the ONLY way to get `coachee.UserUnits` — never `@coachee.UserUnits` in Razor.

**Helper to reuse per-coachee (do NOT reimplement)** (`:52-62`):
```csharp
public static async Task<bool> ValidateAssignmentUnitInUserUnits(
    ApplicationDbContext context, string coacheeId, string? assignmentUnit)
{
    if (string.IsNullOrWhiteSpace(assignmentUnit)) return false;
    var activeUnits = await context.UserUnits
        .Where(uu => uu.UserId == coacheeId && uu.IsActive)
        .Select(uu => uu.Unit).ToListAsync();
    return activeUnits.Any(u =>
        string.Equals(u.Trim(), assignmentUnit.Trim(), StringComparison.OrdinalIgnoreCase));
}
```

**Org-tree validation analog (for CXU-03 unit ∈ Bagian coach)** (`:525-528`) — reuse `GetSectionUnitsDictAsync` exactly:
```csharp
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
if (!sectionUnitsDict.TryGetValue(req.AssignmentSection!.Trim(), out var validUnits)
    || !validUnits.Contains(req.AssignmentUnit!.Trim()))
    return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });
```

**Early-return validation idiom** (`:516-523`) — all guards return `Json(new { success = false, message = "..." })`. The NEW CXU-02 Section guard must follow this exact shape, injected after `:523`/`:528`. Authz attributes (`[Authorize(Roles="Admin, HC")]` `:512` + `[ValidateAntiForgeryToken]` `:513`) — leave untouched (RESEARCH §Security V4/V13).

**DTO reshape analog** (`CoachAssignRequest:1863-1873`) — add a new property alongside existing (do NOT remove `AssignmentUnit` — back-compat fallback, RESEARCH Anti-Patterns):
```csharp
public string? AssignmentUnit { get; set; }            // keep (single fallback)
public Dictionary<string, string>? AssignmentUnits { get; set; }   // NEW: coacheeId→unit (D-05)
```
> System.Text.Json deserializes `Dictionary<string,string>` natively — no custom converter. The existing DTOs in this file (`CoachEditRequest:1875+`) confirm the plain-POCO `[FromBody]` convention.

---

### §E — `Controllers/CDPController.cs` (controller, scope-query) *(MODIFY)*

**Analog:** the 3 self-scope sites themselves + the base-scope they feed.

**The 3 sites to change (CXU-05)** — verified line numbers:
- `:305` `FilterCoachingProton`: `else if (UserRoles.IsCoachingRole(roleLevel)) { section = user.Section; unit = user.Unit; }`
- `:326` `ExportDashboardProgress`: identical line.
- `:647` `BuildProtonProgressSubModelAsync` lockedUnit: `else if (UserRoles.IsCoachingRole(roleLevel)) { lockedSection = user.Section; lockedUnit = user.Unit; }`

**Pattern: STOP forcing primary (RESEARCH Pattern 3)** — keep `section` locked, do NOT set `unit = user.Unit`; validate operator-supplied `unit ∈ coach.UserUnits` else null. The union query already exists at base-scope `:465-480` (no unit filter at base = union for free). Build coach.UserUnits with the SAME junction query as the helper (`_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive)` — Pitfall 1, never `user.Unit` scalar).

> **DO NOT TOUCH** post-filter `:490-503` (already AssignmentUnit-aware from Phase 401/PSU-02 — RESEARCH Pitfall 3; CONTEXT D-04 explicit). Diff including lines 488-545 = warning sign.

---

### §F — `Views/Admin/CoachCoacheeMapping.cshtml` + `_CoachingProtonPartial.cshtml` (component, Razor + JS) *(MODIFY)*

**Analog:** adjacent markup/JS in the SAME file.

**Per-row data-attr convention** (`:442`, the coachee-item to reshape — `data-unit` scalar → `data-units` JSON):
```html
<div class="form-check coachee-item" data-section="@coachee.Section" data-unit="@coachee.Unit">
```
> Reshape to `data-units='@Html.Raw(System.Text.Json.JsonSerializer.Serialize(...))'` (RESEARCH Pattern 2). Idiom for `Html.Raw(JsonSerializer.Serialize(...))` already used in this file at `CoachMappingController.cs:165` (`ViewBag.SectionUnitsJson`) — consistent.

**Per-row checklist filter JS analog** (`:703-713` `filterCoacheesBySection`) — the model for the NEW coach-first auto-scope filter (IC-1): toggle `item.style.display` by `data-section` (same mechanism, source = coach.dataset.section instead of manual dropdown):
```javascript
function filterCoacheesBySection() {
    var selectedSection = document.getElementById('assignSectionFilter').value;
    var items = document.querySelectorAll('#coacheeChecklist .coachee-item');
    items.forEach(function (item) {
        item.style.display = (!selectedSection || item.getAttribute('data-section') === selectedSection) ? '' : 'none';
    });
}
```

**Lock to REMOVE (CXU-04)** — `updateAssignmentDefaults` unit-lock `:729-738`, single-unit auto-fill `:751-763`, and `submitAssign` backstop `:777-784` (`selectedUnits.size > 1` alert). Replace with Bagian-level rule (RESEARCH Finding 6 / UI-SPEC IC-3).

**submitAssign payload + fetch idiom** (`:788-803`) — preserve the `appUrl()` + `RequestVerificationToken` header mechanism; only change `AssignmentUnit: assignmentUnit` (single) → `AssignmentUnits: { coacheeId: unit }` map (IC-4):
```javascript
var payload = { CoachId: coachId, CoacheeIds: coacheeIds, ProtonTrackId: ..., StartDate: ...,
                AssignmentSection: assignmentSection, AssignmentUnit: assignmentUnit /* → AssignmentUnits map */ };
fetch(appUrl('/Admin/CoachCoacheeMappingAssign'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json',
               'RequestVerificationToken': document.querySelector('[name=__RequestVerificationToken]').value },
    body: JSON.stringify(payload) })
```
> The `data.warning`/`confirm` branch (`:807-820`) is DEAD for assign (Phase 401 hard-block, RESEARCH Pitfall 2) — leave but do not build on it.

**Hint text to replace** (`:463-465`) with H-1 copy (UI-SPEC). Use `@OrgLabels.GetLabel(0)`/`GetLabel(1)` for Bagian/Unit labels — NEVER hardcode (UI-SPEC Copywriting; existing usage `:468/:478`).

**CDP unit dropdown enable (CXU-05 UI)** (`_CoachingProtonPartial.cshtml:30-47`) — the `unitLocked = Model.RoleLevel >= 5` flag (`:31`) forces disabled+primary; for multi-unit coach set enabled + `AvailableUnits = coach.UserUnits` + default option "Semua @OrgLabels.GetLabel(1)" (already at `:35`). The exact `ProtonProgressSubModel` prop to toggle = re-verify at plan (RESEARCH Open Question 1 / Assumption A1):
```cshtml
@{
    var unitLocked = Model.RoleLevel >= 5;            // CXU-05: relax for coaching-role multi-unit
    var selectedUnit = Model.LockedUnit ?? Model.FilterUnit;
}
<select id="protonFilterUnit" class="form-select form-select-sm" @(unitLocked ? "disabled" : "")>
    <option value="">Semua @OrgLabels.GetLabel(1)</option>
    @foreach (var u in Model.AvailableUnits) { ... }
</select>
```

---

## Shared Patterns

### Static testable seam (no UserManager/HttpContext mock)
**Source:** `Controllers/CoachMappingController.cs:52-62` (`ValidateAssignmentUnitInUserUnits`) + `WorkerController.ValidateUnitsInSection` (tested in `UnitInSectionValidationTests.cs`).
**Apply to:** any NEW non-trivial guard logic in 402 (CXU-02 Section compare). This codebase NEVER mocks UserManager/HttpContext — extract logic to `public static async Task<...>` accepting `ApplicationDbContext` + primitives, then unit-test via InMemory. RESEARCH Pattern 1.

### EF-InMemory DbContext factory (Guid-per-test)
**Source:** `AssignmentUnitInUserUnitsTests.cs:19-22` (identical in 4 sibling tests).
**Apply to:** all new 402 unit tests. `new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)`.

### Junction-set batch dict (Pitfall 1 — ApplicationUser has NO UserUnits nav)
**Source:** `CoachMappingController.cs:191-194` (`unitsByCoachee` GroupBy→ToDictionary).
**Apply to:** every place 402 needs a coachee's/coach's active units — client expose (CXU-03), per-coachee validation, CDP coach union (CXU-05). NEVER `@user.UserUnits`; ALWAYS `_context.UserUnits.Where(uu => uu.UserId == id && uu.IsActive)`.

### Early-return JSON guard (server is the security boundary)
**Source:** `CoachMappingController.cs:516-535` (`return Json(new { success = false, message = "..." })`).
**Apply to:** NEW CXU-02 cross-Bagian guard + per-coachee CXU-03 validation. Client filter is UX-only; server MUST enforce (RESEARCH Pitfall 4). Keep `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`.

### Org-tree unit validation
**Source:** `_context.GetSectionUnitsDictAsync()` (used `CoachMappingController.cs:162, 525`) + `GetUnitsForSectionAsync` (`UnitInSectionValidationTests.cs:44`).
**Apply to:** CXU-03 "unit ∈ children-of-Bagian-coach" check before the per-coachee `ValidateAssignmentUnitInUserUnits` membership check.

### Razor data-attr JSON serialization
**Source:** `CoachMappingController.cs:165` (`JsonSerializer.Serialize(sectionUnitsDict)` → ViewBag → consumed in JS as `sectionUnitsMap`).
**Apply to:** exposing `coacheeId→units` to client as `data-units` JSON (CXU-03, RESEARCH Pattern 2).

### Playwright login + data-guard skip + ITHandoff port
**Source:** `tests/e2e/coachcoacheemapping-389.spec.ts:21-48` (loginAny helper, `test.skip(noData, ...)`, `--workers=1`).
**Apply to:** new e2e spec. Override `E2E_BASE_URL=http://localhost:5270` (branch ITHandoff). Dynamic Razor MUST be runtime-verified (Lesson Phase 354).

---

## No Analog Found

None. Every file has a strong analog (all modifications sit beside existing patterns; both new test files have direct Phase-401 sibling tests; the e2e spec targets the same modal as an existing spec). This phase is 100% reshape/extend of established patterns — no novel role or data-flow is introduced.

---

## Metadata

**Analog search scope:** `Controllers/` (CoachMappingController.cs, CDPController.cs), `Views/Admin/` + `Views/CDP/Shared/`, `HcPortal.Tests/` (95 test files scanned), `tests/e2e/` (30 specs).
**Files scanned:** ~130 (Glob test+e2e + targeted Read of 6 analog files + 4 source-modify sites).
**Verification:** all cited line numbers Read against live code this session (helper :52-62, eligible loader :172-175, batch dict :191-194, assign endpoint :510-535, DTO :1863-1873, CDP :305/:326/:647, view :442/:463-465/:467-482/:703-713/:729-738/:751-763/:777-795, partial :30-47). RESEARCH line refs CONFIRMED accurate (no drift since research).
**Pattern extraction date:** 2026-06-19

## PATTERN MAPPING COMPLETE
