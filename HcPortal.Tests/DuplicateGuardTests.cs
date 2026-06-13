// Phase 367 Plan 07 — guard duplikat EXACT 3 pintu (#12/#14, D-02). Menguji predikat SHARED
// AdminBaseController.ManualDuplicatePredicate (zero drift: SAMA dgn yg dijalankan AddManual/Import/BulkBackfill)
// terhadap real-SQL + logic dedup intra-batch (seenInBatch). EXACT match (CompletedAt ==), BUKAN ±1 hari (Pitfall 7).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class DuplicateGuardTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public DuplicateGuardTests(RecordCascadeFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "dup-" + Guid.NewGuid().ToString("N")[..8], Email = "dup@test.local", FullName = "Dup Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession Manual(string userId, string title, DateTime completedAt, bool isManual = true) =>
        new AssessmentSession
        {
            UserId = userId, Title = title, Category = "Test", Status = "Completed", AccessToken = "",
            Schedule = completedAt, CompletedAt = completedAt, IsManualEntry = isManual
        };

    // ── #12 — dup EXACT (user+judul+tanggal manual) TERDETEKSI (AddManual reject / Import+BulkBackfill skip) ──
    [Fact]
    public async Task ExactDup_SameUserTitleDate_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var date = new DateTime(2026, 4, 1);
        ctx.AssessmentSessions.Add(Manual(uid, "K3 Listrik", date));
        await ctx.SaveChangesAsync();
        Assert.True(await ctx.AssessmentSessions.AnyAsync(AdminBaseController.ManualDuplicatePredicate(uid, "K3 Listrik", date)));
    }

    // ── Pitfall 7 — tanggal beda (re-entry SAH) → TIDAK terdeteksi (LOLOS), no false-positive ──
    [Fact]
    public async Task DifferentDate_NotDetected_ReEntryPasses()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        ctx.AssessmentSessions.Add(Manual(uid, "K3 Listrik", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        Assert.False(await ctx.AssessmentSessions.AnyAsync(AdminBaseController.ManualDuplicatePredicate(uid, "K3 Listrik", new DateTime(2026, 4, 2))));
    }

    [Fact]
    public async Task DifferentTitle_NotDetected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var date = new DateTime(2026, 4, 1);
        ctx.AssessmentSessions.Add(Manual(uid, "K3 Listrik", date));
        await ctx.SaveChangesAsync();
        Assert.False(await ctx.AssessmentSessions.AnyAsync(AdminBaseController.ManualDuplicatePredicate(uid, "K3 Ketinggian", date)));
    }

    [Fact]
    public async Task DifferentUser_NotDetected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var other = await SeedUserAsync(ctx);
        var date = new DateTime(2026, 4, 1);
        ctx.AssessmentSessions.Add(Manual(uid, "K3 Listrik", date));
        await ctx.SaveChangesAsync();
        Assert.False(await ctx.AssessmentSessions.AnyAsync(AdminBaseController.ManualDuplicatePredicate(other, "K3 Listrik", date)));
    }

    // ── Guard HANYA manual: sesi ONLINE (IsManualEntry=false) key sama TIDAK dianggap dup (tak blok create manual) ──
    [Fact]
    public async Task OnlineSessionSameKey_NotDetected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var date = new DateTime(2026, 4, 1);
        ctx.AssessmentSessions.Add(Manual(uid, "K3 Listrik", date, isManual: false));
        await ctx.SaveChangesAsync();
        Assert.False(await ctx.AssessmentSessions.AnyAsync(AdminBaseController.ManualDuplicatePredicate(uid, "K3 Listrik", date)));
    }

    // ── #12 ImportTraining skip-with-report: dup → Status "Skip" + message "duplikat", row TIDAK di-Add ──
    [Fact]
    public async Task ImportSkip_DupRow_StatusSkip_NotAdded()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var date = new DateTime(2026, 4, 1);
        ctx.AssessmentSessions.Add(Manual(uid, "K3 Listrik", date));
        await ctx.SaveChangesAsync();
        int before = await ctx.AssessmentSessions.CountAsync();

        // replika branch import: dup → Skip + continue (no Add)
        var result = new ImportTrainingResult { NIP = "123", Judul = "K3 Listrik" };
        if (await ctx.AssessmentSessions.AnyAsync(AdminBaseController.ManualDuplicatePredicate(uid, "K3 Listrik", date)))
        {
            result.Status = "Skip";
            result.Message = "duplikat — dilewati";
        }
        Assert.Equal("Skip", result.Status);
        Assert.Contains("duplikat", result.Message);
        Assert.Equal(before, await ctx.AssessmentSessions.CountAsync());
    }

    // ── #14 BulkBackfill intra-batch: 2 baris NIP sama dalam 1 file → baris kedua skip (seenInBatch HashSet) ──
    [Fact]
    public void IntraBatch_SecondIdenticalRow_Skipped()
    {
        var existingUserIds = new HashSet<string>();   // DB kosong
        var seenInBatch = new HashSet<string>();
        var skipped = new List<string>();
        int success = 0;
        foreach (var id in new[] { "U1", "U1", "U2" })
        {
            if (existingUserIds.Contains(id) || !seenInBatch.Add(id)) { skipped.Add(id); continue; }
            success++;
        }
        Assert.Equal(2, success);     // U1(pertama) + U2
        Assert.Single(skipped);       // U1(kedua)
        Assert.Equal("U1", skipped[0]);
    }

    // ── #14 BulkBackfill DB-existing: UserId sudah ada (title+date sama) → skip, success TIDAK bertambah ──
    [Fact]
    public void BulkBackfill_ExistingUserId_Skipped_NoSuccessIncrement()
    {
        var existingUserIds = new HashSet<string> { "U1" };
        var seenInBatch = new HashSet<string>();
        var skipped = new List<string>();
        int success = 0;
        foreach (var id in new[] { "U1", "U2" })
        {
            if (existingUserIds.Contains(id) || !seenInBatch.Add(id)) { skipped.Add(id); continue; }
            success++;
        }
        Assert.Equal(1, success);     // hanya U2
        Assert.Contains("U1", skipped);
    }

    // ── BulkBackfill user berbeda semua → semua masuk (no false-skip) ──
    [Fact]
    public void IntraBatch_DifferentUsers_AllAdded()
    {
        var existingUserIds = new HashSet<string>();
        var seenInBatch = new HashSet<string>();
        int success = 0;
        foreach (var id in new[] { "U1", "U2", "U3" })
            if (!(existingUserIds.Contains(id) || !seenInBatch.Add(id))) success++;
        Assert.Equal(3, success);
    }
}
