# Phase 424: Grading De-dup + Flow/Linking + Gating Pre→Post - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 11 units (3 new helpers, 5 modified sites, 6 test files to extend/create)
**Analogs found:** 11 / 11 (every new/modified unit has a concrete in-repo analog)

> Built ON TOP of `424-RESEARCH.md` (line-verified map). This doc attaches the concrete closest-analog **code excerpts** the planner mirrors. Where RESEARCH already states a line/site, this doc cites it instead of re-deriving. **Use RESEARCH line numbers (re-verified) — CONTEXT line numbers are DRIFTED.**
>
> **GRDF-06 is OUT OF SCOPE** (covered by v32.5 on branch `main`). No participant-management files are mapped here.

---

## File Classification

| New/Modified Unit | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/PrePostPairing.cs` (NEW) | domain helper (pure-ish, async EF lookup) | request-response (pairing lookup) | `Helpers/SiblingSessionQuery.cs` (pure-Expression pairing) | role-match (analog is sync-Expression; this is async-EF, A2) |
| `Helpers/AssessmentScoreAggregator.cs` (MODIFIED — promote to single scorer) | domain helper (pure, EF-free) | itself (`Compute` / `IsQuestionCorrect`) + dedupe pattern `GradingService.cs:87-90` | exact (extend existing pure scorer) |
| `Helpers/ExamTimeRules.cs` (NEW) | domain helper (pure, EF-free) | inline `(Duration+Extra)*60` at `CMPController.cs:1626` (correct site) | role-match (extract existing inline formula) |
| `Controllers/CMPController.cs` StartExam gate (MODIFIED, ~:941) | controller (server-authoritative gate) | existing gate blocks `CMPController.cs:930-974` (TempData+Redirect) | exact (mirror sibling gate) |
| `Controllers/CMPController.cs` essay-empty reject (MODIFIED, :1630-1656) | controller (server-authoritative validation) | existing incomplete-gate `:1630-1656` (on-time branch) | exact (tighten same branch) |
| `Controllers/CMPController.cs` clamp fix (MODIFIED, :469) | controller (write clamp) | correct clamp site `:1626` / `:1175` / `:1548` | role-match (align bug to correct sites) |
| `Controllers/AssessmentAdminController.cs` auto-pair off (MODIFIED, :876-882) | controller (creation-time guard) | existing `CreationMode != "PrePostTest"` branch itself | exact (forward-only disable) |
| `HcPortal.Tests/PrePostGatingTests.cs` (NEW, real-SQL) | test (integration) | `GradingDedupeTests.cs` fixture recipe `:29-83` | exact (reuse disposable-DB fixture) |
| `HcPortal.Tests/PrePostPairingTests.cs` / extend (pure/real-SQL) | test (unit/integration) | `SiblingPrePostFilterTests.cs:12-45` (Compile() truth-table) | exact (mirror predicate truth-table) |
| `HcPortal.Tests/ExamTimeRulesTests.cs` (NEW, pure) | test (unit) | `SiblingPrePostFilterTests.cs` pure style | exact (pure no-DB) |
| `HcPortal.Tests` extend `GradingDedupeTests` / `AssessmentScoreAggregatorTests` / `EnsureCanSubmitStandardTests` / `EssayEmptyPendingParityTests` | test (mixed) | the files themselves (all exist — VERIFIED Glob) | exact (extend in place) |

---

## Pattern Assignments

### 1. `Helpers/PrePostPairing.cs` (NEW) — domain helper, pairing lookup (GRDF-03, feeds GRDF-01)

**Analog:** `Helpers/SiblingSessionQuery.cs` (full, 25 lines) — the project's pure-pairing pattern: a `static` class in `Helpers/` returning a typed result with a doc-comment naming the requirement + every call-site it unifies.

**Analog excerpt to mirror** (`Helpers/SiblingSessionQuery.cs:7-25`):
```csharp
public static class SiblingSessionQuery
{
    // WSE-04 (D-01/D-09): type-aware sibling isolation ... Dipakai IDENTIK di StartExam + ReshufflePackage +
    // ReshuffleAll untuk jaga determinisme workerIndex (Phase 373 invariant).
    public static Expression<Func<AssessmentSession, bool>> SiblingPrePostAwarePredicate(
        string title, string category, DateTime scheduleDate, string? assessmentType)
    {
        bool isPrePost = assessmentType == "PreTest" || assessmentType == "PostTest";
        return s => s.Title == title
                    && s.Category == category
                    && s.Schedule.Date == scheduleDate.Date
                    && ( isPrePost ? s.AssessmentType == assessmentType
                                   : (s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest") );
    }
}
```

**Shape to write** (per RESEARCH Pattern 1; async because gate needs a DB row, A2): `static Task<AssessmentSession?> FindPairedPreAsync(ApplicationDbContext, AssessmentSession post)` — return `null` for orphan/Standard (caller pass-through, D-02). Branch order: (1) `post.LinkedSessionId` explicit Post→Pre, (2) fallback `post.LinkedGroupId + UserId + AssessmentType=="PreTest"`. Title-pattern matching FORBIDDEN.

**The 3 divergent pairing call-sites it must converge** (RESEARCH "Code Examples" + `:352-356`):
| # | Site | Current key | Defect |
|---|------|-------------|--------|
| 1 | `CMPController.cs:292-297` (display grouping) | `LinkedGroupId + AssessmentType + Status` | **NO `UserId` filter** → can pair another worker's Pre (FLOW-01 root). See excerpt below. |
| 2 | `CMPController.cs:3505-3523` `GetGainScoreData` | `LinkedGroupId + UserId` (via Dict) | correct — already filters UserId; converge to helper |
| 3 | `CMPController.cs:2404-2413` (retake) + trend/gain `:2790-2828` etc. | `LinkedSessionId` explicit | correct — most explicit; helper makes this the canonical first branch |

**Defective call-site excerpt** (`CMPController.cs:292-297` — KRITIS, missing UserId):
```csharp
var completedPreSessions = await _context.AssessmentSessions
    .Where(s => s.AssessmentType == "PreTest"
        && s.LinkedGroupId.HasValue
        && postGroupIds.Contains(s.LinkedGroupId.Value)
        && s.Status == "Completed")       // ← NO s.UserId == userId → cross-user pairing (FLOW-01)
    .ToListAsync();
```
> Helper MUST add `s.UserId == post.UserId` on EVERY branch. (RESEARCH Pitfall 4.)

---

### 2. `Helpers/AssessmentScoreAggregator.cs` (MODIFIED — promote to single scorer) — GRDF-02 / D-06 / D-07

**Analog:** the file itself. `Compute` (`:26-60`) and `IsQuestionCorrect` (`:73-98`) are already pure/EF-free. The job is to inject the **dedupe last-write-wins** that already exists in `GradingService.cs:87-90` so all 3 paths converge. **Do NOT recompute Completed sessions** (D-07) — parity tests are the net.

**Canonical dedupe to inject** (copy verbatim from `GradingService.cs:87-90` — the dominant path, already D-06):
```csharp
// MC/single-answer only — MultipleAnswer dibaca penuh (multi-row), JANGAN masuk dedupe ini.
var finalByQuestion = allResponses
    .Where(r => r.PackageOptionId.HasValue)
    .GroupBy(r => r.PackageQuestionId)
    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());  // last-write-wins (D-06)
```

**The drift each path uses today** (must all become last-write-wins for MC; MA/Essay logic stays byte-identical):
| Path | File:line | MC selection today | Action |
|------|-----------|--------------------|--------|
| 1 INITIAL grade | `GradingService.cs:105-114` | `finalByQuestion[...]` (dedupe) ✅ canonical | keep |
| 2 re-grade/preview | `GradingService.cs:410-417` | `mcSel.First()` — **no order** ❌ | feed deduped / pull dedupe into scorer |
| 3 essay-finalize | `AssessmentScoreAggregator.cs:39` | `FirstOrDefault` — **no dedupe** ❌ | inject dedupe |

**Per-question logic that must stay byte-identical across all 3 (already correct in `IsQuestionCorrect`, `:78-96`):**
- MC: single selected option's `IsCorrect`; 0 selected → wrong.
- MA: `selected.Count > 0 && selected.SetEquals(correct)` — non-empty guard. **MA is NOT deduped** (multi-row legal — RESEARCH Pitfall 3).
- Essay: `EssayScore.HasValue ? EssayScore.Value > 0 : null` (null = pending).
- Percentage (LOCKED D-04 Phase 376): `maxScore>0 ? (int)((double)totalScore/maxScore*100) : 0`; `isPassed = pct >= passPercentage` — IDENTICAL at `Aggregator.cs:58`, `GradingService.cs:144-145`, `:429-430`. **Do not touch.**

**`Compute` MC branch today** (`AssessmentScoreAggregator.cs:38-45` — the `FirstOrDefault` to replace with deduped lookup):
```csharp
case "MultipleChoice":
    var mcResp = respList.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue); // ❌ no dedupe
    if (mcResp != null) {
        var opt = q.Options.FirstOrDefault(o => o.Id == mcResp.PackageOptionId!.Value);
        if (opt != null && opt.IsCorrect) totalScore += q.ScoreValue;
    }
    break;
