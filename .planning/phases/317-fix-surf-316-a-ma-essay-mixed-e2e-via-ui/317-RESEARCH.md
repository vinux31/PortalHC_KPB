# Phase 317: Fix SURF-316-A + MA/Essay/Mixed E2E via UI — Research

**Researched:** 2026-05-11
**Domain:** Playwright E2E test authoring untuk ASP.NET MVC + Bootstrap 5 wizard + SignalR-driven exam flow
**Confidence:** HIGH (mayoritas selector & flow ter-verifikasi langsung dari source view; AddExtraTime SignalR flow ter-verifikasi dari hub + view listener)

## Summary

Phase 317 menutup gap test untuk tipe soal (MA/Essay/Mixed), flag AllowAnswerReview, dan fitur AddExtraTime dengan menulis `tests/e2e/exam-types.spec.ts` (FLOW K/L/M/N/O) yang membangun fixture **via HC UI wizard** (bukan SQL seed seperti Phase 315 matrix). Tasks 1+2 (fix SURF-316-A + matrix smoke validation) sudah DONE — riset ini fokus Tasks 3-8.

Tiga temuan riset paling berdampak:
1. **Wizard CreateAssessment adalah 4-step Bootstrap wizard** (Phase 304 era) dengan `.step-panel` toggle via `d-none` class — bukan single-page form. Test HARUS klik `#btnNext1/2/3` untuk traversal step. Selector ID semua field sudah verified (lihat tabel di bawah).
2. **ManagePackageQuestions form adalah 1-page form right-pane** (BUKAN modal) — field names tetap (`name="questionText"`, `name="optionA..D"`, `name="correctA..D"`), tapi tipe soal ditentukan oleh `#QuestionType` dropdown yang mengubah `correct-input` radio↔checkbox via JS. POST ke `/Admin/CreateQuestion`. Setelah submit page reload + form reset.
3. **AddExtraTime via SignalR `ExtraTimeAdded` broadcast** ke group `batch-{Title}|{Category}|{Date}` — worker timer JS update wall-clock anchor real-time (no reload). Group name = batchKey composite (bukan token). Modal HC: `#extraTimeModal` → `#extraTimeSelect` → `#btnConfirmExtraTime` (onclick `addExtraTime()`).

**Primary recommendation:** Tulis `tests/e2e/exam-types.spec.ts` dengan `test.describe.configure({ mode: 'serial' })` (5 describe block FLOW K-O), pakai POM-style helper di file SAMA (atau extract ke `helpers/examTypes.ts` jika cross-flow reuse > 2). REUSE `examMatrix.ts` submit-flow pattern (Promise.all + 2-step submit + dialog handler). JANGAN duplikasi `gradeEssaysAsHc` — call langsung dari `examMatrix.ts` (sudah hardened today).

## User Constraints (from CONTEXT.md)

### Locked Decisions

1. Submit flow = 2-step: `#reviewSubmitBtn` → ExamSummary → "Kumpulkan Ujian" → Results — IMPLEMENTED di examMatrix.ts (Tasks 1).
2. AllowAnswerReview=false = same submit flow, only Results page hides per-question review section.
3. MA checkbox selector = `input.exam-checkbox[value][data-question-id]` — pakai `.nth(N)` saat optionId tidak diketahui di FLOW UI-created.
4. Essay textarea selector = `textarea.exam-essay` + `id="essay_{qId}"` + `data-question-id`.
5. MC radio selector = `input.exam-radio` + `name="radio_{qId}"` + `data-question-id`.
6. Essay score input HC grading = `input.essay-score-input[data-question-id][data-session-id][max]`.
7. Finalize button HC grading = `button.btn-finalize-grading[data-session-id]` text "Selesaikan Penilaian" + browser `confirm()` dialog handler.

### Claude's Discretion

- File organization: single spec file `exam-types.spec.ts` vs extract helper `examTypes.ts` — pilih berdasarkan reuse count (helper kalau ≥3 flows pakai pola sama).
- Wizard step traversal helper API: `createAssessmentViaWizard(page, opts)` (one-shot) vs per-step builder pattern.
- Regression smoke (Task 8): "diagnose only" bukan "fix" — jika ada FAIL di FLOW A-J, dokumentasi SURF-317-x untuk follow-up phase.
- Test order strategy: K → L → M → N → O sequential (per CONTEXT) atau independen — sequential aman untuk debugging.

### Deferred Ideas (OUT OF SCOPE)

- CI integration (deferred to QA-02 future REQ — manual run only).
- Visual regression / screenshot diff (deferred).
- Phase 318: PreTest/PostTest, ExamWindowCloseDate enforcement, Certificate PDF download.
- Phase 319: ManualAssessment, Export Excel, Analytics, CertificationManagement coverage.
- Fix regressions di FLOW A-J (Task 8 hanya diagnose, log sebagai SURF-317-x).

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| QA-02 | Test coverage untuk tipe soal MA/Essay/Mixed + AllowAnswerReview=false + AddExtraTime via UI wizard | Tabel "Wizard Selectors", "ManagePackageQuestions Selectors", "ExtraTime SignalR Flow" di bawah memberikan selector + flow yang precise. Reuse `examMatrix.ts` submit + grading helpers (Tasks 1+2 sudah hardened) — tidak ada hand-roll yang diperlukan. |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Create assessment via wizard | Frontend (Razor view + Bootstrap wizard JS) | API (`/Admin/CreateAssessment` POST) | Test entry point adalah klik step-by-step di view; backend hanya validate + redirect/modal |
| Add questions via form | Frontend (right-pane form di ManagePackageQuestions) | API (`/Admin/CreateQuestion` POST) | Form classical POST + redirect; tidak ada AJAX. Page reload = state refresh untuk add next question |
| Take exam (worker) | Browser/SignalR (StartExam.cshtml) | Hubs/AssessmentHub.cs (SaveAnswer/SaveMultipleAnswer/SaveTextAnswer) | Setiap answer change → SignalR persist; submit = form POST 2-step |
| Grade essay (HC) | Frontend (AssessmentMonitoringDetail JS) | API (`/Admin/SubmitEssayScore` + `/Admin/FinalizeEssayGrading` POST) | Per-question AJAX save + per-session finalize POST |
| Add extra time | Frontend modal (MonitoringDetail) | API + SignalR broadcast (`/Admin/AddExtraTime` → `ExtraTimeAdded` event ke batch group) | HC click modal → AJAX POST → server update DB → SignalR push ke worker group → worker JS update anchor in-place (no reload) |
| Verify Results page | Browser (Results.cshtml render) | API (CMPController.Results action with QuestionReviews ViewModel) | Server-side branch via `Model.AllowAnswerReview` — Razor `@if` toggle entire section |

## Standard Stack

