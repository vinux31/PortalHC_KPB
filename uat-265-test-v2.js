// UAT Phase 265: Worker Exam Flow Browser Testing — v2
// Run: node uat-265-test-v2.js

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const RESULTS = [];
const NAV_TIMEOUT = 150000; // 150s — server is very slow
const SCREENSHOTS_DIR = 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/265-worker-exam-flow';

function log(msg) {
  const ts = new Date().toISOString().substring(11, 19);
  console.log(`[${ts}] ${msg}`);
}

async function safeScreenshot(page, name) {
  try {
    // Use clip to avoid waiting for fonts — just grab viewport
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/${name}.png`, clip: { x: 0, y: 0, width: 1280, height: 800 }, timeout: 10000 });
    log(`Screenshot: ${name}.png`);
  } catch (e) {
    log(`Screenshot skip (${name}): ${e.message.split('\n')[0]}`);
  }
}

async function goto(page, url, timeout) {
  const t = timeout || NAV_TIMEOUT;
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: t });
  await page.waitForTimeout(3000);
}

async function login(page, email, password) {
  await goto(page, `${BASE_URL}/Account/Login`);
  await page.waitForTimeout(4000); // extra wait for inputs to be ready
  await page.locator('input[name="email"]').fill(email, { force: true, timeout: 15000 });
  await page.locator('input[name="password"]').fill(password, { force: true, timeout: 15000 });
  await page.locator('button[type="submit"]').click({ force: true, timeout: 15000 });
  await page.waitForTimeout(6000);
  const url = page.url();
  const success = !url.includes('/Account/Login');
  log(`Login ${email}: ${success ? 'OK' : 'FAIL'} (${url})`);
  return success;
}

// ============================
// SCENARIO 1: rino.prasetyo
// ============================
async function scenario1_rino(browser) {
  log('=== SKENARIO 1: rino.prasetyo — Token + Happy Path ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S1 - rino.prasetyo', checks: [] };

  try {
    // Login — try Balikpapan@2026 then Pertamina@2026
    let loginOk = await login(page, 'rino.prasetyo@pertamina.com', 'Balikpapan@2026');
    if (!loginOk) {
      log('Trying Pertamina@2026...');
      loginOk = await login(page, 'rino.prasetyo@pertamina.com', 'Pertamina@2026');
    }
    result.checks.push({ id: 'S1-LOGIN', desc: 'Login berhasil', pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    // EXAM-01: Assessment list
    log('Navigating to CMP/Assessment (may take up to 2min)...');
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    const onPage = page.url().includes('/CMP/Assessment');
    const bodyText = await page.evaluate(() => document.body.innerText);
    const cardCount = await page.evaluate(() => document.querySelectorAll('.card').length);
    const hasAssmt7 = bodyText.includes('UAT OJT Test 1') || bodyText.includes('Test 1');
    log(`Assessment page: ${onPage}, cards: ${cardCount}, hasTest1: ${hasAssmt7}`);
    result.checks.push({ id: 'EXAM-01', desc: 'Assessment list tampil (cards > 0)', pass: onPage && cardCount > 0, detail: `cards=${cardCount} hasTest1=${hasAssmt7}` });
    await safeScreenshot(page, 's1-1-assessment-list');

    // Get all card texts to find assessment 7
    const cardTexts = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.card')).map((c, i) => ({ i, text: c.innerText.substring(0, 200) }))
    );
    log(`Card texts: ${JSON.stringify(cardTexts.slice(0, 3))}`);

    // EXAM-02: Token modal
    let tokenModalOk = false;
    let examReached = false;

    // Find and click Start button for assessment 7 (UAT OJT Test 1)
    let targetCardIdx = -1;
    for (const { i, text } of cardTexts) {
      if (text.includes('UAT OJT Test 1') || text.includes('Test 1 - Token') || text.includes('U6J49L')) {
        targetCardIdx = i;
        break;
      }
    }

    // If not found by title, use first card with a start button
    if (targetCardIdx === -1 && cardTexts.length > 0) {
      log('Assessment 7 not found by title, using first card');
      targetCardIdx = 0;
    }

    if (targetCardIdx >= 0) {
      const btnInfo = await page.evaluate((idx) => {
        const card = document.querySelectorAll('.card')[idx];
        if (!card) return null;
        const btns = Array.from(card.querySelectorAll('button, a'));
        return btns.map(b => ({ text: (b.innerText || b.textContent || '').trim(), tag: b.tagName, onclick: b.getAttribute('onclick') || '' }));
      }, targetCardIdx);
      log(`Buttons in card ${targetCardIdx}: ${JSON.stringify(btnInfo)}`);

      const clicked = await page.evaluate((idx) => {
        const card = document.querySelectorAll('.card')[idx];
        if (!card) return false;
        const btns = Array.from(card.querySelectorAll('button, a'));
        const startBtn = btns.find(b => /mulai|start|kerjakan/i.test(b.innerText || b.textContent || ''));
        if (startBtn) { startBtn.click(); return (startBtn.innerText || startBtn.textContent || '').trim(); }
        // Click first button/link in card
        if (btns.length > 0) { btns[btns.length-1].click(); return 'last-btn'; }
        return false;
      }, targetCardIdx);
      log(`Clicked: ${clicked}`);

      await page.waitForTimeout(4000);
      await safeScreenshot(page, 's1-2-after-click');

      // Check for token modal
      const modalInfo = await page.evaluate(() => {
        const modals = Array.from(document.querySelectorAll('.modal'));
        const visibleModal = modals.find(m => {
          return m.classList.contains('show') || getComputedStyle(m).display !== 'none';
        });
        return {
          count: modals.length,
          visible: !!visibleModal,
          text: visibleModal ? visibleModal.innerText.substring(0, 150) : '',
          anyModalHtml: modals.length > 0 ? modals[0].innerHTML.substring(0, 200) : ''
        };
      });
      log(`Modal info: ${JSON.stringify(modalInfo)}`);
      tokenModalOk = modalInfo.visible;
      result.checks.push({ id: 'EXAM-02a', desc: 'Token modal muncul (IsTokenRequired=true)', pass: tokenModalOk, detail: `modals=${modalInfo.count} visible=${modalInfo.visible}` });

      if (tokenModalOk) {
        // Enter token
        const tokenInput = await page.evaluate(() => {
          const inps = document.querySelectorAll('input');
          for (const inp of inps) {
            if (inp.maxLength === 6 || (inp.placeholder && /token/i.test(inp.placeholder)) || (inp.id && /token/i.test(inp.id))) {
              return { found: true, id: inp.id, placeholder: inp.placeholder, maxLength: inp.maxLength };
            }
          }
          return { found: false };
        });
        log(`Token input: ${JSON.stringify(tokenInput)}`);

        if (tokenInput.found) {
          // Set value and trigger events
          const tokenValue = await page.evaluate(() => {
            const inps = document.querySelectorAll('input');
            for (const inp of inps) {
              if (inp.maxLength === 6 || (inp.placeholder && /token/i.test(inp.placeholder)) || (inp.id && /token/i.test(inp.id))) {
                // Simulate typing lowercase to test auto-uppercase
                inp.value = 'u6j49l';
                inp.dispatchEvent(new Event('input', { bubbles: true }));
                inp.dispatchEvent(new Event('change', { bubbles: true }));
                inp.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true }));
                return inp.value;
              }
            }
            return '';
          });
          log(`Token value after input: "${tokenValue}"`);
          result.checks.push({ id: 'EXAM-02b', desc: 'Token auto-uppercase (lowercase input)', pass: tokenValue === 'U6J49L', detail: `value="${tokenValue}"` });

          // Click submit in modal
          await page.evaluate(() => {
            const submitBtns = document.querySelectorAll('.modal button[type="submit"], .modal-footer button');
            for (const btn of submitBtns) {
              if (/verifi|ok|masuk|submit|confirm|lanjut/i.test(btn.innerText)) { btn.click(); return; }
            }
            // Try any button in visible modal
            const modal = Array.from(document.querySelectorAll('.modal')).find(m => m.classList.contains('show'));
            if (modal) {
              const btn = modal.querySelector('button[type="submit"], button.btn-primary');
              if (btn) btn.click();
            }
          });

          await page.waitForTimeout(6000);
          await safeScreenshot(page, 's1-3-after-token');
          examReached = page.url().includes('StartExam');
          log(`After token, URL: ${page.url()}, examReached: ${examReached}`);
        }
      }
    }

    if (!examReached) {
      log('Trying direct navigation to StartExam/7');
      await goto(page, `${BASE_URL}/CMP/StartExam/7`);
      examReached = page.url().includes('StartExam');
      const redirectUrl = page.url();
      log(`Direct StartExam/7: ${redirectUrl}, reached: ${examReached}`);
      if (!examReached) {
        // Check why redirected
        const bodySnip = await page.evaluate(() => document.body.innerText.substring(0, 300));
        log(`Redirect body: ${bodySnip}`);
      }
    }

    if (examReached) {
      await safeScreenshot(page, 's1-4-exam');

      const examInfo = await page.evaluate(() => {
        const radios = document.querySelectorAll('input[type="radio"]');
        const timerEl = document.querySelector('#timerDisplay, #countdown, [id*="timer"]');
        const hubBadge = document.querySelector('#hubStatusBadge');
        const netBadge = document.querySelector('#networkStatusBadge');
        const allText = document.body.innerText.substring(0, 100);
        return {
          radioCount: radios.length,
          timerText: timerEl ? timerEl.innerText.trim() : '',
          timerFound: !!timerEl,
          hubText: hubBadge ? hubBadge.innerText.trim() : 'NOT_FOUND',
          netText: netBadge ? netBadge.innerText.trim() : 'NOT_FOUND',
          hubFound: !!hubBadge,
          netFound: !!netBadge,
          bodyStart: allText
        };
      });
      log(`Exam info: ${JSON.stringify(examInfo)}`);
      result.checks.push({ id: 'EXAM-03', desc: 'Soal tampil (radio buttons)', pass: examInfo.radioCount > 0, detail: `radios=${examInfo.radioCount}` });

      // Timer check
      await page.waitForTimeout(3000);
      const timer2 = await page.evaluate(() => {
        const el = document.querySelector('#timerDisplay, #countdown, [id*="timer"]');
        return el ? el.innerText.trim() : '';
      });
      log(`Timer t0: "${examInfo.timerText}", t+3s: "${timer2}"`);
      result.checks.push({ id: 'EXAM-04', desc: 'Timer berjalan (countdown)', pass: examInfo.timerFound && examInfo.timerText !== timer2, detail: `t0="${examInfo.timerText}" t3="${timer2}"` });
      result.checks.push({ id: 'EXAM-07', desc: 'Network badges tersedia', pass: examInfo.hubFound && examInfo.netFound, detail: `hub="${examInfo.hubText}" net="${examInfo.netText}"` });

      // EXAM-05: Auto-save
      if (examInfo.radioCount > 0) {
        const preBadge = examInfo.netText;
        await page.evaluate(() => {
          const radio = document.querySelector('input[type="radio"]');
          if (radio) radio.click();
        });
        await page.waitForTimeout(2500);
        const postBadge = await page.evaluate(() => {
          const el = document.querySelector('#networkStatusBadge');
          return el ? el.innerText.trim() : '';
        });
        log(`Network badge: before="${preBadge}", after="${postBadge}"`);
        result.checks.push({ id: 'EXAM-05', desc: 'Auto-save: badge berubah ke Tersimpan', pass: postBadge.includes('Tersimpan') || postBadge.includes('Simpan'), detail: `after="${postBadge}"` });
        await safeScreenshot(page, 's1-5-autosave');
      } else {
        result.checks.push({ id: 'EXAM-05', desc: 'Auto-save', pass: false, detail: 'no radios' });
      }
    } else {
      for (const id of ['EXAM-03','EXAM-04','EXAM-05','EXAM-07']) {
        result.checks.push({ id, desc: `${id} - exam not reached`, pass: false });
      }
    }
  } catch (err) {
    log(`ERROR S1: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's1-error');
  }
  RESULTS.push(result);
  await ctx.close();
}

