# Phase 379: Migrate exam-taking e2e to wizard - Pattern Map

**Mapped:** 2026-06-14
**Files analyzed:** 3 (1 spec rewrite-in-place + 2 helper extend-additive)
**Analogs found:** 3 / 3 (semua analog SUDAH ada di repo, proven hijau)

> Ini fase TEST-INFRA (TypeScript Playwright). Tidak ada kode produksi disentuh. "Files modified" = `tests/e2e/exam-taking.spec.ts` (rewrite create+question step semua flow A-J + ADD Flow K), `tests/e2e/helpers/examTypes.ts` (extend additive), `tests/e2e/helpers/wizardSelectors.ts` (extend additive). Narasi Bahasa Indonesia; semua kode/selector/path verbatim English.

---

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `tests/e2e/exam-taking.spec.ts` (Flow A-J create+QADD rewrite) | test (e2e spec) | request-response (UI wizard create → DB-state assert) | `tests/e2e/exam-types.spec.ts` (FLOW L/M + smoke W0.1) ; `tests/e2e/shuffle.spec.ts` | exact (analog spec sudah pakai helper yang sama) |
| `tests/e2e/exam-taking.spec.ts` (Flow K BARU essay) | test (e2e spec) | request-response + transform (essay → grade → DB-aggregate assert) | `tests/e2e/exam-types.spec.ts` FLOW L (`:305-428`) | exact (port verbatim, FLOW L sudah un-fixme oleh Phase 376) |
| `tests/e2e/helpers/examTypes.ts` (extend: token/proton/paste) | utility (test helper) | transform (opts → wizard fill) | block extend additive existing di file (Phase 317/318/319) | role-match (pola extend additive sudah ada di file yang sama) |
| `tests/e2e/helpers/wizardSelectors.ts` (extend: tokenSection/protonTrackSelect) | config (selector single-source) | — (konstanta selector) | block `wizardSelectors`/`prePostWizardSelectors` (Phase 317/318) | role-match (pola "extend additive — JANGAN refactor existing" tertulis eksplisit di header) |

---

## Pattern Assignments

### `tests/e2e/exam-taking.spec.ts` — header + imports + serial mode

**Analog:** `tests/e2e/exam-types.spec.ts:1-25`

Target saat ini (`exam-taking.spec.ts:1-6`) BELUM import `examTypes`. Tambahkan import helper kanonik mengikuti analog. Import block verbatim dari analog:

```typescript
// Source: tests/e2e/exam-types.spec.ts:1-25 — VERIFIED
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, yesterday } from '../helpers/utils';
import {
  createAssessmentViaWizard,
  createDefaultPackage,
  addQuestionViaForm,
  submitExamTwoStep,
  checkMAOptionsForQuestion,
  fillEssayAnswer,
  gradeSingleEssaySession,
  // ...
  type QuestionInput,
} from './helpers/examTypes';
import * as db from '../helpers/dbSnapshot';

// Sequential mode — per-flow describe shares state (assessmentId/packageId/sessionId) antar sub-tests.
test.describe.configure({ mode: 'serial' });
```

> Target sudah punya `test.describe.configure({ mode: 'serial' })` di `:6` (PRESERVE). Target import existing `clickResumeForFixture` dkk dari `./helpers/exam313` (`:4`) HARUS DIPERTAHANKAN (dipakai 313-flows downstream). Tambah `db` import (`../helpers/dbSnapshot`) untuk Flow K DB-assert (belum ada di target). `Page` type sudah di-import target `:1` (PRESERVE — dipakai `goToMonitoringDetail`).

---

### `tests/e2e/exam-taking.spec.ts` — Flow A-J CREATE step (ganti flat-form)

**Analog:** `tests/e2e/exam-types.spec.ts:314-333` (FLOW L1) — pola create-wizard + extract assessmentId.

