// UAT Phase 267: EDGE-07 — Timer Habis (Natural Expiry + Auto-Submit)
// Run: node uat-267-timer-test.js
//
// Skenario: Worker Arsyad mengerjakan assessment durasi 2 menit.
// Biarkan timer habis secara natural, verifikasi:
//   - EDGE-07a: timeUpWarningModal muncul
//   - EDGE-07b: auto-submit terjadi (klik OK atau 10s timeout)
//   - EDGE-07c: redirect ke ExamSummary/Results dengan skor
//
// PENTING: Tidak ada manipulasi waktu client-side (per D-09).
// Script ini harus dijalankan SETELAH admin membuat assessment 2 menit
// dan assign ke worker Arsyad.

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const RESULTS = [];
const NAV_TIMEOUT = 60000;
const SCREENSHOTS_DIR = 'C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/267-resilience-edge-cases';

// Assessment setup constants — perlu diisi setelah admin membuat assessment baru
// Script akan mencari assessment dengan nama ini di daftar assessment Arsyad
const TIMER_ASSESSMENT_NAME = 'UAT Timer Test Arsyad';

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

// STEP 1: Setup via Admin — cari assessment baru atau buat satu
// Mengembalikan assessmentId jika ditemukan, null jika tidak
async function findOrLogTimerAssessment(browser) {
  log('=== SETUP: Cek assessment timer untuk Arsyad ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  let assessmentId = null;

  try {
    const loginOk = await login(page, 'admin@pertamina.com', '123456');
    if (!loginOk) {
      log('ERROR: Admin login gagal');
      await ctx.close();
      return null;
    }

    // Navigasi ke Assessment list (admin view)
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    log(`Admin assessment page: ${page.url()}`);

    // Cari assessment dengan nama timer test
    const assessmentInfo = await page.evaluate((targetName) => {
      const allText = document.body.innerText;
      const rows = Array.from(document.querySelectorAll('tr, .card'));
      for (const row of rows) {
        const text = row.innerText || '';
        if (text.includes(targetName)) {
          // Cari link atau ID
          const link = row.querySelector('a[href*="StartExam"], a[href*="Assessment"]');
          const idMatch = (link?.href || '').match(/\/(\d+)/);
          return {
            found: true,
            text: text.substring(0, 150),
            href: link?.href || '',
            id: idMatch ? idMatch[1] : null
          };
        }
      }
      // Cari di semua text yang ada
      return {
        found: allText.includes(targetName),
        allText: allText.substring(0, 500)
      };
    }, TIMER_ASSESSMENT_NAME);

    log(`Assessment "${TIMER_ASSESSMENT_NAME}": ${JSON.stringify(assessmentInfo)}`);

    if (assessmentInfo.found && assessmentInfo.id) {
      assessmentId = parseInt(assessmentInfo.id);
      log(`Assessment timer ditemukan: ID ${assessmentId}`);
    } else {
      log(`Assessment timer TIDAK ditemukan. Perlu admin buat manual:`);
      log(`  - Judul: "${TIMER_ASSESSMENT_NAME}"`);
      log(`  - Kategori: OJT`);
      log(`  - Durasi: 2 menit`);
      log(`  - Import minimal 10 soal`);
      log(`  - Assign worker: mohammad.arsyad@pertamina.com`);
      log(`  - Status: Open`);
    }

    await safeScreenshot(page, 'setup-admin-assessment-list');
  } catch (e) {
    log(`Setup error: ${e.message.split('\n')[0]}`);
  }

  await ctx.close();
  return assessmentId;
}

// SKENARIO UTAMA: Arsyad — EDGE-07 Timer Habis
async function scenarioArsyadTimerExpiry(browser, manualAssessmentId) {
  log('=== SKENARIO ARSYAD: EDGE-07 Timer Habis Natural ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await ctx.newPage();
  const result = { scenario: 'EDGE-07 - Arsyad Timer Habis', checks: [] };

  try {
    // Login sebagai Arsyad
    const loginOk = await login(page, 'mohammad.arsyad@pertamina.com', 'Pertamina@2026');
    result.checks.push({ id: 'E7-LOGIN', desc: 'Login Arsyad berhasil', pass: loginOk });
    if (!loginOk) {
      RESULTS.push(result);
      await ctx.close();
      return;
    }

    // Cari assessment timer di daftar assessment Arsyad
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    await safeScreenshot(page, 'e7-01-assessment-list');

    const assessmentInfo = await page.evaluate((targetName) => {
      const cards = Array.from(document.querySelectorAll('.card, tr'));
      const allText = document.body.innerText;
      log_info = [];
      for (const card of cards) {
        const text = card.innerText || '';
        if (text.toLowerCase().includes('timer') || text.includes(targetName) || text.includes('2 menit') || text.includes('UAT Timer')) {
          const btn = card.querySelector('button, a');
          log_info.push({ text: text.substring(0, 100), btnText: btn?.innerText?.trim() || '', btnHref: btn?.href || '' });
        }
      }
      return {
        totalCards: cards.length,
        timerCards: log_info,
        pageText: allText.substring(0, 600)
      };
    }, TIMER_ASSESSMENT_NAME);

    log(`Assessment cards: ${assessmentInfo.totalCards}, timer-related: ${JSON.stringify(assessmentInfo.timerCards)}`);
    log(`Page text excerpt: ${assessmentInfo.pageText}`);

    // Coba navigasi langsung ke assessment jika ID diketahui
    let examUrl = null;
    let examReached = false;

    // Coba klik tombol Start Assessment di halaman assessment list
    // (lebih reliable daripada navigasi langsung yang bisa AccessDenied)
    const clickResult = await page.evaluate(function(args) {
      const targetId = args.targetId, targetName = args.targetName;
      const btns = Array.from(document.querySelectorAll('button, a'));
      // Cari assessment dengan ID tertentu atau nama tertentu
      for (const btn of btns) {
        const parentCard = btn.closest('.card, tr, li, .col');
        const parentText = parentCard ? (parentCard.innerText || '') : '';
        const btnText = (btn.innerText || btn.textContent || '').toLowerCase().trim();
        const isStartBtn = btnText.includes('start') || btnText.includes('mulai') || btnText.includes('kerjakan');

        if (isStartBtn) {
          // Check if this is the right assessment (2 minutes or UAT Timer)
          if (parentText.includes('2 minutes') || parentText.includes('2 menit') ||
              parentText.includes(targetName) || parentText.includes('UAT OJT Test 2') ||
              parentText.includes('No Token')) {
            btn.click();
            return { clicked: true, text: btn.innerText, parentText: parentText.substring(0, 80) };
          }
        }
      }
      // Fallback: click any Start Assessment button
      for (const btn of btns) {
        const btnText = (btn.innerText || btn.textContent || '').toLowerCase().trim();
        if (btnText.includes('start assessment') || btnText.includes('mulai assessment')) {
          btn.click();
          return { clicked: true, text: btn.innerText, note: 'first-start-btn-fallback' };
        }
      }
      return { clicked: false };
    }, { targetId: manualAssessmentId, targetName: TIMER_ASSESSMENT_NAME });
    log(`Click result: ${JSON.stringify(clickResult)}`);

    await page.waitForTimeout(4000);
    examUrl = page.url();
    examReached = examUrl.includes('StartExam');
    log(`After click, URL: ${examUrl}, examReached: ${examReached}`);

    if (!examReached && manualAssessmentId) {
      log(`Mencoba navigasi langsung ke StartExam/${manualAssessmentId}...`);
      await goto(page, `${BASE_URL}/CMP/StartExam/${manualAssessmentId}`);
      examReached = page.url().includes('StartExam');
      examUrl = page.url();
      log(`Direct navigation result: ${examUrl}`);
    }

    result.checks.push({
      id: 'E7-REACH-EXAM',
      desc: 'Berhasil masuk halaman ujian',
      pass: examReached,
      detail: `URL: ${examUrl}`
    });

    if (!examReached) {
      log('GAGAL masuk ujian. Perlu admin setup assessment baru terlebih dahulu.');
      log('Instruksi setup:');
      log('  1. Login sebagai admin@pertamina.com');
      log('  2. Buat assessment baru: judul "UAT Timer Test Arsyad", durasi 2 menit, kategori OJT');
      log('  3. Import minimal 10 soal');
      log('  4. Assign worker: mohammad.arsyad@pertamina.com');
      log('  5. Set status Open');
      log('  6. Jalankan script ini lagi dengan ID assessment baru');
      RESULTS.push(result);
      await ctx.close();
      return;
    }

    await safeScreenshot(page, 'e7-02-exam-started');

    // Verifikasi timer tampil
    const timerInfo = await page.evaluate(() => {
      const timer = document.getElementById('examTimer');
      const timerText = timer ? timer.innerText : 'NOT FOUND';
      const radios = document.querySelectorAll('input[type="radio"]');
      return {
        timerText,
        timerExists: !!timer,
        radioCount: radios.length
      };
    });
    log(`Timer info: ${JSON.stringify(timerInfo)}`);
    result.checks.push({
      id: 'E7-TIMER-DISPLAY',
      desc: 'Timer #examTimer tampil',
      pass: timerInfo.timerExists && timerInfo.timerText !== 'NOT FOUND',
      detail: `timer="${timerInfo.timerText}" radios=${timerInfo.radioCount}`
    });

    // Jawab 2-3 soal agar ada skor
    if (timerInfo.radioCount > 0) {
      log('Menjawab 2-3 soal...');
      const answeredCount = await page.evaluate(() => {
        const radios = Array.from(document.querySelectorAll('input[type="radio"]'));
        // Pilih radio pertama dari beberapa soal berbeda
        const answered = new Set();
        let count = 0;
        for (const radio of radios) {
          const name = radio.name;
          if (!answered.has(name) && count < 3) {
            radio.click();
            answered.add(name);
            count++;
          }
        }
        return count;
      });
      log(`Dijawab ${answeredCount} soal`);
      await page.waitForTimeout(3000); // tunggu auto-save
      await safeScreenshot(page, 'e7-03-answered-questions');
    }

    // Baca sisa waktu dari timer untuk estimasi berapa lama menunggu
    const timerValue = await page.evaluate(() => {
      const timer = document.getElementById('examTimer');
      return timer ? timer.innerText : '';
    });
    log(`Sisa waktu sebelum menunggu: "${timerValue}"`);

    // Parse sisa waktu (format: MM:SS)
    let remainingSecs = 120; // default 2 menit
    const timerMatch = timerValue.match(/(\d+):(\d+)/);
    if (timerMatch) {
      remainingSecs = parseInt(timerMatch[1]) * 60 + parseInt(timerMatch[2]);
    }
    log(`Estimasi waktu menunggu: ${remainingSecs} detik (~${Math.ceil(remainingSecs/60)} menit)`);

    if (remainingSecs > 180) {
      log('PERINGATAN: Sisa waktu > 3 menit. Assessment mungkin bukan durasi 2 menit.');
      log('Script akan tetap menunggu tetapi harap pastikan assessment yang benar digunakan.');
    }

    // Tunggu timer habis — EDGE-07a: timeUpWarningModal harus muncul
    // Timeout: remainingSecs + 30 detik buffer
    const waitMs = (remainingSecs + 30) * 1000;
    log(`Menunggu timer habis... (timeout: ${Math.ceil(waitMs/1000)}s)`);
    log('Ini akan menunggu hingga ~' + new Date(Date.now() + waitMs).toLocaleTimeString());

    let modalAppeared = false;
    let modalTimedOut = false;

    try {
      await page.waitForSelector('#timeUpWarningModal.show', { timeout: waitMs });
      modalAppeared = true;
      log('timeUpWarningModal MUNCUL!');
    } catch (e) {
      // Coba cek apakah modal visible tapi tanpa class .show
      const modalVisible = await page.evaluate(() => {
        const modal = document.getElementById('timeUpWarningModal');
        if (!modal) return false;
        const style = window.getComputedStyle(modal);
        return style.display !== 'none' || modal.classList.contains('show');
      });
      if (modalVisible) {
        modalAppeared = true;
        log('timeUpWarningModal visible (tanpa class .show)');
      } else {
        log(`Timeout menunggu modal: ${e.message.split('\n')[0]}`);
        modalTimedOut = true;
      }
    }

    await safeScreenshot(page, 'e7-04-timer-expired-modal');

    result.checks.push({
      id: 'E7-07a-MODAL',
      desc: 'timeUpWarningModal muncul saat timer habis',
      pass: modalAppeared,
      detail: modalTimedOut ? 'TIMEOUT — modal tidak muncul dalam waktu yang diharapkan' : 'Modal muncul'
    });

    if (!modalAppeared) {
      // Cek apakah sudah auto-submit tanpa modal (edge case: timer sudah habis sebelumnya)
      const currentUrl = page.url();
      if (currentUrl.includes('ExamSummary') || currentUrl.includes('Results')) {
        log('Halaman sudah di ExamSummary/Results — mungkin auto-submit terjadi tanpa menampilkan modal');
        result.checks.push({
          id: 'E7-07b-AUTOSUBMIT',
          desc: 'Auto-submit berhasil (langsung ke results)',
          pass: true,
          detail: `URL: ${currentUrl}`
        });
        RESULTS.push(result);
        await safeScreenshot(page, 'e7-05-results');
        await ctx.close();
        return;
      }

      log('GAGAL: Modal tidak muncul dan tidak ada redirect ke results');
      result.checks.push({
        id: 'E7-07b-AUTOSUBMIT',
        desc: 'Auto-submit setelah timer habis',
        pass: false,
        detail: 'Modal tidak muncul'
      });
      RESULTS.push(result);
      await ctx.close();
      return;
    }

    // Modal muncul — EDGE-07b: klik OK atau tunggu auto-submit 10 detik
    log('Modal muncul. Klik tombol OK...');
    const okClicked = await page.evaluate(() => {
      const btn = document.getElementById('timeUpOkBtn');
      if (btn) {
        btn.click();
        return true;
      }
      // Fallback: cari tombol di modal
      const modalBtn = document.querySelector('#timeUpWarningModal button.btn-danger, #timeUpWarningModal button');
      if (modalBtn) {
        modalBtn.click();
        return true;
      }
      return false;
    });
    log(`OK button clicked: ${okClicked}`);

    if (!okClicked) {
      log('Tombol OK tidak ditemukan — menunggu auto-submit 10 detik...');
    }

    // Tunggu redirect ke ExamSummary atau Results
    log('Menunggu redirect setelah submit...');
    try {
      await page.waitForURL(/ExamSummary|Results|CMP\/Assessment/, { timeout: 30000 });
      log(`Redirect berhasil ke: ${page.url()}`);
    } catch (e) {
      log(`Timeout menunggu redirect: ${e.message.split('\n')[0]}`);
      // Check current URL
      log(`Current URL: ${page.url()}`);
    }

    await page.waitForTimeout(3000);
    await safeScreenshot(page, 'e7-05-after-submit');

    const afterSubmitUrl = page.url();
    log(`URL setelah submit: ${afterSubmitUrl}`);

    const submitSucceeded = afterSubmitUrl.includes('ExamSummary') ||
      afterSubmitUrl.includes('Results') ||
      !afterSubmitUrl.includes('StartExam');

    result.checks.push({
      id: 'E7-07b-AUTOSUBMIT',
      desc: 'Redirect ke ExamSummary/Results setelah auto-submit',
      pass: submitSucceeded,
      detail: `URL: ${afterSubmitUrl}`
    });

    // EDGE-07c: Verifikasi skor/hasil ditampilkan
    const resultsInfo = await page.evaluate(() => {
      const text = document.body.innerText;
      // Cari skor atau pass/fail indicator
      const hasScore = /\d+\s*(\/|dari|of)\s*\d+/.test(text) ||
        text.includes('Nilai') ||
        text.includes('Skor') ||
        text.includes('Score') ||
        text.includes('PASS') ||
        text.includes('FAIL') ||
        text.includes('Lulus') ||
        text.includes('Tidak Lulus');
      return {
        hasScore,
        textExcerpt: text.substring(0, 400)
      };
    });
    log(`Results info: hasScore=${resultsInfo.hasScore}`);
    log(`Results text: ${resultsInfo.textExcerpt}`);

    result.checks.push({
      id: 'E7-07c-SCORE',
      desc: 'Skor/hasil ujian ditampilkan',
      pass: resultsInfo.hasScore,
      detail: resultsInfo.textExcerpt.substring(0, 100)
    });

    await safeScreenshot(page, 'e7-06-results-score');

    // EDGE-07d: Verifikasi status assessment berubah ke Completed (via Assessment list)
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    const statusInfo = await page.evaluate(() => {
      const text = document.body.innerText;
      return {
        hasCompleted: text.includes('Completed') || text.includes('Selesai'),
        textExcerpt: text.substring(0, 400)
      };
    });
    log(`Status setelah submit: hasCompleted=${statusInfo.hasCompleted}`);

    result.checks.push({
      id: 'E7-07d-COMPLETED',
      desc: 'Status assessment berubah ke Completed',
      pass: statusInfo.hasCompleted,
      detail: statusInfo.textExcerpt.substring(0, 100)
    });

    await safeScreenshot(page, 'e7-07-assessment-list-completed');

  } catch (err) {
    log(`ERROR in Scenario EDGE-07: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    result.stack = err.stack;
    await safeScreenshot(page, 'e7-error');
  }

  RESULTS.push(result);
  await ctx.close();
}

async function main() {
  log('Starting UAT Phase 267 — EDGE-07 Timer Habis');
  log(`BASE_URL: ${BASE_URL}`);
  log(`Screenshots dir: ${SCREENSHOTS_DIR}`);

  // Parse command line args untuk manual assessment ID
  const args = process.argv.slice(2);
  let manualAssessmentId = null;
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--assessment-id' && args[i+1]) {
      manualAssessmentId = parseInt(args[i+1]);
      log(`Manual assessment ID dari args: ${manualAssessmentId}`);
    }
  }

  const browser = await chromium.launch({ headless: true });

  try {
    // Step 1: Cek setup (temukan assessment timer)
    if (!manualAssessmentId) {
      const foundId = await findOrLogTimerAssessment(browser);
      if (foundId) {
        manualAssessmentId = foundId;
      }
    }

    // Step 2: Jalankan skenario EDGE-07
    await scenarioArsyadTimerExpiry(browser, manualAssessmentId);

  } finally {
    await browser.close();
  }

  // Print summary
  log('\n========== UAT RESULTS SUMMARY EDGE-07 ==========');
  let totalPass = 0, totalFail = 0;
  for (const scenario of RESULTS) {
    log(`\n${scenario.scenario}:`);
    if (scenario.error) log(`  ERROR: ${scenario.error}`);
    for (const check of (scenario.checks || [])) {
      const status = check.pass ? 'PASS' : 'FAIL';
      const detail = check.detail ? ` [${check.detail}]` : '';
      log(`  [${status}] ${check.id}: ${check.desc}${detail}`);
      if (check.pass) totalPass++; else totalFail++;
    }
  }
  log(`\nTotal: ${totalPass} PASS, ${totalFail} FAIL`);

  // Save results
  const resultsPath = `${SCREENSHOTS_DIR}/uat-267-timer-results.json`;
  const output = {
    timestamp: new Date().toISOString(),
    baseUrl: BASE_URL,
    totalPass,
    totalFail,
    scenarios: RESULTS
  };
  fs.writeFileSync(resultsPath, JSON.stringify(output, null, 2));
  log(`Results saved to ${resultsPath}`);

  // Exit dengan code error jika ada FAIL
  if (totalFail > 0) {
    log('\nAda test yang FAIL. Periksa detail di atas.');
    // Tidak exit(1) agar bisa dianalisa lebih lanjut
  } else if (totalPass === 0) {
    log('\nTidak ada test yang berjalan. Pastikan assessment timer sudah disiapkan.');
  } else {
    log('\nSemua test PASS!');
  }
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
