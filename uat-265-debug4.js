// Debug: post-login navigation
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';

async function login(page, email, password) {
  await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(5000);
  await page.locator('input[name="email"]').fill(email, { force: true, timeout: 10000 });
  await page.locator('input[name="password"]').fill(password, { force: true, timeout: 10000 });
  await page.locator('button[type="submit"]').click({ force: true, timeout: 10000 });
  await page.waitForTimeout(5000);
  const url = page.url();
  const success = !url.includes('/Account/Login');
  console.log(`Login ${email}: ${success ? 'OK' : 'FAIL'} (${url})`);
  return success;
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  // Login as arsyad
  const ok = await login(page, 'mohammad.arsyad@pertamina.com', 'Pertamina@2026');
  console.log('Login result:', ok);

  if (ok) {
    const cookies = await ctx.cookies();
    console.log('Cookies:', cookies.map(c => `${c.name}=${c.value.substring(0,15)}`));

    // Try to navigate to CMP/Assessment - but just wait for domcontentloaded
    console.log('\nNavigating to CMP/Assessment...');
    const startTime = Date.now();
    try {
      await page.goto(`${BASE_URL}/CMP/Assessment`, { waitUntil: 'domcontentloaded', timeout: 120000 });
      console.log(`Navigation done in ${Date.now() - startTime}ms`);
      console.log('URL:', page.url());
      const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 200));
      console.log('Body:', bodyText);
    } catch(e) {
      console.log(`Navigation error (${Date.now() - startTime}ms):`, e.message.split('\n')[0]);
      console.log('Current URL:', page.url());
    }
  }

  await browser.close();
}

main().catch(e => { console.error(e); process.exit(1); });
