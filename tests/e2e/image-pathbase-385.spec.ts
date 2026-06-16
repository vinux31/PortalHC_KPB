// Phase 385 Plan 01 (PXF-01 / F-09) — e2e gambar soal PathBase-aware di sub-path /KPB-PortalHC.
//
// BUG F-09 (CONFIRMED HARD di Dev): <img src="@imagePath"> emit path leading-slash mentah
//   "/uploads/.." yang BYPASS PathBase → 404 saat app jalan di sub-path "/KPB-PortalHC" (Dev/Prod).
//   Fix render-time di Views/Shared/_QuestionImage.cshtml: resolusi via Url.Content("~"+path) →
//   "/KPB-PortalHC/uploads/.." di sub-path, "/uploads/.." di lokal bare.
//
// KUNCI spec ini (beda dari image-in-assessment.spec.ts):
//   1. SENGAJA akses app via URL BER-PREFIX "/KPB-PortalHC" (override baseURL describe-scope)
//      untuk MEREPRODUKSI PathBase — spec lama pakai bare localhost → TIDAK reproduce F-09 (D-05).
//   2. Assert gambar BENAR-BENAR TER-LOAD (network 200 + naturalWidth>0), BUKAN sekadar regex src.
//      (Note F-09: e2e lama lolos karena cuma cek regex atribut src, bukan load nyata.)
//   App lokal MELAYANI sub-path karena UsePathBase aktif (appsettings.json:9 tidak di-override Development).
//
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev
//   (resolve InstanceDefaultBackupPath) → test → afterAll RESTORE (sukses ATAU gagal) +
//   fs.rmSync folder upload (file fisik TIDAK ter-cover DB RESTORE).
//
// PRECONDITION: app running di http://localhost:5277 (dotnet run, AD off) + DB lokal HcPortalDB_Dev.
//   Jalankan: cd tests; npx playwright test image-pathbase-385 --workers=1
//   Bila browser belum ada: npx playwright install chromium.
//
// Auth: admin (admin@pertamina.com) buat assessment; coachee (rino.prasetyo@pertamina.com) kerjakan.
//   pwd dev lokal 123456 — JANGAN staging/prod.

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
import * as path from 'node:path';

const Q_IMG = path.resolve(__dirname, '../fixtures/q-image.jpg');
const OPT_IMG = path.resolve(__dirname, '../fixtures/opt-image.png');
const Q_IMG_TEXT = 'Soal bergambar 385 PathBase diagram pompa';
const Q_IMG_ALT = 'diagram pompa 385';
const OPT_IMG_ALT = 'opsi impeller 385';

const today = () => new Date().toISOString().slice(0, 10);

// Base URL absolut BER-PREFIX. CATATAN: page.goto('/x') leading-slash MEMBUANG path baseURL
// (resolusi URL standar) → nav jatuh ke origin bare. Untuk benar-benar mengakses app via PathBase,
// WAJIB pakai URL absolut penuh (bukan relative leading-slash + test.use baseURL).
const PREFIX_BASE = 'http://localhost:5277/KPB-PortalHC';

let snapshotPath: string;
let createdPackageId: number | null = null;
let assessmentTitle: string;

test.describe.configure({ mode: 'serial' });

