// Phase 393 INJ-01/02 — Wave 0 test-infra (Plan 01). Kelas test Integration + fixture disposable
// real-SQL + factory real GradingService + seed helper + 5 STUB fact (SC1..SC5).
//
// Plan 01 SENGAJA hanya menetapkan KONTRAK: 5 fact ini HIJAU sebagai placeholder build-only
// (Assert.True(true)), BUKAN RED. Plan 03 mengganti body dengan assertion nyata yang mengunci
// SC1..SC5 (read-after-commit, byte-identik vs AssessmentScoreAggregator.Compute).
//
// Fixture pakai disposable DB HcPortalDB_Test_{guid} + EnsureDeletedAsync. DB lokal HcPortalDB_Dev
// TAK tersentuh (VALIDATION Wave 0 req #3). [Trait Category=Integration] → skip via "Category!=Integration".
// EF Core 8 ExecuteUpdateAsync (dipakai GradeAndCompleteAsync) butuh real-SQL, BUKAN InMemory provider.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class InjectAssessmentFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public InjectAssessmentFixture()
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
                $"Phase 393 InjectAssessment setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug inject. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class InjectAssessmentServiceTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectAssessmentServiceTests(InjectAssessmentFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Real GradingService (delegasi grading Plan 02) — SALIN VERBATIM dari SubmitResurrectionTests:68-76.
    // Semua fake/service sudah ada di HcPortal.Tests/ — JANGAN buat baru.
    private GradingService NewGradingService(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    // Factory service inject dengan real GradingService (key-link Plan 01).
    private InjectAssessmentService NewInjectService(ApplicationDbContext ctx)
        => new InjectAssessmentService(ctx, NewGradingService(ctx), NullLogger<InjectAssessmentService>.Instance);

    // Seed worker dengan NIP (inject resolve by NIP — WAJIB set NIP). UserName/Email unik via Guid.
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string nip)
    {
        var u = new ApplicationUser
        {
            UserName = "inj-" + Guid.NewGuid().ToString("N")[..8],
            Email = $"inj-{Guid.NewGuid().ToString("N")[..8]}@test.local",
            FullName = "Inject Test " + nip,
            NIP = nip
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    // InjectRequest contoh minimal: 1 paket (MC + MA + Essay), satu worker per NIP dengan jawaban
    // semua-benar. TempId memetakan jawaban worker → soal/opsi pre-persist. Plan 03 boleh
    // memperluas (variasi salah/parsial, multi-worker, backdate, cert mode).
    private static InjectRequest BuildSampleRequest(string nip, InjectCertMode certMode = InjectCertMode.None)
    {
        return new InjectRequest
        {
            Title = "Inject Sample " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT",
            AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 60,
            PassPercentage = 70,
            AllowAnswerReview = true,
            CertMode = certMode,
            Questions = new List<InjectQuestionSpec>
            {
                new InjectQuestionSpec
                {
                    TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10,
                    QuestionText = "Soal MC: pilih A.",
                    Options = new List<InjectOptionSpec>
                    {
                        new InjectOptionSpec { TempId = 1, OptionText = "A (benar)", IsCorrect = true },
                        new InjectOptionSpec { TempId = 2, OptionText = "B (salah)", IsCorrect = false },
                    }
                },
                new InjectQuestionSpec
                {
                    TempId = 2, Order = 2, QuestionType = "MultipleAnswer", ScoreValue = 10,
                    QuestionText = "Soal MA: pilih A & B.",
                    Options = new List<InjectOptionSpec>
                    {
                        new InjectOptionSpec { TempId = 3, OptionText = "A (benar)", IsCorrect = true },
                        new InjectOptionSpec { TempId = 4, OptionText = "B (benar)", IsCorrect = true },
                        new InjectOptionSpec { TempId = 5, OptionText = "C (salah)", IsCorrect = false },
                    }
                },
                new InjectQuestionSpec
                {
                    TempId = 3, Order = 3, QuestionType = "Essay", ScoreValue = 10,
                    QuestionText = "Soal Essay: jelaskan.", Rubrik = "Jawaban lengkap = 10."
                },
            },
            Workers = new List<InjectWorkerSpec>
            {
                new InjectWorkerSpec
                {
                    Nip = nip,
                    Answers = new List<InjectAnswerSpec>
                    {
                        new InjectAnswerSpec { QuestionTempId = 1, SelectedOptionTempIds = new List<int> { 1 } },
                        new InjectAnswerSpec { QuestionTempId = 2, SelectedOptionTempIds = new List<int> { 3, 4 } },
                        new InjectAnswerSpec { QuestionTempId = 3, TextAnswer = "Jawaban essay lengkap.", EssayScore = 10 },
                    }
                }
            }
        };
    }

    // ---- 5 STUB fact (nama WAJIB match filter VALIDATION.md FullyQualifiedName~). ----
    // Plan 03 mengganti body dengan assertion nyata SC1..SC5.

    // SC1 — byte-identik online (MC + MA + Essay) vs AssessmentScoreAggregator.Compute.
    [Fact]
    public async Task InjectAssessment_ByteIdentikOnline_MC_MA_Essay()
    {
        await Task.CompletedTask;
        Assert.True(true, "STUB — diisi Plan 03");
    }

    // SC2 — transaksi atomic: error di tengah → rollback total (D-04).
    [Fact]
    public async Task InjectAtomic_RollbackOnError()
    {
        await Task.CompletedTask;
        Assert.True(true, "STUB — diisi Plan 03");
    }

    // SC3 — essay finalize-block data-level → sesi Completed setelah finalize (D-05).
    [Fact]
    public async Task InjectEssayCompleted_AfterFinalize()
    {
        await Task.CompletedTask;
        Assert.True(true, "STUB — diisi Plan 03");
    }

    // SC4 — audit ActionType ManualInject terhitung benar per-session (D-11).
    [Fact]
    public async Task InjectAudit_ManualInjectCountPerSession()
    {
        await Task.CompletedTask;
        Assert.True(true, "STUB — diisi Plan 03");
    }

    // SC5 — kebijakan cert: backdate (D-12) / suppress (D-08) / manual range (D-09).
    [Fact]
    public async Task InjectCertPolicy_BackdateSuppressManualRange()
    {
        await Task.CompletedTask;
        Assert.True(true, "STUB — diisi Plan 03");
    }
}
