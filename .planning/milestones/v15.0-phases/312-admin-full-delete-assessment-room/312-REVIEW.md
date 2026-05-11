---
phase: 312-admin-full-delete-assessment-room
reviewed: 2026-05-07T00:00:00Z
depth: standard
files_reviewed: 3
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
  - tests/e2e/assessment.spec.ts
findings:
  critical: 0
  warning: 5
  info: 7
  total: 12
status: issues_found
---

# Phase 312: Code Review Report

**Reviewed:** 2026-05-07
**Depth:** standard
**Files Reviewed:** 3
**Status:** issues_found

## Summary

Phase 312 mengimplementasi role-tier guard untuk 3 delete actions (`DeleteAssessment`, `DeleteAssessmentGroup`, `DeletePrePostGroup`), endpoint AJAX `GetDeleteImpact`, modal 2-step impact preview di Razor partial, dan FLOW 12 Playwright tests. Implementasi solid secara umum:

- **Security baseline OK**: Ke-3 delete actions sudah memiliki `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` (pre-existing, line 2021/2139/2258). Modal form pakai `@Html.AntiForgeryToken()`. Razor output di-encode otomatis (`@group.Title`, `data-delete-title="@group.Title"`). JS DOM update pakai `textContent` (bukan `innerHTML`) → no XSS.
- **Defense-in-depth OK**: UI hide via Status (line 251-254), backend re-validate via `EnsureCanDeleteAsync` (cek Status + ResponseCount). Sesuai design D-04 + Q1 opsi B.
- **AuditLog konsisten**: Success entries diperluas dengan Status + ResponseCount, blocked entries pakai naming convention `{Action}Blocked`. Try/catch wrapper benar — audit failure tidak block flow utama.
- **Cascade integrity preserved**: Snapshot Status + ResponseCount captured sebelum cascade (mencegah field hilang post-`SaveChangesAsync`).

Temuan utama: **race-condition TOCTOU** antara guard check dan cascade delete (WR-01), **hardcoded path traversal** via `appUrl` JS helper (WR-02), test stability issues karena selector terlalu loose (WR-03), dan beberapa info-level optimization opportunities.

## Warnings

### WR-01: TOCTOU race condition antara `EnsureCanDeleteAsync` dan cascade delete

**File:** `Controllers/AssessmentAdminController.cs:5474-5538` (helper) + 3 caller sites (line 2049, 2171, 2281)
**Issue:** `EnsureCanDeleteAsync` melakukan `CountAsync(r => sessionIds.Contains(...))` untuk validate `responseCount==0`. Setelah return `null` (pass), method caller lanjut cascade — TANPA transaction wrapper. Antara guard-check dan `RemoveRange()`, peserta concurrent bisa POST jawaban baru via `/CMP/Assessment/...` → response baru tersimpan. Saat cascade jalan, `Restrict` FK akan fail dan throw → outer try/catch line 2125+ tangkap → `TempData["Error"]` = "Gagal menghapus..." → user lihat error generik tanpa tahu sebabnya. Worst case: partial cascade (cert/attempt sudah dihapus, response gagal dihapus) jika tidak strict transactional.

**Fix:** Bungkus guard-check + cascade dalam `IDbContextTransaction`:
```csharp
using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
var blockResult = await EnsureCanDeleteAsync(...);
if (blockResult != null) { await tx.RollbackAsync(); return blockResult; }
// ... cascade ...
await _context.SaveChangesAsync();
await tx.CommitAsync();
```
Atau minimal: tambah explicit re-check `responseCount` di-dalam transaction sebelum `SaveChangesAsync`, dan kalau berubah → `TempData["Error"]` spesifik ("Peserta baru menyimpan jawaban — refresh dan coba lagi.").

---

### WR-02: `appUrl()` di Razor JS bergantung pada `<base href>` yang tidak diset di partial

**File:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:436-439, 493, 497, 501, 508`
**Issue:** Helper `appUrl(path)` membaca `document.querySelector('base')?.getAttribute('href') || '/'`. Kalau layout induk (`_Layout.cshtml`) tidak punya `<base href="/KPB-PortalHC/">`, fallback ke `/` → URL menjadi `/Admin/DeleteAssessment` instead of `/KPB-PortalHC/Admin/DeleteAssessment`. Ini adalah path-prefix bug yang muncul saat deploy Dev (`http://10.55.3.3/KPB-PortalHC`) — sesuai CLAUDE.md environment map. Form action akan POST ke wrong endpoint → 404 di Dev tapi works di lokal (`http://localhost:5277` tanpa prefix).

