// Phase 315 — POM-flat helpers untuk assessment matrix exam flow.
// Pattern reference: tests/e2e/helpers/exam313.ts (Phase 313.1 — flat export functions,
// JSDoc docblock per function, source code citation di header).
//
// Source code citations (per PATTERNS § Pattern I):
//  - Hubs/AssessmentHub.cs:188-252     — SaveMultipleAnswer (MA save flow, comma-separated optionIds)
//  - Hubs/AssessmentHub.cs:134-182     — SaveTextAnswer (Essay save flow, 2s debounce client-side)
//  - Views/CMP/StartExam.cshtml:822-857 — MA checkbox handler client-side (SignalR invoke)
//  - Views/CMP/StartExam.cshtml:861-904 — Essay textarea handler + debounce timer
//  - Controllers/CMPController.cs:1569+ — SubmitExam form binding Dictionary<int,int>
//  - Controllers/CMPController.cs:1672-1717 — SubmitExam per-type grading loop (Essay branch skip)
//  - Controllers/AssessmentAdminController.cs:2684 — AssessmentMonitoringDetail query string params
//  - Views/Admin/AssessmentMonitoringDetail.cshtml:348-451 — Essay grading UI markup
//  - Views/Admin/AssessmentMonitoringDetail.cshtml:1327-1408 — Essay grading AJAX handlers
//
// Wave 0 verdicts (315-INVESTIGATION.md):
//  - A2 DB-PERSISTED-AUTHORITATIVE → Essay text 100% via SignalR; form value diabaikan SubmitExam.
//    Helper takeExam WAJIB tunggu saveIndicator='saved' SEBELUM submit (positive confirmation).
//  - A6 AUTO-CREATE-LAZY → UserPackageAssignment auto-create di first StartExam hit;
//    tidak perlu pre-seed.

import { Page, expect } from '@playwright/test';
import { login } from '../../helpers/auth';
import type { AccountKey } from '../../helpers/accounts';
import type { ScenarioConfig, QuestionConfig } from './matrixTypes';
import { softAssert, SkipScenarioError } from './matrixReport';

/**
 * Pilih option salah deterministic — first `allOptionIds` member yang BUKAN di `correctOptionIds`.
 * Per CONTEXT line 71: "salah option pertama dari correctOptionIds[] saja, biar reproducible."
 * Throw kalau seed config malformed (semua option ada di correctOptionIds).
 */
function findWrongOption(q: QuestionConfig): number {
  const wrong = q.allOptionIds.filter((id) => !q.correctOptionIds.includes(id));
  if (wrong.length === 0) {
    throw new Error(`Question ${q.id} tidak punya option salah — semua di correctOptionIds. Periksa seed.`);
  }
  return wrong[0];
}

/**
 * Peserta login + buka StartExam + jawab semua questions + Submit.
 *
 * Phase 316 fix:
 * - Submit click memakai `Promise.all([waitForURL, click])` race-tolerant
 *   (precedent exam313.ts:107). Eliminate "Target page, context or browser has been closed"
 *   regression dari Phase 315 smoke run 2026-05-11T06:14:36Z.
 * - `page.isClosed()` gate di awal setiap softAssert callback (MC/MA/Essay) — throw
 *   SkipScenarioError langsung saat page closed mid-loop. Cegah cascade-fail noise di report.
 * - **Plan 04 (gap closure):** waitForURL regex widen ke `(Results|ExamSummary)` — tolerant
 *   terhadap server-side incomplete-answers branch (Controllers/CMPController.cs:1630)
 *   yang redirect ke ExamSummary saat helper takeExam tidak sukses answer semua soal
 *   sebelum submit (page-closed cascade, mc selector timeout, dll). Server BERPERILAKU
 *   BENAR (D-02 smoke verified) — bug ada di helper regex yang terlalu sempit.
 *
 * Hub readiness gate via `window.assessmentHub.state === 'Connected'` mencegah Pitfall 1
 * (SignalR handshake belum selesai saat checkbox click → SaveMultipleAnswer silent skip
 * di Views/CMP/StartExam.cshtml:850 condition).
 *
 * Per question type:
 * - MC (radio): pick 1 option, wait #saveIndicatorText 'saved'.
 * - MA (checkbox): tick semua correctOptionIds, last change trigger SignalR SaveMultipleAnswer
 *   (Hubs/AssessmentHub.cs:188+). Wait #saveIndicatorText 'saved'.
 * - Essay (textarea): fill answer, wait 2.5s (>2s debounce di Views/CMP/StartExam.cshtml:870+),
 *   wait #saveIndicatorText 'saved'. Per A2 verdict, ini gate satu-satunya — SubmitExam abaikan
 *   form value Essay.
 *
 * Sabotage strategy (untuk skenario "expect fail"):
 * `options.sabotageOneAnswer=true` → question INDEX 0 sengaja jawab salah:
 *  - MC: pakai `findWrongOption` (deterministic first wrong option).
 *  - MA: kirim correctOptionIds.slice(1) (miss first correct → partial scoring).
 *  - Essay: kirim string kosong (HC grading nanti kasih skor 0).
 */
