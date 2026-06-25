// Phase 402 — Coaching Cross-Unit Mapping (CXU-01/03/04/05). Runtime contract (Plan 04).
// CXU-01/03/04 assertions filled here; CXU-05 (CDP union) verified via Plan 03 UAT checkpoint (needs login-as-coach + multi-unit fixture).
// Branch ITHandoff: app runs on localhost:5270 (NOT 5277). App is NOT auto-started.
// Run: dotnet run --urls http://localhost:5270 (env Authentication__UseActiveDirectory=false), then:
//      cd tests && E2E_BASE_URL=http://localhost:5270 npx playwright test coaching-crossunit-402 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)
// Data-guard: tests skip when no eligible coachee / no multi-unit fixture (never falsely fail; live seed per CLAUDE.md Seed Workflow).

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

async function openAssignModal(page: Page) {
  await page.click('button[data-bs-target="#assignModal"]');
  await expect(page.locator('#assignModal')).toBeVisible();
}

// Select the first coach option that carries a non-empty data-section. Returns coachSection or '' if none.
async function selectFirstCoachWithSection(page: Page): Promise<string> {
  const coachSection = await page.evaluate(() => {
    const sel = document.getElementById('assignCoachSelect') as HTMLSelectElement | null;
    if (!sel) return '';
    for (let i = 0; i < sel.options.length; i++) {
      const o = sel.options[i];
      if (o.value && o.getAttribute('data-section')) {
        sel.selectedIndex = i;
        sel.dispatchEvent(new Event('change', { bubbles: true }));
        return o.getAttribute('data-section') || '';
      }
    }
    return '';
  });
  return coachSection;
}

test.describe('Phase 402 — Coaching Cross-Unit Mapping', () => {
  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CoachCoacheeMapping');
    await expect(page.locator('h2', { hasText: 'Coach-Coachee Mapping' })).toBeVisible();
  });

  test('CXU-01: coach-first auto-scope filters coachee checklist to coach.Section', async ({ page }) => {
    await openAssignModal(page);
    const coachSection = await selectFirstCoachWithSection(page);
    test.skip(!coachSection, 'no coach with data-section — needs local seed');

    // (a) manual "Filter Seksi Coachee" group hidden
    await expect(page.locator('#coacheeSectionFilterGroup')).toBeHidden();
    // (b) Bagian Penugasan auto-set to coach.Section AND disabled
    await expect(page.locator('#assignAssignmentSection')).toHaveValue(coachSection);
    await expect(page.locator('#assignAssignmentSection')).toBeDisabled();
    // (c) every VISIBLE coachee-item has data-section == coach.Section
    const mismatched = await page.evaluate((sec) => {
      const items = Array.from(document.querySelectorAll('#coacheeChecklist .coachee-item')) as HTMLElement[];
      return items.filter(it => it.style.display !== 'none' && it.getAttribute('data-section') !== sec).length;
    }, coachSection);
    expect(mismatched).toBe(0);
  });

  test('CXU-03: per-row unit dropdown renders only for multi-unit coachees', async ({ page }) => {
    await openAssignModal(page);
    const coachSection = await selectFirstCoachWithSection(page);
    test.skip(!coachSection, 'no coach with data-section — needs local seed');

    const counts = await page.evaluate(() => {
      const items = Array.from(document.querySelectorAll('#coacheeChecklist .coachee-item')) as HTMLElement[];
      let multi = 0, multiWithSelect = 0, singleWithSelect = 0;
      items.forEach(it => {
        let units: string[] = [];
        try { units = JSON.parse(it.getAttribute('data-units') || '[]'); } catch { units = []; }
        const hasSelect = !!it.querySelector('.coachee-unit-select');
        if (units.length > 1) { multi++; if (hasSelect) multiWithSelect++; }
        else if (units.length === 1 && hasSelect) { singleWithSelect++; }
      });
      return { multi, multiWithSelect, singleWithSelect };
    });
    test.skip(counts.multi === 0, 'no multi-unit coachee fixture — needs local seed');
    expect(counts.multiWithSelect).toBe(counts.multi);   // every multi-unit coachee has a per-row select
    expect(counts.singleWithSelect).toBe(0);             // single-unit coachees have NO select
  });

  test('CXU-04: coachees from two units in same Bagian stay enabled + checkable together', async ({ page }) => {
    await openAssignModal(page);
    const coachSection = await selectFirstCoachWithSection(page);
    test.skip(!coachSection, 'no coach with data-section — needs local seed');

    // Find two visible coachees (same Bagian) with different primary unit
    const pair = await page.evaluate(() => {
      const items = Array.from(document.querySelectorAll('#coacheeChecklist .coachee-item')) as HTMLElement[];
      const visible = items.filter(it => it.style.display !== 'none');
      const byUnit: Record<string, string> = {};
      for (const it of visible) {
        let units: string[] = [];
        try { units = JSON.parse(it.getAttribute('data-units') || '[]'); } catch { units = []; }
        const u = units[0];
        const cb = it.querySelector('.coachee-checkbox') as HTMLInputElement | null;
        if (u && cb && !(u in byUnit)) byUnit[u] = cb.id;
      }
      const ids = Object.values(byUnit);
      return ids.length >= 2 ? [ids[0], ids[1]] : [];
    });
    test.skip(pair.length < 2, 'need >=2 visible coachees from different units in same Bagian — local seed');

    await page.locator('#' + pair[0]).check();
    await page.locator('#' + pair[1]).check();
    // IC-3: relaxed lock — neither checkbox disabled, both stay checked
    await expect(page.locator('#' + pair[0])).toBeChecked();
    await expect(page.locator('#' + pair[1])).toBeChecked();
    await expect(page.locator('#' + pair[0])).toBeEnabled();
    await expect(page.locator('#' + pair[1])).toBeEnabled();
  });

  test('CXU-05: coach multi-unit dashboard shows union; per-unit narrows', async ({ page }) => {
    test.skip(true, 'verified via Plan 03 UAT checkpoint — needs login-as-coach + multi-unit fixture');
  });
});
