using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.4 RTK-08 — pure unit tests untuk <see cref="RiwayatUnifier"/>.
/// No DB, no fixture (mirror <see cref="RetakeArchiveBuilderTests"/>). Membuktikan:
/// (1) urutan AttemptNumber DESC (terbaru dulu / current di atas), (2) current attempt ditandai
/// IsCurrent + AttemptNumber=max+1 + skor/lulus dari session live, (3) empty→empty,
/// (4) grouping per-soal STRICT by AttemptHistoryId (anti salah-attach), (5) provenance skor/lulus/tanggal.
/// </summary>
public class RiwayatUnifierTests
{
    // ---- POCO builders (no DB) ----

    private static AssessmentAttemptHistory Hist(
        int id, int attemptNumber, int? score = null, bool? isPassed = null, DateTime? completedAt = null)
        => new AssessmentAttemptHistory
        {
            Id = id,
            UserId = "u1",
            Title = "Asesmen X",
            Category = "Licencor",
            AttemptNumber = attemptNumber,
            Score = score,
            IsPassed = isPassed,
            CompletedAt = completedAt
        };

    private static AssessmentAttemptResponseArchive Row(int attemptHistoryId, int questionId, string q = "Q", string? a = "A")
        => new AssessmentAttemptResponseArchive
        {
            AttemptHistoryId = attemptHistoryId,
            PackageQuestionId = questionId,
            QuestionText = q,
            AnswerText = a,
            IsCorrect = true,
            AwardedScore = 1
        };

    private static AssessmentSession Session(
        int? score = null, bool? isPassed = null, DateTime? completedAt = null, string status = "Completed")
        => new AssessmentSession
        {
            Id = 99,
            UserId = "u1",
            Title = "Asesmen X",
            Category = "Licencor",
            Status = status,
            Score = score,
            IsPassed = isPassed,
            CompletedAt = completedAt
        };

    [Fact]
    public void Build_ArchivedOnly_OrdersDescending_GroupsByAttemptHistoryId()
    {
        // 2 attempt ter-arsip (AttemptNumber 1 & 2), masing-masing punya baris arsip via AttemptHistoryId.
        var h1 = Hist(id: 10, attemptNumber: 1, score: 40, isPassed: false, completedAt: new DateTime(2026, 1, 1));
        var h2 = Hist(id: 20, attemptNumber: 2, score: 55, isPassed: false, completedAt: new DateTime(2026, 2, 2));
        var archive = new List<AssessmentAttemptResponseArchive>
        {
            Row(10, 100), Row(10, 101),   // milik attempt 1
            Row(20, 200),                  // milik attempt 2
        };

        var result = RiwayatUnifier.Build(Session(), new[] { h1, h2 }, archive, currentRows: new List<AssessmentAttemptResponseArchive>());

        Assert.Equal(2, result.Count);
        // Terbaru dulu: AttemptNumber 2 lalu 1.
        Assert.Equal(2, result[0].AttemptNumber);
        Assert.Equal(1, result[1].AttemptNumber);
        Assert.All(result, vm => Assert.False(vm.IsCurrent));
        // Grouping strict by AttemptHistoryId.
        Assert.Single(result[0].Rows);                                   // attempt 2 → 1 baris (qid 200)
        Assert.Equal(200, result[0].Rows[0].PackageQuestionId);
        Assert.Equal(2, result[1].Rows.Count);                          // attempt 1 → 2 baris
        Assert.All(result[1].Rows, r => Assert.Equal(10, r.AttemptHistoryId));
    }

