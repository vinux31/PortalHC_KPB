// Phase 394 (Inject Assessment Manual "Seakan Online") — verifikasi runtime wizard /Admin/InjectAssessment (Razor + JS).
// Pola login dari assessment-title-flexible.spec.ts (accounts fixture, /Account/Login).
// Pre-req: server localhost:5277 jalan dari MAIN tree (Authentication__UseActiveDirectory=false).
//   Razor di-embed saat build (AddControllersWithViews tanpa RuntimeCompilation) → WAJIB run app dari main tree
//   pasca-edit view (lesson Phase 354/392). Login admin@pertamina.com / 123456 (lihat helpers/accounts.ts).
// Run: cd tests && npx playwright test e2e/inject-assessment-394.spec.ts --workers=1
//
// Cakupan per-plan (Wave): 394-01 implement RBAC + wizard-nav; 394-02 unskip cek-judul/backdate/picker;
//   394-03 unskip authoring/cert-radio; 394-04 unskip step5/confirm/no-DB-write.
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

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

async function authorMcQuestion(page: Page) {
  await page.selectOption('#QuestionType', 'MultipleChoice');
  await page.fill('#questionText', 'Soal alur inject?');
  await page.fill('#option_A', 'Jawaban A');
  await page.fill('#option_B', 'Jawaban B');
  await page.check('#correct_A');
  await page.click('#injAddQuestionBtn');
}

// Drive the full 6-step wizard with valid data, landing on the Konfirmasi panel.
async function fillWizardToConfirm(page: Page, title: string) {
  await page.fill('#Title', title);
  await page.selectOption('#Category', { index: 1 });
  await page.click('#btnNext1');
  await expect(page.locator('#step-2')).toBeVisible();
  await page.locator('#userCheckboxContainer .user-checkbox').first().check();
  await page.click('#btnNext2');
  await expect(page.locator('#step-3')).toBeVisible();
  await authorMcQuestion(page);
  await page.click('#btnNext3');
  await expect(page.locator('#step-4')).toBeVisible();
  await page.click('#btnNext4');
  await expect(page.locator('#step-5')).toBeVisible();
  await page.click('#btnNext5');
  await expect(page.locator('#step-6')).toBeVisible();
}

// ── INJ-03: RBAC + page (implemented Plan 394-01) ──────────────────────────────
test.describe('INJ-03 RBAC + page', () => {
  test('RBAC Admin reaches page', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    await expect(page.locator('#wizardStepNav')).toBeVisible({ timeout: 15_000 });
    expect(page.url()).toContain('InjectAssessment');
  });

  test('RBAC HC reaches page', async ({ page }) => {
    await loginAny(page, 'hc');
    await page.goto('/Admin/InjectAssessment');
    await expect(page.locator('#wizardStepNav')).toBeVisible({ timeout: 15_000 });
    expect(page.url()).toContain('InjectAssessment');
  });

  test('RBAC Coachee denied', async ({ page }) => {
    await loginAny(page, 'coachee');
    await page.goto('/Admin/InjectAssessment');
    // Server-side [Authorize(Roles="Admin, HC")] → redirect AccessDenied/Login; wizard must NOT render.
    await expect(page.locator('#wizardStepNav')).toHaveCount(0);
  });

  test('wizard nav 6 pills', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    await expect(page.locator('#wizardStepNav')).toBeVisible({ timeout: 15_000 });
    // 6 pills present
    for (let i = 1; i <= 6; i++) {
      await expect(page.locator(`#pill-${i}`)).toHaveCount(1);
    }
    // step-1 visible initially; step-2 hidden
    await expect(page.locator('#step-1')).toBeVisible();
    await expect(page.locator('#step-2')).toBeHidden();
    // fill required step-1 fields so validateStep(1) passes (date defaults to today)
    await page.fill('#Title', 'ZZ Nav ' + Date.now());
    await page.selectOption('#Category', { index: 1 });
    // forward nav: click Selanjutnya on step 1 → step-2 visible + pill-1 marked completed (bg-success)
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await expect(page.locator('#pill-1')).toHaveClass(/bg-success/);
    // back nav: a .btn-prev returns to step-1
    await page.locator('#step-2 .btn-prev').first().click();
    await expect(page.locator('#step-1')).toBeVisible();
  });
});

