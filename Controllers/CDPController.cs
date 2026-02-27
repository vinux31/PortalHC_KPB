using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Models.Competency;
using HcPortal.Data;
using HcPortal.Helpers;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.CanAccessProton = user != null && user.RoleLevel <= 5;
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
            bool isCoacheeView = userRole == UserRoles.Coachee;

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
                    .Where(k => k.ProtonTrackId == assignment.ProtonTrackId)
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

                // Load ProtonTrack for display name
                var protonTrack = await _context.ProtonTracks
                    .FirstOrDefaultAsync(t => t.Id == assignment.ProtonTrackId);

                var protonViewModel = new ProtonPlanViewModel
                {
                    TrackType = protonTrack?.TrackType ?? "",
                    TahunKe = protonTrack?.TahunKe ?? "",
                    KompetensiList = kompetensiList,
                    ActiveProgress = activeProgress,
                    FinalAssessment = finalAssessment
                };

                ViewBag.UserRole = userRole;
                ViewBag.IsProtonView = true;
                return View(protonViewModel);
            }

            // Admin sees default view (no view switching)
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

            // === ANALYTICS: HC/Admin ===
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

            // Load ProtonTrack for display
            var track = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == assignment.ProtonTrackId);
            subModel.TrackType = track?.TrackType ?? "";
            subModel.TahunKe = track?.TahunKe ?? "";

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
                .Include(a => a.ProtonTrack)
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
                    TrackType = assignment?.ProtonTrack?.TrackType ?? "",
                    TahunKe = assignment?.ProtonTrack?.TahunKe ?? "",
                    TotalDeliverables = progresses.Count,
                    Approved = progresses.Count(p => p.Status == "Approved"),
                    Submitted = progresses.Count(p => p.Status == "Submitted"),
                    Rejected = progresses.Count(p => p.Status == "Rejected"),
                    Active = progresses.Count(p => p.Status == "Active"),
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
            var statusLabels = new List<string> { "Approved", "Submitted", "Active", "Rejected" };
            var statusData = new List<int>
            {
                allProgresses.Count(p => p.Status == "Approved"),
                allProgresses.Count(p => p.Status == "Submitted"),
                allProgresses.Count(p => p.Status == "Active"),
                allProgresses.Count(p => p.Status == "Rejected")
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
                .Include(a => a.ProtonTrack)
                .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();

            var activeProgresses = await _context.ProtonDeliverableProgresses
                .Where(p => coacheeIds.Contains(p.CoacheeId) && p.Status == "Active")
                .ToListAsync();

            // Load ProtonTracks for assignment dropdown (Phase 33)
            var protonTracks = await _context.ProtonTracks
                .OrderBy(t => t.Urutan)
                .ToListAsync();
            ViewBag.ProtonTracks = protonTracks;

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
        public async Task<IActionResult> AssignTrack(string coacheeId, int protonTrackId)
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

            if (string.IsNullOrEmpty(coacheeId) || protonTrackId <= 0)
            {
                TempData["Error"] = "Data tidak lengkap.";
                return RedirectToAction("ProtonMain");
            }

            // Validate ProtonTrack exists
            var protonTrack = await _context.ProtonTracks.FindAsync(protonTrackId);
            if (protonTrack == null)
            {
                TempData["Error"] = "Track tidak ditemukan.";
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
                ProtonTrackId = protonTrackId,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            };
            _context.ProtonTrackAssignments.Add(newAssignment);
            await _context.SaveChangesAsync();

            // Query all deliverables for the track
            var deliverables = await _context.ProtonDeliverableList
                .Include(d => d.ProtonSubKompetensi)
                    .ThenInclude(s => s.ProtonKompetensi)
                .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == protonTrackId)
                .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
                    .ThenBy(d => d.ProtonSubKompetensi.Urutan)
                    .ThenBy(d => d.Urutan)
                .ToListAsync();

            // Create progress records — all Pending (Locked/Active removed per Phase 65)
            var progressList = deliverables.Select((d, index) => new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = d.Id,
                Status = "Pending",
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

            // Load progress with full hierarchy including ProtonTrack for display info
            var progress = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                            .ThenInclude(k => k.ProtonTrack)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (progress == null) return NotFound();

            // Access check: coachee themselves OR coach/supervisor (RoleLevel <= 5) OR HC
            bool isCoachee = progress.CoacheeId == user.Id;
            bool isCoach = user.RoleLevel <= 5;
            bool isHC = userRole == UserRoles.HC;

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

            // Get track info from current deliverable's hierarchy
            var kompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi;
            string trackType = kompetensi?.ProtonTrack?.TrackType ?? "";
            string tahunKe = kompetensi?.ProtonTrack?.TahunKe ?? "";

            // All deliverables accessible — no sequential lock
            bool isAccessible = true;

            // Get coachee name
            string coacheeName = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => u.FullName ?? u.UserName ?? u.Id)
                .FirstOrDefaultAsync() ?? progress.CoacheeId;

            // CanUpload: status is Active or Rejected AND current user is coach/supervisor
            bool canUpload = (progress.Status == "Active" || progress.Status == "Rejected") && user.RoleLevel <= 5;

            // Phase 6: approval context
            bool isAtasanAccess = userRole == UserRoles.SrSupervisor ||
                                  userRole == UserRoles.SectionHead;
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
                                  userRole == UserRoles.SectionHead;
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
            int trackId = kompetensi?.ProtonTrackId ?? 0;

            // Set approval fields (in memory, before SaveChangesAsync)
            progress.Status = "Approved";
            progress.ApprovedAt = DateTime.UtcNow;
            progress.ApprovedById = user.Id;

            // Load ALL progress records for this coachee's track (for all-approved check only)
            var allProgresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .Where(p => p.CoacheeId == progress.CoacheeId)
                .ToListAsync();

            var orderedProgresses = allProgresses
                .Where(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.ProtonTrackId == trackId)
                .OrderBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Urutan ?? 0)
                .ThenBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.Urutan ?? 0)
                .ThenBy(p => p.ProtonDeliverable?.Urutan ?? 0)
                .ToList();

            // Phase 65: No more sequential unlock — Locked/Active statuses removed

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
                                  userRole == UserRoles.SectionHead;
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
            bool isHCAccess = userRole == UserRoles.HC;
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

            // HC only can access
            bool isHCAccess = userRole == UserRoles.HC;
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
                .Include(a => a.ProtonTrack)
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
                        TrackType = assignment.ProtonTrack?.TrackType ?? "",
                        TahunKe = assignment.ProtonTrack?.TahunKe ?? ""
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
            bool isHCAccess = userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Load the track assignment with ProtonTrack for display
            var assignment = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
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
            bool isHCAccess = userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Load assignment
            var assignment = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ProtonProgress(
            string? coacheeId = null,
            string? bagian = null,
            string? unit = null,
            string? trackType = null,
            string? tahun = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";
            int userLevel = user.RoleLevel;

            // --- STEP 1: Role-scoped coachee IDs (SERVER ENFORCEMENT) ---
            List<string> scopedCoacheeIds;
            List<ApplicationUser>? coacheeList = null;

            if (userLevel <= 2) // HC/Admin — see all coachees
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.RoleLevel == 6)
                    .Select(u => u.Id).ToListAsync();
            }
            else if (userLevel == 4) // SrSpv/SectionHead — same section only
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6)
                    .Select(u => u.Id).ToListAsync();
            }
            else if (userLevel == 5) // Coach — CoachCoacheeMapping only
            {
                scopedCoacheeIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId).ToListAsync();
            }
            else // Level 6 (Coachee)
            {
                scopedCoacheeIds = new List<string> { user.Id };
            }

            // --- STEP 2: Apply Bagian filter (HC/Admin only) ---
            if (userLevel <= 2 && !string.IsNullOrEmpty(bagian))
            {
                var validSections = OrganizationStructure.GetAllSections();
                if (validSections.Contains(bagian))
                {
                    scopedCoacheeIds = await _context.Users
                        .Where(u => scopedCoacheeIds.Contains(u.Id) && u.Section == bagian)
                        .Select(u => u.Id).ToListAsync();
                }
                else
                {
                    bagian = null; // silently ignore invalid bagian
                }
            }

            // --- STEP 3: Apply Unit filter ---
            if (!string.IsNullOrEmpty(unit))
            {
                if (userLevel <= 2)
                {
                    // HC/Admin: any unit (but must be within selected bagian if set)
                    scopedCoacheeIds = await _context.Users
                        .Where(u => scopedCoacheeIds.Contains(u.Id) && u.Unit == unit)
                        .Select(u => u.Id).ToListAsync();
                }
                else if (userLevel == 4)
                {
                    // SrSpv/SectionHead: validate unit is in their section
                    var allowedUnits = OrganizationStructure.GetUnitsForSection(user.Section ?? "");
                    if (allowedUnits.Contains(unit))
                    {
                        scopedCoacheeIds = await _context.Users
                            .Where(u => scopedCoacheeIds.Contains(u.Id) && u.Unit == unit)
                            .Select(u => u.Id).ToListAsync();
                    }
                    else
                    {
                        unit = null; // silently ignore unauthorized unit
                    }
                }
                // Level 5/6: unit param ignored (implicit scope)
            }

            // --- STEP 4: Apply Track filter to Coach's coachee list (FILT-03 cascade for Coach role) ---
            if (userLevel == 5 && !string.IsNullOrEmpty(trackType))
            {
                var trackFilteredIds = await _context.ProtonTrackAssignments
                    .Where(a => scopedCoacheeIds.Contains(a.CoacheeId) && a.IsActive
                                 && a.ProtonTrack!.TrackType == trackType)
                    .Select(a => a.CoacheeId).Distinct().ToListAsync();
                // Keep only coachees that have the selected track
                scopedCoacheeIds = scopedCoacheeIds.Where(id => trackFilteredIds.Contains(id)).ToList();
            }

            // --- STEP 5: Build coachee dropdown list (after scope narrowing) ---
            coacheeList = await _context.Users
                .Where(u => scopedCoacheeIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // --- STEP 6: Apply single coachee filter ---
            string? targetCoacheeId = null;
            if (!string.IsNullOrEmpty(coacheeId) && scopedCoacheeIds.Contains(coacheeId))
            {
                targetCoacheeId = coacheeId;
            }
            // For Level 6 (Coachee): always own data
            if (userLevel == 6)
            {
                targetCoacheeId = user.Id;
            }

            // Determine which coachee IDs to load data for
            var dataCoacheeIds = !string.IsNullOrEmpty(targetCoacheeId)
                ? new List<string> { targetCoacheeId }
                : scopedCoacheeIds;

            // Load deliverable progress data
            List<TrackingItem> data = new();
            int progressPercent = 0;
            int pendingActions = 0;
            int pendingApprovals = 0;

            // Build base query
            var query = _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                            .ThenInclude(k => k!.ProtonTrack)
                .Where(p => dataCoacheeIds.Contains(p.CoacheeId));

            // Apply Track filter (FILT-03)
            if (!string.IsNullOrEmpty(trackType))
                query = query.Where(p =>
                    p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TrackType == trackType);

            // Apply Tahun filter (FILT-03)
            if (!string.IsNullOrEmpty(tahun))
                query = query.Where(p =>
                    p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TahunKe == tahun);

            var progresses = await query
                .OrderBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.Urutan)
                .ThenBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.Urutan)
                .ThenBy(p => p.ProtonDeliverable!.Urutan)
                .ToListAsync();

            // Build coachee name lookup dictionary
            var coacheeNameDict = coacheeList?.ToDictionary(u => u.Id, u => u.FullName ?? "")
                ?? new Dictionary<string, string>();

            // Phase 65: Build approver name lookup for tooltips
            var approverIds = progresses
                .SelectMany(p => new[] { p.SrSpvApprovedById, p.ShApprovedById, p.HCReviewedById })
                .Where(id => id != null)
                .Distinct()
                .ToList();
            var approverNames = approverIds.Any()
                ? await _context.Users
                    .Where(u => approverIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id)
                : new Dictionary<string, string>();

            // Map ProtonDeliverableProgress to TrackingItem
            data = progresses.Select(p => new TrackingItem
            {
                Id = p.Id,
                Kompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "",
                SubKompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "",
                Deliverable = p.ProtonDeliverable?.NamaDeliverable ?? "",
                EvidenceStatus = p.EvidencePath != null ? "Uploaded" : "Pending",
                FullEvidencePath = p.EvidencePath ?? "",
                ApprovalSrSpv = p.SrSpvApprovalStatus,
                ApprovalSectionHead = p.ShApprovalStatus,
                ApprovalHC = p.HCApprovalStatus == "Reviewed" ? "Reviewed" : "Pending",
                SupervisorComments = p.RejectionReason ?? "",
                CoacheeId = p.CoacheeId,
                CoacheeName = coacheeNameDict.TryGetValue(p.CoacheeId, out var name) ? name : "",
                Status = p.Status,
                SrSpvApproverName = p.SrSpvApprovedById != null && approverNames.ContainsKey(p.SrSpvApprovedById) ? approverNames[p.SrSpvApprovedById] : "",
                SrSpvApprovedAt = p.SrSpvApprovedAt.HasValue ? p.SrSpvApprovedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "",
                ShApproverName = p.ShApprovedById != null && approverNames.ContainsKey(p.ShApprovedById) ? approverNames[p.ShApprovedById] : "",
                ShApprovedAt = p.ShApprovedAt.HasValue ? p.ShApprovedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "",
                HcReviewerName = p.HCReviewedById != null && approverNames.ContainsKey(p.HCReviewedById) ? approverNames[p.HCReviewedById] : "",
                HcReviewedAt = p.HCReviewedAt.HasValue ? p.HCReviewedAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "",
            }).ToList();

            // Compute summary stats
            int total = progresses.Count;
            double weightedSum = progresses.Sum(p =>
                p.Status == "Approved" ? 1.0 :
                p.Status == "Submitted" ? 0.5 : 0.0);
            progressPercent = total > 0 ? (int)(weightedSum / total * 100) : 0;
            pendingActions = progresses.Count(p => p.Status == "Pending" || p.Status == "Rejected");
            // Role-aware pending approvals
            if (userRole == UserRoles.SrSupervisor)
                pendingApprovals = progresses.Count(p => p.Status == "Submitted" && p.SrSpvApprovalStatus == "Pending");
            else if (userRole == UserRoles.SectionHead)
                pendingApprovals = progresses.Count(p => p.Status == "Submitted" && p.ShApprovalStatus == "Pending");
            else if (userRole == UserRoles.HC)
                pendingApprovals = progresses.Count(p => p.Status == "Submitted" && p.HCApprovalStatus == "Pending");
            else
                pendingApprovals = progresses.Count(p => p.Status == "Submitted");

            // --- ViewBag: filter option lists ---
            ViewBag.AllBagian = OrganizationStructure.GetAllSections();
            ViewBag.AllUnits = !string.IsNullOrEmpty(bagian)
                ? OrganizationStructure.GetUnitsForSection(bagian)
                : new List<string>();
            ViewBag.AllTracks = new List<string> { "Panelman", "Operator" };
            ViewBag.AllTahun = new List<string> { "Tahun 1", "Tahun 2", "Tahun 3" };
            ViewBag.Coachees = (userLevel == 6) ? null : coacheeList;

            // --- ViewBag: selected filter values ---
            ViewBag.SelectedBagian = bagian;
            ViewBag.SelectedUnit = unit;
            ViewBag.SelectedTrackType = trackType;
            ViewBag.SelectedTahun = tahun;
            ViewBag.SelectedCoacheeId = targetCoacheeId;

            // --- ViewBag: result counts ---
            var totalBeforeFilter = scopedCoacheeIds.Count > 0
                ? await _context.ProtonDeliverableProgresses
                    .CountAsync(p => scopedCoacheeIds.Contains(p.CoacheeId))
                : 0;
            ViewBag.TotalCount = totalBeforeFilter;
            ViewBag.FilteredCount = data.Count;

            // --- ViewBag: existing values ---
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = userLevel;
            ViewBag.UserSection = user.Section;
            ViewBag.UserUnit = user.Unit;
            ViewBag.UserFullName = user.FullName;
            ViewBag.ProgressPercent = progressPercent;
            ViewBag.PendingActions = pendingActions;
            ViewBag.PendingApprovals = pendingApprovals;

            // Track label: only meaningful for single coachee
            if (!string.IsNullOrEmpty(targetCoacheeId))
            {
                var assignment = await _context.ProtonTrackAssignments
                    .Include(a => a.ProtonTrack)
                    .FirstOrDefaultAsync(a => a.CoacheeId == targetCoacheeId && a.IsActive);
                ViewBag.TrackLabel = assignment?.ProtonTrack != null
                    ? $"{assignment.ProtonTrack.TrackType} {assignment.ProtonTrack.TahunKe}"
                    : "";
                var coacheeUser = await _context.Users.FindAsync(targetCoacheeId);
                ViewBag.CoacheeName = coacheeUser?.FullName ?? "";
            }
            else
            {
                ViewBag.TrackLabel = "";
                ViewBag.CoacheeName = "";
            }

            // Empty result message
            if (data.Count == 0)
            {
                ViewBag.EmptyMessage = "Tidak ada data yang sesuai filter";
            }

            return View(data);
        }

        public IActionResult Progress() => RedirectToAction("Index");

        // ===== Phase 65: AJAX approval endpoints =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveFromProgress(int progressId, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Tidak terautentikasi." });

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";

            bool isSrSpv = userRole == UserRoles.SrSupervisor;
            bool isSH = userRole == UserRoles.SectionHead;
            if (!isSrSpv && !isSH)
                return Json(new { success = false, message = "Akses tidak diizinkan." });

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            if (progress.Status != "Submitted")
                return Json(new { success = false, message = "Hanya deliverable dengan status Submitted yang dapat disetujui." });

            // Section check
            var coacheeUser = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.Section })
                .FirstOrDefaultAsync();
            if (coacheeUser == null || coacheeUser.Section != user.Section)
                return Json(new { success = false, message = "Tidak dapat menyetujui deliverable dari seksi berbeda." });

            var now = DateTime.UtcNow;
            if (isSrSpv)
            {
                progress.SrSpvApprovalStatus = "Approved";
                progress.SrSpvApprovedById = user.Id;
                progress.SrSpvApprovedAt = now;
            }
            else
            {
                progress.ShApprovalStatus = "Approved";
                progress.ShApprovedById = user.Id;
                progress.ShApprovedAt = now;
            }

            // Set overall status to Approved
            progress.Status = "Approved";
            progress.ApprovedAt = now;
            progress.ApprovedById = user.Id;

            await _context.SaveChangesAsync();

            var approverName = user.FullName ?? user.UserName ?? user.Id;
            var approvedAtLocal = now.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            return Json(new
            {
                success = true,
                message = "Deliverable berhasil disetujui.",
                newStatus = isSrSpv ? progress.SrSpvApprovalStatus : progress.ShApprovalStatus,
                approverName,
                approvedAt = approvedAtLocal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectFromProgress(int progressId, string rejectionReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Tidak terautentikasi." });

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";

            bool isSrSpv = userRole == UserRoles.SrSupervisor;
            bool isSH = userRole == UserRoles.SectionHead;
            if (!isSrSpv && !isSH)
                return Json(new { success = false, message = "Akses tidak diizinkan." });

            if (string.IsNullOrWhiteSpace(rejectionReason))
                return Json(new { success = false, message = "Alasan penolakan tidak boleh kosong." });

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            if (progress.Status != "Submitted")
                return Json(new { success = false, message = "Hanya deliverable dengan status Submitted yang dapat ditolak." });

            // Section check
            var coacheeUser = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.Section })
                .FirstOrDefaultAsync();
            if (coacheeUser == null || coacheeUser.Section != user.Section)
                return Json(new { success = false, message = "Tidak dapat menolak deliverable dari seksi berbeda." });

            var now = DateTime.UtcNow;
            if (isSrSpv)
            {
                progress.SrSpvApprovalStatus = "Rejected";
                progress.SrSpvApprovedById = user.Id;
                progress.SrSpvApprovedAt = now;
            }
            else
            {
                progress.ShApprovalStatus = "Rejected";
                progress.ShApprovedById = user.Id;
                progress.ShApprovedAt = now;
            }

            // Rejection takes precedence — overall status becomes Rejected
            progress.Status = "Rejected";
            progress.RejectionReason = rejectionReason;
            progress.RejectedAt = now;

            await _context.SaveChangesAsync();

            var approverName = user.FullName ?? user.UserName ?? user.Id;
            var approvedAtLocal = now.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            return Json(new
            {
                success = true,
                message = "Deliverable ditolak.",
                newStatus = isSrSpv ? progress.SrSpvApprovalStatus : progress.ShApprovalStatus,
                approverName,
                approvedAt = approvedAtLocal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HCReviewFromProgress(int progressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Tidak terautentikasi." });

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";

            bool isHC = userRole == UserRoles.HC || userRole == UserRoles.Admin;
            if (!isHC)
                return Json(new { success = false, message = "Akses tidak diizinkan." });

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            var now = DateTime.UtcNow;
            progress.HCApprovalStatus = "Reviewed";
            progress.HCReviewedAt = now;
            progress.HCReviewedById = user.Id;

            await _context.SaveChangesAsync();

            var reviewerName = user.FullName ?? user.UserName ?? user.Id;
            var reviewedAtLocal = now.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            return Json(new
            {
                success = true,
                message = "Deliverable telah direview.",
                reviewerName,
                reviewedAt = reviewedAtLocal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEvidenceWithCoaching(
            [FromForm] string progressIdsJson,
            [FromForm] DateTime date,
            [FromForm] string koacheeCompetencies,
            [FromForm] string catatanCoach,
            [FromForm] string kesimpulan,
            [FromForm] string result,
            IFormFile? evidenceFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Tidak terautentikasi." });

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";
            bool isCoach = user.RoleLevel == 5;
            if (!isCoach)
                return Json(new { success = false, message = "Akses tidak diizinkan. Hanya Coach yang dapat submit evidence." });

            // Parse progress IDs
            List<int> progressIds;
            try
            {
                progressIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(progressIdsJson) ?? new List<int>();
            }
            catch
            {
                return Json(new { success = false, message = "Format data tidak valid." });
            }

            if (!progressIds.Any())
                return Json(new { success = false, message = "Tidak ada deliverable yang dipilih." });

            // Load progress records with full include chain
            var progresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                .Where(p => progressIds.Contains(p.Id))
                .ToListAsync();

            if (!progresses.Any())
                return Json(new { success = false, message = "Data tidak ditemukan." });

            // Validate all belong to coach's coachees
            var coacheeIds = progresses.Select(p => p.CoacheeId).Distinct().ToList();
            var validCoacheeIds = await _context.CoachCoacheeMappings
                .Where(m => m.CoachId == user.Id && coacheeIds.Contains(m.CoacheeId) && m.IsActive)
                .Select(m => m.CoacheeId)
                .ToListAsync();
            if (progresses.Any(p => !validCoacheeIds.Contains(p.CoacheeId)))
                return Json(new { success = false, message = "Akses tidak diizinkan untuk beberapa deliverable." });

            // Validate all statuses are Pending or Rejected
            if (progresses.Any(p => p.Status != "Pending" && p.Status != "Rejected"))
                return Json(new { success = false, message = "Hanya deliverable berstatus Pending atau Rejected yang dapat disubmit." });

            // Handle optional file upload
            string? evidencePath = null;
            string? evidenceFileName = null;
            if (evidenceFile != null && evidenceFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                    return Json(new { success = false, message = "Format file tidak valid. Gunakan PDF, JPG, atau PNG." });
                if (evidenceFile.Length > 10 * 1024 * 1024)
                    return Json(new { success = false, message = "Ukuran file melebihi 10MB." });

                int firstProgressId = progressIds.First();
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var safeFileName = $"{timestamp}_{Path.GetFileName(evidenceFile.FileName)}";
                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "evidence", firstProgressId.ToString());
                Directory.CreateDirectory(uploadFolder);
                var filePath = Path.Combine(uploadFolder, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await evidenceFile.CopyToAsync(stream);
                }
                evidencePath = $"/uploads/evidence/{firstProgressId}/{safeFileName}";
                evidenceFileName = evidenceFile.FileName;
            }

            // Process each progress record
            var now = DateTime.UtcNow;
            var submittedIds = new List<int>();
            foreach (var progress in progresses)
            {
                progress.Status = "Submitted";
                progress.SubmittedAt = now;
                // Reset approval columns for fresh review cycle
                progress.SrSpvApprovalStatus = "Pending";
                progress.SrSpvApprovedById = null;
                progress.SrSpvApprovedAt = null;
                progress.ShApprovalStatus = "Pending";
                progress.ShApprovedById = null;
                progress.ShApprovedAt = null;

                // Apply file upload if provided; otherwise keep existing EvidencePath
                if (evidencePath != null)
                {
                    progress.EvidencePath = evidencePath;
                    progress.EvidenceFileName = evidenceFileName;
                }

                // Create CoachingSession record
                var session = new CoachingSession
                {
                    CoachId = user.Id,
                    CoacheeId = progress.CoacheeId,
                    Date = date,
                    Kompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "",
                    SubKompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "",
                    Deliverable = progress.ProtonDeliverable?.NamaDeliverable ?? "",
                    CoacheeCompetencies = koacheeCompetencies,
                    CatatanCoach = catatanCoach,
                    Kesimpulan = kesimpulan,
                    Result = result,
                    Status = "Submitted",
                    ProtonDeliverableProgressId = progress.Id,
                    CreatedAt = now
                };
                _context.CoachingSessions.Add(session);
                submittedIds.Add(progress.Id);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{submittedIds.Count} deliverable berhasil disubmit",
                submittedIds,
                hasEvidence = evidencePath != null
            });
        }

        // ===== Phase 65-03: Export endpoints =====

        [HttpGet]
        [Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]
        public async Task<IActionResult> ExportProgressExcel(string coacheeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Load coachee user for filename
            var coacheeUser = await _context.Users.FindAsync(coacheeId);
            if (coacheeUser == null) return NotFound();

            // Load deliverable progress for this specific coachee
            var progresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                .Where(p => p.CoacheeId == coacheeId)
                .OrderBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.Urutan)
                    .ThenBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.Urutan)
                    .ThenBy(p => p.ProtonDeliverable!.Urutan)
                .ToListAsync();

            // Load latest coaching session per deliverable progress
            var progressIds = progresses.Select(p => p.Id).ToList();
            var coachingSessions = await _context.CoachingSessions
                .Where(cs => cs.CoacheeId == coacheeId && cs.ProtonDeliverableProgressId != null
                              && progressIds.Contains(cs.ProtonDeliverableProgressId!.Value))
                .GroupBy(cs => cs.ProtonDeliverableProgressId)
                .ToDictionaryAsync(g => g.Key!.Value, g => g.OrderByDescending(cs => cs.CreatedAt).First());

            // Build Excel using ClosedXML
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Proton Progress");

            // Header row (10 columns)
            ws.Cell(1, 1).Value = "Kompetensi";
            ws.Cell(1, 2).Value = "Sub Kompetensi";
            ws.Cell(1, 3).Value = "Deliverable";
            ws.Cell(1, 4).Value = "Evidence";
            ws.Cell(1, 5).Value = "Approval SrSpv";
            ws.Cell(1, 6).Value = "Approval SH";
            ws.Cell(1, 7).Value = "Approval HC";
            ws.Cell(1, 8).Value = "Catatan Coach";
            ws.Cell(1, 9).Value = "Kesimpulan";
            ws.Cell(1, 10).Value = "Result";

            // Style header: bold, light blue background
            var headerRange = ws.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data rows
            for (int i = 0; i < progresses.Count; i++)
            {
                var p = progresses[i];
                var row = i + 2;
                var cs = coachingSessions.ContainsKey(p.Id) ? coachingSessions[p.Id] : null;

                ws.Cell(row, 1).Value = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "";
                ws.Cell(row, 2).Value = p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "";
                ws.Cell(row, 3).Value = p.ProtonDeliverable?.NamaDeliverable ?? "";
                ws.Cell(row, 4).Value = p.EvidencePath != null ? "Sudah Upload" : "Belum Upload";
                ws.Cell(row, 5).Value = p.SrSpvApprovalStatus;
                ws.Cell(row, 6).Value = p.ShApprovalStatus;
                ws.Cell(row, 7).Value = p.HCApprovalStatus;
                ws.Cell(row, 8).Value = cs?.CatatanCoach ?? "";
                ws.Cell(row, 9).Value = cs?.Kesimpulan ?? "";
                ws.Cell(row, 10).Value = cs?.Result ?? "";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var safeName = (coacheeUser.FullName ?? "Coachee").Replace(" ", "_");
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{safeName}_Progress_{DateTime.Now:yyyy-MM-dd}.xlsx");
        }

        [HttpGet]
        [Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]
        public async Task<IActionResult> ExportProgressPdf(string coacheeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Load coachee user for filename/header
            var coacheeUser = await _context.Users.FindAsync(coacheeId);
            if (coacheeUser == null) return NotFound();

            // Load deliverable progress for this specific coachee
            var progresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                .Where(p => p.CoacheeId == coacheeId)
                .OrderBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.Urutan)
                    .ThenBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.Urutan)
                    .ThenBy(p => p.ProtonDeliverable!.Urutan)
                .ToListAsync();

            // Load latest coaching session per deliverable progress
            var progressIds = progresses.Select(p => p.Id).ToList();
            var coachingSessions = await _context.CoachingSessions
                .Where(cs => cs.CoacheeId == coacheeId && cs.ProtonDeliverableProgressId != null
                              && progressIds.Contains(cs.ProtonDeliverableProgressId!.Value))
                .GroupBy(cs => cs.ProtonDeliverableProgressId)
                .ToDictionaryAsync(g => g.Key!.Value, g => g.OrderByDescending(cs => cs.CreatedAt).First());

            var coacheeName = coacheeUser.FullName ?? "Coachee";

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Proton Progress — {coacheeName}").FontSize(14).Bold();
                        col.Item().Text($"Export date: {DateTime.Now:dd MMM yyyy}").FontSize(9);
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);   // Kompetensi
                                cols.RelativeColumn(2);   // Sub Kompetensi
                                cols.RelativeColumn(2);   // Deliverable
                                cols.RelativeColumn(1);   // Evidence
                                cols.RelativeColumn(1);   // SrSpv
                                cols.RelativeColumn(1);   // SH
                                cols.RelativeColumn(1);   // HC
                                cols.RelativeColumn(2);   // Catatan
                                cols.RelativeColumn(1.5f);// Kesimpulan
                                cols.RelativeColumn(1);   // Result
                            });

                            // Header row
                            foreach (var header in new[] { "Kompetensi", "Sub Kompetensi", "Deliverable", "Evidence", "Approval SrSpv", "Approval SH", "Approval HC", "Catatan Coach", "Kesimpulan", "Result" })
                            {
                                table.Cell().Background(QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(3).Text(header).FontSize(8).Bold();
                            }

                            // Data rows
                            foreach (var p in progresses)
                            {
                                var cs = coachingSessions.ContainsKey(p.Id) ? coachingSessions[p.Id] : null;
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "").FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "").FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.ProtonDeliverable?.NamaDeliverable ?? "").FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.EvidencePath != null ? "Sudah Upload" : "Belum Upload").FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.SrSpvApprovalStatus).FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.ShApprovalStatus).FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(p.HCApprovalStatus).FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(cs?.CatatanCoach ?? "").FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(cs?.Kesimpulan ?? "").FontSize(7);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(3).Text(cs?.Result ?? "").FontSize(7);
                            }
                        });
                    });
                });
            });

            var pdfStream = new MemoryStream();
            pdf.GeneratePdf(pdfStream);
            var safeName = coacheeName.Replace(" ", "_");
            return File(pdfStream.ToArray(), "application/pdf",
                $"{safeName}_Progress_{DateTime.Now:yyyy-MM-dd}.pdf");
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> GetCoacheeDeliverables(string coacheeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var roles = await _userManager.GetRolesAsync(user);
            int userLevel = user.RoleLevel;

            // Access control validation — prevents URL manipulation
            if (userLevel == 6) // Coachee
            {
                // Silently redirect to own data
                if (coacheeId != user.Id) coacheeId = user.Id;
            }
            else if (userLevel == 5) // Coach
            {
                var hasAccess = await _context.CoachCoacheeMappings
                    .AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == coacheeId && m.IsActive);
                if (!hasAccess) return Json(new { error = "unauthorized", data = (object?)null });
            }
            else if (userLevel == 4) // SrSpv / SectionHead
            {
                var coacheeUser = await _context.Users.FindAsync(coacheeId);
                if (coacheeUser == null || coacheeUser.Section != user.Section)
                    return Json(new { error = "unauthorized", data = (object?)null });
            }
            // Level 1-2 (HC/Admin): allow all

            // Load deliverable progress
            var progresses = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                .Where(p => p.CoacheeId == coacheeId)
                .OrderBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.Urutan)
                    .ThenBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.Urutan)
                    .ThenBy(p => p.ProtonDeliverable!.Urutan)
                .ToListAsync();

            // Map to anonymous JSON objects (camelCase for JavaScript)
            var items = progresses.Select(p => new
            {
                id = p.Id,
                kompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "",
                subKompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "",
                deliverable = p.ProtonDeliverable?.NamaDeliverable ?? "",
                evidenceStatus = p.EvidencePath != null ? "Uploaded" : "Pending",
                approvalSrSpv = p.Status == "Approved" ? "Approved" : p.Status == "Rejected" ? "Rejected" : p.Status == "Submitted" ? "Pending" : "Not Started",
                approvalSectionHead = p.Status == "Approved" ? "Approved" : p.Status == "Rejected" ? "Rejected" : p.Status == "Submitted" ? "Pending" : "Not Started",
                approvalHC = p.HCApprovalStatus == "Reviewed" ? "Approved" : "Pending",
                supervisorComments = p.RejectionReason ?? "",
                deliverableId = p.ProtonDeliverableId,
            }).ToList();

            // Compute summary stats
            int total = progresses.Count;
            double weightedSum = progresses.Sum(p =>
                p.Status == "Approved" ? 1.0 :
                p.Status == "Submitted" ? 0.5 : 0.0);
            int progressPercent = total > 0 ? (int)(weightedSum / total * 100) : 0;
            int pendingActions = progresses.Count(p => p.Status == "Active" || p.Status == "Rejected");
            int pendingApprovals = progresses.Count(p => p.Status == "Submitted");

            // Get track label and coachee name
            var assignment = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId && a.IsActive);
            string trackLabel = assignment?.ProtonTrack != null
                ? $"{assignment.ProtonTrack.TrackType} Tahun {assignment.ProtonTrack.TahunKe}"
                : "";

            var coacheeDbUser = await _context.Users.FindAsync(coacheeId);
            string coacheeName = coacheeDbUser?.FullName ?? "";

            return Json(new
            {
                items,
                stats = new { progressPercent, pendingActions, pendingApprovals },
                trackLabel,
                coacheeName,
                noTrack = string.IsNullOrEmpty(trackLabel) && total == 0,
                noProgress = !string.IsNullOrEmpty(trackLabel) && total == 0,
            });
        }


    }
}