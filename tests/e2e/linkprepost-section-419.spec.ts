// Phase 419 D-04.3 — LinkPrePost (397) x Section KOHERENSI UAT real-browser @5277.
// GOAL: menaut inject-Pre batch ke room ONLINE Post EXISTING yang ber-Section tetap BERHASIL;
//   online Post Score/Status/IsManualEntry TIDAK termutasi (invarian Phase 397); struktur Section
//   sisi Post tetap utuh; audit "LinkPrePost" tertulis.
//
// CATATAN scope (header skeleton): guard "struktur Section harus identik Pre<->Post" (semula D-02)
//   DI-DROP ke backlog 999.16 — paket inject SELALU all-Lainnya (skip-on-all-Lainnya = no-op).
//   D-04.3 = KOHERENSI (bukan blok): menaut inject-Pre ke room Post existing ber-Section sukses &
//   online Score/Status/IsManualEntry TIDAK termutasi (Phase 397) + struktur Section sisi Post utuh.
//
// Analog: tests/e2e/inject-assessment-397.spec.ts (loginAdmin, seedOnlinePostRoom Kasus B, picker
//   IC-5/8 #btnCariRoom→#roomSearchInput→row→chip, commit, online-unchanged + audit COUNT) +
//   tests/e2e/section-lifecycle-419.spec.ts (backup ke InstanceDefaultBackupPath / restore+unlink,
//   execSql/queryStr, createSection SQL).
//
// KRITIS (Seed Workflow CLAUDE.md): test ini MENULIS DB (commit inject + stiker Kasus B + audit).
//   Snapshot DB lokal di beforeAll (BACKUP ke InstanceDefaultBackupPath; C:\Temp blocked), RESTORE +
//   unlink .bak di afterAll. Online room di-seed ber-Section sebagai standalone PostTest (Kasus B).
//
// Pre-req runtime: server localhost:5277 dari MAIN tree (Razor di-embed saat build; lesson 354/392)
//   dengan Authentication__UseActiveDirectory=false. Login admin@pertamina.com / 123456
//   (helpers/accounts.ts). SQLEXPRESS lokal reachable. WAJIB --workers=1.
// Run: cd tests && npx playwright test e2e/linkprepost-section-419.spec.ts --workers=1
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

test.describe.configure({ mode: 'serial' });

const TS = Date.now();
let snapshotPath = '';

// Pekerja ber-NIP (controller skip null-NIP). Rino = peserta utama cross-grouping (sama dgn 397).
const WORKER_EMAILS = ['rino.prasetyo@pertamina.com'];
const RINO_ID = '4a624dbc-3241-4207-92d7-d1d5784c7137';

