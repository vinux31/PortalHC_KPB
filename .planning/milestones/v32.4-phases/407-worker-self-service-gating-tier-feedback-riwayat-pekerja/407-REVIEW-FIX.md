---
phase: 407
slug: worker-self-service-gating-tier-feedback-riwayat-pekerja
type: code-review-fix
date: 2026-06-22
review_ref: 407-REVIEW.md
critical_fixed: 0
warning_fixed: 1
info_deferred: 3
status: all_warnings_fixed
---

# Phase 407 — Code Review Fix

Sumber: `407-REVIEW.md` (0 Critical / 1 Warning / 3 Info). Keamanan SEMUA lulus (leak-safe, resolver, RetakeExam IDOR/CSRF/token, XSS).

## WR-01 — FIXED (correctness vs RTK-10)

**Masalah:** Tombol cooldown-disabled + countdown tak pernah render. View menggate countdown di dalam `else if (Model.CanRetake)`, padahal `CanRetake` (`CanRetakeAsync`) bernilai **false** selama cooldown (pure `RetakeRules.CanRetake` mengembalikan false sebelum jeda lewat). `IsCapReached` juga false → tak ada blok dirender; cabang countdown + JS jadi dead code. Worker tak lihat affordance/countdown — langgar Success Criterion RTK-10 ("cooldown countdown, tombol disabled bila belum lewat").

**Fix (3 file):**
1. `Models/AssessmentResultsViewModel.cs` — tambah flag `bool IsInCooldown`.
2. `Controllers/CMPController.cs` (Results) — set `IsInCooldown = attemptsRemaining && assessment.IsPassed == false && CooldownUntilUtc.HasValue && CooldownUntilUtc > UtcNow` (layak-ulang-abaikan-cooldown DAN jeda masih aktif). Tidak digate `CanRetake` (yang false selama cooldown).
3. `Views/CMP/Results.cshtml` — restrukturisasi blok kontrol retake: `IsCapReached` (lock) → **`else if CanRetake`** (tombol enabled + modal) → **`else if IsInCooldown`** (tombol disabled + `data-cooldown-until` + `#retakeCountdown`). JS countdown existing (guard lesson 413; at-0 auto-enable + wire modal + relabel) kini reachable.

**Server tetap otoritatif:** countdown UX-only; `CMP/RetakeExam` re-cek `CanRetakeAsync` saat POST (cooldown bypass ditolak server). Fix murni rendering affordance.

**Verifikasi:** `dotnet build` 0 error + unit suite **448/0/2** (no regresi). Live render countdown akan dikonfirmasi di Playwright UAT @5270 (gate berikutnya).

## Info (deferred — non-blocking)
- **IN-01:** e2e skenario 6 bisa lolos tanpa uji countdown (akibat WR-01). Setelah WR-01 fixed, countdown reachable — UAT @5270 mengonfirmasi.
- **IN-02:** `IsCapReached` mengecualikan pending (IsPassed==null). **By-design** — sesi pending belum bisa di-cap (belum gagal). Tak diubah.
- **IN-03:** Query `eraRetakeArchives` counting diduplikasi 3 tempat (controller + 2× RetakeService). Refactor single-source → backlog/Phase 408 (risiko drift rendah; logika identik + ter-cover test). Non-blocking.
