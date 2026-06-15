# Phase 383: Essay Grading Correctness + Test (Fase 1) - Context

**Gathered:** 2026-06-15
**Status:** Ready for planning
**Source:** Brainstorming session 2026-06-15 (design spec approved) + multi-agent root-cause investigation

<domain>
## Phase Boundary

Fix the display-side essay correctness bug on `CMP/Results/{id}`: when HC has graded essay questions, the score % is right (essay-aware) but the "(X/Y benar)" count, the "Elemen Teknis" breakdown, the "Tinjauan Jawaban" per-question badge, and the PDF export all treat essays as wrong. Root cause: two divergent per-question scoring paths in `CMPController.Results()` — Path A (score%) reads `assessment.Score` ← `AssessmentScoreAggregator.Compute()` (essay-aware), Path B (count/ET/Tinjauan) recomputes inline with option-matching only (no Essay branch). Essays store grade in `PackageUserResponse.EssayScore` and have no `PackageOptionId`, so Path B forces them incorrect.

Fix = one centralized pure helper used by all display surfaces + PDF, eliminating the drift. Plus regression tests that lock the already-correct save/finalize logic (point 2) without changing it.

**Read/display-path only. No DB migration, no schema, no backfill, no writes. Pass/Fail untouched (derives from the already-correct score%).**

This is Fase 1 (hotfix, ships first, isolated). Fase 2 (Phase 384) = Monitoring essay UI refactor — OUT of this phase.

</domain>

<decisions>
## Implementation Decisions (LOCKED)

### D-01 Centralized helper (not inline patch)
Add a pure, EF-free, synchronous, unit-testable sibling of `Compute` to `Helpers/AssessmentScoreAggregator.cs`:
```csharp
public static bool? IsQuestionCorrect(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
```
Returns `true`=Benar, `false`=Salah, `null`=essay belum dinilai (pending). Kill-drift — one definition of per-question correctness shared by web Results (count, ET, Tinjauan) AND PDF export. Matches the existing single-source pattern of `Compute` (Phase 376) / Phase 363/365.

### D-01a MC/MA replicated byte-for-byte
The helper's MultipleChoice + MultipleAnswer branches MUST replicate the CURRENT inline logic exactly (verified against `CMPController.cs:2259-2324` + `AssessmentScoreAggregator.cs:46-50`). Only the Essay branch is new behavior. Rule per type:
- **MultipleChoice:** correct iff exactly the selected single option has `IsCorrect`; 0 selected → false.
- **MultipleAnswer:** `selected.Count > 0 && selected.SetEquals(correctIds)` — non-empty guard (closes GRD-02).
- **Essay:** `EssayScore.HasValue ? EssayScore.Value > 0 : null`.

