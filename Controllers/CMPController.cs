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
        private readonly GradingService _gradingService;

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
            IWorkerDataService workerDataService,
            GradingService gradingService)
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
            _gradingService = gradingService;
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

            // === Pre-Post pair grouping (per D-01) ===

            // Step 1: Kelompokkan sessions dari exams menjadi pair dan standalone
            var prePairs = exams
                .Where(e => !string.IsNullOrEmpty(e.AssessmentType) && e.AssessmentType == "PreTest" && e.LinkedGroupId.HasValue)
                .ToList();

            var postPairs = exams
                .Where(e => !string.IsNullOrEmpty(e.AssessmentType) && e.AssessmentType == "PostTest" && e.LinkedGroupId.HasValue)
                .ToList();

            // Gunakan List<dynamic> agar bisa Add() nanti (anonymous type immutable)
            var pairedGroups = new List<dynamic>();

            foreach (var pre in prePairs)
            {
                var post = postPairs.FirstOrDefault(p => p.LinkedGroupId == pre.LinkedGroupId);
                pairedGroups.Add(new { Pre = (dynamic)pre, Post = (dynamic)post });
            }

            // Track IDs yang sudah masuk pair
            var pairedIds = new HashSet<int>(
                pairedGroups.SelectMany(pg => {
                    var ids = new List<int> { (int)pg.Pre.Id };
                    if (pg.Post != null) ids.Add((int)pg.Post.Id);
                    return ids;
                })
            );

            // Step 2: Query Completed Pre sessions yang punya Post di exams
            // SATU query untuk semua — bukan per-pair (review fix: avoid N+1)
            var postGroupIds = postPairs
                .Where(p => p.LinkedGroupId.HasValue)
                .Select(p => p.LinkedGroupId.Value)
                .Except(prePairs.Where(p => p.LinkedGroupId.HasValue).Select(p => p.LinkedGroupId.Value))
                .ToList();

            if (postGroupIds.Any())
            {
                var completedPreSessions = await _context.AssessmentSessions
                    .Where(s => s.AssessmentType == "PreTest"
                        && s.LinkedGroupId.HasValue
                        && postGroupIds.Contains(s.LinkedGroupId.Value)
                        && s.Status == "Completed")
                    .ToListAsync();

                foreach (var completedPre in completedPreSessions)
                {
                    var matchingPost = postPairs.FirstOrDefault(p => p.LinkedGroupId == completedPre.LinkedGroupId);
                    if (matchingPost != null)
                    {
                        pairedGroups.Add(new { Pre = (dynamic)completedPre, Post = (dynamic)matchingPost });
                        pairedIds.Add(completedPre.Id);
                        pairedIds.Add(matchingPost.Id);
                    }
                }
            }

            // Step 3: Standalone = semua yang tidak masuk pair
            var standaloneExams = exams
                .Where(e => !pairedIds.Contains(e.Id))
                .ToList();

            ViewBag.PairedGroups = pairedGroups;
            ViewBag.StandaloneExams = standaloneExams;

            // Pagination info for view
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.PageSize = pageSize;

            // ========== RIWAYAT UJIAN: completed assessment history for worker ==========
            // BUG-14 fix: limit to 50 most recent to prevent loading excessive data
            var completedHistory = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && (a.Status == "Completed" || a.Status == "Abandoned"))
                .OrderByDescending(a => a.CompletedAt ?? a.UpdatedAt)
                .Take(50)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.CompletedAt,
                    a.Score,
                    a.IsPassed,
                    a.Status,
                    a.AssessmentType
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
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Race: concurrent request already inserted this response — retry as update
                    _context.ChangeTracker.Clear();
                    await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.PackageOptionId, optionId)
                            .SetProperty(r => r.SubmittedAt, DateTime.UtcNow));
                }
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

            // Clamp elapsedSeconds dari client sebelum update (per server-authoritative timer logic)
            int clampedElapsed = elapsedSeconds;

            // Clamp 1: tidak boleh melebihi wall-clock elapsed sejak StartedAt
            if (session.StartedAt.HasValue)
            {
                int wallClockElapsed = (int)(DateTime.UtcNow - session.StartedAt.Value).TotalSeconds;
                clampedElapsed = Math.Min(clampedElapsed, wallClockElapsed);
            }

            // Clamp 2: tidak boleh mundur (monotonically increasing)
            clampedElapsed = Math.Max(clampedElapsed, session.ElapsedSeconds);

            // Clamp 3: tidak boleh melebihi durasi total
            clampedElapsed = Math.Min(clampedElapsed, session.DurationMinutes * 60);

            // Atomic update of elapsed time and last active page
            var updated = await _context.AssessmentSessions
                .Where(s => s.Id == sessionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.ElapsedSeconds, clampedElapsed)
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
            if (user == null) return RedirectToAction("Login", "Account");

            var unified = await _workerDataService.GetUnifiedRecords(user.Id);

            // Phase 104: Get worker list for Team View tab (L1-L5: Admin, HC, Management, Section, Coach)
            if (roleLevel <= 5)
            {
                // Scope enforcement: Level 4 (SectionHead, SrSupervisor) locked to their own section
                string? sectionFilter = null;
                if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
                {
                    sectionFilter = user.Section;
                }

                var workerList = await _workerDataService.GetWorkersInSection(sectionFilter);

                // Level 5 (Coach): filter hanya coachee yang di-mapping
                if (roleLevel == 5)
                {
                    var coacheeIds = await _context.CoachCoacheeMappings
                        .Where(m => m.CoachId == user.Id && m.IsActive)
                        .Select(m => m.CoacheeId)
                        .ToListAsync();
                    workerList = workerList.Where(w => coacheeIds.Contains(w.WorkerId)).ToList();
                }

                ViewData["WorkerList"] = workerList;

                // Phase 217: Set category data for Team View partial
                var allCats = await _context.AssessmentCategories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .Include(c => c.Children)
                    .ToListAsync();
                var subCategoryMap = allCats
                    .GroupBy(p => p.Name)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
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
            if (user == null) return RedirectToAction("Login", "Account");

            // Own records: always allowed
            if (workerId != user.Id)
            {
                // Level 6 (Coachee): cannot view other workers
                if (roleLevel >= 6) return Forbid();
                // Level 5 (Coach): hanya bisa lihat coachee yang di-mapping
                if (roleLevel == 5)
                {
                    var isCoachee = await _context.CoachCoacheeMappings
                        .AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == workerId && m.IsActive);
                    if (!isCoachee) return Forbid();
                }
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
            var subCategoryMap = allCats
                .GroupBy(p => p.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
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
        public async Task<IActionResult> ExportRecordsTeamAssessment(string? section, string? unit, string? search, string? statusFilter, string? dateFrom, string? dateTo)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (roleLevel >= 6) return Forbid();

            // Scope enforcement: Level 4 locked to their own section
            string? sectionFilter = section;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            DateTime? from = DateTime.TryParse(dateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(dateTo, out var parsedTo) ? parsedTo : null;

            var (assessmentRows, _) = await _workerDataService.GetAllWorkersHistory();

            // Get filtered worker IDs from GetWorkersInSection (with date range)
            var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, null, search, statusFilter, from, to);

            // Level 5 (Coach): filter hanya coachee yang di-mapping
            if (roleLevel == 5)
            {
                var coacheeIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId)
                    .ToListAsync();
                filteredWorkers = filteredWorkers.Where(w => coacheeIds.Contains(w.WorkerId)).ToList();
            }

            var filteredIds = filteredWorkers
                .Select(w => w.WorkerId)
                .ToHashSet();

            var filtered = assessmentRows
                .Where(r => filteredIds.Contains(r.WorkerId))
                .Where(r => from == null || r.Date.Date >= from.Value.Date)
                .Where(r => to == null || r.Date.Date <= to.Value.Date)
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
        public async Task<IActionResult> ExportRecordsTeamTraining(string? section, string? unit, string? search, string? statusFilter, string? category, string? dateFrom, string? dateTo)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (roleLevel >= 6) return Forbid();

            // Scope enforcement: Level 4 locked to their own section
            string? sectionFilter = section;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            DateTime? from = DateTime.TryParse(dateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(dateTo, out var parsedTo) ? parsedTo : null;

            var (_, trainingRows) = await _workerDataService.GetAllWorkersHistory();

            // Get filtered worker IDs from GetWorkersInSection (with date range)
            var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, category, search, statusFilter, from, to);

            // Level 5 (Coach): filter hanya coachee yang di-mapping
            if (roleLevel == 5)
            {
                var coacheeIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId)
                    .ToListAsync();
                filteredWorkers = filteredWorkers.Where(w => coacheeIds.Contains(w.WorkerId)).ToList();
            }

            var filteredIds = filteredWorkers
                .Select(w => w.WorkerId)
                .ToHashSet();

            var filtered = trainingRows
                .Where(r => filteredIds.Contains(r.WorkerId))
                .Where(r => from == null || r.Date.Date >= from.Value.Date)
                .Where(r => to == null || r.Date.Date <= to.Value.Date)
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
        // Phase 239: AJAX partial endpoint for date range filter (returns tbody rows)
        [HttpGet]
        public async Task<IActionResult> RecordsTeamPartial(
            string? section, string? unit, string? category, string? subCategory,
            string? statusFilter, string? dateFrom, string? dateTo)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");
            if (roleLevel >= 6) return Forbid();

            // L4 section lock — enforce server-side
            string? sectionFilter = (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
                ? user.Section : section;

            DateTime? from = DateTime.TryParse(dateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(dateTo, out var parsedTo) ? parsedTo : null;

            var workerList = await _workerDataService.GetWorkersInSection(
                sectionFilter, unit, category, null, statusFilter, from, to);

            // Level 5 (Coach): filter hanya coachee yang di-mapping
            if (roleLevel == 5)
            {
                var coacheeIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId)
                    .ToListAsync();
                workerList = workerList.Where(w => coacheeIds.Contains(w.WorkerId)).ToList();
            }

            return PartialView("_RecordsTeamBody", workerList);
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
            if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
            {
                TempData["Error"] = "Ujian sudah ditutup. Waktu ujian telah berakhir.";
                return RedirectToAction("Assessment");
            }

            // Guard: DurationMinutes must be > 0 to avoid instant expiry
            if (assessment.DurationMinutes <= 0)
            {
                TempData["Error"] = "Durasi ujian belum diatur. Hubungi HC.";
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
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Race condition: another request already created the assignment (double-click/duplicate tab)
                        // Reload the existing assignment and continue
                        _context.ChangeTracker.Clear();
                        assignment = await _context.UserPackageAssignments
                            .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
                        if (assignment == null)
                            throw; // genuinely unexpected — rethrow
                    }
                }

                // BUG-05 fix: compare against actual assigned question count from ShuffledQuestionIds,
                // not packages.Min() which is wrong for cross-package assignments
                int currentQuestionCount = assignment.GetShuffledQuestionIds().Count;

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
                        Options = opts,
                        QuestionType = q.QuestionType ?? "MultipleChoice",
                        MaxCharacters = q.MaxCharacters > 0 ? q.MaxCharacters : 2000
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
                bool isResume = !justStarted;
                int durationSeconds = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
                int elapsedSec = assessment.ElapsedSeconds;

                // Server-authoritative: cross-check DB elapsed dengan wall-clock elapsed sejak StartedAt
                // Fix untuk bug "waktu habis mendadak" dan "timer bertambah saat resume"
                if (!justStarted && assessment.StartedAt.HasValue)
                {
                    int wallClockElapsed = (int)(DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
                    elapsedSec = Math.Max(elapsedSec, wallClockElapsed);
                }

                // Defensive clamp — tidak boleh melebihi durasi total
                elapsedSec = Math.Min(elapsedSec, durationSeconds);

                int remainingSeconds = durationSeconds - elapsedSec;

                ViewBag.IsResume = isResume;
                ViewBag.LastActivePage = assessment.LastActivePage ?? 0;
                ViewBag.ElapsedSeconds = elapsedSec;
                ViewBag.RemainingSeconds = remainingSeconds;
                ViewBag.ExamExpired = isResume && remainingSeconds <= 0;

                // Load previously saved answers for pre-population (package path)
                if (isResume)
                {
                    var allSaved = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == id)
                        .ToListAsync();

                    // MC: questionId -> optionId (single)
                    var savedAnswers = allSaved
                        .Where(r => r.PackageOptionId.HasValue && r.PackageOptionId > 0 && r.TextAnswer == null)
                        .GroupBy(r => r.PackageQuestionId)
                        .ToDictionary(g => g.Key, g => g.First().PackageOptionId ?? 0);
                    ViewBag.SavedAnswers = System.Text.Json.JsonSerializer.Serialize(savedAnswers);

                    // MA: questionId -> comma-separated optionIds
                    var savedMultiAnswers = allSaved
                        .Where(r => r.PackageOptionId.HasValue && r.PackageOptionId > 0)
                        .GroupBy(r => r.PackageQuestionId)
                        .ToDictionary(g => g.Key, g => string.Join(",", g.Select(r => r.PackageOptionId)));
                    ViewBag.SavedMultiAnswers = System.Text.Json.JsonSerializer.Serialize(savedMultiAnswers);

                    // Essay: questionId -> text
                    var savedTextAnswers = allSaved
                        .Where(r => r.TextAnswer != null)
                        .ToDictionary(r => r.PackageQuestionId, r => r.TextAnswer ?? "");
                    ViewBag.SavedTextAnswers = System.Text.Json.JsonSerializer.Serialize(savedTextAnswers);
                }
                else
                {
                    ViewBag.SavedAnswers = "{}";
                    ViewBag.SavedMultiAnswers = "{}";
                    ViewBag.SavedTextAnswers = "{}";
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

            // Mobile page size (D-15): 5 questions per page on mobile devices
            var userAgent = Request.Headers["User-Agent"].ToString();
            if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
            {
                ViewBag.QuestionsPerPage = 5;
            }

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

            // Phase 2 — Fill remaining quota with balanced ET distribution (round-robin per-ET)
            int remaining = K - selectedIds.Count;
            if (remaining > 0)
            {
                int M = etGroups.Count;
                int basePerET = remaining / M;
                int extraCount = remaining % M;
                var extraETs = etGroups.OrderBy(_ => rng.Next()).Take(extraCount).ToHashSet();

                foreach (var et in etGroups)
                {
                    int quota = basePerET + (extraETs.Contains(et) ? 1 : 0);
                    var etCandidates = allQuestions
                        .Where(x => x.Question.ElemenTeknis == et && !selectedIds.Contains(x.Question.Id))
                        .Select(x => x.Question.Id)
                        .ToList();
                    Shuffle(etCandidates, rng);
                    int toTake = Math.Min(quota, etCandidates.Count);
                    foreach (var id in etCandidates.Take(toTake))
                    {
                        selectedIds.Add(id);
                        selectedList.Add(id);
                    }
                }

                // Fallback: jika masih kurang (ET kehabisan soal), ambil dari NULL-ET atau sisa soal manapun
                if (selectedIds.Count < K)
                {
                    var fallbackCandidates = allQuestions
                        .Where(x => !selectedIds.Contains(x.Question.Id))
                        .Select(x => x.Question.Id)
                        .ToList();
                    Shuffle(fallbackCandidates, rng);
                    foreach (var id in fallbackCandidates.Take(K - selectedIds.Count))
                    {
                        selectedIds.Add(id);
                        selectedList.Add(id);
                    }
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
            // Filter out unanswered entries (model binder converts empty string to 0)
            var validAnswers = answers.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            TempData["PendingAnswers"] = System.Text.Json.JsonSerializer.Serialize(validAnswers);
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
            var tempDataAnswers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(answersJson)
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

            // Fallback: if assignmentId missing from TempData, look up from DB
            if (!assignmentId.HasValue)
            {
                var dbAssignment = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
                assignmentId = dbAssignment?.Id;
            }

            // Merge TempData answers with DB-saved answers (PackageUserResponses).
            // TempData contains only the final form submission; DB contains incrementally
            // auto-saved answers. Merge ensures no answers are lost (TempData wins on conflict).
            var allDbResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == id)
                .ToListAsync();

            // MC/MA option-based answers: questionId -> optionId (first row, for backward compat)
            var dbAnswers = allDbResponses
                .Where(r => r.PackageOptionId.HasValue && r.PackageOptionId > 0)
                .GroupBy(r => r.PackageQuestionId)
                .ToDictionary(g => g.Key, g => g.First().PackageOptionId ?? 0);

            // Start with DB answers, overlay TempData answers (TempData is more recent)
            var answers = new Dictionary<int, int>(dbAnswers);
            foreach (var kvp in tempDataAnswers)
            {
                answers[kvp.Key] = kvp.Value;
            }
            // Remove invalid entries (optionId 0 means unanswered)
            answers = answers.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // MA: questionId -> list of optionIds from DB (all rows)
            var dbMultiAnswers = allDbResponses
                .Where(r => r.PackageOptionId.HasValue && r.PackageOptionId > 0)
                .GroupBy(r => r.PackageQuestionId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.PackageOptionId!.Value).ToList());

            // Essay: questionId -> text answer from DB
            var dbTextAnswers = allDbResponses
                .Where(r => r.TextAnswer != null)
                .ToDictionary(r => r.PackageQuestionId, r => r.TextAnswer ?? "");

            // Build summary items
            var summaryItems = new List<ExamSummaryItem>();

            // Check for package path
            var assignment = assignmentId.HasValue
                ? await _context.UserPackageAssignments.FindAsync(assignmentId.Value)
                : null;

            // BUG-09 fix: redirect with message if assignment not found (e.g. direct URL access)
            if (assignment == null)
            {
                TempData["Error"] = "Data ujian tidak ditemukan. Silakan mulai ujian dari halaman Assessment.";
                return RedirectToAction("Assessment");
            }

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
                    var qtype = q.QuestionType ?? "MultipleChoice";

                    if (qtype == "Essay")
                    {
                        var textAnswer = dbTextAnswers.TryGetValue(qId, out var ta) ? ta : null;
                        summaryItems.Add(new ExamSummaryItem
                        {
                            DisplayNumber = num++,
                            QuestionId = qId,
                            QuestionText = q.QuestionText,
                            QuestionType = qtype,
                            TextAnswer = textAnswer
                        });
                    }
                    else if (qtype == "MultipleAnswer")
                    {
                        var selectedOptIds = dbMultiAnswers.TryGetValue(qId, out var oids) ? oids : new List<int>();
                        // Konversi optionId ke huruf berdasarkan urutan options
                        var orderedOptions = q.Options.OrderBy(o => o.Id).ToList();
                        string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H" };
                        var selectedTexts = selectedOptIds
                            .Select(oid => {
                                var idx = orderedOptions.FindIndex(o => o.Id == oid);
                                return idx >= 0 && idx < letters.Length ? letters[idx] : oid.ToString();
                            })
                            .OrderBy(l => l)
                            .ToList();

                        summaryItems.Add(new ExamSummaryItem
                        {
                            DisplayNumber = num++,
                            QuestionId = qId,
                            QuestionText = q.QuestionText,
                            QuestionType = qtype,
                            SelectedOptionTexts = selectedTexts
                        });
                    }
                    else
                    {
                        // MC (default)
                        int? selectedOptId = answers.TryGetValue(qId, out var v) && v > 0 ? v : (int?)null;
                        string? selectedText = selectedOptId.HasValue && optLookup.TryGetValue(selectedOptId.Value, out var opt)
                            ? opt.OptionText
                            : null;

                        summaryItems.Add(new ExamSummaryItem
                        {
                            DisplayNumber = num++,
                            QuestionId = qId,
                            QuestionText = q.QuestionText,
                            QuestionType = qtype,
                            SelectedOptionId = selectedOptId,
                            SelectedOptionText = selectedText
                        });
                    }
                }
            }
            // Legacy path removed (Phase 227 CLEN-02).

            int unansweredCount = summaryItems.Count(s => !s.IsAnswered);

            // Also update TempData with merged answers for SubmitExam form
            TempData["PendingAnswers"] = System.Text.Json.JsonSerializer.Serialize(answers);

            // Check if timer has expired (no time left to go back)
            bool timerExpired = false;
            if (assessment.StartedAt.HasValue && assessment.DurationMinutes > 0)
            {
                var elapsed = (DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
                var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
                timerExpired = elapsed >= allowed;
            }

            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = id;
            ViewBag.AssignmentId = assignmentId;
            ViewBag.UnansweredCount = unansweredCount;
            ViewBag.Answers = answers; // passed to the hidden final-submit form
            ViewBag.TimerExpired = timerExpired;
            return View(summaryItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(int id, Dictionary<int, int> answers, bool isAutoSubmit = false)
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

            // ---- Block incomplete submission (Phase 272) ----
            // Allow incomplete if: (a) isAutoSubmit from client, OR (b) server timer expired
            bool serverTimerExpired = false;
            if (assessment.StartedAt.HasValue && assessment.DurationMinutes > 0)
            {
                var elapsed = (DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
                var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
                serverTimerExpired = elapsed >= allowed;
            }

            if (!isAutoSubmit && !serverTimerExpired)
            {
                var pkgAssign = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
                if (pkgAssign != null)
                {
                    var shuffledQIds = pkgAssign.GetShuffledQuestionIds();
                    int totalQuestions = shuffledQIds.Count;
                    // Count from form answers (MC) + DB responses for Essay/MA not in form
                    int formAnswered = answers.Count(a => a.Value > 0);
                    var dbResponses = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == id && shuffledQIds.Contains(r.PackageQuestionId))
                        .Select(r => r.PackageQuestionId)
                        .Distinct()
                        .ToListAsync();
                    // Merge: questions answered in form OR in DB
                    var allAnsweredQIds = new HashSet<int>(answers.Where(a => a.Value > 0).Select(a => a.Key));
                    foreach (var qId in dbResponses) allAnsweredQIds.Add(qId);
                    int answeredCount = allAnsweredQIds.Count;
                    if (totalQuestions > 0 && answeredCount < totalQuestions)
                    {
                        int unanswered = totalQuestions - answeredCount;
                        TempData["Error"] = $"Masih ada {unanswered} soal yang belum dijawab. Jawab semua soal terlebih dahulu.";
                        return RedirectToAction("ExamSummary", new { id });
                    }
                }
            }

            // ---- Server-side timer enforcement (LIFE-03) ----
            // Grace period: 2 minutes to account for network latency and slow connections.
            // Skip check if StartedAt is null (legacy sessions that existed before Phase 21).
            if (assessment.StartedAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
                int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0) + 2; // 2-minute grace
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
                // Use GroupBy to handle MA questions which have multiple rows per question
                var allExistingResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                var existingResponses = allExistingResponses
                    .GroupBy(r => r.PackageQuestionId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var qId in shuffledIds)
                {
                    if (!questionLookupById.TryGetValue(qId, out var q)) continue;
                    var qtype = q.QuestionType ?? "MultipleChoice";

                    // MC scoring from form answers
                    if (qtype == "MultipleChoice")
                    {
                        int? selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : (int?)null;
                        if (selectedOptId.HasValue)
                        {
                            var selectedOption = q.Options.FirstOrDefault(o => o.Id == selectedOptId.Value);
                            if (selectedOption != null && selectedOption.IsCorrect)
                                totalScore += q.ScoreValue;
                        }

                        // Upsert MC answer only
                        if (existingResponses.TryGetValue(q.Id, out var existingResponse))
                        {
                            existingResponse.PackageOptionId = selectedOptId;
                            existingResponse.SubmittedAt = DateTime.UtcNow;
                        }
                        else if (selectedOptId.HasValue)
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
                    else if (qtype == "MultipleAnswer")
                    {
                        // MA: score from DB responses (already saved via SignalR)
                        var maResponses = allExistingResponses
                            .Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                            .Select(r => r.PackageOptionId!.Value)
                            .ToHashSet();
                        var correctOptIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                        if (maResponses.SetEquals(correctOptIds))
                            totalScore += q.ScoreValue;
                    }
                    // Essay: scored manually by HC (EssayScore), skip here
                }

                // Hitung finalPercentage dari form POST untuk SignalR push (sebelum SaveChanges)
                int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                // Persist PackageUserResponses (upserted answers) — harus sebelum GradingService.GradeAndCompleteAsync
                // karena GradingService grade dari DB, bukan dari form POST (RESEARCH.md anti-pattern)
                await _context.SaveChangesAsync();

                // GradingService: grade dari DB, handle race-condition, ET scores, TrainingRecord, NomorSertifikat, notifikasi
                bool graded = await _gradingService.GradeAndCompleteAsync(assessment);

                if (!graded)
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
                catch (Exception ex)
                {
                    // Swallow all errors — logging must never block exam flow
                    System.Diagnostics.Debug.WriteLine($"LogActivityAsync failed for session {sessionId}: {ex.Message}");
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

            // Register fonts from wwwroot/fonts/ (graceful if missing)
            var fontsPath = Path.Combine(_env.WebRootPath, "fonts");
            try
            {
                if (Directory.Exists(fontsPath))
                {
                    foreach (var fontFile in Directory.GetFiles(fontsPath, "*.ttf"))
                    {
                        using var fontStream = System.IO.File.OpenRead(fontFile);
                        FontManager.RegisterFont(fontStream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Font registration failed, using default fonts");
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
                // SVG content used inline below — no temp file needed
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to create certificate watermark SVG"); }

            try
            {
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

                            // BUG-11 fix: score badge removed from PDF (consistent with HTML Certificate view)

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

            if (pdfStream.Length == 0)
            {
                _logger.LogError("CertificatePdf: Generated PDF has 0 bytes for session {Id}", id);
                TempData["Error"] = "Failed to generate PDF certificate.";
                return RedirectToAction("Results", new { id });
            }

            var nip = assessment.User?.NIP ?? user.Id;
            var safeTitle = Regex.Replace(assessment.Title ?? "Certificate", @"[^a-zA-Z0-9]", "_");
            var year = completedAt.Year;
            var filename = $"Sertifikat_{nip}_{safeTitle}_{year}.pdf";

            return File(pdfStream.ToArray(), "application/pdf", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CertificatePdf generation failed for session {Id}", id);
                TempData["Error"] = "Gagal membuat PDF sertifikat. Silakan coba lagi.";
                return RedirectToAction("Results", new { id });
            }
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

            // === Pre-Post comparison section (per D-11, D-12, D-13, D-14) ===
            ViewBag.HasComparisonSection = false;
            ViewBag.GainScorePending = false;
            ViewBag.ComparisonData = null;

            if (!string.IsNullOrEmpty(assessment.AssessmentType)
                && assessment.AssessmentType == "PostTest"
                && assessment.LinkedSessionId.HasValue)
            {
                var preSessionId = assessment.LinkedSessionId.Value;

                // Security: validasi Pre session exists DAN milik user yang sama (T-299-01 IDOR prevention)
                var preSession = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(s => s.Id == preSessionId);

                // Null-check preSession (review fix: LinkedSessionId bisa menunjuk ke session yang sudah dihapus)
                if (preSession != null && preSession.UserId == assessment.UserId)
                {
                    // D-17: Essay pending check
                    bool gainPending = assessment.HasManualGrading && assessment.IsPassed == null;

                    // Query ET scores untuk kedua session dalam 2 query (bukan N+1)
                    var preEtScores = await _context.SessionElemenTeknisScores
                        .Where(s => s.AssessmentSessionId == preSessionId)
                        .ToListAsync();

                    var postEtScores = await _context.SessionElemenTeknisScores
                        .Where(s => s.AssessmentSessionId == assessment.Id)
                        .ToListAsync();

                    // Pitfall 1: Jika Pre tidak punya ET scores, jangan tampilkan tabel misleading
                    if (preEtScores.Any() && postEtScores.Any())
                    {
                        var comparisonRows = postEtScores
                            .Select(post => {
                                var pre = preEtScores.FirstOrDefault(p => p.ElemenTeknis == post.ElemenTeknis);
                                double preScore = pre != null && pre.QuestionCount > 0
                                    ? Math.Round((double)pre.CorrectCount / pre.QuestionCount * 100, 1) : 0;
                                double postScore = post.QuestionCount > 0
                                    ? Math.Round((double)post.CorrectCount / post.QuestionCount * 100, 1) : 0;

                                // Gain score formula (D-16): (Post - Pre) / (100 - Pre) * 100
                                // Edge case D-16: PreScore >= 100 -> Gain = 100 (avoid DivisionByZero)
                                // Edge case D-17: Essay pending -> null
                                // Note: PreScore = 0 -> formula yields postScore (intended behavior)
                                double? gainScore = null;
                                if (!gainPending)
                                {
                                    gainScore = preScore >= 100
                                        ? 100
                                        : Math.Round((postScore - preScore) / (100 - preScore) * 100, 1);
                                }

                                return new {
                                    ElemenTeknis = post.ElemenTeknis,
                                    PreScore = preScore,
                                    PostScore = postScore,
                                    GainScore = gainScore
                                };
                            })
                            .OrderBy(r => r.ElemenTeknis)
                            .ToList();

                        ViewBag.ComparisonData = comparisonRows;
                        ViewBag.GainScorePending = gainPending;
                        ViewBag.HasComparisonSection = comparisonRows.Any();
                    }
                }
            }

            return View(viewModel);
        }

        #region Helper Methods

        /// <summary>
        /// Returns (user, roleLevel) for the current authenticated user.
        /// Extracts the repeated role-scoping pattern used across multiple actions.
        /// </summary>
        private async Task<(ApplicationUser? User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (null, 0);
            var userRoles = await _userManager.GetRolesAsync(user);
            var roleLevel = UserRoles.GetRoleLevel(userRoles.FirstOrDefault() ?? "");
            return (user, roleLevel);
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
        [Authorize(Roles = UserRoles.RolesAnalytics)]
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
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> GetAnalyticsData(
            string? bagian,
            string? unit,
            string? kategori,
            string? subKategori,
            DateTime? periodeStart,
            DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
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

            // Gain Score Trend — rata-rata gain score per bulan untuk assessment PrePostTest (D-11, D-12, RPT-06)
            var prePostPostSessions = await _context.AssessmentSessions
                .Where(s => s.AssessmentType == "PostTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue
                            && s.LinkedSessionId.HasValue
                            && s.CompletedAt.HasValue
                            && s.CompletedAt >= periodeStart
                            && s.CompletedAt <= periodeEnd)
                .Select(s => new { s.UserId, PostScore = s.Score!.Value, s.LinkedSessionId, PostCompleted = s.CompletedAt!.Value })
                .ToListAsync();

            if (!string.IsNullOrEmpty(bagian))
            {
                var bagianUserIds = await _context.Users
                    .Where(u => u.Section == bagian)
                    .Select(u => u.Id)
                    .ToListAsync();
                prePostPostSessions = prePostPostSessions.Where(p => bagianUserIds.Contains(p.UserId)).ToList();
            }
            if (!string.IsNullOrEmpty(unit))
            {
                var unitUserIds = await _context.Users
                    .Where(u => u.Unit == unit)
                    .Select(u => u.Id)
                    .ToListAsync();
                prePostPostSessions = prePostPostSessions.Where(p => unitUserIds.Contains(p.UserId)).ToList();
            }

            var preSessionIdsForTrend = prePostPostSessions.Select(p => p.LinkedSessionId!.Value).Distinct().ToList();
            var preScoreDict = await _context.AssessmentSessions
                .Where(s => preSessionIdsForTrend.Contains(s.Id) && s.Score.HasValue)
                .ToDictionaryAsync(s => s.Id, s => s.Score!.Value);

            var gainScoreTrend = prePostPostSessions
                .Where(p => preScoreDict.ContainsKey(p.LinkedSessionId!.Value))
                .Select(p =>
                {
                    double pre = preScoreDict[p.LinkedSessionId!.Value];
                    double post = p.PostScore;
                    double gain = pre >= 100 ? 100 : (100 - pre) == 0 ? 0 : (post - pre) / (100 - pre) * 100;
                    return new { p.PostCompleted.Year, p.PostCompleted.Month, Gain = gain };
                })
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new GainScoreTrendItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    AvgGainScore = Math.Round(g.Average(x => x.Gain), 1),
                    SampleCount = g.Count()
                })
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .ToList();

            return Json(new AnalyticsDataResult
            {
                FailRate = failRate,
                Trend = trend,
                EtBreakdown = etBreakdown,
                ExpiringSoon = expiringSoon,
                GainScoreTrend = gainScoreTrend
            });
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> GetAnalyticsCascadeUnits(string bagian)
        {
            return Json(await _context.GetUnitsForSectionAsync(bagian));
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> GetAnalyticsCascadeSubKategori(string kategori)
        {
            var subCategories = await _context.AssessmentCategories
                .Where(c => c.Parent != null && c.Parent.Name == kategori && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();
            return Json(subCategories);
        }

        // ============================================================
        // GET /CMP/GetPrePostAssessmentList — daftar assessment PrePostTest untuk dropdown
        // ============================================================
        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> GetPrePostAssessmentList(string? bagian, string? unit)
        {
            var query = _context.AssessmentSessions
                .Include(s => s.User)
                .Where(s => s.AssessmentType == "PreTest"
                            && s.LinkedGroupId.HasValue);

            if (!string.IsNullOrEmpty(bagian))
                query = query.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                query = query.Where(s => s.User!.Unit == unit);

            var list = await query
                .GroupBy(s => new { s.LinkedGroupId, s.Title })
                .Select(g => new PrePostAssessmentListItem
                {
                    LinkedGroupId = g.Key.LinkedGroupId!.Value,
                    Title = g.Key.Title,
                    TotalWorker = g.Select(s => s.UserId).Distinct().Count()
                })
                .ToListAsync();

            return Json(list);
        }

        // ============================================================
        // GET /CMP/GetItemAnalysisData — item analysis per soal (p-value, D-index, distractor)
        // ============================================================
        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> GetItemAnalysisData(int assessmentGroupId)
        {
            // Ambil semua sesi PreTest completed dalam group ini
            var sessions = await _context.AssessmentSessions
                .Where(s => s.LinkedGroupId == assessmentGroupId
                            && s.AssessmentType == "PreTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue)
                .Select(s => new { s.Id, s.Score })
                .ToListAsync();

            int totalResponden = sessions.Count;

            // Ambil semua responses untuk sesi-sesi ini, beserta navigation properties
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var responses = await _context.PackageUserResponses
                .Include(r => r.PackageQuestion)
                    .ThenInclude(q => q.Options)
                .Include(r => r.PackageOption)
                .Where(r => sessionIds.Contains(r.AssessmentSessionId))
                .ToListAsync();

            // Session scores untuk Kelley discrimination index
            var sessionScores = sessions
                .Select(s => (SessionId: s.Id, TotalScore: (int)(s.Score ?? 0)))
                .ToList();

            var items = new List<ItemAnalysisRow>();
            int qNum = 0;
            foreach (var group in responses.GroupBy(r => r.PackageQuestionId).ToList())
            {
                qNum++;
                var firstResponse = group.First();
                var question = firstResponse.PackageQuestion;
                if (question == null) continue;

                var questionType = question.QuestionType ?? "MultipleChoice";

                // Difficulty Index (p-value): jumlah benar / total (RPT-01)
                // Jawaban benar = PackageOption yang dipilih memiliki IsCorrect == true
                int correctCount = group.Count(r => r.PackageOption != null && r.PackageOption.IsCorrect);
                double pValue = totalResponden > 0 ? (double)correctCount / totalResponden : 0;

                // Discrimination Index — Kelley upper/lower 27% (RPT-02)
                var correctSessionIds = group
                    .Where(r => r.PackageOption != null && r.PackageOption.IsCorrect)
                    .Select(r => r.AssessmentSessionId)
                    .ToList();
                double? dIndex = CalculateKelleyDiscrimination(sessionScores, correctSessionIds);

                // Distractor analysis (RPT-03) — hanya untuk MC/TF
                var distractors = new List<DistractorRow>();
                if (questionType == "MultipleChoice" || questionType == "TrueFalse")
                {
                    var options = question.Options?.ToList() ?? new List<PackageOption>();
                    foreach (var opt in options)
                    {
                        int optCount = group.Count(r => r.PackageOptionId == opt.Id);
                        distractors.Add(new DistractorRow
                        {
                            OptionText = opt.OptionText ?? "",
                            IsCorrect = opt.IsCorrect,
                            Count = optCount,
                            Percent = totalResponden > 0 ? Math.Round((double)optCount / totalResponden * 100, 1) : 0
                        });
                    }
                }

                items.Add(new ItemAnalysisRow
                {
                    QuestionNumber = qNum,
                    QuestionText = question.QuestionText ?? "",
                    QuestionType = questionType,
                    DifficultyIndex = Math.Round(pValue, 3),
                    DiscriminationIndex = dIndex.HasValue ? Math.Round(dIndex.Value, 3) : null,
                    TotalResponden = totalResponden,
                    IsLowN = totalResponden < 30,
                    Distractors = distractors
                });
            }

            return Json(new ItemAnalysisResult
            {
                TotalResponden = totalResponden,
                IsLowN = totalResponden < 30,
                Items = items
            });
        }

        /// <summary>
        /// Kelley discrimination index: (upper 27% correct rate) - (lower 27% correct rate)
        /// </summary>
        private static double? CalculateKelleyDiscrimination(
            List<(int SessionId, int TotalScore)> sessionScores,
            List<int> correctSessionIds)
        {
            int n = sessionScores.Count;
            if (n == 0) return null;

            int groupSize = (int)Math.Ceiling(n * 0.27);
            if (groupSize == 0) return null;

            var sorted = sessionScores.OrderByDescending(s => s.TotalScore).ToList();
            var upperIds = sorted.Take(groupSize).Select(s => s.SessionId).ToHashSet();
            var lowerIds = sorted.TakeLast(groupSize).Select(s => s.SessionId).ToHashSet();

            var correctSet = new HashSet<int>(correctSessionIds);
            double upperCorrect = upperIds.Count(id => correctSet.Contains(id));
            double lowerCorrect = lowerIds.Count(id => correctSet.Contains(id));

            return (upperCorrect / groupSize) - (lowerCorrect / groupSize);
        }

        // ============================================================
        // GET /CMP/GetGainScoreData — gain score per pekerja, per elemen, group comparison
        // ============================================================
        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> GetGainScoreData(int assessmentGroupId)
        {
            // Per Pekerja — pair Pre dan Post by UserId (D-07 view 1, RPT-04)
            var preSessions = await _context.AssessmentSessions
                .Include(s => s.User)
                .Where(s => s.LinkedGroupId == assessmentGroupId
                            && s.AssessmentType == "PreTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue)
                .ToListAsync();

            var postSessionDict = await _context.AssessmentSessions
                .Where(s => s.LinkedGroupId == assessmentGroupId
                            && s.AssessmentType == "PostTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue)
                .ToDictionaryAsync(s => s.UserId, s => s);

            var perWorker = new List<GainScorePerWorker>();
            foreach (var pre in preSessions)
            {
                if (!postSessionDict.TryGetValue(pre.UserId, out var post)) continue;
                double preScore = pre.Score ?? 0;
                double postScore = post.Score ?? 0;
                // Gain score formula: (Post - Pre) / (100 - Pre) * 100
                // PreScore = 100 → Gain = 100 (WKPPT-06)
                double gainScore = preScore >= 100 ? 100 : (100 - preScore) == 0 ? 0 : Math.Round((postScore - preScore) / (100 - preScore) * 100, 1);

                perWorker.Add(new GainScorePerWorker
                {
                    NamaPekerja = pre.User?.FullName ?? pre.User?.UserName ?? "",
                    NIP = pre.User?.NIP ?? "",
                    Section = pre.User?.Section ?? "Tidak Diketahui",
                    PreScore = Math.Round(preScore, 1),
                    PostScore = Math.Round(postScore, 1),
                    GainScore = gainScore
                });
            }

            // Per Elemen Kompetensi — dari SessionElemenTeknisScores (D-07 view 2, RPT-04)
            var preSessionIds = preSessions.Select(s => s.Id).ToList();
            var postSessionIds = postSessionDict.Values.Select(s => s.Id).ToList();

            var preEt = await _context.SessionElemenTeknisScores
                .Where(e => preSessionIds.Contains(e.AssessmentSessionId))
                .ToListAsync();
            var postEt = await _context.SessionElemenTeknisScores
                .Where(e => postSessionIds.Contains(e.AssessmentSessionId))
                .ToListAsync();

            var preEtAvg = preEt.GroupBy(e => e.ElemenTeknis)
                .ToDictionary(g => g.Key, g => g.Average(e => e.QuestionCount > 0 ? (double)e.CorrectCount * 100.0 / e.QuestionCount : 0));
            var postEtAvg = postEt.GroupBy(e => e.ElemenTeknis)
                .ToDictionary(g => g.Key, g => g.Average(e => e.QuestionCount > 0 ? (double)e.CorrectCount * 100.0 / e.QuestionCount : 0));

            var allEt = preEtAvg.Keys.Union(postEtAvg.Keys).OrderBy(k => k);
            var perElemen = allEt.Select(et =>
            {
                double avgPre = preEtAvg.GetValueOrDefault(et, 0);
                double avgPost = postEtAvg.GetValueOrDefault(et, 0);
                double avgGain = avgPre >= 100 ? 100 : (100 - avgPre) == 0 ? 0 : Math.Round((avgPost - avgPre) / (100 - avgPre) * 100, 1);
                return new GainScorePerElemen
                {
                    ElemenTeknis = et,
                    AvgPre = Math.Round(avgPre, 1),
                    AvgPost = Math.Round(avgPost, 1),
                    AvgGain = avgGain
                };
            }).ToList();

            // Group Comparison — per Bagian (RPT-07)
            var groupComparison = perWorker
                .GroupBy(g => g.Section)
                .Select(grp => new GroupComparisonItem
                {
                    GroupName = grp.Key,
                    WorkerCount = grp.Count(),
                    AvgPreScore = Math.Round(grp.Average(g => g.PreScore), 1),
                    AvgPostScore = Math.Round(grp.Average(g => g.PostScore), 1),
                    AvgGainScore = Math.Round(grp.Average(g => g.GainScore), 1)
                })
                .OrderByDescending(g => g.AvgGainScore)
                .ToList();

            return Json(new GainScoreResult
            {
                PerWorker = perWorker,
                PerElemen = perElemen,
                GroupComparison = groupComparison
            });
        }

        // ============================================================
        // GET /CMP/ExportItemAnalysisExcel — export Item Analysis ke .xlsx (RPT-05, D-09, D-10)
        // ============================================================
        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> ExportItemAnalysisExcel(int assessmentGroupId)
        {
            var sessions = await _context.AssessmentSessions
                .Where(s => s.LinkedGroupId == assessmentGroupId
                            && s.AssessmentType == "PreTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue)
                .Select(s => new { s.Id, s.Score })
                .ToListAsync();

            int totalResponden = sessions.Count;
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var responses = await _context.PackageUserResponses
                .Include(r => r.PackageQuestion)
                    .ThenInclude(q => q.Options)
                .Include(r => r.PackageOption)
                .Where(r => sessionIds.Contains(r.AssessmentSessionId))
                .ToListAsync();

            var sessionScores = sessions
                .Select(s => (SessionId: s.Id, TotalScore: (int)(s.Score ?? 0)))
                .ToList();

            using var wb = new XLWorkbook();

            // Sheet 1: Item Analysis
            var headers1 = new[] { "No", "Soal", "P-Value", "Interpretasi", "D-Index", "N Responden" };
            var ws1 = ExcelExportHelper.CreateSheet(wb, "Item Analysis", headers1);
            ws1.SheetView.FreezeRows(1);
            ws1.Range(1, 1, 1, headers1.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
            ws1.Range(1, 1, 1, headers1.Length).Style.Font.SetFontColor(XLColor.White);

            int row1 = 2;
            int qNum = 0;
            var questionGroups = responses.GroupBy(r => r.PackageQuestionId).ToList();
            foreach (var group in questionGroups)
            {
                qNum++;
                var question = group.First().PackageQuestion;
                if (question == null) continue;

                int correctCount = group.Count(r => r.PackageOption != null && r.PackageOption.IsCorrect);
                double pValue = totalResponden > 0 ? (double)correctCount / totalResponden : 0;
                var correctSessionIds = group.Where(r => r.PackageOption != null && r.PackageOption.IsCorrect).Select(r => r.AssessmentSessionId).ToList();
                double? dIndex = CalculateKelleyDiscrimination(sessionScores, correctSessionIds);

                string interpretasi = pValue > 0.70 ? "Mudah" : pValue >= 0.30 ? "Sedang" : "Sulit";

                ws1.Cell(row1, 1).Value = qNum;
                ws1.Cell(row1, 2).Value = question.QuestionText ?? "";
                ws1.Cell(row1, 3).Value = Math.Round(pValue, 3);
                ws1.Cell(row1, 4).Value = interpretasi;
                ws1.Cell(row1, 5).Value = dIndex.HasValue ? Math.Round(dIndex.Value, 3) : 0;
                ws1.Cell(row1, 6).Value = totalResponden;
                row1++;
            }

            // Sheet 2: Distractor Analysis
            var headers2 = new[] { "No Soal", "Opsi", "Jawaban Benar", "Jumlah Pemilih", "Persentase" };
            var ws2 = ExcelExportHelper.CreateSheet(wb, "Distractor Analysis", headers2);
            ws2.SheetView.FreezeRows(1);
            ws2.Range(1, 1, 1, headers2.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
            ws2.Range(1, 1, 1, headers2.Length).Style.Font.SetFontColor(XLColor.White);

            int row2 = 2;
            qNum = 0;
            foreach (var group in questionGroups)
            {
                qNum++;
                var question = group.First().PackageQuestion;
                if (question == null) continue;
                var questionType = question.QuestionType ?? "MultipleChoice";
                if (questionType != "MultipleChoice" && questionType != "TrueFalse") continue;

                var options = question.Options?.ToList() ?? new List<PackageOption>();
                foreach (var opt in options)
                {
                    int optCount = group.Count(r => r.PackageOptionId == opt.Id);
                    ws2.Cell(row2, 1).Value = qNum;
                    ws2.Cell(row2, 2).Value = opt.OptionText ?? "";
                    ws2.Cell(row2, 3).Value = opt.IsCorrect ? "Ya" : "Tidak";
                    ws2.Cell(row2, 4).Value = optCount;
                    ws2.Cell(row2, 5).Value = totalResponden > 0 ? Math.Round((double)optCount / totalResponden * 100, 1) : 0;
                    if (opt.IsCorrect)
                        ws2.Range(row2, 1, row2, 5).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#d1e7dd"));
                    row2++;
                }
            }

            return ExcelExportHelper.ToFileResult(wb,
                $"ItemAnalysis_{assessmentGroupId}_{DateTime.Now:yyyyMMdd}.xlsx", this);
        }

        // ============================================================
        // GET /CMP/ExportGainScoreExcel — export Gain Score ke .xlsx (RPT-05, D-09, D-10)
        // ============================================================
        [HttpGet]
        [Authorize(Roles = UserRoles.RolesAnalytics)]
        public async Task<IActionResult> ExportGainScoreExcel(int assessmentGroupId)
        {
            var preSessions = await _context.AssessmentSessions
                .Include(s => s.User)
                .Where(s => s.LinkedGroupId == assessmentGroupId
                            && s.AssessmentType == "PreTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue)
                .ToListAsync();

            var postSessionDict = await _context.AssessmentSessions
                .Where(s => s.LinkedGroupId == assessmentGroupId
                            && s.AssessmentType == "PostTest"
                            && s.Status == "Completed"
                            && s.Score.HasValue)
                .ToDictionaryAsync(s => s.UserId, s => s);

            using var wb = new XLWorkbook();

            // Sheet 1: Per Pekerja
            var headers1 = new[] { "Nama Pekerja", "NIP", "Bagian", "Pre Score", "Post Score", "Gain Score" };
            var ws1 = ExcelExportHelper.CreateSheet(wb, "Per Pekerja", headers1);
            ws1.SheetView.FreezeRows(1);
            ws1.Range(1, 1, 1, headers1.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
            ws1.Range(1, 1, 1, headers1.Length).Style.Font.SetFontColor(XLColor.White);

            int row1 = 2;
            foreach (var pre in preSessions)
            {
                if (!postSessionDict.TryGetValue(pre.UserId, out var post)) continue;
                double preScore = pre.Score ?? 0;
                double postScore = post.Score ?? 0;
                double gainScore = preScore >= 100 ? 100 : (100 - preScore) == 0 ? 0 : Math.Round((postScore - preScore) / (100 - preScore) * 100, 1);

                ws1.Cell(row1, 1).Value = pre.User?.FullName ?? pre.User?.UserName ?? "";
                ws1.Cell(row1, 2).Value = pre.User?.NIP ?? "";
                ws1.Cell(row1, 3).Value = pre.User?.Section ?? "";
                ws1.Cell(row1, 4).Value = Math.Round(preScore, 1);
                ws1.Cell(row1, 5).Value = Math.Round(postScore, 1);
                ws1.Cell(row1, 6).Value = gainScore;
                row1++;
            }

            // Sheet 2: Per Elemen Kompetensi
            var preSessionIds = preSessions.Select(s => s.Id).ToList();
            var postSessionIds = postSessionDict.Values.Select(s => s.Id).ToList();

            var preEt = await _context.SessionElemenTeknisScores
                .Where(e => preSessionIds.Contains(e.AssessmentSessionId))
                .ToListAsync();
            var postEt = await _context.SessionElemenTeknisScores
                .Where(e => postSessionIds.Contains(e.AssessmentSessionId))
                .ToListAsync();

            var preEtAvg = preEt.GroupBy(e => e.ElemenTeknis)
                .ToDictionary(g => g.Key, g => g.Average(e => e.QuestionCount > 0 ? (double)e.CorrectCount * 100.0 / e.QuestionCount : 0));
            var postEtAvg = postEt.GroupBy(e => e.ElemenTeknis)
                .ToDictionary(g => g.Key, g => g.Average(e => e.QuestionCount > 0 ? (double)e.CorrectCount * 100.0 / e.QuestionCount : 0));

            var headers2 = new[] { "Elemen Kompetensi", "Avg Pre", "Avg Post", "Avg Gain" };
            var ws2 = ExcelExportHelper.CreateSheet(wb, "Per Elemen Kompetensi", headers2);
            ws2.SheetView.FreezeRows(1);
            ws2.Range(1, 1, 1, headers2.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
            ws2.Range(1, 1, 1, headers2.Length).Style.Font.SetFontColor(XLColor.White);

            int row2 = 2;
            foreach (var et in preEtAvg.Keys.Union(postEtAvg.Keys).OrderBy(k => k))
            {
                double avgPre = preEtAvg.GetValueOrDefault(et, 0);
                double avgPost = postEtAvg.GetValueOrDefault(et, 0);
                double avgGain = avgPre >= 100 ? 100 : (100 - avgPre) == 0 ? 0 : Math.Round((avgPost - avgPre) / (100 - avgPre) * 100, 1);

                ws2.Cell(row2, 1).Value = et;
                ws2.Cell(row2, 2).Value = Math.Round(avgPre, 1);
                ws2.Cell(row2, 3).Value = Math.Round(avgPost, 1);
                ws2.Cell(row2, 4).Value = avgGain;
                row2++;
            }

            return ExcelExportHelper.ToFileResult(wb,
                $"GainScore_{assessmentGroupId}_{DateTime.Now:yyyyMMdd}.xlsx", this);
        }

        // ============================================================
        // Cascade helpers for CertificationManagement filters
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetCascadeOptions(string? section)
        {
            var units = string.IsNullOrEmpty(section) ? new List<string>() : await _context.GetUnitsForSectionAsync(section);
            return Json(new { units });
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories(string? category)
        {
            if (string.IsNullOrEmpty(category))
                return Json(new List<string>());

            var subCategories = await _context.AssessmentCategories
                .Where(c => c.ParentId != null && c.Parent!.Name == category && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();

            return Json(subCategories);
        }

        // ============================================================
        // CertificationManagement — dipindah dari CDPController
        // ============================================================

        public async Task<IActionResult> CertificationManagement(int page = 1)
        {
            var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);

            var groups = BuildSertifikatGroups(allRows);

            var vm = BuildGroupViewModel(groups, roleLevel);

            var paging = PaginationHelper.Calculate(groups.Count, page, vm.PageSize);
            vm.Groups = groups.Skip(paging.Skip).Take(paging.Take).ToList();
            vm.CurrentPage = paging.CurrentPage;
            vm.TotalPages = paging.TotalPages;

            ViewBag.AllCategories = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> FilterCertificationManagement(
            string? category = null,
            string? subCategory = null,
            string? search = null,
            int page = 1)
        {
            var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);

            var groups = BuildSertifikatGroups(allRows);

            if (!string.IsNullOrEmpty(category))
                groups = groups.Where(g => g.Kategori == category).ToList();
            if (!string.IsNullOrEmpty(subCategory))
                groups = groups.Where(g => g.SubKategori == subCategory).ToList();
            if (!string.IsNullOrEmpty(search))
                groups = groups.Where(g => g.Judul.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            var vm = BuildGroupViewModel(groups, roleLevel);

            var paging = PaginationHelper.Calculate(groups.Count, page, vm.PageSize);
            vm.Groups = groups.Skip(paging.Skip).Take(paging.Take).ToList();
            vm.CurrentPage = paging.CurrentPage;
            vm.TotalPages = paging.TotalPages;

            return PartialView("Shared/_SertifikatGroupTablePartial", vm);
        }

        public async Task<IActionResult> CertificationManagementDetail(string judul, int page = 1)
        {
            var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);
            var filtered = allRows.Where(r => r.Judul == judul).ToList();

            var first = filtered.FirstOrDefault();
            var vm = new SertifikatDetailViewModel
            {
                Judul = judul,
                Kategori = first?.Kategori,
                SubKategori = first?.SubKategori,
                TotalCount = filtered.Count,
                AktifCount = filtered.Count(r => r.Status == CertificateStatus.Aktif),
                AkanExpiredCount = filtered.Count(r => r.Status == CertificateStatus.AkanExpired),
                ExpiredCount = filtered.Count(r => r.Status == CertificateStatus.Expired),
                PermanentCount = filtered.Count(r => r.Status == CertificateStatus.Permanent),
                RoleLevel = roleLevel
            };

            var paging = PaginationHelper.Calculate(filtered.Count, page, vm.PageSize);
            vm.Rows = filtered.Skip(paging.Skip).Take(paging.Take).ToList();
            vm.CurrentPage = paging.CurrentPage;
            vm.TotalPages = paging.TotalPages;

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            ViewBag.AllBagian = sectionUnitsDict.Keys.ToList();
            ViewBag.UserBagian = (await GetCurrentUserRoleLevelAsync()).User.Section;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> FilterCertificationManagementDetail(
            string judul,
            string? bagian = null,
            string? unit = null,
            string? status = null,
            int page = 1)
        {
            var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);
            var filtered = allRows.Where(r => r.Judul == judul).ToList();

            if (!string.IsNullOrEmpty(bagian))
                filtered = filtered.Where(r => r.Bagian == bagian).ToList();
            if (!string.IsNullOrEmpty(unit))
                filtered = filtered.Where(r => r.Unit == unit).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
                filtered = filtered.Where(r => r.Status == st).ToList();

            var pageSize = 20;
            var paging = PaginationHelper.Calculate(filtered.Count, page, pageSize);

            var vm = new CertificationManagementViewModel
            {
                Rows = filtered.Skip(paging.Skip).Take(paging.Take).ToList(),
                TotalCount = filtered.Count,
                AktifCount = filtered.Count(r => r.Status == CertificateStatus.Aktif),
                AkanExpiredCount = filtered.Count(r => r.Status == CertificateStatus.AkanExpired),
                ExpiredCount = filtered.Count(r => r.Status == CertificateStatus.Expired),
                PermanentCount = filtered.Count(r => r.Status == CertificateStatus.Permanent),
                CurrentPage = paging.CurrentPage,
                TotalPages = paging.TotalPages,
                PageSize = pageSize,
                RoleLevel = roleLevel
            };

            return PartialView("Shared/_CertificationManagementTablePartial", vm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportSertifikatExcel(
            string? category = null,
            string? subCategory = null,
            string? search = null)
        {
            var (allRows, _) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);

            var groups = BuildSertifikatGroups(allRows);

            if (!string.IsNullOrEmpty(category))
                groups = groups.Where(g => g.Kategori == category).ToList();
            if (!string.IsNullOrEmpty(subCategory))
                groups = groups.Where(g => g.SubKategori == subCategory).ToList();
            if (!string.IsNullOrEmpty(search))
                groups = groups.Where(g => g.Judul.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Sertifikat", new[]
            {
                "No", "Nama Sertifikat", "Kategori", "Sub Kategori", "Jumlah Worker"
            });

            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = g.Judul;
                ws.Cell(row, 3).Value = g.Kategori ?? "";
                ws.Cell(row, 4).Value = g.SubKategori ?? "";
                ws.Cell(row, 5).Value = g.JumlahWorker;
            }

            var fileName = $"Sertifikat_Grouped_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportSertifikatDetailExcel(
            string judul,
            string? bagian = null,
            string? unit = null,
            string? status = null)
        {
            var (allRows, _) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);
            var filtered = allRows.Where(r => r.Judul == judul).ToList();

            if (!string.IsNullOrEmpty(bagian))
                filtered = filtered.Where(r => r.Bagian == bagian).ToList();
            if (!string.IsNullOrEmpty(unit))
                filtered = filtered.Where(r => r.Unit == unit).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var stx))
                filtered = filtered.Where(r => r.Status == stx).ToList();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Detail", new[]
            {
                "No", "Nama Worker", "Bagian", "Unit", "Tipe", "Status",
                "Valid Until", "Nomor Sertifikat", "Sertifikat URL"
            });

            for (int i = 0; i < filtered.Count; i++)
            {
                var r = filtered[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = r.NamaWorker;
                ws.Cell(row, 3).Value = r.Bagian ?? "";
                ws.Cell(row, 4).Value = r.Unit ?? "";
                ws.Cell(row, 5).Value = r.RecordType.ToString();
                ws.Cell(row, 6).Value = r.Status.ToString();
                ws.Cell(row, 7).Value = r.ValidUntil?.ToString("dd MMM yyyy") ?? "";
                ws.Cell(row, 8).Value = r.NomorSertifikat ?? "";
                ws.Cell(row, 9).Value = r.SertifikatUrl ?? "";
            }

            var safeJudul = string.Join("_", judul.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"Sertifikat_{safeJudul}_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        private static List<SertifikatGroupRow> BuildSertifikatGroups(List<SertifikatRow> allRows)
        {
            return allRows
                .GroupBy(r => r.Judul)
                .Select(g => new SertifikatGroupRow
                {
                    Judul = g.Key,
                    Kategori = g.First().Kategori,
                    SubKategori = g.First().SubKategori,
                    JumlahWorker = g.Select(r => r.WorkerId).Distinct().Count()
                })
                .OrderBy(g => g.Judul)
                .ToList();
        }

        private static SertifikatGroupViewModel BuildGroupViewModel(List<SertifikatGroupRow> groups, int roleLevel)
        {
            return new SertifikatGroupViewModel
            {
                TotalCount = groups.Count,
                MandatoryCount = groups.Count(g => string.Equals(g.Kategori, "Mandatory HSSE Training", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(g.Kategori, "MANDATORY", StringComparison.OrdinalIgnoreCase)),
                NonMandatoryCount = groups.Count(g => string.Equals(g.Kategori, "NON MANDATORY", StringComparison.OrdinalIgnoreCase)),
                OjtCount = groups.Count(g => string.Equals(g.Kategori, "OJT", StringComparison.OrdinalIgnoreCase)),
                IhtCount = groups.Count(g => string.Equals(g.Kategori, "IHT", StringComparison.OrdinalIgnoreCase)),
                RoleLevel = roleLevel
            };
        }

        private static string MapKategori(string? raw, Dictionary<string, string>? rawToDisplayMap)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            var trimmed = raw.Trim();
            if (rawToDisplayMap != null && rawToDisplayMap.TryGetValue(trimmed.ToUpperInvariant(), out var displayName))
                return displayName;
            return trimmed;
        }

        private async Task<(List<SertifikatRow> rows, int roleLevel)> BuildSertifikatRowsAsync(bool l5OwnDataOnly = false)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();

            // Build scoped user ID list based on role level
            List<string>? scopedUserIds;
            if (UserRoles.HasFullAccess(roleLevel))
            {
                // L1-3: full access — no filter
                scopedUserIds = null;
            }
            else if (UserRoles.HasSectionAccess(roleLevel))
            {
                // L4: see own section only
                scopedUserIds = await _context.Users
                    .Where(u => u.IsActive && u.Section == user.Section)
                    .Select(u => u.Id)
                    .ToListAsync();
            }
            else if (roleLevel == 5)
            {
                if (l5OwnDataOnly)
                {
                    scopedUserIds = new List<string> { user.Id };
                }
                else
                {
                    // L5: coach sees mapped coachees + own data
                    var coacheeIds = await _context.CoachCoacheeMappings
                        .Where(m => m.CoachId == user.Id && m.IsActive)
                        .Select(m => m.CoacheeId)
                        .ToListAsync();
                    coacheeIds.Add(user.Id);
                    scopedUserIds = coacheeIds;
                }
            }
            else
            {
                // L6: own data only
                scopedUserIds = new List<string> { user.Id };
            }

            // Query TrainingRecords with certificate
            var trQuery = _context.TrainingRecords
                .Include(t => t.User)
                .Where(t => t.SertifikatUrl != null);
            if (scopedUserIds != null)
                trQuery = trQuery.Where(t => scopedUserIds.Contains(t.UserId));

            var trainingAnon = await trQuery
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
            var renewedByAsSessionIds = await _context.AssessmentSessions
                .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsSessionId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedByTrSessionIds = await _context.TrainingRecords
                .Where(t => t.RenewsSessionId.HasValue)
                .Select(t => t.RenewsSessionId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedByAsTrainingIds = await _context.AssessmentSessions
                .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsTrainingId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedByTrTrainingIds = await _context.TrainingRecords
                .Where(t => t.RenewsTrainingId.HasValue)
                .Select(t => t.RenewsTrainingId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedAssessmentSessionIds = new HashSet<int>(renewedByAsSessionIds);
            renewedAssessmentSessionIds.UnionWith(renewedByTrSessionIds);

            var renewedTrainingRecordIds = new HashSet<int>(renewedByAsTrainingIds);
            renewedTrainingRecordIds.UnionWith(renewedByTrTrainingIds);

            // Build rawToDisplayMap for MapKategori
            var allCatsForMap = await _context.AssessmentCategories
                .Where(c => c.IsActive && c.ParentId == null)
                .Select(c => new { c.Name })
                .ToListAsync();
            var rawToDisplayMap = allCatsForMap
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
            var asQuery = _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.GenerateCertificate && a.IsPassed == true);
            if (scopedUserIds != null)
                asQuery = asQuery.Where(a => scopedUserIds.Contains(a.UserId));

            var allCategories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
            var categoryById = allCategories.ToDictionary(c => c.Id);
            var categoryNameLookup = allCategories
                .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
                .ToDictionary(c => c.Name, c => categoryById[c.ParentId!.Value].Name);

            var assessmentAnon = await asQuery
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

            var rows = new List<SertifikatRow>(trainingRows.Count + assessmentRows.Count);
            rows.AddRange(trainingRows);
            rows.AddRange(assessmentRows);
            return (rows, roleLevel);
        }

    }
}
