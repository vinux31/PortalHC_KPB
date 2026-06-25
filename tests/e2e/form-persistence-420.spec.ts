// Phase 420 (FORM-01) — UAT e2e lifecycle persistensi Acak Soal/Pilihan (milestone v32.7).
// Membuktikan AKAR BUG E-01 (HIGH) tertutup end-to-end: shuffle TIDAK reset OFF tiap Edit.
//
// Skenario lifecycle (1 test serial):
//   1. HC create assessment STANDARD via wizard → ShuffleQuestions/ShuffleOptions default ON (Phase 372).
//   2. Buka /Admin/EditAssessment/{id} → assert #ShuffleQuestions + #ShuffleOptions toBeChecked
//      (membuktikan Plan 420-01 me-RENDER shuffle dari Model — sebelum fix view tak render → reset OFF).
//   3. Submit form Edit (tanpa mengubah shuffle) → buka ULANG Edit → assert KEDUA MASIH toBeChecked
//      (E-01 closed: render + write + reopen konsisten).
//
// Template: tests/e2e/shuffle.spec.ts (mode:'serial' + db.backup/restore SEED_WORKFLOW + login admin).
// Selektor NYATA:
//   - wizard create: tests/e2e/helpers/examTypes.ts createAssessmentViaWizard (4-step) →
//     landing /Admin/ManagePackages?assessmentId={id} (id di-parse dari URL).
//   - Edit shuffle: Views/Admin/EditAssessment.cshtml:446/451 (#ShuffleQuestions/#ShuffleOptions, Plan 420-01).
//   - Edit submit: EditAssessment.cshtml:702 button[type=submit] "Simpan Perubahan".
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE.
// PRECONDITION run: app @ E2E_BASE_URL (branch ITHandoff: http://localhost:5270) + DB lokal HcPortalDB_Dev.
//   WAJIB --workers=1 (playwright.config fullyParallel:false, DB isolation).
// Auth: admin@pertamina.com / 123456 (dev lokal — JANGAN staging/prod).
//
// CATATAN: executor TIDAK menjalankan live run — ini gate UAT orchestrator (checkpoint Task 4).

import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard } from './helpers/examTypes';

// Jadwal WAJIB di masa depan (server tolak "Schedule date cannot be in the past").
const futureDate = (): string => {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().slice(0, 10);
};

let snapshotPath: string;

test.describe.configure({ mode: 'serial' });

// Create 1 assessment STANDARD via wizard → return assessmentId (parse dari URL ManagePackages).
async function createStandardAssessment(page: Page, title: string): Promise<number> {
  await login(page, 'admin');
  await createAssessmentViaWizard(page, {
    title,
    category: 'OJT',
    scheduleDate: futureDate(),
    scheduleTime: '00:01',
    durationMinutes: 60,
    passPercentage: 50,
    allowAnswerReview: true,
    generateCertificate: false,
    participantEmails: ['rino.prasetyo@pertamina.com'],
  });
  // Dismiss static success modal (Pitfall 3) → /Admin/ManagePackages?assessmentId={id}
  await page.locator('#modal-manage-btn').click();
  await page.waitForLoadState('networkidle');
  const id = parseInt(new URL(page.url()).searchParams.get('assessmentId') ?? '0', 10);
  expect(id, 'assessmentId ter-parse dari URL ManagePackages').toBeGreaterThan(0);
  return id;
}

async function openEdit(page: Page, id: number): Promise<void> {
  // Route: AssessmentAdminController is attribute-routed `[Route("Admin/[action]")]` (no `{id}`
  // segment) → path-style `/Admin/EditAssessment/{id}` 404s. EditAssessment(int id) binds id from
  // query string. Verified live @5270 UAT 2026-06-23 (Task 4 checkpoint).
  await page.goto(`/Admin/EditAssessment?id=${id}`);
  await page.waitForLoadState('networkidle');
  // Pastikan halaman Edit (bukan redirect manual/lock).
  await expect(page.locator('#editAssessmentForm')).toBeVisible();
}

test.describe('Phase 420 FORM-01 — persistensi Acak Soal/Pilihan (E-01 lifecycle)', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre420persist-${ts}.bak`;
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
    if (restoreError) throw restoreError;
  });

  // ── FORM-01 lifecycle: create shuffle ON → Edit (render checked) → submit → reopen → MASIH checked ──
  test('FORM-01: shuffle TIDAK reset OFF setelah Edit (create → Edit → submit → reopen)', async ({ page }) => {
    test.setTimeout(180_000);
    const id = await createStandardAssessment(page, `Pre Test OJT FORM01 ${Date.now()}`);

    // (1) Buka Edit pertama → shuffle dirender DARI state tersimpan (default ON Phase 372).
    await openEdit(page, id);
    const sq = page.locator('#ShuffleQuestions');
    const so = page.locator('#ShuffleOptions');
    await expect(sq, 'Acak Soal dirender + checked dari Model (Plan 420-01 render fix)').toBeChecked();
    await expect(so, 'Acak Pilihan dirender + checked dari Model').toBeChecked();

    // (2) Submit form Edit TANPA mengubah shuffle (sebelum fix: bind ke false → reset OFF).
    await page.locator('#editAssessmentForm button[type="submit"]').click();
    await page.waitForLoadState('networkidle');

    // (3) Buka ULANG Edit → KEDUA shuffle MASIH checked (E-01 closed: tidak reset OFF).
    await openEdit(page, id);
    await expect(page.locator('#ShuffleQuestions'), 'Acak Soal MASIH ON pasca-Edit (anti-reset E-01)').toBeChecked();
    await expect(page.locator('#ShuffleOptions'), 'Acak Pilihan MASIH ON pasca-Edit').toBeChecked();
  });
});
