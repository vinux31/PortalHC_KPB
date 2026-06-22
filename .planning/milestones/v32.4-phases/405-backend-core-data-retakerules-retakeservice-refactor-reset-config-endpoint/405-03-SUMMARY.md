---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
plan: 03
subsystem: services
tags: [retake, service, claim-atomic, integration, sql-real, v32.4]
requires:
  - "Models/AssessmentAttemptResponseArchive.cs + 3 kolom config + migration AddRetakeColumnsAndArchive (plan 405-01)"
  - "Helpers/RetakeRules.cs (CanRetake pure) + Helpers/RetakeArchiveBuilder.cs (Build snapshot) (plan 405-02)"
provides:
  - "Services/RetakeService.cs — ExecuteAsync (claim-first → snapshot → archive → delete → audit → SignalR reason) + CanRetakeAsync (D-01 snapshot-presence) (RTK-07/13)"
  - "RetakeResult record struct (Success, Error)"
  - "DI registration RetakeService scoped (Program.cs)"
  - "HcPortal.Tests/RetakeServiceTests.cs + RetakeServiceFixture + NoOpHubContext (integration SQL-real)"
affects:
  - "Plan 405-04 (ResetAssessment controller refactor → delegasi _retakeService.ExecuteAsync; TempData clear di controller)"
  - "Phase 407 (worker self-service: RetakeExam controller panggil CanRetakeAsync + ExecuteAsync(actionType=RetakeAssessment))"
tech_stack:
  added: []
  patterns:
    - "Claim-transisi-atomik DULU (ExecuteUpdateAsync WHERE Status NOT IN Cancelled,Open + rows==0 abort) anti double-archive"
    - "Snapshot per-soal SEBELUM RemoveRange via RetakeArchiveBuilder.Build (frozen verdict, retain D-04)"
    - "D-01 snapshot-presence EXISTS subquery — legacy HC-reset archive natural-excluded dari cap counting"
    - "Counting (UserId,Title,Category) anti-konflasi Pre/Post (must-fix #3)"
    - "SignalR reason parameterized (Pitfall 7); audit actionType RetakeAssessment/ResetAssessment (must-fix #6)"
    - "NoOpHubContext hand-stub (project test tanpa Moq/NSubstitute) — SendCoreAsync no-op"
    - "Disposable-DB MigrateAsync full chain (mirror MultiUnitSqlFixture/RecordCascadeFixture)"
key_files:
  created:
    - Services/RetakeService.cs
    - HcPortal.Tests/RetakeServiceTests.cs
  modified:
    - Program.cs
decisions:
  - "AttemptNumber = eraRetakeArchives + 1 (era-retake count, BUKAN total/HC-reset legacy) — A1/D-01 konsisten dengan cap; rekomendasi RESEARCH"
  - "Hub-stub: NoOpHubContext hand-written (implement IHubContext<AssessmentHub>/IHubClients/IClientProxy/IGroupManager) — Moq tak tersedia di HcPortal.Tests.csproj"
  - "Audit + SignalR di-bungkus try/catch warn-only — kegagalan audit/broadcast TIDAK membatalkan reset yang sudah commit"
metrics:
  duration: "12m"
  completed: "2026-06-21"
  tasks: 2
  files: 3
migration: false
notify_it: false
---

# Phase 405 Plan 03: Backend Core — RetakeService Shared Engine Summary

**One-liner:** Mesin ujian ulang bersama v32.4 `RetakeService.ExecuteAsync` yang memproduktisasi inti `ResetAssessment` HC dengan **3 koreksi wajib** — claim-transisi-atomik DULU (anti double-archive), snapshot per-soal SEBELUM delete via `RetakeArchiveBuilder`, dan counting `(UserId,Title,Category)` + D-01 snapshot-presence (arsip HC-reset legacy tak konsumsi cap) — plus `CanRetakeAsync` (server-authoritative) dan DI scoped, terbukti 5/5 di integration SQL-real terhadap `localhost\SQLEXPRESS`.

## What Was Built

Wave 3 — service orchestration layer. Memindahkan (bukan menulis ulang) logika reset existing `ResetAssessment` (`AssessmentAdminController.cs:4238-4323`) ke service reusable yang dipanggil dua jalur (HC plan 405-04 + worker Phase 407), sambil menutup 3 lubang.

