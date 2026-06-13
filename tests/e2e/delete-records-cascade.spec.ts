// Phase 367 Plan 08 (Task 2) — UAT end-to-end hapus cascade dari tab Input Records (kasus Rino).
// Membuktikan lintas-stack RUNTIME (Pitfall 6 — render Razor partial + HTMX wiring TIDAK ketahuan dari build):
//   - tombol hapus online + manual + training → GET DeletePreview → modal "Konfirmasi Hapus — Cascade"
//   - modal menampilkan korban cascade (induk + turunan renewal) — preview, BUKAN blokir (L-03)
//   - "Hapus Semua" → flash HIJAU sukses + record + turunan terhapus dari DB (cascade), list re-fetch bersih (SC1)
//   - online session (kasus Rino, IsManualEntry=false) TAMPIL + bisa dihapus (SC4)
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev (InstanceDefaultBackupPath)
//   → seed renewal-chain via SQL → UAT browser → afterAll RESTORE (sukses ATAU gagal). Catat docs/SEED_JOURNAL.md.
//
// PRECONDITION: app running http://localhost:5277 (Authentication__UseActiveDirectory=false dotnet run) + DB lokal.
//   Jalankan: cd tests; npx playwright test delete-records-cascade --workers=1  (DB isolation — lesson local-e2e).
// Auth: admin@pertamina.com (123456) — dev lokal, JANGAN staging/prod.

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';

const WORKER_EMAIL = 'iwan3@pertamina.com';
const INDUK_TITLE = 'UAT367 Manual Induk Cascade';
const CHILD_JUDUL = 'UAT367 Renewal Anak';
const ONLINE_TITLE = 'UAT367 Online Rino 8hari';

let snapshotPath: string;
let workerId: string;
let indukId = 0;
let childId = 0;
let onlineId = 0;

test.describe.configure({ mode: 'serial' });

