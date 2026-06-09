---
phase: 356-audit-fix-assign-coach-coachee
plan: 02
subsystem: coaching-graduation
tags: [coaching, graduation, transaction, audit-fix]
requires: [356-01]
provides: [MarkMappingCompleted-transactional, AF-6-duplicate-error, AF-4-documented]
affects: [Controllers/CoachMappingController.cs]
tech-stack:
  added: []
  patterns: [transaction-cascade, specific-exception-catch]
key-files:
  created: []
  modified:
    - Controllers/CoachMappingController.cs
key-decisions:
  - "Graduate set IsActive=false + IsCompleted=true + EndDate untuk membebaskan unique-index (re-assign unit lain)"
  - "Cascade deactivate ProtonTrackAssignment (stamp DeactivatedAt), histori progress UTUH (D-04)"
  - "AF-6 catch DbUpdateException spesifik sebelum generic, no ex.Message leak"
  - "AF-4 di-defer (comment-only), 0-migration"
requirements-completed: [AF-3, AF-6, AF-4]
duration: ~15 min
completed: 2026-06-09
---

# Phase 356 Plan 02: AF-3 Graduate Transaction + AF-6 Duplicate Error + AF-4 Defer Summary

`MarkMappingCompleted` kini membungkus graduate dalam `BeginTransactionAsync`, set `IsActive=false`+`EndDate` (membebaskan unique-index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`) dan cascade deactivate `ProtonTrackAssignment` (stamp `DeactivatedAt`, histori progress utuh). `CoachCoacheeMappingAssign` menambahkan catch `DbUpdateException` spesifik (pesan ramah tanpa info-leak) sebelum catch generic. Window ±5s di `Reactivate` didokumentasikan sebagai defer (AF-4).

## Tasks
- **Task 1** (`bb361ad9`): MarkMappingCompleted transaksi + IsActive=false + cascade. Redirect+TempData dipertahankan; rollback on failure.
- **Task 2** (`b80bb797`): AF-6 catch spesifik (index name + 2601/2627) + AF-4 komentar defer.

## Verification
- `dotnet build HcPortal.csproj` → 0 error, 0 warning (incremental).
- grep: `mapping.IsActive = false` (MarkMappingCompleted), `a.DeactivatedAt = deactivationTime`, `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` present, no `RemoveRange` in MarkMappingCompleted, `RedirectToAction("CoachCoacheeMapping")` preserved; `catch (DbUpdateException dbEx) when` (1) before generic; AF-6 message present; AF-4 DEFER comment present; no `dbEx.Message`/`ex.Message` returned to user.

## Deviations from Plan
**[Environmental] Build output lock dari dev server lokal** — Found during: Task 1+2 build gate. `dotnet run` lokal (localhost:5277, PID 15356 lalu 19912) memegang `HcPortal.exe`/`HcPortal.dll` → `dotnet build` gagal di langkah copy obj→bin (MSB3027/MSB3021), BUKAN error CS (Roslyn compile 0 error). Fix: hentikan dev server lokal (`dotnet run` PID 10448 + child) → build ulang 0 error. Dev server akan di-relaunch di Plan 05 UAT (`Authentication__UseActiveDirectory=false dotnet run`). Bukan isu kode.

**Total deviations:** 1 (environmental, dev-server lock). **Impact:** nihil pada kode — semua build 0 error setelah lock dilepas.

## Issues Encountered
**AF-3 data-fix existing (RESEARCH OQ1/A2 — non-blocker, ditunda ke Plan 05):** perlu query DB lokal `SELECT Id, CoacheeId, IsActive, IsCompleted FROM CoachCoacheeMappings WHERE IsCompleted=1` untuk cek apakah ada graduated lama dengan `IsActive=1` (dari logic lama). Bila ada → one-off SQL UPDATE local-only (bukan migration) + catat SEED_JOURNAL. Bash sandbox tak bisa Named Pipes ke SQL Server; dijalankan di Plan 05 (DB/seed access). Fix kode berlaku untuk graduate baru tanpa syarat.

## Next Phase Readiness
Ready for 356-03 (AF-5/AF-7). AF-3 cascade + AF-6 race diverifikasi Plan 05 (UAT). One-off data-fix graduated-existing dievaluasi di Plan 05.
