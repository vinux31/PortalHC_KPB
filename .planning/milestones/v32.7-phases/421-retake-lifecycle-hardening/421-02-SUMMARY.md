---
phase: 421-retake-lifecycle-hardening
plan: 02
subsystem: assessment-retake
tags: [retake, counting, snapshot-presence, kill-drift, hardening]
requires: [RetakeService, CMPController, AssessmentAdminController]
provides: [retake-counting-single-source, snapshot-aware-warning]
affects: [Helpers/RetakeCountingRules.cs, Services/RetakeService.cs, Controllers/CMPController.cs, Controllers/AssessmentAdminController.cs]
tech-stack:
  added: []
  patterns: [pure-rules-kill-drift, snapshot-presence-counting, db-aware-helper]
key-files:
  created:
    - Helpers/RetakeCountingRules.cs
    - HcPortal.Tests/RetakeCountingRulesTests.cs
  modified:
    - Services/RetakeService.cs
    - Controllers/CMPController.cs
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Helper DB-aware (terima ApplicationDbContext) — bukan pure seperti ShuffleToggleRules — agar EF translate predikat snapshot-presence byte-identik dengan inline existing."
  - "Dua bentuk (Pitfall 3, JANGAN collapse): CountForUserAsync (cap per-user a/b/c) + MaxInGroupAsync (warning d max-in-group GroupBy)."
  - "TANPA +1 di helper — caller tambah current-attempt (+1) di tiap situs (semantik arsip-vs-percobaan eksplisit)."
  - "Situs warning ManagePackages (:5795) = SATU-SATUNYA yang berubah perilaku → kini snapshot-aware (legacy pre-v32.4 excluded). 3 situs cap nilai identik (regresi hijau)."
requirements-completed: [RTH-03]
duration: 1 sesi
completed: 2026-06-23
---

# Phase 421 Plan 02: Counting Helper Summary

Satukan penghitungan percobaan era-retake ke satu sumber `RetakeCountingRules` (snapshot-presence), perbaiki situs warning ManagePackages yang over-count (legacy HC-reset pre-v32.4). Kill-drift permanen (RTK-LOGIC-03).

**Durasi:** 1 sesi · **Task:** 2 · **File:** 5 (2 baru + 3 modifikasi).

## Yang dibangun

- **Task 1 (helper + parity test):** `Helpers/RetakeCountingRules.cs` — `EraRetakeBase` (predikat snapshot-presence `archive.Any(a => a.AttemptHistoryId == h.Id)` verbatim) + dua bentuk konsumsi `CountForUserAsync` (per-user cap) & `MaxInGroupAsync` (max-in-group warning, GroupBy dipertahankan). TANPA `+1`. 5 test (`RetakeCountingRulesTests`, real-SQL reuse `RetakeServiceFixture`): per-user count / legacy-excluded / max-in-group / parity cap==warning filter / empty-group guard → **5/5 hijau**.
- **Task 2 (wire 4 situs):** 3 situs cap → `CountForUserAsync` (RetakeService `ExecuteAsync` + `CanRetakeAsync`, CMP Results VM) — nilai numerik IDENTIK (predikat tak berubah). 1 situs warning DIVERGEN `AssessmentAdminController:5795` → `MaxInGroupAsync` — **PERBAIKAN**: tambah filter snapshot-presence (legacy excluded) sambil pertahankan `GroupBy(UserId)` max + `+1`. **0 predikat inline `AttemptHistoryId` tersisa** di 4 call-site (kill-drift).

## Verifikasi

- `dotnet test ~RetakeCountingRules` → **5/5** (parity + legacy-excluded + max-in-group + empty-guard).
- `dotnet test ~RetakeService` → **10/10** (regresi cap hijau — nilai cap tak berubah).
- `dotnet build` → **0 error**.
- Grep: RetakeService helper 2 wire, CMP 1, AssessmentAdmin `MaxInGroupAsync`; `ViewBag.RetakeMaxAttemptsUsedInGroup = ... + 1` dipertahankan; **0 stray inline snapshot predikat** di 4 call-site.

## Deviations from Plan

None - plan executed exactly as written. (Helper di-desain DB-aware terima `ApplicationDbContext` per saran interface plan — bukan deviasi.)

## Self-Check: PASSED

- key-files created (`RetakeCountingRules.cs` + test) ada di disk + modified files ter-commit (2 commit 421-02).
- Acceptance criteria 2 task semua PASS (grep + test exit 0).
- Verification re-run hijau (5 + 10, build 0-err); 0 inline-predikat drift.

## Issues Encountered

None.

## Next

Ready for **421-03** (RTH-04 delete-guard + RTH-05 MaxAttempts confirm + D-02 + D-04). Plan 03 reuse `MaxInGroupAsync` untuk used-count RTH-05 (jangan redefinisi).
