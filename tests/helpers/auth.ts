import { Page } from '@playwright/test';
import { accounts, AccountKey } from './accounts';

export async function login(page: Page, account: AccountKey) {
  const { email, password } = accounts[account];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL('**/Home/**', { timeout: 15_000 });
}

export async function logout(page: Page) {
  await page.goto('/Account/Logout');
}
