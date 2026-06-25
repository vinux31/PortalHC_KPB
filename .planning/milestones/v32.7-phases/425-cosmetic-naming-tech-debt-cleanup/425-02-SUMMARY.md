---
phase: 425-cosmetic-naming-tech-debt-cleanup
plan: 02
subsystem: assessment-timing
tags: [refactor, timer, exam-time-rules, parity-test, tech-debt, cln-04]

# Dependency graph
requires:
  - phase: 424-grading-dedup-flow-gating
    provides: ExamTimeRules.AllowedExamSeconds helper (single-source durasi timer)
provides:
  - "4 situs formula timer CMPController dikonsolidasi ke ExamTimeRules.AllowedExamSeconds (single-source)"
  - "Parity tests ExamTimeRulesTests.cs membuktikan formula lama == helper (incl double-site)"
affects: [425-03, 425-04, milestone-audit-v32.7]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Konsolidasi formula berulang ke pure-helper single-source + parity test (lanjutan pola 424)"
    - "Pertahankan variabel intermediate (allowedMinutes) bila masih dipakai konsumen lain (audit)"

key-files:
  created: []
  modified:
    - HcPortal.Tests/ExamTimeRulesTests.cs
    - Controllers/CMPController.cs

key-decisions:
  - "4 situs (:1191/:1564/:1642 detik + :4661 menit->detik double) → ExamTimeRules.AllowedExamSeconds; paritas numerik identik"
  - "D-03 honored: token gate (TempData.Peek/FLOW-08) + write-on-GET StartExam (FLOW-10) TIDAK disentuh (defer backlog)"
  - "allowedMinutes :4661 dipertahankan — dipakai WriteSubmitBlockedAuditAsync (audit menit), bukan dead variable"

patterns-established:
  - "Pattern: ganti formula inline ke helper HANYA setelah parity test (Wave-0) buktikan identik"
  - "Pattern: konsolidasi non-destruktif — tidak menyentuh perilaku adjacent (token/grace/audit)"

requirements-completed: [CLN-04]

# Metrics
duration: recovery-finalized
completed: 2026-06-24
---

# Phase 425 Plan 02: Timer Consolidation (CLN-04) Summary

**Konsolidasi 4 situs formula durasi timer berulang `(DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60` ke single-source `ExamTimeRules.AllowedExamSeconds(...)` (helper Phase 424). Paritas numerik IDENTIK dibuktikan parity test. No migration, no behavior-change. Token gate + write-on-GET StartExam (D-03) di-defer, tidak disentuh.**

## Accomplishments
- **CLN-04 (FLOW-09):** 4 situs timer di `CMPController` dikonsolidasi ke `ExamTimeRules.AllowedExamSeconds`:
  - `:1191` `durationSeconds` (int, ExamSummary resume state)
  - `:1564` `allowed` (timerExpired check)
  - `:1642` `allowed` (serverTimerExpired check)
  - `:4661` `allowedSec` (double, auto-submit grace path) — `int -> double`, identik `allowedMinutes * 60.0`
- **Parity test (Wave-0):** `ExamTimeRulesTests.cs` di-EXTEND dengan kasus parity yang membuktikan formula lama `(D + (E ?? 0)) * 60` == `AllowedExamSeconds(D, E)` di 4 situs termasuk double-site. Suite naik 748 → 754 passed (+6 test parity).
- **D-03 honored:** Token gate `TempData` (FLOW-08) + write-on-GET StartExam (FLOW-10) TIDAK diubah. `allowedMinutes` `:4661` dipertahankan karena masih dipakai `WriteSubmitBlockedAuditAsync` (audit dalam satuan menit).

## Task Commits
1. **Task 1 (Wave-0): parity tests ExamTimeRules** — `2e87cb0f` (test)
2. **Task 2: konsolidasi 4 situs CMPController → AllowedExamSeconds** — `81e0ee99` (refactor)

## Files Created/Modified
- `HcPortal.Tests/ExamTimeRulesTests.cs` — parity tests 4 situs (incl double-site), Wave-0 sebelum refactor call-site
- `Controllers/CMPController.cs` — 5 baris diganti (4 situs timer; `:4661` allowedMinutes diberi komentar audit-usage)

## Decisions Made
- **Paritas dulu, baru refactor:** parity test ditulis & hijau sebelum call-site CMPController diubah (TDD Wave-0).
- **allowedMinutes dipertahankan:** bukan dead variable — konsumen audit menit. Hanya `allowedSec` yang dialihkan ke helper.
- **Defer FLOW-08/FLOW-10 (D-03):** token gate + write-on-GET di luar scope cleanup low-risk; tetap backlog.

## Deviations from Plan
None — eksekusi sesuai rencana. Catatan proses: executor agent original terputus (API connection closed) setelah Task 1 commit + edit Task 2 di working tree tetapi sebelum commit Task 2/finalize. Orchestrator melakukan recovery: verifikasi diff bersih (4 situs benar, token gate utuh), `dotnet build` 0 error, full suite 754/0/2, lalu commit Task 2 (`81e0ee99`) + finalize SUMMARY/STATE/ROADMAP. Tidak ada perubahan kode tambahan di luar yang sudah dikerjakan executor.

## Issues Encountered
- Executor agent original terputus mid-finalize (API Error: Connection closed). Pekerjaan kode Task 2 sudah lengkap & benar di working tree; di-recover dan di-commit oleh orchestrator setelah verifikasi penuh (diff review + build + full suite).

## Known Stubs
None.

## User Setup Required
None. migration=FALSE (tidak ada migration untuk dijalankan IT).

## Next Phase Readiness
- **Verifikasi:** `dotnet build` 0 error (24 warning baseline, tak bertambah); full suite **754/0/2** (748 baseline + 6 parity, 0 regresi); diff hanya 2 file (test + CMPController); tidak ada file Migrations/ baru.
- **Untuk Plan 425-03:** `TrainingAdminController` / `AddManualAssessment.cshtml` belum disentuh plan ini.
- **migration=FALSE** konsisten untuk notify IT.

## Self-Check: PASSED

**Files verified (2/2 exist):**
- FOUND: HcPortal.Tests/ExamTimeRulesTests.cs
- FOUND: Controllers/CMPController.cs

**Commits verified (2/2 exist):**
- FOUND: 2e87cb0f (Task 1 parity tests)
- FOUND: 81e0ee99 (Task 2 consolidation)

---
*Phase: 425-cosmetic-naming-tech-debt-cleanup*
*Completed: 2026-06-24 (recovery-finalized by orchestrator)*
