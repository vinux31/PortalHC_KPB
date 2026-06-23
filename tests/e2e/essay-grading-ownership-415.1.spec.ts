// Phase 415.1 BUGFIX — Essay Grading Ownership (cross-package) e2e.
//
// BUKTI FIX (controller ASLI, bukan replica xUnit Plan 01): HC menyimpan nilai essay
// untuk peserta yang dapat soal dari paket milik sesi-sibling LAIN (paket di-pool
// lintas sesi by design). Symptom pra-fix (wwwroot/js/essay-grading.js): klik Simpan →
// POST /Admin/SubmitEssayScore → response { success:false } → `alert("Soal bukan milik
// sesi ini.")`. Pasca-fix = badge "Sudah Dinilai" (bg-success), TIDAK ada alert.
//
// PITFALL-4 (RESEARCH §7): WAJIB pasang page.on('dialog') SEBELUM klik — alert() native
// menggantung test (false-negative). Assert dialog NOT contains pesan target (deteksi
// regresi eksplisit, bukan timeout diam).
//
// SEED_WORKFLOW (CLAUDE.md): backup HcPortalDB_Dev → seed [ESSAYOWN415] → run → restore
// (finally) → Layer 4 assert bersih. Klasifikasi temporary + local-only.
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal)
//      (port 5277 lokal; atau 5270 bila di worktree ITHandoff)
//   2) cd tests; npx playwright test essay-grading-ownership-415.1 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;

// Grup A — cross-package
let workerSessionId: number; // sesi S2 worker (paket milik induk S1)
let questionId: number;      // essayQ milik paket induk S1, tapi ADA di UPA S2
let title: string;
let category: string;
let scheduleDate: string;    // yyyy-MM-dd dari seed (hindari tz drift)

// Grup B — Pre/Post SamePackage
let postSessionId: number;   // sesi S4 worker-Post (paket clone milik induk-Post S3)
let postQuestionId: number;
let postTitle: string;
let postCategory: string;
let postScheduleDate: string;

// Inline login — accept any redirect away dari /Account/Login.
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

// Build URL page per-worker /Admin/EssayGrading.
function essayGradingUrl(sessionId: number, t: string, c: string, sd: string): string {
  return `/Admin/EssayGrading?sessionId=${sessionId}`
    + '&title=' + encodeURIComponent(t)
    + '&category=' + encodeURIComponent(c)
    + '&scheduleDate=' + sd;
}

test.describe.configure({ mode: 'serial' });