**Pola LAMA yang diganti** (`exam-taking.spec.ts:36-60`, Flow A1 — flat-form usang):
```typescript
// Source: tests/e2e/exam-taking.spec.ts:39-59 (Flow A1, OBSOLETE flat-form) — HAPUS
await page.goto('/Admin/CreateAssessment');
await page.locator('.user-check-item[data-email="rino.prasetyo@pertamina.com"] input.user-checkbox').check({ force: true });
await page.fill('#Title', title);
await page.selectOption('#Category', 'OJT');
await page.fill('#ScheduleDate', today());
await page.fill('#ScheduleTime', '00:01');
await page.fill('#DurationMinutes', '30');
await page.fill('#PassPercentage', '60');
await page.locator('#AllowAnswerReview').check();
await page.locator('#GenerateCertificate').check();
await page.click('#submitBtn');                       // flat submit — usang
await page.waitForTimeout(3_000);
```

**Pola BARU (copy dari analog FLOW L1)**:
```typescript
// Source: tests/e2e/exam-types.spec.ts:314-333 (FLOW L1) — VERIFIED proven
title = uniqueTitle('Pre Test Legacy Exam');
await login(page, 'hc');
await createAssessmentViaWizard(page, {
  title,
  category: 'OJT',
  scheduleDate: today(),
  scheduleTime: '00:01',
  durationMinutes: 30,
  passPercentage: 60,
  allowAnswerReview: true,        // Flow A9 answer-review butuh ini true
  generateCertificate: true,      // Flow A10 cert butuh ini true
  participantEmails: ['rino.prasetyo@pertamina.com'],
});
const href = await page.locator('#modal-manage-btn').getAttribute('href');
assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
```

> Regex extract assessmentId `/(?:\/|assessmentId=)(\d+)/` = verbatim dari analog (`#modal-manage-btn` href format `/Admin/ManagePackages?assessmentId={id}`). `successModal` static-backdrop di-handle helper (`examTypes.ts:102-103`); caller cukup ambil href (TIDAK perlu klik manage-btn dulu bila langsung `goto` ManagePackages — pola FLOW L2 valid).
> **Multi-worker flows (C/F):** `participantEmails: ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com']` (akun `coachee2`=iwan3, lihat Shared Patterns Auth).

---

### `tests/e2e/exam-taking.spec.ts` — Flow A-J QADD step (ganti `/Admin/ManageQuestions?id=` flat)

**Analog:** `tests/e2e/exam-types.spec.ts:336-355` (FLOW L2+L3) — createDefaultPackage + addQuestionViaForm.

**Pola LAMA yang diganti** (`exam-taking.spec.ts:62-116`, Flow A2/A3 — questions langsung ke assessment):
```typescript
// Source: tests/e2e/exam-taking.spec.ts:83-92 (Flow A3, OBSOLETE) — HAPUS
await page.goto(`/Admin/ManageQuestions?id=${assessmentId}`);
await page.fill('textarea[name="question_text"]', 'Apa kepanjangan OJT?');
await page.locator('input[name="options"]').nth(0).fill('On the Job Training');
await page.locator('input[name="options"]').nth(1).fill('Online Job Test');
await page.locator('input[name="options"]').nth(2).fill('Operation Job Task');
await page.locator('input[name="options"]').nth(3).fill('Operational Job Training');
await page.locator('input[name="correct_option_index"][value="0"]').check();
await page.click('button:has-text("Tambah Soal")');   // flat add — usang
```

**Pola BARU (copy dari analog FLOW L2+L3)**:
```typescript
// Source: tests/e2e/exam-types.spec.ts:339-354 (FLOW L2 + L3) — VERIFIED
await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
await page.waitForLoadState('networkidle');
packageId = await createDefaultPackage(page);          // returns packageId (regex packageId=\d+)
await addQuestionViaForm(page, packageId, {
  type: 'MultipleChoice',
  text: 'Apa kepanjangan OJT?',
  options: ['On the Job Training', 'Online Job Test', 'Operation Job Task', 'Operational Job Training'],
  correctIndex: 0,
  score: 100,
});
// Q2/Q3: ulang addQuestionViaForm(page, packageId, {...correctIndex:1/2})
```

> `QuestionInput` discriminated union (`examTypes.ts:18-21`): `MultipleChoice` (options 4-tuple + correctIndex 0-3), `MultipleAnswer` (correctIndices[]), `Essay` (rubrik + maxCharacters). `addQuestionViaForm` route = `/Admin/ManagePackageQuestions?packageId={N}` (catatan `examTypes.ts:175`: RESEARCH lama sebut `ManageQuestions`, AKTUAL = `ManagePackageQuestions`).

