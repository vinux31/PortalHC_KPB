// Phase 387 Plan 04 (PXF-11 / F-11) — e2e a11y: option-image aria-label berisi huruf opsi (A/B/...).
//
// D-09 MANDATORY (lesson Phase 354): aria render = Razor dinamis → grep+build INSUFFICIENT. Spec ini
//   membuktikan di RUNTIME bahwa <img> opsi pada DUA surface review (Results.cshtml + ExamSummary.cshtml)
//   meng-emit aria-label yang memuat huruf opsi.
//
// KUNCI markup (Views/Shared/_QuestionImage.cshtml L28-30): aria-label =
//   - imageAlt ADA   → "Perbesar gambar: {imageAlt}"            (AriaContext DIABAIKAN)
//   - imageAlt KOSONG → "Perbesar gambar {ariaContext}"          → mis. "Perbesar gambar opsi A"
//   Maka spec seed opsi bergambar TANPA alt (optionAAlt/optionBAlt tidak disuplai) agar huruf
//   ("opsi A"/"opsi B") muncul di accessible name. Inilah perilaku PXF-11 yang sebenarnya dipoles
//   (Results.cshtml:391 + ExamSummary.cshtml:62 → AriaContext = "opsi " + letter).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → test →
//   afterAll RESTORE (sukses ATAU gagal) + fs.rmSync folder upload (file fisik tak ter-cover RESTORE).
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run, AD off) + DB lokal HcPortalDB_Dev.
//   Jalankan: cd tests; npx playwright test aria-opsi-387 --workers=1
//
// Auth: admin (admin@pertamina.com) buat assessment; coachee (rino.prasetyo@pertamina.com) kerjakan.
//   pwd dev lokal 123456 — JANGAN staging/prod.

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
import * as path from 'node:path';

const OPT_IMG = path.resolve(__dirname, '../fixtures/opt-image.png');
const Q_TEXT = 'Soal aria-opsi 387 pilih impeller bergambar';

// LOCAL date (bukan UTC) — validasi server `model.Schedule < DateTime.Today` pakai jam LOKAL server.
// toISOString() = UTC → bila offset lokal ke depan UTC, tanggalnya bisa mundur 1 hari → reject "past".
const today = () => {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
};

let snapshotPath: string;
let createdPackageId: number | null = null;
let assessmentTitle: string;

test.describe.configure({ mode: 'serial' });

test.describe('Phase 387 — aria-label opsi bergambar berisi huruf (PXF-11 / F-11)', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre387aria-${ts}.bak`;
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

  // ── TEST 1: admin buat assessment + 1 soal MC dengan gambar opsi A & B TANPA alt ──────────────
  test('admin buat soal MC dengan 2 opsi bergambar (tanpa alt)', async ({ page }) => {
    test.setTimeout(120_000);
    // Title pola naming-convention standard non-PrePost (review surface = AllowAnswerReview ON).
    assessmentTitle = `Pre Test OJT ARIA387 ${Date.now()}`;
    await login(page, 'admin');

    await createAssessmentViaWizard(page, {
      title: assessmentTitle,
      category: 'OJT',
      scheduleDate: today(),
      scheduleTime: '00:01',
      durationMinutes: 60,
      passPercentage: 50,
      allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      ewcdDate: today(),
      ewcdTime: '23:59',
    });

    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');

    createdPackageId = await createDefaultPackage(page);

    // Soal #1 — gambar opsi A + opsi B, SENGAJA TANPA alt (optionAAlt/optionBAlt dihilangkan)
    // agar aria-label jatuh ke cabang AriaContext ("opsi A"/"opsi B").
    await addQuestionViaForm(
      page,
      createdPackageId,
      { type: 'MultipleChoice', text: Q_TEXT, options: ['Impeller', 'Casing', 'Shaft', 'Bearing'], correctIndex: 0, score: 100 },
      { optionA: OPT_IMG, optionB: OPT_IMG }
    );

    expect(createdPackageId).toBeGreaterThan(0);
  });

  // ── TEST 2: 1 alur peserta — ExamSummary (pra-submit) + Results (pasca-submit) ─────────────────
  // Sesi standard HANYA bisa di-start SEKALI → gabung kedua surface dalam satu alur peserta
  // (StartExam → ExamSummary [assert aria] → submit → Results [assert aria]) supaya tak ada
  // konflik state sesi (start-once). Memenuhi acceptance "BOTH Results and ExamSummary".
  test('aria-label opsi bergambar memuat huruf di ExamSummary DAN Results (opsi A / opsi B)', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: assessmentTitle });
    await expect(card).toBeVisible({ timeout: 10_000 });
    page.once('dialog', (d) => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL(/\/CMP\/StartExam\//, { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible();

    // Pilih opsi A (jawaban benar) supaya soal answered (hindari warning unanswered di ExamSummary).
    await page.locator('input.exam-radio, input[type="radio"]').first().check().catch(() => { /* opsional */ });

    // ── Surface 1: ExamSummary (review pra-submit) ──
    await Promise.all([
      page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
      page.click('#reviewSubmitBtn'),
    ]);
    await assertOptionAriaLetters(page, 'ExamSummary');

    // ── Surface 2: Results (pasca-submit, AllowAnswerReview ON) ──
    page.once('dialog', (d) => d.accept());
    await Promise.all([
      page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
      page.click('button[type="submit"]:has-text("Kumpulkan")'),
    ]);
    await assertOptionAriaLetters(page, 'Results');
  });
});

/**
 * RUNTIME assert: img opsi bergambar meng-emit aria-label yang memuat "opsi A" + "opsi B".
 * Membuktikan PXF-11 (AriaContext = "opsi " + letter) ter-render di DOM, BUKAN sekadar source text.
 */
async function assertOptionAriaLetters(page: import('@playwright/test').Page, surface: string): Promise<void> {
  const optAImg = page.locator('img[aria-label*="opsi A"]').first();
  await expect(optAImg, `${surface}: img aria opsi A visible`).toBeVisible({ timeout: 10_000 });
  const ariaA = await optAImg.getAttribute('aria-label');
  expect(ariaA, `${surface} aria-label opsi A`).toContain('opsi A');

  const optBImg = page.locator('img[aria-label*="opsi B"]').first();
  await expect(optBImg, `${surface}: img aria opsi B visible`).toBeVisible();
  const ariaB = await optBImg.getAttribute('aria-label');
  expect(ariaB, `${surface} aria-label opsi B`).toContain('opsi B');
}