### Task 1 — `RetakeService.ExecuteAsync` + `CanRetakeAsync` + DI (RTK-07/13) — commit `a3d5f5ca`

File baru `Services/RetakeService.cs` (`HcPortal.Services`). Deps DI: `ApplicationDbContext`, `AuditLogService`, `IHubContext<AssessmentHub>`, `ILogger<RetakeService>`. `public readonly record struct RetakeResult(bool Success, string? Error)`.

**`ExecuteAsync(int sessionId, string actorUserId, string actorName, string actionType, string reason)` — urutan WAJIB:**
1. Load sesi → null ⇒ `(false, "Sesi tidak ditemukan.")`.
2. `bool wasCompleted = assessment.Status == "Completed"` (ditangkap SEBELUM claim).
3. **CLAIM-ATOMIK DULU** (Pitfall 1): `ExecuteUpdateAsync WHERE Id==sessionId && Status != "Cancelled" && Status != "Open"` set `Status="Open"` + null-out (Score/IsPassed/Progress/StartedAt/CompletedAt/ElapsedSeconds/LastActivePage/UpdatedAt). `rows==0` ⇒ `(false, "Sesi tidak dapat direset (sudah dibatalkan atau sudah terbuka).")`. `Status != "Open"` mencegah re-claim request kedua (double-click).
4. Jika `wasCompleted`: counting era-retake `(UserId,Title,Category)` + EXISTS snapshot-presence → buat `AssessmentAttemptHistory { AttemptNumber = eraRetakeArchives+1 }`, `SaveChanges` (assign Id) → load `assignment.GetShuffledQuestionIds` → `PackageQuestions.Include(Options)` where qIds + `PackageUserResponses` where session → `RetakeArchiveBuilder.Build(attemptHistory.Id, questions, responses)` → `AddRange` (jika count>0). **SNAPSHOT SEBELUM delete.**
5. Delete live: `PackageUserResponses` + `UserPackageAssignment` + `SessionElemenTeknisScores` (mirror existing `:4262-4282`). `SaveChanges` (snapshot AddRange + delete satu batch).
6. Audit `LogAsync(actorUserId, actorName, actionType, "... (reason={reason})", sessionId, "AssessmentSession")` — try/catch warn-only.
7. SignalR `Clients.User(assessment.UserId).SendAsync("sessionReset", new { reason })` parameterized — try/catch warn-only.
8. `(true, null)`.

**`CanRetakeAsync(int sessionId) → bool`:** load sesi (null⇒false) → `eraRetakeArchives` via EXISTS snapshot-presence query (D-01, grouping `(UserId,Title,Category)`) → `RetakeRules.CanRetake(..., attemptsUsed: eraRetakeArchives+1, ...)`.

**DI:** `Program.cs` setelah `ProtonBypassService` (`AddScoped<HcPortal.Services.RetakeService>()`).

### Task 2 — Integration test SQL-real (RTK-07/13) — commit `3c4210bb`

File baru `HcPortal.Tests/RetakeServiceTests.cs` (`[Trait("Category","Integration")]`). Fixture `RetakeServiceFixture` (disposable `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync` full chain incl `AddRetakeColumnsAndArchive`, drop on dispose + mid-failure catch melempar `XunitException` "MIGRATION-CHAIN break"). Hub-stub `NoOpHubContext` (hand-written; project test tanpa Moq) + `NullLogger` + `AuditLogService` real.

5 test cases (semua GREEN):
- `Claim_DoubleExecute_SecondAborts` — execute ke-1 success; ke-2 (sesi Open) `Success==false`; hanya 1 `AttemptHistory` (anti double-archive).
- `Snapshot_WrittenBeforeResponsesDeleted` — snapshot count==N (4 soal) + live responses==0 + verdict beku true.
- `CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap` (D-01) — legacy AttemptHistory tanpa child → `CanRetake==true`.
- `CanRetake_RetakeEraArchiveWithSnapshot_ConsumesCap` (D-01) — era archive dengan child → `CanRetake==false`.
- `Counting_PrePostSameTitle_NoConflate` — Title="X" Pre punya era archive (cap habis→false), Post TIDAK terpengaruh (true).

