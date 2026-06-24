// Phase 424 GRDF-01/GRDF-03 — real-SQL gate-decision tests untuk PrePostPairing.FindPairedPreAsync
// (dikonsumsi gate StartExam Plan 02 Task 1). Reuse GradingDedupeFixture (disposable HcPortalDB_Test_{guid}).
// Mengunci keputusan block/pass + filter UserId; redirect/TempData penuh di-UAT browser Plan 03.
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class PrePostGatingTests : IClassFixture<GradingDedupeFixture>
{
    private readonly GradingDedupeFixture _fixture;
    public PrePostGatingTests(GradingDedupeFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string tag)
    {
        var u = new ApplicationUser { UserName = $"gate-{tag}-{Guid.NewGuid():N}".Substring(0, 20), Email = "g@test.local", FullName = "Gate Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession Sess(string userId, string type, string status, int? linkedSessionId = null, int? linkedGroupId = null) =>
        new AssessmentSession
        {
            UserId = userId, Title = "Gate", Category = "IHT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), AssessmentType = type,
            LinkedSessionId = linkedSessionId, LinkedGroupId = linkedGroupId
        };

    // Post→Pre(InProgress) via LinkedSessionId → kembalikan Pre, status != Completed → gate BLOCK.
    [Fact]
    public async Task PostLinkedToInProgressPre_ReturnsPre_GateBlocks()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx, "a");
        var pre = Sess(uid, "PreTest", "InProgress");
        ctx.AssessmentSessions.Add(pre); await ctx.SaveChangesAsync();
        var post = Sess(uid, "PostTest", "Open", linkedSessionId: pre.Id);
        ctx.AssessmentSessions.Add(post); await ctx.SaveChangesAsync();

        var paired = await PrePostPairing.FindPairedPreAsync(ctx, post);
        Assert.NotNull(paired);
        Assert.Equal(pre.Id, paired!.Id);
        Assert.NotEqual("Completed", paired.Status);   // gate akan memblok
    }

    // Post→Pre(Completed) → kembalikan Pre Completed → gate PASS.
    [Fact]
    public async Task PostLinkedToCompletedPre_GatePasses()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx, "b");
        var pre = Sess(uid, "PreTest", "Completed");
        ctx.AssessmentSessions.Add(pre); await ctx.SaveChangesAsync();
        var post = Sess(uid, "PostTest", "Open", linkedSessionId: pre.Id);
        ctx.AssessmentSessions.Add(post); await ctx.SaveChangesAsync();

        var paired = await PrePostPairing.FindPairedPreAsync(ctx, post);
        Assert.NotNull(paired);
        Assert.Equal("Completed", paired!.Status);     // gate lewat (tak block)
    }

    // Post tanpa link (orphan) → null → pass-through (D-02).
    [Fact]
    public async Task Orphan_NoLink_ReturnsNull()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx, "c");
        var post = Sess(uid, "PostTest", "Open");
        ctx.AssessmentSessions.Add(post); await ctx.SaveChangesAsync();

        Assert.Null(await PrePostPairing.FindPairedPreAsync(ctx, post));
    }

    // Standard type → null → pass-through.
    [Fact]
    public async Task StandardType_ReturnsNull()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx, "d");
        var std = Sess(uid, "Standard", "Open", linkedSessionId: 999);
        ctx.AssessmentSessions.Add(std); await ctx.SaveChangesAsync();

        Assert.Null(await PrePostPairing.FindPairedPreAsync(ctx, std));
    }

    // Pre milik USER LAIN → null (filter UserId) → pass-through. Cegah cross-user gate (FLOW-01).
    [Fact]
    public async Task PreOwnedByOtherUser_ReturnsNull_FilteredByUserId()
    {
        await using var ctx = NewCtx();
        var userA = await SeedUserAsync(ctx, "e1");
        var userB = await SeedUserAsync(ctx, "e2");
        var preB = Sess(userB, "PreTest", "InProgress");
        ctx.AssessmentSessions.Add(preB); await ctx.SaveChangesAsync();
        var postA = Sess(userA, "PostTest", "Open", linkedSessionId: preB.Id);   // link ke Pre user lain
        ctx.AssessmentSessions.Add(postA); await ctx.SaveChangesAsync();

        Assert.Null(await PrePostPairing.FindPairedPreAsync(ctx, postA));         // filter UserId → null
    }

    // LinkedSessionId diprioritaskan di atas LinkedGroupId.
    [Fact]
    public async Task LinkedSessionId_PrioritizedOverLinkedGroupId()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx, "f");
        var preExplicit = Sess(uid, "PreTest", "InProgress");
        ctx.AssessmentSessions.Add(preExplicit); await ctx.SaveChangesAsync();
        var preGroup = Sess(uid, "PreTest", "Completed", linkedGroupId: 7777);
        ctx.AssessmentSessions.Add(preGroup); await ctx.SaveChangesAsync();
        var post = Sess(uid, "PostTest", "Open", linkedSessionId: preExplicit.Id, linkedGroupId: 7777);
        ctx.AssessmentSessions.Add(post); await ctx.SaveChangesAsync();

        var paired = await PrePostPairing.FindPairedPreAsync(ctx, post);
        Assert.NotNull(paired);
        Assert.Equal(preExplicit.Id, paired!.Id);     // LinkedSessionId menang (bukan preGroup)
    }
}
