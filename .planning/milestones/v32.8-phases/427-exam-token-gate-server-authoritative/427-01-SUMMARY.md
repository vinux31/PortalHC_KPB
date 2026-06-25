---
phase: 427-exam-token-gate-server-authoritative
plan: 01
subsystem: exam-security
tags: [exam, token, security, server-authoritative, migration, ef-core, retake]

# Dependency graph
requires:
  - phase: 405-retake-backend-core
    provides: "RetakeService.ExecuteAsync (ExecuteUpdateAsync reset chain) + RetakeServiceFixture/RetakeExamEndpointTests pola"
  - phase: 424-grading-dedup-gating
    provides: "GRDF-01 Pre→Post gate di StartExam (dipertahankan, di luar scope)"
provides:
  - "Kolom DB AssessmentSession.TokenVerifiedAt (DateTime? nullable) — state verifikasi token server-authoritative"
  - "Migration AddTokenVerifiedAt (datetime2 NULL, aditif, no backfill)"
  - "VerifyToken stamp TokenVerifiedAt=UtcNow + persist (token-required sukses)"
  - "StartExam gate baca kolom (ganti TempData.Peek), guard StartedAt==null utuh"
  - "RetakeService single-source reset TokenVerifiedAt=null (worker + HC retake)"
affects: [428-startexam-write-on-get-idempotency, merge-ithandoff-main-startexam]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Token-gate state pindah dari TempData (client round-trip) ke kolom DB server-authoritative"
    - "Reset single-source di ExecuteUpdateAsync chain (kill-drift lintas jalur retake)"

key-files:
  created:
    - Migrations/20260624133656_AddTokenVerifiedAt.cs
    - Migrations/20260624133656_AddTokenVerifiedAt.Designer.cs
    - HcPortal.Tests/TokenVerifiedAtTests.cs
  modified:
    - Models/AssessmentSession.cs
    - Controllers/CMPController.cs
    - Controllers/AssessmentAdminController.cs
    - Services/RetakeService.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "D-01: reset TokenVerifiedAt=null single-source di RetakeService.ExecuteAsync (cover worker RetakeExam + HC ResetAssessment)"
  - "D-02: full replacement — semua TempData token gate dihapus (grep TokenVerified_ Controllers/+Services/ = 0)"
  - "D-03: stamp HANYA pada jalur token-required sukses; not-required tak stamp (null benar secara semantik)"
  - "Guard StartedAt==null dipertahankan — sesi legacy InProgress tak terkunci pasca-deploy (SC#4)"

patterns-established:
  - "Server-authoritative gate: baca kolom DB, bukan nilai client round-trip (TempData)"
  - "Reset state lintas-jalur via satu ExecuteUpdateAsync SetProperty chain (single source)"

requirements-completed: [EXSEC-01]

# Metrics
duration: 9min
completed: 2026-06-24
---

# Phase 427 Plan 01: Exam Token-Gate Server-Authoritative Summary

**Token verifikasi masuk ujian kini server-authoritative & persisten via kolom `AssessmentSession.TokenVerifiedAt` (DateTime? nullable) — stamp saat VerifyToken sukses, dibaca di gate StartExam (ganti TempData.Peek), reset null single-source di RetakeService; migration AddTokenVerifiedAt applied.**

## Performance

- **Duration:** ~9 min
- **Started:** 2026-06-24T13:36:09Z
- **Completed:** 2026-06-24T13:44:40Z
- **Tasks:** 3
- **Files modified/created:** 8 (3 created, 5 modified)

## Accomplishments

