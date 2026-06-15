# Phase 375: Test & UAT - Pattern Map

**Mapped:** 2026-06-13
**Files analyzed:** 4 (1 e2e spec NEW, 1 xUnit sweep NEW, 1 UAT doc NEW, 1 controller verify-only)
**Analogs found:** 4 / 4 (semua exact/role-match — phase test+UAT, sumber pola = CONTEXT code_context, RESEARCH skipped)

> Phase test+UAT (final v27.0 Shuffle Toggle). **TIDAK ada production source code dibuat** — hanya test code + dokumen UAT. Semua interaksi codebase read-only. Pola diambil dari analog NYATA di repo (bukan abstraksi).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `tests/e2e/shuffle.spec.ts` (NEW) | test (Playwright e2e) | request-response (browser UI render + save-PRG) | `tests/e2e/image-in-assessment.spec.ts` (Phase 355) | exact (template proven: wizard-create + DB snapshot beforeAll/afterAll) |
| `HcPortal.Tests/ShuffleEngineTests.cs` (APPEND consolidation `[Theory]`) atau file baru `ShuffleModeMatrixTests.cs` | test (xUnit pure unit) | transform (engine in-memory, seed-stable) | `HcPortal.Tests/ShuffleEngineTests.cs` (12 test) + `ShuffleToggleRulesTests.cs` (3 `[Theory]`) | exact (engine under test + `new Random(42)` pattern) |
| `.planning/phases/375-test-uat/375-HUMAN-UAT.md` (NEW) | doc (UAT result) | record-keeping | `.planning/phases/373-shuffle-engine-read-logic-reshuffle/373-HUMAN-UAT.md` (status `partial`) | exact (same milestone, same format) |
| `Controllers/CMPController.cs` (verify-only, SHUF-15) | controller | n/a (no edit expected) | self (CMPController.cs:989) | verify-only — sudah bersih (lihat §SHUF-15) |

---

## Pattern Assignments

### `tests/e2e/shuffle.spec.ts` (test, Playwright e2e)

**Analog:** `tests/e2e/image-in-assessment.spec.ts` (Phase 355 — UAT proven template). Reuse helper: `tests/e2e/helpers/examTypes.ts` (`createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`), `tests/helpers/{auth.ts, accounts.ts, dbSnapshot.ts}`.

**Scope (D-02a):** 5 skenario **render + save-PRG** sisi ManagePackages (BUKAN exam-taking). Propagate-detail TIDAK di-assert ulang (sudah unit `ShufflePropagationTests`/`ShuffleUpdateEndpointTests`). Exam-taking effect = manual browser (D-03), bukan e2e.

**Imports pattern** (image-in-assessment.spec.ts lines 21-25 — copy verbatim, drop image-only imports):
```typescript
import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
```

**Serial mode + describe** (image-in-assessment.spec.ts lines 40-42):
```typescript
test.describe.configure({ mode: 'serial' });   // WAJIB — DB isolation (playwright.config fullyParallel:false)

test.describe('Phase 375 — Shuffle Toggle ManagePackages (UAT e2e)', () => {
```

**DB snapshot lifecycle — beforeAll/afterAll** (image-in-assessment.spec.ts lines 44-69 — copy struktur, drop fs upload-cleanup karena shuffle tak upload file):
```typescript
let snapshotPath: string;

test.beforeAll(async () => {
  const dir = (await db.queryString(
    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
  )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre375-${ts}.bak`;
  await db.backup(snapshotPath);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  let restoreError: unknown = null;
  try {
    await db.restore(snapshotPath);
    const fs = await import('node:fs');
    try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
  } catch (e) { restoreError = e; }
  if (restoreError) throw restoreError;
});
```

**Wizard-create skeleton** (image-in-assessment.spec.ts lines 76-115 — D-06 WAJIB pakai `createAssessmentViaWizard`, JANGAN flat-form):
```typescript
// Title WAJIB match ^(Pre|Post)\s*Test\s+.+$ untuk standard non-PrePostTest (AssessmentAdminController:866-874).
const assessmentTitle = `Pre Test OJT SHUF375 ${Date.now()}`;
await login(page, 'admin');

await createAssessmentViaWizard(page, {
  title: assessmentTitle,
  category: 'OJT',
  scheduleDate: today(),         // const today = () => new Date().toISOString().slice(0,10);
  scheduleTime: '00:01',
  durationMinutes: 60,
  passPercentage: 50,
  allowAnswerReview: true,
  generateCertificate: false,
  participantEmails: ['rino.prasetyo@pertamina.com'],
});

