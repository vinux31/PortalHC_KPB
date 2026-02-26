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

            // Seed default bagians if none exist yet
            if (!await _context.KkjBagians.AnyAsync())
            {
                var defaults = new[]
                {
                    new KkjBagian { Name = "RFCC",    DisplayOrder = 1 },
                    new KkjBagian { Name = "GAST",    DisplayOrder = 2 },
                    new KkjBagian { Name = "NGP",     DisplayOrder = 3 },
                    new KkjBagian { Name = "DHT/HMU", DisplayOrder = 4 },
                };
                _context.KkjBagians.AddRange(defaults);
                await _context.SaveChangesAsync();
            }

            var bagians = await _context.KkjBagians.OrderBy(b => b.DisplayOrder).ToListAsync();
            var items   = await _context.KkjMatrices.OrderBy(k => k.No).ToListAsync();

            ViewBag.Bagians = bagians;
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
                            existing.Bagian = row.Bagian ?? "";
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

        // POST /Admin/KkjBagianSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianSave([FromBody] List<KkjBagian> bagians)
        {
            if (bagians == null || !bagians.Any())
                return Json(new { success = false, message = "Tidak ada data bagian." });

            try
            {
                foreach (var b in bagians)
                {
                    if (b.Id == 0)
                    {
                        _context.KkjBagians.Add(b);
                    }
                    else
                    {
                        var existing = await _context.KkjBagians.FindAsync(b.Id);
                        if (existing != null)
                        {
                            existing.Name         = b.Name;
                            existing.DisplayOrder = b.DisplayOrder;
                            existing.Label_SectionHead        = b.Label_SectionHead        ?? "Section Head";
                            existing.Label_SrSpv_GSH          = b.Label_SrSpv_GSH          ?? "Sr Spv GSH";
                            existing.Label_ShiftSpv_GSH       = b.Label_ShiftSpv_GSH       ?? "Shift Spv GSH";
                            existing.Label_Panelman_GSH_12_13 = b.Label_Panelman_GSH_12_13 ?? "Panelman GSH 12-13";
                            existing.Label_Panelman_GSH_14    = b.Label_Panelman_GSH_14    ?? "Panelman GSH 14";
                            existing.Label_Operator_GSH_8_11  = b.Label_Operator_GSH_8_11  ?? "Op GSH 8-11";
                            existing.Label_Operator_GSH_12_13 = b.Label_Operator_GSH_12_13 ?? "Op GSH 12-13";
                            existing.Label_ShiftSpv_ARU       = b.Label_ShiftSpv_ARU       ?? "Shift Spv ARU";
                            existing.Label_Panelman_ARU_12_13 = b.Label_Panelman_ARU_12_13 ?? "Panelman ARU 12-13";
                            existing.Label_Panelman_ARU_14    = b.Label_Panelman_ARU_14    ?? "Panelman ARU 14";
                            existing.Label_Operator_ARU_8_11  = b.Label_Operator_ARU_8_11  ?? "Op ARU 8-11";
                            existing.Label_Operator_ARU_12_13 = b.Label_Operator_ARU_12_13 ?? "Op ARU 12-13";
                            existing.Label_SrSpv_Facility     = b.Label_SrSpv_Facility     ?? "Sr Spv Facility";
                            existing.Label_JrAnalyst          = b.Label_JrAnalyst          ?? "Jr Analyst";
                            existing.Label_HSE                = b.Label_HSE                ?? "HSE";
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/KkjBagianAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianAdd()
        {
            var maxOrder = await _context.KkjBagians.MaxAsync(b => (int?)b.DisplayOrder) ?? 0;
            var newBagian = new KkjBagian
            {
                Name         = "Bagian Baru",
                DisplayOrder = maxOrder + 1
            };
            _context.KkjBagians.Add(newBagian);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success      = true,
                id           = newBagian.Id,
                name         = newBagian.Name,
                displayOrder = newBagian.DisplayOrder
            });
        }

        // POST /Admin/KkjBagianDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianDelete(int id)
        {
            var bagian = await _context.KkjBagians.FindAsync(id);
            if (bagian == null)
                return Json(new { success = false, message = "Bagian tidak ditemukan." });

            var assignedCount = await _context.KkjMatrices
                .CountAsync(k => k.Bagian == bagian.Name);

            if (assignedCount > 0)
                return Json(new { success = false, blocked = true,
                    message = $"Tidak dapat dihapus — masih ada {assignedCount} item yang di-assign ke bagian ini." });

            _context.KkjBagians.Remove(bagian);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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

        // GET /Admin/CpdpItems
        public async Task<IActionResult> CpdpItems()
        {
            ViewData["Title"] = "KKJ-IDP Mapping Editor";
            var items = await _context.CpdpItems
                .OrderBy(c => c.No)
                .ThenBy(c => c.Id)
                .ToListAsync();
            return View(items);
        }

        // POST /Admin/CpdpItemsSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CpdpItemsSave([FromBody] List<CpdpItem> rows)
        {
            if (rows == null || !rows.Any())
                return Json(new { success = false, message = "Tidak ada data yang diterima." });

            try
            {
                foreach (var row in rows)
                {
                    if (row.Id == 0)
                    {
                        _context.CpdpItems.Add(row);
                    }
                    else
                    {
                        var existing = await _context.CpdpItems.FindAsync(row.Id);
                        if (existing != null)
                        {
                            // Warn if NamaKompetensi changed and IdpItems reference the old name
                            if (existing.NamaKompetensi != row.NamaKompetensi)
                            {
                                var refCount = await _context.IdpItems
                                    .CountAsync(i => i.Kompetensi == existing.NamaKompetensi);
                                if (refCount > 0)
                                    return Json(new { success = false,
                                        message = $"Tidak bisa ubah NamaKompetensi '{existing.NamaKompetensi}' — {refCount} IDP record masih mereferensi nama ini." });
                            }

                            existing.No                 = row.No ?? "";
                            existing.NamaKompetensi     = row.NamaKompetensi ?? "";
                            existing.IndikatorPerilaku  = row.IndikatorPerilaku ?? "";
                            existing.DetailIndikator    = row.DetailIndikator ?? "";
                            existing.Silabus            = row.Silabus ?? "";
                            existing.TargetDeliverable  = row.TargetDeliverable ?? "";
                            existing.Status             = row.Status ?? "";
                            existing.Section            = row.Section ?? "";
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var actor = await _userManager.GetUserAsync(User);
                if (actor != null)
                    await _auditLog.LogAsync(actor.Id, actor.FullName, "BulkUpdate",
                        $"CPDP Items bulk-save: {rows.Count} rows", targetType: "CpdpItem");

                return Json(new { success = true, message = $"{rows.Count} baris berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/CpdpItemDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CpdpItemDelete(int id)
        {
            var item = await _context.CpdpItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "CPDP item tidak ditemukan." });

            // Reference guard: check IdpItem records that match by NamaKompetensi string
            var usageCount = await _context.IdpItems
                .CountAsync(i => i.Kompetensi == item.NamaKompetensi);

            if (usageCount > 0)
                return Json(new { success = false, blocked = true,
                    message = $"Tidak dapat dihapus — digunakan oleh {usageCount} IDP record." });

            _context.CpdpItems.Remove(item);
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                    $"Deleted CpdpItem Id={id} ({item.NamaKompetensi})",
                    targetId: id, targetType: "CpdpItem");

            return Json(new { success = true });
        }

        // GET /Admin/CpdpItemsExport?section=RFCC
        public async Task<IActionResult> CpdpItemsExport(string? section)
        {
            var query = _context.CpdpItems.OrderBy(c => c.No).ThenBy(c => c.Id).AsQueryable();

            if (!string.IsNullOrEmpty(section))
                query = query.Where(c => c.Section == section);

            var items = await query.ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("CPDP Items");

            // Header row
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Nama Kompetensi";
            ws.Cell(1, 3).Value = "Indikator Perilaku";
            ws.Cell(1, 4).Value = "Detail Indikator";
            ws.Cell(1, 5).Value = "Silabus / IDP";
            ws.Cell(1, 6).Value = "Target Deliverable";
            ws.Cell(1, 7).Value = "Status";
            ws.Cell(1, 8).Value = "Section";

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#343a40");
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            // Data rows
            for (int i = 0; i < items.Count; i++)
            {
                var row = items[i];
                var r = i + 2;
                ws.Cell(r, 1).Value = row.No;
                ws.Cell(r, 2).Value = row.NamaKompetensi;
                ws.Cell(r, 3).Value = row.IndikatorPerilaku;
                ws.Cell(r, 4).Value = row.DetailIndikator;
                ws.Cell(r, 5).Value = row.Silabus;
                ws.Cell(r, 6).Value = row.TargetDeliverable;
                ws.Cell(r, 7).Value = row.Status;
                ws.Cell(r, 8).Value = row.Section;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = string.IsNullOrEmpty(section)
                ? "CPDP_Items_All.xlsx"
                : $"CPDP_Items_{section}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
