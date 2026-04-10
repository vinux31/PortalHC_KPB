using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HcPortal.Controllers
{
    [Route("Admin/[action]")]
    public class AdminController : AdminBaseController
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IMemoryCache _cache;
        private readonly ImpersonationService _impersonationService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<AdminController> logger,
            IMemoryCache cache,
            ImpersonationService impersonationService)
            : base(context, userManager, auditLog, env)
        {
            _logger = logger;
            _cache = cache;
            _impersonationService = impersonationService;
        }

        // GET /Admin or /Admin/Index
        [Route("~/Admin")]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> Index()
        {
            // Renewal badge count — single source of truth via BuildRenewalRowsAsync
            var renewalRows = await BuildRenewalRowsAsync();
            ViewBag.RenewalCount = renewalRows.Count;

            return View();
        }

        #region Maintenance Mode

        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> Maintenance()
        {
            var maintenance = await _context.MaintenanceModes.FirstOrDefaultAsync();
            if (maintenance == null)
            {
                maintenance = new MaintenanceMode { IsEnabled = false, Message = "", Scope = "All" };
            }
            return View(maintenance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> Maintenance(bool isEnabled, string message, DateTime? estimatedEndTime, string scope, string? selectedModules)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var maintenance = await _context.MaintenanceModes.FirstOrDefaultAsync();
            bool isNew = maintenance == null;
            if (isNew)
            {
                maintenance = new MaintenanceMode();
                _context.MaintenanceModes.Add(maintenance);
            }

            var normalizedScope = MaintenanceScopeCatalog.NormalizeSelectedKeys(selectedModules);
            var isSpecificScope = string.Equals(scope, "Specific", StringComparison.OrdinalIgnoreCase);

            maintenance!.IsEnabled = isEnabled;
            maintenance.Message = message ?? "";
            maintenance.EstimatedEndTime = estimatedEndTime;
            maintenance.Scope = isSpecificScope ? normalizedScope : "All";

            if (isSpecificScope && string.IsNullOrWhiteSpace(normalizedScope))
            {
                ModelState.AddModelError("selectedModules", "Pilih minimal satu halaman jika menggunakan cakupan halaman tertentu.");
                return View(maintenance);
            }

            if (isEnabled)
            {
                maintenance.ActivatedByUserId = user.Id;
                maintenance.ActivatedByName = user.FullName;
                maintenance.ActivatedAt = DateTime.UtcNow;
                maintenance.DeactivatedAt = null;
            }
            else
            {
                maintenance.DeactivatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _cache.Remove("MaintenanceMode_State");

            await _auditLog.LogAsync(
                user.Id, user.FullName,
                isEnabled ? "MaintenanceEnabled" : "MaintenanceDisabled",
                $"Maintenance mode {(isEnabled ? "diaktifkan" : "dinonaktifkan")} — Scope: {MaintenanceScopeCatalog.GetSummary(maintenance.Scope)}",
                maintenance.Id, "MaintenanceMode");

            TempData["SuccessMessage"] = isEnabled
                ? "Mode pemeliharaan berhasil diaktifkan."
                : "Mode pemeliharaan berhasil dinonaktifkan.";

            return RedirectToAction("Maintenance");
        }

        #endregion

        #region Impersonation

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Impersonate()
        {
            var logs = await _context.AuditLogs
                .Where(a => a.ActionType == "ImpersonateStart" || a.ActionType == "ImpersonateEnd")
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(logs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartImpersonation(string mode, string? targetRole, string? targetUserId)
        {
            if (mode != "role" && mode != "user")
            {
                TempData["ErrorMessage"] = "Mode impersonation tidak valid.";
                return RedirectToAction("Index");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Index");

            string description;

            if (mode == "role")
            {
                if (string.IsNullOrEmpty(targetRole) || UserRoles.GetRoleLevel(targetRole) < 2)
                {
                    TempData["ErrorMessage"] = "Role target tidak valid. Tidak bisa impersonate Admin.";
                    return RedirectToAction("Index");
                }

                _impersonationService.StartRole(targetRole);
                description = $"Mulai impersonation sebagai role {targetRole}";
            }
            else
            {
                if (string.IsNullOrEmpty(targetUserId))
                {
                    TempData["ErrorMessage"] = "User target harus dipilih.";
                    return RedirectToAction("Index");
                }

                var targetUser = await _userManager.FindByIdAsync(targetUserId);
                if (targetUser == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                var targetRoles = await _userManager.GetRolesAsync(targetUser);
                if (targetRoles.Contains("Admin"))
                {
                    TempData["ErrorMessage"] = "Tidak bisa impersonate admin lain.";
                    return RedirectToAction("Index");
                }

                _impersonationService.StartUser(targetUserId, targetUser.FullName);
                description = $"Mulai impersonation sebagai user {targetUser.FullName} ({targetUser.NIP})";
            }

            await _auditLog.LogAsync(currentUser.Id, currentUser.FullName, "ImpersonateStart", description);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopImpersonation()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Index");

            var displayName = _impersonationService.GetDisplayName() ?? "unknown";
            var mode = _impersonationService.GetMode() ?? "unknown";

            await _auditLog.LogAsync(currentUser.Id, currentUser.FullName, "ImpersonateEnd",
                $"Mengakhiri impersonation ({mode}: {displayName})");

            _impersonationService.Stop();

            TempData["SuccessMessage"] = "Sesi impersonation berakhir.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchUsersApi(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            // Single query with join to exclude Admin users (fixes N+1)
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var adminUserIds = adminRoleId != null
                ? await _context.UserRoles.Where(ur => ur.RoleId == adminRoleId).Select(ur => ur.UserId).ToListAsync()
                : new List<string>();

            var results = await _userManager.Users
                .Where(u => !adminUserIds.Contains(u.Id)
                    && (u.FullName.Contains(q) || (u.NIP != null && u.NIP.Contains(q))))
                .Take(10)
                .Select(u => new { u.Id, u.FullName, u.NIP, u.SelectedView })
                .ToListAsync();

            return Json(results);
        }

        #endregion
    }
}
