# Phase 395: Mode jawaban (input asli + auto-generate) - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 8 file disentuh/dibuat (6 file produksi + 3 file test)
**Analogs found:** 12 / 13 target (1 koreksi: analog seed-hash TIDAK ADA di codebase — lihat Shared Pattern "Seed Deterministik")

> **Fase ini MEMPERLUAS halaman yang sudah ada (Phase 394).** Hampir semua kode "baru" memirror pola mapan dalam file yang SAMA. Per CLAUDE.md, prosa Bahasa Indonesia; identifier/file:line verbatim.
>
> **0 migration** (DTO `InjectAnswerSpec` + service consumption loop sudah ada & teruji dari 393).

---

## File Classification

| Target File / Method | Role | Data Flow | Closest Analog | Match Quality |
|----------------------|------|-----------|----------------|---------------|
| `Services/InjectAssessmentService.cs` → **`BuildAutoGenAnswers` (static, pure, NEW)** | service / helper | transform (target→pola) | `Helpers/AssessmentScoreAggregator.cs:26-60` (Compute) | role+flow match |
| `Services/InjectAssessmentService.cs` → **`ComputeAutoGenSeed` (static, pure, NEW)** | utility | transform (string→int hash) | ❌ TIDAK ADA SHA256 di codebase — lihat koreksi | no-analog (closest: `CertNumberHelper.cs` static-pure pattern) |
| `Controllers/InjectAssessmentController.cs` → **`PreviewInjectScore` action (NEW)** | controller | request-response (dry-run, EF-free, NO write) | `InjectAssessmentController.cs:52-66` (POST shape) + `GradingService.cs:578-585` (PreviewScoreAsync semantik) | role match (analog dry-run operasi atas session persist; baru = pra-persist) |
| `Controllers/InjectAssessmentController.cs` → **`ParseAnswerVms` (NEW)** | controller / parser | transform (JSON→VM) | `InjectAssessmentController.cs:126-139` (ParseQuestionVms) | exact |
| `Controllers/InjectAssessmentController.cs:116` → **`MapToRequest` isi `Answers`** | controller / mapper | transform (VM→DTO) | `InjectAssessmentController.cs:87-120` (Questions/Workers mapping) | exact |
| `Services/InjectAssessmentService.cs:382-391` → **rule TextAnswer-wajib (NEW, mode-guarded)** | service / validator | request-response (validasi) | `InjectAssessmentService.cs:382-391` (essay branch existing) | exact |
| `Models/InjectAssessmentDtos.cs` ATAU VM → **DTO preview `InjectPreviewRequest/Result` (opsional, NEW)** | model | transform | `Models/InjectAssessmentDtos.cs:72-80` (InjectResult record-ish), `Helpers/AssessmentScoreAggregator.cs:10` (readonly record struct) | role match |
| `ViewModels/InjectAssessmentViewModel.cs` → **`AnswersJson` + `InjectAnswerVM` (NEW)** | view-model | transform | `InjectAssessmentViewModel.cs:33` (QuestionsJson) + `:36-54` (InjectQuestionVM/InjectOptionVM) | exact |
| `Views/Admin/InjectAssessment.cshtml:399-417` → **Step-5 sub-komponen IIFE (NEW)** | component (Razor+JS) | event-driven (client-state render) | `InjectAssessment.cshtml:742-783` (injRenderQuestionList) + `:650-677` (worker picker IIFE) | role+flow match |
| `Views/Admin/InjectAssessment.cshtml:312` → **hidden `#AnswersJson` + JSON.stringify on submit (NEW)** | component | transform (client→server) | `InjectAssessment.cshtml:312` (`#QuestionsJson`) + `:868-875` (submit serialize) | exact |
| `Views/Admin/InjectAssessment.cshtml:479` → **`#btnInject` wire commit** (wujud: controller POST sudah ada; di 394 tak commit) | component → controller | request-response | `InjectAssessmentController.cs:52-66` (POST belum commit) | exact (tinggal panggil `InjectBatchAsync`) |
| `Views/Admin/InjectAssessment.cshtml:735-738` + `:832-833` → **carry-in LBL-02** | component | — | N/A (edit verbatim 4 baris) | exact |
| `HcPortal.Tests/BuildAutoGenAnswersTests.cs` (NEW) | test (unit pure) | — | `HcPortal.Tests/AssessmentScoreAggregatorTests.cs:17-70` | exact |
| `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs` (NEW) | test (integration real-SQL) | — | `HcPortal.Tests/InjectAssessmentServiceTests.cs:1-95` | exact |
| `tests/e2e/inject-assessment-395.spec.ts` (NEW) | test (e2e) | — | `tests/e2e/inject-assessment-394.spec.ts:1-50` | exact |

