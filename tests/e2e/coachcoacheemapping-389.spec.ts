// Phase 389 — CoachCoacheeMapping accordion parity (DSN-01/02/03 + smoke parity DSN-06).
//
// Tujuan: kontrak verifikasi RUNTIME untuk redesign CoachCoacheeMapping jadi accordion card
// per coach (DSN-01 header avatar+nama+section+badge threshold / DSN-02 collapse buka-tutup
// independen + mini-tabel 9 kolom / DSN-03 toolbar seragam + hapus dead-onclick) PLUS smoke
// parity aksi existing (DSN-06*: edit/delete modal, aksi branch, AJAX appUrl, filter).
//
// TEST-FIRST (Wave 1): spec ini ditulis terhadap markup TARGET (accordion card). Markup lama
// masih tabel grouped → sebagian besar test akan RED hingga Plan 02 me-rewrite view — itu BENAR.
// Tugas Wave 1 HANYA menulis spec valid & parse-able (npx playwright test --list daftar 14 test);
// JANGAN jalankan full-green di sini. Mutasi data PENUH (assign/import/export end-to-end) = Phase 390.
//
// CARA RUN (app TIDAK auto-start — playwright.config.ts tak punya webServer):
//   1) dotnet run  dengan env Authentication__UseActiveDirectory=false  (login admin lokal)
//   2) cd tests; npx playwright test coachcoacheemapping-389 --workers=1
//      (--workers=1 WAJIB — NTLM loopback/shared-memory SQL conn, ref: local e2e SQL env fix)
//
// Data note: test struktural (V-01..V-09) skip bila DB lokal tak punya coach group; test parity
// (V-10..V-14) pakai test.skip data-guard bila tak ada coachee/baris disposable. JANGAN buat seed permanen.

import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

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

