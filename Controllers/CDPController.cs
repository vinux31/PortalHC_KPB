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
using HcPortal.Services;

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
        private readonly INotificationService _notificationService;

        public CDPController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IWebHostEnvironment env, INotificationService notificationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
            _notificationService = notificationService;
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
                                Target = d.Target ?? ""
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

        public async Task<IActionResult> Dashboard()
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

            return View(model);
        }

        // ============================================================
        // Phase 121: AJAX endpoint — filter Coaching Proton content
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> FilterCoachingProton(string? section, string? unit, string? category, string? track)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "";

            // Server-side enforcement: override section/unit for restricted roles
            int roleLevel = UserRoles.GetRoleLevel(userRole);
            if (UserRoles.HasSectionAccess(roleLevel)) { section = user.Section; }
            else if (UserRoles.IsCoachingRole(roleLevel)) { section = user.Section; unit = user.Unit; }

            var model = await BuildProtonProgressSubModelAsync(user, userRole, section, unit, category, track);
            return PartialView("Shared/_CoachingProtonContentPartial", model);
        }

        // ============================================================
        // Phase 121: Cascade options endpoint — returns units for section
        // ============================================================
        [HttpGet]
        public IActionResult GetCascadeOptions(string? section)
        {
            var units = string.IsNullOrEmpty(section) ? new List<string>() : OrganizationStructure.GetUnitsForSection(section);
            var categories = _context.ProtonTracks.Select(t => t.TrackType).Distinct().OrderBy(t => t).ToList();
            var tracks = _context.ProtonTracks.OrderBy(t => t.Urutan).Select(t => t.DisplayName).ToList();
            return Json(new { units, categories, tracks });
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
        private async Task<ProtonProgressSubModel> BuildProtonProgressSubModelAsync(ApplicationUser user, string userRole, string? section = null, string? unit = null, string? category = null, string? track = null)
        {
            // DASH-02: Build scoped coachee ID list via active ProtonTrackAssignment
            List<string> scopedCoacheeIds;
            string scopeLabel;
            int roleLevel = UserRoles.GetRoleLevel(userRole);

            // Phase 127: Assignment-based scoping — only coachees with active assignments are visible
            if (userRole == UserRoles.HC || userRole == UserRoles.Admin)
            {
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => a.IsActive)
                    .Select(a => a.CoacheeId)
                    .Distinct()
                    .ToListAsync();
                scopeLabel = "All Sections";
            }
            else if (UserRoles.HasSectionAccess(roleLevel))
            {
                // SectionHead/SrSpv: coachees with active assignment whose section matches
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => a.IsActive)
                    .Join(_context.Users, a => a.CoacheeId, u => u.Id, (a, u) => new { a.CoacheeId, u.Section })
                    .Where(x => x.Section == user.Section)
                    .Select(x => x.CoacheeId)
                    .Distinct()
                    .ToListAsync();
                scopeLabel = $"Section: {user.Section ?? "(unknown)"}";
            }
            else // Coach
            {
                // Coach: mapped coachees that also have active assignments
                var mappedCoacheeIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId)
                    .ToListAsync();
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => a.IsActive && mappedCoacheeIds.Contains(a.CoacheeId))
                    .Select(a => a.CoacheeId)
                    .Distinct()
                    .ToListAsync();
                scopeLabel = !string.IsNullOrEmpty(user.Unit)
                    ? $"Unit: {user.Unit}"
                    : $"Section: {user.Section ?? "(unknown)"} (Unit not set)";
            }

            // Batch load data (avoid N+1)
            var coacheeUsers = await _context.Users
                .Where(u => scopedCoacheeIds.Contains(u.Id))
                .ToListAsync();

            // Phase 121: Apply additional section/unit filters for full-access roles
            if (!string.IsNullOrEmpty(section) && UserRoles.HasFullAccess(roleLevel))
                coacheeUsers = coacheeUsers.Where(u => u.Section == section).ToList();
            if (!string.IsNullOrEmpty(unit))
                coacheeUsers = coacheeUsers.Where(u => u.Unit == unit).ToList();

            var filteredCoacheeIds = coacheeUsers.Select(u => u.Id).ToList();
            var userNames = coacheeUsers.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);

            // Phase 127: Query progress via assignment FK, not just CoacheeId
            var assignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => filteredCoacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();

            var activeAssignmentIds = assignments.Select(a => a.Id).ToList();

            var allProgresses = await _context.ProtonDeliverableProgresses
                .Where(p => activeAssignmentIds.Contains(p.ProtonTrackAssignmentId))
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                .ToListAsync();

            // Phase 129: Defensive unit filter — exclude progress where kompetensi unit doesn't match assignment's resolved unit
            {
                var asnCoacheeIds129 = assignments.Select(a => new { a.Id, a.CoacheeId }).ToList();
                var coacheeIdList129 = asnCoacheeIds129.Select(x => x.CoacheeId).Distinct().ToList();
                var mappingUnits129 = await _context.CoachCoacheeMappings
                    .Where(m => m.IsActive && coacheeIdList129.Contains(m.CoacheeId))
                    .Select(m => new { m.CoacheeId, m.AssignmentUnit })
                    .ToListAsync();
                var userUnits129 = await _context.Users
                    .Where(u => coacheeIdList129.Contains(u.Id))
                    .Select(u => new { u.Id, u.Unit })
                    .ToDictionaryAsync(u => u.Id, u => u.Unit);
                var asnUnitMap129 = asnCoacheeIds129.ToDictionary(
                    x => x.Id,
                    x => (mappingUnits129.FirstOrDefault(m => m.CoacheeId == x.CoacheeId)?.AssignmentUnit
                          ?? userUnits129.GetValueOrDefault(x.CoacheeId))?.Trim() ?? "");

                allProgresses = allProgresses.Where(p =>
                {
                    var kompUnit = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Unit?.Trim() ?? "";
                    var resolvedUnit = asnUnitMap129.GetValueOrDefault(p.ProtonTrackAssignmentId, "");
                    return string.IsNullOrEmpty(resolvedUnit) || kompUnit == resolvedUnit;
                }).ToList();
            }

            // Phase 121: Apply category/track filters
            if (!string.IsNullOrEmpty(category))
                assignments = assignments.Where(a => a.ProtonTrack?.TrackType == category).ToList();
            if (!string.IsNullOrEmpty(track))
                assignments = assignments.Where(a => a.ProtonTrack?.DisplayName == track).ToList();

            // Phase 121: Collect available filter options from loaded data
            var availableCategories = assignments.Select(a => a.ProtonTrack?.TrackType).Where(t => t != null).Distinct().OrderBy(t => t).ToList()!;
            var availableTracks = assignments.OrderBy(a => a.ProtonTrack?.Urutan).Select(a => a.ProtonTrack?.DisplayName).Where(t => t != null).Distinct().ToList()!;

            var assignmentDict = assignments.ToDictionary(a => a.CoacheeId, a => a);

            var finalAssessments = await _context.ProtonFinalAssessments
                .Where(fa => filteredCoacheeIds.Contains(fa.CoacheeId))
                .ToListAsync();
            var finalAssessmentDict = finalAssessments
                .GroupBy(fa => fa.CoacheeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(fa => fa.CreatedAt).First());

            // Build per-coachee rows (flat table sorted by name)
            var progressByCoachee = allProgresses.GroupBy(p => p.CoacheeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // When category/track filter is active, only show coachees that have matching assignments
            var displayCoacheeIds = (!string.IsNullOrEmpty(category) || !string.IsNullOrEmpty(track))
                ? filteredCoacheeIds.Where(id => assignmentDict.ContainsKey(id)).ToList()
                : filteredCoacheeIds;

            var coacheeRows = new List<CoacheeProgressRow>();
            foreach (var coacheeId in displayCoacheeIds)
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

            // Phase 121: Determine locked values and available options
            string? lockedSection = null, lockedUnit = null;
            if (UserRoles.HasSectionAccess(roleLevel)) { lockedSection = user.Section; }
            else if (UserRoles.IsCoachingRole(roleLevel)) { lockedSection = user.Section; lockedUnit = user.Unit; }

            var availableSections = OrganizationStructure.GetAllSections();
            var effectiveSection = lockedSection ?? section;
            var availableUnits = !string.IsNullOrEmpty(effectiveSection)
                ? OrganizationStructure.GetUnitsForSection(effectiveSection)
                : new List<string>();

            var subModel = new ProtonProgressSubModel
            {
                TotalCoachees = displayCoacheeIds.Count,
                TotalDeliverables = allProgresses.Count,
                ApprovedDeliverables = allProgresses.Count(p => p.Status == "Approved"),
                PendingSpvApprovals = pendingSpv,
                PendingHCReviews = pendingHC,
                CompletedCoachees = finalAssessmentDict.Count,
                CoacheeRows = coacheeRows,
                TrendLabels = trendLabels,
                TrendValues = trendValues,
                StatusLabels = statusLabels,
                StatusData = statusData,
                // Phase 121: Filter state
                FilterSection = section,
                FilterUnit = unit,
                FilterCategory = category,
                FilterTrack = track,
                RoleLevel = roleLevel,
                LockedSection = lockedSection,
                LockedUnit = lockedUnit,
                AvailableSections = availableSections,
                AvailableUnits = availableUnits,
                AvailableCategories = availableCategories!,
                AvailableTracks = availableTracks!
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
                // Coach: mapping-based access (supports cross-section coachees)
                var hasMapping = await _context.CoachCoacheeMappings
                    .AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == progress.CoacheeId && m.IsActive);
                if (!hasMapping)
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

            // Phase 117: Load status history for timeline
            ViewBag.StatusHistories = await _context.DeliverableStatusHistories
                .Where(h => h.ProtonDeliverableProgressId == id)
                .OrderBy(h => h.Timestamp)
                .ToListAsync();

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

            // COACH-05: Notify coach and coachee on approval
            try
            {
                var approveMapping = await _context.CoachCoacheeMappings
                    .FirstOrDefaultAsync(m => m.CoacheeId == progress.CoacheeId && m.IsActive);
                var approveCoachee = await _context.Users
                    .Where(u => u.Id == progress.CoacheeId)
                    .Select(u => new { u.FullName, u.UserName })
                    .FirstOrDefaultAsync();
                var approveCoacheeName = approveCoachee?.FullName ?? approveCoachee?.UserName ?? progress.CoacheeId;

                if (approveMapping != null)
                {
                    await _notificationService.SendAsync(
                        approveMapping.CoachId,
                        "COACH_EVIDENCE_APPROVED",
                        "Deliverable Disetujui",
                        $"Deliverable {approveCoacheeName} telah disetujui",
                        "/CDP/CoachingProton"
                    );
                }
                await _notificationService.SendAsync(
                    progress.CoacheeId,
                    "COACH_EVIDENCE_APPROVED",
                    "Deliverable Disetujui",
                    "Deliverable Anda telah disetujui",
                    "/CDP/CoachingProton"
                );
            }
            catch { /* fail silently */ }

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

            // COACH-06: Notify coach and coachee on rejection
            try
            {
                var rejectMapping = await _context.CoachCoacheeMappings
                    .FirstOrDefaultAsync(m => m.CoacheeId == progress.CoacheeId && m.IsActive);
                var rejectCoachee = await _context.Users
                    .Where(u => u.Id == progress.CoacheeId)
                    .Select(u => new { u.FullName, u.UserName })
                    .FirstOrDefaultAsync();
                var rejectCoacheeName = rejectCoachee?.FullName ?? rejectCoachee?.UserName ?? progress.CoacheeId;

                if (rejectMapping != null)
                {
                    await _notificationService.SendAsync(
                        rejectMapping.CoachId,
                        "COACH_EVIDENCE_REJECTED",
                        "Deliverable Ditolak",
                        $"Deliverable {rejectCoacheeName} telah ditolak",
                        "/CDP/CoachingProton"
                    );
                }
                await _notificationService.SendAsync(
                    progress.CoacheeId,
                    "COACH_EVIDENCE_REJECTED",
                    "Deliverable Ditolak",
                    "Deliverable Anda telah ditolak",
                    "/CDP/CoachingProton"
                );
            }
            catch { /* fail silently */ }

            TempData["Success"] = "Deliverable berhasil ditolak.";
            return RedirectToAction("Deliverable", new { id = progressId });
        }

        private async Task NotifyReviewersAsync(string coacheeId, string coacheeName)
        {
            try
            {
                var mapping = await _context.CoachCoacheeMappings
                    .FirstOrDefaultAsync(m => m.CoacheeId == coacheeId && m.IsActive);
                if (mapping == null) return;

                var section = mapping.AssignmentSection;
                var reviewers = await _context.Users
                    .Where(u => u.IsActive && u.Section == section && u.RoleLevel == 4)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var reviewerId in reviewers)
                {
                    await _notificationService.SendAsync(
                        reviewerId,
                        "COACH_EVIDENCE_SUBMITTED",
                        "Deliverable Disubmit",
                        $"Deliverable {coacheeName} telah disubmit untuk review",
                        "/CDP/CoachingProton"
                    );
                }
            }
            catch { /* fail silently */ }
        }

        private async Task CreateHCNotificationAsync(string coacheeId)
        {
            try
            {
                // Deduplication: check UserNotification instead of ProtonNotification
                bool alreadyNotified = await _context.UserNotifications
                    .AnyAsync(n => n.Type == "COACH_ALL_COMPLETE" && n.Message.Contains(coacheeId));
                if (alreadyNotified) return;

                var coachee = await _context.Users
                    .Where(u => u.Id == coacheeId)
                    .Select(u => new { u.FullName, u.UserName })
                    .FirstOrDefaultAsync();
                var coacheeName = coachee?.FullName ?? coachee?.UserName ?? coacheeId;

                var hcUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HC);
                foreach (var hc in hcUsers)
                {
                    await _notificationService.SendAsync(
                        hc.Id,
                        "COACH_ALL_COMPLETE",
                        "Semua Deliverable Selesai",
                        $"Semua deliverable {coacheeName} telah selesai",
                        "/CDP/CoachingProton"
                    );
                }
            }
            catch { /* fail silently */ }
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

            // COACH-04: Notify section reviewers
            var coacheeForNotify = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.FullName, u.UserName })
                .FirstOrDefaultAsync();
            var coacheeNameForNotify = coacheeForNotify?.FullName ?? coacheeForNotify?.UserName ?? progress.CoacheeId;
            await NotifyReviewersAsync(progress.CoacheeId, coacheeNameForNotify);

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
            // --- STEP 1: Role-scoped coachee IDs via active ProtonTrackAssignment (Phase 127) ---
            List<string> scopedCoacheeIds;
            List<ApplicationUser>? coacheeList = null;

            if (userLevel <= 3) // HC/Admin/Direktur/VP/Manager — coachees with active assignments
            {
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => a.IsActive)
                    .Select(a => a.CoacheeId).Distinct().ToListAsync();
            }
            else if (userLevel == 4) // SectionHead/SrSpv — coachees with active assignment in same section
            {
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => a.IsActive)
                    .Join(_context.Users, a => a.CoacheeId, u => u.Id, (a, u) => new { a.CoacheeId, u.Section })
                    .Where(x => x.Section == user.Section)
                    .Select(x => x.CoacheeId).Distinct().ToListAsync();
            }
            else if (userLevel == 5) // Coach — mapped coachees with active assignments
            {
                var coachMappings = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => new { m.CoacheeId, m.AssignmentSection })
                    .ToListAsync();
                var mappedIds = coachMappings.Select(m => m.CoacheeId).ToList();
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => a.IsActive && mappedIds.Contains(a.CoacheeId))
                    .Select(a => a.CoacheeId).Distinct().ToListAsync();
                ViewBag.AssignmentSections = coachMappings
                    .Where(m => !string.IsNullOrEmpty(m.AssignmentSection) && scopedCoacheeIds.Contains(m.CoacheeId))
                    .ToDictionary(m => m.CoacheeId, m => m.AssignmentSection!);
            }
            else // Level 6 (Coachee) — own ID only if has active assignment
            {
                var hasAssignment = await _context.ProtonTrackAssignments
                    .AnyAsync(a => a.IsActive && a.CoacheeId == user.Id);
                scopedCoacheeIds = hasAssignment ? new List<string> { user.Id } : new List<string>();
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

            // Phase 127: Query progress via ProtonTrackAssignmentId join
            var activeAssignmentIds = await _context.ProtonTrackAssignments
                .Where(a => a.IsActive && dataCoacheeIds.Contains(a.CoacheeId))
                .Select(a => a.Id)
                .ToListAsync();

            var query = _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                            .ThenInclude(k => k!.ProtonTrack)
                .Include(p => p.ProtonTrackAssignment)
                    .ThenInclude(a => a!.ProtonTrack)
                .Where(p => activeAssignmentIds.Contains(p.ProtonTrackAssignmentId))
                // Belt and suspenders: deliverable's track must match assignment's track
                .Where(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId
                         == p.ProtonTrackAssignment!.ProtonTrackId);

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

            // Phase 129: Defensive unit filter — exclude progress where kompetensi unit doesn't match assignment's resolved unit
            {
                var asnCoachees129 = await _context.ProtonTrackAssignments
                    .Where(a => activeAssignmentIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.CoacheeId })
                    .ToListAsync();
                var coacheeIds129 = asnCoachees129.Select(x => x.CoacheeId).Distinct().ToList();
                var mappingUnits129 = await _context.CoachCoacheeMappings
                    .Where(m => m.IsActive && coacheeIds129.Contains(m.CoacheeId))
                    .Select(m => new { m.CoacheeId, m.AssignmentUnit })
                    .ToListAsync();
                var userUnits129 = await _context.Users
                    .Where(u => coacheeIds129.Contains(u.Id))
                    .Select(u => new { u.Id, u.Unit })
                    .ToDictionaryAsync(u => u.Id, u => u.Unit);
                var asnUnitMap129 = asnCoachees129.ToDictionary(
                    x => x.Id,
                    x => (mappingUnits129.FirstOrDefault(m => m.CoacheeId == x.CoacheeId)?.AssignmentUnit
                          ?? userUnits129.GetValueOrDefault(x.CoacheeId))?.Trim() ?? "");

                progresses = progresses.Where(p =>
                {
                    var kompUnit = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Unit?.Trim() ?? "";
                    var resolvedUnit = asnUnitMap129.GetValueOrDefault(p.ProtonTrackAssignmentId, "");
                    return string.IsNullOrEmpty(resolvedUnit) || kompUnit == resolvedUnit;
                }).ToList();
            }

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

            // Phase 117: Record status history
            string approveStatusType = isSrSpv ? "SrSpv Approved" : "SH Approved";
            string approveActorRole = isSrSpv ? "Sr. Supervisor" : "Section Head";
            RecordStatusHistory(progress.Id, approveStatusType, user.Id, user.FullName, approveActorRole);

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

            // Phase 117: Record status history
            string rejectStatusType = isSrSpv ? "SrSpv Rejected" : "SH Rejected";
            string rejectActorRole = isSrSpv ? "Sr. Supervisor" : "Section Head";
            RecordStatusHistory(progress.Id, rejectStatusType, user.Id, user.FullName, rejectActorRole, rejectionReason);

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

            // Phase 117: Record status history
            RecordStatusHistory(progress.Id, "HC Reviewed", user.Id, user.FullName, "HC");

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
            IFormFile? evidenceFile,
            [FromForm] string? acuanPedoman,
            [FromForm] string? acuanTko,
            [FromForm] string? acuanBestPractice,
            [FromForm] string? acuanDokumen)
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
                    AcuanPedoman = acuanPedoman ?? "",
                    AcuanTko = acuanTko ?? "",
                    AcuanBestPractice = acuanBestPractice ?? "",
                    AcuanDokumen = acuanDokumen ?? "",
                    Status = "Submitted",
                    ProtonDeliverableProgressId = progress.Id,
                    CreatedAt = now
                };
                _context.CoachingSessions.Add(session);
                submittedIds.Add(progress.Id);
            }

            await _context.SaveChangesAsync();

            // COACH-04: Notify section reviewers (one per unique coachee)
            try
            {
                var uniqueCoacheeIds = progresses.Select(p => p.CoacheeId).Distinct().ToList();
                var coacheeNames = await _context.Users
                    .Where(u => uniqueCoacheeIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName, u.UserName })
                    .ToListAsync();
                foreach (var cid in uniqueCoacheeIds)
                {
                    var c = coacheeNames.FirstOrDefault(x => x.Id == cid);
                    var cName = c?.FullName ?? c?.UserName ?? cid;
                    await NotifyReviewersAsync(cid, cName);
                }
            }
            catch { /* fail silently */ }

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
        public async Task<IActionResult> DownloadEvidencePdf(int progressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            var progress = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d.ProtonSubKompetensi)
                        .ThenInclude(s => s.ProtonKompetensi)
                            .ThenInclude(k => k.ProtonTrack)
                .FirstOrDefaultAsync(p => p.Id == progressId);

            if (progress == null) return NotFound();

            // Access check (same as Deliverable action)
            bool isCoachee = progress.CoacheeId == user.Id;
            bool hasFullAccess = UserRoles.HasFullAccess(user.RoleLevel);
            bool isSectionScoped = UserRoles.HasSectionAccess(user.RoleLevel);
            bool isCoach = user.RoleLevel == 5;

            if (isCoachee || hasFullAccess)
            {
                // allow
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
                return Forbid();
            }

            // Load latest coaching session
            var session = await _context.CoachingSessions
                .Where(cs => cs.ProtonDeliverableProgressId == progressId)
                .OrderByDescending(cs => cs.CreatedAt)
                .FirstOrDefaultAsync();

            if (session == null) return NotFound();

            // Load data
            var coacheeName = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? "Coachee";

            var coachInfo = await _context.Users
                .Where(u => u.Id == session.CoachId)
                .Select(u => new { u.FullName, u.Position, u.Unit, u.NIP, u.Section, u.RoleLevel })
                .FirstOrDefaultAsync();

            // Get coach role name from Identity
            var coachUser = await _context.Users.FindAsync(session.CoachId);
            var coachRoles = coachUser != null ? await _userManager.GetRolesAsync(coachUser) : new List<string>();
            var coachRoleName = coachRoles.FirstOrDefault() ?? "Coach";

            var track = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.ProtonTrack;
            var trackDisplay = track != null ? $"{track.TrackType} {track.TahunKe}" : "-";
            var kompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "-";
            var subKompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "-";
            var deliverable = progress.ProtonDeliverable?.NamaDeliverable ?? "-";

            // Load logos
            var logoPath = Path.Combine(_env.WebRootPath, "images", "psign-pertamina.png");
            byte[]? logoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null;
            var logo135Path = Path.Combine(_env.WebRootPath, "images", "logo-135.png");
            byte[]? logo135Bytes = System.IO.File.Exists(logo135Path) ? System.IO.File.ReadAllBytes(logo135Path) : null;

            var headerBg = "#005B96";
            string Or(string? val) => string.IsNullOrWhiteSpace(val) ? "-" : val;
            // Case-insensitive + trimmed comparison for checkbox matching
            bool IsMatch(string? current, string match) =>
                !string.IsNullOrWhiteSpace(current) && current.Trim().Equals(match, StringComparison.OrdinalIgnoreCase);

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);

                    // HEADER — logo top-right, tanggal below-left
                    page.Header().PaddingBottom(12).Column(hdrCol =>
                    {
                        if (logoBytes != null && logoBytes.Length > 0)
                            hdrCol.Item().AlignRight().MaxWidth(140).Image(logoBytes);
                        hdrCol.Item().PaddingTop(6).AlignLeft()
                            .Text($"Tanggal Coaching : {session.Date.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"))}").FontSize(10);
                    });

                    // CONTENT — 3 column table
                    page.Content().PaddingBottom(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3); // Detail
                            cols.RelativeColumn(4); // Catatan Coach
                            cols.RelativeColumn(3); // Kesimpulan
                        });

                        // Header row
                        table.Cell().Row(1).Column(1).Background(headerBg).Border(1).Padding(6).AlignCenter()
                            .Text("DETAIL").FontSize(10).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                        table.Cell().Row(1).Column(2).Background(headerBg).Border(1).Padding(6).AlignCenter()
                            .Text("CATATAN COACH").FontSize(10).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                        table.Cell().Row(1).Column(3).Background(headerBg).Border(1).Padding(6).AlignCenter()
                            .Text("KESIMPULAN DARI COACH").FontSize(10).Bold().FontColor(QuestPDF.Helpers.Colors.White);

                        // Content row
                        // LEFT — Detail column (split 1:1 between info and acuan)
                        table.Cell().Row(2).Column(1).Border(1).Column(left =>
                        {
                            // Top half — Detail info
                            left.Item().MinHeight(160).Padding(8).Column(detail =>
                            {
                                void DetailRow(string label, string value)
                                {
                                    detail.Item().PaddingBottom(3).Column(inner =>
                                    {
                                        inner.Item().Text(label).FontSize(10).Bold();
                                        inner.Item().PaddingLeft(4).Text(Or(value)).FontSize(9);
                                    });
                                }

                                DetailRow("Kompetensi", kompetensi);
                                DetailRow("Sub Kompetensi", subKompetensi);
                                DetailRow("Deliverable", deliverable);
                            });

                            // Separator
                            left.Item().LineHorizontal(1).LineColor("#adb5bd");

                            // Bottom half — Acuan
                            left.Item().MinHeight(160).Padding(8).Column(acuan =>
                            {
                                acuan.Item().PaddingBottom(6)
                                    .Text("Acuan").FontSize(10).Bold().Underline();

                                void AcuanRow(string label, string value)
                                {
                                    acuan.Item().PaddingBottom(3).Column(inner =>
                                    {
                                        inner.Item().Text(label).FontSize(8).Bold();
                                        inner.Item().PaddingLeft(4).Text(Or(value)).FontSize(9);
                                    });
                                }

                                AcuanRow("Pedoman", session.AcuanPedoman);
                                AcuanRow("TKO/TKI/TKPA", session.AcuanTko);
                                AcuanRow("Best Practice", session.AcuanBestPractice);
                                AcuanRow("Dokumen", session.AcuanDokumen);
                            });
                        });

                        // CENTER — Catatan Coach
                        table.Cell().Row(2).Column(2).Border(1).Padding(10)
                            .Text(Or(session.CatatanCoach)).FontSize(9).LineHeight(1.4f);

                        // RIGHT — Kesimpulan + Result (2/3) + TTD (1/3)
                        table.Cell().Row(2).Column(3).Border(1).Column(right =>
                        {
                            // Top 2/3 — Kesimpulan + Result
                            right.Item().MinHeight(215).Padding(8).Column(ks =>
                            {
                                // Kesimpulan checkboxes
                                ks.Item().PaddingBottom(10).Column(ksi =>
                                {
                                    ksi.Item().PaddingBottom(3).Text("Kesimpulan:").FontSize(10).Bold();
                                    ksi.Item().Text(t => { t.Span(IsMatch(session.Kesimpulan, "Kompeten secara mandiri") ? "☑ " : "☐ ").FontSize(12); t.Span("Kompeten secara mandiri").FontSize(9); });
                                    ksi.Item().Text(t => { t.Span(IsMatch(session.Kesimpulan, "Masih perlu dikembangkan") ? "☑ " : "☐ ").FontSize(12); t.Span("Masih perlu dikembangkan").FontSize(9); });
                                });

                                // Result checkboxes
                                ks.Item().Column(rsi =>
                                {
                                    rsi.Item().PaddingBottom(3).Text("Result:").FontSize(10).Bold();
                                    rsi.Item().Text(t => { t.Span(IsMatch(session.Result, "Need Improvement") ? "☑ " : "☐ ").FontSize(12); t.Span("Need Improvement").FontSize(9); });
                                    rsi.Item().Text(t => { t.Span(IsMatch(session.Result, "Suitable") ? "☑ " : "☐ ").FontSize(12); t.Span("Suitable").FontSize(9); });
                                    rsi.Item().Text(t => { t.Span(IsMatch(session.Result, "Good") ? "☑ " : "☐ ").FontSize(12); t.Span("Good").FontSize(9); });
                                    rsi.Item().Text(t => { t.Span(IsMatch(session.Result, "Excellence") ? "☑ " : "☐ ").FontSize(12); t.Span("Excellence").FontSize(9); });
                                });
                            });

                            // Separator
                            right.Item().LineHorizontal(1).LineColor("#adb5bd");

                            // Bottom 1/3 — TTD Coach P-Sign
                            right.Item().MinHeight(105).Padding(8).AlignCenter().AlignMiddle()
                                .MaxWidth(200).Border(1).BorderColor("#adb5bd").Padding(6).Column(psign =>
                            {
                                if (logoBytes != null && logoBytes.Length > 0)
                                    psign.Item().AlignCenter().MaxWidth(50).Image(logoBytes);
                                psign.Item().AlignCenter().Text(coachRoleName).FontSize(7).Italic();
                                var coachPosition = Or(coachInfo?.Position);
                                var coachUnit = !string.IsNullOrWhiteSpace(coachInfo?.Unit) ? coachInfo.Unit : Or(coachInfo?.Section);
                                psign.Item().AlignCenter().Text($"{coachPosition} - {coachUnit}").FontSize(7);
                                psign.Item().AlignCenter().Text(coachInfo?.FullName ?? "Coach").FontSize(9).Bold();
                                psign.Item().AlignCenter().Text($"NIP: {Or(coachInfo?.NIP)}").FontSize(8);
                            });
                        });
                    });

                    // FOOTER
                    page.Footer().Height(45).Row(ftr =>
                    {
                        ftr.RelativeItem().Background("#CC0000").Padding(8).AlignLeft().AlignMiddle()
                            .Text("ptkpi.pertamina.com").FontSize(10).FontColor(QuestPDF.Helpers.Colors.White);
                        if (logo135Bytes != null && logo135Bytes.Length > 0)
                            ftr.ConstantItem(50).Image(logo135Bytes, QuestPDF.Infrastructure.ImageScaling.FitArea);
                    });
                });
            });

            var pdfStream = new MemoryStream();
            pdf.GeneratePdf(pdfStream);
            var safeName = System.Text.RegularExpressions.Regex.Replace(coacheeName, @"[^a-zA-Z0-9]", "_");
            var safeDeliverable = System.Text.RegularExpressions.Regex.Replace(deliverable, @"[^a-zA-Z0-9]", "_");
            var filename = $"Evidence_{safeName}_{safeDeliverable}_{session.Date:yyyy-MM-dd}.pdf";
            return File(pdfStream.ToArray(), "application/pdf", filename);
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

            // --- Phase 127: Role-scoped coachee IDs via active ProtonTrackAssignment ---
            List<string> scopedCoacheeIds;

            if (userLevel <= 3) // HC/Admin/Direktur/VP/Manager — coachees with any assignment
            {
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Select(a => a.CoacheeId).Distinct().ToListAsync();
            }
            else if (userLevel == 4) // SectionHead/SrSpv — coachees with assignment in same section
            {
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Join(_context.Users, a => a.CoacheeId, u => u.Id, (a, u) => new { a.CoacheeId, u.Section })
                    .Where(x => x.Section == user.Section)
                    .Select(x => x.CoacheeId).Distinct().ToListAsync();
            }
            else if (userLevel == 5) // Coach — mapped coachees with assignments
            {
                var mappedIds = await _context.CoachCoacheeMappings
                    .Where(m => m.CoachId == user.Id && m.IsActive)
                    .Select(m => m.CoacheeId).ToListAsync();
                scopedCoacheeIds = await _context.ProtonTrackAssignments
                    .Where(a => mappedIds.Contains(a.CoacheeId))
                    .Select(a => a.CoacheeId).Distinct().ToListAsync();
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