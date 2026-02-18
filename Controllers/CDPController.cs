using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;

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

                var protonViewModel = new ProtonPlanViewModel
                {
                    TrackType = assignment.TrackType,
                    TahunKe = assignment.TahunKe,
                    KompetensiList = kompetensiList,
                    ActiveProgress = activeProgress
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

        public async Task<IActionResult> Dashboard()
        {
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            var userId = user?.Id ?? "";

            // Base query
            var baseQuery = _context.IdpItems.AsQueryable();

            // ========== VIEW-BASED FILTERING FOR ADMIN ==========
            if (userRole == UserRoles.Admin)
            {
                if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
                {
                    // Show personal stats only
                    baseQuery = baseQuery.Where(i => i.UserId == userId);
                }
                else if (user.SelectedView == "Atasan" && !string.IsNullOrEmpty(user.Section))
                {
                    // Show stats from user's section
                    var section = user.Section;

                    // Get all user IDs in the section
                    var userIdsInSection = await _context.Users
                        .Where(u => u.Section == section)
                        .Select(u => u.Id)
                        .ToListAsync();
                    baseQuery = baseQuery.Where(i => userIdsInSection.Contains(i.UserId));
                }
                // HC view: no filter (show all)
            }
            // For non-admin or admin without specific view, use existing logic (calculate from baseQuery if filtered, otherwise use global stats)

            // ========== CALCULATE STATISTICS ==========
            var totalIdp = await baseQuery.CountAsync();

            var completedIdp = await baseQuery.CountAsync(i => i.ApproveSrSpv == "Approved" &&
                                 i.ApproveSectionHead == "Approved" &&
                                 i.ApproveHC == "Approved");
            
            var completionRate = totalIdp > 0 
                ? (int)((double)completedIdp / totalIdp * 100) 
                : 0;
            
            var pendingAssessments = await _context.AssessmentSessions
                .CountAsync(a => a.Status == "Open" || a.Status == "Upcoming");

            // Assessment summary for quick link widget
            var completedAssessments = await _context.AssessmentSessions
                .Where(a => a.Status == "Completed")
                .CountAsync();

            var assessmentPassRate = completedAssessments > 0
                ? await _context.AssessmentSessions
                    .Where(a => a.Status == "Completed")
                    .CountAsync(a => a.IsPassed == true) * 100.0 / completedAssessments
                : 0;

            var totalUsersAssessed = await _context.AssessmentSessions
                .Where(a => a.Status == "Completed")
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            var model = new DashboardViewModel
            {
                TotalIdp = totalIdp,
                IdpGrowth = 12, // Keep mock for now (needs historical data)
                CompletionRate = completionRate,
                CompletionTarget = "80% (Q4)",
                PendingAssessments = pendingAssessments,
                BudgetUsedPercent = 45, // Keep mock (no budget table yet)
                BudgetUsedText = "Rp 450jt / 1M",

                // Assessment summary
                TotalCompletedAssessments = completedAssessments,
                OverallPassRate = Math.Round(assessmentPassRate, 1),
                TotalUsersAssessed = totalUsersAssessed,

                // Chart Data (Jan - Jun) - keep mock for now (needs time-series data)
                ChartLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                ChartTarget = new List<int> { 100, 100, 120, 120, 150, 150 },
                ChartRealization = new List<int> { 95, 110, 115, 140, 145, 160 },

                // Compliance Data - keep mock for now (needs unit-level aggregation)
                TopUnits = new List<UnitCompliance>
                {
                    new UnitCompliance { UnitName = "SRU Unit", Percentage = 95, ColorClass = "bg-success" },
                    new UnitCompliance { UnitName = "RFCC Unit", Percentage = 92, ColorClass = "bg-success" },
                    new UnitCompliance { UnitName = "Utilities", Percentage = 88, ColorClass = "bg-primary" },
                    new UnitCompliance { UnitName = "Maintenance", Percentage = 74, ColorClass = "bg-warning" },
                    new UnitCompliance { UnitName = "Procurement", Percentage = 40, ColorClass = "bg-danger" }
                }
            };

            return View(model);
        }

        public async Task<IActionResult> Coaching(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userId = user.Id;

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            var query = _context.CoachingSessions
                .Include(s => s.ActionItems)
                .AsQueryable();

            // ========== ROLE-BASED FILTERING ==========
            if (userRole == UserRoles.Admin)
            {
                if (user.SelectedView == "Coachee")
                {
                    // Admin acting as Coachee: see sessions about them
                    query = query.Where(s => s.CoacheeId == userId);
                }
                else if (user.SelectedView == "Coach")
                {
                    // Admin acting as Coach: see sessions they coach
                    query = query.Where(s => s.CoachId == userId);
                }
                else if (user.SelectedView == "Atasan")
                {
                    // Admin acting as Atasan: see sessions they coach
                    query = query.Where(s => s.CoachId == userId);
                }
                // HC view: no filter — see all sessions
            }
            else if (userRole == UserRoles.Coach ||
                     userRole == UserRoles.SrSupervisor ||
                     userRole == UserRoles.SectionHead ||
                     userRole == UserRoles.HC ||
                     userRole == UserRoles.Manager ||
                     userRole == UserRoles.VP ||
                     userRole == UserRoles.Direktur)
            {
                // Coach and management roles see sessions they conduct
                query = query.Where(s => s.CoachId == userId);
            }
            else
            {
                // Coachee and others see sessions about them
                query = query.Where(s => s.CoacheeId == userId);
            }

            // Apply date range and status filters
            if (fromDate.HasValue) query = query.Where(s => s.Date >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(s => s.Date <= toDate.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);

            var sessions = await query.OrderByDescending(s => s.Date).ToListAsync();

            // Build coachee list for create form dropdown (Coach and Admin non-Coachee views only)
            List<ApplicationUser> coacheeList = new();
            bool isCoach = false;

            if (userRole == UserRoles.Coach ||
                userRole == UserRoles.SrSupervisor ||
                userRole == UserRoles.SectionHead ||
                userRole == UserRoles.HC ||
                userRole == UserRoles.Manager ||
                userRole == UserRoles.VP ||
                userRole == UserRoles.Direktur)
            {
                isCoach = true;
                coacheeList = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else if (userRole == UserRoles.Admin && user.SelectedView != "Coachee")
            {
                isCoach = true;
                coacheeList = await _context.Users
                    .Where(u => u.Section == user.Section && u.RoleLevel == 6)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            // Collect all unique user IDs from sessions for name display (batch query — avoids N+1)
            var allUserIds = sessions.SelectMany(s => new[] { s.CoachId, s.CoacheeId })
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var userNames = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);

            var viewModel = new CoachingHistoryViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                StatusFilter = status,
                Sessions = sessions
            };

            var kompetensiList = await _context.KkjMatrices
                .Select(k => k.Kompetensi)
                .Distinct()
                .OrderBy(k => k)
                .ToListAsync();
            ViewBag.KompetensiList = kompetensiList;

            ViewBag.CoacheeList = coacheeList;
            ViewBag.UserRole = userRole;
            ViewBag.IsCoach = isCoach;
            ViewBag.UserNames = userNames;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSession(CreateSessionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only coaching roles can create sessions
            if (user.RoleLevel > 5)
            {
                return Forbid();
            }

            ModelState.Remove("CoachId"); // Set server-side
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Form tidak valid. Periksa kembali isian Anda.";
                return RedirectToAction("Coaching");
            }

            var session = new CoachingSession
            {
                CoachId = user.Id,
                CoacheeId = model.CoacheeId,
                Date = model.Date,
                Kompetensi = model.Kompetensi,
                SubKompetensi = model.SubKompetensi,
                Deliverable = model.Deliverable,
                CoacheeCompetencies = model.CoacheeCompetencies,
                CatatanCoach = model.CatatanCoach,
                Kesimpulan = model.Kesimpulan,
                Result = model.Result,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            _context.CoachingSessions.Add(session);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sesi coaching berhasil dicatat.";
            return RedirectToAction("Coaching");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddActionItem(int sessionId, AddActionItemViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Verify the session belongs to this coach
            var session = await _context.CoachingSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CoachId == user.Id);
            if (session == null) return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Form tidak valid. Periksa kembali isian Anda.";
                return RedirectToAction("Coaching");
            }

            var item = new ActionItem
            {
                CoachingSessionId = sessionId,
                Description = model.Description,
                DueDate = model.DueDate,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _context.ActionItems.Add(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Action item berhasil ditambahkan.";
            return RedirectToAction("Coaching");
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
                .Where(u => u.Section == user.Section && u.RoleLevel == 6)
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
            bool canApprove = (userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead)
                              && progress.Status == "Submitted";
            bool canHCReview = userRole == UserRoles.HC && progress.HCApprovalStatus == "Pending";

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

            // Only SrSupervisor or SectionHead can approve
            if (userRole != UserRoles.SrSupervisor && userRole != UserRoles.SectionHead)
            {
                return Forbid();
            }

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

            // Section check: coachee must be in same section as approver
            var coacheeUser = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.Section })
                .FirstOrDefaultAsync();
            if (coacheeUser == null || coacheeUser.Section != user.Section)
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

            // Only SrSupervisor or SectionHead can reject
            if (userRole != UserRoles.SrSupervisor && userRole != UserRoles.SectionHead)
            {
                return Forbid();
            }

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

            // Section check: coachee must be in same section as rejector
            var coacheeUser = await _context.Users
                .Where(u => u.Id == progress.CoacheeId)
                .Select(u => new { u.Section })
                .FirstOrDefaultAsync();
            if (coacheeUser == null || coacheeUser.Section != user.Section)
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