// OrphanCleanupTests — Phase 368 #23 (cleanup orphan AttemptHistory legacy).
// Uji logika cleanup langsung pada DbContext real-SQL (bukan invoke controller penuh — hindari mock
// UserManager/HttpContext berat, pola A4 RESEARCH). Orphan = AttemptHistory ber-SessionId dangling
// (tak ada AssessmentSession induk). AssessmentAttemptHistory.SessionId plain int, NO FK → insert orphan bebas.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class OrphanCleanupTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public OrphanCleanupTests(RecordCascadeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "orph-" + Guid.NewGuid().ToString("N")[..8], Email = "orph@test.local", FullName = "Orphan Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    // Predikat orphan (mirror controller): AttemptHistory yang SessionId-nya tak match AssessmentSession mana pun.
    private static async Task<int> OrphanCountAsync(ApplicationDbContext ctx)
        => await ctx.AssessmentAttemptHistory
            .Where(h => !ctx.AssessmentSessions.Any(s => s.Id == h.SessionId))
            .CountAsync();

    [Fact]
    public async Task OrphanCleanup_PreviewExecuteIdempotent()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);

        // 1 sesi nyata → AttemptHistory non-orphan (SessionId valid).
        var session = new AssessmentSession
        {
            UserId = userId, Title = "RealSesi", Category = "Test", Status = "Completed",
            AccessToken = "", Schedule = new DateTime(2026, 2, 1)
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        // 2 orphan (SessionId dangling = 999990/999991) + 1 non-orphan (SessionId = session.Id valid).
        ctx.AssessmentAttemptHistory.AddRange(
            new AssessmentAttemptHistory { SessionId = 999990, UserId = userId, Title = "Orphan1", Category = "X", AttemptNumber = 1 },
            new AssessmentAttemptHistory { SessionId = 999991, UserId = userId, Title = "Orphan2", Category = "X", AttemptNumber = 1 },
            new AssessmentAttemptHistory { SessionId = session.Id, UserId = userId, Title = "Valid", Category = "X", AttemptNumber = 1 }
        );
        await ctx.SaveChangesAsync();

        // 2. Preview-count: hanya 2 orphan terhitung (non-orphan tak masuk).
        Assert.Equal(2, await OrphanCountAsync(ctx));

        // 3. Execute: RemoveRange orphan + SaveChanges.
        var orphanRows = await ctx.AssessmentAttemptHistory
            .Where(h => !ctx.AssessmentSessions.Any(s => s.Id == h.SessionId))
            .ToListAsync();
        ctx.AssessmentAttemptHistory.RemoveRange(orphanRows);
        await ctx.SaveChangesAsync();

        // Non-orphan masih ada (SessionId = session.Id valid).
        Assert.True(await ctx.AssessmentAttemptHistory.AnyAsync(h => h.SessionId == session.Id));

        // 4. Idempotent re-run: orphan count = 0.
        Assert.Equal(0, await OrphanCountAsync(ctx));
    }
}
