// Phase 396 (Import Excel) — verifikasi runtime jalur Import Excel di Langkah 5 wizard
//   /Admin/InjectAssessment (Razor + JS). Toggle Form/Excel, Download Template, Upload & Pratinjau,
//   commit byte-identik via #btnInject, daftar error atomic.
// Pre-req runtime: server localhost:5277 dari MAIN tree dengan Authentication__UseActiveDirectory=false
//   (Razor di-embed saat build — AddControllersWithViews tanpa RuntimeCompilation; lesson Phase 354/392).
//   Login admin@pertamina.com / 123456 (helpers/accounts.ts). SQLEXPRESS lokal reachable.
// Run: cd tests && npx playwright test e2e/inject-excel-396.spec.ts --workers=1
//
// KRITIS (anti silent-grade-0, Pitfall 4): test ini MENULIS DB (commit inject aktual + cert + audit).
//   - #AnswersJson WAJIB terisi dari hasil parse upload sebelum submit → POST answers tak kosong.
//   - Skor pasca-commit di AssessmentSessions WAJIB == skor preview (BUKAN 0) → buktikan Excel-cache benar.
// CLAUDE.md Seed Workflow: snapshot DB lokal di beforeAll, RESTORE di afterAll (catat SEED_JOURNAL.md).
//
// Excel authoring: pakai exceljs (devDep di tests/package.json) untuk MEMBACA template ter-download
//   lalu MENULIS sel jawaban (sheet "Jawaban"), simpan ulang, lalu setInputFiles ke #step5ExcelFile.
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
const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'inject-excel-396-'));

const WORKER_EMAILS = ['rino.prasetyo@pertamina.com'];

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

async function authorMcQuestion(page: Page, text: string) {
  await page.selectOption('#QuestionType', 'MultipleChoice');
  await page.fill('#questionText', text);
  await page.fill('#option_A', 'Jawaban A');
  await page.fill('#option_B', 'Jawaban B');
  await page.check('#correct_A');
  await page.click('#injAddQuestionBtn');
}

async function authorEssayQuestion(page: Page, text: string) {
  await page.selectOption('#QuestionType', 'Essay');
  await page.fill('#questionText', text);
  await page.fill('#rubrik', 'Rubrik: poin kunci jawaban essay.');
  await page.click('#injAddQuestionBtn');
}

// Drive Setup → 1 pekerja ber-NIP → 1 soal MC (A benar) + 1 soal Essay (skor 0..10) → Sertifikat → Langkah 5.
// Layout template "Jawaban": col1=NIP, col2=Nama, col3="Soal 1 (MC 1 huruf)",
//   col4="Soal 2 Skor (0..10)", col5="Soal 2 Teks (opsional)".
async function fillToStep5(page: Page, title: string) {
  await page.fill('#Title', title);
  await page.selectOption('#Category', { index: 1 });
  await page.click('#btnNext1');
  await expect(page.locator('#step-2')).toBeVisible();
  await page.locator(`#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[0]}"] .user-checkbox`).check();
  await page.click('#btnNext2');
  await expect(page.locator('#step-3')).toBeVisible();
  await authorMcQuestion(page, 'Soal MC 1 — ' + title);
  await authorEssayQuestion(page, 'Soal Essay 2 — ' + title);
  await page.click('#btnNext3');
  await expect(page.locator('#step-4')).toBeVisible();
  await page.click('#btnNext4');
  await expect(page.locator('#step-5')).toBeVisible();
  await expect(page.locator('#step5Body')).toBeVisible();
}

async function switchToExcel(page: Page) {
  await page.check('#step5MethodExcel');
  await expect(page.locator('#step5ExcelPanel')).toBeVisible();
  await expect(page.locator('#step5FormPath')).toBeHidden();
}

// Download template via tombol → simpan ke disk → return path (verifikasi unduhan nyata).
async function downloadTemplate(page: Page, fileTag: string): Promise<string> {
  const [download] = await Promise.all([
    page.waitForEvent('download', { timeout: 15_000 }),
    page.click('#btnDownloadTemplate'),
  ]);
  const dest = path.join(tmpDir, `${fileTag}.xlsx`);
  await download.saveAs(dest);
  return dest;
}

// NIP pekerja (resolve dari DB; picker checkbox value = user.Id, server resolve NIP).
// Tabel Identity di-rename "Users" di project ini (bukan AspNetUsers).
async function workerNip(email: string): Promise<string> {
  return await db.queryString(
    `SELECT TOP 1 NIP FROM Users WHERE Email='${email.replace(/'/g, "''")}'`
  );
}

