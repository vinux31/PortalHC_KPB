import { Page, expect } from '@playwright/test';

/**
 * Upload Excel file dan proses import
 * @param page - Playwright Page object
 * @param filePath - Path ke file Excel fixture
 */
export async function uploadAndProcessImport(page: Page, filePath: string) {
  await page.setInputFiles('#excelFileInput', filePath);
  await page.click('#btnImport');

  // Wait for loading to complete with better error handling
  try {
    await page.waitForSelector('text=Memproses import...', { state: 'hidden', timeout: 30000 });
  } catch (error) {
    throw new Error('Import processing timeout - server may not be responding');
  }

  // Wait for results to appear
  await page.waitForSelector('.card:has-text("Berhasil Dibuat")', { timeout: 5000 });
}

/**
 * Verify summary count setelah import
 * @param page - Playwright Page object
 * @param success - Expected number of successful imports
 * @param error - Expected number of errors
 */
export async function verifyImportSummary(page: Page, success: number, error: number) {
  await expect(page.locator('.card:has-text("Berhasil Dibuat")')).toContainText(success.toString());
  await expect(page.locator('.card:has-text("Error / Gagal")')).toContainText(error.toString());
}

/**
 * Verify training muncul di ManageAssessment list
 *
 * WARNING: This function navigates away from the current page!
 * Ensure you're done with the current page before calling this.
 *
 * @param page - Playwright Page object
 * @param trainingTitle - Judul training yang dicari
 */
export async function verifyTrainingInList(page: Page, trainingTitle: string) {
  await page.goto('/Admin/ManageAssessment?tab=training');
  await page.waitForLoadState('networkidle');

  const searchInput = page.locator('input[placeholder*="Cari berdasarkan judul,"]').first();
  await searchInput.fill(trainingTitle);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');

  await expect(page.locator(`text=${trainingTitle}`)).toBeVisible();
}

/**
 * Verify assessment muncul di ManageAssessment list
 *
 * WARNING: This function navigates away from the current page!
 * Ensure you're done with the current page before calling this.
 *
 * @param page - Playwright Page object
 * @param assessmentTitle - Judul assessment yang dicari
 */
export async function verifyAssessmentInList(page: Page, assessmentTitle: string) {
  await page.goto('/Admin/ManageAssessment?tab=assessment');
  await page.waitForLoadState('networkidle');

  const searchInput = page.locator('input[placeholder*="Cari berdasarkan judul,"]').first();
  await searchInput.fill(assessmentTitle);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');

  await expect(page.locator(`text=${assessmentTitle}`)).toBeVisible();
}
