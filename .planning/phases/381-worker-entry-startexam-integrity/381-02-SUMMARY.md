---
phase: 381-worker-entry-startexam-integrity
plan: 02
subsystem: assessment-entry
tags: [assessment, impersonation, write-on-get, signalr, exam-entry, security]
requires: [WSE-04-type-aware-sibling]
provides: [WSE-05-impersonate-readonly, startexam-write-guard, in-memory-preview]
affects: [Controllers/CMPController.cs]
tech-stack:
  added: []
  patterns: [write-on-get-guard, build-vs-persist-split, read-only-impersonation-invariant]
key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
key-decisions:
  - "D-04: 3 write-site StartExam GET dibungkus `!_impersonationService.IsImpersonating()` (mirror precedent 911-917 Phase 377). justStarted (StartedAt/Status) + SignalR workerStarted/LogActivity → `justStarted && !IsImpersonating()`; persist assignment → `!IsImpersonating()`."
  - "D-06: build assignment (ShuffleEngine + new UserPackageAssignment) selalu jalan; HANYA persist (Add+SaveChanges) yang di-guard → admin impersonate lihat preview read-only (vm.AssignmentId=0), zero mutasi DB."
  - "D-05: VerifyToken TIDAK disentuh (hanya TempData, bukan mutasi DB). justStarted decl (968) tak dipindah (Pitfall 1) → isResume tetap benar."
requirements-completed: [WSE-05]
duration: ~20 min
completed: 2026-06-15
---

# Phase 381 Plan 02: WSE-05 Impersonation Read-Only Guard Summary

Admin yang impersonate worker X lalu membuka `StartExam` ujian X (Open, StartedAt==null) tidak lagi memutasi state worker: tiga write-site di `StartExam` GET kini ter-guard `!_impersonationService.IsImpersonating()` (mirror precedent Phase 377 di 911-917). `StartedAt`/`Status=InProgress` tak ditulis, SignalR `workerStarted` + `LogActivity("started")` tak fire, dan `UserPackageAssignment` tak dipersist. Admin tetap melihat **preview soal read-only**: assignment dibangun in-memory (ShuffleEngine) tanpa `_context.Add`/`SaveChanges`. Saat worker asli login & StartExam → barulah StartedAt ter-set + assignment ter-persist (SC#3 deferred-start).

## Execution

- **Duration:** ~20 min · **Tasks:** 2 · **Files:** 1 (`CMPController.cs`)
- **Commit:** `f274d231` (WSE-05 = satu fix koheren OPS-01/TOK-03 → satu commit, per arahan plan)

### Task 1 — Guard write-site 1 + 2
`if (justStarted)` → `if (justStarted && !_impersonationService.IsImpersonating())` pada (1) blok StartedAt/Status=InProgress dan (2) blok SignalR `workerStarted` + `LogActivityAsync("started")`. `bool justStarted = assessment.StartedAt == null;` (968) DIBIARKAN di posisi pra-guard (Pitfall 1) — saat impersonate `StartedAt` null → `justStarted=true` tapi guard cegah write; `isResume=!justStarted=false` → blok SavedAnswers tak fire (preview soal bersih, D-07). Empty-package pre-check (Phase 380, `preCheckSiblingIds`) & VerifyToken (D-05) tak disentuh.

### Task 2 — Guard write-site 3 + in-memory preview (D-06)
Blok `if (assignment == null)`: build (rng → `BuildQuestionAssignment` → `assignedQuestions` → `BuildOptionShuffle` → sentinel → `new UserPackageAssignment{ UserId = user.Id, ... }` → `SavedQuestionCount`) DIBIARKAN tanpa guard (jalan untuk worker asli & impersonate). Hanya PERSIST (`_context.UserPackageAssignments.Add` + `SaveChangesAsync` + race-recovery catch) yang dibungkus `if (!_impersonationService.IsImpersonating())`. Konsekuensi terverifikasi aman: saat impersonate `assignment.Id==0` → view-build downstream hanya baca `GetShuffledQuestionIds()` + field string (no DB re-read) → preview render OK (UAT runtime di Plan 03). RNG seed dibiarkan `Random.Shared` (diskresi; preview acak per-refresh tetap valid).

## Verification

- `dotnet build` — Build succeeded (0 errors).
- `dotnet test HcPortal.Tests` — **391 passed, 0 failed** (no regression).
- Grep: `justStarted && !_impersonationService.IsImpersonating()` = 2 (write-site 1+2).
- Grep: `!_impersonationService.IsImpersonating` total = 4 (precedent 912 + 2 write-site + 1 persist).
- Grep: `bool justStarted = assessment.StartedAt == null;` = 1 (tak dipindah).
- VerifyToken method TIDAK mengandung `IsImpersonating` (D-05). `preCheckSiblingIds` intact.
- **No migration:** hanya control-flow guard di controller — no Model/DbContext/Migrations diff.

## Deviations from Plan

None — plan executed exactly as written.

## Manual-Only Verification (carry to Plan 03 / UAT)

- Preview render saat impersonate (`vm.AssignmentId=0`) — Razor dynamic runtime, grep+build tak cukup (lesson 354/355). Verifikasi headed Playwright di Plan 03 Task 3 Bagian A: impersonate buka StartExam Open belum-mulai → render soal tanpa NRE/500 + DB no-mutation assert.

## Next

Ready for Plan 381-03 (Wave 3, **autonomous: false** — checkpoint). e2e #4 (PrePost pool-only) + #7 (impersonate read-only + deferred-start) + UAT render/migration gate.

## Self-Check: PASSED
- key-files exist on disk: ✓ (CMPController.cs)
- `git log --grep="381-02"` returns 1 commit: ✓ (f274d231)
- All acceptance criteria re-run green: ✓
- Plan-level verification (build 0 err, xUnit 391/391, no migration, VerifyToken untouched): ✓