## Verification Results

| Cek | Hasil |
|-----|-------|
| `dotnet build` (full) | Build succeeded, 0 Error ✓ |
| `dotnet build HcPortal.Tests` | Build succeeded, 0 Error ✓ |
| `dotnet test --filter "FullyQualifiedName~RetakeServiceTests"` | **Passed! 5/5, 0 failed** (real SQLEXPRESS, 2s) ✓ |
| `dotnet test --filter "Category!=Integration"` | Passed! 436/438 (2 skipped), 0 failed — no regresi unit ✓ |
| Claim-first (ExecuteUpdateAsync L77 SEBELUM Build L135) | verified grep ✓ |
| WHERE `Status != "Cancelled"` && `Status != "Open"` | verified L76 ✓ |
| Counting `h.Category ==` (bukan Title saja) + EXISTS `AssessmentAttemptResponseArchives.Any` | verified L99-100 / L206-207 ✓ |
| SignalR `new { reason }` parameterized | verified L181 ✓ |
| `Program.cs` `AddScoped<HcPortal.Services.RetakeService>()` | verified L63 ✓ |

## Konfirmasi claim-first urutan

✅ **Claim mendahului archive.** `ExecuteUpdateAsync` (claim-atomik) berada di L77, sedangkan `RetakeArchiveBuilder.Build` (snapshot) di L135 dan `AssessmentAttemptHistory.Add` di L113. Urutan file: load → `wasCompleted` snapshot status → **claim (L76-83)** → `rows==0` abort (L85-86) → archive/snapshot (L88-141) → delete (L144-167) → audit → SignalR. Ini KEBALIKAN dari `ResetAssessment` existing yang archive/delete dulu (`:4239-4286`) lalu claim (`:4288`). Test `Claim_DoubleExecute_SecondAborts` membuktikan request kedua di-abort sebelum menyentuh archive (hanya 1 AttemptHistory).

## Keputusan AttemptNumber (era-retake vs total)

Dipilih **`AttemptNumber = eraRetakeArchives + 1`** (era-retake count, A1/RESEARCH rekomendasi) — konsisten dengan cap counting. Arsip HC-reset legacy (tanpa child snapshot) TIDAK dihitung, sehingga "Percobaan ke-N" yang dilihat pekerja (Phase 407) = N era-retake, bukan termasuk HC-reset legacy pre-v32.4. Monoton naik + terisi (memenuhi syarat 405). Display final = Phase 407.

## Pola Hub-stub yang dipakai

`NoOpHubContext` hand-written (`internal sealed`) yang mengimplementasi `IHubContext<AssessmentHub>` + nested `IHubClients`/`IClientProxy` (`SendCoreAsync` → `Task.CompletedTask`)/`IGroupManager`. Alasan: `HcPortal.Tests.csproj` TIDAK punya Moq/NSubstitute. `ExecuteAsync` memanggil `SendAsync` (extension atas `SendCoreAsync`) yang no-op → tak butuh SignalR backplane. Logger = `NullLogger<RetakeService>.Instance`. `AuditLogService` = instance real atas test DbContext (audit row benar-benar ditulis ke disposable DB).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking adapt] AttemptHistory `SaveChanges` mid-ExecuteAsync untuk assign Id sebelum builder**
- **Found during:** Task 1
- **Issue:** `RetakeArchiveBuilder.Build(attemptHistory.Id, ...)` butuh `attemptHistory.Id` yang ter-generate DB. `ExecuteUpdateAsync` (claim) bypass change-tracker, jadi tak ada konflik; tapi `attemptHistory` di-Add via change-tracker butuh `SaveChanges` agar Id terisi sebelum builder dipanggil.
- **Fix:** Tambah `await _context.SaveChangesAsync()` setelah `AssessmentAttemptHistory.Add` (sebelum Build), lalu `SaveChanges` kedua setelah snapshot AddRange + delete. Dua SaveChanges total dalam `wasCompleted` path (sama seperti existing yang flush archive sebelum claim). Tak ada transaksi eksplisit (konsisten existing).
- **Files modified:** Services/RetakeService.cs
- **Commit:** `a3d5f5ca`

