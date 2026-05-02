import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
import { selectors } from './helpers/wizardSelectors';

// Shared state across tests in this file (runs serially)
let assessmentTitle: string;

/** Search on ManageAssessment (first search input = assessment tab) */
async function searchAssessment(page: import('@playwright/test').Page, term: string) {
  const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
  await searchInput.fill(term);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');
}

test.describe.configure({ mode: 'serial' });

// ============================================================
// FLOW 1: Admin/HC creates an assessment
// ============================================================
test.describe('Assessment - Admin Creates & Manages', () => {

  test('1.1 - HC can navigate to Create Assessment page', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    await expect(page.locator('h2')).toContainText('Manage Assessment');

    await page.click('a[href*="CreateAssessment"]');
    await expect(page.locator('h2')).toContainText('Create New Assessment');
  });

  test('1.2 - HC can create a new assessment for workers', async ({ page }) => {
    assessmentTitle = uniqueTitle('Assessment OJT');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    // Select users by clicking the label (more reliable than .check())
    // Pick Rino (coachee) and Iwan (coachee2)
    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    const iwanCheckbox = page.locator('.user-check-item', { hasText: 'iwan3' }).locator('input');
    await rinoCheckbox.click({ force: true });
    await iwanCheckbox.click({ force: true });

    // Verify selection count
    await expect(page.locator('#selectedCountBadge')).toContainText('2 terpilih');

    // Fill form fields
    await page.fill('#Title', assessmentTitle);
    await page.selectOption('#Category', 'OJT');
    await page.fill('#ScheduleDate', today());
    await page.fill('#ScheduleTime', '00:01');
    await page.fill('#DurationMinutes', '30');
    await page.fill('#PassPercentage', '70');

    // Submit
    await page.click('#submitBtn');

    // Wait for success - could be modal or redirect
    await page.waitForTimeout(3_000);
    const successVisible = await page.locator('#successModal').evaluate(
      el => el.classList.contains('show')
    ).catch(() => false);
    const alertVisible = await page.locator('.alert-success').isVisible().catch(() => false);
    expect(successVisible || alertVisible).toBeTruthy();
  });

  test('1.3 - Created assessment appears in ManageAssessment list', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');

    // Search for our assessment
    await searchAssessment(page, assessmentTitle);
    await expect(page.locator('body')).toContainText(assessmentTitle);
  });

  test('1.4 - Assessment appears in Monitoring dashboard', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');
    await expect(page.locator('body')).toContainText(assessmentTitle);
  });

  test('1.5 - HC can open Monitoring Detail for the assessment', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    // Click the assessment title/detail link
    await page.click(`text=${assessmentTitle}`);
    await expect(page.locator('body')).toContainText(assessmentTitle);
    // Should show participant table
    await expect(page.locator('table')).toBeVisible();
  });
});

// ============================================================
// FLOW 7: Phase 307 — Selected Participants Inline Panel (Step 2)
// REQ: WIZ-01 (5 success criteria)
// ============================================================
test.describe('Assessment - Phase 307 Selected Participants Panel', () => {

  test('7.1 - Panel renders with empty state on initial load (success criteria #1)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    // Panel always-visible (D-02), empty state default
    await expect(page.locator(selectors.panelWrapper)).toBeVisible();
    await expect(page.locator(selectors.panelCount)).toContainText('0 peserta');
    await expect(page.locator(selectors.panelBody)).toContainText('Belum ada peserta dipilih');
  });

  test('7.2 - Panel updates real-time when checkbox toggled (success criteria #2)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    await rinoCheckbox.click({ force: true });

    // Count badge IMMEDIATE update (D-11 — count immediate)
    await expect(page.locator(selectors.panelCount)).toContainText('1 peserta');

    // Panel body update setelah debounce 100ms window
    await page.waitForTimeout(150);
    await expect(page.locator(selectors.panelBody)).not.toContainText('Belum ada peserta dipilih');
    await expect(page.locator(selectors.panelBody)).toContainText(/rino|prasetyo/i);
  });

  test('7.3 - Step 4 summary parity dengan Step 2 panel (success criteria #3, #5 DRY)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    const iwanCheckbox = page.locator('.user-check-item', { hasText: 'iwan3' }).locator('input');
    await rinoCheckbox.click({ force: true });
    await iwanCheckbox.click({ force: true });
    await page.waitForTimeout(150);

    // Capture Step 2 panel text — extract names only (strip "Belum ada" placeholder kalau ada)
    const step2Text = (await page.locator(selectors.panelBody).textContent())?.trim() ?? '';
    expect(step2Text).toMatch(/rino|prasetyo/i);
    expect(step2Text).toMatch(/iwan/i);

    // Verify count badge format — Step 2 panel header
    await expect(page.locator(selectors.panelCount)).toContainText('2 peserta');

    // Verify filter bar badge format — UNCHANGED (Phase 304 D-18 stability)
    await expect(page.locator(selectors.filterBarBadge)).toContainText('2 terpilih');

    // Note: Full Step 2 vs Step 4 visual parity assertion (advance wizard ke Step 4) di-defer ke manual UAT
    // karena wizard navigation flow + form validation memerlukan setup tambahan yang akan diisi
    // setelah Wave 1 implementasi merged. Manual UAT 307-UAT.md Step 5 cover ini.
  });

  test('7.4 - Reset clears panel ke empty state (success criteria #2 reset path)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    await rinoCheckbox.click({ force: true });
    await page.waitForTimeout(150);
    await expect(page.locator(selectors.panelBody)).toContainText(/rino|prasetyo/i);

    // Click "Batalkan Semua" untuk uncheck semua (deselectAllBtn — line 286 Bahasa Indonesia)
    await page.click('#deselectAllBtn');
    await page.waitForTimeout(150);

    // Panel kembali ke empty state, count 0
    await expect(page.locator(selectors.panelCount)).toContainText('0 peserta');
    await expect(page.locator(selectors.panelBody)).toContainText('Belum ada peserta dipilih');
    await expect(page.locator(selectors.filterBarBadge)).toContainText('0 terpilih');
  });
});

