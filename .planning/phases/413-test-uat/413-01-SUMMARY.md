---
phase: 413-test-uat
plan: 01
subsystem: assessment-participant-test
tags: [assessment, participant, test, integration, lifecycle, de-tautology]
requires:
  - AssessmentAdminController.AddParticipantsLive (410)
  - AssessmentAdminController.RemoveParticipantLive/RestoreParticipantLive (411)
  - CMPController.IsParticipantRemoved (409 guard seam)
  - FlexibleParticipantAddFixture (SQLEXPRESS disposable, 410)
  - mini-DI RecordCascadeDeleteService pattern (411)
provides:
  - FlexibleParticipantLifecycleTests (cross-phase lifecycle integration lock)
affects:
  - HcPortal.Tests
tech-stack:
  added: []
  patterns: [xunit-integration, sqlexpress-disposable-fixture, mini-DI-cascade, de-tautology-999.12]
key-files:
  created:
    - HcPortal.Tests/FlexibleParticipantLifecycleTests.cs
  modified: []
decisions:
  - "IsParticipantRemoved dipanggil langsung sebagai CMPController.IsParticipantRemoved(session) (public static) — helper PRODUKSI ASLI, bukan replica/fallback"
metrics:
  duration: ~12m
  completed: 2026-06-21
  tasks: 1
  files: 1
  tests_added: 3
---

# Phase 413 Plan 01: Cross-Phase Lifecycle Integration Test Summary

xUnit integration `FlexibleParticipantLifecycleTests` mengunci integrasi LINTAS-FASE seluruh fitur add/remove/restore peserta live dalam satu alur DB SQLEXPRESS nyata — celah yang belum ter-cover test per-fase (410 add, 411 remove/restore, 409 guard, masing-masing terpisah). 3 [Fact] (L1 standard add→start→soft-remove→guard→restore, L2 Pre/Post pair, L3 hard-delete row+UPA gone), semua drive action `AssessmentAdminController` ASLI + helper produksi `CMPController.IsParticipantRemoved` + assert kolom DB nyata. De-tautologis penuh (999.12), migration=FALSE.

## What Was Built

**File baru:** `HcPortal.Tests/FlexibleParticipantLifecycleTests.cs` (553 baris, 3 [Fact]). 1 kelas `FlexibleParticipantLifecycleTests : IClassFixture<FlexibleParticipantAddFixture>` (`[Trait("Category","Integration")]`).

Helper di-COPY verbatim (isolasi penuh, default plan): `StubUserManager`/`StubUserStore`/`NoopNotificationService`/`MakeLiveController`/`SeedUserAsync`/`SeedRepSessionAsync`/`SeedPackageWithQuestionsAsync` (dari AddLive) + `StubWebHostEnvironment`/`BuildCascadeServiceProvider`/`MakeLiveControllerWithCascade` (dari Remove). `NewCtx()` = `new ApplicationDbContext(_fixture.Options)`. `FlipInProgressAsync` = state-transition test-driven (set StartedAt/Status=InProgress/CompletedAt=null via ctx — bukan replica produksi; produksi flip via StartExam yang butuh HTTP/SignalR di luar scope unit).

**L1 `Lifecycle_Add_Start_SoftRemove_GuardBlocks_Restore_Active`** (PART-06 + PRMV-01 + PRMV-03 + PRMV-04):
1. `AddParticipantsLive(repId, [newUser])` ASLI (batch + paket soal) → assert sesi baru Status=="Open", StartedAt/CompletedAt/RemovedAt null, UPA eager tercipta, `IsParticipantRemoved`==false.
2. `FlipInProgressAsync(newSessionId)` → InProgress + StartedAt set; `IsParticipantRemoved` tetap false (sah lanjut).
3. `RemoveParticipantLive(newSessionId, "tidak hadir")` ASLI → mode=="soft"; RemovedAt set, RemovedBy==actorId, RemovalReason=="tidak hadir", Status UNCHANGED (InProgress).
4. **GUARD LINTAS-FASE:** `CMPController.IsParticipantRemoved(reload)` PRODUKSI == **true** (peserta tak bisa lanjut/submit, PRMV-03).
5. `RestoreParticipantLive(newSessionId)` ASLI → restored=true; 3 kolom removal di-clear; `IsParticipantRemoved` == **false** (aktif lagi).

