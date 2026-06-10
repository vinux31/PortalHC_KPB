// Phase 362 — G-12: ExportHistoriProton harus boleh diakses Coach (RolesCoachAndAbove),
// seragam dengan export Coaching lain (ExportProgressExcel/ExportCoachingTracking).
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class CDPControllerAuthTests
{
    [Fact]
    public void ExportHistoriProton_AllowsCoachAndAbove()
    {
        var method = typeof(CDPController).GetMethod(nameof(CDPController.ExportHistoriProton));
        Assert.NotNull(method);

        var authz = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authz);
        Assert.Equal(UserRoles.RolesCoachAndAbove, authz!.Roles);
    }
}
