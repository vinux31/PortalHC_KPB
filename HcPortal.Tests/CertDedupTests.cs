// CertDedupTests — Phase 368 #25 (CertificationManagement GroupBy dedup helper).
// Bug: `ToDictionary(c => c.Name)` di CMP/CDP throw ArgumentException (500) bila 2 sub-kategori ber-Name
// sama lintas parent. Helper static `SertifikatRow.BuildParentNameLookup` GroupBy-dedup → tidak throw.
// Unit murni (no DB), pola PackageImageDeleteTests.
using System.Collections.Generic;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class CertDedupTests
{
    [Fact]
    public void ParentNameLookup_DuplicateChildName_DoesNotThrow()
    {
        // 2 child ber-Name "Sub-X" lintas parent berbeda (A id=1, B id=2).
        var cats = new List<(int Id, string Name, int? ParentId)>
        {
            (1, "A", null),
            (2, "B", null),
            (3, "Sub-X", 1),
            (4, "Sub-X", 2),   // duplicate child Name → ToDictionary lama akan throw di sini
        };

        var result = SertifikatRow.BuildParentNameLookup(cats);

        Assert.True(result.ContainsKey("Sub-X"));
        Assert.Contains(result["Sub-X"], new[] { "A", "B" });   // GroupBy First — dedup, nilai salah satu parent OK (tidak 500)
    }

    [Fact]
    public void ParentNameLookup_NormalCase_MapsChildToParent()
    {
        var cats = new List<(int Id, string Name, int? ParentId)>
        {
            (1, "A", null),
            (5, "Sub-Y", 1),
        };

        var result = SertifikatRow.BuildParentNameLookup(cats);

        Assert.Equal("A", result["Sub-Y"]);
    }

    [Fact]
    public void ParentNameLookup_RootOnly_Empty()
    {
        var cats = new List<(int Id, string Name, int? ParentId)>
        {
            (1, "A", null),
            (2, "B", null),
        };

        var result = SertifikatRow.BuildParentNameLookup(cats);

        Assert.Empty(result);
    }
}
