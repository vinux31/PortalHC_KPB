// judul-fleksibel-cek-duplikat (2026-06-15) — verifikasi runtime tombol Cek Judul (Razor + JS).
// Pola login dari manage-assessment-filter.spec.ts (accounts fixture, /Account/Login).
// Pre-req: server localhost:5277 jalan (Authentication__UseActiveDirectory=false); login admin@pertamina.com / 123456.
// Jalur "dipakai"/soft-block/judul-fleksibel diverifikasi via UAT live (butuh state DB) + xUnit FindTitleDuplicatesTests.
// Run: cd tests && npx playwright test e2e/assessment-title-flexible.spec.ts --workers=1
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

test.describe('Judul assessment — tombol Cek Judul', () => {
  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CreateAssessment');
    await page.locator('#Title').waitFor({ state: 'visible', timeout: 15_000 });
  });

  test('Judul acak baru → alert aman (hijau)', async ({ page }) => {
    const unique = 'ZZ Judul Unik ' + Date.now();
    await page.fill('#Title', unique);
    await page.click('#btnCheckTitle');
    const box = page.locator('#titleCheckResult');
    await expect(box).toBeVisible();
    await expect(box.locator('.alert-success')).toContainText('Aman', { timeout: 10_000 });
  });

  test('Judul kosong → minta isi judul', async ({ page }) => {
    await page.fill('#Title', '');
    await page.click('#btnCheckTitle');
    await expect(page.locator('#titleCheckResult')).toContainText('Isi judul dulu');
  });
});
