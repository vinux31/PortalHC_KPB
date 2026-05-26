import { Page, expect } from '@playwright/test';
import { exec } from 'node:child_process';
import { promisify } from 'node:util';

const execP = promisify(exec);

/**
 * Worker submit non-essay assessment by title (FULL flow: locate card -> start/resume -> answer all
 * questions with first radio option -> ExamSummary -> Submit -> Results page).
 *
 * Pattern derived from tests/e2e/exam-taking.spec.ts FLOW A tests A6+A7 (lines 136-193).
 *
 * Pre-req: caller HARUS sudah login via existing `login(page, accountKey)` helper
 * (tests/helpers/auth.ts). Assessment dengan judul `assessmentTitle` HARUS visible di
 * /CMP/Assessment untuk akun login (status Open atau In-Progress).
 */
export async function submitNonEssayAssessment(page: Page, assessmentTitle: string): Promise<void> {
  // Navigate ke daftar assessment worker
  await page.goto('/CMP/Assessment');

  // Locate card by title, click Start atau Resume
  const card = page.locator('.assessment-card', { hasText: assessmentTitle });
  await expect(card).toBeVisible({ timeout: 10_000 });
  const startOrResume = card.locator('a:has-text("Start"), a:has-text("Resume")').first();
  await startOrResume.click();
  await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

  // Handle optional resume confirmation modal (kalau Resume)
  const resumeModal = page.locator('#resumeConfirmModal');
  if (await resumeModal.isVisible({ timeout: 2_000 }).catch(() => false)) {
    await page.locator('#resumeConfirmBtn').click();
    await page.waitForTimeout(500);
  }

  // Wait untuk exam header + timer (FLOW A5 verify)
  await expect(page.locator('#examHeader')).toBeVisible({ timeout: 10_000 });
  await expect(page.locator('#examTimer')).toBeVisible({ timeout: 10_000 });

  // Loop semua question card, klik first radio option per question
  const questionCards = page.locator('[id^="qcard_"]');
  const qCount = await questionCards.count();
  if (qCount === 0) {
    throw new Error(`submitNonEssayAssessment: no question card ditemukan untuk assessment "${assessmentTitle}". Pastikan assessment punya soal MC.`);
  }
  for (let i = 0; i < qCount; i++) {
    const qCard = questionCards.nth(i);
    // Pilih first radio (deterministic - bukan testing correctness, testing TR insert behavior)
    await qCard.locator('.exam-radio').first().click({ force: true });
    await page.waitForTimeout(300); // micro-debounce
  }

  // Confirm semua terjawab via progress indicator (FLOW A6 line 168 pattern)
  await expect(page.locator('#answeredProgress')).toContainText(`${qCount}/${qCount}`, { timeout: 5_000 });

  // Navigate ke ExamSummary via reviewSubmit button (FLOW A7 line 185)
  await page.locator('#reviewSubmitBtn').click();
  await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
  await expect(page.locator('body')).toContainText('Submit Exam', { timeout: 5_000 });

  // Submit + handle confirm dialog (FLOW A7 line 190-192)
  page.once('dialog', d => d.accept());
  await page.click('button:has-text("Submit Exam")');
  await page.waitForURL('**/CMP/Results/**', { timeout: 30_000 });
}

/**
 * Navigate ke /CMP/Records, filter by session title, assert exact count untuk
 * "Assessment Online" + "Training Manual" row.
 *
 * Phase 324 primary assertion: post-fix `assessmentOnline: 1, trainingManual: 0`
 * (vs pre-fix `assessmentOnline: 1, trainingManual: 1` yang menyebabkan visual duplicate).
 */
export async function assertRecordsRowCount(
  page: Page,
  sessionTitle: string,
  expected: { assessmentOnline: number; trainingManual: number }
): Promise<void> {
  await page.goto('/CMP/Records');
  await page.waitForLoadState('networkidle');

  // Optional search filter kalau ada banyak row di /CMP/Records
  const searchInput = page.locator('input[type="search"], input[name="search"]').first();
  if (await searchInput.count() > 0) {
    await searchInput.fill(sessionTitle);
    await page.waitForTimeout(500); // debounce filter
  }

  // Count row "Assessment Online" (dari AssessmentSession branch GetUnifiedRecords)
  const assessmentRows = page.locator('tr', { hasText: 'Assessment Online' }).filter({ hasText: sessionTitle });
  await expect(assessmentRows).toHaveCount(expected.assessmentOnline, { timeout: 5_000 });

  // Count row "Training Manual" (dari TrainingRecord branch - post-fix expect 0)
  const trainingRows = page.locator('tr', { hasText: 'Training Manual' }).filter({ hasText: sessionTitle });
  await expect(trainingRows).toHaveCount(expected.trainingManual, { timeout: 5_000 });
}

/**
 * Shell-out ke sqlcmd untuk eksekusi SCALAR count query, parse integer dari output.
 *
 * SECURITY NOTE: query param TIDAK boleh berisi user input - caller responsibility.
 * Phase 324 spec hanya pakai literal const FIXTURE_SESSION_TITLE (controlled string).
 *
 * Helper ini juga di-reuse di Plan 03 Task 1+2+4 (DB schema verify + orphan check + post-cleanup verify).
 */
export async function sqlcmdQueryCount(query: string): Promise<number> {
  const cmd = `sqlcmd -S "localhost\\SQLEXPRESS" -E -d HcPortalDB_Dev -h -1 -W -Q "SET NOCOUNT ON; ${query.replace(/"/g, '\\"')}"`;
  const { stdout } = await execP(cmd);
  const match = stdout.match(/\d+/);
  if (!match) throw new Error(`sqlcmdQueryCount: sqlcmd returned no integer: ${stdout}`);
  return parseInt(match[0], 10);
}
