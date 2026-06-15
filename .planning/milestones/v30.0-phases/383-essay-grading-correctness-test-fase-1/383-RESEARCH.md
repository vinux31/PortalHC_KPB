# Phase 383: Essay Grading Correctness + Test (Fase 1) - Research

**Researched:** 2026-06-15
**Domain:** ASP.NET Core 8 MVC display-path bugfix + EF Core 8 grading helper + xUnit regression tests
**Confidence:** HIGH (semua klaim diverifikasi byte-for-byte terhadap file:line aktual di repo)

> Catatan bahasa: per CLAUDE.md proyek ini berbahasa Indonesia. Riset ditulis Bahasa Indonesia; istilah teknis & nama simbol tetap apa adanya.

## Summary

Phase 383 adalah hotfix display-path murni di `CMP/Results/{id}`: skor % sudah benar (essay-aware lewat `AssessmentScoreAggregator.Compute`), tapi hitungan "(X/Y benar)", breakdown "Elemen Teknis", badge "Tinjauan Jawaban", dan PDF export semuanya me-recompute per-soal secara inline TANPA cabang Essay, sehingga soal essay yang sudah dinilai HC selalu dihitung salah. Solusinya satu helper murni baru `AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ) -> bool?` yang dipakai di ke-3 call-site `CMPController.Results()` + PDF export di `AssessmentAdminController` — kill-drift, sama persis pola single-source `Compute` (Phase 376).

Verifikasi kode mengonfirmasi seluruh asumsi CONTEXT/spec **akurat sampai file:line**: PDF threshold lama `>= ScoreValue/2` ada tepat di `AssessmentAdminController.cs:5017`; ke-3 site buggy di `CMPController.cs` (review-on `2258-2271`, review-off `2304-2327`, Elemen Teknis `2336-2369`) persis seperti spec; `IsEssayPending` di `2299-2300` memang hanya cek `Status == PendingGrading`. Logika MC/MA inline sudah dibaca lengkap sehingga helper bisa direplikasi byte-for-byte.

Dua temuan penting yang BELUM ada di CONTEXT/spec dan harus masuk plan: **(1) View-side gap untuk D-07** — `Views/CMP/Results.cshtml` (review row `324-393`) TIDAK pernah me-render field `UserAnswer`/`CorrectAnswer`; ia hanya render `QuestionText`, badge, dan daftar `Options`. Soal essay tidak punya Options, jadi mengeset `UserAnswer = TextAnswer` di controller TIDAK akan menampilkan apa-apa tanpa perubahan View. **(2) ECG-06 fixture** — `FinalizeEssayGrading` memakai `ExecuteUpdateAsync` (`3593-3599`) yang TIDAK didukung EF8 InMemory (pelajaran Phase 382), jadi regression test-nya WAJIB pakai disposable real-SQL fixture; sudah ada pola jadi (`EssayFinalizeRecomputeFixture`) yang tinggal diperluas.

**Primary recommendation:** Tambah `IsQuestionCorrect` ke `AssessmentScoreAggregator.cs` (MC/MA byte-for-byte dari inline existing, hanya cabang Essay baru), rewire 3 site CMPController + PDF, broaden `IsEssayPending`, TAMBAH blok View untuk render `UserAnswer` essay (D-07), dan tulis unit test (analog `AssessmentScoreAggregatorTests`) + regression test ECG-06 (perluas `EssayFinalizeRecomputeTests` pola disposable SQL). 0 migration.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Per-question correctness (MC/MA/Essay) | Helper murni (`Helpers/AssessmentScoreAggregator`) | — | EF-free, sinkron, unit-testable; single source ECG-01 (kill-drift) |
| "(X/Y benar)" count + Elemen Teknis + badge | API/Backend (`CMPController.Results`) | View (badge render) | Controller bangun ViewModel; View hanya tampilkan |
| Badge Benar/Salah/Pending render | View (`Views/CMP/Results.cshtml`) | — | Logika badge sudah benar di view; controller yg feed nilai salah |
| Render teks jawaban essay (D-07) | View (`Views/CMP/Results.cshtml`) | API (set `UserAnswer`) | **GAP**: view belum render `UserAnswer` sama sekali — butuh blok baru |
| PDF export correctness | API/Backend (`AssessmentAdminController.GeneratePerPesertaPdf`) | Helper | Unify ke `IsQuestionCorrect` (D-03) |
| Save/Finalize essay score | API/Backend (unchanged) | — | D-05 sudah benar; lock dengan test saja |

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 Centralized helper (not inline patch):** Tambah method murni, EF-free, sinkron ke `Helpers/AssessmentScoreAggregator.cs`:
  `public static bool? IsQuestionCorrect(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)`
  Return `true`=Benar, `false`=Salah, `null`=essay belum dinilai. Satu definisi dipakai web Results (count, ET, Tinjauan) DAN PDF export.
