# Design: Essay Grading Correctness + Monitoring UI Refactor (v30.0)

**Date:** 2026-06-15
**Status:** Approved (brainstorming session 2026-06-15)
**Milestone:** v30.0 (phases 383-384)
**Source bug:** User report 2026-06-15 — `CMP/Results/166` after grading all essays fully correct shows "Nilai Anda 100%" but "(4/6 benar)", Elemen Teknis counts essays wrong, Tinjauan Jawaban marks essays red "Salah". Confirms deferred backlog **RES-02** (display-drift X/Y vs Score%) + touches **GRD-02** (empty-MA SetEquals LOW).

---

## 1. Problem (root cause — verified file:line)

Two divergent per-question scoring paths in `Controllers/CMPController.cs` `Results()`:

| Output | Path | Essay-aware? | Status |
|---|---|---|---|
| "Nilai Anda" % | A: reads precomputed `assessment.Score` ← `AssessmentScoreAggregator.Compute()` (`Helpers/AssessmentScoreAggregator.cs:52-54` Essay case) | ✅ yes | correct (100%) |
| "(X/Y benar)" count | B: inline option-matching, **no Essay branch** | ❌ no | wrong (4/6) |
| "Elemen Teknis" breakdown | B: same | ❌ no | wrong |
| "Tinjauan Jawaban" badge | B: same | ❌ no | wrong (Salah) |

Essays store the grade in `PackageUserResponse.EssayScore` and have **no `PackageOptionId`**. Path B only matches options, so essays fall through with zero selected options and are forced incorrect.

Three buggy sites in `CMPController.cs`:
1. **`~2258-2271`** (review enabled): essay hits `else` MC branch → `single = selectedOptions.FirstOrDefault()` is `null` → `isCorrect=false`, `correctCount` not incremented.
2. **`~2304-2327`** (review disabled count): `if (selectedIds.Count == 0) continue;` (2314) → essay skipped, never counted.
3. **`~2336-2369`** (Elemen Teknis): `if (selectedIds.Count == 0) return false;` (2350) → essay counted wrong in ET breakdown.

A fourth surface duplicates the essay-correct rule with a **different threshold**: PDF export `Controllers/AssessmentAdminController.cs:~5017` uses `EssayScore >= ScoreValue/2` (null=pending). This divergence is the same drift class that caused the bug.

`IsEssayPending` (`CMPController.cs:2299-2300`) only suppresses the badge while `Status == PendingGrading`; once finalized (`Completed`) the essay renders the buggy `isCorrect=false`.

**Pass/Fail is NOT affected** — `IsPassed` derives from the score % (Path A), which is correct. Only display count/ET/badge are wrong.

## 2. Save/Finalize logic (point 2) — already correct, no code change

- `AssessmentAdminController.SubmitEssayScore` (`~3436-3465`): atomically persists `response.EssayScore` with validation. ✅
- `AssessmentAdminController.FinalizeEssayGrading` (`~3477-3671`): recomputes session score via `AssessmentScoreAggregator.Compute()` (essay-aware) → why "Nilai Anda" is correct. ✅

Verdict: write/finalize path is sound. v30.0 locks it with regression tests only (no logic change).

## 3. Decisions (locked in brainstorming)

- **D-01 Fix shape:** centralized pure helper (Option 2), not inline patch. Kill-drift — matches the Phase 363/365/376 single-source pattern already used by `AssessmentScoreAggregator`.
- **D-02 Essay "Benar" rule:** `EssayScore > 0` → Benar; `EssayScore == 0` → Salah; `EssayScore == null` → pending (no badge). User-chosen (simpler than the PDF `>= half`).
- **D-03 PDF unify:** PDF export essay correctness routes through the same helper → changes from `>= ScoreValue/2` to `> 0`. Intentional consequence of unification (one rule everywhere).
- **D-04 No migration:** `EssayScore` already exists on `PackageUserResponse` and is populated. Fix is read/display-path only — no schema, no backfill, no writes.
- **D-05 Phase split:** 2 phases. Phase 1 = correctness + tests (urgent hotfix, ship first). Phase 2 = monitoring UI refactor.

## 4. Solution — Phase 383 (Fase 1): Essay Grading Correctness + Test

### 4.1 New helper (single source of truth)

Add to `Helpers/AssessmentScoreAggregator.cs` a pure, EF-free, sibling of `Compute`:

```csharp
/// <summary>Per-question correctness for display (count, ElemenTeknis, Tinjauan, PDF).
/// Returns null = essay not yet graded (pending). Mirrors Compute()'s type handling.</summary>
public static bool? IsQuestionCorrect(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)
{
    var list = responsesForQ as IList<PackageUserResponse> ?? responsesForQ.ToList();
    switch (q.QuestionType ?? "MultipleChoice")
    {
        case "MultipleAnswer":
        {
            var selected = list.Where(r => r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
            var correct  = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            return selected.Count > 0 && selected.SetEquals(correct);   // GRD-02: non-empty guard
        }
        case "Essay":
        {
            var essay = list.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            if (essay?.EssayScore.HasValue != true) return null;        // pending
            return essay.EssayScore.Value > 0;                          // D-02
        }
        default: // MultipleChoice
        {
            var sel = list.Where(r => r.PackageOptionId.HasValue).Select(r => r.PackageOptionId!.Value).ToHashSet();
            if (sel.Count == 0) return false;
            var opt = q.Options.FirstOrDefault(o => sel.Contains(o.Id));
            return opt != null && opt.IsCorrect;
        }
    }
}
```

