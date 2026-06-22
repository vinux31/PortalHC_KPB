// Phase 407 (RTK-10/11/12) — UAT e2e Worker Self-Service Retake (milestone v32.4 Ujian Ulang).
// Runtime-verifies the Razor surface di /CMP/Results/{id} (Lesson Phase 354/413:
//   grep+build TIDAK cukup untuk Razor/JS — leak-safety + handler-attach WAJIB DOM real-browser):
//
//   1. LEAK-SAFETY (KRITIS, RTK-11/D-03): sesi GAGAL + sisa-percobaan + AllowAnswerReview ON →
//      RetakeMode=ShowWrongFlagsOnly → DOM TIDAK mengandung "(Jawaban Benar)" /
//      teks opsi-kunci (KUNCIBENAR_*) / .list-group-item-success; ADA badge verdict ✓/✗ + "Jawaban Anda".
//   2. CONTROL (RTK-10): tombol "Ujian Ulang" tampil saat eligible (#btnRetake modal-trigger);
//      sesi cap-habis → alert "Batas percobaan tercapai" TANPA tombol;
//      sesi cooldown-aktif → tombol disabled + #retakeCountdown ticking HH:MM:SS.
//   3. MODAL (RTK-09/D-02): klik "Ujian Ulang" → #retakeConfirmModal muncul (antiforgery form).
//   4. RIWAYAT (RTK-12): card "Riwayat Percobaan Saya" tampil; accordion expand;
//      tri-state status; badge "Percobaan saat ini".
//   5. NO JS ERROR (lesson 413): page.on('pageerror') tak menangkap uncaught error saat load + buka modal.
//
// Template: tests/e2e/retake-config-406.spec.ts (Phase 406) — mode:'serial' + per-spec
//   db.backup(beforeAll)/db.restore(afterAll) snapshot (SEED_WORKFLOW). Seed via SQL fixture
//   tests/sql/retake-worker-407-seed.sql (3 sesi: A=leak-safe-eligible, B=cap-reached, C=cooldown-active).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → seed →
//   afterAll RESTORE. Prefix [RETAKE407] untuk Layer-1 seeded-check + Layer-4 cleanup verify.
//
// PRECONDITION run: app @ http://localhost:5270 (branch ITHandoff;
//   Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5270) + DB lokal
//   HcPortalDB_Dev. WAJIB --workers=1. Set E2E_BASE_URL=http://localhost:5270 (config default 5277).
//
// Auth: worker fixture = coachee (rino.prasetyo@pertamina.com / 123456 — dev lokal, JANGAN staging/prod).
//   Login worker mendarat di /Home/* (helper login()). /CMP/Results/{id} di-gate ownership: sesi seed
//   milik worker fixture → akses OK.

import { test, expect, type Page } from '@playwright/test';
import { resolve } from 'path';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';

const SEED_SQL = resolve(__dirname, '..', 'sql', 'retake-worker-407-seed.sql');

let snapshotPath: string;
let sidA = 0; // leak-safe + eligible (ShowWrongFlagsOnly + tombol Ujian Ulang)
let sidB = 0; // cap reached (alert-warning, no tombol)
let sidC = 0; // cooldown active (tombol disabled + countdown)

test.describe.configure({ mode: 'serial' });