test.describe('Phase 415.1 — Essay Grading Ownership (cross-package)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre4151-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW).
    await db.backup(snapshotPath);

    // 3. Seed grup A (cross-package) + grup B (Pre/Post clone).
    await db.execScript(path.resolve(__dirname, '../sql/essay-grading-ownership-415.1-seed.sql'));

    // 4. Layer 1: konfirmasi seeded.
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%'");
    expect(n, 'Layer 1: sesi cross-package seeded').toBeGreaterThan(0);

    // 5a. Grup A — resolve sesi WORKER (paket dimiliki sesi LAIN: cross-package).
    workerSessionId = await db.queryScalar(
      "SELECT TOP 1 upa.AssessmentSessionId "
      + "FROM UserPackageAssignments upa "
      + "JOIN AssessmentPackages p ON upa.AssessmentPackageId = p.Id "
      + "JOIN AssessmentSessions s ON upa.AssessmentSessionId = s.Id "
      + "WHERE s.Title LIKE '[[]ESSAYOWN415] %' AND p.AssessmentSessionId <> upa.AssessmentSessionId");
    questionId = await db.queryScalar(
      "SELECT TOP 1 q.Id FROM PackageQuestions q "
      + "JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id "
      + "JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id "
      + "WHERE s.Title LIKE '[[]ESSAYOWN415] %' AND q.QuestionType = 'Essay'");
    title        = await db.queryString("SELECT TOP 1 Title FROM AssessmentSessions WHERE Id = " + workerSessionId);
    category     = await db.queryString("SELECT TOP 1 Category FROM AssessmentSessions WHERE Id = " + workerSessionId);
    scheduleDate = await db.queryString("SELECT TOP 1 CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Id = " + workerSessionId);

    // 5b. Grup B — Pre/Post: resolve sesi worker-Post (paket clone milik induk-Post).
    postSessionId = await db.queryScalar(
      "SELECT TOP 1 upa.AssessmentSessionId "
      + "FROM UserPackageAssignments upa "
      + "JOIN AssessmentPackages p ON upa.AssessmentPackageId = p.Id "
      + "JOIN AssessmentSessions s ON upa.AssessmentSessionId = s.Id "
      + "WHERE s.Title LIKE '[[]ESSAYOWN415-PP]%' AND p.AssessmentSessionId <> upa.AssessmentSessionId");
    postQuestionId = await db.queryScalar(
      "SELECT TOP 1 q.Id FROM PackageQuestions q "
      + "JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id "
      + "JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id "
      + "WHERE s.Title LIKE '[[]ESSAYOWN415-PP]%' AND q.QuestionType = 'Essay'");
    postTitle        = await db.queryString("SELECT TOP 1 Title FROM AssessmentSessions WHERE Id = " + postSessionId);
    postCategory     = await db.queryString("SELECT TOP 1 Category FROM AssessmentSessions WHERE Id = " + postSessionId);
    postScheduleDate = await db.queryString("SELECT TOP 1 CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Id = " + postSessionId);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    // Tangkap error restore independen supaya tidak tertutup assertion Layer 4.
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    // Layer 4: DB lokal bersih (.bak dipertahankan jika restore gagal).
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  // Test A — BUKTI FIX: cross-package save SUCCESS, no alert (controller asli).
  test('415.1 cross-package essay save → SUCCESS, no alert', async ({ page }) => {
    const dialogs: string[] = [];
    page.on('dialog', d => { dialogs.push(d.message()); d.dismiss(); });  // PITFALL-4: alert native gantung test
    await loginAny(page, 'admin');
    await page.goto(essayGradingUrl(workerSessionId, title, category, scheduleDate));  // sesi S2 (paket milik induk S1)
    await page.locator('.essay-score-input').first().fill('5');
    await Promise.all([
      page.waitForResponse(r => r.url().includes('/Admin/SubmitEssayScore') && r.status() === 200),
      page.locator('.btn-save-essay-score').first().click(),
    ]);
    await expect(page.locator(`#badge_${workerSessionId}_${questionId}`)).toHaveText(/Sudah Dinilai/);
    await expect(page.locator(`#badge_${workerSessionId}_${questionId}`)).toHaveClass(/bg-success/);
    expect(dialogs.join('|')).not.toContain('Soal bukan milik sesi ini');  // assert NO alert target
  });

  // Test B — Pre/Post SamePackage paritas (paket clone milik induk-Post).
  test('415.1 Pre/Post SamePackage essay save → SUCCESS', async ({ page }) => {
    const dialogs: string[] = [];
    page.on('dialog', d => { dialogs.push(d.message()); d.dismiss(); });
    await loginAny(page, 'admin');
    await page.goto(essayGradingUrl(postSessionId, postTitle, postCategory, postScheduleDate));  // sesi Post (paket clone)
    await page.locator('.essay-score-input').first().fill('5');
    await Promise.all([
      page.waitForResponse(r => r.url().includes('/Admin/SubmitEssayScore') && r.status() === 200),
      page.locator('.btn-save-essay-score').first().click(),
    ]);
    await expect(page.locator(`#badge_${postSessionId}_${postQuestionId}`)).toHaveText(/Sudah Dinilai/);
    expect(dialogs.join('|')).not.toContain('Soal bukan milik sesi ini');
  });
});