### Core (existing, REUSE)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @playwright/test | ^1.58.2 | E2E test runner | Existing in `tests/package.json`. Phase 315/316 patterns. |
| typescript | ^5.9.3 | Type safety untuk test code | Existing. AccountKey, ScenarioConfig types reusable. |
| exceljs | ^4.4.0 | Excel parse (existing — bukan dipakai phase ini) | Already installed for Phase 301 export test. |

### Supporting (existing helpers — REUSE)

| Module | Path | Purpose | When to Use |
|--------|------|---------|-------------|
| `login(page, account)` | `tests/helpers/auth.ts` | Auth helper login + waitForURL Home | Setiap entry per flow — `hc`, `coachee`, `coachee2` |
| `accounts` map | `tests/helpers/accounts.ts` | Account fixtures dengan email+password+role | `hc` (meylisa), `coachee` (rino), `coachee2` (iwan3) |
| `uniqueTitle(prefix)` | `tests/helpers/utils.ts` | `${prefix} ${Date.now()}` | Title isolation per test run |
| `today()` | `tests/helpers/utils.ts` | YYYY-MM-DD | Schedule date |
| `autoConfirm(page)` | `tests/helpers/utils.ts` | `page.once('dialog', d => d.accept())` | Browser confirm() dialog (submit, finalize, delete) |
| `gradeEssaysAsHc(pageHc, cfg)` | `tests/e2e/helpers/examMatrix.ts:225` | HC navigate MonitoringDetail + grade essay + finalize | FLOW L + FLOW M (essay scoring step) — REUSE, hindari duplikasi |
| `verifyResultPage(page, cfg, peserta)` | `tests/e2e/helpers/examMatrix.ts:297` | Goto `/CMP/Results/{id}` + verify badge | Generic result page assertion |
| `SkipScenarioError` | `tests/e2e/helpers/matrixReport.ts:31` | Throw saat critical step fail | OPTIONAL — exam-types.spec.ts bisa tanpa softAssert/collector karena bukan matrix discovery |

### NEW (Phase 317)

| Module | Path | Purpose |
|--------|------|---------|
| `tests/e2e/exam-types.spec.ts` | new file | 5 FLOW (K/L/M/N/O) describe blocks |
| (optional) `tests/e2e/helpers/examTypes.ts` | new file IF ≥3 flows pakai same wizard helper | `createAssessmentViaWizard()`, `addQuestionViaForm()`, `addExtraTimeViaModal()` |

**Installation:** None — semua deps existing.

**Version verification (verified 2026-05-11 via tests/package.json):**
- `@playwright/test ^1.58.2` [VERIFIED: tests/package.json]
- `typescript ^5.9.3` [VERIFIED: tests/package.json]
- Playwright config: testDir `./e2e`, baseURL `http://localhost:5277`, `actionTimeout: 10_000`, `expect.timeout: 10_000`, `fullyParallel: false`, `retries: 0`, screenshot 'on'. [VERIFIED: tests/playwright.config.ts]

## Architecture Patterns

### System Architecture Diagram

```
[HC user]
  │
  ├─→ [Wizard CreateAssessment 4-step]
  │     step-1 (Title/Category/Type) → btnNext1
  │     step-2 (Peserta selection)   → btnNext2
  │     step-3 (Schedule/Duration/Settings/AllowAnswerReview) → btnNext3
  │     step-4 (Summary)             → btnSubmit
  │       └─POST /AssessmentAdmin/CreateAssessment
  │            └─→ #successModal (data-bs-backdrop=static) → modal-manage-btn link to ManagePackages
  │
  ├─→ [ManagePackages → select package → ManageQuestions]
  │     ManagePackageQuestions right-pane form
  │       └─POST /Admin/CreateQuestion (page reload)
  │
  ├─→ [AssessmentMonitoringDetail (HC grade essay)]
  │     essay-score-input → btn-save-essay-score → AJAX /Admin/SubmitEssayScore
  │     btn-finalize-grading → confirm() dialog → AJAX /Admin/FinalizeEssayGrading
  │
  ├─→ [AddExtraTime modal di MonitoringDetail]
  │     #btnConfirmExtraTime → AJAX /Admin/AddExtraTime
  │       └─→ SignalR Clients.Group("batch-{Title}|{Cat}|{Date}").SendAsync("ExtraTimeAdded", seconds)
  │            └─→ [Worker StartExam.cshtml line 1251 listener]
  │                  → update timerStartRemaining wall-clock anchor (no reload)
  │
[Coachee user (peserta)]
  │
  ├─→ [/CMP/Assessment list] → click "Start" or "Resume" card
  ├─→ [/CMP/StartExam/{id}]
  │     SignalR assessmentHub readiness gate (window.assessmentHub.state === 'Connected')
  │     MC: input.exam-radio click → SaveAnswer hub
  │     MA: input.exam-checkbox check (all corrects) → SaveMultipleAnswer hub (last change)
  │     Essay: textarea.exam-essay fill → 2s debounce → SaveTextAnswer hub
  │     wait #saveIndicatorText "saved|tersimpan"
  │     #reviewSubmitBtn → form POST /CMP/ExamSummary/{id}
  ├─→ [/CMP/ExamSummary/{id}]
  │     button[type=submit]:has-text("Kumpulkan") + onclick=confirm()
  │     → form POST /CMP/SubmitExam/{id} → redirect Results
  └─→ [/CMP/Results/{id}]
       Razor @if (Model.AllowAnswerReview && Model.QuestionReviews != null) → tampil
       else if (!Model.AllowAnswerReview) → div.alert-info "Tinjauan jawaban tidak tersedia"
```

### Recommended Project Structure (Phase 317 additions only)

```
tests/
├── e2e/
│   ├── exam-types.spec.ts                   # NEW — FLOW K/L/M/N/O
│   ├── helpers/
│   │   ├── examTypes.ts                     # NEW (optional) — wizard + add-question + extra-time helpers
│   │   ├── examMatrix.ts                    # EXTEND — re-export gradeEssaysAsHc, verifyResultPage
│   │   └── wizardSelectors.ts               # OPTIONAL EXTEND — add ManagePackageQuestions + AddExtraTime selectors
│   └── exam-taking.spec.ts                  # READ-ONLY — regression smoke target (Task 8)
└── helpers/                                  # shared (auth, accounts, utils — no change)
```

### Pattern 1: 4-Step Wizard Traversal

**What:** Navigate sequentially through 4 step-panels by clicking next buttons; tunggu `d-none` toggle.

**When to use:** Setiap FLOW K/L/M/N/O entry untuk create assessment.

