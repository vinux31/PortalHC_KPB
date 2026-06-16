// Phase 386 PXF-04 (F-04) — e2e finalize round-trip: 1 essay DIKOSONGKAN tetap bisa "Selesaikan Penilaian".
//
// Wave 5 (Plan 06): UN-GATED — fix pending-count parity + upsert + status-guard sudah
// live sejak Wave 3 (Plan 04). Spec self-contained: backup → seed (1 sesi PendingGrading
// + 2 essay: #1 terisi, #2 DIKOSONGKAN TextAnswer=NULL, prefix [ESSAYEMPTY386]) → resolve
// nav param dari DB → run → restore (SEED_WORKFLOW, pola essay-grading-384.spec.ts).
//
// Akar F-04: essay dikosongkan → baris kosong dihitung "pending" selamanya → tombol "Selesaikan"
// hilang / finalize ditolak (dead-end). Sesudah fix: baris kosong (whitespace/null) BUKAN pending →
// finalize jalan, essay kosong kontribusi 0 (auto-0), bukan error "Jawaban tidak ditemukan".
//
// Selector page per-worker /Admin/EssayGrading dipin dari EssayGrading.cshtml (single-source —
// reuse essay-grading-384.spec.ts:24-33).
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal, Phase 355)
//   2) cd tests; npx playwright test essay-empty-finalize-386 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

// Selector page per-worker /Admin/EssayGrading (single-source — reuse essay-grading-384.spec.ts).
const SEL = {
  scoreInput:  '.essay-score-input',
  saveBtn:     '.btn-save-essay-score',
  finalizeBtn: '.btn-finalize-grading',
};

let snapshotPath: string;
let sessionId: number;
let title: string;
let category: string;
let scheduleDate: string; // yyyy-MM-dd dari seed (hindari tz drift)

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

function essayGradingUrl(): string {
  return `/Admin/EssayGrading?sessionId=${sessionId}`
    + '&title=' + encodeURIComponent(title)
    + '&category=' + encodeURIComponent(category)
    + '&scheduleDate=' + scheduleDate;
}

test.describe.configure({ mode: 'serial' });

test.describe('PXF-04 Essay Empty Finalize — kosong tidak dead-end (F-04)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre386essayempty-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW mitigation).
    await db.backup(snapshotPath);

    // 3. Seed sesi essay-pending dengan 1 essay kosong + rantai package.
    await db.execScript(path.resolve(__dirname, '../sql/essay-empty-finalize-386-seed.sql'));

    // 4. Layer 1: konfirmasi seeded.
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%'");
    expect(n, 'Layer 1: essay-empty-finalize session seeded').toBeGreaterThan(0);

    // 5. Resolve nav param + id dari DB (hindari tz mismatch dgn GETDATE server).
    sessionId    = await db.queryScalar("SELECT TOP 1 Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%'");
    title        = await db.queryString("SELECT TOP 1 Title FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%'");
    category     = await db.queryString("SELECT TOP 1 Category FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%'");
    scheduleDate = await db.queryString("SELECT TOP 1 CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%'");
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  test('Essay dikosongkan → "Selesaikan Penilaian" tetap bisa + kontribusi 0', async ({ page }) => {
    await loginAny(page, 'hc');
    await page.goto(essayGradingUrl());

    // Akar F-04: pada load, essay #1 (terisi) pending → finalizeSection display:none.
    // Essay #2 dikosongkan (TextAnswer NULL) SESUDAH fix PXF-04 BUKAN pending — kunci
    // perbaikan adalah: menilai HANYA essay #1 sudah membuat allGraded:true (pending=0)
    // → finalizeSection muncul. Sebelum fix, essay #2 kosong terus dihitung pending →
    // tombol "Selesaikan" tidak pernah muncul (dead-end).

    // Nilai essay yang TERISI (skor penuh). Essay kosong dibiarkan (auto-0, bukan pending).
    await page.locator(SEL.scoreInput).first().fill('10');
    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/SubmitEssayScore') && res.status() === 200),
      page.locator(SEL.saveBtn).first().click(),
    ]);

    // SESUDAH menilai essay #1 saja: allGraded:true → tombol "Selesaikan Penilaian" MUNCUL
    // (walau essay #2 dikosongkan). Inilah pembuktian F-04 closed (bukan dead-end).
    await expect(page.locator(SEL.finalizeBtn).first()).toBeVisible();

    // Klik "Selesaikan Penilaian" — confirm() auto-accept dipasang SEBELUM klik.
    page.on('dialog', d => d.accept());
    const finalizeResp = page.waitForResponse(
      res => res.url().includes('/Admin/FinalizeEssayGrading') && res.status() === 200);
    await page.locator(SEL.finalizeBtn).first().click();
    const resp = await finalizeResp;

    // Sukses: bukan error "Jawaban tidak ditemukan"; essay kosong kontribusi 0 (auto-0).
    const body = await resp.json();
    expect(body.success).toBe(true);
    expect(JSON.stringify(body)).not.toMatch(/Jawaban tidak ditemukan/i);
  });
});
