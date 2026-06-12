# Phase 366: Cascade Image File Cleanup - Pattern Map

**Mapped:** 2026-06-12
**Files analyzed:** 3 (1 new helper, 1 modified controller w/ 6 sites, 1 new/refactored test)
**Analogs found:** 3 / 3 (all exact in-repo analogs — no RESEARCH.md fallback needed)

> Single-file backend refactor. The new helper is a **pure extraction** of 3 byte-identical inline blocks
> that already exist in `AssessmentAdminController.cs`. There is nothing speculative here — every excerpt
> below is verbatim from the current codebase. The planner's job is: lift block → helper, swap 3 call-sites
> to call it (behavior identical, SC#1), install the same helper at 3 `Delete*` post-commit points (SC#2/#3).

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/ImageFileCleanup.cs` (NEW) | utility (static helper) | file-I/O + DB-read (ref-count) | `Helpers/FileUploadHelper.cs` | exact (static helper, same `webRootPath`/`ILogger` param style) |
| `Controllers/AssessmentAdminController.cs` (MODIFY) | controller | request-response + cascade-delete | self (3 existing inline blocks = extraction source) | exact (lift-and-replace own code) |
| `HcPortal.Tests/...` integration test (NEW) | test | file-I/O assert over real-SQL cascade | `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` + `OrgLabelMigrationIntegrationTests.cs` | exact (disposable real-SQL fixture, Phase 344 TEST-05) |
| `HcPortal.Tests/PackageImageDeleteTests.cs` (REFACTOR) | test | in-memory ref-count mirror | self (replace private `DeleteIfUnreferenced` mirror with prod helper) | exact |

---

## Pattern Assignments

### `Helpers/ImageFileCleanup.cs` (NEW — utility / static helper)

**Analog (style/shape):** `Helpers/FileUploadHelper.cs`
**Logic source (the body to lift):** the 3 identical inline blocks in `AssessmentAdminController.cs` at `:5764-5778` (DeletePackage), `:6729-6743` (EditQuestion POST — NOTE: CONTEXT.md said :6834, actual is :6729), `:6834-6848` (DeleteQuestion).

**(a) THE block to lift into the helper** — verbatim from `AssessmentAdminController.cs:5764-5778` (DeletePackage; all 3 sites are byte-identical except the log-label suffix):

```csharp
foreach (var relUrl in imagePathsToDelete.Distinct())
{
    bool stillUsedQ = await _context.PackageQuestions.AnyAsync(x => x.ImagePath == relUrl);
    bool stillUsedO = await _context.PackageOptions.AnyAsync(x => x.ImagePath == relUrl);
    if (stillUsedQ || stillUsedO) continue; // dipakai Post/lain → SKIP
    try
    {
        var physical = Path.Combine(_env.WebRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
    }
    catch (Exception fex)
    {
        _logger.LogWarning(fex, "File.Delete post-commit failed (DeletePackage image): {Path}", relUrl);
    }
}
```

The 3 sites differ ONLY in the log-label inside the warn message:
- `:5776` → `"File.Delete post-commit failed (DeletePackage image): {Path}"`
- `:6741` → `"File.Delete post-commit failed (question image): {Path}"`
- `:6846` → `"File.Delete post-commit failed (DeleteQuestion image): {Path}"`

Per CONTEXT D-03 discretion, the helper takes an optional source-label so each call-site keeps its own log context.

**(b) static signature / DI-param style** — copy from `Helpers/FileUploadHelper.cs:75-79` (`SaveFileAsync` shows the established `webRootPath` + `ILogger? logger = null` param convention; helpers are `public static`, namespace `HcPortal.Helpers`, file-scoped imports of `Microsoft.Extensions.Logging` + `HcPortal.Models`):

```csharp
// FileUploadHelper.cs:75-79 — param-order convention to mirror
public static async Task<string?> SaveFileAsync(
    IFormFile? file,
    string webRootPath,
    string subFolder,
    ILogger? logger = null)
```

Note `DeleteFile` at `FileUploadHelper.cs:114-122` already does a null-safe physical delete, but it does **not** ref-count — so it is NOT reusable here. The new helper must keep the `AnyAsync` ref-count guard (that is the whole point: shared Pre/Post path must survive, SC#3).

**Proposed helper shape** (D-01 locks `static` + in `Helpers/`; exact name/params = planner discretion):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HcPortal.Data;

namespace HcPortal.Helpers
{
    public static class ImageFileCleanup
    {
        /// <summary>
        /// Ref-count + physical delete. For each path, if NO PackageQuestion/PackageOption row still
        /// references it (post-commit AnyAsync = false → batch-only path), File.Delete it; otherwise SKIP
        /// (shared Pre/Post path survives, SC#3). Warn-only per file (Phase 333). Call AFTER tx.CommitAsync.
        /// </summary>
        public static async Task DeleteUnreferencedAsync(
            ApplicationDbContext ctx,
            string webRootPath,
            ILogger logger,
            IEnumerable<string> paths,
            string source = "")
        {
            foreach (var relUrl in paths.Distinct())
            {
                if (string.IsNullOrEmpty(relUrl)) continue;
                bool stillUsedQ = await ctx.PackageQuestions.AnyAsync(x => x.ImagePath == relUrl);
                bool stillUsedO = await ctx.PackageOptions.AnyAsync(x => x.ImagePath == relUrl);
                if (stillUsedQ || stillUsedO) continue;
                try
                {
                    var physical = Path.Combine(webRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                }
                catch (Exception fex)
                {
                    logger.LogWarning(fex, "File.Delete post-commit failed ({Source}): {Path}", source, relUrl);
                }
            }
        }
    }
}
```

> `ApplicationDbContext` is the DbContext type (ctor at `AssessmentAdminController.cs:34`). `_context`/`_env` are
> inherited from `AdminBaseController` (controller does NOT declare them locally — only `_logger` at `:25`).

---

### `Controllers/AssessmentAdminController.cs` — 3 call-site swaps (MODIFY — behavior identical, SC#1)

**Analog:** the blocks themselves (extraction). Each existing `foreach (...) { ... }` block is replaced by ONE await call. **Ordering constraint (D-02):** the call must stay in the SAME position — AFTER `SyncPackagesToPost` (auto-sync Pre→Post) and after `SaveChangesAsync`. Do NOT move it earlier or the shared-path ref-count breaks.

| Site | Method | Replace lines | `imagePathsToDelete` declared at | source label |
|------|--------|---------------|-----------------------------------|--------------|
| 1 | `DeletePackage` | `:5764-5778` | (collected pre-RemoveRange in DeletePackage) | `"DeletePackage image"` |
| 2 | `EditQuestion` POST | `:6729-6743` | `:6605` (built through `ApplyOptionImageIntent`, `:6658`) | `"question image"` |
| 3 | `DeleteQuestion` | `:6834-6848` | `:6781-6784` (collect q.ImagePath + each option) | `"DeleteQuestion image"` |

Replacement (each site collapses its 15-line loop to):

```csharp
await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, _logger, imagePathsToDelete, "DeletePackage image");
```

**Path-collect pattern these sites already use** (DeleteQuestion `:6781-6784` — the canonical "collect before RemoveRange"):

```csharp
var imagePathsToDelete = new List<string>();
if (!string.IsNullOrEmpty(q.ImagePath)) imagePathsToDelete.Add(q.ImagePath);
foreach (var o in q.Options)
    if (!string.IsNullOrEmpty(o.ImagePath)) imagePathsToDelete.Add(o.ImagePath);
```

---

### `Controllers/AssessmentAdminController.cs` — 3 `Delete*` installs (MODIFY — the actual phase goal)

These 3 methods currently `RemoveRange` Questions/Options but leak the physical files. They already follow the **Phase 333 atomic pattern** (collect-snapshot, `BeginTransactionAsync`, `RemoveRange`, `SaveChangesAsync`, `CommitAsync`). The install = (1) collect `ImagePath` via `Distinct()` from the already-`Include`-loaded packages BEFORE `RemoveRange`, (2) call the helper AFTER `CommitAsync` (D-05/D-06: post-commit `AnyAsync` auto-handles batch-awareness + shared survival).

**All 3 already `Include(p => p.Questions).ThenInclude(q => q.Options)`** — so ImagePath is in-memory, no extra query needed:
- `DeleteAssessment:2314-2317` → `var packages = ... .Include(p => p.Questions).ThenInclude(q => q.Options) ...`
- `DeleteAssessmentGroup:2497-2500` → `var allPackages = ... .Include(p => p.Questions).ThenInclude(q => q.Options) ...`
- `DeletePrePostGroup:2677-2680` → `var allPackages = ... .Include(p => p.Questions).ThenInclude(q => q.Options) ...`

So **no new `Include` is required** (CONTEXT.md flagged this as "check" — confirmed: all three already nest-Include).

#### `DeleteAssessment` (controller, cascade-delete) — anchors

| Anchor | Line | Action |
|--------|------|--------|
| packages Include | `:2314-2317` | collect paths from `packages` here (after load) |
| RemoveRange loop | `:2320-2326` | collect must be BEFORE this |
| `SaveChangesAsync` / `CommitAsync` | `:2333-2334` | call helper AFTER this |

The RemoveRange loop to insert collection before (`:2318-2328`):

```csharp
if (packages.Any())
{
    foreach (var pkg in packages)
    {
        foreach (var q in pkg.Questions)
            _context.PackageOptions.RemoveRange(q.Options);
        _context.PackageQuestions.RemoveRange(pkg.Questions);
    }
    _context.AssessmentPackages.RemoveRange(packages);
    logger.LogInformation($"Deleting {packages.Count} packages with their questions/options");
}
```

Collect (insert immediately after `:2317`, before the `if (packages.Any())` block):

```csharp
var imagePaths = packages
    .SelectMany(p => p.Questions)
    .SelectMany(q => new[] { q.ImagePath }.Concat(q.Options.Select(o => o.ImagePath)))
    .Where(p => !string.IsNullOrEmpty(p))
    .Select(p => p!)
    .Distinct()
    .ToList();
```

Then AFTER `:2334` (`await tx.CommitAsync();`), before the audit-log try-block at `:2337`:

```csharp
await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, logger, imagePaths, "DeleteAssessment image");
```

> **Atomicity (Phase 333) — why post-commit:** the file delete runs AFTER `CommitAsync` so a file-system
> failure never rolls back the DB. The helper's internal try/catch is warn-only (matches the existing inline
> blocks). `logger` here is the local `HttpContext.RequestServices.GetRequiredService<ILogger<...>>()`
> (`:2191`), not `_logger` — pass `logger`.

#### `DeleteAssessmentGroup` (controller, cascade-delete) — anchors

| Anchor | Line | Action |
|--------|------|--------|
| `allPackages` Include | `:2497-2500` | collect from `allPackages` (multi-sibling batch) |
| RemoveRange loop | `:2503-2510` | collect before |
| `SaveChangesAsync` / `CommitAsync` | `:2518-2519` | call helper after |

Same `SelectMany` collect over `allPackages`; helper call after `:2519`, label `"DeleteAssessmentGroup image"`.

#### `DeletePrePostGroup` (controller, cascade-delete) — anchors

| Anchor | Line | Action |
|--------|------|--------|
| `allPackages` Include | `:2677-2680` | collect from `allPackages` (Pre + Post = 2 packages, 1 batch) |
| RemoveRange loop | `:2683-2690` | collect before |
| `SaveChangesAsync` / `CommitAsync` | `:2695-2696` | call helper after |

Same `SelectMany` collect over `allPackages`; helper call after `:2696`, label `"DeletePrePostGroup image"`.

> **D-05 / SC#3 (the expensive regression):** because `DeletePrePostGroup` removes Pre AND Post in ONE batch
> and commits before the helper runs, a path shared by Pre+Post is no longer referenced by ANY row →
> `AnyAsync` is false → file is correctly deleted. But for `DeletePackage`/`DeleteQuestion` (single side),
> the surviving Post row still references the shared path → `AnyAsync` true → file survives. **No
> exclusion-set needed** — the post-commit DB read IS the source of truth.

---

## Test Patterns

### NEW integration test (real-SQL, Phase 344 TEST-05 disposable)

**Primary analog:** `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` (seed entities → act → assert against real SQL).
**Fixture analog:** `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66` (the `IAsyncLifetime` disposable-DB fixture).

**(c) disposable real-SQL fixture harness** — copy from `OrgLabelMigrationIntegrationTests.cs:24-66`:

```csharp
public class OrgLabelMigrationFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public OrgLabelMigrationFixture()
    {
        // localhost-only + Integrated Security; TrustServerCertificate=True (SQLEXPRESS self-signed cert).
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();   // run the migrations pipeline (NOT schema-from-model)
            // ... production seed if needed ...
        }
        catch (Exception ex)
        {
            // M1: a mid-migration throw must NOT leave HcPortalDB_Test_<guid> behind.
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException($"setup failed... Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();   // drop disposable DB on success
    }
}
```

**Class-level wiring + per-test seed→act→assert** — copy the shape from `ProtonYearGateIntegrationTests.cs:16-63`:

```csharp
[Trait("Category", "Integration")]   // lets SQL-less CI skip: dotnet test --filter "Category!=Integration"
public class ImageCleanupIntegrationTests : IClassFixture<TFixture>
{
    private readonly TFixture _fixture;
    public ImageCleanupIntegrationTests(TFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task SharedPrePostPath_Survives_WhenOneSideDeleted()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        // 1. seed: a Pre session + Post session whose Question.ImagePath point to the SAME relUrl,
        //          and write a real temp file at webRoot + relUrl.
        // 2. act:  call ImageFileCleanup.DeleteUnreferencedAsync after removing ONLY the Pre rows
        //          (Post still references the path).
        // 3. assert: File.Exists(physical) == true (shared survives, SC#3).
    }

    [Fact]
    public async Task OrphanPath_Deleted_WhenFullCascade()
    {
        // seed session w/ image → remove all rows referencing path → helper → Assert.False(File.Exists).
    }
}
```

> **Two MANDATORY cases (D-03 + `<specifics>`):** (a) file physically deleted on full cascade-delete of an
> imaged session; (b) **shared Pre/Post path SURVIVES** when only one side is deleted (SYN-01 — the most
> expensive regression). Plus manual UAT @5277.
>
> **`webRootPath` in tests:** the prod sites use `_env.WebRootPath`; in the integration test pass a temp
> dir (`Path.Combine(Path.GetTempPath(), ...)`) as `webRootPath` and write real bytes there — mirror the
> `MakeTempDir()` + `File.WriteAllBytes` pattern from `PackageImageDeleteTests.cs:20-25,48`.

### REFACTOR `PackageImageDeleteTests.cs` (D-04 — kill the divergent mirror)

The private mirror `DeleteIfUnreferenced` at `:34-39` + `PathStillReferenced` at `:30-31` duplicate the helper logic (in-memory `Any()` instead of `AnyAsync()`):

```csharp
// PackageImageDeleteTests.cs:34-39 — the FAKE mirror to replace with the real prod helper (D-01/D-04)
private static void DeleteIfUnreferenced(string path, IEnumerable<PackageQuestion> remainingQ, IEnumerable<PackageOption> remainingO)
{
    if (PathStillReferenced(remainingQ, remainingO, path)) return;
    try { if (File.Exists(path)) File.Delete(path); }
    catch { /* warn-only per file (pola 333) */ }
}
```

**D-04 nuance:** the prod helper is `async` + needs an `ApplicationDbContext` (queries `PackageQuestions`/`PackageOptions`),
while these `[Fact]`s are pure in-memory (no DbContext). Planner discretion (CONTEXT D-04): either
(a) re-point the in-memory `[Fact]`s' assertions to drive the real helper via the new integration fixture, or
(b) keep the pure in-memory `[Fact]`s as fast logic-contract tests AND add the real-SQL integration test so
there is one production source of truth exercised end-to-end. Avoid leaving TWO independent ref-count
implementations that can drift. The unchanged `ApplyIntent`/`PathStillReferenced` helpers at `:30-31,132-151`
test EditQuestion replace-intent (out of scope for 366) — leave them.

---

## Shared Patterns

### Atomic delete (Phase 333) — collect-before-tx, delete-after-commit, warn-only
**Source:** the 3 inline blocks (`AssessmentAdminController.cs:5764`, `:6729`, `:6834`) + tx structure in `DeleteAssessment:2235/2333/2334`.
**Apply to:** all 3 `Delete*` installs. Sequence is locked: collect `Distinct()` paths → `RemoveRange` → `SaveChangesAsync` → `CommitAsync` → helper (post-commit). File errors are warn-only and never roll back the DB.

### Physical path resolution
**Source:** `AssessmentAdminController.cs:5771` (and `FileUploadHelper.cs:118` for the simpler variant).
**Apply to:** the helper.
```csharp
var physical = Path.Combine(webRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
```

### Ref-count predicate (the SC#2/#3 guard)
**Source:** `AssessmentAdminController.cs:5766-5768`.
**Apply to:** the helper (keep verbatim — this is what makes shared Pre/Post survive).
```csharp
bool stillUsedQ = await ctx.PackageQuestions.AnyAsync(x => x.ImagePath == relUrl);
bool stillUsedO = await ctx.PackageOptions.AnyAsync(x => x.ImagePath == relUrl);
if (stillUsedQ || stillUsedO) continue;
```

### Disposable real-SQL fixture (Phase 344 TEST-05)
**Source:** `OrgLabelMigrationIntegrationTests.cs:24-66`.
**Apply to:** the new integration test. `HcPortalDB_Test_<guid>` on `localhost\SQLEXPRESS`, dropped on success (`DisposeAsync`) AND mid-setup failure (`InitializeAsync` catch). Never touches `HcPortalDB_Dev` → no SEED_WORKFLOW snapshot needed. Tag `[Trait("Category","Integration")]`.

---

## No Analog Found

None. Every file maps to an exact in-repo analog. No RESEARCH.md patterns required.

---

## Metadata

**Analog search scope:** `Helpers/`, `Controllers/AssessmentAdminController.cs`, `HcPortal.Tests/`
**Files scanned:** `FileUploadHelper.cs`, `AssessmentAdminController.cs` (sites :2189, :2377, :2563, :5764, :6605-6743, :6770-6851), `PackageImageDeleteTests.cs`, `OrgLabelMigrationIntegrationTests.cs`, `ProtonYearGateIntegrationTests.cs`
**Key line-ref correction vs CONTEXT.md:** EditQuestion POST loop is at **:6729-6743** (CONTEXT.md listed :6834 for both EditQuestion and DeleteQuestion; :6834 is DeleteQuestion only).
**Pattern extraction date:** 2026-06-12

> **File-overlap warning (carried from CONTEXT):** these same 3 `Delete*` methods are reworked in Phase 367.
> 366 must ship FIRST; Plan 367 must preserve the image-cleanup helper calls (annotated in ROADMAP).