---

## Pattern Assignments

### 1. `BuildAutoGenAnswers` (NEW — `Services/InjectAssessmentService.cs`, static pure)

**Role:** service/helper · **Data flow:** transform (target → pola jawaban)
**Analog:** `Helpers/AssessmentScoreAggregator.cs:26-60` (formula skor — sumber kebenaran yang harus dicocokkan oleh subset-sum)

**Formula grading yang HARUS direplika (truncation int, denominator selalu termasuk essay)** — `AssessmentScoreAggregator.cs:33-59`:
```csharp
int totalScore = 0, maxScore = 0;
foreach (var q in questions)
{
    maxScore += q.ScoreValue;                                    // denominator: SEMUA soal termasuk essay (:35)
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "MultipleChoice":  // 1 opsi terpilih IsCorrect → +ScoreValue (:38-45)
        case "MultipleAnswer":  // selected.SetEquals(correct) → +ScoreValue (:46-51, all-or-nothing)
        case "Essay":           // + EssayScore.Value (BUKAN ScoreValue penuh) (:52-55)
    }
}
int percentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;   // floor/truncation (:58)
return new ScoreAggregateResult(totalScore, maxScore, percentage, percentage >= passPercentage);  // lulus '>=' (:59)
```

**Konsekuensi yang WAJIB diturunkan `BuildAutoGenAnswers`:**
- `maxScore = Σ ScoreValue SEMUA soal` (termasuk essay).
- `CeilingPercent = floor(Σ(ScoreValue MC/MA) / maxScore × 100)`. Bila `targetPercent > CeilingPercent` → `TargetReachable = false` (D-08.3 BLOCKING).
- **Re-cek `floor()` SETELAH seleksi subset** (Pitfall boundary off-by-one) memakai formula `(int)((double)total/max*100)` yang SAMA dengan `:58` — jangan percaya `ceil(target×N/100)` di mixed-weight.
- MC/MA correctness via `IsCorrect` (`:43` MC, `:49-50` MA SetEquals).

**Default ScoreValue = 10** (kasus equal-weight closed-form) — `Models/InjectAssessmentDtos.cs:19`: `public int ScoreValue { get; set; } = 10;` dan `InjectAssessmentViewModel.cs:40`.

**Struktur record hasil** — mirror pola `readonly record struct` di `AssessmentScoreAggregator.cs:10`:
```csharp
public readonly record struct ScoreAggregateResult(int TotalScore, int MaxScore, int Percentage, bool IsPassed);
```
RESEARCH merekomendasikan `AutoGenResult(List<InjectAnswerSpec> Answers, int CeilingPercent, int MaxScoreIncludingEssay, bool TargetReachable)`.

**Static-pure placement convention** (helper EF-free, unit-testable tanpa DB) — `AssessmentScoreAggregator.cs:24`: `public static class AssessmentScoreAggregator`. `BuildAutoGenAnswers` boleh `public static` di `InjectAssessmentService` (RESEARCH §Pattern 3) agar testable & reuse Phase 396.

---

### 2. `ComputeAutoGenSeed` (NEW — seed deterministik SHA-256)

**Role:** utility · **Data flow:** transform (string kanonik → int)
**Analog:** ⚠️ **TIDAK ADA analog SHA256 di codebase** — lihat Shared Pattern "Seed Deterministik" + KOREKSI di bawah.

