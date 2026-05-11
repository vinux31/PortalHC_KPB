// Phase 315 — Singleton collector + softAssert + markdown renderer untuk matrix discovery
// findings.
//
// Lifecycle assumption: tests/playwright.config.ts:7 `fullyParallel: false` + default 1 worker
// → singleton `collector` state persist across globalSetup → spec → globalTeardown
// (semua di same Node process). RESEARCH.md § Pattern 3 line 573 verified pattern.
//
// Sumber spec utama: docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md
// (commit 94bacecf). Severity classification + isMeta filter mengikuti CONTEXT D-06
// (sentinel [META-CollectorCheck] dipisah dari summary statistik discovery).
//
// Markdown renderer pakai template literal (no markdown lib per RESEARCH § Don't Hand-Roll).

import { Page } from '@playwright/test';
import { writeFile, mkdir } from 'fs/promises';
import { dirname } from 'path';
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
 * Collector singleton — menampung findings sepanjang test run.
 * `flush()` di-call dari globalTeardown (Plan 03) sebelum RESTORE supaya report tetap
 * tertulis kalau RESTORE crash (RESEARCH § Anti-Patterns line 657).
 */
class Collector {
  private findings: Finding[] = [];

  /**
   * Whitelist console error patterns yang aman di-ignore di Layer "no console error".
   * Empty-ish default — diisi di Plan 05 saat polish iterasi setelah smoke run nemu
   * pattern aktual yang noise vs sinyal.
   */
  private consoleErrorWhitelist: RegExp[] = [
    /favicon\.ico/i,            // benign asset 404 saat sub-path / browser pre-fetch
    /SignalR.*reconnect/i,      // benign hub reconnect informational (auto-recovery)
    // Tambah pattern di Plan 05 saat polish iterasi smoke run.
  ];

  record(f: Finding): void {
    this.findings.push(f);
  }

  count(): number {
    return this.findings.length;
  }

  /** Expose whitelist supaya spec page.on('console') handler bisa filter. */
  getConsoleErrorWhitelist(): RegExp[] {
    return this.consoleErrorWhitelist;
  }

  /**
   * Flush ke markdown file. Auto-mkdir parent directory kalau belum ada
   * (docs/test-reports/ first run case).
   */
  async flush(outPath: string): Promise<void> {
    const discovery = this.findings.filter((f) => !f.isMeta);
    const meta = this.findings.filter((f) => f.isMeta);
    const md = renderReport(discovery, meta);
    await mkdir(dirname(outPath), { recursive: true });
    await writeFile(outPath, md, 'utf-8');
  }
}

/** Module-level singleton — single instance di-share Plan 03 teardown + Plan 04 spec. */
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
    const err = e as { message?: string };
    const stepSlug = ctx.step.replace(/\s+/g, '-').replace(/[^a-zA-Z0-9-]/g, '');
    const screenshotPath = `test-results/matrix-s${ctx.scenario.id}-${stepSlug}.png`;
    await ctx.page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});

    collector.record({
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
  lines.push(`- **Hypothesis:** _TBD — Plan 05 polish iterasi finding._`);
  return lines.join('\n');
}
