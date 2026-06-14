// Unit test SertifikatRow.DeriveCertificateStatus (Phase 327 P04 — Plan 04 GREEN post signature flip DateOnly?).
// 8 test case boundary coverage (6 Theory + 2 Fact) per CONTEXT.md D-14 + spec §7.5.
// Helper Today(int offset) return DateOnly cocok dengan signature baru.

using System;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class CertificateStatusTests
{
    // Helper: today UTC + offset hari, return DateOnly (Plan 04 GREEN post-flip).
    private static DateOnly Today(int offset) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);

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

    // Phase 382 CERT-01 (D-08): cert lulus tanpa kedaluwarsa (ValidUntil=null, certificateType non-Permanent)
    // = Aktif/Permanen (BUKAN Expired). Single-source flip — consumer ikut via Status enum. REWRITE dari _ReturnsExpired.
    [Fact]
    public void DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsAktif()
    {
        var result = SertifikatRow.DeriveCertificateStatus(null, null);
        Assert.Equal(CertificateStatus.Aktif, result);
    }

    [Fact]
    public void DeriveCertificateStatus_NullValidUntil_Permanent_ReturnsPermanent()
    {
        var result = SertifikatRow.DeriveCertificateStatus(null, "Permanent");
        Assert.Equal(CertificateStatus.Permanent, result);
    }
}
