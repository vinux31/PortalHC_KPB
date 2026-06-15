// Phase 386 PXF-04 (F-04) — e2e finalize round-trip: 1 essay DIKOSONGKAN tetap bisa "Selesaikan Penilaian".
//
// RED/fixme: di-gate `test.fixme()` — fix pending-count parity di controller dibangun Wave 3.
// UN-SKIP di Wave 3: ganti `test.fixme(` → `test(`. Sebelum itu suite tetap hijau.
//
// Akar F-04: essay dikosongkan → baris kosong dihitung "pending" selamanya → tombol "Selesaikan"
// hilang / finalize ditolak (dead-end). Sesudah fix: baris kosong (whitespace/null) BUKAN pending →
// finalize jalan, essay kosong kontribusi 0 (auto-0), bukan error "Jawaban tidak ditemukan".
//
// Selector dipin dari tests/e2e/essay-grading-384.spec.ts:25-33 (page /Admin/EssayGrading).
// Helper fillEssayAnswer dari tests/e2e/helpers/examTypes.ts:392 (worker StartExam).
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal, Phase 355)
//   2) cd tests; npx playwright test essay-empty-finalize-386 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

// Selector page per-worker /Admin/EssayGrading (single-source — reuse essay-grading-384.spec.ts).
const SEL = {
  scoreInput:  '.essay-score-input',
  saveBtn:     '.btn-save-essay-score',
  finalizeBtn: '.btn-finalize-grading',
};

// Inline login — accept any redirect away dari /Account/Login.
async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('PXF-04 Essay Empty Finalize — kosong tidak dead-end (F-04)', () => {

  // RED Wave 0: di-skip via test.fixme — pending-count parity fix dibangun Wave 3. TODO un-skip di Wave 3.
  test.fixme('Essay dikosongkan → "Selesaikan Penilaian" tetap bisa + kontribusi 0', async ({ page }) => {
    // Prasyarat: sesi PendingGrading dgn ≥2 essay, salah satunya DIKOSONGKAN peserta.
    // VERIFY: seed/resolve sessionId + nav param (title/category/scheduleDate) seperti essay-grading-384 beforeAll.
    const sessionId = 0;        // VERIFY runtime (seed Wave 3)
    const title = '';           // VERIFY runtime
    const category = '';        // VERIFY runtime
    const scheduleDate = '';    // VERIFY runtime (yyyy-MM-dd)

    const essayGradingUrl =
      `/Admin/EssayGrading?sessionId=${sessionId}`
      + '&title=' + encodeURIComponent(title)
      + '&category=' + encodeURIComponent(category)
      + '&scheduleDate=' + scheduleDate;

    await loginAny(page, 'hc');
    await page.goto(essayGradingUrl);

    // Tombol "Selesaikan Penilaian" TERLIHAT (tidak hilang) walau 1 essay kosong.
    await expect(page.locator(SEL.finalizeBtn).first()).toBeVisible();

    // Nilai essay yang terisi (skor penuh).
    await page.locator(SEL.scoreInput).first().fill('10');
    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/SubmitEssayScore') && res.status() === 200),
      page.locator(SEL.saveBtn).first().click(),
    ]);

    // Klik "Selesaikan Penilaian" — confirm() auto-accept dipasang SEBELUM klik.
    page.on('dialog', d => d.accept());
    const finalizeResp = page.waitForResponse(
      res => res.url().includes('/Admin/FinalizeEssayGrading') && res.status() === 200);
    await page.locator(SEL.finalizeBtn).first().click();
    const resp = await finalizeResp;

    // Sukses: bukan error "Jawaban tidak ditemukan"; essay kosong kontribusi 0 (auto-0).
    const body = await resp.json();
    expect(body.success).toBe(true);
    expect(JSON.stringify(body)).not.toMatch(/Jawaban tidak ditemukan/i);
  });
});
