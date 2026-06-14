# Phase 355: Test & UAT (gambar di soal assessment) - Pattern Map

**Mapped:** 2026-06-09
**Files analyzed:** 5 (1 new e2e spec, 2 new fixtures, 1 modified xUnit, 2 modified helpers) + 1 doc append
**Analogs found:** 5 / 5 (semua punya analog langsung — phase test, bukan kode produksi baru)

> Catatan jenis phase: ini phase **TEST/UAT**. "File baru/diubah" = file tes, helper tes, dan fixture aset — bukan kode produksi. Tidak ada source code produksi yang disentuh (read-only constraint terhormat).

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `tests/e2e/image-in-assessment.spec.ts` (NEW) | e2e spec | request-response + file-I/O (upload) + DB-mutation guardrail | `tests/e2e/cmp-records-351.spec.ts` (snapshot/restore) + `tests/e2e/exam-taking.spec.ts` (coachee exam flow) | exact (komposisi 2 analog) |
| `HcPortal.Tests/PackageImageDeleteTests.cs` (MODIFY, +1 `[Fact]`) | unit test | file-I/O (temp-dir) | sibling `[Fact]`s di file yang sama | exact (same file) |
| `tests/e2e/helpers/examTypes.ts` (MODIFY, extend `addQuestionViaForm`) | test helper | request-response + file-I/O (setInputFiles) | `addQuestionViaForm` existing (function yang sama, additive) | exact (same function) |
| `tests/e2e/helpers/wizardSelectors.ts` (MODIFY, +selector gambar) | test helper (selector const) | n/a (static const) | `questionFormSelectors` existing | exact (same const) |
| `tests/fixtures/q-image.jpg` + `tests/fixtures/opt-image.png` (NEW) | fixture asset | static file | (tidak ada `tests/fixtures/` — lihat **No Analog Found**) | new convention |

---

## Pattern Assignments

### `tests/e2e/image-in-assessment.spec.ts` (e2e spec — request-response + file-I/O + DB guardrail)

**Analog utama (struktur snapshot/restore per-spec):** `tests/e2e/cmp-records-351.spec.ts`
**Analog sekunder (flow exam coachee):** `tests/e2e/exam-taking.spec.ts`

> KEPUTUSAN ARSITEKTUR (RESEARCH Pattern 1 + Anti-Pattern): pakai `beforeAll/afterAll` snapshot **per-spec** seperti cmp-records-351. **JANGAN** edit `tests/e2e/global.setup.ts`/`global.teardown.ts` (hardcoded matrix Phase 315).

**Imports + module-scope state pattern** (`cmp-records-351.spec.ts:21-40`):
```typescript
import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;
// + (phase 355) tambah: let createdPackageId: number | null = null;  // untuk cleanup folder upload

test.describe.configure({ mode: 'serial' });
```
> Catatan: spec ini juga butuh helper exam. Tambah import dari `./helpers/examTypes` (`createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`, `submitExamTwoStep`) dan `../helpers/auth` (`login`). Lihat assignment helper di bawah.

