// Phase 411 Plan 02 (PRMV-01 + PRMV-04 + PRMV-05 + PLIV-03) — de-tautology integration untuk
// endpoint remove/restore-participant live (RemoveParticipantLive / RestoreParticipantLive).
//
// Mengunci kontrak Plan 411-01 (RemoveParticipantCoreAsync + 2 wrapper JSON) secara DE-TAUTOLOGIS
// (lesson 999.12, WAJIB): setiap test MENJALANKAN action AssessmentAdminController ASLI / meng-assert
// kolom DB NYATA — TIDAK ada replica predikat SessionHasDataAsync, TIDAK panggil cascade ExecuteAsync
// langsung. Hard: AnyAsync == false (baris+UPA NYATA hilang). Soft: RemovedAt set NYATA +
// Score/IsPassed/NomorSertifikat/Status UNCHANGED (kolom DB direload).
//
//   Bagian A (read-path, FlexibleParticipantRemoveReadTests) → InMemory real-controller (pola
//       FlexibleParticipantAddLiveEligibleTests). Test yang RETURN sebelum resolve-actor
//       (_userManager null! aman): Proton-reject (400), idempotency (RemovedAt!=null → noop),
//       NotFound (404), restore-guard (non-removed → 400), restore NotFound (404).
//
//   Bagian B (write-path, FlexibleParticipantRemoveWriteTests) → SQLEXPRESS disposable
//       (REUSE FlexibleParticipantAddFixture). RemoveParticipantLive/RestoreParticipantLive ASLI
//       di-drive penuh — StubUserManager (override GetUserAsync) + NoopNotificationService.
//       Soft (preserve score/cert), reason-wajib-soft (400 + 0-write), idempotent write, restore
//       clear-3-kolom, Pre/Post pair soft (keduanya + peserta lain untouched), audit row.
//       Hard-delete (row gone + UPA gone D-01 + Pre/Post both-clean hard) via mini-DI
//       service-provider stub (HttpContext.RequestServices ber-RecordCascadeDeleteService).
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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

