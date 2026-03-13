using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Models;
using HcPortal.Models.Competency;
using HcPortal.Data;
using HcPortal.Services;
using HcPortal.Hubs;
using Microsoft.AspNetCore.SignalR;
using ClosedXML.Excel;
using System.Globalization;

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

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IMemoryCache cache,
            IConfiguration config,
            IWebHostEnvironment env,
            ILogger<AdminController> logger,
            INotificationService notificationService,
            IHubContext<AssessmentHub> hubContext)
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
        }

        // GET /Admin/Index
        [Authorize(Roles = "Admin, HC")]
        public IActionResult Index()
        {
            return View();
        }

        #region KKJ File Management

        // GET /Admin/KkjMatrix?bagian={bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> KkjMatrix(int? bagian)
        {
            ViewData["Title"] = "Kelola KKJ Matrix";

            // Seed default bagians if none exist yet
            if (!await _context.KkjBagians.AnyAsync())
            {
                var defaults = new[] {
                    new KkjBagian { Name = "RFCC",    DisplayOrder = 1 },
                    new KkjBagian { Name = "GAST",    DisplayOrder = 2 },
                    new KkjBagian { Name = "NGP",     DisplayOrder = 3 },
                    new KkjBagian { Name = "DHT/HMU", DisplayOrder = 4 },
                };
                _context.KkjBagians.AddRange(defaults);
                await _context.SaveChangesAsync();
            }

            var bagians = await _context.KkjBagians
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // Load active (non-archived) files grouped by bagianId
            var files = await _context.KkjFiles
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            var filesByBagian = bagians.ToDictionary(
                b => b.Id,
                b => files.Where(f => f.BagianId == b.Id).ToList()
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
            var bagians = await _context.KkjBagians.OrderBy(b => b.DisplayOrder).ToListAsync();
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

            var bagian = await _context.KkjBagians.FindAsync(bagianId);
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
                    BagianId = bagianId,
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
                .Include(f => f.Bagian)
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
            int bagianId = kkjFile.BagianId;

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
            var bagian = await _context.KkjBagians.FindAsync(bagianId);
            if (bagian == null) return NotFound();

            var archivedFiles = await _context.KkjFiles
                .Where(f => f.BagianId == bagianId && f.IsArchived)
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
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianDelete(int id, bool confirmed = false)
        {
            var bagian = await _context.KkjBagians.FindAsync(id);
            if (bagian == null)
                return Json(new { success = false, message = "Bagian tidak ditemukan." });

            // Count ACTIVE files (not archived) — these block deletion
            var activeKkjCount = await _context.KkjFiles.CountAsync(f => f.BagianId == id && !f.IsArchived);
            var activeCpdpCount = await _context.CpdpFiles.CountAsync(f => f.BagianId == id && !f.IsArchived);
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
            var archivedKkjCount = await _context.KkjFiles.CountAsync(f => f.BagianId == id && f.IsArchived);
            var archivedCpdpCount = await _context.CpdpFiles.CountAsync(f => f.BagianId == id && f.IsArchived);
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
                .Where(f => f.BagianId == id && f.IsArchived)
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
                .Where(f => f.BagianId == id && f.IsArchived)
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

            _context.KkjBagians.Remove(bagian);
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
                    id, "KkjBagian");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for KkjBagianDelete (bagianId={Id})", id); }

            return Json(new { success = true, message = $"Bagian '{bagian.Name}' berhasil dihapus." });
        }

        #region CPDP File Management

        // GET /Admin/CpdpFiles?bagian={bagianId}
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CpdpFiles(int? bagian)
        {
            ViewData["Title"] = "CPDP File Management";

            var bagians = await _context.KkjBagians
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // Load active (non-archived) CPDP files grouped by bagianId
            var files = await _context.CpdpFiles
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            var filesByBagian = bagians.ToDictionary(
                b => b.Id,
                b => files.Where(f => f.BagianId == b.Id).ToList());

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
            var bagians = await _context.KkjBagians.OrderBy(b => b.DisplayOrder).ToListAsync();
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

            var bagian = await _context.KkjBagians.FindAsync(bagianId);
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
                    BagianId = bagianId,
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
                .Include(f => f.Bagian)
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
            int cpdpBagianId = cpdpFile.BagianId;
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
            var bagian = await _context.KkjBagians.FindAsync(bagianId);
            if (bagian == null) return NotFound();

            var archivedFiles = await _context.CpdpFiles
                .Where(f => f.BagianId == bagianId && f.IsArchived)
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
                        UserCount = g.Count()
                    };
                })
                .OrderByDescending(g => g.Schedule)
                .ToList();

            var totalCount = grouped.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            ViewBag.ManagementData = grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
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
                    workers = await GetWorkersInSection(section, unit, category, search, statusFilter);

                var (assessmentHistory, trainingHistory) = await GetAllWorkersHistory();
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

        // --- CREATE ASSESSMENT ---
        // GET: Show create assessment form
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CreateAssessment()
        {
            // Get list of users for dropdown
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.SelectedUserIds = new List<string>();
            ViewBag.Sections = OrganizationStructure.GetAllSections();
            ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();

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

            return View(model);
        }

        // POST: Process form submission (multi-user)
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssessment(AssessmentSession model, List<string> UserIds)
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
                ViewBag.Sections = OrganizationStructure.GetAllSections();
                ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
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
                    ViewBag.Sections = OrganizationStructure.GetAllSections();
                    ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
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
                        return View("CreateAssessment", model);
                    }
                    protonTahunKe = protonTrack.TahunKe;
                }

                // Create all sessions in memory first
                var sessions = new List<AssessmentSession>();

                foreach (var userId in UserIds)
                {
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
                        Progress = 0,
                        UserId = userId,
                        CreatedBy = currentUser?.Id
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

                // Single SaveChanges with transaction (atomicity)
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

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
                ViewBag.Sections = OrganizationStructure.GetAllSections();
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
            ViewBag.Sections = OrganizationStructure.GetAllSections();

            // Count packages attached to this assessment's sibling group (for schedule-change warning)
            var siblingIds = siblings.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            ViewBag.PackageCount = packageCount;
            ViewBag.OriginalSchedule = assessment.Schedule.ToString("yyyy-MM-dd");

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

            if (model.Schedule < DateTime.Today)
                editErrors.Add("Schedule date cannot be in the past.");
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
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(a => a.Responses)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assessment == null)
                {
                    logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
                    TempData["Error"] = "Assessment not found.";
                    return RedirectToAction("ManageAssessment");
                }

                var assessmentTitle = assessment.Title;
                logger.LogInformation($"Attempting to delete assessment {id}: {assessmentTitle}");

                // Delete in correct order to avoid FK constraint violations
                // 1. Delete UserResponses first
                if (assessment.Responses.Any())
                {
                    logger.LogInformation($"Deleting {assessment.Responses.Count} user responses");
                    _context.UserResponses.RemoveRange(assessment.Responses);
                }

                // 2. Delete Options (child of Questions)
                if (assessment.Questions.Any())
                {
                    var allOptions = assessment.Questions.SelectMany(q => q.Options).ToList();
                    if (allOptions.Any())
                    {
                        logger.LogInformation($"Deleting {allOptions.Count} question options");
                        _context.AssessmentOptions.RemoveRange(allOptions);
                    }

                    // 3. Delete Questions
                    logger.LogInformation($"Deleting {assessment.Questions.Count} questions");
                    _context.AssessmentQuestions.RemoveRange(assessment.Questions);
                }

                // 4. Delete PackageUserResponses (Restrict FK — must be removed before session)
                var pkgResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                if (pkgResponses.Any())
                {
                    logger.LogInformation($"Deleting {pkgResponses.Count} package user responses");
                    _context.PackageUserResponses.RemoveRange(pkgResponses);
                }

                // 5. Delete AssessmentAttemptHistory rows (no FK — orphaned if not removed)
                var attemptHistory = await _context.AssessmentAttemptHistory
                    .Where(h => h.SessionId == id)
                    .ToListAsync();
                if (attemptHistory.Any())
                {
                    logger.LogInformation($"Deleting {attemptHistory.Count} attempt history records");
                    _context.AssessmentAttemptHistory.RemoveRange(attemptHistory);
                }

                // Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)

                // 6. Finally delete the assessment itself
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
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(a => a.Responses)
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

                // Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)

                foreach (var session in siblings)
                {
                    if (session.Responses.Any())
                        _context.UserResponses.RemoveRange(session.Responses);

                    if (session.Questions.Any())
                    {
                        var opts = session.Questions.SelectMany(q => q.Options).ToList();
                        if (opts.Any()) _context.AssessmentOptions.RemoveRange(opts);
                        _context.AssessmentQuestions.RemoveRange(session.Questions);
                    }

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
                    IsCompleted = a.CompletedAt != null || a.Score != null,
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
            else
            {
                // Legacy mode: count AssessmentQuestion rows per session
                questionCountMap = await _context.AssessmentQuestions
                    .Where(q => siblingIds.Contains(q.AssessmentSessionId))
                    .GroupBy(q => q.AssessmentSessionId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Count());
            }

            var sessionViewModels = sessions.Select(a =>
            {
                string userStatus;
                if (a.CompletedAt != null || a.Score != null)
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
                    QuestionCount = questionCountMap.TryGetValue(a.Id, out var qc) ? qc : 0
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

        // --- GET MONITORING PROGRESS (polling endpoint for real-time monitoring) ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetMonitoringProgress(string title, string category, DateTime scheduleDate)
        {
            // Step 1: load sessions (same filter as AssessmentMonitoringDetail)
            var sessions = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
                return Json(Array.Empty<object>());

            var siblingIds = sessions.Select(s => s.Id).ToList();

            // Step 2: detect package mode
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Step 3: build total question count map per session (reuse pattern from AssessmentMonitoringDetail)
            Dictionary<int, int> questionCountMap;
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
            else
            {
                questionCountMap = await _context.AssessmentQuestions
                    .Where(q => siblingIds.Contains(q.AssessmentSessionId))
                    .GroupBy(q => q.AssessmentSessionId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }

            // Step 4: build answered count map (single GROUP BY query, not N+1)
            Dictionary<int, int> answeredCountMap;
            if (isPackageMode)
            {
                answeredCountMap = await _context.PackageUserResponses
                    .Where(p => siblingIds.Contains(p.AssessmentSessionId))
                    .GroupBy(p => p.AssessmentSessionId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            else
            {
                answeredCountMap = await _context.UserResponses
                    .Where(r => siblingIds.Contains(r.AssessmentSessionId))
                    .GroupBy(r => r.AssessmentSessionId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }

            // Step 5: project to DTOs
            var dtos = sessions.Select(a =>
            {
                string status;
                if (a.CompletedAt != null || a.Score != null)
                    status = "Completed";
                else if (a.Status == "Abandoned")
                    status = "Abandoned";
                else if (a.StartedAt != null)
                    status = "InProgress";
                else
                    status = "Not started";

                int? remainingSeconds = null;
                if (status == "InProgress")
                    remainingSeconds = Math.Max(0, (a.DurationMinutes * 60) - a.ElapsedSeconds);

                string? result = a.IsPassed == true ? "Pass" : a.IsPassed == false ? "Fail" : null;

                return new
                {
                    sessionId      = a.Id,
                    status,
                    progress       = answeredCountMap.TryGetValue(a.Id, out var ans) ? ans : 0,
                    totalQuestions = questionCountMap.TryGetValue(a.Id, out var total) ? total : 0,
                    score          = a.Score,
                    result,
                    remainingSeconds,
                    completedAt    = a.CompletedAt
                };
            }).ToList();

            return Json(dtos);
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

            // 1. Delete UserResponse records for this session (legacy path answers)
            var responses = await _context.UserResponses
                .Where(r => r.AssessmentSessionId == id)
                .ToListAsync();
            if (responses.Any())
                _context.UserResponses.RemoveRange(responses);

            // 1b. Delete PackageUserResponse records for this session (package path answers)
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
            }
            else
            {
                // ---- LEGACY PATH ----
                // Find sibling session that has questions attached
                var siblingSessionIds = await _context.AssessmentSessions
                    .Where(s => s.Title == session.Title &&
                                s.Category == session.Category &&
                                s.Schedule.Date == session.Schedule.Date)
                    .Select(s => s.Id)
                    .ToListAsync();

                var siblingWithQuestions = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any())
                    .FirstOrDefaultAsync();

                var legacyQuestions = siblingWithQuestions?.Questions?.ToList() ?? new List<AssessmentQuestion>();

                var userResponses = await _context.UserResponses
                    .Where(r => r.AssessmentSessionId == session.Id)
                    .ToDictionaryAsync(r => r.AssessmentQuestionId, r => r.SelectedOptionId);

                foreach (var question in legacyQuestions)
                {
                    maxScore += question.ScoreValue;
                    if (userResponses.TryGetValue(question.Id, out var selectedOptionId) && selectedOptionId.HasValue)
                    {
                        var selectedOption = question.Options.FirstOrDefault(o => o.Id == selectedOptionId.Value);
                        if (selectedOption != null && selectedOption.IsCorrect)
                            totalScore += question.ScoreValue;
                    }
                }
            }

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
            await NotifyIfGroupCompleted(session);
        }

        /// <summary>
        /// Check if all sessions in an assessment group are Completed (or Cancelled),
        /// and if so, notify HC/Admin users.
        /// </summary>
        private async Task NotifyIfGroupCompleted(AssessmentSession completedSession)
        {
            var allSiblings = await _context.AssessmentSessions
                .Where(s => s.Title == completedSession.Title &&
                            s.Category == completedSession.Category &&
                            s.Schedule.Date == completedSession.Schedule.Date)
                .ToListAsync();

            // Group is "done" when every session is either Completed or Cancelled (no Open/InProgress left)
            if (!allSiblings.All(s => s.Status == "Completed" || s.Status == "Cancelled")) return;

            var hcUsers = await _userManager.GetUsersInRoleAsync("HC");
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var recipientIds = hcUsers.Concat(adminUsers)
                .Select(u => u.Id).Distinct().ToList();

            foreach (var recipientId in recipientIds)
            {
                try
                {
                    await _notificationService.SendAsync(
                        recipientId,
                        "ASMT_ALL_COMPLETED",
                        "Assessment Selesai",
                        $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian",
                        "/CMP/Assessment"
                    );
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
            }
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
            else
            {
                questionCountMap = await _context.AssessmentQuestions
                    .Where(q => siblingIds.Contains(q.AssessmentSessionId))
                    .GroupBy(q => q.AssessmentSessionId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Count());
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

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            // Sanitize title for filename: replace non-alphanumeric with _
            var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
            var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Results.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
        public async Task<IActionResult> AuditLog(int page = 1)
        {
            const int pageSize = 25;

            var totalCount = await _context.AuditLogs.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Clamp page to valid range
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(logs);
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

            if (packages.Count == 1)
                return packages[0].Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();

            int K = packages.Min(p => p.Questions.Count);
            if (K == 0)
                return new List<int>();

            int N = packages.Count;
            int baseCount = K / N;
            int remainder = K % N;

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
            var shuffledIds = new List<int>();
            var orderedQuestions = packages.Select(p => p.Questions.OrderBy(q => q.Order).ToList()).ToList();

            for (int pos = 0; pos < K; pos++)
            {
                int pkgIdx = slots[pos];
                var question = orderedQuestions[pkgIdx][pkgCounter[pkgIdx]];
                pkgCounter[pkgIdx]++;
                shuffledIds.Add(question.Id);
            }

            return shuffledIds;
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
                        AssignmentUnit = r.Mapping.AssignmentUnit ?? ""
                    }).OrderBy(c => c.CoacheeName).ToList()
                })
                .OrderBy(g => g.CoachName)
                .ToList();

            var totalCoachGroups = grouped.Count;
            var totalPages = (int)Math.Ceiling(totalCoachGroups / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var paged = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // 6. Modal data: eligible coaches, eligible coachees, proton tracks
            var activeCoacheeIds = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive)
                .Select(m => m.CoacheeId)
                .Distinct()
                .ToListAsync();

            ViewBag.GroupedCoaches = paged;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCoachGroups;
            ViewBag.ShowAll = showAll;
            ViewBag.SearchTerm = search;
            ViewBag.SectionFilter = section;
            ViewBag.Sections = OrganizationStructure.GetAllSections();
            ViewBag.SectionUnits = OrganizationStructure.SectionUnits;
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

            mapping.IsActive = true;
            mapping.EndDate = null;

            // FIX-01: Only reactivate assignments that were deactivated as part of this mapping's deactivation event.
            // We correlate by DeactivatedAt timestamp (within 5 seconds of mapping.EndDate) to avoid restoring
            // assignments that were independently deactivated for other reasons.
            var deactivationTime = mapping.EndDate; // was EndDate before we cleared it; read below from DB snapshot
            // Re-read EndDate before clearing (mapping.EndDate is now null after line above — capture it first)
            // NOTE: The deactivationTime is captured before clearing EndDate. We need to reload it.
            var mappingEndDate = await _context.CoachCoacheeMappings
                .Where(m => m.Id == id)
                .Select(m => m.EndDate)
                .FirstOrDefaultAsync();
            // mappingEndDate is already null because we set mapping.EndDate = null above and it's tracked.
            // We need the original EndDate — use the one we captured before the change.
            // Since we haven't saved yet, re-read from OriginalValues via EF change tracker.
            var entry = _context.Entry(mapping);
            var originalEndDate = (DateTime?)entry.OriginalValues["EndDate"];

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

            await _auditLog.LogAsync(actor.Id, actor.FullName, "Reactivate",
                $"Reactivated coach-coachee mapping #{id} — {reactivatedCount} ProtonTrackAssignment(s) also reactivated", targetId: id, targetType: "CoachCoacheeMapping");

            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            return Json(new { success = true,
                message = $"Mapping berhasil diaktifkan kembali. {reactivatedCount} track assignment juga diaktifkan kembali.",
                showAssignPrompt = reactivatedCount == 0,
                coacheeName = coacheeUser?.FullName ?? "",
                assignUrl = Url.Action("CoachCoacheeMapping", "Admin") });
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
                var validUnits = OrganizationStructure.GetUnitsForSection(sectionFilter);
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
            ViewBag.AllSections = OrganizationStructure.GetAllSections();
            ViewBag.AllUnits = !string.IsNullOrEmpty(sectionFilter)
                ? OrganizationStructure.GetUnitsForSection(sectionFilter)
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
                var validUnits = OrganizationStructure.GetUnitsForSection(sectionFilter);
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
            var ws = workbook.Worksheets.Add("Pekerja");

            // Header
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Nama";
            ws.Cell(1, 3).Value = "Email";
            ws.Cell(1, 4).Value = "NIP";
            ws.Cell(1, 5).Value = "Jabatan";
            ws.Cell(1, 6).Value = "Bagian";
            ws.Cell(1, 7).Value = "Unit";
            ws.Cell(1, 8).Value = "Status";

            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

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

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Pekerja_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET /Admin/CreateWorker
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult CreateWorker()
        {
            var model = new ManageUserViewModel
            {
                Role = "Coachee"
            };
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

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("CreateWorker validation failed: {Errors}", errors);
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

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("EditWorker validation failed for user {UserId}: {Errors}", model.Id, errors);
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
                var userResponses = await _context.UserResponses
                    .Where(r => userAssessmentIds.Contains(r.AssessmentSessionId))
                    .ToListAsync();
                if (userResponses.Any())
                    _context.UserResponses.RemoveRange(userResponses);

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

            // UserCompetencyLevels (Restrict)
            var competencyLevels = await _context.UserCompetencyLevels
                .Where(c => c.UserId == id)
                .ToListAsync();
            if (competencyLevels.Any())
                _context.UserCompetencyLevels.RemoveRange(competencyLevels);

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

            ws.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "workers_import_template.xlsx");
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

            var results = new List<ImportWorkerResult>();
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();

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

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CoachCoacheeMapping.xlsx");
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
        public async Task<IActionResult> AddTraining()
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

            return View(new CreateTrainingRecordViewModel());
        }

        // POST /Admin/AddTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddTraining(CreateTrainingRecordViewModel model)
        {
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
                return View(model);
            }

            // Handle file upload
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "certificates");
                Directory.CreateDirectory(uploadDir);
                var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(model.CertificateFile.FileName)}";
                var filePath = Path.Combine(uploadDir, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CertificateFile.CopyToAsync(stream);
                }
                sertifikatUrl = $"/uploads/certificates/{safeFileName}";
            }

            var record = new TrainingRecord
            {
                UserId = model.UserId,
                Judul = model.Judul,
                Penyelenggara = model.Penyelenggara,
                Kota = model.Kota,
                Kategori = model.Kategori,
                Tanggal = model.Tanggal,
                TanggalMulai = model.TanggalMulai,
                TanggalSelesai = model.TanggalSelesai,
                Status = model.Status,
                NomorSertifikat = model.NomorSertifikat,
                ValidUntil = model.ValidUntil,
                CertificateType = model.CertificateType,
                SertifikatUrl = sertifikatUrl
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
                Tanggal = record.Tanggal,
                TanggalMulai = record.TanggalMulai,
                TanggalSelesai = record.TanggalSelesai,
                Status = record.Status,
                NomorSertifikat = record.NomorSertifikat,
                ValidUntil = record.ValidUntil,
                CertificateType = record.CertificateType,
                ExistingSertifikatUrl = record.SertifikatUrl,
            };
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

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "certificates");
                Directory.CreateDirectory(uploadDir);
                var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(model.CertificateFile.FileName)}";
                var filePath = Path.Combine(uploadDir, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CertificateFile.CopyToAsync(stream);
                }
                record.SertifikatUrl = $"/uploads/certificates/{safeFileName}";
            }

            record.Judul = model.Judul;
            record.Penyelenggara = model.Penyelenggara;
            record.Kota = model.Kota;
            record.Kategori = model.Kategori;
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

        // --- WORKER TRAINING HELPERS (duplicated from CMPController for AdminController use) ---

        private async Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId)
        {
            var assessments = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .ToListAsync();

            var trainings = await _context.TrainingRecords
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var unified = new List<UnifiedTrainingRecord>();

            unified.AddRange(assessments.Select(a => new UnifiedTrainingRecord
            {
                Date = a.CompletedAt ?? a.Schedule,
                RecordType = "Assessment Online",
                Title = a.Title,
                Score = a.Score,
                IsPassed = a.IsPassed,
                Status = a.IsPassed == true ? "Passed" : "Failed",
                SortPriority = 0
            }));

            unified.AddRange(trainings.Select(t => new UnifiedTrainingRecord
            {
                Date = t.Tanggal,
                RecordType = "Training Manual",
                Title = t.Judul ?? "",
                Penyelenggara = t.Penyelenggara,
                CertificateType = t.CertificateType,
                ValidUntil = t.ValidUntil,
                Status = t.Status,
                SertifikatUrl = t.SertifikatUrl,
                SortPriority = 1,
                TrainingRecordId = t.Id,
                Kategori = t.Kategori,
                Kota = t.Kota,
                NomorSertifikat = t.NomorSertifikat,
                TanggalMulai = t.TanggalMulai,
                TanggalSelesai = t.TanggalSelesai
            }));

            return unified
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.SortPriority)
                .ToList();
        }

        private async Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)> GetAllWorkersHistory()
        {
            var archivedCounts = await _context.AssessmentAttemptHistory
                .GroupBy(h => new { h.UserId, h.Title })
                .Select(g => new { g.Key.UserId, g.Key.Title, Count = g.Count() })
                .ToListAsync();

            var archivedCountLookup = archivedCounts
                .ToDictionary(x => (x.UserId, x.Title), x => x.Count);

            var archivedAttempts = await _context.AssessmentAttemptHistory
                .Include(h => h.User)
                .ToListAsync();

            var assessmentRows = new List<AllWorkersHistoryRow>();

            assessmentRows.AddRange(archivedAttempts.Select(h => new AllWorkersHistoryRow
            {
                WorkerName    = h.User?.FullName ?? h.UserId,
                WorkerNIP     = h.User?.NIP,
                RecordType    = "Assessment Online",
                Title         = h.Title,
                Date          = h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt,
                Score         = h.Score,
                IsPassed      = h.IsPassed,
                AttemptNumber = h.AttemptNumber
            }));

            var currentCompleted = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Status == "Completed")
                .ToListAsync();

            assessmentRows.AddRange(currentCompleted.Select(a =>
            {
                var key = (a.UserId, a.Title ?? "");
                int archived = archivedCountLookup.TryGetValue(key, out var c) ? c : 0;
                return new AllWorkersHistoryRow
                {
                    WorkerName    = a.User?.FullName ?? a.UserId,
                    WorkerNIP     = a.User?.NIP,
                    RecordType    = "Assessment Online",
                    Title         = a.Title ?? "",
                    Date          = a.CompletedAt ?? a.Schedule,
                    Score         = a.Score,
                    IsPassed      = a.IsPassed,
                    AttemptNumber = archived + 1
                };
            }));

            assessmentRows = assessmentRows
                .OrderBy(r => r.Title)
                .ThenByDescending(r => r.Date)
                .ToList();

            var trainings = await _context.TrainingRecords
                .Include(t => t.User)
                .ToListAsync();

            var trainingRows = trainings.Select(t => new AllWorkersHistoryRow
            {
                WorkerName    = t.User?.FullName ?? t.UserId,
                WorkerNIP     = t.User?.NIP,
                RecordType    = "Manual",
                Title         = t.Judul ?? "",
                Date          = t.TanggalMulai ?? t.Tanggal,
                Penyelenggara = t.Penyelenggara
            })
            .OrderByDescending(r => r.Date)
            .ToList();

            return (assessmentRows, trainingRows);
        }

        private async Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null)
        {
            var usersQuery = _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.TrainingRecords)
                .AsQueryable();

            if (!string.IsNullOrEmpty(section))
                usersQuery = usersQuery.Where(u => u.Section == section);

            if (!string.IsNullOrEmpty(unitFilter))
                usersQuery = usersQuery.Where(u => u.Unit == unitFilter);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    (u.NIP != null && u.NIP.Contains(search))
                );
            }

            var users = await usersQuery.ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var passedAssessmentsByUser = await _context.AssessmentSessions
                .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            var passedAssessmentLookup = passedAssessmentsByUser
                .ToDictionary(x => x.UserId, x => x.Count);

            var workerList = new List<WorkerTrainingStatus>();

            foreach (var user in users)
            {
                var trainingRecords = user.TrainingRecords.ToList();
                int completedAssessments = passedAssessmentLookup.TryGetValue(user.Id, out var aCount) ? aCount : 0;

                var totalTrainings = trainingRecords.Count;
                var completedTrainings = trainingRecords.Count(tr =>
                    tr.Status == "Passed" || tr.Status == "Valid"
                );
                var pendingTrainings = trainingRecords.Count(tr =>
                    tr.Status == "Wait Certificate" || tr.Status == "Pending"
                );
                var expiringTrainings = trainingRecords.Count(tr => tr.IsExpiringSoon);

                var worker = new WorkerTrainingStatus
                {
                    WorkerId = user.Id,
                    WorkerName = user.FullName,
                    NIP = user.NIP,
                    Position = user.Position ?? "Staff",
                    Section = user.Section ?? "",
                    Unit = user.Unit ?? "",
                    TotalTrainings = totalTrainings,
                    CompletedTrainings = completedTrainings,
                    PendingTrainings = pendingTrainings,
                    ExpiringSoonTrainings = expiringTrainings,
                    TrainingRecords = trainingRecords,
                    CompletedAssessments = completedAssessments
                };

                if (!string.IsNullOrEmpty(category))
                {
                    bool isCompleted = trainingRecords.Any(r =>
                        !string.IsNullOrEmpty(r.Kategori) &&
                        r.Kategori.Contains(category, StringComparison.OrdinalIgnoreCase) &&
                        (r.Status == "Passed" || r.Status == "Valid" || r.Status == "Permanent")
                    );
                    worker.CompletionPercentage = isCompleted ? 100 : 0;
                }
                else
                {
                    worker.CompletionPercentage = totalTrainings > 0
                        ? (int)((double)completedTrainings / totalTrainings * 100)
                        : 0;
                }

                workerList.Add(worker);
            }

            if (!string.IsNullOrEmpty(statusFilter) && !string.IsNullOrEmpty(category))
            {
                if (statusFilter == "Sudah")
                    workerList = workerList.Where(w => w.CompletionPercentage == 100).ToList();
                else if (statusFilter == "Belum")
                    workerList = workerList.Where(w => w.CompletionPercentage != 100).ToList();
            }

            return workerList;
        }

        #region Question Management (Admin)

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageQuestions(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            return View(assessment);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int has_id, string question_text, List<string> options, int correct_option_index)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(has_id);
            if (assessment == null) return NotFound();

            // Validation
            if (string.IsNullOrWhiteSpace(question_text))
            {
                TempData["Error"] = "Pertanyaan tidak boleh kosong.";
                return RedirectToAction("ManageQuestions", "Admin", new { id = has_id });
            }

            if (options == null || options.Count < 2)
            {
                TempData["Error"] = "Minimal harus ada 2 opsi jawaban.";
                return RedirectToAction("ManageQuestions", "Admin", new { id = has_id });
            }

            if (correct_option_index < 0 || correct_option_index >= options.Count)
            {
                TempData["Error"] = "Index jawaban benar tidak valid.";
                return RedirectToAction("ManageQuestions", "Admin", new { id = has_id });
            }

            var validOptions = options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            if (validOptions.Count < 2)
            {
                TempData["Error"] = "Minimal harus ada 2 opsi jawaban yang terisi.";
                return RedirectToAction("ManageQuestions", "Admin", new { id = has_id });
            }

            var newQuestion = new AssessmentQuestion
            {
                AssessmentSessionId = has_id,
                QuestionText = question_text,
                QuestionType = "MultipleChoice",
                ScoreValue = 10,
                Order = await _context.AssessmentQuestions.CountAsync(q => q.AssessmentSessionId == has_id) + 1
            };

            _context.AssessmentQuestions.Add(newQuestion);
            // Save question first to get its Id, then add options atomically in a single round-trip
            await _context.SaveChangesAsync();

            for (int i = 0; i < options.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(options[i]))
                {
                    _context.AssessmentOptions.Add(new AssessmentOption
                    {
                        AssessmentQuestionId = newQuestion.Id,
                        OptionText = options[i],
                        IsCorrect = (i == correct_option_index)
                    });
                }
            }
            // Save all options in a single round-trip (atomic with question via SaveChangesAsync)
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Roll back orphaned question if options save fails
                _context.AssessmentQuestions.Remove(newQuestion);
                await _context.SaveChangesAsync();
                TempData["Error"] = "Gagal menyimpan opsi jawaban. Pertanyaan dibatalkan.";
                return RedirectToAction("ManageQuestions", "Admin", new { id = has_id });
            }

            return RedirectToAction("ManageQuestions", "Admin", new { id = has_id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.AssessmentQuestions.FindAsync(id);
            if (question == null) return NotFound();

            int assessmentId = question.AssessmentSessionId;
            string questionText = question.QuestionText;

            // Delete UserResponses first: UserResponse.AssessmentQuestionId has Restrict FK
            var responses = await _context.UserResponses
                .Where(r => r.AssessmentQuestionId == id)
                .ToListAsync();
            if (responses.Any())
                _context.UserResponses.RemoveRange(responses);

            _context.AssessmentQuestions.Remove(question);
            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var deleteUser = await _userManager.GetUserAsync(User);
                var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP) ? (deleteUser?.FullName ?? "Unknown") : $"{deleteUser.NIP} - {deleteUser.FullName}";
                await _auditLog.LogAsync(
                    deleteUser?.Id ?? "",
                    deleteActorName,
                    "DeleteQuestion",
                    $"Deleted question '{questionText?.Substring(0, Math.Min(50, questionText?.Length ?? 0))}...' [ID={id}] from assessment {assessmentId}",
                    id,
                    "AssessmentQuestion");
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "Audit log write failed for DeleteQuestion {Id}", id);
            }

            return RedirectToAction("ManageQuestions", "Admin", new { id = assessmentId });
        }

        #endregion

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

            var headers = new[] { "Question", "Option A", "Option B", "Option C", "Option D", "Correct", "Sub Kompetensi" };
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
                "Sub Kompetensi x.x"
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

            ws.Cell(4, 1).Value = "Kolom Sub Kompetensi: opsional, isi nama sub-kompetensi. Kosongkan jika tidak ada.";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;

            ws.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "question_import_template.xlsx");
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

            List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct, string? SubCompetency)> rows;
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
                    SubCompetency = NormalizeSubCompetency(rawSubComp),
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

            // Cross-package SubCompetency mismatch warning
            if (added > 0 && targetSession != null)
            {
                var currentSubComps = rows
                    .Select(r => NormalizeSubCompetency(r.SubCompetency))
                    .Where(s => s != null)
                    .Distinct()
                    .ToHashSet();

                if (currentSubComps.Any())
                {
                    var siblingSubComps = await _context.PackageQuestions
                        .Where(pq => pq.AssessmentPackage!.AssessmentSessionId == pkg.AssessmentSessionId
                                  && pq.AssessmentPackageId != packageId
                                  && pq.SubCompetency != null)
                        .Select(pq => pq.SubCompetency!)
                        .Distinct()
                        .ToListAsync();

                    if (siblingSubComps.Any() && !currentSubComps.SetEquals(siblingSubComps.ToHashSet()))
                    {
                        TempData["Warning"] = "Sub Kompetensi pada paket ini berbeda dari paket lain dalam assessment yang sama. Pastikan konsistensi antar paket.";
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

        private static string? NormalizeSubCompetency(string? raw)
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

        #region Maintenance

        /// <summary>
        /// CLN-01: Manual trigger to deactivate duplicate active ProtonTrackAssignments.
        /// Returns JSON { cleaned: N }.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupDuplicateAssignments()
        {
            var cleaned = await SeedData.DeduplicateProtonTrackAssignments(_context);
            return Json(new { cleaned });
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

            var events = await _context.ExamActivityLogs
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.Timestamp)
                .Select(l => new
                {
                    l.EventType,
                    l.Detail,
                    Timestamp = l.Timestamp.ToString("HH:mm:ss")
                })
                .ToListAsync();

            var answeredCount = await _context.UserResponses
                .CountAsync(r => r.AssessmentSessionId == sessionId);

            // Also count package responses if the session uses a package
            var packageAnsweredCount = await _context.PackageUserResponses
                .CountAsync(r => r.AssessmentSessionId == sessionId);

            int totalAnswered = answeredCount + packageAnsweredCount;

            var lastEventTime = await _context.ExamActivityLogs
                .Where(l => l.SessionId == sessionId)
                .MaxAsync(l => (DateTime?)l.Timestamp);

            int? timeSpentSeconds = null;
            if (session.StartedAt.HasValue)
            {
                var endTime = session.CompletedAt ?? lastEventTime ?? DateTime.UtcNow;
                timeSpentSeconds = (int)(endTime - session.StartedAt.Value).TotalSeconds;
            }

            var disconnectCount = events.Count(e => e.EventType == "disconnected");

            var summary = new
            {
                answeredCount = totalAnswered,
                disconnectCount,
                timeSpentSeconds,
                startedAt = session.StartedAt?.ToString("HH:mm:ss"),
                completedAt = session.CompletedAt?.ToString("HH:mm:ss")
            };

            return Json(new { summary, events });
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
