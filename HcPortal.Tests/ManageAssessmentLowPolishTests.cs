// Phase 349 P05 — Nyquist test logic-bearing untuk 2 fix MAP yang punya predicate testable.
//
// Cakupan xUnit (logic-bearing ter-otomasi):
//   - MAP-13: TotalCount exclude Cancelled (progress bar Monitoring list bisa 100%) — predicate Count(status != Cancelled).
//   - MAP-23: search Monitoring list extend ke Category — predicate Title || Category Contains (case-insensitive).
//
// Predicate di-mirror PERSIS dari controller AssessmentMonitoring (Plan 03):
//   standard/Pre-Post: TotalCount = sessions.Count(a => a.Status != AssessmentConstants.AssessmentStatus.Cancelled)
//   search:            query.Where(a => a.Title.ToLower().Contains(lower) || a.Category.ToLower().Contains(lower))
//
// Sisanya (MAP-01/02/03/04/05/06/07/08/09/10/11/12/14/15/16/17/18/19/20/21/22 = i18n/a11y/empty-state/
// display/markup/signature) di-cover via grep acceptance (Plan 01-05) + Playwright UAT (Plan 05 Task 3).

using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class ManageAssessmentLowPolishTests
{
    // ===== MAP-13: TotalCount exclude Cancelled =====

    // Mirror predicate controller: Count(a => a.Status != Cancelled). Konstanta D-C, BUKAN literal.
    private static int CountExcludingCancelled(IEnumerable<string> statuses) =>
        statuses.Count(s => s != AssessmentConstants.AssessmentStatus.Cancelled);

    [Fact]
    public void Map13_MixedStatuses_ExcludeCancelled_CountsNonCancelledOnly()
    {
        // 5 session: 3 Completed, 1 InProgress, 1 Cancelled -> exclude = 4
        var statuses = new[]
        {
            AssessmentConstants.AssessmentStatus.Completed,
            AssessmentConstants.AssessmentStatus.Completed,
            AssessmentConstants.AssessmentStatus.Completed,
            AssessmentConstants.AssessmentStatus.InProgress,
            AssessmentConstants.AssessmentStatus.Cancelled
        };
        Assert.Equal(4, CountExcludingCancelled(statuses));
    }

    [Fact]
    public void Map13_AllCancelled_ExcludeCancelled_ReturnsZero_NoCrash()
    {
        var statuses = new[]
        {
            AssessmentConstants.AssessmentStatus.Cancelled,
            AssessmentConstants.AssessmentStatus.Cancelled,
            AssessmentConstants.AssessmentStatus.Cancelled
        };
        Assert.Equal(0, CountExcludingCancelled(statuses));  // progress N/A (denominator 0), tak crash
    }

    [Fact]
    public void Map13_NoCancelled_ExcludeCancelled_ParityWithOldTotalCount()
    {
        var statuses = new[]
        {
            AssessmentConstants.AssessmentStatus.Completed,
            AssessmentConstants.AssessmentStatus.InProgress,
            AssessmentConstants.AssessmentStatus.Open,
            AssessmentConstants.AssessmentStatus.Upcoming
        };
        Assert.Equal(statuses.Length, CountExcludingCancelled(statuses));  // parity g.Count() lama saat 0 Cancelled
    }

    [Fact]
    public void Map13_AllNonCancelledCompleted_Progress100Percent()
    {
        // Inti MAP-13: progress = completed / totalExcludeCancelled. Semua non-Cancelled completed -> 100%.
        // Tanpa exclude (denominator 3) hanya 66% -> bug lama yang diperbaiki.
        var statuses = new[]
        {
            AssessmentConstants.AssessmentStatus.Completed,
            AssessmentConstants.AssessmentStatus.Completed,
            AssessmentConstants.AssessmentStatus.Cancelled   // di-exclude dari denominator
        };
        int total = CountExcludingCancelled(statuses);  // 2
        int completed = statuses.Count(s => s == AssessmentConstants.AssessmentStatus.Completed);  // 2
        int progressPct = total > 0 ? (int)((double)completed / total * 100) : 0;
        Assert.Equal(100, progressPct);
    }

    // ===== MAP-23: search Title || Category (case-insensitive) =====

    private record AssessmentStub(string Title, string Category);

    // Mirror predicate controller search (Plan 03 MAP-23).
    private static List<AssessmentStub> SearchTitleOrCategory(IEnumerable<AssessmentStub> items, string search)
    {
        var lower = search.ToLower();
        return items.Where(a => a.Title.ToLower().Contains(lower)
                             || a.Category.ToLower().Contains(lower)).ToList();
    }

    private static readonly List<AssessmentStub> SampleAssessments = new()
    {
        new("Ujian Operator Kilang", "Kepemimpinan"),  // match "kepemimpinan" via Category SAJA (Title tak punya)
        new("Asesmen Keselamatan", "HSSE"),
        new("Evaluasi Lini Manajemen", "Manajemen")
    };

    [Fact]
    public void Map23_SearchMatchesCategoryOnly_ReturnsRow()
    {
        // "kepemimpinan" hanya cocok via Category row 1 (Title="Ujian Operator Kilang" tak mengandungnya).
        var result = SearchTitleOrCategory(SampleAssessments, "kepemimpinan");
        Assert.Single(result);
        Assert.Equal("Kepemimpinan", result[0].Category);
    }

    [Fact]
    public void Map23_SearchMatchesTitle_RegressionPreserved()
    {
        // Perilaku lama (match Title) tetap jalan.
        var result = SearchTitleOrCategory(SampleAssessments, "operator");
        Assert.Single(result);
        Assert.Equal("Ujian Operator Kilang", result[0].Title);
    }

    [Fact]
    public void Map23_SearchNoMatch_ReturnsEmpty()
    {
        var result = SearchTitleOrCategory(SampleAssessments, "xyz-tidak-ada");
        Assert.Empty(result);
    }
}