---

### `tests/e2e/exam-taking.spec.ts` — Flow K (BARU, D-01) Essay full cycle + Score aggregation

**Analog:** `tests/e2e/exam-types.spec.ts:305-428` (FLOW L — port VERBATIM). FLOW L sudah un-fixme oleh Phase 376 dan berdiri sebagai regression guard GRADE-01.

**Excerpt analog penuh (L1-L6 — blueprint port)**:
```typescript
// Source: tests/e2e/exam-types.spec.ts:305-428 (FLOW L) — VERIFIED, un-fixme'd by Phase 376
test.describe('FLOW L — Essay Full Cycle + HC Grading', () => {
  let title: string; let category: string; let scheduleDate: string;
  let assessmentId: number; let packageId: number; let sessionId: number;
  const Q_MARKER = '[317-L] Essay — OJT pengembangan kompetensi';

  // L1 — createAssessmentViaWizard (category 'IHT', durationMinutes 60, passPercentage 70)
  //   → const href = await page.locator('#modal-manage-btn').getAttribute('href');
  //   → assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  // L2 — await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
  //      packageId = await createDefaultPackage(page);
  // L3 — await addQuestionViaForm(page, packageId, {
  //        type: 'Essay', text: `${Q_MARKER} — ...`, rubrik: '...', maxCharacters: 2000, score: 100 });

  // L4 — worker fill essay + submit:
  //   await login(page, 'coachee'); await page.goto('/CMP/Assessment');
  //   const card = page.locator('.assessment-card', { hasText: title }).first();
  //   page.once('dialog', (d) => d.accept());
  //   await card.locator('a:has-text("Mulai"), .btn-start-standard, a:has-text("Resume")').first().click();
  //   await page.waitForURL(/\/CMP\/StartExam\/\d+/, { timeout: 15_000 });
  //   await page.waitForFunction(() => (window as any).assessmentHub?.state === 'Connected', ...);
  //   sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)![1], 10);
  //   const qCard = page.locator('[id^="qcard_"]').filter({ hasText: Q_MARKER });
  //   await fillEssayAnswer(page, qCard, essayAnswer);
  //   await submitExamTwoStep(page);

  // L5 — HC grade + finalize:
  //   await login(page, 'hc');
  //   await gradeSingleEssaySession(page, { title, category, scheduleDate, sessionId, score: 80 });

  test('L6 — Worker scores 80 (DB-based verify per SURF-317-A workaround)', async () => {
    const score = await db.queryScalar(
      `SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`
    );
    expect(score).toBe(80);
    const statusOk = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${sessionId} AND Status = 'Completed'`
    );
    expect(statusOk).toBe(1);
  });
});
```

> **Flow K = port pola ini** ke `exam-taking.spec.ts` (default: append, satu suite — D-01 diskresi). Ganti marker jadi mis. `[379-K] Essay GRADE-01 regression` dan title `uniqueTitle('Pre Test [379-K] Essay')`. K6 assert `Score === 80` (BUKAN 0 → bukti fix 376) + Status 'Completed'. JANGAN assert via UI badge "Sudah Dinilai" saja (tidak buktikan agregasi numerik — D-01). `db.queryScalar` signature: `(sql: string) => Promise<number>` (`dbSnapshot.ts:116`, localhost-only guard).

---

### `tests/e2e/exam-taking.spec.ts` — Flow B (token) CREATE + token

**Analog:** create-wizard FLOW L1 (di atas) + extend helper token (lihat Shared Patterns).

**Pola LAMA + DRIFT** (`exam-taking.spec.ts:319-346`, Flow B1):
```typescript
// Source: tests/e2e/exam-taking.spec.ts:333-340 (Flow B1, OBSOLETE + DRIFT) — HAPUS
await page.locator('#IsTokenRequired').check();
await page.waitForTimeout(300);
await expect(page.locator('#tokenInputContainer')).toBeVisible();   // DRIFT: markup current = #tokenSection
await page.click('button:has-text("Generate")');
const tokenValue = await page.locator('#AccessToken').inputValue();
expect(tokenValue.length).toBe(6);
```

> **DRIFT terverifikasi:** selector lama `#tokenInputContainer` → markup current `#tokenSection` (CreateAssessment.cshtml). `wizardSelectors.isTokenRequired='#IsTokenRequired'` + `wizardSelectors.accessToken='#AccessToken'` SUDAH ADA (`wizardSelectors.ts:67-68`); `#tokenSection` BELUM → tambah additive. Token 6-char alfanumerik. Flow B NO question (tidak QADD). Step B2-B5 (badge `Token Required` + `.btn-start-token` + token modal + monitoring + cleanup) = SURVIVE (selector tervalidasi). Lihat Shared Patterns → Token Extension.

