---
phase: 411-remove-restore-backend-live
plan: 02
subsystem: testing
tags: [xunit, integration-test, sqlexpress, inmemory, de-tautology, mini-di, cascade-delete, soft-delete, audit]

# Dependency graph
requires:
  - phase: 411-remove-restore-backend-live
    plan: 01
    provides: "RemoveParticipantCoreAsync (hybrid hard/soft) + RemoveParticipantLive/RestoreParticipantLive (JSON outcome {sessionId, mode, linkedSessionId}) + SessionHasDataAsync (D-01) — endpoint under test"
  - phase: 410-add-participant-backend-live
    plan: 02
    provides: "Infra test REUSE: FlexibleParticipantAddFixture (SQLEXPRESS HcPortalDB_Test_{guid}) + StubUserManager/StubUserStore/NoopNotificationService + MakeLiveController + SeedUser/SeedRepSession pattern"
provides:
  - "FlexibleParticipantRemoveTests.cs — 15 test de-tautologis (PRMV-01/04/05 + PLIV-03) terkunci regression"
  - "Mini-DI service-provider stub (BuildCascadeServiceProvider + StubWebHostEnvironment + MakeLiveControllerWithCascade) — gap test-infra terbesar 411, reusable utk fase yang drive cascade lewat HttpContext.RequestServices"
