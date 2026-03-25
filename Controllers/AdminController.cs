using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using HcPortal.Hubs;
using Microsoft.AspNetCore.SignalR;
using ClosedXML.Excel;
using System.Globalization;
using HcPortal.Helpers;

namespace HcPortal.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLog;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<AssessmentHub> _hubContext;
        private readonly IWorkerDataService _workerDataService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IMemoryCache cache,
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<AdminController> logger,
            INotificationService notificationService,
            IHubContext<AssessmentHub> hubContext,
            IWorkerDataService workerDataService)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _cache = cache;
            _config = config;
            _env = env;
            _logger = logger;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _workerDataService = workerDataService;
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

        // GET /Admin/ManageAssessment
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageAssessment(string? search, int page = 1, int pageSize = 20,
            string? tab = null, string? section = null, string? unit = null,
            string? category = null, string? statusFilter = null, string? isFiltered = null)
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var managementQuery = _context.AssessmentSessions
                .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                managementQuery = managementQuery.Where(a =>
                    a.Title.ToLower().Contains(lowerSearch) ||
                    a.Category.ToLower().Contains(lowerSearch) ||
                    (a.User != null && (
                        a.User.FullName.ToLower().Contains(lowerSearch) ||
                        (a.User.NIP != null && a.User.NIP.Contains(lowerSearch))
                    ))
                );
            }

            // Category filter — applied before DB fetch for efficiency
            if (!string.IsNullOrEmpty(category))
                managementQuery = managementQuery.Where(a => a.Category == category);

            var allSessions = await managementQuery
                .Include(a => a.User)
                .OrderByDescending(a => a.Schedule)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    a.ExamWindowCloseDate,
                    a.DurationMinutes,
                    a.Status,
                    a.IsTokenRequired,
                    a.AccessToken,
                    a.PassPercentage,
                    a.AllowAnswerReview,
                    a.CreatedAt,
                    UserFullName = a.User != null ? a.User.FullName : "Unknown",
                    UserEmail = a.User != null ? a.User.Email : "",
                    UserId = a.User != null ? a.User.Id : ""
                })
                .ToListAsync();

            // Group by (Title, Category, Schedule.Date) — identical to CMPController manage branch
            var grouped = allSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g =>
                {
                    var rep = g.OrderBy(a => a.CreatedAt).First();
                    // Compute GroupStatus from session statuses
                    string groupStatus;
                    if (g.Any(a => a.Status == "Open" || a.Status == "InProgress"))
                        groupStatus = "Open";
                    else if (g.Any(a => a.Status == "Upcoming"))
                        groupStatus = "Upcoming";
                    else
                        groupStatus = "Closed";
                    return new
                    {
                        rep.Title,
                        rep.Category,
                        rep.Schedule,
                        rep.ExamWindowCloseDate,
                        rep.DurationMinutes,
                        rep.Status,
                        rep.IsTokenRequired,
                        rep.AccessToken,
                        rep.PassPercentage,
                        rep.AllowAnswerReview,
                        RepresentativeId = rep.Id,
                        Users = g.Select(a => new { a.UserFullName, a.UserEmail, a.UserId }).ToList(),
                        AllIds = g.Select(a => a.Id).ToList(),
                        UserCount = g.Count(),
                        GroupStatus = groupStatus
                    };
                })
                .OrderByDescending(g => g.Schedule)
                .ToList();

            // Status filter — applied AFTER grouping (GroupStatus computed from sessions)
            // Default: show Open + Upcoming only (exclude Closed) unless statusFilter param is provided
            if (string.IsNullOrEmpty(statusFilter))
                grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
            else if (statusFilter == "Open" || statusFilter == "Upcoming" || statusFilter == "Closed")
                grouped = grouped.Where(g => g.GroupStatus == statusFilter).ToList();
            // statusFilter == "All" → no filter applied

            // Fetch distinct categories for dropdown
            ViewBag.Categories = await _context.AssessmentSessions
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            ViewBag.SelectedCategory = category ?? "";
            ViewBag.SelectedStatus = statusFilter ?? "";

            var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);

            ViewBag.ManagementData = grouped
                .Skip(paging.Skip)
                .Take(paging.Take)
                .ToList();
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.SearchTerm = search;

            // Tab routing — default to "assessment"
            var activeTab = tab switch { "training" => "training", "history" => "history", _ => "assessment" };
            ViewBag.ActiveTab = activeTab;

            // Training tab data (lazy — only fetch when tab=training or tab=history)
            if (activeTab == "training" || activeTab == "history")
            {
                bool isInitialState = string.IsNullOrEmpty(isFiltered);
                ViewBag.IsInitialState = isInitialState;
                ViewBag.SelectedSection = section;
                ViewBag.SelectedUnit = unit;
                ViewBag.SelectedCategory = category;
                ViewBag.SelectedStatus = statusFilter;

                List<WorkerTrainingStatus> workers;
                if (isInitialState)
                    workers = new List<WorkerTrainingStatus>();
                else
                    workers = await _workerDataService.GetWorkersInSection(section, unit, category, search, statusFilter);

                var (assessmentHistory, trainingHistory) = await _workerDataService.GetAllWorkersHistory();
                ViewBag.Workers = workers;
                ViewBag.AssessmentHistory = assessmentHistory;
                ViewBag.TrainingHistory = trainingHistory;
                ViewBag.AssessmentTitles = assessmentHistory
                    .Select(r => r.Title)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct().OrderBy(t => t).ToList();
            }

            return View();
        }

        // --- CATEGORY MANAGEMENT ---

        private async Task SetCategoriesViewBag()
        {
            // Hierarchical tree for ManageCategories table + optgroup dropdowns
            var parentCategories = await _context.AssessmentCategories
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
                    .ThenInclude(ch => ch.Children.OrderBy(gc => gc.SortOrder).ThenBy(gc => gc.Name))
                .Include(c => c.Signatory)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.ParentCategories = parentCategories;

            // All users for signatory dropdown
            var allUsers = await _userManager.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.NIP, u.Position })
                .ToListAsync();
            ViewBag.AllUsers = allUsers;

            // Potential parents for Parent Category dropdown (depth 0 or 1 only)
            var potentialParents = await _context.AssessmentCategories
                .Include(c => c.Parent)
                .Where(c => c.ParentId == null || (c.Parent != null && c.Parent.ParentId == null))
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.PotentialParents = potentialParents;
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

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageCategories()
        {
            await SetCategoriesViewBag();
            var parentCategories = (IEnumerable<AssessmentCategory>)ViewBag.ParentCategories;
            return View(parentCategories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, int defaultPassPercentage, int sortOrder, int? parentId, string? signatoryUserId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Nama kategori tidak boleh kosong.";
                return RedirectToAction("ManageCategories");
            }

            if (await _context.AssessmentCategories.AnyAsync(c => c.Name == name))
            {
                TempData["Error"] = "Nama kategori sudah digunakan. Gunakan nama yang berbeda.";
                return RedirectToAction("ManageCategories");
            }

            var category = new AssessmentCategory
            {
                Name = name.Trim(),
                DefaultPassPercentage = defaultPassPercentage,
                SortOrder = sortOrder,
                IsActive = true,
                ParentId = parentId,
                SignatoryUserId = string.IsNullOrWhiteSpace(signatoryUserId) ? null : signatoryUserId
            };
            _context.AssessmentCategories.Add(category);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "AddCategory",
                $"Added assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Kategori berhasil ditambahkan.";
            return RedirectToAction("ManageCategories");
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            await SetCategoriesViewBag();
            ViewBag.EditCategory = category;
            var parentCategories = (IEnumerable<AssessmentCategory>)ViewBag.ParentCategories;
            return View("ManageCategories", parentCategories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, int defaultPassPercentage, int sortOrder, int? parentId, string? signatoryUserId)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Nama kategori tidak boleh kosong.";
                return RedirectToAction("ManageCategories");
            }

            if (await _context.AssessmentCategories.AnyAsync(c => c.Name == name && c.Id != id))
            {
                TempData["Error"] = "Nama kategori sudah digunakan. Gunakan nama yang berbeda.";
                return RedirectToAction("ManageCategories");
            }

            category.Name = name.Trim();
            category.DefaultPassPercentage = defaultPassPercentage;
            category.SortOrder = sortOrder;
            category.ParentId = parentId;
            category.SignatoryUserId = string.IsNullOrWhiteSpace(signatoryUserId) ? null : signatoryUserId;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "EditCategory",
                $"Updated assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Kategori berhasil diperbarui.";
            return RedirectToAction("ManageCategories");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (await _context.AssessmentCategories.AnyAsync(c => c.ParentId == id))
            {
                TempData["Error"] = "Hapus sub-kategori terlebih dahulu.";
                return RedirectToAction("ManageCategories");
            }

            _context.AssessmentCategories.Remove(category);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteCategory",
                $"Deleted assessment category '{category.Name}'",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Kategori berhasil dihapus.";
            return RedirectToAction("ManageCategories");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "ToggleCategoryActive",
                $"Toggled category '{category.Name}' to {(category.IsActive ? "Active" : "Inactive")}",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Status kategori berhasil diubah.";
            return RedirectToAction("ManageCategories");
        }

        // --- CREATE ASSESSMENT ---
        // GET: Show create assessment form
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CreateAssessment([FromQuery] List<int>? renewSessionId = null, [FromQuery] List<int>? renewTrainingId = null)
        {
            // Get list of users for dropdown
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.SelectedUserIds = new List<string>();
            ViewBag.Sections = await _context.GetAllSectionsAsync();
            ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.Categories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var parentCats = await _context.AssessmentCategories
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.ParentCategories = parentCats;

            // Pass created assessment data to view if exists (for success modal)
            if (TempData["CreatedAssessment"] != null)
            {
                ViewBag.CreatedAssessment = TempData["CreatedAssessment"];
            }

            // Pre-populate model with secure token
            var model = new AssessmentSession
            {
                AccessToken = GenerateSecureToken(),
                Schedule = DateTime.Today.AddDays(1),  // Default to tomorrow
                PassPercentage = 70,
                AllowAnswerReview = true
            };

            // ===== Phase 201 / 210: Renewal mode pre-fill =====
            bool isRenewalMode = false;

            if (renewSessionId != null && renewSessionId.Count > 0)
            {
                if (renewSessionId.Count == 1)
                {
                    // Single renew — backward-compatible path
                    var sourceSession = await _context.AssessmentSessions
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == renewSessionId[0]);

                    if (sourceSession == null)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    isRenewalMode = true;
                    model.Title = sourceSession.Title;
                    model.Category = sourceSession.Category;
                    model.GenerateCertificate = true;
                    if (sourceSession.ValidUntil.HasValue)
                        model.ValidUntil = sourceSession.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    ViewBag.SelectedUserIds = new List<string> { sourceSession.UserId };
                    ViewBag.RenewalSourceTitle = sourceSession.Title;
                    ViewBag.RenewalSourceUserName = sourceSession.User?.FullName ?? "";
                    ViewBag.RenewsSessionId = renewSessionId[0];
                }
                else
                {
                    // Bulk renew — build per-user FK mapping
                    var sourceSessions = await _context.AssessmentSessions
                        .Include(s => s.User)
                        .Where(s => renewSessionId.Contains(s.Id))
                        .ToListAsync();

                    if (sourceSessions.Count == 0)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    var firstSession = sourceSessions[0];
                    isRenewalMode = true;
                    model.Title = firstSession.Title;
                    model.Category = firstSession.Category;
                    model.GenerateCertificate = true;
                    if (firstSession.ValidUntil.HasValue)
                        model.ValidUntil = firstSession.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    // Build {UserId → SessionId} map (GroupBy to handle duplicate UserId safely)
                    var fkMap = sourceSessions
                        .GroupBy(s => s.UserId)
                        .ToDictionary(g => g.Key, g => g.First().Id);
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(fkMap);
                    ViewBag.RenewalFkMapType = "session";

                    ViewBag.SelectedUserIds = sourceSessions.Select(s => s.UserId).ToList();
                    ViewBag.RenewalSourceTitle = firstSession.Title;
                    ViewBag.RenewalSourceUserName = string.Join(", ", sourceSessions.Select(s => s.User?.FullName ?? ""));
                    // model.RenewsSessionId = null intentionally — resolved per-user at POST
                }
            }
            else if (renewTrainingId != null && renewTrainingId.Count > 0)
            {
                // Build category lookup for MapKategori DB lookup (LDAT-05)
                var catsForRenewal = (await _context.AssessmentCategories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .ToListAsync())
                    .GroupBy(c => c.Name.ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Name);
                if (!catsForRenewal.ContainsKey("MANDATORY")) catsForRenewal["MANDATORY"] = "Mandatory HSSE Training";
                if (!catsForRenewal.ContainsKey("PROTON")) catsForRenewal["PROTON"] = "Assessment Proton";

                if (renewTrainingId.Count == 1)
                {
                    // Single renew — backward-compatible path
                    var sourceTraining = await _context.TrainingRecords
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == renewTrainingId[0]);

                    if (sourceTraining == null)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    isRenewalMode = true;
                    model.Title = sourceTraining.Judul ?? "";
                    model.Category = MapKategori(sourceTraining.Kategori, catsForRenewal);
                    model.GenerateCertificate = true;
                    if (sourceTraining.ValidUntil.HasValue)
                        model.ValidUntil = sourceTraining.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    ViewBag.RenewalSourceTitle = sourceTraining.Judul ?? "";
                    ViewBag.RenewalSourceUserName = sourceTraining.User?.FullName ?? "";
                    ViewBag.RenewsTrainingId = renewTrainingId[0];
                }
                else
                {
                    // Bulk renew — build per-user FK mapping
                    var sourceTrainings = await _context.TrainingRecords
                        .Include(t => t.User)
                        .Where(t => renewTrainingId.Contains(t.Id))
                        .ToListAsync();

                    if (sourceTrainings.Count == 0)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    var firstTraining = sourceTrainings[0];
                    isRenewalMode = true;
                    model.Title = firstTraining.Judul ?? "";
                    model.Category = MapKategori(firstTraining.Kategori, catsForRenewal);
                    model.GenerateCertificate = true;
                    if (firstTraining.ValidUntil.HasValue)
                        model.ValidUntil = firstTraining.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    // Build {UserId → TrainingRecordId} map (GroupBy to handle duplicate UserId safely)
                    var fkMap = sourceTrainings
                        .GroupBy(t => t.UserId ?? "")
                        .ToDictionary(g => g.Key, g => g.First().Id);
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(fkMap);
                    ViewBag.RenewalFkMapType = "training";

                    ViewBag.SelectedUserIds = sourceTrainings.Select(t => t.UserId).Where(id => id != null).ToList();
                    ViewBag.RenewalSourceTitle = firstTraining.Judul ?? "";
                    ViewBag.RenewalSourceUserName = string.Join(", ", sourceTrainings.Select(t => t.User?.FullName ?? ""));
                    // model.RenewsTrainingId = null intentionally — resolved per-user at POST
                }
            }

            ViewBag.IsRenewalMode = isRenewalMode;

            return View(model);
        }

        // POST: Process form submission (multi-user)
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssessment(AssessmentSession model, List<string> UserIds, string? RenewalFkMap = null, string? RenewalFkMapType = null)
        {
            // Remove single UserId from validation since we use UserIds list
            ModelState.Remove("UserId");

            // Handle Token Validation
            if (model.IsTokenRequired)
            {
                // Token is required - validate it
                if (string.IsNullOrWhiteSpace(model.AccessToken))
                {
                    ModelState.AddModelError("AccessToken", "Access Token is required when token security is enabled.");
                }
            }
            else
            {
                // Token is NOT required - remove from validation and clear value
                ModelState.Remove("AccessToken");
                model.AccessToken = "";
            }

            // Validate at least 1 user selected
            if (UserIds == null || UserIds.Count == 0)
            {
                ModelState.AddModelError("UserIds", "Please select at least one user.");
            }

            // Validate category is selected
            if (string.IsNullOrWhiteSpace(model.Category))
            {
                ModelState.AddModelError("Category", "Kategori wajib dipilih.");
            }

            // Rate limiting: max 50 users per request
            if (UserIds != null && UserIds.Count > 50)
            {
                ModelState.AddModelError("UserIds", "Cannot assign to more than 50 users at once. Please split into multiple batches.");
            }

            // Validate schedule date
            if (model.Schedule < DateTime.Today)
            {
                ModelState.AddModelError("Schedule", "Schedule date cannot be in the past.");
            }

            if (model.Schedule > DateTime.Today.AddYears(2))
            {
                ModelState.AddModelError("Schedule", "Schedule date too far in future (maximum 2 years).");
            }

            // Validate duration (skip for Assessment Proton Tahun 3 — interview only, no online exam)
            bool isProtonYear3Check = model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue;
            // We'll resolve TahunKe after ModelState check below; for now use DurationMinutes=0 sentinel
            if (!isProtonYear3Check || model.DurationMinutes != 0)
            {
                if (model.DurationMinutes <= 0)
                {
                    ModelState.AddModelError("DurationMinutes", "Duration must be greater than 0.");
                }

                if (model.DurationMinutes > 480)
                {
                    ModelState.AddModelError("DurationMinutes", "Duration cannot exceed 480 minutes (8 hours).");
                }
            }

            // Validate PassPercentage
            if (model.PassPercentage < 0 || model.PassPercentage > 100)
            {
                ModelState.AddModelError("PassPercentage", "Pass Percentage must be between 0 and 100.");
            }

            // ExamWindowCloseDate is optional — remove from ModelState to prevent accidental validation failure
            ModelState.Remove("ExamWindowCloseDate");
            // ValidUntil: opsional di normal mode, wajib di renewal mode
            bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue || !string.IsNullOrEmpty(RenewalFkMap);
            ModelState.Remove("ValidUntil");
            if (isRenewalModePost && !model.ValidUntil.HasValue)
            {
                ModelState.AddModelError("ValidUntil", "Tanggal expired sertifikat wajib diisi untuk renewal.");
            }

            // XOR validation: hanya satu renewal FK yang boleh diisi
            if (model.RenewsSessionId.HasValue && model.RenewsTrainingId.HasValue)
            {
                ModelState.AddModelError("", "Hanya satu renewal FK yang boleh diisi.");
            }
            // Double renewal prevention (per D-10): check if source cert already renewed
            if (model.RenewsSessionId.HasValue)
            {
                var srcAlreadyRenewed = await _context.AssessmentSessions.AnyAsync(a => a.RenewsSessionId == model.RenewsSessionId && a.IsPassed == true)
                    || await _context.TrainingRecords.AnyAsync(t => t.RenewsSessionId == model.RenewsSessionId);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            if (model.RenewsTrainingId.HasValue)
            {
                var srcAlreadyRenewed = await _context.AssessmentSessions.AnyAsync(a => a.RenewsTrainingId == model.RenewsTrainingId && a.IsPassed == true)
                    || await _context.TrainingRecords.AnyAsync(t => t.RenewsTrainingId == model.RenewsTrainingId);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            // Mixed-type bulk validation (per D-11, EDGE-01)
            if (!string.IsNullOrEmpty(RenewalFkMap) && UserIds != null && UserIds.Count > 1)
            {
                if (string.IsNullOrEmpty(RenewalFkMapType) || (RenewalFkMapType != "session" && RenewalFkMapType != "training"))
                {
                    ModelState.AddModelError("", "Bulk renewal tidak dapat mencampur tipe Assessment dan Training. Renew per tipe secara terpisah.");
                }
            }
            // NomorSertifikat is server-generated — remove from ModelState to prevent validation failure
            ModelState.Remove("NomorSertifikat");

            // Validate model
            if (!ModelState.IsValid)
            {
                // Reload users for validation error (must match GET structure)
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                    .ToListAsync();

                ViewBag.Users = users;
                ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                ViewBag.Sections = await _context.GetAllSectionsAsync();
                ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
                ViewBag.Categories = await _context.AssessmentCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
                {
                    ViewBag.IsRenewalMode = true;
                    ViewBag.RenewsSessionId = model.RenewsSessionId;
                    ViewBag.RenewsTrainingId = model.RenewsTrainingId;
                    ViewBag.RenewalSourceTitle = model.Title ?? "";
                    ViewBag.RenewalSourceUserName = "";
                }
                return View(model);
            }

            // Check for duplicates (warning, not error)
            if (UserIds != null && UserIds.Any())
            {
                var existingAssessments = await _context.AssessmentSessions
                    .Where(a => UserIds.Contains(a.UserId)
                             && a.Title == model.Title
                             && a.Category == model.Category
                             && a.Schedule.Date == model.Schedule.Date)
                    .Include(a => a.User)
                    .Select(a => a.User.FullName)
                    .ToListAsync();

                if (existingAssessments.Any())
                {
                    TempData["Warning"] = $"Similar assessments already exist for: {string.Join(", ", existingAssessments.Take(5))}. Proceeding will create duplicates.";
                }
            }

            // Ensure Token is uppercase
            if (model.IsTokenRequired && !string.IsNullOrEmpty(model.AccessToken))
            {
                model.AccessToken = model.AccessToken.ToUpper();
            }
            else
            {
                model.AccessToken = "";
            }

            // Get current user for audit trail
            var currentUser = await _userManager.GetUserAsync(User);

            // Set default values
            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "Open";
            }

            // Create one AssessmentSession per selected user
            var createdSessions = new List<object>();

            try
            {
                // Prefetch all users at once (fix N+1 query)
                var userDictionary = await _context.Users
                    .Where(u => UserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Validate all UserIds exist
                var missingUsers = UserIds.Except(userDictionary.Keys).ToList();
                if (missingUsers.Any())
                {
                    TempData["Error"] = $"Invalid user IDs: {string.Join(", ", missingUsers)}";
                    // Reload form
                    var users = await _context.Users
                        .Where(u => u.IsActive)
                        .OrderBy(u => u.FullName)
                        .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                        .ToListAsync();
                    ViewBag.Users = users;
                    ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                    ViewBag.Sections = await _context.GetAllSectionsAsync();
                    ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
                    ViewBag.Categories = await _context.AssessmentCategories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.SortOrder)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
                    {
                        ViewBag.IsRenewalMode = true;
                        ViewBag.RenewsSessionId = model.RenewsSessionId;
                        ViewBag.RenewsTrainingId = model.RenewsTrainingId;
                        ViewBag.RenewalSourceTitle = model.Title ?? "";
                        ViewBag.RenewalSourceUserName = "";
                    }
                    return View(model);
                }

                // Proton exam metadata — look up TahunKe from ProtonTrack
                string? protonTahunKe = null;
                if (model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue)
                {
                    var protonTrack = await _context.ProtonTracks.FindAsync(model.ProtonTrackId.Value);
                    if (protonTrack == null)
                    {
                        TempData["Error"] = "Proton Track tidak ditemukan. Silakan pilih track yang valid.";
                        var users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
                        ViewBag.Users = users;
                        ViewBag.Categories = await _context.AssessmentCategories
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.SortOrder)
                            .ThenBy(c => c.Name)
                            .ToListAsync();
                        if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
                        {
                            ViewBag.IsRenewalMode = true;
                            ViewBag.RenewsSessionId = model.RenewsSessionId;
                            ViewBag.RenewsTrainingId = model.RenewsTrainingId;
                            ViewBag.RenewalSourceTitle = model.Title ?? "";
                            ViewBag.RenewalSourceUserName = "";
                        }
                        return View("CreateAssessment", model);
                    }
                    protonTahunKe = protonTrack.TahunKe;
                }

                // Phase 227 CLEN-04: NomorSertifikat is now generated in SubmitExam (when IsPassed=true).
                // Pre-computation block removed — sessions start with NomorSertifikat = null.

                // Phase 210: Deserialize per-user FK map for bulk renew
                Dictionary<string, int>? fkMap = null;
                bool isSessionMap = RenewalFkMapType == "session";
                if (!string.IsNullOrEmpty(RenewalFkMap))
                {
                    try { fkMap = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(RenewalFkMap); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize RenewalFkMap");
                    }
                }

                // Create all sessions in memory first
                var sessions = new List<AssessmentSession>();

                for (int i = 0; i < UserIds.Count; i++)
                {
                    var userId = UserIds[i];
                    var session = new AssessmentSession
                    {
                        Title = model.Title,
                        Category = model.Category,
                        Schedule = model.Schedule,
                        DurationMinutes = model.DurationMinutes,
                        Status = model.Status,
                        BannerColor = model.BannerColor,
                        IsTokenRequired = model.IsTokenRequired,
                        AccessToken = model.AccessToken,
                        PassPercentage = model.PassPercentage,
                        AllowAnswerReview = model.AllowAnswerReview,
                        GenerateCertificate = model.GenerateCertificate,
                        ExamWindowCloseDate = model.ExamWindowCloseDate,
                        ValidUntil = model.ValidUntil,
                        NomorSertifikat = null, // Phase 227 CLEN-04: generated in SubmitExam when IsPassed=true
                        Progress = 0,
                        UserId = userId,
                        CreatedBy = currentUser?.Id,
                        RenewsSessionId = fkMap != null && isSessionMap && fkMap.TryGetValue(userId, out int sessionFk) ? sessionFk : model.RenewsSessionId,
                        RenewsTrainingId = fkMap != null && !isSessionMap && fkMap.TryGetValue(userId, out int trainingFk) ? trainingFk : model.RenewsTrainingId
                    };

                    // Set Proton-specific fields (nullable — null for non-Proton sessions)
                    if (model.Category == "Assessment Proton")
                    {
                        session.ProtonTrackId = model.ProtonTrackId;
                        session.TahunKe = protonTahunKe;
                        // Tahun 3 = interview only; no DurationMinutes required
                        if (protonTahunKe == "Tahun 3")
                            session.DurationMinutes = 0;
                    }

                    sessions.Add(session);
                }

                // Add all sessions
                _context.AssessmentSessions.AddRange(sessions);

                // Single SaveChanges with transaction (atomicity); retry up to 3 times on UNIQUE violation
                var transaction = await _context.Database.BeginTransactionAsync();
                int attempt = 0;
                const int maxAttempts = 3;
                bool saved = false;
                try
                {
                while (!saved && attempt < maxAttempts)
                {
                    attempt++;
                    try
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (attempt < maxAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
                    {
                        // Phase 227 CLEN-04: NomorSertifikat no longer generated here.
                        // This catch is kept for any other unique constraint violations.
                        foreach (var s in sessions) _context.Entry(s).State = EntityState.Detached;
                        await transaction.RollbackAsync();
                        transaction = await _context.Database.BeginTransactionAsync();

                        // Re-add sessions for retry (no cert re-assignment needed)
                        for (int j = 0; j < sessions.Count; j++)
                        {
                            sessions[j].Id = 0; // reset for re-insert
                        }
                        _context.AssessmentSessions.AddRange(sessions);
                    }
                }

                    // ASMT-01: Notify each assigned worker
                    foreach (var session in sessions)
                    {
                        try
                        {
                            await _notificationService.SendAsync(
                                session.UserId,
                                "ASMT_ASSIGNED",
                                "Assessment Baru",
                                $"Anda telah di-assign assessment \"{session.Title}\"",
                                $"/CMP/StartExam/{session.Id}"
                            );
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
                    }

                    // Audit log
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        actorName,
                        "CreateAssessment",
                        $"Created assessment '{model.Title}' ({model.Category}) scheduled {model.Schedule:yyyy-MM-dd} for {sessions.Count} user(s)",
                        sessions.FirstOrDefault()?.Id,
                        "AssessmentSession");

                    // Populate createdSessions with IDs after save
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        var session = sessions[i];
                        var assignedUser = userDictionary[session.UserId];
                        createdSessions.Add(new
                        {
                            Id = session.Id,
                            UserId = session.UserId,
                            UserName = assignedUser.FullName ?? session.UserId,
                            UserEmail = assignedUser.Email ?? ""
                        });
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log error
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error creating assessment sessions");

                // Audit log for failed creation attempt
                try
                {
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        actorName,
                        "CreateAssessment_Failed",
                        $"Failed to create assessment '{model.Title}' ({model.Category}): {ex.Message}",
                        null,
                        "AssessmentSession");
                }
                catch { /* don't let audit logging failure mask the original error */ }

                // Show error to user
                TempData["Error"] = "Gagal membuat assessment. Silakan coba lagi.";

                // Reload form
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                    .ToListAsync();
                ViewBag.Users = users;
                ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                ViewBag.Sections = await _context.GetAllSectionsAsync();
                ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
                return View(model);
            }

            // Serialize batch data for success popup
            TempData["CreatedAssessment"] = System.Text.Json.JsonSerializer.Serialize(new
            {
                Count = createdSessions.Count,
                Title = model.Title,
                Category = model.Category,
                Schedule = model.Schedule.ToString("dd MMMM yyyy"),
                DurationMinutes = model.DurationMinutes,
                Status = model.Status,
                IsTokenRequired = model.IsTokenRequired,
                AccessToken = model.AccessToken,
                Sessions = createdSessions
            });

            return RedirectToAction("CreateAssessment");
        }

        // --- EDIT ASSESSMENT ---
        // GET: Show edit form
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("ManageAssessment");
            }

            // Query sibling sessions: same Title + Category + Schedule.Date (includes the current session)
            var siblings = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == assessment.Title
                         && a.Category == assessment.Category
                         && a.Schedule.Date == assessment.Schedule.Date)
                .ToListAsync();

            var siblingUserIds = siblings
                .Where(a => a.User != null)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            // Build assigned users list for display (read-only)
            ViewBag.AssignedUsers = siblings
                .Where(a => a.User != null)
                .Select(a => new
                {
                    Id = a.Id,
                    FullName = a.User!.FullName ?? "",
                    Email = a.User!.Email ?? "",
                    Section = a.User!.Section ?? ""
                })
                .ToList();

            // Store assigned user IDs so the picker can exclude them
            ViewBag.AssignedUserIds = siblingUserIds;

            // Get list of all users for the picker
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Sections = await _context.GetAllSectionsAsync();

            // Count packages attached to this assessment's sibling group (for schedule-change warning)
            var siblingIds = siblings.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            ViewBag.PackageCount = packageCount;
            ViewBag.OriginalSchedule = assessment.Schedule.ToString("yyyy-MM-dd");
            ViewBag.Categories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var editParentCats = await _context.AssessmentCategories
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.ParentCategories = editParentCats;

            return View(assessment);
        }

        // POST: Update assessment
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, AssessmentSession model, List<string> NewUserIds)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("ManageAssessment");
            }

            // Prevent editing completed assessments (optional - you can remove this if needed)
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "Cannot edit completed assessments.";
                return RedirectToAction("ManageAssessment");
            }

            // Rate limit: guard before any DB work
            if (NewUserIds != null && NewUserIds.Count > 50)
            {
                TempData["Error"] = "Cannot assign more than 50 users at once. Please split into multiple batches.";
                return RedirectToAction("ManageAssessment");
            }

            // Validate editable fields (mirrors CreateAssessment POST validation)
            var editErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Title))
                editErrors.Add("Title is required.");

            if (model.Schedule > DateTime.Today.AddYears(2))
                editErrors.Add("Schedule date too far in future (maximum 2 years).");

            bool editIsProtonYear3 = model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue && model.DurationMinutes == 0;
            if (!editIsProtonYear3)
            {
                if (model.DurationMinutes <= 0)
                    editErrors.Add("Duration must be greater than 0.");
                if (model.DurationMinutes > 480)
                    editErrors.Add("Duration cannot exceed 480 minutes (8 hours).");
            }

            if (model.PassPercentage < 0 || model.PassPercentage > 100)
                editErrors.Add("Pass Percentage must be between 0 and 100.");

            if (model.IsTokenRequired && string.IsNullOrWhiteSpace(model.AccessToken))
                editErrors.Add("Access Token is required when token security is enabled.");

            if (editErrors.Any())
            {
                TempData["Error"] = string.Join(" ", editErrors);
                return RedirectToAction("EditAssessment", new { id });
            }

            // Capture original group key before updating (needed to find siblings)
            var origTitle = assessment.Title;
            var origCategory = assessment.Category;
            var origScheduleDate = assessment.Schedule.Date;

            // Resolve new token value
            string newToken;
            if (model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken))
                newToken = model.AccessToken.ToUpper();
            else if (!model.IsTokenRequired)
                newToken = "";
            else
                newToken = assessment.AccessToken ?? "";

            // Fetch all sibling sessions (same group key) to propagate shared field changes
            var siblings = await _context.AssessmentSessions
                .Where(a => a.Title == origTitle
                         && a.Category == origCategory
                         && a.Schedule.Date == origScheduleDate)
                .ToListAsync();

            // Update shared fields on ALL siblings (including the current session)
            var now = DateTime.UtcNow;
            foreach (var sibling in siblings)
            {
                sibling.Title = model.Title;
                sibling.Category = model.Category;
                sibling.Schedule = model.Schedule;
                sibling.DurationMinutes = model.DurationMinutes;
                sibling.Status = model.Status;
                sibling.BannerColor = model.BannerColor;
                sibling.IsTokenRequired = model.IsTokenRequired;
                sibling.AccessToken = newToken;
                sibling.PassPercentage = model.PassPercentage;
                sibling.AllowAnswerReview = model.AllowAnswerReview;
                sibling.GenerateCertificate = model.GenerateCertificate;
                sibling.ExamWindowCloseDate = model.ExamWindowCloseDate;
                sibling.UpdatedAt = now;
            }

            // InProgress warning: notify if any sibling session is currently in progress
            var hasInProgress = await _context.AssessmentSessions
                .AnyAsync(s => s.Title == origTitle && s.Category == origCategory
                    && s.Schedule.Date == origScheduleDate
                    && s.StartedAt != null && s.CompletedAt == null);
            if (hasInProgress)
            {
                TempData["Warning"] = "Perhatian: Ada peserta yang sedang mengerjakan ujian. Perubahan Title/Category/Schedule tidak akan berlaku untuk sesi yang sedang berjalan.";
            }

            // Fetch actor info before try block so it is available for both edit and bulk-assign audit calls
            var editUser = await _userManager.GetUserAsync(User);
            var editActorName = string.IsNullOrWhiteSpace(editUser?.NIP) ? (editUser?.FullName ?? "Unknown") : $"{editUser.NIP} - {editUser.FullName}";

            try
            {
                await _context.SaveChangesAsync();

                // Audit log — edit
                await _auditLog.LogAsync(
                    editUser?.Id ?? "",
                    editActorName,
                    "EditAssessment",
                    $"Edited assessment '{assessment.Title}' ({assessment.Category}) [ID={id}]",
                    id,
                    "AssessmentSession");

                TempData["Success"] = $"Assessment '{assessment.Title}' has been updated successfully.";
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error updating assessment");
                TempData["Error"] = "Gagal memperbarui assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }

            // ===== BULK ASSIGN: create new sessions for selected users =====
            if (NewUserIds != null && NewUserIds.Count > 0)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                try
                {
                    // Re-load the saved assessment to get current field values
                    var savedAssessment = await _context.AssessmentSessions.FindAsync(id);
                    if (savedAssessment != null)
                    {
                        // Query already-assigned sibling user IDs (Title+Category+Schedule.Date match)
                        var existingSiblingUserIds = await _context.AssessmentSessions
                            .Where(a => a.Title == savedAssessment.Title
                                     && a.Category == savedAssessment.Category
                                     && a.Schedule.Date == savedAssessment.Schedule.Date)
                            .Select(a => a.UserId)
                            .Distinct()
                            .ToListAsync();

                        // Filter out already-assigned users to prevent duplicates
                        var filteredNewUserIds = NewUserIds
                            .Where(uid => !existingSiblingUserIds.Contains(uid))
                            .Distinct()
                            .ToList();

                        if (filteredNewUserIds.Count > 0)
                        {
                            // Validate all provided user IDs exist
                            var userDictionary = await _context.Users
                                .Where(u => filteredNewUserIds.Contains(u.Id))
                                .ToDictionaryAsync(u => u.Id);

                            var missingUsers = filteredNewUserIds.Except(userDictionary.Keys).ToList();
                            if (missingUsers.Any())
                            {
                                logger.LogWarning("Bulk assign: invalid user IDs: {Ids}", string.Join(", ", missingUsers));
                                TempData["Error"] = $"Invalid user IDs detected: {string.Join(", ", missingUsers)}";
                                return RedirectToAction("ManageAssessment");
                            }

                            // Build new sessions (editUser already fetched at outer scope)
                            var newSessions = filteredNewUserIds.Select(uid => new AssessmentSession
                            {
                                Title = savedAssessment.Title,
                                Category = savedAssessment.Category,
                                Schedule = savedAssessment.Schedule,
                                DurationMinutes = savedAssessment.DurationMinutes,
                                Status = savedAssessment.Status,
                                BannerColor = savedAssessment.BannerColor,
                                IsTokenRequired = savedAssessment.IsTokenRequired,
                                AccessToken = savedAssessment.AccessToken,
                                PassPercentage = savedAssessment.PassPercentage,
                                AllowAnswerReview = savedAssessment.AllowAnswerReview,
                                GenerateCertificate = savedAssessment.GenerateCertificate,
                                ExamWindowCloseDate = savedAssessment.ExamWindowCloseDate,
                                Progress = 0,
                                UserId = uid,
                                CreatedBy = editUser?.Id
                            }).ToList();

                            _context.AssessmentSessions.AddRange(newSessions);

                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();

                                // ASMT-01: Notify each newly assigned worker
                                foreach (var ns in newSessions)
                                {
                                    try
                                    {
                                        await _notificationService.SendAsync(
                                            ns.UserId,
                                            "ASMT_ASSIGNED",
                                            "Assessment Baru",
                                            $"Anda telah di-assign assessment \"{ns.Title}\"",
                                            $"/CMP/StartExam/{ns.Id}"
                                        );
                                    }
                                    catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
                                }

                                // Audit log — bulk assign
                                await _auditLog.LogAsync(
                                    editUser?.Id ?? "",
                                    editActorName,
                                    "BulkAssign",
                                    $"Assigned {newSessions.Count} new user(s) to assessment '{savedAssessment.Title}' ({savedAssessment.Category})",
                                    id,
                                    "AssessmentSession");

                                TempData["Success"] = $"Assessment '{savedAssessment.Title}' has been updated. {newSessions.Count} new user(s) assigned.";
                            }
                            catch
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                        // If filteredNewUserIds is empty (all were already assigned), no error needed — existing success message stands
                    }
                }
                catch (Exception ex)
                {
                    var logger2 = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                    logger2.LogError(ex, "Error bulk-assigning users to assessment {Id}", id);
                    TempData["Error"] = "Assessment berhasil diperbarui, tetapi gagal menambahkan user. Silakan coba lagi.";
                }
            }

            return RedirectToAction("ManageAssessment");
        }

        // --- DELETE ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessment(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            try
            {
                var assessment = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assessment == null)
                {
                    logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
                    TempData["Error"] = "Assessment not found.";
                    return RedirectToAction("ManageAssessment");
                }

                var assessmentTitle = assessment.Title;
                logger.LogInformation($"Attempting to delete assessment {id}: {assessmentTitle}");

                // Delete PackageUserResponses (Restrict FK — must be removed before session)
                var pkgResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                if (pkgResponses.Any())
                {
                    logger.LogInformation($"Deleting {pkgResponses.Count} package user responses");
                    _context.PackageUserResponses.RemoveRange(pkgResponses);
                }

                // Delete AssessmentAttemptHistory rows (no FK — orphaned if not removed)
                var attemptHistory = await _context.AssessmentAttemptHistory
                    .Where(h => h.SessionId == id)
                    .ToListAsync();
                if (attemptHistory.Any())
                {
                    logger.LogInformation($"Deleting {attemptHistory.Count} attempt history records");
                    _context.AssessmentAttemptHistory.RemoveRange(attemptHistory);
                }

                // Explicit cleanup: AssessmentPackages + nested Questions + Options
                // (DB may cascade, but explicit removal prevents ordering issues)
                var packages = await _context.AssessmentPackages
                    .Include(p => p.Questions).ThenInclude(q => q.Options)
                    .Where(p => p.AssessmentSessionId == id)
                    .ToListAsync();
                if (packages.Any())
                {
                    foreach (var pkg in packages)
                    {
                        foreach (var q in pkg.Questions)
                            _context.PackageOptions.RemoveRange(q.Options);
                        _context.PackageQuestions.RemoveRange(pkg.Questions);
                    }
                    _context.AssessmentPackages.RemoveRange(packages);
                    logger.LogInformation($"Deleting {packages.Count} packages with their questions/options");
                }

                // Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)

                // Finally delete the assessment itself
                _context.AssessmentSessions.Remove(assessment);

                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var deleteUser = await _userManager.GetUserAsync(User);
                    var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP) ? (deleteUser?.FullName ?? "Unknown") : $"{deleteUser.NIP} - {deleteUser.FullName}";
                    await _auditLog.LogAsync(
                        deleteUser?.Id ?? "",
                        deleteActorName,
                        "DeleteAssessment",
                        $"Deleted assessment '{assessmentTitle}' [ID={id}]",
                        id,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id);
                }

                logger.LogInformation($"Successfully deleted assessment {id}: {assessmentTitle}");
                TempData["Success"] = $"Assessment '{assessmentTitle}' has been deleted successfully.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting assessment {Id}", id);
                TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }
        }

        // --- DELETE ASSESSMENT GROUP ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentGroup(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            try
            {
                // Load representative to get grouping key
                var rep = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (rep == null)
                {
                    logger.LogWarning($"DeleteAssessmentGroup: representative session {id} not found");
                    TempData["Error"] = "Assessment not found.";
                    return RedirectToAction("ManageAssessment");
                }

                var scheduleDate = rep.Schedule.Date;

                // Find all siblings (same Title + Category + Schedule.Date)
                var siblings = await _context.AssessmentSessions
                    .Where(a =>
                        a.Title == rep.Title &&
                        a.Category == rep.Category &&
                        a.Schedule.Date == scheduleDate)
                    .ToListAsync();

                logger.LogInformation($"DeleteAssessmentGroup: deleting {siblings.Count} sessions for '{rep.Title}'");

                var siblingIds = siblings.Select(s => s.Id).ToList();

                // Delete PackageUserResponses for all siblings (Restrict FK — must be removed before sessions)
                var allPkgResponses = await _context.PackageUserResponses
                    .Where(r => siblingIds.Contains(r.AssessmentSessionId))
                    .ToListAsync();
                if (allPkgResponses.Any())
                    _context.PackageUserResponses.RemoveRange(allPkgResponses);

                // Delete AssessmentAttemptHistory for all siblings (no FK — orphaned if not removed)
                var allAttemptHistory = await _context.AssessmentAttemptHistory
                    .Where(h => siblingIds.Contains(h.SessionId))
                    .ToListAsync();
                if (allAttemptHistory.Any())
                    _context.AssessmentAttemptHistory.RemoveRange(allAttemptHistory);

                // Explicit cleanup: AssessmentPackages + nested Questions + Options for all siblings
                var allPackages = await _context.AssessmentPackages
                    .Include(p => p.Questions).ThenInclude(q => q.Options)
                    .Where(p => siblingIds.Contains(p.AssessmentSessionId))
                    .ToListAsync();
                if (allPackages.Any())
                {
                    foreach (var pkg in allPackages)
                    {
                        foreach (var q in pkg.Questions)
                            _context.PackageOptions.RemoveRange(q.Options);
                        _context.PackageQuestions.RemoveRange(pkg.Questions);
                    }
                    _context.AssessmentPackages.RemoveRange(allPackages);
                    logger.LogInformation($"DeleteAssessmentGroup: deleting {allPackages.Count} packages with their questions/options");
                }

                // Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)

                foreach (var session in siblings)
                {
                    _context.AssessmentSessions.Remove(session);
                }

                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var dgUser = await _userManager.GetUserAsync(User);
                    var dgActorName = string.IsNullOrWhiteSpace(dgUser?.NIP) ? (dgUser?.FullName ?? "Unknown") : $"{dgUser.NIP} - {dgUser.FullName}";
                    await _auditLog.LogAsync(
                        dgUser?.Id ?? "",
                        dgActorName,
                        "DeleteAssessmentGroup",
                        $"Deleted assessment group '{rep.Title}' ({rep.Category}) — {siblings.Count} session(s)",
                        id,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessmentGroup {Id}", id);
                }

                logger.LogInformation($"DeleteAssessmentGroup: successfully deleted group '{rep.Title}'");
                TempData["Success"] = $"Assessment '{rep.Title}' and all {siblings.Count} assignment(s) deleted.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeleteAssessmentGroup error for representative {Id}", id);
                TempData["Error"] = "Gagal menghapus grup assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }
        }

        // --- REGENERATE TOKEN ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateToken(int id)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                return Json(new { success = false, message = "Assessment not found." });
            }

            if (!assessment.IsTokenRequired)
            {
                return Json(new { success = false, message = "This assessment does not require a token." });
            }

            try
            {
                var newToken = GenerateSecureToken();
                // Update ALL sibling sessions in the same group (same Title + Category + Schedule.Date)
                var siblings = await _context.AssessmentSessions
                    .Where(a => a.Title == assessment.Title
                             && a.Category == assessment.Category
                             && a.Schedule.Date == assessment.Schedule.Date)
                    .ToListAsync();
                foreach (var sibling in siblings)
                {
                    sibling.AccessToken = newToken;
                    sibling.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                // Audit log
                var regenUser = await _userManager.GetUserAsync(User);
                var regenActorName = string.IsNullOrWhiteSpace(regenUser?.NIP) ? (regenUser?.FullName ?? "Unknown") : $"{regenUser.NIP} - {regenUser.FullName}";
                await _auditLog.LogAsync(
                    regenUser?.Id ?? "",
                    regenActorName,
                    "RegenerateToken",
                    $"Regenerated access token for '{assessment.Title}' ({assessment.Category}, {assessment.Schedule:yyyy-MM-dd}) — {siblings.Count} sibling(s) updated",
                    id,
                    "AssessmentSession");

                return Json(new { success = true, token = newToken, message = "Token regenerated successfully." });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error regenerating token");
                return Json(new { success = false, message = $"Failed to regenerate token: {ex.Message}" });
            }
        }

        // --- PRIVATE HELPERS ---
        private string GenerateSecureToken(int length = 6)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous characters (0, O, 1, I, L)
            var random = new byte[length];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random[i] % chars.Length];
            }
            return new string(result);
        }

        // --- ASSESSMENT MONITORING GROUP LIST ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AssessmentMonitoring(
            string? search,
            string? status,
            string? category)
        {
            // 7-day window — same as ManageAssessment
            // 90-review: 7-day window is intentional for monitoring view; Abandoned sessions with no ExamWindowCloseDate
            // fall back to Schedule for the window check and naturally age out after 7 days (expected behavior).
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var query = _context.AssessmentSessions
                .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
                .AsQueryable();

            // Text search by title
            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(lower));
            }

            // Category filter
            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            var allSessions = await query
                .OrderByDescending(a => a.Schedule)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    a.ExamWindowCloseDate,
                    a.Status,
                    a.IsTokenRequired,
                    a.AccessToken,
                    a.CreatedAt,
                    IsCompleted = a.CompletedAt != null,
                    IsPassed = a.IsPassed ?? false,
                    IsStarted = a.StartedAt != null
                })
                .ToListAsync();

            // Group by (Title, Category, Schedule.Date)
            var grouped = allSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g =>
                {
                    var rep = g.OrderBy(a => a.CreatedAt).First();
                    // Compute GroupStatus from session statuses
                    string groupStatus;
                    if (g.Any(a => a.Status == "Open" || a.Status == "InProgress"))
                        groupStatus = "Open";
                    else if (g.Any(a => a.Status == "Upcoming"))
                        groupStatus = "Upcoming";
                    else
                        groupStatus = "Closed";

                    return new MonitoringGroupViewModel
                    {
                        RepresentativeId = rep.Id,
                        Title = rep.Title,
                        Category = rep.Category,
                        Schedule = rep.Schedule,
                        GroupStatus = groupStatus,
                        TotalCount = g.Count(),
                        CompletedCount = g.Count(a => a.IsCompleted),
                        PassedCount = g.Count(a => a.IsPassed),
                        PendingCount = g.Count(a => !a.IsCompleted && !a.IsStarted),
                        IsTokenRequired = rep.IsTokenRequired,
                        AccessToken = rep.AccessToken ?? ""
                    };
                })
                .OrderByDescending(g => g.Schedule)
                .ToList();

            // Status filter — applied AFTER grouping (GroupStatus computed from sessions)
            // Default: show Open + Upcoming only (exclude Closed) unless status param is provided
            if (string.IsNullOrEmpty(status))
            {
                grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
                status = "active"; // signal to view that default active filter is on
            }
            else if (status == "Open" || status == "Upcoming" || status == "Closed")
            {
                grouped = grouped.Where(g => g.GroupStatus == status).ToList();
            }
            // status == "All" → no filter applied

            ViewBag.SearchTerm = search ?? "";
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = category ?? "";

            return View(grouped);
        }

        // --- ASSESSMENT MONITORING DETAIL ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AssessmentMonitoringDetail(string title, string category, DateTime scheduleDate)
        {
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
            {
                TempData["Error"] = "Assessment group not found.";
                return RedirectToAction("ManageAssessment");
            }

            // Detect package mode: check if any sibling session has packages attached
            var siblingIds = sessions.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Build question count map per session
            Dictionary<int, int> questionCountMap = new();
            if (isPackageMode)
            {
                // Package mode: count PackageQuestion rows via UserPackageAssignment -> AssessmentPackage
                // GroupBy handles users with multiple package assignments per session
                questionCountMap = (await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .Join(_context.AssessmentPackages.Include(p => p.Questions),
                        a => a.AssessmentPackageId,
                        p => p.Id,
                        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
                    .ToListAsync())
                    .GroupBy(x => x.AssessmentSessionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.QuestionCount));
            }
            var sessionViewModels = sessions.Select(a =>
            {
                string userStatus;
                if (a.CompletedAt != null)
                    userStatus = "Completed";
                else if (a.Status == "Cancelled")
                    userStatus = "Dibatalkan";
                else if (a.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (a.StartedAt != null)
                    userStatus = "InProgress";
                else
                    userStatus = "Not started";

                return new MonitoringSessionViewModel
                {
                    Id           = a.Id,
                    UserFullName = a.User?.FullName ?? "Unknown",
                    UserNIP      = a.User?.NIP ?? "",
                    UserStatus   = userStatus,
                    Score        = a.Score,
                    IsPassed     = a.IsPassed,
                    CompletedAt  = a.CompletedAt,
                    StartedAt    = a.StartedAt,
                    QuestionCount = questionCountMap.TryGetValue(a.Id, out var qc) ? qc : 0,
                    DurationMinutes = a.DurationMinutes
                };
            })
            .OrderBy(s => s.UserStatus)   // Not started before Completed
            .ThenBy(s => s.UserFullName)
            .ToList();

            var firstSession = sessions.First();
            var model = new MonitoringGroupViewModel
            {
                RepresentativeId = firstSession.Id,
                Title    = title,
                Category = category,
                Schedule = firstSession.Schedule,
                Sessions = sessionViewModels,
                TotalCount     = sessionViewModels.Count,
                CompletedCount = sessionViewModels.Count(s => s.UserStatus == "Completed"),
                PassedCount    = sessionViewModels.Count(s => s.IsPassed == true),
                GroupStatus    = sessions.Any(a => a.Status == "Open" || a.Status == "InProgress") ? "Open"
                               : sessions.Any(a => a.Status == "Upcoming") ? "Upcoming" : "Closed",
                IsPackageMode  = isPackageMode,
                PendingCount   = sessionViewModels.Count(s => s.UserStatus == "Not started"),
                CancelledCount = sessionViewModels.Count(s => s.UserStatus == "Dibatalkan"),
                InProgressCount = sessionViewModels.Count(s => s.UserStatus == "InProgress")
            };

            model.IsTokenRequired = firstSession.IsTokenRequired;
            model.AccessToken = firstSession.AccessToken ?? "";

            ViewBag.BackUrl = Url.Action("AssessmentMonitoring", "Admin");

            // Proton Tahun 3 interview form support
            // 90-review: For non-Proton categories, ViewBag.GroupTahunKe is not set. Views that access it via
            // (ViewBag.GroupTahunKe as string) ?? "" handle the null safely — no change needed.
            if (model.Category == "Assessment Proton")
            {
                var repSession = await _context.AssessmentSessions.FindAsync(model.RepresentativeId);
                ViewBag.GroupTahunKe = repSession?.TahunKe ?? "";

                if (repSession?.TahunKe == "Tahun 3")
                {
                    var siblingIds2 = model.Sessions.Select(s => s.Id).ToList();
                    ViewBag.SessionObjects = await _context.AssessmentSessions
                        .Where(s => siblingIds2.Contains(s.Id))
                        .ToListAsync();
                }
            }

            ViewBag.AssessmentBatchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";

            return View(model);
        }

        // --- SUBMIT INTERVIEW RESULTS (Assessment Proton Tahun 3) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> SubmitInterviewResults(
            int sessionId,
            string? judges,
            string? notes,
            bool isPassed,
            IFormFile? supportingDoc,
            string? returnTitle,
            string? returnCategory,
            string? returnDate)
        {
            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null)
            {
                TempData["Error"] = "Session tidak ditemukan.";
                return RedirectToAction("ManageAssessment");
            }
            if (session.Category != "Assessment Proton" || session.TahunKe != "Tahun 3")
            {
                TempData["Error"] = "Aksi ini hanya untuk Assessment Proton Tahun 3.";
                return RedirectToAction("ManageAssessment");
            }

            // Collect aspect scores from form fields (name=aspect_{AspectName_Underscored})
            var aspects = new List<string>
            {
                "Pengetahuan Teknis", "Kemampuan Operasional", "Keselamatan Kerja",
                "Komunikasi & Kerjasama", "Sikap Profesional"
            };
            var aspectScores = new Dictionary<string, int>();
            foreach (var aspect in aspects)
            {
                var formKey = "aspect_" + aspect.Replace(" ", "_").Replace("&", "and").Replace(",", "");
                if (int.TryParse(Request.Form[formKey], out int score))
                    aspectScores[aspect] = Math.Clamp(score, 1, 5);
                else
                    aspectScores[aspect] = 3;
            }

            // File upload (optional, max 10MB)
            string? supportingDocPath = null;
            if (supportingDoc != null && supportingDoc.Length > 0 && supportingDoc.Length <= 10 * 1024 * 1024)
            {
                var ext = Path.GetExtension(supportingDoc.FileName).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                if (allowed.Contains(ext))
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "interviews");
                    Directory.CreateDirectory(dir);
                    var safeName = $"{sessionId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
                    using var stream = new FileStream(Path.Combine(dir, safeName), FileMode.Create);
                    await supportingDoc.CopyToAsync(stream);
                    supportingDocPath = $"/uploads/interviews/{safeName}";
                }
            }
            // Preserve existing doc path when no new upload
            if (supportingDocPath == null && !string.IsNullOrEmpty(session.InterviewResultsJson))
            {
                try
                {
                    var old = System.Text.Json.JsonSerializer.Deserialize<InterviewResultsDto>(session.InterviewResultsJson);
                    supportingDocPath = old?.SupportingDocPath;
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse scheduled date field in AssessmentMonitoring"); }
            }

            var dto = new InterviewResultsDto
            {
                Judges = judges?.Trim() ?? "",
                AspectScores = aspectScores,
                Notes = notes?.Trim() ?? "",
                SupportingDocPath = supportingDocPath,
                IsPassed = isPassed
            };

            session.InterviewResultsJson = System.Text.Json.JsonSerializer.Serialize(dto);
            session.IsPassed = isPassed;
            session.Status = "Completed";
            session.CompletedAt = DateTime.UtcNow;

            // [PROTON-06 FIX] Create ProtonFinalAssessment when interview passes so that
            // HistoriProton and CoachingProton dashboard correctly reflect completion status.
            var actorForFix = await _userManager.GetUserAsync(User);
            if (isPassed && session.ProtonTrackId.HasValue)
            {
                var assignment = await _context.ProtonTrackAssignments
                    .FirstOrDefaultAsync(a => a.CoacheeId == session.UserId
                                           && a.ProtonTrackId == session.ProtonTrackId.Value
                                           && a.IsActive);
                if (assignment != null)
                {
                    // Avoid duplicate: only create if none exists for this assignment
                    var alreadyExists = await _context.ProtonFinalAssessments
                        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
                    if (!alreadyExists)
                    {
                        _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
                        {
                            CoacheeId = session.UserId,
                            CreatedById = actorForFix?.Id ?? "",
                            ProtonTrackAssignmentId = assignment.Id,
                            Status = "Completed",
                            CompetencyLevelGranted = 0, // Interview track does not grant a numeric level
                            Notes = $"Interview Tahun 3 lulus. Assessor: {dto.Judges}",
                            CreatedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Audit log
            var user = actorForFix;
            if (user != null)
            {
                var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
                await _auditLog.LogAsync(
                    user.Id,
                    actorName,
                    "SubmitInterviewResults",
                    $"Interview results saved for session ID={sessionId} ({session.Title}), IsPassed={isPassed}",
                    sessionId,
                    "AssessmentSession");
            }

            TempData["Success"] = "Hasil interview berhasil disimpan.";
            return RedirectToAction("AssessmentMonitoringDetail", new
            {
                title = returnTitle ?? session.Title,
                category = returnCategory ?? session.Category,
                scheduleDate = returnDate ?? session.Schedule.Date.ToString("yyyy-MM-dd")
            });
        }

        // --- RESET ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Reset is valid for any active status (Open, InProgress, Completed, Abandoned) — Cancelled is final and NOT resettable
            if (assessment.Status != "Open" && assessment.Status != "InProgress" && assessment.Status != "Completed" && assessment.Status != "Abandoned")
            {
                TempData["Error"] = "Status sesi tidak valid untuk direset.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            // Phase 46: Archive attempt data if session was Completed
            if (assessment.Status == "Completed")
            {
                int existingAttempts = await _context.AssessmentAttemptHistory
                    .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title)
                    .CountAsync();

                var attemptHistory = new AssessmentAttemptHistory
                {
                    SessionId    = assessment.Id,
                    UserId       = assessment.UserId,
                    Title        = assessment.Title ?? "",
                    Category     = assessment.Category ?? "",
                    Score        = assessment.Score,
                    IsPassed     = assessment.IsPassed,
                    StartedAt    = assessment.StartedAt,
                    CompletedAt  = assessment.CompletedAt,
                    AttemptNumber = existingAttempts + 1,
                    ArchivedAt   = DateTime.UtcNow
                };
                _context.AssessmentAttemptHistory.Add(attemptHistory);
            }

            // Delete PackageUserResponse records for this session (package path answers)
            var packageResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == id)
                .ToListAsync();
            if (packageResponses.Any())
                _context.PackageUserResponses.RemoveRange(packageResponses);

            // 2. Delete UserPackageAssignment for this session (package path)
            //    Deleting ensures the next StartExam assigns a fresh random package.
            var assignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
            if (assignment != null)
                _context.UserPackageAssignments.Remove(assignment);

            // 3. Reset session state to Open via status-guarded ExecuteUpdateAsync
            // (Cancelled is the only status that is NOT resettable — guard prevents double-reset race)
            await _context.SaveChangesAsync(); // flush archive + delete operations first

            var rsRowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == id && s.Status != "Cancelled")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Open")
                    .SetProperty(r => r.Score, (int?)null)
                    .SetProperty(r => r.IsPassed, (bool?)null)
                    .SetProperty(r => r.Progress, 0)
                    .SetProperty(r => r.StartedAt, (DateTime?)null)
                    .SetProperty(r => r.CompletedAt, (DateTime?)null)
                    .SetProperty(r => r.ElapsedSeconds, (int)0)
                    .SetProperty(r => r.LastActivePage, (int?)null)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
                );

            if (rsRowsAffected == 0)
            {
                TempData["Error"] = "Sesi tidak dapat direset (mungkin sudah dibatalkan).";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            // Audit log
            var rsUser = await _userManager.GetUserAsync(User);
            var rsActorName = string.IsNullOrWhiteSpace(rsUser?.NIP) ? (rsUser?.FullName ?? "Unknown") : $"{rsUser.NIP} - {rsUser.FullName}";
            await _auditLog.LogAsync(
                rsUser?.Id ?? "",
                rsActorName,
                "ResetAssessment",
                $"Reset assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
                id,
                "AssessmentSession");

            await _hubContext.Clients.User(assessment.UserId).SendAsync("sessionReset", new { reason = "hc_reset" });

            TempData["Success"] = "Sesi ujian telah direset. Peserta dapat mengikuti ujian kembali.";
            return RedirectToAction("AssessmentMonitoringDetail", new {
                title = assessment.Title,
                category = assessment.Category,
                scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
            });
        }

        // --- AKHIRI UJIAN (individual: auto-grade from saved answers) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AkhiriUjian(int id)
        {
            var session = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (session == null) return NotFound();

            // Only InProgress sessions can be ended — use same logic as monitoring view:
            // StartedAt set + not yet completed/scored, and not Cancelled/Abandoned
            var isInProgress = session.StartedAt != null
                && session.CompletedAt == null
                && session.Score == null
                && session.Status != "Cancelled"
                && session.Status != "Abandoned";
            if (!isInProgress)
            {
                TempData["Error"] = "Akhiri Ujian hanya dapat dilakukan pada sesi yang berstatus InProgress.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = session.Title,
                    category = session.Category,
                    scheduleDate = session.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            await GradeFromSavedAnswers(session);

            // Status-guarded write: detach the tracked entity and use ExecuteUpdateAsync with a WHERE guard
            // so that if SubmitExam or another AkhiriUjian already completed this session, we skip silently.
            _context.Entry(session).State = EntityState.Detached;

            var rowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == id
                         && s.StartedAt != null
                         && s.CompletedAt == null
                         && s.Score == null
                         && s.Status != "Cancelled"
                         && s.Status != "Abandoned"
                         && s.Status != "Completed")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Completed")
                    .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
                    .SetProperty(r => r.Score, session.Score)
                    .SetProperty(r => r.IsPassed, session.IsPassed)
                    .SetProperty(r => r.Progress, 100)
                );

            if (rowsAffected == 0)
            {
                // Race: another request already completed or cancelled this session — silent skip
                TempData["Info"] = "Sesi sudah selesai atau dibatalkan.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = session.Title,
                    category = session.Category,
                    scheduleDate = session.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            // Phase 226 CLEN-04: Generate NomorSertifikat when passed (same pattern as CMPController.SubmitExam)
            if (session.GenerateCertificate && session.IsPassed == true)
            {
                var certNow = DateTime.Now;
                int certYear = certNow.Year;
                int certAttempts = 0;
                const int maxCertAttempts = 3;
                bool certSaved = false;

                while (!certSaved && certAttempts < maxCertAttempts)
                {
                    certAttempts++;
                    try
                    {
                        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
                        await _context.AssessmentSessions
                            .Where(s => s.Id == id && s.NomorSertifikat == null)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow))
                            );
                        certSaved = true;
                    }
                    catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
                    {
                        // Retry with fresh sequence
                    }
                }
            }

            _cache.Remove($"exam-status-{id}");

            // Audit log
            var auUser = await _userManager.GetUserAsync(User);
            var auActorName = string.IsNullOrWhiteSpace(auUser?.NIP) ? (auUser?.FullName ?? "Unknown") : $"{auUser.NIP} - {auUser.FullName}";
            await _auditLog.LogAsync(
                auUser?.Id ?? "",
                auActorName,
                "AkhiriUjian",
                $"Ended exam '{session.Title}' for user {session.UserId} [ID={id}], auto-graded score: {session.Score}%",
                id,
                "AssessmentSession");

            await _hubContext.Clients.User(session.UserId).SendAsync("examClosed", new { reason = "hc_closed" });

            TempData["Success"] = "Ujian telah diakhiri dan dinilai dari jawaban tersimpan.";
            return RedirectToAction("AssessmentMonitoringDetail", new {
                title = session.Title,
                category = session.Category,
                scheduleDate = session.Schedule.Date.ToString("yyyy-MM-dd")
            });
        }

        // --- AKHIRI SEMUA UJIAN (bulk: auto-grade InProgress + cancel not-started) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AkhiriSemuaUjian(string title, string category, DateTime scheduleDate)
        {
            // Find all Open or InProgress sessions in this assessment group
            var sessionsToEnd = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date
                         && (a.Status == "Open" || a.Status == "InProgress"))
                .ToListAsync();

            if (!sessionsToEnd.Any())
            {
                TempData["Error"] = "Tidak ada sesi Open atau InProgress untuk diakhiri.";
                return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate = scheduleDate.Date.ToString("yyyy-MM-dd") });
            }

            int gradedCount = 0;
            int cancelledCount = 0;

            foreach (var session in sessionsToEnd)
            {
                bool isInProgress = session.StartedAt != null && session.CompletedAt == null && session.Score == null;
                if (isInProgress)
                {
                    await GradeFromSavedAnswers(session);
                    gradedCount++;
                }
                else
                {
                    // Open / not-started → Cancelled
                    session.Status = "Cancelled";
                    session.UpdatedAt = DateTime.UtcNow;
                    cancelledCount++;
                }
            }

            await _context.SaveChangesAsync();

            // Phase 226 CLEN-04: Generate NomorSertifikat for passed sessions
            foreach (var s in sessionsToEnd.Where(s => s.GenerateCertificate && s.IsPassed == true && s.NomorSertifikat == null))
            {
                var certNow = DateTime.Now;
                int certYear = certNow.Year;
                int certAttempts = 0;
                const int maxCertAttempts = 3;
                bool certSaved = false;

                while (!certSaved && certAttempts < maxCertAttempts)
                {
                    certAttempts++;
                    try
                    {
                        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
                        await _context.AssessmentSessions
                            .Where(x => x.Id == s.Id && x.NomorSertifikat == null)
                            .ExecuteUpdateAsync(x => x
                                .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow))
                            );
                        certSaved = true;
                    }
                    catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
                    {
                        // Retry with fresh sequence
                    }
                }
            }

            // Invalidate cache for all affected sessions
            foreach (var s in sessionsToEnd)
                _cache.Remove($"exam-status-{s.Id}");

            // Audit log
            var actor = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
            await _auditLog.LogAsync(
                actor?.Id ?? "",
                actorName,
                "AkhiriSemuaUjian",
                $"Ended all exams for '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) — {gradedCount} graded, {cancelledCount} cancelled",
                null,
                "AssessmentSession");

            var batchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";
            await _hubContext.Clients.Group($"batch-{batchKey}").SendAsync("examClosed", new { reason = "hc_closed" });

            TempData["Success"] = $"Berhasil mengakhiri ujian: {gradedCount} peserta dinilai dari jawaban tersimpan, {cancelledCount} peserta dibatalkan.";
            return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate = scheduleDate.Date.ToString("yyyy-MM-dd") });
        }

        // --- GET AKHIRI SEMUA COUNTS (for confirmation modal) ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetAkhiriSemuaCounts(string title, string category, DateTime scheduleDate)
        {
            var sessions = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date
                         && (a.Status == "Open" || a.Status == "InProgress"))
                .ToListAsync();

            int inProgressCount = sessions.Count(s => s.StartedAt != null && s.CompletedAt == null && s.Score == null);
            int notStartedCount = sessions.Count(s => s.StartedAt == null);

            return Json(new { inProgressCount, notStartedCount });
        }

        /// <summary>
        /// Auto-grade a single InProgress session from its saved answers.
        /// Handles both package and legacy paths. Creates TrainingRecord (with duplicate guard)
        /// and fires group completion notification. Does NOT call SaveChangesAsync — caller handles it.
        /// </summary>
        private async Task GradeFromSavedAnswers(AssessmentSession session)
        {
            // Detect package mode
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);

            int totalScore = 0;
            int maxScore = 0;

            if (packageAssignment != null)
            {
                // ---- PACKAGE PATH ----
                var shuffledIds = packageAssignment.GetShuffledQuestionIds();

                var packageQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => shuffledIds.Contains(q.Id))
                    .ToListAsync();
                var questionLookup = packageQuestions.ToDictionary(q => q.Id);

                var responses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == session.Id)
                    .ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId);

                foreach (var qId in shuffledIds)
                {
                    if (!questionLookup.TryGetValue(qId, out var q)) continue;
                    maxScore += q.ScoreValue;
                    if (responses.TryGetValue(q.Id, out var optId) && optId.HasValue)
                    {
                        var selectedOption = q.Options.FirstOrDefault(o => o.Id == optId.Value);
                        if (selectedOption != null && selectedOption.IsCorrect)
                            totalScore += q.ScoreValue;
                    }
                }

                packageAssignment.IsCompleted = true;

                // Persist ET scores per session — Phase 223
                var etGroupsAdmin = packageQuestions
                    .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis);

                foreach (var etGroup in etGroupsAdmin)
                {
                    int etCorrect = 0;
                    int etTotal = etGroup.Count();
                    foreach (var q in etGroup)
                    {
                        if (responses.TryGetValue(q.Id, out var optId) && optId.HasValue)
                        {
                            var sel = q.Options.FirstOrDefault(o => o.Id == optId.Value);
                            if (sel != null && sel.IsCorrect) etCorrect++;
                        }
                    }
                    _context.SessionElemenTeknisScores.Add(new HcPortal.Models.SessionElemenTeknisScore
                    {
                        AssessmentSessionId = session.Id,
                        ElemenTeknis = etGroup.Key,
                        CorrectCount = etCorrect,
                        QuestionCount = etTotal
                    });
                }
            }
            // Legacy path removed (Phase 227 CLEN-02) — sessions without package assignment get score 0.

            int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

            session.Score = finalPercentage;
            session.Status = "Completed";
            session.Progress = 100;
            session.IsPassed = finalPercentage >= session.PassPercentage;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            // TrainingRecord creation (duplicate guard: same as SubmitExam)
            var judul = $"Assessment: {session.Title}";
            bool trainingRecordExists = await _context.TrainingRecords.AnyAsync(t =>
                t.UserId == session.UserId &&
                t.Judul == judul &&
                t.Tanggal == session.Schedule);
            if (!trainingRecordExists)
            {
                _context.TrainingRecords.Add(new TrainingRecord
                {
                    UserId = session.UserId,
                    Judul = judul,
                    Kategori = session.Category ?? "Assessment",
                    Tanggal = session.Schedule,
                    TanggalSelesai = session.CompletedAt,
                    Penyelenggara = "Internal",
                    Status = session.IsPassed == true ? "Passed" : "Failed"
                });
            }

            // Group completion notification
            await _workerDataService.NotifyIfGroupCompleted(session);
        }

        // --- EXPORT ASSESSMENT RESULTS ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportAssessmentResults(string title, string category, DateTime scheduleDate)
        {
            // Query all sessions in this group (all workers assigned, regardless of completion status)
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
            {
                TempData["Error"] = "No sessions found for this assessment group.";
                return RedirectToAction("ManageAssessment");
            }

            // Detect package mode and load question counts (same pattern as AssessmentMonitoringDetail)
            var siblingIds = sessions.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Build question count map per session
            Dictionary<int, int> questionCountMap = new();
            if (isPackageMode)
            {
                questionCountMap = await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .Join(_context.AssessmentPackages.Include(p => p.Questions),
                        a => a.AssessmentPackageId,
                        p => p.Id,
                        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
                    .ToDictionaryAsync(
                        x => x.AssessmentSessionId,
                        x => x.QuestionCount);
            }
            // Build row data: one row per session, include all statuses
            var rows = sessions.Select(a =>
            {
                string userStatus;
                if (a.Status == "Cancelled")
                    userStatus = "Dibatalkan";
                else if (a.CompletedAt != null || a.Score != null)
                    userStatus = "Completed";
                else if (a.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (a.StartedAt != null)
                    userStatus = "In Progress";
                else
                    userStatus = "Not Started";

                string resultText = a.Status == "Cancelled" ? "\u2014"
                                  : a.IsPassed == true ? "Pass"
                                  : a.IsPassed == false ? "Fail"
                                  : "\u2014";

                return new
                {
                    UserFullName  = a.User?.FullName ?? "Unknown",
                    UserNIP       = a.User?.NIP ?? "",
                    QuestionCount = questionCountMap.TryGetValue(a.Id, out var qcnt) ? qcnt : 0,
                    UserStatus    = userStatus,
                    Score         = a.Status == "Cancelled" ? (object)"\u2014" : (a.Score.HasValue ? (object)a.Score.Value : "\u2014"),
                    Result        = resultText,
                    CompletedAt   = a.CompletedAt.HasValue
                                    ? a.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm")
                                    : ""
                };
            })
            .OrderBy(r => r.UserStatus)
            .ThenBy(r => r.UserFullName)
            .ToList();

            // Generate workbook (ClosedXML)
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Results");

            var firstSession = sessions.First();
            int totalCols = 7;

            // Assessment info header (rows 1-6)
            worksheet.Cell(1, 1).Value = "Laporan Assessment";
            worksheet.Range(1, 1, 1, totalCols).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            worksheet.Cell(2, 1).Value = "Judul";
            worksheet.Cell(2, 2).Value = title;
            worksheet.Range(2, 2, 2, totalCols).Merge();

            worksheet.Cell(3, 1).Value = "Kategori";
            worksheet.Cell(3, 2).Value = category;
            worksheet.Range(3, 2, 3, totalCols).Merge();

            worksheet.Cell(4, 1).Value = "Jadwal";
            worksheet.Cell(4, 2).Value = firstSession.Schedule.ToString("dd MMM yyyy HH:mm");
            worksheet.Range(4, 2, 4, totalCols).Merge();

            worksheet.Cell(5, 1).Value = "Durasi";
            worksheet.Cell(5, 2).Value = $"{firstSession.DurationMinutes} menit";
            worksheet.Range(5, 2, 5, totalCols).Merge();

            worksheet.Cell(6, 1).Value = "Batas Kelulusan";
            worksheet.Cell(6, 2).Value = $"{firstSession.PassPercentage}%";
            worksheet.Range(6, 2, 6, totalCols).Merge();

            // Bold the labels
            worksheet.Range(2, 1, 6, 1).Style.Font.Bold = true;

            // Row 7 is blank separator, column headers start at row 8
            int headerRow = 8;
            int col = 1;
            worksheet.Cell(headerRow, col++).Value = "Name";
            worksheet.Cell(headerRow, col++).Value = "NIP";
            worksheet.Cell(headerRow, col++).Value = "Jumlah Soal";
            worksheet.Cell(headerRow, col++).Value = "Status";
            worksheet.Cell(headerRow, col++).Value = "Score";
            worksheet.Cell(headerRow, col++).Value = "Result";
            worksheet.Cell(headerRow, col).Value   = "Completed At";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var row = i + headerRow + 1;
                int c = 1;
                worksheet.Cell(row, c++).Value = r.UserFullName;
                worksheet.Cell(row, c++).Value = r.UserNIP;
                worksheet.Cell(row, c++).Value = r.QuestionCount;
                worksheet.Cell(row, c++).Value = r.UserStatus;
                worksheet.Cell(row, c++).Value = r.Score?.ToString() ?? "\u2014";
                worksheet.Cell(row, c++).Value = r.Result;
                worksheet.Cell(row, c).Value   = r.CompletedAt;
            }

            // Sanitize title for filename: replace non-alphanumeric with _
            var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
            var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Results.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // --- USER ASSESSMENT HISTORY ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> UserAssessmentHistory(string userId)
        {
            // Load the target user
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (string.IsNullOrEmpty(userId) || targetUser == null)
            {
                TempData["Error"] = "User not found or invalid userId.";
                return RedirectToAction("ManageAssessment");
            }

            // Query completed assessments for this user
            var assessments = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .OrderByDescending(a => a.CompletedAt)
                .Select(a => new AssessmentReportItem
                {
                    Id = a.Id,
                    Title = a.Title,
                    Category = a.Category,
                    UserId = userId,
                    UserName = targetUser.FullName,
                    UserNIP = targetUser.NIP,
                    UserSection = targetUser.Section,
                    Score = a.Score ?? 0,
                    PassPercentage = a.PassPercentage,
                    IsPassed = a.IsPassed ?? false,
                    CompletedAt = a.CompletedAt
                })
                .ToListAsync();

            // Calculate statistics
            var totalAssessments = assessments.Count;
            var passedCount = assessments.Count(a => a.IsPassed);
            var passRate = totalAssessments > 0 ? passedCount * 100.0 / totalAssessments : 0;
            var averageScore = totalAssessments > 0 ? assessments.Average(a => (double)a.Score) : 0;

            // Build ViewModel
            var viewModel = new UserAssessmentHistoryViewModel
            {
                UserId = userId,
                UserFullName = targetUser.FullName,
                UserNIP = targetUser.NIP,
                UserSection = targetUser.Section,
                UserPosition = targetUser.Position,
                TotalAssessments = totalAssessments,
                PassedCount = passedCount,
                PassRate = passRate,
                AverageScore = averageScore,
                Assessments = assessments
            };

            return View(viewModel);
        }

        // GET /Admin/AuditLog
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AuditLog(int page = 1, DateTime? startDate = null, DateTime? endDate = null)
        {
            const int pageSize = 25;

            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt < endDate.Value.AddDays(1));

            var paging = PaginationHelper.Calculate(await query.CountAsync(), page, pageSize);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(paging.Skip)
                .Take(paging.Take)
                .ToListAsync();

            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }

        // GET /Admin/ExportAuditLog
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportAuditLog(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt < endDate.Value.AddDays(1));

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "AuditLog", new[] { "Waktu", "Aktor", "Aksi", "Detail" });
            ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                ws.Cell(i + 2, 1).Value = log.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");
                ws.Cell(i + 2, 2).Value = log.ActorName;
                ws.Cell(i + 2, 3).Value = log.ActionType;
                ws.Cell(i + 2, 4).Value = log.Description;
            }

            var fileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // CloseEarly removed in Phase 162 — replaced by AkhiriSemuaUjian with auto-grading

        // POST /Admin/ReshufflePackage — reshuffle package for single worker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReshufflePackage(int sessionId)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == sessionId);

            if (assessment == null)
                return Json(new { success = false, message = "Session not found." });

            string userStatus;
            if (assessment.CompletedAt != null || assessment.Score != null)
                userStatus = "Completed";
            else if (assessment.Status == "Abandoned")
                userStatus = "Abandoned";
            else if (assessment.StartedAt != null)
                userStatus = "InProgress";
            else
                userStatus = "Not started";

            if (userStatus != "Not started" && userStatus != "Abandoned")
                return Json(new { success = false, message = "Hanya peserta yang belum mulai atau sesi yang ditinggalkan yang dapat di-reshuffle." });

            var siblingSessionIds = await _context.AssessmentSessions
                .Where(s => s.Title == assessment.Title &&
                            s.Category == assessment.Category &&
                            s.Schedule.Date == assessment.Schedule.Date)
                .Select(s => s.Id)
                .ToListAsync();

            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            if (!packages.Any())
                return Json(new { success = false, message = "Assessment ini tidak menggunakan mode paket." });

            var currentAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);

            if (currentAssignment != null)
                _context.UserPackageAssignments.Remove(currentAssignment);

            var rng = Random.Shared;
            var shuffledIds = BuildCrossPackageAssignment(packages, rng);
            var sentinelPackage = packages.First();

            var newAssignment = new UserPackageAssignment
            {
                AssessmentSessionId = sessionId,
                AssessmentPackageId = sentinelPackage.Id,
                UserId = assessment.UserId,
                ShuffledQuestionIds = System.Text.Json.JsonSerializer.Serialize(shuffledIds),
                ShuffledOptionIdsPerQuestion = "{}"
            };
            _context.UserPackageAssignments.Add(newAssignment);

            await _context.SaveChangesAsync();

            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ReshufflePackage",
                    $"Reshuffled package (cross-package) for user {assessment.UserId} on assessment '{assessment.Title}' [SessionID={sessionId}]: {shuffledIds.Count} questions from {packages.Count} packages",
                    sessionId,
                    "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ReshufflePackage (sessionId={Id})", sessionId); }

            return Json(new { success = true, packageName = $"Cross-package ({packages.Count} paket)", assignmentId = newAssignment.Id });
        }

        // POST /Admin/ReshuffleAll — bulk reshuffle for all workers in assessment group
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReshuffleAll(string title, string category, DateTime scheduleDate)
        {
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title &&
                            a.Category == category &&
                            a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
                return Json(new { success = false, message = "Assessment group not found." });

            var siblingSessionIds = sessions.Select(s => s.Id).ToList();
            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            if (!packages.Any())
                return Json(new { success = false, message = "Assessment ini tidak menggunakan mode paket." });

            var existingAssignments = await _context.UserPackageAssignments
                .Where(a => siblingSessionIds.Contains(a.AssessmentSessionId))
                .ToDictionaryAsync(a => a.AssessmentSessionId);

            var rng = Random.Shared;
            var results = new List<object>();
            int reshuffledCount = 0;

            foreach (var session in sessions)
            {
                string userName = session.User?.FullName ?? "Unknown";

                string userStatus;
                if (session.CompletedAt != null || session.Score != null)
                    userStatus = "Completed";
                else if (session.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (session.StartedAt != null)
                    userStatus = "InProgress";
                else
                    userStatus = "Not started";

                if (userStatus != "Not started")
                {
                    string reason = userStatus == "InProgress" ? "sedang mengerjakan"
                                  : userStatus == "Completed" ? "sudah selesai"
                                  : "dibatalkan";
                    results.Add(new { name = userName, status = $"Dilewati — {reason}" });
                    continue;
                }

                existingAssignments.TryGetValue(session.Id, out var existingAssignment);
                var sessionShuffledIds = BuildCrossPackageAssignment(packages, rng);
                var sentinelPackage = packages.First();

                if (existingAssignment != null)
                    _context.UserPackageAssignments.Remove(existingAssignment);

                _context.UserPackageAssignments.Add(new UserPackageAssignment
                {
                    AssessmentSessionId = session.Id,
                    AssessmentPackageId = sentinelPackage.Id,
                    UserId = session.UserId,
                    ShuffledQuestionIds = System.Text.Json.JsonSerializer.Serialize(sessionShuffledIds),
                    ShuffledOptionIdsPerQuestion = "{}"
                });

                results.Add(new { name = userName, status = $"Reshuffled (cross-package, {packages.Count} paket)" });
                reshuffledCount++;
            }

            await _context.SaveChangesAsync();

            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ReshuffleAll",
                    $"Bulk reshuffled {reshuffledCount} worker(s) on assessment '{title}' [{category}] scheduled {scheduleDate:yyyy-MM-dd}",
                    null,
                    "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ReshuffleAll (groupTitle={Title})", title); }

            return Json(new { success = true, results, reshuffledCount });
        }

        #region Helper Methods

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<int> BuildCrossPackageAssignment(List<AssessmentPackage> packages, Random rng)
        {
            if (packages.Count == 0)
                return new List<int>();

            // Single package: shuffle question order so each worker sees a unique sequence
            if (packages.Count == 1)
            {
                var singlePackageQuestions = packages[0].Questions;
                if (singlePackageQuestions == null || !singlePackageQuestions.Any())
                    return new List<int>();
                var singlePackageIds = singlePackageQuestions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
                Shuffle(singlePackageIds, rng);
                return singlePackageIds;
            }

            // Safety fallback: use minimum question count across packages (edge case per user decision)
            int K = packages.Min(p => p.Questions.Count);
            if (K == 0)
                return new List<int>();

            // Collect all questions across all packages with their package index
            var allQuestions = packages.SelectMany((p, pIdx) =>
                p.Questions.Select(q => new { Question = q, PackageIndex = pIdx })).ToList();

            // Identify distinct ET groups (non-null ElemenTeknis values across all packages)
            var etGroups = allQuestions
                .Where(x => !string.IsNullOrWhiteSpace(x.Question.ElemenTeknis))
                .Select(x => x.Question.ElemenTeknis!)
                .Distinct()
                .ToList();

            // Fallback: if no questions have ElemenTeknis, use original slot-list algorithm
            if (etGroups.Count == 0)
            {
                // No ElemenTeknis data — fall back to original slot-list distribution
                int N0 = packages.Count;
                int baseCount0 = K / N0;
                int remainder0 = K % N0;
                var remainderIndices0 = Enumerable.Range(0, N0)
                    .OrderBy(_ => rng.Next())
                    .Take(remainder0)
                    .ToHashSet();
                var slots0 = new List<int>();
                for (int i = 0; i < N0; i++)
                {
                    int count = baseCount0 + (remainderIndices0.Contains(i) ? 1 : 0);
                    for (int j = 0; j < count; j++)
                        slots0.Add(i);
                }
                Shuffle(slots0, rng);
                var pkgCounter0 = new int[N0];
                var fallbackIds = new List<int>();
                var orderedQuestions0 = packages.Select(p => p.Questions.OrderBy(q => q.Order).ToList()).ToList();
                for (int pos = 0; pos < K; pos++)
                {
                    int pkgIdx = slots0[pos];
                    var question = orderedQuestions0[pkgIdx][pkgCounter0[pkgIdx]];
                    pkgCounter0[pkgIdx]++;
                    fallbackIds.Add(question.Id);
                }
                return fallbackIds;
            }

            // ET-aware distribution
            var selectedIds = new HashSet<int>();
            var selectedList = new List<int>();

            // Phase 1 — Guarantee one question per ET group (best-effort, capped at K)
            // NULL ElemenTeknis questions are excluded from Phase 1 (they participate in Phase 2 only)
            foreach (var etGroup in etGroups)
            {
                if (selectedIds.Count >= K) break;

                var candidates = allQuestions
                    .Where(x => x.Question.ElemenTeknis == etGroup && !selectedIds.Contains(x.Question.Id))
                    .Select(x => x.Question.Id)
                    .ToList();

                Shuffle(candidates, rng);
                if (candidates.Count > 0)
                {
                    int picked = candidates[0];
                    selectedIds.Add(picked);
                    selectedList.Add(picked);
                }
            }

            // Phase 2 — Fill remaining quota with balanced package distribution
            int remaining = K - selectedIds.Count;
            if (remaining > 0)
            {
                int N = packages.Count;
                var orderedByPackage = packages
                    .Select(p => p.Questions.OrderBy(q => q.Order)
                        .Where(q => !selectedIds.Contains(q.Id))
                        .ToList())
                    .ToList();

                // Build slot list for remaining slots using balanced distribution
                int baseCount = remaining / N;
                int remainder = remaining % N;
                var remainderIndices = Enumerable.Range(0, N)
                    .OrderBy(_ => rng.Next())
                    .Take(remainder)
                    .ToHashSet();

                var slots = new List<int>();
                for (int i = 0; i < N; i++)
                {
                    int count = baseCount + (remainderIndices.Contains(i) ? 1 : 0);
                    for (int j = 0; j < count; j++)
                        slots.Add(i);
                }
                Shuffle(slots, rng);

                var pkgCounter = new int[N];
                var pkgAvailable = orderedByPackage.Select(q => q.Count).ToArray();

                foreach (int pkgIdx in slots)
                {
                    // Find a package with available unselected questions
                    int targetPkg = pkgIdx;
                    if (pkgCounter[targetPkg] >= pkgAvailable[targetPkg])
                    {
                        // Redistribute: find any package with remaining questions
                        targetPkg = -1;
                        for (int i = 0; i < N; i++)
                        {
                            if (pkgCounter[i] < pkgAvailable[i]) { targetPkg = i; break; }
                        }
                        if (targetPkg == -1) break; // All packages exhausted
                    }
                    var q = orderedByPackage[targetPkg][pkgCounter[targetPkg]];
                    pkgCounter[targetPkg]++;
                    selectedIds.Add(q.Id);
                    selectedList.Add(q.Id);
                }
            }

            // Phase 3 — Fisher-Yates shuffle the combined list
            Shuffle(selectedList, rng);
            return selectedList;
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

                            var allApproved = !await _context.ProtonDeliverableProgresses
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
        public IActionResult DownloadImportTemplate()
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

            ws.Cell(3, 1).Value = "Kolom Bagian: RFCC / DHT / HMU / NGP / GAST";
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
                return RedirectToAction("ManageAssessment", new { tab = "training" });
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
                return RedirectToAction("ManageAssessment", new { tab = "training" });
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
            return RedirectToAction("ManageAssessment", new { tab = "training" });
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
                    return RedirectToAction("ManageAssessment", new { tab = "training" });
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Ukuran file maksimal 10MB.";
                    return RedirectToAction("ManageAssessment", new { tab = "training" });
                }
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("ManageAssessment", new { tab = "training" });
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
            return RedirectToAction("ManageAssessment", new { tab = "training" });
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
            return RedirectToAction("ManageAssessment", new { tab = "training" });
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

        // Question Management (Admin) region removed in Phase 227 (CLEN-02) — ManageQuestions/AddQuestion/DeleteQuestion
        // were legacy-only actions. Assessment questions are now managed via Package Management.

        #region Package Management (Admin)

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManagePackages(int assessmentId)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == assessmentId);
            if (assessment == null) return NotFound();

            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .Where(p => p.AssessmentSessionId == assessmentId)
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            var packageIds = packages.Select(p => p.Id).ToList();
            var assignmentCounts = await _context.UserPackageAssignments
                .Where(a => packageIds.Contains(a.AssessmentPackageId))
                .GroupBy(a => a.AssessmentPackageId)
                .Select(g => new { PackageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PackageId, x => x.Count);
            ViewBag.AssignmentCounts = assignmentCounts;

            // ET coverage: rows = ET groups, columns = per package
            var allEtGroups = packages
                .SelectMany(p => p.Questions)
                .Select(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "(Tanpa ET)" : q.ElemenTeknis!.Trim())
                .Distinct()
                .OrderBy(g => g == "(Tanpa ET)" ? "zzz" : g) // Tanpa ET last
                .ToList();

            // Dictionary: etGroup -> Dictionary<packageId, questionCount>
            var etCoverage = new Dictionary<string, Dictionary<int, int>>();
            foreach (var et in allEtGroups)
            {
                etCoverage[et] = new Dictionary<int, int>();
                foreach (var pkg in packages)
                {
                    var count = pkg.Questions.Count(q =>
                        (string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "(Tanpa ET)" : q.ElemenTeknis!.Trim()) == et);
                    etCoverage[et][pkg.Id] = count;
                }
            }
            ViewBag.EtCoverage = etCoverage;
            ViewBag.EtGroups = allEtGroups;

            ViewBag.Packages = packages;
            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = assessmentId;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePackage(int assessmentId, string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                TempData["Error"] = "Package name is required.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            var assessment = await _context.AssessmentSessions.FindAsync(assessmentId);
            if (assessment == null) return NotFound();

            var existingCount = await _context.AssessmentPackages
                .CountAsync(p => p.AssessmentSessionId == assessmentId);

            var pkg = new AssessmentPackage
            {
                AssessmentSessionId = assessmentId,
                PackageName = packageName.Trim(),
                PackageNumber = existingCount + 1
            };
            _context.AssessmentPackages.Add(pkg);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Package '{packageName}' created.";
            return RedirectToAction("ManagePackages", new { assessmentId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePackage(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (pkg == null) return NotFound();

            int assessmentId = pkg.AssessmentSessionId;

            var questionIds = pkg.Questions.Select(q => q.Id).ToList();
            if (questionIds.Any())
            {
                var pkgResponses = await _context.PackageUserResponses
                    .Where(r => questionIds.Contains(r.PackageQuestionId))
                    .ToListAsync();
                if (pkgResponses.Any())
                    _context.PackageUserResponses.RemoveRange(pkgResponses);
            }

            var assignments = await _context.UserPackageAssignments
                .Where(a => a.AssessmentPackageId == packageId)
                .ToListAsync();
            if (assignments.Any())
                _context.UserPackageAssignments.RemoveRange(assignments);

            foreach (var q in pkg.Questions)
                _context.PackageOptions.RemoveRange(q.Options);
            _context.PackageQuestions.RemoveRange(pkg.Questions);
            _context.AssessmentPackages.Remove(pkg);

            await _context.SaveChangesAsync();

            try
            {
                var delUser = await _userManager.GetUserAsync(User);
                var delActorName = string.IsNullOrWhiteSpace(delUser?.NIP) ? (delUser?.FullName ?? "Unknown") : $"{delUser.NIP} - {delUser.FullName}";
                await _auditLog.LogAsync(
                    delUser?.Id ?? "",
                    delActorName,
                    "DeletePackage",
                    $"Deleted package '{pkg.PackageName}' from assessment [ID={assessmentId}]" +
                        (assignments.Any() ? $" ({assignments.Count} assignment(s) removed)" : ""),
                    assessmentId,
                    "AssessmentPackage");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeletePackage (packageId={Id})", packageId); }

            TempData["Success"] = $"Package '{pkg.PackageName}' deleted.";
            return RedirectToAction("ManagePackages", new { assessmentId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> PreviewPackage(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (pkg == null) return NotFound();

            var assessment = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (assessment == null) return NotFound();

            ViewBag.PackageName = pkg.PackageName;
            ViewBag.AssessmentTitle = assessment?.Title ?? "";
            ViewBag.AssessmentId = pkg.AssessmentSessionId;

            return View(pkg.Questions.OrderBy(q => q.Order).ToList());
        }

        // GET /Admin/DownloadQuestionTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadQuestionTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Question Import");

            var headers = new[] { "Question", "Option A", "Option B", "Option C", "Option D", "Correct", "Elemen Teknis" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Example row (italic, gray)
            var example = new[]
            {
                "Apa fungsi utama unit RFCC dalam proses pengolahan minyak?",
                "Memecah molekul berat menjadi fraksi ringan",
                "Memurnikan air limbah industri",
                "Menghasilkan energi listrik dari gas alam",
                "Mengolah bahan baku batubara menjadi coke",
                "A",
                "Elemen Teknis x.x"
            };
            for (int i = 0; i < example.Length; i++)
            {
                ws.Cell(2, i + 1).Value = example[i];
                ws.Cell(2, i + 1).Style.Font.Italic = true;
                ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray;
            }

            // Instruction rows
            ws.Cell(3, 1).Value = "Kolom Correct: isi dengan huruf A, B, C, atau D (tidak peka huruf besar/kecil)";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

            ws.Cell(4, 1).Value = "Kolom Elemen Teknis: opsional, isi nama elemen teknis. Kosongkan jika tidak ada.";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;

            return ExcelExportHelper.ToFileResult(workbook, "question_import_template.xlsx", this);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ImportPackageQuestions(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            ViewBag.PackageId = packageId;
            ViewBag.PackageName = pkg.PackageName;
            ViewBag.AssessmentId = pkg.AssessmentSessionId;
            ViewBag.CurrentQuestionCount = pkg.Questions.Count;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPackageQuestions(
            int packageId, IFormFile? excelFile, string? pasteText)
        {
            // File type guard: only allow Excel files
            if (excelFile != null && excelFile.Length > 0)
            {
                var allowedQuestionsExtensions = new[] { ".xlsx", ".xls" };
                var questionsExt = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
                if (!allowedQuestionsExtensions.Contains(questionsExt))
                {
                    TempData["Error"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                    return RedirectToAction("ImportPackageQuestions", new { packageId });
                }
            }

            // File size guard: reject files larger than 5 MB to avoid memory pressure
            if (excelFile != null && excelFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File terlalu besar. Maksimal ukuran file adalah 5 MB.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            var existingFingerprints = pkg.Questions.Select(q =>
            {
                var opts = q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList();
                return MakePackageFingerprint(
                    q.QuestionText,
                    opts.ElementAtOrDefault(0) ?? "",
                    opts.ElementAtOrDefault(1) ?? "",
                    opts.ElementAtOrDefault(2) ?? "",
                    opts.ElementAtOrDefault(3) ?? "");
            }).ToHashSet();
            var seenInBatch = new HashSet<string>();

            List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct, string? ElemenTeknis)> rows;
            var errors = new List<string>();

            if (excelFile != null && excelFile.Length > 0)
            {
                rows = new List<(string, string, string, string, string, string, string?)>();
                try
                {
                    using var stream = excelFile.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    int rowNum = 1;
                    foreach (var row in ws.RowsUsed().Skip(1))
                    {
                        rowNum++;
                        var q   = (row.Cell(1).GetString() ?? "").Trim();
                        var a   = (row.Cell(2).GetString() ?? "").Trim();
                        var b   = (row.Cell(3).GetString() ?? "").Trim();
                        var c   = (row.Cell(4).GetString() ?? "").Trim();
                        var d   = (row.Cell(5).GetString() ?? "").Trim();
                        var cor = (row.Cell(6).GetString() ?? "").Trim().ToUpper();
                        var cell7 = (row.Cell(7).GetString() ?? "").Trim();
                        string? subComp = string.IsNullOrWhiteSpace(cell7) ? null : cell7;
                        rows.Add((q, a, b, c, d, cor, subComp));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read Excel file for package {PackageId}", packageId);
                    TempData["Error"] = "Gagal membaca file Excel. Pastikan format file benar.";
                    return RedirectToAction("ImportPackageQuestions", new { packageId });
                }
            }
            else if (!string.IsNullOrWhiteSpace(pasteText))
            {
                rows = new List<(string, string, string, string, string, string, string?)>();
                var lines = pasteText.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                int startIndex = 0;
                if (lines.Count > 0)
                {
                    var firstCells = lines[0].Split('\t');
                    if (firstCells.Length >= 6 && firstCells[5].Trim().ToLower() == "correct")
                        startIndex = 1;
                }

                for (int i = startIndex; i < lines.Count; i++)
                {
                    var cells = lines[i].Split('\t');
                    if (cells.Length < 6)
                    {
                        errors.Add($"Row {i + 1}: expected at least 6 columns, got {cells.Length}.");
                        continue;
                    }
                    string? subComp = cells.Length >= 7 ? cells[6].Trim() : null;
                    if (string.IsNullOrWhiteSpace(subComp)) subComp = null;
                    rows.Add((
                        cells[0].Trim(), cells[1].Trim(), cells[2].Trim(),
                        cells[3].Trim(), cells[4].Trim(), cells[5].Trim().ToUpper(),
                        subComp
                    ));
                }
            }
            else
            {
                TempData["Error"] = "Please upload an Excel file or paste question data.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Cross-package count validation
            var targetSession = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (targetSession != null)
            {
                var siblingSessionIds = await _context.AssessmentSessions
                    .Where(s => s.Title == targetSession.Title &&
                                s.Category == targetSession.Category &&
                                s.Schedule.Date == targetSession.Schedule.Date)
                    .Select(s => s.Id)
                    .ToListAsync();

                var siblingPackagesWithQuestions = await _context.AssessmentPackages
                    .Include(p => p.Questions)
                    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId)
                             && p.Id != packageId
                             && p.Questions.Any())
                    .ToListAsync();

                if (siblingPackagesWithQuestions.Any())
                {
                    var validRowCount = rows.Count(r =>
                    {
                        var (rq, ra, rb, rc, rd, rcor, _) = r;
                        var normalizedCor = ExtractPackageCorrectLetter(rcor);
                        return !string.IsNullOrWhiteSpace(rq) &&
                               !string.IsNullOrWhiteSpace(ra) && !string.IsNullOrWhiteSpace(rb) &&
                               !string.IsNullOrWhiteSpace(rc) && !string.IsNullOrWhiteSpace(rd) &&
                               new[] { "A", "B", "C", "D" }.Contains(normalizedCor);
                    });

                    var referencePackage = siblingPackagesWithQuestions.First();
                    int expectedCount = referencePackage.Questions.Count;

                    if (validRowCount != expectedCount)
                    {
                        TempData["Error"] = $"Jumlah soal tidak sama dengan paket lain. {referencePackage.PackageName}: {expectedCount} soal. Harap masukkan {expectedCount} soal.";
                        return RedirectToAction("ImportPackageQuestions", new { packageId });
                    }
                }
            }

            int order = pkg.Questions.Count + 1;
            int added = 0;
            int skipped = 0;
            // Collect all new questions (with options embedded) before saving — avoids N+1 SaveChangesAsync
            var newQuestions = new List<PackageQuestion>();
            for (int i = 0; i < rows.Count; i++)
            {
                var (q, a, b, c, d, cor, rawSubComp) = rows[i];
                var normalizedCor = ExtractPackageCorrectLetter(cor);
                if (string.IsNullOrWhiteSpace(q))
                {
                    errors.Add($"Row {i + 1}: Question text is empty. Skipped.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) ||
                    string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(d))
                {
                    errors.Add($"Row {i + 1}: One or more options are empty. Skipped.");
                    continue;
                }
                if (!new[] { "A", "B", "C", "D" }.Contains(normalizedCor))
                {
                    errors.Add($"Row {i + 1}: 'Correct' column must be A, B, C, or D. Got '{cor}'. Skipped.");
                    continue;
                }

                var fp = MakePackageFingerprint(q, a, b, c, d);
                if (existingFingerprints.Contains(fp) || seenInBatch.Contains(fp))
                {
                    skipped++;
                    continue;
                }
                seenInBatch.Add(fp);

                int correctIndex = normalizedCor == "A" ? 0 : normalizedCor == "B" ? 1 : normalizedCor == "C" ? 2 : 3;
                var newQ = new PackageQuestion
                {
                    AssessmentPackageId = packageId,
                    QuestionText = q,
                    Order = order++,
                    ScoreValue = 10,
                    ElemenTeknis = NormalizeElemenTeknis(rawSubComp),
                    // Add options directly to the navigation collection (EF resolves FK after save)
                    Options = new List<PackageOption>
                    {
                        new PackageOption { OptionText = a, IsCorrect = (0 == correctIndex) },
                        new PackageOption { OptionText = b, IsCorrect = (1 == correctIndex) },
                        new PackageOption { OptionText = c, IsCorrect = (2 == correctIndex) },
                        new PackageOption { OptionText = d, IsCorrect = (3 == correctIndex) }
                    }
                };
                newQuestions.Add(newQ);
                added++;
            }

            // Persist all new questions + options in a single transaction (single SaveChangesAsync)
            if (newQuestions.Count > 0)
            {
                _context.PackageQuestions.AddRange(newQuestions);
                using var importTx = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await importTx.CommitAsync();
                }
                catch
                {
                    await importTx.RollbackAsync();
                    throw;
                }
            }

            if (added == 0 && skipped == 0)
            {
                TempData["Warning"] = "No valid questions found in the import. Check the format and try again.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }
            if (added == 0 && skipped > 0)
            {
                TempData["Warning"] = "All questions were already in the package. Nothing was added.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Audit log
            try
            {
                var importUser = await _userManager.GetUserAsync(User);
                var importActorName = string.IsNullOrWhiteSpace(importUser?.NIP) ? (importUser?.FullName ?? "Unknown") : $"{importUser.NIP} - {importUser.FullName}";
                string source = excelFile != null && excelFile.Length > 0 ? $"file '{excelFile.FileName}'" : "pasted text";
                await _auditLog.LogAsync(
                    importUser?.Id ?? "",
                    importActorName,
                    "ImportQuestions",
                    $"Imported {added} questions from {source} to package {pkg.PackageName} [ID={packageId}] in assessment {pkg.AssessmentSessionId}",
                    packageId,
                    "AssessmentPackage");
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "Audit log write failed for ImportPackageQuestions {PackageId}", packageId);
            }

            // Cross-package ElemenTeknis distribution warning
            if (added > 0 && targetSession != null)
            {
                // Gather all ET groups across ALL packages in this assessment (including current)
                var allPackagesForSession = await _context.AssessmentPackages
                    .Include(p => p.Questions)
                    .Where(p => p.AssessmentSessionId == pkg.AssessmentSessionId)
                    .ToListAsync();

                if (allPackagesForSession.Count > 1)
                {
                    var allEtGroups = allPackagesForSession
                        .SelectMany(p => p.Questions)
                        .Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                        .Select(q => q.ElemenTeknis!.Trim())
                        .Distinct()
                        .ToHashSet();

                    if (allEtGroups.Any())
                    {
                        var missingPerPackage = new List<string>();
                        foreach (var p in allPackagesForSession)
                        {
                            var pkgEtGroups = p.Questions
                                .Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                                .Select(q => q.ElemenTeknis!.Trim())
                                .Distinct()
                                .ToHashSet();
                            var missing = allEtGroups.Except(pkgEtGroups).ToList();
                            if (missing.Any())
                                missingPerPackage.Add($"{p.PackageName}: tidak ada soal untuk {string.Join(", ", missing)}");
                        }

                        if (missingPerPackage.Any())
                        {
                            TempData["Warning"] = "Distribusi Elemen Teknis tidak lengkap — " + string.Join("; ", missingPerPackage) +
                                ". Pastikan setiap paket memiliki minimal 1 soal per Elemen Teknis untuk hasil assessment yang optimal.";
                        }
                    }
                }
            }

            if (excelFile != null && excelFile.Length > 0)
                TempData["Success"] = $"Imported from file: {added} added, {skipped} skipped.";
            else
                TempData["Success"] = $"{added} added, {skipped} skipped.";

            return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
        }

        // Package import helpers (named with "Package" prefix to avoid collision)
        private static string ExtractPackageCorrectLetter(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            if (raw.Length == 1) return raw;
            if ("ABCD".Contains(raw[0]) && !char.IsLetterOrDigit(raw[1]))
                return raw[0].ToString();
            if (raw.StartsWith("OPTION ") && raw.Length > 7 && "ABCD".Contains(raw[7]))
                return raw[7].ToString();
            return raw;
        }

        private static string? NormalizeElemenTeknis(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }

        private static string NormalizePackageText(string s)
            => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();

        private static string MakePackageFingerprint(string q, string a, string b, string c, string d)
            => string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizePackageText));

        #endregion

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

        #region Activity Log (Phase 166)

        /// <summary>
        /// Returns the activity log for a given exam session as JSON.
        /// Used by HC to audit worker behaviour during the exam.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetActivityLog(int sessionId)
        {
            var session = await _context.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound(new { error = "Session not found." });

            var wib = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var events = await _context.ExamActivityLogs
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.Timestamp)
                .Select(l => new
                {
                    l.EventType,
                    l.Detail,
                    TimestampUtc = l.Timestamp
                })
                .ToListAsync();

            var eventsFormatted = events.Select(e => new
            {
                e.EventType,
                e.Detail,
                Timestamp = TimeZoneInfo.ConvertTimeFromUtc(e.TimestampUtc, wib).ToString("HH:mm:ss")
            }).ToList();

            var totalAnswered = await _context.PackageUserResponses
                .CountAsync(r => r.AssessmentSessionId == sessionId);

            var lastEventTime = await _context.ExamActivityLogs
                .Where(l => l.SessionId == sessionId)
                .MaxAsync(l => (DateTime?)l.Timestamp);

            int? timeSpentSeconds = null;
            if (session.StartedAt.HasValue)
            {
                var endTime = session.CompletedAt ?? lastEventTime ?? DateTime.UtcNow;
                timeSpentSeconds = (int)(endTime - session.StartedAt.Value).TotalSeconds;
            }

            var disconnectCount = eventsFormatted.Count(e => e.EventType == "disconnected");

            var summary = new
            {
                answeredCount = totalAnswered,
                disconnectCount,
                timeSpentSeconds,
                startedAt = session.StartedAt.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(session.StartedAt.Value, wib).ToString("HH:mm:ss")
                    : null,
                completedAt = session.CompletedAt.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(session.CompletedAt.Value, wib).ToString("HH:mm:ss")
                    : null
            };

            return Json(new { summary, events = eventsFormatted });
        }

        #endregion

        // Certificate Helpers moved to Helpers/CertNumberHelper.cs (Phase 227 CLEN-04)

        #region Renewal Certificate

        private static string MapKategori(string? raw, Dictionary<string, string>? rawToDisplayMap)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            var trimmed = raw.Trim();
            if (rawToDisplayMap != null && rawToDisplayMap.TryGetValue(trimmed.ToUpperInvariant(), out var displayName))
                return displayName;
            return trimmed;
        }

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

            unit.Name = name.Trim();
            await _context.SaveChangesAsync();

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
