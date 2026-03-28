// UAT Phase 267: Resilience & Edge Cases
// Run: node uat-267-test.js
// Tests: EDGE-01 (koneksi putus), EDGE-02 (tab close + resume modal),
//        EDGE-03 (timer resume), EDGE-04 (jawaban masih tercentang),
//        EDGE-05 (progress counter), EDGE-06 (browser refresh)
//
// Worker Regan = Moch Regan Sabela Widyadhana (moch.widyadhana@pertamina.com)

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const RESULTS = [];
const NAV_TIMEOUT = 60000; // 60s for slow server
const SCREENSHOTS_DIR = 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases';

const ADMIN_EMAIL = 'admin@pertamina.com';
const ADMIN_PASSWORD = '123456';

// Worker Regan = Moch Regan Sabela Widyadhana (discovered from ManageWorkers)
const REGAN_EMAIL = 'moch.widyadhana@pertamina.com';
const REGAN_PASSWORD = 'Balikpapan@2026';

function log(msg) {
  const ts = new Date().toISOString().substring(11, 19);
  console.log(`[${ts}] ${msg}`);
}

async function safeScreenshot(page, name) {
  try {
    const path = `${SCREENSHOTS_DIR}/${name}.png`;
    await page.screenshot({ path, timeout: 15000 });
    log(`Screenshot saved: ${name}.png`);
  } catch (e) {
    log(`Screenshot failed (${name}): ${e.message.split('\n')[0]}`);
  }
}

async function goto(page, url) {
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await page.waitForTimeout(3000);
}

async function login(page, email, password) {
  await goto(page, `${BASE_URL}/Account/Login`);
  await page.fill('input[name="Email"], input[type="email"]', email);
  await page.fill('input[name="Password"], input[type="password"]', password);
  await page.evaluate(() => {
    const btn = document.querySelector('button[type="submit"]');
    if (btn) btn.click();
  });
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(3000);
  const url = page.url();
  const success = !url.includes('/Account/Login');
  log(`Login ${email}: ${success ? 'OK' : 'FAIL'} (url: ${url})`);
  return success;
}

function parseTimer(s) {
  if (!s || !s.includes(':')) return -1;
  const parts = s.split(':');
  return parseInt(parts[0]) * 60 + parseInt(parts[1]);
}

