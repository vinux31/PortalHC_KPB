# Phase 316: Fix SubmitExam page-closed bug + matrix test infra polish - Pattern Map

**Mapped:** 2026-05-11
**Files analyzed:** 3 (2 modify + 1 verify-only)
**Analogs found:** 3 / 3 (semua punya analog konkret di repo)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `tests/e2e/helpers/examMatrix.ts` (modify lines 96-160) | test-helper | request-response (Playwright page action + redirect) | `tests/e2e/helpers/exam313.ts:39, 61-66, 86-89, 106-108` | exact (Phase 313.1 race-tolerant precedent) |
| `tests/e2e/helpers/matrixReport.ts` (modify lines 198-227 + 274-287) | test-report-utility | file-I/O (screenshot capture + disk existsSync probe + markdown render) | `tests/e2e/helpers/matrixReport.ts:121-184` (existing `Collector.flush` file-system pattern) | role-match (same file, internal precedent for fs ops) |
| `tests/e2e/assessment-matrix.spec.ts` (verify-only, no edit) | test-spec consumer | event-driven (catch SkipScenarioError → continue) | `tests/e2e/assessment-matrix.spec.ts:154-159` (existing catch boundary) | exact (self-reference; no edit needed) |

## Pattern Assignments

### `tests/e2e/helpers/examMatrix.ts` (test-helper, request-response)

**Analog:** `tests/e2e/helpers/exam313.ts` — Phase 313.1 race-tolerant precedent

**Imports pattern** (current file, lines 22-26 — no change needed):
```typescript
import { Page, expect } from '@playwright/test';
import { login } from '../../helpers/auth';
import type { AccountKey } from '../../helpers/accounts';
import type { ScenarioConfig, QuestionConfig } from './matrixTypes';
import { softAssert, SkipScenarioError } from './matrixReport';
```
**Pattern note:** `SkipScenarioError` sudah ter-import (line 26) — siap pakai untuk page-closed gate throw. Tidak butuh tambah import.

---

**Core pattern A — Promise.all race-tolerant submit** (analog `exam313.ts:60-66, 85-90`):

Reference (precedent — Phase 313.1 `assertTier1Reject`):
```typescript
// exam313.ts:61-66
await Promise.race([
  page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 30_000 }).catch(() => {}),
  expect(page.locator('.alert-danger, .alert-warning').first())
    .toContainText(/Waktu ujian.*habis|Server menolak submit|Submit gagal/, { timeout: 30_000 })
    .catch(() => {}),
]);
```

Reference (precedent — Phase 313.1 navigation arm-before-fire `exam313.ts:37-40`):
```typescript
await resumeLink.click();
// Lands at StartExam (alive) atau ExamSummary (server-side redirect timer-expired).
await page.waitForURL(/\/CMP\/(StartExam|ExamSummary)\/\d+/, { timeout: 10_000 });
```

**Current bug location** (`examMatrix.ts:150-159`):
```typescript
await softAssert(
  { scenario: cfg, step: 'submit-exam', severity: 'critical', page },
  async () => {
    // Submit button — id #reviewSubmitBtn (review modal) atau direct [type="submit"]
    // (Controllers/CMPController.cs:1569 SubmitExam form binding).
    await page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)');
    await page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 });
  },
  'SubmitExam redirects to /CMP/Results/{id}'
);
```

**Target pattern after fix** (per D-01, D-03):
```typescript
await softAssert(
  { scenario: cfg, step: 'submit-exam', severity: 'critical', page },
  async () => {
    // Phase 316 fix: arm waitForURL BEFORE click fires navigate.
    // Race-tolerant per Phase 313.1 precedent (exam313.ts:39, 107).
    // Order matters: waitForURL index 0 (listener arm sync), click index 1 (action fire).
    await Promise.all([
      page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
      page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'),
    ]);
  },
  'SubmitExam redirects to /CMP/Results/{id}'
);
```

