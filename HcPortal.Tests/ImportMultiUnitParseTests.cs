// Phase 399 Plan 02 Task 3 — MU-04 import parse pipe-delimited (GREEN).
// Menguji WorkerController.ParseUnitCell (pure-logic, no DB).
//   - "UnitA|UnitB|UnitC" → split('|') + trim + dedup (OrdinalIgnoreCase); first = primary.
//   - Backward-compat "UnitA" (no pipe) → 1 elemen, primary = UnitA (template lama tetap valid).
//   - Empty/whitespace/"|" → 0 elemen.
using System.Collections.Generic;
using System.Linq;
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class ImportMultiUnitParseTests
{
    [Fact]
    public void Parse_PipeDelimited_SplitsTrimsDedups_FirstIsPrimary()
    {
        // " UnitA | UnitB |UnitA " → ["UnitA","UnitB"] (dedup case-insensitif), primary="UnitA"
        var units = WorkerController.ParseUnitCell(" UnitA | UnitB |UnitA ");

        Assert.Equal(new List<string> { "UnitA", "UnitB" }, units);
        Assert.Equal("UnitA", units.FirstOrDefault());   // first = primary
    }

    [Fact]
    public void Parse_SingleUnit_NoPipe_BackwardCompatible()
    {
        // "UnitA" (template lama, tanpa pipe) → ["UnitA"], primary="UnitA"
        var units = WorkerController.ParseUnitCell("UnitA");

        Assert.Single(units);
        Assert.Equal("UnitA", units[0]);
        Assert.Equal("UnitA", units.FirstOrDefault());
    }

    [Fact]
    public void Parse_EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Empty(WorkerController.ParseUnitCell(""));
        Assert.Empty(WorkerController.ParseUnitCell("   "));
        Assert.Empty(WorkerController.ParseUnitCell("|"));
        Assert.Empty(WorkerController.ParseUnitCell(null));
    }
}
