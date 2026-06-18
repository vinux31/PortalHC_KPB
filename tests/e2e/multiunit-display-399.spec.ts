// Phase 399 Plan 04 — Multi-unit DISPLAY (MU-03 / D-07/D-08/D-09) runtime verification.
//
// Tujuan: kontrak verifikasi RUNTIME bahwa SEMUA unit pekerja tampil (primary ditandai) di
// surface HTML (Profile, WorkerDetail, tabel ManageWorkers) dan urutan primary-first. Badge
// primary = bg-success + bi-star-fill + teks "Utama"; sekunder = bg-secondary. Pekerja 0-unit
// menampilkan fallback "Belum diisi" / "-" (D-09).
//
// LESSON Phase 354: Razor dinamis (badge loop dari data) WAJIB di-verify runtime — grep+build
// tak cukup. Spec ini ditulis test-first (Task 3) lalu dijalankan headless (Task 4).
//
// _PSign cetak (all-units comma-join, D-07) + Excel export (primary-first comma) = render
// cetak/binary → diverifikasi MANUAL di Task 4 (browser print + buka .xlsx), bukan di spec ini.
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal)
//      (jalankan SQLBrowser bila login 500; ref: local e2e SQL env fix)
//   2) cd tests; npx playwright test multiunit-display-399 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)
//
// Data note: spec membuat pekerja TEMPORARY (2-unit & 0-unit) via UI CreateWorker. DB lokal di-
// RESTORE oleh global.teardown (snapshot global.setup) → pekerja temporary otomatis bersih
// (Seed Data Workflow: snapshot→test→restore, journal dikelola harness).

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

// Pilih Bagian pertama (dari sectionSelect di CreateWorker) yang punya ≥minUnits unit ter-render.
// Return { section, units } atau null. Tidak hardcode nama Bagian (data-driven).
async function pickSectionWithUnits(page: Page, minUnits: number): Promise<{ section: string; units: string[] } | null> {
  const select = page.locator('#sectionSelect');
  const values = await select.locator('option').evaluateAll(
    opts => opts.map(o => (o as HTMLOptionElement).value).filter(v => v !== ''),
  );
  for (const section of values) {
    await select.selectOption(section);
    await page.waitForTimeout(150);
    const units = await page.locator('#unitMultiContainer .uu-check').evaluateAll(
      els => els.map(e => (e as HTMLInputElement).value),
    );
    if (units.length >= minUnits) return { section, units };
  }
  return null;
}

// Buat pekerja via CreateWorker. units kosong → 0-unit; primaryIndex → unit primary.
// Return { nip, fullName }.
async function createWorker(
  page: Page,
  opts: { units: string[]; primaryIndex: number },
): Promise<{ nip: string; fullName: string }> {
  const stamp = Date.now().toString().slice(-9) + Math.floor(Math.random() * 90 + 10);
  const nip = 'D399' + stamp;
  const email = 'disp399_' + stamp + '@pertamina.test';
  const fullName = 'Display399 ' + stamp;

  await page.goto('/Admin/CreateWorker');
  await expect(page.locator('#unitMultiContainer')).toBeVisible();

  await page.fill('input[name="FullName"]', fullName);
  await page.fill('input[name="Email"]', email);
  await page.fill('input[name="NIP"]', nip);
  const pwd = page.locator('input[name="Password"]');
  if (await pwd.count() > 0) {
    await pwd.fill('Test123!');
    await page.locator('input[name="ConfirmPassword"]').fill('Test123!');
  }

  if (opts.units.length > 0) {
    // Bagian sudah ter-select oleh pemanggil (pickSectionWithUnits). Centang unit by value.
    for (const u of opts.units) {
      await page.locator('#unitMultiContainer .uu-check[value="' + u + '"]').check();
    }
    // set primary
    await page.locator('#unitMultiContainer .uu-primary[value="' + opts.units[opts.primaryIndex] + '"]').check();
  }

  await Promise.all([
    page.waitForURL(/ManageWorkers/, { timeout: 15_000 }),
    page.locator('#createWorkerForm button[type="submit"]').click(),
  ]);
  return { nip, fullName };
}

// Cari baris pekerja di ManageWorkers by NIP → return locator <tr>.
async function findWorkerRow(page: Page, nip: string) {
  await page.goto('/Admin/ManageWorkers');
  await page.fill('input[name="search"]', nip);
  await Promise.all([
    page.waitForLoadState('networkidle'),
    page.locator('input[name="search"]').press('Enter'),
  ]);
  return page.locator('tr.worker-row').filter({ hasText: nip });
}

