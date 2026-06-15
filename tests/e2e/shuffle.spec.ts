// Phase 375 (SHUF-16) — UAT e2e sisi ManagePackages untuk Shuffle Toggle (milestone v27.0).
// Meng-automate-kan UAT Phase 374 (manual-verified 7/7) jadi regresi-proof: 5 skenario
//   RENDER + SAVE-PRG (D-02a). BUKAN exam-taking effect — itu manual browser (D-03, Plan 03).
//   Propagate-detail TIDAK di-assert ulang (sudah unit ShufflePropagationTests/ShuffleUpdateEndpointTests).
//
// Template: tests/e2e/image-in-assessment.spec.ts (Phase 355) — mode:'serial' + DB backup/restore
//   beforeAll/afterAll + createAssessmentViaWizard (D-06: JANGAN flat-form, JANGAN sentuh
//   exam-taking.spec.ts / exam-types.spec.ts = scope Phase 364).
//
// Selektor NYATA: Views/Admin/ManagePackages.cshtml:83-132 (#shuffleQuestions / #shuffleOptions /
//   #shuffleSizeWarning / .alert-info lock / .alert-warning reminder / button "Simpan Pengaturan").
// Aturan render: Helpers/ShuffleToggleRules.cs (IsShuffleLocked = anyStarted||anyAssignment;
//   Hide = (Proton & Tahun 3) || IsManualEntry) + AssessmentAdminController.ManagePackages:5326-5429.
//
// State khusus skenario 2/3/5 (started / Pre-Post linked / hide) di-set via SQL UPDATE pada record
//   yang baru dibuat wizard (snapshot beforeAll/restore afterAll melindungi DB lokal). Setiap SQL
//   didokumentasikan inline.
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE.
// PRECONDITION run: app di http://localhost:5277 (Authentication__UseActiveDirectory=false dotnet run)
//   + DB lokal HcPortalDB_Dev. WAJIB --workers=1 (playwright.config fullyParallel:false, DB isolation).
//   Bila browser not found → cd tests; npx playwright install chromium.
//
// Auth: admin@pertamina.com / 123456 (dev lokal — JANGAN staging/prod).

import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

// Jadwal WAJIB di masa depan (server tolak "Schedule date cannot be in the past").
// Pakai besok 00:01 supaya selalu valid berapa pun jam run. ManagePackages render tak peduli jadwal.
const futureDate = () => {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().slice(0, 10);
};

let snapshotPath: string;

test.describe.configure({ mode: 'serial' });

// Jalankan UPDATE/DDL via sqlcmd (localhost-guard di dbSnapshot). queryScalar membungkus
// `SET NOCOUNT ON; <sql>` lalu kita append `SELECT @@ROWCOUNT` supaya ada output numerik (rowcount).
async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}

// login admin + wizard-create 1 assessment standard + arrive di /Admin/ManagePackages.
// Return assessmentId (di-parse dari query string URL setelah dismiss success modal).
// doLogin=false untuk create kedua dalam SATU test (page sudah authenticated → /Account/Login redirect, no email field).
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
  // Dismiss static success modal (Pitfall 3) → /Admin/ManagePackages?assessmentId={id}
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

// Selektor card Pengacakan (ManagePackages.cshtml:83-132). Hanya satu .card yang memuat teks ini.
function shuffleCard(page: Page) {
  const card = page.locator('.card', { hasText: 'Pengacakan Soal' });
  return {
    card,
    swQuestions: page.locator('#shuffleQuestions'),
    swOptions: page.locator('#shuffleOptions'),
    sizeWarning: page.locator('#shuffleSizeWarning'),
    lockBanner: card.locator('.alert-info', { hasText: 'Pengaturan pengacakan terkunci' }),
    postReminder: card.locator('.alert-warning', { hasText: 'Pre diatur OFF, Post masih ON' }),
    saveBtn: card.locator('button[type="submit"]:has-text("Simpan Pengaturan")'),
  };
}

