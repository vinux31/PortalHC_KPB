// Phase 420 (FORM-07/08/09/10/11) — UAT e2e render per-mode form Create Pre-Post (milestone v32.7).
// Membuktikan redesign layout mode Pre-Post + backward-compat mode Standard (Pitfall #2).
//
// Skenario (serial, render-only — toggle DOM via #creationMode change):
//   - FORM-08: pilih Pre-Post → dua sub-kartu ("Setelan Post-Test" + "Setelan Bersama Pre & Post") toBeVisible;
//              kembali Standard → sub-kartu hidden (regresi DOM tunggal).
//   - FORM-07: Pre-Post → SamePackage di header (#samePackageHeaderWrapper, di LUAR kartu Post) toBeVisible;
//              Standard → header SamePackage hidden.
//   - FORM-11: Pre-Post → blok Ujian Ulang (#retakeBlockCreate) hidden + input retake toBeDisabled;
//              Standard → blok retake visible + enabled.
//   - FORM-09: Pre-Post → #standard-jadwal-section inputs + #schedHidden + #ewcdHidden toBeDisabled (tak ter-POST);
//              Standard → enabled.
//   - FORM-10: smoke binding — pilih Pre-Post → sub-kartu muncul (membuktikan name="CreationMode" ter-bind &
//              JS getElementById('creationMode') jalan setelah rename atomik).
//   - Regresi Standard (Pitfall #2): mode Standard render → layout tunggal (Group B/C/D visible), retake visible,
//              std input enabled, sub-kartu absent — DOM/perilaku identik dgn sebelum redesign.
//
// Template: tests/e2e/shuffle.spec.ts (mode:'serial' + db.backup/restore SEED_WORKFLOW + login admin).
// Selektor NYATA (Views/Admin/CreateAssessment.cshtml setelah Plan 420-03):
//   #creationMode (select, FORM-10) / #samePackageHeaderWrapper / #ppt-jadwal-section /
//   #prePostSettingsCards / #slotPostTest / #slotShared / #retakeBlockCreate / #standard-jadwal-section /
//   #schedHidden / #ewcdHidden / #groupBCard / #groupCCard / #groupDCard.
//
// SEED_WORKFLOW (CLAUDE.md): render-only TIDAK menulis DB, namun snapshot/restore tetap dipasang untuk
//   konsistensi idiom + jaga-jaga (login admin tak memutasi data). beforeAll BACKUP → afterAll RESTORE.
// PRECONDITION run: app @ E2E_BASE_URL (branch ITHandoff: http://localhost:5270). WAJIB --workers=1.
// Auth: admin@pertamina.com / 123456 (dev lokal — JANGAN staging/prod).
//
// CATATAN: executor TIDAK menjalankan live run — ini gate UAT orchestrator (checkpoint Task 4).

import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';

let snapshotPath: string;

test.describe.configure({ mode: 'serial' });

async function gotoCreate(page: Page): Promise<void> {
  await login(page, 'admin');
  await page.goto('/Admin/CreateAssessment');
  await page.waitForLoadState('networkidle');
  await expect(page.locator('#creationMode')).toBeVisible();
}

async function setMode(page: Page, value: 'Standard' | 'PrePostTest'): Promise<void> {
  await page.selectOption('#creationMode', value);
}

const postTestCard = (page: Page) => page.locator('.card', { hasText: 'Setelan Post-Test' });
const sharedCard = (page: Page) => page.locator('.card', { hasText: 'Setelan Bersama Pre' });

