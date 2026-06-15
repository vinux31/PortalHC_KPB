---
phase: 360-bypass-backend-b
plan: 04
subsystem: proton-bypass
tags: [proton, bypass, pending, notification, grading-hook]
requires: [360-03]
provides:
  - "BypassSaveAsync — entry semua mode (E8 + validasi pure + D-10 + dispatch)"
  - "ExecutePendingBypassAsync — jalur pending CL-B(b) §5.2"
  - "MarkPendingReadyIfAnyAsync — hook grading flip Menunggu→Siap + notif (no-tx)"
  - "RevertPendingToMenungguAsync — hook re-grade fail Siap→Menunggu (no-tx, D-15)"
affects: [360-05, 360-06, 360-07]
tech-stack:
  added: []
  patterns:
    - "ExecuteUpdateAsync WHERE-guard rowsAffected untuk flip status atomik tanpa transaksi (pola GradingService:234)"
    - "Bare AssessmentSession: UserId + AssessmentType eksplisit (ISS-04/B-05)"
key-files:
  created: []
  modified:
    - Services/ProtonBypassService.cs
    - HcPortal.Tests/ProtonBypassServiceTests.cs
key-decisions:
  - "E8 (B-04) dicek di BypassSaveAsync entry SEMUA mode via activeCount (ToListAsync 1 query, source assignment dipakai resolve flag)"
  - "ExecutePendingBypassAsync TANPA guard mode tambahan — selalu CL-B(b) by construction (W-05, dispatch satu-satunya caller)"
  - "Notif PROTON_BYPASS_READY hanya ke InitiatedById (HC); worker tidak dapat (T-360-13)"
requirements-completed: [PBYP-02, PBYP-03, PBYP-06]
duration: 13 min
completed: 2026-06-10
---

# Phase 360 Plan 04: Jalur Pending CL-B(b) + Lifecycle Menunggu→Siap Summary

CL-B(b) buat-tunggu §5.2 lengkap: BypassSaveAsync entry-dispatch (E8 B-04 + D-10), bare AssessmentSession UserId+AssessmentType (B-05), pending Menunggu, dan 2 hook no-tx (flip Siap + notif HC; revert D-15) — 13/13 integration test real-SQL hijau.

## Signature 4 method (konsumsi plan 05/06/07)

```csharp
public Task<BypassResult> BypassSaveAsync(BypassRequest req);           // entry SEMUA mode → endpoint plan 07
public Task<BypassResult> ExecutePendingBypassAsync(BypassRequest req); // §5.2 (dipanggil dispatch saja)
public Task<bool> MarkPendingReadyIfAnyAsync(int sessionId);            // hook GradingService plan 06 (no-tx)
public Task RevertPendingToMenungguAsync(int sessionId);               // hook re-grade fail plan 06 (no-tx, D-15)
```

## Penempatan E8 (B-04)

Di `BypassSaveAsync` langkah pertama, SEBELUM validasi/dispatch: `ToListAsync` assignment aktif → `activeCount != 1` tolak. Menutup celah pending CL-B(b) dari worker dobel-assignment → 2 aktif pasca-confirm. `ExecuteInstantBypassAsync` tetap re-cek defense-in-depth (jalur lama plan 03 tak berubah).

## Bare session CL-B(b) (B-05/W-06)

`UserId=req.CoacheeId` (per-worker key — tanpa ini worker tak lihat exam, penanda EnsureAsync gagal diam-diam, pending nyangkut Menunggu) + `AssessmentType="Standard"` (kolom DB NOT NULL — NULL → SqlException 515). ProtonTrackId=source, TahunKe=source year, Status="Upcoming", GenerateCertificate=true, TANPA paket (D-01) — return `ShowAttachPackageReminder=true` (D-02, controller plan 07 wajib TempData warning). Force-approve deliverable source D-13 TANPA terbit penanda (penanda nanti Origin="Exam" oleh GradingService).

## Hook no-tx (Pitfall 4 / T-360-11)

Kedua hook TANPA `BeginTransactionAsync` (hot-path grading). Flip atomik `ExecuteUpdateAsync WHERE Status` + guard rowsAffected → idempotent (test SafeRepeat: panggilan ke-2 return false, notif tidak dobel). Notif ke `pending.InitiatedById` (HC), bukan worker.

## Commits

| Task | Commit | Isi |
|------|--------|-----|
| 1 | b522d527 | BypassSaveAsync (E8+D-10+dispatch) + ExecutePendingBypassAsync §5.2 |
| 2 | 5a0eaa3e | MarkPendingReadyIfAnyAsync + RevertPendingToMenungguAsync (no-tx) |
| 3 | eb3505a3 | 6 integration test CL-B(b) pending lifecycle |

## Deviations from Plan

**[Rule 1 - Bug] Test seed FK AssessmentSessions.UserId** — Found during: Task 3 | Issue: 5 test baru gagal — bare session insert kena FK required `AssessmentSessions.UserId → AspNetUsers` (coachee random tidak ada di Users; `ProtonTrackAssignment.CoacheeId` tanpa FK jadi plan 03 tak kena) | Fix: helper `SeedUserAsync` seed `ApplicationUser` per coachee di test CL-B(b) | Files: HcPortal.Tests/ProtonBypassServiceTests.cs | Verification: 13/13 hijau | Commit: eb3505a3

**Total deviations:** 1 auto-fixed (1 test-seed bug). **Impact:** test-only; service code sesuai plan verbatim.

## Verification

- `dotnet build` 0 error; full suite **189/189** hijau (169 unit + 20 integration).
- `dotnet test --filter FullyQualifiedName~ProtonBypassServiceTests` → **13/13** (7 plan 03 + 6 baru).
- Grep gates: BeginTransactionAsync hanya di ExecuteInstant (:101) + ExecutePending (:330), TIDAK di hook; `UserPackageAssignment` 0 match di service (D-01).

## Self-Check: PASSED

## Next

Ready for 360-05 (ConfirmBypassAsync §5.3 + CancelPendingAsync §8.1).