**Fix:** Render URL via `Url.Action` di Razor (server-side) → simpan di `data-*` attribute, lalu JS read:
```html
<button type="button" ... 
        data-delete-action-single="@Url.Action("DeleteAssessment", "AssessmentAdmin")"
        data-delete-action-group="@Url.Action("DeleteAssessmentGroup", "AssessmentAdmin")"
        data-delete-action-prepost="@Url.Action("DeletePrePostGroup", "AssessmentAdmin")"
        data-impact-url="@Url.Action("GetDeleteImpact", "AssessmentAdmin")"
        ...>
```
JS:
```js
form.action = trigger.dataset.deleteActionSingle;  // already prefix-aware
var url = trigger.dataset.impactUrl + '?type=' + ...;
```
Atau verifikasi `<base href>` ada di `_Layout.cshtml` dan terisi dengan `~/` resolved (`Url.Content("~/")`).

---

### WR-03: Playwright selectors `tr:has-text("Open")` / `tr:has-text("Completed")` rentan false-match

**File:** `tests/e2e/assessment.spec.ts:625, 662, 677, 699`
**Issue:** Selector `page.locator('tr:has-text("Open"))` cocok dengan baris manapun yang mengandung substring "Open" di kolom apapun — bisa Title (e.g., "OpenAI Assessment"), Category, atau bahkan tooltip. Risiko false-positive yang lolos test-skip guard. Sama untuk "Completed".

**Fix:** Scope ke badge spesifik:
```ts
const openRow = page.locator('tr', { has: page.locator('span.badge.bg-success', { hasText: /^Open$/ }) }).first();
const completedRow = page.locator('tr', { has: page.locator('span.badge.bg-secondary', { hasText: /^Completed$/ }) }).first();
```
Atau tambah `data-testid="status-badge"` di `_AssessmentGroupsTab.cshtml:178` dan filter via testid + exact text.

---

### WR-04: Test 12.1 hapus seed assessment Open pertama → potensi kerusakan dependency test

**File:** `tests/e2e/assessment.spec.ts:620-656` (test 12.1)
**Issue:** Test 12.1 melakukan DELETE actual terhadap row Open pertama yang ditemukan. Karena `test.describe.configure({ mode: 'serial' })` line 17, test berikutnya (12.2-12.6) bisa terpengaruh kalau seed terbatas. Test 3.2 (line 461) juga sebelumnya bisa hapus seed yang sama. Tidak ada cleanup atau create-fixture per test. Untuk lingkungan UAT/CI, ini akan flake.

**Fix:** Buat fixture seed khusus per test via API/factory di `beforeEach`:
```ts
test.beforeAll(async ({ request }) => {
  // Create dedicated Open assessment via /Admin/CreateAssessment POST
  // Store ID untuk test 12.1, hapus di afterAll
});
```
Atau pakai title fixture bertanda timestamp + `searchAssessment(page, fixtureTitle)` untuk scope ke row spesifik (mirror pattern test 12.5/12.6 line 716, 737).

---

### WR-05: `EnsureCanDeleteAsync` tidak validate `sessions` non-empty → silent pass kalau caller lupa load

**File:** `Controllers/AssessmentAdminController.cs:5481-5495`
**Issue:** Kalau `sessions` parameter empty list (e.g., caller bug atau race delete-by-other-admin), `sessionIds=[]` → `CountAsync(r => sessionIds.Contains(r.AssessmentSessionId))` returns 0, `sessions.Any(s => s.Status=="Completed")` returns false → guard returns `null` (pass) → caller lanjut cascade no-op. Bukan exploit tapi defensive guard absent. Saat ini ke-3 caller sudah validate non-empty BEFORE memanggil helper (line 2032 null-check, line 2161 implicit, line 2273 `!groupSessions.Any()` check), jadi tidak hit production. Tapi helper tidak self-defend.

**Fix:** Tambah guard di awal helper:
```csharp
if (sessions == null || sessions.Count == 0)
{
    // Caller bug — fail loud
    var lg = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
    lg.LogWarning("EnsureCanDeleteAsync called with empty sessions for {Action} TargetId={Id}", actionPrefix, targetId);
    return null; // atau return RedirectToAction dengan error — tergantung policy
}
```

## Info

### IN-01: Duplikasi query `responseCount` di guard helper + caller

**File:** `Controllers/AssessmentAdminController.cs:5485-5486` + `2057-2058, 2179-2180, 2293-2294`
**Issue:** `EnsureCanDeleteAsync` query `CountAsync(r => sessionIds.Contains(...))` (line 5485). Setelah pass, caller `DeleteAssessment` query LAGI yang sama untuk `preDeleteResponseCount` (line 2057). Total 2 round-trip identik ke DB.

