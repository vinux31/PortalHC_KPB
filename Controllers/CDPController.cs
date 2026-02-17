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