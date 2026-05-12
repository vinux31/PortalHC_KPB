# Phase 318: PreTest/PostTest + EWCD + Certificate PDF E2E + SURF-317 carryover — Research

**Researched:** 2026-05-12
**Domain:** Playwright E2E coverage advanced exam features (PrePostTest paired sessions, ExamWindowCloseDate enforcement, Certificate PDF download, AllowAnswerReview comparison) + 2 production-grade fixes (SURF-317-A controller + SURF-317-A1 legacy test selector)
**Confidence:** HIGH (semua selector + endpoint signature + DB schema verified langsung dari source 2026-05-12; reuse 100% pattern dari Phase 317)

---

## Summary

Phase 318 = QA-08 coverage extension + 2 carryover fix dari Phase 317. Test code append ke `tests/e2e/exam-types.spec.ts` (27 → ~46 sub-tests) + helper extension `examTypes.ts` (8 → ~10 exports) + 1 selector const baru di `wizardSelectors.ts`. Production fix `Controllers/CMPController.cs:2190` (1-line + Razor loop adjust) + Razor view `Views/CMP/Results.cshtml:316-399` minor adjustment. Test fix `tests/e2e/exam-taking.spec.ts:40` (1-line selector swap, ~3 LOC delta).

Tiga temuan riset paling berdampak:

1. **PrePostTest pairing = 2 sessions per peserta dibuat ATOMIC di POST /Admin/CreateAssessment** (`Controllers/AssessmentAdminController.cs:1155-1279`). Pre + Post session sama-sama `Status="Upcoming"` di-create dalam single transaction; cross-link via `LinkedSessionId` field (Pre→Post + Post→Pre) + `LinkedGroupId` (shared = Pre[0].Id). Wizard pakai SEPARATE schedule/duration/EWCD inputs untuk Pre dan Post (`PreSchedule`, `PostSchedule`, `PreExamWindowCloseDate`, `PostExamWindowCloseDate` — `Views/Admin/CreateAssessment.cshtml:411-465`). Trigger via `AssessmentTypeInput = "PrePostTest"`. **BOTH sessions live di DB sejak create — TIDAK ada lazy/on-demand PostTest spawn**, jadi test FLOW P bisa langsung query 2 rows post-wizard.

2. **EWCD enforcement = single guard di `CMPController.StartExam` line 863** dengan formula `DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value`. Effect = `TempData["Error"] = "Ujian sudah ditutup..."` + `RedirectToAction("Assessment")`. Tidak ada gating di lower layers (AssessmentHub tidak re-check EWCD; hanya `Status == "InProgress"` filter). Session.Status stays `Upcoming` selama tidak pernah masuk StartExam. **WIB hardcoded `AddHours(7)`** — test harus consistent dengan WIB timezone DB.

3. **CertificatePdf endpoint = simple GET `/CMP/CertificatePdf/{id}`** (`CMPController.cs:1898`) yang return `File(pdfStream.ToArray(), "application/pdf", filename)` dengan filename pattern `Sertifikat_{NIP}_{safeTitle}_{year}.pdf`. Auth = owner OR Admin OR HC role. Gating berurutan: `IsAssessmentSubmitted(Status)` → reject ke `Assessment` jika tidak; `Status == PendingGrading` → redirect Results dengan TempData Info; `!GenerateCertificate` → `NotFound()`; `IsPassed != true` → redirect Results dengan Error. **NomorSertifikat di-assign di `GradingService.cs:288-307`** (di SubmitExam path) dengan retry 3x WHERE NomorSertifikat IS NULL — SYNCHRONOUS di submit, bukan async background. Test FLOW R worker submit (lulus) → NomorSertifikat tersedia immediately di DB; PDF download standalone fetch verify.

**Primary recommendation:** Plan urutan = Wave 1 (parallel) Plan 01 SURF-317-A1 test patch + regression rerun gate, Plan 02 SURF-317-A controller fix + Razor adjust + Phase 317 MA regression rerun gate. Wave 2 (sequential) Plan 03 FLOW P+Q, Plan 04 FLOW R+S, Plan 05 REQUIREMENTS QA-08 doc + final suite gate. Reuse 100% pattern Phase 317 (DOM-text marker, multi-context try/finally, direct SignalR hub invoke, modal href query-string regex). 1 helper baru required: `createPrePostAssessmentViaWizard(opts)` + `verifyCertificatePdfDownload(page, sessionId)`. Selector tambah: `prePostWizardSelectors` const (5 field PreSchedule/PostSchedule/PreDuration/PostDuration/PreEWCD/PostEWCD/samePackageCheck).

---

## User Constraints (from CONTEXT.md)

### Locked Decisions

1. **D-318-01:** SURF-317 fix scope = BOTH A1 (test fixture) + A (production code). A1 = `tests/e2e/exam-taking.spec.ts` FLOW A1 selector single-file ~10 LOC. A = `Controllers/CMPController.cs:2190` `ToDictionary` → `ToLookup` pattern + Razor view update. Risk mitigation: rerun Phase 317 FLOW K + FLOW M post-fix verify still hijau (gate per plan).
2. **D-318-02:** PreTest/PostTest pairing depth = PAIRING + SCORING only. FLOW P verify: (a) wizard `PrePostTest` create 2 sessions, (b) both completable end-to-end, (c) `statusSummary` format `"PreTest:Completed,PostTest:Completed"` di MonitoringDetail. **Skipped:** Razor dual-render Pre vs Post side-by-side (tidak exist di code); PostTest start-gating (tidak explicit di controller); analytics endpoint score delta (Phase 319).
3. **D-318-03:** EWCD test = wizard set past date → worker `StartExam` reject. Cert PDF test = UI download + DB NomorSertifikat verify. **Skipped:** PDF text parse (brittle); EWCD Tier-1/Tier-2 extension (Phase 313 covers).
4. **D-318-04:** FLOW S = AllowAnswerReview true vs false COMPARISON (paired sub-tests S1-S6) — verify Razor branch toggle `Views/CMP/Results.cshtml:316-399`. Phase 317 FLOW N covers negative case sudah; FLOW S formally pairs.
5. **D-318-05:** Test file organization = APPEND FLOW P/Q/R/S ke `tests/e2e/exam-types.spec.ts` (sequential mode, shared state per describe). SURF-317-A1 = separate file change. SURF-317-A = production code change (controller + Razor).
6. **D-318-06:** Add QA-08 baru di `.planning/REQUIREMENTS.md` Future Requirements section. Preserve QA-02 + QA-03. Update ROADMAP Phase 318 `Requirements: QA-08`.

### Claude's Discretion

- Helper file split: extend `examTypes.ts` atau new `examTypes318.ts`. Recommended: **extend** (consistent dengan Phase 317 reuse pattern — flat exports, satu file).
- DB verify helper for FLOW P/R: inline `db.queryScalar` (Phase 317 K5/M5 pattern) atau add named export `getNomorSertifikat(sessionId)` + `getPrePostPairIds(title, category, userId)`. Recommended: **inline** (1-shot queries, no cross-flow reuse needed).
- FLOW Q reject-verify strategy: scrape TempData `.alert-danger` message OR check redirect URL `/CMP/Assessment` OR check DB session.Status NOT changed to InProgress. Recommended: **kombinasi 2** (redirect URL + DB Status untuk robustness).
- FLOW R PDF download: `page.request.get()` (raw fetch via Playwright APIRequest) atau `page.on('download')` UI download. Recommended: **APIRequest** (simpler, bytes assertion direct, no temp file).

### Deferred Ideas (OUT OF SCOPE)

- PDF text extraction + NomorSertifikat parse from PDF body (brittle template-dependent).
- PostTest start-gating server-side reject test (code path tidak explicit; Phase 313 partial).
- Analytics endpoint paired Pre/Post score delta (Phase 319).
- Cross-session score improvement metric assertion (beyond QA-08).
- Razor dual-render Pre vs Post side-by-side test (tidak exist di code).
- Wholesale FLOW A-J refresh (Phase 320 territory — out of Phase 318 scope kecuali SURF-317-A1 single-line patch).

---

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| QA-08 | Advanced exam features E2E: PrePostTest paired full cycle, EWCD enforcement (post-window reject), Certificate PDF download (NomorSertifikat generated + downloadable), AllowAnswerReview true vs false comparison | Sections "FLOW P/Q/R/S Skeletons" + "Helper Extensions Required" + "State of the Art" memberikan selector + endpoint + DB schema yang precise per requirement. Carryover SURF-317-A1 + SURF-317-A fix didokumentasi di section "SURF-317-A Production Fix" + "SURF-317-A1 Test Fix". |

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Create paired PrePost sessions via wizard | Frontend (CreateAssessment.cshtml wizard JS) | API (`POST /AssessmentAdmin/CreateAssessment` → branch isPrePostMode) | Wizard switch via `#assessmentTypeInput` value="PrePostTest"; backend creates 2 sessions ATOMIC per peserta dalam single transaction |
| Enforce EWCD post-window reject | API (`CMPController.StartExam:863`) | Browser (TempData warning + redirect Assessment) | Guard `DateTime.UtcNow.AddHours(7) > ExamWindowCloseDate.Value`. No lower-layer re-check di Hub |
| Download Certificate PDF | API (`CMPController.CertificatePdf:1898`) | Browser (binary stream download) | QuestPDF synchronous generation; auth=owner/Admin/HC; gating Status submitted → !PendingGrading → GenerateCertificate=true → IsPassed=true |
| Verify NomorSertifikat generated | DB (AssessmentSessions.NomorSertifikat) | API (`GradingService.cs:288-307`) | Sync di SubmitExam path, retry 3x WHERE NomorSertifikat IS NULL — bukan async background |
| Verify AllowAnswerReview branch | Browser (Razor `@if Model.AllowAnswerReview`) | API (Results action populate QuestionReviews) | Server-side Razor `@if` toggle entire `.card "Tinjauan Jawaban"` block; `else if (!AllowAnswerReview)` shows `.alert-info` |
| SURF-317-A fix (MA Results aggregation) | API (`CMPController.Results:2190` ToDictionary → ToLookup) | Browser (Razor loop adjust `Views/CMP/Results.cshtml:355` foreach option iteration) | Root cause: SaveMultipleAnswer insert N rows per MA question (1 row per selected option); ToDictionary throws "An item with the same key" ArgumentException |
| SURF-317-A1 fix (legacy test selector) | Test code (`tests/e2e/exam-taking.spec.ts:40`) | — | Single-line selector swap; mirrors Phase 317 examTypes.ts:62-74 pattern |

