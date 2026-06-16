using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 376 GRADE-01/02 — pure unit tests for <see cref="AssessmentScoreAggregator"/>.
/// No DB, no fixture, not category-tagged Integration. The aggregator is pure (D-02 kill-drift:
/// single source of truth shared by FinalizeEssayGrading forward-path AND the RecomputeEssayScores endpoint).
/// Locks formula D-04: percentage = maxScore>0 ? (int)((double)total/max*100) : 0; isPassed = pct >= passPct.
/// Mixed no-drift case mirrors the inline math at AssessmentAdminController.FinalizeEssayGrading (L3535-3564)
/// and GradingService interim formula (L200) so the extracted helper cannot diverge.
/// </summary>
public class AssessmentScoreAggregatorTests
{
    // ---- in-memory builders (no DB) ----
    private static PackageQuestion Q(int id, string type, int scoreValue, params (int optId, bool correct)[] opts) =>
        new PackageQuestion
        {
            Id = id,
            QuestionType = type,
            ScoreValue = scoreValue,
            Options = opts.Select(o => new PackageOption { Id = o.optId, IsCorrect = o.correct }).ToList()
        };

    private static PackageUserResponse Resp(int qId, int? optId = null, int? essay = null) =>
        new PackageUserResponse { PackageQuestionId = qId, PackageOptionId = optId, EssayScore = essay };

    // ---- essay-only PASS (GRADE-01): 1 Essay SV=100, EssayScore=80, pass=70 → 80%, lulus ----
    [Fact]
    public void EssayOnly_Graded80_Returns80AndPassed()
    {
        var questions = new[] { Q(1, "Essay", 100) };
        var responses = new[] { Resp(1, essay: 80) };

        var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);

        Assert.Equal(80, result.TotalScore);
        Assert.Equal(100, result.MaxScore);
        Assert.Equal(80, result.Percentage);
        Assert.True(result.IsPassed);
    }

    // ---- essay-only FAIL: EssayScore=50, pass=70 → 50%, tidak lulus ----
    [Fact]
    public void EssayOnly_Graded50_Returns50AndNotPassed()
    {
        var questions = new[] { Q(1, "Essay", 100) };
        var responses = new[] { Resp(1, essay: 50) };

        var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);

        Assert.Equal(50, result.Percentage);
        Assert.False(result.IsPassed);
    }

    // ---- maxScore=0 edge (D-05): semua essay ScoreValue=0 → 0%, TIDAK throw ----
    [Fact]
    public void MaxScoreZero_ReturnsZero_NoThrow()
    {
        var questions = new[] { Q(1, "Essay", 0) };
        var responses = new[] { Resp(1, essay: 0) };

        var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);

        Assert.Equal(0, result.MaxScore);
        Assert.Equal(0, result.Percentage);
        Assert.False(result.IsPassed);
    }

    // ---- mixed no-drift (GRADE-02): MC(50,benar)+MA(50,set benar)+Essay(100,EssayScore=80) → 180/200 = 90% ----
    [Fact]
    public void Mixed_McMaEssay_MatchesInlineFormula_90Percent()
    {
        var questions = new[]
        {
            Q(1, "MultipleChoice", 50, (10, true), (11, false)),
            Q(2, "MultipleAnswer", 50, (20, true), (21, true), (22, false)),
            Q(3, "Essay", 100)
        };
        var responses = new[]
        {
            Resp(1, optId: 10),                 // MC correct → +50
            Resp(2, optId: 20), Resp(2, optId: 21), // MA exact correct set → +50
            Resp(3, essay: 80)                  // Essay manual → +80
        };

        var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);

        Assert.Equal(180, result.TotalScore);
        Assert.Equal(200, result.MaxScore);
        Assert.Equal(90, result.Percentage); // (int)(180.0/200*100)
        Assert.True(result.IsPassed);
    }

    // ---- MA wrong set → no points for that question (mirrors SetEquals gate) ----
    [Fact]
    public void MultipleAnswer_PartialSet_ScoresZeroForQuestion()
    {
        var questions = new[] { Q(1, "MultipleAnswer", 100, (10, true), (11, true), (12, false)) };
        var responses = new[] { Resp(1, optId: 10) }; // only one of two correct → not SetEquals

        var result = AssessmentScoreAggregator.Compute(questions, responses, passPercentage: 70);

        Assert.Equal(0, result.TotalScore);
        Assert.Equal(0, result.Percentage);
    }

    // ============================================================================
    // SKENARIO USER 2026-06-15 (readiness lisensor) — MA benar = {A,C,D}.
    // Soal punya 4 opsi: A=10, B=11(SALAH), C=12, D=13. correct={10,12,13}, ScoreValue=100, pass=70.
    // Buktikan ALL-OR-NOTHING (SetEquals): hanya jawaban PERSIS {A,C,D} dapat poin penuh; selain itu 0.
    // ============================================================================
    private static PackageQuestion MaACD() =>
        Q(1, "MultipleAnswer", 100, (10, true), (11, false), (12, true), (13, true));

    [Fact] // jawab PERSIS A,C,D → 100% (lulus)
    public void MA_ACD_ExactAllThree_FullScore()
    {
        var r = AssessmentScoreAggregator.Compute(new[] { MaACD() },
            new[] { Resp(1, 10), Resp(1, 12), Resp(1, 13) }, passPercentage: 70);
        Assert.Equal(100, r.TotalScore);
        Assert.Equal(100, r.Percentage);
        Assert.True(r.IsPassed);
    }

    [Fact] // jawab A,C saja (kurang D) → 0
    public void MA_ACD_PartialAC_Zero()
    {
        var r = AssessmentScoreAggregator.Compute(new[] { MaACD() },
            new[] { Resp(1, 10), Resp(1, 12) }, passPercentage: 70);
        Assert.Equal(0, r.TotalScore);
        Assert.Equal(0, r.Percentage);
        Assert.False(r.IsPassed);
    }

    [Fact] // jawab A saja → 0
    public void MA_ACD_OnlyA_Zero()
    {
        var r = AssessmentScoreAggregator.Compute(new[] { MaACD() },
            new[] { Resp(1, 10) }, passPercentage: 70);
        Assert.Equal(0, r.Percentage);
    }

    [Fact] // jawab A,C,D + B (semua benar + 1 salah) → 0
    public void MA_ACD_AllCorrectPlusWrongB_Zero()
    {
        var r = AssessmentScoreAggregator.Compute(new[] { MaACD() },
            new[] { Resp(1, 10), Resp(1, 11), Resp(1, 12), Resp(1, 13) }, passPercentage: 70);
        Assert.Equal(0, r.Percentage);
    }

    // ---- empty questions: 0%, no throw (guard kosong) ----
    [Fact]
    public void EmptyQuestions_ReturnsZero_NoThrow()
    {
        var result = AssessmentScoreAggregator.Compute(
            System.Array.Empty<PackageQuestion>(),
            System.Array.Empty<PackageUserResponse>(),
            passPercentage: 70);

        Assert.Equal(0, result.MaxScore);
        Assert.Equal(0, result.Percentage);
        Assert.False(result.IsPassed);
    }
}
