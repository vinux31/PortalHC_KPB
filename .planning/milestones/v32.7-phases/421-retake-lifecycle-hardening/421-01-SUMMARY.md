---
phase: 421-retake-lifecycle-hardening
plan: 01
subsystem: assessment-retake
tags: [retake, lifecycle, window-gate, certificate, hardening]
requires: [RetakeRules, RetakeService, CMPController]
provides: [window-gate-eligibility, abort-before-destroy, cert-null-on-reset, window-aware-results-ux]
affects: [Helpers/RetakeRules.cs, Services/RetakeService.cs, Controllers/CMPController.cs, Views/CMP/Results.cshtml]
tech-stack:
  added: []
  patterns: [pure-rules-kill-drift, plus-7h-WIB-window-convention, defense-in-depth-gate]
key-files:
  created: []
  modified:
    - Helpers/RetakeRules.cs
    - Services/RetakeService.cs
    - Controllers/CMPController.cs
    - Views/CMP/Results.cshtml
    - HcPortal.Tests/RetakeRulesTests.cs
    - HcPortal.Tests/RetakeServiceTests.cs
key-decisions:
  - "Window gate dua-lapis: eligibility (RetakeRules.CanRetake +examWindowCloseDate) + eksekusi (RetakeService.ExecuteAsync abort-before-destroy). Defense-in-depth D-01."
  - "Abort window berlaku juga jalur HC ResetAssessment (delegasi penuh ke ExecuteAsync) — cegah dead shell, bukan hanya worker."
  - "+7h WIB byte-identik StartExam (CMPController:956) di semua titik — nol drift TZ."
  - "windowClosed → attemptsRemaining=false (tier full review) + IsInCooldown=false (no countdown) — Pitfall 1 & 2."
requirements-completed: [RTH-01, RTH-02]
duration: 1 sesi
completed: 2026-06-23
---

# Phase 421 Plan 01: Window Gate + Cert-Null Summary

Window-gate dua-lapis anti dead-end (RTK-LOGIC-02 HIGH) + pencabutan NomorSertifikat saat reset (RTK-LOGIC-01), dengan Results window-aware — keystone fase retake hardening.

**Durasi:** 1 sesi · **Task:** 3 · **File:** 6 (4 produksi + 2 test).

## Yang dibangun

- **Task 1 (RTH-01 eligibility):** `RetakeRules.CanRetake` +param `examWindowCloseDate` (no default, paksa caller suplai fakta). Gate `nowUtc.AddHours(7) > examWindowCloseDate.Value` SEBELUM cooldown; EWCD null = no gate (backward-compat). 4 boundary test (open/closed/null/+7h boundary) → **RetakeRulesTests 26/26 hijau**.
- **Task 2 (RTH-01 eksekusi + RTH-02):** `RetakeService.ExecuteAsync` abort-before-destroy (return `RetakeResult(false, "Masa ujian sudah ditutup…")` SEBELUM `BeginTransactionAsync`/`RemoveRange` → sesi live utuh). Claim `ExecuteUpdateAsync` +`SetProperty(NomorSertifikat, null)`. `CanRetakeAsync` suplai `s.ExamWindowCloseDate`. 3 test integration (abort-before-destroy/cert-null/window-open) → **RetakeServiceTests 10/10 hijau**. Jalur HC `ResetAssessment` tercakup via delegasi (no hand-roll di controller).
- **Task 3 (RTH-01 UX):** `CMPController` Results hitung `windowClosed` → `attemptsRemaining=false` (ResolveReviewMode buka full review, Pitfall 2) + `IsInCooldown=false` (countdown tak muncul, Pitfall 1) + `ViewBag.WindowClosed`. `Results.cshtml` cabang `alert-secondary` "Masa ujian sudah ditutup…" (reuse alert Bootstrap, bukan tombol disabled). Tombol Ujian Ulang auto-hide via `CanRetake=false`.

## Verifikasi

- `dotnet test ~RetakeRulesTests` → **26/26** (exit 0).
- `dotnet test ~RetakeServiceTests` → **10/10** (exit 0, SQLEXPRESS live).
- `dotnet build` → **0 error**.
- Grep guard: `nowUtc.AddHours(7)` (RetakeRules), `DateTime.UtcNow.AddHours(7)` (RetakeService/CMP); tak ada `DateTime.Now`/`+8`/`TimeZoneInfo` BARU. Abort muncul SEBELUM tx/RemoveRange (verified visual).

## Deviations from Plan

**[Sekuensing — compile-coupling]** Task 1 (signature `CanRetake` +param tanpa default) memutus build di satu-satunya caller `RetakeService.CanRetakeAsync:255` sampai Task 2-A me-wire-nya. Karena C# compile seluruh-proyek, Task 1 + Task 2-code dikerjakan berurutan SEBELUM run test pertama; tetap di-commit atomik terpisah per-task (T1 `26ba67b6`, T2 `8d082676`, T3 `7daa0d6b`). Tidak ada perubahan scope — hanya urutan eksekusi.

**Total deviations:** 1 (sekuensing, bukan scope). **Impact:** nihil — semua acceptance + test hijau.

## Self-Check: PASSED

- key-files modified semua ada di disk + ter-commit (3 commit feat 421-01).
- Acceptance criteria 3 task semua PASS (grep + test exit 0).
- Verification commands re-run hijau (26 + 10 unit/integration, build 0-err).

## Issues Encountered

None.

## Next

Ready for **421-02** (RTH-03 counting helper `CountEraRetakeArchives`). Phase 421 retake hardening lanjut.