// Dismiss static success modal (Pitfall 3) → arrive di ManagePackages
await page.locator('#modal-manage-btn').click();
await page.waitForLoadState('networkidle');
// page sekarang di /Admin/ManagePackages?assessmentId={id}

const pkgId = await createDefaultPackage(page);                    // returns packageId
await addQuestionViaForm(page, pkgId, {
  type: 'MultipleChoice', text: 'Soal SHUF375 #1',
  options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 50,
});
```

**Exact ManagePackages route + shuffle card selectors** (Views/Admin/ManagePackages.cshtml:83-132 + AssessmentAdminController `[Route("Admin/[action]")]`):
```typescript
// Route: /Admin/ManagePackages?assessmentId={id}  (controller [Route("Admin/[action]")])
// Card wrap: @if (ViewBag.HideShuffleToggle != true) → card .card dengan <h5> "Pengacakan Soal & Jawaban"

const card        = page.locator('.card', { hasText: 'Pengacakan Soal & Jawaban' });
const swQuestions = page.locator('#shuffleQuestions');   // checkbox "Acak Soal"
const swOptions   = page.locator('#shuffleOptions');     // checkbox "Acak Pilihan Jawaban"
const sizeWarning = page.locator('#shuffleSizeWarning'); // alert-warning §9 (d-none saat tak relevan)
const lockBanner  = card.locator('.alert-info', { hasText: 'Pengaturan pengacakan terkunci' });
const postReminder= card.locator('.alert-warning', { hasText: 'Pre diatur OFF, Post masih ON' });
const saveBtn     = card.locator('button[type="submit"]:has-text("Simpan Pengaturan")');
```

**Scenario 1 — card render + saved-state + Simpan → success PRG** (assert `.alert-success` saja, D-02a "TempData success only"):
```typescript
// ManagePackages success-alert: Views/Admin/ManagePackages.cshtml:20-22
//   @if (TempData["Success"] != null) → <div class="alert alert-success ...">{msg}</div>
// Endpoint UpdateShuffleSettings (AssessmentAdminController:5311) set:
//   TempData["Success"] = "Pengaturan pengacakan berhasil disimpan."; → RedirectToAction("ManagePackages")
await expect(card).toBeVisible();
await expect(swQuestions).toBeChecked();   // default ON (migration default, Phase 372)
await expect(swOptions).toBeChecked();
await swQuestions.uncheck();
await saveBtn.click();
await page.waitForLoadState('networkidle');
await expect(page.locator('.alert-success', { hasText: 'berhasil disimpan' })).toBeVisible();
```

**Scenario 2 — lock disabled + banner** (assessment dgn peserta started → `IsShuffleLocked`):
```typescript
// View: @(isShuffleLocked ? "disabled" : "") pada kedua switch + saveBtn; alert-info banner muncul.
await expect(lockBanner).toBeVisible();
await expect(swQuestions).toBeDisabled();
await expect(swOptions).toBeDisabled();
await expect(saveBtn).toBeDisabled();
```

**Scenario 3 — reminder Pre/Post** (Pre OFF + Post ON → alert di Post, SHUF-13):
```typescript
// View:373 @if (ViewBag.IsPostSession==true && ViewBag.PreShuffleQuestions==false && sqChecked)
//   → alert-warning "Pre diatur OFF, Post masih ON — sengaja?"
await expect(postReminder).toBeVisible();   // di halaman Post; di Pre TIDAK ada (no cascade)
```

**Scenario 4 — warning §9 live-JS flip** (multi-paket ukuran beda, SHUF-12):
```typescript
// View:324-332 JS addEventListener('change') #shuffleQuestions → classList.toggle d-none.
// Multi-paket mismatch + Acak Soal OFF → #shuffleSizeWarning visible; flip ON → hidden.
await swQuestions.uncheck();
await expect(sizeWarning).toBeVisible();   // tidak ada d-none
await swQuestions.check();
await expect(sizeWarning).toBeHidden();    // d-none kembali (no reload — live JS)
```

**Scenario 5 — hide Proton-Th3 / Manual** (SHUF-14, card tidak dirender):
```typescript
// View:84 @if (ViewBag.HideShuffleToggle != true) — untuk Proton Tahun 3 / Manual → card absent.
await expect(page.locator('.card', { hasText: 'Pengacakan Soal & Jawaban' })).toHaveCount(0);
```

**Text-based option selectors (shuffle-safe)** — saat assert urutan (manual D-03, juga panduan e2e bila perlu): pakai `label:has-text("${optionText}")` / `label.list-group-item { hasText }`, BUKAN `.nth()` posisi (image-in-assessment.spec.ts lines 144-148; examTypes.ts:293-296 `checkMAOptionsForQuestion`).

---

### `HcPortal.Tests/ShuffleEngineTests.cs` — consolidation sweep `[Theory]` (test, pure unit)

**Analog:** `HcPortal.Tests/ShuffleEngineTests.cs` (12 test, di-APPEND atau file baru) + `ShuffleToggleRulesTests.cs` (pola `[Theory]/[InlineData]`).

**D-01 / D-01a:** Sweep = **high-level invariant per-mode** (single source of truth eksplisit), di ATAS 27 test existing — JANGAN duplikasi assertion detail. Reuse pola `new Random(42)` seed-stability. Suite baseline 347 → 348+ (SC#1 hijau termasuk sweep).

**ShuffleEngine API signatures** (Helpers/ShuffleEngine.cs — engine under test):
```csharp
public static List<int> BuildQuestionAssignment(
    List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng);