> **KOREKSI RESEARCH (load-bearing):** RESEARCH.md `:89` & `:227` menulis `[VERIFIED: digunakan di Controllers/AssessmentAdminController.cs:2774]` untuk SHA256. **Verifikasi gagal:** `AssessmentAdminController.cs:2770-2784` adalah `GenerateSecureToken` yang memakai **`System.Security.Cryptography.RandomNumberGenerator`**, BUKAN SHA256. Grep `SHA256|ComputeHash` di seluruh `*.cs` = **0 match**. Jadi SHA-256 untuk seed adalah **API BCL yang belum pernah dipakai di repo ini** — planner perlu menulis dari nol (bukan menyalin analog). Pola crypto terdekat yang nyata ada:

```csharp
// Source: Controllers/AssessmentAdminController.cs:2770-2783 (GenerateSecureToken) — pola `using` crypto disposable.
using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
{
    rng.GetBytes(random);
}
```
Pola yang harus diikuti: `using` block + `System.Security.Cryptography.*` namespace fully-qualified. Untuk SHA256 (RESEARCH §Pattern 4): `using var sha = System.Security.Cryptography.SHA256.Create(); var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)); return BitConverter.ToInt32(hash,0) & 0x7FFFFFFF;`. **JANGAN** pakai `string.GetHashCode()` (randomized per-proses → preview≠commit).

**Convention static-pure placement:** mirror `CertNumberHelper.cs` — kelas helper `static`, method `static`, EF-free (kecuali yang butuh `ApplicationDbContext`). `ComputeAutoGenSeed` tak butuh DB → kandidat `public static` di `InjectAssessmentService` atau helper baru.

---

### 3. `PreviewInjectScore` action (NEW — `Controllers/InjectAssessmentController.cs`)

**Role:** controller · **Data flow:** request-response (dry-run, EF-free, NO DB write, NO CertNumberHelper)
**Analog struktur action POST:** `InjectAssessmentController.cs:49-66`

