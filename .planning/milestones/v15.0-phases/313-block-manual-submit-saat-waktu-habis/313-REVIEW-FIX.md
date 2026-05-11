---
phase: 313-block-manual-submit-saat-waktu-habis
fixed_at: 2026-05-08T00:00:00Z
review_path: .planning/phases/313-block-manual-submit-saat-waktu-habis/313-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 6
skipped: 0
status: all_fixed
---

# Phase 313: Code Review Fix Report

**Fixed at:** 2026-05-08
**Source review:** .planning/phases/313-block-manual-submit-saat-waktu-habis/313-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (Critical: 2, Warning: 4)
- Fixed: 6
- Skipped: 0
- Build status: 92 warnings (== baseline), 0 errors

**Note:** CR-02 di-commit bersamaan dengan CR-01 di `a6e4d6ca` karena keduanya
memodifikasi file `Views/CMP/ExamSummary.cshtml` dengan dependency saling-terkait
(token rendering CR-01 + retry handler CR-02 di file/section yang sama).
Atomic per-finding tetap traceable via diff hunk + commit-message detail.

---

## Fixed Issues

### CR-01: `isAutoSubmit` spoofing bypasses Tier-1 enforcement (TMR-01 defeated)

**Files modified:** `Controllers/CMPController.cs`, `Views/CMP/ExamSummary.cshtml`
**Commit:** `a6e4d6ca`
**Applied fix:**
- Generate one-shot token (`Guid.NewGuid("N")`) di `ExamSummary` GET saat `timerExpired=true`,
  simpan di `TempData["AutoSubmitToken_{id}"]`, expose ke view via `ViewBag.AutoSubmitToken`.
- Render hidden field `<input name="autoSubmitToken">` di form submit.
- `SubmitExam` action signature tambah parameter `string? autoSubmitToken = null`.
- `EnsureCanSubmitExamAsync` signature tambah `(string? autoSubmitToken, int sessionId)`,
  validate token + consume (`TempData.Remove`) dengan ordinal string compare.
- Tier-1 condition diubah: `elapsedSec >= allowedSec && !serverApprovedAutoSubmit`.
  Client-supplied `isAutoSubmitClientHint` TIDAK lagi bypass Tier-1 — harus dibarengi token sah.

**Verification needed (human):** Confirm DevTools spoof scenario — set hidden field
`isAutoSubmit=true` via DevTools tanpa token sah, verifikasi Tier-1 reject + AuditLog ditulis.

### CR-02: Tier-2 redirect dianggap sukses oleh fetch retry → potensi infinite loop

**Files modified:** `Views/CMP/ExamSummary.cshtml`
**Commit:** `a6e4d6ca` (commit gabungan dengan CR-01)
**Applied fix:**
- `attemptSubmit()` handler: detect URL pattern `/CMP/StartExam(\/|\?|$)` di `r.url` saat
  `r.redirected=true`, treat as "submit ditolak server" → set `stopRetry=true`,
  panggil `showRetryFailBanner()` dengan custom message, STOP retry chain.
- Hanya redirect ke `/CMP/Results` (atau path non-StartExam) yang dianggap sukses.
- `showRetryFailBanner()` sekarang menerima parameter optional `customMessage` untuk
  membedakan "server reject" vs "gagal jaringan".
- Tambah flag `stopRetry` untuk early-return di setTimeout retry.

**Verification needed (human):** Reproduce Tier-2 fire (manual submit setelah grace lewat),
confirm browser tidak loop ke StartExam dan banner permanen muncul.

### WR-01: Race double-submit antara updateTimer() dan EXAM_EXPIRED init block

**Files modified:** `Views/CMP/StartExam.cshtml`
**Commit:** `dd60ce6d`
**Applied fix:**
- Wrap entire EXAM_EXPIRED branch dengan `if (!submitted)` guard.
- Set `submitted = true` SEBELUM `examForm.submit()` di kedua path (button click + setTimeout 5000).
- Reuse flag `submitted` yang declared di line 442 — selaras dengan pattern di `updateTimer()`
  (line 468) dan `visibilitychange` (line 485).

### WR-02: Inkonsistensi satuan perbandingan elapsed (seconds vs minutes float)

**Files modified:** `Controllers/CMPController.cs`
**Commit:** `a6e4d6ca`
**Applied fix:**
- `EnsureCanSubmitExamAsync` standardize ke detik integer dengan operator `>=`:
  `elapsedSec >= allowedSec` (Tier-1), `elapsedSec >= graceLimitSec` (Tier-2).
- Konsisten dengan `ExamSummary` GET line 1542 dan `SubmitExam` awal line 1585.
- `allowedSec = allowedMinutes * 60.0`, `graceLimitSec = allowedSec + 120.0`.
- Boundary [allowed*60, allowed*60+1) detik sekarang konsisten antara Tier-1 dan ExamSummary GET.

### WR-03: `WriteSubmitBlockedAuditAsync` swallow tanpa fallback alert

**Files modified:** `Controllers/CMPController.cs`
**Commit:** `a6e4d6ca`
**Applied fix:**
- Tambah structured field `Event=audit_drop_phase313` di `_logger.LogWarning` payload.
- Memungkinkan dashboard log grep `Event=audit_drop_phase313` untuk monitoring drop-rate
  + alarm kalau audit-write gagal sistematik.

### WR-04: Cast `(int)elapsed.TotalMinutes` truncates → audit log understates elapsed

**Files modified:** `Controllers/CMPController.cs`
**Commit:** `a6e4d6ca`
**Applied fix:**
- `WriteSubmitBlockedAuditAsync` description sekarang log dual unit:
  `ElapsedSec={(int)elapsed.TotalSeconds} AllowedSec={allowedMinutes * 60} ElapsedMin={...} AllowedMin={...}`
- Reviewer audit dapat melihat presisi detik supaya tidak kontradiktif `ElapsedMin == AllowedMin`
  saat sebenarnya elapsed > allowed.
- Backward-compat: ElapsedMin/AllowedMin tetap dipertahankan untuk konsumen lama.

---

## Skipped Issues

_None — semua 6 in-scope findings (2 Critical + 4 Warning) berhasil di-fix._

---

## Info Findings (Out of Scope, fix_scope=critical_warning)

5 Info findings (IN-01 sampai IN-05) tidak masuk scope iteration ini. IN-01 (duplicate
ViewBag read) ter-fix opportunistic saat memodifikasi ExamSummary.cshtml untuk CR-01/CR-02.
IN-02..IN-05 tidak ditangani — bisa dijadwalkan iteration berikutnya kalau prioritas naik.

---

## Verification Summary

Semua fix di-verifikasi dengan:
1. **Tier-1 (re-read):** File post-edit di-baca ulang, fix text confirmed, surrounding code intact.
2. **Tier-2 (build):** `dotnet build --nologo --verbosity minimal` — 92 warnings (== baseline), 0 errors di setiap step.

**Logic-bug warnings:** CR-01 dan CR-02 mengandung perubahan kondisi/logic (token validation,
URL pattern matching) yang Tier-1/Tier-2 tidak bisa fully verify. Manual UAT direkomendasikan
untuk:
- DevTools spoof scenario (CR-01)
- Tier-2 redirect-loop reproduction (CR-02)

---

_Fixed: 2026-05-08_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
