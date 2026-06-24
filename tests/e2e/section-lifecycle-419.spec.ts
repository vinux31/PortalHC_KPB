// Phase 419 D-04.1 — Lifecycle Section inti UAT real-browser @5277 (PAG-04 + cross-feature).
// Membuktikan RUNTIME (lesson 354: Razor/JS WAJIB UAT browser) alur lengkap Section + opsi dinamis A–F +
// pagination + resume + EXPORT label Section, end-to-end via UI sungguhan:
//   1. Admin wizard create OJT startable + paket + 2 Section (Proses / Keselamatan StartNewPage).
//   2. Soal opsi DINAMIS: 1 soal 6-opsi (A–F, benar E) di Proses + 1 soal 5-opsi (A–E, benar E) di Keselamatan
//      + filler 4-opsi tiap Section. Assign Section via SQL (pola 416/417).
//   3. Peserta (rino) StartExam → ASSERT huruf A–F dinamis (E+F di kartu 6-opsi, E tanpa F di 5-opsi) +
//      header Section (Proses + Keselamatan) + pagination (Keselamatan StartNewPage → halaman baru).
//   4. Resume: seed LastActivePage>0 → re-enter → modal resume → landing halaman benar + toast.
//   5. Tandai sesi Completed (SQL) → admin export per-soal Excel → ASSERT band-header "Section 1: Proses"
//      + "Section 2: Keselamatan" di sheet "Detail Per Soal" (PAG-04, parse exceljs).
//
// Analog: tests/e2e/section-pagination.spec.ts (helper inline + lifecycle) + option-dynamic-418.spec.ts (A–F)
//   + curl-UAT export band-header (sesi ini). SEED_WORKFLOW: temporary + local-only, beforeAll BACKUP /
//   afterAll RESTORE. WAJIB --workers=1. App @5277 (Authentication__UseActiveDirectory=false) + SQLEXPRESS.
// Auth: admin@pertamina.com / 123456. Peserta: rino.prasetyo@pertamina.com (coachee).

import { test, expect, type Page, type Browser } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
import { questionFormSelectors as Q } from './helpers/wizardSelectors';

test.describe.configure({ mode: 'serial' });

let snapshotPath: string;

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

async function createOjtArriveMP(page: Page, title: string): Promise<number> {
  await login(page, 'admin');
  await createAssessmentViaWizard(page, {
    title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
    durationMinutes: 120, passPercentage: 50, allowAnswerReview: true, generateCertificate: false,
    participantEmails: ['rino.prasetyo@pertamina.com'], ewcdDate: tomorrow(), ewcdTime: '23:59',
  });
  await page.locator('#modal-manage-btn').click();
  await page.waitForLoadState('networkidle');
  const id = parseInt(new URL(page.url()).searchParams.get('assessmentId') ?? '0', 10);
  expect(id, 'assessmentId ter-parse').toBeGreaterThan(0);
  return id;
}

// Author 1 soal MC opsi-dinamis (5/6 opsi) via form ManagePackageQuestions (pola option-dynamic-418).
// Section di-assign via SQL setelahnya (tak pakai #sectionIdSelect supaya tak bergantung urutan create Section).
async function authorDynamicMc(
  page: Page, packageId: number, text: string, optionTexts: string[], correctLetter: string
): Promise<void> {
  await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
  await page.waitForLoadState('networkidle');
  await page.locator(Q.formCard).waitFor({ state: 'visible' });
  await page.selectOption(Q.questionType, 'MultipleChoice');
  await page.locator(Q.optionsSection).waitFor({ state: 'visible' });
  await page.fill(Q.questionText, text);
  // Form mulai 4 baris (A–D); tumbuhkan ke len opsi.
  const addBtn = page.locator(Q.addOptionBtn);
  for (let i = 4; i < optionTexts.length; i++) await addBtn.click();
  const ids = [Q.optionA, Q.optionB, Q.optionC, Q.optionD, Q.optionE, Q.optionF];
  for (let i = 0; i < optionTexts.length; i++) await page.fill(ids[i], optionTexts[i]);
  const correct: Record<string, string> = { A: Q.correctA, B: Q.correctB, C: Q.correctC, D: Q.correctD, E: Q.correctE, F: Q.correctF };
  await page.locator(correct[correctLetter]).check();
  await page.fill(Q.scoreValue, '10');
  await page.locator(Q.submitBtn).click();
  await page.waitForLoadState('networkidle');
  await expect(page.locator('.alert-success').first(), `soal "${text}" tersimpan`).toBeVisible({ timeout: 8_000 });
}

async function questionIdsOrdered(packageId: number): Promise<number[]> {
  const raw = await queryStr(
    `SELECT STRING_AGG(CAST(Id AS VARCHAR(12)), ',') WITHIN GROUP (ORDER BY [Order]) ` +
    `FROM PackageQuestions WHERE AssessmentPackageId = ${packageId}`
  );
  return raw.split(',').map((x) => parseInt(x.trim(), 10)).filter((x) => x > 0);
}

