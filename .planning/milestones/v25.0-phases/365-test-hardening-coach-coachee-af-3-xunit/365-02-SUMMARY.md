---
phase: 365-test-hardening-coach-coachee-af-3-xunit
plan: 02
subsystem: coaching-graduate
tags: [coaching, test, xunit, real-sql, integration, proton, af-3]
requires: [365-01]
provides: [MarkMappingCompletedTests]
affects:
  - HcPortal.Tests/MarkMappingCompletedTests.cs
tech-stack:
  added: []
  patterns: [real-sql-fixture-reuse, static-core-direct-call, 2nd-context-asnotracking-verify]
key-files:
  created:
    - HcPortal.Tests/MarkMappingCompletedTests.cs
  modified: []
key-decisions:
  - "Fact #4 dipecah jadi 2 [Fact] (ProgressNotApproved + NoFinalAssessment) → total 7 [Fact] (≥6 OK)."
  - "Bonus: Fact #2 buktikan index bergigi (insert mapping aktif KEDUA pra-graduate → ThrowsAnyAsync<DbUpdateException>) di context terpisah, lalu re-assign sukses pasca-graduate."
  - "Core dipanggil langsung tanpa ambient transaksi → SaveChangesAsync auto-commit; verify via 2nd context AsNoTracking (pola ProtonApproveRejectParityTests)."
requirements-completed: []
duration: ~5 min
completed: 2026-06-12
---

# Phase 365 Plan 02: MarkMappingCompletedTests (6→7 [Fact] real-SQL AF-3 lock)

`HcPortal.Tests/MarkMappingCompletedTests.cs` — kunci perilaku graduate AF-3 di real SQL, memanggil `CoachMappingController.MarkMappingCompletedCore` (core Plan 01) langsung via `ProtonCompletionFixture` (REUSE, tidak redefinisi).

## 7 [Fact] (Fact #4 dipecah 2)
| # | Nama | Skenario | Assert inti |
|---|------|----------|-------------|
| 1 | `MarkMappingCompleted_Happy_FullEndState` | Tahun-3 lulus penuh | ok=true, cascadeCount==activeBefore; IsCompleted/IsActive/CompletedAt/EndDate; tiap assignment IsActive=false+DeactivatedAt; progress count utuh (D-07) |
| 2 | `MarkMappingCompleted_ReassignableAfterGraduate` | re-assign pasca-graduate | pra-graduate: mapping aktif ke-2 → `ThrowsAnyAsync<DbUpdateException>` (index bergigi); pasca-graduate: insert aktif baru → SUKSES (Id>0) — bukti D-03 index bebas |
| 3 | `MarkMappingCompleted_Guard_NoTahun3` | tanpa Tahun 3 | ok=false, Contains("Tahun 3"), cascadeCount=0, mapping tak termutasi |
| 4a | `MarkMappingCompleted_Guard_Tahun3_ProgressNotApproved` | progress "Submitted" | ok=false, Contains("belum lulus"), tak termutasi |
| 4b | `MarkMappingCompleted_Guard_Tahun3_NoFinalAssessment` | Approved tapi tanpa FinalAssessment | ok=false, Contains("belum lulus"), tak termutasi |
| 5 | `MarkMappingCompleted_MappingNotFound` | core(ctx, -1) | ok=false, Contains("tidak ditemukan"), cascadeCount=0 |
| 6 | `MarkMappingCompleted_ProgressHistoryIntact` | graduate tak hapus progress | count progress pasca == baseline |

## Helper seed
- `SeedTrackChainAsync(ctx, coacheeId, tahunKe, progressStatus, withFinalAssessment)` — chain track(REUSE FirstAsync)→komp→sub→2 deliverable→2 progress (+ optional FinalAssessment 1:1).
- `SeedActiveMappingAsync` — CoachCoacheeMapping aktif (StartDate WAJIB set).
- `SeedGraduateReadyAsync` — Tahun 3 + Approved + FinalAssessment + mapping aktif.
- Isolasi per-fact: `coacheeId = $"grad-{Guid.NewGuid():N}"`. Track migration di-REUSE (NO `ProtonTracks.Add`). Fixture TIDAK diredefinisi.

## Hasil
- Filter `MarkMappingCompletedTests`: **Passed! 7/7 (0 failed, 980 ms)**.
- Full suite `dotnet test`: **Passed! 236/236 (0 failed, 39 s)** = baseline 229 + 7 baru, **0 regresi**.

## Grep acceptance (semua PASS)
Trait Integration ×1, IClassFixture<ProtonCompletionFixture> ×1, [Fact] ×7, `MarkMappingCompletedCore(ctx` ×7, `SeedGraduateReadyAsync` ×4, reuse-track FirstAsync ×1, `ProtonTracks.Add(` ×0, `class ProtonCompletionFixture` ×0, token Tahun 3/belum lulus/tidak ditemukan present, happy fields (IsCompleted/IsActive/CompletedAt/EndDate/DeactivatedAt/cascadeCount) present.

## Files changed
- `HcPortal.Tests/MarkMappingCompletedTests.cs` (created).

## Catatan
TIDAK butuh SEED_WORKFLOW (DB disposable `HcPortalDB_Test_<guid>`; `HcPortalDB_Dev` tak tersentuh). TIDAK assert audit log (D-09). Real SQL `localhost\SQLEXPRESS` (sama dgn ProtonCompletionServiceTests). migration=false.