---

## Standard Stack

### Core (REUSE Phase 317)

| Library | Version | Purpose | Status |
|---------|---------|---------|--------|
| @playwright/test | ^1.58.2 | E2E test runner | [VERIFIED: tests/package.json 2026-05-12] — no upgrade |
| typescript | ^5.9.3 | Type safety untuk test code | [VERIFIED: tests/package.json] — no upgrade |
| QuestPDF (server-side) | (per .csproj) | PDF generation `CertificatePdf` action | [VERIFIED: Controllers/CMPController.cs:1962 `Document.Create`] — read-only, no install change |

### Helpers (REUSE Phase 317 — 8 existing exports)

| Module | Path | Purpose | Reuse Phase 318 |
|--------|------|---------|------------------|
| `createAssessmentViaWizard` | `tests/e2e/helpers/examTypes.ts:51` | 4-step wizard traversal (Standard type) | FLOW Q+R+S — reuse |
| `createDefaultPackage` | `tests/e2e/helpers/examTypes.ts:121` | Create 1 package post-wizard | FLOW P+Q+R+S — reuse |
| `addQuestionViaForm` | `tests/e2e/helpers/examTypes.ts:154` | Right-pane form sequential add | FLOW P+R+S — reuse |
| `submitExamTwoStep` | `tests/e2e/helpers/examTypes.ts:223` | 2-step submit StartExam→Summary→Results | FLOW P+R+S — reuse |
| `checkMAOptionsForQuestion` | `tests/e2e/helpers/examTypes.ts:255` | DOM-text MA correct-options tick | (none — FLOW P/Q/R/S pakai MC default) |
| `fillEssayAnswer` | `tests/e2e/helpers/examTypes.ts:288` | Direct SignalR hub invoke Essay | (none — FLOW P/Q/R/S MC-only per scope) |
| `gradeSingleEssaySession` | `tests/e2e/helpers/examTypes.ts:354` | HC essay grading + finalize | (none — FLOW P MC default) |
| `addExtraTimeViaModal` | `tests/e2e/helpers/examTypes.ts:431` | SignalR ExtraTime broadcast | (none — Phase 317 FLOW O coverage) |

### NEW Phase 318 (target ~2-3 exports)

| Module | Path | Purpose |
|--------|------|---------|
| `createPrePostAssessmentViaWizard` | `tests/e2e/helpers/examTypes.ts` (append) | Wrap wizard dengan `assessmentTypeInput = "PrePostTest"` + Pre/Post schedule/duration/EWCD fields |
| `verifyCertificatePdfDownload` | `tests/e2e/helpers/examTypes.ts` (append) | `page.request.get('/CMP/CertificatePdf/{id}')` + assert status=200 + content-type + bytes>0 |
| `prePostWizardSelectors` | `tests/e2e/helpers/wizardSelectors.ts` (append) | 7 const fields: preSchedule, preDurationMinutes, preExamWindowCloseDate, postSchedule, postDurationMinutes, postExamWindowCloseDate, samePackageCheck |

**Installation:** None — semua deps existing.

---

## Helper Extensions Required

### `createPrePostAssessmentViaWizard(page, opts)`

```typescript
// Append ke tests/e2e/helpers/examTypes.ts
// Source: Views/Admin/CreateAssessment.cshtml:198-201 (#assessmentTypeInput dropdown)
//         Views/Admin/CreateAssessment.cshtml:411-465 (#ppt-jadwal-section collapse panel)
//         Controllers/AssessmentAdminController.cs:1156-1279 (isPrePostMode 2-session create)
//
// Pre-condition: caller sudah login as 'hc'. Helper handles:
//  - Step 1: select AssessmentTypeInput=PrePostTest (TIDAK default Standard).
//  - Step 1: 'change' event di assessmentTypeInput show ppt-jadwal-section (CreateAssessment.cshtml:950-960
//    show/hide via collapse).
//  - Step 3: fill PreSchedule + PreDurationMinutes + PreExamWindowCloseDate +
//    PostSchedule + PostDurationMinutes + PostExamWindowCloseDate. SamePackageCheck optional.
//  - Step 3: SKIP single-#schedDateInput/#ewcdDateInput fields (validation skip for PrePost per
//    CreateAssessment.cshtml:1443 "skip for Pre-Post — has per-phase EWCD").
//
// Verify: success modal `#successModal` show + #createdAssessmentData JSON contains
// `IsPrePostTest: true` + array `Sessions[{ PreId, PostId, UserId, UserName, UserEmail }]`.

export interface CreatePrePostOpts {
  title: string;
  category: string;
  preSchedule: string;       // ISO datetime-local format: 'YYYY-MM-DDTHH:mm'
  preDurationMinutes: number;
  preExamWindowCloseDate: string; // ISO datetime-local
  postSchedule: string;
  postDurationMinutes: number;
  postExamWindowCloseDate: string;
  passPercentage: number;
  allowAnswerReview: boolean;
  generateCertificate?: boolean;
  participantEmails: string[];
  samePackage?: boolean;     // checkbox name="SamePackage" value="true"
}

export async function createPrePostAssessmentViaWizard(
  page: Page,
  opts: CreatePrePostOpts
): Promise<{ preIds: number[]; postIds: number[] }>;
```

**Return value strategy:** parse `#createdAssessmentData` script tag JSON (controller injects per `AssessmentAdminController.cs:1251-1266`) untuk extract sessionId pair per peserta. Alternative: DB query post-wizard `SELECT Id, AssessmentType FROM AssessmentSessions WHERE Title=@t AND Category=@c ORDER BY Id` — robust tapi extra roundtrip.

### `verifyCertificatePdfDownload(page, sessionId)`

```typescript
// Append ke tests/e2e/helpers/examTypes.ts
// Source: Controllers/CMPController.cs:1898-1930 (auth + gating + File response)
//
// Strategy: pakai page.request.get() (APIRequest context) — bypass DOM/navigation.
// Pre-condition: page sudah login (cookies inherited dari context).
//
// Assertions:
//  1. response.status() === 200
//  2. response.headers()['content-type'] startsWith 'application/pdf'
//  3. response.headers()['content-disposition'] match /attachment; filename=Sertifikat_/i
//  4. (await response.body()).length > 1024  — minimum 1KB (avoid 0-byte regression
//     guarded di line 2118-2122)

export async function verifyCertificatePdfDownload(
  page: Page,
  sessionId: number
): Promise<{ bytes: number; filename: string }>;
```

### Selectors append `prePostWizardSelectors` const

```typescript
// Append ke tests/e2e/helpers/wizardSelectors.ts
// Source: Views/Admin/CreateAssessment.cshtml:420-465 (Pre + Post jadwal panels)
//
// Field naming convention: ID camelCase, name PascalCase (mirror Razor markup).
// All inputs di-render dengan id + name attribute — pakai #id (consistent dengan
// pattern wizardSelectors lain).

export const prePostWizardSelectors = {
  jadwalSection: '#ppt-jadwal-section',     // collapse panel parent
  preSchedule: '#preSchedule',                 // datetime-local
  preDurationMinutes: '#preDurationMinutes',
  preExamWindowCloseDate: '#preExamWindowCloseDate',
  postSchedule: '#postSchedule',
  postDurationMinutes: '#postDurationMinutes',
  postExamWindowCloseDate: '#postExamWindowCloseDate',
  samePackageCheck: '#samePackageCheck',
} as const;
```

### DB helper extensions (inline, NO named export)

Phase 317 Plan 02 pattern (FLOW M5 line 230-247) — inline `db.queryScalar` per assertion:

```typescript
// FLOW P3 — verify 2 sessions created
const pairCount = await db.queryScalar(
  `SELECT COUNT(*) FROM AssessmentSessions WHERE Title = '${title}' AND Category = '${category}'`
);
expect(pairCount).toBe(2);

// FLOW P3 — verify Pre + Post pair linked
const linkedOk = await db.queryScalar(
  `SELECT COUNT(*) FROM AssessmentSessions
   WHERE Title = '${title}'
     AND AssessmentType IN ('PreTest', 'PostTest')
     AND LinkedSessionId IS NOT NULL`
);
expect(linkedOk).toBe(2);

// FLOW R — verify NomorSertifikat generated
const nomor = await db.queryScalar(
  `SELECT ISNULL(NomorSertifikat, '') FROM AssessmentSessions WHERE Id = ${sessionId}`
);
expect(typeof nomor).toBe('string');
expect((nomor as string).length).toBeGreaterThan(0);

// FLOW Q — verify session not started (no InProgress flip post-EWCD reject)
const statusUnchanged = await db.queryScalar(
  `SELECT Status FROM AssessmentSessions WHERE Id = ${sessionId}`
);
expect(statusUnchanged).not.toBe('InProgress');
```

**SQL injection note:** Title is from `uniqueTitle()` (`Date.now()` numeric suffix), Category is fixed enum literal. No user input flows ke SQL — concat-OK untuk test code (local dev DB only per CLAUDE.md SEED_WORKFLOW).

---

## FLOW P/Q/R/S Skeletons

### FLOW P — PreTest/PostTest Paired Full Cycle

**Marker convention:** `[318-P]` prefix di title + `[318-P-PRE]` / `[318-P-POST]` di question text.

**Sub-test breakdown (estimated 6 sub-tests):**

