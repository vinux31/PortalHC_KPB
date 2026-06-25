// Phase 423 CERT-07 (D-08/D-09) — badge umur sesi "Menunggu Penilaian" (PendingGrading)
// di DUA view tempat HC bekerja: AssessmentMonitoringDetail (daftar) + EssayGrading (per-worker).
//
// Smoke read-only: badge "Menunggu N hari" ter-render dengan warna ber-ambang via
// CertIssuanceRules.PendingAgeBadgeClass (>3 kuning bg-warning, >7 merah bg-danger, else abu bg-secondary).
// KRITIS: TIDAK ada auto-finalize — status sesi tetap "Menunggu Penilaian" setelah load (D-08).
//
// SEED-NOTE: spec ini READ-ONLY terhadap data PendingGrading existing di DB lokal.
// Jika tidak ada sesi PendingGrading (CompletedAt set + belum finalized), test `test.skip`
// dengan pesan jelas — UAT manual Task 3 (checkpoint orchestrator) yang autoritative untuk
// seed terkontrol 3-ambang (2/5/8 hari) via SEED_WORKFLOW (snapshot→seed→test→restore).
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run --urls http://localhost:5270  (branch ITHandoff; env Authentication__UseActiveDirectory=false)
//   2) cd tests; E2E_BASE_URL=http://localhost:5270 npx playwright test cert-pending-age-badge --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';

// ---------- Selector konstan (badge dari Task 1 render server-side Razor) ----------
const SEL = {
  // Badge umur memakai teks "Menunggu N hari" (BUKAN status-label "Menunggu Penilaian").
  ageBadge: '.badge:has-text("Menunggu ")',
};

// Status sesi yang menandai PendingGrading di view (UserStatus remap).
const PENDING_STATUS = 'Menunggu Penilaian';

// Param sesi PendingGrading existing (di-resolve dari DB di beforeAll; null = skip-gracefully).
let hasPending = false;
let sessionId = 0;
let title = '';
let category = '';
let scheduleDate = ''; // yyyy-MM-dd dari DB (hindari tz drift dgn GETDATE server)

// Inline login — terima redirect apa pun selain /Account/Login (pola essay-grading-384.spec.ts).
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

function monitoringUrl(): string {
  return '/Admin/AssessmentMonitoringDetail'
    + '?title=' + encodeURIComponent(title)
    + '&category=' + encodeURIComponent(category)
    + '&scheduleDate=' + scheduleDate;
}

function essayGradingUrl(): string {
  return `/Admin/EssayGrading?sessionId=${sessionId}`
    + '&title=' + encodeURIComponent(title)
    + '&category=' + encodeURIComponent(category)
    + '&scheduleDate=' + scheduleDate;
}

test.describe.configure({ mode: 'serial' });

test.describe('CERT-07 — badge umur PendingGrading (display-only, no auto-finalize)', () => {

  test.beforeAll(async () => {
    // Cari sesi PendingGrading existing: status "Menunggu Penilaian" + CompletedAt ter-set + belum
    // finalized (NomorSertifikat NULL). Pakai literal status raw di DB ("Menunggu Penilaian").
    // SQL gagal/SQL tak hidup → biarkan hasPending=false (test skip-gracefully, bukan error).
    try {
      const n = await db.queryScalar(
        "SELECT COUNT(*) FROM AssessmentSessions " +
        "WHERE Status = 'Menunggu Penilaian' AND CompletedAt IS NOT NULL AND NomorSertifikat IS NULL");
      hasPending = n > 0;
      if (hasPending) {
        sessionId    = await db.queryScalar(
          "SELECT TOP 1 Id FROM AssessmentSessions " +
          "WHERE Status = 'Menunggu Penilaian' AND CompletedAt IS NOT NULL AND NomorSertifikat IS NULL " +
          "ORDER BY CompletedAt ASC");
        title        = await db.queryString(`SELECT TOP 1 Title FROM AssessmentSessions WHERE Id = ${sessionId}`);
        category     = await db.queryString(`SELECT TOP 1 Category FROM AssessmentSessions WHERE Id = ${sessionId}`);
        scheduleDate = await db.queryString(`SELECT TOP 1 CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Id = ${sessionId}`);
      }
    } catch {
      hasPending = false; // SQL tak tersedia → skip-gracefully (UAT manual Task 3 autoritative).
    }
  });

  test('AssessmentMonitoringDetail menampilkan badge umur "Menunggu N hari"', async ({ page }) => {
    test.skip(!hasPending, 'Tidak ada sesi PendingGrading di DB lokal — seed via SEED_WORKFLOW (UAT Task 3).');
    await loginAny(page, 'admin');
    await page.goto(monitoringUrl());

    // Badge umur terlihat (teks "Menunggu N hari", N angka).
    const badge = page.locator(SEL.ageBadge).filter({ hasText: /Menunggu \d+ hari/ }).first();
    await expect(badge).toBeVisible();

    // Warna badge salah satu kelas ambang (abu/kuning/merah).
    const cls = await badge.getAttribute('class');
    expect(cls).toMatch(/bg-(secondary|warning|danger)/);
  });

  test('EssayGrading menampilkan badge umur saat belum finalized', async ({ page }) => {
    test.skip(!hasPending, 'Tidak ada sesi PendingGrading di DB lokal — seed via SEED_WORKFLOW (UAT Task 3).');
    await loginAny(page, 'admin');
    await page.goto(essayGradingUrl());

    const badge = page.locator(SEL.ageBadge).filter({ hasText: /Menunggu \d+ hari/ }).first();
    await expect(badge).toBeVisible();
    const cls = await badge.getAttribute('class');
    expect(cls).toMatch(/bg-(secondary|warning|danger)/);
  });

  test('TIDAK ada auto-finalize — status tetap "Menunggu Penilaian" setelah load (D-08)', async ({ page }) => {
    test.skip(!hasPending, 'Tidak ada sesi PendingGrading di DB lokal — seed via SEED_WORKFLOW (UAT Task 3).');
    await loginAny(page, 'admin');
    await page.goto(monitoringUrl());
    // Status-label sesi tetap "Menunggu Penilaian" (render badge umur TIDAK mengubah status).
    await expect(page.locator('.status-cell', { hasText: PENDING_STATUS }).first()).toBeVisible();

    // Re-verify di DB: status sesi target TIDAK berubah jadi Completed (no write dari view).
    const stillPending = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${sessionId} AND Status = 'Menunggu Penilaian'`);
    expect(stillPending, 'D-08: status tetap PendingGrading setelah render (no auto-finalize)').toBe(1);
  });
});