```

---

### 3. `Helpers/ExamTimeRules.cs` (NEW) — domain helper, duration math (GRDF-05)

**Analog:** the correct inline formula already used at 4 sites — extract it. Representative correct site (`CMPController.cs:1626`, the `serverTimerExpired` calc):
```csharp
var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;  // ✅ includes ExtraTime
```

**Shape to write** (RESEARCH Pattern 4):
```csharp
public static class ExamTimeRules
{
    public static int AllowedExamSeconds(int durationMinutes, int? extraTimeMinutes)
        => (durationMinutes + (extraTimeMinutes ?? 0)) * 60;
}
```

**The bug to fix** — clamp at `CMPController.cs:469` drops ExtraTime (over-clamps `ElapsedSeconds` → export under-reports):
```csharp
// Clamp 3: tidak boleh melebihi durasi total
clampedElapsed = Math.Min(clampedElapsed, session.DurationMinutes * 60);   // ❌ BOLONG — no ExtraTime
// → REPLACE with:
// clampedElapsed = Math.Min(clampedElapsed, ExamTimeRules.AllowedExamSeconds(session.DurationMinutes, session.ExtraTimeMinutes));
```
> Export "Durasi Aktual" (`AssessmentAdminController.cs:4929-4931`, `ElapsedSeconds/60`) needs **no math change** — it is correct once the clamp at `:469` is fixed (root cause is the write-side clamp, not the read — RESEARCH Pitfall 8).

---

### 4. `Controllers/CMPController.cs` StartExam gate (MODIFIED, insert ~:941) — GRDF-01 / D-01/D-02/D-03

**Analog:** the existing gate chain in the SAME method (`CMPController.cs:930-974`). Every gate is `TempData["Error"] = "..."; return RedirectToAction("Assessment");`. Insert AFTER the `Completed` check (`:936-940`), BEFORE the token gate (`:945`).

**Existing gate template to mirror** (`CMPController.cs:955-960`, the exam-window gate):
```csharp
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
{
    TempData["Error"] = "Ujian sudah ditutup. Waktu ujian telah berakhir.";
    return RedirectToAction("Assessment");
}
```

**Gate to insert** (RESEARCH Pattern 2 — uses helper #1; orphan/Standard pass-through; `Completed` only, NOT `IsPassed`):
```csharp
var pairedPre = await PrePostPairing.FindPairedPreAsync(_context, assessment);
if (pairedPre != null && pairedPre.Status != "Completed")   // D-01: Completed saja
{
    TempData["Error"] = "Selesaikan Pre-Test dulu sebelum mulai Post-Test.";
    return RedirectToAction("Assessment");
}
// pairedPre == null (orphan/Standard) → lewat (D-02 non-destruktif).
```
> Placement constraints: AFTER `Completed` check (`:936`) so a finished Post reloading is not re-gated; BEFORE `StartedAt` write (`:977`) so a blocked session has no write-on-GET. **Owner-check at `:914` stays above the gate** (V4 access control). **Open Question (RESEARCH #1):** mirror the token-gate pattern `:945` — gate worker only (`assessment.UserId == user.Id`); Admin/HC bypass for monitoring. Planner to confirm.

---

### 5. `Controllers/CMPController.cs` essay-empty reject (MODIFIED, :1630-1656) — GRDF-07 / D-04/D-05

**Analog:** the existing on-time incomplete gate in `SubmitExam`, already correctly scoped to `if (!serverTimerExpired)`. This is exactly where the essay-empty reject belongs (D-04: on-time only; timeout still finalizes).

**On-time vs timeout discriminator is already computed server-side** (`CMPController.cs:1622-1628`):
```csharp
bool serverTimerExpired = false;
if (assessment.StartedAt.HasValue && assessment.DurationMinutes > 0)
{
    var elapsed = (DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
    var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;  // (use ExamTimeRules)
    serverTimerExpired = elapsed >= allowed;
}
```

**Existing on-time gate to tighten** (`CMPController.cs:1630-1656`) — the "answered" count today treats an essay with `TextAnswer == ""` as answered (it counts a DB row existing, not content):
```csharp
if (!serverTimerExpired)
{
    // ...
    var dbResponses = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == id && shuffledQIds.Contains(r.PackageQuestionId))
        .Select(r => r.PackageQuestionId)          // ❌ no TextAnswer / QuestionType → empty essay counts as answered
        .Distinct().ToListAsync();
    // ... answeredCount < totalQuestions → TempData["Error"] = $"Masih ada {unanswered} soal...";
    //     return RedirectToAction("ExamSummary", new { id });
}
// else (timeout) → falls through, finalize → PendingGrading (D-04, Phase 386 PXF-04 — DO NOT touch)
```

**GRDF-07 change** (RESEARCH Pitfall 7 + Open Question #2): widen the `dbResponses` projection to `{ PackageQuestionId, TextAnswer }`, look up which `qId` are `QuestionType=="Essay"` (from `packageQuestions`), and for those count "answered" only when `!string.IsNullOrWhiteSpace(TextAnswer)`. On-time + ≥1 empty essay → block the WHOLE submit with the existing `TempData["Error"]`+`RedirectToAction("ExamSummary")` shape (D-05) using a friendly message (e.g. "Isi semua jawaban essay dulu"). Keep MC/MA counting as-is. **Server authoritative — `flushEssay` client stays as UX only** (lesson Phase 413). **No answer-key leak in the message** (V5).

---

### 6. `Controllers/AssessmentAdminController.cs` auto-pair off (MODIFIED, :876-882) — GRDF-04 / D-08

**Analog:** the existing creation-time branch itself. Forward-only: stop calling `TryAutoDetectCounterpartGroup` (title-regex `^(Pre|Post)\s*Test\s+...$` at `:7663-7689`) for Standard. Old mislinked rows untouched (D-08).

**Branch to disable for Standard** (`AssessmentAdminController.cs:876-887`):
```csharp
// Phase 338 REST-06: Auto-pair LinkedGroupId via title pattern. Hanya untuk standard mode (non PrePost).
if (CreationMode != "PrePostTest" && model.LinkedGroupId == null && !string.IsNullOrEmpty(model.Title))
{
    var counterpartId = await TryAutoDetectCounterpartGroup(model.Title, model.Category);  // ← GRDF-04: stop for Standard
    if (counterpartId.HasValue)
    {
        model.LinkedGroupId = counterpartId.Value;
        TempData["Info"] = $"Auto-paired LinkedGroupId={counterpartId.Value} ...";
    }
}
```
> D-08 forward-only: disable/remove this auto-pair for Standard creation. PrePostTest mode generates its own GroupId elsewhere (unaffected). No retroactive cleanup of old pseudo-linked rows.

---

## Shared Patterns

### Pure helper class convention (422/423 → 424)
**Source:** `Helpers/SiblingSessionQuery.cs`, `Helpers/AssessmentScoreAggregator.cs`, `Helpers/CertIssuanceRules.cs` (Phase 423 — **do not touch**).
**Apply to:** `PrePostPairing.cs`, `ExamTimeRules.cs`, and the `AssessmentScoreAggregator` promotion.
**Recipe:** `static` class in `Helpers/`; namespace `HcPortal.Helpers`; doc-comment naming the REQ + every call-site unified ("kill-drift"); pure where possible (`System.Linq` + `HcPortal.Models` only, EF-free, sync) so it is `Compile()`-able / unit-testable without a DB. `PrePostPairing` is the one exception (async EF lookup, A2).

### Existing gate / TempData+Redirect convention
**Source:** `CMPController.cs:930-974` (StartExam gate chain).
**Apply to:** GRDF-01 StartExam gate, GRDF-07 essay-empty reject (`RedirectToAction("ExamSummary")`).
**Excerpt:** `TempData["Error"] = "<friendly Bahasa Indonesia>"; return RedirectToAction("Assessment");` — server-authoritative, no view change (D-03).

### Disposable real-SQL test fixture (for any path touching `ExecuteUpdateAsync`)
**Source:** `HcPortal.Tests/GradingDedupeTests.cs:29-83` (`GradingDedupeFixture` + `NewGradingService` ctor recipe).
**Apply to:** `PrePostGatingTests` (new), parity extensions in `GradingDedupeTests`, GRDF-07 integration.
**Why:** EF Core 8 InMemory does NOT support `ExecuteUpdateAsync` (throws before logic runs — verified in `GradingDedupeTests.cs:3-7`). Use `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync`, drop-on-dispose, `[Trait("Category","Integration")]`.
**Fixture excerpt** (`GradingDedupeTests.cs:36-61`):
```csharp
public GradingDedupeFixture() {
    _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
}
public async Task InitializeAsync() {
    _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
    await using var ctx = new ApplicationDbContext(_options);
    await ctx.Database.MigrateAsync();
}
public async Task DisposeAsync() {
    await using var ctx = new ApplicationDbContext(_options);
    await ctx.Database.EnsureDeletedAsync();
}
```

### Pure predicate truth-table test (for `PrePostPairing` extractable logic + `ExamTimeRules`)
**Source:** `HcPortal.Tests/SiblingPrePostFilterTests.cs:12-45` — `Compile()` the Expression, evaluate in-memory (no DB, no Moq), one `[Fact]` per discriminator case.
**Apply to:** `ExamTimeRulesTests.cs` (pure `AllowedExamSeconds`, null-extra=0), `PrePostPairingTests.cs` (Standard→null, PostTest→target — pure portion). Async DB cases → `PrePostGatingTests` real-SQL.
**Excerpt** (`SiblingPrePostFilterTests.cs:23-36`):
```csharp
private static Func<AssessmentSession, bool> Pred(string? type) =>
    SiblingSessionQuery.SiblingPrePostAwarePredicate("Welding", "OJ", Sched.Date, type).Compile();