- **D-01a MC/MA replicated byte-for-byte** (verified `CMPController.cs:2259-2324` + `AssessmentScoreAggregator.cs:46-50`). Hanya cabang Essay yang baru:
  - **MultipleChoice:** benar iff opsi tunggal terpilih ber-`IsCorrect`; 0 terpilih → false.
  - **MultipleAnswer:** `selected.Count > 0 && selected.SetEquals(correctIds)` — non-empty guard (closes GRD-02).
  - **Essay:** `EssayScore.HasValue ? EssayScore.Value > 0 : null`.
- **D-02 Essay "Benar" rule = `EssayScore > 0`** (`==0`→Salah, `null`→pending). Caveat partial-credit diterima.
- **D-03 PDF export unifies to helper:** ganti `resp.EssayScore.Value >= (q.ScoreValue / 2)` (`AssessmentAdminController.cs:5017`) → `IsQuestionCorrect` (essay `>0`, null=pending). Perubahan behavior PDF disengaja.
- **D-04 No migration.** `EssayScore` sudah ada di `PackageUserResponse` & terisi. Display-path only.
- **D-05 Point 2 (save/finalize) sudah benar — lock dengan tests, no code change.** `SubmitEssayScore` (~3458-3487) + `FinalizeEssayGrading` (~3499-3671) sound.
- **D-06 IsEssayPending broadened:** dari `Status == PendingGrading` ke `question is Essay && IsQuestionCorrect(...) == null`, independen status sesi. Label tetap `AssessmentConstants.AssessmentStatus.PendingGrading` ("Menunggu Penilaian").
- **D-07 Tinjauan essay answer text:** set `UserAnswer = TextAnswer` dan `CorrectAnswer = "Dinilai manual"` (atau score string).

### Claude's Discretion

- Varian signature helper (terima full list atau pre-filtered per-soal) — asal pure & EF-free.
- Tempat regression tests (test class mana) + bentuk fixture (pakai real-SQL disposable bila kena limit `ExecuteUpdateAsync`/EF8 InMemory, pelajaran Phase 382).
- Apakah refactor logic per-tipe `Compute` agar share dengan `IsQuestionCorrect` internal — asal scoring (points) & correctness (bool?) tetap concern terpisah dan formula D-04 `Compute` tak berubah.

### Deferred Ideas (OUT OF SCOPE)

- Refactor UI Monitoring penilaian essay (tabel list worker + page "Tinjau Essay" per-worker) → Phase 384 (UIG-01..04).
- Essay rubric editor / partial-credit weighting di luar aturan biner `>0` → out of milestone.
- Bulk essay grading lintas worker.
- Pass/Fail logic (derive dari Score%, sudah benar) — tidak disentuh.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ECG-01 | Helper terpusat `IsQuestionCorrect(question, responses)` → `bool?` (MC/MA byte-for-byte, Essay `>0`); closes GRD-02 | Logika MC/MA terverifikasi `CMPController.cs:2258-2326`; pola helper murni = `AssessmentScoreAggregator.Compute` (`Helpers/AssessmentScoreAggregator.cs:26-60`); test analog `AssessmentScoreAggregatorTests.cs` |
| ECG-02 | "(X/Y benar)" sertakan essay benar — review-on (`2258-2271`) + review-off (`2304-2327`); closes RES-02 | Kedua site terverifikasi; `correctCount` di-declare `CMPController.cs:2203`, dikonsumsi `CorrectAnswers` ViewModel `2384` |
| ECG-03 | Elemen Teknis (`2336-2369`) hitung essay sesuai nilai HC | Predicate `g.Count(q => ...)` terverifikasi `2343-2358`; group key `ElemenTeknis` |
| ECG-04 | Badge "Tinjauan" tampilkan Benar/Salah essay sesuai nilai; pending="Menunggu Penilaian" terlepas status; teks `TextAnswer` tampil | Badge view sudah benar (`Results.cshtml:333-350`); **GAP D-07**: view belum render `UserAnswer` (lihat Pitfall 1) |
| ECG-05 | PDF export (`AssessmentAdminController.cs:5017`) pakai `IsQuestionCorrect` (essay `>0`) | Threshold lama `>= ScoreValue/2` terverifikasi tepat di L5017 dalam `GeneratePerPesertaPdf` (L4888) |
| ECG-06 | Regression lock `SubmitEssayScore` (persist+authz) + `FinalizeEssayGrading` (recompute+idempotent) tanpa ubah kode | `SubmitEssayScore` pakai `SaveChangesAsync` (L3477) — InMemory-OK; `FinalizeEssayGrading` pakai `ExecuteUpdateAsync` (L3593) — **WAJIB real-SQL fixture**. Pola jadi: `EssayFinalizeRecomputeTests.cs` |

## Standard Stack

Proyek brownfield — stack sudah terkunci. Tidak ada paket baru.

