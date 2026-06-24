// Phase 419 D-04.4 — Add/Remove peserta LIVE saat ujian ber-Section + paginated aktif (v32.5 x Section/Pagination).
// UAT real-browser @5277. Membuktikan RUNTIME (lesson 354/412/413: Razor/JS/SignalR WAJIB UAT browser —
// build+grep+runtime-smoke tak menangkap handler-attach mati / drift assignment) bahwa:
//   D-04.4 inti: tambah peserta LIVE saat ujian ber-Section + pagination (Phase 416/417) aktif →
//     peserta baru dapat EAGER per-section assignment (ShuffledQuestionIds blok kontigu per Section,
//     identik sibling — CreateEagerAssignmentsAsync memuat q.Section + ShuffleEngine.BuildQuestionAssignment),
//     peserta yang resume TAK terganggu (ShuffledQuestionIds-nya tetap), pagination + header Section benar,
//     dan baris SignalR live MUNCUL di tabel monitoring TANPA reload. Lalu hapus + restore peserta baru.
//
// Analog/PRIMARY: tests/e2e/flexible-participant-412.spec.ts (add/remove/restore + SignalR, multi-context,
//   buildMonitoringUrl/waitHubConnected/openRowHapusModal/picker AddParticipantsLive/intercept added[0].id/
//   live-row assert/light modal/.btn-restore-peserta). Plus tests/e2e/section-lifecycle-419.spec.ts (lifecycle
//   backup/restore + createOjtArriveMP/createDefaultPackage/createSection/assignToSection/startExamAsParticipant/
//   pageOfQ) + tests/e2e/section-pagination.spec.ts (seed 12+4 Section A 'Pompa' SNP0 + Section B 'Valve' SNP1) +
//   tests/e2e/scoped-shuffle.spec.ts (assertContiguousSectionBlocks + questionSectionMap, sentinel 0 "Lainnya").
//
// SELEKTOR (terkonfirmasi Views/Admin/AssessmentMonitoringDetail.cshtml + Controllers/AssessmentAdminController.cs):
//   - Add: #btnTambahPeserta (data-bs-toggle → #tambahPesertaModal; show.bs.modal AJAX GetEligibleParticipantsToAdd)
//          → .tambah-peserta-check[value="<iwan3 User.Id>"] (value = u.Id, controller :2322) → #btnKonfirmasiTambah.
//          POST /Admin/AddParticipantsLive → body.added[0].id = AssessmentSession.Id (controller :2510-2516).
//          iwan3 User.Id di-resolve via `SELECT Id FROM Users WHERE Email='iwan3@pertamina.com'` (tabel Users —
//          controller GetEligibleParticipantsToAdd query _context.Users, BUKAN AspNetUsers; verified :2319).
//   - Eager UPA: SELECT ShuffledQuestionIds FROM UserPackageAssignments WHERE AssessmentSessionId=<addedSessionId>
//          (CreateEagerAssignmentsAsync :2562-2567 keyed by s.Id; engine sama StartExam → blok-per-section).
//   - Live row: tbody:not(#tbodyRemoved) tr[data-session-id="<addedSessionId>"] muncul TANPA reload.
//   - Remove (Not started + no cert) → modal RINGAN #hapusPesertaLightModal → #btnHapusLightKonfirmasi.
//   - Restore: #tbodyRemoved → expand #panelPesertaDikeluarkanHeader → .btn-restore-peserta (1-klik, D-04).
//   - Active count WAJIB exclude removed: tbody:not(#tbodyRemoved) tr[data-session-id] (Pitfall 2).
//
// SEED_WORKFLOW (CLAUDE.md + docs/SEED_WORKFLOW.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev →
//   afterAll RESTORE + unlink. PRECONDITION run: app @http://localhost:5277 (Authentication__UseActiveDirectory=
//   false dotnet run) + SQLEXPRESS hidup. WAJIB --workers=1 (NTLM loopback + DB isolation).
// Auth: admin@pertamina.com / 123456. Peserta resume = rino.prasetyo@pertamina.com (coachee). Peserta live ditambah
//   = iwan3@pertamina.com (coachee2) — server-side via AddParticipantsLive (BUKAN di browser; multi-context).

import { test, expect, type Page, type Browser } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { accounts } from '../helpers/accounts';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

test.describe.configure({ mode: 'serial' });

let snapshotPath: string;

