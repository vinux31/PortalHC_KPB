using System.Collections.Generic;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 359 (PCOMP-07) — predikat pure cross-year gate. Tanpa DbContext (lihat ProtonYearGateIntegrationTests
/// untuk jembatan DB IsPrevYearPassedAsync).
/// </summary>
public class ProtonYearGateTests
{
    [Fact] // Tahun 1 (prevTahunKe == null) → selalu allowed, tanpa prasyarat (D-03)
    public void Year1_NullPrev_Allowed() =>
        Assert.True(ProtonYearGate.IsAllowed(null, new string[0]));

    [Fact] // Tahun 2: prev "Tahun 1" ada di passed → allowed
    public void Year2_PrevYear1Passed_Allowed() =>
        Assert.True(ProtonYearGate.IsAllowed("Tahun 1", new[] { "Tahun 1" }));

    [Fact] // Tahun 3: prev "Tahun 2" belum lulus (cuma Tahun 1) → blocked
    public void Year3_PrevYear2NotPassed_Blocked() =>
        Assert.False(ProtonYearGate.IsAllowed("Tahun 2", new[] { "Tahun 1" }));

    [Fact] // prev "Tahun 1", passedYears kosong → blocked
    public void EmptyPassed_Blocked() =>
        Assert.False(ProtonYearGate.IsAllowed("Tahun 1", new string[0]));

    [Fact] // prev "Tahun 1", passedYears null → blocked (null-safe)
    public void NullPassed_Blocked() =>
        Assert.False(ProtonYearGate.IsAllowed("Tahun 1", null));

    [Fact] // trim dua sisi: " Tahun 1 " match "Tahun 1"
    public void Whitespace_Trimmed_Allowed() =>
        Assert.True(ProtonYearGate.IsAllowed("Tahun 1", new[] { " Tahun 1 " }));
}
