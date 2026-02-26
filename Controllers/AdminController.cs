using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLog;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
        }

        // GET /Admin/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET /Admin/KkjMatrix
        public async Task<IActionResult> KkjMatrix()
        {
            ViewData["Title"] = "Kelola KKJ Matrix";
            var items = await _context.KkjMatrices.OrderBy(k => k.No).ToListAsync();
            return View(items);
        }

        // POST /Admin/KkjMatrixSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjMatrixSave([FromBody] List<KkjMatrixItem> rows)
        {
            if (rows == null || !rows.Any())
                return Json(new { success = false, message = "Tidak ada data yang diterima." });

            try
            {
                foreach (var row in rows)
                {
                    if (row.Id == 0)
                    {
                        _context.KkjMatrices.Add(row);
                    }
                    else
                    {
                        var existing = await _context.KkjMatrices.FindAsync(row.Id);
                        if (existing != null)
                        {
                            existing.No = row.No;
                            existing.SkillGroup = row.SkillGroup ?? "";
                            existing.SubSkillGroup = row.SubSkillGroup ?? "";
                            existing.Indeks = row.Indeks ?? "";
                            existing.Kompetensi = row.Kompetensi ?? "";
                            existing.Target_SectionHead = row.Target_SectionHead ?? "-";
                            existing.Target_SrSpv_GSH = row.Target_SrSpv_GSH ?? "-";
                            existing.Target_ShiftSpv_GSH = row.Target_ShiftSpv_GSH ?? "-";
                            existing.Target_Panelman_GSH_12_13 = row.Target_Panelman_GSH_12_13 ?? "-";
                            existing.Target_Panelman_GSH_14 = row.Target_Panelman_GSH_14 ?? "-";
                            existing.Target_Operator_GSH_8_11 = row.Target_Operator_GSH_8_11 ?? "-";
                            existing.Target_Operator_GSH_12_13 = row.Target_Operator_GSH_12_13 ?? "-";
                            existing.Target_ShiftSpv_ARU = row.Target_ShiftSpv_ARU ?? "-";
                            existing.Target_Panelman_ARU_12_13 = row.Target_Panelman_ARU_12_13 ?? "-";
                            existing.Target_Panelman_ARU_14 = row.Target_Panelman_ARU_14 ?? "-";
                            existing.Target_Operator_ARU_8_11 = row.Target_Operator_ARU_8_11 ?? "-";
                            existing.Target_Operator_ARU_12_13 = row.Target_Operator_ARU_12_13 ?? "-";
                            existing.Target_SrSpv_Facility = row.Target_SrSpv_Facility ?? "-";
                            existing.Target_JrAnalyst = row.Target_JrAnalyst ?? "-";
                            existing.Target_HSE = row.Target_HSE ?? "-";
                        }
                    }
                }
                await _context.SaveChangesAsync();

                var actor = await _userManager.GetUserAsync(User);
                if (actor != null)
                    await _auditLog.LogAsync(actor.Id, actor.FullName, "BulkUpdate",
                        $"KKJ Matrix bulk-save: {rows.Count} rows", targetType: "KkjMatrixItem");

                return Json(new { success = true, message = $"{rows.Count} baris berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/KkjMatrixDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjMatrixDelete(int id)
        {
            var item = await _context.KkjMatrices.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Item tidak ditemukan." });

            var usageCount = await _context.UserCompetencyLevels
                .CountAsync(u => u.KkjMatrixItemId == id);

            if (usageCount > 0)
                return Json(new { success = false, blocked = true,
                    message = $"Tidak dapat dihapus — digunakan oleh {usageCount} pekerja." });

            _context.KkjMatrices.Remove(item);
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                    $"Deleted KkjMatrixItem Id={id} ({item.Kompetensi})",
                    targetId: id, targetType: "KkjMatrixItem");

            return Json(new { success = true });
        }
    }
}