// Online PostTest standalone (Kasus B) BER-SECTION yang di-seed untuk Rino — judul unik per-run.
const ONLINE_POST_TITLE = 'ZZ Online Post Sec419 ' + TS;
const ONLINE_POST_TITLE_SQL = ONLINE_POST_TITLE.replace(/'/g, "''");

// Judul batch inject-Pre yang akan ditautkan ke room Post di atas.
const INJECT_PRE_TITLE = 'ZZ Inject Pre Sec419 ' + TS;
const INJECT_PRE_TITLE_SQL = INJECT_PRE_TITLE.replace(/'/g, "''");

// Diisi di beforeAll (seed) — id paket + sesi Post online ber-Section.
let postPkgId = 0;
let postSessionId = 0;

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

// Seed 1 sesi ONLINE PostTest standalone (Kasus B) BER-SECTION untuk Rino.
//   - AssessmentSessions: kolom-list MIRROR EXACT seedOnlinePostRoom 397 (Score=80, IsPassed=1,
//     Status=Completed, IsManualEntry=0, AssessmentType=PostTest, AccessToken='ONLINE').
//   - 1 AssessmentPackages on that session.
//   - 2 AssessmentPackageSections (1 'Proses' SNP=0 / 2 'Keselamatan' SNP=1, ShuffleEnabled=1).
//   - 2 PackageQuestions dengan SectionId di-set (1 per Section).
// IsManualEntry=0 (online asli) → saat ditautkan: stiker write-back + audit "LinkPrePost" wajib (D-09).
async function seedOnlinePostRoomWithSections(): Promise<void> {
  const sql = `
SET NOCOUNT ON;
DECLARE @sid INT;
DECLARE @pkg INT;
DECLARE @sec1 INT;
DECLARE @sec2 INT;

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
SET @sid = SCOPE_IDENTITY();

INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
VALUES (@sid, 'Paket A', 1, GETUTCDATE());
SET @pkg = SCOPE_IDENTITY();

INSERT INTO AssessmentPackageSections (AssessmentPackageId, SectionNumber, Name, StartNewPage, ShuffleEnabled)
VALUES (@pkg, 1, N'Proses', 0, 1);
SET @sec1 = SCOPE_IDENTITY();
INSERT INTO AssessmentPackageSections (AssessmentPackageId, SectionNumber, Name, StartNewPage, ShuffleEnabled)
VALUES (@pkg, 2, N'Keselamatan', 1, 1);
SET @sec2 = SCOPE_IDENTITY();

INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters, SectionId)
VALUES (@pkg, N'Soal Proses (Post online)', 1, 10, 'MultipleChoice', 2000, @sec1);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters, SectionId)
VALUES (@pkg, N'Soal Keselamatan (Post online)', 2, 10, 'MultipleChoice', 2000, @sec2);
`.trim();
  const tmp = path.join(os.tmpdir(), `seed-419lps-${TS}.sql`);
  fs.writeFileSync(tmp, sql, 'utf8');
  try { await db.execScript(tmp); } finally { fs.unlinkSync(tmp); }
}

test.beforeAll(async () => {
  // Seed Workflow: BACKUP sebelum write-test (default backup dir; C:\Temp blocked).
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  const dir = dirRaw.replace(/[\\/]+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre419lps-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
  console.log(`[linkprepost-419] snapshot: ${snapshotPath}`);

  // Seed online Post room ber-Section (Kasus B) untuk cross-grouping.
  await seedOnlinePostRoomWithSections();
  console.log(`[linkprepost-419] seeded online Post room ber-Section: ${ONLINE_POST_TITLE}`);

  // Tangkap id sesi + paket Post (join AssessmentPackages→AssessmentSessions by title).
  postSessionId = await db.queryScalar(
    `SELECT Id FROM AssessmentSessions WHERE Title='${ONLINE_POST_TITLE_SQL}' AND UserId='${RINO_ID}'`
  );
  postPkgId = await db.queryScalar(
    `SELECT TOP 1 p.Id FROM AssessmentPackages p
     INNER JOIN AssessmentSessions s ON s.Id = p.AssessmentSessionId
     WHERE s.Title='${ONLINE_POST_TITLE_SQL}' AND s.UserId='${RINO_ID}'`
  );
  console.log(`[linkprepost-419] Post sessionId=${postSessionId} pkgId=${postPkgId}`);
  expect(postSessionId, 'Post sessionId ter-seed').toBeGreaterThan(0);
  expect(postPkgId, 'Post packageId ter-seed').toBeGreaterThan(0);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  let err: unknown = null;
  try {
    await db.restore(snapshotPath);
    console.log(`[linkprepost-419] RESTORE OK dari ${snapshotPath}`);
    try { fs.unlinkSync(snapshotPath); } catch { /* best-effort cleanup */ }
  } catch (e) {
    err = e;
    console.error('[linkprepost-419] RESTORE GAGAL — restore manual via snapshot di atas:', e);
  }
  if (err) throw err;
});

test.describe('Phase 419 D-04.3 — LinkPrePost 397 x Section (koherensi; guard di backlog 999.16)', () => {
  test('link inject-Pre -> room Post ber-Section: sukses, online untouched, Section utuh', async ({ page }) => {
    test.setTimeout(360_000);

    // ── BEFORE: tangkap invarian online + struktur Section + audit baseline ──
    const onlineScoreBefore = await db.queryScalar(
      `SELECT Score FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    const onlineStatusBefore = await db.queryString(
      `SELECT Status FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    const onlineManualBefore = await db.queryScalar(
      `SELECT CAST(IsManualEntry AS INT) FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    const sectionCountBefore = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId=${postPkgId}`
    );
    const sectionedQBefore = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageQuestions WHERE AssessmentPackageId=${postPkgId} AND SectionId IS NOT NULL`
    );
    const auditBefore = await db.queryScalar(
      `SELECT COUNT(*) FROM AuditLogs WHERE ActionType='LinkPrePost'`
    );
    expect(onlineScoreBefore, 'baseline online Score=80').toBe(80);
    expect(onlineStatusBefore, 'baseline online Status=Completed').toBe('Completed');
    expect(onlineManualBefore, 'baseline online IsManualEntry=0').toBe(0);
    expect(sectionCountBefore, 'baseline 2 Section di paket Post').toBe(2);
    expect(sectionedQBefore, 'baseline 2 soal ber-Section di paket Post').toBe(2);

    // ── Wizard: Step-1 Setup PreTest + tautkan ke room Post ber-Section (picker IC-5) ──
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');

    await page.fill('#Title', INJECT_PRE_TITLE);
    await page.selectOption('#Category', { index: 1 });
    await page.selectOption('#assessmentTypeInput', 'PreTest');
    await expect(page.locator('#prePostLinkBlock')).toBeVisible();

    // Buka modal picker (Bootstrap, BUKAN native dialog) → cari room Post → pilih → chip.
    await page.click('#btnCariRoom');
    await expect(page.locator('#roomPickerModal')).toBeVisible();
    await page.fill('#roomSearchInput', ONLINE_POST_TITLE);
    const row = page.locator('#roomPickerResults .list-group-item-action', { hasText: ONLINE_POST_TITLE });
    await expect(row).toBeVisible({ timeout: 10_000 });
    await row.click();
    await expect(page.locator('#roomPickerModal')).toBeHidden();
    await expect(page.locator('#selectedRoomChip')).toBeVisible();
    await expect(page.locator('#selectedRoomChipText')).toContainText(ONLINE_POST_TITLE);
    const repId = await page.locator('#LinkedTargetRepId').inputValue();
    expect(repId, 'LinkedTargetRepId ter-set dari chip').not.toBe('');

    // ── Step-2: pilih Rino (punya sibling Post online → ter-pair) ──
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await page.locator(
      `#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[0]}"] .user-checkbox`
    ).check();
    await page.click('#btnNext2');

    // ── Step-3: authoring 1 MC all-Lainnya (A benar) — paket inject SELALU all-Lainnya ──
    await expect(page.locator('#step-3')).toBeVisible();
    await page.selectOption('#QuestionType', 'MultipleChoice');
    await page.fill('#questionText', 'Soal Pre Sec419 — ' + INJECT_PRE_TITLE);
    await page.fill('#option_A', 'Jawaban A');
    await page.fill('#option_B', 'Jawaban B');
    await page.check('#correct_A');
    await page.click('#injAddQuestionBtn');
    await page.click('#btnNext3');

    // ── Step-4: sertifikat default → Step-5 ──
    await expect(page.locator('#step-4')).toBeVisible();
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Body')).toBeVisible();

    // ── Step-5: jawab benar (opsi A pertama) untuk Rino ──
    const radios = page.locator('#step5AnswerForm input[type="radio"]');
    await expect(radios).toHaveCount(2);
    await radios.nth(0).check();
    await page.click('#btnNext5');

    // ── Step-6: ringkasan pairing Kasus B (online akan disentuh = stiker LinkedGroupId) ──
    await expect(page.locator('#step-6')).toBeVisible();
    await expect(page.locator('#previewPairingSummary')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#pairingTouchOnlineBanner')).toBeVisible();

    // === COMMIT ===
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(
      page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first()
    ).toBeVisible({ timeout: 10_000 });

    // ── ASSERTIONS ──

    // (a) link sukses: injected Pre LinkedGroupId > 0 AND == online Post LinkedGroupId; grup Rino COUNT==2.
    const injGroupId = await db.queryScalar(
      `SELECT TOP 1 ISNULL(LinkedGroupId, 0) FROM AssessmentSessions WHERE Title='${INJECT_PRE_TITLE_SQL}' AND UserId='${RINO_ID}'`
    );
    expect(injGroupId, 'inject Pre ber-LinkedGroupId > 0').toBeGreaterThan(0);
    const onlinePostGroup = await db.queryScalar(
      `SELECT ISNULL(LinkedGroupId, 0) FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    expect(onlinePostGroup, 'online Post LinkedGroupId == inject (cross-grouping)').toBe(injGroupId);
    const pairCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE LinkedGroupId=${injGroupId} AND UserId='${RINO_ID}'`
    );
    expect(pairCount, 'tepat 2 sesi (1 Pre inject + 1 Post online) dalam grup Rino').toBe(2);

    // (b) online UNCHANGED: Score / Status / IsManualEntry tak termutasi (invarian Phase 397).
    const onlineScoreAfter = await db.queryScalar(
      `SELECT Score FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    const onlineStatusAfter = await db.queryString(
      `SELECT Status FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    const onlineManualAfter = await db.queryScalar(
      `SELECT CAST(IsManualEntry AS INT) FROM AssessmentSessions WHERE Id=${postSessionId}`
    );
    expect(onlineScoreAfter, 'online Score tak berubah').toBe(onlineScoreBefore);
    expect(onlineStatusAfter, 'online Status tak berubah').toBe(onlineStatusBefore);
    expect(onlineManualAfter, 'online IsManualEntry tak berubah (tetap 0)').toBe(onlineManualBefore);

    // (c) Section intact: 2 Section + 2 soal ber-Section di paket Post tetap utuh.
    const sectionCountAfter = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId=${postPkgId}`
    );
    const sectionedQAfter = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageQuestions WHERE AssessmentPackageId=${postPkgId} AND SectionId IS NOT NULL`
    );
    expect(sectionCountAfter, 'jumlah Section paket Post tetap 2').toBe(2);
    expect(sectionedQAfter, 'jumlah soal ber-Section paket Post tetap 2').toBe(2);

    // (d) audit: COUNT AuditLogs LinkPrePost meningkat (data online disentuh → compliance trail D-09).
    const auditAfter = await db.queryScalar(
      `SELECT COUNT(*) FROM AuditLogs WHERE ActionType='LinkPrePost'`
    );
    expect(auditAfter, 'audit LinkPrePost bertambah').toBeGreaterThan(auditBefore);
  });
});
