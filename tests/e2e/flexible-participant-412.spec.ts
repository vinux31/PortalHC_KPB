// Phase 413-02 e2e — 7 sinyal LIVE add/remove/restore/force-kick/broadcast (handoff 412-VALIDATION
// §Deferred to Phase 413). Multi-context SignalR (real browser, lesson Phase 354: build+grep tak
// cukup untuk Razor/JS/SignalR dinamis). Pola kanonik Flow O (exam-types.spec.ts:735-785).
//
// 7 sinyal (sumber kebenaran = handoff 412):
//   (a) add live (PART-05)     — admin Tambah picker → baris muncul live di tabel aktif TANPA reload.
//   (b) modal keras (PRMV-02)  — peserta InProgress → #hapusPesertaHardModal (bukan ringan).
//   (c) force-kick (PRMV-02)   — 2-ctx: admin hapus InProgress → worker #examRemovedModal + redirect /CMP/Assessment.
//   (d) panel removed (PLIV-01)— baris soft-removed pindah ke #tbodyRemoved live.
//   (e) restore (PRMV-04/D-04) — .btn-restore-peserta 1-klik → baris balik aktif live.
//   (f) count exclude (Pitfall2)— updateSummaryFromDOM count aktif exclude #tbodyRemoved.
//   (g) multi-observer (PLIV-02)— admin A + admin B lihat perubahan sama tanpa reload.
//
// SEED_WORKFLOW (CLAUDE.md + docs/SEED_WORKFLOW.md): beforeAll BACKUP HcPortalDB_Dev + flip 1 sesi
// (PUNYA paket soal, non-Proton, non-pair, milik rino) ke InProgress; afterAll RESTORE + .bak unlink.
// Klasifikasi seed = temporary + local-only. SEED_JOURNAL entry 413 active→cleaned.
//
// Run: app @localhost:5277 (Authentication__UseActiveDirectory=false) + SQLEXPRESS hidup.
//   cd tests && npx playwright test flexible-participant-412 --workers=1   (NTLM loopback → --workers=1)

import { test, expect, Page, Browser } from '@playwright/test';
import { login } from '../helpers/auth';
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

// ---- module-scope state (di-resolve di beforeAll, dipakai lintas test serial) ----
let snapshotPath = '';
let inProgressSessionId = 0;       // sesi yang di-flip InProgress (force-kick target, milik rino)
let batchTitle = '';
let batchCategory = '';
let batchScheduleDate = '';        // yyyy-MM-dd (CONVERT 23 — hindari tz drift)
let monitoringUrl = '';

// Worker login: sesi InProgress di-resolve agar dimiliki account 'coachee' (rino). Seed SQL
// memfilter `UserId = rino` → tak perlu flip UserId. Didokumentasikan di SUMMARY.
const WORKER_ACCOUNT = 'coachee' as const;   // rino.prasetyo@pertamina.com

function buildMonitoringUrl(title: string, category: string, scheduleDate: string): string {
  return '/Admin/AssessmentMonitoringDetail'
    + '?title=' + encodeURIComponent(title)
    + '&category=' + encodeURIComponent(category)
    + '&scheduleDate=' + scheduleDate;
}

// Tunggu SignalR hub Connected (Pitfall 3 — fire aksi sebelum Connected = false-fail).
async function waitHubConnected(page: Page, timeout = 15_000): Promise<void> {
  await page.waitForFunction(
    () => {
      const w = window as unknown as { assessmentHub?: { state?: string } };
      return w.assessmentHub?.state === 'Connected';
    },
    undefined,
    { timeout }
  );
}

test.describe.configure({ mode: 'serial' });

