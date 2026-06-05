// Phase 350 (SF-01/SF-02/SF-06) — Playwright UAT: Team View server-side search scope + export parity.
// Surface: Team View (RecordsTeam) — search judul assessment "ojt v14.2" → worker pemilik muncul
//   (SF-01, fix bug 999.2: sebelumnya 0 worker) · dropdown "Lingkup" label "Judul Kegiatan" +
//   placeholder jujur sebut assessment (SF-02) · export href bawa scope+search (SF-06 plumbing, by-design F).
//
// SEED_WORKFLOW (CLAUDE.md): backup HcPortalDB_Dev -> seed [PENDING350] (Status='Completed' Category='OJT'
//   titled 'OJT v14.2 Migas') -> UAT -> restore (afterAll, sukses ATAU gagal) -> Layer 4 assert bersih.
//   Klasifikasi temporary + local-only.
//
// Auth: manager (L3 atasan) — Team View accessible (section-scope enforced server-side).
//   loginAny — accept any redirect away dari /Account/Login (pola cmp-records-346.spec.ts).
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run) + DB lokal HcPortalDB_Dev.
//
// NOTE: scope = href-only + counter assertions (RESEARCH Open Question 2 — cukup minimum untuk
//   SF-01/02 + SF-06 plumbing). XLSX-download-parse stretch + Category-drop-archived content check
//   = manual verification di Plan 03 phase gate (localhost:5277 eyeball).

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;

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

test.describe('Phase 350 — Team View search scope + export parity', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre350-${ts}.bak`;
    await db.backup(snapshotPath);
    await db.execScript(path.resolve(__dirname, '../sql/cmp350-seed.sql'));
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%'");
    expect(n, 'Layer 1: pending350 session seeded').toBeGreaterThan(0);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  // ── SF-01 / SF-02 / SF-06 Team View ───────────────────────────────────────
  test('Team View — search judul assessment → worker muncul + export href + honest copy', async ({ page }) => {
    await loginAny(page, 'manager');
    await page.goto('/CMP/Records');
    const teamTab = page.locator('a[href="#pane-team"], #tab-team').first();
    if (await teamTab.count() > 0) await teamTab.click();
    await expect(page.locator('#teamSearch')).toBeVisible();
    await expect(page.locator('#searchScope')).toBeVisible();

    // SF-02 honest copy assertions:
    await expect(page.locator('#searchScope option[value="Training"]')).toHaveText('Judul Kegiatan');
    await expect(page.locator('#teamSearch')).toHaveAttribute('placeholder', /judul assessment/);

    // SF-01 behavior: search the seeded assessment title → owner appears.
    await page.fill('#teamSearch', 'OJT v14.2');
    await page.selectOption('#searchScope', 'Keduanya');
    await page.waitForTimeout(600); // debounce 300ms + fetch
    await expect(page.locator('#workerCount')).not.toHaveText('0');

    // SF-06 plumbing (by-design F): export href carries scope + search.
    const href = await page.locator('#btnExportAssessment').getAttribute('href');
    expect(href, 'export href berisi searchScope').toContain('searchScope=Keduanya');
    expect(href, 'export href berisi search').toContain('search=OJT');
  });
});