### D-02 Essay "Benar" rule = EssayScore > 0
User-chosen (simpler than PDF's `>= ScoreValue/2`). `EssayScore > 0` → Benar; `== 0` → Salah; `null` → pending. Caveat accepted (partial-credit essay shows Benar in the binary count while contributing partial to %).

### D-03 PDF export unifies to the helper
`AssessmentAdminController.cs:~5017` currently uses `resp.EssayScore.Value >= (q.ScoreValue / 2)`. Replace essay correctness with `IsQuestionCorrect` (essay `> 0`, null = pending). Intentional behavior change to PDF so web Results and PDF share one rule.

### D-04 No migration
`EssayScore` already exists on `PackageUserResponse` and is populated by `SubmitEssayScore`. Display-path fix only.

### D-05 Point 2 (save/finalize) already correct — lock with tests, no code change
`AssessmentAdminController.SubmitEssayScore` (~3436-3465) persists `EssayScore` atomically with validation. `FinalizeEssayGrading` (~3477-3671) recomputes session score via `AssessmentScoreAggregator.Compute()` (essay-aware). Verdict: sound. Add regression tests only.

### D-06 IsEssayPending broadened
`CMPController.cs:2299-2300` `IsEssayPending` currently true only when `Status == PendingGrading`. Broaden to: essay question AND `IsQuestionCorrect(...) == null` (no `EssayScore`), independent of session status — so a graded essay in a `Completed` session renders its real Benar/Salah, and an ungraded essay always shows "Menunggu Penilaian" (defensive). Keep using the existing `AssessmentConstants.AssessmentStatus.PendingGrading` label "Menunggu Penilaian".

### D-07 Tinjauan essay answer text
For essay review rows set `UserAnswer = TextAnswer` (worker's essay) and `CorrectAnswer = "Dinilai manual"` (or a score string) so the review renders the worker's answer instead of empty / "N/A".

### Claude's Discretion
- Exact method signature variant (whether helper takes the full responses list or pre-filtered per-question — keep it pure and EF-free either way).
- Where to place regression tests (which test class) + exact fixture shape (use real-SQL disposable fixture if `ExecuteUpdateAsync`/EF8 InMemory limits apply, per the Phase 382 lesson).
- Whether to refactor `Compute`'s per-type logic to share with `IsQuestionCorrect` internally, as long as scoring (points) and correctness (bool?) stay distinct concerns and `Compute`'s D-04 LOCKED formula is unchanged.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design + scope
- `docs/superpowers/specs/2026-06-15-essay-grading-correctness-design.md` — full design, root cause, decisions, fix shape, test list
- `.planning/REQUIREMENTS.md` — ECG-01..06 + UIG-01..04
- `.planning/ROADMAP.md` — Phase 383 section (goal + 5 success criteria + files)

### Code (bug + fix surface)
- `Helpers/AssessmentScoreAggregator.cs` — existing `Compute` (essay-aware scoring, line 52-54); ADD `IsQuestionCorrect` here
- `Controllers/CMPController.cs` — `Results()` 3 buggy sites: review-on `~2258-2271`, review-off count `~2304-2327`, Elemen Teknis `~2336-2369`; `IsEssayPending` `~2299-2300`; ViewModel build `~2371+`
- `Controllers/AssessmentAdminController.cs` — PDF export essay correctness `~5017`; `SubmitEssayScore` `~3436-3465`; `FinalizeEssayGrading` `~3477-3671`
- `Models/PackageUserResponse.cs` — `EssayScore` (int?), `TextAnswer`, `PackageOptionId`, `PackageQuestionId`
- `Views/CMP/Results.cshtml` — consumes `CorrectAnswers` (line 55), `ElemenTeknisScores`, per-question `IsCorrect` + `IsEssayPending` badge (~333-350)

### Test patterns
- Existing `HcPortal.Tests/` xUnit suite (415/415 green at v29.0 close); `AssessmentScoreAggregator` tests (Phase 376) as the analog for pure-helper unit tests.

</canonical_refs>

<specifics>
## Specific Ideas

- Reproduce the reported case: a session with N MC (all correct) + 2 essays graded fully → expect `CorrectAnswers == N+2` (e.g. 6/6), ET counts essays, Tinjauan essay badge green Benar.
- Unit matrix for `IsQuestionCorrect`: MC {correct, incorrect, unanswered}; MA {exact, partial-subset, superset, unanswered → all-but-exact false, non-empty guard}; Essay {`>0` true, `=0` false, `null` null}.
- Verify per CLAUDE.md dev workflow: `dotnet build` + `dotnet test` + `dotnet run` → `http://localhost:5277` check `CMP/Results/166` shows 6/6 + green Benar on Soal 5/6.

</specifics>

<deferred>
## Deferred Ideas

- Monitoring essay UI refactor (worker-list table + "Tinjau Essay" per-worker page) → Phase 384 (UIG-01..04).
- Essay rubric editor / partial-credit weighting beyond the binary `>0` badge rule → out of milestone.

</deferred>

---

*Phase: 383-essay-grading-correctness-test-fase-1*
*Context gathered: 2026-06-15 via brainstorming design spec*