test.describe('Phase 413 — 7 sinyal live add/remove/restore/force-kick/broadcast (multi-context SignalR)', () => {

  test.beforeAll(async () => {
    // 1. Resolve default backup dir (C:\Temp blocked oleh SQL service account).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre413-${ts}.bak`;

    // 2. Snapshot SEBELUM seed (SEED_WORKFLOW T-413-E1 mitigation).
    await db.backup(snapshotPath);

    // 3. Flip 1 sesi (punya paket, non-Proton, non-pair, milik rino) → InProgress.
    await db.execScript(path.resolve(__dirname, '../sql/flexible-participant-413-seed.sql'));

    // 4. Resolve sesi InProgress + batch key (server-side truth, hindari tz drift).
    inProgressSessionId = await db.queryScalar(`
      SELECT TOP 1 s.Id FROM AssessmentSessions s
      WHERE s.Status = 'InProgress' AND s.RemovedAt IS NULL
        AND s.LinkedGroupId IS NULL AND s.Category <> 'Assessment Proton'
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
    monitoringUrl = buildMonitoringUrl(batchTitle, batchCategory, batchScheduleDate);

    // 5. Layer-1 assert: sesi InProgress benar-benar ter-seed.
    const seeded = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${inProgressSessionId} AND Status = 'InProgress'`);
    expect(seeded, 'Layer 1: seed InProgress applied').toBe(1);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    // WR-02 (pending-grade pola): tangkap error restore independen supaya tak tertutup assertion.
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs');
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    // Layer-4 (informational): sesi balik baseline (Status != InProgress sisa seed).
    let residualInProgress = -1;
    if (inProgressSessionId > 0) {
      residualInProgress = await db.queryScalar(
        `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${inProgressSessionId} AND Status = 'InProgress'`);
    }
    if (restoreError) throw restoreError;   // surface error restore asli
    expect(residualInProgress, 'Layer 4: sesi balik baseline (bukan InProgress sisa seed)').toBe(0);
  });

  // sesi yang ditambahkan via picker di t(a) — dipakai ulang di t(g) multi-observer.
  let addedSessionId = 0;

  // ============================================================================
  // t(a) — add live: picker → baris muncul live di tabel aktif TANPA reload (PART-05)
  // ============================================================================
  test('add live — picker Tambah → baris muncul live tanpa reload (PART-05)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto(monitoringUrl);
    await waitHubConnected(page);

    // Buka picker → tunggu daftar eligible ter-render (GetEligibleParticipantsToAdd).
    await page.locator('#btnTambahPeserta').click();
    const firstCheck = page.locator('.tambah-peserta-check').first();
    await firstCheck.waitFor({ state: 'visible', timeout: 10_000 });
    await firstCheck.check();

    // Intercept respons AddParticipantsLive → ambil added[].id (deterministik, bukan tebak DOM).
    const respPromise = page.waitForResponse(
      (r) => r.url().includes('/AddParticipantsLive') && r.request().method() === 'POST',
      { timeout: 12_000 }
    );
    await page.locator('#btnKonfirmasiTambah').click();
    const resp = await respPromise;
    expect(resp.ok(), 'AddParticipantsLive 2xx').toBeTruthy();
    const body = await resp.json();
    const added = (body && body.added) || [];
    expect(added.length, 'minimal 1 peserta ditambahkan').toBeGreaterThan(0);
    addedSessionId = added[0].id;

    // Baris baru muncul LIVE di tabel aktif (participantAdded handler / fallback inject) TANPA reload.
    await page.waitForSelector(
      `tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`,
      { timeout: 10_000 }
    );

    // Komplemen DB: sesi baru tercatat aktif (RemovedAt NULL).
    const liveCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${addedSessionId} AND RemovedAt IS NULL`);
    expect(liveCount, 'sesi baru aktif di DB').toBe(1);
  });

  // ============================================================================
  // t(b+c+d+f) — force-kick 2-context: admin hapus InProgress → worker examRemoved + redirect;
  // baris pindah ke #tbodyRemoved live; count aktif exclude removed. (PRMV-02, PLIV-01, Pitfall 2)
  // ============================================================================
  test('force kick worker — admin hapus InProgress → worker examRemoved + panel removed + count exclude (PRMV-02/PLIV-01)', async ({ browser }: { browser: Browser }) => {
    test.setTimeout(90_000);
    const ctxWorker = await browser.newContext();   // cookie-isolated (Pitfall 4)
    const ctxHc = await browser.newContext();
    const pageWorker = await ctxWorker.newPage();
    const pageHc = await ctxHc.newPage();

    try {
      // Worker: login + masuk ujian InProgress (seed) via Resume.
      await login(pageWorker, WORKER_ACCOUNT);
      await pageWorker.goto(`/CMP/StartExam/${inProgressSessionId}`);
      // Resume scenario: overlay hide dulu, modal resume bila muncul (pola exam-types O4).
      const loadingOverlay = pageWorker.locator('#examLoadingOverlay');
      if (await loadingOverlay.count() > 0) {
        await expect(loadingOverlay).not.toBeVisible({ timeout: 15_000 });
      }
      const resumeModal = pageWorker.locator('#resumeConfirmModal');
      if (await resumeModal.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await pageWorker.locator('#resumeConfirmBtn').click();
        await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
      }
      await waitHubConnected(pageWorker);
      // Buffer pendek: server set status InProgress + worker join grup batch (Pitfall 3).
      await pageWorker.waitForTimeout(2_000);   // satu-satunya buffer post-StartExam (≤2s, bukan tunggu SignalR)

      // Admin: login (context terpisah) + monitoring + hub Connected.
      await login(pageHc, 'admin');
      await pageHc.goto(monitoringUrl);
      await waitHubConnected(pageHc);

      // Snapshot count aktif SEBELUM hapus (Pitfall 2 — count exclude #tbodyRemoved).
      const activeBefore = await pageHc.locator('tbody:not(#tbodyRemoved) tr[data-session-id]').count();

      // (b) modal KERAS — peserta InProgress → #hapusPesertaHardModal (BUKAN ringan).
      await pageHc.locator(`tr[data-session-id="${inProgressSessionId}"] .btn-hapus-peserta`).first().click();
      await expect(pageHc.locator('#hapusPesertaHardModal')).toBeVisible({ timeout: 8_000 });
      await expect(pageHc.locator('#hapusPesertaLightModal')).toBeHidden();

      // Fire force-kick: isi alasan + konfirmasi keras.
      await pageHc.fill('#hapusHardReason', 'force-kick e2e test 413');
      await pageHc.locator('#btnHapusHardKonfirmasi').click();

      // (c) ASSERT worker: examRemoved modal live + pesan verbatim + redirect /CMP/Assessment.
      await expect(pageWorker.locator('#examRemovedModal')).toBeVisible({ timeout: 12_000 });
      await expect(pageWorker.locator('#examRemovedModal'))
        .toContainText('Anda telah dikeluarkan dari ujian ini.');
      await pageWorker.waitForURL('**/CMP/Assessment**', { timeout: 15_000 });

      // (d) ASSERT admin: baris pindah ke #tbodyRemoved live (soft-remove → panel).
      await pageHc.waitForSelector(
        `#tbodyRemoved tr[data-session-id="${inProgressSessionId}"][data-removed="true"]`,
        { timeout: 10_000 }
      );
      await expect(pageHc.locator('#panelPesertaDikeluarkan')).toBeVisible();
      // baris hilang dari tabel aktif
      await expect(
        pageHc.locator(`tbody:not(#tbodyRemoved) tr[data-session-id="${inProgressSessionId}"]`)
      ).toHaveCount(0);

      // (f) count aktif TURUN 1 (exclude removed) — updateSummaryFromDOM (Pitfall 2).
      await expect
        .poll(async () => pageHc.locator('tbody:not(#tbodyRemoved) tr[data-session-id]').count(),
              { timeout: 8_000 })
        .toBe(activeBefore - 1);

      // ASSERT DB: soft-remove tertulis (RemovedAt NOT NULL).
      const removed = await db.queryScalar(
        `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${inProgressSessionId} AND RemovedAt IS NOT NULL`);
      expect(removed, 'soft-remove tertulis DB').toBe(1);
    } finally {
      await ctxWorker.close().catch(() => { /* already closed */ });
      await ctxHc.close().catch(() => { /* already closed */ });
    }
  });

  // ============================================================================
  // t(e) — restore: .btn-restore-peserta 1-klik → baris balik ke aktif live (PRMV-04/D-04)
  // ============================================================================
  test('restore — .btn-restore-peserta 1-klik → baris balik aktif live (PRMV-04/D-04)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto(monitoringUrl);
    await waitHubConnected(page);

    // Baris removed (dari force-kick) ada di panel — buka collapse bila perlu.
    const removedRow = page.locator(`#tbodyRemoved tr[data-session-id="${inProgressSessionId}"]`);
    await removedRow.waitFor({ state: 'attached', timeout: 8_000 });
    // Klik Restore (1-klik, tanpa konfirmasi — D-04). force:true karena panel bisa collapsed.
    await page.locator(`#tbodyRemoved tr[data-session-id="${inProgressSessionId}"] .btn-restore-peserta`)
      .click({ force: true });

    // Baris balik ke tabel aktif live (participantAdded restore-from-panel).
    await page.waitForSelector(
      `tbody:not(#tbodyRemoved) tr[data-session-id="${inProgressSessionId}"]`,
      { timeout: 10_000 }
    );
    await expect(
      page.locator(`#tbodyRemoved tr[data-session-id="${inProgressSessionId}"]`)
    ).toHaveCount(0);

    // ASSERT DB: RemovedAt clear (aktif lagi).
    const active = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${inProgressSessionId} AND RemovedAt IS NULL`);
    expect(active, 'restore clear RemovedAt di DB').toBe(1);
  });

  // ============================================================================
  // t(g) — multi-observer: admin A hapus → admin B lihat baris pindah ke panel TANPA reload (PLIV-02)
  // ============================================================================
  test('multi observer — admin A hapus → admin B lihat perubahan live (PLIV-02)', async ({ browser }: { browser: Browser }) => {
    test.setTimeout(90_000);
    expect(addedSessionId, 'sesi added dari t(a) tersedia').toBeGreaterThan(0);

    const ctxA = await browser.newContext();   // admin (fire)
    const ctxB = await browser.newContext();   // hc (observer)
    const pageA = await ctxA.newPage();
    const pageB = await ctxB.newPage();

    try {
      await login(pageA, 'admin');
      await login(pageB, 'hc');
      await pageA.goto(monitoringUrl);
      await pageB.goto(monitoringUrl);
      await waitHubConnected(pageA);
      await waitHubConnected(pageB);

      // Pastikan baris added (Not started) ada di kedua observer (aktif).
      await pageA.waitForSelector(`tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`, { timeout: 8_000 });
      await pageB.waitForSelector(`tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`, { timeout: 8_000 });

      // A fire remove (Not started → modal ringan, tanpa alasan wajib).
      await pageA.locator(`tr[data-session-id="${addedSessionId}"] .btn-hapus-peserta`).first().click();
      // Not started + no cert → modal RINGAN.
      const lightModal = pageA.locator('#hapusPesertaLightModal');
      const hardModal = pageA.locator('#hapusPesertaHardModal');
      if (await lightModal.isVisible({ timeout: 4_000 }).catch(() => false)) {
        await pageA.locator('#btnHapusLightKonfirmasi').click();
      } else {
        // fallback bila tier keras (mis. punya cert) — isi alasan.
        await expect(hardModal).toBeVisible({ timeout: 4_000 });
        await pageA.fill('#hapusHardReason', 'multi-observer e2e 413');
        await pageA.locator('#btnHapusHardKonfirmasi').click();
      }

      // ASSERT observer B: baris hilang dari tabel aktif TANPA reload (broadcast participantRemoved).
      await expect
        .poll(async () => pageB.locator(`tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`).count(),
              { timeout: 10_000 })
        .toBe(0);
    } finally {
      await ctxA.close().catch(() => { /* already closed */ });
      await ctxB.close().catch(() => { /* already closed */ });
    }
  });
});