### Core (existing — verified)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 8.0.418 | Build/runtime | `dotnet --version` [VERIFIED: shell] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test host | `HcPortal.Tests.csproj:14` [VERIFIED: csproj] |
| xunit | 2.9.3 | Unit/integration framework | `HcPortal.Tests.csproj:15` [VERIFIED] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | In-memory DB untuk test ringan | `csproj:12` [VERIFIED] — TIDAK dukung `ExecuteUpdateAsync` |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real-SQL disposable fixture | `csproj:13` [VERIFIED] — dipakai `EssayFinalizeRecomputeFixture` |
| QuestPDF | 2026.2.2 | PDF generation (export) | komentar `AssessmentAdminController.cs:4821` [CITED: code comment] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Real-SQL fixture (ECG-06 FinalizeEssayGrading) | EF8 InMemory | InMemory throw `InvalidOperationException` pada `ExecuteUpdateAsync` SEBELUM mencapai logika diuji (Phase 382 lesson, didokumentasi di `GradingDedupeTests.cs:3-8`) — TIDAK viable untuk Finalize |
| Real-SQL fixture (ECG-06 SubmitEssayScore) | EF8 InMemory | SubmitEssayScore hanya `SaveChangesAsync`+`CountAsync`+`Join` → InMemory secara teknis bisa. TAPI demi konsistensi 1 fixture & seed shape sama, lebih bersih satukan ke real-SQL. Discretion. |
| Instantiate controller dengan fakes | Mirror logic di data-level | `AssessmentAdminController` ctor punya 12 dependency (`L33-45`). Precedent `EssayFinalizeRecomputeTests` & `GradingDedupeTests` MEMILIH mirror data-level untuk hindari DI berat — REKOMENDASI ikut pola ini |

**Installation:** Tidak ada. `dotnet build` + `dotnet test` saja.

## Architecture Patterns

### System Architecture Diagram (data flow correctness)

```
                    PackageUserResponse(s) per soal
                    (PackageOptionId | EssayScore | TextAnswer)
                              │
                              ▼
        ┌──────────────────────────────────────────────┐
        │  AssessmentScoreAggregator (Helpers/, murni)    │
        │  ┌────────────┐        ┌──────────────────────┐ │
        │  │ Compute()  │        │ IsQuestionCorrect()  │ │  ← BARU (ECG-01)
        │  │ → points % │        │ → bool? per soal     │ │
        │  │ (sudah ada)│        │ MC/MA byte-for-byte  │ │
        │  └─────┬──────┘        │ Essay: EssayScore>0  │ │
        │        │               └──────────┬───────────┘ │
        └────────┼──────────────────────────┼─────────────┘
                 │ (Path A, benar)           │ (Path B — kini lewat helper)
                 ▼                           ▼
        assessment.Score          ┌──────────────────────────────┐
        (Nilai Anda %)            │ CMPController.Results()        │
                 │                │  • site1 review-on  2258-2271 │ correctCount + IsCorrect/IsEssayPending
                 │                │  • site2 review-off 2304-2327 │ correctCount
                 │                │  • site3 ElemenTeknis 2336-69 │ ET correct count
                 │                └──────────────┬────────────────┘
                 │                               ▼
                 │                 AssessmentResultsViewModel
                 │                 (Score, CorrectAnswers, QuestionReviews[], ElemenTeknisScores[])
                 │                               ▼
                 └──────────────────► Views/CMP/Results.cshtml
                                       • "@Model.Score%" (L54)  • "(@CorrectAnswers/@Total benar)" (L55)
                                       • badge Pending/Benar/Salah (L333-350)
                                       • ⚠ TIDAK render UserAnswer (gap D-07)

        ┌──────────────────────────────────────────────┐
        │ AssessmentAdminController.GeneratePerPesertaPdf│
        │  L5017: essay correct = EssayScore>=SV/2       │ → ganti ke IsQuestionCorrect (D-03/ECG-05)
        └──────────────────────────────────────────────┘
```

### Recommended Project Structure (file impact)
```
Helpers/AssessmentScoreAggregator.cs        # + IsQuestionCorrect (ECG-01) — 1 method baru
Controllers/CMPController.cs                 # rewire 3 site + IsEssayPending (ECG-02/03/04, D-06)
Controllers/AssessmentAdminController.cs     # PDF L5017 unify (ECG-05) — NO change SubmitEssayScore/Finalize
Views/CMP/Results.cshtml                     # + blok render UserAnswer untuk essay (D-07 gap)
HcPortal.Tests/IsQuestionCorrectTests.cs     # BARU (ECG-01) atau extend AssessmentScoreAggregatorTests
HcPortal.Tests/EssayFinalizeRecomputeTests.cs# extend untuk ECG-06 (Submit + Finalize lock)
```

