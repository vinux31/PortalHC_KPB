---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
plan: 03
subsystem: proton-completion
tags: [proton, penanda, audit, notification, surface-miss]
requires: []
provides:
  - "EnsureAsync no-assignment miss → AuditLog PROTON_PENANDA_MISS + bell HC (tidak silent lagi)"
  - "FakeNotificationService shared di HcPortal.Tests (lifted)"
affects: [363-07]
tech-stack:
  added: []
  patterns: ["dedup-notif-by-exact-message (D-14)", "system actor di AuditLog"]
key-files:
  created:
    - HcPortal.Tests/FakeNotificationService.cs
    - HcPortal.Tests/ProtonCompletionMissTests.cs
  modified:
    - Services/ProtonCompletionService.cs
    - HcPortal.Tests/ProtonBypassServiceTests.cs
    - HcPortal.Tests/ProtonCompletionServiceTests.cs
    - HcPortal.Tests/ProtonYearGateIntegrationTests.cs
key-decisions:
  - "FakeNotificationService di-lift ke file shared (bukan duplikat) — dipakai BypassTests + MissTests + 2 NewSvc"
  - "NewBypassSvc kini share SATU instance fake antara ProtonCompletionService & ProtonBypassService (notif dari kedua layer terekam)"
requirements-completed: [T4]
duration: 10 min
completed: 2026-06-11
---

# Phase 363 Plan 03: Surface Penanda Miss (T4) Summary

`ProtonCompletionService.EnsureAsync` kini surface miss penanda (assignment nonaktif saat lulus exam): AuditLog `PROTON_PENANDA_MISS` (actor system) + broadcast bell ke HC (RoleLevel==2, dedup by exact message) menunjuk BackfillProtonPenanda — IsActive tetap strict (D-07), jalur idempotent tetap silent (Pitfall 3).

- Duration: 10 min | Tasks: 2/2 | Files: 6

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `81b861c4` | feat(363-03): surface penanda miss via AuditLog + HC bell notification |
| 2 | `bac1305d` | test(363-03): miss-surface tests + update 3 ctor sites |

## What Was Built

- Ctor `ProtonCompletionService` +2 dep (`INotificationService`, `AuditLogService`) — keduanya scoped di Program.cs:51/:67, DI auto-resolve tanpa perubahan registrasi.
- Branch `assignment == null`: LogWarning existing dipertahankan, lalu `_auditLog.LogAsync("system","system/grading","PROTON_PENANDA_MISS",...)` + broadcast `SendAsync` ke tiap HC aktif dengan dedup `UserNotifications` exact-message, actionUrl `/Admin/ManageAssessment`.
- Branch already-exists (`:48`) tidak disentuh; filter `IsActive` tidak diubah.
- 3 site `new ProtonCompletionService(` test di-update 4-arg; `FakeNotificationService` lifted ke file shared; `NewBypassSvc` share satu instance fake ke kedua service.
- `ProtonCompletionMissTests`: 2 fact (surface-on-miss: audit Single + notif ke HC; idempotent: nol audit + nol notif).

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- `dotnet build` 0 error.
- `dotnet test --filter ProtonCompletionMiss` → 2/2 PASS.
- `dotnet test --filter "Category=Integration&FullyQualifiedName~Proton"` → 41/41 PASS (ctor baru tidak meregresi suite Proton existing).

## Self-Check: PASSED

## Next

Ready for 363-04 (T6 asimetri ValidUntil sertifikat — GradingService).
