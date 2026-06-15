# Phase 383: Essay Grading Correctness + Test (Fase 1) - Pattern Map

**Mapped:** 2026-06-15
**Files analyzed:** 6 (4 modify + 2 test create/extend); 1 NEW helper method inside existing file
**Analogs found:** 6 / 6 (all exact or same-file)

> Bahasa: per CLAUDE.md proyek ini berbahasa Indonesia. Catatan ditulis Bahasa Indonesia; nama simbol & excerpt kode apa adanya.
> Semua path RELATIF terhadap repo root `PortalHC_KPB-ITHandoff/`.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/AssessmentScoreAggregator.cs` (NEW method `IsQuestionCorrect`) | helper (pure/static) | transform (per-soal correctness) | `AssessmentScoreAggregator.Compute` (SAMA FILE, L26-60) | exact (sibling) |
| `Controllers/CMPController.cs` (MODIFY 3 site + IsEssayPending) | controller | request-response (read/display) | inline existing di file sendiri (L2258-2369) — diganti panggilan helper | self / exact |
| `Controllers/AssessmentAdminController.cs` (MODIFY PDF L5017) | controller | file-I/O (PDF export) | inline existing essay-correct L5014-5017 — diganti panggilan helper | self / exact |
| `Views/CMP/Results.cshtml` (MODIFY review row L324-393, D-07) | view (Razor) | request-response (render) | review row existing (option-list render) + badge L333-350 | partial (gap — lihat catatan) |
| `HcPortal.Tests/IsQuestionCorrectTests.cs` (NEW) atau extend `AssessmentScoreAggregatorTests.cs` | test (pure unit) | transform-under-test | `AssessmentScoreAggregatorTests.cs` (L17-125) | exact |
| `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (EXTEND, ECG-06) | test (integration real-SQL) | event-driven (DB lifecycle) | `EssayFinalizeRecomputeTests.cs` fixture (L19-52) + mirror (L101-149) | exact (same file) |

---

## Pattern Assignments

### `Helpers/AssessmentScoreAggregator.cs` — NEW `IsQuestionCorrect` (helper, transform)

**Analog:** SAMA FILE — `AssessmentScoreAggregator.Compute` (`Helpers/AssessmentScoreAggregator.cs:26-60`). Helper baru WAJIB jadi sibling murni: hanya `System.Linq` + `HcPortal.Models`, sinkron, EF-free, `static`. Copy struktur kelas + XML-doc-comment style + `switch (q.QuestionType ?? "MultipleChoice")` apa adanya.

**Imports / kelas pattern** (L1-30 — copy apa adanya, sudah lengkap untuk method baru):
```csharp
using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    public static class AssessmentScoreAggregator
    {
        // ... Compute(...) existing ...
    }
}
```

**Core switch pattern to MIRROR** — `Compute`'s per-tipe switch (`Helpers/AssessmentScoreAggregator.cs:36-56`). Inilah yang menjadi acuan bentuk; tapi PERHATIKAN: untuk MC/MA, helper baru me-mirror DISPLAY-path (CMPController inline), BUKAN scoring-path `Compute`. Lihat catatan byte-for-byte di bawah.
```csharp
// Source: Helpers/AssessmentScoreAggregator.cs:36-56 (Compute — SCORING, points)
switch (q.QuestionType ?? "MultipleChoice")
{
    case "MultipleChoice":
        var mcResp = respList.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
        if (mcResp != null) { var opt = q.Options.FirstOrDefault(o => o.Id == mcResp.PackageOptionId!.Value);
            if (opt != null && opt.IsCorrect) totalScore += q.ScoreValue; }
        break;
    case "MultipleAnswer":
        var maSelected = respList.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
            .Select(r => r.PackageOptionId!.Value).ToHashSet();
        var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
        if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;   // NB: NO non-empty guard (scoring)
        break;
    case "Essay":
        var essayResp = respList.FirstOrDefault(r => r.PackageQuestionId == q.Id);
        if (essayResp?.EssayScore.HasValue == true) totalScore += essayResp.EssayScore.Value;
        break;
}
```