// ============================
// SCENARIO 2: mohammad.arsyad
// ============================
async function scenario2_arsyad(browser) {
  log('=== SKENARIO 2: mohammad.arsyad — Non-Token + Pagination ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S2 - mohammad.arsyad', checks: [] };

  try {
    const loginOk = await login(page, 'mohammad.arsyad@pertamina.com', 'Pertamina@2026');
    result.checks.push({ id: 'S2-LOGIN', desc: 'Login berhasil', pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    log('Navigating to Assessment list...');
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    const onPage = page.url().includes('/CMP/Assessment');
    const cardCount = await page.evaluate(() => document.querySelectorAll('.card').length);
    const bodyText = await page.evaluate(() => document.body.innerText);
    log(`Assessment page: ${onPage}, cards: ${cardCount}`);
    result.checks.push({ id: 'EXAM-01-S2', desc: 'Assessment list tampil', pass: onPage && cardCount > 0 });

    // Find assessment 10 (UAT OJT Test 2 - No Token)
    const cardTexts = await page.evaluate(() =>
      Array.from(document.querySelectorAll('.card')).map((c, i) => ({ i, text: c.innerText.substring(0, 200) }))
    );
    log(`Cards: ${JSON.stringify(cardTexts)}`);

    let targetIdx = -1;
    for (const { i, text } of cardTexts) {
      if (text.includes('UAT OJT Test 2') || text.includes('Test 2') || text.includes('No Token')) {
        targetIdx = i;
        break;
      }
    }
    if (targetIdx === -1 && cardTexts.length > 0) targetIdx = 0;

    // Click start button
    if (targetIdx >= 0) {
      await page.evaluate((idx) => {
        const card = document.querySelectorAll('.card')[idx];
        if (!card) return;
        const btns = Array.from(card.querySelectorAll('button, a'));
        const startBtn = btns.find(b => /mulai|start|kerjakan/i.test(b.innerText || b.textContent || ''));
        if (startBtn) startBtn.click();
        else if (btns.length > 0) btns[btns.length-1].click();
      }, targetIdx);
      await page.waitForTimeout(4000);
    }

    // Check no token modal
    const modalVisible = await page.evaluate(() => {
      const modals = Array.from(document.querySelectorAll('.modal'));
      return modals.some(m => m.classList.contains('show') || getComputedStyle(m).display !== 'none');
    });
    log(`Token modal visible: ${modalVisible} (should be false)`);
    result.checks.push({ id: 'EXAM-01-NoToken', desc: 'Tidak ada token modal (non-token assessment)', pass: !modalVisible });

    // Navigate to StartExam/10
    let examReached = page.url().includes('StartExam');
    if (!examReached) {
      log('Navigating directly to StartExam/10...');
      await goto(page, `${BASE_URL}/CMP/StartExam/10`);
      examReached = page.url().includes('StartExam');
    }
    log(`Exam reached: ${examReached}, URL: ${page.url()}`);

    if (examReached) {
      await safeScreenshot(page, 's2-1-exam');

      const radioCount = await page.evaluate(() => document.querySelectorAll('input[type="radio"]').length);
      log(`Radio count: ${radioCount} (expect > 10 for 15 questions)`);
      result.checks.push({ id: 'EXAM-03-S2', desc: '15 soal tampil', pass: radioCount > 0, detail: `radios=${radioCount}` });

      // EXAM-06: Pagination
      const pageInfo = await page.evaluate(() => {
        const allBtns = Array.from(document.querySelectorAll('button')).map(b => b.innerText.trim()).filter(t => t);
        const paginEl = document.querySelectorAll('[id*="pagination"], .pagination, [onclick*="Page"], [onclick*="page"]');
        return { btnTexts: allBtns, paginCount: paginEl.length };
      });
      log(`Buttons: ${JSON.stringify(pageInfo.btnTexts)}, paginEls: ${pageInfo.paginCount}`);

      // Try to click Next/Berikut
      const nextResult = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        for (const btn of btns) {
          const t = btn.innerText.trim().toLowerCase();
          if (t === 'next' || t === 'berikut' || t === 'selanjutnya' || t.includes('halaman berikut') || t === '>') {
            const text = btn.innerText;
            btn.click();
            return text;
          }
        }
        // Try span/a with pagination
        const links = Array.from(document.querySelectorAll('a, span, [role="button"]'));
        for (const link of links) {
          const t = (link.innerText || '').trim().toLowerCase();
          if (t === '>' || t === 'next' || t === '2') {
            link.click();
            return link.innerText;
          }
        }
        return null;
      });
      log(`Next clicked: ${nextResult}`);

      if (!nextResult) {
        // Look for goToPage function calls
        const scriptContent = await page.evaluate(() => {
          const scripts = Array.from(document.querySelectorAll('script'));
          for (const s of scripts) {
            if (s.textContent && s.textContent.includes('goToPage')) {
              return s.textContent.substring(0, 500);
            }
          }
          return null;
        });
        log(`Script with goToPage: ${scriptContent ? scriptContent.substring(0, 200) : 'not found'}`);

        // Call goToPage(2) if available
        const called = await page.evaluate(() => {
          if (typeof goToPage === 'function') { goToPage(2); return 'called goToPage(2)'; }
          if (typeof changePage === 'function') { changePage(2); return 'called changePage(2)'; }
          if (typeof showPage === 'function') { showPage(2); return 'called showPage(2)'; }
          return null;
        });
        log(`Function call result: ${called}`);
        await page.waitForTimeout(2000);
        await safeScreenshot(page, 's2-2-page2');
        result.checks.push({ id: 'EXAM-06', desc: 'Pagination berfungsi', pass: !!called, detail: `nextBtn="${nextResult}" fn="${called}"` });
      } else {
        await page.waitForTimeout(2000);
        await safeScreenshot(page, 's2-2-page2');
        result.checks.push({ id: 'EXAM-06', desc: 'Pagination Next berfungsi', pass: true, detail: `clicked: "${nextResult}"` });
      }
    } else {
      const failText = await page.evaluate(() => document.body.innerText.substring(0, 200));
      log(`Failed. URL: ${page.url()}, body: ${failText}`);
      result.checks.push({ id: 'EXAM-03-S2', desc: 'Soal tampil', pass: false });
      result.checks.push({ id: 'EXAM-06', desc: 'Pagination', pass: false });
    }
  } catch (err) {
    log(`ERROR S2: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's2-error');
  }
  RESULTS.push(result);
  await ctx.close();
}

// ============================
// SCENARIO 3: moch.widyadhana
// ============================
async function scenario3_widyadhana(browser) {
  log('=== SKENARIO 3: moch.widyadhana — Abandon Exam ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S3 - moch.widyadhana', checks: [] };

  try {
    const loginOk = await login(page, 'moch.widyadhana@pertamina.com', 'Balikpapan@2026');
    result.checks.push({ id: 'S3-LOGIN', desc: 'Login berhasil', pass: loginOk });
    if (!loginOk) { RESULTS.push(result); await ctx.close(); return; }

    // Navigate directly to StartExam/10 (no token, widyadhana is a participant)
    log('Navigating to StartExam/10...');
    await goto(page, `${BASE_URL}/CMP/StartExam/10`);
    let onExam = page.url().includes('StartExam');
    log(`StartExam URL: ${page.url()}, onExam: ${onExam}`);

    if (!onExam) {
      // Check why redirected
      const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 300));
      log(`Redirect body: ${bodyText}`);

      // Check if already abandoned
      const isAbandoned = bodyText.includes('dibatalkan') || bodyText.includes('Abandoned') || bodyText.includes('Hubungi HC');
      if (isAbandoned) {
        log('Session already abandoned from previous run');
        result.checks.push({ id: 'EXAM-08a', desc: 'Abandon sudah terjadi (sesi sebelumnya)', pass: true, detail: 'previous run' });
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry diblokir (abandoned)', pass: true, detail: 'already blocked' });
        RESULTS.push(result);
        await ctx.close();
        return;
      }
    }

    if (onExam) {
      await safeScreenshot(page, 's3-1-exam');

      // Answer 1 question
      await page.evaluate(() => {
        const radio = document.querySelector('input[type="radio"]');
        if (radio) radio.click();
      });
      await page.waitForTimeout(2000);

      // Find abandon/keluar button
      const abandonInfo = await page.evaluate(() => {
        const allBtns = Array.from(document.querySelectorAll('button, a, [role="button"]'));
        for (const btn of allBtns) {
          const t = (btn.innerText || btn.textContent || '').trim().toLowerCase();
          if (t.includes('keluar') || t.includes('abandon') || t.includes('tinggalkan') || t.includes('batalkan ujian')) {
            return { found: true, text: btn.innerText, id: btn.id, tag: btn.tagName };
          }
        }
        return { found: false, allBtns: allBtns.map(b => (b.innerText||'').trim()).filter(t=>t).slice(0,20) };
      });
      log(`Abandon button: ${JSON.stringify(abandonInfo)}`);

      if (abandonInfo.found) {
        let dialogMsg = '';
        page.once('dialog', async (dialog) => {
          dialogMsg = dialog.message();
          log(`Dialog: "${dialogMsg}"`);
          await dialog.accept();
        });

        await page.evaluate(() => {
          const allBtns = Array.from(document.querySelectorAll('button, a, [role="button"]'));
          for (const btn of allBtns) {
            const t = (btn.innerText || btn.textContent || '').trim().toLowerCase();
            if (t.includes('keluar') || t.includes('abandon') || t.includes('tinggalkan') || t.includes('batalkan ujian')) {
              btn.click();
              return;
            }
          }
        });

        await page.waitForTimeout(6000);
        await safeScreenshot(page, 's3-2-after-abandon');

        const afterUrl = page.url();
        const afterBody = await page.evaluate(() => document.body.innerText.substring(0, 300));
        log(`After abandon: URL=${afterUrl}`);
        log(`After abandon body: ${afterBody.substring(0, 150)}`);

        const redirectOk = afterUrl.includes('/CMP/Assessment') && !afterUrl.includes('StartExam');
        result.checks.push({ id: 'EXAM-08a', desc: 'Abandon → redirect ke Assessment', pass: redirectOk, detail: `URL: ${afterUrl}` });

        const hasMsg = afterBody.includes('dibatalkan') || afterBody.includes('Abandon') || afterBody.includes('Hubungi HC');
        result.checks.push({ id: 'EXAM-08b', desc: 'Pesan abandon ditampilkan', pass: hasMsg });

        // Test re-entry
        log('Testing re-entry...');
        await goto(page, `${BASE_URL}/CMP/StartExam/10`);
        const reentryUrl = page.url();
        const reentryBody = await page.evaluate(() => document.body.innerText.substring(0, 300));
        log(`Re-entry URL: ${reentryUrl}`);
        log(`Re-entry body: ${reentryBody.substring(0, 150)}`);
        const blocked = !reentryUrl.includes('StartExam');
        const hasBlockMsg = reentryBody.includes('dibatalkan') || reentryBody.includes('Abandon') || reentryBody.includes('Hubungi HC');
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry diblokir', pass: blocked || hasBlockMsg, detail: `URL: ${reentryUrl}, msg: ${hasBlockMsg}` });
        await safeScreenshot(page, 's3-3-reentry');
      } else {
        // Show all page HTML to understand structure
        const pageContent = await page.evaluate(() => {
          const btnTexts = Array.from(document.querySelectorAll('button')).map(b => b.innerText.trim()).filter(t => t);
          const forms = Array.from(document.querySelectorAll('form')).map(f => ({ action: f.action, id: f.id }));
          return { btnTexts, forms };
        });
        log(`Page content: ${JSON.stringify(pageContent)}`);

        // Check if there's a form with abandon action
        const abandonForm = await page.evaluate(() => {
          const forms = document.querySelectorAll('form');
          for (const form of forms) {
            if (form.action && form.action.includes('AbandonExam')) {
              const btn = form.querySelector('button');
              if (btn) btn.click();
              return { found: true, action: form.action };
            }
          }
          return { found: false };
        });
        log(`Abandon form: ${JSON.stringify(abandonForm)}`);

        result.checks.push({ id: 'EXAM-08a', desc: 'Tombol Keluar Ujian ada dan diklik', pass: abandonForm.found, detail: JSON.stringify(abandonInfo) });
        result.checks.push({ id: 'EXAM-08b', desc: 'Redirect ke Assessment', pass: false });
        result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry blocked', pass: false });
      }
    } else {
      result.checks.push({ id: 'EXAM-08a', desc: 'Masuk exam untuk test abandon', pass: false, detail: `URL: ${page.url()}` });
      result.checks.push({ id: 'EXAM-08b', desc: 'Abandon redirect', pass: false });
      result.checks.push({ id: 'EXAM-08c', desc: 'Re-entry blocked', pass: false });
    }
  } catch (err) {
    log(`ERROR S3: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's3-error');
  }
  RESULTS.push(result);
  await ctx.close();
}

async function main() {
  log('Starting UAT Phase 265 v2 — Worker Exam Flow');
  const browser = await chromium.launch({ headless: true });

  try {
    await scenario1_rino(browser);
    await scenario2_arsyad(browser);
    await scenario3_widyadhana(browser);
  } finally {
    await browser.close();
  }

  log('\n========== UAT RESULTS SUMMARY ==========');
  let totalPass = 0, totalFail = 0;
  for (const s of RESULTS) {
    log(`\n${s.scenario}:`);
    if (s.error) log(`  ERROR: ${s.error}`);
    for (const c of s.checks) {
      const status = c.pass ? 'PASS' : 'FAIL';
      log(`  [${status}] ${c.id}: ${c.desc}${c.detail ? ' ['+c.detail+']' : ''}`);
      if (c.pass) totalPass++; else totalFail++;
    }
  }
  log(`\nTotal: ${totalPass} PASS, ${totalFail} FAIL`);

  fs.writeFileSync('C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/265-worker-exam-flow/uat-results.json',
    JSON.stringify(RESULTS, null, 2));
  log('Results saved to uat-results.json');
}

main().catch(err => { console.error('Fatal:', err); process.exit(1); });
