// Phase 384 UIG-04 — Monitoring Essay Grading UI Refactor (Fase 2) e2e FLOW 384.
//
// Round-trip: tabel worker-list (badge 🟡 "{N} belum dinilai") → "Tinjau Essay"
// → page per-worker /Admin/EssayGrading → "Simpan Skor" (AJAX) → "Selesaikan
// Penilaian" → state "Selesai" in-place (D-09, URL tetap /EssayGrading) + D-10 read-only.
//
// SEED_WORKFLOW (CLAUDE.md): backup HcPortalDB_Dev → seed [ESSAY384] → run → restore
// (finally, sukses ATAU gagal) → Layer 4 assert bersih. Klasifikasi temporary + local-only.
//
// RED/fixme: 4 test ber-`test.fixme()` — struktur + assertion final TAPI di-skip karena UI
// dibangun di Wave 1 (Plan 02 page per-worker + Plan 03 tabel monitoring). Plan 04 menghapus
// `.fixme` dan menjalankan hijau setelah UI ada (Razor dynamic → Playwright runtime wajib, Phase 354).
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal, Phase 355)
//   2) cd tests; npx playwright test essay-grading-384 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

// ---------- Selector constants (single-source — Plan 04 reuse apa adanya) ----------
const SEL = {
  reviewLink:   'a:has-text("Tinjau Essay")',
  pendingBadge: '.badge.bg-warning',          // hasText "belum dinilai"
  workerHeader: 'h2',                          // UserFullName di page per-worker
  essayCard:    '.essay-grading-card',
  scoreInput:   '.essay-score-input',
  saveBtn:      '.btn-save-essay-score',
  finalizeBtn:  '.btn-finalize-grading',
};

let snapshotPath: string;
let sessionId: number;
let questionId: number;
let workerName: string;
let title: string;
let category: string;
let scheduleDate: string; // yyyy-MM-dd dari seed (hindari tz drift)

// Inline login — accept any redirect away dari /Account/Login (pola assessment-pending-grade.spec.ts).
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

// Build URL Monitoring Detail dari nav param yang di-resolve dari DB.
function monitoringUrl(): string {
  return '/Admin/AssessmentMonitoringDetail'
    + '?title=' + encodeURIComponent(title)
    + '&category=' + encodeURIComponent(category)
    + '&scheduleDate=' + scheduleDate;
}

// Build URL page per-worker /Admin/EssayGrading.
function essayGradingUrl(): string {
  return `/Admin/EssayGrading?sessionId=${sessionId}`
    + '&title=' + encodeURIComponent(title)
    + '&category=' + encodeURIComponent(category)
    + '&scheduleDate=' + scheduleDate;
}

test.describe.configure({ mode: 'serial' });

test.describe('FLOW 384 — Monitoring Essay UI Refactor (UIG-04)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre384-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW T-384-01-T mitigation).
    await db.backup(snapshotPath);

    // 3. Seed sesi essay-pending + rantai package.
    await db.execScript(path.resolve(__dirname, '../sql/essay-grading-384-seed.sql'));

    // 4. Layer 1: konfirmasi seeded.
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%'");
    expect(n, 'Layer 1: essay-pending session seeded').toBeGreaterThan(0);

    // 5. Resolve nav param + id dari DB (hindari tz mismatch dgn GETDATE server).
    sessionId    = await db.queryScalar("SELECT TOP 1 Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%'");
    title        = await db.queryString("SELECT TOP 1 Title FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%'");
    category     = await db.queryString("SELECT TOP 1 Category FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%'");
    scheduleDate = await db.queryString("SELECT TOP 1 CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%'");
    workerName   = await db.queryString(
      "SELECT TOP 1 u.FullName FROM AssessmentSessions s JOIN Users u ON s.UserId = u.Id WHERE s.Title LIKE '[[]ESSAY384]%'");
    questionId   = await db.queryScalar(
      "SELECT TOP 1 q.Id FROM PackageQuestions q "
      + "JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id "
      + "JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id "
      + "WHERE s.Title LIKE '[[]ESSAY384]%' AND q.QuestionType = 'Essay'");
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    // Tangkap error restore independen supaya tidak tertutup assertion Layer 4.
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    // Layer 4: DB lokal bersih (query informational; .bak dipertahankan jika restore gagal).
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  // RED: di-skip via test.fixme — UI dibangun Wave 1 (Plan 02/03). Plan 04 ganti `test.fixme(` → `test(`.
  test('UIG-01 tabel worker-list render + badge pending', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(monitoringUrl());
    await expect(page.locator(SEL.reviewLink).first()).toBeVisible();
    await expect(
      page.locator(SEL.pendingBadge, { hasText: 'belum dinilai' }).first()
    ).toBeVisible();
  });

  test('UIG-02 Tinjau Essay navigasi ke page per-worker', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(monitoringUrl());
    await Promise.all([
      page.waitForURL(/EssayGrading/),
      page.locator(SEL.reviewLink).first().click(),
    ]);
    await expect(page.locator(SEL.workerHeader, { hasText: workerName }).first()).toBeVisible();
    await expect(page.locator(SEL.essayCard).first()).toBeVisible();
  });

  test('UIG-03 Simpan Skor + Selesaikan round-trip + D-09 in-place', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(essayGradingUrl());

    // Simpan Skor (AJAX) — isi skor penuh lalu klik.
    await page.locator(SEL.scoreInput).first().fill('10');
    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/SubmitEssayScore') && res.status() === 200),
      page.locator(SEL.saveBtn).first().click(),
    ]);
    await expect(page.locator(`#badge_${sessionId}_${questionId}`)).toHaveText(/Sudah Dinilai/);
    await expect(page.locator(`#badge_${sessionId}_${questionId}`)).toHaveClass(/bg-success/);

    // Selesaikan Penilaian — confirm() auto-accept dipasang SEBELUM klik.
    page.on('dialog', d => d.accept());
    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/FinalizeEssayGrading') && res.status() === 200),
      page.locator(SEL.finalizeBtn).first().click(),
    ]);

    // D-09: TETAP di page per-worker (BUKAN redirect/reload ke monitoring) + input disabled IN-PLACE.
    expect(page.url()).toContain('/EssayGrading');
    await expect(page.locator(SEL.scoreInput).first()).toBeDisabled();
  });

  // Setelah UIG-03 finalize (serial, seed session sama), buka ULANG page → render READ-ONLY (D-10) persisted.
  test('UIG-04 finalized read-only persisted (D-10) + URL /EssayGrading', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(essayGradingUrl());

    // Session sudah Completed+NomorSertifikat (cert dari finalize UIG-03) → IsFinalized → read-only.
    expect(page.url()).toContain('/EssayGrading');
    await expect(page.locator(SEL.scoreInput).first()).toBeDisabled();
    // D-10: tombol "Simpan Skor" hilang saat finalized.
    await expect(page.locator(SEL.saveBtn)).toHaveCount(0);
  });
});
