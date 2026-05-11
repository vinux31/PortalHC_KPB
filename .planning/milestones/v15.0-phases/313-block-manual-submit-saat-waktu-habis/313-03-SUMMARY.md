---
phase: 313-block-manual-submit-saat-waktu-habis
plan: 03
subsystem: cmp-exam-frontend
tags: [razor, frontend, timer, ux, retry-handler, modal]
type: execute
wave: 1
depends_on: [01]
requires:
  - "Plan 02 backend EnsureCanSubmitExamAsync helper (Tier-1 manual reject + Tier-2 grace preserved)"
  - "Hidden field isAutoSubmit di ExamSummary.cshtml line 110 (preserved)"
provides:
  - "ExamSummary.cshtml: 3-branch button conditional + @section Scripts retry handler"
  - "StartExam.cshtml: modal info-only + fire submit paralel langsung saat countdown=0"
affects:
  - "Views/CMP/ExamSummary.cshtml (refine submit button + add retry JS)"
  - "Views/CMP/StartExam.cshtml (modal markup + timer flow)"
tech-stack:
  added: []
  patterns:
    - "Razor 3-branch conditional render"
    - "JS retry chain dengan exponential backoff [1s, 2s, 4s]"
    - "Bootstrap 5 modal info-only + spinner-border indicator"
key-files:
  created: []
  modified:
    - "Views/CMP/ExamSummary.cshtml"
    - "Views/CMP/StartExam.cshtml"
decisions:
  - "Implementasi D-13 banner alert-info (Claude discretion) di atas form ExamSummary saat timerExpired untuk awareness reload-after-timeup"
  - "Selector retry handler pakai form[action$=\"/SubmitExam\"] (suffix-match, robust thd base path prefix WR-02)"
  - "Cleanup komentar: hapus reference 'timeUpOkBtn' di komentar agar grep count benar 0 (tidak hanya code-level cleanup)"
metrics:
  duration_minutes: 8
  completed_date: "2026-05-08"
  task_count: 2
  file_count: 2
---

# Phase 313 Plan 03: Frontend Submit Button + Modal Update Summary

Modifikasi 2 file Razor view (ExamSummary.cshtml + StartExam.cshtml) untuk close UX gap TMR-01 — UI mencegah klik manual submit setelah timeup, JS handle network resilience untuk auto-submit, modal info-only fire submit paralel langsung tanpa delay 10 detik.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Refine ExamSummary.cshtml: 3-branch button + retry @section Scripts | `36648e5d` | Views/CMP/ExamSummary.cshtml |
| 2 | Modify StartExam.cshtml: modal info-only + fire submit paralel | `cbe08099` | Views/CMP/StartExam.cshtml |

## Task 1 Outcomes

**File:** `Views/CMP/ExamSummary.cshtml` (146 → 224 lines, +79 / -1)

**Changes:**

1. **D-13 banner info** (NEW) di atas form, render saat `timerExpired == true`:
   ```razor
   <div class="alert alert-info" role="alert">
       <i class="bi bi-info-circle me-2"></i>
       Waktu ujian sudah habis. Submit otomatis sudah/sedang berjalan.
   </div>
   ```

2. **3-branch button conditional** (D-03 / C-02) replace 2-branch existing:
   - Branch 1 (NEW — `timerExpired == true`): button disabled `btn-secondary` + `id="manualSubmitDisabledBtn"` + spinner `spinner-border-sm` + label `Waktu Habis - Submit Otomatis Berjalan...` + tooltip
   - Branch 2 (preserved — `unanswered > 0`): existing disabled "Jawab semua soal terlebih dahulu"
   - Branch 3 (preserved — happy path): submit + `confirm()` Bahasa Indonesia

