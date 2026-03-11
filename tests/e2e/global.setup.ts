import { test as setup, expect } from '@playwright/test';

setup('verify app is running', async ({ page }) => {
  const response = await page.goto('/Account/Login');
  expect(response?.ok()).toBeTruthy();
  await expect(page.locator('button[type="submit"]')).toBeVisible();
});
