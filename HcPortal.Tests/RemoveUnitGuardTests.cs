// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-07 guard hapus-unit (asimetris). Diisi/diaktifkan plan 02 T2.
//
// Behavior yang dikontrak (plan 02):
//   - Hapus unit dgn ProtonTrackAssignment aktif (resolved PROTON unit) → BLOCK (hard).
//   - Hapus unit dgn CoachCoacheeMapping aktif (AssignmentUnit, tanpa PTA) →
//     ConfirmedDeactivate=false → re-prompt; true → auto-deactivate mapping (1 tx).
// Strategy: InMemory DB (Guid per test). Reuse pola active-record query (CoacheeId && IsActive).
using System;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class RemoveUnitGuardTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T2 (PTA aktif → hard block)")]
    public void RemoveUnit_WithActiveProtonTrackAssignment_IsBlocked()
    {
        // plan 02: removed unit = resolved PROTON unit + ada PTA aktif → ModelState error, no mutate.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T2 (mapping aktif, no PTA → re-prompt)")]
    public void RemoveUnit_WithActiveMapping_NoConfirm_RePrompts()
    {
        // plan 02: AssignmentUnit ∈ removed, no PTA, ConfirmedDeactivate=false → NeedConfirm, no mutate.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T2 (confirm → auto-deactivate)")]
    public void RemoveUnit_WithActiveMapping_Confirmed_AutoDeactivates()
    {
        // plan 02: ConfirmedDeactivate=true → mapping.IsActive=false + EndDate set (1 tx).
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }
}
