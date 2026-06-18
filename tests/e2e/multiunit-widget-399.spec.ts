// Phase 399 Plan 03 — Multi-select Unit widget (MU-01/MU-02) runtime verification.
//
// Tujuan: kontrak verifikasi RUNTIME untuk widget checkbox-list Unit + radio "Utama" per baris
// di CreateWorker/EditWorker (di-render client-side oleh initSectionUnitMultiCascade dari
// ViewBag.SectionUnitsJson). Assert diturunkan dari UI-SPEC §A state machine (8 state) + §Accessibility.
//
// LESSON Phase 354: Razor dinamis WAJIB di-verify runtime (grep+build tak cukup untuk widget
// checkbox/radio yang di-render JS). Spec ini ditulis test-first (Task 3) lalu dijalankan (Task 4).
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal)
//      (jalankan SQLBrowser bila login 500; ref: local e2e SQL env fix)
//   2) cd tests; npx playwright test multiunit-widget-399 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)
//
// Data note: state-machine test (W-01..W-05, W-07, W-08) butuh ≥1 Bagian punya unit (cascade dict).
// Round-trip (W-06) butuh ≥1 Bagian punya ≥2 unit — buat pekerja temporary via UI lalu reopen Edit;
// DB lokal di-RESTORE oleh global.teardown (snapshot global.setup) → pekerja temporary otomatis bersih
// (Seed Data Workflow: snapshot→test→restore, journal dikelola harness). MU-07 (W-09) skip bila tak ada
// fixture coach-mapping aktif.

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

// Pilih Bagian pertama (dari sectionSelect) yang punya ≥minUnits unit ter-render di container.
// Return { section, units } atau null bila tak ada. Tidak hardcode nama Bagian (data-driven).
async function pickSectionWithUnits(page: Page, minUnits: number): Promise<{ section: string; units: string[] } | null> {
  const select = page.locator('#sectionSelect');
  const values = await select.locator('option').evaluateAll(
    opts => opts.map(o => (o as HTMLOptionElement).value).filter(v => v !== ''),
  );
  for (const section of values) {
    await select.selectOption(section);
    // tunggu render JS selesai — checkbox uu-check muncul (atau placeholder bila 0 unit)
    await page.waitForTimeout(150);
    const units = await page.locator('#unitMultiContainer .uu-check').evaluateAll(
      els => els.map(e => (e as HTMLInputElement).value),
    );
    if (units.length >= minUnits) return { section, units };
  }
  return null;
}

