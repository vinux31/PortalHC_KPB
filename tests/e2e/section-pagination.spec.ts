// Phase 417 (PAG-01/02/03) — UAT e2e Section Pagination (render ujian section-aware) real-browser.
// Membuktikan RUNTIME (lesson 354: Razor/JS WAJIB UAT browser — unit murni tak menangkap render DOM,
// navigator grouping, resume toast, page-break visual, backward-compat flat) bahwa engine pagination
// section-aware (Plan 01 SectionPaginator.ComputePages/ClampResumePage) + wiring controller+view (Plan 02)
// benar saat ujian sungguhan di /CMP/StartExam:
//   S1 (header on section change, PAG-01): saat berganti Section, header NAMA Section (tanpa "Section N:")
//       di-render `div.text-primary.fw-semibold` di atas grup soal Section. ≥2 header (2 Section).
//   S2 ("(lanjutan)" auto-split, PAG-02): Section A >10 soal → halaman ke-2 Section A menampilkan header
//       Section + `span:has-text("(lanjutan)")`. Soal #11 berada di `#page_>0` dengan header "(lanjutan)".
//   S3 (StartNewPage page-break, PAG-02): Section B (StartNewPage=1) → soal pertama Section B berada di
//       `exam-page` div BARU (mis. #page_1), bukan menyambung halaman Section A meski belum penuh.
//   S4 (navigator grouping, D-417-03): #panelNumbers punya label grup full-width (style gridColumn 1/-1)
//       berisi nama Section, di atas badge grup. ≥2 label (2 Section).
//   S5 (resume landing + toast, PAG-03/D-417-06): set AssessmentSessions.LastActivePage>0 via SQL pada sesi
//       InProgress, re-visit StartExam → isResume=true → modal resume → klik "Lanjutkan" → halaman aktif =
//       RESUME_PAGE terhitung (bukan 0) DAN toast #resumeInfoToast "Lanjut dari soal no. X".
//   S6 (no-Section flat backward-compat, PAG-01): assessment TANPA Section (semua SectionId=null) → StartExam
//       → TIDAK ada header Section, navigator flat (tanpa label gridColumn), indikator "Halaman n/total"
//       (tanpa nama Section). Smoke render tanpa error/crash.
//   S7 (admin quick-button "Semua Section mulai halaman baru", Phase 415 VERIFY-ONLY): login admin → klik
//       tombol di ManagePackageQuestions → konfirmasi SEMUA AssessmentPackageSections.StartNewPage=1 (SQL) →
//       login coachee → StartExam → SETIAP Section (kecuali yang pertama) mulai di exam-page div baru.
//
// Template/analog: tests/e2e/scoped-shuffle.spec.ts (Phase 416 — analog LANGSUNG, sama domain Section, sama
//   jalur StartExam). Clone: mode:'serial' + DB backup/restore beforeAll/afterAll + createAssessmentViaWizard
//   + createDefaultPackage + addQuestionViaForm + seed Section via SQL UPDATE/INSERT pada record baru wizard.
//   Ambil ujian peserta: login coachee → /CMP/Assessment → .btn-start-standard → StartExam (dismiss resume modal).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE + unlink.
// PRECONDITION run: app di http://localhost:5277 (Authentication__UseActiveDirectory=false dotnet run)
//   + DB lokal HcPortalDB_Dev. WAJIB --workers=1 (playwright.config fullyParallel:false, DB isolation).
//   Bila browser not found → cd tests; npx playwright install chromium.
//
// Auth: admin@pertamina.com / 123456 (dev lokal — JANGAN staging/prod). Peserta: rino.prasetyo@pertamina.com
//   (coachee) — di-add oleh createAssessmentViaWizard via participantEmails.

import { test, expect, type Page, type Browser } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

test.describe.configure({ mode: 'serial' });

let snapshotPath: string;

// Jadwal: HARI INI supaya assessment langsung Open/startable. EWCD = besok supaya jendela ujian terbuka.
const fmtLocal = (d: Date) => {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
};
const today = () => fmtLocal(new Date());
const tomorrow = () => { const d = new Date(); d.setDate(d.getDate() + 1); return fmtLocal(d); };

