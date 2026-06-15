using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 383 ECG-02/03 — pure regression unit tests yang MENGUNCI reproduksi bug user 2026-06-15
/// ("Nilai Anda 100% tapi (4/6 benar)"). Me-mirror logika count CMPController.Results (loop soal,
/// sum IsQuestionCorrect==true) dan Elemen Teknis (group by ElemenTeknis, count IsQuestionCorrect==true)
/// memakai helper terpusat <see cref="AssessmentScoreAggregator.IsQuestionCorrect"/>.
/// No DB, no fixture, bukan Category=Integration. Builder Q/Resp di-copy verbatim dari
/// AssessmentScoreAggregatorTests (Plan 01). Sebelum fix: essay graded dihitung salah → count 4/6, ET 0.
/// </summary>
public class ResultsEssayCorrectnessTests
{
    // ---- in-memory builders (no DB) — verbatim AssessmentScoreAggregatorTests:20-30 ----
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

    // ECG-02 — 4 MC (benar) + 2 essay graded >0 → CorrectAnswers == 6 (BUKAN 4). Mengunci reproduksi 6/6.
    [Fact]
    public void Count_4Mc_2GradedEssay_Equals6()
    {
        var questions = new[]
        {
            Q(1, "MultipleChoice", 10, (10, true), (11, false)),
            Q(2, "MultipleChoice", 10, (20, true), (21, false)),
            Q(3, "MultipleChoice", 10, (30, true), (31, false)),
            Q(4, "MultipleChoice", 10, (40, true), (41, false)),
            Q(5, "Essay", 100),
            Q(6, "Essay", 100)
        };
        var responses = new[]
        {
            Resp(1, optId: 10), Resp(2, optId: 20), Resp(3, optId: 30), Resp(4, optId: 40),
            Resp(5, essay: 80), Resp(6, essay: 50)
        };

        // Mirror CMPController.Results count: loop soal, sum IsQuestionCorrect==true.
        int correctCount = questions.Count(q =>
            AssessmentScoreAggregator.IsQuestionCorrect(q, responses.Where(r => r.PackageQuestionId == q.Id)) == true);

        Assert.Equal(6, correctCount); // 6/6 — bukan 4/6
    }

    // ECG-03 — Elemen Teknis grup hitung essay benar (predicate IsQuestionCorrect==true).
    [Fact]
    public void ElemenTeknis_GroupCountsEssay()
    {
        var q1 = Q(1, "MultipleChoice", 10, (10, true), (11, false));
        q1.ElemenTeknis = "Teknik A";
        var q2 = Q(2, "Essay", 100);
        q2.ElemenTeknis = "Teknik A";
        var questions = new[] { q1, q2 };
        var responses = new[] { Resp(1, optId: 10), Resp(2, essay: 75) };

        // Mirror CMPController.Results Elemen Teknis: group by ElemenTeknis, count IsQuestionCorrect==true.
        var groups = questions.GroupBy(q => q.ElemenTeknis);
        var teknikA = groups.First(g => g.Key == "Teknik A");
        int correct = teknikA.Count(q =>
            AssessmentScoreAggregator.IsQuestionCorrect(q, responses.Where(r => r.PackageQuestionId == q.Id)) == true);

        Assert.Equal(2, correct); // MC benar + essay benar
    }

    // ECG-02 boundary — essay EssayScore==0 → Salah (tidak dihitung benar).
    [Fact]
    public void Essay_ScoreZero_NotCounted()
    {
        var q = Q(1, "Essay", 100);
        var responses = new[] { Resp(1, essay: 0) };

        Assert.False(AssessmentScoreAggregator.IsQuestionCorrect(q, responses) == true);
    }

    // ECG-04 boundary — essay belum dinilai (EssayScore==null) → null (pending "Menunggu Penilaian").
    [Fact]
    public void Essay_Ungraded_ReturnsNullPending()
    {
        var q = Q(1, "Essay", 100);
        var responses = new[] { Resp(1) };

        Assert.Null(AssessmentScoreAggregator.IsQuestionCorrect(q, responses));
    }
}
