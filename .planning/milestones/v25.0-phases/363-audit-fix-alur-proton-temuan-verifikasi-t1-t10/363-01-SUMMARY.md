---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
plan: 01
subsystem: proton-approval
tags: [cdp, proton, approve, reject, extraction, pin-tests]
requires: []
provides:
  - "CDPController.ApproveDeliverableCoreAsync (static, context-only) — per-role set + race-guard D-10 + allApproved"
  - "CDPController.RejectDeliverableCoreAsync (static) — full chain reset including HCApprovalStatus/HCReviewedById/At"
  - "CDPController.AddDeliverableStatusHistory (static) — single status-history insert path"
  - "CDPController.DispatchApproveNotificationsAsync (instance) — shared coach/coachee + HC allApproved notif"
  - "ProtonApproveRejectParityTests — executable contract Plan 02 must satisfy"
affects: [363-02, 363-06, 363-07]
tech-stack:
  added: []
  patterns: ["static core on controller (public static, no InternalsVisibleTo)", "real-SQL disposable fixture pin tests"]
key-files:
  created:
    - HcPortal.Tests/ProtonApproveRejectParityTests.cs
  modified:
    - Controllers/CDPController.cs
key-decisions:
  - "Cores public static (bukan internal per plan) — proyek tidak punya InternalsVisibleTo; konvensi CMPController:3969/phase-351 = public static agar test reachable"
  - "Error routing endpoint: error 'Deliverable tidak ditemukan.' → NotFound(); race-guard error → TempData + RedirectToAction(CoachingProton) — preserve perilaku asli"
requirements-completed: [T1, T2, T7]
duration: 18 min
completed: 2026-06-11
---

# Phase 363 Plan 01: Extract Approve/Reject Cores + Pin Tests Summary

Static cores `ApproveDeliverableCoreAsync`/`RejectDeliverableCoreAsync` di CDPController menyerap verbatim logika gold-standard (per-role set, race-guard D-10 reload-fresh, allApproved, full chain reset termasuk HC); endpoint `ApproveDeliverable`/`RejectDeliverable` di-rewire ke core dengan zero behavior change, dibuktikan 4 pin test real-SQL hijau.

- Duration: 18 min (08:15–08:33 UTC)
- Tasks: 2/2 | Files: 2

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `bb4529ff` | refactor(363-01): extract approve/reject domain cores from gold-standard endpoints |
| 2 | `3b9f2e9f` | test(363-01): pin gold-standard approve/reject end-state contract |

## What Was Built

- `ApproveDeliverableCoreAsync(context, progressId, actorId, actorName, actorRole, isSrSpv, isSH)` → `(ok, error, allApproved)`: load include-chain, per-role approval set (Pattern A), race-guard reload-fresh AsNoTracking + stillCanApprove (Pattern B/T7), overall Status=Approved, status history, allApproved computation (Pattern C/T1), SaveChanges.
- `RejectDeliverableCoreAsync(context, progressId, actorId, actorName, actorRole, rejectionReason)` → `(ok, error)`: full chain reset (Pattern D/T2) — SrSpv/SH/HC semua → Pending + null ID/timestamp TERMASUK `HCApprovalStatus`+`HCReviewedById/At`; reject TIDAK set approver-id (anomali RejectFromProgress :2057-2059 tidak direplikasi).
- `AddDeliverableStatusHistory` static; `RecordStatusHistory` jadi thin wrapper — semua caller existing tetap valid.
- `DispatchApproveNotificationsAsync(progress, allApproved)`: notif coach/coachee COACH_EVIDENCE_APPROVED + `CreateHCNotificationAsync` saat allApproved — siap dipakai ApproveFromProgress di Plan 02.
- `ApproveFromProgress`/`RejectFromProgress` TIDAK disentuh (masih buggy — Plan 02).

## Pin Tests (kontrak Plan 02)

`ProtonApproveRejectParityTests` — `[Trait("Category","Integration")]`, `IClassFixture<ProtonCompletionFixture>`, panggil core langsung tanpa konstruksi controller. 4/4 PASS @SQLEXPRESS:
1. `RejectCore_ResetsFullChain_IncludingHC` — HC "Reviewed" + semua approver wajib ter-reset.
2. `ApproveCore_LastDeliverable_ReturnsAllApprovedTrue`
3. `ApproveCore_NotLast_ReturnsAllApprovedFalse`
4. `ApproveCore_RaceGuard_RejectsStaleSecondApprove` — error "diproses oleh approver lain".

## Deviations from Plan

**[Rule 3 - Blocker] Visibility core `public static` alih-alih `internal static`** — Found during: Task 1 | Issue: plan minta `internal`, tapi proyek tidak punya `[assembly: InternalsVisibleTo("HcPortal.Tests")]` → pin test Task 2 tidak bisa compile | Fix: pakai `public static` sesuai konvensi proyek terdokumentasi (CMPController:3969, phase 351-02) | Files: Controllers/CDPController.cs | Verification: build 0 error + 4/4 test pass | Commit: bb4529ff

**Total deviations:** 1 auto-fixed (1 Rule 3 blocker). **Impact:** none — visibility lebih lebar dari plan tapi konsisten konvensi proyek; tidak ada perubahan perilaku.

## Verification

- `dotnet build HcPortal.csproj -c Debug` → 0 error (22 warning pre-existing Views).
- `dotnet test --filter "FullyQualifiedName~ProtonApproveRejectParity"` → 4/4 PASS (1s).
- Grep AC: definisi + call site kedua core ada; `HCApprovalStatus = "Pending"` di reject core; FromProgress signatures utuh (:2001, :2080).

## Self-Check: PASSED

## Next

Ready for 363-02 (rewire ApproveFromProgress/RejectFromProgress ke core — pin test jadi gate).
