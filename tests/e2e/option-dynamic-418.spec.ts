// Phase 418-04 Task 2 (OPT-01/02/03 + flag#1/#4 + D-418-02) — UAT e2e Opsi Jawaban Dinamis 2–6 real-browser.
// Membuktikan RUNTIME (lesson 354: Razor/JS WAJIB UAT browser — pure unit/integration tak menangkap
// interaksi DOM form dinamis, re-letter, reasosiasi gambar, single-select MC lintas baris, render Razor
// huruf A–F, alert edit-shrink) bahwa:
//   S1 (add → 6 → disabled@6, OPT-01): form fresh 4 baris → klik "+ Tambah Opsi" 2× → 6 baris (E,F muncul) →
//       tombol Tambah disabled.
//   S2 (remove → min 2 + re-letter, OPT-01): hapus baris s/d 2 → tombol Hapus hidden saat 2; setelah hapus
//       baris-tengah, huruf re-letter berurutan A,B,C (tak ada gap).
//   S3 (image reassoc flag#4, KRITIS): isi 4 opsi, beri gambar di opsi C, hapus baris B → gambar tetap
//       menempel pada opsi yang benar (C jadi B, thumbnail ikut) — bukan hilang/pindah salah baris. Simpan →
//       DB: opsi ber-teks "C" yang punya ImagePath (gambar tak ter-misalign ke "A").
//   S4 (single-select MC flag#1 + render A–F OPT-02): buat soal MC 6-opsi (benar=E) → DB tepat 1 IsCorrect=E
//       → ambil ujian peserta → huruf E & F tampil di kartu soal (StartExam render dinamis A–F).
//   S5 (PreviewPackage 6th="F", OPT-02 regresi modulo): /Admin/PreviewPackage soal 6-opsi → opsi ke-6 tampil
//       "F." (BUKAN "A." — bug modulo wrap lama sudah dihapus).
//   S6 (edit imported 5-opsi prefill flag#2, OPT-01): seed soal 5-opsi via SQL → buka edit (AJAX populate) →
//       form prefill 5 baris terisi (bukan 4).
//   S7 (edit-shrink answered blocked D-418-02): seed soal MC + PackageUserResponse ke salah satu opsi →
//       edit, hapus opsi terjawab → .alert-danger "sudah dijawab" (BUKAN halaman 500 / error).
//   S8 (backward-compat 4-opsi, OPT-01): buat + render soal 4-opsi lama → A–D identik, render exam OK,
//       PreviewPackage opsi ke-4 "D." (regresi tak rusak).
//
// Template/analog LANGSUNG: tests/e2e/section-pagination.spec.ts (Phase 417) — sama lifecycle
//   beforeAll BACKUP / afterAll RESTORE (default backup dir, SEED_WORKFLOW), createOjtArriveMP +
//   createDefaultPackage + addQuestionViaForm + startExamAsParticipant + execSql/queryStr helper.
//   option-validation-386.spec.ts (pola #option_A..D + reject .alert-danger).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE.
//   (Catatan: playwright.config globalSetup juga seed matrix + globalTeardown RESTORE — pola analog 417:
//    spec backup di beforeAll meng-capture state matrix-seeded, restore di afterAll, matrix dibersihkan
//    globalTeardown. Idempoten.)
// PRECONDITION run: app di http://localhost:5277 (Authentication__UseActiveDirectory=false dotnet run)
//   + DB lokal HcPortalDB_Dev. WAJIB --workers=1 (playwright.config fullyParallel:false, DB isolation).
// Auth: admin@pertamina.com / 123456 (dev lokal). Peserta: rino.prasetyo@pertamina.com (coachee).

import { test, expect, type Page, type Browser } from '@playwright/test';
import * as path from 'node:path';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
import { questionFormSelectors } from './helpers/wizardSelectors';

test.describe.configure({ mode: 'serial' });

let snapshotPath: string;

// Fixture gambar PNG existing di wwwroot (reasoc flag#4 cukup butuh src ter-set, konten gambar tak relevan).
const IMG_FIXTURE = path.resolve(__dirname, '../fixtures/option-img.png');

const fmtLocal = (d: Date) => {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
};
const today = () => fmtLocal(new Date());
const tomorrow = () => { const d = new Date(); d.setDate(d.getDate() + 1); return fmtLocal(d); };

