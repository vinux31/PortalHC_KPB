// Phase 361 (PBYP-08/09/10) — UAT end-to-end UI Tab2 "Bypass Tahun" (/ProtonData/Override).
// Membuktikan lintas-stack: 2 tab (Tab1 utuh) -> wizard 3-langkah linear -> save closure mode
//   (CL-A instan + CL-B(b) pending) -> panel "Menunggu Konfirmasi" + deep-link auto-open modal
//   -> batal pending -> refleksi UI re-grade (badge Siap Dikonfirmasi <-> Menunggu Exam).
//
// Lesson Phase 354: perubahan Razor/JS WAJIB diverifikasi runtime di browser nyata —
//   build+grep TIDAK cukup (bug runtime lolos compile).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev
//   (resolve InstanceDefaultBackupPath) -> seed .planning/seeds/361-bypass-fixtures.sql
//   (4 worker multi-state) -> assert -> afterAll RESTORE (sukses ATAU gagal).
//
// PRECONDITION: app running di http://localhost:5277 dengan AD dimatikan:
//   Authentication__UseActiveDirectory=false dotnet run
//   Bila Playwright "browser not found" -> cd tests; npx playwright install chromium.
//
// Auth: hc (meylisa.tjiang@pertamina.com) — endpoint bypass [Authorize(Roles="Admin,HC")].
//   pwd dev lokal 123456 — JANGAN staging/prod.
//
// Catatan D-24 (re-grade Pass->Fail): trigger via UI admin Edit Nilai TIDAK feasible untuk
//   sesi bare fixture (tanpa paket soal). Logic flip pending Siap->Menunggu sudah ter-cover
//   xUnit 360 (ProtonBypassServiceTests Revert_PassFail). Spec ini meng-assert REFLEKSI UI
//   kedua state (badge + visibilitas tombol Konfirmasi) via state DB; alur re-grade penuh
//   diverifikasi di UAT live MCP (checkpoint Plan 361-04 Task 2).

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import * as path from 'node:path';

const SEED_SQL = path.resolve(__dirname, '../../.planning/seeds/361-bypass-fixtures.sql');

let snapshotPath: string;
let namaA: string; // choirul.anam — komplit -> CL-A
let namaB: string; // moch.widyadhana — partial -> CL-B(a)/(b)
let namaD: string; // iwan3 — pending Menunggu (E5)
let pendingIdD: number;

test.describe.configure({ mode: 'serial' });

