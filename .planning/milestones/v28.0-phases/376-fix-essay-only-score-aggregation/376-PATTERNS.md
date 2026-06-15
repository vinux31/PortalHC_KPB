# Phase 376: Fix Essay-Only Score Aggregation - Pattern Map

**Mapped:** 2026-06-14
**Files analyzed:** 6 (2 new, 3 modified, 1 e2e un-fixme)
**Analogs found:** 6 / 6 (semua punya precedent eksak/role-match di codebase)

> Catatan: bug-fix backend ASP.NET Core MVC + EF Core (C#). Migration=false, backend-only. Pesan user-facing (Json message, audit description, TempData, IT handoff) WAJIB Bahasa Indonesia per CLAUDE.md. Excerpt teknis di bawah = verbatim dari kode existing (sumber ekstraksi).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/AssessmentScoreAggregator.cs` (NEW) | utility (pure helper) | transform | `Helpers/ShuffleEngine.cs` | exact (static-pure-helper) |
| `Controllers/AssessmentAdminController.cs` → `FinalizeEssayGrading` (MODIFIED L3535-3565) | controller | request-response | itu sendiri (L3535-3564 inline → call helper) | self / in-place extract |
| `Controllers/AssessmentAdminController.cs` → `RecomputeEssayScores` (NEW action) | controller | batch | `BackfillProtonPenanda` (L3794-3895) + `BulkBackfillAssessment` (L843-) | exact (admin idempotent batch) |
| `Services/GradingService.cs` (RECONCILE-ONLY, no refactor) | service | request-response | `ComputeScoreAndETInternalAsync` (L331) — reference, **JANGAN ubah** | role-match (read-only reconcile) |
| `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` (NEW) | test (unit pure) | transform | `ShuffleToggleRulesTests.cs` / `ShuffleEngineTests.cs` | exact (pure, no fixture) |
| `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (NEW) | test (integration real-SQL) | CRUD | `RecordCascadeFixture` / `ProtonCompletionFixture` | exact (disposable DB IClassFixture) |
| `tests/e2e/exam-types.spec.ts` FLOW L6 (MODIFIED, un-`.fixme` ~L412-426) | test (e2e) | event-driven | itu sendiri (L412-426 sudah ada, fixmed) | self / un-fixme |

---

## Pattern Assignments

### `Helpers/AssessmentScoreAggregator.cs` (NEW — utility, pure transform)

**Analog:** `Helpers/ShuffleEngine.cs` (Phase 373 pure engine, kill-drift)

**Namespace + imports pattern** (`Helpers/ShuffleEngine.cs:1-22`):
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// ... Pure by design (only System/Linq/HcPortal.Models) → unit-testable without a database.
    /// </summary>
    public static class ShuffleEngine
    {
        public static List<int> BuildQuestionAssignment(List<AssessmentPackage> packages, ...)
```
- **Copy:** `public static class` + XML doc menyebut "Pure by design (only System/Linq/HcPortal.Models) → unit-testable without a database" + "single source of truth". Tidak inject `_context`, tidak `async`. Caller (controller) menyuplai `List<PackageQuestion>` + `List<PackageUserResponse>` yang sudah di-load EF.

**Core math pattern — PORT VERBATIM dari `AssessmentAdminController.cs:3535-3565`** (sumber ekstraksi D-02/D-04):
```csharp
int totalScore = 0;
int maxScore = 0;
foreach (var q in allQuestions)
{
    maxScore += q.ScoreValue;
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "MultipleChoice":
            var mcResp = allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
            if (mcResp != null)
            {
                var opt = q.Options.FirstOrDefault(o => o.Id == mcResp.PackageOptionId!.Value);
                if (opt != null && opt.IsCorrect) totalScore += q.ScoreValue;
            }
            break;
        case "MultipleAnswer":
            var maSelected = allResponses
                .Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                .Select(r => r.PackageOptionId!.Value).ToHashSet();
            var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;
            break;
        case "Essay":
            var essayResp = allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            if (essayResp?.EssayScore.HasValue == true) totalScore += essayResp.EssayScore.Value;
            break;
    }
}
int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;   // D-04 LOCKED
bool isPassed = finalPercentage >= session.PassPercentage;
```
- **D-04 formula LOCKED:** `maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0`. Salin persis. `IsPassed = percentage >= passPercentage` (helper terima `int passPercentage`).
- **D-05 edge:** `maxScore == 0` → `percentage = 0` (fallback existing dipertahankan). Helper TIDAK throw, TIDAK block. Log warning dilakukan di CALLER (helper murni tak punya `_logger`).
- **Return shape (DISCRETION planner):** RESEARCH usul `readonly record struct ScoreAggregateResult(int TotalScore, int MaxScore, int Percentage, bool IsPassed)`.

**Pure-function guard signature** (analog `ShuffleEngine.BuildQuestionAssignment` L39 menerima list + flags, no EF): helper menerima `IEnumerable<PackageQuestion> questions, IEnumerable<PackageUserResponse> responses, int passPercentage` → `ScoreAggregateResult`.

---

### `Controllers/AssessmentAdminController.cs` → `FinalizeEssayGrading` (MODIFIED — controller, request-response)

**Analog:** itu sendiri (in-place extract). **Sentuh HANYA blok math L3535-3565.** Ganti dengan derivasi question-set robust + panggil helper.

**PRESERVE VERBATIM (jangan sentuh — Pitfall 2 idempotency Phase 310):**
- Attributes `[HttpPost] [Authorize(Roles = "Admin, HC")] [ValidateAntiForgeryToken]` (L3466-3468).
- Early-return Completed `alreadyFinalized` (L3476-3491).
- Status switch non-PendingGrading (L3494-3504).
- Guard "semua essay dinilai" (L3523-3524).
- `ExecuteUpdateAsync` WHERE-guard `Status == PendingGrading` + `rowsAffected` capture (L3569-3600).
- Cert/Proton/notif side-effects (L3606-3660) — forward path D-03 UTUH.

**Robust question-set derivation (D-06) — GANTI L3527-3530 + log warning (D-05):**
```csharp
// Existing rapuh (L3512, L3527-3530):
var shuffledIds = packageAssignment.GetShuffledQuestionIds();
var allQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => shuffledIds.Contains(q.Id))
    .ToListAsync();
```
Fallback (RESEARCH §Robust question-set derivation): bila `shuffledIds.Count == 0` → derive dari `PackageUserResponses` session (`Select(r => r.PackageQuestionId).Distinct()`), `_logger.LogWarning(...)`. Lalu panggil helper menggantikan L3535-3565:
```csharp
var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
if (agg.MaxScore == 0)
    _logger.LogWarning("FinalizeEssayGrading: maxScore=0 session {SessionId} — anomali data, Score fallback 0.", sessionId);
int finalPercentage = agg.Percentage;
bool isPassed = agg.IsPassed;
// L3569+ ExecuteUpdateAsync TIDAK BERUBAH (pakai finalPercentage + isPassed)
```

**Logger field** (`AssessmentAdminController.cs:25,49`): `private readonly ILogger<AssessmentAdminController> _logger;` sudah ada — pakai untuk warning D-05/D-06.

---

### `Controllers/AssessmentAdminController.cs` → `RecomputeEssayScores` (NEW action — controller, batch)

**Analog utama:** `BackfillProtonPenanda` (`AssessmentAdminController.cs:3794-3895`) — admin idempotent batch dengan counters + AnyAsync guard + warn-only audit + no-info-leak.

**Attributes pattern** (`BackfillProtonPenanda` L3797-3800; lebih ketat dari FinalizeEssayGrading karena mass-repair — V4 ASVS):
```csharp
[HttpPost]
[Authorize(Roles = "Admin")]               // Admin-only (BulkBackfill L844 precedent; RESEARCH OQ4 rekomendasi)
[ValidateAntiForgeryToken]
public async Task<IActionResult> RecomputeEssayScores()
```

**Candidate predicate (D-02) — pola query batch** (`BackfillProtonPenanda` L3806-3812; field terverifikasi `AssessmentSession.Score int? L26`, `HasManualGrading bool L184`, `IsPassed bool? L44`, `PassPercentage int L30`; `AssessmentStatus.Completed`; `PackageUserResponse.EssayScore int? L32`):
```csharp
var candidateIds = await _context.AssessmentSessions
    .Where(s => s.Status == AssessmentConstants.AssessmentStatus.Completed
             && s.HasManualGrading
             && (s.Score == null || s.Score == 0))
    .Select(s => s.Id).ToListAsync();
// + per-kandidat: skip bila ada PackageUserResponse essay dengan EssayScore == null (belum lengkap dinilai)
```

**Counters + warn-only audit + no-info-leak pattern** (`BackfillProtonPenanda` L3802, L3868-3892):
```csharp
int repaired = 0, skipped = 0, alreadyOk = 0;
try
{
    // foreach kandidat: derive question-set robust (sama spt forward) → AssessmentScoreAggregator.Compute → idempotent update
    ...
    try
    {
        var actor = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
        await _auditLog.LogAsync(actor?.Id ?? "", actorName, "RecomputeEssayScores",
            $"Recompute essay-only: {repaired} diperbaiki, {skipped} dilewati, {alreadyOk} sudah benar",
            0, "AssessmentSession");
    }
    catch (Exception auditEx) { _logger.LogWarning(auditEx, "RecomputeEssayScores: audit log gagal."); }
    TempData["Success"] = $"Recompute selesai: {repaired} skor diperbaiki, {skipped} dilewati.";
}
catch (Exception ex)
{
    _logger.LogError(ex, "RecomputeEssayScores gagal.");   // detail ke log
    TempData["Error"] = "Recompute skor essay gagal. Cek log untuk detail.";   // pesan generik (Phase 334 D6)
}
return RedirectToAction("ManageAssessment");
```

**Idempotent update — Score+IsPassed ONLY (D-03, Pitfall 3)** — pola `ExecuteUpdateAsync` WHERE-guard dari `FinalizeEssayGrading:3569-3575`, distrip jadi 2 SetProperty:
```csharp
var rows = await _context.AssessmentSessions
    .Where(s => s.Id == cand.Id && (s.Score == null || s.Score == 0))   // idempotent WHERE-guard
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, agg.Percentage)
        .SetProperty(r => r.IsPassed, agg.IsPassed));
// NO Status change (Pitfall 5), NO cert (CertNumberHelper), NO Proton (_protonCompletionService),
// NO NotifyIfGroupCompleted, NO TrainingRecord (Pitfall 3/4). D-03.
```

**Audit signature (terverifikasi `Services/AuditLogService.cs:21-27`):**
```csharp
LogAsync(string actorUserId, string actorName, string actionType, string description, int? targetId = null, string? targetType = null)
```

---

### `Services/GradingService.cs` (RECONCILE-ONLY — service, JANGAN refactor)

**Analog/reference:** `ComputeScoreAndETInternalAsync` (`GradingService.cs:331-429`) + `hasEssay` interim branch (`GradingService.cs:196-234`).

**JANGAN ubah (RESEARCH Anti-Patterns + Pitfall):** `ComputeScoreAndETInternalAsync` **SKIP Essay** (`case "Essay": break;` L382-384) — tidak bisa dipakai finalize essay (akan abaikan skor manual). `hasEssay` branch L197-234 set interim Score MC+MA-only (`interimPercentage` L200) — **NORMAL & benar**, jangan disentuh.

**Reconcile = verifikasi konsistensi formula, bukan edit.** Interim formula L200 identik dengan D-04:
```csharp
int interimPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;   // L200 — sama dgn helper
```
- **Aksi planner:** tambahkan unit test no-drift (helper == formula L200/L387/L564 untuk dataset mixed) membuktikan "satu formula, dua jalur" (D-04/GRADE-02). **TIDAK** edit `GradingService.cs` (regression risk, di luar boundary).

---

### `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` (NEW — unit pure, no DB)

**Analog:** `ShuffleToggleRulesTests.cs` (pure, no fixture) + `ShuffleEngineTests.cs` (in-memory model builder).

**Pure test class pattern** (`ShuffleToggleRulesTests.cs:1-13`):
```csharp
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 376 GRADE-01/02 — pure unit tests for AssessmentScoreAggregator.
/// No DB, no fixture, no [Trait("Category","Integration")].
/// </summary>
public class AssessmentScoreAggregatorTests
{
    [Theory]
    [InlineData(...)]
    public void Compute_...(...) { Assert.Equal(expected, AssessmentScoreAggregator.Compute(...).Percentage); }
}
```

**In-memory model builder pattern** (`ShuffleEngineTests.cs:17-30` — build `PackageQuestion` + `PackageOption` tanpa DB):
```csharp
private static PackageQuestion Q(int id, string type, int scoreValue, params (int optId, bool correct)[] opts) =>
    new PackageQuestion { Id = id, QuestionType = type, ScoreValue = scoreValue,
        Options = opts.Select(o => new PackageOption { Id = o.optId, IsCorrect = o.correct }).ToList() };
```
- **Cases wajib (RESEARCH §Test Map):** essay-only 1 soal ScoreValue=100/EssayScore=80 → percentage=80; maxScore=0 → percentage=0 (no throw, D-05); mixed MC+MA+essay no-drift vs formula inline.

---

### `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (NEW — integration real-SQL)

**Analog:** `RecordCascadeFixture` (`RecordCascadeIntegrationTests.cs:21-54`) / `ProtonCompletionFixture` (`ProtonCompletionServiceTests.cs:25-61`).

**Disposable DB fixture pattern (VERBATIM struktur)** (`RecordCascadeIntegrationTests.cs:21-54`):
```csharp
public class EssayFinalizeRecomputeFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public EssayFinalizeRecomputeFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }
    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.MigrateAsync();   // pipeline penuh (no InMemory)
    }
    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]   // skip via --filter "Category!=Integration"
public class EssayFinalizeRecomputeTests : IClassFixture<EssayFinalizeRecomputeFixture>
{
    private readonly EssayFinalizeRecomputeFixture _fixture;
    public EssayFinalizeRecomputeTests(EssayFinalizeRecomputeFixture f) => _fixture = f;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    [Fact]
    public async Task Forward_EssayOnly_ScoreNotZero() { ... }   // GRADE-02
    [Fact]
    public async Task Recompute_Idempotent_OnlyTouchesScoreZero() { ... }   // D-02/D-07
    [Fact]
    public async Task Recompute_NoSideEffects_NoCertNoProtonNoNotif() { ... }   // D-03
}
```
- **Per-[Fact] seed user unik** (`RecordCascadeIntegrationTests.cs:83-89` `SeedUserAsync` pakai `Guid`), assert by id spesifik. DB lokal `HcPortalDB_Dev` TAK tersentuh (no SEED_WORKFLOW snapshot).
- **Init failure wrap** (`RecordCascadeIntegrationTests.cs:41-46` / `ProtonCompletionServiceTests.cs:48-53`): catch → EnsureDeleted best-effort → `throw new Xunit.Sdk.XunitException("...MIGRATION-CHAIN break...")`.

---

### `tests/e2e/exam-types.spec.ts` FLOW L6 (MODIFIED — un-`.fixme`)

**Analog:** itu sendiri (`exam-types.spec.ts:412-426`). Hapus baris `test.fixme(...)` L415, biarkan assertion existing.

**Current (L412-426):**
```ts
test('L6 — Worker scores 80 (DB-based verify per SURF-317-A workaround)', async () => {
    test.fixme(true, '364: essay finalize session.Score=0 ...');   // ← HAPUS baris ini
    test.setTimeout(FLOW_TIMEOUT_MS);
    const score = await db.queryScalar(
      `SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`);
    expect(score).toBe(80);   // assertion sudah benar (essay-only ScoreValue→grade 80)
    const statusOk = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${sessionId} AND Status = 'Completed'`);
    expect(statusOk).toBe(1);
});
```
- **Run command:** `npx playwright test exam-types --grep "L6" --workers=1` (DB isolation — MEMORY `reference_local_e2e_sql_env_fix`). FLOW L = essay-only (`qCards` count=1, L383-384).

---

## Shared Patterns

### Authentication / Authorization (V2/V4)
**Source:** `AssessmentAdminController.cs:3466-3468` (FinalizeEssayGrading) & `:3797-3799` (BackfillProtonPenanda, Admin-only)
**Apply to:** Endpoint `RecomputeEssayScores` baru
```csharp
[HttpPost]
[Authorize(Roles = "Admin")]          // mass-repair → Admin-only (lebih ketat dari "Admin, HC")
[ValidateAntiForgeryToken]
```

### Error handling / no-info-leak
**Source:** `AssessmentAdminController.cs:3887-3892` (Phase 334 D6)
**Apply to:** `RecomputeEssayScores` outer try/catch
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "RecomputeEssayScores gagal.");          // detail → log
    TempData["Error"] = "Recompute skor essay gagal. Cek log untuk detail.";   // generik → user (BI)
}
```