// Jalankan UPDATE/INSERT/DDL via sqlcmd (localhost-guard di dbSnapshot). queryScalar membungkus
// `SET NOCOUNT ON; <sql>` lalu append `SELECT @@ROWCOUNT` → output numerik (rowcount).
async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}

// queryString untuk ambil 1 nilai string (mis. STRING_AGG id → 1 baris).
async function queryStr(sql: string): Promise<string> {
  return db.queryString(`SET NOCOUNT ON; ${sql}`);
}

// login admin + wizard-create 1 assessment OJT startable + arrive di /Admin/ManagePackages.
// Return assessmentId (di-parse dari query string URL setelah dismiss success modal).
async function createOjtArriveMP(page: Page, title: string, doLogin = true): Promise<number> {
  if (doLogin) await login(page, 'admin');
  await createAssessmentViaWizard(page, {
    title,
    category: 'OJT',
    scheduleDate: today(),       // startable hari ini
    scheduleTime: '00:01',
    durationMinutes: 120,        // longgar — beberapa skenario punya banyak soal
    passPercentage: 50,
    allowAnswerReview: true,
    generateCertificate: false,
    participantEmails: ['rino.prasetyo@pertamina.com'],
    ewcdDate: tomorrow(),        // jendela ujian terbuka (besok 23:59)
    ewcdTime: '23:59',
  });
  // Dismiss static success modal → /Admin/ManagePackages?assessmentId={id}
  await page.locator('#modal-manage-btn').click();
  await page.waitForLoadState('networkidle');
  const id = parseInt(new URL(page.url()).searchParams.get('assessmentId') ?? '0', 10);
  expect(id, 'assessmentId ter-parse dari URL ManagePackages').toBeGreaterThan(0);
  return id;
}

// Ambil daftar PackageQuestion.Id (urut Order) untuk paket → dipakai map ke Section + assign.
async function questionIdsOrdered(packageId: number): Promise<number[]> {
  const raw = await queryStr(
    `SELECT STRING_AGG(CAST(Id AS VARCHAR(12)), ',') WITHIN GROUP (ORDER BY [Order]) ` +
    `FROM PackageQuestions WHERE AssessmentPackageId = ${packageId}`
  );
  return raw.split(',').map((x) => parseInt(x.trim(), 10)).filter((x) => x > 0);
}

// Buat 1 Section pada paket (StartNewPage param), return Section.Id.
async function createSection(
  packageId: number,
  sectionNumber: number,
  name: string,
  startNewPage: boolean
): Promise<number> {
  await execSql(
    `INSERT INTO AssessmentPackageSections (AssessmentPackageId, SectionNumber, Name, StartNewPage, ShuffleEnabled) ` +
    `VALUES (${packageId}, ${sectionNumber}, N'${name.replace(/'/g, "''")}', ${startNewPage ? 1 : 0}, 0)`
  );
  return db.queryScalar(
    `SELECT Id FROM AssessmentPackageSections WHERE AssessmentPackageId=${packageId} AND SectionNumber=${sectionNumber}`
  );
}

// Assign rentang soal (urut Order, inklusif index) ke Section.
async function assignToSection(qids: number[], from: number, to: number, sectionId: number): Promise<void> {
  for (let i = from; i < to; i++) {
    await execSql(`UPDATE PackageQuestions SET SectionId = ${sectionId} WHERE Id = ${qids[i]}`);
  }
}

// Peserta (coachee rino) login + mulai ujian standard via lobby → return { page, sessionId }.
// Pakai CONTEXT BARU (cookie terpisah): page utama dipakai admin sehingga masih authenticated admin.
// Caller WAJIB close. dismissResume=true → dismiss resume modal bila muncul (untuk skenario non-resume).
async function startExamAsParticipant(
  browser: Browser,
  title: string,
  dismissResume = true
): Promise<{ page: Page; sessionId: number; close: () => Promise<void> }> {
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

  if (dismissResume) {
    const resumeModal = page.locator('#resumeConfirmModal');
    await resumeModal.waitFor({ state: 'visible', timeout: 4_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click();
      await expect(resumeModal).not.toBeVisible({ timeout: 5_000 });
    }
  }

  const sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)?.[1] ?? '0', 10);
  expect(sessionId, 'sessionId dari URL StartExam').toBeGreaterThan(0);
  return { page, sessionId, close: () => ctx.close() };
}

