using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

public class CoacheeEligibilityCalculatorTests
{
    [Fact] // unit A: 3/3 Approved (expectedCount=3) → eligible
    public void FullApproved_Eligible() =>
        Assert.True(CoacheeEligibilityCalculator.IsEligiblePerUnit(
            new[] { "Approved", "Approved", "Approved" }, 3));

    [Fact] // unit B: 0 progress (expectedCount=1) → NOT eligible
    public void ZeroProgress_NotEligible() =>
        Assert.False(CoacheeEligibilityCalculator.IsEligiblePerUnit(new string[0], 1));

    [Fact] // sebagian (2/3 Approved, 1 Pending) → NOT eligible
    public void PartialApproved_NotEligible() =>
        Assert.False(CoacheeEligibilityCalculator.IsEligiblePerUnit(
            new[] { "Approved", "Approved", "Pending" }, 3));

    [Fact] // expectedCount==0 → NOT eligible (Tahun 3 ditangani di call-site, bukan helper)
    public void ExpectedCountZero_NotEligible() =>
        Assert.False(CoacheeEligibilityCalculator.IsEligiblePerUnit(new string[0], 0));
}
