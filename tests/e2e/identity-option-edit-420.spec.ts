// Phase 420 (OPTEDIT-01..05 + VRF-01) — UAT e2e Identity-Based Option Editing real-browser.
// Membuktikan RUNTIME (lesson 354: Razor/JS WAJIB UAT browser — integration controller test tak menangkap
// reindex client-side hidden Id carrier / clone-reset) bahwa upsert opsi EditQuestion kini IDENTITY-based:
//   S1 (delete-middle ANSWERED → BLOCKED, OPTEDIT-02/VRF-01): soal MC 4-opsi + PackageUserResponse ke opsi B
//       (tengah) → buka edit (hidden option_B_id terisi dari GET JSON) → hapus baris B → Simpan →
//       .alert-danger memuat "sudah dijawab" + "B" (BUKAN 500 / relabel senyap). DB: opsi B utuh, response utuh.
//   S2 (delete-middle UNANSWERED → no-relabel, OPTEDIT-01): soal MC 4-opsi belum-dijawab → hapus baris B →
//       Simpan → sukses; DB opsi tersisa Id+teks UTUH (C tetap "C", BUKAN ter-relabel jadi "B").
//   S3 (add-option clone-gotcha §2c, OPTEDIT-05): soal MC 4-opsi → "Tambah Opsi" → baris E hidden Id KOSONG
//       (clone-reset clear type=hidden) → set A benar + isi "E" → Simpan → DB 5 opsi; opsi A Id stabil teks "A"
//       (BUKAN ter-overwrite oleh opsi baru); opsi E ter-ADD.
//
// Catatan: UAT ini juga dijalankan via Playwright MCP driver (real browser, DB-verified) saat eksekusi fase —
//   lihat .planning/phases/420-.../420-UAT.md. Spec ini = artefak regresi (jalankan ulang kapan saja).
//
// Template/analog: tests/e2e/option-dynamic-418.spec.ts (S7 edit-shrink answered) + helpers/dbSnapshot + helpers/auth.
// SEED_WORKFLOW (CLAUDE.md): temporary + local-only. beforeAll BACKUP HcPortalDB_Dev → afterAll RESTORE.
// PRECONDITION: app @ http://localhost:5277 (Authentication__UseActiveDirectory=false dotnet run) + DB lokal.
//   WAJIB --workers=1. Auth: admin@pertamina.com / 123456 (dev lokal).

import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';

test.describe.configure({ mode: 'serial' });

let snapshotPath: string;
let pkg = 0, q1 = 0, q1b = 0, q2 = 0, q3 = 0;

// Seed 1 sesi + 1 paket + 3 soal MC 4-opsi; response ke opsi B soal Q1. Return id pipe-joined.
const SEED_SQL = `
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users ORDER BY Id);
DECLARE @sid INT, @pid INT, @qa INT, @qb INT, @qc INT;
INSERT INTO AssessmentSessions (UserId, Title, Category, Status, AccessToken, Schedule, DurationMinutes, PassPercentage, Progress, AssessmentType, BannerColor, IsTokenRequired)
 VALUES (@uid, 'ZZ-UAT420 Identity Option Edit', 'OJT', 'Open', '', CAST(GETDATE() AS DATE), 60, 70, 0, 'Standard', '#0d6efd', 0);
SET @sid = SCOPE_IDENTITY();
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt) VALUES (@sid, 'ZZ-UAT420 Paket', 1, GETDATE());
SET @pid = SCOPE_IDENTITY();
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters) VALUES (@pid, 'UAT420 Q1 (B sudah dijawab)', 1, 10, 'MultipleChoice', 2000);
SET @qa = SCOPE_IDENTITY();
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qa,'A',1),(@qa,'B',0),(@qa,'C',0),(@qa,'D',0);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters) VALUES (@pid, 'UAT420 Q2 (belum dijawab)', 2, 10, 'MultipleChoice', 2000);
SET @qb = SCOPE_IDENTITY();
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qb,'A',1),(@qb,'B',0),(@qb,'C',0),(@qb,'D',0);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters) VALUES (@pid, 'UAT420 Q3 (tambah opsi)', 3, 10, 'MultipleChoice', 2000);
SET @qc = SCOPE_IDENTITY();
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qc,'A',1),(@qc,'B',0),(@qc,'C',0),(@qc,'D',0);
DECLARE @qab INT = (SELECT Id FROM PackageOptions WHERE PackageQuestionId=@qa AND OptionText='B');
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt) VALUES (@sid, @qa, @qab, GETDATE());
SELECT CAST(@pid AS VARCHAR)+'|'+CAST(@qa AS VARCHAR)+'|'+CAST(@qab AS VARCHAR)+'|'+CAST(@qb AS VARCHAR)+'|'+CAST(@qc AS VARCHAR);
`;

