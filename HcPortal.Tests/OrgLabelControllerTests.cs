// Phase 341 Plan 03 — OrgLabelController inline validation paths.
// REQ coverage: ORG-LABEL-04 (validation enforcement), ORG-LABEL-06 (server-side checks).
// Strategy: skip actor-resolution test (UserManager null-substitute) per RESEARCH §Code Examples #7 Option 2.
// All 7 [Fact] exercise validation rejects that return BEFORE _userManager.GetUserAsync, so null is safe.
// Happy-path mutation + audit log coverage retained by Phase 340 OrgLabelServiceTests (mutation [Fact]).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 341 Plan 03 — Controller validation paths.
/// Strategy: skip actor-resolution (UserManager null-substitute) and assert pre-service inline
/// validation. Validation rejects return before _userManager.GetUserAsync call, so null is safe.
/// </summary>
public class OrgLabelControllerTests
{
    // ── Factory ──────────────────────────────────────────────────────────────

    private static (OrgLabelController ctrl, ApplicationDbContext ctx) MakeControllerWithCtx(bool seedUnits = false)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);

        // Seed 3 default label rows (Level 0/1/2).
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );

        // Optionally seed 1 OrganizationUnit at Level 2 to exercise IsUsed delete guard.
        if (seedUnits)
        {
            ctx.OrganizationUnits.Add(new OrganizationUnit
            {
                Name = "TestUnit-L2",
                Level = 2,
                IsActive = true
            });
        }
        ctx.SaveChanges();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var auditLog = new AuditLogService(ctx);
        var svc = new OrgLabelService(ctx, cache, auditLog);

        // UserManager null-substitute: validation-rejection tests don't reach GetUserAsync call.
        #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var ctrl = new OrgLabelController(svc, ctx, null!);
        #pragma warning restore CS8625

        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return (ctrl, ctx);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool GetSuccess(IActionResult result)
    {
        var json = Assert.IsType<JsonResult>(result);
        return (bool)json.Value!.GetType().GetProperty("success")!.GetValue(json.Value)!;
    }

    private static string GetMessage(IActionResult result)
    {
        var json = Assert.IsType<JsonResult>(result);
        return (string)json.Value!.GetType().GetProperty("message")!.GetValue(json.Value)!;
    }

    // ── [Fact] tests — inline validation rejects (D-05 + D-08 + Pitfall 7) ─────

    [Fact]
    public async Task UpdateLevelLabel_EmptyLabel_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx();

        var result = await ctrl.UpdateLevelLabel(0, "");

        Assert.False(GetSuccess(result));
        Assert.Contains("kosong", GetMessage(result));
    }

    [Fact]
    public async Task UpdateLevelLabel_WhitespaceLabel_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx();

        var result = await ctrl.UpdateLevelLabel(0, "   ");

        Assert.False(GetSuccess(result));
        Assert.Contains("kosong", GetMessage(result));
    }

    [Fact]
    public async Task UpdateLevelLabel_TooLong_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx();

        var result = await ctrl.UpdateLevelLabel(0, new string('x', 51));

        Assert.False(GetSuccess(result));
        Assert.Contains("50", GetMessage(result));
    }

    [Fact]
    public async Task UpdateLevelLabel_DuplicateAcrossLevels_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx();

        // "Unit" already exists at Level 1; updating Level 0 to "Unit" must reject.
        var result = await ctrl.UpdateLevelLabel(0, "Unit");

        Assert.False(GetSuccess(result));
        Assert.Contains("sudah dipakai", GetMessage(result));
    }

    [Fact]
    public async Task AddLevelLabel_NonNextLevel_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx();

        // Max configured = 2, expected next = 3; level 99 must reject (D-08).
        var result = await ctrl.AddLevelLabel(99, "X");

        Assert.False(GetSuccess(result));
        Assert.Contains("Hanya level berikutnya", GetMessage(result));
    }

    [Fact]
    public async Task DeleteLevelLabel_NonHighest_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx();

        // Max = 2; deleting Level 0 (mid-tier) must reject (Pitfall 7 / T-341-05).
        var result = await ctrl.DeleteLevelLabel(0);

        Assert.False(GetSuccess(result));
        Assert.Contains("tertinggi", GetMessage(result));
    }

    [Fact]
    public async Task DeleteLevelLabel_HighestInUse_ReturnsJsonFailure()
    {
        var (ctrl, _) = MakeControllerWithCtx(seedUnits: true);

        // Level 2 is highest AND referenced by TestUnit-L2 → must reject.
        var result = await ctrl.DeleteLevelLabel(2);

        Assert.False(GetSuccess(result));
        Assert.Contains("dipakai", GetMessage(result));
    }

    // ── TEST-02: permission contract (reflection — pipeline-enforced, not unit-callable) ──
    // Auth is enforced by the ASP.NET pipeline via [Authorize] attributes, NOT in the controller
    // body, so a directly-instantiated controller cannot exercise a 403. We assert the attribute
    // contract via reflection here; the live 403 (incl. a coach POST) is covered in Plan 03 (Playwright).
    private static AuthorizeAttribute? RolesAttr(string method)
    {
        var m = typeof(OrgLabelController).GetMethod(method)!;
        return m.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
    }

    [Fact] public void ManageOrgLevelLabels_RequiresAdminOrHcRole()
        => Assert.Equal("Admin, HC", RolesAttr(nameof(OrgLabelController.ManageOrgLevelLabels))!.Roles);

    [Fact] public void UpdateLevelLabel_RequiresAdminOrHcRole()
        => Assert.Equal("Admin, HC", RolesAttr(nameof(OrgLabelController.UpdateLevelLabel))!.Roles);

    [Fact] public void AddLevelLabel_RequiresAdminOrHcRole()
        => Assert.Equal("Admin, HC", RolesAttr(nameof(OrgLabelController.AddLevelLabel))!.Roles);

    [Fact] public void DeleteLevelLabel_RequiresAdminOrHcRole()
        => Assert.Equal("Admin, HC", RolesAttr(nameof(OrgLabelController.DeleteLevelLabel))!.Roles);

    [Fact] public void GetLevelLabels_DoesNotRequireAdminOrHcRole()   // locks ORG-LABEL-03 contract
    {
        var attr = RolesAttr(nameof(OrgLabelController.GetLevelLabels));
        Assert.True(attr is null || string.IsNullOrEmpty(attr.Roles));
    }
}