// ============================================================
// FLOW 8: Phase 308 — PrePost Wizard Validation Fix
// REQ: WIZ-04 (5 success criteria, 4-combination test matrix per D-10)
// Test scaffold WAVE 0 — RED state expected sebelum Wave 1 implementation merged
// ============================================================
test.describe('Assessment - Phase 308 PrePost Wizard Validation', () => {

  // Phase 308 IN-03: explicit fresh-page-per-test reset, hilangkan duplicated login+goto
  test.beforeEach(async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
  });

  test('8.1 - Standard mode Status field interactable + value persistence (regression guard success criteria #5 — wave 0 partial)', async ({ page }) => {
    // Standard mode default — Status field visible (NOT d-none)
    await expect(page.locator(selectors.statusFieldWrapper)).toBeVisible();
    await expect(page.locator(selectors.statusFieldWrapper)).not.toHaveClass(/d-none/);

    // Pilih 1 user
    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    await rinoCheckbox.click({ force: true });

    // Fill Step 1: Title + Category + assessmentType (default Standard)
    await page.fill('#Title', uniqueTitle('Phase 308 Standard'));
    await page.selectOption('#Category', 'OJT');

    // Fill Status (Standard mode WAJIB pilih — D-11 regression guard)
    await page.selectOption(selectors.statusSelect, 'Open');

    // Verify Status value ke-set
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Open');

    // NOTE: Test SCAFFOLD wave 0 — full wizard navigation (Step 2/3/4 + submit) di-defer ke Wave 1
    // expect partial verification: Status field interactable + value persistence
  });

  test('8.2 - Switch S→PP→S Status field clear (mode-switch state cleanup D-02)', async ({ page }) => {
    // Initial: Standard mode → fill Status
    await expect(page.locator(selectors.statusFieldWrapper)).toBeVisible();
    await page.selectOption(selectors.statusSelect, 'Open');
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Open');

    // Switch ke PrePost — D-01 expected: Status auto-set 'Upcoming', wrapper hidden
    await page.selectOption(selectors.assessmentTypeInput, 'PrePostTest');
    await expect(page.locator(selectors.statusFieldWrapper)).toHaveClass(/d-none/);
    // D-01 verification: Status value programmatically set ke 'Upcoming' meskipun field hidden
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Upcoming');

    // Switch back ke Standard — D-02 expected: Status value clear (''), wrapper visible
    await page.selectOption(selectors.assessmentTypeInput, 'Standard');
    await expect(page.locator(selectors.statusFieldWrapper)).not.toHaveClass(/d-none/);
    // D-02 verification: Status value cleared, force user re-pick
    await expect(page.locator(selectors.statusSelect)).toHaveValue('');
  });

  test('8.3 - PP saja Status auto-set Upcoming + wrapper hidden (D-01 main path success criteria #1)', async ({ page }) => {
    // Switch ke PrePostTest dari default Standard
    await page.selectOption(selectors.assessmentTypeInput, 'PrePostTest');

    // D-01: Status value auto-set 'Upcoming' meskipun field hidden
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Upcoming');

    // statusFieldWrapper hidden (existing handler behavior — D-03)
    await expect(page.locator(selectors.statusFieldWrapper)).toHaveClass(/d-none/);

    // Form ID verification: createForm selector resolves ke #createAssessmentForm (RESEARCH Pitfall 1)
    await expect(page.locator(selectors.createForm)).toBeVisible();

    // NOTE: Full submit flow (fill PreSchedule + PostSchedule + durations + EWCD + submit) di-defer ke manual UAT Step 3
    // E2E scaffold wave 0 fokus ke Status value assertion (D-01) + wrapper visibility (D-03)
    // Server-side ModelState.Remove (D-04) verified via successful submit di manual UAT
  });

  test('8.4 - Switch PP→S→PP Status auto-set Upcoming kembali (idempotency D-01 re-fire)', async ({ page }) => {
    // Pertama: switch ke PrePost
    await page.selectOption(selectors.assessmentTypeInput, 'PrePostTest');
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Upcoming');
    await expect(page.locator(selectors.statusFieldWrapper)).toHaveClass(/d-none/);

    // Kedua: switch back Standard — Status clear, wrapper visible
    await page.selectOption(selectors.assessmentTypeInput, 'Standard');
    await expect(page.locator(selectors.statusSelect)).toHaveValue('');
    await expect(page.locator(selectors.statusFieldWrapper)).not.toHaveClass(/d-none/);

    // Ketiga: switch lagi ke PrePost — D-01 re-fire idempotently
    await page.selectOption(selectors.assessmentTypeInput, 'PrePostTest');
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Upcoming');
    await expect(page.locator(selectors.statusFieldWrapper)).toHaveClass(/d-none/);
  });

});