MC/MA branches replicate the **current** inline logic byte-for-byte (verified against `CMPController.cs:2259-2324` + `AssessmentScoreAggregator.cs:46-50`) so MC/MA behavior does not change. Only Essay is new.

### 4.2 Call-site rewires (`CMPController.cs` Results)

- Site 1 (review on, `~2258-2271`): replace inline `isCorrect` with `IsQuestionCorrect(question, userResponses)`. Map: `true`→Benar + `correctCount++`; `false`→Salah; `null`→pending (set `IsEssayPending`).
- Site 2 (review off, `~2304-2327`): `correctCount += IsQuestionCorrect(...) == true ? 1 : 0`.
- Site 3 (Elemen Teknis, `~2336-2369`): correct predicate → `IsQuestionCorrect(...) == true`.
- `IsEssayPending`: broaden to `question is Essay && IsQuestionCorrect(...) == null` (essay with no `EssayScore`), independent of session status — a graded essay in a `Completed` session renders its real Benar/Salah, an ungraded one always shows pending (defensive).
- **Tinjauan display for essays:** set `UserAnswer = TextAnswer` and `CorrectAnswer = "Dinilai manual"` (or score string) so the review row renders the worker's essay text instead of empty/"N/A".

### 4.3 PDF export unify (`AssessmentAdminController.cs:~5017`)

Replace `resp.EssayScore.Value >= (q.ScoreValue / 2)` essay correctness with `IsQuestionCorrect(q, respsForQ)` (essay → `> 0`, null → pending). Keeps web Results and PDF identical.

### 4.4 Tests

- Unit (`AssessmentScoreAggregatorTests` or new `IsQuestionCorrectTests`): MC (correct/incorrect/unanswered), MA (exact/partial/superset/unanswered → non-empty guard GRD-02), Essay (`>0` Benar, `=0` Salah, `null` pending).
- Regression: a session with N MC + 2 graded essays → `CorrectAnswers == N+2`, ET counts essays, Tinjauan essay badge Benar. Reproduces the reported `CMP/Results/166` symptom.
- Regression (point 2 lock, no code change): `SubmitEssayScore` persists `EssayScore` + authz; `FinalizeEssayGrading` recomputes score incl essay + idempotent.

### 4.5 Verification

`dotnet build` + `dotnet test` (all green) + `dotnet run` → check `CMP/Results/166` shows 6/6 + green Benar on Soal 5/6 (per CLAUDE.md dev workflow). No migration.

## 5. Solution — Phase 384 (Fase 2): Monitoring Essay Grading UI Refactor

Current: `Views/Admin/AssessmentMonitoringDetail.cshtml:381-481` renders all pending essays inline per worker on one long page. "Simpan Skor" → JS POST `/Admin/SubmitEssayScore` (handler `~1471-1517`); "Selesaikan Penilaian" → JS POST `/Admin/FinalizeEssayGrading` (handler `~1519-1558`).

Refactor (backend endpoints **unchanged**):
- **UIG-01:** Replace inline essay block with a **worker-list table** (columns: Worker/NIP, jumlah essay belum dinilai, status [Selesai / N belum dinilai]) + "Tinjau Essay" button per row (right-aligned).
- **UIG-02 / UIG-03:** New GET action (e.g. `EssayGrading?sessionId=...` or `?groupId=&userId=`) returning a **per-worker grading page** that reuses `EssayGradingItemViewModel` (`Models/AssessmentMonitoringViewModel.cs:73-86`) + the existing `SubmitEssayScore` + `FinalizeEssayGrading` POST endpoints. "Selesaikan Penilaian" lives on this page.
- New ViewModel for the worker-list (worker, count pending/graded, status).
- **UIG-04:** Playwright e2e — list renders, "Tinjau Essay" navigates, grade + finalize round-trips. (Razor dynamic → Playwright runtime required, per Phase 354 lesson.)

Decoupled from Phase 383. Low backend risk (view + 1 GET action + reuse).

## 6. Out of scope

- Essay rubric editor / partial-credit weighting beyond the binary `>0` badge rule.
- Bulk essay grading across workers in one action.
- Changing how `EssayScore` is captured or its data type.
- Proton / non-essay grading logic.

## 7. Risk (fast but safe)

- **No migration**, read/display-path only for Phase 383. Pass/Fail untouched.
- MC/MA logic preserved byte-for-byte in helper → covered by replicating tests + existing suite.
- Ship order: Phase 383 (isolated hotfix) → build/test/run verify → push → notify IT → Phase 384.