**Example:**
```typescript
// Source: Views/Admin/CreateAssessment.cshtml lines 119-712 + wizard JS line 841+
async function createAssessmentViaWizard(page: Page, opts: {
  title: string;
  category: string;
  scheduleDate: string;       // YYYY-MM-DD
  scheduleTime?: string;      // HH:mm, default '08:00'
  durationMinutes: number;
  passPercentage: number;
  allowAnswerReview: boolean;
  generateCertificate?: boolean;
  participantEmails: string[];  // ['rino.prasetyo@pertamina.com']
  ewcdDate?: string;
  ewcdTime?: string;
}): Promise<void> {
  await page.goto('/Admin/CreateAssessment');

  // STEP 1 — Title + Category + Type
  await page.locator('#step-1').waitFor({ state: 'visible' });
  await page.selectOption('#Category', opts.category);
  await page.fill('#Title', opts.title);
  // AssessmentTypeInput default = 'Standard' (cukup), tidak perlu touch
  await page.locator('#btnNext1').click();

  // STEP 2 — Peserta selection
  await page.locator('#step-2').waitFor({ state: 'visible' });
  for (const email of opts.participantEmails) {
    // .user-check-item dengan data-email attribute (line 301)
    await page.locator(`.user-check-item[data-email="${email}"] input.user-checkbox`).check();
  }
  await page.locator('#btnNext2').click();

  // STEP 3 — Schedule + Settings (most fields)
  await page.locator('#step-3').waitFor({ state: 'visible' });
  await page.fill('#schedDateInput', opts.scheduleDate);
  await page.fill('#schedTimeInput', opts.scheduleTime ?? '08:00');
  // DurationMinutes — selector via asp-for binding (id="DurationMinutes" generated)
  await page.fill('#DurationMinutes', String(opts.durationMinutes));
  await page.fill('#ewcdDateInput', opts.ewcdDate ?? opts.scheduleDate);
  await page.fill('#ewcdTimeInput', opts.ewcdTime ?? '23:59');
  await page.selectOption('#Status', 'Open');
  await page.fill('#PassPercentage', String(opts.passPercentage));
  if (opts.allowAnswerReview) {
    await page.locator('#AllowAnswerReview').check();
  } else {
    await page.locator('#AllowAnswerReview').uncheck();
  }
  if (opts.generateCertificate) {
    await page.locator('#GenerateCertificate').check();
  }
  await page.locator('#btnNext3').click();

  // STEP 4 — Summary + Submit
  await page.locator('#step-4').waitFor({ state: 'visible' });
  await page.locator('#btnSubmit').click();

  // Success modal appears with data-bs-backdrop="static" — wait for it
  await page.locator('#successModal.show').waitFor({ state: 'visible', timeout: 15_000 });
}
```

### Pattern 2: Add Question Sequential (page-reload form)

**What:** Right-pane form POST + page reload, repeat per question.

**When to use:** Setiap FLOW yang butuh ≥1 question.

**Example:**
```typescript
// Source: Views/Admin/ManagePackageQuestions.cshtml line 122-207
type QuestionInput =
  | { type: 'MultipleChoice'; text: string; options: [string, string, string, string]; correctIndex: 0|1|2|3; score: number }
  | { type: 'MultipleAnswer'; text: string; options: [string, string, string, string]; correctIndices: (0|1|2|3)[]; score: number }
  | { type: 'Essay'; text: string; rubrik: string; maxCharacters?: number; score: number };

async function addQuestionViaForm(page: Page, packageId: number, q: QuestionInput): Promise<void> {
  await page.goto(`/Admin/ManageQuestions?packageId=${packageId}`);
  // Verify form card visible
  await page.locator('#questionFormCard').waitFor({ state: 'visible' });

  await page.selectOption('#QuestionType', q.type);
  await page.fill('#questionText', q.text);

  if (q.type === 'Essay') {
    await page.fill('#rubrik', q.rubrik);
    if (q.maxCharacters) await page.fill('#maxCharacters', String(q.maxCharacters));
  } else {
    // Wait for type-switch JS to flip radio/checkbox + show optionsSection
    const letters = ['A', 'B', 'C', 'D'] as const;
    for (let i = 0; i < 4; i++) {
      await page.fill(`#option_${letters[i]}`, q.options[i]);
    }
    if (q.type === 'MultipleChoice') {
      // radio — single correct
      await page.locator(`#correct_${letters[q.correctIndex]}`).check();
    } else {
      // checkbox (post type-switch JS) — multiple correct
      for (const idx of q.correctIndices) {
        await page.locator(`#correct_${letters[idx]}`).check();
      }
    }
  }

  await page.fill('#scoreValue', String(q.score));
  await page.locator('#submitBtn').click();
  // POST /Admin/CreateQuestion → redirect back to ManageQuestions with TempData.Success
  await page.waitForLoadState('networkidle');
  // Optional verify: alert-success or row count increment
}
```

**Critical detail:** Setelah POST CreateQuestion, server redirect kembali ke ManageQuestions yang **reset form ke MultipleChoice default** (line 452-453). Jadi panggilan kedua HARUS re-selectOption + re-fill.

### Pattern 3: ExtraTime SignalR Real-Time Update

**What:** HC click modal → AJAX → SignalR `ExtraTimeAdded` event → worker timer anchor update (no reload).

**Source:** Controllers/AssessmentAdminController.cs:5485 + Views/CMP/StartExam.cshtml:1250-1256.

```typescript
// FLOW O test flow:
// 1. Two pages: pageHc (HC user) + pageWorker (coachee user)
// 2. pageWorker.goto('/CMP/StartExam/{id}') → wait timer visible + SignalR connected
// 3. Capture initial timer remaining via page.evaluate(() => timerStartRemaining)
// 4. pageHc.goto('/Admin/AssessmentMonitoringDetail?title=...&category=...&scheduleDate=...')
// 5. pageHc.click('button[data-bs-target="#extraTimeModal"]')
// 6. pageHc.selectOption('#extraTimeSelect', '10')   // 10 menit
// 7. pageHc.click('#btnConfirmExtraTime')   // calls addExtraTime() inline JS
// 8. Verify alert-success di pageHc body (text: "berhasil ditambahkan")
// 9. SignalR broadcast → pageWorker JS line 1251 listener fires
//    → timerStartRemaining bertambah 600 detik
// 10. Verify pageWorker via page.waitForFunction(prev => window.timerStartRemaining > prev + 500, prev=initial)
```

**Critical detail:** Group name = `batch-{Title}|{Category}|{Date:yyyy-MM-dd}` — BUKAN per-session group. Jadi HC tidak perlu pilih session tertentu; broadcast otomatis ke semua peserta di batch.

### Pattern 4: AllowAnswerReview=false Negative Assertion

**Source:** Views/CMP/Results.cshtml lines 316-399.

```typescript
// FLOW N — verify Results page shows alert-info instead of review card
// FLOW K/L/M control = AllowAnswerReview=true (default in CreateAssessment line 580 default checked? CHECK)

