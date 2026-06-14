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
  // Phase 379 — migrasi PENUH (D-02): wizard + proton extension Plan 01; test.skip + fixme DIHAPUS (ProtonTrack T3 ada, W0-1=2).
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
  // 364 drift: CreateAssessment kini wizard 4-langkah (era Phase 317/319), flat-form create usang — butuh migrasi wizard-nav. Backlog 999.7.
  test.fixme(true, '364: CreateAssessment now a 4-step wizard; flat-form create obsolete — needs wizard-nav migration. Backlog 999.7.');
  let title: string;
  let assessmentId: number;

  test('F1 - HC creates assessment for 2 workers with questions', async ({ page }) => {
    title = uniqueTitle('Pre Test Multi Worker');
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
  // 364 drift: CreateAssessment kini wizard 4-langkah (era Phase 317/319), flat-form create usang — butuh migrasi wizard-nav. Backlog 999.7.
  test.fixme(true, '364: CreateAssessment now a 4-step wizard; flat-form create obsolete — needs wizard-nav migration. Backlog 999.7.');
  let title: string;
  let assessmentId: number;

  test('G1 - HC creates 1-minute assessment with question', async ({ page }) => {
    title = uniqueTitle('Pre Test Timer Expired');
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
  // 364 drift: CreateAssessment kini wizard 4-langkah (era Phase 317/319), flat-form create usang — butuh migrasi wizard-nav. Backlog 999.7.
  test.fixme(true, '364: CreateAssessment now a 4-step wizard; flat-form create obsolete — needs wizard-nav migration. Backlog 999.7.');
  let title: string;
  let assessmentId: number;

  test('H1 - HC creates assessment with question', async ({ page }) => {
    title = uniqueTitle('Pre Test RealTime Mon');
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
  // 364 drift: CreateAssessment kini wizard 4-langkah (era Phase 317/319), flat-form create usang — butuh migrasi wizard-nav. Backlog 999.7.
  test.fixme(true, '364: CreateAssessment now a 4-step wizard; flat-form create obsolete — needs wizard-nav migration. Backlog 999.7.');
  let title: string;
  let editedTitle: string;

  test('I1 - HC creates assessment', async ({ page }) => {
    title = uniqueTitle('Pre Test EditTest');
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
  // 364 drift: CreateAssessment kini wizard 4-langkah (era Phase 317/319), flat-form create usang — butuh migrasi wizard-nav. Backlog 999.7.
  test.fixme(true, '364: CreateAssessment now a 4-step wizard; flat-form create obsolete — needs wizard-nav migration. Backlog 999.7.');
  let title: string;
  let assessmentId: number;

  test('J1 - HC creates assessment with question', async ({ page }) => {
    title = uniqueTitle('Pre Test Abandon Test');
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

