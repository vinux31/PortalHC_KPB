// Phase 380 (WSE-03 / RST-01) — reflection-authz: AddExtraTime must be gated to Admin/HC only.
// Mirrors CDPControllerAuthTests.cs pattern. "Admin, HC" exact string (WITH space) — Pitfall 4.
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class AddExtraTimeAuthTests
{
    [Fact] // WSE-03 / RST-01: only Admin/HC may grant extra time (closes privilege-escalation hole T-380-04)
    public void AddExtraTime_RequiresAdminOrHc()
    {
        var method = typeof(AssessmentAdminController)
            .GetMethod(nameof(AssessmentAdminController.AddExtraTime));
        Assert.NotNull(method);
        var authz = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(authz);
        Assert.Equal("Admin, HC", authz!.Roles);   // exact string WITH space (Pitfall 4)
    }
}
