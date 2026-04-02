using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using ClosedXML.Excel;
using System.Globalization;
using HcPortal.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace HcPortal.Controllers
{
    [Route("Admin")]
    [Route("Admin/[action]")]
    public class AdminController : AdminBaseController
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<AdminController> logger)
            : base(context, userManager, auditLog, env)
        {
            _logger = logger;
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

        #region KKJ File Management

        // GET /Admin/KkjMatrix?bagian={bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> KkjMatrix(int? bagian)
        {
            ViewData["Title"] = "Kelola KKJ Matrix";

            // OrganizationUnits are seeded via migration; no runtime seeding needed
            var bagians = await _context.OrganizationUnits
                .Where(u => u.ParentId == null)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // Load active (non-archived) files grouped by bagianId
            var files = await _context.KkjFiles
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            var filesByBagian = bagians.ToDictionary(
                b => b.Id,
                b => files.Where(f => f.OrganizationUnitId == b.Id).ToList()
            );

            // Determine which tab to show active
            var selectedBagianId = bagian ?? bagians.FirstOrDefault()?.Id ?? 0;

            ViewBag.Bagians = bagians;
            ViewBag.FilesByBagian = filesByBagian;
            ViewBag.SelectedBagianId = selectedBagianId;

            return View();
        }

        // GET /Admin/KkjUpload?bagianId={id}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> KkjUpload(int? bagianId)
        {
            ViewData["Title"] = "Upload File KKJ Matrix";
            var bagians = await _context.OrganizationUnits.Where(u => u.ParentId == null).OrderBy(b => b.DisplayOrder).ToListAsync();
            ViewBag.Bagians = bagians;
            ViewBag.SelectedBagianId = bagianId ?? 0;
            return View();
        }

        // POST /Admin/KkjUpload
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjUpload(IFormFile file, string? keterangan, int bagianId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Pilih file terlebih dahulu.";
                return RedirectToAction("KkjUpload", new { bagianId });
            }

            var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Hanya file PDF atau Excel yang didukung (.pdf, .xlsx, .xls).";
                return RedirectToAction("KkjUpload", new { bagianId });
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                TempData["Error"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return RedirectToAction("KkjUpload", new { bagianId });
            }

            var bagian = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagian == null)
            {
                TempData["Error"] = "Bagian tidak ditemukan.";
                return RedirectToAction("KkjUpload", new { bagianId });
            }

            try
            {
                var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kkj", bagianId.ToString());
                Directory.CreateDirectory(storageDir);

                // Safe filename: {unixTimestamp}_{originalNameNoSpaces}{ext}
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var originalName = Path.GetFileNameWithoutExtension(file.FileName)
                                       .Replace(" ", "_")
                                       .Replace("..", "");
                var safeName = $"{timestamp}_{originalName}{fileExtension}";
                var physicalPath = Path.Combine(storageDir, safeName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var kkjFile = new KkjFile
                {
                    OrganizationUnitId = bagianId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/kkj/{bagianId}/{safeName}",
                    FileSizeBytes = file.Length,
                    FileType = fileExtension.TrimStart('.'),
                    Keterangan = keterangan,
                    UploadedAt = DateTimeOffset.UtcNow,
                    UploaderName = (currentUser as ApplicationUser)?.FullName ?? currentUser?.UserName ?? "Unknown",
                    IsArchived = false
                };
                _context.KkjFiles.Add(kkjFile);
                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var uploadActorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        uploadActorName,
                        "UploadKKJFile",
                        $"Uploaded KKJ file '{file.FileName}' ({file.Length} bytes) to bagian {bagian.Name} [BagianId={bagianId}]",
                        kkjFile.Id,
                        "KkjFile");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit log write failed for KkjUpload");
                }

                TempData["Success"] = $"File '{file.FileName}' berhasil di-upload ke bagian {bagian.Name}.";
                return RedirectToAction("KkjMatrix", new { bagian = bagianId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload KKJ file for bagian {BagianId}", bagianId);
                TempData["Error"] = "Gagal menyimpan file. Silakan coba lagi.";
                return RedirectToAction("KkjUpload", new { bagianId });
            }
        }

        // GET /Admin/KkjFileDownload/{id}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> KkjFileDownload(int id)
        {
            var kkjFile = await _context.KkjFiles
                .Include(f => f.OrganizationUnit)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (kkjFile == null) return NotFound();

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                kkjFile.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(physicalPath)) return NotFound("File tidak ditemukan di server.");

            var contentType = kkjFile.FileType switch
            {
                "pdf" => "application/pdf",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "xls"  => "application/vnd.ms-excel",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            return File(fileBytes, contentType, kkjFile.FileName);
        }

        // POST /Admin/KkjFileDelete
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjFileDelete(int id)
        {
            var kkjFile = await _context.KkjFiles.FindAsync(id);
            if (kkjFile == null) return Json(new { success = false, message = "File tidak ditemukan." });

            string fileName = kkjFile.FileName;
            int bagianId = kkjFile.OrganizationUnitId;

            // Soft delete: archive the file (moves to history view, physical file retained)
            kkjFile.IsArchived = true;
            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var deleteUser = await _userManager.GetUserAsync(User);
                var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP) ? (deleteUser?.FullName ?? "Unknown") : $"{deleteUser.NIP} - {deleteUser.FullName}";
                await _auditLog.LogAsync(
                    deleteUser?.Id ?? "",
                    deleteActorName,
                    "ArchiveKKJFile",
                    $"Archived KKJ file '{fileName}' [ID={id}] in bagian {bagianId}",
                    id,
                    "KkjFile");
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "Audit log write failed for KkjFileDelete {Id}", id);
            }

            return Json(new { success = true, message = "File berhasil diarsipkan." });
        }

        // GET /Admin/KkjFileHistory/{bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> KkjFileHistory(int bagianId)
        {
            var bagian = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagian == null) return NotFound();

            var archivedFiles = await _context.KkjFiles
                .Where(f => f.OrganizationUnitId == bagianId && f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            ViewData["Title"] = $"Riwayat File — {bagian.Name}";
            ViewBag.Bagian = bagian;
            ViewBag.ArchivedFiles = archivedFiles;
            return View();
        }

        #endregion

        // POST /Admin/KkjBagianAdd
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianAdd()
        {
            var maxOrder = await _context.OrganizationUnits.Where(u => u.ParentId == null).MaxAsync(b => (int?)b.DisplayOrder) ?? 0;
            var newBagian = new OrganizationUnit
            {
                Name         = "Bagian Baru",
                DisplayOrder = maxOrder + 1,
                Level        = 0
            };
            _context.OrganizationUnits.Add(newBagian);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success      = true,
                id           = newBagian.Id,
                name         = newBagian.Name,
                displayOrder = newBagian.DisplayOrder
            });
        }

        // POST /Admin/DeleteBagian
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBagian(int id, bool confirmed = false)
        {
            var bagian = await _context.OrganizationUnits.FindAsync(id);
            if (bagian == null)
                return Json(new { success = false, message = "Bagian tidak ditemukan." });

            // Count ACTIVE files (not archived) — these block deletion
            var activeKkjCount = await _context.KkjFiles.CountAsync(f => f.OrganizationUnitId == id && !f.IsArchived);
            var activeCpdpCount = await _context.CpdpFiles.CountAsync(f => f.OrganizationUnitId == id && !f.IsArchived);
            var totalActive = activeKkjCount + activeCpdpCount;

            if (totalActive > 0)
            {
                // Active files block deletion — return specific block message
                return Json(new
                {
                    success = false,
                    blocked = true,
                    message = $"Bagian ini memiliki {totalActive} file aktif (KKJ: {activeKkjCount}, CPDP: {activeCpdpCount}). " +
                              $"Arsipkan atau hapus semua file aktif terlebih dahulu sebelum menghapus bagian ini."
                });
            }

            // Count ARCHIVED files — these cascade delete (with confirmation)
            var archivedKkjCount = await _context.KkjFiles.CountAsync(f => f.OrganizationUnitId == id && f.IsArchived);
            var archivedCpdpCount = await _context.CpdpFiles.CountAsync(f => f.OrganizationUnitId == id && f.IsArchived);
            var totalArchived = archivedKkjCount + archivedCpdpCount;

            if (totalArchived > 0 && !confirmed)
            {
                // Has archived files — require explicit confirmation with count
                return Json(new
                {
                    success = false,
                    needsConfirm = true,
                    archivedCount = totalArchived,
                    message = $"Bagian '{bagian.Name}' memiliki {totalArchived} file arsip yang akan ikut terhapus permanen (KKJ: {archivedKkjCount}, CPDP: {archivedCpdpCount}). Tindakan ini tidak dapat dibatalkan."
                });
            }

            // Proceed with deletion: cascade delete archived files from disk + DB
            var archivedKkjFiles = await _context.KkjFiles
                .Where(f => f.OrganizationUnitId == id && f.IsArchived)
                .ToListAsync();
            foreach (var f in archivedKkjFiles)
            {
                if (!string.IsNullOrEmpty(f.FilePath))
                {
                    var diskPath = Path.Combine(_env.WebRootPath, f.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(diskPath))
                        System.IO.File.Delete(diskPath);
                }
            }
            if (archivedKkjFiles.Any())
                _context.KkjFiles.RemoveRange(archivedKkjFiles);

            var archivedCpdpFiles = await _context.CpdpFiles
                .Where(f => f.OrganizationUnitId == id && f.IsArchived)
                .ToListAsync();
            foreach (var f in archivedCpdpFiles)
            {
                if (!string.IsNullOrEmpty(f.FilePath))
                {
                    var diskPath = Path.Combine(_env.WebRootPath, f.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(diskPath))
                        System.IO.File.Delete(diskPath);
                }
            }
            if (archivedCpdpFiles.Any())
                _context.CpdpFiles.RemoveRange(archivedCpdpFiles);

            _context.OrganizationUnits.Remove(bagian);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUser = await _userManager.GetUserAsync(User);
            try
            {
                var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                    ? (currentUser?.FullName ?? "Unknown")
                    : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(
                    currentUser?.Id ?? "", actorName, "DeleteBagian",
                    $"Deleted bagian '{bagian.Name}' (ID {id}). Cascaded {totalArchived} archived file(s) (KKJ: {archivedKkjCount}, CPDP: {archivedCpdpCount}).",
                    id, "OrganizationUnit");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteBagian (bagianId={Id})", id); }

            return Json(new { success = true, message = $"Bagian '{bagian.Name}' berhasil dihapus." });
        }

        #region CPDP File Management

        // GET /Admin/CpdpFiles?bagian={bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CpdpFiles(int? bagian)
        {
            ViewData["Title"] = "CPDP File Management";

            var bagians = await _context.OrganizationUnits
                .Where(u => u.ParentId == null)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // Load active (non-archived) CPDP files grouped by OrganizationUnitId
            var files = await _context.CpdpFiles
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            var filesByBagian = bagians.ToDictionary(
                b => b.Id,
                b => files.Where(f => f.OrganizationUnitId == b.Id).ToList());

            var selectedBagianId = bagians.Any(b => b.Id == bagian)
                ? bagian!.Value
                : bagians.FirstOrDefault()?.Id ?? 0;

            ViewBag.Bagians = bagians;
            ViewBag.FilesByBagian = filesByBagian;
            ViewBag.SelectedBagianId = selectedBagianId;

            return View();
        }

        // GET /Admin/CpdpUpload?bagianId={id}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CpdpUpload(int? bagianId)
        {
            ViewData["Title"] = "Upload File CPDP";
            var bagians = await _context.OrganizationUnits.Where(u => u.ParentId == null).OrderBy(b => b.DisplayOrder).ToListAsync();
            ViewBag.Bagians = bagians;
            ViewBag.SelectedBagianId = bagianId ?? 0;
            return View();
        }

        // POST /Admin/CpdpUpload
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CpdpUpload(IFormFile file, string? keterangan, int bagianId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Pilih file terlebih dahulu.";
                return RedirectToAction("CpdpUpload", new { bagianId });
            }

            var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Hanya file PDF atau Excel yang didukung (.pdf, .xlsx, .xls).";
                return RedirectToAction("CpdpUpload", new { bagianId });
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                TempData["Error"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return RedirectToAction("CpdpUpload", new { bagianId });
            }

            var bagian = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagian == null)
            {
                TempData["Error"] = "Bagian tidak ditemukan.";
                return RedirectToAction("CpdpUpload", new { bagianId });
            }

            try
            {
                var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cpdp", bagianId.ToString());
                Directory.CreateDirectory(storageDir);

                var safeName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Path.GetFileName(file.FileName).Replace(" ", "_")}";
                var fullPath = Path.Combine(storageDir, safeName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var cpdpFile = new CpdpFile
                {
                    OrganizationUnitId = bagianId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/cpdp/{bagianId}/{safeName}",
                    FileSizeBytes = file.Length,
                    FileType = fileExtension.TrimStart('.'),
                    Keterangan = keterangan,
                    UploadedAt = DateTimeOffset.UtcNow,
                    UploaderName = (currentUser as ApplicationUser)?.FullName ?? currentUser?.UserName ?? "Unknown",
                    IsArchived = false
                };
                _context.CpdpFiles.Add(cpdpFile);
                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var uploadActorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        uploadActorName,
                        "UploadCPDPFile",
                        $"Uploaded CPDP file '{file.FileName}' ({file.Length} bytes) to bagian {bagian.Name} [BagianId={bagianId}]",
                        cpdpFile.Id,
                        "CpdpFile");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit log write failed for CpdpUpload");
                }

                TempData["Success"] = $"File '{file.FileName}' berhasil di-upload ke bagian {bagian.Name}.";
                return RedirectToAction("CpdpFiles", new { bagian = bagianId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload CPDP file for bagian {BagianId}", bagianId);
                TempData["Error"] = "Gagal menyimpan file. Silakan coba lagi.";
                return RedirectToAction("CpdpUpload", new { bagianId });
            }
        }

        // GET /Admin/CpdpFileDownload/{id}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CpdpFileDownload(int id)
        {
            var cpdpFile = await _context.CpdpFiles
                .Include(f => f.OrganizationUnit)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (cpdpFile == null) return NotFound();

            var physicalPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                cpdpFile.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            var contentType = cpdpFile.FileType switch
            {
                "pdf"  => "application/pdf",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "xls"  => "application/vnd.ms-excel",
                _      => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            return File(fileBytes, contentType, cpdpFile.FileName);
        }

        // POST /Admin/CpdpFileArchive
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CpdpFileArchive(int id)
        {
            var cpdpFile = await _context.CpdpFiles.FindAsync(id);
            if (cpdpFile == null) return Json(new { success = false, message = "File tidak ditemukan." });

            // Soft delete: archive the file (moves to history view, physical file retained)
            string cpdpFileName = cpdpFile.FileName;
            int cpdpBagianId = cpdpFile.OrganizationUnitId;
            cpdpFile.IsArchived = true;
            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var archiveUser = await _userManager.GetUserAsync(User);
                var archiveActorName = string.IsNullOrWhiteSpace(archiveUser?.NIP) ? (archiveUser?.FullName ?? "Unknown") : $"{archiveUser.NIP} - {archiveUser.FullName}";
                await _auditLog.LogAsync(
                    archiveUser?.Id ?? "",
                    archiveActorName,
                    "ArchiveCPDPFile",
                    $"Archived CPDP file '{cpdpFileName}' [ID={id}] in bagian {cpdpBagianId}",
                    id,
                    "CpdpFile");
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "Audit log write failed for CpdpFileArchive {Id}", id);
            }

            return Json(new { success = true, message = "File berhasil diarsipkan." });
        }

        // GET /Admin/CpdpFileHistory/{bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CpdpFileHistory(int bagianId)
        {
            var bagian = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagian == null) return NotFound();

            var archivedFiles = await _context.CpdpFiles
                .Where(f => f.OrganizationUnitId == bagianId && f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            ViewData["Title"] = $"Riwayat File CPDP — {bagian.Name}";
            ViewBag.Bagian = bagian;
            ViewBag.ArchivedFiles = archivedFiles;
            return View();
        }

        #endregion



        private async Task SetTrainingCategoryViewBag()
        {
            var allCats = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            ViewBag.KategoriOptions = allCats
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToList();

            ViewBag.SubKategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId != null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
        }

        // --- TRAINING CRUD (moved from CMPController, redirects to ManageAssessment?tab=training) ---

        // GET /Admin/AddTraining
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddTraining(
            [FromQuery] List<int>? renewSessionId = null,
            [FromQuery] List<int>? renewTrainingId = null)
        {
            var workers = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.NIP })
                .ToListAsync();

            ViewBag.Workers = workers.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = w.Id,
                Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
            }).ToList();

            bool isRenewalMode = false;
            var model = new CreateTrainingRecordViewModel();

            if (renewTrainingId != null && renewTrainingId.Count > 0)
            {
                if (renewTrainingId.Count == 1)
                {
                    var src = await _context.TrainingRecords.Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == renewTrainingId[0]);
                    if (src == null)
                    {
                        TempData["Warning"] = "Training record asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    model.Judul = src.Judul ?? "";
                    model.Kategori = src.Kategori ?? "";
                    model.RenewsTrainingId = src.Id;
                    ViewBag.RenewalSourceTitle = src.Judul ?? "";
                    ViewBag.RenewalSourceUserName = src.User?.FullName ?? "";
                    ViewBag.SelectedUserId = src.UserId;
                }
                else
                {
                    var srcs = await _context.TrainingRecords.Include(t => t.User)
                        .Where(t => renewTrainingId.Contains(t.Id)).ToListAsync();
                    if (srcs.Count == 0)
                    {
                        TempData["Warning"] = "Training record asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    var first = srcs[0];
                    model.Judul = first.Judul ?? "";
                    model.Kategori = first.Kategori ?? "";
                    ViewBag.RenewalSourceTitle = first.Judul ?? "";
                    ViewBag.RenewalSourceUserName = string.Join(", ", srcs.Select(t => t.User?.FullName ?? ""));
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(
                        srcs.ToDictionary(t => t.UserId ?? "", t => t.Id));
                    ViewBag.RenewalFkMapType = "training";
                    ViewBag.SelectedUserIds = srcs.Select(t => t.UserId).ToList();
                }
            }
            else if (renewSessionId != null && renewSessionId.Count > 0)
            {
                // Build category lookup for MapKategori DB lookup (LDAT-05)
                var catsForSessionRenewal = (await _context.AssessmentCategories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .ToListAsync())
                    .GroupBy(c => c.Name.ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Name);
                if (!catsForSessionRenewal.ContainsKey("MANDATORY")) catsForSessionRenewal["MANDATORY"] = "Mandatory HSSE Training";
                if (!catsForSessionRenewal.ContainsKey("PROTON")) catsForSessionRenewal["PROTON"] = "Assessment Proton";

                if (renewSessionId.Count == 1)
                {
                    var src = await _context.AssessmentSessions.Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == renewSessionId[0]);
                    if (src == null)
                    {
                        TempData["Warning"] = "Assessment session asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    model.Judul = src.Title ?? "";
                    model.Kategori = MapKategori(src.Category, catsForSessionRenewal);
                    model.RenewsSessionId = src.Id;
                    ViewBag.RenewalSourceTitle = src.Title ?? "";
                    ViewBag.RenewalSourceUserName = src.User?.FullName ?? "";
                    ViewBag.SelectedUserId = src.UserId;
                }
                else
                {
                    var srcs = await _context.AssessmentSessions.Include(s => s.User)
                        .Where(s => renewSessionId.Contains(s.Id)).ToListAsync();
                    if (srcs.Count == 0)
                    {
                        TempData["Warning"] = "Assessment session asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    var first = srcs[0];
                    model.Judul = first.Title ?? "";
                    model.Kategori = MapKategori(first.Category, catsForSessionRenewal);
                    ViewBag.RenewalSourceTitle = first.Title ?? "";
                    ViewBag.RenewalSourceUserName = string.Join(", ", srcs.Select(s => s.User?.FullName ?? ""));
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(
                        srcs.ToDictionary(s => s.UserId, s => s.Id));
                    ViewBag.RenewalFkMapType = "session";
                    ViewBag.SelectedUserIds = srcs.Select(s => s.UserId).ToList();
                }
            }

            ViewBag.IsRenewalMode = isRenewalMode;
            await SetTrainingCategoryViewBag();
            return View(model);
        }

        // POST /Admin/AddTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddTraining(CreateTrainingRecordViewModel model)
        {
            // Manual validation: minimal 1 pekerja harus dipilih
            bool hasWorkerCerts = model.WorkerCerts != null && model.WorkerCerts.Count > 0;
            bool hasSingleUser = !string.IsNullOrEmpty(model.UserId);
            if (!hasWorkerCerts && !hasSingleUser)
            {
                ModelState.AddModelError("", "Pilih minimal 1 pekerja.");
            }
            if (hasWorkerCerts && model.WorkerCerts!.Count > 20)
            {
                ModelState.AddModelError("", "Maksimal 20 pekerja per submission.");
            }

            // File validation
            string? sertifikatUrl = null;
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("CertificateFile", "Hanya file PDF, JPG, dan PNG yang diperbolehkan.");
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("CertificateFile", "Ukuran file maksimal 10MB.");
                }
            }

            // Per-worker file validation (all-or-nothing)
            if (hasWorkerCerts)
            {
                var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                foreach (var wc in model.WorkerCerts!)
                {
                    if (wc.CertificateFile != null && wc.CertificateFile.Length > 0)
                    {
                        var wcExt = Path.GetExtension(wc.CertificateFile.FileName).ToLowerInvariant();
                        if (!allowedExts.Contains(wcExt))
                            ModelState.AddModelError("", $"File untuk pekerja {wc.UserId} harus berformat PDF, JPG, atau PNG.");
                        if (wc.CertificateFile.Length > 10 * 1024 * 1024)
                            ModelState.AddModelError("", $"File untuk pekerja {wc.UserId} melebihi batas 10MB.");
                    }
                }
            }

            // Baca bulk renewal params
            var fkMapJson = Request.Form["renewalFkMap"].FirstOrDefault();
            var fkMapType = Request.Form["renewalFkMapType"].FirstOrDefault();
            Dictionary<string, int>? fkMap = null;
            if (!string.IsNullOrEmpty(fkMapJson))
                fkMap = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(fkMapJson);

            var userIdsJson = Request.Form["UserIds"].FirstOrDefault();
            List<string>? bulkUserIds = null;
            if (!string.IsNullOrEmpty(userIdsJson))
                bulkUserIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(userIdsJson);

            // FK mutual exclusion (per D-04): AddTraining hanya boleh salah satu FK
            if (model.RenewsTrainingId.HasValue && model.RenewsSessionId.HasValue)
            {
                ModelState.AddModelError("", "Renewal FK tidak valid: hanya boleh mengisi salah satu dari RenewsTrainingId atau RenewsSessionId.");
            }
            // Double renewal prevention (per D-10)
            if (model.RenewsTrainingId.HasValue)
            {
                var srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(t => t.RenewsTrainingId == model.RenewsTrainingId)
                    || await _context.AssessmentSessions.AnyAsync(a => a.RenewsTrainingId == model.RenewsTrainingId && a.IsPassed == true);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            if (model.RenewsSessionId.HasValue)
            {
                var srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(t => t.RenewsSessionId == model.RenewsSessionId)
                    || await _context.AssessmentSessions.AnyAsync(a => a.RenewsSessionId == model.RenewsSessionId && a.IsPassed == true);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            // Mixed-type bulk validation (per D-11, EDGE-01)
            if (fkMap != null && bulkUserIds != null && bulkUserIds.Count > 1)
            {
                if (string.IsNullOrEmpty(fkMapType) || (fkMapType != "training" && fkMapType != "session"))
                {
                    ModelState.AddModelError("", "Bulk renewal tidak dapat mencampur tipe Assessment dan Training. Renew per tipe secara terpisah.");
                }
            }

            if (!ModelState.IsValid)
            {
                var workersList = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, u.FullName, u.NIP })
                    .ToListAsync();
                ViewBag.Workers = workersList.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = w.Id,
                    Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
                }).ToList();
                ViewBag.IsRenewalMode = (model.RenewsTrainingId != null || model.RenewsSessionId != null || !string.IsNullOrEmpty(fkMapJson));
                if (model.RenewsTrainingId != null)
                {
                    var src = await _context.TrainingRecords.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == model.RenewsTrainingId);
                    if (src != null) { ViewBag.RenewalSourceTitle = src.Judul ?? ""; ViewBag.RenewalSourceUserName = src.User?.FullName ?? ""; }
                }
                else if (model.RenewsSessionId != null)
                {
                    var src = await _context.AssessmentSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == model.RenewsSessionId);
                    if (src != null) { ViewBag.RenewalSourceTitle = src.Title ?? ""; ViewBag.RenewalSourceUserName = src.User?.FullName ?? ""; }
                }
                await SetTrainingCategoryViewBag();
                return View(model);
            }

            // Handle file upload
            var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
            if (uploadedUrl != null)
            {
                sertifikatUrl = uploadedUrl;
            }

            // Multi-select non-renewal: create 1 record per worker with per-worker cert
            if (hasWorkerCerts && bulkUserIds == null)
            {
                foreach (var wc in model.WorkerCerts!)
                {
                    string? wcUrl = null;
                    if (wc.CertificateFile != null && wc.CertificateFile.Length > 0)
                        wcUrl = await FileUploadHelper.SaveFileAsync(wc.CertificateFile, _env.WebRootPath, "uploads/certificates");

                    var rec = new TrainingRecord
                    {
                        UserId = wc.UserId,
                        Judul = model.Judul,
                        Penyelenggara = model.Penyelenggara,
                        Kota = model.Kota,
                        Kategori = model.Kategori,
                        SubKategori = model.SubKategori,
                        Tanggal = model.Tanggal,
                        TanggalMulai = model.TanggalMulai,
                        TanggalSelesai = model.TanggalSelesai,
                        Status = model.Status,
                        NomorSertifikat = wc.NomorSertifikat,
                        ValidUntil = model.ValidUntil,
                        CertificateType = model.CertificateType,
                        SertifikatUrl = wcUrl
                    };
                    _context.TrainingRecords.Add(rec);
                }
                await _context.SaveChangesAsync();

                var multiActor = await _userManager.GetUserAsync(User);
                if (multiActor != null)
                    await _auditLog.LogAsync(multiActor.Id, multiActor.FullName, "Create",
                        $"Training record ditambahkan: {model.Judul} untuk {model.WorkerCerts!.Count} pekerja", 0, "TrainingRecord");

                TempData["Success"] = $"Berhasil membuat {model.WorkerCerts!.Count} training record.";
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            // Bulk renewal: buat N TrainingRecord dengan FK per-user
            if (bulkUserIds != null && bulkUserIds.Count > 1)
            {
                foreach (var uid in bulkUserIds)
                {
                    var rec = new TrainingRecord
                    {
                        UserId = uid,
                        Judul = model.Judul,
                        Penyelenggara = model.Penyelenggara,
                        Kota = model.Kota,
                        Kategori = model.Kategori,
                        SubKategori = model.SubKategori,
                        Tanggal = model.Tanggal,
                        TanggalMulai = model.TanggalMulai,
                        TanggalSelesai = model.TanggalSelesai,
                        Status = model.Status,
                        NomorSertifikat = model.NomorSertifikat,
                        ValidUntil = model.ValidUntil,
                        CertificateType = model.CertificateType,
                        SertifikatUrl = sertifikatUrl
                    };

                    if (fkMap != null && fkMap.TryGetValue(uid, out var fkId))
                    {
                        if (fkMapType == "training") rec.RenewsTrainingId = fkId;
                        else rec.RenewsSessionId = fkId;
                    }

                    _context.TrainingRecords.Add(rec);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Berhasil membuat {bulkUserIds.Count} training record (renewal).";
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            var record = new TrainingRecord
            {
                UserId = model.UserId,
                Judul = model.Judul,
                Penyelenggara = model.Penyelenggara,
                Kota = model.Kota,
                Kategori = model.Kategori,
                SubKategori = model.SubKategori,
                Tanggal = model.Tanggal,
                TanggalMulai = model.TanggalMulai,
                TanggalSelesai = model.TanggalSelesai,
                Status = model.Status,
                NomorSertifikat = model.NomorSertifikat,
                ValidUntil = model.ValidUntil,
                CertificateType = model.CertificateType,
                SertifikatUrl = sertifikatUrl,
                RenewsTrainingId = model.RenewsTrainingId,
                RenewsSessionId = model.RenewsSessionId
            };

            _context.TrainingRecords.Add(record);
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            var workerName = (await _context.Users.FindAsync(model.UserId))?.FullName ?? model.UserId;
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Create",
                    $"Training record ditambahkan: {model.Judul} untuk {workerName}", record.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil dibuat.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // GET /Admin/EditTraining/{id}
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditTraining(int id)
        {
            var record = await _context.TrainingRecords
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (record == null) return NotFound();

            var model = new EditTrainingRecordViewModel
            {
                Id = record.Id,
                WorkerId = record.UserId,
                WorkerName = record.User?.FullName ?? "",
                Judul = record.Judul,
                Penyelenggara = record.Penyelenggara,
                Kota = record.Kota,
                Kategori = record.Kategori,
                SubKategori = record.SubKategori,
                Tanggal = record.Tanggal,
                TanggalMulai = record.TanggalMulai,
                TanggalSelesai = record.TanggalSelesai,
                Status = record.Status,
                NomorSertifikat = record.NomorSertifikat,
                ValidUntil = record.ValidUntil,
                CertificateType = record.CertificateType,
                ExistingSertifikatUrl = record.SertifikatUrl,
            };
            await SetTrainingCategoryViewBag();
            return View(model);
        }

        // POST /Admin/EditTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditTraining(EditTrainingRecordViewModel model)
        {
            // File validation
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Hanya file PDF, JPG, dan PNG yang diperbolehkan.";
                    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Ukuran file maksimal 10MB.";
                    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
                }
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            var record = await _context.TrainingRecords.FindAsync(model.Id);
            if (record == null) return NotFound();

            // Handle file upload — replace old file if new file provided
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(record.SertifikatUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
                if (uploadedUrl != null) record.SertifikatUrl = uploadedUrl;
            }

            record.Judul = model.Judul;
            record.Penyelenggara = model.Penyelenggara;
            record.Kota = model.Kota;
            record.Kategori = model.Kategori;
            record.SubKategori = model.SubKategori;
            record.Tanggal = model.Tanggal;
            record.TanggalMulai = model.TanggalMulai;
            record.TanggalSelesai = model.TanggalSelesai;
            record.Status = model.Status;
            record.NomorSertifikat = model.NomorSertifikat;
            record.ValidUntil = model.ValidUntil;
            record.CertificateType = model.CertificateType;

            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Update",
                    $"Training record diperbarui: {model.Judul}", model.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil diperbarui.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // POST /Admin/DeleteTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DeleteTraining(int id)
        {
            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();

            if (!string.IsNullOrEmpty(record.SertifikatUrl))
            {
                var path = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            var actor = await _userManager.GetUserAsync(User);
            _context.TrainingRecords.Remove(record);
            await _context.SaveChangesAsync();

            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                    $"Training record dihapus: {record.Judul}", record.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil dihapus.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // --- IMPORT TRAINING (moved from CMPController, Phase 198) ---

        // GET /Admin/DownloadImportTrainingTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadImportTrainingTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import Training");

            var headers = new[] {
                "NIP", "Judul", "Kategori", "SubKategori (opsional)",
                "Tanggal (YYYY-MM-DD)", "TanggalMulai (YYYY-MM-DD, opsional)", "TanggalSelesai (YYYY-MM-DD, opsional)",
                "Penyelenggara", "Kota (opsional)",
                "Status", "ValidUntil (YYYY-MM-DD, opsional)", "NomorSertifikat (opsional)"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Example row
            ws.Cell(2, 1).Value = "123456";
            ws.Cell(2, 2).Value = "Pelatihan K3 Dasar";
            ws.Cell(2, 3).Value = "MANDATORY";
            ws.Cell(2, 4).Value = "";
            ws.Cell(2, 5).Value = "2024-03-15";
            ws.Cell(2, 6).Value = "2024-03-15";
            ws.Cell(2, 7).Value = "2024-03-17";
            ws.Cell(2, 8).Value = "Internal";
            ws.Cell(2, 9).Value = "Balikpapan";
            ws.Cell(2, 10).Value = "Passed";
            ws.Cell(2, 11).Value = "2027-03-15";
            ws.Cell(2, 12).Value = "CERT-001";
            for (int i = 1; i <= 12; i++)
            {
                ws.Cell(2, i).Style.Font.Italic = true;
                ws.Cell(2, i).Style.Font.FontColor = XLColor.Gray;
            }

            ws.Cell(3, 1).Value = "Kolom Kategori: PROTON / OJT / MANDATORY";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

            ws.Cell(4, 1).Value = "Kolom Status: Passed / Valid / Expired / Failed";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;

            return ExcelExportHelper.ToFileResult(workbook, "training_import_template.xlsx", this);
        }

        // GET /Admin/ImportTraining
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult ImportTraining()
        {
            return View(new List<HcPortal.Models.ImportTrainingResult>());
        }

        // POST /Admin/ImportTraining
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTraining(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Pilih file Excel terlebih dahulu.";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            const long maxSize = 10 * 1024 * 1024;
            if (excelFile.Length > maxSize)
            {
                TempData["Error"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            var results = new List<HcPortal.Models.ImportTrainingResult>();

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nip             = row.Cell(1).GetString().Trim();
                    var judul           = row.Cell(2).GetString().Trim();
                    var kategori        = row.Cell(3).GetString().Trim();
                    var subKategori     = row.Cell(4).GetString().Trim();
                    var tanggalStr      = row.Cell(5).GetString().Trim();
                    var tanggalMulaiStr = row.Cell(6).GetString().Trim();
                    var tanggalSelesaiStr = row.Cell(7).GetString().Trim();
                    var penyelenggara   = row.Cell(8).GetString().Trim();
                    var kota            = row.Cell(9).GetString().Trim();
                    var status          = row.Cell(10).GetString().Trim();
                    var validUntilStr   = row.Cell(11).GetString().Trim();
                    var nomorSertifikat = row.Cell(12).GetString().Trim();

                    // Skip completely blank rows
                    if (string.IsNullOrWhiteSpace(nip) && string.IsNullOrWhiteSpace(judul)) continue;

                    var result = new HcPortal.Models.ImportTrainingResult { NIP = nip, Judul = judul };

                    if (string.IsNullOrWhiteSpace(nip))
                    {
                        result.Status = "Error";
                        result.Message = "NIP tidak boleh kosong";
                        results.Add(result);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(judul))
                    {
                        result.Status = "Error";
                        result.Message = "Judul tidak boleh kosong";
                        results.Add(result);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(tanggalStr) || !DateTime.TryParse(tanggalStr, out var parsedDate))
                    {
                        result.Status = "Error";
                        result.Message = "Format Tanggal tidak valid (YYYY-MM-DD)";
                        results.Add(result);
                        continue;
                    }

                    var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.NIP == nip);
                    if (targetUser == null)
                    {
                        result.Status = "Error";
                        result.Message = $"NIP '{nip}' tidak ditemukan dalam sistem";
                        results.Add(result);
                        continue;
                    }

                    try
                    {
                        var record = new HcPortal.Models.TrainingRecord
                        {
                            UserId = targetUser.Id,
                            Judul = judul,
                            Kategori = kategori,
                            SubKategori = string.IsNullOrWhiteSpace(subKategori) ? null : subKategori,
                            Tanggal = parsedDate,
                            TanggalMulai = DateTime.TryParse(tanggalMulaiStr, out var tm) ? tm : (DateTime?)null,
                            TanggalSelesai = DateTime.TryParse(tanggalSelesaiStr, out var ts) ? ts : (DateTime?)null,
                            Penyelenggara = penyelenggara,
                            Kota = string.IsNullOrWhiteSpace(kota) ? null : kota,
                            Status = status,
                            ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : (DateTime?)null,
                            NomorSertifikat = nomorSertifikat
                        };
                        _context.TrainingRecords.Add(record);
                        await _context.SaveChangesAsync();
                        result.Status = "Success";
                        result.Message = $"Training record berhasil dibuat untuk {targetUser.FullName}";
                    }
                    catch (Exception ex)
                    {
                        result.Status = "Error";
                        result.Message = $"Gagal menyimpan: {ex.Message}";
                    }

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read Excel import file for training");
                TempData["Error"] = $"Gagal memproses file: {ex.Message}";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            return View(results);
        }

        // Question Management (Admin) region removed in Phase 227 (CLEN-02)
        // Package Management region moved to AssessmentAdminController (Phase 287)



        // Certificate Helpers moved to Helpers/CertNumberHelper.cs (Phase 227 CLEN-04)

        #region Renewal Certificate

        private async Task<List<SertifikatRow>> BuildRenewalRowsAsync()
        {
            // Query TrainingRecords with certificate (no role scoping — Admin/HC full access)
            var trainingAnon = await _context.TrainingRecords
                .Include(t => t.User)
                .Where(t => t.SertifikatUrl != null)
                .Select(t => new
                {
                    t.Id,
                    UserId = t.User != null ? t.User.Id : "",
                    NamaWorker = t.User != null ? t.User.FullName : "",
                    Bagian = t.User != null ? t.User.Section : null,
                    Unit = t.User != null ? t.User.Unit : null,
                    Judul = t.Judul ?? "",
                    t.Kategori,
                    t.NomorSertifikat,
                    TanggalTerbit = (DateTime?)t.Tanggal,
                    t.ValidUntil,
                    t.CertificateType,
                    t.SertifikatUrl
                })
                .ToListAsync();

            // ===== Renewal chain resolution: batch lookup =====
            // Set 1: AS IDs renewed by another AS (IsPassed==true)
            var renewedByAsSessionIds = await _context.AssessmentSessions
                .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsSessionId!.Value)
                .Distinct()
                .ToListAsync();

            // Set 2: AS IDs renewed by a TR
            var renewedByTrSessionIds = await _context.TrainingRecords
                .Where(t => t.RenewsSessionId.HasValue)
                .Select(t => t.RenewsSessionId!.Value)
                .Distinct()
                .ToListAsync();

            // Set 3: TR IDs renewed by an AS (IsPassed==true)
            var renewedByAsTrainingIds = await _context.AssessmentSessions
                .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsTrainingId!.Value)
                .Distinct()
                .ToListAsync();

            // Set 4: TR IDs renewed by another TR
            var renewedByTrTrainingIds = await _context.TrainingRecords
                .Where(t => t.RenewsTrainingId.HasValue)
                .Select(t => t.RenewsTrainingId!.Value)
                .Distinct()
                .ToListAsync();

            // Merge: all AS IDs that have been renewed
            var renewedAssessmentSessionIds = new HashSet<int>(renewedByAsSessionIds);
            renewedAssessmentSessionIds.UnionWith(renewedByTrSessionIds);

            // Merge: all TR IDs that have been renewed
            var renewedTrainingRecordIds = new HashSet<int>(renewedByAsTrainingIds);
            renewedTrainingRecordIds.UnionWith(renewedByTrTrainingIds);

            // Build rawToDisplayMap from AssessmentCategories for MapKategori DB lookup
            var allCategories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
            var rawToDisplayMap = allCategories
                .Where(c => c.ParentId == null)
                .GroupBy(c => c.Name.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().Name);
            if (!rawToDisplayMap.ContainsKey("MANDATORY"))
                rawToDisplayMap["MANDATORY"] = "Mandatory HSSE Training";
            if (!rawToDisplayMap.ContainsKey("PROTON"))
                rawToDisplayMap["PROTON"] = "Assessment Proton";

            var trainingRows = trainingAnon.Select(t => new SertifikatRow
            {
                SourceId = t.Id,
                RecordType = RecordType.Training,
                WorkerId = t.UserId,
                NamaWorker = t.NamaWorker,
                Bagian = t.Bagian,
                Unit = t.Unit,
                Judul = t.Judul,
                Kategori = MapKategori(t.Kategori, rawToDisplayMap),
                SubKategori = null,
                NomorSertifikat = t.NomorSertifikat,
                TanggalTerbit = t.TanggalTerbit,
                ValidUntil = t.ValidUntil,
                Status = SertifikatRow.DeriveCertificateStatus(t.ValidUntil, t.CertificateType),
                SertifikatUrl = t.SertifikatUrl,
                IsRenewed = renewedTrainingRecordIds.Contains(t.Id)
            }).ToList();

            // Query AssessmentSessions with certificate
            var categoryById = allCategories.ToDictionary(c => c.Id);
            var categoryNameLookup = allCategories
                .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => categoryById[g.First().ParentId!.Value].Name);

            var assessmentAnon = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.GenerateCertificate && a.IsPassed == true)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    NamaWorker = a.User != null ? a.User.FullName : "",
                    Bagian = a.User != null ? a.User.Section : null,
                    Unit = a.User != null ? a.User.Unit : null,
                    a.Title,
                    a.Category,
                    a.NomorSertifikat,
                    a.CompletedAt,
                    a.ValidUntil
                })
                .ToListAsync();

            var assessmentRows = assessmentAnon.Select(a =>
            {
                string kategori = a.Category;
                string? subKategori = null;
                if (categoryNameLookup.TryGetValue(a.Category, out var parentName))
                {
                    kategori = parentName;
                    subKategori = a.Category;
                }
                return new SertifikatRow
                {
                    SourceId = a.Id,
                    RecordType = RecordType.Assessment,
                    WorkerId = a.UserId,
                    NamaWorker = a.NamaWorker,
                    Bagian = a.Bagian,
                    Unit = a.Unit,
                    Judul = a.Title,
                    Kategori = kategori,
                    SubKategori = subKategori,
                    NomorSertifikat = a.NomorSertifikat,
                    TanggalTerbit = a.CompletedAt,
                    ValidUntil = a.ValidUntil,
                    Status = SertifikatRow.DeriveCertificateStatus(a.ValidUntil, null),
                    SertifikatUrl = null,
                    IsRenewed = renewedAssessmentSessionIds.Contains(a.Id)
                };
            }).ToList();

            // Merge all rows
            var rows = new List<SertifikatRow>(trainingRows.Count + assessmentRows.Count);
            rows.AddRange(trainingRows);
            rows.AddRange(assessmentRows);

            // POST-FILTER: hanya Expired/AkanExpired yang belum di-renew
            rows = rows
                .Where(r => !r.IsRenewed && (r.Status == CertificateStatus.Expired || r.Status == CertificateStatus.AkanExpired))
                .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                .ToList();

            return rows;
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CertificateHistory(string workerId, string mode = "readonly")
        {
            if (string.IsNullOrEmpty(workerId))
                return BadRequest("workerId required");

            // 1. Query semua sertifikat pekerja ini
            var trainingCerts = await _context.TrainingRecords
                .Where(t => t.UserId == workerId && t.SertifikatUrl != null)
                .Select(t => new {
                    t.Id,
                    Judul = t.Judul ?? "",
                    t.Kategori,
                    t.NomorSertifikat,
                    TanggalTerbit = (DateTime?)t.Tanggal,
                    t.ValidUntil,
                    t.CertificateType,
                    t.SertifikatUrl,
                    t.RenewsSessionId,
                    t.RenewsTrainingId
                })
                .ToListAsync();

            var assessmentCerts = await _context.AssessmentSessions
                .Where(a => a.UserId == workerId && a.GenerateCertificate && a.IsPassed == true)
                .Select(a => new {
                    a.Id,
                    Judul = a.Title,
                    a.Category,
                    a.NomorSertifikat,
                    TanggalTerbit = a.CompletedAt,
                    a.ValidUntil,
                    a.RenewsSessionId,
                    a.RenewsTrainingId
                })
                .ToListAsync();

            // 2. Category resolution
            var allCategories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
            var categoryById = allCategories.ToDictionary(c => c.Id);
            var categoryNameLookup = allCategories
                .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => categoryById[g.First().ParentId!.Value].Name);
            var rawToDisplayMapHist = allCategories
                .Where(c => c.ParentId == null)
                .GroupBy(c => c.Name.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().Name);
            if (!rawToDisplayMapHist.ContainsKey("MANDATORY")) rawToDisplayMapHist["MANDATORY"] = "Mandatory HSSE Training";
            if (!rawToDisplayMapHist.ContainsKey("PROTON")) rawToDisplayMapHist["PROTON"] = "Assessment Proton";

            // 3. Renewal chain batch lookup — scoped to this worker's certs
            var mySessionIds = assessmentCerts.Select(a => a.Id).ToHashSet();
            var myTrainingIds = trainingCerts.Select(t => t.Id).ToHashSet();

            var renewedSessionIds = new HashSet<int>(
                await _context.AssessmentSessions
                    .Where(a => a.RenewsSessionId.HasValue && mySessionIds.Contains(a.RenewsSessionId.Value) && a.IsPassed == true)
                    .Select(a => a.RenewsSessionId!.Value).ToListAsync());
            renewedSessionIds.UnionWith(
                await _context.TrainingRecords
                    .Where(t => t.RenewsSessionId.HasValue && mySessionIds.Contains(t.RenewsSessionId.Value))
                    .Select(t => t.RenewsSessionId!.Value).ToListAsync());

            var renewedTrainingIds = new HashSet<int>(
                await _context.AssessmentSessions
                    .Where(a => a.RenewsTrainingId.HasValue && myTrainingIds.Contains(a.RenewsTrainingId.Value) && a.IsPassed == true)
                    .Select(a => a.RenewsTrainingId!.Value).ToListAsync());
            renewedTrainingIds.UnionWith(
                await _context.TrainingRecords
                    .Where(t => t.RenewsTrainingId.HasValue && myTrainingIds.Contains(t.RenewsTrainingId.Value))
                    .Select(t => t.RenewsTrainingId!.Value).ToListAsync());

            // 4. Build SertifikatRow list
            var rows = new List<SertifikatRow>();

            foreach (var t in trainingCerts)
            {
                rows.Add(new SertifikatRow
                {
                    SourceId = t.Id,
                    RecordType = RecordType.Training,
                    WorkerId = workerId,
                    Judul = t.Judul,
                    Kategori = MapKategori(t.Kategori, rawToDisplayMapHist),
                    SubKategori = null,
                    NomorSertifikat = t.NomorSertifikat,
                    TanggalTerbit = t.TanggalTerbit,
                    ValidUntil = t.ValidUntil,
                    Status = SertifikatRow.DeriveCertificateStatus(t.ValidUntil, t.CertificateType),
                    SertifikatUrl = t.SertifikatUrl,
                    IsRenewed = renewedTrainingIds.Contains(t.Id)
                });
            }

            foreach (var a in assessmentCerts)
            {
                string kategori = a.Category;
                string? subKategori = null;
                if (categoryNameLookup.TryGetValue(a.Category, out var parentName))
                {
                    kategori = parentName;
                    subKategori = a.Category;
                }
                rows.Add(new SertifikatRow
                {
                    SourceId = a.Id,
                    RecordType = RecordType.Assessment,
                    WorkerId = workerId,
                    Judul = a.Judul,
                    Kategori = kategori,
                    SubKategori = subKategori,
                    NomorSertifikat = a.NomorSertifikat,
                    TanggalTerbit = a.TanggalTerbit,
                    ValidUntil = a.ValidUntil,
                    Status = SertifikatRow.DeriveCertificateStatus(a.ValidUntil, null),
                    IsRenewed = renewedSessionIds.Contains(a.Id)
                });
            }

            // 5. Build renewal chain graph using Union-Find
            var parent = new Dictionary<string, string>();
            string Find(string x) {
                if (!parent.ContainsKey(x)) parent[x] = x;
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(string a, string b) {
                var ra = Find(a); var rb = Find(b);
                if (ra != rb) parent[ra] = rb;
            }

            // Register all cert nodes
            foreach (var r in rows)
                Find(r.RecordType == RecordType.Assessment ? $"AS:{r.SourceId}" : $"TR:{r.SourceId}");

            // Build edges from renewal FKs
            foreach (var a in assessmentCerts)
            {
                var key = $"AS:{a.Id}";
                if (a.RenewsSessionId.HasValue) Union(key, $"AS:{a.RenewsSessionId.Value}");
                if (a.RenewsTrainingId.HasValue) Union(key, $"TR:{a.RenewsTrainingId.Value}");
            }
            foreach (var t in trainingCerts)
            {
                var key = $"TR:{t.Id}";
                if (t.RenewsSessionId.HasValue) Union(key, $"AS:{t.RenewsSessionId.Value}");
                if (t.RenewsTrainingId.HasValue) Union(key, $"TR:{t.RenewsTrainingId.Value}");
            }

            // Group rows by chain
            var groups = rows
                .GroupBy(r => Find(r.RecordType == RecordType.Assessment ? $"AS:{r.SourceId}" : $"TR:{r.SourceId}"))
                .Select(g =>
                {
                    var certs = g.OrderByDescending(c => c.ValidUntil ?? DateTime.MaxValue).ToList();
                    var oldest = g.OrderBy(c => c.ValidUntil ?? DateTime.MaxValue).First();
                    var chainTitle = !string.IsNullOrEmpty(oldest.SubKategori) ? oldest.SubKategori
                                   : !string.IsNullOrEmpty(oldest.Kategori) ? oldest.Kategori
                                   : oldest.Judul;
                    return new CertificateChainGroup
                    {
                        ChainTitle = chainTitle,
                        Certificates = certs,
                        LatestValidUntil = certs.First().ValidUntil
                    };
                })
                .OrderByDescending(g => g.LatestValidUntil ?? DateTime.MaxValue)
                .ToList();

            ViewBag.Mode = mode;
            return PartialView("~/Views/Shared/_CertificateHistoryModalContent.cshtml", groups);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> RenewalCertificate(int page = 1)
        {
            var allRows = await BuildRenewalRowsAsync();

            var vm = new CertificationManagementViewModel
            {
                TotalCount = allRows.Count,
                ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
                AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired),

            };

            ViewBag.AllBagian = await _context.GetAllSectionsAsync();

            ViewBag.AllCategories = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedView = "RenewalCertificate";

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> FilterRenewalCertificate(
            string? bagian = null,
            string? unit = null,
            string? status = null,
            string? category = null,
            string? subCategory = null,
            string? tipe = null,
            int page = 1)
        {
            var allRows = await BuildRenewalRowsAsync();

            if (!string.IsNullOrEmpty(bagian))
                allRows = allRows.Where(r => r.Bagian == bagian).ToList();
            if (!string.IsNullOrEmpty(unit))
                allRows = allRows.Where(r => r.Unit == unit).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
                allRows = allRows.Where(r => r.Status == st).ToList();
            if (!string.IsNullOrEmpty(category))
                allRows = allRows.Where(r => r.Kategori == category).ToList();
            if (!string.IsNullOrEmpty(subCategory))
                allRows = allRows.Where(r => r.SubKategori == subCategory).ToList();
            if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
                allRows = allRows.Where(r => r.RecordType == rt).ToList();

            allRows = allRows
                .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                .ToList();

            // Group by judul sertifikat
            var grouped = allRows
                .GroupBy(r => r.Judul, StringComparer.OrdinalIgnoreCase)
                .Select(g => new RenewalGroup
                {
                    GroupKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g.Key))
                                     .Replace("+", "_").Replace("/", "-").Replace("=", ""),
                    Judul = g.Key,
                    Kategori = g.First().Kategori,
                    SubKategori = g.First().SubKategori,
                    TotalCount = g.Count(),
                    ExpiredCount = g.Count(r => r.Status == CertificateStatus.Expired),
                    AkanExpiredCount = g.Count(r => r.Status == CertificateStatus.AkanExpired),
                    MinValidUntil = g.Min(r => r.ValidUntil)
                })
                .OrderBy(g => g.MinValidUntil ?? DateTime.MaxValue)
                .ToList();

            foreach (var group in grouped)
            {
                var groupRows = allRows
                    .Where(r => string.Equals(r.Judul, group.Judul, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                    .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                    .ToList();
                var paging = PaginationHelper.Calculate(groupRows.Count, 1, group.PageSize);
                group.Rows = groupRows.Skip(paging.Skip).Take(paging.Take).ToList();
                group.CurrentPage = paging.CurrentPage;
                group.TotalPages = paging.TotalPages;
            }

            var gvm = new RenewalGroupViewModel
            {
                Groups = grouped,
                TotalExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
                TotalAkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired)
            };
            gvm.IsFiltered = !string.IsNullOrEmpty(bagian) || !string.IsNullOrEmpty(unit)
                || !string.IsNullOrEmpty(category) || !string.IsNullOrEmpty(subCategory)
                || !string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(tipe);

            return PartialView("Shared/_RenewalGroupedPartial", gvm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> FilterRenewalCertificateGroup(
            string groupKey,
            string judul,
            int page = 1,
            string? bagian = null, string? unit = null,
            string? status = null, string? category = null, string? subCategory = null,
            string? tipe = null)
        {
            judul = Uri.UnescapeDataString(judul ?? "");
            var allRows = await BuildRenewalRowsAsync();

            if (!string.IsNullOrEmpty(bagian))
                allRows = allRows.Where(r => r.Bagian == bagian).ToList();
            if (!string.IsNullOrEmpty(unit))
                allRows = allRows.Where(r => r.Unit == unit).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
                allRows = allRows.Where(r => r.Status == st).ToList();
            if (!string.IsNullOrEmpty(category))
                allRows = allRows.Where(r => r.Kategori == category).ToList();
            if (!string.IsNullOrEmpty(subCategory))
                allRows = allRows.Where(r => r.SubKategori == subCategory).ToList();
            if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
                allRows = allRows.Where(r => r.RecordType == rt).ToList();

            var groupRows = allRows
                .Where(r => string.Equals(r.Judul, judul, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                .ToList();

            var paging = PaginationHelper.Calculate(groupRows.Count, page, 10);
            var group = new RenewalGroup
            {
                GroupKey = groupKey,
                Judul = judul,
                Rows = groupRows.Skip(paging.Skip).Take(paging.Take).ToList(),
                CurrentPage = paging.CurrentPage,
                TotalPages = paging.TotalPages,
                TotalCount = groupRows.Count,
                ExpiredCount = groupRows.Count(r => r.Status == CertificateStatus.Expired),
                AkanExpiredCount = groupRows.Count(r => r.Status == CertificateStatus.AkanExpired)
            };

            return PartialView("Shared/_RenewalGroupTablePartial", group);
        }

        #endregion
    }
}