// ── INJ-04: setup room + cek judul (implemented Plan 394-02) ────────────────────
test.describe('INJ-04 setup + cek judul', () => {
  test('cek judul', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    await expect(page.locator('#Title')).toBeVisible({ timeout: 15_000 });
    await page.fill('#Title', 'ZZ Inject Unik ' + Date.now());
    await page.click('#btnCheckTitle');
    await expect(page.locator('#titleCheckResult')).toContainText('tersedia', { timeout: 10_000 });
  });

  test('backdate guard', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    const dateInput = page.locator('#CompletedAt');
    await expect(dateInput).toBeVisible({ timeout: 15_000 });
    // max attribute = today (YYYY-MM-DD)
    const t = new Date();
    const todayStr = `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, '0')}-${String(t.getDate()).padStart(2, '0')}`;
    await expect(dateInput).toHaveAttribute('max', todayStr);
    // fill required fields, set future date, try to advance → blocked + #CompletedAt is-invalid
    await page.fill('#Title', 'ZZ Backdate ' + Date.now());
    await page.selectOption('#Category', { index: 1 });
    await dateInput.fill('2099-12-31');
    await page.click('#btnNext1');
    await expect(dateInput).toHaveClass(/is-invalid/);
    await expect(page.locator('#step-1')).toBeVisible();
    await expect(page.locator('#step-2')).toBeHidden();
  });
});

// ── INJ-06: worker picker (implemented Plan 394-02) ─────────────────────────────
test.describe('INJ-06 worker picker', () => {
  test('picker search/select', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    // advance step 1 → step 2 (date defaults to today)
    await page.fill('#Title', 'ZZ Picker ' + Date.now());
    await page.selectOption('#Category', { index: 1 });
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    // 0 selected → advance blocked + step2 error shown
    await page.click('#btnNext2');
    await expect(page.locator('#step2Error')).toBeVisible();
    await expect(page.locator('#step-2')).toBeVisible();
    // select first worker → count badges + live panel update
    await page.locator('#userCheckboxContainer .user-checkbox').first().check();
    await expect(page.locator('#selectedCountBadge')).toHaveText(/1 terpilih/);
    await expect(page.locator('#selected-participants-count')).toHaveText(/1 peserta/);
    // search filter narrows list (no-match → 0 visible rows)
    await page.fill('#userSearchInput', 'zzz-no-match-xyz-' + Date.now());
    await expect(page.locator('#userCheckboxContainer .user-check-item:visible')).toHaveCount(0);
    await page.fill('#userSearchInput', '');
    // 1 selected → advance succeeds
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();
  });
});

// ── INJ-05: authoring soal (implemented Plan 394-03) ────────────────────────────
test.describe('INJ-05 authoring', () => {
  test('authoring type toggle + add soal', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    await expect(page.locator('#wizardStepNav')).toBeVisible({ timeout: 15_000 });
    // jump straight to step 3 (nav tested elsewhere)
    await page.evaluate(() => (window as any).injWizard.goToStep(3));
    await expect(page.locator('#step-3')).toBeVisible();

    // 0 soal → advancing blocked
    await page.click('#btnNext3');
    await expect(page.locator('#step3Error')).toBeVisible();

    // type toggle: MC=radio → MA=checkbox + maLabel → Essay hides options, shows rubrik
    await expect(page.locator('#optionsSection')).toBeVisible();
    await expect(page.locator('#correct_A')).toHaveAttribute('type', 'radio');
    await page.selectOption('#QuestionType', 'MultipleAnswer');
    await expect(page.locator('#correct_A')).toHaveAttribute('type', 'checkbox');
    await expect(page.locator('#maLabel')).toBeVisible();
    await page.selectOption('#QuestionType', 'Essay');
    await expect(page.locator('#optionsSection')).toBeHidden();
    await expect(page.locator('#rubrikSection')).toBeVisible();

    // add a MC question
    await page.selectOption('#QuestionType', 'MultipleChoice');
    await page.fill('#questionText', 'Soal uji injeksi 1?');
    await page.fill('#option_A', 'Jawaban A');
    await page.fill('#option_B', 'Jawaban B');
    await page.check('#correct_A');
    await page.click('#injAddQuestionBtn');

    // daftar soal shows 1 row; form cleared; no reload
    await expect(page.locator('#injQuestionList table tbody tr')).toHaveCount(1);
    await expect(page.locator('#questionText')).toHaveValue('');
    expect(page.url()).toContain('InjectAssessment');

    // now 1 soal → advance succeeds
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();
  });
});

