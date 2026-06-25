// Phase 424 GRDF-03 — PrePostPairing.FindPairedPreAsync pure-portion (pass-through cases yang RETURN
// sebelum query DB, jadi ApplicationDbContext tak pernah ter-dereference → tanpa SQL, masuk suite pure).
// Cabang Linked* (real-SQL, filter UserId) diuji di Plan 02 PrePostGatingTests.
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class PrePostPairingTests
{
    // Context tak pernah di-query untuk dua kasus ini (early return) → tak butuh koneksi.
    private static ApplicationDbContext NoQueryCtx() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=__pairing_unused__;Integrated Security=True;TrustServerCertificate=True")
            .Options);

    private static AssessmentSession Sess(string? type, int? linkedSessionId = null, int? linkedGroupId = null) =>
        new AssessmentSession
        {
            UserId = "u1", Title = "X", Category = "C", Schedule = new DateTime(2026, 1, 1),
            Status = "Open", AccessToken = "", AssessmentType = type,
            LinkedSessionId = linkedSessionId, LinkedGroupId = linkedGroupId
        };

    [Fact]
    public async Task Standard_ReturnsNull_PassThrough()
    {
        await using var ctx = NoQueryCtx();
        Assert.Null(await PrePostPairing.FindPairedPreAsync(ctx, Sess("Standard")));
    }

    [Fact]
    public async Task PostTest_NoLink_ReturnsNull_Orphan()
    {
        await using var ctx = NoQueryCtx();
        Assert.Null(await PrePostPairing.FindPairedPreAsync(ctx, Sess("PostTest")));
    }
}