test.describe('Phase 389 — CoachCoacheeMapping accordion parity (DSN-01/02/03)', () => {

  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CoachCoacheeMapping');
    await expect(page.locator('h2', { hasText: 'Coach-Coachee Mapping' })).toBeVisible();
  });

  // V-01 (DSN-01): tiap coach = 1 card.shadow-sm; header punya avatar-initial + nama coach.
  test('V-01 card per coach + header', async ({ page }) => {
    const cards = page.locator('.card.shadow-sm');
    const cardCount = await cards.count();
    test.skip(cardCount === 0, 'no coach group data — accordion card belum ada (RED pra-Plan 02)');

    expect(cardCount).toBeGreaterThan(0);
    const firstHeader = cards.first().locator('.card-header');
    // avatar inisial dekoratif
    await expect(firstHeader.locator('.avatar-initial')).toBeVisible();
    // nama coach (text non-kosong) ada di header
    const headerText = ((await firstHeader.textContent()) ?? '').trim();
    expect(headerText.length).toBeGreaterThan(0);
  });

  // V-02 (DSN-01): badge beban warna ikut threshold <5 bg-info / >=5 bg-warning / >=8 bg-danger.
  test('V-02 badge threshold', async ({ page }) => {
    const headers = page.locator('.card.shadow-sm .card-header');
    const n = await headers.count();
    test.skip(n === 0, 'no coach group data — badge threshold belum bisa di-assert');

    for (let i = 0; i < n; i++) {
      const badge = headers.nth(i).locator('.badge').first();
      if ((await badge.count()) === 0) continue;
      // ambil angka beban + className via evaluate (korelasi angka<->kelas warna)
      const cls = (await badge.getAttribute('class')) ?? '';
      const countText = ((await badge.textContent()) ?? '').replace(/\D/g, '');
      if (countText === '') continue;
      const c = parseInt(countText, 10);
      if (c >= 8) {
        expect(cls).toContain('bg-danger');
      } else if (c >= 5) {
        expect(cls).toContain('bg-warning');
      } else {
        expect(cls).toContain('bg-info');
      }
    }
  });

  // V-03 (DSN-02): default ALL CLOSED — .collapse.show count == 0; tiap header aria-expanded="false".
  test('V-03 default closed', async ({ page }) => {
    const cards = page.locator('.card.shadow-sm');
    const cardCount = await cards.count();
    test.skip(cardCount === 0, 'no coach group data — accordion belum ada');

    await expect(page.locator('.collapse.show')).toHaveCount(0);
    const headers = page.locator('.card.shadow-sm .card-header');
    const n = await headers.count();
    for (let i = 0; i < n; i++) {
      expect(await headers.nth(i).getAttribute('aria-expanded')).toBe('false');
    }
  });

  // V-04 (DSN-02): klik header → #collapse-0 visible + aria-expanded=true; klik lagi → tertutup.
  test('V-04 collapse buka tutup', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — tak ada header untuk di-toggle');

    const body0 = page.locator('#collapse-0');
    await header0.click();
    await expect(body0).toBeVisible();
    expect(await header0.getAttribute('aria-expanded')).toBe('true');
    // Tunggu transisi Bootstrap collapse SELESAI (class show, bukan collapsing) sebelum klik ke-2.
    // Klik saat masih .collapsing akan dibalik Bootstrap → flaky. Phase 354 lesson: assert state stabil.
    await expect(body0).toHaveClass(/(^|\s)show(\s|$)/);

    await header0.click();
    await expect(body0).not.toBeVisible();
    expect(await header0.getAttribute('aria-expanded')).toBe('false');
  });

  // V-05 (DSN-02): INDEPENDENT multi-open — buka card 0 + card 1 → keduanya .show bersamaan (no data-bs-parent).
  test('V-05 independent multi-open', async ({ page }) => {
    const headers = page.locator('.card.shadow-sm .card-header');
    const cardCount = await headers.count();
    test.skip(cardCount < 2, 'need >=2 coach groups untuk bukti independent multi-open');

    await headers.nth(0).click();
    await headers.nth(1).click();
    await expect(page.locator('#collapse-0')).toBeVisible();
    await expect(page.locator('#collapse-1')).toBeVisible();
  });

  // V-06 (DSN-02): mini-tabel thead th == 9; TIDAK ada th "Coachee Aktif" (D-07).
  test('V-06 9 kolom', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — mini-tabel belum ada');

    await header0.click();
    await expect(page.locator('#collapse-0')).toBeVisible();
    const ths = page.locator('#collapse-0 thead th');
    await expect(ths).toHaveCount(9);
    await expect(page.locator('#collapse-0 thead th', { hasText: 'Coachee Aktif' })).toHaveCount(0);
  });

  // V-07 (DSN-02 / Phase 354): a11y header toggle — role=button atau <button>; aria-controls == id body;
  // focus header → Enter buka → Space toggle (aria-expanded berubah).
  test('V-07 a11y header toggle', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — header belum ada');

    // role=button ATAU tagName BUTTON
    const tagName = await header0.evaluate(el => el.tagName);
    const role = await header0.getAttribute('role');
    expect(tagName === 'BUTTON' || role === 'button').toBeTruthy();

    // aria-controls menunjuk id body collapse
    const controls = await header0.getAttribute('aria-controls');
    expect(controls).toBeTruthy();
    await expect(page.locator('#' + controls)).toHaveCount(1);

    // keyboard: Enter buka, Space toggle
    const ctrlBody = page.locator('#' + controls);
    await header0.focus();
    await page.keyboard.press('Enter');
    await expect(ctrlBody).toBeVisible();
    expect(await header0.getAttribute('aria-expanded')).toBe('true');
    // Tunggu transisi collapse selesai (.show stabil) sebelum toggle ke-2 — hindari race .collapsing.
    await expect(ctrlBody).toHaveClass(/(^|\s)show(\s|$)/);

    await header0.focus();
    await page.keyboard.press('Space');
    await expect(ctrlBody).not.toBeVisible();
    expect(await header0.getAttribute('aria-expanded')).toBe('false');
  });

  // V-08 (DSN-03): toolbar seragam — "Tambah Mapping" btn-primary solo; 3 tombol Excel di .btn-group; semua btn-sm.
  test('V-08 toolbar seragam', async ({ page }) => {
    const tambah = page.locator('button:has-text("Tambah Mapping")');
    await expect(tambah).toHaveClass(/btn-primary/);

    const group = page.locator('.btn-group');
    await expect(group).toHaveCount(1);
    // 3 aksi Excel dalam btn-group (Download Template / Import Excel / Export Excel)
    const excelBtns = group.locator('a, button');
    await expect(excelBtns).toHaveCount(3);
    // semua tombol toolbar Excel ber-btn-sm
    const m = await excelBtns.count();
    for (let i = 0; i < m; i++) {
      await expect(excelBtns.nth(i)).toHaveClass(/btn-sm/);
    }
  });

  // V-09 (DSN-03): dead onclick hilang — Tambah Mapping onclick null/kosong; klik → #assignModal visible.
  test('V-09 tambah mapping buka modal', async ({ page }) => {
    const tambah = page.locator('button:has-text("Tambah Mapping")');
    const onclick = await tambah.getAttribute('onclick');
    expect(onclick === null || onclick.trim() === '').toBeTruthy();

    await tambah.click();
    await expect(page.locator('#assignModal')).toBeVisible();
  });

  // V-10 (DSN-06* smoke — full parity Phase 390): Edit → #editModal visible + #editCoacheeName ter-set
  // (bukti openEditModal 7-arg jalan).
  test('V-10 edit modal', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — tak ada card untuk dibuka');
    await header0.click();
    await expect(page.locator('#collapse-0')).toBeVisible();

    const editBtn = page.locator('#collapse-0 button:has-text("Edit")').first();
    test.skip((await editBtn.count()) === 0, 'no coachee row — Edit dikunci data; full parity Phase 390');
    await editBtn.click();
    await expect(page.locator('#editModal')).toBeVisible();
    // #editCoacheeName = <p class="form-control-plaintext"> (bukan input); openEditModal set .textContent.
    // Bukti openEditModal 7-arg jalan: nama coachee ter-isi (text non-kosong).
    await expect(page.locator('#editCoacheeName')).not.toHaveText('');
    // 390-01 promote: bukti openEditModal mengisi SEMUA field (coach select + tanggal mulai),
    // bukan cuma nama → parity-strength (bukan sekadar modal-visible).
    await expect(page.locator('#editCoachSelect')).not.toHaveValue('');
    await expect(page.locator('#editStartDate')).toHaveValue(/\d{4}-\d{2}-\d{2}/);
  });

  // V-11 (DSN-06* smoke — full parity Phase 390): Hapus → #deleteModal terbuka + tombol submit "Hapus".
  // Row-removal tr[data-mapping-id] penuh = Phase 390.
  test('V-11 delete hapus row', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — tak ada card untuk dibuka');
    await header0.click();
    await expect(page.locator('#collapse-0')).toBeVisible();

    const hapusBtn = page.locator('#collapse-0 button:has-text("Hapus")').first();
    test.skip((await hapusBtn.count()) === 0, 'no deletable row — mutasi penuh = Phase 390');
    // 390-01 promote: daftarkan route SEBELUM klik supaya fetch preview (confirmDelete) tertangkap.
    let previewHit = false;
    await page.route('**/Admin/CoachCoacheeMappingDeletePreview*', r => { previewHit = true; r.continue(); });
    await hapusBtn.click();
    await expect(page.locator('#deleteModal')).toBeVisible();
    // tombol submit "Hapus" dalam modal ada
    await expect(page.locator('#deleteModal button:has-text("Hapus")')).toHaveCount(1);
    // preview preload jalan (appUrl fetch) + nama coachee ter-render (bukan placeholder "Memuat...").
    await expect.poll(() => previewHit, { timeout: 10_000 }).toBe(true);
    const delName = ((await page.locator('#deleteCoacheeName').textContent()) ?? '').trim();
    expect(delName.length).toBeGreaterThan(0);
    expect(delName).not.toBe('Memuat...');
    // NON-DESTRUCTIVE: JANGAN klik submit "Hapus" — delete nyata = Plan 02 UAT live.
  });

  // V-12 (DSN-06* smoke — full parity Phase 390): aksi branch — baris IsCompleted (badge Graduated) TIDAK
  // menampilkan tombol "Aktifkan" (cek IsCompleted DULU, Phase 356 D-06). Setiap baris punya tombol "Edit".
  test('V-12 aksi branch', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — tak ada card untuk dibuka');
    await header0.click();
    await expect(page.locator('#collapse-0')).toBeVisible();

    const rows = page.locator('#collapse-0 tr[data-mapping-id]');
    const rowCount = await rows.count();
    test.skip(rowCount === 0, 'no coachee row — aksi branch dikunci data; full parity Phase 390');

    // struktural: setiap baris punya tombol Edit
    for (let i = 0; i < rowCount; i++) {
      await expect(rows.nth(i).locator('button:has-text("Edit")')).toHaveCount(1);
    }

    // baris Graduated (IsCompleted) tak boleh punya tombol "Aktifkan" (D-06 dicek sebelum IsActive)
    const graduatedRows = page.locator('#collapse-0 tr[data-mapping-id]', { has: page.locator('.badge:has-text("Graduated")') });
    const gCount = await graduatedRows.count();
    test.skip(gCount === 0, 'no graduated row — branch IsCompleted dikunci data; full parity Phase 390');
    for (let i = 0; i < gCount; i++) {
      await expect(graduatedRows.nth(i).locator('button:has-text("Aktifkan")')).toHaveCount(0);
    }
  });

  // V-13 (DSN-06* smoke — full parity Phase 390): AJAX via appUrl — route intercept
  // **/Admin/CoachCoacheeMappingDeletePreview* terkena saat confirmDelete (bukti appUrl sub-path, tak 404).
  test('V-13 ajax appUrl subpath', async ({ page }) => {
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — tak ada card untuk dibuka');
    await header0.click();
    await expect(page.locator('#collapse-0')).toBeVisible();

    const hapusBtn = page.locator('#collapse-0 button:has-text("Hapus")').first();
    test.skip((await hapusBtn.count()) === 0, 'no deletable row — confirmDelete dikunci data; full parity Phase 390');

    let hit = false;
    let hitPath = '';
    await page.route('**/Admin/CoachCoacheeMappingDeletePreview*', route => {
      hit = true;
      hitPath = route.request().url();
      route.continue();
    });

    await hapusBtn.click();
    await expect(page.locator('#deleteModal')).toBeVisible();
    // beri waktu fetch appUrl jalan
    await expect.poll(() => hit, { timeout: 10_000 }).toBe(true);
    expect(hitPath).toContain('/Admin/CoachCoacheeMappingDeletePreview');
  });

  // V-14 (DSN-06* smoke — full parity Phase 390): filter Seksi + submit "Cari" → URL section= (resetPageAndSubmit).
  test('V-14 filter pagination', async ({ page }) => {
    const select = page.locator('select[name="section"]');
    const optionValues = await select.locator('option').evaluateAll(
      (opts) => opts.map((o) => (o as HTMLOptionElement).value).filter((v) => v !== ''),
    );
    test.skip(optionValues.length === 0, 'no section options to filter — full parity Phase 390');

    await select.selectOption(optionValues[0]);
    await Promise.all([
      page.waitForURL(/section=/, { timeout: 15_000 }),
      page.getByRole('button', { name: 'Cari' }).click(),
    ]);
    expect(page.url()).toContain('section=');
  });

  // V-15 (390-01, DSN-06 export parity): "Export Excel" → download event → nama file kontrak.
  // Tak butuh data — export jalan walau kosong. Bukti tombol export tak rusak pasca-accordion.
  test('V-15 export excel download', async ({ page }) => {
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('link', { name: 'Export Excel' }).click(),
    ]);
    expect(download.suggestedFilename()).toBe('CoachCoacheeMapping.xlsx');
  });

  // V-16 (390-01): "Download Template" → download event → nama file template import.
  test('V-16 download template', async ({ page }) => {
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('link', { name: 'Download Template' }).click(),
    ]);
    expect(download.suggestedFilename()).toBe('coach_coachee_import_template.xlsx');
  });

  // V-17 (390-01, console-error gate): 0 error console/pageerror saat buka collapse + modal Edit.
  // Bukti JS-contract (openEditModal/appUrl/bootstrap) selamat dari rewrite accordion. Data-guard.
  test('V-17 zero console error on interactions', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('pageerror', e => errors.push(e.message));
    const header0 = page.locator('.card.shadow-sm .card-header').first();
    test.skip((await header0.count()) === 0, 'no coach group data — no card to interact');
    await header0.click();
    await expect(page.locator('#collapse-0')).toBeVisible();
    const editBtn = page.locator('#collapse-0 button:has-text("Edit")').first();
    if (await editBtn.count() > 0) {
      await editBtn.click();
      await expect(page.locator('#editModal')).toBeVisible();
      await page.locator('#editModal [data-bs-dismiss="modal"]').first().click();
    }
    expect(errors, errors.join('\n')).toHaveLength(0);
  });

});
