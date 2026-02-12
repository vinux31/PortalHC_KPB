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

        public CDPController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string? bagian = null, string? unit = null, string? level = null)
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
            
            var model = new DashboardViewModel
            {
                TotalIdp = totalIdp,
                IdpGrowth = 12, // Keep mock for now (needs historical data)
                CompletionRate = completionRate,
                CompletionTarget = "80% (Q4)",
                PendingAssessments = pendingAssessments,
                BudgetUsedPercent = 45, // Keep mock (no budget table yet)
                BudgetUsedText = "Rp 450jt / 1M",
                
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

        public async Task<IActionResult> Coaching()
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            var userId = user?.Id ?? "";

            // ========== VIEW-BASED FILTERING FOR ADMIN ==========
            if (userRole == UserRoles.Admin)
            {
                if (user.SelectedView == "Coach")
                {
                    // Show coachee selector for Coach view
                    ViewBag.ShowCoacheeSelector = true;

                    // Get list of coachees in user's section
                    var coacheeList = await _context.Users
                        .Where(u => u.Section == user.Section)
                        .ToListAsync();
                    ViewBag.CoacheeList = coacheeList;
                }
            }
            // For non-admin or admin without specific view, use existing logic

            // Query coaching logs from database where user is coach or coachee
            var history = await _context.CoachingLogs
                .Where(c => c.CoachId == userId || c.CoacheeId == userId)
                .OrderByDescending(c => c.Tanggal)
                .ToListAsync();

            return View(history);
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