**Snapshot beforeAll pattern** (`cmp-records-351.spec.ts:44-58`) — resolve backup dir via SERVERPROPERTY (C:\Temp diblokir service account):
```typescript
test.beforeAll(async () => {
  const dir = (await db.queryString(
    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
  )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre355-${ts}.bak`;
  await db.backup(snapshotPath);
  // Phase 355: TIDAK perlu execScript seed (D-03 = admin-UI-driven create di dalam test).
  //   cmp-records-351 panggil db.execScript(cmp351-seed.sql) di sini — Phase 355 SKIP itu.
});
```

**Restore + Layer-4 assert afterAll pattern** (`cmp-records-351.spec.ts:60-70`) — restore lalu assert DB bersih:
```typescript
test.afterAll(async () => {
  if (!snapshotPath) return;
  let restoreError: unknown = null;
  try {
    await db.restore(snapshotPath);
    const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
  } catch (e) { restoreError = e; }
  // Phase 355 ADD (RESEARCH Pattern 3): cleanup file upload yang TIDAK ter-cover RESTORE.
  //   RESTORE balikkan rows, TAPI file fisik di wwwroot/uploads/questions/{pkgId}/ tetap nyangkut.
  if (createdPackageId != null) {
    const fs = await import('node:fs');
    const p = await import('node:path');
    const dir = p.resolve(__dirname, '../../wwwroot/uploads/questions', String(createdPackageId));
    try { fs.rmSync(dir, { recursive: true, force: true }); } catch { /* best-effort */ }
  }
  if (restoreError) throw restoreError;
  // (opsional Layer-4 assert: query COUNT row prefix unik = 0 — cmp-records-351:67-69 pattern)
});
```
> CRITICAL ordering (cmp-records-351:62-69): capture `restoreError` di try/catch, lakukan Layer-4 + cleanup, BARU `throw restoreError` di akhir — restore tetap jalan walau assert gagal.

**Admin upload via setInputFiles** (selector dari `Views/Admin/ManagePackageQuestions.cshtml:145-211` [VERIFIED], file input HIDDEN — `setInputFiles` tetap bekerja):
```typescript
// Setelah goto /Admin/ManagePackageQuestions?packageId={pkgId} + isi teks soal/opsi:
await page.setInputFiles('#questionImgField', path.resolve(__dirname, '../fixtures/q-image.jpg'));
await page.fill('#questionImageAlt', 'diagram pompa');
await page.setInputFiles('#optAImgField', path.resolve(__dirname, '../fixtures/opt-image.png'));
await page.fill('#optAImageAlt', 'opsi impeller');
await page.locator('#submitBtn').click();
await page.waitForLoadState('networkidle');
await expect(page.locator('.alert-success').first()).toBeVisible({ timeout: 5_000 });
```
> Selector gambar VERIFIED: soal `#questionImgField` (name `questionImage`, accept `image/png,image/jpeg`, L155) + `#questionImageAlt` (L165). Opsi `#optAImgField`..`#optDImgField` (name `optionAImage`.., L201) + `#optAImageAlt`.. (L198). Rekomendasi RESEARCH: jalankan lewat helper `addQuestionViaForm` yang di-extend (lihat assignment helper) — JANGAN duplikat logika isi-form.

**Coachee start-exam flow** (`exam-taking.spec.ts:122-134`) — login coachee → assessment card → start-standard → accept dialog → StartExam:
```typescript
await login(page, 'coachee');
await page.goto('/CMP/Assessment');
const card = page.locator('.assessment-card', { hasText: title });
const startBtn = card.locator('.btn-start-standard');
page.once('dialog', d => d.accept());
await startBtn.click();
await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
await expect(page.locator('#examHeader')).toBeVisible();
await expect(page.locator('#examTimer')).toBeVisible();
```

**Assert `<img>` render** (target markup `Views/Shared/_QuestionImage.cshtml:38-46` [VERIFIED] — render HANYA jika ImagePath non-null = guard L-02/D-06):
```typescript
// Gambar ADA + responsif (RESEARCH §B). JANGAN pakai .nth() positional (StartExam SHUFFLE opsi).
const img = page.locator('img.question-image-zoom').first();
await expect(img).toBeVisible();
expect(await img.getAttribute('class')).toContain('img-fluid');
expect(await img.getAttribute('loading')).toBe('lazy');
expect(await img.getAttribute('src')).toMatch(/\/uploads\/questions\//);
// Lightbox trigger: data-bs-target="#imageLightboxModal" + data-img-src
```
Markup acuan (apa yang di-assert):
```html
<img src="@imagePath" alt="@imageAlt"
     class="img-fluid rounded border mb-3 question-image-zoom[ d-block w-100 mt-2 mb-0 jika opsi]"
     style="max-height:{cap}px; cursor:pointer" loading="lazy"
     role="button" tabindex="0" onclick="event.preventDefault();"
     data-bs-toggle="modal" data-bs-target="#imageLightboxModal"
     data-img-src="@imagePath" data-img-alt="@imageAlt" aria-label="..." />
```

**Submit → Results** (helper `submitExamTwoStep`, `examTypes.ts:223-233`) — JANGAN hand-roll dialog timing:
```typescript
import { submitExamTwoStep } from './helpers/examTypes';
// (jawab minimal 1 radio sebelum submit — ExamSummary blok submit bila unanswered>0, Pitfall 5)
await submitExamTwoStep(page); // → waitForURL **/CMP/Results/**
// lalu assert img.question-image-zoom di section "Tinjauan Jawaban" (Results butuh AllowAnswerReview=true)
```

**Test 3 — guard null branch D-06** (soal TANPA gambar → tak render `<img>`):
```typescript
// Scope ke qcard soal tanpa-gambar → expect tidak ada img.question-image-zoom di dalamnya.
await expect(noImgCard.locator('img.question-image-zoom')).toHaveCount(0);
```

---

### `HcPortal.Tests/PackageImageDeleteTests.cs` (unit test — file-I/O, +1 `[Fact]`)

**Analog:** sibling `[Fact]`s di file yang sama (gaya HARUS identik — D-02 no churn).