affects: [412-live-monitoring-ui-signalr, 413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "De-tautology 999.12: drive action AssessmentAdminController ASLI + assert kolom DB NYATA — NO replica SessionHasDataAsync (komentar saja), NO panggil cascade ExecuteAsync langsung"
    - "Mini-DI ServiceProvider stub di HttpContext.RequestServices (share ctx SQLEXPRESS yang SAMA) → hard-delete teruji integral lewat RemoveParticipantLive end-to-end (bukan ExecuteAsync langsung)"
    - "Dual-pattern: read-path InMemory (return sebelum resolve-actor → userManager null! aman) + write-path SQLEXPRESS disposable (StubUserManager actor)"
    - "Sanity pre-assert (UPA ada SEBELUM remove) untuk membuktikan AnyAsync==false bukan tautologi"

key-files:
  created:
    - "HcPortal.Tests/FlexibleParticipantRemoveTests.cs (812 baris, 15 [Fact])"
    - ".planning/phases/411-remove-restore-backend-live/411-02-SUMMARY.md"
  modified: []

key-decisions:
  - "Mini-DI (Rekomendasi PATTERNS) dipilih daripada panggil ExecuteAsync langsung — hard-delete drive lewat RemoveParticipantLive ASLI agar RemoveParticipantCoreAsync teruji integral end-to-end (de-tautology + acceptance grep ExecuteAsync==0)"
  - "ProtonCompletionService/RecordCascadeDeleteService logger didaftarkan eksplisit sebagai ILogger<T> (bukan NullLogger<T>.Instance polos) agar AddScoped resolve ctor via reflection menemukan tipe yang benar"
  - "SeedResponseAsync seed AssessmentPackage+PackageQuestion dulu (FK Restrict PackageUserResponse→PackageQuestion enforced di SQLEXPRESS); SeedUpaAsync seed AssessmentPackage dulu (FK UserPackageAssignment→AssessmentPackage)"
  - "B7 (Pre/Post both-clean hard) dipindahkan ke Task 2 sebagai C3 (butuh mini-DI), sesuai instruksi plan Task 1 langkah B7"

patterns-established:
  - "BuildCascadeServiceProvider(ctx) → IServiceProvider: registrasi minimal RecordCascadeDeleteService + ProtonCompletionService + AuditLogService + INotificationService + IWebHostEnvironment(temp WebRootPath) yang share ctx SQLEXPRESS sama"
  - "MakeLiveControllerWithCascade(ctx, actor) = MakeLiveController + set ControllerContext.HttpContext.RequestServices = mini-DI"

requirements-completed: [PRMV-01, PRMV-04, PRMV-05, PLIV-03]

# Metrics
duration: 12min
completed: 2026-06-21
---

# Phase 411 Plan 02: Remove + Restore Backend Live — Integration Tests Summary

**15 test de-tautologis (lesson 999.12) yang mengunci kontrak Plan 411-01: setiap test MENJALANKAN action `RemoveParticipantLive`/`RestoreParticipantLive` ASLI dan meng-assert kolom DB NYATA — hard-delete (baris+UPA `AnyAsync==false` SQLEXPRESS), soft-remove (RemovedAt set NYATA + Score/cert/Status UNCHANGED), idempotency, reason-wajib-soft, restore, Pre/Post pair-as-unit, audit — termasuk mini-DI service-provider stub yang menutup gap test-infra terbesar 411 untuk jalur hard-delete.**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-06-21T06:12:10Z
- **Completed:** 2026-06-21T06:24:29Z
- **Tasks:** 2
- **Files created:** 1 (`HcPortal.Tests/FlexibleParticipantRemoveTests.cs`, 812 baris)

## Accomplishments

### Bagian A — Read-path InMemory (`FlexibleParticipantRemoveReadTests`, 5 test)
Drive action ASLI untuk jalur yang RETURN sebelum `_userManager.GetUserAsync` (userManager `null!` aman):
- **A1 `RemoveParticipantLive_Proton_Rejected400`** — sesi `Category="Assessment Proton"` → 400 + 0-write (RemovedAt null).
- **A2 `RemoveParticipantLive_AlreadyRemoved_NoOp`** — `RemovedAt!=null` → `JsonResult` mode="noop" (idempotency, Pitfall 3).
- **A3 `RemoveParticipantLive_NotFound_404`** — sessionId tak ada → 404.
- **A4 `RestoreParticipantLive_NotRemoved_Rejected400`** — sesi aktif → 400 "Sesi ini tidak dalam keadaan dihapus." (PRMV-04 guard).
- **A5 `RestoreParticipantLive_NotFound_404`** — sessionId tak ada → 404.

### Bagian B — Write-path SQLEXPRESS soft/restore (`FlexibleParticipantRemoveWriteTests`, 7 test)
REUSE `FlexibleParticipantAddFixture`; drive action ASLI dengan StubUserManager actor:
- **B1 `RemoveInProgress_SoftRemoves_PreservesData`** (PRMV-01) — StartedAt+Score=80+InProgress+response → mode="soft"; RemovedAt/RemovedBy/RemovalReason set NYATA; **Score/Status/IsPassed UNCHANGED**, response utuh.
- **B2 `RemoveCertified_SoftRemoves_PreservesCert`** (PRMV-01) — Completed+NomorSertifikat+ManualSertifikatUrl+IsPassed → mode="soft"; **cert/score/status UNCHANGED**.
- **B3 `RemoveSoft_NoReason_Rejected400`** (D-02/PLIV-03) — sesi berdata + reason=null → 400 "Alasan penghapusan wajib diisi." + 0-write (RemovedAt null).
- **B4 `RemoveInProgress_Idempotent_NoOp`** (PRMV-01) — remove(soft) ×2 → panggilan ke-2 mode="noop"; RemovedAt tetap nilai pertama, reason pertama tak tertimpa.
- **B5 `Restore_SoftRemoved_ClearsColumns`** (PRMV-04) — remove(soft) → restore → restored=true; 3 kolom removal di-clear NYATA.
- **B6 `RemovePrePost_OneHasData_SoftBoth`** (PRMV-05) — Pre(berdata)+Post(bersih) LinkedSessionId cross-set → mode="soft"; **kedua** RemovedAt set; JSON linkedSessionId==postId; **peserta LAIN di batch TIDAK ter-remove** (Pitfall 1).
- **B8 `Remove_WritesAuditRow`** (PLIV-03) — soft → `AuditLogs.AnyAsync(ActionType="RemoveParticipantLive" && TargetId==sessionId)`==true.

### Bagian B (Task 2) — Hard-delete via mini-DI service-provider stub (3 test) — gap terbesar 411
- **C1 `RemoveNotStarted_HardDeletes_RowGone`** (PRMV-01 + D-02 hard-no-reason) — sesi bersih + reason=null → mode="hard"; baris `AssessmentSessions.AnyAsync==false` NYATA.
- **C2 `RemoveWithEagerUPA_StillHardDeletes_UpaGone`** (D-01) — sesi bersih + eager-UPA (sanity pre-assert UPA ada) → mode="hard"; baris session HILANG **DAN** `UserPackageAssignments.AnyAsync==false` (UPA bukan "data", cascade hapus UPA).
- **C3 `RemovePrePost_BothClean_HardBoth`** (PRMV-05) — Pre+Post bersih LinkedSessionId cross-set → mode="hard"; **kedua** baris HILANG; **peserta LAIN bersih MASIH ada** (Pitfall 1).

## Mini-DI Approach (gap test-infra terbesar 411)

Jalur hard-delete `RemoveParticipantCoreAsync` memanggil `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()`. `DefaultHttpContext().RequestServices` kosong → throw. Solusi (Rekomendasi PATTERNS, BUKAN alternatif "panggil cascade langsung"):

- **`StubWebHostEnvironment`** — `IWebHostEnvironment` dengan `WebRootPath = Path.GetTempPath()` (cascade hapus file cert post-commit pakai `_env.WebRootPath`; not-started tak punya cert → file tak ada, tapi `Path.Combine` butuh WebRootPath valid).
- **`BuildCascadeServiceProvider(ctx)`** — `ServiceCollection` mini-DI: `AddSingleton(ctx)` (share **ctx SQLEXPRESS yang SAMA** dengan controller agar `BeginTransactionAsync` cascade hapus baris yang dilihat assert) + `AuditLogService(ctx)` + `INotificationService`(noop) + `ILogger<ProtonCompletionService>`/`ILogger<RecordCascadeDeleteService>` (NullLogger eksplisit per-tipe) + `IWebHostEnvironment`(stub) + `AddScoped<ProtonCompletionService>` + `AddScoped<RecordCascadeDeleteService>`.
- **`MakeLiveControllerWithCascade(ctx, actor)`** — `MakeLiveController` + `ctrl.ControllerContext.HttpContext.RequestServices = BuildCascadeServiceProvider(ctx)`. Dipakai SEMUA test hard-delete.

Hasil: hard-delete drive lewat `RemoveParticipantLive` ASLI end-to-end (de-tautology), cascade benar-benar `RemoveRange` baris+UPA dari SQLEXPRESS. **0 fake-green, 0 skip.**

## De-tautology Compliance (lesson 999.12, WAJIB)

- `grep -c "SessionHasDataAsync"` = 2 → **keduanya di komentar** (header banner + Bagian B banner). 0 di kode (NO replica predikat).
- `grep -c "ExecuteAsync"` = 2 → **keduanya di komentar** (NO panggil cascade langsung — drive via RemoveParticipantLive ASLI).
- Hard assert: `AnyAsync == false` atas DB SQLEXPRESS NYATA (3×). Soft assert: kolom set/unchanged NYATA via reload ctx.
- Sanity pre-assert C2 (UPA ada SEBELUM remove) membuktikan assert `AnyAsync==false` bukan tautologi.

## Test Results (pass counts per pattern)

| Pattern | Class | Count | Result |
|---------|-------|-------|--------|
| Read-path InMemory (A1-A5) | FlexibleParticipantRemoveReadTests | 5 | 5/5 PASS |
| Write-path soft/restore/audit (B1-B6,B8) | FlexibleParticipantRemoveWriteTests | 7 | 7/7 PASS |
| Write-path hard-delete mini-DI (C1-C3) | FlexibleParticipantRemoveWriteTests | 3 | 3/3 PASS |
| **Total FlexibleParticipantRemove** | | **15** | **15/15 PASS** (28 s) |
| **Full suite (no regression)** | HcPortal.Tests | **596** | **596/596 PASS** (3 m 47 s) |

Baseline 581 (411-01 SUMMARY) + 15 baru = 596. 409 guard (ParticipantRemovalGuardTests) + 410 add (FlexibleParticipantAddLiveTests) tetap hijau.

## Task Commits

1. **Task 1: read-path InMemory + write-path soft/restore/Pre-Post/audit (de-tautology)** — `cafd641d` (test)
2. **Task 2: hard-delete write-path via mini-DI service-provider stub (D-01)** — `3ec00420` (test)

## Files Created/Modified

- `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (NEW, 812 baris, 15 `[Fact]`) — Bagian A InMemory + Bagian B SQLEXPRESS (soft+hard) + mini-DI.
- `.planning/phases/411-remove-restore-backend-live/411-02-SUMMARY.md` — this file.

## Deviations from Plan

None — plan executed exactly as written.

Catatan kecil (bukan deviasi, sesuai instruksi plan):
- **B7 dipindahkan ke Task 2 sebagai C3** — eksplisit di plan Task 1 langkah B7 ("Pindahkan B7 + hard-delete tests ke Task 2 (butuh service-provider stub)"). Total tetap 15 test (12 nama B1-B8 di plan; B7→C3, plus C1/C2/C4-gabung-C1).
- **C4 (hard-no-reason) digabung ke C1** — sesuai plan Task 2 langkah C4 ("bisa digabung ke C1 dengan reason:null"). C1 memanggil `RemoveParticipantLive(sessionId, reason: null)` → menguji hard tanpa reason sukses (D-02 opsional di hard).

## Issues Encountered

- **CS0136 variable shadowing** (B6: `pre` di seed-block & verify-block) — fixed inline (rename verify-block ke `vPre`/`vPost`/`vOther`). Tertangkap saat build pertama, bukan runtime.
- **Logger registration** — `NullLogger<T>.Instance` perlu didaftarkan eksplisit sebagai `ILogger<ProtonCompletionService>`/`ILogger<RecordCascadeDeleteService>` (bukan tipe konkret) agar `AddScoped` ctor-reflection menemukan dependency. Diatasi dengan cast eksplisit `AddSingleton<ILogger<T>>(NullLogger<T>.Instance)`.
- Tidak ada constraint nyata lain — mini-DI berhasil resolve cascade end-to-end; hard-delete benar-benar hapus baris+UPA dari SQLEXPRESS.

## Environment / Constraint Notes

- **SQLEXPRESS tersedia** (SQL Server 2025 17.0.1000.7) — write-path Integration BENAR berjalan (bukan skip): 10 test `[Trait Category=Integration]` executed.
- **DB test auto-disposed** — `sys.databases LIKE 'HcPortalDB_Test_%'` = 0 rows pasca-run (fixture `DisposeAsync` → `EnsureDeletedAsync`).
- **HcPortalDB_Dev TIDAK disentuh** (CLAUDE.md Seed Workflow).
- **migration=FALSE** — `git status Migrations/ Data/` kosong (hanya tambah file test).

## User Setup Required

None. migration=FALSE. Branch main, NOT pushed (deploy bareng v32.5 bundle).

## Next Phase Readiness

- **Plan 411-02 SELESAI** — PRMV-01/04/05 + PLIV-03 terkunci regression de-tautologis. Mini-DI pattern (`BuildCascadeServiceProvider`/`MakeLiveControllerWithCascade`) tersedia untuk fase mendatang yang perlu drive cascade lewat `HttpContext.RequestServices`.
- **Phase 412** (UI Monitoring Detail + SignalR) konsumsi JSON outcome `{sessionId, mode, linkedSessionId}` — kontrak sudah teruji nyata di sini.
- Notify IT: Phase 411 (01+02) = migration=FALSE.

## Self-Check: PASSED

- FOUND: `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (812 baris, 15 [Fact])
- FOUND commit: `cafd641d` (Task 1)
- FOUND commit: `3ec00420` (Task 2)
- Tests: 15/15 FlexibleParticipantRemove PASS; full suite 596/596 PASS (0 failed, 0 skipped)
- De-tautology: SessionHasDataAsync=2 (komentar saja), ExecuteAsync=2 (komentar saja), AnyAsync==false ×3 (hard NYATA)
- migration=FALSE (Migrations/ Data/ clean); no lingering HcPortalDB_Test_* DBs

---
*Phase: 411-remove-restore-backend-live*
*Completed: 2026-06-21*
