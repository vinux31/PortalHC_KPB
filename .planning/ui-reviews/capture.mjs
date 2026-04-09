import { chromium } from 'playwright';

const dir = process.argv[2] || '.planning/ui-reviews/282-screenshots';

const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await context.newPage();

// Login
await page.goto('http://localhost:5277/Account/Login');
await page.fill('input[name="Email"], input[type="email"]', 'rustam.nugroho@pertamina.com');
await page.fill('input[name="Password"], input[type="password"]', '123456');
await page.click('button[type="submit"], input[type="submit"]');
await page.waitForURL('**/Home/**', { timeout: 10000 }).catch(() => {});
await page.waitForTimeout(2000);

// Admin/Maintenance desktop
await page.goto('http://localhost:5277/Admin/Maintenance');
await page.waitForTimeout(2000);
await page.screenshot({ path: `${dir}/admin-maintenance-desktop.png`, fullPage: true });

// Admin/Maintenance mobile
await page.setViewportSize({ width: 375, height: 812 });
await page.waitForTimeout(500);
await page.screenshot({ path: `${dir}/admin-maintenance-mobile.png`, fullPage: true });

// Admin/Index card
await page.setViewportSize({ width: 1440, height: 900 });
await page.goto('http://localhost:5277/Admin');
await page.waitForTimeout(2000);
await page.screenshot({ path: `${dir}/admin-index-card.png`, fullPage: true });

// Home/Maintenance (user view) - need to check if maintenance is active
await page.goto('http://localhost:5277/Home/Maintenance');
await page.waitForTimeout(2000);
await page.screenshot({ path: `${dir}/home-maintenance.png`, fullPage: true });

await browser.close();
console.log('Done');