async function createSection(packageId: number, sectionNumber: number, name: string, startNewPage: boolean): Promise<number> {
  await execSql(
    `INSERT INTO AssessmentPackageSections (AssessmentPackageId, SectionNumber, Name, StartNewPage, ShuffleEnabled) ` +
    `VALUES (${packageId}, ${sectionNumber}, N'${name.replace(/'/g, "''")}', ${startNewPage ? 1 : 0}, 1)`
  );
  return db.queryScalar(
    `SELECT Id FROM AssessmentPackageSections WHERE AssessmentPackageId=${packageId} AND SectionNumber=${sectionNumber}`
  );
}
async function assignToSection(qids: number[], from: number, to: number, sectionId: number): Promise<void> {
  for (let i = from; i < to; i++) await execSql(`UPDATE PackageQuestions SET SectionId = ${sectionId} WHERE Id = ${qids[i]}`);
}

async function startExamAsParticipant(
  browser: Browser, title: string, dismissResume = true
): Promise<{ page: Page; sessionId: number; close: () => Promise<void> }> {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await login(page, 'coachee');
  await page.goto('/CMP/Assessment');
  const card = page.locator('.assessment-card', { hasText: title });
  await expect(card, `kartu "${title}" di lobby`).toBeVisible({ timeout: 10_000 });
  const startBtn = card.locator('.btn-start-standard');
  const resumeLink = card.locator('a:has-text("Resume")');
  page.once('dialog', (d) => d.accept());
  if (await startBtn.isVisible().catch(() => false)) await startBtn.click();
  else await resumeLink.first().click();
  await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });
  if (dismissResume) {
    const m = page.locator('#resumeConfirmModal');
    await m.waitFor({ state: 'visible', timeout: 4_000 }).catch(() => {});
    if (await m.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(m).not.toBeVisible({ timeout: 5_000 });
    }
  }
  const sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)?.[1] ?? '0', 10);
  expect(sessionId, 'sessionId dari URL').toBeGreaterThan(0);
  return { page, sessionId, close: () => ctx.close() };
}

const pageOfQ = (page: Page, qid: number) => page.evaluate((id) => {
  const c = document.getElementById('qcard_' + id);
  const p = c?.closest('div.exam-page');
  return p ? parseInt((p.id || '').replace('page_', ''), 10) : -1;
}, qid);