async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}
async function queryStr(sql: string): Promise<string> {
  return db.queryString(`SET NOCOUNT ON; ${sql}`);
}

// Login admin + wizard-create 1 assessment OJT startable + arrive di /Admin/ManagePackages. Return assessmentId.
async function createOjtArriveMP(page: Page, title: string, doLogin = true): Promise<number> {
  if (doLogin) await login(page, 'admin');
  await createAssessmentViaWizard(page, {
    title,
    category: 'OJT',
    scheduleDate: today(),
    scheduleTime: '00:01',
    durationMinutes: 120,
    passPercentage: 50,
    allowAnswerReview: true,
    generateCertificate: false,
    participantEmails: ['rino.prasetyo@pertamina.com'],
    ewcdDate: tomorrow(),
    ewcdTime: '23:59',
  });
  await page.locator('#modal-manage-btn').click();
  await page.waitForLoadState('networkidle');
  const id = parseInt(new URL(page.url()).searchParams.get('assessmentId') ?? '0', 10);
  expect(id, 'assessmentId ter-parse dari URL ManagePackages').toBeGreaterThan(0);
  return id;
}

// Buka form Kelola Soal (fresh, mode buat baru) untuk paket.
async function openQuestionForm(page: Page, packageId: number): Promise<void> {
  await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
  await page.locator(questionFormSelectors.formCard).waitFor({ state: 'visible' });
  await page.locator(questionFormSelectors.optionsSection).waitFor({ state: 'visible' });
}

// Jumlah baris opsi di form authoring.
async function optionRowCount(page: Page): Promise<number> {
  return page.locator('#optionRows [data-option-row]').count();
}

// Daftar huruf display (data-letter span) per baris, urut DOM.
async function optionLetters(page: Page): Promise<string[]> {
  return page.locator('#optionRows [data-option-row] [data-letter]').allInnerTexts();
}

// Peserta (coachee rino) login + mulai ujian standard → return { page, sessionId, close }.
async function startExamAsParticipant(browser: Browser, title: string): Promise<{ page: Page; sessionId: number; close: () => Promise<void> }> {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await login(page, 'coachee');
  await page.goto('/CMP/Assessment');
  const card = page.locator('.assessment-card', { hasText: title });
  await expect(card, `kartu assessment "${title}" tampil di lobby peserta`).toBeVisible({ timeout: 10_000 });

  const startBtn = card.locator('.btn-start-standard');
  const resumeLink = card.locator('a:has-text("Resume")');
  page.once('dialog', (d) => d.accept());
  if (await startBtn.isVisible().catch(() => false)) {
    await startBtn.click();
  } else {
    await resumeLink.first().click();
  }
  await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

  // Dismiss resume modal bila muncul.
  const resumeModal = page.locator('#resumeConfirmModal');
  await resumeModal.waitFor({ state: 'visible', timeout: 4_000 }).catch(() => {});
  if (await resumeModal.isVisible().catch(() => false)) {
    await page.locator('#resumeConfirmBtn').click();
    await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
  }

  const sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)?.[1] ?? '0', 10);
  expect(sessionId, 'sessionId dari URL StartExam').toBeGreaterThan(0);
  return { page, sessionId, close: () => ctx.close() };
}

// Ambil daftar PackageQuestion.Id (urut Order) untuk paket.
async function questionIdsOrdered(packageId: number): Promise<number[]> {
  const raw = await queryStr(
    `SELECT STRING_AGG(CAST(Id AS VARCHAR(12)), ',') WITHIN GROUP (ORDER BY [Order]) ` +
    `FROM PackageQuestions WHERE AssessmentPackageId = ${packageId}`
  );
  return raw.split(',').map((x) => parseInt(x.trim(), 10)).filter((x) => x > 0);
}