**Target shape (verified di RESEARCH/design §4.1)** — method baru returns `bool?`:
```csharp
public static bool? IsQuestionCorrect(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
{
    var list = responsesForQ as IList<PackageUserResponse> ?? responsesForQ.ToList();
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "MultipleAnswer":
            var selected = list.Where(r => r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
            var correct  = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            return selected.Count > 0 && selected.SetEquals(correct);   // GRD-02 non-empty guard
        case "Essay":
            var essay = list.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            if (essay?.EssayScore.HasValue != true) return null;        // pending
            return essay.EssayScore.Value > 0;                          // D-02
        default: // MultipleChoice
            var sel = list.Where(r => r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
            if (sel.Count == 0) return false;
            var opt = q.Options.FirstOrDefault(o => sel.Contains(o.Id));
            return opt != null && opt.IsCorrect;
    }
}
```

**CRITICAL — MC/MA byte-for-byte source = CMPController inline (DISPLAY), bukan Compute (SCORING):**
- MC display (review-on): `Controllers/CMPController.cs:2266-2270` → `single = selectedOptions.FirstOrDefault(); isCorrect = single != null && single.IsCorrect;` (+ `if (sel.Count==0) return false` dari L2314/L2350). Helper `default` branch replikasi ini.
- MA display: `Controllers/CMPController.cs:2259-2264` → `selectedOptionIds.Count > 0 && correctIds.SetEquals(selectedOptionIds)`. Helper MA branch replikasi ini (non-empty guard ADA, beda dgn `Compute` L50 yang tak punya guard — by design, lihat Pitfall 5 RESEARCH).
- `HashSet.SetEquals` simetris → `selected.SetEquals(correct)` ≡ `correctIds.SetEquals(selectedOptionIds)` (Pitfall 4 RESEARCH).

---

### `Controllers/CMPController.cs` — MODIFY (controller, request-response)

**Analog:** kode inline di file sendiri yang akan DIGANTI panggilan helper. 3 site + IsEssayPending. ViewModel build di `L2371-2390` (konsumsi `correctCount` → `CorrectAnswers` L2384) TIDAK berubah.

**Per-soal responses sudah tersedia** — `userResponses` (review-on, L2250) / `responseLookup[qId]` (review-off, L2310) / `responseLookup[q.Id]` (ET, L2346). Helper dipanggil dengan list per-soal ini.

**Site 1 — review-on inline correctness (DIGANTI), `Controllers/CMPController.cs:2258-2271`:**
```csharp
bool isCorrect;
if ((question.QuestionType ?? "MultipleChoice") == "MultipleAnswer")
{
    var correctIds = correctOptions.Select(o => o.Id).ToHashSet();
    isCorrect = selectedOptionIds.Count > 0 && correctIds.SetEquals(selectedOptionIds);
}
else
{
    // MC / Essay path: single selection   ← BUG: essay jatuh sini → single==null → false
    var single = selectedOptions.FirstOrDefault();
    isCorrect = single != null && single.IsCorrect;
}
if (isCorrect) correctCount++;
```
→ Ganti dengan `var verdict = AssessmentScoreAggregator.IsQuestionCorrect(question, userResponses);` lalu map `true`→`correctCount++` + `IsCorrect=true`; `false`→`IsCorrect=false`; `null`→pending. (ECG-02)

**Site 1b — IsEssayPending (BROADEN, D-06), `Controllers/CMPController.cs:2299-2300`:**
```csharp
IsEssayPending = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading
                 && (question.QuestionType ?? "MultipleChoice") == "Essay")
```
→ `(question.QuestionType ?? "MultipleChoice") == "Essay" && AssessmentScoreAggregator.IsQuestionCorrect(question, userResponses) == null` (independen status sesi).

**Site 1c — D-07 essay answer text (set di QuestionReviewItem build, L2280-2301):** existing set `UserAnswer = userAnswerText` (option-join, L2273-2284) dan `CorrectAnswer = correctAnswerText` (L2276-2278, fallback `"N/A"`). Untuk essay (Options kosong, `userAnswerText==null`): set `UserAnswer = userResponses.FirstOrDefault(r => r.PackageQuestionId == qId)?.TextAnswer` dan `CorrectAnswer = "Dinilai manual"`. Field `UserAnswer`/`CorrectAnswer` SUDAH ADA di `QuestionReviewItem` (`Models/AssessmentResultsViewModel.cs:28-29`) — tidak perlu tambah field. NB: butuh perubahan VIEW juga (lihat Results.cshtml di bawah — Pitfall 1).

