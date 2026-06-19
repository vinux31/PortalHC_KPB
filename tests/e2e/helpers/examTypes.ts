// Phase 317 — POM-flat helpers untuk MA/Essay/Mixed E2E via HC UI creation.
// Pattern reference: tests/e2e/helpers/examMatrix.ts (Phase 315/316 — flat export functions,
// JSDoc docblock per function, source citation di header).
//
// Source code citations:
//  - Views/Admin/CreateAssessment.cshtml:77-815 — 4-step wizard structure, field IDs, success modal
//  - Views/Admin/ManagePackageQuestions.cshtml:117-458 — right-pane form, QuestionType switch JS
//  - Views/CMP/StartExam.cshtml:200-203 — #reviewSubmitBtn form submit
//  - Views/CMP/ExamSummary.cshtml — Kumpulkan Ujian button + confirm() dialog
//
// Wave 0 assumption verifications (317-RESEARCH.md A1-A7):
//  - A4 question order persistence — verified di smoke wave-0 block
//  - A5 window.timerStartRemaining scope — verified di smoke wave-0 block

import { Page, Locator, expect } from '@playwright/test';
import { wizardSelectors, questionFormSelectors, extraTimeSelectors, prePostWizardSelectors } from './wizardSelectors';

export type QuestionInput =
  | { type: 'MultipleChoice'; text: string; options: [string, string, string, string]; correctIndex: 0 | 1 | 2 | 3; score: number }
  | { type: 'MultipleAnswer'; text: string; options: [string, string, string, string]; correctIndices: (0 | 1 | 2 | 3)[]; score: number }
  | { type: 'Essay'; text: string; rubrik: string; maxCharacters?: number; score: number };

export interface CreateAssessmentOpts {
  title: string;
  category: string;            // Value option dropdown #Category (e.g. 'OJT', 'IHT')
  scheduleDate: string;        // YYYY-MM-DD
  scheduleTime?: string;       // HH:mm, default '00:01'
  durationMinutes: number;
  passPercentage: number;
  allowAnswerReview: boolean;
  generateCertificate?: boolean;
  participantEmails: string[]; // ['rino.prasetyo@pertamina.com']
  ewcdDate?: string;
  ewcdTime?: string;
  // Phase 379 — additive (D-04: JANGAN ubah field existing). Token (Flow B) + Proton T3 (Flow E).
  isTokenRequired?: boolean;   // default false; STEP 3 check #IsTokenRequired
  accessToken?: string;        // 6-char alfanumerik; jika kosong + isTokenRequired → klik Generate
  protonTrackId?: number;      // value option #protonTrackSelect (alternatif protonTrackTahun)
  protonTrackTahun?: 'Tahun 1' | 'Tahun 2' | 'Tahun 3'; // pilih option by data-tahun (lebih robust)
}

/**
 * HC create assessment via 4-step Bootstrap wizard.
 *
 * Pattern source: Views/Admin/CreateAssessment.cshtml lines 77-815 (verified 2026-05-11).
 * Pitfalls mitigated:
 *  - P3 (RESEARCH.md): successModal pakai data-bs-backdrop="static" — caller WAJIB dismiss
 *    via `page.locator(wizardSelectors.modalManageBtn).click()` sebelum nav berikutnya.
 *  - P6: pakai `#schedDateInput` (BUKAN `#ScheduleDate`) — wizard dual-input strategy.
 *  - A7 (Open Q): selector `.user-check-item[data-email="..."]` dengan literal "@" valid
 *    di Playwright CSS attribute selector (string quoted). Fallback ke text-based kalau gagal.
 *
 * @param page HC user page (sudah login)
 * @param opts wizard fields
 */
