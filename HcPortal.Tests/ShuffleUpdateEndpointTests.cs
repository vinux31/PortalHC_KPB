using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 374 SHUF-10 (Wave 0) — real-SQL: kontrak endpoint UpdateShuffleSettings propagate
/// flag ke SEMUA sibling grup. Test mereplikasi body endpoint atas grup REAL di SQL Server
/// memakai key sibling LENGKAP (Title + Category + Schedule.Date — spec §5), BUKAN hanya Title.
/// Mengunci kontrak sebelum endpoint ditulis (Plan 02). Wiring controller di-cover terpisah.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class ShuffleUpdateEndpointTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ShuffleUpdateEndpointTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "shufupd-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "shufupd@test.local",
            FullName = "Shuffle Update Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static AssessmentSession Sib(string userId, string title, DateTime schedule) => new AssessmentSession
    {
        UserId = userId,
        Title = title,
        Category = "Test",
        Status = "Open",
        AccessToken = "",
        Schedule = schedule,
        ShuffleQuestions = true,   // nilai AWAL grup = ON
        ShuffleOptions = true
    };

    // SHUF-10 — replika body endpoint UpdateShuffleSettings: query siblings by key LENGKAP
    // (Title + Category + Schedule.Date), foreach set flag dari POST + UpdatedAt → SEMUA sibling ikut.
    [Fact]
    public async Task UpdateShuffleSettings_PropagatesToAllSiblings()
    {
        var marker = "SHUF-10-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sched = new DateTime(2026, 3, 1, 8, 0, 0);

        // POST payload: matikan kedua toggle.
        const bool postShuffleQuestions = false;
        const bool postShuffleOptions = false;

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            for (int i = 0; i < 3; i++)
            {
                var userId = await SeedUserAsync(ctx);
                ctx.AssessmentSessions.Add(Sib(userId, marker, sched));
                await ctx.SaveChangesAsync();
            }
        }

        // Replika body endpoint: sibling key (Title + Category + Schedule.Date) — spec §5.
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var now = DateTime.UtcNow;
            var siblings = await ctx.AssessmentSessions
                .Where(s => s.Title == marker && s.Category == "Test" && s.Schedule.Date == sched.Date)
                .ToListAsync();
            Assert.Equal(3, siblings.Count);
            foreach (var sibling in siblings)
            {
                sibling.ShuffleQuestions = postShuffleQuestions;
                sibling.ShuffleOptions = postShuffleOptions;
                sibling.UpdatedAt = now;
            }
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var rows = await readCtx.AssessmentSessions.AsNoTracking()
            .Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.False(r.ShuffleQuestions));
        Assert.All(rows, r => Assert.False(r.ShuffleOptions));
    }
}
