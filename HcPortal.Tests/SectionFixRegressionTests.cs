// Phase 415-04 (Nyquist gap-fill) — REGRESSION LOCKS untuk 8 bug-fix code-review (H1/H2/H3/H4 + M1/M2/M3/L3).
// Tujuan: mengunci perilaku PASCA-fix sehingga tidak bisa regres diam-diam. Tiap [Fact] di sini LULUS pada
// kode saat ini dan secara konsep GAGAL pada kode pra-fix.
//
// De-tautology: tes menggerakkan action ASLI (ImportPackageQuestions / CreateSection / EditSection /
// CopyPackagesFromPre) atas SQL Server NYATA (SectionFixture, di-reuse) lewat .xlsx ClosedXML NYATA.
// TIDAK ada replika logika parser/compare/sibling/clone di test. Assert state DB nyata + TempData.
//
// Harness (controller construction, stub UserManager/UserStore/WebHostEnv/TempData, SeedActor,
// SeedSessionPackage, xlsx builders) disalin VERBATIM dari SectionImportTests agar kompilasi & konvensi
// sama persis; ditambah helper Pre/Post SamePackage + PackageUserResponse untuk H1/H4.
//
// [Trait Category=Integration] — butuh FK/unique-index/Section-persist → SQLEXPRESS. HcPortalDB_Dev TAK tersentuh.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class SectionFixRegressionTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public SectionFixRegressionTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ============================ Controller harness (verbatim dari SectionImportTests) ============================
    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        private readonly ApplicationUser _actor;
        public StubUserManager(ApplicationUser actor)
            : base(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
            => _actor = actor;
        public override Task<ApplicationUser?> GetUserAsync(System.Security.Claims.ClaimsPrincipal principal)
            => Task.FromResult<ApplicationUser?>(_actor);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private sealed class StubWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "HcPortal.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
    }

    private sealed class StubUserStore : IUserStore<ApplicationUser>
    {
        public Task<string> GetUserIdAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult<string?>(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, System.Threading.CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult<string?>(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, System.Threading.CancellationToken ct) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, System.Threading.CancellationToken ct) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, System.Threading.CancellationToken ct) => Task.FromResult<ApplicationUser?>(null);
        public void Dispose() { }
    }

    private AssessmentAdminController MakeController(ApplicationDbContext ctx, ApplicationUser actor, string actionName)
    {
        var auditLog = new AuditLogService(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        #pragma warning disable CS8625
        var ctrl = new AssessmentAdminController(
            ctx,
            userManager:             new StubUserManager(actor),
            auditLog:                auditLog,
            env:                     new StubWebHostEnvironment(),
            cache:                   cache,
            logger:                  NullLogger<AssessmentAdminController>.Instance,
            notificationService:     null!,
            hubContext:              new NoopHubContext(),
            workerDataService:       null!,
            gradingService:          null!,
            protonCompletionService: null!,
            protonBypassService:     null!);
        #pragma warning restore CS8625
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            ActionDescriptor = new ControllerActionDescriptor { ActionName = actionName }
        };
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        return ctrl;
    }

    private static async Task<ApplicationUser> SeedActorAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser
        {
            UserName = "actor-" + Guid.NewGuid().ToString("N")[..8],
            Email = "actor@test.local", FullName = "HC Actor", NIP = "99999", IsActive = true
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    // Seed: 1 session + 1 package. assessmentType default "Standard". Return (sessionId, packageId, title, category, schedDate).
    private static async Task<(int sessionId, int packageId, string title, string category, DateTime sched)> SeedSessionPackageAsync(
        ApplicationDbContext ctx, string packageName = "Paket A", string? title = null, string? category = null,
        DateTime? sched = null, string assessmentType = "Standard", int? linkedSessionId = null, bool samePackage = false)
    {
        var user = new ApplicationUser
        {
            UserName = "imp-" + Guid.NewGuid().ToString("N")[..8],
            Email = "imp@test.local", FullName = "Import Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var t = title ?? ("Imp-" + Guid.NewGuid().ToString("N")[..8]);
        var cat = category ?? "OJT";
        var sc = sched ?? DateTime.UtcNow.Date;
        var session = new AssessmentSession
        {
            UserId = user.Id, Title = t, Category = cat, Status = "Open", AccessToken = "",
            Schedule = sc, DurationMinutes = 60, PassPercentage = 70, Progress = 0,
            AssessmentType = assessmentType, LinkedSessionId = linkedSessionId, SamePackage = samePackage
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = packageName, PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return (session.Id, pkg.Id, t, cat, sc);
    }

    // Seed N soal MC (1 benar, opsi A/B) ber-Section di paket. Buat Section bila belum ada. Return sectionId (null=Lainnya).
    private static async Task<int?> SeedQuestionsInSectionAsync(
        ApplicationDbContext ctx, int packageId, int? sectionNumber, int count, string tag)
    {
        int? sectionId = null;
        if (sectionNumber.HasValue)
        {
            var existing = await ctx.AssessmentPackageSections
                .FirstOrDefaultAsync(s => s.AssessmentPackageId == packageId && s.SectionNumber == sectionNumber.Value);
            if (existing == null)
            {
                existing = new AssessmentPackageSection { AssessmentPackageId = packageId, SectionNumber = sectionNumber.Value, Name = $"Sec{sectionNumber}" };
                ctx.AssessmentPackageSections.Add(existing);
                await ctx.SaveChangesAsync();
            }
            sectionId = existing.Id;
        }
        int baseOrder = await ctx.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId);
        for (int i = 0; i < count; i++)
        {
            ctx.PackageQuestions.Add(new PackageQuestion
            {
                AssessmentPackageId = packageId, QuestionText = $"{tag}-Q{i}", Order = baseOrder + i + 1,
                ScoreValue = 10, QuestionType = "MultipleChoice", SectionId = sectionId,
                Options = new List<PackageOption> { new() { OptionText = "A", IsCorrect = true }, new() { OptionText = "B", IsCorrect = false } }
            });
        }
        await ctx.SaveChangesAsync();
        return sectionId;
    }

    // ---- .xlsx fixture builders (verbatim dari SectionImportTests) ----
    private static IFormFile BuildNewFormatFile(IEnumerable<string[]> dataRows)
    {
        var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Opsi E", "Opsi F", "Jawaban Benar", "No. Section", "Nama Section", "Elemen Teknis", "QuestionType", "Rubrik" };
        return BuildFile(headers, dataRows);
    }

    private static IFormFile BuildLegacyFormatFile(IEnumerable<string[]> dataRows)
    {
        var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Jawaban Benar", "Elemen Teknis", "QuestionType", "Rubrik" };
        return BuildFile(headers, dataRows);
    }

    private static IFormFile BuildFile(string[] headers, IEnumerable<string[]> dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Question Import");
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        int r = 2;
        foreach (var row in dataRows)
        {
            for (int c = 0; c < row.Length; c++)
                if (!string.IsNullOrEmpty(row[c])) ws.Cell(r, c + 1).Value = row[c];
            r++;
        }
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return new FormFile(ms, 0, ms.Length, "excelFile", "import.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private static List<string>? DeserializeMismatch(TempDataDictionary td)
    {
        var json = td["SectionMismatch"] as string;
        return string.IsNullOrWhiteSpace(json) ? null : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json!);
    }

    // =================================================================================================
    //  H1 — Pre/Post-aware sibling isolation (SEC-04). Sibling DIPILIH via SiblingPrePostAwarePredicate:
    //  Pre & Post se-tanggal sah punya struktur Section beda → import ke Pre TAK boleh dibandingkan lawan
    //  paket Post. Pra-fix (key tanpa AssessmentType) → salah-tolak.
    // =================================================================================================

    // H1 (utama): Pre + Post se-tanggal, struktur Section beda → import ke Pre SUKSES (Post bukan sibling).
    [Fact]
    public async Task H1_PrePostSameDate_DifferentSection_ImportNotBlocked()
    {
        int prePackageId; string actorId;
        var sched = DateTime.UtcNow.Date;
        string title = "H1-" + Guid.NewGuid().ToString("N")[..8];
        await using (var seed = NewCtx())
        {
            // Pre session+package: Section 1 akan menerima import 2 soal.
            var (preSessionId, prePkgId, _, _, _) =
                await SeedSessionPackageAsync(seed, "Paket Pre", title, "OJT", sched, assessmentType: "PreTest");
            prePackageId = prePkgId;

            // Post session+package: SAMA Title/Category/Schedule.Date tapi struktur Section BEDA (Section 1 = 3 soal).
            var (_, postPkgId, _, _, _) =
                await SeedSessionPackageAsync(seed, "Paket Post", title, "OJT", sched, assessmentType: "PostTest", linkedSessionId: preSessionId);
            await SeedQuestionsInSectionAsync(seed, postPkgId, sectionNumber: 1, count: 3, tag: "post");

            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Import 2 soal Section 1 ke paket Pre. Sibling Pre-only = tidak ada (hanya paket Pre ini) → guard tak fire.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Pre S1 a?", "p1", "p2", "p3", "p4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Pre S1 b?", "q1", "q2", "q3", "q4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(prePackageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
            // TIDAK boleh terblok oleh perbandingan lawan Post (struktur beda) → no SectionMismatch.
            Assert.Null(DeserializeMismatch((TempDataDictionary)ctrl.TempData));
        }

        await using (var verify = NewCtx())
        {
            // Import berhasil: 2 soal masuk ke paket Pre.
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == prePackageId));
        }
    }

    // H1 (kontrol): dua sibling SE-TIPE (kedua PreTest) struktur beda → import DITOLAK (sibling set memang same-type).
    [Fact]
    public async Task H1_TwoPreTestSiblings_DifferentSection_ImportRejected()
    {
        int targetPreId; string actorId;
        var sched = DateTime.UtcNow.Date;
        string title = "H1c-" + Guid.NewGuid().ToString("N")[..8];
        await using (var seed = NewCtx())
        {
            // Target Pre (import ke sini).
            var (_, tPkg, _, _, _) = await SeedSessionPackageAsync(seed, "Pre A", title, "OJT", sched, assessmentType: "PreTest");
            targetPreId = tPkg;
            // Sibling Pre KEDUA (sama Title/Category/Schedule + AssessmentType=PreTest): Section 1 = 3 soal.
            var (_, sibPkg, _, _, _) = await SeedSessionPackageAsync(seed, "Pre B", title, "OJT", sched, assessmentType: "PreTest");
            await SeedQuestionsInSectionAsync(seed, sibPkg, sectionNumber: 1, count: 3, tag: "sibpre");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Import HANYA 2 soal Section 1 (mismatch vs sibling Pre yang punya 3).
        var file = BuildNewFormatFile(new[]
        {
            new[] { "T S1 a?", "p1", "p2", "p3", "p4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "T S1 b?", "q1", "q2", "q3", "q4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(targetPreId, file, null);
            var mismatch = DeserializeMismatch((TempDataDictionary)ctrl.TempData);
            Assert.NotNull(mismatch);
            Assert.Contains(mismatch!, s => s.Contains("Section 1") && s.Contains("harus sama"));
        }

        await using (var verify = NewCtx())
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPreId));
    }

    // =================================================================================================
    //  H2 — huruf benar WAJIB menunjuk opsi non-kosong (IMP-02/03). Correct='E' dgn E kosong → soal 0-benar
    //  (ungradeable). Pra-fix: ter-import sbg soal 0 IsCorrect. Pasca-fix: baris DITOLAK (skip).
    // =================================================================================================

    // H2 (MC): Correct='E' tapi opsi E kosong → baris ditolak; TIDAK ada soal dgn 0 opsi benar tersimpan.
    [Fact]
    public async Task H2_CorrectLetterPointsToEmptyOption_RowRejected_NoZeroCorrectQuestion()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "H2pkg");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // MC: A-D terisi, E/F kosong, Jawaban Benar='E' → huruf benar nunjuk opsi kosong.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "MC E-kosong?", "A1", "A2", "A3", "A4", "", "", "E", "", "", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(packageId, file, null);
        }

        await using (var verify = NewCtx())
        {
            // Tidak ada soal tersimpan sama sekali.
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId));
            // Defensif: tidak ada soal dgn 0 opsi benar di paket ini.
            var zeroCorrect = await verify.PackageQuestions
                .Where(q => q.AssessmentPackageId == packageId && !q.Options.Any(o => o.IsCorrect))
                .CountAsync();
            Assert.Equal(0, zeroCorrect);
        }
    }

    // H2 (MA): Jawaban Benar='A,E' tapi E kosong → huruf benar tak semua nunjuk opsi terisi → ditolak.
    [Fact]
    public async Task H2_MultipleAnswerCorrectIncludesEmptyOption_RowRejected()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "H2ma");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "MA A,E?", "A1", "A2", "A3", "A4", "", "", "A,E", "", "", "K3", "MultipleAnswer", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(packageId, file, null);
        }

        await using (var verify = NewCtx())
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId));
    }

    // H2 (kontrol): Correct='A' dgn A terisi → ter-import, opsi A IsCorrect=true.
    [Fact]
    public async Task H2_CorrectLetterPointsToFilledOption_Imported()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "H2ok");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "MC A-benar?", "Av", "Bv", "Cv", "Dv", "", "", "A", "", "", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(packageId, file, null);
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("Av", q.Options.Single(o => o.IsCorrect).OptionText); // A benar
        }
    }

    // =================================================================================================
    //  H3 — TOLAK edit soal >4 opsi (preserve data). Form edit hanya A–D; soal 5–6 opsi (E/F) ditolak agar
    //  tak menghapus opsi E/F senyap atau kena hard-block correctCount. Data dipertahankan utuh.
    // =================================================================================================

    // H3 (utama): soal 6 opsi (benar di E) → EditQuestion ditolak (TempData Error >4 opsi), opsi UTUH (6, IsCorrect lestari).
    [Fact]
    public async Task H3_EditQuestionWithMoreThan4Options_Rejected_OptionsUnchanged()
    {
        int packageId, questionId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "H3pkg");
            actorId = (await SeedActorAsync(seed)).Id;
            var q = new PackageQuestion
            {
                AssessmentPackageId = packageId, QuestionText = "Soal 6 opsi", Order = 1, ScoreValue = 10,
                QuestionType = "MultipleChoice", MaxCharacters = 2000,
                Options = new List<PackageOption>
                {
                    new() { OptionText = "A", IsCorrect = false },
                    new() { OptionText = "B", IsCorrect = false },
                    new() { OptionText = "C", IsCorrect = false },
                    new() { OptionText = "D", IsCorrect = false },
                    new() { OptionText = "E", IsCorrect = true  },   // benar di E (di luar A–D form)
                    new() { OptionText = "F", IsCorrect = false },
                }
            };
            seed.PackageQuestions.Add(q);
            await seed.SaveChangesAsync();
            questionId = q.Id;
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "EditQuestion");
            // EditQuestion form A–D: questionType=MultipleChoice, correctA=true (akan dipakai bila lolos guard).
            var res = await ctrl.EditQuestion(
                questionId, packageId, "Soal 6 opsi (edit)", "MultipleChoice", 10, "K3", null, 2000,
                "A", "B", "C", "D", true, false, false, false,
                null, null, false, null, null, null, null,
                null, null, null, null, false, false, false, false,
                sectionId: null);
            var redirect = Assert.IsType<RedirectToActionResult>(res);
            Assert.Equal("ManagePackageQuestions", redirect.ActionName);
            var err = ctrl.TempData["Error"] as string;
            Assert.False(string.IsNullOrWhiteSpace(err));
            Assert.Contains("opsi", err!, StringComparison.OrdinalIgnoreCase);  // pesan keterbatasan >4 opsi
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.Id == questionId);
            Assert.Equal(6, q.Options.Count);                                   // opsi UTUH (tak terhapus)
            Assert.Equal("E", q.Options.Single(o => o.IsCorrect).OptionText);   // IsCorrect asli (E) lestari
            Assert.Equal("Soal 6 opsi", q.QuestionText);                        // teks soal pun tak berubah
        }
    }

    // H3 (kontrol): soal 4 opsi → EditQuestion sukses normal (mengubah teks + jawaban benar).
    [Fact]
    public async Task H3_EditQuestionWith4Options_SucceedsNormally()
    {
        int packageId, questionId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "H3ok");
            actorId = (await SeedActorAsync(seed)).Id;
            var q = new PackageQuestion
            {
                AssessmentPackageId = packageId, QuestionText = "Soal 4 opsi", Order = 1, ScoreValue = 10,
                QuestionType = "MultipleChoice", MaxCharacters = 2000,
                Options = new List<PackageOption>
                {
                    new() { OptionText = "A", IsCorrect = true },
                    new() { OptionText = "B", IsCorrect = false },
                    new() { OptionText = "C", IsCorrect = false },
                    new() { OptionText = "D", IsCorrect = false },
                }
            };
            seed.PackageQuestions.Add(q);
            await seed.SaveChangesAsync();
            questionId = q.Id;
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "EditQuestion");
            var res = await ctrl.EditQuestion(
                questionId, packageId, "Soal 4 opsi (edit)", "MultipleChoice", 10, "K3", null, 2000,
                "A", "B", "C", "D", false, true, false, false,   // pindah jawaban benar → B
                null, null, false, null, null, null, null,
                null, null, null, null, false, false, false, false,
                sectionId: null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.Id == questionId);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("Soal 4 opsi (edit)", q.QuestionText);                 // edit ter-apply
            Assert.Equal("B", q.Options.Single(o => o.IsCorrect).OptionText);   // jawaban benar pindah ke B
        }
    }

    // =================================================================================================
    //  H4 — SamePackage Post-sync saat Section CRUD + skip-if-Post-taken (SEC-06). Mutasi Section pada Pre
    //  men-sync paket Post (SyncToPostIfSamePackageAsync). Bila Post SUDAH dikerjakan (PackageUserResponse),
    //  sync di-SKIP (jangan re-clone → langgar FK Restrict / 500). KUNCI re-check.
    // =================================================================================================

    // Seed sepasang Pre+Post SamePackage; Pre punya 1 paket berisi 1 Section + 1 soal. Return ids paket Pre & Post.
    private static async Task<(int preSessionId, int postSessionId, int prePkgId, int postPkgId)> SeedSamePackagePrePostAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "sp-" + Guid.NewGuid().ToString("N")[..8],
            Email = "sp@test.local", FullName = "SamePkg Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var title = "SP-" + Guid.NewGuid().ToString("N")[..8];
        var sched = DateTime.UtcNow;

        var pre = new AssessmentSession
        {
            UserId = user.Id, Title = title, Category = "OJT", AssessmentType = "PreTest",
            Status = "Open", AccessToken = "", Schedule = sched, DurationMinutes = 60, PassPercentage = 70, Progress = 0
        };
        ctx.AssessmentSessions.Add(pre);
        await ctx.SaveChangesAsync();

        var post = new AssessmentSession
        {
            UserId = user.Id, Title = title, Category = "OJT", AssessmentType = "PostTest",
            Status = "Open", AccessToken = "", Schedule = sched, DurationMinutes = 60, PassPercentage = 70, Progress = 0,
            LinkedSessionId = pre.Id, SamePackage = true
        };
        ctx.AssessmentSessions.Add(post);
        await ctx.SaveChangesAsync();

        // Bidirectional link: SyncToPostIfSamePackageAsync membaca parentSession(Pre).LinkedSessionId → Post.
        // (CreateSection/EditSection memicu sync via Pre, BUKAN via CopyPackagesFromPre yang baca Post.LinkedSessionId.)
        pre.LinkedSessionId = post.Id;
        await ctx.SaveChangesAsync();

        var prePkg = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(prePkg);
        await ctx.SaveChangesAsync();

        var sec = new AssessmentPackageSection { AssessmentPackageId = prePkg.Id, SectionNumber = 1, Name = "Pompa" };
        ctx.AssessmentPackageSections.Add(sec);
        await ctx.SaveChangesAsync();

        ctx.PackageQuestions.Add(new PackageQuestion
        {
            AssessmentPackageId = prePkg.Id, QuestionText = "Pre Q1", Order = 1, ScoreValue = 10,
            QuestionType = "MultipleChoice", SectionId = sec.Id,
            Options = new List<PackageOption> { new() { OptionText = "Ya", IsCorrect = true }, new() { OptionText = "Tidak", IsCorrect = false } }
        });
        await ctx.SaveChangesAsync();

        // Sinkronkan Post sekali agar paket Post ada (mensimulasikan state SamePackage normal).
        // (Tidak memanggil controller di seed; cukup buat paket Post + soal kosong klon dasar lewat sync nyata nanti.)
        return (pre.Id, post.Id, prePkg.Id, post.Id);
    }

    // H4 (a): CreateSection di paket Pre → paket Post tersinkron (struktur Section ikut termuat di Post).
    [Fact]
    public async Task H4_CreateSectionOnPre_SyncsPostStructure()
    {
        int prePkgId, postSessionId; string actorId;
        await using (var seed = NewCtx())
        {
            var ids = await SeedSamePackagePrePostAsync(seed);
            prePkgId = ids.prePkgId;
            postSessionId = ids.postSessionId;
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // CreateSection #2 (No.2) di paket Pre → memicu SyncToPostIfSamePackageAsync.
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CreateSection");
            var res = await ctrl.CreateSection(prePkgId, sectionNumber: 2, name: "Kompresor", startNewPage: true, shuffleEnabled: false);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            // Paket Post ter-clone dari Pre dengan KEDUA Section (No.1 Pompa + No.2 Kompresor).
            var postPkg = await verify.AssessmentPackages.SingleAsync(p => p.AssessmentSessionId == postSessionId);
            var postSections = await verify.AssessmentPackageSections
                .Where(s => s.AssessmentPackageId == postPkg.Id)
                .OrderBy(s => s.SectionNumber)
                .ToListAsync();
            Assert.Equal(2, postSections.Count);
            Assert.Equal(new[] { 1, 2 }, postSections.Select(s => s.SectionNumber).ToArray());
            Assert.Equal("Kompresor", postSections.Single(s => s.SectionNumber == 2).Name);
        }
    }

    // H4 (b) KUNCI: Post SUDAH dikerjakan (PackageUserResponse) → mutasi Section Pre TIDAK throw + Post UTUH (sync di-skip).
    [Fact]
    public async Task H4_SectionEdit_PostAlreadyTaken_SkipsSyncNoThrow()
    {
        int prePkgId, postSessionId, postPkgId, postQuestionId; string actorId;
        await using (var seed = NewCtx())
        {
            var ids = await SeedSamePackagePrePostAsync(seed);
            prePkgId = ids.prePkgId;
            postSessionId = ids.postSessionId;
            actorId = (await SeedActorAsync(seed)).Id;

            // Bangun paket Post nyata (clone Pre) lewat CopyPackagesFromPre agar ada soal Post yang bisa "dikerjakan".
            var actor = await seed.Users.FindAsync(actorId);
            var ctrlSeed = MakeController(seed, actor!, "CopyPackagesFromPre");
            await ctrlSeed.CopyPackagesFromPre(postSessionId);

            var postPkg = await seed.AssessmentPackages.SingleAsync(p => p.AssessmentSessionId == postSessionId);
            postPkgId = postPkg.Id;
            var postQ = await seed.PackageQuestions.FirstAsync(q => q.AssessmentPackageId == postPkgId);
            postQuestionId = postQ.Id;

            // Worker mengerjakan Post: insert PackageUserResponse merujuk soal Post → FK Restrict aktif.
            seed.PackageUserResponses.Add(new PackageUserResponse
            {
                AssessmentSessionId = postSessionId,
                PackageQuestionId = postQuestionId,
                SubmittedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        int postSectionCountBefore;
        int postQuestionCountBefore;
        await using (var pre = NewCtx())
        {
            postSectionCountBefore = await pre.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == postPkgId);
            postQuestionCountBefore = await pre.PackageQuestions.CountAsync(q => q.AssessmentPackageId == postPkgId);
        }

        // Mutasi Section pada paket Pre → SyncToPostIfSamePackageAsync HARUS skip (Post taken). TIDAK boleh throw.
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CreateSection");
            var ex = await Record.ExceptionAsync(() =>
                ctrl.CreateSection(prePkgId, sectionNumber: 2, name: "Kompresor", startNewPage: false, shuffleEnabled: true));
            Assert.Null(ex);                                                    // TIDAK 500 (pra-fix: DbUpdateException FK Restrict)
            Assert.IsType<RedirectToActionResult>(await ctrl.CreateSection(prePkgId, sectionNumber: 3, name: "Turbin", startNewPage: false, shuffleEnabled: true));
        }

        await using (var verify = NewCtx())
        {
            // Pre benar-benar termutasi (Section ke-2/3 dibuat).
            Assert.True(await verify.AssessmentPackageSections.AnyAsync(s => s.AssessmentPackageId == prePkgId && s.SectionNumber == 2));

            // Post UTUH (sync di-skip): jumlah soal & Section Post TIDAK berubah, response peserta tetap ada.
            Assert.Equal(postQuestionCountBefore, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == postPkgId));
            Assert.Equal(postSectionCountBefore, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == postPkgId));
            Assert.True(await verify.PackageUserResponses.AnyAsync(r => r.AssessmentSessionId == postSessionId));
            // Soal Post yang dirujuk response masih ada (tidak ter-RemoveRange).
            Assert.True(await verify.PackageQuestions.AnyAsync(q => q.Id == postQuestionId));
        }
    }

    // =================================================================================================
    //  H4 SOURCE-GUARD (round-3 / Issue-1). Skip-if-Post-taken dipindah KE DALAM SyncPackagesToPost (guard di
    //  SUMBER), sehingga melindungi SEMUA pemanggil — termasuk caller level-paket (CreatePackage / DeletePackage /
    //  CopyPackagesFromPre) yang dulu memanggil SyncPackagesToPost mentah dan akan 500 (FK Restrict) saat Post sudah
    //  dikerjakan. KUNCI: lewat caller LEVEL-PAKET (CreatePackage), bukan Section-CRUD (yang sudah dikunci H4(b)).
    //
    //  Caller dipilih: CreatePackage. Alasan: signature paling lugas (assessmentId Pre + packageName), dan blok
    //  auto-sync di akhirnya memanggil SyncPackagesToPost(Pre,Post) MENTAH (tanpa bungkus try/skip caller-level) —
    //  jadi yang teruji murni guard di SUMBER. Pra-fix: CreatePackage di Pre → SyncPackagesToPost RemoveRange soal
    //  Post yang dirujuk PackageUserResponse → DbUpdateException (FK Restrict) → 500.
    // =================================================================================================
    [Fact]
    public async Task H4Source_CreatePackageOnPreWithTakenPost_NoThrow_PostUntouched()
    {
        int preSessionId, postSessionId, postPkgId, postQuestionId; string actorId;
        int postQuestionTextHashBefore;
        await using (var seed = NewCtx())
        {
            var ids = await SeedSamePackagePrePostAsync(seed);
            preSessionId = ids.preSessionId;
            postSessionId = ids.postSessionId;
            actorId = (await SeedActorAsync(seed)).Id;

            // Bangun paket Post NYATA (clone Pre) via CopyPackagesFromPre agar ada soal Post yang bisa "dikerjakan".
            var actor = await seed.Users.FindAsync(actorId);
            var ctrlSeed = MakeController(seed, actor!, "CopyPackagesFromPre");
            await ctrlSeed.CopyPackagesFromPre(postSessionId);

            var postPkg = await seed.AssessmentPackages.SingleAsync(p => p.AssessmentSessionId == postSessionId);
            postPkgId = postPkg.Id;
            var postQ = await seed.PackageQuestions.FirstAsync(q => q.AssessmentPackageId == postPkgId);
            postQuestionId = postQ.Id;
            postQuestionTextHashBefore = postQ.QuestionText.GetHashCode();

            // Worker mengerjakan Post: PackageUserResponse merujuk soal Post → FK Restrict aktif (RemoveRange = 500).
            seed.PackageUserResponses.Add(new PackageUserResponse
            {
                AssessmentSessionId = postSessionId,
                PackageQuestionId = postQuestionId,
                SubmittedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        int postQuestionCountBefore, postSectionCountBefore, postPkgCountBefore;
        await using (var before = NewCtx())
        {
            postQuestionCountBefore = await before.PackageQuestions.CountAsync(q => q.AssessmentPackageId == postPkgId);
            postSectionCountBefore = await before.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == postPkgId);
            postPkgCountBefore = await before.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postSessionId);
        }

        // CreatePackage caller LEVEL-PAKET di sesi PRE → blok auto-sync memanggil SyncPackagesToPost(Pre,Post) mentah.
        // Guard di SUMBER (Post taken) HARUS men-skip clone → tak throw. Pra-fix: DbUpdateException FK Restrict (500).
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CreatePackage");
            var ex = await Record.ExceptionAsync(() => ctrl.CreatePackage(preSessionId, "Paket Pre Baru"));
            Assert.Null(ex);                                                    // TIDAK 500 (guard di sumber men-skip sync)
        }

        await using (var verify = NewCtx())
        {
            // Pre benar-benar termutasi: paket baru ter-tambah di sesi Pre.
            var preSession = await verify.AssessmentSessions.SingleAsync(s => s.Id == preSessionId);
            Assert.True(await verify.AssessmentPackages.AnyAsync(p => p.AssessmentSessionId == preSessionId && p.PackageName == "Paket Pre Baru"));

            // Post UTUH (sync di-skip, TIDAK di-clone ulang): jumlah paket/soal/Section Post TIDAK berubah.
            Assert.Equal(postPkgCountBefore, await verify.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postSessionId));
            Assert.Equal(postQuestionCountBefore, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == postPkgId));
            Assert.Equal(postSectionCountBefore, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == postPkgId));

            // Soal Post yang dirujuk response masih ada (tidak ter-RemoveRange) + identitasnya tak berubah.
            var postQ = await verify.PackageQuestions.SingleAsync(q => q.Id == postQuestionId);
            Assert.Equal(postQuestionTextHashBefore, postQ.QuestionText.GetHashCode());
            Assert.True(await verify.PackageUserResponses.AnyAsync(r => r.AssessmentSessionId == postSessionId && r.PackageQuestionId == postQuestionId));
        }
    }

    // =================================================================================================
    //  Round-4 (CopyPackagesFromPre false-success). Aksi EKSPLISIT "Salin dari Pre" pada Post yang SUDAH dikerjakan:
    //  source skip-if-taken (round-3) bikin SyncPackagesToPost no-op (FK-safe) — TAPI dulu tetap tampil
    //  TempData["Success"] (sukses palsu). Fix: pre-check → TempData["Error"], bukan sukses palsu; Post dibiarkan utuh.
    // =================================================================================================
    [Fact]
    public async Task CopyPackagesFromPre_PostAlreadyTaken_ShowsError_NotFalseSuccess()
    {
        int postSessionId, postPkgId; string actorId;
        await using (var seed = NewCtx())
        {
            var ids = await SeedSamePackagePrePostAsync(seed);
            postSessionId = ids.postSessionId;
            actorId = (await SeedActorAsync(seed)).Id;

            var actor = await seed.Users.FindAsync(actorId);
            var ctrlSeed = MakeController(seed, actor!, "CopyPackagesFromPre");
            await ctrlSeed.CopyPackagesFromPre(postSessionId);   // bangun paket Post (Post belum taken)

            var postPkg = await seed.AssessmentPackages.SingleAsync(p => p.AssessmentSessionId == postSessionId);
            postPkgId = postPkg.Id;
            var postQ = await seed.PackageQuestions.FirstAsync(q => q.AssessmentPackageId == postPkgId);

            seed.PackageUserResponses.Add(new PackageUserResponse
            {
                AssessmentSessionId = postSessionId,
                PackageQuestionId = postQ.Id,
                SubmittedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        int postQCountBefore;
        await using (var before = NewCtx())
            postQCountBefore = await before.PackageQuestions.CountAsync(q => q.AssessmentPackageId == postPkgId);

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CopyPackagesFromPre");
            var res = await ctrl.CopyPackagesFromPre(postSessionId);   // salin ulang pada Post yang SUDAH taken
            Assert.IsType<RedirectToActionResult>(res);
            var td = (TempDataDictionary)ctrl.TempData;
            Assert.NotNull(td["Error"]);   // pesan "tidak dapat menyalin"
            Assert.Null(td["Success"]);    // BUKAN sukses palsu
        }

        await using (var verify = NewCtx())
            // Post utuh — tak di-clone ulang (jumlah soal sama).
            Assert.Equal(postQCountBefore, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == postPkgId));
    }

    // =================================================================================================
    //  H3 ESSAY-CONVERSION BYPASS CLOSED (round-3 / Issue-3). Guard >4 opsi di EditQuestion DIUBAH dari
    //  `questionType != "Essay" && q.Options.Count > 4` → `q.Options.Count > 4` (tipe APA PUN). Konversi soal
    //  6-opsi tersimpan ke Essay (yang akan RemoveRange SEMUA opsi termasuk E/F senyap) kini JUGA DITOLAK.
    //  Pra-fix: cabang Essay lolos guard → opsi E/F (dan A–D) ter-RemoveRange diam-diam (data loss).
    // =================================================================================================
    [Fact]
    public async Task H3_EditQuestionConvertToEssay_MoreThan4Options_Rejected_OptionsIntact()
    {
        int packageId, questionId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "H3essay");
            actorId = (await SeedActorAsync(seed)).Id;
            // Mirror seed 6-opsi (A–F) dari H3_EditQuestionWithMoreThan4Options_Rejected_OptionsUnchanged.
            var q = new PackageQuestion
            {
                AssessmentPackageId = packageId, QuestionText = "Soal 6 opsi (ke Essay)", Order = 1, ScoreValue = 10,
                QuestionType = "MultipleAnswer", MaxCharacters = 2000,
                Options = new List<PackageOption>
                {
                    new() { OptionText = "A", IsCorrect = true  },
                    new() { OptionText = "B", IsCorrect = false },
                    new() { OptionText = "C", IsCorrect = false },
                    new() { OptionText = "D", IsCorrect = false },
                    new() { OptionText = "E", IsCorrect = true  },   // benar di E (di luar A–D form)
                    new() { OptionText = "F", IsCorrect = false },
                }
            };
            seed.PackageQuestions.Add(q);
            await seed.SaveChangesAsync();
            questionId = q.Id;
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "EditQuestion");
            // Sama signature dgn H3 existing, TAPI questionType="Essay" (konversi). Pra-fix: lolos guard → RemoveRange opsi.
            var res = await ctrl.EditQuestion(
                questionId, packageId, "Soal 6 opsi (jadi Essay)", "Essay", 10, "K3", "Rubrik X", 2000,
                "A", "B", "C", "D", false, false, false, false,
                null, null, false, null, null, null, null,
                null, null, null, null, false, false, false, false,
                sectionId: null);
            var redirect = Assert.IsType<RedirectToActionResult>(res);
            Assert.Equal("ManagePackageQuestions", redirect.ActionName);
            var err = ctrl.TempData["Error"] as string;
            Assert.False(string.IsNullOrWhiteSpace(err));
            Assert.Contains("opsi", err!, StringComparison.OrdinalIgnoreCase);  // pesan keterbatasan >4 opsi (bukan rubrik/dll)
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.Id == questionId);
            Assert.Equal(6, q.Options.Count);                                   // 6 opsi UTUH (TIDAK ter-RemoveRange oleh cabang Essay)
            Assert.Equal("MultipleAnswer", q.QuestionType);                     // tipe tak berubah jadi Essay
            Assert.Equal("Soal 6 opsi (ke Essay)", q.QuestionText);            // teks soal tak berubah
            var correct = q.Options.Where(o => o.IsCorrect).Select(o => o.OptionText).OrderBy(x => x).ToList();
            Assert.Equal(new[] { "A", "E" }, correct);                          // IsCorrect asli (A & E) lestari
        }
    }

    // =================================================================================================
    //  M1 — dedup-aware sibling count (IMP-03). Count untuk validasi sibling = jumlah yang BENAR-BENAR ter-insert
    //  (setelah dedup), bukan jumlah baris valid mentah. Sibling Section 1 = 2; import 3 baris (1 duplikat) →
    //  distinct=2 → cocok → import sukses, tepat 2 soal di Section 1.
    // =================================================================================================
    [Fact]
    public async Task M1_DedupAwareCount_MatchesSiblingAfterDedup_ImportSucceeds()
    {
        int targetPackageId; string actorId; int siblingPackageId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket B");
            var (_, sibPkgId, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            siblingPackageId = sibPkgId;
            await SeedQuestionsInSectionAsync(seed, siblingPackageId, sectionNumber: 1, count: 2, tag: "sib");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // 3 baris Section 1: baris #2 IDENTIK baris #1 (dedup) → distinct = 2 (cocok sibling).
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Dup soal?",  "d1", "d2", "d3", "d4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Dup soal?",  "d1", "d2", "d3", "d4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" }, // duplikat fingerprint
            new[] { "Beda soal?", "e1", "e2", "e3", "e4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(targetPackageId, file, null);
            Assert.Null(DeserializeMismatch((TempDataDictionary)ctrl.TempData)); // count pakai distinct (2) → cocok
        }

        await using (var verify = NewCtx())
        {
            // Tepat 2 soal ter-insert (duplikat di-skip) — count validasi == jumlah ter-insert (no phantom inflation).
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
        }
    }

    // =================================================================================================
    //  M2 — deteksi format dari NAMA HEADER (IMP-02). File legacy 9-kolom dgn konten nyasar di kolom 10
    //  (LastCellUsed > 9) TIDAK boleh salah-deteksi format baru. Validasi nama header marker.
    // =================================================================================================

    // M2 (utama): legacy 9-kolom + sel nyasar @kolom10 (header) → tetap di-parse LEGACY (SectionId=null, A–D, jawaban benar utuh).
    [Fact]
    public async Task M2_LegacyHeadersWithStrayColumn10_ParsedAsLegacy_NotShifted()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "M2pkg");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Bangun manual: header legacy 9-kolom + konten NYASAR di header kolom 10 (memaksa LastCellUsed=10).
        IFormFile file;
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Question Import");
            var legacyHeaders = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Jawaban Benar", "Elemen Teknis", "QuestionType", "Rubrik" };
            for (int i = 0; i < legacyHeaders.Length; i++) ws.Cell(1, i + 1).Value = legacyHeaders[i];
            ws.Cell(1, 10).Value = "catatan-nyasar"; // konten di kolom 10 → colCount=10 tapi BUKAN "Nama Section"
            // Data row legacy: Q | A-D | Correct(C) | ET | Type | Rubrik (9 kolom).
            var dr = new[] { "Legacy stray?", "L1", "L2", "L3", "L4", "C", "K3", "MultipleChoice", "" };
            for (int c = 0; c < dr.Length; c++) if (!string.IsNullOrEmpty(dr[c])) ws.Cell(2, c + 1).Value = dr[c];
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            file = new FormFile(ms, 0, ms.Length, "excelFile", "legacy-stray.xlsx")
            { Headers = new HeaderDictionary(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(packageId, file, null);
        }

        await using (var verify = NewCtx())
        {
            // Di-parse LEGACY: TIDAK ada Section, SectionId=null, hanya opsi A–D, jawaban benar 'C' (opsi ke-3).
            Assert.Equal(0, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == packageId));
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.Null(q.SectionId);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("L3", q.Options.Single(o => o.IsCorrect).OptionText); // C = opsi ke-3 (kolom tidak geser)
        }
    }

    // M2 (kontrol): template 13-kolom asli (header "Opsi E"/"Opsi F"/"No. Section"/"Nama Section") → format baru (E/F + Section dihormati).
    [Fact]
    public async Task M2_GenuineNewFormatHeaders_ParsedAsNewFormat()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "M2new");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "MA 5 opsi?", "N1", "N2", "N3", "N4", "N5", "", "A,E", "1", "Pompa", "K3", "MultipleAnswer", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(packageId, file, null);
        }

        await using (var verify = NewCtx())
        {
            // Format baru: Section dihormati + opsi E tersimpan + jawaban A,E benar.
            Assert.Equal(1, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == packageId && s.SectionNumber == 1));
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.NotNull(q.SectionId);
            Assert.Equal(5, q.Options.Count);                                   // A–E
            var correct = q.Options.Where(o => o.IsCorrect).Select(o => o.OptionText).OrderBy(x => x).ToList();
            Assert.Equal(new[] { "N1", "N5" }, correct);                        // A & E benar (opsi E terbaca)
        }
    }

    // =================================================================================================
    //  M3 — baseline count ADDITIVE (IMP-03). Target SUDAH punya Section 1 = 2. Sibling Section 1 = 2.
    //  Import 2 LAGI → pasca-insert 4 vs sibling 2 → DITOLAK. Baseline harus sertakan soal existing target.
    // =================================================================================================

    // M3 (utama): target sudah 2 + import 2 = 4 vs sibling 2 → DITOLAK (baseline additive).
    [Fact]
    public async Task M3_AdditiveBaseline_TargetPreexisting_ImportRejected()
    {
        int targetPackageId; string actorId; int siblingPackageId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket B");
            var (_, sibPkgId, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            siblingPackageId = sibPkgId;

            // Target SUDAH punya 2 soal di Section 1.
            await SeedQuestionsInSectionAsync(seed, targetPackageId, sectionNumber: 1, count: 2, tag: "tgt");
            // Sibling Section 1 = 2 soal.
            await SeedQuestionsInSectionAsync(seed, siblingPackageId, sectionNumber: 1, count: 2, tag: "sib");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Import 2 LAGI ke Section 1 → pasca-insert target = 4 vs sibling 2 → mismatch.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Tambah a?", "n1", "n2", "n3", "n4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Tambah b?", "m1", "m2", "m3", "m4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(targetPackageId, file, null);
            var mismatch = DeserializeMismatch((TempDataDictionary)ctrl.TempData);
            Assert.NotNull(mismatch);
            Assert.Contains(mismatch!, s => s.Contains("Section 1") && s.Contains("harus sama"));
        }

        await using (var verify = NewCtx())
        {
            // 0 write tambahan: tetap 2 soal (import ditolak atomik).
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
        }
    }

    // M3 (kontrol): target KOSONG + import 2 mencapai sibling 2 → SUKSES.
    [Fact]
    public async Task M3_EmptyTarget_ReachesSiblingCount_ImportSucceeds()
    {
        int targetPackageId; string actorId; int siblingPackageId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket B");
            var (_, sibPkgId, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            siblingPackageId = sibPkgId;
            await SeedQuestionsInSectionAsync(seed, siblingPackageId, sectionNumber: 1, count: 2, tag: "sib");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "Kosong a?", "n1", "n2", "n3", "n4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Kosong b?", "m1", "m2", "m3", "m4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(targetPackageId, file, null);
            Assert.Null(DeserializeMismatch((TempDataDictionary)ctrl.TempData));
        }

        await using (var verify = NewCtx())
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
    }

    // =================================================================================================
    //  L3 — bandingkan SETIAP sibling, bukan stop-at-First() (IMP-03). Intent L3 fix hidup di KEDUA cabang
    //  (legacy 7296-7301 + Section-aware 7307-7319). Lock di sini lewat cabang Section-aware (yang REACHABLE):
    //  dua sibling dgn struktur Section berbeda; import cocok SALAH SATU tapi bukan yang lain → DITOLAK
    //  (tertangkap vs sibling non-cocok — membuktikan loop tak berhenti di sibling pertama).
    //  Varian legacy-murni (all-null) di-lock terpisah oleh L3_LegacyAllNull_WithSiblings_ComparesEvery_NoNullKeyThrow
    //  (dulu ke-escalate karena bug null-key Dictionary<int?,int>; sudah di-fix via SectionStructureComparer.KeyOf).
    // =================================================================================================
    [Fact]
    public async Task L3_ComparesEverySibling_NotStopAtFirst_RejectsAgainstNonMatching()
    {
        int targetPackageId; string actorId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket Target");
            // Sibling A: Section 1 = 2 soal (akan COCOK dengan incoming).
            var (_, sibA, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            await SeedQuestionsInSectionAsync(seed, sibA, sectionNumber: 1, count: 2, tag: "sibA");
            // Sibling B: Section 1 = 3 soal (akan TIDAK cocok — inkonsistensi pre-existing antar-saudara).
            var (_, sibB, _, _, _) = await SeedSessionPackageAsync(seed, "Paket B", title, cat, sched);
            await SeedQuestionsInSectionAsync(seed, sibB, sectionNumber: 1, count: 3, tag: "sibB");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Import 2 soal Section 1 → cocok sibling A (2) tapi TIDAK sibling B (3) → harus DITOLAK.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Sec1 a?", "L1", "L2", "L3", "L4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Sec1 b?", "M1", "M2", "M3", "M4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            await ctrl.ImportPackageQuestions(targetPackageId, file, null);
            var mismatch = DeserializeMismatch((TempDataDictionary)ctrl.TempData);
            Assert.NotNull(mismatch);
            // Mismatch tertangkap melawan sibling B yang punya 3 (loop TIDAK berhenti di sibling A yg cocok).
            Assert.Contains(mismatch!, s => s.Contains("Section 1") && s.Contains("3 soal") && s.Contains("Paket B"));
        }

        await using (var verify = NewCtx())
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
    }

    // =================================================================================================
    //  L3 (LEGACY-NULL, sebelumnya ESCALATE → kini REACHABLE pasca-fix null-safe key).
    //  Pra-fix: `existingCounts`/`incomingCounts` memakai Dictionary<int?,int> dgn key q.Section?.SectionNumber.
    //  Saat ada paket saudara dgn soal (cabang validasi aktif) DAN soal legacy ber-Section null → GroupBy
    //  menghasilkan key null → `incomingCounts[grp.Key]` melempar ArgumentNullException (500) SEBELUM
    //  perbandingan. Pasca-fix: SectionStructureComparer.KeyOf(null) → LainnyaKey (int.MinValue) → tidak throw,
    //  perbandingan total-count legacy berjalan terhadap SETIAP saudara.
    // =================================================================================================

    // L3-legacy (utama): dua saudara legacy all-null dgn total BEDA (A=2, B=3). Legacy-import 2 soal ke target
    // → cocok A tapi bukan B → DITOLAK (mismatchList non-empty + TempData["SectionMismatch"]) DAN TIDAK throw.
    [Fact]
    public async Task L3_LegacyAllNull_WithSiblings_ComparesEvery_NoNullKeyThrow()
    {
        int targetPackageId; string actorId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            // Target legacy (Standard, all-null). Kosong saat import.
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket Target Legacy");
            // Sibling A legacy all-null = 2 soal (akan COCOK dgn incoming total 2).
            var (_, sibA, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            await SeedQuestionsInSectionAsync(seed, sibA, sectionNumber: null, count: 2, tag: "legA");
            // Sibling B legacy all-null = 3 soal (TIDAK cocok → inkonsistensi pre-existing antar-saudara).
            var (_, sibB, _, _, _) = await SeedSessionPackageAsync(seed, "Paket B", title, cat, sched);
            await SeedQuestionsInSectionAsync(seed, sibB, sectionNumber: null, count: 3, tag: "legB");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // LEGACY .xlsx 9-kolom (tanpa kolom Section) → semua baris SectionNumber=null. Total incoming = 2.
        var file = BuildLegacyFormatFile(new[]
        {
            new[] { "Legacy a?", "L1", "L2", "L3", "L4", "A", "K3", "MultipleChoice", "" },
            new[] { "Legacy b?", "M1", "M2", "M3", "M4", "A", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            // KUNCI: pra-fix melempar ArgumentNullException di sini (key null pada Dictionary<int?,int>).
            var ex = await Record.ExceptionAsync(() => ctrl.ImportPackageQuestions(targetPackageId, file, null));
            Assert.Null(ex);                                                     // tidak ada exception (null key di-handle via LainnyaKey)
            var mismatch = DeserializeMismatch((TempDataDictionary)ctrl.TempData);
            Assert.NotNull(mismatch);                                            // ditolak (cocok A tapi bukan B)
            // Cabang legacy total-count: mismatch tertangkap vs Paket B (3 soal) — loop tak berhenti di A yg cocok.
            Assert.Contains(mismatch!, s => s.Contains("Lainnya") && s.Contains("3 soal") && s.Contains("Paket B"));
        }

        await using (var verify = NewCtx())
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId)); // 0 write (tolak atomik)
    }

    // L3-legacy (happy-path null-safe): saudara legacy all-null dgn total COCOK → import SUKSES, no SectionMismatch, no throw.
    [Fact]
    public async Task NullSafe_LegacyImportWithMatchingSibling_Succeeds_NoThrow()
    {
        int targetPackageId; string actorId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket Target Legacy OK");
            // Satu saudara legacy all-null = 2 soal → cocok incoming total 2.
            var (_, sib, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            await SeedQuestionsInSectionAsync(seed, sib, sectionNumber: null, count: 2, tag: "legOK");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildLegacyFormatFile(new[]
        {
            new[] { "Legacy ok a?", "L1", "L2", "L3", "L4", "A", "K3", "MultipleChoice", "" },
            new[] { "Legacy ok b?", "M1", "M2", "M3", "M4", "A", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var ex = await Record.ExceptionAsync(() => ctrl.ImportPackageQuestions(targetPackageId, file, null));
            Assert.Null(ex);                                                     // tidak throw (null key aman)
            Assert.Null(DeserializeMismatch((TempDataDictionary)ctrl.TempData)); // total cocok → tak terblok
        }

        await using (var verify = NewCtx())
        {
            // Import sukses: 2 soal masuk, semua tanpa Section (legacy → SectionId null).
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId && q.SectionId != null));
            Assert.Equal(0, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == targetPackageId));
        }
    }

    // Null-safe MIXED: import format-baru berisi soal Section 1 + soal "Lainnya" (kolom Section kosong → null),
    // melawan saudara ber-struktur SAMA (Section 1 = 1 + Lainnya = 1). Perbandingan per-Section (cabang
    // Section-aware) menyertakan grup "Lainnya" via sentinel LainnyaKey → BERJALAN tanpa throw + DITERIMA (cocok).
    [Fact]
    public async Task NullSafe_MixedSectionAndLainnya_ComparesWithoutThrow()
    {
        int targetPackageId; string actorId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket Target Mixed");
            // Saudara: Section 1 = 1 soal + Lainnya (null) = 1 soal → struktur campuran sama dgn incoming.
            var (_, sib, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            await SeedQuestionsInSectionAsync(seed, sib, sectionNumber: 1, count: 1, tag: "mixSec");
            await SeedQuestionsInSectionAsync(seed, sib, sectionNumber: null, count: 1, tag: "mixLain");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Format baru: baris #1 Section 1, baris #2 kolom Section KOSONG → Lainnya (sectionNumber null).
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Sec1 soal?",    "S1", "S2", "S3", "S4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Lainnya soal?", "X1", "X2", "X3", "X4", "", "", "A", "",  "",     "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            // KUNCI: grup "Lainnya" (null) dibandingkan via sentinel — pra-fix throw saat GroupBy key null.
            var ex = await Record.ExceptionAsync(() => ctrl.ImportPackageQuestions(targetPackageId, file, null));
            Assert.Null(ex);                                                     // perbandingan per-Section tak throw
            Assert.Null(DeserializeMismatch((TempDataDictionary)ctrl.TempData)); // Section 1 (1==1) + Lainnya (1==1) cocok → diterima
        }

        await using (var verify = NewCtx())
        {
            // Import diterima: 2 soal masuk (1 ber-Section 1, 1 Lainnya/null).
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
            Assert.Equal(1, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId && q.SectionId != null));
            Assert.Equal(1, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId && q.SectionId == null));
        }
    }

    // =================================================================================================
    //  L1 (D: backfill-saat-kosong) — re-import mengisi Nama Section existing yang KOSONG dari kolom Excel
    //  "Nama Section", TAPI tidak menimpa Nama yang sudah ada (lindungi edit manual di panel). Pra-fix:
    //  guard `newSections.Contains(existing)` selalu false untuk Section dari DB → Nama existing tak pernah
    //  ke-backfill (silent no-op). Single package (tanpa sibling) → count guard di-skip.
    // =================================================================================================
    [Fact]
    public async Task L1_ImportBackfillsBlankSectionName_DoesNotOverwriteExisting()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            var (_, pkgId, _, _, _) = await SeedSessionPackageAsync(seed, "Paket L1");
            packageId = pkgId;
            // Section 1: Nama KOSONG (null) → harus ke-backfill dari Excel.
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection { AssessmentPackageId = pkgId, SectionNumber = 1, Name = null });
            // Section 2: Nama sudah ada → TIDAK boleh ditimpa.
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection { AssessmentPackageId = pkgId, SectionNumber = 2, Name = "Manual Pompa" });
            await seed.SaveChangesAsync();
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "S1 soal?", "a1", "a2", "a3", "a4", "", "", "A", "1", "Excel Pompa",    "K3", "MultipleChoice", "" },
            new[] { "S2 soal?", "b1", "b2", "b3", "b4", "", "", "A", "2", "Excel Override", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
            Assert.Null(DeserializeMismatch((TempDataDictionary)ctrl.TempData));
        }

        await using (var verify = NewCtx())
        {
            var sec1 = await verify.AssessmentPackageSections.FirstAsync(s => s.AssessmentPackageId == packageId && s.SectionNumber == 1);
            var sec2 = await verify.AssessmentPackageSections.FirstAsync(s => s.AssessmentPackageId == packageId && s.SectionNumber == 2);
            Assert.Equal("Excel Pompa", sec1.Name);   // KOSONG → ke-backfill dari Excel
            Assert.Equal("Manual Pompa", sec2.Name);  // non-kosong → TIDAK ditimpa
        }
    }
}
