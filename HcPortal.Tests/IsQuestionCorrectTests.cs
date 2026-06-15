using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 383 ECG-01 — pure unit tests for <see cref="AssessmentScoreAggregator.IsQuestionCorrect"/>.
/// No DB, no fixture, NOT category-tagged Integration. The helper is pure (kill-drift: single source of
/// truth for per-question correctness shared by web Results count/ElemenTeknis/Tinjauan AND PDF export).
/// Matrix locks D-01a/D-02:
///   - MultipleChoice: benar iff opsi tunggal terpilih ber-IsCorrect; 0 terpilih → false.
///   - MultipleAnswer: selected.Count > 0 && selected.SetEquals(correctIds) — non-empty guard (closes GRD-02).
///   - Essay: EssayScore > 0 → true; == 0 → false; null → null (pending).
/// Reproduksi ECG-02: N MC benar + 2 essay graded → sum(IsQuestionCorrect == true) == N+2.
/// Builder Q/Resp di-COPY VERBATIM dari AssessmentScoreAggregatorTests.cs:20-30.
/// </summary>
public class IsQuestionCorrectTests
{
    // ---- in-memory builders (no DB) — verbatim dari AssessmentScoreAggregatorTests.cs ----
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

    // ============ MultipleChoice (3) ============

    // ---- MC correct: 1 opsi benar terpilih → true ----
    [Fact]
    public void MultipleChoice_Correct_ReturnsTrue()
    {
        var q = Q(1, "MultipleChoice", 10, (10, true), (11, false));
        var responses = new[] { Resp(1, optId: 10) };

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.True(result == true);
    }

    // ---- MC incorrect: 1 opsi salah terpilih → false ----
    [Fact]
    public void MultipleChoice_Incorrect_ReturnsFalse()
    {
        var q = Q(1, "MultipleChoice", 10, (10, true), (11, false));
        var responses = new[] { Resp(1, optId: 11) };

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.False(result == true);
    }

    // ---- MC unanswered: 0 opsi terpilih → false ----
    [Fact]
    public void MultipleChoice_Unanswered_ReturnsFalse()
    {
        var q = Q(1, "MultipleChoice", 10, (10, true), (11, false));
        var responses = System.Array.Empty<PackageUserResponse>();

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.False(result == true);
    }

    // ============ MultipleAnswer (4) ============

    // ---- MA exact: semua opsi benar persis terpilih → true ----
    [Fact]
    public void MultipleAnswer_ExactSet_ReturnsTrue()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true), (11, true), (12, false));
        var responses = new[] { Resp(1, optId: 10), Resp(1, optId: 11) }; // 2 benar persis

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.True(result == true);
    }

    // ---- MA partial-subset: sebagian opsi benar terpilih → false ----
    [Fact]
    public void MultipleAnswer_PartialSubset_ReturnsFalse()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true), (11, true), (12, false));
        var responses = new[] { Resp(1, optId: 10) }; // hanya 1 dari 2 benar

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.False(result == true);
    }

    // ---- MA superset: semua benar + 1 salah terpilih → false (guard tidak terlalu permisif) ----
    [Fact]
    public void MultipleAnswer_Superset_ReturnsFalse()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true), (11, true), (12, false));
        var responses = new[] { Resp(1, optId: 10), Resp(1, optId: 11), Resp(1, optId: 12) }; // 2 benar + 1 salah

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.False(result == true);
    }

    // ---- MA empty/unanswered: 0 terpilih → false (non-empty guard GRD-02) ----
    [Fact]
    public void MultipleAnswer_EmptyUnanswered_ReturnsFalse()
    {
        var q = Q(1, "MultipleAnswer", 100, (10, true), (11, true));
        var responses = System.Array.Empty<PackageUserResponse>(); // 0 terpilih → SetEquals empty TIDAK boleh true

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.False(result == true);
    }

    // ============ Essay (3) ============

    // ---- Essay graded >0 (EssayScore=80) → true ----
    [Fact]
    public void Essay_GradedAbove0_ReturnsTrue()
    {
        var q = Q(1, "Essay", 100);
        var responses = new[] { Resp(1, essay: 80) };

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.True(result == true);
    }

    // ---- Essay graded =0 (EssayScore=0) → false ----
    [Fact]
    public void Essay_GradedZero_ReturnsFalse()
    {
        var q = Q(1, "Essay", 100);
        var responses = new[] { Resp(1, essay: 0) };

        var result = AssessmentScoreAggregator.IsQuestionCorrect(q, responses);

        Assert.False(result == true);
        Assert.False(result == null); // bukan pending — sudah dinilai, hasilnya Salah
    }

    // ---- Essay ungraded (EssayScore=null) → null (pending) ----
    [Fact]
    public void Essay_Ungraded_ReturnsNull()
    {
        var q = Q(1, "Essay", 100);
        var responses = new[] { Resp(1, essay: null) }; // EssayScore == null

        Assert.Null(AssessmentScoreAggregator.IsQuestionCorrect(q, responses));
    }

    // ============ Reproduksi ECG-02 (1) ============

    // ---- 4 MC (semua benar) + 2 Essay (EssayScore>0) → loop semua soal, sum(IsQuestionCorrect==true) == 6 ----
    [Fact]
    public void Repro_4Mc_2GradedEssay_SumsTo6()
    {
        var questions = new[]
        {
            Q(1, "MultipleChoice", 10, (10, true), (11, false)),
            Q(2, "MultipleChoice", 10, (20, true), (21, false)),
            Q(3, "MultipleChoice", 10, (30, true), (31, false)),
            Q(4, "MultipleChoice", 10, (40, true), (41, false)),
            Q(5, "Essay", 100), Q(6, "Essay", 100)
        };
        var allResponses = new[]
        {
            Resp(1, optId: 10), Resp(2, optId: 20), Resp(3, optId: 30), Resp(4, optId: 40),
            Resp(5, essay: 80), Resp(6, essay: 50)
        };

        int correct = questions.Count(q =>
            AssessmentScoreAggregator.IsQuestionCorrect(q, allResponses.Where(r => r.PackageQuestionId == q.Id)) == true);

        Assert.Equal(6, correct);
    }
}
