---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
plan: 02
subsystem: domain-helpers
tags: [retake, pure-helper, tdd, eligibility, archive, v32.4]
requires:
  - "Models/AssessmentAttemptResponseArchive.cs (entity output type, plan 405-01)"
  - "Helpers/AssessmentScoreAggregator.cs (IsQuestionCorrect/BuildAnswerCell verdict source)"
provides:
  - "Helpers/RetakeRules.cs — CanRetake + ShouldHideRetakeToggle (pure eligibility RTK-03/13)"
  - "Helpers/RetakeArchiveBuilder.cs — Build snapshot beku per-soal + essay full-text (RTK-02)"
affects:
  - "Plan 405-03 (RetakeService.CanRetakeAsync bungkus RetakeRules.CanRetake; ExecuteAsync panggil RetakeArchiveBuilder.Build sebelum delete)"
  - "Phase 407 (worker ViewModel/controller pakai RetakeRules untuk gating — kill-drift sama helper)"
tech_stack:
  added: []
  patterns:
    - "Pure helper no-DI (EF-free, sinkron) mirror ShuffleToggleRules — kill-drift dipakai 2 tempat"
    - "CanRetake terima attemptsUsed sebagai param (counting era-retake D-01 di service, bukan helper)"
    - "nowUtc di-inject (bukan DateTime.UtcNow internal) → cooldown deterministic-testable"
    - "Verdict beku via AssessmentScoreAggregator.IsQuestionCorrect (single source, no re-grade inline)"
    - "Essay AnswerText full-text (Pitfall 2: BuildAnswerCell truncate 300 dihindari per D-04)"
    - "TDD RED→GREEN commit terpisah per task (test() lalu feat())"
key_files:
  created:
    - Helpers/RetakeRules.cs
    - Helpers/RetakeArchiveBuilder.cs
    - HcPortal.Tests/RetakeRulesTests.cs
    - HcPortal.Tests/RetakeArchiveBuilderTests.cs
  modified: []
decisions:
  - "RetakeRules.CanRetake tetap PURE — attemptsUsed int sebagai input; counting snapshot-presence (D-01) ditinggalkan ke RetakeService plan 405-03 agar helper unit-testable di SEMUA cabang"
  - "Essay AnswerText = TextAnswer FULL (no truncate) per Pitfall 2 / D-04 (archive permanen ISO 17024); MC/MA tetap BuildAnswerCell"
  - "AwardedScore: essay → EssayScore ?? 0; MC/MA → verdict==true ? ScoreValue : 0"
metrics:
  duration: "7m"
  completed: "2026-06-21"
  tasks: 2
  files: 4
migration: false
notify_it: false
---

# Phase 405 Plan 02: Backend Core — RetakeRules + RetakeArchiveBuilder (Pure Helpers) Summary

**One-liner:** Dua helper PURE EF-free v32.4 via TDD — `RetakeRules` (kelayakan ujian ulang `CanRetake` 7-guard + `ShouldHideRetakeToggle`) dan `RetakeArchiveBuilder` (snapshot per-soal beku via `AssessmentScoreAggregator.IsQuestionCorrect` dengan essay disimpan FULL-TEXT, bukan truncate 300 char) — keduanya unit-tested di semua cabang, siap dipakai dua tempat (RetakeService 405-03 + Phase 407 ViewModel) tanpa drift.

## What Was Built

Wave 2 (paralel-able dengan plan 405-03 yang bergantung pada signature di sini). Dua helper murni — keputusan logika dipisah dari I/O DB sehingga unit-testable di semua cabang dan dipakai dua jalur (service + Phase 407 worker) tanpa divergensi (pola kill-drift `ShuffleToggleRules`).

