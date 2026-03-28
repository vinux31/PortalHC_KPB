// Debug login via fetch/form submission
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  // Get login page first (to get antiforgery token and cookies)
  console.log('Getting login page...');
  await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(5000);
  console.log('Login page ready, URL:', page.url());

  // Get antiforgery token from page
  const token = await page.evaluate(() => {
    const inp = document.querySelector('input[name="__RequestVerificationToken"]');
    return inp ? inp.value : null;
  });
  console.log('CSRF token:', token ? token.substring(0, 20) + '...' : 'NOT FOUND');

  // Get current cookies
  const cookies1 = await ctx.cookies();
  console.log('Cookies before login:', cookies1.map(c => `${c.name}=${c.value.substring(0,10)}`));

  // Submit form using fetch inside page context
  const loginResult = await page.evaluate(async ({ baseUrl, email, pass, tok }) => {
    const formData = new URLSearchParams();
    formData.append('email', email);
    formData.append('password', pass);
    formData.append('rememberMe', 'false');
    formData.append('__RequestVerificationToken', tok);

    const resp = await fetch(`${baseUrl}/Account/Login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: formData.toString(),
      redirect: 'follow',
      credentials: 'include'
    });
    return { status: resp.status, url: resp.url, ok: resp.ok };
  }, { baseUrl: BASE_URL, email: 'rino.prasetyo@pertamina.com', pass: '123456', tok: token });
  console.log('Login fetch result:', loginResult);

  await page.waitForTimeout(2000);
  const cookies2 = await ctx.cookies();
  console.log('Cookies after login:', cookies2.map(c => `${c.name}=${c.value.substring(0,10)}`));

  // Now navigate to assessment
  console.log('\nNavigating to Assessment...');
  try {
    await page.goto(`${BASE_URL}/CMP/Assessment`, { waitUntil: 'domcontentloaded', timeout: 90000 });
    console.log('Assessment URL:', page.url());
    const text = await page.evaluate(() => document.body.innerText.substring(0, 300));
    console.log('Body:', text);
  } catch(e) {
    console.log('Error:', e.message.split('\n')[0]);
  }

  await browser.close();
}

main().catch(e => { console.error(e); process.exit(1); });