// Razor branch:
//   @if (Model.AllowAnswerReview && Model.QuestionReviews != null) → div.card has "Tinjauan Jawaban"
//   else if (!Model.AllowAnswerReview) → div.alert.alert-info has "Tinjauan jawaban tidak tersedia"

await expect(page.locator('.alert-info', { hasText: /Tinjauan jawaban tidak tersedia/i }))
  .toBeVisible();
await expect(page.locator('.card', { hasText: /Tinjauan Jawaban/i }))
  .toHaveCount(0);
```

### Anti-Patterns to Avoid

- **Single-page CreateAssessment fill** (FLOW A pattern di `exam-taking.spec.ts:34-58`): markup sudah 4-step wizard. `#submitBtn` SAJA tidak cukup — harus traverse step-1 → btnNext1 → step-2 → btnNext2 → ... Ini juga akan jadi salah satu temuan SURF-317-x di Task 8 regression smoke.
- **`#ScheduleDate` selector** (pakai FLOW A): wizard pakai `#schedDateInput` + `#schedTimeInput` yang JS combine ke hidden `#schedHidden`. Direct fill `#ScheduleDate` tidak ada di markup current.
- **Modal-driven add question** (incorrect assumption di CONTEXT initial draft): ManagePackageQuestions BUKAN modal — form di right-pane (col-lg-5). Hapus istilah "modal" dari plan.
- **Polling timer DOM untuk verify ExtraTime**: Anchor `timerStartRemaining` adalah JS var (not DOM). Pakai `page.evaluate(() => window.timerStartRemaining)` atau `page.waitForFunction(() => window.timerStartRemaining > X)`. Tidak ada element bernilai detik raw.
- **Asumsi `assessmentId` di-return dari CreateAssessment redirect**: Server redirect ke `/Admin/CreateAssessment` lagi dengan ViewBag.CreatedAssessment JSON + success modal. `assessmentId` harus di-extract dari `#successModal #modal-manage-btn` href (`/Admin/ManagePackages/{id}`) ATAU dari ViewBag JSON di `<script id="createdAssessmentData">`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Submit exam 2-step | New submit helper | Reuse pattern dari `examMatrix.ts:186-204` (Promise.all + dialog handler) | Already hardened today; SURF-316-A fix proven |
| Essay HC grading | New grading helper | Call `gradeEssaysAsHc(pageHc, cfg)` from examMatrix.ts | Already hardened today (data-session-id targeting + dialog handler) |
| Verify Result page | New result helper | Call `verifyResultPage(page, cfg, peserta)` from examMatrix.ts | Already hardened today (3-badge race-tolerant selector) |
| Login + account fixtures | New login | Reuse `login(page, 'hc'/'coachee'/'coachee2')` | Existing, tested across all phases |
| AssessmentMonitoringDetail navigation | New URL builder | Use `URLSearchParams` pattern dari `examMatrix.ts:230-235` | Composite query (title+category+scheduleDate) sudah standardized |
| SignalR wait state | New connection wait | Reuse `page.waitForFunction(() => window.assessmentHub?.state === 'Connected')` pattern dari examMatrix.ts:94-103 | SignalR readiness gate handles Pitfall 1 (race condition) |
| Dialog auto-accept | New handler | Use `autoConfirm(page)` from utils.ts OR `page.once('dialog', d => d.accept())` inline | Consistent across codebase |

**Key insight:** Helper hardening dari Phase 316/Task 1+2 sudah cover 80% Phase 317 patterns. Hindari paralel implementasi — call existing helpers dari examMatrix.ts. Yang BARU hanya: wizard traversal, add-question form, AddExtraTime modal click — 3 helper functions, total ~120 LOC.

## Runtime State Inventory

> Phase 317 adalah pure test-authoring + bug fix; tidak ada rename/refactor produksi. State inventory minimal.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Tests create assessment + questions via UI → records persist in dev DB. Each test uses `uniqueTitle()` so no collision. | TIDAK ada cleanup otomatis (BUKAN matrix test SQL seed). Test pollute dev DB dengan rows berlabel "MA Exam {timestamp}". Acceptable per CLAUDE.md SEED_WORKFLOW jika klasifikasi temporary + local-only — but should be noted di plan + UAT step manual delete via ManageAssessment UI. |
| Live service config | None — Phase 317 hanya read existing config (no n8n, no Datadog). | None. |
| OS-registered state | None — Playwright runner tidak register OS-level task. | None. |
| Secrets/env vars | None new. Test pakai existing dev creds (accounts.ts). | None. |
| Build artifacts | New file `tests/e2e/exam-types.spec.ts` (no compile output). `tsc` checks via Playwright auto-build. | None. |

## Common Pitfalls

### Pitfall 1: SignalR Hub Race (peserta exam page)
**What goes wrong:** Worker fill answer SEBELUM `window.assessmentHub.state === 'Connected'` → SignalR invoke silent skip → answer tidak ter-persist → submit dapat 0 score.

**Why it happens:** SignalR `start()` async; markup render selesai sebelum negotiate.

**How to avoid:** Pre-fill gate (already di `examMatrix.ts:94-103`):
```typescript
await page.waitForFunction(
  () => {
    const w = window as unknown as { assessmentHub?: { state?: string } };
    return w.assessmentHub?.state === 'Connected';
  },
  undefined,
  { timeout: 10_000 }
);
```
**Warning signs:** Random "score = 0 unexpected" failures.

### Pitfall 2: ManagePackageQuestions Form Type Switch JS Race
**What goes wrong:** Test `selectOption('#QuestionType', 'MultipleAnswer')` → langsung check `#correct_A` sebelum JS line 351 `applyQTypeSwitch` selesai flip radio→checkbox → click ke radio (stale type) → state inconsistent → server reject.

**Why it happens:** JS handler `.addEventListener('change', ...)` async; Playwright next action fire sebelum DOM mutation complete.

**How to avoid:** Setelah `selectOption('#QuestionType', ...)`, tunggu visual cue spesifik tipe:
- MA: `await page.locator('#maLabel').waitFor({ state: 'visible' });`
- Essay: `await page.locator('#rubrikSection').waitFor({ state: 'visible' });`
- MC: `await page.locator('#optionsSection').waitFor({ state: 'visible' }); await expect(page.locator('#maLabel')).toBeHidden();`

**Warning signs:** "Pilih minimal 1 jawaban benar" server-side error padahal test mengisi 2 checkbox.

### Pitfall 3: Success Modal Static Backdrop Blocks Next Navigation
**What goes wrong:** `#successModal` punya `data-bs-backdrop="static"` (line 720) — click di luar modal tidak menutupnya; `goto('/Admin/ManageQuestions')` di test berikutnya bisa tertahan modal yang masih open (kalau test reuse same page).