**Critical ordering constraint:** `waitForURL` HARUS di array index 0. Reverse order = bug-equivalent (lihat Pitfall 1 di RESEARCH.md:368-375).

---

**Core pattern B — page.isClosed() defensive gate** (no codebase precedent — new pattern):

**Placement:** Awal setiap softAssert callback body di MC (line 104), MA (line 118), Essay (line 135). Per D-06 throw SkipScenarioError langsung — bypass softAssert severity='major' continue-logic.

Current MC step (lines 102-113):
```typescript
await softAssert(
  { scenario: cfg, step: `mc-q${q.id}`, severity: 'major', page },
  async () => {
    // Radio click — change handler invoke SaveAnswer endpoint (server-side persist).
    await page.check(`input.exam-radio[data-question-id="${q.id}"][value="${optId}"]`);
    await page
      .locator(`#saveIndicatorText`)
      .filter({ hasText: /saved|tersimpan/i })
      .waitFor({ timeout: 5_000 });
  },
  `MC q${q.id} optionId=${optId} saved`
);
```

**Target pattern after fix** (per D-06):
```typescript
await softAssert(
  { scenario: cfg, step: `mc-q${q.id}`, severity: 'major', page },
  async () => {
    // Phase 316: page-closed gate — abort cascade saat submit-exam (langkah sebelumnya) sudah
    // close context. SkipScenarioError di-rethrow oleh softAssert catch handler (see matrixReport.ts).
    if (page.isClosed()) {
      throw new SkipScenarioError(`page closed before mc-q${q.id} step — cascade abort`);
    }
    await page.check(`input.exam-radio[data-question-id="${q.id}"][value="${optId}"]`);
    await page
      .locator(`#saveIndicatorText`)
      .filter({ hasText: /saved|tersimpan/i })
      .waitFor({ timeout: 5_000 });
  },
  `MC q${q.id} optionId=${optId} saved`
);
```

**Apply identical isClosed gate pattern ke:**
- MA softAssert callback (lines 116-128) — message `page closed before ma-q${q.id} step — cascade abort`
- Essay softAssert callback (lines 133-145) — message `page closed before essay-q${q.id} step — cascade abort`

**Coupling note:** Pattern bekerja HANYA kalau softAssert catch handler (matrixReport.ts:203-205) di-amend dengan `if (e instanceof SkipScenarioError) throw e;` re-throw branch BEFORE generic error handling (lihat Pitfall 5 RESEARCH.md:400-407 + Open Question 2 RESEARCH.md:486-489).

---

**Error handling pattern** (current — preserved):

softAssert wrapper sudah handle try/catch + finding record + screenshot path emit. Tidak ada perubahan handler di sisi helper. Yang berubah cuma callback body content (Promise.all + isClosed gate).

---

**Docstring update** (per "Claude's Discretion" CONTEXT line 53):

Tambahkan reference Phase 316 fix di JSDoc header `takeExam`:
```typescript
/**
 * Peserta login + buka StartExam + jawab semua questions + Submit.
 *
 * Phase 316 fix:
 * - Submit click memakai `Promise.all([waitForURL, click])` race-tolerant
 *   (precedent exam313.ts:107). Eliminate "Target page, context or browser has been closed"
 *   regression dari Phase 315 smoke run 2026-05-11T06:14:36Z.
 * - `page.isClosed()` gate di awal setiap softAssert callback (MC/MA/Essay) — throw
 *   SkipScenarioError langsung saat page closed mid-loop. Cegah cascade-fail noise di report.
 *
 * [original JSDoc continues...]
 */