- **P1** — HC create PrePost assessment via wizard (new helper) + extract preId + postId dari `#createdAssessmentData` JSON OR DB query.
- **P2** — HC navigate ManagePackages Pre session + createDefaultPackage + add 1 MC question (`[318-P-PRE] correct=A`).
- **P3** — HC navigate ManagePackages Post session + createDefaultPackage + add 1 MC question (`[318-P-POST] correct=B`). (Alternatif: `samePackage=true` → skip P3 manual create, package auto-sync — but scope D-318-02 says "Both complete-able" tidak require samePackage feature test).
- **P4** — Worker take PreTest → submit → DB verify Status=Completed + score>0.
- **P5** — Worker take PostTest → submit → DB verify Status=Completed + score>0.
- **P6** — HC visit AssessmentMonitoringDetail (URLSearchParams pattern) + verify `statusSummary` text format `PreTest:Completed,PostTest:Completed`. Source: `AssessmentAdminController.cs:3614` Json response field `status`. **Strategi verify:** intercept JSON via `page.route` OR scrape DOM (jika MonitoringDetail render statusSummary text di view).

**Selector dependencies:**
- Existing wizardSelectors (Step 1/2/4 + #assessmentTypeInput).
- NEW prePostWizardSelectors (Step 3 pre/post fields).
- Existing questionFormSelectors (no change).
- Existing extraTimeSelectors (no use — N/A FLOW P).

**DB verify queries:**

```sql
-- P1 verify pair create
SELECT Id, AssessmentType, Status, LinkedSessionId, LinkedGroupId
  FROM AssessmentSessions
 WHERE Title = @title AND Category = @category
 ORDER BY Id;
-- Expect: 2 rows, types PreTest + PostTest, LinkedSessionId cross-pointing,
-- LinkedGroupId same value (=preSessions[0].Id per controller line 1190).

-- P4 verify PreTest submit
SELECT Status, Score FROM AssessmentSessions WHERE Id = @preId;
-- Expect: Status='Completed' (or 'PendingGrading' jika ada Essay; MC-only = Completed), Score>0.

-- P6 verify Post completed (independent dari Pre)
SELECT Status FROM AssessmentSessions WHERE Id = @postId;
-- Expect: Status='Completed'.
```

**Critical pitfalls (FLOW P):**

1. **AssessmentType dropdown trigger:** `#assessmentTypeInput` change event toggles `#ppt-jadwal-section` visibility (CreateAssessment.cshtml:950-960). Test HARUS `selectOption` SEBELUM mengisi Step 3 jadwal, otherwise pre/post fields hidden. Add waitForFunction kalau perlu.
2. **Step 3 validation skip for PrePost:** Per CreateAssessment.cshtml:1443, validation `EWCD >= Schedule` di-skip jika `isPrePost === true`. Standard `#schedDateInput` + `#ewcdDateInput` ignored — JANGAN fill (Razor markup tidak require Pre input ke hidden combiner). Confirmed via `Controllers/AssessmentAdminController.cs:1156-1188` — model.Schedule TIDAK consumed; pakai PreSchedule + PostSchedule discrete.
3. **`#createdAssessmentData` JSON extraction:** Script tag injected via `Views/Admin/CreateAssessment.cshtml:1660-1665` (`var a = JSON.parse(createdData);` — IsPrePostTest === true branch). Pakai `page.evaluate(() => JSON.parse(document.getElementById('createdAssessmentData').textContent))`. Sessions array provides PreId + PostId per peserta — DETERMINISTIC mapping.
4. **Worker `/CMP/Assessment` list separate card per session:** PreTest + PostTest tampil sebagai 2 cards dengan title sama. Worker click card harus filter by `AssessmentType` badge OR text — Phase 317 Plan 02 FLOW O pakai `.first()` (collision avoidance). FLOW P harus distinguish: pakai `:has-text("Pre-Test")` filter atau iterate dengan order asumsi (Pre first di list per Schedule sort).
5. **Schedule format `datetime-local`:** Input type `datetime-local` accept format `YYYY-MM-DDTHH:mm`. Today's format helper `today()` return `YYYY-MM-DD` saja — perlu concat `T08:00` atau buat new helper `nowIso()`.

**DOM-text marker convention:** Use `[318-P-PRE]` + `[318-P-POST]` di question text (consistent dengan Phase 317 `[317-K]` precedent). Helps disambiguate jika SamePackage feature ditest later.

---

### FLOW Q — ExamWindowCloseDate Reject

**Marker convention:** `[318-Q]` prefix di title + `[318-Q-PAST]` question marker.

**Sub-test breakdown (estimated 4 sub-tests):**

- **Q1** — HC create assessment via standard wizard dengan EWCD = YESTERDAY + 23:59 WIB.
- **Q2** — HC createDefaultPackage + add 1 MC question.
- **Q3** — Worker login + navigate `/CMP/StartExam/{id}` directly (atau click card dari `/CMP/Assessment` list).
- **Q4** — Verify reject: (a) page redirected ke `/CMP/Assessment`, (b) `.alert-danger` atau TempData error message match `/Ujian sudah ditutup/i`, (c) DB session.Status TIDAK `InProgress` (still `Open` atau `Upcoming`).

**Selector dependencies:**
- Existing wizardSelectors (full reuse).
- Existing `#ewcdDateInput` + `#ewcdTimeInput` (sudah di `wizardSelectors`).

**DB verify queries:**

```sql
-- Q4 verify session NOT transitioned to InProgress
SELECT Status, StartedAt FROM AssessmentSessions WHERE Id = @sessionId;
-- Expect: Status IN ('Open', 'Upcoming'), StartedAt IS NULL.
```

**Critical pitfalls (FLOW Q):**

1. **WIB timezone hardcoded `UtcNow.AddHours(7)`** (`CMPController.cs:863`). Test machine local clock harus assumed WIB OR test set EWCD ≥24h di past (yesterday 23:59 WIB = safe margin). Recommend: `ewcdDate = yesterday()`, `ewcdTime = '23:59'`.
2. **`yesterday()` helper missing:** `tests/helpers/utils.ts` only export `today()`. Phase 318 needs `yesterday()`:
   ```typescript
   export const yesterday = (): string => {
     const d = new Date(Date.now() - 24 * 60 * 60 * 1000);
     return d.toISOString().split('T')[0];
   };
   ```
   Add ke `tests/helpers/utils.ts` (single-line addition, low risk).
3. **Worker `/CMP/Assessment` card mungkin TIDAK clickable post-EWCD:** card render mungkin disable Start button kalau EWCD expired (UI guard supplement). Verify via direct navigation `page.goto('/CMP/StartExam/{id}')` to bypass UI guard — pure server-side guard test.
4. **Schedule date = scheduleDate today, EWCD = yesterday — inconsistent:** Wizard validation `EWCD >= Schedule` (CreateAssessment.cshtml:1446) BLOCKS submit kalau EWCD < schedule. Workaround: schedule juga = yesterday (validates pass since equal). Alternative: helper accept `ewcdOverride` flag yang explicitly set scheduleDate same as ewcdDate.
5. **TempData["Error"] flash semantics:** TempData survives one redirect. Setelah redirect ke `/CMP/Assessment`, message tampil di list page (kemungkinan via `_TempDataAlert.cshtml` partial). Selector: `.alert-danger:has-text("Ujian sudah ditutup")` di body.

**DOM-text marker convention:** Title prefix `[318-Q]` cukup; no need question-level marker (only 1 question, never executed).

---

### FLOW R — Certificate PDF Download + NomorSertifikat

**Marker convention:** `[318-R]` prefix di title + `[318-R] correct=A` question marker.

**Sub-test breakdown (estimated 5 sub-tests):**

- **R1** — HC create assessment dengan `generateCertificate=true`, `passPercentage=70`, `allowAnswerReview=true`.
- **R2** — HC createDefaultPackage + add 1 MC question (correct=A) score 100.
- **R3** — Worker take exam dengan correct answer → submitExamTwoStep → verify Status=Completed + IsPassed=true di DB.
- **R4** — Worker GET `/CMP/CertificatePdf/{sessionId}` via APIRequest (cookies inherited) → assert response status=200 + content-type application/pdf + body bytes > 1024.
- **R5** — DB verify NomorSertifikat populated: `SELECT NomorSertifikat FROM AssessmentSessions WHERE Id={sessionId}` returns non-null + non-empty string (format `CertNumberHelper.Build` per `GradingService.cs:306`).

**Selector dependencies:**
- Existing wizardSelectors (`#GenerateCertificate` checkbox already di wizardSelectors).
- No new selectors needed.

**DB verify queries:**

```sql
-- R3 verify submit OK + passed
SELECT Status, IsPassed, Score, GenerateCertificate
  FROM AssessmentSessions WHERE Id = @sessionId;
-- Expect: Status='Completed', IsPassed=1, Score=100, GenerateCertificate=1.

-- R5 verify NomorSertifikat generated SYNCHRONOUSLY at submit (NOT async)
SELECT NomorSertifikat FROM AssessmentSessions WHERE Id = @sessionId;
-- Expect: non-null, non-empty string per CertNumberHelper.Build format
-- (verified: GradingService.cs:288-307 sync in submit path).
```

**Critical pitfalls (FLOW R):**

1. **CertificatePdf auth = cookies-bound:** APIRequest via `page.request.get()` inherits page context cookies. Pre-condition: worker harus sudah login DI page context yang sama. Jangan instantiate `request.newContext()` fresh (would lose auth).
2. **PendingGrading branch redirect:** Jika ada Essay question di package, Status=`PendingGrading` setelah submit → CertificatePdf redirect ke Results dengan TempData Info (line 1923-1927). FLOW R must be MC-only.
3. **GenerateCertificate=false → 404:** Line 1929 `if (!assessment.GenerateCertificate) return NotFound()`. Wizard `#GenerateCertificate` checkbox MUST be checked explicitly (opts.generateCertificate: true).
4. **IsPassed != true → redirect Results:** Line 1931. Worker MUST answer correct enough untuk lulus passPercentage. Test simple: 1 question score 100, correct answer = score 100 ≥ pass 70.
5. **PDF generation 0-byte regression:** Line 2118-2122 guard log error + return Error redirect. Worker pdf.GeneratePdf may fail silently (e.g., missing font); assert `body().length > 1024` (1KB sanity, ribuan bytes typical untuk QuestPDF 1-page certificate).
6. **NomorSertifikat retry race:** `GradingService.cs:298-322` retry up to 3 attempts WHERE NomorSertifikat IS NULL with sequence increment. After successful submit, NomorSertifikat is populated SYNCHRONOUSLY (await ExecuteUpdateAsync). Test can query DB IMMEDIATELY post-submit, no need delay.
7. **Filename pattern brittle:** `Sertifikat_{NIP}_{safeTitle}_{year}.pdf` — title regex-sanitized `[^a-zA-Z0-9]` → `_`. Avoid asserting exact filename; only assert `startsWith('Sertifikat_')` via Content-Disposition header.

**DOM-text marker convention:** `[318-R]` title prefix only.

---

### FLOW S — AllowAnswerReview True vs False (Paired Comparison)

**Marker convention:** `[318-S-TRUE]` + `[318-S-FALSE]` prefix di title (2 separate assessments untuk explicit paired comparison).

**Sub-test breakdown (estimated 6 sub-tests — 2 assessments × 3 sub-tests):**

- **S1** — HC create assessment A `allowAnswerReview=true`, 1 MC question.
- **S2** — Worker submit answer → navigate /CMP/Results/{id} → verify `.card "Tinjauan Jawaban"` VISIBLE (positive).
- **S3** — Verify positive: `.list-group-item` count = 1 (matching question count) + badge `text-bg-success` atau `text-bg-danger` visible per question.
- **S4** — HC create assessment B `allowAnswerReview=false`, 1 MC question (separate title `[318-S-FALSE]`).
- **S5** — Worker submit answer → navigate /CMP/Results/{id} → verify `.alert-info "Tinjauan jawaban tidak tersedia"` VISIBLE (negative — mirror Phase 317 FLOW N pattern).
- **S6** — Verify negative: `.card "Tinjauan Jawaban"` count = 0 (`toHaveCount(0)`).

**Selector dependencies:**
- Existing wizardSelectors (`#AllowAnswerReview` checkbox).
- No new selectors.

**DB verify queries:** Not strictly needed for FLOW S (UI assertions cukup). Optional sanity:

```sql
-- After S1, verify allowAnswerReview=true
SELECT AllowAnswerReview FROM AssessmentSessions WHERE Id = @sessionA;
-- Expect: 1.

-- After S4, verify allowAnswerReview=false
SELECT AllowAnswerReview FROM AssessmentSessions WHERE Id = @sessionB;
-- Expect: 0.
```

**Critical pitfalls (FLOW S):**

1. **Razor branch logic** (`Views/CMP/Results.cshtml:316-399`):
   ```
   @if (Model.AllowAnswerReview && Model.QuestionReviews != null) → div.card "Tinjauan Jawaban"
   else if (!Model.AllowAnswerReview) → div.alert.alert-info "Tinjauan jawaban tidak tersedia"
   ```
   Edge case: `AllowAnswerReview=true` + `QuestionReviews == null` → NEITHER branch tampil. Should not happen di FLOW S (worker submit → QuestionReviews populated). Verify CMPController.Results:2200 explicitly creates QuestionReviews list when AllowAnswerReview.
2. **MC-only question type:** Pakai MC default untuk avoid PendingGrading complication. Essay/Mixed bukan target FLOW S.
3. **SURF-317-A regression risk:** FLOW S uses MC default (NOT MA) → SURF-317-A bug TIDAK trigger. Boleh assert UI langsung tanpa DB workaround.
4. **2 separate assessments, 2 separate sessionIds:** Variables `sessionA` + `sessionB` shared antar sub-test (sequential mode di describe block).
5. **Negative S5 reuses Phase 317 FLOW N pattern:** Phase 317 FLOW N (`exam-types.spec.ts` N1-N4) sudah passing — copy assertion idiom verbatim. Selector: `.alert-info, .alert.alert-info` dengan `hasText: /Tinjauan jawaban tidak tersedia/i`.

**DOM-text marker convention:** `[318-S-TRUE]` + `[318-S-FALSE]` title prefix untuk visual separation di test report.

---

## SURF-317-A Production Fix Approach

### Root Cause Recap

**File:** `Controllers/CMPController.cs:2190`

**Current code:**
```csharp
var responseDict = packageResponses.ToDictionary(r => r.PackageQuestionId);
```

**Bug:** `Hubs/AssessmentHub.cs:240-249` `SaveMultipleAnswer` insert ONE `PackageUserResponse` row PER selected option (e.g., MA question with 2 correct answers → 2 rows with same `PackageQuestionId`). `ToDictionary(r => r.PackageQuestionId)` throws `ArgumentException: An item with the same key has already been added` saat ada MA question. Result: Results page 500.

**Discovered:** Phase 317 Plan 01 FLOW K → workaround pakai DB-based score verify di K5 + M5.

### Proposed Fix (1 controller + 1 Razor view)

**Step 1: Controller `Controllers/CMPController.cs:2190` — switch to ToLookup**

```csharp
// BEFORE:
var responseDict = packageResponses.ToDictionary(r => r.PackageQuestionId);

// AFTER:
var responseLookup = packageResponses.ToLookup(r => r.PackageQuestionId);
```

**Step 2: Update consumers di Results action (lines 2209-2249)**

Current consumer line 2209:
```csharp
responseDict.TryGetValue(qId, out var userResponse);
var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
var selectedOption = userResponse?.PackageOptionId != null
    ? question.Options.FirstOrDefault(o => o.Id == userResponse.PackageOptionId)
    : null;
bool isCorrect = selectedOption != null && selectedOption.IsCorrect;
```

Refactored (MA-aware):
```csharp
var userResponses = responseLookup[qId].ToList();  // empty list if no answer
var correctOptions = question.Options.Where(o => o.IsCorrect).ToList();
var selectedOptionIds = userResponses
    .Where(r => r.PackageOptionId != null)
    .Select(r => r.PackageOptionId!.Value)
    .ToHashSet();
var selectedOptions = question.Options.Where(o => selectedOptionIds.Contains(o.Id)).ToList();

// MA-correct semantics: user selected EXACTLY all-correct, none-incorrect
bool isCorrect;
if ((question.QuestionType ?? "MultipleChoice") == "MultipleAnswer")
{
    var correctIds = correctOptions.Select(o => o.Id).ToHashSet();
    isCorrect = correctIds.SetEquals(selectedOptionIds);
}
else
{
    // MC + Essay path: single PackageOptionId or null
    var single = selectedOptions.FirstOrDefault();
    isCorrect = single != null && single.IsCorrect;
}
if (isCorrect) correctCount++;
```

**Step 3: Update consumer line 2244 (AllowAnswerReview=false branch correct counting)**

Same MA-aware logic — extract ke helper `IsResponseCorrect(question, userResponses)` if duplication painful.

**Step 4: Update consumer line 2267-2273 (ElemenTeknis scoring)**

```csharp
var correct = g.Count(q =>
{
    var resp = responseLookup[q.Id].ToList();
    if (!resp.Any()) return false;
    // MA-aware:
    var selectedIds = resp.Where(r => r.PackageOptionId != null)
                          .Select(r => r.PackageOptionId!.Value).ToHashSet();
    if ((q.QuestionType ?? "MultipleChoice") == "MultipleAnswer")
    {
        var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
        return correctIds.SetEquals(selectedIds);
    }
    else
    {
        var sel = q.Options.FirstOrDefault(o => selectedIds.Contains(o.Id));
        return sel != null && sel.IsCorrect;
    }
});
```

**Step 5: Razor view `Views/CMP/Results.cshtml:355-386` — option-level review loop**

Current logic per option (line 365):
```csharp
else if (option.IsSelected && !option.IsCorrect) { ... }
```

`OptionReviewItem.IsSelected` (set di controller line 2228 `IsSelected = userResponse?.PackageOptionId == o.Id`) needs MA-aware update — controller should populate IsSelected via `selectedOptionIds.Contains(o.Id)` (HashSet match for both MC + MA). Razor markup tidak butuh change kalau controller correctly populates IsSelected.

Adjustment di controller line 2228:
```csharp
// BEFORE:
IsSelected = userResponse?.PackageOptionId == o.Id

// AFTER:
IsSelected = selectedOptionIds.Contains(o.Id)
```

### Regression Risk + Mitigation

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| MC questions break (changed IsSelected semantics) | LOW — HashSet single-item equivalent to direct `==` for MC | Rerun Phase 317 FLOW N (MC + allowAnswerReview=false) + FLOW O (MC + AddExtraTime) — must stay hijau |
| MA questions correct-count regressed | MEDIUM | Rerun Phase 317 FLOW K (MA full cycle) + FLOW M (Mixed includes MA) — DB-based score expected to stay 100 |
| Essay PendingGrading branch break | LOW — Essay doesn't use ToDictionary path heavily | Rerun Phase 317 FLOW L (Essay full cycle) |
| ElemenTeknis section break | LOW — same MA-aware refactor pattern | Manual test: assessment with elemenTeknis label populated; pass% calculation matches |

**Plan structure:** Plan 02 SURF-317-A fix MUST gate on Phase 317 full suite rerun (`npx playwright test exam-types.spec.ts`) post-fix — 28/28 must pass.

---

## SURF-317-A1 Test Fix Approach

### Target

**File:** `tests/e2e/exam-taking.spec.ts:40`

**Current:**
```typescript
await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
```

**Bug:** Phase 304+ markup wraps `<input>` di Bootstrap form-check label. `.user-check-item input` resolves to actual element but classifies as "not visible" (visually hidden behind label styling). `.click({ force: true })` worked di legacy Bootstrap 4 markup; current Bootstrap 5 form-check requires `.check()` semantic atau click via label.

### Proposed Fix

Mirror Phase 317 `examTypes.ts:62-74` pattern:

```typescript
// BEFORE (line ~40):
await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });

// AFTER:
await page.locator('.user-check-item[data-email="rino.prasetyo@pertamina.com"] input.user-checkbox').check();
```

**Why this works:**
- `data-email` attribute selector targets DOM uniquely (line 195 Phase 317 RESEARCH verifies attribute present).
- `input.user-checkbox` matches actual checkbox class (Phase 304+ form-check pattern).
- `.check()` is Playwright checkbox-aware action — handles label-wrap, visibility, and aria-checked state automatically.

**LOC delta:** ~1 line modified (10 LOC max if any surrounding waitFor or assertion adjustment needed). Single-file change to `exam-taking.spec.ts`.

### Surrounding Adjustments (likely none, but verify)

Phase 317 baseline report identifies 8 other obsolete selectors di FLOW A:
- `#submitBtn` → `#btnSubmit` (single button at step-4)
- `#ScheduleDate` → `#schedDateInput`
- `input[name="correct_option_index"][value="N"]` → `#correct_A/B/C/D`
- `name="question_text"` → `name="questionText"`
- `name="options"` array → `name="optionA"`..`name="optionD"`
- `/Admin/ManagePackages/{id}` path → `?assessmentId={id}` query
- `/Admin/ManageQuestions` → `/Admin/ManagePackageQuestions`
- Wizard auto-create package → manual createDefaultPackage required

**Per scope guardrail (D-318-01 + scope guard from prompt):** "DO NOT propose changes to exam-taking.spec.ts beyond SURF-317-A1 line-targeted patch."

→ Phase 318 Plan 01 fix LINE 40 ONLY. Tests A2+ may still fail (cascade through other obsolete selectors), but at least file-level serial mode no longer aborts ON LINE 40. Per-FLOW pass rate becomes visible for the first time — that's the Phase 318 deliverable (visibility), not wholesale FLOW A-J fix.

### Verification

After fix, rerun `npx playwright test exam-taking.spec.ts --reporter=list`:
- Expected: A1 PASS (selector now valid), A2+ may still fail due to other obsolete selectors.
- Phase 320 (proposed, deferred per Phase 317 closure) = wholesale FLOW A-J rewrite using examTypes.ts helpers.

---

## Common Pitfalls Inherited from Phase 317

Carryover patterns dari Phase 317 yang APPLY ke Phase 318 sub-tests. Tidak ada penemuan baru — semua sudah hardened di Plan 01 + Plan 02 Phase 317.

### 1. DOM-Text Matching Post-Shuffle (anti-cheat)

**Source:** Phase 317 RESEARCH Pivot 2026-05-11. `CMPController.cs:1188` `BuildCrossPackageAssignment` shuffles questions per session + `StartExam.cshtml:125-128` shuffles options per question.

**Apply to:** FLOW P PreTest/PostTest worker answer (P4, P5). FLOW R worker answer (R3). FLOW S worker answer (S2, S5).

**Pattern:**
```typescript
const qCard = page.locator('[id^="qcard_"]').filter({ hasText: '[318-P-PRE]' });
await qCard.locator('label.list-group-item', { hasText: 'OptionA-text' })
  .locator('input.exam-radio').check();
```

### 2. Direct SignalR Hub Invoke for Essay

**Source:** Phase 317 Plan 01 `fillEssayAnswer` (examTypes.ts:288). UI debounce 2s sets indicator='saved' WITHOUT awaiting roundtrip.

**Apply to:** None — FLOW P/Q/R/S MC-only per scope. Inherit pattern in case future plan extends Essay coverage.

### 3. Modal manage-btn href `?assessmentId=` query-string

**Source:** Phase 317 Plan 01 Wave 0 W0.1 fail discovery. RESEARCH A6 wrong-assumption (path-style); actual = query-string.

**Apply to:** FLOW P1, Q1, R1, S1, S4 — wizard complete → extract assessmentId via regex:
```typescript
const href = await page.locator('#modal-manage-btn').getAttribute('href');
expect(href).toMatch(/\/Admin\/ManagePackages(?:\/|\?assessmentId=)\d+/);
assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
```

**FLOW P specific:** Modal href untuk PrePost mungkin point ke ONE session (Pre[0]) atau group landing — verify dari `Views/Admin/CreateAssessment.cshtml:1663-1700` script tag yang inject createdAssessmentData. Wave 0 verification needed.

### 4. `/Admin/ManagePackageQuestions?packageId=` (NOT ManageQuestions)

**Source:** Phase 317 RESEARCH wrong-then-corrected (`examTypes.ts:155-156` comment).

**Apply to:** FLOW P2, P3, R2, S1, S4 — addQuestionViaForm already encapsulates correct URL.

### 5. createDefaultPackage required before addQuestionViaForm

**Source:** Phase 317 Plan 01 Wave 0 W0.1 discovery. Wizard tidak auto-create package; "Packages (0). No packages yet."

**Apply to:** All FLOW P/Q/R/S that need questions. FLOW Q only needs 1 question (never executed), tetap perlu createDefaultPackage + addQuestionViaForm.

### 6. Multi-context try/finally defensive close

**Source:** Phase 317 Plan 02 FLOW O multi-context isolation.

**Apply to:** None expected in Phase 318 (no concurrent HC + worker flows). FLOW R might benefit if APIRequest is done from separate context — but recommended approach (use `page.request.get()` on existing context) avoids multi-context entirely.

### 7. Static modal backdrop dismiss via `#modal-manage-btn`

**Source:** Phase 317 Pitfall 3. `#successModal` has `data-bs-backdrop="static"` — caller MUST click `#modal-manage-btn` (natural flow) OR `.btn-close-white` to dismiss before next nav.

**Apply to:** Wizard helper `createAssessmentViaWizard` already returns AFTER modal `.show`. Caller (FLOW P1, R1, S1, S4 — but not FLOW Q1 wrap) must click modal-manage-btn to dismiss and navigate to ManagePackages. Phase 317 W0.1 + K1 pattern: extract href first, THEN click modal-manage-btn for dismiss + navigation.

### 8. Strict-mode `.alert-success` violation — pakai `.first()`

**Source:** Phase 317 Plan 01 examTypes.ts:211 (addQuestionViaForm strict-mode fix). Global toast scoped (`b-06zfpy70xb`) + inline TempData alert coexist.

**Apply to:** Any `.alert-success` assertion — always wrap dengan `.first()`. Already encapsulated di Phase 317 helpers.

---

## Code Examples (Verified)

### FLOW P Skeleton (PreTest/PostTest Pairing)

```typescript
// tests/e2e/exam-types.spec.ts (APPEND)
test.describe('FLOW P — PreTest/PostTest Paired Full Cycle', () => {
  let title: string;
  let preId: number;
  let postId: number;
  let prePackageId: number;
  let postPackageId: number;
  const Q_PRE_MARKER = '[318-P-PRE] Q1 — Apa singkatan OJT?';
  const Q_POST_MARKER = '[318-P-POST] Q1 — Apa hasil utama OJT?';
  const PRE_CORRECT = 'On the Job Training';
  const POST_CORRECT = 'Competency improvement';

  test('P1 — HC creates PrePost assessment via wizard', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    title = uniqueTitle('[318-P] PrePost Exam');
    await login(page, 'hc');
    const result = await createPrePostAssessmentViaWizard(page, {
      title,
      category: 'OJT',
      preSchedule: `${today()}T00:01`,
      preDurationMinutes: 60,
      preExamWindowCloseDate: `${today()}T23:59`,
      postSchedule: `${today()}T00:02`,
      postDurationMinutes: 60,
      postExamWindowCloseDate: `${today()}T23:59`,
      passPercentage: 70,
      allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      samePackage: false,
    });
    preId = result.preIds[0];
    postId = result.postIds[0];
    expect(preId).toBeGreaterThan(0);
    expect(postId).toBeGreaterThan(0);
  });

  test('P2 — HC creates package + question for PreTest', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${preId}`);
    await page.waitForLoadState('networkidle');
    prePackageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, prePackageId, {
      type: 'MultipleChoice',
      text: Q_PRE_MARKER,
      options: [PRE_CORRECT, 'Online Job Test', 'Off Job Theory', 'Operational Training'],
      correctIndex: 0,
      score: 100,
    });
  });

  test('P3 — HC creates package + question for PostTest', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${postId}`);
    await page.waitForLoadState('networkidle');
    postPackageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, postPackageId, {
      type: 'MultipleChoice',
      text: Q_POST_MARKER,
      options: ['Theory only', 'Competency improvement', 'Salary increase', 'Promotion'],
      correctIndex: 1,
      score: 100,
    });

    // DB verify pair integrity
    const pairOk = await db.queryScalar(
      `SELECT COUNT(*) FROM AssessmentSessions
       WHERE Title = '${title}' AND LinkedSessionId IS NOT NULL`
    );
    expect(pairOk).toBe(2);
  });

  test('P4 — Worker submits PreTest', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    // Filter card by title + PreTest hint (badge atau AssessmentType text)
    const preCard = page.locator('.assessment-card', { hasText: title })
      .filter({ hasText: /Pre-?Test/i }).first();
    await preCard.locator('a:has-text("Mulai"), a:has-text("Start")').first().click();
    await page.waitForURL(/\/CMP\/StartExam\/\d+/);
    await page.waitForFunction(() => {
      const w = window as unknown as { assessmentHub?: { state?: string } };
      return w.assessmentHub?.state === 'Connected';
    });

    const qCard = page.locator('[id^="qcard_"]').filter({ hasText: Q_PRE_MARKER });
    await qCard.locator('label.list-group-item', { hasText: PRE_CORRECT })
      .locator('input.exam-radio').check();
    await page.locator('#saveIndicatorText').filter({ hasText: /saved|tersimpan/i })
      .waitFor({ timeout: 7_500 });
    await submitExamTwoStep(page);

    const preStatus = await db.queryScalar(
      `SELECT Status FROM AssessmentSessions WHERE Id = ${preId}`
    );
    expect(preStatus).toBe('Completed');
  });

  test('P5 — Worker submits PostTest', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const postCard = page.locator('.assessment-card', { hasText: title })
      .filter({ hasText: /Post-?Test/i }).first();
    await postCard.locator('a:has-text("Mulai"), a:has-text("Start")').first().click();
    await page.waitForURL(/\/CMP\/StartExam\/\d+/);
    await page.waitForFunction(() => {
      const w = window as unknown as { assessmentHub?: { state?: string } };
      return w.assessmentHub?.state === 'Connected';
    });

    const qCard = page.locator('[id^="qcard_"]').filter({ hasText: Q_POST_MARKER });
    await qCard.locator('label.list-group-item', { hasText: POST_CORRECT })
      .locator('input.exam-radio').check();
    await page.locator('#saveIndicatorText').filter({ hasText: /saved|tersimpan/i })
      .waitFor({ timeout: 7_500 });
    await submitExamTwoStep(page);

    const postStatus = await db.queryScalar(
      `SELECT Status FROM AssessmentSessions WHERE Id = ${postId}`
    );
    expect(postStatus).toBe('Completed');
  });

  test('P6 — statusSummary format verify', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'hc');

    // Intercept GetSessionStatus JSON OR scrape rendered text
    // Strategy: visit MonitoringDetail and scrape rendered statusSummary string
    // (atau page.route intercept JSON endpoint untuk strict format check)
    const params = new URLSearchParams({
      title,
      category: 'OJT',
      scheduleDate: today(),
    });
    await page.goto(`/Admin/AssessmentMonitoringDetail?${params.toString()}`);
    await page.waitForLoadState('networkidle');

    // statusSummary text per AssessmentAdminController.cs:3614 format
    // Sample DOM expected: "Status: PreTest:Completed,PostTest:Completed" OR JSON in JS state
    // Decision: Wave 0 verify rendering — if not directly in DOM, intercept JSON via page.route.
    await expect(
      page.locator('body', { hasText: /PreTest:Completed.*PostTest:Completed/i })
    ).toBeVisible({ timeout: 10_000 });
  });
});
```

### FLOW Q Skeleton (EWCD Reject)

```typescript
test.describe('FLOW Q — ExamWindowCloseDate Reject', () => {
  let title: string;
  let assessmentId: number;
  let packageId: number;
  let sessionId: number;

  test('Q1 — HC creates assessment with yesterday EWCD', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    title = uniqueTitle('[318-Q] EWCD Past Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title,
      category: 'OJT',
      scheduleDate: yesterday(),  // SAME as ewcdDate to pass wizard EWCD>=Schedule validation
      scheduleTime: '00:01',
      durationMinutes: 60,
      passPercentage: 70,
      allowAnswerReview: true,
      generateCertificate: false,
      participantEmails: ['rino.prasetyo@pertamina.com'],
      ewcdDate: yesterday(),
      ewcdTime: '23:59',  // yesterday 23:59 WIB — already passed
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');
  });

  test('Q2 — HC creates package + MC question', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice',
      text: '[318-Q] Q1 — placeholder, never executed',
      options: ['A', 'B', 'C', 'D'],
      correctIndex: 0,
      score: 100,
    });

    // Extract sessionId from CMP/Assessment list (via DB to avoid card click hazard)
    sessionId = await db.queryScalar(
      `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title = '${title}'
       AND UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'rino.prasetyo@pertamina.com')`
    ) as number;
    expect(sessionId).toBeGreaterThan(0);
  });

  test('Q3 — Worker attempt StartExam → reject', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'coachee');
    await page.goto(`/CMP/StartExam/${sessionId}`);
    // Server-side guard line 863 triggers → redirect to /CMP/Assessment with TempData Error
    await page.waitForURL(/\/CMP\/Assessment$/, { timeout: 10_000 });

    // Verify TempData Error message
    await expect(
      page.locator('.alert-danger, .alert.alert-danger', { hasText: /Ujian sudah ditutup/i }).first()
    ).toBeVisible({ timeout: 5_000 });
  });

  test('Q4 — DB verify session stays NotStarted', async () => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    const status = await db.queryScalar(
      `SELECT Status FROM AssessmentSessions WHERE Id = ${sessionId}`
    );
    expect(status).not.toBe('InProgress');
    expect(status).not.toBe('Completed');

    const startedAt = await db.queryScalar(
      `SELECT ISNULL(CAST(StartedAt AS varchar), '<null>') FROM AssessmentSessions WHERE Id = ${sessionId}`
    );
    expect(startedAt).toBe('<null>');
  });
});
```