3. **`@section Scripts` block (NEW)** dengan retry handler D-10:
   - `delays = [1000, 2000, 4000]` exponential backoff
   - `fetch(form.action, { method: 'POST', body: fd, redirect: 'follow' })` — pakai `form.action` DOM property (WR-02 mitigation, no hardcoded URL)
   - `form[action$="/SubmitExam"]` selector suffix-match (path-prefix safe)
   - `console.error('[Phase 313] Submit attempt N failed', err)` log marker
   - `showRetryFailBanner()` (D-11) banner permanent: `Submit gagal karena masalah jaringan. Hubungi admin. Jawaban Anda tersimpan di tab ini, jangan tutup browser.`
   - Auto-fire on page-ready kalau `timerExpired` (chain dari StartExam timer-up flow)
   - Guard: kalau `!timerExpired` early-return — happy path pakai native form submit + confirm

**Hidden field `isAutoSubmit`** (line 110) — PRESERVED unchanged untuk Plan 02 backend dependency.

## Task 2 Outcomes

**File:** `Views/CMP/StartExam.cshtml` (1518 → 1509 lines, +12 / -21)

**Changes:**

1. **Modal `timeUpWarningModal`** (line 376-391 area) — info-only per C-03:
   - Body text update: `Jawaban Anda sedang dikirim otomatis. Mohon tunggu, jangan refresh halaman.` (replace existing copy)
   - Hapus button `id="timeUpOkBtn"` "OK — Kirim Jawaban"
   - Tambah div spinner indicator: `<span class="spinner-border text-danger">` + label `Mengirim...` (text-danger fw-semibold)
   - `data-bs-backdrop="static"` + `data-bs-keyboard="false"` PRESERVED
   - Comment header: `Phase 313 C-03: modal info-only ...`

2. **JS timer flow** (line 462-475 area) — fire submit paralel:
   - HAPUS `setTimeout(function() { ... }, 10000)` wrapper
   - `submitted = true` set SEBELUM `timeupModal.show()` (prevent visibilitychange race re-trigger)
   - `examForm.submit()` fire immediate setelah modal show (modal tetap visible during POST → ExamSummary chain)
   - `clearInterval(timerInterval)` + `clearInterval(saveInterval)` + `window.onbeforeunload = null` PRESERVED
   - Comment: `Phase 313 C-03: modal muncul (info awareness) + submit fire PARALEL langsung (no setTimeout 10s delay).`

3. **Cleanup orphan listener** line ~1263:
   - Hapus `document.getElementById('timeUpOkBtn').addEventListener('click', ...)` (8 lines deleted)
   - Replace dengan komentar Phase 313 C-03 explanatory (tanpa menyebut nama element — agar grep `timeUpOkBtn` count = 0)

**Preserved untouched:**
- `examForm` declaration line 71 (`asp-action="ExamSummary"` routing)
- `submitted` flag declaration (line 441)
- `visibilitychange` listener line 487-498 (Pitfall 7 — wall-clock anchor)
- `timerStartWallClock` + `timerStartRemaining` Phase D-07 drift fix

## Verification — Aggregate

### `dotnet build` Output

```
Build succeeded.
    92 Warning(s)
    0 Error(s)
```

**Errors:** 0 (Razor compile gate PASS)
**Warnings:** 92 (Phase 312 baseline preserved — semua dari `LdapAuthService.cs` CA1416 platform warnings + 1 `RecordsTeam.cshtml` MVC1000 — TIDAK terkait Phase 313 changes)

### Acceptance Criteria — Task 1 (grep verification)

| Acceptance | Pattern | Count | Status |
|-----------|---------|-------|--------|
| `manualSubmitDisabledBtn` present | `id="manualSubmitDisabledBtn"` | 1 | PASS |
| Disabled button label exact text | `Waktu Habis - Submit Otomatis Berjalan` | 1 | PASS |
| Spinner indicator | `spinner-border spinner-border-sm me-2` | 1 | PASS |
| 3-branch first branch | `@if (timerExpired)` | 3 (banner + button + JS Json.Serialize) | PASS |
| 3-branch second branch | `else if (unanswered > 0)` | 1 | PASS |
| `@section Scripts` block | `@section Scripts {` | 1 | PASS |
| D-10 retry pattern | `fetch(form.action,` | 1 | PASS |
| Exponential backoff array | `delays = [1000, 2000, 4000]` | 1 | PASS |
| D-11 banner exact text | `Submit gagal karena masalah jaringan. Hubungi admin. Jawaban Anda tersimpan di tab ini, jangan tutup browser.` | 1 | PASS |
| `[Phase 313]` log marker | `[Phase 313]` | 1 | PASS |
| WR-02 mitigation (no hardcode URL) | `'/CMP/SubmitExam'` literal | 0 | PASS |
| Hidden field preserved | `name="isAutoSubmit" value="@(timerExpired ? "true" : "false")"` | 1 | PASS |