```

---

### `tests/e2e/helpers/matrixReport.ts` (test-report-utility, file-I/O)

**Analog:** Same file `Collector.flush` (lines 121-184) — existing precedent untuk fs operations + best-effort cleanup pattern.

**Imports pattern** (current lines 21-24, needs extension):
```typescript
import { Page } from '@playwright/test';
import { writeFile, mkdir, readdir, readFile, unlink, appendFile } from 'fs/promises';
import { dirname, resolve, join } from 'path';
import type { Finding, Severity, ScenarioConfig } from './matrixTypes';
```

**Target after fix** (add `existsSync` + sync `readdirSync` untuk render-time disk probe — async tidak applicable di `renderFinding` yang sinkron):
```typescript
import { Page } from '@playwright/test';
import { writeFile, mkdir, readdir, readFile, unlink, appendFile } from 'fs/promises';
import { existsSync, readdirSync } from 'fs';
import { dirname, resolve, join } from 'path';
import type { Finding, Severity, ScenarioConfig } from './matrixTypes';
```
**Rationale:** `renderFinding` (line 274) currently sinkron. Convert ke async = blast radius besar (touch flush + caller chain). Pakai sync fs probe — minimal change per D-12.

---

**Core pattern A — softAssert SkipScenarioError re-throw branch** (current bug location `matrixReport.ts:198-227`):

Current handler (lines 198-227):
```typescript
export async function softAssert<T>(
  ctx: { scenario: ScenarioConfig; step: string; severity: Severity; page: Page; isMeta?: boolean },
  fn: () => Promise<T>,
  expected: string
): Promise<T | null> {
  try {
    return await fn();
  } catch (e: unknown) {
    const err = e as { message?: string };
    const stepSlug = ctx.step.replace(/\s+/g, '-').replace(/[^a-zA-Z0-9-]/g, '');
    const screenshotPath = `test-results/matrix-s${ctx.scenario.id}-${stepSlug}.png`;
    await ctx.page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});

    await collector.record({ /* ... */ });

    if (ctx.severity === 'critical') {
      throw new SkipScenarioError(`Critical at ${ctx.step}: ${err?.message ?? String(e)}`);
    }
    return null;
  }
}
```

**Target pattern after fix** (per D-09 + Pitfall 5 mitigation):
```typescript
export async function softAssert<T>(
  ctx: { scenario: ScenarioConfig; step: string; severity: Severity; page: Page; isMeta?: boolean },
  fn: () => Promise<T>,
  expected: string
): Promise<T | null> {
  try {
    return await fn();
  } catch (e: unknown) {
    // Phase 316: re-throw SkipScenarioError tanpa record finding — helper sudah signal
    // explicit skip (page-closed cascade abort). Tanpa branch ini, isClosed gate akan
    // di-swallow oleh severity='major' continue-logic → cascade noise tetap muncul.
    if (e instanceof SkipScenarioError) {
      throw e;
    }

    const err = e as { message?: string };
    const stepSlug = ctx.step.replace(/\s+/g, '-').replace(/[^a-zA-Z0-9-]/g, '');
    const candidatePath = `test-results/matrix-s${ctx.scenario.id}-${stepSlug}.png`;

    // Phase 316: defensive screenshot capture — page-closed pre-check + try/catch.
    // Page closed → skip custom path (jangan throw, jangan retry); renderer fallback
    // ke Playwright auto-capture di renderFinding.
    let screenshotPath: string | undefined;
    if (!ctx.page.isClosed()) {
      try {
        await ctx.page.screenshot({ path: candidatePath, fullPage: true });
        screenshotPath = candidatePath;  // only set on successful write
      } catch {
        // Page may have closed antara isClosed() check dan screenshot fire (microsec race).
        // Skip silent — renderer fallback handles missing custom path.
      }
    }

    await collector.record({
      scenarioId: ctx.scenario.id,
      scenarioTitle: ctx.scenario.title,
      step: ctx.step,
      expected,
      actual: err?.message ?? String(e),
      screenshotPath,  // may be undefined → renderFinding fallback ke auto-capture
      severity: ctx.severity,
      isMeta: ctx.isMeta,
    });

    if (ctx.severity === 'critical') {
      throw new SkipScenarioError(`Critical at ${ctx.step}: ${err?.message ?? String(e)}`);
    }
    return null;
  }
}
```

---

**Core pattern B — renderFinding fallback screenshot resolution** (current bug location `matrixReport.ts:274-287`):

Current renderer:
```typescript
function renderFinding(f: Finding): string {
  const lines = [
    `### Scenario ${f.scenarioId}: ${f.scenarioTitle} — ${f.step}`,
    ``,
    `- **Severity:** ${f.severity}`,
    `- **Expected:** ${f.expected}`,
    `- **Actual:** ${f.actual}`,
  ];
  if (f.screenshotPath) {
    lines.push(`- **Screenshot:** \`${f.screenshotPath}\``);
  }
  lines.push(`- **Hypothesis:** ${deriveHypothesis(f)}`);
  return lines.join('\n');
}
```

**Target pattern after fix** (per D-11 fallback strategy):
```typescript
/**
 * Phase 316 fallback strategy:
 * 1. Kalau `f.screenshotPath` di-set DAN file exists → emit custom path.
 * 2. Kalau missing OR undefined → scan Playwright auto-capture dir
 *    (`test-results/assessment-matrix-Scenario-*/test-failed-*.png`), emit first match.
 *    Best-effort — slug ke scenario.id correlation tidak guaranteed (Pitfall 3).
 * 3. Kalau juga tidak ada → omit Screenshot line (no dead link di markdown).
 *
 * Forward-slash path enforced — Windows backslash di markdown link broken di GitHub viewer
 * (Pitfall 4). String concat, JANGAN path.join.
 */
function resolveScreenshotPath(f: Finding): string | undefined {
  // Layer 1: custom path exists.
  if (f.screenshotPath) {
    const abs = resolve(f.screenshotPath);
    if (existsSync(abs)) {
      return f.screenshotPath;
    }
  }

  // Layer 2: Playwright auto-capture fallback. Scan FINDINGS_DIR untuk
  // dir berawalan 'assessment-matrix-Scenario-' lalu cari 'test-failed-*.png' di dalamnya.
  // [ASSUMED A1] naming pattern observed di smoke run 315-UAT.md:24 — Wave 0 verify.
  try {
    if (!existsSync(FINDINGS_DIR)) return undefined;
    const subdirs = readdirSync(FINDINGS_DIR, { withFileTypes: true })
      .filter((d) => d.isDirectory() && d.name.startsWith('assessment-matrix-Scenario-'));
    for (const d of subdirs) {
      const inner = readdirSync(join(FINDINGS_DIR, d.name))
        .filter((n) => n.startsWith('test-failed-') && n.endsWith('.png'));
      if (inner.length > 0) {
        // Forward-slash output untuk markdown viewer compat.
        return `test-results/${d.name}/${inner[0]}`;
      }
    }
  } catch {
    // fs probe error — fall through to undefined (no Screenshot line).
  }
  return undefined;
}

function renderFinding(f: Finding): string {
  const lines = [
    `### Scenario ${f.scenarioId}: ${f.scenarioTitle} — ${f.step}`,
    ``,
    `- **Severity:** ${f.severity}`,
    `- **Expected:** ${f.expected}`,
    `- **Actual:** ${f.actual}`,
  ];
  const resolvedPath = resolveScreenshotPath(f);
  if (resolvedPath) {
    lines.push(`- **Screenshot:** \`${resolvedPath}\``);
  }
  lines.push(`- **Hypothesis:** ${deriveHypothesis(f)}`);
  return lines.join('\n');
}
```

---

**Error handling pattern** (analog existing pattern in this file, lines 90-98 + 148-154):

Best-effort try/catch dengan console.warn, jangan halt eksekusi. Pattern dari `Collector.record`:
```typescript
try {
  await mkdir(FINDINGS_DIR, { recursive: true });
  await appendFile(workerFindingsFile(), JSON.stringify(f) + '\n', 'utf-8');
} catch (e) {
  // Jangan halt test execution kalau write findings file gagal — log saja.
  console.warn(`[matrixReport] gagal append findings file (worker boundary): ${(e as Error)?.message ?? e}`);
}
```

**Apply ke screenshot capture dan fallback resolve:** swallow exception, no halt. Sama style.

---

**Type definitions** (per D-12 — `matrixTypes.ts` touch HANYA kalau wajib):

Inspect `Finding.screenshotPath`. Kalau already typed `string | undefined`, no change. Kalau typed wajib `string`, ubah jadi optional `string | undefined`. Planner verify saat implement.

---

### `tests/e2e/assessment-matrix.spec.ts` (verify-only, no edit)

**Analog:** Self-reference — existing catch boundary di file yang sama.

**Catch boundary pattern** (current lines 154-159 — TARGET-OF-VERIFICATION, no edit):
```typescript
} catch (e) {
  if (e instanceof SkipScenarioError) {
    // Critical fail sudah ter-record di collector via softAssert; lanjut scenario berikut.
    console.log(`[S${cfg.id}] Critical fail — skip sisa step scenario: ${e.message}`);
    return;
  }
  // Unexpected error — let Playwright handle (will fail test, recorded di run statistics).
  throw e;
}
```

**Pattern interpretation untuk Phase 316:**
- isClosed gate di examMatrix throws SkipScenarioError → softAssert re-throws (new branch) → bubble ke `runDiscoveryScenario` → catch boundary line 154 → `return` normal → Playwright test pass → next scenario starts.
- Mechanism sudah valid; Phase 316 cuma menambah CASE penghasil SkipScenarioError. Tidak butuh edit spec.

**Verification only (D-13 + D-14):**
- Run `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` → S5 reach `/CMP/Results/{id}` post Promise.all fix.
- Run `cd tests && npx playwright test assessment-matrix` → S1..S10 all execute, S10 expected-fail (test.fail() inner form), exit 0.

---

## Shared Patterns

### Race-tolerant Promise.all (arm-before-fire)
**Source:** `tests/e2e/helpers/exam313.ts:37-40, 60-66, 85-90, 106-108` (Phase 313.1)
**Apply to:** `examMatrix.ts:155-156` submit step
**Rule:** `waitForURL` listener HARUS arm BEFORE click fires navigate. Array order matters: `Promise.all([waitForURL, click])` not reverse.

```typescript
// Canonical pattern:
await Promise.all([
  page.waitForURL(/pattern/, { timeout: NNN }),  // index 0 = listener arm sync
  page.click(selector),                           // index 1 = action fire
]);
```

### SkipScenarioError untuk continue-on-fail
**Source:** `tests/e2e/helpers/matrixReport.ts:30-35` (class definition) + `tests/e2e/assessment-matrix.spec.ts:154-159` (catch boundary)
**Apply to:** Any helper step yang detect "scenario unrecoverable, skip sisa step, continue ke next scenario."
**Rule:** Throw `SkipScenarioError` explicit. softAssert catch handler MUST re-throw `instanceof SkipScenarioError` tanpa record finding (Phase 316 amendment).

```typescript
// Throw site (helper):
if (page.isClosed()) {
  throw new SkipScenarioError(`page closed before ${step} — cascade abort`);
}