**Atribut & signature pattern** (RBAC + antiforgery — WAJIB sama untuk endpoint baru) — `:49-52`:
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> InjectAssessment(InjectAssessmentViewModel vm)
```

**Engine preview (preview == commit)** — pakai `AssessmentScoreAggregator.Compute` atas POCO in-memory yang dipetakan dari pola usulan (TempId sebagai Id sintetis). Pola map sudah dicontohkan RESEARCH §Code Examples; formula identik dengan finalize (`InjectAssessmentService.cs:236`):
```csharp
// InjectAssessmentService.cs:236 (finalize) — engine SAMA yang harus dipakai preview:
var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
```

**JANGAN panggil CertNumberHelper di preview** — `CertNumberHelper.cs:23-35` `GetNextSeqAsync` read-only (Max+1, no-reserve); nomor tak ter-reserve sampai commit (unique-index retry `InjectAssessmentService.cs:268-281`) → preview nomor menyesatkan (D-09).

**Catatan:** analog `GradingService.PreviewScoreAsync` (`GradingService.cs:578-585`) beroperasi atas **session ter-persist** (`AssessmentSession` + `overrideAnswers`) → BUKAN cocok untuk pra-persist. Pakai `Aggregator.Compute` atas POCO in-memory (bukan `PreviewScoreAsync`).

---

### 4. `ParseAnswerVms` (NEW — `Controllers/InjectAssessmentController.cs`)

**Role:** controller/parser · **Data flow:** transform (JSON → VM, try/catch fallback)
**Analog:** `InjectAssessmentController.cs:126-139` (`ParseQuestionVms`) — **mirror verbatim**:
```csharp
private static List<InjectAssessmentViewModel.InjectQuestionVM> ParseQuestionVms(InjectAssessmentViewModel vm)
{
    if (!string.IsNullOrWhiteSpace(vm.QuestionsJson))
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<InjectAssessmentViewModel.InjectQuestionVM>>(
                vm.QuestionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed != null) return parsed;
        }
        catch (JsonException) { /* malformed → fallback */ }
    }
    return vm.Questions ?? new();
}
```
`ParseAnswerVms` = struktur identik tapi deserialize `vm.AnswersJson` ke bentuk per-worker `[{Nip/UserId, Answers:[InjectAnswerVM-shape]}]`. **`catch (JsonException)` WAJIB** (Security Domain: malformed → fallback, bukan 500). `using System.Text.Json;` sudah ada di `:5`.

---

### 5. `MapToRequest` isi `Answers` (`Controllers/InjectAssessmentController.cs:116`)

**Role:** controller/mapper · **Data flow:** transform (VM → DTO)
**Analog (di file & method yang SAMA):** `:113-119` — saat ini hardcoded:
```csharp
req.Workers.Add(new InjectWorkerSpec
{
    Nip = nip,
    Answers = new(),   // ← :116 kosong di 394 — GANTI dengan ParseAnswerVms hasil per-worker (match by NIP/UserId)
    ManualCertNumber = vm.CertMode == InjectCertMode.Manual ? vm.ManualCertNumber : null,
    CertValidUntil = certValidUntil
});
```
Pola loop UserId→NIP sudah ada `:109-120`; `userIdToNip` dict dibangun di action `:56-58`. Untuk worker auto-gen, RESEARCH §Open-Q 1 merekomendasikan **server-otoritas**: panggil `BuildAutoGenAnswers(...)` ulang di sini dengan seed deterministik → pola identik preview.

---

### 6. Rule TextAnswer-wajib mode-guarded (`Services/InjectAssessmentService.cs:382-391`)

**Role:** service/validator · **Data flow:** request-response (kumpul error, JANGAN early-return)
**Analog (sisip di branch essay yang SAMA):** `:382-391`:
```csharp
if (qType == "Essay")
{
    if (ans.SelectedOptionTempIds.Count > 0)
        errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Soal essay tidak boleh punya opsi untuk NIP {w.Nip}." });
    if (!ans.EssayScore.HasValue)
        errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Skor essay NIP {w.Nip} wajib diisi." });
    else if (ans.EssayScore.Value < 0 || ans.EssayScore.Value > qSpec.ScoreValue)
        errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Skor essay NIP {w.Nip} di luar rentang 0..{qSpec.ScoreValue}." });
    // ← SISIP DI SINI (RESEARCH §Code Examples): guard "engaged" = essay spec dikirim DAN EssayScore.HasValue
    // if (ans.EssayScore.HasValue && string.IsNullOrWhiteSpace(ans.TextAnswer))
    //     errors.Add(new InjectRowError { Nip = w.Nip, Message = $"Teks jawaban essay NIP {w.Nip} wajib diisi (mode input asli)." });
}
```
**KRITIS:** rule WAJIB di-scope `ans.EssayScore.HasValue` (= essay engaged). Essay di-skip = OMIT spec (D-05) → tak masuk loop ini → tak terblokir. Pola error: `errors.Add(new InjectRowError { Nip = w.Nip, Message = "..." })` Bahasa Indonesia, kumpul semua (reject-all D-03), tak menulis DB. `InjectRowError` shape di `Models/InjectAssessmentDtos.cs:64-69`.

---

### 7. ViewModel `AnswersJson` + `InjectAnswerVM` (`ViewModels/InjectAssessmentViewModel.cs`)

**Role:** view-model · **Data flow:** transform
**Analog (di file yang SAMA):** `:33` (`QuestionsJson`) + `:36-54` (nested VM classes):
```csharp
public string? QuestionsJson { get; set; }   // :33 — tambah paralel: public string? AnswersJson { get; set; }

public class InjectQuestionVM   // :36 — pola nested VM mirror InjectQuestionSpec
{
    public string QuestionText { get; set; } = "";
    public int TempId { get; set; }
    public List<InjectOptionVM> Options { get; set; } = new();
}
```
Tambah `InjectAnswerVM` (mirror `InjectAnswerSpec` di `Models/InjectAssessmentDtos.cs:28-34`: `QuestionTempId`, `List<int> SelectedOptionTempIds`, `string? TextAnswer`, `int? EssayScore`) + wrapper per-worker `{ Nip/UserId, List<InjectAnswerVM> Answers }`.

---

### 8. Step-5 sub-komponen IIFE (`Views/Admin/InjectAssessment.cshtml:399-417`)

**Role:** component (Razor+JS) · **Data flow:** event-driven (client-state, nol round-trip render)
**Seam:** ganti `#step5Placeholder` (`:404`) **tanpa menyentuh** `btnPrev5`/`btnNext5` (`:408/:411`) maupun pills (seam comment `:403`).