### Pattern 1: Pure helper sibling of Compute (kill-drift)
**What:** Method statis murni di `AssessmentScoreAggregator`, hanya `System.Linq` + `HcPortal.Models`, sinkron, EF-free.
**When to use:** Logika scoring/correctness yang dipakai >1 surface (kill-drift Phase 363/365/376).
**Example (verified shape dari design spec §4.1 — MC/MA byte-for-byte dari inline existing):**
```csharp
// Source: docs/superpowers/specs/2026-06-15-essay-grading-correctness-design.md §4.1
//         + verified terhadap CMPController.cs:2259-2324, AssessmentScoreAggregator.cs:46-50
public static bool? IsQuestionCorrect(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
{
    var list = responsesForQ as IList<PackageUserResponse> ?? responsesForQ.ToList();
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "MultipleAnswer":
        {
            var selected = list.Where(r => r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
            var correct  = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            return selected.Count > 0 && selected.SetEquals(correct);   // GRD-02 non-empty guard
        }
        case "Essay":
        {
            var essay = list.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            if (essay?.EssayScore.HasValue != true) return null;        // pending
            return essay.EssayScore.Value > 0;                          // D-02
        }
        default: // MultipleChoice
        {
            var sel = list.Where(r => r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
            if (sel.Count == 0) return false;
            var opt = q.Options.FirstOrDefault(o => sel.Contains(o.Id));
            return opt != null && opt.IsCorrect;
        }
    }
}
```

### Pattern 2: Pure-helper unit test (no DB, no fixture)
**What:** xUnit `[Fact]` dengan in-memory builder lokal, tanpa `[Trait Category=Integration]`.
**Example:** `AssessmentScoreAggregatorTests.cs:20-30` builder `Q(...)`/`Resp(...)` — REUSE persis untuk `IsQuestionCorrect`.

### Pattern 3: Disposable real-SQL integration fixture
**What:** `IAsyncLifetime` fixture buat DB `HcPortalDB_Test_{guid}@localhost\SQLEXPRESS`, `MigrateAsync`, drop on dispose; class `[Trait("Category","Integration")]`.
**Source:** `EssayFinalizeRecomputeTests.cs:19-52`.

### Anti-Patterns to Avoid
- **Recompute correctness inline (drift):** Penyebab bug ini. JANGAN tambah cabang essay inline di tiap site — panggil helper.
- **Test FinalizeEssayGrading via InMemory:** throw pada `ExecuteUpdateAsync`. Pakai real-SQL fixture.
- **Set `UserAnswer` saja tanpa update View:** field di-set tapi tak tampil (lihat Pitfall 1).
- **Ubah formula `Compute` (D-04 LOCKED):** scoring tetap, hanya tambah method correctness terpisah.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Skor sesi essay-aware | Recompute manual | `AssessmentScoreAggregator.Compute` (sudah ada) | Sudah single-source, formula D-04 LOCKED |
| Disposable test DB | Setup SQL manual per-test | `EssayFinalizeRecomputeFixture` pattern | Sudah ada, MigrateAsync + drop-on-dispose |
| Status label "Menunggu Penilaian" | String literal | `AssessmentConstants.AssessmentStatus.PendingGrading` | `Models/AssessmentConstants.cs:18` (label = nilai status) |
| Cek session submitted | Manual `== Completed \|\| == PendingGrading` | `AssessmentConstants.IsAssessmentSubmitted(status)` | `AssessmentConstants.cs:87-89` |

**Key insight:** Bug ini lahir dari hand-rolling correctness di 4 tempat. Fix = SATU helper. Jangan duplikasi lagi.

## Runtime State Inventory

Phase ini display/read-path only, NO write, NO migration, NO rename. Tetap diaudit per kategori:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — `EssayScore` sudah ada & terisi di `PackageUserResponse.EssayScore` (int?). Tidak ada backfill/migration (D-04). | None |
| Live service config | None — tidak menyentuh n8n/Datadog/external. | None |
| OS-registered state | None — tidak ada task/registrasi OS. | None |
| Secrets/env vars | None — tidak ada secret baru. | None |
| Build artifacts | `HcPortal.Tests` rebuild otomatis saat `dotnet test`. Tidak ada egg-info/package-rename. | None |

**Verified:** Tidak ada migration baru (cek konsisten dengan REQUIREMENTS "0 migration" + D-04). Tidak ada carry-migration baru untuk notify IT dari phase ini.

## Common Pitfalls

### Pitfall 1: D-07 "tampilkan TextAnswer" butuh perubahan VIEW, bukan hanya controller (HIGH)
**What goes wrong:** Plan set `UserAnswer = TextAnswer` di `QuestionReviewItem` (controller), lalu mengira essay text muncul di Tinjauan. Padahal tidak muncul.
**Why it happens:** `Views/CMP/Results.cshtml` review row (`324-393`) HANYA render `question.QuestionText`, badge, dan loop `question.Options`. Field `question.UserAnswer` dan `question.CorrectAnswer` **tidak pernah dirujuk view** (verified — grep tidak ada `UserAnswer`/`CorrectAnswer` di blok review). Essay tidak punya Options → row essay hanya tampilkan teks soal + badge, tanpa jawaban worker.
**How to avoid:** Tambah blok Razor di review row yang, bila soal essay (mis. `question.Options` kosong atau flag tipe), render `question.UserAnswer` ("Jawaban Anda: ...") dan `question.CorrectAnswer` ("Dinilai manual"). Plan ECG-04 HARUS punya task View, bukan hanya controller. **`QuestionReviewItem` sudah punya field `UserAnswer` (L28) + `CorrectAnswer` (L29)** — tidak perlu tambah model field, hanya render.
**Warning signs:** Verifikasi `dotnet run` → `CMP/Results/166` badge essay hijau tapi baris kosong tanpa teks jawaban.

