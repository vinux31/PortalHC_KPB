// Phase 402 — Coaching Cross-Unit Mapping (CXU-01/03/04/05). TEST-FIRST skeleton (Plan 01).
// Bodies filled in Plan 04 (assign modal UI) + verified runtime (Lesson Phase 354).
// Branch ITHandoff: app runs on localhost:5270 (NOT 5277).
// Run: cd tests && E2E_BASE_URL=http://localhost:5270 npx playwright test coaching-crossunit-402 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Phase 402 — Coaching Cross-Unit Mapping', () => {
  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CoachCoacheeMapping');
    await expect(page.locator('h2', { hasText: 'Coach-Coachee Mapping' })).toBeVisible();
  });

  test('CXU-01: coach-first auto-scope filters coachee checklist to coach.Section', async ({ page }) => {
    test.skip(true, 'pending Plan 04 UI wiring (coach-first auto-scope)');
  });
  test('CXU-03: per-row unit dropdown renders only for multi-unit coachees', async ({ page }) => {
    test.skip(true, 'pending Plan 04 UI wiring (conditional per-row unit select)');
  });
  test('CXU-04: coachees from two units in same Bagian stay enabled + submit succeeds', async ({ page }) => {
    test.skip(true, 'pending Plan 04 UI wiring (relax lock to Bagian-level)');
  });
  test('CXU-05: coach multi-unit dashboard shows union; per-unit narrows', async ({ page }) => {
    test.skip(true, 'pending Plan 03 CDP self-scope + Plan 04 (requires multi-unit fixture)');
  });
});
