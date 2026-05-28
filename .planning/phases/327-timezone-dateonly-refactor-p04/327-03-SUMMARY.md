---
phase: 327-timezone-dateonly-refactor-p04
plan: 03
status: complete
date: 2026-05-28
commits: [08c05c9f, 6160bec4]
---

# Plan 327-03 — SUMMARY

## One-Liner
5 file Models flip 9 property `ValidUntil`/`TanggalExpired` ke `DateOnly?` / `DateOnly` (4 input VM + 5 rollup props). DeriveCertificateStatus signature TIDAK disentuh (Plan 04 punya). NO pre-add `[DisplayFormat]` (smoke first Plan 08).

## What Was Built
| File | Property | Change | Commit |
|------|----------|--------|--------|
| `Models/CreateTrainingRecordViewModel.cs` | ValidUntil L51 | `DateTime? → DateOnly?` | `08c05c9f` |
| `Models/EditTrainingRecordViewModel.cs` | ValidUntil L53 | `DateTime? → DateOnly?` | `08c05c9f` |
| `Models/CreateManualAssessmentViewModel.cs` | ValidUntil L34 + L97 (2 occurrence) | `DateTime? → DateOnly?` | `08c05c9f` |
| `Models/CertificationManagementViewModel.cs` | SertifikatRow.ValidUntil L38 | `DateTime? → DateOnly?` | `6160bec4` |
| `Models/CertificationManagementViewModel.cs` | CertificateChainGroup.LatestValidUntil L74 | `DateTime? → DateOnly?` | `6160bec4` |
| `Models/CertificationManagementViewModel.cs` | RenewalGroup.MinValidUntil L108 | `DateTime? → DateOnly?` | `6160bec4` |
| `Models/AnalyticsDashboardViewModel.cs` | ExpiringSoonItem.TanggalExpired L68 | `DateTime → DateOnly` (non-nullable) | `6160bec4` |

## Verification
- Task 1: 9/9 grep PASS (4 new DateOnly? + 0 old + 4 [DataType] preserve + 0 DisplayFormat)
- Task 2: 9/9 grep PASS (3 new + 0 old di CertVM, 1 new + 0 old di AnalVM, DCS sig SKIP confirmed)
- `dotnet build` NOT RUN (cascade error expected Plan 04 + 05)
- xUnit Plan 01 GREEN tetap (DCS signature `DateTime?` belum di-flip)

## Threats
| ID | Status |
|----|--------|
| T-327-02 (Razor TagHelper #47628) | accept (smoke first) — NO pre-add DisplayFormat, Plan 08 browser inspect input value, retrofit Pattern F kalau bug aktif |
| T-327-04 (compile cascade) | accept — DCS signature belum di-flip → call site Controllers tetap mismatch (Plan 04+05) |

## Decisions Applied
- D-04: `[DataType(DataType.Date)]` annotation preserve ✓ (4× in CreateTraining file confirmed)
- D-07: 4 input VM flip ✓
- D-08: 5 rollup VM flip ✓ (RESEARCH Audit 2 correction nama: `ExpiringSoonItem` BUKAN `RenewalCertificateRow`)
- D-14: Pitfall 5 smoke-first (NO pre-add DisplayFormat) ✓

## Pending Downstream
- **Plan 04:** DeriveCertificateStatus signature refactor → DateOnly? + body DayNumber + helper Today + 8 xUnit GREEN kembali
- **Plan 05:** Cascade fix Controllers (CMP+CDP+Renewal+AdminBase var today sweep + RenewalController ?? DateOnly.MaxValue + GradingService cast)
- **Plan 08:** Pitfall 5 smoke + Pattern F retrofit conditional

## Commits
- `08c05c9f` — flip 4 input VM ValidUntil DateOnly?
- `6160bec4` — flip 5 rollup VM (SertifikatRow + 2 CertGroup + ExpiringSoonItem) DateOnly

## Next Plan
Plan 327-04 — TDD DeriveCertificateStatus signature DateOnly? + body DayNumber arithmetic + xUnit test helper Today refactor (8/8 GREEN kembali).
