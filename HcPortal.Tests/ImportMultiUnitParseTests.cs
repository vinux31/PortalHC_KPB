// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-04 import parse pipe-delimited. Diisi/diaktifkan plan 02 T3.
//
// Behavior yang dikontrak (plan 02):
//   - "UnitA|UnitB|UnitC" → split('|') + trim + dedup (OrdinalIgnoreCase); first = primary.
//   - Backward-compat "UnitA" (no pipe) → 1 elemen, primary = UnitA (template lama tetap valid).
//   - Empty/whitespace → 0 elemen.
// Strategy: pure-logic (no DB). Parser di WorkerController.Import (helper diuji langsung).
using Xunit;

namespace HcPortal.Tests;

public class ImportMultiUnitParseTests
{
    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T3 (parse pipe split+trim+dedup)")]
    public void Parse_PipeDelimited_SplitsTrimsDedups_FirstIsPrimary()
    {
        // plan 02: " UnitA | UnitB |UnitA " → ["UnitA","UnitB"], primary="UnitA".
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T3 (backward-compat 1-unit)")]
    public void Parse_SingleUnit_NoPipe_BackwardCompatible()
    {
        // plan 02: "UnitA" → ["UnitA"], primary="UnitA".
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T3 (empty → 0 elemen)")]
    public void Parse_EmptyOrWhitespace_ReturnsEmpty()
    {
        // plan 02: "" / "  " / "|" → [] (RemoveEmptyEntries).
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }
}