### Acceptance Criteria — Task 2 (grep verification)

| Acceptance | Pattern | Count | Status |
|-----------|---------|-------|--------|
| Phase 313 C-03 anchor | `Phase 313 C-03` | 3 | PASS |
| Modal body new copy | `Jawaban Anda sedang dikirim otomatis. Mohon tunggu, jangan refresh halaman.` | 1 | PASS |
| Spinner label | `Mengirim...` | 1 | PASS |
| Spinner indicator | `<span class="spinner-border text-danger"` | 1 | PASS |
| Button id removed | `id="timeUpOkBtn"` (count 0) | 0 | PASS |
| Button label removed | `OK — Kirim Jawaban` | 0 | PASS |
| 10s setTimeout removed | `}, 10000)` | 0 | PASS |
| Old modal copy removed | `Waktu pengerjaan ujian telah habis. Jawaban Anda akan dikirimkan secara otomatis.` | 0 | PASS |
| Static backdrop preserved | `data-bs-backdrop="static"` | 5 (5 modals di file, all preserved) | PASS |
| Keyboard false preserved | `data-bs-keyboard="false"` | 5 | PASS |
| `submitted = true` before modal show | line 469 (verified visual) | — | PASS |
| `examForm.submit()` paralel | line 473 (verified visual) | — | PASS |
| `clearInterval(timerInterval)` preserved | count | 4 | PASS |
| `clearInterval(saveInterval)` preserved | count | 4 | PASS |

## Compliance Status

| Requirement | Detail | Status |
|-------------|--------|--------|
| **D-03** (button disabled greyed-out + spinner + label) | ExamSummary.cshtml branch `timerExpired` | PASS |
| **D-10** (retry 3x backoff [1s, 2s, 4s]) | ExamSummary.cshtml @section Scripts retry handler | PASS |
| **D-11** (banner permanent setelah retry exhausted) | `showRetryFailBanner()` D-11 verbatim copy | PASS |
| **D-13** (banner info reload-after-timeup) | ExamSummary.cshtml alert-info di atas form | PASS (Claude discretion) |
| **C-02** (Submit utama di ExamSummary, BUKAN StartExam) | Honored — modifikasi terpusat di ExamSummary | PASS |
| **C-03** (modal tetap muncul + submit paralel langsung, no setTimeout 10s) | StartExam.cshtml modal markup + JS timer flow | PASS |
| **WR-02** (path-prefix mitigation, no hardcode URL) | `form.action` DOM property + `form[action$="/SubmitExam"]` selector | PASS |
| **Pitfall 2** (modify wrong file) | Submit final di ExamSummary, NOT StartExam — confirmed | PASS |
| **Pitfall 7** (visibilitychange listener wall-clock anchor) | Listener line 487-498 preserved unchanged | PASS |
| **Pitfall 3** (TempData rendering) | `_Layout.cshtml:199-208` auto-render TempData["Error"] (verified earlier in PATTERNS) | PASS |

## Threat Model Compliance

