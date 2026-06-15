// Phase 380 (WSE-02 / TOK-01) — defensive both-sides token compare (D-01a).
// Tests the pure CMPController.AccessTokenMatches helper that backs the VerifyToken gate (:876).
// Controller construction is infeasible (14-dep ctor); the compare is exercised via the pure helper.
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class VerifyTokenTests
{
    [Fact] // WSE-02: stored LOWERCASE token (admin edited lowercase) matches uppercased input → auto-heal
    public void VerifyToken_StoredLowercase_MatchesUppercasedInput()
    {
        // Client force-uppercases input (Assessment.cshtml:757) → "ABC23X"; stored is legacy lowercase.
        Assert.True(CMPController.AccessTokenMatches("abc23x", "ABC23X"));
    }

    [Fact] // WSE-02: lowercase on BOTH sides still matches (defensive)
    public void VerifyToken_BothLowercase_Matches()
    {
        Assert.True(CMPController.AccessTokenMatches("abc23x", "abc23x"));
    }

    [Fact] // WSE-02: surrounding whitespace tolerated (Trim) on either side
    public void VerifyToken_WhitespacePadded_Matches()
    {
        Assert.True(CMPController.AccessTokenMatches("  ABC23X  ", "abc23x"));
    }

    [Fact] // negative: genuinely different tokens do NOT match
    public void VerifyToken_DifferentToken_DoesNotMatch()
    {
        Assert.False(CMPController.AccessTokenMatches("ABC23X", "WRONG99"));
    }

    [Fact] // null/empty stored never matches a non-empty input
    public void VerifyToken_NullStored_DoesNotMatchInput()
    {
        Assert.False(CMPController.AccessTokenMatches(null, "ABC23X"));
    }
}
