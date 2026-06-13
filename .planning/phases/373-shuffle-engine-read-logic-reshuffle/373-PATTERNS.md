# Phase 373: Shuffle Engine (read logic + reshuffle) - Pattern Map

**Mapped:** 2026-06-13
**Files analyzed:** 4 (1 new helper, 1 new test, 2 modified controllers)
**Analogs found:** 4 / 4

> **Line numbers re-grepped live this session (2026-06-13)** — file-overlap v25.0 (367/368) active. Drift since RESEARCH is minimal but real: StartExam stale-count guard moved `1027→1028`; ReshufflePackage sibling lookup `5083→5086`; comment `:1054`, both `BuildCrossPackageAssignment` (CMP `:1230`, Admin `:5250`), both `"{}"` (`:5119`, `:5213`), `Shuffle<T>` (`:1212`, `:5241`) all UNCHANGED. **Executor MUST re-grep anchor strings (not line numbers) at execute-time.**

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/ShuffleEngine.cs` (NEW) | utility (pure static helper) | transform (in-memory list/dict) | `Helpers/ImageFileCleanup.cs` + `Models/QuestionTypeLabels.cs` | exact (role) — both are static helpers extracted from inline controller logic |
| `HcPortal.Tests/ShuffleEngineTests.cs` (NEW) | test (pure unit) | transform | `HcPortal.Tests/QuestionTypeLabelsTests.cs` | exact — pure `[Theory]`/`[Fact]`, calls static directly, no DB/fixture |
| `Controllers/CMPController.cs` (MODIFY) | controller | request-response | self (StartExam build branch is its own pattern) | self-analog — delegate to core, delete local `BuildCrossPackageAssignment` (CANONICAL) + `Shuffle<T>`, fix comment `:1054` |
| `Controllers/AssessmentAdminController.cs` (MODIFY) | controller | request-response | self + CMPController StartExam (option-dict build at `:982-989`) | self-analog — delegate to core, delete DIVERGENT `BuildCrossPackageAssignment` + `Shuffle<T>`, fix `"{}"` bug |

**Optional (planner discretion — CONTEXT D, RESEARCH A3):** real-SQL integration test for SHUF-09 reshuffle. Analog: `HcPortal.Tests/ShuffleCreatePersistenceTests.cs` / `ShufflePropagationTests.cs` (`[Trait("Category","Integration")]` + `IClassFixture<ProtonCompletionFixture>`). RESEARCH recommends at minimum a Wave-0 assertion that optDict is NOT `"{}"` when `ShuffleOptions=ON` (closes the existing-bug regression).

---

## CRITICAL: The Two `BuildCrossPackageAssignment` Copies Are NOT Identical

RESEARCH's central finding, CONFIRMED by direct read this session. Both copies share: empty guard, single-package Fisher-Yates, `K = packages.Min(p => p.Questions.Count)`, no-ET fallback slot-list, and Phase 1 (one-per-ET). **Phase 2 diverges.** Only the **CMPController version is canonical** for the core (locks SC#1 — StartExam behavior must not change). Do NOT blind-copy the AssessmentAdminController version.

### CANONICAL — `Controllers/CMPController.cs:1230-1362` (move VERBATIM into core ON-path)

Phase 2 = **round-robin per-ElemenTeknis** (`basePerET = remaining / M` where `M = etGroups.Count`):

```csharp
// CMPController.cs:1318-1357 — Phase 2 (CANONICAL)
int remaining = K - selectedIds.Count;
if (remaining > 0)
{
    int M = etGroups.Count;
    int basePerET = remaining / M;                                   // ◄── per-ET divisor
    int extraCount = remaining % M;
    var extraETs = etGroups.OrderBy(_ => rng.Next()).Take(extraCount).ToHashSet();

    foreach (var et in etGroups)
    {
        int quota = basePerET + (extraETs.Contains(et) ? 1 : 0);
        var etCandidates = allQuestions
            .Where(x => x.Question.ElemenTeknis == et && !selectedIds.Contains(x.Question.Id))
            .Select(x => x.Question.Id).ToList();
        Shuffle(etCandidates, rng);
        int toTake = Math.Min(quota, etCandidates.Count);
        foreach (var id in etCandidates.Take(toTake)) { selectedIds.Add(id); selectedList.Add(id); }
    }
    // Fallback: NULL-ET / sisa soal manapun jika ET kehabisan
    if (selectedIds.Count < K) { /* take from any unselected */ }
}
Shuffle(selectedList, rng); // Phase 3
return selectedList;
```

### DIVERGENT — `Controllers/AssessmentAdminController.cs:5250-5393` (DO NOT use this Phase 2; DELETE the whole method)

Phase 2 = **slot-distribution per-package** (`baseCount = remaining / N` where `N = packages.Count`) + per-package exhaustion redistribution:

```csharp
// AssessmentAdminController.cs:5338-5388 — Phase 2 (DIVERGENT — discard)
int remaining = K - selectedIds.Count;
if (remaining > 0)
{
    int N = packages.Count;                                          // ◄── per-PACKAGE divisor (DIFFERENT)
    var orderedByPackage = packages.Select(p => p.Questions.OrderBy(q => q.Order)
        .Where(q => !selectedIds.Contains(q.Id)).ToList()).ToList();
    int baseCount = remaining / N;
    int remainder = remaining % N;
    // ... builds per-package slot list, Shuffle(slots), then redistributes
    //     to any package with remaining questions when a package is exhausted ...
}
```

**Consequence (planner MUST flag, RESEARCH Pitfall 1):** after dedup, reshuffle's ON-path changes to the CMPController algorithm. This is **correct and intended** (reshuffle↔StartExam consistency), but it is NOT a no-op move. Warning sign of regression: an ON ≥2-package test with a fixed seed produces a different order before/after refactor at the reshuffle call-site.

---

## Pattern Assignments

### `Helpers/ShuffleEngine.cs` (NEW — utility, transform)

**Analog 1 (structure/namespace/static):** `Helpers/ImageFileCleanup.cs`
**Analog 2 (pure no-DB switch helper):** `Models/QuestionTypeLabels.cs`

**Namespace + static-class shape** (copy from `Helpers/ImageFileCleanup.cs:5-8`):
```csharp
using HcPortal.Models;   // AssessmentPackage, PackageQuestion, PackageOption

