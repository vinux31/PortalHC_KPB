// Phase 382 WSE-06 (SAVE-01) — GradingService MC scoring membaca jawaban FINAL per soal.
//
// DEVIATION dari PLAN (Rule 3 blocking): plan menyebut "InMemory", tapi GradeAndCompleteAsync memakai
// ExecuteUpdateAsync yang TIDAK didukung EF Core 8 InMemory provider (throw InvalidOperationException
// sebelum mencapai logika yang diuji). Maka dipakai disposable real-SQL fixture (pola
// EssayFinalizeRecomputeFixture / ProtonCompletionFixture) — HcPortalDB_Test_{guid} @localhost\SQLEXPRESS,
// MigrateAsync penuh, drop on dispose. [Trait Category=Integration] → skip via --filter "Category!=Integration".
// DB lokal HcPortalDB_Dev TAK tersentuh — no SEED_WORKFLOW snapshot/restore.
//
// Test A (Dedupe_PicksLatestSubmittedAt): satu soal MC dengan 2 PackageUserResponse (opsi beda,
//   SubmittedAt beda) → Score harus mencerminkan opsi FINAL (SubmittedAt terbaru), bukan baris pertama. RED.
// Test B (Dedupe_MultipleAnswer_NotDeduped): soal MA dengan 2 baris response benar → MA tetap dinilai
//   penuh (tidak ter-dedupe jadi 1 baris). GREEN sebelum & sesudah fix (MA tak disentuh).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class GradingDedupeFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public GradingDedupeFixture()
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
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 382 GradingDedupe setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug dedupe. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class GradingDedupeTests : IClassFixture<GradingDedupeFixture>
{
    private readonly GradingDedupeFixture _fixture;
    public GradingDedupeTests(GradingDedupeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // GradingService punya dependency berat (IWorkerDataService, ProtonCompletionService, ProtonBypassService).
    // Untuk session NON-Proton (Category != "Assessment Proton") branch Proton tak terpanggil; cukup fake
    // IWorkerDataService (NotifyIfGroupCompleted no-op) + Proton service di-instantiate dengan fake notif/audit.
    private GradingService NewGradingService(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "ded-" + Guid.NewGuid().ToString("N")[..8], Email = "ded@test.local", FullName = "Dedupe Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    // ---- Test A: MC dengan 2 response (opsi beda, SubmittedAt beda) → Score = opsi FINAL ----
    [Fact]
    public async Task Dedupe_PicksLatestSubmittedAt()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);

        var session = new AssessmentSession
        {
            UserId = userId, Title = "MC Dedupe", Category = "IHT", Status = S.InProgress, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70, GenerateCertificate = false
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        // 1 soal MC, ScoreValue 100. Opsi A = benar, Opsi B = salah.
        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "MultipleChoice", ScoreValue = 100, Order = 1, QuestionText = "Pilih A." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        var optA = new PackageOption { PackageQuestionId = q.Id, OptionText = "A (benar)", IsCorrect = true };
        var optB = new PackageOption { PackageQuestionId = q.Id, OptionText = "B (salah)", IsCorrect = false };
        ctx.PackageOptions.AddRange(optA, optB);
        await ctx.SaveChangesAsync();

        var t0 = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        // Baris-1 (basi): pilih B (salah), SubmittedAt = t0.
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optB.Id, SubmittedAt = t0 });
        // Baris-2 (FINAL): pilih A (benar), SubmittedAt = t0 + 10s.
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optA.Id, SubmittedAt = t0.AddSeconds(10) });
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId,
            ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id })
        });
        await ctx.SaveChangesAsync();

        var svc = NewGradingService(ctx);
        var ok = await svc.GradeAndCompleteAsync(session);

        Assert.True(ok);
        await using var verify = NewCtx();
        var graded = await verify.AssessmentSessions.FindAsync(session.Id);
        // FINAL = opsi A benar → 100%. Jika dedupe gagal & ambil baris basi (B) → 0%.
        Assert.Equal(100, graded!.Score);
        Assert.True(graded.IsPassed);
    }

    // ---- Test B: MA dengan 2 baris response benar → MA tetap dinilai penuh (tidak ter-dedupe) ----
    [Fact]
    public async Task Dedupe_MultipleAnswer_NotDeduped()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);

        var session = new AssessmentSession
        {
            UserId = userId, Title = "MA NoDedupe", Category = "IHT", Status = S.InProgress, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70, GenerateCertificate = false
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        // 1 soal MA, ScoreValue 100. Opsi X & Y = benar, Z = salah. All-or-nothing: harus pilih X+Y persis.
        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "MultipleAnswer", ScoreValue = 100, Order = 1, QuestionText = "Pilih X dan Y." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        var optX = new PackageOption { PackageQuestionId = q.Id, OptionText = "X (benar)", IsCorrect = true };
        var optY = new PackageOption { PackageQuestionId = q.Id, OptionText = "Y (benar)", IsCorrect = true };
        var optZ = new PackageOption { PackageQuestionId = q.Id, OptionText = "Z (salah)", IsCorrect = false };
        ctx.PackageOptions.AddRange(optX, optY, optZ);
        await ctx.SaveChangesAsync();

        var t0 = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        // DUA baris response sah untuk soal MA yang sama (X dan Y) — keduanya HARUS terbaca.
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optX.Id, SubmittedAt = t0 });
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optY.Id, SubmittedAt = t0.AddSeconds(5) });
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId,
            ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id })
        });
        await ctx.SaveChangesAsync();

        var svc = NewGradingService(ctx);
        var ok = await svc.GradeAndCompleteAsync(session);

        Assert.True(ok);
        await using var verify = NewCtx();
        var graded = await verify.AssessmentSessions.FindAsync(session.Id);
        // MA all-or-nothing: pilih {X,Y} = set benar → 100%. Jika MA salah ter-dedupe jadi 1 baris →
        // set {X} ≠ {X,Y} → 0%. Maka 100% membuktikan MA TIDAK ter-dedupe.
        Assert.Equal(100, graded!.Score);
        Assert.True(graded.IsPassed);
    }

    // ============================================================================================
    // Phase 424 GRDF-02 PARITY LOCK (D-07): GradeAndCompleteAsync (PATH 1, kanonik) HARUS ==
    // AssessmentScoreAggregator.Compute (PATH 3) untuk kasus normal (single MC, MA). Hijau SEKARANG dan
    // WAJIB tetap hijau setelah Task 2 mengkonvergensikan Aggregator MC ke last-write-wins → bukti skor
    // sesi normal tidak berubah (forward-only, sesi Completed lama aman).
    // ============================================================================================

    [Fact]
    public async Task Parity_SingleMc_GradeAndComplete_Equals_Aggregator()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var session = new AssessmentSession
        {
            UserId = userId, Title = "MC Parity", Category = "IHT", Status = S.InProgress, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70, GenerateCertificate = false
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();
        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "P", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "MultipleChoice", ScoreValue = 100, Order = 1, QuestionText = "Pilih A." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        var optA = new PackageOption { PackageQuestionId = q.Id, OptionText = "A", IsCorrect = true };
        var optB = new PackageOption { PackageQuestionId = q.Id, OptionText = "B", IsCorrect = false };
        ctx.PackageOptions.AddRange(optA, optB);
        await ctx.SaveChangesAsync();
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optA.Id, SubmittedAt = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc) });
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        { AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId, ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id }) });
        await ctx.SaveChangesAsync();

        var svc = NewGradingService(ctx);
        Assert.True(await svc.GradeAndCompleteAsync(session));

        await using var verify = NewCtx();
        var graded = await verify.AssessmentSessions.FindAsync(session.Id);
        var questions = await verify.PackageQuestions.Include(x => x.Options).Where(x => x.AssessmentPackageId == pkg.Id).ToListAsync();
        var responses = await verify.PackageUserResponses.Where(r => r.AssessmentSessionId == session.Id).ToListAsync();
        var agg = AssessmentScoreAggregator.Compute(questions, responses, session.PassPercentage);
        Assert.Equal(100, graded!.Score);
        Assert.Equal(graded.Score, agg.Percentage);   // PATH 1 == PATH 3
    }

    [Fact]
    public async Task Parity_Ma_GradeAndComplete_Equals_Aggregator()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var session = new AssessmentSession
        {
            UserId = userId, Title = "MA Parity", Category = "IHT", Status = S.InProgress, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70, GenerateCertificate = false
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();
        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "P", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "MultipleAnswer", ScoreValue = 100, Order = 1, QuestionText = "Pilih X dan Y." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        var optX = new PackageOption { PackageQuestionId = q.Id, OptionText = "X", IsCorrect = true };
        var optY = new PackageOption { PackageQuestionId = q.Id, OptionText = "Y", IsCorrect = true };
        var optZ = new PackageOption { PackageQuestionId = q.Id, OptionText = "Z", IsCorrect = false };
        ctx.PackageOptions.AddRange(optX, optY, optZ);
        await ctx.SaveChangesAsync();
        var t0 = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optX.Id, SubmittedAt = t0 });
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optY.Id, SubmittedAt = t0.AddSeconds(5) });
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        { AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId, ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id }) });
        await ctx.SaveChangesAsync();

        var svc = NewGradingService(ctx);
        Assert.True(await svc.GradeAndCompleteAsync(session));

        await using var verify = NewCtx();
        var graded = await verify.AssessmentSessions.FindAsync(session.Id);
        var questions = await verify.PackageQuestions.Include(x => x.Options).Where(x => x.AssessmentPackageId == pkg.Id).ToListAsync();
        var responses = await verify.PackageUserResponses.Where(r => r.AssessmentSessionId == session.Id).ToListAsync();
        var agg = AssessmentScoreAggregator.Compute(questions, responses, session.PassPercentage);
        Assert.Equal(100, graded!.Score);            // MA all-or-nothing utuh
        Assert.Equal(graded.Score, agg.Percentage);  // PATH 1 == PATH 3 (MA tak ter-dedupe di kedua path)
    }
}
