using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.4 RTK-02 — pure unit tests for <see cref="RetakeArchiveBuilder"/>.
/// No DB, no fixture. Membuktikan verdict dibekukan via aggregator terpusat
/// (<see cref="AssessmentScoreAggregator.IsQuestionCorrect"/>, kill-drift) DAN essay disimpan
/// FULL-TEXT (Pitfall 2 — BUKAN truncate 300 char dari BuildAnswerCell — archive permanen per D-04).
/// </summary>
public class RetakeArchiveBuilderTests
{
    /// <summary>Helper: bangun PackageQuestion MultipleChoice dengan opsi (optId, correct).</summary>
    private static PackageQuestion Mc(int id, int scoreValue, params (int optId, bool correct)[] opts)
        => new PackageQuestion
        {
            Id = id,
            QuestionText = $"Q{id}",
            ScoreValue = scoreValue,
            QuestionType = "MultipleChoice",
            Options = opts.Select(o => new PackageOption
            {
                Id = o.optId,
                OptionText = $"opt{o.optId}",
                IsCorrect = o.correct
            }).ToList()
        };

    private static PackageQuestion Essay(int id, int scoreValue)
        => new PackageQuestion { Id = id, QuestionText = $"Q{id}", ScoreValue = scoreValue, QuestionType = "Essay" };

    [Fact]
    public void Build_MarksCorrectAndAwardsScore()
    {
        var q = Mc(10, 5, (100, true), (101, false));
        var responses = new List<PackageUserResponse>
        {
            new PackageUserResponse { PackageQuestionId = 10, PackageOptionId = 100 }
        };

        var rows = RetakeArchiveBuilder.Build(7, new[] { q }, responses);

        var row = Assert.Single(rows);
        Assert.Equal(7, row.AttemptHistoryId);
        Assert.Equal(10, row.PackageQuestionId);
        Assert.Equal("Q10", row.QuestionText);
        Assert.True(row.IsCorrect);
        Assert.Equal(5, row.AwardedScore);
        Assert.Equal("opt100", row.AnswerText);
    }

    [Fact]
    public void Build_WrongAnswer_ZeroScore_NotCorrect()
    {
        var q = Mc(10, 5, (100, true), (101, false));
        var responses = new List<PackageUserResponse>
        {
            new PackageUserResponse { PackageQuestionId = 10, PackageOptionId = 101 }
        };

        var row = Assert.Single(RetakeArchiveBuilder.Build(7, new[] { q }, responses));
        Assert.False(row.IsCorrect);
        Assert.Equal(0, row.AwardedScore);
        Assert.Equal("opt101", row.AnswerText);
    }

    [Fact]
    public void Build_EssayPending_NullCorrect_ZeroScore()
    {
        var q = Essay(20, 10);
        var responses = new List<PackageUserResponse>
        {
            new PackageUserResponse { PackageQuestionId = 20, TextAnswer = "jawaban panjang", EssayScore = null }
        };

        var row = Assert.Single(RetakeArchiveBuilder.Build(7, new[] { q }, responses));
        Assert.Null(row.IsCorrect);              // essay belum dinilai → pending
        Assert.Equal(0, row.AwardedScore);
        Assert.Equal("jawaban panjang", row.AnswerText);
    }

    // Pitfall 2 (WAJIB): essay > 300 char TIDAK boleh ter-truncate (archive permanen, D-04).
    [Fact]
    public void Build_EssayLongText_NotTruncated()
    {
        var longText = new string('x', 500);
        var q = Essay(20, 10);
        var responses = new List<PackageUserResponse>
        {
            new PackageUserResponse { PackageQuestionId = 20, TextAnswer = longText, EssayScore = 8 }
        };

        var row = Assert.Single(RetakeArchiveBuilder.Build(7, new[] { q }, responses));
        Assert.Equal(500, row.AnswerText!.Length);   // BUKAN 303 (truncate 300 + "...")
        Assert.Equal(longText, row.AnswerText);
        Assert.Equal(8, row.AwardedScore);           // essay → EssayScore
    }
}
