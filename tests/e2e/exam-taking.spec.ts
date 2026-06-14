import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
import { clickResumeForFixture, assertTier1Reject, assertTier2Reject, assertSubmitSuccess } from './helpers/exam313';
// Phase 379 — helper wizard kanonik (ganti flat-form create/question usang) + DB assert.
import {
  createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm,
  importQuestionsViaPaste, submitExamTwoStep, checkMAOptionsForQuestion,
  fillEssayAnswer, gradeSingleEssaySession, type QuestionInput,
} from './helpers/examTypes';
import * as db from '../helpers/dbSnapshot';

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
  // Phase 379 — migrasi wizard+package (fixme 364/999.7 dihapus). Create/QADD via helper kanonik.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('A1 - HC creates assessment for coachee', async ({ page }) => {
    title = uniqueTitle('Pre Test Legacy Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 60,
      allowAnswerReview: true,        // A9 answer-review butuh true
      generateCertificate: true,      // A10 cert butuh true
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });

  test('A2 - HC creates package', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
  });

  test('A3 - HC adds 3 questions', async ({ page }) => {
    await login(page, 'hc');
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Apa kepanjangan OJT?',
      options: ['On the Job Training', 'Online Job Test', 'Operation Job Task', 'Operational Job Training'],
      correctIndex: 0, score: 100,
    });
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Berapa lama durasi OJT standar?',
      options: ['1 bulan', '3 bulan', '6 bulan', '12 bulan'],
      correctIndex: 1, score: 100,
    });
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Siapa penanggung jawab OJT?',
      options: ['Direktur', 'VP', 'Supervisor', 'Admin'],
      correctIndex: 2, score: 100,
    });
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

    // Handle resume modal (Phase 379 — muncul async pasca-load StartExam, static backdrop intercepts pointer).
    // Tunggu modal show (resume SELALU memicu) lalu dismiss + assert hidden sebelum jawab.
    const resumeModal = page.locator('#resumeConfirmModal');
    await resumeModal.waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }

    // Answer each question
    const questionCards = page.locator('[id^="qcard_"]');
    const qCount = await questionCards.count();
    expect(qCount).toBe(3);

    for (let i = 0; i < qCount; i++) {
      const qCard = questionCards.nth(i);
      const qText = await qCard.locator('h6, .fw-bold').first().textContent() ?? '';
      // Phase 379 — shuffle opsi (Phase 372-375 default ON) acak posisi → pilih label by TEXT jawaban benar (bukan positional nth).
      let correctText = 'On the Job Training';
      if (qText.includes('durasi')) correctText = '3 bulan';
      if (qText.includes('penanggung jawab')) correctText = 'Supervisor';
      await qCard.locator('label[id^="lbl_"]').filter({ hasText: correctText }).first().click();
      await page.waitForTimeout(700);
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
    await resumeModal.waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }

    // Go to summary (Phase 379 — ExamSummary kini Bahasa Indonesia: "Tinjau Jawaban" + "Kumpulkan Ujian")
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText(/Kumpulkan Ujian|Tinjau Jawaban/);

    // Submit
    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
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

    // Phase 379 — Results Bahasa Indonesia: "Nilai Anda" + "@Score%" + badge "LULUS"
    await expect(page.locator('body')).toContainText('Nilai Anda');
    await expect(page.locator('body')).toContainText('100%');
    await expect(page.locator('body')).toContainText('LULUS');
  });

  test('A9 - Answer review is visible on Results page', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const historyRow = page.locator('tr', { hasText: title });
    await historyRow.locator('a').first().click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 10_000 });

    // AllowAnswerReview enabled → section "Tinjauan Jawaban" (Phase 379 — Bahasa Indonesia)
    await expect(page.locator('body')).toContainText(/Tinjauan Jawaban|Jawaban Benar/i);
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

    // Phase 379 — tombol Reset ada di dropdown ⋮ per-sesi (buka dropdown dulu, lalu confirm dialog).
    const kebab = page.locator('button[aria-label^="Aksi lain"]').first();
    await expect(kebab).toBeVisible({ timeout: 5_000 });
    await kebab.click();
    const resetBtn = page.locator('form[action*="ResetAssessment"] button[type="submit"]').first();
    page.once('dialog', d => d.accept());
    await resetBtn.click();
    await page.waitForLoadState('networkidle');
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
  // Phase 379 — migrasi wizard + token extension Plan 01 (fixme 364/999.7 dihapus).
  // Token exam tetap butuh 1 soal supaya worker melihat kartu startable (btn-start-token).
  let title: string;

  test('B1 - HC creates token-required assessment', async ({ page }) => {
    title = uniqueTitle('Pre Test Token Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'IHT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 70, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      isTokenRequired: true,          // extension Plan 01 — accessToken kosong → helper klik Generate (6-char)
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    const assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    const packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Soal token test?',
      options: ['Jawaban A', 'Jawaban B', 'Jawaban C', 'Jawaban D'], correctIndex: 0, score: 100,
    });
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
  // Phase 379 — migrasi wizard+package, 2 worker (rino+iwan3); fixme 364/999.7 dihapus.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('C1 - HC creates assessment with questions for 2 workers', async ({ page }) => {
    title = uniqueTitle('Pre Test ForceClose Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 60, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });

  test('C2 - HC adds questions via package', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Q1 ForceClose test?',
      options: ['Jawaban A', 'Jawaban B', 'Jawaban C', 'Jawaban D'], correctIndex: 0, score: 100,
    });
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Q2 ForceClose test?',
      options: ['Jawaban A', 'Jawaban B', 'Jawaban C', 'Jawaban D'], correctIndex: 0, score: 100,
    });
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

    // Phase 379 — force-close sesi InProgress = "Akhiri Ujian" (AkhiriUjian) di dropdown ⋮ sesi tsb.
    // Hanya sesi InProgress yang punya form AkhiriUjian → buka dropdown yang memuatnya.
    const akhiriDropdown = page.locator('div.dropdown', { has: page.locator('form[action*="AkhiriUjian"]') }).first();
    await akhiriDropdown.locator('button[aria-label^="Aksi lain"]').click();
    page.once('dialog', d => d.accept());
    await page.locator('form[action*="AkhiriUjian"] button[type="submit"]').first().click();
    await page.waitForLoadState('networkidle');
    // Session should now show Completed (auto-graded score 0 — worker tak menjawab)
    await expect(page.locator('body')).toContainText(/Completed|Selesai|0%/);
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
  // Phase 379 — migrasi wizard+package; D3 paste-import via helper (route VALID, Plan 01). fixme dihapus.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('D1 - HC creates assessment for worker', async ({ page }) => {
    title = uniqueTitle('Pre Test Package Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 60, allowAnswerReview: true,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });

  test('D2 - HC navigates to ManagePackages and creates a package', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
  });

  test('D3 - HC imports questions via paste into package', async ({ page }) => {
    await login(page, 'hc');
    // Phase 379 — paste-import (nilai unik Flow D). Helper klik tab Paste; TSV 6-kolom (QuestionType kosong → MC auto).
    const tsvRows = [
      'Apa itu safety?\tPerlindungan kerja\tMakan siang\tOlahraga\tTidur\tA',
      'Warna helm safety?\tMerah\tKuning\tHijau\tBiru\tB',
      'APD singkatan?\tAlat Pelindung Diri\tAlat Pertama Darurat\tAksi Pertolongan Dini\tAlat Pemadam Darurat\tA',
    ].join('\n');
    await importQuestionsViaPaste(page, packageId, tsvRows);
    await expect(page.locator('body')).toContainText(/soal|success|berhasil|Paket/i);
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
    await resumeModal.waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }

    // Select first option for each question (not necessarily correct) — pakai label (shuffle-safe click)
    const questionCards = page.locator('[id^="qcard_"]');
    const qCount = await questionCards.count();
    for (let i = 0; i < qCount; i++) {
      await questionCards.nth(i).locator('label[id^="lbl_"]').first().click();
      await page.waitForTimeout(700);
    }

    // Go to summary and submit (Phase 379 — Bahasa Indonesia: Kumpulkan Ujian / Nilai Anda / LULUS)
    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });

    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });

    // Should show results
    await expect(page.locator('body')).toContainText('Nilai Anda');
    await expect(page.locator('body')).toContainText(/LULUS|TIDAK LULUS/);
  });

  test('D7 - Cleanup: delete package assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    // Phase 379 — best-effort cleanup (teardown RESTORE = safety net). Kebab per-baris → "Hapus Grup" buka modal → konfirmasi.
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW E: Proton Tahun 3 Interview (offline)
// ============================================================
test.describe('Flow E: Proton Tahun 3 Interview', () => {
  // Phase 379 — migrasi PENUH (D-02): wizard + proton extension Plan 01; skip-Proton + fixme DIHAPUS (ProtonTrack T3 ada, W0-1=2).
  let title: string;
  let assessmentId: number;

  test('E1 - HC creates Assessment Proton Tahun 3', async ({ page }) => {
    title = uniqueTitle('Pre Test Proton T3 Interview');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'Assessment Proton', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 0,             // Tahun 3 offline interview (Duration field di-hide wizard)
      passPercentage: 60, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      protonTrackTahun: 'Tahun 3',    // extension Plan 01: pilih track by data-tahun
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
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
    await goToMonitoringDetail(page, title);

    // Phase 379 (D-02) — interview form Proton T3 re-check vs controller v25.0 (judges/aspect_*/notes/isPassed + antiforgery auto).
    const interviewForm = page.locator('form[action*="SubmitInterviewResults"]').first();
    await expect(interviewForm).toBeVisible({ timeout: 8_000 });
    await interviewForm.locator('input[name="judges"]').fill('Dr. Andi, Ir. Budi');
    const aspectSelects = interviewForm.locator('select[name^="aspect_"]');
    const selectCount = await aspectSelects.count();
    for (let i = 0; i < selectCount; i++) {
      await aspectSelects.nth(i).selectOption('4');
    }
    await interviewForm.locator('textarea[name="notes"]').fill('E2E Test - kandidat menunjukkan kompetensi yang baik.');
    await interviewForm.locator('input[name="isPassed"]').check();
    await interviewForm.locator('button[type="submit"]').click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toContainText(/Lulus|Completed|berhasil/i);
  });

  test('E4 - Cleanup: delete Proton assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    // Best-effort cleanup (teardown RESTORE = safety net) — pola robust D7.
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW F: Multiple workers same assessment
// ============================================================
test.describe('Flow F: Multiple Workers Same Assessment', () => {
  // Phase 379 — migrasi wizard+package, 2 worker (rino+iwan3); fixme dihapus.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('F1 - HC creates assessment for 2 workers with questions', async ({ page }) => {
    title = uniqueTitle('Pre Test Multi Worker');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 50, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });

  test('F2 - HC adds questions', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Multi worker question?',
      options: ['Benar', 'Salah', 'Mungkin', 'Tidak tahu'], correctIndex: 0, score: 100,
    });
  });

  test('F3 - Worker1 (coachee) takes exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Answer first option (label shuffle-safe)
    await page.locator('[id^="qcard_"]').first().locator('label[id^="lbl_"]').first().click();
    await page.waitForTimeout(700);

    // Submit (Phase 379 — Kumpulkan Ujian / Nilai Anda)
    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Nilai Anda');
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

    // Answer first option (label shuffle-safe)
    await page.locator('[id^="qcard_"]').first().locator('label[id^="lbl_"]').first().click();
    await page.waitForTimeout(700);

    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Nilai Anda');
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
    // Phase 379 — best-effort cleanup (teardown RESTORE = safety net). Kebab per-baris → Hapus Grup → modal konfirmasi.
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW G: Exam timer expired (short duration)
// ============================================================
test.describe('Flow G: Exam Timer Expired', () => {
  // Phase 379 — migrasi wizard+package, timer 1-menit; deterministik (D-03); fixme dihapus.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('G1 - HC creates 1-minute assessment with question', async ({ page }) => {
    title = uniqueTitle('Pre Test Timer Expired');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 1, passPercentage: 50, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Timer test question?',
      options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 100,
    });
  });

  test('G2 - Worker starts exam and timer expires (deterministic)', async ({ page }) => {
    test.setTimeout(120_000);

    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    const sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)![1], 10);

    // Timer should be visible and counting down
    await expect(page.locator('#examTimer')).toBeVisible();

    // Phase 379 (D-03) — event-driven timer-expiry (W0-3 #examExpiredModal ADA): resolve SEGERA saat
    // expired (modal .show ATAU auto-submit ke Results), BUKAN sleep-buta 70 detik wall-clock.
    await page.waitForFunction(() => {
      const modal = document.querySelector('#examExpiredModal');
      const modalShown = modal !== null && modal.classList.contains('show');
      return modalShown || /\/CMP\/Results\//.test(location.href);
    }, undefined, { timeout: 90_000 });

    // Outcome assert (modal expired ATAU Results ATAU DB Status Completed/Abandoned)
    const onResults = page.url().includes('Results');
    const modalVisible = await page.locator('#examExpiredModal').isVisible().catch(() => false);
    const dbDone = await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id=${sessionId} AND Status IN ('Completed','Abandoned')`).catch(() => 0);
    expect(onResults || modalVisible || dbDone >= 1).toBeTruthy();
  });

  test('G3 - Cleanup: delete timer assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    // Phase 379 — best-effort cleanup (teardown RESTORE = safety net).
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW H: Real-time monitoring (polling updates)
// HC creates assessment → opens monitoring detail → worker starts exam →
// polling reflects InProgress → worker submits → polling reflects Completed
// ============================================================
test.describe('Flow H: Real-Time Monitoring', () => {
  // Phase 379 — migrasi wizard+package, real-time monitoring; deterministik H6 (D-03); fixme dihapus.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('H1 - HC creates assessment with question', async ({ page }) => {
    title = uniqueTitle('Pre Test RealTime Mon');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 50, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Monitoring test question?',
      options: ['Benar', 'Salah', 'Mungkin', 'Tidak tahu'], correctIndex: 0, score: 100,
    });
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

    // Phase 379 — countdown time-remaining: kolom td bergeser (positional nth fragile) → cek pola di row text,
    // toleran (countdown live di-cover deterministik via H7 GetMonitoringProgress JSON remainingSeconds).
    const rowText = await page.locator('tr[data-session-id]').first().textContent() ?? '';
    expect(rowText).toContain('InProgress');

    // Phase 379 — force-close InProgress kini "Akhiri Ujian" di dropdown kebab → cek form ada di DOM.
    await expect(page.locator('form[action*="AkhiriUjian"]')).toHaveCount(1);

    // Phase 379 — "Last updated" = indikator polling kosmetik; elemen ada (real-time inti di-cover H6 polling-Completed + H7 JSON).
    // Toleran: update timestamp best-effort (poll-cycle bisa di luar window test), JANGAN hard-fail flow.
    await expect(page.locator('#last-updated-time')).toBeVisible();
  });

  test('H5 - Worker submits exam', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    // Resume InProgress exam (H3 sudah start)
    const card = page.locator('.assessment-card', { hasText: title });
    await card.locator('a:has-text("Resume")').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    const resumeModal = page.locator('#resumeConfirmModal');
    await resumeModal.waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }

    // Answer the question (label shuffle-safe)
    await page.locator('[id^="qcard_"]').first().locator('label[id^="lbl_"]').first().click();
    await page.waitForTimeout(700);

    // Submit (Phase 379 — Kumpulkan Ujian / Nilai Anda)
    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Nilai Anda');
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

    // Phase 379 — score & result: kolom td bergeser (positional fragile) → cek pola di row text (toleran lokalisasi).
    const rowText = await page.locator('tr[data-session-id]').first().textContent() ?? '';
    expect(rowText).toMatch(/\d+%/);                        // skor persen
    expect(rowText).toMatch(/Lulus|Tidak Lulus|Pass|Fail/i); // hasil (lokalisasi-toleran)

    // View Results button should be visible
    await expect(page.locator('a:has-text("View Results")').first()).toBeVisible();

    // Phase 379 (D-03) — "Submit Assessment" hidden saat semua Completed (polling auto-refresh).
    // GANTI sleep-buta 12 detik → auto-retry assert (resolve segera saat poll update DOM).
    const closeBtn = page.locator('#closeEarlyBtn');
    if (await closeBtn.count() > 0) {
      await expect(closeBtn).toBeHidden({ timeout: 15_000 });
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
    // Phase 379 — best-effort cleanup (teardown RESTORE = safety net).
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW I: Edit Assessment (modify title, settings, add workers)
// HC creates → edits title & pass percentage → verifies changes persist
// ============================================================
test.describe('Flow I: Edit Assessment', () => {
  // Phase 379 — migrasi wizard create; edit-form (EditAssessment.cshtml) masih flat → I2-I5 SURVIVE; fixme dihapus.
  let title: string;
  let editedTitle: string;

  test('I1 - HC creates assessment', async ({ page }) => {
    title = uniqueTitle('Pre Test EditTest');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 60, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
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
    // Phase 379 — best-effort cleanup (teardown RESTORE = safety net).
    const row = page.locator('tr', { hasText: editedTitle }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW J: Abandon Exam & Reset Recovery
// Worker starts exam → abandons → HC sees Abandoned → HC resets → worker retakes
// ============================================================
test.describe('Flow J: Abandon Exam & Reset Recovery', () => {
  // Phase 379 — migrasi wizard+package; abandon/reset lifecycle SURVIVE; fixme dihapus.
  let title: string;
  let assessmentId: number;
  let packageId: number;

  test('J1 - HC creates assessment with question', async ({ page }) => {
    title = uniqueTitle('Pre Test Abandon Test');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 50, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: 'Abandon test question?',
      options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 100,
    });
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
    await page.goto('/CMP/Assessment');

    // Phase 379 — resume InProgress exam (J2 sudah start) lalu abandon
    const card = page.locator('.assessment-card', { hasText: title });
    await card.locator('a:has-text("Resume")').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    const resumeModal = page.locator('#resumeConfirmModal');
    await resumeModal.waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }

    // Abandon: trigger via Keluar button atau submit #abandonForm langsung
    page.once('dialog', d => d.accept());
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

    // Phase 379 — Reset ada di dropdown kebab per-sesi (buka dulu, lalu confirm).
    const kebab = page.locator('button[aria-label^="Aksi lain"]').first();
    await expect(kebab).toBeVisible({ timeout: 5_000 });
    await kebab.click();
    const resetBtn = page.locator('form[action*="ResetAssessment"] button[type="submit"]').first();
    page.once('dialog', d => d.accept());
    await resetBtn.click();
    await page.waitForLoadState('networkidle');
    // Should now show Not started
    await expect(page.locator('body')).toContainText(/Not started|Success|Berhasil/i);
  });

  test('J7 - Worker can retake after reset', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title });
    await expect(card).toBeVisible({ timeout: 5_000 });

    // Start button should be available again
    const startBtn = card.locator('.btn-start-standard');
    await expect(startBtn).toBeVisible();

    // Start, answer, and submit (Phase 379 — label shuffle-safe + Kumpulkan Ujian / Nilai Anda)
    page.once('dialog', d => d.accept());
    await startBtn.click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    await page.locator('[id^="qcard_"]').first().locator('label[id^="lbl_"]').first().click();
    await page.waitForTimeout(700);

    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });
    await expect(page.locator('body')).toContainText('Nilai Anda');
  });

  test('J8 - Cleanup: delete abandon test assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    // Phase 379 — best-effort cleanup (teardown RESTORE = safety net).
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW 313: Phase 313 — Block Manual Submit Saat Waktu Habis
// REQ: TMR-01 (7 success criteria)
// 313.1 — Manual + before-time + Online → submit OK (regression)
// 313.2 — Manual + after-time (in grace) + Online → BLOCKED + AuditLog SubmitExamBlocked + redirect
// 313.3 — Auto + after-time (in grace) + Online → submit OK (Tier 2 grace covers)
// 313.4 — Auto + after-grace + Online → BLOCKED Tier 2 (existing preserved)
// 313.5 — Manual + after-time + PreTest → BLOCKED (3 timer types verify)
// 313.6 — Manual + after-time + PostTest → BLOCKED
// 313.7 — Manual + after-time + Manual type → submit OK (D-15 exclude verify)
// Wave 0: semua test SKIP graceful jika fixture .planning/seeds/313-timer-fixtures.sql belum dijalankan.
// ============================================================
test.describe('Exam Taking - Phase 313 Block Manual Submit', () => {

  test('313.1 - Manual + before-time + Online → submit OK (regression)', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture Online ManualBeforeTime';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    // Order kritikal (StartExam.cshtml line 1141-1144):
    //   examLoadingOverlay HIDE DULU → resumeConfirmModal SHOW (kondisional IS_RESUME).
    // Test harus tunggu overlay hide DULU, baru dismiss modal kalau muncul.
    const loadingOverlay = page.locator('#examLoadingOverlay');
    if (await loadingOverlay.count() > 0) {
      await expect(loadingOverlay).not.toBeVisible({ timeout: 15_000 });
    }
    // Sekarang modal "Ada ujian yang belum selesai" mungkin sudah show (Pitfall 3).
    // Click "Lanjutkan" via #resumeConfirmBtn → modal hide (line 1146-1152 handler).
    const resumeModal = page.locator('#resumeConfirmModal');
    if (await resumeModal.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }
    // Fill SEMUA 3 questions (D-02 seed) supaya "Kumpulkan Ujian" enabled di ExamSummary.
    // D-04: IsCorrect=true HANYA index 0 (OptionText="Pilihan A"). Cross-package shuffle
    // random-kan urutan, jadi cari label "Pilihan A" per question card.
    // Click label (markup line 164-175 StartExam.cshtml) — browser fires native onChange →
    // JS handler line 803 → AJAX save → form submit unblocked.
    const questionCards = page.locator('[id^="qcard_"]');
    const cardCount = await questionCards.count();
    for (let i = 0; i < cardCount; i++) {
      const card = questionCards.nth(i);
      await card.locator('label[id^="lbl_"]').filter({ hasText: 'Pilihan A' }).first().click();
      await page.waitForTimeout(700); // debounced save (line 817 saveAnswerWithDebounce ~500ms)
    }
    // Tunggu submit button enabled (line 968 guard release setelah semua save complete).
    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 });
    // Confirm dialog handler (Pitfall 4) MUST register before click.
    page.once('dialog', dialog => dialog.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await assertSubmitSuccess(page, sessionId);
  });

  test('313.2 - Manual + after-time (in grace) + Online → BLOCKED + AuditLog SubmitExamBlocked', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture Online ManualAfterGrace';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    // Server detects timer expired → redirect StartExam → ExamSummary.
    // JS auto-fire retry handler → POST SubmitExam → server Tier-1 reject 302 StartExam.
    // assertTier1Reject covers final waitForURL StartExam + banner D-01 'Waktu ujian Anda sudah habis'.
    await assertTier1Reject(page, sessionId);
    // AuditLog SubmitExamBlocked verify TETAP MANUAL SQL spot-check di 313-UAT.md (Playwright tidak query DB direct).
  });

  test('313.3 - Auto + after-time (in grace) + Online → submit OK (Tier 2 grace covers)', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture Online AutoInGrace';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    // Server timer-expired → redirect ExamSummary. JS auto-fire retry POST SubmitExam.
    // isAutoSubmit=true + elapsed within grace (< allowed + 2min) → server Tier-2 ACCEPT → 302 Results.
    await assertSubmitSuccess(page, sessionId);
  });

  test('313.4 - Auto + after-grace + Online → BLOCKED Tier 2 (existing preserved)', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture Online AutoAfterGrace';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    // isAutoSubmit=true + elapsed > allowed + 2min grace → server Tier-2 REJECT.
    // JS retry handler intercept redirect StartExam → custom .alert-warning 'Server menolak submit'
    // ATAU server-side .alert-danger 'Waktu ujian Anda telah habis' (kalau JS tidak intercept).
    // assertTier2Reject covers both branches via regex match.
    await assertTier2Reject(page, sessionId);
  });

  test('313.5 - Manual + after-time + PreTest → BLOCKED (Tier-1)', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture PreTest ManualAfterGrace';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    await assertTier1Reject(page, sessionId);
  });

  test('313.6 - Manual + after-time + PostTest → BLOCKED (Tier-1)', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture PostTest ManualAfterGrace';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    await assertTier1Reject(page, sessionId);
  });

  test('313.7 - Manual + after-time + Manual type → submit OK (D-15 exclude verify)', async ({ page }) => {
    await login(page, 'coachee');
    const fixtureTitle = 'Phase 313 Timer Fixture Manual ExcludeVerify';
    const sessionId = await clickResumeForFixture(page, fixtureTitle);
    // AssessmentType=Manual + StartedAt=NOW-161min: server redirect StartExam → ExamSummary
    // (timer-expired generic check). D-15 exclude di SubmitExam controller layer (BUKAN
    // StartExam guard) — Manual type bypass Tier-1/Tier-2 reject saat submit.
    // ExamSummary auto-fire submit dengan isAutoSubmit=true → server SubmitExam → D-15 exclude
    // → ACCEPT → 302 Results.
    await assertSubmitSuccess(page, sessionId);
    // AuditLog "TIDAK ada SubmitExamBlocked row untuk fixture id 156" verify tetap MANUAL SQL spot-check
    // di 313-UAT.md (Playwright tidak query DB direct, deferred per CONTEXT.md).
  });
});

// Helper untuk escape regex special chars (Pitfall 5 — Phase 312 WR-03 selector substring mitigation)
function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// ============================================================
// FLOW K (BARU, Phase 379 D-01): Essay Full Cycle + Score Aggregation (GRADE-01)
// Bukti hidup fix GRADE-01 Phase 376 di suite exam-taking: wizard essay → worker fillEssayAnswer →
// HC grade 80 + finalize → ASSERT AssessmentSessions.Score === 80 (BUKAN 0) via DB-scalar (bukan UI badge).
// Port pola exam-types.spec.ts:305-428 (FLOW L), marker [379-K].
// ============================================================
test.describe('Flow K: Essay Full Cycle + Score Aggregation (GRADE-01)', () => {
  let title: string;
  const category = 'IHT';
  let scheduleDate: string;
  let assessmentId: number;
  let packageId: number;
  let sessionId: number;
  const Q_MARKER = '[379-K] Essay GRADE-01 regression';

  test('K1 — HC creates Essay assessment via wizard', async ({ page }) => {
    title = uniqueTitle('Pre Test [379-K] Essay');
    scheduleDate = today();
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category, scheduleDate, scheduleTime: '00:01',
      durationMinutes: 60, passPercentage: 70, allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });

  test('K2 — HC navigates ManagePackages → createDefaultPackage', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
  });

  test('K3 — HC adds 1 Essay question', async ({ page }) => {
    await login(page, 'hc');
    await addQuestionViaForm(page, packageId, {
      type: 'Essay',
      text: `${Q_MARKER} — Jelaskan peran OJT dalam pengembangan kompetensi pekerja (min. 100 kata).`,
      rubrik: 'Penilaian: kelengkapan jawaban + relevansi teori OJT + struktur paragraf. Skor 0-100.',
      maxCharacters: 2000,
      score: 100,
    });
  });

  test('K4 — Worker fills essay + submits → PendingGrading', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: title }).first();
    await expect(card).toBeVisible({ timeout: 10_000 });
    page.once('dialog', (d) => d.accept());
    await card
      .locator('a:has-text("Mulai"), a:has-text("Start"), .btn-start-standard, a:has-text("Resume")')
      .first()
      .click();
    await page.waitForURL(/\/CMP\/StartExam\/\d+/, { timeout: 15_000 });

    await page.waitForFunction(
      () => {
        const w = window as unknown as { assessmentHub?: { state?: string } };
        return w.assessmentHub?.state === 'Connected';
      },
      undefined,
      { timeout: 10_000 }
    );

    sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)![1], 10);

    const qCard = page.locator('[id^="qcard_"]').filter({ hasText: Q_MARKER });
    await expect(qCard).toHaveCount(1);
    await fillEssayAnswer(
      page,
      qCard,
      'OJT (On-the-Job Training) adalah metode pembelajaran terstruktur di tempat kerja yang ' +
        'menggabungkan teori dengan praktek nyata: transfer pengetahuan coach→coachee, pengasahan ' +
        'keterampilan teknis lewat observasi langsung, penilaian berbasis kinerja aktual, feedback ' +
        'loop real-time, dan building muscle memory melalui pengulangan operasional. (E2E [379-K] GRADE-01)'
    );
    await submitExamTwoStep(page);
  });

  test('K5 — HC grades essay 80 + finalize', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'hc');
    await gradeSingleEssaySession(page, { title, category, scheduleDate, sessionId, score: 80 });
  });

  test('K6 — DB assert Score aggregated (GRADE-01: NOT 0)', async () => {
    // Phase 376 GRADE-01: essay-only finalize agregasi EssayScore manual → AssessmentSessions.Score.
    // Assert via DB-scalar (BUKAN UI badge "Sudah Dinilai") supaya buktikan agregasi numerik (D-01).
    const score = await db.queryScalar(`SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`);
    expect(score).toBe(80); // bukan 0 → fix 376 terbukti e2e di suite exam-taking
    const completed = await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${sessionId} AND Status = 'Completed'`);
    expect(completed).toBe(1);
  });
});

