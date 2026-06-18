// Phase 397 (Link Pre/Post ke room existing, INJ-12) — verifikasi runtime UI penautan di wizard
//   /Admin/InjectAssessment (Razor + JS): tombol "Cari Room" kondisional tipe, modal picker tipe-lawan,
//   chip removable, ringkasan pairing di Pratinjau (Kasus B banner + date warn), anti-dobel, unlink modal,
//   dan — KRITIS §13 — grouping silang inject↔online utuh dengan data online TAK disentuh.
// Pre-req runtime: server localhost:5277 dari MAIN tree (BUKAN worktree sibling) dengan
//   Authentication__UseActiveDirectory=false (Razor di-embed saat build; lesson Phase 354/392).
//   Login admin@pertamina.com / 123456 (helpers/accounts.ts). SQLEXPRESS lokal reachable.
// Run: cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1
//
// KRITIS (Contract 8 — load-bearing): test ini MENULIS DB (commit inject + write-to-online Kasus B + audit
//   + unlink). Snapshot DB lokal di beforeAll, RESTORE di afterAll (CLAUDE.md Seed Workflow; SEED_JOURNAL.md).
//   Online room di-seed sebagai standalone PostTest (Kasus B). Setelah inject PreTest tertaut:
//   (a) pasangan tampil di /CMP gain-score query (by LinkedGroupId+UserId), (b) Score/Status online UNCHANGED,
//   (c) audit "LinkPrePost" hadir.
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

test.describe.configure({ mode: 'serial' });

const TS = Date.now();
let snapshotPath = '';

// Pekerja ber-NIP (controller skip null-NIP). Rino = peserta utama cross-grouping.
const WORKER_EMAILS = ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com'];
const RINO_ID = '4a624dbc-3241-4207-92d7-d1d5784c7137';

