// Phase 367 Plan 04 Task 1 — #18 DeleteAssessmentGroup sibling no over-match.
// Uji predikat single-source AssessmentAdminController.StandardGroupSiblingPredicate (Expression yang SAMA dipakai query EF).
// Compile() → evaluasi in-memory: manual/Pre-Post/linked EXCLUDED, online standard ikut.
using System;
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class SiblingFilterTests
{
    private static readonly DateTime Sched = new DateTime(2026, 3, 10, 9, 0, 0);

    private static AssessmentSession S(int id, bool manual = false, string? type = null, int? linkedGroup = null, string title = "Welding", string category = "OJ") =>
        new AssessmentSession
        {
            Id = id, UserId = "u1", Title = title, Category = category, Schedule = Sched, Status = "Completed",
            AccessToken = "", IsManualEntry = manual, AssessmentType = type, LinkedGroupId = linkedGroup
        };

    private static Func<AssessmentSession, bool> Pred() =>
        AssessmentAdminController.StandardGroupSiblingPredicate("Welding", "OJ", Sched.Date).Compile();

    // 2 online standard + 1 manual sama Title/Category/Tanggal → manual EXCLUDED.
    [Fact]
    public void Sibling_ExcludesManual()
    {
        var pred = Pred();
        Assert.True(pred(S(1)));                 // online standard
        Assert.True(pred(S(2)));                 // online standard
        Assert.False(pred(S(3, manual: true)));  // manual → EXCLUDED
    }

    // Pre/Post group + linked judul sama → EXCLUDED dari sibling standard.
    [Fact]
    public void Sibling_ExcludesPrePostAndLinked()
    {
        var pred = Pred();
        Assert.False(pred(S(1, type: "PreTest")));
        Assert.False(pred(S(2, type: "PostTest")));
        Assert.False(pred(S(3, linkedGroup: 99)));
    }

    // Semua online standard match ikut (no false-negative); beda key → bukan sibling.
    [Fact]
    public void Sibling_IncludesAllStandardOnline_ExcludesDifferentKey()
    {
        var pred = Pred();
        Assert.True(pred(S(1)));
        Assert.True(pred(S(2)));
        Assert.False(pred(S(3, title: "Other")));
        Assert.False(pred(S(4, category: "IHT")));
    }
}