### Task 1 — `RetakeRules` CanRetake + ShouldHideRetakeToggle (RTK-03/13) [TDD]
- **RED** commit `ddc95b0f` — `HcPortal.Tests/RetakeRulesTests.cs`: 12 Fact + 1 Theory(4 case) = **16 test case**, gagal compile ("RetakeRules does not exist").
- **GREEN** commit `b8aa094e` — `Helpers/RetakeRules.cs` (`HcPortal.Helpers`, `public static class RetakeRules`).
  - `CanRetake(bool allowRetake, string? assessmentType, bool isManualEntry, string status, bool? isPassed, int attemptsUsed, int maxAttempts, int retakeCooldownHours, DateTime? completedAt, DateTime nowUtc) → bool`. Guard fail-fast berurutan: `!allowRetake` → `PreTest` → `isManualEntry` → `status!="Completed"` → `isPassed != false` (null=PendingGrading & true=Lulus di-block) → `attemptsUsed >= maxAttempts` → cooldown (`<=0` → true; `completedAt==null` → false; else `nowUtc >= completedAt+jam`).
  - `ShouldHideRetakeToggle(string? assessmentType, bool isManualEntry) → bool` = `assessmentType=="PreTest" || isManualEntry`.
  - PURE (EF-free, sinkron); `nowUtc` di-inject untuk cooldown deterministic.
- **16/16 GREEN** (`dotnet test --filter FullyQualifiedName~RetakeRulesTests`).

### Task 2 — `RetakeArchiveBuilder` snapshot beku + essay full-text (RTK-02) [TDD]
- **RED** commit `35eec73d` — `HcPortal.Tests/RetakeArchiveBuilderTests.cs`: **4 test case** termasuk `Build_EssayLongText_NotTruncated` (assert `Length==500`), gagal compile.
- **GREEN** commit `175eb0a5` — `Helpers/RetakeArchiveBuilder.cs` (`HcPortal.Helpers`, `public static class RetakeArchiveBuilder`).
  - `Build(int attemptHistoryId, IEnumerable<PackageQuestion> questions, IEnumerable<PackageUserResponse> responses) → List<AssessmentAttemptResponseArchive>`.
  - Per soal: `verdict = AssessmentScoreAggregator.IsQuestionCorrect(q, forQ)` (kill-drift — no re-grade inline). `IsCorrect = verdict`. `QuestionText = q.QuestionText ?? ""`. `AttemptHistoryId` = param.
  - **Essay:** `AnswerText = essayResp?.TextAnswer` FULL (no truncate, Pitfall 2 ditutup); `AwardedScore = essayResp?.EssayScore ?? 0`.
  - **MC/MA:** `AnswerText = AssessmentScoreAggregator.BuildAnswerCell(q, forQ)`; `AwardedScore = verdict==true ? q.ScoreValue : 0`.
- **4/4 GREEN** (`dotnet test --filter FullyQualifiedName~RetakeArchiveBuilderTests`).

## Konfirmasi Pitfall 2 (essay full-text)

✅ **Essay TIDAK ter-truncate.** Test `Build_EssayLongText_NotTruncated` mem-feed `TextAnswer = new string('x', 500)` dengan `EssayScore = 8` dan meng-assert `row.AnswerText.Length == 500` (BUKAN 303 = 300+"..."), plus `Assert.Equal(longText, row.AnswerText)`. Builder cabang Essay memakai `responseForQ.TextAnswer` langsung — BUKAN `BuildAnswerCell` (yang truncate 300 char di `AssessmentScoreAggregator.cs:120` untuk display PDF/Excel). MC/MA tetap pakai `BuildAnswerCell` (OptionText, tak ter-truncate). Sesuai D-04 (archive permanen, retain-all, ISO 17024).

## Signature final

```csharp
// Helpers/RetakeRules.cs
public static bool CanRetake(
    bool allowRetake, string? assessmentType, bool isManualEntry, string status,
    bool? isPassed, int attemptsUsed, int maxAttempts, int retakeCooldownHours,
    DateTime? completedAt, DateTime nowUtc);
public static bool ShouldHideRetakeToggle(string? assessmentType, bool isManualEntry);

// Helpers/RetakeArchiveBuilder.cs
public static List<AssessmentAttemptResponseArchive> Build(
    int attemptHistoryId,
    IEnumerable<PackageQuestion> questions,
    IEnumerable<PackageUserResponse> responses);
```

## Jumlah test cases

| File | Cases | Hasil |
|------|-------|-------|
| `RetakeRulesTests.cs` | 16 (12 Fact + 4 Theory inline) | Passed 16/16 |
| `RetakeArchiveBuilderTests.cs` | 4 (3 Fact + 1 Pitfall-2 Fact) | Passed 4/4 |

