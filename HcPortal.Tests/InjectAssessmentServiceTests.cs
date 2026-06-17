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
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
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

    // ---- Helper: load questions(+options) + responses persisted untuk satu sesi (scope sessionId — caveat shared-DB) ----
    private static async Task<(List<PackageQuestion> questions, List<PackageUserResponse> responses)>
        LoadGradedAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pkg = await ctx.AssessmentPackages.FirstAsync(p => p.AssessmentSessionId == sessionId);
        var questions = await ctx.PackageQuestions.Include(q => q.Options)
            .Where(q => q.AssessmentPackageId == pkg.Id).ToListAsync();
        var responses = await ctx.PackageUserResponses
            .Where(r => r.AssessmentSessionId == sessionId).ToListAsync();
        return (questions, responses);
    }

    // SC1 — byte-identik online (MC + MA + Essay) vs AssessmentScoreAggregator.Compute + ET + cert D-12.
    [Fact]
    public async Task InjectAssessment_ByteIdentikOnline_MC_MA_Essay()
    {
        var nip = "INJ-SC1-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        var completedAt = new DateTime(2026, 5, 15);   // Mei → ROMAN "V", tahun 2026 (uji D-12 backdate)
        var req = new InjectRequest
        {
            Title = "Inject SC1 " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT",
            AssessmentType = "Standard",
            CompletedAt = completedAt,
            PassPercentage = 70,
            CertMode = InjectCertMode.Auto,
            Questions = new List<InjectQuestionSpec>
            {
                new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, ElemenTeknis = "ET-A", QuestionText = "MC",
                    Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } },
                new() { TempId = 2, Order = 2, QuestionType = "MultipleAnswer", ScoreValue = 10, ElemenTeknis = "ET-B", QuestionText = "MA",
                    Options = new() { new() { TempId = 3, OptionText = "b1", IsCorrect = true }, new() { TempId = 4, OptionText = "b2", IsCorrect = true }, new() { TempId = 5, OptionText = "s", IsCorrect = false } } },
                new() { TempId = 3, Order = 3, QuestionType = "Essay", ScoreValue = 10, ElemenTeknis = "ET-C", QuestionText = "Essay", Rubrik = "r" },
            },
            Workers = new List<InjectWorkerSpec>
            {
                new() { Nip = nip, Answers = new()
                {
                    new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } },        // MC benar (10)
                    new() { QuestionTempId = 2, SelectedOptionTempIds = new() { 3, 4 } },     // MA all-or-nothing benar (10)
                    new() { QuestionTempId = 3, TextAnswer = "jawaban", EssayScore = 8 },     // Essay 8/10
                } }
            }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);
        Assert.Single(result.SuccessSessionIds);
        var sessionId = result.SuccessSessionIds[0];

        // Read-after-commit (context BARU)
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.True(s!.IsManualEntry);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, s.Status);

        // Byte-identik: skor inject == AssessmentScoreAggregator.Compute (mesin yang SAMA dipakai online)
        var (questions, responses) = await LoadGradedAsync(verify, sessionId);
        var agg = AssessmentScoreAggregator.Compute(questions, responses, req.PassPercentage);
        Assert.Equal(agg.Percentage, s.Score);     // 28/30 = 93
        Assert.Equal(agg.IsPassed, s.IsPassed);     // true
        Assert.True(s.IsPassed);

        // ElemenTeknis: MC benar 1/1, MA all-or-nothing benar 1/1, Essay di-skip ET scoring → 0/1 (etTotal tetap hitung)
        var ets = await verify.SessionElemenTeknisScores.Where(e => e.AssessmentSessionId == sessionId).ToListAsync();
        Assert.Equal(3, ets.Count);
        var etA = ets.First(e => e.ElemenTeknis == "ET-A"); Assert.Equal(1, etA.CorrectCount); Assert.Equal(1, etA.QuestionCount);
        var etB = ets.First(e => e.ElemenTeknis == "ET-B"); Assert.Equal(1, etB.CorrectCount); Assert.Equal(1, etB.QuestionCount);
        var etC = ets.First(e => e.ElemenTeknis == "ET-C"); Assert.Equal(0, etC.CorrectCount); Assert.Equal(1, etC.QuestionCount);

        // Cert auto: D-12 ROMAN/tahun = tanggal backdate (Mei 2026 = "V/2026"), BUKAN bulan test dijalankan
        Assert.Matches(@"^KPB/\d{3}/V/2026$", s.NomorSertifikat);
    }

    // SC1 negatif — MA partial-select (1 dari 2 benar) → all-or-nothing 0; MC salah → 0. Skor == Compute.
    [Fact]
    public async Task InjectAssessment_PartialMA_WrongMC_ScoreMatchesCompute()
    {
        var nip = "INJ-SC1N-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        var req = new InjectRequest
        {
            Title = "Inject SC1neg " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT",
            AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15),
            PassPercentage = 70,
            CertMode = InjectCertMode.None,
            Questions = new List<InjectQuestionSpec>
            {
                new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, ElemenTeknis = "ET-A", QuestionText = "MC",
                    Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } },
                new() { TempId = 2, Order = 2, QuestionType = "MultipleAnswer", ScoreValue = 10, ElemenTeknis = "ET-B", QuestionText = "MA",
                    Options = new() { new() { TempId = 3, OptionText = "b1", IsCorrect = true }, new() { TempId = 4, OptionText = "b2", IsCorrect = true }, new() { TempId = 5, OptionText = "s", IsCorrect = false } } },
            },
            Workers = new List<InjectWorkerSpec>
            {
                new() { Nip = nip, Answers = new()
                {
                    new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 2 } },     // MC salah → 0
                    new() { QuestionTempId = 2, SelectedOptionTempIds = new() { 3 } },     // MA partial (1 dari 2) → 0
                } }
            }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);
        var sessionId = result.SuccessSessionIds[0];

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        var (questions, responses) = await LoadGradedAsync(verify, sessionId);
        var agg = AssessmentScoreAggregator.Compute(questions, responses, req.PassPercentage);
        Assert.Equal(agg.Percentage, s!.Score);    // 0/20 = 0
        Assert.Equal(0, s.Score);
        Assert.False(s.IsPassed);
        Assert.Null(s.NomorSertifikat);            // CertMode=None → tak ada cert
    }

    // Helper: 1 soal MC sederhana (opsi benar=TempId 1).
    private static List<InjectQuestionSpec> OneMcQuestion() => new()
    {
        new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, ElemenTeknis = "ET-A", QuestionText = "MC",
            Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } }
    };

    // SC2 — atomic: reject-all (D-03) + 0 parsial saat collision lintas batch (D-04/D-09).
    [Fact]
    public async Task InjectAtomic_RollbackOnError()
    {
        InjectResult result;

        // ---- SC2a: 1 NIP invalid → reject-all, 0 tulisan (D-03) ----
        var validNip = "INJ-SC2-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, validNip); }

        var titleA = "Inject SC2a " + Guid.NewGuid().ToString("N")[..6];
        var reqA = new InjectRequest
        {
            Title = titleA, Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = OneMcQuestion(),
            Workers = new()
            {
                new() { Nip = validNip, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } },
                new() { Nip = "NIP-TIDAK-ADA", Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } },
            }
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqA, "test-actor-id", "Test Actor");
        Assert.True(result.Rejected);
        Assert.False(result.Success);
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == titleA));   // 0 tulisan
            Assert.Equal(0, await verify.AuditLogs.CountAsync(a => a.ActionType == "ManualInject" && a.Description.Contains(titleA)));
        }

        // ---- SC2b: cert manual collision lintas batch → reject batch B, 0 parsial ----
        var nipB1 = "INJ-SC2b1-" + Guid.NewGuid().ToString("N")[..6];
        var nipB2 = "INJ-SC2b2-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nipB1); await SeedUserAsync(seed, nipB2); }

        const string sharedCert = "KPB/999/X/2099";
        var titleB1 = "Inject SC2b-A " + Guid.NewGuid().ToString("N")[..6];
        var reqB1 = new InjectRequest
        {
            Title = titleB1, Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2025, 10, 1), PassPercentage = 70, CertMode = InjectCertMode.Manual,
            Questions = OneMcQuestion(),
            Workers = new() { new() { Nip = nipB1, ManualCertNumber = sharedCert, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } } }
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqB1, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);   // batch A commit dgn cert manual sharedCert

        var titleB2 = "Inject SC2b-B " + Guid.NewGuid().ToString("N")[..6];
        var reqB2 = new InjectRequest
        {
            Title = titleB2, Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2025, 10, 2), PassPercentage = 70, CertMode = InjectCertMode.Manual,
            Questions = OneMcQuestion(),
            Workers = new() { new() { Nip = nipB2, ManualCertNumber = sharedCert, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } } }
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqB2, "test-actor-id", "Test Actor");
        Assert.True(result.Rejected);   // pre-flight D-09 menangkap collision DB
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == titleB2));   // 0 parsial
        }
    }

    // SC3 — essay finalize-block (D-05) → Status=Completed (BUKAN PendingGrading) + backdate preserve (Pitfall 1).
    [Fact]
    public async Task InjectEssayCompleted_AfterFinalize()
    {
        var nip = "INJ-SC3-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        var backdate = new DateTime(2026, 5, 15);
        var req = new InjectRequest
        {
            Title = "Inject SC3 " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT",
            AssessmentType = "Standard",
            CompletedAt = backdate,
            PassPercentage = 70,
            CertMode = InjectCertMode.None,
            Questions = new List<InjectQuestionSpec>
            {
                new() { TempId = 1, Order = 1, QuestionType = "Essay", ScoreValue = 10, ElemenTeknis = "ET-C", QuestionText = "Essay", Rubrik = "r" },
            },
            Workers = new List<InjectWorkerSpec>
            {
                new() { Nip = nip, Answers = new()
                {
                    new() { QuestionTempId = 1, TextAnswer = "jawaban essay", EssayScore = 8 },   // 8/10 = 80% ≥ 70 lulus
                } }
            }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);
        var sessionId = result.SuccessSessionIds[0];

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, s!.Status);   // ⚠ BUKAN PendingGrading (D-05, Pitfall 5)
        Assert.Equal(80, s.Score);
        Assert.True(s.IsPassed);

        // Backdate ter-preserve pasca finalize/grade (re-apply [G], Pitfall 1) — BUKAN ≈ today
        Assert.NotNull(s.CompletedAt);
        Assert.Equal(backdate.Date, s.CompletedAt!.Value.Date);
    }

    // SC4 — audit ActionType ManualInject terhitung benar per-session (D-11).
    [Fact]
    public async Task InjectAudit_ManualInjectCountPerSession()
    {
        await Task.CompletedTask;
        Assert.True(true, "STUB — diisi Plan 03");
    }

    // SC5 — kebijakan cert/validasi: backdate (D-12) / suppress (D-08) / manual collision (D-09) /
    //        EssayScore range (D-07) / tanggal masa depan (D-06).
    [Fact]
    public async Task InjectCertPolicy_BackdateSuppressManualRange()
    {
        InjectResult result;

        // (i) D-12 cert auto ROMAN/tahun = tanggal backdate (Maret 2024 → "III/2024")
        var nip1 = "INJ-SC5a-" + Guid.NewGuid().ToString("N")[..6];
        await using (var s1 = NewCtx()) { await SeedUserAsync(s1, nip1); }
        var reqPass = new InjectRequest
        {
            Title = "Inject SC5 D12 " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2024, 3, 10), PassPercentage = 70, CertMode = InjectCertMode.Auto,
            Questions = OneMcQuestion(),
            Workers = new() { new() { Nip = nip1, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } } }
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqPass, "a", "A");
        Assert.True(result.Success, result.Message);
        await using (var verify = NewCtx())
        {
            var s = await verify.AssessmentSessions.FindAsync(result.SuccessSessionIds[0]);
            Assert.Matches(@"^KPB/\d{3}/III/2024$", s!.NomorSertifikat);   // D-12: ROMAN III, tahun 2024 (backdate)
        }

        // (ii) D-08 suppress: !lulus (MC salah) + CertMode=Auto → NomorSertifikat null
        var nip2 = "INJ-SC5b-" + Guid.NewGuid().ToString("N")[..6];
        await using (var s2 = NewCtx()) { await SeedUserAsync(s2, nip2); }
        var reqFail = new InjectRequest
        {
            Title = "Inject SC5 supp " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2024, 3, 10), PassPercentage = 70, CertMode = InjectCertMode.Auto,
            Questions = OneMcQuestion(),
            Workers = new() { new() { Nip = nip2, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 2 } } } } }   // salah
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqFail, "a", "A");
        Assert.True(result.Success, result.Message);
        await using (var verify = NewCtx())
        {
            var s = await verify.AssessmentSessions.FindAsync(result.SuccessSessionIds[0]);
            Assert.False(s!.IsPassed);
            Assert.Null(s.NomorSertifikat);   // D-08: cert di-suppress walau toggle Auto
        }

        // (iii) D-09 cert manual collision intra-batch → reject + 0 tulisan
        var nip3a = "INJ-SC5c1-" + Guid.NewGuid().ToString("N")[..6];
        var nip3b = "INJ-SC5c2-" + Guid.NewGuid().ToString("N")[..6];
        await using (var s3 = NewCtx()) { await SeedUserAsync(s3, nip3a); await SeedUserAsync(s3, nip3b); }
        var titleC = "Inject SC5 dup " + Guid.NewGuid().ToString("N")[..6];
        var reqDup = new InjectRequest
        {
            Title = titleC, Category = "IHT", AssessmentType = "Standard", CompletedAt = new DateTime(2025, 1, 5),
            PassPercentage = 70, CertMode = InjectCertMode.Manual,
            Questions = OneMcQuestion(),
            Workers = new()
            {
                new() { Nip = nip3a, ManualCertNumber = "KPB/777/I/2025", Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } },
                new() { Nip = nip3b, ManualCertNumber = "KPB/777/I/2025", Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } },
            }
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqDup, "a", "A");
        Assert.True(result.Rejected);
        Assert.Contains(result.PerRowErrors, e => e.Message.Contains("sertifikat"));
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == titleC));
        }

        // (iv) D-07 EssayScore > ScoreValue → reject + 0 tulisan
        var nip4 = "INJ-SC5d-" + Guid.NewGuid().ToString("N")[..6];
        await using (var s4 = NewCtx()) { await SeedUserAsync(s4, nip4); }
        var titleD = "Inject SC5 range " + Guid.NewGuid().ToString("N")[..6];
        var reqRange = new InjectRequest
        {
            Title = titleD, Category = "IHT", AssessmentType = "Standard", CompletedAt = new DateTime(2025, 1, 5),
            PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = new() { new() { TempId = 1, Order = 1, QuestionType = "Essay", ScoreValue = 10, ElemenTeknis = "ET-C", QuestionText = "Essay" } },
            Workers = new() { new() { Nip = nip4, Answers = new() { new() { QuestionTempId = 1, TextAnswer = "x", EssayScore = 15 } } } }   // > ScoreValue
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqRange, "a", "A");
        Assert.True(result.Rejected);
        Assert.Contains(result.PerRowErrors, e => e.Message.Contains("rentang") || e.Message.Contains("essay"));
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == titleD));
        }

        // (v) D-06 tanggal masa depan → reject + 0 tulisan
        var nip5 = "INJ-SC5e-" + Guid.NewGuid().ToString("N")[..6];
        await using (var s5 = NewCtx()) { await SeedUserAsync(s5, nip5); }
        var titleE = "Inject SC5 future " + Guid.NewGuid().ToString("N")[..6];
        var reqFuture = new InjectRequest
        {
            Title = titleE, Category = "IHT", AssessmentType = "Standard", CompletedAt = DateTime.Today.AddDays(5),
            PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = OneMcQuestion(),
            Workers = new() { new() { Nip = nip5, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } } } }
        };
        result = await NewInjectService(NewCtx()).InjectBatchAsync(reqFuture, "a", "A");
        Assert.True(result.Rejected);
        Assert.Contains(result.PerRowErrors, e => e.Message.Contains("masa depan") || e.Message.Contains("Tanggal"));
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == titleE));
        }
    }
}
