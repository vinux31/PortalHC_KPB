import { Page, expect } from '@playwright/test';

/** Format tomorrow's date as YYYY-MM-DD */
export function tomorrow(): string {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().split('T')[0];
}

/** Format today's date as YYYY-MM-DD */
export function today(): string {
  return new Date().toISOString().split('T')[0];
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
