// Phase 395 INJ-08/INJ-09 — Integration real-SQL: preview == commit (D-09), skip=omit grade 0 (D-05),
// TextAnswer-wajib reject (D-04). Mengunci jaminan "what-you-see-is-what-commits":
//   - PreviewInjectScore controller memetakan pola usulan → in-memory PackageQuestion/Response (TempId=Id
//     sintetis) → AssessmentScoreAggregator.Compute. Commit (InjectBatchAsync) memakai engine yang SAMA.
//     Test ini me-MIRROR map-pola helper controller (MapToInMemory) lalu membandingkan dengan Score di DB.
//   - Auto-gen: BuildAutoGenAnswers(seed deterministik) dipakai untuk KEDUA preview & commit → pola identik.
//
// Fixture pakai disposable DB HcPortalDB_Test_{guid} (InjectAssessmentFixture, di-reuse dari
// InjectAssessmentServiceTests). DB lokal HcPortalDB_Dev TAK tersentuh. [Trait Category=Integration] →
// skip via "Category!=Integration". Bila SQLEXPRESS tak tersedia, fixture melempar XunitException
// MIGRATION-CHAIN (bukan bug inject) — jalankan dari dev box dengan SQLEXPRESS.
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

[Trait("Category", "Integration")]
public class InjectPreviewEqualsCommitTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectPreviewEqualsCommitTests(InjectAssessmentFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Real GradingService (delegasi grading) — SALIN VERBATIM dari InjectAssessmentServiceTests:69-76.
    private GradingService NewGradingService(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    private InjectAssessmentService NewInjectService(ApplicationDbContext ctx)
        => new InjectAssessmentService(ctx, NewGradingService(ctx), NullLogger<InjectAssessmentService>.Instance);

    // Seed worker dengan NIP (inject resolve by NIP — WAJIB set NIP). UserName/Email unik via Guid.
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string nip)
    {
        var u = new ApplicationUser
        {
            UserName = "inj-" + Guid.NewGuid().ToString("N")[..8],
            Email = $"inj-{Guid.NewGuid().ToString("N")[..8]}@test.local",
            FullName = "Inject Preview Test " + nip,
            NIP = nip
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    // Load questions(+options) + responses persisted untuk satu sesi (read-after-commit).
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

    // MIRROR helper controller MapToInMemory (InjectAssessmentController) — pola usulan → in-memory POCO untuk
    // preview (TempId = Id sintetis; Aggregator match by Id, EF-free). Engine preview == engine commit.
    private static int PreviewPercentage(
        IReadOnlyList<InjectQuestionSpec> questions,
        IReadOnlyList<InjectAnswerSpec> answers,
        int passPercentage)
    {
        var qInMem = questions.Select(q => new PackageQuestion
        {
            Id = q.TempId,
            QuestionType = q.QuestionType ?? "MultipleChoice",
            ScoreValue = q.ScoreValue,
            Options = (q.Options ?? new()).Select(o => new PackageOption { Id = o.TempId, IsCorrect = o.IsCorrect }).ToList()
        }).ToList();

        var respInMem = new List<PackageUserResponse>();
        foreach (var a in answers)
        {
            var q = questions.FirstOrDefault(x => x.TempId == a.QuestionTempId);
            if (q == null) continue;
            if ((q.QuestionType ?? "MultipleChoice") == "Essay")
                respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, EssayScore = a.EssayScore, TextAnswer = a.TextAnswer });
            else
                foreach (var optTemp in (a.SelectedOptionTempIds ?? new()))
                    respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, PackageOptionId = optTemp });
        }
        return AssessmentScoreAggregator.Compute(qInMem, respInMem, passPercentage).Percentage;
    }

    // Paket MC + MA + Essay (ScoreValue 10 masing-masing; maxScore 30).
    private static List<InjectQuestionSpec> McMaEssayPackage() => new()
    {
        new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, ElemenTeknis = "ET-A", QuestionText = "MC",
            Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } },
        new() { TempId = 2, Order = 2, QuestionType = "MultipleAnswer", ScoreValue = 10, ElemenTeknis = "ET-B", QuestionText = "MA",
            Options = new() { new() { TempId = 3, OptionText = "b1", IsCorrect = true }, new() { TempId = 4, OptionText = "b2", IsCorrect = true }, new() { TempId = 5, OptionText = "s", IsCorrect = false } } },
        new() { TempId = 3, Order = 3, QuestionType = "Essay", ScoreValue = 10, ElemenTeknis = "ET-C", QuestionText = "Essay", Rubrik = "r" },
    };

    // ── INJ-08/09: preview == commit (input-asli, jawaban campur) ──
    [Fact]
    public async Task PreviewEqualsCommit_InputAsli_MixedAnswers()
    {
        var nip = "PVC-MAN-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        var questions = McMaEssayPackage();
        // MC benar (10), MA partial → 0 (all-or-nothing), Essay 7/10 → total 17/30 = 56%.
        var answers = new List<InjectAnswerSpec>
        {
            new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } },
            new() { QuestionTempId = 2, SelectedOptionTempIds = new() { 3 } },          // partial → 0
            new() { QuestionTempId = 3, TextAnswer = "jawaban essay", EssayScore = 7 },
        };

        var req = new InjectRequest
        {
            Title = "PVC MAN " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = answers } }
        };

        // Preview (pra-persist, engine Aggregator atas pola yang SAMA).
        int previewPct = PreviewPercentage(questions, answers, req.PassPercentage);
        Assert.Equal(56, previewPct);   // 17/30 = 56 (truncation)

        // Commit (read-after-commit Score dari DB).
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);
        var sessionId = result.SuccessSessionIds[0];

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Equal(previewPct, s!.Score);   // preview == commit (jaminan D-09)

        // Sanity tambahan: skor commit juga == Compute atas pola ter-persist (engine identik).
        var (qDb, rDb) = await LoadGradedAsync(verify, sessionId);
        Assert.Equal(AssessmentScoreAggregator.Compute(qDb, rDb, req.PassPercentage).Percentage, s.Score);
    }

    // ── INJ-09: preview == commit (auto-gen), pola identik via seed deterministik, skor >= target ──
    [Fact]
    public async Task PreviewEqualsCommit_AutoGen_HitsTargetAndMatches()
    {
        var nip = "PVC-AUTO-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        // Paket MC+MA saja (tanpa essay → ceiling 100; target 70 reachable). 4 soal MC ScoreValue 10 (max 40).
        var questions = new List<InjectQuestionSpec>
        {
            new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC1",
                Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } },
            new() { TempId = 2, Order = 2, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC2",
                Options = new() { new() { TempId = 3, OptionText = "benar", IsCorrect = true }, new() { TempId = 4, OptionText = "salah", IsCorrect = false } } },
            new() { TempId = 3, Order = 3, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC3",
                Options = new() { new() { TempId = 5, OptionText = "benar", IsCorrect = true }, new() { TempId = 6, OptionText = "salah", IsCorrect = false } } },
            new() { TempId = 4, Order = 4, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC4",
                Options = new() { new() { TempId = 7, OptionText = "benar", IsCorrect = true }, new() { TempId = 8, OptionText = "salah", IsCorrect = false } } },
        };

        const int target = 70;
        var completedAt = new DateTime(2026, 5, 15);
        var title = "PVC AUTO " + Guid.NewGuid().ToString("N")[..6];

        // Seed deterministik dipakai untuk KEDUA preview & commit → pola IDENTIK (server-otoritas A1).
        var sd = InjectAssessmentService.ComputeAutoGenSeed(nip, title, "IHT", completedAt, target);
        var ag = InjectAssessmentService.BuildAutoGenAnswers(questions, target, sd);
        Assert.True(ag.TargetReachable);

        int previewPct = PreviewPercentage(questions, ag.Answers, target);
        Assert.True(previewPct >= target, $"preview {previewPct} < target {target} (bias jamin-lulus dilanggar)");

        var req = new InjectRequest
        {
            Title = title, Category = "IHT", AssessmentType = "Standard",
            CompletedAt = completedAt, PassPercentage = target, CertMode = InjectCertMode.None,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = ag.Answers } }   // pola hasil preview = pola commit
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);
        var sessionId = result.SuccessSessionIds[0];

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Equal(previewPct, s!.Score);          // preview == commit
        Assert.True(s.Score >= target);              // bias jamin-lulus (D-06)
        Assert.True(s.IsPassed);
    }

    // ── INJ-08/D-05: skip = OMIT spec → soal tak terjawab grade 0 (BUKAN reject-all) ──
    [Fact]
    public async Task SkipOmit_UnansweredGradedZero_NotRejectAll()
    {
        var nip = "PVC-SKIP-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        // 4 soal MC (max 40). Worker hanya jawab 2 (TempId 1 & 2), 2 lainnya DI-OMIT (bukan spec kosong).
        var questions = new List<InjectQuestionSpec>
        {
            new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC1",
                Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } },
            new() { TempId = 2, Order = 2, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC2",
                Options = new() { new() { TempId = 3, OptionText = "benar", IsCorrect = true }, new() { TempId = 4, OptionText = "salah", IsCorrect = false } } },
            new() { TempId = 3, Order = 3, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC3",
                Options = new() { new() { TempId = 5, OptionText = "benar", IsCorrect = true }, new() { TempId = 6, OptionText = "salah", IsCorrect = false } } },
            new() { TempId = 4, Order = 4, QuestionType = "MultipleChoice", ScoreValue = 10, QuestionText = "MC4",
                Options = new() { new() { TempId = 7, OptionText = "benar", IsCorrect = true }, new() { TempId = 8, OptionText = "salah", IsCorrect = false } } },
        };
        var answers = new List<InjectAnswerSpec>
        {
            new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } },   // benar
            new() { QuestionTempId = 2, SelectedOptionTempIds = new() { 3 } },   // benar
            // TempId 3 & 4 DI-OMIT (skip) → grade 0
        };

        int previewPct = PreviewPercentage(questions, answers, 70);
        Assert.Equal(50, previewPct);   // 2*10 / 40 * 100 = 50

        var req = new InjectRequest
        {
            Title = "PVC SKIP " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = answers } }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.False(result.Rejected, "batch TIDAK boleh reject-all saat skip=omit (D-05)");
        Assert.True(result.Success, result.Message);
        var sessionId = result.SuccessSessionIds[0];

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Equal(50, s!.Score);            // soal di-omit grade 0
        Assert.Equal(previewPct, s.Score);     // preview == commit
    }

    // ── INJ-08/D-04: TextAnswer-wajib (essay engaged) → reject; auto-gen essay omit → TIDAK reject ──
    [Fact]
    public async Task TextAnswerRequired_EssayScoreWithoutText_Rejects()
    {
        var nip = "PVC-TXT-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        var questions = new List<InjectQuestionSpec>
        {
            new() { TempId = 1, Order = 1, QuestionType = "Essay", ScoreValue = 10, QuestionText = "Essay", Rubrik = "r" },
        };

        // Essay EssayScore=5 tetapi TextAnswer kosong → pre-flight D-04 reject (engaged).
        var reqReject = new InjectRequest
        {
            Title = "PVC TXT REJECT " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = new()
            {
                new() { QuestionTempId = 1, EssayScore = 5, TextAnswer = "   " },   // whitespace = kosong
            } } }
        };
        var resReject = await NewInjectService(NewCtx()).InjectBatchAsync(reqReject, "test-actor-id", "Test Actor");
        Assert.True(resReject.Rejected, "essay EssayScore tanpa TextAnswer harus reject (D-04)");
        Assert.False(resReject.Success);
        await using (var verify = NewCtx())
            Assert.Equal(0, await verify.AssessmentSessions.CountAsync(x => x.Title == reqReject.Title));   // 0 tulisan (reject-all)

        // Sebaliknya: essay auto-gen DI-OMIT (tak ada spec essay) → TIDAK reject (D-08/D-05 konsisten).
        var reqOk = new InjectRequest
        {
            Title = "PVC TXT OK " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = new() } }   // essay di-omit → grade 0, tak terblokir
        };
        var resOk = await NewInjectService(NewCtx()).InjectBatchAsync(reqOk, "test-actor-id", "Test Actor");
        Assert.False(resOk.Rejected, "essay di-omit TIDAK boleh terblokir rule TextAnswer (D-05/D-08)");
        Assert.True(resOk.Success, resOk.Message);
    }
}
