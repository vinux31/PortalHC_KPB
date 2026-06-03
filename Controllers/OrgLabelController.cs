using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Models.ViewModels;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    /// <summary>
    /// Phase 340 endpoint untuk JS consumer (Phase 342+343 view integration).
    /// Phase 341 tambah page CRUD actions (ManageOrgLevelLabels/Update/Add/Delete).
    /// </summary>
    [Authorize]
    [Route("Admin/[action]")]
    public class OrgLabelController : Controller
    {
        private readonly IOrgLabelService _orgLabels;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrgLabelController(
            IOrgLabelService orgLabels,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _orgLabels = orgLabels;
            _context = context;
            _userManager = userManager;
        }

        // Override View resolution to use Views/Admin/ folder (controller name is OrgLabel, but views stay in Admin/)
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        // GET /Admin/GetLevelLabels
        // Response example: { "0": "Bagian", "1": "Unit", "2": "Sub-unit" }
        [HttpGet]
        public IActionResult GetLevelLabels()
        {
            var dict = _orgLabels.GetAll();
            var jsonDict = dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            return Json(jsonDict);
        }

        // GET /Admin/ManageOrgLevelLabels
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageOrgLevelLabels()
        {
            var labels = _orgLabels.GetAll();
            int maxConfig = _orgLabels.GetMaxConfiguredLevel();
            int maxUsed = await _orgLabels.GetMaxUsedLevelAsync();
            int displayMax = Math.Max(maxConfig, maxUsed);

            // Pre-compute which levels are "in use" for the disable-Delete decision
            var usedLevels = await _context.OrganizationUnits
                .Select(u => u.Level)
                .Distinct()
                .ToListAsync();
            var usedSet = new HashSet<int>(usedLevels);

            var rows = new List<LabelRowVM>();
            for (int level = 0; level <= displayMax; level++)
            {
                bool hasLabel = labels.TryGetValue(level, out var lbl);
                bool isHighest = (level == maxConfig);
                bool isUsed = usedSet.Contains(level);
                rows.Add(new LabelRowVM
                {
                    Level = level,
                    Label = hasLabel ? lbl! : null,
                    IsHighest = isHighest,
                    IsUsed = isUsed,
                    CanDelete = hasLabel && isHighest && !isUsed
                });
            }

            // Buffer row (level = displayMax + 1) "(belum diset)"
            rows.Add(new LabelRowVM
            {
                Level = displayMax + 1,
                Label = null,
                IsHighest = false,
                IsUsed = false,
                CanDelete = false
            });

            var vm = new ManageOrgLevelLabelsViewModel
            {
                Rows = rows,
                MaxConfigured = maxConfig,
                MaxUsed = maxUsed,
                NextAddLevel = displayMax + 1
            };
            return View(vm);
        }

        // POST /Admin/UpdateLevelLabel
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLevelLabel(int level, string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return Json(new { success = false, message = "Label tidak boleh kosong." });

            label = label.Trim();

            if (label.Length > 50)
                return Json(new { success = false, message = "Label maksimal 50 karakter." });

            bool duplicate = await _context.OrganizationLevelLabels
                .AnyAsync(l => l.Label == label && l.Level != level);
            if (duplicate)
                return Json(new { success = false, message = $"Label '{label}' sudah dipakai level lain." });

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";

            try
            {
                await _orgLabels.UpdateAsync(level, label, currentUser?.Id ?? "", actorName);
                return Json(new { success = true, message = $"Label level {level} berhasil diubah menjadi '{label}'." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/AddLevelLabel
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLevelLabel(int level, string label)
        {
            // D-08: prevent arbitrary level injection
            int expectedNext = _orgLabels.GetMaxConfiguredLevel() + 1;
            if (level != expectedNext)
                return Json(new { success = false, message = $"Hanya level berikutnya (Level {expectedNext}) yang bisa ditambahkan." });

            if (string.IsNullOrWhiteSpace(label))
                return Json(new { success = false, message = "Label tidak boleh kosong." });
            label = label.Trim();
            if (label.Length > 50)
                return Json(new { success = false, message = "Label maksimal 50 karakter." });

            bool duplicate = await _context.OrganizationLevelLabels.AnyAsync(l => l.Label == label);
            if (duplicate)
                return Json(new { success = false, message = $"Label '{label}' sudah dipakai level lain." });

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";

            try
            {
                await _orgLabels.AddAsync(level, label, currentUser?.Id ?? "", actorName);
                return Json(new { success = true, message = $"Level {level} '{label}' berhasil ditambahkan." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, message = "Level sudah ada, silakan refresh halaman." });
            }
        }

        // POST /Admin/DeleteLevelLabel
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLevelLabel(int level)
        {
            int maxConfig = _orgLabels.GetMaxConfiguredLevel();
            if (level != maxConfig)
                return Json(new { success = false, message = "Hanya level tertinggi yang bisa dihapus." });

            bool isUsed = await _context.OrganizationUnits.AnyAsync(u => u.Level == level);
            if (isUsed)
                return Json(new { success = false, message = "Level masih dipakai unit, tidak bisa dihapus." });

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";

            try
            {
                await _orgLabels.DeleteAsync(level, currentUser?.Id ?? "", actorName);
                return Json(new { success = true, message = $"Level {level} berhasil dihapus." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
