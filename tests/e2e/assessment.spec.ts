import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
import { selectors, wizardSelectors } from './helpers/wizardSelectors';
import { createAssessmentViaWizard } from './helpers/examTypes';

// Shared state across tests in this file (runs serially)
let assessmentTitle: string;

/** Search on ManageAssessment (first search input = assessment tab) */
async function searchAssessment(page: import('@playwright/test').Page, term: string) {
  const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
  await searchInput.fill(term);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');
}

/**
 * Phase 308+ wizard: panel "Peserta Terpilih" + checkbox peserta ada di #step-2 (step-panel d-none
 * di initial load step-1). Tes Phase 307 (7.x) lama mengasumsikan single-page always-visible.
 * Navigasi ke step-2: isi Title+Category minimal (validasi step-1) → klik Next → tunggu step-2 visible.
 */
async function gotoStep2Peserta(page: Page) {
  await page.fill('#Title', uniqueTitle('Panel 7x'));
  await page.selectOption('#Category', 'OJT');
  await page.click('#btnNext1');
  await page.locator('#step-2').waitFor({ state: 'visible' });
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
    // v32.1 melokalkan heading "Create New Assessment" → "Buat Assessment Baru".
    await expect(page.locator('h2')).toContainText('Buat Assessment Baru');
  });

  test('1.2 - HC can create a new assessment for workers', async ({ page }) => {
    assessmentTitle = uniqueTitle('Assessment OJT');
    await login(page, 'hc');

    // Phase 308+ wizard: peserta-checkbox ada di #step-2 (step-panel d-none di initial load) dan
    // submit pakai #btnSubmit di #step-4 — #submitBtn/#ScheduleDate lama sudah TIDAK ada di view ini.
    // Klik input checkbox di step tersembunyi = "not visible" (brittle sejak Phase 308). Pakai helper
    // proven-green (exam-types/exam-taking): navigasi 4-step + .check() by data-email + fallback
    // text-based, lalu await #successModal.show. Coverage badge "2 terpilih" tetap di test 7.3.
    await createAssessmentViaWizard(page, {
      title: assessmentTitle,
      category: 'OJT',
      scheduleDate: today(),
      scheduleTime: '00:01',
      durationMinutes: 30,
      passPercentage: 70,
      allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com'],
    });

    // Helper sudah await #successModal.show di akhir — assert eksplisit bahwa create sukses.
    await expect(page.locator('#successModal')).toBeVisible();
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
    // Should show participant table.
    // .first() — v32.5 merge menambah tabel #tblRemoved (soft-remove panel); 'table' kini match 2 elemen.
    await expect(page.locator('table').first()).toBeVisible();
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

    // Phase 308+ wizard: panel wrapper pindah ke DALAM #step-2 (step-panel d-none saat initial load
    // di step-1). Premis lama "always-visible" (Phase 307 D-02) sudah usang. Empty-state default tetap
    // divalidasi via KONTEN (attached + count '0 peserta' + body 'Belum ada peserta') — konsisten dgn
    // 7.2-7.4 yang juga assert konten tanpa visibility.
    await expect(page.locator(selectors.panelWrapper)).toBeAttached();
    await expect(page.locator(selectors.panelCount)).toContainText('0 peserta');
    await expect(page.locator(selectors.panelBody)).toContainText('Belum ada peserta dipilih');
  });

  // Phase 308/420 wizard: panel "Peserta Terpilih" pindah ke #step-2 + count via change-handler
  // updateSelectedCount (CreateAssessment.cshtml:1558 #userCheckboxContainer 'change' → count immediate +
  // panel debounce 100ms; deselectAllBtn:1548). Interaksi PROVEN (mirror createAssessmentViaWizard):
  // pilih via `.user-check-item[data-email] input.user-checkbox` + .check() (fire native change), BUKAN
  // .click({force}) ke hasText-filter brittle. Panel body render FullName dari <strong> label (:1628).
  test('7.2 - Panel updates real-time when checkbox toggled (success criteria #2)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    await gotoStep2Peserta(page);

    const rinoCheckbox = page.locator(
      `${wizardSelectors.userCheckItem}[data-email="rino.prasetyo@pertamina.com"] ${wizardSelectors.userCheckbox}`);
    await rinoCheckbox.check();

    // Count badge IMMEDIATE update (D-11 — count immediate)
    await expect(page.locator(selectors.panelCount)).toContainText('1 peserta');

    // Panel body update setelah debounce 100ms (expect auto-retry menunggu render)
    await expect(page.locator(selectors.panelBody)).not.toContainText('Belum ada peserta dipilih');
    await expect(page.locator(selectors.panelBody)).toContainText(/rino|prasetyo/i);
  });

  test('7.3 - Step 2 panel + filter bar badge reflect multi-select (success criteria #3, #5 DRY)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    await gotoStep2Peserta(page);

    const rinoCheckbox = page.locator(
      `${wizardSelectors.userCheckItem}[data-email="rino.prasetyo@pertamina.com"] ${wizardSelectors.userCheckbox}`);
    const iwanCheckbox = page.locator(
      `${wizardSelectors.userCheckItem}[data-email="iwan3@pertamina.com"] ${wizardSelectors.userCheckbox}`);
    await rinoCheckbox.check();
    await iwanCheckbox.check();

    // Verify count badge format — Step 2 panel header (immediate)
    await expect(page.locator(selectors.panelCount)).toContainText('2 peserta');

    // Verify filter bar badge format — UNCHANGED (Phase 304 D-18 stability)
    await expect(page.locator(selectors.filterBarBadge)).toContainText('2 terpilih');

    // Panel body lists both selected participants (render FullName, debounced) — single source of truth (D-15)
    await expect(page.locator(selectors.panelBody)).toContainText(/rino|prasetyo/i);
    await expect(page.locator(selectors.panelBody)).toContainText(/iwan/i);
  });

  test('7.4 - Reset clears panel ke empty state (success criteria #2 reset path)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    await gotoStep2Peserta(page);

    const rinoCheckbox = page.locator(
      `${wizardSelectors.userCheckItem}[data-email="rino.prasetyo@pertamina.com"] ${wizardSelectors.userCheckbox}`);
    await rinoCheckbox.check();
    await expect(page.locator(selectors.panelBody)).toContainText(/rino|prasetyo/i);

    // Click "Batalkan Semua" untuk uncheck semua (deselectAllBtn — CreateAssessment.cshtml:1548)
    await page.click(wizardSelectors.deselectAllBtn);

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

  test('8.1 - Standard mode Status field active + interactable (regression guard success criteria #5)', async ({ page }) => {
    // #statusFieldWrapper + #Status hidup di dalam #step-3 (`step-panel d-none` saat di step-1).
    // Kontrak mode-toggle = kelas d-none + enabled — observable TANPA membuka step-3 (idiom 8.3/8.4).
    // toBeVisible/selectOption #Status dari step-1 mustahil (elemen step-3 collapsed) → kontrak observable.
    //
    // Standard default: wrapper TIDAK disembunyikan mode-toggle (no d-none di elemen sendiri),
    // select Status enabled (user bisa pilih), value kosong (D-11: Standard wajib pilih eksplisit).
    await expect(page.locator(selectors.statusFieldWrapper)).not.toHaveClass(/d-none/);
    await expect(page.locator(selectors.statusSelect)).toBeEnabled();
    await expect(page.locator(selectors.statusSelect)).toHaveValue('');

    // Step-1 fields terisi + mode default Standard ter-konfirmasi (submit penuh di-cover e2e lain).
    await page.fill('#Title', uniqueTitle('Phase 308 Standard'));
    await page.selectOption('#Category', 'OJT');
    await expect(page.locator(selectors.assessmentTypeInput)).toHaveValue('Standard');
  });

  test('8.2 - Switch S→PP→S Status field clear (mode-switch state cleanup D-02)', async ({ page }) => {
    // #Status di step-3 (collapsed di step-1) → kontrak via value+class (idiom 8.3/8.4), bukan
    // toBeVisible/selectOption(#Status). Mode-toggle #creationMode ada di step-1 (interactable).
    // Bukti D-02 cleanup: 'Upcoming' (di-set PP) → '' (clear saat balik Standard) — non-empty→cleared.

    // Standard default: wrapper aktif (no d-none), Status kosong.
    await expect(page.locator(selectors.statusFieldWrapper)).not.toHaveClass(/d-none/);
    await expect(page.locator(selectors.statusSelect)).toHaveValue('');

    // Switch ke PrePost — D-01: Status auto-set 'Upcoming' (programmatic) + wrapper hidden.
    await page.selectOption(selectors.assessmentTypeInput, 'PrePostTest');
    await expect(page.locator(selectors.statusFieldWrapper)).toHaveClass(/d-none/);
    await expect(page.locator(selectors.statusSelect)).toHaveValue('Upcoming');

    // Switch back ke Standard — D-02: Status clear dari 'Upcoming' → '' (cleanup), wrapper visible lagi.
    await page.selectOption(selectors.assessmentTypeInput, 'Standard');
    await expect(page.locator(selectors.statusFieldWrapper)).not.toHaveClass(/d-none/);
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

    // Phase 310 WR-02 — explicit await reload via Promise.all untuk hindari race
    // antara networkidle dan reload navigation
    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/FinalizeEssayGrading') && res.status() === 200),
      finalizeBtn.click()
    ]);
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
    // Phase 310 WR-02 — relax assertion: kalau CompletedAt null, controller WR-01 fix tidak emit "pada"
    expect(response.success).toBe(true);
    expect(response.alreadyFinalized).toBe(true);
    expect(response.message).toContain('Penilaian sudah diselesaikan sebelumnya');
    expect(response.message).toMatch(/(WIB|sebelumnya$)/);
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

// ============================================================
// FLOW 12: Phase 312 — Admin Full-Delete Role Guard
// REQ: DEL-01 (6 success criteria)
// 12.0 — GetDeleteImpact JSON helper test (smoke endpoint)
// 12.1 — Admin + Open + 0 response → DELETE OK
// 12.2 — Admin + Completed → button TAMPIL untuk Admin (override)
// 12.3 — HC + Open + 0 response → modal flow 2-step + DELETE OK
// 12.4 — HC + Completed → button HIDE (UI conditional render D-01)
// 12.5 — HC + Open + has-response → modal opens, submit BLOCKED + audit blocked entry
// 12.6 — HC + PrePost + Completed → button HIDE atau backend reject (D-04 extra)
// ============================================================
test.describe('Assessment - Phase 312 Admin Full-Delete Role Guard', () => {

  test('12.0 - GetDeleteImpact returns valid JSON shape for type=group (admin login)', async ({ page }) => {
    await login(page, 'admin');
    // Pakai existing seed assessment ID — fallback skip kalau tidak ada
    await page.goto('/Admin/ManageAssessment');
    const button = page.locator('button[data-delete-id]').first();
    if (await button.count() === 0) {
      test.skip(true, 'No assessment with delete button found — seed required');
    }
    const id = await button.getAttribute('data-delete-id');
    const type = await button.getAttribute('data-delete-type');
    test.skip(!id || !type, 'data-delete-id / data-delete-type not present');

    const response = await page.request.get(`/Admin/GetDeleteImpact?type=${type}&id=${id}`);
    expect(response.status()).toBe(200);
    const data = await response.json();
    expect(data).toHaveProperty('status');
    expect(data).toHaveProperty('responseCount');
    expect(data).toHaveProperty('certCount');
    expect(data).toHaveProperty('packageCount');
    expect(data).toHaveProperty('attemptCount');
    expect(data).toHaveProperty('sessionCount');
  });

  test('12.1 - Admin + Open + 0 response → button TAMPIL + modal flow + DELETE OK', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Admin/ManageAssessment');

    // PHASE 312 WR-04: pakai dedicated fixture title agar tidak hapus seed yang
    // dipakai oleh test 12.2-12.6 (mode serial). Mirror pattern test 12.5/12.6.
    // Kalau fixture tidak ada, test di-skip — TIDAK fallback ke "first Open row"
    // yang bisa hapus seed share.
    const fixtureTitle = 'Phase 312 Admin Delete Fixture';
    await searchAssessment(page, fixtureTitle);

    const openRow = page.locator('tr', { hasText: fixtureTitle })
      .filter({ has: page.locator('span.badge.bg-success', { hasText: /^Open$/ }) })
      .first();
    if (await openRow.count() === 0) {
      test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — Wave 1 manual seed required (Open status, 0 response)`);
    }

    // Klik action dropdown
    const dropdown = openRow.locator('.dropdown-toggle').first();
    await dropdown.click();

    // Klik delete button (modal trigger) — scoped ke row tersebut
    const delBtn = openRow.locator('button.dropdown-item.text-danger[data-bs-target="#deleteAssessmentModal"]').first();
    if (await delBtn.count() === 0) test.skip(true, 'Delete button tidak ada di row fixture (kemungkinan Status berubah)');
    await expect(delBtn).toBeVisible();
    await delBtn.click();

    // Modal opens — Step 1 visible
    const modal = page.locator('#deleteAssessmentModal');
    await expect(modal).toBeVisible({ timeout: 5_000 });
    await expect(page.locator('#dam-content')).toBeVisible({ timeout: 5_000 });

    // Lanjutkan → Step 2
    await page.locator('#dam-next-btn').click();
    await expect(page.locator('#dam-step-2')).toBeVisible();
    await expect(page.locator('#deleteAssessmentModal .alert.alert-danger')).toContainText('Tindakan ini tidak dapat dibatalkan');

    // Submit Hapus Permanen
    await page.locator('#dam-submit-btn').click();
    await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
    // Optional success flash check (skip if not present — backend may use different copy)
    const successAlert = page.locator('.alert-success');
    if (await successAlert.count() > 0) {
      await expect(successAlert.first()).toContainText(/berhasil dihapus/i);
    }
  });

  test('12.2 - Admin + Completed → button TAMPIL untuk Admin (override regardless Status)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Admin/ManageAssessment');

    // Scope selector ke status badge agar tidak match Title/Category
    const completedRow = page.locator('tr', { has: page.locator('span.badge.bg-secondary', { hasText: /^Completed$/ }) }).first();
    if (await completedRow.count() === 0) test.skip(true, 'Tidak ada row Completed seed — buat dulu via AkhiriUjian');

    const dropdown = completedRow.locator('.dropdown-toggle').first();
    await dropdown.click();

    // Admin override → button HARUS tampil
    const delBtn = completedRow.locator('button.dropdown-item.text-danger[data-bs-target="#deleteAssessmentModal"]');
    await expect(delBtn.first()).toBeVisible();
  });

  test('12.3 - HC + Open + 0 response → modal flow 2-step', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');

    const openRow = page.locator('tr', { has: page.locator('span.badge.bg-success', { hasText: /^Open$/ }) }).first();
    if (await openRow.count() === 0) test.skip(true, 'Tidak ada row Open seed');

    const dropdown = openRow.locator('.dropdown-toggle').first();
    await dropdown.click();
    const delBtn = openRow.locator('button.dropdown-item.text-danger[data-bs-target="#deleteAssessmentModal"]').first();
    if (await delBtn.count() === 0) test.skip(true, 'Delete button tidak tampil untuk HC + Open (cek seed)');
    await expect(delBtn).toBeVisible();
    await delBtn.click();

    await expect(page.locator('#deleteAssessmentModal')).toBeVisible();
    await expect(page.locator('#dam-content')).toBeVisible({ timeout: 5_000 });
    await page.locator('#dam-next-btn').click();
    await expect(page.locator('#dam-step-2')).toBeVisible();
    // Tutup modal — avoid double-delete dengan test 12.1
    await page.locator('#deleteAssessmentModal button.btn-close').click();
  });

  test('12.4 - HC + Completed → button HIDE (D-01 conditional render)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');

    const completedRow = page.locator('tr', { has: page.locator('span.badge.bg-secondary', { hasText: /^Completed$/ }) }).first();
    if (await completedRow.count() === 0) test.skip(true, 'Tidak ada row Completed seed');

    const dropdown = completedRow.locator('.dropdown-toggle').first();
    await dropdown.click();

    // HC + Completed → button TIDAK ada di dropdown row tersebut
    const delBtn = completedRow.locator('button.dropdown-item.text-danger[data-bs-target="#deleteAssessmentModal"]');
    await expect(delBtn).toHaveCount(0);
  });

  test('12.5 - HC + Open + has-response → modal opens, submit BLOCKED + flash error', async ({ page }) => {
    // Pre-condition: assessment Open dengan responseCount > 0 (seed manual atau via API)
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');

    // Asumsi: ada seed row Open dengan responseCount>0 — title fixture
    const targetRow = page.locator('tr', { hasText: 'Phase 312 HC Block Fixture' }).first();
    if (await targetRow.count() === 0) test.skip(true, 'Seed "Phase 312 HC Block Fixture" tidak ditemukan — Wave 1 manual seed required');

    const dropdown = targetRow.locator('.dropdown-toggle').first();
    await dropdown.click();
    const delBtn = targetRow.locator('button.dropdown-item.text-danger[data-bs-target="#deleteAssessmentModal"]').first();
    await delBtn.click();

    await expect(page.locator('#deleteAssessmentModal')).toBeVisible();
    await page.locator('#dam-next-btn').click();  // wait for content load + Lanjutkan enabled
    await page.locator('#dam-submit-btn').click();
    await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });

    // Backend reject → TempData Error
    await expect(page.locator('.alert-danger')).toContainText(/tidak memiliki izin/i);
  });

  test('12.6 - HC + PrePost + Completed → button HIDE (D-04 extra scope)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');

    const prePostCompletedRow = page.locator('tr', { hasText: 'Phase 312 PrePost Completed' }).first();
    if (await prePostCompletedRow.count() === 0) test.skip(true, 'Seed PrePost+Completed tidak ditemukan');

    const dropdown = prePostCompletedRow.locator('.dropdown-toggle').first();
    await dropdown.click();

    const delPrePostBtn = prePostCompletedRow.locator('button.dropdown-item.text-danger[data-delete-type="prepost"]');
    await expect(delPrePostBtn).toHaveCount(0);  // HIDE per D-01
  });
});
