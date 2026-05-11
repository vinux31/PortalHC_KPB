// Phase 315 — Collector + softAssert + markdown renderer untuk matrix discovery findings.
//
// LIFECYCLE STRATEGY (Plan 05 polish — fix worker-singleton issue):
//   Playwright spawn worker per project (setup project = worker A, chromium project = worker B).
//   Module-level `collector` di matrixReport.ts ter-instantiate di EACH worker process — bukan
//   shared dengan main process yang jalan globalTeardown. Singleton di-memory tidak cukup.
//
//   Solusi: FILE-SYSTEM-BACKED collector.
//   - `record()` di worker process append ke `tests/test-results/matrix-findings-w{workerIndex}.json`
//     (NDJSON-style append, single line JSON per finding) — atomic write per record.
//   - `flush()` di main process scan `tests/test-results/matrix-findings-*.json`, parse semua
//     line, aggregate, render markdown, lalu cleanup file-file tersebut.
//   - In-memory `findings` di-keep untuk worker yang ALSO jadi main (worker 0 di single-worker
//     run) — backward compat dengan Plan 02 lifecycle assumption.
//
// CONTEXT D-06: sentinel `[META-*]` finding (isMeta=true) dipisah dari summary statistik
// discovery — section `## Meta-validation results` terpisah.
//
// Source spec: docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md (commit 94bacecf)

import { Page } from '@playwright/test';
import { writeFile, mkdir, readdir, readFile, unlink, appendFile } from 'fs/promises';
import { dirname, resolve, join } from 'path';
import type { Finding, Severity, ScenarioConfig } from './matrixTypes';

/**
 * Thrown oleh `softAssert` saat severity = 'critical' fail. Caller di spec catch ini
 * untuk skip sisa step skenario, tapi tetap lanjut ke skenario berikutnya.
 */
export class SkipScenarioError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'SkipScenarioError';
  }
}

/**
 * Direktori findings antar-worker (file-system-backed collector).
 * `tests/test-results/` adalah Playwright default output dir — sudah ter-gitignore.
 * Path absolute via __dirname supaya independent dari cwd Playwright runner.
 */
const FINDINGS_DIR = resolve(__dirname, '..', '..', 'test-results');

/** File name pattern per worker — wildcard saat aggregate di flush. */
function workerFindingsFile(): string {
  // process.env.TEST_WORKER_INDEX = workerIndex (set Playwright runtime).
  // Fallback 'main' kalau dipanggil dari main process (globalSetup/teardown context).
  const workerIdx = process.env.TEST_WORKER_INDEX ?? 'main';
  return join(FINDINGS_DIR, `matrix-findings-w${workerIdx}.json`);
}

/**
 * Collector — file-system-backed antar-worker.
 *
 * `record()` di worker process: append 1 baris JSON ke per-worker file (atomic).
 * Juga simpan in-memory copy untuk single-process scenario.
 *
 * `flush()` di main process (globalTeardown): scan semua per-worker file, parse, render
 * markdown, cleanup. In-memory findings ditambahkan juga (fallback kalau main jadi worker 0).
 */
class Collector {
  /** In-memory cache — dipakai kalau process sama (single-worker) atau backup. */
  private findings: Finding[] = [];

  /**
   * Whitelist console error patterns yang aman di-ignore di Layer "no console error".
   * Pattern ini di-load oleh spec page.on('console') handler untuk filter benign noise.
   * Setiap pattern HARUS berkomentar inline supaya jelas asal pattern.
   */
  private consoleErrorWhitelist: RegExp[] = [
    /favicon\.ico/i,                            // benign asset 404 di subpath / browser pre-fetch
    /SignalR.*reconnect/i,                      // benign hub reconnect informational (auto-recovery)
    /Failed to load resource.*manifest\.json/i, // PWA manifest 404 benign — tidak ada manifest di Portal HC
    /DevTools.*download/i,                      // DevTools console hint pas headed mode
    /\.woff2?.*404/i,                           // font asset 404 di subpath base (benign)
    /\[HMR\]/i,                                 // HMR informational (tidak applicable production build tapi muncul kalau Vite dev)
    /preload was not used/i,                    // resource hint informational dari browser
    // CATAT pattern baru hasil smoke run iteration di sini.
  ];

