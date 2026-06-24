// Phase 419 D-04.2 — Inject v32.2 x Section + opsi 5–6 (A–F, Phase 418) UAT real-browser @5277.
// Analog: tests/e2e/inject-assessment-395.spec.ts (loginAdmin + wizard + step-5 + commit + DB asserts) +
//   tests/e2e/inject-seakan-online-398.spec.ts (assertCertPdf + assertResultsPerSoal + seedOnlineSession
//   kolom penuh) + tests/e2e/section-lifecycle-419.spec.ts (beforeAll BACKUP / afterAll RESTORE+unlink ke
//   InstanceDefaultBackupPath, execSql/queryStr, createSection/PackageQuestion via SQL).
//
// GOAL (D-04.2): inject hasil manual (/Admin/InjectAssessment) commit KOHEREN (skor/cert/per-soal) +
//   preview == commit, SAAT soal authoring inject pakai 5–6 opsi (A–F) DAN ada sibling room ber-Section.
//   INVARIAN: paket buatan inject SELALU all-Lainnya (SectionId NULL) — diuji eksplisit (assertion 2).
//   Sibling ber-Section di-seed via SQL (kondisi "Section present") supaya inject TIDAK terganggu olehnya.
//
// Pre-req runtime: server localhost:5277 dari MAIN tree (Authentication__UseActiveDirectory=false; Razor
//   di-embed saat build — lesson Phase 354/392). Login admin@pertamina.com / 123456 (helpers/accounts.ts).
//   SQLEXPRESS lokal reachable (SQLBrowser up). Run: cd tests && npx playwright test
//   e2e/inject-section-419.spec.ts --workers=1
//
// KRITIS (anti silent-grade-0, Pitfall 4): test ini MENULIS DB (commit inject aktual + cert + audit +
//   seed sibling). #AnswersJson WAJIB terisi sebelum submit (window.injBuildWorkerAnswers) → POST tak
//   kosong. Skor pasca-commit WAJIB == skor preview (BUKAN 0). CLAUDE.md Seed Workflow: BACKUP beforeAll /
//   RESTORE afterAll (data prefix 'ZZ ...' temporary + local-only).
import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { accounts } from '../helpers/accounts';
import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';

test.describe.configure({ mode: 'serial' });
test.setTimeout(360_000);

const TS = Date.now();
const TITLE_PREFIX = 'ZZ Inject Sec419 ';
const SIBLING_TITLE = 'ZZ Online Sec419 ' + TS; // sibling ber-Section (kondisi "Section present")
let snapshotPath = '';
const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'inject-sec419-'));

// Peserta utama — coachee ber-NIP (controller inject skip null-NIP user). UserId hard-coded (pola 398).
const WORKER_EMAIL = accounts.coachee.email; // rino.prasetyo@pertamina.com
const RINO_ID = '4a624dbc-3241-4207-92d7-d1d5784c7137';

// 6 opsi (A–F) — teks unik supaya assertion per-soal Results bisa mencocokkan semuanya (Alpha..Foxtrot).
const OPTION_TEXTS = ['Alpha', 'Bravo', 'Charlie', 'Delta', 'Echo', 'Foxtrot'];
const CORRECT_INDEX = 4; // opsi E benar (single MC) → preview 100%

const sqlEsc = (s: string) => s.replace(/'/g, "''");

async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}

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

// Author 1 soal MC dengan 5–6 opsi (A–F) — inject form pakai #injAddOptionBtn (form mulai 4 baris A–D).
// optionTexts.length ∈ {5,6}. correctIndex menandai opsi benar (single MC). Tambah ke list via #injAddQuestionBtn.
async function authorDynamicMc(page: Page, text: string, optionTexts: string[], correctIndex: number) {
  await page.selectOption('#QuestionType', 'MultipleChoice');
  await page.fill('#questionText', text);
  // Tumbuhkan baris opsi 4 → optionTexts.length (#injAddOptionBtn, BUKAN #addOptionBtn — beda dari authoring).
  const addBtn = page.locator('#injAddOptionBtn');
  for (let i = 4; i < optionTexts.length; i++) await addBtn.click();
  const optIds = ['#option_A', '#option_B', '#option_C', '#option_D', '#option_E', '#option_F'];
  const correctIds = ['#correct_A', '#correct_B', '#correct_C', '#correct_D', '#correct_E', '#correct_F'];
  for (let i = 0; i < optionTexts.length; i++) await page.fill(optIds[i], optionTexts[i]);
  await page.check(correctIds[correctIndex]);
  await page.click('#injAddQuestionBtn');
}

