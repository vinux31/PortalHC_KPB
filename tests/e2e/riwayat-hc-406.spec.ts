// Phase 406 RTK-08 — Riwayat Percobaan HC modal e2e FLOW 406 (milestone v32.4 Ujian Ulang).
//
// Runtime-verifies the Razor surface (Lesson Phase 354: grep+build insufficient) di
// Views/Admin/AssessmentMonitoringDetail.cshtml:
//   - dropdown "Riwayat Percobaan" (.btn-riwayat-percobaan) → #riwayatPercobaanModal lazy-fetch
//     appUrl('/Admin/RiwayatPercobaan?sessionId=...') → _RiwayatPercobaan (Plan 406-01) ke #riwayatBody
//   - accordion #riwayatAccordion per-attempt (terbaru dulu, current di-badge "Percobaan saat ini")
//   - tabel per-soal (No/Soal/Jawaban/Status/Skor) tri-state ✓/✗/—
//
// Scenario (grep-able -g): open · per-soal · current · pending · xss
//
// SEED_WORKFLOW (CLAUDE.md / docs/SEED_WORKFLOW.md): temporary + local-only. beforeAll BACKUP
//   HcPortalDB_Dev → execScript tests/sql/riwayat-hc-406-seed.sql → afterAll RESTORE (sukses ATAU
//   gagal) + Layer 4 cleanup assert. Fixture: 1 AssessmentSession Completed [RIWAYAT406] + package
//   chain (current attempt LIVE) + 2 AssessmentAttemptHistory + 5 AssessmentAttemptResponseArchive
//   (correct/wrong/XSS di attempt-1; correct/essay-pending IsCorrect=NULL di attempt-2).
//
// PRECONDITION run: app @ http://localhost:5270 (branch ITHandoff; Authentication__UseActiveDirectory=false
//   dotnet run --urls http://localhost:5270) + DB lokal HcPortalDB_Dev (SQLEXPRESS). WAJIB --workers=1
//   (NTLM loopback/shared-memory SQL conn). Set E2E_BASE_URL=http://localhost:5270 (config default 5277).
//
// Auth: admin@pertamina.com / 123456 (dev lokal — JANGAN staging/prod).

import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;
let sessionId: number;
let workerName: string;
let title: string;
let category: string;
let scheduleDate: string; // yyyy-MM-dd dari seed (hindari tz drift)

// Inline login — accept any redirect away dari /Account/Login (pola essay-grading-384.spec.ts,
// lebih toleran dari helper auth.login yang menunggu '**/Home/**').
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

// Buka modal riwayat untuk baris [RIWAYAT406], tunggu spinner→accordion. Return modal locator.
async function openRiwayatModal(page: Page) {
  await loginAny(page, 'admin');
  await page.goto(monitoringUrl());

  const modal = page.locator('#riwayatPercobaanModal');
  const trigger = page.locator('.btn-riwayat-percobaan').first();

  // .btn-riwayat-percobaan adalah dropdown-item (hidden saat menu collapsed). Buka dropdown ⋮
  // baris peserta [RIWAYAT406] dulu agar item visible+clickable.
  const row = page.locator('tr', { has: page.locator(`.btn-riwayat-percobaan[data-session-id="${sessionId}"]`) });
  await row.locator('button[aria-haspopup="true"]').click(); // toggle dropdown ⋮
  await expect(trigger).toBeVisible({ timeout: 10_000 });
  await trigger.click();

  await expect(modal).toBeVisible({ timeout: 10_000 });
  // Body transisi spinner → accordion (AJAX lazy-fetch).
  await expect(modal.locator('#riwayatAccordion')).toBeVisible({ timeout: 10_000 });
  return modal;
}

test.describe.configure({ mode: 'serial' });