**Helper signatures yang DIPAKAI ULANG** (sudah ada di file, `PackageImageDeleteTests.cs:20-39, 132-151`):
```csharp
private static string MakeTempDir()                                              // L20
private static bool PathStillReferenced(IEnumerable<PackageQuestion>, IEnumerable<PackageOption>, string path) // L30
private static void DeleteIfUnreferenced(string path, IEnumerable<PackageQuestion> remainingQ, IEnumerable<PackageOption> remainingO) // L34
private static void ApplyIntent(PackageOption target, bool newFilePresent, string? savedNewPath,
                                string? alt, bool removeChecked, List<string> deleteList) // L132
```

**Representative existing `[Fact]` body** (`PackageImageDeleteTests.cs:67-88`) — tunjukkan pola: `MakeTempDir` → write file nyata → run logic → assert `File.Exists` → `finally` cleanup dir:
```csharp
[Fact]
public void RefCount_Deletes_WhenNoOtherRowSharesPath()
{
    var dir = MakeTempDir();
    try
    {
        var path = Path.Combine(dir, "orphan.jpg");
        File.WriteAllBytes(path, new byte[] { 9, 9, 9 });

        var remainingQuestions = new List<PackageQuestion>();
        var remainingOptions = new List<PackageOption>();

        DeleteIfUnreferenced(path, remainingQuestions, remainingOptions);

        Assert.False(File.Exists(path), "File harus HILANG karena tak dipakai baris lain (D-10 delete).");
    }
    finally
    {
        Directory.Delete(dir, recursive: true);
    }
}
```

**Gap yang ditutup (RESEARCH §xUnit Gap Audit):** `ReplaceConflict_NewFileWins_OverRemoveCheckbox` (`:187-201`) HANYA assert `deleteList.Contains(old)` — TIDAK menulis file lama nyata lalu jalankan delete loop lalu assert `File.Exists(old)==false`. Tambah 1 `[Fact]` `Replace_NewFileWins_DeletesOldFileOnDisk` yang merantai `MakeTempDir` → write old+new file → `ApplyIntent(newFilePresent:true)` → loop `DeleteIfUnreferenced` → assert old hilang + new tetap (template lengkap di RESEARCH.md:296-323). Tetap "logic-mirror" (konsisten 3 file existing), BUKAN integration test (Deferred).

---

### `tests/e2e/helpers/examTypes.ts` (test helper — extend `addQuestionViaForm`)

**Analog:** function `addQuestionViaForm` yang sama (`examTypes.ts:154-212`) — extend ADDITIVE (tambah param `images?`), JANGAN refactor.

**Current `QuestionInput` + signature** (`examTypes.ts:18-21, 154`):
```typescript
export type QuestionInput =
  | { type: 'MultipleChoice'; text: string; options: [string,string,string,string]; correctIndex: 0|1|2|3; score: number }
  | { type: 'MultipleAnswer'; text: string; options: [string,string,string,string]; correctIndices: (0|1|2|3)[]; score: number }
  | { type: 'Essay'; text: string; rubrik: string; maxCharacters?: number; score: number };

export async function addQuestionViaForm(page: Page, packageId: number, q: QuestionInput): Promise<void> {
```

**Current fill-loop core** (tempat sisip `setInputFiles`, `examTypes.ts:172-205`) — sisip upload SETELAH fill teks opsi, SEBELUM `scoreValue`+submit:
```typescript
  await page.fill(questionFormSelectors.questionText, q.text);
  // ... (essay vs MC/MA branch fill options/correct) ...
  await page.fill(questionFormSelectors.scoreValue, String(q.score));
  await page.locator(questionFormSelectors.submitBtn).click();
  await page.waitForLoadState('networkidle');
  await expect(page.locator('.alert-success, .alert.alert-success').first()).toBeVisible({ timeout: 5_000 });
```

**Cara extend (additive — pola RESEARCH Pattern 2):** tambah parameter opsional `images?: { question?: string; optionA?: string; optionB?: string; optionC?: string; optionD?: string; questionAlt?: string; optionAAlt?: string; ... }` → di dalam fungsi panggil `page.setInputFiles(questionFormSelectors.questionImgField, images.question)` (dst.) hanya jika di-supply. Path fixture pakai `path.resolve(__dirname, '../fixtures/...')` (cwd bisa `tests/` saat `cd tests`).

> Catatan pitfall (sudah dimitigasi di helper, JANGAN dihilangkan): Pitfall 2 wait `applyQTypeSwitch` (`:163-170`), strict-mode `.first()` pada alert (`:211`), route benar `/Admin/ManagePackageQuestions` (BUKAN `ManageQuestions`, `:156`).

---

### `tests/e2e/helpers/wizardSelectors.ts` (test helper — selector const, +selector gambar)

