// Phase 395 (Mode jawaban: input-asli + auto-generate) — verifikasi runtime Langkah 5 wizard
//   /Admin/InjectAssessment (Razor + JS) + COMMIT aktual "seakan online" (commit pertama milestone).
// Pre-req runtime: server localhost:5277 dari MAIN tree dengan Authentication__UseActiveDirectory=false
//   (Razor di-embed saat build — AddControllersWithViews tanpa RuntimeCompilation; lesson Phase 354/392).
//   Login admin@pertamina.com / 123456 (helpers/accounts.ts). SQLEXPRESS lokal reachable.
// Run: cd tests && npx playwright test e2e/inject-assessment-395.spec.ts --workers=1
//
// KRITIS (anti silent-grade-0, Pitfall 4): test ini MENULIS DB (commit inject aktual + cert + audit).
//   - #AnswersJson WAJIB terisi sebelum submit (page.evaluate) → POST answers tak kosong.
//   - Skor pasca-commit di /CMP/Results WAJIB == skor preview (BUKAN 0) → buktikan serialize benar.
// CLAUDE.md Seed Workflow: snapshot DB lokal di beforeAll, RESTORE di afterAll (catat SEED_JOURNAL.md).
//   (Defensif; saat run penuh, global.setup/teardown matrix pipeline juga BACKUP/RESTORE.)
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';

test.describe.configure({ mode: 'serial' });

const TS = Date.now();
let snapshotPath = '';

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

// Author 1 soal MC (A benar) dgn teks unik supaya answer-pattern deterministik.
async function authorMcQuestion(page: Page, text: string) {
  await page.selectOption('#QuestionType', 'MultipleChoice');
  await page.fill('#questionText', text);
  await page.fill('#option_A', 'Jawaban A');
  await page.fill('#option_B', 'Jawaban B');
  await page.check('#correct_A');
  await page.click('#injAddQuestionBtn');
}

// Pekerja yang WAJIB ber-NIP (controller skip null-NIP user, surfaced commit 395) — pakai email yang
// dipastikan ber-NIP supaya commit menghasilkan sesi (bukan 0 sesi karena NIP NULL).
const WORKER_EMAILS = ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com'];

// Drive Setup → Pilih N pekerja ber-NIP (by email) → Authoring 2 soal MC → Sertifikat → Langkah 5.
async function fillToStep5(page: Page, title: string, workerCount: number) {
  await page.fill('#Title', title);
  await page.selectOption('#Category', { index: 1 });
  await page.click('#btnNext1');
  await expect(page.locator('#step-2')).toBeVisible();
  // Pilih pekerja spesifik ber-NIP via data-email pada .user-check-item.
  for (let i = 0; i < workerCount; i++) {
    const email = WORKER_EMAILS[i];
    await page.locator(`#userCheckboxContainer .user-check-item[data-email="${email}"] .user-checkbox`).check();
  }
  await page.click('#btnNext2');
  await expect(page.locator('#step-3')).toBeVisible();
  await authorMcQuestion(page, 'Soal MC 1 — ' + title);
  await authorMcQuestion(page, 'Soal MC 2 — ' + title);
  await page.click('#btnNext3');
  await expect(page.locator('#step-4')).toBeVisible();
  await page.click('#btnNext4');
  await expect(page.locator('#step-5')).toBeVisible();
  await expect(page.locator('#step5Body')).toBeVisible();
}

// Author 1 soal Essay (rubrik) untuk uji validasi D-04 teks-wajib.
async function authorEssayQuestion(page: Page, text: string) {
  await page.selectOption('#QuestionType', 'Essay');
  await page.fill('#questionText', text);
  await page.fill('#rubrik', 'Rubrik: poin kunci jawaban essay.');
  await page.click('#injAddQuestionBtn');
}

// Drive Setup → 1 pekerja ber-NIP → 1 soal Essay → Sertifikat → Langkah 5.
async function fillToStep5Essay(page: Page, title: string) {
  await page.fill('#Title', title);
  await page.selectOption('#Category', { index: 1 });
  await page.click('#btnNext1');
  await expect(page.locator('#step-2')).toBeVisible();
  await page.locator(`#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[0]}"] .user-checkbox`).check();
  await page.click('#btnNext2');
  await expect(page.locator('#step-3')).toBeVisible();
  await authorEssayQuestion(page, 'Soal Essay — ' + title);
  await page.click('#btnNext3');
  await expect(page.locator('#step-4')).toBeVisible();
  await page.click('#btnNext4');
  await expect(page.locator('#step-5')).toBeVisible();
  await expect(page.locator('#step5Body')).toBeVisible();
}

