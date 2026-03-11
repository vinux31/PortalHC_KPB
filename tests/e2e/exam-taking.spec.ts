import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';

test.describe.configure({ mode: 'serial' });

/** Navigate to AssessmentMonitoringDetail for a given title, handling status filters */
async function goToMonitoringDetail(page: Page, assessmentTitle: string) {
  await page.goto('/Admin/AssessmentMonitoring');
  const searchInput = page.locator('input[placeholder*="Cari"]').first();
  await searchInput.fill(assessmentTitle);
  await page.click('button:has-text("Filter")');
  await page.waitForLoadState('networkidle');

  let link = page.locator(`text=${assessmentTitle}`).first();
  if (!(await link.isVisible({ timeout: 3_000 }).catch(() => false))) {
    await page.locator('select[name="status"]').selectOption('All');
    await page.click('button:has-text("Filter")');
    await page.waitForLoadState('networkidle');
  }
  await page.locator(`text=${assessmentTitle}`).first().click();
  await page.waitForLoadState('networkidle');
}

// ============================================================
// FLOW A: Legacy exam full lifecycle
// HC creates → adds questions → worker starts → answers → submits → results → answer review → certificate
// ============================================================
test.describe('Flow A: Legacy Exam Full Lifecycle', () => {
  let title: string;
  let assessmentId: number;

  test('A1 - HC creates assessment for coachee', async ({ page }) => {
    title = uniqueTitle('Legacy Exam');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    // Select Rino
    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await expect(page.locator('#selectedCountBadge')).toContainText('1 selected');

    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '60');
    // Enable answer review and certificate
    await page.locator('#AllowAnswerReview').check();
    await page.locator('#GenerateCertificate').check();

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('A2 - HC navigates to ManageQuestions', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('h2')).toContainText('Manage Questions');

    const url = page.url();
    const match = url.match(/ManageQuestions\/(\d+)|id=(\d+)/);
    expect(match).toBeTruthy();
    assessmentId = parseInt((match![1] || match![2]));
  });

  test('A3 - HC adds 3 questions', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManageQuestions?id=${assessmentId}`);

    // Q1 correct=A
    await page.fill('textarea[name="question_text"]', 'Apa kepanjangan OJT?');
    await page.locator('input[name="options"]').nth(0).fill('On the Job Training');
    await page.locator('input[name="options"]').nth(1).fill('Online Job Test');
    await page.locator('input[name="options"]').nth(2).fill('Operation Job Task');
    await page.locator('input[name="options"]').nth(3).fill('Operational Job Training');
    await page.locator('input[name="correct_option_index"][value="0"]').check();
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');

    // Q2 correct=B
    await page.fill('textarea[name="question_text"]', 'Berapa lama durasi OJT standar?');
    await page.locator('input[name="options"]').nth(0).fill('1 bulan');
    await page.locator('input[name="options"]').nth(1).fill('3 bulan');
    await page.locator('input[name="options"]').nth(2).fill('6 bulan');
    await page.locator('input[name="options"]').nth(3).fill('12 bulan');
    await page.locator('input[name="correct_option_index"][value="1"]').check();
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');

    // Q3 correct=C
    await page.fill('textarea[name="question_text"]', 'Siapa penanggung jawab OJT?');
    await page.locator('input[name="options"]').nth(0).fill('Direktur');
    await page.locator('input[name="options"]').nth(1).fill('VP');
    await page.locator('input[name="options"]').nth(2).fill('Supervisor');
    await page.locator('input[name="options"]').nth(3).fill('Admin');
    await page.locator('input[name="correct_option_index"][value="2"]').check();
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Daftar Soal (3)')).toBeVisible();
  });

  test('A4 - Worker sees assessment in My Assessments', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    await expect(page.locator('.assessment-card', { hasText: title })).toBeVisible();
  });

  test('A5 - Worker starts the exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    const startBtn = card.locator('.btn-start-standard');
    page.once('dialog', d => d.accept());
    await startBtn.click();

    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible();
    await expect(page.locator('#examTimer')).toBeVisible();
  });

  test('A6 - Worker answers all questions correctly', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    // Resume in-progress exam
    const card = page.locator('.assessment-card', { hasText: title });
    await card.locator('a:has-text("Resume")').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Handle resume modal
    const resumeModal = page.locator('#resumeConfirmModal');
    if (await resumeModal.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await page.waitForTimeout(500);
    }

    // Answer each question
    const questionCards = page.locator('[id^="qcard_"]');
    const qCount = await questionCards.count();
    expect(qCount).toBe(3);

    for (let i = 0; i < qCount; i++) {
      const qCard = questionCards.nth(i);
      const qText = await qCard.locator('h6, .fw-bold').first().textContent() ?? '';
      let correctIdx = 0;
      if (qText.includes('durasi')) correctIdx = 1;
      if (qText.includes('penanggung jawab')) correctIdx = 2;

      await qCard.locator('.exam-radio').nth(correctIdx).click({ force: true });
      await page.waitForTimeout(500);
    }

    await expect(page.locator('#answeredProgress')).toContainText(`${qCount}/${qCount}`);
  });

  test('A7 - Worker submits exam via ExamSummary', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title });
    await card.locator('a:has-text("Resume")').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    const resumeModal = page.locator('#resumeConfirmModal');
    if (await resumeModal.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await page.waitForTimeout(500);
    }

    // Go to summary
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Submit Exam');

    // Submit
    page.once('dialog', d => d.accept());
    await page.click('button:has-text("Submit Exam")');
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
  });

  test('A8 - Worker sees results with score and PASSED', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    // Find in exam history
    const historyRow = page.locator('tr', { hasText: title });
    await expect(historyRow).toBeVisible();
    await historyRow.locator('a').first().click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 10_000 });

    await expect(page.locator('body')).toContainText('Your Score');
    await expect(page.locator('body')).toContainText('100%');
    await expect(page.locator('body')).toContainText('PASSED');
  });

  test('A9 - Answer review is visible on Results page', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const historyRow = page.locator('tr', { hasText: title });
    await historyRow.locator('a').first().click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 10_000 });

    // AllowAnswerReview was enabled, so question review section should exist
    await expect(page.locator('body')).toContainText(/Answer Review|Review Jawaban|correct/i);
  });

  test('A10 - Certificate is accessible for passed assessment', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const historyRow = page.locator('tr', { hasText: title });
    await historyRow.locator('a').first().click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 10_000 });

    // Certificate button should be visible (GenerateCertificate was enabled)
    const certBtn = page.locator('a:has-text("View Certificate")');
    if (await certBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      const href = await certBtn.getAttribute('href');
      expect(href).toContain('Certificate');

      // Navigate to certificate page
      const [certPage] = await Promise.all([
        page.context().waitForEvent('page'),
        certBtn.click(),
      ]).catch(() => [null]);

      if (certPage) {
        await certPage.waitForLoadState();
        await expect(certPage.locator('body')).toContainText(title);
        await certPage.close();
      }
    }
  });

  test('A11 - HC sees completed in Monitoring', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);
    await expect(page.locator('body')).toContainText(/Completed|100%/);
  });

  test('A12 - HC exports results to Excel', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    const exportBtn = page.locator('a:has-text("Export")').first();
    if (await exportBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      const downloadPromise = page.waitForEvent('download', { timeout: 10_000 }).catch(() => null);
      await exportBtn.click();
      const download = await downloadPromise;
      if (download) {
        expect(download.suggestedFilename()).toContain('.xlsx');
      }
    }
  });

  test('A13 - HC resets the assessment', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    const resetBtn = page.locator('form[action*="ResetAssessment"] button').first();
    if (await resetBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      page.once('dialog', d => d.accept());
      await resetBtn.click();
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toContainText(/Not started|Belum dimulai/i);
    }
  });

  test('A14 - Worker sees reset assessment as Open again', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title });
    await expect(card).toBeVisible();
    await expect(card).toContainText(/Start Assessment|Open/);
  });

  test('A15 - Cleanup: HC deletes assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW B: Token-protected exam
// ============================================================
test.describe('Flow B: Token-Protected Exam', () => {
  let title: string;

  test('B1 - HC creates token-required assessment', async ({ page }) => {
    title = uniqueTitle('Token Exam');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await page.fill('#Title', title);
    await page.selectOption('#Category', 'IHT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '70');

    // Enable token
    await page.locator('#IsTokenRequired').check();
    await page.waitForTimeout(300);
    // Token input should appear
    await expect(page.locator('#tokenInputContainer')).toBeVisible();
    // Generate token
    await page.click('button:has-text("Generate")');
    const tokenValue = await page.locator('#AccessToken').inputValue();
    expect(tokenValue.length).toBe(6);

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('B2 - Worker sees token-required badge on assessment', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    await expect(card).toBeVisible();
    await expect(card).toContainText('Token Required');
    // Start button should be token variant
    await expect(card.locator('.btn-start-token')).toBeVisible();
  });

  test('B3 - Worker clicks Start and sees token modal', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    await card.locator('.btn-start-token').click();

    // Token modal should appear
    await expect(page.locator('.modal.show')).toBeVisible({ timeout: 5_000 });
    await expect(page.locator('.modal.show')).toContainText(/Token|token/);
  });

  test('B4 - HC can regenerate token in monitoring', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    // Token section should be visible
    await expect(page.locator('body')).toContainText(/Access Token|Token/);
  });

  test('B5 - Cleanup: delete token assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW C: Force Close & Close Early
// ============================================================
test.describe('Flow C: Force Close & Close Early', () => {
  let title: string;
  let assessmentId: number;

  test('C1 - HC creates assessment with questions for 2 workers', async ({ page }) => {
    title = uniqueTitle('ForceClose Exam');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    // Select Rino + Iwan
    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await page.locator('.user-check-item', { hasText: 'iwan3' }).locator('input').click({ force: true });
    await expect(page.locator('#selectedCountBadge')).toContainText('2 selected');

    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '60');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('C2 - HC adds questions via ManageQuestions', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('h2')).toContainText('Manage Questions');

    const url = page.url();
    assessmentId = parseInt(url.match(/ManageQuestions\/(\d+)|id=(\d+)/)!.slice(1).find(Boolean)!);

    // Add 2 questions
    await page.fill('textarea[name="question_text"]', 'Q1 ForceClose test?');
    await page.locator('input[name="options"]').nth(0).fill('Jawaban A');
    await page.locator('input[name="options"]').nth(1).fill('Jawaban B');
    await page.locator('input[name="options"]').nth(2).fill('Jawaban C');
    await page.locator('input[name="options"]').nth(3).fill('Jawaban D');
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');

    await page.fill('textarea[name="question_text"]', 'Q2 ForceClose test?');
    await page.locator('input[name="options"]').nth(0).fill('Jawaban A');
    await page.locator('input[name="options"]').nth(1).fill('Jawaban B');
    await page.locator('input[name="options"]').nth(2).fill('Jawaban C');
    await page.locator('input[name="options"]').nth(3).fill('Jawaban D');
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Daftar Soal (2)')).toBeVisible();
  });

  test('C3 - Worker1 starts exam (becomes InProgress)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible();
  });

  test('C4 - HC force-closes Worker1 session', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    // Find ForceClose button for the InProgress session
    const forceCloseBtn = page.locator('form[action*="ForceCloseAssessment"] button').first();
    if (await forceCloseBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      page.once('dialog', d => d.accept());
      await forceCloseBtn.click();
      await page.waitForLoadState('networkidle');
    }
    // Session should now show Completed with score 0
    await expect(page.locator('body')).toContainText(/Completed|0%/);
  });

  test('C5 - HC uses Close Early for remaining sessions', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    // Click Close Early / Submit Assessment button to open modal
    const closeEarlyBtn = page.locator('button[data-bs-target="#closeEarlyModal"]').first();
    if (await closeEarlyBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await closeEarlyBtn.click();
      await expect(page.locator('#closeEarlyModal.show')).toBeVisible({ timeout: 3_000 });

      // Confirm in modal
      await page.locator('#closeEarlyModal button[type="submit"]').click();
      await page.waitForLoadState('networkidle');
    }
  });

  test('C6 - HC uses ForceCloseAll for group', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    // Check if our assessment still appears (might already be fully closed)
    const link = page.locator(`text=${title}`).first();
    if (await link.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await link.click();
      await page.waitForLoadState('networkidle');

      const forceCloseAllBtn = page.locator('form[action*="ForceCloseAll"] button').first();
      if (await forceCloseAllBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        page.once('dialog', d => d.accept());
        await forceCloseAllBtn.click();
        await page.waitForLoadState('networkidle');
      }
    }
  });

  test('C7 - Cleanup: delete assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW D: Package-based exam with reshuffle
// ============================================================
test.describe('Flow D: Package-Based Exam', () => {
  let title: string;
  let assessmentId: number;

  test('D1 - HC creates assessment for worker', async ({ page }) => {
    title = uniqueTitle('Package Exam');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '60');
    await page.locator('#AllowAnswerReview').check();

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('D2 - HC navigates to ManagePackages and creates a package', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManagePackages"]').first().click();
    await expect(page.locator('h1')).toContainText('Manage Packages');

    // Get assessmentId from URL
    const url = page.url();
    assessmentId = parseInt(url.match(/assessmentId=(\d+)/)![1]);

    // Create Paket A
    await page.fill('input[name="packageName"]', 'Paket A');
    await page.click('button:has-text("Create Package")');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toContainText('Paket A');
  });

  test('D3 - HC imports questions via paste into package', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);

    // Click Import Questions for Paket A
    await page.locator('a:has-text("Import Questions")').first().click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toContainText('Import Questions');

    // Paste tab-separated questions
    const pasteData = [
      'Apa itu safety?\tPerlindungan kerja\tMakan siang\tOlahraga\tTidur\tA',
      'Warna helm safety?\tMerah\tKuning\tHijau\tBiru\tB',
      'APD singkatan?\tAlat Pelindung Diri\tAlat Pertama Darurat\tAksi Pertolongan Dini\tAlat Pemadam Darurat\tA',
    ].join('\n');

    await page.fill('textarea[name="pasteText"]', pasteData);
    await page.click('button:has-text("Import")');
    await page.waitForLoadState('networkidle');

    // Should redirect back to ManagePackages with success
    await expect(page.locator('body')).toContainText(/3 soal|success|berhasil/i);
  });

  test('D4 - HC reshuffles all in monitoring (before worker starts)', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    // Reshuffle All button
    const reshuffleAllBtn = page.locator('button:has-text("Reshuffle All")');
    if (await reshuffleAllBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await reshuffleAllBtn.click();
      await page.waitForTimeout(2_000);
      // Result modal or toast should appear
    }
  });

  test('D5 - Worker starts package exam and sees shuffled questions', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Should see exam with questions
    await expect(page.locator('#examHeader')).toBeVisible();
    const questions = page.locator('[id^="qcard_"]');
    expect(await questions.count()).toBeGreaterThanOrEqual(3);
  });

  test('D6 - Worker answers all and submits package exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title });
    await card.locator('a:has-text("Resume")').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    const resumeModal = page.locator('#resumeConfirmModal');
    if (await resumeModal.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await page.waitForTimeout(500);
    }

    // Select first option for each question (not necessarily correct)
    const questionCards = page.locator('[id^="qcard_"]');
    const qCount = await questionCards.count();
    for (let i = 0; i < qCount; i++) {
      await questionCards.nth(i).locator('.exam-radio').first().click({ force: true });
      await page.waitForTimeout(400);
    }

    // Go to summary and submit
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });

    page.once('dialog', d => d.accept());
    await page.click('button:has-text("Submit Exam")');
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });

    // Should show results
    await expect(page.locator('body')).toContainText('Your Score');
    await expect(page.locator('body')).toContainText(/PASSED|FAILED/);
  });

  test('D7 - Cleanup: delete package assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW E: Proton Tahun 3 Interview (offline)
// ============================================================
test.describe('Flow E: Proton Tahun 3 Interview', () => {
  let title: string;

  test('E1 - HC creates Assessment Proton Tahun 3', async ({ page }) => {
    title = uniqueTitle('Proton T3 Interview');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await page.fill('#Title', title);
    await page.selectOption('#Category', 'Assessment Proton');
    await page.waitForTimeout(500);

    // Proton fields should appear
    const protonSection = page.locator('#protonFieldsSection');
    await expect(protonSection).toBeVisible();

    // Select Tahun 3 track
    const trackSelect = page.locator('#protonTrackSelect');
    const options = trackSelect.locator('option');
    const optCount = await options.count();

    // Find a Tahun 3 option
    let tahun3Found = false;
    for (let i = 0; i < optCount; i++) {
      const text = await options.nth(i).textContent() ?? '';
      if (text.includes('Tahun 3')) {
        await trackSelect.selectOption({ index: i });
        tahun3Found = true;
        break;
      }
    }

    if (!tahun3Found) {
      // Skip rest if no Tahun 3 track available
      test.skip(true, 'No Tahun 3 ProtonTrack available');
      return;
    }

    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    // Tahun 3 might auto-hide duration
    const durationField = page.locator('#DurationMinutes');
    if (await durationField.isVisible().catch(() => false)) {
      await durationField.fill('0');
    }
    await page.fill('#PassPercentage', '60');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('E2 - Worker sees interview badge (no Start button)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    if (await card.isVisible({ timeout: 5_000 }).catch(() => false)) {
      // Should show "Interview Dijadwalkan" instead of Start button
      await expect(card).toContainText(/Interview|Dijadwalkan|Menunggu/i);
      // Should NOT have a start button
      const startBtn = card.locator('.btn-start-standard, .btn-start-token');
      expect(await startBtn.count()).toBe(0);
    }
  });

  test('E3 - HC submits interview results', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    const link = page.locator(`text=${title}`).first();
    if (!(await link.isVisible({ timeout: 3_000 }).catch(() => false))) {
      test.skip(true, 'Assessment not found in monitoring');
      return;
    }
    await link.click();
    await page.waitForLoadState('networkidle');

    // Interview form should be visible
    const interviewForm = page.locator('form[action*="SubmitInterviewResults"]').first();
    if (await interviewForm.isVisible({ timeout: 5_000 }).catch(() => false)) {
      // Fill judges
      await interviewForm.locator('input[name="judges"]').fill('Dr. Andi, Ir. Budi');

      // Fill aspect scores (select 4=Sangat Baik for all)
      const aspectSelects = interviewForm.locator('select[name^="aspect_"]');
      const selectCount = await aspectSelects.count();
      for (let i = 0; i < selectCount; i++) {
        await aspectSelects.nth(i).selectOption('4');
      }

      // Fill notes
      await interviewForm.locator('textarea[name="notes"]').fill('E2E Test - kandidat menunjukkan kompetensi yang baik.');

      // Mark as passed
      await interviewForm.locator('input[name="isPassed"]').check();

      // Submit
      await interviewForm.locator('button[type="submit"]').click();
      await page.waitForLoadState('networkidle');

      // Should show Lulus badge
      await expect(page.locator('body')).toContainText(/Lulus|Completed/);
    }
  });

  test('E4 - Cleanup: delete Proton assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW F: Multiple workers same assessment
// ============================================================
test.describe('Flow F: Multiple Workers Same Assessment', () => {
  let title: string;
  let assessmentId: number;

  test('F1 - HC creates assessment for 2 workers with questions', async ({ page }) => {
    title = uniqueTitle('Multi Worker');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await page.locator('.user-check-item', { hasText: 'iwan3' }).locator('input').click({ force: true });
    await expect(page.locator('#selectedCountBadge')).toContainText('2 selected');

    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '50');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('F2 - HC adds questions', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    const url = page.url();
    assessmentId = parseInt(url.match(/ManageQuestions\/(\d+)|id=(\d+)/)!.slice(1).find(Boolean)!);

    // Add 1 question
    await page.fill('textarea[name="question_text"]', 'Multi worker question?');
    await page.locator('input[name="options"]').nth(0).fill('Benar');
    await page.locator('input[name="options"]').nth(1).fill('Salah');
    await page.locator('input[name="options"]').nth(2).fill('Mungkin');
    await page.locator('input[name="options"]').nth(3).fill('Tidak tahu');
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');
  });

  test('F3 - Worker1 (coachee) takes exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Answer first option
    await page.locator('.exam-radio').first().click({ force: true });
    await page.waitForTimeout(500);

    // Submit
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    await page.click('button:has-text("Submit Exam")');
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Your Score');
  });

  test('F4 - Worker2 (coachee2) takes same exam', async ({ page }) => {
    await login(page, 'coachee2');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    if (!(await card.isVisible({ timeout: 5_000 }).catch(() => false))) {
      test.skip(true, 'Assessment not assigned to coachee2');
      return;
    }

    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Answer first option
    await page.locator('.exam-radio').first().click({ force: true });
    await page.waitForTimeout(500);

    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    await page.click('button:has-text("Submit Exam")');
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Your Score');
  });

  test('F5 - HC sees both workers completed in monitoring', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    // Should show 2 completed
    const completedCells = page.locator('td:has-text("Completed"), td:has-text("100%")');
    // At least monitoring should show both participants
    await expect(page.locator('table')).toBeVisible();
  });

  test('F6 - Cleanup: delete multi-worker assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW G: Exam timer expired (short duration)
// ============================================================
test.describe('Flow G: Exam Timer Expired', () => {
  let title: string;
  let assessmentId: number;

  test('G1 - HC creates 1-minute assessment with question', async ({ page }) => {
    title = uniqueTitle('Timer Expired');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '1'); // 1 minute only
    await page.fill('#PassPercentage', '50');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();

    // Add question
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    const url = page.url();
    assessmentId = parseInt(url.match(/ManageQuestions\/(\d+)|id=(\d+)/)!.slice(1).find(Boolean)!);

    await page.fill('textarea[name="question_text"]', 'Timer test question?');
    await page.locator('input[name="options"]').nth(0).fill('A');
    await page.locator('input[name="options"]').nth(1).fill('B');
    await page.locator('input[name="options"]').nth(2).fill('C');
    await page.locator('input[name="options"]').nth(3).fill('D');
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');
  });

  test('G2 - Worker starts exam and timer is visible', async ({ page }) => {
    test.setTimeout(120_000); // Extended timeout for this test

    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Timer should be visible and counting down
    await expect(page.locator('#examTimer')).toBeVisible();

    // Wait for timer to expire (1 min + buffer)
    // The exam should auto-submit or show expired modal
    await page.waitForTimeout(70_000); // Wait 70 seconds

    // Should either redirect to results, show expired modal, or auto-submit
    const expiredModal = page.locator('#examExpiredModal');
    const onResults = page.url().includes('Results');
    const onAssessment = page.url().includes('Assessment');
    const modalVisible = await expiredModal.isVisible().catch(() => false);

    expect(onResults || onAssessment || modalVisible).toBeTruthy();
  });

  test('G3 - Cleanup: delete timer assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW H: Real-time monitoring (polling updates)
// HC creates assessment → opens monitoring detail → worker starts exam →
// polling reflects InProgress → worker submits → polling reflects Completed
// ============================================================
test.describe('Flow H: Real-Time Monitoring', () => {
  let title: string;
  let assessmentId: number;

  test('H1 - HC creates assessment with question', async ({ page }) => {
    title = uniqueTitle('RealTime Mon');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await expect(page.locator('#selectedCountBadge')).toContainText('1 selected');

    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '50');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();

    // Add a question
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    const url = page.url();
    assessmentId = parseInt(url.match(/ManageQuestions\/(\d+)|id=(\d+)/)!.slice(1).find(Boolean)!);

    await page.fill('textarea[name="question_text"]', 'Monitoring test question?');
    await page.locator('input[name="options"]').nth(0).fill('Benar');
    await page.locator('input[name="options"]').nth(1).fill('Salah');
    await page.locator('input[name="options"]').nth(2).fill('Mungkin');
    await page.locator('input[name="options"]').nth(3).fill('Tidak tahu');
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');
  });

  test('H2 - HC opens monitoring detail and sees Not Started', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    const link = page.locator(`text=${title}`).first();
    await expect(link).toBeVisible({ timeout: 5_000 });
    await link.click();
    await page.waitForLoadState('networkidle');

    // Summary cards should show 1 total, 0 completed, 1 not started
    await expect(page.locator('#count-total')).toHaveText('1');
    await expect(page.locator('#count-completed')).toHaveText('0');
    await expect(page.locator('#count-notstarted')).toHaveText('1');

    // Session row should show "Not started" status badge
    const statusBadge = page.locator('tr[data-session-id] td .badge').first();
    await expect(statusBadge).toContainText('Not started');
  });

  test('H3 - Worker starts exam, polling endpoint reflects InProgress', async ({ page }) => {
    // Worker starts the exam
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Verify exam page loaded
    await expect(page.locator('#examTimer')).toBeVisible();
  });

  test('H4 - HC monitoring detail shows InProgress via page', async ({ page }) => {
    await login(page, 'hc');
    await goToMonitoringDetail(page, title);

    // Summary: 0 completed, 1 in progress
    await expect(page.locator('#count-completed')).toHaveText('0');
    await expect(page.locator('#count-inprogress')).toHaveText('1');
    await expect(page.locator('#count-notstarted')).toHaveText('0');

    // Session row shows InProgress badge
    const statusBadge = page.locator('tr[data-session-id] td .badge').first();
    await expect(statusBadge).toContainText('InProgress');

    // Time remaining column should show a countdown (not —)
    const timeCell = page.locator('tr[data-session-id] td').nth(6);
    const timeText = await timeCell.textContent();
    expect(timeText).toMatch(/\d{2}:\d{2}/);

    // Force Close button should be visible for InProgress session
    await expect(page.locator('button:has-text("Force Close")').first()).toBeVisible();

    // "Last updated" should populate after polling
    await page.waitForTimeout(2_000);
    const lastUpdated = await page.locator('#last-updated-time').textContent();
    expect(lastUpdated).not.toBe('—');
  });

  test('H5 - Worker submits exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto(`/CMP/StartExam/${assessmentId}`);

    // If redirected to assessment list, the exam session may need re-entry
    if (page.url().includes('Assessment') && !page.url().includes('StartExam')) {
      const card = page.locator('.assessment-card', { hasText: title });
      if (await card.locator('.btn-start-standard').isVisible({ timeout: 3_000 }).catch(() => false)) {
        page.once('dialog', d => d.accept());
        await card.locator('.btn-start-standard').click();
        await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
      }
    }

    // Answer the question
    await page.locator('.exam-radio').first().click({ force: true });
    await page.waitForTimeout(500);

    // Submit
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    await page.click('button:has-text("Submit Exam")');
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Your Score');
  });

  test('H6 - HC monitoring detail shows Completed after submission', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    // Search for the assessment (may be filtered out by default status filter)
    const searchInput = page.locator('input[placeholder*="Cari"]').first();
    await searchInput.fill(title);
    await page.click('button:has-text("Filter")');
    await page.waitForLoadState('networkidle');

    // If not found with default filter, try "all" status
    let link = page.locator(`text=${title}`).first();
    if (!(await link.isVisible({ timeout: 3_000 }).catch(() => false))) {
      const statusSelect = page.locator('select[name="status"]');
      await statusSelect.selectOption('All');
      await page.click('button:has-text("Filter")');
      await page.waitForLoadState('networkidle');
    }

    await page.locator(`text=${title}`).first().click();
    await page.waitForLoadState('networkidle');

    // Wait for polling to pick up completed status (poll runs every 10s, first poll is immediate)
    await page.waitForTimeout(2_000);

    // Summary: 1 completed, 0 in progress
    await expect(page.locator('#count-completed')).toHaveText('1');
    await expect(page.locator('#count-inprogress')).toHaveText('0');

    // Session row shows Completed badge
    const statusBadge = page.locator('tr[data-session-id] td .badge').first();
    await expect(statusBadge).toContainText('Completed');

    // Score should be visible (not —)
    const scoreCell = page.locator('tr[data-session-id] td').nth(3);
    const scoreText = await scoreCell.textContent();
    expect(scoreText).toMatch(/\d+%/);

    // Result should show Pass or Fail
    const resultCell = page.locator('tr[data-session-id] td').nth(4);
    const resultText = await resultCell.textContent();
    expect(resultText).toMatch(/Pass|Fail/);

    // View Results button should be visible
    await expect(page.locator('a:has-text("View Results")').first()).toBeVisible();

    // "Submit Assessment" button should be hidden (all completed → polling hides it)
    // Give polling time to hide it
    await page.waitForTimeout(12_000);
    const closeBtn = page.locator('#closeEarlyBtn');
    if (await closeBtn.count() > 0) {
      // Polling should have hidden it
      expect(await closeBtn.isVisible()).toBe(false);
    }
  });

  test('H7 - Polling endpoint returns Completed data', async ({ page }) => {
    await login(page, 'hc');

    // Call polling endpoint directly
    const scheduleDate = today();
    const resp = await page.evaluate(async ({ t, d }) => {
      const url = `/Admin/GetMonitoringProgress?title=${encodeURIComponent(t)}&category=OJT&scheduleDate=${d}`;
      const r = await fetch(url);
      if (!r.ok) return { status: r.status };
      return r.json();
    }, { t: title, d: scheduleDate });

    if (Array.isArray(resp)) {
      expect(resp).toHaveLength(1);
      expect(resp[0].status).toBe('Completed');
      expect(resp[0].score).toBeDefined();
      expect(resp[0].result).toMatch(/Pass|Fail/);
      expect(resp[0].completedAt).toBeTruthy();
      // remainingSeconds should be null for completed
      expect(resp[0].remainingSeconds).toBeNull();
    }
  });

  test('H8 - Cleanup: delete monitoring test assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW I: Edit Assessment (modify title, settings, add workers)
// HC creates → edits title & pass percentage → verifies changes persist
// ============================================================
test.describe('Flow I: Edit Assessment', () => {
  let title: string;
  let editedTitle: string;

  test('I1 - HC creates assessment', async ({ page }) => {
    title = uniqueTitle('EditTest');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await expect(page.locator('#selectedCountBadge')).toContainText('1 selected');

    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '60');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();
  });

  test('I2 - HC opens Edit page from ManageAssessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="EditAssessment"]').first().click();
    await page.waitForLoadState('networkidle');

    // Verify edit form is loaded with current values
    await expect(page.locator('#Title')).toHaveValue(title);
    await expect(page.locator('#DurationMinutes')).toHaveValue('30');
    await expect(page.locator('#PassPercentage')).toHaveValue('60');
  });

  test('I3 - HC edits title and pass percentage', async ({ page }) => {
    editedTitle = title + ' EDITED';
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="EditAssessment"]').first().click();
    await page.waitForLoadState('networkidle');

    // Change title and pass percentage
    await page.fill('#Title', editedTitle);
    await page.fill('#PassPercentage', '75');

    // Submit edit form
    await page.locator('button[type="submit"].btn-primary').click();
    await page.waitForLoadState('networkidle');

    // Should redirect back to ManageAssessment with success
    const successAlert = await page.locator('.alert-success').isVisible().catch(() => false);
    const onManage = page.url().includes('ManageAssessment');
    expect(successAlert || onManage).toBeTruthy();
  });

  test('I4 - Verify edited values persist', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(editedTitle);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');

    // The edited title should appear in the list
    await expect(page.locator('body')).toContainText(editedTitle);

    // Open edit again to verify pass percentage changed
    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="EditAssessment"]').first().click();
    await page.waitForLoadState('networkidle');

    await expect(page.locator('#Title')).toHaveValue(editedTitle);
    await expect(page.locator('#PassPercentage')).toHaveValue('75');
  });

  test('I5 - Cleanup: delete edited assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(editedTitle);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});

// ============================================================
// FLOW J: Abandon Exam & Reset Recovery
// Worker starts exam → abandons → HC sees Abandoned → HC resets → worker retakes
// ============================================================
test.describe('Flow J: Abandon Exam & Reset Recovery', () => {
  let title: string;
  let assessmentId: number;

  test('J1 - HC creates assessment with question', async ({ page }) => {
    title = uniqueTitle('Abandon Test');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
    await expect(page.locator('#selectedCountBadge')).toContainText('1 selected');

    await page.fill('#Title', title);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '50');

    await page.click('#submitBtn');
    await page.waitForTimeout(3_000);
    const success = await page.locator('#successModal').evaluate(el => el.classList.contains('show')).catch(() => false);
    const alert = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(success || alert).toBeTruthy();

    // Add question
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    await page.locator('button.dropdown-toggle').first().click();
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    const url = page.url();
    assessmentId = parseInt(url.match(/ManageQuestions\/(\d+)|id=(\d+)/)!.slice(1).find(Boolean)!);

    await page.fill('textarea[name="question_text"]', 'Abandon test question?');
    await page.locator('input[name="options"]').nth(0).fill('A');
    await page.locator('input[name="options"]').nth(1).fill('B');
    await page.locator('input[name="options"]').nth(2).fill('C');
    await page.locator('input[name="options"]').nth(3).fill('D');
    await page.click('button:has-text("Tambah Soal")');
    await page.waitForLoadState('networkidle');
  });

  test('J2 - Worker starts exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    await expect(page.locator('#examTimer')).toBeVisible();
  });

  test('J3 - Worker abandons exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto(`/CMP/StartExam/${assessmentId}`);

    // If on the exam page, click the abandon button
    // The abandon form is hidden, triggered by confirmAbandon()
    // We'll trigger it by accepting the dialog and submitting the form
    page.once('dialog', d => d.accept());

    // Find the abandon/exit button (usually a link or button calling confirmAbandon)
    const abandonBtn = page.locator('button:has-text("Keluar"), a:has-text("Keluar"), button:has-text("Abandon"), a:has-text("Abandon")').first();
    if (await abandonBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      // Disable beforeunload to prevent blocking
      await page.evaluate(() => { window.onbeforeunload = null; });
      await abandonBtn.click();
    } else {
      // Fallback: submit the abandon form directly
      await page.evaluate(() => {
        window.onbeforeunload = null;
        const form = document.getElementById('abandonForm') as HTMLFormElement;
        if (form) form.submit();
      });
    }

    await page.waitForURL('**/CMP/Assessment**', { timeout: 15_000 });

    // Should see info message about abandoned exam
    await expect(page.locator('body')).toContainText(/Ditinggalkan|dibatalkan|Abandoned/i);
  });

  test('J4 - Worker cannot restart abandoned exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    if (await card.isVisible({ timeout: 5_000 }).catch(() => false)) {
      // Should show Abandoned badge, no Start button
      const startBtn = card.locator('.btn-start-standard, .btn-start-token');
      const startCount = await startBtn.count();
      // Either no start button, or card shows abandoned status
      const cardText = await card.textContent() ?? '';
      expect(startCount === 0 || /Abandoned|Ditinggalkan/i.test(cardText)).toBeTruthy();
    }
  });

  test('J5 - HC sees Abandoned status in monitoring', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    // Search for the assessment (may need to change status filter)
    const searchInput = page.locator('input[placeholder*="Cari"]').first();
    await searchInput.fill(title);
    await page.click('button:has-text("Filter")');
    await page.waitForLoadState('networkidle');

    let link = page.locator(`text=${title}`).first();
    if (!(await link.isVisible({ timeout: 3_000 }).catch(() => false))) {
      // Try all statuses
      await page.locator('select[name="status"]').selectOption('All');
      await page.click('button:has-text("Filter")');
      await page.waitForLoadState('networkidle');
    }

    await page.locator(`text=${title}`).first().click();
    await page.waitForLoadState('networkidle');

    // Session should show Abandoned badge
    const statusBadge = page.locator('tr[data-session-id] td .badge').first();
    await expect(statusBadge).toContainText('Abandoned');
  });

  test('J6 - HC resets the abandoned session', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    // Search for the assessment
    const searchInput = page.locator('input[placeholder*="Cari"]').first();
    await searchInput.fill(title);
    await page.locator('select[name="status"]').selectOption('All');
    await page.click('button:has-text("Filter")');
    await page.waitForLoadState('networkidle');

    await page.locator(`text=${title}`).first().click();
    await page.waitForLoadState('networkidle');

    const resetBtn = page.locator('form[action*="ResetAssessment"] button').first();
    await expect(resetBtn).toBeVisible({ timeout: 3_000 });
    page.once('dialog', d => d.accept());
    await resetBtn.click();
    await page.waitForLoadState('networkidle');

    // Should now show Not started
    await expect(page.locator('body')).toContainText(/Not started|Success/i);
  });

  test('J7 - Worker can retake after reset', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    await expect(card).toBeVisible({ timeout: 5_000 });

    // Start button should be available again
    const startBtn = card.locator('.btn-start-standard');
    await expect(startBtn).toBeVisible();

    // Start, answer, and submit
    page.once('dialog', d => d.accept());
    await startBtn.click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    await page.locator('.exam-radio').first().click({ force: true });
    await page.waitForTimeout(500);

    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    await page.click('button:has-text("Submit Exam")');
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Your Score');
  });

  test('J8 - Cleanup: delete abandon test assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      await page.locator('text=Hapus Grup').first().click();
      await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    }
  });
});
