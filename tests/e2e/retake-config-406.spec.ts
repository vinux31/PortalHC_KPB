// Phase 406 (RTK-05) — UAT e2e Retake Config UI (milestone v32.4 Ujian Ulang).
// Runtime-verifies the Razor surface (Lesson Phase 354: grep+build insufficient):
//   1. card render + progressive disclosure (#allowRetake reveals #retakeFields)
//   2. hide untuk Pre-Test (ShouldHideRetakeToggle)
//   3. save POST → PRG + persisted values reflected on reload
//   4. non-blocking warning (MaxAttempts < RetakeMaxAttemptsUsedInGroup) + Save tetap enabled
//   5. binding CreateAssessment Step 3 (asp-for AllowRetake/MaxAttempts/RetakeCooldownHours)
//   6. binding EditAssessment (reflect persisted values)
//
// Template: tests/e2e/shuffle.spec.ts (Phase 375) — mode:'serial' + DB backup/restore
//   beforeAll/afterAll + createAssessmentViaWizard. JANGAN sentuh exam-taking flow (Phase 408).
//
// Selektor NYATA: Views/Admin/ManagePackages.cshtml (#allowRetake / #retakeFields / #maxAttempts /
//   #retakeCooldownHours / .alert-warning / button "Simpan Pengaturan") +
//   CreateAssessment.cshtml/EditAssessment.cshtml (#AllowRetake / #MaxAttempts / #RetakeCooldownHours).
// Aturan render: Helpers/RetakeRules.ShouldHideRetakeToggle (Hide = AssessmentType=="PreTest" || IsManualEntry)
//   + AssessmentAdminController.ManagePackages ViewBag retake (:5752-5760) + UpdateRetakeSettings (:5613).
//
// State khusus skenario 2/4 (PreTest hide / warning) di-set via SQL UPDATE/INSERT pada record
//   yang baru dibuat wizard (snapshot beforeAll/restore afterAll melindungi DB lokal). Setiap SQL
//   didokumentasikan inline.
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE.
// PRECONDITION run: app @ http://localhost:5270 (branch ITHandoff; Authentication__UseActiveDirectory=false
//   dotnet run --urls http://localhost:5270) + DB lokal HcPortalDB_Dev. WAJIB --workers=1.
//   Set E2E_BASE_URL=http://localhost:5270 (playwright.config default 5277).
//
// Auth: admin@pertamina.com / 123456 (dev lokal — JANGAN staging/prod).

import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard } from './helpers/examTypes';
import { wizardSelectors } from './helpers/wizardSelectors';

// Jadwal WAJIB di masa depan (server tolak "Schedule date cannot be in the past").
const futureDate = () => {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().slice(0, 10);
};

let snapshotPath: string;

test.describe.configure({ mode: 'serial' });

// Jalankan UPDATE/INSERT via sqlcmd (localhost-guard di dbSnapshot). Append SELECT @@ROWCOUNT
// supaya ada output numerik (rowcount).
async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}