test.beforeAll(async () => {
  const backupDir = await db.queryString(`SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000))`);
  snapshotPath = `${backupDir.replace(/[\\/]+$/, '')}\\HcPortalDB_Dev-pre420-uat.bak`;
  await db.backup(snapshotPath);
  const ids = await db.queryString(SEED_SQL);
  [pkg, q1, q1b, q2, q3] = ids.split('|').map((s) => parseInt(s.trim(), 10));
  expect(pkg, 'packageId ter-seed').toBeGreaterThan(0);
});

test.afterAll(async () => {
  if (snapshotPath) await db.restore(snapshotPath);
});

async function openEdit(page: any, questionId: number) {
  await page.goto(`/Admin/ManagePackageQuestions?packageId=${pkg}`);
  await page.locator(`button[onclick="loadEditForm(${questionId})"]`).click();
  await expect(page.locator('#option_A')).toHaveValue('A');
}

test('S1: hapus opsi tengah SUDAH-dijawab → diblokir pesan ramah (OPTEDIT-02/VRF-01)', async ({ page }) => {
  await login(page, 'admin');
  await openEdit(page, q1);
  // GET JSON populate hidden Id carrier (PATCH C)
  await expect(page.locator('#option_B_id')).toHaveValue(String(q1b));
  await page.getByRole('button', { name: 'Hapus opsi B' }).click();
  await page.locator('#submitBtn').click();
  // Diblokir — pesan D-04 (huruf + cuplik teks). BUKAN 500.
  // .first() — strict-mode: halaman bisa punya >1 .alert (toast global + inline). Pesan blokir di alert pertama.
  await expect(page.locator('.alert-danger').first()).toContainText('sudah dijawab');
  await expect(page.locator('.alert-danger').first()).toContainText('"B"');
  // DB: opsi B utuh + response utuh.
  expect(await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${q1}`)).toBe(4);
  expect(await db.queryScalar(`SELECT COUNT(*) FROM PackageUserResponses WHERE PackageOptionId=${q1b}`)).toBe(1);
});

test('S2: hapus opsi tengah BELUM-dijawab → sukses tanpa relabel (OPTEDIT-01)', async ({ page }) => {
  await login(page, 'admin');
  await openEdit(page, q2);
  await page.getByRole('button', { name: 'Hapus opsi B' }).click();
  await page.locator('#submitBtn').click();
  await expect(page.locator('.alert-success').first()).toContainText('berhasil diperbarui');
  // DB: 3 opsi tersisa teks A,C,D — C TIDAK ter-relabel jadi "B".
  expect(await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${q2}`)).toBe(3);
  const texts = await db.queryString(`SELECT STRING_AGG(OptionText, ',') WITHIN GROUP (ORDER BY Id) FROM PackageOptions WHERE PackageQuestionId=${q2}`);
  expect(texts).toBe('A,C,D');
});

test('S3: tambah opsi (clone gotcha §2c) → opsi baru ADD, A tak ter-overwrite (OPTEDIT-05)', async ({ page }) => {
  await login(page, 'admin');
  await openEdit(page, q3);
  await page.locator('#addOptionBtn').click();
  // Clone-reset clear hidden Id (PATCH D) — baris baru E hidden Id KOSONG (bukan warisi A).
  await expect(page.locator('#option_E_id')).toHaveValue('');
  await page.locator('#correct_A').check();          // MC: re-pilih A benar (reletter drop seleksi)
  await page.locator('#option_E').fill('E');
  await page.locator('#submitBtn').click();
  await expect(page.locator('.alert-success').first()).toContainText('berhasil diperbarui');
  // DB: 5 opsi; A teks tetap "A" (Id stabil, tak ter-overwrite); E ter-ADD.
  expect(await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${q3}`)).toBe(5);
  const aText = await db.queryString(`SELECT OptionText FROM PackageOptions WHERE PackageQuestionId=${q3} ORDER BY Id`);
  expect(aText).toBe('A');
  expect(await db.queryScalar(`SELECT COUNT(*) FROM PackageOptions WHERE PackageQuestionId=${q3} AND OptionText='E'`)).toBe(1);
});
