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

  test('5.3 - Admin sees 12 accordion items on CMP page', async ({ page }) => {
    await login(page, 'admin');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(12);
  });

  test('5.4 - Coachee sees 7 accordion items on CMP page (1 new + 6 existing)', async ({ page }) => {
    await login(page, 'coachee');
    await page.goto('/Home/GuideDetail?module=cmp');
    await page.waitForLoadState('networkidle');

    const accordionItems = page.locator('#guideAccordion .accordion-item');
    await expect(accordionItems).toHaveCount(7);

    await expect(page.locator('text=Tipe-tipe Assessment')).toBeVisible();

    await expect(page.locator('text=Fitur Khusus Admin')).not.toBeVisible();
    await expect(page.locator('text=Cara Manage Kategori Assessment')).not.toBeVisible();
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

});
