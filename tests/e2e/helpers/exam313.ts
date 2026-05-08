// Phase 313.1 — Helpers untuk FLOW 313 timer-enforcement E2E tests
// Centralized fixture interactions: locate row, click Resume, assert outcome banners.
// Banner literals match Controllers/CMPController.cs:4599 (Tier-1) + 4607 (Tier-2)
// + Views/CMP/ExamSummary.cshtml:206 (custom JS retry warning).

import { Page, expect, test } from '@playwright/test';

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/**
 * Locate fixture row by Title, click Resume link, wait for navigation to either
 * StartExam (alive session) or ExamSummary (server-side redirect timer-expired).
 * Returns sessionId parsed from URL.
 *
 * Auto-skips test if fixture not found (seed Wave 0 not yet applied).
 */
export async function clickResumeForFixture(
  page: Page,
  fixtureTitle: string
): Promise<number> {
  await page.goto('/CMP/Assessment');
  const targetRow = page.locator('tr', {
    hasText: new RegExp(`^\\s*${escapeRegex(fixtureTitle)}\\s*`, 'm')
  }).first();
  if (await targetRow.count() === 0) {
    test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — jalankan .planning/seeds/313-timer-fixtures.sql`);
  }
  await targetRow.locator('a:has-text("Resume")').click();
  // Lands at StartExam (alive) atau ExamSummary (server-side redirect timer-expired)
  await page.waitForURL(/\/CMP\/(StartExam|ExamSummary)\/\d+/, { timeout: 10_000 });
  const m = page.url().match(/\/(?:StartExam|ExamSummary)\/(\d+)/);
  return m ? parseInt(m[1], 10) : 0;
}

/**
 * Assert Tier-1 reject outcome (used by 313.2/313.5/313.6 — Manual+AfterGrace).
 * Path: ExamSummary auto-fire JS retry → server 302 StartExam + TempData["Error"]
 * Banner D-01 verbatim from Controllers/CMPController.cs:4599 (substring match).
 */
export async function assertTier1Reject(page: Page, sessionId: number) {
  await page.waitForURL(new RegExp(`/CMP/StartExam/${sessionId}\\b`), { timeout: 30_000 });
  await expect(page.locator('.alert-danger')).toContainText(
    'Waktu ujian Anda sudah habis'
  );
}

/**
 * Assert Tier-2 reject outcome (used by 313.4 — Auto+AfterGrace).
 * Path: server 302 StartExam, JS retry handler intercept → custom .alert-warning
 * ATAU server-side .alert-danger dari TempData (jika JS tidak intercept).
 * Banner regex covers both branches per RESEARCH.md "Banner Text Literals" Tier-2 section.
 */
export async function assertTier2Reject(page: Page, sessionId: number) {
  await page.waitForURL(new RegExp(`/CMP/StartExam/${sessionId}\\b`), { timeout: 30_000 });
  const banner = page.locator('.alert-warning, .alert-danger').first();
  await expect(banner).toContainText(
    /Server menolak submit|Waktu ujian Anda telah habis/
  );
}

/**
 * Assert submit success outcome (used by 313.1 — Manual+BeforeTime,
 * 313.3 — Auto+InGrace, 313.7 — Manual type exclude D-15).
 * Path: SubmitExam POST → 302 Results.
 */
export async function assertSubmitSuccess(page: Page, sessionId: number) {
  await page.waitForURL(new RegExp(`/CMP/Results/${sessionId}\\b`), { timeout: 30_000 });
}
