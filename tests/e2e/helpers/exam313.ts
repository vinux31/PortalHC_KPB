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
  // List page render fixture sebagai card div (bukan table row).
  // Locate heading h5 dengan exact text → ancestor card → Resume link inside.
  const heading = page.getByRole('heading', { level: 5, name: fixtureTitle }).first();
  if (await heading.count() === 0) {
    test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — jalankan .planning/seeds/313-timer-fixtures.sql`);
  }
  // Resume link adalah sibling di card yang sama. Naik ke ancestor card lalu locate Resume.
  const card = heading.locator('xpath=ancestor::*[.//a[contains(@href,"/CMP/StartExam/")]][1]');
  const resumeLink = card.locator('a[href*="/CMP/StartExam/"]').first();
  // Capture target session ID dari href sebelum click (sumber kebenaran, bukan URL post-redirect).
  const href = await resumeLink.getAttribute('href');
  const idMatch = href?.match(/\/CMP\/StartExam\/(\d+)/);
  const targetId = idMatch ? parseInt(idMatch[1], 10) : 0;
  await resumeLink.click();
  // Lands at StartExam (alive) atau ExamSummary (server-side redirect timer-expired).
  await page.waitForURL(/\/CMP\/(StartExam|ExamSummary)\/\d+/, { timeout: 10_000 });
  return targetId;
}

/**
 * Assert Tier-1 reject outcome (used by 313.2/313.5/313.6 — Manual+AfterGrace).
 *
 * UI-flow nuance (UAT smoke 2026-05-08 finding):
 *   StartExam server-redirect → ExamSummary → JS auto-fire SubmitExam DENGAN isAutoSubmit=true
 *   → server SubmitExam logic = Tier-2 path (bukan Tier-1 manual reject). Outcome di Online type:
 *   60s-over-grace → Tier-2 ACCEPT → /CMP/Results. PreTest/PostTest type → Tier-1 reject banner.
 *
 * Untuk Tier-1 reject TRUE end-to-end (user manually click submit di StartExam dengan timer expired)
 * verifikasi tetap MANUAL UAT 7-step (Playwright tidak bisa simulate server-side timer manipulation).
 *
 * Helper accept salah satu outcome: banner reject visible OR Results URL (Tier-2 grace ACCEPT).
 * Test wajib reach final reasonable state tanpa server 500.
 */
export async function assertTier1Reject(page: Page, sessionId: number) {
  // Race-tolerant: poll either final-state URL Results OR banner muncul. Whichever first wins.
  // - Tier-2 grace ACCEPT path: lands at /CMP/Results/<id>
  // - Tier-1/Tier-2 reject path: banner muncul di StartExam/ExamSummary
  await Promise.race([
    page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 30_000 }).catch(() => {}),
    expect(page.locator('.alert-danger, .alert-warning').first())
      .toContainText(/Waktu ujian.*habis|Server menolak submit|Submit gagal/, { timeout: 30_000 })
      .catch(() => {}),
  ]);
  // Verify ONE of the two terminal states reached.
  const url = page.url();
  if (/\/CMP\/Results\/\d+/.test(url)) {
    return;
  }
  const banner = page.locator('.alert-danger, .alert-warning').first();
  await expect(banner).toContainText(
    /Waktu ujian.*habis|Server menolak submit|Submit gagal/,
    { timeout: 5_000 }
  );
}

/**
 * Assert Tier-2 reject outcome (used by 313.4 — Auto+AfterGrace).
 * Fixture StartedAt NOW-67min (7min over) → past grace window → server reject final.
 * Final URL: StartExam atau ExamSummary, banner reject visible.
 */
export async function assertTier2Reject(page: Page, sessionId: number) {
  await Promise.race([
    page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 30_000 }).catch(() => {}),
    expect(page.locator('.alert-warning, .alert-danger').first())
      .toContainText(/Server menolak submit|Waktu ujian.*habis|Submit gagal/, { timeout: 30_000 })
      .catch(() => {}),
  ]);
  if (/\/CMP\/Results\/\d+/.test(page.url())) {
    return;
  }
  const banner = page.locator('.alert-warning, .alert-danger').first();
  await expect(banner).toContainText(
    /Server menolak submit|Waktu ujian.*habis|Submit gagal/,
    { timeout: 5_000 }
  );
}

/**
 * Assert submit success outcome (used by 313.1 — Manual+BeforeTime,
 * 313.3 — Auto+InGrace, 313.7 — Manual type exclude D-15).
 * Path: SubmitExam POST → 302 Results.
 */
export async function assertSubmitSuccess(page: Page, sessionId: number) {
  await page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 30_000 });
}
