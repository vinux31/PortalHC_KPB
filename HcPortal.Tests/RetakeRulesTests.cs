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
        DateTime? nowUtc = null,
        DateTime? examWindowCloseDate = null)
        => RetakeRules.CanRetake(
            allowRetake, assessmentType, isManualEntry, status, isPassed,
            attemptsUsed, maxAttempts, retakeCooldownHours,
            completedAt ?? EligibleCompletedAt, nowUtc ?? Now, examWindowCloseDate);

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

    // v32.7 RTH-01 (RTK-LOGIC-02) — window gate +7h WIB. now+7h > EWCD → false (retake mustahil).
    [Fact]
    public void Eligible_WhenWindowOpen()                        // EWCD jauh di masa depan → tak ada gate
        => Assert.True(Can(examWindowCloseDate: Now.AddDays(30)));

    [Fact]
    public void Blocked_WhenWindowClosed()                       // now+7h (19:00) > EWCD (Now-1h) → gate aktif
        => Assert.False(Can(examWindowCloseDate: Now.AddHours(-1)));

    [Fact]
    public void Eligible_WhenWindowNull_NoGate()                 // EWCD null → backward-compat, tak ada gate
        => Assert.True(Can(examWindowCloseDate: null));

    [Fact]
    public void Blocked_WhenWindowBoundary_NowPlus7h()           // EWCD == Now → now+7h (19:00) > 12:00 → false (sisi +7h)
        => Assert.False(Can(examWindowCloseDate: Now));

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

    // v32.7 RTH-01/D-02 — CooldownMayExceedWindow: peringatan dini cooldown bisa lewat ExamWindowCloseDate.
    // nowWib = Now + 7h (= 2026-06-19 19:00 UTC pada fixed clock).
    [Fact]
    public void CooldownMayExceedWindow_NullWindow_False()
        => Assert.False(RetakeRules.CooldownMayExceedWindow(Now, null, 24));

    [Fact]
    public void CooldownMayExceedWindow_ZeroCooldown_False()
        => Assert.False(RetakeRules.CooldownMayExceedWindow(Now, Now.AddDays(1), 0));

    [Fact]
    public void CooldownMayExceedWindow_WindowAlreadyClosed_False()   // nowWib(19:00) > EWCD(12:00) → urusan gate D-01
        => Assert.False(RetakeRules.CooldownMayExceedWindow(Now, Now, 24));

    [Fact]
    public void CooldownMayExceedWindow_OpenButCooldownExceeds_True() // EWCD=Now+8h(20:00) open; nowWib+2h(21:00) > 20:00
        => Assert.True(RetakeRules.CooldownMayExceedWindow(Now, Now.AddHours(8), 2));

    [Fact]
    public void CooldownMayExceedWindow_OpenAndCooldownFits_False()   // EWCD=Now+17h; nowWib+2h(Now+9h) <= EWCD
        => Assert.False(RetakeRules.CooldownMayExceedWindow(Now, Now.AddHours(17), 2));

    [Fact]
    public void CooldownMayExceedWindow_Boundary_Plus7h_Sensitive()   // EWCD=Now+7h30m: +7h→open→true; +8h→tutup→false (kill-drift)
        => Assert.True(RetakeRules.CooldownMayExceedWindow(Now, Now.AddHours(7).AddMinutes(30), 1));
}