**Why it happens:** Static backdrop = manual dismiss only.

**How to avoid:** Sebelum nav ke ManagePackages/ManageQuestions, baik:
1. Click `#modal-manage-btn` (link "Manage Packages" — line 782) — natural flow.
2. Atau dismiss via `await page.locator('#successModal .btn-close-white').click()`.
3. Atau gunakan `page` baru per test (browser context fresh).

**Warning signs:** "Timeout waiting for #questionFormCard" karena page masih di CreateAssessment dengan modal stuck.

### Pitfall 4: AddExtraTime Group Name Mismatch
**What goes wrong:** Pakai SignalR group `batch-{token}` (old API) → server broadcast ke group composite (`batch-{Title}|{Category}|{Date}`) → worker JS listener tidak fire → timer tidak update → test timeout.

**Why it happens:** ISS-08 fix di Phase 302 → group naming changed ke composite. Test legacy yang asumsi token-based group akan fail silently.

**How to avoid:** Hanya verify VIA effects:
1. HC alert-success message text (line 1437-1443) — confirms server-side success.
2. Worker JS variable `window.timerStartRemaining` increase via `page.waitForFunction`.

Tidak perlu manual subscribe ke SignalR di test code.

**Warning signs:** Worker timer "tidak bertambah" padahal HC dapat alert success.

### Pitfall 5: HC Login Same Page Conflict di FLOW O
**What goes wrong:** FLOW O butuh HC + worker SIMULTAN aktif (worker exam in-progress, HC monitor). Pakai single `page` → login HC → cookies overwrite worker session → worker page expire mid-test.

**Why it happens:** Single browser context = shared cookies.

**How to avoid:** Pakai 2 browser context (atau 2 `page` dari `browser.newContext()`):
```typescript
test('O - AddExtraTime mid-exam', async ({ browser }) => {
  const ctxWorker = await browser.newContext();
  const ctxHc = await browser.newContext();
  const pageWorker = await ctxWorker.newPage();
  const pageHc = await ctxHc.newPage();
  // ... worker starts exam, HC adds extra time
  await ctxWorker.close(); await ctxHc.close();
});
```
**Warning signs:** Worker `#examHeader` tidak visible setelah HC step karena session di-overwrite.

### Pitfall 6: Submit Form `name="ScheduleDate"` vs `id="schedDateInput"` Confusion
**What goes wrong:** Test `page.fill('#ScheduleDate', today())` — element tidak ada (markup pakai `id="schedDateInput"` + `name="ScheduleDate"` di line 369). Test gagal saat step-3.

**Why it happens:** Wizard pakai dual-input strategy (visible date+time → hidden combiner). Legacy single-form FLOW A pakai assumed `#ScheduleDate` yang tidak match current markup.

**How to avoid:** SELALU pakai ID `#schedDateInput`, `#schedTimeInput`, `#ewcdDateInput`, `#ewcdTimeInput`. Avoid `#ScheduleDate` direct.

**Warning signs:** "Element not found #ScheduleDate" — terjadi di FLOW A di regression smoke.

## Code Examples

### Wizard CreateAssessment — Full Selectors Map

```typescript
// Source: Views/Admin/CreateAssessment.cshtml verified 2026-05-11
const wizardSelectors = {
  // Step navigation
  step1: '#step-1',
  step2: '#step-2',
  step3: '#step-3',
  step4: '#step-4',
  pill1: '#pill-1', pill2: '#pill-2', pill3: '#pill-3', pill4: '#pill-4',
  btnNext1: '#btnNext1',
  btnNext2: '#btnNext2',
  btnNext3: '#btnNext3',
  btnPrev2: '#btnPrev2', btnPrev3: '#btnPrev3', btnPrev4: '#btnPrev4',
  btnSubmit: '#btnSubmit',

  // Step 1 fields
  category: '#Category',
  title: '#Title',
  assessmentType: '#assessmentTypeInput',  // dropdown Standard/PreTest/PostTest/etc

  // Step 2 fields
  userContainer: '#userCheckboxContainer',
  userCheckItem: '.user-check-item',           // dengan data-email + data-name
  userCheckbox: 'input.user-checkbox',          // [value="@user.Id"]
  selectedCountBadge: '#selectedCountBadge',    // "0 terpilih"
  selectAllBtn: '#selectAllBtn',
  deselectAllBtn: '#deselectAllBtn',

  // Step 3 fields
  schedDateInput: '#schedDateInput',
  schedTimeInput: '#schedTimeInput',
  durationMinutes: '#DurationMinutes',          // asp-for generated
  ewcdDateInput: '#ewcdDateInput',
  ewcdTimeInput: '#ewcdTimeInput',
  status: '#Status',
  passPercentage: '#PassPercentage',
  isTokenRequired: '#IsTokenRequired',
  accessToken: '#AccessToken',
  generateCertificate: '#GenerateCertificate',
  allowAnswerReview: '#AllowAnswerReview',
  validUntil: '#ValidUntil',

  // Submit modal
  successModal: '#successModal',
  modalManageBtn: '#modal-manage-btn',
  createdAssessmentData: '#createdAssessmentData',  // JSON ViewBag injected
};
```

### ManagePackageQuestions — Full Selectors Map

```typescript
// Source: Views/Admin/ManagePackageQuestions.cshtml verified 2026-05-11
const questionFormSelectors = {
  formCard: '#questionFormCard',
  formTitle: '#formTitle',
  questionForm: '#questionForm',
  editQuestionId: '#editQuestionId',
  questionType: '#QuestionType',   // select MC/MA/Essay
  questionText: '#questionText',   // textarea
  optionsSection: '#optionsSection',
  maLabel: '#maLabel',             // hint "Centang semua opsi yang benar"
  rubrikSection: '#rubrikSection',
  rubrik: '#rubrik',
  maxCharacters: '#maxCharacters',
  scoreValue: '#scoreValue',
  elemenTeknis: '#elemenTeknis',
  submitBtn: '#submitBtn',
  cancelEditBtn: '#cancelEditBtn',

  // Per-option (A..D)
  optionA: '#option_A', optionB: '#option_B', optionC: '#option_C', optionD: '#option_D',
  correctA: '#correct_A', correctB: '#correct_B', correctC: '#correct_C', correctD: '#correct_D',
};
```

### ExtraTime Modal — Full Selectors Map

