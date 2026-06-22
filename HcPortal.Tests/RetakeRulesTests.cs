using System;
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.4 RTK-03/13 — pure unit tests for <see cref="RetakeRules"/>.
/// No DB, no fixture (mirror <see cref="ShuffleToggleRulesTests"/>). Mengunci SEMUA cabang
/// kelayakan ujian ulang (allowRetake/PreTest/Manual/status/isPassed/cap/cooldown) +
/// visibilitas toggle. <c>CanRetake</c> PURE: terima <c>attemptsUsed</c> int sebagai param —
/// counting era-retake DB-aware (D-01) hidup di RetakeService (plan 405-03), bukan di sini.
/// </summary>
public class RetakeRulesTests
{
    // Fixed clock + default eligible-completedAt (25h ago > 24h cooldown).
    private static readonly DateTime Now = new DateTime(2026, 06, 19, 12, 00, 00, DateTimeKind.Utc);
    private static readonly DateTime EligibleCompletedAt = Now.AddHours(-25);

    /// <summary>Helper: panggil CanRetake dengan default semua-eligible, override per cabang.</summary>
    private static bool Can(
        bool allowRetake = true,
        string? assessmentType = "PostTest",
        bool isManualEntry = false,
        string status = "Completed",
        bool? isPassed = false,
        int attemptsUsed = 0,
        int maxAttempts = 2,
        int retakeCooldownHours = 24,
        DateTime? completedAt = null,
        DateTime? nowUtc = null)
        => RetakeRules.CanRetake(
            allowRetake, assessmentType, isManualEntry, status, isPassed,
            attemptsUsed, maxAttempts, retakeCooldownHours,
            completedAt ?? EligibleCompletedAt, nowUtc ?? Now);

    [Fact]
    public void Eligible_WhenAllConditionsMet()
        => Assert.True(Can());

    [Fact]
    public void Blocked_WhenAllowRetakeOff()
        => Assert.False(Can(allowRetake: false));

    [Fact]
    public void Blocked_WhenPreTest()
        => Assert.False(Can(assessmentType: "PreTest"));

    [Fact]
    public void Blocked_WhenManualEntry()
        => Assert.False(Can(isManualEntry: true));

    [Fact]
    public void Blocked_WhenNotCompleted()
        => Assert.False(Can(status: "InProgress"));

    [Fact]
    public void Blocked_WhenCancelled()
        => Assert.False(Can(status: "Cancelled"));

    [Fact]
    public void Blocked_WhenPassed()
        => Assert.False(Can(isPassed: true));

    [Fact]
    public void Blocked_WhenPendingGrading()
        => Assert.False(Can(isPassed: null));   // IsPassed null = PendingGrading → tak eligible

    [Fact]
    public void Blocked_WhenAttemptsExhausted()
        => Assert.False(Can(attemptsUsed: 2, maxAttempts: 2));

    [Fact]
    public void Blocked_WhenCooldownNotElapsed()
        => Assert.False(Can(completedAt: Now.AddHours(-1)));   // 1h < 24h cooldown

    [Fact]
    public void Eligible_WhenCooldownZero_NoWait()
        => Assert.True(Can(retakeCooldownHours: 0, completedAt: Now));   // 0 = no jeda

    [Fact]
    public void Eligible_NullAssessmentType_StandaloneGraded()
        => Assert.True(Can(assessmentType: null));

    // ShouldHideRetakeToggle = PreTest || ManualEntry (Proton TETAP retakeable — beda dari shuffle).
    [Theory]
    [InlineData("PreTest", false, true)]
    [InlineData(null, true, true)]
    [InlineData("PostTest", false, false)]
    [InlineData(null, false, false)]
    public void ShouldHideRetakeToggle_Cases(string? assessmentType, bool isManualEntry, bool expected)
        => Assert.Equal(expected, RetakeRules.ShouldHideRetakeToggle(assessmentType, isManualEntry));

    // ResolveReviewMode = tier feedback PURE 3-state, LEAK-SAFE (A1 orchestrator-locked).
    // Truth table: !allowReview → ScoreOnly; (belum-lulus: failed ATAU pending null) & attempt-sisa →
    // WrongFlagsOnly (tahan kunci selama retake MASIH MUNGKIN); passed | (belum-lulus & exhausted) →
    // FullReview (retake tak mungkin lagi). Pending(null) diperlakukan SAMA dengan failed.
    [Fact]
    public void Tier_ScoreOnly_WhenReviewDisabled()
        => Assert.Equal(RetakeReviewMode.ShowScoreOnly, RetakeRules.ResolveReviewMode(false, false, true));

    [Fact]
    public void Tier_WrongFlagsOnly_WhenFailedWithAttemptsLeft()
        => Assert.Equal(RetakeReviewMode.ShowWrongFlagsOnly, RetakeRules.ResolveReviewMode(true, false, true));

    [Fact]
    public void Tier_WrongFlagsOnly_WhenPendingNullWithAttemptsLeft()    // A1 leak-safe: pending+sisa = sembunyikan kunci
        => Assert.Equal(RetakeReviewMode.ShowWrongFlagsOnly, RetakeRules.ResolveReviewMode(true, null, true));

    [Fact]
    public void Tier_FullReview_WhenFailedExhausted()
        => Assert.Equal(RetakeReviewMode.ShowFullReview, RetakeRules.ResolveReviewMode(true, false, false));

    [Fact]
    public void Tier_FullReview_WhenPendingNullExhausted()    // pending tapi tak ada sisa → aman full
        => Assert.Equal(RetakeReviewMode.ShowFullReview, RetakeRules.ResolveReviewMode(true, null, false));

    [Fact]
    public void Tier_FullReview_WhenPassed()
        => Assert.Equal(RetakeReviewMode.ShowFullReview, RetakeRules.ResolveReviewMode(true, true, true));
}
