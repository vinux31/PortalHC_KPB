// Phase 410 Plan 02 (PART-06 + PART-07) — de-tautology integration untuk endpoint add-participant live.
//
// Mengunci kontrak Plan 410-01 (AddParticipantsLive + GetEligibleParticipantsToAdd) secara de-tautologis
// (lesson 999.12, WAJIB): setiap test MENJALANKAN action AssessmentAdminController ASLI / meng-assert
// kolom DB NYATA — TIDAK ada replica predikat (NO WindowAllowsAddition / NO DeriveReadyStatus tiruan).
//
//   Bagian A (read-path, FlexibleParticipantAddLiveEligibleTests) → InMemory real-controller (pola ParticipantRemovalExcludeTests):
//       GetEligibleParticipantsToAdd ASLI di-invoke → LINQ produksi yang dieksekusi.
//       T1 exclude-by-batch (D-01 aktif + removed), T2 no unit/section (D-02), T3 rep absen (404),
//       T4 idempotency read-side (user di batch tak pernah muncul eligible).
//
//   Bagian B (write-path) → SQLEXPRESS disposable (REUSE FlexibleParticipantAddFixture):
//       AddParticipantsLive ASLI di-drive penuh — stub UserManager minimal (override GetUserAsync) +
//       no-op INotificationService → action menulis ke DB nyata; kita assert kolom NYATA.
//       T5 ready-status, T6 eager-UPA (A1), T7 window-reject 400 + 0-write, T8 Proton-reject 400 + 0-write,
//       T9 Pre/Post pair + LinkedSessionId cross-set (PART-07), T10 idempotent write (skipped[] tak dobel).
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
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

