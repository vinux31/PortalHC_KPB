using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Models.Competency;
using HcPortal.Data;
using HcPortal.Helpers;
using ClosedXML.Excel;

namespace HcPortal.Controllers
{
    [Authorize]
    public class CDPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CDPController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PlanIdp(string? bagian = null, string? unit = null, string? level = null)
        {
            // Get current user and their role
            var user = await _userManager.GetUserAsync(User);
            string userRole = "Operator"; // Default
            int userLevel = 6; // Default: Coachee

            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRole = roles.FirstOrDefault() ?? "Operator";
                userLevel = user.RoleLevel;
            }

            // ========== COACHEE ROLE PATH (Proton DB view) ==========
            bool isCoacheeView = userRole == UserRoles.Coachee ||
                                 (userRole == UserRoles.Admin && user != null && user.SelectedView == "Coachee");

            if (isCoacheeView && user != null)
            {
                var assignment = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == user.Id && a.IsActive)
                    .FirstOrDefaultAsync();

                if (assignment == null)
                {
                    ViewBag.UserRole = userRole;
                    ViewBag.NoAssignment = true;
                    return View();
                }

                var kompetensiList = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .Where(k => k.TrackType == assignment.TrackType && k.TahunKe == assignment.TahunKe)
                    .OrderBy(k => k.Urutan)
                    .ToListAsync();

                var activeProgress = await _context.ProtonDeliverableProgresses
                    .Where(p => p.CoacheeId == user.Id && p.Status == "Active")
                    .FirstOrDefaultAsync();

                // Phase 6: load final assessment for PROTN-08
                var finalAssessment = await _context.ProtonFinalAssessments
                    .Where(fa => fa.CoacheeId == user.Id)
                    .OrderByDescending(fa => fa.CreatedAt)
                    .FirstOrDefaultAsync();

                var protonViewModel = new ProtonPlanViewModel
                {
                    TrackType = assignment.TrackType,
                    TahunKe = assignment.TahunKe,
                    KompetensiList = kompetensiList,
                    ActiveProgress = activeProgress,
                    FinalAssessment = finalAssessment
                };

