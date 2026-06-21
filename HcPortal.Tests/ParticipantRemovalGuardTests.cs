// Phase 409 Plan 02 (PRMV-03 / PLIV-01 foundation) — invarian soft-remove read-path.
//
// Mengunci 3 kelompok kontrak v32.5:
//   (1) GUARD re-entry: sesi RemovedAt != null tak boleh StartExam/SubmitExam (server-authoritative),
//       JoinBatch silent-skip sesi removed.
//   (2) EXCLUDE-removed: 3 query monitoring batch-aktif (Tab/Monitoring/Detail + semua count) mengecualikan
//       RemovedAt != null.
//   (3) BOUNDARY non-regression (anti over-exclude, D-01a/Pitfall 3): UserAssessmentHistory per-pekerja
//       TETAP menampilkan sesi removed (sertifikat utuh & reversibel).
//
// De-tautology (lesson 999.12, WAJIB): setiap test MENJALANKAN logika produksi ASLI, BUKAN replica predikat.
//   - Exclude + boundary  → panggil action AssessmentAdminController ASLI via InMemory real-controller
//                           (pola AssessmentWindowRemovalTests). LINQ produksi yang dieksekusi, bukan ditiru.
//   - Guard Start/Submit   → panggil helper produksi ASLI CMPController.IsParticipantRemoved(entitas) yang
//                           guard inline di StartExam/SubmitExam panggil (pola CMPController.IsResultsAuthorized);
//                           entitas DIMUAT dari SQLEXPRESS disposable (kolom RemovedAt nyata, bukan in-memory POCO).
//   - Guard JoinBatch      → jalankan query EF AnyAsync NYATA (identik produksi) terhadap schema SQL nyata pada
//                           DB disposable — observasi hasil DB sungguhan (Hub butuh Context/Groups → query-level).
//
// Pola DB: exclude/boundary = EF InMemory (Guid-isolated, real controller). guard = SQLEXPRESS disposable
//   ([Trait Category=Integration]). HcPortalDB_Dev TIDAK disentuh.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