---

### `tests/e2e/exam-taking.spec.ts` — Flow D (paste-import) D1 CREATE + D3 paste

**Analog:** create-wizard FLOW L1 + `createDefaultPackage` (D2 sudah package-aware). D3 = helper paste-import baru (opsional) atau preserve flat-paste.

**Pola D3 LAMA (preserve / wrap jadi helper)** (`exam-taking.spec.ts:599-621`):
```typescript
// Source: tests/e2e/exam-taking.spec.ts:603-620 (Flow D3) — selector masih ada, verify route
await page.locator('a:has-text("Import Questions")').first().click();
await page.waitForLoadState('networkidle');
await expect(page.locator('body')).toContainText('Import Questions');
const pasteData = [
  'Apa itu safety?\tPerlindungan kerja\tMakan siang\tOlahraga\tTidur\tA',
  // ...6-kolom TSV (Q \t optA..D \t correct)
].join('\n');
await page.fill('textarea[name="pasteText"]', pasteData);
await page.click('button:has-text("Import")');
await page.waitForLoadState('networkidle');
await expect(page.locator('body')).toContainText(/3 soal|success|berhasil/i);
```

> D1 create masih flat-form → migrasi ke `createAssessmentViaWizard`. D2 sudah pakai `input[name="packageName"]` + Create Package = identik dgn `createDefaultPackage` → ganti ke helper. D3 paste-import = nilai UNIK Flow D, PRESERVE coverage: bungkus jadi helper additive `importQuestionsViaPaste(page, packageId, tsvRows)` (lihat Shared Patterns) ATAU fallback `addQuestionViaForm` ×3 bila route drift (Open Q2 — verify Wave 0).

---

### `tests/e2e/exam-taking.spec.ts` — Flow E (Proton T3 interview, D-02 double-drift)

**Analog:** create-wizard FLOW L1 + extend helper proton (Step 1). Interview form E3 = pola sudah verified (RESEARCH Code Examples).

**Pola E1 LAMA (Proton create — Step 1 drift)** (`exam-taking.spec.ts:716-743`):
```typescript
// Source: tests/e2e/exam-taking.spec.ts:718-743 (Flow E1, OBSOLETE flat-form) — HAPUS
await page.selectOption('#Category', 'Assessment Proton');
await page.waitForTimeout(500);
const protonSection = page.locator('#protonFieldsSection');
await expect(protonSection).toBeVisible();
const trackSelect = page.locator('#protonTrackSelect');
const options = trackSelect.locator('option');
// loop options, cari text.includes('Tahun 3') → selectOption({ index: i })
if (!tahun3Found) { test.skip(true, 'No Tahun 3 ProtonTrack available'); return; }  // D-02 LARANG skip
```

**Pola E3 interview submit (SURVIVE — field cocok 100%, verified controller 3669-3705)**:
```typescript
// Source: tests/e2e/exam-taking.spec.ts:790-813 + RESEARCH Code Examples — VERIFIED field match
const form = page.locator('form[action*="SubmitInterviewResults"]').first();
await form.locator('input[name="judges"]').fill('Dr. Andi, Ir. Budi');
const aspects = form.locator('select[name^="aspect_"]');                  // 5 aspek
for (let i = 0; i < await aspects.count(); i++) await aspects.nth(i).selectOption('4');
await form.locator('textarea[name="notes"]').fill('E2E — kompetensi baik.');
await form.locator('input[name="isPassed"]').check();
await form.locator('button[type="submit"]').click();                     // @Html.AntiForgeryToken auto-included
await page.waitForLoadState('networkidle');
await expect(page.locator('body')).toContainText(/Lulus|berhasil disimpan/i);
```

