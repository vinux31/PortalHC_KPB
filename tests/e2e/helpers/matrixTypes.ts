// Phase 315 — Type definitions untuk assessment matrix discovery test
// Centralized types — single source of truth untuk consumer (matrixReport, examMatrix, spec utama).
// Source spec: docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md (commit 94bacecf)
// Wave 0 verdicts (315-INVESTIGATION.md):
//   - A1: 1-PER-SESSION → setiap sibling session punya AssessmentPackage row tersendiri.
//   - A2: DB-PERSISTED-AUTHORITATIVE → Essay text 100% lewat SignalR SaveTextAnswer; form value diabaikan server.
//   - A6: AUTO-CREATE-LAZY → UserPackageAssignment dibuat saat StartExam first hit; seed skip UPA.

/**
 * Severity klasifikasi finding di matrix discovery report.
 * - critical: blocking failure → skip sisa step skenario (softAssert throw SkipScenarioError).
 * - major: bug fungsional yang harus dilaporkan tapi tidak mem-block lanjutan skenario.
 * - minor: cosmetic / nice-to-have observation.
 */
export type Severity = 'critical' | 'major' | 'minor';

/**
 * Finding yang ter-record di Collector.
 * `isMeta` true → finding berasal dari sentinel `[META-*]` skenario, dipisah dari summary discovery.
 */
export type Finding = {
  scenarioId: number;
  scenarioTitle: string;
  step: string;
  expected: string;
  actual: string;
  screenshotPath?: string;
  severity: Severity;
  isMeta?: boolean;
};

/**
 * Per-question definition di dalam ScenarioConfig.
 * `correctOptionIds` + `allOptionIds` di-derive deterministic dari seed SQL (formula
 * `optId = 80001 + (qId - 50001) * 4 + optIndex` — single source of truth dengan seed Plan 03).
 * `findWrongOption` di examMatrix.ts memanfaatkan `allOptionIds \ correctOptionIds` untuk sabotage.
 */
export type QuestionConfig = {
  id: number;
  type: 'MultipleChoice' | 'MultipleAnswer' | 'Essay';
  scoreValue: number;
  correctOptionIds: number[];
  allOptionIds: number[];
};

/**
 * Scenario configuration — 1 skenario = 1 pasang sibling session (peserta1 + peserta2).
 * Pitfall 3 (RESEARCH.md line 723-738): AssessmentSession.UserId adalah single FK string,
 * sehingga "2 peserta per skenario" = 2 sibling sessions (Title+Category+ScheduleDate identik,
 * UserId berbeda). Total 9 skenario × 2 peserta = 18 sessions (range 9001-9018).
 */
export type ScenarioConfig = {
  id: number;                      // 1..7 untuk discovery, 8..10 untuk sentinel [META-*]
  sessionIdPeserta1: number;       // sibling session UserId = coachee (rino.prasetyo)
  sessionIdPeserta2: number;       // sibling session UserId = coachee2 (iwan3)
  title: string;                   // '[MATRIX_TEST_2026_05_11] <descriptive>' atau '[META-*]'
  type: 'Manual' | 'Online' | 'PreTest' | 'PostTest';
  category: string;                // 'Matrix Test Category'
  scheduleDate: string;            // 'YYYY-MM-DD'
  hasEssay: boolean;               // dipakai spec untuk skip gradeEssaysAsHc jika false
  questions: QuestionConfig[];
};
