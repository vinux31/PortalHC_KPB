// Phase 315 — globalSetup pipeline: app-check (existing) → BACKUP → seed SQL → Layer 1 validation
// → state.json write → SEED_JOURNAL.md append (status=active).
//
// PRESERVE existing assertion (verify app is running di /Account/Login).
// Snapshot path: resolved runtime via SERVERPROPERTY('InstanceDefaultBackupPath') —
//   C:\Temp\ DIBLOKIR oleh SQL Server service account, jadi pakai default backup dir
//   (e.g. C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\).
//
// Source-of-truth: docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md (94bacecf)
// Wave 0 verdicts dipakai disini:
//   - A1 1-PER-SESSION → expect 18 sessions, 18 packages
//   - A6 AUTO-CREATE-LAZY → seed tidak insert UPA; Layer 1 expect UPA=0
//   - D-05 Title prefix marker → Layer 1 filter '[MATRIX_TEST_2026_05_11]%'

import { test as setup, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { writeFile, appendFile, mkdir } from 'fs/promises';
import { resolve } from 'path';

// Path resolver — semua path resolved relatif __dirname (`tests/e2e/`) supaya
// independent dari cwd Playwright runner (cwd bisa `tests/` saat `cd tests && npx
// playwright test`, atau worktree-root via custom invocation). Tanpa ini, writeFile
// 'tests/.matrix-state.json' dengan cwd=tests/ akan resolve ke `tests/tests/...` (Rule 3
// blocking fix saat Plan 04 smoke run).
const TESTS_DIR = resolve(__dirname, '..');                    // -> tests/
const REPO_ROOT = resolve(__dirname, '..', '..');              // -> worktree root
const STATE_FILE = resolve(TESTS_DIR, '.matrix-state.json');
const SEED_SQL = resolve(TESTS_DIR, 'sql', 'assessment-matrix-seed.sql');
const REPORTS_DIR = resolve(REPO_ROOT, 'docs', 'test-reports');
const JOURNAL_FILE = resolve(REPO_ROOT, 'docs', 'SEED_JOURNAL.md');

setup('verify app is running + seed matrix', async ({ page }) => {
  // ===== EXISTING (PRESERVE) =====
  const response = await page.goto('/Account/Login');
  expect(response?.ok()).toBeTruthy();
  await expect(page.locator('button[type="submit"]')).toBeVisible();

  // ===== NEW Phase 315: matrix test setup =====

  // Step 1: Resolve SQL Server default backup directory (C:\Temp\ blocked oleh service account).
  // SERVERPROPERTY returns path dengan trailing backslash. Pakai forward-slash untuk Node fs
  // (SQL Server accept both).
  const defaultBackupDirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  // Normalize: strip trailing backslash, convert backslash → forward slash.
  const defaultBackupDir = defaultBackupDirRaw.replace(/\\+$/, '').replace(/\\/g, '/');

  // Step 2: Build snapshot path. Timestamp sanitized (replace `:` dan `.` → `-` supaya valid
  // Windows filename + tidak terinterpretasi sebagai NTFS alternate-data-stream).
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  const snapshotPath = `${defaultBackupDir}/HcPortalDB_Dev-matrix-${ts}.bak`;
  console.log(`[setup] Snapshot path: ${snapshotPath}`);

  // Step 3: Pre-check ID collision (defense — RESEARCH Q4 mitigation). Seed SQL juga punya
  // THROW guard sendiri, tapi cek di TS layer supaya error message lebih jelas + skip
  // BACKUP step kalau collision sudah jelas.
  const collisionCount = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9018`
  );
  expect(
    collisionCount,
    `Pre-check: ID range 9001-9018 sudah dipakai (${collisionCount} row di AssessmentSessions). ` +
      `Inspect: SELECT Id, Title FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9018. ` +
      `Run cleanup manual atau pilih range lain.`
  ).toBe(0);

  // Step 4: BACKUP DB lokal (SEED_WORKFLOW.md §5.1 SOP — wajib sebelum seed temporary).
  await db.backup(snapshotPath);
  console.log(`[setup] BACKUP OK: ${snapshotPath}`);

  // Step 5: Execute seed SQL (idempotent — internal cleanup chain handle re-run aman).
  await db.execScript(SEED_SQL);
  console.log(`[setup] Seed SQL executed: ${SEED_SQL}`);

  // Step 6: Layer 1 validation — expect 18 sessions tagged.
  // Multiple count checks supaya kalau gagal, error message indikatif (bukan generic "expected 18 got X").
  const sessionCount = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]MATRIX_TEST_2026_05_11]%'`
  );
  expect(sessionCount, `Layer 1: expected 18 matrix AssessmentSessions seeded`).toBe(18);

  const packageCount = await db.queryScalar(
    `SELECT COUNT(*) FROM AssessmentPackages WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]MATRIX_TEST_2026_05_11]%')`
  );
  expect(packageCount, `Layer 1: expected 10 matrix AssessmentPackages seeded (peserta2 shares via sibling pool)`).toBe(10);

  const questionCount = await db.queryScalar(
    `SELECT COUNT(*) FROM PackageQuestions WHERE AssessmentPackageId BETWEEN 9001 AND 9018`
  );
  expect(questionCount, `Layer 1: expected 30 PackageQuestions seeded (peserta1 only)`).toBe(30);

  const optionCount = await db.queryScalar(
    `SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId BETWEEN 50001 AND 50054`
  );
  expect(optionCount, `Layer 1: expected 80 PackageOptions seeded`).toBe(80);

  // A6 verdict cross-check: UPA harus 0 (lazy create saat StartExam).
  const upaCount = await db.queryScalar(
    `SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId BETWEEN 9001 AND 9018`
  );
  expect(upaCount, `Layer 1 (A6 verdict): UPA must be 0 pre-test (auto-create-lazy)`).toBe(0);

  console.log(`[setup] Layer 1 OK: sessions=${sessionCount} packages=${packageCount} questions=${questionCount} options=${optionCount} UPA=${upaCount}`);

  // Step 7: Pre-create docs/test-reports/ supaya teardown matrixReport.flush() tidak race
  // mkdir di parallel run.
  await mkdir(REPORTS_DIR, { recursive: true });

  // Step 8: Write tests/.matrix-state.json — consumed oleh Plan 04 spec + globalTeardown.
  const scenarios = buildScenarios();
  const stateJson = {
    snapshotPath,
    seededAt: new Date().toISOString(),
    scenarios,
  };
  await writeFile(STATE_FILE, JSON.stringify(stateJson, null, 2));
  console.log(`[setup] State file written: ${STATE_FILE} (${scenarios.length} scenarios)`);

  // Step 9: Append SEED_JOURNAL.md entry (status=active). globalTeardown akan regex-replace
  // active → cleaned setelah RESTORE + Layer 4 sukses.
  const today = new Date().toISOString().slice(0, 10);
  const journalEntry =
    `| ${today} | 315 | temporary + local-only | Assessment matrix discovery test sweep — ` +
    `7 discovery + 3 sentinel scenario (matrix-test, MATRIX_TEST_2026_05_11) | ` +
    `AssessmentSessions(18), Packages(18), Questions(54), Options(144) prefix [MATRIX_TEST_2026_05_11] | ` +
    `${snapshotPath} | active |\n`;
  await appendFile(JOURNAL_FILE, journalEntry);
  console.log(`[setup] SEED_JOURNAL.md appended (status=active): ${JOURNAL_FILE}`);
});