```typescript
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml lines 132-143 + 1462-1495
const extraTimeSelectors = {
  triggerBtn: 'button[data-bs-target="#extraTimeModal"]',
  modal: '#extraTimeModal',
  select: '#extraTimeSelect',           // option values: 5,10,15,20,25,30,45,60,90,120
  confirmBtn: '#btnConfirmExtraTime',   // onclick=addExtraTime()
  // Effect on HC page: alert-success/alert-danger di .container-fluid
};

// Worker side (Views/CMP/StartExam.cshtml line 1251):
// window.assessmentHub.on('ExtraTimeAdded', additionalSeconds => {
//   timerStartRemaining = timerStartRemaining - elapsed + additionalSeconds;
//   timerStartWallClock = Date.now();
// });
// → Verify via: page.waitForFunction(prev => window.timerStartRemaining > prev + 500, prevValue)
```

### FLOW K Skeleton (MA full cycle)

```typescript
// tests/e2e/exam-types.spec.ts (NEW)
import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';

test.describe.configure({ mode: 'serial' });

test.describe('FLOW K — MA Full Cycle', () => {
  let title: string;
  let assessmentId: number;
  let packageId: number;
  let sessionId: number;

  test('K1 — HC creates assessment via wizard', async ({ page }) => {
    title = uniqueTitle('MA Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title, category: 'OJT',
      scheduleDate: today(), scheduleTime: '00:01',
      durationMinutes: 60, passPercentage: 70,
      allowAnswerReview: true, generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    // Extract assessmentId from modal
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/ManagePackages\/(\d+)/)![1]);
  });

  test('K2 — HC navigates ManagePackages → ManagePackageQuestions', async ({ page }) => {
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages/${assessmentId}`);
    // First package row → ManageQuestions link
    await page.locator('a[href*="ManageQuestions"]').first().click();
    await page.waitForLoadState('networkidle');
    const url = page.url();
    packageId = parseInt(url.match(/packageId=(\d+)/)![1]);
  });

  test('K3 — HC adds 2 MA questions', async ({ page }) => {
    await login(page, 'hc');
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleAnswer',
      text: 'Pilih aspek OJT yang benar (pilih ≥2)',
      options: ['On-the-Job Training', 'Online Job Test', 'Off-Job Theory', 'Practical Assessment'],
      correctIndices: [0, 3],
      score: 50,
    });
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleAnswer',
      text: 'Komponen evaluasi OJT (pilih ≥2)',
      options: ['Observation', 'Self-Assessment', 'Multiple Choice Quiz', 'Coaching Notes'],
      correctIndices: [0, 1],
      score: 50,
    });
    await expect(page.locator(`text=Daftar Soal (2`)).toBeVisible();
  });

  test('K4 — Worker takes MA exam + submits', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title });
    autoConfirm(page);
    await card.locator('.btn-start-standard, a:has-text("Resume")').first().click();
    await page.waitForURL('**/CMP/StartExam/**', { timeout: 15_000 });

    // Wait SignalR connected (Pitfall 1 mitigation)
    await page.waitForFunction(() => (window as any).assessmentHub?.state === 'Connected');

    // Extract sessionId from URL
    sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)![1]);

    // Answer Q1: tick correct checkboxes per question card (nth strategy — IDs unknown)
    const qCards = page.locator('[id^="qcard_"]');
    await expect(qCards).toHaveCount(2);

    // For each question card, find all .exam-checkbox + tick correct indices [0,3] for Q1, [0,1] for Q2
    // (assuming option order preserved from CreateQuestion submission)
    const corrects = [[0, 3], [0, 1]];
    for (let i = 0; i < 2; i++) {
      const cbs = qCards.nth(i).locator('input.exam-checkbox');
      for (const idx of corrects[i]) {
        await cbs.nth(idx).check();
      }
      // Wait saved
      await page.locator('#saveIndicatorText').filter({ hasText: /saved|tersimpan/i })
        .waitFor({ timeout: 5_000 });
    }

    // Submit 2-step
    await Promise.all([
      page.waitForURL(/\/CMP\/ExamSummary\/\d+/, { timeout: 15_000 }),
      page.click('#reviewSubmitBtn'),
    ]);
    page.once('dialog', d => d.accept());
    await Promise.all([
      page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
      page.click('button[type="submit"]:has-text("Kumpulkan")'),
    ]);
  });

  test('K5 — Worker sees score 100 on Results', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto(`/CMP/Results/${sessionId}`);
    await expect(page.locator('body')).toContainText(/100/);
    await expect(page.locator('.badge.text-bg-success').first()).toBeVisible();
  });
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single-page CreateAssessment | 4-step Bootstrap wizard | Phase 304 (2026-04-28) | FLOW A `exam-taking.spec.ts:34-58` obsolete → predicted Task 8 finding |
| `correct_option_index` value index | `correct{Letter}` radio/checkbox per option | Phase 298 (2026-04-07) | FLOW A:89 `input[name="correct_option_index"]` obsolete → Q1 fail |
| `name="question_text"` + `name="options"` array | `name="questionText"` + `name="optionA..D"` | Phase 298 | FLOW A:84-88 obsolete → add-question fail |
| SubmitExam direct from StartExam | 2-step (StartExam → ExamSummary → SubmitExam) | Pre-Phase-300, but selector confusion Phase 316 SURF-316-A | Tasks 1 fixed examMatrix.ts:186-204 |
| `gradeEssaysAsHc nth(sessionIndex)` | `[data-session-id]` selector | Phase 317 Task 2 bonus | gradeEssaysAsHc hardened — no wrong-session bug |
| `verifyResultPage span.score-badge` | `.badge.text-bg-{secondary|success|danger}` | Phase 317 Task 2 bonus | Matches Results.cshtml actual class |

**Deprecated/outdated:**
- `exam-taking.spec.ts` FLOW A `#submitBtn` single-page click — markup sudah wizard 4-step, button = `#btnSubmit`.
- `exam-taking.spec.ts` FLOW A `#ScheduleDate` direct fill — markup pakai `#schedDateInput` + `#schedTimeInput`.
- `exam-taking.spec.ts` FLOW A `input[name="correct_option_index"][value="N"]` — markup pakai `#correct_A/B/C/D` radio.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `name="DurationMinutes"` `id="DurationMinutes"` (asp-for binding generates id from PascalCase property) | Wizard Selectors | Test step-3 fill fail; mitigation: read CreateAssessment.cshtml ~line 383 confirm — likely TRUE per asp-for convention |
| A2 | `.btn-start-standard` class masih ada di assessment-card markup (FLOW A:127 used it) | FLOW K skeleton | Worker start step fail; mitigation: jika fail, ganti ke `a:has-text("Mulai")` atau resume button |
| A3 | `[id^="qcard_"]` masih jadi per-question wrapper di StartExam.cshtml (FLOW A:153 used it) | FLOW K skeleton | Cannot iterate per-question; mitigation: dapat juga pakai `.exam-question` atau grouping by `[data-question-id]` |
| A4 | Question order saat exam render = order saat CreateQuestion submission (Q1=first added, Q2=second) | FLOW K K4 corrects=[[0,3],[0,1]] | Score < 100 di K5; mitigation: simpan questionText per question + read DOM untuk identify correct mapping |
| A5 | `window.timerStartRemaining` adalah JS variable yang accessible via `page.evaluate` (bukan closure) | Pattern 3 + FLOW O | Cannot verify timer increment; mitigation: cek StartExam.cshtml apakah var di scope global atau IIFE — kalau IIFE, ganti strategi pakai #examTimer text scrape + delta calculation |
| A6 | `successModal` `id="modal-manage-btn"` href format = `/Admin/ManagePackages/{id}` | createAssessmentViaWizard + K1 | Cannot extract assessmentId; mitigation: parse JSON `#createdAssessmentData` script content |
| A7 | `data-email="@user.Email"` attribute literal contains "@" char yang valid di Playwright CSS attribute selector | Wizard Step 2 example | Selector tidak match user; mitigation: use `.user-check-item:has-text("rino.prasetyo")` text-based |