export async function createAssessmentViaWizard(page: Page, opts: CreateAssessmentOpts): Promise<void> {
  await page.goto('/Admin/CreateAssessment');

  // STEP 1 — Title + Category + Type (default Standard)
  await page.locator(wizardSelectors.step1).waitFor({ state: 'visible' });
  await page.selectOption(wizardSelectors.category, opts.category);
  await page.fill(wizardSelectors.title, opts.title);

  // Phase 379 (Flow E) — Proton T3: pilih track saat Category='Assessment Proton'.
  // Section #protonFieldsSection di-show oleh JS saat category Proton dipilih (CreateAssessment.cshtml:947,1277). Additive.
  if (opts.protonTrackTahun || opts.protonTrackId) {
    await page.locator(wizardSelectors.protonFieldsSection).waitFor({ state: 'visible', timeout: 5_000 });
    if (opts.protonTrackTahun) {
      const opt = page.locator(`${wizardSelectors.protonTrackSelect} option[data-tahun="${opts.protonTrackTahun}"]`).first();
      const val = await opt.getAttribute('value');
      await page.selectOption(wizardSelectors.protonTrackSelect, val!);
    } else if (opts.protonTrackId) {
      await page.selectOption(wizardSelectors.protonTrackSelect, String(opts.protonTrackId));
    }
  }

  await page.locator(wizardSelectors.btnNext1).click();

  // STEP 2 — Peserta selection
  await page.locator(wizardSelectors.step2).waitFor({ state: 'visible', timeout: 5_000 });
  // Phase 379 — proton: coachee eligible di-render AJAX ke #protonUserCheckboxContainer (TANPA data-email);
  // standard di #userCheckboxContainer (punya data-email). Scope fallback ke container relevan supaya tak
  // klik item hidden di container yang lain.
  const isProtonCreate = !!(opts.protonTrackTahun || opts.protonTrackId);
  const participantContainer = isProtonCreate ? '#protonUserCheckboxContainer' : '#userCheckboxContainer';
  for (const email of opts.participantEmails) {
    const attrSelector = `${wizardSelectors.userCheckItem}[data-email="${email}"] ${wizardSelectors.userCheckbox}`;
    try {
      if (isProtonCreate) throw new Error('proton: text-based fallback (item tanpa data-email)');
      await page.locator(attrSelector).check({ timeout: 3_000 });
    } catch {
      // Fallback A7 mitigation — text-based local-part, di-scope ke container relevan
      const localPart = email.split('@')[0];
      const fallback = page.locator(`${participantContainer} ${wizardSelectors.userCheckItem}`)
        .filter({ hasText: localPart })
        .locator(wizardSelectors.userCheckbox)
        .first();
      await fallback.waitFor({ state: 'visible', timeout: 8_000 });
      await fallback.check();
    }
  }
  await page.locator(wizardSelectors.btnNext2).click();

  // STEP 3 — Schedule + Settings
  await page.locator(wizardSelectors.step3).waitFor({ state: 'visible', timeout: 5_000 });
  await page.fill(wizardSelectors.schedDateInput, opts.scheduleDate);
  await page.fill(wizardSelectors.schedTimeInput, opts.scheduleTime ?? '00:01');
  // Phase 379 — Proton Tahun 3 hide Duration + PassPercentage (CreateAssessment.cshtml:1636-1648). Fill hanya bila field visible.
  if (await page.locator(wizardSelectors.durationMinutes).isVisible().catch(() => false)) {
    await page.fill(wizardSelectors.durationMinutes, String(opts.durationMinutes));
  }
  await page.fill(wizardSelectors.ewcdDateInput, opts.ewcdDate ?? opts.scheduleDate);
  await page.fill(wizardSelectors.ewcdTimeInput, opts.ewcdTime ?? '23:59');
  await page.selectOption(wizardSelectors.status, 'Open');
  if (await page.locator(wizardSelectors.passPercentage).isVisible().catch(() => false)) {
    await page.fill(wizardSelectors.passPercentage, String(opts.passPercentage));
  }

  // AllowAnswerReview default = TRUE (per CreateAssessmentController Add() get default + asp-for binding)
  if (opts.allowAnswerReview) {
    await page.locator(wizardSelectors.allowAnswerReview).check();
  } else {
    await page.locator(wizardSelectors.allowAnswerReview).uncheck();
  }
  if (opts.generateCertificate) {
    await page.locator(wizardSelectors.generateCertificate).check();
  }

  // Phase 379 (Flow B) — Token wajib + access token. Markup current #tokenSection (CreateAssessment.cshtml:506-514). Additive.
  if (opts.isTokenRequired) {
    await page.locator(wizardSelectors.isTokenRequired).check();
    await page.locator(wizardSelectors.tokenSection).waitFor({ state: 'visible', timeout: 5_000 });
    if (opts.accessToken) {
      await page.fill(wizardSelectors.accessToken, opts.accessToken);
    } else {
      // generateToken() global onclick (CreateAssessment.cshtml:513,851) — auto-fill #AccessToken
      await page.locator('button:has-text("Generate"), button[onclick*="generateToken"]').first().click();
    }
  }

  await page.locator(wizardSelectors.btnNext3).click();

  // STEP 4 — Summary + Submit
  await page.locator(wizardSelectors.step4).waitFor({ state: 'visible', timeout: 5_000 });
  await page.locator(wizardSelectors.btnSubmit).click();

  // Success modal (static backdrop) — caller harus dismiss via modal-manage-btn click sebelum nav
  await page.locator(`${wizardSelectors.successModal}.show`).waitFor({ state: 'visible', timeout: 15_000 });
}

/**
 * HC create 1 package di ManagePackages page setelah wizard create assessment.
 *
 * Pattern source: Views/Admin/ManagePackages.cshtml lines 168-194 (verified 2026-05-11 W0.1 fail).
 * Wizard tidak auto-create package — assessment landing punya "Packages (0). No packages yet."
 * Caller WAJIB call ini sebelum addQuestionViaForm.
 *
 * Form: POST `/Admin/CreatePackage` dengan hidden `assessmentId` + visible `packageName`.
 * Server redirect kembali ke ManagePackages dengan TempData.Success → page reload + link
 * `a[href*="ManagePackageQuestions"]` muncul.
 *
 * @param page HC user page yang sudah arrive di /Admin/ManagePackages?assessmentId={id}
 * @param packageName default 'Paket A' (mengikuti naming convention placeholder Razor view)
 * @returns extracted packageId dari link ManagePackageQuestions
 */
export async function createDefaultPackage(page: Page, packageName = 'Paket A'): Promise<number> {
  await page.locator('input[name="packageName"]').fill(packageName);
  await page.locator('button[type="submit"]:has-text("Create Package")').click();
  await page.waitForLoadState('networkidle');

  // Verify alert-success + package count incremented
  await expect(page.locator('.alert-success').first()).toBeVisible({ timeout: 5_000 });

  // Extract packageId dari link `a[href*="ManagePackageQuestions"]`
  const manageQLink = page.locator('a[href*="ManagePackageQuestions"]').first();
  await expect(manageQLink).toBeVisible({ timeout: 5_000 });
  const href = await manageQLink.getAttribute('href');
  const match = href?.match(/packageId=(\d+)/);
  if (!match) {
    throw new Error(`createDefaultPackage: unable to extract packageId from href="${href}"`);
  }
  return parseInt(match[1], 10);
}

