// Phase 370 / URG-02 / SC1 — Regression guard: window 7-hari dihapus.
//
// Pre-370 code had a Where(a => a.Schedule >= DateTime.UtcNow.AddDays(-7)) filter that
// would exclude sessions older than 7 days from the default view.  Phase 370 removed it.
// This test seeds one "old" session (Schedule -30 days) and asserts it survives into
// ViewBag.ManagementData when no search/filter is supplied.
//
// IF anyone re-introduces a 7-day (or any recency) window filter in ManageAssessmentTab_Assessment,
// the old session disappears and this test goes RED.
//
// Strategy: InMemory ApplicationDbContext (Guid-named, isolated per run) + real MemoryCache.
// null! substitutes for 9 ctor deps that the tested code path never dereferences.
// _logger IS called via LogInformation — supply NullLogger to avoid NRE.
// AdminBaseController ctor only assigns its fields — no null deref on construction.
//
// IMPORTANT: The action's Select projection references a.User (navigation property).
// EF Core InMemory performs in-memory joins for navigation projections — if no matching
// ApplicationUser row exists, EF silently returns 0 rows (treats it as inner join).
// Fix: seed minimal ApplicationUser rows matching each session's UserId.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

public class AssessmentWindowRemovalTests
{
    private static (AssessmentAdminController ctrl, ApplicationDbContext ctx) MakeController()
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
            retakeService:           null!);
        #pragma warning restore CS8625

        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Pin ViewDataDictionary so action writes and test reads share the same instance.
        ctrl.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary());

        return (ctrl, ctx);
    }

    // Helper: seed a minimal ApplicationUser row so EF InMemory navigation join succeeds.
    private static ApplicationUser MakeUser(string id, string name) => new ApplicationUser
    {
        Id           = id,
        UserName     = id,
        NormalizedUserName = id.ToUpper(),
        Email        = $"{id}@test.local",
        NormalizedEmail = $"{id}@TEST.LOCAL",
        FullName     = name,
        SecurityStamp = Guid.NewGuid().ToString()
    };

    /// <summary>
    /// Phase 370 SC1 regression guard — URG-02.
    ///
    /// Seeds an AssessmentSession with Schedule 30 days in the past plus a matching
    /// ApplicationUser (required because EF InMemory joins navigation properties in-memory
    /// and silently drops rows when the FK target is absent).
    ///
    /// Calls ManageAssessmentTab_Assessment with all defaults (no search, no filter).
    /// Asserts the old session IS present in ViewBag.ManagementData.
    ///
    /// If a 7-day window filter is re-added, the old session is excluded and this test fails.
    /// </summary>
    [Fact]
    public async Task DefaultView_NoSearch_IncludesSessionOlderThan7Days()
    {
        // ── Arrange ──────────────────────────────────────────────────────────────
        var (ctrl, ctx) = MakeController();

        // Seed ApplicationUser rows first — required for EF InMemory navigation join.
        ctx.Users.AddRange(
            MakeUser("user-old",    "Worker Old"),
            MakeUser("user-recent", "Worker Recent"));

        // OLD session: 30 days ago, Status=Open, LinkedGroupId=null (standard, not pre/post).
        // Status=Open ensures CIL-02 default-hide-Closed filter does NOT remove it.
        ctx.AssessmentSessions.AddRange(
            new AssessmentSession
            {
                Id              = 1,
                UserId          = "user-old",
                Title           = "OLD OJT SESSION",
                Category        = "OJT",
                Schedule        = DateTime.UtcNow.AddDays(-30),
                Status          = "Open",
                LinkedGroupId   = null,
                DurationMinutes = 60,
                CreatedAt       = DateTime.UtcNow.AddDays(-31)
            },
            new AssessmentSession
            {
                Id              = 2,
                UserId          = "user-recent",
                Title           = "RECENT SESSION",
                Category        = "OJT",
                Schedule        = DateTime.UtcNow.AddDays(-1),
                Status          = "Open",
                LinkedGroupId   = null,
                DurationMinutes = 60,
                CreatedAt       = DateTime.UtcNow.AddDays(-2)
            }
        );
        ctx.SaveChanges();

        // ── Act ───────────────────────────────────────────────────────────────────
        await ctrl.ManageAssessmentTab_Assessment(
            search:       null,
            page:         1,
            pageSize:     20,
            category:     null,
            statusFilter: null);

        // ── Assert ────────────────────────────────────────────────────────────────
        // Read via ViewData[] (not ViewBag) — ViewBag dynamic `as T` silently returns null.
        var raw = ctrl.ViewData["ManagementData"];
        Assert.NotNull(raw);
        var groups = (System.Collections.IList)raw!;

        // Anonymous types are internal to HcPortal.dll — the test assembly cannot access
        // their members via the dynamic binder (RuntimeBinderException).
        // Use reflection to read the Title property from each group object.
        static string? GetTitle(object g)
            => g.GetType().GetProperty("Title")?.GetValue(g) as string;

        var titles = groups
            .Cast<object>()
            .Select(GetTitle)
            .ToList();

        // Primary regression guard: OLD session (>7 days) MUST appear.
        // If a 7-day window filter is re-added, "OLD OJT SESSION" disappears and this fails.
        Assert.Contains("OLD OJT SESSION", titles);

        // Sanity: recent session also present.
        Assert.Contains("RECENT SESSION", titles);
    }
}
