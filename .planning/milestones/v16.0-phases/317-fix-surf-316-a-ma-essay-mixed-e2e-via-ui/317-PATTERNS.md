# Phase 317: Fix SURF-316-A + MA/Essay/Mixed E2E via UI — Pattern Map

**Mapped:** 2026-05-11
**Files analyzed:** 3 (2 NEW, 1 MODIFY)
**Analogs found:** 3 / 3 (all exact-match — codebase punya analog presisi)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `tests/e2e/exam-types.spec.ts` (NEW) | E2E test spec (Playwright) | request-response + SignalR event-driven | `tests/e2e/assessment-matrix.spec.ts` | exact (spec orchestrator dengan describe + context isolation) |
| `tests/e2e/helpers/examTypes.ts` (NEW) | POM-flat helper module | request-response + UI wizard form-fill | `tests/e2e/helpers/examMatrix.ts` | exact (POM-flat export functions + JSDoc + source citations) |
| `tests/e2e/helpers/wizardSelectors.ts` (MODIFY) | DOM selector constant map | static config | `tests/e2e/helpers/wizardSelectors.ts` (existing Phase 307+308 structure) | exact (extend pattern existing — tambah const map, jangan refactor) |

**Secondary reference (read-only, for FLOW A-J regression smoke Task 8):** `tests/e2e/exam-taking.spec.ts` — bukan analog (sebagian besar selector legacy obsolete per RESEARCH State-of-the-Art), hanya target diagnose-only.

---

## Pattern Assignments

### `tests/e2e/exam-types.spec.ts` (NEW — spec, request-response + SignalR)

**Analog:** `tests/e2e/assessment-matrix.spec.ts`

**Imports + describe configuration pattern** (assessment-matrix.spec.ts lines 54-59 + spec-level config):

```typescript
import { test, expect, type Browser, type Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
import {
  createAssessmentViaWizard,
  addQuestionViaForm,
  addExtraTimeViaModal,
  // ...
} from './helpers/examTypes';
import { gradeEssaysAsHc, verifyResultPage } from './helpers/examMatrix';

// Sequential mode di spec level karena per-flow describe punya shared state (assessmentId,
// packageId, sessionId) yang harus persist across sub-tests K1→K5. Matrix-spec drop this
// di Phase 316 Plan 05 karena failure isolation, tapi exam-types BUTUH serial untuk state
// pass antar sub-test — keep `test.describe.configure({ mode: 'serial' })`.
test.describe.configure({ mode: 'serial' });
```

**Per-flow describe block pattern** (assessment-matrix.spec.ts:186-211 + RESEARCH FLOW K skeleton lines 549-648):

```typescript
test.describe('FLOW K — MA Full Cycle', () => {
  let title: string;
  let assessmentId: number;
  let packageId: number;
  let sessionId: number;

  test('K1 — HC creates assessment via wizard', async ({ page }) => {
    title = uniqueTitle('MA Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, { /* opts */ });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/ManagePackages\/(\d+)/)![1]);
  });

  test('K2 — HC navigates ManagePackages → ManageQuestions', async ({ page }) => { /* ... */ });
  test('K3 — HC adds 2 MA questions', async ({ page }) => { /* ... */ });
  test('K4 — Worker takes MA exam + submits', async ({ page }) => { /* ... */ });
  test('K5 — Worker sees score 100 on Results', async ({ page }) => { /* ... */ });
});
```

**Per-test timeout pattern** (assessment-matrix.spec.ts:77 + 188):

```typescript
// Default 60s di playwright.config.ts tidak cukup untuk wizard + add questions + worker
// exam + grading. Matrix pakai 240s; FLOW K-N estimate 120s; FLOW O (2-context simul) 180s.
const FLOW_TIMEOUT_MS = 120_000;
const FLOW_O_TIMEOUT_MS = 180_000;

test('K1 — ...', async ({ page }) => {
  test.setTimeout(FLOW_TIMEOUT_MS);
  // ...
});
```

**Multi-context pattern untuk FLOW O** (assessment-matrix.spec.ts:135-141 + RESEARCH Pitfall 5):