export async function takeExam(
  page: Page,
  cfg: ScenarioConfig,
  peserta: AccountKey,
  sessionId: number,
  options: { sabotageOneAnswer?: boolean } = {}
): Promise<void> {
  await login(page, peserta);

  await softAssert(
    { scenario: cfg, step: 'navigate-start-exam', severity: 'critical', page },
    async () => {
      await page.goto(`/CMP/StartExam/${sessionId}`);
      await expect(page.locator('#examForm')).toBeVisible({ timeout: 10_000 });
    },
    'StartExam page renders #examForm'
  );

  // SignalR readiness gate — Pitfall 1 (RESEARCH.md line 691-706) mitigation.
  await softAssert(
    { scenario: cfg, step: 'signalr-ready', severity: 'critical', page },
    async () => {
      await page.waitForFunction(
        () => {
          const w = window as unknown as { assessmentHub?: { state?: string } };
          return w.assessmentHub?.state === 'Connected';
        },
        undefined,
        { timeout: 10_000 }
      );
    },
    'SignalR assessmentHub connected (window.assessmentHub.state === Connected)'
  );

  for (let i = 0; i < cfg.questions.length; i++) {
    const q = cfg.questions[i];
    const isSabotaged = Boolean(options.sabotageOneAnswer) && i === 0;

    if (q.type === 'MultipleChoice') {
      const optId = isSabotaged ? findWrongOption(q) : q.correctOptionIds[0];
      await softAssert(
        { scenario: cfg, step: `mc-q${q.id}`, severity: 'major', page },
        async () => {
          // Phase 316: page-closed gate — abort cascade saat submit-exam (langkah sebelumnya)
          // sudah close context. SkipScenarioError di-rethrow oleh softAssert catch handler
          // (matrixReport.ts) tanpa record finding. Ref: 316-PATTERNS.md Pattern B (line 87-133).
          if (page.isClosed()) {
            throw new SkipScenarioError(`page closed before mc-q${q.id} step — cascade abort`);
          }
          // Radio click — change handler invoke SaveAnswer endpoint (server-side persist).
          await page.check(`input.exam-radio[data-question-id="${q.id}"][value="${optId}"]`);
          await page
            .locator(`#saveIndicatorText`)
            .filter({ hasText: /saved|tersimpan/i })
            .waitFor({ timeout: 5_000 });
        },
        `MC q${q.id} optionId=${optId} saved`
      );
    } else if (q.type === 'MultipleAnswer') {
      const targets = isSabotaged ? q.correctOptionIds.slice(1) : q.correctOptionIds;
      await softAssert(
        { scenario: cfg, step: `ma-q${q.id}`, severity: 'major', page },
        async () => {
          // Phase 316: page-closed gate (lihat MC step di atas untuk rasional).
          if (page.isClosed()) {
            throw new SkipScenarioError(`page closed before ma-q${q.id} step — cascade abort`);
          }
          // Tick semua target checkbox; last change trigger SignalR SaveMultipleAnswer
          // (Hubs/AssessmentHub.cs:188 — wipe-and-insert atomic per question).
          for (const oid of targets) {
            await page.check(`input.exam-checkbox[data-question-id="${q.id}"][value="${oid}"]`);
          }
          await page
            .locator(`#saveIndicatorText`)
            .filter({ hasText: /saved|tersimpan/i })
            .waitFor({ timeout: 5_000 });
        },
        `MA q${q.id} optionIds=[${targets.join(',')}] saved via SaveMultipleAnswer hub`
      );
    } else if (q.type === 'Essay') {
      const answer = isSabotaged ? '' : 'Jawaban essay benar dari peserta untuk Phase 315 matrix test.';
      await softAssert(
        { scenario: cfg, step: `essay-q${q.id}`, severity: 'major', page },
        async () => {
          // Phase 316: page-closed gate (lihat MC step di atas untuk rasional).
          if (page.isClosed()) {
            throw new SkipScenarioError(`page closed before essay-q${q.id} step — cascade abort`);
          }
          // Capture indicator text before fill — used to detect NEW save event (avoid stale text).
          const prevText = await page.evaluate(() => {
            const el = document.getElementById('saveIndicatorText');
            return (el?.textContent || '').trim();
          });
          await page.fill(`textarea.exam-essay[data-question-id="${q.id}"]`, answer);
          // Wait for indicator text to CHANGE to new "saved" text after 2s debounce.
          // Uses text check (not visibility) — indicator fades visually but textContent persists
          // (Views/CMP/StartExam.cshtml:560-568 — showSaveIndicator auto-fade after 2s display).
          await page.waitForFunction(
            (prev) => {
              const el = document.getElementById('saveIndicatorText');
              const text = ((el as HTMLElement)?.textContent || '').trim();
              return text !== prev && /saved|tersimpan/i.test(text);
            },
            prevText,
            { timeout: 7_500 }
          );
        },
        `Essay q${q.id} saved via SaveTextAnswer hub (debounce 2s)`
      );
    }
  }

  await softAssert(
    { scenario: cfg, step: 'submit-exam', severity: 'critical', page },
    async () => {
      // Step 1: #reviewSubmitBtn POSTs examForm to /CMP/ExamSummary/{id} (StartExam.cshtml:71).
      // Phase 316 fix: arm waitForURL BEFORE click (race-tolerant, exam313.ts precedent).
      await Promise.all([
        page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
        page.click('#reviewSubmitBtn'),
      ]);
      // Step 2: ExamSummary → klik "Kumpulkan Ujian" → /CMP/SubmitExam/{id} → Results.
      // onclick="return confirm(...)" triggers browser dialog — must arm listener before click.
      page.once('dialog', dialog => dialog.accept());
      await Promise.all([
        page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
        page.click('button[type="submit"]:has-text("Kumpulkan")'),
      ]);
    },
    'SubmitExam 2-step: StartExam→ExamSummary→Results'
  );
}

