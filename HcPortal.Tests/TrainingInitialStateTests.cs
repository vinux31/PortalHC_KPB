// Unit test AssessmentAdminController.IsTrainingInitialState (Phase 348 P03 — MAM-06).
// initial-state = TIDAK ada filter aktif → skip full-roster query (empty-state "Pilih filter").
// isFiltered hidden field di-post saat user interaksi filter (_TrainingRecordsTab.cshtml:32).

using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class TrainingInitialStateTests
{
    [Fact]
    public void AllFiltersEmpty_ReturnsTrue()
    {
        var result = AssessmentAdminController.IsTrainingInitialState(null, null, null, null, null, null);
        Assert.True(result);
    }

    [Theory]
    [InlineData("true", null, null, null, null, null)]      // isFiltered set (user interaksi)
    [InlineData(null, "Refinery", null, null, null, null)]  // section terisi tanpa isFiltered
    [InlineData(null, null, "UnitA", null, null, null)]     // unit
    [InlineData(null, null, null, "OJT", null, null)]       // category
    [InlineData(null, null, null, null, "Sudah", null)]     // statusFilter
    [InlineData(null, null, null, null, null, "budi")]      // search
    public void AnyFilterPresent_ReturnsFalse(
        string? isFiltered, string? section, string? unit, string? category, string? statusFilter, string? search)
    {
        var result = AssessmentAdminController.IsTrainingInitialState(
            isFiltered, section, unit, category, statusFilter, search);
        Assert.False(result);
    }
}
