// Audit konsolidasi v31.0 + v32.0 — UI/browser, goal: sistem tidak ada error.
//
// PART 1 (tutup lubang test 391): tambah peserta FLEKSIBEL saat ujian berjalan lewat
//   halaman EditAssessment NYATA (driving controller via browser, BUKAN xUnit tautologis
//   yang di-flag milestone-audit v32.0). Membuktikan REQ PART-01/02/03 + PART-04 end-to-end:
//   (a) penambahan saat ada sesi InProgress TIDAK terblokir, (b) sesi baru lahir ready-status
//   (Open/Upcoming via DeriveReadyStatus, BUKAN InProgress), (c) sesi InProgress existing TIDAK
//   ter-overwrite (Status/Schedule/Duration), (d) notice info (bukan error) muncul.
//
// PART 2 (goal "sistem tidak ada error"): sweep halaman inti v31+v32 sebagai admin →
//   assert TIDAK ada console-error / pageerror / HTTP 5xx, dan navigasi top-level < 400.
//
// SEED_WORKFLOW (CLAUDE.md + docs/SEED_WORKFLOW.md): PART 1 beforeAll BACKUP HcPortalDB_Dev +
//   reuse seed flexible-participant-413-seed.sql (flip 1 sesi rino → InProgress, window +1 hari);
//   afterAll RESTORE + unlink .bak. Klasifikasi seed = temporary + local-only.
//
// Run: app @localhost:5277 ATAU 5270 (Authentication__UseActiveDirectory=false) + SQLEXPRESS hidup.
//   cd tests && E2E_BASE_URL=http://localhost:5270 npx playwright test audit-v31-v32 --workers=1
//   (NTLM loopback / SignalR → --workers=1)

import { test, expect, type Page } from '@playwright/test';
import { login } from '../helpers/auth';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';
import * as fs from 'node:fs';

test.describe.configure({ mode: 'serial' });

// ---- module-scope state (resolve di beforeAll, dipakai lintas test serial) ----
let snapshotPath = '';
let inProgressSessionId = 0;
let batchTitle = '';
let batchCategory = '';
let batchScheduleDate = ''; // yyyy-MM-dd
let origSchedule = '';      // CONVERT 121 (ISO ms) — deteksi overwrite presisi
let origDuration = '';