//   ON  → BuildCrossPackageAssignment (1pkg=acak; ≥2pkg=sampling K-min ET-balanced)
//   OFF 1pkg → questions.OrderBy(q.Order).Select(q.Id)         [SHUF-05: urut, no shuffle]
//   OFF ≥2pkg → packagesWithQuestions[workerIndex % count] paket UTUH urut Order  [SHUF-06]
//              (filter Questions.Count>0 SEBELUM modulo → guard DivideByZero)

public static Dictionary<int, List<int>> BuildOptionShuffle(
    IEnumerable<PackageQuestion> questions, bool shuffleOptions, Random rng);
//   ON  → dict[questionId] = Fisher-Yates option Ids ;  OFF → empty dict (caller serializes "{}")

public static void Shuffle<T>(List<T> list, Random rng);   // Fisher-Yates in-place
```

**In-memory package builder** (ShuffleEngineTests.cs lines 18-30 — reuse `Pkg` helper verbatim; tiap question dapat 2 option ids `id*10`, `id*10+1`):
```csharp
private static AssessmentPackage Pkg(int packageNumber, params (int id, int order, string? et)[] qs)
{
    var p = new AssessmentPackage { PackageNumber = packageNumber, Id = packageNumber };
    foreach (var (id, order, et) in qs)
        p.Questions.Add(new PackageQuestion {
            Id = id, Order = order, ElemenTeknis = et,
            Options = { new PackageOption { Id = id * 10 }, new PackageOption { Id = id * 10 + 1 } }
        });
    return p;
}
```

**Seed-stability assertion** (ShuffleEngineTests.cs lines 115-126 — pola `new Random(42)` dipanggil dua kali, output identik):
```csharp
var a = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
var b = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
Assert.Equal(a, b);                                        // seed-stable
Assert.Equal(new HashSet<int> { 10, 11, 12, 13 }, a.ToHashSet());   // berisi SEMUA id
```

**Representative `[Theory]/[InlineData]` mode-matrix sweep** (gabung pola ShuffleEngineTests.cs:48-65 round-robin + ShuffleToggleRulesTests.cs:14-22 Theory structure):
```csharp
// High-level per-mode invariant (D-01a — bukan dobel detail). Contoh shape:
[Theory]
// shuffleQuestions, shuffleOptions, packageCount → invariant yang dicek di body
[InlineData(true,  true,  1)]   // ON  1pkg → shuffled & seed-stable; opt dict non-empty
[InlineData(false, false, 1)]   // OFF 1pkg → urut asli q.Order; opt dict empty
[InlineData(false, true,  2)]   // OFF ≥2pkg → paket UTUH round-robin index-stabil; opt dict non-empty
[InlineData(true,  false, 2)]   // ON  ≥2pkg → sampling K-min; opt dict empty
public void ModeMatrix_Invariant(bool shuffleQuestions, bool shuffleOptions, int packageCount)
{
    var packages = BuildPackages(packageCount);
    var qIds  = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions, workerIndex: 0, rng: new Random(42));
    var qIds2 = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions, workerIndex: 0, rng: new Random(42));
    Assert.Equal(qIds, qIds2);   // determinisme seed (semua mode)
    // per-mode high-level invariant (OFF 1pkg = urut; OFF ≥2 = paket utuh; opt dict non/empty) ...
}

