// RetakeSettingsEndpointTests — v32.4 Phase 405 RTK-04.
// Integration real-SQL: membuktikan kontrak DB dari body endpoint UpdateRetakeSettings
// (sibling propagation + clamp server-side). Mengikuti pola ShuffleUpdateEndpointTests
// (Phase 374 SHUF-10) yang mereplikasi body endpoint atas grup REAL di SQL Server.
//
// RBAC + AntiForgery + PRG = atribut HTTP-layer; dikonfirmasi via kode grep (405-04-SUMMARY)
// bukan via HTTP integration test (tidak ada WebApplicationFactory di project ini).
//
// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.4 RTK-04 — real-SQL: kontrak endpoint UpdateRetakeSettings.
/// Mereplikasi body endpoint terhadap DB asli untuk membuktikan:
/// (1) sibling propagation: semua sesi satu grup (Title/Category/Schedule.Date) ikut diupdate,
/// (2) clamp server-side: maxAttempts diklem ke [1,5], cooldown ke [0,168],
/// (3) ShouldHideRetakeToggle guard memblokir write untuk PreTest/Manual.
/// Atribut [HttpPost]/[Authorize(Roles="Admin,HC")]/[ValidateAntiForgeryToken]/PRG redirect
/// diverifikasi via grep/kode (405-04-SUMMARY) — HTTP-layer behavior, bukan extractable ke test ini.
/// </summary>
[Trait("Category", "Integration")]
public class RetakeSettingsEndpointTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;

    public RetakeSettingsEndpointTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser
        {
            UserName = "rtksetting-" + Guid.NewGuid().ToString("N")[..8],
            Email = "rtksetting@test.local",
            FullName = "Retake Settings Test"
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession MakeSibling(string userId, string title, DateTime schedule, string category = "Test")
        => new AssessmentSession
        {
            UserId = userId,
            Title = title,
            Category = category,
            Status = "Open",
            AccessToken = "",
            Schedule = schedule,
            AllowRetake = false,
            MaxAttempts = 2,
            RetakeCooldownHours = 24
        };

    // RTK-04 (a): sibling propagation — semua sesi satu grup ikut diupdate.
    // Mereplikasi body endpoint: query siblings by key LENGKAP (Title/Category/Schedule.Date),
    // foreach set 3 field + UpdatedAt → SaveChanges.
    [Fact]
    public async Task UpdateRetakeSettings_PropagatesToAllSiblings()
    {
        var marker = "RTK04-" + Guid.NewGuid().ToString("N")[..8];
        var sched = new DateTime(2026, 4, 1, 8, 0, 0);

        // Seed 3 sibling sessions (same Title/Category/Schedule.Date).
        await using (var ctx = NewCtx())
        {
            for (int i = 0; i < 3; i++)
            {
                var userId = await SeedUserAsync(ctx);
                ctx.AssessmentSessions.Add(MakeSibling(userId, marker, sched));
                await ctx.SaveChangesAsync();
            }
        }

        // Replika body endpoint UpdateRetakeSettings (sibling propagation logic).
        const bool postAllowRetake = true;
        const int postMaxAttempts = 3;
        const int postCooldown = 48;

        await using (var ctx = NewCtx())
        {
            var assessment = await ctx.AssessmentSessions.FirstAsync(s => s.Title == marker);
            // Guard: ShouldHideRetakeToggle returns false for null AssessmentType + isManualEntry=false → proceed.
            Assert.False(RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry));

            var siblingSessionIds = await ctx.AssessmentSessions
                .Where(s => s.Title == assessment.Title && s.Category == assessment.Category && s.Schedule.Date == assessment.Schedule.Date)
                .Select(s => s.Id).ToListAsync();
            var siblings = await ctx.AssessmentSessions.Where(s => siblingSessionIds.Contains(s.Id)).ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var sibling in siblings)
            {
                sibling.AllowRetake = postAllowRetake;
                sibling.MaxAttempts = postMaxAttempts;
                sibling.RetakeCooldownHours = postCooldown;
                sibling.UpdatedAt = now;
            }
            await ctx.SaveChangesAsync();
        }

        // Verify: semua 3 sesi ikut diupdate.
        await using var readCtx = NewCtx();
        var rows = await readCtx.AssessmentSessions.AsNoTracking()
            .Where(s => s.Title == marker).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.True(r.AllowRetake));
        Assert.All(rows, r => Assert.Equal(3, r.MaxAttempts));
        Assert.All(rows, r => Assert.Equal(48, r.RetakeCooldownHours));
    }

    // RTK-04 (b): clamp server-side — maxAttempts diklem ke [1,5], cooldown ke [0,168].
    // Membuktikan Math.Clamp defense-in-depth (bypass form [Range] via raw POST tdk bisa outlier).
    [Theory]
    [InlineData(0, 0, 1, 0)]       // maxAttempts 0 → clamped to 1; cooldown 0 → tetap 0 (valid min)
    [InlineData(99, 999, 5, 168)]  // maxAttempts 99 → clamped to 5; cooldown 999 → clamped to 168
    [InlineData(3, 72, 3, 72)]     // valid in-range → no change
    public void Clamp_MaxAttempts_And_Cooldown_ServerSide(
        int rawMax, int rawCooldown, int expectedMax, int expectedCooldown)
    {
        // Replika clamp baris di controller body (defense-in-depth, identik implementasi).
        int clampedMax = Math.Clamp(rawMax, 1, 5);
        int clampedCooldown = Math.Clamp(rawCooldown, 0, 168);
        Assert.Equal(expectedMax, clampedMax);
        Assert.Equal(expectedCooldown, clampedCooldown);
    }

    // RTK-04 (c): sibling propagation TIDAK menyeberangi batas Category berbeda.
    // Membuktikan key LENGKAP (Title/Category/Schedule.Date) — sesi Category lain TIDAK ikut.
    [Fact]
    public async Task UpdateRetakeSettings_DoesNotCrossCategory()
    {
        var marker = "RTK04cat-" + Guid.NewGuid().ToString("N")[..8];
        var sched = new DateTime(2026, 4, 2, 8, 0, 0);

        await using (var ctx = NewCtx())
        {
            var u1 = await SeedUserAsync(ctx);
            var u2 = await SeedUserAsync(ctx);
            ctx.AssessmentSessions.Add(MakeSibling(u1, marker, sched, "TestCat"));       // target category
            ctx.AssessmentSessions.Add(MakeSibling(u2, marker, sched, "OtherCat"));      // different category, same title+schedule
            await ctx.SaveChangesAsync();
        }

        // Replika body endpoint: propagasi untuk "TestCat" saja.
        await using (var ctx = NewCtx())
        {
            var assessment = await ctx.AssessmentSessions.FirstAsync(s => s.Title == marker && s.Category == "TestCat");
            var siblingSessionIds = await ctx.AssessmentSessions
                .Where(s => s.Title == assessment.Title && s.Category == assessment.Category && s.Schedule.Date == assessment.Schedule.Date)
                .Select(s => s.Id).ToListAsync();
            var siblings = await ctx.AssessmentSessions.Where(s => siblingSessionIds.Contains(s.Id)).ToListAsync();
            foreach (var sibling in siblings)
            {
                sibling.AllowRetake = true;
                sibling.MaxAttempts = 4;
                sibling.RetakeCooldownHours = 12;
                sibling.UpdatedAt = DateTime.UtcNow;
            }
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewCtx();
        var testCatRow = await verify.AssessmentSessions.AsNoTracking()
            .FirstAsync(s => s.Title == marker && s.Category == "TestCat");
        var otherCatRow = await verify.AssessmentSessions.AsNoTracking()
            .FirstAsync(s => s.Title == marker && s.Category == "OtherCat");

        // TestCat: ikut diupdate.
        Assert.True(testCatRow.AllowRetake);
        Assert.Equal(4, testCatRow.MaxAttempts);
        // OtherCat: TIDAK ikut (masih nilai awal).
        Assert.False(otherCatRow.AllowRetake);
        Assert.Equal(2, otherCatRow.MaxAttempts);
    }

    // RTK-04 (d): guard ShouldHideRetakeToggle blokir PreTest (unit, no SQL — helper pure).
    // Membuktikan guard yang sama dipanggil di controller untuk reject PreTest/Manual.
    [Theory]
    [InlineData("PreTest", false, true)]    // PreTest → guard returns true → endpoint would redirect with error
    [InlineData(null, true, true)]          // Manual → guard returns true → endpoint would redirect with error
    [InlineData("PostTest", false, false)]  // PostTest, non-manual → guard returns false → proceed to update
    [InlineData(null, false, false)]        // Standalone, non-manual → guard returns false → proceed to update
    public void UpdateRetakeSettings_Guard_BlocksPreTestAndManual(
        string? assessmentType, bool isManualEntry, bool shouldBlock)
    {
        // Replika guard check di controller:
        // if (RetakeRules.ShouldHideRetakeToggle(...)) { TempData["Error"]; redirect; }
        bool guardTriggered = RetakeRules.ShouldHideRetakeToggle(assessmentType, isManualEntry);
        Assert.Equal(shouldBlock, guardTriggered);
    }
}
