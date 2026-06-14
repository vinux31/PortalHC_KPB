import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { tomorrow } from '../helpers/utils';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
import * as db from '../helpers/dbSnapshot';

test.describe.configure({ mode: 'serial' });

// ============================================================
// Impersonation E2E (Phase 283 baseline + Phase 377 identity-across-surfaces)
// IMP-01: View as role (HC/User)        IMP-02: Impersonate specific user
// IMP-03: Banner                        IMP-05: Read-only enforcement
// IMP-06: Audit log                     IMP-07: SearchUsersApi excludes admin
// IMP-08: Stop impersonation
// IMP-377-SC2/SC3/D03: identitas efektif lintas surface (CMP Records / Home / Assessment)
//
// NB (Phase 377): UI impersonation kini di halaman dedikasi /Admin/Impersonate
// (sebelumnya inline navbar dropdown — di-redesign sejak Phase 283; spec di-update di sini).
//   - Role: form[action*=StartImpersonation] dgn input targetRole=HC|User, card div[role=button]
//     onclick confirm()→submit.
//   - User: #imp-search → #imp-results [data-userid]; click → confirm()→POST StartImpersonation.
//   - StartImpersonation → redirect /Home/Index. Banner #impersonation-banner (alert-danger).
// ============================================================

async function gotoImpersonatePage(page: Page) {
  await page.goto('/Admin/Impersonate');
  await page.waitForLoadState('networkidle');
}

async function startRole(page: Page, role: 'HC' | 'User') {
  await gotoImpersonatePage(page);
  page.once('dialog', (d) => d.accept());
  await page
    .locator('form[action*="StartImpersonation"]')
    .filter({ has: page.locator(`input[name="targetRole"][value="${role}"]`) })
    .locator('div[role="button"]')
    .click();
  await page.waitForURL('**/Home/**', { timeout: 15_000 });
}

async function startUser(page: Page, query: string, target: { id: string; fullName: string }) {
  await gotoImpersonatePage(page);
  const searchInput = page.locator('#imp-search');
  await expect(searchInput).toBeVisible();
  await searchInput.fill(query);
  const results = page.locator('#imp-results');
  await expect(results).toContainText(target.fullName, { timeout: 5_000 });
  page.once('dialog', (d) => d.accept());
  await results.locator(`[data-userid="${target.id}"]`).first().click();
  await page.waitForURL('**/Home/**', { timeout: 15_000 });
}

async function stopImpersonation(page: Page) {
  await page.locator('#impersonation-banner button[type="submit"]').click();
  await page.waitForURL('**/Admin**', { timeout: 15_000 });
}

async function resolveUser(page: Page, query: string, fullName: string) {
  const res = await page.request.get(`/Admin/SearchUsersApi?q=${encodeURIComponent(query)}`);
  expect(res.status()).toBe(200);
  const users = await res.json();
  const target =
    users.find((u: { id: string; fullName: string }) => u.fullName === fullName) ?? users[0];
  expect(target?.id).toBeTruthy();
  return target as { id: string; fullName: string };
}

