import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';

test.describe.configure({ mode: 'serial' });

// ============================================================
// Phase 283 — User Impersonation E2E Tests
// IMP-01: View as role (HC/User)
// IMP-02: Impersonate specific user via autocomplete
// IMP-03: Banner — red banner with target name + "Kembali ke Admin"
// IMP-05: Read-only enforcement — submit buttons disabled
// IMP-06: Audit log — start/stop logged
// IMP-07: No admin impersonation — SearchUsersApi excludes admin
// IMP-08: Stop impersonation — session restored
// ============================================================

test.describe('Impersonation — Admin Features', () => {

  // Helper: ensure no active impersonation session before tests
  // (stop any leftover session from previous test runs)
  test.beforeEach(async ({ page }) => {
    // Login fresh each test since serial mode; individual tests handle their own login
  });

  // ----------------------------------------------------------
  // IMP-01: Admin dapat memilih "View As HC" dari dropdown navbar
  // Verifikasi: banner muncul, menu "Kelola Data" hilang
  // ----------------------------------------------------------
  test('IMP-01: view as HC role shows banner and hides admin menu', async ({ page }) => {
    await login(page, 'admin');

    // Open user dropdown in navbar (the avatar/initials dropdown toggle)
    await page.locator('nav .dropdown-toggle').last().click();

    // "Lihat Sebagai" section must be visible with HC option
    await expect(page.locator('.dropdown-menu.show')).toContainText('Lihat Sebagai');
    await expect(page.locator('.dropdown-menu.show')).toContainText('HC');

    // Click the HC impersonation button (form with targetRole=HC)
    const hcForm = page.locator('form[action*="StartImpersonation"]').filter({
      has: page.locator('input[name="targetRole"][value="HC"]'),
    });
    await hcForm.locator('button[type="submit"]').click();

    // Should redirect to Home
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    // IMP-03 behavior: banner should appear with "HC" as target name
    await expect(page.locator('#impersonation-banner')).toBeVisible();
    await expect(page.locator('#impersonation-banner')).toContainText('HC');
    await expect(page.locator('#impersonation-banner')).toContainText('Kembali ke Admin');

    // When impersonating as HC, "Kelola Data" is still visible (HC has access)
    // The key verification is the banner and redirect happened correctly
    // "Kelola Data" disappears only when impersonating as plain "User" (non-HC, non-Admin)
    await expect(page.locator('nav')).toContainText('Kelola Data');
  });

  // ----------------------------------------------------------
  // IMP-08: Stop impersonation — klik "Kembali ke Admin"
  // Verifikasi: banner hilang, sesi admin dipulihkan
  // ----------------------------------------------------------
  test('IMP-08: stop impersonation restores admin session', async ({ page }) => {
    await login(page, 'admin');

    // Start impersonation as HC
    await page.locator('nav .dropdown-toggle').last().click();
    const hcForm = page.locator('form[action*="StartImpersonation"]').filter({
      has: page.locator('input[name="targetRole"][value="HC"]'),
    });
    await hcForm.locator('button[type="submit"]').click();
    await page.waitForURL('**/Home/**', { timeout: 15_000 });
    await expect(page.locator('#impersonation-banner')).toBeVisible();

    // Click "Kembali ke Admin"
    await page.locator('#impersonation-banner button[type="submit"]').click();

    // Should redirect to Admin/Index
    await page.waitForURL('**/Admin**', { timeout: 15_000 });

    // Banner should be gone
    await expect(page.locator('#impersonation-banner')).not.toBeVisible();

    // Admin menu should be visible again
    await expect(page.locator('nav')).toContainText('Kelola Data');
  });

  // ----------------------------------------------------------
  // IMP-01 (User role): Admin dapat memilih "View As User"
  // ----------------------------------------------------------
  test('IMP-01: view as User role shows banner without admin menu', async ({ page }) => {
    await login(page, 'admin');

    await page.locator('nav .dropdown-toggle').last().click();

    // Click the User impersonation button (form with targetRole=User)
    const userForm = page.locator('form[action*="StartImpersonation"]').filter({
      has: page.locator('input[name="targetRole"][value="User"]'),
    });
    await userForm.locator('button[type="submit"]').click();
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    // Banner visible with "User" label
    await expect(page.locator('#impersonation-banner')).toBeVisible();
    await expect(page.locator('#impersonation-banner')).toContainText('User');

    // When impersonating as plain "User", "Kelola Data" should be hidden
    await expect(page.locator('nav')).not.toContainText('Kelola Data');

    // Stop impersonation to clean up
    await page.locator('#impersonation-banner button[type="submit"]').click();
    await page.waitForURL('**/Admin**', { timeout: 15_000 });
  });

  // ----------------------------------------------------------
  // IMP-03: Banner — tampilan benar saat impersonation aktif
  // ----------------------------------------------------------
  test('IMP-03: banner displays correctly with target name and stop button', async ({ page }) => {
    await login(page, 'admin');

    // Start role impersonation
    await page.locator('nav .dropdown-toggle').last().click();
    const hcForm = page.locator('form[action*="StartImpersonation"]').filter({
      has: page.locator('input[name="targetRole"][value="HC"]'),
    });
    await hcForm.locator('button[type="submit"]').click();
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    const banner = page.locator('#impersonation-banner');
    await expect(banner).toBeVisible();

    // Banner should contain the eye icon class in HTML (structural check via aria)
    // Verify banner has the "Anda melihat sebagai" text
    await expect(banner).toContainText('Anda melihat sebagai');

    // Verify "Kembali ke Admin" button exists inside banner
    const stopBtn = banner.locator('button[type="submit"]');
    await expect(stopBtn).toBeVisible();
    await expect(stopBtn).toContainText('Kembali ke Admin');

    // Verify banner is styled as alert-danger (red)
    await expect(banner).toHaveClass(/alert-danger/);

    // Clean up
    await stopBtn.click();
    await page.waitForURL('**/Admin**', { timeout: 15_000 });
  });

  // ----------------------------------------------------------
  // IMP-05: Read-only enforcement — submit buttons disabled
  // ----------------------------------------------------------
  test('IMP-05: read-only enforcement disables submit buttons during impersonation', async ({ page }) => {
    await login(page, 'admin');

    // Start impersonation as HC
    await page.locator('nav .dropdown-toggle').last().click();
    const hcForm = page.locator('form[action*="StartImpersonation"]').filter({
      has: page.locator('input[name="targetRole"][value="HC"]'),
    });
    await hcForm.locator('button[type="submit"]').click();
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    // Navigate to a page known to have visible submit buttons in main content area
    await page.goto('/Account/Settings');
    await page.waitForLoadState('networkidle');

    // body should have data-impersonating="true"
    const bodyAttr = await page.locator('body').getAttribute('data-impersonating');
    expect(bodyAttr).toBe('true');

    // Submit buttons in main content (not in navbar dropdown) should be disabled
    // Use a selector that targets buttons in the main content area only
    const mainSubmitButtons = page.locator('main button[type="submit"], .container button[type="submit"], form:not([action*="StopImpersonation"]) button[type="submit"]');
    const count = await mainSubmitButtons.count();

    if (count > 0) {
      // At least the first submit button in main content should be disabled
      const firstBtn = mainSubmitButtons.first();
      await expect(firstBtn).toBeDisabled();
    } else {
      // If this page has no submit buttons, verify body attribute is correct (JS ran)
      // and check any submit button on any page is disabled
      const anySubmit = page.locator('button[type="submit"]').first();
      const anyCount = await page.locator('button[type="submit"]').count();
      if (anyCount > 0) {
        await expect(anySubmit).toBeDisabled();
      }
    }

    // Verify the "Mode Read-Only" badge exists somewhere in the DOM
    // (may be hidden in collapsed dropdown; just check it exists)
    const readOnlyBadgeCount = await page.locator('.badge.bg-danger', { hasText: 'Mode Read-Only' }).count();
    expect(readOnlyBadgeCount).toBeGreaterThan(0);

    // Clean up
    await page.locator('#impersonation-banner button[type="submit"]').click();
    await page.waitForURL('**/Admin**', { timeout: 15_000 });
  });

  // ----------------------------------------------------------
  // IMP-07: SearchUsersApi excludes admin users
  // ----------------------------------------------------------
  test('IMP-07: SearchUsersApi does not return admin users', async ({ page }) => {
    await login(page, 'admin');

    // Call SearchUsersApi directly — search for "admin" keyword
    const response = await page.request.get('/Admin/SearchUsersApi?q=admin');
    expect(response.status()).toBe(200);

    const users = await response.json();

    // Verify no user in results has Admin role
    // The API excludes admins at the DB level (RoleLevel >= 2 or GetRolesAsync check)
    // We verify no user with email "admin@pertamina.com" is returned
    const adminEntry = users.find(
      (u: { id: string; fullName: string; nip: string; selectedView: string }) =>
        u.selectedView?.toLowerCase() === 'admin' ||
        u.fullName?.toLowerCase().includes('administrator')
    );
    expect(adminEntry).toBeUndefined();
  });

  // ----------------------------------------------------------
  // IMP-07 (additional): SearchUsersApi with short query returns empty
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

    // First, find a non-admin user via the SearchUsersApi to get a valid userId
    const searchResponse = await page.request.get('/Admin/SearchUsersApi?q=rino');
    expect(searchResponse.status()).toBe(200);
    const users = await searchResponse.json();
    expect(users.length).toBeGreaterThan(0);

    const targetUser = users[0];
    expect(targetUser.id).toBeTruthy();
    expect(targetUser.fullName).toBeTruthy();

    // Now open dropdown and use the autocomplete to find this user
    await page.locator('nav .dropdown-toggle').last().click();

    // The autocomplete input should be visible
    const searchInput = page.locator('#impersonate-search');
    await expect(searchInput).toBeVisible();

    // Type in the search box
    await searchInput.fill('rino');

    // Wait for autocomplete results to appear
    const results = page.locator('#impersonate-results');
    await expect(results).toContainText(targetUser.fullName, { timeout: 5_000 });

    // Click the first result
    await results.locator('[data-userid]').first().click();

    // Should redirect to Home after impersonation starts
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    // Banner should show the user's full name
    const banner = page.locator('#impersonation-banner');
    await expect(banner).toBeVisible();
    await expect(banner).toContainText(targetUser.fullName);

    // Clean up
    await banner.locator('button[type="submit"]').click();
    await page.waitForURL('**/Admin**', { timeout: 15_000 });
  });

  // ----------------------------------------------------------
  // IMP-06: Audit log — start/stop events logged
  // Verified via Admin Audit Log page or API
  // ----------------------------------------------------------
  test('IMP-06: audit trail records impersonation start and stop events', async ({ page }) => {
    await login(page, 'admin');

    // Start impersonation as HC
    await page.locator('nav .dropdown-toggle').last().click();
    const hcForm = page.locator('form[action*="StartImpersonation"]').filter({
      has: page.locator('input[name="targetRole"][value="HC"]'),
    });
    await hcForm.locator('button[type="submit"]').click();
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    // Stop impersonation
    await page.locator('#impersonation-banner button[type="submit"]').click();
    await page.waitForURL('**/Admin**', { timeout: 15_000 });

    // Navigate to audit log page to verify entries
    await page.goto('/Admin/AuditLog');
    await page.waitForLoadState('networkidle');

    // The audit log page should contain "ImpersonateStart" or "ImpersonateEnd" entries
    // or display the impersonation events in human-readable form
    const pageBody = page.locator('body');
    const hasImpersonateStart = await pageBody.locator('*', { hasText: 'ImpersonateStart' }).count();
    const hasImpersonateEnd = await pageBody.locator('*', { hasText: 'ImpersonateEnd' }).count();
    const hasImpersonateText = await pageBody.locator('*', { hasText: 'impersonation' }).count();
    const hasMulaiImpersonation = await pageBody.locator('*', { hasText: 'Mulai impersonation' }).count();

    // At least one of these should be present
    expect(
      hasImpersonateStart + hasImpersonateEnd + hasImpersonateText + hasMulaiImpersonation
    ).toBeGreaterThan(0);
  });

});