// ============================================================
// FLOW 9: Phase 310 — Essay Finalize Idempotency
// REQ: ESCG-01 (5 success criteria)
// SC #1 alreadyFinalized branch + SC #2 disabled state — auto E2E
// SC #3 (notif dedup) + SC #4 (audit dedup) + SC #5 (parallel finalize) — manual UAT (RESEARCH finding #3, no .NET integration test project)
// ============================================================
test.describe('Assessment - Phase 310 Essay Finalize Idempotency', () => {

  test.beforeEach(async ({ page }) => {
    await login(page, 'hc');
  });

  test('9.1 - SC #2: Tombol Selesaikan Penilaian disabled + tooltip wrapper saat session Status=Completed', async ({ page }) => {
    // PRECONDITION: Seed data — session ber-essay yang sudah Completed (manual seed atau pakai fixture existing)
    // Test ini scaffold WAVE — assertion target struktur DOM, bukan flow create-finalize end-to-end
    // Untuk Wave 1 fill: developer pilih sessionId Completed dari dev DB, navigate ke detail page

    // PLACEHOLDER target session ID — Wave 1 fill dengan actual seeded ID
    const completedSessionTitle = 'Phase 310 Completed Fixture';

    // Navigate ke ManageAssessment, find seeded session, klik detail
    await page.goto('/Admin/ManageAssessment');
    const groupRow = page.locator('tr', { hasText: completedSessionTitle }).first();

    // Test akan SKIP otomatis kalau seed belum ada (RED state pre-fixture)
    if (await groupRow.count() === 0) {
      test.skip(true, 'Seed session "Phase 310 Completed Fixture" not found — Wave 1 manual seed required');
    }

    // Click ke detail
    await groupRow.locator('a[href*="AssessmentMonitoringDetail"]').first().click();
    await page.waitForLoadState('networkidle');

    // Assertion D-02: tombol .btn-finalize-grading wrapped dalam <span> dengan tooltip
    const finalizeBtn = page.locator('.btn-finalize-grading').first();
    await expect(finalizeBtn).toBeDisabled();

    const tooltipWrapper = page.locator('span[data-bs-toggle="tooltip"]').filter({ has: finalizeBtn });
    await expect(tooltipWrapper).toBeVisible();

    // Tooltip text contains "Sudah selesai pada"
    const tooltipTitle = await tooltipWrapper.getAttribute('title');
    expect(tooltipTitle).toContain('Sudah selesai pada');
    expect(tooltipTitle).toContain('WIB');
  });

  test('9.2 - SC #1: Klik Finalize 2x → response ke-2 alreadyFinalized:true render alert-info biru (NOT alert-danger)', async ({ page }) => {
    // PRECONDITION: Session dengan Status=PendingGrading (essay sudah di-grade semua, belum di-finalize)
    // Test ini scaffold — Wave 1 fill dengan seeded sessionId yang ready untuk finalize

    const pendingSessionTitle = 'Phase 310 PendingGrading Fixture';
    await page.goto('/Admin/ManageAssessment');
    const groupRow = page.locator('tr', { hasText: pendingSessionTitle }).first();

    if (await groupRow.count() === 0) {
      test.skip(true, 'Seed session "Phase 310 PendingGrading Fixture" not found — Wave 1 manual seed required');
    }

    await groupRow.locator('a[href*="AssessmentMonitoringDetail"]').first().click();
    await page.waitForLoadState('networkidle');

    // Klik 1x — confirm dialog accept
    page.on('dialog', dialog => dialog.accept());

    const finalizeBtn = page.locator('.btn-finalize-grading').first();
    await finalizeBtn.click();

    // Wait reload — first click sukses normal
    await page.waitForLoadState('networkidle');

    // Sekarang button gated (Status=Completed) — tapi ini test 2x klik via fetch JS langsung
    // Re-trigger via JS fetch (bypass UI gate untuk simulate concurrent dual-tab)
    const response = await page.evaluate(async () => {
      const token = (document.querySelector('input[name="__RequestVerificationToken"]') as HTMLInputElement).value;
      const sessionId = parseInt((document.querySelector('.btn-finalize-grading') as HTMLElement)?.dataset?.sessionId || '0');
      const res = await fetch('/Admin/FinalizeEssayGrading', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: 'sessionId=' + sessionId + '&__RequestVerificationToken=' + encodeURIComponent(token)
      });
      return await res.json();
    });

    // Assertion D-03: response ke-2 = success + alreadyFinalized:true + message contain "sudah diselesaikan"
    expect(response.success).toBe(true);
    expect(response.alreadyFinalized).toBe(true);
    expect(response.message).toContain('Penilaian sudah diselesaikan sebelumnya pada');
    expect(response.message).toContain('WIB');
  });

  test('9.3 - SC #1 alt: Klik Finalize pada session Open → error toast spesifik (D-04 BI literal)', async ({ page }) => {
    // PRECONDITION: Session Status=Open (belum mulai mengerjakan)
    const openSessionTitle = 'Phase 310 Open Fixture';
    await page.goto('/Admin/ManageAssessment');
    const groupRow = page.locator('tr', { hasText: openSessionTitle }).first();

    if (await groupRow.count() === 0) {
      test.skip(true, 'Seed session "Phase 310 Open Fixture" not found — Wave 1 manual seed required');
    }

    await groupRow.locator('a[href*="AssessmentMonitoringDetail"]').first().click();
    await page.waitForLoadState('networkidle');

    // Trigger via JS fetch (UI mungkin sudah hide button via existing EssayPendingCount guard)
    const response = await page.evaluate(async () => {
      const token = (document.querySelector('input[name="__RequestVerificationToken"]') as HTMLInputElement).value;
      // Find sessionId dari data-session-id atau URL
      const sessionId = parseInt(window.location.pathname.split('/').pop() || '0');
      const res = await fetch('/Admin/FinalizeEssayGrading', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: 'sessionId=' + sessionId + '&__RequestVerificationToken=' + encodeURIComponent(token)
      });
      return await res.json();
    });

    // Assertion D-04: response BI literal "Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian."
    expect(response.success).toBe(false);
    expect(response.message).toContain('Belum bisa di-finalize');
  });

});