test.describe('Phase 367 — hapus cascade tab Input Records (UAT)', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre367-${ts}.bak`;
    await db.backup(snapshotPath);

    workerId = await db.queryString(`SELECT TOP 1 Id FROM AspNetUsers WHERE Email='${WORKER_EMAIL}'`);

    // Induk = assessment MANUAL (IsManualEntry=1) Completed → tampil di tab Input Records (manual branch).
    indukId = await db.queryScalar(`
      SET NOCOUNT ON;
      INSERT INTO AssessmentSessions (UserId, Title, Category, Status, AccessToken, Schedule, CompletedAt,
        IsManualEntry, IsPassed, GenerateCertificate, AssessmentType, DurationMinutes, Progress, PassPercentage,
        IsTokenRequired, HasManualGrading, SamePackage, AllowAnswerReview, ElapsedSeconds, Score, CreatedAt)
      VALUES ('${workerId}', '${INDUK_TITLE}', 'Test', 'Completed', '', '2026-01-01', '2026-01-01',
        1, 1, 1, 'Manual', 60, 100, 70, 0, 0, 0, 1, 0, 90, GETUTCDATE());
      SELECT CAST(SCOPE_IDENTITY() AS INT);`);

    // Anak = TrainingRecord yang me-renew induk (RenewsSessionId) → turunan cascade (L-03 IKUT terhapus).
    childId = await db.queryScalar(`
      SET NOCOUNT ON;
      INSERT INTO TrainingRecords (UserId, Judul, Tanggal, Status, RenewsSessionId)
      VALUES ('${workerId}', '${CHILD_JUDUL}', '2027-01-01', 'Valid', ${indukId});
      SELECT CAST(SCOPE_IDENTITY() AS INT);`);

    // Online (kasus Rino) = IsManualEntry=0, Completed, >7 hari lalu → tab tampil + deletable (SC4).
    onlineId = await db.queryScalar(`
      SET NOCOUNT ON;
      INSERT INTO AssessmentSessions (UserId, Title, Category, Status, AccessToken, Schedule, CompletedAt,
        IsManualEntry, IsPassed, GenerateCertificate, AssessmentType, DurationMinutes, Progress, PassPercentage,
        IsTokenRequired, HasManualGrading, SamePackage, AllowAnswerReview, ElapsedSeconds, Score, CreatedAt)
      VALUES ('${workerId}', '${ONLINE_TITLE}', 'Test', 'Completed', '', DATEADD(day,-30,GETDATE()), DATEADD(day,-30,GETDATE()),
        0, 1, 1, 'Standard', 60, 100, 70, 0, 0, 0, 1, 0, 88, GETUTCDATE());
      SELECT CAST(SCOPE_IDENTITY() AS INT);`);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    await db.restore(snapshotPath);
  });

  // Buka tab Input Records + expand worker iwan3.
  async function openWorkerTab(page: import('@playwright/test').Page) {
    await login(page, 'admin');
    await page.goto('/Admin/ManageAssessment?tab=training');
    // filter cari worker (form filterFormTraining) — fallback: search by NIP/nama. Pakai search box bila ada.
    const search = page.locator('#filterFormTraining input[name="search"], input[name="search"]').first();
    if (await search.count()) {
      await search.fill('iwan');
      await search.press('Enter');
      await page.waitForTimeout(1500);
    }
    // expand worker (chevron toggle) yang punya nama mengandung Iwan
    const row = page.locator('tr', { hasText: 'Iwan' }).first();
    await row.locator('button.toggle-chevron').click();
    await page.waitForTimeout(800);
  }

  test('SC4 + SC2 + SC1 — online tampil + preview cascade + hapus sukses (DB bersih, flash hijau)', async ({ page }) => {
    await openWorkerTab(page);

    // SC4: online (Rino, >7hari) tampil dengan badge "Assessment Online" + tombol hapus
    const onlineRow = page.locator('tr', { hasText: ONLINE_TITLE }).first();
    await expect(onlineRow).toBeVisible();
    await expect(onlineRow.locator('.btn-outline-danger')).toBeVisible();

    // SC2: klik hapus INDUK manual → modal preview cascade (induk + turunan), BUKAN blokir
    const indukRow = page.locator('tr', { hasText: INDUK_TITLE }).first();
    await indukRow.locator('.btn-outline-danger').click();
    const modal = page.locator('#cascadePreviewModal');
    await expect(modal).toBeVisible();
    await expect(modal).toContainText('Konfirmasi Hapus — Cascade');
    await expect(modal).toContainText('Tindakan ini tidak dapat dibatalkan');
    await expect(modal.locator('#cascade-preview-body')).toContainText(INDUK_TITLE);
    await expect(modal.locator('#cascade-preview-body')).toContainText(CHILD_JUDUL); // turunan renewal tampil
    await expect(modal.getByRole('button', { name: /Hapus Semua/ })).toBeVisible();

    // SC1: Hapus Semua → flash hijau + DB cascade bersih
    await modal.getByRole('button', { name: /Hapus Semua/ }).click();
    await expect(page.locator('#records-flash .alert-success')).toBeVisible({ timeout: 15_000 });

    expect(await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id=${indukId}`)).toBe(0);
    expect(await db.queryScalar(`SELECT COUNT(*) FROM TrainingRecords WHERE Id=${childId}`)).toBe(0);
  });

  test('SC4 — hapus online >7hari (kasus Rino) tuntas dari DB', async ({ page }) => {
    await openWorkerTab(page);
    const onlineRow = page.locator('tr', { hasText: ONLINE_TITLE }).first();
    await onlineRow.locator('.btn-outline-danger').click();
    const modal = page.locator('#cascadePreviewModal');
    await expect(modal).toBeVisible();
    await expect(modal.locator('#cascade-preview-body')).toContainText(ONLINE_TITLE);
    await modal.getByRole('button', { name: /Hapus Semua/ }).click();
    await expect(page.locator('#records-flash .alert-success')).toBeVisible({ timeout: 15_000 });
    expect(await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id=${onlineId}`)).toBe(0);
  });

  // SC3 (flash MERAH saat gagal) = kontrak honest-split (recordDeleteFailed→alert-danger) diuji unit
  // RecordCascadeUiTests + listener htmx:afterRequest di _TrainingRecordsTab; trigger gagal deterministik
  // di browser sulit (butuh constraint sengaja) → diverifikasi manual + unit. SC5 (guard dup + badge) =
  // DuplicateGuardTests (07) + BadgeRecomputeTests (03).
});