test.describe('Phase 399 — Multi-select Unit widget (MU-01/MU-02)', () => {

  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CreateWorker');
    await expect(page.locator('#unitMultiContainer')).toBeVisible();
  });

  // W-01: Bagian kosong → container tampil placeholder muted (no rows / no checkbox).
  test('W-01 placeholder saat Bagian kosong', async ({ page }) => {
    // baseline form load: sectionSelect value kosong (Create)
    await expect(page.locator('#sectionSelect')).toHaveValue('');
    await expect(page.locator('#unitMultiContainer .uu-check')).toHaveCount(0);
    await expect(page.locator('#unitMultiContainer')).toContainText('Pilih');
    await expect(page.locator('#unitMultiContainer .text-muted')).toBeVisible();
  });

  // W-02: pilih Bagian → checkbox-list unit Bagian itu ter-render (count == jumlah unit Bagian).
  test('W-02 render checkbox-list saat Bagian dipilih', async ({ page }) => {
    const pick = await pickSectionWithUnits(page, 1);
    test.skip(pick === null, 'no Bagian dengan unit di DB lokal — cascade tak bisa di-assert');

    const checks = page.locator('#unitMultiContainer .uu-check');
    await expect(checks).toHaveCount(pick!.units.length);
    // radio Utama 1:1 per checkbox
    await expect(page.locator('#unitMultiContainer .uu-primary')).toHaveCount(pick!.units.length);
    // name binding kontrak MVC
    await expect(page.locator('#unitMultiContainer input[name="Units"]')).toHaveCount(pick!.units.length);
    await expect(page.locator('#unitMultiContainer input[name="PrimaryUnit"]')).toHaveCount(pick!.units.length);
  });

  // W-03: centang unit → radio "Utama"-nya enabled (tidak disabled); default state radio disabled.
  test('W-03 radio Utama enabled saat checkbox dicentang', async ({ page }) => {
    const pick = await pickSectionWithUnits(page, 1);
    test.skip(pick === null, 'no Bagian dengan unit — tak bisa centang');

    const firstRow = page.locator('#unitMultiContainer > div').first();
    const chk = firstRow.locator('.uu-check');
    const radio = firstRow.locator('.uu-primary');
    // baseline: radio disabled sebelum dicentang
    await expect(radio).toBeDisabled();
    await chk.check();
    await expect(radio).toBeEnabled();
  });

  // W-04: centang unit pertama → radionya auto-checked (default primary = first checked, D-02).
  test('W-04 default primary = unit tercentang pertama', async ({ page }) => {
    const pick = await pickSectionWithUnits(page, 1);
    test.skip(pick === null, 'no Bagian dengan unit — tak bisa centang');

    const firstRow = page.locator('#unitMultiContainer > div').first();
    await firstRow.locator('.uu-check').check();
    await expect(firstRow.locator('.uu-primary')).toBeChecked();
    // tepat 1 radio checked di grup
    await expect(page.locator('#unitMultiContainer .uu-primary:checked')).toHaveCount(1);
  });

  // W-05: uncheck unit yang jadi primary → primary promote ke unit tercentang berikutnya; radio uncheck disabled.
  test('W-05 primary promote saat unit primary di-uncheck', async ({ page }) => {
    const pick = await pickSectionWithUnits(page, 2);
    test.skip(pick === null, 'butuh Bagian dgn ≥2 unit untuk bukti promote');

    const rows = page.locator('#unitMultiContainer > div');
    const row0 = rows.nth(0);
    const row1 = rows.nth(1);
    // centang 2 unit; unit0 jadi default primary
    await row0.locator('.uu-check').check();
    await row1.locator('.uu-check').check();
    await expect(row0.locator('.uu-primary')).toBeChecked();

    // uncheck unit0 (primary) → promote ke unit1; radio unit0 di-disable + tidak checked
    await row0.locator('.uu-check').uncheck();
    await expect(row0.locator('.uu-primary')).toBeDisabled();
    await expect(row0.locator('.uu-primary')).not.toBeChecked();
    await expect(row1.locator('.uu-primary')).toBeChecked();
    await expect(page.locator('#unitMultiContainer .uu-primary:checked')).toHaveCount(1);
  });

  // W-06: round-trip — buat pekerja 2-unit (primary unit kedua), reopen Edit → kedua checked + primary kedua.
  test('W-06 round-trip 2-unit Create → Edit', async ({ page }) => {
    const pick = await pickSectionWithUnits(page, 2);
    test.skip(pick === null, 'butuh Bagian dgn ≥2 unit untuk round-trip');

    const section = pick!.section;
    const unitA = pick!.units[0];
    const unitB = pick!.units[1];

    // NIP/email unik (suffix timestamp) supaya tak bentrok; DB di-RESTORE oleh teardown (cleanup).
    const stamp = Date.now().toString().slice(-9);
    const nip = 'T399' + stamp;
    const email = 'mu399_' + stamp + '@pertamina.test';

    await page.fill('input[name="FullName"]', 'MultiUnit Test ' + stamp);
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="NIP"]', nip);
    // password (non-AD lokal)
    const pwd = page.locator('input[name="Password"]');
    if (await pwd.count() > 0) {
      await pwd.fill('Test123!');
      await page.locator('input[name="ConfirmPassword"]').fill('Test123!');
    }
    // Bagian sudah ter-select oleh pickSectionWithUnits → centang 2 unit, set primary = unitB
    const rows = page.locator('#unitMultiContainer > div');
    await rows.nth(0).locator('.uu-check').check();
    await rows.nth(1).locator('.uu-check').check();
    await rows.nth(1).locator('.uu-primary').check(); // primary = unitB

    await Promise.all([
      page.waitForURL(/ManageWorkers/, { timeout: 15_000 }),
      page.click('button[type="submit"]'),
    ]);

    // cari pekerja baru by NIP → buka Edit
    await page.fill('input[name="search"]', nip);
    await Promise.all([
      page.waitForLoadState('networkidle'),
      page.locator('input[name="search"]').press('Enter'),
    ]);
    const editLink = page.locator('a[href*="EditWorker"]').first();
    await expect(editLink).toBeVisible();
    await editLink.click();
    await expect(page.locator('#unitMultiContainer')).toBeVisible();

    // round-trip assert: Bagian = section; unitA + unitB checked; primary = unitB
    await expect(page.locator('#sectionSelect')).toHaveValue(section);
    const chkA = page.locator('#unitMultiContainer .uu-check[value="' + unitA + '"]');
    const chkB = page.locator('#unitMultiContainer .uu-check[value="' + unitB + '"]');
    await expect(chkA).toBeChecked();
    await expect(chkB).toBeChecked();
    const primB = page.locator('#unitMultiContainer .uu-primary[value="' + unitB + '"]');
    await expect(primB).toBeChecked();
    // tepat 1 primary di grup, dan itu unitB
    await expect(page.locator('#unitMultiContainer .uu-primary:checked')).toHaveCount(1);
  });

  // W-07: 0 unit dicentang → submit valid (tidak ada error primary). Buat pekerja tanpa unit.
  test('W-07 0 unit = valid submit', async ({ page }) => {
    const stamp = Date.now().toString().slice(-9);
    const nip = 'T399Z' + stamp;
    const email = 'mu399z_' + stamp + '@pertamina.test';

    await page.fill('input[name="FullName"]', 'NoUnit Test ' + stamp);
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="NIP"]', nip);
    const pwd = page.locator('input[name="Password"]');
    if (await pwd.count() > 0) {
      await pwd.fill('Test123!');
      await page.locator('input[name="ConfirmPassword"]').fill('Test123!');
    }
    // JANGAN pilih Bagian / centang unit → 0 unit
    await Promise.all([
      page.waitForURL(/ManageWorkers/, { timeout: 15_000 }),
      page.click('button[type="submit"]'),
    ]);
    // sampai ManageWorkers = submit sukses (tak ada blokir validasi primary)
    expect(page.url()).toContain('ManageWorkers');
  });

  // W-08 (a11y): container role=group; tiap checkbox punya <label for>; radio disabled punya attr disabled.
  test('W-08 a11y role=group + label-for + disabled native', async ({ page }) => {
    await expect(page.locator('#unitMultiContainer')).toHaveAttribute('role', 'group');
    await expect(page.locator('#unitMultiContainer')).toHaveAttribute('aria-label', /.+/);

    const pick = await pickSectionWithUnits(page, 1);
    test.skip(pick === null, 'no Bagian dengan unit — a11y row tak bisa di-assert');

    const firstRow = page.locator('#unitMultiContainer > div').first();
    const chkId = await firstRow.locator('.uu-check').getAttribute('id');
    const primId = await firstRow.locator('.uu-primary').getAttribute('id');
    // label[for] asosiasi (bukan wrap)
    await expect(page.locator('label[for="' + chkId + '"]')).toHaveCount(1);
    await expect(page.locator('label[for="' + primId + '"]')).toHaveCount(1);
    // radio disabled native (sebelum checkbox dicentang)
    await expect(firstRow.locator('.uu-primary')).toBeDisabled();
  });

  // W-09 (MU-07, fixture-guarded): Edit pekerja dgn coach-mapping aktif, hapus unit-nya, submit →
  // modal "Konfirmasi Penghapusan" + tombol "Ya, Hapus & Nonaktifkan". Skip bila tak ada fixture.
  // Modal MU-07 di-trigger server-side (ViewBag.NeedConfirm); tanpa fixture coach-mapping aktif
  // pada pekerja multi-unit, jalur ini tak bisa dipicu deterministik — di-skip (UAT manual Task 4).
  test('W-09 MU-07 modal konfirmasi (fixture coach-mapping aktif)', async ({ page }) => {
    test.skip(true, 'butuh fixture coach-mapping aktif pada pekerja multi-unit — UAT manual Task 4 / Phase 402 seed');
    // Placeholder kontrak (bila fixture tersedia):
    //   buka Edit pekerja ber-mapping aktif → uncheck unit ber-AssignmentUnit → submit
    //   → expect #mu07ConfirmModal visible + tombol "Ya, Hapus & Nonaktifkan"
    await expect(page.locator('#mu07ConfirmModal')).toBeVisible();
    await expect(page.locator('#mu07ConfirmModal button:has-text("Ya, Hapus")')).toHaveCount(1);
  });
});