> **D-02:** E migrasi PENUH + re-check Proton. E1 create Proton ada di STEP 1 (Category='Assessment Proton' → `#protonFieldsSection` show → pilih track by `data-tahun==='Tahun 3'`). Extend `CreateAssessmentOpts` dgn `protonTrackId`/`protonTrackTahun` (Shared Patterns → Proton Extension). E2 badge "Interview Dijadwalkan"/no-Start + E3 field `judges`/`aspect_*`/`notes`/`isPassed` = SURVIVE (verified match). **D-02 LARANG `test.skip`** — jika ProtonTrack Tahun 3 absen di DB lokal (Open Q1), seed minimal (SEED_WORKFLOW + journal) atau `SeedProtonTracksAsync` (idempotent). Verify Wave 0: `db.queryScalar("SELECT COUNT(*) FROM ProtonTracks WHERE TahunKe='Tahun 3'")`.

---

### `tests/e2e/exam-taking.spec.ts` — Flow G (timer 1-min) deterministik (D-03)

**Analog:** create-wizard FLOW L1 (`durationMinutes: 1`) + event-driven assertion (pola `waitForFunction` `examTypes.ts:501-508`).

**Pola LAMA (sleep-buta — HAPUS)** (`exam-taking.spec.ts:1032`):
```typescript
// Source: tests/e2e/exam-taking.spec.ts:1032 (Flow G2, FLAKY) — HAPUS
await page.waitForTimeout(70_000); // Wait 70 seconds — flaky + lambat (D-03 larang)
```

**Pola BARU (event-driven, pola dari `addExtraTimeViaModal` waitForFunction)**:
```typescript
// Source pattern: tests/e2e/helpers/examTypes.ts:501-508 (waitForFunction event-driven) — VERIFIED
await page.waitForFunction(
  () => {
    const remaining = (window as unknown as { timerStartRemaining?: number }).timerStartRemaining ?? 1;
    const expired = document.querySelector('#examExpiredModal');
    return remaining <= 0 || (expired && getComputedStyle(expired).display !== 'none')
      || /\/CMP\/Results\//.test(location.href);
  },
  undefined,
  { timeout: 75_000 }   // bounded, resolve segera saat expired — bukan fixed sleep
);
```

> **D-03:** `durationMinutes` SUDAH param `CreateAssessmentOpts` (no helper extension) — cukup `durationMinutes: 1`. Yang perlu = deterministik timer-expiry: ganti `waitForTimeout(70_000)` buta dengan `waitForFunction` event-driven (resolve segera saat `timerStartRemaining <= 0` ATAU `#examExpiredModal` visible ATAU URL → Results). Verify Wave 0: `#examExpiredModal` masih ada di StartExam.cshtml (A3); fallback DB-state assert Status 'Completed'/'Abandoned'. Flow H sebagian sudah deterministik (`#count-completed` + `GetMonitoringProgress` JSON) — ganti `waitForTimeout(12_000)` H6 dengan `expect(closeBtn).toBeHidden()` auto-retry.

---

### `tests/e2e/helpers/examTypes.ts` — extend additive (token/proton/paste)

**Analog (pola extend additive di file yang sama):** blok Phase 318 PrePost (`examTypes.ts:511-613`) + Phase 319 (`:655-749`) — semua tambah interface/function BARU tanpa ubah signature existing.

**`CreateAssessmentOpts` existing (extend, JANGAN ubah field existing)** (`examTypes.ts:23-35`):
```typescript
// Source: tests/e2e/helpers/examTypes.ts:23-35 — interface existing
export interface CreateAssessmentOpts {
  title: string;
  category: string;            // Value option dropdown #Category (e.g. 'OJT', 'IHT')
  scheduleDate: string;        // YYYY-MM-DD
  scheduleTime?: string;       // HH:mm, default '00:01'
  durationMinutes: number;
  passPercentage: number;
  allowAnswerReview: boolean;
  generateCertificate?: boolean;
  participantEmails: string[]; // ['rino.prasetyo@pertamina.com']
  ewcdDate?: string;
  ewcdTime?: string;
  // ── TAMBAH (semua optional → flow existing tetap kompatibel) ──
  // isTokenRequired?: boolean;
  // accessToken?: string;
  // protonTrackId?: number;
  // protonTrackTahun?: 'Tahun 1' | 'Tahun 2' | 'Tahun 3';
}
```

