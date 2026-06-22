---
phase: 410-add-participant-backend-live
plan: 02
subsystem: test
tags: [assessment, participant, test, integration, de-tautology, eager-upa]

# Dependency graph
requires:
  - phase: 410-01
    provides: "AddParticipantsLive (HttpPost) + GetEligibleParticipantsToAdd (HttpGet) + CreateEagerAssignmentsAsync (eager UPA A1)"
  - phase: 391
    provides: "FlexibleParticipantAddFixture (SQLEXPRESS disposable HcPortalDB_Test_{guid})"
  - phase: 409
    provides: "ParticipantRemovalExcludeTests pola InMemory real-controller + kolom RemovedAt"
provides:
  - "FlexibleParticipantAddLiveEligibleTests — read-path de-tautology (GetEligibleParticipantsToAdd ASLI)"
  - "FlexibleParticipantAddLiveWriteTests — write-path de-tautology (AddParticipantsLive ASLI atas SQLEXPRESS)"
affects: [413-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Drive controller-mutasi ASLI via stub UserManager minimal (override GetUserAsync) + no-op INotificationService — Opsi 2a de-tautology penuh untuk write-path"
    - "JSON round-trip (Serialize→JsonDocument.Parse) untuk assert Json(anonymous) tanpa coupling tipe"

key-files:
  created:
    - "HcPortal.Tests/FlexibleParticipantAddLiveTests.cs — 10 test (4 read-path InMemory + 6 write-path SQLEXPRESS)"
  modified: []

key-decisions:
  - "Opsi 2a dipakai untuk SEMUA write-path test (T5-T10): stub UserManager (GetUserAsync→actor) + no-op INotificationService → AddParticipantsLive ASLI menulis ke DB nyata; assert kolom NYATA. Bukan Opsi 2b (seed-then-assert) — de-tautology penuh tercapai."
  - "Read-path class diberi nama FlexibleParticipantAddLiveEligibleTests (mengandung 'Live') agar filter plan FullyQualifiedName~FlexibleParticipantAddLive menangkap KESEPULUH test, bukan hanya 6 write-path."
  - "NO replica predikat (WindowAllowsAddition/DeriveReadyStatus/IsRunning tiruan) di file — anti-pattern 999.12. Occurrence kata kunci hanya di komentar dokumentasi."

requirements-completed: [PART-06, PART-07]

# Metrics
duration: 12min
completed: 2026-06-21
---

# Phase 410 Plan 02: FlexibleParticipantAddLive Integration Tests Summary

**10 test de-tautologis untuk endpoint Phase 410: read-path (4) menjalankan `GetEligibleParticipantsToAdd` ASLI via InMemory real-controller (exclude sesi APAPUN D-01, no unit/section D-02, 404, idempotency), write-path (6) menjalankan `AddParticipantsLive` ASLI atas SQLEXPRESS disposable via stub UserManager (ready-status, EAGER UPA A1, window-reject 400 + 0-write, Proton-reject, Pre/Post pair + LinkedSessionId, idempotent skipped[]). Tanpa replica predikat (999.12); 10/10 hijau; full suite 581/581; migration=FALSE.**

## Performance
- **Duration:** ~12 min
- **Tasks:** 1 file test baru (2 kelas, 10 [Fact])
- **Files created:** 1 (`HcPortal.Tests/FlexibleParticipantAddLiveTests.cs`, 584 baris)

## Accomplishments

### Bagian A — Read-path (`FlexibleParticipantAddLiveEligibleTests`, Pola 1 InMemory real-controller)
Drive `GetEligibleParticipantsToAdd` ASLI (service null AMAN — action tak pakai userManager/notif). Hasil JSON di-round-trip (Serialize→JsonDocument) untuk baca `id` tanpa coupling tipe anonim.

- **T1 `Eligible_ExcludesUsersWithAnySession_IncludingRemoved`** (D-01): user A (sesi aktif `RemovedAt==null`) + user B (sesi `RemovedAt!=null`) + user C (tanpa sesi) → eligible HANYA C; A DAN B excluded (removed TETAP excluded — hanya balik via Restore 411). `RemovedAt` di-seed.
- **T2 `Eligible_IgnoresUnitSection_ExcludesInactive`** (D-02): user dengan Section/Unit BEDA dari batch tetap eligible (tanpa filter unit/section); user `IsActive=false` excluded.
- **T3 `Eligible_RepNotFound_Returns404`**: `sessionId` tak ada → `NotFoundObjectResult`.
- **T4 `Eligible_AlreadyInBatch_NeverAppears`**: user dengan sesi di batch tak pernah muncul eligible (idempotency read-side, konsisten D-01).

### Bagian B — Write-path (`FlexibleParticipantAddLiveWriteTests`, Pola 2 SQLEXPRESS disposable)
**REUSE `FlexibleParticipantAddFixture`** (`HcPortalDB_Test_{guid}` + `MigrateAsync`; HcPortalDB_Dev TIDAK disentuh). **Opsi 2a**: drive `AddParticipantsLive` ASLI penuh via `StubUserManager` (override `GetUserAsync`→actor seeded, `IUserStore` no-op) + `NoopNotificationService` → action menulis ke DB nyata; assert kolom NYATA.

- **T5 `..._NewSession_HasReadyStatus_RemovalNull`** (PART-06): sesi baru `Status=="Open"` (DeriveReadyStatus, schedule lampau), `StartedAt/CompletedAt/RemovedAt` NULL, NEVER InProgress.
- **T6 `..._WithPackages_CreatesEagerUserPackageAssignment`** (A1): batch dgn 1 `AssessmentPackage` + 3 `PackageQuestion` + `PackageOption` → setelah add, `UserPackageAssignment` tercipta EAGER (`AssessmentSessionId==sesi baru`, `ShuffledQuestionIds != "[]"`, 3 qids).
- **T7 `..._WindowClosed_Returns400_NoWrite`** (PART-06): `ExamWindowCloseDate` kemarin → `BadRequestObjectResult` berisi verbatim **"Window ujian sudah tutup, tidak bisa tambah peserta."** + 0 sesi baru (count batch tak bertambah).
- **T8 `..._Proton_Returns400_NoWrite`**: `Category=="Assessment Proton"` → 400 + 0 write.
- **T9 `..._PrePost_CreatesPair_WithCrossLink`** (PART-07): rep `AssessmentType="PreTest"` + `LinkedGroupId` → 2 sesi baru (PreTest+PostTest) untuk user, `LinkedSessionId` cross-set kedua arah, `LinkedGroupId` inherited, keduanya `Status=="Open"` (ready, bukan hardcoded "Upcoming").
- **T10 `..._ExistingUser_Skipped_NoDuplicate`** (PART-06): `userIds=[existing,fresh]` → `addedCount==1` + `skippedCount==1`; existing tetap 1 sesi (tak dobel), fresh dapat 1 sesi.

## Test Results
- **Filter plan** `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAddLive"`: **Passed! 10/0/0** (4 read-path + 6 write-path).
- **Full suite** `dotnet test HcPortal.Tests`: **Passed! 581/0/0** (tak regresi; 575 baseline + 6 write-path Integration baru terhitung di sini, read-path InMemory juga masuk).
- **Build:** `dotnet build HcPortal.Tests` → 0 error (26 warning pre-existing).
- **SQLEXPRESS:** tersedia lokal (SQL Server 2025 `localhost\SQLEXPRESS`); write-path Integration BENAR-BENAR dijalankan (bukan skipped). Tidak ada constraint — semua 10 test berjalan & hijau.

## Decisions Made
- **Opsi drive write-path = 2a (PREFERRED), bukan 2b.** Plan menawarkan 2a (stub UserManager) vs 2b (seed-then-assert kolom). `UserManager<ApplicationUser>` non-sealed + `GetUserAsync(ClaimsPrincipal)` virtual → subclass minimal cukup (base ctor 9-arg dgn `IUserStore` stub no-op; dependency lain `null!` karena hanya `GetUserAsync` dipanggil). Ini de-tautology penuh: SELURUH logika produksi (DeriveReadyStatus, window-guard, Proton-guard, idempotency, transaksi atomic, CreateEagerAssignmentsAsync, Pre/Post cross-link) DIJALANKAN, bukan ditiru. T7/T8 reject-path sebenarnya return sebelum `GetUserAsync` (Langkah 3 < Langkah 6) — tetap di-drive lewat action ASLI dengan stub untuk konsistensi.
- **Rename read-path class** `FlexibleParticipantAddEligibleTests` → `FlexibleParticipantAddLiveEligibleTests` agar verify-filter plan (`~FlexibleParticipantAddLive`) menangkap kesepuluh test (semula hanya 6 write-path yang cocok).
- **Drive ASLI vs seed-then-assert:** SEMUA 10 test drive action controller ASLI (read: GetEligibleParticipantsToAdd; write: AddParticipantsLive). Tidak ada test yang sekadar seed-then-assert-replica.

## Deviations from Plan

None signifikan — plan dieksekusi sesuai tulisan. Catatan implementasi (bukan deviasi):
- Plan menawarkan Opsi 2a ATAU 2b untuk create-path; dipilih **2a penuh** untuk SEMUA write-path (de-tautology maksimal) karena stub UserManager terbukti feasible (`GetUserAsync` virtual). Tidak ada test yang turun ke 2b.
- Class read-path diberi nama mengandung "Live" (di luar yang persis disebut plan §A "FlexibleParticipantAddEligibleTests") agar SATU filter verify plan menangkap semua test. Acceptance criteria tetap terpenuhi: ada kelas write-path `[Trait("Category","Integration")]` + kelas read-path InMemory.
- Tidak ada deviasi Rule 1-4; tidak ada perubahan kode produksi (test-only); migration=FALSE.

## Anti-Tautology Confirmation (999.12)
- **NO replica predikat** di file: tidak ada method `WindowAllowsAddition`/`DeriveReadyStatus`/`IsRunning` tiruan (analog lama `FlexibleParticipantAddTests :86-118` TIDAK ditiru). Grep `WindowAllowsAddition|DeriveReadyStatus|IsRunning` → hanya muncul di komentar dokumentasi, bukan definisi.
- Read-path **menjalankan LINQ produksi ASLI** (`GetEligibleParticipantsToAdd`); write-path **menjalankan transaksi + eager-UPA produksi ASLI** (`AddParticipantsLive` + `CreateEagerAssignmentsAsync`) atas schema SQL nyata.

## Acceptance Criteria Check
- [x] File `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` ada
- [x] `class FlexibleParticipantAddLiveEligibleTests` (Pola 1 InMemory) + `class FlexibleParticipantAddLiveWriteTests` dengan `[Trait("Category","Integration")]`
- [x] Read-path invoke REAL action: `await ctrl.GetEligibleParticipantsToAdd(` present
- [x] Write-path `ctrl.AddParticipantsLive(` present
- [x] `RemovedAt` present di seed eligible (D-01)
- [x] `UserPackageAssignment` present di write-path (A1)
- [x] Verbatim `Window ujian sudah tutup, tidak bisa tambah peserta.` present
- [x] `LinkedSessionId` present (PART-07)
- [x] NO `WindowAllowsAddition` replica method (hanya komentar)
- [x] `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAddLive"` → 10/10 Passed

## Task Commits
1. **FlexibleParticipantAddLive integration tests** — `2ff434c5` (test) — 1 file, 584 insertions, 0 deletions, 0 migration.

## Issues Encountered
None. SQLEXPRESS tersedia → write-path Integration berjalan penuh (tidak ada skip / tidak ada fake-green).

## Next Phase Readiness
- **Phase 411 (Remove/Restore):** pola test ini (read InMemory real-controller + write SQLEXPRESS Opsi-2a stub UserManager) siap di-reuse untuk `RemoveParticipantLive`/`RestoreParticipantLive`.
- **Phase 413 (Test+UAT):** suite e2e endpoint penuh (antiforgery + HttpContext lengkap) — Plan 02 sudah mengunci logika inti via action ASLI; 413 fokus Playwright + e2e antiforgery.
- **NOT pushed** (lokal saja; deploy bareng milestone v32.5; migration=FALSE).

## Self-Check: PASSED
- FOUND: `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs`
- FOUND commit: `2ff434c5`
- 10/10 new tests Passed · Full suite 581/581 · Build 0 error · migration=FALSE · 0 deletions in commit

---
*Phase: 410-add-participant-backend-live*
*Completed: 2026-06-21*
