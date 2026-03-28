// UAT 267 Setup: Buat assessment 2 menit untuk Arsyad
// Run: node uat-267-setup.js

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const ARSYAD_USER_ID = 'acf6b7e4-3ff2-4fa8-8fef-82b1ce8490d2';
const ARSYAD_CHECKBOX_ID = 'user_acf6b7e4-3ff2-4fa8-8fef-82b1ce8490d2';

function log(msg) {
  console.log(`[${new Date().toISOString().substring(11,19)}] ${msg}`);
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  // Login admin
  await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle', timeout: 90000 });
  await page.fill('input[type="email"]', 'admin@pertamina.com');
  await page.fill('input[type="password"]', '123456');
  await page.evaluate(function() { document.querySelector('button[type="submit"]').click(); });
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(2000);
  log('Login: ' + page.url());

  // Navigasi ke form buat assessment
  await page.goto(`${BASE_URL}/Admin/CreateAssessment`, { waitUntil: 'networkidle', timeout: 90000 });
  log('Create form: ' + page.url());

  // Cek semua field yang tersedia
  const fields = await page.evaluate(function() {
    return Array.from(document.querySelectorAll('input, select')).filter(function(i) { return i.name; }).map(function(i) {
      return { name: i.name, id: i.id, type: i.type || i.tagName, placeholder: i.placeholder || '' };
    }).filter(function(f) { return f.name !== 'UserIds' && f.name !== '__RequestVerificationToken'; });
  });
  log('Fields: ' + JSON.stringify(fields));

  // Isi Category = OJT
  await page.selectOption('select[name="Category"]', 'On Job Training (OJT)');
  await page.waitForTimeout(1000);

  // Isi Title
  await page.fill('input[name="Title"]', 'UAT Timer Test Arsyad');

  // Isi DurationMinutes = 2 (mungkin dalam section yang tersembunyi, pakai JS force)
  const durationSet = await page.evaluate(function() {
    const inp = document.querySelector('input[name="DurationMinutes"]');
    if (inp) {
      // Force visible
      inp.style.display = 'block';
      let parent = inp.parentElement;
      while (parent && parent !== document.body) {
        const s = window.getComputedStyle(parent);
        if (s.display === 'none') parent.style.display = 'block';
        if (s.visibility === 'hidden') parent.style.visibility = 'visible';
        parent = parent.parentElement;
      }
      inp.value = '2';
      inp.dispatchEvent(new Event('input', { bubbles: true }));
      inp.dispatchEvent(new Event('change', { bubbles: true }));
      return 'set to 2';
    }
    return 'not found';
  });
  log('DurationMinutes: ' + durationSet);

  // Force semua field tersembunyi agar visible, lalu isi
  await page.evaluate(function() {
    // Make all hidden form sections visible
    document.querySelectorAll('.collapse, [style*="display:none"], [style*="display: none"]').forEach(function(el) {
      el.style.display = 'block';
    });
  });
  await page.waitForTimeout(500);

  // Isi PassPercentage (pakai JS)
  await page.evaluate(function() {
    const inp = document.querySelector('input[name="PassPercentage"]');
    if (inp) { inp.value = '60'; inp.dispatchEvent(new Event('change', {bubbles:true})); }
  });

  // Set ScheduleDate (besok)
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const dateStr = tomorrow.toISOString().split('T')[0];
  await page.evaluate(function(d) {
    const inp = document.querySelector('input[name="ScheduleDate"]');
    if (inp) { inp.value = d; inp.dispatchEvent(new Event('change', {bubbles:true})); }
    const hiddenSched = document.querySelector('input[name="Schedule"]');
    if (hiddenSched) { hiddenSched.value = d + 'T00:00:00'; }
  }, dateStr);

  // Centang Arsyad (via JS)
  const arsyadChecked = await page.evaluate(function(cbId) {
    const cb = document.getElementById(cbId);
    if (cb) {
      cb.checked = true;
      cb.dispatchEvent(new Event('change', { bubbles: true }));
      return true;
    }
    return false;
  }, ARSYAD_CHECKBOX_ID);
  log('Arsyad checked: ' + arsyadChecked);

  // IsTokenRequired = false
  await page.evaluate(function() {
    const cb = document.querySelector('input[id="IsTokenRequired"]');
    if (cb) { cb.checked = false; cb.dispatchEvent(new Event('change', {bubbles:true})); }
  });

  // Status: Open (via JS)
  await page.evaluate(function() {
    const sel = document.querySelector('select[name="Status"]');
    if (sel) { sel.value = 'Open'; sel.dispatchEvent(new Event('change', {bubbles:true})); }
  });
  log('Form fields filled via JS');

  // Screenshot sebelum submit
  await page.screenshot({ path: 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/setup-form-before-submit.png' });

  // Submit form
  log('Submitting form...');
  await page.evaluate(function() {
    const btn = document.querySelector('button[type="submit"], input[type="submit"]');
    if (btn) btn.click();
  });
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(3000);
  log('After submit: ' + page.url());

  const afterBody = await page.evaluate(function() { return document.body.innerText.substring(0, 500); });
  log('After body: ' + afterBody);

  await page.screenshot({ path: 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/setup-form-after-submit.png' });

  // Cari assessment ID baru
  const assessmentId = await page.evaluate(function() {
    // Cek TempData flash atau success message yang mungkin ada ID
    const text = document.body.innerText;
    const match = text.match(/ID[:\s]+(\d+)/i) || text.match(/assessment[:\s#]+(\d+)/i);
    return match ? match[1] : null;
  });
  log('Assessment ID dari halaman: ' + assessmentId);

  // Coba cari di daftar assessment Arsyad
  await page.goto(`${BASE_URL}/CMP/Assessment`, { waitUntil: 'networkidle', timeout: 90000 });
  const arsyadLoginCtx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const arsyadPage = await arsyadLoginCtx.newPage();
  await arsyadPage.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle', timeout: 90000 });
  await arsyadPage.fill('input[type="email"]', 'mohammad.arsyad@pertamina.com');
  await arsyadPage.fill('input[type="password"]', 'Pertamina@2026');
  await arsyadPage.evaluate(function() { document.querySelector('button[type="submit"]').click(); });
  await arsyadPage.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await arsyadPage.waitForTimeout(2000);
  await arsyadPage.goto(`${BASE_URL}/CMP/Assessment`, { waitUntil: 'networkidle', timeout: 90000 });

  const arsyadAssessments = await arsyadPage.evaluate(function(title) {
    const cards = Array.from(document.querySelectorAll('.card, tr'));
    const results = [];
    for (const card of cards) {
      const text = card.innerText || '';
      if (text.includes(title) || text.includes('UAT Timer')) {
        const link = card.querySelector('a, button');
        const hrefMatch = (link && link.href) ? link.href.match(/StartExam\/(\d+)/) : null;
        results.push({ text: text.substring(0, 150), href: link ? link.href : '', id: hrefMatch ? hrefMatch[1] : null });
      }
    }
    return { results, allText: document.body.innerText.substring(0, 600) };
  }, 'UAT Timer Test Arsyad');
  log('Arsyad assessment list: ' + JSON.stringify(arsyadAssessments.results));
  log('All text: ' + arsyadAssessments.allText);

  await browser.close();
  await arsyadLoginCtx.close();

  // Simpan hasil
  const result = {
    timestamp: new Date().toISOString(),
    assessmentId: assessmentId,
    arsyadAssessments: arsyadAssessments.results,
    status: arsyadAssessments.results.length > 0 ? 'ASSESSMENT_FOUND' : 'ASSESSMENT_NOT_FOUND'
  };
  fs.writeFileSync('C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/setup-267-result.json', JSON.stringify(result, null, 2));
  log('Setup result saved');
  log('Status: ' + result.status);
}

main().catch(function(e) {
  console.error('Fatal error:', e.message);
  process.exit(1);
});
