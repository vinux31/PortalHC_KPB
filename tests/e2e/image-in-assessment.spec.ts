// Phase 355 (TST-02) — UAT end-to-end gambar di soal assessment (milestone v24.0).
// Membuktikan lintas-stack: admin upload gambar soal + opsi via FORM NYATA (setInputFiles)
//   -> peserta StartExam render <img> responsif + lightbox tanpa toggle radio (guard bug 926a57e1)
//   -> peserta Results (pembahasan) render <img>
//   -> guard null-branch: soal tanpa gambar TIDAK render <img> (D-06).
//
// Lesson Phase 354: _QuestionImage reflection-based — bug runtime LOLOS build+grep, hanya ketahuan
//   runtime. UAT WAJIB menjalankan render NYATA di browser (bukan static check).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev
//   (resolve InstanceDefaultBackupPath — C:\Temp diblokir service account) -> UAT (admin buat
//   assessment+paket+2 soal via UI, file ke wwwroot/uploads/questions/{pkgId}) -> afterAll RESTORE
//   (sukses ATAU gagal) + fs.rmSync folder upload (file fisik TIDAK ter-cover DB RESTORE).
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run) + DB lokal HcPortalDB_Dev.
//   Bila Playwright "browser not found" -> cd tests; npx playwright install chromium.
//
// Auth: admin (admin@pertamina.com) buat assessment; coachee (rino.prasetyo@pertamina.com) kerjakan.
//   pwd dev lokal 123456 — JANGAN staging/prod.

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
import * as path from 'node:path';

const Q_IMG = path.resolve(__dirname, '../fixtures/q-image.jpg');
const OPT_IMG = path.resolve(__dirname, '../fixtures/opt-image.png');
const Q_IMG_TEXT = 'Soal bergambar 355 diagram pompa';
const Q_NOIMG_TEXT = 'Soal teks 355 tanpa gambar';
const Q_IMG_ALT = 'diagram pompa';
const OPT_IMG_ALT = 'opsi impeller';

const today = () => new Date().toISOString().slice(0, 10);

let snapshotPath: string;
let createdPackageId: number | null = null;
let assessmentTitle: string;

test.describe.configure({ mode: 'serial' });