## Verification Results

| Cek | Hasil |
|-----|-------|
| `dotnet test --filter "FullyQualifiedName~RetakeRules"` | Passed! 16/16, 0 failed ✓ |
| `dotnet test --filter "FullyQualifiedName~RetakeArchiveBuilder"` | Passed! 4/4, 0 failed ✓ |
| `dotnet build` | Build succeeded, 0 Warning, 0 Error ✓ |
| TDD discipline | RED gagal-compile dulu (kedua task), lalu GREEN — commit terpisah ✓ |
| No re-grade inline | Builder hanya panggil `IsQuestionCorrect` + `BuildAnswerCell` (essay pakai TextAnswer) ✓ |

## Deviations from Plan

None — plan dieksekusi persis seperti tertulis. Semua signature aggregator/entity/model terverifikasi langsung dari repo (`AssessmentScoreAggregator.cs:73,110`, `Models/AssessmentAttemptResponseArchive.cs`, `Models/PackageUserResponse.cs`, `Models/AssessmentPackage.cs` untuk `PackageQuestion`/`PackageOption`) dan cocok dengan plan `<interfaces>`. `PackageQuestion.Options` adalah `ICollection<PackageOption>` (test helper `Mc` meng-assign `List<PackageOption>` — kompatibel). Tidak wiring ke RetakeService/controller (itu plan 405-03/04).

## TDD Gate Compliance

Plan `type: tdd`. Gate sequence per task terpenuhi (verified di git log):
- Task 1: `test(405-02): RED RetakeRules ...` (`ddc95b0f`) → `feat(405-02): RetakeRules pure eligibility ...` (`b8aa094e`).
- Task 2: `test(405-02): RED RetakeArchiveBuilder ...` (`35eec73d`) → `feat(405-02): RetakeArchiveBuilder ...` (`175eb0a5`).
- REFACTOR tidak dibutuhkan (implementasi minimal sudah bersih). RED keduanya gagal-compile dulu (tidak ada false-pass).

## Notes for Downstream Plans

- **Plan 405-03** (`RetakeService`): `CanRetakeAsync` membungkus `RetakeRules.CanRetake(...)` dengan `attemptsUsed = eraRetakeArchives + 1` (snapshot-presence query D-01 — `AssessmentAttemptHistory` ber-child `AssessmentAttemptResponseArchive`, grouping `(UserId, Title, Category)`). `ExecuteAsync` memanggil `RetakeArchiveBuilder.Build(attemptHistoryId, questions, responses)` SETELAH claim atomik dan SEBELUM `RemoveRange(PackageUserResponses)` (urutan claim→snapshot→archive→delete).
- **Phase 407** (worker UI): pakai `RetakeRules.CanRetake` (gating tombol) + `ShouldHideRetakeToggle` (visibilitas) — helper sama, no drift.

## No Known Stubs

Tidak ada stub. Kedua helper murni terimplementasi penuh + semua cabang ter-cover test. Tidak ter-render ke UI di phase ini (helper backend; wiring ke service/controller = 405-03/04 & Phase 407).

## No Threat Flags

Tidak ada surface keamanan baru di luar `<threat_model>` plan. T-405-05 (re-grade inline drift) di-mitigate: builder WAJIB `AssessmentScoreAggregator.IsQuestionCorrect` (verified, no inline grading). T-405-07 (eligibility longgar) di-mitigate: cabang `isPassed != false → false` + `status!="Completed"` di-uji eksplisit (`Blocked_WhenPassed`/`WhenPendingGrading`/`WhenNotCompleted`/`WhenCancelled`). T-405-06 (essay full-text) accept-by-design (jawaban worker sendiri, bukan answer-key; gating display = Phase 407).

## Self-Check: PASSED

- Files: `Helpers/RetakeRules.cs`, `Helpers/RetakeArchiveBuilder.cs`, `HcPortal.Tests/RetakeRulesTests.cs`, `HcPortal.Tests/RetakeArchiveBuilderTests.cs`, `405-02-SUMMARY.md` — semua FOUND.
- Commits: `ddc95b0f`, `b8aa094e`, `35eec73d`, `175eb0a5` — semua FOUND.