**Analog A — render list dari client-state (`.textContent` XSS-safe + tabel)** — `injRenderQuestionList` `:742-783`:
```javascript
function injRenderQuestionList() {
    var listEl = document.getElementById('injQuestionList');
    if (injQuestions.length === 0) { listEl.innerHTML = '<div class="p-4 text-center text-muted">...</div>'; }
    else {
        var table = document.createElement('table');
        table.className = 'table table-hover table-sm mb-0 align-middle';   // ← pola tabel roster K1
        injQuestions.forEach(function (q, idx) {
            var span = document.createElement('span');
            span.title = q.QuestionText; span.textContent = q.QuestionText;   // :763 XSS-safe — WAJIB diikuti
            var badge = document.createElement('span'); badge.className = 'badge bg-secondary small';
            badge.textContent = injTypeLabel(q.QuestionType);
        });
    }
}
```

**Analog B — IIFE ber-state + selected workers** — worker picker `:650-677` + `injForm` IIFE `:492-877`:
```javascript
(function WizardController() {     // :492 — pola IIFE closure ber-state
    var injQuestions = [];          // :498 — client-state soal (sumber render Step-5)
    function injRenderSelectedNames(targetEl, checkboxes) {
        var names = (checkboxes || []).map(function (cb) { ... });
        node.textContent = names.join(', ');   // :665 XSS-safe textContent
        if (typeof targetEl.replaceChildren === 'function') targetEl.replaceChildren(node);   // :667
    }
})();
```

**Analog C — daftar pekerja terpilih** (sumber roster 1-pekerja-per-layar) — `:577`, `:605`, `:651`:
```javascript
document.querySelectorAll('#userCheckboxContainer .user-checkbox:checked')   // :577/:605 — selected workers
```
Label nama pekerja diambil via `document.querySelector('label[for="' + cb.id + '"]')` (`:653`).

**KRITIS (Pitfall TempId dangling):** rebuild state saat masuk Step-5 (event `goToStep(5)` di `:503-519`); prune answer dengan `QuestionTempId` yang tak ada lagi di `injQuestions[]`. `goToStep` luar hanya toggle `.step-panel .d-none` (`:508-512`) — tak sentuh DOM dalam, jadi sub-komponen aman independen.

**Analog opsi A/B/C/D + aria-label (K3 input-asli)** — `_InjectQuestionForm.cshtml:35-47`:
```razor
@foreach (var (letter, name) in new[] { ("A", "optionA"), ("B", "optionB"), ("C", "optionC"), ("D", "optionD") })
{
    <div class="input-group input-group-sm mb-2">
        <div class="input-group-text" style="width:36px">
            <input class="form-check-input mt-0 correct-input" type="radio" aria-label="Opsi @letter benar" />  // :40-42 a11y
        </div>
        <span class="input-group-text fw-bold">@letter</span>   // :44
    </div>
}
```

---

### 9. Hidden `#AnswersJson` + serialize on submit (`Views/Admin/InjectAssessment.cshtml`)

**Role:** component · **Data flow:** transform (client → server)
**Analog (di file yang SAMA):** hidden field `:312` + serialize `:868-875`:
```html
<input type="hidden" asp-for="QuestionsJson" id="QuestionsJson" />   <!-- :312 → tambah paralel asp-for="AnswersJson" id="AnswersJson" -->
```
```javascript
var injForm = document.getElementById('injectAssessmentForm');
if (injForm) {
    injForm.addEventListener('submit', function () {                 // :871 — listener SUBMIT yang SAMA
        var hidden = document.getElementById('QuestionsJson');
        if (hidden) hidden.value = JSON.stringify(injQuestions);     // :873
        // ← TAMBAH di listener INI (JANGAN buat listener kedua):
        // document.getElementById('AnswersJson').value = JSON.stringify(buildWorkerAnswersPayload());
    });
}
```
**Pitfall kritis (CONTEXT/RESEARCH):** lupa serialize answers = POST `Answers` kosong → semua worker grade 0 (silent). E2e WAJIB cek skor pasca-commit, bukan sekadar redirect.