- Kolom DB `TokenVerifiedAt` + migration `AddTokenVerifiedAt` (nullable, no backfill) applied ke `HcPortalDB_Dev` (terverifikasi `sqlcmd COL_LENGTH=8`).
- `VerifyToken` (token-required sukses) men-stamp `TokenVerifiedAt=UtcNow` + `SaveChangesAsync` (D-03); not-required branch tak stamp.
- `StartExam` gate baca `assessment.TokenVerifiedAt == null` menggantikan `TempData.Peek` (D-02); outer guard `IsTokenRequired && owner && StartedAt==null` dipertahankan PERSIS (SC#4 no-lockout).
- `RetakeService.ExecuteAsync` me-reset `TokenVerifiedAt=null` via `.SetProperty(...)` di `ExecuteUpdateAsync` chain — single source D-01 (cover worker `RetakeExam` + HC `ResetAssessment`).
- Semua TempData token gate dihapus penuh — `grep TokenVerified_` di `Controllers/` + `Services/` = 0 hit (D-02).
- 5 test real-SQL T1-T5 (gate/stamp/reset/no-lockout) hijau; suite non-Integration 544/0/2 (no regresi).

## Task Commits

Each task was committed atomically:

1. **Task 1 [BLOCKING]: Kolom TokenVerifiedAt + migration AddTokenVerifiedAt** - `5e585e99` (feat)
2. **Task 2: VerifyToken stamp + StartExam gate kolom + RetakeService reset + hapus TempData** - `bc1dbb63` (feat)
3. **Task 3: 5 test T1-T5 real-SQL (TokenVerifiedAtTests.cs)** - `f3ccf380` (test)

**Plan metadata:** _(final docs commit — STATE.md + ROADMAP.md + REQUIREMENTS.md + this SUMMARY)_

## Files Created/Modified

- `Models/AssessmentSession.cs` - Kolom `DateTime? TokenVerifiedAt` setelah `AccessToken`.
- `Migrations/20260624133656_AddTokenVerifiedAt.cs` - `ALTER TABLE AssessmentSessions ADD TokenVerifiedAt datetime2 NULL` (aditif, no backfill).
- `Migrations/20260624133656_AddTokenVerifiedAt.Designer.cs` - Designer migrasi.
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Regen snapshot (memuat `TokenVerifiedAt`).
- `Controllers/CMPController.cs` - VerifyToken stamp (D-03) + StartExam gate read kolom (D-02) + hapus TempData token (VerifyToken 2 cabang + RetakeExam).
- `Controllers/AssessmentAdminController.cs` - Hapus `TempData.Remove($"TokenVerified_{id}")` di ResetAssessment (D-02).
- `Services/RetakeService.cs` - `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` di ExecuteUpdateAsync (D-01) + update XML comment usang.
- `HcPortal.Tests/TokenVerifiedAtTests.cs` - 5 test real-SQL T1-T5 (`[Trait("Category","Integration")]`, `IClassFixture<RetakeServiceFixture>`).

## Decisions Made

- Mengikuti rencana persis untuk D-01/D-02/D-03 + guard `StartedAt==null`.
- Migration name `AddTokenVerifiedAt`, timestamp `20260624133656` (otomatis > semua migrasi existing branch, R-2 patuh; snapshot auto-regen, tak edit migrasi lama).
- DB lokal target = `HcPortalDB_Dev` (dari `appsettings.Development.json` connection string).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Wire NoOpHubContext + StubUrlHelper ke CMPController factory test**
- **Found during:** Task 3 (5 test T1-T5)
- **Issue:** Factory `MakeCmp` (disalin dari `RetakeExamEndpointTests`) null-substitute `IHubContext` dan tak set `ctrl.Url`. Run-1: T2 `StartExam_Proceeds` lempar `NullReferenceException` di broadcast `workerStarted` (`_hubContext.Clients.Group(...)` saat `justStarted`); T3 `VerifyToken` lempar `ArgumentNullException` di `Url.Action(...)` (IUrlHelper null). Bukan bug produk — kedua deps memang dipanggil jalur sukses StartExam/VerifyToken yang test ini latih (RetakeExam tidak melatihnya, makanya pola sumber bisa null-substitute keduanya).
- **Fix:** Pasang `new NoOpHubContext()` ke posisi `hubContext` constructor (broadcast no-op) + `ctrl.Url = new StubUrlHelper(...)` (Url.Action → string non-null). Tambah `using Microsoft.AspNetCore.Mvc.Routing` untuk `UrlActionContext`/`UrlRouteContext`.
- **Files modified:** `HcPortal.Tests/TokenVerifiedAtTests.cs` (test infra saja — produk tak tersentuh).
- **Verification:** Run-2 → 5/5 hijau.
- **Committed in:** `f3ccf380` (Task 3 commit).

---

**Total deviations:** 1 auto-fixed (1 blocking — test infra).
**Impact on plan:** Auto-fix murni di harness test (wiring deps yang jalur sukses butuh). Tak ada perubahan kode produk; tak ada scope creep.

## Issues Encountered

- StartExam tanpa paket me-redirect `Assessment` dengan error "tidak memiliki paket soal" (BUKAN error token) — agar T2/T5 jadi sinyal "lolos gate" yang tegas, di-seed 1 package+soal sehingga StartExam mencapai `View(vm)` (ViewResult). Resolved via `SeedPackageAsync` (pola dari `RetakeServiceTests.SeedPackageWithResponsesAsync`).

## Known Stubs

None — tidak ada stub/placeholder; semua jalur ter-wire ke kolom DB nyata.

## User Setup Required

None - no external service configuration required. **migration=TRUE** — saat promosi, notify IT: Phase 427 `AddTokenVerifiedAt` (datetime2 NULL, aditif, zero-downtime, no backfill).

## Next Phase Readiness

- **Phase 428 (EXSEC-02, write-on-GET StartExam idempotency)** siap — sama-sama edit `StartExam`; token-gate kini surgical (predikat dalam saja diubah), memudahkan merge & lapis berikutnya.
- **Merge risk (R-1):** `StartExam` = zona konflik vs `main`. Edit 427 surgical (gate inner predicate + guard utuh) — saat merge ITHandoff↔main pertahankan KEDUA (GRDF-01 setelah cek-Completed, sebelum token-gate). Migrasi `AddTokenVerifiedAt` stamp setelah semua migrasi kedua branch + snapshot regen (R-2).
- Branch ITHandoff, app port 5270 (dev). NOT pushed (deploy bundle bersama v32.1+v32.3+v32.4+v32.7+v32.8).

## Self-Check: PASSED

- Files created/modified (8/8): all FOUND.
- Task commits (5e585e99, bc1dbb63, f3ccf380): all FOUND in git log.
- DB column `AssessmentSessions.TokenVerifiedAt`: present (sqlcmd COL_LENGTH=8).
- grep `TokenVerified_` Controllers/+Services/: 0 hits.
- TokenVerifiedAtTests: 5/5 green; non-Integration suite 544/0/2 (no regression).

---
*Phase: 427-exam-token-gate-server-authoritative*
*Completed: 2026-06-24*