/**
 * Phase 379 (Flow D3) — import soal ke package via paste-from-Excel tab.
 *
 * Source: Views/Admin/ImportPackageQuestions.cshtml (verified 2026-06-14).
 *  - Route: GET/POST `/Admin/ImportPackageQuestions?packageId={id}`.
 *  - Paste textarea ada di TAB kedua (#paste-pane) — default tab = "Upload Excel File" (#file-pane).
 *    WAJIB klik #paste-tab dulu supaya textarea[name="pasteText"] interactable.
 *  - Format kolom CURRENT = 9 kolom TSV: Pertanyaan | Opsi A-D | Jawaban Benar | Elemen Teknis | QuestionType | Rubrik
 *    (DRIFT dari 6-kolom lama; caller bertanggung jawab format tsvRows). MC = QuestionType kosong/MultipleChoice.
 *  - Submit "Import from Paste" → redirect balik ke ManagePackages + TempData.Success.
 *
 * @param page HC user page (sudah login)
 * @param packageId target package ID
 * @param tsvRows baris TSV (\n-separated; \t antar kolom) sesuai format 9-kolom di atas
 */
export async function importQuestionsViaPaste(page: Page, packageId: number, tsvRows: string): Promise<void> {
  await page.goto(`/Admin/ImportPackageQuestions?packageId=${packageId}`);
  await page.waitForLoadState('networkidle');

  // Aktifkan tab "Paste from Excel" (default aktif = Upload Excel File)
  await page.locator('#paste-tab').click();
  const pane = page.locator('#paste-pane');
  await pane.locator('textarea[name="pasteText"]').fill(tsvRows);
  await pane.locator('button[type="submit"]').click();
  await page.waitForLoadState('networkidle');
}

/**
 * HC add 1 question via ManagePackageQuestions right-pane form.
 *
 * Pattern source: Views/Admin/ManagePackageQuestions.cshtml lines 117-458 (verified 2026-05-11).
 * Pitfall 2 mitigation: setelah `selectOption('#QuestionType', ...)`, WAJIB wait visible cue
 * spesifik tipe (maLabel/rubrikSection/optionsSection) supaya JS `applyQTypeSwitch` flip
 * radio↔checkbox selesai sebelum action berikutnya.
 *
 * Form POST → server redirect ke ManageQuestions → page reload → form reset ke MC default.
 *
 * @param page HC user page (sudah login)
 * @param packageId target package ID
 * @param q QuestionInput discriminated union
 * @param images Phase 355 (opsional) — path fixture gambar soal + tiap opsi (setInputFiles pada hidden file input)
 */
export interface QuestionImages {
  question?: string;
  questionAlt?: string;
  optionA?: string;
  optionB?: string;
  optionC?: string;
  optionD?: string;
  optionAAlt?: string;
  optionBAlt?: string;
  optionCAlt?: string;
  optionDAlt?: string;
}

export async function addQuestionViaForm(
  page: Page,
  packageId: number,
  q: QuestionInput,
  images?: QuestionImages
): Promise<void> {
  // Action route: [Route("Admin/[action]")] on AssessmentAdminController → /Admin/ManagePackageQuestions?packageId={N}
  // (RESEARCH said `/Admin/ManageQuestions` — verified salah 2026-05-11; actual action name `ManagePackageQuestions`.)
  await page.goto(`/Admin/ManagePackageQuestions?packageId=${packageId}`);
  await page.locator(questionFormSelectors.formCard).waitFor({ state: 'visible' });

  await page.selectOption(questionFormSelectors.questionType, q.type);

  // Pitfall 2 — wait JS handler applyQTypeSwitch flip selesai
  if (q.type === 'MultipleAnswer') {
    await page.locator(questionFormSelectors.maLabel).waitFor({ state: 'visible', timeout: 3_000 });
  } else if (q.type === 'Essay') {
    await page.locator(questionFormSelectors.rubrikSection).waitFor({ state: 'visible', timeout: 3_000 });
  } else {
    await page.locator(questionFormSelectors.optionsSection).waitFor({ state: 'visible', timeout: 3_000 });
    await expect(page.locator(questionFormSelectors.maLabel)).toBeHidden();
  }

  await page.fill(questionFormSelectors.questionText, q.text);

  if (q.type === 'Essay') {
    await page.fill(questionFormSelectors.rubrik, q.rubrik);
    if (q.maxCharacters) {
      await page.fill(questionFormSelectors.maxCharacters, String(q.maxCharacters));
    }
  } else {
    const optionFields = [
      questionFormSelectors.optionA,
      questionFormSelectors.optionB,
      questionFormSelectors.optionC,
      questionFormSelectors.optionD,
    ];
    const correctFields = [
      questionFormSelectors.correctA,
      questionFormSelectors.correctB,
      questionFormSelectors.correctC,
      questionFormSelectors.correctD,
    ];
    for (let i = 0; i < 4; i++) {
      await page.fill(optionFields[i], q.options[i]);
    }
    if (q.type === 'MultipleChoice') {
      await page.locator(correctFields[q.correctIndex]).check();
    } else {
      for (const idx of q.correctIndices) {
        await page.locator(correctFields[idx]).check();
      }
    }
  }

  // Phase 355 — upload gambar soal + tiap opsi via hidden file input (setInputFiles).
  // Bersyarat: hanya bila path fixture disuplai. File input hidden → setInputFiles tetap bekerja.
  if (images?.question) { await page.setInputFiles(questionFormSelectors.questionImgField, images.question); }
  if (images?.questionAlt) { await page.fill(questionFormSelectors.questionImageAlt, images.questionAlt); }
  if (images?.optionA) { await page.setInputFiles(questionFormSelectors.optAImgField, images.optionA); }
  if (images?.optionAAlt) { await page.fill(questionFormSelectors.optAImageAlt, images.optionAAlt); }
  if (images?.optionB) { await page.setInputFiles(questionFormSelectors.optBImgField, images.optionB); }
  if (images?.optionBAlt) { await page.fill(questionFormSelectors.optBImageAlt, images.optionBAlt); }
  if (images?.optionC) { await page.setInputFiles(questionFormSelectors.optCImgField, images.optionC); }
  if (images?.optionCAlt) { await page.fill(questionFormSelectors.optCImageAlt, images.optionCAlt); }
  if (images?.optionD) { await page.setInputFiles(questionFormSelectors.optDImgField, images.optionD); }
  if (images?.optionDAlt) { await page.fill(questionFormSelectors.optDImageAlt, images.optionDAlt); }

  await page.fill(questionFormSelectors.scoreValue, String(q.score));
  await page.locator(questionFormSelectors.submitBtn).click();
  await page.waitForLoadState('networkidle');

  // Verify post-submit success (Open Q4 mitigation).
  // Strict-mode fix: page mungkin punya 2 alerts simultan — global toast (b-06zfpy70xb scoped) +
  // inline TempData alert. Pakai .first() supaya tidak strict mode violation.
  await expect(page.locator('.alert-success, .alert.alert-success').first()).toBeVisible({ timeout: 5_000 });
}

