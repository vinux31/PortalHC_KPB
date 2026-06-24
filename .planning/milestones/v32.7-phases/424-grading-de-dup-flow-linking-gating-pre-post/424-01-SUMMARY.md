# Plan 424-01 Summary — Parity-lock + helpers + single-scorer (KEYSTONE)

**Status:** ✅ Complete
**Requirements:** GRDF-02 (+ helper untuk GRDF-03/GRDF-05/GRDF-01 dikonsumsi Plan 02)
**Commits:** `b8991c6a` (Task 1), `2e6fa1e5` (Task 2)
**migration:** false

## What was built

**Task 1 — Parity-lock + 2 pure helper (`b8991c6a`):**
- `Helpers/ExamTimeRules.cs` — `AllowedExamSeconds(duration, extra) = (d + (extra ?? 0)) * 60` (GRDF-05, dikonsumsi clamp Plan 02).
- `Helpers/PrePostPairing.cs` — `FindPairedPreAsync(ctx, post)`: pairing Pre→Post per-UserId via link eksplisit (LinkedSessionId → fallback LinkedGroupId), `s.UserId == post.UserId` di SETIAP cabang (fix FLOW-01 root), no title-pattern; Standard/Pre/orphan → null (pass-through D-02). Dikonsumsi gate GRDF-01 Plan 02.
- Tests: `ExamTimeRulesTests` (3 pure), `PrePostPairingTests` (2 pure pass-through), `GradingDedupeTests` +2 parity-LOCK (PATH1==Aggregator single-MC & MA) — HIJAU terhadap kode sekarang = jaring D-07.

**Task 2 — Konvergensi scorer (`2e6fa1e5`):**
- `Helpers/AssessmentScoreAggregator.cs` `Compute` MC: `FirstOrDefault` → `finalByQuestion` dedupe last-write-wins (port verbatim pola kanonik `GradingService.cs:87-90`). MA `SetEquals` / Essay / pct formula (D-04) TIDAK disentuh. Doc-comment "single source of truth" dijadikan benar (GRDF-02).
- `Services/GradingService.cs` `ComputeScoreAndETInternalAsync`: `mcSel.First()` (2 situs main + ET) → `FinalMcOption` lokal (override=jawaban final admin; selain itu last-write-wins by SubmittedAt). MA `SelectedOptions` penuh utuh. PATH 1 `GradeAndCompleteAsync` tak diubah (kanonik).
- Tests: Aggregator MC >1-resp last-write-wins + order-independent + essay-null-pending; GradingDedupe PreviewScore PATH 2 >1-resp last-write-wins.

## Key files
- created: `Helpers/ExamTimeRules.cs`, `Helpers/PrePostPairing.cs`, `HcPortal.Tests/ExamTimeRulesTests.cs`, `HcPortal.Tests/PrePostPairingTests.cs`
- modified: `Helpers/AssessmentScoreAggregator.cs`, `Services/GradingService.cs`, `HcPortal.Tests/GradingDedupeTests.cs`, `HcPortal.Tests/AssessmentScoreAggregatorTests.cs`

## Verification
- `dotnet build` 0 error (2×).
- Task 1: `dotnet test --filter ExamTimeRules|PrePostPairing|GradingDedupe` → 9/9 pass (parity-lock hijau vs kode sekarang).
- Task 2: `dotnet test --filter AssessmentScoreAggregator|IsQuestionCorrect|GradingDedupe` → **29/29 pass**.
- Acceptance grep: `mcSel.First()`=0 · Aggregator `OrderByDescending` present · MA `SetEquals` retained (4×) · no title-pattern in PrePostPairing.
- **D-07 terbukti:** parity-LOCK (PATH1==Aggregator, single-MC & MA) ditulis HIJAU sebelum konvergensi, TETAP HIJAU sesudah → skor sesi normal/Completed tak berubah.

## Deviations
- **Sequencing minor (Rule 3, non-blocking):** plan menempatkan parity case (b) "MC >1-response → all-3-paths last-write-wins" di Task 1. Karena PATH 2/3 BELUM dedupe sebelum Task 2, test cross-path-equal untuk >1-response akan RED sebelum Task 2 (kontradiksi "hijau vs kode sekarang"). Resolusi TDD-benar: Task 1 berisi LOCK tests (green now & after: single-MC & MA PATH1==PATH3); Task 2 berisi CONVERGENCE tests (MC >1-resp via Aggregator + PreviewScore PATH 2) sebagai bukti fix. Coverage net identik, sequencing lebih bersih.
- `FinalMcOption` override path pakai `ov[0]` (deterministik) menggantikan `ToHashSet().First()` (arbitrer) — untuk MC override yang selalu single-value, identik; lebih deterministik untuk edge multi-value.

## Self-Check: PASSED
- GRDF-02: satu fungsi scorer (Aggregator promoted) + dedupe last-write-wins seragam 3 jalur. ✓
- Helper PrePostPairing + ExamTimeRules siap dikonsumsi Plan 02. ✓
- 0 regresi (full filter 29/29). ✓
