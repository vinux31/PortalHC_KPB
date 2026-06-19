// Phase 398 (INJ-13: Test + UAT "seakan online") — DOWNSTREAM PARITY konsolidasi.
//   Spec 395/396/397 berhenti di assertion COMMIT (DB Score == preview, anti silent-grade-0).
//   Plan 398-01 MELANJUTKAN setelah commit: navigasi ke 4 surface downstream pekerja —
//   /CMP/RecordsWorkerDetail (label tipe Assessment, RecordType "Assessment Online"),
//   /CMP/Results/{id} (per-soal Benar/Salah + Analisis Elemen Teknis + essay no-pending),
//   /CMP/CertificatePdf/{id} (PDF nyata) — membuktikan hasil inject TAK BISA DIBEDAKAN dari online.
//
// Pre-req runtime: server localhost:5277 dari MAIN tree dengan Authentication__UseActiveDirectory=false
//   (Razor di-embed saat build — AddControllersWithViews tanpa RuntimeCompilation; lesson Phase 354/392).
//   Login admin@pertamina.com / 123456 (helpers/accounts.ts). SQLEXPRESS lokal reachable (SQLBrowser up).
// Run: cd tests && npx playwright test e2e/inject-seakan-online-398.spec.ts --workers=1
//
// KRITIS (CLAUDE.md Seed Workflow): test ini MENULIS DB (commit inject + cert + audit + seed online).
//   snapshot DB lokal di beforeAll, RESTORE di afterAll (catat docs/SEED_JOURNAL.md). Data prefix
//   'ZZ ... 398' (LIKE 'ZZ %398%') — Plan 398-02 verifikasi COUNT residu == 0 pasca-restore.
//   Judul SENGAJA tanpa kata "Inject"/"Manual" agar assertion parity row-level (tak ada penanda
//   inject↔online) tidak salah-positif oleh nama data itu sendiri.
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';
import ExcelJS from 'exceljs';

test.describe.configure({ mode: 'serial' });

const TS = Date.now();
let snapshotPath = '';
const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'inject-398-'));

// Pekerja yang WAJIB ber-NIP (controller skip null-NIP user) — pakai email ber-NIP pasti.
const WORKER_EMAILS = ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com'];
const RINO_ID = '4a624dbc-3241-4207-92d7-d1d5784c7137'; // peserta utama linked + side-by-side
// Sesi online (IsManualEntry=0) di-seed untuk Rino — judul unik per-run (LIKE 'ZZ %398%').
const ONLINE_POST_TITLE = 'ZZ Online Post 398 ' + TS;
const ONLINE_SIBLING_TITLE = 'ZZ Online Sibling 398 ' + TS;

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

// Author 1 soal MC (A benar) — teks unik untuk answer-pattern deterministik.
async function authorMcQuestion(page: Page, text: string) {
  await page.selectOption('#QuestionType', 'MultipleChoice');
  await page.fill('#questionText', text);
  await page.fill('#option_A', 'Jawaban A');
  await page.fill('#option_B', 'Jawaban B');
  await page.check('#correct_A');
  await page.click('#injAddQuestionBtn');
}

// Varian: MC + ElemenTeknis terisi → Results render card "Analisis Elemen Teknis" (D-02c).
async function authorMcQuestionWithElemen(page: Page, text: string, elemen: string) {
  await page.selectOption('#QuestionType', 'MultipleChoice');
  await page.fill('#questionText', text);
  await page.fill('#option_A', 'Jawaban A');
  await page.fill('#option_B', 'Jawaban B');
  await page.check('#correct_A');
  await page.fill('#elemenTeknis', elemen);
  await page.click('#injAddQuestionBtn');
}

// Author 1 soal Essay (rubrik wajib).
async function authorEssayQuestion(page: Page, text: string) {
  await page.selectOption('#QuestionType', 'Essay');
  await page.fill('#questionText', text);
  await page.fill('#rubrik', 'Rubrik: poin kunci jawaban essay.');
  await page.click('#injAddQuestionBtn');
}

// Pilih 1 pekerja (by email) di Langkah 2.
async function pickWorker(page: Page, email: string) {
  await page.locator(`#userCheckboxContainer .user-check-item[data-email="${email}"] .user-checkbox`).check();
}

// Set sertifikat = Generate Otomatis (Nomor Resmi) di Langkah 4.
async function setCertAuto(page: Page) {
  await page.check('#certModeAuto');
}

