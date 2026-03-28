// Versi 2: Edit duration assessment 10 via force click
const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';

function log(msg) { console.log('[' + new Date().toISOString().substring(11,19) + '] ' + msg); }

async function main() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  await page.goto(BASE_URL + '/Account/Login', { waitUntil: 'networkidle', timeout: 90000 });
  await page.fill('input[type="email"]', 'admin@pertamina.com');
  await page.fill('input[type="password"]', '123456');
  await page.evaluate(function() { document.querySelector('button[type="submit"]').click(); });
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(2000);
  log('Login: ' + page.url());

  await page.goto(BASE_URL + '/Admin/EditAssessment/10', { waitUntil: 'networkidle', timeout: 90000 });
  log('Edit form loaded');

  // Make all form elements visible and fill DurationMinutes
  await page.evaluate(function() {
    // Force all hidden elements visible
    document.querySelectorAll('*').forEach(function(el) {
      const s = window.getComputedStyle(el);
      if (s.display === 'none' && (el.tagName === 'INPUT' || el.tagName === 'BUTTON' || el.tagName === 'SELECT')) {
        el.style.setProperty('display', 'block', 'important');
      }
    });
    // Set duration
    const dur = document.getElementById('DurationMinutes');
    if (dur) {
      dur.value = '2';
      dur.dispatchEvent(new Event('change', { bubbles: true }));
    }
    // Force submit button visible
    const btn = document.querySelector('button[type="submit"]');
    if (btn) {
      btn.style.setProperty('display', 'inline-block', 'important');
      btn.style.setProperty('visibility', 'visible', 'important');
      btn.style.setProperty('opacity', '1', 'important');
    }
  });
  await page.waitForTimeout(500);

  // Check button visibility
  const btnVis = await page.evaluate(function() {
    const btn = document.querySelector('button[type="submit"]');
    if (!btn) return 'NOT FOUND';
    const s = window.getComputedStyle(btn);
    return { display: s.display, visibility: s.visibility, opacity: s.opacity };
  });
  log('Submit button style: ' + JSON.stringify(btnVis));

  // Try force click
  try {
    await page.click('button[type="submit"]', { force: true, timeout: 5000 });
    await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
    await page.waitForTimeout(2000);
    log('After submit: ' + page.url());
    const body = await page.evaluate(function() { return document.body.innerText.substring(0,200); });
    log('Body: ' + body);
  } catch (e) {
    log('Force click error: ' + e.message.substring(0,100));

    // Fallback: manual fetch dengan anti-forgery token
    log('Trying fetch approach...');
    const result = await page.evaluate(function() {
      const token = document.querySelector('input[name="__RequestVerificationToken"]');
      const form = document.querySelector('form');
      if (!form || !token) return { error: 'form or token not found' };

      // Collect all form data
      const data = new FormData(form);
      const entries = [];
      for (const pair of data.entries()) {
        entries.push({ key: pair[0], value: pair[1] });
      }
      return { tokenFound: !!token, tokenValue: token.value.substring(0,20), entries: entries };
    });
    log('Form data: ' + JSON.stringify(result));

    // Submit via fetch
    const fetchResult = await page.evaluate(function() {
      const form = document.getElementById('editAssessmentForm') || document.querySelector('form');
      if (!form) return { error: 'form not found' };

      // Set duration before collecting form data
      const dur = document.getElementById('DurationMinutes');
      if (dur) dur.value = '2';

      const formData = new FormData(form);
      return fetch(form.action, {
        method: 'POST',
        body: formData,
        credentials: 'include'
      }).then(function(r) {
        return { status: r.status, url: r.url, redirected: r.redirected };
      }).catch(function(e) {
        return { error: e.message };
      });
    });
    log('Fetch result: ' + JSON.stringify(fetchResult));

    await page.waitForTimeout(2000);
    log('After fetch: ' + page.url());
  }

  // Verify change
  await page.goto(BASE_URL + '/Admin/EditAssessment/10', { waitUntil: 'networkidle', timeout: 90000 });
  const newDur = await page.evaluate(function() {
    const inp = document.getElementById('DurationMinutes');
    return inp ? inp.value : 'not found';
  });
  log('Verification - Duration is now: ' + newDur);

  await browser.close();

  const success = newDur === '2';
  log('RESULT: ' + (success ? 'SUCCESS - duration changed to 2 min' : 'FAIL - duration is ' + newDur));
  fs.writeFileSync('C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/edit-duration2-result.json', JSON.stringify({ duration: newDur, success }));
}

main().catch(function(e) {
  console.error('Fatal:', e.message);
  process.exit(1);
});
