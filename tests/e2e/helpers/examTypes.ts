// Phase 317 — POM-flat helpers untuk MA/Essay/Mixed E2E via HC UI creation.
// Pattern reference: tests/e2e/helpers/examMatrix.ts (Phase 315/316 — flat export functions,
// JSDoc docblock per function, source citation di header).
//
// Source code citations:
//  - Views/Admin/CreateAssessment.cshtml:77-815 — 4-step wizard structure, field IDs, success modal
//  - Views/Admin/ManagePackageQuestions.cshtml:117-458 — right-pane form, QuestionType switch JS
//  - Views/CMP/StartExam.cshtml:200-203 — #reviewSubmitBtn form submit
//  - Views/CMP/ExamSummary.cshtml — Kumpulkan Ujian button + confirm() dialog
//
// Wave 0 assumption verifications (317-RESEARCH.md A1-A7):
//  - A4 question order persistence — verified di smoke wave-0 block
//  - A5 window.timerStartRemaining scope — verified di smoke wave-0 block

import { Page, expect } from '@playwright/test';
import { wizardSelectors, questionFormSelectors } from './wizardSelectors';

export type QuestionInput =
  | { type: 'MultipleChoice'; text: string; options: [string, string, string, string]; correctIndex: 0 | 1 | 2 | 3; score: number }
  | { type: 'MultipleAnswer'; text: string; options: [string, string, string, string]; correctIndices: (0 | 1 | 2 | 3)[]; score: number }
  | { type: 'Essay'; text: string; rubrik: string; maxCharacters?: number; score: number };

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
}

/**
 * HC create assessment via 4-step Bootstrap wizard.
 *
 * Pattern source: Views/Admin/CreateAssessment.cshtml lines 77-815 (verified 2026-05-11).
 * Pitfalls mitigated:
 *  - P3 (RESEARCH.md): successModal pakai data-bs-backdrop="static" — caller WAJIB dismiss
 *    via `page.locator(wizardSelectors.modalManageBtn).click()` sebelum nav berikutnya.
 *  - P6: pakai `#schedDateInput` (BUKAN `#ScheduleDate`) — wizard dual-input strategy.
 *  - A7 (Open Q): selector `.user-check-item[data-email="..."]` dengan literal "@" valid
 *    di Playwright CSS attribute selector (string quoted). Fallback ke text-based kalau gagal.
 *
 * @param page HC user page (sudah login)
 * @param opts wizard fields
 */
