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
        // --- HALAMAN 3: ASSESSMENT LOBBY ---
        // --- HALAMAN 3: ASSESSMENT LOBBY ---
        public async Task<IActionResult> Assessment(string? search, string? view, int page = 1, int pageSize = 20)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;  // Max 100 per page

            // Get current user and role
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? "";
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();

            // Default view mode
            if (string.IsNullOrEmpty(view))
            {
                view = "personal";
            }

            // Authorization check for manage view
            if (view == "manage" && userRole != UserRoles.Admin && userRole != "HC")
            {
                // Non-admin/HC trying to access manage view - redirect to personal
                return RedirectToAction("Assessment", new { view = "personal" });
            }

            // Base Query
            var query = _context.AssessmentSessions
                .Include(a => a.User)
                .AsQueryable();

            // ========== VIEW-BASED FILTERING ==========
            if (view == "manage")
            {
                // Manage View: Show ALL assessments (HC/Admin only)
                // No filtering needed - show everything
            }
            else // view == "personal"
            {
                // Personal View: Show only user's own assessments
                query = query.Where(a => a.UserId == userId);
            }

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

            // Pagination info for view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            // View mode info
            ViewBag.ViewMode = view;
            ViewBag.UserRole = userRole;
            ViewBag.CanManage = (userRole == UserRoles.Admin || userRole == "HC");

            return View(exams);
        }

        // --- EDIT ASSESSMENT ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("Assessment", new { view = "manage" });
            }

            // Get list of users for potential reassignment (optional feature)
            var users = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;

            return View(assessment);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, AssessmentSession model)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("Assessment", new { view = "manage" });
            }

            // Prevent editing completed assessments (optional - you can remove this if needed)
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "Cannot edit completed assessments.";
                return RedirectToAction("Assessment", new { view = "manage" });
            }

            // Update only allowed fields
            assessment.Title = model.Title;
            assessment.Category = model.Category;
            assessment.Schedule = model.Schedule;
            assessment.DurationMinutes = model.DurationMinutes;
            assessment.Status = model.Status;
            assessment.BannerColor = model.BannerColor;
            assessment.IsTokenRequired = model.IsTokenRequired;

            // Update token if token is required
            if (model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken))
            {
                assessment.AccessToken = model.AccessToken.ToUpper();
            }
            else if (!model.IsTokenRequired)
            {
                assessment.AccessToken = "";
            }

            assessment.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.AssessmentSessions.Update(assessment);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Assessment '{assessment.Title}' has been updated successfully.";
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
                logger.LogError(ex, "Error updating assessment");
                TempData["Error"] = $"Failed to update assessment: {ex.Message}";
            }

            return RedirectToAction("Assessment", new { view = "manage" });
        }

        // --- DELETE ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessment(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();

            try
            {
                var assessment = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(a => a.Responses)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assessment == null)
                {
                    logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
                    return Json(new { success = false, message = "Assessment not found." });
                }

                var assessmentTitle = assessment.Title;
                logger.LogInformation($"Attempting to delete assessment {id}: {assessmentTitle}");

                // Delete in correct order to avoid FK constraint violations
                // 1. Delete UserResponses first
                if (assessment.Responses.Any())
                {
                    logger.LogInformation($"Deleting {assessment.Responses.Count} user responses");
                    _context.UserResponses.RemoveRange(assessment.Responses);
                }

                // 2. Delete Options (child of Questions)
                if (assessment.Questions.Any())
                {
                    var allOptions = assessment.Questions.SelectMany(q => q.Options).ToList();
                    if (allOptions.Any())
                    {
                        logger.LogInformation($"Deleting {allOptions.Count} question options");
                        _context.AssessmentOptions.RemoveRange(allOptions);
                    }

                    // 3. Delete Questions
                    logger.LogInformation($"Deleting {assessment.Questions.Count} questions");
                    _context.AssessmentQuestions.RemoveRange(assessment.Questions);
                }

                // 4. Finally delete the assessment itself
                _context.AssessmentSessions.Remove(assessment);

                await _context.SaveChangesAsync();

                logger.LogInformation($"Successfully deleted assessment {id}: {assessmentTitle}");
                return Json(new { success = true, message = $"Assessment '{assessmentTitle}' has been deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error deleting assessment {id}: {ex.Message}");
                return Json(new { success = false, message = $"Failed to delete assessment: {ex.Message}" });
            }
        }

        // --- REGENERATE TOKEN ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateToken(int id)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                return Json(new { success = false, message = "Assessment not found." });
            }

            if (!assessment.IsTokenRequired)
            {
                return Json(new { success = false, message = "This assessment does not require a token." });
            }

            try
            {
                // Generate new token
                assessment.AccessToken = GenerateSecureToken();
                assessment.UpdatedAt = DateTime.UtcNow;

                _context.AssessmentSessions.Update(assessment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, token = assessment.AccessToken, message = "Token regenerated successfully." });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
                logger.LogError(ex, "Error regenerating token");
                return Json(new { success = false, message = $"Failed to regenerate token: {ex.Message}" });
            }
        }

        // --- HALAMAN 5: CREATE ASSESSMENT (NEW) ---
        // GET: Tampilkan form create assessment
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CreateAssessment()
        {
            // Get list of users for dropdown
            var users = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.SelectedUserIds = new List<string>();
            ViewBag.Sections = OrganizationStructure.GetAllSections();

            // Pass created assessment data to view if exists (for success modal)
            if (TempData["CreatedAssessment"] != null)
            {
                ViewBag.CreatedAssessment = TempData["CreatedAssessment"];
            }

            // Pre-populate model with secure token
            var model = new AssessmentSession
            {
                AccessToken = GenerateSecureToken(),
                Schedule = DateTime.Today.AddDays(1)  // Default to tomorrow
            };

            return View(model);
        }

        // POST: Process form submission (multi-user)
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssessment(AssessmentSession model, List<string> UserIds)
        {
            // Remove single UserId from validation since we use UserIds list
            ModelState.Remove("UserId");

            // Handle Token Validation
            if (model.IsTokenRequired)
            {
                // Token is required - validate it
                if (string.IsNullOrWhiteSpace(model.AccessToken))
                {
                    ModelState.AddModelError("AccessToken", "Access Token is required when token security is enabled.");
                }
            }
            else
            {
                // Token is NOT required - remove from validation and clear value
                ModelState.Remove("AccessToken");
                model.AccessToken = "";
            }

            // Validate at least 1 user selected
            if (UserIds == null || UserIds.Count == 0)
            {
                ModelState.AddModelError("UserIds", "Please select at least one user.");
            }

            // Rate limiting: max 50 users per request
            if (UserIds != null && UserIds.Count > 50)
            {
                ModelState.AddModelError("UserIds", "Cannot assign to more than 50 users at once. Please split into multiple batches.");
            }

            // Validate schedule date
            if (model.Schedule < DateTime.Today)
            {
                ModelState.AddModelError("Schedule", "Schedule date cannot be in the past.");
            }

            if (model.Schedule > DateTime.Today.AddYears(2))
            {
                ModelState.AddModelError("Schedule", "Schedule date too far in future (maximum 2 years).");
            }

            // Validate duration
            if (model.DurationMinutes <= 0)
            {
                ModelState.AddModelError("DurationMinutes", "Duration must be greater than 0.");
            }

            if (model.DurationMinutes > 480)
            {
                ModelState.AddModelError("DurationMinutes", "Duration cannot exceed 480 minutes (8 hours).");
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                // Reload users for validation error (must match GET structure)
                var users = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                    .ToListAsync();

                ViewBag.Users = users;
                ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                ViewBag.Sections = OrganizationStructure.GetAllSections();
                return View(model);
            }

            // Check for duplicates (warning, not error)
            if (UserIds != null && UserIds.Any())
            {
                var existingAssessments = await _context.AssessmentSessions
                    .Where(a => UserIds.Contains(a.UserId)
                             && a.Title == model.Title
                             && a.Category == model.Category
                             && a.Schedule.Date == model.Schedule.Date)
                    .Include(a => a.User)
                    .Select(a => a.User.FullName)
                    .ToListAsync();

                if (existingAssessments.Any())
                {
                    TempData["Warning"] = $"Similar assessments already exist for: {string.Join(", ", existingAssessments.Take(5))}. Proceeding will create duplicates.";
                }
            }

            // Ensure Token is uppercase
            if (model.IsTokenRequired && !string.IsNullOrEmpty(model.AccessToken))
            {
                model.AccessToken = model.AccessToken.ToUpper();
            }
            else
            {
                model.AccessToken = "";
            }

            // Get current user for audit trail
            var currentUser = await _userManager.GetUserAsync(User);

            // Set default values
            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "Open";
            }

            // Create one AssessmentSession per selected user
            var createdSessions = new List<object>();

            try
            {
                // Prefetch all users at once (fix N+1 query)
                var userDictionary = await _context.Users
                    .Where(u => UserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Validate all UserIds exist
                var missingUsers = UserIds.Except(userDictionary.Keys).ToList();
                if (missingUsers.Any())
                {
                    TempData["Error"] = $"Invalid user IDs: {string.Join(", ", missingUsers)}";
                    // Reload form
                    var users = await _context.Users
                        .OrderBy(u => u.FullName)
                        .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                        .ToListAsync();
                    ViewBag.Users = users;
                    ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                    ViewBag.Sections = OrganizationStructure.GetAllSections();
                    return View(model);
                }

                // Create all sessions in memory first
                var sessions = new List<AssessmentSession>();

                foreach (var userId in UserIds)
                {
                    var session = new AssessmentSession
                    {
                        Title = model.Title,
                        Category = model.Category,
                        Schedule = model.Schedule,
                        DurationMinutes = model.DurationMinutes,
                        Status = model.Status,
                        BannerColor = model.BannerColor,
                        IsTokenRequired = model.IsTokenRequired,
                        AccessToken = model.AccessToken,
                        Progress = 0,
                        UserId = userId,
                        CreatedBy = currentUser?.Id
                    };

                    sessions.Add(session);
                }

                // Add all sessions
                _context.AssessmentSessions.AddRange(sessions);

                // Single SaveChanges with transaction (atomicity)
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Populate createdSessions with IDs after save
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        var session = sessions[i];
                        var assignedUser = userDictionary[session.UserId];
                        createdSessions.Add(new
                        {
                            Id = session.Id,
                            UserId = session.UserId,
                            UserName = assignedUser.FullName ?? session.UserId,
                            UserEmail = assignedUser.Email ?? ""
                        });
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log error
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
                logger.LogError(ex, "Error creating assessment sessions");

                // Show error to user
                TempData["Error"] = $"Failed to create assessments: {ex.Message}";

                // Reload form
                var users = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                    .ToListAsync();
                ViewBag.Users = users;
                ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                ViewBag.Sections = OrganizationStructure.GetAllSections();
                return View(model);
            }

            // Serialize batch data for success popup
            TempData["CreatedAssessment"] = System.Text.Json.JsonSerializer.Serialize(new
            {
                Count = createdSessions.Count,
                Title = model.Title,
                Category = model.Category,
                Schedule = model.Schedule.ToString("dd MMMM yyyy"),
                DurationMinutes = model.DurationMinutes,
                Status = model.Status,
                IsTokenRequired = model.IsTokenRequired,
                AccessToken = model.AccessToken,
                Sessions = createdSessions
            });

            return RedirectToAction(nameof(CreateAssessment));
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
            if (userRole == UserRoles.Admin)
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
            // ✅ QUERY USERS FROM DATABASE WITH TRAINING RECORDS (Fix N+1)
            var usersQuery = _context.Users
                .Include(u => u.TrainingRecords)  // Load related data in single query
                .AsQueryable();

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
                // Training records already loaded via Include
                var trainingRecords = user.TrainingRecords.ToList();
                
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
                return Json(new { success = true, redirectUrl = Url.Action("StartExam", new { id = assessment.Id }) });
            }

            if (string.IsNullOrEmpty(token) || assessment.AccessToken != token.ToUpper())
            {
                return Json(new { success = false, message = "Invalid Token. Please check and try again." });
            }

            // Token Valid -> Redirect to Exam
            return Json(new { success = true, redirectUrl = Url.Action("StartExam", new { id = assessment.Id }) });
        }

        // --- HALAMAN EXAM SKELETON ---
        public async Task<IActionResult> StartExam(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (assessment == null) return NotFound();

            // Verification: Ensure User is the owner (or Admin/HC)
            var user = await _userManager.GetUserAsync(User);
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
            {
                return Forbid();
            }

            // Prevent re-taking completed assessments
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "This assessment has already been completed.";
                return RedirectToAction("Assessment");
            }

            return View(assessment);
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

            int totalScore = 0;
            int maxScore = 0;

            // Process Answers
            foreach (var question in assessment.Questions)
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

                // Save User Response
                _context.UserResponses.Add(new UserResponse
                {
                    AssessmentSessionId = id,
                    AssessmentQuestionId = question.Id,
                    SelectedOptionId = selectedOptionId
                });
            }

            // Calculate Grade (0-100 scale if needed, or raw score)
            // For now, let's store the raw score sum or percentage? 
            // Model.Score is int, usually 0-100 logic is preferred.
            int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

            assessment.Score = finalPercentage;
            assessment.Status = "Completed";
            assessment.Progress = 100;

            _context.AssessmentSessions.Update(assessment);
            await _context.SaveChangesAsync();

            // Redirect to Results Page (or back to list for now)
            return RedirectToAction("Assessment");
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