**Site 2 — review-off count (DIGANTI), `Controllers/CMPController.cs:2304-2327`:**
```csharp
foreach (var qId in orderedQuestionIds)
{
    if (!questionLookup.TryGetValue(qId, out var question)) continue;
    var selectedIds = responseLookup[qId].Where(r => r.PackageOptionId != null)
        .Select(r => r.PackageOptionId!.Value).ToHashSet();
    if (selectedIds.Count == 0) continue;   // ← BUG: essay (no option) di-skip, tak pernah dihitung
    if ((question.QuestionType ?? "MultipleChoice") == "MultipleAnswer") { ... if (correctIds.SetEquals(selectedIds)) correctCount++; }
    else { var selectedOpt = ...; if (selectedOpt != null && selectedOpt.IsCorrect) correctCount++; }
}
```
→ `correctCount += AssessmentScoreAggregator.IsQuestionCorrect(question, responseLookup[qId].ToList()) == true ? 1 : 0;` (buang guard `Count==0 continue`). (ECG-02)

**Site 3 — Elemen Teknis predicate (DIGANTI), `Controllers/CMPController.cs:2336-2369`:**
```csharp
var correct = g.Count(q =>
{
    var selectedIds = responseLookup[q.Id].Where(r => r.PackageOptionId != null)
        .Select(r => r.PackageOptionId!.Value).ToHashSet();
    if (selectedIds.Count == 0) return false;   // ← BUG: essay → false
    if ((q.QuestionType ?? "MultipleChoice") == "MultipleAnswer") { ... return correctIds.SetEquals(selectedIds); }
    var sel = q.Options.FirstOrDefault(o => selectedIds.Contains(o.Id));
    return sel != null && sel.IsCorrect;
});
```
→ `var correct = g.Count(q => AssessmentScoreAggregator.IsQuestionCorrect(q, responseLookup[q.Id].ToList()) == true);` (ECG-03). `Percentage` formula L2364 (`(double)correct/total*100`) TIDAK berubah.

---

### `Controllers/AssessmentAdminController.cs` — MODIFY PDF export (controller, file-I/O)

**Analog:** inline essay-correct di `GeneratePerPesertaPdf` (private, signature `L4888-4894`). Questions di-load `.Include(q => q.Options)` (`L4858-4861`) → helper MC/MA aman. Per-soal responses: filter `sessionResponses` ke `r => r.PackageQuestionId == q.Id`.

**Current essay correctness (DIGANTI), `Controllers/AssessmentAdminController.cs:5014-5017`:**
```csharp
else if (!string.IsNullOrEmpty(resp.TextAnswer))
{
    jawaban = resp.TextAnswer.Length > 300 ? resp.TextAnswer.Substring(0, 300) + "..." : resp.TextAnswer;
    correct = resp.EssayScore.HasValue ? (bool?)(resp.EssayScore.Value >= (q.ScoreValue / 2)) : null;   // ← ganti
}
```
→ `correct = AssessmentScoreAggregator.IsQuestionCorrect(q, sessionResponses.Where(r => r.PackageQuestionId == q.Id));` (essay `> 0`, null=pending). (ECG-05, D-03 — perubahan behavior PDF disengaja). Tambah `using HcPortal.Helpers;` bila belum ada. `statusColor`/`statusText` mapping di `L5021-5024` (Green/Red/Grey + "✓ Benar"/"✗ Salah"/"— Pending") TIDAK berubah — `bool?` cocok langsung.

**NO CHANGE:** `SubmitEssayScore` (`L3458-3487`) & `FinalizeEssayGrading` (`L3499+`) — D-05 lock-with-tests only.

---

### `Views/CMP/Results.cshtml` — MODIFY review row (view, render) — D-07/ECG-04

**Analog:** review row existing (`Views/CMP/Results.cshtml:324-393`). Badge logic (Pending/Benar/Salah, `L333-350`) SUDAH BENAR — tinggal di-feed nilai benar dari controller. **GAP (Pitfall 1 RESEARCH, HIGH):** row HANYA render `QuestionText` (L329), badge (L333-350), dan loop `question.Options` (L356-390). Field `question.UserAnswer` / `question.CorrectAnswer` TIDAK PERNAH dirujuk view. Essay tak punya Options → baris essay kosong tanpa teks jawaban meski controller set `UserAnswer`.

**Existing badge block (TIDAK diubah, sudah benar)** — `Views/CMP/Results.cshtml:333-350`:
```cshtml
@if (question.IsEssayPending)
{
    <span class="badge text-bg-secondary"><i class="bi bi-hourglass-split me-1"></i>Menunggu Penilaian</span>
}
else if (question.IsCorrect)
{
    <span class="badge text-bg-success"><i class="bi bi-check-circle-fill me-1"></i>Benar</span>
}
else
{
    <span class="badge text-bg-danger"><i class="bi bi-x-circle-fill me-1"></i>Salah</span>
}
```