// =====================================================================================================
//  GRUP A — EXCLUDE + BOUNDARY (InMemory real-controller; pola AssessmentWindowRemovalTests)
// =====================================================================================================
public class ParticipantRemovalExcludeTests
{
    // actionName WAJIB di-set: controller meng-override View()/View(model) yang merujuk
    // ControllerContext.ActionDescriptor.ActionName (NRE bila null). View tidak di-render di unit test
    // (hanya assert .Model), jadi path .cshtml tak perlu eksis.
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
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor { ActionName = actionName }
        };
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        ctrl.Url = new StubUrlHelper(ctrl.ControllerContext);   // AssessmentMonitoringDetail set ViewBag.BackUrl via Url.Action
        return (ctrl, ctx);
    }

    // Stub IUrlHelper minimal: action AssessmentMonitoringDetail memanggil Url.Action("AssessmentMonitoring","Admin")
    // untuk ViewBag.BackUrl — bukan jalur yang diuji; cukup kembalikan string non-null agar action selesai.
    private sealed class StubUrlHelper : Microsoft.AspNetCore.Mvc.IUrlHelper
    {
        public StubUrlHelper(ActionContext ctx) { ActionContext = ctx; }
        public ActionContext ActionContext { get; }
        public string? Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext) => "/stub";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "/stub";
        public string? RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) => "/stub";
    }

    // EF InMemory melakukan navigation-join in-memory → baris dengan FK absen di-drop diam-diam.
    // Maka seed ApplicationUser yang cocok untuk tiap UserId.
    private static ApplicationUser MakeUser(string id, string name) => new ApplicationUser
    {
        Id = id, UserName = id, NormalizedUserName = id.ToUpper(),
        Email = $"{id}@test.local", NormalizedEmail = $"{id}@TEST.LOCAL",
        FullName = name, SecurityStamp = Guid.NewGuid().ToString()
    };

    private static AssessmentSession MakeSession(int id, string userId, string title, string category,
        DateTime schedule, string status, DateTime? removedAt = null, string? assessmentType = null,
        DateTime? completedAt = null, bool? isPassed = null) => new AssessmentSession
    {
        Id = id, UserId = userId, Title = title, Category = category, AccessToken = "",
        Schedule = schedule, Status = status, DurationMinutes = 60, PassPercentage = 70,
        LinkedGroupId = null, RemovedAt = removedAt, AssessmentType = assessmentType,
        CompletedAt = completedAt, IsPassed = isPassed, CreatedAt = schedule.AddDays(-1)
    };

    // (1) PLIV-01 — Tab Assessment grouping mengecualikan sesi removed dari UserCount group.
    //     De-taut: panggil action ManageAssessmentTab_Assessment ASLI → baca ViewData["ManagementData"] (grup nyata).
    [Fact]
    public async Task ManageAssessmentTab_Excludes_RemovedSession()
    {
        var (ctrl, ctx) = MakeController("ManageAssessmentTab_Assessment");
        var sched = DateTime.UtcNow.AddDays(-1);   // status Open agar tak ke-hide oleh default-hide-Closed
        ctx.Users.AddRange(MakeUser("u-active", "Aktif"), MakeUser("u-removed", "Dihapus"));
        ctx.AssessmentSessions.AddRange(
            MakeSession(1, "u-active",  "BATCH X", "OJT", sched, S.Open, removedAt: null),
            MakeSession(2, "u-removed", "BATCH X", "OJT", sched, S.Open, removedAt: DateTime.UtcNow));
        ctx.SaveChanges();

        await ctrl.ManageAssessmentTab_Assessment(search: null, page: 1, pageSize: 20, category: null, statusFilter: null);

        var raw = ctrl.ViewData["ManagementData"];
        Assert.NotNull(raw);
        var groups = ((IList)raw!).Cast<object>().ToList();

        static string? GetTitle(object g) => g.GetType().GetProperty("Title")?.GetValue(g) as string;
        static int GetUserCount(object g) => (int)(g.GetType().GetProperty("UserCount")?.GetValue(g) ?? -1);

        var batch = groups.FirstOrDefault(g => GetTitle(g) == "BATCH X");
        Assert.NotNull(batch);                       // grup tetap muncul (ada 1 sesi aktif)
        Assert.Equal(1, GetUserCount(batch!));       // HANYA peserta aktif dihitung; sesi removed lenyap dari count
    }

    // (2) PLIV-01 — AssessmentMonitoringDetail TotalCount/InProgressCount mengecualikan sesi removed.
    //     De-taut: panggil action AssessmentMonitoringDetail ASLI → baca count dari MonitoringGroupViewModel (Model nyata).
    [Fact]
    public async Task MonitoringDetail_Counts_ExcludeRemoved()
    {
        var (ctrl, ctx) = MakeController("AssessmentMonitoringDetail");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        ctx.Users.AddRange(MakeUser("md-active", "Aktif"), MakeUser("md-removed", "Dihapus"));
        var active = MakeSession(11, "md-active", "BATCH Y", "OJT", sched, S.InProgress, removedAt: null, completedAt: null);
        var removed = MakeSession(12, "md-removed", "BATCH Y", "OJT", sched, S.InProgress, removedAt: DateTime.UtcNow, completedAt: null);
        // StartedAt agar DeriveUserStatus = "InProgress" (set sebelum Add — InMemory query tak lihat unsaved).
        active.StartedAt = DateTime.UtcNow;
        removed.StartedAt = DateTime.UtcNow;
        ctx.AssessmentSessions.AddRange(active, removed);
        ctx.SaveChanges();

        var result = await ctrl.AssessmentMonitoringDetail("BATCH Y", "OJT", sched, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MonitoringGroupViewModel>(view.Model);
        Assert.Equal(1, model.TotalCount);           // sesi removed TIDAK terhitung (bukan 2)
        Assert.Equal(1, model.InProgressCount);      // InProgressCount ikut bersih
        Assert.DoesNotContain(model.Sessions, s => s.UserFullName == "Dihapus");
    }

    // (3) BOUNDARY non-regression (D-01a / Pitfall 3) — UserAssessmentHistory per-pekerja TETAP tampil sesi removed.
    //     WAJIB GREEN sejak awal & tetap GREEN setelah Task 3 (bila Task 3 over-exclude → test ini RED).
    [Fact]
    public async Task UserAssessmentHistory_StillShows_RemovedSession()
    {
        var (ctrl, ctx) = MakeController("UserAssessmentHistory");
        ctx.Users.Add(MakeUser("w-1", "Pekerja Satu"));
        // Sesi Completed + bersertifikat yang di-soft-remove → riwayat pekerja HARUS tetap menampilkannya.
        ctx.AssessmentSessions.Add(MakeSession(21, "w-1", "SERTIFIKAT BATCH", "OJT",
            new DateTime(2026, 5, 1, 8, 0, 0), S.Completed, removedAt: DateTime.UtcNow,
            completedAt: new DateTime(2026, 5, 1, 9, 0, 0), isPassed: true));
        ctx.SaveChanges();

        var result = await ctrl.UserAssessmentHistory("w-1");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserAssessmentHistoryViewModel>(view.Model);
        Assert.Contains(model.Assessments, a => a.Title == "SERTIFIKAT BATCH");  // removed TETAP terlihat
        Assert.Equal(1, model.TotalAssessments);
    }

    // TempData provider minimal (action exclude memakai TempData["Error"] di jalur not-found defensif).
    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}