test.describe('Phase 420 FORM-07..11 — render per-mode + regresi Standard', () => {
  test.beforeAll(async () => {
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre420ux-${ts}.bak`;
    await db.backup(snapshotPath);
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs');
      try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    if (restoreError) throw restoreError;
  });

  // ── FORM-08 + FORM-10 (smoke binding): dua sub-kartu muncul Pre-Post, hilang Standard ──
  test('FORM-08/10: dua sub-kartu Pre-Post muncul (binding CreationMode utuh) + hilang Standard', async ({ page }) => {
    await gotoCreate(page);

    // Default Standard → sub-kartu absent (regresi).
    await expect(page.locator('#prePostSettingsCards')).toHaveClass(/d-none/);

    // Pilih Pre-Post → wrapper sub-kartu tampil + dua heading visible (FORM-10: JS creationMode jalan).
    await setMode(page, 'PrePostTest');
    await expect(page.locator('#prePostSettingsCards')).not.toHaveClass(/d-none/);
    await expect(postTestCard(page)).toBeVisible();
    await expect(sharedCard(page)).toBeVisible();

    // Kembali Standard → wrapper sub-kartu hidden lagi (regresi DOM tunggal).
    await setMode(page, 'Standard');
    await expect(page.locator('#prePostSettingsCards')).toHaveClass(/d-none/);
  });

  // ── FORM-07: SamePackage di header (di luar kartu Post) saat Pre-Post; hidden saat Standard ──
  test('FORM-07: SamePackage di header section Pre-Post (bukan kartu Post)', async ({ page }) => {
    await gotoCreate(page);

    // Standard → header SamePackage hidden.
    await expect(page.locator('#samePackageHeaderWrapper')).toHaveClass(/d-none/);

    // Pre-Post → SamePackage checkbox tampil di header + berada DI LUAR #ppt-jadwal-section (kartu Pre/Post).
    await setMode(page, 'PrePostTest');
    await expect(page.locator('#samePackageHeaderWrapper')).not.toHaveClass(/d-none/);
    await expect(page.locator('#samePackageHeaderWrapper input[name="SamePackage"]')).toBeVisible();
    // Header berada di luar kartu Pre/Post (tidak ada SamePackage di dalam #ppt-jadwal-section).
    await expect(page.locator('#ppt-jadwal-section input[name="SamePackage"]')).toHaveCount(0);
  });

  // ── FORM-11: retake hidden + disabled saat Pre-Post; visible + enabled saat Standard ──
  test('FORM-11: blok Ujian Ulang hidden + disabled saat Pre-Post', async ({ page }) => {
    await gotoCreate(page);

    // Standard → retake visible + enabled.
    await expect(page.locator('#retakeBlockCreate')).not.toHaveClass(/d-none/);
    await expect(page.locator('#AllowRetake')).toBeEnabled();

    // Pre-Post → retake hidden + input retake disabled (tak ter-POST).
    await setMode(page, 'PrePostTest');
    await expect(page.locator('#retakeBlockCreate')).toHaveClass(/d-none/);
    await expect(page.locator('#AllowRetake')).toBeDisabled();
    await expect(page.locator('#MaxAttempts')).toBeDisabled();
    await expect(page.locator('#RetakeCooldownHours')).toBeDisabled();

    // Kembali Standard → retake enabled lagi (aksi-balik Pitfall #2).
    await setMode(page, 'Standard');
    await expect(page.locator('#retakeBlockCreate')).not.toHaveClass(/d-none/);
    await expect(page.locator('#AllowRetake')).toBeEnabled();
  });

  // ── FORM-09: std jadwal/EWCD inputs + hidden combiner disabled saat Pre-Post (tak ter-POST) ──
  test('FORM-09: input standard jadwal + #schedHidden/#ewcdHidden disabled saat Pre-Post', async ({ page }) => {
    await gotoCreate(page);

    // Standard → std jadwal + hidden combiner enabled.
    await expect(page.locator('#schedDateInput')).toBeEnabled();
    await expect(page.locator('#schedHidden')).toBeEnabled();
    await expect(page.locator('#ewcdHidden')).toBeEnabled();

    // Pre-Post → SEMUA input std jadwal + hidden combiner disabled (Pitfall #4/#5).
    await setMode(page, 'PrePostTest');
    await expect(page.locator('#schedDateInput')).toBeDisabled();
    await expect(page.locator('#schedTimeInput')).toBeDisabled();
    await expect(page.locator('#schedHidden')).toBeDisabled();
    await expect(page.locator('#ewcdHidden')).toBeDisabled();

    // Kembali Standard → enabled lagi (aksi-balik).
    await setMode(page, 'Standard');
    await expect(page.locator('#schedDateInput')).toBeEnabled();
    await expect(page.locator('#schedHidden')).toBeEnabled();
    await expect(page.locator('#ewcdHidden')).toBeEnabled();
  });

  // ── Regresi Standard (Pitfall #2): layout tunggal utuh, sub-kartu absent, DOM identik ──
  test('Regresi Standard: layout Group B/C/D tunggal utuh + sub-kartu absent', async ({ page }) => {
    await gotoCreate(page);

    // Default Standard render: Group B/C/D visible, sub-kartu hidden, std input + retake enabled.
    await expect(page.locator('#groupBCard')).not.toHaveClass(/d-none/);
    await expect(page.locator('#groupCCard')).not.toHaveClass(/d-none/);
    await expect(page.locator('#groupDCard')).not.toHaveClass(/d-none/);
    await expect(page.locator('#prePostSettingsCards')).toHaveClass(/d-none/);
    await expect(page.locator('#samePackageHeaderWrapper')).toHaveClass(/d-none/);
    await expect(page.locator('#PassPercentage')).toBeEnabled();
    await expect(page.locator('#ShuffleQuestions')).toBeEnabled();
    await expect(page.locator('#AllowAnswerReview')).toBeEnabled();

    // Round-trip Pre-Post → Standard tetap mengembalikan DOM tunggal (field kembali ke Group B/C/D).
    await setMode(page, 'PrePostTest');
    await setMode(page, 'Standard');
    await expect(page.locator('#groupBCard')).not.toHaveClass(/d-none/);
    await expect(page.locator('#prePostSettingsCards')).toHaveClass(/d-none/);
    // Field PassPercentage/Shuffle kembali enabled (tidak tertinggal disabled/di sub-kartu).
    await expect(page.locator('#PassPercentage')).toBeEnabled();
    await expect(page.locator('#ShuffleQuestions')).toBeEnabled();
  });
});