export async function createAssessmentViaWizard(page: Page, opts: CreateAssessmentOpts): Promise<void> {
  await page.goto('/Admin/CreateAssessment');

  // STEP 1 — Title + Category + Type (default Standard)
  await page.locator(wizardSelectors.step1).waitFor({ state: 'visible' });
  await page.selectOption(wizardSelectors.category, opts.category);
  await page.fill(wizardSelectors.title, opts.title);
  await page.locator(wizardSelectors.btnNext1).click();

  // STEP 2 — Peserta selection
  await page.locator(wizardSelectors.step2).waitFor({ state: 'visible', timeout: 5_000 });
  for (const email of opts.participantEmails) {
    const attrSelector = `${wizardSelectors.userCheckItem}[data-email="${email}"] ${wizardSelectors.userCheckbox}`;
    try {
      await page.locator(attrSelector).check({ timeout: 3_000 });
    } catch {
      // Fallback A7 mitigation — text-based selector dengan email local-part
      const localPart = email.split('@')[0];
      const fallback = page.locator(`${wizardSelectors.userCheckItem}:has-text("${localPart}") ${wizardSelectors.userCheckbox}`);
      // eslint-disable-next-line no-console
      console.warn(`[examTypes] attribute selector failed for ${email}, falling back to text-based`);
      await fallback.first().check();
    }
  }
  await page.locator(wizardSelectors.btnNext2).click();

  // STEP 3 — Schedule + Settings
  await page.locator(wizardSelectors.step3).waitFor({ state: 'visible', timeout: 5_000 });
  await page.fill(wizardSelectors.schedDateInput, opts.scheduleDate);
  await page.fill(wizardSelectors.schedTimeInput, opts.scheduleTime ?? '00:01');
  await page.fill(wizardSelectors.durationMinutes, String(opts.durationMinutes));
  await page.fill(wizardSelectors.ewcdDateInput, opts.ewcdDate ?? opts.scheduleDate);
  await page.fill(wizardSelectors.ewcdTimeInput, opts.ewcdTime ?? '23:59');
  await page.selectOption(wizardSelectors.status, 'Open');
  await page.fill(wizardSelectors.passPercentage, String(opts.passPercentage));

  // AllowAnswerReview default = TRUE (per CreateAssessmentController Add() get default + asp-for binding)
  if (opts.allowAnswerReview) {
    await page.locator(wizardSelectors.allowAnswerReview).check();
  } else {
    await page.locator(wizardSelectors.allowAnswerReview).uncheck();
  }
  if (opts.generateCertificate) {
    await page.locator(wizardSelectors.generateCertificate).check();
  }
  await page.locator(wizardSelectors.btnNext3).click();

  // STEP 4 — Summary + Submit
  await page.locator(wizardSelectors.step4).waitFor({ state: 'visible', timeout: 5_000 });
  await page.locator(wizardSelectors.btnSubmit).click();

  // Success modal (static backdrop) — caller harus dismiss via modal-manage-btn click sebelum nav
  await page.locator(`${wizardSelectors.successModal}.show`).waitFor({ state: 'visible', timeout: 15_000 });
}

/**
 * HC create 1 package di ManagePackages page setelah wizard create assessment.
 *
 * Pattern source: Views/Admin/ManagePackages.cshtml lines 168-194 (verified 2026-05-11 W0.1 fail).
 * Wizard tidak auto-create package — assessment landing punya "Packages (0). No packages yet."
 * Caller WAJIB call ini sebelum addQuestionViaForm.
 *
 * Form: POST `/Admin/CreatePackage` dengan hidden `assessmentId` + visible `packageName`.
 * Server redirect kembali ke ManagePackages dengan TempData.Success → page reload + link
 * `a[href*="ManagePackageQuestions"]` muncul.
 *
 * @param page HC user page yang sudah arrive di /Admin/ManagePackages?assessmentId={id}
 * @param packageName default 'Paket A' (mengikuti naming convention placeholder Razor view)
 * @returns extracted packageId dari link ManagePackageQuestions
 */
export async function createDefaultPackage(page: Page, packageName = 'Paket A'): Promise<number> {
  await page.locator('input[name="packageName"]').fill(packageName);
  await page.locator('button[type="submit"]:has-text("Create Package")').click();
  await page.waitForLoadState('networkidle');

  // Verify alert-success + package count incremented
  await expect(page.locator('.alert-success').first()).toBeVisible({ timeout: 5_000 });

  // Extract packageId dari link `a[href*="ManagePackageQuestions"]`
  const manageQLink = page.locator('a[href*="ManagePackageQuestions"]').first();
  await expect(manageQLink).toBeVisible({ timeout: 5_000 });
  const href = await manageQLink.getAttribute('href');
  const match = href?.match(/packageId=(\d+)/);
  if (!match) {
    throw new Error(`createDefaultPackage: unable to extract packageId from href="${href}"`);
  }
  return parseInt(match[1], 10);
}

/**
 * HC add 1 question via ManagePackageQuestions right-pane form.
 *
 * Pattern source: Views/Admin/ManagePackageQuestions.cshtml lines 117-458 (verified 2026-05-11).
 * Pitfall 2 mitigation: setelah `selectOption('#QuestionType', ...)`, WAJIB wait visible cue
 * spesifik tipe (maLabel/rubrikSection/optionsSection) supaya JS `applyQTypeSwitch` flip
 * radio↔checkbox selesai sebelum action berikutnya.
 *
 * Form POST → server redirect ke ManageQuestions → page reload → form reset ke MC default.
 *
 * @param page HC user page (sudah login)
 * @param packageId target package ID
 * @param q QuestionInput discriminated union
 */