test.describe('FLOW 406 — Riwayat Percobaan HC modal (RTK-08)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre406rtk08-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW).
    await db.backup(snapshotPath);

    // 3. Seed sesi Completed + 2 attempt arsip + current attempt live.
    await db.execScript(path.resolve(__dirname, '../sql/riwayat-hc-406-seed.sql'));

    // 4. Layer 1: konfirmasi seeded.
    const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%'");
    expect(n, 'Layer 1: riwayat fixture session seeded').toBeGreaterThan(0);

    // 5. Resolve nav param + id dari DB (hindari tz mismatch dgn GETDATE server).
    sessionId    = await db.queryScalar("SELECT TOP 1 Id FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%'");
    title        = await db.queryString("SELECT TOP 1 Title FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%'");
    category     = await db.queryString("SELECT TOP 1 Category FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%'");
    scheduleDate = await db.queryString("SELECT TOP 1 CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%'");
    workerName   = await db.queryString(
      "SELECT TOP 1 u.FullName FROM AssessmentSessions s JOIN Users u ON s.UserId = u.Id WHERE s.Title LIKE '[[]RIWAYAT406]%'");
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    // Layer 4: DB lokal bersih.
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore (DB lokal bersih)').toBe(0);
  });

  // ── open: trigger → modal open + AJAX body load (spinner → accordion) + title = nama pekerja ──
  test('open: dropdown Riwayat Percobaan membuka modal + lazy-load accordion', async ({ page }) => {
    test.setTimeout(120_000);
    const modal = await openRiwayatModal(page);

    // Title set via .textContent → "Riwayat Percobaan — {nama}".
    await expect(page.locator('#riwayatModalLabel')).toContainText('Riwayat Percobaan');
    await expect(page.locator('#riwayatModalLabel')).toContainText(workerName);

    // Accordion: 3 attempt (current + 2 arsip). data-bs-parent="#riwayatAccordion".
    await expect(modal.locator('#riwayatAccordion .accordion-item')).toHaveCount(3);
  });

  // ── per-soal: expand attempt → tabel per-soal kolom + nilai seeded ──
  test('per-soal: tabel per-soal render kolom + teks soal/jawaban/skor seeded', async ({ page }) => {
    test.setTimeout(120_000);
    const modal = await openRiwayatModal(page);

    // Header kolom tabel per-soal.
    await expect(modal.locator('thead', { hasText: 'No' }).first()).toBeVisible();
    await expect(modal.locator('thead', { hasText: 'Soal' }).first()).toBeVisible();
    await expect(modal.locator('thead', { hasText: 'Jawaban Peserta' }).first()).toBeVisible();
    await expect(modal.locator('thead', { hasText: 'Status' }).first()).toBeVisible();
    await expect(modal.locator('thead', { hasText: 'Skor' }).first()).toBeVisible();

    // Expand attempt arsip ke-1 (Percobaan ke-1) — accordion item terakhir (terbaru dulu).
    const att1Btn = modal.locator('.accordion-button', { hasText: 'Percobaan ke-1' });
    await att1Btn.click();
    const att1Body = modal.locator('.accordion-collapse.show', { has: page.locator('td', { hasText: 'Jawaban tepat A' }) }).first();

    // Teks soal + jawaban peserta + skor seeded (attempt 1: benar 'Jawaban tepat A' skor 50).
    await expect(modal.locator('td', { hasText: 'Arsip A1 soal benar' }).first()).toBeVisible();
    await expect(modal.locator('td', { hasText: 'Jawaban tepat A' }).first()).toBeVisible();
    await expect(modal.locator('td', { hasText: 'Jawaban keliru A' }).first()).toBeVisible();
  });

  // ── current: attempt LIVE di-badge "Percobaan saat ini" + ordering terbaru-dulu (current paling atas) ──
  test('current: attempt saat ini di-badge "Percobaan saat ini" dan tampil paling atas', async ({ page }) => {
    test.setTimeout(120_000);
    const modal = await openRiwayatModal(page);

    // Badge current.
    await expect(modal.locator('.badge.bg-info', { hasText: 'Percobaan saat ini' })).toHaveCount(1);

    // Item pertama (paling atas) = current (AttemptNumber tertinggi = max arsip + 1 = 3).
    const firstHeader = modal.locator('#riwayatAccordion .accordion-item').first().locator('.accordion-button');
    await expect(firstHeader).toContainText('Percobaan saat ini');
    await expect(firstHeader).toContainText('Percobaan ke-3'); // 2 arsip → current = ke-3
  });

  // ── pending: essay IsCorrect=null → muted "—"/Menunggu (BUKAN ✗) ──
  test('pending: essay belum dinilai tampil "—"/Menunggu, bukan ikon salah', async ({ page }) => {
    test.setTimeout(120_000);
    const modal = await openRiwayatModal(page);

    // Expand attempt ke-2 (punya baris essay-pending IsCorrect=NULL).
    await modal.locator('.accordion-button', { hasText: 'Percobaan ke-2' }).click();

    // Baris essay → status muted dengan title="Menunggu penilaian" + visually-hidden "Menunggu".
    const essayRow = modal.locator('tr', { has: page.locator('td', { hasText: 'Arsip A2 soal essay' }) });
    await expect(essayRow).toBeVisible();
    await expect(essayRow.locator('[title="Menunggu penilaian"]')).toHaveCount(1);
    // BUKAN ikon salah (x-circle) di baris essay.
    await expect(essayRow.locator('.bi-x-circle-fill')).toHaveCount(0);
  });

  // ── xss: AnswerText '<script>...' di-render sebagai TEKS (encoded, tak eksekusi) ──
  test('xss: payload <script> di jawaban ter-encode inert (tak eksekusi)', async ({ page }) => {
    test.setTimeout(120_000);
    const modal = await openRiwayatModal(page);

    // Expand attempt ke-1 (punya baris XSS).
    await modal.locator('.accordion-button', { hasText: 'Percobaan ke-1' }).click();

    // Sentinel global TIDAK boleh ter-set (script tak dieksekusi → @-encoded).
    const xssExecuted = await page.evaluate(() => (window as any).__riwayatXss406);
    expect(xssExecuted, 'XSS payload TIDAK dieksekusi (encoded inert)').toBeUndefined();

    // Literal string '<script>' tampil sebagai TEKS di sel jawaban (auto HTML-escape Razor @).
    const xssRow = modal.locator('tr', { has: page.locator('td', { hasText: 'Arsip A1 soal XSS' }) });
    await expect(xssRow).toBeVisible();
    await expect(xssRow).toContainText('<script>window.__riwayatXss406=1</script>');
  });
});