test.describe('Phase 419 D-04.1 — Lifecycle Section inti (Section + opsi A–F + pagination + resume + export)', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"))
      .replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre419life-${ts}.bak`;
    await db.backup(snapshotPath);
  });
  test.afterAll(async () => {
    if (!snapshotPath) return;
    let err: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs');
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { err = e; }
    if (err) throw err;
  });

  test('create → assign Section → ujian render A–F + header + pagination → resume → export label Section', async ({ page, browser }) => {
    test.setTimeout(360_000);
    const title = `Lifecycle Section 419 ${Date.now()}`;

    // 1. Wizard + paket.
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page, 'Paket A');

    // 2. Soal (urut Order): [0] 6-opsi Proses, [1] filler Proses, [2] 5-opsi Keselamatan, [3] filler Keselamatan.
    await authorDynamicMc(page, packageId, 'Soal 6-opsi (Proses)', ['Alpha', 'Bravo', 'Charlie', 'Delta', 'Echo', 'Foxtrot'], 'E');
    await addQuestionViaForm(page, packageId, { type: 'MultipleChoice', text: 'Filler Proses', options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10 });
    await authorDynamicMc(page, packageId, 'Soal 5-opsi (Keselamatan)', ['Alpha', 'Bravo', 'Charlie', 'Delta', 'Echo'], 'E');
    await addQuestionViaForm(page, packageId, { type: 'MultipleChoice', text: 'Filler Keselamatan', options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10 });

    const qids = await questionIdsOrdered(packageId);
    expect(qids.length, 'paket punya 4 soal').toBe(4);
    const secProses = await createSection(packageId, 1, 'Proses', false);
    const secKeselamatan = await createSection(packageId, 2, 'Keselamatan', true);   // StartNewPage → page-break
    await assignToSection(qids, 0, 2, secProses);       // qids[0]=6-opsi, qids[1]=filler
    await assignToSection(qids, 2, 4, secKeselamatan);  // qids[2]=5-opsi, qids[3]=filler

    // 3. Peserta StartExam → render A–F + header + pagination.
    const taker = await startExamAsParticipant(browser, title);
    const sessionId = taker.sessionId;
    try {
      const ep = taker.page;
      // Header Section (Proses page 0 visible, Keselamatan page-break hidden → assert attached).
      const headers = ep.locator('div.text-primary.fw-semibold');
      await expect(headers.filter({ hasText: 'Proses' }).first(), 'header "Proses"').toBeVisible();
      expect(await headers.filter({ hasText: 'Keselamatan' }).count(), 'header "Keselamatan" ter-render').toBeGreaterThanOrEqual(1);
      // A–F dinamis: kartu 6-opsi (Proses, page 0 visible) punya huruf E. + F.
      const card6 = ep.locator(`#qcard_${qids[0]}`);
      await expect(card6.locator('span.fw-bold', { hasText: /^E\.$/ }), 'opsi E pada soal 6-opsi').toBeVisible();
      await expect(card6.locator('span.fw-bold', { hasText: /^F\.$/ }), 'opsi F pada soal 6-opsi').toBeVisible();
      // Kartu 5-opsi (Keselamatan, hidden page) punya E. tapi TIDAK ada F.
      const card5 = ep.locator(`#qcard_${qids[2]}`);
      expect(await card5.locator('span.fw-bold', { hasText: /^E\.$/ }).count(), 'opsi E ada di soal 5-opsi').toBeGreaterThanOrEqual(1);
      expect(await card5.locator('span.fw-bold', { hasText: /^F\.$/ }).count(), 'opsi F TIDAK ada di soal 5-opsi').toBe(0);
      // Pagination: Keselamatan (StartNewPage) di halaman > Proses.
      const pageProses = await pageOfQ(ep, qids[0]);
      const pageKesel = await pageOfQ(ep, qids[2]);
      expect(pageProses, 'Proses di page 0').toBe(0);
      expect(pageKesel, 'Keselamatan (StartNewPage) di halaman baru > Proses').toBeGreaterThan(pageProses);
    } finally {
      await taker.close();
    }

    // 4. Resume: seed LastActivePage = halaman Keselamatan, re-enter → modal + landing + toast.
    const pageKeselDb = 1; // Keselamatan StartNewPage → page 1 (Proses 2 soal < perPage → page 0)
    const upd = await execSql(`UPDATE AssessmentSessions SET LastActivePage = ${pageKeselDb} WHERE Id = ${sessionId}`);
    expect(upd, 'LastActivePage ter-update').toBe(1);
    const resume = await startExamAsParticipant(browser, title, /* dismissResume */ false);
    try {
      const ep = resume.page;
      expect(resume.sessionId, 'resume sesi sama').toBe(sessionId);
      const modal = ep.locator('#resumeConfirmModal');
      await expect(modal, 'modal resume muncul').toBeVisible({ timeout: 8_000 });
      await ep.locator('#resumeConfirmBtn').click();
      await expect(modal, 'modal tertutup').not.toBeVisible({ timeout: 5_000 });
      await expect(ep.locator(`#page_${pageKeselDb}`), `landing #page_${pageKeselDb}`).toBeVisible({ timeout: 5_000 });
      await expect(ep.locator('#page_0'), 'page 0 hidden saat resume>0').toBeHidden();
      const toast = ep.locator('#resumeInfoToast .toast-body');
      await expect(toast, 'toast resume').toBeVisible({ timeout: 6_000 });
      await expect(toast).toContainText(/Lanjut dari soal no\.\s*\d+/);
    } finally {
      await resume.close();
    }

    // 5. Tandai sesi Completed (eligible utk export) → export Excel → assert band-header Section.
    const done = await execSql(
      `UPDATE AssessmentSessions SET Status='Completed', CompletedAt=GETDATE(), Score=80, IsPassed=1 WHERE Id=${sessionId}`
    );
    expect(done, 'sesi ditandai Completed').toBe(1);

    const params = new URLSearchParams({ title, category: 'OJT', scheduleDate: today() });
    const url = `/Admin/ExportAssessmentResults?${params.toString()}`;
    const resp = await page.request.get(url);
    expect(resp.status(), 'export HTTP 200').toBe(200);
    expect(resp.headers()['content-type'] ?? '', 'export Excel MIME').toContain('spreadsheetml');
    const buf = await resp.body();
    // Unzip xlsx + scan xl/sharedStrings.xml utk literal band-header Section (pola curl-UAT sesi ini;
    // exceljs SAX-load rapuh di runner ini → JSZip langsung, dep exceljs yang sudah ada).
    const JSZipMod: any = await import('jszip');
    const JSZip = JSZipMod.default ?? JSZipMod;
    const zip = await JSZip.loadAsync(buf);
    const shared = (await zip.file('xl/sharedStrings.xml')?.async('string')) ?? '';
    expect(shared.length, 'sharedStrings.xml ter-ekstrak').toBeGreaterThan(0);
    expect(shared.includes('Section 1: Proses'), 'band-header "Section 1: Proses" di Excel').toBe(true);
    expect(shared.includes('Section 2: Keselamatan'), 'band-header "Section 2: Keselamatan" di Excel').toBe(true);
  });
});
