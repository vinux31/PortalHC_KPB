using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using HcPortal.Models.Competency;
using HcPortal.Helpers;

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

            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;

            // Authorization check for manage view
            if (view == "manage" && !isHCAccess)
            {
                // Non-admin/HC trying to access manage view - redirect to personal
                return RedirectToAction("Assessment", new { view = "personal" });
            }

            // ========== HC/ADMIN BRANCH: Dual ViewBag data sets ==========
            if (view == "manage" && isHCAccess)
            {
                // Management tab: ALL assessments (CRUD operations)
                var managementQuery = _context.AssessmentSessions
                    .Include(a => a.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    var lowerSearch = search.ToLower();
                    managementQuery = managementQuery.Where(a =>
                        a.Title.ToLower().Contains(lowerSearch) ||
                        a.Category.ToLower().Contains(lowerSearch) ||
                        (a.User != null && (
                            a.User.FullName.ToLower().Contains(lowerSearch) ||
                            (a.User.NIP != null && a.User.NIP.Contains(lowerSearch))
                        ))
                    );
                }

                ViewBag.SearchTerm = search;

                var totalCount = await managementQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var managementData = await managementQuery
                    .OrderByDescending(a => a.Schedule)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageSize = pageSize;
                ViewBag.ManagementData = managementData;

                // Monitoring tab: Open + Upcoming only, sorted by schedule (soonest first)
                var monitorData = await _context.AssessmentSessions
                    .Include(a => a.User)
                    .Where(a => a.Status == "Open" || a.Status == "Upcoming")
                    .OrderBy(a => a.Schedule)
                    .ToListAsync();

                ViewBag.MonitorData = monitorData;

                ViewBag.ViewMode = view;
                ViewBag.UserRole = userRole;
                ViewBag.CanManage = isHCAccess;

                return View(managementData); // Model is Management data; Monitoring is in ViewBag
            }

            // ========== WORKER PERSONAL BRANCH ==========
            var query = _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.UserId == userId);

            // Workers see only actionable assessments — Completed lives in Training Records (/CMP/Records)
            query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming");

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
            var totalCount2 = await query.CountAsync();
            var totalPages2 = (int)Math.Ceiling(totalCount2 / (double)pageSize);

            // Execute Query with pagination
            var exams = await query
                .OrderByDescending(a => a.Schedule)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pagination info for view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages2;
            ViewBag.TotalCount = totalCount2;
            ViewBag.PageSize = pageSize;

            // View mode info
            ViewBag.ViewMode = view;
            ViewBag.UserRole = userRole;
            ViewBag.CanManage = isHCAccess;

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

            // Query sibling sessions: same Title + Category + Schedule.Date (includes the current session)
            var siblings = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == assessment.Title
                         && a.Category == assessment.Category
                         && a.Schedule.Date == assessment.Schedule.Date)
                .ToListAsync();

            var siblingUserIds = siblings
                .Where(a => a.User != null)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            // Build assigned users list for display (read-only)
            ViewBag.AssignedUsers = siblings
                .Where(a => a.User != null)
                .Select(a => new
                {
                    Id = a.Id,
                    FullName = a.User!.FullName ?? "",
                    Email = a.User!.Email ?? "",
                    Section = a.User!.Section ?? ""
                })
                .ToList();

            // Store assigned user IDs so the picker can exclude them
            ViewBag.AssignedUserIds = siblingUserIds;

            // Get list of all users for the picker
            var users = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Sections = OrganizationStructure.GetAllSections();

            return View(assessment);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, AssessmentSession model, List<string> NewUserIds)
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

            // Rate limit: guard before any DB work
            if (NewUserIds != null && NewUserIds.Count > 50)
            {
                TempData["Error"] = "Cannot assign more than 50 users at once. Please split into multiple batches.";
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
            assessment.PassPercentage = model.PassPercentage;
            assessment.AllowAnswerReview = model.AllowAnswerReview;

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
                return RedirectToAction("Assessment", new { view = "manage" });
            }

            // ===== BULK ASSIGN: create new sessions for selected users =====
            if (NewUserIds != null && NewUserIds.Count > 0)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
                try
                {
                    // Re-load the saved assessment to get current field values
                    var savedAssessment = await _context.AssessmentSessions.FindAsync(id);
                    if (savedAssessment != null)
                    {
                        // Query already-assigned sibling user IDs (Title+Category+Schedule.Date match)
                        var existingSiblingUserIds = await _context.AssessmentSessions
                            .Where(a => a.Title == savedAssessment.Title
                                     && a.Category == savedAssessment.Category
                                     && a.Schedule.Date == savedAssessment.Schedule.Date)
                            .Select(a => a.UserId)
                            .Distinct()
                            .ToListAsync();

                        // Filter out already-assigned users to prevent duplicates
                        var filteredNewUserIds = NewUserIds
                            .Where(uid => !existingSiblingUserIds.Contains(uid))
                            .Distinct()
                            .ToList();

                        if (filteredNewUserIds.Count > 0)
                        {
                            // Validate all provided user IDs exist
                            var userDictionary = await _context.Users
                                .Where(u => filteredNewUserIds.Contains(u.Id))
                                .ToDictionaryAsync(u => u.Id);

                            var missingUsers = filteredNewUserIds.Except(userDictionary.Keys).ToList();
                            if (missingUsers.Any())
                            {
                                logger.LogWarning("Bulk assign: invalid user IDs: {Ids}", string.Join(", ", missingUsers));
                                TempData["Error"] = $"Invalid user IDs detected: {string.Join(", ", missingUsers)}";
                                return RedirectToAction("Assessment", new { view = "manage" });
                            }

                            // Get current user for audit
                            var currentUser = await _userManager.GetUserAsync(User);

                            // Build new sessions
                            var newSessions = filteredNewUserIds.Select(uid => new AssessmentSession
                            {
                                Title = savedAssessment.Title,
                                Category = savedAssessment.Category,
                                Schedule = savedAssessment.Schedule,
                                DurationMinutes = savedAssessment.DurationMinutes,
                                Status = savedAssessment.Status,
                                BannerColor = savedAssessment.BannerColor,
                                IsTokenRequired = savedAssessment.IsTokenRequired,
                                AccessToken = savedAssessment.AccessToken,
                                PassPercentage = savedAssessment.PassPercentage,
                                AllowAnswerReview = savedAssessment.AllowAnswerReview,
                                Progress = 0,
                                UserId = uid,
                                CreatedBy = currentUser?.Id
                            }).ToList();

                            _context.AssessmentSessions.AddRange(newSessions);

                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();
                                TempData["Success"] = $"Assessment '{savedAssessment.Title}' has been updated. {newSessions.Count} new user(s) assigned.";
                            }
                            catch
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                        // If filteredNewUserIds is empty (all were already assigned), no error needed — existing success message stands
                    }
                }
                catch (Exception ex)
                {
                    var logger2 = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
                    logger2.LogError(ex, "Error bulk-assigning users to assessment {Id}", id);
                    TempData["Error"] = $"Assessment updated but bulk assign failed: {ex.Message}";
                }
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
                Schedule = DateTime.Today.AddDays(1),  // Default to tomorrow
                PassPercentage = 70,
                AllowAnswerReview = true
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

            // Validate PassPercentage
            if (model.PassPercentage < 0 || model.PassPercentage > 100)
            {
                ModelState.AddModelError("PassPercentage", "Pass Percentage must be between 0 and 100.");
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
                        PassPercentage = model.PassPercentage,
                        AllowAnswerReview = model.AllowAnswerReview,
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

            return RedirectToAction("Assessment", new { view = "manage" });
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

            // Phase 10: Admin always gets HC worker list (elevated access, not personal view)
            // Only literal Coach/Coachee roles see personal unified records
            bool isCoacheeView = userRole == UserRoles.Coach || userRole == UserRoles.Coachee;
            if (isCoacheeView)
            {
                var unified = await GetUnifiedRecords(user!.Id);
                return View("Records", unified);
            }
            // HC, Admin (all SelectedView values), Management, Supervisor -> worker list
            
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

            // Phase 10: Get worker's unified records (assessments + trainings)
            var unified = await GetUnifiedRecords(workerId);

            return View("WorkerDetail", unified);
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
                SortPriority = 1
            }));

            return unified
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.SortPriority)
                .ToList();
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
                .Include(a => a.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options)
                .Include(a => a.Responses)
                    .ThenInclude(r => r.SelectedOption)
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

            var passPercentage = assessment.PassPercentage;
            var score = assessment.Score ?? 0;

            var viewModel = new AssessmentResultsViewModel
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

            return View(viewModel);
        }
        #endregion

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> UserAssessmentHistory(string userId)
        {
            // Load the target user
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (targetUser == null)
            {
                return NotFound();
            }

            // Query completed assessments for this user
            var assessments = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .OrderByDescending(a => a.CompletedAt)
                .Select(a => new AssessmentReportItem
                {
                    Id = a.Id,
                    Title = a.Title,
                    Category = a.Category,
                    UserId = userId,
                    UserName = targetUser.FullName,
                    UserNIP = targetUser.NIP,
                    UserSection = targetUser.Section,
                    Score = a.Score ?? 0,
                    PassPercentage = a.PassPercentage,
                    IsPassed = a.IsPassed ?? false,
                    CompletedAt = a.CompletedAt
                })
                .ToListAsync();

            // Calculate statistics
            var totalAssessments = assessments.Count;
            var passedCount = assessments.Count(a => a.IsPassed);
            var passRate = totalAssessments > 0 ? passedCount * 100.0 / totalAssessments : 0;
            var averageScore = totalAssessments > 0 ? assessments.Average(a => (double)a.Score) : 0;

            // Build ViewModel
            var viewModel = new UserAssessmentHistoryViewModel
            {
                UserId = userId,
                UserFullName = targetUser.FullName,
                UserNIP = targetUser.NIP,
                UserSection = targetUser.Section,
                UserPosition = targetUser.Position,
                TotalAssessments = totalAssessments,
                PassedCount = passedCount,
                PassRate = passRate,
                AverageScore = averageScore,
                Assessments = assessments
            };

            return View(viewModel);
        }

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