| Threat ID | Mitigation Status |
|-----------|-------------------|
| T-313-09 (DevTools `removeAttribute('disabled')` bypass) | Defense-in-depth: backend Plan 02 EnsureCanSubmitExamAsync Tier-1 reject — UI disable adalah deterrent, server authoritative. Honored. |
| T-313-10 (network 5xx storm infinite retry) | Hard cap `maxAttempts = 3` + total ~7s recovery window. Honored. |
| T-313-11 (console.error log leak) | Generic `[Phase 313] Submit attempt N failed` — no PII, no SessionId, no UserId. Honored. |
| T-313-12 (hardcoded URL path-prefix bug) | `form.action` DOM property — no literal `/CMP/SubmitExam`. Honored. |
| T-313-13 (refresh during retry) | D-11 banner instruct user "Jawaban Anda tersimpan di tab ini, jangan tutup browser." Honored. |
| T-313-14 (XSS via TempData) | Razor auto-escape via `_Layout.cshtml`. Honored (no new TempData usage di Plan 03). |
| T-313-15 (modal show + form submit race) | `submitted = true` set SEBELUM `timeupModal.show()` + `examForm.submit()`. Honored. |

## Deviations from Plan

**None — plan dieksekusi sesuai spec.**

Catatan kecil:
- Komentar inline post-cleanup orphan listener awalnya menyebut `timeUpOkBtn` (literal nama element) — adjusted ke "tombol OK pada modal time-up" agar `grep -c "timeUpOkBtn"` benar return 0 sesuai acceptance criteria. Tidak ada code-level deviation; hanya komentar refactor.

## Notes untuk UAT Operator

Setelah backend Plan 02 (EnsureCanSubmitExamAsync helper + audit blocked entry) merged + DB seed Plan 01 dijalankan, UAT operator dapat eksekusi:

1. Jalankan SQL seed: `.planning/seeds/313-timer-fixtures.sql` di SSMS / sqlcmd terhadap DB lokal (snapshot DB dulu per CLAUDE.md SEED_WORKFLOW)
2. Login coachee (`rino.prasetyo@pertamina.com / 123456` per `tests/helpers/accounts.ts:4`)
3. Jalankan 7-step manual UAT dari `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-UAT.md`
4. Spot-check AuditLog SQL: `SELECT TOP 5 ActionType, Description, TargetId, CreatedAt FROM AuditLogs WHERE ActionType = 'SubmitExamBlocked' ORDER BY CreatedAt DESC`
5. Restore DB snapshot setelah UAT (per SEED_WORKFLOW temporary classification)

**Browser smoke test (verifikasi C-03 user override):**
- Mulai ujian dengan duration pendek (3-5 menit) — biarkan timer expire alami
- Verify: Modal "Waktu Habis!" muncul dengan spinner "Mengirim..." (no OK button)
- Verify: Form submit fire langsung paralel (no 10s delay) → redirect ke ExamSummary
- Verify: ExamSummary render banner alert-info "Waktu ujian sudah habis. Submit otomatis sudah/sedang berjalan."
- Verify: Submit button disabled `manualSubmitDisabledBtn` greyed-out + spinner + label "Waktu Habis - Submit Otomatis Berjalan..."
- Verify: JS retry handler auto-fire — DevTools Network tab harus tunjukkan POST `/CMP/SubmitExam`
- Verify (network failure simulation via DevTools throttle Offline): retry log `[Phase 313] Submit attempt 1 failed` di console + retry attempt 2/3 dengan backoff + banner alert-warning "Submit gagal karena masalah jaringan..."

## Notes untuk Plan 01 Closure

Setelah Plan 02 (backend) + Plan 03 (frontend) merged + DB seed Plan 01 dijalankan, FLOW 313 Playwright tests transisi RED → GREEN. Tester finalisasi assertion bodies di `tests/e2e/exam-taking.spec.ts` FLOW 313 describe block sesuai UAT result.

## Self-Check: PASSED

**Files verified existing:**
- `Views/CMP/ExamSummary.cshtml` — FOUND (modified)
- `Views/CMP/StartExam.cshtml` — FOUND (modified)

**Commits verified existing:**
- `36648e5d` (Task 1) — FOUND in `git log`
- `cbe08099` (Task 2) — FOUND in `git log`

**Build verified:** `dotnet build` exit 0, 0 errors, 92 warnings (baseline preserved)

**Acceptance criteria verified:** Semua grep patterns Task 1 + Task 2 PASS sesuai tabel di atas.