test.describe('Phase 361 — UI Tab2 Bypass Tahun (UAT end-to-end)', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre361-${ts}.bak`;
    await db.backup(snapshotPath);
    await db.execScript(SEED_SQL);

    namaA = (await db.queryString(
      "SELECT FullName FROM Users WHERE Email='choirul.anam@pertamina.com'")).trim();
    namaB = (await db.queryString(
      "SELECT FullName FROM Users WHERE Email='moch.widyadhana@pertamina.com'")).trim();
    namaD = (await db.queryString(
      "SELECT FullName FROM Users WHERE Email='iwan3@pertamina.com'")).trim();
    pendingIdD = Number(await db.queryScalar(
      "SELECT TOP 1 Id FROM PendingProtonBypasses WHERE Reason LIKE 'Phase 361%' AND Status='Menunggu'"));
    expect(pendingIdD).toBeGreaterThan(0);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    await db.restore(snapshotPath);
    const fs = await import('node:fs');
    try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
  });

  // Helper: aktifkan Tab2 + tunggu panel pending selesai load.
  async function openTab2(page) {
    await page.goto('/ProtonData/Override');
    await page.click('#tab-bypass');
    await expect(page.locator('#pane-bypass')).toBeVisible();
  }

  // Helper: filter GAST -> Alkylation -> track 1 -> Muat Data.
  async function loadWorkers(page) {
    await page.selectOption('#bypassBagian', 'GAST');
    await page.selectOption('#bypassUnit', 'Alkylation Unit (065)');
    await page.selectOption('#bypassTrack', '1');
    await page.click('#btnLoadBypass');
    await expect(page.locator('#bypassTableContainer table')).toBeVisible();
  }

  // ── T1: 2 tab + Tab1 utuh + URL sync (PBYP-08 SC1, D-08) ────────────────
  test('T1 — page 2 tab, Tab1 markup utuh, URL sync saat switch', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/ProtonData/Override');

    await expect(page.locator('#tab-deliverable')).toHaveClass(/active/);
    await expect(page.locator('#tab-bypass')).toBeVisible();
    // Tab1 existing intact
    await expect(page.locator('#overrideBagian')).toBeAttached();
    await expect(page.locator('#btnLoadOverride')).toBeAttached();
    await expect(page.locator('#overrideModal')).toBeAttached();

    await page.click('#tab-bypass');
    await expect(page.locator('#pane-bypass')).toBeVisible();
    await expect(page).toHaveURL(/tab=bypass/); // D-08
    // Panel pending selalu tampil (D-16) — fixture punya 1 pending Worker D
    await expect(page.locator('#pendingPanelContainer')).toBeVisible();
    await expect(page.locator('#pendingCount')).toHaveText('1');
  });

  // ── T2: wizard nav 3-step linear (PBYP-08, D-01/D-02/D-03/D-10) ─────────
  test('T2 — wizard linear: Lanjut disabled sampai valid, mode card eligible, recap+warning', async ({ page }) => {
    await login(page, 'hc');
    await openTab2(page);
    await loadWorkers(page);

    const rowA = page.locator('#bypassTableContainer tr', { hasText: namaA });
    await rowA.locator('.btn-bypass-open').click();
    await expect(page.locator('#bypassWizardModal')).toBeVisible();
    await expect(page.locator('#wizStep1')).toBeVisible();
    await expect(page.locator('#wizNext')).toBeDisabled(); // D-02 linear

    await page.selectOption('#wizTargetTrack', '2'); // Panelman Tahun 2 (delta=1)
    await expect(page.locator('#wizNext')).toBeEnabled();
    await page.click('#wizNext');

    await expect(page.locator('#wizStep2')).toBeVisible();
    // Worker A: final ada -> CL-A eligible, CL-B disabled + alasan (D-10)
    const cardCLA = page.locator('#wizModeCards .wiz-mode-radio[value="CL-A"]');
    const cardCLBa = page.locator('#wizModeCards .wiz-mode-radio[value="CL-B(a)"]');
    await expect(cardCLA).toBeEnabled();
    await expect(cardCLBa).toBeDisabled();
    await expect(page.locator('#wizModeCards')).toContainText('CL-B hanya tersedia bila penanda final tahun asal belum ada.');

    await cardCLA.check();
    await page.click('#wizNext');
    await expect(page.locator('#wizStep3')).toBeVisible();
    await expect(page.locator('#wizSubmit')).toBeVisible();
    await expect(page.locator('#wizSubmit')).toBeDisabled(); // unit+alasan belum diisi

    // Kembali berfungsi (D-02)
    await page.click('#wizBack');
    await expect(page.locator('#wizStep2')).toBeVisible();
    await page.click('#wizNext');
    await expect(page.locator('#wizStep3')).toBeVisible();

    await page.selectOption('#wizTargetBagian', 'GAST');
    await page.selectOption('#wizTargetUnit', 'Alkylation Unit (065)');
    await page.fill('#wizReason', 'Uji wizard T2 — tidak disubmit');
    // Recap + warning tampil (D-03)
    await expect(page.locator('#wizRecap')).toContainText(namaA);
    await expect(page.locator('#wizWarning')).toContainText('Worker dipindahkan instan dengan penanda kelulusan yang sudah ada.');
    await expect(page.locator('#wizSubmit')).toBeEnabled();
    // Tutup tanpa submit
    await page.click('#bypassWizardModal .btn-close');
  });

  // ── T3: save closure mode — CL-A instan + CL-B(b) pending (PBYP-08) ─────
  test('T3 — BypassSave CL-A sukses toast; CL-B(b) buat pending + reminder paket soal', async ({ page }) => {
    await login(page, 'hc');
    await openTab2(page);
    await loadWorkers(page);

    // CL-A Worker A (instan)
    const rowA = page.locator('#bypassTableContainer tr', { hasText: namaA });
    await rowA.locator('.btn-bypass-open').click();
    await page.selectOption('#wizTargetTrack', '2');
    await page.click('#wizNext');
    await page.locator('#wizModeCards .wiz-mode-radio[value="CL-A"]').check();
    await page.click('#wizNext');
    await page.selectOption('#wizTargetBagian', 'GAST');
    await page.selectOption('#wizTargetUnit', 'Alkylation Unit (065)');
    await page.fill('#wizReason', 'Phase 361 e2e — CL-A Worker A');
    await page.click('#wizSubmit');
    await expect(page.locator('.toast.text-bg-success')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#bypassWizardModal')).toBeHidden();

    // CL-B(b) Worker B (pending + reminder kuning D-04)
    await loadWorkers(page);
    const rowB = page.locator('#bypassTableContainer tr', { hasText: namaB });
    await rowB.locator('.btn-bypass-open').click();
    await page.selectOption('#wizTargetTrack', '2');
    await page.click('#wizNext');
    await page.locator('#wizModeCards .wiz-mode-radio[value="CL-B(b)"]').check();
    await page.click('#wizNext');
    await page.selectOption('#wizTargetBagian', 'GAST');
    await page.selectOption('#wizTargetUnit', 'Alkylation Unit (065)');
    await page.fill('#wizReason', 'Phase 361 e2e — CL-B(b) Worker B');
    await page.click('#wizSubmit');
    await expect(page.locator('.toast.text-bg-warning')).toContainText(
      'lampirkan paket soal di Kelola Assessment', { timeout: 10_000 });
    // Pending bertambah: Worker D (fixture) + Worker B (baru) = 2
    await expect(page.locator('#pendingCount')).toHaveText('2');
  });

  // ── T4: deep-link auto-open modal + stale toast (PBYP-09, D-05/D-06) ────
  test('T4 — deep-link pending valid auto-buka modal; stale id toast info', async ({ page }) => {
    await login(page, 'hc');

    // Valid: pending Worker D (Menunggu) -> modal auto-open, Konfirmasi hidden (D-13)
    await page.goto(`/ProtonData/Override?tab=bypass&pending=${pendingIdD}`);
    await expect(page.locator('#bypassConfirmModal')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#confirmModalBody')).toContainText(namaD);
    await expect(page.locator('#confirmModalBody')).toContainText('Menunggu Exam');
    await expect(page.locator('#confirmBtnGo')).toBeHidden(); // hanya Siap (D-13)
    await page.locator('#bypassConfirmModal [data-bs-dismiss="modal"]').first().click();

    // Stale: id tak ada -> toast info + Tab2 normal (D-06)
    await page.goto('/ProtonData/Override?tab=bypass&pending=999999');
    await expect(page.locator('.toast')).toContainText(
      'Pending bypass sudah diproses atau tidak ditemukan.', { timeout: 10_000 });
    await expect(page.locator('#pane-bypass')).toBeVisible();
  });

  // ── T5: refleksi UI state Siap + batal pending (PBYP-09/10, D-17/D-19) ──
  test('T5 — pending Siap: badge + Konfirmasi muncul; Batal hapus rencana (confirm dialog)', async ({ page }) => {
    // Simulasi exam lulus -> pending Siap (flip service ter-cover xUnit 360; lihat header).
    await db.queryString(
      `UPDATE AssessmentSessions SET IsPassed=1, Score=85, CompletedAt=SYSUTCDATETIME()
       WHERE Id=(SELECT LinkedAssessmentSessionId FROM PendingProtonBypasses WHERE Id=${pendingIdD});
       UPDATE PendingProtonBypasses SET Status='Siap' WHERE Id=${pendingIdD};
       SELECT 'OK';`);

    await login(page, 'hc');
    await openTab2(page);
    const rowD = page.locator('#pendingPanelContainer tr', { hasText: namaD });
    await expect(rowD.locator('.badge')).toContainText('Siap Dikonfirmasi'); // D-17
    await expect(rowD.locator('.btn-pending-view')).toBeVisible(); // D-13

    // Modal detail D-18: skor + hasil exam tampil
    await rowD.locator('.btn-pending-view').click();
    await expect(page.locator('#bypassConfirmModal')).toBeVisible();
    await expect(page.locator('#confirmModalBody')).toContainText('Lulus');
    await expect(page.locator('#confirmModalBody')).toContainText('skor 85');
    await expect(page.locator('#confirmBtnGo')).toBeVisible();
    await page.locator('#bypassConfirmModal [data-bs-dismiss="modal"]').first().click();

    // Balikkan ke Menunggu (refleksi re-grade Pass->Fail, D-24 UI-part)
    await db.queryString(
      `UPDATE AssessmentSessions SET IsPassed=NULL, Score=NULL, CompletedAt=NULL
       WHERE Id=(SELECT LinkedAssessmentSessionId FROM PendingProtonBypasses WHERE Id=${pendingIdD});
       UPDATE PendingProtonBypasses SET Status='Menunggu' WHERE Id=${pendingIdD};
       SELECT 'OK';`);
    await openTab2(page);
    const rowD2 = page.locator('#pendingPanelContainer tr', { hasText: namaD });
    await expect(rowD2.locator('.badge')).toContainText('Menunggu Exam');
    await expect(rowD2.locator('.btn-pending-view')).toHaveCount(0); // Konfirmasi hilang

    // Batal pending (D-19 confirm dialog) -> row hilang
    page.on('dialog', (d) => d.accept());
    await rowD2.locator('.btn-pending-cancel').click();
    await expect(page.locator('.toast.text-bg-success')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#pendingPanelContainer tr', { hasText: namaD })).toHaveCount(0);
  });
});
