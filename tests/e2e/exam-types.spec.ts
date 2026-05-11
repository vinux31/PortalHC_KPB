import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today } from '../helpers/utils';
import {
  createAssessmentViaWizard,
  createDefaultPackage,
  addQuestionViaForm,
  submitExamTwoStep,
  type QuestionInput,
} from './helpers/examTypes';
import { verifyResultPage } from './helpers/examMatrix';

// Sequential mode — per-flow describe shares state (assessmentId/packageId/sessionId) antar sub-tests.
test.describe.configure({ mode: 'serial' });

const FLOW_TIMEOUT_MS = 120_000;

test.describe('smoke wave-0 (verify RESEARCH A4 + A5 assumptions)', () => {
  let smokeTitle: string;
  let smokeAssessmentId: number;
  let smokePackageId: number;

  test('W0.1 — HC create assessment + 3 MC questions distinct text', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    smokeTitle = uniqueTitle('[317-SMOKE-W0] Order Verify');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title: smokeTitle,
      category: 'OJT',
      scheduleDate: today(),
      scheduleTime: '00:01',
      durationMinutes: 60,
      passPercentage: 70,
      allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });

    // Extract assessmentId dari success modal manage-btn href
    // Format actual (verified 2026-05-11 W0.1 fail): `/Admin/ManagePackages?assessmentId={id}` query-string
    // (RESEARCH A6 asumsi path-style `/Admin/ManagePackages/{id}` salah — markup pakai query param).
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    expect(href).toMatch(/\/Admin\/ManagePackages(?:\/|\?assessmentId=)\d+/);
    smokeAssessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);

    // Dismiss static modal via manage-btn click (Pitfall 3 mitigation) → arrive di /Admin/ManagePackages
    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');

    // Wizard tidak auto-create package (verified 2026-05-11 W0.1 fail: "Packages (0)").
    // Create default package via ManagePackages form → extract packageId dari link.
    smokePackageId = await createDefaultPackage(page);

    // Add 3 MC questions DISTINCT TEXT (A4 verifier — creation order = render order check)
    const orderQuestions: QuestionInput[] = [
      { type: 'MultipleChoice', text: 'Q-ORDER-1 (first added)', options: ['A1', 'B1', 'C1', 'D1'], correctIndex: 0, score: 33 },
      { type: 'MultipleChoice', text: 'Q-ORDER-2 (second added)', options: ['A2', 'B2', 'C2', 'D2'], correctIndex: 1, score: 33 },
      { type: 'MultipleChoice', text: 'Q-ORDER-3 (third added)', options: ['A3', 'B3', 'C3', 'D3'], correctIndex: 2, score: 34 },
    ];
    for (const q of orderQuestions) {
      await addQuestionViaForm(page, smokePackageId, q);
    }
  });

  test('W0.2 — Coachee start exam: verify A4 question order + A5 timerStartRemaining scope', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');

    // CMP/Assessment markup nest .assessment-card (outer wrapper data-status) + .assessment-card-item (inner .card)
    // Pakai outer wrapper `.assessment-card` saja + .first() supaya hindari strict mode violation.
    const card = page.locator('.assessment-card', { hasText: smokeTitle }).first();
    await expect(card).toBeVisible({ timeout: 10_000 });

    page.once('dialog', (d) => d.accept());
    await card
      .locator('a:has-text("Mulai"), a:has-text("Start"), .btn-start-standard, a:has-text("Resume")')
      .first()
      .click();
    await page.waitForURL(/\/CMP\/StartExam\/\d+/, { timeout: 15_000 });

    // SignalR readiness gate (Pitfall 1 mitigation)
    await page.waitForFunction(
      () => {
        const w = window as unknown as { assessmentHub?: { state?: string } };
        return w.assessmentHub?.state === 'Connected';
      },
      undefined,
      { timeout: 10_000 }
    );

    // === A4 VERIFICATION — REVISED 2026-05-11 ===
    // RESEARCH A4 GREEN-light SALAH. Source ground truth:
    //   Controllers/CMPController.cs BuildCrossPackageAssignment lines 1188-1196:
    //     "Single package: shuffle question order so each worker sees a unique sequence"
    //     var singlePackageIds = singlePackageQuestions.OrderBy(q => q.Order)...
    //     Shuffle(singlePackageIds, rng);  ← anti-cheat shuffle deterministic per-session
    //
    // Render order ≠ creation order. FLOW K/L MUST pakai DOM-text matching (per-qcard hasText
    // identifier), BUKAN positional `.nth(N)` correctIndices mapping.
    //
    // Wave 0 assertion: all 3 Q-ORDER-* texts present di DOM (any permutation).
    const qcards = page.locator('[id^="qcard_"]');
    await expect(qcards).toHaveCount(3);
    const textContents = await qcards.allTextContents();
    const concatenated = textContents.join('|');
    expect(concatenated).toContain('Q-ORDER-1');
    expect(concatenated).toContain('Q-ORDER-2');
    expect(concatenated).toContain('Q-ORDER-3');

    // === A5 VERIFICATION — window.timerStartRemaining accessible JS var (not IIFE closure) ===
    const timerType = await page.evaluate(
      () => typeof (window as unknown as { timerStartRemaining?: unknown }).timerStartRemaining
    );
    expect(timerType).toBe('number');

    const timerValue = await page.evaluate(
      () => (window as unknown as { timerStartRemaining?: number }).timerStartRemaining ?? 0
    );
    expect(timerValue).toBeGreaterThan(0);
    expect(timerValue).toBeLessThanOrEqual(60 * 60 + 10); // 60 menit duration + 10s margin
  });
});

test.describe('FLOW K — MA Full Cycle', () => {
  // BODY ditulis di Task 3
  test('placeholder', async () => {
    expect(true).toBe(true);
  });
});

test.describe('FLOW L — Essay Full Cycle + HC Grading', () => {
  // BODY ditulis di Task 4
  test('placeholder', async () => {
    expect(true).toBe(true);
  });
});

// Suppress unused-import warnings — these symbols dipakai di Task 3-4 bodies (placeholder skeleton).
void submitExamTwoStep;
void verifyResultPage;
