# Phase 380: Admin/Engine Integrity - Pattern Map

**Mapped:** 2026-06-14
**Files analyzed:** 3 modified source files + 5 test files (2 modify/add, 3 new)
**Analogs found:** 8 / 8 (all in-repo, all verified line-by-line this session)

> **Bug-fix phase — NO new source files.** Every fix is "make the broken path match its
> working sibling in the SAME file." All analogs are concrete, in-tree, and tested.
> Read-only analysis; executor copies the excerpts below verbatim where noted.

---

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Helpers/ShuffleEngine.cs` (ON-path `BuildCrossPackageAssignment`) | engine (pure) | transform | OFF-path filter in **same file** `:53-57` | exact (same file, same concern) |
| `Controllers/CMPController.cs` (`VerifyToken` compare `:876`) | controller | request-response (access gate) | `CreateAssessment` uppercase write `AssessmentAdminController.cs:1104-1108` | role-match (normalization invariant) |
| `Controllers/CMPController.cs` (`StartExam` D-05 all-empty guard) | controller | request-response | existing no-package `else` guard in **same method** `:1198-1203` | exact (same method, adjacent gap) |
| `Controllers/AssessmentAdminController.cs` (`AddExtraTime` authz `:6866`) | controller | request-response | sibling `ResetAssessment` `:3996-4000` | exact (sibling action, same file) |
| `Controllers/AssessmentAdminController.cs` (`AddExtraTime` cap `:6897-6899`) | controller | request-response | per-call bound `:6870-6871` + accumulator `:6899` (same method) | exact (same method) |
| `Controllers/AssessmentAdminController.cs` (EditAssessment Pre/Post token writes `:1812/1916/1937`) | controller | CRUD (write normalization) | `CreateAssessment` uppercase `:1104-1108` (same file) | exact (same file, same pattern) |
| `HcPortal.Tests/ShuffleEngineTests.cs` (add 2 ON-path facts) | test (unit, pure) | transform | existing `Off_AllPackagesEmpty_*` + `On_MultiPackage_SeedStable_*` facts (same file) | exact |
| `HcPortal.Tests/AddExtraTimeAuthTests.cs` (NEW reflection-authz) | test (unit, pure) | reflection | `CDPControllerAuthTests.cs` (whole file) | exact |
| Integration tests (cap + token write + token compare) | test (integration, DB) | CRUD | `ProtonYearGateIntegrationTests.cs` (`IClassFixture` + `[Trait]`) | role-match |
| `tests/e2e/exam-taking.spec.ts` (add #5 token, #6 empty-pkg) | test (e2e) | request-response | existing Flow B + `examTypes.ts` helpers | role-match |

---

## Pattern Assignments

### 1. `Helpers/ShuffleEngine.cs` — `BuildCrossPackageAssignment` ON-path (engine, transform) — D-04/D-05

**Analog:** the OFF-path empty-filter in the **same file** (`:53-57`). This is the canonical template the audit, CONTEXT, and RESEARCH all point at — copy it verbatim into the ON-path.

**EXACT OFF-path template to mirror (`ShuffleEngine.cs:52-59`, VERIFIED current source):**
```csharp
// OFF + ≥2 paket (SHUF-06)
var packagesWithQuestions = packages
    .Where(p => p.Questions != null && p.Questions.Count > 0)              // D-02b: guard SEBELUM modulo
    .OrderBy(p => p.PackageNumber)                                         // anchor stabil
    .ToList();