test.beforeAll(async () => {
  // CLAUDE.md Seed Workflow: BACKUP sebelum commit-test. Pakai SQL default backup dir
  // (C:\Temp\ diblokir service account — pola Phase 315/355).
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`
  );
  const dir = dirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre395-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
  console.log(`[inject-395] snapshot: ${snapshotPath}`);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  try {
    await db.restore(snapshotPath);
    console.log(`[inject-395] RESTORE OK dari ${snapshotPath}`);
  } catch (e) {
    console.error('[inject-395] RESTORE GAGAL — restore manual via snapshot di atas:', e);
    throw e;
  }
});

// ── INJ-08: input-asli → commit → skor benar di /CMP/Results (anti silent-grade-0) ──
test.describe('INJ-08 input-asli commit', () => {
  test('input-asli → Pratinjau → commit → skor /CMP/Results == preview (bukan 0)', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Inject IA ' + TS;
    const titleSql = title.replace(/'/g, "''");
    const before = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );

    await fillToStep5(page, title, 2);

    // Pekerja 1 — mode default input-asli. Pilih opsi BENAR (A) di 2 soal → harus 100%.
    await expect(page.locator('#step5WorkerHeading')).toContainText('Pekerja 1 dari 2');
    await expect(page.locator('#step5ModeManual')).toBeChecked();
    // 2 grup radio (1 per soal); pilih radio pertama (opsi A = benar) tiap grup.
    const radios = page.locator('#step5AnswerForm input[type="radio"]');
    await expect(radios).toHaveCount(4); // 2 soal × 2 opsi
    await radios.nth(0).check(); // soal1 opsi A (benar)
    await radios.nth(2).check(); // soal2 opsi A (benar)

    // Pratinjau Skor → skor 100% + badge Lulus, tanpa nomor sertifikat.
    await page.click('#step5PreviewBtn');
    await expect(page.locator('#step5PreviewResult')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#step5PreviewResult')).toContainText('100%');
    await expect(page.locator('#step5PreviewResult')).toContainText('Lulus');
    await expect(page.locator('#step5PreviewResult')).not.toContainText('KPB/'); // no cert# di preview

    // Pekerja 2 — input-asli, pilih opsi SALAH (B) di kedua soal → 0%.
    await page.click('#step5NextWorker');
    await expect(page.locator('#step5WorkerHeading')).toContainText('Pekerja 2 dari 2');
    await radios.nth(1).check(); // soal1 opsi B (salah)
    await radios.nth(3).check(); // soal2 opsi B (salah)

    // === ANTI SILENT-GRADE-0: #AnswersJson WAJIB terisi sebelum submit (cek payload builder) ===
    const answersRaw = await page.evaluate(
      () => JSON.stringify((window as any).injBuildWorkerAnswers())
    );
    expect(answersRaw).toBeTruthy();
    const parsed = JSON.parse(answersRaw);
    expect(Array.isArray(parsed)).toBe(true);
    expect(parsed.length).toBe(2);
    expect(parsed[0]).toHaveProperty('userId');
    expect(parsed[0]).toHaveProperty('mode');
    expect(Array.isArray(parsed[0].answers)).toBe(true);
    expect(parsed[0].answers.length).toBe(2); // 2 soal terjawab worker-1
    expect(parsed[1].answers.length).toBe(2); // 2 soal terjawab worker-2

    // === COMMIT aktual "seakan online" (submit-listener serialize #AnswersJson) ===
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');

    // Flash sukses + sesi ter-commit (2 worker).
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });
    const after = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}'`
    );
    expect(after).toBe(before + 2);

    // Worker-1 (semua benar) → Score=100. Worker-2 (semua salah) → Score=0. Buktikan grading bukan
    // silent-0: ada sesi ber-Score=100 (kalau #AnswersJson kosong, KEDUA sesi = 0 → test gagal).
    const passCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}' AND Score=100 AND IsPassed=1`
    );
    expect(passCount).toBe(1); // == preview worker-1 (bukan 0)
    const failCount = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${titleSql}' AND Score=0`
    );
    expect(failCount).toBe(1); // worker-2 input-asli semua salah
  });
});

