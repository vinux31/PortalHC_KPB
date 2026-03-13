using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using HcPortal.Models.Competency;
// PositionTargetHelper removed in Phase 90 (KKJ tables dropped)
using System.Text.Json;
using HcPortal.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using HcPortal.Hubs;

namespace HcPortal.Controllers
{
    [Authorize]
    public class CMPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CMPController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<AssessmentHub> _hubContext;

        public CMPController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            AuditLogService auditLog,
            IMemoryCache cache,
            ILogger<CMPController> logger,
            INotificationService notificationService,
            IHubContext<AssessmentHub> hubContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
            _auditLog = auditLog;
            _cache = cache;
            _logger = logger;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- HALAMAN 1: SUSUNAN KKJ (FILE VIEW) ---
        // GET /CMP/Kkj?section={bagianName}
        [HttpGet]
        public async Task<IActionResult> Kkj(string? section)
        {
            ViewData["Title"] = "KKJ Matrix";

            var currentUser = await _userManager.GetUserAsync(User) as ApplicationUser;
            var userLevel = currentUser?.RoleLevel ?? 6;

            // Role-based bagian filtering: L1-L4 see all, L5-L6 see own bagian only
            IQueryable<KkjBagian> bagiansQuery = _context.KkjBagians;
            if (userLevel >= 5 && currentUser?.Section != null)
            {
                var sectionFiltered = _context.KkjBagians
                    .Where(b => b.Name.ToLower() == currentUser.Section.ToLower());

                // Only apply filter if it matches at least one bagian; otherwise show all (safe fallback)
                if (await sectionFiltered.AnyAsync())
                {
                    bagiansQuery = sectionFiltered;
                }
            }

            var availableBagians = await bagiansQuery.OrderBy(b => b.DisplayOrder).ToListAsync();
            ViewBag.AllBagians = availableBagians;

            if (!availableBagians.Any())
            {
                ViewBag.SelectedBagian = "";
                ViewBag.Files = new List<KkjFile>();
                ViewBag.SelectedBagianRecord = null;
                return View();
            }

            // Select bagian from URL param or default to first available
            var selectedBagian = availableBagians.FirstOrDefault(b => b.Name == section)
                              ?? availableBagians.First();

            // Validate section param for L5/L6: if section provided but not in their accessible list, use first
            if (userLevel >= 5 && section != null && !availableBagians.Any(b => b.Name == section))
            {
                // Safe to use .First() here because we already checked .Any() at line 70
                selectedBagian = availableBagians.First();
            }

            // Load active files for selected bagian
            var files = await _context.KkjFiles
                .Where(f => f.BagianId == selectedBagian.Id && !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            ViewBag.SelectedBagian = selectedBagian.Name;
            ViewBag.SelectedBagianRecord = selectedBagian;
            ViewBag.Files = files;

            return View();
        }

        // --- HALAMAN 2: MAPPING KKJ - CPDP ---
        public async Task<IActionResult> Mapping()
        {
            // Query all bagians ordered by DisplayOrder
            var allBagians = await _context.KkjBagians
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // Query all active (non-archived) CPDP files, grouped by BagianId
            var allFiles = await _context.CpdpFiles
                .Where(f => !f.IsArchived)
                .OrderBy(f => f.UploadedAt)
                .ToListAsync();

            var filesByBagian = allFiles
                .GroupBy(f => f.BagianId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Role-based tab filtering
            var user = await _userManager.GetUserAsync(User);
            var filteredBagians = allBagians;

            if (user != null && user.RoleLevel >= 5 && !string.IsNullOrEmpty(user.Section))
            {
                var sectionFiltered = allBagians
                    .Where(b => b.Name.ToLower() == user.Section.ToLower())
                    .ToList();

                // Only apply filter if it matches at least one bagian; otherwise show all (safe fallback)
                if (sectionFiltered.Count > 0)
                {
                    filteredBagians = sectionFiltered;
                }
            }

            var selectedBagianId = filteredBagians.FirstOrDefault()?.Id ?? 0;

            ViewBag.Bagians = filteredBagians;
            ViewBag.FilesByBagian = filesByBagian;
            ViewBag.SelectedBagianId = selectedBagianId;

            return View();
        }
        // --- HALAMAN 3: MY ASSESSMENTS (personal view only) ---
        public async Task<IActionResult> Assessment(string? search, int page = 1, int pageSize = 20)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;  // Max 100 per page

            // Get current user
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? "";

            // ========== PERSONAL VIEW: worker's own assessments ==========
            var query = _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.UserId == userId);

            // Workers see only actionable assessments — Completed lives in Training Records (/CMP/Records)
            // InProgress is included so workers can resume mid-exam sessions (Phase 42)
            query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress");

            // ========== SEARCH FILTER ==========
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(lowerSearch) ||
                    a.Category.ToLower().Contains(lowerSearch) ||
                    (a.User != null && (
                        a.User.FullName.ToLower().Contains(lowerSearch) ||
                        (a.User.NIP != null && a.User.NIP.Contains(lowerSearch))
                    ))
                );
            }

            ViewBag.SearchTerm = search;

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Execute Query with pagination
            var exams = await query
                .OrderByDescending(a => a.Schedule)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Auto-transition display: show Upcoming as Open when scheduled date+time has arrived in WIB (display-only, no SaveChangesAsync)
            var nowWib = DateTime.UtcNow.AddHours(7);
            foreach (var exam in exams)
            {
                if (exam.Status == "Upcoming" && exam.Schedule <= nowWib)
                    exam.Status = "Open";
            }

            // Pagination info for view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            // ========== RIWAYAT UJIAN: completed assessment history for worker ==========
            var completedHistory = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && (a.Status == "Completed" || a.Status == "Abandoned"))
                .OrderByDescending(a => a.CompletedAt ?? a.UpdatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.CompletedAt,
                    a.Score,
                    a.IsPassed,
                    a.Status
                })
                .ToListAsync();

            ViewBag.CompletedHistory = completedHistory;

            return View(exams);
        }

        // --- SAVE ANSWER (incremental — called by worker JS on each radio change) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer(int sessionId, int questionId, int optionId)
        {
            // Validate parameters
            if (sessionId <= 0 || questionId <= 0 || optionId <= 0)
            {
                return Json(new { success = false, error = "Invalid parameters" });
            }

            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only the session owner may save answers
            if (session.UserId != user.Id)
                return Json(new { success = false, error = "Unauthorized" });

            // Session must still be in progress
            if (session.Status == "Completed" || session.Status == "Abandoned" || session.Status == "Cancelled")
                return Json(new { success = false, error = "Session already closed" });

            // Atomic upsert: update existing row, or insert if none exists
            var updatedCount = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.PackageOptionId, optionId)
                    .SetProperty(r => r.SubmittedAt, DateTime.UtcNow)
                );

            if (updatedCount == 0)
            {
                _context.PackageUserResponses.Add(new PackageUserResponse
                {
                    AssessmentSessionId = sessionId,
                    PackageQuestionId = questionId,
                    PackageOptionId = optionId,
                    SubmittedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            // SignalR push: notify HC monitor group of progress update (DB write above is always first)
            var answeredCount = await _context.PackageUserResponses
                .CountAsync(r => r.AssessmentSessionId == sessionId);

            var assignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
            int totalQuestions = assignment?.GetShuffledQuestionIds().Count ?? 0;

            var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
            var progressPayload = new { sessionId, progress = answeredCount, totalQuestions };
            await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("progressUpdate", progressPayload);
            await _hubContext.Clients.User(session.UserId).SendAsync("progressUpdate", progressPayload);

            return Json(new { success = true });
        }

        // --- SAVE LEGACY ANSWER (auto-save for legacy exam path → UserResponse) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLegacyAnswer(int sessionId, int questionId, int optionId)
        {
            // Validate parameters
            if (sessionId <= 0 || questionId <= 0 || optionId <= 0)
            {
                return Json(new { success = false, error = "Invalid parameters" });
            }

            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only the session owner may save answers
            if (session.UserId != user.Id)
                return Json(new { success = false, error = "Unauthorized" });

            // Session must still be in progress
            if (session.Status == "Completed" || session.Status == "Abandoned" || session.Status == "Cancelled")
                return Json(new { success = false, error = "Session already closed" });

            // Atomic upsert: update existing row, or insert if none exists
            var updatedCount = await _context.UserResponses
                .Where(r => r.AssessmentSessionId == sessionId && r.AssessmentQuestionId == questionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.SelectedOptionId, optionId)
                );

            if (updatedCount == 0)
            {
                _context.UserResponses.Add(new UserResponse
                {
                    AssessmentSessionId = sessionId,
                    AssessmentQuestionId = questionId,
                    SelectedOptionId = optionId
                });
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // --- CHECK EXAM STATUS (polled by worker JS every 10s to detect early close) ---
        [HttpGet]
        public async Task<IActionResult> CheckExamStatus(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            // Only the session owner may poll status
            if (session.UserId != user.Id)
                return Json(new { closed = false });

            string cacheKey = $"exam-status-{sessionId}";

            // Return cached result if available (5-second TTL — reduces DB load for concurrent workers)
            if (_cache.TryGetValue(cacheKey, out (bool closed, string url) hit))
                return Json(new { closed = hit.closed, redirectUrl = hit.url });

            bool isClosed = false;

            // Closed if ExamWindowCloseDate has been set and is in the past
            if (session.ExamWindowCloseDate.HasValue && DateTime.UtcNow > session.ExamWindowCloseDate.Value)
                isClosed = true;

            // Closed if session is already Completed (auto-graded by AkhiriUjian/AkhiriSemuaUjian or SubmitExam)
            if (session.Status == "Completed")
                isClosed = true;

            // Cancelled by AkhiriSemuaUjian (not-started worker) — redirect to Assessment page, not Results
            if (session.Status == "Cancelled")
                isClosed = true;

            string redirectUrl = isClosed
                ? (session.Status == "Cancelled"
                    ? (Url.Action("Assessment") ?? "/CMP/Assessment")
                    : (Url.Action("Results", new { id = sessionId }) ?? "/CMP/Assessment"))
                : "";

            // Cache result for 5 seconds (ownership already verified above)
            _cache.Set(cacheKey, (isClosed, redirectUrl), TimeSpan.FromSeconds(5));

            return Json(new { closed = isClosed, redirectUrl });
        }

        // --- UPDATE SESSION PROGRESS (saves elapsed time + current page for resume) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSessionProgress(int sessionId, int elapsedSeconds, int currentPage)
        {
            // Validate parameters
            if (sessionId <= 0 || elapsedSeconds < 0 || currentPage < 1)
            {
                return Json(new { success = false, error = "Invalid parameters" });
            }

            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Session ownership check (same pattern as SaveAnswer)
            if (session.UserId != user.Id)
                return Json(new { success = false, error = "Unauthorized" });

            // Skip update if session already closed
            if (session.Status == "Completed" || session.Status == "Abandoned")
                return Json(new { success = false, error = "Session already closed" });

            // Atomic update of elapsed time and last active page
            var updated = await _context.AssessmentSessions
                .Where(s => s.Id == sessionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.ElapsedSeconds, elapsedSeconds)
                    .SetProperty(r => r.LastActivePage, currentPage)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
                );

            if (updated == 0)
                return Json(new { success = false, error = "Update failed" });

            return Json(new { success = true });
        }


        // HALAMAN 4: CAPABILITY BUILDING RECORDS
        public async Task<IActionResult> Records(string? section, string? unit, string? category, string? search, string? statusFilter, string? isFiltered)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var unified = await GetUnifiedRecords(user.Id);

            // Phase 104: Get worker list for Team View tab (only for users level 1-4)
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            var roleLevel = UserRoles.GetRoleLevel(userRole ?? "");

            if (roleLevel <= 4)
            {
                // Scope enforcement: Level 4 (SectionHead, SrSupervisor) locked to their own section
                string? sectionFilter = null;
                if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
                {
                    sectionFilter = user.Section;
                }

                var workerList = await GetWorkersInSection(sectionFilter);
                ViewData["WorkerList"] = workerList;
            }

            return View("Records", unified);
        }

        // Phase 104: Team View tab for monitoring team members' training & assessment compliance
        public async Task<IActionResult> RecordsTeam()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            var roleLevel = UserRoles.GetRoleLevel(userRole ?? "");

            // Role-based access control: Level 5-6 (Coach, Supervisor, Coachee) forbidden
            if (roleLevel >= 5)
            {
                return Forbid();
            }

            // Scope enforcement: Level 4 (SectionHead, SrSupervisor) locked to their own section
            string? sectionFilter = null;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            // Level 1-3: No section restriction (full access to all sections/units)
            var workerList = await GetWorkersInSection(sectionFilter);

            return View("RecordsTeam", workerList);
        }

        // Phase 104: Worker Detail page showing unified assessment + training history
        public async Task<IActionResult> RecordsWorkerDetail(string workerId, string? section, string? unit, string? category, string? status, string? search)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userRoles = await _userManager.GetRolesAsync(user);
            var roleLevel = UserRoles.GetRoleLevel(userRoles.FirstOrDefault() ?? "");

            // Own records: always allowed
            if (workerId != user.Id)
            {
                // Level 5-6 (Coach, Coachee): cannot view other workers
                if (roleLevel >= 5) return Forbid();
                // Level 4 (SectionHead, SrSupervisor): section-scoped
                if (roleLevel == 4)
                {
                    var targetUser = await _context.Users.FindAsync(workerId);
                    if (targetUser == null || targetUser.Section != user.Section)
                        return Forbid();
                }
                // Level 1-3: full access
            }

            var worker = await _userManager.FindByIdAsync(workerId);
            if (worker == null)
            {
                return NotFound();
            }

            var unifiedRecords = await GetUnifiedRecords(workerId);

            var viewModel = new
            {
                WorkerName = worker.FullName ?? worker.Id,
                NIP = worker.NIP,
                Position = worker.Position ?? "—",
                Section = worker.Section ?? "—",
                UnifiedRecords = unifiedRecords,
                FilterState = new
                {
                    Section = section,
                    Unit = unit,
                    Category = category,
                    Status = status,
                    Search = search
                }
            };

            return View(viewModel);
        }

        // Phase 20: HC Edit Training Record — POST only (no GET; modal is pre-populated inline via Razor in WorkerDetail.cshtml)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainingRecord(EditTrainingRecordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Validate file if provided
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Hanya file PDF, JPG, dan PNG yang diperbolehkan.";
                    return RedirectToAction("ManageAssessment", "Admin", new { tab = "training" });
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Ukuran file maksimal 10MB.";
                    return RedirectToAction("ManageAssessment", "Admin", new { tab = "training" });
                }
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("ManageAssessment", "Admin", new { tab = "training" });
            }

            var record = await _context.TrainingRecords.FindAsync(model.Id);
            if (record == null) return NotFound();

            // Handle file upload — replace old file if new file provided
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(record.SertifikatUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // Save new file with timestamp prefix
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "certificates");
                Directory.CreateDirectory(uploadDir);
                var originalExt = Path.GetExtension(model.CertificateFile.FileName);
                var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{originalExt}";
                var filePath = Path.Combine(uploadDir, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CertificateFile.CopyToAsync(stream);
                }
                record.SertifikatUrl = $"/uploads/certificates/{safeFileName}";
            }
            // Else: keep record.SertifikatUrl unchanged

            // Update all editable fields — UserId (worker) is intentionally NOT updated
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

            TempData["Success"] = "Training record berhasil diperbarui.";
            return RedirectToAction("ManageAssessment", "Admin", new { tab = "training" });
        }

        // Phase 20: HC Delete Training Record — POST only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainingRecord(int id, string workerId, string workerName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();

            // Delete certificate file from disk if it exists
            if (!string.IsNullOrEmpty(record.SertifikatUrl))
            {
                var path = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            string trainingTitle = record.Judul ?? "Unknown";
            string deletedWorkerName = record.UserId;

            _context.TrainingRecords.Remove(record);
            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var deleteActorName = string.IsNullOrWhiteSpace(user?.NIP) ? (user?.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
                await _auditLog.LogAsync(
                    user?.Id ?? "",
                    deleteActorName,
                    "DeleteTrainingRecord",
                    $"Deleted training record '{trainingTitle}' for worker '{deletedWorkerName}' [ID={id}]",
                    id,
                    "TrainingRecord");
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "Audit log write failed for DeleteTrainingRecord {Id}", id);
            }

            TempData["Success"] = "Training record berhasil dihapus.";
            return RedirectToAction("ManageAssessment", "Admin", new { tab = "training" });
        }

        // Helper method: Get personal training records for Coach/Coachee
        private async Task<List<TrainingRecord>> GetPersonalTrainingRecords(string userId)
        {
            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var trainingRecords = await _context.TrainingRecords
                .Where(tr => tr.UserId == userId)
                .OrderByDescending(tr => tr.Tanggal)
                .ToListAsync();

            return trainingRecords;
        }

        // Phase 10: unified records helper — merges AssessmentSessions + TrainingRecords
        private async Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId)
        {
            // Query 1: Completed assessments only
            var assessments = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .ToListAsync();

            // Query 2: All training records
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
                SortPriority = 0,
                AssessmentSessionId = a.Id,
                GenerateCertificate = a.GenerateCertificate
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

        // Phase 46: Split all-workers history into assessment and training — supports Attempt # computation
        private async Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)> GetAllWorkersHistory()
        {
            // --- Assessment history ---

            // Batch count archived attempts per user+title to avoid N+1
            var archivedCounts = await _context.AssessmentAttemptHistory
                .GroupBy(h => new { h.UserId, h.Title })
                .Select(g => new { g.Key.UserId, g.Key.Title, Count = g.Count() })
                .ToListAsync();

            var archivedCountLookup = archivedCounts
                .ToDictionary(x => (x.UserId, x.Title), x => x.Count);

            // Query 1: Archived attempts (AttemptNumber already stored)
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

            // Query 2: Current completed sessions (Attempt # = archived count for that user+title + 1)
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

            // Sort: grouped by title then date descending
            assessmentRows = assessmentRows
                .OrderBy(r => r.Title)
                .ThenByDescending(r => r.Date)
                .ToList();

            // --- Training history ---
            var trainings = await _context.TrainingRecords
                .Include(t => t.User)
                .ToListAsync();

            var trainingRows = trainings.Select(t => new AllWorkersHistoryRow
            {
                WorkerName    = t.User?.FullName ?? t.UserId,
                WorkerNIP     = t.User?.NIP,
                RecordType    = "Manual",
                Title         = t.Judul ?? "",
                // PITFALL: TanggalMulai is nullable — must coalesce to Tanggal
                Date          = t.TanggalMulai ?? t.Tanggal,
                Penyelenggara = t.Penyelenggara
            })
            .OrderByDescending(r => r.Date)
            .ToList();

            return (assessmentRows, trainingRows);
        }

        // Helper method: Get all workers in a section (with optional filters)
        private async Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null)
        {
            // ✅ QUERY USERS FROM DATABASE WITH TRAINING RECORDS (Fix N+1)
            var usersQuery = _context.Users
                .Include(u => u.TrainingRecords)  // Load related data in single query
                .AsQueryable();

            // 0. FILTER BY SECTION
            if (!string.IsNullOrEmpty(section))
            {
                usersQuery = usersQuery.Where(u => u.Section == section);
            }

            // 0b. FILTER BY UNIT
            if (!string.IsNullOrEmpty(unitFilter))
            {
                usersQuery = usersQuery.Where(u => u.Unit == unitFilter);
            }

            // 1. FILTER BY SEARCH (Name or NIP)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    (u.NIP != null && u.NIP.Contains(search))
                );
            }

            var users = await usersQuery.ToListAsync();

            // Phase 10: Batch query — passed assessments per user (avoids N+1)
            var userIds = users.Select(u => u.Id).ToList();
            var passedAssessmentsByUser = await _context.AssessmentSessions
                .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            var passedAssessmentLookup = passedAssessmentsByUser
                .ToDictionary(x => x.UserId, x => x.Count);

            // 2. BUILD WORKER STATUS LIST WITH TRAINING STATISTICS
            var workerList = new List<WorkerTrainingStatus>();

            foreach (var user in users)
            {
                // Training records already loaded via Include
                var trainingRecords = user.TrainingRecords.ToList();

                // Phase 10: assessments counted from batch lookup
                int completedAssessments = passedAssessmentLookup.TryGetValue(user.Id, out var aCount) ? aCount : 0;

                // Calculate statistics
                var totalTrainings = trainingRecords.Count;
                // Phase 10: only Passed|Valid count — "Permanent" removed per phase decision
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
                    // Phase 10: combined completion count
                    CompletedAssessments = completedAssessments
                };
                
                // Calculate category-specific status if category filter is applied
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
                    // Calculate overall completion percentage
                    worker.CompletionPercentage = totalTrainings > 0 
                        ? (int)((double)completedTrainings / totalTrainings * 100) 
                        : 0;
                }
                
                workerList.Add(worker);
            }
            
            // 3. FILTER BY STATUS (Sudah/Belum) - Applied AFTER status calculation
            if (!string.IsNullOrEmpty(statusFilter) && !string.IsNullOrEmpty(category))
            {
                if (statusFilter == "Sudah")
                {
                    workerList = workerList.Where(w => w.CompletionPercentage == 100).ToList();
                }
                else if (statusFilter == "Belum")
                {
                    workerList = workerList.Where(w => w.CompletionPercentage != 100).ToList();
                }
            }
            
            return workerList;
        }
        

        // API: Verify Token for Assessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyToken(int id, string token)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                return Json(new { success = false, message = "Assessment not found." });
            }

            // Authorization: only owner, Admin, or HC can verify
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
            {
                return Json(new { success = false, message = "You are not authorized to access this assessment." });
            }

            if (!assessment.IsTokenRequired)
            {
                // If token not required, just success
                TempData[$"TokenVerified_{assessment.Id}"] = true;
                return Json(new { success = true, redirectUrl = Url.Action("StartExam", new { id = assessment.Id }) });
            }

            if (string.IsNullOrEmpty(token) || assessment.AccessToken != token.ToUpper())
            {
                return Json(new { success = false, message = "Invalid Token. Please check and try again." });
            }

            // Token Valid -> Redirect to Exam
            TempData[$"TokenVerified_{assessment.Id}"] = true;
            return Json(new { success = true, redirectUrl = Url.Action("StartExam", new { id = assessment.Id }) });
        }

        // --- HALAMAN EXAM SKELETON ---
        [HttpGet]
        public async Task<IActionResult> StartExam(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();

            // Auto-transition: Upcoming → Open when scheduled date+time has arrived in WIB (persisted to DB)
            if (assessment.Status == "Upcoming" && assessment.Schedule <= DateTime.UtcNow.AddHours(7))
            {
                assessment.Status = "Open";
                assessment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Time gate: block access if assessment is still Upcoming (scheduled time not yet reached)
            if (assessment.Status == "Upcoming")
            {
                TempData["Error"] = "Ujian belum dibuka. Silakan kembali setelah waktu ujian dimulai.";
                return RedirectToAction("Assessment");
            }

            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "This assessment has already been completed.";
                return RedirectToAction("Assessment");
            }

            // Enforce token requirement — workers must verify token via Assessment lobby first (SEC-01)
            // HC and Admin bypass: they are not exam takers; they may access StartExam for debugging/monitoring
            // InProgress sessions bypass: token was checked on first entry; reload must not block the worker
            if (assessment.IsTokenRequired && assessment.UserId == user.Id && assessment.StartedAt == null)
            {
                var tokenVerified = TempData.Peek($"TokenVerified_{id}");
                if (tokenVerified == null)
                {
                    TempData["Error"] = "Ujian ini membutuhkan token akses. Silakan masukkan token terlebih dahulu.";
                    return RedirectToAction("Assessment");
                }
            }

            // Enforce exam window close date (LIFE-02 / DATA-03)
            if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow > assessment.ExamWindowCloseDate.Value)
            {
                TempData["Error"] = "Ujian sudah ditutup. Waktu ujian telah berakhir.";
                return RedirectToAction("Assessment");
            }

            // Block re-entry of Abandoned sessions — worker must contact HC for Reset (LIFE-02)
            if (assessment.Status == "Abandoned")
            {
                TempData["Error"] = "Ujian Anda sebelumnya telah dibatalkan. Hubungi HC untuk mengulang.";
                return RedirectToAction("Assessment");
            }

            // Mark InProgress on first load only (idempotent — skip if already started)
            bool justStarted = assessment.StartedAt == null;
            if (justStarted)
            {
                assessment.Status = "InProgress";
                assessment.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // SignalR push: notify HC monitor group that worker started (only on first entry)
            if (justStarted)
            {
                var startBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
                await _hubContext.Clients.Group($"monitor-{startBatchKey}").SendAsync("workerStarted",
                    new { sessionId = assessment.Id, workerName = user.FullName, status = "InProgress" });

                // Activity log: record exam start (fire-and-forget — must never break exam flow)
                _ = LogActivityAsync(assessment.Id, "started");
            }

            // Packages are attached to the representative session (the one HC used when clicking "Packages"),
            // so search across all sibling sessions (same Title + Category + Schedule.Date).
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

            PackageExamViewModel vm;

            if (packages.Any())
            {
                // ---- PACKAGE PATH ----

                // Check for existing assignment (idempotent — resume)
                var assignment = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

                if (assignment == null)
                {
                    var rng = Random.Shared;

                    // Build cross-package ShuffledQuestionIds (per user decision: slot-list algorithm)
                    var shuffledIds = BuildCrossPackageAssignment(packages, rng);

                    // Build option shuffle: per question, randomize A/B/C/D option order
                    // Stored as Dictionary<questionId, List<optionId>> serialized to JSON
                    var optionShuffleDict = new Dictionary<int, List<int>>();
                    var questionsForOptionShuffle = packages.SelectMany(p => p.Questions).ToList();
                    foreach (var q in questionsForOptionShuffle)
                    {
                        var optionIds = q.Options.Select(o => o.Id).ToList();
                        Shuffle(optionIds, rng);
                        optionShuffleDict[q.Id] = optionIds;
                    }

                    // Sentinel: store first package ID (no schema change — AssessmentPackageId still required by FK)
                    var sentinelPackage = packages.First();

                    assignment = new UserPackageAssignment
                    {
                        AssessmentSessionId = id,
                        AssessmentPackageId = sentinelPackage.Id,  // sentinel per discretion decision
                        UserId = user.Id,
                        ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
                        ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict)
                    };
                    // Record question count for stale-question detection on resume (RESUME-03 safety net)
                    assignment.SavedQuestionCount = shuffledIds.Count;
                    _context.UserPackageAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                // For cross-package assignments, SavedQuestionCount = shuffledIds.Count at time of creation
                // Compare against re-derived count: minimum question count across packages (safety fallback)
                int currentQuestionCount = packages.Min(p => p.Questions.Count) == 0
                    ? 0
                    : (packages.Count == 1 ? packages[0].Questions.Count : packages.Min(p => p.Questions.Count));

                // Stale question set check: compare count at session start vs. now
                // HC cannot normally edit questions once a session is active (existing guard), but this is a defensive safety net
                if (assessment.StartedAt != null && assignment.SavedQuestionCount.HasValue &&
                    assignment.SavedQuestionCount.Value != currentQuestionCount)
                {
                    // Clear saved progress so worker gets a clean restart when HC resets
                    await _context.AssessmentSessions
                        .Where(s => s.Id == id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.ElapsedSeconds, 0)
                            .SetProperty(r => r.LastActivePage, (int?)null)
                        );

                    TempData["Error"] = "Soal ujian telah berubah. Hubungi HC untuk mengatur ulang ujian Anda.";
                    return RedirectToAction("Assessment");
                }

                // Build ViewModel in shuffled order
                var shuffledQuestionIds = assignment.GetShuffledQuestionIds();

                // Cross-package: build lookup across all packages (ShuffledQuestionIds may reference any package)
                var allPackageQuestions = packages.SelectMany(p => p.Questions).ToDictionary(q => q.Id);

                var examQuestions = new List<ExamQuestionItem>();
                int displayNum = 1;
                foreach (var qId in shuffledQuestionIds)
                {
                    if (!allPackageQuestions.TryGetValue(qId, out var q)) continue;

                    // Options in original DB order — option shuffle removed per user decision
                    var opts = q.Options.OrderBy(o => o.Id).Select(o => new ExamOptionItem
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText
                    }).ToList();

                    examQuestions.Add(new ExamQuestionItem
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        DisplayNumber = displayNum++,
                        Options = opts
                    });
                }

                vm = new PackageExamViewModel
                {
                    AssessmentSessionId = id,
                    Title = assessment.Title,
                    DurationMinutes = assessment.DurationMinutes,
                    HasPackages = true,
                    AssignmentId = assignment.Id,
                    Questions = examQuestions
                };

                // Resume state: set ViewBag flags for frontend
                bool isResume = assessment.StartedAt != null;
                int durationSeconds = assessment.DurationMinutes * 60;
                int elapsedSec = assessment.ElapsedSeconds;
                int remainingSeconds = durationSeconds - elapsedSec;

                ViewBag.IsResume = isResume;
                ViewBag.LastActivePage = assessment.LastActivePage ?? 0;
                ViewBag.ElapsedSeconds = elapsedSec;
                ViewBag.RemainingSeconds = remainingSeconds;
                ViewBag.ExamExpired = isResume && remainingSeconds <= 0;

                // Load previously saved answers for pre-population (package path)
                if (isResume)
                {
                    var savedAnswers = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == id)
                        .ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId ?? 0);
                    ViewBag.SavedAnswers = System.Text.Json.JsonSerializer.Serialize(savedAnswers);
                }
                else
                {
                    ViewBag.SavedAnswers = "{}";
                }

                // Parse option shuffle for view rendering (from existing assignment)
                var optionShuffleRaw = assignment?.ShuffledOptionIdsPerQuestion ?? "{}";
                var parsedOptionShuffle = new Dictionary<int, List<int>>();
                if (!string.IsNullOrEmpty(optionShuffleRaw) && optionShuffleRaw != "{}")
                {
                    try {
                        parsedOptionShuffle = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(optionShuffleRaw)
                                              ?? new Dictionary<int, List<int>>();
                    } catch (Exception ex) {
                        var _logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
                        _logger.LogWarning(ex, "Failed to deserialize option shuffle JSON for session {SessionId}, using default order", id);
                    }
                }
                ViewBag.OptionShuffle = parsedOptionShuffle;
            }
            else
            {
                // ---- LEGACY PATH: no packages, use old AssessmentQuestion/Option ----
                var sessionWithQuestions = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any())
                    .FirstOrDefaultAsync();

                var legacyQuestions = sessionWithQuestions?.Questions
                    .OrderBy(q => q.Order)
                    .Select((q, i) => new ExamQuestionItem
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        DisplayNumber = i + 1,
                        Options = q.Options.Select(o => new ExamOptionItem
                        {
                            OptionId = o.Id,
                            OptionText = o.OptionText
                        }).ToList()
                    }).ToList() ?? new List<ExamQuestionItem>();

                vm = new PackageExamViewModel
                {
                    AssessmentSessionId = id,
                    Title = assessment.Title,
                    DurationMinutes = assessment.DurationMinutes,
                    HasPackages = false,
                    AssignmentId = null,
                    Questions = legacyQuestions
                };

                // Resume state for legacy path
                bool isResumeLegacy = assessment.StartedAt != null;
                int durationSecondsLegacy = assessment.DurationMinutes * 60;
                int elapsedSecLegacy = assessment.ElapsedSeconds;
                int remainingSecondsLegacy = durationSecondsLegacy - elapsedSecLegacy;

                ViewBag.IsResume = isResumeLegacy;
                ViewBag.LastActivePage = assessment.LastActivePage ?? 0;
                ViewBag.ElapsedSeconds = elapsedSecLegacy;
                ViewBag.RemainingSeconds = remainingSecondsLegacy;
                ViewBag.ExamExpired = isResumeLegacy && remainingSecondsLegacy <= 0;

                // Load previously saved answers for pre-population (legacy path)
                if (isResumeLegacy)
                {
                    var savedAnswersLegacy = await _context.UserResponses
                        .Where(r => r.AssessmentSessionId == id)
                        .ToDictionaryAsync(r => r.AssessmentQuestionId, r => r.SelectedOptionId ?? 0);
                    ViewBag.SavedAnswers = System.Text.Json.JsonSerializer.Serialize(savedAnswersLegacy);
                }
                else
                {
                    ViewBag.SavedAnswers = "{}";
                }
            }

            ViewBag.AssessmentBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";

            return View(vm);
        }

        // --- ABANDON EXAM ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AbandonExam(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Authorization: only the session owner can abandon their own exam
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id)
                return Forbid();

            // Only abandon if currently InProgress (idempotent guard)
            if (assessment.Status != "InProgress" && assessment.Status != "Open")
            {
                TempData["Error"] = "Sesi ujian ini tidak dapat dibatalkan dalam status saat ini.";
                return RedirectToAction("Assessment");
            }

            // Mark Abandoned — keep StartedAt so HC can see when the exam was started
            assessment.Status = "Abandoned";
            assessment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Info"] = "Ujian telah dibatalkan. Hubungi HC jika Anda ingin mengulang.";
            return RedirectToAction("Assessment");
        }

        // Helper: Fisher-Yates shuffle
        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Builds a cross-package ShuffledQuestionIds list using the slot-list algorithm.
        /// Per user decision: guaranteed even distribution (K/N per package, remainder randomly allocated),
        /// Fisher-Yates shuffle of the slot list, then position i → package[slot[i]].Questions ordered by Order, take index pkgCounter[pkgIdx].
        /// For 1 package: returns questions in original DB order (no shuffle).
        /// All packages must be loaded with .Include(p => p.Questions) — questions ordered by q.Order.
        /// </summary>
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

            int N = packages.Count;
            int baseCount = K / N;
            int remainder = K % N;

            // Decide which package indices get +1 question (random allocation of remainder)
            var remainderIndices = Enumerable.Range(0, N)
                .OrderBy(_ => rng.Next())
                .Take(remainder)
                .ToHashSet();

            // Build slot list: [pkg0 × baseCount(+1?), pkg1 × baseCount(+1?), ...]
            var slots = new List<int>();
            for (int i = 0; i < N; i++)
            {
                int count = baseCount + (remainderIndices.Contains(i) ? 1 : 0);
                for (int j = 0; j < count; j++)
                    slots.Add(i);
            }

            // Fisher-Yates shuffle the slot list
            Shuffle(slots, rng);

            // Build ShuffledQuestionIds: for position p, take package[slots[p]].Questions ordered by Order, at index pkgCounter[pkgIdx]
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

        // Helper: extract A/B/C/D from common Correct-column formats
        // Accepts: "A", "B.", "C. some text", "OPTION D", "d" (case-insensitive after ToUpper())
        private static string ExtractCorrectLetter(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            // Already a single valid letter
            if (raw.Length == 1) return raw;
            // Starts with A/B/C/D followed by a non-letter: "A.", "B. text", "D. SOME ANSWER"
            if ("ABCD".Contains(raw[0]) && !char.IsLetterOrDigit(raw[1]))
                return raw[0].ToString();
            // "OPTION A" format (case already uppercased by caller)
            if (raw.StartsWith("OPTION ") && raw.Length > 7 && "ABCD".Contains(raw[7]))
                return raw[7].ToString();
            return raw; // unchanged — will fail validation and show error
        }

        // Helpers: text normalization and fingerprint for import deduplication
        private static string NormalizeText(string s)
            => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();

        private static string MakeFingerprint(string q, string a, string b, string c, string d)
            => string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizeText));

        // --- EXAM SUMMARY (PRE-SUBMIT REVIEW) ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExamSummary(int id, int? assignmentId, Dictionary<int, int> answers)
        {
            // Null-coalesce for nullable dictionary parameter
            answers ??= new Dictionary<int, int>();

            // Validate ownership
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null) return NotFound();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "This assessment has already been completed.";
                return RedirectToAction("Assessment");
            }

            // Store answers in TempData (dictionary key=questionId, value=selectedOptionId)
            TempData["PendingAnswers"] = System.Text.Json.JsonSerializer.Serialize(answers);
            TempData["PendingAssessmentId"] = id;
            TempData["PendingAssignmentId"] = assignmentId;

            return RedirectToAction("ExamSummary", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExamSummary(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null) return NotFound();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();

            // Retrieve pending answers from TempData
            var answersJson = TempData["PendingAnswers"] as string ?? "{}";
            var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(answersJson)
                          ?? new Dictionary<int, int>();

            // Preserve for the final submit form
            TempData.Keep("PendingAnswers");
            TempData.Keep("PendingAssessmentId");
            TempData.Keep("PendingAssignmentId");

            // CookieTempDataProvider serializes through JSON, which deserializes integers as long — handle both int and long
            int? assignmentId = TempData["PendingAssignmentId"] switch {
                int i => i,
                long l => (int)l,
                _ => (int?)null
            };

            // Build summary items
            var summaryItems = new List<ExamSummaryItem>();

            // Check for package path
            var assignment = assignmentId.HasValue
                ? await _context.UserPackageAssignments.FindAsync(assignmentId.Value)
                : null;

            if (assignment != null)
            {
                // Cross-package: load questions by IDs from ShuffledQuestionIds
                var shuffledQIds = assignment.GetShuffledQuestionIds();
                var questions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => shuffledQIds.Contains(q.Id))
                    .ToListAsync();

                var qLookup = questions.ToDictionary(q => q.Id);
                var optLookup = questions.SelectMany(q => q.Options).ToDictionary(o => o.Id);

                int num = 1;
                foreach (var qId in shuffledQIds)
                {
                    if (!qLookup.TryGetValue(qId, out var q)) continue;
                    int? selectedOptId = answers.TryGetValue(qId, out var v) ? v : (int?)null;
                    string? selectedText = selectedOptId.HasValue && optLookup.TryGetValue(selectedOptId.Value, out var opt)
                        ? opt.OptionText
                        : null;

                    summaryItems.Add(new ExamSummaryItem
                    {
                        DisplayNumber = num++,
                        QuestionId = qId,
                        QuestionText = q.QuestionText,
                        SelectedOptionId = selectedOptId,
                        SelectedOptionText = selectedText
                    });
                }
            }
            else
            {
                // Legacy path: AssessmentQuestion
                var siblingSessionIds = await _context.AssessmentSessions
                    .Where(s => s.Title == assessment.Title &&
                                s.Category == assessment.Category &&
                                s.Schedule.Date == assessment.Schedule.Date)
                    .Select(s => s.Id)
                    .ToListAsync();

                var legacyQuestions = await _context.AssessmentQuestions
                    .Include(q => q.Options)
                    .Where(q => siblingSessionIds.Contains(q.AssessmentSessionId))
                    .OrderBy(q => q.Order)
                    .ToListAsync();

                var optLookup = legacyQuestions.SelectMany(q => q.Options).ToDictionary(o => o.Id);

                int num = 1;
                foreach (var q in legacyQuestions)
                {
                    int? selectedOptId = answers.TryGetValue(q.Id, out var v) ? v : (int?)null;
                    string? selectedText = selectedOptId.HasValue && optLookup.TryGetValue(selectedOptId.Value, out var opt)
                        ? opt.OptionText
                        : null;

                    summaryItems.Add(new ExamSummaryItem
                    {
                        DisplayNumber = num++,
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        SelectedOptionId = selectedOptId,
                        SelectedOptionText = selectedText
                    });
                }
            }

            int unansweredCount = summaryItems.Count(s => !s.SelectedOptionId.HasValue);

            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = id;
            ViewBag.AssignmentId = assignmentId;
            ViewBag.UnansweredCount = unansweredCount;
            ViewBag.Answers = answers; // passed to the hidden final-submit form
            return View(summaryItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(int id, Dictionary<int, int> answers)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Authorization: only owner or Admin can submit
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
            {
                return Forbid();
            }

            // Prevent re-submission of completed assessments
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "This assessment has already been completed.";
                return RedirectToAction("Assessment");
            }

            // ---- Server-side timer enforcement (LIFE-03) ----
            // Grace period: 2 minutes to account for network latency and slow connections.
            // Skip check if StartedAt is null (legacy sessions that existed before Phase 21).
            if (assessment.StartedAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
                int allowedMinutes = assessment.DurationMinutes + 2; // 2-minute grace
                if (elapsed.TotalMinutes > allowedMinutes)
                {
                    TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses.";
                    return RedirectToAction("StartExam", new { id });
                }
            }

            // Check for package path — must happen before any grading logic
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

            if (packageAssignment != null)
            {
                // ---- PACKAGE PATH: ID-based grading via PackageOption.IsCorrect ----
                // Cross-package: load questions by IDs from ShuffledQuestionIds (spans multiple packages)
                var shuffledIds = packageAssignment.GetShuffledQuestionIds();
                var packageQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => shuffledIds.Contains(q.Id))
                    .ToListAsync();
                var questionLookupById = packageQuestions.ToDictionary(q => q.Id);

                int totalScore = 0;
                int maxScore = shuffledIds.Sum(qId =>
                    questionLookupById.TryGetValue(qId, out var qq) ? qq.ScoreValue : 0);

                // Batch-load all existing responses to avoid N+1 queries in the grading loop
                var existingResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToDictionaryAsync(r => r.PackageQuestionId);

                foreach (var qId in shuffledIds)
                {
                    if (!questionLookupById.TryGetValue(qId, out var q)) continue;
                    int? selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : (int?)null;

                    if (selectedOptId.HasValue)
                    {
                        var selectedOption = q.Options.FirstOrDefault(o => o.Id == selectedOptId.Value);
                        if (selectedOption != null && selectedOption.IsCorrect)
                            totalScore += q.ScoreValue;
                    }

                    // Persist answer for package-based answer review (upsert: SaveAnswer may have already
                    // written a record incrementally; update it rather than inserting a duplicate)
                    if (existingResponses.TryGetValue(q.Id, out var existingResponse))
                    {
                        existingResponse.PackageOptionId = selectedOptId;
                        existingResponse.SubmittedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.PackageUserResponses.Add(new PackageUserResponse
                        {
                            AssessmentSessionId = id,
                            PackageQuestionId = q.Id,
                            PackageOptionId = selectedOptId,
                            SubmittedAt = DateTime.UtcNow
                        });
                    }
                }

                int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                // Competency auto-update removed in Phase 90 (KKJ tables dropped)

                // Save PackageUserResponses first (answer persistence is safe to run before status claim)
                await _context.SaveChangesAsync();

                // Status-guarded write: detach entity and use ExecuteUpdateAsync so that if AkhiriUjian
                // already completed this session, we get rowsAffected==0 and skip silently.
                _context.Entry(assessment).State = EntityState.Detached;

                var rowsAffected = await _context.AssessmentSessions
                    .Where(s => s.Id == id && s.Status != "Completed")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Score, finalPercentage)
                        .SetProperty(r => r.Status, "Completed")
                        .SetProperty(r => r.Progress, 100)
                        .SetProperty(r => r.IsPassed, finalPercentage >= assessment.PassPercentage)
                        .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
                    );

                if (rowsAffected == 0)
                {
                    // Race: AkhiriUjian already completed this session — inform user and redirect to results
                    TempData["Info"] = "Ujian Anda sudah diakhiri oleh pengawas.";
                    return RedirectToAction("Results", new { id });
                }

                // SignalR push: notify HC monitor group that worker submitted (package path)
                {
                    var submitBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
                    var result = finalPercentage >= assessment.PassPercentage ? "Pass" : "Fail";
                    int totalQuestionsSubmit = shuffledIds.Count;
                    await _hubContext.Clients.Group($"monitor-{submitBatchKey}").SendAsync("workerSubmitted",
                        new { sessionId = id, workerName = user.FullName, score = finalPercentage, result, status = "Completed", totalQuestions = totalQuestionsSubmit });
                }

                // Update assignment completion separately
                await _context.UserPackageAssignments
                    .Where(a => a.AssessmentSessionId == id)
                    .ExecuteUpdateAsync(a => a.SetProperty(r => r.IsCompleted, true));

                // ASSESS-08: Auto-create TrainingRecord on exam completion (duplicate guard prevents double-insert on retry)
                var judul = $"Assessment: {assessment.Title}";
                bool trainingRecordExists = await _context.TrainingRecords.AnyAsync(t =>
                    t.UserId == assessment.UserId &&
                    t.Judul == judul &&
                    t.Tanggal == assessment.Schedule);
                if (!trainingRecordExists)
                {
                    _context.TrainingRecords.Add(new TrainingRecord
                    {
                        UserId = assessment.UserId,
                        Judul = judul,
                        Kategori = assessment.Category ?? "Assessment",
                        Tanggal = assessment.Schedule,
                        TanggalSelesai = assessment.CompletedAt,
                        Penyelenggara = "Internal",
                        Status = assessment.IsPassed == true ? "Passed" : "Failed"
                    });
                    await _context.SaveChangesAsync();
                }

                // ASMT-02: Check group completion and notify HC/Admin
                await NotifyIfGroupCompleted(assessment);

                // Activity log: record exam submission (fire-and-forget)
                _ = LogActivityAsync(id, "submitted");

                return RedirectToAction("Results", new { id });
            }
            else
            {
                // ---- LEGACY PATH: existing AssessmentQuestion + AssessmentOption grading ----
                var siblingSessionIds = await _context.AssessmentSessions
                    .Where(s => s.Title == assessment.Title &&
                                s.Category == assessment.Category &&
                                s.Schedule.Date == assessment.Schedule.Date)
                    .Select(s => s.Id)
                    .ToListAsync();

                var siblingWithQuestions = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any())
                    .FirstOrDefaultAsync();

                var questionsForGrading = siblingWithQuestions?.Questions?.ToList()
                    ?? new List<AssessmentQuestion>();

                // Pre-load existing auto-saved responses (from Phase 41 SaveLegacyAnswer) to avoid duplicate inserts
                var existingLegacyResponses = await _context.UserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                var existingLegacyDict = existingLegacyResponses.ToDictionary(r => r.AssessmentQuestionId);

                int totalScore = 0;
                int maxScore = 0;

                // Process Answers
                foreach (var question in questionsForGrading)
                {
                    maxScore += question.ScoreValue;
                    int? selectedOptionId = null;

                    if (answers.ContainsKey(question.Id))
                    {
                        selectedOptionId = answers[question.Id];
                        var selectedOption = question.Options.FirstOrDefault(o => o.Id == selectedOptionId);

                        // Check if correct
                        if (selectedOption != null && selectedOption.IsCorrect)
                        {
                            totalScore += question.ScoreValue;
                        }
                    }

                    // Upsert: update auto-saved row if it exists, otherwise insert new row
                    // (mirrors package path — avoids MERGE FK conflict from duplicate inserts)
                    if (existingLegacyDict.TryGetValue(question.Id, out var existingLegacyResponse))
                    {
                        existingLegacyResponse.SelectedOptionId = selectedOptionId;
                    }
                    else
                    {
                        _context.UserResponses.Add(new UserResponse
                        {
                            AssessmentSessionId = id,
                            AssessmentQuestionId = question.Id,
                            SelectedOptionId = selectedOptionId
                        });
                    }
                }

                // Calculate Grade (0-100 scale if needed, or raw score)
                // For now, let's store the raw score sum or percentage?
                // Model.Score is int, usually 0-100 logic is preferred.
                int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                // Competency auto-update removed in Phase 90 (KKJ tables dropped)

                // Save UserResponses first, then status-guarded claim
                await _context.SaveChangesAsync();

                _context.Entry(assessment).State = EntityState.Detached;

                var legacyRowsAffected = await _context.AssessmentSessions
                    .Where(s => s.Id == id && s.Status != "Completed")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Score, finalPercentage)
                        .SetProperty(r => r.Status, "Completed")
                        .SetProperty(r => r.Progress, 100)
                        .SetProperty(r => r.IsPassed, finalPercentage >= assessment.PassPercentage)
                        .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
                    );

                if (legacyRowsAffected == 0)
                {
                    TempData["Info"] = "Ujian Anda sudah diakhiri oleh pengawas.";
                    return RedirectToAction("Results", new { id });
                }

                // SignalR push: notify HC monitor group that worker submitted (legacy path)
                {
                    var legacyBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
                    var legacyResult = finalPercentage >= assessment.PassPercentage ? "Pass" : "Fail";
                    int legacyTotalQ = questionsForGrading.Count;
                    await _hubContext.Clients.Group($"monitor-{legacyBatchKey}").SendAsync("workerSubmitted",
                        new { sessionId = id, workerName = user.FullName, score = finalPercentage, result = legacyResult, status = "Completed", totalQuestions = legacyTotalQ });
                }

                // ASSESS-08: Auto-create TrainingRecord on exam completion (duplicate guard prevents double-insert on retry)
                var judulLegacy = $"Assessment: {assessment.Title}";
                bool trainingRecordExistsLegacy = await _context.TrainingRecords.AnyAsync(t =>
                    t.UserId == assessment.UserId &&
                    t.Judul == judulLegacy &&
                    t.Tanggal == assessment.Schedule);
                if (!trainingRecordExistsLegacy)
                {
                    _context.TrainingRecords.Add(new TrainingRecord
                    {
                        UserId = assessment.UserId,
                        Judul = judulLegacy,
                        Kategori = assessment.Category ?? "Assessment",
                        Tanggal = assessment.Schedule,
                        TanggalSelesai = assessment.CompletedAt,
                        Penyelenggara = "Internal",
                        Status = assessment.IsPassed == true ? "Passed" : "Failed"
                    });
                    await _context.SaveChangesAsync();
                }

                // ASMT-02: Check group completion and notify HC/Admin
                await NotifyIfGroupCompleted(assessment);

                // Activity log: record exam submission (fire-and-forget)
                _ = LogActivityAsync(id, "submitted");

                // Redirect to Results Page
                return RedirectToAction("Results", new { id = id });
            }
        }
        /// <summary>
        /// Fire-and-forget helper to log exam activity. Errors are swallowed — logging must
        /// never break the exam flow.
        /// </summary>
        private async Task LogActivityAsync(int sessionId, string eventType, string? detail = null)
        {
            try
            {
                _context.ExamActivityLogs.Add(new HcPortal.Models.ExamActivityLog
                {
                    SessionId = sessionId,
                    EventType = eventType,
                    Detail = detail,
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Swallow all errors — logging must never block exam flow
            }
        }

        [HttpGet]
        public async Task<IActionResult> Certificate(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Security: Owner, Admin, HC
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Force login if session expired

            var userRoles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = assessment.UserId == user.Id || 
                              userRoles.Contains("Admin") || 
                              userRoles.Contains("HC");

            if (!isAuthorized) return Forbid();

            // Only generate if Completed
            if (assessment.Status != "Completed")
            {
                TempData["Error"] = "Assessment not completed yet.";
                return RedirectToAction("Assessment");
            }

            // Guard: certificate generation disabled for this assessment
            if (!assessment.GenerateCertificate)
                return NotFound();

            // Guard: certificate only available for passed assessments
            if (assessment.IsPassed != true)
            {
                TempData["Error"] = "Certificate is only available for passed assessments.";
                return RedirectToAction("Results", new { id });
            }

            return View(assessment);
        }

        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Authorization: owner, Admin, or HC
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = assessment.UserId == user.Id ||
                                userRoles.Contains("Admin") ||
                                userRoles.Contains("HC");
            if (!isAuthorized) return Forbid();

            // Must be completed
            if (assessment.Status != "Completed")
            {
                TempData["Error"] = "Assessment not completed yet.";
                return RedirectToAction("Assessment");
            }

            // Build ViewModel
            var correctCount = 0;
            List<QuestionReviewItem>? questionReviews = null;
            var passPercentage = assessment.PassPercentage;
            var score = assessment.Score ?? 0;

            // Detect package path
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

            // For legacy path only: explicitly load Questions (ordered) and Responses.
            // Kept separate to avoid EF Core generating a ROW_NUMBER() full table scan when
            // OrderBy is used inside an Include on a large AssessmentQuestions table.
            if (packageAssignment == null)
            {
                await _context.Entry(assessment)
                    .Collection(a => a.Questions)
                    .Query()
                    .Include(q => q.Options)
                    .OrderBy(q => q.Order)
                    .LoadAsync();

                await _context.Entry(assessment)
                    .Collection(a => a.Responses)
                    .Query()
                    .Include(r => r.SelectedOption)
                    .LoadAsync();
            }

            AssessmentResultsViewModel viewModel;

            if (packageAssignment != null)
            {
                // Package path: load PackageQuestion + PackageOption + PackageUserResponse data
                // Cross-package: load questions by IDs from ShuffledQuestionIds
                var shuffledQuestionIds = packageAssignment.GetShuffledQuestionIds();
                var packageQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => shuffledQuestionIds.Contains(q.Id))
                    .ToListAsync();

                var packageResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                var responseDict = packageResponses.ToDictionary(r => r.PackageQuestionId);

                // Use shuffled order from assignment for display (shuffledQuestionIds already declared above)
                var questionLookup = packageQuestions.ToDictionary(q => q.Id);

                // If shuffled IDs are empty (edge case), fall back to natural order
                var orderedQuestionIds = shuffledQuestionIds.Any()
                    ? shuffledQuestionIds.Where(qid => questionLookup.ContainsKey(qid)).ToList()
                    : packageQuestions.Select(q => q.Id).ToList();

                if (assessment.AllowAnswerReview)
                {
                    questionReviews = new List<QuestionReviewItem>();
                    int questionNum = 0;
                    foreach (var qId in orderedQuestionIds)
                    {
                        if (!questionLookup.TryGetValue(qId, out var question)) continue;
                        questionNum++;

                        responseDict.TryGetValue(qId, out var userResponse);
                        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                        var selectedOption = userResponse?.PackageOptionId != null
                            ? question.Options.FirstOrDefault(o => o.Id == userResponse.PackageOptionId)
                            : null;
                        bool isCorrect = selectedOption != null && selectedOption.IsCorrect;
                        if (isCorrect) correctCount++;

                        questionReviews.Add(new QuestionReviewItem
                        {
                            QuestionNumber = questionNum,
                            QuestionText = question.QuestionText,
                            UserAnswer = selectedOption?.OptionText,
                            CorrectAnswer = correctOption?.OptionText ?? "N/A",
                            IsCorrect = isCorrect,
                            Options = question.Options.Select(o => new OptionReviewItem
                            {
                                OptionText = o.OptionText,
                                IsCorrect = o.IsCorrect,
                                IsSelected = userResponse?.PackageOptionId == o.Id
                            }).ToList()
                        });
                    }
                }
                else
                {
                    // Count correct even when review disabled
                    foreach (var qId in orderedQuestionIds)
                    {
                        if (!questionLookup.TryGetValue(qId, out var question)) continue;
                        responseDict.TryGetValue(qId, out var userResponse);
                        if (userResponse?.PackageOptionId != null)
                        {
                            var selectedOpt = question.Options.FirstOrDefault(o => o.Id == userResponse.PackageOptionId);
                            if (selectedOpt != null && selectedOpt.IsCorrect)
                                correctCount++;
                        }
                    }
                }

                // Sub-competency scoring
                List<SubCompetencyScore>? subCompScores = null;
                var examQuestions = orderedQuestionIds
                    .Where(qid => questionLookup.ContainsKey(qid))
                    .Select(qid => questionLookup[qid])
                    .ToList();
                var hasRealSubCompetency = examQuestions.Any(q => !string.IsNullOrWhiteSpace(q.SubCompetency));
                if (hasRealSubCompetency)
                {
                    subCompScores = examQuestions
                        .GroupBy(q => string.IsNullOrWhiteSpace(q.SubCompetency) ? "Lainnya" : q.SubCompetency!)
                        .Select(g =>
                        {
                            var total = g.Count();
                            var correct = g.Count(q =>
                            {
                                if (!responseDict.TryGetValue(q.Id, out var resp)) return false;
                                if (resp.PackageOptionId == null) return false;
                                var sel = q.Options.FirstOrDefault(o => o.Id == resp.PackageOptionId);
                                return sel != null && sel.IsCorrect;
                            });
                            return new SubCompetencyScore
                            {
                                Name = g.Key,
                                Correct = correct,
                                Total = total,
                                Percentage = Math.Round((double)correct / total * 100, 1)
                            };
                        })
                        .OrderBy(s => s.Name)
                        .ToList();
                }

                viewModel = new AssessmentResultsViewModel
                {
                    AssessmentId = assessment.Id,
                    Title = assessment.Title,
                    Category = assessment.Category,
                    UserFullName = assessment.User?.FullName ?? "Unknown",
                    Score = score,
                    PassPercentage = passPercentage,
                    IsPassed = score >= passPercentage,
                    AllowAnswerReview = assessment.AllowAnswerReview,
                    GenerateCertificate = assessment.GenerateCertificate,
                    CompletedAt = assessment.CompletedAt,
                    TotalQuestions = orderedQuestionIds.Count,
                    CorrectAnswers = correctCount,
                    QuestionReviews = questionReviews,
                    SubCompetencyScores = subCompScores
                };
            }
            else
            {
                // Legacy path: existing UserResponse + AssessmentQuestion data
                var legacyQuestions = assessment.Questions ?? new List<AssessmentQuestion>();
                var legacyResponses = assessment.Responses ?? new List<UserResponse>();
                if (assessment.AllowAnswerReview)
                {
                    questionReviews = new List<QuestionReviewItem>();
                    int questionNum = 0;
                    foreach (var question in legacyQuestions)
                    {
                        questionNum++;
                        var userResponse = legacyResponses
                            .FirstOrDefault(r => r.AssessmentQuestionId == question.Id);
                        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                        var selectedOption = userResponse?.SelectedOption;
                        bool isCorrect = selectedOption != null && selectedOption.IsCorrect;
                        if (isCorrect) correctCount++;

                        questionReviews.Add(new QuestionReviewItem
                        {
                            QuestionNumber = questionNum,
                            QuestionText = question.QuestionText,
                            UserAnswer = selectedOption?.OptionText,
                            CorrectAnswer = correctOption?.OptionText ?? "N/A",
                            IsCorrect = isCorrect,
                            Options = question.Options.Select(o => new OptionReviewItem
                            {
                                OptionText = o.OptionText,
                                IsCorrect = o.IsCorrect,
                                IsSelected = userResponse?.SelectedOptionId == o.Id
                            }).ToList()
                        });
                    }
                }
                else
                {
                    // Still count correct for summary even when review disabled
                    foreach (var question in legacyQuestions)
                    {
                        var userResponse = legacyResponses
                            .FirstOrDefault(r => r.AssessmentQuestionId == question.Id);
                        if (userResponse?.SelectedOption != null && userResponse.SelectedOption.IsCorrect)
                            correctCount++;
                    }
                }

                viewModel = new AssessmentResultsViewModel
                {
                    AssessmentId = assessment.Id,
                    Title = assessment.Title,
                    Category = assessment.Category,
                    UserFullName = assessment.User?.FullName ?? "Unknown",
                    Score = score,
                    PassPercentage = passPercentage,
                    IsPassed = score >= passPercentage,
                    AllowAnswerReview = assessment.AllowAnswerReview,
                    GenerateCertificate = assessment.GenerateCertificate,
                    CompletedAt = assessment.CompletedAt,
                    TotalQuestions = legacyQuestions.Count,
                    CorrectAnswers = correctCount,
                    QuestionReviews = questionReviews
                };
            }

            // Competency gains section removed in Phase 90 (KKJ tables dropped)

            return View(viewModel);
        }

        #region Helper Methods

        /// <summary>
        /// Generate cryptographically secure random token
        /// </summary>
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

        #endregion

        // ASMT-02: Check if all siblings in an assessment group completed, notify HC/Admin
        private async Task NotifyIfGroupCompleted(AssessmentSession completedSession)
        {
            var allSiblings = await _context.AssessmentSessions
                .Where(s => s.Title == completedSession.Title &&
                            s.Category == completedSession.Category &&
                            s.Schedule.Date == completedSession.Schedule.Date)
                .ToListAsync();

            if (!allSiblings.All(s => s.Status == "Completed")) return;

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
    }
}
