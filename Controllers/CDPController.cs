using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Models.Competency;
using HcPortal.Data;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;

namespace HcPortal.Controllers
{
    // ====================================================================
    // PHASE 87-02 DASHBOARD QA FIXES
    // ====================================================================
    // Bugs fixed during dashboard data accuracy verification:
    //
    // 1. [BUG] Coachee Dashboard ActiveDeliverables (line ~266)
    //    - Was checking Status == "Active" which doesn't exist in ProtonDeliverableProgress
    //    - Fixed to check Status == "Pending" (valid status for in-progress deliverables)
    //    - Impact: Coachee dashboard now shows correct count of active/in-progress deliverables
    //
    // 2. [BUG] Proton Progress missing IsActive filters (lines ~292-323)
    //    - BuildProtonProgressSubModelAsync was querying all RoleLevel==6 users without IsActive check
    //    - Fixed: Added u.IsActive filter to all 4 role branches (HC/Admin, SrSpv, SectionHead, Coach)
    //    - Impact: Dashboard now excludes inactive coachees from all stats, rows, and charts
    // ====================================================================

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

        public async Task<IActionResult> PlanIdp(string? bagian = null, string? unit = null, int? trackId = null)
        {
            // Get current user + role
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";
            bool isCoachee = userRole == UserRoles.Coachee;

            // Coachee: lock to their assigned Bagian (cannot browse other sections)
            string? coacheeBagian = null;
            if (isCoachee)
            {
                var assignment = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == user.Id && a.IsActive)
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    var firstKomp = await _context.ProtonKompetensiList
                        .Where(k => k.ProtonTrackId == assignment.ProtonTrackId && k.IsActive)
                        .FirstOrDefaultAsync();
                    if (firstKomp != null)
                    {
                        coacheeBagian = firstKomp.Bagian;
                        // Force bagian to coachee's own — ignore URL param
                        bagian = coacheeBagian;
                        unit ??= firstKomp.Unit;
                        trackId ??= assignment.ProtonTrackId;
                    }
                    ViewBag.HasAssignment = true;
                    ViewBag.AssignedTrackId = assignment.ProtonTrackId;
                }
                else
                {
                    ViewBag.HasAssignment = false;
                }
            }
            else
            {
                // Non-coachee roles: HasAssignment is not relevant (suppress "no assignment" message)
                ViewBag.HasAssignment = true;
                ViewBag.AssignedTrackId = (object?)null;
            }

            // L4 (SectionHead/SrSpv): lock Bagian to their section
            bool isL4 = !isCoachee && user.RoleLevel == 4;
            if (isL4 && !string.IsNullOrEmpty(user.Section))
            {
                bagian = user.Section; // Force bagian to user's section
            }

            ViewBag.CoacheeBagian = coacheeBagian ?? "";

            // Load all tracks for dropdowns
            var allTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();

            // Build silabus rows as JSON (only if all 3 filters are set)
            var silabusRows = new List<object>();
            if (!string.IsNullOrEmpty(bagian) && !string.IsNullOrEmpty(unit) && trackId.HasValue)
            {
                var kompetensiList = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value && k.IsActive)
                    .OrderBy(k => k.Urutan)
                    .ToListAsync();

                foreach (var k in kompetensiList)
                {
                    foreach (var s in k.SubKompetensiList.OrderBy(s => s.Urutan))
                    {
                        foreach (var d in s.Deliverables.OrderBy(d => d.Urutan))
                        {
                            silabusRows.Add(new {
                                KompetensiId = k.Id,
                                Kompetensi = k.NamaKompetensi,
                                SubKompetensiId = s.Id,
                                SubKompetensi = s.NamaSubKompetensi,
                                DeliverableId = d.Id,
                                Deliverable = d.NamaDeliverable,
                                No = d.Urutan.ToString(),
                                Target = s.Target ?? ""
                            });
                        }
                    }
                }
            }

            // Build coaching guidance grouped hierarchy (Bagian > Unit > TrackType > TahunKe)
            // Coachee: limited to their own Bagian only
            var guidanceQuery = _context.CoachingGuidanceFiles
                .Include(f => f.ProtonTrack)
                .AsQueryable();
            if (isCoachee && coacheeBagian != null)
                guidanceQuery = guidanceQuery.Where(f => f.Bagian == coacheeBagian);
            else if (isL4 && !string.IsNullOrEmpty(user.Section))
                guidanceQuery = guidanceQuery.Where(f => f.Bagian == user.Section);

            var allGuidanceFiles = await guidanceQuery
                .OrderBy(f => f.Bagian)
                    .ThenBy(f => f.Unit)
                    .ThenBy(f => f.ProtonTrack!.Urutan)
                    .ThenBy(f => f.FileName)
                .ToListAsync();

            var guidanceGrouped = allGuidanceFiles
                .GroupBy(f => f.Bagian)
                .OrderBy(g => g.Key)
                .Select(bagianGroup => new {
                    Bagian = bagianGroup.Key,
                    Units = bagianGroup.GroupBy(f => f.Unit)
                        .OrderBy(g => g.Key)
                        .Select(unitGroup => new {
                            Unit = unitGroup.Key,
                            TrackTypes = unitGroup.GroupBy(f => f.ProtonTrack!.TrackType)
                                .OrderBy(g => g.Key)
                                .Select(ttGroup => new {
                                    TrackType = ttGroup.Key,
                                    TahunKeList = ttGroup.GroupBy(f => f.ProtonTrack!.TahunKe)
                                        .OrderBy(g => g.Key)
                                        .Select(tkGroup => new {
                                            TahunKe = tkGroup.Key,
                                            Files = tkGroup.Select(f => new {
                                                f.Id,
                                                f.FileName,
                                                f.FileSize,
                                                UploadedAt = f.UploadedAt.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"))
                                            }).ToList()
                                        }).ToList()
                                }).ToList()
                        }).ToList()
                }).ToList();

            // Set ViewBag values
            ViewBag.UserRole = userRole;
            ViewBag.UserLevel = user.RoleLevel;
            ViewBag.LockedSection = isL4 ? user.Section : null;
            ViewBag.IsCoachee = isCoachee;
            ViewBag.AllTracks = allTracks;
            ViewBag.Bagian = bagian ?? "";
            ViewBag.Unit = unit ?? "";
            ViewBag.TrackId = trackId;
            ViewBag.HasFilter = !string.IsNullOrEmpty(bagian) && !string.IsNullOrEmpty(unit) && trackId.HasValue;
            ViewBag.SilabusRowsJson = System.Text.Json.JsonSerializer.Serialize(silabusRows,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
            ViewBag.GuidanceGroupedJson = System.Text.Json.JsonSerializer.Serialize(guidanceGrouped,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
            ViewBag.OrgStructureJson = System.Text.Json.JsonSerializer.Serialize(
                HcPortal.Models.OrganizationStructure.SectionUnits);

            return View();
        }

        // GET: /CDP/GuidanceDownload?id=X
        // Open to any authenticated user (coachees, coaches, HC, Admin, etc.)
        public async Task<IActionResult> GuidanceDownload(int id)
        {
            var record = await _context.CoachingGuidanceFiles.FindAsync(id);
            if (record == null) return NotFound();

            var physicalPath = Path.Combine(_env.WebRootPath, record.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            // Validate path stays within wwwroot to prevent path traversal
            var fullPath = Path.GetFullPath(physicalPath);
            if (!fullPath.StartsWith(_env.WebRootPath, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file path.");
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var contentType = record.FilePath.ToLowerInvariant() switch
            {
                var p when p.EndsWith(".pdf") => "application/pdf",
                var p when p.EndsWith(".doc") => "application/msword",
                var p when p.EndsWith(".docx") => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                var p when p.EndsWith(".xls") => "application/vnd.ms-excel",
                var p when p.EndsWith(".xlsx") => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                var p when p.EndsWith(".ppt") => "application/vnd.ms-powerpoint",
                var p when p.EndsWith(".pptx") => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };
            return PhysicalFile(fullPath, contentType, record.FileName);
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
            if (track == null)
                return subModel; // Track deleted — return defaults rather than proceeding with incomplete data
            subModel.TrackType = track.TrackType ?? "";
            subModel.TahunKe = track.TahunKe ?? "";

            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.CoacheeId == userId)
                .ToListAsync();

            subModel.TotalDeliverables = progresses.Count;
            subModel.ApprovedDeliverables = progresses.Count(p => p.Status == "Approved");
            // FIX: ProtonDeliverableProgress has no "Active" status; use "Pending" for in-progress deliverables
            subModel.ActiveDeliverables = progresses.Count(p => p.Status == "Pending");

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
                    .Where(u => u.RoleLevel == 6 && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();
                scopeLabel = "All Sections";
            }
            else if (UserRoles.HasSectionAccess(UserRoles.GetRoleLevel(userRole)))
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6 && u.IsActive)
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
                        .Where(u => u.Unit == user.Unit && u.RoleLevel == 6 && u.IsActive)
                        .Select(u => u.Id)
                        .ToListAsync();
                    scopeLabel = $"Unit: {user.Unit}";
                }
                else
                {
                    scopedCoacheeIds = await _context.Users
                        .Where(u => u.Section == user.Section && u.RoleLevel == 6 && u.IsActive)
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
                    Active = progresses.Count(p => p.Status == "Pending"),
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
            var statusLabels = new List<string> { "Approved", "Submitted", "Pending", "Rejected" };
            var statusData = new List<int>
            {
                allProgresses.Count(p => p.Status == "Approved"),
                allProgresses.Count(p => p.Status == "Submitted"),
                allProgresses.Count(p => p.Status == "Pending"),
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

            // Access check: coachee themselves OR coach/supervisor OR management OR HC OR Admin
            bool isCoachee = progress.CoacheeId == user.Id;
            bool hasFullAccess = UserRoles.HasFullAccess(user.RoleLevel); // Level 1-3: Admin, HC, Direktur, VP, Manager
            bool isSectionScoped = UserRoles.HasSectionAccess(user.RoleLevel); // Level 4: SectionHead, SrSupervisor
            bool isCoach = user.RoleLevel == 5; // Level 5 only: Coach, Supervisor

            if (isCoachee || hasFullAccess)
            {
                // Own data or full-access role — allow
            }
            else if (isSectionScoped)
            {
                // SrSupervisor: section check
                var coachee = await _context.Users
                    .Where(u => u.Id == progress.CoacheeId)
                    .Select(u => new { u.Section })
                    .FirstOrDefaultAsync();
                if (coachee == null || coachee.Section != user.Section)
                    return Forbid();
            }
            else if (isCoach)
            {
                // Coach: section check (coach-coachee are always same section)
                var coachee = await _context.Users
                    .Where(u => u.Id == progress.CoacheeId)
                    .Select(u => new { u.Section })
                    .FirstOrDefaultAsync();
                if (coachee == null || coachee.Section != user.Section)
                    return Forbid();
            }
            else
            {
                // Coachee accessing other coachee, or unknown role — deny
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

            // Phase 74: upload restricted to Coach role only (not level — SrSupervisor must not upload)
            bool canUpload = (progress.Status == "Pending" || progress.Status == "Rejected") && userRole == UserRoles.Coach;

            // Phase 6: approval context
            bool isHC = userRole == UserRoles.HC;
            int roleLevel = UserRoles.GetRoleLevel(userRole ?? "");
            bool isAtasanAccess = UserRoles.HasSectionAccess(roleLevel);
            bool isSrSpv = userRole == UserRoles.SrSupervisor;
            bool isSH = userRole == UserRoles.SectionHead;
            // Co-sign: allow approve when own approval is still Pending, even if overall Status is already Approved
            bool canApprove = isAtasanAccess && (
                progress.Status == "Submitted" ||
                (progress.Status == "Approved" && (
                    (isSrSpv && progress.SrSpvApprovalStatus != "Approved") ||
                    (isSH && progress.ShApprovalStatus != "Approved"))));
            bool canHCReview = isHC && progress.HCApprovalStatus == "Pending";

            // Phase 65-03: Load coaching sessions linked to this deliverable progress
            var coachingSessions = await _context.CoachingSessions
                .Where(cs => cs.ProtonDeliverableProgressId == id)
                .OrderByDescending(cs => cs.CreatedAt)
                .ToListAsync();
            ViewBag.CoachingSessions = coachingSessions;

            if (coachingSessions.Any())
            {
                var coachIds = coachingSessions.Select(cs => cs.CoachId).Distinct().ToList();
                var coachNames = await _context.Users
                    .Where(u => coachIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);
                ViewBag.CoachNames = coachNames;
            }
            else
            {
                ViewBag.CoachNames = new Dictionary<string, string>();
            }

            // Build approver name lookup for timeline
            var approverIds = new[] { progress.SrSpvApprovedById, progress.ShApprovedById, progress.HCReviewedById, progress.ApprovedById }
                .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            if (approverIds.Any())
            {
                ViewBag.ApproverNames = await _context.Users
                    .Where(u => approverIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);
            }
            else
            {
                ViewBag.ApproverNames = new Dictionary<string, string>();
            }

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

            // Level-based access: only L4 (SrSupervisor/SectionHead) can approve
            int roleLevel = UserRoles.GetRoleLevel(userRole ?? "");
            if (!UserRoles.HasSectionAccess(roleLevel)) return Forbid();

            bool isSrSpv = userRole == UserRoles.SrSupervisor;
            bool isSH = userRole == UserRoles.SectionHead;

            // Load progress with full Include chain
            var progress = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return NotFound();

            // Guard: allow Submitted, or co-sign when already Approved but own approval still Pending
            bool canApprove = progress.Status == "Submitted" ||
                (progress.Status == "Approved" && (
                    (isSrSpv && progress.SrSpvApprovalStatus != "Approved") ||
                    (isSH && progress.ShApprovalStatus != "Approved")));
            if (!canApprove)
            {
                TempData["Error"] = "Deliverable ini tidak dapat disetujui saat ini.";
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

            // Set per-role approval fields
            if (userRole == UserRoles.SrSupervisor)
            {
                progress.SrSpvApprovalStatus = "Approved";
                progress.SrSpvApprovedById = user.Id;
                progress.SrSpvApprovedAt = DateTime.UtcNow;
            }
            else if (userRole == UserRoles.SectionHead)
            {
                progress.ShApprovalStatus = "Approved";
                progress.ShApprovedById = user.Id;
                progress.ShApprovedAt = DateTime.UtcNow;
            }

            // Set overall approval fields (in memory, before SaveChangesAsync)
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

            // Phase 117: Record status history
            string approveStatusType = isSrSpv ? "SrSpv Approved" : "SH Approved";
            string approveActorRole = isSrSpv ? "Sr. Supervisor" : "Section Head";
            RecordStatusHistory(progress.Id, approveStatusType, user.Id, user.FullName, approveActorRole);

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

            // Level-based access: only L4 (SrSupervisor/SectionHead) can reject
            int roleLevel = UserRoles.GetRoleLevel(userRole ?? "");
            if (!UserRoles.HasSectionAccess(roleLevel)) return Forbid();

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

            // Guard: Submitted or Approved (co-sign scenario) can be rejected
            if (progress.Status != "Submitted" && progress.Status != "Approved")
            {
                TempData["Error"] = "Deliverable ini tidak dapat ditolak saat ini.";
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

            // Reset all approval chain fields
            progress.SrSpvApprovalStatus = "Pending";
            progress.SrSpvApprovedById = null;
            progress.SrSpvApprovedAt = null;
            progress.ShApprovalStatus = "Pending";
            progress.ShApprovedById = null;
            progress.ShApprovedAt = null;
            progress.HCApprovalStatus = "Pending";
            progress.HCReviewedById = null;
            progress.HCReviewedAt = null;

            // Phase 117: Record rejection history
            bool isSrSpvReject = userRole == UserRoles.SrSupervisor;
            string rejectStatusType = isSrSpvReject ? "SrSpv Rejected" : "SH Rejected";
            string rejectActorRole = isSrSpvReject ? "Sr. Supervisor" : "Section Head";
            RecordStatusHistory(progress.Id, rejectStatusType, user.Id, user.FullName, rejectActorRole, rejectionReason);

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
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Concurrent request already created the notification — safe to ignore
            }
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
            bool isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin;
            if (!isHCAccess) return Forbid();

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null) return NotFound();

            // Guard: must be Pending
            if (progress.HCApprovalStatus != "Pending")
            {
                TempData["Error"] = "Deliverable ini sudah diperiksa HC sebelumnya.";
                return RedirectToAction("CoachingProton");
            }

            progress.HCApprovalStatus = "Reviewed";
            progress.HCReviewedAt = DateTime.UtcNow;
            progress.HCReviewedById = user.Id;

            // Phase 117: Record HC review history
            RecordStatusHistory(progress.Id, "HC Reviewed", user.Id, user.FullName, "HC");

            await _context.SaveChangesAsync();

            TempData["Success"] = "Deliverable telah ditandai sebagai sudah diperiksa HC.";
            return RedirectToAction("Deliverable", new { id = progressId });
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

            // Phase 74: upload restricted to Coach role only
            var uploadRoles = await _userManager.GetRolesAsync(user);
            var uploadUserRole = uploadRoles.FirstOrDefault();
            if (uploadUserRole != UserRoles.Coach)
            {
                return Forbid();
            }

            // Status check: must be Pending or Rejected
            if (progress.Status != "Pending" && progress.Status != "Rejected")
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

            // Reset approval chain for fresh review cycle (match SubmitEvidenceWithCoaching pattern)
            progress.SrSpvApprovalStatus = "Pending";
            progress.SrSpvApprovedById = null;
            progress.SrSpvApprovedAt = null;
            progress.ShApprovalStatus = "Pending";
            progress.ShApprovedById = null;
            progress.ShApprovedAt = null;
            if (wasRejected)
            {
                progress.RejectedAt = null;
                progress.RejectionReason = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Evidence berhasil diupload. Menunggu review approver.";
            return RedirectToAction("Deliverable", new { id = progressId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEvidence(int progressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Load progress record
            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null || string.IsNullOrEmpty(progress.EvidencePath))
            {
                return NotFound();
            }

            // Access control: coachee themselves OR coach/supervisor OR management OR HC OR Admin
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();
            bool isCoachee = progress.CoacheeId == user.Id;
            bool hasFullAccess = UserRoles.HasFullAccess(user.RoleLevel); // Level 1-3
            bool isSectionScoped = UserRoles.HasSectionAccess(user.RoleLevel); // Level 4
            bool isCoach = user.RoleLevel == 5; // Level 5 only

            if (isCoachee || hasFullAccess)
            {
                // Own data or full-access role — allow
            }
            else if (isSectionScoped || isCoach)
            {
                var coachee = await _context.Users
                    .Where(u => u.Id == progress.CoacheeId)
                    .Select(u => new { u.Section })
                    .FirstOrDefaultAsync();
                if (coachee == null || coachee.Section != user.Section)
                    return Forbid();
            }
            else
            {
                // Coachee accessing other coachee, or unknown role — deny
                return Forbid();
            }

            // Validate and build file path
            var relativePath = progress.EvidencePath.TrimStart('/');
            var filePath = Path.Combine(_env.WebRootPath, relativePath);

            // Security check: ensure file is within evidence directory
            var evidenceDir = Path.Combine(_env.WebRootPath, "uploads", "evidence");
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.FullName.StartsWith(evidenceDir, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            // Check file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Determine content type
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            string contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            // Read and return file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, progress.EvidenceFileName ?? "evidence");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CoachingProton(
            string? coacheeId = null,
            string? bagian = null,
            string? unit = null,
            string? trackType = null,
            string? tahun = null,
            int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";
            int userLevel = user.RoleLevel;
            // --- STEP 1: Role-scoped coachee IDs (SERVER ENFORCEMENT) ---
            List<string> scopedCoacheeIds;
            List<ApplicationUser>? coacheeList = null;

            if (userLevel <= 3) // HC/Admin/Direktur/VP/Manager — see all coachees
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.RoleLevel == 6 && u.IsActive)
                    .Select(u => u.Id).ToListAsync();
            }
            else if (userLevel == 4) // SectionHead/SrSpv — same section only
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6 && u.IsActive)
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

            // --- STEP 2: Apply Bagian filter (HC/Admin + Direktur/VP/Manager) ---
            if (userLevel <= 3 && !string.IsNullOrEmpty(bagian))
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
                if (userLevel <= 3)
                {
                    // HC/Admin/Direktur/VP/Manager: any unit (but must be within selected bagian if set)
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
            // SectionHead/SrSpv (level 4), Coach (level 5): default to empty until a coachee is selected
            // Direktur/VP/Manager (level 3): load all by default like HC/Admin
            bool requiresCoacheeSelection = (userLevel >= 4 && userLevel <= 5);
            var dataCoacheeIds = !string.IsNullOrEmpty(targetCoacheeId)
                ? new List<string> { targetCoacheeId }
                : requiresCoacheeSelection
                    ? new List<string>()
                    : scopedCoacheeIds;

            // Load deliverable progress data
            List<TrackingItem> data = new();
            int progressPercent = 0;
            int pendingActions = 0;
            int pendingApprovals = 0;

            // Build base query (filter out progresses for coachees with inactive ProtonTrackAssignments)
            var activeAssignmentCoacheeIds = await _context.ProtonTrackAssignments
                .Where(a => a.IsActive && dataCoacheeIds.Contains(a.CoacheeId))
                .Select(a => a.CoacheeId)
                .Distinct()
                .ToListAsync();

            var query = _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                            .ThenInclude(k => k!.ProtonTrack)
                .Where(p => activeAssignmentCoacheeIds.Contains(p.CoacheeId));

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
                SrSpvApprovedAt = p.SrSpvApprovedAt.HasValue ? p.SrSpvApprovedAt.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID")) : "",
                ShApproverName = p.ShApprovedById != null && approverNames.ContainsKey(p.ShApprovedById) ? approverNames[p.ShApprovedById] : "",
                ShApprovedAt = p.ShApprovedAt.HasValue ? p.ShApprovedAt.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID")) : "",
                HcReviewerName = p.HCReviewedById != null && approverNames.ContainsKey(p.HCReviewedById) ? approverNames[p.HCReviewedById] : "",
                HcReviewedAt = p.HCReviewedAt.HasValue ? p.HCReviewedAt.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID")) : "",
            }).ToList();

            // --- PAGINATION: Group-boundary slicing (UI-04) ---
            const int targetRowsPerPage = 20;
            int pageNumber = Math.Max(1, page);

            // Group data by Kompetensi (then SubKompetensi) to build pages that never split a group
            // For multi-coachee view: group by CoacheeName > Kompetensi > SubKompetensi
            // For single-coachee view: group by Kompetensi > SubKompetensi
            // Group key: (CoacheeName, Kompetensi, SubKompetensi) — finest grouping unit for boundary check
            var finestGroups = data
                .GroupBy(item => new { item.CoacheeName, item.Kompetensi, item.SubKompetensi })
                .ToList();

            // Slice groups into pages, never splitting a group
            var pagesGroups = new List<List<TrackingItem>>();
            var currentPageItems = new List<TrackingItem>();
            int currentRowCount = 0;

            foreach (var group in finestGroups)
            {
                int groupSize = group.Count();
                // Start a new page if adding this group would exceed target AND we already have rows
                if (currentRowCount > 0 && currentRowCount + groupSize > targetRowsPerPage)
                {
                    pagesGroups.Add(new List<TrackingItem>(currentPageItems));
                    currentPageItems = new List<TrackingItem>();
                    currentRowCount = 0;
                }
                currentPageItems.AddRange(group);
                currentRowCount += groupSize;
            }
            if (currentPageItems.Count > 0)
                pagesGroups.Add(currentPageItems);

            int totalPages = Math.Max(1, pagesGroups.Count);
            // Clamp page number
            if (pageNumber > totalPages) pageNumber = totalPages;

            // Slice data to current page
            var paginatedData = pageNumber >= 1 && pageNumber <= pagesGroups.Count
                ? pagesGroups[pageNumber - 1]
                : new List<TrackingItem>();

            // Compute display row range (1-based, based on full data positions)
            int pageFirstRow = 0;
            int pageLastRow = 0;
            if (pagesGroups.Count > 0 && pageNumber >= 1 && pageNumber <= pagesGroups.Count)
            {
                pageFirstRow = pagesGroups.Take(pageNumber - 1).Sum(p => p.Count) + 1;
                pageLastRow = pageFirstRow + paginatedData.Count - 1;
            }

            // Replace data with paginated slice (summary stats already computed from full `progresses`)
            data = paginatedData;

            // Compute summary stats
            int total = progresses.Count;
            double weightedSum = progresses.Sum(p =>
                p.Status == "Approved" ? 1.0 :
                p.Status == "Submitted" ? 0.5 : 0.0);
            progressPercent = total > 0 ? (int)(weightedSum / total * 100) : 0;
            pendingActions = progresses.Count(p => p.Status == "Pending" || p.Status == "Rejected");
            // Role-aware pending approvals (includes co-sign: Approved but own approval still Pending)
            if (userRole == UserRoles.SrSupervisor)
                pendingApprovals = progresses.Count(p => (p.Status == "Submitted" || p.Status == "Approved") && p.SrSpvApprovalStatus == "Pending");
            else if (userRole == UserRoles.SectionHead)
                pendingApprovals = progresses.Count(p => (p.Status == "Submitted" || p.Status == "Approved") && p.ShApprovalStatus == "Pending");
            else if (userRole == UserRoles.HC)
                pendingApprovals = progresses.Count(p => p.Status == "Submitted" && p.HCApprovalStatus == "Pending");
            else
                pendingApprovals = progresses.Count(p => p.Status == "Submitted");

            // --- ViewBag: pagination state (UI-04) ---
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageFirstRow = pageFirstRow;
            ViewBag.PageLastRow = pageLastRow;
            // FilteredCount = total rows across all pages (before pagination); keep existing assignment below

            // --- ViewBag: filter option lists ---
            ViewBag.AllBagian = OrganizationStructure.GetAllSections();
            if (!string.IsNullOrEmpty(bagian))
            {
                ViewBag.AllUnits = OrganizationStructure.GetUnitsForSection(bagian);
            }
            else if (userLevel == 4)
            {
                // Section-scoped roles: auto-populate units from their own section
                ViewBag.AllUnits = OrganizationStructure.GetUnitsForSection(user.Section ?? "");
            }
            else
            {
                ViewBag.AllUnits = new List<string>();
            }
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
            ViewBag.FilteredCount = progresses.Count; // total rows across all pages (for "Menampilkan X-Y dari Z" display)

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

            // --- Empty state scenario detection (UI-02) ---
            // Detect BEFORE pagination so we check the full dataset count
            // `progresses.Count` = full filtered dataset (0 means no results for current filter)
            // `scopedCoacheeIds.Count` = whether the user has any coachees in scope at all
            string emptyScenario = "";
            if (progresses.Count == 0)
            {
                if (scopedCoacheeIds.Count == 0)
                {
                    // No coachees assigned at all (role scope is empty)
                    emptyScenario = "no_coachees";
                }
                else if (dataCoacheeIds.Count == 0 && string.IsNullOrEmpty(targetCoacheeId) && requiresCoacheeSelection)
                {
                    // Coach/SrSpv/SH: no coachee selected yet — prompt to select
                    emptyScenario = "select_coachee";
                }
                else if (!string.IsNullOrEmpty(bagian) || !string.IsNullOrEmpty(unit) ||
                         !string.IsNullOrEmpty(trackType) || !string.IsNullOrEmpty(tahun) ||
                         !string.IsNullOrEmpty(targetCoacheeId))
                {
                    // Coachees exist but active filters narrow to zero results
                    emptyScenario = "no_filter_match";
                }
                else
                {
                    // Coachees exist, no filters active, but no deliverables yet
                    emptyScenario = "no_deliverables";
                }
            }
            ViewBag.EmptyScenario = emptyScenario;

            // HC pending review panel (Phase 53) — load pending deliverables for HC/Admin to review
            if (userLevel <= 2) // HC or Admin
            {
                // Load all deliverables with HCApprovalStatus == "Pending" within scoped coachees
                // Use a fresh query (not filtered by pagination) so panel shows ALL pending even when table is filtered
                var hcPendingRaw = await _context.ProtonDeliverableProgresses
                    .Include(p => p.ProtonDeliverable)
                        .ThenInclude(d => d!.ProtonSubKompetensi)
                            .ThenInclude(s => s!.ProtonKompetensi)
                    .Where(p => scopedCoacheeIds.Contains(p.CoacheeId)
                             && p.HCApprovalStatus == "Pending"
                             && (p.Status == "Submitted" || p.Status == "Approved" || p.Status == "Rejected"))
                    .OrderBy(p => p.SubmittedAt)
                    .ToListAsync();

                // Batch-load coachee names for the panel
                var panelCoacheeIds = hcPendingRaw.Select(p => p.CoacheeId).Distinct().ToList();
                var panelCoacheeNames = panelCoacheeIds.Any()
                    ? await _context.Users
                        .Where(u => panelCoacheeIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id)
                    : new Dictionary<string, string>();

                ViewBag.HcPendingReviews = hcPendingRaw.Select(p => new
                {
                    ProgressId = p.Id,
                    CoacheeName = panelCoacheeNames.TryGetValue(p.CoacheeId, out var pn) ? pn : p.CoacheeId,
                    Deliverable = p.ProtonDeliverable?.NamaDeliverable ?? "",
                    Kompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "",
                    SubKompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "",
                    Status = p.Status,
                    SubmittedAt = p.SubmittedAt.HasValue ? p.SubmittedAt.Value.ToLocalTime().ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID")) : "-"
                }).ToList();
            }
            else
            {
                ViewBag.HcPendingReviews = null;
            }

            return View(data);
        }

        // ===== Phase 65: AJAX approval endpoints =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveFromProgress(int progressId, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Tidak terautentikasi." });

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";

            // Level-based access: only L4 (SrSupervisor/SectionHead) can approve
            int roleLevel = UserRoles.GetRoleLevel(userRole);
            if (!UserRoles.HasSectionAccess(roleLevel))
                return Json(new { success = false, message = "Akses tidak diizinkan." });

            bool isSrSpv = userRole == UserRoles.SrSupervisor;
            bool isSH = userRole == UserRoles.SectionHead;

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            // Co-sign guard: allow Submitted, or Approved when own approval still Pending
            bool canApprove = progress.Status == "Submitted" ||
                (progress.Status == "Approved" && (
                    (isSrSpv && progress.SrSpvApprovalStatus != "Approved") ||
                    (isSH && progress.ShApprovalStatus != "Approved")));
            if (!canApprove)
                return Json(new { success = false, message = "Deliverable ini tidak dapat disetujui saat ini." });

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
            var approvedAtLocal = now.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID"));

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

            // Level-based access: only L4 (SrSupervisor/SectionHead) can reject
            int roleLevel = UserRoles.GetRoleLevel(userRole);
            if (!UserRoles.HasSectionAccess(roleLevel))
                return Json(new { success = false, message = "Akses tidak diizinkan." });

            bool isSrSpv = userRole == UserRoles.SrSupervisor;
            bool isSH = userRole == UserRoles.SectionHead;

            if (string.IsNullOrWhiteSpace(rejectionReason))
                return Json(new { success = false, message = "Alasan penolakan tidak boleh kosong." });

            var progress = await _context.ProtonDeliverableProgresses
                .FirstOrDefaultAsync(p => p.Id == progressId);
            if (progress == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            // Reject allowed for Submitted or Approved (co-sign scenario: one L4 can reject after the other approved)
            if (progress.Status != "Submitted" && progress.Status != "Approved")
                return Json(new { success = false, message = "Deliverable ini tidak dapat ditolak saat ini." });

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
            var approvedAtLocal = now.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID"));

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
            var reviewedAtLocal = now.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("id-ID"));

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
                // Phase 117: Record status history (before overwriting Status)
                bool isResubmit = progress.Status == "Rejected";
                string submitStatusType = isResubmit ? "Re-submitted" : "Submitted";
                RecordStatusHistory(progress.Id, submitStatusType, user.Id, user.FullName, "Coach");

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

            // Scope validation: section-scoped roles can only export their own section
            // Full access roles (Admin, HC, management level 3) bypass; SectionHead is level 4 (section-scoped)
            if (!UserRoles.HasFullAccess(user.RoleLevel))
            {
                if (coacheeUser.Section != user.Section)
                    return Forbid();
            }

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
            var ws = workbook.Worksheets.Add("Coaching Proton");

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

            // Scope validation: section-scoped roles can only export their own section
            // Full access roles (Admin, HC, management level 3) bypass; SectionHead is level 4 (section-scoped)
            if (!UserRoles.HasFullAccess(user.RoleLevel))
            {
                if (coacheeUser.Section != user.Section)
                    return Forbid();
            }

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
                        col.Item().Text($"Coaching Proton — {coacheeName}").FontSize(14).Bold();
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
                if (string.IsNullOrEmpty(user.Section))
                    return Json(new { error = "unauthorized", data = (object?)null });
                var coacheeUser = await _context.Users.FindAsync(coacheeId);
                if (coacheeUser == null || coacheeUser.Section != user.Section)
                    return Json(new { error = "unauthorized", data = (object?)null });
            }
            // Level 1-3 (Admin, HC, Direktur/VP/Manager): allow all

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
            int pendingActions = progresses.Count(p => p.Status == "Pending" || p.Status == "Rejected");
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

        // ====================================================================
        // PHASE 107: HISTORI PROTON — Worker List & Detail
        // ====================================================================

        public async Task<IActionResult> HistoriProton()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            int userLevel = user.RoleLevel;

            // --- Role-scoped coachee IDs (same pattern as CoachingProton) ---
            List<string> scopedCoacheeIds;

            if (userLevel <= 3) // HC/Admin/Direktur/VP/Manager — all
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.RoleLevel == 6 && u.IsActive)
                    .Select(u => u.Id).ToListAsync();
            }
            else if (userLevel == 4) // SectionHead/SrSpv — same section
            {
                scopedCoacheeIds = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6 && u.IsActive)
                    .Select(u => u.Id).ToListAsync();
            }
            else if (userLevel == 5) // Coach — mapped coachees only
            {
                scopedCoacheeIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId).ToListAsync();
            }
            else // Level 6 (Coachee) — redirect to own detail
            {
                return RedirectToAction("HistoriProtonDetail", new { userId = user.Id });
            }

            // --- Query assignments for scoped coachees ---
            var assignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => scopedCoacheeIds.Contains(a.CoacheeId))
                .ToListAsync();

            // Only coachees with at least 1 assignment appear (HIST-05)
            var coacheeIdsWithAssignments = assignments.Select(a => a.CoacheeId).Distinct().ToList();

            // Get final assessments for those assignments
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var assessments = await _context.ProtonFinalAssessments
                .Where(fa => assignmentIds.Contains(fa.ProtonTrackAssignmentId))
                .ToListAsync();
            var assessmentsByAssignmentId = assessments.ToDictionary(fa => fa.ProtonTrackAssignmentId);

            // Get user info for coachees with assignments
            var coacheeUsers = await _context.Users
                .Where(u => coacheeIdsWithAssignments.Contains(u.Id) && u.IsActive)
                .ToListAsync();

            // Build worker rows
            var coacheeGroups = assignments.GroupBy(a => a.CoacheeId);
            var workers = new List<HistoriProtonWorkerRow>();

            foreach (var group in coacheeGroups)
            {
                var coacheeUser = coacheeUsers.FirstOrDefault(u => u.Id == group.Key);
                if (coacheeUser == null) continue;

                var coacheeAssignments = group.ToList();
                var latestAssignment = coacheeAssignments.OrderByDescending(a => a.AssignedAt).First();

                // Determine Jalur from latest assignment
                string jalur = latestAssignment.ProtonTrack?.TrackType ?? "";

                // Check progress per TahunKe
                bool tahun1Done = false, tahun2Done = false, tahun3Done = false;
                bool tahun1InProgress = false, tahun2InProgress = false, tahun3InProgress = false;

                foreach (var a in coacheeAssignments)
                {
                    if (a.ProtonTrack == null) continue;
                    string tahunKe = a.ProtonTrack.TahunKe;
                    bool hasAssessment = assessmentsByAssignmentId.ContainsKey(a.Id);

                    if (tahunKe == "Tahun 1")
                    {
                        if (hasAssessment) tahun1Done = true;
                        else tahun1InProgress = true;
                    }
                    else if (tahunKe == "Tahun 2")
                    {
                        if (hasAssessment) tahun2Done = true;
                        else tahun2InProgress = true;
                    }
                    else if (tahunKe == "Tahun 3")
                    {
                        if (hasAssessment) tahun3Done = true;
                        else tahun3InProgress = true;
                    }
                }

                // Status based on latest assignment
                string status;
                if (assessmentsByAssignmentId.ContainsKey(latestAssignment.Id))
                    status = "Lulus";
                else
                    status = "Dalam Proses";

                workers.Add(new HistoriProtonWorkerRow
                {
                    UserId = coacheeUser.Id,
                    Nama = coacheeUser.FullName,
                    NIP = coacheeUser.NIP ?? "",
                    Section = coacheeUser.Section ?? "",
                    Unit = coacheeUser.Unit ?? "",
                    Jalur = jalur,
                    Tahun1Done = tahun1Done,
                    Tahun2Done = tahun2Done,
                    Tahun3Done = tahun3Done,
                    Tahun1InProgress = tahun1InProgress,
                    Tahun2InProgress = tahun2InProgress,
                    Tahun3InProgress = tahun3InProgress,
                    Status = status
                });
            }

            // Sort by Nama A-Z (default)
            workers = workers.OrderBy(w => w.Nama).ToList();

            // Use OrganizationStructure for filter dropdowns (not data-driven)
            var viewModel = new HistoriProtonViewModel
            {
                Workers = workers,
                AvailableSections = HcPortal.Models.OrganizationStructure.GetAllSections(),
                AvailableUnits = new List<string>() // Units populated client-side via cascade
            };

            // L4 lock and cascade data
            ViewBag.LockedSection = userLevel == 4 ? user.Section : null;
            ViewBag.UserLevel = userLevel;
            ViewBag.OrgStructureJson = System.Text.Json.JsonSerializer.Serialize(
                HcPortal.Models.OrganizationStructure.SectionUnits);

            return View(viewModel);
        }

        public async Task<IActionResult> HistoriProtonDetail(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            int userLevel = user.RoleLevel;

            // Authorization check
            if (userLevel >= 6)
            {
                // Coachee can only view own
                if (userId != user.Id) return Forbid();
            }
            else if (userLevel == 5)
            {
                // Coach can view mapped coachees
                var isMapped = await _context.CoachCoacheeMappings
                    .AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == userId && m.IsActive);
                if (!isMapped) return Forbid();
            }
            else if (userLevel == 4)
            {
                // SrSpv/SH can view same section workers
                var sectionTarget = await _context.Users.FindAsync(userId);
                if (sectionTarget == null || sectionTarget.Section != user.Section) return Forbid();
            }
            // Level <= 3 (HC/Admin) can view all — no check needed

            var targetUser = await _context.Users.FindAsync(userId);
            if (targetUser == null) return NotFound();

            var assignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => a.CoacheeId == userId)
                .OrderBy(a => a.ProtonTrack!.Urutan)
                .ToListAsync();

            if (!assignments.Any()) return NotFound();

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var assessments = await _context.ProtonFinalAssessments
                .Where(fa => assignmentIds.Contains(fa.ProtonTrackAssignmentId))
                .ToDictionaryAsync(fa => fa.ProtonTrackAssignmentId);

            // Get coach name
            var coachMapping = await _context.CoachCoacheeMappings
                .Where(m => m.CoacheeId == userId)
                .OrderByDescending(m => m.IsActive)
                .ThenByDescending(m => m.Id)
                .FirstOrDefaultAsync();
            string coachName = "N/A";
            if (coachMapping != null)
            {
                var coach = await _context.Users.FindAsync(coachMapping.CoachId);
                coachName = coach?.FullName ?? "N/A";
            }

            string jalur = assignments.First().ProtonTrack?.TrackType ?? "";

            var nodes = assignments.Select(a => {
                var hasAssessment = assessments.TryGetValue(a.Id, out var fa);
                return new ProtonTimelineNode
                {
                    AssignmentId = a.Id,
                    TahunKe = a.ProtonTrack?.TahunKe ?? "",
                    TahunUrutan = a.ProtonTrack?.Urutan ?? 0,
                    Unit = targetUser.Unit ?? "",
                    CoachName = coachName,
                    Status = hasAssessment ? "Lulus" : "Dalam Proses",
                    CompetencyLevel = hasAssessment ? fa!.CompetencyLevelGranted : null,
                    StartDate = a.AssignedAt,
                    EndDate = hasAssessment ? fa!.CompletedAt : null
                };
            }).OrderBy(n => n.TahunUrutan).ToList();

            var viewModel = new HistoriProtonDetailViewModel
            {
                Nama = targetUser.FullName,
                NIP = targetUser.NIP ?? "",
                Unit = targetUser.Unit ?? "",
                Section = targetUser.Section ?? "",
                Jalur = jalur,
                Nodes = nodes
            };

            return View(viewModel);
        }

        // ===== Phase 117: Status History Helper =====
        private void RecordStatusHistory(int progressId, string statusType, string actorId, string actorName, string actorRole, string? rejectionReason = null)
        {
            _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
            {
                ProtonDeliverableProgressId = progressId,
                StatusType = statusType,
                ActorId = actorId,
                ActorName = actorName,
                ActorRole = actorRole,
                RejectionReason = rejectionReason,
                Timestamp = DateTime.UtcNow
            });
        }

    }
}