// ============================================================
// buildScenarios() — single source of truth untuk ScenarioConfig list
// dipakai oleh spec utama (Plan 04) via tests/.matrix-state.json.
//
// DETERMINISTIC FORMULA (mirror dari tests/sql/assessment-matrix-seed.sql header):
//   optId = 80001 + (qId - 50001) * 4 + optIndex
//   where qId ∈ [50001..50054], optIndex ∈ [0..3]
//   MC: correctOptionIds = [opt(0)]            → [80001 + (qId-50001)*4 + 0]
//   MA: correctOptionIds = [opt(0), opt(1)]    → [80001 + (qId-50001)*4 + 0, +1]
//   Essay: correctOptionIds = [], allOptionIds = []  (Essay tidak punya options di DB)
//
// JANGAN hand-type literal arrays — pakai formula closure. Mismatch dengan seed = bug.
// ============================================================

type QuestionType = 'MultipleChoice' | 'MultipleAnswer' | 'Essay';

interface QuestionConfig {
  id: number;
  type: QuestionType;
  scoreValue: number;
  correctOptionIds: number[];
  allOptionIds: number[];
}

interface ScenarioConfig {
  id: number;
  title: string;
  sessionIdPeserta1: number;
  sessionIdPeserta2: number;
  type: 'Manual' | 'Online' | 'PreTest' | 'PostTest';
  category: string;
  scheduleDate: string;
  hasEssay: boolean;
  questions: QuestionConfig[];
}

