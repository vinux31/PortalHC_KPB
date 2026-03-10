using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HcPortal.Models;

namespace HcPortal.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var viewModel = new DashboardHomeViewModel
        {
            CurrentUser = user,
            Greeting = GetTimeBasedGreeting()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Guide()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var userRoles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = userRoles.FirstOrDefault() ?? "User";
        return View();
    }

    public async Task<IActionResult> GuideDetail(string module)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var userRoles = await _userManager.GetRolesAsync(user);
        var userRole = userRoles.FirstOrDefault() ?? "User";

        // Validate module & role access
        var adminModules = new[] { "data", "admin" };
        if (adminModules.Contains(module) && userRole != "Admin" && userRole != "HC")
            return RedirectToAction("Guide");

        var validModules = new[] { "cmp", "cdp", "account", "data", "admin" };
        if (!validModules.Contains(module))
            return RedirectToAction("Guide");

        ViewBag.UserRole = userRole;
        ViewBag.Module = module;
        return View();
    }

    private string GetTimeBasedGreeting()
    {
        var hour = DateTime.Now.Hour;
        return hour < 12 ? "Selamat Pagi"
             : hour < 15 ? "Selamat Siang"
             : hour < 18 ? "Selamat Sore"
             : "Selamat Malam";
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
