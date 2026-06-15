// Phase 385 Plan 02 (PXF-03 / F-21) — e2e essay flush sebelum submit (keystroke terakhir tak hilang
//   + peserta sudah-ketik tak ditolak "belum dijawab").
//
// BUG F-21: jawaban essay HANYA tersimpan via SignalR debounce 2 detik tanpa flush saat submit/blur.
//   Akibat: keystroke ~2 detik terakhir HILANG saat submit langsung, dan baris essay belum ter-save
//   sehingga gate incomplete (CMPController.cs:1627-1653, UNION form+DB) menolak submit palsu
//   "Masih ada N soal belum dijawab".
//   Fix JS-side (Views/CMP/StartExam.cshtml): flushEssay() + await SEBELUM submit/changePage,
//   save-on-blur, timeout best-effort.
//
// DUA SKENARIO (D-05):
//   A — flush keystroke terakhir: ketik essay → submit LANGSUNG (TANPA waitForTimeout 2000) →
//       assert baris PackageUserResponses.TextAnswer == teks utuh (exact-match COUNT di SQL).
//       KUNCI: pakai raw page.fill + submit langsung — BUKAN helper fillEssayAnswer() yang
//       sengaja await hub.invoke (itu mem-bypass bug → test palsu-lulus). Tanpa fix, flush/blur
//       tak ada → teks tak ter-save sebelum submit.
//   B — peserta sudah-ketik tak ditolak: essay terisi → submit → TIDAK ada "belum dijawab",
//       landing di Results (sukses). Tanpa fix: ExamSummary unanswered>0 → Kumpulkan disabled/ditolak.
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → test →
//   afterAll RESTORE (sukses ATAU gagal).
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run, AD off) + DB lokal HcPortalDB_Dev.
//   Jalankan: cd tests; npx playwright test essay-flush-385 --workers=1
//   Bila browser belum ada: npx playwright install chromium.
//
// Auth: admin (admin@pertamina.com) buat assessment; coachee (rino.prasetyo@pertamina.com) kerjakan.
//   pwd dev lokal 123456 — JANGAN staging/prod.

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

// Teks essay TANPA single-quote (aman untuk exact-match SQL N'...').
const ESSAY_TEXT = 'Jawaban essay 385 flush keystroke terakhir wajib tersimpan utuh tanpa hilang saat submit langsung';
const Q_ESSAY_TEXT = 'Soal essay 385 flush sebelum submit';

// LOCAL date (NOT toISOString/UTC) — server validates Schedule against DateTime.Today (local).
// UTC slice fails daily in WIB after local-midnight while UTC is still the prior day → false "past date".
const today = () => {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
};

let snapshotPath: string;
let assessmentTitle: string;

test.describe.configure({ mode: 'serial' });

