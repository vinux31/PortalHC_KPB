// Phase 388 DSN-04/05 — CoachWorkload polish parity spec.
//
// Tujuan: assert PARITY runtime CoachWorkload pasca-polish (card framing DSN-04 +
// cleanup inline DSN-05) — filter bar & "Saran Penyeimbangan" ber-card; item saran
// = list-group-item.suggestion-card (BUKAN card-in-card); hook approve/skip + data-*
// utuh; chart #workloadChart + legend .legend-dot render; filter submit jalan.
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal)
//   2) cd tests; npx playwright test coachworkload-388 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)
//
// Data note: "Saran Penyeimbangan" butuh coach overload (>threshold). Bila DB lokal tak
// punya data overload, test parity approve/skip (test 3) auto-skip runtime; card-framing +
// chart + filter (test 1/2/4/5) tetap ter-assert tanpa data overload. JANGAN buat seed permanen.

import { test, expect, Page } from '@playwright/test';
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

test.describe('Phase 388 — CoachWorkload polish parity (DSN-04/05)', () => {

  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CoachWorkload');
    await expect(page.locator('h2', { hasText: 'Coach Workload' })).toBeVisible();
  });

  test('DSN-04: filter bar terbungkus card dgn card-header "Filter"', async ({ page }) => {
    // card-header bertuliskan "Filter" (ikon corong)
    const filterHeader = page.locator('.card-header', { hasText: 'Filter' });
    await expect(filterHeader).toBeVisible();
    // select section ADA DI DALAM .card .card-body
    const selectInCard = page.locator('.card .card-body select[name="section"]');
    await expect(selectInCard).toHaveCount(1);
    // tombol Filter + Reset ada di dalam card-body yang sama
    const cardBody = page.locator('.card', { has: filterHeader }).locator('.card-body');
    await expect(cardBody.getByRole('button', { name: 'Filter' })).toBeVisible();
    await expect(cardBody.getByRole('link', { name: 'Reset' })).toBeVisible();
  });

  test('DSN-04: "Saran Penyeimbangan" dalam 1 card; item = list-group-item bukan card', async ({ page }) => {
    const saranHeader = page.locator('.card-header', { hasText: 'Saran Penyeimbangan' });
    await expect(saranHeader).toBeVisible();
    // tidak boleh ada card-in-card: .card.suggestion-card = 0
    await expect(page.locator('.card.suggestion-card')).toHaveCount(0);

    const items = page.locator('.list-group-item.suggestion-card');
    const n = await items.count();
    if (n > 0) {
      // ada saran → item = list-group-item
      await expect(items.first()).toBeVisible();
    } else {
      // tak ada saran → empty-state alert hijau di dalam card
      const saranCard = page.locator('.card', { has: saranHeader });
      await expect(saranCard.locator('.alert-success')).toBeVisible();
    }
  });

  test('DSN-06 parity: hook approve/skip & data-* utuh', async ({ page }) => {
    const items = page.locator('.list-group-item.suggestion-card');
    const n = await items.count();
    test.skip(n === 0, 'no suggestion data (no overload) — parity approve/skip dikunci UAT/Phase 390');

    const first = items.first();
    // id diawali "sug-"
    await expect(first).toHaveAttribute('id', /^sug-/);
    // approve-btn punya 5 data-*
    const approve = first.locator('.approve-btn');
    await expect(approve).toHaveCount(1);
    for (const attr of ['data-mapping-id', 'data-new-coach-id', 'data-coachee-name', 'data-from-coach', 'data-to-coach']) {
      await expect(approve).toHaveAttribute(attr, /.+/);
    }
    // skip-btn punya data-mapping-id
    const skip = first.locator('.skip-btn');
    await expect(skip).toHaveCount(1);
    await expect(skip).toHaveAttribute('data-mapping-id', /.+/);
  });

  test('DSN-05/parity: chart canvas + legend dot (.legend-dot) render', async ({ page }) => {
    // legend dot pakai kelas .legend-dot (>=3: Normal/Mendekati/Overloaded)
    const dots = page.locator('.legend-dot');
    await expect(dots).toHaveCount(3);
    // tiap dot punya inline background (warna status ditahan inline)
    await expect(dots.first()).toHaveAttribute('style', /background/);
    // chart canvas render bila ada data mapping
    const hasData = await page.locator('#workloadChart').count();
    if (hasData > 0) {
      await expect(page.locator('#workloadChart')).toBeVisible();
    }
  });

  test('parity: filter submit + reset', async ({ page }) => {
    const select = page.locator('select[name="section"]');
    const optionValues = await select.locator('option').evaluateAll(
      (opts) => opts.map((o) => (o as HTMLOptionElement).value).filter((v) => v !== ''),
    );
    test.skip(optionValues.length === 0, 'no section options to filter');

    await select.selectOption(optionValues[0]);
    await Promise.all([
      page.waitForURL(/section=/, { timeout: 15_000 }),
      page.getByRole('button', { name: 'Filter' }).click(),
    ]);
    expect(page.url()).toContain('section=');

    await Promise.all([
      page.waitForURL(url => !url.toString().includes('section='), { timeout: 15_000 }),
      page.getByRole('link', { name: 'Reset' }).click(),
    ]);
    expect(page.url()).not.toContain('section=');
  });

  // W-EXP (390-01, DSN-06 export parity): "Export Excel" → download event → nama file kontrak.
  test('W-EXP export excel download', async ({ page }) => {
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('link', { name: 'Export Excel' }).click(),
    ]);
    expect(download.suggestedFilename()).toBe('coach_workload.xlsx');
  });

  // W-THR (390-01, threshold modal Admin, non-destructive): role-gate admin tampil → modal
  // terisi nilai threshold existing → tutup via "Batal" TANPA simpan (mutasi nyata = Plan 02 UAT).
  test('W-THR threshold modal fields filled (non-destructive)', async ({ page }) => {
    const setBtn = page.getByRole('button', { name: 'Set Threshold' });
    await expect(setBtn).toBeVisible();          // role-gate Admin holds
    await setBtn.click();
    await expect(page.locator('#thresholdModal')).toBeVisible();
    await expect(page.locator('#maxCoachees')).not.toHaveValue('');
    await expect(page.locator('#warningThreshold')).not.toHaveValue('');
    // batal — JANGAN simpan (mutasi nyata = Plan 02 UAT live)
    await page.locator('#thresholdModal .btn-secondary[data-bs-dismiss="modal"]').first().click();
    await expect(page.locator('#thresholdModal')).not.toBeVisible();
  });

  // W-ERR (390-01, console-error gate): 0 error saat buka/tutup threshold modal + expand "Lihat".
  test('W-ERR zero console error on interactions', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('pageerror', e => errors.push(e.message));
    const setBtn = page.getByRole('button', { name: 'Set Threshold' });
    if (await setBtn.count() > 0) {
      await setBtn.click();
      await expect(page.locator('#thresholdModal')).toBeVisible();
      await page.locator('#thresholdModal .btn-secondary[data-bs-dismiss="modal"]').first().click();
    }
    const expand = page.locator('button[data-bs-toggle="collapse"] .expand-chevron').first();
    if (await expand.count() > 0) await expand.locator('xpath=ancestor::button').first().click();
    expect(errors, errors.join('\n')).toHaveLength(0);
  });

});
