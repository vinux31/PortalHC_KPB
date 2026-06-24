using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
// PositionTargetHelper removed in Phase 90 (KKJ tables dropped)
using System.Data.Common;
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
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus; // Phase 382 — single-source status label

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
        private readonly ImpersonationService _impersonationService;
        private readonly RetakeService _retakeService;

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
            GradingService gradingService,
            ImpersonationService impersonationService,
            RetakeService retakeService)
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
            _impersonationService = impersonationService;
            _retakeService = retakeService;
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
            // Respect impersonation: override role level if impersonating
            var userLevel = _impersonationService.GetEffectiveRoleLevel() ?? currentUser?.RoleLevel ?? 6;

            // Load all bagians (top-level OrganizationUnits) ordered by DisplayOrder
            var allBagians = await _context.OrganizationUnits
                .AsNoTracking()
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
                .AsNoTracking()
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
            var kkjFilesByBagian = kkjFiles
                .GroupBy(f => f.OrganizationUnitId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Load CPDP files (non-archived) grouped by OrganizationUnitId
            var cpdpFiles = await _context.CpdpFiles
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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

            // Get current user — Phase 377: effective user (impersonate user X → assessment X; mode-role → kosong, bukan admin).
            var (user, _) = await GetCurrentUserRoleLevelAsync();
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
                pairedGroups.Add(new { Pre = (dynamic)pre, Post = (dynamic?)post });
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
                .Select(p => p.LinkedGroupId!.Value)
                .Except(prePairs.Where(p => p.LinkedGroupId.HasValue).Select(p => p.LinkedGroupId!.Value))
                .ToList();

            if (postGroupIds.Any())
            {
                var completedPreSessions = await _context.AssessmentSessions
                    .Where(s => s.AssessmentType == "PreTest"
                        && s.UserId == userId                  // GRDF-03 (FLOW-01 fix): hanya Pre milik peserta ini — cegah cross-user pairing
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

            // TOK-02 (WSE-10 / T-382-09): sesi token-required tapi belum lewat lobby token (StartedAt==null)
            // tak boleh menyimpan jawaban (bypass proctoring). StartExam set StartedAt hanya setelah VerifyToken.
            if (ShouldGateMissingStart(session.IsTokenRequired, session.StartedAt))
                return Json(new { success = false, error = "Ujian belum dimulai. Masukkan token melalui halaman ujian." });

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

            // Clamp 3: tidak boleh melebihi durasi total + ExtraTimeMinutes (GRDF-05/FLOW-02 — root fix
            // under-report export "Durasi Aktual"; sebelumnya over-clamp tanpa ExtraTime).
            clampedElapsed = Math.Min(clampedElapsed, ExamTimeRules.AllowedExamSeconds(session.DurationMinutes, session.ExtraTimeMinutes));

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
            if (user == null)
            {
                // D-03 (Phase 377 Option A): impersonate mode-role → render KOSONG + hint, BUKAN redirect Login (Pitfall 1)
                // dan BUKAN identitas/data admin (User=null — leak identitas dilarang). Selain itu (genuinely null) → Login.
                if (_impersonationService.IsImpersonating() && _impersonationService.GetMode() == "role")
                {
                    ViewBag.ImpersonateRoleHint = "Pilih user spesifik untuk melihat data worker.";
                    ViewBag.ActualCategoriesJson = "[]";
                    var emptyVm = new HcPortal.Models.ViewModels.CMPRecordsViewModel
                    {
                        User = null,
                        RoleLevel = roleLevel,
                        UnifiedRecords = new List<HcPortal.Models.UnifiedTrainingRecord>(),
                        AssessmentCount = 0,
                        TrainingCount = 0,
                        TotalCount = 0,
                        YearOptions = new List<int>()
                    };
                    return View("Records", emptyVm);
                }
                return RedirectToAction("Login", "Account");
            }

            var unified = await _workerDataService.GetUnifiedRecords(user.Id);

            // Phase 337 CMP-23: memoize year list di controller (sebelumnya hitung inline di view)
            var yearOptions = unified.Select(r => r.Date.Year).Distinct().OrderByDescending(y => y).ToList();

            // Phase 337 CMP-21/22: encapsulate ke ViewModel single-source roleLevel
            var vm = new HcPortal.Models.ViewModels.CMPRecordsViewModel
            {
                User = user,
                RoleLevel = roleLevel,
                UnifiedRecords = unified,
                AssessmentCount = unified.Count(r => r.RecordType == "Assessment Online"),
                TrainingCount = unified.Count(r => r.RecordType == "Training Manual"),
                TotalCount = unified.Count,
                YearOptions = yearOptions
            };

            // Phase 351 SF-05: opsi Kategori My Records dari record AKTUAL (distinct unified.Kategori),
            // tersedia untuk SEMUA role (di luar block Team-View roleLevel<=4).
            ViewBag.ActualCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
                BuildActualCategories(unified));

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

            return View("Records", vm);
        }

        // Phase 104: Worker Detail page showing unified assessment + training history
        public async Task<IActionResult> RecordsWorkerDetail(string workerId, string? section, string? unit, string? category, string? status, string? search)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");

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
            var subCategoryMap = allCats
                .GroupBy(p => p.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
                );
            ViewBag.SubCategoryMapJson = System.Text.Json.JsonSerializer.Serialize(subCategoryMap);
            // Phase 351 SF-04: opsi Kategori dari record AKTUAL (distinct unifiedRecords.Kategori),
            // bukan master — tampung free-text/legacy + buang opsi mati. SubCategoryMap tetap master.
            ViewBag.ActualCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
                BuildActualCategories(unifiedRecords));

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
            // Phase 377: route ke effective user (impersonate user X → export records X, D-01).
            var (user, _) = await GetCurrentUserRoleLevelAsync();
            if (user == null)
            {
                // mode-role / genuinely-null → tak ada data personal untuk diekspor (D-03 konsisten).
                if (_impersonationService.IsImpersonating() && _impersonationService.GetMode() == "role")
                    return RedirectToAction("Records");
                return Challenge();
            }

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
        public async Task<IActionResult> ExportRecordsTeamAssessment(string? section, string? unit, string? search, string? statusFilter, string? category, string? subCategory, string? dateFrom, string? dateTo, string? searchScope = null)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (roleLevel >= 5) return Forbid();

            // Scope enforcement: Level 4 locked to their own section
            string? sectionFilter = section;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            DateTime? from = DateTime.TryParse(dateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(dateTo, out var parsedTo) ? parsedTo : null;

            // Phase 337 CMP-24/25: get filtered worker IDs first, then SQL push-down ke GetAllWorkersHistory
            var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, category, search, statusFilter, from, to, subCategory, searchScope);
            var filteredIds = filteredWorkers.Select(w => w.WorkerId).ToList();

            var (assessmentRows, _) = await _workerDataService.GetAllWorkersHistory(
                workerIds: filteredIds,
                from: from,
                to: to,
                category: null,         // Assessment tidak filter by category column
                subCategory: null);

            var filtered = assessmentRows;
            if (!string.IsNullOrEmpty(category))
            {
                // SF-06 / D-07: current sessions narrowed by Category (case-insensitive);
                // archived rows (Kategori == null — no Category column) auto-dropped to match on-screen worker-visibility.
                filtered = assessmentRows.Where(r =>
                    !string.IsNullOrEmpty(r.Kategori) &&
                    string.Equals(r.Kategori, category, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

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
                ws.Cell(i + 2, 7).Value = r.IsPassed == true ? "Passed" : (r.IsPassed == false ? "Failed" : AssessmentConstants.AssessmentStatus.PendingGrading);
                ws.Cell(i + 2, 8).Value = r.AttemptNumber?.ToString() ?? "";
            }

            var filename = $"RecordsTeam_Assessment_{DateTime.Now:yyyy-MM-dd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, filename, this);
        }

        // Phase 176: Export team training records as Excel (filtered by current view params)
        [HttpGet]
        public async Task<IActionResult> ExportRecordsTeamTraining(string? section, string? unit, string? search, string? statusFilter, string? category, string? subCategory, string? dateFrom, string? dateTo, string? searchScope = null)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (roleLevel >= 5) return Forbid();

            // Scope enforcement: Level 4 locked to their own section
            string? sectionFilter = section;
            if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
            {
                sectionFilter = user.Section;
            }

            DateTime? from = DateTime.TryParse(dateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(dateTo, out var parsedTo) ? parsedTo : null;

            // Phase 337 CMP-24/25: SQL push-down filter (workerIds + date + category + subCategory)
            var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, category, search, statusFilter, from, to, subCategory, searchScope);
            var filteredIds = filteredWorkers.Select(w => w.WorkerId).ToList();

            var (_, trainingRows) = await _workerDataService.GetAllWorkersHistory(
                workerIds: filteredIds,
                from: from,
                to: to,
                category: category,
                subCategory: subCategory);

            var filtered = trainingRows;

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
        // Phase 239 + 337 CMP-26: AJAX partial endpoint + pagination (numeric pager 20/50/100)
        [HttpGet]
        public async Task<IActionResult> RecordsTeamPartial(
            string? section, string? unit, string? category, string? subCategory,
            string? statusFilter, string? dateFrom, string? dateTo,
            string? search = null, string? searchScope = null,
            int page = 1, int pageSize = 20)
        {
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return RedirectToAction("Login", "Account");
            if (roleLevel >= 5) return Forbid();

            // L4 section lock — enforce server-side
            string? sectionFilter = (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
                ? user.Section : section;

            DateTime? from = DateTime.TryParse(dateFrom, out var parsedFrom) ? parsedFrom : null;
            DateTime? to = DateTime.TryParse(dateTo, out var parsedTo) ? parsedTo : null;

            var workerList = await _workerDataService.GetWorkersInSection(
                sectionFilter, unit, category, search, statusFilter, from, to, subCategory, searchScope);

            // Phase 337 CMP-26 (D-02): pageSize whitelist (20/50/100), default 20 untuk invalid
            var pageSizeValidated = (pageSize == 20 || pageSize == 50 || pageSize == 100) ? pageSize : 20;
            var paging = HcPortal.Helpers.PaginationHelper.Calculate(workerList.Count, page, pageSizeValidated);
            var pagedWorkerList = workerList.Skip(paging.Skip).Take(paging.Take).ToList();

            // X-Pagination header untuk JS parse + render pager
            Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
            {
                paging.CurrentPage,
                paging.TotalPages,
                paging.TotalCount,
                PageSize = paging.Take
            }));

            return PartialView("_RecordsTeamBody", pagedWorkerList);
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

        // WSE-02 (D-01a): defensive both-sides token compare — single source for the VerifyToken gate.
        // Both sides Trim()+ToUpper() so a legacy LOWERCASE stored token (admin edited lowercase) still
        // matches the (client-uppercased) input — auto-heals at read-time, zero DB touch. Pure → unit-testable.
        public static bool AccessTokenMatches(string? stored, string? input)
            => (stored ?? "").Trim().ToUpperInvariant() == (input ?? "").Trim().ToUpperInvariant();

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

            // WSE-02 (D-01a): defensive both-sides compare (auto-heal legacy lowercase-stored tokens).
            if (string.IsNullOrEmpty(token) || !AccessTokenMatches(assessment.AccessToken, token))
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

            // Phase 377: route authz ke effective user (impersonate user X → owner-check pakai X.Id).
            var (user, _) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();

            // Auto-transition: Upcoming → Open when scheduled date+time has arrived in WIB (persisted to DB)
            if (assessment.Status == "Upcoming" && assessment.Schedule <= DateTime.UtcNow.AddHours(7))
            {
                // Phase 377 (Pitfall 3 / T-377-09): write-on-GET guard — JANGAN tulis DB saat impersonasi (read-only invariant).
                if (!_impersonationService.IsImpersonating())
                {
                    assessment.Status = "Open";
                    assessment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
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

            // GRDF-01 (FLOW-04, keputusan bisnis a): Post-Test tak boleh dimulai bila pasangan Pre-nya ADA
            // tapi belum Completed. Worker-only — Admin/HC bypass (monitoring/impersonate), konsisten token gate.
            // Penempatan: SETELAH cek Completed (reload Post selesai tak ke-gate), SEBELUM StartedAt write (no write-on-GET sesi terblok).
            if (assessment.UserId == user.Id)
            {
                var pairedPre = await PrePostPairing.FindPairedPreAsync(_context, assessment);
                if (pairedPre != null && pairedPre.Status != "Completed")   // D-01: Completed saja, BUKAN IsPassed
                {
                    TempData["Error"] = "Selesaikan Pre-Test dulu sebelum mulai Post-Test.";
                    return RedirectToAction("Assessment");
                }
                // pairedPre == null (orphan / Standard / Pre milik user lain) → lewat (D-02 non-destruktif).
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

            // WSE-01 (D-05): paket ADA tapi SEMUA kosong → blokir SEBELUM tulis StartedAt/Status/assignment/SignalR.
            // Cegah 0% Fail palsu (worker submit ujian 0 soal → maxScore=0, auto-grade Fail tanpa recourse).
            // Kasus "zero paket" (tak ada paket sama sekali) tetap ditangani else di bawah (~:1198).
            if (justStarted)
            {
                var preCheckSiblingIds = await _context.AssessmentSessions
                    .Where(s => s.Title == assessment.Title &&
                                s.Category == assessment.Category &&
                                s.Schedule.Date == assessment.Schedule.Date)
                    .Select(s => s.Id)
                    .ToListAsync();
                bool anyPackages = await _context.AssessmentPackages
                    .AnyAsync(p => preCheckSiblingIds.Contains(p.AssessmentSessionId));
                bool anyWithQuestions = anyPackages && await _context.AssessmentPackages
                    .AnyAsync(p => preCheckSiblingIds.Contains(p.AssessmentSessionId) && p.Questions.Any());
                if (anyPackages && !anyWithQuestions)
                {
                    TempData["Error"] = "Ujian belum siap — belum ada soal pada paket. Silakan hubungi Admin atau HC.";
                    return RedirectToAction("Assessment");
                }
            }

            // WSE-05/D-04 (OPS-01/TOK-03): write-on-GET guard, mirror 911-917 — JANGAN tulis state worker saat impersonate.
            if (justStarted && !_impersonationService.IsImpersonating())
            {
                assessment.Status = "InProgress";
                assessment.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // SignalR push: notify HC monitor group that worker started (only on first entry)
            // WSE-05/D-04 (OPS-01/TOK-03): broadcast+log hanya saat worker asli mulai (bukan impersonate).
            if (justStarted && !_impersonationService.IsImpersonating())
            {
                var startBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
                await _hubContext.Clients.Group($"monitor-{startBatchKey}").SendAsync("workerStarted",
                    new { sessionId = assessment.Id, workerName = user.FullName, status = "InProgress" });

                // Activity log: record exam start (fire-and-forget — must never break exam flow)
                LogActivityAsync(assessment.Id, "started");
            }

            // Packages are attached to the representative session (the one HC used when clicking "Packages"),
            // so search across all sibling sessions. WSE-04 (D-01/D-09): type-aware isolation via shared
            // helper — Pre/Post same-day tak saling memungut paket; Standard/''/null tetap satu grup.
            // Helper dipakai IDENTIK di ReshufflePackage + ReshuffleAll → workerIndex konsisten (Phase 373).
            var siblingSessionIds = await _context.AssessmentSessions
                .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(
                    assessment.Title, assessment.Category, assessment.Schedule.Date, assessment.AssessmentType))
                .Select(s => s.Id)
                .ToListAsync();

            // Phase 373: stable worker index for OFF≥2 round-robin. SQL Server does not guarantee
            // row order without ORDER BY (Pitfall 2), so sort in-memory; this same sibling set + order
            // must match the reshuffle endpoints for cross-call determinism (OQ#1). ON-path ignores it.
            var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();
            int workerIndex = sortedSiblingIds.IndexOf(id);

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

                    // Phase 373: build ShuffledQuestionIds via the shared ShuffleEngine, gated on the
                    // per-assessment flag (propagated from the assessment's own session in Phase 372).
                    // ON = canonical existing behavior; OFF = deterministic (1 paket urut / ≥2 paket 1 paket utuh per worker).
                    var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
                        packages, assessment.ShuffleQuestions, workerIndex, rng);

                    // Option shuffle gated on ShuffleOptions (independent flag). OFF → empty dict →
                    // serializes "{}" → view falls back to DB option order. Only the assigned questions need entries.
                    var assignedQuestions = packages.SelectMany(p => p.Questions)
                        .Where(q => shuffledIds.Contains(q.Id));
                    var optionShuffleDict = ShuffleEngine.BuildOptionShuffle(
                        assignedQuestions, assessment.ShuffleOptions, rng);

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

                    // WSE-05/D-06: saat impersonate, JANGAN persist — object in-memory cukup feed view (preview read-only).
                    // Worker asli StartExam nanti → assignment baru ter-create & persist normal (SC#3 deferred-start).
                    if (!_impersonationService.IsImpersonating())
                    {
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

                    // Options in DB order here (base list); per-user reorder applied in view via
                    // ViewBag.OptionShuffle when ShuffleOptions=ON. OFF stores "{}" → view falls back to this DB order.
                    var opts = q.Options.OrderBy(o => o.Id).Select(o => new ExamOptionItem
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText,
                        ImagePath = o.ImagePath,
                        ImageAlt = o.ImageAlt
                    }).ToList();

                    examQuestions.Add(new ExamQuestionItem
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        DisplayNumber = displayNum++,
                        Options = opts,
                        QuestionType = q.QuestionType ?? "MultipleChoice",
                        MaxCharacters = q.MaxCharacters > 0 ? q.MaxCharacters : 2000,
                        ImagePath = q.ImagePath,
                        ImageAlt = q.ImageAlt
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

            // STAT-02 (WSE-08 / T-382-05/06): transisi atomic ber-guard — GANTI TOCTOU lama
            // (read-check status lalu SaveChanges) yang bisa di-race oleh grading/force-close.
            // Ownership (UserId) DIPERTAHANKAN di WHERE (Pitfall 2) → atomic + spoof-proof.
            // Guard (InProgress||Open): sesi sudah Completed/Abandoned/Cancelled/Menunggu Penilaian → 0 baris,
            // verdict graded TIDAK ter-overwrite (StartedAt juga tak disentuh — kolom tak di-SET).
            var rowsAffected = await _context.AssessmentSessions
                .Where(a => a.Id == id && a.UserId == user.Id
                    && (a.Status == S.InProgress || a.Status == S.Open))
                .ExecuteUpdateAsync(a => a
                    .SetProperty(x => x.Status, S.Abandoned)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                TempData["Error"] = "Sesi ujian ini tidak dapat dibatalkan karena sudah selesai atau dinilai.";
                return RedirectToAction("Assessment");
            }

            TempData["Info"] = "Ujian telah dibatalkan. Hubungi HC jika Anda ingin mengulang.";
            return RedirectToAction("Assessment");
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
                            TextAnswer = textAnswer,
                            ImagePath = q.ImagePath,
                            ImageAlt = q.ImageAlt
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
                            SelectedOptionTexts = selectedTexts,
                            ImagePath = q.ImagePath,
                            ImageAlt = q.ImageAlt,
                            OptionImages = q.Options.OrderBy(o => o.Id).Select(o => new ExamSummaryOptionItem
                            {
                                OptionId = o.Id,
                                OptionText = o.OptionText,
                                ImagePath = o.ImagePath,
                                ImageAlt = o.ImageAlt
                            }).ToList()
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
                            SelectedOptionText = selectedText,
                            ImagePath = q.ImagePath,
                            ImageAlt = q.ImageAlt,
                            OptionImages = q.Options.OrderBy(o => o.Id).Select(o => new ExamSummaryOptionItem
                            {
                                OptionId = o.Id,
                                OptionText = o.OptionText,
                                ImagePath = o.ImagePath,
                                ImageAlt = o.ImageAlt
                            }).ToList()
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

            // Phase 313 CR-01 fix: server-generated one-shot token saat timerExpired=true.
            // Token ini di-render ke hidden field di form, lalu di-validate + consume oleh
            // EnsureCanSubmitExamAsync. Tujuan: Tier-1 (no-grace) hanya bypass kalau request
            // berasal dari path auto-submit yang sah (server-issued), BUKAN dari client yang
            // bisa spoof `isAutoSubmit=true` via DevTools.
            string? autoSubmitToken = null;
            if (timerExpired)
            {
                autoSubmitToken = Guid.NewGuid().ToString("N");
                TempData[$"AutoSubmitToken_{id}"] = autoSubmitToken;
            }

            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = id;
            ViewBag.AssignmentId = assignmentId;
            ViewBag.UnansweredCount = unansweredCount;
            ViewBag.Answers = answers; // passed to the hidden final-submit form
            ViewBag.TimerExpired = timerExpired;
            ViewBag.AutoSubmitToken = autoSubmitToken; // Phase 313 CR-01 — null kalau !timerExpired
            return View(summaryItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(int id, [FromForm(Name = "answers")] Dictionary<int, int>? answers, bool isAutoSubmit = false, string? autoSubmitToken = null)
        {
            // Phase 313 F-313-UAT-02 fix: explicit FromForm prefix prevents DictionaryModelBinder fallback
            // dari greedy-binding semua top-level form field ke `answers` saat zero MC answers (auto-submit
            // timer-expired tanpa user pernah jawab). Default-null + normalize ke empty dict supaya kode
            // hilir (Count, ContainsKey) tetap aman tanpa null-guard tambahan.
            answers ??= new Dictionary<int, int>();

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

            // TOK-02 (WSE-10 / T-382-09): gate bersama STAT-01 di awal handler, SEBELUM mutasi. Sesi token-required
            // yang belum lewat lobby token (StartedAt==null) tak boleh submit/grading (bypass proctoring).
            if (ShouldGateMissingStart(assessment.IsTokenRequired, assessment.StartedAt))
            {
                TempData["Error"] = "Ujian belum dimulai. Masukkan token melalui halaman ujian.";
                return RedirectToAction("Assessment");
            }

            // STAT-01 (WSE-07 / T-382-04): tolak resurrection sesi terminal via SubmitExam.
            // Sebelumnya guard hanya `== "Completed"` → sesi Abandoned/Cancelled/PendingGrading bisa
            // di-POST ulang jadi Completed-lulus + sertifikat. Perluas ke seluruh terminal set + audit.
            if (assessment.Status == S.Completed || assessment.Status == S.Abandoned
                || assessment.Status == S.Cancelled || assessment.Status == S.PendingGrading)
            {
                // Reuse WriteSubmitBlockedAuditAsync (try/catch-swallow di dalam helper) — audit jangan block.
                await WriteSubmitBlockedAuditAsync(assessment, TimeSpan.Zero, 0);
                TempData["Error"] = "Sesi ujian ini sudah berakhir dan tidak dapat dikirim ulang.";
                return RedirectToAction("Assessment");
            }

            // ---- Block incomplete submission (Phase 272 + Phase 382 TMR-02 / T-382-08) ----
            // serverTimerExpired di-hitung SERVER-SIDE = otoritas. Client `isAutoSubmit` (raw flag, bisa
            // di-spoof via DevTools) TIDAK lagi boleh jadi SATU-SATUNYA jalan lolos gate incomplete:
            // submit incomplete hanya di-izinkan ketika waktu BENAR-BENAR habis (serverTimerExpired).
            // Submit on-time yang lengkap tetap lolos (answeredCount==totalQuestions, tak masuk branch ini).
            bool serverTimerExpired = false;
            if (assessment.StartedAt.HasValue && assessment.DurationMinutes > 0)
            {
                var elapsed = (DateTime.UtcNow - assessment.StartedAt.Value).TotalSeconds;
                var allowed = (assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0)) * 60;
                serverTimerExpired = elapsed >= allowed;
            }

            if (!serverTimerExpired)
            {
                var pkgAssign = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
                if (pkgAssign != null)
                {
                    var shuffledQIds = pkgAssign.GetShuffledQuestionIds();

                    // GRDF-07 (VAL-03): soal Essay dianggap "terjawab" HANYA bila TextAnswer non-kosong (server-
                    // authoritative), bukan sekadar baris response ada. MC/MA: response ber-opsi di DB atau jawaban form.
                    var qTypeById = await _context.PackageQuestions
                        .Where(q => shuffledQIds.Contains(q.Id))
                        .Select(q => new { q.Id, q.QuestionType })
                        .ToDictionaryAsync(q => q.Id, q => q.QuestionType ?? "MultipleChoice");
                    var dbResp = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == id && shuffledQIds.Contains(r.PackageQuestionId))
                        .Select(r => new { r.PackageQuestionId, HasOption = r.PackageOptionId.HasValue, r.TextAnswer })
                        .ToListAsync();

                    var formAnswered = new HashSet<int>(answers.Where(a => a.Value > 0).Select(a => a.Key));
                    var completion = EvaluateOnTimeCompletion(
                        shuffledQIds,
                        qTypeById,
                        formAnswered,
                        dbResp.Select(r => (r.PackageQuestionId, r.HasOption, r.TextAnswer)).ToList());

                    if (completion.Blocked)
                    {
                        // Pesan ramah, server-authoritative (client flushEssay non-otoritatif — lesson Phase 413).
                        // TIDAK membocorkan kunci/jawaban benar (V5).
                        TempData["Error"] = completion.EmptyEssay
                            ? "Isi semua jawaban essay terlebih dahulu sebelum submit."
                            : $"Masih ada {completion.Unanswered} soal yang belum dijawab. Jawab semua soal terlebih dahulu.";
                        return RedirectToAction("ExamSummary", new { id });
                    }
                }
            }

            // ---- Server-side timer enforcement (LIFE-03 + Phase 313 2-tier TMR-01) ----
            // Phase 313: 2-tier branching — manual reject tanpa grace (D-09), auto reject setelah grace (existing).
            // Helper extraction mirror Phase 312 EnsureCanDeleteAsync pattern (D-04 lock, body-method placement).
            // AssessmentType Manual exclude di-handle dalam helper (D-15 defense-in-depth).
            // Phase 313 CR-01 fix: pass autoSubmitToken (server-issued one-shot) ke helper.
            // Tier-1 enforcement TIDAK lagi trust client `isAutoSubmit` flag — token-based check.
            var timerBlockResult = await EnsureCanSubmitExamAsync(assessment, isAutoSubmit, autoSubmitToken, id);
            if (timerBlockResult != null) return timerBlockResult;

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
                // SAVE-01 (Pitfall 1): pilih baris FINAL per soal (OrderByDescending SubmittedAt) supaya
                // push Score (SignalR) == Score GradingService (yang juga baca FINAL). Tanpa OrderBy,
                // .First() bisa pilih baris basi/duplikat (race multi-tab) → divergen.
                var existingResponses = allExistingResponses
                    .GroupBy(r => r.PackageQuestionId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());

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
                            if (answers.ContainsKey(q.Id)) // guard: jangan null-overwrite jawaban tersimpan untuk soal absent di form
                            {
                                existingResponse.PackageOptionId = selectedOptId;
                                existingResponse.SubmittedAt = DateTime.UtcNow;
                            }
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

                // TMR-03 (WSE-09 / T-382-10): konsumsi AutoSubmitToken HANYA setelah grading sukses (one-shot).
                // EnsureCanSubmitExamAsync sengaja TIDAK remove token (TempData.Keep) → bila grading sempat gagal
                // (DB hiccup), retry masih punya token (tak permanent-reject). Di sini grading sudah commit sukses.
                if (ShouldConsumeAutoSubmitToken(graded))
                {
                    TempData.Remove($"AutoSubmitToken_{id}");
                }

                // MAM-05: GradingService set essay Status=PendingGrading via ExecuteUpdateAsync (bypass change-tracker).
                // Entity `assessment` tidak ter-update — reload status dari DB untuk tahu apakah essay-pending.
                var freshStatus = await _context.AssessmentSessions
                    .Where(s => s.Id == id)
                    .Select(s => s.Status)
                    .FirstAsync();
                bool isPendingGrading = freshStatus == AssessmentConstants.AssessmentStatus.PendingGrading;

                // SignalR push: notify HC monitor group that worker submitted (package path)
                {
                    var submitBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
                    // MAM-05: essay-pending → result "—" + status "Menunggu Penilaian" (hindari verdict prematur yang flip setelah HC nilai).
                    var result = isPendingGrading
                        ? "—"
                        : (finalPercentage >= assessment.PassPercentage ? "Pass" : "Fail");
                    var pushStatus = isPendingGrading
                        ? AssessmentConstants.AssessmentStatus.PendingGrading
                        : "Completed";
                    int totalQuestionsSubmit = shuffledIds.Count;
                    await _hubContext.Clients.Group($"monitor-{submitBatchKey}").SendAsync("workerSubmitted",
                        new { sessionId = id, workerName = user.FullName, score = finalPercentage, result, status = pushStatus, totalQuestions = totalQuestionsSubmit });
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
            try
            {
                var assessment = await _context.AssessmentSessions
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assessment == null) return NotFound();

                // REC-04 (D-01/D-09): owner || roleLevel<=3 || (L4 section-scoped, Section non-null)
                var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
                if (user == null) return Challenge(); // Force login if session expired

                bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section);

                if (!isAuthorized) return Forbid();

                // SUB-01 D-06: normalize submitted status (Completed OR PendingGrading)
                if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status))
                {
                    TempData["Error"] = "Assessment belum selesai.";
                    return RedirectToAction("Assessment");
                }

                // SUB-01 D-07: PendingGrading branch — HARUS sebelum check GenerateCertificate dan IsPassed (Pitfall 3)
                if (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
                {
                    TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";
                    return RedirectToAction("Results", new { id });
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
            catch (DbException ex)
            {
                _logger.LogError(ex, "Certificate view failed for session {Id}", id);
                TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
                return RedirectToAction("Results", new { id });
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Certificate view failed for session {Id}", id);
                TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
                return RedirectToAction("Results", new { id });
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError(ex, "Certificate view failed for session {Id}", id);
                TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
                return RedirectToAction("Results", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Certificate view failed for session {Id}", id);
                TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
                return RedirectToAction("Results", new { id });
            }
        }

        private async Task<PSignViewModel> ResolveCategorySignatory(string? categoryName)
        {
            var fallback = new PSignViewModel { Position = "HC Manager", FullName = "" };
            if (string.IsNullOrWhiteSpace(categoryName)) return fallback;

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ResolveCategorySignatory failed for category {Category}", categoryName);
                return fallback;
            }
        }

        [HttpGet]
        public async Task<IActionResult> CertificatePdf(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // REC-04 (D-01/D-09): owner || roleLevel<=3 || (L4 section-scoped, Section non-null)
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return Challenge();

            bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section);
            if (!isAuthorized) return Forbid();

            // SUB-01 D-06: normalize submitted status (Completed OR PendingGrading)
            if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status))
            {
                TempData["Error"] = "Assessment belum selesai.";
                return RedirectToAction("Assessment");
            }

            // SUB-01 D-07: PendingGrading branch — HARUS sebelum check GenerateCertificate dan IsPassed (Pitfall 3)
            if (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
            {
                TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";
                return RedirectToAction("Results", new { id });
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

            // REC-04 (D-01/D-09): owner || roleLevel<=3 || (L4 section-scoped, Section non-null)
            var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
            if (user == null) return Challenge();
            bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section);
            if (!isAuthorized) return Forbid();

            // SUB-01 D-06: normalize submitted status (Completed OR PendingGrading)
            if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status))
            {
                TempData["Error"] = "Assessment belum selesai.";
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
                // SURF-317-A fix (Phase 318 Plan 02): ToDictionary throws ArgumentException
                // untuk MA multi-row-per-question (Hubs/AssessmentHub.cs SaveMultipleAnswer
                // insert 1 row per selected option). ToLookup safe grouping.
                var responseLookup = packageResponses.ToLookup(r => r.PackageQuestionId);

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

                        // SURF-317-A fix: multi-row aggregation via ToLookup
                        var userResponses = responseLookup[qId].ToList();
                        var selectedOptionIds = userResponses
                            .Where(r => r.PackageOptionId != null)
                            .Select(r => r.PackageOptionId!.Value)
                            .ToHashSet();
                        var correctOptions = question.Options.Where(o => o.IsCorrect).ToList();
                        var selectedOptions = question.Options.Where(o => selectedOptionIds.Contains(o.Id)).ToList();

                        // ECG-02/04 (Phase 383): correctness via helper terpusat IsQuestionCorrect (kill-drift).
                        // verdict: true=Benar (correctCount++), false=Salah, null=essay pending (badge via IsEssayPending).
                        var verdict = AssessmentScoreAggregator.IsQuestionCorrect(question, userResponses);
                        bool isCorrect = verdict == true;
                        if (isCorrect) correctCount++;

                        var userAnswerText = selectedOptions.Any()
                            ? string.Join(", ", selectedOptions.Select(o => o.OptionText))
                            : null;
                        var correctAnswerText = correctOptions.Any()
                            ? string.Join(", ", correctOptions.Select(o => o.OptionText))
                            : "N/A";

                        // D-07: essay tak punya Options → tampilkan TextAnswer worker + label "Dinilai manual"
                        bool isEssay = (question.QuestionType ?? "MultipleChoice") == "Essay";
                        if (isEssay)
                        {
                            userAnswerText = userResponses.FirstOrDefault(r => r.PackageQuestionId == qId)?.TextAnswer;
                            correctAnswerText = "Dinilai manual";
                        }

                        questionReviews.Add(new QuestionReviewItem
                        {
                            QuestionNumber = questionNum,
                            QuestionText = question.QuestionText,
                            UserAnswer = userAnswerText,
                            CorrectAnswer = correctAnswerText,
                            IsCorrect = isCorrect,
                            ImagePath = question.ImagePath,
                            ImageAlt = question.ImageAlt,
                            Options = question.Options.Select(o => new OptionReviewItem
                            {
                                OptionText = o.OptionText,
                                IsCorrect = o.IsCorrect,
                                IsSelected = selectedOptionIds.Contains(o.Id),
                                ImagePath = o.ImagePath,
                                ImageAlt = o.ImageAlt
                            }).ToList(),
                            // D-06 (Phase 383): correctness-based pending, independen status sesi — graded essay di sesi
                            // Completed render Benar/Salah; essay tanpa EssayScore (verdict==null) selalu "Menunggu Penilaian".
                            IsEssayPending = (question.QuestionType ?? "MultipleChoice") == "Essay"
                                             && AssessmentScoreAggregator.IsQuestionCorrect(question, userResponses) == null
                        });
                    }
                }
                else
                {
                    // ECG-02 (Phase 383): count correct even when review disabled, via helper terpusat (essay-aware).
                    // Guard Count==0 lama DIHAPUS — essay (no option) tak boleh di-skip; helper handle internal.
                    foreach (var qId in orderedQuestionIds)
                    {
                        if (!questionLookup.TryGetValue(qId, out var question)) continue;
                        if (AssessmentScoreAggregator.IsQuestionCorrect(question, responseLookup[qId].ToList()) == true)
                            correctCount++;
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
                            // ECG-03 (Phase 383): Elemen Teknis hitung essay sesuai nilai HC via helper terpusat (essay-aware).
                            var correct = g.Count(q =>
                                AssessmentScoreAggregator.IsQuestionCorrect(q, responseLookup[q.Id].ToList()) == true);
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
                    NomorSertifikat = assessment.NomorSertifikat,
                    // SUB-01 D-08: pending mode flag — true saat assessment.Status == PendingGrading
                    IsPendingGrading = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
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
                    NomorSertifikat = assessment.NomorSertifikat,
                    // SUB-01 D-08: pending mode flag — true saat assessment.Status == PendingGrading
                    IsPendingGrading = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
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

            // === v32.4 RTK-09/10/12/13 (Phase 407): flag retake/tier + riwayat pekerja ke VM ===
            // View (407-03) hanya MERENDER; eligibility/tier dihitung server (leak-safety = keputusan server).
            // (a) Flag retake/tier — mirror counting RetakeService.CanRetakeAsync :237-242.
            //     Tier pakai assessment.IsPassed (bool?), BUKAN viewModel.IsPassed (bool) — Pitfall 5.
            // v32.7 RTH-03 (D-05): satu sumber counting era-retake snapshot-presence (kill-drift).
            int eraRetakeArchives = await RetakeCountingRules.CountForUserAsync(
                _context, assessment.UserId, assessment.Title, assessment.Category);
            int currentAttempt = eraRetakeArchives + 1;
            // v32.7 RTH-01 (Pitfall 1 & 2): masa ujian tutup → retake MUSTAHIL. +7h WIB byte-identik StartExam:956.
            bool windowClosed = assessment.ExamWindowCloseDate.HasValue
                && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value;
            ViewBag.WindowClosed = windowClosed;
            // attemptsRemaining utk TIER = "retake mungkin secara prinsip" (abaikan cooldown timing):
            // AllowRetake ON, tipe tak dikecualikan (PreTest/Manual via ShouldHideRetakeToggle), attempt belum habis.
            // WAJIB sertakan AllowRetake + ShouldHideRetakeToggle → assessment non-retake → kunci boleh tampil (ShowFullReview).
            // RTH-01 Pitfall 2: window tutup → attemptsRemaining=false → ResolveReviewMode buka full review (retake mustahil).
            bool attemptsRemaining = assessment.AllowRetake
                && !RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry)
                && currentAttempt < assessment.MaxAttempts
                && !windowClosed;
            viewModel.CurrentAttempt = currentAttempt;
            viewModel.MaxAttempts = assessment.MaxAttempts;
            viewModel.CanRetake = await _retakeService.CanRetakeAsync(assessment.Id);
            viewModel.RetakeMode = RetakeRules.ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining);
            viewModel.CooldownUntilUtc = (assessment.AllowRetake && assessment.RetakeCooldownHours > 0 && assessment.CompletedAt.HasValue)
                ? assessment.CompletedAt.Value.AddHours(assessment.RetakeCooldownHours) : (DateTime?)null;
            viewModel.IsCapReached = assessment.IsPassed == false && assessment.AllowRetake && currentAttempt >= assessment.MaxAttempts;
            // WR-01 fix (RTK-10): tombol cooldown-disabled HARUS tampil saat masa jeda belum lewat.
            // CanRetake (CanRetakeAsync) bernilai FALSE selama cooldown, jadi countdown tak boleh digate olehnya.
            // IsInCooldown = layak-ulang-abaikan-cooldown (gagal + attempt-sisa) DAN cooldown masih aktif.
            viewModel.IsInCooldown = attemptsRemaining && assessment.IsPassed == false
                && viewModel.CooldownUntilUtc.HasValue && viewModel.CooldownUntilUtc > DateTime.UtcNow
                && !windowClosed;   // v32.7 RTH-01 Pitfall 1: window tutup → JANGAN tampilkan countdown cooldown (dead-end UX)

            // (b) Riwayat load — cermin verbatim RiwayatPercobaan :3493-3522 (reuse RetakeArchiveBuilder + RiwayatUnifier).
            var histories = await _context.AssessmentAttemptHistory
                .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title && h.Category == assessment.Category)
                .OrderByDescending(h => h.AttemptNumber).ToListAsync();
            var histIds = histories.Select(h => h.Id).ToList();
            var archiveRows = await _context.AssessmentAttemptResponseArchives
                .Where(a => histIds.Contains(a.AttemptHistoryId)).ToListAsync();
            var currentRows = new List<AssessmentAttemptResponseArchive>();
            if (assessment.Status == "Completed")
            {
                var assign = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == assessment.Id);
                var qids = assign?.GetShuffledQuestionIds() ?? new List<int>();
                if (qids.Count > 0)
                {
                    var qs = await _context.PackageQuestions.Include(q => q.Options).Where(q => qids.Contains(q.Id)).ToListAsync();
                    var resp = await _context.PackageUserResponses.Where(r => r.AssessmentSessionId == assessment.Id).ToListAsync();
                    if (qs.Count > 0) currentRows = RetakeArchiveBuilder.Build(0, qs, resp);
                }
            }
            viewModel.RiwayatAttempts = RiwayatUnifier.Build(assessment, histories, archiveRows, currentRows);

            return View(viewModel);
        }

        // --- UJIAN ULANG (worker self-service) ---
        // v32.4 RTK-09 (Phase 407): pekerja men-trigger retake-nya sendiri dari halaman Hasil.
        // Cermin baris-per-baris HC ResetAssessment (AssessmentAdminController :4244-4327); beda:
        // actor = worker (effective user, impersonation-aware), guard = ownership Forbid + server-authoritative
        // CanRetakeAsync re-check (countdown JS BUKAN gate — D-01), redirect = StartExam (HC redirect ke Monitoring).
        [HttpPost]
        [ValidateAntiForgeryToken]                 // RTK-09 CSRF (class-level [Authorize] sudah :25)
        public async Task<IActionResult> RetakeExam(int id)
        {
            var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id);
            if (assessment == null) return NotFound();

            var (user, _) = await GetCurrentUserRoleLevelAsync();   // effective user (impersonation-aware) — idiom :909
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id) return Forbid();      // RTK-09 ownership — IDOR guard (worker self-service only)

            // Server-authoritative re-check (D-01): countdown/disable JS hanya UX, server otoritatif atas cooldown/cap.
            if (!await _retakeService.CanRetakeAsync(id))
            {
                TempData["Error"] = "Ujian ulang tidak bisa dijalankan saat ini. Coba muat ulang halaman atau hubungi HC.";
                return RedirectToAction("Results", new { id });
            }

            // actorName format mirror ResetAssessment :4298.
            var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
            var rs = await _retakeService.ExecuteAsync(id, user.Id, actorName, "RetakeAssessment", "worker_retake");
            if (!rs.Success)
            {
                TempData["Error"] = rs.Error ?? "Gagal menjalankan ujian ulang.";
                return RedirectToAction("Results", new { id });
            }

            // must-fix #1 — re-arm token (StartExam pakai TempData.Peek non-consume :944); clear setelah sukses.
            TempData.Remove($"TokenVerified_{id}");
            return RedirectToAction("StartExam", new { id });       // spec re-entry target
        }

        #region Helper Methods

        /// <summary>
        /// Returns (user, roleLevel) for the current authenticated user.
        /// Extracts the repeated role-scoping pattern used across multiple actions.
        /// </summary>
        // Phase 377 (D-05 single-source): impersonation-aware. Konsumsi resolver ImpersonationService.GetEffectiveUserAsync.
        // UseRealUser → user asli (SC4 identik); RoleModeEmpty → (null, effective role-level) caller render kosong+hint (D-03);
        // TargetUser → user efektif X (full-fidelity D-01). ~9 caller self-read terfix otomatis di hulu.
        private async Task<(ApplicationUser? User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
        {
            var (effUser, decision) = await _impersonationService.GetEffectiveUserAsync(_userManager);

            // SC4: non-impersonate (atau expired) → identik perilaku hari ini.
            if (decision == EffectiveUserDecision.UseRealUser)
            {
                var real = await _userManager.GetUserAsync(User);
                if (real == null) return (null, 0);
                var realRoles = await _userManager.GetRolesAsync(real);
                return (real, UserRoles.GetRoleLevel(realRoles.FirstOrDefault() ?? ""));
            }

            // D-03: mode role → user null (caller render kosong+hint); role-level efektif tetap untuk gating UI.
            if (decision == EffectiveUserDecision.RoleModeEmpty)
                return (null, _impersonationService.GetEffectiveRoleLevel() ?? 0);

            // mode user (TargetUser): effUser = X (defensif null = D-04, middleware sudah redirect).
            if (effUser == null) return (null, 0);
            var effLevel = _impersonationService.GetEffectiveRoleLevel()
                           ?? UserRoles.GetRoleLevel((await _userManager.GetRolesAsync(effUser)).FirstOrDefault() ?? "");
            return (effUser, effLevel);
        }

        // REC-04 (D-09): authz single-source untuk Results/Certificate/CertificatePdf.
        // owner ∥ roleLevel 1-3 (Admin/HC/L3) ∥ (L4 section-scoped, Section non-null).
        public static bool IsResultsAuthorized(string? ownerUserId, string currentUserId, int roleLevel, string? currentUserSection, string? ownerSection)
        {
            if (ownerUserId == currentUserId) return true;          // owner (coach/coachee self)
            if (roleLevel is >= 1 and <= 3) return true;            // Admin(1)/HC(2)/Direktur-VP-Manager(3): full
            if (roleLevel == 4
                && !string.IsNullOrEmpty(currentUserSection)        // guard: L4 Section null/empty tidak lolos
                && ownerSection == currentUserSection)              // SectionHead/SrSupervisor: section-scoped
                return true;
            return false;                                           // L5/L6 non-owner, L4 beda/null section, roleLevel 0
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
            var today = DateTime.UtcNow.AddHours(7).Date;
            // Phase 327 — DateOnly hybrid: today (DateTime) untuk periodeEnd, todayDate (DateOnly) untuk ValidUntil compare
            var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
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
            // Phase 327 — pakai todayDate (DateOnly) untuk ValidUntil compare
            var thirtyDaysFromNow = todayDate.AddDays(30);

            var trainingExpiring = _context.TrainingRecords
                .Include(t => t.User)
                .Where(t => t.Status == "Valid"
                    && t.ValidUntil.HasValue
                    && t.ValidUntil >= todayDate
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
                    && s.ValidUntil >= todayDate
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

        // ============================================================
        // Analytics Dashboard v2 — Summary, FailRate, Trend, ET Breakdown, Expiring, DrillDown, Export
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetAnalyticsSummary(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            // Phase 327 — DateOnly hybrid: today (DateTime) untuk periodeEnd, todayDate (DateOnly) untuk ValidUntil compare
            var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var baseQuery = _context.AssessmentSessions
                .AsNoTracking()
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

            var totalSessions = await baseQuery.CountAsync();
            var passedCount = await baseQuery.CountAsync(s => s.IsPassed == true);
            var passRate = totalSessions > 0 ? Math.Round((double)passedCount / totalSessions * 100, 1) : 0;

            // Expiring certificates count (30 days)
            var thirtyDays = todayDate.AddDays(30);
            var expiringTraining = _context.TrainingRecords.AsNoTracking()
                .Where(t => t.Status == "Valid" && t.ValidUntil.HasValue && t.ValidUntil >= todayDate && t.ValidUntil <= thirtyDays);
            var expiringSession = _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.IsPassed == true && s.GenerateCertificate && s.ValidUntil.HasValue && s.ValidUntil >= todayDate && s.ValidUntil <= thirtyDays);

            if (!string.IsNullOrEmpty(bagian))
            {
                expiringTraining = expiringTraining.Where(t => t.User!.Section == bagian);
                expiringSession = expiringSession.Where(s => s.User!.Section == bagian);
            }
            if (!string.IsNullOrEmpty(unit))
            {
                expiringTraining = expiringTraining.Where(t => t.User!.Unit == unit);
                expiringSession = expiringSession.Where(s => s.User!.Unit == unit);
            }

            var expiringCount = await expiringTraining.CountAsync() + await expiringSession.CountAsync();

            // Avg gain score
            var postSessions = await _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.AssessmentType == "PostTest" && s.Status == "Completed"
                    && s.Score.HasValue && s.LinkedSessionId.HasValue
                    && s.CompletedAt.HasValue && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd)
                .Select(s => new { s.UserId, PostScore = s.Score!.Value, s.LinkedSessionId })
                .ToListAsync();

            if (!string.IsNullOrEmpty(bagian))
            {
                var userIds = await _context.Users.AsNoTracking().Where(u => u.Section == bagian).Select(u => u.Id).ToListAsync();
                postSessions = postSessions.Where(p => userIds.Contains(p.UserId)).ToList();
            }

            double avgGainScore = 0;
            if (postSessions.Any())
            {
                var preIds = postSessions.Select(p => p.LinkedSessionId!.Value).Distinct().ToList();
                var preScores = await _context.AssessmentSessions.AsNoTracking()
                    .Where(s => preIds.Contains(s.Id) && s.Score.HasValue)
                    .ToDictionaryAsync(s => s.Id, s => s.Score!.Value);

                var gains = postSessions
                    .Where(p => preScores.ContainsKey(p.LinkedSessionId!.Value))
                    .Select(p => {
                        double pre = preScores[p.LinkedSessionId!.Value];
                        double post = p.PostScore;
                        return pre >= 100 ? 100 : (100 - pre) == 0 ? 0 : (post - pre) / (100 - pre) * 100;
                    }).ToList();

                avgGainScore = gains.Any() ? Math.Round(gains.Average(), 1) : 0;
            }

            return Json(new { totalSessions, passRate, expiringCount, avgGainScore });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetFailRateData(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var baseQuery = _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.IsPassed.HasValue && s.CompletedAt.HasValue
                    && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd);

            if (!string.IsNullOrEmpty(bagian))
                baseQuery = baseQuery.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                baseQuery = baseQuery.Where(s => s.User!.Unit == unit);
            if (!string.IsNullOrEmpty(kategori))
                baseQuery = baseQuery.Where(s => s.Category == kategori);

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

            return Json(failRate);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetTrendData(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var baseQuery = _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.IsPassed.HasValue && s.CompletedAt.HasValue
                    && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd);

            if (!string.IsNullOrEmpty(bagian))
                baseQuery = baseQuery.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                baseQuery = baseQuery.Where(s => s.User!.Unit == unit);
            if (!string.IsNullOrEmpty(kategori))
                baseQuery = baseQuery.Where(s => s.Category == kategori);

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

            // Gain score trend
            var postSessions = await _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.AssessmentType == "PostTest" && s.Status == "Completed"
                    && s.Score.HasValue && s.LinkedSessionId.HasValue
                    && s.CompletedAt.HasValue && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd)
                .Select(s => new { s.UserId, PostScore = s.Score!.Value, s.LinkedSessionId, PostCompleted = s.CompletedAt!.Value })
                .ToListAsync();

            if (!string.IsNullOrEmpty(bagian))
            {
                var userIds = await _context.Users.AsNoTracking().Where(u => u.Section == bagian).Select(u => u.Id).ToListAsync();
                postSessions = postSessions.Where(p => userIds.Contains(p.UserId)).ToList();
            }
            if (!string.IsNullOrEmpty(unit))
            {
                var userIds = await _context.Users.AsNoTracking().Where(u => u.Unit == unit).Select(u => u.Id).ToListAsync();
                postSessions = postSessions.Where(p => userIds.Contains(p.UserId)).ToList();
            }

            var preIds = postSessions.Select(p => p.LinkedSessionId!.Value).Distinct().ToList();
            var preScores = await _context.AssessmentSessions.AsNoTracking()
                .Where(s => preIds.Contains(s.Id) && s.Score.HasValue)
                .ToDictionaryAsync(s => s.Id, s => s.Score!.Value);

            var gainScoreTrend = postSessions
                .Where(p => preScores.ContainsKey(p.LinkedSessionId!.Value))
                .Select(p => {
                    double pre = preScores[p.LinkedSessionId!.Value];
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

            return Json(new { trend, gainScoreTrend });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetEtBreakdownData(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var etBaseQuery = _context.SessionElemenTeknisScores.AsNoTracking()
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

            return Json(etBreakdown);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetExpiringSoonData(
            string? bagian, string? unit, int days = 30)
        {
            // Phase 327 — DateOnly migrasi P04 (Pattern B)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var futureDate = today.AddDays(days);

            var trainingExpiring = _context.TrainingRecords.AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.Status == "Valid" && t.ValidUntil.HasValue
                    && t.ValidUntil >= today && t.ValidUntil <= futureDate);

            if (!string.IsNullOrEmpty(bagian))
                trainingExpiring = trainingExpiring.Where(t => t.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                trainingExpiring = trainingExpiring.Where(t => t.User!.Unit == unit);

            var trainingItems = await trainingExpiring
                .Select(t => new {
                    namaPekerja = t.User!.FullName ?? t.User.UserName ?? "",
                    namaSertifikat = t.Judul ?? "",
                    tanggalExpired = t.ValidUntil!.Value,
                    sectionUnit = (t.User!.Section ?? "") + (t.User.Unit != null ? " / " + t.User.Unit : "")
                })
                .ToListAsync();

            var sessionExpiring = _context.AssessmentSessions.AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.IsPassed == true && s.GenerateCertificate
                    && s.ValidUntil.HasValue && s.ValidUntil >= today && s.ValidUntil <= futureDate);

            if (!string.IsNullOrEmpty(bagian))
                sessionExpiring = sessionExpiring.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                sessionExpiring = sessionExpiring.Where(s => s.User!.Unit == unit);

            var sessionItems = await sessionExpiring
                .Select(s => new {
                    namaPekerja = s.User!.FullName ?? s.User.UserName ?? "",
                    namaSertifikat = s.Title,
                    tanggalExpired = s.ValidUntil!.Value,
                    sectionUnit = (s.User!.Section ?? "") + (s.User.Unit != null ? " / " + s.User.Unit : "")
                })
                .ToListAsync();

            var result = trainingItems.Concat(sessionItems)
                .OrderBy(e => e.tanggalExpired)
                .ToList();

            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetFailRateDrillDown(
            string section, string category,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var items = await _context.AssessmentSessions.AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.IsPassed.HasValue && s.CompletedAt.HasValue
                    && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd
                    && s.User!.Section == section && s.Category == category)
                .OrderByDescending(s => s.CompletedAt)
                .Select(s => new
                {
                    namaPekerja = s.User!.FullName ?? s.User.UserName ?? "",
                    skor = s.Score ?? 0,
                    tanggalAssessment = s.CompletedAt!.Value,
                    status = s.IsPassed == true ? "Lulus" : "Gagal"
                })
                .ToListAsync();

            return Json(items);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportFailRateExcel(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var baseQuery = _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.IsPassed.HasValue && s.CompletedAt.HasValue
                    && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd);

            if (!string.IsNullOrEmpty(bagian))
                baseQuery = baseQuery.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                baseQuery = baseQuery.Where(s => s.User!.Unit == unit);
            if (!string.IsNullOrEmpty(kategori))
                baseQuery = baseQuery.Where(s => s.Category == kategori);

            var failRate = await baseQuery
                .GroupBy(s => new { Section = s.User!.Section ?? "Tidak Diketahui", s.Category })
                .Select(g => new { Section = g.Key.Section, Category = g.Key.Category, Total = g.Count(), Failed = g.Count(s => s.IsPassed == false) })
                .ToListAsync();

            using var wb = new XLWorkbook();
            var headers = new[] { "Bagian", "Kategori", "Total Sesi", "Gagal", "Fail Rate (%)" };
            var ws = ExcelExportHelper.CreateSheet(wb, "Fail Rate", headers);
            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, 1, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#dc3545"));
            ws.Range(1, 1, 1, headers.Length).Style.Font.SetFontColor(XLColor.White);

            int row = 2;
            foreach (var item in failRate)
            {
                double rate = item.Total > 0 ? Math.Round((double)item.Failed / item.Total * 100, 1) : 0;
                ws.Cell(row, 1).Value = item.Section;
                ws.Cell(row, 2).Value = item.Category;
                ws.Cell(row, 3).Value = item.Total;
                ws.Cell(row, 4).Value = item.Failed;
                ws.Cell(row, 5).Value = rate;
                row++;
            }

            return ExcelExportHelper.ToFileResult(wb, $"FailRate_{DateTime.Now:yyyyMMdd}.xlsx", this);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportTrendExcel(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var baseQuery = _context.AssessmentSessions.AsNoTracking()
                .Where(s => s.IsPassed.HasValue && s.CompletedAt.HasValue
                    && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd);

            if (!string.IsNullOrEmpty(bagian))
                baseQuery = baseQuery.Where(s => s.User!.Section == bagian);
            if (!string.IsNullOrEmpty(unit))
                baseQuery = baseQuery.Where(s => s.User!.Unit == unit);
            if (!string.IsNullOrEmpty(kategori))
                baseQuery = baseQuery.Where(s => s.Category == kategori);

            var trend = await baseQuery
                .GroupBy(s => new { s.CompletedAt!.Value.Year, s.CompletedAt!.Value.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Passed = g.Count(s => s.IsPassed == true), Failed = g.Count(s => s.IsPassed == false) })
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var headers = new[] { "Bulan", "Lulus", "Gagal", "Total" };
            var ws = ExcelExportHelper.CreateSheet(wb, "Trend Assessment", headers);
            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, 1, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
            ws.Range(1, 1, 1, headers.Length).Style.Font.SetFontColor(XLColor.White);

            int row = 2;
            foreach (var item in trend)
            {
                ws.Cell(row, 1).Value = $"{item.Year}-{item.Month:D2}";
                ws.Cell(row, 2).Value = item.Passed;
                ws.Cell(row, 3).Value = item.Failed;
                ws.Cell(row, 4).Value = item.Passed + item.Failed;
                row++;
            }

            return ExcelExportHelper.ToFileResult(wb, $"TrendAssessment_{DateTime.Now:yyyyMMdd}.xlsx", this);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportEtBreakdownExcel(
            string? bagian, string? unit, string? kategori, string? subKategori,
            DateTime? periodeStart, DateTime? periodeEnd)
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            periodeEnd ??= today;
            periodeStart ??= periodeEnd.Value.AddYears(-1);

            var etBaseQuery = _context.SessionElemenTeknisScores.AsNoTracking()
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

            var data = await etBaseQuery
                .GroupBy(e => new { e.ElemenTeknis, e.AssessmentSession.Category })
                .Select(g => new {
                    ElemenTeknis = g.Key.ElemenTeknis,
                    Category = g.Key.Category,
                    AvgPct = g.Average(e => (double)e.CorrectCount * 100.0 / e.QuestionCount),
                    MinPct = g.Min(e => (double)e.CorrectCount * 100.0 / e.QuestionCount),
                    MaxPct = g.Max(e => (double)e.CorrectCount * 100.0 / e.QuestionCount),
                    SampleCount = g.Count()
                })
                .ToListAsync();

            using var wb = new XLWorkbook();
            var headers = new[] { "Elemen Teknis", "Kategori", "Rata-rata (%)", "Min (%)", "Max (%)", "Jumlah Sesi" };
            var ws = ExcelExportHelper.CreateSheet(wb, "Skor Elemen Teknis", headers);
            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, 1, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#ffc107"));
            ws.Range(1, 1, 1, headers.Length).Style.Font.SetFontColor(XLColor.Black);

            int row = 2;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.ElemenTeknis;
                ws.Cell(row, 2).Value = item.Category;
                ws.Cell(row, 3).Value = Math.Round(item.AvgPct, 1);
                ws.Cell(row, 4).Value = Math.Round(item.MinPct, 1);
                ws.Cell(row, 5).Value = Math.Round(item.MaxPct, 1);
                ws.Cell(row, 6).Value = item.SampleCount;
                row++;
            }

            return ExcelExportHelper.ToFileResult(wb, $"SkorElemenTeknis_{DateTime.Now:yyyyMMdd}.xlsx", this);
        }

        // ============================================================
        // GET /CMP/GetPrePostAssessmentList — daftar assessment PrePostTest untuk dropdown
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
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
        [Authorize(Roles = "Admin, HC")]
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
                if (questionType == "MultipleChoice")
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
        [Authorize(Roles = "Admin, HC")]
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
        [Authorize(Roles = "Admin, HC")]
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
                if (questionType != "MultipleChoice") continue;

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
        [Authorize(Roles = "Admin, HC")]
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

        // CertificationManagement: route lama /CMP/CertificationManagement.
        // Entry produktif (Views/CMP/Index.cshtml) menunjuk action CDP canonical;
        // view CMP tak pernah ada sehingga dulu 500. Redirect 302 ke CDP (Phase 378, CMPRT-01).
        public IActionResult CertificationManagement()
            => RedirectToAction("CertificationManagement", "CDP");

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

        // Phase 351 (SF-04/SF-05): opsi Kategori dari record AKTUAL (distinct unifiedRecords.Kategori),
        // bukan master AssessmentCategories — menampung kategori free-text/legacy + buang opsi mati.
        // public static agar reachable dari HcPortal.Tests tanpa InternalsVisibleTo.
        public static List<string> BuildActualCategories(IEnumerable<HcPortal.Models.UnifiedTrainingRecord> records) =>
            records.Where(r => !string.IsNullOrEmpty(r.Kategori))
                   .Select(r => r.Kategori!)
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .OrderBy(n => n)
                   .ToList();

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
                    .Where(u => u.IsActive && u.Section == user!.Section)
                    .Select(u => u.Id)
                    .ToListAsync();
            }
            else if (roleLevel == 5)
            {
                if (l5OwnDataOnly)
                {
                    scopedUserIds = new List<string> { user!.Id };
                }
                else
                {
                    // L5: coach sees mapped coachees + own data
                    var coacheeIds = await _context.CoachCoacheeMappings
                        .Where(m => m.CoachId == user!.Id && m.IsActive)
                        .Select(m => m.CoacheeId)
                        .ToListAsync();
                    coacheeIds.Add(user!.Id);
                    scopedUserIds = coacheeIds;
                }
            }
            else
            {
                // L6: own data only
                scopedUserIds = new List<string> { user!.Id };
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
            // #25: GroupBy-dedup via helper shared (cegah ArgumentException/500 pada duplicate child Name lintas parent).
            var categoryNameLookup = SertifikatRow.BuildParentNameLookup(
                allCategories.Select(c => (c.Id, c.Name, c.ParentId)));

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

        // ========== Budget Training/Assessment ==========

        [Authorize(Roles = "Admin,HC")]
        public async Task<IActionResult> BudgetTraining(
            int? tahun, string? type, string? kategori, string? search,
            string? sortBy, string? sortDir, int page = 1, int pageSize = 20)
        {
            if (pageSize is not 20 and not 50 and not 100) pageSize = 20;
            var query = _context.BudgetItems.AsQueryable();

            if (tahun.HasValue) query = query.Where(b => b.TahunAnggaran == tahun.Value);
            if (!string.IsNullOrEmpty(type)) query = query.Where(b => b.Type == type);
            if (!string.IsNullOrEmpty(kategori)) query = query.Where(b => b.Kategori == kategori);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.Judul.Contains(search) || (b.Vendor != null && b.Vendor.Contains(search)));

            var totalItems = await query.CountAsync();

            // Sorting
            var dir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            IOrderedQueryable<BudgetItem> ordered = sortBy?.ToLower() switch
            {
                "judul" => dir == "asc" ? query.OrderBy(b => b.Judul) : query.OrderByDescending(b => b.Judul),
                "kategori" => dir == "asc" ? query.OrderBy(b => b.Kategori) : query.OrderByDescending(b => b.Kategori),
                "anggaran" => dir == "asc" ? query.OrderBy(b => b.EstimasiBiayaTotal) : query.OrderByDescending(b => b.EstimasiBiayaTotal),
                "realisasi" => dir == "asc" ? query.OrderBy(b => b.RealisasiBiaya) : query.OrderByDescending(b => b.RealisasiBiaya),
                "serapan" => dir == "asc"
                    ? query.OrderBy(b => b.EstimasiBiayaTotal == 0 ? 0 : b.RealisasiBiaya / b.EstimasiBiayaTotal)
                    : query.OrderByDescending(b => b.EstimasiBiayaTotal == 0 ? 0 : b.RealisasiBiaya / b.EstimasiBiayaTotal),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Chart data — by kategori (uses same filtered query as table)
            var chartData = await query
                .GroupBy(b => b.Kategori ?? "Lainnya")
                .Select(g => new BudgetChartData
                {
                    Kategori = g.Key,
                    Rencana = g.Sum(x => x.EstimasiBiayaTotal),
                    Realisasi = g.Sum(x => x.RealisasiBiaya)
                }).ToListAsync();

            // Chart data — by type (Training vs Assessment)
            var chartByType = await query
                .GroupBy(b => b.Type)
                .Select(g => new BudgetChartData
                {
                    Kategori = g.Key,
                    Rencana = g.Sum(x => x.EstimasiBiayaTotal),
                    Realisasi = g.Sum(x => x.RealisasiBiaya)
                }).ToListAsync();

            // Top 10 items by anggaran
            var topItems = await query
                .OrderByDescending(b => b.EstimasiBiayaTotal)
                .Take(10)
                .Select(b => new BudgetTopItem { Judul = b.Judul, Anggaran = b.EstimasiBiayaTotal })
                .ToListAsync();

            // Summary from full filtered query (not paged) — uses same filters as table
            var totalRencana = await query.SumAsync(b => b.EstimasiBiayaTotal);
            var totalRealisasi = await query.SumAsync(b => b.RealisasiBiaya);

            var vm = new BudgetTrainingViewModel
            {
                Items = items,
                FilterTahun = tahun,
                FilterType = type,
                FilterKategori = kategori,
                Search = search,
                SortBy = sortBy ?? "",
                SortDir = dir,
                TotalRencana = totalRencana,
                TotalRealisasi = totalRealisasi,
                TotalItems = totalItems,
                ChartData = chartData,
                ChartByType = chartByType,
                TopItems = topItems,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                PageSize = pageSize,
                AvailableTahun = await _context.BudgetItems.Select(b => b.TahunAnggaran).Distinct().OrderByDescending(t => t).ToListAsync(),
                AvailableKategori = await _context.BudgetItems.Where(b => b.Kategori != null).Select(b => b.Kategori!).Distinct().OrderBy(k => k).ToListAsync()
            };

            // Kategori options for edit modal cascade dropdown
            ViewBag.KategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            ViewBag.SubKategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId != null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HC")]
        public async Task<IActionResult> BudgetTrainingCreate()
        {
            ViewBag.KategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            ViewBag.SubKategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId != null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingCreate(BudgetItem item)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            item.CreatedByUserId = user.Id;
            item.CreatedAt = DateTime.Now;
            item.EstimasiBiayaTotal = item.JumlahPeserta * item.BiayaPerOrang;

            _context.BudgetItems.Add(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Budget item berhasil ditambahkan.";
            return RedirectToAction("BudgetTraining", new { tahun = item.TahunAnggaran });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingEdit(BudgetItem item)
        {
            var existing = await _context.BudgetItems.FindAsync(item.Id);
            if (existing == null) return NotFound();

            existing.Type = item.Type;
            existing.Judul = item.Judul;
            existing.Kategori = item.Kategori;
            existing.SubKategori = item.SubKategori;
            existing.TahunAnggaran = item.TahunAnggaran;
            existing.JumlahPeserta = item.JumlahPeserta;
            existing.BiayaPerOrang = item.BiayaPerOrang;
            existing.EstimasiBiayaTotal = item.JumlahPeserta * item.BiayaPerOrang;
            existing.RealisasiBiaya = item.RealisasiBiaya;
            existing.Vendor = item.Vendor;
            existing.Catatan = item.Catatan;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Budget item berhasil diperbarui.";
            return RedirectToAction("BudgetTraining", new { tahun = existing.TahunAnggaran });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingDelete(int id)
        {
            var item = await _context.BudgetItems.FindAsync(id);
            if (item == null) return NotFound();

            _context.BudgetItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Budget item berhasil dihapus.";
            return RedirectToAction("BudgetTraining");
        }

        [Authorize(Roles = "Admin,HC")]
        public async Task<IActionResult> BudgetTrainingExport(int? tahun)
        {
            var query = _context.BudgetItems.AsQueryable();
            if (tahun.HasValue) query = query.Where(b => b.TahunAnggaran == tahun.Value);
            var items = await query.OrderBy(b => b.Type).ThenBy(b => b.Kategori).ThenBy(b => b.Judul).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Budget Training");
            var headers = new[] { "No", "Type", "Judul", "Kategori", "Sub Kategori", "Tahun", "Jml Peserta", "Biaya/Orang", "Estimasi Total", "Realisasi", "Vendor", "Catatan" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var b = items[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = b.Type;
                ws.Cell(i + 2, 3).Value = b.Judul;
                ws.Cell(i + 2, 4).Value = b.Kategori ?? "";
                ws.Cell(i + 2, 5).Value = b.SubKategori ?? "";
                ws.Cell(i + 2, 6).Value = b.TahunAnggaran;
                ws.Cell(i + 2, 7).Value = b.JumlahPeserta;
                ws.Cell(i + 2, 8).Value = (double)b.BiayaPerOrang;
                ws.Cell(i + 2, 9).Value = (double)b.EstimasiBiayaTotal;
                ws.Cell(i + 2, 10).Value = (double)b.RealisasiBiaya;
                ws.Cell(i + 2, 11).Value = b.Vendor ?? "";
                ws.Cell(i + 2, 12).Value = b.Catatan ?? "";
            }

            // Format currency columns (Biaya/Orang, Estimasi Total, Realisasi)
            ws.Column(8).Style.NumberFormat.Format = "#,##0";
            ws.Column(9).Style.NumberFormat.Format = "#,##0";
            ws.Column(10).Style.NumberFormat.Format = "#,##0";

            // Auto-filter on header row
            ws.RangeUsed()!.SetAutoFilter();
            ws.Columns().AdjustToContents();

            // Sheet 2: Ringkasan
            var ws2 = workbook.Worksheets.Add("Ringkasan");
            var summaryHeaders = new[] { "Kategori", "Total Rencana", "Total Realisasi", "Selisih" };
            for (int i = 0; i < summaryHeaders.Length; i++)
            {
                ws2.Cell(1, i + 1).Value = summaryHeaders[i];
                ws2.Cell(1, i + 1).Style.Font.Bold = true;
                ws2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            }

            // Per Kategori
            var grouped = items.GroupBy(b => b.Kategori ?? "Lainnya").OrderBy(g => g.Key).ToList();
            int row = 2;
            foreach (var g in grouped)
            {
                ws2.Cell(row, 1).Value = g.Key;
                ws2.Cell(row, 2).Value = (double)g.Sum(x => x.EstimasiBiayaTotal);
                ws2.Cell(row, 3).Value = (double)g.Sum(x => x.RealisasiBiaya);
                ws2.Cell(row, 4).Value = (double)(g.Sum(x => x.EstimasiBiayaTotal) - g.Sum(x => x.RealisasiBiaya));
                row++;
            }

            // Per Type
            row++;
            ws2.Cell(row, 1).Value = "Per Type";
            ws2.Cell(row, 1).Style.Font.Bold = true;
            row++;
            foreach (var g in items.GroupBy(b => b.Type))
            {
                ws2.Cell(row, 1).Value = g.Key;
                ws2.Cell(row, 2).Value = (double)g.Sum(x => x.EstimasiBiayaTotal);
                ws2.Cell(row, 3).Value = (double)g.Sum(x => x.RealisasiBiaya);
                ws2.Cell(row, 4).Value = (double)(g.Sum(x => x.EstimasiBiayaTotal) - g.Sum(x => x.RealisasiBiaya));
                row++;
            }

            // Grand Total
            row++;
            ws2.Cell(row, 1).Value = "GRAND TOTAL";
            ws2.Cell(row, 1).Style.Font.Bold = true;
            ws2.Cell(row, 2).Value = (double)items.Sum(x => x.EstimasiBiayaTotal);
            ws2.Cell(row, 3).Value = (double)items.Sum(x => x.RealisasiBiaya);
            ws2.Cell(row, 4).Value = (double)(items.Sum(x => x.EstimasiBiayaTotal) - items.Sum(x => x.RealisasiBiaya));
            ws2.Row(row).Style.Font.Bold = true;

            ws2.Column(2).Style.NumberFormat.Format = "#,##0";
            ws2.Column(3).Style.NumberFormat.Format = "#,##0";
            ws2.Column(4).Style.NumberFormat.Format = "#,##0";
            ws2.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BudgetTraining_{tahun?.ToString() ?? "All"}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = "Admin,HC")]
        public IActionResult BudgetTrainingImport()
        {
            return View("BudgetTrainingImport", (List<BudgetTrainingImportResult>?)null);
        }

        [Authorize(Roles = "Admin,HC")]
        public IActionResult DownloadBudgetTrainingTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Template");

            var headers = new[] { "No", "Type", "Judul", "Kategori", "Sub Kategori", "Tahun", "Jml Peserta", "Biaya/Orang", "Estimasi Total", "Realisasi", "Vendor", "Catatan" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
            }

            // Sample row 1
            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "Training";
            ws.Cell(2, 3).Value = "Leadership Development";
            ws.Cell(2, 4).Value = "IHT";
            ws.Cell(2, 5).Value = "-";
            ws.Cell(2, 6).Value = DateTime.Now.Year;
            ws.Cell(2, 7).Value = 30;
            ws.Cell(2, 8).Value = 5000000;
            ws.Cell(2, 9).Value = 150000000;
            ws.Cell(2, 10).Value = 0;
            ws.Cell(2, 11).Value = "PT Training Co";
            ws.Cell(2, 12).Value = "-";

            // Sample row 2
            ws.Cell(3, 1).Value = 2;
            ws.Cell(3, 2).Value = "Assessment";
            ws.Cell(3, 3).Value = "Safety Competency Test";
            ws.Cell(3, 4).Value = "MANDATORY";
            ws.Cell(3, 5).Value = "Safety";
            ws.Cell(3, 6).Value = DateTime.Now.Year;
            ws.Cell(3, 7).Value = 50;
            ws.Cell(3, 8).Value = 2000000;
            ws.Cell(3, 9).Value = 100000000;
            ws.Cell(3, 10).Value = 0;
            ws.Cell(3, 11).Value = "-";
            ws.Cell(3, 12).Value = "Batch 1";

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Template_BudgetTraining.xlsx");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingImport(IFormFile file, int tahunAnggaran)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "File tidak valid.";
                return View("BudgetTrainingImport", (List<BudgetTrainingImportResult>?)null);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var results = new List<BudgetTrainingImportResult>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var rows = ws.RowsUsed().Skip(1); // skip header
            int rowNum = 1;

            foreach (var row in rows)
            {
                rowNum++;
                var judul = row.Cell(3).GetString()?.Trim() ?? "";

                if (string.IsNullOrEmpty(judul))
                {
                    results.Add(new BudgetTrainingImportResult { Row = rowNum, Judul = "(kosong)", Status = "Skip", Message = "Judul kosong — baris dilewati" });
                    continue;
                }

                try
                {
                    var peserta = row.Cell(7).TryGetValue(out int p) ? p : 0;
                    var biayaPerOrang = row.Cell(8).TryGetValue(out decimal bpo) ? bpo : 0;

                    var typeVal = row.Cell(2).GetString()?.Trim() ?? "";
                    var kategoriVal = row.Cell(4).GetString()?.Trim() ?? "";
                    var subKategoriVal = row.Cell(5).GetString()?.Trim() ?? "";
                    var vendorVal = row.Cell(11).GetString()?.Trim() ?? "";
                    var catatanVal = row.Cell(12).GetString()?.Trim() ?? "";

                    var item = new BudgetItem
                    {
                        Type = typeVal == "Assessment" ? "Assessment" : "Training",
                        Judul = judul,
                        Kategori = string.IsNullOrWhiteSpace(kategoriVal) ? null : kategoriVal,
                        SubKategori = string.IsNullOrWhiteSpace(subKategoriVal) ? null : subKategoriVal,
                        TahunAnggaran = tahunAnggaran,
                        JumlahPeserta = peserta,
                        BiayaPerOrang = biayaPerOrang,
                        EstimasiBiayaTotal = peserta * biayaPerOrang,
                        RealisasiBiaya = row.Cell(10).TryGetValue(out decimal r) ? r : 0,
                        Vendor = string.IsNullOrWhiteSpace(vendorVal) ? null : vendorVal,
                        Catatan = string.IsNullOrWhiteSpace(catatanVal) ? null : catatanVal,
                        CreatedByUserId = user.Id,
                        CreatedAt = DateTime.Now
                    };
                    _context.BudgetItems.Add(item);
                    await _context.SaveChangesAsync();

                    results.Add(new BudgetTrainingImportResult { Row = rowNum, Judul = judul, Status = "Success", Message = "Berhasil diimport" });
                }
                catch (Exception ex)
                {
                    results.Add(new BudgetTrainingImportResult { Row = rowNum, Judul = judul, Status = "Error", Message = ex.Message });
                }
            }

            return View("BudgetTrainingImport", results);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingQuickUpdate(int id, decimal realisasi)
        {
            if (realisasi < 0) return Json(new { success = false, message = "Nilai tidak boleh negatif" });

            var item = await _context.BudgetItems.FindAsync(id);
            if (item == null) return NotFound();

            item.RealisasiBiaya = realisasi;
            item.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, realisasi = item.RealisasiBiaya });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingBulkDelete([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return BadRequest();

            var items = await _context.BudgetItems.Where(b => ids.Contains(b.Id)).ToListAsync();
            _context.BudgetItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Json(new { success = true, deleted = items.Count });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetTrainingBulkUpdateRealisasi([FromBody] Dictionary<string, decimal> updates)
        {
            if (updates == null || updates.Count == 0) return BadRequest();

            var ids = updates.Keys.Select(k => int.Parse(k)).ToArray();
            var items = await _context.BudgetItems.Where(b => ids.Contains(b.Id)).ToListAsync();

            foreach (var item in items)
            {
                if (updates.TryGetValue(item.Id.ToString(), out var val))
                {
                    item.RealisasiBiaya = val;
                    item.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, updated = items.Count });
        }

        // ============================================================
        // Phase 313 — Block Manual Submit Saat Waktu Habis (TMR-01)
        // 2-tier server-side timer enforcement helper (analog Phase 312 EnsureCanDeleteAsync)
        // ============================================================

        /// <summary>
        /// Enforces 2-tier timer rules pada SubmitExam:
        ///  - Tier 1 (Phase 313 NEW, D-09): manual submit (!isAutoSubmit) reject tanpa grace bila elapsed > Duration+ExtraTime.
        ///  - Tier 2 (existing LIFE-03 preserved, D-06): auto submit reject bila elapsed > Duration+ExtraTime+2min grace.
        /// AssessmentType Manual / null di-skip (D-15 defense-in-depth).
        /// Returns IActionResult kalau reject (caller `return` value), atau null kalau pass (caller lanjut).
        /// </summary>
        // ==================== Phase 382 PURE DECISION HELPERS (Wave 0) ====================
        // Konvensi repo: CMPController ber-ctor 14-dep → uji lewat pure static helper (lihat VerifyTokenTests.cs).
        // Helper = sumber kebenaran tunggal; controller mendelegasikan ke helper (anti-drift, pola Phase 380/363).

        /// <summary>
        /// TMR-01: apakah timer submit di-enforce untuk AssessmentType ini.
        /// BLOCKLIST — skip HANYA Manual / null / kosong; selain itu (Standard/Online/PreTest/PostTest) di-enforce.
        /// TMR-01 (WSE-09 / T-382-07): inversi dari allowlist lama (yg meninggalkan "Standard" tak ter-enforce = dead code).
        /// </summary>
        public static bool ShouldEnforceSubmitTimer(string? assessmentType)
        {
            // Skip guard HANYA untuk Manual / null / kosong; sisanya (termasuk literal "Standard") di-enforce.
            return !(assessmentType == AssessmentConstants.AssessmentType.Manual
                || string.IsNullOrEmpty(assessmentType));
        }

        public enum SubmitTimerDecision { Pass, BlockNoGrace, BlockGrace }

        /// <summary>
        /// Keputusan tier timer (pure). Tier-1 (BlockNoGrace) bila elapsed≥allowed tanpa server-approved token;
        /// Tier-2 (BlockGrace) bila elapsed≥grace; selain itu Pass. Server-approved auto-submit (D-05) selalu Pass
        /// hingga grace.
        /// </summary>
        public static SubmitTimerDecision EvaluateSubmitTimerDecision(
            double elapsedSec, double allowedSec, double graceSec, bool serverApprovedAutoSubmit)
        {
            if (elapsedSec >= graceSec) return SubmitTimerDecision.BlockGrace;
            if (elapsedSec >= allowedSec && !serverApprovedAutoSubmit) return SubmitTimerDecision.BlockNoGrace;
            return SubmitTimerDecision.Pass;
        }

        /// <summary>
        /// TOK-02 (WSE-10 / T-382-09): apakah handler harus menolak karena belum lewat lobby token.
        /// StartedAt di-set HANYA setelah VerifyToken sukses (StartExam) → token-required && StartedAt==null
        /// = proxy "belum lewat lobby token" → reject. Sesi non-token (IsTokenRequired=false) tak pernah ter-gate.
        /// </summary>
        public static bool ShouldGateMissingStart(bool isTokenRequired, DateTime? startedAt)
            => isTokenRequired && startedAt == null;

        /// <summary>
        /// TMR-03 (WSE-09 / T-382-10): apakah AutoSubmitToken boleh dikonsumsi (di-remove). HANYA pada grading sukses.
        /// Bila grading gagal (DB hiccup), token TIDAK dikonsumsi → retry aman (tak permanent-reject).
        /// </summary>
        public static bool ShouldConsumeAutoSubmitToken(bool gradingSucceeded) => gradingSucceeded;

        /// <summary>Hasil evaluasi kelengkapan submit on-time (GRDF-07).</summary>
        public readonly record struct OnTimeCompletionResult(bool Blocked, bool EmptyEssay, int Unanswered);

        /// <summary>
        /// Phase 424 GRDF-07 (VAL-03) — keputusan blokir submit on-time: soal Essay "terjawab" HANYA bila
        /// TextAnswer non-kosong (server-authoritative, bukan baris-ada); MC/MA terjawab via jawaban form
        /// (PackageOptionId>0) atau response ber-opsi di DB. Pure, EF-free, unit-testable (pola
        /// ShouldEnforceSubmitTimer). HANYA dipakai di cabang on-time (!serverTimerExpired) — jalur timeout
        /// TIDAK memakai ini (D-04/PXF-04: timeout + essay kosong tetap finalize PendingGrading).
        /// </summary>
        public static OnTimeCompletionResult EvaluateOnTimeCompletion(
            IReadOnlyCollection<int> shuffledQuestionIds,
            IReadOnlyDictionary<int, string> questionTypeById,
            ISet<int> formAnsweredQuestionIds,
            IReadOnlyCollection<(int questionId, bool hasOption, string? textAnswer)> dbResponses)
        {
            var answered = new HashSet<int>(formAnsweredQuestionIds);
            bool emptyEssay = false;
            foreach (var qId in shuffledQuestionIds)
            {
                var type = questionTypeById.TryGetValue(qId, out var t) ? (t ?? "MultipleChoice") : "MultipleChoice";
                if (type == "Essay")
                {
                    bool filled = dbResponses.Any(r => r.questionId == qId && !string.IsNullOrWhiteSpace(r.textAnswer));
                    if (filled) answered.Add(qId);
                    else emptyEssay = true;                          // essay kosong → belum terjawab
                }
                else if (dbResponses.Any(r => r.questionId == qId && r.hasOption))
                {
                    answered.Add(qId);                              // MC/MA terjawab via response ber-opsi di DB
                }
            }
            int total = shuffledQuestionIds.Count;
            int answeredCount = shuffledQuestionIds.Count(qId => answered.Contains(qId));
            int unanswered = Math.Max(0, total - answeredCount);
            bool blocked = total > 0 && answeredCount < total;
            return new OnTimeCompletionResult(blocked, emptyEssay, unanswered);
        }
        // ================================================================================

        private async Task<IActionResult?> EnsureCanSubmitExamAsync(
            AssessmentSession assessment,
            bool isAutoSubmitClientHint,
            string? autoSubmitToken,
            int sessionId)
        {
            // TMR-01 (WSE-09 / T-382-07): enforce untuk Standard/Online/PreTest/PostTest; skip HANYA Manual/null.
            // Sebelumnya allowlist (Online/PreTest/PostTest) → "Standard" jatuh ke skip = dead code (timer tak ditegakkan).
            // Logika blocklist dipusatkan di pure helper ShouldEnforceSubmitTimer (anti-drift, ter-uji unit).
            if (!ShouldEnforceSubmitTimer(assessment.AssessmentType))
            {
                return null; // Manual / null AssessmentType — skip guard
            }

            // Legacy session: StartedAt null (sessions sebelum Phase 21) — skip check (existing convention preserved).
            if (!assessment.StartedAt.HasValue) return null;

            // Phase 313 WR-02 fix: gunakan satuan dan operator yang konsisten — detik integer, ≥.
            // Selaras dengan ExamSummary GET dan SubmitExam awal.
            var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
            int allowedMinutes = assessment.DurationMinutes + (assessment.ExtraTimeMinutes ?? 0);
            double elapsedSec = elapsed.TotalSeconds;
            double allowedSec = allowedMinutes * 60.0;
            double graceLimitSec = allowedSec + 120.0; // 2-minute grace untuk network latency (existing tier-2)

            // Phase 313 CR-01 fix: server-side token validation menggantikan trust client `isAutoSubmit`.
            // Token = one-shot Guid yang di-issue di ExamSummary GET saat timerExpired=true (TempData).
            // Kalau attacker spoof `isAutoSubmit=true` via DevTools tanpa token valid, Tier-1 tetap reject.
            // TMR-03 (Pitfall 3): JANGAN remove token di sini (pre-grading). Konsumsi ditunda ke SubmitExam
            // success path (setelah GradeAndCompleteAsync sukses) supaya retry pasca-DB-hiccup tidak permanent-reject.
            // TempData.Keep dipanggil supaya token bertahan ke request berikutnya bila pass (read tak meng-clear).
            bool serverApprovedAutoSubmit = false;
            var tempKey = $"AutoSubmitToken_{sessionId}";
            var expectedToken = TempData[tempKey] as string;
            if (!string.IsNullOrEmpty(autoSubmitToken)
                && !string.IsNullOrEmpty(expectedToken)
                && string.Equals(autoSubmitToken, expectedToken, StringComparison.Ordinal))
            {
                serverApprovedAutoSubmit = true;
            }
            // Pertahankan token (peek semantics) — konsumsi hanya di success path (TMR-03).
            TempData.Keep(tempKey);

            // Keputusan tier (pure helper, ter-uji unit). D-05: on-time auto-submit (server-approved) tetap Pass.
            var decision = EvaluateSubmitTimerDecision(elapsedSec, allowedSec, graceLimitSec, serverApprovedAutoSubmit);

            // Tier 1 (Phase 313 NEW + CR-01 hardening): manual reject tanpa grace (D-09 strict 0-grace) + audit.
            if (decision == SubmitTimerDecision.BlockNoGrace)
            {
                await WriteSubmitBlockedAuditAsync(assessment, elapsed, allowedMinutes);
                // D-01 explanatory message — Bahasa Indonesia per CLAUDE.md.
                TempData["Error"] = "Waktu ujian Anda sudah habis. Sistem akan otomatis mengirim jawaban Anda dalam beberapa detik. Mohon tunggu, jangan refresh halaman.";
                return RedirectToAction("StartExam", new { id = assessment.Id });
            }

            // Tier 2 (existing LIFE-03 preserved): auto reject setelah grace.
            // D-06: TIDAK tulis AuditLog Blocked entry untuk Tier 2 (scope minimal, hanya tier-1 yang baru).
            if (decision == SubmitTimerDecision.BlockGrace)
            {
                TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses.";
                return RedirectToAction("StartExam", new { id = assessment.Id });
            }

            return null; // Pass — caller lanjut grading flow (token belum dikonsumsi, di-remove pasca-grading).
        }

        /// <summary>
        /// Tulis AuditLog `SubmitExamBlocked` entry (D-05) untuk Tier-1 manual reject.
        /// Try/catch swallow — audit failure jangan block primary action (Phase 312 T-306-02 precedent).
        /// </summary>
        private async Task WriteSubmitBlockedAuditAsync(
            AssessmentSession assessment,
            TimeSpan elapsed,
            int allowedMinutes)
        {
            try
            {
                var blockUser = await _userManager.GetUserAsync(User);
                // Actor name pattern: NIP - FullName (Phase 312 EnsureCanDeleteAsync line 5567-5570 reference).
                var blockActor = string.IsNullOrWhiteSpace(blockUser?.NIP)
                    ? (blockUser?.FullName ?? "Unknown")
                    : $"{blockUser.NIP} - {blockUser.FullName}";

                // D-05 Description format EXACT — parsing-friendly key=value:
                // "HC/User role manual submit blocked after timeup. Type={...} ElapsedSec={X} AllowedSec={Y} SessionId={id}"
                // Phase 313 WR-04 fix: log dengan presisi detik (bukan truncated minutes) supaya
                // reviewer audit tidak melihat kontradiktif `ElapsedMin == AllowedMin` saat sebenarnya elapsed > allowed.
                var description =
                    $"HC/User role manual submit blocked after timeup. " +
                    $"Type={assessment.AssessmentType} " +
                    $"ElapsedSec={(int)elapsed.TotalSeconds} " +
                    $"AllowedSec={allowedMinutes * 60} " +
                    $"ElapsedMin={(int)elapsed.TotalMinutes} " +
                    $"AllowedMin={allowedMinutes} " +
                    $"SessionId={assessment.Id}";

                await _auditLog.LogAsync(
                    actorUserId: blockUser?.Id ?? "",
                    actorName: blockActor,
                    actionType: "SubmitExamBlocked", // D-05 — Phase 312 {Action}Blocked convention
                    description: description,
                    targetId: assessment.Id,
                    targetType: "AssessmentSession");
            }
            catch (Exception auditEx)
            {
                // Swallow — audit failure tidak boleh block primary action (Phase 312 T-306-02 precedent).
                // Phase 313 WR-03 fix: structured key `event=audit_drop_phase313` untuk dashboard log
                // grep + drop-rate monitoring. Memungkinkan alarm kalau audit-write gagal sistematik.
                _logger.LogWarning(auditEx,
                    "AuditLog SubmitExamBlocked write failed for SessionId={SessionId} Event={Event}",
                    assessment.Id, "audit_drop_phase313");
            }
        }

    }
}
