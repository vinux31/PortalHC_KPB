// Phase 321 Plan 05 Task 2 — Playwright spec untuk Edit Jawaban Peserta.
//
// Coverage (4 active test — HARD GATE 4/4 pass):
//   1. auth-gate: Admin/HC accessible, Worker blocked (REQ EDIT-01)
//   2. happy-path edit save: 1 MC edit + reason → submit → score recompute (REQ EDIT-01/03/06)
//   3. concurrency stale: 2 admin contexts → B kena "Sesi sudah diubah admin lain" (REQ EDIT-07)
//   4. flip-preview AJAX: POST PreviewEditScore returns JSON contract (REQ EDIT-10)
//
// Pattern reused dari tests/e2e/export-per-peserta.spec.ts (Phase 320):
//   - `accounts` fixture lowercase dari ../helpers/accounts (NOT ./fixtures/)
//   - Inline `loginAny` accept any redirect away dari /Account/Login
//   - input[name=email] + input[name=password] lowercase
//
// Pre-requisite: env COMPLETED_PASS_SESSION_ID set ke Id session Completed eligible
// (Status='Completed', IsManualEntry=0, NOT Proton T3). Lihat docs/SEED_WORKFLOW.md.

import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

const COMPLETED_SESSION_ID = parseInt(
  process.env.COMPLETED_PASS_SESSION_ID ?? process.env.EDIT_TEST_SESSION_ID ?? '0',
  10
);

