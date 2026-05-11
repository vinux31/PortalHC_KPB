---
phase: 308-prepost-wizard-validation-fix
reviewed: 2026-04-29T14:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Views/Admin/CreateAssessment.cshtml
  - Controllers/AssessmentAdminController.cs
  - tests/e2e/assessment.spec.ts
  - tests/e2e/helpers/wizardSelectors.ts
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 308: Code Review Report

**Reviewed:** 2026-04-29T14:00:00Z
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found (3 Info)

## Summary

Phase 308 (REQ WIZ-04, PrePost Wizard Validation Fix) berhasil mengimplementasikan perbaikan validasi `Status` field saat mode PrePost dengan pendekatan minimal-diff yang sangat aman. Total perubahan produksi hanya 6 baris JS (Views/Admin/CreateAssessment.cshtml line 1872-1893) + 5 baris controller (Controllers/AssessmentAdminController.cs line 781-787).

**Verifikasi area kritis:**

1. **JS handler edit (D-01/D-02 di line 1872-1893)** — robust:
   - `statusEl` lookup di-scope di dalam handler `change` event (line 1875), bukan di outer scope. Jika element di-render dinamis, lookup akan re-evaluate setiap fire. Null-safe via `if (statusEl)` guard di kedua branch (line 1885 dan 1892). Tidak ada listener leak: handler tetap satu (`addEventListener('change', ...)`), edit hanya menambah body statement.
   - Tidak ada race condition dengan switch type cepat — setiap fire `change` event men-trigger ulang sinkron, dan `value =` adalah operasi atomik DOM. Debounce dari Phase 307 helpers (`scheduleRenderSelectedPanel` di line 1543) tidak intersect dengan handler ini karena beda target element.
   - Comment di line 1884 menyebut "line 1078/1112/1170" — referensi line di-shift +6 oleh edit ini sendiri (real path PrePost create di line 1084/1118/1176 setelah merge). Lihat IN-01.

2. **Controller edit (D-04 di line 781-787)** — proper placement:
   - `ModelState.Remove("Status")` di-insert SETELAH determinasi `isPrePostMode` (line 779) dan SEBELUM blok validasi `Schedule` (line 787-799), `DurationMinutes` (line 802-818), `ExamWindowCloseDate` (line 826-838), serta validasi PrePost-spesifik `PreSchedule/PostSchedule/EWCD` (line 886-917). Mirror pattern persis dengan `ModelState.Remove("UserId")` line 742 dan `ModelState.Remove("AccessToken")` line 756.
   - **No bypass risk untuk Standard mode**: removal hanya terjadi di-dalam `if (isPrePostMode)` guard. Standard mode tetap kena `[Required]` validation dari `Status` field. Plus defense kedua di line 981-984 fallback `if (string.IsNullOrEmpty(model.Status)) model.Status = "Open"` — meskipun user bypass JS validateStep client (line 1029-1032), server tetap menulis "Open" sebagai default ke DB.

3. **Defense-in-depth (T-308-01 mitigasi)** — 5 PrePost session creation paths UNCHANGED dan tetap hardcode `Status = "Upcoming"`:
   - Line 1084 (Pre session create dalam transaksi)
   - Line 1118 (Post session create dalam transaksi)
   - Line 1176 (TempData payload `CreatedAssessment`)
   - Line 1650 (Edit/add Pre, foreach newUserId)
   - Line 1669 (Edit/add Post, foreach newUserId)

   Grep verifikasi total 9 occurrences `Status = "Upcoming"` di controller (line 122, 144, 1084, 1118, 1176, 1650, 1669, 2375, 2446) — line 122/144 dan 2375/2446 adalah read-side group status aggregation (bukan write path), tidak terkait edit phase 308.

4. **Phase 307 helpers area (line 1469-1614)** — UNTOUCHED. Verified via Read:
   - `renderSelectedParticipants` (line 1469-1539) intact
   - `scheduleRenderSelectedPanel` debounce (line 1542-1549) intact
   - `updateSelectedCount` hoisted helper (line 1553-1576) intact
   - Proton AJAX IIFE (line 1579+) intact

