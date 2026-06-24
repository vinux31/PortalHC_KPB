// Phase 424 GRDF-05 — ExamTimeRules.AllowedExamSeconds = (duration + extra) * 60. Pure, no DB.
using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

public class ExamTimeRulesTests
{
    [Fact]
    public void WithExtraTime_AddsBeforeMultiply() =>
        Assert.Equal(900, ExamTimeRules.AllowedExamSeconds(10, 5));   // (10+5)*60

    [Fact]
    public void NullExtraTime_TreatedAsZero() =>
        Assert.Equal(600, ExamTimeRules.AllowedExamSeconds(10, null)); // (10+0)*60

    [Fact]
    public void ZeroAll_IsZero() =>
        Assert.Equal(0, ExamTimeRules.AllowedExamSeconds(0, 0));
}