// Urutan exam-page div terlihat di DOM: id "page_0","page_1",... → array index numerik.
async function examPageIds(page: Page): Promise<number[]> {
  return page.locator('div.exam-page[id^="page_"]').evaluateAll((els) =>
    els.map((e) => parseInt((e.id || '').replace('page_', ''), 10)).filter((x) => !Number.isNaN(x))
  );
}

// Untuk page div tertentu, daftar PackageQuestion.Id (urut DOM) dari qcard_{id} di dalamnya.
async function qcardIdsOnPage(page: Page, pageIndex: number): Promise<number[]> {
  return page.locator(`#page_${pageIndex} [id^="qcard_"]`).evaluateAll((els) =>
    els.map((e) => parseInt((e.id || '').replace('qcard_', ''), 10)).filter((x) => x > 0)
  );
}

test.describe('Phase 417 — Section Pagination (render ujian section-aware) UAT e2e', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre417-${ts}.bak`;
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

  // ── S1+S2+S3+S4: header / "(lanjutan)" / StartNewPage page-break / navigator grouping ────────────────
  // Satu assessment "kaya": Section A = 12 soal (auto-split per 10) + Section B (StartNewPage=1) = 4 soal.
  // Reuse 1 sesi peserta untuk meng-assert keempat skenario render (efisien; render statis sekali muat).
  test('S1-S4: header on section change + "(lanjutan)" auto-split + StartNewPage page-break + navigator grouping', async ({ page, browser }) => {
    test.setTimeout(300_000);
    const title = `Pre Test OJT PAGINASI417 RICH ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // 16 soal: 12 Section A (Pompa) untuk auto-split per-10, 4 Section B (Valve) StartNewPage=1.
    for (let i = 0; i < 12; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `S1 Pompa #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    for (let i = 0; i < 4; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `S1 Valve #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    const qids = await questionIdsOrdered(packageId);
    expect(qids.length, 'paket RICH punya 16 soal').toBe(16);

    // Section A (Pompa, StartNewPage=0) = 12 soal pertama; Section B (Valve, StartNewPage=1) = 4 berikut.
    const secA = await createSection(packageId, 1, 'Pompa', false);
    const secB = await createSection(packageId, 2, 'Valve', true);
    await assignToSection(qids, 0, 12, secA);
    await assignToSection(qids, 12, 16, secB);

    const participant = await startExamAsParticipant(browser, title);
    try {
      const ep = participant.page;

      // S1 — header NAMA Section (tanpa "Section N:") di-render. Pompa + Valve hadir, tanpa nomor prefix.
      // CATATAN: header berada DI DALAM exam-page div; hanya #page_0 yang display:block saat muat. Header
      //   Section B (Valve) di halaman page-break (hidden) — assert KEHADIRAN (count/attached), bukan visible.
      const sectionHeaders = ep.locator('div.text-primary.fw-semibold');
      // Section A muncul (page 0), continuation (page 1), Section B (page 2). ≥3 header total.
      expect(await sectionHeaders.count(), 'minimal 3 header Section (A + A-lanjutan + B)').toBeGreaterThanOrEqual(3);
      // Header Section A "Pompa" ada di page 0 (aktif) → visible.
      await expect(sectionHeaders.filter({ hasText: 'Pompa' }).first(), 'header "Pompa" hadir (page 0)').toBeVisible();
      // Header Section B "Valve" ada di halaman page-break (display:none) → assert attached (di DOM), bukan visible.
      expect(await sectionHeaders.filter({ hasText: 'Valve' }).count(), 'header "Valve" ter-render di DOM').toBeGreaterThanOrEqual(1);
      // Nama saja, tak ada prefix "Section 1:" / "Section 2:".
      expect(await sectionHeaders.first().innerText(), 'header tanpa prefix "Section N:"').not.toMatch(/Section\s*\d+\s*:/);

      // S2 — "(lanjutan)": Section A 12 soal → halaman ke-2 menampilkan header Pompa + "(lanjutan)".
      // (penanda ada di page 1 yang hidden → assert count di DOM, bukan visible)
      const lanjutanMarker = ep.locator('span:has-text("(lanjutan)")');
      expect(await lanjutanMarker.count(), 'minimal 1 penanda "(lanjutan)" (auto-split Section A)').toBeGreaterThanOrEqual(1);
      // Soal #11 (index 10, qid ke-11) ada di halaman > 0 (auto-split) dengan header continuation.
      const pageOfQ11 = await ep.evaluate((qid) => {
        const card = document.getElementById('qcard_' + qid);
        const pageDiv = card?.closest('div.exam-page');
        return pageDiv ? parseInt((pageDiv.id || '').replace('page_', ''), 10) : -1;
      }, qids[10]);
      expect(pageOfQ11, 'soal #11 Section A berada di halaman > 0 (auto-split per-10)').toBeGreaterThan(0);

      // S3 — StartNewPage page-break: soal pertama Section B (qids[12]) berada di exam-page div BARU,
      // di halaman > halaman soal terakhir Section A (qids[11]) → page-break, tak menyambung.
      const pageOfFirstB = await ep.evaluate((qid) => {
        const card = document.getElementById('qcard_' + qid);
        const pageDiv = card?.closest('div.exam-page');
        return pageDiv ? parseInt((pageDiv.id || '').replace('page_', ''), 10) : -1;
      }, qids[12]);
      const pageOfLastA = await ep.evaluate((qid) => {
        const card = document.getElementById('qcard_' + qid);
        const pageDiv = card?.closest('div.exam-page');
        return pageDiv ? parseInt((pageDiv.id || '').replace('page_', ''), 10) : -1;
      }, qids[11]);
      expect(pageOfFirstB, 'Section B (StartNewPage) mulai di halaman baru, > halaman terakhir Section A')
        .toBeGreaterThan(pageOfLastA);
      // Section B di halaman SENDIRI: tak ada qcard Section A di halaman pertama Section B.
      const bPageQcards = await qcardIdsOnPage(ep, pageOfFirstB);
      const secAQids = new Set(qids.slice(0, 12));
      expect(bPageQcards.some((q) => secAQids.has(q)), 'halaman Section B tak berisi soal Section A (page-break)')
        .toBe(false);

      // S4 — navigator grouping: #panelNumbers punya label grup full-width (gridColumn 1/-1) Pompa + Valve.
      // updatePanel() dipanggil saat init (no-resume) → label sudah ter-render.
      const groupLabels = ep.locator('#panelNumbers > div[style*="grid-column"]');
      // Catatan: browser menormalkan inline style "1 / -1" → "grid-column: 1 / -1". Cek count ≥2 (2 Section).
      const labelCount = await groupLabels.count();
      expect(labelCount, 'minimal 2 label grup Section di navigator (Pompa + Valve)').toBeGreaterThanOrEqual(2);
      await expect(groupLabels.filter({ hasText: 'Pompa' }).first(), 'label navigator "Pompa"').toBeVisible();
      await expect(groupLabels.filter({ hasText: 'Valve' }).first(), 'label navigator "Valve"').toBeVisible();
    } finally {
      await participant.close();
    }
  });

  // ── S5: resume landing page + toast (PAG-03 / D-417-06) ──────────────────────────────────────────────
  test('S5: resume mendarat di halaman terhitung > 0 + toast "Lanjut dari soal no. X"', async ({ page, browser }) => {
    test.setTimeout(300_000);
    const title = `Pre Test OJT PAGINASI417 RESUME ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // 8 soal: Section A (Awal) 4 soal + Section B (Lanjut) StartNewPage=1 → soal Section B di halaman > 0.
    for (let i = 0; i < 8; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `S5 Soal #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    const qids = await questionIdsOrdered(packageId);
    expect(qids.length, 'paket RESUME punya 8 soal').toBe(8);
    const secA = await createSection(packageId, 1, 'Awal', false);
    const secB = await createSection(packageId, 2, 'Lanjut', true);   // StartNewPage → page-break ke halaman 1
    await assignToSection(qids, 0, 4, secA);
    await assignToSection(qids, 4, 8, secB);

    // 1) Peserta START ujian pertama kali (justStarted → isResume=false; tak ada modal). Sesi → InProgress.
    const first = await startExamAsParticipant(browser, title);
    const sessionId = first.sessionId;
    // Konfirmasi halaman Section B = page 1 (page-break). qids[4] (soal pertama Section B) di halaman > 0.
    const pageOfB = await first.page.evaluate((qid) => {
      const card = document.getElementById('qcard_' + qid);
      const pageDiv = card?.closest('div.exam-page');
      return pageDiv ? parseInt((pageDiv.id || '').replace('page_', ''), 10) : -1;
    }, qids[4]);
    expect(pageOfB, 'Section B (StartNewPage) di halaman > 0').toBeGreaterThan(0);
    await first.close();

    // 2) Seed LastActivePage = halaman Section B (>0) pada sesi InProgress (migrasi-laten: page-index global).
    const upd = await execSql(
      `UPDATE AssessmentSessions SET LastActivePage = ${pageOfB} WHERE Id = ${sessionId}`
    );
    expect(upd, 'LastActivePage ter-update di sesi InProgress').toBe(1);

    // 3) Re-visit StartExam (sesi sudah InProgress → justStarted=false → isResume=true → modal resume).
    //    JANGAN dismiss modal di helper; kita klik "Lanjutkan" manual lalu assert landing + toast.
    const resume = await startExamAsParticipant(browser, title, /* dismissResume */ false);
    try {
      const ep = resume.page;
      expect(resume.sessionId, 'resume ke sesi yang sama').toBe(sessionId);

      const modal = ep.locator('#resumeConfirmModal');
      await expect(modal, 'modal resume muncul (isResume=true)').toBeVisible({ timeout: 8_000 });
      await ep.locator('#resumeConfirmBtn').click();
      await expect(modal, 'modal resume tertutup setelah Lanjutkan').not.toBeVisible({ timeout: 5_000 });

      // Halaman aktif = RESUME_PAGE terhitung (> 0): page_{pageOfB} display:block, page_0 hidden.
      await expect(ep.locator(`#page_${pageOfB}`), `halaman resume (#page_${pageOfB}) tampil`).toBeVisible({ timeout: 5_000 });
      await expect(ep.locator('#page_0'), 'halaman 0 disembunyikan saat resume > 0').toBeHidden();

      // Toast informatif "Lanjut dari soal no. X".
      const toast = ep.locator('#resumeInfoToast .toast-body');
      await expect(toast, 'toast resume #resumeInfoToast muncul').toBeVisible({ timeout: 6_000 });
      await expect(toast, 'toast berisi "Lanjut dari soal no."').toContainText(/Lanjut dari soal no\.\s*\d+/);
    } finally {
      await resume.close();
    }
  });

  // ── S6: no-Section flat backward-compat (PAG-01) ────────────────────────────────────────────────────
  test('S6: backward-compat no-Section — flat tanpa header Section, navigator flat, indikator tanpa nama Section', async ({ page, browser }) => {
    test.setTimeout(240_000);
    const title = `Pre Test OJT PAGINASI417 FLAT ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // 6 soal TANPA Section (SectionId tetap null) → perilaku flat lama identik.
    for (let i = 0; i < 6; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `S6 Flat #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    const qids = await questionIdsOrdered(packageId);
    expect(qids.length, 'paket FLAT punya 6 soal').toBe(6);
    const noSections = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId = ${packageId}`
    );
    expect(noSections, 'tidak ada Section di paket FLAT').toBe(0);

    const participant = await startExamAsParticipant(browser, title);
    try {
      const ep = participant.page;

      // Render tanpa error: semua 6 soal ter-render.
      const qcardCount = await ep.locator('[id^="qcard_"]').count();
      expect(qcardCount, 'semua 6 soal dirender (tak ada error render)').toBe(6);

      // TIDAK ada header Section (div.text-primary.fw-semibold di area soal).
      expect(await ep.locator('div.text-primary.fw-semibold').count(), 'tak ada header Section (flat)').toBe(0);
      // TIDAK ada penanda "(lanjutan)".
      expect(await ep.locator('span:has-text("(lanjutan)")').count(), 'tak ada penanda lanjutan (flat)').toBe(0);

      // Navigator flat: tak ada label grup (gridColumn 1/-1).
      expect(
        await ep.locator('#panelNumbers > div[style*="grid-column"]').count(),
        'navigator flat — tak ada label grup Section'
      ).toBe(0);

      // Indikator "Halaman n/total" tanpa nama Section (tanpa " — ").
      const indicator = ep.locator('#pageSectionIndicator');
      await expect(indicator, 'indikator halaman muncul').toBeVisible();
      const indText = (await indicator.innerText()).trim();
      expect(indText, 'indikator format "Halaman n/total" tanpa nama Section').toMatch(/^Halaman\s+\d+\/\d+$/);
    } finally {
      await participant.close();
    }
  });

  // ── S7: admin quick-button "Semua Section mulai halaman baru" (Phase 415 VERIFY-ONLY) → page-break per section ──
  test('S7: quick-button SetAllSectionsNewPage → semua StartNewPage=1 → page-break per section di StartExam', async ({ page, browser }) => {
    test.setTimeout(300_000);
    const title = `Pre Test OJT PAGINASI417 QUICKBTN ${Date.now()}`;
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page);

    // 6 soal: 2 Section A + 2 Section B + 2 Section C. Awalnya StartNewPage=0 semua (mengalir 1 halaman).
    for (let i = 0; i < 6; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `S7 Soal #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    const qids = await questionIdsOrdered(packageId);
    expect(qids.length, 'paket QUICKBTN punya 6 soal').toBe(6);
    const secA = await createSection(packageId, 1, 'Bagian A', false);
    const secB = await createSection(packageId, 2, 'Bagian B', false);
    const secC = await createSection(packageId, 3, 'Bagian C', false);
    await assignToSection(qids, 0, 2, secA);
    await assignToSection(qids, 2, 4, secB);
    await assignToSection(qids, 4, 6, secC);

    // Admin buka Kelola Soal → klik quick-button "Semua Section mulai halaman baru" (Phase 415, VERIFY-ONLY).
    await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
    await page.waitForLoadState('networkidle');
    const quickBtn = page.locator('button:has-text("Semua Section mulai halaman baru")');
    await expect(quickBtn, 'tombol quick-button hadir (Phase 415)').toBeVisible();
    await quickBtn.click();
    await page.waitForLoadState('networkidle');

    // Konfirmasi DB: SEMUA Section StartNewPage=1 (action SetAllSectionsNewPage).
    const startNewPageOn = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId=${packageId} AND StartNewPage=1`
    );
    const totalSections = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentPackageSections WHERE AssessmentPackageId=${packageId}`
    );
    expect(totalSections, 'paket punya 3 Section').toBe(3);
    expect(startNewPageOn, 'SEMUA Section StartNewPage=1 setelah quick-button').toBe(totalSections);

    // Peserta StartExam → SETIAP Section (kecuali yang pertama) mulai di exam-page div baru (page-break).
    const participant = await startExamAsParticipant(browser, title);
    try {
      const ep = participant.page;
      const pageA = await ep.evaluate((qid) => {
        const c = document.getElementById('qcard_' + qid);
        const p = c?.closest('div.exam-page');
        return p ? parseInt((p.id || '').replace('page_', ''), 10) : -1;
      }, qids[0]);
      const pageB = await ep.evaluate((qid) => {
        const c = document.getElementById('qcard_' + qid);
        const p = c?.closest('div.exam-page');
        return p ? parseInt((p.id || '').replace('page_', ''), 10) : -1;
      }, qids[2]);
      const pageC = await ep.evaluate((qid) => {
        const c = document.getElementById('qcard_' + qid);
        const p = c?.closest('div.exam-page');
        return p ? parseInt((p.id || '').replace('page_', ''), 10) : -1;
      }, qids[4]);

      // Section A pertama tetap di page 0 (backward-compat: tak paksa page-break di awal).
      expect(pageA, 'Section A (pertama) di page 0').toBe(0);
      // Section B & C masing-masing di halaman BARU (page-break per section).
      expect(pageB, 'Section B mulai di halaman baru (> Section A)').toBeGreaterThan(pageA);
      expect(pageC, 'Section C mulai di halaman baru (> Section B)').toBeGreaterThan(pageB);
      // 3 halaman terpisah (1 section per halaman, masing-masing 2 soal < perPage).
      const pages = await examPageIds(ep);
      expect(pages.length, 'minimal 3 exam-page div (page-break per section)').toBeGreaterThanOrEqual(3);
    } finally {
      await participant.close();
    }
  });
});
