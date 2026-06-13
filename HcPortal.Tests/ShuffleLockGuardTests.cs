using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 374 SHUF-11 (Wave 0) — real-SQL: guard server-side endpoint UpdateShuffleSettings.
/// Lock-condition = ada sibling StartedAt != null ATAU ada UserPackageAssignment di grup.
/// Keputusan lock dipanggil via ShuffleToggleRules.IsShuffleLocked (single-source, sama dgn GET).
/// Saat locked → JANGAN tulis (defense-in-depth D-04a). Saat clean → tulis ke semua sibling.
/// Mengikat helper Plan 01 ke kontrak endpoint sebelum endpoint ditulis (Plan 02).
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class ShuffleLockGuardTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ShuffleLockGuardTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "shuflock-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "shuflock@test.local",
            FullName = "Shuffle Lock Test"
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

    // SHUF-11 — sibling sudah StartedAt != null → isLocked true → guard reject, tidak menulis.
    [Fact]
    public async Task Guard_RejectsWrite_WhenSiblingStarted()
    {
        var marker = "SHUF-11s-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sched = new DateTime(2026, 4, 1, 8, 0, 0);

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u1 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(Sib(u1, marker, sched));
            var u2 = await SeedUserAsync(ctx);
            var started = Sib(u2, marker, sched);
            started.StartedAt = DateTime.UtcNow;   // peserta sudah mulai
            ctx.AssessmentSessions.Add(started);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var siblings = await ctx.AssessmentSessions.Where(s => s.Title == marker).ToListAsync();
            var siblingIds = siblings.Select(s => s.Id).ToList();
            var anyStarted = siblings.Any(s => s.StartedAt != null);
            var anyAssignment = await ctx.UserPackageAssignments
                .AnyAsync(a => siblingIds.Contains(a.AssessmentSessionId));
            var isLocked = ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment);

            Assert.True(isLocked);
            // locked → JANGAN tulis (guard reject). Tidak ada SaveChanges.
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var rows = await readCtx.AssessmentSessions.AsNoTracking().Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.ShuffleQuestions));   // MASIH ON — tidak berubah
    }

    // SHUF-11 — grup bersih (no started, no assignment) → isLocked false → tulis ke semua sibling.
    [Fact]
    public async Task Guard_AllowsWrite_WhenClean()
    {
        var marker = "SHUF-11c-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sched = new DateTime(2026, 4, 8, 8, 0, 0);

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u1 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(Sib(u1, marker, sched));
            var u2 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(Sib(u2, marker, sched));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var siblings = await ctx.AssessmentSessions.Where(s => s.Title == marker).ToListAsync();
            var siblingIds = siblings.Select(s => s.Id).ToList();
            var anyStarted = siblings.Any(s => s.StartedAt != null);
            var anyAssignment = await ctx.UserPackageAssignments
                .AnyAsync(a => siblingIds.Contains(a.AssessmentSessionId));
            var isLocked = ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment);

            Assert.False(isLocked);
            // clean → tulis ke semua sibling.
            foreach (var s in siblings) s.ShuffleQuestions = false;
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var rows = await readCtx.AssessmentSessions.AsNoTracking().Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.False(r.ShuffleQuestions));   // tertulis OFF
    }

    // SHUF-11 — ada UserPackageAssignment di grup → isLocked true → guard reject, tidak menulis.
    [Fact]
    public async Task Guard_RejectsWrite_WhenAssignmentExists()
    {
        var marker = "SHUF-11a-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var sched = new DateTime(2026, 4, 15, 8, 0, 0);

        int targetSessionId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u1 = await SeedUserAsync(ctx);
            var s1 = Sib(u1, marker, sched);
            ctx.AssessmentSessions.Add(s1);
            var u2 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(Sib(u2, marker, sched));
            await ctx.SaveChangesAsync();
            targetSessionId = s1.Id;

            // Seed AssessmentPackage (FK Restrict) lalu UserPackageAssignment yang memicu lock.
            var pkg = new AssessmentPackage
            {
                AssessmentSessionId = targetSessionId,
                PackageName = "Lock Pkg",
                PackageNumber = 1
            };
            ctx.AssessmentPackages.Add(pkg);
            await ctx.SaveChangesAsync();

            ctx.UserPackageAssignments.Add(new UserPackageAssignment
            {
                AssessmentSessionId = targetSessionId,
                AssessmentPackageId = pkg.Id,
                UserId = u1
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var siblings = await ctx.AssessmentSessions.Where(s => s.Title == marker).ToListAsync();
            var siblingIds = siblings.Select(s => s.Id).ToList();
            var anyStarted = siblings.Any(s => s.StartedAt != null);
            var anyAssignment = await ctx.UserPackageAssignments
                .AnyAsync(a => siblingIds.Contains(a.AssessmentSessionId));
            var isLocked = ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment);

            Assert.True(isLocked);
            // locked → JANGAN tulis.
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var rows = await readCtx.AssessmentSessions.AsNoTracking().Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.ShuffleQuestions));   // MASIH ON
    }
}