### FLOW R Skeleton (Certificate PDF Download)

```typescript
test.describe('FLOW R — Certificate PDF Download + NomorSertifikat', () => {
  let title: string;
  let assessmentId: number;
  let packageId: number;
  let sessionId: number;
  const Q_MARKER = '[318-R] Q1 — Apa singkatan HC?';
  const Q_CORRECT = 'Human Capital';

  test('R1 — HC creates assessment with GenerateCertificate=true', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    title = uniqueTitle('[318-R] Cert Exam');
    await login(page, 'hc');
    await createAssessmentViaWizard(page, {
      title,
      category: 'OJT',
      scheduleDate: today(),
      scheduleTime: '00:01',
      durationMinutes: 60,
      passPercentage: 70,
      allowAnswerReview: true,
      generateCertificate: true,  // CRITICAL
      participantEmails: ['rino.prasetyo@pertamina.com'],
    });
    const href = await page.locator('#modal-manage-btn').getAttribute('href');
    assessmentId = parseInt(href!.match(/(?:\/|assessmentId=)(\d+)/)![1], 10);
    await page.locator('#modal-manage-btn').click();
    await page.waitForLoadState('networkidle');
  });

  test('R2 — HC creates package + 1 MC question (correct=A)', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'hc');
    await page.goto(`/Admin/ManagePackages?assessmentId=${assessmentId}`);
    await page.waitForLoadState('networkidle');
    packageId = await createDefaultPackage(page);
    await addQuestionViaForm(page, packageId, {
      type: 'MultipleChoice',
      text: Q_MARKER,
      options: [Q_CORRECT, 'Health Care', 'Hard Copy', 'Help Center'],
      correctIndex: 0,
      score: 100,
    });
  });

  test('R3 — Worker takes exam (correct answer) + submits + DB Passed', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'coachee');
    await page.goto('/CMP/Assessment');
    const card = page.locator('.assessment-card', { hasText: title }).first();
    await card.locator('a:has-text("Mulai"), a:has-text("Start")').first().click();
    await page.waitForURL(/\/CMP\/StartExam\/\d+/);
    sessionId = parseInt(page.url().match(/StartExam\/(\d+)/)![1], 10);
    await page.waitForFunction(() => {
      const w = window as unknown as { assessmentHub?: { state?: string } };
      return w.assessmentHub?.state === 'Connected';
    });

    const qCard = page.locator('[id^="qcard_"]').filter({ hasText: Q_MARKER });
    await qCard.locator('label.list-group-item', { hasText: Q_CORRECT })
      .locator('input.exam-radio').check();
    await page.locator('#saveIndicatorText').filter({ hasText: /saved|tersimpan/i })
      .waitFor({ timeout: 7_500 });
    await submitExamTwoStep(page);

    const passed = await db.queryScalar(
      `SELECT CAST(IsPassed AS int) FROM AssessmentSessions WHERE Id = ${sessionId}`
    );
    expect(passed).toBe(1);
  });

  test('R4 — Worker downloads Certificate PDF via APIRequest', async ({ page }) => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    await login(page, 'coachee');  // ensure cookies fresh in context
    const result = await verifyCertificatePdfDownload(page, sessionId);
    expect(result.bytes).toBeGreaterThan(1024);
    expect(result.filename).toMatch(/^Sertifikat_/);
  });

  test('R5 — DB verify NomorSertifikat populated', async () => {
    test.setTimeout(FLOW_TIMEOUT_MS);
    const nomor = await db.queryScalar(
      `SELECT ISNULL(NomorSertifikat, '') FROM AssessmentSessions WHERE Id = ${sessionId}`
    );
    expect(typeof nomor).toBe('string');
    expect((nomor as string).length).toBeGreaterThan(0);
  });
});
```