**Existing options loop (analog untuk gaya markup)** — `Views/CMP/Results.cshtml:354-391`. Render `<div class="list-group">` dgn item per opsi. Untuk essay (Options kosong) loop ini tidak menghasilkan apa-apa.

**ACTION (D-07):** tambah blok Razor di review row (mis. setelah options loop, sebelum `</div>` L392) yang, bila essay (mis. `!question.Options.Any()` atau `question.UserAnswer != null`), render teks jawaban worker — pakai gaya `list-group-item` agar konsisten:
```cshtml
@if (!question.Options.Any() && !string.IsNullOrEmpty(question.UserAnswer))
{
    <div class="list-group-item">
        <small class="text-muted d-block">Jawaban Anda:</small>
        <span>@question.UserAnswer</span>
        <small class="text-muted d-block mt-2">@question.CorrectAnswer</small>
    </div>
}
```
Verifikasi WAJIB runtime (`dotnet run` → `CMP/Results/{id}`) — Razor dynamic, grep+build tak cukup (Phase 354 lesson). ECG-04 plan HARUS punya task View, bukan hanya controller.

---

### `HcPortal.Tests/IsQuestionCorrectTests.cs` (NEW) atau extend `AssessmentScoreAggregatorTests.cs` (test, pure unit)

**Analog:** `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` (`L17-125`). REUSE persis builder helper `Q(...)` + `Resp(...)`. TANPA `[Trait Category=Integration]`, tanpa DB/fixture.

**Builder helpers to COPY VERBATIM** — `HcPortal.Tests/AssessmentScoreAggregatorTests.cs:20-30`:
```csharp
private static PackageQuestion Q(int id, string type, int scoreValue, params (int optId, bool correct)[] opts) =>
    new PackageQuestion
    {
        Id = id, QuestionType = type, ScoreValue = scoreValue,
        Options = opts.Select(o => new PackageOption { Id = o.optId, IsCorrect = o.correct }).ToList()
    };

private static PackageUserResponse Resp(int qId, int? optId = null, int? essay = null) =>
    new PackageUserResponse { PackageQuestionId = qId, PackageOptionId = optId, EssayScore = essay };
```

**Fact structure to COPY** — `AssessmentScoreAggregatorTests.cs:33-45` (Arrange `Q[]`/`Resp[]` → Act call helper → Assert):
```csharp
[Fact]
public void EssayOnly_Graded80_Returns80AndPassed()
{
    var questions = new[] { Q(1, "Essay", 100) };
    var responses = new[] { Resp(1, essay: 80) };
    var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);
    Assert.Equal(80, result.TotalScore);
    // ...
}
```

**Test matrix `IsQuestionCorrect` (ECG-01, dari CONTEXT specifics + design §4.4):**
- MC: correct (`true`), incorrect (`false`), unanswered/0-selected (`false`).
- MA: exact set (`true`), partial-subset (`false`), superset (`false`), empty/unanswered (`false` — non-empty guard GRD-02).
- Essay: `EssayScore > 0` (`true`), `== 0` (`false`), `null` (`null` pending).
- ECG-02 reproduksi: N MC benar + 2 essay `>0` → loop semua soal, sum `IsQuestionCorrect(...)==true` → `N+2` (mis. 6/6).

---

### `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` — EXTEND (test, integration real-SQL) — ECG-06

**Analog:** SAMA FILE — disposable real-SQL fixture + mirror-data-level pattern. WAJIB real-SQL (bukan InMemory): `FinalizeEssayGrading` pakai `ExecuteUpdateAsync` (`AssessmentAdminController.cs:3593`) yang EF8 InMemory tak dukung (Pitfall 2, Phase 382 lesson `GradingDedupeTests.cs:3-8`).

**Fixture pattern to REUSE** — `HcPortal.Tests/EssayFinalizeRecomputeTests.cs:19-52` (`IAsyncLifetime`, DB `HcPortalDB_Test_{guid}@localhost\SQLEXPRESS`, MigrateAsync, drop-on-dispose):
```csharp
public class EssayFinalizeRecomputeFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options => _options;
    public EssayFinalizeRecomputeFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }
    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}
```