**Fix:** Refactor helper return `(IActionResult? block, int responseCount, bool anyCompleted)` tuple — caller reuse value untuk audit:
```csharp
private async Task<(IActionResult? block, int responseCount, bool anyCompleted)> EnsureCanDeleteAsync(...)
```
Atau pakai out-param. Skip kalau performance non-issue (delete operation low-frequency).

---

### IN-02: `GetDeleteImpact` issue tidak rate-limited — HC bisa enumerate aggregated counts

**File:** `Controllers/AssessmentAdminController.cs:3481-3585`
**Issue:** Endpoint `GET /Admin/GetDeleteImpact?type=...&id=...` hanya gated oleh `[Authorize(Roles="Admin, HC")]`. HC bisa loop semua ID untuk discover responseCount/certCount aggregate. Risk = info-disclosure ringan. Sudah documented di T-312-04 ("HC accept disclosure of aggregated counts") tapi tidak ada rate limit / audit trail.

**Fix:** Optional — tambah `_logger.LogInformation` ringan untuk track query pattern, atau implementasi rate limit middleware untuk endpoint admin (project-wide concern). Skip kalau diterima sebagai design.

---

### IN-03: URL encoding hilang di pagination link Razor (pre-existing, bukan Phase 312)

**File:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:305, 311, 322, 328, 334`
**Issue:** `"&search=" + searchTerm` tanpa `Uri.EscapeDataString` / `@Url.Action`. Kalau search punya `&`, `=`, atau spasi → URL malformed dan filter rusak. Pre-existing bug bukan introduced di Phase 312.

**Fix:** Pakai `@Url.Action("ManageAssessment", new { tab="assessment", page=i, search=searchTerm })` agar framework yang encode.

---

### IN-04: Hardcoded copy "Anda tidak memiliki izin..." duplicated antara helper dan test assertion

**File:** `Controllers/AssessmentAdminController.cs:5526` + `tests/e2e/assessment.spec.ts:730`
**Issue:** Test 12.5 assert `.alert-danger` contain `/tidak memiliki izin/i`. Kalau copy diubah di helper → test silent break. Tidak critical tapi konvensi bisa pakai resource string atau constant.

**Fix:** Ekstrak ke constant `ErrorMessages.HC_DELETE_BLOCKED` di shared location, reference dari controller dan test fixture.

---

### IN-05: `dam-form-id` dan `dam-form-linkedid` `disabled` toggle pattern fragile

**File:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:413-415, 489-505`
**Issue:** Hidden input pakai `disabled` attribute + JS toggle untuk exclude dari form submission. Pattern ini tidak idiomatic — `disabled` pada hidden input tidak berguna untuk styling. Kalau lupa enable salah satu (e.g., copy-paste branch baru), hidden value tidak terkirim → controller terima `id=0` → cascade fail.

**Fix:** Pakai `name` attribute toggle saja:
```js
formId.name = (type === 'prepost') ? '' : 'id';
formLinkedId.name = (type === 'prepost') ? 'linkedGroupId' : '';
```
Atau buat 2 form terpisah dan show/hide via display.

---

### IN-06: Test 12.0 `test.skip(!id || !type, ...)` redundant dengan early-return pattern

**File:** `tests/e2e/assessment.spec.ts:602-607`
**Issue:** Line 602 sudah `if (await button.count() === 0) test.skip(...)` kemudian line 605-606 ambil attribute, line 607 skip lagi kalau null. Logika OK tapi struktur agak verbose. `getAttribute` pada element yang sudah ada returns string (non-null) untuk attribute yang ada di markup — kemungkinan null hanya kalau attribute hilang (markup bug, bukan runtime).

**Fix:** Optional — assert non-null:
```ts
const id = await button.getAttribute('data-delete-id');
const type = await button.getAttribute('data-delete-type');
expect(id, 'data-delete-id should exist').not.toBeNull();
expect(type, 'data-delete-type should exist').not.toBeNull();
```
Skip kalau prefer skip-over-fail.

---

### IN-07: Razor inline script tidak punya `nonce` / CSP-friendly attribute

**File:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:426-575`
**Issue:** `<script>` block 150 baris di partial. Kalau project nanti enable Content-Security-Policy (`script-src 'self'`), inline script akan blocked. Project belum implementasi CSP (project-wide) jadi non-blocking sekarang.

**Fix:** Pindahkan ke `wwwroot/js/admin/delete-assessment-modal.js` lalu reference via `<script src="@Url.Content("~/js/admin/delete-assessment-modal.js")"></script>`. Bonus: mempermudah unit-test JS logic.

---

_Reviewed: 2026-05-07_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
