# Phase 398: Test + UAT "seakan online" (INJ-13) - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 3 (1 new e2e spec + 1 modified SEED_JOURNAL doc + 1 audit artifact created by tooling) + 2 rerun-only specs (D-05)
**Analogs found:** 3 / 3 (all new/modified artifacts have an exact or strong analog already in repo)

> **TEST/VERIFICATION phase — ~0 production code.** The single new artifact is a Playwright e2e spec.
> Everything else is reuse (helpers) or rerun (online-path specs) or tooling output (audit).
> **CRITICAL path note (planner must fix CONTEXT/RESEARCH drift):** helpers are at
> `tests/helpers/` (NOT `tests/e2e/helpers/`). The inject specs import them as `'../helpers/accounts'`
> and `'../helpers/dbSnapshot'`. The `examTypes`/`examMatrix`/`exam313` helpers ARE under
> `tests/e2e/helpers/` (imported as `'./helpers/examTypes'`). Use the exact import path the analog uses.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `tests/e2e/inject-seakan-online-398.spec.ts` (NEW) | test (e2e spec) | request-response + DB-assert (drive UI → commit → navigate surfaces) | `tests/e2e/inject-assessment-395.spec.ts` (scaffold) + `inject-excel-396.spec.ts` (Excel) + `inject-assessment-397.spec.ts` (seed online sibling, Pre/Post) | exact (same role, same flow, same harness) |
| `tests/e2e/exam-types.spec.ts` (RERUN, no edit) | test (e2e regression) | request-response full-cycle (create→take→grade→cert) | self (FLOW K/L/M/R already cover MA/Essay/Mixed/Cert) | exact (reuse as-is, D-05ii) |
| `tests/e2e/exam-taking.spec.ts` (RERUN, no edit) | test (e2e regression) | request-response full lifecycle (Flow A MC + review + cert) | self (Flow A) | exact (reuse as-is, D-05ii) |
| `docs/SEED_JOURNAL.md` (MODIFIED, append rows) | doc (audit trail) | append-only log | existing 397-04 / 395-03 rows | exact (copy row format) |
| `.planning/v32.2-MILESTONE-AUDIT.md` (CREATED by tooling) | doc (milestone audit) | tooling output | prior `v29/v30/v31-MILESTONE-AUDIT.md` | n/a (produced by `/gsd-audit-milestone`, D-06) |

