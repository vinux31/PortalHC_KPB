using System.Collections.Generic;
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class BuildActualCategoriesTests
{
    [Fact]
    public void DistinctNonEmpty_CaseInsensitive_Ordered()
    {
        var records = new List<UnifiedTrainingRecord>
        {
            new() { Kategori = "OJT" },
            new() { Kategori = "ojt" },
            new() { Kategori = "Legacy Free Text" },
            new() { Kategori = null },
            new() { Kategori = "" },
        };
        var result = CMPController.BuildActualCategories(records);
        Assert.Equal(new[] { "Legacy Free Text", "OJT" }, result);
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(CMPController.BuildActualCategories(new List<UnifiedTrainingRecord>()));
    }

    [Fact]
    public void AllNullOrEmpty_ReturnsEmptyList()
    {
        var records = new List<UnifiedTrainingRecord> { new() { Kategori = null }, new() { Kategori = "" } };
        Assert.Empty(CMPController.BuildActualCategories(records));
    }
}