### Pitfall 2: `FinalizeEssayGrading` test via InMemory crash (HIGH)
**What goes wrong:** Test ECG-06 untuk Finalize ditulis pakai EF8 InMemory → `InvalidOperationException` pada `ExecuteUpdateAsync` (`AssessmentAdminController.cs:3593`).
**Why it happens:** EF Core 8 InMemory provider tidak support `ExecuteUpdate/ExecuteDelete` (Phase 382 lesson, didokumentasi `GradingDedupeTests.cs:3-8`).
**How to avoid:** Pakai disposable real-SQL fixture (`EssayFinalizeRecomputeFixture`). `SubmitEssayScore` (hanya `SaveChangesAsync`, L3477) boleh InMemory tapi satukan ke 1 fixture untuk konsistensi.
**Warning signs:** Test gagal saat setup/aksi dengan pesan "ExecuteUpdate ... not supported by the in-memory ...".

### Pitfall 3: `IsResultsAuthorized` short-circuit memblok Results untuk non-owner (MEDIUM)
**What goes wrong:** Saat verifikasi manual `CMP/Results/166`, login user yang bukan owner & roleLevel >3 beda section → `Forbid()` (`CMPController.cs:2193`).
**Why it happens:** Authz REC-04 (`IsResultsAuthorized`, terkunci `ResultsAuthorizationTests`).
**How to avoid:** Verifikasi pakai Admin/HC (roleLevel ≤3) atau owner session. Per memory: admin@pertamina.com untuk /Admin/* lokal.
**Warning signs:** Halaman Results balik redirect/403 saat UAT.

### Pitfall 4: MA `SetEquals` symmetric — jangan salah baca arah (LOW)
**What goes wrong:** Spec helper tulis `selected.SetEquals(correct)`; inline existing tulis `correctIds.SetEquals(selectedOptionIds)` (`CMPController.cs:2263`).
**Why it happens:** `HashSet.SetEquals` simetris — kedua arah identik. Tapi pereview bisa kira "byte-for-byte" dilanggar.
**How to avoid:** Dokumentasikan bahwa keduanya ekuivalen; unit test MA exact/partial/superset/empty membuktikan.
**Warning signs:** —

### Pitfall 5: `Compute` MA branch TIDAK punya non-empty guard, helper PUNYA (LOW, by design)
**What goes wrong:** Mengira helper harus 100% sama dengan `Compute`. `Compute` MA (`AssessmentScoreAggregator.cs:50`) = `maSelected.SetEquals(maCorrect)` tanpa `Count>0` guard.
**Why it happens:** Untuk scoring, jika `maCorrect` non-empty maka set kosong tak match (0 poin) — guard tak perlu. Untuk display GRD-02, helper menambahkan `selected.Count > 0 &&` eksplisit (sesuai inline display existing `2263`/`2319`). Ini disengaja: helper meniru logic DISPLAY (CMPController), bukan SCORING (Compute).
**How to avoid:** Pahami helper = mirror display-path (CMPController inline), bukan scoring-path (Compute). MC/MA byte-for-byte mengacu ke `CMPController.cs:2259-2324`, bukan `Compute`.
**Warning signs:** —

## Code Examples

### Verified MC inline logic (review-on) — yang harus direplikasi helper
```csharp
// Source: Controllers/CMPController.cs:2265-2270 (review-on, MC/Essay branch)
else
{
    // MC / Essay path: single selection
    var single = selectedOptions.FirstOrDefault();
    isCorrect = single != null && single.IsCorrect;
}
```
Catatan: di sini essay jatuh ke cabang MC, `selectedOptions` kosong (no PackageOptionId) → `single==null` → `isCorrect=false`. Inilah bug-nya.

### Verified MA inline logic (review-on)
```csharp
// Source: Controllers/CMPController.cs:2259-2264
if ((question.QuestionType ?? "MultipleChoice") == "MultipleAnswer")
{
    var correctIds = correctOptions.Select(o => o.Id).ToHashSet();
    isCorrect = selectedOptionIds.Count > 0 && correctIds.SetEquals(selectedOptionIds);
}
```

### Verified PDF essay correctness (yang diganti — ECG-05)
```csharp
// Source: Controllers/AssessmentAdminController.cs:5014-5017 (dalam GeneratePerPesertaPdf, L4888)
else if (!string.IsNullOrEmpty(resp.TextAnswer))
{
    jawaban = resp.TextAnswer.Length > 300 ? resp.TextAnswer.Substring(0, 300) + "..." : resp.TextAnswer;
    correct = resp.EssayScore.HasValue ? (bool?)(resp.EssayScore.Value >= (q.ScoreValue / 2)) : null;  // → IsQuestionCorrect
}
```
Catatan PDF: `allQuestions` di-load dengan `.Include(q => q.Options)` (`AssessmentAdminController.cs:4858-4859`), jadi helper MC/MA aman dipanggil. Per-soal responses: `sessionResponses` (semua respons sesi) — filter ke `r => r.PackageQuestionId == q.Id` saat panggil helper.

### Verified IsEssayPending current (yang di-broaden — D-06)
```csharp
// Source: Controllers/CMPController.cs:2299-2300 (saat ini)
IsEssayPending = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading
                 && (question.QuestionType ?? "MultipleChoice") == "Essay")
// → D-06: IsEssayPending = (question.QuestionType ?? "MultipleChoice") == "Essay"
//                          && IsQuestionCorrect(question, userResponses) == null
```

### Verified unit-test builder (REUSE untuk ECG-01)
```csharp
// Source: HcPortal.Tests/AssessmentScoreAggregatorTests.cs:20-30
private static PackageQuestion Q(int id, string type, int scoreValue, params (int optId, bool correct)[] opts) =>
    new PackageQuestion { Id = id, QuestionType = type, ScoreValue = scoreValue,
        Options = opts.Select(o => new PackageOption { Id = o.optId, IsCorrect = o.correct }).ToList() };
private static PackageUserResponse Resp(int qId, int? optId = null, int? essay = null) =>
    new PackageUserResponse { PackageQuestionId = qId, PackageOptionId = optId, EssayScore = essay };
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Inline per-soal scoring di tiap surface | Helper terpusat `Compute` (scoring) + `IsQuestionCorrect` (correctness) | Phase 376 (Compute), Phase 383 (IsQuestionCorrect) | Kill-drift |
| PDF essay `>= ScoreValue/2` | `> 0` lewat helper | Phase 383 (D-03) | PDF konsisten dengan web |
| `IsEssayPending` hanya status-based | correctness-based (`== null`) | Phase 383 (D-06) | Graded essay di sesi Completed tampil benar |

**Deprecated/outdated:** Tidak ada paket deprecated. PDF threshold `>= ScoreValue/2` digantikan — bukan deprecation library, perubahan rule internal disengaja.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Test integration jalan terhadap `localhost\SQLEXPRESS` (bukan SQLBrowser/lpc override yang dipakai Playwright e2e) | Validation Architecture | LOW — fixture `EssayFinalizeRecomputeFixture:28` literal `Server=localhost\SQLEXPRESS`. Sudah dipakai & lulus di v29.0 close. Verifikasi: jalankan suite. |
| A2 | Session demo untuk verifikasi adalah id `166` (dari CONTEXT/spec) dengan N MC + 2 essay graded | Validation Architecture | LOW — id `166` dari laporan user; bila data lokal beda, pilih sesi essay graded lain. Cek DB lokal sebelum UAT. |
| A3 | Tidak ada surface ke-5 yang me-recompute correctness inline di luar 3 site + PDF | Architecture | MEDIUM — riset fokus pada surface yang disebut spec. Rekomendasi: grep `selectedIds.Count == 0` / `SetEquals` di codebase saat planning untuk konfirmasi tidak ada drift lain. |

## Open Questions

1. **Sesi reference untuk UAT manual (id 166?)**
   - What we know: laporan user menyebut `CMP/Results/166` (100% tapi 4/6). Harus ada N MC + 2 essay graded.
   - What's unclear: apakah id 166 ada di DB lokal saat ini.
   - Recommendation: planner/executor cek DB lokal (atau seed sesi sesuai SEED_WORKFLOW: temporary + local-only) sebelum verifikasi `dotnet run`. Jangan tinggalkan seed nempel (CLAUDE.md).

2. **ECG-06: mirror data-level vs invoke controller langsung**
   - What we know: precedent `EssayFinalizeRecomputeTests` & `GradingDedupeTests` MIRROR logic di data-level (hindari 12-dep ctor).
   - What's unclear: apakah plan mau "lock kode aktual controller" secara harfiah (butuh instantiate controller) atau "lock behavior" (mirror).
   - Recommendation: ikut precedent — mirror behavior data-level. Tambah komentar yang menautkan ke `SubmitEssayScore`/`FinalizeEssayGrading` file:line agar drift kode controller ketahuan. (Catatan: mirror tidak menangkap regresi authz/`[Authorize]` attribute — bila authz lock penting, tambah satu assert ringan via reflection atribut atau catat sebagai limitasi.)

3. **Authz lock SubmitEssayScore (ECG-06 menyebut "authz")**
   - What we know: `SubmitEssayScore` & `FinalizeEssayGrading` punya `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (`L3456-3457`, `L3497-3498`).
   - What's unclear: mirror data-level tidak mengeksekusi atribut authz.
   - Recommendation: lock authz dengan reflection-assert pada atribut (analog pengecekan atribut) ATAU dokumentasikan bahwa authz dijaga oleh attribute (test reflection opsional). Validasi range skor (`score < 0 || score > ScoreValue`, `L3472`) BISA di-mirror sebagai behavior test.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build/test/run | ✓ | 8.0.418 | — |
| SQL Server `localhost\SQLEXPRESS` | ECG-06 integration tests (fixture) | Asumsi ✓ (A1) | — | `--filter "Category!=Integration"` skip integration (tetap jalankan unit ECG-01) |
| QuestPDF 2026.2.2 | PDF export (ECG-05) | ✓ (terpasang) | 2026.2.2 | — |

**Missing dependencies with no fallback:** Tidak ada yang memblok unit-test (ECG-01). Integration ECG-06 butuh SQL Server lokal; bila absen, unit + manual UAT tetap bisa, integration di-skip (catat di VALIDATION).

**Missing dependencies with fallback:** ECG-06 integration → skip-able via trait filter (degradasi: hilang regression DB-level, masih ada unit + manual).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 [VERIFIED: csproj] |
| Config file | none (konvensi xUnit, project `HcPortal.Tests/HcPortal.Tests.csproj`) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (unit murni, no DB) |
| Full suite command | `dotnet test` (termasuk integration real-SQL) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ECG-01 | `IsQuestionCorrect` MC {benar/salah/unanswered}, MA {exact/partial/superset/empty→guard}, Essay {>0 true, =0 false, null pending} | pure unit | `dotnet test --filter "FullyQualifiedName~IsQuestionCorrect"` | ❌ Wave 0 (analog `AssessmentScoreAggregatorTests`) |
| ECG-02 | Sesi N MC + 2 essay graded → `CorrectAnswers == N+2` (kedua jalur review-on/off) | pure unit (panggil helper utk semua soal & sum) + manual UAT | `dotnet test --filter "FullyQualifiedName~IsQuestionCorrect"` | ❌ Wave 0 |
| ECG-03 | Elemen Teknis grup hitung essay benar (predicate `IsQuestionCorrect==true`) | pure unit (helper) + manual UAT | sama ECG-01/02 | ❌ Wave 0 |
| ECG-04 | Badge: graded essay→Benar/Salah, ungraded→pending terlepas status; `TextAnswer` tampil | manual/Playwright (Razor runtime, Phase 354 lesson) + pure unit utk `bool?`/pending mapping | `dotnet run` → `CMP/Results/{id}` manual | ❌ (view change) — manual UAT wajib |
| ECG-05 | PDF essay correctness = `>0` (sama web) | pure unit (helper hasil sama) + manual cek PDF | unit ECG-01 cover rule; PDF render manual | ❌ Wave 0 |
| ECG-06 | `SubmitEssayScore` persist `EssayScore` + range guard; `FinalizeEssayGrading` recompute incl essay + idempotent (no-op saat Completed) | integration real-SQL | `dotnet test --filter "FullyQualifiedName~EssayFinalizeRecompute"` | ⚠ extend existing `EssayFinalizeRecomputeTests.cs` |

### Reference Dataset
Satu sesi: **N MultipleChoice (semua benar) + 2 Essay (keduanya `EssayScore > 0`, fully graded), status `Completed`**, `AllowAnswerReview = true`. Ekspektasi: `CorrectAnswers == N+2` (mis. 6/6), ET hitung essay, badge essay hijau "Benar", baris essay tampilkan `TextAnswer`. Tambahan edge untuk unit: 1 essay `EssayScore == 0` (Salah), 1 essay `null` (pending "Menunggu Penilaian"). (Per CONTEXT/spec: id `166` — verifikasi keberadaan di DB lokal, A2.)

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "Category!=Integration"` (unit cepat).
- **Per wave merge:** `dotnet test` (full, termasuk integration ECG-06).
- **Phase gate:** Full suite green + `dotnet run` manual `CMP/Results/{id}` (6/6, badge hijau, teks essay tampil) sebelum `/gsd-verify-work` (per CLAUDE.md dev workflow).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/IsQuestionCorrectTests.cs` (atau extend `AssessmentScoreAggregatorTests.cs`) — covers ECG-01/02/03/05 (pure unit, reuse builder `Q`/`Resp`).
- [ ] Extend `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` — Submit persist+range guard + Finalize idempotent lock (ECG-06), pola disposable SQL existing.
- [ ] Manual/Playwright UAT untuk ECG-04 view (Razor dynamic → runtime check, Phase 354 lesson). Tidak ada test framework gap — semua paket sudah ada.
- Framework install: **None** — `xunit` + `Test.Sdk` + InMemory + SqlServer semua terpasang.

## Security Domain

> `security_enforcement` default enabled. Phase ini display/read-path + test lock; permukaan keamanan minimal.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak ubah auth |
| V3 Session Management | no | — |
| V4 Access Control | yes (lock, no change) | `CMPController.IsResultsAuthorized` (REC-04) sudah dikunci `ResultsAuthorizationTests`; `SubmitEssayScore`/`FinalizeEssayGrading` `[Authorize(Roles="Admin, HC")]` — ECG-06 mengunci tanpa mengubah |
| V5 Input Validation | yes (lock) | `SubmitEssayScore` range guard `score < 0 \|\| score > ScoreValue` (`L3472`) — ECG-06 lock |
| V6 Cryptography | no | — |

### Known Threat Patterns for ASP.NET Core MVC (stack ini)

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR baca Results sesi orang lain | Information Disclosure | `IsResultsAuthorized` (owner/role/section) — sudah ada, tak diubah |
| CSRF pada Submit/Finalize POST | Tampering | `[ValidateAntiForgeryToken]` di kedua action (`L3457`, `L3498`) — tak diubah |
| Skor essay di luar range (0..ScoreValue) | Tampering | Range guard `L3472` — ECG-06 lock |
| Display-path tak ubah authz | — | Helper `IsQuestionCorrect` murni (no DB, no user input langsung) — zero new attack surface |

## Sources

### Primary (HIGH confidence) — verified file:line di repo
- `Helpers/AssessmentScoreAggregator.cs:26-60` — `Compute` (pola helper murni, formula D-04, cabang Essay scoring L52-54)
- `Controllers/CMPController.cs:2181-2415` — `Results()` action: 3 site (2258-2271, 2304-2327, 2336-2369), `IsEssayPending` 2299-2300, authz 2193, ViewModel 2371-2390
- `Controllers/AssessmentAdminController.cs:3458-3487` (`SubmitEssayScore`), `3499-3679` (`FinalizeEssayGrading`, `ExecuteUpdateAsync` 3593), `4888-5044` (`GeneratePerPesertaPdf`, threshold 5017), ctor `33-56`
- `Models/PackageUserResponse.cs:19-32` — `PackageOptionId` (int?), `TextAnswer` (string?), `EssayScore` (int?)
- `Models/AssessmentPackage.cs:27-86` — `PackageQuestion` (QuestionType L48, ScoreValue L41, ElemenTeknis L51, Rubrik L54, Options L67) + `PackageOption.IsCorrect` L86
- `Models/AssessmentResultsViewModel.cs:24-37` — `QuestionReviewItem` (UserAnswer L28, CorrectAnswer L29, IsCorrect L30, IsEssayPending L32)
- `Models/AssessmentConstants.cs:13-21,87-89` — `PendingGrading="Menunggu Penilaian"` L18, `IsAssessmentSubmitted` L87-89
- `Views/CMP/Results.cshtml:43-417` — score L54, count L55, badge L333-350 (UserAnswer/CorrectAnswer TIDAK dirujuk)
- `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` — unit analog ECG-01
- `HcPortal.Tests/EssayFinalizeRecomputeTests.cs:19-215` — disposable SQL fixture + mirror pattern (ECG-06 analog)
- `HcPortal.Tests/GradingDedupeTests.cs:1-55` — Phase 382 ExecuteUpdateAsync/InMemory lesson, fixture pattern
- `HcPortal.Tests/ResultsAuthorizationTests.cs` — pure static authz test pattern
- `HcPortal.Tests/HcPortal.Tests.csproj` — paket & versi
- `dotnet --version` → 8.0.418 [VERIFIED: shell]

### Secondary (MEDIUM confidence)
- `docs/superpowers/specs/2026-06-15-essay-grading-correctness-design.md` — design + root cause (dikonfirmasi terhadap kode)
- `.planning/REQUIREMENTS.md` (ECG-01..06), `383-CONTEXT.md` (D-01..D-07)

### Tertiary (LOW confidence)
- Memory MEMORY.md — "415/415 green at v29.0" (klaim historis, tidak dijalankan ulang di sesi ini)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — csproj + dotnet --version diverifikasi langsung.
- Architecture (helper + call-sites + PDF): HIGH — semua file:line dibaca, MC/MA/PDF/IsEssayPending dikonfirmasi byte-for-byte.
- Pitfalls: HIGH — Pitfall 1 (view gap) & Pitfall 2 (InMemory) diverifikasi terhadap view actual + Phase 382 lesson code.
- Validation: HIGH — fixture & test analog ada di repo; hanya A1 (SQLEXPRESS reachable saat run) belum dieksekusi ulang.

**Research date:** 2026-06-15
**Valid until:** 2026-07-15 (kode brownfield stabil; revalidasi bila CMPController.Results / AssessmentAdminController PDF di-refactor sebelum eksekusi)
