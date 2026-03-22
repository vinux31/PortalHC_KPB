using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
// PositionTargetHelper removed in Phase 90 (KKJ tables dropped)
using System.Text.Json;
using HcPortal.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using HcPortal.Hubs;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using HcPortal.Helpers;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWorkerDataService _workerDataService;

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
            IHubContext<AssessmentHub> hubContext,
            IServiceScopeFactory scopeFactory,
            IWorkerDataService workerDataService)
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
            _scopeFactory = scopeFactory;
            _workerDataService = workerDataService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- HALAMAN GABUNGAN: DOKUMEN KKJ & ALIGNMENT KKJ/IDP ---
        // GET /CMP/DokumenKkj?tab={kkj|alignment}
        [HttpGet]
        public async Task<IActionResult> DokumenKkj(string? tab)
        {
            ViewData["Title"] = "Dokumen KKJ & Alignment KKJ/IDP";

            var currentUser = await _userManager.GetUserAsync(User) as ApplicationUser;
            var userLevel = currentUser?.RoleLevel ?? 6;

            // Load all bagians (top-level OrganizationUnits) ordered by DisplayOrder
            var allBagians = await _context.OrganizationUnits
                .Where(u => u.ParentId == null)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            // Role-based filtering: L1-L4 see all, L5-L6 see own bagian only
            var filteredBagians = allBagians;
            if (userLevel >= 5 && currentUser?.Section != null)
            {
                var sectionFiltered = allBagians
                    .Where(b => b.Name.ToLower() == currentUser.Section.ToLower())
                    .ToList();

                // Only apply filter if it matches at least one bagian; otherwise show all (safe fallback)
                if (sectionFiltered.Count > 0)
                {
                    filteredBagians = sectionFiltered;
                }
            }

            // Load KKJ files (non-archived) grouped by BagianId
            var kkjFiles = await _context.KkjFiles
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
            var kkjFilesByBagian = kkjFiles
                .GroupBy(f => f.OrganizationUnitId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Load CPDP files (non-archived) grouped by OrganizationUnitId
            var cpdpFiles = await _context.CpdpFiles
                .Where(f => !f.IsArchived)
                .OrderBy(f => f.UploadedAt)
                .ToListAsync();
            var cpdpFilesByBagian = cpdpFiles
                .GroupBy(f => f.OrganizationUnitId)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Bagians = filteredBagians;
            ViewBag.KkjFilesByBagian = kkjFilesByBagian;
            ViewBag.CpdpFilesByBagian = cpdpFilesByBagian;
            ViewBag.ActiveTab = tab == "alignment" ? "alignment" : "kkj";

            return View();
        }

        // GET /CMP/KkjFileDownload/{id}
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

        // GET /CMP/CpdpFileDownload/{id}
        public async Task<IActionResult> CpdpFileDownload(int id)
        {
            var cpdpFile = await _context.CpdpFiles
                .Include(f => f.OrganizationUnit)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (cpdpFile == null) return NotFound();

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                cpdpFile.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(physicalPath)) return NotFound("File tidak ditemukan di server.");

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
            var paging = PaginationHelper.Calculate(await query.CountAsync(), page, pageSize);

            // Execute Query with pagination
            var exams = await query
                .OrderByDescending(a => a.Schedule)
                .Skip(paging.Skip)
                .Take(paging.Take)
                .ToListAsync();

            // Auto-transition display: show Upcoming as Open when scheduled date+time has arrived in WIB (display-only, no SaveChangesAsync)
            var nowWib = DateTime.UtcNow.AddHours(7);
            foreach (var exam in exams)
            {
                if (exam.Status == "Upcoming" && exam.Schedule <= nowWib)
                    exam.Status = "Open";
            }

            // Pagination info for view
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
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

        // SaveLegacyAnswer removed in Phase 227 (CLEN-02) — legacy exam path no longer exists.

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
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();

            var unified = await _workerDataService.GetUnifiedRecords(user.Id);

            // Phase 104: Get worker list for Team View tab (only for users level 1-4)
            if (roleLevel <= 4)
            {
                // Scope enforcement: Level 4 (SectionHead, SrSupervisor) locked to their own section
                string? sectionFilter = null;
                if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
                {
                    sectionFilter = user.Section;
                }

                var workerList = await _workerDataService.GetWorkersInSection(sectionFilter);
                ViewData["WorkerList"] = workerList;

                // Phase 217: Set category data for Team View partial
                var allCats = await _context.AssessmentCategories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .Include(c => c.Children)
                    .ToListAsync();
                var subCategoryMap = allCats.ToDictionary(
                    p => p.Name,
                    p => p.Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
                );
                ViewBag.SubCategoryMapJson = System.Text.Json.JsonSerializer.Serialize(subCategoryMap);
                var masterCategories = allCats.Select(c => c.Name).OrderBy(n => n).ToList();
                ViewBag.MasterCategoriesJson = System.Text.Json.JsonSerializer.Serialize(masterCategories);

                var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
                ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
                ViewBag.AllSections = sectionUnitsDict.Keys.ToList();
            }

            return View("Records", unified);
        }

        // Phase 104: Worker Detail page showing unified assessment + training history
        public async Task<IActionResult> RecordsWorkerDetail(string workerId, string? section, string? unit, string? category, string? status, string? search)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();

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

            var unifiedRecords = await _workerDataService.GetUnifiedRecords(workerId);

            var allCats = await _context.AssessmentCategories
                .Where(c => c.IsActive && c.ParentId == null)
                .Include(c => c.Children)
                .ToListAsync();
            var subCategoryMap = allCats.ToDictionary(
                p => p.Name,
                p => p.Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
            );
            ViewBag.SubCategoryMapJson = System.Text.Json.JsonSerializer.Serialize(subCategoryMap);
            ViewBag.MasterCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
                allCats.Select(c => c.Name).OrderBy(n => n).ToList()
            );

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

        // Phase 176: Export personal records as Excel (2 sheets: Assessment + Training)
        [HttpGet]
        public async Task<IActionResult> ExportRecords()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var unified = await _workerDataService.GetUnifiedRecords(user.Id);

            using var workbook = new XLWorkbook();

            // Sheet 1: Assessment
            var wsAssessment = ExcelExportHelper.CreateSheet(workbook, "Assessment", new[] { "No", "Tanggal", "Judul", "Skor", "Status", "Sertifikat" });

            var assessmentRecords = unified.Where(r => r.RecordType == "Assessment Online").ToList();
            for (int i = 0; i < assessmentRecords.Count; i++)
            {
                var r = assessmentRecords[i];
                wsAssessment.Cell(i + 2, 1).Value = i + 1;
                wsAssessment.Cell(i + 2, 2).Value = r.Date.ToString("yyyy-MM-dd");
                wsAssessment.Cell(i + 2, 3).Value = r.Title;
                wsAssessment.Cell(i + 2, 4).Value = r.Score?.ToString() ?? "";
                wsAssessment.Cell(i + 2, 5).Value = r.Status;
                wsAssessment.Cell(i + 2, 6).Value = r.GenerateCertificate == true ? "Ya" : "Tidak";
            }

            // Sheet 2: Training
            var wsTraining = ExcelExportHelper.CreateSheet(workbook, "Training", new[] { "No", "Tanggal", "Judul", "Penyelenggara", "Kategori", "Kota", "Nomor Sertifikat", "Valid Until", "Status" });

            var trainingRecords = unified.Where(r => r.RecordType == "Training Manual").ToList();
            for (int i = 0; i < trainingRecords.Count; i++)
            {
                var r = trainingRecords[i];
                wsTraining.Cell(i + 2, 1).Value = i + 1;
                wsTraining.Cell(i + 2, 2).Value = r.Date.ToString("yyyy-MM-dd");
                wsTraining.Cell(i + 2, 3).Value = r.Title;
                wsTraining.Cell(i + 2, 4).Value = r.Penyelenggara ?? "";
                wsTraining.Cell(i + 2, 5).Value = r.Kategori ?? "";
                wsTraining.Cell(i + 2, 6).Value = r.Kota ?? "";
                wsTraining.Cell(i + 2, 7).Value = r.NomorSertifikat ?? "";
                wsTraining.Cell(i + 2, 8).Value = r.ValidUntil?.ToString("yyyy-MM-dd") ?? "";
                wsTraining.Cell(i + 2, 9).Value = r.Status ?? "";
            }

            var safeName = (user.FullName ?? user.Id).Replace(" ", "_");
            var filename = $"Records_{safeName}_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, filename, this);
        }

        // Phase 176: Export team assessment records as Excel (filtered by current view params)
        [HttpGet]
        public async Task<IActionResult> ExportRecordsTeamAssessment(string? section, string? unit, string? search, string? statusFilter)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();

            if (roleLevel >= 5) return Forbid();

            // Scope enforcement: Level 4 locked to their own section
            string? sectionFilter = section;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            var (assessmentRows, _) = await _workerDataService.GetAllWorkersHistory();

            // Get filtered worker IDs from GetWorkersInSection
            var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, null, search, statusFilter);
            var filteredIds = filteredWorkers
                .Select(w => w.WorkerId)
                .ToHashSet();

            var filtered = assessmentRows
                .Where(r => filteredIds.Contains(r.WorkerId))
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Assessment", new[] { "No", "Nama", "NIP", "Judul", "Tanggal", "Skor", "Status", "Attempt" });

            for (int i = 0; i < filtered.Count; i++)
            {
                var r = filtered[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = r.WorkerName;
                ws.Cell(i + 2, 3).Value = r.WorkerNIP ?? "";
                ws.Cell(i + 2, 4).Value = r.Title;
                ws.Cell(i + 2, 5).Value = r.Date.ToString("yyyy-MM-dd");
                ws.Cell(i + 2, 6).Value = r.Score?.ToString() ?? "";
                ws.Cell(i + 2, 7).Value = r.IsPassed == true ? "Passed" : (r.IsPassed == false ? "Failed" : "");
                ws.Cell(i + 2, 8).Value = r.AttemptNumber?.ToString() ?? "";
            }

            var filename = $"RecordsTeam_Assessment_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, filename, this);
        }

        // Phase 176: Export team training records as Excel (filtered by current view params)
        [HttpGet]
        public async Task<IActionResult> ExportRecordsTeamTraining(string? section, string? unit, string? search, string? statusFilter, string? category)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();

            if (roleLevel >= 5) return Forbid();

            // Scope enforcement: Level 4 locked to their own section
            string? sectionFilter = section;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            var (_, trainingRows) = await _workerDataService.GetAllWorkersHistory();

            // Get filtered worker IDs from GetWorkersInSection
            var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, category, search, statusFilter);
            var filteredIds = filteredWorkers
                .Select(w => w.WorkerId)
                .ToHashSet();

            var filtered = trainingRows
                .Where(r => filteredIds.Contains(r.WorkerId))
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Training", new[] { "No", "Nama", "NIP", "Judul", "Tanggal", "Penyelenggara" });

            for (int i = 0; i < filtered.Count; i++)
            {
                var r = filtered[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = r.WorkerName;
                ws.Cell(i + 2, 3).Value = r.WorkerNIP ?? "";
                ws.Cell(i + 2, 4).Value = r.Title;
                ws.Cell(i + 2, 5).Value = r.Date.ToString("yyyy-MM-dd");
                ws.Cell(i + 2, 6).Value = r.Penyelenggara ?? "";
            }

            var filename = $"RecordsTeam_Training_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, filename, this);
        }
        // PHASE 198: Import/Edit/Delete Training actions moved to AdminController

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
                return Json(new { success = false, message = "Token tidak valid. Silakan periksa dan coba lagi." });
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
                LogActivityAsync(assessment.Id, "started");
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
                // Legacy path removed (Phase 227 CLEN-02) — sessions without packages return error.
                TempData["Error"] = "Sesi ujian ini tidak memiliki paket soal. Hubungi Admin atau HC.";
                return RedirectToAction("Assessment");
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
        /// Builds a cross-package ShuffledQuestionIds list using the ET-aware distribution algorithm.
        /// For 1 package: returns all questions shuffled (ET coverage is inherent).
        /// For N packages: Phase 1 guarantees at least one question per ElemenTeknis group (best-effort),
        /// Phase 2 fills remaining quota with balanced package distribution,
        /// Phase 3 Fisher-Yates shuffles the combined list.
        /// Falls back to original slot-list algorithm when no questions have ElemenTeknis data.
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
            // Legacy path removed (Phase 227 CLEN-02).

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

                // Persist ET scores per session (Phase 223)
                var etGroups = packageQuestions
                    .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis);

                foreach (var etGroup in etGroups)
                {
                    int etCorrect = 0;
                    int etTotal = etGroup.Count();
                    foreach (var q in etGroup)
                    {
                        if (answers.ContainsKey(q.Id))
                        {
                            var sel = q.Options.FirstOrDefault(o => o.Id == answers[q.Id]);
                            if (sel != null && sel.IsCorrect) etCorrect++;
                        }
                    }
                    _context.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore
                    {
                        AssessmentSessionId = id,
                        ElemenTeknis = etGroup.Key,
                        CorrectCount = etCorrect,
                        QuestionCount = etTotal
                    });
                }

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

                // Phase 227 CLEN-04: Generate NomorSertifikat only when passed
                bool isPassed = finalPercentage >= assessment.PassPercentage;
                if (assessment.GenerateCertificate && isPassed)
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

                // ASMT-02: Check group completion and notify HC/Admin
                await _workerDataService.NotifyIfGroupCompleted(assessment);

                // Activity log: record exam submission (fire-and-forget)
                LogActivityAsync(id, "submitted");

                return RedirectToAction("Results", new { id });
            }
            else
            {
                // Legacy path removed (Phase 227 CLEN-02) — sessions without package assignment cannot be submitted.
                TempData["Error"] = "Sesi ujian ini tidak memiliki paket soal. Hubungi Admin atau HC.";
                return RedirectToAction("Assessment");
            }
        }
        /// <summary>
        /// Fire-and-forget helper to log exam activity. Errors are swallowed — logging must
        /// never break the exam flow.
        /// </summary>
        private void LogActivityAsync(int sessionId, string eventType, string? detail = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.ExamActivityLogs.Add(new HcPortal.Models.ExamActivityLog
                    {
                        SessionId = sessionId,
                        EventType = eventType,
                        Detail = detail,
                        Timestamp = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Swallow all errors — logging must never block exam flow
                }
            });
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

            ViewBag.PSign = await ResolveCategorySignatory(assessment.Category);
            return View(assessment);
        }

        private async Task<PSignViewModel> ResolveCategorySignatory(string? categoryName)
        {
            var fallback = new PSignViewModel { Position = "HC Manager", FullName = "" };
            if (string.IsNullOrWhiteSpace(categoryName)) return fallback;

            var category = await _context.AssessmentCategories
                .Include(c => c.Signatory)
                .Include(c => c.Parent).ThenInclude(p => p!.Signatory)
                .FirstOrDefaultAsync(c => c.Name == categoryName);

            if (category?.Signatory != null)
                return new PSignViewModel
                {
                    FullName = category.Signatory.FullName ?? "",
                    Position = category.Signatory.Position ?? "HC Manager"
                };

            if (category?.Parent?.Signatory != null)
                return new PSignViewModel
                {
                    FullName = category.Parent.Signatory.FullName ?? "",
                    Position = category.Parent.Signatory.Position ?? "HC Manager"
                };

            return fallback;
        }

        [HttpGet]
        public async Task<IActionResult> CertificatePdf(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userRoles = await _userManager.GetRolesAsync(user);
            bool isAuthorized = assessment.UserId == user.Id ||
                                userRoles.Contains("Admin") ||
                                userRoles.Contains("HC");
            if (!isAuthorized) return Forbid();

            if (assessment.Status != "Completed")
            {
                TempData["Error"] = "Assessment not completed yet.";
                return RedirectToAction("Assessment");
            }

            if (!assessment.GenerateCertificate) return NotFound();

            if (assessment.IsPassed != true)
            {
                TempData["Error"] = "Certificate is only available for passed assessments.";
                return RedirectToAction("Results", new { id });
            }

            var pSign = await ResolveCategorySignatory(assessment.Category);

            // Register fonts from wwwroot/fonts/
            var fontsPath = Path.Combine(_env.WebRootPath, "fonts");
            foreach (var fontFile in Directory.GetFiles(fontsPath, "*.ttf"))
            {
                using var fontStream = System.IO.File.OpenRead(fontFile);
                FontManager.RegisterFont(fontStream);
            }

            var completedAt = assessment.CompletedAt ?? assessment.UpdatedAt ?? assessment.CreatedAt;
            var dateStr = completedAt.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));

            // Generate watermark SVG as image for the PDF
            byte[]? watermarkBytes = null;
            try
            {
                var svgContent = @"<svg viewBox='0 0 400 350' xmlns='http://www.w3.org/2000/svg'>
                    <path d='M200 20L30 330H370L200 20ZM200 80L325 310H75L200 80Z' fill='#1a4a8d' opacity='0.05'/>
                </svg>";
                var svgPath = Path.Combine(Path.GetTempPath(), "cert_watermark.svg");
                System.IO.File.WriteAllText(svgPath, svgContent);
            }
            catch { /* Skip watermark if SVG fails */ }

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(15);

                    // Outer blue border + inner gold double border (matches HTML)
                    page.Content()
                        .Border(2).BorderColor("#1a4a8d")
                        .Padding(4)
                        .Border(1).BorderColor("#c49a00")
                        .Padding(1)
                        .Border(1).BorderColor("#c49a00")
                        .Layers(layers =>
                        {
                            // Watermark triangle (matches HTML SVG at 5% opacity)
                            layers.Layer().AlignCenter().AlignMiddle()
                                .Width(PageSizes.A4.Landscape().Width * 0.55f)
                                .Height(PageSizes.A4.Landscape().Height * 0.65f)
                                .Svg("<svg viewBox='0 0 400 350' xmlns='http://www.w3.org/2000/svg'><path d='M200 20L30 330H370L200 20ZM200 80L325 310H75L200 80Z' fill='#1a4a8d' opacity='0.05'/></svg>");

                            // Score badge at bottom-right as circle (matches HTML .badge-score)
                            if (assessment.Score.HasValue)
                            {
                                // Circle badge via SVG
                                layers.Layer().AlignRight().AlignBottom()
                                    .Padding(20)
                                    .Width(80).Height(80)
                                    .Svg($"<svg viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'><circle cx='50' cy='50' r='50' fill='#c49a00'/><circle cx='50' cy='50' r='46' fill='#1a4a8d'/><text x='50' y='38' text-anchor='middle' fill='white' font-family='Lato,sans-serif' font-size='12' font-weight='bold'>SCORE</text><text x='50' y='68' text-anchor='middle' fill='white' font-family='Lato,sans-serif' font-size='28' font-weight='bold'>{assessment.Score}%</text></svg>");
                            }

                            // Main content (vertically centered like HTML flexbox)
                            layers.PrimaryLayer()
                                .AlignCenter().AlignMiddle()
                                .Width(PageSizes.A4.Landscape().Width * 0.75f)
                                .Column(col =>
                                {
                                    col.Spacing(0);

                                    // Header: Pertamina logo + HC PORTAL KPB
                                    col.Item().AlignCenter().PaddingBottom(18).Row(headerRow =>
                                    {
                                        var logoPath = Path.Combine(_env.WebRootPath, "images", "psign-pertamina.png");
                                        if (System.IO.File.Exists(logoPath))
                                        {
                                            headerRow.AutoItem().Height(45)
                                                .Image(logoPath).FitHeight()
                                                .WithCompressionQuality(ImageCompressionQuality.High);
                                            headerRow.ConstantItem(12);
                                        }
                                        headerRow.AutoItem().AlignMiddle().Column(logoText =>
                                        {
                                            logoText.Item()
                                                .Text("HC PORTAL KPB")
                                                .FontFamily("Playfair Display").FontSize(18).Bold()
                                                .LetterSpacing(0.05f).FontColor("#1a4a8d");
                                            logoText.Item()
                                                .Text("Human Capital Development Portal")
                                                .FontFamily("Lato").FontSize(12).FontColor("#666666");
                                        });
                                    });

                                    // Certificate of Completion
                                    col.Item().AlignCenter().PaddingBottom(6)
                                        .Text("Certificate of Completion")
                                        .FontFamily("Playfair Display").FontSize(42).Bold()
                                        .FontColor("#1a4a8d");

                                    // This verifies that
                                    col.Item().AlignCenter().PaddingBottom(28)
                                        .Text("This verifies that")
                                        .FontFamily("Lato").FontSize(15).Italic()
                                        .FontColor("#555555");

                                    // Recipient name with underline
                                    col.Item().AlignCenter().PaddingBottom(20)
                                        .BorderBottom(1.5f).BorderColor("#cccccc")
                                        .PaddingBottom(4)
                                        .Text(assessment.User?.FullName ?? "")
                                        .FontFamily("Playfair Display").FontSize(36).Bold().Italic();

                                    // NIP (if available)
                                    if (!string.IsNullOrEmpty(assessment.User?.NIP))
                                    {
                                        col.Item().AlignCenter().PaddingBottom(15)
                                            .Text($"NIP: {assessment.User.NIP}")
                                            .FontFamily("Lato").FontSize(15)
                                            .FontColor("#555555");
                                    }

                                    // Achievement text
                                    col.Item().AlignCenter().PaddingBottom(6)
                                        .Text("Has successfully completed the competency assessment module")
                                        .FontFamily("Lato").FontSize(15)
                                        .FontColor("#444444");

                                    // Assessment title in gold
                                    col.Item().AlignCenter().PaddingBottom(6)
                                        .Text((assessment.Title ?? "").ToUpperInvariant())
                                        .FontFamily("Lato").FontSize(24).Bold()
                                        .FontColor("#c49a00");

                                    // Proficiency text
                                    col.Item().AlignCenter().PaddingBottom(40)
                                        .Text("Demonstrating proficiency and understanding of the subject matter.")
                                        .FontFamily("Lato").FontSize(15)
                                        .FontColor("#444444");

                                    // Footer row: Date left, Signature right
                                    col.Item().PaddingHorizontal(30).Row(row =>
                                    {
                                        // Left: date + certificate info
                                        row.RelativeItem().Column(left =>
                                        {
                                            left.Item()
                                                .Text(dateStr)
                                                .FontFamily("Lato").FontSize(15).Bold();
                                            left.Item().BorderTop(1).BorderColor("#333333").PaddingTop(3)
                                                .Text("Date of Issue")
                                                .FontFamily("Lato").FontSize(12)
                                                .FontColor("#666666");
                                            if (!string.IsNullOrEmpty(assessment.NomorSertifikat))
                                            {
                                                left.Item().PaddingTop(4)
                                                    .Text($"No. Sertifikat: {assessment.NomorSertifikat}")
                                                    .FontFamily("Lato").FontSize(12)
                                                    .FontColor("#666666");
                                            }
                                            if (assessment.ValidUntil.HasValue)
                                            {
                                                left.Item().PaddingTop(2)
                                                    .Text($"Berlaku Hingga: {assessment.ValidUntil.Value.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("id-ID"))}")
                                                    .FontFamily("Lato").FontSize(12)
                                                    .FontColor("#666666");
                                            }
                                        });

                                        // Right: P-Sign (logo + position + name)
                                        row.AutoItem().AlignRight().AlignBottom().PaddingRight(30).Column(right =>
                                        {
                                            var sigLogoPath = Path.Combine(_env.WebRootPath, "images", "psign-pertamina.png");
                                            if (System.IO.File.Exists(sigLogoPath))
                                            {
                                                right.Item().AlignCenter().Height(40)
                                                    .Image(sigLogoPath).FitHeight()
                                                    .WithCompressionQuality(ImageCompressionQuality.High);
                                            }
                                            right.Item().AlignCenter().PaddingTop(4)
                                                .Text(pSign.Position ?? "HC Manager")
                                                .FontFamily("Lato").FontSize(10).FontColor("#333333");
                                            right.Item().AlignCenter().PaddingTop(2)
                                                .Text(pSign.FullName)
                                                .FontFamily("Lato").FontSize(11).Bold().FontColor("#000000");
                                        });
                                    });
                                });
                        });
                });
            });

            var pdfStream = new MemoryStream();
            pdf.GeneratePdf(pdfStream);

            var nip = assessment.User?.NIP ?? user.Id;
            var safeTitle = Regex.Replace(assessment.Title ?? "Certificate", @"[^a-zA-Z0-9]", "_");
            var year = completedAt.Year;
            var filename = $"Sertifikat_{nip}_{safeTitle}_{year}.pdf";

            return File(pdfStream.ToArray(), "application/pdf", filename);
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

                // ElemenTeknis scoring
                List<ElemenTeknisScore>? elemenTeknisScores = null;
                var examQuestions = orderedQuestionIds
                    .Where(qid => questionLookup.ContainsKey(qid))
                    .Select(qid => questionLookup[qid])
                    .ToList();
                var hasRealElemenTeknis = examQuestions.Any(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis));
                if (hasRealElemenTeknis)
                {
                    elemenTeknisScores = examQuestions
                        .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis!)
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
                            return new ElemenTeknisScore
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
                    ElemenTeknisScores = elemenTeknisScores,
                    NomorSertifikat = assessment.NomorSertifikat
                };
            }
            else
            {
                // Legacy path removed (Phase 227 CLEN-02) — Results shows empty state for sessions without packages.
                viewModel = new AssessmentResultsViewModel
                {
                    AssessmentId = assessment.Id,
                    Title = assessment.Title,
                    Category = assessment.Category,
                    UserFullName = assessment.User?.FullName ?? "Unknown",
                    Score = score,
                    PassPercentage = passPercentage,
                    IsPassed = score >= passPercentage,
                    AllowAnswerReview = false,
                    GenerateCertificate = assessment.GenerateCertificate,
                    CompletedAt = assessment.CompletedAt,
                    TotalQuestions = 0,
                    CorrectAnswers = 0,
                    QuestionReviews = null,
                    ElemenTeknisScores = null,
                    NomorSertifikat = assessment.NomorSertifikat
                };
            }

            // Competency gains section removed in Phase 90 (KKJ tables dropped)

            return View(viewModel);
        }

        #region Helper Methods

        /// <summary>
        /// Returns (user, roleLevel) for the current authenticated user.
        /// Extracts the repeated role-scoping pattern used across multiple actions.
        /// </summary>
        private async Task<(ApplicationUser User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user!);
            var roleLevel = UserRoles.GetRoleLevel(userRoles.FirstOrDefault() ?? "");
            return (user!, roleLevel);
        }

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

        // ============================================================
        // Analytics Dashboard — Phase 224
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AnalyticsDashboard()
        {
            var vm = new AnalyticsDashboardViewModel
            {
                Sections = await _context.GetAllSectionsAsync(),
                Categories = await _context.AssessmentCategories
                    .Where(c => c.ParentId == null && c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => c.Name)
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetAnalyticsData(
            string? bagian,
            string? unit,
            string? kategori,
            string? subKategori,
            DateTime? periodeStart,
            DateTime? periodeEnd)
        {
            var today = DateTime.Today;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            // Base query
            var baseQuery = _context.AssessmentSessions
                .Include(s => s.User)
                .Where(s => s.IsPassed.HasValue
                    && s.CompletedAt.HasValue
                    && s.CompletedAt >= periodeStart
                    && s.CompletedAt <= periodeEnd);

            if (!string.IsNullOrEmpty(bagian))
                baseQuery = baseQuery.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                baseQuery = baseQuery.Where(s => s.User!.Unit == unit);
            if (!string.IsNullOrEmpty(kategori))
                baseQuery = baseQuery.Where(s => s.Category == kategori);
            // subKategori: AssessmentSession has no SubCategory field — skip

            // Fail rate query
            var failRate = await baseQuery
                .GroupBy(s => new { Section = s.User!.Section ?? "Tidak Diketahui", s.Category })
                .Select(g => new FailRateItem
                {
                    Section = g.Key.Section,
                    Category = g.Key.Category,
                    Total = g.Count(),
                    Failed = g.Count(s => s.IsPassed == false)
                })
                .ToListAsync();

            // Trend query
            var trend = await baseQuery
                .GroupBy(s => new { s.CompletedAt!.Value.Year, s.CompletedAt!.Value.Month })
                .Select(g => new TrendItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Passed = g.Count(s => s.IsPassed == true),
                    Failed = g.Count(s => s.IsPassed == false)
                })
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .ToListAsync();

            // ET Breakdown query
            var etBaseQuery = _context.SessionElemenTeknisScores
                .Include(e => e.AssessmentSession)
                    .ThenInclude(s => s.User)
                .Where(e => e.QuestionCount > 0
                    && e.AssessmentSession.CompletedAt.HasValue
                    && e.AssessmentSession.CompletedAt >= periodeStart
                    && e.AssessmentSession.CompletedAt <= periodeEnd);

            if (!string.IsNullOrEmpty(bagian))
                etBaseQuery = etBaseQuery.Where(e => e.AssessmentSession.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                etBaseQuery = etBaseQuery.Where(e => e.AssessmentSession.User!.Unit == unit);
            if (!string.IsNullOrEmpty(kategori))
                etBaseQuery = etBaseQuery.Where(e => e.AssessmentSession.Category == kategori);

            var etBreakdown = await etBaseQuery
                .GroupBy(e => new { e.ElemenTeknis, e.AssessmentSession.Category })
                .Select(g => new EtBreakdownItem
                {
                    ElemenTeknis = g.Key.ElemenTeknis,
                    Category = g.Key.Category,
                    AvgPct = g.Average(e => (double)e.CorrectCount * 100.0 / e.QuestionCount),
                    MinPct = g.Min(e => (double)e.CorrectCount * 100.0 / e.QuestionCount),
                    MaxPct = g.Max(e => (double)e.CorrectCount * 100.0 / e.QuestionCount),
                    SampleCount = g.Count()
                })
                .ToListAsync();

            // Expiring soon query (filter Bagian/Unit saja, bukan kategori)
            var thirtyDaysFromNow = today.AddDays(30);

            var trainingExpiring = _context.TrainingRecords
                .Include(t => t.User)
                .Where(t => t.Status == "Valid"
                    && t.ValidUntil.HasValue
                    && t.ValidUntil >= today
                    && t.ValidUntil <= thirtyDaysFromNow);

            if (!string.IsNullOrEmpty(bagian))
                trainingExpiring = trainingExpiring.Where(t => t.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                trainingExpiring = trainingExpiring.Where(t => t.User!.Unit == unit);

            var trainingExpiringSoon = await trainingExpiring
                .Select(t => new ExpiringSoonItem
                {
                    NamaPekerja = t.User!.FullName ?? t.User.UserName ?? "",
                    NamaSertifikat = t.Judul ?? "",
                    TanggalExpired = t.ValidUntil!.Value,
                    SectionUnit = (t.User!.Section ?? "") + (t.User.Unit != null ? " / " + t.User.Unit : "")
                })
                .ToListAsync();

            var sessionExpiring = _context.AssessmentSessions
                .Include(s => s.User)
                .Where(s => s.IsPassed == true
                    && s.GenerateCertificate
                    && s.ValidUntil.HasValue
                    && s.ValidUntil >= today
                    && s.ValidUntil <= thirtyDaysFromNow);

            if (!string.IsNullOrEmpty(bagian))
                sessionExpiring = sessionExpiring.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                sessionExpiring = sessionExpiring.Where(s => s.User!.Unit == unit);

            var sessionExpiringSoon = await sessionExpiring
                .Select(s => new ExpiringSoonItem
                {
                    NamaPekerja = s.User!.FullName ?? s.User.UserName ?? "",
                    NamaSertifikat = s.Title,
                    TanggalExpired = s.ValidUntil!.Value,
                    SectionUnit = (s.User!.Section ?? "") + (s.User.Unit != null ? " / " + s.User.Unit : "")
                })
                .ToListAsync();

            var expiringSoon = trainingExpiringSoon
                .Concat(sessionExpiringSoon)
                .OrderBy(e => e.TanggalExpired)
                .ToList();

            return Json(new AnalyticsDataResult
            {
                FailRate = failRate,
                Trend = trend,
                EtBreakdown = etBreakdown,
                ExpiringSoon = expiringSoon
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetAnalyticsCascadeUnits(string bagian)
        {
            return Json(await _context.GetUnitsForSectionAsync(bagian));
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetAnalyticsCascadeSubKategori(string kategori)
        {
            var subCategories = await _context.AssessmentCategories
                .Where(c => c.Parent != null && c.Parent.Name == kategori && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();
            return Json(subCategories);
        }

    }
}
