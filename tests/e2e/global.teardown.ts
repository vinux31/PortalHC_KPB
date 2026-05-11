// Phase 315 — globalTeardown pipeline: flush report FIRST → RESTORE → Layer 4 validation
// → journal cleaned → cleanup state.json + .bak.
//
// CRITICAL ordering (Pitfall: preserve findings):
//   matrixReport.flush() WAJIB sebelum db.restore() supaya kalau RESTORE crash, findings
//   tetap ter-record di markdown report (gak hilang).
//
// Layer 4 validation: post-RESTORE, expect 0 matrix rows. Throw kalau != 0 (T-315-03
// cleanup leak prevention — test infra integrity violation).
//
// Source spec: docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md (94bacecf)
// Pattern G (SEED_JOURNAL active → cleaned regex replace) per PATTERNS.md.

import type { FullConfig } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { collector } from './helpers/matrixReport';
import { readFile, unlink, writeFile } from 'fs/promises';

async function globalTeardown(_config: FullConfig): Promise<void> {
  const today = new Date().toISOString().slice(0, 10);
  const reportPath = `docs/test-reports/${today}-assessment-matrix.md`;

  // ============================================================
  // Step 1: FLUSH report FIRST (preserve findings sebelum RESTORE)
  // ============================================================
  try {
    await collector.flush(reportPath);
    console.log(`[teardown] Report ditulis: ${reportPath} (${collector.count()} findings)`);
  } catch (e) {
    console.error('[teardown] matrixReport.flush() gagal:', e);
    // JANGAN throw di sini — tetap lanjut RESTORE supaya DB tidak dirty.
  }

  // ============================================================
  // Step 2: Read state file untuk dapat snapshotPath
  // ============================================================
  let state: { snapshotPath: string } | null = null;
  try {
    const raw = await readFile('tests/.matrix-state.json', 'utf-8');
    state = JSON.parse(raw);
  } catch (e) {
    console.error('[teardown] tests/.matrix-state.json missing atau parse error:', e);
    throw new Error(
      'Teardown abort: state file tidak ditemukan. globalSetup belum jalan atau state.json terhapus. ' +
        'Manual restore via SSMS pakai snapshot terbaru di SQL default backup directory.'
    );
  }

  if (!state || !state.snapshotPath) {
    throw new Error('Teardown abort: state file invalid (snapshotPath kosong).');
  }

  // ============================================================
  // Step 3: RESTORE DB dari snapshot
  // ============================================================
  try {
    await db.restore(state.snapshotPath);
    console.log(`[teardown] RESTORE OK dari ${state.snapshotPath}`);
  } catch (e) {
    console.error('[teardown] RESTORE GAGAL — manual restore command:');
    console.error(
      `  sqlcmd -S "localhost\\SQLEXPRESS" -E -Q "USE master; ` +
        `ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ` +
        `RESTORE DATABASE HcPortalDB_Dev FROM DISK='${state.snapshotPath}' WITH REPLACE; ` +
        `ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"`
    );
    throw e;
  }

  // ============================================================
  // Step 4: Layer 4 validation — expect 0 matrix rows post-RESTORE.
  //   T-315-03 mitigation: kalau ada row tersisa, BACKUP/RESTORE bermasalah →
  //   throw untuk halt CI (jangan biarkan dirty state lolos ke run berikutnya).
  // ============================================================
  const remainingSessions = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[MATRIX_TEST_2026_05_11]%'`
  );
  if (remainingSessions !== 0) {
    console.error(
      `[teardown] Layer 4 FAIL: ${remainingSessions} matrix AssessmentSessions remain post-RESTORE`
    );
    throw new Error(
      `Layer 4 cleanup validation failed: ${remainingSessions} matrix rows remain post-RESTORE. ` +
        `BACKUP/RESTORE pipeline broken — investigate snapshot integrity di ${state.snapshotPath}.`
    );
  }
  console.log(`[teardown] Layer 4 OK: 0 matrix rows post-RESTORE`);

  // ============================================================
  // Step 5: Update SEED_JOURNAL.md entry dari 'active' → 'cleaned' (Pattern G regex)
  //   Target entry: baris yang berisi snapshot filename specific (matrix-*-.bak).
  //   Backslash di Windows path harus di-escape sebagai regex literal — pakai escape helper.
  // ============================================================
  try {
    const journalText = await readFile('docs/SEED_JOURNAL.md', 'utf-8');
    // Regex: cari baris journal dengan ".bak" yang mengandung "matrix" + status "active",
    // ganti active → cleaned. Pakai non-greedy match supaya tidak nyangkut baris lain.
    const updatedJournal = journalText.replace(
      /(\|\s*[^|]*matrix[^|]*\.bak\s*\|\s*)active(\s*\|)/,
      '$1cleaned$2'
    );
    if (updatedJournal === journalText) {
      console.warn(
        `[teardown] SEED_JOURNAL.md regex tidak match — entry mungkin sudah cleaned sebelumnya ` +
          `atau snapshot path format berbeda. Cek manual.`
      );
    } else {
      await writeFile('docs/SEED_JOURNAL.md', updatedJournal);
      console.log(`[teardown] SEED_JOURNAL.md updated → cleaned`);
    }
  } catch (e) {
    console.error('[teardown] SEED_JOURNAL.md update gagal (non-fatal):', e);
    // JANGAN throw — journal update bukan critical path; DB sudah clean.
  }

  // ============================================================
  // Step 6: Cleanup runtime artifacts (best-effort, swallow errors)
  // ============================================================
  await unlink('tests/.matrix-state.json').catch(() => {});
  await unlink(state.snapshotPath).catch(() => {});
  console.log(`[teardown] Cleanup state.json + ${state.snapshotPath} (best-effort).`);
}

export default globalTeardown;