5. **Test scaffold hygiene** — 4 test cases (8.1-8.4) dengan selectors dari `wizardSelectors.ts` line 22-26. Form ID koreksi sudah benar `#createAssessmentForm` (BUKAN `#createForm` yang ada di CONTEXT.md draft awal). Catatan flaky timing dan risiko kebocoran state ada di IN-02 dan IN-03 (low priority — wave 0 scaffold by design).

**No bugs, no security issues, no regressions found.** 3 finding info-level berikut adalah catatan dokumentasi/hygiene dan tidak menghalangi merge.

## Info

### IN-01: Comment line reference di-skewed by own edit

**File:** `Views/Admin/CreateAssessment.cshtml:1884`
**Issue:** Comment "Phase 308 D-01: auto-set Status='Upcoming' (matches server hardcode at line 1078/1112/1170)" — angka line yang dikutip (1078/1112/1170) merujuk ke posisi referensi dari plan 308-02-PLAN.md (state pre-edit). Setelah edit phase 308 sendiri men-shift area line +6 (5 path Status="Upcoming" sekarang berada di line 1084/1118/1176/1650/1669), comment menjadi sedikit out-of-date. Bukan bug — code execution tidak terpengaruh, hanya catatan dokumentasi.
**Fix:** (Opsional, low priority) update comment ke:
```js
// Phase 308 D-01: auto-set Status='Upcoming' (matches server hardcode at line 1084/1118/1176/1650/1669)
if (statusEl) statusEl.value = 'Upcoming';
```
Atau cukup hilangkan angka line spesifik untuk mengurangi maintenance burden ke depan:
```js
// Phase 308 D-01: auto-set Status='Upcoming' (matches server hardcoded PrePost session creation paths)
```

### IN-02: Test 8.1 belum melakukan assertion submit success — partial coverage

**File:** `tests/e2e/assessment.spec.ts:180-204`
**Issue:** Test 8.1 ("Standard saja submit sukses") menyebut "regression guard success criteria #5" tetapi hanya mem-verifikasi `Status` field interactable + value persistence (line 200), tidak melakukan submit dan check success modal/redirect. Comment di line 202-203 secara eksplisit menyatakan ini "test SCAFFOLD wave 0 — full wizard navigation di-defer ke Wave 1". Sesuai plan, tetapi nama test "submit sukses" agak misleading.
**Fix:** Saat Wave 1 follow-up, perluas test 8.1 untuk advance Step 2 → Step 3 → Step 4 → submit, kemudian verifikasi `#successModal` visible (mirror pattern test 1.2 line 60-65). Atau rename test 8.1 sekarang ke "Standard mode Status field interactable" untuk reflect actual coverage scope. Tidak blocking untuk merge wave 0.

### IN-03: Test 8.x state sharing — Status filled di test 8.2 mungkin bocor ke 8.3

**File:** `tests/e2e/assessment.spec.ts:206-247`
**Issue:** Test suite di-konfigurasi `serial` di line 17 dan tiap test melakukan `page.goto('/Admin/CreateAssessment')` di awal — Playwright fresh navigation seharusnya reset state form (browser tidak menyimpan unsaved form input cross-navigation by default). Verified: tidak ada actual bug di sini karena setiap test `await page.goto(...)` di line 208/229/251 dan goto akan re-render form server-side (Status field empty kembali). Tetapi best practice E2E adalah explicit reset assertion atau `beforeEach` yang clear state. Wave 0 scaffold by design lean, tetapi nanti saat Wave 1 expand ke flow lengkap, pertimbangkan factor out helper `resetWizardForm(page)` agar lebih DRY dan eksplisit.
**Fix:** (Opsional, low priority) tambahkan `beforeEach` di describe FLOW 8:
```ts
test.describe('Assessment - Phase 308 PrePost Wizard Validation', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
  });
  // ...remove duplicated login+goto calls dari setiap test
});
```
Mengurangi 8 baris duplicated code dan eksplisit menyatakan "fresh page per test". Tidak blocking untuk merge wave 0.

---

_Reviewed: 2026-04-29T14:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
