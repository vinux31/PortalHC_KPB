---
phase: 360-bypass-backend-b
plan: 06
subsystem: proton-bypass
tags: [proton, bypass, grading-hook, notification, essay]
requires: [360-04, 360-05]
provides:
  - "4 titik hook §7: exam CL-B(b) lulus → pending Siap + notif HC; re-grade fail → revert"
affects: [360-08]
tech-stack:
  added: []
  patterns:
    - "Hook hot-path = flip+notif saja (no-tx), idempotent via rowsAffected guard"
key-files:
  created: []
  modified:
    - Services/GradingService.cs
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "DI satu arah grading→bypass (Open Q3): GradingService + AssessmentAdminController inject ProtonBypassService; ProtonBypassService TIDAK inject GradingService — build hijau membuktikan no circular"
  - "W-09: titik 2 (:531) & titik 3 (:485) brace-less if dikonversi braced { } SEBELUM append hook — hook dijamin dalam guard Proton (T-360-35)"
requirements-completed: [PBYP-03]
duration: 8 min
completed: 2026-06-10
---

# Phase 360 Plan 06: Hook Notif §7 di 4 Titik Summary

Exam CL-B(b) lulus kini SELALU mem-flip pending→Siap + notif HC dari 4 jalur grading (langsung lulus, re-grade Fail→Pass, essay finalisasi) dan re-grade Pass→Fail me-revert pending ke Menunggu (D-15) — tanpa menyentuh logic grading existing.

## 4 titik hook

| # | Lokasi | Hook | Catatan |
|---|--------|------|---------|
| 1 | `GradingService.GradeAndCompleteAsync` (blok PCOMP-01) | `MarkPendingReadyIfAnyAsync(session.Id)` setelah `EnsureAsync` | blok sudah braced |
| 2 | `GradingService.RegradeAfterEditAsync` Fail→Pass | `MarkPendingReadyIfAnyAsync` setelah `EnsureAsync` | **W-09: brace-less → braced** |
| 3 | `GradingService.RegradeAfterEditAsync` Pass→Fail | `RevertPendingToMenungguAsync` setelah `RemoveExamOriginAsync` (D-15) | **W-09: brace-less → braced** |
| 4 | `AssessmentAdminController.FinalizeEssayGrading` (blok D-05a) | `MarkPendingReadyIfAnyAsync` setelah `EnsureAsync` | Pitfall 2 — essay early-return; blok sudah braced |

Semua guard `Category=="Assessment Proton" && (isPassed) && ProtonTrackId.HasValue` existing — hook di DALAM guard. Hook no-tx (method plan 04), idempotent.

## Circular DI

Tidak ada: `GradingService → ProtonBypassService` satu arah; `ProtonBypassService` ctor tetap (context, ProtonCompletionService, INotificationService, AuditLogService, logger). Build 0 error memverifikasi.

## Commits

| Task | Commit | Isi |
|------|--------|-----|
| 1 | 3491505c | GradingService inject + 3 hook (W-09 braced) |
| 2 | d134a569 | Essay hook titik 4 + inject controller |

## Deviations from Plan

**[Rule 3 - Blocker] Build lock berulang** — Found during: Task 1 verify | Issue: `HcPortal.exe`/`HcPortal.dll` locked oleh dev server @5277 yang di-respawn berkala via WMI (`cmd /c set Authentication__UseActiveDirectory=false && dotnet run --no-build > C:\Temp\portalhc_5277.log`) — kemungkinan sesi/monitor paralel; log menunjukkan traffic aktif | Fix: kill process tree sebelum build (respawner pakai `--no-build` → serve bit baru setelahnya) | Verification: build 0 error, full suite 195/195 | Commit: n/a

**Total deviations:** 1 (environmental). **Impact:** none pada kode; perhatian untuk plan 07/08 — server @5277 dikuasai proses respawner eksternal.

## Verification

- `dotnet build` 0 error (no circular DI); full suite **195/195** (grading existing tak regresi).
- Grep: `MarkPendingReadyIfAnyAsync(session.Id)` ×2 GradingService + ×1 controller; `RevertPendingToMenungguAsync(session.Id)` ×1.
- Flip behavior diverifikasi integration test plan 04; E2E exam→Siap = UAT plan 08.

## Self-Check: PASSED

## Next

Ready for 360-07 (6 endpoint ProtonDataController).