// Seed 1 sesi ONLINE asli ber-Section (IsManualEntry=0) untuk Rino + paket + 2 Section + 2 soal ber-SectionId.
// Kolom AssessmentSessions di-mirror PERSIS dari inject-assessment-397/398 seedOnlineSession.
// PackageQuestions kolom NOT NULL: AssessmentPackageId, MaxCharacters, [Order], QuestionText, ScoreValue
//   (verified Migrations/ApplicationDbContextModelSnapshot.cs). SectionId = FK ke section (kondisi "Section present").
async function seedOnlineSiblingWithSections(title: string): Promise<{ sessionId: number; packageId: number }> {
  const titleSql = sqlEsc(title);
  const sql = `
SET NOCOUNT ON;
DECLARE @sid INT, @pid INT, @sec1 INT, @sec2 INT;

INSERT INTO AssessmentSessions
  (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor, Score,
   IsTokenRequired, AccessToken, CreatedAt, AllowAnswerReview, CompletedAt, IsPassed, PassPercentage,
   ElapsedSeconds, GenerateCertificate, AssessmentType, HasManualGrading, SamePackage, IsManualEntry,
   ShuffleOptions, ShuffleQuestions)
VALUES
  ('${RINO_ID}', '${titleSql}', 'Teknis', GETDATE(), 60, 'Completed', 100, 'green', 80,
   0, 'ONLINE', GETUTCDATE(), 1, GETDATE(), 1, 70,
   0, 0, 'PostTest', 0, 0, 0,
   0, 0);
SET @sid = SCOPE_IDENTITY();

INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
VALUES (@sid, N'Paket Sibling', 1, GETUTCDATE());
SET @pid = SCOPE_IDENTITY();

INSERT INTO AssessmentPackageSections (AssessmentPackageId, SectionNumber, Name, StartNewPage, ShuffleEnabled)
VALUES (@pid, 1, N'Pompa', 0, 1);
SET @sec1 = SCOPE_IDENTITY();
INSERT INTO AssessmentPackageSections (AssessmentPackageId, SectionNumber, Name, StartNewPage, ShuffleEnabled)
VALUES (@pid, 2, N'Valve', 1, 1);
SET @sec2 = SCOPE_IDENTITY();

INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters, SectionId)
VALUES (@pid, N'Soal sibling Pompa', 0, 10, 'MultipleChoice', 2000, @sec1);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters, SectionId)
VALUES (@pid, N'Soal sibling Valve', 1, 10, 'MultipleChoice', 2000, @sec2);

SELECT CAST(@sid AS VARCHAR(12)) + ',' + CAST(@pid AS VARCHAR(12));
`.trim();
  const tmp = path.join(tmpDir, `seed-sibling-${TS}.sql`);
  fs.writeFileSync(tmp, sql, 'utf8');
  await db.execScript(tmp);
  // Resolve id sibling (sesi + paket) untuk assertion 3 — query terpisah (execScript tak return value).
  const sessionId = await db.queryScalar(
    `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
  );
  const packageId = await db.queryScalar(
    `SELECT TOP 1 Id FROM AssessmentPackages WHERE AssessmentSessionId=${sessionId} ORDER BY Id DESC`
  );
  return { sessionId, packageId };
}

test.beforeAll(async () => {
  // CLAUDE.md Seed Workflow: BACKUP sebelum commit-test. SQL default backup dir
  // (C:\Temp\ diblokir service account — pola Phase 315/355/395/419-lifecycle).
  const dir = (await db.queryString("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"))
    .replace(/[\\/]+$/, '').replace(/\\/g, '/');
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre419inject-${ts}.bak`;
  await db.backup(snapshotPath);
  console.log(`[inject-sec419] snapshot: ${snapshotPath}`);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  let err: unknown = null;
  try {
    await db.restore(snapshotPath);
    try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    console.log(`[inject-sec419] RESTORE OK dari ${snapshotPath}`);
  } catch (e) {
    err = e;
    console.error('[inject-sec419] RESTORE GAGAL — restore manual via snapshot di atas:', e);
  } finally {
    try { fs.rmSync(tmpDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }
  if (err) throw err;
});

test.describe('Phase 419 D-04.2 — Inject v32.2 x Section + opsi 5–6', () => {
  test('inject opsi A–F koheren saat ada sibling ber-Section: skor==preview==100, own all-Lainnya, sibling Section utuh, cert+per-soal', async ({ page }) => {
    // ── 0. Seed sibling ber-Section (kondisi "Section present") ──
    const sibling = await seedOnlineSiblingWithSections(SIBLING_TITLE);
    expect(sibling.sessionId, 'sibling session ter-seed').toBeGreaterThan(0);
    expect(sibling.packageId, 'sibling package ter-seed').toBeGreaterThan(0);
    const siblingSectionsBefore = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId=${sibling.packageId}`
    );
    expect(siblingSectionsBefore, 'sibling punya 2 Section').toBe(2);

    // ── 1. Login + buka wizard inject ──
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = TITLE_PREFIX + TS;
    const titleSql = sqlEsc(title);
    const before = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );

    // ── Step 1: Setup (Category index 1, Title; AssessmentType biarkan 'Standard') ──
    await page.selectOption('#Category', { index: 1 });
    await page.fill('#Title', title);
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();

    // ── Step 2: pilih pekerja (rino, ber-NIP) ──
    await page.locator(
      `#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAIL}"] .user-checkbox`
    ).check();
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();

    // ── Step 3: authoring 1 soal MC 6-opsi (A–F, benar E) + 1 soal MC 5-opsi (A–E, benar E) ──
    await authorDynamicMc(page, 'Soal 6-opsi (A–F) — ' + title, OPTION_TEXTS, CORRECT_INDEX);
    await authorDynamicMc(page, 'Soal 5-opsi (A–E) — ' + title, OPTION_TEXTS.slice(0, 5), CORRECT_INDEX);
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();

    // ── Step 4: sertifikat Auto (nomor resmi) ──
    await page.check('#certModeAuto');
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Body')).toBeVisible();

    // ── Step 5: input-asli — pilih opsi benar E di kedua soal ──
    // step5RenderAnswerForm render radio per opsi dalam urutan DOM A→F (q.Options order). 2 soal:
    //   soal-1 (6 opsi A–F) → radio idx 0..5; soal-2 (5 opsi A–E) → radio idx 6..10. Benar = E.
    //   soal-1 E = nth(4); soal-2 E = nth(6 + 4) = nth(10).
    await expect(page.locator('#step5ModeManual')).toBeChecked();
    const radios = page.locator('#step5AnswerForm input[type="radio"]');
    await expect(radios, '6 + 5 = 11 radio (opsi A–F & A–E)').toHaveCount(11);
    await radios.nth(4).check();  // soal-1 opsi E (benar)
    await radios.nth(10).check(); // soal-2 opsi E (benar)

    // Anti silent-grade-0: payload builder berisi 1 worker, jawaban tak kosong.
    const answersRaw = await page.evaluate(() => JSON.stringify((window as any).injBuildWorkerAnswers()));
    expect(answersRaw).toBeTruthy();
    const parsed = JSON.parse(answersRaw);
    expect(Array.isArray(parsed)).toBe(true);
    expect(parsed.length, 'payload 1 worker').toBe(1);
    expect(parsed[0].answers.length, 'worker menjawab 2 soal (tak kosong)').toBe(2);
    expect(
      parsed[0].answers.every((a: any) => Array.isArray(a.selectedOptionTempIds) && a.selectedOptionTempIds.length === 1),
      'tiap jawaban memilih tepat 1 opsi'
    ).toBe(true);

    // ── Pratinjau Skor: 100% + Lulus, TANPA nomor sertifikat (KPB/ hanya muncul saat commit) ──
    await page.click('#step5PreviewBtn');
    await expect(page.locator('#step5PreviewResult')).toBeVisible({ timeout: 10_000 });
    const previewText = await page.locator('#step5PreviewResult').innerText();
    const m = previewText.match(/(\d+)%/);
    expect(m, 'preview menampilkan persentase').toBeTruthy();
    const previewScore = parseInt(m![1], 10);
    expect(previewScore, 'preview 100% (kedua soal benar)').toBe(100);
    await expect(page.locator('#step5PreviewResult')).toContainText('Lulus');
    await expect(page.locator('#step5PreviewResult')).not.toContainText('KPB/');

    // ── Step 6: commit aktual "seakan online" ──
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(
      page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first()
    ).toBeVisible({ timeout: 10_000 });

    // ── ASSERTION 1: sesi inject — Score == previewScore == 100 (anti silent-grade-0), IsManualEntry=1, Completed ──
    const after = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );
    expect(after, 'tepat 1 sesi inject ter-commit').toBe(before + 1);
    const sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(sessionId).toBeGreaterThan(0);
    const committedScore = await db.queryScalar(`SELECT Score FROM AssessmentSessions WHERE Id=${sessionId}`);
    expect(committedScore, 'commit score == preview (preview==commit)').toBe(previewScore);
    expect(committedScore, 'bukan silent-grade-0').toBe(100);
    const isManual = await db.queryScalar(`SELECT IsManualEntry FROM AssessmentSessions WHERE Id=${sessionId}`);
    expect(isManual, 'IsManualEntry=1').toBe(1);
    const status = await db.queryString(`SELECT Status FROM AssessmentSessions WHERE Id=${sessionId}`);
    expect(status, 'sesi Completed').toBe('Completed');

    // ── ASSERTION 2: INVARIAN — paket buatan inject SELALU all-Lainnya (SectionId NULL) ──
    const injectSectioned = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageQuestions pq
         INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
       WHERE ap.AssessmentSessionId = ${sessionId} AND pq.SectionId IS NOT NULL`
    );
    expect(injectSectioned, 'soal inject SELALU all-Lainnya (SectionId NULL)').toBe(0);
    // Sanity: paket inject memang punya 2 soal (memastikan COUNT=0 bukan karena tak ada soal).
    const injectTotalQ = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageQuestions pq
         INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
       WHERE ap.AssessmentSessionId = ${sessionId}`
    );
    expect(injectTotalQ, 'paket inject punya 2 soal').toBe(2);

    // ── ASSERTION 3: sibling Section utuh — inject tak mengganggu Section sibling ──
    const siblingSectionsAfter = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId=${sibling.packageId}`
    );
    expect(siblingSectionsAfter, 'sibling Section tak berubah (== 2)').toBe(siblingSectionsBefore);

    // ── ASSERTION 4: sertifikat — PDF nyata (200, application/pdf, > 1024 byte) ──
    const certResp = await page.request.get(`/CMP/CertificatePdf/${sessionId}`);
    expect(certResp.status(), 'cert PDF 200').toBe(200);
    expect(certResp.headers()['content-type'] ?? '', 'cert PDF MIME').toMatch(/^application\/pdf/i);
    expect((await certResp.body()).length, 'cert PDF > 1024 byte').toBeGreaterThan(1024);

    // ── ASSERTION 5: per-soal Results — marker Benar/Salah + semua 6 teks opsi (Alpha..Foxtrot) tampil ──
    await page.goto(`/CMP/Results/${sessionId}`);
    await expect(page.locator('text=Tinjauan jawaban tidak tersedia')).toHaveCount(0);
    await expect(
      page.locator('.bi-check-circle-fill, .bi-x-circle-fill').first(),
      'marker per-soal Benar/Salah'
    ).toBeVisible();
    for (const optText of OPTION_TEXTS) {
      await expect(
        page.getByText(optText, { exact: false }).first(),
        `teks opsi "${optText}" tampil di Results (huruf A–F dinamis koheren)`
      ).toBeVisible();
    }
  });
});
