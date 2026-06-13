using HcPortal.Helpers;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 374 SHUF-11/12/14 — pure unit tests for <see cref="ShuffleToggleRules"/>.
/// No DB, no fixture. Mengunci decision-logic lock/hide/warning yang dipakai
/// GET ViewBag dan POST guard (single-source, cegah Pitfall 2 divergensi).
/// </summary>
public class ShuffleToggleRulesTests
{
    // SHUF-11: isLocked = anyStarted || anyAssignment.
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, true)]
    public void Lock_OrLogic(bool anyStarted, bool anyAssignment, bool expected)
    {
        Assert.Equal(expected, ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment));
    }

    // SHUF-14: hide untuk Proton Tahun 3 ATAU Manual entry.
    [Theory]
    [InlineData("Assessment Proton", "Tahun 3", false, true)]
    [InlineData("Assessment Proton", "Tahun 2", false, false)]
    [InlineData("Assessment OJT", null, false, false)]
    [InlineData("Assessment OJT", null, true, true)]
    [InlineData("Assessment Proton", "Tahun 1", true, true)]
    public void Hide_ProtonTahun3OrManual(string? category, string? tahunKe, bool isManualEntry, bool expected)
    {
        Assert.Equal(expected, ShuffleToggleRules.ShouldHideShuffleToggle(category, tahunKe, isManualEntry));
    }

    // SHUF-12: warning hanya bila >=2 paket-ber-soal AND Acak Soal OFF AND mismatch.
    [Theory]
    [InlineData(2, false, true, true)]
    [InlineData(2, true, true, false)]
    [InlineData(1, false, true, false)]
    [InlineData(2, false, false, false)]
    [InlineData(3, false, true, true)]
    public void Warning_Predicate(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch, bool expected)
    {
        Assert.Equal(expected, ShuffleToggleRules.ShouldShowSizeMismatchWarning(packagesWithQuestions, shuffleQuestions, hasMismatch));
    }
}