  /**
   * Record finding. Dual-write:
   * 1. In-memory (backward compat — Plan 02 lifecycle assumption single-process).
   * 2. File-system per-worker (Plan 05 fix — worker process boundary).
   *
   * appendFile dengan mkdir defensive (test-results/ mungkin belum exist di first run).
   */
  async record(f: Finding): Promise<void> {
    this.findings.push(f);
    try {
      await mkdir(FINDINGS_DIR, { recursive: true });
      // NDJSON: 1 finding per line — atomic append, parser-friendly.
      await appendFile(workerFindingsFile(), JSON.stringify(f) + '\n', 'utf-8');
    } catch (e) {
      // Jangan halt test execution kalau write findings file gagal — log saja.
      // In-memory copy tetap ter-track sebagai fallback.
      console.warn(`[matrixReport] gagal append findings file (worker boundary): ${(e as Error)?.message ?? e}`);
    }
  }

  count(): number {
    return this.findings.length;
  }

  /** Expose whitelist supaya spec page.on('console') handler bisa filter. */
  getConsoleErrorWhitelist(): RegExp[] {
    return this.consoleErrorWhitelist;
  }

  /**
   * Aggregate semua findings dari per-worker file + in-memory, render markdown.
   * Dipanggil di globalTeardown (Plan 03) — main process context.
   *
   * Sequence:
   *   1. Scan FINDINGS_DIR untuk `matrix-findings-w*.json`.
   *   2. Parse setiap file (NDJSON), accumulate ke `allFindings`.
   *   3. Merge dengan `this.findings` (de-dup via stringify identity check).
   *   4. Render markdown (Discovery + Meta sections).
   *   5. Cleanup per-worker files (best-effort, swallow error).
   */
  async flush(outPath: string): Promise<void> {
    const allFindings: Finding[] = [];
    const seenKeys = new Set<string>();

    // Step 1-3: aggregate dari file-system + in-memory.
    try {
      const files = await readdir(FINDINGS_DIR).catch(() => [] as string[]);
      const findingsFiles = files.filter(
        (n) => n.startsWith('matrix-findings-w') && n.endsWith('.json')
      );
      for (const fname of findingsFiles) {
        const fpath = join(FINDINGS_DIR, fname);
        try {
          const raw = await readFile(fpath, 'utf-8');
          for (const line of raw.split(/\r?\n/)) {
            const trimmed = line.trim();
            if (!trimmed) continue;
            try {
              const f = JSON.parse(trimmed) as Finding;
              const key = `${f.scenarioId}|${f.step}|${f.actual}`;
              if (seenKeys.has(key)) continue;
              seenKeys.add(key);
              allFindings.push(f);
            } catch (parseErr) {
              console.warn(`[matrixReport.flush] skip malformed line di ${fname}: ${(parseErr as Error)?.message ?? parseErr}`);
            }
          }
        } catch (e) {
          console.warn(`[matrixReport.flush] gagal baca ${fpath}: ${(e as Error)?.message ?? e}`);
        }
      }
    } catch (e) {
      console.warn(`[matrixReport.flush] scan FINDINGS_DIR gagal: ${(e as Error)?.message ?? e}`);
    }
    // Merge in-memory dgn dedup.
    for (const f of this.findings) {
      const key = `${f.scenarioId}|${f.step}|${f.actual}`;
      if (seenKeys.has(key)) continue;
      seenKeys.add(key);
      allFindings.push(f);
    }

    // Step 4: render.
    const discovery = allFindings.filter((f) => !f.isMeta);
    const meta = allFindings.filter((f) => f.isMeta);
    const md = renderReport(discovery, meta);
    await mkdir(dirname(outPath), { recursive: true });
    await writeFile(outPath, md, 'utf-8');

    // Step 5: cleanup per-worker findings (best-effort).
    try {
      const files = await readdir(FINDINGS_DIR).catch(() => [] as string[]);
      for (const fname of files) {
        if (fname.startsWith('matrix-findings-w') && fname.endsWith('.json')) {
          await unlink(join(FINDINGS_DIR, fname)).catch(() => {});
        }
      }
    } catch {
      // Swallow — cleanup non-critical.
    }

    // Update in-memory count supaya logger di teardown report total benar.
    this.findings = allFindings;
  }
}

/** Module-level singleton — single instance per process (file-system-backed antar-worker). */
export const collector = new Collector();