**Titik sisip di `createAssessmentViaWizard` STEP 3** (`examTypes.ts:84-96`, setelah `passPercentage` fill — token blok):
```typescript
// Source: titik sisip examTypes.ts:85-96 (STEP 3) — tambah blok kondisional token
if (opts.isTokenRequired) {
  await page.locator(wizardSelectors.isTokenRequired).check();
  await page.locator('#tokenSection').waitFor({ state: 'visible' });   // markup current (additive selector)
  if (opts.accessToken) await page.fill(wizardSelectors.accessToken, opts.accessToken);
  else await page.locator('button:has-text("Generate"), button[onclick*="generateToken"]').click();
}
```

**Titik sisip Proton di STEP 1** (`examTypes.ts:54-58`, setelah `selectOption(category)`):
```typescript
// Source: titik sisip examTypes.ts:56 (STEP 1) — bila category === 'Assessment Proton'
if (opts.protonTrackTahun) {
  await page.locator('#protonFieldsSection').waitFor({ state: 'visible' });
  // pilih option #protonTrackSelect by data-tahun===opts.protonTrackTahun (loop options + getAttribute)
  // atau selectOption({ value: String(opts.protonTrackId) })
}
```

**Helper paste-import BARU (pola function flat-export, JSDoc + source citation seperti existing)**:
```typescript
// Pola: examTypes.ts:632 verifyCertificatePdfDownload (flat-export + JSDoc + source citation)
export async function importQuestionsViaPaste(page: Page, packageId: number, tsvRows: string): Promise<void> {
  // navigate ke Import Questions untuk package → fill textarea[name="pasteText"] → klik Import → verify success
}
```

> **D-04:** Extend ADDITIVE (semua field optional, helper baru = function baru). JANGAN ubah param order / signature existing (preserve blame). Pola sudah dicontohkan blok Phase 318/319 di file yang sama (tiap blok punya header `// Phase N ...` + source citation). Header file `examTypes.ts:1-16` = template citation style untuk dokumentasi blok baru.

---

### `tests/e2e/helpers/wizardSelectors.ts` — extend additive (tokenSection/protonTrackSelect)

**Analog (pola tertulis eksplisit di header file):** `wizardSelectors.ts:33` — `// Pattern: extend additive — JANGAN refactor selectors existing (preserve Phase 307/308 blame)`.

**Selektor token existing (SUDAH ADA — pakai langsung)** (`wizardSelectors.ts:67-68`):
```typescript
// Source: tests/e2e/helpers/wizardSelectors.ts:67-69 — SUDAH ADA di wizardSelectors block
  isTokenRequired: '#IsTokenRequired',
  accessToken: '#AccessToken',
  validUntil: '#ValidUntil',
```

**Tambah additive (selector yang BELUM ada)**:
```typescript
// TAMBAH ke wizardSelectors block (additive — JANGAN refactor existing):
//   tokenSection: '#tokenSection',              // markup current (gantikan #tokenInputContainer drift)
//   protonFieldsSection: '#protonFieldsSection',
//   protonTrackSelect: '#protonTrackSelect',    // name=ProtonTrackId; opsi punya data-tahun
```

> Pola extend = tambah key ke object `as const` yang sudah ada (atau blok `export const ...Selectors` baru bila grup berbeda, seperti `extraTimeSelectors`/`prePostWizardSelectors`/`questionFormSelectors` yang masing-masing blok terpisah). JANGAN refactor `selectors`/`wizardSelectors` existing (Phase 307/308/317 blame).

---

## Shared Patterns

### Auth (login akun nyata)
**Source:** `tests/helpers/auth.ts` + `tests/helpers/accounts.ts` (RESEARCH-verified)
**Apply to:** SEMUA flow.
```typescript
await login(page, 'hc');        // meylisa.tjiang — HC (create/grade/monitoring)
await login(page, 'coachee');   // rino.prasetyo@pertamina.com — worker utama (semua flow take-exam)
await login(page, 'coachee2');  // iwan3 — worker kedua (Flow C 2-worker, Flow F4)
```
> Multi-worker (C/F): `participantEmails: ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com']`.