test.describe('Phase 418 — Opsi Jawaban Dinamis 2–6 (authoring + render A–F + edit-shrink) UAT e2e', () => {
  test.beforeAll(async () => {
    // Fixture gambar: buat PNG 1×1 bila belum ada (reassoc flag#4 hanya butuh file image valid).
    const fs = await import('node:fs');
    if (!fs.existsSync(IMG_FIXTURE)) {
      fs.mkdirSync(path.dirname(IMG_FIXTURE), { recursive: true });
      // PNG 1×1 transparan (base64 minimal valid).
      const pngB64 =
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
      fs.writeFileSync(IMG_FIXTURE, Buffer.from(pngB64, 'base64'));
    }

    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre418-${ts}.bak`;
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

  // ── S1: add → 6 baris (E,F muncul) → tombol Tambah disabled@6 (OPT-01) ──────────────────────────────
  test('S1: tambah opsi s/d 6 — baris E & F muncul, tombol Tambah disabled@6', async ({ page }) => {
    test.setTimeout(180_000);
    const title = `Pre Test OJT OPSIDINAMIS418 ADD ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);
    await openQuestionForm(page, packageId);

    expect(await optionRowCount(page), 'form fresh = 4 baris (A–D, D-418-01)').toBe(4);
    const addBtn = page.locator(questionFormSelectors.addOptionBtn);
    await expect(addBtn, 'tombol Tambah aktif saat <6 baris').toBeEnabled();

    await addBtn.click();   // → 5 baris (E)
    expect(await optionRowCount(page), '5 baris setelah Tambah ke-1').toBe(5);
    await addBtn.click();   // → 6 baris (F)
    expect(await optionRowCount(page), '6 baris setelah Tambah ke-2').toBe(6);

    // Huruf A–F berurutan + input E/F hadir.
    expect(await optionLetters(page), 'huruf A–F berurutan').toEqual(['A', 'B', 'C', 'D', 'E', 'F']);
    await expect(page.locator(questionFormSelectors.optionE), 'input opsi E hadir').toBeVisible();
    await expect(page.locator(questionFormSelectors.optionF), 'input opsi F hadir').toBeVisible();

    // Tombol Tambah disabled@6 (title "Maksimal 6 opsi").
    await expect(addBtn, 'tombol Tambah disabled saat 6 baris').toBeDisabled();
    await expect(addBtn).toHaveAttribute('title', /Maksimal 6 opsi/i);
  });

  // ── S2: hapus → min 2 (tombol Hapus hidden@2) + re-letter A,B,C tanpa gap (OPT-01) ──────────────────
  test('S2: hapus opsi s/d minimal 2 + re-letter berurutan setelah hapus baris-tengah', async ({ page }) => {
    test.setTimeout(180_000);
    const title = `Pre Test OJT OPSIDINAMIS418 REMOVE ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);
    await openQuestionForm(page, packageId);

    // Fresh 4 baris. Hapus baris B (index 1) → C,D re-letter jadi B,C.
    const rows = page.locator('#optionRows [data-option-row]');
    await rows.nth(1).locator('.remove-option-btn').click();
    expect(await optionRowCount(page), '3 baris setelah hapus baris-tengah').toBe(3);
    expect(await optionLetters(page), 'huruf re-letter berurutan A,B,C (tanpa gap)').toEqual(['A', 'B', 'C']);

    // Hapus 1 baris lagi → 2 baris; tombol Hapus jadi hidden (d-none) di SEMUA baris saat <=2.
    await rows.nth(0).locator('.remove-option-btn').click();
    expect(await optionRowCount(page), '2 baris (minimum)').toBe(2);
    const visibleRemoveBtns = await page.locator('#optionRows [data-option-row] .remove-option-btn:not(.d-none)').count();
    expect(visibleRemoveBtns, 'tombol Hapus hidden saat 2 baris (tak boleh <2)').toBe(0);
  });

  // ── S3 (KRITIS flag#4): gambar di opsi C, hapus baris B → gambar tetap di opsi benar (C→B) ──────────
  test('S3: reasosiasi gambar baris-tengah — gambar opsi C ikut saat baris B dihapus (flag#4)', async ({ page }) => {
    test.setTimeout(180_000);
    const title = `Pre Test OJT OPSIDINAMIS418 IMGREASSOC ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);
    await openQuestionForm(page, packageId);

    await page.fill(questionFormSelectors.questionText, '[OPSIDINAMIS418] Soal reasosiasi gambar');
    await page.fill(questionFormSelectors.optionA, 'Opsi A teks');
    await page.fill(questionFormSelectors.optionB, 'Opsi B teks');
    await page.fill(questionFormSelectors.optionC, 'Opsi C teks');
    await page.fill(questionFormSelectors.optionD, 'Opsi D teks');
    // Gambar di opsi C (hidden file input optCImgField).
    await page.setInputFiles(questionFormSelectors.optCImgField, IMG_FIXTURE);
    // Thumbnail opsi C tampil (showThumb → img tak d-none).
    await expect(page.locator('#optCImgThumb'), 'thumbnail gambar opsi C tampil setelah pick').not.toHaveClass(/d-none/);

    // Tandai jawaban benar A (MC default). Hapus baris B → C jadi B, gambar IKUT (reletter prefixForNode).
    await page.locator(questionFormSelectors.correctA).check();
    const rows = page.locator('#optionRows [data-option-row]');
    await rows.nth(1).locator('.remove-option-btn').click();   // hapus B

    expect(await optionLetters(page), 're-letter A,B,C setelah hapus B').toEqual(['A', 'B', 'C']);
    // Gambar kini di posisi baris ke-2 (sekarang huruf B) — id thumbnail opt B aktif, teks baris itu "Opsi C teks".
    // Selektor TEKS opsi via name$=".Text" (input ImageAlt juga type=text.form-control → hindari strict-mode bentrok).
    const newBRowText = await rows.nth(1).locator('input[name$=".Text"]').inputValue();
    expect(newBRowText, 'baris ke-2 (kini B) berisi teks opsi C asli (gambar ikut node)').toBe('Opsi C teks');
    await expect(page.locator('#optBImgThumb'), 'thumbnail gambar kini di baris B (node C ter-reletter)').not.toHaveClass(/d-none/);

    // Simpan → server. Pastikan tak error + DB: opsi ber-teks "Opsi C teks" punya ImagePath (gambar tak misalign ke A).
    await page.fill(questionFormSelectors.scoreValue, '10');
    await page.locator(questionFormSelectors.submitBtn).click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.alert-success, .alert.alert-success').first(), 'simpan sukses').toBeVisible({ timeout: 6_000 });

    // DB: gambar (ImagePath not null) menempel pada opsi ber-teks "Opsi C teks", BUKAN "Opsi A teks".
    const qids = await questionIdsOrdered(packageId);
    const lastQ = qids[qids.length - 1];
    const imgOnC = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${lastQ} AND OptionText=N'Opsi C teks' AND ImagePath IS NOT NULL AND LEN(ImagePath)>0`
    );
    expect(imgOnC, 'gambar menempel pada opsi "Opsi C teks" (reasosiasi benar, flag#4)').toBe(1);
    const imgOnA = await db.queryScalar(
      `SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${lastQ} AND OptionText=N'Opsi A teks' AND ImagePath IS NOT NULL AND LEN(ImagePath)>0`
    );
    expect(imgOnA, 'gambar TIDAK misalign ke opsi A').toBe(0);
  });

  // ── S4 (flag#1 + OPT-02): soal MC 6-opsi (benar=E single-select) → render A–F di StartExam ──────────
  test('S4: MC 6-opsi single-select (benar E) → DB 1 IsCorrect + huruf E,F tampil di ujian', async ({ page, browser }) => {
    test.setTimeout(240_000);
    const title = `Pre Test OJT OPSIDINAMIS418 RENDERAF ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);
    await openQuestionForm(page, packageId);

    // Tambah ke 6 baris.
    const addBtn = page.locator(questionFormSelectors.addOptionBtn);
    await addBtn.click();
    await addBtn.click();
    expect(await optionRowCount(page), '6 baris').toBe(6);

    await page.fill(questionFormSelectors.questionText, '[OPSIDINAMIS418] Soal MC 6 opsi render A-F');
    const optInputs = ['option_A', 'option_B', 'option_C', 'option_D', 'option_E', 'option_F'];
    const optTexts = ['Alpha', 'Bravo', 'Charlie', 'Delta', 'Echo', 'Foxtrot'];
    for (let i = 0; i < 6; i++) await page.locator(`#${optInputs[i]}`).fill(optTexts[i]);

    // Single-select MC: centang radio E. Lalu coba centang F → E harus uncheck (native single-select flag#1).
    await page.locator(questionFormSelectors.correctE).check();
    expect(await page.locator(questionFormSelectors.correctE).isChecked(), 'radio E checked').toBe(true);
    await page.locator(questionFormSelectors.correctF).check();
    expect(await page.locator(questionFormSelectors.correctF).isChecked(), 'radio F checked').toBe(true);
    expect(await page.locator(questionFormSelectors.correctE).isChecked(), 'radio E auto-uncheck (single-select MC lintas 6 baris, flag#1)').toBe(false);
    // Set benar kembali ke E (jawaban final).
    await page.locator(questionFormSelectors.correctE).check();

    await page.fill(questionFormSelectors.scoreValue, '10');
    await page.locator(questionFormSelectors.submitBtn).click();
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.alert-success, .alert.alert-success').first(), 'simpan MC 6-opsi sukses').toBeVisible({ timeout: 6_000 });

    // DB: tepat 1 IsCorrect, dan opsi benar = "Echo" (E).
    const qids = await questionIdsOrdered(packageId);
    const lastQ = qids[qids.length - 1];
    const optCount = await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${lastQ}`);
    expect(optCount, 'soal punya 6 opsi tersimpan').toBe(6);
    const correctCount = await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${lastQ} AND IsCorrect=1`);
    expect(correctCount, 'tepat 1 opsi benar (single-select MC server-authoritative)').toBe(1);
    const correctText = await queryStr(`SELECT TOP 1 OptionText FROM PackageOptions WHERE PackageQuestionId=${lastQ} AND IsCorrect=1`);
    expect(correctText, 'opsi benar = Echo (E)').toBe('Echo');

    // Ambil ujian peserta → huruf E & F tampil di kartu soal (StartExam render dinamis A–F).
    const participant = await startExamAsParticipant(browser, title);
    try {
      const ep = participant.page;
      const card = ep.locator('[id^="qcard_"]', { hasText: 'Soal MC 6 opsi render A-F' });
      await expect(card, 'kartu soal 6-opsi ter-render').toBeVisible({ timeout: 10_000 });
      // Huruf E. dan F. muncul (render letters array A–F, bukan numerik/kosong).
      await expect(card.locator('span.fw-bold', { hasText: /^E\.$/ }), 'huruf E. tampil di ujian (OPT-02)').toBeVisible();
      await expect(card.locator('span.fw-bold', { hasText: /^F\.$/ }), 'huruf F. tampil di ujian (OPT-02)').toBeVisible();
      // Opsi teks Echo/Foxtrot hadir.
      await expect(card.locator('label.list-group-item', { hasText: 'Echo' }), 'opsi Echo hadir').toBeVisible();
      await expect(card.locator('label.list-group-item', { hasText: 'Foxtrot' }), 'opsi Foxtrot hadir').toBeVisible();
    } finally {
      await participant.close();
    }
  });

  // ── S5 (OPT-02 regresi modulo): PreviewPackage soal 6-opsi → opsi ke-6 "F." (BUKAN "A.") ─────────────
  test('S5: PreviewPackage soal 6-opsi → opsi ke-6 tampil "F." (bukan "A." — modulo wrap dihapus)', async ({ page }) => {
    test.setTimeout(180_000);
    const title = `Pre Test OJT OPSIDINAMIS418 PREVIEWF ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);
    await openQuestionForm(page, packageId);

    const addBtn = page.locator(questionFormSelectors.addOptionBtn);
    await addBtn.click();
    await addBtn.click();
    await page.fill(questionFormSelectors.questionText, '[OPSIDINAMIS418] Soal preview 6 opsi');
    const optTexts = ['Satu', 'Dua', 'Tiga', 'Empat', 'Lima', 'Enam'];
    const optInputs = ['option_A', 'option_B', 'option_C', 'option_D', 'option_E', 'option_F'];
    for (let i = 0; i < 6; i++) await page.locator(`#${optInputs[i]}`).fill(optTexts[i]);
    await page.locator(questionFormSelectors.correctA).check();
    await page.fill(questionFormSelectors.scoreValue, '10');
    await page.locator(questionFormSelectors.submitBtn).click();
    await page.waitForLoadState('networkidle');

    // PreviewPackage (read-only, urut import — tak ter-shuffle). Opsi ke-6 "Enam" ber-prefix "F.".
    await page.goto(`/Admin/PreviewPackage?packageId=${packageId}`);
    await page.waitForLoadState('networkidle');
    const enamLabel = page.locator('label.form-check-label', { hasText: 'Enam' });
    await expect(enamLabel, 'opsi ke-6 "Enam" ter-render di preview').toBeVisible();
    const enamText = (await enamLabel.innerText()).trim();
    expect(enamText, 'opsi ke-6 ber-huruf "F." (bukan "A." modulo wrap lama)').toMatch(/^F\.\s/);
    // Pastikan tidak ada huruf "A." kedua untuk soal 6-opsi (regresi modulo: dulu ke-6 = "A").
    const lima = page.locator('label.form-check-label', { hasText: 'Lima' });
    expect((await lima.innerText()).trim(), 'opsi ke-5 "Lima" = "E."').toMatch(/^E\.\s/);
  });

  // ── S6 (flag#2 OPT-01): edit soal 5-opsi (seed via SQL) → form prefill 5 baris ──────────────────────
  test('S6: edit soal 5-opsi import → form prefill 5 baris terisi (flag#2)', async ({ page }) => {
    test.setTimeout(180_000);
    const title = `Pre Test OJT OPSIDINAMIS418 PREFILL5 ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // Seed soal 5-opsi langsung via SQL (simulasi soal import 415 yang dulu tak-editable).
    await execSql(
      `INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters) ` +
      `VALUES (${packageId}, N'[OPSIDINAMIS418] Soal 5 opsi import', 1, 10, 'MultipleChoice', 2000)`
    );
    const qid = await db.queryScalar(
      `SELECT TOP 1 Id FROM PackageQuestions WHERE AssessmentPackageId=${packageId} ORDER BY Id DESC`
    );
    const opts = ['P1', 'P2', 'P3', 'P4', 'P5'];
    for (let i = 0; i < 5; i++) {
      await execSql(
        `INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) ` +
        `VALUES (${qid}, N'${opts[i]}', ${i === 2 ? 1 : 0})`   // P3 benar
      );
    }

    // Buka Kelola Soal → klik Edit pada soal → AJAX populateEditForm → form prefill 5 baris.
    await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
    await page.waitForLoadState('networkidle');
    // Tombol Edit per-baris = <button onclick="loadEditForm(@item.Id)" title="Edit"> (ikon pensil, tanpa teks).
    await page.locator(`button[onclick="loadEditForm(${qid})"]`).click();

    // Form prefill: 5 baris opsi, teks P1..P5, radio P3 (correct_C) checked.
    await expect(page.locator('#formTitle'), 'form masuk mode edit').toContainText(/Edit Soal/i, { timeout: 6_000 });
    await expect.poll(async () => optionRowCount(page), { timeout: 6_000 }).toBe(5);
    expect(await optionLetters(page), 'huruf A–E (5 baris)').toEqual(['A', 'B', 'C', 'D', 'E']);
    expect(await page.locator('#option_A').inputValue(), 'opsi A = P1').toBe('P1');
    expect(await page.locator('#option_E').inputValue(), 'opsi E = P5').toBe('P5');
    expect(await page.locator('#correct_C').isChecked(), 'radio benar di P3 (C)').toBe(true);
  });

  // ── S7 (D-418-02): edit-shrink hapus opsi terjawab → .alert-danger (BUKAN 500) ──────────────────────
  test('S7: hapus opsi yang sudah dijawab → alert-danger "sudah dijawab" (bukan halaman 500)', async ({ page }) => {
    test.setTimeout(180_000);
    const title = `Pre Test OJT OPSIDINAMIS418 EDITSHRINK ${Date.now()}`;
    const assessmentId = await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // Seed soal MC 4-opsi (A benar) via form + 1 PackageUserResponse ke opsi B (dijawab peserta) via SQL.
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: '[OPSIDINAMIS418] Soal edit-shrink guard',
      options: ['EA', 'EB', 'EC', 'ED'], correctIndex: 0, score: 10,
    });
    const qids = await questionIdsOrdered(packageId);
    const qid = qids[qids.length - 1];
    const optBId = await db.queryScalar(`SELECT Id FROM PackageOptions WHERE PackageQuestionId=${qid} AND OptionText=N'EB'`);
    const sessionId = await db.queryScalar(`SELECT TOP 1 Id FROM AssessmentSessions WHERE Id=${assessmentId}`);
    // Insert response peserta ke opsi B → FK PackageUserResponse→PackageOption Restrict aktif.
    await execSql(
      `INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt) ` +
      `VALUES (${sessionId}, ${qid}, ${optBId}, GETDATE())`
    );

    // Buka edit soal → kosongkan teks opsi B → simpan → guard blok: alert-danger, BUKAN 500.
    await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
    await page.waitForLoadState('networkidle');
    await page.locator(`button[onclick="loadEditForm(${qid})"]`).click();
    await expect(page.locator('#formTitle')).toContainText(/Edit Soal/i, { timeout: 6_000 });
    await expect.poll(async () => optionRowCount(page), { timeout: 6_000 }).toBe(4);

    // Kosongkan teks opsi B (server tandai dihapus → terblok karena sudah dijawab).
    await page.locator('#option_B').fill('');
    const resp = await Promise.all([
      page.waitForResponse((r) => /\/Admin\/EditQuestion/i.test(r.url()) && r.request().method() === 'POST', { timeout: 15_000 }),
      page.locator(questionFormSelectors.submitBtn).click(),
    ]);
    // Server merespons 200/302 redirect (BUKAN 500).
    expect(resp[0].status(), 'EditQuestion POST tidak 500 (guard, bukan FK Restrict)').toBeLessThan(500);
    await page.waitForLoadState('networkidle');

    // alert-danger memuat "sudah dijawab" (guard D-418-02).
    const danger = page.locator('.alert-danger').first();
    await expect(danger, 'alert-danger muncul (bukan halaman error)').toBeVisible({ timeout: 6_000 });
    await expect(danger).toContainText(/sudah dijawab/i);

    // DB: opsi B TETAP ADA (tak terhapus) → soal masih 4 opsi.
    const optCount = await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${qid}`);
    expect(optCount, 'opsi B tak terhapus — soal masih 4 opsi (guard preserved)').toBe(4);
  });

  // ── S8 (backward-compat): soal 4-opsi lama create/render/preview identik (OPT-01 regresi) ───────────
  test('S8: backward-compat soal 4-opsi — create + render exam + preview "D." identik', async ({ page, browser }) => {
    test.setTimeout(240_000);
    const title = `Pre Test OJT OPSIDINAMIS418 BACKWARD4 ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // Buat soal 4-opsi via helper lama (addQuestionViaForm → #option_A..D + correct_A..D).
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice', text: '[OPSIDINAMIS418] Soal 4 opsi backward-compat',
      options: ['W', 'X', 'Y', 'Z'], correctIndex: 3, score: 10,
    });
    const qids = await questionIdsOrdered(packageId);
    const qid = qids[qids.length - 1];
    const optCount = await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${qid}`);
    expect(optCount, 'soal 4-opsi tersimpan (backward-compat)').toBe(4);
    const correctText = await queryStr(`SELECT TOP 1 OptionText FROM PackageOptions WHERE PackageQuestionId=${qid} AND IsCorrect=1`);
    expect(correctText, 'jawaban benar = Z (opsi ke-4)').toBe('Z');

    // PreviewPackage: opsi ke-4 "Z" ber-huruf "D." (bukan E/F/numerik).
    await page.goto(`/Admin/PreviewPackage?packageId=${packageId}`);
    await page.waitForLoadState('networkidle');
    const zLabel = page.locator('label.form-check-label', { hasText: 'Z' }).first();
    await expect(zLabel, 'opsi ke-4 "Z" ter-render').toBeVisible();
    expect((await zLabel.innerText()).trim(), 'opsi ke-4 ber-huruf "D." identik perilaku lama').toMatch(/^D\.\s/);

    // Render exam: huruf A–D tampil, tak ada E/F.
    const participant = await startExamAsParticipant(browser, title);
    try {
      const ep = participant.page;
      const card = ep.locator('[id^="qcard_"]', { hasText: 'Soal 4 opsi backward-compat' });
      await expect(card, 'kartu soal 4-opsi ter-render').toBeVisible({ timeout: 10_000 });
      await expect(card.locator('span.fw-bold', { hasText: /^D\.$/ }), 'huruf D. tampil').toBeVisible();
      expect(await card.locator('span.fw-bold', { hasText: /^E\.$/ }).count(), 'tak ada huruf E. (hanya 4 opsi)').toBe(0);
    } finally {
      await participant.close();
    }
  });
});