// Find open assessment for Regan from his assessment list
async function findReganAssessment(browser) {
  log('=== Finding open assessment for Regan ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  let assessmentId = null;

  try {
    const loginOk = await login(page, REGAN_EMAIL, REGAN_PASSWORD);
    if (!loginOk) {
      log('Regan login failed');
      await ctx.close();
      return null;
    }

    await goto(page, `${BASE_URL}/CMP/Assessment`);
    await safeScreenshot(page, 'regan-assessment-list');

    const info = await page.evaluate(() => {
      const bodyText = document.body.innerText.substring(0, 600);
      const links = Array.from(document.querySelectorAll('a[href*="StartExam"]')).map(a => a.href);
      const cardTexts = Array.from(document.querySelectorAll('.card')).map(c => c.innerText.substring(0, 100));
      return { bodyText, links, cardTexts };
    });
    log(`Assessment list body: ${info.bodyText}`);
    log(`StartExam links: ${JSON.stringify(info.links)}`);

    if (info.links.length > 0) {
      const parts = info.links[0].split('/');
      assessmentId = parseInt(parts[parts.length - 1]);
      log(`Found assessment ID: ${assessmentId}`);
    }
  } catch (e) {
    log(`Error finding assessment: ${e.message.split('\n')[0]}`);
  }

  await ctx.close();
  return assessmentId;
}

async function scenario_regan_resilience(browser, assessmentId) {
  log('=== SKENARIO REGAN RESILIENCE (EDGE-01 sampai EDGE-06) ===');
  const result = { scenario: 'Regan - Resilience Edge Cases', checks: [] };

  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();

  try {
    // Login as Regan
    const loginOk = await login(page, REGAN_EMAIL, REGAN_PASSWORD);
    result.checks.push({ id: 'SETUP-LOGIN', desc: `Login Regan (${REGAN_EMAIL})`, pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    // Navigate to exam
    let examUrl = assessmentId
      ? `${BASE_URL}/CMP/StartExam/${assessmentId}`
      : `${BASE_URL}/CMP/Assessment`;

    if (assessmentId) {
      await goto(page, examUrl);
    } else {
      await goto(page, `${BASE_URL}/CMP/Assessment`);
      const links = await page.evaluate(() =>
        Array.from(document.querySelectorAll('a[href*="StartExam"]')).map(a => a.href)
      );
      if (links.length > 0) {
        examUrl = links[0];
        await goto(page, examUrl);
      }
    }

    const onExam = page.url().includes('StartExam');
    result.checks.push({ id: 'SETUP-EXAM', desc: 'Berhasil masuk ujian', pass: onExam, detail: `URL: ${page.url()}` });
    if (!onExam) {
      const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 400));
      log(`Not on exam. Body: ${bodyText}`);
      RESULTS.push(result);
      await ctx.close();
      return;
    }

    examUrl = page.url(); // capture actual URL with session
    log(`Exam URL: ${examUrl}`);
    await page.waitForTimeout(3000);
    await safeScreenshot(page, 'edge-00-exam-start');

    // Handle resume modal if already shown (previous session)
    const resumeAlreadyVisible = await page.evaluate(() => {
      const modal = document.getElementById('resumeConfirmModal');
      return modal ? (modal.classList.contains('show') || window.getComputedStyle(modal).display !== 'none') : false;
    });
    if (resumeAlreadyVisible) {
      log('Resume modal on entry (pre-existing session) — clicking continue');
      await page.evaluate(() => {
        const btn = document.getElementById('resumeConfirmBtn');
        if (btn) btn.click();
      });
      await page.waitForTimeout(2000);
    }

    // Get initial page state
    const initInfo = await page.evaluate(() => {
      const radios = document.querySelectorAll('input.exam-radio');
      const netBadge = document.getElementById('networkStatusBadge');
      const timer = document.getElementById('examTimer');
      return {
        radioCount: radios.length,
        netBadge: netBadge ? netBadge.textContent.trim() : 'NOT FOUND',
        timerText: timer ? timer.innerText.trim() : 'NOT FOUND'
      };
    });
    log(`Init: radios=${initInfo.radioCount}, badge="${initInfo.netBadge}", timer="${initInfo.timerText}"`);
    result.checks.push({ id: 'SETUP-STATE', desc: 'networkStatusBadge dan timer tersedia', pass: initInfo.netBadge !== 'NOT FOUND' && initInfo.timerText !== 'NOT FOUND', detail: `badge="${initInfo.netBadge}", timer="${initInfo.timerText}"` });

    // =========================================================
    // EDGE-01: Lost Connection
    // =========================================================
    log('--- EDGE-01: Lost Connection (route block SaveAnswer) ---');

    // Answer first question normally (to confirm save works)
    if (initInfo.radioCount > 0) {
      await page.evaluate(() => {
        const radio = document.querySelector('input.exam-radio');
        if (radio) radio.click();
      });
      await page.waitForTimeout(3000); // wait for autosave
      const badgeAfterNormal = await page.evaluate(() => {
        const b = document.getElementById('networkStatusBadge');
        return b ? b.textContent.trim() : '';
      });
      log(`Badge after normal save: "${badgeAfterNormal}"`);
    }

    // Block SaveAnswer (simulate offline)
    await page.route('**/CMP/SaveAnswer', route => route.abort());
    log('Route blocked: SaveAnswer aborted');

    // Answer another question while offline
    await page.evaluate(() => {
      const radios = document.querySelectorAll('input.exam-radio');
      // Find an unchecked radio to click
      for (let i = 0; i < radios.length; i++) {
        if (!radios[i].checked) { radios[i].click(); return; }
      }
      // If all checked, click first anyway to trigger save attempt
      if (radios[1]) radios[1].click();
    });

    // Wait for retry exhaustion: 3 attempts = 0s + 1s + 3s + ~1s overhead = ~6s
    await page.waitForTimeout(8000);
    await safeScreenshot(page, 'edge-01-offline-state');

    const offlineState = await page.evaluate(() => {
      const b = document.getElementById('networkStatusBadge');
      const pending = typeof pendingAnswers !== 'undefined' ? pendingAnswers.length : -1;
      return {
        badge: b ? b.textContent.trim() : '',
        pendingCount: pending
      };
    });
    log(`Offline state: badge="${offlineState.badge}", pending=${offlineState.pendingCount}`);

    const isOffline = offlineState.badge.toLowerCase().includes('offline') ||
                      offlineState.badge.toLowerCase().includes('gagal') ||
                      offlineState.badge.toLowerCase().includes('error');
    result.checks.push({
      id: 'EDGE-01-OFFLINE',
      desc: 'Offline badge muncul saat koneksi putus',
      pass: isOffline,
      detail: `badge="${offlineState.badge}"`
    });
    result.checks.push({
      id: 'EDGE-01-QUEUE',
      desc: 'pendingAnswers queue terisi saat offline',
      pass: offlineState.pendingCount > 0,
      detail: `pendingCount=${offlineState.pendingCount}`
    });

    // Restore connection
    await page.unroute('**/CMP/SaveAnswer');
    log('Route restored: SaveAnswer unblocked');

    // Trigger a new answer to kick off flush check
    await page.evaluate(() => {
      const radio = document.querySelector('input.exam-radio');
      if (radio) radio.click();
    });

    // Wait for flush to happen
    await page.waitForTimeout(10000);
    await safeScreenshot(page, 'edge-01-after-reconnect');

    const afterRestore = await page.evaluate(() => {
      const b = document.getElementById('networkStatusBadge');
      const pending = typeof pendingAnswers !== 'undefined' ? pendingAnswers.length : -1;
      return {
        badge: b ? b.textContent.trim() : '',
        pendingCount: pending
      };
    });
    log(`After restore: badge="${afterRestore.badge}", pending=${afterRestore.pendingCount}`);

    const isFlushed = afterRestore.badge.toLowerCase().includes('tersimpan') ||
                      afterRestore.badge.toLowerCase().includes('saved') ||
                      afterRestore.pendingCount === 0;
    result.checks.push({
      id: 'EDGE-01-FLUSH',
      desc: 'Pending answers di-flush setelah koneksi pulih',
      pass: isFlushed,
      detail: `badge="${afterRestore.badge}", pending=${afterRestore.pendingCount}`
    });

    // =========================================================
    // EDGE-02 + EDGE-03 + EDGE-04 + EDGE-05: Tab Close + Resume
    // =========================================================
    log('--- EDGE-02,03,04,05: Tab Close + Resume ---');

    // Navigate to page 1 if on page 0 (so resume modal triggers)
    const totalPages = await page.evaluate(() => typeof TOTAL_PAGES !== 'undefined' ? TOTAL_PAGES : 1);
    if (totalPages > 1) {
      await page.evaluate(() => { if (typeof changePage === 'function') changePage(1); });
      await page.waitForTimeout(2000);
      log('Navigated to page 1 for resume test');
    }

    // Capture state before close
    const beforeClose = await page.evaluate(() => {
      const timer = document.getElementById('examTimer');
      const answered = document.getElementById('answeredProgress');
      const checkedRadios = document.querySelectorAll('input.exam-radio:checked').length;
      return {
        timerText: timer ? timer.innerText.trim() : '',
        answeredText: answered ? answered.innerText.trim() : '',
        checkedRadios
      };
    });
    log(`Before close: timer="${beforeClose.timerText}", answered="${beforeClose.answeredText}", checked=${beforeClose.checkedRadios}`);

    // Force session progress save before close
    await page.evaluate(() => {
      if (typeof saveSessionProgress === 'function') saveSessionProgress();
    });
    await page.waitForTimeout(2000);

    const closeTime = Date.now();

    // Close tab
    await page.close();
    log('Tab closed');
    await new Promise(r => setTimeout(r, 1500));

    // Reopen in same context (preserves auth cookies)
    const newPage = await ctx.newPage();
    await goto(newPage, examUrl);
    await safeScreenshot(newPage, 'edge-02-after-reopen');

    const reopenTime = Date.now();
    log(`Time between close and reopen: ${Math.floor((reopenTime - closeTime) / 1000)}s`);

    // EDGE-02: Resume modal muncul
    const resumeState = await newPage.evaluate(() => {
      const modal = document.getElementById('resumeConfirmModal');
      const resumeNum = document.getElementById('resumePageNum');
      const modalShow = modal ? (modal.classList.contains('show') || window.getComputedStyle(modal).display !== 'none') : false;
      const isResume = typeof IS_RESUME !== 'undefined' ? IS_RESUME : false;
      const resumePage = typeof RESUME_PAGE !== 'undefined' ? RESUME_PAGE : -1;
      return {
        modalVisible: modalShow,
        resumeNumText: resumeNum ? resumeNum.innerText.trim() : '',
        isResume,
        resumePage
      };
    });
    log(`Resume state after reopen: ${JSON.stringify(resumeState)}`);

    const resumeOk = resumeState.modalVisible || resumeState.isResume;
    result.checks.push({
      id: 'EDGE-02',
      desc: 'Resume modal muncul setelah tab close',
      pass: resumeOk,
      detail: `modalVisible=${resumeState.modalVisible}, IS_RESUME=${resumeState.isResume}, RESUME_PAGE=${resumeState.resumePage}`
    });

    // EDGE-03: Timer lanjut (tidak reset ke penuh)
    const timerAfterReopen = await newPage.evaluate(() => {
      const t = document.getElementById('examTimer');
      return t ? t.innerText.trim() : '';
    });
    log(`Timer: before="${beforeClose.timerText}", after="${timerAfterReopen}"`);

    const tBefore = parseTimer(beforeClose.timerText);
    const tAfter = parseTimer(timerAfterReopen);
    // Timer after should be <= timer before (time passed), tolerance +15s for load time
    const timerContinued = tAfter !== -1 && tBefore !== -1 && tAfter <= (tBefore + 15);
    result.checks.push({
      id: 'EDGE-03',
      desc: 'Timer lanjut dari sisa waktu (tidak reset ke penuh)',
      pass: timerContinued,
      detail: `before="${beforeClose.timerText}"(${tBefore}s), after="${timerAfterReopen}"(${tAfter}s)`
    });
    await safeScreenshot(newPage, 'edge-03-timer-check');

    // Click resume button if modal visible
    if (resumeState.modalVisible) {
      await newPage.evaluate(() => {
        const btn = document.getElementById('resumeConfirmBtn');
        if (btn) btn.click();
      });
      await newPage.waitForTimeout(2000);
      await safeScreenshot(newPage, 'edge-02-resumed');
    }

    // EDGE-04: Jawaban masih tercentang
    const checkedAfterResume = await newPage.evaluate(() => {
      return document.querySelectorAll('input.exam-radio:checked').length;
    });
    log(`Checked radios after resume: ${checkedAfterResume}`);
    result.checks.push({
      id: 'EDGE-04',
      desc: 'Jawaban masih tercentang setelah resume',
      pass: checkedAfterResume > 0,
      detail: `checkedCount=${checkedAfterResume} (before close: ${beforeClose.checkedRadios})`
    });
    await safeScreenshot(newPage, 'edge-04-answers-restored');

    // EDGE-05: Progress counter akurat
    const progressAfter = await newPage.evaluate(() => {
      const el = document.getElementById('answeredProgress');
      if (!el) return { text: 'NOT FOUND', count: -1 };
      const text = el.innerText.trim();
      const m = text.match(/(\d+)/);
      return { text, count: m ? parseInt(m[1]) : 0 };
    });
    log(`Progress after resume: "${progressAfter.text}"`);
    result.checks.push({
      id: 'EDGE-05',
      desc: 'Progress counter akurat setelah resume (answered > 0)',
      pass: progressAfter.count > 0,
      detail: `progress="${progressAfter.text}"`
    });

    // =========================================================
    // EDGE-06: Browser Refresh
    // =========================================================
    log('--- EDGE-06: Browser Refresh ---');

    // Answer a question to ensure there are answers to preserve
    await newPage.evaluate(() => {
      const radios = document.querySelectorAll('input.exam-radio');
      for (let i = 0; i < radios.length; i++) {
        if (!radios[i].checked) { radios[i].click(); return; }
      }
    });
    await newPage.waitForTimeout(3000); // wait autosave

    // Record state before refresh
    const beforeRefresh = await newPage.evaluate(() => {
      const timer = document.getElementById('examTimer');
      const checked = document.querySelectorAll('input.exam-radio:checked').length;
      return { timerText: timer ? timer.innerText.trim() : '', checkedCount: checked };
    });
    log(`Before refresh: timer="${beforeRefresh.timerText}", checked=${beforeRefresh.checkedCount}`);

    // Reload
    await newPage.reload({ waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    await newPage.waitForTimeout(4000);
    await safeScreenshot(newPage, 'edge-06-after-refresh');

    // Handle resume modal if shown
    const refreshResumeModal = await newPage.evaluate(() => {
      const modal = document.getElementById('resumeConfirmModal');
      return modal ? (modal.classList.contains('show') || window.getComputedStyle(modal).display !== 'none') : false;
    });
    if (refreshResumeModal) {
      log('Resume modal after refresh - clicking continue');
      await newPage.evaluate(() => {
        const btn = document.getElementById('resumeConfirmBtn');
        if (btn) btn.click();
      });
      await newPage.waitForTimeout(2000);
    }

    // Verify state after refresh
    const afterRefresh = await newPage.evaluate(() => {
      const timer = document.getElementById('examTimer');
      const checked = document.querySelectorAll('input.exam-radio:checked').length;
      return { timerText: timer ? timer.innerText.trim() : '', checkedCount: checked };
    });
    log(`After refresh: timer="${afterRefresh.timerText}", checked=${afterRefresh.checkedCount}`);
    await safeScreenshot(newPage, 'edge-06-state-check');

    result.checks.push({
      id: 'EDGE-06-ANSWERS',
      desc: 'Browser refresh: jawaban tidak hilang',
      pass: afterRefresh.checkedCount > 0,
      detail: `before=${beforeRefresh.checkedCount}, after=${afterRefresh.checkedCount}`
    });

    const tBeforeRefresh = parseTimer(beforeRefresh.timerText);
    const tAfterRefresh = parseTimer(afterRefresh.timerText);
    const timerNotReset = tAfterRefresh !== -1 && tAfterRefresh > 0 && tAfterRefresh <= (tBeforeRefresh + 30);
    result.checks.push({
      id: 'EDGE-06-TIMER',
      desc: 'Browser refresh: timer lanjut (tidak reset ke durasi penuh)',
      pass: timerNotReset,
      detail: `before="${beforeRefresh.timerText}"(${tBeforeRefresh}s), after="${afterRefresh.timerText}"(${tAfterRefresh}s)`
    });

  } catch (err) {
    log(`ERROR in Regan Resilience scenario: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    try { await safeScreenshot(page, 'edge-error'); } catch (_) {}
  }

  RESULTS.push(result);
  await ctx.close();
}

async function main() {
  log('Starting UAT Phase 267 — Resilience & Edge Cases');
  const browser = await chromium.launch({ headless: true });

  try {
    // Find open assessment for Regan
    const assessmentId = await findReganAssessment(browser);
    log(`Assessment ID: ${assessmentId}`);

    // Run resilience scenarios EDGE-01 to EDGE-06
    await scenario_regan_resilience(browser, assessmentId);

  } finally {
    await browser.close();
  }

  // Print summary
  log('\n========== UAT RESULTS SUMMARY ==========');
  let totalPass = 0, totalFail = 0;
  for (const scenario of RESULTS) {
    log(`\n${scenario.scenario}:`);
    if (scenario.error) log(`  ERROR: ${scenario.error}`);
    for (const check of scenario.checks) {
      const status = check.pass ? 'PASS' : 'FAIL';
      const detail = check.detail ? ` [${check.detail}]` : '';
      log(`  [${status}] ${check.id}: ${check.desc}${detail}`);
      if (check.pass) totalPass++; else totalFail++;
    }
  }
  log(`\nTotal: ${totalPass} PASS, ${totalFail} FAIL`);

  const resultPath = 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases/uat-267-results.json';
  fs.writeFileSync(resultPath, JSON.stringify(RESULTS, null, 2));
  log(`Results saved to ${resultPath}`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