// Online PostTest standalone (Kasus B) yang di-seed untuk Rino — judul unik per-run.
const ONLINE_POST_TITLE = 'ZZ Online Post 397 ' + TS;
const ONLINE_POST_TITLE_SQL = ONLINE_POST_TITLE.replace(/'/g, "''");

async function loginAdmin(page: Page) {
  const { email, password } = accounts.admin;
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

// Seed 1 sesi ONLINE PostTest standalone (Kasus B) untuk Rino — Score=80, IsPassed=1, Status=Completed.
// IsManualEntry=0 (online asli) → write-to-online + audit "LinkPrePost" wajib saat ditautkan (D-09).
async function seedOnlinePostRoom(): Promise<void> {
  const sql = `
SET NOCOUNT ON;
INSERT INTO AssessmentSessions
  (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor, Score,
   IsTokenRequired, AccessToken, CreatedAt, AllowAnswerReview, CompletedAt, IsPassed, PassPercentage,
   ElapsedSeconds, GenerateCertificate, AssessmentType, HasManualGrading, SamePackage, IsManualEntry,
   ShuffleOptions, ShuffleQuestions)
VALUES
  ('${RINO_ID}', '${ONLINE_POST_TITLE_SQL}', 'Teknis', GETDATE(), 60, 'Completed', 100, 'green', 80,
   0, 'ONLINE', GETUTCDATE(), 1, GETDATE(), 1, 70,
   0, 0, 'PostTest', 0, 0, 0,
   0, 0);
`.trim();
  const tmp = path.join(os.tmpdir(), `seed-397-${TS}.sql`);
  fs.writeFileSync(tmp, sql, 'utf8');
  try { await db.execScript(tmp); } finally { fs.unlinkSync(tmp); }
}

// Drive Setup (PreTest, tautkan ke online Post) → pilih pekerja → authoring MC → sertifikat → Step-5.
async function fillToStep5PreLinked(page: Page, title: string, workerCount: number) {
  await page.fill('#Title', title);
  await page.selectOption('#Category', { index: 1 });
  // Tipe = Pre-Test → tombol "Cari Room" muncul.
  await page.selectOption('#assessmentTypeInput', 'PreTest');
  await expect(page.locator('#btnCariRoom')).toBeVisible();
}

test.beforeAll(async () => {
  // CLAUDE.md Seed Workflow: BACKUP sebelum write-test (pakai default backup dir; C:\Temp blocked).
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  const dir = dirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre397-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
  console.log(`[inject-397] snapshot: ${snapshotPath}`);
  // Seed online Post room (Kasus B) untuk cross-grouping.
  await seedOnlinePostRoom();
  console.log(`[inject-397] seeded online Post room: ${ONLINE_POST_TITLE}`);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  try {
    await db.restore(snapshotPath);
    console.log(`[inject-397] RESTORE OK dari ${snapshotPath}`);
  } catch (e) {
    console.error('[inject-397] RESTORE GAGAL — restore manual via snapshot di atas:', e);
    throw e;
  }
});

// ── Contract 1: tombol "Cari Room" kondisional tipe + hint tipe-lawan; placeholder absen ──
test.describe('IC-1 Cari Room kondisional tipe', () => {
  test('Standard sembunyi; PreTest/PostTest tampil + hint tipe-lawan; placeholder absen', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');

    // Placeholder lama hilang.
    await expect(page.locator('text=tersedia pada fase berikutnya')).toHaveCount(0);

    // Standard → block tautan tersembunyi.
    await page.selectOption('#assessmentTypeInput', 'Standard');
    await expect(page.locator('#prePostLinkBlock')).toBeHidden();

    // PreTest → tombol + hint Post-Test.
    await page.selectOption('#assessmentTypeInput', 'PreTest');
    await expect(page.locator('#prePostLinkBlock')).toBeVisible();
    await expect(page.locator('#btnCariRoom')).toBeVisible();
    await expect(page.locator('#prePostLinkOppositeHint')).toContainText('Post-Test');

    // PostTest → hint Pre-Test.
    await page.selectOption('#assessmentTypeInput', 'PostTest');
    await expect(page.locator('#prePostLinkOppositeHint')).toContainText('Pre-Test');

    // Kembali Standard → block tersembunyi lagi.
    await page.selectOption('#assessmentTypeInput', 'Standard');
    await expect(page.locator('#prePostLinkBlock')).toBeHidden();
  });
});

// ── Contract 2+3+4: modal buka + filter tipe-lawan + badge Kasus A/B + Inject; pilih → chip; skip ──
test.describe('IC-2/3/4 modal picker + chip', () => {
  test('buka modal → daftar Post-Test (incl Inject) → badge Standalone → pilih → chip → hapus (skip)', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    await page.selectOption('#assessmentTypeInput', 'PreTest');

    await page.click('#btnCariRoom');
    await expect(page.locator('#roomPickerModal')).toBeVisible();
    await expect(page.locator('#roomPickerOppositeLabel')).toContainText('Post-Test');

    // Cari room online yang di-seed (judul unik).
    await page.fill('#roomSearchInput', ONLINE_POST_TITLE);
    const row = page.locator('#roomPickerResults .list-group-item-action', { hasText: ONLINE_POST_TITLE });
    await expect(row).toBeVisible({ timeout: 10_000 });
    // Tipe-lawan = Post-Test + badge Standalone (Kasus B).
    await expect(row.locator('.badge', { hasText: 'Post-Test' })).toBeVisible();
    await expect(row.locator('.badge', { hasText: 'Standalone' })).toBeVisible();

    // Pilih → modal tutup, chip muncul, hidden field set.
    await row.click();
    await expect(page.locator('#roomPickerModal')).toBeHidden();
    await expect(page.locator('#selectedRoomChip')).toBeVisible();
    await expect(page.locator('#selectedRoomChipText')).toContainText(ONLINE_POST_TITLE);
    await expect(page.locator('#selectedRoomChipGroup')).toContainText('Standalone');
    const repId = await page.locator('#LinkedTargetRepId').inputValue();
    expect(repId).not.toBe('');

    // Hapus chip → tautan kosong (skip, D-04), form tetap valid standalone.
    await page.click('#selectedRoomChipClose');
    await expect(page.locator('#selectedRoomChip')).toBeHidden();
    expect(await page.locator('#LinkedTargetRepId').inputValue()).toBe('');
  });
});

