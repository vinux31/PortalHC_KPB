---
phase: 309
plan: 03
subsystem: assessment-grading
tags:
  - refactor
  - constants
  - grading-service
  - opportunistic-fix
dependency_graph:
  requires:
    - "309-02 (AssessmentConstants.AssessmentStatus.PendingGrading constant introduction)"
  provides:
    - "Services/GradingService.cs internal call sites pakai AssessmentConstants — drift-free dengan Plan 309-02 constant introduction"
  affects:
    - "Phase 310 FinalizeEssayGrading future plan: tinggal reference constant tanpa risk drift literal vs constant"
tech-stack:
  added: []
  patterns:
    - "Compile-time const string referencing untuk EF Core LINQ-to-SQL Where/SetProperty (IL bytecode equivalent dengan literal — translation identik)"
key-files:
  created: []
  modified:
    - "Services/GradingService.cs (L196 Where clause + L199 SetProperty: 4 literal swap ke AssessmentConstants)"
decisions:
  - "Refactor literal `\"Menunggu Penilaian\"` L196 + L199 ke AssessmentConstants.AssessmentStatus.PendingGrading constant (per OQ#2 RESOLVED iter-1)"
  - "Refactor literal `\"Completed\"` L196 IKUT ke AssessmentConstants.AssessmentStatus.Completed untuk konsistensi (eliminate typo + drift risk di line yang sama)"
  - "TIDAK refactor literal substring `Menunggu Penilaian` di log messages L209/L223 — out of scope (CONTEXT D-06 strict 3-lokasi rollout, log narasi bukan status assignment)"
  - "TIDAK refactor literal `\"Completed\"` di line LAIN GradingService.cs (e.g., L232 non-essay flow guard) — out of scope; opportunistic refactor terbatas pada line yang BERSAMAAN dengan PendingGrading literal saja"
metrics:
  duration: "~2 menit (single-task autonomous, 4 literal swap di 2 baris)"
  completed: "2026-05-01"
  tasks: 1
  files_modified: 1
  commits: 1
---

# Phase 309 Plan 03: GradingService Opportunistic Constant Refactor Summary

Opportunistic refactor `Services/GradingService.cs` line 196 (Where clause) + line 199 (SetProperty) — swap 4 literal string (`"Completed"` + `"Menunggu Penilaian"` di L196, `"Menunggu Penilaian"` di L199) ke `AssessmentConstants.AssessmentStatus` constants yang di-introduce di Plan 309-02 Task 1, untuk eliminate typo + drift risk di milestone berikutnya (Phase 310 FinalizeEssayGrading).

## Tasks Executed

| Task | Name                                                                              | Commit   | Files                       |
| ---- | --------------------------------------------------------------------------------- | -------- | --------------------------- |
| 1    | GradingService.cs opportunistic refactor literal status ke AssessmentConstants    | a06519b7 | `Services/GradingService.cs` |

## Decisions Made

1. **Refactor scope strict line 196 + 199 saja**: opportunistic refactor TIDAK dirambah ke literal `"Completed"` di line lain GradingService.cs (e.g., non-essay flow guard L232) — CONTEXT D-06 strict 3-lokasi rollout per phase. Plan 309 mengontrol PendingGrading flow; literal "Completed" lain dibiarkan untuk milestone berikutnya jika dianggap perlu refactor.

2. **Literal substring `Menunggu Penilaian` di log messages L209/L223 tetap apa adanya**: log narasi (`"GradingService: race condition session {SessionId} — sudah Completed/Menunggu Penilaian."` dan `"GradingService: session {SessionId} status Menunggu Penilaian — ..."`) bukan status assignment ke property — mereka adalah human-readable log narrative. Refactor log message berisiko mengurangi readability tanpa benefit konsistensi (constant value sama dengan substring narrative). Out of scope per task action specific instruction #2.

3. **Refactor `"Completed"` L196 ikut diswap**: line 196 Where clause punya 2 literal (`"Completed"` + `"Menunggu Penilaian"`); konsisten kalau keduanya pakai constant — eliminate typo + drift risk per OQ#2 decision iter-1.

## Compile Result

- `dotnet build` exit code: **0** (success, no errors)
- Warning count: **92** (memenuhi acceptance ≤ 92, baseline Phase 308 maintained)
- Time elapsed: ~30s

## Acceptance Criteria Verification