// Bangun file Excel upload FRESH dengan exceljs (sheet "Jawaban" — parser baca by name + posisi kolom).
// row1 = header (teks tak dibaca parser), row2 = worker (col1=NIP, col2=Nama, col3+=jawaban).
// edits: { colNumber: value } untuk row2. extraRows: baris tambahan (mis. NIP asing) row3+.
// ClosedXML round-trip TIDAK dipakai (exceljs.readFile incompat dgn output ClosedXML) — file fresh setara.
async function buildUploadXlsx(
  outTag: string,
  nip: string,
  row2: { [col: number]: string | number },
  extraRows?: Array<{ [col: number]: string | number }>
): Promise<string> {
  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet('Jawaban');
  // Header row (label apa pun — parser skip row 1, baca posisi).
  ws.getRow(1).values = ['NIP', 'Nama', 'Soal 1 (MC 1 huruf)', 'Soal 2 Skor (0..10)', 'Soal 2 Teks (opsional)'];
  ws.getRow(2).getCell(1).value = nip;
  ws.getRow(2).getCell(2).value = 'Rino Prasetyo';
  for (const c of Object.keys(row2)) {
    ws.getRow(2).getCell(parseInt(c, 10)).value = row2[parseInt(c, 10) as any] as any;
  }
  if (extraRows) {
    let r = 3;
    for (const er of extraRows) {
      for (const c of Object.keys(er)) {
        ws.getRow(r).getCell(parseInt(c, 10)).value = er[parseInt(c, 10) as any] as any;
      }
      r++;
    }
  }
  const out = path.join(tmpDir, `${outTag}.xlsx`);
  await wb.xlsx.writeFile(out);
  return out;
}

test.beforeAll(async () => {
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  const dir = dirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre396-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
  console.log(`[inject-396] snapshot: ${snapshotPath}`);
});

test.afterAll(async () => {
  try { fs.rmSync(tmpDir, { recursive: true, force: true }); } catch { /* ignore */ }
  if (!snapshotPath) return;
  try {
    await db.restore(snapshotPath);
    console.log(`[inject-396] RESTORE OK dari ${snapshotPath}`);
  } catch (e) {
    console.error('[inject-396] RESTORE GAGAL — restore manual via snapshot di atas:', e);
    throw e;
  }
});

// ── Scenario 1: toggle Form↔Excel mutually exclusive ──
test.describe('N1 toggle metode', () => {
  test('toggle Import Excel sembunyikan form, kembali tampilkan form', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    await fillToStep5(page, 'ZZ Excel TGL ' + TS);

    // Default: form path tampil, excel panel sembunyi.
    await expect(page.locator('#step5FormPath')).toBeVisible();
    await expect(page.locator('#step5ExcelPanel')).toBeHidden();

    // Pilih Import Excel → form sembunyi, panel Excel tampil.
    await page.check('#step5MethodExcel');
    await expect(page.locator('#step5FormPath')).toBeHidden();
    await expect(page.locator('#step5ExcelPanel')).toBeVisible();
    await expect(page.locator('#Step5Method')).toHaveValue('excel');

    // Kembali ke Form → terbalik.
    await page.check('#step5MethodForm');
    await expect(page.locator('#step5FormPath')).toBeVisible();
    await expect(page.locator('#step5ExcelPanel')).toBeHidden();
    await expect(page.locator('#Step5Method')).toHaveValue('form');
  });
});

// ── Scenario 2: Download Template memicu unduhan .xlsx ──
test.describe('N2b Download Template', () => {
  test('klik Download Template → event download .xlsx (inject_template)', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    await fillToStep5(page, 'ZZ Excel DL ' + TS);
    await switchToExcel(page);

    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 15_000 }),
      page.click('#btnDownloadTemplate'),
    ]);
    const name = download.suggestedFilename();
    expect(name).toContain('inject_template');
    expect(name.endsWith('.xlsx')).toBe(true);
    // Simpan & verifikasi file .xlsx nyata (non-empty, ZIP signature "PK").
    const dest = path.join(tmpDir, 'dl-verify.xlsx');
    await download.saveAs(dest);
    const buf = fs.readFileSync(dest);
    expect(buf.length).toBeGreaterThan(0);
    expect(buf.slice(0, 2).toString('latin1')).toBe('PK'); // OOXML = ZIP container
  });
});

