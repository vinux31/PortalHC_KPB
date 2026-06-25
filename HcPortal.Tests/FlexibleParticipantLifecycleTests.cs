// Phase 413 Plan 01 (PART-06 + PART-07 + PRMV-01 + PRMV-03 + PRMV-04 + PRMV-05) —
// INTEGRASI LINTAS-FASE de-tautologis untuk seluruh siklus add/remove/restore peserta live.
//
// Celah yang ditutup (BELUM ada di test per-fase): 410 menguji add, 411 menguji remove/restore
// terpisah, 409 menguji guard re-entry terpisah — TIDAK ADA satu test pun yang menjalankan
// SELURUH siklus `add → start → soft-remove → guard-block → restore → aktif lagi` sebagai SATU
// alur DB nyata. File ini mengunci integrasi lintas-fase tersebut.
//
// De-tautology 999.12 (WAJIB): setiap test MENJALANKAN action AssessmentAdminController ASLI
// (AddParticipantsLive / RemoveParticipantLive / RestoreParticipantLive) + memanggil helper PRODUKSI
// ASLI CMPController.IsParticipantRemoved (public static — helper yang sama dipakai guard inline
// StartExam :373 / SubmitExam :924 / :1611) + meng-assert kolom DB NYATA via reload ctx baru.
// TIDAK ada replica predikat (NO SessionHasDataAsync / DeriveReadyStatus / WindowAllowsAddition tiruan;
// NO RecordCascadeDeleteService.ExecuteAsync langsung — hard-delete di-drive lewat RemoveParticipantLive).
//
//   L1 Lifecycle_Add_Start_SoftRemove_GuardBlocks_Restore_Active (PART-06 + PRMV-01 + PRMV-03 + PRMV-04):
//       Add ASLI (ready-status + eager UPA) → flip InProgress via ctx → RemoveParticipantLive ASLI (soft) →
//       CMPController.IsParticipantRemoved(reload) == true (guard re-entry lintas-fase, peserta tak bisa
//       lanjut/submit) → RestoreParticipantLive ASLI → IsParticipantRemoved == false (aktif lagi).
//   L2 Lifecycle_PrePost_Add_SoftRemoveBoth_RestoreBoth (PART-07 + PRMV-05):
//       Add Pre/Post ASLI → pasangan (LinkedSessionId cross-set) → flip Pre InProgress → remove soft →
//       KEDUA RemovedAt!=null + peserta lain TIDAK ikut (Pitfall 1) → restore → KEDUA RemovedAt==null.
//   L3 Lifecycle_Add_NotStarted_HardRemove_RowAndUpaGone (PRMV-01, mini-DI):
//       Add bersih ASLI → remove hard via mini-DI → baris + UPA HILANG (AnyAsync==false) → restore 404/400.
//
// DB disposable HcPortalDB_Test_{guid} (HcPortalDB_Dev TIDAK disentuh; CLAUDE.md Seed Workflow). migration=FALSE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

// =====================================================================================================
//  LIFECYCLE LINTAS-FASE — WRITE-PATH (SQLEXPRESS disposable; REUSE FlexibleParticipantAddFixture)
//  Drive AddParticipantsLive / RemoveParticipantLive / RestoreParticipantLive ASLI penuh: StubUserManager
//  (override GetUserAsync) + NoopNotificationService + NoopHubContext → action menulis ke DB NYATA; assert
//  kolom nyata + panggil CMPController.IsParticipantRemoved PRODUKSI (guard re-entry). Hard-delete via
//  mini-DI service-provider (RecordCascadeDeleteService) di HttpContext.RequestServices.
//  Helper di-COPY verbatim dari FlexibleParticipantAddLiveTests + FlexibleParticipantRemoveTests (isolasi).
// =====================================================================================================
[Trait("Category", "Integration")]
public class FlexibleParticipantLifecycleTests : IClassFixture<FlexibleParticipantAddFixture>
{
    private readonly FlexibleParticipantAddFixture _fixture;
    public FlexibleParticipantLifecycleTests(FlexibleParticipantAddFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Stub UserManager minimal: hanya GetUserAsync(ClaimsPrincipal) dipakai action (actor resolve). ----
    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        private readonly ApplicationUser _actor;
        public StubUserManager(ApplicationUser actor)
            : base(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
            => _actor = actor;
        public override Task<ApplicationUser?> GetUserAsync(System.Security.Claims.ClaimsPrincipal principal)
            => Task.FromResult<ApplicationUser?>(_actor);
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

    // ---- No-op INotificationService (action kirim notif post-commit; SendAsync return true). ----
    private sealed class NoopNotificationService : INotificationService
    {
        public Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null) => Task.FromResult(true);
        public Task<List<UserNotification>> GetAsync(string userId, int count = 50) => Task.FromResult(new List<UserNotification>());
        public Task<bool> MarkAsReadAsync(int notificationId, string userId) => Task.FromResult(true);
        public Task<int> MarkAllAsReadAsync(string userId) => Task.FromResult(0);
        public Task<int> GetUnreadCountAsync(string userId) => Task.FromResult(0);
        public Task<bool> SendByTemplateAsync(string userId, string type, Dictionary<string, object>? context = null) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int notificationId, string userId) => Task.FromResult(true);
    }

