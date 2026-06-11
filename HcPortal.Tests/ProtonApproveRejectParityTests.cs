using System;
using Microsoft.EntityFrameworkCore;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 363-01 — pin tests kontrak end-state approve/reject gold-standard (T1/T2/T7).
/// Memanggil static core CDPController.ApproveDeliverableCoreAsync / RejectDeliverableCoreAsync
/// LANGSUNG (tanpa konstruksi controller) via ProtonCompletionFixture (real SQL disposable).
/// Pin STATE mutation saja — notif dispatch butuh UserManager, dicover UAT Plan 07.
/// Kontrak ini WAJIB tetap hijau setelah Plan 02 me-rewire ApproveFromProgress/RejectFromProgress.
/// </summary>
[Trait("Category", "Integration")]
public class ProtonApproveRejectParityTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ProtonApproveRejectParityTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Seed chain track→kompetensi→sub→N deliverable + 1 progress per status.
    /// Return progress IDs urut deliverable. DB shared antar-fact → coacheeId unik per fact.
    /// </summary>
    private static async Task<List<int>> SeedProgressChainAsync(
        ApplicationDbContext ctx, string coacheeId, string[] statuses)
    {
        // ProtonTracks di-seed migration HasData — reuse, jangan insert (UNIQUE TrackType+TahunKe).
        var track = await ctx.ProtonTracks.FirstAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1");

        var asg = new ProtonTrackAssignment { CoacheeId = coacheeId, AssignedById = "hc", ProtonTrackId = track.Id, IsActive = true };
        ctx.ProtonTrackAssignments.Add(asg);
        var komp = new ProtonKompetensi { Bagian = "Bagian-PAR", Unit = $"U-{coacheeId[..10]}", NamaKompetensi = $"K-{coacheeId}", Urutan = 1, ProtonTrackId = track.Id };
        ctx.ProtonKompetensiList.Add(komp);
        await ctx.SaveChangesAsync();
        var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
        ctx.ProtonSubKompetensiList.Add(sub);
        await ctx.SaveChangesAsync();

        var ids = new List<int>();
        for (int i = 0; i < statuses.Length; i++)
        {
            var d = new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = $"D{i + 1}", Urutan = i + 1 };
            ctx.ProtonDeliverableList.Add(d);
            await ctx.SaveChangesAsync();
            var p = new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = d.Id,
                ProtonTrackAssignmentId = asg.Id,
                Status = statuses[i],
                CreatedAt = DateTime.UtcNow
            };
            ctx.ProtonDeliverableProgresses.Add(p);
            await ctx.SaveChangesAsync();
            ids.Add(p.Id);
        }
        return ids;
    }

    [Fact]
    public async Task RejectCore_ResetsFullChain_IncludingHC()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"par-{Guid.NewGuid():N}";
        var ids = await SeedProgressChainAsync(ctx, coachee, new[] { "Approved" });

        // Pre-state T2: chain penuh ter-set TERMASUK HC "Reviewed" — state yang dulu survive reject.
        var seeded = await ctx.ProtonDeliverableProgresses.FirstAsync(p => p.Id == ids[0]);
        seeded.SrSpvApprovalStatus = "Approved"; seeded.SrSpvApprovedById = "srspv-1"; seeded.SrSpvApprovedAt = DateTime.UtcNow;
        seeded.ShApprovalStatus = "Approved"; seeded.ShApprovedById = "sh-1"; seeded.ShApprovedAt = DateTime.UtcNow;
        seeded.HCApprovalStatus = "Reviewed"; seeded.HCReviewedById = "hc-1"; seeded.HCReviewedAt = DateTime.UtcNow;
        seeded.ApprovedById = "srspv-1"; seeded.ApprovedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var (ok, error) = await CDPController.RejectDeliverableCoreAsync(
            ctx, ids[0], "srspv-1", "Sr Spv Satu", UserRoles.SrSupervisor, "Evidence tidak sesuai");

        Assert.True(ok);
        Assert.Null(error);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var p = await verify.ProtonDeliverableProgresses.AsNoTracking().FirstAsync(x => x.Id == ids[0]);
        Assert.Equal("Rejected", p.Status);
        Assert.Equal("Pending", p.SrSpvApprovalStatus);
        Assert.Null(p.SrSpvApprovedById);
        Assert.Equal("Pending", p.ShApprovalStatus);
        Assert.Null(p.ShApprovedById);
        Assert.Equal("Pending", p.HCApprovalStatus);
        Assert.Null(p.HCReviewedById);
        Assert.Null(p.HCReviewedAt);
        Assert.Null(p.ApprovedById);
        Assert.Null(p.ApprovedAt);
        Assert.Equal("Evidence tidak sesuai", p.RejectionReason);
        Assert.NotNull(p.RejectedAt);
    }

    [Fact]
    public async Task ApproveCore_LastDeliverable_ReturnsAllApprovedTrue()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"par-{Guid.NewGuid():N}";
        var ids = await SeedProgressChainAsync(ctx, coachee, new[] { "Approved", "Approved", "Submitted" });

        var (ok, error, allApproved) = await CDPController.ApproveDeliverableCoreAsync(
            ctx, ids[2], "srspv-1", "Sr Spv Satu", UserRoles.SrSupervisor, isSrSpv: true, isSH: false);

        Assert.True(ok);
        Assert.Null(error);
        Assert.True(allApproved);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var p = await verify.ProtonDeliverableProgresses.AsNoTracking().FirstAsync(x => x.Id == ids[2]);
        Assert.Equal("Approved", p.Status);
        Assert.Equal("Approved", p.SrSpvApprovalStatus);
        Assert.Equal("srspv-1", p.SrSpvApprovedById);
        Assert.Equal("srspv-1", p.ApprovedById);
    }

    [Fact]
    public async Task ApproveCore_NotLast_ReturnsAllApprovedFalse()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"par-{Guid.NewGuid():N}";
        var ids = await SeedProgressChainAsync(ctx, coachee, new[] { "Submitted", "Submitted" });

        var (ok, error, allApproved) = await CDPController.ApproveDeliverableCoreAsync(
            ctx, ids[0], "sh-1", "Sect Head Satu", UserRoles.SectionHead, isSrSpv: false, isSH: true);

        Assert.True(ok);
        Assert.Null(error);
        Assert.False(allApproved);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var p = await verify.ProtonDeliverableProgresses.AsNoTracking().FirstAsync(x => x.Id == ids[0]);
        Assert.Equal("Approved", p.Status);
        Assert.Equal("Approved", p.ShApprovalStatus);
        Assert.Equal("sh-1", p.ShApprovedById);
    }

    [Fact]
    public async Task ApproveCore_RaceGuard_RejectsStaleSecondApprove()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"par-{Guid.NewGuid():N}";
        var ids = await SeedProgressChainAsync(ctx, coachee, new[] { "Approved" });

        // Approver lain sudah memproses: Status="Approved" + SrSpv sudah "Approved" di DB (T7/D-10).
        var seeded = await ctx.ProtonDeliverableProgresses.FirstAsync(p => p.Id == ids[0]);
        seeded.SrSpvApprovalStatus = "Approved"; seeded.SrSpvApprovedById = "srspv-lain"; seeded.SrSpvApprovedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var (ok, error, allApproved) = await CDPController.ApproveDeliverableCoreAsync(
            ctx, ids[0], "srspv-1", "Sr Spv Satu", UserRoles.SrSupervisor, isSrSpv: true, isSH: false);

        Assert.False(ok);
        Assert.False(allApproved);
        Assert.Contains("diproses oleh approver lain", error!);
    }
}