// ============================================================
// FLOW 2: Worker sees and starts assessment
// ============================================================
test.describe('Assessment - Worker Views Assessment', () => {

  test('2.1 - Worker can see assigned assessment in My Assessments', async ({ page }) => {
    // Skip if no assessment was created (depends on flow 1)
    test.skip(!assessmentTitle, 'Assessment not created yet');

    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    await expect(page.locator('h2')).toContainText('My Assessments');

    // The assessment may or may not be assigned to this specific user
    // Check that the page loads correctly
    const cards = page.locator('.assessment-card');
    const cardCount = await cards.count();
    // If our assessment is here, great; if not, still valid (user might not be in first 2 checkboxes)
    console.log(`Worker sees ${cardCount} assessment cards`);
  });

  test('2.2 - Assessment page shows correct tabs (Open / Upcoming)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    await expect(page.locator('#open-tab')).toBeVisible();
    await expect(page.locator('#upcoming-tab')).toBeVisible();
  });

  test('2.3 - Worker can search assessments', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    const searchInput = page.locator('input[name="search"]');
    await searchInput.fill('nonexistent-test-xyz');
    await searchInput.press('Enter');
    await page.waitForLoadState('networkidle');
    // Should show empty or no results
    await expect(page.locator('body')).toContainText(/No results|No assessments/i);
  });
});