/**
 * Coachee submit exam — 2-step flow (StartExam → ExamSummary → Results).
 *
 * Source: tests/e2e/helpers/examMatrix.ts:186-204 (Phase 316 SURF-316-A hardening).
 * Critical: dialog handler MUST be armed BEFORE clicking "Kumpulkan Ujian" — button onclick
 * fires browser confirm() synchronously.
 *
 * @param page coachee page at /CMP/StartExam/{id}
 */
export async function submitExamTwoStep(page: Page): Promise<void> {
  await Promise.all([
    page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
    page.click('#reviewSubmitBtn'),
  ]);
  page.once('dialog', (dialog) => dialog.accept());
  await Promise.all([
    page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
    page.click('button[type="submit"]:has-text("Kumpulkan")'),
  ]);
}

/**
 * Worker check semua MA correct options untuk 1 qcard (DOM-text matching, batch SaveMultipleAnswer).
 *
 * Pivot 2026-05-11 — Plan 317 Wave 0 A4 verdict:
 *   Controllers/CMPController.cs:1188-1196 BuildCrossPackageAssignment SHUFFLE single-package
 *   questions per-session (anti-cheat). PLUS Views/CMP/StartExam.cshtml:125-128 SHUFFLE
 *   options A/B/C/D per-question. Positional .nth() correctIndices mapping = SALAH.
 *
 * Pattern source: tests/e2e/helpers/examMatrix.ts:132-152 (Phase 315 MA flow):
 *   "Tick semua target checkbox; last change trigger SignalR SaveMultipleAnswer
 *    (Hubs/AssessmentHub.cs:188 — wipe-and-insert atomic per question)."
 *   → BATCH check, ONCE wait save indicator (BUKAN per-option wait — race fade-out).
 *
 * Strategy: scope locator ke `qCard`, find label by visible option text, check each input,
 * then wait #saveIndicatorText visible + text matches `saved|tersimpan` (auto-fade 2s harmless).
 *
 * @param page worker page (StartExam)
 * @param qCard Locator untuk specific [id^="qcard_"] yang sudah di-filter by hasText marker
 * @param optionTexts array display text option (substring match, case-sensitive)
 */
export async function checkMAOptionsForQuestion(
  page: Page,
  qCard: Locator,
  optionTexts: string[]
): Promise<void> {
  for (const optText of optionTexts) {
    await qCard
      .locator('label.list-group-item', { hasText: optText })
      .locator('input.exam-checkbox')
      .check();
  }
  // ONCE wait — last check triggers SignalR SaveMultipleAnswer batch atomic
  await page
    .locator('#saveIndicatorText')
    .filter({ hasText: /saved|tersimpan/i })
    .waitFor({ state: 'visible', timeout: 7_500 });
}

/**
 * Worker fill Essay textarea + wait saveIndicator text-change (SignalR debounce 2s).
 *
 * Pattern source: tests/e2e/helpers/examMatrix.ts:153-182 (Phase 315 Essay flow):
 *   Capture prev indicator text → fill textarea → waitForFunction text CHANGES AND matches saved.
 *   Save indicator auto-fades 2s setelah 'saved' set (Views/CMP/StartExam.cshtml:566-569) —
 *   visibility-based wait race-prone. Text-change pattern defeats fade-out.
 *
 * StartExam.cshtml:861-889 textarea 'input' event listener debounce 2 detik sebelum fire
 * SaveTextAnswer hub. Test wait minimal 3s post-fill (debounce 2s + roundtrip).
 *
 * @param page worker page (StartExam)
 * @param qCard Locator untuk specific [id^="qcard_"] yang sudah di-filter by hasText marker
 * @param answer essay text untuk fill
 */