// =====================================================================================
// PART 1 — 391: tambah peserta saat ujian berjalan via EditAssessment (browser → controller)
// =====================================================================================
test.describe('Audit v31+v32 · 391 — tambah peserta saat ujian berjalan (EditAssessment nyata)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp diblokir SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    snapshotPath = `${dir}/HcPortalDB_Dev-audit-v31v32.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW SOP).
    await db.backup(snapshotPath);

    // 3. Reuse seed 413: flip 1 sesi (punya paket, non-Proton, non-pair, milik rino) → InProgress
    //    + window ujian +1 hari (jadi window TERBUKA → penambahan harus lolos, PART-02).
    await db.execScript(path.resolve(__dirname, '../sql/flexible-participant-413-seed.sql'));

    // 4. Resolve sesi InProgress + batch key (server-side truth).
    inProgressSessionId = await db.queryScalar(`
      SELECT TOP 1 s.Id FROM AssessmentSessions s
      WHERE s.Status = 'InProgress' AND s.RemovedAt IS NULL
        AND s.LinkedGroupId IS NULL AND s.Category <> 'Assessment Proton'
        AND s.Title NOT LIKE '[[]MATRIX[_]TEST%'
        AND EXISTS (SELECT 1 FROM AssessmentPackages p WHERE p.AssessmentSessionId = s.Id)
        AND s.UserId = (SELECT Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com')
      ORDER BY s.Id DESC`);
    expect(inProgressSessionId, 'seed: sesi InProgress ter-flip').toBeGreaterThan(0);

    batchTitle = await db.queryString(
      `SELECT Title FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
    batchCategory = await db.queryString(
      `SELECT Category FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
    batchScheduleDate = await db.queryString(
      `SELECT CONVERT(varchar(10), Schedule, 23) FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);

    // 5. Snapshot field volatil sesi InProgress (baseline anti-overwrite, D-03/PART-04c).
    origSchedule = await db.queryString(
      `SELECT CONVERT(varchar(30), Schedule, 121) FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
    origDuration = await db.queryString(
      `SELECT CAST(ISNULL(DurationMinutes,-1) AS varchar(12)) FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    if (restoreError) throw restoreError;
  });

  test('tambah peserta eligible → sesi baru ready-status, InProgress utuh, notice info (PART-01/02/03/04)', async ({ page }) => {
    test.setTimeout(90_000);
    // Submit form Edit memicu confirm() schedule-change (originalSchedule format ≠ nilai input date →
    // mismatch palsu walau jadwal tak diubah). Default Playwright = dismiss → submit batal. Terima semua
    // dialog supaya submit lanjut (jadwal sesungguhnya tak berubah → POST aman).
    page.on('dialog', (d) => d.accept().catch(() => { /* already handled */ }));
    await login(page, 'admin');

    // GET halaman Edit untuk batch yang ada sesi InProgress-nya.
    const resp = await page.goto(`/Admin/EditAssessment?id=${inProgressSessionId}`);
    expect(resp?.status(), 'EditAssessment GET 2xx (PART-02: tak terblokir Completed/InProgress)').toBeLessThan(400);
    await page.locator('#editAssessmentForm').waitFor({ state: 'visible', timeout: 15_000 });

    // Dropdown Status hanya berisi {Open,Upcoming,Completed,Cancelled} — 'InProgress' (sesi seed)
    // tak ada di daftar → select default ke placeholder kosong → ModelState "Status required".
    // Set status valid 'Open' agar submit lolos. KRUSIAL untuk uji D-03: walau form post Status='Open',
    // controller TIDAK boleh meng-overwrite sesi yang sedang berjalan (StartedAt≠null,CompletedAt=null)
    // → assert di bawah membuktikan sesi InProgress TETAP 'InProgress' (proteksi sesi berjalan).
    await page.selectOption('#Status', 'Open');

    // Pilih 1 user eligible (checkbox NewUserIds) — view sudah exclude user yang sudah ter-assign,
    // jadi checkbox pertama = user BUKAN bagian batch (termasuk bukan rino yang InProgress).
    const firstCheck = page.locator('#newUserCheckboxContainer .new-user-checkbox').first();
    await firstCheck.waitFor({ state: 'attached', timeout: 10_000 });
    const checkedUserId = await firstCheck.getAttribute('value');
    expect(checkedUserId, 'ada minimal 1 user eligible untuk ditambah').toBeTruthy();
    await firstCheck.check({ force: true }); // container scroll → force klik aman

    // Submit form Edit → commit penambahan (BULK ASSIGN path, controller nyata).
    await Promise.all([
      page.waitForURL((u) => !u.toString().includes('/EditAssessment'), { timeout: 20_000 }),
      page.locator('#editAssessmentForm button[type="submit"]').click(),
    ]);

    // (d) PART-03 — notice info muncul (alert info/success, BUKAN error). Toleran: salah satu hadir.
    const notice = page.locator('.alert-info, .alert-success').filter({ hasText: /.+/ }).first();
    await expect(notice, 'notice info/success pasca-tambah (bukan alert error)').toBeVisible({ timeout: 10_000 });

    // (a+b) PART-01 — sesi baru untuk user yang ditambah LAHIR + ready-status (Open/Upcoming).
    const userIdSql = checkedUserId!.replace(/'/g, "''");
    const newSessionCount = await db.queryScalar(`
      SELECT COUNT(*) FROM AssessmentSessions
      WHERE Title = N'${batchTitle.replace(/'/g, "''")}' AND Category = N'${batchCategory.replace(/'/g, "''")}'
        AND CAST(Schedule AS DATE) = '${batchScheduleDate}'
        AND UserId = '${userIdSql}' AND RemovedAt IS NULL`);
    expect(newSessionCount, 'sesi baru untuk peserta yang ditambah tercatat').toBeGreaterThan(0);

    const newStatus = await db.queryString(`
      SELECT TOP 1 Status FROM AssessmentSessions
      WHERE Title = N'${batchTitle.replace(/'/g, "''")}' AND Category = N'${batchCategory.replace(/'/g, "''")}'
        AND CAST(Schedule AS DATE) = '${batchScheduleDate}'
        AND UserId = '${userIdSql}' AND RemovedAt IS NULL
      ORDER BY Id DESC`);
    expect(['Open', 'Upcoming'], `sesi baru ready-status (BUKAN InProgress) — dapat '${newStatus}'`).toContain(newStatus);

    // (c) PART-04 — sesi InProgress existing TIDAK ter-overwrite (Status/Schedule/Duration).
    const ipStatus = await db.queryString(
      `SELECT Status FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
    expect(ipStatus, 'sesi InProgress tetap InProgress (tak ter-overwrite)').toBe('InProgress');

    const ipSchedule = await db.queryString(
      `SELECT CONVERT(varchar(30), Schedule, 121) FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
    expect(ipSchedule, 'Schedule sesi InProgress tak berubah').toBe(origSchedule);

    const ipDuration = await db.queryString(
      `SELECT CAST(ISNULL(DurationMinutes,-1) AS varchar(12)) FROM AssessmentSessions WHERE Id = ${inProgressSessionId}`);
    expect(ipDuration, 'DurationMinutes sesi InProgress tak berubah').toBe(origDuration);
  });
});

// =====================================================================================
// PART 2 — sweep error: halaman inti v31+v32 tak boleh ada console-error / pageerror / 5xx
// =====================================================================================
test.describe('Audit v31+v32 · sweep — halaman inti bebas console-error & HTTP 5xx', () => {
  // Halaman GET param-free, relevan v31 (ujian/penilaian esai) + v32 (peserta/pekerja).
  const PAGES: { url: string; label: string }[] = [
    { url: '/Admin',               label: 'Kelola Data (index)' },
    { url: '/Admin/ManageWorkers', label: 'Kelola Pekerja (v32 WRKR)' },
    { url: '/Admin/CreateWorker',  label: 'Buat Pekerja (v32 392)' },
    { url: '/Admin/EssayGrading',  label: 'Penilaian Esai (v31)' },
    { url: '/CMP/Index',           label: 'CMP / daftar ujian' },
    { url: '/Home/Index',          label: 'Dashboard' },
  ];

  // Console noise jinak yang TIDAK dihitung sebagai error sistem.
  const BENIGN = /favicon|ERR_NETWORK_CHANGED|net::ERR_ABORTED|signalr.*negotiat|Failed to load resource.*404.*\.(map|ico)/i;

  for (const { url, label } of PAGES) {
    test(`bebas error: ${url} — ${label}`, async ({ page }) => {
      const consoleErrors: string[] = [];
      const serverErrors: string[] = [];
      page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
      page.on('pageerror', (e) => consoleErrors.push('pageerror: ' + e.message));
      page.on('response', (r) => { if (r.status() >= 500) serverErrors.push(`${r.status()} ${r.url()}`); });

      await login(page, 'admin');
      const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 20_000 });
      expect(resp?.status(), `${label}: navigasi top-level < 400`).toBeLessThan(400);
      await page.waitForTimeout(1_200); // beri jeda script async/SignalR sempat lapor error bila ada

      expect(serverErrors, `${label}: tak boleh ada respons HTTP 5xx`).toEqual([]);
      const fatal = consoleErrors.filter((e) => !BENIGN.test(e));
      expect(fatal, `${label}: tak boleh ada console-error/pageerror fatal`).toEqual([]);
    });
  }
});