    // Instantiate AssessmentAdminController ASLI atas SQLEXPRESS ctx (stub userManager actor + no-op notif + noop hub).
    private AssessmentAdminController MakeLiveController(ApplicationDbContext ctx, ApplicationUser actor, string actionName = "AddParticipantsLive")
    {
        var auditLog = new AuditLogService(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        #pragma warning disable CS8625
        var ctrl = new AssessmentAdminController(
            ctx,
            userManager:             new StubUserManager(actor),
            auditLog:                auditLog,
            env:                     null!,
            cache:                   cache,
            logger:                  NullLogger<AssessmentAdminController>.Instance,
            notificationService:     new NoopNotificationService(),
            hubContext:              new NoopHubContext(),   // endpoint broadcast post-commit → hub non-null wajib
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
        return ctrl;
    }

    // ===== mini-DI service-provider stub untuk hard-delete (COPY verbatim FlexibleParticipantRemoveTests) =====
    // Jalur hard-delete RemoveParticipantCoreAsync panggil HttpContext.RequestServices
    //   .GetRequiredService<RecordCascadeDeleteService>(). DefaultHttpContext().RequestServices KOSONG → throw.
    // Bangun ServiceProvider minimal yang share ctx SQLEXPRESS yang SAMA dengan controller.
    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = System.IO.Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "HcPortal.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = System.IO.Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }

    private static IServiceProvider BuildCascadeServiceProvider(ApplicationDbContext ctx)
    {
        var services = new ServiceCollection();
        services.AddSingleton(ctx);                                                       // ctx SQLEXPRESS yang SAMA
        services.AddSingleton(new AuditLogService(ctx));
        services.AddSingleton<INotificationService>(new NoopNotificationService());
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<ProtonCompletionService>>(NullLogger<ProtonCompletionService>.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<RecordCascadeDeleteService>>(NullLogger<RecordCascadeDeleteService>.Instance);
        services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment());
        services.AddScoped<ProtonCompletionService>();
        services.AddScoped<RecordCascadeDeleteService>();
        return services.BuildServiceProvider();
    }

    private AssessmentAdminController MakeLiveControllerWithCascade(ApplicationDbContext ctx, ApplicationUser actor, string actionName = "RemoveParticipantLive")
    {
        var ctrl = MakeLiveController(ctx, actor, actionName);
        ctrl.ControllerContext.HttpContext.RequestServices = BuildCascadeServiceProvider(ctx);
        return ctrl;
    }

    // ---- Seed helpers (pola FlexibleParticipantAddLiveTests / FlexibleParticipantRemoveTests) ----
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string? fullName = null, string? nip = null)
    {
        var u = new ApplicationUser
        {
            UserName = "lc-" + Guid.NewGuid().ToString("N")[..8], Email = "lc@test.local",
            FullName = fullName ?? "Lifecycle Test", NIP = nip, IsActive = true
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedRepSessionAsync(ApplicationDbContext ctx, string userId, string title,
        string category, DateTime schedule, string status, DateTime? examWindowCloseDate = null,
        string? assessmentType = null, int? linkedGroupId = null, bool generateCertificate = false,
        int durationMinutes = 60)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status, AccessToken = "",
            Schedule = schedule, DurationMinutes = durationMinutes, PassPercentage = 70, Progress = 0,
            ExamWindowCloseDate = examWindowCloseDate, AssessmentType = assessmentType, LinkedGroupId = linkedGroupId,
            GenerateCertificate = generateCertificate
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // Seed AssessmentPackage + 3 PackageQuestion + Options ke rep → CreateEagerAssignmentsAsync membuat UPA
    // (pola FlexibleParticipantAddLiveTests T6).
    private static async Task SeedPackageWithQuestionsAsync(ApplicationDbContext ctx, int repSessionId)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = repSessionId, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        for (int i = 0; i < 3; i++)
        {
            var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionText = $"Soal {i}", Order = i, ScoreValue = 10, QuestionType = "MultipleChoice" };
            ctx.PackageQuestions.Add(q);
            await ctx.SaveChangesAsync();
            ctx.PackageOptions.AddRange(
                new PackageOption { PackageQuestionId = q.Id, OptionText = "A", IsCorrect = true },
                new PackageOption { PackageQuestionId = q.Id, OptionText = "B", IsCorrect = false });
        }
        await ctx.SaveChangesAsync();
    }

    private static string? ModeOf(JsonResult json)
    {
        var raw = JsonSerializer.Serialize(json.Value);
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.TryGetProperty("mode", out var m) ? m.GetString() : null;
    }

    private static int? LinkedSessionIdOf(JsonResult json)
    {
        var raw = JsonSerializer.Serialize(json.Value);
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.TryGetProperty("linkedSessionId", out var l) && l.ValueKind == JsonValueKind.Number
            ? l.GetInt32() : (int?)null;
    }

    // Flip sesi ke InProgress via ctx (state-transition test-driven, BUKAN replica logika produksi).
    // Produksi flip via StartExam yang butuh HTTP/SignalR (di luar scope unit) — kita simulasikan transisi DB.
    private async Task FlipInProgressAsync(int sessionId)
    {
        await using var ctx = NewCtx();
        var s = await ctx.AssessmentSessions.FindAsync(sessionId);
        s!.StartedAt = DateTime.UtcNow;
        s.Status = S.InProgress;
        s.CompletedAt = null;
        await ctx.SaveChangesAsync();
    }

    // =================================================================================================
    //  L1 — STANDARD LIFECYCLE (de-tautology penuh)
    //  Add ASLI → flip InProgress → soft-remove ASLI → guard PRODUKSI true → restore ASLI → guard false.
    // =================================================================================================
    [Fact]
    public async Task Lifecycle_Add_Start_SoftRemove_GuardBlocks_Restore_Active()
    {
        var title = "LC-Std-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);   // schedule lampau (WIB) → DeriveReadyStatus = Open
        string repUser, newUser, actorId;
        int repId;

        // --- Seed batch standard + paket soal (eager UPA) + user baru + actor ---
        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Rep");
            newUser = await SeedUserAsync(seed, "Peserta Baru", "L1-001");
            actorId = await SeedUserAsync(seed, "Admin Actor", "L1-ADM");
            repId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Open);
            await SeedPackageWithQuestionsAsync(seed, repId);   // → UPA eager saat Add
        }

