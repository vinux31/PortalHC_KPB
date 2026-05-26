import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import {
  submitNonEssayAssessment,
  assertRecordsRowCount,
  sqlcmdQueryCount,
} from './helpers/phase324';

// ============================================================
// FIXTURE TITLES - pre-req: HC sudah buat 2 assessment lokal via UI
// Phase 324 D-07a (CONTEXT.md): WAJIB IMPLEMENT S1 + S2
// Phase 324 D-07b (CONTEXT.md): S3-S7 DEFERRED ke Phase 325
//   (slug draft: complete-uat-phase324-s3-to-s7, akan di-spawn user via /gsd-add-phase)
// ============================================================
const FIXTURE_TITLE_S1 = '[Phase 324] Test Non-Essay';   // assessment non-essay biasa untuk Rino
const FIXTURE_TITLE_S2 = '[Phase 324] Test PreTest';     // PreTest untuk Iwan3

test.describe.serial('Phase 324 - No Duplicate TrainingRecord on Assessment Completion', () => {

  // ============================================================
  // S1: Worker submit non-essay -> /CMP/Records 1 row "Assessment Online"
  // DUPL-02a criteria 2 - PRIMARY regression guard untuk Plan 01 Task 1 (D-01)
  // ============================================================
  test('S1: Worker submit non-essay -> /CMP/Records 1 row "Assessment Online"', async ({ page }) => {
    // Baseline TR count pre-test (untuk verify post-test = baseline, bukan baseline+1)
    const preCount = await sqlcmdQueryCount(
      `SELECT COUNT(*) FROM TrainingRecords WHERE Judul = 'Assessment: ${FIXTURE_TITLE_S1}'`
    );

    // Worker login + submit
    await login(page, 'coachee');
    await submitNonEssayAssessment(page, FIXTURE_TITLE_S1);

    // PRIMARY ASSERTION: /CMP/Records show 1 row Assessment Online, 0 row Training Manual
    // Pre-fix: 1 + 1 (duplicate). Post-fix: 1 + 0 (sole source AssessmentSession).
    await assertRecordsRowCount(page, FIXTURE_TITLE_S1, {
      assessmentOnline: 1,
      trainingManual: 0,
    });

    // DB-level verify: post-submit TR count HARUS SAMA dengan preCount (bukan preCount+1)
    const postCount = await sqlcmdQueryCount(
      `SELECT COUNT(*) FROM TrainingRecords WHERE Judul = 'Assessment: ${FIXTURE_TITLE_S1}'`
    );
    expect(postCount).toBe(preCount); // POST-FIX: tidak ada TR baru ter-insert
  });

  // ============================================================
  // S2: PreTest tetap skip TR (regression guard existing behavior)
  // DUPL-02a criteria 3 - confirms PreTest branch unaffected oleh Phase 324 edit
  // ============================================================
  test('S2: PreTest tetap skip TR (regression guard)', async ({ page }) => {
    const preCount = await sqlcmdQueryCount(
      `SELECT COUNT(*) FROM TrainingRecords WHERE Judul = 'Assessment: ${FIXTURE_TITLE_S2}'`
    );

    await login(page, 'coachee2');
    await submitNonEssayAssessment(page, FIXTURE_TITLE_S2);

    // Records: PreTest tetap appear di Assessment Online branch (via AssessmentSession),
    // TIDAK ada Training Manual row (pre-existing PreTest skip behavior - line 264 pre-fix).
    await assertRecordsRowCount(page, FIXTURE_TITLE_S2, {
      assessmentOnline: 1,
      trainingManual: 0,
    });

    // DB verify: PreTest TIDAK pernah insert TR (pre-fix dan post-fix sama)
    const postCount = await sqlcmdQueryCount(
      `SELECT COUNT(*) FROM TrainingRecords WHERE Judul = 'Assessment: ${FIXTURE_TITLE_S2}'`
    );
    expect(postCount).toBe(preCount);
  });

  // ============================================================
  // S3-S7: DEFERRED ke Phase 325 (slug: complete-uat-phase324-s3-to-s7)
  // Rationale: setiap scenario butuh fixture seed assessment berbeda (essay vs running session
  // vs Pass-state vs Fail-state) + multi-actor orchestration (HC + worker). Context cost > 50%
  // single agent kalau dipaksa di Phase 324.
  // Decided 2026-05-26 via checker iter 3 BLOCKER 1 PHASE SPLIT.
  // Reference: CONTEXT.md D-07b, REQUIREMENTS.md DUPL-02b.
  // ============================================================

  test('S3: Essay flow finalize -> tidak insert TR', async ({ page }) => {
    // TODO Phase 325: butuh fixture seed essay assessment + HC FinalizeEssayGrading action.
    // Pattern reference: tests/e2e/essay-grading.spec.ts (kalau ada) atau buat baru.
    // Assertions: worker submit essay (status PendingGrading) -> HC finalize via
    //   /Admin/FinalizeEssayGrading -> /CMP/Records 1 row Assessment Online, 0 row Training Manual.
    test.skip(true, 'Implementasi di Phase 325 - butuh fixture seed essay assessment + sertifikat existing state. Lihat CONTEXT.md D-07b.');
  });

  test('S4: HC AkhiriUjian (force-end single) -> grading jalan, tidak insert TR', async ({ page }) => {
    // TODO Phase 325: butuh fixture seed running session + 1 worker yang belum submit.
    // Pattern reference: AssessmentMonitoringDetail HC view + AkhiriUjian action.
    // Assertions: HC click AkhiriUjian -> AssessmentSession.Status=Completed + IsPassed evaluated
    //   -> /CMP/Records 1 row Assessment Online, 0 row Training Manual.
    test.skip(true, 'Implementasi di Phase 325 - butuh fixture seed running session + 1 worker yang belum submit. Lihat CONTEXT.md D-07b.');
  });

  test('S5: HC AkhiriSemuaUjian (bulk) -> grading jalan untuk semua, tidak insert TR', async ({ page }) => {
    // TODO Phase 325: butuh fixture seed running session + multi-worker pending.
    // Pattern reference: AssessmentMonitoringDetail HC view + AkhiriSemuaUjian bulk action.
    // Assertions: HC click AkhiriSemuaUjian -> semua worker AssessmentSession.Status=Completed
    //   -> /CMP/Records per-worker 1 row Assessment Online, 0 row Training Manual.
    test.skip(true, 'Implementasi di Phase 325 - butuh fixture seed running session + multi-worker pending. Lihat CONTEXT.md D-07b.');
  });

  test('S6: Regrade Pass->Fail -> AssessmentSession.IsPassed update, no TR cascade', async ({ page }) => {
    // TODO Phase 325: butuh fixture seed Pass session + sertifikat existing untuk revoke verify.
    // Pattern reference: tests/e2e/edit-peserta-answers.spec.ts (Phase 321 EditPesertaAnswers spec).
    // Assertions: HC EditPesertaAnswers ganti jawaban supaya Pass->Fail, submit -> AssessmentSession.IsPassed=false
    //   + NomorSertifikat=null + TR Status TIDAK berubah jadi Failed (cascade dihapus Plan 01 D-03).
    test.skip(true, 'Implementasi di Phase 325 - butuh fixture seed Pass session + sertifikat existing state untuk revoke verify. Lihat CONTEXT.md D-07b.');
  });

  test('S7: Regrade Fail->Pass -> NomorSertifikat generate, no TR cascade', async ({ page }) => {
    // TODO Phase 325: butuh fixture seed Fail session (GenerateCertificate=true, !PreTest).
    // Pattern reference: tests/e2e/edit-peserta-answers.spec.ts (Phase 321).
    // Assertions: HC EditPesertaAnswers ganti jawaban supaya Fail->Pass, submit -> AssessmentSession.NomorSertifikat != null
    //   + TR row TIDAK ter-insert/update (cascade dihapus Plan 01 D-03).
    test.skip(true, 'Implementasi di Phase 325 - butuh fixture seed Fail session + GenerateCertificate=true assessment. Lihat CONTEXT.md D-07b.');
  });
});