---

### 10. `#btnInject` wire commit (`Views/Admin/InjectAssessment.cshtml:479` → controller)

**Role:** component → controller · **Data flow:** request-response (commit pertama milestone)
`#btnInject` `:479-481` sudah `type="submit"` dalam `<form id="injectAssessmentForm">`. Wire commit = **server-side**: di action POST `:52-66`, ganti blok no-commit `:62-65` dengan panggilan `_injectService.InjectBatchAsync(req, ...)`. Service sudah di-DI `:19/:29`. Method `InjectBatchAsync` di `Services/InjectAssessmentService.cs:42-334` (write `:189-218`, finalize `:230-243`, cert `:258-283`, audit `:289-307`). **Tidak perlu JS baru** untuk `#btnInject` — submit form sudah memicu POST.

---

### 11. Carry-in LBL-02 (`Views/Admin/InjectAssessment.cshtml:735-738` + `:832-833`)

**Role:** component · edit verbatim ~4 baris. Analog = string mapan LBL-02 ("Single Answer"/"Multiple Answer").
```javascript
// :735-738 injTypeLabel — GANTI 2 return:
function injTypeLabel(t) {
    if (t === 'MultipleChoice') return 'Pilihan Ganda';   // → 'Single Answer'
    if (t === 'MultipleAnswer') return 'Pilihan Majemuk';  // → 'Multiple Answer'
    if (t === 'Essay') return 'Essay';
    return t;
}
// :832-833 pesan validasi authoring — GANTI 2 string:
if (qType === 'MultipleChoice' && correctCount !== 1) problems.push('Pilihan Ganda harus tepat 1 jawaban benar.');     // → 'Single Answer harus tepat 1 jawaban benar.'
if (qType === 'MultipleAnswer' && correctCount < 2) problems.push('Pilihan Majemuk butuh minimal 2 jawaban benar.');  // → 'Multiple Answer butuh minimal 2 jawaban benar.'
```

---

### 12. `BuildAutoGenAnswersTests.cs` (NEW — unit pure)

**Role:** test (unit, no DB, no `[Trait Integration]`) · **Analog:** `HcPortal.Tests/AssessmentScoreAggregatorTests.cs:17-70`
```csharp
namespace HcPortal.Tests;
public class AssessmentScoreAggregatorTests
{
    // in-memory builders (no DB) — pola yang harus diikuti BuildAutoGenAnswersTests:
    private static PackageQuestion Q(int id, string type, int scoreValue, params (int optId, bool correct)[] opts) =>
        new PackageQuestion { Id = id, QuestionType = type, ScoreValue = scoreValue,
            Options = opts.Select(o => new PackageOption { Id = o.optId, IsCorrect = o.correct }).ToList() };

    [Fact]
    public void EssayOnly_Graded80_Returns80AndPassed()
    {
        var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);
        Assert.Equal(80, result.Percentage);
        Assert.True(result.IsPassed);
    }
}
```
Untuk `BuildAutoGenAnswers`: builder `InjectQuestionSpec`/`InjectOptionSpec` (POCO, `Models/InjectAssessmentDtos.cs:6-25`) in-memory, `[Fact]`/`[Theory]` per: hit-target ≥target & smallest-such (equal-weight), boundary off-by-one (mixed-weight), ceiling-essay `TargetReachable=false`, seed reproducible (sama→sama, beda-room→beda), degenerate (all-correct/1-opsi forced-correct). TANPA fixture/DB.

---

### 13. `InjectPreviewEqualsCommitTests.cs` (NEW — integration real-SQL)

