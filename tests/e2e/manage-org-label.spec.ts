// Phase 344 (TEST-06 + TEST-02c) — E2E for ManageOrgLevelLabels (Level Label CRUD) + app-wide
// label integration + live permission denial. Verifies the v21.0 (Phase 340-343) shipped behavior.
//
// Coverage:
//   TEST-06 sc.1  tree load + legend visible (#org-legend, .org-tier-badge)
//   TEST-06 sc.2  dropdown pre-order (#unitModalParent) + DETERMINISTIC inactive parent " (nonaktif)" (H5)
//   TEST-06 sc.3  cascade warning modal appears with non-zero counts (#cascadeConfirmModal) —
//                 EXACT-count accuracy is MANUAL per D-04 (344-HUMAN-UAT UAT-5); here = modal-presence only
//   TEST-06 sc.4  label rename Bagian->Direktorat => tree badge shows new AND old "Bagian" GONE (H4)
//   TEST-06 sc.5  renamed label visible in 2+ integration pages: /CMP/Records + /Admin/CreateWorker (admin, Pitfall 5)
//   TEST-02c GET  coach authenticated THEN denied at /Admin/ManageOrgLevelLabels -> /Account/AccessDenied (H2)
//   TEST-02c POST coach denied on mutating POST /Admin/UpdateLevelLabel (H3 — the real EoP surface)
//
// Pattern: reused from tests/e2e/manage-assessment-filter.spec.ts — accounts fixture + inline loginAny.
//
// RUN PREREQUISITE (C3): this spec inherits the Phase-315 matrix setup (chromium dependencies:['setup']
// in playwright.config.ts). global.setup.ts runs the FULL matrix pipeline first: BACKUP HcPortalDB_Dev +
// seed 18 AssessmentSessions (IDs 9001-9018) + a pre-check that THROWS if those IDs already exist. The
// dev DB MUST be free of AssessmentSession IDs 9001-9018 before the run, else setup aborts and this spec
// never executes. The matrix BACKUP is NOT a neutral org-only BACKUP — do not assume it just BACKUP/RESTOREs.
// Revert of the label rename does NOT rely solely on the existing matrix DB RESTORE (which is skipped on a
// killed run, Pitfall 4) — there is a MANDATORY afterAll rename-back to "Bagian" (H6).
//
// Run command:
//   cd tests && npx playwright test e2e/manage-org-label.spec.ts

import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

const MANAGE_ORG = '/Admin/ManageOrganization';
const MANAGE_LABELS = '/Admin/ManageOrgLevelLabels';

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

// Navigate to ManageOrganization and wait until the JS-rendered tree has nodes.
async function gotoOrgTree(page: Page) {
  await page.goto(MANAGE_ORG);
  await page.locator('#org-tree-container li.tree-node').first().waitFor({ state: 'visible', timeout: 20_000 });
}

// Rename Level 0 label via the ManageOrgLevelLabels page's own ajaxPost (carries its antiforgery token).
async function setLevel0Label(page: Page, label: string) {
  await page.goto(MANAGE_LABELS);
  await page.locator('#labelEditValue, [onclick^="openEditModal"]').first().waitFor({ state: 'attached', timeout: 15_000 });
  const res = await page.evaluate(async (lbl) => {
    // ajaxPost is defined inline on ManageOrgLevelLabels and appends __RequestVerificationToken.
    // @ts-ignore
    return await ajaxPost('/Admin/UpdateLevelLabel', { level: 0, label: lbl });
  }, label);
  expect((res as any).success, `UpdateLevelLabel(${label}) should succeed`).toBeTruthy();
}

// H6 — MANDATORY revert: restore Level 0 label to "Bagian" regardless of test outcome.
// Does NOT rely on the existing matrix DB RESTORE (skipped if the run is killed mid-way — Pitfall 4).
test.afterAll(async ({ browser }) => {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  try {
    await loginAny(page, 'admin');
    await setLevel0Label(page, 'Bagian');
  } finally {
    await ctx.close();
  }
});

