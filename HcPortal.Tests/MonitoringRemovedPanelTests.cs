// Phase 412 Nyquist validation — assertable gap: ViewBag.RemovedSessions query di AssessmentMonitoringDetail.
//
// PLIV-01 (Phase 412 Plan 01): action AssessmentMonitoringDetail mengquery sesi dengan RemovedAt != null
// (KEBALIKAN exclude-query Phase 409) dan memaparkannya ke ViewBag.RemovedSessions (List<RemovedParticipantViewModel>).
// ViewModel memakai:
//   - FullName  = s.User?.FullName ?? "Unknown"     (dari .Include(a => a.User) pada query removed)
//   - RemovedByName = resolve dari removerMap        (users yang ID-nya ada di RemovedBy)
//
// Gap ini BELUM ter-cover oleh suite existing (MonitoringDetail_Counts_ExcludeRemoved di ParticipantRemovalExcludeTests
// hanya mengecek TotalCount/InProgressCount dari model query utama [RemovedAt==null] — BUKAN ViewBag.RemovedSessions).
//
// De-tautology (lesson 999.12): setiap test menjalankan controller action ASLI + LINQ EF produksi terhadap
// InMemory real context. Tidak ada replica predikat. Assert ViewBag yang NYATA di-set oleh action.
//
// Pola: identik ParticipantRemovalExcludeTests (ParticipantRemovalGuardTests.cs) — InMemory, StubUrlHelper,
// MakeUser dengan User terseed (navigation join InMemory silently drop baris tanpa User seed).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
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

