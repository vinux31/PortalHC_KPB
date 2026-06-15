using System;
using Microsoft.EntityFrameworkCore;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 363-06 — T5 ("Belum Mulai" set computation) + T8 (AppendEvidencePathHistory).
/// Append tests murni (tanpa DB); set computation pakai ProtonCompletionFixture (real SQL,
/// predikat-replikasi BuildBelumMulaiRowsAsync level&lt;=3 — jaga sinkron dgn controller).
/// </summary>
[Trait("Category", "Integration")]
public class ProtonHistoriAndEvidenceTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ProtonHistoriAndEvidenceTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AppendEvidencePathHistory_AppendsOldPath()
    {
        var progress = new ProtonDeliverableProgress
        {
            EvidencePath = "/uploads/evidence/1/a.pdf",
            EvidencePathHistory = null
        };

        CDPController.AppendEvidencePathHistory(progress);
        var history = System.Text.Json.JsonSerializer.Deserialize<List<string>>(progress.EvidencePathHistory!)!;
        Assert.Single(history);
        Assert.Contains("/uploads/evidence/1/a.pdf", history);

        // Overwrite kedua → history kumulatif (a.pdf lalu b.pdf).
        progress.EvidencePath = "/uploads/evidence/1/b.pdf";
        CDPController.AppendEvidencePathHistory(progress);
        history = System.Text.Json.JsonSerializer.Deserialize<List<string>>(progress.EvidencePathHistory!)!;
        Assert.Equal(2, history.Count);
        Assert.Equal(new[] { "/uploads/evidence/1/a.pdf", "/uploads/evidence/1/b.pdf" }, history);
    }

    [Fact]
    public void AppendEvidencePathHistory_NoOp_WhenEmptyPath()
    {
        var progress = new ProtonDeliverableProgress { EvidencePath = null, EvidencePathHistory = null };
        CDPController.AppendEvidencePathHistory(progress);
        Assert.Null(progress.EvidencePathHistory);

        progress.EvidencePath = "";
        CDPController.AppendEvidencePathHistory(progress);
        Assert.Null(progress.EvidencePathHistory);
    }

    [Fact]
    public async Task BelumMulai_SetComputation()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coacheeNoAsg = $"bm-{Guid.NewGuid():N}";
        var coacheeWithAsg = $"bm-{Guid.NewGuid():N}";
        var track = await ctx.ProtonTracks.FirstAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1");

        // Coachee 1: mapping aktif TANPA assignment → harus masuk set "Belum Mulai".
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping
        { CoacheeId = coacheeNoAsg, CoachId = "coach-bm", AssignmentSection = "S-BM", AssignmentUnit = "U-BM", IsActive = true, StartDate = DateTime.UtcNow });
        // Coachee 2: mapping aktif + assignment → TIDAK masuk set.
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping
        { CoacheeId = coacheeWithAsg, CoachId = "coach-bm", AssignmentSection = "S-BM", AssignmentUnit = "U-BM", IsActive = true, StartDate = DateTime.UtcNow });
        ctx.ProtonTrackAssignments.Add(new ProtonTrackAssignment
        { CoacheeId = coacheeWithAsg, AssignedById = "hc", ProtonTrackId = track.Id, IsActive = true });
        await ctx.SaveChangesAsync();

        // Predikat-replikasi BuildBelumMulaiRowsAsync (cabang Level <=3) — jaga identik dgn controller.
        var activeMappingCoacheeIds = await ctx.CoachCoacheeMappings
            .Where(m => m.IsActive)
            .Select(m => m.CoacheeId).Distinct().ToListAsync();
        var coacheeIdsWithAssignments = await ctx.ProtonTrackAssignments
            .Select(a => a.CoacheeId).Distinct().ToListAsync();
        var belumMulaiIds = activeMappingCoacheeIds.Except(coacheeIdsWithAssignments).ToList();

        Assert.Contains(coacheeNoAsg, belumMulaiIds);
        Assert.DoesNotContain(coacheeWithAsg, belumMulaiIds);
    }
}