// Re-throw site (softAssert catch handler, Phase 316 amendment):
if (e instanceof SkipScenarioError) {
  throw e;  // bypass severity logic, bubble ke runDiscoveryScenario
}

// Catch boundary site (spec):
} catch (e) {
  if (e instanceof SkipScenarioError) {
    console.log(`[S${cfg.id}] Critical fail — skip sisa step scenario: ${e.message}`);
    return;  // exit normal — Playwright treat sebagai PASSED
  }
  throw e;
}
```

### Best-effort fs operations dengan swallow + warn
**Source:** `tests/e2e/helpers/matrixReport.ts:88-98, 121-184` (Collector.record + flush)
**Apply to:** Screenshot capture (defensive), fallback path resolve, any disk I/O di hot path.
**Rule:** Try/catch dengan console.warn (jangan throw, jangan halt). Test execution > artifact write integrity.

```typescript
try {
  await fsOperation();
} catch (e) {
  // Best-effort — swallow + warn. Test execution must continue.
  console.warn(`[matrixReport] op failed: ${(e as Error)?.message ?? e}`);
}
```

### Forward-slash path emission (Windows compat)
**Source:** `tests/e2e/helpers/matrixReport.ts:208` (existing path string emission)
**Apply to:** Any markdown path emit (Screenshot, Trace, Findings link).
**Rule:** Use string concat dengan `/`, JANGAN `path.join` (Windows emits `\` yang broken di GitHub markdown viewer — lihat Pitfall 4 RESEARCH.md:393-398).

```typescript
// Correct:
return `test-results/${dirName}/${fileName}`;

