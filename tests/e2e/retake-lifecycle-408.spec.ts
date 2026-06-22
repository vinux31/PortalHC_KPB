// Phase 408 (RTK-14) — UAT e2e LIFECYCLE Ujian Ulang (capstone milestone v32.4).
// GAP-3: SATU happy-path real-browser yang membuktikan ALUR RETAKE BENAR-BENAR DIEKSEKUSI
//   end-to-end — dari KEGAGALAN sampai TERBITNYA SERTIFIKAT (bukti visual akhir):
//
//   1. Login coachee → /CMP/Results/{sesi gagal} → RetakeMode=ShowWrongFlagsOnly:
//      skor + verdict ✓/✗ TANPA kunci jawaban (.list-group-item-success count 0,
//      tidak ada "(Jawaban Benar)"). [leak-safe, RTK-11/D-03]
//   2. Klik #btnRetake → #retakeConfirmModal (antiforgery) → POST RetakeExam (ExecuteAsync
//      reset: hapus responses+assignment + arsip snapshot, Status→Open, clear token) →
//      redirect StartExam. [RTK-09/10]
//   3. StartExam (fresh pasca-reset, worker generate ulang assignment) → jawab BENAR semua
//      (label by-TEXT BENAR408_Qn_*, shuffle-safe v27.0) → submit (submitExamTwoStep) →
//      grade dari DB → Score 100 ≥ PassPercentage 50 → LULUS → terbit NomorSertifikat.
//   4. Results: badge LULUS + Nomor Sertifikat KPB/{seq}/{RomanMonth}/{year}. [RTK-14]
//   5. NO JS ERROR (lesson 354/413): page.on('pageerror') kosong di tiap langkah lifecycle
//      (bug class monFlashRow ReferenceError hanya ketangkap real-browser).
//
//   Exactly-1-cert invariant (anti double-cert) DIBUKTIKAN-ULANG oleh xUnit GAP-1
//   (HcPortal.Tests/RetakeThenPassCertTests.cs, plan 408-01). Cabang lock (cap habis) &
//   cooldown-aktif SUDAH dibuktikan smoke 407 (skenario 5 & 6) → TIDAK diulang di sini.
//
// Harness: tests/e2e/retake-worker-407.spec.ts (mode:'serial' + per-spec db.backup/restore
//   snapshot SEED_WORKFLOW + login coachee + pageerror) DI-JAHIT dengan exam-taking Flow A
//   (answer-by-text + submitExamTwoStep helper). Seed via SQL fixture
//   tests/sql/retake-lifecycle-408-seed.sql (1 sesi [RETAKE408] Lifecycle Fail-to-Pass).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → seed →
//   afterAll RESTORE. Prefix [RETAKE408] untuk seeded-check + cleanup verify.
//
// PRECONDITION run: app @ http://localhost:5270 (branch ITHandoff;
//   Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5270) + DB lokal
//   HcPortalDB_Dev. WAJIB --workers=1. Set E2E_BASE_URL=http://localhost:5270 (config default 5277).
//
// Auth: worker fixture = coachee (rino.prasetyo@pertamina.com — dev lokal, JANGAN staging/prod).
//   /CMP/Results/{id} di-gate ownership: sesi seed milik worker fixture → akses OK.

import { test, expect } from '@playwright/test';
import { resolve } from 'path';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { submitExamTwoStep } from './helpers/examTypes';

const SEED_SQL = resolve(__dirname, '..', 'sql', 'retake-lifecycle-408-seed.sql');

let snapshotPath: string;
let sidFailed = 0; // sesi gagal cooldown=0 → eligible retake → bisa LULUS + cert (lifecycle target)

test.describe.configure({ mode: 'serial' });