**L2 `Lifecycle_PrePost_Add_SoftRemoveBoth_RestoreBoth`** (PART-07 + PRMV-05): Add Pre/Post pair ASLI → 2 sesi (LinkedSessionId cross-set 2-arah) + ready-status. Flip Pre InProgress → `RemoveParticipantLive(newPreId)` → mode=="soft", JSON linkedSessionId==newPostId; KEDUA partner RemovedAt!=null + `IsParticipantRemoved`==true; peserta LAIN di batch TIDAK ikut removed (Pitfall 1). `RestoreParticipantLive(newPreId)` → KEDUA partner RemovedAt==null + `IsParticipantRemoved`==false.

**L3 `Lifecycle_Add_NotStarted_HardRemove_RowAndUpaGone`** (PRMV-01, mini-DI): Add bersih ASLI → sesi not-started (StartedAt null) + UPA eager (sanity ADA sebelum remove). `RemoveParticipantLive(newSessionId, null)` via `MakeLiveControllerWithCascade` → mode=="hard"; `AssessmentSessions.AnyAsync(Id)`==false DAN `UserPackageAssignments.AnyAsync`==false (D-01: UPA bukan "data"). `RestoreParticipantLive(newSessionId)` → NotFoundObjectResult/BadRequestObjectResult (hard tak reversibel).

## Cara Panggil `IsParticipantRemoved`

**Helper PRODUKSI langsung** — `CMPController.IsParticipantRemoved(session)`. Signature di `Controllers/CMPController.cs:2540`: `public static bool IsParticipantRemoved(AssessmentSession session) => session.RemovedAt != null`. Helper yang SAMA dipanggil guard inline StartExam (`:373`), SubmitExam (`:924`), dan `:1611`. Karena `public static`, dipanggil langsung tanpa instantiate CMPController. **TIDAK ada fallback/replica** — assertion guard memakai helper produksi ASLI 16× (false saat aktif/restored, true saat soft-removed). Pola identik `ParticipantRemovalGuardTests.cs:284-315` (409 de-tautologis).

## Verification

- **`dotnet build HcPortal.Tests`** → 0 error (26 warning pre-existing, NOL dari file baru).
- **`dotnet test --filter "FullyQualifiedName~FlexibleParticipantLifecycle"`** → **Passed: 3, Failed: 0, Skipped: 0** (Duration 2s — SQLEXPRESS write-path BENAR berjalan, BUKAN skip).
- **Full suite `dotnet test HcPortal.Tests`** → **Passed: 605, Failed: 0, Skipped: 0** (baseline 602 + 3 baru, NOL regresi).
- **De-tautology:** `grep -nE "SessionHasDataAsync|WindowAllowsAddition|\.ExecuteAsync"` di luar komentar == **0** (hanya di header comment + comment penjelas). NO replica predikat.
- **migration=FALSE:** `git status Migrations/ Data/` kosong.
- Acceptance: [Fact]=3 ✓, IClassFixture=1 ✓, action-drive=11 (≥5) ✓, IsParticipantRemoved=16 (≥1) ✓, AnyAsync=6 (≥1) ✓, LinkedSessionId=6 (≥1) ✓.

## Deviations from Plan

None - plan executed exactly as written. Test-only plan; nol kode produksi disentuh. Helper accessible langsung (public static) → tak perlu fallback yang diantisipasi plan.

## Authentication Gates

None.

## Known Stubs

None — test mendrive jalur produksi nyata atas SQLEXPRESS disposable; tak ada stub yang menggantikan logika di bawah test (StubUserManager/NoopNotification/mini-DI hanya menyediakan dependency infrastruktur, bukan menggantikan logika add/remove/restore/guard yang diuji).

## Commits

- `e43025ce` — test(413-01): cross-phase lifecycle integration (1 file, +553)

## Self-Check: PASSED

- FOUND: HcPortal.Tests/FlexibleParticipantLifecycleTests.cs
- FOUND commit: e43025ce