        // === LANGKAH 2 — AddParticipantsLive ASLI → ready-status + eager UPA ===
        int newSessionId;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "AddParticipantsLive");

            var result = await ctrl.AddParticipantsLive(repId, new List<string> { newUser });
            Assert.IsType<JsonResult>(result);   // sukses → JSON added[]/skipped[]

            newSessionId = await act.AssessmentSessions
                .Where(a => a.Title == title && a.UserId == newUser).Select(a => a.Id).FirstAsync();
        }

        // Assert sesi baru ready-status + UPA eager (kolom NYATA dari produksi).
        await using (var verify = NewCtx())
        {
            var added = await verify.AssessmentSessions.FindAsync(newSessionId);
            Assert.NotNull(added);
            Assert.Equal(S.Open, added!.Status);                 // DeriveReadyStatus → Open (schedule lampau)
            Assert.NotEqual(S.InProgress, added.Status);         // NEVER InProgress saat add (PART-06)
            Assert.Null(added.StartedAt);
            Assert.Null(added.CompletedAt);
            Assert.Null(added.RemovedAt);                        // aktif
            // Guard PRODUKSI: sesi yang baru ditambah BELUM removed → guard false (boleh mulai/lanjut).
            Assert.False(CMPController.IsParticipantRemoved(added));
            // UPA eager tercipta (A1).
            Assert.True(await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == newSessionId));
        }

        // === LANGKAH 3 — flip InProgress via ctx (peserta mulai mengerjakan) ===
        await FlipInProgressAsync(newSessionId);
        await using (var ip = NewCtx())
        {
            var s = await ip.AssessmentSessions.FindAsync(newSessionId);
            Assert.Equal(S.InProgress, s!.Status);
            Assert.NotNull(s.StartedAt);
            // Saat InProgress + belum removed → guard PRODUKSI tetap false (peserta sah lanjut).
            Assert.False(CMPController.IsParticipantRemoved(s));
        }

        // === LANGKAH 4 — RemoveParticipantLive ASLI (soft, sesi berdata/InProgress) ===
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "RemoveParticipantLive");

            var result = await ctrl.RemoveParticipantLive(newSessionId, "tidak hadir");
            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("soft", ModeOf(json));   // InProgress → SOFT (preserve)
        }

        // === LANGKAH 5 — GUARD LINTAS-FASE: IsParticipantRemoved PRODUKSI == true (PRMV-03) ===
        // Inilah inti integrasi: sesi yang ditambah+dihapus lewat ENDPOINT NYATA membuat helper guard
        // produksi (yang dipanggil StartExam/SubmitExam) memblok re-entry. BUKAN replica predikat.
        await using (var verify = NewCtx())
        {
            var removed = await verify.AssessmentSessions.FindAsync(newSessionId);
            Assert.NotNull(removed);
            Assert.NotNull(removed!.RemovedAt);                          // soft-removed NYATA
            Assert.Equal(actorId, removed.RemovedBy);
            Assert.Equal("tidak hadir", removed.RemovalReason);
            Assert.Equal(S.InProgress, removed.Status);                  // Status UNCHANGED (guard andalkan RemovedAt)
            // GUARD PRODUKSI ASLI → true → StartExam/SubmitExam akan redirect (peserta tak bisa lanjut/submit).
            Assert.True(CMPController.IsParticipantRemoved(removed));
        }

        // === LANGKAH 6 — RestoreParticipantLive ASLI → aktif lagi, guard false ===
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "RestoreParticipantLive");

            var result = await ctrl.RestoreParticipantLive(newSessionId);
            var json = Assert.IsType<JsonResult>(result);
            var raw = JsonSerializer.Serialize(json.Value);
            using var doc = JsonDocument.Parse(raw);
            Assert.True(doc.RootElement.GetProperty("restored").GetBoolean());
        }

        await using (var verify = NewCtx())
        {
            var restored = await verify.AssessmentSessions.FindAsync(newSessionId);
            Assert.NotNull(restored);
            Assert.Null(restored!.RemovedAt);                            // 3 kolom removal di-clear NYATA
            Assert.Null(restored.RemovedBy);
            Assert.Null(restored.RemovalReason);
            // GUARD PRODUKSI ASLI → false → peserta boleh lanjut/submit lagi (re-entry dibuka kembali).
            Assert.False(CMPController.IsParticipantRemoved(restored));
        }
    }

    // =================================================================================================
    //  L2 — PRE/POST LIFECYCLE (PART-07 + PRMV-05)
    //  Add Pre/Post pair ASLI → flip Pre InProgress → soft-remove keduanya → restore keduanya.
    //  Peserta LAIN di batch TIDAK ikut removed (Pitfall 1).
    // =================================================================================================
    [Fact]
    public async Task Lifecycle_PrePost_Add_SoftRemoveBoth_RestoreBoth()
    {
        var title = "LC-PrePost-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var preSched  = DateTime.UtcNow.AddHours(7).AddHours(-3);   // Pre lebih awal
        var postSched = DateTime.UtcNow.AddHours(7).AddHours(-1);   // Post lebih lambat
        var preWindow  = DateTime.UtcNow.AddHours(7).AddDays(3);
        var postWindow = DateTime.UtcNow.AddHours(7).AddDays(10);
        const int linkedGroupId = 9911;
        string repUser, newUser, otherUser, actorId;
        int preRepId, otherPreRepId;

        // --- Seed batch Pre/Post existing (rep) + peserta LAIN + user baru + actor ---
        await using (var seed = NewCtx())
        {
            repUser   = await SeedUserAsync(seed, "Peserta Rep");
            newUser   = await SeedUserAsync(seed, "Peserta Baru", "L2-001");
            otherUser = await SeedUserAsync(seed, "Peserta Lain", "L2-OTH");
            actorId   = await SeedUserAsync(seed, "Admin Actor", "L2-ADM");

            // Existing Pre/Post pasangan rep (PreTest cert=false + PostTest cert=true, config distinct).
            preRepId = await SeedRepSessionAsync(seed, repUser, title, cat, preSched, S.Open,
                examWindowCloseDate: preWindow, assessmentType: "PreTest", linkedGroupId: linkedGroupId,
                generateCertificate: false, durationMinutes: 45);
            await SeedRepSessionAsync(seed, repUser, title, cat, postSched, S.Open,
                examWindowCloseDate: postWindow, assessmentType: "PostTest", linkedGroupId: linkedGroupId,
                generateCertificate: true, durationMinutes: 90);

            // Peserta LAIN (Pre saja, di batch sama) — Pitfall 1: tak boleh ikut ter-remove.
            otherPreRepId = await SeedRepSessionAsync(seed, otherUser, title, cat, preSched, S.Open,
                examWindowCloseDate: preWindow, assessmentType: "PreTest", linkedGroupId: linkedGroupId);
        }

        // === Add Pre/Post pair ASLI (caller passing PreTest rep) ===
        int newPreId, newPostId;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "AddParticipantsLive");

            var result = await ctrl.AddParticipantsLive(preRepId, new List<string> { newUser });
            Assert.IsType<JsonResult>(result);

            var userSessions = await act.AssessmentSessions
                .Where(a => a.UserId == newUser && a.Title == title).ToListAsync();
            Assert.Equal(2, userSessions.Count);   // pasangan Pre + Post tercipta
            newPreId  = userSessions.Single(s => s.AssessmentType == "PreTest").Id;
            newPostId = userSessions.Single(s => s.AssessmentType == "PostTest").Id;
        }

        // Assert pasangan: LinkedSessionId cross-set 2-arah + ready-status + guard PRODUKSI false (aktif).
        await using (var verify = NewCtx())
        {
            var newPre  = await verify.AssessmentSessions.FindAsync(newPreId);
            var newPost = await verify.AssessmentSessions.FindAsync(newPostId);
            Assert.Equal(newPost!.Id, newPre!.LinkedSessionId);   // cross-link Pre → Post
            Assert.Equal(newPre.Id,  newPost.LinkedSessionId);    // cross-link Post → Pre
            Assert.Equal(S.Open, newPre.Status);
            Assert.Equal(S.Open, newPost.Status);
            Assert.False(CMPController.IsParticipantRemoved(newPre));
            Assert.False(CMPController.IsParticipantRemoved(newPost));
        }

        // === Flip Pre InProgress (berdata → jalur soft) ===
        await FlipInProgressAsync(newPreId);

        // === RemoveParticipantLive(newPreId) ASLI → soft KEDUA (pair-as-unit) ===
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "RemoveParticipantLive");

            var result = await ctrl.RemoveParticipantLive(newPreId, "keliru tambah pasangan");
            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("soft", ModeOf(json));
            Assert.Equal(newPostId, LinkedSessionIdOf(json));   // JSON linkedSessionId == partner Post
        }

        // KEDUA partner soft-removed + guard PRODUKSI true; peserta LAIN TIDAK ikut (Pitfall 1).
        await using (var verify = NewCtx())
        {
            var vPre   = await verify.AssessmentSessions.FindAsync(newPreId);
            var vPost  = await verify.AssessmentSessions.FindAsync(newPostId);
            var vOther = await verify.AssessmentSessions.FindAsync(otherPreRepId);
            Assert.NotNull(vPre!.RemovedAt);    // Pre soft-removed
            Assert.NotNull(vPost!.RemovedAt);   // Post soft-removed (pair-as-unit)
            Assert.True(CMPController.IsParticipantRemoved(vPre));    // guard re-entry true (Pre)
            Assert.True(CMPController.IsParticipantRemoved(vPost));   // guard re-entry true (Post)
            Assert.Null(vOther!.RemovedAt);                          // peserta LAIN tetap aktif (Pitfall 1)
            Assert.False(CMPController.IsParticipantRemoved(vOther));
        }

        // === RestoreParticipantLive(newPreId) ASLI → KEDUA partner aktif lagi ===
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "RestoreParticipantLive");

            var result = await ctrl.RestoreParticipantLive(newPreId);
            var json = Assert.IsType<JsonResult>(result);
            var raw = JsonSerializer.Serialize(json.Value);
            using var doc = JsonDocument.Parse(raw);
            Assert.True(doc.RootElement.GetProperty("restored").GetBoolean());
        }

        await using (var verify = NewCtx())
        {
            var vPre  = await verify.AssessmentSessions.FindAsync(newPreId);
            var vPost = await verify.AssessmentSessions.FindAsync(newPostId);
            Assert.Null(vPre!.RemovedAt);    // KEDUA partner di-clear NYATA
            Assert.Null(vPost!.RemovedAt);
            Assert.False(CMPController.IsParticipantRemoved(vPre));
            Assert.False(CMPController.IsParticipantRemoved(vPost));
        }
    }

    // =================================================================================================
    //  L3 — HARD-DELETE LIFECYCLE (PRMV-01, mini-DI)
    //  Add bersih ASLI → remove hard via mini-DI → baris + UPA HILANG → restore tak mungkin (404/400).
    // =================================================================================================
    [Fact]
    public async Task Lifecycle_Add_NotStarted_HardRemove_RowAndUpaGone()
    {
        var title = "LC-Hard-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, newUser, actorId;
        int repId;

        // --- Seed batch + paket soal (UPA eager) + user baru + actor ---
        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Rep");
            newUser = await SeedUserAsync(seed, "Peserta Bersih", "L3-001");
            actorId = await SeedUserAsync(seed, "Admin Actor", "L3-ADM");
            repId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Open);
            await SeedPackageWithQuestionsAsync(seed, repId);   // → UPA eager saat Add (D-01: bukan "data")
        }

        // === Add bersih ASLI → sesi baru not-started (StartedAt null, 0 response) + UPA eager ===
        int newSessionId;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "AddParticipantsLive");

            var result = await ctrl.AddParticipantsLive(repId, new List<string> { newUser });
            Assert.IsType<JsonResult>(result);

            newSessionId = await act.AssessmentSessions
                .Where(a => a.Title == title && a.UserId == newUser).Select(a => a.Id).FirstAsync();
        }

        // Sanity: sesi bersih + UPA ADA SEBELUM remove (membuktikan assert AnyAsync==false bukan tautologi).
        await using (var pre = NewCtx())
        {
            var s = await pre.AssessmentSessions.FindAsync(newSessionId);
            Assert.Null(s!.StartedAt);   // not-started → jalur HARD
            Assert.True(await pre.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == newSessionId));
        }

        // === RemoveParticipantLive ASLI via mini-DI (cascade) → mode="hard" ===
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveControllerWithCascade(act, actor!);

            var result = await ctrl.RemoveParticipantLive(newSessionId, reason: null);   // reason opsional di hard (D-02)
            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("hard", ModeOf(json));
        }

        // === Baris + UPA HILANG NYATA (D-01: UPA bukan "data", hard tetap cascade bersih) ===
        await using (var verify = NewCtx())
        {
            Assert.False(await verify.AssessmentSessions.AnyAsync(s => s.Id == newSessionId));
            Assert.False(await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == newSessionId));
        }

        // === Restore tak mungkin (baris sudah hilang) → 404/400 (hard tak reversibel) ===
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!, "RestoreParticipantLive");

            var result = await ctrl.RestoreParticipantLive(newSessionId);
            Assert.True(result is NotFoundObjectResult or BadRequestObjectResult,
                $"Restore atas baris hard-deleted harus 404/400, dapat {result.GetType().Name}");
        }
    }
}
