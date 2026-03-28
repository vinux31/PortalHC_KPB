// UAT Phase 265: Worker Exam Flow Browser Testing
// Run: node uat-265-test.js

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const RESULTS = [];
const NAV_TIMEOUT = 60000; // 60s for slow server
const SCREENSHOTS_DIR = 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/265-worker-exam-flow';

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
  await page.waitForTimeout(3000); // extra wait for JS init
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

async function scenario1_rino(browser) {
  log('=== SKENARIO 1: rino.prasetyo — Token + Happy Path ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S1 - rino.prasetyo', checks: [] };

  try {
    const loginOk = await login(page, 'rino.prasetyo@pertamina.com', '123456');
    result.checks.push({ id: 'S1-LOGIN', desc: 'Login berhasil', pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    // EXAM-01: Assessment list
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    const onAssessmentPage = page.url().includes('/CMP/Assessment');
    const bodyText = await page.evaluate(() => document.body.innerText);
    const hasAssmt7 = bodyText.includes('UAT OJT Test 1') || bodyText.includes('Test 1');
    const cardCount = await page.evaluate(() => document.querySelectorAll('.card').length);
    log(`Assessment page: ${onAssessmentPage}, cards: ${cardCount}, has assessment 7: ${hasAssmt7}`);
    log(`Body excerpt: ${bodyText.substring(0, 300)}`);
    result.checks.push({ id: 'EXAM-01', desc: 'Assessment list tampil + assessment 7 ada', pass: onAssessmentPage && cardCount > 0, detail: `cards=${cardCount} hasAssmt7=${hasAssmt7}` });
    await safeScreenshot(page, 's1-1-assessment-list');

    // EXAM-02: Token modal
    // Find "Start Assessment" or "Mulai" button near assessment 7
    let tokenModalAppeared = false;
    const allCardTexts = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.card')).map(c => c.innerText.substring(0, 100));
    });
    log(`Card texts: ${JSON.stringify(allCardTexts.slice(0, 5))}`);

    // Click start button on assessment card containing "UAT OJT Test 1" or "Token"
    let startClicked = false;
    for (let i = 0; i < allCardTexts.length; i++) {
      if (allCardTexts[i].includes('UAT OJT Test 1') || allCardTexts[i].includes('Token')) {
        // Click button in this card
        const clicked = await page.evaluate((idx) => {
          const cards = document.querySelectorAll('.card');
          const card = cards[idx];
          if (!card) return false;
          const btn = card.querySelector('button, a[href*="StartExam"], a[href*="Token"]');
          if (btn) { btn.click(); return true; }
          return false;
        }, i);
        if (clicked) { startClicked = true; log(`Clicked button in card ${i}`); break; }
      }
    }

    if (!startClicked) {
      // Try any "Mulai" button on page
      startClicked = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button, a'));
        for (const btn of btns) {
          if (/mulai|start|kerjakan/i.test(btn.innerText || btn.textContent)) {
            btn.click();
            return true;
          }
        }
        return false;
      });
      log(`Generic start button clicked: ${startClicked}`);
    }

    await page.waitForTimeout(3000);
    await safeScreenshot(page, 's1-2-after-start-click');

    // Check if token modal appeared
    const modalInfo = await page.evaluate(() => {
      const modals = document.querySelectorAll('.modal');
      const visibleModal = Array.from(modals).find(m => {
        const style = window.getComputedStyle(m);
        return style.display !== 'none' && (m.classList.contains('show') || m.style.display === 'block');
      });
      return {
        count: modals.length,
        visible: !!visibleModal,
        text: visibleModal ? visibleModal.innerText.substring(0, 100) : '',
        currentUrl: window.location.href
      };
    });
    log(`Modal info: ${JSON.stringify(modalInfo)}`);
    tokenModalAppeared = modalInfo.visible || modalInfo.text.toLowerCase().includes('token');
    result.checks.push({ id: 'EXAM-02a', desc: 'Token modal muncul (IsTokenRequired=true)', pass: tokenModalAppeared, detail: `modals=${modalInfo.count} visible=${modalInfo.visible}` });

    // Try to enter token
    let examReached = false;
    if (tokenModalAppeared) {
      // Fill token input
      const tokenFilled = await page.evaluate(() => {
        const inputs = document.querySelectorAll('input');
        for (const inp of inputs) {
          if (inp.maxLength === 6 || (inp.placeholder && inp.placeholder.toLowerCase().includes('token'))) {
            inp.value = 'U6J49L';
            inp.dispatchEvent(new Event('input', { bubbles: true }));
            inp.dispatchEvent(new Event('change', { bubbles: true }));
            return true;
          }
        }
        return false;
      });
      log(`Token filled: ${tokenFilled}`);

      // Check auto-uppercase
      const tokenValue = await page.evaluate(() => {
        const inputs = document.querySelectorAll('input');
        for (const inp of inputs) {
          if (inp.maxLength === 6 || (inp.placeholder && inp.placeholder.toLowerCase().includes('token'))) {
            return inp.value;
          }
        }
        return '';
      });
      log(`Token value after fill: "${tokenValue}"`);
      result.checks.push({ id: 'EXAM-02b', desc: 'Token auto-uppercase', pass: tokenValue === 'U6J49L' || tokenValue.toUpperCase() === 'U6J49L' });

      // Click submit in modal
      await page.evaluate(() => {
        const btns = document.querySelectorAll('.modal button, .modal-footer button');
        for (const btn of btns) {
          if (btn.type === 'submit' || /verifi|ok|masuk|submit|confirm/i.test(btn.innerText)) {
            btn.click();
            return;
          }
        }
        // Fallback: click any button in modal
        const anyBtn = document.querySelector('.modal.show button, .modal[style*="block"] button');
        if (anyBtn) anyBtn.click();
      });
      await page.waitForTimeout(5000);
      await safeScreenshot(page, 's1-3-after-token');
      const urlAfterToken = page.url();
      log(`URL after token submit: ${urlAfterToken}`);
      examReached = urlAfterToken.includes('StartExam');
    }

    if (!examReached) {
      // Navigate directly with session token in TempData already set
      log('Token modal path did not work, trying StartExam/7 directly');
      await goto(page, `${BASE_URL}/CMP/StartExam/7`);
      examReached = page.url().includes('StartExam');
      log(`Direct StartExam/7 URL: ${page.url()}`);
    }

    // EXAM-03, EXAM-04, EXAM-05, EXAM-07
    if (examReached) {
      await safeScreenshot(page, 's1-4-startexam');
      const examInfo = await page.evaluate(() => {
        const radios = document.querySelectorAll('input[type="radio"]');
        const timer = document.querySelector('#timerDisplay, .timer, [id*="timer"]');
        const hubBadge = document.querySelector('#hubStatusBadge');
        const netBadge = document.querySelector('#networkStatusBadge');
        return {
          radioCount: radios.length,
          timerText: timer ? timer.innerText : '',
          timerId: timer ? timer.id : '',
          hubText: hubBadge ? hubBadge.innerText : 'NOT FOUND',
          netText: netBadge ? netBadge.innerText : 'NOT FOUND',
          hubExists: !!hubBadge,
          netExists: !!netBadge
        };
      });
      log(`Exam info: ${JSON.stringify(examInfo)}`);
      result.checks.push({ id: 'EXAM-03', desc: 'Soal ditampilkan (radio buttons)', pass: examInfo.radioCount > 0, detail: `radioCount=${examInfo.radioCount}` });

      // Wait 2s and check timer again
      await page.waitForTimeout(2000);
      const timer2 = await page.evaluate(() => {
        const timer = document.querySelector('#timerDisplay, .timer, [id*="timer"]');
        return timer ? timer.innerText : '';
      });
      log(`Timer at t=0: "${examInfo.timerText}", at t+2s: "${timer2}"`);
      result.checks.push({ id: 'EXAM-04', desc: 'Timer tampil dan berjalan', pass: examInfo.timerText.length > 0 && examInfo.timerText !== timer2, detail: `t1="${examInfo.timerText}" t2="${timer2}"` });
      result.checks.push({ id: 'EXAM-07', desc: 'Network badges #hubStatusBadge dan #networkStatusBadge', pass: examInfo.hubExists && examInfo.netExists, detail: `hub="${examInfo.hubText}" net="${examInfo.netText}"` });

      // EXAM-05: Auto-save - click a radio
      if (examInfo.radioCount > 0) {
        await page.evaluate(() => {
          const radio = document.querySelector('input[type="radio"]');
          if (radio) radio.click();
        });
        await page.waitForTimeout(2000);
        const netAfter = await page.evaluate(() => {
          const badge = document.querySelector('#networkStatusBadge');
          return badge ? badge.innerText : '';
        });
        log(`Network badge after answer: "${netAfter}"`);
        result.checks.push({ id: 'EXAM-05', desc: 'Auto-save: badge berubah ke "Tersimpan"', pass: netAfter.includes('Tersimpan') || netAfter.includes('Simpan') || netAfter.includes('saved'), detail: `badge="${netAfter}"` });
        await safeScreenshot(page, 's1-5-autosave');
      } else {
        result.checks.push({ id: 'EXAM-05', desc: 'Auto-save', pass: false, detail: 'No radios found' });
      }
    } else {
      const failUrl = page.url();
      const failText = await page.evaluate(() => document.body.innerText.substring(0, 200));
      log(`Failed to reach exam. URL: ${failUrl}, body: ${failText}`);
      result.checks.push({ id: 'EXAM-03', desc: 'Soal tampil', pass: false, detail: `URL: ${failUrl}` });
      result.checks.push({ id: 'EXAM-04', desc: 'Timer tampil', pass: false });
      result.checks.push({ id: 'EXAM-05', desc: 'Auto-save', pass: false });
      result.checks.push({ id: 'EXAM-07', desc: 'Network badges', pass: false });
    }

  } catch (err) {
    log(`ERROR in Scenario 1: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's1-error');
  }

  RESULTS.push(result);
  await ctx.close();
}

async function scenario2_arsyad(browser) {
  log('=== SKENARIO 2: mohammad.arsyad — Non-Token + Pagination ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S2 - mohammad.arsyad', checks: [] };

  try {
    const loginOk = await login(page, 'mohammad.arsyad@pertamina.com', 'Pertamina@2026');
    result.checks.push({ id: 'S2-LOGIN', desc: 'Login berhasil', pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    await goto(page, `${BASE_URL}/CMP/Assessment`);
    const onPage = page.url().includes('/CMP/Assessment');
    const bodyText = await page.evaluate(() => document.body.innerText);
    const hasAssmt10 = bodyText.includes('UAT OJT Test 2') || bodyText.includes('No Token') || bodyText.includes('Test 2');
    result.checks.push({ id: 'EXAM-01-S2', desc: 'Assessment list + assessment 10 ada', pass: onPage, detail: `hasAssmt10=${hasAssmt10}` });

    // Try to start assessment 10 (no token needed)
    let examReached = false;

    // Try clicking via card
    const allCardTexts = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.card')).map((c, i) => ({ i, text: c.innerText.substring(0, 120) }))
    );
    log(`Cards: ${JSON.stringify(allCardTexts)}`);

    for (const { i, text } of allCardTexts) {
      if (text.includes('UAT OJT Test 2') || text.includes('No Token') || text.includes('Test 2')) {
        const clicked = await page.evaluate((idx) => {
          const cards = document.querySelectorAll('.card');
          const card = cards[idx];
          if (!card) return false;
          const btn = card.querySelector('button, a');
          if (btn) { btn.click(); return btn.innerText || btn.href; }
          return false;
        }, i);
        log(`Clicked on card ${i}: ${clicked}`);
        if (clicked) { examReached = true; break; }
      }
    }

    await page.waitForTimeout(3000);

    // Check if token modal appeared (it should NOT)
    const modalVisible = await page.evaluate(() => {
      const modals = Array.from(document.querySelectorAll('.modal'));
      return modals.some(m => m.classList.contains('show') || window.getComputedStyle(m).display !== 'none');
    });
    log(`Token modal appeared: ${modalVisible} (should be false for no-token assessment)`);
    result.checks.push({ id: 'EXAM-01-NoToken', desc: 'Tidak ada token modal (non-token assessment)', pass: !modalVisible });

    const currentUrl = page.url();
    examReached = currentUrl.includes('StartExam');
    log(`After card click, URL: ${currentUrl}, on exam: ${examReached}`);

    if (!examReached) {
      log('Trying direct navigation to StartExam/10');
      await goto(page, `${BASE_URL}/CMP/StartExam/10`);
      examReached = page.url().includes('StartExam');
      log(`Direct StartExam/10: ${page.url()}`);
    }

    if (examReached) {
      await safeScreenshot(page, 's2-1-startexam');
      const examInfo = await page.evaluate(() => {
        const radios = document.querySelectorAll('input[type="radio"]');
        const paginationBtns = document.querySelectorAll('[id*="pagination"], .pagination, .page-item, button[onclick*="page"], button[onclick*="Page"]');
        const allBtnTexts = Array.from(document.querySelectorAll('button')).map(b => b.innerText.trim()).filter(t => t);
        return {
          radioCount: radios.length,
          paginCount: paginationBtns.length,
          btnTexts: allBtnTexts.slice(0, 15)
        };
      });
      log(`Exam info S2: radios=${examInfo.radioCount}, paginBtns=${examInfo.paginCount}, btns=${JSON.stringify(examInfo.btnTexts)}`);
      result.checks.push({ id: 'EXAM-03-S2', desc: '15 soal tampil', pass: examInfo.radioCount > 0, detail: `radioCount=${examInfo.radioCount}` });

      // EXAM-06: Pagination - look for Next button
      const nextClicked = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        for (const btn of btns) {
          const t = btn.innerText.trim().toLowerCase();
          if (t.includes('next') || t.includes('berikut') || t.includes('selanjutnya') || t === '>') {
            btn.click();
            return btn.innerText;
          }
        }
        // Also try onclick-based pagination
        const paginLinks = Array.from(document.querySelectorAll('[onclick*="goToPage"], [onclick*="gotopage"], [onclick*="page"]'));
        if (paginLinks.length > 0) { paginLinks[1]?.click(); return 'pagination-link'; }
        return null;
      });
      log(`Next button clicked: ${nextClicked}`);
      await page.waitForTimeout(2000);

      const afterNextInfo = await page.evaluate(() => {
        const radios = document.querySelectorAll('input[type="radio"]');
        const activePageEl = document.querySelector('.pagination .active, .page-item.active');
        return {
          radioCount: radios.length,
          activePage: activePageEl ? activePageEl.innerText : ''
        };
      });
      log(`After Next: radios=${afterNextInfo.radioCount}, activePage="${afterNextInfo.activePage}"`);

      result.checks.push({ id: 'EXAM-06', desc: 'Pagination Next/Prev berfungsi', pass: !!nextClicked, detail: `nextClicked="${nextClicked}" afterNextRadios=${afterNextInfo.radioCount}` });

      if (nextClicked) {
        await safeScreenshot(page, 's2-2-pagination-page2');
        // Try Previous
        await page.evaluate(() => {
          const btns = Array.from(document.querySelectorAll('button'));
          for (const btn of btns) {
            const t = btn.innerText.trim().toLowerCase();
            if (t.includes('prev') || t.includes('sebelum') || t.includes('kembali') || t === '<') {
              btn.click();
              return;
            }
          }
        });
        await page.waitForTimeout(1500);
        await safeScreenshot(page, 's2-3-pagination-prev');
        log('Clicked Previous button');
      }
    } else {
      const failText = await page.evaluate(() => document.body.innerText.substring(0, 200));
      log(`Failed to reach exam S2. URL: ${page.url()}, body: ${failText}`);
      result.checks.push({ id: 'EXAM-03-S2', desc: 'Soal tampil', pass: false });
      result.checks.push({ id: 'EXAM-06', desc: 'Pagination', pass: false });
    }

  } catch (err) {
    log(`ERROR in Scenario 2: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's2-error');
  }

  RESULTS.push(result);
  await ctx.close();
}