**Downstream-surface assertion analogs** (these inform the NEW spec's `page.goto('/CMP/...')` blocks, not new files):
- Records row + "Assessment Online" label → `tests/e2e/cmp-records-346.spec.ts:65-94` + `Views/CMP/Records.cshtml:138,197-209`
- Results per-soal Benar/Salah + empty-state negative → `tests/e2e/exam-types.spec.ts:670-685` (FLOW N) + `Views/CMP/Results.cshtml:320-416`
- Cert PDF download → `tests/e2e/helpers/examTypes.ts:708-729` (`verifyCertificatePdfDownload`) + `tests/e2e/exam-types.spec.ts:1246-1254` (FLOW R4)

---

## Pattern Assignments

### `tests/e2e/inject-seakan-online-398.spec.ts` (test, request-response + DB-assert) — NEW, Wave 0

**Primary analog:** `tests/e2e/inject-assessment-395.spec.ts` (snapshot/restore + wizard-drive + commit + DB-assert scaffold).
**Secondary analogs:** `inject-excel-396.spec.ts` (Excel mode, exceljs fresh build, download verify), `inject-assessment-397.spec.ts` (seed online sibling for D-03 parity, Pre/Post link).

#### Imports + harness header (copy from 395:13-20)
```typescript
// Source: tests/e2e/inject-assessment-395.spec.ts:13-20
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';      // tests/helpers/accounts.ts (NOT tests/e2e/helpers)
import * as db from '../helpers/dbSnapshot';         // tests/helpers/dbSnapshot.ts

test.describe.configure({ mode: 'serial' });          // WAJIB serial (shared SQLEXPRESS + --workers=1)

const TS = Date.now();
let snapshotPath = '';
```
For Excel scenario add (from 396:19-22): `import * as path/os/fs` + `import ExcelJS from 'exceljs';`

#### Login helper (copy verbatim from 395:22-31)
```typescript
// Source: tests/e2e/inject-assessment-395.spec.ts:22-31
async function loginAdmin(page: Page) {
  const { email, password } = accounts.admin;        // admin@pertamina.com / 123456
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}
```

#### Snapshot/restore lifecycle (copy verbatim from 395:94-115 — CLAUDE.md Seed Workflow WAJIB)
```typescript
// Source: tests/e2e/inject-assessment-395.spec.ts:94-115 (identical in 396:137-157, 397:76-99)
test.beforeAll(async () => {
  const dirRaw = await db.queryString(
    `SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))`  // C:\Temp blocked
  );
  const dir = dirRaw.replace(/\\+$/, '').replace(/\\/g, '/');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre398-${new Date().toISOString().replace(/[:.]/g, '-')}.bak`;
  await db.backup(snapshotPath);
});
test.afterAll(async () => {
  if (!snapshotPath) return;
  try {
    await db.restore(snapshotPath);                  // restore on success OR failure
  } catch (e) {
    console.error('[inject-398] RESTORE GAGAL — restore manual:', e);
    throw e;
  }
});
```

#### Wizard-drive → Step-5 (copy `fillToStep5` from 395:48-67; essay variant 395:78-92)
```typescript
// Source: tests/e2e/inject-assessment-395.spec.ts:48-67
const WORKER_EMAILS = ['rino.prasetyo@pertamina.com', 'iwan3@pertamina.com']; // ber-NIP (controller skip null-NIP)
async function fillToStep5(page: Page, title: string, workerCount: number) {
  await page.fill('#Title', title);
  await page.selectOption('#Category', { index: 1 });
  await page.click('#btnNext1');
  await expect(page.locator('#step-2')).toBeVisible();
  for (let i = 0; i < workerCount; i++) {
    await page.locator(`#userCheckboxContainer .user-check-item[data-email="${WORKER_EMAILS[i]}"] .user-checkbox`).check();
  }
  await page.click('#btnNext2');
  await expect(page.locator('#step-3')).toBeVisible();
  await authorMcQuestion(page, 'Soal MC 1 — ' + title);   // 395:34-41 selectOption #QuestionType, #correct_A, #injAddQuestionBtn
  await page.click('#btnNext3');
  await expect(page.locator('#step-4')).toBeVisible();
  await page.click('#btnNext4');
  await expect(page.locator('#step-5')).toBeVisible();
  await expect(page.locator('#step5Body')).toBeVisible();
}
```

#### Core pattern — commit + DB-assert + NEW: navigate downstream surfaces
**395 stops at DB-assert. 398's GAP = the `page.goto('/CMP/...')` blocks AFTER commit.** Combine 395's commit (below) with the surface assertions in the next 3 blocks.
```typescript
// Source COMMIT: tests/e2e/inject-assessment-395.spec.ts:166-189
const title = 'ZZ Inject 398 Form ' + TS;
const titleSql = title.replace(/'/g, "''");
// ... answer radios + #AnswersJson check (395:152-164 anti silent-grade-0) ...
await page.click('#btnNext5');
await expect(page.locator('#step-6')).toBeVisible();
await page.click('#btnInject');
await page.waitForLoadState('load');
await expect(page.locator('.alert-success').filter({ hasText: /Inject berhasil/i }).first())
  .toBeVisible({ timeout: 10_000 });
const sessionId = await db.queryScalar(
  `SELECT TOP 1 Id FROM AssessmentSessions WHERE Title='${titleSql}' ORDER BY Id DESC`);
```

#### D-02a — Records row + "Assessment Online" label (NEW surface, analog cmp-records-346)
```typescript
// Source: tests/e2e/cmp-records-346.spec.ts:65-94 + Views/CMP/Records.cshtml:138,197-209
//   Label set server-side in Services/WorkerDataService.cs:47 RecordType="Assessment Online"
//   (no IsManualEntry filter; includes Completed + PendingGrading rows — WorkerDataService.cs:33).
await page.goto('/CMP/Records');                       // worker's own records (login as the worker)
const row = page.locator('table tr', { hasText: title });
await expect(row.first()).toBeVisible();
await expect(row.first()).toContainText('Assessment Online');  // identical to online; NO inject marker
// Worker Detail variant (manager/L3): /CMP/RecordsWorkerDetail?workerId=${uid} (cmp-records-346:99-101)
```

#### D-02b/c — Results per-soal Benar/Salah + Elemen Teknis (NEW surface, analog FLOW N + Results.cshtml)
```typescript
// Source negative-assert: tests/e2e/exam-types.spec.ts:670-685 (FLOW N)
// Source DOM anchors: Views/CMP/Results.cshtml:320 "Tinjauan Jawaban" card, :342 Benar (.bi-check-circle-fill),
//   :348 Salah (.bi-x-circle-fill), :336 "Menunggu Penilaian" (.bi-hourglass-split — essay MUST NOT show this),
//   :207-254 "Analisis Elemen Teknis" table, :416 empty-state "Tinjauan jawaban tidak tersedia".
// Route: Url.Action("Results","CMP",{id}) => /CMP/Results/{id} (Records.cshtml:198); controller Results(int id)
//   (CMPController.cs:2184) accepts both /CMP/Results/{id} and /CMP/Results?id={id}.
await page.goto(`/CMP/Results/${sessionId}`);
// NOT empty-state (gating: needs UserPackageAssignment + PackageUserResponses + AllowAnswerReview=true)
await expect(page.locator('text=Tinjauan jawaban tidak tersedia')).toHaveCount(0);
await expect(page.locator('.card', { hasText: /^Tinjauan Jawaban/ })).toBeVisible();
// per-soal verdict rendered (at least one Benar/Salah badge)
await expect(page.locator('.bi-check-circle-fill, .bi-x-circle-fill').first()).toBeVisible();
// D-02c breakdown elemen teknis (only if authored — guard with count>0 or author one in the scenario)
await expect(page.getByText('Analisis Elemen Teknis')).toBeVisible();
```

#### D-04 essay (§13 risk) — Status=Completed NOT PendingGrading + per-soal essay rendered
```typescript
// Essay scenario MUST prove: NOT stuck "Menunggu Penilaian".
const status = await db.queryString(`SELECT Status FROM AssessmentSessions WHERE Id=${sessionId}`);
expect(status).toBe('Completed');                     // NOT 'Menunggu Penilaian'/'PendingGrading'
await page.goto(`/CMP/Results/${sessionId}`);
await expect(page.locator('.bi-hourglass-split')).toHaveCount(0);  // no pending badge (Results.cshtml:336)
// essay text + score visible in review (not pending badge). Author essay via 395:70-75 authorEssayQuestion.
```

#### D-02d — Cert PDF download (NEW surface, analog examTypes helper)
```typescript
// Source: tests/e2e/helpers/examTypes.ts:708-729 verifyCertificatePdfDownload + exam-types.spec.ts:1246-1254
//   Endpoint /CMP/CertificatePdf/{id} = SAME as online (CMPController.cs:1943, QuestPDF). Inline via page.request:
const resp = await page.request.get(`/CMP/CertificatePdf/${sessionId}`);
expect(resp.status()).toBe(200);
expect(resp.headers()['content-type']).toMatch(/^application\/pdf/i);
const body = await resp.body();
expect(body.length).toBeGreaterThan(1024);            // real PDF, not empty
// (alt: page.waitForEvent('download') on a Results/Certificate button, RESEARCH §Code Examples)
```

#### Excel scenario (D-04 Excel) — build .xlsx FRESH (copy from 396:108-135 + 226-228)
```typescript
// Source: tests/e2e/inject-excel-396.spec.ts:104-135 buildUploadXlsx + 96-102 workerNip
// sheet "Jawaban"; row1=header (ignored), row2=worker (col1=NIP, col2=Nama, col3+=jawaban).
// DO NOT round-trip ClosedXML output (exceljs.readFile incompat). Toggle via #step5MethodExcel (396:79-83).
const nip = await db.queryString(`SELECT TOP 1 NIP FROM Users WHERE Email='${WORKER_EMAILS[0]}'`); // Users, NOT AspNetUsers
const filled = await buildUploadXlsx('ok-398', nip, { 3: 'A', 4: 10 });
await page.setInputFiles('#step5ExcelFile', filled);
await page.click('#btnUploadExcel');
await expect(page.locator('#step5ExcelPreview')).toBeVisible({ timeout: 15_000 });
// then #btnNext5 → #btnInject (same commit path) → navigate downstream surfaces (above).
```

#### D-03 side-by-side parity — seed online sibling (copy from 397:48-65)
```typescript
// Source: tests/e2e/inject-assessment-397.spec.ts:48-65 seedOnlinePostRoom (execScript INSERT IsManualEntry=0)
// ⚠ RESEARCH Assumption A1 + Open Q1: a MINIMAL SQL seed (no package/assignment/responses) is enough for
//   RECORDS-row parity (both show "Assessment Online", no distinguishing marker) but NOT for RESULTS per-soal
//   render (Results gates on UserPackageAssignment+PackageUserResponses+package — CMPController.cs:2212-2217).
//   For Results-level parity, drive 1 LIGHT online exam via UI (1 MC, take, submit) as the comparator,
//   OR limit D-03 parity to Records-row level. PLANNER DECISION (see RESEARCH Open Q1).
async function seedOnlineSibling(): Promise<void> {
  const sql = `SET NOCOUNT ON; INSERT INTO AssessmentSessions (UserId, Title, Category, ... IsManualEntry, ...)
               VALUES ('${RINO_ID}', '${ONLINE_TITLE_SQL}', ... 0, ...);`;  // full column list at 397:51-60
  const tmp = path.join(os.tmpdir(), `seed-398-${TS}.sql`);
  fs.writeFileSync(tmp, sql, 'utf8');
  try { await db.execScript(tmp); } finally { fs.unlinkSync(tmp); }
}
// Parity assert: in /CMP/Records, BOTH the inject row and the online row show "Assessment Online" with NO
// attribute/badge/text that distinguishes inject from online (load-bearing INJ-13 "tak bisa dibedakan").
```

---

### `tests/e2e/exam-types.spec.ts` + `exam-taking.spec.ts` (test, regression) — RERUN ONLY (D-05ii)

**Analog:** self. **DO NOT rewrite — rerun the existing specs.** They already drive the full online path:

| Spec / FLOW | What it proves online-path is intact | Source lines |
|-------------|--------------------------------------|--------------|
| `exam-types.spec.ts` FLOW K (MA) | create wizard → package → 2 MA → take (DOM-text post-shuffle) → submit → DB Score=100, Status Completed | `exam-types.spec.ts:179-303` |
| `exam-types.spec.ts` FLOW L (Essay+grade) | create → essay → take → submit PendingGrading → HC grade 80 → finalize → DB Score=80 Completed | `exam-types.spec.ts:305-428` |
| `exam-types.spec.ts` FLOW M (Mixed MC+MA+Essay) | full mixed cycle → grade essay → DB total 100 Completed | `exam-types.spec.ts:430-586` |
| `exam-types.spec.ts` FLOW R (Cert) | create GenerateCertificate=true → take correct → CertificatePdf download (>1024 bytes) + NomorSertifikat populated | `exam-types.spec.ts:1151-1269` |
| `exam-taking.spec.ts` Flow A (MC lifecycle) | create→3 MC→start→answer→submit→Results→answer-review→certificate | `exam-taking.spec.ts:37-...` |

**Run command (D-05ii):**
```bash
cd tests && npx playwright test e2e/exam-types.spec.ts e2e/exam-taking.spec.ts --workers=1
```
> RESEARCH Assumption A3: baseline these FIRST (before concluding regression) — if a spec is pre-flaky,
> the failure is not caused by inject. These use `helpers/auth.ts` `login(page, 'hc'|'coachee')` +
> `./helpers/examTypes` (NOT the inject specs' `loginAdmin`/`accounts` pattern). Different login helper — leave as-is.

---

### `docs/SEED_JOURNAL.md` (doc, append-only) — MODIFIED

**Analog:** existing `397-04` row (`docs/SEED_JOURNAL.md:9`) and `395-03` row (`docs/SEED_JOURNAL.md:11`).
**Copy the row format exactly** (table columns: Tanggal | Phase | Klasifikasi | Tujuan | Dampak | Snapshot file | Status):
```
| 2026-06-18 | 398 (e2e INJ-13 seakan-online downstream parity) | temporary + local-only | <tujuan per skenario> | AssessmentSessions(+N prefix 'ZZ … 398') + Packages/Questions/Options/Assignments/Responses/AuditLogs(+N) + NomorSertifikat; online sibling D-03 IsManualEntry=0. Semua revert via RESTORE. Data/SeedData.cs tak tersentuh. | {SQL InstanceDefaultBackupPath}/HcPortalDB_Dev-pre398-{ts}.bak | cleaned (2026-06-18, afterAll RESTORE OK; COUNT 'ZZ %398%'=0 verified) |
```
**Klasifikasi WAJIB `temporary + local-only`. Status starts `active`, flip to `cleaned` only after restore verified.**

---

### `.planning/v32.2-MILESTONE-AUDIT.md` (doc) — CREATED BY TOOLING (D-06)

Not authored by hand. Produced by `/gsd-audit-milestone v32.2` (aggregates `393-398/*VERIFICATION.md`, traces INJ-01..INJ-13 = 13/13). Analog: prior `v29/v30/v31-MILESTONE-AUDIT.md`. RESEARCH Assumption A2: path may vary slightly; artifact is created regardless.

---

## Shared Patterns

### Snapshot/Restore (CLAUDE.md Seed Workflow) — apply to EVERY DB-writing scenario
**Source:** `tests/helpers/dbSnapshot.ts` (`backup`/`restore`/`queryScalar`/`queryString`/`execScript`)
- `backup(path)` — `BACKUP ... WITH INIT, FORMAT` (dbSnapshot.ts:67-70)
- `restore(path)` — `SINGLE_USER WITH ROLLBACK IMMEDIATE` → `RESTORE ... WITH REPLACE` → `MULTI_USER` (dbSnapshot.ts:80-99)
- `queryScalar(sql)` returns int; `queryString(sql)` returns first non-empty line (dbSnapshot.ts:116-156)
- Built-in **non-localhost guard** (dbSnapshot.ts:39-44) — never targets remote SQL.
**Apply to:** beforeAll backup + afterAll restore in the 398 spec; restore runs on success OR failure (try/finally).

### Login + accounts — apply to all scenarios
**Source:** `tests/helpers/accounts.ts:1-12` (`accounts.admin` = admin@pertamina.com/123456; `accounts.coachee` = rino.prasetyo; `accounts.coachee2` = iwan3). Inject specs use a local `loginAdmin(page)` (395:22-31). Online-regression specs use `helpers/auth.ts` `login(page, roleKey)`.

### Anti silent-grade-0 (preview == commit) — apply to every commit
**Source:** `inject-assessment-395.spec.ts:152-164` (assert `window.injBuildWorkerAnswers()` non-empty before submit), `inject-excel-396.spec.ts:241-243` (`window.injExcelAnswersCache` not `'[]'`). After commit, assert DB `Score == preview` (395:182-189, 396:257-265). **398 must NOT re-prove grading depth (already in 395/396/397) — but keep the non-empty + non-zero sanity assert so a broken commit can't pass silently.**

### Worker NIP / Identity table — apply to Excel + any NIP lookup
**Source:** `inject-excel-396.spec.ts:96-102` — Identity table is **`Users`** (NOT `AspNetUsers`) in this project: `SELECT TOP 1 NIP FROM Users WHERE Email=...`.

### Run constraints (environment correctness — the real risk)
- `--workers=1` always (shared SQLEXPRESS + serial state; `playwright.config.ts:8` `fullyParallel:false`).
- Server `dotnet run` from **MAIN tree** with `Authentication__UseActiveDirectory=false` (Razor embedded at build — `tests/e2e/inject-*.spec.ts` header note + STATE.md:57). NEVER run e2e from worktree sibling.
- `baseURL: http://localhost:5277` (`playwright.config.ts:12`). `globalTeardown` also restores (`playwright.config.ts:5`).
- If login 500 / SQL conn fail: start SQLBrowser + `lpc:` shared-memory override (memory `reference_local_e2e_sql_env_fix`).

---

## No Analog Found

None. Every artifact maps to an existing pattern.

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none) | — | — | 100% reuse phase — RESEARCH §Don't Hand-Roll confirms all infra exists |

> **One nuance (not a missing analog, a planner decision):** D-03 *Results-level* parity has no direct
> seed analog deep enough — `seedOnlinePostRoom` (397:48-65) seeds a session WITHOUT package/assignment/
> responses, which is sufficient for Records-row parity but NOT Results per-soal render (gating at
> CMPController.cs:2212-2217). For Results-level parity, either drive a LIGHT online exam via UI (1 MC,
> reuse `exam-types` helpers) OR scope D-03 parity to Records-row. See RESEARCH Open Q1 + Assumption A1.

---

## Metadata

**Analog search scope:** `tests/e2e/`, `tests/helpers/`, `tests/e2e/helpers/`, `Views/CMP/`, `Controllers/CMPController.cs`, `Services/WorkerDataService.cs`, `docs/SEED_JOURNAL.md`, `tests/playwright.config.ts`
**Files scanned:** 11 (4 inject specs, 2 online specs, cmp-records-346, 3 helpers, Results.cshtml, Records.cshtml, WorkerDataService.cs, CMPController.cs grep, SEED_JOURNAL.md, playwright.config.ts)
**Pattern extraction date:** 2026-06-18