/**
 * Soft assertion wrapper — try-catch fn, on fail: record finding + screenshot.
 * - severity='critical' → throw SkipScenarioError (caller skip sisa step skenario).
 * - severity='major'/'minor' → return null, lanjut.
 *
 * Screenshot dipath ke test-results/matrix-s{id}-{step-slug}.png. Slug sanitize
 * non-alnum char supaya file path valid di Windows.
 */
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
    // Ref: 316-RESEARCH.md Pitfall 5 (line 400-407) + Open Question 2 (line 486-489).
    if (e instanceof SkipScenarioError) {
      throw e;
    }

    const err = e as { message?: string };
    const stepSlug = ctx.step.replace(/\s+/g, '-').replace(/[^a-zA-Z0-9-]/g, '');
    const candidatePath = `test-results/matrix-s${ctx.scenario.id}-${stepSlug}.png`;

    // Phase 316: defensive screenshot — page-closed pre-check + try/catch.
    // Closed → skip custom path (jangan throw, jangan retry); renderer fallback
    // ke Playwright auto-capture handle missing custom path di renderFinding.
    let screenshotPath: string | undefined;
    if (!ctx.page.isClosed()) {
      try {
        await ctx.page.screenshot({ path: candidatePath, fullPage: true });
        screenshotPath = candidatePath;  // only set kalau write sukses
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
      screenshotPath,
      severity: ctx.severity,
      isMeta: ctx.isMeta,
    });

    if (ctx.severity === 'critical') {
      throw new SkipScenarioError(`Critical at ${ctx.step}: ${err?.message ?? String(e)}`);
    }
    return null;
  }
}

/**
 * Render full markdown report — Summary + Discovery findings + Meta-validation results.
 * Discovery section adalah primary output (severity statistik); Meta section terpisah
 * untuk sentinel `[META-*]` (collector self-check). Per CONTEXT D-06.
 */
function renderReport(discovery: Finding[], meta: Finding[]): string {
  const today = new Date().toISOString().slice(0, 10);
  const severityCount = (arr: Finding[], sev: Severity): number =>
    arr.filter((f) => f.severity === sev).length;

  const summarySection = [
    `# Phase 315 Matrix Test Report — ${today}`,
    ``,
    `## Summary`,
    ``,
    `**Total discovery findings:** ${discovery.length}`,
    `- Critical: ${severityCount(discovery, 'critical')}`,
    `- Major: ${severityCount(discovery, 'major')}`,
    `- Minor: ${severityCount(discovery, 'minor')}`,
    ``,
    `**Meta-validation findings:** ${meta.length} (excluded dari discovery statistik — lihat § Meta-validation results)`,
    ``,
  ].join('\n');

  const discoverySection = [
    `## Discovery findings`,
    ``,
    discovery.length === 0
      ? '_Tidak ada finding di run ini._'
      : discovery.map((f) => renderFinding(f)).join('\n\n'),
    ``,
  ].join('\n');

  const metaSection = [
    `## Meta-validation results`,
    ``,
    meta.length === 0
      ? '_Tidak ada finding sentinel di run ini._'
      : meta.map((f) => renderFinding(f)).join('\n\n'),
    ``,
  ].join('\n');

  return [summarySection, discoverySection, metaSection].join('\n');
}

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

/**
 * Map Finding step + actual ke konkret hypothesis text. Pattern matching ordered specific
 * → general; fallback default jika tidak ada match.
 *
 * Source: docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md § Error
 * handling + classification table. Citation path mengarah ke source code aktual yang
 * harus dicek oleh investigator (dari examMatrix.ts JSDoc citations Plan 02).
 */