test.describe('Phase 385 — essay flush sebelum submit + no reject palsu (F-21 / PXF-03)', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre385essay-${ts}.bak`;
    await db.backup(snapshotPath);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    await db.restore(snapshotPath);
    const fs = await import('node:fs');
    try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
  });

  // ── TEST 1: admin buat assessment + 1 soal Essay, assign peserta ──────────────
  test('admin buat assessment + 1 soal essay (assign peserta)', async ({ page }) => {
    test.setTimeout(120_000);
    assessmentTitle = `Pre Test OJT ESSAY385 ${Date.now()}`;
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

    const packageId = await createDefaultPackage(page);
    expect(packageId).toBeGreaterThan(0);

    // 1 soal Essay (rubrik wajib untuk tipe Essay).
    await addQuestionViaForm(page, packageId, {
      type: 'Essay',
      text: Q_ESSAY_TEXT,
      rubrik: 'Rubrik: jawaban menyebut flush + keystroke tersimpan.',
      maxCharacters: 2000,
      score: 100,
    });
  });

  // ── TEST 2: peserta ketik essay → submit LANGSUNG → tersimpan utuh + tak ditolak ──
  test('peserta ketik essay → submit langsung → tersimpan utuh (A) + tak ditolak "belum dijawab" (B)', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const card = page.locator('.assessment-card', { hasText: assessmentTitle });
    await expect(card).toBeVisible({ timeout: 10_000 });
    page.once('dialog', (d) => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible();

    // Capture sessionId + questionId SEBELUM submit (untuk DB assert skenario A).
    const sid = parseInt(page.url().match(/StartExam\/(\d+)/)?.[1] || '0', 10);
    expect(sid, 'sessionId dari URL StartExam').toBeGreaterThan(0);

    const qcard = page.locator('[id^="qcard_"]', { hasText: Q_ESSAY_TEXT });
    await expect(qcard).toBeVisible();
    const qcardId = await qcard.getAttribute('id');
    const qid = parseInt(qcardId?.match(/qcard_(\d+)/)?.[1] || '0', 10);
    expect(qid, 'questionId dari qcard id').toBeGreaterThan(0);

    // Tunggu SignalR hub Connected SEBELUM interaksi. flushEssay/blur (fix) cek
    // `assessmentHub.state === 'Connected'`; tanpa koneksi aktif, flush by-design skip
    // (best-effort). Real user butuh waktu → hub pasti connect; di test WAJIB tunggu eksplisit
    // (BUKAN waitForTimeout 2000 debounce — kita uji flush, bukan debounce).
    await page.waitForFunction(
      () => {
        const w = window as unknown as { assessmentHub?: { state?: string } };
        return !!w.assessmentHub && w.assessmentHub.state === 'Connected';
      },
      undefined,
      { timeout: 15_000 }
    );

    // Isi essay: set value via JS + dispatch 'input' (mensimulasikan ketik). Pakai evaluate (bukan
    // page.fill) supaya robust terhadap atribut maxlength textarea (fill menghormati maxlength;
    // assignment programatik tidak) — kita uji FLUSH-on-submit, bukan enforcement maxlength.
    const textarea = qcard.locator('textarea.exam-essay');
    await textarea.evaluate((el, v) => {
      (el as HTMLTextAreaElement).value = v as string;
      el.dispatchEvent(new Event('input', { bubbles: true }));
    }, ESSAY_TEXT);
    // Sanity: pastikan teks benar-benar masuk textarea sebelum submit (guard regresi empty-fill).
    expect(await textarea.inputValue(), 'textarea essay terisi penuh sebelum submit').toBe(ESSAY_TEXT);

    // KUNCI: submit LANGSUNG — TIDAK menunggu debounce. Hanya flushEssay()/blur (fix) yang
    // menyelamatkan teks. Tanpa fix: teks tak ter-save sebelum examForm.submit().
    await Promise.all([
      page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 30_000 }),
      page.click('#reviewSubmitBtn'),
    ]);

    // --- Skenario A (PRIMARY, ground-truth): baris PackageUserResponses.TextAnswer ter-flush UTUH
    //     ke DB SEBELUM ExamSummary di-render. COUNT exact-match hindari truncation output sqlcmd.
    const savedCount = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageUserResponses ` +
      `WHERE AssessmentSessionId=${sid} AND PackageQuestionId=${qid} AND TextAnswer = N'${ESSAY_TEXT}'`
    );
    expect(savedCount, 'baris essay TextAnswer ter-flush utuh sebelum submit (DB)').toBe(1);

    // --- Skenario B: di ExamSummary TIDAK ada banner peringatan unanswered "belum dijawab" ---
    await expect(page.locator('.alert-warning', { hasText: 'belum dijawab' })).toHaveCount(0);

    // Kumpulkan Ujian → Results (sukses, tidak ditolak balik).
    page.once('dialog', (d) => d.accept());
    await Promise.all([
      page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 30_000 }),
      page.click('button[type="submit"]:has-text("Kumpulkan")'),
    ]);
    // Landing di Results = submit sukses (bukan redirect balik dengan error gate).
    await expect(page).toHaveURL(/\/CMP\/Results\/\d+/);
  });
});
