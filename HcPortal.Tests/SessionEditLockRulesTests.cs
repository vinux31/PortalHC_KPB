using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 D-07/SHFX-03 — pure unit tests untuk <see cref="SessionEditLockRules.IsSessionEditLocked"/>.
/// No DB, no fixture. Truth-table AssessmentType x SamePackage: lock HANYA untuk Post-Test ber-SamePackage.
/// Backward-compat: Pre/Standard/Post-non-same TIDAK terkunci.
/// </summary>
public class SessionEditLockRulesTests
{
    [Theory]
    [InlineData("PostTest", true, true)]    // Post-Test + SamePackage -> terkunci
    [InlineData("PostTest", false, false)]  // Post-Test tanpa SamePackage -> bebas
    [InlineData("PreTest", true, false)]    // Pre-Test (sumber edit) -> bebas walau SamePackage
    [InlineData("PreTest", false, false)]
    [InlineData("Standard", false, false)]  // Standard (non Pre-Post) -> bebas
    [InlineData("Standard", true, false)]   // Standard tak relevan SamePackage -> bebas
    [InlineData(null, true, false)]         // tipe null (legacy) -> bebas
    public void IsSessionEditLocked_TruthTable(string? assessmentType, bool samePackage, bool expected)
    {
        var session = new AssessmentSession { AssessmentType = assessmentType, SamePackage = samePackage };
        Assert.Equal(expected, SessionEditLockRules.IsSessionEditLocked(session));
    }
}
