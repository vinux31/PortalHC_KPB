---
phase: 327-timezone-dateonly-refactor-p04
plan: 02
status: complete
date: 2026-05-28
commits: [baf9427a, 3b61d2c1, f57c43fe]
---

# Plan 327-02 — SUMMARY

## One-Liner
3 file Models flip `ValidUntil DateTime? → DateOnly?` + rewrite 3 computed props (TrainingRecord.IsExpiringSoon + DaysUntilExpiry + UnifiedTrainingRecord.IsExpired) pakai DayNumber arithmetic + UtcNow alignment. Build EXPECTED gagal (cascade preventer signal per Pitfall 1).

## What Was Built
| File | Change | Commit |
|------|--------|--------|
| `Models/TrainingRecord.cs` | L43 type flip + L71-82 IsExpiringSoon rewrite + L85-95 DaysUntilExpiry rewrite (Pattern A + D-09 + D-10) | `baf9427a` |
| `Models/AssessmentSession.cs` | L65 type flip (single line) | `3b61d2c1` |
| `Models/UnifiedTrainingRecord.cs` | L26 type flip + L40 IsExpired UtcNow alignment (kill DateTime.Now 2nd tz bug) | `f57c43fe` |

## Verification
- Task 1: 5/5 grep acceptance PASS (1 DateOnly? ValidUntil + 2 UtcNow + ≥2 DayNumber + 0 old pattern + 0 old type)
- Task 2: 2/2 grep PASS (1 DateOnly? ValidUntil + 0 DateTime?)
- Task 3: 4/4 grep PASS (1 DateOnly? ValidUntil + 1 UtcNow + 0 DateTime.Now + 0 DateTime?)
- `dotnet build` NOT RUN (cascade error expected per Pitfall 1) — fix di Plan 03 (VM) + Plan 05 (Controller)
- xUnit baseline Plan 01: TIDAK terpengaruh (`CertificationManagementViewModel.cs SertifikatRow.DeriveCertificateStatus` signature belum di-flip, Plan 04 yang flip)

## Threats
| ID | Status |
|----|--------|
| T-327-04 (compile cascade) | accept — build expected fail, signal benar, cascade fix Plan 03+05 |

## Decisions Applied
- D-07: Entity flip (TrainingRecord + AssessmentSession) ✓
- D-08: Rollup flip extension (UnifiedTrainingRecord) ✓
- D-09: Today reference `DateOnly.FromDateTime(DateTime.UtcNow)` di computed props ✓ (UnifiedTrainingRecord juga, kill 2nd tz bug)
- D-10: Computed props rewrite DateOnly arithmetic ✓ (TrainingRecord IsExpiringSoon + DaysUntilExpiry)

## Pending Downstream
- **Plan 03:** Cascade flip 4 input VM + rollup SertifikatRow + LatestValidUntil + MinValidUntil + ExpiringSoonItem.TanggalExpired (per AnalyticsDashboardViewModel.cs:68 per RESEARCH Audit 2 correction)
- **Plan 04:** DeriveCertificateStatus signature refactor DateOnly? + helper Today + 8 test GREEN kembali
- **Plan 05:** Cascade fix Controllers/ (CMP+CDP+Renewal var today sweep + AdminBaseController + RenewalController ?? DateOnly.MaxValue + GradingService cast)

## Commits
- `baf9427a` — TrainingRecord ValidUntil DateOnly + DayNumber arithmetic computed props
- `3b61d2c1` — AssessmentSession ValidUntil DateOnly?
- `f57c43fe` — UnifiedTrainingRecord ValidUntil DateOnly + IsExpired UtcNow alignment

## Next Plan
Plan 327-03 — VM + Rollup Props Cascade Flip (4 input VM + 5 rollup props di CertificationManagementViewModel.cs + AnalyticsDashboardViewModel.cs).
