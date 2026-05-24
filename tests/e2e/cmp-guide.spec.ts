import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';

test.describe('CMP Guide Page - PDF Card & Accordion Role-Gating', () => {

  test('5.1 - Admin sees 2 PDF cards on /Home/GuideDetail?module=cmp', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const pdfCards = page.locator('.guide-tutorial-card');
    await expect(pdfCards).toHaveCount(2);

    await expect(pdfCards.nth(0)).toContainText('Panduan Lengkap Assessment');
    await expect(pdfCards.nth(1)).toContainText('Panduan Buat Assessment');
  });

  test('5.2 - Coachee sees only 1 PDF card on /Home/GuideDetail?module=cmp', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const pdfCards = page.locator('.guide-tutorial-card');
    await expect(pdfCards).toHaveCount(1);
    await expect(pdfCards.first()).toContainText('Panduan Lengkap Assessment');
    await expect(pdfCards.first()).not.toContainText('Panduan Buat Assessment');
  });

  test('5.3 - Admin sees 13 accordion items on CMP page (post Budget Training)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(13);
  });

  test('5.4 - Coachee sees 5 accordion items on CMP page (5 role-All)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(5);

    // 5 role-All accordion MUST be visible
    await expect(page.locator('text=Library KKJ')).toBeVisible();
    await expect(page.locator('text=Alignment KKJ')).toBeVisible();
    await expect(page.locator('text=Training Records')).toBeVisible();
    await expect(page.locator('text=Pre-Post Test')).toBeVisible();
    await expect(page.locator('text=Tipe-tipe Assessment')).toBeVisible();

    // AdminHC-only accordion MUST NOT be visible
    await expect(page.locator('text=Fitur Khusus Admin')).not.toBeVisible();
    await expect(page.locator('text=Cara Manage Kategori Assessment')).not.toBeVisible();
    await expect(page.locator('text=Budget Training')).not.toBeVisible();
  });

  test('5.5 - Data module no longer has PDF card (admin)', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=data');
    await page.waitForLoadState('networkidle');

    const pdfCards = page.locator('.guide-tutorial-card');
    await expect(pdfCards).toHaveCount(0);
  });

  test('5.6 - Admin can open PDF coachee in new tab', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const coacheePdfCard = page.locator('.guide-tutorial-card', { hasText: 'Panduan Lengkap Assessment' });
    const lihatLink = coacheePdfCard.locator('a:has-text("Lihat")');

    await expect(lihatLink).toHaveAttribute('target', '_blank');
    const href = await lihatLink.getAttribute('href');
    expect(href).toContain('/documents/guides/Panduan-Lengkap-Assessment.html');
  });

  test('5.7 - Admin can open PDF admin in new tab', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const adminPdfCard = page.locator('.guide-tutorial-card', { hasText: 'Panduan Buat Assessment' });
    const lihatLink = adminPdfCard.locator('a:has-text("Lihat")');

    await expect(lihatLink).toHaveAttribute('target', '_blank');
    const href = await lihatLink.getAttribute('href');
    expect(href).toContain('/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html');
  });

  test('5.8 - Accordion Acc-5 expands to show 3 steps (Pre/Post/Regular)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const acc5 = page.locator('.accordion-item', { hasText: 'Tipe-tipe Assessment' });
    await acc5.locator('button.accordion-button').click();

    await page.waitForTimeout(500);

    const body = acc5.locator('.accordion-collapse.show');
    await expect(body).toContainText('Pre-Test');
    await expect(body).toContainText('Post-Test');
    await expect(body).toContainText('Regular Assessment');
  });

  test('5.9 - Admin sees Budget Training accordion + 13 total', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(13);

    // Acc-7 Budget Training visible to admin
    const budgetAcc = page.locator('.accordion-item', { hasText: 'Budget Training' });
    await expect(budgetAcc).toBeVisible();

    // Expand and verify content
    await budgetAcc.locator('button.accordion-button').click();
    await page.waitForTimeout(500);
    const body = budgetAcc.locator('.accordion-collapse.show');
    await expect(body).toContainText('Data Budget');
    await expect(body).toContainText('Import Excel');
    await expect(body).toContainText('Ringkasan');
  });

});