/// <summary>
/// PLIV-01 (Phase 412): ViewBag.RemovedSessions query di AssessmentMonitoringDetail.
/// Gap assertable: sesi RemovedAt!=null masuk panel (FullName + RemovedByName resolved).
/// </summary>
public class MonitoringRemovedPanelTests
{
    // actionName WAJIB di-set: controller override View(object) merujuk ActionDescriptor.ActionName.
    // Pola identik ParticipantRemovalExcludeTests (ParticipantRemovalGuardTests.cs:51-84).
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
            hubContext:              null!,    // read-only action; broadcast tak terjadi di GET
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
        // AssessmentMonitoringDetail memanggil Url.Action("AssessmentMonitoring","Admin") untuk ViewBag.BackUrl —
        // cukup kembalikan string non-null agar action selesai (bukan jalur yang diuji).
        ctrl.Url = new StubUrlHelper(ctrl.ControllerContext);
        return (ctrl, ctx);
    }

    // Stub IUrlHelper minimal — identik ParticipantRemovalExcludeTests.StubUrlHelper.
    private sealed class StubUrlHelper : IUrlHelper
    {
        public StubUrlHelper(ActionContext ctx) { ActionContext = ctx; }
        public ActionContext ActionContext { get; }
        public string? Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext) => "/stub";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "/stub";
        public string? RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) => "/stub";
    }

    // InMemory navigation join: seed User yang cocok agar .Include(a => a.User) tak drop baris diam-diam.
    private static ApplicationUser MakeUser(string id, string name, string? nip = null) => new ApplicationUser
    {
        Id = id, UserName = id, NormalizedUserName = id.ToUpper(),
        Email = $"{id}@test.local", NormalizedEmail = $"{id}@TEST.LOCAL",
        FullName = name, NIP = nip, IsActive = true, SecurityStamp = Guid.NewGuid().ToString()
    };

    private static AssessmentSession MakeSession(int id, string userId, string title, string category,
        DateTime schedule, string status, DateTime? removedAt = null,
        string? removedBy = null, string? removalReason = null, DateTime? startedAt = null) => new AssessmentSession
    {
        Id = id, UserId = userId, Title = title, Category = category, AccessToken = "",
        Schedule = schedule, Status = status, DurationMinutes = 60, PassPercentage = 70,
        RemovedAt = removedAt, RemovedBy = removedBy, RemovalReason = removalReason,
        StartedAt = startedAt, CreatedAt = schedule.AddDays(-1)
    };

    // Helper: invoke action → cast ViewResult → baca ViewBag.RemovedSessions (typed List<RemovedParticipantViewModel>).
    private static List<RemovedParticipantViewModel> GetRemovedPanel(ViewResult view)
    {
        var raw = view.ViewData["RemovedSessions"];   // ViewBag backed by ViewData
        if (raw == null) return new List<RemovedParticipantViewModel>();
        return (List<RemovedParticipantViewModel>)raw;
    }

    // M1 (PLIV-01 — panel query): sesi RemovedAt!=null masuk ViewBag.RemovedSessions;
    //    sesi aktif (RemovedAt==null) TIDAK masuk panel.
    //    De-taut: action AssessmentMonitoringDetail ASLI → LINQ produksi RemovedAt!=null dieksekusi →
    //    assert panel count + id sesuai — bukan replica predikat.
    [Fact]
    public async Task MonitoringDetail_ViewBagRemovedSessions_OnlyContainsSoftRemovedSessions()
    {
        var (ctrl, ctx) = MakeController("AssessmentMonitoringDetail");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        var removedAt = new DateTime(2026, 6, 21, 10, 0, 0);

        // Seed: 1 sesi aktif + 1 sesi removed di batch yang sama.
        ctx.Users.AddRange(
            MakeUser("u-aktif",   "Peserta Aktif"),
            MakeUser("u-removed", "Peserta Dihapus"));
        ctx.AssessmentSessions.AddRange(
            MakeSession(10, "u-aktif",   "BATCH M1", "OJT", sched, S.Open, removedAt: null),
            MakeSession(11, "u-removed", "BATCH M1", "OJT", sched, S.Open, removedAt: removedAt));
        ctx.SaveChanges();

        var result = await ctrl.AssessmentMonitoringDetail("BATCH M1", "OJT", sched, null);

        var view = Assert.IsType<ViewResult>(result);
        var panel = GetRemovedPanel(view);

        // Panel HANYA berisi sesi removed (id=11).
        Assert.Single(panel);
        Assert.Equal(11, panel[0].Id);
        // Sesi aktif (id=10) TIDAK masuk panel.
        Assert.DoesNotContain(panel, p => p.Id == 10);
    }

    // M2 (PLIV-01 — FullName): ViewBag.RemovedSessions[].FullName diisi dari User entity,
    //    bukan default "Unknown" (membuktikan .Include(a => a.User) + seed berfungsi).
    //    De-taut: LINQ produksi (.Include + navigation) + assert FullName nyata dari DB.
    [Fact]
    public async Task MonitoringDetail_RemovedSession_FullNameFromUserNavigation()
    {
        var (ctrl, ctx) = MakeController("AssessmentMonitoringDetail");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        var removedAt = new DateTime(2026, 6, 21, 9, 0, 0);
        const string expectedName = "Budi Santoso";

        // Seed User dengan nama spesifik + sesi aktif agar action tak redirect (butuh ≥1 sesi aktif).
        ctx.Users.AddRange(
            MakeUser("u-budi",    expectedName, nip: "12345"),
            MakeUser("u-anchor",  "Peserta Anchor"));
        ctx.AssessmentSessions.AddRange(
            MakeSession(20, "u-anchor", "BATCH M2", "OJT", sched, S.Open, removedAt: null),
            MakeSession(21, "u-budi",   "BATCH M2", "OJT", sched, S.Open, removedAt: removedAt));
        ctx.SaveChanges();

        var result = await ctrl.AssessmentMonitoringDetail("BATCH M2", "OJT", sched, null);

        var view = Assert.IsType<ViewResult>(result);
        var panel = GetRemovedPanel(view);
        var item = Assert.Single(panel);

        // FullName harus dari User.FullName (bukan "Unknown").
        Assert.Equal(expectedName, item.FullName);
        Assert.Equal("12345", item.Nip);
        // RemovedAt harus terisi.
        Assert.Equal(removedAt, item.RemovedAt);
    }

    // M3 (PLIV-01 — RemovedByName resolve): ViewBag.RemovedSessions[].RemovedByName = FullName admin yang hapus
    //    (di-resolve via removerMap dari _context.Users), BUKAN raw userId.
    //    De-taut: LINQ produksi (removerMap query) + assert nama resolved — membuktikan query kedua benar.
    [Fact]
    public async Task MonitoringDetail_RemovedSession_RemovedByNameResolvedFromRemoverMap()
    {
        var (ctrl, ctx) = MakeController("AssessmentMonitoringDetail");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);
        var removedAt = new DateTime(2026, 6, 21, 11, 0, 0);
        const string adminId = "admin-id-xyz";
        const string adminName = "Admin Penghapus";

        // Seed: User peserta, User admin (remover), dan sesi aktif (anchor agar action tak redirect).
        ctx.Users.AddRange(
            MakeUser("u-peserta", "Peserta Dihapus"),
            MakeUser("u-anchor",  "Peserta Anchor"),
            MakeUser(adminId,     adminName));
        ctx.AssessmentSessions.AddRange(
            MakeSession(30, "u-anchor",  "BATCH M3", "OJT", sched, S.Open, removedAt: null),
            MakeSession(31, "u-peserta", "BATCH M3", "OJT", sched, S.Open,
                removedAt: removedAt, removedBy: adminId, removalReason: "Pelanggaran tata tertib"));
        ctx.SaveChanges();

        var result = await ctrl.AssessmentMonitoringDetail("BATCH M3", "OJT", sched, null);

        var view = Assert.IsType<ViewResult>(result);
        var panel = GetRemovedPanel(view);
        var item = Assert.Single(panel);

        // RemovedByName harus = nama admin, bukan raw userId.
        Assert.Equal(adminName, item.RemovedByName);
        Assert.Equal("Pelanggaran tata tertib", item.RemovalReason);
    }

    // M4 (PLIV-01 — empty panel): batch tanpa sesi removed → ViewBag.RemovedSessions kosong (Count=0).
    //    Guard: pastikan query RemovedAt!=null tidak over-include.
    [Fact]
    public async Task MonitoringDetail_NoRemovedSessions_PanelIsEmpty()
    {
        var (ctrl, ctx) = MakeController("AssessmentMonitoringDetail");
        var sched = new DateTime(2026, 6, 17, 8, 0, 0);

        ctx.Users.Add(MakeUser("u-only", "Peserta Satu-satunya"));
        ctx.AssessmentSessions.Add(
            MakeSession(40, "u-only", "BATCH M4", "OJT", sched, S.Open, removedAt: null));
        ctx.SaveChanges();

        var result = await ctrl.AssessmentMonitoringDetail("BATCH M4", "OJT", sched, null);

        var view = Assert.IsType<ViewResult>(result);
        var panel = GetRemovedPanel(view);

        // Tidak ada sesi removed → panel harus kosong (query RemovedAt!=null return 0 baris).
        Assert.Empty(panel);
    }

    // M5 (PLIV-01 — cross-batch isolation): sesi removed dari batch LAIN tidak masuk panel batch ini.
    //    De-taut: filter Title+Category+Schedule.Date produksi dieksekusi → assert isolasi.
    [Fact]
    public async Task MonitoringDetail_RemovedFromOtherBatch_NotInPanel()
    {
        var (ctrl, ctx) = MakeController("AssessmentMonitoringDetail");
        var schedA = new DateTime(2026, 6, 17, 8, 0, 0);
        var schedB = new DateTime(2026, 6, 18, 8, 0, 0);   // tanggal berbeda = batch berbeda
        var removedAt = new DateTime(2026, 6, 21, 12, 0, 0);

        // Batch A: 1 aktif + 0 removed. Batch B: 1 aktif + 1 removed.
        ctx.Users.AddRange(
            MakeUser("u-a1", "Aktif A"),
            MakeUser("u-b1", "Aktif B"),
            MakeUser("u-b2", "Removed B"));
        ctx.AssessmentSessions.AddRange(
            MakeSession(50, "u-a1", "BATCH SAMA", "OJT", schedA, S.Open, removedAt: null),
            MakeSession(51, "u-b1", "BATCH SAMA", "OJT", schedB, S.Open, removedAt: null),
            MakeSession(52, "u-b2", "BATCH SAMA", "OJT", schedB, S.Open, removedAt: removedAt));
        ctx.SaveChanges();

        // Panggil detail untuk Batch A (schedA).
        var result = await ctrl.AssessmentMonitoringDetail("BATCH SAMA", "OJT", schedA, null);

        var view = Assert.IsType<ViewResult>(result);
        var panel = GetRemovedPanel(view);

        // Sesi removed dari Batch B (schedB) TIDAK boleh masuk panel Batch A.
        Assert.Empty(panel);
        Assert.DoesNotContain(panel, p => p.Id == 52);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}
