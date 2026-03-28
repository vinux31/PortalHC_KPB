// UAT Phase 266: Continue from where Playwright MCP crashed
// Rino session 9 sudah di ExamSummary, semua 5 soal terjawab
// Script ini: Submit rino → Results → Certificate → Logout → Arsyad scenario
// Run: node uat-266-continue.js

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const SCREENSHOTS_DIR = 'uat-266-screenshots';
const RESULTS = [];
const NAV_TIMEOUT = 60000;

function log(msg) {
  const ts = new Date().toISOString().substring(11, 19);
  console.log(`[${ts}] ${msg}`);
}

async function screenshot(page, name) {
  try {
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/${name}.png`, fullPage: true, timeout: 15000 });
    log(`  Screenshot: ${name}.png`);
  } catch (e) {
    log(`  Screenshot FAILED (${name}): ${e.message.split('\n')[0]}`);
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
  await page.evaluate(() => document.querySelector('button[type="submit"]')?.click());
  await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
  await page.waitForTimeout(2000);
  log(`  Logged in as ${email}`);
}

async function logout(page) {
  await page.evaluate(() => {
    const form = document.querySelector('form[action*="Logout"]');
    if (form) { form.submit(); return; }
    const btn = document.querySelector('button[onclick*="Logout"], a[href*="Logout"]');
    if (btn) btn.click();
  });
  await page.waitForTimeout(2000);
  // Fallback: click avatar then logout
  try {
    const avatar = page.locator('.dropdown-toggle, [data-bs-toggle="dropdown"]').last();
    await avatar.click({ timeout: 3000 });
    await page.waitForTimeout(500);
    const logoutBtn = page.locator('button:has-text("Logout"), a:has-text("Logout")');
    await logoutBtn.click({ timeout: 3000 });
    await page.waitForTimeout(2000);
  } catch {}
  log('  Logged out');
}

function record(test, status, detail = '') {
  RESULTS.push({ test, status, detail });
  const icon = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️';
  log(`${icon} ${test}: ${status}${detail ? ' — ' + detail : ''}`);
}

async function main() {
  if (!fs.existsSync(SCREENSHOTS_DIR)) fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();

  // Auto-accept confirm dialogs
  page.on('dialog', async dialog => {
    log(`  Dialog: "${dialog.message()}" → accepting`);
    await dialog.accept();
  });

  try {
    // ============================================================
    // SKENARIO 1: RINO — Happy Path (submit → results → certificate)
    // ============================================================
    log('=== SKENARIO 1: RINO (Happy Path) ===');

    await login(page, 'rino.prasetyo@pertamina.com', 'TotenhimFeb!26');

    // Go to ExamSummary session 9
    log('Navigating to ExamSummary/9...');
    await goto(page, `${BASE_URL}/CMP/ExamSummary/9`);
    await screenshot(page, '01-rino-exam-summary');

    // Check if we're on ExamSummary or redirected to StartExam
    const summaryUrl = page.url();
    if (summaryUrl.includes('ExamSummary')) {
      log('  On ExamSummary page ✓');

      // Check all questions answered
      const alertText = await page.textContent('.alert-success, .alert').catch(() => '');
      log(`  Alert: ${alertText.trim()}`);

      // TEST 1 already passed (verified via MCP). Re-verify here.
      const rows = await page.locator('table tbody tr').count();
      record('SUBMIT-01: Daftar soal & status', rows >= 5 ? 'PASS' : 'FAIL', `${rows} rows in table`);

      // Click "Kumpulkan Ujian"
      log('Clicking Kumpulkan Ujian...');
      await page.click('button:has-text("Kumpulkan Ujian")');
      await page.waitForTimeout(5000);

      const afterSubmitUrl = page.url();
      log(`  After submit URL: ${afterSubmitUrl}`);
      await screenshot(page, '02-rino-after-submit');

      if (afterSubmitUrl.includes('Results')) {
        // TEST 3: Submit & Grading
        record('SUBMIT-03: Submit & grading', 'PASS', 'Redirected to Results page');

        // TEST 4: Results page — score & badge
        const scoreText = await page.textContent('.badge, [class*="score"], h1, h2, h3').catch(() => '');
        const pageContent = await page.content();
        const hasLulus = pageContent.includes('Lulus') || pageContent.includes('LULUS');
        const hasTidakLulus = pageContent.includes('Tidak Lulus') || pageContent.includes('TIDAK LULUS');
        const hasBadge = hasLulus || hasTidakLulus;
        const scoreMatch = pageContent.match(/(\d+)%/);
        const score = scoreMatch ? scoreMatch[1] : 'unknown';
        log(`  Score: ${score}%, Badge: ${hasLulus ? 'LULUS' : hasTidakLulus ? 'TIDAK LULUS' : 'NOT FOUND'}`);
        record('RESULT-01: Skor & badge', hasBadge ? 'PASS' : 'FAIL', `Score: ${score}%, Lulus: ${hasLulus}, TidakLulus: ${hasTidakLulus}`);

        await screenshot(page, '03-rino-results');

        // TEST 5: Review jawaban (AllowAnswerReview=true)
        const hasReview = pageContent.includes('Tinjauan Jawaban') || pageContent.includes('Answer Review');
        record('RESULT-02: Review jawaban', hasReview ? 'PASS' : 'FAIL', hasReview ? 'Section found' : 'Section NOT found');

        // Check ET analysis (RESULT-03 — might be N/A)
        const hasET = pageContent.includes('Elemen Teknis') || pageContent.includes('Analisis');
        log(`  ET Analysis section: ${hasET ? 'found' : 'not found (N/A expected if no ET data)'}`);

        await screenshot(page, '04-rino-results-full');

        // Scroll down for full page
        await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
        await page.waitForTimeout(1000);
        await screenshot(page, '05-rino-results-bottom');

        // TEST 6: Certificate (only if passed)
        if (hasLulus) {
          log('Rino LULUS — testing certificate...');
          const certLink = page.locator('a:has-text("Sertifikat"), a:has-text("Certificate"), a[href*="Certificate"]').first();
          const certExists = await certLink.count();

          if (certExists > 0) {
            await certLink.click();
            await page.waitForTimeout(5000);
            await screenshot(page, '06-rino-certificate');
            const certUrl = page.url();
            log(`  Certificate page: ${certUrl}`);
            record('CERT-01: Sertifikat preview', certUrl.includes('Certificate') ? 'PASS' : 'FAIL', certUrl);

            // Test PDF download
            const sessionId = afterSubmitUrl.match(/Results\/(\d+)/)?.[1] || '9';
            const pdfUrl = `${BASE_URL}/CMP/CertificatePdf/${sessionId}`;
            log(`  Testing PDF: ${pdfUrl}`);
            const pdfResponse = await page.goto(pdfUrl, { timeout: 30000 }).catch(e => null);
            if (pdfResponse) {
              const contentType = pdfResponse.headers()['content-type'] || '';
              const isPdf = contentType.includes('pdf');
              record('CERT-01: PDF download', isPdf ? 'PASS' : 'FAIL', `Content-Type: ${contentType}`);
            } else {
              record('CERT-01: PDF download', 'FAIL', 'No response');
            }
          } else {
            record('CERT-01: Sertifikat', 'FAIL', 'No certificate link found despite LULUS');
          }
        } else {
          log('Rino TIDAK LULUS — certificate test skipped for rino');
          record('CERT-01: Sertifikat', 'SKIP', 'Rino did not pass — will test with different data if needed');
        }

        // Extract Results session ID for DB verification
        const resultsSessionId = afterSubmitUrl.match(/Results\/(\d+)/)?.[1];
        log(`  Results session ID: ${resultsSessionId}`);

      } else if (afterSubmitUrl.includes('StartExam')) {
        // Timer probably expired again
        const errorText = await page.textContent('.alert-danger, .alert').catch(() => '');
        record('SUBMIT-03: Submit & grading', 'FAIL', `Redirected to StartExam. Error: ${errorText.trim()}`);
      } else {
        record('SUBMIT-03: Submit & grading', 'FAIL', `Unexpected URL: ${afterSubmitUrl}`);
      }

    } else if (summaryUrl.includes('StartExam')) {
      log('  Redirected to StartExam — need to navigate to summary first');
      // Try clicking Review and Submit
      await page.click('button:has-text("Review and Submit"), button:has-text("Selesai")').catch(() => {});
      await page.waitForTimeout(3000);
      record('SUBMIT-01: Daftar soal & status', 'SKIP', 'Redirected to StartExam, manual intervention needed');
    }

    // Logout rino
    await goto(page, `${BASE_URL}/Account/Login`);
    await page.waitForTimeout(2000);

    // ============================================================
    // SKENARIO 2: ARSYAD — Partial Submit (warning + possibly fail)
    // ============================================================
    log('\n=== SKENARIO 2: ARSYAD (Partial Submit) ===');

    await login(page, 'mohammad.arsyad@pertamina.com', '123456');

    // Navigate to Assessment page
    await goto(page, `${BASE_URL}/CMP/Assessment`);
    await screenshot(page, '10-arsyad-assessment');

    // Find arsyad's InProgress session
    const arsyadContent = await page.content();
    log(`  Assessment page URL: ${page.url()}`);

    // Look for Resume or Start Assessment button for assessment 10
    const resumeLink = page.locator('a:has-text("Resume")').first();
    const startBtn = page.locator('button:has-text("Start Assessment")').first();
    const resumeCount = await resumeLink.count();
    const startCount = await startBtn.count();
    log(`  Resume links: ${resumeCount}, Start buttons: ${startCount}`);

    let arsyadSessionId = null;

    if (resumeCount > 0) {
      const href = await resumeLink.getAttribute('href');
      arsyadSessionId = href.match(/StartExam\/(\d+)/)?.[1];
      log(`  Arsyad session ID: ${arsyadSessionId}`);
      await resumeLink.click();
    } else if (startCount > 0) {
      await startBtn.click();
    }
    await page.waitForTimeout(5000);
    await screenshot(page, '11-arsyad-exam');

    const examUrl = page.url();
    if (examUrl.includes('StartExam')) {
      // Get session ID from URL
      if (!arsyadSessionId) arsyadSessionId = examUrl.match(/StartExam\/(\d+)/)?.[1];
      log(`  On exam page, session: ${arsyadSessionId}`);

      // Answer ONLY first question (skip rest for partial submit test)
      const firstRadio = page.locator('input[type="radio"]').first();
      if (await firstRadio.count() > 0) {
        await firstRadio.click();
        await page.waitForTimeout(2000);
        log('  Answered first question only (partial)');
      }

      // Click Review and Submit
      await page.click('button:has-text("Review and Submit"), button:has-text("Selesai")').catch(() => {});
      await page.waitForTimeout(5000);
      await screenshot(page, '12-arsyad-exam-summary');

      const summaryContent = await page.content();
      const summaryPageUrl = page.url();
      log(`  After review URL: ${summaryPageUrl}`);

      if (summaryPageUrl.includes('ExamSummary')) {
        // TEST 2: Warning soal belum dijawab
        const hasWarning = summaryContent.includes('table-warning') || summaryContent.includes('alert-warning') || summaryContent.includes('belum dijawab');
        record('SUBMIT-02: Warning soal belum dijawab', hasWarning ? 'PASS' : 'FAIL',
          hasWarning ? 'Warning visual ditemukan' : 'Warning NOT found');

        // Submit
        log('Clicking Kumpulkan Ujian (arsyad)...');
        await page.click('button:has-text("Kumpulkan Ujian")');
        await page.waitForTimeout(5000);

        const arsyadAfterUrl = page.url();
        log(`  After submit URL: ${arsyadAfterUrl}`);
        await screenshot(page, '13-arsyad-results');

        if (arsyadAfterUrl.includes('Results')) {
          const arsyadPageContent = await page.content();
          const arsyadHasLulus = arsyadPageContent.includes('Lulus') && !arsyadPageContent.includes('Tidak Lulus');
          const arsyadHasTidakLulus = arsyadPageContent.includes('Tidak Lulus');
          log(`  Arsyad: Lulus=${arsyadHasLulus}, TidakLulus=${arsyadHasTidakLulus}`);

          // TEST 7: Worker gagal tidak ada tombol sertifikat
          const hasCertBtn = arsyadPageContent.includes('Sertifikat') || arsyadPageContent.includes('Certificate');
          if (arsyadHasTidakLulus) {
            record('TEST-07: Gagal tidak ada sertifikat', !hasCertBtn ? 'PASS' : 'FAIL',
              hasCertBtn ? 'Certificate button found for failed worker!' : 'No certificate button ✓');
          } else {
            record('TEST-07: Gagal tidak ada sertifikat', 'SKIP', 'Arsyad unexpectedly passed');
          }

          await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
          await page.waitForTimeout(1000);
          await screenshot(page, '14-arsyad-results-bottom');
        } else {
          record('SUBMIT-02: Arsyad submit', 'FAIL', `Unexpected URL: ${arsyadAfterUrl}`);
        }
      }
    } else {
      log(`  Unexpected URL for arsyad exam: ${examUrl}`);
      record('ARSYAD scenario', 'FAIL', `Could not reach exam page: ${examUrl}`);
    }

  } catch (e) {
    log(`FATAL ERROR: ${e.message}`);
    await screenshot(page, 'error-final').catch(() => {});
  } finally {
    // Print summary
    log('\n========================================');
    log('UAT RESULTS SUMMARY');
    log('========================================');
    for (const r of RESULTS) {
      const icon = r.status === 'PASS' ? '✅' : r.status === 'FAIL' ? '❌' : '⚠️';
      log(`${icon} ${r.test}: ${r.status} ${r.detail ? '(' + r.detail + ')' : ''}`);
    }

    const passed = RESULTS.filter(r => r.status === 'PASS').length;
    const failed = RESULTS.filter(r => r.status === 'FAIL').length;
    const skipped = RESULTS.filter(r => r.status === 'SKIP').length;
    log(`\nTotal: ${RESULTS.length} | PASS: ${passed} | FAIL: ${failed} | SKIP: ${skipped}`);

    // Save results JSON
    fs.writeFileSync(`${SCREENSHOTS_DIR}/results.json`, JSON.stringify(RESULTS, null, 2));
    log(`Results saved to ${SCREENSHOTS_DIR}/results.json`);

    await browser.close();
  }
}

main().catch(console.error);