| Criterion | Expected | Actual | Status |
|-----------|----------|--------|--------|
| `dotnet build` exit | 0 | 0 | PASS |
| Warning count | ≤ 92 | 92 | PASS |
| `grep -F '"Menunggu Penilaian"' Services/GradingService.cs` literal **assignment** | 0 | 0 (1 hit di comment L189 non-code) | PASS |
| `grep -c 'AssessmentConstants.AssessmentStatus.PendingGrading'` | ≥ 2 | 2 (L196 + L199) | PASS |
| `grep -F 'AssessmentConstants.AssessmentStatus.Completed'` | ≥ 1 | 1 (L196) | PASS |
| `grep -F 'ExecuteUpdateAsync'` preserved | ≥ 1 | 7 | PASS |
| `grep -c 'interimPercentage'` preserved | ≥ 1 | 2 | PASS |
| `grep -F 'IsPassed, (bool?)null'` preserved | ≥ 1 | 1 | PASS |
| `grep -F 'Progress, 100'` preserved | ≥ 1 | 2 | PASS |
| `grep -F 'CompletedAt, DateTime.UtcNow'` preserved | ≥ 1 | 2 | PASS |
| `grep -F 'using HcPortal.Models;'` | ≥ 1 | 1 (L3) | PASS |

**Catatan grep `"Menunggu Penilaian"` literal assignment = 0**:
Hit yang muncul (L189) adalah dalam C# comment block (`// ---- 3a. Essay flow: status "Menunggu Penilaian", tidak generate sertifikat/TrainingRecord ----`) — BUKAN kode runtime. Code assignment (L196 Where, L199 SetProperty) sudah refactored ke constant. Log messages L209/L223 menggunakan substring `Menunggu Penilaian` di interpolated narrative tanpa quoted exact-match `"Menunggu Penilaian"` (substring di tengah string log), sehingga grep `-F '"Menunggu Penilaian"'` (exact dengan quotes) tidak match log lines — hanya match comment line L189. Acceptance "0 hit literal eliminated dari assignment" tercapai bersih.

## Deviations from Plan

**None — plan executed exactly as written.**

Plan single-task autonomous, 4 literal swap di 2 baris, dependency Plan 309-02 sudah complete (PendingGrading constant available di `Models/AssessmentConstants.cs` L18). Pre-checks (using directive ada, konstanta ada) lulus tanpa modifikasi tambahan. Build sukses 0 error 92 warning (baseline preserved).

## Files Changed

**Modified (1):**
- `Services/GradingService.cs` — 4 literal swap di L196 + L199 (2 insertions, 2 deletions per `git diff --stat`)

**Created (0):** none.
**Deleted (0):** none.

## Commits Created

| Hash     | Type     | Message                                                                          |
|----------|----------|----------------------------------------------------------------------------------|
| a06519b7 | refactor | refactor(309-03): swap GradingService literal status ke AssessmentConstants     |

## Phase 309 Progress

- **Plan 309-01**: Complete (per phase tracking).
- **Plan 309-02**: Complete (commit 3e3dc935 per recent log; PendingGrading constant introduced).
- **Plan 309-03**: Complete (this plan, commit a06519b7).

**Phase 309 status**: 3/3 plans complete — ready untuk `/gsd-verify-work` closure di main branch setelah orchestrator merge worktrees Wave 2.

## Phase 310 Dependency

`Phase 310 FinalizeEssayGrading` sekarang dapat reference `AssessmentConstants.AssessmentStatus.PendingGrading` constant tanpa risk drift literal vs constant — semua call site internal di `Services/GradingService.cs` sudah konsisten pakai constant. Future plan tinggal `using HcPortal.Models;` (sudah standard di Services/) dan reference constant — IL bytecode equivalent dengan literal string `"Menunggu Penilaian"` di EF Core LINQ-to-SQL translation, no runtime behavior change.

## Threat Model Compliance

Threat T-309-08 (Tampering — refactor literal ke constant di Where clause status guard): **disposition `accept` — mitigation memenuhi**.

- Compile-time `const string` equivalent dengan literal `"Menunggu Penilaian"` di IL bytecode (Roslyn compiler inlines const value).
- EF Core LINQ-to-SQL translation IDENTIK pre/post (Where clause SQL `WHERE Status != 'Completed' AND Status != 'Menunggu Penilaian'` produced sama).
- No race condition surface introduced — refactor tidak ubah ExecuteUpdateAsync atomicity guard (Where clause status check tetap atomic).
- Wave 2 sequential setelah 309-02 ensures PendingGrading constant available di codebase saat refactor diterapkan (verified via grep pre-check).

## Known Stubs

**None.** Refactor terbatas swap literal ke constant — tidak introduce empty/placeholder data atau TODO marker. Existing logic interim score + IsPassed null + Progress 100 + CompletedAt fully preserved.

## Self-Check: PASSED

- File `Services/GradingService.cs` exists dan menggunakan `AssessmentConstants.AssessmentStatus.PendingGrading` (L196 + L199) + `AssessmentConstants.AssessmentStatus.Completed` (L196): verified via grep + Read tool.
- Commit `a06519b7` exists di git log: verified via `git rev-parse`.
- File SUMMARY.md ini akan di-commit di metadata commit step (post-write).