// Resolve UserId pemilik sesi (untuk navigasi RecordsWorkerDetail admin→worker).
async function ownerId(sessionId: number): Promise<string> {
  return db.queryString(`SELECT TOP 1 UserId FROM AssessmentSessions WHERE Id=${sessionId}`);
}

// === Assertion bersama: sesi (inject) tampil "seakan online" di 4 surface downstream ===
//  - RecordsWorkerDetail: baris ber-title, tipe Assessment (RecordType "Assessment Online"),
//    link "Lihat Hasil" → Results. Penanda inject↔online TAK ADA (row-level no "Manual"/"Inject").
//  - Results: no empty-state, ≥1 badge Benar/Salah.
//  - CertificatePdf: 200 + application/pdf + >1024 byte.
async function assertRecordsRowSeakanOnline(page: Page, uid: string, title: string) {
  await page.goto(`/CMP/RecordsWorkerDetail?workerId=${uid}`);
  // Label "Assessment Online" hadir di halaman (stat heading + RecordType internal).
  await expect(page.getByText('Assessment Online').first()).toBeVisible();
  const row = page.locator('tbody tr', { hasText: title });
  await expect(row.first()).toBeVisible();
  // Bukti server-side RecordType == "Assessment Online" (data-type = RecordType.ToLower()).
  await expect(row.first()).toHaveAttribute('data-type', 'assessment online');
  // Tipe badge sama persis dgn online ("Assessment") + jalur Results ("Lihat Hasil").
  await expect(row.first()).toContainText('Assessment');
  await expect(row.first()).toContainText('Lihat Hasil');
  // Indistinguishable: baris inject tak punya penanda yang membedakan dari online.
  await expect(row.first()).not.toContainText('Manual');
  await expect(row.first()).not.toContainText('Inject');
}

async function assertResultsPerSoal(page: Page, sessionId: number, opts: { elemen?: boolean; noEssayPending?: boolean } = {}) {
  await page.goto(`/CMP/Results/${sessionId}`);
  await expect(page.locator('text=Tinjauan jawaban tidak tersedia')).toHaveCount(0);
  await expect(page.locator('.bi-check-circle-fill, .bi-x-circle-fill').first()).toBeVisible();
  if (opts.elemen) {
    await expect(page.getByText('Analisis Elemen Teknis')).toBeVisible();
  }
  if (opts.noEssayPending) {
    await expect(page.locator('.bi-hourglass-split')).toHaveCount(0);
  }
}

async function assertCertPdf(page: Page, sessionId: number) {
  const resp = await page.request.get(`/CMP/CertificatePdf/${sessionId}`);
  expect(resp.status()).toBe(200);
  expect(resp.headers()['content-type']).toMatch(/^application\/pdf/i);
  expect((await resp.body()).length).toBeGreaterThan(1024);
}

// ── Excel helpers (mode Import Excel, copy pola inject-excel-396.spec.ts) ──
async function switchToExcel(page: Page) {
  await page.check('#step5MethodExcel');
  await expect(page.locator('#step5ExcelPanel')).toBeVisible();
  await expect(page.locator('#step5FormPath')).toBeHidden();
}

// NIP pekerja — tabel Identity di project ini = "Users" (BUKAN AspNetUsers).
async function workerNip(email: string): Promise<string> {
  return db.queryString(`SELECT TOP 1 NIP FROM Users WHERE Email='${email.replace(/'/g, "''")}'`);
}

// Bangun file Excel upload FRESH (exceljs) — sheet "Jawaban", row1 header (di-skip parser),
// row2 worker (col1=NIP, col2=Nama, col3+=jawaban via row2 map).
async function buildUploadXlsx(outTag: string, nip: string, row2: { [col: number]: string | number }): Promise<string> {
  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet('Jawaban');
  ws.getRow(1).values = ['NIP', 'Nama', 'Soal 1 (MC 1 huruf)', 'Soal 2 Skor (0..10)', 'Soal 2 Teks (opsional)'];
  ws.getRow(2).getCell(1).value = nip;
  ws.getRow(2).getCell(2).value = 'Rino Prasetyo';
  for (const c of Object.keys(row2)) {
    ws.getRow(2).getCell(parseInt(c, 10)).value = row2[parseInt(c, 10) as any] as any;
  }
  const out = path.join(tmpDir, `${outTag}.xlsx`);
  await wb.xlsx.writeFile(out);
  return out;
}

