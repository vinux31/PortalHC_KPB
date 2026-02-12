using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Controllers
{
    [Authorize]
    public class CMPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public CMPController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
        public async Task<IActionResult> Mapping()
        {
            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var cpdpData = await _context.CpdpItems
                .OrderBy(c => c.No)
                .ToListAsync();

            return View(cpdpData);
        }
        // --- HALAMAN 3: ASSESSMENT LOBBY ---
        public async Task<IActionResult> Assessment()
        {
            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? "";
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();

            // ✅ QUERY FROM DATABASE instead of hardcoded data
            var exams = await _context.AssessmentSessions
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.Schedule)
                .ToListAsync();

            // ========== VIEW-BASED FILTERING FOR ADMIN ==========
            if (userRole == UserRoles.Admin && !string.IsNullOrEmpty(user?.SelectedView))
            {
                if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
                {
                    // Show personal assessments only
                    return View(exams);
                }
                else if (user.SelectedView == "Atasan" && !string.IsNullOrEmpty(user.Section))
                {
                    // Show assessments from user's section
                    var section = user.Section;

                    // Get all user IDs in the section
                    var teamUserIds = await _context.Users
                        .Where(u => u.Section == section)
                        .Select(u => u.Id)
                        .ToListAsync();

                    var teamExams = await _context.AssessmentSessions
                        .Include(a => a.User)
                        .Where(a => teamUserIds.Contains(a.UserId))
                        .OrderBy(a => a.Schedule)
                        .ToListAsync();
                    return View(teamExams);
                }
                else if (user.SelectedView == "HC")
                {
                    // Show all assessments - already queried above
                    return View(exams);
                }
            }

            return View(exams);
        }

        // --- HALAMAN 5: CREATE ASSESSMENT (NEW) ---
        // GET: Tampilkan form create assessment
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult CreateAssessment()
        {
            // Get list of users for dropdown
            var users = _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", NIP = u.NIP ?? "" })
                .ToList();

            ViewBag.Users = users;
            return View();
        }

        // POST: Process form submission
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssessment(AssessmentSession model)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                // Reload users dropdown for validation error
                var users = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();

                ViewBag.Users = users;
                return View(model);
            }

            // Get current user for audit trail
            var currentUser = await _userManager.GetUserAsync(User);

            // Set default values
            model.Status = "Open";
            model.Progress = 0;

            // If UserId not provided, use current user
            if (string.IsNullOrEmpty(model.UserId))
            {
                model.UserId = currentUser?.Id ?? "";
            }

            // Add to database
            _context.AssessmentSessions.Add(model);
            await _context.SaveChangesAsync();

            // Log action
            TempData["SuccessMessage"] = $"Assessment '{model.Title}' successfully created for {currentUser?.FullName ?? model.UserId}";

            return RedirectToAction(nameof(Assessment));
        }

        // HALAMAN 4: CAPABILITY BUILDING RECORDS
        public async Task<IActionResult> Records(string? section, string? category, string? search, string? statusFilter, string? isFiltered)
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
            ViewBag.SelectedCategory = category;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = statusFilter;
            ViewBag.IsInitialState = isInitialState;
            
            // Determine if we should show Status column (Filter Mode) or Stats columns (Default Mode)
            bool isFilterMode = !string.IsNullOrEmpty(category);
            ViewBag.IsFilterMode = isFilterMode;

            // Admin (Level 1) dengan SelectedView override - Gunakan view preference
            if (userRole == UserRoles.Admin && !string.IsNullOrEmpty(user?.SelectedView))
            {
                // Jika Admin memilih view Coach/Coachee, tampilkan personal records
                if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
                {
                    var personalRecords = await GetPersonalTrainingRecords(user.Id);
                    return View("Records", personalRecords);
                }
                // Untuk HC/Atasan view, lanjut ke worker list (existing logic)
            }

            // Role: Level 5-6 (Coach/Coachee) - Show personal training records
            if (UserRoles.IsCoachingRole(userLevel))
            {
                var personalRecords = await GetPersonalTrainingRecords(user?.Id ?? "");
                return View("Records", personalRecords);
            }
            
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
                workers = await GetWorkersInSection(section, null, category, search, statusFilter);
            }

            return View("RecordsWorkerList", workers);
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

            // Get worker's training records
            var trainingRecords = await GetPersonalTrainingRecords(workerId);

            return View("WorkerDetail", trainingRecords);
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

        // Helper method: Get all workers in a section (with optional filters)
        private async Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null)
        {
            // ✅ QUERY USERS FROM DATABASE
            var usersQuery = _context.Users.AsQueryable();
            
            // 0. FILTER BY SECTION
            if (!string.IsNullOrEmpty(section))
            {
                usersQuery = usersQuery.Where(u => u.Section == section);
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
            
            // 2. BUILD WORKER STATUS LIST WITH TRAINING STATISTICS
            var workerList = new List<WorkerTrainingStatus>();
            
            foreach (var user in users)
            {
                // Get all training records for this user
                var trainingRecords = await _context.TrainingRecords
                    .Where(tr => tr.UserId == user.Id)
                    .ToListAsync();
                
                // Calculate statistics
                var totalTrainings = trainingRecords.Count;
                var completedTrainings = trainingRecords.Count(tr => 
                    tr.Status == "Passed" || tr.Status == "Valid" || tr.Status == "Permanent"
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
                    TrainingRecords = trainingRecords
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
        

    }
}