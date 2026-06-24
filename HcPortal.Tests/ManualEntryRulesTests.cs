// Phase 425 CLN-02 (FLD-5.2-04/05) — ManualEntryRules.PassStatusMismatch cross-validate entry manual.
// Pure, EF-free; mismatch->true / match->false / null->false / boundary Score==Pass->false.
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

public class ManualEntryRulesTests
{
    [Theory]
    [InlineData(80, 70, true, false)]    // selaras lulus
    [InlineData(60, 70, false, false)]   // selaras tidak-lulus
    [InlineData(60, 70, true, true)]     // mismatch: ditandai Lulus, Score<Pass
    [InlineData(80, 70, false, true)]    // mismatch: ditandai Tidak Lulus, Score>=Pass
    [InlineData(70, 70, true, false)]    // boundary == Pass => lulus
    public void PassStatusMismatch_ScoreProvided(int score, int pass, bool isPassed, bool expected) =>
        Assert.Equal(expected, ManualEntryRules.PassStatusMismatch(score, pass, isPassed));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PassStatusMismatch_ScoreNull_NeverMismatch(bool isPassed) =>
        Assert.False(ManualEntryRules.PassStatusMismatch(null, 70, isPassed));
}
