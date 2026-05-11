---
phase: 312-admin-full-delete-assessment-room
fixed_at: 2026-05-07T00:00:00Z
review_path: .planning/phases/312-admin-full-delete-assessment-room/312-REVIEW.md
iteration: 1
findings_in_scope: 5
fixed: 5
skipped: 0
status: all_fixed
---

# Phase 312: Code Review Fix Report

**Fixed at:** 2026-05-07
**Source review:** `.planning/phases/312-admin-full-delete-assessment-room/312-REVIEW.md`
**Iteration:** 1

**Summary:**
- Findings in scope: 5 (Critical: 0, Warning: 5)
- Fixed: 5
- Skipped: 0
- Info findings (7): out of scope, not addressed (per fix_scope=critical_warning)

**Build status:** PASS (92 warnings = baseline, 0 errors) — verified after each fix.

## Fixed Issues

### WR-05: `EnsureCanDeleteAsync` tidak validate `sessions` non-empty

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `551f8e90`
**Applied fix:** Tambah early-return guard di awal `EnsureCanDeleteAsync` (line 5480-5490). Kalau `sessions` null atau empty, helper sekarang log warning + set `TempData["Error"]` generik + redirect ke `ManageAssessment`. Sebelumnya silent-pass (return null) → caller cascade no-op. Mencegah race delete-by-other-admin atau caller bug yang lupa load sessions.

### WR-01: TOCTOU race condition antara `EnsureCanDeleteAsync` dan cascade delete

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `cac00b44`
**Applied fix:** Bungkus 3 delete methods (`DeleteAssessment`, `DeleteAssessmentGroup`, `DeletePrePostGroup`) dalam `IDbContextTransaction` via `_context.Database.BeginTransactionAsync()`. Tambah re-check `responseCount > 0` setelah guard pass — kalau berubah dari 0 antara guard dan cascade (HC tier only), rollback + error spesifik ("Peserta baru menyimpan jawaban..."). Admin override tetap lanjut (D-04 menerima konsekuensi). Setiap delete method punya `tx.RollbackAsync()` di error path dan `tx.CommitAsync()` setelah `SaveChangesAsync()` sukses.

### WR-02: `appUrl()` di Razor JS bergantung pada `<base href>` yang tidak diset

**Files modified:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml`
**Commit:** `70dd99d5`
**Applied fix:** Replace JS `appUrl()` helper dengan URL pre-rendered server-side via `@Url.Action`. Modal element sekarang punya 4 data-attributes: `data-action-single`, `data-action-group`, `data-action-prepost`, `data-impact-url`. JS membaca via `modalEl.dataset.actionSingle` dll. Path-prefix (`/KPB-PortalHC`) di Dev environment ter-resolve correctly oleh Razor framework. Razor comment menggunakan `@@Url.Action` (escape) untuk hindari Razor parser error.

### WR-03: Playwright selectors `tr:has-text("Open")` rentan false-match

**Files modified:** `tests/e2e/assessment.spec.ts`
**Commit:** `dd244701`
**Applied fix:** 4 selectors di FLOW 12 tests (12.1, 12.2, 12.3, 12.4) di-scope ke status badge spesifik. `tr:has-text("Open")` jadi `tr` filtered by `span.badge.bg-success` dengan `hasText: /^Open$/` (exact match regex). Untuk Completed: `span.badge.bg-secondary` dengan `/^Completed$/`. Mencegah false-positive dari substring match di Title (e.g., "OpenAI Assessment"), Category, atau tooltip.

### WR-04: Test 12.1 hapus seed assessment Open pertama → potensi kerusakan dependency test

**Files modified:** `tests/e2e/assessment.spec.ts`
**Commit:** `6a1cd702`
**Applied fix:** Refactor test 12.1 ke fixture title pattern (mirror 12.5/12.6). Sekarang search via `searchAssessment(page, 'Phase 312 Admin Delete Fixture')` dan filter row by exact title + Open badge. Kalau fixture tidak ada → `test.skip` dengan instruksi "Wave 1 manual seed required" — TIDAK fallback ke "first Open row" yang bisa hapus seed share dengan test lain di mode serial. Mencegah flake di UAT/CI dengan seed terbatas.

## Verification

Build verification dilakukan setelah setiap fix:

| Fix | Build Result | Warnings | Errors |
|-----|-------------|----------|--------|
| Baseline | PASS | 92 | 0 |
| WR-05 | PASS | 92 | 0 |
| WR-01 | PASS | 92 | 0 |
| WR-02 | FAIL → FIX (Razor `@@` escape) → PASS | 92 | 0 |
| WR-03 | (no compile, TS test file) | - | - |
| WR-04 | (no compile, TS test file) | - | - |
| Final | PASS | 92 | 0 |

WR-02 sempat error CS1503 karena `@Url.Action` di Razor HTML comment di-parse — fixed dengan escape `@@Url.Action`. Build final tetap di baseline 92 warnings.

**Process management:** `HcPortal.exe` (PID 15228) di-kill sebelum first build untuk hindari file lock pada output stage.

## Action Items untuk Wave 1 Manual Seed (post-fix)

WR-04 introduce dependency: test 12.1 sekarang membutuhkan seed assessment dengan title `Phase 312 Admin Delete Fixture` (Status=Open, ResponseCount=0). Sebelum run Playwright FLOW 12:

1. Login sebagai Admin atau HC di lokal/UAT
2. Buat assessment baru via `/Admin/CreateAssessment` dengan:
   - **Title:** `Phase 312 Admin Delete Fixture`
   - **Category:** OJT (atau apapun)
   - **Status:** Open (default for fresh assessment)
   - **Response count:** 0 (jangan ada peserta yang submit)
3. Test 12.1 akan hapus fixture ini saat run — re-create kalau mau re-run
4. Fixture ini OPSIONAL: kalau tidak ada, test 12.1 akan skip (bukan fail)

---

_Fixed: 2026-05-07_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