// ── INJ-09: auto-generate → Pratinjau (skor >= target) → commit → skor match ──
test.describe('INJ-09 auto-generate commit', () => {
  test('auto-gen → Pratinjau skor >= target + badge → commit → skor /CMP/Results match', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    const title = 'ZZ Inject AG ' + TS;
    await fillToStep5(page, title, 1);

    // Pekerja 1 → mode auto-generate, target 50% (2 soal equal-weight 10 → 1 benar = 50%).
    await page.check('#step5ModeAuto');
    await expect(page.locator('#step5AutoBody')).toBeVisible();
    await page.fill('#step5TargetScore', '50');

    // Pratinjau Skor → skor final aktual >= 50 + badge Lulus/Tidak, tanpa cert#.
    await page.click('#step5PreviewBtn');
    await expect(page.locator('#step5PreviewResult')).toBeVisible({ timeout: 10_000 });
    const previewText = await page.locator('#step5PreviewResult').innerText();
    const m = previewText.match(/(\d+)%/);
    expect(m).toBeTruthy();
    const previewPct = parseInt(m![1], 10);
    expect(previewPct).toBeGreaterThanOrEqual(50);
    await expect(page.locator('#step5PreviewResult')).not.toContainText('KPB/');

    // Commit.
    const before = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${title.replace(/'/g, "''")}'`
    );
    await page.click('#btnNext5');
    await expect(page.locator('#step-6')).toBeVisible();
    await page.click('#btnInject');
    await page.waitForLoadState('load');
    await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
      .toBeVisible({ timeout: 10_000 });

    const after = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions WHERE Title='${title.replace(/'/g, "''")}'`
    );
    expect(after).toBe(before + 1);

    // Skor commit == preview (auto-gen seed deterministik → preview == commit). Bukan 0.
    const committedScore = await db.queryScalar(
      `SELECT TOP 1 Score FROM AssessmentSessions WHERE Title='${title.replace(/'/g, "''")}' ORDER BY Id DESC`
    );
    expect(committedScore).toBe(previewPct);
    expect(committedScore).toBeGreaterThanOrEqual(50);
  });
});

// ── LBL-02: badge tipe soal "Single Answer"/"Multiple Answer" (bukan Pilihan Ganda) ──
test.describe('LBL-02 label tipe soal', () => {
  test('badge tipe soal form jawaban = Single Answer (bukan Pilihan Ganda)', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    await fillToStep5(page, 'ZZ Inject LBL ' + TS, 1);
    const badge = page.locator('#step5AnswerForm .badge').first();
    await expect(badge).toContainText('Single Answer');
    await expect(badge).not.toContainText('Pilihan Ganda');
  });
});

// ── D-04 (UI-SPEC K3): teks essay WAJIB bila skor diisi — validasi inline saat Pratinjau ──
//    Regresi FINDING-1 UAT: dulu Pratinjau menghitung skor (mis. 90% Lulus) walau teks essay kosong
//    (D-04 hanya di-guard server pada commit → preview != commit, menyesatkan). Fix: validasi client-side
//    di handler Pratinjau memblokir + tampilkan error inline + TIDAK memanggil endpoint preview.
test.describe('D-04 inline essay-text validation (Pratinjau)', () => {
  test('skor essay diisi + teks kosong → Pratinjau blokir inline (tak hitung skor); isi teks → hitung', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/InjectAssessment');
    await fillToStep5Essay(page, 'ZZ Inject D04 ' + TS);

    // input-asli default — isi SKOR essay, BIARKAN teks kosong.
    await expect(page.locator('#step5ModeManual')).toBeChecked();
    await page.locator('#step5AnswerForm input[type="number"]').fill('8');

    // Pratinjau → diblok inline (D-04), preview TIDAK dirender (tak ada skor menyesatkan).
    await page.click('#step5PreviewBtn');
    await expect(
      page.locator('#step5AnswerForm').getByText('Teks jawaban essay wajib diisi karena skornya diisi.')
    ).toBeVisible();
    await expect(page.locator('#step5PreviewResult')).toBeHidden();

    // Isi teks → error hilang + Pratinjau menghitung skor (preview == commit dipulihkan).
    await page.locator('#step5AnswerForm textarea').fill('Jawaban essay lengkap dengan poin kunci.');
    await page.click('#step5PreviewBtn');
    await expect(page.locator('#step5PreviewResult')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('#step5PreviewResult')).toContainText('%');
  });
});
