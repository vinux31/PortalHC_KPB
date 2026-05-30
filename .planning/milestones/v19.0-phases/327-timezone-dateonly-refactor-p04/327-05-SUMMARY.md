---
phase: 327-timezone-dateonly-refactor-p04
plan: 05
status: complete
date: 2026-05-28
commits: [c7adcb73, 24912e33, 37712c76]
deviations: [hybrid-per-site-classification, importtraining-bonus-pull-forward]
---

# Plan 327-05 — SUMMARY

## One-Liner
Cascade fix Controllers + Services. **Build SUKSES 0 Error. 18/18 test GREEN.** Plan 05 selesai dengan 2 deviation dari plan literal: (1) hybrid per-site classification var today (10 CMP + 3 Home), (2) ImportTraining 2-site pull-forward dari Plan 06 untuk unblock build acceptance.

## What Was Built (3 commit, 6 file touched)

### Commit 1 `c7adcb73`: var today hybrid sweep (CMP+Home)
| File | Method | Approach |
|------|--------|----------|
| CMPController | @2975 GetExpiringSoonData | Full flip DateOnly (ValidUntil-only) |
| CMPController | @2517 GetCertificateAnalytics | Hybrid (keep today DateTime + add todayDate DateOnly + replace L2598/L2621/L2592) |
| CMPController | @2740 GetAnalyticsSummary | Hybrid (same pattern + replace L2763/L2765/L2767) |
| CMPController | @2821, @2856, @2935, @3029, @3057, @3105, @3152 | NO TOUCH (7 method periodeEnd-only) |
| HomeController | @78 TriggerCertExpiredNotificationsAsync | Full flip (ValidUntil-only) |
| HomeController | @164 GetCertAlertCountsAsync | Full flip (ValidUntil-only) |
| HomeController | @277 GetUpcomingEvents | NO TOUCH (CoachingSession.Date/AssessmentSession.Schedule) |
| CDPController | L2187 | NO TOUCH (coaching deliverable out-of-scope per RESEARCH MED-03) |

**Grep verify:** CMP 1 today DateOnly + 2 todayDate hybrid + 9 today DateTime preserve; Home 2 today DateOnly + 1 today DateTime preserve; CDP 1 today DateTime preserve.

### Commit 2 `24912e33`: ?? DateOnly.MaxValue (Renewal+AdminBase)
| File | Sites |
|------|-------|
| RenewalController.cs | 7 (L189, L190, L201, L263, L281, L289, L339) |
| AdminBaseController.cs | 1 (L201) |

**Deviation:** RESEARCH Audit 4 said 2 AdminBase sites tapi grep actual 1 (L131 plain assign, no fallback).

### Commit 3 `37712c76`: GradingService cast + ImportTraining bonus
| File | Line | Change |
|------|------|--------|
| GradingService.cs | L465 | `(DateTime?)null` → `(DateOnly?)null` (SetProperty cast) |
| GradingService.cs | L488 | `certNow.AddYears(3)` → `DateOnly.FromDateTime(certNow).AddYears(3)` (DateOnly wrap) |
| TrainingAdminController.cs | L1037 | ImportTraining Pattern D — Plan 06 scope pulled forward untuk build acceptance |
| TrainingAdminController.cs | L1138 | Same Pattern D |

## Verification
- `dotnet build HcPortal.sln --nologo` → **0 Error, 23 warnings unrelated (pre-existing nullability hints)**, 31.96s
- `dotnet test HcPortal.sln --nologo` → **Passed 18/0, 123 ms** (10 Phase 325 FileUploadHelper + 8 Phase 327 CertificateStatusTests)
- xUnit baseline regression: 8/8 PASS — Phase 04 GREEN confirmed retroactive (test runtime verify deferred di Plan 04 SUMMARY sekarang validated)

## Threats
| ID | Status |
|----|--------|
| T-327-04 (compile cascade) | **MITIGATED COMPLETE** — build sukses, test 18/18 GREEN |

## Decisions Applied
- D-09: `DateOnly.FromDateTime(DateTime.UtcNow)` standardize ✓
- D-13: ImportTraining cast `DateOnly.FromDateTime(vu)` pattern ✓ (pulled forward from Plan 06)

## Deviations (intentional, audited)

### Deviation 1: Hybrid per-site classification (CMP var today)
**Reason:** Plan 05 literal said "13 var today sweep DateOnly" but 6 CMP methods use `today` for `periodeEnd ??= today` (DateTime context, NOT ValidUntil). Blind sweep akan break compile.
**Resolution:** Per-site analysis → 1 full flip + 2 hybrid + 7 NO TOUCH. User approved "Hybrid minimal touch per site" via AskUserQuestion.
**Impact:** Plan acceptance grep adjust di SUMMARY (10 → 1 DateOnly + 2 todayDate + 9 preserve). Build SUKSES validates correctness.

### Deviation 2: ImportTraining bonus pull-forward
**Reason:** Plan 05 acceptance requires `dotnet build` 0 Error. Post-Plan-05-literal-scope build masih 2 error di TrainingAdminController L1037 + L1138 (ImportTraining ValidUntil DateTime → DateOnly mismatch). Plan 06 first task akan fix exact ini.
**Resolution:** Apply Pattern D cast sekarang. Plan 06 sisa kerja = Razor TagHelper smoke + DisplayFormat fallback conditional (no compile concern).
**Impact:** Plan 06 task 1 effectively done. Plan 06 SUMMARY akan note bonus complete.

## Pending Downstream
- **Plan 06:** Razor TagHelper #47628 smoke browser inspect + `[DisplayFormat]` fallback retrofit conditional (kalau bug aktif di .NET 8.0.418)
- **Plan 07:** EF migration generate + sqlcmd pre-check + apply + JSON API audit
- **Plan 08:** Manual UAT 7 SC + JSON timezone Pitfall 3 smoke + Phase 326 regression smoke + IT_NOTIFY.md draft

## Commits
- `c7adcb73` — sweep var today DateOnly hybrid per-site (CMP+Home)
- `24912e33` — sweep ?? DateOnly.MaxValue fallback (Renewal 7 + AdminBase 1)
- `37712c76` — GradingService cast DateOnly + ImportTraining bonus fix (build SUKSES)

## Next Plan
Plan 327-06 — ImportTraining (bonus done) + Razor TagHelper #47628 smoke verify + conditional DisplayFormat retrofit ke 4 input VM.
