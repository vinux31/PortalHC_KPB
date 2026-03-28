// Debug: proper form submit via page.locator with force
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';

async function tryLogin(browser, email, password) {
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(5000);
  console.log('Login page loaded');

  // Fill using locator with force option
  await page.locator('input[name="email"]').fill(email, { force: true, timeout: 10000 });
  await page.locator('input[name="password"]').fill(password, { force: true, timeout: 10000 });
  console.log('Fields filled');

  // Submit form
  await page.locator('button[type="submit"]').click({ force: true, timeout: 10000 });
  console.log('Submit clicked');

  // Wait for navigation
  try {
    await page.waitForURL(url => !url.includes('/Account/Login'), { timeout: 20000 });
    console.log(`Login OK! URL: ${page.url()}`);
    const cookies = await ctx.cookies();
    console.log(`Cookies: ${cookies.map(c => c.name).join(', ')}`);
    await ctx.close();
    return true;
  } catch(e) {
    const url = page.url();
    const errText = await page.evaluate(() => {
      const alert = document.querySelector('.alert, .validation-summary-errors, [class*="error"]');
      return alert ? alert.innerText : '';
    });
    console.log(`Login FAIL. URL: ${url}, Error: "${errText}"`);
    await ctx.close();
    return false;
  }
}

async function main() {
  const browser = await chromium.launch({ headless: true });

  // Test with arsyad first (known working password)
  console.log('\n--- Testing mohammad.arsyad ---');
  await tryLogin(browser, 'mohammad.arsyad@pertamina.com', 'Pertamina@2026');

  // Test with rino
  console.log('\n--- Testing rino.prasetyo ---');
  for (const pass of ['123456', 'Pertamina@2026', 'Balikpapan@2026']) {
    console.log(`Trying: ${pass}`);
    const ok = await tryLogin(browser, 'rino.prasetyo@pertamina.com', pass);
    if (ok) break;
  }

  await browser.close();
}

main().catch(e => { console.error(e); process.exit(1); });
