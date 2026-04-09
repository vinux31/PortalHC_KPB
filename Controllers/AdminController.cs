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
    [Route("Admin")]
    [Route("Admin/[action]")]
    public class AdminController : AdminBaseController
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IMemoryCache _cache;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<AdminController> logger,
            IMemoryCache cache)
            : base(context, userManager, auditLog, env)
        {
            _logger = logger;
            _cache = cache;
        }

        // GET /Admin/Index
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
        public async Task<IActionResult> Maintenance(bool isEnabled, string message, DateTime? scheduledStartTime, DateTime? scheduledEndTime, string scope, string? selectedModules)
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

            // Validasi: start harus sebelum end
            if (scheduledStartTime.HasValue && scheduledEndTime.HasValue && scheduledStartTime >= scheduledEndTime)
            {
                ModelState.AddModelError("scheduledEndTime", "Tanggal selesai harus setelah tanggal dimulai.");
                return View(maintenance!);
            }

            maintenance!.IsEnabled = isEnabled;
            maintenance.Message = message ?? "";
            maintenance.ScheduledStartTime = scheduledStartTime;
            maintenance.ScheduledEndTime = scheduledEndTime;
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

            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maintenance mode save failed");
                TempData["ErrorMessage"] = "Gagal menyimpan pengaturan maintenance. Silakan coba lagi.";
                return View(maintenance);
            }

            return RedirectToAction("Maintenance");
        }

        #endregion
    }
}
