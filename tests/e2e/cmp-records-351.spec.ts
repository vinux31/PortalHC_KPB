// Phase 351 (SF-03/SF-04/SF-05/SF-07) — Playwright UAT: Worker Detail + cross-surface filter consistency.
// Surface:
//   SF-03 (Worker Detail): search non-matching → #workerDetailEmptyState visible + #wdRecordCounter "Menampilkan 0 dari".
//   SF-04 (Worker Detail): #categoryFilter punya opsi off-master 'Legacy-FreeText-351' (distinct-actual, bukan master).
//   SF-05 (My Records): #categoryFilter + #typeFilter ada; filter Tipe assessment menyembunyikan baris data-type="training".
//   SF-07 (Records.cshtml): "Back to Team View" → #pane-team AKTIF tanpa klik manual + filter ter-restore.
//
// SEED_WORKFLOW (CLAUDE.md): backup HcPortalDB_Dev -> seed [PENDING351] (TrainingRecord Kategori
//   off-master 'Legacy-FreeText-351') -> UAT -> restore (afterAll, sukses ATAU gagal) -> Layer 4 assert bersih.
//   Klasifikasi temporary + local-only.
//
// Auth: manager (L3 atasan) — Team View + Worker Detail cross-section accessible (section-scope server-side).
//   Test SF-05 (My Records) pakai 'manager' juga — My Records milik akun login; manager punya baris
//   assessment + training sehingga guard data-type bisa diuji. loginAny — accept any redirect away /Account/Login.
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run) + DB lokal HcPortalDB_Dev.
//
// NOTE: spec ditulis RED lebih dulu (Wave 0 Nyquist) — selector #workerDetailEmptyState/#wdRecordCounter/
//   #typeFilter/distinct-actual #categoryFilter + active-tab back-nav baru hijau setelah Plan 02/03/04.

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;
let rinoId: string;

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

test.describe('Phase 351 — Worker Detail + cross-surface filter consistency', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre351-${ts}.bak`;
    await db.backup(snapshotPath);
    await db.execScript(path.resolve(__dirname, '../sql/cmp351-seed.sql'));
    const n = await db.queryScalar("SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%'");
    expect(n, 'Layer 1: pending351 TrainingRecord seeded').toBeGreaterThan(0);
    rinoId = (await db.queryString(
      "SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com'"
    )).trim();
    expect(rinoId, 'rino.prasetyo user id resolved').not.toBe('');
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  // ── SF-03 Worker Detail: 0-match empty-state + counter ────────────────────
  test('SF-03 0-match — empty-state + counter', async ({ page }) => {
    await loginAny(page, 'manager');
    await page.goto(`/CMP/RecordsWorkerDetail?workerId=${rinoId}`);
    await page.fill('#searchInput', 'zzz-nomatch-zzz');
    await page.waitForTimeout(400);
    await expect(page.locator('#workerDetailEmptyState')).toBeVisible();
    await expect(page.locator('#wdRecordCounter')).toContainText('Menampilkan 0 dari');
  });

  // ── SF-04 Worker Detail: off-master Kategori muncul + filter ──────────────
  test('SF-04 legacy-Kategori filters', async ({ page }) => {
    await loginAny(page, 'manager');
    await page.goto(`/CMP/RecordsWorkerDetail?workerId=${rinoId}`);
    await expect(
      page.locator('#categoryFilter option', { hasText: 'Legacy-FreeText-351' })
    ).toHaveCount(1);
    await page.selectOption('#categoryFilter', { label: 'Legacy-FreeText-351' });
    await page.waitForTimeout(400);
    await expect(page.locator('text=[PENDING351] Legacy Training Migas')).toBeVisible();
  });

  // ── SF-05 My Records: parity Kategori + Tipe value-map ────────────────────
  test('SF-05 parity — My Records Kategori+Tipe value-map', async ({ page }) => {
    await loginAny(page, 'manager');
    await page.goto('/CMP/Records');
    await expect(page.locator('#categoryFilter')).toBeVisible();
    await expect(page.locator('#typeFilter')).toBeVisible();
    await expect(page.locator('#typeFilter option[value="assessment"]')).toHaveCount(1);
    await expect(page.locator('#typeFilter option[value="training"]')).toHaveCount(1);
    await page.selectOption('#typeFilter', 'assessment');
    await page.waitForTimeout(400);
    // T2 guard: baris training hidden, baris assessment tetap visible.
    const trainingRows = page.locator('tr[data-type="training"]:visible');
    await expect(trainingRows).toHaveCount(0);
  });

  // ── SF-07 back-nav: Team tab active + restored filter ─────────────────────
  test('SF-07 back-nav — Team tab active + restored filter', async ({ page }) => {
    await loginAny(page, 'manager');
    await page.goto('/CMP/Records');
    const teamTab = page.locator('a[href="#pane-team"], #tab-team').first();
    if (await teamTab.count() > 0) await teamTab.click();
    await expect(page.locator('#dateFrom')).toBeVisible();
    await page.fill('#dateFrom', '2026-01-01');
    // Masuk Worker Detail lalu kembali via "Back to Team View".
    await page.goto(`/CMP/RecordsWorkerDetail?workerId=${rinoId}`);
    await page.click('text=/Back to Team View|Kembali ke Team/i');
    // Pitfall 4: JANGAN klik tab manual — assert tab AKTIF dari back-nav handler.
    await expect(page.locator('#pane-team')).toHaveClass(/active/);
    await expect(page.locator('#dateFrom')).toHaveValue('2026-01-01');
  });
});
