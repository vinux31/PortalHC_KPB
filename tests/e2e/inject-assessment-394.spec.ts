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
  test.skip('authoring type toggle + add soal', async () => { /* Plan 394-03: applyQTypeSwitch + injAddQuestionBtn → injQuestions[] */ });
});

// ── INJ-07: sertifikat radio 3-mode (implemented Plan 394-03) ───────────────────
test.describe('INJ-07 cert radio', () => {
  test.skip('cert radio 3-mode toggle', async () => { /* Plan 394-03: CertMode Auto/Manual/Tanpa → conditional blocks */ });
});

// ── D-07: step5 placeholder + no DB write (implemented Plan 394-04) ─────────────
test.describe('D-07 step5 placeholder + no DB write', () => {
  test.skip('step5 placeholder navigable', async () => { /* Plan 394-04: #step5Placeholder navigable + confirm summary + 0 DB write */ });
});
