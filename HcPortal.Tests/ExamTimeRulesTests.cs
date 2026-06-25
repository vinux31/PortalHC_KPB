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

    // Phase 425 CLN-04 — parity: helper == formula inline lama di 4 situs CMPController (detik, int).
    // Membuktikan konsolidasi (DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60 → helper tanpa ubah nilai.
    [Theory]
    [InlineData(10, 5, 900)]      // :1191/:1564/:1642 dgn extra
    [InlineData(10, null, 600)]   // null extra
    [InlineData(0, 0, 0)]         // batas bawah
    [InlineData(60, 15, 4500)]    // durasi besar
    public void Parity_AllTimerSites(int duration, int? extra, int expected) =>
        Assert.Equal(expected, ExamTimeRules.AllowedExamSeconds(duration, extra));

    // Phase 425 CLN-04 — situs :4661 (menit→double): helper int di-assign ke double allowedSec;
    // buktikan numerik identik dgn formula lama (allowedMinutes * 60.0).
    [Theory]
    [InlineData(10, 5, 900.0)]
    [InlineData(10, null, 600.0)]
    public void Parity_DoubleSite_4661(int duration, int? extra, double expected)
    {
        double allowedSec = ExamTimeRules.AllowedExamSeconds(duration, extra);   // int→double implicit
        int allowedMinutesOld = duration + (extra ?? 0);
        double allowedSecOld = allowedMinutesOld * 60.0;                          // formula lama
        Assert.Equal(expected, allowedSec);
        Assert.Equal(allowedSecOld, allowedSec);                                 // parity eksplisit
    }
}