// Seed 1 sesi ONLINE asli (IsManualEntry=0) untuk Rino — Completed, Score=80, IsPassed=1.
// Dipakai sebagai sibling side-by-side (Standard) / pasangan Post (PostTest) untuk parity Records.
// Pola execScript penuh kolom (copy inject-assessment-397.spec.ts:48-65).
async function seedOnlineSession(title: string, assessmentType: string): Promise<void> {
  const titleSql = title.replace(/'/g, "''");
  const sql = `
SET NOCOUNT ON;
INSERT INTO AssessmentSessions
  (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor, Score,
   IsTokenRequired, AccessToken, CreatedAt, AllowAnswerReview, CompletedAt, IsPassed, PassPercentage,
   ElapsedSeconds, GenerateCertificate, AssessmentType, HasManualGrading, SamePackage, IsManualEntry,
   ShuffleOptions, ShuffleQuestions)
VALUES
  ('${RINO_ID}', '${titleSql}', 'Teknis', GETDATE(), 60, 'Completed', 100, 'green', 80,
   0, 'ONLINE', GETUTCDATE(), 1, GETDATE(), 1, 70,
   0, 0, '${assessmentType}', 0, 0, 0,
   0, 0);
`.trim();
  const tmp = path.join(tmpDir, `seed-${assessmentType}-${TS}.sql`);
  fs.writeFileSync(tmp, sql, 'utf8');
  await db.execScript(tmp);
}

test.beforeAll(async () => {
  // CLAUDE.md Seed Workflow: BACKUP sebelum commit-test. Pakai SQL default backup dir
  // (C:\Temp\ diblokir service account — pola Phase 315/355/395).
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  const dir = dirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre398-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
  console.log(`[inject-398] snapshot: ${snapshotPath}`);
  // Seed sesi ONLINE asli (IsManualEntry=0) untuk Rino — pasangan Post (linked) + sibling (side-by-side).
  await seedOnlineSession(ONLINE_POST_TITLE, 'PostTest');
  await seedOnlineSession(ONLINE_SIBLING_TITLE, 'Standard');
  console.log(`[inject-398] seeded online: ${ONLINE_POST_TITLE} + ${ONLINE_SIBLING_TITLE}`);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  try {
    await db.restore(snapshotPath);
    console.log(`[inject-398] RESTORE OK dari ${snapshotPath}`);
  } catch (e) {
    console.error('[inject-398] RESTORE GAGAL — restore manual via snapshot di atas:', e);
    throw e;
  } finally {
    try { fs.rmSync(tmpDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }
});

// ─────────────────────────────────────────────────────────────────────────────
// SKENARIO 1 — Form + essay + ElemenTeknis: inject → commit → 4 surface (D-02 a/b/c/d)
// ─────────────────────────────────────────────────────────────────────────────
test.describe('INJ-13 Form: 4 surface seakan-online', () => {
  test('Form (MC+ElemenTeknis + essay) → commit → Records label + Results per-soal+ET + essay Completed + cert PDF', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Form 398 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    // Wizard: Setup → 1 pekerja → authoring (MC ber-ElemenTeknis + essay) → cert auto → Langkah 5.
    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await pickWorker(page, WORKER_EMAILS[0]);
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();
    await authorMcQuestionWithElemen(page, 'Soal MC 1 — ' + title, 'Elemen A');
    await authorEssayQuestion(page, 'Soal Essay 2 — ' + title);
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();
    await setCertAuto(page);
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Body')).toBeVisible();

    // Step-5 input-asli pekerja 1: MC opsi A (benar) + essay teks + skor penuh (10).
    await expect(page.locator('#step5ModeManual')).toBeChecked();
    await page.locator('#step5AnswerForm input[type="radio"]').nth(0).check(); // MC opsi A benar
    await page.locator('#step5AnswerForm textarea').fill('Jawaban essay lengkap dengan poin kunci.');
    await page.locator('#step5AnswerForm input[type="number"]').fill('10'); // skor essay penuh

    // Anti silent-grade-0: payload builder berisi 1 worker × 2 soal terjawab.
    const answersRaw = await page.evaluate(() => JSON.stringify((window as any).injBuildWorkerAnswers()));
    const parsed = JSON.parse(answersRaw);
    expect(parsed.length).toBe(1);
    expect(parsed[0].answers.length).toBe(2);

    // Commit aktual "seakan online".
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(sessionId).toBeGreaterThan(0);

    // D-04 essay §13: status Completed (BUKAN "Menunggu Penilaian").
    const status = await db.queryString(`SELECT Status FROM AssessmentSessions WHERE Id=${sessionId}`);
    expect(status).toBe('Completed');

    const uid = await ownerId(sessionId);

    // D-02a Records label + D-02b/c Results per-soal + ET + no-essay-pending + D-02d cert PDF.
    await assertRecordsRowSeakanOnline(page, uid, title);
    await assertResultsPerSoal(page, sessionId, { elemen: true, noEssayPending: true });
    await assertCertPdf(page, sessionId);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// SKENARIO 2 — Auto-generate: tembus surface (D-04 representatif)
// ─────────────────────────────────────────────────────────────────────────────
test.describe('INJ-13 Auto-generate: tembus surface', () => {
  test('auto-gen target 100 → preview Lulus no-cert# → commit → Records label + Results per-soal + cert PDF', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ AutoGen 398 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await pickWorker(page, WORKER_EMAILS[0]);
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();
    await authorMcQuestion(page, 'Soal MC 1 — ' + title);
    await authorMcQuestion(page, 'Soal MC 2 — ' + title);
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();
    await setCertAuto(page);
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Body')).toBeVisible();

    // Mode auto-generate target 100 → 2 MC equal-weight → keduanya benar → 100% Lulus.
    await page.check('#step5ModeAuto');
    await expect(page.locator('#step5AutoBody')).toBeVisible();
    await page.fill('#step5TargetScore', '100');
    await page.click('#step5PreviewBtn');
    await expect(page.locator('#step5PreviewResult')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#step5PreviewResult')).toContainText('100%');
    await expect(page.locator('#step5PreviewResult')).toContainText('Lulus');
    await expect(page.locator('#step5PreviewResult')).not.toContainText('KPB/'); // no cert# di preview

    // Anti silent-grade-0: payload 1 worker.
    const parsed = JSON.parse(await page.evaluate(() => JSON.stringify((window as any).injBuildWorkerAnswers())));
    expect(parsed.length).toBe(1);

    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(sessionId).toBeGreaterThan(0);
    const uid = await ownerId(sessionId);

    await assertRecordsRowSeakanOnline(page, uid, title);
    await assertResultsPerSoal(page, sessionId);
    await assertCertPdf(page, sessionId);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// SKENARIO 3 — Excel: tembus surface (D-04 representatif + essay §13)
// ─────────────────────────────────────────────────────────────────────────────
test.describe('INJ-13 Excel: tembus surface', () => {
  test('Excel (MC A + essay skor 10) → commit → Records label + Results per-soal + essay Completed + cert PDF', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Excel 398 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await pickWorker(page, WORKER_EMAILS[0]);
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();
    await authorMcQuestion(page, 'Soal MC 1 — ' + title);
    await authorEssayQuestion(page, 'Soal Essay 2 — ' + title);
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();
    await setCertAuto(page);
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await expect(page.locator('#step5Body')).toBeVisible();

    // Mode Import Excel: build file fresh (MC col3='A' benar, essay col4=10 skor penuh) → upload.
    await switchToExcel(page);
    const nip = await workerNip(WORKER_EMAILS[0]);
    const filled = await buildUploadXlsx('ok-398', nip, { 3: 'A', 4: 10 });
    await page.setInputFiles('#step5ExcelFile', filled);
    await page.click('#btnUploadExcel');
    await expect(page.locator('#step5ExcelPreview')).toBeVisible({ timeout: 15_000 });

    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(sessionId).toBeGreaterThan(0);
    // Essay §13: Status Completed (Excel skor tanpa teks D-05 tetap finalize).
    const status = await db.queryString(`SELECT Status FROM AssessmentSessions WHERE Id=${sessionId}`);
    expect(status).toBe('Completed');
    const uid = await ownerId(sessionId);

    await assertRecordsRowSeakanOnline(page, uid, title);
    await assertResultsPerSoal(page, sessionId, { noEssayPending: true });
    await assertCertPdf(page, sessionId);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// SKENARIO 4 — Pre/Post linked silang inject↔online (D-04 +Pre/Post)
//   Fokus DOWNSTREAM: inject Pre tertaut ke online Post → KEDUA anggota pasangan
//   tampil "Assessment Online" di Records Rino (397 sudah buktikan write-to-online).
// ─────────────────────────────────────────────────────────────────────────────
test.describe('INJ-13 Pre/Post linked silang inject↔online', () => {
  test('inject Pre tertaut ke online Post → cross-group LinkedGroupId + kedua baris Assessment Online di Records', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Pre 398 ' + TS;
    const titleSql = title.replace(/'/g, "''");
    const onlineSql = ONLINE_POST_TITLE.replace(/'/g, "''");

    // Setup PreTest + Cari Room → pilih online Post (Kasus B) untuk Rino.
    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.selectOption('#assessmentTypeInput', 'PreTest');
    await page.click('#btnCariRoom');
    await expect(page.locator('#roomPickerModal')).toBeVisible();
    await page.fill('#roomSearchInput', ONLINE_POST_TITLE);
    const room = page.locator('#roomPickerResults .list-group-item-action', { hasText: ONLINE_POST_TITLE });
    await expect(room).toBeVisible({ timeout: 10_000 });
    await room.click();
    await expect(page.locator('#selectedRoomChip')).toBeVisible();

    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await pickWorker(page, WORKER_EMAILS[0]); // Rino (punya sibling Post online → ter-pair)
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();
    await authorMcQuestion(page, 'Soal Pre 398 — ' + title);
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();
    await setCertAuto(page);
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();

    await page.locator('#step5AnswerForm input[type="radio"]').nth(0).check(); // MC A benar
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(sessionId).toBeGreaterThan(0);

    // Cross-grouping: inject Pre & online Post berbagi LinkedGroupId (pasangan silang utuh, §13).
    const linkedMatch = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions a
         INNER JOIN AssessmentSessions b ON a.LinkedGroupId = b.LinkedGroupId
       WHERE a.Id=${sessionId} AND a.LinkedGroupId IS NOT NULL
         AND b.Title='${onlineSql}' AND b.UserId='${RINO_ID}'`
    );
    expect(linkedMatch).toBeGreaterThanOrEqual(1);

    // Downstream: KEDUA anggota pasangan tampil "Assessment Online" di Records Rino.
    await assertRecordsRowSeakanOnline(page, RINO_ID, title);              // inject Pre
    await assertRecordsRowSeakanOnline(page, RINO_ID, ONLINE_POST_TITLE);  // online Post
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// SKENARIO 5 — side-by-side parity inject vs online (D-03)
//   Records-row level: sesi inject + sesi online sibling (IsManualEntry=0) KEDUANYA
//   "Assessment Online" TANPA penanda yang membedakan inject dari online.
// ─────────────────────────────────────────────────────────────────────────────
test.describe('INJ-13 side-by-side parity inject vs online', () => {
  test('inject Form + online sibling → kedua baris Records identik (tak ada penanda inject/manual)', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Side 398 ' + TS;
    const titleSql = title.replace(/'/g, "''");

    // Inject Form ringan (1 MC) untuk Rino.
    await page.fill('#Title', title);
    await page.selectOption('#Category', { index: 1 });
    await page.click('#btnNext1');
    await expect(page.locator('#step-2')).toBeVisible();
    await pickWorker(page, WORKER_EMAILS[0]);
    await page.click('#btnNext2');
    await expect(page.locator('#step-3')).toBeVisible();
    await authorMcQuestion(page, 'Soal Side 398 — ' + title);
    await page.click('#btnNext3');
    await expect(page.locator('#step-4')).toBeVisible();
    await setCertAuto(page);
    await page.click('#btnNext4');
    await expect(page.locator('#step-5')).toBeVisible();
    await page.locator('#step5AnswerForm input[type="radio"]').nth(0).check();
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(sessionId).toBeGreaterThan(0);
    // Konfirmasi inject = IsManualEntry=1 (DB), online sibling = IsManualEntry=0 — beda di DB,
    // SAMA di tampilan Records (load-bearing INJ-13 "seakan online").
    const injManual = await db.queryScalar(`SELECT IsManualEntry FROM AssessmentSessions WHERE Id=${sessionId}`);
    expect(injManual).toBe(1);

    // Side-by-side: baris inject DAN baris online sibling KEDUANYA "Assessment Online",
    // data-type sama, badge "Assessment" sama, tanpa penanda 'Manual'/'Inject' (row-level).
    await assertRecordsRowSeakanOnline(page, RINO_ID, title);                 // inject
    await assertRecordsRowSeakanOnline(page, RINO_ID, ONLINE_SIBLING_TITLE);  // online sibling
  });
});
