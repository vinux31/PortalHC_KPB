// Phase 345 CMP06R-05 — Playwright UAT: badge "Menunggu Penilaian" (Completed+IsPassed-null)
// di 3 surface (RecordsWorkerDetail + UserAssessmentHistory + BulkExportPdf).
//
// SEED_WORKFLOW (CLAUDE.md): backup HcPortalDB_Dev -> seed [PENDING345] -> UAT -> restore
// (finally, sukses ATAU gagal) -> Layer 4 assert bersih. Klasifikasi temporary + local-only.
//
// Auth: pakai `admin` untuk SEMUA surface. CMPController = [Authorize] (RecordsWorkerDetail
// terbuka untuk authenticated + workerId param); UserAssessmentHistory + BulkExportPdf =
// [Authorize(Roles="Admin, HC")] -> admin cukup. HC login broken di test infra existing
// (lihat export-per-peserta.spec.ts) -> sengaja TIDAK pakai HC.
//
// Label "Menunggu Penilaian" di DALAM PDF (di dalam .zip) = verifikasi human/MCP (RESEARCH A3);
// gate automated di sini = .zip download sukses + size>0.

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;
let coacheeUid: string;
let scheduleDate: string; // yyyy-MM-dd dari seed (hindari tz drift)

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

// Trigger download via navigation; page.goto throw "Download is starting" -> catch + abaikan.
async function triggerDownload(page: Page, url: string) {
  const downloadPromise = page.waitForEvent('download', { timeout: 30_000 });
  await page.goto(url).catch(() => { /* expected ketika response = file download */ });
  return downloadPromise;
}

test.describe.configure({ mode: 'serial' });

test.describe('Phase 345 Pending-Grade — badge "Menunggu Penilaian" 3 surface', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre345-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW T-345-04-01 mitigation).
    await db.backup(snapshotPath);

    // 3. Seed sesi Completed+IsPassed-null.
    await db.execScript(path.resolve(__dirname, '../sql/pending345-seed.sql'));

    // 4. Layer 1: konfirmasi seeded.
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%'");
    expect(n, 'Layer 1: pending session seeded').toBeGreaterThan(0);

    // 5. Resolve uid + scheduleDate dari DB (hindari tz mismatch dgn GETDATE server).
    coacheeUid = await db.queryString("SELECT TOP 1 Id FROM Users WHERE Email='rino.prasetyo@pertamina.com'");
    scheduleDate = await db.queryString("SELECT CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%'");
  });

  test.afterAll(async () => {
    // Restore di finally — JAMIN DB lokal bersih walau test gagal di tengah.
    try {
      if (snapshotPath) await db.restore(snapshotPath);
      // Restore sukses -> hapus .bak (hindari akumulasi backup tiap re-run). Best-effort.
      if (snapshotPath) { const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ } }
    } finally {
      if (snapshotPath) {
        const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%'");
        expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
      }
    }
  });

  test('Surface 1 — RecordsWorkerDetail: badge amber "Menunggu Penilaian" visible', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(`/CMP/RecordsWorkerDetail?workerId=${coacheeUid}`);
    await expect(
      page.locator('.badge.bg-warning', { hasText: 'Menunggu Penilaian' }).first()
    ).toBeVisible();
  });

  test('Surface 2 — UserAssessmentHistory: badge amber + indikator pending visible', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(`/Admin/UserAssessmentHistory?userId=${coacheeUid}`);
    await expect(
      page.locator('.badge.bg-warning', { hasText: 'Menunggu Penilaian' }).first()
    ).toBeVisible();
  });

  test('Surface 3 — BulkExportPdf: download _Bundle.zip sukses', async ({ page }) => {
    await loginAny(page, 'admin');
    const url = '/Admin/BulkExportPdf'
      + '?title=' + encodeURIComponent('[PENDING345] Essay Pending')
      + '&category=' + encodeURIComponent('OJT')
      + '&scheduleDate=' + scheduleDate;
    const download = await triggerDownload(page, url);
    const p = await download.path();
    expect(p, 'download path truthy').toBeTruthy();
    expect(download.suggestedFilename()).toMatch(/_Bundle\.zip$/);
    const fs = await import('node:fs');
    expect(fs.statSync(p!).size, 'zip size > 512B').toBeGreaterThan(512);
    // Label "Menunggu Penilaian" di dalam PDF-dalam-zip -> verifikasi human/MCP (RESEARCH A3).
  });
});