### FLOW S Skeleton (AllowAnswerReview Comparison)

```typescript
test.describe('FLOW S — AllowAnswerReview True vs False', () => {
  let titleTrue: string;
  let titleFalse: string;
  let aIdTrue: number;
  let aIdFalse: number;
  let pkgTrue: number;
  let pkgFalse: number;
  let sessTrue: number;
  let sessFalse: number;
  const Q_TRUE_MARKER = '[318-S-TRUE] Q1';
  const Q_FALSE_MARKER = '[318-S-FALSE] Q1';

  test('S1 — HC creates assessment A allowAnswerReview=true + worker submits + Results visible review card', async ({ page }) => {
    // ... wizard + package + question (correct=A) ...
    // ... worker submit ...
    // Worker visit /CMP/Results/{sessTrue}
    await expect(
      page.locator('.card', { hasText: /Tinjauan Jawaban/i })
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.locator('.card').filter({ hasText: /Tinjauan Jawaban/i })
        .locator('.list-group-item').first()
    ).toBeVisible();
  });

  // S2, S3 split for atomicity ... (mirror Phase 317 FLOW N split N1-N4 pattern)

  test('S4-S6 — HC creates assessment B allowAnswerReview=false + worker submits + Results shows alert-info', async ({ page }) => {
    // ... same flow, allowAnswerReview=false ...
    await expect(
      page.locator('.alert-info, .alert.alert-info', { hasText: /Tinjauan jawaban tidak tersedia/i }).first()
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      page.locator('.card', { hasText: /Tinjauan Jawaban/i })
    ).toHaveCount(0);
  });
});
```