/**
 * Admin HC login + navigate ke `/Admin/AssessmentMonitoringDetail` (query string
 * title+category+scheduleDate per Controllers/AssessmentAdminController.cs:2684) →
 * isi skor per essay question → klik finalize per session.
 *
 * Selector finalized DI SINI berdasarkan markup Views/Admin/AssessmentMonitoringDetail.cshtml:
 * - Container per essay item: `.essay-grading-card` dengan `id="essay_{questionId}"` (line 374).
 * - Score input: `input.essay-score-input[data-question-id="{qId}"]` (line 399-403).
 * - Save per-question: `button.btn-save-essay-score[data-question-id="{qId}"]` (line 405-407).
 * - Finalize per-session: `button.btn-finalize-grading[data-session-id="{sessionId}"]` (line 428, 438).
 *
 * Endpoint AJAX (per Views/Admin/AssessmentMonitoringDetail.cshtml:1343 + 1383):
 * - POST /Admin/SubmitEssayScore (per session+question pair).
 * - POST /Admin/FinalizeEssayGrading (per session).
 *
 * Karena 1 skenario = 2 sibling sessions, helper grade keduanya sequential.
 * Skip total kalau `cfg.hasEssay === false`.
 */
export async function gradeEssaysAsHc(pageHc: Page, cfg: ScenarioConfig): Promise<void> {
  if (!cfg.hasEssay) return;

  await login(pageHc, 'hc');

  const baseUrl = pageHc.url() || 'http://localhost:5277';
  const url = new URL('/Admin/AssessmentMonitoringDetail', baseUrl);
  url.searchParams.set('title', cfg.title);
  url.searchParams.set('category', cfg.category);
  url.searchParams.set('scheduleDate', cfg.scheduleDate);

  await softAssert(
    { scenario: cfg, step: 'hc-navigate-monitoring-detail', severity: 'critical', page: pageHc },
    async () => {
      await pageHc.goto(url.toString());
      await expect(pageHc.locator('body')).toContainText(cfg.title, { timeout: 10_000 });
    },
    `HC navigates ke AssessmentMonitoringDetail title=${cfg.title}`
  );

  const essayQs = cfg.questions.filter((q) => q.type === 'Essay');
  const sessionIds = [cfg.sessionIdPeserta1, cfg.sessionIdPeserta2];

  // Grade per (session × essay question) lalu finalize per session.
  await softAssert(
    { scenario: cfg, step: 'hc-grade-essays', severity: 'major', page: pageHc },
    async () => {
      for (const sessionId of sessionIds) {
        // Phase 384 UIG-02: grading UI dipindah dari inline AssessmentMonitoringDetail ke
        // page per-worker /Admin/EssayGrading?sessionId=... (selector identik). Navigasi per-session.
        const egUrl = new URL('/Admin/EssayGrading', baseUrl);
        egUrl.searchParams.set('sessionId', String(sessionId));
        egUrl.searchParams.set('title', cfg.title);
        egUrl.searchParams.set('category', cfg.category);
        egUrl.searchParams.set('scheduleDate', cfg.scheduleDate);
        await pageHc.goto(egUrl.toString());
        await pageHc.waitForLoadState('networkidle', { timeout: 10_000 });

        for (const q of essayQs) {
          // Target input + button via [data-session-id] + [data-question-id] — precise, no nth().
          // Both attributes present per Views/Admin/EssayGrading.cshtml (Phase 384, selector clone).
          const scoreInput = pageHc.locator(
            `input.essay-score-input[data-question-id="${q.id}"][data-session-id="${sessionId}"]`
          );
          if (await scoreInput.count() === 0) {
            throw new Error(`Tidak ada essay-score-input untuk questionId=${q.id} session=${sessionId}`);
          }
          await scoreInput.fill(String(q.scoreValue));

          const saveBtn = pageHc.locator(
            `button.btn-save-essay-score[data-question-id="${q.id}"][data-session-id="${sessionId}"]`
          );
          await saveBtn.click();
          // Wait for badge to show "Sudah Dinilai" — confirms AJAX SubmitEssayScore success
          // (Views/Admin/AssessmentMonitoringDetail.cshtml:1354-1358).
          await pageHc
            .locator(`#badge_${sessionId}_${q.id}`)
            .filter({ hasText: /Sudah Dinilai/i })
            .waitFor({ timeout: 10_000 });
        }

        // Finalize per session — finalizeSection_ div visible when EssayPendingCount=0
        // (line 414-415 AJAX sets display:block after last SubmitEssayScore).
        // btn-finalize-grading click handler shows confirm() dialog (line 1377).
        const finalizeBtn = pageHc.locator(
          `button.btn-finalize-grading[data-session-id="${sessionId}"]`
        ).first();
        await finalizeBtn.waitFor({ state: 'visible', timeout: 10_000 });
        pageHc.once('dialog', (d) => d.accept());
        await finalizeBtn.click();
        await pageHc.waitForLoadState('networkidle', { timeout: 15_000 });
      }
    },
    `HC grades ${essayQs.length} essay questions × ${sessionIds.length} sessions + finalize`
  );
}

/**
 * Peserta navigate ke `/CMP/Results/{sessionId}` + verify score badge visible.
 * Race-tolerant per PATTERNS § Pattern H — minimal accept salah satu indicator
 * `.score-badge`, `[data-score]`, `.result-score`.
 */
export async function verifyResultPage(
  page: Page,
  cfg: ScenarioConfig,
  peserta: AccountKey
): Promise<void> {
  const sessionId = peserta === 'coachee' ? cfg.sessionIdPeserta1 : cfg.sessionIdPeserta2;
  await softAssert(
    { scenario: cfg, step: 'verify-result-page', severity: 'major', page },
    async () => {
      await page.goto(`/CMP/Results/${sessionId}`);
      // Status badge — one of 3 classes per Views/CMP/Results.cshtml:69-83.
      await expect(
        page.locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()
      ).toBeVisible({ timeout: 10_000 });
    },
    `Result page score visible untuk ${peserta} session ${sessionId}`
  );
}

export { SkipScenarioError };