```typescript
// FLOW O — HC + worker concurrent. Single context = cookie collision (Pitfall 5).
test('O3 — HC adds extra time while worker exam in-progress', async ({ browser }) => {
  test.setTimeout(FLOW_O_TIMEOUT_MS);
  const ctxWorker = await browser.newContext();
  const ctxHc = await browser.newContext();
  const pageWorker = await ctxWorker.newPage();
  const pageHc = await ctxHc.newPage();
  try {
    // ... worker starts exam, HC adds extra time
  } finally {
    // Defensive close per assessment-matrix.spec.ts:174-176 pattern.
    await ctxWorker.close().catch(() => { /* already closed — non-fatal */ });
    await ctxHc.close().catch(() => { /* already closed — non-fatal */ });
  }
});
```

**Reuse pattern (DON'T re-implement)** — call helper langsung:

```typescript
// FLOW L (Essay) + FLOW M (Mixed) reuse examMatrix.ts helpers:
import { gradeEssaysAsHc, verifyResultPage } from './helpers/examMatrix';

// L4: HC grade essay
await gradeEssaysAsHc(pageHc, {
  // Adapter — ScenarioConfig shape; jika butuh, define adapter di examTypes.ts
  // atau alternatif: inline grading sederhana panggil selector langsung
});

// Verify result race-tolerant (3-badge selector dari examMatrix.ts:308-310)
await expect(
  page.locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()
).toBeVisible({ timeout: 10_000 });
```

**FLOW N negative assertion pattern** (RESEARCH Pattern 4):

```typescript
// AllowAnswerReview=false → Results.cshtml branch ke alert-info
await expect(
  page.locator('.alert-info', { hasText: /Tinjauan jawaban tidak tersedia/i })
).toBeVisible();
await expect(page.locator('.card', { hasText: /Tinjauan Jawaban/i })).toHaveCount(0);
```

**Error handling pattern** — TANPA softAssert/collector:

Berbeda dengan `assessment-matrix.spec.ts` yang pakai `softAssert + collector + SkipScenarioError` untuk continue-on-fail behavior (matrix discovery focus), `exam-types.spec.ts` adalah **acceptance test** — first-fail = test fails (standar Playwright). Pakai langsung:

- `await expect(locator).toBeVisible()` untuk hard assertion
- `Promise.all([page.waitForURL(...), page.click(...)])` untuk race-tolerant navigation (dari examMatrix.ts:191-201)
- `page.once('dialog', d => d.accept())` SEBELUM click yang trigger `confirm()` (dari examMatrix.ts:197)

---

### `tests/e2e/helpers/examTypes.ts` (NEW — helper, request-response + form-fill)

**Analog:** `tests/e2e/helpers/examMatrix.ts`

**File header pattern** (examMatrix.ts lines 1-26):

```typescript
// Phase 317 — POM-flat helpers untuk MA/Essay/Mixed E2E via HC UI creation.
// Pattern reference: tests/e2e/helpers/examMatrix.ts (Phase 315/316 — flat export functions,
// JSDoc docblock per function, source code citation di header).
//
// Source code citations:
//  - Views/Admin/CreateAssessment.cshtml:77-815 — 4-step wizard structure, field IDs, success modal
//  - Views/Admin/ManagePackageQuestions.cshtml:117-458 — right-pane form, QuestionType switch JS
//  - Views/Admin/AssessmentMonitoringDetail.cshtml:122-143 — AddExtraTime modal markup
//  - Views/Admin/AssessmentMonitoringDetail.cshtml:1462-1495 — AddExtraTime AJAX handler
//  - Controllers/AssessmentAdminController.cs:5483-5527 — AddExtraTime endpoint + SignalR broadcast
//  - Views/CMP/StartExam.cshtml:1240-1268 — SignalR ExtraTimeAdded listener
//
// Wave 0 assumption verifications (317-RESEARCH.md Assumptions A1-A7):
//  - A4 question order persistence — VERIFY via DOM read sebelum K4 assert correctness
//  - A5 window.timerStartRemaining scope — VERIFY via page.evaluate sebelum O3 implementation

import { Page, expect } from '@playwright/test';
```

**Flat export function pattern with JSDoc** (examMatrix.ts:74-205 — `takeExam`):

```typescript
/**
 * HC login + navigate /Admin/CreateAssessment + traversal 4-step wizard + submit.
 *
 * 4-step pattern (Views/Admin/CreateAssessment.cshtml:77-815):
 *  - Step 1: #Title + #Category + #assessmentTypeInput → #btnNext1
 *  - Step 2: .user-check-item[data-email] checkbox → #btnNext2
 *  - Step 3: #schedDateInput + #schedTimeInput + #DurationMinutes + #PassPercentage +
 *            #AllowAnswerReview + #GenerateCertificate → #btnNext3
 *  - Step 4: review summary → #btnSubmit
 *  - Success: #successModal (data-bs-backdrop=static) appears with #modal-manage-btn
 *
 * Pitfall mitigations:
 *  - Pitfall 6 (RESEARCH.md): JANGAN pakai #ScheduleDate; selalu #schedDateInput.
 *  - Pitfall 3: success modal static-backdrop — caller WAJIB dismiss via #modal-manage-btn
 *    click ATAU .btn-close-white click sebelum nav ke ManagePackages.
 *  - Open Question 3: setelah btnNextN click, await step-(N+1) visible (timeout 5s) — kalau
 *    timeout = sign of missing required field (fail loud).
 */
export async function createAssessmentViaWizard(page: Page, opts: {
  title: string;
  category: string;
  scheduleDate: string;
  scheduleTime?: string;
  durationMinutes: number;
  passPercentage: number;
  allowAnswerReview: boolean;
  generateCertificate?: boolean;
  participantEmails: string[];
  ewcdDate?: string;
  ewcdTime?: string;
}): Promise<void> {
  // implementation per RESEARCH Pattern 1 (lines 168-225)
}
```

**Helper-internal soft sequencing pattern** (examMatrix.ts:108-184 — type-branch loop):

```typescript
// Pattern: setiap step ada visible cue WAITFOR sebelum next action (mitigasi Pitfall 2:
// JS handler async race). Bukan pakai page.waitForTimeout — pakai DOM cue spesifik.
export async function addQuestionViaForm(page: Page, packageId: number, q: QuestionInput): Promise<void> {
  await page.goto(`/Admin/ManageQuestions?packageId=${packageId}`);
  await page.locator('#questionFormCard').waitFor({ state: 'visible' });

  await page.selectOption('#QuestionType', q.type);

  // Pitfall 2 mitigation: wait visual cue per type SEBELUM next action
  if (q.type === 'MultipleAnswer') {
    await page.locator('#maLabel').waitFor({ state: 'visible' });
  } else if (q.type === 'Essay') {
    await page.locator('#rubrikSection').waitFor({ state: 'visible' });
  } else {
    await page.locator('#optionsSection').waitFor({ state: 'visible' });
    await expect(page.locator('#maLabel')).toBeHidden();
  }

  await page.fill('#questionText', q.text);
  // ... type-specific fields per RESEARCH Pattern 2 (lines 242-275)
  await page.locator('#submitBtn').click();
  await page.waitForLoadState('networkidle');
  // Optional verify: alert-success visible atau row count incremented (RESEARCH Open Q4)
}
```

**SignalR + multi-page coordination pattern** (examMatrix.ts:94-103 readiness gate + RESEARCH Pattern 3):

```typescript
/**
 * HC fire AddExtraTime modal → AJAX POST → SignalR broadcast → worker timer JS update.
 *
 * Group naming (Controllers/AssessmentAdminController.cs:5485):
 *   batch-{Title}|{Category}|{Date:yyyy-MM-dd} — composite key, BUKAN per-token group.
 *
 * Verify strategy (Pitfall 4 mitigation):
 *  1. pageHc: alert-success text "berhasil ditambahkan" — server-side success
 *  2. pageWorker: window.timerStartRemaining increased ≥ N seconds via waitForFunction
 *
 * Pre-condition: pageWorker MUST be at /CMP/StartExam/{id} with SignalR Connected state.
 */
export async function addExtraTimeViaModal(
  pageHc: Page,
  pageWorker: Page,
  opts: { title: string; category: string; scheduleDate: string; extraMinutes: 5|10|15|20|25|30|45|60|90|120 }
): Promise<void> {
  // 1. Capture initial timer remaining di worker (page.evaluate)
  const initialRemaining = await pageWorker.evaluate(() => {
    return (window as unknown as { timerStartRemaining?: number }).timerStartRemaining ?? 0;
  });

  // 2. HC navigate to MonitoringDetail (URLSearchParams pattern dari examMatrix.ts:230-235)
  const baseUrl = pageHc.url() || 'http://localhost:5277';
  const url = new URL('/Admin/AssessmentMonitoringDetail', baseUrl);
  url.searchParams.set('title', opts.title);
  url.searchParams.set('category', opts.category);
  url.searchParams.set('scheduleDate', opts.scheduleDate);
  await pageHc.goto(url.toString());

  // 3. Fire modal + confirm
  await pageHc.locator('button[data-bs-target="#extraTimeModal"]').click();
  await pageHc.locator('#extraTimeModal').waitFor({ state: 'visible' });
  await pageHc.selectOption('#extraTimeSelect', String(opts.extraMinutes));
  await pageHc.locator('#btnConfirmExtraTime').click();

  // 4. Verify HC alert-success
  await expect(
    pageHc.locator('.alert-success', { hasText: /berhasil ditambahkan/i })
  ).toBeVisible({ timeout: 10_000 });

  // 5. Verify worker timer increased (SignalR broadcast received)
  const expectedDelta = opts.extraMinutes * 60 - 30; // -30s margin for elapsed time
  await pageWorker.waitForFunction(
    (args) => {
      const remaining = (window as unknown as { timerStartRemaining?: number }).timerStartRemaining ?? 0;
      return remaining > args.initial + args.expectedDelta;
    },
    { initial: initialRemaining, expectedDelta },
    { timeout: 15_000 }
  );
}
```

**Submit-exam reuse pattern** (examMatrix.ts:186-204 — 2-step submit):

PRINSIP: JANGAN duplikasi. Ekspor `submitExamTwoStep(page)` helper KECIL atau panggil pattern inline (sesuai pilihan helper-extract di RESEARCH "Claude's Discretion"):

```typescript
/**
 * Worker submit exam 2-step (StartExam → ExamSummary → Results).
 * Equivalent to inline pattern di examMatrix.ts:186-204.
 *
 * Phase 316 SURF-316-A fix: arm waitForURL BEFORE click (race-tolerant).
 * Step 2 confirm() dialog: arm `page.once('dialog', d => d.accept())` BEFORE click.
 */
export async function submitExamTwoStep(page: Page): Promise<void> {
  await Promise.all([
    page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
    page.click('#reviewSubmitBtn'),
  ]);
  page.once('dialog', dialog => dialog.accept());
  await Promise.all([
    page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
    page.click('button[type="submit"]:has-text("Kumpulkan")'),
  ]);
}
```

---

### `tests/e2e/helpers/wizardSelectors.ts` (MODIFY — selectors, static config)

**Analog:** Existing file itself (Phase 307+308 structure — extend, jangan refactor).

**Existing structure pattern** (full file — 28 lines, lihat read above):

```typescript
// Phase 307 — DOM ID selectors untuk Step 2 panel "Peserta Terpilih"
// Centralized selector constants — single source of truth untuk Playwright tests.
// IDs match production markup di Views/Admin/CreateAssessment.cshtml.

export const selectors = {
  // Phase 307 panel (Wave 1 markup — sesudah line 309 #userCheckboxContainer)
  panelWrapper: '#selected-participants-panel-wrapper',
  // ...

  // Phase 308 — PrePost Wizard Validation Fix
  createForm: '#createAssessmentForm',
  assessmentTypeInput: '#assessmentTypeInput',
  // ...
} as const;
```

**Extension pattern (Phase 317 add — preserve existing keys + add new const exports)**:

Karena existing `selectors` adalah single const dengan mixed scope, dan Phase 317 adalah 3 domain berbeda (Wizard, ManagePackageQuestions form, ExtraTime modal), **rekomendasi: tambah const baru terpisah** di file SAMA — bukan inject ke `selectors` existing (preserve namespace dan blame history):

```typescript
// Phase 307+308 (existing — JANGAN refactor)
export const selectors = {
  // ... existing keys preserved ...
} as const;

// Phase 317 — Wizard 4-step lengkap (selain Step 2 + 4 yang sudah ada di `selectors`)
// Source: Views/Admin/CreateAssessment.cshtml lines 77-815 (verified 2026-05-11)
export const wizardSelectors = {
  step1: '#step-1', step2: '#step-2', step3: '#step-3', step4: '#step-4',
  pill1: '#pill-1', pill2: '#pill-2', pill3: '#pill-3', pill4: '#pill-4',
  btnNext1: '#btnNext1', btnNext2: '#btnNext2', btnNext3: '#btnNext3',
  btnPrev2: '#btnPrev2', btnPrev3: '#btnPrev3', btnPrev4: '#btnPrev4',
  btnSubmit: '#btnSubmit',

  // Step 1 fields
  category: '#Category', title: '#Title',
  assessmentType: '#assessmentTypeInput',

  // Step 2 fields (selain panelWrapper di `selectors`)
  userContainer: '#userCheckboxContainer',
  userCheckItem: '.user-check-item',
  userCheckbox: 'input.user-checkbox',
  selectedCountBadge: '#selectedCountBadge',
  selectAllBtn: '#selectAllBtn',
  deselectAllBtn: '#deselectAllBtn',

  // Step 3 fields
  schedDateInput: '#schedDateInput',
  schedTimeInput: '#schedTimeInput',
  durationMinutes: '#DurationMinutes',
  ewcdDateInput: '#ewcdDateInput',
  ewcdTimeInput: '#ewcdTimeInput',
  status: '#Status',
  passPercentage: '#PassPercentage',
  allowAnswerReview: '#AllowAnswerReview',
  generateCertificate: '#GenerateCertificate',
  isTokenRequired: '#IsTokenRequired',
  accessToken: '#AccessToken',
  validUntil: '#ValidUntil',

  // Submit modal
  successModal: '#successModal',
  modalManageBtn: '#modal-manage-btn',
  createdAssessmentData: '#createdAssessmentData',
} as const;

// Phase 317 — ManagePackageQuestions form
// Source: Views/Admin/ManagePackageQuestions.cshtml lines 117-458 (verified 2026-05-11)
export const questionFormSelectors = {
  formCard: '#questionFormCard',
  formTitle: '#formTitle',
  questionForm: '#questionForm',
  editQuestionId: '#editQuestionId',
  questionType: '#QuestionType',
  questionText: '#questionText',
  optionsSection: '#optionsSection',
  maLabel: '#maLabel',
  rubrikSection: '#rubrikSection',
  rubrik: '#rubrik',
  maxCharacters: '#maxCharacters',
  scoreValue: '#scoreValue',
  elemenTeknis: '#elemenTeknis',
  submitBtn: '#submitBtn',
  cancelEditBtn: '#cancelEditBtn',
  optionA: '#option_A', optionB: '#option_B', optionC: '#option_C', optionD: '#option_D',
  correctA: '#correct_A', correctB: '#correct_B', correctC: '#correct_C', correctD: '#correct_D',
} as const;

// Phase 317 — AddExtraTime modal
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml lines 132-143 + 1462-1495
export const extraTimeSelectors = {
  triggerBtn: 'button[data-bs-target="#extraTimeModal"]',
  modal: '#extraTimeModal',
  select: '#extraTimeSelect',
  confirmBtn: '#btnConfirmExtraTime',
} as const;
```

**Naming convention pattern** (existing file convention — Phase 307/308):
- camelCase keys
- `'#elementId'` literal string values
- `as const` type guard di akhir
- Comment header per-phase batch dengan source citation line numbers

---

## Shared Patterns

### Login + Auth

**Source:** `tests/helpers/auth.ts` (full file — 16 lines)
**Apply to:** Every test entry point yang butuh login (`'hc'`, `'coachee'`, `'coachee2'`)

```typescript
import { login } from '../helpers/auth';
// ...
await login(page, 'hc');         // FLOW K-N entry (HC create)
await login(page, 'coachee');    // FLOW K-O worker actions
await login(page, 'coachee2');   // FLOW O — second participant kalau perlu
```

Login helper sudah `waitForURL('**/Home/**', { timeout: 15_000 })` — no need add nav assertion sendiri.

### Title Isolation

**Source:** `tests/helpers/utils.ts:16-18`
**Apply to:** Setiap test yang create assessment via wizard

```typescript
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
title = uniqueTitle('MA Exam');     // → "MA Exam 1715441234567"
const scheduleDate = today();        // → "2026-05-11"
```

Title unique per `Date.now()` — TIDAK collide bahkan saat parallel run (yang di-disable via `fullyParallel: false`).

### Browser Confirm Dialog Handling

**Source:** `tests/helpers/utils.ts:29-31` + `examMatrix.ts:197, 283`
**Apply to:**
- Worker SubmitExam step 2 (`button:has-text("Kumpulkan")` → onclick=confirm)
- Worker StartExam dari card (kalau ada confirm dialog)
- HC Finalize Essay Grading (`btn-finalize-grading` → confirm)
- HC AddExtraTime modal (kalau ada confirm — verify dari markup)

```typescript
// Pattern A: inline once-handler BEFORE click (preferred — local scope, lebih explicit)
page.once('dialog', dialog => dialog.accept());
await page.click('button[type="submit"]:has-text("Kumpulkan")');

// Pattern B: autoConfirm helper (DRY tapi kurang explicit untuk reader)
autoConfirm(page);
await someButton.click();
```

Preferred: Pattern A untuk konsistensi dengan examMatrix.ts:197 (Phase 316 SURF-316-A fix).

### Race-Tolerant Navigation

**Source:** `examMatrix.ts:191-201` (Phase 316 SURF-316-A fix)
**Apply to:** Setiap submit/click yang trigger navigation

```typescript
// WAJIB arm waitForURL BEFORE click (race-tolerant pattern — exam313.ts precedent).
// JANGAN: await click; await waitForURL — bisa race "Target page closed" error.
await Promise.all([
  page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
  page.click('#reviewSubmitBtn'),
]);
```

### SignalR Hub Readiness Gate

**Source:** `examMatrix.ts:94-103` (Pitfall 1 mitigation)
**Apply to:** Setiap test yang fill answer di `/CMP/StartExam/{id}` (FLOW K4, L4, M4, N2, O2)

```typescript
await page.waitForFunction(
  () => {
    const w = window as unknown as { assessmentHub?: { state?: string } };
    return w.assessmentHub?.state === 'Connected';
  },
  undefined,
  { timeout: 10_000 }
);
```

### Defensive Context Close

**Source:** `assessment-matrix.spec.ts:168-177`
**Apply to:** FLOW O multi-context test (HC + worker simultan)

```typescript
try {
  // ... test body
} finally {
  await ctxWorker.close().catch(() => { /* already closed — non-fatal */ });
  await ctxHc.close().catch(() => { /* already closed — non-fatal */ });
}
```

### Verify Result Page (race-tolerant 3-badge)

**Source:** `examMatrix.ts:308-310` (hardened Phase 317 Task 2 bonus)
**Apply to:** FLOW K5, L6, M5, N3 result verification

```typescript
await expect(
  page.locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()
).toBeVisible({ timeout: 10_000 });
```

### Save Indicator Wait (per-answer save)

**Source:** `examMatrix.ts:126-128` (MC/MA) + `examMatrix.ts:171-179` (Essay text-check vs visibility)
**Apply to:** FLOW K4 (MA), L4 (Essay), M4 (Mixed), N2 (MC default)

```typescript
// MC + MA — visibility check OK (indicator visible saat saved)
await page.locator('#saveIndicatorText')
  .filter({ hasText: /saved|tersimpan/i })
  .waitFor({ timeout: 5_000 });

// Essay — TEXT-CHANGE check (bukan visibility) karena fade-out race (Phase 317 Task 2 bonus)
const prevText = await page.evaluate(() => {
  const el = document.getElementById('saveIndicatorText');
  return (el?.textContent || '').trim();
});
await page.fill(`textarea.exam-essay[data-question-id="${q.id}"]`, answer);
await page.waitForFunction(
  (prev) => {
    const el = document.getElementById('saveIndicatorText');
    const text = ((el as HTMLElement)?.textContent || '').trim();
    return text !== prev && /saved|tersimpan/i.test(text);
  },
  prevText,
  { timeout: 7_500 }
);
```

### Essay HC Grading (REUSE — jangan duplikasi)

**Source:** `examMatrix.ts:225-290` (hardened Phase 317 Task 2 bonus — `[data-session-id]` targeting)
**Apply to:** FLOW L5, M5 (essay grading step)

```typescript
import { gradeEssaysAsHc } from './helpers/examMatrix';
// gradeEssaysAsHc terima ScenarioConfig — kalau exam-types.spec.ts ngga pakai ScenarioConfig,
// pertimbangkan: (a) construct adapter object, atau (b) ekspor inline-grader baru di
// examTypes.ts yang lebih lightweight (no sibling-pool assumption).
//
// REKOMENDASI per RESEARCH "Don't Hand-Roll" line 333: call adapter di examTypes.ts yang
// build ScenarioConfig minimal dari (title, category, scheduleDate, sessionId, qId, score).
```

### URLSearchParams Pattern (MonitoringDetail navigation)

**Source:** `examMatrix.ts:230-235`
**Apply to:** FLOW L5, M5 (HC grade), FLOW O3 (HC add extra time)

```typescript
const baseUrl = pageHc.url() || 'http://localhost:5277';
const url = new URL('/Admin/AssessmentMonitoringDetail', baseUrl);
url.searchParams.set('title', cfg.title);
url.searchParams.set('category', cfg.category);
url.searchParams.set('scheduleDate', cfg.scheduleDate);
await pageHc.goto(url.toString());
```

---

## No Analog Found

Semua 3 file PUNYA analog presisi di codebase. Tidak ada gap pattern.

| File | Status |
|------|--------|
| `tests/e2e/exam-types.spec.ts` | Analog: `assessment-matrix.spec.ts` (exact). Berbeda strategi (hard-assert vs softAssert+collector) tapi struktur describe + context isolation + multi-page identical. |
| `tests/e2e/helpers/examTypes.ts` | Analog: `examMatrix.ts` (exact). POM-flat export pattern + JSDoc + source citation header reusable verbatim. |
| `tests/e2e/helpers/wizardSelectors.ts` | Analog: itself (extend pattern). Phase 307+308 multi-batch additive structure proven across 2 phases. |

---

## Key Patterns Identified

1. **POM-flat helpers, bukan POM-class** — semua existing helpers (`examMatrix.ts`, `exam313.ts`) pakai flat exported async functions dengan typed opts param. Hindari class-based Page Object Model untuk konsistensi.

2. **Source code citation di file header** — setiap helper file mulai dengan comment block listing `Source: Path:lineRange — purpose`. Wajib untuk `examTypes.ts`.

3. **JSDoc per function dengan pitfall mitigation explicit** — `examMatrix.ts` function docblock cite RESEARCH Pitfall # + mitigation strategy. Apply ke setiap function di `examTypes.ts` (wizard, addQuestion, addExtraTime).

4. **Race-tolerant 2-step submit (Phase 316 SURF-316-A fix)** — `Promise.all([waitForURL, click])` + `page.once('dialog', d => d.accept())` BEFORE step 2 click. Battle-tested.

5. **SignalR readiness gate** — `window.assessmentHub.state === 'Connected'` waitForFunction WAJIB sebelum answer fill di StartExam page. Sudah jadi standard di examMatrix.ts.

6. **Selectors centralized in `wizardSelectors.ts`** — single source of truth pattern; extend dengan const baru per-phase (bukan refactor existing).

7. **Visible-cue WAITFOR, bukan waitForTimeout** — setelah `selectOption`/`click` yang trigger JS handler async, await DOM cue spesifik (`#maLabel`, `#rubrikSection`, dll). Pitfall 2 mitigation.

8. **Multi-context untuk concurrent HC + worker** — `browser.newContext()` × 2 dengan defensive close di finally. FLOW O only.

9. **Hard-assert untuk acceptance test (exam-types.spec.ts), bukan softAssert+collector** — collector pattern adalah matrix-discovery-specific. Acceptance test pakai standar Playwright `expect(...).toBe...()` untuk first-fail.

10. **Reuse hardened helpers** — `gradeEssaysAsHc`, `verifyResultPage`, `submitExamTwoStep` pattern sudah hardened Phase 316/317 Task 2. JANGAN re-implement — call directly atau wrap thin adapter.

---

## Metadata

**Analog search scope:**
- `tests/e2e/` (specs + helpers — 3 files inspected)
- `tests/helpers/` (auth, utils, accounts — 3 files inspected)

**Files scanned for analog selection:**
- `tests/e2e/assessment-matrix.spec.ts` (437 lines — full read)
- `tests/e2e/helpers/examMatrix.ts` (317 lines — full read)
- `tests/e2e/helpers/wizardSelectors.ts` (28 lines — full read)
- `tests/e2e/exam-taking.spec.ts` (lines 1-300 read — legacy FLOW A reference only, NOT analog)
- `tests/helpers/auth.ts` (16 lines — shared pattern)
- `tests/helpers/utils.ts` (32 lines — shared pattern)
- `tests/helpers/accounts.ts` (15 lines — shared pattern)
- `tests/e2e/helpers/matrixReport.ts` (head 60 lines — sanity check softAssert pattern NOT applicable)

**Pattern extraction date:** 2026-05-11

**Confidence:** HIGH — semua analog full-read; selectors di RESEARCH cross-verified dengan markup yang sudah dibaca di Phase 317 research session.