// + 1 guard test all-empty → no DivideByZero (pola ShuffleEngineTests.cs:87-94):
[Fact]
public void AllPackagesEmpty_NoDivideByZero()
{
    var ex = Record.Exception(() =>
        ShuffleEngine.BuildQuestionAssignment(new List<AssessmentPackage> { Pkg(1), Pkg(2) }, false, 0, new Random()));
    Assert.Null(ex);
}
```

**Option shuffle ON/OFF invariant** (ShuffleEngineTests.cs:144-161):
```csharp
var on  = ShuffleEngine.BuildOptionShuffle(questions, shuffleOptions: true,  rng: new Random(42));
Assert.NotEmpty(on);                       // ON → dict non-empty
var off = ShuffleEngine.BuildOptionShuffle(questions, shuffleOptions: false, rng: new Random());
Assert.Empty(off);                         // OFF → empty (caller serializes "{}")
```

---

### `.planning/phases/375-test-uat/375-HUMAN-UAT.md` (doc, UAT result)

**Analog:** `.planning/phases/373-shuffle-engine-read-logic-reshuffle/373-HUMAN-UAT.md` (status `partial`, milestone sama). NB: `374-HUMAN-UAT.md` **tidak ada** — Phase 374 UAT hasil tercatat di `374-03-SUMMARY.md` tabel "UAT Browser — 7/7 PASS" (pola tabel tsb dipakai untuk skenario ManagePackages e2e).

**Frontmatter + struktur** (373-HUMAN-UAT.md lines 1-9, status `partial` per D-08):
```markdown
---
status: partial
phase: 375-test-uat
source: [375-VERIFICATION.md]
started: 2026-06-13
updated: 2026-06-13
---

## Current Test
[awaiting human testing — `Authentication__UseActiveDirectory=false dotnet run` @localhost:5277]

## Tests

### 1. <judul skenario>
expected: <perilaku>
result: [pending]

## Summary
total: N
passed: 0
issues: 0
pending: N
...

## Gaps
(...)
```

**Tabel UAT pola Phase 374** (374-03-SUMMARY.md lines 44-52 — untuk catat 5 skenario ManagePackages e2e + manual exam-diff D-03):
```markdown
| # | Skenario | Hasil |
|---|----------|-------|
| 1 | Card render + toggle saved-state + Simpan PRG | PASS — ... |
| ... |
```

**Exam-taking effect (D-03 manual, SC#2 pass-bar D-03a)** — catat 2-peserta diff + screenshot:
- ShuffleQuestions ON → urutan soal **beda** antar 2 peserta.
- ShuffleOptions ON → urutan opsi **beda**.
- ShuffleQuestions OFF + ≥2 paket → tiap worker **1 paket UTUH urut asli** (round-robin index).
- Bukti = visual browser + screenshot. StartExam route: `/CMP/StartExam?sessionId=...` (atau `/CMP/StartExam/{id}` per examTypes.ts:127); answer text-match; submit `Kumpulkan Ujian` (examTypes.ts `submitExamTwoStep`).

---

### `Controllers/CMPController.cs` — SHUF-15 (verify-only)

**Analog:** self. **Status: SUDAH BERSIH** — re-grep 2026-06-13 (read-only):
- Line 989: `// Option shuffle gated on ShuffleOptions (independent flag). OFF → empty dict →` ✅ benar.
- Line 1060: `// ViewBag.OptionShuffle when ShuffleOptions=ON. OFF stores "{}" → view falls back...` ✅ benar.
- **TIDAK ditemukan** komentar stale `option shuffle removed` / `shuffle removed`.

**Aksi 375:** verify clean + close box (D-07). JANGAN edit kecuali execute menemukan komentar stale baru. Re-grep saat execute: `option shuffle removed`/`shuffle removed` di `Controllers/CMPController.cs`.

---

## Shared Patterns

### DB snapshot/restore (SEED_WORKFLOW, D-04)
**Source:** `tests/helpers/dbSnapshot.ts` + `tests/e2e/image-in-assessment.spec.ts` beforeAll/afterAll.
**Apply to:** `shuffle.spec.ts` (auto via Playwright global setup/teardown Phase 315) + manual exam-diff D-03 (snapshot DB lokal → seed multi-paket+2 peserta → exam-diff → **restore** + tandai `docs/SEED_JOURNAL.md` cleaned). Klasifikasi seed = `temporary + local-only`.
```typescript
// localhost-guard built-in (dbSnapshot.ts:36-44 runSqlcmd reject non-localhost).
await db.backup(snapshotPath);   // BACKUP ... WITH INIT, FORMAT
await db.restore(snapshotPath);  // SINGLE_USER ROLLBACK IMMEDIATE → RESTORE WITH REPLACE → MULTI_USER
```