test.describe('Phase 399 — Multi-unit DISPLAY (MU-03)', () => {

  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
  });

  // D-01: WorkerDetail pekerja 2-unit → 2 badge; primary punya .bi-star-fill + teks "Utama".
  test('D-01 WorkerDetail 2-unit: 2 badge, primary bintang + Utama', async ({ page }) => {
    await page.goto('/Admin/CreateWorker');
    const pick = await pickSectionWithUnits(page, 2);
    test.skip(pick === null, 'butuh Bagian dgn ≥2 unit di DB lokal');

    const unitA = pick!.units[0];
    const unitB = pick!.units[1];
    // primary = unitB (index 1) untuk bukti penanda primary bukan sekadar unit pertama
    const w = await createWorker(page, { units: [unitA, unitB], primaryIndex: 1 });

    const row = await findWorkerRow(page, w.nip);
    await expect(row).toHaveCount(1);
    await row.locator('a[href*="WorkerDetail"]').click();
    await expect(page).toHaveURL(/WorkerDetail/);

    // 2 badge unit tampil (cari badge yang memuat nama unit)
    await expect(page.locator('.badge', { hasText: unitA })).toHaveCount(1);
    await expect(page.locator('.badge', { hasText: unitB })).toHaveCount(1);
    // primary (unitB) = badge hijau + bintang + "Utama"
    const primBadge = page.locator('.badge.text-success', { hasText: unitB });
    await expect(primBadge).toHaveCount(1);
    await expect(primBadge.locator('.bi-star-fill')).toHaveCount(1);
    await expect(primBadge).toContainText('Utama');
    // sekunder (unitA) = TIDAK ada bintang/Utama
    const secBadge = page.locator('.badge.bg-secondary', { hasText: unitA });
    await expect(secBadge).toHaveCount(1);
    await expect(secBadge.locator('.bi-star-fill')).toHaveCount(0);
  });

  // D-02 + D-05: WorkerDetail ordering — primary muncul PERTAMA (sebelum sekunder), walau alfabetis berbeda.
  test('D-02/D-05 ordering primary-first di WorkerDetail', async ({ page }) => {
    await page.goto('/Admin/CreateWorker');
    const pick = await pickSectionWithUnits(page, 2);
    test.skip(pick === null, 'butuh Bagian dgn ≥2 unit');

    const unitA = pick!.units[0];
    const unitB = pick!.units[1];
    // primary = unitB; karena ordering primary-first, badge unitB harus muncul SEBELUM unitA di DOM
    const w = await createWorker(page, { units: [unitA, unitB], primaryIndex: 1 });

    const row = await findWorkerRow(page, w.nip);
    await row.locator('a[href*="WorkerDetail"]').click();
    await expect(page).toHaveURL(/WorkerDetail/);

    // ambil urutan teks badge unit di DOM (cell unit) — primary (unitB) harus index lebih kecil
    const badgeTexts = await page.locator('.badge').filter({ hasText: /./ }).allInnerTexts();
    const idxPrimary = badgeTexts.findIndex(t => t.includes(unitB));
    const idxSecondary = badgeTexts.findIndex(t => t.includes(unitA) && !t.includes(unitB));
    expect(idxPrimary).toBeGreaterThanOrEqual(0);
    expect(idxSecondary).toBeGreaterThanOrEqual(0);
    expect(idxPrimary).toBeLessThan(idxSecondary); // primary lebih dulu
  });

  // D-03: ManageWorkers row pekerja 2-unit → kedua unit tampil di cell.
  test('D-03 ManageWorkers cell tampil kedua unit', async ({ page }) => {
    await page.goto('/Admin/CreateWorker');
    const pick = await pickSectionWithUnits(page, 2);
    test.skip(pick === null, 'butuh Bagian dgn ≥2 unit');

    const unitA = pick!.units[0];
    const unitB = pick!.units[1];
    const w = await createWorker(page, { units: [unitA, unitB], primaryIndex: 0 });

    const row = await findWorkerRow(page, w.nip);
    await expect(row).toHaveCount(1);
    // kedua unit muncul di dalam baris (cell unit)
    await expect(row).toContainText(unitA);
    await expect(row).toContainText(unitB);
    // ada badge primary (Utama) di baris
    await expect(row.locator('.badge.text-success')).toHaveCount(1);
  });

  // D-04: pekerja 0-unit → ManageWorkers cell "-"; WorkerDetail "Belum diisi".
  test('D-04 pekerja 0-unit: fallback "-" (tabel) + "Belum diisi" (detail)', async ({ page }) => {
    const w = await createWorker(page, { units: [], primaryIndex: 0 });

    const row = await findWorkerRow(page, w.nip);
    await expect(row).toHaveCount(1);
    // tak ada badge unit primary di baris (0 unit)
    await expect(row.locator('.badge.text-success')).toHaveCount(0);

    await row.locator('a[href*="WorkerDetail"]').click();
    await expect(page).toHaveURL(/WorkerDetail/);
    // unit row WorkerDetail tampil fallback "Belum diisi"
    await expect(page.locator('text=Belum diisi').first()).toBeVisible();
    // tak ada badge primary unit
    await expect(page.locator('.badge.text-success .bi-star-fill')).toHaveCount(0);
  });

  // D-06: Profile (admin sendiri) merender area unit tanpa error (badge bila ada unit, atau fallback).
  // Smoke: pastikan halaman Profile load + struktur unit ter-render (primary-first invariant via UI).
  test('D-06 Profile load + render area unit (smoke)', async ({ page }) => {
    await page.goto('/Account/Profile');
    await expect(page).toHaveURL(/Profile/);
    // label Unit-tier hadir (org label) → area unit ada di DOM
    await expect(page.locator('body')).toContainText('Preview P-Sign');
    // bila admin punya primary unit, badge primary muncul dgn bintang+Utama; bila tidak, "Belum diisi".
    const primCount = await page.locator('.badge.text-success .bi-star-fill').count();
    const emptyCount = await page.locator('text=Belum diisi').count();
    expect(primCount + emptyCount).toBeGreaterThanOrEqual(1);
  });
});