export async function fillEssayAnswer(page: Page, qCard: Locator, answer: string): Promise<void> {
  // 999.13 (398.1): JANGAN re-implement product save via direct hub.invoke. ANDALKAN product flushEssay()
  //   yang dipicu #reviewSubmitBtn (StartExam.cshtml:1024-1045: flushEssay() clear debounce + invoke
  //   'SaveTextAnswer' pakai ta.dataset.questionId DOM yang BENAR + tunggu hasPendingSaves() SEBELUM submit).
  //   Pola PERSIS essay-flush-385.spec.ts:130-152 (PROVEN 3/3 reliable). Kenapa bukan direct invoke:
  //   direct invoke pakai qId hasil parse `qcard_{id}` yang bisa ≠ ta.dataset.questionId di ujian ter-shuffle
  //   → save ke pertanyaan salah / tak persist (akar flaky 999.13). Product flush baca DOM live → selalu benar.

  // 1) Tunggu hub Connected (flushEssay best-effort skip bila belum Connected).
  await page.waitForFunction(
    () => {
      const w = window as unknown as { assessmentHub?: { state?: string } };
      return !!w.assessmentHub && w.assessmentHub.state === 'Connected';
    },
    undefined,
    { timeout: 15_000 }
  );

  // 2) Sanity format qcard id (kontrak pemanggil).
  const qcardId = await qCard.getAttribute('id');
  if (!qcardId?.match(/qcard_(\d+)/)) {
    throw new Error(`fillEssayAnswer: qcard id "${qcardId}" tidak match pattern qcard_{qId}`);
  }

  // 3) Set value via evaluate (bukan page.fill: fill hormati maxlength + butuh visible; evaluate robust
  //    di ujian paginasi) + dispatch 'input' → product input listener arm debounce/flush path.
  const textarea = qCard.locator('textarea.exam-essay');
  await textarea.evaluate((el, v) => {
    (el as HTMLTextAreaElement).value = v as string;
    el.dispatchEvent(new Event('input', { bubbles: true }));
  }, answer);
  // 4) Guard regresi empty-fill: pastikan teks benar-benar masuk sebelum submit.
  const actual = await textarea.inputValue();
  if (actual !== answer) {
    throw new Error(`fillEssayAnswer: textarea value mismatch (len ${actual.length} vs ${answer.length})`);
  }
  // Persist dijamin product flushEssay() saat #reviewSubmitBtn diklik (submitExamTwoStep). NO direct hub.invoke.
}

/**
 * HC grade SINGLE essay session via AssessmentMonitoringDetail UI + finalize.
 *
 * Markup source: Views/Admin/AssessmentMonitoringDetail.cshtml (verified 2026-05-11):
 *  - lines 399-409 — `input.essay-score-input[data-question-id][data-session-id][max]` + sibling `button.btn-save-essay-score`
 *  - lines 1327-1372 — btn-save-essay-score click → fetch /Admin/SubmitEssayScore JSON
 *    → on success: badge#badge_{sid}_{qid} text "Sudah Dinilai" class bg-success
 *    → if data.allGraded: #finalizeSection_{sid} display block
 *  - lines 1375-1413 — btn-finalize-grading click → confirm() dialog → fetch /Admin/FinalizeEssayGrading JSON
 *    → success normal → `location.reload()` (line 1402)
 *
 * Controller signature (AssessmentAdminController.cs:2684):
 *   AssessmentMonitoringDetail(string title, string category, DateTime scheduleDate, string? assessmentType=null)
 *   → /Admin/AssessmentMonitoringDetail?title=...&category=...&scheduleDate=YYYY-MM-DD
 *
 * @param pageHc HC user page (sudah login)
 * @param opts.title assessment title (URL param)
 * @param opts.category category value (URL param)
 * @param opts.scheduleDate YYYY-MM-DD format
 * @param opts.sessionId target session ID (data-session-id filter)
 * @param opts.score score 0..maxScore untuk semua essay questions di session ini
 */
