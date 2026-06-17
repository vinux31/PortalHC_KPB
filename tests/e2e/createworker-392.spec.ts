// Phase 392 (Perbaikan CreateWorker + Audit Field) — WRKR-01/02/03 runtime verification.
// Pre-req: server localhost:5277 jalan dengan Authentication__UseActiveDirectory=false (mode login lokal),
//   SQLBrowser + shared-memory conn (lihat reference_local_e2e_sql_env_fix). Login admin@pertamina.com / 123456.
// Run: cd tests && npx playwright test e2e/createworker-392.spec.ts --workers=1
//
// TEST A (static guard, no app): buktikan CreateWorker.cshtml hasil-fix TAK PUNYA readonly=/bg-light pada
//   FullName/Email → editable di AD mode BY CONSTRUCTION (run AD-off tak bisa exercise bug readonly-AD; Pitfall F-NEW-04).
// TEST B (runtime): Nama/Email bisa diketik, Email type=email, validasi live surface, cascade Bagian→Unit,
//   create submission sukses (redirect ManageWorkers + Success flash + baris DB). Teardown self-cleaning via DeleteWorker.
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
import * as fs from 'fs';
import * as path from 'path';

test.describe.configure({ mode: 'serial' });

const TS = Date.now();
const EMAIL = `e2e-cw-${TS}@local.test`;
const FULLNAME = `E2E CreateWorker ${TS}`;
let workerId = '';

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

test.describe('Phase 392 — /Admin/CreateWorker usable + audited', () => {
  // TEST A — static source-grep guard (D-06): readonly/bg-light removed unconditionally → editable di AD mode by construction.
  test('static guard: CreateWorker.cshtml has no readonly/bg-light on FullName/Email (WRKR-01 by construction)', async () => {
    const cshtmlPath = path.resolve(__dirname, '../../Views/Admin/CreateWorker.cshtml');
    const src = fs.readFileSync(cshtmlPath, 'utf8');
    // Penghapusan unconditional (bukan @if(isAdMode)) menjamin field editable walau AD mode aktif.
    expect(src).not.toMatch(/readonly=/);
    expect(src).not.toMatch(/bg-light/);
    // Sanity: Plan 01 landed.
    expect(src).toContain('type="email"');
    expect(src).toContain('_ValidationScriptsPartial');
  });

  // TEST B — runtime e2e (D-06): editable + type=email + validasi live + cascade + create sukses.
  test('field editable + type=email + cascade + live validation + create succeeds (WRKR-01/02/03)', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/CreateWorker');
    await page.locator('#FullName').waitFor({ state: 'visible', timeout: 15_000 });

    // WRKR-02 — Email input bertipe email.
    expect(await page.locator('#Email').getAttribute('type')).toBe('email');

    // D-05 — validasi client-side AKTIF: jquery-validation ter-load via _ValidationScriptsPartial di @section Scripts.
    const hasValidator = await page.evaluate(() => !!(window as any).jQuery && !!(window as any).jQuery.validator);
    expect(hasValidator).toBe(true);

    // WRKR-02 — pesan error per-field surface: Nama kosong + Email valid-format → submit ditolak (client unobtrusive ATAU server),
    // tetap di CreateWorker + .field-validation-error muncul. (Email valid-format menghindari native HTML5 type=email bubble.)
    await page.fill('#FullName', '');
    await page.fill('#Email', 'valid-format@local.test');
    await page.click('#createWorkerForm button[type="submit"]');
    await expect(page).toHaveURL(/\/Admin\/CreateWorker/);
    await expect(page.locator('#createWorkerForm .field-validation-error').filter({ hasText: /.+/ }).first())
      .toBeVisible({ timeout: 10_000 });

    // CATATAN (DEF-392-01): `initFormLoading` (wwwroot/js/shared-loading.js, infra bersama pra-existing) men-disable
    // tombol submit pada event submit MESKI validasi membatalkan submit (preventDefault tak hentikan listener native lain),
    // jadi setelah submit-ditolak di atas tombolnya nyangkut disabled. Reload halaman → form segar/tombol enabled untuk
    // memisahkan assert "validasi surface" dari assert "create sukses". (Bukan masking — dicatat di deferred-items.md;
    // fix infra bersama = phase tersendiri, di luar scope view-only 392.)
    await page.goto('/Admin/CreateWorker');
    await page.locator('#FullName').waitFor({ state: 'visible', timeout: 15_000 });

    // WRKR-01 — field bisa diketik (tidak readonly).
    await page.fill('#FullName', FULLNAME);
    await page.fill('#Email', EMAIL);
    expect(await page.locator('#FullName').inputValue()).toBe(FULLNAME);
    expect(await page.locator('#Email').inputValue()).toBe(EMAIL);

    // WRKR-03 — cascade Bagian→Unit membangun opsi Unit saat runtime.
    const sectionValue = await page.locator('#sectionSelect option:not([value=""])').first().getAttribute('value');
    expect(sectionValue).toBeTruthy();
    await page.selectOption('#sectionSelect', sectionValue!);
    await expect.poll(
      () => page.locator('#unitSelect option:not([value=""])').count(),
      { timeout: 10_000 },
    ).toBeGreaterThan(0);

    // AD-off → blok password tampil (Password BUKAN [Required], tapi divalidasi StringLength min 6 + Compare).
    await page.fill('#passwordField', 'Test123!');
    await page.fill('#confirmPasswordField', 'Test123!');

    // WRKR-03 — submit sukses: redirect ManageWorkers + Success flash.
    await Promise.all([
      page.waitForURL('**/ManageWorkers', { timeout: 15_000 }),
      page.click('#createWorkerForm button[type="submit"]'),
    ]);
    await expect(page.locator('.alert').filter({ hasText: /berhasil/i }).first()).toBeVisible({ timeout: 10_000 });

    // DB assert + resolve workerId untuk teardown.
    workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${EMAIL}'`);
    expect(workerId).toBeTruthy();
  });
});

// D-07 — teardown self-cleaning (jalan walau test gagal): hapus worker test via DeleteWorker POST (Identity cascade roles).
test.afterAll(async ({ browser }) => {
  try {
    if (!workerId) {
      workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${EMAIL}'`).catch(() => '');
    }
    if (!workerId) return;
    const page = await browser.newPage();
    try {
      await loginAdmin(page);
      await page.goto('/Admin/ManageWorkers');
      const token = await page.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value');
      // Worker baru = AKTIF → hanya #deactivate-{id} yang render, BUKAN #delete-{id}. Pakai POST langsung (bypass UI gating).
      // JANGAN raw-SQL DELETE (skip Identity cascade → orphan AspNetUserRoles, F-NEW-07).
      await page.request.post('/Admin/DeleteWorker', {
        form: { id: workerId, __RequestVerificationToken: token! },
      });
      const remaining = await db.queryScalar(`SELECT COUNT(*) FROM Users WHERE Email='${EMAIL}'`).catch(() => 0);
      if (remaining !== 0) {
        console.warn(`[teardown] worker ${EMAIL} masih ada (count=${remaining}) — cek manual.`);
      }
    } finally {
      await page.close();
    }
  } catch (e) {
    console.warn('[teardown] DeleteWorker gagal (warn-only):', e);
  }
});