**Role:** test (integration, `[Trait("Category","Integration")]`) · **Analog:** `HcPortal.Tests/InjectAssessmentServiceTests.cs:1-95`
```csharp
[Trait("Category", "Integration")]   // :59 — disposable DB, skip via "Category!=Integration"
public class InjectAssessmentServiceTests : IClassFixture<InjectAssessmentFixture>
{
    private InjectAssessmentService NewInjectService(ApplicationDbContext ctx)      // :80-81
        => new InjectAssessmentService(ctx, NewGradingService(ctx), NullLogger<InjectAssessmentService>.Instance);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string nip)   // :84-95
    {
        var u = new ApplicationUser { UserName = "inj-"+Guid.NewGuid()..., NIP = nip };       // NIP WAJIB (resolve by NIP)
        ctx.Users.Add(u); await ctx.SaveChangesAsync(); return u.Id;
    }
}
```
Fixture `InjectAssessmentFixture` (`:24-57`) = disposable `HcPortalDB_Test_{guid}` + `MigrateAsync`/`EnsureDeletedAsync` (DB lokal HcPortalDB_Dev tak tersentuh). `NewGradingService` (`:69-76`) = SALIN VERBATIM (semua fake sudah ada di `HcPortal.Tests/`). Tes: preview `Aggregator.Compute` == skor `InjectBatchAsync` finalize; skip=omit grade 0; TextAnswer-wajib reject.

---

### 14. `inject-assessment-395.spec.ts` (NEW — e2e)

**Role:** test (e2e Playwright) · **Analog:** `tests/e2e/inject-assessment-394.spec.ts:1-50`
```typescript
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';

async function loginAny(page: Page, accountKey: AccountKey) {   // :14-23 — login admin@pertamina.com / 123456
    await page.goto('/Account/Login'); await page.fill('input[name="email"]', email); ...
}
async function fillWizardToConfirm(page: Page, title: string) {   // :35-50 — drive 6-step wizard ke Konfirmasi
    await page.click('#btnNext1'); ... await expect(page.locator('#step-5')).toBeVisible();
}
```
**Pre-req runtime:** server localhost:5277 dari MAIN tree, `Authentication__UseActiveDirectory=false` (Razor di-embed saat build, lesson 354/392). Run: `cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1`. Cakupan: input-asli + auto-gen + Pratinjau + commit → skor di /CMP/Results; `#AnswersJson` terisi (anti silent-grade-0).

---

## Shared Patterns

### Authentication / RBAC
**Source:** `InjectAssessmentController.cs:40,50` — `[Authorize(Roles = "Admin, HC")]`
**Apply to:** endpoint `PreviewInjectScore` BARU (server-authoritative; non-Admin/HC → denied).
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```
Controller mewarisi `AdminBaseController` (`:17`). View override resolution `~/Views/Admin/` (`:33-36`) — endpoint preview yang return JSON tak perlu View, tapi action lain di controller ini tunduk override path.

### Anti-Forgery (CSRF)
**Source:** `InjectAssessmentController.cs:51` — `[ValidateAntiForgeryToken]`
**Apply to:** preview + commit (keduanya POST). Form `#injectAssessmentForm` sudah render token (Razor `asp-` tag-helper).

### Error Handling — reject-all batch (kumpul, jangan early-return)
**Source:** `InjectAssessmentService.cs:336-404` (`PreflightValidateAsync`) + `Models/InjectAssessmentDtos.cs:64-69` (`InjectRowError`)
**Apply to:** rule TextAnswer baru + validasi auto-gen. Pola: `errors.Add(new InjectRowError { Nip = w.Nip, Message = "..."(Bahasa Indonesia) })`, tak menulis DB, kumpul SEMUA error (D-03 reject-all).

### Deserialize JSON dengan fallback
**Source:** `InjectAssessmentController.cs:128-137` (`ParseQuestionVms`)
**Apply to:** `ParseAnswerVms`. `JsonSerializer.Deserialize<...>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })` dalam `try { } catch (JsonException) { /* fallback */ }`. Mencegah malformed → 500 (Security V5).

