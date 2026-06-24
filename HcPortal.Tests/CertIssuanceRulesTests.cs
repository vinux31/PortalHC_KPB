using System;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 423 (CERT-01/02/04/06/07) — pure unit tests untuk <see cref="CertIssuanceRules"/>.
/// No DB, no fixture, no [Trait] (selalu jalan di CI). Truth-table:
/// ShouldIssueCertificate (PreTest selalu tolak), DeriveValidUntil (Permanent/Annual/3-Year/non-kanonik),
/// ResemblesAutoCertFormat (regex namespace-guard), PendingAgeBadgeClass (ambang >3/>7 hari).
/// Analog SessionEditLockRulesTests (fase 422).
/// </summary>
public class CertIssuanceRulesTests
{
    // CERT-01 (D-01) — gate kelayakan: PreTest SELALU tolak; wajib GenerateCertificate && IsPassed==true.
    [Theory]
    [InlineData("PreTest", true, true, false)]   // PreTest selalu tolak walau lulus + generate
    [InlineData("PostTest", true, true, true)]   // Post-Test lulus + generate -> terbit
    [InlineData("PostTest", false, true, false)] // GenerateCertificate=false -> tolak
    [InlineData("PostTest", true, false, false)] // IsPassed=false -> tolak
    [InlineData("Standard", true, true, true)]   // Standard (non Pre) -> terbit
    [InlineData("Manual", true, true, true)]     // Manual bukan PreTest -> terbit
    [InlineData(null, true, true, true)]         // tipe null (legacy) bukan PreTest -> terbit
    public void ShouldIssueCertificate_TruthTable(string? assessmentType, bool generate, bool passed, bool expected)
    {
        var session = new AssessmentSession
        {
            AssessmentType = assessmentType,
            GenerateCertificate = generate,
            IsPassed = passed
        };
        Assert.Equal(expected, CertIssuanceRules.ShouldIssueCertificate(session));
    }

    // CERT-01 — IsPassed null (bool?) != true -> tolak. Dipisah [Fact] supaya InlineData tetap bool sederhana.
    [Fact]
    public void ShouldIssueCertificate_IsPassedNull_ReturnsFalse()
    {
        var session = new AssessmentSession
        {
            AssessmentType = "PostTest",
            GenerateCertificate = true,
            IsPassed = (bool?)null
        };
        Assert.False(CertIssuanceRules.ShouldIssueCertificate(session));
    }

    // CERT-02/06 (D-04/D-05/D-10) — derive ValidUntil dari CompletedAt utk CertificateType KANONIK saja.
    [Fact]
    public void DeriveValidUntil_FromCompletedAt()
    {
        var completedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc);

        // Permanent -> null (D-04)
        Assert.Null(CertIssuanceRules.DeriveValidUntil("Permanent", completedAt));
        // Annual -> +1 tahun
        Assert.Equal(DateOnly.FromDateTime(completedAt).AddYears(1),
            CertIssuanceRules.DeriveValidUntil("Annual", completedAt));
        // 3-Year -> +3 tahun
        Assert.Equal(DateOnly.FromDateTime(completedAt).AddYears(3),
            CertIssuanceRules.DeriveValidUntil("3-Year", completedAt));
        // Non-kanonik (Kompetensi) -> null (helper TIDAK derive; caller passthrough, D-10)
        Assert.Null(CertIssuanceRules.DeriveValidUntil("Kompetensi", completedAt));
    }

    // CERT-02 — CompletedAt null -> null (tidak bisa derive tanpa tanggal dasar).
    [Fact]
    public void DeriveValidUntil_CompletedAtNull_ReturnsNull()
    {
        Assert.Null(CertIssuanceRules.DeriveValidUntil("Annual", null));
    }

    // CERT-04 (D-02) — nomor manual TIDAK boleh menyerupai format auto KPB/{seq:D3}/{ROMAN}/{YEAR}.
    [Theory]
    [InlineData("KPB/005/VI/2026", true)]    // format auto persis
    [InlineData("KPB/001/I/2026", true)]     // bulan I (roman valid)
    [InlineData("KPB/12/VI/2026", false)]    // seq bukan 3-digit
    [InlineData("KPB/005/6/2026", false)]    // bulan bukan roman
    [InlineData("KPB/005/VI/26", false)]     // tahun bukan 4-digit
    [InlineData("SERT/MANUAL/001", false)]   // free-text manual lain
    [InlineData("KPB-005-VI-2026", false)]   // separator beda
    public void ResemblesAutoCertFormat_Regex(string? nomor, bool expected)
    {
        Assert.Equal(expected, CertIssuanceRules.ResemblesAutoCertFormat(nomor));
    }

    // CERT-04 — null/empty/whitespace -> false (bukan format auto).
    [Fact]
    public void ResemblesAutoCertFormat_NullEmptyWhitespace_ReturnsFalse()
    {
        Assert.False(CertIssuanceRules.ResemblesAutoCertFormat(null));
        Assert.False(CertIssuanceRules.ResemblesAutoCertFormat(""));
        Assert.False(CertIssuanceRules.ResemblesAutoCertFormat("   "));
    }

    // CERT-07 (D-09) — umur PendingGrading -> bootstrap badge class. >7=merah, >3=kuning, <=3=abu.
    [Fact]
    public void PendingAgeBadgeClass_Thresholds()
    {
        var now = new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal("bg-danger", CertIssuanceRules.PendingAgeBadgeClass(now.AddDays(-8), now));        // >7 hari
        Assert.Equal("bg-warning text-dark", CertIssuanceRules.PendingAgeBadgeClass(now.AddDays(-5), now)); // >3 hari
        Assert.Equal("bg-secondary", CertIssuanceRules.PendingAgeBadgeClass(now.AddDays(-2), now));     // <=3 hari
        Assert.Equal("bg-secondary", CertIssuanceRules.PendingAgeBadgeClass(null, now));               // null -> default
    }

    // CERT-07 — boundary tepat di ambang (>7 strict, >3 strict).
    [Fact]
    public void PendingAgeBadgeClass_BoundaryExact()
    {
        var now = new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc);

        // tepat 7 hari (tidak > 7) -> masih kuning (>3)
        Assert.Equal("bg-warning text-dark", CertIssuanceRules.PendingAgeBadgeClass(now.AddDays(-7), now));
        // tepat 3 hari (tidak > 3) -> abu
        Assert.Equal("bg-secondary", CertIssuanceRules.PendingAgeBadgeClass(now.AddDays(-3), now));
    }
}
