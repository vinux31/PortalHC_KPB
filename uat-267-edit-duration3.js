// Versi 3: Edit duration menggunakan fetch API langsung dengan form data lengkap
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

  // Get anti-forgery token dan current form values
  const formState = await page.evaluate(function() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    const title = document.querySelector('input[name="Title"]');
    const category = document.querySelector('select[name="Category"]');
    const status = document.querySelector('select[name="Status"]');
    const duration = document.getElementById('DurationMinutes');
    const passPerc = document.getElementById('PassPercentage');
    const schedule = document.getElementById('ScheduleHidden');
    const allowReview = document.querySelector('input[name="AllowAnswerReview"][type="checkbox"]');
    const genCert = document.querySelector('input[name="GenerateCertificate"][type="checkbox"]');
    const isToken = document.querySelector('input[name="IsTokenRequired"][type="checkbox"]');
    const accessToken = document.getElementById('AccessToken');
    const examWClose = document.getElementById('ExamWindowCloseDate');

    return {
      token: token ? token.value : null,
      title: title ? title.value : 'UAT OJT Test 2 - No Token',
      category: category ? category.value : 'On Job Training (OJT)',
      status: status ? status.value : 'Open',
      duration: duration ? duration.value : '60',
      passPerc: passPerc ? passPerc.value : '80',
      schedule: schedule ? schedule.value : '2026-03-29T08:00:00',
      allowReview: allowReview ? allowReview.checked : true,
      genCert: genCert ? genCert.checked : false,
      isToken: isToken ? isToken.checked : false,
      accessToken: accessToken ? accessToken.value : '',
      examWClose: examWClose ? examWClose.value : ''
    };
  });
  log('Form state: ' + JSON.stringify(formState));

  if (!formState.token) {
    log('ERROR: No anti-forgery token found!');
    await browser.close();
    return;
  }

  // Submit via fetch dengan body yang lengkap, DurationMinutes = 2
  const fetchResult = await page.evaluate(function(state) {
    const body = new URLSearchParams();
    body.append('__RequestVerificationToken', state.token);
    body.append('Title', state.title);
    body.append('Category', state.category);
    body.append('Status', 'Open');
    body.append('DurationMinutes', '2');  // Changed to 2!
    body.append('Schedule', state.schedule);
    body.append('PassPercentage', state.passPerc);
    body.append('AllowAnswerReview', 'true');
    body.append('GenerateCertificate', 'false');
    body.append('IsTokenRequired', 'false');
    body.append('ExamWindowCloseDate', '');

    return fetch('/KPB-PortalHC/Admin/EditAssessment/10', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'RequestVerificationToken': state.token
      },
      body: body.toString(),
      credentials: 'include'
    }).then(function(r) {
      return { status: r.status, url: r.url, redirected: r.redirected };
    }).catch(function(e) {
      return { error: e.message };
    });
  }, formState);
  log('Fetch result: ' + JSON.stringify(fetchResult));

  await page.waitForTimeout(2000);

  // Verify
  await page.goto(BASE_URL + '/Admin/EditAssessment/10', { waitUntil: 'networkidle', timeout: 90000 });
  const newDur = await page.evaluate(function() {
    const inp = document.getElementById('DurationMinutes');
    return inp ? inp.value : 'not found';
  });
  log('Duration after edit: ' + newDur);

  await browser.close();

  const success = newDur === '2';
  log('RESULT: ' + (success ? 'SUCCESS - Duration changed to 2 minutes!' : 'FAIL - Duration is ' + newDur));

  fs.writeFileSync('C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/edit-duration3-result.json', JSON.stringify({ duration: newDur, success, fetchResult }));
}

main().catch(function(e) {
  console.error('Fatal:', e.message);
  process.exit(1);
});