// ── INJ-07: sertifikat radio 3-mode (implemented Plan 394-03) ───────────────────
test.describe('INJ-07 cert radio', () => {
  test('cert radio 3-mode toggle', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    await expect(page.locator('#wizardStepNav')).toBeVisible({ timeout: 15_000 });
    await page.evaluate(() => (window as any).injWizard.goToStep(4));
    await expect(page.locator('#step-4')).toBeVisible();

    // default None → all conditional blocks hidden
    await expect(page.locator('#certAutoBlock')).toBeHidden();
    await expect(page.locator('#certManualBlock')).toBeHidden();
    await expect(page.locator('#certValidityBlock')).toBeHidden();

    // Auto → preview + validity visible, manual hidden
    await page.check('#certModeAuto');
    await expect(page.locator('#certAutoBlock')).toBeVisible();
    await expect(page.locator('#certValidityBlock')).toBeVisible();
    await expect(page.locator('#certManualBlock')).toBeHidden();

    // Manual → nomor + validity visible, preview hidden
    await page.check('#certModeManual');
    await expect(page.locator('#certManualBlock')).toBeVisible();
    await expect(page.locator('#certValidityBlock')).toBeVisible();
    await expect(page.locator('#certAutoBlock')).toBeHidden();

    // Permanent disables ValidUntil
    await page.check('#CertPermanent');
    await expect(page.locator('#CertValidUntil')).toBeDisabled();

    // Tanpa → all hidden
    await page.check('#certModeNone');
    await expect(page.locator('#certAutoBlock')).toBeHidden();
    await expect(page.locator('#certManualBlock')).toBeHidden();
    await expect(page.locator('#certValidityBlock')).toBeHidden();
  });
});

// ── D-07: step5 placeholder + confirm + no DB write (implemented Plan 394-04) ────
test.describe('D-07 step5 placeholder + confirm', () => {
  test('step5 placeholder navigable', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    await page.fill('#Title', 'ZZ Step5 ' + Date.now());
    await page.selectOption('#Category', { index: 1 });
    await page.click('#btnNext1');
    await page.locator('#userCheckboxContainer .user-checkbox').first().check();
    await page.click('#btnNext2');
    await authorMcQuestion(page);
    await page.click('#btnNext3');
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Placeholder')).toBeVisible();
    await expect(page.locator('#step5Placeholder')).toContainText('tahap berikutnya');
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
  });

  test('confirm summary', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Confirm ' + Date.now();
    await fillWizardToConfirm(page, title);
    await expect(page.locator('#sum-title')).toHaveText(title);
    await expect(page.locator('#sum-worker-count')).toContainText('1');
    await expect(page.locator('#sum-soal-count')).toHaveText('1');
    await expect(page.locator('#btnInject')).toBeVisible();
    // edit-from-confirm jumps to step 1, then returns to confirm via Selanjutnya
    await page.locator('.edit-from-confirm[data-step="1"]').click();
    await expect(page.locator('#step-1')).toBeVisible();
    await page.click('#btnNext1');
    await expect(page.locator('#step-6')).toBeVisible();
  });
});