export async function gradeSingleEssaySession(
  pageHc: Page,
  opts: {
    title: string;
    category: string;
    scheduleDate: string;
    sessionId: number;
    score: number;
  }
): Promise<void> {
  // Phase 384 UIG-02: grading UI dipindah dari inline AssessmentMonitoringDetail ke
  // page per-worker /Admin/EssayGrading?sessionId=... (selector identik di page baru).
  const params = new URLSearchParams({
    sessionId: String(opts.sessionId),
    title: opts.title,
    category: opts.category,
    scheduleDate: opts.scheduleDate,
  });
  await pageHc.goto(`/Admin/EssayGrading?${params.toString()}`);
  await pageHc.waitForLoadState('networkidle');

  // Find all essay-score-input untuk session ini (scoped by data-session-id)
  const scoreInputs = pageHc.locator(`input.essay-score-input[data-session-id="${opts.sessionId}"]`);
  const inputCount = await scoreInputs.count();
  if (inputCount === 0) {
    throw new Error(
      `gradeSingleEssaySession: no essay-score-input untuk sessionId=${opts.sessionId} di ${pageHc.url()}`
    );
  }

  // Fill score + click save per essay question, wait badge update sebelum next
  for (let i = 0; i < inputCount; i++) {
    const input = scoreInputs.nth(i);
    const qId = await input.getAttribute('data-question-id');
    if (!qId) {
      throw new Error(`gradeSingleEssaySession: essay-score-input #${i} missing data-question-id`);
    }
    await input.fill(String(opts.score));
    await pageHc
      .locator(`button.btn-save-essay-score[data-session-id="${opts.sessionId}"][data-question-id="${qId}"]`)
      .click();
    // Badge update text → "Sudah Dinilai" = save persisted server-side
    await expect(
      pageHc.locator(`#badge_${opts.sessionId}_${qId}`).filter({ hasText: /sudah dinilai/i })
    ).toBeVisible({ timeout: 5_000 });
  }

  // Wait finalize button visible (allGraded triggered display:block via JS line 1359-1361)
  const finalizeBtn = pageHc.locator(
    `button.btn-finalize-grading[data-session-id="${opts.sessionId}"]:not([disabled])`
  );
  await expect(finalizeBtn).toBeVisible({ timeout: 5_000 });

  // Arm confirm() dialog handler BEFORE click (browser native confirm fires synchronously)
  pageHc.once('dialog', (dialog) => dialog.accept());
  await finalizeBtn.click();

  // Phase 384 D-09: finalize sukses → update IN-PLACE (no location.reload, URL tetap /EssayGrading).
  // networkidle resolve setelah fetch FinalizeEssayGrading settle (tanpa navigasi).
  await pageHc.waitForLoadState('networkidle');
}

/**
 * HC fire AddExtraTime modal → AJAX POST → SignalR broadcast → worker timer JS update.
 *
 * Group naming (Controllers/AssessmentAdminController.cs:5483-5527 — verified 2026-05-11):
 *   batch-{Title}|{Category}|{Date:yyyy-MM-dd} — composite key, BUKAN per-token group.
 *
 * Verify strategy (RESEARCH Pitfall 4 mitigation):
 *  1. pageHc: alert-success text /berhasil ditambahkan/i — server-side success confirmation
 *  2. pageWorker: window.timerStartRemaining increased ≥ (extraMinutes*60 - 30s margin)
 *
 * Pre-condition: pageWorker MUST be at /CMP/StartExam/{id} with SignalR Connected state.
 *
 * Open Q5: server filter status="InProgress" — pageWorker MUST be at StartExam
 * (status auto-flipped to InProgress at StartExam controller action).
 *
 * @param pageHc HC user page (sudah login, fresh context untuk avoid cookie collision)
 * @param pageWorker worker page sudah di /CMP/StartExam dengan SignalR Connected
 * @param opts assessment lookup (composite group key) + extraMinutes
 */
export async function addExtraTimeViaModal(
  pageHc: Page,
  pageWorker: Page,
  opts: {
    title: string;
    category: string;
    scheduleDate: string;
    extraMinutes: 5 | 10 | 15 | 20 | 25 | 30 | 45 | 60 | 90 | 120;
  }
): Promise<void> {
  // 1. Capture initial timerStartRemaining (verified A5 Plan 01 Wave 0 — accessible JS var)
  const initialRemaining = await pageWorker.evaluate(() => {
    return (window as unknown as { timerStartRemaining?: number }).timerStartRemaining ?? 0;
  });
  expect(initialRemaining).toBeGreaterThan(0);

  // 2. HC navigate to AssessmentMonitoringDetail via URLSearchParams (gradeSingleEssaySession pattern)
  const params = new URLSearchParams({
    title: opts.title,
    category: opts.category,
    scheduleDate: opts.scheduleDate,
  });
  await pageHc.goto(`/Admin/AssessmentMonitoringDetail?${params.toString()}`);
  await pageHc.waitForLoadState('networkidle');

  // 3. Fire modal + confirm
  await pageHc.locator(extraTimeSelectors.triggerBtn).first().click();
  await pageHc.locator(extraTimeSelectors.modal).waitFor({ state: 'visible', timeout: 10_000 });
  await pageHc.selectOption(extraTimeSelectors.select, String(opts.extraMinutes));
  await pageHc.locator(extraTimeSelectors.confirmBtn).click();

  // 4. Verify HC alert-success (server-side success)
  await expect(
    pageHc.locator('.alert-success, .alert.alert-success', { hasText: /berhasil ditambahkan/i }).first()
  ).toBeVisible({ timeout: 10_000 });

  // 5. Verify worker timer increased (SignalR broadcast received)
  const expectedDelta = opts.extraMinutes * 60 - 30; // -30s margin for elapsed time during HC action
  await pageWorker.waitForFunction(
    (args) => {
      const remaining = (window as unknown as { timerStartRemaining?: number }).timerStartRemaining ?? 0;
      return remaining > args.initial + args.expectedDelta;
    },
    { initial: initialRemaining, expectedDelta },
    { timeout: 20_000 }
  );
}

// ============================================================
// Phase 318 Plan 03 — PrePostTest wizard helper
// Source: Views/Admin/CreateAssessment.cshtml lines 195-465 + 1660-1700 (verified 2026-05-12)
// Controller: AssessmentAdminController.cs lines 1155-1279 ATOMIC 2-session create
// ============================================================