test.describe('Phase 355 — gambar di soal assessment (UAT end-to-end)', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre355-${ts}.bak`;
    await db.backup(snapshotPath);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs');
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    // Cleanup file fisik upload — TIDAK ter-cover DB RESTORE (Seed Workflow D-05).
    if (createdPackageId != null) {
      const fs = await import('node:fs');
      const p = await import('node:path');
      const dir = p.resolve(__dirname, '../../wwwroot/uploads/questions', String(createdPackageId));
      try { fs.rmSync(dir, { recursive: true, force: true }); } catch { /* best-effort */ }
    }
    if (restoreError) throw restoreError;
  });

  // ── TEST 1: admin upload gambar soal + opsi via FORM NYATA ────────────────
  test('admin upload gambar soal + opsi via form (setInputFiles)', async ({ page }) => {
    test.setTimeout(120_000);
    // Title WAJIB pola naming-convention Phase 336/339 (AssessmentAdminController.cs:866-874):
    // standard assessment non-PrePostTest harus match ^(Pre|Post)\s*Test\s+.+$ .
    assessmentTitle = `Pre Test OJT IMG355 ${Date.now()}`;
    await login(page, 'admin');

    await createAssessmentViaWizard(page, {
      title: assessmentTitle,
      category: 'OJT',
      scheduleDate: today(),
      scheduleTime: '00:01',
      durationMinutes: 60,
      passPercentage: 50,
      allowAnswerReview: true,        // WAJIB: Results render pembahasan + gambar
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      ewcdDate: today(),
      ewcdTime: '23:59',
    });

    // Dismiss static success modal via manage-btn (Pitfall 3) -> /Admin/ManagePackages
    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');

    createdPackageId = await createDefaultPackage(page);

    // Soal #1 — DENGAN gambar soal + gambar opsi A (upload via form nyata).
    await addQuestionViaForm(
      page,
      createdPackageId,
      { type: 'MultipleChoice', text: Q_IMG_TEXT, options: ['Impeller', 'Casing', 'Shaft', 'Bearing'], correctIndex: 0, score: 50 },
      { question: Q_IMG, questionAlt: Q_IMG_ALT, optionA: OPT_IMG, optionAAlt: OPT_IMG_ALT }
    );

    // Soal #2 — TANPA gambar (untuk guard null-branch D-06).
    await addQuestionViaForm(
      page,
      createdPackageId,
      { type: 'MultipleChoice', text: Q_NOIMG_TEXT, options: ['Benar', 'Salah', 'Mungkin', 'Tidak tahu'], correctIndex: 0, score: 50 }
    );

    expect(createdPackageId).toBeGreaterThan(0);
  });

  // ── TEST 2: peserta StartExam render + guard toggle + guard null + Results ──
  test('peserta StartExam + Results render gambar (responsif) + guard toggle + guard null', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: assessmentTitle });
    await expect(card).toBeVisible({ timeout: 10_000 });
    page.once('dialog', (d) => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible();

    // --- RND-01 + RND-07: gambar SOAL render responsif di StartExam ---
    const qcardImg = page.locator('[id^="qcard_"]', { hasText: Q_IMG_TEXT });
    const qImg = qcardImg.locator(`img.question-image-zoom[data-img-alt="${Q_IMG_ALT}"]`);
    await expect(qImg).toBeVisible();
    expect(await qImg.getAttribute('class')).toContain('img-fluid');
    expect(await qImg.getAttribute('loading')).toBe('lazy');
    expect(await qImg.getAttribute('src')).toMatch(/\/uploads\/questions\//);

    // gambar OPSI render
    const optImg = page.locator(`img.question-image-zoom[data-img-alt="${OPT_IMG_ALT}"]`).first();
    await expect(optImg).toBeVisible();

    // --- Guard toggle (bug 926a57e1): gambar opsi DI DALAM <label> ---
    // klik gambar opsi -> lightbox open DAN radio opsi TIDAK ke-toggle (preventDefault).
    const optLabel = page.locator('label.list-group-item', {
      has: page.locator(`img.question-image-zoom[data-img-alt="${OPT_IMG_ALT}"]`),
    });
    const optRadio = optLabel.locator('input.exam-radio');
    expect(await optRadio.isChecked()).toBe(false);
    await optImg.click();
    await expect(page.locator('#imageLightboxModal')).toBeVisible({ timeout: 5_000 });
    expect(await optRadio.isChecked()).toBe(false);          // radio TIDAK ke-toggle
    await page.locator('#imageLightboxModal .btn-close[data-bs-dismiss="modal"]').click(); // tutup lightbox
    await expect(page.locator('#imageLightboxModal')).toBeHidden({ timeout: 5_000 });

    // --- Guard null-branch (D-06): soal TANPA gambar tidak render <img> ---
    const qcardNoImg = page.locator('[id^="qcard_"]', { hasText: Q_NOIMG_TEXT });
    await expect(qcardNoImg.locator('img.question-image-zoom')).toHaveCount(0);

    // --- Jawab kedua soal (radio pertama tiap qcard) supaya submit lolos (ExamSummary unanswered=0) ---
    for (const qc of [qcardImg, qcardNoImg]) {
      await qc.locator('input.exam-radio').first().check({ force: true });
    }
    // Pastikan kedua jawaban ter-register + SignalR auto-save settle sebelum review
    // (reviewSubmitBtn menunda submit sampai pendingSaves=0 → cegah nav telat).
    await expect(page.locator('#answeredProgress')).toContainText('2/2', { timeout: 10_000 });
    await page.waitForTimeout(2_000);

    // --- RND-03: Results (pembahasan) render gambar soal + opsi ---
    // StartExam → ExamSummary (POST #examForm → redirect /CMP/ExamSummary/{id})
    await Promise.all([
      page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 30_000 }),
      page.click('#reviewSubmitBtn'),
    ]);
    // ExamSummary → Results (confirm dialog "Kumpulkan Ujian")
    page.once('dialog', (d) => d.accept());
    await Promise.all([
      page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 30_000 }),
      page.click('button[type="submit"]:has-text("Kumpulkan")'),
    ]);
    await expect(page.locator(`img.question-image-zoom[data-img-alt="${Q_IMG_ALT}"]`).first()).toBeVisible({ timeout: 10_000 });
    await expect(page.locator(`img.question-image-zoom[data-img-alt="${OPT_IMG_ALT}"]`).first()).toBeVisible();
  });
});