**Analog:** const `questionFormSelectors` yang sama (`wizardSelectors.ts:103-121`) — extend ADDITIVE.

**Current shape** (`wizardSelectors.ts:103-121`):
```typescript
export const questionFormSelectors = {
  formCard: '#questionFormCard',
  questionType: '#QuestionType',
  questionText: '#questionText',
  optionsSection: '#optionsSection',
  maLabel: '#maLabel',
  rubrikSection: '#rubrikSection',
  rubrik: '#rubrik',
  maxCharacters: '#maxCharacters',
  scoreValue: '#scoreValue',
  submitBtn: '#submitBtn',
  optionA: '#option_A', optionB: '#option_B', optionC: '#option_C', optionD: '#option_D',
  correctA: '#correct_A', correctB: '#correct_B', correctC: '#correct_C', correctD: '#correct_D',
} as const;
```

**Selector gambar yang DITAMBAHKAN** (VERIFIED dari `Views/Admin/ManagePackageQuestions.cshtml:155,165,201,198,162,207`):
```typescript
  // Phase 355 — image upload fields (ManagePackageQuestions.cshtml:145-211)
  questionImgField: '#questionImgField',     // name="questionImage", hidden, accept image/png,image/jpeg
  questionImageAlt: '#questionImageAlt',      // name="questionImageAlt"
  removeQuestionImage: '#removeQuestionImage',// EDIT-only checkbox
  optAImgField: '#optAImgField', optBImgField: '#optBImgField', optCImgField: '#optCImgField', optDImgField: '#optDImgField', // name="optionAImage".. hidden
  optAImageAlt: '#optAImageAlt', optBImageAlt: '#optBImageAlt', optCImageAlt: '#optCImageAlt', optDImageAlt: '#optDImageAlt',
```
> Pola const lain di file (`wizardSelectors`/`extraTimeSelectors`/`prePostWizardSelectors`) semua `as const` + komentar `Source: ...cshtml lines` di header blok — ikuti konvensi itu.

---

## Shared Patterns

### DB snapshot/restore (Seed Workflow D-05)
**Source:** `tests/helpers/dbSnapshot.ts` — `backup(path)`, `restore(path)`, `queryString(sql)`, `queryScalar(sql)`, `execScript(path)`.
**Apply to:** `image-in-assessment.spec.ts` beforeAll/afterAll.
- `db.backup(snapshotPath)` → `BACKUP DATABASE ... WITH INIT, FORMAT` (`dbSnapshot.ts:67`).
- `db.restore(snapshotPath)` → SINGLE_USER ROLLBACK IMMEDIATE → RESTORE WITH REPLACE → MULTI_USER (`dbSnapshot.ts:80`).
- `db.queryString("SELECT ... InstanceDefaultBackupPath ...")` resolve backup dir (C:\Temp blocked, `dbSnapshot.ts:139`).
- Guard built-in: `runSqlcmd` REJECT non-localhost `-S` (`dbSnapshot.ts:39-44`) — jangan bypass.

### Login Playwright
**Source:** `tests/helpers/auth.ts:4-11` — `login(page, account)`.
**Apply to:** semua test di spec (admin upload + coachee exam).
```typescript
export async function login(page: Page, account: AccountKey) {
  const { email, password } = accounts[account];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL('**/Home/**', { timeout: 15_000 });
}
```
**Account keys** (`tests/helpers/accounts.ts`): `admin` = `admin@pertamina.com` / `123456`; `coachee` = `rino.prasetyo@pertamina.com` / `123456`; `hc` = `meylisa.tjiang@pertamina.com`. Semua pwd `123456` (dev lokal — JANGAN dipakai staging/prod).

### Create assessment + package + question (jangan klik wizard manual)
**Source:** `tests/e2e/helpers/examTypes.ts` — `createAssessmentViaWizard(page, opts)` (`:51`), `createDefaultPackage(page, name)` → return `packageId` (`:121`), `addQuestionViaForm(page, packageId, q)` (`:154`, EXTEND untuk gambar), `submitExamTwoStep(page)` (`:223`).
**Apply to:** test 1 (admin create) + test 2 (coachee submit) di spec.
- `createAssessmentViaWizard` opts punya `allowAnswerReview: true` (wajib agar Results section "Tinjauan Jawaban" render).
- `createDefaultPackage` extract `packageId` dari `a[href*="ManagePackageQuestions"]` → simpan ke `createdPackageId` untuk cleanup folder (Pattern 3).
- `participantEmails: ['rino.prasetyo@pertamina.com']` (sama dengan key `coachee` → peserta lihat assessment di `/CMP/Assessment`).