**Plan-checker action:** A4 dan A5 should be verified during Wave 0 implementation (read StartExam.cshtml + try selector) BEFORE committing test code — minimal smoke run untuk K4/O steps.

## Open Questions

1. **Question order persistence**
   - What we know: ManagePackageQuestions list shows `@q.Order` column; CreateQuestion likely sets `Order = packageQuestions.Count + 1`.
   - What's unclear: Apakah StartExam respect `Order` ASC atau random shuffle (untuk anti-cheat).
   - Recommendation: Read `CMPController.StartExam` ViewModel build (~line 1100-1200) → confirm `OrderBy(q => q.Order)` exists. Jika random, A4 strategi DOM-read mandatory.

2. **CreateAssessment default AllowAnswerReview state**
   - What we know: Server default `AllowAnswerReview = true` di Add() get action (line 661); checkbox markup line 580 tidak menampilkan `checked` literal.
   - What's unclear: Apakah Razor binding `asp-for="AllowAnswerReview"` auto-renders checked={server default}.
   - Recommendation: Default = TRUE (per asp-for + server default). FLOW K/L/M leave alone. FLOW N explicit `.uncheck()`.

3. **Wizard validation in step traversal**
   - What we know: `btnNext1/2/3` likely have client-side validation (required field check) before allowing next step.
   - What's unclear: Apakah validation block click langsung atau show invalid-feedback + stay step.
   - Recommendation: Setelah click btnNextN, `await page.locator('#step-(N+1)').waitFor({ state: 'visible', timeout: 5_000 })` — kalau timeout, fail loud (sign of missing required field di plan).

4. **K3 add-question form reset across calls**
   - What we know: `resetForm()` called on cancel; setelah submit success → server redirect ke ManageQuestions → page reload → form re-rendered fresh (default MC).
   - What's unclear: Apakah ada flash error setelah POST CreateQuestion yang prevent next add.
   - Recommendation: Setelah `submitBtn.click() + waitForLoadState`, assert `.alert-success` visible atau row count incremented sebelum lanjut next question.

5. **FLOW O 2-context strategy timing**
   - What we know: pageWorker harus stay in `InProgress` status (StartExam page) saat HC fire AddExtraTime.
   - What's unclear: Apakah server `AddExtraTime` filter `status == "InProgress"` (line 5508) match session yang baru saja start exam (status set ke InProgress di StartExam controller action).
   - Recommendation: Confirm StartExam controller set `session.Status = "InProgress"` SEBELUM render view. Pakai 1-2s buffer setelah worker waitForURL StartExam sebelum HC fire modal.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Playwright runtime | All E2E tests | ✓ | 1.58.2 | — |
| Node.js | Playwright | Assumed ✓ | (existing tests run) | — |
| Local dev server (`dotnet run`) port 5277 | All E2E tests baseURL | Assumed ✓ when developer runs | — | Manual start before test run |
| Dev DB (LocalDB SQL Server) | Assessment seed + fixture rows | Assumed ✓ | — | — |
| Test accounts (admin/hc/coachee/coachee2) | Login flow | ✓ — verified via `tests/helpers/accounts.ts` + dev DB | — | — |
| SignalR hub `/assessmentHub` | Worker exam answer save + ExtraTime broadcast | ✓ — Hubs/AssessmentHub.cs registered | — | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — all infrastructure exists.

## Validation Architecture