### Login + accounts (auth)
**Source:** `tests/helpers/auth.ts` + `tests/helpers/accounts.ts`.
**Apply to:** `shuffle.spec.ts` (admin untuk ManagePackages) + manual exam-diff (coachee + coachee2 untuk 2-peserta diff).
```typescript
await login(page, 'admin');     // admin@pertamina.com / 123456
// peserta 2-diff: 'coachee' (rino.prasetyo@) + 'coachee2' (iwan3@) — keduanya pwd 123456
```

### Wizard-create (D-06, JANGAN flat-form)
**Source:** `tests/e2e/helpers/examTypes.ts` (`createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`) + `tests/e2e/helpers/wizardSelectors.ts`.
**Apply to:** SEMUA setup assessment di 375. JANGAN sentuh `exam-taking.spec.ts` `.fixme` / `exam-types.spec.ts` W0 (scope Phase 364).

### Env Playwright lokal
**Source:** CLAUDE.md + memory `reference_local_e2e_sql_env_fix`.
**Apply to:** semua run e2e + manual UAT.
```
Authentication__UseActiveDirectory=false dotnet run     # AD off lokal
# bila login 500 (NTLM loopback): ConnectionStrings__DefaultConnection='Server=lpc:localhost\SQLEXPRESS;Database=HcPortalDB_Dev;...'
# combined Playwright run WAJIB --workers=1 (fullyParallel:false di config — DB isolation)
```

### Test run commands
**Source:** `374-VALIDATION.md` Test Infrastructure.
**Apply to:** SC#1.
```
dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Shuffle"   # subset cepat
dotnet test HcPortal.Tests/HcPortal.Tests.csproj                                          # full suite (baseline 347 → 348+)
npx playwright test e2e/shuffle.spec.ts                                                    # e2e shuffle (cd tests)
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none) | — | — | Semua 4 file punya analog langsung di repo. |

> Exam-taking **order-diff otomatis** (2 worker assert urutan beda) sengaja TIDAK dibuat (ditolak by design, D-03) — pakai manual browser. Bukan "no analog", tapi "out of scope automated".

---

## Metadata

**Analog search scope:** `tests/e2e/`, `tests/helpers/`, `HcPortal.Tests/`, `Helpers/ShuffleEngine.cs`, `Views/Admin/ManagePackages.cshtml`, `Controllers/{AssessmentAdminController,CMPController}.cs`, `.planning/phases/{373,374}/`.
**Files scanned:** 14 (read) + 4 grep.
**Pattern extraction date:** 2026-06-13.
**RESEARCH.md:** tidak ada (research di-skip) — pola = CONTEXT code_context + analog NYATA.

---

## PATTERN MAPPING COMPLETE

**Phase:** 375 - test-uat
**Files classified:** 4
**Analogs found:** 4 / 4

### Coverage
- Files with exact analog: 3 (`shuffle.spec.ts`→image-in-assessment, sweep→ShuffleEngineTests, HUMAN-UAT→373-HUMAN-UAT)
- Files with role-match analog: 1 (CMPController SHUF-15 = self, verify-only)
- Files with no analog: 0

### Key Patterns Identified
- **e2e:** `image-in-assessment.spec.ts` template = `mode:'serial'` + DB `backup`/`restore` beforeAll/afterAll + `createAssessmentViaWizard` (drop flat-form & file-upload cleanup). Card selectors NYATA dari `ManagePackages.cshtml`: `#shuffleQuestions`, `#shuffleOptions`, `#shuffleSizeWarning`, `.alert-info` lock banner, `button:has-text("Simpan Pengaturan")`. Save-PRG assert = `.alert-success` "Pengaturan pengacakan berhasil disimpan." (endpoint `/Admin/UpdateShuffleSettings` → `RedirectToAction("ManagePackages")`).
- **xUnit sweep:** `ShuffleEngine.{BuildQuestionAssignment, BuildOptionShuffle}` di-uji via `[Theory]/[InlineData]` high-level per-mode invariant (D-01a no-dobel) + `new Random(42)` seed-stability (panggil 2× → `Assert.Equal`). Reuse `Pkg(...)` in-memory builder. Plus 1 guard all-empty no-DivideByZero.
- **UAT doc:** `373-HUMAN-UAT.md` frontmatter `status: partial` + Tests/Summary/Gaps; tabel hasil pola `374-03-SUMMARY.md` (UAT 7/7). SC#2 pass-bar D-03a = manual 2-peserta visual + screenshot.
- **SHUF-15:** CMPController sudah bersih (line 989/1060 benar, tak ada "shuffle removed" stale) → verify + close.

### File Created
`.planning/phases/375-test-uat/375-PATTERNS.md`

### Ready for Planning
Pattern mapping selesai. Planner bisa rujuk analog patterns langsung di PLAN.md.
