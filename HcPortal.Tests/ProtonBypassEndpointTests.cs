// Phase 360 (PBYP-07) — reflection test atribut endpoint bypass (pola Phase 344 TEST-05):
// class [Authorize(Admin,HC)] men-gate 6 endpoint (T-360-22); 3 POST mutator wajib
// [HttpPost] + [ValidateAntiForgeryToken] (T-360-23); GET read-only tanpa antiforgery.
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class ProtonBypassEndpointTests
{
    [Fact]
    public void ProtonDataController_ClassLevel_AuthorizeAdminHC()
    {
        var authz = typeof(ProtonDataController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authz);
        Assert.Contains("Admin", authz!.Roles ?? "");
        Assert.Contains("HC", authz.Roles ?? "");
    }

    [Theory]
    [InlineData(nameof(ProtonDataController.BypassSave))]
    [InlineData(nameof(ProtonDataController.BypassConfirm))]
    [InlineData(nameof(ProtonDataController.BypassCancelPending))]
    public void PostMutator_HasHttpPost_AndValidateAntiForgeryToken(string methodName)
    {
        var method = typeof(ProtonDataController).GetMethod(methodName);
        Assert.NotNull(method);

        Assert.NotEmpty(method!.GetCustomAttributes(typeof(HttpPostAttribute), false));
        Assert.NotEmpty(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false));
    }

    [Theory]
    [InlineData(nameof(ProtonDataController.BypassList))]
    [InlineData(nameof(ProtonDataController.BypassPendingList))]
    [InlineData(nameof(ProtonDataController.BypassDetail))]
    public void GetReadEndpoint_Exists_NoAntiForgery(string methodName)
    {
        var method = typeof(ProtonDataController).GetMethod(methodName);
        Assert.NotNull(method);

        Assert.Empty(method!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false));
        Assert.Empty(method.GetCustomAttributes(typeof(HttpPostAttribute), false));
    }

    [Fact]
    public void PostMutator_Count_Is3_GetRead_Count_Is3()
    {
        var bypassMethods = typeof(ProtonDataController).GetMethods()
            .Where(m => m.Name.StartsWith("Bypass")).ToList();

        var posts = bypassMethods.Where(m =>
            m.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()).Select(m => m.Name).ToList();
        var gets = bypassMethods.Where(m =>
            !m.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()).Select(m => m.Name).ToList();

        Assert.Equal(3, posts.Count);
        Assert.Equal(3, gets.Count);
    }
}
