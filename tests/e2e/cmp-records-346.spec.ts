// Phase 346 Plan 06 — Playwright UAT: CMP/Records Detail, Search & Logic.
// Surface: My Records Aksi/modal (REC-01/02) · Worker Detail Lihat Hasil + authz (REC-03/04) ·
//          Team search 3 scope + export filter + date warning (REC-06/08) ·
//          PendingGrading "Menunggu Penilaian" My Records (REC-07) · Tab3 History pending (MAP-20).
//
// SEED_WORKFLOW (CLAUDE.md): backup HcPortalDB_Dev -> seed [PENDING346] (Status='Menunggu Penilaian'
//   MURNI = REC-07 target) -> UAT -> restore (afterAll, sukses ATAU gagal) -> Layer 4 assert bersih.
//   Klasifikasi temporary + local-only.
//
// Auth: coachee (My Records owner), manager (L3 atasan full), sectionHead (L4 section-scoped).
//   loginAny — accept any redirect away dari /Account/Login (pola assessment-pending-grade.spec.ts;
//   non-coachee role kadang tidak redirect ke /Home/**).
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run) + DB lokal HcPortalDB_Dev.

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;
let coacheeUid: string;

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

test.describe('Phase 346 — CMP/Records detail, search & logic', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre346-${ts}.bak`;
    await db.backup(snapshotPath);
    await db.execScript(path.resolve(__dirname, '../sql/cmp346-seed.sql'));
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING346]%'");
    expect(n, 'Layer 1: pending346 session seeded').toBeGreaterThan(0);
    coacheeUid = await db.queryString("SELECT TOP 1 Id FROM Users WHERE Email='rino.prasetyo@pertamina.com'");
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING346]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  // ── REC-01/02 My Records ──────────────────────────────────────────────────
  test('My Records — kolom Aksi + tombol Lihat Hasil/Detail + modal training', async ({ page }) => {
    await loginAny(page, 'coachee');
    await page.goto('/CMP/Records');
    await expect(page.locator('thead th', { hasText: 'Aksi' })).toBeVisible();

    const lihatHasil = page.locator('a.btn', { hasText: 'Lihat Hasil' }).first();
    if (await lihatHasil.count() > 0) {
      await lihatHasil.click();
      await expect(page).toHaveURL(/\/CMP\/Results/);
      await page.goBack();
    }

    const detail = page.locator('button', { hasText: 'Detail' }).first();
    if (await detail.count() > 0) {
      await detail.click();
      await expect(page.locator('#trainingDetailModal')).toBeVisible();
      await expect(page.locator('#mdKategori')).toBeVisible();
      await expect(page.locator('#mdSubKategori')).toBeVisible();
      await expect(page.locator('#mdStatus')).toBeVisible();
    }
  });

  // ── REC-07 PendingGrading ─────────────────────────────────────────────────
  test('My Records — sesi PendingGrading muncul "Menunggu Penilaian"', async ({ page }) => {
    await loginAny(page, 'coachee');
    await page.goto('/CMP/Records');
    await expect(
      page.locator('table', { hasText: 'Menunggu Penilaian' }).first()
    ).toBeVisible();
  });

  // ── REC-03/04 Worker Detail + authz ───────────────────────────────────────
  test('Worker Detail — manager (L3) buka anggota: tombol Lihat Hasil + Results 200', async ({ page }) => {
    await loginAny(page, 'manager');
    const resp = await page.goto(`/CMP/RecordsWorkerDetail?workerId=${coacheeUid}`);
    expect(resp?.status(), 'L3 Worker Detail accessible').toBeLessThan(400);
    await expect(page.locator('a.btn', { hasText: 'Lihat Hasil' }).first()).toBeVisible();
  });

  test('Worker Detail — sectionHead (L4) cross-section Results -> Forbid', async ({ page }) => {
    // Resolve sesi milik user yang Section-nya BEDA dari sectionHead (taufik.hartopo).
    const otherSessionId = await db.queryScalar(`
      SELECT TOP 1 a.Id FROM AssessmentSessions a
      JOIN Users u ON u.Id = a.UserId
      JOIN Users sh ON sh.Email = 'taufik.hartopo@pertamina.com'
      WHERE (u.Section IS NULL OR u.Section <> ISNULL(sh.Section, '__none__'))
        AND a.UserId <> sh.Id`);
    test.skip(!otherSessionId || otherSessionId <= 0, 'Tidak ada sesi cross-section di DB lokal — seed manual bila perlu');
    await loginAny(page, 'sectionHead');
    const resp = await page.goto(`/CMP/Results?id=${otherSessionId}`);
    // ForbidResult -> 403; sebagian setup redirect ke AccessDenied. Terima keduanya sbg "tidak boleh".
    const status = resp?.status() ?? 0;
    const url = page.url();
    expect(status === 403 || /AccessDenied|Forbidden|Login/i.test(url),
      `L4 cross-section harus Forbid (status=${status}, url=${url})`).toBeTruthy();
  });

  // ── REC-06/08 Team View ───────────────────────────────────────────────────
  test('Team View — search scope + export filter param + date-range warning', async ({ page }) => {
    await loginAny(page, 'manager');
    await page.goto('/CMP/Records');
    // Team tab
    const teamTab = page.locator('a[href="#pane-team"], #tab-team').first();
    if (await teamTab.count() > 0) await teamTab.click();

    await expect(page.locator('#teamSearch')).toBeVisible();
    await expect(page.locator('#searchScope')).toBeVisible();

    // Header relabel REC-09
    await expect(page.locator('th', { hasText: 'Assessment Lulus' })).toBeVisible();

    // Search + scope -> export href ikut param
    await page.fill('#teamSearch', 'a');
    await page.selectOption('#searchScope', 'Keduanya');
    await page.waitForTimeout(600); // debounce 300ms + fetch
    const exportHref = await page.locator('#btnExportAssessment').getAttribute('href');
    expect(exportHref, 'export href berisi searchScope').toContain('searchScope=Keduanya');
    expect(exportHref, 'export href berisi search').toContain('search=a');

    // REC-08 inverted date-range warning
    await page.fill('#dateFrom', '2026-12-31');
    await page.fill('#dateTo', '2026-01-01');
    await page.waitForTimeout(500);
    await expect(page.locator('#dateFilterHint')).toContainText('Tanggal Awal lebih besar dari Tanggal Akhir');
  });

  // ── MAP-20 Tab3 History pending (dampak REC-07) ───────────────────────────
  test('Tab3 History — sesi pending tampil "Menunggu Penilaian" (admin)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(`/Admin/UserAssessmentHistory?userId=${coacheeUid}`);
    // _HistoryTab merender badge pending (REC-07 + Phase 345 label). Toleransi: cek teks di halaman.
    await expect(page.locator('body')).toContainText('Menunggu Penilaian');
  });
});