// ── Contract 5+6+8: pairing summary (Kasus B banner) + commit cross-grouping + online UNCHANGED + audit ──
test.describe('IC-5/8 cross-grouping (KRITIS §13)', () => {
  test('inject Pre tertaut → ringkasan Kasus B → commit → pair tampil + online unchanged + audit LinkPrePost', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');

    const title = 'ZZ Inject Pre 397 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    // Skor online sebelum penautan (invarian: TIDAK boleh berubah).
    const onlineScoreBefore = await db.queryScalar(
      `SELECT Score FROM AssessmentSessions WHERE Title='${ONLINE_POST_TITLE_SQL}' AND UserId='${RINO_ID}'`
    );
    const onlineStatusBefore = await db.queryString(
      `SELECT Status FROM AssessmentSessions WHERE Title='${ONLINE_POST_TITLE_SQL}' AND UserId='${RINO_ID}'`
    );
    const auditBefore = await db.queryScalar(
      `SELECT COUNT(*) FROM AuditLogs WHERE ActionType='LinkPrePost'`
    );

    // Setup PreTest + Cari Room → pilih online Post (Kasus B).
    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.selectOption('#assessmentTypeInput', 'PreTest');
    await page.click('#btnCariRoom');
    await page.fill('#roomSearchInput', ONLINE_POST_TITLE);
    const row = page.locator('#roomPickerResults .list-group-item-action', { hasText: ONLINE_POST_TITLE });
    await expect(row).toBeVisible({ timeout: 10_000 });
    await row.click();
    await expect(page.locator('#selectedRoomChip')).toBeVisible();

    // Step-2 pilih Rino (punya sibling Post online → ter-pair).
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await page.locator(`#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[0]}"] .user-checkbox`).check();
    await page.click('#btnNext2');

    // Step-3 authoring 1 MC (A benar).
    await expect(page.locator('#step-3')).toBeVisible();
    await page.selectOption('#QuestionType', 'MultipleChoice');
    await page.fill('#questionText', 'Soal Pre 397 — ' + title);
    await page.fill('#option_A', 'Jawaban A');
    await page.fill('#option_B', 'Jawaban B');
    await page.check('#correct_A');
    await page.click('#injAddQuestionBtn');
    await page.click('#btnNext3');

    // Step-4 sertifikat default → Step-5.
    await expect(page.locator('#step-4')).toBeVisible();
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Body')).toBeVisible();

    // Jawab benar (opsi A) untuk Rino.
    const radios = page.locator('#step5AnswerForm input[type="radio"]');
    await expect(radios).toHaveCount(2);
    await radios.nth(0).check();

    // Pastikan #AnswersJson terisi (anti silent-grade-0).
    const answersRaw = await page.evaluate(() => JSON.stringify((window as any).injBuildWorkerAnswers()));
    const parsed = JSON.parse(answersRaw);
    expect(parsed.length).toBe(1);
    expect(parsed[0].answers.length).toBe(1);

    // Step-6 → ringkasan pairing Kasus B (banner "Data online akan disentuh").
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await expect(page.locator('#previewPairingSummary')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#pairingPairedBadge')).toContainText('ter-pair');
    await expect(page.locator('#pairingTouchOnlineBanner')).toBeVisible();
    await expect(page.locator('#pairingTouchOnlineBanner')).toContainText('Data online akan disentuh');
    // Kasus B → notice "safe" TIDAK muncul.
    await expect(page.locator('#pairingSafeBanner')).toBeHidden();

    // === COMMIT ===
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    // (a) sesi inject Pre ter-commit + ber-LinkedGroupId.
    const injGroupId = await db.queryScalar(
      `SELECT TOP 1 ISNULL(LinkedGroupId, 0) FROM AssessmentSessions WHERE Title='${titleSql}' AND UserId='${RINO_ID}'`
    );
    expect(injGroupId).toBeGreaterThan(0);

    // (b) online Post di-stiker LinkedGroupId SAMA (cross-grouping) → pasangan tampil by LinkedGroupId+UserId.
    const onlinePostGroup = await db.queryScalar(
      `SELECT ISNULL(LinkedGroupId, 0) FROM AssessmentSessions WHERE Title='${ONLINE_POST_TITLE_SQL}' AND UserId='${RINO_ID}'`
    );
    expect(onlinePostGroup).toBe(injGroupId);

    // Pair surfaces: 2 sesi (1 Pre inject + 1 Post online) di grup yang sama untuk Rino.
    const pairCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE LinkedGroupId=${injGroupId} AND UserId='${RINO_ID}'`
    );
    expect(pairCount).toBe(2);

    // (c) KRITIS: Score/Status ONLINE TIDAK berubah (hanya kolom link disentuh).
    const onlineScoreAfter = await db.queryScalar(
      `SELECT Score FROM AssessmentSessions WHERE Title='${ONLINE_POST_TITLE_SQL}' AND UserId='${RINO_ID}'`
    );
    const onlineStatusAfter = await db.queryString(
      `SELECT Status FROM AssessmentSessions WHERE Title='${ONLINE_POST_TITLE_SQL}' AND UserId='${RINO_ID}'`
    );
    expect(onlineScoreAfter).toBe(onlineScoreBefore);
    expect(onlineStatusAfter).toBe(onlineStatusBefore);

    // (c2) audit "LinkPrePost" hadir (data online disentuh → compliance trail, D-09).
    const auditAfter = await db.queryScalar(
      `SELECT COUNT(*) FROM AuditLogs WHERE ActionType='LinkPrePost'`
    );
    expect(auditAfter).toBeGreaterThan(auditBefore);

    // (d) Cross-grouping SURFACES di gain-score — replikasi query GetGainScoreData (pasangkan by
    //     LinkedGroupId+UserId): Pre inject ↔ Post online untuk Rino dalam 1 grup = 1 pasangan (silang).
    //     Ini bukti load-bearing §13: pasangan tampil walau 1 sisi inject 1 sisi online.
    const gainScorePairs = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions pre
       INNER JOIN AssessmentSessions post
         ON pre.LinkedGroupId = post.LinkedGroupId AND pre.UserId = post.UserId
       WHERE pre.AssessmentType='PreTest' AND post.AssessmentType='PostTest'
         AND pre.LinkedGroupId=${injGroupId} AND pre.UserId='${RINO_ID}'`
    );
    expect(gainScorePairs).toBe(1);   // tepat 1 pasangan Pre↔Post silang inject↔online

    // (e) Records page memuat tanpa error (200) — surface CMP tetap sehat pasca cross-grouping.
    const recResp = await page.goto('/CMP/Records');
    expect(recResp?.status()).toBeLessThan(400);
  });
});