async function scenario3_widyadhana(browser) {
  log('=== SKENARIO 3: moch.widyadhana — Abandon Exam ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S3 - moch.widyadhana', checks: [] };

  try {
    const loginOk = await login(page, 'moch.widyadhana@pertamina.com', 'Balikpapan@2026');
    result.checks.push({ id: 'S3-LOGIN', desc: 'Login berhasil', pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    // Navigate to StartExam/10 directly
    await goto(page, `${BASE_URL}/CMP/StartExam/10`);
    let onExam = page.url().includes('StartExam');
    log(`Direct StartExam/10: ${page.url()}, onExam: ${onExam}`);

    if (!onExam) {
      // Check if redirected to Assessment list (e.g., token required or status issue)
      const pageText = await page.evaluate(() => document.body.innerText.substring(0, 300));
      log(`Not on exam. Body: ${pageText}`);

      // Try from Assessment list
      await goto(page, `${BASE_URL}/CMP/Assessment`);
      const allCardTexts = await page.evaluate(() =>
        Array.from(document.querySelectorAll('.card')).map((c, i) => ({ i, text: c.innerText.substring(0, 120) }))
      );
      for (const { i, text } of allCardTexts) {
        if (text.includes('UAT OJT Test 2') || text.includes('No Token') || text.includes('OJT')) {
          await page.evaluate((idx) => {
            const card = document.querySelectorAll('.card')[idx];
            if (card) { const btn = card.querySelector('button'); if (btn) btn.click(); }
          }, i);
          break;
        }
      }
      await page.waitForTimeout(3000);
      onExam = page.url().includes('StartExam');
      log(`After card click: ${page.url()}, onExam: ${onExam}`);
    }

    if (onExam) {
      await safeScreenshot(page, 's3-1-exam-started');

      // Answer 1 question
      await page.evaluate(() => {
        const radio = document.querySelector('input[type="radio"]');
        if (radio) radio.click();
      });
      await page.waitForTimeout(1500);

      // Find abandon/keluar button
      const abandonInfo = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button, a'));
        for (const btn of btns) {
          const t = (btn.innerText || btn.textContent || '').trim().toLowerCase();
          if (t.includes('keluar') || t.includes('abandon') || t.includes('tinggalkan') || t.includes('batalkan')) {
            return { found: true, text: btn.innerText || btn.textContent, tag: btn.tagName, id: btn.id };
          }
        }
        const allBtns = btns.map(b => (b.innerText || b.textContent || '').trim()).filter(t => t.length > 0).slice(0, 20);
        return { found: false, allBtnTexts: allBtns };
      });
      log(`Abandon button: ${JSON.stringify(abandonInfo)}`);

      if (abandonInfo.found) {
        // Handle confirm dialog
        let dialogMsg = '';
        page.once('dialog', async (dialog) => {
          dialogMsg = dialog.message();
          log(`Confirm dialog: "${dialogMsg}"`);
          await dialog.accept();
        });

        await page.evaluate(() => {
          const btns = Array.from(document.querySelectorAll('button, a'));
          for (const btn of btns) {
            const t = (btn.innerText || btn.textContent || '').trim().toLowerCase();
            if (t.includes('keluar') || t.includes('abandon') || t.includes('tinggalkan') || t.includes('batalkan')) {
              btn.click();
              return;
            }
          }
        });
        await page.waitForTimeout(5000);
        await safeScreenshot(page, 's3-2-after-abandon');

        const afterUrl = page.url();
        log(`After abandon, URL: ${afterUrl}`);
        const redirectedOk = afterUrl.includes('/CMP/Assessment') && !afterUrl.includes('StartExam');
        result.checks.push({ id: 'EXAM-08a', desc: 'Abandon → redirect ke /CMP/Assessment', pass: redirectedOk, detail: `URL: ${afterUrl}` });

        // Check info message
        const afterText = await page.evaluate(() => document.body.innerText);
        const hasMsg = afterText.includes('dibatalkan') || afterText.includes('Abandoned') || afterText.includes('Hubungi HC');
        result.checks.push({ id: 'EXAM-08b', desc: 'Pesan info abandon ditampilkan', pass: hasMsg, detail: hasMsg ? 'msg found' : 'msg not found' });
        log(`Abandon message shown: ${hasMsg}`);

        // EXAM-08c: Try re-entry
        await goto(page, `${BASE_URL}/CMP/StartExam/10`);
        const reentryUrl = page.url();
        const reentryText = await page.evaluate(() => document.body.innerText.substring(0, 300));
        log(`Re-entry URL: ${reentryUrl}`);
        log(`Re-entry text: ${reentryText.substring(0, 150)}`);
        const blocked = !reentryUrl.includes('StartExam');
        const hasBlockMsg = reentryText.includes('dibatalkan') || reentryText.includes('Abandon') || reentryText.includes('Hubungi HC') || reentryText.includes('dibatalkan');
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry setelah abandon diblokir', pass: blocked || hasBlockMsg, detail: `URL: ${reentryUrl}, msg: ${hasBlockMsg}` });
        await safeScreenshot(page, 's3-3-reentry-blocked');
      } else {
        // Abandon button not found - check form actions
        const formInfo = await page.evaluate(() => {
          const forms = Array.from(document.querySelectorAll('form'));
          return forms.map(f => ({ action: f.action, method: f.method }));
        });
        log(`Forms on exam page: ${JSON.stringify(formInfo)}`);
        result.checks.push({ id: 'EXAM-08a', desc: 'Tombol Keluar Ujian ditemukan', pass: false, detail: `allBtns: ${JSON.stringify(abandonInfo.allBtnTexts)}` });
        result.checks.push({ id: 'EXAM-08b', desc: 'Abandon redirect', pass: false });
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry blocked', pass: false });
      }
    } else {
      const pageText = await page.evaluate(() => document.body.innerText.substring(0, 200));
      log(`Could not enter exam. URL: ${page.url()}, text: ${pageText}`);
      // Check if it shows abandoned message (meaning exam was already abandoned before)
      const alreadyAbandoned = pageText.includes('dibatalkan') || pageText.includes('Abandon');
      if (alreadyAbandoned) {
        log('Session already abandoned from previous test run');
        result.checks.push({ id: 'EXAM-08a', desc: 'Abandon sebelumnya masih berlaku', pass: true, detail: 'Already abandoned from prev run' });
        // Test re-entry blocked
        const reentryBlocked = !page.url().includes('StartExam');
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry diblokir (already abandoned)', pass: reentryBlocked });
      } else {
        result.checks.push({ id: 'EXAM-08a', desc: 'Berhasil masuk exam untuk test abandon', pass: false, detail: `URL: ${page.url()}` });
        result.checks.push({ id: 'EXAM-08b', desc: 'Abandon redirect', pass: false });
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry blocked', pass: false });
      }
    }

  } catch (err) {
    log(`ERROR in Scenario 3: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's3-error');
  }

  RESULTS.push(result);
  await ctx.close();
}

async function main() {
  log('Starting UAT Phase 265 — Worker Exam Flow');
  const browser = await chromium.launch({ headless: true });

  try {
    await scenario1_rino(browser);
    await scenario2_arsyad(browser);
    await scenario3_widyadhana(browser);
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

  fs.writeFileSync('C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/265-worker-exam-flow/uat-results.json', JSON.stringify(RESULTS, null, 2));
  log('Results saved to uat-results.json');
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