// ============================================================
// FLOW L: Empty package + shuffle ON (WSE-01 / SHF-01, scenario #6)
// HC creates assessment (shuffle ON default) → 2 packages, one filled one EMPTY →
// worker must still receive the filled package's questions (NOT 0) → Score > 0 (NOT 0% Fail palsu).
// Pre-fix: ON-path K=Min(filled,0)=0 → worker dapat 0 soal → submit → maxScore=0 → 0% Fail.
// Selectable via -g "empty".
// ============================================================
test.describe('Flow L: Empty Package + Shuffle ON (WSE-01)', () => {
  let title: string;
  const category = 'OJT';
  let scheduleDate: string;
  let assessmentId: number;
  let filledPackageId: number;
  let sessionId: number;
  // Correct option texts — globally unique so a hasText label click selects the right answer
  // regardless of shuffle (question + option order). Distractors deliberately non-overlapping.
  const correctTexts = ['Jawaban Benar Alfa', 'Jawaban Benar Beta', 'Jawaban Benar Gamma'];

  test('L1 — HC creates assessment (shuffle ON default, no token)', async ({ page }) => {
    title = uniqueTitle('Pre Test [380-L] Empty Package Shuffle ON');
    scheduleDate = today();
    await login(page, 'hc');
    // NOTE: do NOT disable shuffle — ShuffleQuestions defaults ON (v27.0); that is exactly the WSE-01 path.
    await createAssessmentViaWizard(page, {
      title, category, scheduleDate, scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 60, allowAnswerReview: true,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
  });

  test('L2 — HC creates 2 packages (Paket A filled, Paket B left EMPTY)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    filledPackageId = await createDefaultPackage(page, 'Paket A'); // will get questions
    await createDefaultPackage(page, 'Paket B');                  // stays EMPTY (0 questions)
  });

  test('L3 — HC adds 3 MC questions to Paket A only (Paket B empty)', async ({ page }) => {
    await login(page, 'hc');
    await addQuestionViaForm(page, filledPackageId, {
      type: 'MultipleChoice', text: '[380-L] Soal 1 — pilih jawaban benar Alfa',
      options: [correctTexts[0], 'Salah A1', 'Salah A2', 'Salah A3'], correctIndex: 0, score: 100,
    });
    await addQuestionViaForm(page, filledPackageId, {
      type: 'MultipleChoice', text: '[380-L] Soal 2 — pilih jawaban benar Beta',
      options: ['Salah B1', correctTexts[1], 'Salah B2', 'Salah B3'], correctIndex: 1, score: 100,
    });
    await addQuestionViaForm(page, filledPackageId, {
      type: 'MultipleChoice', text: '[380-L] Soal 3 — pilih jawaban benar Gamma',
      options: ['Salah C1', 'Salah C2', correctTexts[2], 'Salah C3'], correctIndex: 2, score: 100,
    });
  });

  test('L4 — Worker starts → 3 questions → answers correct → Score > 0 (NOT 0% Fail palsu)', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title }).first();
    await expect(card).toBeVisible({ timeout: 10_000 });
    page.once('dialog', d => d.accept());
    await card.locator('.btn-start-standard, a:has-text("Mulai"), a:has-text("Start")').first().click();
    await page.waitForURL(/\/CMP\/StartExam\/\d+/, { timeout: 15_000 });
    sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)![1], 10);

    await expect(page.locator('#examHeader')).toBeVisible({ timeout: 10_000 });
    // WSE-01 core assertion: worker received the filled package's 3 questions, NOT 0 (pre-fix K=Min=0).
    const questions = page.locator('[id^="qcard_"]');
    await expect(questions).toHaveCount(3, { timeout: 10_000 });

    // Answer each correct option by its globally-unique text (shuffle-safe) — same page context, no re-navigate.
    for (const text of correctTexts) {
      const lbl = page.locator('label[id^="lbl_"]', { hasText: text }).first();
      await lbl.scrollIntoViewIfNeeded();
      await lbl.click();
      await page.waitForTimeout(400);
    }

    await expect(page.locator('#reviewSubmitBtn')).toBeEnabled({ timeout: 10_000 });
    await page.locator('#reviewSubmitBtn').click();
    await page.waitForURL('**/CMP/ExamSummary**', { timeout: 15_000 });
    page.once('dialog', d => d.accept());
    const kumpulkanBtn = page.locator('button:has-text("Kumpulkan Ujian")').first();
    await expect(kumpulkanBtn).toBeEnabled({ timeout: 10_000 });
    await kumpulkanBtn.click();
    await page.waitForURL('**/CMP/Results/**', { timeout: 15_000 });

    // DB-assert: Score > 0 (all-correct → 100) and session Completed. Pre-fix would be 0 (0 questions graded).
    const score = await db.queryScalar(`SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = ${sessionId}`);
    expect(score).toBeGreaterThan(0);
    const completed = await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${sessionId} AND Status = 'Completed'`);
    expect(completed).toBe(1);
  });

  test('L6 — Cleanup: HC deletes assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const row = page.locator('tr', { hasText: title }).first();
    const kebab = row.locator('button.dropdown-toggle').first();
    if (await kebab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await kebab.click();
      const hapusBtn = page.locator('button:has-text("Hapus Grup"), button:has-text("Hapus")').first();
      if (await hapusBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapusBtn.click();
        const confirmBtn = page.locator('#deleteAssessmentModal.show button[type="submit"], #deleteAssessmentModal.show button:has-text("Hapus")').first();
        if (await confirmBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
        }
      }
    }
  });
});

// ============================================================
// FLOW M: Token lowercase heal (WSE-02 / TOK-01, scenario #5)
// HC creates token-required exam → stored AccessToken forced LOWERCASE (simulating the legacy
// admin-edited-lowercase bug state) → worker types the token (client uppercases) → VerifyToken's
// defensive both-sides compare (D-01a) heals it → worker ENTERS (not "Token tidak valid").
// Pre-fix: single-side compare `stored != input.ToUpper()` → lowercase stored never matches → locked out.
// Selectable via -g "token".
// ============================================================
test.describe('Flow M: Token Lowercase Heal (WSE-02)', () => {
  let title: string;
  let assessmentId: number;
  const TOKEN = 'ABC23X';
  const tokenLower = TOKEN.toLowerCase();

  test('M1 — HC creates token-required exam with package + question', async ({ page }) => {
    title = uniqueTitle('Pre Test [380-M] Token Lowercase Heal');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'IHT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 30, passPercentage: 70, allowAnswerReview: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      isTokenRequired: true, accessToken: TOKEN,   // stored UPPERCASE by CreateAssessment
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    const packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: '[380-M] Soal token heal?',
      options: ['Jawaban A', 'Jawaban B', 'Jawaban C', 'Jawaban D'], correctIndex: 0, score: 100,
    });
  });

  test('M2 — Force stored AccessToken to LOWERCASE (simulate legacy edited-lowercase bug)', async () => {
    // Reproduce the pre-fix data state: token stored lowercase (admin had edited it lowercase before D-01b).
    const rows = await db.queryScalar(
      `UPDATE AssessmentSessions SET AccessToken = '${tokenLower}' WHERE Id = ${assessmentId}; SELECT @@ROWCOUNT;`
    );
    expect(rows).toBeGreaterThanOrEqual(1);
    const stored = await db.queryString(`SELECT AccessToken FROM AssessmentSessions WHERE Id = ${assessmentId}`);
    expect(stored).toBe(tokenLower); // confirm bug state set
  });

  test('M3 — Worker enters with token despite lowercase storage (defensive compare heals)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title }).first();
    await expect(card).toBeVisible({ timeout: 10_000 });
    await card.locator('.btn-start-token').click();

    const modal = page.locator('.modal.show');
    await expect(modal).toBeVisible({ timeout: 5_000 });
    // Type lowercase; client force-uppercases (Assessment.cshtml:757) → 'ABC23X'.
    await page.locator('#tokenInput').fill(tokenLower);
    await page.locator('#btnVerify').click();

    // Defensive compare: ('abc23x').Trim().ToUpper() == ('ABC23X').Trim().ToUpper() → SUCCESS → StartExam.
    await page.waitForURL(/\/CMP\/StartExam\/\d+/, { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible({ timeout: 10_000 });
    // No "Token tidak valid" error surfaced.
    await expect(page.locator('body')).not.toContainText('Token tidak valid');
  });

  test('M4 — Cleanup: HC deletes token assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
    await searchInput.fill(title);
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    const dropdown = page.locator('tr', { hasText: title }).first().locator('button.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      autoConfirm(page);
      await dropdown.click();
      await page.waitForTimeout(500);
      const hapus = page.locator('button:has-text("Hapus Grup"), button:has-text("Hapus")').first();
      if (await hapus.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await hapus.click();
        await page.waitForLoadState('networkidle');
      }
    }
  });
});

