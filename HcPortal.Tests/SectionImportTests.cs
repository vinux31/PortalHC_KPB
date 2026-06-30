// Phase 415-03 (Wave 3) — Import Excel diperluas: dual-format parser + Section auto-create + fingerprint
// (+Opsi E/F +SectionNumber) + per-Section count hard-block (D-13 titik #1). REQ: IMP-01/02/03 + SEC-04.
//
// De-tautology: tes membangun .xlsx NYATA via ClosedXML (XLWorkbook → MemoryStream → IFormFile stub) lalu
// menggerakkan action ASLI AssessmentAdminController.ImportPackageQuestions (parser/fingerprint/auto-create
// dijalankan apa-adanya; TIDAK ada replika logika di test). Assert state DB nyata (soal+opsi+section ditambah,
// 0 write saat mismatch) + ketidakcocokan per-Section daftar LENGKAP.
//
// Integration: butuh FK (Section→Package) + unique index + Section auto-create persist → SQLEXPRESS nyata
// (SectionFixture, di-reuse). DB lokal HcPortalDB_Dev TAK tersentuh. [Trait Category=Integration].
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
public class SectionImportTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public SectionImportTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Controller harness (salin pola SectionCrudTests) ----
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
            protonBypassService:     null!,
            retakeService:           new RetakeService(ctx, auditLog, new NoopHubContext(), NullLogger<RetakeService>.Instance));
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

    // Seed: 1 session + 1 package, return (sessionId, packageId, title, category, scheduleDate).
    private static async Task<(int sessionId, int packageId, string title, string category, DateTime sched)> SeedSessionPackageAsync(
        ApplicationDbContext ctx, string packageName = "Paket A", string? title = null, string? category = null, DateTime? sched = null)
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
            Schedule = sc, DurationMinutes = 60, PassPercentage = 70, Progress = 0
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = packageName, PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return (session.Id, pkg.Id, t, cat, sc);
    }

    // ---- .xlsx fixture builders (ClosedXML) ----

    // 14-col header (universal/new format) — Phase 999.17-02: +kolom "Skor" (N/14) untuk bobot ScoreValue.
    // rows: string[14] (Skor di index 13). string[13] (tanpa Skor) tetap valid → kolom 14 kosong = default 10.
    private static IFormFile BuildNewFormatFile(IEnumerable<string[]> dataRows)
    {
        var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Opsi E", "Opsi F", "Jawaban Benar", "No. Section", "Nama Section", "Elemen Teknis", "QuestionType", "Rubrik", "Skor" };
        return BuildFile(headers, dataRows);
    }

    // 13-col header (universal LAMA, TANPA kolom Skor) — regression LEGACY-SAFE (999.17-02): file Universal yang
    // sudah beredar sebelum kolom Skor tetap terdeteksi format BARU (marker 6/7/9/10 utuh) + ScoreValue default 10.
    private static IFormFile BuildUniversal13ColFile(IEnumerable<string[]> dataRows)
    {
        var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Opsi E", "Opsi F", "Jawaban Benar", "No. Section", "Nama Section", "Elemen Teknis", "QuestionType", "Rubrik" };
        return BuildFile(headers, dataRows);
    }

    // 9-col header (legacy). rows: string[9] (Pertanyaan|A-D|Jawaban Benar|ET|Type|Rubrik).
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

    // ===================== IMP-01: new 13-col roundtrip + Section auto-create + A–F =====================
    [Fact]
    public async Task NewFormat_ImportsOptionsAtoF_AutoCreatesSection_StoresCorrectLetters()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Soal MC dgn jawaban benar B di Section 1 "Pompa"; soal MA 6 opsi jawaban A,C,E di Section 1.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal MC Pompa?", "A1", "A2", "A3", "A4", "",   "",   "B",     "1", "Pompa", "K3", "MultipleChoice", "" },
            new[] { "Soal MA 6 opsi?", "B1", "B2", "B3", "B4", "B5", "B6", "A,C,E", "1", "Pompa", "K3", "MultipleAnswer", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var section = await verify.AssessmentPackageSections
                .SingleAsync(s => s.AssessmentPackageId == packageId && s.SectionNumber == 1);
            Assert.Equal("Pompa", section.Name);
            Assert.False(section.StartNewPage);   // Excel tak bawa toggle → default
            Assert.True(section.ShuffleEnabled);

            var qs = await verify.PackageQuestions
                .Include(q => q.Options)
                .Where(q => q.AssessmentPackageId == packageId)
                .ToListAsync();
            Assert.Equal(2, qs.Count);
            Assert.All(qs, q => Assert.Equal(section.Id, q.SectionId)); // ter-assign ke Section 1

            var mc = qs.Single(q => q.QuestionType == "MultipleChoice");
            Assert.Equal(4, mc.Options.Count);
            Assert.Single(mc.Options.Where(o => o.IsCorrect));
            Assert.Equal("A2", mc.Options.Single(o => o.IsCorrect).OptionText); // B = opsi ke-2

            var ma = qs.Single(q => q.QuestionType == "MultipleAnswer");
            Assert.Equal(6, ma.Options.Count);                                  // A–F semua tersimpan
            var correct = ma.Options.Where(o => o.IsCorrect).Select(o => o.OptionText).OrderBy(x => x).ToList();
            Assert.Equal(new[] { "B1", "B3", "B5" }, correct);                  // A,C,E benar (E = opsi ke-5)
        }
    }

    // ===================== IMP-02: legacy 9-col backward-compat =====================
    [Fact]
    public async Task LegacyFormat_Imports_SectionIdNull_NoOptionsEF()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildLegacyFormatFile(new[]
        {
            new[] { "Legacy MC?", "L1", "L2", "L3", "L4", "C", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == packageId));
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.Null(q.SectionId);                       // SectionId=null (Lainnya)
            Assert.Equal(4, q.Options.Count);               // hanya A–D (E/F kosong → tidak dibuat)
            Assert.Equal("L3", q.Options.Single(o => o.IsCorrect).OptionText); // C = opsi ke-3
        }
    }

    // ===================== IMP-03: fingerprint +Section +E/F (no false dedup) =====================
    [Fact]
    public async Task Fingerprint_SameQuestion_DifferentSection_NotDeduped()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Soal IDENTIK (teks+opsi+jawaban) tapi beda SectionNumber → HARUS dianggap berbeda (2 row tersimpan).
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal sama?", "x1", "x2", "x3", "x4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Soal sama?", "x1", "x2", "x3", "x4", "", "", "A", "2", "Sec2", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var qs = await verify.PackageQuestions.Where(q => q.AssessmentPackageId == packageId).ToListAsync();
            Assert.Equal(2, qs.Count);                                          // beda section → tidak ter-dedup
            Assert.Equal(2, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == packageId));
        }
    }

    [Fact]
    public async Task Fingerprint_IdenticalQuestionSameSection_Deduped()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Soal IDENTIK + section sama → row ke-2 di-skip (dedup), hanya 1 tersimpan.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal dup?", "y1", "y2", "y3", "y4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "Soal dup?", "y1", "y2", "y3", "y4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            Assert.Equal(1, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId));
        }
    }

    // ===================== SEC-04 / D-13: per-Section count mismatch → full list + 0 write =====================
    [Fact]
    public async Task PerSectionCountMismatch_RejectsImport_FullList_ZeroWrites()
    {
        // Paket saudara A (sibling, sudah ada soal) + paket B (target import). Sibling key Title+Category+Schedule.Date.
        int targetPackageId; string actorId; int siblingPackageId; int siblingSectionId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            // Paket B target (import ke sini).
            (var sessB, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket B");
            // Paket A sibling: SAMA Title+Category+Schedule → seed session+package terpisah dgn nilai identik.
            var (_, sibPkgId, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            siblingPackageId = sibPkgId;
            actorId = (await SeedActorAsync(seed)).Id;

            // Sibling Section 1 dgn 2 soal.
            var sibSec = new AssessmentPackageSection { AssessmentPackageId = siblingPackageId, SectionNumber = 1, Name = "Sec1" };
            seed.AssessmentPackageSections.Add(sibSec);
            await seed.SaveChangesAsync();
            siblingSectionId = sibSec.Id;
            seed.PackageQuestions.AddRange(
                new PackageQuestion { AssessmentPackageId = siblingPackageId, QuestionText = "sibQ1", Order = 1, SectionId = siblingSectionId, QuestionType = "MultipleChoice" },
                new PackageQuestion { AssessmentPackageId = siblingPackageId, QuestionText = "sibQ2", Order = 2, SectionId = siblingSectionId, QuestionType = "MultipleChoice" });
            await seed.SaveChangesAsync();
        }

        // Import ke Paket B: Section 1 hanya 1 soal (mismatch: sibling punya 2) + Section 2 1 soal (sibling punya 0) → 2 entri mismatch.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "B Sec1 soal?", "p1", "p2", "p3", "p4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "B Sec2 soal?", "q1", "q2", "q3", "q4", "", "", "A", "2", "Sec2", "K3", "MultipleChoice", "" },
        });

        TempDataDictionary capturedTempData;
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(targetPackageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
            capturedTempData = (TempDataDictionary)ctrl.TempData;
        }

        // Daftar ketidakcocokan LENGKAP (2 entri: Section 1 & Section 2), bukan stop-at-first.
        var smJson = capturedTempData["SectionMismatch"] as string;
        Assert.False(string.IsNullOrWhiteSpace(smJson));
        var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(smJson!);
        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        Assert.Contains(list, s => s.Contains("Section 1") && s.Contains("harus sama"));
        Assert.Contains(list, s => s.Contains("Section 2") && s.Contains("harus sama"));

        // 0 write ke Paket B (atomic reject).
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
            Assert.Equal(0, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == targetPackageId));
        }
    }

    [Fact]
    public async Task PerSectionCountMatch_AcrossSiblings_ImportSucceeds()
    {
        int targetPackageId; string actorId; int siblingPackageId;
        string title; string cat; DateTime sched;
        await using (var seed = NewCtx())
        {
            (_, targetPackageId, title, cat, sched) = await SeedSessionPackageAsync(seed, "Paket B");
            var (_, sibPkgId, _, _, _) = await SeedSessionPackageAsync(seed, "Paket A", title, cat, sched);
            siblingPackageId = sibPkgId;
            actorId = (await SeedActorAsync(seed)).Id;

            var sibSec = new AssessmentPackageSection { AssessmentPackageId = siblingPackageId, SectionNumber = 1, Name = "Sec1" };
            seed.AssessmentPackageSections.Add(sibSec);
            await seed.SaveChangesAsync();
            seed.PackageQuestions.AddRange(
                new PackageQuestion { AssessmentPackageId = siblingPackageId, QuestionText = "sibQ1", Order = 1, SectionId = sibSec.Id, QuestionType = "MultipleChoice" },
                new PackageQuestion { AssessmentPackageId = siblingPackageId, QuestionText = "sibQ2", Order = 2, SectionId = sibSec.Id, QuestionType = "MultipleChoice" });
            await seed.SaveChangesAsync();
        }

        // Import ke Paket B: Section 1 PERSIS 2 soal → cocok → import sukses.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "B Sec1 A?", "p1", "p2", "p3", "p4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
            new[] { "B Sec1 B?", "q1", "q2", "q3", "q4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(targetPackageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
            var smJson = ((TempDataDictionary)ctrl.TempData)["SectionMismatch"] as string;
            Assert.True(string.IsNullOrWhiteSpace(smJson)); // tidak ada mismatch
        }

        await using (var verify = NewCtx())
        {
            Assert.Equal(2, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == targetPackageId));
            Assert.Equal(1, await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == targetPackageId));
        }
    }

    [Fact]
    public async Task SinglePackage_NoSibling_PerSectionGuardSkipped_ImportSucceeds()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed, "Solo");
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Tanpa paket saudara → guard per-Section di-skip; import jalan walau hanya 1 soal di Section 1.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Solo soal?", "s1", "s2", "s3", "s4", "", "", "A", "1", "Sec1", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            Assert.Equal(1, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId));
        }
    }

    // ============================================================================================
    // Phase 999.17-02 — kolom Skor (N/14) → PackageQuestion.ScoreValue + validasi server 1-100 (D-09)
    //   + tolak-keras-atomic (D-12) + kompatibel-mundur (D-07/LEGACY-SAFE). Tuple/parser dijalankan apa-adanya.
    // ============================================================================================

    // SKOR-PARSE (D-06): kolom Skor="25" → ScoreValue=25 (ganti hardcode 10).
    [Fact]
    public async Task Import_UniversalWithSkor_StoresScoreValue()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // 1 soal MC Universal, kolom Skor (index 13) = "25". No.Section kosong → Lainnya (tanpa sibling → guard skip).
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal skor 25?", "A1", "A2", "A3", "A4", "", "", "A", "", "", "K3", "MultipleChoice", "", "25" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.Equal(25, q.ScoreValue);   // RED sekarang: hardcode ScoreValue=10
        }
    }

    // D-08: kolom Skor kosong → default 10 (guard kompatibilitas; hijau bahkan sebelum impl).
    [Fact]
    public async Task Import_UniversalBlankSkor_DefaultsTo10()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal skor kosong?", "A1", "A2", "A3", "A4", "", "", "A", "", "", "K3", "MultipleChoice", "", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.Equal(10, q.ScoreValue);
        }
    }

    // SKOR-VAL atomic (D-12): ≥1 baris Skor invalid → 0 write + daftar error LENGKAP via TempData["ScoreErrors"].
    [Fact]
    public async Task Import_InvalidSkor_AtomicReject_FullList_ZeroWrites()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // 3 soal: baris-1 valid (25), baris-2 Skor="0" (≤0), baris-3 Skor="abc" (non-angka) → 2 error → tolak SEMUA.
        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal valid?",   "A1", "A2", "A3", "A4", "", "", "A", "", "", "K3", "MultipleChoice", "", "25" },
            new[] { "Soal skor 0?",  "B1", "B2", "B3", "B4", "", "", "A", "", "", "K3", "MultipleChoice", "", "0" },
            new[] { "Soal skor abc?","C1", "C2", "C3", "C4", "", "", "A", "", "", "K3", "MultipleChoice", "", "abc" },
        });

        TempDataDictionary capturedTempData;
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
            capturedTempData = (TempDataDictionary)ctrl.TempData;
        }

        var seJson = capturedTempData["ScoreErrors"] as string;
        Assert.False(string.IsNullOrWhiteSpace(seJson));
        var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(seJson!);
        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);   // daftar LENGKAP (bukan stop-at-first), baris valid pun tak lolos

        // 0 write (atomic reject D-12).
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId));
        }
    }

    // SKOR-VAL varian (D-09): >100, desimal, negatif → masing-masing ditolak (0 write).
    [Theory]
    [InlineData("101")]
    [InlineData("5.5")]
    [InlineData("-3")]
    public async Task Import_SkorOutOfRangeOrDecimal_Rejected_ZeroWrites(string badScore)
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        var file = BuildNewFormatFile(new[]
        {
            new[] { "Soal skor bad?", "A1", "A2", "A3", "A4", "", "", "A", "", "", "K3", "MultipleChoice", "", badScore },
        });

        TempDataDictionary capturedTempData;
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
            capturedTempData = (TempDataDictionary)ctrl.TempData;
        }

        var seJson = capturedTempData["ScoreErrors"] as string;
        Assert.False(string.IsNullOrWhiteSpace(seJson));
        await using (var verify = NewCtx())
        {
            Assert.Equal(0, await verify.PackageQuestions.CountAsync(q => q.AssessmentPackageId == packageId));
        }
    }

    // LEGACY-SAFE (a): file Universal 13-kolom LAMA (tanpa Skor) → tetap NEW + section dibuat + ScoreValue=10.
    [Fact]
    public async Task Import_Universal13ColOld_NoSkorColumn_StillNewFormat_Default10()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            (_, packageId, _, _, _) = await SeedSessionPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // 13-kolom (tanpa Skor) + No.Section=1 → terdeteksi NEW (marker 6/7/9/10 utuh) + section auto-create.
        var file = BuildUniversal13ColFile(new[]
        {
            new[] { "Soal lama?", "A1", "A2", "A3", "A4", "", "", "A", "1", "Pompa", "K3", "MultipleChoice", "" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "ImportPackageQuestions");
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var q = await verify.PackageQuestions.Include(x => x.Options)
                .SingleAsync(x => x.AssessmentPackageId == packageId);
            Assert.Equal(10, q.ScoreValue);                 // default (tak ada kolom Skor)
            Assert.NotNull(q.SectionId);                    // terdeteksi NEW → section auto-create
            Assert.Equal(4, q.Options.Count);               // A–D
        }
    }
}
