// Phase 386 PXF-02 (F-DEV-01) — e2e reject path: simpan soal Single/Multiple tanpa opsi berisi → ditolak.
//
// Wave 5 (Plan 06): UN-GATED — validasi server (CreateQuestion/EditQuestion via
// QuestionOptionValidator) sudah live sejak Wave 2 (Plan 03). Spec self-contained:
// backup → seed (1 session + 1 paket KOSONG, prefix [OPTVAL386]) → resolve packageId
// → run → restore (SEED_WORKFLOW, pola essay-grading-384.spec.ts).
//
// Selector dipin dari tests/e2e/helpers/wizardSelectors.ts:109-126 (questionFormSelectors):
//   #QuestionType, #questionText, #option_A..D, #correct_A..D, #submitBtn.
// Reject path: CreateQuestion redirect ke ManagePackageQuestions dengan TempData["Error"]
//   → di-render sebagai <div class="alert alert-danger ...">@TempData["Error"]</div>
//   (ManagePackageQuestions.cshtml:32-34).
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal, Phase 355)
//   2) cd tests; npx playwright test option-validation-386 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';
import { questionFormSelectors } from './helpers/wizardSelectors';

let snapshotPath: string;
let packageId: number;

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

test.describe.configure({ mode: 'serial' });

test.describe('PXF-02 Option Validation — reject soal tanpa opsi (F-DEV-01)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre386optval-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW mitigation).
    await db.backup(snapshotPath);

    // 3. Seed sesi + paket kosong (prefix [OPTVAL386]).
    await db.execScript(path.resolve(__dirname, '../sql/option-validation-386-seed.sql'));

    // 4. Layer 1: konfirmasi seeded.
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386]%'");
    expect(n, 'Layer 1: option-validation session seeded').toBeGreaterThan(0);

    // 5. Resolve packageId nyata dari DB (hindari hardcode).
    packageId = await db.queryScalar(
      "SELECT TOP 1 p.Id FROM AssessmentPackages p "
      + "JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id "
      + "WHERE s.Title LIKE '[[]OPTVAL386]%'");
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  test('MC: correct flag tanpa opsi berisi → .alert-danger + soal tidak tersimpan', async ({ page }) => {
    await loginAny(page, 'admin');

    await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
    await page.locator(questionFormSelectors.formCard).waitFor({ state: 'visible' });

    // Paket di-seed KOSONG → 0 soal sebelum submit.
    const soalText = '[OPTVAL386] Soal tanpa opsi (harus ditolak)';
    await expect(page.locator(`text=${soalText}`)).toHaveCount(0);

    // MultipleChoice, tandai #correct_A benar, BIARKAN semua #option_A..D kosong → malformed.
    await page.selectOption(questionFormSelectors.questionType, 'MultipleChoice');
    await page.locator(questionFormSelectors.optionsSection).waitFor({ state: 'visible', timeout: 3_000 });
    await page.fill(questionFormSelectors.questionText, soalText);
    await page.locator(questionFormSelectors.correctA).check();   // correctA benar, optionA kosong
    await page.fill(questionFormSelectors.scoreValue, '10');
    await page.locator(questionFormSelectors.submitBtn).click();
    await page.waitForLoadState('networkidle');

    // Assert reject: .alert-danger terlihat, pesan minimal-2-opsi / berisi-teks.
    const alert = page.locator('.alert-danger').first();
    await expect(alert).toBeVisible();
    await expect(alert).toHaveText(/minimal 2 opsi|berisi teks/i);

    // Soal malformed TIDAK muncul di daftar soal (paket tetap kosong).
    await expect(page.locator(`text=${soalText}`)).toHaveCount(0);
  });
});
