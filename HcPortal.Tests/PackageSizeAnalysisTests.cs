using System.Collections.Generic;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 D-05/SHFX-07 — pure unit tests untuk <see cref="PackageSizeAnalysis.Compute"/>.
/// No DB, no fixture. Mengunci paritas hasMismatch/refCount/withQ yang dipakai GET ViewBag
/// (single-source, ganti duplikasi controller :5844-5856 + view :72-78 — cegah drift SHUF-ISS-07).
/// </summary>
public class PackageSizeAnalysisTests
{
    private static AssessmentPackage Pkg(int questionCount)
    {
        var pkg = new AssessmentPackage { Questions = new List<PackageQuestion>() };
        for (int i = 0; i < questionCount; i++)
            pkg.Questions.Add(new PackageQuestion());
        return pkg;
    }

    [Fact]
    public void Compute_NoPackages_ReturnsZeroNullFalse()
    {
        var r = PackageSizeAnalysis.Compute(new List<AssessmentPackage>());
        Assert.Equal(0, r.PackagesWithQuestions);
        Assert.Null(r.ReferenceCount);
        Assert.False(r.HasMismatch);
    }

    [Fact]
    public void Compute_SinglePackage_NoMismatch()
    {
        var r = PackageSizeAnalysis.Compute(new[] { Pkg(3) });
        Assert.Equal(1, r.PackagesWithQuestions);
        Assert.Equal(3, r.ReferenceCount);
        Assert.False(r.HasMismatch);
    }

    [Fact]
    public void Compute_TwoEqualPackages_NoMismatch()
    {
        var r = PackageSizeAnalysis.Compute(new[] { Pkg(3), Pkg(3) });
        Assert.Equal(2, r.PackagesWithQuestions);
        Assert.Equal(3, r.ReferenceCount);
        Assert.False(r.HasMismatch);
    }

    [Fact]
    public void Compute_TwoDifferentPackages_HasMismatch()
    {
        var r = PackageSizeAnalysis.Compute(new[] { Pkg(3), Pkg(5) });
        Assert.Equal(2, r.PackagesWithQuestions);
        Assert.Equal(3, r.ReferenceCount);   // referensi = paket-ber-soal pertama
        Assert.True(r.HasMismatch);
    }

    [Fact]
    public void Compute_EmptyPackageIgnored()
    {
        // paket kosong (0 soal) diabaikan -> hanya 1 paket-ber-soal terhitung, no mismatch.
        var r = PackageSizeAnalysis.Compute(new[] { Pkg(0), Pkg(3) });
        Assert.Equal(1, r.PackagesWithQuestions);
        Assert.Equal(3, r.ReferenceCount);
        Assert.False(r.HasMismatch);
    }
}
