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
/// Phase 372 SHUF-03 — real-SQL: ubah toggle di EditAssessment propagate ke SEMUA sibling grup.
///
/// Pendekatan: data/persistence level. Test mereplikasi propagation-foreach controller VERBATIM
/// (F2 standard `sibling.ShuffleQuestions = model.ShuffleQuestions`; F1 Pre-Post `s.ShuffleQuestions = model.ShuffleQuestions`)
/// atas grup sibling REAL di SQL Server, lalu assert SELURUH baris grup ikut nilai model. Wiring 2 foreach
/// di controller di-cover terpisah (anchored-insertion + grep + build, 372-02 Task 1). Test ini membuktikan
/// INVARIANT: perubahan flag pada grup ter-persist ke setiap baris (tak ada sibling tertinggal nilai lama).
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class ShufflePropagationTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ShufflePropagationTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "shufprop-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "shufprop@test.local",
            FullName = "Shuffle Prop Test"
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

    // SHUF-03a — grup standard (>=2 sibling, key Title/Category/Schedule.Date sama) flag awal ON,
    // propagate model{false,false} via foreach (replika F2 sibling.*) → SEMUA sibling jadi false.
    [Fact]
    public async Task Propagation_Standard_AllSiblingsFollowModel()
    {
        var marker = "SHUF-03a-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sched = new DateTime(2026, 3, 1, 8, 0, 0);
        var model = new AssessmentSession { ShuffleQuestions = false, ShuffleOptions = false };

        var ids = new List<int>();
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            for (int i = 0; i < 3; i++)
            {
                var userId = await SeedUserAsync(ctx);
                var s = Sib(userId, marker, sched);
                ctx.AssessmentSessions.Add(s);
                await ctx.SaveChangesAsync();
                ids.Add(s.Id);
            }
        }

        // Propagate (replika F2 standard foreach): semua sibling grup ikut nilai model.
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var siblings = await ctx.AssessmentSessions.Where(s => s.Title == marker).ToListAsync();
            Assert.Equal(3, siblings.Count);
            foreach (var sibling in siblings)
            {
                sibling.ShuffleQuestions = model.ShuffleQuestions;
                sibling.ShuffleOptions = model.ShuffleOptions;
            }
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var rows = await readCtx.AssessmentSessions.AsNoTracking().Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.False(r.ShuffleQuestions));
        Assert.All(rows, r => Assert.False(r.ShuffleOptions));
    }

    // SHUF-03b — grup Pre-Post (allGroupSessions Pre + Post) flag awal ON, propagate model{false,true}
    // via foreach (replika F1 s.*) → seluruh grup ikut nilai model (independensi terjaga lintas baris).
    [Fact]
    public async Task Propagation_PrePost_AllGroupSessionsFollowModel()
    {
        var marker = "SHUF-03b-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var model = new AssessmentSession { ShuffleQuestions = false, ShuffleOptions = true };

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            // Pre + Post = grup terpisah Schedule beda, di-edit bersama (allGroupSessions).
            var u1 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(Sib(u1, marker, new DateTime(2026, 3, 1, 8, 0, 0)));   // Pre
            var u2 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(Sib(u2, marker, new DateTime(2026, 3, 8, 8, 0, 0)));   // Post
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var allGroupSessions = await ctx.AssessmentSessions.Where(s => s.Title == marker).ToListAsync();
            Assert.Equal(2, allGroupSessions.Count);
            foreach (var s in allGroupSessions)
            {
                s.ShuffleQuestions = model.ShuffleQuestions;
                s.ShuffleOptions = model.ShuffleOptions;
            }
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var rows = await readCtx.AssessmentSessions.AsNoTracking().Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.False(r.ShuffleQuestions));
        Assert.All(rows, r => Assert.True(r.ShuffleOptions));
    }
}
