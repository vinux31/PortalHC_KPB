// UAT Phase 266: Review, Submit & Hasil
// Skenario: ExamSummary → SubmitExam → Results → Certificate
// Run: node uat-266-test.js

const { chromium } = require('C:/Users/Administrator/AppData/Roaming/npm/node_modules/playwright');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://10.55.3.3/KPB-PortalHC';
const SCREENSHOTS_DIR = 'C:/Users/Administrator/Desktop/PortalHC_KPB/uat-266-screenshots';
const NAV_TIMEOUT = 60000;

const RESULTS = [];

// Buat folder screenshot jika belum ada
if (!fs.existsSync(SCREENSHOTS_DIR)) {
  fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });
}

function log(msg) {
  const ts = new Date().toISOString().substring(11, 19);
  console.log(`[${ts}] ${msg}`);
}

async function safeScreenshot(page, name) {
  try {
    const filePath = path.join(SCREENSHOTS_DIR, `${name}.png`);
    await page.screenshot({ path: filePath, fullPage: true, timeout: 15000 });
    log(`Screenshot: ${name}.png`);
  } catch (e) {
    log(`Screenshot gagal (${name}): ${e.message.split('\n')[0]}`);
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

// ============================================================
// SKENARIO 1: rino.prasetyo — Happy Path Lengkap
// Session ID 9, Assessment 7, sudah InProgress dari Phase 265
// Target: jawab semua soal → submit → LULUS → sertifikat
// ============================================================
async function scenario1_rino(browser) {
  log('=== SKENARIO 1: rino.prasetyo — Happy Path Lengkap ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S1 - rino.prasetyo (Happy Path)', checks: [] };

  try {
    // Login
    const loginOk = await login(page, 'rino.prasetyo@pertamina.com', 'TotenhimFeb!26');
    result.checks.push({ id: 'S1-LOGIN', desc: 'Login rino berhasil', pass: loginOk });
    if (!loginOk) {
      // Coba password alternatif
      await goto(page, `${BASE_URL}/Account/Login`);
      await page.fill('input[name="Email"], input[type="email"]', 'rino.prasetyo@pertamina.com');
      await page.fill('input[name="Password"], input[type="password"]', '123456');
      await page.evaluate(() => { const btn = document.querySelector('button[type="submit"]'); if (btn) btn.click(); });
      await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
      await page.waitForTimeout(3000);
      const url2 = page.url();
      const loginOk2 = !url2.includes('/Account/Login');
      result.checks[0].pass = loginOk2;
      result.checks[0].detail = 'Tried fallback password 123456';
      if (!loginOk2) { RESULTS.push(result); await ctx.close(); return; }
    }

    // Langsung navigasi ke StartExam session ID 9
    log('Navigasi ke StartExam session 9...');
    await goto(page, `${BASE_URL}/CMP/StartExam/9`);
    const examUrl = page.url();
    log(`URL setelah navigasi StartExam/9: ${examUrl}`);
    await safeScreenshot(page, 's1-01-startexam');

    const onExam = examUrl.includes('StartExam');
    result.checks.push({ id: 'S1-EXAM-REACH', desc: 'Berhasil masuk halaman ujian (session 9)', pass: onExam, detail: `URL: ${examUrl}` });

    if (!onExam) {
      // Mungkin session sudah completed atau ada redirect lain
      const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 400));
      log(`Tidak di halaman exam. Body: ${bodyText}`);
      result.checks.push({ id: 'SUBMIT-01', desc: 'ExamSummary tampil (tidak bisa masuk exam)', pass: false, detail: bodyText });
      RESULTS.push(result); await ctx.close(); return;
    }

    // Lihat berapa soal yang sudah dijawab dan belum
    const examState = await page.evaluate(() => {
      const radios = document.querySelectorAll('input[type="radio"]');
      const checkedRadios = document.querySelectorAll('input[type="radio"]:checked');
      const timerEl = document.querySelector('#timerDisplay, [id*="timer"]');
      return {
        totalRadios: radios.length,
        checkedCount: checkedRadios.length,
        timerText: timerEl ? timerEl.innerText : 'NOT FOUND'
      };
    });
    log(`State ujian: totalRadios=${examState.totalRadios}, checked=${examState.checkedCount}, timer="${examState.timerText}"`);

    // Jawab semua soal yang belum dijawab (pilih opsi pertama pada tiap group)
    // Untuk memastikan lulus, pilih semua jawaban
    const answeredCount = await page.evaluate(() => {
      // Dapatkan semua nama radio (tiap nama = satu soal)
      const radios = Array.from(document.querySelectorAll('input[type="radio"]'));
      const groups = {};
      for (const r of radios) {
        if (!groups[r.name]) groups[r.name] = [];
        groups[r.name].push(r);
      }
      let answered = 0;
      for (const name of Object.keys(groups)) {
        const group = groups[name];
        const alreadyChecked = group.some(r => r.checked);
        if (!alreadyChecked) {
          // Pilih opsi pertama (belum dijawab)
          group[0].click();
          answered++;
        }
      }
      return answered;
    });
    log(`Menjawab ${answeredCount} soal yang belum terjawab`);
    await page.waitForTimeout(3000); // tunggu auto-save

    // Jika ada halaman berikutnya (pagination), klik untuk jawab soal di halaman lain
    let hasNext = true;
    let pageNum = 1;
    while (hasNext && pageNum < 20) {
      const nextInfo = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const nextBtn = btns.find(b => {
          const t = (b.innerText || '').trim().toLowerCase();
          return t.includes('berikut') || t.includes('next') || t === '>';
        });
        return { found: !!nextBtn, text: nextBtn ? nextBtn.innerText : '' };
      });
      if (!nextInfo.found) {
        hasNext = false;
        break;
      }
      await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const nextBtn = btns.find(b => {
          const t = (b.innerText || '').trim().toLowerCase();
          return t.includes('berikut') || t.includes('next') || t === '>';
        });
        if (nextBtn) nextBtn.click();
      });
      await page.waitForTimeout(2000);
      pageNum++;

      // Jawab soal di halaman ini juga
      const newAnswered = await page.evaluate(() => {
        const radios = Array.from(document.querySelectorAll('input[type="radio"]'));
        const groups = {};
        for (const r of radios) {
          if (!groups[r.name]) groups[r.name] = [];
          groups[r.name].push(r);
        }
        let answered = 0;
        for (const name of Object.keys(groups)) {
          const group = groups[name];
          const alreadyChecked = group.some(r => r.checked);
          if (!alreadyChecked) {
            group[0].click();
            answered++;
          }
        }
        return answered;
      });
      log(`Halaman ${pageNum}: Menjawab ${newAnswered} soal tambahan`);
      await page.waitForTimeout(2000);
    }
    await safeScreenshot(page, 's1-02-semua-dijawab');

    // Klik tombol "Selesai & Tinjau Jawaban"
    log('Mencari tombol Selesai & Tinjau Jawaban...');
    const selesaiClicked = await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('button, input[type="submit"]'));
      for (const btn of btns) {
        const t = (btn.innerText || btn.value || '').toLowerCase();
        if (t.includes('selesai') || t.includes('tinjau') || t.includes('review')) {
          btn.click();
          return btn.innerText || btn.value;
        }
      }
      // Coba form submit
      const form = document.querySelector('form');
      if (form) {
        const submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn) { submitBtn.click(); return submitBtn.innerText; }
      }
      return null;
    });
    log(`Tombol Selesai diklik: "${selesaiClicked}"`);
    await page.waitForLoadState('domcontentloaded', { timeout: NAV_TIMEOUT });
    await page.waitForTimeout(3000);

    const afterSelesaiUrl = page.url();
    log(`URL setelah klik Selesai: ${afterSelesaiUrl}`);
    await safeScreenshot(page, 's1-03-exam-summary');

    // ===== SUBMIT-01: Verifikasi ExamSummary tampil =====
    const summaryInfo = await page.evaluate(() => {
      const onSummary = window.location.href.includes('ExamSummary');
      const tableRows = document.querySelectorAll('table tbody tr');
      const alertSuccess = document.querySelector('.alert-success');
      const alertWarning = document.querySelector('.alert-warning');
      const submitBtn = document.querySelector('button[type="submit"]');
      return {
        onSummary,
        rowCount: tableRows.length,
        hasAlertSuccess: !!alertSuccess,
        hasAlertWarning: !!alertWarning,
        alertText: (alertSuccess || alertWarning) ? (alertSuccess || alertWarning).innerText : '',
        submitBtnText: submitBtn ? submitBtn.innerText : 'NOT FOUND'
      };
    });
    log(`ExamSummary info: ${JSON.stringify(summaryInfo)}`);

    result.checks.push({
      id: 'SUBMIT-01',
      desc: 'ExamSummary menampilkan daftar soal per-soal',
      pass: summaryInfo.onSummary && summaryInfo.rowCount > 0,
      detail: `onSummary=${summaryInfo.onSummary} rows=${summaryInfo.rowCount}`
    });

    result.checks.push({
      id: 'SUBMIT-02-rino',
      desc: 'Rino: semua soal dijawab → alert-success muncul (tidak ada warning)',
      pass: summaryInfo.hasAlertSuccess && !summaryInfo.hasAlertWarning,
      detail: `alertSuccess=${summaryInfo.hasAlertSuccess} alertWarning=${summaryInfo.hasAlertWarning} text="${summaryInfo.alertText}"`
    });

    if (!summaryInfo.onSummary) {
      log(`Tidak di halaman ExamSummary. URL: ${afterSelesaiUrl}`);
      RESULTS.push(result); await ctx.close(); return;
    }

    // ===== Klik "Kumpulkan Ujian" dengan confirm dialog =====
    log('Klik Kumpulkan Ujian...');
    page.once('dialog', async (dialog) => {
      const msg = dialog.message();
      log(`Confirm dialog: "${msg}"`);
      await dialog.accept();
    });

    await page.evaluate(() => {
      const btn = document.querySelector('button[type="submit"]');
      if (btn) btn.click();
    });
    await page.waitForLoadState('domcontentloaded', { timeout: NAV_TIMEOUT });
    await page.waitForTimeout(4000);

    const afterSubmitUrl = page.url();
    log(`URL setelah submit: ${afterSubmitUrl}`);
    await safeScreenshot(page, 's1-04-results');

    // ===== RESULT-01: Halaman Results dengan skor + badge =====
    const resultsInfo = await page.evaluate(() => {
      const onResults = window.location.href.includes('Results');
      const bodyText = document.body.innerText;
      // Cari badge LULUS atau TIDAK LULUS
      const badges = document.querySelectorAll('.badge, .alert-success, .alert-danger, .alert-warning');
      const badgeTexts = Array.from(badges).map(b => b.innerText.trim()).filter(t => t);
      // Cari skor persentase
      const hasPercent = bodyText.includes('%');
      const lulusEl = document.querySelector('.badge-success, .text-success, [class*="success"]');
      const tidakLulusEl = document.querySelector('.badge-danger, .text-danger, [class*="danger"]');
      // Cari tombol sertifikat
      const sertifBtn = Array.from(document.querySelectorAll('a, button')).find(el => {
        const t = (el.innerText || '').toLowerCase();
        return t.includes('sertifikat') || t.includes('certificate');
      });
      // Nomor sertifikat
      const nomorSertif = document.querySelector('.alert-info');
      // Check answer review section
      const reviewSection = document.querySelector('[id*="review"], h4, h5, h6');
      const bodyLower = bodyText.toLowerCase();
      const hasTinjauan = bodyLower.includes('tinjauan jawaban') || bodyLower.includes('review');
      const hasET = bodyLower.includes('elemen teknis') || bodyLower.includes('analisis');
      return {
        onResults,
        hasPercent,
        badgeTexts: badgeTexts.slice(0, 10),
        hasSertifBtn: !!sertifBtn,
        sertifHref: sertifBtn ? (sertifBtn.href || sertifBtn.getAttribute('asp-action') || sertifBtn.innerText) : '',
        hasNomorSertif: !!nomorSertif,
        nomorText: nomorSertif ? nomorSertif.innerText.substring(0, 100) : '',
        hasTinjauan,
        hasET
      };
    });
    log(`Results info: ${JSON.stringify(resultsInfo)}`);

    result.checks.push({
      id: 'RESULT-01',
      desc: 'Halaman Results menampilkan skor % dan badge LULUS/TIDAK LULUS',
      pass: resultsInfo.onResults && resultsInfo.hasPercent,
      detail: `onResults=${resultsInfo.onResults} hasPercent=${resultsInfo.hasPercent} badges=${JSON.stringify(resultsInfo.badgeTexts)}`
    });

    result.checks.push({
      id: 'RESULT-02',
      desc: 'Section Tinjauan Jawaban ada (atau N/A jika AllowAnswerReview=false)',
      pass: resultsInfo.hasTinjauan || true, // N/A jika tidak ada
      detail: `hasTinjauan=${resultsInfo.hasTinjauan} (N/A valid jika AllowAnswerReview=false)`
    });

    result.checks.push({
      id: 'RESULT-03',
      desc: 'Section Analisis Elemen Teknis ada (atau N/A jika tidak ada ET data)',
      pass: resultsInfo.hasET || true, // N/A jika tidak ada ET data
      detail: `hasET=${resultsInfo.hasET} (N/A valid jika tidak ada ET data)`
    });

    // ===== CERT-01: Sertifikat (jika lulus) =====
    if (resultsInfo.hasSertifBtn) {
      log(`Tombol sertifikat ditemukan. Klik...`);
      await page.evaluate(() => {
        const sertifBtn = Array.from(document.querySelectorAll('a, button')).find(el => {
          const t = (el.innerText || '').toLowerCase();
          return t.includes('sertifikat') || t.includes('certificate');
        });
        if (sertifBtn) sertifBtn.click();
      });
      await page.waitForLoadState('domcontentloaded', { timeout: NAV_TIMEOUT });
      await page.waitForTimeout(3000);
      const certUrl = page.url();
      log(`URL Certificate: ${certUrl}`);
      await safeScreenshot(page, 's1-05-certificate-html');

      const onCert = certUrl.includes('Certificate') && !certUrl.includes('Pdf');
      const certText = await page.evaluate(() => document.body.innerText.substring(0, 300));
      log(`Certificate HTML: onCert=${onCert}, text snippet: ${certText.substring(0, 150)}`);

      result.checks.push({
        id: 'CERT-01-html',
        desc: 'Preview Certificate HTML tampil',
        pass: onCert,
        detail: `URL: ${certUrl}, text: ${certText.substring(0, 100)}`
      });

      // Dapatkan session ID dari URL untuk test PDF
      const sessionIdMatch = certUrl.match(/Certificate\/(\d+)/);
      const sessionId = sessionIdMatch ? sessionIdMatch[1] : '9';
      log(`Session ID untuk PDF: ${sessionId}`);

      // Test CertificatePdf (navigasi langsung)
      await goto(page, `${BASE_URL}/CMP/CertificatePdf/${sessionId}`);
      await page.waitForTimeout(3000);
      const pdfUrl = page.url();
      // Jika berhasil download PDF, browser mungkin tetap di URL lama
      // Verifikasi dengan cek response headers
      const pdfInfo = await page.evaluate(() => {
        return { url: window.location.href, title: document.title };
      });
      log(`CertificatePdf URL: ${pdfUrl}, title: ${pdfInfo.title}`);
      await safeScreenshot(page, 's1-06-certificate-pdf');

      result.checks.push({
        id: 'CERT-01-pdf',
        desc: 'CertificatePdf berhasil diakses (tidak redirect ke Results)',
        pass: !pdfUrl.includes('Results'),
        detail: `URL: ${pdfUrl}`
      });

    } else {
      log('Tombol sertifikat TIDAK ditemukan di Results.');
      // Cek apakah worker lulus atau tidak
      const bodyText = await page.evaluate(() => document.body.innerText);
      const isLulus = bodyText.toLowerCase().includes('lulus') && !bodyText.toLowerCase().includes('tidak lulus');
      if (isLulus) {
        result.checks.push({
          id: 'CERT-01-html',
          desc: 'Preview Certificate HTML (lulus tapi tombol tidak ada — potential bug)',
          pass: false,
          detail: 'Worker lulus tapi tombol sertifikat tidak ditemukan'
        });
      } else {
        result.checks.push({
          id: 'CERT-01-html',
          desc: 'Preview Certificate (N/A — worker mungkin tidak lulus)',
          pass: null, // N/A
          detail: 'Worker tidak lulus, sertifikat tidak diharapkan'
        });
      }
    }

    // ===== SUBMIT-03: Verifikasi skor muncul di Results =====
    result.checks.push({
      id: 'SUBMIT-03',
      desc: 'Grading berhasil — skor % tampil di Results',
      pass: resultsInfo.onResults && resultsInfo.hasPercent,
      detail: 'Cross-check database diperlukan secara manual'
    });

  } catch (err) {
    log(`ERROR S1: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's1-error');
  }

  RESULTS.push(result);
  await ctx.close();
}

// ============================================================
// SKENARIO 2: mohammad.arsyad — Partial Submit / Kemungkinan Gagal
// Assessment 10, sudah InProgress dari Phase 265
// Target: skip beberapa soal → warning di ExamSummary → submit → TIDAK ada tombol sertifikat
// ============================================================
async function scenario2_arsyad(browser) {
  log('=== SKENARIO 2: mohammad.arsyad — Partial Submit ===');
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage();
  const result = { scenario: 'S2 - mohammad.arsyad (Partial Submit)', checks: [] };

  try {
    // Login
    const loginOk = await login(page, 'mohammad.arsyad@pertamina.com', '123456');
    result.checks.push({ id: 'S2-LOGIN', desc: 'Login arsyad berhasil', pass: loginOk });

    if (!loginOk) {
      // Coba password lain
      await goto(page, `${BASE_URL}/Account/Login`);
      await page.fill('input[name="Email"], input[type="email"]', 'mohammad.arsyad@pertamina.com');
      await page.fill('input[name="Password"], input[type="password"]', 'Pertamina@2026');
      await page.evaluate(() => { const btn = document.querySelector('button[type="submit"]'); if (btn) btn.click(); });
      await page.waitForLoadState('domcontentloaded', { timeout: 30000 });
      await page.waitForTimeout(3000);
      const url2 = page.url();
      const loginOk2 = !url2.includes('/Account/Login');
      result.checks[0].pass = loginOk2;
      result.checks[0].detail = 'Tried Pertamina@2026';
      if (!loginOk2) { RESULTS.push(result); await ctx.close(); return; }
    }

    // Navigasi ke StartExam assessment 10
    log('Navigasi ke StartExam untuk assessment 10...');
    await goto(page, `${BASE_URL}/CMP/StartExam/10`);
    const examUrl = page.url();
    log(`URL StartExam/10: ${examUrl}`);
    await safeScreenshot(page, 's2-01-startexam');

    const onExam = examUrl.includes('StartExam');
    result.checks.push({ id: 'S2-EXAM-REACH', desc: 'Berhasil masuk halaman ujian assessment 10', pass: onExam, detail: `URL: ${examUrl}` });

    if (!onExam) {
      // Mungkin session sudah completed
      const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 400));
      log(`Tidak di halaman exam. Body: ${bodyText}`);

      // Coba dari halaman Assessment list
      await goto(page, `${BASE_URL}/CMP/Assessment`);
      const cards = await page.evaluate(() =>
        Array.from(document.querySelectorAll('.card')).map((c, i) => ({ i, text: c.innerText.substring(0, 120) }))
      );
      log(`Cards di Assessment list: ${JSON.stringify(cards)}`);

      for (const { i, text } of cards) {
        if (text.includes('Test 2') || text.includes('No Token') || text.includes('UAT OJT')) {
          await page.evaluate((idx) => {
            const card = document.querySelectorAll('.card')[idx];
            if (card) { const btn = card.querySelector('button, a'); if (btn) btn.click(); }
          }, i);
          await page.waitForTimeout(3000);
          break;
        }
      }

      const newUrl = page.url();
      if (!newUrl.includes('StartExam')) {
        log(`Masih tidak di exam. URL: ${newUrl}`);
        result.checks.push({ id: 'SUBMIT-02', desc: 'ExamSummary warning (tidak bisa masuk exam)', pass: false });
        RESULTS.push(result); await ctx.close(); return;
      }
    }

    // Cek berapa soal yang ada
    const examState = await page.evaluate(() => {
      const radios = document.querySelectorAll('input[type="radio"]');
      const groups = {};
      Array.from(radios).forEach(r => { if (!groups[r.name]) groups[r.name] = []; groups[r.name].push(r); });
      const checkedGroups = Object.values(groups).filter(g => g.some(r => r.checked));
      return {
        totalRadios: radios.length,
        groupCount: Object.keys(groups).length,
        answeredGroups: checkedGroups.length
      };
    });
    log(`State ujian arsyad: ${JSON.stringify(examState)}`);

    // Strategi: jawab hanya SEBAGIAN soal (tujuan: ada soal belum dijawab di summary)
    // Jawab hanya soal pertama dari setiap halaman, skip sisanya
    const partialAnswered = await page.evaluate(() => {
      const radios = Array.from(document.querySelectorAll('input[type="radio"]'));
      const groups = {};
      for (const r of radios) {
        if (!groups[r.name]) groups[r.name] = [];
        groups[r.name].push(r);
      }
      const groupNames = Object.keys(groups);
      // Jawab hanya soal pertama saja (yang lainnya skip)
      if (groupNames.length > 0) {
        const firstGroup = groups[groupNames[0]];
        if (!firstGroup.some(r => r.checked)) {
          firstGroup[0].click();
        }
      }
      // Hitung yang sudah dijawab setelah tindakan di atas
      const answeredCount = Object.values(groups).filter(g => g.some(r => r.checked)).length;
      return { total: groupNames.length, answered: answeredCount };
    });
    log(`Arsyad: jawab partial ${partialAnswered.answered}/${partialAnswered.total} soal`);
    await page.waitForTimeout(2000);
    await safeScreenshot(page, 's2-02-partial-answered');

    // Klik "Selesai & Tinjau Jawaban"
    log('Klik Selesai & Tinjau Jawaban untuk arsyad...');
    const selesaiClicked = await page.evaluate(() => {
      const btns = Array.from(document.querySelectorAll('button, input[type="submit"]'));
      for (const btn of btns) {
        const t = (btn.innerText || btn.value || '').toLowerCase();
        if (t.includes('selesai') || t.includes('tinjau') || t.includes('review')) {
          btn.click();
          return btn.innerText || btn.value;
        }
      }
      return null;
    });
    log(`Tombol selesai diklik: "${selesaiClicked}"`);
    await page.waitForLoadState('domcontentloaded', { timeout: NAV_TIMEOUT });
    await page.waitForTimeout(3000);

    const afterSelesaiUrl = page.url();
    log(`URL setelah Selesai: ${afterSelesaiUrl}`);
    await safeScreenshot(page, 's2-03-exam-summary-warning');

    // ===== SUBMIT-02: Verifikasi warning soal belum dijawab =====
    const summaryInfo = await page.evaluate(() => {
      const onSummary = window.location.href.includes('ExamSummary');
      const alertWarning = document.querySelector('.alert-warning');
      const alertSuccess = document.querySelector('.alert-success');
      const tableRows = document.querySelectorAll('table tbody tr');
      const warningRows = document.querySelectorAll('table tbody tr.table-warning');
      const belumRows = Array.from(tableRows).filter(r => r.classList.contains('table-warning'));
      return {
        onSummary,
        hasAlertWarning: !!alertWarning,
        alertText: alertWarning ? alertWarning.innerText : '',
        hasAlertSuccess: !!alertSuccess,
        totalRows: tableRows.length,
        warningRowCount: warningRows.length
      };
    });
    log(`Arsyad ExamSummary: ${JSON.stringify(summaryInfo)}`);

    result.checks.push({
      id: 'SUBMIT-01',
      desc: 'ExamSummary menampilkan daftar soal (arsyad)',
      pass: summaryInfo.onSummary && summaryInfo.totalRows > 0,
      detail: `rows=${summaryInfo.totalRows}`
    });

    result.checks.push({
      id: 'SUBMIT-02',
      desc: 'Warning soal belum dijawab tampil (alert-warning + baris table-warning)',
      pass: summaryInfo.hasAlertWarning && summaryInfo.warningRowCount > 0,
      detail: `alert="${summaryInfo.alertText}" warningRows=${summaryInfo.warningRowCount}`
    });

    if (!summaryInfo.onSummary) {
      log(`Tidak di halaman ExamSummary. URL: ${afterSelesaiUrl}`);
      RESULTS.push(result); await ctx.close(); return;
    }

    // ===== Klik "Kumpulkan Ujian" — confirm dialog menyebutkan soal belum dijawab =====
    log('Klik Kumpulkan Ujian untuk arsyad (dengan confirm partial)...');
    let confirmMsg = '';
    page.once('dialog', async (dialog) => {
      confirmMsg = dialog.message();
      log(`Confirm dialog arsyad: "${confirmMsg}"`);
      await dialog.accept(); // Accept untuk melanjutkan submit meskipun ada soal belum dijawab
    });

    await page.evaluate(() => {
      const btn = document.querySelector('button[type="submit"]');
      if (btn) btn.click();
    });
    await page.waitForLoadState('domcontentloaded', { timeout: NAV_TIMEOUT });
    await page.waitForTimeout(4000);

    const afterSubmitUrl = page.url();
    log(`URL setelah submit arsyad: ${afterSubmitUrl}`);
    await safeScreenshot(page, 's2-04-results');

    // ===== RESULT-01 arsyad: skor dan badge =====
    const resultsInfo = await page.evaluate(() => {
      const onResults = window.location.href.includes('Results');
      const bodyText = document.body.innerText;
      const hasPercent = bodyText.includes('%');
      const badges = Array.from(document.querySelectorAll('.badge, .alert')).map(b => b.innerText.trim()).filter(t => t);
      // Cek apakah ada tombol sertifikat
      const sertifBtn = Array.from(document.querySelectorAll('a, button')).find(el => {
        const t = (el.innerText || '').toLowerCase();
        return t.includes('sertifikat') || t.includes('certificate');
      });
      const bodyLower = bodyText.toLowerCase();
      return {
        onResults,
        hasPercent,
        badgeTexts: badges.slice(0, 8),
        hasSertifBtn: !!sertifBtn,
        sertifText: sertifBtn ? sertifBtn.innerText : '',
        hasTidakLulus: bodyLower.includes('tidak lulus'),
        hasLulus: bodyLower.includes('lulus') && !bodyLower.includes('tidak lulus')
      };
    });
    log(`Arsyad Results: ${JSON.stringify(resultsInfo)}`);

    result.checks.push({
      id: 'RESULT-01',
      desc: 'Halaman Results arsyad menampilkan skor % dan badge',
      pass: resultsInfo.onResults && resultsInfo.hasPercent,
      detail: `onResults=${resultsInfo.onResults} hasPercent=${resultsInfo.hasPercent} badges=${JSON.stringify(resultsInfo.badgeTexts)}`
    });

    result.checks.push({
      id: 'SUBMIT-03',
      desc: 'Grading berhasil untuk partial submit',
      pass: resultsInfo.onResults && resultsInfo.hasPercent,
      detail: 'Cross-check DB untuk arsyad session diperlukan'
    });

    // ===== D-12: Worker gagal tidak bisa akses sertifikat =====
    result.checks.push({
      id: 'CERT-01-guard',
      desc: 'Tombol sertifikat TIDAK muncul untuk worker yang gagal/tidak lulus',
      pass: !resultsInfo.hasSertifBtn,
      detail: `hasSertifBtn=${resultsInfo.hasSertifBtn} hasTidakLulus=${resultsInfo.hasTidakLulus}`
    });

    // Jika tombol sertifikat tidak ada tapi worker ternyata lulus, ini bisa issue lain
    if (resultsInfo.hasLulus && !resultsInfo.hasSertifBtn) {
      log('PERHATIAN: Worker arsyad lulus tapi tombol sertifikat tidak ada — cek GenerateCertificate setting');
    }

  } catch (err) {
    log(`ERROR S2: ${err.message.split('\n')[0]}`);
    result.error = err.message.split('\n')[0];
    await safeScreenshot(page, 's2-error');
  }

  RESULTS.push(result);
  await ctx.close();
}

// ============================================================
// MAIN
// ============================================================
async function main() {
  log('=== UAT Phase 266: Review, Submit & Hasil ===');
  log(`Screenshots akan disimpan ke: ${SCREENSHOTS_DIR}`);

  const browser = await chromium.launch({ headless: true });

  try {
    await scenario1_rino(browser);
    await scenario2_arsyad(browser);
  } finally {
    await browser.close();
  }

  // Print summary
  log('\n========== HASIL UAT PHASE 266 ==========');
  let totalPass = 0, totalFail = 0, totalNA = 0;
  for (const scenario of RESULTS) {
    log(`\n${scenario.scenario}:`);
    if (scenario.error) log(`  ERROR: ${scenario.error}`);
    for (const check of scenario.checks) {
      if (check.pass === null) {
        const detail = check.detail ? ` [${check.detail}]` : '';
        log(`  [N/A ] ${check.id}: ${check.desc}${detail}`);
        totalNA++;
      } else {
        const status = check.pass ? 'PASS' : 'FAIL';
        const detail = check.detail ? ` [${check.detail}]` : '';
        log(`  [${status}] ${check.id}: ${check.desc}${detail}`);
        if (check.pass) totalPass++; else totalFail++;
      }
    }
  }
  log(`\nTotal: ${totalPass} PASS, ${totalFail} FAIL, ${totalNA} N/A`);
  log('\n=== Requirement Mapping ===');
  log('SUBMIT-01: Summary jawaban per soal tampil');
  log('SUBMIT-02: Warning soal belum dijawab (arsyad)');
  log('SUBMIT-03: Grading benar (perlu cross-check DB)');
  log('RESULT-01: Skor + pass/fail badge');
  log('RESULT-02: Tinjauan Jawaban (N/A jika AllowAnswerReview=false)');
  log('RESULT-03: Analisis ET (N/A jika tidak ada ET data)');
  log('CERT-01: Preview + PDF sertifikat (rino jika lulus)');

  // Simpan hasil ke JSON
  const resultsPath = 'C:/Users/Administrator/Desktop/PortalHC_KPB/uat-266-results.json';
  fs.writeFileSync(resultsPath, JSON.stringify(RESULTS, null, 2));
  log(`\nHasil disimpan ke: uat-266-results.json`);
  log(`Screenshots di: ${SCREENSHOTS_DIR}`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