// Jadwal HARI INI supaya assessment langsung Open/startable; EWCD = besok supaya jendela ujian terbuka
// (AddParticipantsLive window-guard butuh jendela ujian aktif).
const fmtLocal = (d: Date) => {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
};
const today = () => fmtLocal(new Date());
const tomorrow = () => { const d = new Date(); d.setDate(d.getDate() + 1); return fmtLocal(d); };

// sqlcmd helpers (localhost-guard di dbSnapshot). queryScalar membungkus `SET NOCOUNT ON; <sql>` + @@ROWCOUNT.
async function execSql(sql: string): Promise<number> {
  return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}
async function queryStr(sql: string): Promise<string> {
  return db.queryString(`SET NOCOUNT ON; ${sql}`);
}

// ── Section/wizard helpers (copy section-lifecycle-419 / section-pagination) ──────────────────────────────
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

async function questionIdsOrdered(packageId: number): Promise<number[]> {
  const raw = await queryStr(
    `SELECT STRING_AGG(CAST(Id AS VARCHAR(12)), ',') WITHIN GROUP (ORDER BY [Order]) ` +
    `FROM PackageQuestions WHERE AssessmentPackageId = ${packageId}`
  );
  return raw.split(',').map((x) => parseInt(x.trim(), 10)).filter((x) => x > 0);
}

// Map setiap PackageQuestion.Id → SectionId (null → 0 sentinel "Lainnya"). qToSection untuk assert blok kontigu.
// (Pola persis scoped-shuffle.spec.ts:questionSectionMap — kunci grup = SectionId DB; sentinel 0 = section null.)
async function questionSectionMap(packageId: number): Promise<Map<number, number>> {
  const raw = await queryStr(
    `SELECT STRING_AGG(CAST(Id AS VARCHAR(12)) + ':' + CAST(ISNULL(SectionId,0) AS VARCHAR(12)), ',') ` +
    `FROM PackageQuestions WHERE AssessmentPackageId = ${packageId}`
  );
  const map = new Map<number, number>();
  for (const pair of raw.split(',')) {
    const [qid, sid] = pair.split(':').map((x) => parseInt(x.trim(), 10));
    if (qid > 0) map.set(qid, sid);
  }
  return map;
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

// Peserta (coachee rino) login + mulai ujian standard via lobby → { page, sessionId, close }. Context BARU
// (cookie terpisah dari admin). dismissResume=true → dismiss modal resume bila muncul.
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
  expect(sessionId, 'sessionId dari URL StartExam').toBeGreaterThan(0);
  return { page, sessionId, close: () => ctx.close() };
}

// Halaman exam-page (page_N index) untuk soal tertentu (qid) → -1 bila tak ditemukan. (copy pageOfQ.)
const pageOfQ = (page: Page, qid: number) => page.evaluate((id) => {
  const c = document.getElementById('qcard_' + id);
  const p = c?.closest('div.exam-page');
  return p ? parseInt((p.id || '').replace('page_', ''), 10) : -1;
}, qid);

// ── Monitoring + SignalR helpers (copy flexible-participant-412) ──────────────────────────────────────────
function buildMonitoringUrl(title: string, category: string, scheduleDate: string): string {
  const params = new URLSearchParams({ title, category, scheduleDate });
  return `/Admin/AssessmentMonitoringDetail?${params.toString()}`;
}

// Tunggu SignalR hub Connected (Pitfall 3 — fire aksi sebelum Connected = false-fail).
async function waitHubConnected(page: Page, timeout = 15_000): Promise<void> {
  await page.waitForFunction(
    () => {
      const w = window as unknown as { assessmentHub?: { state?: string } };
      return w.assessmentHub?.state === 'Connected';
    },
    undefined,
    { timeout }
  );
}

// Buka dropdown ⋮ baris lalu klik item "Hapus Peserta" (.btn-hapus-peserta di dalam dropdown Bootstrap).
async function openRowHapusModal(page: Page, sessionId: number): Promise<void> {
  const row = page.locator(`tr[data-session-id="${sessionId}"]`);
  await row.locator('.dropdown button[data-bs-toggle="dropdown"]').first().click();
  const hapusItem = row.locator('.btn-hapus-peserta').first();
  await hapusItem.waitFor({ state: 'visible', timeout: 8_000 });
  await hapusItem.click();
}

