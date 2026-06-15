---
phase: 360-bypass-backend-b
plan: 05
subsystem: proton-bypass
tags: [proton, bypass, confirm, cancel, atomic-guard]
requires: [360-04]
provides:
  - "ConfirmBypassAsync — §5.3 D-11 stale-check + D-12 atomic Siap→Selesai + pindah"
  - "CancelPendingAsync — §8.1 Dibatalkan atomik + auto-cancel exam per status worker (W-03)"
  - "MoveAssignmentAsync (private) — step pindah §5.1 shared ExecuteInstant+Confirm, param excludeSessionId"
affects: [360-07]
tech-stack:
  added: []
  patterns:
    - "ExecuteUpdateAsync WHERE Status + rowsAffected==0 untuk transisi status anti-race dalam transaksi"
key-files:
  created: []
  modified:
    - Services/ProtonBypassService.cs
    - HcPortal.Tests/ProtonBypassServiceTests.cs
key-decisions:
  - "Refactor shared helper MoveAssignmentAsync (opsi plan, anti-duplikasi): step (2)-(6) §5.1 dipakai ExecuteInstant + Confirm; ConfirmBypass pass excludeSessionId=LinkedAssessmentSessionId"
  - "W-03: cancel session via WHERE s.Status != \"Completed\" — Completed-gagal (MISS-2) tidak di-overwrite"
  - "I-06: branch lulus/belum dari pending.Status (sebelum ExecuteUpdate), tanpa query paket"
requirements-completed: [PBYP-02, PBYP-06]
duration: 16 min
completed: 2026-06-10
---

# Phase 360 Plan 05: ConfirmBypassAsync §5.3 + CancelPendingAsync §8.1 Summary

Resolusi pending lengkap: konfirmasi pindah anti-race/anti-stale (D-11+D-12) yang reuse step pindah §5.1 via helper `MoveAssignmentAsync` (bootstrap W-02 + coach + excludeSessionId T-360-17), dan batal pending dua-branch dengan guard W-03 — 19/19 bypass suite + 195/195 full hijau.

## Signature (konsumsi endpoint plan 07)

```csharp
public Task<BypassResult> ConfirmBypassAsync(int pendingId, string hcId, string hcName); // §5.3
public Task<BypassResult> CancelPendingAsync(int pendingId, string hcId, string hcName); // §8.1
```

## ConfirmBypassAsync (1 transaksi)

Pending wajib `Siap` → **D-11** re-check (source assignment masih aktif + linked `IsPassed==true` + penanda `Origin="Exam"` ada) → **D-12** `ExecuteUpdateAsync WHERE Status="Siap"` + `rows==0` guard (klik dobel/2 HC → pindah SEKALI) → pindah via `MoveAssignmentAsync(source, req, excludeSessionId: LinkedAssessmentSessionId)` **TANPA re-force-approve & TANPA re-create penanda** → audit (hcId/hcName pemanggil, bukan inisiator).

## CancelPendingAsync — keputusan W-03 (penting untuk plan 07)

`workerLulus = pending.Status=="Siap"` disimpan SEBELUM ExecuteUpdate. Branch:
- **Lulus (Siap):** session + penanda Exam DIPERTAHANKAN — hanya rencana batal.
- **Belum (Menunggu):** cancel session via `WHERE s.Status != "Completed"` — sesi `Completed` IsPassed=false (worker kerjakan-gagal, MISS-2) TIDAK di-overwrite "Dibatalkan".
Force-approve source tidak di-revert. Status valid untuk batal: `Menunggu`/`Siap` saja.

## Refactor MoveAssignmentAsync

Step (2)-(6) §5.1 (cancel exam source + deactivate + create target Origin="Bypass" + bootstrap unit-FORM + coach D-16/D-16b/E15) diekstrak dari `ExecuteInstantBypassAsync` jadi helper privat tanpa tx sendiri. 13 test existing tetap hijau pasca-refactor (perilaku tak berubah).

## Commits

| Task | Commit | Isi |
|------|--------|-----|
| 1 | 3e6e5722 | ConfirmBypassAsync + helper MoveAssignmentAsync (refactor) |
| 2 | 80742c1f | CancelPendingAsync §8.1 |
| 3 | 2c80c9cd | 6 integration test confirm/cancel |

## Deviations from Plan

**[Rule 3 - Blocker] Build lock dev server orphan** — Found during: Task 1-2 verify | Issue: `HcPortal.exe` locked by orphan `dotnet run --no-build` dari sesi sebelumnya (PID 25280, lalu 24688; parent process sudah mati) | Fix: kill process tree | Files: none | Verification: build sukses | Commit: n/a

**[Criterion adjusted] Grep W-02 `ProtonDeliverableBootstrap.CreateProgressAsync` ≥2 → actual 1** — Plan menyuruh dua hal yang bertentangan: grep count ≥2 DAN "pilih refactor yang menghindari duplikasi step pindah". Dipilih shared helper (opsi eksplisit plan) → call muncul 1x di `MoveAssignmentAsync` yang dipakai KEDUA jalur. Intent W-02 (bootstrap jalan di confirm) diverifikasi test `Confirm_HappyPath` assert 2 progress target.

**[Rule 1 - Bug] Test stale tracker** — Found during: Task 3 | Issue: 2 test Confirm gagal "Pending tidak ditemukan atau belum siap" — flip `ExecuteUpdateAsync` bypass change tracker; `FindAsync` di ctx test shared return entity stale `Menunggu` (produksi aman: context per-request fresh) | Fix: `ctx.Entry(pending).ReloadAsync()` di helper test setelah flip | Files: HcPortal.Tests/ProtonBypassServiceTests.cs | Verification: 19/19 | Commit: 2c80c9cd

**Total deviations:** 3 (1 blocker env, 1 criterion adjusted by design, 1 test-only bug). **Impact:** service code sesuai plan; tidak ada perubahan perilaku produksi di luar plan.

## Observasi non-blocking

Commit `3e6e5722` ikut menyapu rename `.gitkeep` `.planning/phases/999.3-* → 366-*` yang sudah ter-stage dari sesi paralel lain — harmless, isi file kosong.

## Verification

- `dotnet build` 0 error; bypass suite **19/19**; full suite **195/195**.
- Double-confirm XOR sukses + assignment aktif tetap 1; cancel 3 skenario benar; confirm tanpa penanda dobel.

## Self-Check: PASSED

## Next

Ready for 360-06 (hook GradingService 4 titik §7).
