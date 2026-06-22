---
phase: 413-test-uat
reviewed: 2026-06-21T12:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - HcPortal.Tests/FlexibleParticipantLifecycleTests.cs
  - tests/e2e/flexible-participant-412.spec.ts
  - Views/Admin/AssessmentMonitoringDetail.cshtml
  - docs/SEED_JOURNAL.md
findings:
  critical: 0
  warning: 0
  info: 2
  total: 2
status: clean
---

# Phase 413: Code Review Report

**Reviewed:** 2026-06-21
**Depth:** standard
**Files Reviewed:** 4
**Status:** clean (0 Critical, 0 Warning, 2 Info)

## Summary

Phase 413 mencakup tiga deliverable: (1) xUnit lifecycle lintas-fase (`FlexibleParticipantLifecycleTests.cs`), (2) Playwright e2e multi-context 7 sinyal live (`flexible-participant-412.spec.ts`), dan (3) satu production fix di `Views/Admin/AssessmentMonitoringDetail.cshtml` (hoist `monFlashRow`). Seed journal dikonfirmasi `cleaned`.

**Production fix (`monFlashRow` hoist):** Benar dan lengkap. `window.monFlashRow = flashRow` dipindah dari blok script atas (IIFE di baris 1037–1571) ke blok `@section Scripts` (baris 1741–2146) tepat setelah definisi `flashRow` di baris 1788. Semua empat fungsi yang *masih* diekspor di blok atas (`buildActionsHtml`, `statusBadgeClass`, `statusDisplayLabel`, `isPackageMode`) terdefinisi di scope yang sama — tidak ada cross-block hazard tersisa. Fungsi `monInjectParticipantRow` dan `monClearAddedFallback` di blok `@section Scripts` dikonsumsi dari blok atas hanya melalui defensive `typeof ... === 'function'` guard, sehingga urutan parse aman. Fix tidak mengubah logika sama sekali — hanya memindah ekspos.

**xUnit tests:** De-tautologis sesuai aturan 999.12. L1/L2/L3 masing-masing drive endpoint produksi asli, assert kolom DB nyata via reload ctx baru, dan memanggil `CMPController.IsParticipantRemoved` (produksi, `public static`) sebagai guard re-entry lintas-fase. L2 membuktikan Pitfall 1 (peserta lain tidak ikut ter-remove) dengan benar: `otherPreRepId` adalah rep session milik `otherUser` tanpa `LinkedSessionId` ke `newPreId`, sehingga `RemoveParticipantCoreAsync` (yang resolve partner via `LinkedSessionId`, bukan `LinkedGroupId`) tidak menyentuhnya. L3 pre-assert (UPA ADA sebelum remove) menghindari tautologi pada AnyAsync false.

**Playwright e2e:** 7 sinyal semua tercakup. Multi-context (2 browser.newContext) untuk force-kick dan multi-observer. `waitHubConnected` menggunakan `waitForFunction` (deterministik). Satu-satunya `waitForTimeout(2_000)` di baris 200 diperuntukkan worker join grup batch post-StartExam — tidak ada sinyal deterministik yang tersedia untuk momen ini karena `examRemoved` dikirim via `_hubContext.Clients.User()` (user-targeted, bukan group), dan justifikasi tercatat sebagai komentar. Panel collapse diatasi dengan `state:'attached'` + expand sebelum Restore. Modal-wait menggunakan `waitFor({state:'visible'})` nyata. DB assertions (Layer-1, Layer-4) hadir.

**Seed/restore:** BACKUP sebelum seed, RESTORE di afterAll dengan error-isolation (restoreError dilempar setelah Layer-4 query). `snapshotPath = ''` saat `beforeAll` gagal sebelum backup → `afterAll` return early tanpa RESTORE, yang benar (tidak ada backup yang perlu di-restore). SEED_JOURNAL entry 413 status `cleaned`.

---

## Info

### IN-01: `workers` tidak dikunci di `playwright.config.ts`

**File:** `tests/playwright.config.ts:1`
**Issue:** Config menetapkan `fullyParallel: false` tetapi tidak menetapkan `workers: 1`. Tanpa `--workers=1` saat memanggil `npx playwright test`, Playwright menggunakan default (setengah jumlah CPU core), sehingga spec ini bisa berjalan paralel dengan spec lain yang berbagi DB. Spec baru ini mencatat `--workers=1` hanya di komentar baris 19, konsisten dengan konvensi proyek (semua spec lain sama). Bila suatu saat seseorang menjalankan `npx playwright test` tanpa flag, ada risiko konflik DB antar spec.
**Fix:** Tambah `workers: 1` ke `playwright.config.ts` agar flag tidak perlu disertakan manual setiap kali. Ini bukan perubahan breaking — semua spec sudah menggunakan `--workers=1` secara konvensi. Alternatif: biarkan seperti sekarang jika `--workers=1` sudah dianggap wajib di SOP lokal (dan tidak ada CI yang memanggil tanpa flag ini).

---

### IN-02: `waitForTimeout(2_000)` tanpa komentar signal alternatif

**File:** `tests/e2e/flexible-participant-412.spec.ts:200`
**Issue:** Buffer `waitForTimeout(2_000)` setelah `waitHubConnected(pageWorker)` diperlukan agar worker sempat bergabung ke grup batch (`JoinBatch` di hub) sebelum admin men-trigger force-kick. Ini sudah didokumentasikan sebagai "satu-satunya buffer post-StartExam". Namun tidak ada penjelasan mengapa `waitForFunction` (cek DOM signal dari StartExam selesai) tidak bisa digunakan sebagai alternatif — misalnya menunggu elemen timer muncul (`#examTimer`) atau tombol Submit enabled.
**Fix:** Info saja — buffer 2s terbukti cukup dan sudah diverifikasi di real browser (5/5 green). Bila di masa depan ada flakiness pada mesin lambat, pertimbangkan mengganti dengan `waitForSelector('#examTimer', { state: 'visible' })` atau signal DOM serupa dari StartExam render selesai, agar buffer tidak bergantung pada timing absolut.

---

_Reviewed: 2026-06-21_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