    [Fact]
    public void Build_WithCurrentRows_CompletedSession_AddsCurrentVm_NumberedMaxPlusOne_FloatsFirst()
    {
        var h1 = Hist(id: 10, attemptNumber: 1, score: 40, isPassed: false);
        var h2 = Hist(id: 20, attemptNumber: 2, score: 55, isPassed: false);
        var archive = new List<AssessmentAttemptResponseArchive> { Row(10, 100), Row(20, 200) };
        var session = Session(score: 88, isPassed: true, completedAt: new DateTime(2026, 3, 3), status: "Completed");
        var currentRows = new List<AssessmentAttemptResponseArchive>
        {
            Row(0, 300), Row(0, 301),   // sentinel AttemptHistoryId=0 (live, dari RetakeArchiveBuilder.Build(0,...))
        };

        var result = RiwayatUnifier.Build(session, new[] { h1, h2 }, archive, currentRows);

        Assert.Equal(3, result.Count);
        var currentVm = result[0];                 // current naik ke atas (AttemptNumber tertinggi)
        Assert.True(currentVm.IsCurrent);
        Assert.Equal(3, currentVm.AttemptNumber);  // max(2) + 1
        Assert.Equal(88, currentVm.ScorePercent);  // dari session live
        Assert.True(currentVm.IsPassed);
        Assert.Equal(new DateTime(2026, 3, 3), currentVm.CompletedAt);
        Assert.Equal(2, currentVm.Rows.Count);
        // sisanya ter-arsip, descending.
        Assert.Equal(2, result[1].AttemptNumber);
        Assert.Equal(1, result[2].AttemptNumber);
    }

    [Fact]
    public void Build_CurrentRows_NoArchive_NumbersCurrentAsOne()
    {
        var session = Session(score: 70, isPassed: true, status: "Completed");
        var currentRows = new List<AssessmentAttemptResponseArchive> { Row(0, 300) };

        var result = RiwayatUnifier.Build(
            session, Enumerable.Empty<AssessmentAttemptHistory>(),
            Enumerable.Empty<AssessmentAttemptResponseArchive>(), currentRows);

        var vm = Assert.Single(result);
        Assert.True(vm.IsCurrent);
        Assert.Equal(1, vm.AttemptNumber);   // tak ada arsip → 0 + 1
    }

    [Fact]
    public void Build_EmptyArchive_EmptyCurrent_ReturnsEmpty()
    {
        var result = RiwayatUnifier.Build(
            Session(),
            Enumerable.Empty<AssessmentAttemptHistory>(),
            Enumerable.Empty<AssessmentAttemptResponseArchive>(),
            Enumerable.Empty<AssessmentAttemptResponseArchive>());

        Assert.Empty(result);
    }

    [Fact]
    public void Build_ArchiveRow_WithUnmatchedAttemptHistoryId_NotAttachedToWrongAttempt()
    {
        var h1 = Hist(id: 10, attemptNumber: 1);
        // Baris arsip dengan AttemptHistoryId=999 yang TIDAK match history manapun → tak boleh nyangkut.
        var archive = new List<AssessmentAttemptResponseArchive> { Row(10, 100), Row(999, 500) };

        var result = RiwayatUnifier.Build(Session(), new[] { h1 }, archive, new List<AssessmentAttemptResponseArchive>());

        var vm = Assert.Single(result);
        Assert.Single(vm.Rows);                         // hanya baris AttemptHistoryId=10
        Assert.Equal(100, vm.Rows[0].PackageQuestionId);
        Assert.DoesNotContain(vm.Rows, r => r.PackageQuestionId == 500);
    }

    [Fact]
    public void Build_ScoreAndPassProvenance_FromHistoryForArchived()
    {
        var h1 = Hist(id: 10, attemptNumber: 1, score: 42, isPassed: false, completedAt: new DateTime(2026, 5, 6, 7, 8, 0));

        var result = RiwayatUnifier.Build(
            Session(), new[] { h1 },
            Enumerable.Empty<AssessmentAttemptResponseArchive>(),
            Enumerable.Empty<AssessmentAttemptResponseArchive>());

        var vm = Assert.Single(result);
        Assert.False(vm.IsCurrent);
        Assert.Equal(42, vm.ScorePercent);                             // history.Score
        Assert.False(vm.IsPassed);                                     // history.IsPassed
        Assert.Equal(new DateTime(2026, 5, 6, 7, 8, 0), vm.CompletedAt); // history.CompletedAt
        Assert.Empty(vm.Rows);                                         // tak ada arsip baris → list kosong (bukan null)
    }
}
