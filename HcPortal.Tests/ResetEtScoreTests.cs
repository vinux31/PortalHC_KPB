// ResetEtScoreTests — Phase 368 #22 (ResetAssessment bersihkan SessionElemenTeknisScores).
// Akar bug: unique index (AssessmentSessionId, ElemenTeknis) → GradingService gagal Add ET baru saat retake
// (sesi sama) → catch(DbUpdateException) swallow → ET stale. Reset harus hapus ET dulu agar retake regen fresh.
// Uji logika cleanup+regen langsung pada DbContext real-SQL (hindari mock HubContext/UserManager/SignalR).
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class ResetEtScoreTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public ResetEtScoreTests(RecordCascadeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "et-" + Guid.NewGuid().ToString("N")[..8], Email = "et@test.local", FullName = "ET Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(ApplicationDbContext ctx, string userId)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = "EtSesi", Category = "Test", Status = "Completed",
            AccessToken = "", Schedule = new DateTime(2026, 2, 1)
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    [Fact]
    public async Task ResetEtCleanup_RemovesEtScores()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId);

        ctx.SessionElemenTeknisScores.AddRange(
            new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "E1", CorrectCount = 1, QuestionCount = 2 },
            new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "E2", CorrectCount = 0, QuestionCount = 2 }
        );
        await ctx.SaveChangesAsync();

        // Logika cleanup #22 (mirror controller, sebelum SaveChanges):
        var et = await ctx.SessionElemenTeknisScores.Where(e => e.AssessmentSessionId == sessionId).ToListAsync();
        ctx.SessionElemenTeknisScores.RemoveRange(et);
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.SessionElemenTeknisScores.CountAsync(e => e.AssessmentSessionId == sessionId));
    }

    [Fact]
    public async Task ResetEtCleanup_RetakeReinsertNoUniqueViolation()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId);

        ctx.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "E1", CorrectCount = 1, QuestionCount = 2 });
        await ctx.SaveChangesAsync();

        // Cleanup (#22) → slot ElemenTeknis kosong.
        var et = await ctx.SessionElemenTeknisScores.Where(e => e.AssessmentSessionId == sessionId).ToListAsync();
        ctx.SessionElemenTeknisScores.RemoveRange(et);
        await ctx.SaveChangesAsync();

        // Retake regen ET ElemenTeknis sama → TIDAK throw unique-violation → fresh.
        ctx.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "E1", CorrectCount = 2, QuestionCount = 2 });
        await ctx.SaveChangesAsync();

        var fresh = await ctx.SessionElemenTeknisScores.SingleAsync(e => e.AssessmentSessionId == sessionId && e.ElemenTeknis == "E1");
        Assert.Equal(2, fresh.CorrectCount);                                 // fresh (bukan stale 1)
    }

    [Fact]
    public async Task ResetEtCleanup_WithoutCleanup_DuplicateThrows()
    {
        // Bukti akar #22: tanpa cleanup, Add ElemenTeknis sama (sesi sama) → unique-index violation.
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId);

        ctx.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "E1", CorrectCount = 1, QuestionCount = 2 });
        await ctx.SaveChangesAsync();

        ctx.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "E1", CorrectCount = 2, QuestionCount = 2 });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }
}