function deriveHypothesis(f: Finding): string {
  const step = f.step.toLowerCase();
  const actual = (f.actual || '').toLowerCase();

  if (step.includes('signalr-ready')) {
    return 'Hub belum `Connected` dalam 10s. Cek SignalR negotiate roundtrip; verifikasi `Hubs/AssessmentHub.cs` OnConnectedAsync + browser DevTools Network tab (filter `assessmentHub/negotiate`).';
  }
  // Page-closed pattern (cross-cutting): page.{action} sering throw "Target page... closed"
  // saat context premature-close — biasanya kaskade dari submit-exam page navigate race.
  const isPageClosed = actual.includes('closed') || actual.includes('test ended') || actual.includes('context');
  if (step.includes('mc-q')) {
    if (isPageClosed) {
      return 'MC step gagal akibat page/context closed (kaskade dari critical fail di langkah sebelumnya — biasanya submit-exam navigate race). Cek finding sebelumnya di scenario ini + `Controllers/CMPController.cs` SaveAnswer (line 348-417).';
    }
    return 'MC HTTP `/CMP/SaveAnswer` mungkin 500/timeout, atau `#saveIndicatorText` selector berubah / race-collapse. Cek `Controllers/CMPController.cs` SaveAnswer (line 348-417) + Network tab response status + `Views/CMP/StartExam.cshtml` indicator transition.';
  }
  if (step.includes('ma-q')) {
    if (isPageClosed) {
      return 'MA step gagal akibat page/context closed (kaskade dari critical fail di langkah sebelumnya — biasanya submit-exam navigate race). Cek finding sebelumnya di scenario ini + `Hubs/AssessmentHub.cs` line 188-252.';
    }
    return 'MA `SaveMultipleAnswer` SignalR hub invoke mungkin gagal, atau locator `input.exam-checkbox[data-question-id="..."][value="..."]` tidak match DOM. Cek `Hubs/AssessmentHub.cs` line 188-252 + hub state log + verifikasi `correctOptionIds` di seed match dengan DOM `data-question-id` + `value`.';
  }
  if (step.includes('essay-q')) {
    if (isPageClosed) {
      return 'Essay step gagal akibat page/context closed (kaskade dari critical fail di langkah sebelumnya — biasanya submit-exam navigate race). Cek finding sebelumnya di scenario ini + `Hubs/AssessmentHub.cs` line 134-182.';
    }
    return 'Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.';
  }
  if (step.includes('submit-exam')) {
    if (isPageClosed) {
      return 'SubmitExam click race: page.click trigger redirect SEBELUM Playwright finish click event → context closed mid-action (recurrence dari Plan 04 S5 finding). Solusi: refactor helper pakai `Promise.all([page.waitForURL("**/CMP/Results/**"), page.click(...)])` race-tolerant pattern. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) untuk redirect target.';
    }
    return 'SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.';
  }
  if (step.includes('verify-result-page') || step.includes('result-page')) {
    return 'Score badge selector tidak ketemu — mungkin scoring belum komplet (Essay grading pending) atau selector view berubah. Cek `Views/CMP/Results.cshtml` + verifikasi `gradeEssaysAsHc` step lulus + `AssessmentSession.Status = "Completed"` di DB.';
  }
  if (step.includes('hc-grade-essays') || step.includes('grade-essay')) {
    return 'HC essay grading workflow gagal — selector input score / finalize button mungkin berubah. Cek `Controllers/AssessmentAdminController.cs` SubmitEssayScore + FinalizeEssayGrading (line 2873-2950) + `Views/Admin/AssessmentMonitoringDetail.cshtml` line 348-451 markup.';
  }
  if (step.includes('navigate-start-exam') || step.includes('start-exam')) {
    return 'StartExam page tidak render `#examForm` — kemungkinan: (1) sessionId tidak owned by login user, (2) UserPackageAssignment belum ter-create (A6 AUTO-CREATE-LAZY gagal), (3) view error. Cek `Controllers/CMPController.cs` StartExam (line 880-1000) + AssessmentSession.Status valid.';
  }
  if (step.includes('hc-navigate-monitoring-detail') || step.includes('monitoring-detail')) {
    return 'AssessmentMonitoringDetail page tidak menampilkan title — kemungkinan URL encoding masalah (A5 verdict), atau query string binding gagal. Cek `Controllers/AssessmentAdminController.cs` AssessmentMonitoringDetail (line 2684-2702) + verifikasi URL via DevTools Network tab.';
  }
  if (step.includes('login')) {
    return 'Login fixture gagal — fixture user kemungkinan belum seeded di DB lokal atau password berubah. Cek `tests/helpers/accounts.ts` fixture credentials + verifikasi user exists di tabel Users (`SELECT Email FROM Users WHERE Email IN (...fixtures...)`).';
  }
  if (step.includes('console') || step.includes('error-context')) {
    return 'Console error pattern muncul di luar whitelist. Cek `tests/e2e/helpers/matrixReport.ts` `consoleErrorWhitelist` — tambahkan regex baru kalau pattern terbukti benign, atau investigate kalau pattern menunjukkan real bug di client-side JS.';
  }

  return 'Hypothesis otomatis tidak tersedia untuk step+actual pattern ini. Periksa screenshot, URL bar, browser console log, dan Playwright trace (kalau di-enable). Catat pattern baru di `deriveHypothesis()` matrixReport.ts untuk reproducibility iterasi berikutnya.';
}