// ── Scenario 3: upload sukses → preview (skor != 0, Lulus) → commit → DB Score == preview ──
test.describe('Upload sukses → preview → commit (anti silent-grade-0)', () => {
  test('isi jawaban benar (essay skor tanpa teks, D-05) → preview Lulus → commit grade benar', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Excel OK ' + TS;
    const titleSql = title.replace(/'/g, "''");
    const before = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );

    await fillToStep5(page, title);
    await switchToExcel(page);
    await downloadTemplate(page, 'ok-tpl');   // exercise tombol download (unduhan nyata)
    const nip = await workerNip(WORKER_EMAILS[0]);

    // MC benar = "A" (col3). Essay skor 10 tanpa teks (D-05 — teks opsional di Excel).
    // Skor: MC 10/10 + Essay 10/10 = 100% Lulus.
    const filled = await buildUploadXlsx('ok-filled', nip, { 3: 'A', 4: 10 });

    await page.setInputFiles('#step5ExcelFile', filled);
    await page.click('#btnUploadExcel');

    // Preview tampil, error tersembunyi.
    await expect(page.locator('#step5ExcelPreview')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('#step5ExcelErrors')).toBeHidden();
    const previewRow = page.locator('#step5ExcelPreviewBody tr').first();
    await expect(previewRow).toContainText('100%');
    await expect(previewRow.locator('.badge')).toContainText('Lulus');
    // Tanpa nomor sertifikat di preview.
    await expect(page.locator('#step5ExcelPreview')).not.toContainText('KPB/');

    // #AnswersJson cache terisi (bukan kosong) sebelum commit.
    const cache = await page.evaluate(() => (window as any).injExcelAnswersCache);
    expect(cache).toBeTruthy();
    expect(cache).not.toBe('[]');

    // Commit via #btnInject (jalur sama 395).
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const after = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );
    expect(after).toBe(before + 1);
    // Anti silent-grade-0: skor commit == preview (100), bukan 0.
    const committedScore = await db.queryScalar(
      `SELECT TOP 1 Score FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`
    );
    expect(committedScore).toBe(100);
    const passCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}' AND Score=100 AND IsPassed=1`
    );
    expect(passCount).toBe(1);
  });
});

// ── Scenario 4: upload invalid → daftar error LENGKAP (≥2) + preview tersembunyi + 0 write ──
test.describe('Upload invalid → daftar error atomic (D-09)', () => {
  test('huruf MC invalid + skor essay melebihi maks + NIP asing → semua error tampil, 0 commit', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Excel ERR ' + TS;
    const titleSql = title.replace(/'/g, "''");
    const before = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );

    await fillToStep5(page, title);
    await switchToExcel(page);
    await downloadTemplate(page, 'err-tpl');   // exercise tombol download
    const nip = await workerNip(WORKER_EMAILS[0]);

    // Worker row: MC "E" (invalid, hanya A/B) + Essay skor 99 (melebihi maks 10).
    // Tambah baris asing: NIP 'NIP-ASING-999' tak ada di picker → error D-02.
    const bad = await buildUploadXlsx(
      'err-filled', nip,
      { 3: 'E', 4: 99 },
      [{ 1: 'NIP-ASING-999', 2: 'Orang Asing', 3: 'A', 4: 5 }]
    );

    await page.setInputFiles('#step5ExcelFile', bad);
    await page.click('#btnUploadExcel');

    // Panel error tampil (danger), preview TIDAK tampil.
    await expect(page.locator('#step5ExcelErrors')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('#step5ExcelErrors')).toHaveClass(/alert-danger/);
    await expect(page.locator('#step5ExcelPreview')).toBeHidden();

    // Daftar LENGKAP (≥2 item) — bukan stop-di-error-pertama.
    const items = page.locator('#step5ExcelErrorList li');
    expect(await items.count()).toBeGreaterThanOrEqual(2);

    // T-396-10: cache "[]" → commit tak menghasilkan sesi.
    const cache = await page.evaluate(() => (window as any).injExcelAnswersCache);
    expect(cache).toBe('[]');

    // Tidak ada sesi ter-commit untuk title ini.
    const after = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );
    expect(after).toBe(before);
  });
});

// ── Scenario 5 (D-06): sel kosong → upload sukses + warn "dihitung 0" + preview tampil, commit boleh ──
test.describe('Sel kosong = warn-but-allow (D-06)', () => {
  test('MC kosong → upload sukses, warn sel-kosong, preview menghitung soal kosong = 0', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Excel BLANK ' + TS;
    await fillToStep5(page, title);
    await switchToExcel(page);
    await downloadTemplate(page, 'blank-tpl');   // exercise tombol download
    const nip = await workerNip(WORKER_EMAILS[0]);

    // Hanya isi Essay (skor 10); MC (col3) DIBIARKAN kosong → skipped → dihitung 0.
    const filled = await buildUploadXlsx('blank-filled', nip, { 4: 10 });

    await page.setInputFiles('#step5ExcelFile', filled);
    await page.click('#btnUploadExcel');

    await expect(page.locator('#step5ExcelPreview')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('#step5ExcelErrors')).toBeHidden();
    // Warn sel kosong muncul.
    await expect(page.locator('#step5ExcelBlankWarn')).toBeVisible();
    await expect(page.locator('#step5ExcelBlankWarn')).toContainText('dihitung 0');
    // Soal terjawab 1 / 2 (MC kosong di-skip).
    const previewRow = page.locator('#step5ExcelPreviewBody tr').first();
    await expect(previewRow).toContainText('1 / 2');
  });
});