(Full skeleton mirror Phase 317 FLOW N split N1-N4 pattern — atomic step separation. Plan 04 task akan finalize sub-test count.)

---

## State of the Art

| Old Approach (Phase 317 baseline) | Current Approach (Phase 318) | Why |
|--------------|------------------|-----|
| MA Results page 500 (SURF-317-A discovered) — DB workaround | ToLookup fix + MA-aware correct semantics | Phase 318 Plan 02 production fix |
| FLOW A1 selector `.user-check-item input` cascade abort | `.user-check-item[data-email] input.user-checkbox` + `.check()` | Phase 318 Plan 01 single-line patch |
| Wizard helper Standard-only | Added `createPrePostAssessmentViaWizard` for `AssessmentTypeInput=PrePostTest` | Phase 318 Plan 03 new helper |
| FLOW N alone covers AllowAnswerReview=false | FLOW S formally pairs true vs false | Phase 318 Plan 04 explicit comparison |
| FLOW K-O 5 flows = 27 sub-tests | FLOW P-S 4 flows additive = 27 + ~17 = ~44 sub-tests | Phase 318 D-318-05 append |
| EWCD never tested E2E | FLOW Q tests server-side reject + DB Status unchanged | Phase 318 Plan 03 |
| Certificate PDF never tested E2E | FLOW R tests download + bytes>1024 + DB NomorSertifikat | Phase 318 Plan 04 |
| QA-02 only (Phase 317 coverage) | QA-08 added in REQUIREMENTS.md | Phase 318 Plan 05 |