**Class trait + seed helper to REUSE** — `EssayFinalizeRecomputeTests.cs:54-99` (`[Trait("Category","Integration")]`, `IClassFixture<>`, `SeedUserAsync`, `SeedEssayOnlyAsync`):
```csharp
[Trait("Category", "Integration")]
public class EssayFinalizeRecomputeTests : IClassFixture<EssayFinalizeRecomputeFixture>
{
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
    // SeedUserAsync (L62-68), SeedEssayOnlyAsync (L71-99) — seed session+pkg+question+response+assignment
}
```

**Mirror-data-level pattern (precedent — hindari 12-dep ctor)** — `EssayFinalizeRecomputeTests.cs:101-149`. ECG-06 lock me-mirror behavior, BUKAN instantiate controller (Open Question #2/#3 RESEARCH):
- `SubmitEssayScore` lock (ECG-06): mirror persist `EssayScore` + range guard `score < 0 || score > ScoreValue` (`AssessmentAdminController.cs:3472`) — InMemory-able tapi satukan ke fixture untuk konsistensi.
- `FinalizeEssayGrading` lock (ECG-06): mirror recompute via `AssessmentScoreAggregator.Compute` (essay-aware) + idempotent. Pakai `ForwardAggregateAsync` (`L101-114`) sbg template.
- Caveat (RESEARCH OQ#3): mirror TIDAK eksekusi `[Authorize(Roles="Admin, HC")]` (`L3456-3457`/`L3497-3498`). Authz lock opsional via reflection-assert atribut, atau dokumentasikan limitasi.

---

## Shared Patterns

### Kill-drift via single-source helper
**Source:** `Helpers/AssessmentScoreAggregator.cs` (Phase 363/365/376 pattern).
**Apply to:** ke-3 site CMPController + PDF AssessmentAdminController. SATU definisi `IsQuestionCorrect` dipakai semua surface display. JANGAN tambah cabang essay inline per-site (itulah akar bug ini — 4 tempat hand-rolled correctness).

### Status label konstanta (jangan string literal)
**Source:** `Models/AssessmentConstants.cs:18` — `PendingGrading = "Menunggu Penilaian"`.
**Apply to:** badge pending (View sudah pakai label ini L336; D-06 broaden tetap pakai konstanta). `AssessmentConstants.IsAssessmentSubmitted(status)` (`L87-89`) untuk cek sesi submitted (CMPController L2196).

### Per-soal response lookup (multi-row MA-safe)
**Source:** `Controllers/CMPController.cs:2230` — `responseLookup = packageResponses.ToLookup(r => r.PackageQuestionId)` (SURF-317-A: MA insert 1 row/option → `ToDictionary` throws, `ToLookup` aman).
**Apply to:** saat panggil helper, beri list per-soal (`responseLookup[qId].ToList()` / `userResponses`). PDF: `sessionResponses.Where(r => r.PackageQuestionId == q.Id)`.

### Pure-helper unit test (no DB)
**Source:** `HcPortal.Tests/AssessmentScoreAggregatorTests.cs:20-30` builder `Q`/`Resp`.
**Apply to:** test ECG-01. Run cepat `dotnet test --filter "Category!=Integration"`.

### Disposable real-SQL integration fixture
**Source:** `HcPortal.Tests/EssayFinalizeRecomputeTests.cs:19-52`.
**Apply to:** test ECG-06 (Finalize butuh `ExecuteUpdateAsync` → real SQL). Run penuh `dotnet test`.

---

## No Analog Found

Tidak ada. Semua 6 file punya analog exact/same-file. Satu CATATAN bukan "no analog" tapi GAP:

| File | Role | Data Flow | Catatan |
|------|------|-----------|---------|
| `Views/CMP/Results.cshtml` (D-07 essay answer text) | view | render | Analog markup ADA (options loop L354-391, badge L333-350) TAPI tidak ada blok existing yang me-render `UserAnswer`/`CorrectAnswer` per-soal → butuh blok Razor BARU (bukan copy 1:1). Verifikasi runtime wajib (Phase 354 lesson). |

---

## Metadata

**Analog search scope:** `Helpers/`, `Controllers/`, `Views/CMP/`, `Models/`, `HcPortal.Tests/`
**Files scanned:** 8 (AssessmentScoreAggregator.cs, CMPController.cs, AssessmentAdminController.cs, Results.cshtml, AssessmentResultsViewModel.cs, AssessmentScoreAggregatorTests.cs, EssayFinalizeRecomputeTests.cs, GradingDedupeTests.cs ref)
**Migration impact:** 0 (D-04 — display/read-path only)
**Pattern extraction date:** 2026-06-15
</content>
</invoke>