                ViewBag.UserRole = userRole;
                ViewBag.IsProtonView = true;
                return View(protonViewModel);
            }

            // ========== VIEW-BASED FILTERING FOR ADMIN ==========
            if (userRole == UserRoles.Admin)
            {
                if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
                {
                    // Show only user's documents
                    bagian = null; // Override: user's section
                    unit = null; // Override: user's unit
                    level = null; // Override: user's level
                }
                else if (user.SelectedView == "HC")
                {
                    // Show all documents (keep filters as-is)
                    // bagian, unit, level remain from parameters
                }
                // For Atasan view, use existing logic (filter by bagian)
            }
            // For non-admin or admin without specific view, use existing logic

            // Check if HC user has selected a bagian
            bool hasBagianSelected = !string.IsNullOrEmpty(bagian);

            // Pass role and selection to view
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.HasBagianSelected = hasBagianSelected;
            ViewBag.SelectedBagian = bagian ?? "GAST";
            ViewBag.SelectedUnit = unit ?? "RFCC NHT";
            ViewBag.SelectedLevel = level ?? "Operator";

            // Build PDF filename based on selection
            var pdfFileName = $"{(bagian ?? "GAST").Replace(" ", "").Replace("/", "")}_{(unit ?? "RFCC NHT").Replace(" ", "").Replace("/", "")}_{level ?? "Operator"}_Kompetensi_02022026.pdf";
            ViewBag.PdfFileName = pdfFileName;

            return View();
        }

        public async Task<IActionResult> Dashboard(
            string? analyticsCategory = null,
            DateTime? analyticsStartDate = null,
            DateTime? analyticsEndDate = null,
            string? analyticsSection = null,
            string? analyticsUserSearch = null,
            int analyticsPage = 1,
            int analyticsPageSize = 20)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";

            var model = new CDPDashboardViewModel { CurrentUserRole = userRole };

            // === COACHEE BRANCH: literal Coachee role only (Admin simulating Coachee is NOT a literal Coachee) ===
            bool isLiteralCoachee = userRole == UserRoles.Coachee;
            if (isLiteralCoachee)
            {
                model.CoacheeData = await BuildCoacheeSubModelAsync(user.Id);
                return View(model);
            }

            // === PROTON PROGRESS: all non-Coachee roles ===
            model.ProtonProgressData = await BuildProtonProgressSubModelAsync(user, userRole);
            model.ScopeLabel = _lastScopeLabel;

            // === ANALYTICS: HC/Admin regardless of SelectedView (per Phase 12 locked decision) ===
            bool isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin;
            if (isHCAccess)
            {
                model.AssessmentAnalyticsData = await BuildAnalyticsSubModelAsync(
                    analyticsCategory, analyticsStartDate, analyticsEndDate,
                    analyticsSection, analyticsUserSearch, analyticsPage, analyticsPageSize);
            }

            return View(model);
        }

        // ============================================================
        // Helper: Coachee personal deliverable sub-model
        // ============================================================
        private async Task<CoacheeDashboardSubModel> BuildCoacheeSubModelAsync(string userId)
        {
            var subModel = new CoacheeDashboardSubModel();

            var assignment = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == userId && a.IsActive)
                .FirstOrDefaultAsync();

            if (assignment == null)
                return subModel;

            subModel.TrackType = assignment.TrackType;
            subModel.TahunKe = assignment.TahunKe;

            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.CoacheeId == userId)
                .ToListAsync();

            subModel.TotalDeliverables = progresses.Count;
            subModel.ApprovedDeliverables = progresses.Count(p => p.Status == "Approved");
            subModel.ActiveDeliverables = progresses.Count(p => p.Status == "Active");

            var finalAssessment = await _context.ProtonFinalAssessments
                .Where(fa => fa.CoacheeId == userId)
                .OrderByDescending(fa => fa.CreatedAt)
                .FirstOrDefaultAsync();

            subModel.CompetencyLevelGranted = finalAssessment?.CompetencyLevelGranted;
            subModel.CurrentStatus = finalAssessment != null ? "Completed" : "In Progress";

            return subModel;
        }

        // ============================================================
        // Helper: Proton Progress sub-model (supervisor / HC view)
        // Scoping: HC/Admin=all, SrSpv/SectionHead=section, Coach=unit
        // ============================================================
        private async Task<ProtonProgressSubModel> BuildProtonProgressSubModelAsync(ApplicationUser user, string userRole)
        {
            // DASH-02: Build scoped coachee ID list
            List<string> scopedCoacheeIds;
            string scopeLabel;

            if (userRole == UserRoles.HC || userRole == UserRoles.Admin)
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.RoleLevel == 6)
                    .Select(u => u.Id)
                    .ToListAsync();
                scopeLabel = "All Sections";
            }
            else if (userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead)
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6)
                    .Select(u => u.Id)
                    .ToListAsync();
                scopeLabel = $"Section: {user.Section ?? "(unknown)"}";
            }
            else // Coach
            {
                // Null-guard: fall back to Section if Unit is unset
                if (!string.IsNullOrEmpty(user.Unit))
                {
                    scopedCoacheeIds = await _context.Users
                        .Where(u => u.Unit == user.Unit && u.RoleLevel == 6)
                        .Select(u => u.Id)
                        .ToListAsync();
                    scopeLabel = $"Unit: {user.Unit}";
                }
                else
                {
                    scopedCoacheeIds = await _context.Users
                        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
                        .Select(u => u.Id)
                        .ToListAsync();
                    scopeLabel = $"Section: {user.Section ?? "(unknown)"} (Unit not set)";
                }
            }

            // Batch load data (avoid N+1)
            var coacheeUsers = await _context.Users
                .Where(u => scopedCoacheeIds.Contains(u.Id))
                .ToListAsync();
            var userNames = coacheeUsers.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);

            var allProgresses = await _context.ProtonDeliverableProgresses
                .Where(p => scopedCoacheeIds.Contains(p.CoacheeId))
                .ToListAsync();

            var assignments = await _context.ProtonTrackAssignments
                .Where(a => scopedCoacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();
            var assignmentDict = assignments.ToDictionary(a => a.CoacheeId, a => a);

            var finalAssessments = await _context.ProtonFinalAssessments
                .Where(fa => scopedCoacheeIds.Contains(fa.CoacheeId))
                .ToListAsync();
            var finalAssessmentDict = finalAssessments
                .GroupBy(fa => fa.CoacheeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(fa => fa.CreatedAt).First());

            // Build per-coachee rows (flat table sorted by name)
            var progressByCoachee = allProgresses.GroupBy(p => p.CoacheeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var coacheeRows = new List<CoacheeProgressRow>();
            foreach (var coacheeId in scopedCoacheeIds)
            {
                var progresses = progressByCoachee.GetValueOrDefault(coacheeId) ?? new List<ProtonDeliverableProgress>();
                var assignment = assignmentDict.GetValueOrDefault(coacheeId);
                finalAssessmentDict.TryGetValue(coacheeId, out var finalAssessment);

                coacheeRows.Add(new CoacheeProgressRow
                {
                    CoacheeId = coacheeId,
                    CoacheeName = userNames.GetValueOrDefault(coacheeId, coacheeId),
                    TrackType = assignment?.TrackType ?? "",
                    TahunKe = assignment?.TahunKe ?? "",
                    TotalDeliverables = progresses.Count,
                    Approved = progresses.Count(p => p.Status == "Approved"),
                    Submitted = progresses.Count(p => p.Status == "Submitted"),
                    Rejected = progresses.Count(p => p.Status == "Rejected"),
                    Active = progresses.Count(p => p.Status == "Active"),
                    Locked = progresses.Count(p => p.Status == "Locked"),
                    HasFinalAssessment = finalAssessment != null,
                    CompetencyLevelGranted = finalAssessment?.CompetencyLevelGranted
                });
            }
            coacheeRows = coacheeRows.OrderBy(r => r.CoacheeName).ToList();

            // Stat card totals
            int pendingSpv = allProgresses.Count(p => p.Status == "Submitted");
            int pendingHC  = allProgresses.Count(p => p.HCApprovalStatus == "Pending" && p.Status == "Approved");

            // Trend chart: competency level granted grouped by month
            var scopedCompletedAssessments = finalAssessments
                .Where(fa => fa.CompletedAt.HasValue)
                .OrderBy(fa => fa.CompletedAt)
                .ToList();

            List<string> trendLabels = new();
            List<double> trendValues = new();

            if (scopedCompletedAssessments.Any())
            {
                var grouped = scopedCompletedAssessments
                    .GroupBy(fa => new { fa.CompletedAt!.Value.Year, fa.CompletedAt!.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

                foreach (var g in grouped)
                {
                    trendLabels.Add($"{g.Key.Year}-{g.Key.Month:D2}");
                    trendValues.Add(Math.Round(g.Average(fa => (double)fa.CompetencyLevelGranted), 2));
                }
            }

            // Doughnut chart: status distribution
            var statusLabels = new List<string> { "Approved", "Submitted", "Active", "Rejected", "Locked" };
            var statusData = new List<int>
            {
                allProgresses.Count(p => p.Status == "Approved"),
                allProgresses.Count(p => p.Status == "Submitted"),
                allProgresses.Count(p => p.Status == "Active"),
                allProgresses.Count(p => p.Status == "Rejected"),
                allProgresses.Count(p => p.Status == "Locked")
            };

            var subModel = new ProtonProgressSubModel
            {
                TotalCoachees = scopedCoacheeIds.Count,
                TotalDeliverables = allProgresses.Count,
                ApprovedDeliverables = allProgresses.Count(p => p.Status == "Approved"),
                PendingSpvApprovals = pendingSpv,
                PendingHCReviews = pendingHC,
                CompletedCoachees = finalAssessmentDict.Count,
                CoacheeRows = coacheeRows,
                TrendLabels = trendLabels,
                TrendValues = trendValues,
                StatusLabels = statusLabels,
                StatusData = statusData
            };

            // Propagate scope label to wrapper model via a field that helper sets on ProtonProgressSubModel is not wired
            // ScopeLabel is on CDPDashboardViewModel — set it via caller after this method returns
            // We store it temporarily in a thread-local-safe way: use a private field approach isn't clean
            // Better: pass out via a tuple or set it inline in Dashboard()
            // Since C# doesn't have ref returns cleanly here, we call SetScopeLabel after return
            _lastScopeLabel = scopeLabel;

            return subModel;
        }

        // Stores scope label from BuildProtonProgressSubModelAsync for Dashboard() to retrieve
        private string _lastScopeLabel = "";

        // ============================================================
        // Helper: Assessment Analytics sub-model (HC/Admin only)
        // Logic copied from CMPController.ReportsIndex()
        // ============================================================
        private async Task<AssessmentAnalyticsSubModel> BuildAnalyticsSubModelAsync(
            string? category,
            DateTime? startDate,
            DateTime? endDate,
            string? section,
            string? userSearch,
            int page,
            int pageSize)
        {
            // Base query: only completed assessments
            var query = _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Status == "Completed");

            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            if (startDate.HasValue)
                query = query.Where(a => a.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CompletedAt < endOfDay);
            }

            if (!string.IsNullOrEmpty(section))
                query = query.Where(a => a.User != null && a.User.Section == section);

            if (!string.IsNullOrEmpty(userSearch))
                query = query.Where(a => a.User != null &&
                    (a.User.FullName.Contains(userSearch) ||
                     (a.User.NIP != null && a.User.NIP.Contains(userSearch))));

            // Summary stats
            var totalCompleted = await query.CountAsync();
            var passedCount = await query.CountAsync(a => a.IsPassed == true);
            var avgScore = totalCompleted > 0
                ? await query.AverageAsync(a => (double?)a.Score) ?? 0
                : 0;
            var totalAssigned = await _context.AssessmentSessions.CountAsync();
            var passRate = totalCompleted > 0 ? passedCount * 100.0 / totalCompleted : 0;

            // Category statistics
            var categoryStats = await query
                .GroupBy(a => a.Category)
                .Select(g => new CategoryStatistic
                {
                    CategoryName = g.Key,
                    TotalAssessments = g.Count(),
                    PassedCount = g.Count(a => a.IsPassed == true),
                    PassRate = g.Count() > 0
                        ? Math.Round(g.Count(a => a.IsPassed == true) * 100.0 / g.Count(), 1)
                        : 0,
                    AverageScore = Math.Round(g.Average(a => (double?)a.Score) ?? 0, 1)
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // Score distribution histogram
            var allScores = await query.Select(a => a.Score ?? 0).ToListAsync();
            var scoreDistribution = new List<int>
            {
                allScores.Count(s => s >= 0 && s <= 20),
                allScores.Count(s => s >= 21 && s <= 40),
                allScores.Count(s => s >= 41 && s <= 60),
                allScores.Count(s => s >= 61 && s <= 80),
                allScores.Count(s => s >= 81 && s <= 100)
            };

            // Pagination
            var totalPages = (int)Math.Ceiling(totalCompleted / (double)pageSize);

            var assessments = await query
                .OrderByDescending(a => a.CompletedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AssessmentReportItem
                {
                    Id = a.Id,
                    Title = a.Title,
                    Category = a.Category,
                    UserId = a.UserId,
                    UserName = a.User != null ? a.User.FullName : "Unknown",
                    UserNIP = a.User != null ? a.User.NIP : null,
                    UserSection = a.User != null ? a.User.Section : null,
                    Score = a.Score ?? 0,
                    PassPercentage = a.PassPercentage,
                    IsPassed = a.IsPassed ?? false,
                    CompletedAt = a.CompletedAt
                })
                .ToListAsync();

            // Filter dropdowns
            var categories = await _context.AssessmentSessions
                .Where(a => a.Status == "Completed")
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var sections = OrganizationStructure.GetAllSections();

            return new AssessmentAnalyticsSubModel
            {
                TotalAssigned = totalAssigned,
                TotalCompleted = totalCompleted,
                PassedCount = passedCount,
                PassRate = passRate,
                AverageScore = avgScore,
                Assessments = assessments,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                CurrentFilters = new ReportFilters
                {
                    Category = category,
                    StartDate = startDate,
                    EndDate = endDate,
                    Section = section,
                    UserSearch = userSearch
                },
                AvailableCategories = categories,
                AvailableSections = sections,
                CategoryStats = categoryStats,
                ScoreDistribution = scoreDistribution
            };
        }

        // ============================================================
        // ExportAnalyticsResults: moved from CMPController.ExportResults()
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportAnalyticsResults(
            string? category,
            DateTime? startDate,
            DateTime? endDate,
            string? section,
            string? userSearch)
        {
            // Base query: only completed assessments
            var query = _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Status == "Completed");

            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            if (startDate.HasValue)
                query = query.Where(a => a.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CompletedAt < endOfDay);
            }

            if (!string.IsNullOrEmpty(section))
                query = query.Where(a => a.User != null && a.User.Section == section);

            if (!string.IsNullOrEmpty(userSearch))
                query = query.Where(a => a.User != null &&
                    (a.User.FullName.Contains(userSearch) ||
                     (a.User.NIP != null && a.User.NIP.Contains(userSearch))));

            // Get all matching results (capped at 10,000 for performance)
            var maxExportRows = 10000;
            var results = await query
                .OrderByDescending(a => a.CompletedAt)
                .Take(maxExportRows)
                .Select(a => new {
                    AssessmentTitle = a.Title,
                    Category = a.Category,
                    UserName = a.User != null ? a.User.FullName : "",
                    NIP = a.User != null ? a.User.NIP ?? "" : "",
                    Section = a.User != null ? a.User.Section ?? "" : "",
                    Score = a.Score ?? 0,
                    PassPercentage = a.PassPercentage,
                    Status = a.IsPassed == true ? "Pass" : "Fail",
                    CompletedAt = a.CompletedAt
                })
                .ToListAsync();

            // Generate Excel using ClosedXML
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Assessment Results");

            // Header row
            worksheet.Cell(1, 1).Value = "Assessment Title";
            worksheet.Cell(1, 2).Value = "Category";
            worksheet.Cell(1, 3).Value = "User Name";
            worksheet.Cell(1, 4).Value = "NIP";
            worksheet.Cell(1, 5).Value = "Section";
            worksheet.Cell(1, 6).Value = "Score";
            worksheet.Cell(1, 7).Value = "Pass Percentage";
            worksheet.Cell(1, 8).Value = "Status";
            worksheet.Cell(1, 9).Value = "Completed At";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data rows
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = r.AssessmentTitle;
                worksheet.Cell(row, 2).Value = r.Category;
                worksheet.Cell(row, 3).Value = r.UserName;
                worksheet.Cell(row, 4).Value = r.NIP;
                worksheet.Cell(row, 5).Value = r.Section;
                worksheet.Cell(row, 6).Value = r.Score;
                worksheet.Cell(row, 7).Value = r.PassPercentage;
                worksheet.Cell(row, 8).Value = r.Status;
                worksheet.Cell(row, 9).Value = r.CompletedAt?.ToString("yyyy-MM-dd HH:mm");
            }

            worksheet.Columns().AdjustToContents();

            // Return as file download
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            var fileName = $"AssessmentResults_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ============================================================
        // SearchUsers: moved from CMPController (only used by ReportsIndex autocomplete)
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var users = await _context.Users
                .Where(u => u.FullName.Contains(term) ||
                             (u.NIP != null && u.NIP.Contains(term)))
                .OrderBy(u => u.FullName)
                .Take(10)
                .Select(u => new {
                    fullName = u.FullName,
                    nip = u.NIP ?? "",
                    section = u.Section ?? ""
                })
                .ToListAsync();

            return Json(users);
        }

        public async Task<IActionResult> ProtonMain()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // Only RoleLevel <= 5 or SrSupervisor can access
            if (user.RoleLevel > 5 && userRole != UserRoles.SrSupervisor)
            {
                return Forbid();
            }

            var coachees = await _context.Users
                .Where(u => (string.IsNullOrEmpty(user.Section) || u.Section == user.Section)
                            && u.RoleLevel == 6)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var coacheeIds = coachees.Select(c => c.Id).ToList();

            var assignments = await _context.ProtonTrackAssignments
                .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();

            var activeProgresses = await _context.ProtonDeliverableProgresses
                .Where(p => coacheeIds.Contains(p.CoacheeId) && p.Status == "Active")
                .ToListAsync();

            var viewModel = new ProtonMainViewModel
            {
                Coachees = coachees,
                Assignments = assignments,
                ActiveProgresses = activeProgresses
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTrack(string coacheeId, string trackType, string tahunKe)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // Only RoleLevel <= 5 or SrSupervisor can access
            if (user.RoleLevel > 5 && userRole != UserRoles.SrSupervisor)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(coacheeId) || string.IsNullOrEmpty(trackType) || string.IsNullOrEmpty(tahunKe))
            {
                TempData["Error"] = "Data tidak lengkap.";
                return RedirectToAction("ProtonMain");
            }

            // Deactivate existing assignments
            var existingAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == coacheeId && a.IsActive)
                .ToListAsync();
            foreach (var assignment in existingAssignments)
            {
                assignment.IsActive = false;
            }

            // Delete existing progress records for this coachee
            var existingProgresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.CoacheeId == coacheeId)
                .ToListAsync();
            _context.ProtonDeliverableProgresses.RemoveRange(existingProgresses);

            // Create new assignment
            var newAssignment = new ProtonTrackAssignment
            {
                CoacheeId = coacheeId,
                AssignedById = user.Id,
                TrackType = trackType,
                TahunKe = tahunKe,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            };
            _context.ProtonTrackAssignments.Add(newAssignment);
            await _context.SaveChangesAsync();

            // Query all deliverables for the track
            var deliverables = await _context.ProtonDeliverableList
                .Include(d => d.ProtonSubKompetensi)
                    .ThenInclude(s => s.ProtonKompetensi)
                .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.TrackType == trackType &&
                            d.ProtonSubKompetensi.ProtonKompetensi.TahunKe == tahunKe)
                .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
                    .ThenBy(d => d.ProtonSubKompetensi.Urutan)
                    .ThenBy(d => d.Urutan)
                .ToListAsync();

            // Create progress records — first Active, rest Locked
            var progressList = deliverables.Select((d, index) => new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = d.Id,
                Status = index == 0 ? "Active" : "Locked",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.ProtonDeliverableProgresses.AddRange(progressList);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Track Proton berhasil ditetapkan.";
            return RedirectToAction("ProtonMain");
        }

        public async Task<IActionResult> Deliverable(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Get user role for approval context
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // Load progress with full hierarchy
            var progress = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (progress == null) return NotFound();

            // Access check: coachee themselves OR coach/supervisor (RoleLevel <= 5) OR HC
            bool isCoachee = progress.CoacheeId == user.Id;
            bool isCoach = user.RoleLevel <= 5;
            bool isHC = userRole == UserRoles.HC ||
                        (userRole == UserRoles.Admin && user?.SelectedView == "HC");

            // HC has full access — no section check required
            if (!isCoachee && !isHC && isCoach)
            {
                var coachee = await _context.Users
                    .Where(u => u.Id == progress.CoacheeId)
                    .Select(u => new { u.Section })
                    .FirstOrDefaultAsync();
                if (coachee == null || coachee.Section != user.Section)
                {
                    return Forbid();
                }
            }
            else if (!isCoachee && !isHC && !isCoach)
            {
                return Forbid();
            }

            // Load ALL progress records for this coachee in one query (avoid N+1)
            var allProgresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .Where(p => p.CoacheeId == progress.CoacheeId)
                .ToListAsync();

            // Get track info from current deliverable's hierarchy
            var kompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi;
            string trackType = kompetensi?.TrackType ?? "";
            string tahunKe = kompetensi?.TahunKe ?? "";

            // Order all deliverables for this track by Kompetensi.Urutan, SubKompetensi.Urutan, Deliverable.Urutan
            var orderedProgresses = allProgresses
                .Where(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.TrackType == trackType
                         && p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.TahunKe == tahunKe)
                .OrderBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Urutan ?? 0)
                .ThenBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.Urutan ?? 0)
                .ThenBy(p => p.ProtonDeliverable?.Urutan ?? 0)
                .ToList();

            // Sequential lock check
            int currentIndex = orderedProgresses.FindIndex(p => p.Id == progress.Id);
            bool isAccessible;
            if (currentIndex <= 0)
            {
                // First deliverable — always accessible
                isAccessible = true;
            }
            else
            {
                // Check if previous deliverable is Approved
                var previousProgress = orderedProgresses[currentIndex - 1];
                isAccessible = previousProgress.Status == "Approved";
            }

            // Get coachee name
            string coacheeName = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => u.FullName ?? u.UserName ?? u.Id)
                .FirstOrDefaultAsync() ?? progress.CoacheeId;

            // CanUpload: status is Active or Rejected AND current user is coach/supervisor
            bool canUpload = (progress.Status == "Active" || progress.Status == "Rejected") && user.RoleLevel <= 5;

            // Phase 6: approval context
            bool isAtasanAccess = userRole == UserRoles.SrSupervisor ||
                                  userRole == UserRoles.SectionHead ||
                                  (userRole == UserRoles.Admin &&
                                   (user.SelectedView == "Atasan" || user.SelectedView == "HC"));
            bool canApprove = isAtasanAccess && progress.Status == "Submitted";
            bool canHCReview = isHC && progress.HCApprovalStatus == "Pending";

            var viewModel = new DeliverableViewModel
            {
                Progress = progress,
                Deliverable = progress.ProtonDeliverable,
                CoacheeName = coacheeName,
                TrackType = trackType,
                TahunKe = tahunKe,
                IsAccessible = isAccessible,
                CanUpload = canUpload,
                CanApprove = canApprove,
                CanHCReview = canHCReview,
                CurrentUserRole = userRole ?? ""
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDeliverable(int progressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // SrSupervisor, SectionHead, or Admin simulating Atasan/HC view can approve
            bool isAtasanAccess = userRole == UserRoles.SrSupervisor ||
                                  userRole == UserRoles.SectionHead ||
                                  (userRole == UserRoles.Admin &&
                                   (user.SelectedView == "Atasan" || user.SelectedView == "HC"));
            if (!isAtasanAccess) return Forbid();

            // Load progress with full Include chain
            var progress = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return NotFound();

            // Guard: only Submitted status can be approved
            if (progress.Status != "Submitted")
            {
                TempData["Error"] = "Hanya deliverable dengan status Submitted yang dapat disetujui.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Section check: coachee must be in same section as approver (Admin can approve cross-section)
            var coacheeUser = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.Section })
                .FirstOrDefaultAsync();
            if (userRole != UserRoles.Admin &&
                (coacheeUser == null || coacheeUser.Section != user.Section))
            {
                return Forbid();
            }

            // Get track info for ordering
            var kompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi;
            string trackType = kompetensi?.TrackType ?? "";
            string tahunKe = kompetensi?.TahunKe ?? "";

            // Set approval fields (in memory, before SaveChangesAsync)
            progress.Status = "Approved";
            progress.ApprovedAt = DateTime.UtcNow;
            progress.ApprovedById = user.Id;

            // Load ALL progress records for this coachee's track to unlock next deliverable
            var allProgresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .Where(p => p.CoacheeId == progress.CoacheeId)
                .ToListAsync();

            var orderedProgresses = allProgresses
                .Where(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.TrackType == trackType
                         && p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.TahunKe == tahunKe)
                .OrderBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Urutan ?? 0)
                .ThenBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.Urutan ?? 0)
                .ThenBy(p => p.ProtonDeliverable?.Urutan ?? 0)
                .ToList();

            // Unlock next deliverable: find index of just-approved record and set next to Active
            int currentIndex = orderedProgresses.FindIndex(p => p.Id == progress.Id);
            if (currentIndex >= 0 && currentIndex < orderedProgresses.Count - 1)
            {
                var nextProgress = orderedProgresses[currentIndex + 1];
                if (nextProgress.Status == "Locked")
                {
                    nextProgress.Status = "Active";
                }
            }

            // Check all-approved: use in-memory state (current record already set to "Approved" above)
            bool allApproved = orderedProgresses.All(p => p.Status == "Approved");

            await _context.SaveChangesAsync();

            if (allApproved)
            {
                await CreateHCNotificationAsync(progress.CoacheeId);
            }

            TempData["Success"] = "Deliverable berhasil disetujui.";
            return RedirectToAction("Deliverable", new { id = progressId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDeliverable(int progressId, string rejectionReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // SrSupervisor, SectionHead, or Admin simulating Atasan/HC view can reject
            bool isAtasanAccess = userRole == UserRoles.SrSupervisor ||
                                  userRole == UserRoles.SectionHead ||
                                  (userRole == UserRoles.Admin &&
                                   (user.SelectedView == "Atasan" || user.SelectedView == "HC"));
            if (!isAtasanAccess) return Forbid();

            // Validate rejection reason
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] = "Alasan penolakan tidak boleh kosong.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Load progress record
            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return NotFound();

            // Guard: only Submitted status can be rejected
            if (progress.Status != "Submitted")
            {
                TempData["Error"] = "Hanya deliverable dengan status Submitted yang dapat ditolak.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Section check: coachee must be in same section as rejector (Admin can reject cross-section)
            var coacheeUser = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.Section })
                .FirstOrDefaultAsync();
            if (userRole != UserRoles.Admin &&
                (coacheeUser == null || coacheeUser.Section != user.Section))
            {
                return Forbid();
            }

            // Set rejection fields
            progress.Status = "Rejected";
            progress.RejectedAt = DateTime.UtcNow;
            progress.RejectionReason = rejectionReason;
            progress.ApprovedById = null;
            progress.ApprovedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Deliverable berhasil ditolak.";
            return RedirectToAction("Deliverable", new { id = progressId });
        }

        private async Task CreateHCNotificationAsync(string coacheeId)
        {
            // Deduplication: check if notification already exists for this coachee
            bool alreadyNotified = await _context.ProtonNotifications
                .AnyAsync(n => n.CoacheeId == coacheeId && n.Type == "AllDeliverablesComplete");
            if (alreadyNotified) return;

            var coachee = await _context.Users
                .Where(u => u.Id == coacheeId)
                .Select(u => new { u.FullName, u.UserName })
                .FirstOrDefaultAsync();
            var coacheeName = coachee?.FullName ?? coachee?.UserName ?? coacheeId;

            var hcUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HC);

            var notifications = hcUsers.Select(hc => new ProtonNotification
            {
                RecipientId = hc.Id,
                CoacheeId = coacheeId,
                CoacheeName = coacheeName,
                Message = $"{coacheeName} telah menyelesaikan semua deliverable Proton.",
                Type = "AllDeliverablesComplete",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.ProtonNotifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HCReviewDeliverable(int progressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // HC or Admin simulating HC view can review
            bool isHCAccess = userRole == UserRoles.HC ||
                              (userRole == UserRoles.Admin && user.SelectedView == "HC");
            if (!isHCAccess) return Forbid();

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null) return NotFound();

            // Guard: must be Pending
            if (progress.HCApprovalStatus != "Pending")
            {
                TempData["Error"] = "Deliverable ini sudah diperiksa HC sebelumnya.";
                return RedirectToAction("HCApprovals");
            }

            progress.HCApprovalStatus = "Reviewed";
            progress.HCReviewedAt = DateTime.UtcNow;
            progress.HCReviewedById = user.Id;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Deliverable telah ditandai sebagai sudah diperiksa HC.";
            return RedirectToAction("HCApprovals");
        }

        public async Task<IActionResult> HCApprovals()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // HC or Admin simulating HC view can access
            bool isHCAccess = userRole == UserRoles.HC ||
                              (userRole == UserRoles.Admin && user.SelectedView == "HC");
            if (!isHCAccess) return Forbid();

            // Query pending HC reviews (any deliverable that HC hasn't reviewed yet)
            var pendingReviews = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .Where(p => p.HCApprovalStatus == "Pending"
                         && (p.Status == "Submitted" || p.Status == "Approved" || p.Status == "Rejected"))
                .OrderBy(p => p.SubmittedAt)
                .ToListAsync();

            // Unread notifications for this HC user
            var notifications = await _context.ProtonNotifications
                .Where(n => n.RecipientId == user.Id && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Batch-query coachee names to avoid N+1
            var coacheeIds = pendingReviews.Select(p => p.CoacheeId).Distinct().ToList();
            var userNames = await _context.Users
                .Where(u => coacheeIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);

            // Build "Ready for Final Assessment" list
            var allAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.IsActive)
                .ToListAsync();

            var existingAssessmentCoacheeIds = await _context.ProtonFinalAssessments
                .Select(fa => fa.CoacheeId)
                .Distinct()
                .ToListAsync();

            var readyForAssessment = new List<FinalAssessmentCandidate>();
            foreach (var assignment in allAssignments)
            {
                if (existingAssessmentCoacheeIds.Contains(assignment.CoacheeId)) continue;

                var progresses = await _context.ProtonDeliverableProgresses
                    .Where(p => p.CoacheeId == assignment.CoacheeId)
                    .ToListAsync();

                if (progresses.Any() &&
                    progresses.All(p => p.Status == "Approved") &&
                    progresses.All(p => p.HCApprovalStatus == "Reviewed"))
                {
                    // Add coachee name if not already in dictionary
                    if (!userNames.ContainsKey(assignment.CoacheeId))
                    {
                        var coacheeUser = await _context.Users.FindAsync(assignment.CoacheeId);
                        userNames[assignment.CoacheeId] = coacheeUser?.FullName ?? coacheeUser?.UserName ?? assignment.CoacheeId;
                    }

                    readyForAssessment.Add(new FinalAssessmentCandidate
                    {
                        CoacheeId = assignment.CoacheeId,
                        CoacheeName = userNames[assignment.CoacheeId],
                        TrackAssignmentId = assignment.Id,
                        TrackType = assignment.TrackType,
                        TahunKe = assignment.TahunKe
                    });
                }
            }

            // Build viewModel before marking notifications as read
            var viewModel = new HCApprovalQueueViewModel
            {
                PendingReviews = pendingReviews,
                Notifications = notifications,
                UserNames = userNames,
                ReadyForFinalAssessment = readyForAssessment
            };

            // Mark notifications as read (after building viewModel so they still appear as "new")
            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }
            if (notifications.Any()) await _context.SaveChangesAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> CreateFinalAssessment(int trackAssignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // HC or Admin simulating HC view can access
            bool isHCAccess = userRole == UserRoles.HC ||
                              (userRole == UserRoles.Admin && user.SelectedView == "HC");
            if (!isHCAccess) return Forbid();

            // Load the track assignment
            var assignment = await _context.ProtonTrackAssignments
                .FirstOrDefaultAsync(a => a.Id == trackAssignmentId && a.IsActive);
            if (assignment == null) return NotFound();

            // Check if assessment already exists for this coachee
            var existingAssessment = await _context.ProtonFinalAssessments
                .Where(fa => fa.CoacheeId == assignment.CoacheeId)
                .OrderByDescending(fa => fa.CreatedAt)
                .FirstOrDefaultAsync();

            // Count deliverables
            int totalDeliverables = await _context.ProtonDeliverableProgresses
                .CountAsync(p => p.CoacheeId == assignment.CoacheeId);

            int approvedDeliverables = await _context.ProtonDeliverableProgresses
                .CountAsync(p => p.CoacheeId == assignment.CoacheeId && p.Status == "Approved");

            // Check if all HC reviews are done
            bool allHCReviewed = totalDeliverables > 0 &&
                !await _context.ProtonDeliverableProgresses
                    .AnyAsync(p => p.CoacheeId == assignment.CoacheeId && p.HCApprovalStatus == "Pending");

            // Load available KKJ competencies for dropdown
            var availableCompetencies = await _context.KkjMatrices
                .OrderBy(k => k.SkillGroup)
                .ThenBy(k => k.Kompetensi)
                .ToListAsync();

            // Get coachee name
            var coacheeName = await _context.Users
                .Where(u => u.Id == assignment.CoacheeId)
                .Select(u => u.FullName ?? u.UserName ?? u.Id)
                .FirstOrDefaultAsync() ?? assignment.CoacheeId;

            var viewModel = new FinalAssessmentViewModel
            {
                Assignment = assignment,
                CoacheeName = coacheeName,
                TotalDeliverables = totalDeliverables,
                ApprovedDeliverables = approvedDeliverables,
                AllHCReviewed = allHCReviewed,
                AvailableCompetencies = availableCompetencies,
                ExistingAssessment = existingAssessment
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFinalAssessment(int trackAssignmentId, int competencyLevelGranted, int? kkjMatrixItemId, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // HC or Admin simulating HC view can create
            bool isHCAccess = userRole == UserRoles.HC ||
                              (userRole == UserRoles.Admin && user.SelectedView == "HC");
            if (!isHCAccess) return Forbid();

            // Load assignment
            var assignment = await _context.ProtonTrackAssignments
                .FirstOrDefaultAsync(a => a.Id == trackAssignmentId && a.IsActive);
            if (assignment == null) return NotFound();

            // Guard: no pending HC reviews for this coachee
            bool hasPendingHCReviews = await _context.ProtonDeliverableProgresses
                .AnyAsync(p => p.CoacheeId == assignment.CoacheeId
                            && p.HCApprovalStatus == "Pending"
                            && (p.Status == "Submitted" || p.Status == "Approved"));
            if (hasPendingHCReviews)
            {
                TempData["Error"] = "Selesaikan semua review HC sebelum membuat final assessment.";
                return RedirectToAction("CreateFinalAssessment", new { trackAssignmentId });
            }

            // Guard: no duplicate assessment
            bool alreadyExists = await _context.ProtonFinalAssessments
                .AnyAsync(fa => fa.CoacheeId == assignment.CoacheeId);
            if (alreadyExists)
            {
                TempData["Error"] = "Final assessment sudah dibuat untuk coachee ini.";
                return RedirectToAction("HCApprovals");
            }

            // Validate competency level
            if (competencyLevelGranted < 0 || competencyLevelGranted > 5)
            {
                TempData["Error"] = "Level kompetensi harus antara 0 dan 5.";
                return RedirectToAction("CreateFinalAssessment", new { trackAssignmentId });
            }

            // Create final assessment
            var finalAssessment = new ProtonFinalAssessment
            {
                CoacheeId = assignment.CoacheeId,
                CreatedById = user.Id,
                ProtonTrackAssignmentId = trackAssignmentId,
                Status = "Completed",
                CompetencyLevelGranted = competencyLevelGranted,
                KkjMatrixItemId = kkjMatrixItemId,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _context.ProtonFinalAssessments.Add(finalAssessment);

            // Upsert UserCompetencyLevel if kkjMatrixItemId provided
            if (kkjMatrixItemId.HasValue)
            {
                var existingLevel = await _context.UserCompetencyLevels
                    .FirstOrDefaultAsync(l => l.UserId == assignment.CoacheeId
                                           && l.KkjMatrixItemId == kkjMatrixItemId.Value);
                if (existingLevel != null)
                {
                    existingLevel.CurrentLevel = competencyLevelGranted;
                    existingLevel.Source = "Proton";
                    existingLevel.UpdatedAt = DateTime.UtcNow;
                    existingLevel.UpdatedBy = user.Id;
                }
                else
                {
                    _context.UserCompetencyLevels.Add(new UserCompetencyLevel
                    {
                        UserId = assignment.CoacheeId,
                        KkjMatrixItemId = kkjMatrixItemId.Value,
                        CurrentLevel = competencyLevelGranted,
                        TargetLevel = competencyLevelGranted, // Set TargetLevel = granted level (Proton is targeted certification)
                        Source = "Proton",
                        AchievedAt = DateTime.UtcNow,
                        UpdatedBy = user.Id
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Final Proton Assessment berhasil dibuat.";
            return RedirectToAction("HCApprovals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadEvidence(int progressId, IFormFile? evidenceFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Validate file not null/empty
            if (evidenceFile == null || evidenceFile.Length == 0)
            {
                TempData["Error"] = "File tidak boleh kosong.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Hanya PDF, JPG, dan PNG yang diperbolehkan.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Validate file size (max 10MB)
            if (evidenceFile.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "Ukuran file maksimal 10MB.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Load progress record
            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null) return NotFound();

            // Authorization: only coach/supervisor (RoleLevel <= 5) can upload
            if (user.RoleLevel > 5)
            {
                return Forbid();
            }

            // Status check: must be Active or Rejected
            if (progress.Status != "Active" && progress.Status != "Rejected")
            {
                TempData["Error"] = "Deliverable ini tidak dapat diupload.";
                return RedirectToAction("Deliverable", new { id = progressId });
            }

            // Build upload directory
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "evidence", progressId.ToString());
            Directory.CreateDirectory(uploadDir);

            // Sanitize filename: timestamp prefix + original filename (Path.GetFileName prevents path traversal)
            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(evidenceFile.FileName)}";
            var filePath = Path.Combine(uploadDir, safeFileName);

            // Write file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await evidenceFile.CopyToAsync(stream);
            }

            // Update progress record
            bool wasRejected = progress.Status == "Rejected";
            progress.EvidencePath = $"/uploads/evidence/{progressId}/{safeFileName}";
            progress.EvidenceFileName = evidenceFile.FileName;
            progress.Status = "Submitted";
            progress.SubmittedAt = DateTime.UtcNow;
            if (wasRejected)
            {
                progress.RejectedAt = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Evidence berhasil diupload. Menunggu review approver.";
            return RedirectToAction("Deliverable", new { id = progressId });
        }

        public async Task<IActionResult> Progress(string? bagian = null, string? unit = null, string? coacheeId = null)
        {
            // Get current user and their role
            var user = await _userManager.GetUserAsync(User);
            string userRole = "Coachee"; // Default
            int userLevel = 6; // Default: Coachee
            
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRole = roles.FirstOrDefault() ?? "Coachee";
                userLevel = user.RoleLevel;
            }

            // For Level 4 (Section Head, Sr Supervisor): Bagian is auto-filled from user profile
            if (userLevel == 4 && user != null && string.IsNullOrEmpty(bagian))
            {
                bagian = user.Section;
            }

            // Pass user context to view
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.UserSection = user?.Section;
            ViewBag.UserUnit = user?.Unit;
            ViewBag.UserFullName = user?.FullName;
            ViewBag.UserPosition = user?.Position;
            
            ViewBag.SelectedBagian = bagian;
            ViewBag.SelectedUnit = unit;
            ViewBag.SelectedCoacheeId = coacheeId;

            // Mock data: Coachees for Coach role
            if (userRole == "Coach")
            {
                var mockCoachees = new List<ApplicationUser>
                {
                    new ApplicationUser { Id = "coachee1", FullName = "Ahmad Fauzi", Position = "Operator I" },
                    new ApplicationUser { Id = "coachee2", FullName = "Siti Nurhaliza", Position = "Operator II" },
                    new ApplicationUser { Id = "coachee3", FullName = "Bambang Wijaya", Position = "Panelman" }
                };
                ViewBag.Coachees = mockCoachees;
            }

            // ========== VIEW-BASED FILTERING FOR ADMIN ==========
            if (userRole == UserRoles.Admin)
            {
                if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
                {
                    // Force coacheeId to current user
                    coacheeId = user.Id;
                }
                else if (user.SelectedView == "HC")
                {
                    // Leave coacheeId empty (user can select from dropdown)
                    coacheeId = null;
                }
                // For Atasan view, let existing logic work (filter by bagian)
            }

            // For non-admin or admin without specific view, use existing logic

            // Check if HC user has selected a bagian
            bool hasBagianSelected = !string.IsNullOrEmpty(bagian);

            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var targetUserId = coacheeId ?? user?.Id ?? "";
            
            var idpItems = await _context.IdpItems
                .Where(i => i.UserId == targetUserId)
                .OrderBy(i => i.Kompetensi)
                .ThenBy(i => i.SubKompetensi)
                .ToListAsync();
            
            // Map IdpItem to TrackingItem for view compatibility
            var data = idpItems.Select(idp => new TrackingItem
            {
                Id = idp.Id,
                Kompetensi = idp.Kompetensi ?? "",
                SubKompetensi = idp.SubKompetensi ?? "",
                Deliverable = idp.Deliverable ?? "",
                Periode = "", // Not in IdpItem schema, can be added later if needed
                EvidenceStatus = string.IsNullOrEmpty(idp.Evidence) ? "Pending" : "Uploaded",
                ApprovalSrSpv = idp.ApproveSrSpv ?? "Not Started",
                ApprovalSectionHead = idp.ApproveSectionHead ?? "Not Started",
                ApprovalHC = idp.ApproveHC ?? "Not Started",
                SupervisorComments = "" // Not in IdpItem schema
            }).ToList();

            return View(data);
        }


    }
}