// Phase 322 Nyquist validation — regression spec untuk Filter Scope Per Tab ManageAssessment.
//
// Coverage 8 gap (per /gsd-validate-phase 322 audit):
//   FILTER-01    Bug 1 fix — no double filter Tab 1 (shell shared form deleted)
//   FILTER-02a   Bug 2 prevention (D-21 Strategy D) — Tab 2 switch XHR drop category/statusFilter/search
//   FILTER-02b   D-10 URL bookmark backward compat — Tab 1 initial load XHR include filter + dropdown pre-selected
//   FILTER-03    Bug 3 fix — pagination preserve filter via hx-include #filterFormAssessment (conditional skip)
//   FILTER-04    Cascade Bagian → Unit di Tab 2 (XHR section + unit cleared pre-HTMX)
//   FILTER-05    Sub-tab Riwayat Training filter NEW client-side (no XHR fire)
//   REGRESSION-A ViewBag null coalesce fix (commit 6ecb7a50) — textbox value NOT literal "null"
//   REGRESSION-B HTMX hx-vals inheritance fix (commit 773c970c, CRITICAL) — wrapper TIDAK punya hx-vals
//
// Pattern: reused dari tests/e2e/edit-peserta-answers.spec.ts (Phase 321) + export-per-peserta (Phase 320):
//   - accounts fixture lowercase dari ../helpers/accounts
//   - Inline loginAny accept any redirect away dari /Account/Login
//
// Pre-requisite:
//   - dotnet watch run di port 5277 (server harus jalan sebelum test run)
//   - DB lokal minimal 1 grup assessment exists (UAT lokal: "Test Pre-Post UAT 297" OJT Upcoming)
//   - Login admin@pertamina.com / 123456
//
// Run command:
//   cd tests && npx playwright test e2e/manage-assessment-filter.spec.ts

import { test, expect, type Page, type Request } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

const MANAGE_URL = '/Admin/ManageAssessment';
const TAB_ASSESSMENT_ENDPOINT = '/Admin/ManageAssessmentTab_Assessment';
const TAB_TRAINING_ENDPOINT = '/Admin/ManageAssessmentTab_Training';

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

// Helper: collect URLs hitting Tab 1/Tab 2 partial endpoint. Returns array reference yang
// di-mutate on-the-fly oleh page.on('request') listener.
function collectPartialRequests(page: Page, endpoint: string): string[] {
  const urls: string[] = [];
  page.on('request', (req: Request) => {
    const url = req.url();
    if (url.includes(endpoint)) {
      urls.push(url);
    }
  });
  return urls;
}

// Wait Tab 1 partial swap selesai (filter form rendered) sebelum lanjut interact.
async function waitTab1Loaded(page: Page) {
  await page.locator('#filterFormAssessment').waitFor({ state: 'visible', timeout: 15_000 });
}