**Deprecated/outdated:**
- DB-based score verify untuk MA (FLOW K5, M5) — once SURF-317-A fix lands, prefer UI assertion via `.badge.text-bg-success`. Plan 02 task includes optional refactor pass (low priority).

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `#preSchedule`, `#preDurationMinutes`, etc. are valid Razor-rendered IDs (verified literal in CreateAssessment.cshtml:420-465) | Helper Extensions | LOW — confirmed line numbers; wizard JS validates accessibly via `getElementById('preSchedule')` etc. |
| A2 | `#createdAssessmentData` script tag contains JSON yang accessible via `page.evaluate` parse — same pattern as standard wizard | Helper Extensions FLOW P | MEDIUM — Phase 317 helper currently only checks `#modal-manage-btn` href; not yet parsed JSON. Wave 0 verify in P1 — fallback: DB query post-wizard |
| A3 | `MonitoringDetail` page renders statusSummary text dalam visible DOM (atau accessible via DOM text scrape) — exact format `"PreTest:Completed,PostTest:Completed"` per controller line 3614 | FLOW P6 | MEDIUM — Wave 0 verify: jika TIDAK rendered text → intercept JSON via `page.route` |
| A4 | `page.request.get()` inherits page context cookies untuk APIRequest auth on CertificatePdf | Helper Extensions FLOW R | LOW — Playwright docs confirm (`page.request` = `context.request`) |
| A5 | `db.queryScalar` returns proper JSON-parseable value (string for varchar, number for int) — Phase 317 K5 + M5 uses this pattern | FLOW P/Q/R DB verify | LOW — proven 3x in Phase 317 |
| A6 | NomorSertifikat populated SYNCHRONOUSLY at SubmitExam — readable in immediate post-submit DB query | FLOW R5 | LOW — verified via GradingService.cs:288-307 `await ExecuteUpdateAsync` semantic |
| A7 | Worker `/CMP/Assessment` list displays BOTH PreTest + PostTest cards immediately after pair create (no Schedule date dependency) | FLOW P4/P5 | MEDIUM — Assessment list filter may hide Pre/Post jika Schedule > now. Mitigation: set preSchedule=today T00:01 yang dijamin ≤ now |
| A8 | `samePackage=false` (default) → 2 separate packages required (P2 + P3) | FLOW P helper | LOW — controller line 1217 `SamePackage = SamePackage` field stored; Phase 317 W0.1 confirms package not auto-created |
| A9 | Razor branch `@if (Model.AllowAnswerReview && Model.QuestionReviews != null)` evaluates to TRUE when MC questions submitted with allowAnswerReview=true | FLOW S2 | LOW — verified line 316 + controller line 2200 `if (assessment.AllowAnswerReview) questionReviews = new List<>` |

**Plan-checker action:** A2 (createdAssessmentData JSON parse) + A3 (MonitoringDetail statusSummary rendering) + A7 (PreTest schedule today list visibility) — all should be verified in Wave 0 smoke step BEFORE FLOW P bulk implementation.

---

## Open Questions

### HIGH confidence (likely needs Wave 0 verify)

1. **GREEN: PrePostTest wizard creates 2 sessions IMMEDIATELY (no on-demand PostTest spawn).**
   - Verified: `Controllers/AssessmentAdminController.cs:1156-1232` creates both Pre + Post sessions di same transaction.
   - Both rows are in DB sejak POST CreateAssessment returns.
   - Test FLOW P can DB-query 2 rows post-wizard.

2. **GREEN: NomorSertifikat generated synchronously di SubmitExam.**
   - Verified: `GradingService.cs:288-307` uses `await ExecuteUpdateAsync` dengan WHERE clause + retry.
   - Test FLOW R can DB-query NomorSertifikat immediately post-submit.

3. **GREEN: EWCD enforcement single point of guard.**
   - Verified: `CMPController.cs:863` only location with `ExamWindowCloseDate` + `UtcNow.AddHours(7)` comparison.
   - No race in Hub layer; if guard fires, session.Status untouched.

### MEDIUM confidence (Wave 0 should verify)

4. **YELLOW: Does MonitoringDetail page render statusSummary in visible DOM?**
   - Known: `AssessmentAdminController.cs:3614` returns statusSummary in JSON (likely from a separate AJAX endpoint, not initial GET render).
   - Unclear: Apakah text muncul di rendered HTML atau diisi via JS post-load.
   - Recommendation: Plan 03 Wave 0 smoke W1.1 — read MonitoringDetail Razor view, search "statusSummary" usage. Fallback: `page.route` intercept JSON endpoint.

5. **YELLOW: Wizard step 3 PrePost-mode validation — is #ppt-jadwal-section visible without first selecting #assessmentTypeInput?**
   - Known: CreateAssessment.cshtml:950-960 toggles `#ppt-jadwal-section` collapse based on `#assessmentTypeInput` value === 'PrePostTest'.
   - Unclear: Apakah toggle fire pada `change` event sahaja, atau juga pada `selectOption` programmatic action.
   - Recommendation: Plan 03 Wave 0 smoke verify — after `selectOption('#assessmentTypeInput', 'PrePostTest')`, wait `#ppt-jadwal-section` visible (timeout 3s).

6. **YELLOW: CertificatePdf endpoint accessible by coachee via APIRequest without redirect to auth challenge?**
   - Known: `CMPController.cs:1906-1913` requires authenticated user + auth check role/owner.
   - Unclear: Apakah login session di Playwright context-passing works untuk APIRequest (vs direct page nav).
   - Recommendation: Plan 04 Wave 0 verify — minimal smoke fetch PDF from coachee context.

### LOW confidence (safe to defer to plan execution)

7. **GREEN: PreSchedule + PostSchedule schedule date filter di /CMP/Assessment worker list.**
   - Recommendation: Set preSchedule=today (T00:01) + postSchedule=today (T00:02) untuk pastikan both cards immediately visible.

8. **GREEN: Wizard helper `createPrePostAssessmentViaWizard` extract preId+postId via #createdAssessmentData script.**
   - Fallback: DB query post-wizard `SELECT Id FROM AssessmentSessions WHERE Title=@t ORDER BY Id`.
   - LOW risk because fallback is trivial.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Playwright runtime | All FLOW P-S tests | ✓ | 1.58.2 | — |