test.describe('Phase 408 — Lifecycle Ujian Ulang (UAT e2e gagal → ulang → lulus → cert)', () => {
  test.beforeAll(async () => {
    // SEED_WORKFLOW: snapshot dulu (default backup dir SQL Server; C:\Temp blocked).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre408-${ts}.bak`;
    await db.backup(snapshotPath);

    // Seed 1 sesi lifecycle (idempotent WIPE-AND-INSERT prefix [RETAKE408]).
    await db.execScript(SEED_SQL);

    // Resolve session id by Title.
    sidFailed = await db.queryScalar(
      "SELECT TOP 1 Id FROM AssessmentSessions WHERE Title = '[RETAKE408] Lifecycle Fail-to-Pass'"
    );
    expect(sidFailed, 'sesi lifecycle ter-seed').toBeGreaterThan(0);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs');
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    if (restoreError) throw restoreError;
  });

  // Mapping marker soal → OptionText benar (harus cocok seed Task 1; shuffle-safe by-TEXT).
  function correctTextFor(qText: string): string {
    if (qText.includes('Q2')) return 'BENAR408_Q2_Alkylate';
    if (qText.includes('Q3')) return 'BENAR408_Q3_Isobutana';
    return 'BENAR408_Q1_AsamHF'; // default / Q1
  }

  test('lifecycle: gagal → Ujian Ulang → jawab benar → LULUS + cert terbit (leak-safe, 0 pageerror)', async ({ page }) => {
    test.setTimeout(180_000);
    const errors: string[] = [];
    // Arm pageerror SEBELUM navigasi (lesson 413 — bug class monFlashRow ReferenceError).
    page.on('pageerror', (err) => errors.push(err.message));

    // ── (a) Login coachee + buka Hasil sesi gagal ──
    await login(page, 'coachee');
    await page.goto(`/CMP/Results/${sidFailed}`);
    await page.waitForLoadState('networkidle');

    // ── (b) Hasil pra-retake = ShowWrongFlagsOnly (skor + verdict TANPA kunci jawaban) ──
    await expect(page.locator('.card', { hasText: 'Tinjauan Jawaban' })).toBeVisible();
    await expect(page.locator('[role="status"]', { hasText: 'Kunci jawaban disembunyikan' })).toBeVisible();
    const preBodyHtml = await page.content();
    expect(preBodyHtml, 'tidak ada label "(Jawaban Benar)" pra-retake').not.toContain('(Jawaban Benar)');
    // Kunci jawaban (BENAR408_*) WAJIB tersembunyi selama retake masih mungkin.
    expect(preBodyHtml, 'kunci jawaban BENAR408_* TIDAK boleh bocor pra-retake').not.toContain('BENAR408_');
    await expect(page.locator('.list-group-item-success')).toHaveCount(0);
    expect(errors, `pageerror saat load Results: ${errors.join(' | ')}`).toEqual([]);

    // ── (c) Retake: modal antiforgery → POST RetakeExam → redirect StartExam ──
    await page.locator('#btnRetake').click();
    const modal = page.locator('#retakeConfirmModal');
    await expect(modal).toBeVisible();
    await expect(modal.locator('form input[name="__RequestVerificationToken"]')).toHaveCount(1);
    await Promise.all([
      page.waitForURL('**/CMP/StartExam/**', { timeout: 20_000 }),
      modal.locator('button[type="submit"]').click(), // "Ya, Ujian Ulang"
    ]);
    await page.waitForLoadState('networkidle');
    expect(errors, `pageerror saat retake → StartExam: ${errors.join(' | ')}`).toEqual([]);

    // ── (d) Defensive resume-modal dismiss (lifecycle masuk FRESH pasca-reset; jarang muncul) ──
    const resumeModal = page.locator('#resumeConfirmModal');
    await resumeModal.waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }

    // ── (e) Jawab BENAR semua (shuffle-safe label-by-text) ──
    const questionCards = page.locator('[id^="qcard_"]');
    const qCount = await questionCards.count();
    expect(qCount, 'soal lifecycle ter-render di StartExam').toBeGreaterThan(0);
    for (let i = 0; i < qCount; i++) {
      const qCard = questionCards.nth(i);
      const qText = (await qCard.locator('h6, .fw-bold').first().textContent()) ?? '';
      const correctText = correctTextFor(qText);
      await qCard.locator('label[id^="lbl_"]').filter({ hasText: correctText }).first().click();
      await page.waitForTimeout(700);
    }
    await expect(page.locator('#answeredProgress')).toContainText(`${qCount}/${qCount}`);
    expect(errors, `pageerror saat jawab soal: ${errors.join(' | ')}`).toEqual([]);

    // Pastikan auto-save jawaban (AJAX per-klik) sudah commit ke DB SEBELUM submit —
    // GradingService grade dari DB (bukan form). Tanpa flush, submit bisa balapan dgn auto-save → skor 0.
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1500);

    // ── (f) Submit 2-step (ExamSummary → Kumpulkan → Results) ──
    await submitExamTwoStep(page);
    await page.waitForLoadState('networkidle');

    // ── (g) ASSERT FINAL: LULUS + Nomor Sertifikat terbit (RTK-14) ──
    // Status badge LULUS = span.badge.text-bg-success "LULUS" (Results.cshtml:76). Pakai .first() —
    // badge "Lulus" riwayat juga text-bg-success (hindari strict-mode multi-match).
    await expect(page.locator('.badge.text-bg-success').filter({ hasText: 'LULUS' }).first()).toBeVisible();
    // Skor agregat sesi WAJIB 100% (bukan 0% — defect retake-grade yang ditangkap capstone).
    await expect(page.locator('h2', { hasText: '100%' }).first()).toBeVisible();
    // Pesan sukses + sertifikat terbit (RTK-14).
    await expect(page.locator('body')).toContainText('Anda lulus assessment ini');
    await expect(page.locator('body')).toContainText('Nomor Sertifikat');
    await expect(page.locator('body')).toContainText(/KPB\/\d+\/[IVX]+\/\d{4}/);
    expect(errors, `pageerror saat Results lulus: ${errors.join(' | ')}`).toEqual([]);
  });
});