namespace HcPortal.Helpers
{
    public static class ShuffleEngine
    {
        // ...
    }
}
```
> `ImageFileCleanup` imports `Microsoft.EntityFrameworkCore` + `HcPortal.Data` because it touches `ApplicationDbContext`. **ShuffleEngine must NOT** — it is pure (no EF, no DB). Import only `HcPortal.Models` + BCL (`System`, `System.Linq`, `System.Collections.Generic`). Keeping EF out = the purity that makes it unit-testable (RESEARCH anti-pattern: "Menaruh query EF di dalam core").

**Fisher-Yates `Shuffle<T>`** — move VERBATIM from `CMPController.cs:1212-1219` (identical copy at `AssessmentAdminController.cs:5241-5248`; delete both after move):
```csharp
public static void Shuffle<T>(List<T> list, Random rng)
{
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = rng.Next(i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}
```
> Make it `public static` (was `private static` in both controllers) so the core hosts it and call-sites/tests can reach it. `Don't Hand-Roll`: never replace with `OrderBy(rng.Next())` (biased + non-deterministic-with-seed).

**ON-path** = the entire CANONICAL `BuildCrossPackageAssignment` body from `CMPController.cs:1230-1362` moved byte-for-byte (single-pkg Fisher-Yates, K=min, no-ET fallback slot-list, Phase 1/2/3). It already takes `(List<AssessmentPackage>, Random)` and is already `static` — minimal adaptation.

**OFF-path** (NEW — SHUF-05/06, D-02/D-02b/D-05). Recommended combined entry signature (RESEARCH §"Recommended Structure"; final signature = planner discretion per CONTEXT D-01):
```csharp
public static List<int> BuildQuestionAssignment(
    List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
{
    if (packages.Count == 0) return new List<int>();
    if (shuffleQuestions) return BuildCrossPackageAssignment(packages, rng);   // verbatim canonical

    // OFF + 1 paket (SHUF-05): urut q.Order, NO shuffle, identik semua peserta
    if (packages.Count == 1)
    {
        var q = packages[0].Questions;
        if (q == null || q.Count == 0) return new List<int>();
        return q.OrderBy(x => x.Order).Select(x => x.Id).ToList();
    }
    // OFF + ≥2 paket (SHUF-06): filter-then-modulo (D-02), paket UTUH (D-05)
    var packagesWithQuestions = packages
        .Where(p => p.Questions != null && p.Questions.Count > 0)   // D-02b: guard SEBELUM modulo
        .OrderBy(p => p.PackageNumber)                              // anchor stabil
        .ToList();
    if (packagesWithQuestions.Count == 0) return new List<int>();  // guard DivideByZero (V5)
    var chosen = packagesWithQuestions[workerIndex % packagesWithQuestions.Count];
    return chosen.Questions.OrderBy(x => x.Order).Select(x => x.Id).ToList();  // utuh, urut, NO shuffle
}
```

**Option-shuffle path** (NEW — SHUF-07, D-06; pattern lifted from inline build at `CMPController.cs:982-989`):
```csharp
public static Dictionary<int, List<int>> BuildOptionShuffle(
    IEnumerable<PackageQuestion> questions, bool shuffleOptions, Random rng)
{
    var dict = new Dictionary<int, List<int>>();
    if (!shuffleOptions) return dict;        // OFF → empty → caller serializes "{}" → view DB-order fallback
    foreach (var q in questions)
    {
        var optionIds = q.Options.Select(o => o.Id).ToList();
        Shuffle(optionIds, rng);
        dict[q.Id] = optionIds;
    }
    return dict;
}
```
> Source inline (CMPController.cs:982-989) currently feeds `packages.SelectMany(p => p.Questions)` (ALL questions). For OFF≥2 only the worker's package questions are relevant — but extra keys are harmless (view looks up by qId). A4/OQ#2: planner picks `shuffledIds`-filtered (cleaner) or all (both correct).

---

### `HcPortal.Tests/ShuffleEngineTests.cs` (NEW — test, pure unit)

**Analog:** `HcPortal.Tests/QuestionTypeLabelsTests.cs` (entire file — pure, no DB)

**Pattern** (copy structure from `QuestionTypeLabelsTests.cs:1-24`):
```csharp
using HcPortal.Helpers;   // ShuffleEngine  (QuestionTypeLabelsTests uses HcPortal.Models)
using HcPortal.Models;    // AssessmentPackage, PackageQuestion, PackageOption
using Xunit;

namespace HcPortal.Tests;

public class ShuffleEngineTests
{
    [Fact]
    public void Off_SinglePackage_ReturnsQuestionsInOrder() { /* ... */ }
    // [Theory]/[InlineData] for worker-index → package mapping, etc.
}
```

**Key conventions to copy:**
- **No** `[Trait("Category","Integration")]`, **no** `IClassFixture`, **no** `ApplicationDbContext` (contrast with `ShuffleCreatePersistenceTests.cs`).
- `[Theory]`/`[InlineData]` for parametrized cases (QuestionTypeLabelsTests style); `[Fact]` for single-shape assertions.
- Call the static directly: `Assert.Equal(expected, ShuffleEngine.BuildQuestionAssignment(...))`.
- **In-memory POCO construction is valid** — `AssessmentPackage`/`PackageQuestion`/`PackageOption` (`Models/AssessmentPackage.cs:6-95`) have collection-initializer navs (`= new List<...>()`) and plain scalar props. Build fixtures with object initializers, no DB:
  ```csharp
  var pkg = new AssessmentPackage {
      PackageNumber = 1,
      Questions = { new PackageQuestion { Id = 10, Order = 2, ElemenTeknis = "ET-A",
          Options = { new PackageOption { Id = 100 }, new PackageOption { Id = 101 } } } }
  };
  ```
- **Determinism ON:** pass `new Random(seed)` (fixed) → assert exact order deterministic. **OFF:** no rng needed (or any rng — output independent of it).
- Coverage (RESEARCH Wave-0 Gaps): SHUF-04 (ON 1pkg + ON ≥2 seed-stable), SHUF-05 (OFF 1pkg ordered), SHUF-06 (OFF ≥2 worker[i]→pkg[i%count] utuh; index-stable on append; empty-package excluded BEFORE modulo), SHUF-07 (ON dict non-empty / OFF empty; independence: questions OFF + options ON), SHUF-08 (call 2× same input → identical output).

---

### `Controllers/CMPController.cs` (MODIFY — controller, request-response)

**StartExam build branch** (`:973-1003`) — currently calls local `BuildCrossPackageAssignment(packages, rng)` (`:978`) and inline option-dict (`:982-989`). REPLACE with calls to `ShuffleEngine`, gated on `assessment.ShuffleQuestions`/`assessment.ShuffleOptions` (flags already loaded — propagated Phase 372).

Existing build-branch context to preserve verbatim (sentinel, persist, race-guard):
```csharp
// CMPController.cs:991-1018 — KEEP AS-IS (only the shuffledIds/optionDict SOURCE changes)
var sentinelPackage = packages.First();
assignment = new UserPackageAssignment {
    AssessmentSessionId = id,
    AssessmentPackageId = sentinelPackage.Id,   // sentinel
    UserId = user.Id,
    ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
    ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict)
};
assignment.SavedQuestionCount = shuffledIds.Count;
_context.UserPackageAssignments.Add(assignment);
try { await _context.SaveChangesAsync(); }
catch (DbUpdateException) { /* race-guard: reload existing */ }
```

**Worker-index resolution** (NEW, controller-side — RESEARCH Pattern 2 + Pitfall 2). The sibling query `:949-954` has **no `OrderBy`** — SQL Server order not guaranteed → `IndexOf` unstable. Planner MUST sort before computing index:
```csharp
// CMPController.cs:949-954 sibling query — add stable ordering for index, then:
var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();
int workerIndex = sortedSiblingIds.IndexOf(id);   // id = this participant's session
```
> `workerIndex` used only by OFF≥2; ON ignores it. Use the SAME sibling set (Title+Category+Schedule.Date, NO status filter) at all 3 call-sites for cross-call determinism (OQ#1).

**Stale-count guard** (`:1027-1040`) — D-03: PRESERVE verbatim. Determinism (D-02) keeps count stable so it does not false-trigger.

**Comment fix** (`:1054` — SHUF-15/D-07). Replace:
```csharp
// SEBELUM (stale):
// Options in original DB order — option shuffle removed per user decision
// SESUDAH (reflects reality — options active, gated by ShuffleOptions):
// Options in DB order here (base list); per-user reorder applied in view via
// ViewBag.OptionShuffle when ShuffleOptions=ON. OFF stores "{}" → view falls back to this DB order.
```
> Verify via `rg "option shuffle removed" Controllers/CMPController.cs` → 0 matches.

**Delete after move:** local `BuildCrossPackageAssignment` (`:1230-1362`) + local `Shuffle<T>` (`:1212-1219`). The VM opts build (`:1055-1061`, `OrderBy(o => o.Id)`) and `ViewBag.OptionShuffle` parse (`:1144-1157`) are UNCHANGED (Pitfall 4: don't add shuffle to VM opts).

---

### `Controllers/AssessmentAdminController.cs` (MODIFY — controller, request-response)

**`ReshufflePackage`** (`:5065`) — `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (`:5062-5064`) and guard `:5083-5084` ("Not started/Abandoned") PRESERVE verbatim (D-04a, V4). Fix the call + the `"{}"` bug:
```csharp
// AssessmentAdminController.cs:5109-5120 — SEBELUM:
var rng = Random.Shared;
var shuffledIds = BuildCrossPackageAssignment(packages, rng);   // ignores flag, always shuffles
// ...
ShuffledOptionIdsPerQuestion = "{}"                              // BUG: always empty (:5119)

// SESUDAH (assessment already loaded :5067-5068, has .ShuffleQuestions/.ShuffleOptions):
var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();          // :5086-5091 set
int workerIndex = sortedSiblingIds.IndexOf(sessionId);
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
    packages, assessment.ShuffleQuestions, workerIndex, Random.Shared);
var assignedQs = packages.SelectMany(p => p.Questions).Where(q => shuffledIds.Contains(q.Id));
var optDict = ShuffleEngine.BuildOptionShuffle(assignedQs, assessment.ShuffleOptions, Random.Shared);
// ... ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optDict)   // FIX (was "{}")
```

**`ReshuffleAll`** (`:5146`) — same attributes (`:5143-5145`) + guard `:5191` PRESERVE. Same fix per-session inside `foreach (var session in sessions)` (`:5177`). The `"{}"` bug is at `:5213`. Sort `siblingSessionIds` (`:5158`) once; `workerIndex = sortedIds.IndexOf(session.Id)` per iteration. Verify the sibling set here equals StartExam's (OQ#1 — both key on Title+Category+Schedule.Date).

**Delete after move:** DIVERGENT `BuildCrossPackageAssignment` (`:5250-5393`) + local `Shuffle<T>` (`:5241-5248`). The audit-log blocks (`:5125-5137`, `:5222-5234`) and sentinel-package logic (`:5111`, `:5202`) are UNCHANGED.

---

## Shared Patterns

### Static-helper extraction (project standard)
**Source:** `Helpers/ImageFileCleanup.cs` (Phase 366), `Models/QuestionTypeLabels.cs` (Phase 357)
**Apply to:** `Helpers/ShuffleEngine.cs`
Pattern: pure stateless logic pulled out of a controller into `public static class` under `Helpers/` (logic) or `Models/` (labels); controllers delegate. Precedent for the WHOLE phase. ShuffleEngine differs from ImageFileCleanup in that it stays pure (no `ApplicationDbContext`, no `async`) — all EF stays in controllers.

### Fisher-Yates `Shuffle<T>`
**Source:** `CMPController.cs:1212-1219` == `AssessmentAdminController.cs:5241-5248` (currently duplicated)
**Apply to:** hosted once in `ShuffleEngine`; both controllers' copies deleted. Unbiased + seed-deterministic.

### Stable worker-index = `OrderBy(Id)` + modulo
**Source:** NEW (D-02); anchor pattern from `Packages OrderBy(p => p.PackageNumber)` used at all queries (`CMP:960`, `Admin:5097/5163`)
**Apply to:** all 3 call-sites (StartExam, ReshufflePackage, ReshuffleAll).
Anti-pattern to AVOID (D-02c): `assignmentCount % n` / "open order" → shifts on resume/reshuffle → false "Soal telah berubah". Guard empty packages BEFORE modulo (D-02b) to avoid `% 0` DivideByZero (V5).

### Auth/guard preservation (security — V4)
**Source:** `AssessmentAdminController.cs:5062-5064` + `:5143-5145` (`[Authorize]`+`[ValidateAntiForgeryToken]`), guards `:5083-5084` / `:5191`; StartExam ownership check `~:869`
**Apply to:** both reshuffle endpoints + StartExam — refactor must NOT loosen any of these (D-04a). No new endpoints, no new user input.

### JSON persist
**Source:** `System.Text.Json.JsonSerializer.Serialize(...)` (CMP:999-1000, Admin:5118/5212) ↔ `UserPackageAssignment.GetShuffledQuestionIds()`/`GetShuffledOptionIds()` (`Models/UserPackageAssignment.cs:60-94`)
**Apply to:** assignment persist in all 3 call-sites. Format unchanged; grading by `PackageOption.Id` via `GetShuffledQuestionIds()` (`GradingService.cs:70,339`) — both modes, UNCHANGED (D-06a).

### Pure unit-test convention
**Source:** `HcPortal.Tests/QuestionTypeLabelsTests.cs`
**Apply to:** `ShuffleEngineTests.cs`. No fixture/DB/Trait; `[Theory]`/`[Fact]`; call static directly; in-memory POCO fixtures.
**Contrast (for the optional SHUF-09 integration test):** `ShuffleCreatePersistenceTests.cs:23-31` — `[Trait("Category","Integration")]` + `IClassFixture<ProtonCompletionFixture>` + `new ApplicationDbContext(_fixture.Options)`.

---

## No Analog Found

None. Every piece of logic already exists in production; Phase 373 is consolidation + flag-gating, not invention. The OFF-path (SHUF-05/06) and `BuildOptionShuffle` are new code, but each follows an existing project pattern (POCO LINQ over `Questions`/`Options`, the same `OrderBy`/`Shuffle` primitives) — no novel architecture.

## Metadata

**Analog search scope:** `Helpers/`, `Models/`, `Controllers/`, `HcPortal.Tests/`, `Views/CMP/`
**Files scanned (read in full or relevant region):** `Helpers/ImageFileCleanup.cs`, `Models/QuestionTypeLabels.cs`, `Models/AssessmentPackage.cs`, `Models/UserPackageAssignment.cs`, `Controllers/CMPController.cs` (StartExam region + helpers), `Controllers/AssessmentAdminController.cs` (reshuffle region + helpers), `HcPortal.Tests/QuestionTypeLabelsTests.cs`, `HcPortal.Tests/ShuffleCreatePersistenceTests.cs`
**Pattern extraction date:** 2026-06-13
**Line-number caveat:** v25.0 file-overlap (367/368) — re-grep anchors at execute-time, do not trust line numbers.
