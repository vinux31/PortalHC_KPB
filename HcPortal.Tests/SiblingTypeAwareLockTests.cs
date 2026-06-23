using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 SHFX-06/SHUF-ISS-01 (Wave 2) — sibling key TYPE-AWARE untuk LOCK-DETECTION saja.
/// Bug: key type-agnostic (Title/Category/Schedule.Date) menggabung Pre+Post → Pre mulai = over-lock Post.
/// Fix: lock-detection (anyStarted/anyAssignment) pakai SiblingPrePostAwarePredicate (Pre & Post tak saling kunci).
///
/// ⚠️ SCOPE GUARD (Pitfall 4 / RESEARCH A3): propagation write shuffle Pre↔Post di UpdateShuffleSettings
/// (:5661-5671) SENGAJA cross-type — TIDAK boleh berubah. Test propagation di bawah meng-assert sibling
/// cross-type (type-agnostic) MASIH mencakup BAIK Pre maupun Post (propagation tak regresi).
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class SiblingTypeAwareLockTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public SiblingTypeAwareLockTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly DateTime Sched = new DateTime(2026, 5, 18, 8, 0, 0);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "sibtype-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "sibtype@test.local",
            FullName = "Sibling Type Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static AssessmentSession Sess(string userId, string title, string type) => new AssessmentSession
    {
        UserId = userId,
        Title = title,
        Category = "Test",
        Status = "Open",
        AccessToken = "",
        Schedule = Sched,
        AssessmentType = type
    };

    // Type-AGNOSTIC key lama (replika kode pra-fix) — untuk meng-assert over-lock yang DIPERBAIKI.
    private static System.Linq.Expressions.Expression<Func<AssessmentSession, bool>> TypeAgnosticKey(
        string title, string category, DateTime scheduleDate)
        => s => s.Title == title && s.Category == category && s.Schedule.Date == scheduleDate.Date;

    // CORE: Pre mulai → lock-detection POST type-aware = FALSE (Pre TIDAK mengunci Post).
    //       Counter type-agnostic lama = TRUE (over-lock yang diperbaiki SHFX-06).
    [Fact]
    public async Task PreStarted_DoesNotLockPost_WhenTypeAware()
    {
        var marker = "SIBT-A-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u = await SeedUserAsync(ctx);
            var pre = Sess(u, marker, "PreTest");
            pre.StartedAt = DateTime.UtcNow;     // peserta Pre MULAI
            var post = Sess(u, marker, "PostTest");   // Post belum mulai
            ctx.AssessmentSessions.AddRange(pre, post);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = new ApplicationDbContext(_fixture.Options);

        // Lock-detection untuk POST (assessmentType="PostTest") via predicate TYPE-AWARE.
        var postLockSiblings = await ctx2.AssessmentSessions
            .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(marker, "Test", Sched.Date, "PostTest"))
            .Select(s => s.Id).ToListAsync();
        bool postAnyStarted = await ctx2.AssessmentSessions
            .AnyAsync(s => postLockSiblings.Contains(s.Id) && s.StartedAt != null);
        bool postLocked = ShuffleToggleRules.IsShuffleLocked(postAnyStarted, false);

        Assert.False(postLocked);   // ✅ FIX: Pre mulai TIDAK mengunci Post

        // Counter: key type-agnostic lama mencampur Pre → over-lock (TRUE) — bug yang diperbaiki.
        var agnosticSiblings = await ctx2.AssessmentSessions
            .Where(TypeAgnosticKey(marker, "Test", Sched.Date))
            .Select(s => s.Id).ToListAsync();
        bool agnosticAnyStarted = await ctx2.AssessmentSessions
            .AnyAsync(s => agnosticSiblings.Contains(s.Id) && s.StartedAt != null);
        Assert.True(ShuffleToggleRules.IsShuffleLocked(agnosticAnyStarted, false)); // over-lock lama
    }

    // Symmetry: Post mulai → lock-detection PRE type-aware = FALSE (Post TIDAK mengunci Pre).
    [Fact]
    public async Task PostStarted_DoesNotLockPre_WhenTypeAware()
    {
        var marker = "SIBT-B-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u = await SeedUserAsync(ctx);
            var pre = Sess(u, marker, "PreTest");
            var post = Sess(u, marker, "PostTest");
            post.StartedAt = DateTime.UtcNow;     // peserta Post MULAI
            ctx.AssessmentSessions.AddRange(pre, post);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = new ApplicationDbContext(_fixture.Options);
        var preLockSiblings = await ctx2.AssessmentSessions
            .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(marker, "Test", Sched.Date, "PreTest"))
            .Select(s => s.Id).ToListAsync();
        bool preAnyStarted = await ctx2.AssessmentSessions
            .AnyAsync(s => preLockSiblings.Contains(s.Id) && s.StartedAt != null);
        Assert.False(ShuffleToggleRules.IsShuffleLocked(preAnyStarted, false)); // Post mulai tak mengunci Pre
    }

    // PROPAGATION no-regress: sibling cross-type (type-AGNOSTIC, dipakai foreach write shuffle)
    // MASIH mencakup BAIK Pre maupun Post — propagation Pre↔Post share shuffle by design TIDAK berubah.
    [Fact]
    public async Task ShufflePropagation_RemainsCrossType_PreAndPost()
    {
        var marker = "SIBT-P-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int preId, postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u = await SeedUserAsync(ctx);
            var pre = Sess(u, marker, "PreTest");
            var post = Sess(u, marker, "PostTest");
            ctx.AssessmentSessions.AddRange(pre, post);
            await ctx.SaveChangesAsync();
            preId = pre.Id; postId = post.Id;
        }

        await using var ctx2 = new ApplicationDbContext(_fixture.Options);
        // Propagation memakai key type-AGNOSTIC (siblingSessionIds di UpdateShuffleSettings :5655-5660).
        var propagationSiblings = await ctx2.AssessmentSessions
            .Where(TypeAgnosticKey(marker, "Test", Sched.Date))
            .Select(s => s.Id).ToListAsync();

        Assert.Contains(preId, propagationSiblings);   // Pre tercakup
        Assert.Contains(postId, propagationSiblings);  // Post tercakup → cross-type propagation utuh
        Assert.Equal(2, propagationSiblings.Count);
    }
}