### Idempotent atomic update
**Source:** `AssessmentAdminController.cs:3569-3575` (FinalizeEssayGrading replay guard)
**Apply to:** forward finalize (preserve) + recompute (strip ke Score+IsPassed only)
```csharp
await _context.AssessmentSessions
    .Where(s => s.Id == <id> && <idempotent-guard>)
    .ExecuteUpdateAsync(s => s.SetProperty(...));
```

### Audit log (warn-only, non-fatal)
**Source:** `AssessmentAdminController.cs:3627-3641` (FinalizeEssayGrading) & `:3871-3883` (BackfillProtonPenanda); signature `Services/AuditLogService.cs:21-27`
**Apply to:** forward finalize (existing) + recompute (baru)
```csharp
var actor = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
try { await _auditLog.LogAsync(actor?.Id ?? "", actorName, "<Action>", "<deskripsi BI>", <targetId>, "AssessmentSession"); }
catch (Exception ex) { _logger.LogWarning(ex, "..."); }   // jangan break primary flow (Phase 306 D-10)
```

### Pure helper (no EF, single source of truth)
**Source:** `Helpers/ShuffleEngine.cs:22` (`public static class`, XML doc "Pure by design")
**Apply to:** `AssessmentScoreAggregator` — dipakai BERSAMA forward finalize + recompute (kill-drift D-02)

---

## No Analog Found

Tidak ada. Semua 6 file punya precedent eksak/role-match terverifikasi di codebase. Phase = ekstraksi (kill-drift) + 1 endpoint baru (pola `BackfillProtonPenanda`) + 2 file test (pola fixture/pure existing) + un-fixme. **Nyaris zero teknologi baru** — planner tak perlu fallback ke RESEARCH abstract patterns.

---

## Metadata

**Analog search scope:** `Helpers/`, `Controllers/AssessmentAdminController.cs`, `Services/GradingService.cs` + `AuditLogService.cs`, `Models/`, `HcPortal.Tests/`, `tests/e2e/`
**Files scanned:** ShuffleEngine.cs, AssessmentAdminController.cs (FinalizeEssayGrading L3460-3669, BackfillProtonPenanda L3790-3895, CreateAssessment L825-989, constructor L1-60), GradingService.cs (L180-429), RecordCascadeIntegrationTests.cs, ProtonCompletionServiceTests.cs, ShuffleToggleRulesTests.cs, ShuffleEngineTests.cs, exam-types.spec.ts (L355-426), AuditLogService.cs, AssessmentSession.cs + PackageUserResponse.cs (field verify)
**Pattern extraction date:** 2026-06-14