// login admin + wizard-create 1 assessment Standard + arrive di /Admin/ManagePackages.
// Return assessmentId (parse dari query string). doLogin=false untuk create kedua dalam SATU test.
async function createAssessmentArriveMP(page: Page, title: string, doLogin = true): Promise<number> {
  if (doLogin) await login(page, 'admin');
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

// Selektor card Ujian Ulang (ManagePackages). Hanya satu .card yang memuat teks ini.
function retakeCard(page: Page) {
  const card = page.locator('.card', { hasText: 'Ujian Ulang' });
  return {
    card,
    allowRetake: page.locator('#allowRetake'),
    retakeFields: page.locator('#retakeFields'),
    maxAttempts: page.locator('#maxAttempts'),
    cooldown: page.locator('#retakeCooldownHours'),
    warning: card.locator('.alert-warning', { hasText: 'Maksimal percobaan yang Anda set lebih kecil' }),
    saveBtn: card.locator('button[type="submit"]:has-text("Simpan Pengaturan")'),
  };
}

test.describe('Phase 406 — Retake Config ManagePackages (UAT e2e)', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre406-${ts}.bak`;
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

  // ── Scenario 1: card render + disclosure ──
  test('card render + disclosure: card visible, #retakeFields d-none saat OFF, reveal saat toggle', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Std OJT RTK406 S1 ${Date.now()}`);
    await gotoMP(page, id);

    const r = retakeCard(page);
    await expect(r.card).toBeVisible();
    await expect(r.allowRetake).toBeVisible();
    // Default migration AllowRetake=false → fields hidden (d-none).
    await expect(r.retakeFields).toHaveClass(/d-none/);
    await expect(r.maxAttempts).toBeHidden();

    // Toggle ON → reveal (live JS, no reload).
    await r.allowRetake.check();
    await expect(r.retakeFields).not.toHaveClass(/d-none/);
    await expect(r.maxAttempts).toBeVisible();
    await expect(r.cooldown).toBeVisible();
  });

  // ── Scenario 2: hide untuk Pre-Test ──
  test('hide: card Ujian Ulang TIDAK dirender untuk Pre-Test', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Std OJT RTK406 S2 ${Date.now()}`);
    // ShouldHideRetakeToggle: AssessmentType=="PreTest" || IsManualEntry. Set PreTest (paling stabil).
    await execSql(`UPDATE AssessmentSessions SET AssessmentType = 'PreTest' WHERE Id = ${id}`);
    await gotoMP(page, id);

    await expect(page.locator('.card', { hasText: 'Ujian Ulang' })).toHaveCount(0);
    await expect(page.locator('#allowRetake')).toHaveCount(0);
  });

  // ── Scenario 3: save persist → PRG + reflect on reload ──
  test('save: set maxAttempts+cooldown → Simpan → PRG success + persisted on reload', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Std OJT RTK406 S3 ${Date.now()}`);
    await gotoMP(page, id);

    const r = retakeCard(page);
    await r.allowRetake.check();
    await r.maxAttempts.fill('4');
    await r.cooldown.fill('48');
    await r.saveBtn.click();
    await page.waitForLoadState('networkidle');
    // UpdateRetakeSettings → TempData["Success"] → RedirectToAction (PRG). .first() hindari strict-mode.
    await expect(page.locator('.alert-success', { hasText: 'berhasil disimpan' }).first()).toBeVisible();

    // Reload → persisted: toggle ON + values reflected.
    await gotoMP(page, id);
    const r2 = retakeCard(page);
    await expect(r2.allowRetake).toBeChecked();
    await expect(r2.retakeFields).not.toHaveClass(/d-none/);
    await expect(r2.maxAttempts).toHaveValue('4');
    await expect(r2.cooldown).toHaveValue('48');
  });

  // ── Scenario 4: non-blocking warning ──
  test('warning: MaxAttempts < terpakai → alert-warning muncul, Save TETAP enabled (non-blocking)', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Std OJT RTK406 S4 ${Date.now()}`);

    // Ambil UserId/Title/Category sesi untuk seed 2 baris AssessmentAttemptHistory (1 user, 2 attempt
    // archived) → RetakeMaxAttemptsUsedInGroup = max(count per user) + 1 = 2 + 1 = 3.
    const uid = await db.queryString(`SET NOCOUNT ON; SELECT TOP 1 UserId FROM AssessmentSessions WHERE Id = ${id}`);
    // Set AllowRetake ON + MaxAttempts=1 supaya 1 < 3 → warning. (Reuse UpdateRetakeSettings clamp domain.)
    await execSql(`UPDATE AssessmentSessions SET AllowRetake = 1, MaxAttempts = 1 WHERE Id = ${id}`);
    // Seed 2 arsip attempt untuk user yang sama (Title/Category match sesi).
    await execSql(
      `INSERT INTO AssessmentAttemptHistory (SessionId, UserId, Title, Category, Score, IsPassed, AttemptNumber, ArchivedAt, CreatedAt) ` +
      `SELECT s.Id, '${uid}', s.Title, s.Category, 40, 0, 1, GETUTCDATE(), GETUTCDATE() FROM AssessmentSessions s WHERE s.Id = ${id} ` +
      `UNION ALL ` +
      `SELECT s.Id, '${uid}', s.Title, s.Category, 45, 0, 2, GETUTCDATE(), GETUTCDATE() FROM AssessmentSessions s WHERE s.Id = ${id}`
    );
    await gotoMP(page, id);

    const r = retakeCard(page);
    await expect(r.allowRetake).toBeChecked();         // AllowRetake=1 → fields visible
    await expect(r.warning).toBeVisible();             // 1 < 3 → warning
    await expect(r.saveBtn).toBeEnabled();             // non-blocking — Save tidak disabled (D-03 no-lock)

    // Save TETAP berhasil (non-blocking warning) → bump MaxAttempts to 5.
    await r.maxAttempts.fill('5');
    await r.saveBtn.click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.alert-success', { hasText: 'berhasil disimpan' }).first()).toBeVisible();
  });

  // ── Scenario 5: binding CreateAssessment Step 3 ──
  test('binding: CreateAssessment Step 3 — toggle AllowRetake reveal MaxAttempts/RetakeCooldownHours', async ({ page }) => {
    test.setTimeout(120_000);
    await login(page, 'admin');
    await page.goto('/Admin/CreateAssessment');

    // Navigate STEP 1 → STEP 2 (pilih 1 peserta, validateStep gate) → STEP 3 (settings + retake binding).
    await page.locator(wizardSelectors.step1).waitFor({ state: 'visible' });
    await page.selectOption(wizardSelectors.category, 'OJT');
    await page.fill(wizardSelectors.title, `Std OJT RTK406 S5 ${Date.now()}`);
    await page.locator(wizardSelectors.btnNext1).click();

    await page.locator(wizardSelectors.step2).waitFor({ state: 'visible', timeout: 5_000 });
    const fallback = page.locator(`${wizardSelectors.userContainer} ${wizardSelectors.userCheckItem}`)
      .filter({ hasText: 'rino.prasetyo'.split('@')[0] })
      .locator(wizardSelectors.userCheckbox)
      .first();
    await fallback.waitFor({ state: 'visible', timeout: 8_000 });
    await fallback.check();
    await page.locator(wizardSelectors.btnNext2).click();

    await page.locator(wizardSelectors.step3).waitFor({ state: 'visible', timeout: 5_000 });

    // Retake binding fields rendered (asp-for emits id=property name).
    const allowRetake = page.locator('#AllowRetake');
    const retakeFieldsCreate = page.locator('#retakeFieldsCreate');
    await expect(allowRetake).toHaveCount(1);
    await expect(page.locator('#MaxAttempts')).toHaveCount(1);
    await expect(page.locator('#RetakeCooldownHours')).toHaveCount(1);

    // Default OFF → hidden; toggle ON → reveal (disclosure JS keyed on #AllowRetake).
    await expect(retakeFieldsCreate).toHaveClass(/d-none/);
    await allowRetake.check();
    await expect(retakeFieldsCreate).not.toHaveClass(/d-none/);
    await expect(page.locator('#MaxAttempts')).toBeVisible();
    await expect(page.locator('#RetakeCooldownHours')).toBeVisible();
  });

  // ── Scenario 6: binding EditAssessment reflect persisted ──
  test('binding: EditAssessment — 3 field retake render + reflect persisted values', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Std OJT RTK406 S6 ${Date.now()}`);
    // Persist nilai retake known → Edit harus reflect.
    await execSql(`UPDATE AssessmentSessions SET AllowRetake = 1, MaxAttempts = 3, RetakeCooldownHours = 72 WHERE Id = ${id}`);

    // Route [Route("Admin/[action]")] tanpa segmen {id} → id bind dari query string.
    await page.goto(`/Admin/EditAssessment?id=${id}`);
    await page.waitForLoadState('networkidle');

    const allowRetake = page.locator('#AllowRetake');
    await expect(allowRetake).toHaveCount(1);
    await expect(allowRetake).toBeChecked();                    // AllowRetake=1
    await expect(page.locator('#retakeFieldsEdit')).not.toHaveClass(/d-none/);
    await expect(page.locator('#MaxAttempts')).toHaveValue('3');
    await expect(page.locator('#RetakeCooldownHours')).toHaveValue('72');
  });
});