### XSS-safe render (`.textContent`, bukan `innerHTML` untuk data user)
**Source:** `InjectAssessment.cshtml:763` (`span.textContent = q.QuestionText`), `:665` (`node.textContent = names.join(', ')`)
**Apply to:** SEMUA render data user di sub-komponen Step-5 (UI-SPEC K1/K3 mewajibkan). `innerHTML` hanya untuk markup statis (`:747`, `:771`).

### Seed Deterministik (SHA-256) — ⚠️ NO ANALOG, tulis dari nol
**Source:** TIDAK ADA — SHA256/ComputeHash tak dipakai di codebase (grep 0 match). RESEARCH `:89/:227` klaim `:2774` KELIRU (itu `RandomNumberGenerator`).
**Apply to:** `ComputeAutoGenSeed`. Pola crypto-disposable terdekat: `AssessmentAdminController.cs:2774` (`using (var rng = ...Create())`). Gunakan `System.Security.Cryptography.SHA256.Create()` + `ComputeHash(Encoding.UTF8.GetBytes(canonical))` + `BitConverter.ToInt32(hash,0) & 0x7FFFFFFF`. **JANGAN** `string.GetHashCode()` (per-proses randomized → preview≠commit). `CompletedAt` pakai `ToString("yyyy-MM-dd")` (tanggal saja). Urutan soal stabil: `OrderBy(q => q.Order)` (match persist `InjectAssessmentService.cs:146`).

### Grading reuse — single source of truth (nol duplikasi skor)
**Source:** `Helpers/AssessmentScoreAggregator.cs:26-60` (Compute) — dipakai finalize (`InjectAssessmentService.cs:236`), preview baru, DAN harus dipatuhi `BuildAutoGenAnswers`.
**Apply to:** preview + auto-gen verify. JANGAN hitung skor di JS untuk commit (anti-pattern drift). Formula `:58` `(int)((double)total/max*100)` = truncation; lulus `:59` `>=`. Identik `GradingService.cs:144-145`.

---

## No Analog Found

| Target | Role | Data Flow | Reason |
|--------|------|-----------|--------|
| `ComputeAutoGenSeed` (SHA-256 hashing) | utility | transform | SHA256/ComputeHash **0 match** di codebase. RESEARCH klaim `:2774` keliru (= `RandomNumberGenerator`). Planner tulis dari API BCL `System.Security.Cryptography.SHA256` dari nol; pola `using`-disposable terdekat = `AssessmentAdminController.cs:2774`. |
| Subset-sum hit-target loop dalam `BuildAutoGenAnswers` | service | transform | Algoritma genuine-baru (closed-form equal-weight + greedy+verify mixed). Tak ada subset-sum di repo. Verifikasi WAJIB lewat `AssessmentScoreAggregator.Compute` (formula `:58`). Lihat RESEARCH §Pattern 3/7. |

> Untuk dua item di atas, planner mengikuti spesifikasi algoritma RESEARCH.md §Pattern 3/4/7 (bukan analog kode). Semua jalur grading/persist/cert/preview LAIN punya analog konkret di tabel atas.

---

## Metadata

**Analog search scope:** `Helpers/`, `Services/`, `Controllers/`, `Models/`, `ViewModels/`, `Views/Admin/`, `HcPortal.Tests/`, `tests/e2e/`
**Files scanned (read penuh/parsial):** `AssessmentScoreAggregator.cs`, `InjectAssessmentDtos.cs`, `InjectAssessmentController.cs`, `InjectAssessmentService.cs`, `CertNumberHelper.cs`, `GradingService.cs`, `AssessmentAdminController.cs` (seg), `InjectAssessment.cshtml` (seg), `InjectAssessmentViewModel.cs`, `_InjectQuestionForm.cshtml` (seg), `AssessmentScoreAggregatorTests.cs`, `InjectAssessmentServiceTests.cs`, `inject-assessment-394.spec.ts`
**Grep verifikasi:** `SHA256|Sha256|ComputeHash` → 0 match (koreksi RESEARCH).
**Pattern extraction date:** 2026-06-18
