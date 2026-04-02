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
        private readonly IConfiguration _config;
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<AdminController> logger,
            INotificationService notificationService)
            : base(context, userManager, auditLog, env)
        {
            _config = config;
            _logger = logger;
            _notificationService = notificationService;
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


        // ==================== COACH-COACHEE MAPPING ====================

        // GET /Admin/CoachCoacheeMapping
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMapping(
            string? search, string? section, bool showAll = false, int page = 1)
        {
            const int pageSize = 20;

            // 1. Load all users once (avoid N+1); use all for mapping display dict, active-only for modal dropdowns
            var allUsers = await _context.Users
                .Select(u => new { u.Id, u.FullName, u.NIP, u.Section, u.Unit, u.Position, u.RoleLevel, u.IsActive })
                .ToListAsync();
            var userDict = allUsers.ToDictionary(u => u.Id);
            var activeUsers = allUsers.Where(u => u.IsActive).ToList();

            // 2. Load mappings
            var query = _context.CoachCoacheeMappings.AsQueryable();
            if (!showAll)
                query = query.Where(m => m.IsActive);
            var mappings = await query.ToListAsync();

            // 3. Join with user data + apply filters (including parent.IsActive to prevent orphans)
            var rows = mappings.Select(m => new {
                Mapping = m,
                Coach = userDict.GetValueOrDefault(m.CoachId),
                Coachee = userDict.GetValueOrDefault(m.CoacheeId)
            }).Where(r => r.Coach?.IsActive == true && r.Coachee?.IsActive == true).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                rows = rows.Where(r =>
                    (r.Coach?.FullName?.ToLower().Contains(lower) ?? false) ||
                    (r.Coachee?.FullName?.ToLower().Contains(lower) ?? false) ||
                    (r.Coachee?.NIP?.ToLower().Contains(lower) ?? false))
                    .ToList();
            }
            if (!string.IsNullOrEmpty(section))
            {
                rows = rows.Where(r =>
                    r.Coach?.Section == section ||
                    r.Coachee?.Section == section)
                    .ToList();
            }

            // 4. Load active ProtonTrack assignments keyed by CoacheeId (filter out assignments with inactive ProtonKompetensi)
            var activeTrackAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.IsActive)
                .Include(a => a.ProtonTrack)
                    .ThenInclude(t => t.KompetensiList)
                .ToListAsync();
            var trackByCoachee = activeTrackAssignments
                .Where(a => a.ProtonTrack?.KompetensiList?.Any(k => k.IsActive) == true)
                .GroupBy(a => a.CoacheeId)
                .ToDictionary(g => g.Key, g => g.First().ProtonTrack?.DisplayName ?? "");

            // 5. Group by Coach, paginate over coach groups
            var grouped = rows
                .GroupBy(r => r.Mapping.CoachId)
                .Select(g => new {
                    CoachId = g.Key,
                    CoachName = g.First().Coach?.FullName ?? g.Key,
                    CoachSection = g.First().Coach?.Section ?? "",
                    ActiveCount = g.Count(r => r.Mapping.IsActive),
                    Coachees = g.Select(r => new {
                        r.Mapping.Id,
                        r.Mapping.IsActive,
                        r.Mapping.StartDate,
                        r.Mapping.EndDate,
                        r.Mapping.CoachId,
                        CoacheeName = r.Coachee?.FullName ?? r.Mapping.CoacheeId,
                        CoacheeNIP = r.Coachee?.NIP ?? "",
                        CoacheeSection = r.Coachee?.Section ?? "",
                        CoacheePosition = r.Coachee?.Position ?? "",
                        r.Mapping.CoacheeId,
                        ProtonTrack = trackByCoachee.GetValueOrDefault(r.Mapping.CoacheeId, ""),
                        AssignmentSection = r.Mapping.AssignmentSection ?? "",
                        AssignmentUnit = r.Mapping.AssignmentUnit ?? "",
                        IsCompleted = r.Mapping.IsCompleted
                    }).OrderBy(c => c.CoacheeName).ToList()
                })
                .OrderBy(g => g.CoachName)
                .ToList();

            var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);
            var paged = grouped.Skip(paging.Skip).Take(paging.Take).ToList();

            // 6. Modal data: eligible coaches, eligible coachees, proton tracks
            var activeCoacheeIds = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive)
                .Select(m => m.CoacheeId)
                .Distinct()
                .ToListAsync();

            ViewBag.GroupedCoaches = paged;
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.ShowAll = showAll;
            ViewBag.SearchTerm = search;
            ViewBag.SectionFilter = section;
            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.Sections = sectionUnitsDict.Keys.ToList();
            ViewBag.SectionUnits = sectionUnitsDict;
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            // Phase 74: Coach role only — not level (Supervisor is level 5 but never a coach)
            // Filter to active users only so deactivated workers don't appear in assignment modals
            var coachRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);
            ViewBag.EligibleCoaches = coachRoleUsers
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName).ToList();
            var coacheeRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coachee);
            ViewBag.EligibleCoachees = coacheeRoleUsers
                .Where(u => u.IsActive && !activeCoacheeIds.Contains(u.Id))
                .OrderBy(u => u.FullName).ToList();
            ViewBag.AllUsers = activeUsers.OrderBy(u => u.FullName).ToList();
            ViewBag.ProtonTracks = await _context.ProtonTracks
                .OrderBy(t => t.Urutan).ToListAsync();

            return View();
        }

        // GET /Admin/DownloadMappingImportTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadMappingImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import CoachCoachee");

            // Row 1: Headers
            ws.Cell(1, 1).Value = "NIP Coach";
            ws.Cell(1, 2).Value = "NIP Coachee";
            for (int i = 1; i <= 2; i++)
            {
                ws.Cell(1, i).Style.Font.Bold = true;
                ws.Cell(1, i).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i).Style.Font.FontColor = XLColor.White;
            }

            // Row 2: Example data
            ws.Cell(2, 1).Value = "123456";
            ws.Cell(2, 2).Value = "789012";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
            ws.Cell(2, 2).Style.Font.Italic = true;
            ws.Cell(2, 2).Style.Font.FontColor = XLColor.Gray;

            // Row 3: Note
            ws.Cell(3, 1).Value = "Isi NIP Coach dan NIP Coachee. StartDate otomatis hari ini, IsActive otomatis true.";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

            return ExcelExportHelper.ToFileResult(workbook, "coach_coachee_import_template.xlsx", this);
        }

        // POST /Admin/ImportCoachCoacheeMapping
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportCoachCoacheeMapping(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ImportError"] = "Pilih file Excel terlebih dahulu.";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ImportError"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            const long maxSize = 10 * 1024 * 1024; // 10MB
            if (excelFile.Length > maxSize)
            {
                TempData["ImportError"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            // Load users keyed by NIP (handle duplicate NIPs by taking first)
            var usersByNip = (await _context.Users
                .Where(u => u.NIP != null)
                .Select(u => new { u.Id, u.NIP, u.Section, u.Unit })
                .ToListAsync())
                .GroupBy(u => u.NIP!)
                .ToDictionary(g => g.Key, g => g.First());

            // Load all existing mappings
            var allMappings = await _context.CoachCoacheeMappings.ToListAsync();

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

            var results = new List<ImportMappingResult>();
            var newMappings = new List<CoachCoacheeMapping>();
            var reactivatedMappings = new List<CoachCoacheeMapping>();

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();

                // D-16: Header validation
                var expectedHeaders = new[] { "NIP Coach", "NIP Coachee" };
                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var actual = ws.Cell(1, i + 1).GetString().Trim();
                    if (!actual.Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ImportError"] = $"Header kolom {i + 1} tidak cocok. Diharapkan: '{expectedHeaders[i]}', ditemukan: '{actual}'. Pastikan menggunakan template yang benar.";
                        return RedirectToAction(nameof(CoachCoacheeMapping));
                    }
                }

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nipCoach = (row.Cell(1).GetString() ?? "").Trim();
                    var nipCoachee = (row.Cell(2).GetString() ?? "").Trim();

                    // Skip completely blank rows
                    if (string.IsNullOrWhiteSpace(nipCoach) && string.IsNullOrWhiteSpace(nipCoachee))
                        continue;

                    var result = new ImportMappingResult
                    {
                        RowNum = row.RowNumber(),
                        NipCoach = nipCoach,
                        NipCoachee = nipCoachee
                    };

                    if (string.IsNullOrWhiteSpace(nipCoach) || string.IsNullOrWhiteSpace(nipCoachee))
                    {
                        result.Status = "Error";
                        result.Message = "NIP Coach atau NIP Coachee kosong";
                        results.Add(result);
                        continue;
                    }

                    if (!usersByNip.TryGetValue(nipCoach, out var coachUser))
                    {
                        result.Status = "Error";
                        result.Message = $"NIP Coach '{nipCoach}' tidak ditemukan";
                        results.Add(result);
                        continue;
                    }

                    if (!usersByNip.TryGetValue(nipCoachee, out var coacheeUser))
                    {
                        result.Status = "Error";
                        result.Message = $"NIP Coachee '{nipCoachee}' tidak ditemukan";
                        results.Add(result);
                        continue;
                    }

                    if (coachUser.Id == coacheeUser.Id)
                    {
                        result.Status = "Error";
                        result.Message = "Coach tidak dapat menjadi coachee dirinya sendiri";
                        results.Add(result);
                        continue;
                    }

                    // Phase 261: Validate coachee Section/Unit against OrganizationUnit
                    if (string.IsNullOrEmpty(coacheeUser.Section) || string.IsNullOrEmpty(coacheeUser.Unit)
                        || !sectionUnitsDict.TryGetValue(coacheeUser.Section.Trim(), out var vuImport)
                        || !vuImport.Contains(coacheeUser.Unit.Trim()))
                    {
                        result.Status = "Error";
                        result.Message = $"Section/Unit coachee ('{coacheeUser.Section}'/'{coacheeUser.Unit}') tidak valid di OrganizationUnit aktif";
                        results.Add(result);
                        continue;
                    }

                    // Check for existing active mapping
                    var activeMapping = allMappings.FirstOrDefault(m =>
                        m.CoachId == coachUser.Id && m.CoacheeId == coacheeUser.Id && m.IsActive);
                    if (activeMapping != null)
                    {
                        result.Status = "Skip";
                        result.Message = "Mapping sudah aktif";
                        results.Add(result);
                        continue;
                    }

                    // Check for existing inactive mapping (reactivate)
                    var inactiveMapping = allMappings.FirstOrDefault(m =>
                        m.CoachId == coachUser.Id && m.CoacheeId == coacheeUser.Id && !m.IsActive);
                    if (inactiveMapping != null)
                    {
                        inactiveMapping.IsActive = true;
                        inactiveMapping.StartDate = DateTime.Today;
                        inactiveMapping.EndDate = null;
                        inactiveMapping.AssignmentSection = coacheeUser.Section.Trim();
                        inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim();
                        reactivatedMappings.Add(inactiveMapping);
                        result.Status = "Reactivated";
                        result.Message = "Mapping diaktifkan kembali";
                        results.Add(result);
                        continue;
                    }

                    // Create new mapping
                    var newMapping = new CoachCoacheeMapping
                    {
                        CoachId = coachUser.Id,
                        CoacheeId = coacheeUser.Id,
                        IsActive = true,
                        StartDate = DateTime.Today,
                        AssignmentSection = coacheeUser.Section,
                        AssignmentUnit = coacheeUser.Unit
                    };
                    newMappings.Add(newMapping);
                    result.Status = "Success";
                    result.Message = "Berhasil dibuat";
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = $"Gagal membaca file Excel: {ex.Message}";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            // D-13: Wrap insert phase dalam transaction untuk atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (newMappings.Any())
                    await _context.CoachCoacheeMappings.AddRangeAsync(newMappings);
                // reactivated mappings sudah di-track oleh EF (IsActive diubah di-memory)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "ImportCoachCoacheeMapping transaction failed");
                TempData["ImportError"] = "Import gagal. Semua perubahan dibatalkan.";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            var successCount = results.Count(r => r.Status == "Success");
            var reactivatedCount = results.Count(r => r.Status == "Reactivated");
            var skipCount = results.Count(r => r.Status == "Skip");
            var errorCount = results.Count(r => r.Status == "Error");

            var actor = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actor?.Id ?? "system",
                ActorName = actor?.FullName ?? "system",
                ActionType = "ImportCoachCoacheeMapping",
                Description = $"Import {successCount} mapping baru, {reactivatedCount} diaktifkan kembali, {skipCount} dilewati, {errorCount} error",
                TargetType = "CoachCoacheeMapping",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["ImportResults"] = System.Text.Json.JsonSerializer.Serialize(results);
            return RedirectToAction(nameof(CoachCoacheeMapping));
        }

        // POST /Admin/CoachCoacheeMappingAssign
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingAssign([FromBody] CoachAssignRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.CoachId) || req.CoacheeIds == null || req.CoacheeIds.Count == 0)
                return Json(new { success = false, message = "Data tidak lengkap." });

            if (req.CoacheeIds.Contains(req.CoachId))
                return Json(new { success = false, message = "Coach tidak dapat menjadi coachee dirinya sendiri." });

            if (string.IsNullOrWhiteSpace(req.AssignmentSection) || string.IsNullOrWhiteSpace(req.AssignmentUnit))
                return Json(new { success = false, message = "Assignment Section dan Unit wajib diisi." });

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            if (!sectionUnitsDict.TryGetValue(req.AssignmentSection!.Trim(), out var validUnits)
                || !validUnits.Contains(req.AssignmentUnit!.Trim()))
                return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });

            // Check for duplicate active mappings
            var existingMappings = await _context.CoachCoacheeMappings
                .Where(m => req.CoacheeIds.Contains(m.CoacheeId) && m.IsActive)
                .ToListAsync();

            if (existingMappings.Any())
            {
                var allUsers = await _context.Users
                    .Select(u => new { u.Id, FullName = u.FullName ?? u.Id })
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);

                var names = existingMappings
                    .Select(m => allUsers.GetValueOrDefault(m.CoacheeId, m.CoacheeId))
                    .Distinct()
                    .ToList();

                return Json(new { success = false, message = $"Coachee sudah memiliki coach aktif: {string.Join(", ", names)}" });
            }

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            // D-09/D-10/D-11/D-12: Progression warning check for Tahun 2/3 assignment
            if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
            {
                var requestedTrack = await _context.ProtonTracks.FindAsync(req.ProtonTrackId.Value);
                if (requestedTrack != null)
                {
                    // Find previous track in the same TrackType (e.g. Panelman Tahun 1 before Panelman Tahun 2)
                    var prevTrack = await _context.ProtonTracks
                        .Where(t => t.TrackType == requestedTrack.TrackType
                                 && t.Urutan == requestedTrack.Urutan - 1)
                        .FirstOrDefaultAsync();
                    if (prevTrack != null)
                    {
                        var incompleteCoachees = new List<string>();
                        foreach (var coacheeId in req.CoacheeIds)
                        {
                            // D-11: Skip warning if coachee already has an assignment for this track (reactivated scenario)
                            var hasExistingAssignment = await _context.ProtonTrackAssignments
                                .AnyAsync(a => a.CoacheeId == coacheeId
                                           && a.ProtonTrackId == req.ProtonTrackId.Value);
                            if (hasExistingAssignment) continue;

                            // Check if previous track assignment exists and all progress is Approved
                            var prevAssignment = await _context.ProtonTrackAssignments
                                .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId
                                                       && a.ProtonTrackId == prevTrack.Id);
                            if (prevAssignment == null)
                            {
                                incompleteCoachees.Add(coacheeId);
                                continue;
                            }

                            var prevProgressCount = await _context.ProtonDeliverableProgresses
                                .CountAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id);
                            var allApproved = prevProgressCount > 0 && !await _context.ProtonDeliverableProgresses
                                .AnyAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id
                                           && p.Status != "Approved");
                            if (!allApproved)
                                incompleteCoachees.Add(coacheeId);
                        }

                        // D-09: Warning only — return warning response if incomplete and user hasn't confirmed
                        if (incompleteCoachees.Any() && !req.ConfirmProgressionWarning)
                        {
                            return Json(new { success = false, warning = true,
                                message = $"{incompleteCoachees.Count} coachee belum menyelesaikan {prevTrack.DisplayName}. Tetap lanjutkan?",
                                incompleteCount = incompleteCoachees.Count });
                        }
                    }
                }
            }

            var startDate = req.StartDate ?? DateTime.Today;

            var newMappings = req.CoacheeIds.Select(id => new CoachCoacheeMapping
            {
                CoachId = req.CoachId,
                CoacheeId = id,
                IsActive = true,
                StartDate = startDate,
                AssignmentSection = req.AssignmentSection!.Trim(),
                AssignmentUnit = req.AssignmentUnit!.Trim()
            }).ToList();

            _context.CoachCoacheeMappings.AddRange(newMappings);

            // ProtonTrack side-effect
            if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
            {
                // Deactivate any currently active assignments for a different track
                var existingTracks = await _context.ProtonTrackAssignments
                    .Where(a => req.CoacheeIds.Contains(a.CoacheeId) && a.IsActive && a.ProtonTrackId != req.ProtonTrackId.Value)
                    .ToListAsync();
                foreach (var t in existingTracks)
                {
                    t.IsActive = false;
                    t.DeactivatedAt = DateTime.UtcNow;
                }

                // FIX-02: For each coachee, reuse an existing inactive assignment for this track instead of creating a duplicate.
                var allWarnings = new List<string>();
                foreach (var coacheeId in req.CoacheeIds)
                {
                    // Check if already active for this track (no-op)
                    var alreadyActive = await _context.ProtonTrackAssignments
                        .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == req.ProtonTrackId.Value && a.IsActive);
                    if (alreadyActive) continue;

                    // Check for an existing inactive assignment for this coachee+track
                    var existing = await _context.ProtonTrackAssignments
                        .Where(a => a.CoacheeId == coacheeId && a.ProtonTrackId == req.ProtonTrackId.Value && !a.IsActive)
                        .OrderByDescending(a => a.Id)
                        .FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        // Reuse it — existing ProtonDeliverableProgress rows are already linked
                        existing.IsActive = true;
                        existing.DeactivatedAt = null;
                    }
                    else
                    {
                        // Create a new assignment and auto-create progress rows
                        var newAssignment = new ProtonTrackAssignment
                        {
                            CoacheeId = coacheeId,
                            AssignedById = actor.Id,
                            ProtonTrackId = req.ProtonTrackId.Value,
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.ProtonTrackAssignments.Add(newAssignment);
                        await _context.SaveChangesAsync(); // flush to get assignment ID
                        var w = await AutoCreateProgressForAssignment(newAssignment.Id, newAssignment.ProtonTrackId, coacheeId);
                        allWarnings.AddRange(w);
                    }
                }
                if (allWarnings.Any())
                    TempData["Warning"] = string.Join("\n", allWarnings);
            }

            await _context.SaveChangesAsync();

            var count = newMappings.Count;
            await _auditLog.LogAsync(actor.Id, actor.FullName, "Assign",
                $"Assigned coach to {count} coachee(s) [Section: {req.AssignmentSection}, Unit: {req.AssignmentUnit}]",
                targetType: "CoachCoacheeMapping");

            // COACH-01: Notify coach for each coachee assigned
            try
            {
                var coacheeUsers = await _context.Users
                    .Where(u => req.CoacheeIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);
                foreach (var coacheeId in req.CoacheeIds)
                {
                    var coacheeName = coacheeUsers.GetValueOrDefault(coacheeId, coacheeId);
                    await _notificationService.SendAsync(req.CoachId, "COACH_ASSIGNED",
                        "Coach Ditunjuk",
                        $"Anda ditunjuk sebagai coach untuk {coacheeName}",
                        "/CDP/CoachingProton");
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }

            return Json(new { success = true, message = $"{count} mapping berhasil dibuat." });
        }

        // POST /Admin/CoachCoacheeMappingEdit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingEdit([FromBody] CoachEditRequest req)
        {
            if (req == null)
                return Json(new { success = false, message = "Data tidak lengkap." });

            var mapping = await _context.CoachCoacheeMappings.FindAsync(req.MappingId);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });

            if (req.CoachId == mapping.CoacheeId)
                return Json(new { success = false, message = "Coach tidak dapat menjadi coachee dirinya sendiri." });

            // Check for duplicate: if changing coach, ensure no other active mapping exists for coachee with new coach
            if (req.CoachId != mapping.CoachId)
            {
                var duplicate = await _context.CoachCoacheeMappings
                    .AnyAsync(m => m.CoacheeId == mapping.CoacheeId && m.CoachId == req.CoachId && m.IsActive && m.Id != req.MappingId);
                if (duplicate)
                    return Json(new { success = false, message = "Sudah ada mapping aktif antara coach dan coachee ini." });
            }

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            var secEdit = req.AssignmentSection?.Trim();
            var unitEdit = req.AssignmentUnit?.Trim();
            if (!string.IsNullOrEmpty(secEdit) && !string.IsNullOrEmpty(unitEdit))
            {
                if (!sectionUnitsDict.TryGetValue(secEdit, out var validUnitsEdit) || !validUnitsEdit.Contains(unitEdit))
                    return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });
            }

            mapping.CoachId = req.CoachId;
            if (req.StartDate.HasValue)
                mapping.StartDate = req.StartDate.Value;

            // Phase 129: Detect AssignmentUnit change for progress rebuild
            var oldUnit = mapping.AssignmentUnit;
            mapping.AssignmentSection = req.AssignmentSection?.Trim();
            mapping.AssignmentUnit = req.AssignmentUnit?.Trim();
            var newUnit = mapping.AssignmentUnit;
            bool unitChanged = (oldUnit?.Trim() ?? "") != (newUnit?.Trim() ?? "");

            // ProtonTrack side-effect
            if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
            {
                var existingTracks = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                    .ToListAsync();
                foreach (var t in existingTracks)
                {
                    t.IsActive = false;
                    await CleanupProgressForAssignment(t.Id);
                }

                var newAssignment = new ProtonTrackAssignment
                {
                    CoacheeId = mapping.CoacheeId,
                    AssignedById = actor.Id,
                    ProtonTrackId = req.ProtonTrackId.Value,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                };
                _context.ProtonTrackAssignments.Add(newAssignment);
                await _context.SaveChangesAsync(); // flush to get assignment ID

                var editWarnings = await AutoCreateProgressForAssignment(newAssignment.Id, newAssignment.ProtonTrackId, mapping.CoacheeId);
                if (editWarnings.Any())
                    TempData["Warning"] = string.Join("\n", editWarnings);
            }

            await _context.SaveChangesAsync();

            // Phase 129: If unit changed and ProtonTrack wasn't already rebuilt, rebuild progress for new unit
            if (unitChanged && !(req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0))
            {
                var activeAssignments = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                    .ToListAsync();

                int deletedCount = 0, createdCount = 0;
                foreach (var a in activeAssignments)
                {
                    // Count existing progress before cleanup
                    deletedCount += await _context.ProtonDeliverableProgresses
                        .CountAsync(p => p.ProtonTrackAssignmentId == a.Id);
                    await CleanupProgressForAssignment(a.Id);
                }
                await _context.SaveChangesAsync(); // flush deletes before recreate

                foreach (var a in activeAssignments)
                {
                    var warnings = await AutoCreateProgressForAssignment(a.Id, a.ProtonTrackId, mapping.CoacheeId);
                    createdCount += await _context.ProtonDeliverableProgresses
                        .CountAsync(p => p.ProtonTrackAssignmentId == a.Id);
                    if (warnings.Any())
                        TempData["Warning"] = string.Join("\n", warnings);
                }

                TempData["Info"] = $"Unit berubah dari '{oldUnit}' ke '{newUnit}' → {deletedCount} progress dihapus, {createdCount} progress baru dibuat untuk unit {newUnit}";
            }

            await _auditLog.LogAsync(actor.Id, actor.FullName, "Edit",
                $"Edited coach-coachee mapping #{mapping.Id}", targetId: mapping.Id, targetType: "CoachCoacheeMapping");

            // COACH-02: Notify both coach and coachee about mapping edit
            try
            {
                var coachUser = await _context.Users.FindAsync(mapping.CoachId);
                var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
                var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
                var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

                await _notificationService.SendAsync(mapping.CoachId, "COACH_MAPPING_EDITED",
                    "Mapping Coaching Diubah",
                    $"Mapping coaching Anda dengan {coacheeName} telah diubah",
                    "/CDP/CoachingProton");
                await _notificationService.SendAsync(mapping.CoacheeId, "COACH_MAPPING_EDITED",
                    "Mapping Coaching Diubah",
                    $"Mapping coaching Anda dengan {coachName} telah diubah",
                    "/CDP/CoachingProton");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }

            return Json(new { success = true, message = "Mapping berhasil diperbarui." });
        }

        // POST /Admin/CleanupCoachCoacheeMappingOrg
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupCoachCoacheeMappingOrg()
        {
            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

            var activeMappings = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive)
                .ToListAsync();

            var userDict = await _context.Users
                .Select(u => new { u.Id, u.Section, u.Unit })
                .ToDictionaryAsync(u => u.Id, u => new { u.Section, u.Unit });

            int autoFixed = 0;
            var unfixable = new List<object>();

            foreach (var m in activeMappings)
            {
                var sec = m.AssignmentSection?.Trim();
                var unit = m.AssignmentUnit?.Trim();
                bool isValid = !string.IsNullOrEmpty(sec) && !string.IsNullOrEmpty(unit)
                    && sectionUnitsDict.TryGetValue(sec, out var vu) && vu.Contains(unit);

                if (isValid) continue;

                // Try fix from coachee user record
                if (userDict.TryGetValue(m.CoacheeId, out var coacheeInfo))
                {
                    var userSec = coacheeInfo.Section?.Trim();
                    var userUnit = coacheeInfo.Unit?.Trim();
                    bool userValid = !string.IsNullOrEmpty(userSec) && !string.IsNullOrEmpty(userUnit)
                        && sectionUnitsDict.TryGetValue(userSec, out var vuUser) && vuUser.Contains(userUnit);

                    if (userValid)
                    {
                        m.AssignmentSection = userSec;
                        m.AssignmentUnit = userUnit;
                        autoFixed++;
                        continue;
                    }
                }

                unfixable.Add(new { m.Id, m.CoacheeId, m.AssignmentSection, m.AssignmentUnit });
            }

            await _context.SaveChangesAsync();

            TempData["CleanupReport"] = System.Text.Json.JsonSerializer.Serialize(new { autoFixed, unfixable });
            return RedirectToAction(nameof(CoachCoacheeMapping));
        }

        // POST /Admin/CoachCoacheeMappingGetSessionCount
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingGetSessionCount(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });

            var activeSessionCount = await _context.CoachingSessions
                .CountAsync(s => s.CoachId == mapping.CoachId && s.CoacheeId == mapping.CoacheeId && s.Status == "Draft");

            return Json(new { success = true, count = activeSessionCount });
        }

        // GET /Admin/CoachCoacheeMappingActiveAssignmentCount
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMappingActiveAssignmentCount(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null) return Json(new { count = 0 });
            var count = await _context.ProtonTrackAssignments
                .CountAsync(a => a.CoacheeId == mapping.CoacheeId && a.IsActive);
            return Json(new { count });
        }

        // POST /Admin/CoachCoacheeMappingDeactivate
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingDeactivate(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });
            if (!mapping.IsActive)
                return Json(new { success = false, message = "Mapping sudah tidak aktif." });

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
            mapping.IsActive = false;
            mapping.EndDate = DateTime.UtcNow;

            // Cascade: deactivate all ProtonTrackAssignments for this coachee
            // FIX-01: stamp DeactivatedAt so reactivation can correlate assignments back to this event
            var deactivationTime = mapping.EndDate.Value;
            var activeAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                .ToListAsync();
            foreach (var a in activeAssignments)
            {
                a.IsActive = false;
                a.DeactivatedAt = deactivationTime;
            }
            int cascadeCount = activeAssignments.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogAsync(actor.Id, actor.FullName, "Deactivate",
                $"Deactivated coach-coachee mapping #{id} — {cascadeCount} ProtonTrackAssignment(s) also deactivated", targetId: id, targetType: "CoachCoacheeMapping");

            // COACH-03: Notify both coach and coachee about deactivation
            try
            {
                var coachUser = await _context.Users.FindAsync(mapping.CoachId);
                var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
                var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
                var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

                await _notificationService.SendAsync(mapping.CoachId, "COACH_MAPPING_DEACTIVATED",
                    "Mapping Coaching Dinonaktifkan",
                    $"Mapping coaching Anda dengan {coacheeName} telah dinonaktifkan",
                    "/CDP/CoachingProton");
                await _notificationService.SendAsync(mapping.CoacheeId, "COACH_MAPPING_DEACTIVATED",
                    "Mapping Coaching Dinonaktifkan",
                    $"Mapping coaching Anda dengan {coachName} telah dinonaktifkan",
                    "/CDP/CoachingProton");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }

            return Json(new { success = true, message = $"Mapping berhasil dinonaktifkan. {cascadeCount} track assignment juga dinonaktifkan." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "CoachCoacheeMappingDeactivate transaction failed for mapping {Id}", id);
                return Json(new { success = false, message = "Operasi gagal. Semua perubahan dibatalkan." });
            }
        }

        // POST /Admin/CoachCoacheeMappingReactivate
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingReactivate(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });
            if (mapping.IsActive)
                return Json(new { success = false, message = "Mapping sudah aktif." });

            // Validate: no other active mapping for the same coachee
            var duplicateActive = await _context.CoachCoacheeMappings
                .AnyAsync(m => m.CoacheeId == mapping.CoacheeId && m.IsActive && m.Id != id);
            if (duplicateActive)
                return Json(new { success = false, message = "Coachee sudah memiliki coach aktif lain. Nonaktifkan dulu sebelum mengaktifkan mapping ini." });

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            // D-08: Capture originalEndDate BEFORE modifying mapping (avoid fragile OriginalValues API)
            var originalEndDate = mapping.EndDate;
            mapping.IsActive = true;
            mapping.EndDate = null;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
            // FIX-01: Only reactivate assignments that were deactivated as part of this mapping's deactivation event.
            // We correlate by DeactivatedAt timestamp (within 5 seconds of originalEndDate) to avoid restoring
            // assignments that were independently deactivated for other reasons.
            List<ProtonTrackAssignment> inactiveAssignments;
            if (originalEndDate.HasValue)
            {
                inactiveAssignments = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == mapping.CoacheeId
                        && !a.IsActive
                        && a.DeactivatedAt != null
                        && EF.Functions.DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) >= -5
                        && EF.Functions.DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) <= 5)
                    .ToListAsync();
            }
            else
            {
                // Mapping was deactivated before DeactivatedAt existed — fall back to all inactive assignments
                inactiveAssignments = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == mapping.CoacheeId && !a.IsActive && a.DeactivatedAt == null)
                    .ToListAsync();
            }

            foreach (var a in inactiveAssignments)
            {
                a.IsActive = true;
                a.DeactivatedAt = null;
            }
            int reactivatedCount = inactiveAssignments.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogAsync(actor.Id, actor.FullName, "Reactivate",
                $"Reactivated coach-coachee mapping #{id} — {reactivatedCount} ProtonTrackAssignment(s) also reactivated", targetId: id, targetType: "CoachCoacheeMapping");

            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            return Json(new { success = true,
                message = $"Mapping berhasil diaktifkan kembali. {reactivatedCount} track assignment juga diaktifkan kembali.",
                showAssignPrompt = reactivatedCount == 0,
                coacheeName = coacheeUser?.FullName ?? "",
                assignUrl = Url.Action("CoachCoacheeMapping", "Admin") });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "CoachCoacheeMappingReactivate transaction failed for mapping {Id}", id);
                return Json(new { success = false, message = "Operasi gagal. Semua perubahan dibatalkan." });
            }
        }

        // Phase 236 COMP-04: completion criteria helper per D-13
        private async Task<bool> IsYearCompletedAsync(int assignmentId)
        {
            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == assignmentId)
                .ToListAsync();
            if (!progresses.Any()) return false;
            bool allApproved = progresses.All(p => p.Status == "Approved");
            bool hasFinalAssessment = await _context.ProtonFinalAssessments
                .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
            return allApproved && hasFinalAssessment;
        }

        // Phase 236 COMP-04: Mark mapping as completed/graduated per D-15
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> MarkMappingCompleted(int mappingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var mapping = await _context.CoachCoacheeMappings.FindAsync(mappingId);
            if (mapping == null) return NotFound();
            // Validate: coachee harus punya semua tahun completed
            var assignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                .ToListAsync();
            var tahun3Assignment = assignments
                .FirstOrDefault(a => a.ProtonTrack != null && a.ProtonTrack.TahunKe == "Tahun 3");
            if (tahun3Assignment == null)
            {
                TempData["Error"] = "Coachee belum memiliki assignment Tahun 3.";
                return RedirectToAction("CoachCoacheeMapping");
            }
            bool tahun3Complete = await IsYearCompletedAsync(tahun3Assignment.Id);
            if (!tahun3Complete)
            {
                TempData["Error"] = "Tahun 3 belum selesai — semua deliverable harus Approved dan final assessment harus ada.";
                return RedirectToAction("CoachCoacheeMapping");
            }
            mapping.IsCompleted = true;
            mapping.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var actorName = string.IsNullOrWhiteSpace(user.NIP)
                ? (user.FullName ?? "Unknown")
                : $"{user.NIP} - {user.FullName}";
            await _auditLog.LogAsync(user.Id, actorName, "MarkMappingCompleted",
                $"Mapping ID={mappingId} ditandai graduated. CoacheeId={mapping.CoacheeId}", mappingId, "CoachCoacheeMapping");
            TempData["Success"] = "Coachee berhasil ditandai sebagai graduated.";
            return RedirectToAction("CoachCoacheeMapping");
        }

        // GET /Admin/CoachCoacheeMappingDeletePreview
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMappingDeletePreview(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return NotFound(new { success = false, message = "Mapping tidak ditemukan." });
            if (mapping.IsActive)
                return BadRequest(new { success = false, message = "Hanya mapping nonaktif yang dapat dihapus." });

            var coachUser = await _context.Users.FindAsync(mapping.CoachId);
            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
            var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

            var assignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == mapping.CoacheeId)
                .Select(a => a.Id)
                .ToListAsync();

            var progressCount = await _context.ProtonDeliverableProgresses
                .CountAsync(p => assignments.Contains(p.ProtonTrackAssignmentId));

            return Json(new
            {
                coachName,
                coacheeName,
                assignmentCount = assignments.Count,
                progressCount
            });
        }

        // POST /Admin/CoachCoacheeMappingDelete
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingDelete(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });
            if (mapping.IsActive)
                return Json(new { success = false, message = "Hanya mapping nonaktif yang dapat dihapus." });

            var coachUser = await _context.Users.FindAsync(mapping.CoachId);
            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
            var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            var assignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == mapping.CoacheeId)
                .ToListAsync();

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => assignmentIds.Contains(p.ProtonTrackAssignmentId))
                .ToListAsync();

            var finalAssessments = await _context.ProtonFinalAssessments
                .Where(fa => assignmentIds.Contains(fa.ProtonTrackAssignmentId))
                .ToListAsync();

            int assignmentCount = assignments.Count;
            int progressCount = progresses.Count;

            _context.ProtonFinalAssessments.RemoveRange(finalAssessments);
            _context.ProtonDeliverableProgresses.RemoveRange(progresses);
            _context.ProtonTrackAssignments.RemoveRange(assignments);
            _context.CoachCoacheeMappings.Remove(mapping);

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(actor.Id, actor.FullName, "DeleteMapping",
                $"Hapus mapping: Coach {coachName} -> Coachee {coacheeName}, {assignmentCount} track assignments, {progressCount} progress records deleted",
                targetId: id, targetType: "CoachCoacheeMapping");

            return Json(new { success = true, message = "Mapping berhasil dihapus." });
        }

        // ==================== MANAGE WORKERS (migrated from CMP) ====================

        // GET /Admin/ManageWorkers
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? unitFilter, string? roleFilter, bool showInactive = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Server-side validation: ignore unitFilter if it doesn't belong to selected sectionFilter
            if (!string.IsNullOrEmpty(unitFilter) && !string.IsNullOrEmpty(sectionFilter))
            {
                var validUnits = await _context.GetUnitsForSectionAsync(sectionFilter);
                if (!validUnits.Contains(unitFilter))
                    unitFilter = null;
            }
            else if (!string.IsNullOrEmpty(unitFilter) && string.IsNullOrEmpty(sectionFilter))
            {
                unitFilter = null;
            }

            var query = _context.Users.AsQueryable();

            // Search by name, email, or NIP
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s) ||
                    (u.NIP != null && u.NIP.Contains(s))
                );
            }

            // Filter by section
            if (!string.IsNullOrEmpty(sectionFilter))
            {
                query = query.Where(u => u.Section == sectionFilter);
            }

            // Filter by unit
            if (!string.IsNullOrEmpty(unitFilter))
            {
                query = query.Where(u => u.Unit == unitFilter);
            }

            // Filter by role level
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var roleLevel = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == roleLevel);
            }

            // Filter by IsActive
            if (!showInactive)
                query = query.Where(u => u.IsActive);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            // Get roles for each user
            var userRolesDict = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userRolesDict[u.Id] = roles.FirstOrDefault() ?? "No Role";
            }
            ViewBag.UserRoles = userRolesDict;

            // Stats
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.AdminCount = await _context.Users.CountAsync(u => u.RoleLevel == 1);
            ViewBag.HcCount = await _context.Users.CountAsync(u => u.RoleLevel == 2);
            ViewBag.WorkerCount = await _context.Users.CountAsync(u => u.RoleLevel >= 5);

            // Filters state
            ViewBag.Search = search;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.UnitFilter = unitFilter;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.ShowInactive = showInactive;
            ViewBag.AllSections = await _context.GetAllSectionsAsync();
            ViewBag.AllUnits = !string.IsNullOrEmpty(sectionFilter)
                ? await _context.GetUnitsForSectionAsync(sectionFilter)
                : new List<string>();

            return View(users);
        }

        // GET /Admin/ExportWorkers
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportWorkers(string? search, string? sectionFilter, string? unitFilter, string? roleFilter, bool showInactive = false)
        {
            // Server-side validation: ignore unitFilter if it doesn't belong to selected sectionFilter
            if (!string.IsNullOrEmpty(unitFilter) && !string.IsNullOrEmpty(sectionFilter))
            {
                var validUnits = await _context.GetUnitsForSectionAsync(sectionFilter);
                if (!validUnits.Contains(unitFilter))
                    unitFilter = null;
            }
            else if (!string.IsNullOrEmpty(unitFilter) && string.IsNullOrEmpty(sectionFilter))
            {
                unitFilter = null;
            }

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s) ||
                    (u.NIP != null && u.NIP.Contains(s))
                );
            }

            if (!string.IsNullOrEmpty(sectionFilter))
                query = query.Where(u => u.Section == sectionFilter);

            if (!string.IsNullOrEmpty(unitFilter))
                query = query.Where(u => u.Unit == unitFilter);

            if (!string.IsNullOrEmpty(roleFilter))
            {
                var roleLevel = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == roleLevel);
            }

            if (!showInactive)
                query = query.Where(u => u.IsActive);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Pekerja", new[] { "No", "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Status" });
            ws.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < users.Count; i++)
            {
                var u = users[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = u.FullName;
                ws.Cell(i + 2, 3).Value = u.Email;
                ws.Cell(i + 2, 4).Value = u.NIP ?? "-";
                ws.Cell(i + 2, 5).Value = u.Position ?? "-";
                ws.Cell(i + 2, 6).Value = u.Section ?? "-";
                ws.Cell(i + 2, 7).Value = u.Unit ?? "-";
                ws.Cell(i + 2, 8).Value = u.IsActive ? "Active" : "Inactive";
            }

            var fileName = $"Pekerja_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // GET /Admin/CreateWorker
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CreateWorker()
        {
            var model = new ManageUserViewModel
            {
                Role = "Coachee"
            };
            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            return View(model);
        }

        // POST /Admin/CreateWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorker(ManageUserViewModel model)
        {
            // Password required only in local mode; AD mode auto-generates
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            if (!useAD && string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password harus diisi untuk user baru");
            }

            // Validate Section/Unit against active OrganizationUnits in DB
            if (!string.IsNullOrEmpty(model.Section))
            {
                var validSections = await _context.GetAllSectionsAsync();
                if (!validSections.Contains(model.Section))
                {
                    ModelState.AddModelError("Section", $"Bagian '{model.Section}' tidak ditemukan di data organisasi");
                }
                else if (!string.IsNullOrEmpty(model.Unit))
                {
                    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
                    if (!validUnits.Contains(model.Unit))
                    {
                        ModelState.AddModelError("Unit", $"Unit '{model.Unit}' tidak valid untuk bagian '{model.Section}'");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("CreateWorker validation failed: {Errors}", errors);
                var sectionUnitsDictErr = await _context.GetSectionUnitsDictAsync();
                ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictErr);
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email sudah terdaftar di sistem");
                return View(model);
            }

            var roleLevel = UserRoles.GetRoleLevel(model.Role);

            // Determine default SelectedView based on role
            var selectedView = UserRoles.GetDefaultView(model.Role);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName,
                NIP = model.NIP,
                Position = model.Position,
                Section = model.Section,
                Unit = model.Unit,
                Directorate = model.Directorate,
                JoinDate = model.JoinDate,
                RoleLevel = roleLevel,
                SelectedView = selectedView
            };

            // AD mode auto-generates password; local mode uses form value
            var password = useAD ? GenerateRandomPassword() : model.Password!;
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                // Audit log
                try
                {
                    var actor = await _userManager.GetUserAsync(User);
                    var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                    await _auditLog.LogAsync(
                        actor?.Id ?? "",
                        actorName,
                        "CreateWorker",
                        $"Created user '{model.FullName}' ({model.Email}) with role '{model.Role}'",
                        null,
                        "ApplicationUser");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for CreateWorker (userId={UserId})", user.Id); }

                TempData["Success"] = $"User '{model.FullName}' berhasil ditambahkan dengan role '{model.Role}'.";
                return RedirectToAction("ManageWorkers");
            }

            // Identity errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // GET /Admin/EditWorker
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new ManageUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                NIP = user.NIP,
                Position = user.Position,
                Section = user.Section,
                Unit = user.Unit,
                Directorate = user.Directorate,
                JoinDate = user.JoinDate,
                Role = roles.FirstOrDefault() ?? "Coachee"
            };

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            return View(model);
        }

        // POST /Admin/EditWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorker(ManageUserViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return BadRequest();

            // Password is optional for edit — remove validation if blank
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            // Validate Section/Unit against active OrganizationUnits in DB
            if (!string.IsNullOrEmpty(model.Section))
            {
                var validSections = await _context.GetAllSectionsAsync();
                if (!validSections.Contains(model.Section))
                {
                    ModelState.AddModelError("Section", $"Bagian '{model.Section}' tidak ditemukan di data organisasi");
                }
                else if (!string.IsNullOrEmpty(model.Unit))
                {
                    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
                    if (!validUnits.Contains(model.Unit))
                    {
                        ModelState.AddModelError("Unit", $"Unit '{model.Unit}' tidak valid untuk bagian '{model.Section}'");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("EditWorker validation failed for user {UserId}: {Errors}", model.Id, errors);
                var sectionUnitsDictErr = await _context.GetSectionUnitsDictAsync();
                ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictErr);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Check if email changed and already in use by another user
            if (user.Email != model.Email)
            {
                var emailUser = await _userManager.FindByEmailAsync(model.Email);
                if (emailUser != null && emailUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Email sudah digunakan oleh user lain");
                    return View(model);
                }
                user.UserName = model.Email;
                user.Email = model.Email;
            }

            // Track changes for audit
            var changes = new List<string>();
            if (user.FullName != model.FullName) changes.Add($"Name: '{user.FullName}' → '{model.FullName}'");
            if (user.NIP != model.NIP) changes.Add($"NIP: '{user.NIP}' → '{model.NIP}'");
            if (user.Position != model.Position) changes.Add($"Position: '{user.Position}' → '{model.Position}'");
            if (user.Section != model.Section) changes.Add($"Section: '{user.Section}' → '{model.Section}'");
            if (user.Unit != model.Unit) changes.Add($"Unit: '{user.Unit}' → '{model.Unit}'");

            // Update fields
            user.FullName = model.FullName;
            user.NIP = model.NIP;
            user.Position = model.Position;
            user.Section = model.Section;
            user.Unit = model.Unit;
            user.Directorate = model.Directorate;
            user.JoinDate = model.JoinDate;

            // Update role if changed
            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();
            if (currentRole != model.Role)
            {
                if (currentRole != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, currentRole);
                }
                await _userManager.AddToRoleAsync(user, model.Role);

                var newRoleLevel = UserRoles.GetRoleLevel(model.Role);
                user.RoleLevel = newRoleLevel;

                // Update SelectedView based on new role
                user.SelectedView = UserRoles.GetDefaultView(model.Role);

                changes.Add($"Role: '{currentRole}' → '{model.Role}'");
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // AD mode: password managed via Pertamina portal — never change it here
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            if (!useAD && !string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError("Password", error.Description);
                    }
                    return View(model);
                }
                changes.Add("Password: reset");
            }

            // Audit log
            try
            {
                var actor = await _userManager.GetUserAsync(User);
                var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                await _auditLog.LogAsync(
                    actor?.Id ?? "",
                    actorName,
                    "EditWorker",
                    $"Updated user '{model.FullName}' ({model.Email}). Changes: {(changes.Any() ? string.Join("; ", changes) : "none")}",
                    null,
                    "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for EditWorker (userId={Id})", model.Id); }

            TempData["Success"] = $"Data user '{model.FullName}' berhasil diperbarui.";
            return RedirectToAction("ManageWorkers");
        }

        // POST /Admin/DeleteWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Prevent self-deletion
            if (currentUser.Id == id)
            {
                TempData["Error"] = "Anda tidak dapat menghapus akun Anda sendiri!";
                return RedirectToAction("ManageWorkers");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User tidak ditemukan.";
                return RedirectToAction("ManageWorkers");
            }

            var userName = user.FullName;
            var userEmail = user.Email;

            // Delete related data that uses Restrict delete behavior
            // UserResponses (Restrict on AssessmentSession)
            var userAssessmentIds = await _context.AssessmentSessions
                .Where(a => a.UserId == id)
                .Select(a => a.Id)
                .ToListAsync();

            if (userAssessmentIds.Any())
            {
                var packageUserResponses = await _context.PackageUserResponses
                    .Where(r => userAssessmentIds.Contains(r.AssessmentSessionId))
                    .ToListAsync();
                if (packageUserResponses.Any())
                    _context.PackageUserResponses.RemoveRange(packageUserResponses);

                var packageAssignments = await _context.UserPackageAssignments
                    .Where(a => userAssessmentIds.Contains(a.AssessmentSessionId))
                    .ToListAsync();
                if (packageAssignments.Any())
                    _context.UserPackageAssignments.RemoveRange(packageAssignments);
            }

            // UserCompetencyLevels removed (Phase 227 CLEN-03 — orphan table dropped)

            // ProtonDeliverableProgress (references CoacheeId as string)
            var protonProgress = await _context.ProtonDeliverableProgresses
                .Where(p => p.CoacheeId == id)
                .ToListAsync();
            if (protonProgress.Any())
                _context.ProtonDeliverableProgresses.RemoveRange(protonProgress);

            // ProtonFinalAssessments (Restrict on ProtonTrackAssignment — must be deleted before assignments)
            var protonFinalAssessments = await _context.ProtonFinalAssessments
                .Where(fa => fa.CoacheeId == id)
                .ToListAsync();
            if (protonFinalAssessments.Any())
                _context.ProtonFinalAssessments.RemoveRange(protonFinalAssessments);

            // ProtonTrackAssignments
            var protonAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == id)
                .ToListAsync();
            if (protonAssignments.Any())
                _context.ProtonTrackAssignments.RemoveRange(protonAssignments);

            // ProtonNotifications
            var protonNotifs = await _context.ProtonNotifications
                .Where(n => n.RecipientId == id || n.CoacheeId == id)
                .ToListAsync();
            if (protonNotifs.Any())
                _context.ProtonNotifications.RemoveRange(protonNotifs);

            // CoachCoacheeMappings
            var coachMappings = await _context.CoachCoacheeMappings
                .Where(m => m.CoachId == id || m.CoacheeId == id)
                .ToListAsync();
            if (coachMappings.Any())
                _context.CoachCoacheeMappings.RemoveRange(coachMappings);

            // CoachingSessions
            var coachSessions = await _context.CoachingSessions
                .Where(s => s.CoachId == id || s.CoacheeId == id)
                .ToListAsync();
            if (coachSessions.Any())
                _context.CoachingSessions.RemoveRange(coachSessions);

            // CoachingLogs
            var coachLogs = await _context.CoachingLogs
                .Where(l => l.CoachId == id || l.CoacheeId == id)
                .ToListAsync();
            if (coachLogs.Any())
                _context.CoachingLogs.RemoveRange(coachLogs);

            await _context.SaveChangesAsync();

            // Now delete the user (cascade will handle TrainingRecords, AssessmentSessions, IdpItems)
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                // Audit log
                try
                {
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser.Id,
                        actorName,
                        "DeleteWorker",
                        $"Deleted user '{userName}' ({userEmail})",
                        null,
                        "ApplicationUser");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteWorker (userId={Id})", id); }

                TempData["Success"] = $"User '{userName}' berhasil dihapus dari sistem.";
            }
            else
            {
                TempData["Error"] = $"Gagal menghapus user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction("ManageWorkers");
        }

        // POST /Admin/DeactivateWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (currentUser.Id == id)
            {
                TempData["Error"] = "Anda tidak dapat menonaktifkan akun Anda sendiri!";
                return RedirectToAction("ManageWorkers");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["Error"] = "User tidak ditemukan."; return RedirectToAction("ManageWorkers"); }
            if (!user.IsActive) { TempData["Error"] = $"User '{user.FullName}' sudah tidak aktif."; return RedirectToAction("ManageWorkers"); }

            // Count active coaching and assessments for confirmation message
            var activeCoachingCount = await _context.CoachCoacheeMappings
                .CountAsync(m => (m.CoachId == id || m.CoacheeId == id) && m.IsActive);
            var activeAssessmentCount = await _context.AssessmentSessions
                .CountAsync(a => a.UserId == id && (a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress"));

            // Auto-close active coaching mappings
            var activeMappings = await _context.CoachCoacheeMappings
                .Where(m => (m.CoachId == id || m.CoacheeId == id) && m.IsActive)
                .ToListAsync();
            foreach (var m in activeMappings) { m.IsActive = false; m.EndDate = DateTime.Today; }

            // Cascade: deactivate ProtonTrackAssignments for all deactivated mappings
            var coacheeIds = activeMappings.Where(m => m.CoacheeId == id).Select(m => m.CoacheeId)
                .Union(activeMappings.Where(m => m.CoachId == id).Select(m => m.CoacheeId))
                .Distinct().ToList();
            var activeTrackAssignments = await _context.ProtonTrackAssignments
                .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();
            foreach (var a in activeTrackAssignments) { a.IsActive = false; }
            var trackAssignmentCount = activeTrackAssignments.Count;

            // Auto-cancel active assessment sessions
            var activeSessions = await _context.AssessmentSessions
                .Where(a => a.UserId == id && (a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress"))
                .ToListAsync();
            foreach (var s in activeSessions) { s.Status = "Closed"; }

            // Soft delete: set IsActive = false
            user.IsActive = false;
            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var actorName = string.IsNullOrWhiteSpace(currentUser.NIP) ? currentUser.FullName : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(currentUser.Id, actorName, "DeactivateWorker",
                    $"Nonaktifkan user '{user.FullName}' ({user.Email}). {activeCoachingCount} coaching ditutup, {activeAssessmentCount} assessment dibatalkan, {trackAssignmentCount} track assignment dinonaktifkan. UserId={id}",
                    null, "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeactivateWorker (userId={Id})", id); }

            var detail = "";
            if (activeCoachingCount > 0) detail += $" {activeCoachingCount} coaching aktif ditutup.";
            if (activeAssessmentCount > 0) detail += $" {activeAssessmentCount} assessment dibatalkan.";
            if (trackAssignmentCount > 0) detail += $" {trackAssignmentCount} track assignment dinonaktifkan.";
            TempData["Success"] = $"User '{user.FullName}' berhasil dinonaktifkan.{detail}";
            return RedirectToAction("ManageWorkers", new { showInactive = true });
        }

        // POST /Admin/ReactivateWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["Error"] = "User tidak ditemukan."; return RedirectToAction("ManageWorkers", new { showInactive = true }); }
            if (user.IsActive) { TempData["Error"] = $"User '{user.FullName}' sudah aktif."; return RedirectToAction("ManageWorkers"); }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            try
            {
                var actorName = string.IsNullOrWhiteSpace(currentUser.NIP) ? currentUser.FullName : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(currentUser.Id, actorName, "ReactivateWorker",
                    $"Aktifkan kembali user '{user.FullName}' ({user.Email}). UserId={id}",
                    null, "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ReactivateWorker (userId={Id})", id); }

            TempData["Success"] = $"User '{user.FullName}' berhasil diaktifkan kembali.";
            return RedirectToAction("ManageWorkers", new { showInactive = true });
        }


        // GET /Admin/WorkerDetail
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> WorkerDetail(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.FirstOrDefault() ?? "No Role";

            return View(user);
        }

        // GET /Admin/ImportWorkers
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult ImportWorkers()
        {
            return View();
        }

        // GET /Admin/DownloadImportTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import Workers");

            // Dynamic headers based on auth mode
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            var headers = new List<string>
            {
                "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung (YYYY-MM-DD)"
            };
            if (!useAD)
            {
                headers.Add("Password");
            }
            for (int i = 0; i < headers.Count; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var example = new List<object>
            {
                "Ahmad Fauzi", "ahmad.fauzi@pertamina.com", "123456", "Operator",
                "RFCC", "RFCC LPG Treating Unit (062)", "CSU Process", "Coachee", "2024-01-15"
            };
            if (!useAD) { example.Add("Password123!"); }
            for (int i = 0; i < example.Count; i++)
            {
                ws.Cell(2, i + 1).Value = example[i]?.ToString();
                ws.Cell(2, i + 1).Style.Font.Italic = true;
                ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray;
            }

            var sections = await _context.GetAllSectionsAsync();
            ws.Cell(3, 1).Value = $"Kolom Bagian: {string.Join(" / ", sections)}";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;
            ws.Cell(4, 1).Value = $"Kolom Role: {string.Join(" / ", UserRoles.AllRoles)}";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;
            if (useAD)
            {
                ws.Cell(5, 1).Value = "Mode AD aktif: Kolom Password tidak diperlukan. Sistem akan membuat password acak.";
                ws.Cell(5, 1).Style.Font.Italic = true;
                ws.Cell(5, 1).Style.Font.FontColor = XLColor.DarkBlue;
            }

            return ExcelExportHelper.ToFileResult(workbook, "workers_import_template.xlsx", this);
        }

        // POST /Admin/ImportWorkers
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportWorkers(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Pilih file Excel terlebih dahulu.";
                return View();
            }

            var allowedImportExtensions = new[] { ".xlsx", ".xls" };
            var importExt = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedImportExtensions.Contains(importExt))
            {
                TempData["Error"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                return View();
            }

            const long maxImportSize = 10 * 1024 * 1024; // 10MB
            if (excelFile.Length > maxImportSize)
            {
                TempData["Error"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return View();
            }

            var results = new List<ImportWorkerResult>();
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();

                var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nama = (row.Cell(1).GetString() ?? "").Trim();
                    var email = (row.Cell(2).GetString() ?? "").Trim();
                    var nip = (row.Cell(3).GetString() ?? "").Trim();
                    var jabatan = (row.Cell(4).GetString() ?? "").Trim();
                    var bagian = (row.Cell(5).GetString() ?? "").Trim();
                    var unit = (row.Cell(6).GetString() ?? "").Trim();
                    var directorate = (row.Cell(7).GetString() ?? "").Trim();
                    var role = (row.Cell(8).GetString() ?? "").Trim();
                    var tglStr = (row.Cell(9).GetString() ?? "").Trim();

                    // Skip blank rows (e.g. notes/example rows)
                    if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(email)) continue;

                    var result = new ImportWorkerResult { Nama = nama, Email = email, Role = role };

                    // AD mode generates password; local mode reads from column 10
                    string password;
                    if (useAD)
                    {
                        password = GenerateRandomPassword();
                    }
                    else
                    {
                        password = (row.Cell(10).GetString() ?? "").Trim();
                    }

                    var errors = new List<string>();
                    if (string.IsNullOrWhiteSpace(nama)) errors.Add("Nama kosong");
                    if (string.IsNullOrWhiteSpace(email)) errors.Add("Email kosong");
                    if (!useAD && string.IsNullOrWhiteSpace(password)) errors.Add("Password kosong");
                    if (string.IsNullOrWhiteSpace(role) || !UserRoles.AllRoles.Contains(role))
                        errors.Add($"Role tidak valid");

                    // Validasi Section terhadap OrganizationUnit database
                    if (!string.IsNullOrWhiteSpace(bagian) && !sectionUnitsDict.ContainsKey(bagian))
                        errors.Add($"Section '{bagian}' tidak ditemukan di database");

                    // Validasi Unit: harus child dari Section yang dipilih
                    if (!string.IsNullOrWhiteSpace(unit))
                    {
                        if (string.IsNullOrWhiteSpace(bagian))
                            errors.Add("Unit tidak boleh diisi tanpa Section");
                        else if (sectionUnitsDict.TryGetValue(bagian, out var validUnits) && !validUnits.Contains(unit))
                            errors.Add($"Unit '{unit}' bukan child dari Section '{bagian}'");
                    }

                    if (errors.Any())
                    {
                        result.Status = "Error";
                        result.Message = string.Join("; ", errors);
                        results.Add(result);
                        continue;
                    }

                    var existing = await _userManager.FindByEmailAsync(email);
                    if (existing != null)
                    {
                        if (!existing.IsActive)
                        {
                            result.Status = "PerluReview";
                            result.Message = "Email terdaftar tapi tidak aktif — dapat diaktifkan kembali";
                            result.ExistingUserId = existing.Id;
                        }
                        else
                        {
                            result.Status = "Skip";
                            result.Message = "Email sudah terdaftar, dilewati";
                        }
                        results.Add(result);
                        continue;
                    }

                    DateTime? joinDate = null;
                    if (!string.IsNullOrWhiteSpace(tglStr) && DateTime.TryParse(tglStr, out var parsedDate))
                        joinDate = parsedDate;

                    var roleLevel = UserRoles.GetRoleLevel(role);
                    var selectedView = UserRoles.GetDefaultView(role);

                    var newUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = nama,
                        NIP = string.IsNullOrWhiteSpace(nip) ? null : nip,
                        Position = string.IsNullOrWhiteSpace(jabatan) ? null : jabatan,
                        Section = string.IsNullOrWhiteSpace(bagian) ? null : bagian,
                        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                        Directorate = string.IsNullOrWhiteSpace(directorate) ? null : directorate,
                        JoinDate = joinDate,
                        RoleLevel = roleLevel,
                        SelectedView = selectedView
                    };

                    var createResult = await _userManager.CreateAsync(newUser, password);
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, role);
                        result.Status = "Success";
                        result.Message = "Berhasil dibuat";
                    }
                    else
                    {
                        result.Status = "Error";
                        result.Message = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    }

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read Excel import file");
                TempData["Error"] = "Gagal membaca file Excel. Pastikan format file benar.";
                return View();
            }

            // Audit log
            try
            {
                var actor = await _userManager.GetUserAsync(User);
                var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                var successCount = results.Count(r => r.Status == "Success");
                var reviewCount = results.Count(r => r.Status == "PerluReview");
                await _auditLog.LogAsync(actor?.Id ?? "", actorName, "ImportWorkers",
                    $"Bulk import: {successCount} berhasil, {results.Count(r => r.Status == "Error")} error, {results.Count(r => r.Status == "Skip")} dilewati, {reviewCount} perlu review",
                    null, "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ImportWorkers"); }

            ViewBag.ImportResults = results;
            return View();
        }

        // GET /Admin/CoachCoacheeMappingExport
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMappingExport()
        {
            var mappings = await _context.CoachCoacheeMappings
                .OrderBy(m => m.CoachId)
                .ThenBy(m => m.StartDate)
                .ToListAsync();

            var allUserIds = mappings.SelectMany(m => new[] { m.CoachId, m.CoacheeId }).Distinct().ToList();
            var allUsers = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => new {
                    u.Id,
                    FullName = u.FullName ?? "",
                    NIP = u.NIP ?? "",
                    Section = u.Section ?? "",
                    Position = u.Position ?? ""
                })
                .ToDictionaryAsync(u => u.Id);

            var coacheeIds = mappings.Select(m => m.CoacheeId).Distinct().ToList();
            var activeTrackAssignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();
            var trackByCoachee = activeTrackAssignments
                .GroupBy(a => a.CoacheeId)
                .ToDictionary(g => g.Key, g => g.First().ProtonTrack?.DisplayName ?? "");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Coach-Coachee Mapping");

            // Header row
            var headers = new[] {
                "Coach Name", "Coach Section", "Coachee Name", "Coachee NIP",
                "Coachee Section", "Coachee Position", "Bagian Penugasan", "Unit Penugasan",
                "Current Track", "Status", "Start Date", "End Date"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.DarkGray;
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data rows
            int row = 2;
            foreach (var m in mappings)
            {
                var coach = allUsers.GetValueOrDefault(m.CoachId);
                var coachee = allUsers.GetValueOrDefault(m.CoacheeId);
                var track = trackByCoachee.GetValueOrDefault(m.CoacheeId, "");
                var status = m.IsActive ? "Active" : "Inactive";

                ws.Cell(row, 1).Value = coach?.FullName ?? m.CoachId;
                ws.Cell(row, 2).Value = coach?.Section ?? "";
                ws.Cell(row, 3).Value = coachee?.FullName ?? m.CoacheeId;
                ws.Cell(row, 4).Value = coachee?.NIP ?? "";
                ws.Cell(row, 5).Value = coachee?.Section ?? "";
                ws.Cell(row, 6).Value = coachee?.Position ?? "";
                ws.Cell(row, 7).Value = string.IsNullOrEmpty(m.AssignmentSection) ? "\u2014" : m.AssignmentSection;
                ws.Cell(row, 8).Value = string.IsNullOrEmpty(m.AssignmentUnit) ? "\u2014" : m.AssignmentUnit;
                ws.Cell(row, 9).Value = track;
                ws.Cell(row, 10).Value = status;
                ws.Cell(row, 11).Value = m.StartDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 12).Value = m.EndDate.HasValue ? m.EndDate.Value.ToString("yyyy-MM-dd") : "";
                row++;
            }

            return ExcelExportHelper.ToFileResult(workbook, "CoachCoacheeMapping.xlsx", this);
        }

        /// <summary>
        /// AJAX: Returns coachees eligible for a Proton exam — assigned to the track + 100% deliverables Approved.
        /// Called from CreateAssessment form JS when category=Assessment Proton and track is selected.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetEligibleCoachees(int protonTrackId)
        {
            if (protonTrackId <= 0) return Json(new List<object>());

            // Coachees with active assignment to this track
            var assignedCoacheeIds = await _context.ProtonTrackAssignments
                .Where(a => a.ProtonTrackId == protonTrackId && a.IsActive)
                .Select(a => a.CoacheeId)
                .Distinct()
                .ToListAsync();

            if (!assignedCoacheeIds.Any()) return Json(new List<object>());

            // All deliverable IDs for this track (via Kompetensi → SubKompetensi → Deliverable)
            var trackDeliverableIds = await _context.ProtonKompetensiList
                .Where(k => k.ProtonTrackId == protonTrackId)
                .SelectMany(k => k.SubKompetensiList)
                .SelectMany(s => s.Deliverables)
                .Select(d => d.Id)
                .ToListAsync();

            // Tahun 3 (interview) tracks have no deliverables — all assigned coachees are eligible
            if (!trackDeliverableIds.Any())
            {
                var allAssigned = await _context.Users
                    .Where(u => assignedCoacheeIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName, u.Email, u.NIP, u.Section })
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
                return Json(allAssigned);
            }

            // Batch-load progress records for all assigned coachees on this track's deliverables
            var progressRecords = await _context.ProtonDeliverableProgresses
                .Where(p => assignedCoacheeIds.Contains(p.CoacheeId)
                         && trackDeliverableIds.Contains(p.ProtonDeliverableId))
                .Select(p => new { p.CoacheeId, p.ProtonDeliverableId, p.Status })
                .ToListAsync();

            // Eligible = has exactly trackDeliverableIds.Count Approved progress records
            var eligibleCoacheeIds = assignedCoacheeIds
                .Where(id =>
                {
                    var mine = progressRecords.Where(p => p.CoacheeId == id).ToList();
                    return mine.Count == trackDeliverableIds.Count && mine.All(p => p.Status == "Approved");
                })
                .ToList();

            if (!eligibleCoacheeIds.Any()) return Json(new List<object>());

            var users = await _context.Users
                .Where(u => eligibleCoacheeIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email, u.NIP, u.Section })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Json(users);
        }

        // Helper: Crypto-random 16-char password for AD mode auto-generation
        private static string GenerateRandomPassword()
        {
            var bytes = new byte[12];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            // Base64 ensures uppercase + lowercase + digits, no special chars that break Identity validation
            return Convert.ToBase64String(bytes);
        }

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

        #region Proton Progress Helpers

        private async Task<List<string>> AutoCreateProgressForAssignment(int assignmentId, int protonTrackId, string coacheeId)
        {
            var warnings = new List<string>();

            // Resolve unit: AssignmentUnit from active mapping, fallback to User.Unit
            var assignmentUnit = await _context.CoachCoacheeMappings
                .Where(m => m.CoacheeId == coacheeId && m.IsActive)
                .Select(m => m.AssignmentUnit)
                .FirstOrDefaultAsync();

            var resolvedUnit = assignmentUnit;
            if (string.IsNullOrWhiteSpace(resolvedUnit))
            {
                resolvedUnit = await _context.Users
                    .Where(u => u.Id == coacheeId)
                    .Select(u => u.Unit)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(resolvedUnit))
            {
                warnings.Add($"Coachee {coacheeId} tidak memiliki AssignmentUnit maupun Unit — progress tidak dibuat.");
                return warnings;
            }

            var deliverableIds = await _context.ProtonDeliverableList
                .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
                         && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())
                .Select(d => d.Id)
                .ToListAsync();

            if (!deliverableIds.Any())
            {
                var trackName = await _context.ProtonTracks
                    .Where(t => t.Id == protonTrackId)
                    .Select(t => t.DisplayName)
                    .FirstOrDefaultAsync() ?? protonTrackId.ToString();
                warnings.Add($"Tidak ada deliverable untuk unit {resolvedUnit} di track {trackName}.");
                return warnings;
            }

            var progresses = deliverableIds.Select(dId => new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = dId,
                ProtonTrackAssignmentId = assignmentId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.ProtonDeliverableProgresses.AddRange(progresses);
            await _context.SaveChangesAsync(); // flush to get IDs for StatusHistory

            // D-17: Insert initial "Pending" StatusHistory for each new progress
            foreach (var p in progresses)
            {
                _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
                {
                    ProtonDeliverableProgressId = p.Id,
                    StatusType = "Pending",
                    ActorId = "system",
                    ActorName = "System",
                    ActorRole = "System",
                    Timestamp = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            return warnings;
        }

        private async Task CleanupProgressForAssignment(int assignmentId)
        {
            var progressIds = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == assignmentId)
                .Select(p => p.Id)
                .ToListAsync();

            if (!progressIds.Any()) return;

            var histories = await _context.DeliverableStatusHistories
                .Where(h => progressIds.Contains(h.ProtonDeliverableProgressId))
                .ToListAsync();
            _context.DeliverableStatusHistories.RemoveRange(histories);

            var sessions = await _context.CoachingSessions
                .Where(s => s.ProtonDeliverableProgressId.HasValue && progressIds.Contains(s.ProtonDeliverableProgressId.Value))
                .ToListAsync();
            _context.CoachingSessions.RemoveRange(sessions);

            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == assignmentId)
                .ToListAsync();
            _context.ProtonDeliverableProgresses.RemoveRange(progresses);
        }

        #endregion


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

        #region Organization Management

        // GET /Admin/ManageOrganization
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageOrganization(int? editId)
        {
            var roots = await _context.OrganizationUnits
                .Include(u => u.Children.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
                    .ThenInclude(c => c.Children.OrderBy(gc => gc.DisplayOrder).ThenBy(gc => gc.Name))
                .Where(u => u.ParentId == null)
                .OrderBy(u => u.DisplayOrder)
                .ThenBy(u => u.Name)
                .ToListAsync();

            ViewBag.PotentialParents = await _context.OrganizationUnits
                .Where(u => u.IsActive)
                .OrderBy(u => u.Level)
                .ThenBy(u => u.DisplayOrder)
                .ToListAsync();

            if (editId.HasValue)
            {
                ViewBag.EditUnit = await _context.OrganizationUnits.FindAsync(editId.Value);
            }

            return View("ManageOrganization", roots);
        }

        // POST /Admin/AddOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrganizationUnit(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Nama tidak boleh kosong.";
                TempData["ShowAddForm"] = true;
                return RedirectToAction("ManageOrganization");
            }

            bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim());
            if (duplicate)
            {
                TempData["Error"] = "Nama unit sudah digunakan. Gunakan nama yang berbeda.";
                TempData["ShowAddForm"] = true;
                return RedirectToAction("ManageOrganization");
            }

            int level = 0;
            if (parentId.HasValue)
            {
                var parent = await _context.OrganizationUnits.FindAsync(parentId.Value);
                level = parent != null ? parent.Level + 1 : 0;
            }

            int maxOrder = await _context.OrganizationUnits
                .Where(u => u.ParentId == parentId)
                .MaxAsync(u => (int?)u.DisplayOrder) ?? 0;

            var unit = new OrganizationUnit
            {
                Name = name.Trim(),
                ParentId = parentId,
                Level = level,
                DisplayOrder = maxOrder + 1,
                IsActive = true
            };

            _context.OrganizationUnits.Add(unit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Unit berhasil ditambahkan.";
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/EditOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrganizationUnit(int id, string name, int? parentId)
        {
            var unit = await _context.OrganizationUnits.FindAsync(id);
            if (unit == null) return NotFound();

            string oldName = unit.Name;
            int? oldParentId = unit.ParentId;

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Nama tidak boleh kosong.";
                return RedirectToAction("ManageOrganization", new { editId = id });
            }

            bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim() && u.Id != id);
            if (duplicate)
            {
                TempData["Error"] = "Nama unit sudah digunakan. Gunakan nama yang berbeda.";
                return RedirectToAction("ManageOrganization", new { editId = id });
            }

            if (parentId.HasValue)
            {
                if (parentId.Value == id)
                {
                    TempData["Error"] = "Tidak dapat memindahkan unit ke sub-unitnya sendiri (circular reference).";
                    return RedirectToAction("ManageOrganization", new { editId = id });
                }

                bool isDescendant = await IsDescendantAsync(id, parentId.Value);
                if (isDescendant)
                {
                    TempData["Error"] = "Tidak dapat memindahkan unit ke sub-unitnya sendiri (circular reference).";
                    return RedirectToAction("ManageOrganization", new { editId = id });
                }
            }

            if (unit.ParentId != parentId)
            {
                int newLevel = 0;
                if (parentId.HasValue)
                {
                    var newParent = await _context.OrganizationUnits.FindAsync(parentId.Value);
                    newLevel = newParent != null ? newParent.Level + 1 : 0;
                }
                unit.ParentId = parentId;
                unit.Level = newLevel;
                await UpdateChildrenLevelsAsync(unit);
            }

            // Cascade rename and reparent to denormalized fields
            int cascadedUsers = 0;
            int cascadedMappings = 0;

            // Cascade rename
            if (oldName != name.Trim())
            {
                if (unit.Level == 0)
                {
                    var affectedUsers = await _context.Users.Where(u => u.Section == oldName).ToListAsync();
                    foreach (var u in affectedUsers) u.Section = name.Trim();
                    cascadedUsers += affectedUsers.Count;

                    var affectedMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentSection == oldName).ToListAsync();
                    foreach (var m in affectedMappings) m.AssignmentSection = name.Trim();
                    cascadedMappings += affectedMappings.Count;
                }
                else
                {
                    var affectedUsers = await _context.Users.Where(u => u.Unit == oldName).ToListAsync();
                    foreach (var u in affectedUsers) u.Unit = name.Trim();
                    cascadedUsers += affectedUsers.Count;

                    var affectedMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentUnit == oldName).ToListAsync();
                    foreach (var m in affectedMappings) m.AssignmentUnit = name.Trim();
                    cascadedMappings += affectedMappings.Count;
                }
            }

            // Cascade reparent — update Section for users in this unit when parent changes
            if (oldParentId != parentId && unit.Level >= 1)
            {
                // Find root ancestor (Level 0) from new parent
                string newSectionName = "";
                if (parentId.HasValue)
                {
                    var ancestor = await _context.OrganizationUnits.FindAsync(parentId.Value);
                    while (ancestor != null && ancestor.Level > 0 && ancestor.ParentId.HasValue)
                    {
                        ancestor = await _context.OrganizationUnits.FindAsync(ancestor.ParentId.Value);
                    }
                    if (ancestor != null) newSectionName = ancestor.Name;
                }

                if (!string.IsNullOrEmpty(newSectionName))
                {
                    var reparentUsers = await _context.Users.Where(u => u.Unit == oldName).ToListAsync();
                    foreach (var u in reparentUsers) u.Section = newSectionName;
                    cascadedUsers += reparentUsers.Count;

                    var reparentMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentUnit == oldName).ToListAsync();
                    foreach (var m in reparentMappings) m.AssignmentSection = newSectionName;
                    cascadedMappings += reparentMappings.Count;
                }
            }

            unit.Name = name.Trim();
            await _context.SaveChangesAsync();

            if (cascadedUsers > 0 || cascadedMappings > 0)
                TempData["Success"] = $"Unit berhasil diperbarui. {cascadedUsers} user dan {cascadedMappings} mapping terupdate.";
            else
                TempData["Success"] = "Unit berhasil diperbarui.";
            return RedirectToAction("ManageOrganization");
        }

        private async Task<bool> IsDescendantAsync(int nodeId, int targetId)
        {
            var current = await _context.OrganizationUnits.FindAsync(targetId);
            while (current != null && current.ParentId.HasValue)
            {
                if (current.ParentId.Value == nodeId) return true;
                current = await _context.OrganizationUnits.FindAsync(current.ParentId.Value);
            }
            return false;
        }

        private async Task UpdateChildrenLevelsAsync(OrganizationUnit unit)
        {
            var children = await _context.OrganizationUnits
                .Where(u => u.ParentId == unit.Id)
                .ToListAsync();

            foreach (var child in children)
            {
                child.Level = unit.Level + 1;
                await UpdateChildrenLevelsAsync(child);
            }
        }

        // POST /Admin/ToggleOrganizationUnitActive
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOrganizationUnitActive(int id)
        {
            var unit = await _context.OrganizationUnits
                .Include(u => u.Children)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return NotFound();

            if (unit.IsActive && unit.Children.Any(c => c.IsActive))
            {
                TempData["Error"] = "Nonaktifkan semua unit di bawahnya terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            if (unit.IsActive)
            {
                bool hasActiveUsers;
                if (unit.Level == 0)
                    hasActiveUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name);
                else
                    hasActiveUsers = await _context.Users.AnyAsync(u => u.Unit == unit.Name);

                if (hasActiveUsers)
                {
                    TempData["Error"] = "Tidak dapat menonaktifkan unit. Masih ada user aktif yang terdaftar di unit ini. Pindahkan semua user terlebih dahulu.";
                    return RedirectToAction("ManageOrganization");
                }
            }

            unit.IsActive = !unit.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Status berhasil diubah menjadi {(unit.IsActive ? "Aktif" : "Nonaktif")}.";
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/DeleteOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrganizationUnit(int id)
        {
            var unit = await _context.OrganizationUnits
                .Include(u => u.Children)
                .Include(u => u.KkjFiles)
                .Include(u => u.CpdpFiles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return NotFound();

            if (unit.Children.Any(c => c.IsActive))
            {
                TempData["Error"] = "Hapus atau nonaktifkan unit di bawahnya terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            if (unit.KkjFiles.Any() || unit.CpdpFiles.Any())
            {
                TempData["Error"] = "Unit ini masih memiliki file KKJ/CPDP yang ter-assign. Hapus file terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            bool hasUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name || u.Unit == unit.Name);
            if (hasUsers)
            {
                TempData["Error"] = "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            _context.OrganizationUnits.Remove(unit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Unit berhasil dihapus.";
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/ReorderOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderOrganizationUnit(int id, string direction)
        {
            var unit = await _context.OrganizationUnits.FindAsync(id);
            if (unit == null) return NotFound();

            var siblings = await _context.OrganizationUnits
                .Where(u => u.ParentId == unit.ParentId)
                .OrderBy(u => u.DisplayOrder)
                .ToListAsync();

            int index = siblings.FindIndex(u => u.Id == id);

            if (direction == "up" && index > 0)
            {
                var prev = siblings[index - 1];
                (unit.DisplayOrder, prev.DisplayOrder) = (prev.DisplayOrder, unit.DisplayOrder);
            }
            else if (direction == "down" && index < siblings.Count - 1)
            {
                var next = siblings[index + 1];
                (unit.DisplayOrder, next.DisplayOrder) = (next.DisplayOrder, unit.DisplayOrder);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManageOrganization");
        }

        #endregion
    }
}

public class CoachAssignRequest
{
    public string CoachId { get; set; } = "";
    public List<string> CoacheeIds { get; set; } = new();
    public int? ProtonTrackId { get; set; }
    public DateTime? StartDate { get; set; }
    public string? AssignmentSection { get; set; }
    public string? AssignmentUnit { get; set; }
    /// <summary>D-09: If true, user confirmed to proceed despite incomplete progression warning.</summary>
    public bool ConfirmProgressionWarning { get; set; }
}

public class CoachEditRequest
{
    public int MappingId { get; set; }
    public string CoachId { get; set; } = "";
    public int? ProtonTrackId { get; set; }
    public DateTime? StartDate { get; set; }
    public string? AssignmentSection { get; set; }
    public string? AssignmentUnit { get; set; }
}
