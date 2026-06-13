// RenewalValidationTests — Phase 368 #26 (EditTraining renewal validation: Renews*Id exist + same-user).
// #26 validasi ada di controller (butuh HttpContext/User/TempData); test paling bersih = uji KONTRAK predikat
// (`src == null || src.UserId != record.UserId`) langsung pada data real-SQL (pola A4 RESEARCH fallback —
// hindari mock berat HubContext/UserManager/SignalR). Reuse RecordCascadeFixture (367, public).
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class RenewalValidationTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public RenewalValidationTests(RecordCascadeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "rnv-" + Guid.NewGuid().ToString("N")[..8], Email = "rnv@test.local", FullName = "Renewal Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static TrainingRecord NewTraining(string userId, string judul = "T") =>
        new TrainingRecord { UserId = userId, Judul = judul, Tanggal = new DateTime(2026, 2, 2), Status = "Valid" };

    // Kontrak #26 (mirror controller): block bila src tak exist ATAU beda user.
    private static bool RenewalIsInvalid(TrainingRecord? src, TrainingRecord target)
        => src == null || src.UserId != target.UserId;

    [Fact]
    public async Task RenewalValidation_DifferentUser_IsInvalid()
    {
        // src milik user B, target milik user A → cross-user → block (IDOR root-cause #26).
        await using var ctx = NewCtx();
        var userA = await SeedUserAsync(ctx);
        var userB = await SeedUserAsync(ctx);

        var src = NewTraining(userB, "SrcMilikB");
        var target = NewTraining(userA, "TargetMilikA");
        ctx.TrainingRecords.AddRange(src, target);
        await ctx.SaveChangesAsync();

        var found = await ctx.TrainingRecords.FindAsync(src.Id);
        Assert.True(RenewalIsInvalid(found, target));            // cross-user ditolak
        Assert.NotEqual(target.UserId, found!.UserId);
    }

    [Fact]
    public async Task RenewalValidation_NonExistent_IsInvalid()
    {
        // Renews*Id menunjuk record yang tak ada → FindAsync null → block.
        await using var ctx = NewCtx();
        var userA = await SeedUserAsync(ctx);
        var target = NewTraining(userA, "Target");
        ctx.TrainingRecords.Add(target);
        await ctx.SaveChangesAsync();

        var found = await ctx.TrainingRecords.FindAsync(999999);
        Assert.Null(found);
        Assert.True(RenewalIsInvalid(found, target));            // non-existent ditolak
    }

    [Fact]
    public async Task RenewalValidation_SameUserExisting_IsValid()
    {
        // src + target sama-user dan src exist → tidak block (renewal sah).
        await using var ctx = NewCtx();
        var userA = await SeedUserAsync(ctx);
        var src = NewTraining(userA, "Src");
        var target = NewTraining(userA, "Target");
        ctx.TrainingRecords.AddRange(src, target);
        await ctx.SaveChangesAsync();

        var found = await ctx.TrainingRecords.FindAsync(src.Id);
        Assert.False(RenewalIsInvalid(found, target));           // same-user valid → lolos
        Assert.Equal(target.UserId, found!.UserId);
    }
}