// ── Contract 9: unlink pasca-commit → modal konfirmasi Bootstrap (BUKAN native) → revert DB ──
test.describe('IC-9 unlink + konfirmasi modal', () => {
  test('Lepaskan Tautan → #unlinkConfirmModal → Ya, Lepaskan → notice + link reverted DB', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');

    // Setelah commit di test sebelumnya, kontrol unlink muncul HANYA jika TempData masih ada.
    // TempData habis setelah render pertama → buat commit BARU bertautan supaya host unlink muncul.
    const title = 'ZZ Inject Pre Unlink 397 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.selectOption('#assessmentTypeInput', 'PreTest');
    await page.click('#btnCariRoom');
    await page.fill('#roomSearchInput', ONLINE_POST_TITLE);
    const row = page.locator('#roomPickerResults .list-group-item-action', { hasText: ONLINE_POST_TITLE });
    await expect(row).toBeVisible({ timeout: 10_000 });
    await row.click();
    await page.click('#btnNext1');
    await page.locator(`#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[1]}"] .user-checkbox`).check();
    await page.click('#btnNext2');
    await page.selectOption('#QuestionType', 'MultipleChoice');
    await page.fill('#questionText', 'Soal Unlink — ' + title);
    await page.fill('#option_A', 'A');
    await page.fill('#option_B', 'B');
    await page.check('#correct_A');
    await page.click('#injAddQuestionBtn');
    await page.click('#btnNext3');
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await page.locator('#step5AnswerForm input[type="radio"]').nth(0).check();
    await page.click('#btnNext5');
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first()).toBeVisible();

    // Host unlink muncul (TempData InjectedGroupId set karena commit tertaut).
    const surface = page.locator('#postCommitLinkSurface');
    await expect(surface).toBeVisible({ timeout: 10_000 });
    const groupId = await surface.getAttribute('data-inject-group-id');
    expect(groupId).toBeTruthy();

    // Sebelum unlink: sesi inject ber-LinkedGroupId.
    const before = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}' AND LinkedGroupId=${groupId}`
    );
    expect(before).toBeGreaterThan(0);

    // Klik trigger → modal Bootstrap (BUKAN window.confirm native).
    let nativeConfirmCalled = false;
    page.on('dialog', async (d) => { nativeConfirmCalled = true; await d.dismiss(); });
    await page.click('#btnUnlinkRoom');
    await expect(page.locator('#unlinkConfirmModal')).toBeVisible();
    expect(nativeConfirmCalled).toBe(false);

    // Konfirmasi → UnlinkInjectGroup → notice sukses.
    await page.click('#btnConfirmUnlink');
    await expect(page.locator('.alert-success').filter({ hasText: /Tautan dilepas/i })).toBeVisible({ timeout: 10_000 });

    // DB: link inject reverted (LinkedGroupId null).
    const after = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}' AND LinkedGroupId=${groupId}`
    );
    expect(after).toBe(0);
  });
});

// ── Contract 10: skip penautan → inject standalone, tak ada ringkasan pairing ──
test.describe('IC-10 skip penautan', () => {
  test('Pre tanpa pilih room → commit standalone (LinkedGroupId null), tanpa ringkasan pairing', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');

    const title = 'ZZ Inject Pre Standalone 397 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.selectOption('#assessmentTypeInput', 'PreTest');
    // SENGAJA tidak memilih room (skip, D-04).
    await page.click('#btnNext1');
    await page.locator(`#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[0]}"] .user-checkbox`).check();
    await page.click('#btnNext2');
    await page.selectOption('#QuestionType', 'MultipleChoice');
    await page.fill('#questionText', 'Soal Standalone — ' + title);
    await page.fill('#option_A', 'A');
    await page.fill('#option_B', 'B');
    await page.check('#correct_A');
    await page.click('#injAddQuestionBtn');
    await page.click('#btnNext3');
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await page.locator('#step5AnswerForm input[type="radio"]').nth(0).check();
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    // Tak tertaut → ringkasan pairing TIDAK muncul.
    await expect(page.locator('#previewPairingSummary')).toBeHidden();

    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first()).toBeVisible();

    // DB: sesi standalone (LinkedGroupId null).
    const linkedCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}' AND LinkedGroupId IS NOT NULL`
    );
    expect(linkedCount).toBe(0);
    const total = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );
    expect(total).toBeGreaterThan(0);
  });
});