function buildScenarios(): ScenarioConfig[] {
  const optBase = (qId: number): number => 80001 + (qId - 50001) * 4;
  const optAll = (qId: number): number[] => [
    optBase(qId),
    optBase(qId) + 1,
    optBase(qId) + 2,
    optBase(qId) + 3,
  ];
  const mcQ = (qId: number, scoreValue: number): QuestionConfig => ({
    id: qId,
    type: 'MultipleChoice',
    scoreValue,
    correctOptionIds: [optBase(qId)], // optIndex 0
    allOptionIds: optAll(qId),
  });
  const maQ = (qId: number, scoreValue: number): QuestionConfig => ({
    id: qId,
    type: 'MultipleAnswer',
    scoreValue,
    correctOptionIds: [optBase(qId), optBase(qId) + 1], // optIndex 0+1
    allOptionIds: optAll(qId),
  });
  const essayQ = (qId: number, scoreValue: number): QuestionConfig => ({
    id: qId,
    type: 'Essay',
    scoreValue,
    correctOptionIds: [],
    allOptionIds: [], // Essay: no options seeded
  });

  return [
    {
      id: 1,
      title: '[MATRIX_TEST_2026_05_11] S1 Manual Mixed',
      sessionIdPeserta1: 9001,
      sessionIdPeserta2: 9002,
      type: 'Manual',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50001, 30), maQ(50002, 35), essayQ(50003, 35)],
    },
    {
      id: 2,
      title: '[MATRIX_TEST_2026_05_11] S2 Online Mixed',
      sessionIdPeserta1: 9003,
      sessionIdPeserta2: 9004,
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50007, 30), maQ(50008, 35), essayQ(50009, 35)],
    },
    {
      id: 3,
      title: '[MATRIX_TEST_2026_05_11] S3 PreTest Mixed',
      sessionIdPeserta1: 9005,
      sessionIdPeserta2: 9006,
      type: 'PreTest',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50013, 30), maQ(50014, 35), essayQ(50015, 35)],
    },
    {
      id: 4,
      title: '[MATRIX_TEST_2026_05_11] S4 PostTest Mixed',
      sessionIdPeserta1: 9007,
      sessionIdPeserta2: 9008,
      type: 'PostTest',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50019, 30), maQ(50020, 35), essayQ(50021, 35)],
    },
    {
      id: 5,
      title: '[MATRIX_TEST_2026_05_11] S5 Online MC only',
      sessionIdPeserta1: 9009,
      sessionIdPeserta2: 9010,
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: false,
      questions: [mcQ(50025, 34), mcQ(50026, 33), mcQ(50027, 33)],
    },
    {
      id: 6,
      title: '[MATRIX_TEST_2026_05_11] S6 Online MA only',
      sessionIdPeserta1: 9011,
      sessionIdPeserta2: 9012,
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: false,
      questions: [maQ(50031, 34), maQ(50032, 33), maQ(50033, 33)],
    },
    {
      id: 7,
      title: '[MATRIX_TEST_2026_05_11] S7 Online Essay only',
      sessionIdPeserta1: 9013,
      sessionIdPeserta2: 9014,
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [essayQ(50037, 34), essayQ(50038, 33), essayQ(50039, 33)],
    },
    {
      id: 8,
      title: '[MATRIX_TEST_2026_05_11] [META-AllCorrect] Sentinel',
      sessionIdPeserta1: 9015,
      sessionIdPeserta2: 9016,
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50043, 30), maQ(50044, 35), essayQ(50045, 35)],
    },
    {
      id: 9,
      title: '[MATRIX_TEST_2026_05_11] [META-AllWrong] Sentinel',
      sessionIdPeserta1: 9017,
      sessionIdPeserta2: 9017, // sentinel single-peserta (coachee saja)
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50049, 30), maQ(50050, 35), essayQ(50051, 35)],
    },
    {
      id: 10,
      title: '[MATRIX_TEST_2026_05_11] [META-CollectorCheck] Sentinel',
      sessionIdPeserta1: 9018,
      sessionIdPeserta2: 9018, // sentinel single-peserta (coachee saja, test.fail() block)
      type: 'Online',
      category: 'Matrix Test Category',
      scheduleDate: '2026-05-11',
      hasEssay: true,
      questions: [mcQ(50052, 30), maQ(50053, 35), essayQ(50054, 35)],
    },
  ];
}
