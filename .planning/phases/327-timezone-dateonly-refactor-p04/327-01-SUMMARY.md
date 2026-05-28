---
phase: 327-timezone-dateonly-refactor-p04
plan: 01
status: complete
date: 2026-05-28
commit: 148add50
---

# Plan 327-01 — SUMMARY

## One-Liner
xUnit baseline `CertificateStatusTests.cs` 8 case GREEN pakai signature existing `DateTime?` (Wave 0 foundation, pre-refactor safety net Plan 04).

## What Was Built
- File baru: `HcPortal.Tests/CertificateStatusTests.cs` (45 baris)
- 1 Theory method dengan 6 InlineData + 2 Fact method
- Helper `Today(int offset)` return `DateTime.UtcNow.Date.AddDays(offset)`
- Vanilla `Assert.Equal` (zero FluentAssertions per D-14)
- File-scoped namespace `HcPortal.Tests` (mirror FileUploadHelperTests.cs)
- Komentar BI top file

## Test Coverage (8 case)
| # | Type | Scenario | Expected | Result |
|---|------|----------|----------|--------|
| 1 | Theory | today+100, "Annual" | Aktif | PASS |
| 2 | Theory | today+30, "Annual" | AkanExpired (boundary) | PASS |
| 3 | Theory | today+1, "Annual" | AkanExpired | PASS |
| 4 | Theory | today (offset 0), "Annual" | AkanExpired (days=0) | PASS |
| 5 | Theory | today-1, "Annual" | Expired | PASS |
| 6 | Theory | today+100, "Permanent" | Permanent (override) | PASS |
| 7 | Fact | null, null | Expired | PASS |
| 8 | Fact | null, "Permanent" | Permanent | PASS |

## Verification
- `dotnet test --filter "FullyQualifiedName~CertificateStatusTests"` → **Passed 8/0, 602 ms**
- `dotnet test HcPortal.sln` (full suite) → **Passed 18/0, 151 ms** (10 Phase 325 FileUploadHelper + 8 Phase 327)
- Zero modifikasi production code (only `HcPortal.Tests/CertificateStatusTests.cs`)
- Acceptance criteria 8/8 grep+test verify PASS

## Sampling Baseline (Nyquist Dim 8)
- Quick: 8 test ~600ms (per-task commit gate)
- Full suite: 18 test ~150ms (per-wave gate)
- VALIDATION.md `wave_0_complete` → flip true post-Plan 01 (deferred admin cleanup)

## Threats
| ID | Status |
|----|--------|
| T-327-04 (compile cascade) | mitigate — baseline established sebelum Plan 02 break anything |
| T-327-05 (Phase 326 regression) | accept — zero Controller/ touch |

## Decisions Applied
- D-14: xUnit + Assert vanilla pattern Phase 325 match (no FluentAssertions enforced via grep acceptance)
- D-09: Today reference `DateTime.UtcNow.Date` baseline (Plan 04 flip ke DateOnly.FromDateTime(DateTime.UtcNow))

## Pending Downstream
- Plan 04 refactor signature → DateOnly? + update helper Today return DateOnly + 8 test GREEN kembali

## Commit
`148add50` — `test(327-01): add CertificateStatusTests 8 baseline cases pre-refactor`

## Next Plan
Plan 327-02 — Entity flip TrainingRecord.cs + AssessmentSession.cs + UnifiedTrainingRecord.cs ValidUntil DateOnly? + computed props DayNumber rewrite + UtcNow alignment.
