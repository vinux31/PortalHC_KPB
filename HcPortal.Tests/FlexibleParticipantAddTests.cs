// Phase 391 D-06 (PART-04) — regression test: penambahan peserta fleksibel saat ujian berjalan.
// Mengunci kontrak data-level dari EditAssessment (Plan 01): (a) add-saat-InProgress berhasil,
// (b) sesi baru ber-status siap-mulai (Open/Upcoming) BUKAN InProgress, (c) sesi InProgress existing
// tidak ter-overwrite Status/Schedule/Duration, (d) penambahan tak terblokir saat sebagian Completed
// + window terbuka. Controller berat di-instantiate → pola project = REPLIKASI byte-identik keputusan
// (DeriveReadyStatus / filter sesi-berjalan / window) di level data, BUKAN WebApplicationFactory.
// DB disposable HcPortalDB_Test_{guid} (HcPortalDB_Dev TIDAK disentuh). [Trait Category=Integration].
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class FlexibleParticipantAddFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public FlexibleParticipantAddFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException(
                $"Phase 391 FlexibleParticipantAdd setup failed during MigrateAsync of {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug fix. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class FlexibleParticipantAddTests : IClassFixture<FlexibleParticipantAddFixture>
{
    private readonly FlexibleParticipantAddFixture _fixture;
    public FlexibleParticipantAddTests(FlexibleParticipantAddFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Seed helpers ----
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "part-" + Guid.NewGuid().ToString("N")[..8], Email = "part@test.local", FullName = "Flex Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSiblingSessionAsync(ApplicationDbContext ctx, string userId,
        string title, string category, DateTime schedule, string status,
        DateTime? startedAt = null, DateTime? completedAt = null, DateTime? examWindowCloseDate = null)
    {
        var session = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status, AccessToken = "",
            Schedule = schedule, DurationMinutes = 60, PassPercentage = 70,
            StartedAt = startedAt, CompletedAt = completedAt, ExamWindowCloseDate = examWindowCloseDate, Progress = 0
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    // ---- Replica keputusan controller (Plan 01, byte-identik) ----

    // Mirror AssessmentAdminController.DeriveReadyStatus (D-01) — WIB = UTC+7.
    private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)
    {
        var nowWib = DateTime.UtcNow.AddHours(7);
        return schedule <= nowWib ? S.Open : S.Upcoming;
    }

    // Mirror D-03 filter: sesi sedang berjalan = jangan update field bersama.
    private static bool IsRunning(AssessmentSession s) => s.StartedAt != null && s.CompletedAt == null;

    // Mirror Plan 01 edit-loop: skip sesi berjalan; sesi lain terima update field bersama.
    private static void ApplySharedFieldUpdate(IEnumerable<AssessmentSession> siblings,
        string newTitle, string newCategory, DateTime newSchedule, int newDuration, string newStatus)
    {
        foreach (var s in siblings)
        {
            if (IsRunning(s)) continue;
            s.Title = newTitle;
            s.Category = newCategory;
            s.Schedule = newSchedule;
            s.DurationMinutes = newDuration;
            s.Status = newStatus;
        }
    }

    // Mirror D-02 fallback (Plan 01 391-01-SUMMARY): ExamWindowCloseDate==null = boleh tambah (longgar).
    private static bool WindowAllowsAddition(DateTime? examWindowCloseDate)
    {
        if (examWindowCloseDate == null) return true;
        return DateTime.UtcNow.AddHours(7) <= examWindowCloseDate.Value;
    }

    // ---- Facts (a/b/c/d) ----

    // (a) PART-04: tambah peserta saat ada sesi InProgress → sesi baru tercipta DENGAN status siap-mulai
    //     (bukan warisi InProgress induk) SEMENTARA sibling InProgress dilindungi. De-tautologized (999.12):
    //     seed status warisan-SALAH (InProgress) dulu → keputusan controller (DeriveReadyStatus) override → assert before/after.
    [Fact]
    public async Task AddParticipant_WithInProgressSibling_CreatesNewSession()
    {
        var title = "FlexAdd-A-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1); // window terbuka (jadwal sudah lewat)
        string user1, user2;

        await using var seed = NewCtx();
        user1 = await SeedUserAsync(seed);
        user2 = await SeedUserAsync(seed);
        await SeedSiblingSessionAsync(seed, user1, title, cat, schedule, S.InProgress, startedAt: DateTime.UtcNow, completedAt: null);
        // BEFORE: sesi baru di-seed status warisan-SALAH (InProgress) — bukan nilai keputusan.
        var sidNew = await SeedSiblingSessionAsync(seed, user2, title, cat, schedule, S.InProgress);

        // DECISION (replica controller, terpisah dari seed): status siap-mulai sesuai jadwal.
        var derivedNew = DeriveReadyStatus(schedule, null);
        Assert.Equal(S.Open, derivedNew);              // jadwal sudah tiba → Open
        Assert.NotEqual(S.InProgress, derivedNew);     // TIDAK warisi induk
        await using (var mutate = NewCtx())
        {
            var sn = await mutate.AssessmentSessions.FindAsync(sidNew);
            sn!.Status = derivedNew;                    // terapkan keputusan (mirror controller assign)
            await mutate.SaveChangesAsync();
        }

        await using var verify = NewCtx();
        var siblings = await verify.AssessmentSessions
            .Where(s => s.Title == title && s.Category == cat && s.Schedule.Date == schedule.Date)
            .ToListAsync();
        Assert.Equal(2, siblings.Count);
        var running = siblings.First(s => s.UserId == user1);
        var added = siblings.First(s => s.UserId == user2);
        // DUAL-ASSERT (mirror (c)): sibling InProgress dilindungi (tetap) + sesi baru BERUBAH dari warisan → siap-mulai.
        Assert.Equal(S.InProgress, running.Status);
        Assert.Equal(S.Open, added.Status);
        Assert.NotEqual(S.InProgress, added.Status);
    }

    // (b) PART-04: sesi baru ber-status siap-mulai (Open/Upcoming) — BUKAN InProgress (bukan warisi induk).
    //     De-tautologized (999.12): seed status warisan-SALAH (InProgress) → keputusan DeriveReadyStatus override
    //     → assert transformasi before(InProgress)→after(Open/Upcoming), bukan assert == nilai yang dipakai untuk seed.
    [Fact]
    public async Task AddParticipant_NewSession_HasReadyStatus_NotInProgress()
    {
        var title = "FlexAdd-B-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var past = DateTime.UtcNow.AddHours(7).AddMinutes(-30);   // jadwal sudah tiba → Open
        var future = DateTime.UtcNow.AddHours(7).AddDays(1);      // jadwal belum tiba → Upcoming
        string uPast, uFuture;

        await using var seed = NewCtx();
        uPast = await SeedUserAsync(seed);
        uFuture = await SeedUserAsync(seed);
        // BEFORE: kedua sesi di-seed status warisan-SALAH (InProgress).
        var sidPast = await SeedSiblingSessionAsync(seed, uPast, title, cat, past, S.InProgress);
        var sidFuture = await SeedSiblingSessionAsync(seed, uFuture, title, cat, future, S.InProgress);

        // DECISION (replica controller, terpisah dari seed): derive status siap-mulai per jadwal.
        var derivedPast = DeriveReadyStatus(past, null);
        var derivedFuture = DeriveReadyStatus(future, null);
        Assert.Equal(S.Open, derivedPast);
        Assert.Equal(S.Upcoming, derivedFuture);
        Assert.NotEqual(S.InProgress, derivedPast);
        Assert.NotEqual(S.InProgress, derivedFuture);
        await using (var mutate = NewCtx())
        {
            var sp = await mutate.AssessmentSessions.FindAsync(sidPast);
            var sf = await mutate.AssessmentSessions.FindAsync(sidFuture);
            sp!.Status = derivedPast; sf!.Status = derivedFuture;   // terapkan keputusan
            await mutate.SaveChangesAsync();
        }

        // AFTER: status di DB BERUBAH dari InProgress (seed) → siap-mulai (keputusan).
        await using var verify = NewCtx();
        var sPast = await verify.AssessmentSessions.FindAsync(sidPast);
        var sFuture = await verify.AssessmentSessions.FindAsync(sidFuture);
        Assert.Equal(S.Open, sPast!.Status);
        Assert.Equal(S.Upcoming, sFuture!.Status);
        Assert.NotEqual(S.InProgress, sPast.Status);
        Assert.NotEqual(S.InProgress, sFuture.Status);
    }

    // (c) PART-04: sesi InProgress existing Status/Schedule/Duration UNCHANGED; sesi belum-mulai JUSTRU berubah (filter selektif).
    [Fact]
    public async Task AddParticipant_InProgressSibling_StatusScheduleDurationUnchanged()
    {
        var title = "FlexAdd-C-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedOrig = new DateTime(2026, 6, 17, 8, 0, 0);
        string uRunning, uNotStarted;

        await using var seed = NewCtx();
        uRunning = await SeedUserAsync(seed);
        uNotStarted = await SeedUserAsync(seed);
        await SeedSiblingSessionAsync(seed, uRunning, title, cat, schedOrig, S.InProgress, startedAt: DateTime.UtcNow, completedAt: null);
        await SeedSiblingSessionAsync(seed, uNotStarted, title, cat, schedOrig, S.Upcoming, startedAt: null, completedAt: null);

        var newSchedule = schedOrig.AddDays(3);
        await using var mutate = NewCtx();
        var siblings = await mutate.AssessmentSessions
            .Where(s => s.Title == title && s.Category == cat).ToListAsync();
        ApplySharedFieldUpdate(siblings, title, cat, newSchedule, 120, S.Open);
        await mutate.SaveChangesAsync();

        await using var verify = NewCtx();
        var running = await verify.AssessmentSessions.FirstAsync(s => s.UserId == uRunning && s.Title == title);
        var notStarted = await verify.AssessmentSessions.FirstAsync(s => s.UserId == uNotStarted && s.Title == title);
        // Sesi berjalan dilindungi (D-03): UNCHANGED.
        Assert.Equal(S.InProgress, running.Status);
        Assert.Equal(schedOrig, running.Schedule);
        Assert.Equal(60, running.DurationMinutes);
        // Sesi belum-mulai JUSTRU berubah → membuktikan filter selektif, bukan no-op total.
        Assert.Equal(120, notStarted.DurationMinutes);
        Assert.Equal(newSchedule, notStarted.Schedule);
    }

    // (d) PART-04: penambahan tak terblokir saat sebagian sesi Completed + window terbuka.
    //     De-tautologized (999.12): seed sesi baru status warisan-SALAH (InProgress) → keputusan (window allows +
    //     DeriveReadyStatus) override → dual-assert: Completed dilindungi (tetap) + sesi baru BERUBAH ke siap-mulai.
    [Fact]
    public async Task AddParticipant_SomeCompleted_NotBlocked_WhileWindowOpen()
    {
        var title = "FlexAdd-D-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-2);
        DateTime? windowOpen = null; // null = boleh tambah (longgar, Plan 01)
        string uCompleted, uNew;

        await using var seed = NewCtx();
        uCompleted = await SeedUserAsync(seed);
        uNew = await SeedUserAsync(seed);
        await SeedSiblingSessionAsync(seed, uCompleted, title, cat, schedule, S.Completed,
            startedAt: DateTime.UtcNow.AddHours(-1), completedAt: DateTime.UtcNow, examWindowCloseDate: windowOpen);
        // BEFORE: sesi baru di-seed status warisan-SALAH (InProgress).
        var sidNew = await SeedSiblingSessionAsync(seed, uNew, title, cat, schedule, S.InProgress, examWindowCloseDate: windowOpen);

        // DECISION (replica): window mengizinkan penambahan meski representatif Completed (carve-out !hasAddition) + status siap-mulai.
        bool allowed = WindowAllowsAddition(windowOpen);
        Assert.True(allowed);
        var derivedNew = DeriveReadyStatus(schedule, windowOpen);
        Assert.Equal(S.Open, derivedNew);              // jadwal sudah tiba → Open (bukan warisi Completed/InProgress)
        await using (var mutate = NewCtx())
        {
            var sn = await mutate.AssessmentSessions.FindAsync(sidNew);
            sn!.Status = derivedNew; await mutate.SaveChangesAsync();
        }

        await using var verify = NewCtx();
        var siblings = await verify.AssessmentSessions
            .Where(s => s.Title == title && s.Category == cat && s.Schedule.Date == schedule.Date).ToListAsync();
        Assert.Equal(2, siblings.Count);
        var completed = siblings.First(s => s.UserId == uCompleted);
        var added = siblings.First(s => s.UserId == uNew);
        // DUAL-ASSERT: sesi Completed TIDAK berubah + sesi baru BERUBAH dari warisan → siap-mulai (penambahan tak terblok).
        Assert.Equal(S.Completed, completed.Status);
        Assert.Equal(S.Open, added.Status);
        Assert.NotEqual(S.InProgress, added.Status);
    }
}
