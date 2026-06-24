// Phase 419 D-04.1 — Lifecycle Section inti UAT real-browser @5277.
// STATUS: test.fixme (draft siap-jalan). Live-UAT executor: un-fixme + verifikasi selektor panel Section
//   (Phase 415 ManagePackageSections) + tombol export terhadap app live, lalu jalankan.
// Analog: tests/e2e/scoped-shuffle.spec.ts (snapshot+wizard+section) + export-per-peserta (download-assert).
// Prasyarat live: app @5277 (dotnet run), SQLBrowser/lpc, npx playwright test --workers=1.
import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

test.describe.configure({ mode: 'serial' });

let snapshotPath: string;
const fmtLocal = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const today = () => fmtLocal(new Date());
const tomorrow = () => { const d = new Date(); d.setDate(d.getDate() + 1); return fmtLocal(d); };

test.beforeAll(async () => { snapshotPath = `bak_419_lifecycle_${Date.now()}`; await db.backup(snapshotPath); });
test.afterAll(async () => { if (snapshotPath) await db.restore(snapshotPath); });

test.describe('Phase 419 D-04.1 — Lifecycle Section inti (Section + shuffle + pagination + opsi 2-6)', () => {
  test.fixme('create -> assign Section -> ujian render A-F -> resume -> export label Section', async ({ page, browser }) => {
    const title = `Lifecycle Section 419 ${Date.now()}`;

    // 1. Admin login + wizard create OJT startable (analog scoped-shuffle createOjtArriveMP).
    await login(page, 'admin');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT', scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 60, passPercentage: 50, allowAnswerReview: true, generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'], ewcdDate: tomorrow(), ewcdTime: '23:59',
    });
    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');
    const assessmentId = parseInt(new URL(page.url()).searchParams.get('assessmentId') ?? '0', 10);
    expect(assessmentId).toBeGreaterThan(0);

    // 2. Buat paket + soal (>=1 dengan 5-6 opsi → uji huruf A-F dinamis Phase 418).
    const packageId = await createDefaultPackage(page, 'Paket A');
    // TODO live: addQuestionViaForm dengan 5-6 opsi (extend opts examTypes bila perlu) + ElemenTeknis.
    await addQuestionViaForm(page, packageId, { /* TODO 5-6 opsi MC + ElemenTeknis */ } as any);

    // 3. Buat Section (panel kelola Phase 415) + assign soal. TODO live: selektor #addSectionBtn / dropdown assign.
    //    Section 1 "Proses", Section 2 "Keselamatan"; set toggle Mulai Halaman Baru per spec.
    //    await page.goto(`/Admin/ManagePackageSections?packageId=${packageId}`) ATAU panel inline di ManagePackageQuestions.

    // 4. Peserta ambil ujian (context baru) → ASSERT render huruf A-F dinamis + header Section + pagination (417).
    //    Reuse pola startExamAsParticipant (scoped-shuffle.spec.ts:115). ASSERT label opsi 'A'..'F' sesuai jumlah.

    // 5. Resume: reload mid-exam → LastActivePage kembali ke halaman benar (417 PAG-03).

    // 6. Admin export per-soal → ASSERT band-header Excel "Section 1: Proses" + heading PDF (PAG-04).
    //    Reuse verifyExcelDownload (examTypes.ts:749) — assert sheet "Detail Per Soal" memuat sel "Section 1:".
    expect(true).toBe(true); // placeholder sampai langkah live diisi
  });
});
