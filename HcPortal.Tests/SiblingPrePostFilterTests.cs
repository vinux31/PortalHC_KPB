// Phase 381 Plan 01 Task 1 — WSE-04 type-aware sibling isolation (D-01/D-09).
// Uji predikat single-source SiblingSessionQuery.SiblingPrePostAwarePredicate (Expression yang SAMA dipakai
// query EF di StartExam + ReshufflePackage + ReshuffleAll). Compile() → evaluasi in-memory (no DB, no Moq).
// Hanya PreTest/PostTest yang diisolasi; Standard/""/null dikelompokkan bersama (non-PrePost) → aman legacy.
using System;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class SiblingPrePostFilterTests
{
    private static readonly DateTime Sched = new DateTime(2026, 3, 10, 9, 0, 0);

    private static AssessmentSession S(int id, string? type, string title = "Welding", string category = "OJ") =>
        new AssessmentSession
        {
            Id = id, UserId = "u1", Title = title, Category = category, Schedule = Sched,
            Status = "Open", AccessToken = "", AssessmentType = type
        };

    private static Func<AssessmentSession, bool> Pred(string? type) =>
        SiblingSessionQuery.SiblingPrePostAwarePredicate("Welding", "OJ", Sched.Date, type).Compile();

    // Pre caller → match PreTest saja; Post & non-PrePost EXCLUDED.
    [Fact]
    public void PreCaller_IsolatesPreTest()
    {
        var pred = Pred("PreTest");
        Assert.True(pred(S(1, "PreTest")));
        Assert.False(pred(S(2, "PostTest")));
        Assert.False(pred(S(3, "Standard")));
        Assert.False(pred(S(4, "")));
        Assert.False(pred(S(5, null)));
    }

    // Post caller → match PostTest saja; Pre & non-PrePost EXCLUDED.
    [Fact]
    public void PostCaller_IsolatesPostTest()
    {
        var pred = Pred("PostTest");
        Assert.True(pred(S(1, "PostTest")));
        Assert.False(pred(S(2, "PreTest")));
        Assert.False(pred(S(3, "Standard")));
    }

    // Standard caller → match Standard DAN "" DAN null (non-PrePost satu grup / D-09 legacy-safe); Pre/Post EXCLUDED.
    [Fact]
    public void StandardCaller_GroupsAllNonPrePost()
    {
        var pred = Pred("Standard");
        Assert.True(pred(S(1, "Standard")));
        Assert.True(pred(S(2, "")));
        Assert.True(pred(S(3, null)));
        Assert.False(pred(S(4, "PreTest")));
        Assert.False(pred(S(5, "PostTest")));
    }

    // null caller (legacy) → match non-PrePost grup (Standard/""/null); Pre/Post EXCLUDED.
    [Fact]
    public void NullCaller_GroupsNonPrePost()
    {
        var pred = Pred(null);
        Assert.True(pred(S(1, null)));
        Assert.True(pred(S(2, "Standard")));
        Assert.True(pred(S(3, "")));
        Assert.False(pred(S(4, "PreTest")));
        Assert.False(pred(S(5, "PostTest")));
    }

    // Beda Title / Category / Schedule.Date → bukan sibling (false) terlepas type.
    [Fact]
    public void DifferentKey_IsNotSibling()
    {
        var preCaller = Pred("PreTest");
        Assert.False(preCaller(S(1, "PreTest", title: "Other")));
        Assert.False(preCaller(S(2, "PreTest", category: "IHT")));

        var stdCaller = Pred("Standard");
        Assert.False(stdCaller(S(3, "Standard", title: "Other")));
        Assert.False(stdCaller(S(4, "Standard", category: "IHT")));
    }
}