### Token Extension (Flow B)
**Source:** `wizardSelectors.ts:67-68` (existing) + `examTypes.ts:84-96` (titik sisip STEP 3)
**Apply to:** Flow B1.
> `isTokenRequired`/`accessToken` selector SUDAH ADA; tambah `tokenSection` additive; extend `CreateAssessmentOpts` 2 field optional; sisip blok kondisional di STEP 3 helper. DRIFT: `#tokenInputContainer` (lama) → `#tokenSection` (current).

### Proton Extension (Flow E)
**Source:** `examTypes.ts:54-58` (titik sisip STEP 1) + interview form `:790-813` (SURVIVE)
**Apply to:** Flow E1 (create), E3 (interview = SURVIVE).
> Extend `CreateAssessmentOpts` dgn `protonTrackId`/`protonTrackTahun`; pilih track by `data-tahun`. D-02 LARANG skip. Prereq data ProtonTrack Tahun 3 (Open Q1).

### DB Assert (Flow K K6 + Flow G fallback + Flow E verify)
**Source:** `tests/helpers/dbSnapshot.ts:116-128` — `queryScalar(sql): Promise<number>` (localhost-only guard `:39-44`)
**Apply to:** Flow K (Score aggregation, WAJIB), Flow G fallback (Status state), Flow E (interview persist).
```typescript
const score = await db.queryScalar(`SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`);
expect(score).toBe(80);
```
> `runSqlcmd` REJECT non-localhost (`Refusing to target non-localhost SQL Server`) — CLAUDE.md compliance. Import `import * as db from '../helpers/dbSnapshot';`.

### Data Lifecycle / Cleanup (diskresi D-04)
**Source:** `tests/e2e/global.teardown.ts:64-66` (`db.restore(snapshotPath)` penuh) + DeleteAssessment cascade `Controllers/AssessmentAdminController.cs:2205-2299`
**Apply to:** cleanup step tiap flow.
> **VERIFIED A2:** `DeleteAssessment` pakai `RecordCascadeDeleteService.ExecuteAsync("session", id, ...)` (`:2275`) = atomic cascade root + turunan → package + question IKUT terhapus. Cleanup per-flow existing (`Hapus Grup` dropdown) cukup, TIDAK perlu package-delete terpisah. global.teardown `db.restore()` = safety-net penuh; cleanup per-flow hanya untuk isolasi title dalam run (mitigasi `uniqueTitle()` timestamp). Step cleanup existing (B5 `:380-395` dst) = SURVIVE.

### Worker take-exam (SURVIVE — tidak ter-drift)
**Source:** `examTypes.ts` — `submitExamTwoStep` (`:255`), `fillEssayAnswer` (`:320`), `checkMAOptionsForQuestion` (`:287`)
**Apply to:** semua flow worker-side (start/answer/submit/results/review/cert/monitoring/reset).
> Selector CMP (`.btn-start-standard`, `[id^="qcard_"]`, `#reviewSubmitBtn`, `/CMP/Results/`) tervalidasi hijau di exam-types/shuffle/313-flows (A5). Anti-shuffle: jawab by option-text/label (`label[id^="lbl_"]` filter hasText), BUKAN positional `.nth(correctIndex)`.

---

## No Analog Found

*(tidak ada)* — Semua file yang dimodifikasi punya analog langsung di repo (`exam-types.spec.ts` FLOW L/M + smoke, `shuffle.spec.ts`, blok extend-additive di `examTypes.ts`/`wizardSelectors.ts`). Tooling migrasi sudah lengkap & proven hijau. Risiko bukan logika baru, melainkan drift selector (token/proton/paste/timer) + prereq data ProtonTrack Tahun 3 (Open Q1) — semua di-verify Wave 0.

---

## Metadata

**Analog search scope:** `tests/e2e/` (specs + helpers), `tests/helpers/` (db/auth/utils), `Controllers/AssessmentAdminController.cs` (DeleteAssessment cascade verify)
**Files scanned:** 8 (exam-taking.spec.ts, exam-types.spec.ts, shuffle.spec.ts, examTypes.ts, wizardSelectors.ts, dbSnapshot.ts, global.teardown.ts, AssessmentAdminController.cs)
**Pattern extraction date:** 2026-06-14