test.describe('Phase 344 — ManageOrgLevelLabels + integrasi label app-wide', () => {

  test('sc.1 tree load + legend visible', async ({ page }) => {
    await loginAny(page, 'admin');
    await gotoOrgTree(page);
    // Legend renders the 3 tier labels.
    const legend = page.locator('#org-legend');
    await expect(legend).toBeVisible();
    await expect(legend.locator('.org-legend-item')).not.toHaveCount(0);
    // At least one tree row carries a tier badge.
    await expect(page.locator('#org-tree-container .org-tier-badge').first()).toBeVisible();
  });

  test('sc.2 dropdown pre-order + deterministic inactive parent " (nonaktif)" (H5)', async ({ page }) => {
    await loginAny(page, 'admin');
    await gotoOrgTree(page);

    // H5 — DETERMINISTICALLY ensure an inactive unit exists: pick an active LEAF unit and toggle it off.
    const leafId = await page.evaluate(() => {
      // @ts-ignore  _flatUnits is the global tree data populated by initTree()
      const units = _flatUnits as Array<{ id: number; parentId: number | null; isActive: boolean }>;
      const hasChild = new Set(units.filter(u => u.parentId != null).map(u => u.parentId));
      const leaf = units.find(u => u.isActive && !hasChild.has(u.id));
      return leaf ? leaf.id : null;
    });
    expect(leafId, 'an active leaf unit must exist to toggle inactive').not.toBeNull();

    // Toggle it inactive (doToggle is dialog-free: ajaxPost + initTree reload).
    await page.evaluate(async (id) => { /* @ts-ignore */ await doToggle(id); }, leafId);
    await page.waitForTimeout(500);

    // Open Add modal -> populates #unitModalParent via populateParentDropdown (pre-order DFS).
    await page.evaluate(() => { /* @ts-ignore */ openAddModal(); });
    await page.locator('#unitModal').waitFor({ state: 'visible', timeout: 10_000 });

    const opts = await page.locator('#unitModalParent option').allTextContents();
    // Drop the placeholder "— Tidak ada (root) —".
    const items = opts.slice(1);
    // Pre-order property: the FIRST real option is a root (no leading indent); at least one option is
    // indented (a child appears AFTER its root, never before the first root).
    const isIndented = (s: string) => /^[\s ]/.test(s);
    expect(items.length, 'dropdown has units').toBeGreaterThan(0);
    expect(isIndented(items[0]), 'first option is a root (pre-order: roots are not indented)').toBeFalsy();
    expect(items.some(isIndented), 'at least one indented child option (hierarchy rendered)').toBeTruthy();
    // Deterministic inactive suffix present.
    expect(items.some(s => s.trimEnd().endsWith('(nonaktif)')), 'an inactive unit shows " (nonaktif)" suffix').toBeTruthy();

    // Close modal + REVERT the toggle so the dev DB returns to baseline.
    await page.evaluate(() => { /* @ts-ignore */ bootstrap.Modal.getInstance(document.getElementById('unitModal'))?.hide(); });
    await page.evaluate(async (id) => { /* @ts-ignore */ await doToggle(id); }, leafId);
  });

  test('sc.3 cascade warning modal appears with non-zero counts (D-04: exact count is MANUAL)', async ({ page }) => {
    await loginAny(page, 'admin');
    await gotoOrgTree(page);

    // Find a unit whose edit has cascade impact. Impact is reported only when the name (or parent)
    // CHANGES — probe with a changed name (current + " ZZ"); parentId unchanged.
    const impacted = await page.evaluate(async () => {
      // @ts-ignore
      const units = _flatUnits as Array<{ id: number; name: string; parentId: number | null }>;
      for (const u of units) {
        // @ts-ignore
        const pv = await ajaxPost('PreviewEditCascade', { id: u.id, name: u.name + ' ZZ', parentId: u.parentId ?? '' });
        const total = (pv.affectedUsersCount || 0) + (pv.affectedMappingsCount || 0)
                    + (pv.affectedKompetensiCount || 0) + (pv.affectedGuidanceCount || 0);
        if (total > 0) return { id: u.id, name: u.name, total };
      }
      return null;
    });
    expect(impacted, 'at least one unit must have cascade impact (seeded org has users)').not.toBeNull();

    // Drive the real edit flow with a CHANGED name so the cascade-confirm modal renders, then ABORT
    // (dismiss without clicking "Lanjut") — submitUnitModal aborts before EditOrganizationUnit, no mutation.
    await page.evaluate((id) => { /* @ts-ignore */ openEditModal(id); }, (impacted as any).id);
    await page.locator('#unitModal').waitFor({ state: 'visible', timeout: 10_000 });
    await page.fill('#unitModalName', (impacted as any).name + ' ZZ');   // change name -> triggers cascade preview
    await page.locator('#unitModalSubmit').click();

    const cascade = page.locator('#cascadeConfirmModal');
    await expect(cascade).toBeVisible({ timeout: 10_000 });
    // Counts present; at least one non-zero (structure/presence — NOT exact value, that is manual UAT-5).
    const sum = await page.evaluate(() =>
      ['cascadeUsers', 'cascadeMappings', 'cascadeKompetensi', 'cascadeGuidance']
        .reduce((a, id) => a + (parseInt(document.getElementById(id)!.textContent || '0', 10) || 0), 0));
    expect(sum, 'cascade modal shows a non-zero total impact count').toBeGreaterThan(0);

    // Batal / dismiss — abort before EditOrganizationUnit (no DB change).
    await page.evaluate(() => { /* @ts-ignore */ bootstrap.Modal.getInstance(document.getElementById('cascadeConfirmModal'))?.hide(); });
  });

  test('sc.4 label rename Bagian->Direktorat: tree badge shows new, OLD "Bagian" gone (H4)', async ({ page }) => {
    await loginAny(page, 'admin');
    await setLevel0Label(page, 'Direktorat');

    await gotoOrgTree(page);
    const level0Badges = page.locator('#org-tree-container .org-tier-badge.level-0');
    await expect(level0Badges.first()).toHaveText('Direktorat');
    // H4: no level-0 tree badge still reads "Bagian" (propagation, not stale coexistence).
    await expect(page.locator('#org-tree-container .org-tier-badge.level-0', { hasText: 'Bagian' })).toHaveCount(0);

    // revert handled by afterAll (H6).
  });

  test('sc.5 renamed label visible in 2+ integration pages (CMP + Worker form, admin)', async ({ page }) => {
    await loginAny(page, 'admin');               // Pitfall 5: CMP Team-View labels render only for roleLevel <= 4
    await setLevel0Label(page, 'Direktorat');

    // Integration page 1: /CMP/Records — the org label renders in the "Team View" tab
    // (RecordsTeam: filter label + "Direktorat" column header), NOT the default "My Records" tab.
    await page.goto('/CMP/Records');
    await page.getByRole('tab', { name: 'Team View' }).click();
    await expect(page.getByRole('columnheader', { name: 'Direktorat' })).toBeVisible({ timeout: 15_000 });

    // Integration page 2: /Admin/CreateWorker — the label renders as a <select> placeholder option
    // ("-- Pilih Direktorat --"). Options inside a closed <select> are NOT toBeVisible() in Playwright,
    // so assert the option EXISTS (text), not its visibility.
    await page.goto('/Admin/CreateWorker');
    await expect(page.locator('option', { hasText: 'Pilih Direktorat' })).toHaveCount(1);

    // revert handled by afterAll (H6).
  });

  test('TEST-02c GET — coach authenticated then denied on OrgLabel page (H2)', async ({ page }) => {
    await loginAny(page, 'coach');
    // Confirm the coach is genuinely AUTHENTICATED (not bounced unauthenticated): reach a coach-allowed page.
    await page.goto('/Home/Index');
    expect(page.url(), 'coach login succeeded (not bounced to Login)').not.toContain('/Account/Login');

    // Now request the Admin/HC-only page -> must land on the SPECIFIC Authorize denial path.
    await page.goto(MANAGE_LABELS);
    expect(page.url(), 'coach denied to AccessDenied, not a Login bounce').toContain('/Account/AccessDenied');
  });

  test('TEST-02c POST — coach denied on mutating POST UpdateLevelLabel (H3 EoP)', async ({ page }) => {
    await loginAny(page, 'coach');
    // Issue the privileged mutation directly as the authenticated coach. Authorization (role) is enforced
    // by the pipeline BEFORE the action/antiforgery, so this is rejected (403 or AccessDenied redirect),
    // never a successful 200 mutation. A __RequestVerificationToken field is included for realism.
    const resp = await page.request.post('/Admin/UpdateLevelLabel', {
      form: { level: '0', label: 'Hacked', __RequestVerificationToken: 'x' },
      headers: { 'X-Requested-With': 'XMLHttpRequest' },
      maxRedirects: 0,
    });
    const status = resp.status();
    const denied = status === 403
      || (status === 302 && (resp.headers()['location'] || '').includes('AccessDenied'));
    expect(denied, `coach POST must be denied (403 or AccessDenied redirect); got ${status} -> ${resp.headers()['location'] || ''}`).toBeTruthy();
  });

});
