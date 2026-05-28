// Unit test SertifikatRow.DeriveCertificateStatus (Phase 327 P04 — Wave 0 baseline, signature DateTime? existing).
// 8 test case boundary coverage (6 Theory + 2 Fact) per CONTEXT.md D-14 + spec §7.5.
// Plan 04 akan refactor signature → DateOnly? + helper Today(int offset) sesuaikan.

using System;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class CertificateStatusTests
{
    // Helper: today UTC + offset hari, return DateTime midnight.
    // Plan 04 akan ubah return DateOnly.
    private static DateTime Today(int offset) =>
        DateTime.UtcNow.Date.AddDays(offset);

    [Theory]
    [InlineData(100, "Annual", CertificateStatus.Aktif)]           // > 30 hari = Aktif
    [InlineData(30, "Annual", CertificateStatus.AkanExpired)]      // boundary inclusive (days <= 30)
    [InlineData(1, "Annual", CertificateStatus.AkanExpired)]       // 1 hari lagi
    [InlineData(0, "Annual", CertificateStatus.AkanExpired)]       // hari ini (days = 0)
    [InlineData(-1, "Annual", CertificateStatus.Expired)]          // sudah lewat
    [InlineData(100, "Permanent", CertificateStatus.Permanent)]    // Permanent override
    public void DeriveCertificateStatus_VariousScenarios_ReturnsExpected(
        int offset, string certificateType, CertificateStatus expected)
    {
        var result = SertifikatRow.DeriveCertificateStatus(Today(offset), certificateType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired()
    {
        var result = SertifikatRow.DeriveCertificateStatus(null, null);
        Assert.Equal(CertificateStatus.Expired, result);
    }

    [Fact]
    public void DeriveCertificateStatus_NullValidUntil_Permanent_ReturnsPermanent()
    {
        var result = SertifikatRow.DeriveCertificateStatus(null, "Permanent");
        Assert.Equal(CertificateStatus.Permanent, result);
    }
}