**2. [Rule 3 - Blocking adapt] NoOpHubContext hand-stub (no Moq)**
- **Found during:** Task 2
- **Issue:** Plan `<action>` menyarankan Moq/NSubstitute "bila tersedia"; `HcPortal.Tests.csproj` tidak mereferensikan keduanya.
- **Fix:** Implementasi `NoOpHubContext` manual (mengikuti opsi fallback plan: "minimal stub class implement IHubContext yang return no-op IClientProxy"). Zero dependency baru.
- **Files modified:** HcPortal.Tests/RetakeServiceTests.cs
- **Commit:** `3c4210bb`

Selebihnya plan dieksekusi persis: signature `RetakeArchiveBuilder.Build`/`RetakeRules.CanRetake` cocok dengan plan `<interfaces>`; `AuditLogService.LogAsync` cocok; `IHubContext<AssessmentHub>` cocok; field null-out `ExecuteUpdateAsync` mirror existing `:4288-4300`.

## TDD Gate Compliance

Plan frontmatter Task 1 ber-`tdd="true"`, tetapi struktur plan menempatkan service implementation di Task 1 (verify = `dotnet build`) dan integration test di Task 2 (separate). Bukan plan-level `type: tdd` — RED/GREEN per-task tidak dipisah karena integration test (Task 2) memang tahap kedua plan. Service + test keduanya commit terpisah (`feat` lalu `test`); semua 5 integration test GREEN saat ditulis terhadap implementasi Task 1.

## Notes for Downstream Plans

- **Plan 405-04** (refactor `ResetAssessment`): inject `RetakeService` ke constructor controller; REPLACE inline block `:4238-4323` dengan `await _retakeService.ExecuteAsync(id, user.Id, actorName, "ResetAssessment", "hc_reset")` + cek `result.Success` untuk error-redirect + `TempData.Remove($"TokenVerified_{id}")` (must-fix #1 — service TIDAK sentuh TempData). KEEP guard `IsResettable`/Pre-Post/status di controller. `RetakeResult.Error` → `TempData["Error"]`.
- **Phase 407** (worker self-service): `RetakeExam` controller re-cek `await _retakeService.CanRetakeAsync(id)` (server-authoritative, jangan trust client) + ownership UserId → `ExecuteAsync(id, workerId, workerName, "RetakeAssessment", "worker_retake")` + `TempData.Remove`.
- `CanRetakeAsync` adalah satu-satunya entry counting era-retake DB-aware (D-01) — jangan duplikasi query di controller.

## Known Stubs

`NoOpHubContext` adalah test-double (no-op SignalR) — sengaja, hanya di test project. BUKAN stub produksi: `RetakeService` produksi memakai `IHubContext<AssessmentHub>` real via DI. Tidak ter-render ke UI. Tidak ada stub produksi.

## Threat Flags

Tidak ada surface keamanan baru di luar `<threat_model>` plan.
- **T-405-08** (double-click double-archive) di-mitigate: claim-atomik DULU + `rows==0` abort (test `Claim_DoubleExecute_SecondAborts`).
- **T-405-09** (legacy archive konsumsi cap → DoS-by-policy) di-mitigate: D-01 snapshot-presence EXISTS (test `CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap`).
- **T-405-10** (cap bypass via client) di-mitigate (partial): `CanRetakeAsync` server-authoritative; enforcement worker = Phase 407.
- **T-405-11** (counting konflasi Pre/Post) di-mitigate: `(UserId,Title,Category)` (test `Counting_PrePostSameTitle_NoConflate`).
- **T-405-12** (reset tanpa audit) di-mitigate: `LogAsync(actionType, reason)` setelah delete.
RetakeService TIDAK melakukan authz (by-design — caller wajib RBAC HC 405-04 / ownership worker 407, didokumentasikan di XML-doc).

## Self-Check: PASSED

- Files: `Services/RetakeService.cs`, `HcPortal.Tests/RetakeServiceTests.cs`, `Program.cs` (modified), `405-03-SUMMARY.md` — semua FOUND.
- Commits: `a3d5f5ca` (feat), `3c4210bb` (test) — semua FOUND.
