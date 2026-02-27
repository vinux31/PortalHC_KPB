using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using HcPortal.Models.Competency;
using HcPortal.Helpers;
using System.Text.Json;
using HcPortal.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HcPortal.Controllers
{
    [Authorize]
    public class CMPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;
        private readonly IMemoryCache _cache;

        public CMPController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            AuditLogService auditLog,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
            _auditLog = auditLog;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- HALAMAN 1: SUSUNAN KKJ (MATRIX VIEW) ---
        public async Task<IActionResult> Kkj(string? section)
        {
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            int userLevel = user?.RoleLevel ?? 6;
            
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.SelectedSection = section;
            
            // If Level 1-3 (Admin, HC, Management) and no section selected, show selection page
            if (UserRoles.HasFullAccess(userLevel) && string.IsNullOrEmpty(section))
            {
                return View("KkjSectionSelect");
            }

            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var matrixData = await _context.KkjMatrices
                .OrderBy(k => k.No)
                .ToListAsync();

            return View(matrixData);
        }

        // --- HALAMAN 2: MAPPING KKJ - CPDP ---
        public async Task<IActionResult> Mapping(string? section)
        {
            // If no section selected, show bagian selection page
            if (string.IsNullOrEmpty(section))
            {
                return View("MappingSectionSelect");
            }

            ViewBag.SelectedSection = section;

            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var cpdpData = await _context.CpdpItems
                .Where(c => c.Section == section)
                .OrderBy(c => c.No)
                .ToListAsync();

            return View(cpdpData);
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

        // --- MONITORING TAB LAZY-LOAD ENDPOINT ---
        [HttpGet]
        public async Task<IActionResult> GetMonitorData()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            var cutoff = DateTime.UtcNow.AddDays(-30);
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var monitorSessions = await _context.AssessmentSessions
                .Where(a => ((a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
                         && (a.Status == "Open"
                          || a.Status == "InProgress"
                          || a.Status == "Abandoned"
                          || a.Status == "Upcoming"
                          || (a.Status == "Completed" && a.CompletedAt != null && a.CompletedAt >= cutoff)))
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    a.Status,
                    a.Score,
                    a.IsPassed,
                    a.CompletedAt,
                    a.StartedAt,
                    UserFullName = a.User != null ? a.User.FullName : "Unknown",
                    UserNIP      = a.User != null ? a.User.NIP      : ""
                })
                .ToListAsync();

            // Auto-transition display: show Upcoming as Open when scheduled date+time has arrived in WIB (display-only, no SaveChangesAsync)
            var nowWib = DateTime.UtcNow.AddHours(7);
            monitorSessions = monitorSessions
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    Status       = (a.Status == "Upcoming" && a.Schedule <= nowWib) ? "Open" : a.Status,
                    a.Score,
                    a.IsPassed,
                    a.CompletedAt,
                    a.StartedAt,
                    UserFullName = a.UserFullName,
                    UserNIP      = a.UserNIP
                })
                .ToList();

            var monitorGroups = monitorSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g =>
                {
                    var sessions = g.Select(a =>
                    {
                        string userStatus;
                        if (a.CompletedAt != null || a.Score != null)
                            userStatus = "Completed";
                        else if (a.Status == "Abandoned")
                            userStatus = "Abandoned";
                        else if (a.StartedAt != null)
                            userStatus = "In Progress";
                        else
                            userStatus = "Not started";

                        return new MonitoringSessionViewModel
                        {
                            Id           = a.Id,
                            UserFullName = a.UserFullName,
                            UserNIP      = a.UserNIP,
                            UserStatus   = userStatus,
                            Score        = a.Score,
                            IsPassed     = a.IsPassed,
                            CompletedAt  = a.CompletedAt,
                            StartedAt    = a.StartedAt
                        };
                    }).ToList();

                    bool hasOpen     = g.Any(a => a.Status == "Open" || a.Status == "InProgress");
                    bool hasUpcoming = g.Any(a => a.Status == "Upcoming");
                    string groupStatus = hasOpen ? "Open" : hasUpcoming ? "Upcoming" : "Closed";

                    int completedCount = sessions.Count(s => s.UserStatus == "Completed");
                    int passedCount    = sessions.Count(s => s.IsPassed == true);

                    return new MonitoringGroupViewModel
                    {
                        Title          = g.Key.Title,
                        Category       = g.Key.Category,
                        Schedule       = g.First().Schedule,
                        GroupStatus    = groupStatus,
                        TotalCount     = sessions.Count,
                        CompletedCount = completedCount,
                        PassedCount    = passedCount,
                        Sessions       = sessions
                    };
                })
                .OrderByDescending(g => g.Schedule)
                .ToList();

            return Json(monitorGroups);
        }

        // --- SAVE ANSWER (incremental — called by worker JS on each radio change) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer(int sessionId, int questionId, int optionId)
        {
            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only the session owner may save answers
            if (session.UserId != user.Id)
                return Json(new { success = false, error = "Unauthorized" });

            // Session must still be in progress
            if (session.Status == "Completed" || session.Status == "Abandoned")
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

            return Json(new { success = true });
        }

        // --- SAVE LEGACY ANSWER (auto-save for legacy exam path → UserResponse) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLegacyAnswer(int sessionId, int questionId, int optionId)
        {
            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only the session owner may save answers
            if (session.UserId != user.Id)
                return Json(new { success = false, error = "Unauthorized" });

            // Session must still be in progress
            if (session.Status == "Completed" || session.Status == "Abandoned")
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

            // Closed if ExamWindowCloseDate has been set and is in the past (CloseEarly fired)
            if (session.ExamWindowCloseDate.HasValue && DateTime.UtcNow > session.ExamWindowCloseDate.Value)
                isClosed = true;

            // Also closed if session is already Completed (CloseEarly scored it)
            if (session.Status == "Completed")
                isClosed = true;

            string redirectUrl = isClosed
                ? Url.Action("Results", new { id = sessionId }) ?? "/CMP/Assessment"
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
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            int userLevel = user?.RoleLevel ?? 6;

            // Check if this is an initial load (no filter applied explicitly)
            // We use the hidden input 'isFiltered' from the form to differentiate
            bool isInitialState = string.IsNullOrEmpty(isFiltered);

            // Default section to GAST if not specified, JUST for the dropdown display
            // REMOVED: User wants "Semua Bagian" option
            // if (string.IsNullOrEmpty(section))
            // {
            //     section = "GAST";
            // }

            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.SelectedSection = section; // Can be null/empty for "Semua Bagian"
            ViewBag.SelectedUnit = unit;
            ViewBag.SelectedCategory = category;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = statusFilter;
            ViewBag.IsInitialState = isInitialState;

            // Determine if we should show Status column (Filter Mode) or Stats columns (Default Mode)
            bool isFilterMode = !string.IsNullOrEmpty(category);
            ViewBag.IsFilterMode = isFilterMode;

            // Phase 10: Admin always gets HC worker list (elevated access, not personal view)
            // Only literal Coach/Coachee roles see personal unified records
            bool isCoacheeView = userRole == UserRoles.Coach || userRole == UserRoles.Coachee;
            if (isCoacheeView)
            {
                var unified = await GetUnifiedRecords(user!.Id);
                return View("Records", unified);
            }
            // HC, Admin, Management, Supervisor -> worker list

            // Supervisor view:
            List<WorkerTrainingStatus> workers;

            if (isInitialState)
            {
                // Return empty list on initial load
                workers = new List<WorkerTrainingStatus>();
            }
            else
            {
                // Fetch filtered data
                workers = await GetWorkersInSection(section, unit, category, search, statusFilter);
            }

            var (assessmentHistory, trainingHistory) = await GetAllWorkersHistory();

            return View("RecordsWorkerList", new RecordsWorkerListViewModel
            {
                Workers           = workers,
                AssessmentHistory = assessmentHistory,
                TrainingHistory   = trainingHistory,
                AssessmentTitles  = assessmentHistory
                    .Select(r => r.Title)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList()
            });
        }

        // WORKER DETAIL PAGE - Show individual worker's training records
        public async Task<IActionResult> WorkerDetail(string workerId, string name)
        {
            // Get current user info
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            int userLevel = user?.RoleLevel ?? 6;

            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.WorkerId = workerId;
            ViewBag.WorkerName = name;

            // Phase 10: Get worker's unified records (assessments + trainings)
            var unified = await GetUnifiedRecords(workerId);

            return View("WorkerDetail", unified);
        }
        
        // Phase 19: HC Create Training Record — GET
        [HttpGet]
        public async Task<IActionResult> CreateTrainingRecord()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Load ALL workers system-wide (not section-filtered) for dropdown
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

        // Phase 19: HC Create Training Record — POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainingRecord(CreateTrainingRecordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Validate file if provided
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
                // Re-populate workers dropdown
                var workers = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, u.FullName, u.NIP })
                    .ToListAsync();
                ViewBag.Workers = workers.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
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

            // Create record
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

            TempData["Success"] = "Training record berhasil dibuat.";
            return RedirectToAction("Records", new { isFiltered = "true" });
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
                    return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Ukuran file maksimal 10MB.";
                    return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
                }
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
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
                var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(model.CertificateFile.FileName)}";
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
            return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
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

            _context.TrainingRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Training record berhasil dihapus.";
            return RedirectToAction("WorkerDetail", new { workerId = workerId, name = workerName });
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
                var tokenVerified = TempData[$"TokenVerified_{id}"];
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
            if (assessment.StartedAt == null)
            {
                assessment.Status = "InProgress";
                assessment.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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
                    var rng = new Random();

                    // Build cross-package ShuffledQuestionIds (per user decision: slot-list algorithm)
                    var shuffledIds = BuildCrossPackageAssignment(packages, rng);

                    // Sentinel: store first package ID (no schema change — AssessmentPackageId still required by FK)
                    var sentinelPackage = packages.First();

                    assignment = new UserPackageAssignment
                    {
                        AssessmentSessionId = id,
                        AssessmentPackageId = sentinelPackage.Id,  // sentinel per discretion decision
                        UserId = user.Id,
                        ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
                        ShuffledOptionIdsPerQuestion = "{}"  // option shuffle removed per user decision
                    };
                    _context.UserPackageAssignments.Add(assignment);
                    await _context.SaveChangesAsync();

                    // Record question count for stale-question detection on resume (RESUME-03 safety net)
                    assignment.SavedQuestionCount = shuffledIds.Count;
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

            // Single package: return questions in original DB order (no shuffle per user decision)
            if (packages.Count == 1)
            {
                return packages[0].Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
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

        // ... existing code ...

        #region Question Management
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

            var newQuestion = new AssessmentQuestion
            {
                AssessmentSessionId = has_id,
                QuestionText = question_text,
                QuestionType = "MultipleChoice",
                ScoreValue = 10,
                Order = await _context.AssessmentQuestions.CountAsync(q => q.AssessmentSessionId == has_id) + 1
            };

            _context.AssessmentQuestions.Add(newQuestion);
            await _context.SaveChangesAsync(); // Save to get ID

            // Add Options
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
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageQuestions", new { id = has_id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.AssessmentQuestions.FindAsync(id);
            if (question == null) return NotFound();

            int assessmentId = question.AssessmentSessionId;
            _context.AssessmentQuestions.Remove(question);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageQuestions", new { id = assessmentId });
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
            if (assessment.UserId != user.Id && !User.IsInRole("Admin"))
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
                    var existingResponse = await _context.PackageUserResponses
                        .FirstOrDefaultAsync(r => r.AssessmentSessionId == id && r.PackageQuestionId == q.Id);
                    if (existingResponse != null)
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

                assessment.Score = finalPercentage;
                assessment.Status = "Completed";
                assessment.Progress = 100;
                assessment.IsPassed = finalPercentage >= assessment.PassPercentage;
                assessment.CompletedAt = DateTime.UtcNow;

                packageAssignment.IsCompleted = true;

                // Auto-update competency levels (same logic as legacy path)
                if (assessment.IsPassed == true)
                {
                    var mappedCompetencies = await _context.AssessmentCompetencyMaps
                        .Include(m => m.KkjMatrixItem)
                        .Where(m => m.AssessmentCategory == assessment.Category &&
                                    (m.TitlePattern == null || assessment.Title.Contains(m.TitlePattern)))
                        .ToListAsync();

                    if (mappedCompetencies.Any())
                    {
                        var assessmentUser = await _context.Users.FindAsync(assessment.UserId);
                        foreach (var mapping in mappedCompetencies)
                        {
                            if (mapping.MinimumScoreRequired.HasValue && assessment.Score < mapping.MinimumScoreRequired.Value)
                                continue;

                            var existingLevel = await _context.UserCompetencyLevels
                                .FirstOrDefaultAsync(c => c.UserId == assessment.UserId &&
                                                         c.KkjMatrixItemId == mapping.KkjMatrixItemId);
                            if (existingLevel == null)
                            {
                                int targetLevel = PositionTargetHelper.GetTargetLevel(mapping.KkjMatrixItem!, assessmentUser?.Position);
                                _context.UserCompetencyLevels.Add(new UserCompetencyLevel
                                {
                                    UserId = assessment.UserId,
                                    KkjMatrixItemId = mapping.KkjMatrixItemId,
                                    CurrentLevel = mapping.LevelGranted,
                                    TargetLevel = targetLevel,
                                    Source = "Assessment",
                                    AssessmentSessionId = assessment.Id,
                                    AchievedAt = DateTime.UtcNow
                                });
                            }
                            else if (mapping.LevelGranted > existingLevel.CurrentLevel)
                            {
                                existingLevel.CurrentLevel = mapping.LevelGranted;
                                existingLevel.Source = "Assessment";
                                existingLevel.AssessmentSessionId = assessment.Id;
                                existingLevel.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }
                }

                _context.AssessmentSessions.Update(assessment);
                await _context.SaveChangesAsync();

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

                assessment.Score = finalPercentage;
                assessment.Status = "Completed";
                assessment.Progress = 100;
                assessment.IsPassed = finalPercentage >= assessment.PassPercentage;
                assessment.CompletedAt = DateTime.UtcNow;

                // ========== AUTO-UPDATE COMPETENCY LEVELS ==========
                if (assessment.IsPassed == true)
                {
                    // Find competencies mapped to this assessment's category
                    var mappedCompetencies = await _context.AssessmentCompetencyMaps
                        .Include(m => m.KkjMatrixItem)
                        .Where(m => m.AssessmentCategory == assessment.Category &&
                                    (m.TitlePattern == null || assessment.Title.Contains(m.TitlePattern)))
                        .ToListAsync();

                    if (mappedCompetencies.Any())
                    {
                        // Get user's position for target level resolution
                        var assessmentUser = await _context.Users.FindAsync(assessment.UserId);

                        foreach (var mapping in mappedCompetencies)
                        {
                            // Check minimum score if specified, otherwise use pass status
                            if (mapping.MinimumScoreRequired.HasValue && assessment.Score < mapping.MinimumScoreRequired.Value)
                                continue;

                            // Check if user already has a level for this competency
                            var existingLevel = await _context.UserCompetencyLevels
                                .FirstOrDefaultAsync(c => c.UserId == assessment.UserId &&
                                                         c.KkjMatrixItemId == mapping.KkjMatrixItemId);

                            if (existingLevel == null)
                            {
                                // Create new competency level record
                                int targetLevel = PositionTargetHelper.GetTargetLevel(mapping.KkjMatrixItem!, assessmentUser?.Position);
                                _context.UserCompetencyLevels.Add(new UserCompetencyLevel
                                {
                                    UserId = assessment.UserId,
                                    KkjMatrixItemId = mapping.KkjMatrixItemId,
                                    CurrentLevel = mapping.LevelGranted,
                                    TargetLevel = targetLevel,
                                    Source = "Assessment",
                                    AssessmentSessionId = assessment.Id,
                                    AchievedAt = DateTime.UtcNow
                                });
                            }
                            else if (mapping.LevelGranted > existingLevel.CurrentLevel)
                            {
                                // Only upgrade, never downgrade (monotonic progression)
                                existingLevel.CurrentLevel = mapping.LevelGranted;
                                existingLevel.Source = "Assessment";
                                existingLevel.AssessmentSessionId = assessment.Id;
                                existingLevel.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }
                }

                _context.AssessmentSessions.Update(assessment);
                await _context.SaveChangesAsync();

                // Redirect to Results Page
                return RedirectToAction("Results", new { id = id });
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
                    CompletedAt = assessment.CompletedAt,
                    TotalQuestions = orderedQuestionIds.Count,
                    CorrectAnswers = correctCount,
                    QuestionReviews = questionReviews
                };
            }
            else
            {
                // Legacy path: existing UserResponse + AssessmentQuestion data
                if (assessment.AllowAnswerReview)
                {
                    questionReviews = new List<QuestionReviewItem>();
                    int questionNum = 0;
                    foreach (var question in assessment.Questions)
                    {
                        questionNum++;
                        var userResponse = assessment.Responses
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
                    foreach (var question in assessment.Questions)
                    {
                        var userResponse = assessment.Responses
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
                    CompletedAt = assessment.CompletedAt,
                    TotalQuestions = assessment.Questions.Count,
                    CorrectAnswers = correctCount,
                    QuestionReviews = questionReviews
                };
            }

            // ========== COMPETENCY GAINS (shared by both paths) ==========
            if (viewModel.IsPassed)
            {
                var competencyMappings = await _context.AssessmentCompetencyMaps
                    .Include(m => m.KkjMatrixItem)
                    .Where(m => m.AssessmentCategory == assessment.Category &&
                                (m.TitlePattern == null || assessment.Title.Contains(m.TitlePattern)))
                    .ToListAsync();

                if (competencyMappings.Any())
                {
                    viewModel.CompetencyGains = competencyMappings
                        .Select(m => new CompetencyGainItem
                        {
                            CompetencyName = m.KkjMatrixItem?.Kompetensi ?? "Unknown",
                            LevelGranted = m.LevelGranted
                        })
                        .ToList();
                }
            }

            return View(viewModel);
        }
        #endregion

        #region Package Management

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

            // Determine next package number
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

            // Delete PackageUserResponses that reference questions in this package
            var questionIds = pkg.Questions.Select(q => q.Id).ToList();
            if (questionIds.Any())
            {
                var pkgResponses = await _context.PackageUserResponses
                    .Where(r => questionIds.Contains(r.PackageQuestionId))
                    .ToListAsync();
                if (pkgResponses.Any())
                    _context.PackageUserResponses.RemoveRange(pkgResponses);
            }

            // Delete UserPackageAssignments that reference this package
            var assignments = await _context.UserPackageAssignments
                .Where(a => a.AssessmentPackageId == packageId)
                .ToListAsync();
            if (assignments.Any())
                _context.UserPackageAssignments.RemoveRange(assignments);

            // Cascade: options -> questions -> package
            foreach (var q in pkg.Questions)
                _context.PackageOptions.RemoveRange(q.Options);
            _context.PackageQuestions.RemoveRange(pkg.Questions);
            _context.AssessmentPackages.Remove(pkg);

            await _context.SaveChangesAsync();

            try
            {
                var delUser = await _userManager.GetUserAsync(User);
                var delActorName = $"{delUser?.NIP ?? "?"} - {delUser?.FullName ?? "Unknown"}";
                await _auditLog.LogAsync(
                    delUser?.Id ?? "",
                    delActorName,
                    "DeletePackage",
                    $"Deleted package '{pkg.PackageName}' from assessment [ID={assessmentId}]" +
                        (assignments.Any() ? $" ({assignments.Count} assignment(s) removed)" : ""),
                    assessmentId,
                    "AssessmentPackage");
            }
            catch { /* audit failure must not roll back successful delete */ }

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
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            // Build fingerprint set from existing package questions (for deduplication)
            var existingFingerprints = pkg.Questions.Select(q =>
            {
                var opts = q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList();
                return MakeFingerprint(
                    q.QuestionText,
                    opts.ElementAtOrDefault(0) ?? "",
                    opts.ElementAtOrDefault(1) ?? "",
                    opts.ElementAtOrDefault(2) ?? "",
                    opts.ElementAtOrDefault(3) ?? "");
            }).ToHashSet();
            var seenInBatch = new HashSet<string>();

            List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct)> rows;
            var errors = new List<string>();

            if (excelFile != null && excelFile.Length > 0)
            {
                // Parse xlsx with ClosedXML
                rows = new List<(string, string, string, string, string, string)>();
                try
                {
                    using var stream = excelFile.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    int rowNum = 1;
                    foreach (var row in ws.RowsUsed().Skip(1)) // skip header row
                    {
                        rowNum++;
                        var q   = row.Cell(1).GetString().Trim();
                        var a   = row.Cell(2).GetString().Trim();
                        var b   = row.Cell(3).GetString().Trim();
                        var c   = row.Cell(4).GetString().Trim();
                        var d   = row.Cell(5).GetString().Trim();
                        var cor = row.Cell(6).GetString().Trim().ToUpper();
                        rows.Add((q, a, b, c, d, cor));
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Could not read Excel file: {ex.Message}";
                    return RedirectToAction("ImportPackageQuestions", new { packageId });
                }
            }
            else if (!string.IsNullOrWhiteSpace(pasteText))
            {
                // Parse tab-separated paste (skip first line if it looks like a header)
                rows = new List<(string, string, string, string, string, string)>();
                var lines = pasteText.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                // Detect and skip header: if first row's last cell is "Correct" (case-insensitive)
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
                        errors.Add($"Row {i + 1}: expected 6 columns (Question|OptA|OptB|OptC|OptD|Correct), got {cells.Length}.");
                        continue;
                    }
                    rows.Add((
                        cells[0].Trim(), cells[1].Trim(), cells[2].Trim(),
                        cells[3].Trim(), cells[4].Trim(), cells[5].Trim().ToUpper()
                    ));
                }
            }
            else
            {
                TempData["Error"] = "Please upload an Excel file or paste question data.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Cross-package count validation (per user decision):
            // If other sibling packages already have questions, imported count must match their count
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
                        var (rq, ra, rb, rc, rd, rcor) = r;
                        var normalizedCor = ExtractCorrectLetter(rcor);
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

            // Validate and persist rows
            int order = pkg.Questions.Count + 1;
            int added = 0;
            int skipped = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var (q, a, b, c, d, cor) = rows[i];
                // Normalize Correct column: accept "A", "B. text", "OPTION C", etc. — extract the letter
                var normalizedCor = ExtractCorrectLetter(cor);
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

                // Deduplication: skip rows already in the package or seen in this import batch
                var fp = MakeFingerprint(q, a, b, c, d);
                if (existingFingerprints.Contains(fp) || seenInBatch.Contains(fp))
                {
                    skipped++;
                    continue;
                }
                seenInBatch.Add(fp);

                var newQ = new PackageQuestion
                {
                    AssessmentPackageId = packageId,
                    QuestionText = q,
                    Order = order++,
                    ScoreValue = 10
                };
                _context.PackageQuestions.Add(newQ);
                await _context.SaveChangesAsync(); // flush to get ID

                // Correct index: A=0, B=1, C=2, D=3
                int correctIndex = normalizedCor == "A" ? 0 : normalizedCor == "B" ? 1 : normalizedCor == "C" ? 2 : 3;
                var opts = new[] { a, b, c, d };
                for (int oi = 0; oi < opts.Length; oi++)
                {
                    _context.PackageOptions.Add(new PackageOption
                    {
                        PackageQuestionId = newQ.Id,
                        OptionText = opts[oi],
                        IsCorrect = (oi == correctIndex)
                    });
                }
                await _context.SaveChangesAsync();
                added++;
            }

            // 0-valid-rows: something submitted but nothing parseable
            if (added == 0 && skipped == 0)
            {
                TempData["Warning"] = "No valid questions found in the import. Check the format and try again.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // All-duplicates: every parseable row was already in the package
            if (added == 0 && skipped > 0)
            {
                TempData["Warning"] = "All questions were already in the package. Nothing was added.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Normal success: at least 1 row was added
            if (excelFile != null && excelFile.Length > 0)
                TempData["Success"] = $"Imported from file: {added} added, {skipped} skipped.";
            else
                TempData["Success"] = $"{added} added, {skipped} skipped.";

            return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
        }

        #endregion

        // --- CPDP PROGRESS TRACKING ---
        public async Task<IActionResult> CpdpProgress(string? userId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isHcOrAdmin = userRoles.Contains("Admin") || userRoles.Contains("HC");

            var targetUserId = (isHcOrAdmin && !string.IsNullOrEmpty(userId)) ? userId : currentUser.Id;
            var targetUser = await _userManager.FindByIdAsync(targetUserId);

            if (targetUser == null) return NotFound();

            // Load all CPDP items
            var cpdpItems = await _context.CpdpItems.OrderBy(c => c.No).ToListAsync();

            // Load user's competency levels
            var userLevels = await _context.UserCompetencyLevels
                .Include(c => c.KkjMatrixItem)
                .Include(c => c.AssessmentSession)
                .Where(c => c.UserId == targetUserId)
                .ToListAsync();

            // Load all assessment-competency mappings to find linked assessments
            var competencyMaps = await _context.AssessmentCompetencyMaps
                .Include(m => m.KkjMatrixItem)
                .ToListAsync();

            // Load user's completed assessments for evidence
            var userAssessments = await _context.AssessmentSessions
                .Where(a => a.UserId == targetUserId && a.Status == "Completed")
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            // Load user's IDP items for cross-referencing
            var userIdpItems = await _context.IdpItems
                .Where(i => i.UserId == targetUserId)
                .ToListAsync();

            // Load KKJ items for target level resolution
            var kkjItems = await _context.KkjMatrices.ToListAsync();

            // Build progress items
            var progressItems = cpdpItems.Select(cpdp =>
            {
                // Find matching KKJ competency by name
                var matchingKkj = kkjItems.FirstOrDefault(k =>
                    k.Kompetensi.Contains(cpdp.NamaKompetensi, StringComparison.OrdinalIgnoreCase) ||
                    cpdp.NamaKompetensi.Contains(k.Kompetensi, StringComparison.OrdinalIgnoreCase));

                // Get user's competency level for this KKJ item
                var userLevel = matchingKkj != null
                    ? userLevels.FirstOrDefault(ul => ul.KkjMatrixItemId == matchingKkj.Id)
                    : null;

                int? currentLevel = userLevel?.CurrentLevel;
                int? targetLevel = matchingKkj != null
                    ? PositionTargetHelper.GetTargetLevel(matchingKkj, targetUser.Position)
                    : null;

                // Determine competency status
                string compStatus = "Not Tracked";
                if (targetLevel.HasValue && targetLevel.Value > 0)
                {
                    if (currentLevel.HasValue && currentLevel.Value >= targetLevel.Value)
                        compStatus = "Met";
                    else if (currentLevel.HasValue && currentLevel.Value > 0)
                        compStatus = "Gap";
                    else
                        compStatus = "Not Started";
                }

                // Find assessment evidence: assessments mapped to this competency's KKJ item
                var evidences = new List<AssessmentEvidence>();
                if (matchingKkj != null)
                {
                    var mappingsForCompetency = competencyMaps
                        .Where(m => m.KkjMatrixItemId == matchingKkj.Id)
                        .ToList();

                    foreach (var mapping in mappingsForCompetency)
                    {
                        var matchingAssessments = userAssessments
                            .Where(a => a.Category == mapping.AssessmentCategory &&
                                       (mapping.TitlePattern == null || a.Title.Contains(mapping.TitlePattern)))
                            .ToList();

                        foreach (var assessment in matchingAssessments)
                        {
                            // Avoid duplicate evidence entries
                            if (!evidences.Any(e => e.AssessmentSessionId == assessment.Id))
                            {
                                evidences.Add(new AssessmentEvidence
                                {
                                    AssessmentSessionId = assessment.Id,
                                    Title = assessment.Title,
                                    Category = assessment.Category,
                                    Score = assessment.Score,
                                    IsPassed = assessment.IsPassed,
                                    CompletedAt = assessment.CompletedAt,
                                    LevelGranted = mapping.LevelGranted
                                });
                            }
                        }
                    }
                }

                // Check IDP activity
                var idpMatch = userIdpItems.FirstOrDefault(i =>
                    i.Kompetensi != null && (
                        cpdp.NamaKompetensi.Contains(i.Kompetensi, StringComparison.OrdinalIgnoreCase) ||
                        i.Kompetensi.Contains(cpdp.NamaKompetensi, StringComparison.OrdinalIgnoreCase)));

                return new CpdpProgressItem
                {
                    CpdpItemId = cpdp.Id,
                    No = cpdp.No,
                    NamaKompetensi = cpdp.NamaKompetensi,
                    IndikatorPerilaku = cpdp.IndikatorPerilaku,
                    Silabus = cpdp.Silabus,
                    TargetDeliverable = cpdp.TargetDeliverable,
                    CpdpStatus = cpdp.Status,
                    CurrentLevel = currentLevel,
                    TargetLevel = targetLevel > 0 ? targetLevel : null,
                    CompetencyStatus = compStatus,
                    Evidences = evidences.OrderByDescending(e => e.CompletedAt).ToList(),
                    HasIdpActivity = idpMatch != null,
                    IdpStatus = idpMatch?.Status
                };
            }).ToList();

            var viewModel = new CpdpProgressViewModel
            {
                UserId = targetUserId,
                UserName = targetUser.FullName,
                Position = targetUser.Position,
                Section = targetUser.Section,
                Items = progressItems,
                TotalCpdpItems = progressItems.Count,
                ItemsWithEvidence = progressItems.Count(i => i.Evidences.Any()),
                EvidenceCoverage = progressItems.Count > 0
                    ? Math.Round(progressItems.Count(i => i.Evidences.Any()) * 100.0 / progressItems.Count, 1)
                    : 0
            };

            if (isHcOrAdmin)
            {
                var allUsers = await _userManager.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, u.FullName, u.Position, u.Section })
                    .ToListAsync();
                ViewBag.AllUsers = allUsers;
                ViewBag.SelectedUserId = targetUserId;
            }

            ViewBag.IsHcOrAdmin = isHcOrAdmin;

            return View(viewModel);
        }

        #region Worker Management (CRUD)

        // --- MANAGE WORKERS: LIST ALL USERS ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? roleFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

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

            // Filter by role level
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var roleLevel = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == roleLevel);
            }

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
            ViewBag.RoleFilter = roleFilter;

            return View(users);
        }

        // --- CREATE WORKER: GET ---
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

        // --- CREATE WORKER: POST ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorker(ManageUserViewModel model)
        {
            // Password is required for create
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password harus diisi untuk user baru");
            }

            if (!ModelState.IsValid)
            {
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
            var selectedView = model.Role switch
            {
                "Admin" => "Admin",
                "HC" => "HC",
                "Coach" => "Coach",
                "Direktur" or "VP" or "Manager" or "Section Head" or "Sr Supervisor" => "Atasan",
                _ => "Coachee"
            };

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

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                // Audit log
                try
                {
                    var actor = await _userManager.GetUserAsync(User);
                    var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
                    await _auditLog.LogAsync(
                        actor?.Id ?? "",
                        actorName,
                        "CreateWorker",
                        $"Created user '{model.FullName}' ({model.Email}) with role '{model.Role}'",
                        null,
                        "ApplicationUser");
                }
                catch { /* audit failure must not block creation */ }

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

        // --- EDIT WORKER: GET ---
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

        // --- EDIT WORKER: POST ---
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
                user.SelectedView = model.Role switch
                {
                    "Admin" => "Admin",
                    "HC" => "HC",
                    "Coach" => "Coach",
                    "Direktur" or "VP" or "Manager" or "Section Head" or "Sr Supervisor" => "Atasan",
                    _ => "Coachee"
                };

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

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(model.Password))
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
                var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
                await _auditLog.LogAsync(
                    actor?.Id ?? "",
                    actorName,
                    "EditWorker",
                    $"Updated user '{model.FullName}' ({model.Email}). Changes: {(changes.Any() ? string.Join("; ", changes) : "none")}",
                    null,
                    "ApplicationUser");
            }
            catch { /* audit failure must not block update */ }

            TempData["Success"] = $"Data user '{model.FullName}' berhasil diperbarui.";
            return RedirectToAction("ManageWorkers");
        }

        // --- DELETE WORKER: POST ---
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
                    var actorName = $"{currentUser.NIP ?? "?"} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser.Id,
                        actorName,
                        "DeleteWorker",
                        $"Deleted user '{userName}' ({userEmail})",
                        null,
                        "ApplicationUser");
                }
                catch { /* audit failure must not block deletion */ }

                TempData["Success"] = $"User '{userName}' berhasil dihapus dari sistem.";
            }
            else
            {
                TempData["Error"] = $"Gagal menghapus user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction("ManageWorkers");
        }

        // --- EXPORT WORKERS TO EXCEL ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportWorkers(string? search, string? sectionFilter, string? roleFilter)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s) ||
                    (u.NIP != null && u.NIP.Contains(s)));
            }
            if (!string.IsNullOrEmpty(sectionFilter))
                query = query.Where(u => u.Section == sectionFilter);
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var level = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == level);
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            var roleDict = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                roleDict[u.Id] = roles.FirstOrDefault() ?? "-";
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Workers");

            var headers = new[] { "No", "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 2, no = 1;
            foreach (var u in users)
            {
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = u.FullName;
                ws.Cell(row, 3).Value = u.Email;
                ws.Cell(row, 4).Value = u.NIP ?? "";
                ws.Cell(row, 5).Value = u.Position ?? "";
                ws.Cell(row, 6).Value = u.Section ?? "";
                ws.Cell(row, 7).Value = u.Unit ?? "";
                ws.Cell(row, 8).Value = u.Directorate ?? "";
                ws.Cell(row, 9).Value = roleDict.ContainsKey(u.Id) ? roleDict[u.Id] : "-";
                ws.Cell(row, 10).Value = u.JoinDate.HasValue ? u.JoinDate.Value.ToString("yyyy-MM-dd") : "";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"workers_export_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // --- WORKER DETAIL VIEW ---
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

        // --- IMPORT WORKERS: GET ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult ImportWorkers()
        {
            return View();
        }

        // --- DOWNLOAD IMPORT TEMPLATE ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import Workers");

            var headers = new[] { "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung (YYYY-MM-DD)", "Password" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var example = new[] { "Ahmad Fauzi", "ahmad.fauzi@pertamina.com", "123456", "Operator", "RFCC", "RFCC LPG Treating Unit (062)", "CSU Process", "Coachee", "2024-01-15", "Password123!" };
            for (int i = 0; i < example.Length; i++)
            {
                ws.Cell(2, i + 1).Value = example[i];
                ws.Cell(2, i + 1).Style.Font.Italic = true;
                ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray;
            }

            ws.Cell(3, 1).Value = "Kolom Bagian: RFCC / DHT / HMU / NGP / GAST";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;
            ws.Cell(4, 1).Value = $"Kolom Role: {string.Join(" / ", UserRoles.AllRoles)}";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;

            ws.Columns().AdjustToContents();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "workers_import_template.xlsx");
        }

        // --- IMPORT WORKERS: POST ---
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

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nama = row.Cell(1).GetString().Trim();
                    var email = row.Cell(2).GetString().Trim();
                    var nip = row.Cell(3).GetString().Trim();
                    var jabatan = row.Cell(4).GetString().Trim();
                    var bagian = row.Cell(5).GetString().Trim();
                    var unit = row.Cell(6).GetString().Trim();
                    var directorate = row.Cell(7).GetString().Trim();
                    var role = row.Cell(8).GetString().Trim();
                    var tglStr = row.Cell(9).GetString().Trim();
                    var password = row.Cell(10).GetString().Trim();

                    // Skip blank rows (e.g. notes/example rows)
                    if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(email)) continue;

                    var result = new ImportWorkerResult { Nama = nama, Email = email, Role = role };

                    var errors = new List<string>();
                    if (string.IsNullOrWhiteSpace(nama)) errors.Add("Nama kosong");
                    if (string.IsNullOrWhiteSpace(email)) errors.Add("Email kosong");
                    if (string.IsNullOrWhiteSpace(password)) errors.Add("Password kosong");
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
                        result.Status = "Skip";
                        result.Message = "Email sudah terdaftar, dilewati";
                        results.Add(result);
                        continue;
                    }

                    DateTime? joinDate = null;
                    if (!string.IsNullOrWhiteSpace(tglStr) && DateTime.TryParse(tglStr, out var parsedDate))
                        joinDate = parsedDate;

                    var roleLevel = UserRoles.GetRoleLevel(role);
                    var selectedView = role switch
                    {
                        "Admin" => "Admin",
                        "HC" => "HC",
                        "Coach" => "Coach",
                        "Direktur" or "VP" or "Manager" or "Section Head" or "Sr Supervisor" => "Atasan",
                        _ => "Coachee"
                    };

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
                TempData["Error"] = $"Gagal membaca file: {ex.Message}";
                return View();
            }

            // Audit log
            try
            {
                var actor = await _userManager.GetUserAsync(User);
                var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
                var successCount = results.Count(r => r.Status == "Success");
                await _auditLog.LogAsync(actor?.Id ?? "", actorName, "ImportWorkers",
                    $"Bulk import: {successCount} berhasil, {results.Count(r => r.Status == "Error")} error, {results.Count(r => r.Status == "Skip")} dilewati",
                    null, "ApplicationUser");
            }
            catch { }

            ViewBag.ImportResults = results;
            return View();
        }

        #endregion

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
    }
}