test.describe('Phase 385 — gambar soal PathBase-aware di sub-path /KPB-PortalHC (F-09 / PXF-01)', () => {

  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre385img-${ts}.bak`;
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
    // Cleanup file fisik upload — TIDAK ter-cover DB RESTORE (Seed Workflow D-05).
    if (createdPackageId != null) {
      const fs = await import('node:fs');
      const p = await import('node:path');
      const dir = p.resolve(__dirname, '../../wwwroot/uploads/questions', String(createdPackageId));
      try { fs.rmSync(dir, { recursive: true, force: true }); } catch { /* best-effort */ }
    }
    if (restoreError) throw restoreError;
  });

  // ── TEST 1: admin upload gambar soal + opsi via FORM NYATA (lewat URL ber-prefix) ──────────
  test('admin upload gambar soal + opsi via form (setInputFiles)', async ({ page }) => {
    test.setTimeout(120_000);
    // Title WAJIB pola naming-convention: standard non-PrePostTest match ^(Pre|Post)\s*Test\s+.+$
    assessmentTitle = `Pre Test OJT IMG385 ${Date.now()}`;
    await login(page, 'admin');

    await createAssessmentViaWizard(page, {
      title: assessmentTitle,
      category: 'OJT',
      scheduleDate: today(),
      scheduleTime: '00:01',
      durationMinutes: 60,
      passPercentage: 50,
      allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      ewcdDate: today(),
      ewcdTime: '23:59',
    });

    // Dismiss static success modal via manage-btn → /Admin/ManagePackages
    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');

    createdPackageId = await createDefaultPackage(page);

    // Soal #1 — DENGAN gambar soal + gambar opsi A (upload via form nyata).
    await addQuestionViaForm(
      page,
      createdPackageId,
      { type: 'MultipleChoice', text: Q_IMG_TEXT, options: ['Impeller', 'Casing', 'Shaft', 'Bearing'], correctIndex: 0, score: 100 },
      { question: Q_IMG, questionAlt: Q_IMG_ALT, optionA: OPT_IMG, optionAAlt: OPT_IMG_ALT }
    );

    expect(createdPackageId).toBeGreaterThan(0);
  });

  // ── TEST 2: peserta StartExam — gambar LOAD via URL ber-prefix (src prefix + naturalWidth>0) ──
  test('peserta StartExam: src ber-prefix /KPB-PortalHC + gambar ter-load (naturalWidth>0, no 404)', async ({ page }) => {
    test.setTimeout(120_000);

    // (Opsional kuat D-05) intercept response: pastikan TIDAK ada 404 ke /uploads/questions/.
    const badImageResponses: string[] = [];
    page.on('response', (r) => {
      if (r.url().includes('/uploads/questions/') && r.status() >= 400) {
        badImageResponses.push(`${r.status()} ${r.url()}`);
      }
    });

    await login(page, 'coachee');
    // Load Assessment via URL absolut BER-PREFIX → AJAX VerifyToken (startStandardAssessment) juga
    // ber-prefix → redirectUrl server-gen (Url.Action + PathBase) ber-prefix → StartExam dilayani via PathBase.
    await page.goto(`${PREFIX_BASE}/CMP/Assessment`);

    const card = page.locator('.assessment-card', { hasText: assessmentTitle });
    await expect(card).toBeVisible({ timeout: 10_000 });
    page.once('dialog', (d) => d.accept());
    await card.locator('.btn-start-standard').click();
    await page.waitForURL(/\/KPB-PortalHC\/CMP\/StartExam\//, { timeout: 15_000 });
    await expect(page.locator('#examHeader')).toBeVisible();

    // URL halaman HARUS ber-prefix (bukti app dilayani via sub-path).
    expect(page.url()).toContain('/KPB-PortalHC/CMP/StartExam/');

    // --- Gambar SOAL: src ber-prefix /KPB-PortalHC/uploads/ + benar-benar TER-LOAD ---
    const qImg = page.locator(`img.question-image-zoom[data-img-alt="${Q_IMG_ALT}"]`).first();
    await expect(qImg).toBeVisible();
    // src render ber-prefix sub-path (bukan /uploads/ mentah yang akan 404).
    await expect(qImg).toHaveAttribute('src', /\/KPB-PortalHC\/uploads\//);
    // lightbox data-img-src juga PathBase-aware (D-01a).
    await expect(qImg).toHaveAttribute('data-img-src', /\/KPB-PortalHC\/uploads\//);
    // LOAD nyata (bukan ikon rusak / 404): naturalWidth>0.
    const qNat = await qImg.evaluate((el: HTMLImageElement) => el.naturalWidth);
    expect(qNat, 'gambar soal naturalWidth (load 200, bukan 404)').toBeGreaterThan(0);

    // --- Gambar OPSI: idem (src prefix + naturalWidth>0) ---
    const optImg = page.locator(`img.question-image-zoom[data-img-alt="${OPT_IMG_ALT}"]`).first();
    await expect(optImg).toBeVisible();
    await expect(optImg).toHaveAttribute('src', /\/KPB-PortalHC\/uploads\//);
    const optNat = await optImg.evaluate((el: HTMLImageElement) => el.naturalWidth);
    expect(optNat, 'gambar opsi naturalWidth (load 200, bukan 404)').toBeGreaterThan(0);

    // --- Tak ada response 404/5xx ke gambar soal selama load halaman ---
    expect(badImageResponses, `response gagal ke /uploads/questions/: ${badImageResponses.join(', ')}`).toHaveLength(0);
  });
});
