using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Route("Admin/[action]")]
    public class DocumentAdminController : AdminBaseController
    {
        private readonly ILogger<DocumentAdminController> _logger;

        public DocumentAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<DocumentAdminController> logger)
            : base(context, userManager, auditLog, env)
        {
            _logger = logger;
        }

        // Override View resolution to use Views/Admin/ folder
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        #region KKJ File Management

        // GET /Admin/KkjMatrix?bagian={bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> KkjMatrix(int? bagian)
        {
            ViewData["Title"] = "Kelola KKJ Matrix";

            var bagians = await _context.OrganizationUnits
                .Where(u => u.ParentId == null)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            var files = await _context.KkjFiles
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            var filesByBagian = bagians.ToDictionary(
                b => b.Id,
                b => files.Where(f => f.OrganizationUnitId == b.Id).ToList()
            );

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

            var bagianEntity = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagianEntity == null)
            {
                TempData["Error"] = "Bagian tidak ditemukan.";
                return RedirectToAction("KkjUpload", new { bagianId });
            }

            try
            {
                var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kkj", bagianId.ToString());
                Directory.CreateDirectory(storageDir);

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
                        $"Uploaded KKJ file '{file.FileName}' ({file.Length} bytes) to bagian {bagianEntity.Name} [BagianId={bagianId}]",
                        kkjFile.Id,
                        "KkjFile");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit log write failed for KkjUpload");
                }

                TempData["Success"] = $"File '{file.FileName}' berhasil di-upload ke bagian {bagianEntity.Name}.";
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
            var bagianEntity = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagianEntity == null) return NotFound();

            var archivedFiles = await _context.KkjFiles
                .Where(f => f.OrganizationUnitId == bagianId && f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            ViewData["Title"] = $"Riwayat File — {bagianEntity.Name}";
            ViewBag.Bagian = bagianEntity;
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
            var bagianEntity = await _context.OrganizationUnits.FindAsync(id);
            if (bagianEntity == null)
                return Json(new { success = false, message = "Bagian tidak ditemukan." });

            var activeKkjCount = await _context.KkjFiles.CountAsync(f => f.OrganizationUnitId == id && !f.IsArchived);
            var activeCpdpCount = await _context.CpdpFiles.CountAsync(f => f.OrganizationUnitId == id && !f.IsArchived);
            var totalActive = activeKkjCount + activeCpdpCount;

            if (totalActive > 0)
            {
                return Json(new
                {
                    success = false,
                    blocked = true,
                    message = $"Bagian ini memiliki {totalActive} file aktif (KKJ: {activeKkjCount}, CPDP: {activeCpdpCount}). " +
                              $"Arsipkan atau hapus semua file aktif terlebih dahulu sebelum menghapus bagian ini."
                });
            }

            var archivedKkjCount = await _context.KkjFiles.CountAsync(f => f.OrganizationUnitId == id && f.IsArchived);
            var archivedCpdpCount = await _context.CpdpFiles.CountAsync(f => f.OrganizationUnitId == id && f.IsArchived);
            var totalArchived = archivedKkjCount + archivedCpdpCount;

            if (totalArchived > 0 && !confirmed)
            {
                return Json(new
                {
                    success = false,
                    needsConfirm = true,
                    archivedCount = totalArchived,
                    message = $"Bagian '{bagianEntity.Name}' memiliki {totalArchived} file arsip yang akan ikut terhapus permanen (KKJ: {archivedKkjCount}, CPDP: {archivedCpdpCount}). Tindakan ini tidak dapat dibatalkan."
                });
            }

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

            _context.OrganizationUnits.Remove(bagianEntity);
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
                    $"Deleted bagian '{bagianEntity.Name}' (ID {id}). Cascaded {totalArchived} archived file(s) (KKJ: {archivedKkjCount}, CPDP: {archivedCpdpCount}).",
                    id, "OrganizationUnit");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteBagian (bagianId={Id})", id); }

            return Json(new { success = true, message = $"Bagian '{bagianEntity.Name}' berhasil dihapus." });
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

            var bagianEntity = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagianEntity == null)
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
                        $"Uploaded CPDP file '{file.FileName}' ({file.Length} bytes) to bagian {bagianEntity.Name} [BagianId={bagianId}]",
                        cpdpFile.Id,
                        "CpdpFile");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit log write failed for CpdpUpload");
                }

                TempData["Success"] = $"File '{file.FileName}' berhasil di-upload ke bagian {bagianEntity.Name}.";
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
            var bagianEntity = await _context.OrganizationUnits.FindAsync(bagianId);
            if (bagianEntity == null) return NotFound();

            var archivedFiles = await _context.CpdpFiles
                .Where(f => f.OrganizationUnitId == bagianId && f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            ViewData["Title"] = $"Riwayat File CPDP — {bagianEntity.Name}";
            ViewBag.Bagian = bagianEntity;
            ViewBag.ArchivedFiles = archivedFiles;
            return View();
        }

        #endregion
    }
}