test.describe('Impersonation — Admin Features', () => {
  // ----------------------------------------------------------
  // IMP-01: View As HC → banner + Kelola Data tetap (HC punya akses)
  // ----------------------------------------------------------
  test('IMP-01: view as HC role shows banner and keeps admin menu', async ({ page }) => {
    await login(page, 'admin');
    await gotoImpersonatePage(page);
    await expect(page.locator('body')).toContainText('View As HC');

    await startRole(page, 'HC');

    await expect(page.locator('#impersonation-banner')).toBeVisible();
    await expect(page.locator('#impersonation-banner')).toContainText('HC');
    await expect(page.locator('#impersonation-banner')).toContainText('Kembali ke Admin');
    await expect(page.locator('nav')).toContainText('Kelola Data');

    await stopImpersonation(page);
  });

  // ----------------------------------------------------------
  // IMP-08: Stop impersonation restores admin session
  // ----------------------------------------------------------
  test('IMP-08: stop impersonation restores admin session', async ({ page }) => {
    await login(page, 'admin');
    await startRole(page, 'HC');
    await expect(page.locator('#impersonation-banner')).toBeVisible();

    await stopImpersonation(page);

    await expect(page.locator('#impersonation-banner')).not.toBeVisible();
    await expect(page.locator('nav')).toContainText('Kelola Data');
  });

  // ----------------------------------------------------------
  // IMP-01 (User role): banner + Kelola Data hilang
  // ----------------------------------------------------------
  test('IMP-01: view as User role shows banner without admin menu', async ({ page }) => {
    await login(page, 'admin');
    await startRole(page, 'User');

    await expect(page.locator('#impersonation-banner')).toBeVisible();
    await expect(page.locator('#impersonation-banner')).toContainText('User');
    await expect(page.locator('nav')).not.toContainText('Kelola Data');

    await stopImpersonation(page);
  });

  // ----------------------------------------------------------
  // IMP-03: Banner tampilan benar (alert-danger, target name, stop button)
  // ----------------------------------------------------------
  test('IMP-03: banner displays correctly with target name and stop button', async ({ page }) => {
    await login(page, 'admin');
    await startRole(page, 'HC');

    const banner = page.locator('#impersonation-banner');
    await expect(banner).toBeVisible();
    await expect(banner).toContainText('Anda melihat sebagai');
    await expect(banner).toHaveClass(/alert-danger/);

    const stopBtn = banner.locator('button[type="submit"]');
    await expect(stopBtn).toBeVisible();
    await expect(stopBtn).toContainText('Kembali ke Admin');

    await stopImpersonation(page);
  });

  // ----------------------------------------------------------
  // IMP-05: Read-only enforcement — submit buttons disabled
  // ----------------------------------------------------------
  test('IMP-05: read-only enforcement disables submit buttons during impersonation', async ({ page }) => {
    await login(page, 'admin');
    await startRole(page, 'HC');

    await page.goto('/Account/Settings');
    await page.waitForLoadState('networkidle');

    const bodyAttr = await page.locator('body').getAttribute('data-impersonating');
    expect(bodyAttr).toBe('true');

    const mainSubmitButtons = page.locator(
      'main button[type="submit"], .container button[type="submit"], form:not([action*="StopImpersonation"]) button[type="submit"]'
    );
    const count = await mainSubmitButtons.count();
    if (count > 0) {
      await expect(mainSubmitButtons.first()).toBeDisabled();
    } else {
      const anyCount = await page.locator('button[type="submit"]').count();
      if (anyCount > 0) await expect(page.locator('button[type="submit"]').first()).toBeDisabled();
    }

    const readOnlyBadgeCount = await page
      .locator('.badge.bg-danger', { hasText: 'Mode Read-Only' })
      .count();
    expect(readOnlyBadgeCount).toBeGreaterThan(0);

    await stopImpersonation(page);
  });

  // ----------------------------------------------------------
  // IMP-07: SearchUsersApi excludes admin users
  // ----------------------------------------------------------
  test('IMP-07: SearchUsersApi does not return admin users', async ({ page }) => {
    await login(page, 'admin');
    const response = await page.request.get('/Admin/SearchUsersApi?q=admin');
    expect(response.status()).toBe(200);
    const users = await response.json();
    const adminEntry = users.find(
      (u: { id: string; fullName: string; nip: string; selectedView: string }) =>
        u.selectedView?.toLowerCase() === 'admin' ||
        u.fullName?.toLowerCase().includes('administrator')
    );
    expect(adminEntry).toBeUndefined();
  });

  // ----------------------------------------------------------
  // IMP-07 (additional): short query → empty
  // ----------------------------------------------------------
  test('IMP-07: SearchUsersApi returns empty for query shorter than 2 chars', async ({ page }) => {
    await login(page, 'admin');
    const response = await page.request.get('/Admin/SearchUsersApi?q=a');
    expect(response.status()).toBe(200);
    const users = await response.json();
    expect(Array.isArray(users)).toBe(true);
    expect(users.length).toBe(0);
  });

  // ----------------------------------------------------------
  // IMP-02: Impersonate specific user via autocomplete search
  // ----------------------------------------------------------
  test('IMP-02: impersonate specific user via autocomplete search shows user name in banner', async ({ page }) => {
    await login(page, 'admin');
    const target = await resolveUser(page, 'rino', 'Rino');
    await startUser(page, 'rino', target);

    const banner = page.locator('#impersonation-banner');
    await expect(banner).toBeVisible();
    await expect(banner).toContainText(target.fullName);

    await stopImpersonation(page);
  });

  // ----------------------------------------------------------
  // IMP-06: Audit log — start/stop events logged
  // ----------------------------------------------------------
  test('IMP-06: audit trail records impersonation start and stop events', async ({ page }) => {
    await login(page, 'admin');
    await startRole(page, 'HC');
    await stopImpersonation(page);

    await page.goto('/Admin/AuditLog');
    await page.waitForLoadState('networkidle');

    const pageBody = page.locator('body');
    const hasImpersonateStart = await pageBody.locator('*', { hasText: 'ImpersonateStart' }).count();
    const hasImpersonateEnd = await pageBody.locator('*', { hasText: 'ImpersonateEnd' }).count();
    const hasImpersonateText = await pageBody.locator('*', { hasText: 'impersonation' }).count();
    const hasMulaiImpersonation = await pageBody.locator('*', { hasText: 'Mulai impersonation' }).count();
    expect(
      hasImpersonateStart + hasImpersonateEnd + hasImpersonateText + hasMulaiImpersonation
    ).toBeGreaterThan(0);
  });

  // ==========================================================
  // Phase 377 — Identitas efektif lintas worker-data surface.
  // Target X = Iwan (iwan3@pertamina.com): 2 TrainingRecords ('Pelatihan K3 Dasar'),
  // admin = 0 trainings. Akar bug LIVE 999.6 (impersonate Iwan → /CMP/Records tampil data admin).
  // ==========================================================

  // SC2 (akar bug 999.6): impersonate user X → /CMP/Records tampil data X (training Iwan), bukan admin.
  test('IMP-377-SC2: impersonate user shows X data in /CMP/Records (not admin)', async ({ page }) => {
    await login(page, 'admin');
    const target = await resolveUser(page, 'iwan', 'Iwan');
    await startUser(page, 'iwan', target);
    await expect(page.locator('#impersonation-banner')).toContainText(target.fullName);

    await page.goto('/CMP/Records');
    await page.waitForLoadState('networkidle');

    // Bukti data X: training Iwan 'Pelatihan K3 Dasar' tampil (admin 0 training → tak mungkin admin).
    await expect(page.locator('body')).toContainText('Pelatihan K3 Dasar', { timeout: 10_000 });

    await stopImpersonation(page);
  });

  // SC3: impersonate user X → /Home greeting = X (split-brain folded) + /CMP/Assessment resolve X.
  test('IMP-377-SC3: impersonate user resolves X in /Home and /CMP/Assessment', async ({ page }) => {
    await login(page, 'admin');
    const target = await resolveUser(page, 'iwan', 'Iwan');
    await startUser(page, 'iwan', target);

    await page.goto('/Home/Index');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('.hero-greeting')).toContainText('Iwan');

    await page.goto('/CMP/Assessment');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/CMP\/Assessment/);

    await stopImpersonation(page);
  });

  // D-03: impersonate ROLE (tanpa user) → /CMP/Records kosong + hint, BUKAN redirect Login, BUKAN data admin.
  test('IMP-377-D03: impersonate ROLE shows empty + hint in /CMP/Records', async ({ page }) => {
    await login(page, 'admin');
    await startRole(page, 'HC');

    await page.goto('/CMP/Records');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/CMP\/Records/);
    await expect(page.locator('body')).toContainText('Pilih user spesifik', { timeout: 10_000 });

    await stopImpersonation(page);
  });
});

