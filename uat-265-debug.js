// Debug script to check login and navigation
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  // Login rino
  console.log('Navigating to login page...');
  await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  console.log('Login page URL:', page.url());
  await page.waitForTimeout(2000);

  // Wait a bit more then inspect the page
  await page.waitForTimeout(5000);
  const inputs = await page.evaluate(() => {
    return Array.from(document.querySelectorAll('input')).map(i => ({
      name: i.name, type: i.type, id: i.id, placeholder: i.placeholder, disabled: i.disabled, visible: i.offsetParent !== null
    }));
  });
  console.log('Inputs:', JSON.stringify(inputs));
  const bodyHtml = await page.evaluate(() => document.body.innerHTML.substring(0, 500));
  console.log('Body HTML snippet:', bodyHtml);

  console.log('Filling credentials via evaluate...');
  await page.evaluate(({ email, pass }) => {
    const inputs = document.querySelectorAll('input');
    for (const inp of inputs) {
      if (inp.type === 'email' || inp.name.toLowerCase() === 'email') {
        inp.value = email;
        inp.dispatchEvent(new Event('input', { bubbles: true }));
      }
      if (inp.type === 'password') {
        inp.value = pass;
        inp.dispatchEvent(new Event('input', { bubbles: true }));
      }
    }
    const btn = document.querySelector('button[type="submit"]');
    if (btn) btn.click();
  }, { email: 'rino.prasetyo@pertamina.com', pass: '123456' });

  console.log('Waiting after submit...');
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(3000);
  console.log('URL after login:', page.url());

  // Check cookies
  const cookies = await ctx.cookies();
  console.log('Cookies count:', cookies.length);
  console.log('Cookie names:', cookies.map(c => c.name));

  // Try navigation with longer timeout
  console.log('Navigating to CMP/Assessment...');
  try {
    await page.goto(`${BASE_URL}/CMP/Assessment`, { waitUntil: 'domcontentloaded', timeout: 120000 });
    console.log('Assessment URL:', page.url());
    const text = await page.evaluate(() => document.body.innerText.substring(0, 200));
    console.log('Body:', text);
  } catch(e) {
    console.log('Error:', e.message.split('\n')[0]);
    console.log('Current URL:', page.url());
  }

  await browser.close();
}

main().catch(e => { console.error(e); process.exit(1); });