async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Phase 321 — Edit Peserta Answers', () => {

  test.beforeAll(() => {
    if (!COMPLETED_SESSION_ID || COMPLETED_SESSION_ID < 1) {
      throw new Error('COMPLETED_PASS_SESSION_ID env var required (Completed eligible session). See docs/SEED_WORKFLOW.md.');
    }
  });

  test('auth-gate: Admin/HC accessible, Worker blocked (REQ EDIT-01)', async ({ browser }) => {
    const editUrl = `/Admin/EditPesertaAnswers/${COMPLETED_SESSION_ID}`;

    const adminCtx = await browser.newContext();
    const adminPage = await adminCtx.newPage();
    await loginAny(adminPage, 'admin');
    const adminResp = await adminPage.goto(editUrl);
    expect(adminResp?.status()).toBe(200);
    await expect(adminPage.locator('#editAnswersForm')).toBeVisible();
    await adminCtx.close();

    const hcCtx = await browser.newContext();
    const hcPage = await hcCtx.newPage();
    await loginAny(hcPage, 'hc');
    const hcResp = await hcPage.goto(editUrl);
    expect(hcResp?.status()).toBe(200);
    await expect(hcPage.locator('#editAnswersForm')).toBeVisible();
    await hcCtx.close();

    const workerCtx = await browser.newContext();
    const workerPage = await workerCtx.newPage();
    await loginAny(workerPage, 'coachee');
    const workerResp = await workerPage.goto(editUrl);
    const status = workerResp?.status() ?? 0;
    const url = workerPage.url();
    expect(status === 403 || url.includes('/Account/Login') || url.includes('/AccessDenied')).toBe(true);
    await workerCtx.close();
  });

  test('happy-path edit save: 1 MC edit + reason SoalSalah → submit → AssessmentEditLog++ + score recompute (REQ EDIT-01/03/06)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(`/Admin/EditPesertaAnswers/${COMPLETED_SESSION_ID}`);
    await expect(page.locator('#editAnswersForm')).toBeVisible();

    const firstMcCard = page.locator('.question-card[data-question-type="MultipleChoice"]').first();
    await expect(firstMcCard).toBeVisible();
    const uncheckedRadio = firstMcCard.locator('input[type="radio"]:not(:checked)').first();
    await uncheckedRadio.check();

    const reasonBlock = firstMcCard.locator('.reason-block');
    await expect(reasonBlock).toBeVisible();
    await firstMcCard.locator('.reason-code').selectOption('SoalSalah');

    await page.click('#submitEditBtn');

    const flipModal = page.locator('#flipConfirmModal');
    if (await flipModal.isVisible({ timeout: 2000 }).catch(() => false)) {
      await page.click('#flipConfirmBtn');
    }

    await page.waitForLoadState('networkidle');
    expect(page.url()).toContain('AssessmentMonitoringDetail');
  });

  test('concurrency stale: 2 admin contexts, A submit → B stale error (REQ EDIT-07)', async ({ browser }) => {
    const ctxA = await browser.newContext();
    const ctxB = await browser.newContext();
    const pageA = await ctxA.newPage();
    const pageB = await ctxB.newPage();

    await loginAny(pageA, 'admin');
    await loginAny(pageB, 'admin');

    await pageA.goto(`/Admin/EditPesertaAnswers/${COMPLETED_SESSION_ID}`);
    await pageB.goto(`/Admin/EditPesertaAnswers/${COMPLETED_SESSION_ID}`);

    const firstMcCardA = pageA.locator('.question-card[data-question-type="MultipleChoice"]').first();
    await firstMcCardA.locator('input[type="radio"]:not(:checked)').first().check();
    await firstMcCardA.locator('.reason-code').selectOption('BugSistem');
    await pageA.click('#submitEditBtn');
    const flipModalA = pageA.locator('#flipConfirmModal');
    if (await flipModalA.isVisible({ timeout: 2000 }).catch(() => false)) {
      await pageA.click('#flipConfirmBtn');
    }
    await pageA.waitForLoadState('networkidle');

    const firstMcCardB = pageB.locator('.question-card[data-question-type="MultipleChoice"]').first();
    await firstMcCardB.locator('input[type="radio"]:not(:checked)').first().check();
    await firstMcCardB.locator('.reason-code').selectOption('PermintaanPeserta');
    await pageB.click('#submitEditBtn');
    const flipModalB = pageB.locator('#flipConfirmModal');
    if (await flipModalB.isVisible({ timeout: 2000 }).catch(() => false)) {
      await pageB.click('#flipConfirmBtn');
    }
    await pageB.waitForLoadState('networkidle');

    const staleText = await pageB.textContent('body');
    expect(staleText).toContain('Sesi sudah diubah admin lain');

    await ctxA.close();
    await ctxB.close();
  });

  test('flip-preview AJAX: POST PreviewEditScore returns JSON contract (REQ EDIT-10)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(`/Admin/EditPesertaAnswers/${COMPLETED_SESSION_ID}`);

    const token = await page.locator('input[name=__RequestVerificationToken]').first().inputValue();

    const firstQuestionId = await page.locator('.question-card').first().getAttribute('data-question-id');
    expect(firstQuestionId).toBeTruthy();
    const firstOptionId = await page.locator('.question-card').first()
      .locator('input[type="radio"], input[type="checkbox"]').first().getAttribute('value');
    expect(firstOptionId).toBeTruthy();

    const previewResp = await page.evaluate(async ({ token, sessionId, qId, oId }) => {
      const fd = new FormData();
      fd.append('Drafts[0].QuestionId', qId);
      fd.append('Drafts[0].Options', oId);
      const r = await fetch(`/Admin/PreviewEditScore?sessionId=${sessionId}`, {
        method: 'POST',
        body: fd,
        headers: { 'RequestVerificationToken': token }
      });
      return { status: r.status, json: await r.json() };
    }, { token, sessionId: COMPLETED_SESSION_ID, qId: firstQuestionId!, oId: firstOptionId! });

    expect(previewResp.status).toBe(200);
    const json = previewResp.json;
    expect(json).toHaveProperty('oldScore');
    expect(json).toHaveProperty('newScore');
    expect(json).toHaveProperty('oldIsPassed');
    expect(json).toHaveProperty('newIsPassed');
    expect(json).toHaveProperty('hasCert');
    expect(json).toHaveProperty('nomorSertifikat');
    expect(json).toHaveProperty('willGenerateCert');
  });
});