### Serial mode + describe
**Source:** `cmp-records-351.spec.ts:40-42` — `test.describe.configure({ mode: 'serial' })` + `test.describe('Phase 355 — ...', () => { ... })`.
**Apply to:** spec (test 1 admin → test 2 coachee → test 3 guard saling bergantung).

### SEED_JOURNAL entry (D-05)
**Source:** `docs/SEED_JOURNAL.md` (header L7 format) + contoh Phase 354 (L9) & Phase 351 (L10).
**Apply to:** append 1 baris untuk Phase 355.
Kolom: `| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |`. Klasifikasi `temporary + local-only`. Status `cleaned` setelah RESTORE sukses. Catat juga cleanup file `wwwroot/uploads/questions/{pkgId}` (non-DB, di luar RESTORE). Template baris persis ada di RESEARCH.md:390.

### xUnit harness (logic-mirror, no DbContext)
**Source:** `PackageImageDeleteTests.cs` (Sync/Delete tests = pure in-memory object).
**Apply to:** `[Fact]` baru di PackageImageDeleteTests.cs.
- Pakai `MakeTempDir` + `File.WriteAllBytes(path, new byte[]{...})` + `finally Directory.Delete(dir, recursive: true)`.
- Reuse `ApplyIntent` + `DeleteIfUnreferenced` yang sudah ada (JANGAN buat helper baru).
- Pesan assert pakai Bahasa Indonesia + kode butir (mis. "SYN-02 replace") seperti existing.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `tests/fixtures/q-image.jpg` + `tests/fixtures/opt-image.png` | fixture asset | static file | Direktori `tests/fixtures/` BELUM ADA (verified: glob kosong). Konvensi aset tes existing = SQL seed di `tests/sql/*-seed.sql` (di-load via `path.resolve(__dirname, '../sql/...')`), bukan binary fixture. Planner buat direktori baru `tests/fixtures/`. Syarat fixture (RESEARCH OQ3): JPG/PNG VALID magic-byte (JPG `FF D8 FF`, PNG `89 50 4E 47`) agar lolos `ValidateImageFile` + render `<img>` di browser; ukuran kecil (~1KB-10KB). JANGAN rename .txt→.jpg (itu kasus invalid yang justru diuji ditolak). Path-resolve di spec/helper: `path.resolve(__dirname, '../fixtures/q-image.jpg')` (mengikuti pola `path.resolve(__dirname, '../sql/...')` di cmp-records-351.spec.ts:51). |

> Catatan: ini satu-satunya item tanpa analog struktural. Selebihnya semua phase 355 = komposisi/extend pola existing (key insight RESEARCH:262).

## Anti-Patterns (jangan diulang — dari RESEARCH)

- **JANGAN edit `tests/e2e/global.setup.ts`/`global.teardown.ts`** — hardcoded matrix Phase 315 (Layer-1 expect 18 sessions). Pakai per-spec beforeAll/afterAll.
- **JANGAN andalkan DB RESTORE untuk bersihkan file upload** — file `wwwroot/uploads/questions/{pkgId}/` bukan bagian DB; wajib `fs.rmSync` di afterAll.
- **JANGAN pakai positional `.nth()` untuk opsi setelah StartExam render** — opsi di-SHUFFLE per-question. Assert `img.question-image-zoom` by count/visibility atau by `alt`/`data-img-alt`.
- **JANGAN tulis ulang 3 file tes xUnit** (D-02) — hanya tambah 1 `[Fact]` gap.
- **JANGAN tiru `exam-taking.spec.ts` blok create-soal** (route lama `/Admin/ManageQuestions` + `name="question_text"`) — pakai `addQuestionViaForm` (route+selector benar Phase 353).

## Metadata

**Analog search scope:** `tests/e2e/`, `tests/e2e/helpers/`, `tests/helpers/`, `HcPortal.Tests/`, `Views/Admin/ManagePackageQuestions.cshtml`, `Views/Shared/_QuestionImage.cshtml`, `docs/SEED_JOURNAL.md`, `tests/playwright.config.ts`.
**Files scanned (dibaca penuh/parsial sesi ini):** cmp-records-351.spec.ts, examTypes.ts, wizardSelectors.ts, PackageImageDeleteTests.cs, auth.ts, accounts.ts, dbSnapshot.ts, exam-taking.spec.ts, ManagePackageQuestions.cshtml, _QuestionImage.cshtml, SEED_JOURNAL.md, playwright.config.ts.
**Project skills dir:** tidak ada (`.claude/skills/` & `.agents/skills/` absent — konsisten RESEARCH:533).
**Pattern extraction date:** 2026-06-09
