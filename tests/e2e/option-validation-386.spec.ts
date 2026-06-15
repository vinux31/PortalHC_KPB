// Phase 386 PXF-02 (F-DEV-01) — e2e reject path: simpan soal Single/Multiple tanpa opsi berisi → ditolak.
//
// RED/fixme: di-gate `test.fixme()` — validasi server (CreateQuestion/EditQuestion) dibangun Wave 2
// (Plan 06). UN-SKIP di Wave 2: ganti `test.fixme(` → `test(`. Sebelum itu suite tetap hijau.
//
// Selector dipin dari tests/e2e/helpers/wizardSelectors.ts:109-126 (questionFormSelectors):
//   #QuestionType, #questionText, #option_A..D, #correct_A..D, #submitBtn.
// Bila id berbeda saat runtime, lihat komentar // VERIFY.
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal, Phase 355)
//   2) cd tests; npx playwright test option-validation-386 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import { questionFormSelectors } from './helpers/wizardSelectors';

// Inline login — accept any redirect away dari /Account/Login (pola export-per-peserta.spec.ts).
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

test.describe('PXF-02 Option Validation — reject soal tanpa opsi (F-DEV-01)', () => {

  // RED Wave 0: di-skip via test.fixme — validasi dibangun Wave 2 (Plan 06). TODO un-skip di Wave 2.
  test.fixme('MC: correct flag tanpa opsi berisi → .alert-danger + soal tidak tersimpan', async ({ page }) => {
    await loginAny(page, 'admin');

    // VERIFY: ganti {packageId} dgn package nyata di DB Dev/lokal sebelum un-skip (lihat seed Wave 2).
    const packageId = 1; // VERIFY runtime
    await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
    await page.locator(questionFormSelectors.formCard).waitFor({ state: 'visible' });

    // Hitung jumlah soal SEBELUM submit (untuk assert "tidak bertambah").
    const beforeCount = await page.locator('.question-list-item').count(); // VERIFY selector list soal

    // MultipleChoice, tandai #correct_A benar, BIARKAN semua #option_A..D kosong → malformed.
    await page.selectOption(questionFormSelectors.questionType, 'MultipleChoice');
    await page.locator(questionFormSelectors.optionsSection).waitFor({ state: 'visible', timeout: 3_000 });
    await page.fill(questionFormSelectors.questionText, 'Soal tanpa opsi (harus ditolak)');
    await page.locator(questionFormSelectors.correctA).check();   // correctA benar, optionA kosong
    await page.fill(questionFormSelectors.scoreValue, '10');
    await page.locator(questionFormSelectors.submitBtn).click();
    await page.waitForLoadState('networkidle');

    // Assert reject: .alert-danger terlihat, pesan minimal-2-opsi / berisi-teks.
    await expect(page.locator('.alert-danger, .alert.alert-danger').first()).toBeVisible();
    await expect(page.locator('.alert-danger, .alert.alert-danger').first())
      .toHaveText(/minimal 2 opsi|berisi teks/i);

    // Soal malformed TIDAK muncul di daftar (count tak bertambah).
    const afterCount = await page.locator('.question-list-item').count(); // VERIFY selector list soal
    expect(afterCount).toBe(beforeCount);
  });
});
