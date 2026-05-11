/**
 * Phase 316 Plan 03 Wave 0 — THROWAWAY probe spec.
 *
 * Tujuan: Validate research assumption A2 (316-RESEARCH-GAP-316-2.md line 470-471):
 *   "test.describe() boundary mengisolasi failure di mode `fullyParallel: false`"
 *
 * Expected output (jika A2 valid):
 *   1 failed, 1 passed
 *
 * Anti-expected (jika A2 invalid, fallback path needed):
 *   1 failed, 1 did not run
 *
 * Cleanup: File ini dihapus di Plan 06 final task. JANGAN commit jika hasil "did not run".
 * Sumber: 316-RESEARCH-GAP-316-2.md Open Question Q2 + Wave 0 Gaps line 433-436.
 */

import { test } from '@playwright/test';

// NO test.describe.configure({ mode: 'serial' }) di sini — point adalah test default mode
// dengan fullyParallel: false (config-level).

test.describe('_PROBE_316 Block A: always-fail-fast', () => {
  test('synthetic throw — no page operation', async () => {
    // Throw langsung, tidak ada Playwright wait yang bisa accumulate timer.
    // Pure synthetic untuk isolate A2 dari A1 (page.check timeout question).
    throw new Error('Phase 316 Wave 0 probe — intentional fail to test describe boundary');
  });
});

test.describe('_PROBE_316 Block B: should-run-after-A-fails', () => {
  test('synthetic pass — confirm block B executed', async () => {
    // Kalau test ini EXECUTE dan PASS → A2 VALID (describe boundary isolate failure).
    // Kalau test ini "did not run" di output summary → A2 INVALID (full halt-on-first-fail).
    console.log('[_PROBE_316] Block B executed — A2 validation: PASS');
  });
});