if (packagesWithQuestions.Count == 0) return new List<int>();              // guard DivideByZero (V5)
```

**BUG site — ON-path K compute (`ShuffleEngine.cs:91-110`, the broken path):**
```csharp
private static List<int> BuildCrossPackageAssignment(List<AssessmentPackage> packages, Random rng)
{
    if (packages.Count == 0)
        return new List<int>();                                           // ← D-04: insert filter RIGHT AFTER this guard

    // Single package: shuffle question order so each worker sees a unique sequence
    if (packages.Count == 1)                                              // ← Pitfall 2: this must run on the FILTERED list
    {
        var singlePackageQuestions = packages[0].Questions;
        if (singlePackageQuestions == null || !singlePackageQuestions.Any())
            return new List<int>();
        var singlePackageIds = singlePackageQuestions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
        Shuffle(singlePackageIds, rng);
        return singlePackageIds;
    }

    // Safety fallback: use minimum question count across packages (edge case per user decision)
    int K = packages.Min(p => p.Questions.Count);                        // ← BUG: empty pkg ⇒ K=0
    if (K == 0)
        return new List<int>();                                           // ← everyone gets 0 questions
    ...
```

**Fix placement (Pitfall 2 — CRITICAL):** Insert the OFF-path filter at the **TOP** of `BuildCrossPackageAssignment` — right after the `packages.Count == 0` guard at `:94`, BEFORE the `packages.Count == 1` early-return at `:97`. Replace subsequent references to `packages` (the `Count==1` check, `K = ...Min`, `SelectMany`) with the filtered list. This makes "2 packages, one empty" collapse into the single-package shuffle branch (worker gets the full filled package's questions). Filter the variable; do NOT change the method signature.

**Why same-file analog is exact:** ON and OFF are two branches of one pure engine; the OFF branch already has the guard, the ON branch drifted without it (State of the Art: v27.0 Phase 372/374 added OFF guard, ON missed). Engine fix = single point heals all 3 callers (verified below).

**3 callers heal automatically (all go through `BuildQuestionAssignment` → `BuildCrossPackageAssignment` when ON):**
- `Controllers/CMPController.cs:1019-1020` (StartExam)
- `Controllers/AssessmentAdminController.cs:5210` (ReshufflePackage)
- `Controllers/AssessmentAdminController.cs:5308` (ReshuffleAll)

---

### 2. `Controllers/CMPController.cs` — `VerifyToken` compare (controller, access gate) — D-01a

**Analog:** the uppercase invariant established at `CreateAssessment` (`AssessmentAdminController.cs:1104-1108`) — both sides must be uppercased to match.

**EXACT current line to convert (`CMPController.cs:876`, single-side compare — the BUG):**
```csharp
if (string.IsNullOrEmpty(token) || assessment.AccessToken != token.ToUpper())
{
    return Json(new { success = false, message = "Token tidak valid. Silakan periksa dan coba lagi." });
}
```

**Fix (both-sides defensive, per D-01a / RESEARCH Pattern 2):**
```csharp
if (string.IsNullOrEmpty(token)
    || (assessment.AccessToken ?? "").Trim().ToUpper() != (token ?? "").Trim().ToUpper())
{
    return Json(new { success = false, message = "Token tidak valid. Silakan periksa dan coba lagi." });
}
```

**Safety note (VERIFIED):** `:876` is the ONLY token comparison in the codebase. Uppercasing the stored side never breaks an already-uppercase token (CreateAssessment + GenerateSecureToken emit uppercase; client force-uppercases at `Views/CMP/Assessment.cshtml:757`) — it only heals lowercase ones. Auto-heals all locked-out workers with zero DB touch.

---

### 3. `Controllers/CMPController.cs` — `StartExam` D-05 all-empty guard (controller, request-response) — D-05

**Analog:** the existing no-package guard in the **same method** (`CMPController.cs:1198-1203`) — same friendly-message + redirect contract; D-05 is the adjacent "packages exist but all empty" gap.

**EXACT existing sibling guard to mirror (`CMPController.cs:1198-1203`):**
```csharp
else
{
    // Legacy path removed (Phase 227 CLEN-02) — sessions without packages return error.
    TempData["Error"] = "Sesi ujian ini tidak memiliki paket soal. Hubungi Admin atau HC.";
    return RedirectToAction("Assessment");
}
```

**CRITICAL placement (Pitfall 1 — write happens BEFORE the engine call):** The `justStarted` mutation writes `Status="InProgress"` + `StartedAt` at `:960-967`, which executes BEFORE the package load (`:995`) and the engine call (`:1019`). The D-05 guard MUST be hoisted BEFORE the `:960-967` write (RESEARCH approach A — recommended), so an all-empty exam never persists `StartedAt`/`Status`, never creates `UserPackageAssignment`, never fires the SignalR `workerStarted` broadcast (`:970-978`), never logs `LogActivityAsync` (`:977`).

**EXACT write block that must be guarded against (`CMPController.cs:960-967`):**
```csharp
// Mark InProgress on first load only (idempotent — skip if already started)
bool justStarted = assessment.StartedAt == null;
if (justStarted)
{
    assessment.Status = "InProgress";
    assessment.StartedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

**EXACT engine consume site for the all-empty detection (`CMPController.cs:1019-1020`):**
```csharp
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
    packages, assessment.ShuffleQuestions, workerIndex, rng);
```

**Recommended D-05 implementation:** hoist a cheap "any non-empty sibling package?" check before line `:960`. The sibling-package load already exists at `:995-1000` (`.Include(p => p.Questions)`); approach A loads (or counts non-empty) before the `justStarted` write. If all packages empty AND ≥1 package exists → block with a friendly BI message (Claude's discretion wording, e.g. "Ujian belum siap — belum ada soal. Silakan hubungi admin.") + `RedirectToAction("Assessment")`, BEFORE any write. The distinct "zero packages at all" case is already handled by the `else` at `:1198`.

---

### 4. `Controllers/AssessmentAdminController.cs` — `AddExtraTime` authz (controller, request-response) — D-02

**Analog:** sibling action `ResetAssessment` (`AssessmentAdminController.cs:3996-4000`) — copy the EXACT attribute string.

**EXACT sibling authz template (`AssessmentAdminController.cs:3996-4000`, VERIFIED):**
```csharp
// --- RESET ASSESSMENT ---
[HttpPost]
[Authorize(Roles = "Admin, HC")]      // ← NOTE: "Admin, HC" WITH A SPACE — copy verbatim (Pitfall 4)
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResetAssessment(int id)
```

**BUG site — current `AddExtraTime` signature (`:6866-6868`, MISSING role gate):**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddExtraTime(int assessmentId, int minutes)
```

**Fix:** insert `[Authorize(Roles = "Admin, HC")]` between the two existing attributes. Use the EXACT string `"Admin, HC"` (with the space) so the reflection-authz test string-equality assertion passes (Pitfall 4 — CONTEXT.md D-02 wrote `"Admin,HC"` without space; defer to the actual sibling convention `"Admin, HC"`).

---

### 5. `Controllers/AssessmentAdminController.cs` — `AddExtraTime` cap (controller, business rule) — D-03

**Analog:** the existing per-call bound + JSON reject contract in the **same method** (`:6870-6871`), and the accumulator at `:6899`.

**EXACT current per-call bound (the reject-contract template, `:6870-6871`):**
```csharp
if (minutes < 5 || minutes > 120 || minutes % 5 != 0)
    return Json(new { success = false, message = "Waktu harus antara 5-120 menit, kelipatan 5." });
```

**EXACT current batch resolve + accumulation (the BUG — no total cap, `:6887-6901`):**
```csharp
var sessions = await _context.AssessmentSessions
    .Where(s => s.Title == repTitle
             && s.Category == repCategory
             && s.Schedule.Date == repDate
             && s.Status == "InProgress")
    .ToListAsync();

if (!sessions.Any())
    return Json(new { success = false, message = "Tidak ada peserta aktif." });

foreach (var session in sessions)
{
    session.ExtraTimeMinutes = (session.ExtraTimeMinutes ?? 0) + minutes;   // ← BUG: unbounded accumulation
}
await _context.SaveChangesAsync();
```

**Fix (per-session cap against `DurationMinutes`, reject-whole-batch — RESEARCH Pattern 4 / Pitfall 3):** insert a pre-check loop BEFORE the accumulation `foreach`, returning the SAME `Json(new { success, message })` contract:
```csharp
foreach (var session in sessions)
{
    var currentExtra = session.ExtraTimeMinutes ?? 0;
    if (currentExtra + minutes > session.DurationMinutes)
        return Json(new { success = false,
            message = $"Total tambahan waktu tidak boleh melebihi durasi ujian ({session.DurationMinutes} menit). Saat ini sudah +{currentExtra} menit." });
}
```

**Model fields confirmed (`Models/AssessmentSession.cs`):** `DurationMinutes` (int, `:19`, original duration), `ExtraTimeMinutes` (int?, `:199`, accumulator). No migration — both already exist. Reject path returns JSON (NOT TempData) — matches the view JS contract (Pitfall 5: e2e helper `addExtraTimeViaModal` asserts `.alert-success`; the cap-reject path won't show that, so the cap e2e must assert the failure UX).

---

### 6. `Controllers/AssessmentAdminController.cs` — EditAssessment Pre/Post token writes (controller, CRUD write) — D-01b

**Analog:** `CreateAssessment` uppercase write (`AssessmentAdminController.cs:1104-1108`, same file).

**EXACT `.ToUpper()` write template (`CreateAssessment`, `:1104-1112`, VERIFIED):**
```csharp
// Ensure Token is uppercase
if (model.IsTokenRequired && !string.IsNullOrEmpty(model.AccessToken))
{
    model.AccessToken = model.AccessToken.ToUpper();
}
else
{
    model.AccessToken = "";
}
```

**3 BUG sites — Pre/Post token writes lacking `.ToUpper()`:**

Site 1 — shared-field loop (`:1812`) — has fallback-to-existing semantics (preserve!):
```csharp
s.AccessToken = model.IsTokenRequired ? (model.AccessToken ?? s.AccessToken ?? "") : "";
```

Site 2 — new Pre session (`:1916`):
```csharp
AccessToken = model.IsTokenRequired ? (model.AccessToken ?? "") : "",
```

Site 3 — new Post session (`:1937`):
```csharp
AccessToken = model.IsTokenRequired ? (model.AccessToken ?? "") : "",
```

**Fix (RESEARCH Pattern 3):** compute one normalized token near the top of the Pre/Post branch (after `:1786`), then assign uppercase at all 3 sites:
```csharp
string normalizedToken = (model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken))
    ? model.AccessToken.ToUpper()
    : "";
// :1812  s.AccessToken = model.IsTokenRequired ? (normalizedToken != "" ? normalizedToken : (s.AccessToken ?? "")) : "";
// :1916/:1937  AccessToken = model.IsTokenRequired ? normalizedToken : "",
```
**Gotcha (`:1812`):** preserve the "keep existing token if model didn't supply one" fallback — but uppercase whatever ends up stored. Do NOT silently wipe an existing token when `model.AccessToken` is null. The Pre/Post branch RETURNS at `:1957` (`return RedirectToAction("ManageAssessment")`), BEFORE the single-mode uppercase logic at `:2010-2017` — so these 3 sites are the only normalization the Pre/Post path gets.

---

## Test Templates

### 7. `HcPortal.Tests/ShuffleEngineTests.cs` — ADD 2 ON-path facts (WSE-01 engine) — pure unit

**Analog:** existing facts in the SAME file. The `Pkg(...)` in-memory builder already exists (`:18-30`); empty package = `Pkg(n)` with no question args. Determinism via `new Random(42)`. Mirror the existing `Off_AllPackagesEmpty_ReturnsEmpty_NoDivideByZero` (`:88-94`) and `On_MultiPackage_SeedStable_SamplesKMin` (`:128-140`).

**EXACT `Pkg` builder (`:18-30`, reuse as-is):**
```csharp
private static AssessmentPackage Pkg(int packageNumber, params (int id, int order, string? et)[] qs)
{
    var p = new AssessmentPackage { PackageNumber = packageNumber, Id = packageNumber };
    foreach (var (id, order, et) in qs)
        p.Questions.Add(new PackageQuestion
        {
            Id = id, Order = order, ElemenTeknis = et,
            Options = { new PackageOption { Id = id * 10 }, new PackageOption { Id = id * 10 + 1 } }
        });
    return p;
}
```

**Add (RESEARCH Code Examples):**
```csharp
[Fact] // WSE-01: ON-path, 2 packages one empty → worker gets the filled package's questions (NOT empty)
public void On_MultiPackage_OneEmpty_ReturnsFilledPackageQuestions()
{
    var p1 = Pkg(1, (10, 1, null), (11, 2, null), (12, 3, null)); // 3 questions
    var p2 = Pkg(2);                                              // EMPTY
    var packages = new List<AssessmentPackage> { p1, p2 };
    var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
    Assert.NotEmpty(result);                                       // BUG: currently returns [] (K=Min=0)
    Assert.Equal(new HashSet<int> { 10, 11, 12 }, result.ToHashSet());
}

[Fact] // D-05 engine half: ON-path, all packages empty → engine returns empty (controller blocks)
public void On_AllPackagesEmpty_ReturnsEmpty()
{
    var packages = new List<AssessmentPackage> { Pkg(1), Pkg(2) };
    var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
    Assert.Empty(result);
}
```

### 8. `HcPortal.Tests/AddExtraTimeAuthTests.cs` — NEW reflection-authz (WSE-03 / RST-01) — pure unit

**Analog:** `CDPControllerAuthTests.cs` (whole file, `:1-27`) — exact reflection pattern.

**EXACT template (`CDPControllerAuthTests.cs:11-27`, VERIFIED):**
```csharp
public class CDPControllerAuthTests
{
    [Fact]
    public void ExportHistoriProton_AllowsCoachAndAbove()
    {
        var method = typeof(CDPController).GetMethod(nameof(CDPController.ExportHistoriProton));
        Assert.NotNull(method);
        var authz = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(authz);
        Assert.Equal(UserRoles.RolesCoachAndAbove, authz!.Roles);
    }
}
```

**Adapt for AddExtraTime (assert exact string `"Admin, HC"` — Pitfall 4):**
```csharp
var method = typeof(AssessmentAdminController).GetMethod(nameof(AssessmentAdminController.AddExtraTime));
...
Assert.Equal("Admin, HC", authz!.Roles);   // MUST match the attribute string written in fix #4
```
Imports: `System.Linq`, `Microsoft.AspNetCore.Authorization`, `HcPortal.Controllers`, `Xunit`. `AddExtraTime` is `public async Task<IActionResult>` → reflection-discoverable.

### 9. Integration tests (cap + token write + token compare) — DB-backed (WSE-02 write/compare, WSE-03 cap)

**Analog:** `ProtonYearGateIntegrationTests.cs` (`:1-31`) — `[Trait("Category","Integration")]` + `IClassFixture<Fixture>` over a disposable real-SQL `HcPortalDB_Test_<guid>` (NOT `HcPortalDB_Dev` → no SEED_WORKFLOW snapshot needed; skip in SQL-less CI).

**EXACT class scaffold template (`ProtonYearGateIntegrationTests.cs:16-28`):**
```csharp
[Trait("Category", "Integration")]
public class ProtonYearGateIntegrationTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;
    public ProtonYearGateIntegrationTests(ProtonCompletionFixture fixture) { _fixture = fixture; }
    // await using var ctx = new ApplicationDbContext(_fixture.Options);  ← DB access pattern
```
Use this scaffold for: VerifyToken defensive-compare (seed lowercase `AccessToken` → assert match), EditAssessment token-uppercase-on-write (assert stored UPPERCASE at all 3 Pre/Post sites), AddExtraTime cap (seed session with `ExtraTimeMinutes` near `DurationMinutes` → assert reject). A reflection-only test (template #8) covers the authz half without DB.

### 10. `tests/e2e/exam-taking.spec.ts` — ADD #5 (token lowercase) + #6 (empty package) — e2e

**Analog:** existing Flow B in the same spec + helpers in `tests/e2e/helpers/examTypes.ts`.

**Token wizard support (`examTypes.ts:129-139`, VERIFIED — `createAssessmentViaWizard` `isTokenRequired`/`accessToken`):**
```typescript
if (opts.isTokenRequired) {
    await page.locator(wizardSelectors.isTokenRequired).check();
    await page.locator(wizardSelectors.tokenSection).waitFor({ state: 'visible', timeout: 5_000 });
    if (opts.accessToken) { await page.fill(wizardSelectors.accessToken, opts.accessToken); }
    ...
}
```
- **#5 token lowercase:** HC creates token-required Pre/Post → EDIT with lowercase token (e.g. `'abc23x'`) → login worker → token modal (input auto-uppercases per `Assessment.cshtml:757`) → `POST /CMP/VerifyToken` → assert success redirect (NOT "Token tidak valid"). DB-assert via `tests/helpers/dbSnapshot.ts` `queryString` that `AccessToken` is stored UPPERCASE after the edit.
- **#6 empty package + shuffle ON:** call `createDefaultPackage` twice, `addQuestionViaForm` to only ONE → worker StartExam → assert questions count > 0 → submit → Score > 0 (NOT 0% Fail). Helpers: `createDefaultPackage`, `addQuestionViaForm`, `submitExamTwoStep`, `dbSnapshot`.
- **Cap e2e (optional):** `addExtraTimeViaModal` (`examTypes.ts:535`) asserts `.alert-success`; the cap-reject path won't show that — assert the failure UX instead (Pitfall 5).
- **Run constraint (MEMORY / Pitfall 6):** `cd tests && npx playwright test --workers=1`; local SQL needs SQLBrowser + `lpc:` shared-memory override; local `dotnet run` needs `Authentication__UseActiveDirectory=false`.

---

## Shared Patterns

### Uppercase-token invariant (applies to fixes #2 + #6)
**Source:** `AssessmentAdminController.cs:1104-1108` (`CreateAssessment`).
**Apply to:** `CMPController.VerifyToken` compare (both sides) + `EditAssessment` Pre/Post writes.
**Invariant:** tokens are stored & compared UPPERCASE; client force-uppercases (`Assessment.cshtml:757` — do NOT remove). Defensive compare heals legacy lowercase at read-time (zero DB touch); uppercase-on-write prevents NEW lowercase.

### Role attribute on admin write actions (applies to fix #4)
**Source:** `AssessmentAdminController.cs:3998` — `[Authorize(Roles = "Admin, HC")]` (exact string, with space).
**Apply to:** `AddExtraTime`. ~60 sibling actions follow this; base `[Authorize]` on `AdminBaseController` permits any authenticated user (no global FallbackPolicy in `Program.cs`), so the per-action role attribute is the real RST-01 fix. `[ValidateAntiForgeryToken]` (already present) stops CSRF but NOT a legit-authenticated worker.

### Friendly-message + redirect / JSON-reject contracts
**Source (redirect):** `CMPController.cs:1198-1203` (`TempData["Error"] = …; return RedirectToAction("Assessment");`).
**Source (JSON):** `AssessmentAdminController.cs:6870-6871` (`return Json(new { success = false, message = … });`).
**Apply to:** D-05 StartExam guard uses the redirect contract; AddExtraTime cap reject uses the JSON contract (matches view JS).

### Empty-package filter parity (applies to fix #1)
**Source:** `ShuffleEngine.cs:53-57` (OFF-path).
**Apply to:** ON-path `BuildCrossPackageAssignment`. The single canonical filter expression `.Where(p => p.Questions != null && p.Questions.Count > 0)` — do NOT hand-roll a variant (Don't Hand-Roll).

### Pure-unit vs reflection-authz vs DB-integration test split
**Source:** `ShuffleEngineTests.cs` (pure), `CDPControllerAuthTests.cs` (reflection), `ProtonYearGateIntegrationTests.cs` (`[Trait("Category","Integration")]` + fixture).
**Apply to:** engine facts = pure; authz = reflection; cap/token-write/token-compare = integration (disposable test DB).

---

## No Analog Found

None. Every fix has an existing, tested in-repo precedent (OFF-path filter, CreateAssessment uppercase, ResetAssessment authz, no-package guard, three test-type templates). The correct implementation is "make the broken path match its working sibling" — no RESEARCH-only / invented patterns required.

---

## No-Migration Confirmation

`migration=false` (VERIFIED RESEARCH). No fix touches a model property, relationship, or index. `ExtraTimeMinutes` (`AssessmentSession.cs:199`), `DurationMinutes` (`:19`), `AccessToken`, `ShuffleQuestions` (`:39`) all already exist; ShuffleEngine is pure logic. Executor MUST verify NO `dotnet ef migrations add` produces a model-snapshot diff.

---

## Metadata

**Analog search scope:** `Helpers/`, `Controllers/`, `Models/`, `HcPortal.Tests/`, `tests/e2e/helpers/`, `tests/helpers/`.
**Files scanned (read or grep-verified this session):** `ShuffleEngine.cs` (full), `CMPController.cs` (`:845-1215`), `AssessmentAdminController.cs` (`:1098-1117`, `:1780-1958`, `:3990-4014`, `:5208-5310`, `:6855-6930`), `AssessmentSession.cs` (fields), `ShuffleEngineTests.cs` (full), `CDPControllerAuthTests.cs` (full), `ProtonYearGateIntegrationTests.cs` (`:1-75`), `examTypes.ts` (`:120-145`, `:525-554`).
**Pattern extraction date:** 2026-06-14
**Line-number caveat:** verified against current source this session; Phase 381/382 also touch `CMPController` — re-verify `CMPController.cs` line anchors if a parallel phase edits it before execution.