// Wrong (Windows):
return path.join('test-results', dirName, fileName);  // → "test-results\dir\file"
```

### Source code citation di JSDoc header
**Source:** `tests/e2e/helpers/examMatrix.ts:1-21` (existing pattern)
**Apply to:** Any helper file edit — keep citation block updated kalau referenced line numbers shift.
**Rule:** Cite `Controllers/...:line`, `Views/...:line`, `Hubs/...:line`, atau sibling helper `tests/e2e/helpers/...:line` di header comment.

---

## No Analog Found

| File / Pattern | Role | Data Flow | Reason |
|----------------|------|-----------|--------|
| `page.isClosed()` gate pattern | runtime context check | sync boolean check before async page action | Tidak ada precedent `isClosed()` usage di entire `tests/` directory (grep verified — 0 matches). Pattern baru untuk Phase 316. Mitigasi: pattern dari Playwright docs (RESEARCH.md:163), placement strategy didokumentasikan eksplisit di RESEARCH.md:166-186. |
| Screenshot auto-capture fallback path resolve | render-time disk probe + glob | sync fs read | Tidak ada precedent fallback path resolver di codebase. Pattern baru — adopt `existsSync` + `readdirSync` synchronous (renderFinding sinkron, tidak boleh async). Best-effort fallback per D-11 (Pitfall 3 disclaimers acknowledged). |

---

## Metadata

**Analog search scope:**
- `tests/e2e/helpers/` (5 files — `examMatrix.ts`, `matrixReport.ts`, `matrixTypes.ts`, `exam313.ts`, fixtures)
- `tests/e2e/` (spec files — `assessment-matrix.spec.ts` consumer, other specs untuk pattern precedent)
- Cross-cutting search: `page.isClosed`, `existsSync`, `readdirSync`, `Promise.all`, `Promise.race`

**Files scanned:** 7 (helpers + specs + 1 grep across `tests/`)

**Key analog precedents validated:**
1. `exam313.ts:39, 61-66, 86-89, 106-108` — Promise.all/Promise.race race-tolerant precedent (Phase 313.1) — HIGH confidence
2. `matrixReport.ts:121-184` (`Collector.flush`) — fs operations + swallow pattern — internal precedent
3. `assessment-matrix.spec.ts:154-159` — SkipScenarioError catch boundary — consumer-side mechanism intact

**Patterns BARU (no precedent):**
- `page.isClosed()` defensive gate (RESEARCH.md:160-196)
- Screenshot auto-capture fallback resolver (RESEARCH.md:198-256)

Both new patterns documented di RESEARCH.md dengan canonical implementation + edge cases + assumption flags (A1, A2).

**Pattern extraction date:** 2026-05-11

---

*Phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso*
*Pattern mapping completed: 2026-05-11*
