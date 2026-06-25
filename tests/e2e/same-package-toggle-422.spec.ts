// v32.7 Phase 422 SHFX-02/SHFX-07 (Wave 3) — UAT e2e smoke ManagePackages toggle SamePackage.
// Mengotomasi sebagian checkpoint UAT Task 3 jadi regresi-proof: RENDER + happy-path toggle ON.
//   Full lifecycle (Import sync, OFF keep, guard anyStarted, warning, PackageNumber) = checkpoint
//   manual live @5270 (orchestrator). Spec ini = smoke: card render, toggle ON → success + lock banner +
//   Kelola Soal disabled, backward-compat Standard tak menampilkan card.
//
// Template: tests/e2e/shuffle.spec.ts (Phase 375) — mode:'serial' + DB backup/restore beforeAll/afterAll
//   + createAssessmentViaWizard + execSql untuk set state Post-paired/SamePackage pada record wizard.
//
// Selektor NYATA: Views/Admin/ManagePackages.cshtml — #samePackageForm / #samePackageSwitch /
//   .card "Paket Soal Sama dengan Pre-Test" / .alert-info "Paket soal disinkronkan dari Pre-Test" /
//   "Kelola Soal" disabled span. Endpoint ToggleSamePackage (AssessmentAdminController) PRG → TempData.
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE.
// PRECONDITION run (branch ITHandoff): app di http://localhost:5270 (E2E_BASE_URL=http://localhost:5270
//   Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5270) + DB lokal
//   HcPortalDB_Dev. WAJIB --workers=1 (DB isolation). Browser not found → cd tests; npx playwright install chromium.
//
// Auth: admin@pertamina.com (dev lokal — JANGAN staging/prod).

import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

const futureDate = () => {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().slice(0, 10);
};

let snapshotPath: string;

test.describe.configure({ mode: 'serial' });

async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}

// login admin + wizard-create 1 assessment standard + arrive di /Admin/ManagePackages. Return assessmentId.
async function createAssessmentArriveMP(page: Page, title: string, category = 'OJT', doLogin = true): Promise<number> {
  if (doLogin) await login(page, 'admin');
  await createAssessmentViaWizard(page, {
    title,
    category,
    scheduleDate: futureDate(),
    scheduleTime: '00:01',
    durationMinutes: 60,
    passPercentage: 50,
    allowAnswerReview: true,
    generateCertificate: false,
    participantEmails: ['rino.prasetyo@pertamina.com'],
  });
  await page.locator('#modal-manage-btn').click();
  await page.waitForLoadState('networkidle');
  const id = parseInt(new URL(page.url()).searchParams.get('assessmentId') ?? '0', 10);
  expect(id, 'assessmentId ter-parse dari URL ManagePackages').toBeGreaterThan(0);
  return id;
}

async function gotoMP(page: Page, assessmentId: number): Promise<void> {
  await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
  await page.waitForLoadState('networkidle');
}

// Card toggle SamePackage (ManagePackages.cshtml). Hanya satu .card memuat teks ini.
function samePackageCard(page: Page) {
  const card = page.locator('.card', { hasText: 'Paket Soal Sama dengan Pre-Test' });
  return {
    card,
    sw: page.locator('#samePackageSwitch'),
    saveBtn: page.locator('#samePackageForm button[type="submit"]'),
    lockBanner: page.locator('.alert-info', { hasText: 'Paket soal disinkronkan dari Pre-Test' }),
    kelolaSoalDisabled: page.locator('span.btn.disabled', { hasText: 'Kelola Soal' }),
    kelolaSoalLink: page.locator('a.btn', { hasText: 'Kelola Soal' }),
  };
}

test.describe('Phase 422 — Toggle SamePackage ManagePackages (UAT e2e smoke)', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre422-${ts}.bak`;
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

  // Helper: buat grup Pre+Post ter-link (Pre punya 1 paket 1 soal), arrive di halaman Post. Return {preId, postId}.
  async function createLinkedPrePostArrivePost(page: Page, tag: string): Promise<{ preId: number; postId: number }> {
    const preId = await createAssessmentArriveMP(page, `Pre Test OJT SP422 ${tag} ${Date.now()}`);
    // Pre punya 1 paket dengan 1 soal (sumber sync).
    const prePkgId = await createDefaultPackage(page);
    await addQuestionViaForm(page, prePkgId, {
      type: 'MultipleChoice', text: `Soal SP422 ${tag} #1`,
      options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 50,
    });
    // Post (doLogin=false — sudah authenticated).
    const postId = await createAssessmentArriveMP(page, `Post Test OJT SP422 ${tag} ${Date.now()}`, 'OJT', false);
    // Link Post→Pre + tandai PostTest (grup pasangan). SamePackage default false (toggle OFF).
    await execSql(
      `UPDATE AssessmentSessions SET AssessmentType = 'PostTest', LinkedSessionId = ${preId}, SamePackage = 0 WHERE Id = ${postId}`
    );
    await execSql(`UPDATE AssessmentSessions SET AssessmentType = 'PreTest', LinkedSessionId = ${postId} WHERE Id = ${preId}`);
    return { preId, postId };
  }

  // ── S1: card render pada Post berpasangan + switch present (default OFF) ──
  test('S1: toggle card render di Post berpasangan + switch unchecked default', async ({ page }) => {
    test.setTimeout(180_000);
    const { postId } = await createLinkedPrePostArrivePost(page, 'S1');
    await gotoMP(page, postId);

    const c = samePackageCard(page);
    await expect(c.card).toBeVisible();
    await expect(c.sw).toBeVisible();
    await expect(c.sw).not.toBeChecked();   // SamePackage=0 → switch OFF
    await expect(c.saveBtn).toBeEnabled();
  });

  // ── S2: toggle ON → success toast + lock banner + Kelola Soal disabled ──
  test('S2: toggle ON → success PRG + lock banner + Kelola Soal disabled', async ({ page }) => {
    test.setTimeout(180_000);
    const { postId } = await createLinkedPrePostArrivePost(page, 'S2');
    await gotoMP(page, postId);

    const c = samePackageCard(page);
    await expect(c.sw).not.toBeChecked();

    // Aktifkan switch → submit. confirm() native → auto-accept dialog.
    page.once('dialog', d => d.accept());
    await c.sw.check();
    await c.saveBtn.click();
    await page.waitForLoadState('networkidle');

    // Endpoint ToggleSamePackage → TempData["Success"] → PRG. (.first() — bisa ada global toast + inline TempData.)
    await expect(page.locator('.alert-success', { hasText: 'diaktifkan' }).first()).toBeVisible();
    // Lock banner muncul + switch kini checked.
    await expect(samePackageCard(page).lockBanner).toBeVisible();
    await expect(samePackageCard(page).sw).toBeChecked();
    // Tombol Kelola Soal kini disabled (span.disabled) — link aktif tak boleh ada.
    await expect(samePackageCard(page).kelolaSoalDisabled.first()).toBeVisible();
    await expect(samePackageCard(page).kelolaSoalLink).toHaveCount(0);
  });

  // ── S3: backward-compat — assessment Standard TIDAK menampilkan toggle card ──
  test('S3: Standard assessment tak menampilkan toggle card (backward-compat)', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Test Standard SP422 S3 ${Date.now()}`);
    await gotoMP(page, id);
    // Standard (bukan Post berpasangan) → card SamePackage tak dirender.
    await expect(page.locator('.card', { hasText: 'Paket Soal Sama dengan Pre-Test' })).toHaveCount(0);
  });
});