export interface CreatePrePostOpts {
  title: string;
  category: string;
  preSchedule: string;           // 'YYYY-MM-DDTHH:mm' datetime-local
  preDurationMinutes: number;
  preExamWindowCloseDate: string;
  postSchedule: string;
  postDurationMinutes: number;
  postExamWindowCloseDate: string;
  passPercentage: number;
  allowAnswerReview: boolean;
  generateCertificate?: boolean;
  participantEmails: string[];
  samePackage?: boolean;
}

/**
 * HC create PrePostTest assessment via 4-step wizard.
 * Returns { preIds, postIds } extracted dari #createdAssessmentData JSON.
 * Fallback: caller dapat pakai DB query kalau JSON parse gagal (lihat FLOW P1).
 */
export async function createPrePostAssessmentViaWizard(
  page: Page,
  opts: CreatePrePostOpts
): Promise<{ preIds: number[]; postIds: number[] }> {
  await page.goto('/Admin/CreateAssessment');

  // STEP 1 — Title + Category + Type=PrePostTest
  await page.locator(wizardSelectors.step1).waitFor({ state: 'visible' });
  await page.selectOption(wizardSelectors.category, opts.category);
  await page.fill(wizardSelectors.title, opts.title);
  await page.selectOption(wizardSelectors.assessmentType, 'PrePostTest');
  // NOTE: #ppt-jadwal-section is inside Step 3 wrapper — not visible at Step 1.
  // selectOption fires change event sync; Bootstrap collapse animation completes
  // by the time Step 3 is reached.
  await page.locator(wizardSelectors.btnNext1).click();

  // STEP 2 — Peserta
  await page.locator(wizardSelectors.step2).waitFor({ state: 'visible', timeout: 5_000 });
  for (const email of opts.participantEmails) {
    const attrSelector = `${wizardSelectors.userCheckItem}[data-email="${email}"] ${wizardSelectors.userCheckbox}`;
    try {
      await page.locator(attrSelector).check({ timeout: 3_000 });
    } catch {
      const localPart = email.split('@')[0];
      await page
        .locator(`${wizardSelectors.userCheckItem}:has-text("${localPart}") ${wizardSelectors.userCheckbox}`)
        .first()
        .check();
    }
  }
  await page.locator(wizardSelectors.btnNext2).click();

  // STEP 3 — Pre + Post discrete schedule/duration/EWCD
  await page.locator(wizardSelectors.step3).waitFor({ state: 'visible', timeout: 5_000 });
  await page.fill(prePostWizardSelectors.preSchedule, opts.preSchedule);
  await page.fill(prePostWizardSelectors.preDurationMinutes, String(opts.preDurationMinutes));
  await page.fill(prePostWizardSelectors.preExamWindowCloseDate, opts.preExamWindowCloseDate);
  await page.fill(prePostWizardSelectors.postSchedule, opts.postSchedule);
  await page.fill(prePostWizardSelectors.postDurationMinutes, String(opts.postDurationMinutes));
  await page.fill(prePostWizardSelectors.postExamWindowCloseDate, opts.postExamWindowCloseDate);

  await page.fill(wizardSelectors.passPercentage, String(opts.passPercentage));
  if (opts.allowAnswerReview) await page.locator(wizardSelectors.allowAnswerReview).check();
  if (opts.generateCertificate) await page.locator(wizardSelectors.generateCertificate).check();
  if (opts.samePackage) {
    await page
      .locator(prePostWizardSelectors.samePackageCheck)
      .check()
      .catch(() => {
        /* checkbox optional */
      });
  }

  await page.locator(wizardSelectors.btnNext3).click();

  // STEP 4 — Submit
  await page.locator(wizardSelectors.step4).waitFor({ state: 'visible', timeout: 5_000 });
  await page.locator(wizardSelectors.btnSubmit).click();

  await page.locator(wizardSelectors.successModal).waitFor({ state: 'visible', timeout: 15_000 });

  const dataRaw = await page.locator(wizardSelectors.createdAssessmentData).textContent();
  if (!dataRaw) {
    throw new Error('createPrePostAssessmentViaWizard: #createdAssessmentData empty');
  }
  const parsed = JSON.parse(dataRaw) as {
    IsPrePostTest?: boolean;
    Sessions?: Array<{ PreId: number; PostId: number; UserId: string; UserName: string; UserEmail: string }>;
  };
  if (!parsed.IsPrePostTest || !parsed.Sessions) {
    throw new Error('createPrePostAssessmentViaWizard: createdAssessmentData JSON missing PrePostTest fields');
  }
  const preIds = parsed.Sessions.map((s) => s.PreId);
  const postIds = parsed.Sessions.map((s) => s.PostId);
  return { preIds, postIds };
}

// ============================================================
// Phase 318 Plan 04 — Certificate PDF download via APIRequest
// Source: Controllers/CMPController.cs:1898-1962 (verified 2026-05-12)
// ============================================================

/**
 * Verify Certificate PDF download dari /CMP/CertificatePdf/{sessionId}.
 * APIRequest pattern: page.request.get() inherits page context cookies → auth preserved.
 *
 * Assertions:
 *  - status === 200
 *  - content-type startsWith 'application/pdf'
 *  - content-disposition match /attachment.*Sertifikat_/i
 *  - body bytes > 1024 (guard zero-byte regression CMPController.cs:2118-2122)
 *
 * Pre-condition: caller logged in; session Completed + IsPassed=true + GenerateCertificate=true.
 */