[Fact]
public void PreCaller_IsolatesPreTest() {
    var pred = Pred("PreTest");
    Assert.True(pred(S(1, "PreTest")));
    Assert.False(pred(S(2, "PostTest")));   // ... non-PrePost excluded
}
```

### Pure static-helper-for-controller-decision test (for GRDF-07 essay decision)
**Source:** `HcPortal.Tests/EnsureCanSubmitStandardTests.cs:17-33` — CMPController's 14-dep ctor is infeasible to construct, so the **decision logic is extracted to a pure static helper** on the controller and unit-tested directly (`CMPController.ShouldEnforceSubmitTimer(...)`).
**Apply to:** GRDF-07 — extract the essay-empty / on-time decision into a pure static helper (e.g. `bool ShouldRejectEmptyEssayOnTime(...)`) so it is unit-testable without constructing the controller; the integration path is covered by the real-SQL fixture.
**Excerpt** (`EnsureCanSubmitStandardTests.cs:21-25`):
```csharp
[Fact]
public void EnsureCanSubmitStandard_StandardType_IsEnforced()
    => Assert.True(CMPController.ShouldEnforceSubmitTimer("Standard"));
```

---

## Test Coverage Map (extend-targets all VERIFIED to exist)

| Test file | Status | What to add |
|-----------|--------|-------------|
| `HcPortal.Tests/PrePostGatingTests.cs` | **NEW** (real-SQL, reuse `GradingDedupeFixture`) | GRDF-01: Pre `!=Completed`→block; Pre `==Completed`→pass; orphan/Standard→pass-through; Pre of **another user**→pass-through (UserId filter). GRDF-03 `FindPairedPreAsync` LinkedSessionId>LinkedGroupId. |
| `HcPortal.Tests/ExamTimeRulesTests.cs` | **NEW** (pure) | `AllowedExamSeconds(d,e)==(d+e)*60`; `extra==null`→0. |
| `HcPortal.Tests/PrePostPairingTests.cs` | NEW (pure portion) OR fold into PrePostGating | Standard→null; PostTest→target. |
| `HcPortal.Tests/GradingDedupeTests.cs` | EXTEND | Parity 3-path MC>1-response (last-write-wins identical across GradeAndComplete / Compute / Aggregator). Keep `Dedupe_MultipleAnswer_NotDeduped` (MA all-or-nothing — DO NOT regress). |
| `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` + `IsQuestionCorrectTests.cs` | EXTEND | Aggregator as single-scorer with dedupe injected; correctness + LOCKED pct truth-table. |
| `HcPortal.Tests/EnsureCanSubmitStandardTests.cs` | EXTEND | GRDF-07: on-time + empty essay → block; on-time + all filled → pass (pure helper). |
| `HcPortal.Tests/EssayEmptyPendingParityTests.cs` | DO-NOT-REGRESS | timeout + empty essay still finalizes → PendingGrading (D-04, Phase 386). |

**Regression guard (RESEARCH `:506`):** do not break `GradingDedupeTests`, `AssessmentScoreAggregatorTests`, `IsQuestionCorrectTests`, `ResultsEssayCorrectnessTests`, `EssayEmptyPendingParityTests`, `SiblingPrePostFilterTests`, `EnsureCanSubmitStandardTests`, `TokenGateTests`, `EssayFinalizeRecomputeTests`.

**Run commands:** pure per-commit `dotnet test HcPortal.Tests --filter "Category!=Integration"`; full per-wave `dotnet test HcPortal.Tests` (needs `localhost\SQLEXPRESS`, `sqlcmd -C -I`). UAT browser @ `http://localhost:5270` (branch ITHandoff).

---

## No Analog Found

None. Every new/modified unit has a concrete in-repo analog (RESEARCH's central finding: "almost all scoring/pairing/timing primitives ALREADY EXIST — this phase is unify + gate, not build new").

---

## Metadata

**Analog search scope:** `Helpers/`, `Services/`, `Controllers/`, `HcPortal.Tests/`.
**Files read this session:** `SiblingSessionQuery.cs`, `AssessmentScoreAggregator.cs`, `GradingService.cs` (:80-149, :405-439), `CMPController.cs` (:287-306, :458-483, :904-993, :1620-1664), `AssessmentAdminController.cs` (:872-887), `GradingDedupeTests.cs` (:1-95), `SiblingPrePostFilterTests.cs` (:1-45), `EnsureCanSubmitStandardTests.cs` (:1-40); + Glob confirming 5 extend-target test files exist.
**Pattern extraction date:** 2026-06-24
**Scope note:** GRDF-06 deliberately excluded (covered by v32.5 on branch `main`). migration=FALSE.
