// Ubah durasi assessment 10 menjadi 2 menit untuk test EDGE-07
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const ASSESSMENT_ID = 10; // UAT OJT Test 2 - No Token, Arsyad's assessment

function log(msg) { console.log('[' + new Date().toISOString().substring(11,19) + '] ' + msg); }

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  // Login admin
  await page.goto(BASE_URL + '/Account/Login', { waitUntil: 'networkidle', timeout: 90000 });
  await page.fill('input[type="email"]', 'admin@pertamina.com');
  await page.fill('input[type="password"]', '123456');
  await page.evaluate(function() { document.querySelector('button[type="submit"]').click(); });
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(2000);
  log('Login: ' + page.url());

  // Buka edit form
  await page.goto(BASE_URL + '/Admin/EditAssessment/' + ASSESSMENT_ID, { waitUntil: 'networkidle', timeout: 90000 });
  log('Edit form: ' + page.url());

  // Lihat field form
  const formFields = await page.evaluate(function() {
    return Array.from(document.querySelectorAll('input, select')).filter(function(i) { return i.name; }).map(function(i) {
      return { name: i.name, id: i.id, type: i.type || i.tagName, value: i.type === 'checkbox' ? i.checked : i.value };
    }).filter(function(f) { return !['UserIds', '__RequestVerificationToken', 'NewUserIds'].includes(f.name); });
  });
  log('Form fields: ' + JSON.stringify(formFields));

  // Ubah DurationMinutes ke 2 via JS
  const durationChanged = await page.evaluate(function() {
    const inp = document.getElementById('DurationMinutes') || document.querySelector('input[name="DurationMinutes"]');
    if (inp) {
      const old = inp.value;
      inp.value = '2';
      inp.dispatchEvent(new Event('input', { bubbles: true }));
      inp.dispatchEvent(new Event('change', { bubbles: true }));
      return 'changed from ' + old + ' to 2';
    }
    return 'not found';
  });
  log('Duration: ' + durationChanged);

  // Screenshot sebelum submit
  await page.screenshot({ path: 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/edit-form-before.png' });

  // Submit via klik tombol submit
  const submitBtn = await page.$('button[type="submit"], input[type="submit"]');
  if (submitBtn) {
    log('Clicking submit button...');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(function(e) { log('Nav timeout: ' + e.message.substring(0,50)); return null; }),
      submitBtn.click()
    ]);
  } else {
    log('No submit button found, trying form.submit()');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(function(e) { return null; }),
      page.evaluate(function() {
        const form = document.querySelector('form');
        if (form) form.submit();
      })
    ]);
  }
  await page.waitForTimeout(2000);
  log('After submit: ' + page.url());

  const afterBody = await page.evaluate(function() { return document.body.innerText.substring(0, 300); });
  log('After body: ' + afterBody);

  await page.screenshot({ path: 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/edit-form-after.png' });

  // Verifikasi perubahan
  await page.goto(BASE_URL + '/Admin/EditAssessment/' + ASSESSMENT_ID, { waitUntil: 'networkidle', timeout: 90000 });
  const newDuration = await page.evaluate(function() {
    const inp = document.getElementById('DurationMinutes') || document.querySelector('input[name="DurationMinutes"]');
    return inp ? inp.value : 'not found';
  });
  log('Verification - DurationMinutes after edit: ' + newDuration);

  await browser.close();

  fs.writeFileSync('C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/edit-duration-result.json', JSON.stringify({
    assessmentId: ASSESSMENT_ID,
    durationChanged,
    newDuration,
    success: newDuration === '2'
  }, null, 2));
  log('Done. Duration is now: ' + newDuration);
}

main().catch(function(e) {
  console.error('Fatal error:', e.message);
  process.exit(1);
});