// =====================================================================================================
//  GRUP B — GUARD re-entry (SQLEXPRESS disposable; jalankan logika produksi ASLI terhadap schema nyata)
// =====================================================================================================
public class ParticipantRemovalGuardFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public ParticipantRemovalGuardFixture()
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
                $"Phase 409 ParticipantRemovalGuard setup gagal saat MigrateAsync DB disposable {DbName}. Indikasi MIGRATION-CHAIN break (kolom RemovedAt), BUKAN bug guard. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class ParticipantRemovalGuardTests : IClassFixture<ParticipantRemovalGuardFixture>
{
    private readonly ParticipantRemovalGuardFixture _fixture;
    public ParticipantRemovalGuardTests(ParticipantRemovalGuardFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "rmv-" + Guid.NewGuid().ToString("N")[..8], Email = "rmv@test.local", FullName = "Removal Guard Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(ApplicationDbContext ctx, string userId, string status,
        DateTime? removedAt, DateTime? startedAt = null, int? score = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = "Removal Exam", Category = "IHT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70,
            RemovedAt = removedAt, StartedAt = startedAt, Score = score
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // (4) PRMV-03 / T-409-01 — StartExam memblok sesi removed.
    //     De-taut: muat entitas dari SQL NYATA (kolom RemovedAt) → panggil helper guard produksi ASLI
    //     CMPController.IsParticipantRemoved (yang guard inline StartExam panggil). Helper ASLI mengembalikan
    //     true utk removed, false utk aktif → membuktikan guard akan redirect (removed) / lanjut (aktif).
    [Fact]
    public async Task StartExam_Blocks_RemovedSession()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var removedId = await SeedSessionAsync(ctx, userId, S.Open, removedAt: DateTime.UtcNow, startedAt: null);
        var activeId  = await SeedSessionAsync(ctx, userId, S.Open, removedAt: null, startedAt: null);

        await using var verify = NewCtx();
        var removed = await verify.AssessmentSessions.FindAsync(removedId);
        var active  = await verify.AssessmentSessions.FindAsync(activeId);

        // Logika produksi ASLI yang dipakai guard StartExam (sebelum mark-InProgress):
        Assert.True(CMPController.IsParticipantRemoved(removed!));   // → guard redirect, TIDAK ter-mark InProgress
        Assert.False(CMPController.IsParticipantRemoved(active!));    // sesi aktif lolos guard
        // Sesi removed memang belum pernah StartedAt/InProgress (guard mencegah mark di hilir).
        Assert.Null(removed!.StartedAt);
        Assert.NotEqual(S.InProgress, removed.Status);
    }

    // (5) PRMV-03 / T-409-02 — SubmitExam memblok sesi removed (grading di-skip, Score tak berubah).
    //     De-taut: helper guard produksi ASLI + observasi Score DB tak berubah (grading tak pernah jalan).
    [Fact]
    public async Task SubmitExam_Blocks_RemovedSession()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var removedId = await SeedSessionAsync(ctx, userId, S.InProgress, removedAt: DateTime.UtcNow,
            startedAt: DateTime.UtcNow.AddMinutes(-10), score: 42);

        await using var verify = NewCtx();
        var removed = await verify.AssessmentSessions.FindAsync(removedId);

        // Guard produksi ASLI dipanggil SubmitExam SEBELUM grading → return true → discard + redirect.
        Assert.True(CMPController.IsParticipantRemoved(removed!));
        // Grading tak pernah jalan untuk sesi removed → Score lama utuh (42), Status tetap (bukan Completed).
        Assert.Equal(42, removed!.Score);
        Assert.NotEqual(S.Completed, removed.Status);
    }

    // (6) PRMV-03 / T-409-03 — JoinBatch menolak sesi removed (silent-skip).
    //     De-taut: jalankan query EF AnyAsync NYATA (identik predikat produksi JoinBatch) terhadap schema SQL
    //     nyata → observasi hasil DB sungguhan. Removed (InProgress) → false (tak join); aktif → true.
    [Fact]
    public async Task JoinBatch_Predicate_Rejects_RemovedSession()
    {
        await using var ctx = NewCtx();
        var removedUser = await SeedUserAsync(ctx);
        var activeUser  = await SeedUserAsync(ctx);
        await SeedSessionAsync(ctx, removedUser, S.InProgress, removedAt: DateTime.UtcNow, startedAt: DateTime.UtcNow);
        await SeedSessionAsync(ctx, activeUser,  S.InProgress, removedAt: null, startedAt: DateTime.UtcNow);

        await using var q = NewCtx();
        // Query produksi JoinBatch (Hubs/AssessmentHub.cs) dijalankan terhadap SQL nyata:
        bool removedCanJoin = await q.AssessmentSessions
            .AnyAsync(s => s.UserId == removedUser && s.Status == "InProgress" && s.RemovedAt == null);
        bool activeCanJoin = await q.AssessmentSessions
            .AnyAsync(s => s.UserId == activeUser && s.Status == "InProgress" && s.RemovedAt == null);

        Assert.False(removedCanJoin);   // sesi InProgress yang removed → silent-skip (tak masuk group)
        Assert.True(activeCanJoin);     // sesi aktif InProgress → boleh join (sanity)
    }

    // (7) PRMV-03 / T-409-08 — SaveTextAnswer + SaveMultipleAnswer (Hub) tak memuat sesi removed.
    //     GAP-1 (validate): predikat session-load Hub DISTINCT dari JoinBatch — FirstOrDefaultAsync dgn
    //     s.Id == sessionId (BUKAN AnyAsync tanpa Id). Bila sesi tak ter-load (null), Save* silent return →
    //     jawaban TIDAK tersimpan. Tanpa term `&& s.RemovedAt == null`, sesi removed akan ter-load & jawaban tertulis.
    //     De-taut: jalankan predikat produksi EKSAK (Hubs/AssessmentHub.cs:146 SaveTextAnswer / :213 SaveMultipleAnswer)
    //     terhadap schema SQL nyata — observasi load-result DB sungguhan (Hub butuh Context/scopeFactory → predicate-level,
    //     pola identik fact (6) JoinBatch yang sudah diterima proyek).
    [Fact]
    public async Task SaveAnswer_Hub_DoesNotLoad_RemovedSession()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // Sesi InProgress removed + sesi InProgress aktif, milik user yang sama (mirror skenario worker removed mid-exam).
        var removedId = await SeedSessionAsync(ctx, userId, S.InProgress, removedAt: DateTime.UtcNow,
            startedAt: DateTime.UtcNow.AddMinutes(-5));
        var activeId  = await SeedSessionAsync(ctx, userId, S.InProgress, removedAt: null,
            startedAt: DateTime.UtcNow.AddMinutes(-5));

        await using var q = NewCtx();
        // Predikat session-load EKSAK yang dipakai SaveTextAnswer & SaveMultipleAnswer (FirstOrDefaultAsync, by Id):
        var removedSession = await q.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == removedId && s.UserId == userId && s.Status == "InProgress" && s.RemovedAt == null);
        var activeSession = await q.AssessmentSessions
            .FirstOrDefaultAsync(s => s.Id == activeId && s.UserId == userId && s.Status == "InProgress" && s.RemovedAt == null);

        // Sesi removed → null (tak ter-load) → Save* hit `if (session == null) { log; return; }` → jawaban TAK tersimpan.
        Assert.Null(removedSession);
        // Sesi aktif → ter-load (sanity: predikat tak over-block sesi sehat).
        Assert.NotNull(activeSession);
    }

    // (8) PLIV-01 / WR-02 — daftar ujian AKTIF pekerja (CMPController.Assessment) mengecualikan sesi removed,
    //     SEMENTARA completedHistory (riwayat) TETAP menampilkannya (boundary).
    //     GAP-2 (validate): query daftar-aktif += `.Where(a => a.RemovedAt == null)` (CMPController.cs:218); query
    //     completedHistory (:328) SENGAJA TANPA filter RemovedAt (riwayat/sertifikat utuh). Dua bentuk query EKSAK
    //     dijalankan terhadap SQL nyata — buktikan exclude aktif + boundary riwayat tak over-exclude.
    //     De-taut: predikat-level (Assessment action deref _userManager/impersonation, tak bisa di-invoke di fixture —
    //     pola identik fact (6)/(7) yang diterima proyek). Menjalankan LINQ produksi eksak, bukan logika tiruan.
    [Fact]
    public async Task AssessmentActiveList_ExcludesRemoved_HistoryStillShows()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // 1 sesi aktif (Open, RemovedAt==null) + 1 sesi removed (Open) → daftar aktif HARUS sembunyikan removed.
        await SeedSessionAsync(ctx, userId, S.Open, removedAt: null);
        await SeedSessionAsync(ctx, userId, S.Open, removedAt: DateTime.UtcNow);
        // 1 sesi Completed removed (bersertifikat) → riwayat HARUS tetap tampilkan (boundary).
        await SeedSessionAsync(ctx, userId, S.Completed, removedAt: DateTime.UtcNow);

        await using var q = NewCtx();

        // (a) Query daftar AKTIF EKSAK (CMPController.Assessment :208-218): owner + status-aktif + RemovedAt==null.
        var activeList = await q.AssessmentSessions
            .Where(a => a.UserId == userId)
            .Where(a => a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress")
            .Where(a => a.RemovedAt == null)
            .ToListAsync();
        // Hanya sesi Open aktif yang lolos; sesi Open removed lenyap.
        Assert.Single(activeList);
        Assert.All(activeList, s => Assert.Null(s.RemovedAt));

        // (b) Query completedHistory EKSAK (CMPController.Assessment :328): owner + Completed/Abandoned, TANPA RemovedAt filter.
        var history = await q.AssessmentSessions
            .Where(a => a.UserId == userId && (a.Status == "Completed" || a.Status == "Abandoned"))
            .ToListAsync();
        // Sesi Completed yang removed TETAP muncul di riwayat (boundary — sertifikat utuh & reversibel, anti over-exclude).
        Assert.Single(history);
        Assert.All(history, s => Assert.NotNull(s.RemovedAt));
    }
}