| Node.js | Playwright | Assumed ✓ | (Phase 317 ran successfully) | — |
| ASP.NET Core dev server (port 5277) | Test baseURL | Assumed ✓ via `dotnet run` | — | Manual start before run |
| SQL Server Express (localhost\SQLEXPRESS, HcPortalDB_Dev) | DB verify queries via dbSnapshot.ts | Assumed ✓ | — | — |
| Test accounts (hc/coachee) | login fixtures | ✓ | accounts.ts | — |
| QuestPDF library (server-side) | CertificatePdf endpoint | ✓ | per .csproj | — |
| sqlcmd CLI | dbSnapshot.ts runSqlcmd | Assumed ✓ (Phase 315 W0 verified) | — | — |
| Fonts wwwroot/fonts/*.ttf | CertificatePdf graceful fallback | Optional — graceful fallback per line 1953 | — | Default system fonts |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — all infrastructure exists from Phase 315/316/317.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | @playwright/test 1.58.2 |
| Config file | `tests/playwright.config.ts` (testDir: ./e2e, baseURL: http://localhost:5277, fullyParallel: false, retries: 0) |
| Quick run command (per FLOW) | `cd tests && npx playwright test exam-types.spec.ts --grep "FLOW P"` |
| Full suite command | `cd tests && npx playwright test exam-types.spec.ts` |
| Regression smoke command | `cd tests && npx playwright test exam-taking.spec.ts --reporter=list` |
| Phase 317 carryover regression | Same full suite — must stay 28/28 + new ~16-20 = ~44-48/44-48 |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QA-08 (PrePost pairing) | HC wizard PrePostTest creates 2 sessions, both completable, statusSummary `PreTest:Completed,PostTest:Completed` | e2e (~6 sub-tests P1-P6) | `npx playwright test exam-types.spec.ts -g "FLOW P"` | ❌ Wave 0 |
| QA-08 (EWCD) | Worker StartExam reject post-window, session Status unchanged | e2e (~4 sub-tests Q1-Q4) | `npx playwright test exam-types.spec.ts -g "FLOW Q"` | ❌ Wave 0 |
| QA-08 (Cert PDF) | Worker downloads PDF, content-type application/pdf, bytes>1024, DB NomorSertifikat populated | e2e (~5 sub-tests R1-R5) | `npx playwright test exam-types.spec.ts -g "FLOW R"` | ❌ Wave 0 |
| QA-08 (AllowAnswerReview compare) | Razor branch toggle verified positive + negative paired | e2e (~6 sub-tests S1-S6) | `npx playwright test exam-types.spec.ts -g "FLOW S"` | ❌ Wave 0 |
| SURF-317-A fix | Phase 317 FLOW K + FLOW M regression rerun stays hijau | e2e regression gate | `npx playwright test exam-types.spec.ts -g "FLOW K\|FLOW M"` | ✓ Existing (post-fix re-run gate) |
| SURF-317-A1 fix | Phase 317 baseline rerun shows A1 PASS | e2e regression gate | `npx playwright test exam-taking.spec.ts -g "A1"` | ✓ Existing (post-fix re-run gate) |

### Sampling Rate

- **Per task commit:** Quick run FLOW being implemented (e.g., `--grep "FLOW P"`).
- **Per wave merge:**
  - Wave 1 (SURF fixes): full Phase 317 suite + exam-taking A1 (regression gate).
  - Wave 2 (FLOW P-S): full `exam-types.spec.ts` (~44 sub-tests).
- **Phase gate:** `exam-types.spec.ts` 100% green + `exam-taking.spec.ts` A1 pass + REQUIREMENTS.md QA-08 inserted + ROADMAP Phase 318 Requirements updated.

### Wave 0 Gaps

- [ ] `tests/e2e/helpers/examTypes.ts` extended (createPrePostAssessmentViaWizard + verifyCertificatePdfDownload).
- [ ] `tests/e2e/helpers/wizardSelectors.ts` extended (prePostWizardSelectors const).
- [ ] `tests/helpers/utils.ts` extended (yesterday() helper).
- [ ] `tests/e2e/exam-types.spec.ts` extended (FLOW P/Q/R/S describes — ~17 sub-tests).
- [ ] `tests/e2e/exam-taking.spec.ts` patch line ~40 (SURF-317-A1).
- [ ] `Controllers/CMPController.cs` patch line 2190 + lines 2209-2249 + 2267-2273 + 2228 (SURF-317-A).
- [ ] `Views/CMP/Results.cshtml` review lines 355-386 (option-level IsSelected MA-aware).
- [ ] `.planning/REQUIREMENTS.md` insert QA-08 entry.
- [ ] `.planning/ROADMAP.md:394-399` Phase 318 update Requirements: QA-08.
- [ ] (Optional) `docs/test-reports/2026-05-12-phase-318-summary.md` per CLAUDE.md SOP.

---

## Security Domain

> Phase 318 = mostly test authoring + 1 narrow production fix (SURF-317-A). Security domain mostly N/A.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Test pakai existing login; CertificatePdf endpoint preserves owner/Admin/HC auth |
| V3 Session Management | no | N/A |
| V4 Access Control | partial — FLOW R verifies CertificatePdf access control implicit | Endpoint already enforces auth checks (line 1906-1913); test exercises happy path only |
| V5 Input Validation | partial — server-side EWCD enforcement tested in FLOW Q | Server-side guard line 863 |
| V6 Cryptography | no | N/A — QuestPDF generation deterministic |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SURF-317-A bug = Information Disclosure latent (Results 500 leak stack trace possible) | Information Disclosure | Production fix Plan 02 + verified via ASP.NET Core ProductionMode strips stack from response |
| SURF-317-A1 cascade test abort hides regression visibility | (test-only) | Single-line patch Plan 01 unblocks pass-rate visibility |
| EWCD bypass attempt (worker direct nav StartExam post-window) | Elevation of Privilege (time-based access) | Server-side guard line 863 — Plan 03 FLOW Q tests |
| CertificatePdf access by non-owner | Information Disclosure (PII leak) | Endpoint auth check lines 1909-1913 — implicit coverage Plan 04 FLOW R happy path |

---

## Project Constraints (from CLAUDE.md)

Direktive applicable untuk Phase 318:

1. **Bahasa Indonesia untuk respons + dokumen** — apply ke RESEARCH/PLAN/SUMMARY. Code TypeScript + C# tetap English identifier (consistent dengan codebase).
2. **DEV_WORKFLOW.md SOP**: Test dijalankan **lokal** (port 5277). Production fix SURF-317-A staged via `dotnet build && dotnet run` lokal verify dulu sebelum commit. Tidak deploy langsung ke Dev/Prod.
3. **Seed Data Workflow (lokal)**:
   - FLOW P/Q/R/S create rows di dev DB (klasifikasi temporary + local-only).
   - Sebelum full run, snapshot DB via `dbSnapshot.ts` backup() (Phase 315 infra).
   - Setelah test selesai, restore — atau tandai di `docs/SEED_JOURNAL.md` sebagai temporary cleanup-needed (prefix `[318-P]`, `[318-Q]`, `[318-R]`, `[318-S-TRUE]`, `[318-S-FALSE]`).
4. **Tidak edit kode/DB di server**: Production fix SURF-317-A staged via commit + push. Promosi ke server Dev = Team IT (notif: commit hash + flag "no migration").
5. **Commit format**:
   - Plan 01 (SURF-317-A1): `fix(318-01): SURF-317-A1 — user-check-item selector form-check compat`.
   - Plan 02 (SURF-317-A): `fix(318-02): SURF-317-A — MA Results ToLookup + Razor IsSelected MA-aware` + flag "no migration" + Phase 317 regression rerun results.
   - Plan 03 (FLOW P+Q): `feat(318-03): FLOW P PrePostTest + FLOW Q EWCD reject hijau`.
   - Plan 04 (FLOW R+S): `feat(318-04): FLOW R Cert PDF + FLOW S AllowAnswerReview compare hijau`.
   - Plan 05 (docs): `docs(318-05): REQUIREMENTS QA-08 + ROADMAP Phase 318 closure`.
6. **Promosi ke Dev/Prod = Team IT**: Notif commit hash + flag "no migration" untuk Plan 02 (SURF-317-A production code change). Plan 01 + 03 + 04 + 05 = test code only.

---

## Sources

### Primary (HIGH confidence — direct file read 2026-05-12)

- `.planning/phases/318-pretest-posttest-full-cycle-examwindowclosedate-certificate-pdf-e2e/318-CONTEXT.md` (full).
- `.planning/phases/317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui/317-CONTEXT.md` (full).
- `.planning/phases/317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui/317-RESEARCH.md` (full).
- `.planning/phases/317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui/317-PATTERNS.md` (full).
- `.planning/phases/317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui/317-02-SUMMARY.md` (full).
- `tests/e2e/helpers/examTypes.ts` (full — 478 lines) — 8 existing exports validated.
- `tests/e2e/helpers/wizardSelectors.ts` (full — 106 lines) — wizardSelectors + extraTimeSelectors + questionFormSelectors validated.
- `tests/helpers/dbSnapshot.ts` (head 80 lines) — sqlcmd integration + queryScalar pattern.
- `tests/e2e/exam-types.spec.ts` (head 250 lines) — current 27 sub-tests structure for append baseline.
- `tests/e2e/exam-taking.spec.ts` (head 120 lines) — FLOW A1 target patch verification.
- `Controllers/CMPController.cs:840-960` — EWCD enforcement (line 863) + StartExam flow.
- `Controllers/CMPController.cs:1898-2138` — CertificatePdf full action.
- `Controllers/CMPController.cs:2141-2330` — Results action (SURF-317-A target line 2190 + 2209-2249).
- `Controllers/AssessmentAdminController.cs:1100-1280` — PrePostTest 2-session create flow.
- `Controllers/AssessmentAdminController.cs:2320-2360` — DeletePrePostGroup statusSummary format reference.
- `Controllers/AssessmentAdminController.cs:3600-3640` — Get*Status statusSummary format for type="prepost".
- `Hubs/AssessmentHub.cs:180-290` — SaveMultipleAnswer logic (SURF-317-A root cause).
- `Services/GradingService.cs:286-322` — NomorSertifikat sync generation in SubmitExam.
- `Views/Admin/CreateAssessment.cshtml:195-210, 385-465, 950-960, 1425-1455, 1660-1700` — wizard markup + PrePost UI + script hooks.
- `Views/CMP/Results.cshtml:300-410` — AllowAnswerReview Razor branch.
- `docs/test-reports/2026-05-11-flow-a-j-regression.md` (full) — SURF-317-A1 anchor + fix recommendation.

### Secondary (MEDIUM confidence)

- `Views/CMP/StartExam.cshtml` (referenced by Phase 317 RESEARCH) — exam UI markup.
- `tests/helpers/auth.ts`, `tests/helpers/accounts.ts`, `tests/helpers/utils.ts` (referenced) — shared fixtures.

### Tertiary (LOW confidence — Wave 0 verify recommended)

- MonitoringDetail Razor rendering of statusSummary text (Open Q4) — needs view file read in Wave 0.
- `#createdAssessmentData` script content parsing for PrePostTest mode (Assumption A2) — needs verify in Wave 0 P1.

---

## Metadata

**Confidence breakdown:**

- Helper extensions (createPrePostAssessmentViaWizard, verifyCertificatePdfDownload): HIGH — markup IDs + endpoint signatures verified.
- PrePostTest flow (FLOW P): HIGH — controller 2-session ATOMIC create verified line-by-line.
- EWCD enforcement (FLOW Q): HIGH — single guard line 863 with WIB+7 formula verified.
- Certificate PDF (FLOW R): HIGH — endpoint full read; NomorSertifikat sync timing verified via GradingService.
- AllowAnswerReview comparison (FLOW S): HIGH — Razor branch + controller view-model populate verified.
- SURF-317-A fix: HIGH — root cause confirmed via Hub SaveMultipleAnswer + controller ToDictionary; refactor strategy clear.
- SURF-317-A1 fix: HIGH — Phase 317 examTypes.ts:62-74 pattern proven; baseline report explicit anchor.

**Research date:** 2026-05-12
**Valid until:** 2026-06-12 (30 days — stable codebase, no upcoming UI markup changes planned per ROADMAP).