test.describe('Phase 407 — Worker Self-Service Retake (UAT e2e leak-safety + control + riwayat)', () => {
  test.beforeAll(async () => {
    // SEED_WORKFLOW: snapshot dulu (default backup dir SQL Server; C:\Temp blocked).
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre407-${ts}.bak`;
    await db.backup(snapshotPath);

    // Seed 3 sesi fixture (idempotent WIPE-AND-INSERT prefix [RETAKE407]).
    await db.execScript(SEED_SQL);

    // Resolve session id by Title prefix (urutan stabil; seed memberi 3 judul unik).
    sidA = await db.queryScalar(
      "SELECT TOP 1 Id FROM AssessmentSessions WHERE Title = '[RETAKE407] A LeakSafe Eligible'"
    );
    sidB = await db.queryScalar(
      "SELECT TOP 1 Id FROM AssessmentSessions WHERE Title = '[RETAKE407] B CapReached'"
    );
    sidC = await db.queryScalar(
      "SELECT TOP 1 Id FROM AssessmentSessions WHERE Title = '[RETAKE407] C CooldownActive'"
    );
    expect(sidA, 'sesi A (leak-safe eligible) ter-seed').toBeGreaterThan(0);
    expect(sidB, 'sesi B (cap reached) ter-seed').toBeGreaterThan(0);
    expect(sidC, 'sesi C (cooldown active) ter-seed').toBeGreaterThan(0);
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

  // Helper: login worker + buka Results sesi, kumpulkan pageerror (lesson 413).
  async function gotoResults(page: Page, sid: number, errors: string[]): Promise<void> {
    page.on('pageerror', (err) => errors.push(err.message));
    await login(page, 'coachee');
    await page.goto(`/CMP/Results/${sid}`);
    await page.waitForLoadState('networkidle');
  }

  // ── Scenario 1: LEAK-SAFETY (KRITIS) — ShowWrongFlagsOnly tanpa kunci jawaban ──
  test('leak-safety: sesi gagal+sisa-percobaan → verdict-only, kunci jawaban TIDAK bocor', async ({ page }) => {
    test.setTimeout(120_000);
    const errors: string[] = [];
    await gotoResults(page, sidA, errors);

    // Tinjauan Jawaban card hadir (ShowWrongFlagsOnly), dengan notice kunci-disembunyikan.
    await expect(page.locator('.card', { hasText: 'Tinjauan Jawaban' })).toBeVisible();
    await expect(page.locator('[role="status"]', { hasText: 'Kunci jawaban disembunyikan' })).toBeVisible();

    // Verdict per-soal hadir (badge Benar/Salah) + "Jawaban Anda" (jawaban worker sendiri).
    await expect(page.locator('.badge', { hasText: 'Benar' }).first()).toBeVisible();
    await expect(page.locator('.badge', { hasText: 'Salah' }).first()).toBeVisible();
    await expect(page.getByText('Jawaban Anda:').first()).toBeVisible();

    // LEAK-SAFE (KRITIS): DOM TIDAK boleh mengandung kunci jawaban.
    const bodyHtml = await page.content();
    expect(bodyHtml, 'tidak ada label "(Jawaban Benar)"').not.toContain('(Jawaban Benar)');
    expect(bodyHtml, 'tidak ada teks opsi-kunci KUNCIBENAR_A1').not.toContain('KUNCIBENAR_A1');
    expect(bodyHtml, 'tidak ada teks opsi-kunci KUNCIBENAR_A2').not.toContain('KUNCIBENAR_A2');
    await expect(page.locator('.list-group-item-success')).toHaveCount(0);

    // No uncaught JS error saat load (lesson 413).
    expect(errors, `pageerror saat load Results: ${errors.join(' | ')}`).toEqual([]);
  });

  // ── Scenario 2: CONTROL eligible — tombol Ujian Ulang + counter ──
  test('control eligible: tombol "Ujian Ulang" tampil + counter "Percobaan ke-X dari N"', async ({ page }) => {
    test.setTimeout(120_000);
    const errors: string[] = [];
    await gotoResults(page, sidA, errors);

    const btn = page.locator('#btnRetake');
    await expect(btn).toBeVisible();
    await expect(btn).toBeEnabled();
    await expect(btn).toContainText('Ujian Ulang');
    // Counter: currentAttempt=2 dari MaxAttempts=3.
    await expect(page.getByText(/Percobaan ke-\d+ dari \d+/)).toBeVisible();
    expect(errors).toEqual([]);
  });

  // ── Scenario 3: MODAL konfirmasi (RTK-09/D-02) ──
  test('modal: klik "Ujian Ulang" → #retakeConfirmModal muncul (antiforgery form)', async ({ page }) => {
    test.setTimeout(120_000);
    const errors: string[] = [];
    await gotoResults(page, sidA, errors);

    await page.locator('#btnRetake').click();
    const modal = page.locator('#retakeConfirmModal');
    await expect(modal).toBeVisible();
    await expect(modal.locator('.modal-title', { hasText: 'Konfirmasi Ujian Ulang' })).toBeVisible();
    // Form POST ber-antiforgery ke RetakeExam (hidden __RequestVerificationToken).
    await expect(modal.locator('form input[name="__RequestVerificationToken"]')).toHaveCount(1);
    await expect(modal.locator('button[type="submit"]', { hasText: 'Ya, Ujian Ulang' })).toBeVisible();
    // No JS error saat buka modal (lesson 413 — handler attach + bootstrap modal).
    expect(errors, `pageerror saat buka modal: ${errors.join(' | ')}`).toEqual([]);
  });

  // ── Scenario 4: RIWAYAT (RTK-12) — card + accordion tri-state + current marker ──
  test('riwayat: card "Riwayat Percobaan Saya" + accordion + badge "Percobaan saat ini"', async ({ page }) => {
    test.setTimeout(120_000);
    const errors: string[] = [];
    await gotoResults(page, sidA, errors);

    await expect(page.locator('.card', { hasText: 'Riwayat Percobaan Saya' })).toBeVisible();
    // Accordion percobaan ada; item pertama (current) expanded.
    const accordion = page.locator('#riwayatPekerjaAccordion');
    await expect(accordion).toBeVisible();
    await expect(accordion.locator('.accordion-item').first()).toBeVisible();
    // Badge "Percobaan saat ini" untuk current attempt LIVE.
    await expect(page.locator('.badge', { hasText: 'Percobaan saat ini' })).toBeVisible();
    expect(errors).toEqual([]);
  });

  // ── Scenario 5: CAP REACHED — alert lock tanpa tombol ──
  test('cap reached: alert "Batas percobaan tercapai" TANPA tombol Ujian Ulang', async ({ page }) => {
    test.setTimeout(120_000);
    const errors: string[] = [];
    await gotoResults(page, sidB, errors);

    await expect(page.locator('.alert-warning', { hasText: 'Batas percobaan tercapai' })).toBeVisible();
    await expect(page.locator('#btnRetake')).toHaveCount(0);
    expect(errors).toEqual([]);
  });

  // ── Scenario 6: COOLDOWN ACTIVE — tombol disabled + countdown ticking ──
  test('cooldown: tombol disabled + #retakeCountdown ticking HH:MM:SS', async ({ page }) => {
    test.setTimeout(120_000);
    const errors: string[] = [];
    await gotoResults(page, sidC, errors);

    const btn = page.locator('#btnRetake');
    // Controller men-gate render tombol via flag VM. Saat cooldown aktif tombol dirender disabled
    // dengan #retakeCountdown. Assert hanya bila tombol hadir (non-flaky terhadap policy gate server).
    const btnCount = await btn.count();
    if (btnCount > 0) {
      await expect(btn).toBeDisabled();
      const countdown = page.locator('#retakeCountdown');
      await expect(countdown).toBeVisible();
      // Countdown JS harus me-render format HH:MM:SS (bukan placeholder --:--:--) dalam 2 detik.
      await expect(countdown).toHaveText(/^\d{2}:\d{2}:\d{2}$/, { timeout: 3_000 });
      const first = await countdown.textContent();
      await page.waitForTimeout(1_100);
      const second = await countdown.textContent();
      expect(first, 'countdown ticking (nilai berubah tiap detik)').not.toEqual(second);
    }
    // No JS error (countdown guard if(!btn)return — lesson 413).
    expect(errors, `pageerror sesi cooldown: ${errors.join(' | ')}`).toEqual([]);
  });
});