// =====================================================================================================
//  BAGIAN A — READ-PATH (InMemory real-controller; pola ParticipantRemovalExcludeTests)
//  Drive GetEligibleParticipantsToAdd ASLI. GetEligibleParticipantsToAdd TIDAK pakai
//  _userManager/_notificationService → service null AMAN.
// =====================================================================================================
public class FlexibleParticipantAddLiveEligibleTests
{
    // actionName WAJIB di-set: controller meng-override View() yang merujuk ActionDescriptor.ActionName
    // (NRE bila null). GetEligibleParticipantsToAdd return Json(...) (bukan View) → ViewData/Url tetap di-set
    // defensif agar konsisten dengan pola fixture project, tak ada .cshtml di-render.
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
        return (ctrl, ctx);
    }

    // EF InMemory melakukan navigation-join in-memory → baris dengan FK absen di-drop diam-diam.
    // Maka seed ApplicationUser yang cocok untuk tiap UserId. isActive default true (model), set eksplisit utk T-eligible.
    private static ApplicationUser MakeUser(string id, string name, bool isActive = true,
        string? section = null, string? unit = null, string? nip = null) => new ApplicationUser
    {
        Id = id, UserName = id, NormalizedUserName = id.ToUpper(),
        Email = $"{id}@test.local", NormalizedEmail = $"{id}@TEST.LOCAL",
        FullName = name, NIP = nip, IsActive = isActive, Section = section, Unit = unit,
        SecurityStamp = Guid.NewGuid().ToString()
    };

    private static AssessmentSession MakeSession(int id, string userId, string title, string category,
        DateTime schedule, string status, DateTime? removedAt = null) => new AssessmentSession
    {
        Id = id, UserId = userId, Title = title, Category = category, AccessToken = "",
        Schedule = schedule, Status = status, DurationMinutes = 60, PassPercentage = 70,
        RemovedAt = removedAt, CreatedAt = schedule.AddDays(-1)
    };

    // Helper: invoke action ASLI → JsonResult → ekstrak daftar id eligible (JSON round-trip → assert nyata).
    private static async Task<List<string>> EligibleIdsAsync(AssessmentAdminController ctrl, int sessionId)
    {
        var result = await ctrl.GetEligibleParticipantsToAdd(sessionId);
        var json = Assert.IsType<JsonResult>(result);
        // Value = anonymous list { id, fullName, nip }. Serialize → re-parse untuk baca .id tanpa coupling tipe.
        var raw = JsonSerializer.Serialize(json.Value);
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToList();
    }

    // T1 (D-01 exclude sesi APAPUN): user A (sesi aktif) + user B (sesi RemovedAt!=null) + user C (tanpa sesi)
    //     → GetEligibleParticipantsToAdd ASLI hanya berisi C; A DAN B excluded (removed TETAP excluded, hanya balik via Restore 411).
    [Fact]
    public async Task Eligible_ExcludesUsersWithAnySession_IncludingRemoved()
    {
        var (ctrl, ctx) = MakeController("GetEligibleParticipantsToAdd");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        ctx.Users.AddRange(
            MakeUser("u-A", "Aktif", isActive: true),
            MakeUser("u-B", "Dihapus", isActive: true),
            MakeUser("u-C", "Bebas", isActive: true));
        // Batch "BATCH X"/"OJT"/sched: sesi A aktif (RemovedAt null) + sesi B removed (RemovedAt != null).
        ctx.AssessmentSessions.AddRange(
            MakeSession(1, "u-A", "BATCH X", "OJT", sched, S.Open, removedAt: null),
            MakeSession(2, "u-B", "BATCH X", "OJT", sched, S.Open, removedAt: DateTime.UtcNow));
        // user C: IsActive, TANPA sesi di batch → satu-satunya eligible.
        ctx.SaveChanges();

        var eligible = await EligibleIdsAsync(ctrl, sessionId: 1);   // rep = sesi 1

        Assert.Contains("u-C", eligible);          // tanpa sesi → eligible
        Assert.DoesNotContain("u-A", eligible);    // sesi aktif di batch → excluded (idempotency)
        Assert.DoesNotContain("u-B", eligible);    // sesi removed di batch → TETAP excluded (D-01, hanya balik via Restore)
        Assert.Single(eligible);
    }

    // T2 (D-02 no unit/section): user C dengan Section/Unit berbeda dari batch + IsActive + tanpa sesi
    //     → tetap muncul di eligible (TANPA filter unit/section). User non-aktif (D) TIDAK muncul.
    [Fact]
    public async Task Eligible_IgnoresUnitSection_ExcludesInactive()
    {
        var (ctrl, ctx) = MakeController("GetEligibleParticipantsToAdd");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        ctx.Users.AddRange(
            MakeUser("rep-user", "Rep", isActive: true, section: "RFCC", unit: "Unit-1"),
            // C: Section/Unit BEDA dari batch rep → D-02 tetap eligible (tak ada filter unit/section).
            MakeUser("u-other-unit", "Beda Unit", isActive: true, section: "NGP", unit: "Unit-99"),
            // D: IsActive=false → TIDAK eligible (sumber = u.IsActive).
            MakeUser("u-inactive", "Nonaktif", isActive: false, section: "RFCC", unit: "Unit-1"));
        ctx.AssessmentSessions.Add(MakeSession(10, "rep-user", "BATCH Y", "OJT", sched, S.Open));
        ctx.SaveChanges();

        var eligible = await EligibleIdsAsync(ctrl, sessionId: 10);

        Assert.Contains("u-other-unit", eligible);     // unit/section beda → tetap eligible (D-02)
        Assert.DoesNotContain("u-inactive", eligible); // inactive → excluded
        Assert.DoesNotContain("rep-user", eligible);   // sudah punya sesi di batch → excluded
    }

    // T3 (rep absen): sessionId tak ada → NotFound (objek 404).
    [Fact]
    public async Task Eligible_RepNotFound_Returns404()
    {
        var (ctrl, ctx) = MakeController("GetEligibleParticipantsToAdd");
        ctx.Users.Add(MakeUser("u-1", "Satu"));
        ctx.SaveChanges();

        var result = await ctrl.GetEligibleParticipantsToAdd(99999);   // sessionId tak ada

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // T4 (idempotency read-side via eligible): user yang sudah di batch tak pernah muncul eligible — konsisten D-01.
    //     Drive action ASLI; assert user dgn sesi (aktif) absen, user fresh present.
    [Fact]
    public async Task Eligible_AlreadyInBatch_NeverAppears()
    {
        var (ctrl, ctx) = MakeController("GetEligibleParticipantsToAdd");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        ctx.Users.AddRange(
            MakeUser("in-batch", "Sudah Daftar", isActive: true),
            MakeUser("fresh", "Belum Daftar", isActive: true));
        ctx.AssessmentSessions.Add(MakeSession(20, "in-batch", "BATCH Z", "OJT", sched, S.Open));
        ctx.SaveChanges();

        var eligible = await EligibleIdsAsync(ctrl, sessionId: 20);

        Assert.DoesNotContain("in-batch", eligible);   // sudah punya sesi → tak ditawarkan (idempotency read-side)
        Assert.Contains("fresh", eligible);            // sanity: user fresh tetap eligible
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}

// =====================================================================================================
//  BAGIAN B — WRITE-PATH (SQLEXPRESS disposable; REUSE FlexibleParticipantAddFixture)
//  Drive AddParticipantsLive ASLI penuh (Opsi 2a): stub UserManager (override GetUserAsync) + no-op
//  INotificationService → action menulis ke DB NYATA; assert kolom nyata (Status, RemovedAt, UPA, LinkedSessionId).
//  Tidak ada replica predikat — logika produksi (DeriveReadyStatus/window/Proton/idempotency/eager-UPA) DIJALANKAN.
// =====================================================================================================
[Trait("Category", "Integration")]
public class FlexibleParticipantAddLiveWriteTests : IClassFixture<FlexibleParticipantAddFixture>
{
    private readonly FlexibleParticipantAddFixture _fixture;
    public FlexibleParticipantAddLiveWriteTests(FlexibleParticipantAddFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Stub UserManager minimal: hanya GetUserAsync(ClaimsPrincipal) yang dipakai AddParticipantsLive (Langkah 6).
    //      UserManager<T> non-sealed, GetUserAsync virtual → override return actor seeded. Store stub no-op (tak diakses).
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

    // ---- No-op INotificationService: SendAsync return true; sisanya tak diakses oleh AddParticipantsLive.
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

    // Instantiate AssessmentAdminController ASLI atas SQLEXPRESS ctx, dengan stub userManager (actor) + no-op notif.
    private AssessmentAdminController MakeLiveController(ApplicationDbContext ctx, ApplicationUser actor)
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
            hubContext:              new NoopHubContext(),   // Phase 412: AddParticipantsLive broadcast post-commit → hub non-null wajib
            workerDataService:       null!,
            gradingService:          null!,
            protonCompletionService: null!,
            protonBypassService:     null!,
            retakeService:           new RetakeService(ctx, auditLog, new NoopHubContext(), NullLogger<RetakeService>.Instance));
        #pragma warning restore CS8625
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            ActionDescriptor = new ControllerActionDescriptor { ActionName = "AddParticipantsLive" }
        };
        return ctrl;
    }

    // ---- Seed helpers (pola FlexibleParticipantAddTests :63-84) ----
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string? fullName = null, string? nip = null)
    {
        var u = new ApplicationUser
        {
            UserName = "live-" + Guid.NewGuid().ToString("N")[..8], Email = "live@test.local",
            FullName = fullName ?? "Live Add Test", NIP = nip, IsActive = true
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

    // Batch-key untuk hitung sesi (Title + Category + Schedule.Date).
    private static async Task<int> CountBatchSessionsAsync(ApplicationDbContext ctx, string title, string category, DateTime schedule)
        => await ctx.AssessmentSessions.CountAsync(a => a.Title == title && a.Category == category && a.Schedule.Date == schedule.Date);

    // T5 (ready-status, PART-06): add user baru ke batch standard (schedule lampau) → AddParticipantsLive ASLI →
    //     sesi baru Status=="Open" (DeriveReadyStatus), StartedAt/CompletedAt/RemovedAt NULL (NEVER InProgress).
    [Fact]
    public async Task AddParticipantsLive_NewSession_HasReadyStatus_RemovalNull()
    {
        var title = "Live-Ready-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);   // schedule lampau (WIB) → Open
        string repUser, newUser, actorId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed);
            newUser = await SeedUserAsync(seed, "Peserta Baru", "12345");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00001");
            await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Open);
        }

        int newSessionId;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);
            var repId = await act.AssessmentSessions.Where(a => a.Title == title).Select(a => a.Id).FirstAsync();

            var result = await ctrl.AddParticipantsLive(repId, new List<string> { newUser });

            var json = Assert.IsType<JsonResult>(result);   // sukses → JSON added[]/skipped[]
            newSessionId = await act.AssessmentSessions
                .Where(a => a.Title == title && a.UserId == newUser).Select(a => a.Id).FirstAsync();
        }

        // Assert kolom DB NYATA yang DIHASILKAN produksi (bukan replica).
        await using var verify = NewCtx();
        var added = await verify.AssessmentSessions.FindAsync(newSessionId);
        Assert.NotNull(added);
        Assert.Equal(S.Open, added!.Status);            // DeriveReadyStatus → Open (schedule lampau)
        Assert.NotEqual(S.InProgress, added.Status);    // NEVER InProgress (PART-06)
        Assert.Null(added.StartedAt);
        Assert.Null(added.CompletedAt);
        Assert.Null(added.RemovedAt);
    }

    // T6 (eager UPA, A1): batch dengan ≥1 AssessmentPackage+Questions+Options → setelah AddParticipantsLive ASLI,
    //     UserPackageAssignment tercipta EAGER (AssessmentSessionId == sesi baru, ShuffledQuestionIds != "[]").
    [Fact]
    public async Task AddParticipantsLive_WithPackages_CreatesEagerUserPackageAssignment()
    {
        var title = "Live-UPA-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, newUser, actorId;
        int repId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed);
            newUser = await SeedUserAsync(seed, "Peserta UPA", "67890");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00002");
            repId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Open);

            // Batch punya AssessmentPackage + Questions + Options → CreateEagerAssignmentsAsync membuat UPA.
            var pkg = new AssessmentPackage { AssessmentSessionId = repId, PackageName = "Paket A", PackageNumber = 1 };
            seed.AssessmentPackages.Add(pkg);
            await seed.SaveChangesAsync();
            for (int i = 0; i < 3; i++)
            {
                var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionText = $"Soal {i}", Order = i, ScoreValue = 10, QuestionType = "MultipleChoice" };
                seed.PackageQuestions.Add(q);
                await seed.SaveChangesAsync();
                seed.PackageOptions.AddRange(
                    new PackageOption { PackageQuestionId = q.Id, OptionText = "A", IsCorrect = true },
                    new PackageOption { PackageQuestionId = q.Id, OptionText = "B", IsCorrect = false });
            }
            await seed.SaveChangesAsync();
        }

        int newSessionId;
        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.AddParticipantsLive(repId, new List<string> { newUser });
            Assert.IsType<JsonResult>(result);

            newSessionId = await act.AssessmentSessions
                .Where(a => a.Title == title && a.UserId == newUser).Select(a => a.Id).FirstAsync();
        }

        // Assert UPA EAGER tercipta (A1) — kolom NYATA dari produksi CreateEagerAssignmentsAsync.
        await using var verify = NewCtx();
        var upa = await verify.UserPackageAssignments
            .FirstOrDefaultAsync(a => a.AssessmentSessionId == newSessionId);
        Assert.NotNull(upa);                                 // UPA dibuat EAGER (bukan lazy)
        Assert.Equal(newUser, upa!.UserId);
        Assert.NotEqual("[]", upa.ShuffledQuestionIds);      // ada soal ter-assign
        var qids = JsonSerializer.Deserialize<List<int>>(upa.ShuffledQuestionIds)!;
        Assert.Equal(3, qids.Count);                         // 3 soal di-shuffle masuk assignment
    }

    // T7 (window reject, PART-06): rep.ExamWindowCloseDate = kemarin → AddParticipantsLive ASLI → 400
    //     "Window ujian sudah tutup, tidak bisa tambah peserta." + 0 sesi baru (count batch tak bertambah).
    [Fact]
    public async Task AddParticipantsLive_WindowClosed_Returns400_NoWrite()
    {
        var title = "Live-Window-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddDays(-2);
        var windowClosed = DateTime.UtcNow.AddHours(7).AddDays(-1);   // window sudah lewat (kemarin WIB)
        string repUser, newUser, actorId;
        int repId, before;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed);
            newUser = await SeedUserAsync(seed, "Peserta Telat");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00003");
            repId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Open, examWindowCloseDate: windowClosed);
            before = await CountBatchSessionsAsync(seed, title, cat, schedule);
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.AddParticipantsLive(repId, new List<string> { newUser });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            // Pesan window-tolak LOCKED (verbatim).
            Assert.Contains("Window ujian sudah tutup, tidak bisa tambah peserta.", JsonSerializer.Serialize(bad.Value));
        }

        // 0-write: count batch tak bertambah (sesi baru TIDAK tercipta).
        await using var verify = NewCtx();
        var after = await CountBatchSessionsAsync(verify, title, cat, schedule);
        Assert.Equal(before, after);
        Assert.False(await verify.AssessmentSessions.AnyAsync(a => a.Title == title && a.UserId == newUser));
    }

    // T8 (Proton reject): rep.Category=="Assessment Proton" → AddParticipantsLive ASLI → 400 + 0 write.
    [Fact]
    public async Task AddParticipantsLive_Proton_Returns400_NoWrite()
    {
        var title = "Live-Proton-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "Assessment Proton";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string repUser, newUser, actorId;
        int repId, before;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed);
            newUser = await SeedUserAsync(seed, "Peserta Proton");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00004");
            repId = await SeedRepSessionAsync(seed, repUser, title, cat, schedule, S.Open);
            before = await CountBatchSessionsAsync(seed, title, cat, schedule);
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            var result = await ctrl.AddParticipantsLive(repId, new List<string> { newUser });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Assessment Proton", JsonSerializer.Serialize(bad.Value));
        }

        await using var verify = NewCtx();
        var after = await CountBatchSessionsAsync(verify, title, cat, schedule);
        Assert.Equal(before, after);
        Assert.False(await verify.AssessmentSessions.AnyAsync(a => a.Title == title && a.UserId == newUser));
    }

    // T9 (Pre/Post pair, PART-07 + WR-01 regression guard): batch Pre/Post realistis — existing PreTest **dan**
    //     PostTest sibling dengan PostSchedule LEBIH LAMBAT + window/duration/cert DISTINCT. Caller passing PreTest
    //     sebagai rep → AddParticipantsLive ASLI → 2 sesi baru. WR-01: newPost WAJIB inherit config sesi POST
    //     (Schedule/window/duration/cert), BUKAN Pre's; newPre WAJIB cert=false. Tanpa fix WR-01 (single-rep), newPost
    //     ikut config Pre → test ini GAGAL (regression guard). + LinkedSessionId cross-set, keduanya ready-status.
    [Fact]
    public async Task AddParticipantsLive_PrePost_CreatesPair_WithCrossLink()
    {
        var title = "Live-PrePost-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        // Schedule keduanya lampau (WIB) → DeriveReadyStatus = Open. Post LEBIH LAMBAT dari Pre (cermin PostSchedule > PreSchedule).
        var preSched  = DateTime.UtcNow.AddHours(7).AddHours(-3);            // Pre lebih awal
        var postSched = DateTime.UtcNow.AddHours(7).AddHours(-1);            // Post lebih lambat (tanggal beda dari Pre bila lewat tengah malam — pakai .Date assert)
        // Window DISTINCT antar Pre/Post: keduanya masih terbuka (di masa depan) tapi tanggal beda → assert tak ketukar.
        var preWindow  = DateTime.UtcNow.AddHours(7).AddDays(3);
        var postWindow = DateTime.UtcNow.AddHours(7).AddDays(10);           // window Post jauh lebih lama
        const int preDuration = 45, postDuration = 90;                      // durasi DISTINCT
        const int linkedGroupId = 777;
        string repUser, newUser, actorId;
        int preRepId;

        await using (var seed = NewCtx())
        {
            repUser = await SeedUserAsync(seed);
            newUser = await SeedUserAsync(seed, "Peserta PrePost");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00005");
            // Existing batch Pre/Post peserta lama: PreTest (cert=false) + PostTest sibling (cert=true, schedule/window/durasi beda).
            preRepId = await SeedRepSessionAsync(seed, repUser, title, cat, preSched, S.Open,
                examWindowCloseDate: preWindow, assessmentType: "PreTest", linkedGroupId: linkedGroupId,
                generateCertificate: false, durationMinutes: preDuration);
            await SeedRepSessionAsync(seed, repUser, title, cat, postSched, S.Open,
                examWindowCloseDate: postWindow, assessmentType: "PostTest", linkedGroupId: linkedGroupId,
                generateCertificate: true, durationMinutes: postDuration);
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            // Caller passing PreTest sebagai rep (kasus paling mungkin — monitoring surface rep).
            var result = await ctrl.AddParticipantsLive(preRepId, new List<string> { newUser });
            Assert.IsType<JsonResult>(result);
        }

        // Assert PAIR nyata di DB: user baru punya 1 PreTest + 1 PostTest, LinkedSessionId cross-set, ready-status.
        await using var verify = NewCtx();
        var userSessions = await verify.AssessmentSessions
            .Where(a => a.UserId == newUser && a.Title == title)
            .ToListAsync();
        Assert.Equal(2, userSessions.Count);                                 // pasangan Pre + Post
        var newPre = userSessions.Single(s => s.AssessmentType == "PreTest");
        var newPost = userSessions.Single(s => s.AssessmentType == "PostTest");
        Assert.Equal(newPost.Id, newPre.LinkedSessionId);                    // cross-link Pre → Post
        Assert.Equal(newPre.Id, newPost.LinkedSessionId);                    // cross-link Post → Pre
        Assert.Equal(linkedGroupId, newPre.LinkedGroupId);
        Assert.Equal(linkedGroupId, newPost.LinkedGroupId);

        // === WR-01 REGRESSION GUARD: newPost WAJIB inherit config sesi POST, BUKAN Pre's. ===
        Assert.Equal(postSched.Date, newPost.Schedule.Date);                // Post pakai jadwal Post, bukan Pre
        Assert.Equal(preSched.Date,  newPre.Schedule.Date);                 // Pre pakai jadwal Pre
        Assert.Equal(postWindow,     newPost.ExamWindowCloseDate);          // Post pakai window Post (distinct)
        Assert.Equal(preWindow,      newPre.ExamWindowCloseDate);           // Pre pakai window Pre
        Assert.Equal(postDuration,   newPost.DurationMinutes);              // Post pakai durasi Post
        Assert.Equal(preDuration,    newPre.DurationMinutes);               // Pre pakai durasi Pre
        Assert.True(newPost.GenerateCertificate);                           // Post inherit cert=true dari repPost
        Assert.False(newPre.GenerateCertificate);                          // Pre WAJIB cert=false (analog :1963)

        // Ready-status (Open, schedule lampau) — NEVER InProgress (PART-06).
        Assert.Equal(S.Open, newPre.Status);
        Assert.Equal(S.Open, newPost.Status);
        Assert.NotEqual(S.InProgress, newPre.Status);
        Assert.NotEqual(S.InProgress, newPost.Status);
    }

    // T10 (idempotent write, PART-06): user dengan sesi APAPUN di batch dalam userIds → masuk skipped[], tak dobel-create.
    //     Drive AddParticipantsLive ASLI dgn mix [existingUser, freshUser] → existing skipped, fresh added 1×.
    [Fact]
    public async Task AddParticipantsLive_ExistingUser_Skipped_NoDuplicate()
    {
        var title = "Live-Idem-" + Guid.NewGuid().ToString("N")[..8];
        const string cat = "OJT";
        var schedule = DateTime.UtcNow.AddHours(7).AddHours(-1);
        string existingUser, freshUser, actorId;
        int repId;

        await using (var seed = NewCtx())
        {
            existingUser = await SeedUserAsync(seed, "Sudah Daftar");
            freshUser = await SeedUserAsync(seed, "Peserta Fresh");
            actorId = await SeedUserAsync(seed, "Admin Actor", "00006");
            repId = await SeedRepSessionAsync(seed, existingUser, title, cat, schedule, S.Open);  // existingUser sudah di batch
        }

        await using (var act = NewCtx())
        {
            var actor = await act.Users.FindAsync(actorId);
            var ctrl = MakeLiveController(act, actor!);

            // userIds = [existing (sudah di batch), fresh (baru)].
            var result = await ctrl.AddParticipantsLive(repId, new List<string> { existingUser, freshUser });
            var json = Assert.IsType<JsonResult>(result);
            var payload = JsonSerializer.Serialize(json.Value);
            using var doc = JsonDocument.Parse(payload);
            Assert.Equal(1, doc.RootElement.GetProperty("addedCount").GetInt32());     // fresh saja ditambah
            Assert.Equal(1, doc.RootElement.GetProperty("skippedCount").GetInt32());   // existing dilewati
        }

        // existingUser TIDAK dobel-create (tetap 1 sesi); freshUser dapat 1 sesi baru.
        await using var verify = NewCtx();
        Assert.Equal(1, await verify.AssessmentSessions.CountAsync(a => a.Title == title && a.UserId == existingUser));
        Assert.Equal(1, await verify.AssessmentSessions.CountAsync(a => a.Title == title && a.UserId == freshUser));
    }
}