// Assert urutan `order` (PackageQuestion.Id) membentuk blok-blok kontigu per Section (tak interleave), dengan
// "Lainnya" (sentinel 0) SELALU blok terakhir bila ada (D-15). Mirror scoped-shuffle.spec.ts (PROVEN). Memetakan
// tiap qid → nomor grup section (via qToSection); urutan nomor section non-decreasing dalam known sections +
// "Lainnya" terakhir = blok kontigu tanpa interleave.
function assertContiguousSectionBlocks(order: number[], qToSection: Map<number, number>): void {
  expect(order.length, 'urutan soal tidak kosong').toBeGreaterThan(0);
  const seenSections: number[] = [];
  let prev: number | null = null;
  for (const qid of order) {
    const sid = qToSection.get(qid);
    expect(sid, `SectionId untuk qid ${qid} ter-map`).not.toBeUndefined();
    if (sid !== prev) {
      // Transisi ke Section baru: Section ini belum pernah muncul (kalau sudah → interleave!).
      expect(
        seenSections.includes(sid!),
        `Section ${sid} muncul sebagai blok kontigu (tak interleave); urutan section terlihat=[${seenSections.join(',')}], qid=${qid}`
      ).toBe(false);
      seenSections.push(sid!);
      prev = sid!;
    }
  }
  // "Lainnya" (0) bila hadir HARUS blok terakhir (D-15).
  if (seenSections.includes(0)) {
    expect(seenSections[seenSections.length - 1], '"Lainnya" (null section) di blok terakhir').toBe(0);
  }
}

