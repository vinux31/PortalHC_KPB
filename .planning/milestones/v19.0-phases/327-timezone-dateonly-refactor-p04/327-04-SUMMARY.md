---
phase: 327-timezone-dateonly-refactor-p04
plan: 04
status: complete (runtime test verify deferred Plan 05)
date: 2026-05-28
commits: [89341b35, 8156e34b]
---

# Plan 327-04 — SUMMARY

## One-Liner
TDD RED→GREEN: DeriveCertificateStatus signature `DateTime?` → `DateOnly?` + body DayNumber arithmetic. xUnit helper Today() flip return DateOnly. Code-level acceptance 8/8 grep PASS. Runtime test DEFER Plan 05 (test project depends main HcPortal.csproj DLL yang cascade compile error).

## What Was Built
| File | Change | Commit |
|------|--------|--------|
| `Models/CertificationManagementViewModel.cs` L48-63 | Signature DateOnly? + body DayNumber + UtcNow per D-06 | `89341b35` |
| `HcPortal.Tests/CertificateStatusTests.cs` L17-19 | Helper Today return DateOnly + AddDays formula | `8156e34b` |

## Verification
- **Task 1 RED (signature refactor) code grep:** 5/5 PASS (1 DateOnly? + 0 DateTime? + 1 UtcNow + 2 DayNumber + 0 old pattern)
- **Task 2 GREEN (helper update) code grep:** 3/3 PASS (1 DateOnly Today + 1 FromDateTime.AddDays + 0 DateTime Today)
- **Runtime test:** DEFERRED Plan 05
  - Main project `HcPortal.csproj` build fails 10 site di CMPController L2621, L2622, L2762, L2764, L2981, L3000 — query expr `var today` + `t.ValidUntil >= today` DateOnly?/DateTime mismatch
  - Test project depends main project DLL → can't standalone build either (CertificateStatusTests.cs L27 `Today(offset)` DateOnly vs old DLL `DateTime?` signature mismatch since DLL stale)
  - Plan 05 cascade fix akan unblock dotnet test → 8/8 GREEN target

## Threats
| ID | Status |
|----|--------|
| T-327-04 (compile cascade) | accept partial — Plan 05 next unblock test runtime verify. Test compile error exact match expected per Pitfall 1 (`Cannot apply >= to DateOnly and DateTime`). |

## Decisions Applied
- D-06: DeriveCertificateStatus signature DateOnly? + body DayNumber arithmetic + UtcNow ✓
- D-14: xUnit + Assert vanilla helper pattern preserved (no FluentAssertions) ✓
- D-09: today = `DateOnly.FromDateTime(DateTime.UtcNow)` ✓

## Build State Snapshot (post Plan 04)
- `dotnet build HcPortal.csproj` → FAIL, 10 errors di CMPController (cascade Plan 05)
- `dotnet build HcPortal.Tests/HcPortal.Tests.csproj --no-dependencies` → FAIL, 1 error CertificateStatusTests.cs:27 (stale main DLL)
- `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` → FAIL (no compiled test DLL)

**Expected GREEN:** Setelah Plan 05 commit, full build + test re-run 18/18 (10 Phase 325 + 8 Phase 327).

## Pending Downstream
- **Plan 05 CRITICAL:** Cascade fix Controllers (CMP+CDP+Renewal+AdminBase var today sweep + RenewalController ?? DateOnly.MaxValue + GradingService cast). 10 site CMPController explicit visible di error output.

## Commits
- `89341b35` — DeriveCertificateStatus signature DateOnly? + DayNumber body (RED)
- `8156e34b` — CertificateStatusTests helper Today return DateOnly (GREEN code-level)

## Next Plan
Plan 327-05 — Cascade fix Controllers (CMP+CDP+Renewal+AdminBase var today + RenewalController DateOnly.MaxValue + GradingService cast). Build SUKSES + xUnit 18/18 GREEN target.