test.describe('Phase 375 — Shuffle Toggle ManagePackages (UAT e2e)', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre375-${ts}.bak`;
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

  // ── Scenario 1: card render + saved-state default ON + Simpan → success PRG ──
  test('S1: card render + toggle default ON + Simpan → .alert-success PRG', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Pre Test OJT SHUF375 S1 ${Date.now()}`);
    const pkgId = await createDefaultPackage(page);
    await addQuestionViaForm(page, pkgId, {
      type: 'MultipleChoice', text: 'Soal SHUF375 S1 #1',
      options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 50,
    });
    await gotoMP(page, id);

    const s = shuffleCard(page);
    await expect(s.card).toBeVisible();
    await expect(s.swQuestions).toBeChecked();   // migration default ON (Phase 372)
    await expect(s.swOptions).toBeChecked();

    await s.swQuestions.uncheck();
    await s.saveBtn.click();
    await page.waitForLoadState('networkidle');
    // Endpoint UpdateShuffleSettings → TempData["Success"] → RedirectToAction (PRG).
    // .first() — page punya 2 alert-success simultan (global toast + inline TempData) → hindari strict-mode (pola examTypes.ts:243).
    await expect(page.locator('.alert-success', { hasText: 'berhasil disimpan' }).first()).toBeVisible();
  });

  // ── Scenario 2: lock — peserta started → banner + switch & saveBtn disabled ──
  test('S2: lock disabled + banner saat ada peserta started', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Pre Test OJT SHUF375 S2 ${Date.now()}`);
    // Lock precondition (controller:5393-5400 → ShuffleToggleRules.IsShuffleLocked):
    //   sibling session StartedAt != null → anyStarted=true → locked.
    await execSql(`UPDATE AssessmentSessions SET StartedAt = GETDATE() WHERE Id = ${id}`);
    await gotoMP(page, id);

    const s = shuffleCard(page);
    await expect(s.lockBanner).toBeVisible();
    await expect(s.swQuestions).toBeDisabled();
    await expect(s.swOptions).toBeDisabled();
    await expect(s.saveBtn).toBeDisabled();
  });

  // ── Scenario 3: reminder Pre/Post — Post ON linked Pre OFF → alert di Post, tak ada di Pre ──
  test('S3: reminder Pre OFF + Post ON muncul di Post, tidak di Pre', async ({ page }) => {
    test.setTimeout(150_000);
    // Pre session (akan di-set ShuffleQuestions=0).
    const preId = await createAssessmentArriveMP(page, `Pre Test OJT SHUF375 S3PRE ${Date.now()}`);
    // Post session (akan di-link ke Pre + ShuffleQuestions=1 + AssessmentType=PostTest).
    // doLogin=false — sudah login dari create Pre di atas (same page/context).
    const postId = await createAssessmentArriveMP(page, `Post Test OJT SHUF375 S3POST ${Date.now()}`, false);

    // Saved-state untuk reminder cond (view:117 — IsPostSession && PreShuffleQuestions==false && sqChecked):
    await execSql(`UPDATE AssessmentSessions SET ShuffleQuestions = 0 WHERE Id = ${preId}`);
    await execSql(
      `UPDATE AssessmentSessions SET ShuffleQuestions = 1, AssessmentType = 'PostTest', ` +
      `LinkedSessionId = ${preId} WHERE Id = ${postId}`
    );

    // Halaman Post → reminder visible.
    await gotoMP(page, postId);
    await expect(shuffleCard(page).postReminder).toBeVisible();

    // Halaman Pre → reminder TIDAK ada (no cascade).
    await gotoMP(page, preId);
    await expect(
      page.locator('.card', { hasText: 'Pengacakan Soal' })
        .locator('.alert-warning', { hasText: 'Pre diatur OFF' })
    ).toHaveCount(0);
  });

  // ── Scenario 4: warning §9 live-JS flip — multi-paket ukuran beda + Acak Soal OFF ──
  test('S4: warning §9 muncul saat Acak Soal OFF (multi-paket ukuran beda), hilang saat ON', async ({ page }) => {
    test.setTimeout(180_000);
    const id = await createAssessmentArriveMP(page, `Pre Test OJT SHUF375 S4 ${Date.now()}`);

    // Paket A = 2 soal, Paket B = 1 soal → hasMismatch=true, packagesWithQuestions=2 (multiPkg).
    const pkgA = await createDefaultPackage(page, 'Paket A');
    await addQuestionViaForm(page, pkgA, { type: 'MultipleChoice', text: 'S4 A1', options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 50 });
    await addQuestionViaForm(page, pkgA, { type: 'MultipleChoice', text: 'S4 A2', options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 50 });
    await gotoMP(page, id);

    // Buat Paket B inline (BUKAN createDefaultPackage — helper pakai .first() → kembalikan pkgA saat ada 2 paket).
    // Extract pkgB id dari link ManagePackageQuestions yang BUKAN pkgA.
    await page.locator('input[name="packageName"]').fill('Paket B');
    await page.locator('button[type="submit"]:has-text("Create Package")').click();
    await page.waitForLoadState('networkidle');
    const pkgIds = await page.locator('a[href*="ManagePackageQuestions"]').evaluateAll(
      (els) => els.map((e) => parseInt(new URL((e as HTMLAnchorElement).href).searchParams.get('packageId') ?? '0', 10))
    );
    const pkgB = pkgIds.find((x) => x > 0 && x !== pkgA);
    if (!pkgB) throw new Error(`S4: gagal extract pkgB id (ids=${pkgIds.join(',')}, pkgA=${pkgA})`);
    await addQuestionViaForm(page, pkgB, { type: 'MultipleChoice', text: 'S4 B1', options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 50 });
    await gotoMP(page, id);

    const s = shuffleCard(page);
    // JS (view:323-338): show = multiPkg && hasMismatch && !sq.checked → live toggle d-none on change.
    await s.swQuestions.uncheck();
    await expect(s.sizeWarning).toBeVisible();   // d-none lepas
    await s.swQuestions.check();
    await expect(s.sizeWarning).toBeHidden();     // d-none kembali (no reload — live JS)
  });

  // ── Scenario 5: hide — Manual entry / Proton Tahun 3 → card tidak dirender ──
  test('S5: hide card Pengacakan untuk Manual entry', async ({ page }) => {
    test.setTimeout(120_000);
    const id = await createAssessmentArriveMP(page, `Pre Test OJT SHUF375 S5 ${Date.now()}`);
    // HideShuffleToggle = (Category=="Assessment Proton" && TahunKe=="Tahun 3") || IsManualEntry.
    // Pakai IsManualEntry=1 (paling stabil; alternatif Proton-Th3 butuh Category+TahunKe).
    await execSql(`UPDATE AssessmentSessions SET IsManualEntry = 1 WHERE Id = ${id}`);
    await gotoMP(page, id);

    await expect(page.locator('.card', { hasText: 'Pengacakan Soal' })).toHaveCount(0);
  });
});