export async function addQuestionViaForm(page: Page, packageId: number, q: QuestionInput): Promise<void> {
  // Action route: [Route("Admin/[action]")] on AssessmentAdminController → /Admin/ManagePackageQuestions?packageId={N}
  // (RESEARCH said `/Admin/ManageQuestions` — verified salah 2026-05-11; actual action name `ManagePackageQuestions`.)
  await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
  await page.locator(questionFormSelectors.formCard).waitFor({ state: 'visible' });

  await page.selectOption(questionFormSelectors.questionType, q.type);

  // Pitfall 2 — wait JS handler applyQTypeSwitch flip selesai
  if (q.type === 'MultipleAnswer') {
    await page.locator(questionFormSelectors.maLabel).waitFor({ state: 'visible', timeout: 3_000 });
  } else if (q.type === 'Essay') {
    await page.locator(questionFormSelectors.rubrikSection).waitFor({ state: 'visible', timeout: 3_000 });
  } else {
    await page.locator(questionFormSelectors.optionsSection).waitFor({ state: 'visible', timeout: 3_000 });
    await expect(page.locator(questionFormSelectors.maLabel)).toBeHidden();
  }

  await page.fill(questionFormSelectors.questionText, q.text);

  if (q.type === 'Essay') {
    await page.fill(questionFormSelectors.rubrik, q.rubrik);
    if (q.maxCharacters) {
      await page.fill(questionFormSelectors.maxCharacters, String(q.maxCharacters));
    }
  } else {
    const optionFields = [
      questionFormSelectors.optionA,
      questionFormSelectors.optionB,
      questionFormSelectors.optionC,
      questionFormSelectors.optionD,
    ];
    const correctFields = [
      questionFormSelectors.correctA,
      questionFormSelectors.correctB,
      questionFormSelectors.correctC,
      questionFormSelectors.correctD,
    ];
    for (let i = 0; i < 4; i++) {
      await page.fill(optionFields[i], q.options[i]);
    }
    if (q.type === 'MultipleChoice') {
      await page.locator(correctFields[q.correctIndex]).check();
    } else {
      for (const idx of q.correctIndices) {
        await page.locator(correctFields[idx]).check();
      }
    }
  }

  await page.fill(questionFormSelectors.scoreValue, String(q.score));
  await page.locator(questionFormSelectors.submitBtn).click();
  await page.waitForLoadState('networkidle');

  // Verify post-submit success (Open Q4 mitigation).
  // Strict-mode fix: page mungkin punya 2 alerts simultan — global toast (b-06zfpy70xb scoped) +
  // inline TempData alert. Pakai .first() supaya tidak strict mode violation.
  await expect(page.locator('.alert-success, .alert.alert-success').first()).toBeVisible({ timeout: 5_000 });
}

/**
 * Coachee submit exam — 2-step flow (StartExam → ExamSummary → Results).
 *
 * Source: tests/e2e/helpers/examMatrix.ts:186-204 (Phase 316 SURF-316-A hardening).
 * Critical: dialog handler MUST be armed BEFORE clicking "Kumpulkan Ujian" — button onclick
 * fires browser confirm() synchronously.
 *
 * @param page coachee page at /CMP/StartExam/{id}
 */
export async function submitExamTwoStep(page: Page): Promise<void> {
  await Promise.all([
    page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
    page.click('#reviewSubmitBtn'),
  ]);
  page.once('dialog', (dialog) => dialog.accept());
  await Promise.all([
    page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
    page.click('button[type="submit"]:has-text("Kumpulkan")'),
  ]);
}
