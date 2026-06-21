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

  // ===== 7 sinyal (Task 2) — ditulis di bawah =====
});