// =====================================================================================================
//  BAGIAN A — READ-PATH (InMemory real-controller; pola FlexibleParticipantAddLiveEligibleTests)
//  Drive RemoveParticipantLive / RestoreParticipantLive ASLI untuk jalur yang RETURN SEBELUM
//  resolve-actor (_userManager.GetUserAsync). Proton-reject + idempotency + NotFound + restore-guard
//  semua return sebelum baris `await _userManager.GetUserAsync(User)` → userManager null! AMAN.
// =====================================================================================================
public class FlexibleParticipantRemoveReadTests
{
    // actionName WAJIB di-set: controller override View() merujuk ActionDescriptor.ActionName (NRE bila null).
    private static (AssessmentAdminController ctrl, ApplicationDbContext ctx) MakeController(string actionName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        var auditLog = new AuditLogService(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());

        #pragma warning disable CS8625
        var ctrl = new AssessmentAdminController(
            ctx,
            userManager:             null!,
            auditLog:                auditLog,
            env:                     null!,
            cache:                   cache,
            logger:                  NullLogger<AssessmentAdminController>.Instance,
            notificationService:     null!,
            hubContext:              null!,
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
        return (ctrl, ctx);
    }

    private static AssessmentSession MakeSession(int id, string title, string category, string status,
        DateTime? removedAt = null, DateTime? startedAt = null) => new AssessmentSession
    {
        Id = id, UserId = "u-" + id, Title = title, Category = category, AccessToken = "",
        Schedule = new DateTime(2026, 6, 17, 8, 0, 0), Status = status, DurationMinutes = 60, PassPercentage = 70,
        RemovedAt = removedAt, StartedAt = startedAt, CreatedAt = new DateTime(2026, 6, 16, 8, 0, 0)
    };

    // Baca property "mode" dari JsonResult.Value (anonymous) via reflection — pola FlexibleParticipantAddLiveTests
    // (JsonSerializer round-trip → JsonDocument). NO replica logika.
    private static string? ModeOf(JsonResult json)
    {
        var raw = JsonSerializer.Serialize(json.Value);
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.TryGetProperty("mode", out var m) ? m.GetString() : null;
    }

    // A1 (Proton reject, scope §F): sesi Category="Assessment Proton" → RemoveParticipantLive ASLI → 400 + 0-write.
    [Fact]
    public async Task RemoveParticipantLive_Proton_Rejected400()
    {
        var (ctrl, ctx) = MakeController("RemoveParticipantLive");
        ctx.AssessmentSessions.Add(MakeSession(1, "BATCH-PROTON", "Assessment Proton", S.Open));
        ctx.SaveChanges();

        var result = await ctrl.RemoveParticipantLive(1, "alasan apapun");

        Assert.IsType<BadRequestObjectResult>(result);
        // 0-write: sesi masih ada, RemovedAt null (tak ter-soft-remove).
        var s = await ctx.AssessmentSessions.FindAsync(1);
        Assert.NotNull(s);
        Assert.Null(s!.RemovedAt);
    }

    // A2 (idempotency, PRMV-01, Pitfall 3): sesi RemovedAt!=null → RemoveParticipantLive ASLI → JsonResult mode="noop".
    [Fact]
    public async Task RemoveParticipantLive_AlreadyRemoved_NoOp()
    {
        var (ctrl, ctx) = MakeController("RemoveParticipantLive");
        ctx.AssessmentSessions.Add(MakeSession(2, "BATCH-X", "OJT", S.Open, removedAt: DateTime.UtcNow));
        ctx.SaveChanges();

        var result = await ctrl.RemoveParticipantLive(2, "alasan");

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal("noop", ModeOf(json));   // idempotent: panggilan ke sesi sudah-removed = no-op sukses
    }

    // A3 (NotFound, PRMV-01): sessionId tak ada → RemoveParticipantLive ASLI → 404.
    [Fact]
    public async Task RemoveParticipantLive_NotFound_404()
    {
        var (ctrl, _) = MakeController("RemoveParticipantLive");

        var result = await ctrl.RemoveParticipantLive(99999, "alasan");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // A4 (restore-guard, PRMV-04): sesi aktif (RemovedAt null) → RestoreParticipantLive ASLI → 400.
    [Fact]
    public async Task RestoreParticipantLive_NotRemoved_Rejected400()
    {
        var (ctrl, ctx) = MakeController("RestoreParticipantLive");
        ctx.AssessmentSessions.Add(MakeSession(3, "BATCH-Y", "OJT", S.Open, removedAt: null));
        ctx.SaveChanges();

        var result = await ctrl.RestoreParticipantLive(3);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Sesi ini tidak dalam keadaan dihapus.", JsonSerializer.Serialize(bad.Value));
    }

    // A5 (restore NotFound, PRMV-04): sessionId tak ada → RestoreParticipantLive ASLI → 404.
    [Fact]
    public async Task RestoreParticipantLive_NotFound_404()
    {
        var (ctrl, _) = MakeController("RestoreParticipantLive");

        var result = await ctrl.RestoreParticipantLive(88888);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}

// =====================================================================================================
//  BAGIAN B — WRITE-PATH (SQLEXPRESS disposable; REUSE FlexibleParticipantAddFixture)
//  Drive RemoveParticipantLive / RestoreParticipantLive ASLI penuh: StubUserManager (override
//  GetUserAsync) + NoopNotificationService → action menulis ke DB NYATA; assert kolom nyata
//  (RemovedAt/RemovedBy/RemovalReason set, Score/cert/Status UNCHANGED, AuditLogs, baris+UPA hilang).
//  Tidak ada replica predikat — logika produksi (SessionHasDataAsync/reason-gate/Pre-Post/cascade) DIJALANKAN.
//  Hard-delete: mini-DI service-provider (Task 2) di HttpContext.RequestServices.
// =====================================================================================================
[Trait("Category", "Integration")]
public class FlexibleParticipantRemoveWriteTests : IClassFixture<FlexibleParticipantAddFixture>
{
    private readonly FlexibleParticipantAddFixture _fixture;
    public FlexibleParticipantRemoveWriteTests(FlexibleParticipantAddFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Stub UserManager minimal: hanya GetUserAsync(ClaimsPrincipal) dipakai Remove/Restore (actor resolve). ----
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

    // ---- No-op INotificationService (RecordCascadeDelete 411 tak kirim notif user-bound; SendAsync return true). ----
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

    // Instantiate AssessmentAdminController ASLI atas SQLEXPRESS ctx (stub userManager actor + no-op notif).
    private AssessmentAdminController MakeLiveController(ApplicationDbContext ctx, ApplicationUser actor, string actionName = "RemoveParticipantLive")
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
            hubContext:              null!,
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
        return ctrl;
    }

    // ---- Seed helpers (pola FlexibleParticipantAddLiveTests) ----
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string? fullName = null, string? nip = null)
    {
        var u = new ApplicationUser
        {
            UserName = "rm-" + Guid.NewGuid().ToString("N")[..8], Email = "rm@test.local",
            FullName = fullName ?? "Remove Test", NIP = nip, IsActive = true
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedRepSessionAsync(ApplicationDbContext ctx, string userId, string title,
        string category, DateTime schedule, string status, DateTime? startedAt = null,
        int? score = null, bool? isPassed = null, string? nomorSertifikat = null, string? manualSertifikatUrl = null,
        string? assessmentType = null, int? linkedGroupId = null, int? linkedSessionId = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status, AccessToken = "",
            Schedule = schedule, DurationMinutes = 60, PassPercentage = 70, Progress = 0,
            StartedAt = startedAt, Score = score, IsPassed = isPassed,
            NomorSertifikat = nomorSertifikat, ManualSertifikatUrl = manualSertifikatUrl,
            AssessmentType = assessmentType, LinkedGroupId = linkedGroupId, LinkedSessionId = linkedSessionId
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // Seed 1 PackageUserResponse untuk sesi → tandai sesi "berdata" (jalur soft). FK butuh PackageQuestion valid.
    private static async Task SeedResponseAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionText = "Soal 1", Order = 0, ScoreValue = 10, QuestionType = "MultipleChoice" };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = sessionId, PackageQuestionId = q.Id, TextAnswer = "jawaban", SubmittedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
    }

    // Seed 1 UserPackageAssignment eager (simulasi 410) → untuk D-01 hard-delete-with-UPA. FK butuh AssessmentPackage valid.
    private static async Task SeedUpaAsync(ApplicationDbContext ctx, int sessionId, string userId)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "Paket UPA", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = sessionId, AssessmentPackageId = pkg.Id, UserId = userId,
            ShuffledQuestionIds = "[]", ShuffledOptionIdsPerQuestion = "{}", AssignedAt = DateTime.UtcNow
        });
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

    // B1 (PRMV-01 soft in-progress preserve): sesi StartedAt set + Score=80 + Status=InProgress + 1 response.
    //     RemoveParticipantLive ASLI → mode="soft"; RemovedAt set NYATA; Score/Status UNCHANGED; response masih ada.
    [Fact]
    public async Task RemoveInProgress_SoftRemoves_PreservesData()
    {
        var title = "Rm-Soft-IP-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, actorId;
        int sessionId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta IP");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00011");
            sessionId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-5), score: 80, isPassed: true);
            await SeedResponseAsync(seed, sessionId);
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.RemoveParticipantLive(sessionId, "tidak hadir");
            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("soft", ModeOf(json));   // berdata → SOFT
        }

        // Assert kolom DB NYATA: removal di-set; Score/Status/IsPassed UNCHANGED; response tetap ada.
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.NotNull(s!.RemovedAt);                          // soft-removed NYATA
        Assert.Equal(actorId, s.RemovedBy);
        Assert.Equal("tidak hadir", s.RemovalReason);
        Assert.Equal(80, s.Score);                             // UNCHANGED (Pitfall 2)
        Assert.Equal(S.InProgress, s.Status);                  // UNCHANGED (guard 409 andalkan RemovedAt, bukan Status)
        Assert.True(s.IsPassed);                               // UNCHANGED
        Assert.True(await verify.PackageUserResponses.AnyAsync(r => r.AssessmentSessionId == sessionId));  // response utuh
    }

    // B2 (PRMV-01 soft certified preserve): sesi Completed + NomorSertifikat + ManualSertifikatUrl + IsPassed.
    //     RemoveParticipantLive ASLI → mode="soft"; cert/score/status UNCHANGED.
    [Fact]
    public async Task RemoveCertified_SoftRemoves_PreservesCert()
    {
        var title = "Rm-Soft-Cert-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddDays(-1);
        string repUser, actorId;
        int sessionId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Cert");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00012");
            sessionId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Completed,
                startedAt: DateTime.UtcNow.AddHours(-2), score: 95, isPassed: true,
                nomorSertifikat: "KPB/X/VI/2026", manualSertifikatUrl: "/certs/x.pdf");
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.RemoveParticipantLive(sessionId, "salah input");
            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("soft", ModeOf(json));
        }

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.NotNull(s!.RemovedAt);                          // soft-removed NYATA
        Assert.Equal("KPB/X/VI/2026", s.NomorSertifikat);      // cert UTUH (Pitfall 2)
        Assert.Equal("/certs/x.pdf", s.ManualSertifikatUrl);   // file cert UTUH
        Assert.True(s.IsPassed);                               // UNCHANGED
        Assert.Equal(S.Completed, s.Status);                   // UNCHANGED
        Assert.Equal(95, s.Score);                             // UNCHANGED
    }

    // B3 (D-02 reason-wajib-soft, PLIV-03): sesi berdata (StartedAt set), reason=null → RemoveParticipantLive ASLI
    //     → 400 "Alasan penghapusan wajib diisi." + 0-write (RemovedAt tetap null).
    [Fact]
    public async Task RemoveSoft_NoReason_Rejected400()
    {
        var title = "Rm-NoReason-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, actorId;
        int sessionId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Berdata");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00013");
            sessionId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-3));
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.RemoveParticipantLive(sessionId, reason: null);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Alasan penghapusan wajib diisi.", JsonSerializer.Serialize(bad.Value));
        }

        // 0-write: RemovedAt tetap null (reason-gate tolak SEBELUM mutasi).
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Null(s!.RemovedAt);
    }

    // B4 (PRMV-01 idempotent write): sesi berdata → remove (soft, panggilan 1) → remove KEDUA → panggilan ke-2
    //     mode="noop"; RemovedAt tetap dari panggilan pertama (tak ter-overwrite).
    [Fact]
    public async Task RemoveInProgress_Idempotent_NoOp()
    {
        var title = "Rm-Idem-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, actorId;
        int sessionId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Idem");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00014");
            sessionId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-4));
        }

        DateTime firstRemovedAt;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var first = await ctrl.RemoveParticipantLive(sessionId, "alasan pertama");
            Assert.Equal("soft", ModeOf(Assert.IsType<JsonResult>(first)));
        }
        await using (var snap = NewCtx())
        {
            firstRemovedAt = (await snap.AssessmentSessions.FindAsync(sessionId))!.RemovedAt!.Value;
        }

        await using (var act2 = NewCtx())
        {
            var actor = await act2.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act2, actor!);

            var second = await ctrl.RemoveParticipantLive(sessionId, "alasan kedua");
            Assert.Equal("noop", ModeOf(Assert.IsType<JsonResult>(second)));   // panggilan ke-2 = noop
        }

        // RemovedAt tetap nilai panggilan PERTAMA (tak ter-overwrite oleh "alasan kedua").
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.Equal(firstRemovedAt, s!.RemovedAt);
        Assert.Equal("alasan pertama", s.RemovalReason);   // reason pertama tak tertimpa
    }

    // B5 (PRMV-04 restore clear): sesi berdata → remove (soft) → RestoreParticipantLive ASLI → restored=true;
    //     RemovedAt/RemovedBy/RemovalReason di-clear NYATA.
    [Fact]
    public async Task Restore_SoftRemoved_ClearsColumns()
    {
        var title = "Rm-Restore-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, actorId;
        int sessionId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Restore");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00015");
            sessionId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-6));
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);
            await ctrl.RemoveParticipantLive(sessionId, "keliru tambah");   // soft-remove dulu
        }

        await using (var act2 = NewCtx())
        {
            var actor = await act2.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act2, actor!, actionName: "RestoreParticipantLive");

            var result = await ctrl.RestoreParticipantLive(sessionId);
            var json = Assert.IsType<JsonResult>(result);
            var raw = JsonSerializer.Serialize(json.Value);
            using var doc = JsonDocument.Parse(raw);
            Assert.True(doc.RootElement.GetProperty("restored").GetBoolean());
        }

        // 3 kolom removal di-clear NYATA.
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.NotNull(s);
        Assert.Null(s!.RemovedAt);
        Assert.Null(s.RemovedBy);
        Assert.Null(s.RemovalReason);
    }

    // B6 (PRMV-05 Pre/Post soft both + batch-isolation): pasangan Pre+Post 1 peserta via LinkedSessionId cross-set.
    //     Pre berdata (StartedAt set), Post bersih. RemoveParticipantLive(preId) ASLI → mode="soft";
    //     KEDUA sesi RemovedAt!=null; linkedSessionId JSON == postId. Peserta LAIN di batch TIDAK ikut removed.
    [Fact]
    public async Task RemovePrePost_OneHasData_SoftBoth()
    {
        var title = "Rm-PrePost-Soft-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var preSched = DateTime.UtcNow.AddHours(7).AddHours(-3);
        var postSched = DateTime.UtcNow.AddHours(7).AddHours(-1);
        const int linkedGroupId = 5511;
        string targetUser, otherUser, actorId;
        int preId, postId, otherPreId;

        await using (var seed = NewCtx())
        {
            targetUser = await SeedUserAsync(seed, "Peserta Target");
            otherUser = await SeedUserAsync(seed, "Peserta Lain");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00016");

            // Pasangan target: Pre (berdata, StartedAt) + Post (bersih), LinkedSessionId cross-set.
            preId = await SeedRepSessionAsync(seed, targetUser, title, cat, preSched, S.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-2), assessmentType: "PreTest", linkedGroupId: linkedGroupId);
            postId = await SeedRepSessionAsync(seed, targetUser, title, cat, postSched, S.Open,
                assessmentType: "PostTest", linkedGroupId: linkedGroupId, linkedSessionId: preId);
            // Cross-link Pre → Post (Post→Pre sudah di-set di atas).
            var pre = await seed.AssessmentSessions.FindAsync(preId);
            pre!.LinkedSessionId = postId;
            await seed.SaveChangesAsync();

            // Peserta LAIN di batch yang sama (Pitfall 1: tak boleh ikut ter-remove).
            otherPreId = await SeedRepSessionAsync(seed, otherUser, title, cat, preSched, S.Open,
                assessmentType: "PreTest", linkedGroupId: linkedGroupId);
        }

        int? jsonLinked;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.RemoveParticipantLive(preId, "tidak hadir post-test");
            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("soft", ModeOf(json));
            jsonLinked = LinkedSessionIdOf(json);
        }

        Assert.Equal(postId, jsonLinked);   // JSON linkedSessionId == partner Post

        // KEDUA sesi target soft-removed; peserta LAIN (otherPreId) TIDAK ter-remove (bukan seluruh batch).
        await using var verify = NewCtx();
        var vPre = await verify.AssessmentSessions.FindAsync(preId);
        var vPost = await verify.AssessmentSessions.FindAsync(postId);
        var vOther = await verify.AssessmentSessions.FindAsync(otherPreId);
        Assert.NotNull(vPre!.RemovedAt);     // Pre soft-removed
        Assert.NotNull(vPost!.RemovedAt);    // Post soft-removed (pair-as-unit)
        Assert.Null(vOther!.RemovedAt);      // peserta LAIN tetap aktif (Pitfall 1)
    }

    // B8 (PLIV-03 audit): sesi berdata → remove (soft) → AuditLogs ada baris ActionType="RemoveParticipantLive".
    [Fact]
    public async Task Remove_WritesAuditRow()
    {
        var title = "Rm-Audit-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, actorId;
        int sessionId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed, "Peserta Audit");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00018");
            sessionId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-7));
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);
            await ctrl.RemoveParticipantLive(sessionId, "alasan audit");
        }

        await using var verify = NewCtx();
        Assert.True(await verify.AuditLogs.AnyAsync(a =>
            a.ActionType == "RemoveParticipantLive" && a.TargetId == sessionId));
    }
}