test.describe('Phase 419 D-04.4 — Add/Remove peserta live x Section + pagination (multi-context SignalR)', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre419addremove-${ts}.bak`;
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

  test('tambah/hapus peserta live saat ujian ber-Section + pagination tetap konsisten (D-04.4)', async ({ page, browser }) => {
    test.setTimeout(360_000);
    const title = `AddRemove Section 419 ${Date.now()}`;

    // ── 1. Assessment ber-Section + paginated: 16 soal (12 Section A 'Pompa' SNP0 + 4 Section B 'Valve' SNP1). ──
    await createOjtArriveMP(page, title);
    const packageId = await createDefaultPackage(page, 'Paket A');
    for (let i = 0; i < 12; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `Pompa #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    for (let i = 0; i < 4; i++) {
      await addQuestionViaForm(page, packageId, {
        type: 'MultipleChoice', text: `Valve #${i + 1}`,
        options: ['A', 'B', 'C', 'D'], correctIndex: 0, score: 10,
      });
    }
    const qids = await questionIdsOrdered(packageId);
    expect(qids.length, 'paket punya 16 soal').toBe(16);
    const secA = await createSection(packageId, 1, 'Pompa', false);     // Section A, mengalir (SNP0)
    const secB = await createSection(packageId, 2, 'Valve', true);      // Section B, page-break (SNP1)
    await assignToSection(qids, 0, 12, secA);
    await assignToSection(qids, 12, 16, secB);
    // qToSection: qid → SectionId (0 = Lainnya); semua soal ter-assign (tak ada Lainnya di seed ini).
    const qToSection = await questionSectionMap(packageId);

    // ── 2. rino START ujian (peserta resume). Assert pagination/header Section + capture rinoOrder. KEEP open. ──
    const rino = await startExamAsParticipant(browser, title);
    const rinoSessionId = rino.sessionId;
    try {
      const ep = rino.page;
      // Header Section: Pompa (page 0 aktif → visible) + Valve (page-break, hidden → assert kehadiran/count).
      const headers = ep.locator('div.text-primary.fw-semibold');
      await expect(headers.filter({ hasText: 'Pompa' }).first(), 'header "Pompa" (page 0)').toBeVisible();
      expect(await headers.filter({ hasText: 'Valve' }).count(), 'header "Valve" ter-render di DOM').toBeGreaterThanOrEqual(1);
      // Pagination page-break: ShuffleEnabled → urutan DALAM section teracak, jadi JANGAN asumsikan qids[0] pertama.
      // Buktikan blok: SEMUA soal Valve (Section B, StartNewPage) di halaman > SEMUA soal Pompa (Section A).
      const pompaPages = await Promise.all(qids.slice(0, 12).map((q) => pageOfQ(ep, q)));
      const valvePages = await Promise.all(qids.slice(12, 16).map((q) => pageOfQ(ep, q)));
      expect(Math.min(...pompaPages), 'Pompa (Section A) mulai di page 0').toBe(0);
      expect(Math.min(...valvePages), 'Valve (StartNewPage) mulai di halaman > halaman terakhir Pompa')
        .toBeGreaterThan(Math.max(...pompaPages));

      // rinoOrder = ShuffledQuestionIds dari UPA (DB otoritatif). Sanity: blok-per-section juga.
      const rinoJson = await queryStr(
        `SELECT TOP 1 ShuffledQuestionIds FROM UserPackageAssignments WHERE AssessmentSessionId = ${rinoSessionId}`
      );
      const rinoOrder = JSON.parse(rinoJson) as number[];
      expect(rinoOrder.length, 'rino ShuffledQuestionIds == 16').toBe(16);
      assertContiguousSectionBlocks(rinoOrder, qToSection);

      // ── 3. Admin: monitoring + hub Connected + tambah iwan3 LIVE → assert live row + capture addedSessionId. ──
      // Resolve iwan3 User.Id (tabel Users — GetEligibleParticipantsToAdd query _context.Users, value checkbox=u.Id).
      const iwan3UserId = await queryStr(
        `SELECT TOP 1 Id FROM Users WHERE Email = '${accounts.coachee2.email}'`
      );
      expect(iwan3UserId.length, 'iwan3 User.Id ter-resolve dari tabel Users').toBeGreaterThan(0);

      const monitoringUrl = buildMonitoringUrl(title, 'OJT', today());
      // `page` sudah admin (createOjtArriveMP). JANGAN login ulang — re-visit /Account/Login saat sudah auth
      // → redirect Home → input[name=email] tak muncul → timeout. Cukup navigasi ke monitoring.
      await page.goto(monitoringUrl);
      await waitHubConnected(page);

      // Buka picker (data-bs-toggle → modal; show.bs.modal AJAX GetEligibleParticipantsToAdd) → centang iwan3.
      await page.locator('#btnTambahPeserta').click();
      const iwan3Check = page.locator(`.tambah-peserta-check[value="${iwan3UserId}"]`);
      await iwan3Check.waitFor({ state: 'visible', timeout: 10_000 });
      await iwan3Check.check();

      // Intercept AddParticipantsLive → added[0].id = AssessmentSession.Id (deterministik, bukan tebak DOM).
      const respPromise = page.waitForResponse(
        (r) => r.url().includes('/AddParticipantsLive') && r.request().method() === 'POST',
        { timeout: 12_000 }
      );
      await page.locator('#btnKonfirmasiTambah').click();
      const resp = await respPromise;
      expect(resp.ok(), 'AddParticipantsLive 2xx').toBeTruthy();
      const body = await resp.json();
      const added = (body && body.added) || [];
      expect(added.length, 'minimal 1 peserta ditambahkan (D-01: iwan3 belum di batch)').toBeGreaterThan(0);
      const addedSessionId: number = added[0].id;
      expect(addedSessionId, 'addedSessionId ter-capture').toBeGreaterThan(0);

      // Baris baru MUNCUL LIVE di tabel aktif TANPA reload (participantAdded handler / fallback inject).
      // Lesson 412/413 (monFlashRow): assert baris DOM visible + DB backstop.
      await page.waitForSelector(
        `tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`,
        { timeout: 10_000 }
      );

      // ── 4. ASSERT eager per-section assignment untuk peserta baru (D-04.4 inti). ──
      // UPA iwan3 keyed by addedSessionId (CreateEagerAssignmentsAsync). ShuffledQuestionIds blok-per-section.
      const iwanJson = await queryStr(
        `SELECT TOP 1 ShuffledQuestionIds FROM UserPackageAssignments WHERE AssessmentSessionId = ${addedSessionId}`
      );
      expect(iwanJson.trim().startsWith('['), 'UPA eager untuk peserta baru ada (JSON array)').toBe(true);
      const iwanOrder = JSON.parse(iwanJson) as number[];
      expect(iwanOrder.length, 'peserta baru ShuffledQuestionIds == 16 (eager full pool)').toBe(16);
      // Inti drift-free: semua Section-A qids mendahului semua Section-B qids (blok kontigu, "Lainnya" terakhir).
      assertContiguousSectionBlocks(iwanOrder, qToSection);
      // Eksplisit: prefix == seluruh Section A (12), suffix == seluruh Section B (4) — tak ada interleave.
      const headBlock = iwanOrder.slice(0, 12).map((q) => qToSection.get(q));
      const tailBlock = iwanOrder.slice(12, 16).map((q) => qToSection.get(q));
      expect(new Set(headBlock).size, '12 soal pertama peserta baru semua dari SATU section (A)').toBe(1);
      expect(new Set(tailBlock).size, '4 soal terakhir peserta baru semua dari SATU section (B)').toBe(1);
      expect(headBlock[0], 'blok pertama == Section A (Pompa)').toBe(secA);
      expect(tailBlock[0], 'blok kedua == Section B (Valve)').toBe(secB);

      // ── 5. ASSERT peserta resume (rino) TAK terganggu: re-read UPA == rinoOrder (assignment tak digeser). ──
      const rinoJson2 = await queryStr(
        `SELECT TOP 1 ShuffledQuestionIds FROM UserPackageAssignments WHERE AssessmentSessionId = ${rinoSessionId}`
      );
      const rinoOrder2 = JSON.parse(rinoJson2) as number[];
      expect(rinoOrder2, 'rino (resume) ShuffledQuestionIds tak berubah oleh add-live').toEqual(rinoOrder);

      // ── 6. Hapus iwan3 (Not-started + no-data → HARD delete: baris hilang, sesi terhapus, TANPA panel/Restore). ──
      // Reload dulu: baris peserta-baru tadi di-INJECT via JS (participantAdded / fallback monInjectParticipantRow);
      // render ulang server-side supaya dropdown/aksi hapus andal (live-add TANPA reload sudah dibuktikan di step 3).
      // RemoveParticipantCoreAsync: peserta TANPA StartedAt & TANPA response → Mode="hard" (clean delete). Jalur
      // soft-remove + Restore khusus peserta BERDATA (sudah ter-UAT penuh di Phase 413), bukan fokus D-04.4.
      await page.reload({ waitUntil: 'networkidle' });
      await waitHubConnected(page);
      await page.waitForSelector(`tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`, { timeout: 10_000 });
      const activeBefore = await page.locator('tbody:not(#tbodyRemoved) tr[data-session-id]').count();

      await openRowHapusModal(page, addedSessionId);
      const lightModal = page.locator('#hapusPesertaLightModal');
      await expect(lightModal, 'peserta baru Not-started → modal RINGAN').toBeVisible({ timeout: 8_000 });
      const removeRespP = page.waitForResponse(
        (r) => r.url().includes('/RemoveParticipantLive') && r.request().method() === 'POST', { timeout: 12_000 });
      await page.locator('#btnHapusLightKonfirmasi').click();
      const removeResp = await removeRespP;
      expect(removeResp.ok(), 'RemoveParticipantLive 2xx').toBeTruthy();
      const removeBody = await removeResp.json();
      expect(removeBody.mode, 'peserta Not-started/no-data → HARD delete').toBe('hard');

      // Baris HILANG dari tabel aktif live (hard → tak ke #tbodyRemoved).
      await expect(
        page.locator(`tbody:not(#tbodyRemoved) tr[data-session-id="${addedSessionId}"]`)
      ).toHaveCount(0, { timeout: 10_000 });
      // Count aktif TURUN 1 (Pitfall 2 — exclude removed).
      await expect
        .poll(async () => page.locator('tbody:not(#tbodyRemoved) tr[data-session-id]').count(), { timeout: 8_000 })
        .toBe(activeBefore - 1);
      // DB backstop: hard delete → sesi peserta-baru TERHAPUS (bukan soft RemovedAt).
      const stillExists = await db.queryScalar(
        `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${addedSessionId}`);
      expect(stillExists, 'hard delete → sesi peserta-baru terhapus dari DB').toBe(0);
      // KRITIS: peserta resume (rino) TIDAK ikut terhapus oleh remove peserta-baru (tak ganggu peserta lain).
      const rinoStillActive = await db.queryScalar(
        `SELECT COUNT(*) FROM AssessmentSessions WHERE Id = ${rinoSessionId} AND RemovedAt IS NULL`);
      expect(rinoStillActive, 'rino (resume) tetap aktif setelah remove peserta-baru').toBe(1);
    } finally {
      // 7. Tutup peserta resume (rino).
      await rino.close();
    }
  });
});
