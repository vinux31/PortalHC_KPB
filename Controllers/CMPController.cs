using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using HcPortal.Models.Competency;
using HcPortal.Helpers;
using System.Text.Json;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize]
    public class CMPController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;

        public CMPController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            AuditLogService auditLog)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
            _auditLog = auditLog;
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
                // Management tab: ALL assessments (CRUD operations) — projected to avoid loading full User entities
                var managementQuery = _context.AssessmentSessions
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

                // Project only needed fields — no full User entity load
                var allSessions = await managementQuery
                    .OrderByDescending(a => a.Schedule)
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Category,
                        a.Schedule,
                        a.DurationMinutes,
                        a.Status,
                        a.IsTokenRequired,
                        a.AccessToken,
                        a.PassPercentage,
                        a.AllowAnswerReview,
                        a.CreatedAt,
                        UserFullName = a.User != null ? a.User.FullName : "Unknown",
                        UserEmail    = a.User != null ? a.User.Email    : ""
                    })
                    .ToListAsync();

                // Group by (Title, Category, Schedule.Date) — in-memory after projection
                var grouped = allSessions
                    .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                    .Select(g =>
                    {
                        var rep = g.OrderBy(a => a.CreatedAt).First();
                        return new
                        {
                            Title             = rep.Title,
                            Category          = rep.Category,
                            Schedule          = rep.Schedule,
                            DurationMinutes   = rep.DurationMinutes,
                            Status            = rep.Status,
                            IsTokenRequired   = rep.IsTokenRequired,
                            AccessToken       = rep.AccessToken,
                            PassPercentage    = rep.PassPercentage,
                            AllowAnswerReview = rep.AllowAnswerReview,
                            RepresentativeId  = rep.Id,
                            Users    = g.Select(a => new { FullName = a.UserFullName, Email = a.UserEmail }).ToList(),
                            AllIds   = g.Select(a => a.Id).ToList()
                        };
                    })
                    .OrderByDescending(g => g.Schedule)
                    .ToList();

                var totalCount = grouped.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var managementData = grouped
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage    = page;
                ViewBag.TotalPages     = totalPages;
                ViewBag.TotalCount     = totalCount;
                ViewBag.PageSize       = pageSize;
                ViewBag.ManagementData = managementData;

                ViewBag.ViewMode = view;
                ViewBag.UserRole = userRole;
                ViewBag.CanManage = isHCAccess;

                return View(); // ManagementData is in ViewBag; no typed model needed
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

        // --- MONITORING TAB LAZY-LOAD ENDPOINT ---
        [HttpGet]
        public async Task<IActionResult> GetMonitorData()
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            var cutoff = DateTime.UtcNow.AddDays(-30);
            var monitorSessions = await _context.AssessmentSessions
                .Where(a => a.Status == "Open"
                         || a.Status == "InProgress"
                         || a.Status == "Upcoming"
                         || (a.Status == "Completed" && a.CompletedAt != null && a.CompletedAt >= cutoff))
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    a.Status,
                    a.Score,
                    a.IsPassed,
                    a.CompletedAt,
                    UserFullName = a.User != null ? a.User.FullName : "Unknown",
                    UserNIP      = a.User != null ? a.User.NIP      : ""
                })
                .ToListAsync();

            var monitorGroups = monitorSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g =>
                {
                    var sessions = g.Select(a =>
                    {
                        bool isCompleted = a.Score != null || a.CompletedAt != null;
                        return new MonitoringSessionViewModel
                        {
                            Id           = a.Id,
                            UserFullName = a.UserFullName,
                            UserNIP      = a.UserNIP,
                            UserStatus   = isCompleted ? "Completed" : "Not started",
                            Score        = a.Score,
                            IsPassed     = a.IsPassed,
                            CompletedAt  = a.CompletedAt
                        };
                    }).ToList();

                    bool hasOpen     = g.Any(a => a.Status == "Open" || a.Status == "InProgress");
                    bool hasUpcoming = g.Any(a => a.Status == "Upcoming");
                    string groupStatus = hasOpen ? "Open" : hasUpcoming ? "Upcoming" : "Closed";

                    int completedCount = sessions.Count(s => s.UserStatus == "Completed");
                    int passedCount    = sessions.Count(s => s.IsPassed == true);

                    return new MonitoringGroupViewModel
                    {
                        Title          = g.Key.Title,
                        Category       = g.Key.Category,
                        Schedule       = g.First().Schedule,
                        GroupStatus    = groupStatus,
                        TotalCount     = sessions.Count,
                        CompletedCount = completedCount,
                        PassedCount    = passedCount,
                        Sessions       = sessions
                    };
                })
                .OrderBy(g => g.GroupStatus == "Closed" ? 1 : 0)
                .ThenBy(g => g.GroupStatus != "Closed" ? g.Schedule : DateTime.MaxValue)
                .ThenByDescending(g => g.GroupStatus == "Closed" ? g.Schedule : DateTime.MinValue)
                .ToList();

            return Json(monitorGroups);
        }

        // --- ASSESSMENT MONITORING DETAIL ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AssessmentMonitoringDetail(string title, string category, DateTime scheduleDate)
        {
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
            {
                TempData["Error"] = "Assessment group not found.";
                return RedirectToAction("Assessment", new { view = "manage" });
            }

            var sessionViewModels = sessions.Select(a =>
            {
                string userStatus;
                if (a.CompletedAt != null || a.Score != null)
                    userStatus = "Completed";
                else if (a.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (a.StartedAt != null)
                    userStatus = "InProgress";
                else
                    userStatus = "Not started";

                return new MonitoringSessionViewModel
                {
                    Id           = a.Id,
                    UserFullName = a.User?.FullName ?? "Unknown",
                    UserNIP      = a.User?.NIP ?? "",
                    UserStatus   = userStatus,
                    Score        = a.Score,
                    IsPassed     = a.IsPassed,
                    CompletedAt  = a.CompletedAt,
                    StartedAt    = a.StartedAt
                };
            })
            .OrderBy(s => s.UserStatus)   // Not started before Completed
            .ThenBy(s => s.UserFullName)
            .ToList();

            var model = new MonitoringGroupViewModel
            {
                Title    = title,
                Category = category,
                Schedule = sessions.First().Schedule,
                Sessions = sessionViewModels,
                TotalCount     = sessionViewModels.Count,
                CompletedCount = sessionViewModels.Count(s => s.UserStatus == "Completed"),
                PassedCount    = sessionViewModels.Count(s => s.IsPassed == true),
                GroupStatus    = sessions.Any(a => a.Status == "Open" || a.Status == "InProgress") ? "Open"
                               : sessions.Any(a => a.Status == "Upcoming") ? "Upcoming" : "Closed"
            };

            ViewBag.BackUrl = Url.Action("Assessment", "CMP", new { view = "manage" });
            return View(model);
        }

        // --- RESET ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Only reset Completed or Abandoned sessions
            if (assessment.Status != "Completed" && assessment.Status != "Abandoned")
            {
                TempData["Error"] = "Hanya sesi yang telah selesai atau ditinggalkan yang dapat direset.";
                return RedirectToAction("AssessmentMonitoringDetail", new
                {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule
                });
            }

            // 1. Delete UserResponse records for this session (legacy path answers)
            var responses = await _context.UserResponses
                .Where(r => r.AssessmentSessionId == id)
                .ToListAsync();
            if (responses.Any())
                _context.UserResponses.RemoveRange(responses);

            // 1b. Delete PackageUserResponse records for this session (package path answers)
            var packageResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == id)
                .ToListAsync();
            if (packageResponses.Any())
                _context.PackageUserResponses.RemoveRange(packageResponses);

            // 2. Delete UserPackageAssignment for this session (package path)
            //    Deleting ensures the next StartExam assigns a fresh random package.
            var assignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
            if (assignment != null)
                _context.UserPackageAssignments.Remove(assignment);

            // 3. Reset session state to Open
            assessment.Status = "Open";
            assessment.Score = null;
            assessment.IsPassed = null;
            assessment.CompletedAt = null;
            assessment.StartedAt = null;
            assessment.Progress = 0;
            assessment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            var rsUser = await _userManager.GetUserAsync(User);
            var rsActorName = $"{rsUser?.NIP ?? "?"} - {rsUser?.FullName ?? "Unknown"}";
            await _auditLog.LogAsync(
                rsUser?.Id ?? "",
                rsActorName,
                "ResetAssessment",
                $"Reset assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
                id,
                "AssessmentSession");

            TempData["Success"] = "Sesi ujian telah direset. Peserta dapat mengikuti ujian kembali.";
            return RedirectToAction("AssessmentMonitoringDetail", new
            {
                title = assessment.Title,
                category = assessment.Category,
                scheduleDate = assessment.Schedule
            });
        }

        // --- FORCE CLOSE ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceCloseAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Only force-close Open or InProgress sessions
            if (assessment.Status != "Open" && assessment.Status != "InProgress")
            {
                TempData["Error"] = "Force Close hanya dapat dilakukan pada sesi yang berstatus Open atau InProgress.";
                return RedirectToAction("AssessmentMonitoringDetail", new
                {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule
                });
            }

            // Mark as Completed with system score of 0
            assessment.Status = "Completed";
            assessment.Score = 0;
            assessment.IsPassed = false;
            assessment.CompletedAt = DateTime.UtcNow;
            assessment.Progress = 100;
            assessment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            var fcUser = await _userManager.GetUserAsync(User);
            var fcActorName = $"{fcUser?.NIP ?? "?"} - {fcUser?.FullName ?? "Unknown"}";
            await _auditLog.LogAsync(
                fcUser?.Id ?? "",
                fcActorName,
                "ForceCloseAssessment",
                $"Force-closed assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
                id,
                "AssessmentSession");

            TempData["Success"] = "Sesi ujian telah ditutup paksa oleh sistem dengan skor 0.";
            return RedirectToAction("AssessmentMonitoringDetail", new
            {
                title = assessment.Title,
                category = assessment.Category,
                scheduleDate = assessment.Schedule
            });
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
            assessment.ExamWindowCloseDate = model.ExamWindowCloseDate;

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

            // Fetch actor info before try block so it is available for both edit and bulk-assign audit calls
            var editUser = await _userManager.GetUserAsync(User);
            var editActorName = $"{editUser?.NIP ?? "?"} - {editUser?.FullName ?? "Unknown"}";

            try
            {
                _context.AssessmentSessions.Update(assessment);
                await _context.SaveChangesAsync();

                // Audit log — edit
                await _auditLog.LogAsync(
                    editUser?.Id ?? "",
                    editActorName,
                    "EditAssessment",
                    $"Edited assessment '{assessment.Title}' ({assessment.Category}) [ID={id}]",
                    id,
                    "AssessmentSession");

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

                            // Build new sessions (editUser already fetched at outer scope)
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
                                ExamWindowCloseDate = savedAssessment.ExamWindowCloseDate,
                                Progress = 0,
                                UserId = uid,
                                CreatedBy = editUser?.Id
                            }).ToList();

                            _context.AssessmentSessions.AddRange(newSessions);

                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();

                                // Audit log — bulk assign
                                await _auditLog.LogAsync(
                                    editUser?.Id ?? "",
                                    editActorName,
                                    "BulkAssign",
                                    $"Assigned {newSessions.Count} new user(s) to assessment '{savedAssessment.Title}' ({savedAssessment.Category})",
                                    id,
                                    "AssessmentSession");

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

                // Audit log
                try
                {
                    var deleteUser = await _userManager.GetUserAsync(User);
                    var deleteActorName = $"{deleteUser?.NIP ?? "?"} - {deleteUser?.FullName ?? "Unknown"}";
                    await _auditLog.LogAsync(
                        deleteUser?.Id ?? "",
                        deleteActorName,
                        "DeleteAssessment",
                        $"Deleted assessment '{assessmentTitle}' [ID={id}]",
                        id,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id);
                }

                logger.LogInformation($"Successfully deleted assessment {id}: {assessmentTitle}");
                return Json(new { success = true, message = $"Assessment '{assessmentTitle}' has been deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error deleting assessment {id}: {ex.Message}");
                return Json(new { success = false, message = $"Failed to delete assessment: {ex.Message}" });
            }
        }

        // --- DELETE ASSESSMENT GROUP ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentGroup(int representativeId)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();

            try
            {
                // Load representative to get grouping key
                var rep = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(a => a.Id == representativeId);

                if (rep == null)
                {
                    logger.LogWarning($"DeleteAssessmentGroup: representative session {representativeId} not found");
                    return Json(new { success = false, message = "Assessment not found." });
                }

                var scheduleDate = rep.Schedule.Date;

                // Find all siblings (same Title + Category + Schedule.Date)
                var siblings = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(a => a.Responses)
                    .Where(a =>
                        a.Title == rep.Title &&
                        a.Category == rep.Category &&
                        a.Schedule.Date == scheduleDate)
                    .ToListAsync();

                logger.LogInformation($"DeleteAssessmentGroup: deleting {siblings.Count} sessions for '{rep.Title}'");

                foreach (var session in siblings)
                {
                    if (session.Responses.Any())
                        _context.UserResponses.RemoveRange(session.Responses);

                    if (session.Questions.Any())
                    {
                        var opts = session.Questions.SelectMany(q => q.Options).ToList();
                        if (opts.Any()) _context.AssessmentOptions.RemoveRange(opts);
                        _context.AssessmentQuestions.RemoveRange(session.Questions);
                    }

                    _context.AssessmentSessions.Remove(session);
                }

                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var dgUser = await _userManager.GetUserAsync(User);
                    var dgActorName = $"{dgUser?.NIP ?? "?"} - {dgUser?.FullName ?? "Unknown"}";
                    await _auditLog.LogAsync(
                        dgUser?.Id ?? "",
                        dgActorName,
                        "DeleteAssessmentGroup",
                        $"Deleted assessment group '{rep.Title}' ({rep.Category}) — {siblings.Count} session(s)",
                        representativeId,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessmentGroup {Id}", representativeId);
                }

                logger.LogInformation($"DeleteAssessmentGroup: successfully deleted group '{rep.Title}'");
                return Json(new { success = true, message = $"Assessment '{rep.Title}' and all {siblings.Count} assignment(s) deleted." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"DeleteAssessmentGroup error for representative {representativeId}: {ex.Message}");
                return Json(new { success = false, message = $"Failed to delete assessment group: {ex.Message}" });
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

            // ExamWindowCloseDate is optional — remove from ModelState to prevent accidental validation failure
            ModelState.Remove("ExamWindowCloseDate");

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
                        ExamWindowCloseDate = model.ExamWindowCloseDate,
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

                    // Audit log
                    var actorName = $"{currentUser?.NIP ?? "?"} - {currentUser?.FullName ?? "Unknown"}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        actorName,
                        "CreateAssessment",
                        $"Created assessment '{model.Title}' ({model.Category}) scheduled {model.Schedule:yyyy-MM-dd} for {sessions.Count} user(s)",
                        sessions.FirstOrDefault()?.Id,
                        "AssessmentSession");

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
        
        // Phase 19: HC Create Training Record — GET
        [HttpGet]
        public async Task<IActionResult> CreateTrainingRecord()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Load ALL workers system-wide (not section-filtered) for dropdown
            var workers = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.NIP })
                .ToListAsync();

            ViewBag.Workers = workers.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = w.Id,
                Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
            }).ToList();

            return View(new CreateTrainingRecordViewModel());
        }

        // Phase 19: HC Create Training Record — POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainingRecord(CreateTrainingRecordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Validate file if provided
            string? sertifikatUrl = null;
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("CertificateFile", "Hanya file PDF, JPG, dan PNG yang diperbolehkan.");
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("CertificateFile", "Ukuran file maksimal 10MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Re-populate workers dropdown
                var workers = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, u.FullName, u.NIP })
                    .ToListAsync();
                ViewBag.Workers = workers.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = w.Id,
                    Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
                }).ToList();
                return View(model);
            }

            // Handle file upload
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "certificates");
                Directory.CreateDirectory(uploadDir);
                var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(model.CertificateFile.FileName)}";
                var filePath = Path.Combine(uploadDir, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CertificateFile.CopyToAsync(stream);
                }
                sertifikatUrl = $"/uploads/certificates/{safeFileName}";
            }

            // Create record
            var record = new TrainingRecord
            {
                UserId = model.UserId,
                Judul = model.Judul,
                Penyelenggara = model.Penyelenggara,
                Kota = model.Kota,
                Kategori = model.Kategori,
                Tanggal = model.Tanggal,
                TanggalMulai = model.TanggalMulai,
                TanggalSelesai = model.TanggalSelesai,
                Status = model.Status,
                NomorSertifikat = model.NomorSertifikat,
                ValidUntil = model.ValidUntil,
                CertificateType = model.CertificateType,
                SertifikatUrl = sertifikatUrl
            };

            _context.TrainingRecords.Add(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Training record berhasil dibuat.";
            return RedirectToAction("Records", new { isFiltered = "true" });
        }

        // Phase 20: HC Edit Training Record — POST only (no GET; modal is pre-populated inline via Razor in WorkerDetail.cshtml)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainingRecord(EditTrainingRecordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            // Validate file if provided
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Hanya file PDF, JPG, dan PNG yang diperbolehkan.";
                    return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Ukuran file maksimal 10MB.";
                    return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
                }
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
            }

            var record = await _context.TrainingRecords.FindAsync(model.Id);
            if (record == null) return NotFound();

            // Handle file upload — replace old file if new file provided
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(record.SertifikatUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // Save new file with timestamp prefix
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "certificates");
                Directory.CreateDirectory(uploadDir);
                var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(model.CertificateFile.FileName)}";
                var filePath = Path.Combine(uploadDir, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CertificateFile.CopyToAsync(stream);
                }
                record.SertifikatUrl = $"/uploads/certificates/{safeFileName}";
            }
            // Else: keep record.SertifikatUrl unchanged

            // Update all editable fields — UserId (worker) is intentionally NOT updated
            record.Judul = model.Judul;
            record.Penyelenggara = model.Penyelenggara;
            record.Kota = model.Kota;
            record.Kategori = model.Kategori;
            record.Tanggal = model.Tanggal;
            record.TanggalMulai = model.TanggalMulai;
            record.TanggalSelesai = model.TanggalSelesai;
            record.Status = model.Status;
            record.NomorSertifikat = model.NomorSertifikat;
            record.ValidUntil = model.ValidUntil;
            record.CertificateType = model.CertificateType;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Training record berhasil diperbarui.";
            return RedirectToAction("WorkerDetail", new { workerId = model.WorkerId, name = model.WorkerName });
        }

        // Phase 20: HC Delete Training Record — POST only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainingRecord(int id, string workerId, string workerName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();
            bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
            if (!isHCAccess) return Forbid();

            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();

            // Delete certificate file from disk if it exists
            if (!string.IsNullOrEmpty(record.SertifikatUrl))
            {
                var path = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.TrainingRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Training record berhasil dihapus.";
            return RedirectToAction("WorkerDetail", new { workerId = workerId, name = workerName });
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
                SertifikatUrl = t.SertifikatUrl,
                SortPriority = 1,
                TrainingRecordId = t.Id,
                Kategori = t.Kategori,
                Kota = t.Kota,
                NomorSertifikat = t.NomorSertifikat,
                TanggalMulai = t.TanggalMulai,
                TanggalSelesai = t.TanggalSelesai
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
                TempData[$"TokenVerified_{assessment.Id}"] = true;
                return Json(new { success = true, redirectUrl = Url.Action("StartExam", new { id = assessment.Id }) });
            }

            if (string.IsNullOrEmpty(token) || assessment.AccessToken != token.ToUpper())
            {
                return Json(new { success = false, message = "Invalid Token. Please check and try again." });
            }

            // Token Valid -> Redirect to Exam
            TempData[$"TokenVerified_{assessment.Id}"] = true;
            return Json(new { success = true, redirectUrl = Url.Action("StartExam", new { id = assessment.Id }) });
        }

        // --- HALAMAN EXAM SKELETON ---
        [HttpGet]
        public async Task<IActionResult> StartExam(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();

            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "This assessment has already been completed.";
                return RedirectToAction("Assessment");
            }

            // Enforce token requirement — workers must verify token via Assessment lobby first (SEC-01)
            // HC and Admin bypass: they are not exam takers; they may access StartExam for debugging/monitoring
            // InProgress sessions bypass: token was checked on first entry; reload must not block the worker
            if (assessment.IsTokenRequired && assessment.UserId == user.Id && assessment.StartedAt == null)
            {
                var tokenVerified = TempData[$"TokenVerified_{id}"];
                if (tokenVerified == null)
                {
                    TempData["Error"] = "Ujian ini membutuhkan token akses. Silakan masukkan token terlebih dahulu.";
                    return RedirectToAction("Assessment");
                }
            }

            // Enforce exam window close date (LIFE-02 / DATA-03)
            if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow > assessment.ExamWindowCloseDate.Value)
            {
                TempData["Error"] = "Ujian sudah ditutup. Waktu ujian telah berakhir.";
                return RedirectToAction("Assessment");
            }

            // Block re-entry of Abandoned sessions — worker must contact HC for Reset (LIFE-02)
            if (assessment.Status == "Abandoned")
            {
                TempData["Error"] = "Ujian Anda sebelumnya telah dibatalkan. Hubungi HC untuk mengulang.";
                return RedirectToAction("Assessment");
            }

            // Mark InProgress on first load only (idempotent — skip if already started)
            if (assessment.StartedAt == null)
            {
                assessment.Status = "InProgress";
                assessment.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Packages are attached to the representative session (the one HC used when clicking "Packages"),
            // so search across all sibling sessions (same Title + Category + Schedule.Date).
            var siblingSessionIds = await _context.AssessmentSessions
                .Where(s => s.Title == assessment.Title &&
                            s.Category == assessment.Category &&
                            s.Schedule.Date == assessment.Schedule.Date)
                .Select(s => s.Id)
                .ToListAsync();

            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            PackageExamViewModel vm;

            if (packages.Any())
            {
                // ---- PACKAGE PATH ----

                // Check for existing assignment (idempotent — resume)
                var assignment = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

                if (assignment == null)
                {
                    // Randomly assign a package
                    var rng = new Random();
                    var selectedPackage = packages[rng.Next(packages.Count)];

                    // Shuffle question order
                    var questionIds = selectedPackage.Questions
                        .OrderBy(q => q.Order)
                        .Select(q => q.Id)
                        .ToList();
                    Shuffle(questionIds, rng);

                    // Shuffle options per question
                    var optionOrderDict = new Dictionary<int, List<int>>();
                    foreach (var q in selectedPackage.Questions)
                    {
                        var optIds = q.Options.Select(o => o.Id).ToList();
                        Shuffle(optIds, rng);
                        optionOrderDict[q.Id] = optIds;
                    }

                    assignment = new UserPackageAssignment
                    {
                        AssessmentSessionId = id,
                        AssessmentPackageId = selectedPackage.Id,
                        UserId = user.Id,
                        ShuffledQuestionIds = JsonSerializer.Serialize(questionIds),
                        ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(
                            optionOrderDict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value))
                    };
                    _context.UserPackageAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                // Load the assigned package
                var assignedPackage = packages.First(p => p.Id == assignment.AssessmentPackageId);

                // Build ViewModel in shuffled order
                var shuffledQuestionIds = assignment.GetShuffledQuestionIds();
                var shuffledOptionIds = assignment.GetShuffledOptionIds();

                var questionLookup = assignedPackage.Questions.ToDictionary(q => q.Id);
                var optionLookup = assignedPackage.Questions
                    .SelectMany(q => q.Options)
                    .ToDictionary(o => o.Id);

                var examQuestions = new List<ExamQuestionItem>();
                int displayNum = 1;
                foreach (var qId in shuffledQuestionIds)
                {
                    if (!questionLookup.TryGetValue(qId, out var q)) continue;

                    var orderedOptIds = shuffledOptionIds.TryGetValue(qId, out var optIds)
                        ? optIds
                        : q.Options.Select(o => o.Id).ToList();

                    var opts = orderedOptIds
                        .Where(oid => optionLookup.ContainsKey(oid))
                        .Select(oid => new ExamOptionItem
                        {
                            OptionId = optionLookup[oid].Id,
                            OptionText = optionLookup[oid].OptionText
                        }).ToList();

                    examQuestions.Add(new ExamQuestionItem
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        DisplayNumber = displayNum++,
                        Options = opts
                    });
                }

                vm = new PackageExamViewModel
                {
                    AssessmentSessionId = id,
                    Title = assessment.Title,
                    DurationMinutes = assessment.DurationMinutes,
                    HasPackages = true,
                    AssignmentId = assignment.Id,
                    Questions = examQuestions
                };
            }
            else
            {
                // ---- LEGACY PATH: no packages, use old AssessmentQuestion/Option ----
                var assessmentWithQuestions = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .FirstOrDefaultAsync(a => a.Id == id);

                var legacyQuestions = assessmentWithQuestions?.Questions
                    .OrderBy(q => q.Order)
                    .Select((q, i) => new ExamQuestionItem
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        DisplayNumber = i + 1,
                        Options = q.Options.Select(o => new ExamOptionItem
                        {
                            OptionId = o.Id,
                            OptionText = o.OptionText
                        }).ToList()
                    }).ToList() ?? new List<ExamQuestionItem>();

                vm = new PackageExamViewModel
                {
                    AssessmentSessionId = id,
                    Title = assessment.Title,
                    DurationMinutes = assessment.DurationMinutes,
                    HasPackages = false,
                    AssignmentId = null,
                    Questions = legacyQuestions
                };
            }

            return View(vm);
        }

        // --- ABANDON EXAM ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AbandonExam(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Authorization: only the session owner can abandon their own exam
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (assessment.UserId != user.Id)
                return Forbid();

            // Only abandon if currently InProgress (idempotent guard)
            if (assessment.Status != "InProgress" && assessment.Status != "Open")
            {
                TempData["Error"] = "Sesi ujian ini tidak dapat dibatalkan dalam status saat ini.";
                return RedirectToAction("Assessment");
            }

            // Mark Abandoned — keep StartedAt so HC can see when the exam was started
            assessment.Status = "Abandoned";
            assessment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Info"] = "Ujian telah dibatalkan. Hubungi HC jika Anda ingin mengulang.";
            return RedirectToAction("Assessment");
        }

        // Helper: Fisher-Yates shuffle
        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Helper: extract A/B/C/D from common Correct-column formats
        // Accepts: "A", "B.", "C. some text", "OPTION D", "d" (case-insensitive after ToUpper())
        private static string ExtractCorrectLetter(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            // Already a single valid letter
            if (raw.Length == 1) return raw;
            // Starts with A/B/C/D followed by a non-letter: "A.", "B. text", "D. SOME ANSWER"
            if ("ABCD".Contains(raw[0]) && !char.IsLetterOrDigit(raw[1]))
                return raw[0].ToString();
            // "OPTION A" format (case already uppercased by caller)
            if (raw.StartsWith("OPTION ") && raw.Length > 7 && "ABCD".Contains(raw[7]))
                return raw[7].ToString();
            return raw; // unchanged — will fail validation and show error
        }

        // --- EXAM SUMMARY (PRE-SUBMIT REVIEW) ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExamSummary(int id, int? assignmentId, Dictionary<int, int> answers)
        {
            // Validate ownership
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null) return NotFound();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "This assessment has already been completed.";
                return RedirectToAction("Assessment");
            }

            // Store answers in TempData (dictionary key=questionId, value=selectedOptionId)
            TempData["PendingAnswers"] = System.Text.Json.JsonSerializer.Serialize(answers);
            TempData["PendingAssessmentId"] = id;
            TempData["PendingAssignmentId"] = assignmentId;

            return RedirectToAction("ExamSummary", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExamSummary(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null) return NotFound();
            if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
                return Forbid();

            // Retrieve pending answers from TempData
            var answersJson = TempData["PendingAnswers"] as string ?? "{}";
            var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(answersJson)
                          ?? new Dictionary<int, int>();

            // Preserve for the final submit form
            TempData.Keep("PendingAnswers");
            TempData.Keep("PendingAssessmentId");
            TempData.Keep("PendingAssignmentId");

            // CookieTempDataProvider serializes through JSON, which deserializes integers as long — handle both int and long
            int? assignmentId = TempData["PendingAssignmentId"] switch {
                int i => i,
                long l => (int)l,
                _ => (int?)null
            };

            // Build summary items
            var summaryItems = new List<ExamSummaryItem>();

            // Check for package path
            var assignment = assignmentId.HasValue
                ? await _context.UserPackageAssignments.FindAsync(assignmentId.Value)
                : null;

            if (assignment != null)
            {
                var shuffledQIds = assignment.GetShuffledQuestionIds();
                var questions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => q.AssessmentPackageId == assignment.AssessmentPackageId)
                    .ToListAsync();

                var qLookup = questions.ToDictionary(q => q.Id);
                var optLookup = questions.SelectMany(q => q.Options).ToDictionary(o => o.Id);

                int num = 1;
                foreach (var qId in shuffledQIds)
                {
                    if (!qLookup.TryGetValue(qId, out var q)) continue;
                    int? selectedOptId = answers.TryGetValue(qId, out var v) ? v : (int?)null;
                    string? selectedText = selectedOptId.HasValue && optLookup.TryGetValue(selectedOptId.Value, out var opt)
                        ? opt.OptionText
                        : null;

                    summaryItems.Add(new ExamSummaryItem
                    {
                        DisplayNumber = num++,
                        QuestionId = qId,
                        QuestionText = q.QuestionText,
                        SelectedOptionId = selectedOptId,
                        SelectedOptionText = selectedText
                    });
                }
            }
            else
            {
                // Legacy path: AssessmentQuestion
                var legacyQuestions = await _context.AssessmentQuestions
                    .Include(q => q.Options)
                    .Where(q => q.AssessmentSessionId == id)
                    .OrderBy(q => q.Order)
                    .ToListAsync();

                var optLookup = legacyQuestions.SelectMany(q => q.Options).ToDictionary(o => o.Id);

                int num = 1;
                foreach (var q in legacyQuestions)
                {
                    int? selectedOptId = answers.TryGetValue(q.Id, out var v) ? v : (int?)null;
                    string? selectedText = selectedOptId.HasValue && optLookup.TryGetValue(selectedOptId.Value, out var opt)
                        ? opt.OptionText
                        : null;

                    summaryItems.Add(new ExamSummaryItem
                    {
                        DisplayNumber = num++,
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        SelectedOptionId = selectedOptId,
                        SelectedOptionText = selectedText
                    });
                }
            }

            int unansweredCount = summaryItems.Count(s => !s.SelectedOptionId.HasValue);

            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = id;
            ViewBag.AssignmentId = assignmentId;
            ViewBag.UnansweredCount = unansweredCount;
            ViewBag.Answers = answers; // passed to the hidden final-submit form
            return View(summaryItems);
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
        [ValidateAntiForgeryToken]
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

            // ---- Server-side timer enforcement (LIFE-03) ----
            // Grace period: 2 minutes to account for network latency and slow connections.
            // Skip check if StartedAt is null (legacy sessions that existed before Phase 21).
            if (assessment.StartedAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
                int allowedMinutes = assessment.DurationMinutes + 2; // 2-minute grace
                if (elapsed.TotalMinutes > allowedMinutes)
                {
                    TempData["Error"] = "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses.";
                    return RedirectToAction("StartExam", new { id });
                }
            }

            // Check for package path — must happen before any grading logic
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

            if (packageAssignment != null)
            {
                // ---- PACKAGE PATH: ID-based grading via PackageOption.IsCorrect ----
                var packageQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => q.AssessmentPackageId == packageAssignment.AssessmentPackageId)
                    .ToListAsync();

                int totalScore = 0;
                int maxScore = packageQuestions.Count * 10; // each question = 10 points

                foreach (var q in packageQuestions)
                {
                    int? selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : (int?)null;

                    if (selectedOptId.HasValue)
                    {
                        var selectedOption = q.Options.FirstOrDefault(o => o.Id == selectedOptId.Value);
                        if (selectedOption != null && selectedOption.IsCorrect)
                            totalScore += q.ScoreValue;
                    }

                    // Persist answer for package-based answer review
                    _context.PackageUserResponses.Add(new PackageUserResponse
                    {
                        AssessmentSessionId = id,
                        PackageQuestionId = q.Id,
                        PackageOptionId = selectedOptId,
                        SubmittedAt = DateTime.UtcNow
                    });
                }

                int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                assessment.Score = finalPercentage;
                assessment.Status = "Completed";
                assessment.Progress = 100;
                assessment.IsPassed = finalPercentage >= assessment.PassPercentage;
                assessment.CompletedAt = DateTime.UtcNow;

                packageAssignment.IsCompleted = true;

                // Auto-update competency levels (same logic as legacy path)
                if (assessment.IsPassed == true)
                {
                    var mappedCompetencies = await _context.AssessmentCompetencyMaps
                        .Include(m => m.KkjMatrixItem)
                        .Where(m => m.AssessmentCategory == assessment.Category &&
                                    (m.TitlePattern == null || assessment.Title.Contains(m.TitlePattern)))
                        .ToListAsync();

                    if (mappedCompetencies.Any())
                    {
                        var assessmentUser = await _context.Users.FindAsync(assessment.UserId);
                        foreach (var mapping in mappedCompetencies)
                        {
                            if (mapping.MinimumScoreRequired.HasValue && assessment.Score < mapping.MinimumScoreRequired.Value)
                                continue;

                            var existingLevel = await _context.UserCompetencyLevels
                                .FirstOrDefaultAsync(c => c.UserId == assessment.UserId &&
                                                         c.KkjMatrixItemId == mapping.KkjMatrixItemId);
                            if (existingLevel == null)
                            {
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

                return RedirectToAction("Results", new { id });
            }
            else
            {
                // ---- LEGACY PATH: existing AssessmentQuestion + AssessmentOption grading ----
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
            var passPercentage = assessment.PassPercentage;
            var score = assessment.Score ?? 0;

            // Detect package path
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);

            if (packageAssignment != null)
            {
                // Package path: load PackageQuestion + PackageOption + PackageUserResponse data
                var packageQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => q.AssessmentPackageId == packageAssignment.AssessmentPackageId)
                    .OrderBy(q => q.Order)
                    .ToListAsync();

                var packageResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                var responseDict = packageResponses.ToDictionary(r => r.PackageQuestionId);

                // Use shuffled order from assignment for display
                var shuffledQuestionIds = packageAssignment.GetShuffledQuestionIds();
                var questionLookup = packageQuestions.ToDictionary(q => q.Id);

                // If shuffled IDs are empty (edge case), fall back to natural order
                var orderedQuestionIds = shuffledQuestionIds.Any()
                    ? shuffledQuestionIds.Where(qid => questionLookup.ContainsKey(qid)).ToList()
                    : packageQuestions.Select(q => q.Id).ToList();

                if (assessment.AllowAnswerReview)
                {
                    questionReviews = new List<QuestionReviewItem>();
                    int questionNum = 0;
                    foreach (var qId in orderedQuestionIds)
                    {
                        if (!questionLookup.TryGetValue(qId, out var question)) continue;
                        questionNum++;

                        responseDict.TryGetValue(qId, out var userResponse);
                        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                        var selectedOption = userResponse?.PackageOptionId != null
                            ? question.Options.FirstOrDefault(o => o.Id == userResponse.PackageOptionId)
                            : null;
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
                                IsSelected = userResponse?.PackageOptionId == o.Id
                            }).ToList()
                        });
                    }
                }
                else
                {
                    // Count correct even when review disabled
                    foreach (var qId in orderedQuestionIds)
                    {
                        if (!questionLookup.TryGetValue(qId, out var question)) continue;
                        responseDict.TryGetValue(qId, out var userResponse);
                        if (userResponse?.PackageOptionId != null)
                        {
                            var selectedOpt = question.Options.FirstOrDefault(o => o.Id == userResponse.PackageOptionId);
                            if (selectedOpt != null && selectedOpt.IsCorrect)
                                correctCount++;
                        }
                    }
                }

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
                    TotalQuestions = orderedQuestionIds.Count,
                    CorrectAnswers = correctCount,
                    QuestionReviews = questionReviews
                };

                return View(viewModel);
            }
            else
            {
                // Legacy path: existing UserResponse + AssessmentQuestion data
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
        }
        #endregion

        #region Package Management

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManagePackages(int assessmentId)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == assessmentId);
            if (assessment == null) return NotFound();

            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .Where(p => p.AssessmentSessionId == assessmentId)
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            ViewBag.Packages = packages;
            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = assessmentId;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePackage(int assessmentId, string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                TempData["Error"] = "Package name is required.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            var assessment = await _context.AssessmentSessions.FindAsync(assessmentId);
            if (assessment == null) return NotFound();

            // Determine next package number
            var existingCount = await _context.AssessmentPackages
                .CountAsync(p => p.AssessmentSessionId == assessmentId);

            var pkg = new AssessmentPackage
            {
                AssessmentSessionId = assessmentId,
                PackageName = packageName.Trim(),
                PackageNumber = existingCount + 1
            };
            _context.AssessmentPackages.Add(pkg);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Package '{packageName}' created.";
            return RedirectToAction("ManagePackages", new { assessmentId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePackage(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (pkg == null) return NotFound();

            int assessmentId = pkg.AssessmentSessionId;

            // Cascade: options -> questions -> package
            foreach (var q in pkg.Questions)
                _context.PackageOptions.RemoveRange(q.Options);
            _context.PackageQuestions.RemoveRange(pkg.Questions);
            _context.AssessmentPackages.Remove(pkg);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Package '{pkg.PackageName}' deleted.";
            return RedirectToAction("ManagePackages", new { assessmentId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> PreviewPackage(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (pkg == null) return NotFound();

            var assessment = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (assessment == null) return NotFound();

            ViewBag.PackageName = pkg.PackageName;
            ViewBag.AssessmentTitle = assessment?.Title ?? "";
            ViewBag.AssessmentId = pkg.AssessmentSessionId;

            return View(pkg.Questions.OrderBy(q => q.Order).ToList());
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ImportPackageQuestions(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            ViewBag.PackageId = packageId;
            ViewBag.PackageName = pkg.PackageName;
            ViewBag.AssessmentId = pkg.AssessmentSessionId;
            ViewBag.CurrentQuestionCount = pkg.Questions.Count;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPackageQuestions(
            int packageId, IFormFile? excelFile, string? pasteText)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct)> rows;
            var errors = new List<string>();

            if (excelFile != null && excelFile.Length > 0)
            {
                // Parse xlsx with ClosedXML
                rows = new List<(string, string, string, string, string, string)>();
                try
                {
                    using var stream = excelFile.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    int rowNum = 1;
                    foreach (var row in ws.RowsUsed().Skip(1)) // skip header row
                    {
                        rowNum++;
                        var q   = row.Cell(1).GetString().Trim();
                        var a   = row.Cell(2).GetString().Trim();
                        var b   = row.Cell(3).GetString().Trim();
                        var c   = row.Cell(4).GetString().Trim();
                        var d   = row.Cell(5).GetString().Trim();
                        var cor = row.Cell(6).GetString().Trim().ToUpper();
                        rows.Add((q, a, b, c, d, cor));
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Could not read Excel file: {ex.Message}";
                    return RedirectToAction("ImportPackageQuestions", new { packageId });
                }
            }
            else if (!string.IsNullOrWhiteSpace(pasteText))
            {
                // Parse tab-separated paste (skip first line if it looks like a header)
                rows = new List<(string, string, string, string, string, string)>();
                var lines = pasteText.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                // Detect and skip header: if first row's last cell is "Correct" (case-insensitive)
                int startIndex = 0;
                if (lines.Count > 0)
                {
                    var firstCells = lines[0].Split('\t');
                    if (firstCells.Length >= 6 && firstCells[5].Trim().ToLower() == "correct")
                        startIndex = 1;
                }

                for (int i = startIndex; i < lines.Count; i++)
                {
                    var cells = lines[i].Split('\t');
                    if (cells.Length < 6)
                    {
                        errors.Add($"Row {i + 1}: expected 6 columns (Question|OptA|OptB|OptC|OptD|Correct), got {cells.Length}.");
                        continue;
                    }
                    rows.Add((
                        cells[0].Trim(), cells[1].Trim(), cells[2].Trim(),
                        cells[3].Trim(), cells[4].Trim(), cells[5].Trim().ToUpper()
                    ));
                }
            }
            else
            {
                TempData["Error"] = "Please upload an Excel file or paste question data.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Validate and persist rows
            int order = pkg.Questions.Count + 1;
            int added = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var (q, a, b, c, d, cor) = rows[i];
                // Normalize Correct column: accept "A", "B. text", "OPTION C", etc. — extract the letter
                var normalizedCor = ExtractCorrectLetter(cor);
                if (string.IsNullOrWhiteSpace(q))
                {
                    errors.Add($"Row {i + 1}: Question text is empty. Skipped.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) ||
                    string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(d))
                {
                    errors.Add($"Row {i + 1}: One or more options are empty. Skipped.");
                    continue;
                }
                if (!new[] { "A", "B", "C", "D" }.Contains(normalizedCor))
                {
                    errors.Add($"Row {i + 1}: 'Correct' column must be A, B, C, or D. Got '{cor}'. Skipped.");
                    continue;
                }

                var newQ = new PackageQuestion
                {
                    AssessmentPackageId = packageId,
                    QuestionText = q,
                    Order = order++,
                    ScoreValue = 10
                };
                _context.PackageQuestions.Add(newQ);
                await _context.SaveChangesAsync(); // flush to get ID

                // Correct index: A=0, B=1, C=2, D=3
                int correctIndex = normalizedCor == "A" ? 0 : normalizedCor == "B" ? 1 : normalizedCor == "C" ? 2 : 3;
                var opts = new[] { a, b, c, d };
                for (int oi = 0; oi < opts.Length; oi++)
                {
                    _context.PackageOptions.Add(new PackageOption
                    {
                        PackageQuestionId = newQ.Id,
                        OptionText = opts[oi],
                        IsCorrect = (oi == correctIndex)
                    });
                }
                await _context.SaveChangesAsync();
                added++;
            }

            if (errors.Any())
                TempData["Warning"] = $"Imported {added} question(s) with {errors.Count} error(s): " +
                                      string.Join(" | ", errors.Take(5));
            else
                TempData["Success"] = $"Successfully imported {added} question(s) into {pkg.PackageName}.";

            return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
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