// ==========================================================
// Phase 381 WSE-05 (OPS-01/TOK-03) — StartExam impersonate read-only.
// Admin impersonate worker X buka StartExam X (Open/StartedAt-null/non-token) → ZERO mutasi DB.
// Stop impersonate + worker asli StartExam → BARU saat itu StartedAt ter-set + assignment (SC#3).
// SignalR workerStarted absence diverifikasi via 3 DB-assert (broadcast di-gate sama, RESEARCH A4).
// ==========================================================
test.describe('Phase 381 WSE-05 — StartExam impersonate read-only', () => {
  test('WSE-05: impersonate StartExam no-mutation, then deferred-start on real worker', async ({ page }) => {
    // Setup: HC create Standard assessment milik worker (coachee) + paket berisi 1 soal.
    await login(page, 'hc');
    const title = `Pre Test OJT IMP381 ${Date.now()}`; // 336-NAMING-CONVENTION: '{Stage} Test {Track} {Lokasi}'
    await createAssessmentViaWizard(page, {
      title,
      category: 'OJT',
      scheduleDate: tomorrow(), // future → lolos validasi "not in past"; Status nanti di-force Open via SQL
      scheduleTime: '00:01',
      durationMinutes: 30,
      passPercentage: 60,
      allowAnswerReview: true,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    const sessionId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    expect(sessionId).toBeGreaterThan(0);

    await page.goto(`/Admin/ManagePackages?assessmentId=${sessionId}`);
    await page.waitForLoadState('networkidle');
    const pkg = await createDefaultPackage(page, 'Paket IMP 381');
    await addQuestionViaForm(page, pkg, {
      type: 'MultipleChoice',
      text: 'IMP-Q1 soal impersonate',
      options: ['A', 'B', 'C', 'D'],
      correctIndex: 0,
      score: 100,
    });

    // Precondition: Status='Open', StartedAt=null (kombinasi yang tak bisa dihasilkan worker-GET).
    const updated = await db.queryScalar(
      `UPDATE AssessmentSessions SET Status='Open' WHERE Id=${sessionId}; SELECT @@ROWCOUNT;`
    );
    expect(updated).toBe(1);

    // Admin impersonate worker X → buka StartExam X → ASSERT no mutation.
    await page.context().clearCookies(); // switch HC → admin (Logout = POST-only; clear cookie)
    await login(page, 'admin');
    const target = await resolveUser(page, 'rino', 'Rino');
    await startUser(page, 'rino', target);
    const impResp = await page.goto(`/CMP/StartExam/${sessionId}`);
    await page.waitForLoadState('networkidle');

    // D-06 preview render (auto-cover T3 Bagian A): admin lihat soal read-only TANPA 500/NRE.
    // In-memory assignment (vm.AssignmentId=0) harus feed view tanpa RuntimeBinderException (lesson 354/355).
    expect(impResp?.status() ?? 0).toBeLessThan(400);
    await expect(page.locator('[id^="qcard_"]').first()).toBeVisible({ timeout: 10_000 });

    expect(
      await db.queryScalar(
        `SELECT CASE WHEN StartedAt IS NULL THEN 1 ELSE 0 END FROM AssessmentSessions WHERE Id=${sessionId}`
      )
    ).toBe(1); // StartedAt tetap null
    expect(
      await db.queryScalar(`SELECT COUNT(*) FROM AssessmentSessions WHERE Id=${sessionId} AND Status='Open'`)
    ).toBe(1); // Status tetap Open
    expect(
      await db.queryScalar(`SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId=${sessionId}`)
    ).toBe(0); // tak ada assignment dibuat (broadcast workerStarted di-gate sama → tak fire)

    await stopImpersonation(page);

    // Worker asli login → StartExam → BARU saat itu StartedAt ter-set + 1 assignment (SC#3 deferred-start).
    await page.context().clearCookies(); // kembali ke admin → clear cookie sebelum login worker asli
    await login(page, 'coachee');
    await page.goto(`/CMP/StartExam/${sessionId}`);
    await page.waitForLoadState('networkidle');
    const resumeModal = page.locator('#resumeConfirmModal');
    await resumeModal.waitFor({ state: 'visible', timeout: 5_000 }).catch(() => {});
    if (await resumeModal.isVisible().catch(() => false)) {
      await page.locator('#resumeConfirmBtn').click().catch(() => {});
    }

    expect(
      await db.queryScalar(
        `SELECT CASE WHEN StartedAt IS NOT NULL THEN 1 ELSE 0 END FROM AssessmentSessions WHERE Id=${sessionId}`
      )
    ).toBe(1); // StartedAt ter-set setelah worker asli
    expect(
      await db.queryScalar(`SELECT COUNT(*) FROM UserPackageAssignments WHERE AssessmentSessionId=${sessionId}`)
    ).toBe(1); // 1 assignment ter-persist
  });
});