test.describe('Phase 322 — Filter Scope Per Tab ManageAssessment', () => {

  test('FILTER-01: Tab Assessment Groups initial load — no double filter row (Bug 1)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(MANAGE_URL);
    await waitTab1Loaded(page);

    // Exactly 1 form #filterFormAssessment visible (partial Tab 1)
    const filterFormPartial = page.locator('#filterFormAssessment');
    await expect(filterFormPartial).toHaveCount(1);
    await expect(filterFormPartial).toBeVisible();

    // Shell shared #filter-form deleted (Phase 322 PLAN 02 Task 5a)
    const shellFilterForm = page.locator('#filter-form');
    await expect(shellFilterForm).toHaveCount(0);
  });

  test('FILTER-02b: URL bookmark Tab 1 — initial XHR include filter + dropdown pre-selected (D-10)', async ({ page }) => {
    await loginAny(page, 'admin');
    const tab1Urls = collectPartialRequests(page, TAB_ASSESSMENT_ENDPOINT);

    await page.goto(`${MANAGE_URL}?category=OJT&statusFilter=Open`);
    await waitTab1Loaded(page);

    // XHR initial load Tab 1 wrapper hx-get URL query string include filter params
    const tab1Xhr = tab1Urls.find(u => u.includes('category=OJT'));
    expect(tab1Xhr, `Expected Tab 1 XHR with category=OJT, got: ${JSON.stringify(tab1Urls)}`).toBeTruthy();
    expect(tab1Xhr!).toContain('category=OJT');
    expect(tab1Xhr!).toContain('statusFilter=Open');
    expect(tab1Xhr!).toContain('page=1');

    // Dropdown Kategori pre-selected OJT
    const categoryDropdown = page.locator('#categoryFilter');
    await expect(categoryDropdown).toHaveValue('OJT');

    // Dropdown Status pre-selected Open
    const statusDropdown = page.locator('#statusFilter');
    await expect(statusDropdown).toHaveValue('Open');

    // Reset Filter button visible (rendered conditional because filter aktif)
    const resetBtn = page.getByRole('button', { name: /Reset Filter/i });
    await expect(resetBtn).toBeVisible();
  });

  test('FILTER-02a: Tab 2 switch from URL bookmark Tab 1 — XHR drop category/statusFilter/search (Bug 2 prevention, D-21)', async ({ page }) => {
    await loginAny(page, 'admin');
    const tab2Urls = collectPartialRequests(page, TAB_TRAINING_ENDPOINT);

    // Pre-condition Step 12a: URL bookmark Tab 1 active
    await page.goto(`${MANAGE_URL}?category=OJT&statusFilter=Open`);
    await waitTab1Loaded(page);

    // Click Tab 2 → trigger XHR ke ManageAssessmentTab_Training
    await page.locator('#tab-training').click();
    // Wait sampai partial Tab 2 form rendered di DOM (state: attached BUKAN visible —
    // Bootstrap tab pane fade keeps #pane-training display:none sampai animation done,
    // tapi DOM partial swap completes via HTMX shown.bs.tab trigger sebelum animation).
    await page.locator('#filterFormTraining').waitFor({ state: 'attached', timeout: 15_000 });
    await page.waitForLoadState('networkidle');

    // Tab 2 XHR HARUS exactly section/unit/page only — NO category, NO statusFilter, NO search
    const tab2Xhr = tab2Urls[0];
    expect(tab2Xhr, `Expected Tab 2 XHR, got: ${JSON.stringify(tab2Urls)}`).toBeTruthy();

    // D-21 Strategy D: TIDAK ada category=OJT atau statusFilter=Open (cross-tab contamination prevention)
    expect(tab2Xhr!).not.toContain('category=OJT');
    expect(tab2Xhr!).not.toContain('statusFilter=Open');
    // Tab 2 wrapper urlTraining hanya section + unit + page (per ManageAssessment.cshtml baris 17)
    expect(tab2Xhr!).toContain('section=');
    expect(tab2Xhr!).toContain('unit=');
    expect(tab2Xhr!).toContain('page=');

    // All Tab 2 form fields empty (default state, tidak ter-set dari URL bookmark Tab 1)
    await expect(page.locator('#filterSection')).toHaveValue('');
    await expect(page.locator('#filterCategory')).toHaveValue('');
    await expect(page.locator('#filterUnit')).toHaveValue('');
    await expect(page.locator('#filterStatus')).toHaveValue('');
    await expect(page.locator('#searchTraining')).toHaveValue('');
  });

  test('FILTER-03: pagination preserve filter state via hx-include (Bug 3, conditional skip if totalPages<=1)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(MANAGE_URL);
    await waitTab1Loaded(page);

    // Check totalPages via pagination block. Skip kalau DB lokal cuma 1 page.
    const paginationNav = page.locator('nav[aria-label="Assessment pagination"]');
    const hasPagination = await paginationNav.isVisible().catch(() => false);
    test.skip(!hasPagination, 'DB lokal totalPages <= 1, pagination block tidak rendered — skip runtime test (bonus fix verified via code review per UAT.md Step 3).');

    // Verify pagination buttons punya hx-include="#filterFormAssessment"
    const pageButtons = paginationNav.locator('button.page-link[hx-include="#filterFormAssessment"]');
    const buttonCount = await pageButtons.count();
    expect(buttonCount).toBeGreaterThan(0);

    // Apply filter Kategori=OJT first
    await page.locator('#categoryFilter').selectOption('OJT');
    await page.waitForLoadState('networkidle');

    // Collect XHR pagination click
    const tab1Urls = collectPartialRequests(page, TAB_ASSESSMENT_ENDPOINT);

    // Click page 2 button (if exists)
    const page2Btn = paginationNav.locator('button.page-link', { hasText: '2' }).first();
    if (await page2Btn.isVisible().catch(() => false)) {
      await page2Btn.click();
      await page.waitForLoadState('networkidle');

      const paginationXhr = tab1Urls.find(u => u.includes('page=2'));
      expect(paginationXhr, `Expected page=2 XHR, got: ${JSON.stringify(tab1Urls)}`).toBeTruthy();
      // Filter state preserved via hx-include — category=OJT IKUT serta
      expect(paginationXhr!).toContain('category=OJT');
    }
  });

  test('FILTER-04: cascade Bagian → Unit di Tab 2 (XHR + Unit value cleared pre-HTMX)', async ({ page }) => {
    await loginAny(page, 'admin');
    const tab2Urls = collectPartialRequests(page, TAB_TRAINING_ENDPOINT);

    await page.goto(`${MANAGE_URL}?tab=training`);
    await page.locator('#filterFormTraining').waitFor({ state: 'attached', timeout: 15_000 });
    await page.waitForLoadState('networkidle');

    // Capture XHR count before change (initial load XHR akan ada juga)
    const xhrCountBefore = tab2Urls.length;

    // Section dropdown has options selain "" (kalau DB punya data section)
    const sectionDropdown = page.locator('#filterSection');
    const sectionOptions = await sectionDropdown.locator('option').count();
    test.skip(sectionOptions <= 1, 'DB lokal tidak punya section data — skip cascade test.');

    // Select first non-empty section option
    const firstSectionValue = await sectionDropdown.locator('option').nth(1).getAttribute('value');
    expect(firstSectionValue).toBeTruthy();

    // Pakai evaluate untuk bypass Bootstrap tab pane display:none — Playwright selectOption
    // require element visible. Manual UAT approach: set option.selected + dispatch change event.
    await page.evaluate((val: string) => {
      const sel = document.getElementById('filterSection') as HTMLSelectElement;
      Array.from(sel.options).forEach(o => o.selected = (o.value === val));
      sel.value = val;
      sel.dispatchEvent(new Event('change', { bubbles: true }));
    }, firstSectionValue!);
    await page.waitForLoadState('networkidle');

    // XHR fired dengan section=<value>
    const newXhrs = tab2Urls.slice(xhrCountBefore);
    const sectionXhr = newXhrs.find(u => u.includes(`section=${encodeURIComponent(firstSectionValue!)}`));
    expect(sectionXhr, `Expected XHR with section=${firstSectionValue}, got: ${JSON.stringify(newXhrs)}`).toBeTruthy();

    // Pre-HTMX onchange clear Unit (value="" sebelum HTMX fire). After cascade, Unit dropdown
    // populate dengan unit dari section terpilih — Unit value empty (cleared) atau di-populate ulang.
    // Read value via JS (element hidden by Bootstrap tab pane fade — toHaveValue triggers visibility check)
    const unitValue = await page.evaluate(() => (document.getElementById('filterUnit') as HTMLSelectElement)?.value);
    expect(unitValue).toBe('');
  });

  test('FILTER-05: sub-tab Riwayat Training filter NEW client-side (no XHR fire)', async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto(`${MANAGE_URL}?tab=history`);

    // Wait Tab 3 partial loaded — sub-tab nav attached (Bootstrap fade may hide initially)
    await page.locator('#historySubTabs').waitFor({ state: 'attached', timeout: 15_000 });
    await page.waitForLoadState('networkidle');

    // Click sub-tab Riwayat Training
    await page.locator('#riwayat-training-tab').click();
    const riwayatTrainingPane = page.locator('#riwayat-training-pane');
    await riwayatTrainingPane.waitFor({ state: 'attached', timeout: 5_000 });

    // Skip kalau empty state (no training history data)
    const trainingTable = page.locator('#trainingHistoryTable');
    const tableAttached = (await trainingTable.count()) > 0;
    test.skip(!tableAttached, 'DB lokal empty trainingHistory — skip filter client-side test.');

    // DOM structure assertions (per PLAN 01 Task 4) — element attached cukup; visibility tergantung Bootstrap fade
    const filterInput = page.locator('#trainingWorkerFilter');
    await expect(filterInput).toBeAttached();
    await expect(filterInput).toHaveAttribute('oninput', /filterTrainingRows\(\)/);

    const rows = page.locator('.training-history-row');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(0);

    // Each row must have data-worker attribute
    const firstRowDataWorker = await rows.first().getAttribute('data-worker');
    expect(firstRowDataWorker).toBeTruthy();

    // Type di filter → rows non-match hidden via display:none. Capture XHR network drop check.
    let xhrCount = 0;
    const xhrListener = (req: Request) => {
      // Only count XHR ke partial endpoints — bukan resource (CSS/JS/image)
      const url = req.url();
      if (url.includes('/Admin/ManageAssessmentTab_') && req.method() === 'GET') {
        xhrCount++;
      }
    };
    page.on('request', xhrListener);

    // Pakai unique random string yang TIDAK match data — kepastian semua rows hidden.
    // Pakai evaluate (bypass Bootstrap tab pane display:none — Playwright fill require visible).
    await page.evaluate(() => {
      const inp = document.getElementById('trainingWorkerFilter') as HTMLInputElement;
      inp.value = 'zzzz_unlikely_match_xyz';
      inp.dispatchEvent(new Event('input', { bubbles: true }));
    });
    // small wait untuk JS oninput sync execute
    await page.waitForTimeout(500);

    // All rows hidden (display:none) karena no match
    const visibleAfter = await rows.evaluateAll(els =>
      els.filter(el => (el as HTMLElement).style.display !== 'none').length
    );
    expect(visibleAfter).toBe(0);

    // NO XHR fire (client-side filter network-free)
    expect(xhrCount).toBe(0);

    page.off('request', xhrListener);
  });

  test('REGRESSION-A: clean URL — search textbox value="" NOT literal "null" (commit 6ecb7a50)', async ({ page }) => {
    await loginAny(page, 'admin');
    const tab1Urls = collectPartialRequests(page, TAB_ASSESSMENT_ENDPOINT);

    // Clean URL, no query string
    await page.goto(MANAGE_URL);
    await waitTab1Loaded(page);

    // Textbox value MUST be empty, NOT "null" literal string
    const searchInput = page.locator('#searchAssessment');
    await expect(searchInput).toHaveValue('');
    const value = await searchInput.inputValue();
    expect(value).not.toBe('null');

    // Wrapper Tab 1 initial XHR — search= (empty), NOT search=null
    const tab1Xhr = tab1Urls[0];
    expect(tab1Xhr, `Expected Tab 1 XHR, got: ${JSON.stringify(tab1Urls)}`).toBeTruthy();
    expect(tab1Xhr!).not.toContain('search=null');
    expect(tab1Xhr!).not.toContain('category=null');
    expect(tab1Xhr!).not.toContain('statusFilter=null');
    // Empty params expected: search=&category=&statusFilter=
    expect(tab1Xhr!).toMatch(/search=(&|$)/);
  });

  test('REGRESSION-B: wrapper TIDAK punya hx-vals attribute, hanya hx-get URL query string (commit 773c970c CRITICAL)', async ({ page }) => {
    await loginAny(page, 'admin');
    const tab1Urls = collectPartialRequests(page, TAB_ASSESSMENT_ENDPOINT);

    await page.goto(MANAGE_URL);
    await waitTab1Loaded(page);

    // Verify wrapper Tab 1 element has hx-get attribute WITHOUT hx-vals (avoid HTMX inheritance bug)
    const wrapper = page.locator('#pane-assessment .htmx-tab-wrapper');
    await expect(wrapper).toHaveCount(1);

    const hxGet = await wrapper.getAttribute('hx-get');
    expect(hxGet, 'wrapper Tab 1 must have hx-get attribute').toBeTruthy();
    expect(hxGet!).toContain('/Admin/ManageAssessmentTab_Assessment');

    // CRITICAL: wrapper MUST NOT have hx-vals attribute (fix commit 773c970c migrated to URL query string)
    const hxVals = await wrapper.getAttribute('hx-vals');
    expect(hxVals, `wrapper Tab 1 must NOT have hx-vals attribute (hx-vals inheritance bug). Got: ${hxVals}`).toBeNull();

    // Same check untuk wrapper Tab 2 + Tab 3
    const wrapperTab2 = page.locator('#pane-training .htmx-tab-wrapper');
    const hxValsTab2 = await wrapperTab2.getAttribute('hx-vals');
    expect(hxValsTab2, `wrapper Tab 2 must NOT have hx-vals. Got: ${hxValsTab2}`).toBeNull();

    const wrapperTab3 = page.locator('#pane-history .htmx-tab-wrapper');
    const hxValsTab3 = await wrapperTab3.getAttribute('hx-vals');
    expect(hxValsTab3, `wrapper Tab 3 must NOT have hx-vals. Got: ${hxValsTab3}`).toBeNull();

    // Functional verify: change dropdown Kategori ke OJT → XHR include category=OJT (form data WIN)
    const xhrCountBefore = tab1Urls.length;
    await page.locator('#categoryFilter').selectOption('OJT');
    await page.waitForLoadState('networkidle');

    const newXhrs = tab1Urls.slice(xhrCountBefore);
    const categoryXhr = newXhrs.find(u => u.includes('category=OJT'));
    expect(categoryXhr,
      `Expected XHR with category=OJT (form data WIN over wrapper hx-vals). Got: ${JSON.stringify(newXhrs)}`
    ).toBeTruthy();
    // Verify form data dropdown serialize correctly — no empty category= override
    expect(categoryXhr!).toContain('category=OJT');
  });

});
