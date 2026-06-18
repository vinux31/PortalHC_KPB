// Phase 396 INJ-10 (Wave 3) — Integration real-SQL: jalur Excel == jalur Form (preview == commit, D-08),
// teks essay OPSIONAL hanya bila EssayTextRequired=false (D-05), dan rollback atomik saat ada error (D-09).
//
// Membuktikan jaminan kunci 396: worker yang di-parse dari sel Excel (InjectExcelHelper.ParseMatrix) menghasilkan
// set jawaban yang STRUKTURNYA SAMA dengan jalur form (TempId terpilih + skor essay identik), lalu di-commit lewat
// InjectBatchAsync (engine GradingService + AssessmentScoreAggregator yang SAMA) → Score di DB == skor yang
// AssessmentScoreAggregator.Compute prediksi untuk pola yang sama (preview == commit, NOL cabang grading baru).
//
// Fixture pakai disposable DB HcPortalDB_Test_{guid} (InjectAssessmentFixture, di-reuse dari InjectAssessmentServiceTests).
// DB lokal HcPortalDB_Dev TAK tersentuh. [Trait Category=Integration] → skip via "Category!=Integration".
// Bila SQLEXPRESS tak tersedia, fixture melempar XunitException MIGRATION-CHAIN (bukan bug 396).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using HcPortal.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class InjectExcelImportTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectExcelImportTests(InjectAssessmentFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Real GradingService (delegasi grading) — SALIN VERBATIM dari InjectPreviewEqualsCommitTests.
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

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string nip)
    {
        var u = new ApplicationUser
        {
            UserName = "injx-" + Guid.NewGuid().ToString("N")[..8],
            Email = $"injx-{Guid.NewGuid().ToString("N")[..8]}@test.local",
            FullName = "Inject Excel Test " + nip,
            NIP = nip
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    // ---- in-memory builders (no DB) — mirror InjectExcelHelperTests / BuildAutoGenAnswersTests ----

    private static InjectQuestionSpec Q(int tempId, string type, int sv, int order,
        params (int optTemp, bool correct)[] opts) =>
        new InjectQuestionSpec
        {
            TempId = tempId,
            QuestionType = type,
            ScoreValue = sv,
            Order = order,
            ElemenTeknis = "ET-" + tempId,
            QuestionText = type + tempId,
            Options = opts.Select(o => new InjectOptionSpec { TempId = o.optTemp, OptionText = "opt" + o.optTemp, IsCorrect = o.correct }).ToList()
        };

    /// <summary>MC 4 opsi A-D: A(tempId*10+1)=benar, B/C/D salah.</summary>
    private static InjectQuestionSpec Mc(int tempId, int sv = 10, int? order = null) =>
        Q(tempId, "MultipleChoice", sv, order ?? tempId,
            (tempId * 10 + 1, true), (tempId * 10 + 2, false), (tempId * 10 + 3, false), (tempId * 10 + 4, false));

    /// <summary>MA 4 opsi A-D: A &amp; C benar, B &amp; D salah.</summary>
    private static InjectQuestionSpec Ma(int tempId, int sv = 10, int? order = null) =>
        Q(tempId, "MultipleAnswer", sv, order ?? tempId,
            (tempId * 10 + 1, true), (tempId * 10 + 2, false), (tempId * 10 + 3, true), (tempId * 10 + 4, false));

    private static InjectQuestionSpec Essay(int tempId, int sv = 10, int? order = null) =>
        new InjectQuestionSpec { TempId = tempId, QuestionType = "Essay", ScoreValue = sv, Order = order ?? tempId, ElemenTeknis = "ET-" + tempId, QuestionText = "Essay" + tempId, Rubrik = "r" };

    // ---- ClosedXML helpers (mirror InjectExcelHelperTests) ----

    private static List<InjectQuestionSpec> Ordered(IEnumerable<InjectQuestionSpec> questions) =>
        questions.OrderBy(q => q.Order).ThenBy(q => q.TempId).ToList();

    /// <summary>Kolom (1-based) soal tertentu di sheet "Jawaban". 1=NIP,2=Nama,3+=soal. Essay = kolom Skor.</summary>
    private static int QuestionColumn(IReadOnlyList<InjectQuestionSpec> questions, int questionTempId)
    {
        var ord = Ordered(questions);
        int col = 3;
        foreach (var q in ord)
        {
            if (q.TempId == questionTempId) return col;
            // Essay menempati 2 kolom (skor + teks), tipe lain 1 kolom.
            col += (q.QuestionType ?? "MultipleChoice") == "Essay" ? 2 : 1;
        }
        return -1;
    }

    private static void SetCell(XLWorkbook wb, int row, int col, string value)
        => wb.Worksheet("Jawaban").Cell(row, col).Value = value;

    private static MemoryStream ToStream(XLWorkbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static IReadOnlySet<string> Nips(params string[] nips) => new HashSet<string>(nips);
    private static IReadOnlyDictionary<string, string> NipMap(params (string nip, string userId)[] map) =>
        map.ToDictionary(m => m.nip, m => m.userId);

    // Mirror controller MapToInMemory + Aggregator → prediksi preview (engine SAMA dengan commit).
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

    // VM → spec (mirror controller ToAnswerSpec) untuk membandingkan jalur Excel vs Form + commit.
    private static InjectAnswerSpec ToAnswerSpec(InjectAssessmentViewModel.InjectAnswerVM a) => new InjectAnswerSpec
    {
        QuestionTempId = a.QuestionTempId,
        SelectedOptionTempIds = a.SelectedOptionTempIds ?? new(),
        TextAnswer = a.TextAnswer,
        EssayScore = a.EssayScore
    };

    // =========================================================================
    // 1) Jalur Excel == jalur Form (preview == commit) — INTI 396
    // =========================================================================
    [Fact]
    public async Task ExcelPath_ProducesSameScore_AsFormPath()
    {
        var nip = "EX1-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }
        string uid;
        await using (var ctx = NewCtx())
            uid = (await ctx.Users.FirstAsync(u => u.NIP == nip)).Id;

        // Paket MC + MA + Essay (max 30). MC benar (10), MA benar penuh A&C (10), Essay 10/10 → 30/30 = 100%.
        var questions = new List<InjectQuestionSpec> { Mc(1, 10, 1), Ma(2, 10, 2), Essay(3, 10, 3) };

        // (a) jalur FORM — bangun InjectAnswerVM langsung (yang biasanya dari #AnswersJson form).
        var formAnswers = new List<InjectAssessmentViewModel.InjectAnswerVM>
        {
            new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 * 10 + 1 } },               // MC: A benar
            new() { QuestionTempId = 2, SelectedOptionTempIds = new() { 2 * 10 + 1, 2 * 10 + 3 } },   // MA: A,C benar
            new() { QuestionTempId = 3, EssayScore = 10 },                                            // Essay penuh, tanpa teks
        };

        // (b) jalur EXCEL — generate template, tulis sel EKUIVALEN, ParseMatrix → worker dari Excel.
        var wb = InjectExcelHelper.GenerateTemplate(questions, new[] { (nip, "Budi") });
        SetCell(wb, 2, QuestionColumn(questions, 1), "A");      // MC → A
        SetCell(wb, 2, QuestionColumn(questions, 2), "A,C");    // MA → A & C
        SetCell(wb, 2, QuestionColumn(questions, 3), "10");     // Essay skor (kolom teks dibiarkan kosong, D-05)

        List<InjectAssessmentViewModel.InjectWorkerAnswersVM> excelWorkers;
        List<InjectRowError> parseErrors;
        using (var stream = ToStream(wb))
            (excelWorkers, parseErrors, _) = InjectExcelHelper.ParseMatrix(stream, questions, Nips(nip), NipMap((nip, uid)));

        Assert.Empty(parseErrors);
        var excelWorker = Assert.Single(excelWorkers);

        // Struktural: set jawaban Excel == set jawaban form (TempId terpilih + skor essay identik).
        var excelAnswers = excelWorker.Answers.OrderBy(a => a.QuestionTempId).ToList();
        var formSorted = formAnswers.OrderBy(a => a.QuestionTempId).ToList();
        Assert.Equal(formSorted.Count, excelAnswers.Count);
        for (int i = 0; i < formSorted.Count; i++)
        {
            Assert.Equal(formSorted[i].QuestionTempId, excelAnswers[i].QuestionTempId);
            Assert.Equal(formSorted[i].SelectedOptionTempIds.OrderBy(x => x), excelAnswers[i].SelectedOptionTempIds.OrderBy(x => x));
            Assert.Equal(formSorted[i].EssayScore, excelAnswers[i].EssayScore);
        }

        // Preview (engine Aggregator atas pola Excel) — prediksi skor commit.
        var excelSpecs = excelWorker.Answers.Select(ToAnswerSpec).ToList();
        int previewPct = PreviewPercentage(questions, excelSpecs, 70);
        Assert.Equal(100, previewPct);   // 30/30

        // Commit jalur Excel (EssayTextRequired=false, D-05 — essay tanpa teks tetap valid).
        var req = new InjectRequest
        {
            Title = "EXCEL FORM PARITY " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            EssayTextRequired = false,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = excelSpecs } }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Success, result.Message);
        var sessionId = result.SuccessSessionIds[0];

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Equal(previewPct, s!.Score);   // preview == commit (jalur Excel grade IDENTIK jalur form)
        Assert.True(s.IsPassed);
    }

    // =========================================================================
    // 2) Essay skor tanpa teks: ditolak bila EssayTextRequired=true (Form),
    //    diterima bila EssayTextRequired=false (Excel, D-05) — kunci scoping kedua arah.
    // =========================================================================
    [Fact]
    public async Task ExcelPath_EssayScoreNoText_NotRejected()
    {
        var nip = "EX2-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nip); }

        var questions = new List<InjectQuestionSpec> { Essay(1, 10, 1) };
        var answers = new List<InjectAnswerSpec> { new() { QuestionTempId = 1, EssayScore = 8 } };   // skor, TANPA teks

        // (a) Form (EssayTextRequired=true) → reject (teks essay wajib).
        var reqForm = new InjectRequest
        {
            Title = "EXCEL ESSAY FORM " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            EssayTextRequired = true,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = answers } }
        };
        var resForm = await NewInjectService(NewCtx()).InjectBatchAsync(reqForm, "test-actor-id", "Test Actor");
        Assert.True(resForm.Rejected, "Form (EssayTextRequired=true): essay skor tanpa teks harus reject (D-04)");
        await using (var v = NewCtx())
            Assert.Equal(0, await v.AssessmentSessions.CountAsync(x => x.Title == reqForm.Title));

        // (b) Excel (EssayTextRequired=false) → tidak reject, sesi ter-commit (D-05).
        var reqExcel = new InjectRequest
        {
            Title = "EXCEL ESSAY OK " + Guid.NewGuid().ToString("N")[..6],
            Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            EssayTextRequired = false,
            Questions = questions,
            Workers = new() { new() { Nip = nip, Answers = answers } }
        };
        var resExcel = await NewInjectService(NewCtx()).InjectBatchAsync(reqExcel, "test-actor-id", "Test Actor");
        Assert.False(resExcel.Rejected, "Excel (EssayTextRequired=false): essay skor tanpa teks TIDAK boleh reject (D-05)");
        Assert.True(resExcel.Success, resExcel.Message);

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(resExcel.SuccessSessionIds[0]);
        Assert.NotNull(s);
        Assert.Equal(80, s!.Score);   // 8/10 = 80%
    }

    // =========================================================================
    // 3) Error apa pun → rollback atomik: 0 sesi ter-commit (D-09)
    // =========================================================================
    [Fact]
    public async Task ExcelPath_AnyError_AtomicRollback()
    {
        var nipOk = "EX3A-" + Guid.NewGuid().ToString("N")[..6];
        var nipBad = "EX3B-" + Guid.NewGuid().ToString("N")[..6];
        await using (var seed = NewCtx()) { await SeedUserAsync(seed, nipOk); await SeedUserAsync(seed, nipBad); }

        var questions = new List<InjectQuestionSpec> { Mc(1, 10, 1) };
        var title = "EXCEL ATOMIC " + Guid.NewGuid().ToString("N")[..6];

        // Worker valid + worker invalid (MC dengan 2 opsi terpilih → PreflightValidate "wajib tepat 1 jawaban").
        var req = new InjectRequest
        {
            Title = title, Category = "IHT", AssessmentType = "Standard",
            CompletedAt = new DateTime(2026, 5, 15), PassPercentage = 70, CertMode = InjectCertMode.None,
            EssayTextRequired = false,
            Questions = questions,
            Workers = new()
            {
                new() { Nip = nipOk,  Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 * 10 + 1 } } } },
                new() { Nip = nipBad, Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 * 10 + 1, 1 * 10 + 2 } } } },  // 2 opsi MC = invalid
            }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "test-actor-id", "Test Actor");
        Assert.True(result.Rejected, "≥1 error → batch harus reject-all (D-09)");
        Assert.False(result.Success);

        // Re-query via context baru → konfirmasi NOL sesi ter-commit (tak ada partial write).
        await using var verify = NewCtx();
        Assert.Equal(0, await verify.AssessmentSessions.CountAsync(x => x.Title == title));
    }
}