export async function verifyCertificatePdfDownload(
  page: Page,
  sessionId: number
): Promise<{ bytes: number; filename: string }> {
  const response = await page.request.get(`/CMP/CertificatePdf/${sessionId}`);

  expect(response.status(), `CertificatePdf status (sessionId=${sessionId})`).toBe(200);

  const contentType = response.headers()['content-type'] ?? '';
  expect(contentType, 'Content-Type header').toMatch(/^application\/pdf/i);

  const contentDisp = response.headers()['content-disposition'] ?? '';
  expect(contentDisp, 'Content-Disposition header').toMatch(/attachment.*Sertifikat_/i);
  const filenameMatch = contentDisp.match(/filename=(?:"([^"]+)"|([^;\s]+))/i);
  const filename = (filenameMatch?.[1] ?? filenameMatch?.[2] ?? '').trim();
  expect(filename, 'Filename extracted from Content-Disposition').toMatch(/^Sertifikat_/);

  const body = await response.body();
  expect(body.length, 'PDF body bytes').toBeGreaterThan(1024);

  return { bytes: body.length, filename };
}

// ============================================================
// Phase 319 Plan 01 — Excel download + Analytics JSON intercept helpers
// Staged in Plan 01 untuk consume di Plan 03 (FLOW V + W).
// Source: Helpers/ExcelExportHelper.cs:1-40, Controllers/CMPController.cs:2731-2813
// ============================================================

/**
 * Phase 319 — verify Excel download dari endpoint binary.
 * Pattern: adaptasi `verifyCertificatePdfDownload` (Phase 318 R4 PROVEN).
 * APIRequest cookies inherit dari page context — caller MUST be logged in.
 *
 * Assertions:
 *  1. response.status() === 200
 *  2. content-type matches Excel MIME (xlsx OR xls)
 *  3. content-disposition match /attachment.*\.xlsx?/i (filename optional pattern)
 *  4. body bytes > opts.minBytes (default 2048; lowering OK untuk empty-DB header-only)
 *
 * @param page logged-in page context
 * @param endpointPath e.g. '/AssessmentAdmin/ExportCategoriesExcel'
 * @param opts.minBytes minimum byte threshold (default 2048; can lower to 256 utk header-only)
 * @param opts.filenamePattern optional regex utk filename verify
 * @returns { bytes, filename, contentType }
 */
export async function verifyExcelDownload(
  page: Page,
  endpointPath: string,
  opts: { minBytes?: number; filenamePattern?: RegExp } = {}
): Promise<{ bytes: number; filename: string; contentType: string }> {
  const response = await page.request.get(endpointPath);

  expect(response.status(), `Excel download status (${endpointPath})`).toBe(200);

  const contentType = response.headers()['content-type'] ?? '';
  expect(contentType, `Content-Type (${endpointPath})`).toMatch(
    /application\/vnd\.(openxmlformats-officedocument\.spreadsheetml\.sheet|ms-excel)/i
  );

  const contentDisp = response.headers()['content-disposition'] ?? '';
  expect(contentDisp, `Content-Disposition (${endpointPath})`).toMatch(/attachment.*\.xlsx?/i);

  const filenameMatch = contentDisp.match(/filename=(?:"([^"]+)"|([^;\s]+))/i);
  const filename = (filenameMatch?.[1] ?? filenameMatch?.[2] ?? '').trim();
  if (opts.filenamePattern) {
    expect(filename, `Filename pattern (${endpointPath})`).toMatch(opts.filenamePattern);
  }

  const body = await response.body();
  const minBytes = opts.minBytes ?? 2048;
  expect(body.length, `Excel body bytes (min ${minBytes}, ${endpointPath})`).toBeGreaterThan(minBytes);

  return { bytes: body.length, filename, contentType };
}

/**
 * Phase 319 — Analytics endpoint JSON response shape.
 * Verified dari Controllers/CMPController.cs:2731-2813 (`Json(new { totalSessions, passRate, ... })`).
 * ASP.NET Core default JsonSerializerOptions = camelCase (anonymous object → camelCase keys).
 *
 * NOTE: Wave 0 W0.W0 di Plan 03 verify camelCase serialization (RESEARCH A4 YELLOW).
 */
export interface AnalyticsResponseShape {
  totalSessions: number;
  passRate: number;
  expiringCount: number;
  avgGainScore: number;
}

/**
 * Phase 319 — intercept JSON response dari analytics endpoint.
 * Pattern: page.waitForResponse() → action() → parse JSON → cast T.
 *
 * @param page logged-in page
 * @param action navigation/click yang trigger endpoint fire
 * @param endpointMatcher string substring (URL.includes) atau RegExp utk URL match
 * @returns parsed JSON cast ke generic T
 */
export async function interceptAnalyticsResponse<T = AnalyticsResponseShape>(
  page: Page,
  action: () => Promise<void>,
  endpointMatcher: string | RegExp
): Promise<T> {
  const responsePromise = page.waitForResponse(
    (r) => {
      if (typeof endpointMatcher === 'string') {
        return r.url().includes(endpointMatcher);
      }
      return endpointMatcher.test(r.url());
    },
    { timeout: 15_000 }
  );
  await action();
  const response = await responsePromise;
  expect(response.status(), `Analytics response status (${endpointMatcher})`).toBe(200);
  return (await response.json()) as T;
}
