import { Page, expect } from '@playwright/test';

/**
 * Format a Date as YYYY-MM-DD using LOCAL date components (NOT UTC).
 * Phase 382 (Rule 3 fix): server schedule validation pakai `DateTime.Today` (waktu LOKAL server).
 * `toISOString()` mengembalikan tanggal UTC — di zona UTC+ (mis. Singapore UTC+8) saat jam lokal
 * dini hari, tanggal UTC = tanggal LOKAL kemarin → `today()` lama menghasilkan tanggal kemarin →
 * server tolak "Schedule date cannot be in the past." Pakai komponen lokal supaya selaras server.
 */
function fmtLocal(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Format tomorrow's date as YYYY-MM-DD (local). */
export function tomorrow(): string {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return fmtLocal(d);
}

/** Format today's date as YYYY-MM-DD (local — selaras `DateTime.Today` server). */
export function today(): string {
  return fmtLocal(new Date());
}

/** Format yesterday's date as YYYY-MM-DD (local) (Phase 318 Plan 03 — FLOW Q EWCD past). */
export function yesterday(): string {
  const d = new Date();
  d.setDate(d.getDate() - 1);
  return fmtLocal(d);
}

/** Generate a unique assessment title for test isolation */
export function uniqueTitle(prefix = 'E2E Test'): string {
  return `${prefix} ${Date.now()}`;
}

/** Wait for page navigation to complete after an action */
export async function waitForNav(page: Page, action: () => Promise<void>) {
  await Promise.all([
    page.waitForLoadState('networkidle'),
    action(),
  ]);
}

/** Accept browser confirm() dialog */
export function autoConfirm(page: Page) {
  page.once('dialog', dialog => dialog.accept());
}