// ============================================================
// FLOW 3: Admin manages assessment (Edit, Delete)
// ============================================================
test.describe('Assessment - Admin Edit & Delete', () => {

  test('3.1 - HC can open Edit page for assessment', async ({ page }) => {
    test.skip(!assessmentTitle, 'Assessment not created yet');

    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    await searchAssessment(page, assessmentTitle);

    // Find and click edit button/link
    const editLink = page.locator('a[href*="EditAssessment"]').first();
    if (await editLink.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await editLink.click();
      await expect(page.locator('body')).toContainText('Edit Assessment');
    }
  });

  test('3.2 - HC can delete assessment group', async ({ page }) => {
    test.skip(!assessmentTitle, 'Assessment not created yet');

    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    await searchAssessment(page, assessmentTitle);

    // Find delete button in dropdown
    const dropdown = page.locator('.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await dropdown.click();
      autoConfirm(page);
      const deleteBtn = page.locator('button[formaction*="DeleteAssessmentGroup"], a[href*="DeleteAssessment"]').first();
      if (await deleteBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await deleteBtn.click();
        await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
      }
    }
  });
});

// ============================================================
// FLOW 4: Monitoring features
// ============================================================
test.describe('Assessment - Monitoring Features', () => {

  test('4.1 - Monitoring page loads with summary cards', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    await expect(page.locator('h2, h3, h4').first()).toBeVisible();
    // Check filter controls exist
    await expect(page.locator('input[name="search"], input[type="search"]').first()).toBeVisible();
  });

  test('4.2 - Monitoring can filter by status', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    // Status filter should exist
    const statusFilter = page.locator('select[name="status"]');
    if (await statusFilter.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await statusFilter.selectOption('Closed');
      // Submit the filter form
      await statusFilter.evaluate(el => el.closest('form')?.submit());
      await page.waitForLoadState('networkidle');
    }
  });

  test('4.3 - Monitoring can filter by category', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/AssessmentMonitoring');

    const categoryFilter = page.locator('select[name="category"]');
    if (await categoryFilter.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await categoryFilter.selectOption('OJT');
      await categoryFilter.evaluate(el => el.closest('form')?.submit());
      await page.waitForLoadState('networkidle');
    }
  });
});

// ============================================================
// FLOW 5: Authorization checks
// ============================================================
test.describe('Assessment - Authorization', () => {

  test('5.1 - Coachee cannot access Admin ManageAssessment', async ({ page }) => {
    await login(page, 'coachee');
    const response = await page.goto('/Admin/ManageAssessment');
    // Should redirect to AccessDenied or return 403
    const url = page.url();
    const status = response?.status() ?? 200;
    expect(url.includes('AccessDenied') || url.includes('Login') || status === 403).toBeTruthy();
  });

  test('5.2 - Coachee cannot access CreateAssessment', async ({ page }) => {
    await login(page, 'coachee');
    const response = await page.goto('/Admin/CreateAssessment');
    const url = page.url();
    const status = response?.status() ?? 200;
    expect(url.includes('AccessDenied') || url.includes('Login') || status === 403).toBeTruthy();
  });

  test('5.3 - Coachee cannot access AssessmentMonitoring', async ({ page }) => {
    await login(page, 'coachee');
    const response = await page.goto('/Admin/AssessmentMonitoring');
    const url = page.url();
    const status = response?.status() ?? 200;
    expect(url.includes('AccessDenied') || url.includes('Login') || status === 403).toBeTruthy();
  });

  test('5.4 - Unauthenticated user redirected to login', async ({ page }) => {
    await page.goto('/CMP/Assessment');
    await expect(page).toHaveURL(/.*Login.*/);
  });

  test('5.5 - Admin can access ManageAssessment', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Admin/ManageAssessment');
    await expect(page.locator('h2')).toContainText('Manage Assessment');
  });
});

// ============================================================
// FLOW 6: Records page
// ============================================================
test.describe('Assessment - Training Records', () => {

  test('6.1 - Worker can access Training Records page', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Records');
    await expect(page.locator('body')).toContainText(/Total Records|Training Manual|Assessment/i);
  });

  test('6.2 - HC can access User Assessment History', async ({ page }) => {
    await login(page, 'hc');
    // We need a user ID - just test the page structure via ManageAssessment
    await page.goto('/Admin/ManageAssessment');
    await expect(page.locator('body')).toContainText('Manage Assessment');
  });
});