> Note: nyquist_validation tidak explicit di .planning/config.json — treat as enabled per default.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | @playwright/test 1.58.2 |
| Config file | `tests/playwright.config.ts` (testDir: ./e2e, baseURL: http://localhost:5277, fullyParallel: false, retries: 0) |
| Quick run command | `cd tests && npx playwright test exam-types.spec.ts --grep "FLOW K"` |
| Full suite command | `cd tests && npx playwright test exam-types.spec.ts` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QA-02 (MA cycle) | HC creates → 2 MA questions → worker takes → submits → score=100 | e2e (5 sub-tests K1-K5) | `npx playwright test exam-types.spec.ts -g "FLOW K"` | ❌ Wave 0 |
| QA-02 (Essay cycle) | HC creates → 1 Essay → worker submits → PendingGrading → HC grades → finalize → score=80 | e2e (6 sub-tests L1-L6) | `npx playwright test exam-types.spec.ts -g "FLOW L"` | ❌ Wave 0 |
| QA-02 (Mixed) | MC+MA+Essay combined → submit → HC grades essay → total score = 100 | e2e (5 sub-tests M1-M5) | `npx playwright test exam-types.spec.ts -g "FLOW M"` | ❌ Wave 0 |
| QA-02 (AllowAnswerReview=false) | Results page tampil alert-info "Tinjauan jawaban tidak tersedia", no review card | e2e (3 sub-tests N1-N3) | `npx playwright test exam-types.spec.ts -g "FLOW N"` | ❌ Wave 0 |
| QA-02 (AddExtraTime) | HC fire modal → worker timer remaining ↑ tanpa reload | e2e (5 sub-tests O1-O5, 2-context) | `npx playwright test exam-types.spec.ts -g "FLOW O"` | ❌ Wave 0 |
| Regression smoke (Task 8) | FLOW A-J di exam-taking.spec.ts catat pass rate (≥ baseline) | e2e diagnose-only | `npx playwright test exam-taking.spec.ts --reporter=list` | ✓ Existing (read-only) |

### Sampling Rate

- **Per task commit:** Quick run FLOW being implemented (e.g., `--grep "FLOW K"`).
- **Per wave merge:** Full `exam-types.spec.ts` (5 flows = ~24 sub-tests).
- **Phase gate:** Full `exam-types.spec.ts` green + `exam-taking.spec.ts` pass rate ≥ baseline (Task 8).

### Wave 0 Gaps

- [ ] `tests/e2e/exam-types.spec.ts` — covers QA-02 (5 flows). DOES NOT EXIST.
- [ ] (Optional) `tests/e2e/helpers/examTypes.ts` — extract jika ≥3 flows pakai same wizard/add-question helper. Recommend YES (reusable across K/L/M/N/O).
- [ ] (Optional) `tests/e2e/helpers/wizardSelectors.ts` extend — tambah const map untuk ManagePackageQuestions + ExtraTime (lihat tabel Selectors di atas). Recommend YES (single source of truth).
- [ ] Manual UAT checklist `317-UAT.md` — 5 langkah verify masing-masing FLOW K-O end-to-end di local browser headed mode (acceptable kalau full automated pass).
- [ ] Test framework install: NONE — Playwright + deps sudah installed.

## Security Domain

> Phase 317 adalah pure test authoring — tidak ada perubahan kode produksi (Tasks 1+2 sudah done, di luar scope research ini). Security domain mostly N/A; test code akses dev DB via existing creds.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Test pakai existing login; tidak ada auth code change |
| V3 Session Management | no | N/A |
| V4 Access Control | no | N/A (HC role gating sudah di-cover di Phase 312, bukan Phase 317) |
| V5 Input Validation | partial — test triggers server validation paths | Server-side controls existing (ModelState, asp-validation) |
| V6 Cryptography | no | N/A |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Test data leak ke dev DB (uniqueTitle) | Information Disclosure (minor) | Pakai prefix `[E2E-317]` atau `MA Exam {timestamp}` agar mudah identify + cleanup manual |
| Test credentials di repo | Information Disclosure | Existing — accounts.ts dev creds documented di MEMORY.md sebagai dev-only |
| Concurrent test runs interfere | Tampering | `fullyParallel: false` + `test.describe.configure({ mode: 'serial' })` + `uniqueTitle()` per test |

## Project Constraints (from CLAUDE.md)

Direktive applicable untuk Phase 317:

1. **Bahasa Indonesia untuk respons** — apply ke RESEARCH/PLAN/UAT docs. Code TypeScript pakai English identifier (consistent dengan codebase).
2. **DEV_WORKFLOW.md SOP**: Test dijalankan **lokal** (port 5277). JANGAN run otomatis ke server Dev 10.55.3.3 atau Prod. Verifikasi lokal via `dotnet build && dotnet run` + `npx playwright test exam-types.spec.ts`.
3. **Seed Data Workflow (lokal)**: Test FLOW K-O create rows di dev DB. Klasifikasi: **temporary + local-only**. Sebelum full run, snapshot DB (`sqlcmd BACKUP DATABASE`). Setelah selesai, restore — atau tandai di `docs/SEED_JOURNAL.md` sebagai temporary cleanup-needed. Hindari membiarkan rows berakumulasi antar-session.
4. **Tidak edit kode/DB di server**: All test artifacts dan code change stay di lokal sampai commit + push.
5. **Commit format**: Sertakan migration file flag jika ada (Phase 317 tidak ada migration — tests-only + helper code).
6. **Promosi ke Dev/Prod = Team IT**: Phase 317 cuma update test code (tidak akan di-deploy ke server). Notif IT N/A.

## Sources

### Primary (HIGH confidence)

- `Views/Admin/CreateAssessment.cshtml` lines 77-815 — wizard step structure, field IDs, success modal markup, ViewBag CreatedAssessment script — read 2026-05-11.
- `Views/Admin/ManagePackageQuestions.cshtml` lines 117-458 — right-pane form, QuestionType switch JS, option A-D map — read 2026-05-11.
- `Views/Admin/AssessmentMonitoringDetail.cshtml` lines 122-143, 348-451, 1327-1495 — AddExtraTime button, modal, JS handler, essay grading UI — read 2026-05-11.
- `Views/CMP/StartExam.cshtml` lines 1240-1268 — SignalR ExtraTimeAdded listener, timer anchor update — read 2026-05-11.
- `Views/CMP/Results.cshtml` lines 316-399 — AllowAnswerReview branching (review card vs alert-info) — read 2026-05-11.
- `Controllers/AssessmentAdminController.cs` lines 5483-5527 — AddExtraTime endpoint signature, group naming, SignalR broadcast — read 2026-05-11.
- `Hubs/AssessmentHub.cs` lines 200-220 — ExtraTimeMinutes timer validation (T-298-08 reference) — read 2026-05-11.
- `tests/e2e/helpers/examMatrix.ts` lines 1-317 — submit flow 2-step, gradeEssaysAsHc, verifyResultPage helpers — read 2026-05-11.
- `tests/playwright.config.ts` — Playwright config baseURL + timeout — read 2026-05-11.
- `tests/package.json` — dependency versions — read 2026-05-11.
- `tests/helpers/accounts.ts` — account map — read 2026-05-11.
- `tests/helpers/auth.ts` — login helper — read 2026-05-11.
- `tests/helpers/utils.ts` — uniqueTitle, today, autoConfirm — read 2026-05-11.

### Secondary (MEDIUM confidence)

- `tests/e2e/exam-taking.spec.ts` lines 1-289 — FLOW A pattern reference (legacy, partially obsolete) — read 2026-05-11. Confidence MEDIUM karena banyak selector legacy; used as comparison only.
- `~/.claude/plans/phase-315-dan-316-streamed-llama.md` lines 149-330 — Phase 317 task draft + selector tentative — read via initial CONTEXT.

### Tertiary (LOW confidence — needs validation in Wave 0)

- StartExam.cshtml question card structure (`#qcard_{id}` + `.exam-question`) — assumed per FLOW A:153 but not directly re-read in this research session; verify before FLOW K K4 implementation.
- Worker JS variable scope (`window.timerStartRemaining` vs IIFE closure) — assumption A5; needs page.evaluate smoke check.

## Metadata

**Confidence breakdown:**

- Wizard selectors: HIGH — directly read CreateAssessment.cshtml step panels + button IDs.
- ManagePackageQuestions form: HIGH — directly read markup + JS switch handler.
- AddExtraTime SignalR flow: HIGH — read both server endpoint + worker listener; group naming verified.
- AllowAnswerReview branching: HIGH — read Results.cshtml Razor `@if` block.
- StartExam exam-taking selectors: MEDIUM — most verified via examMatrix.ts existing usage; question card iteration assumes FLOW A pattern (not re-verified in markup this session).
- Question order persistence at exam render: LOW — open question Q1; needs CMPController.StartExam read.

**Research date:** 2026-05-11
**Valid until:** 2026-06-11 (30 days — stable codebase, no upcoming Phase 318+319 changes to wizard/exam markup planned)
