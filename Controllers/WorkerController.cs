using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using ClosedXML.Excel;
using System.Globalization;
using HcPortal.Helpers;

namespace HcPortal.Controllers
{
    [Route("Admin")]
    [Route("Admin/[action]")]
    public class WorkerController : AdminBaseController
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WorkerController> _logger;

        public WorkerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<WorkerController> logger)
            : base(context, userManager, auditLog, env)
        {
            _config = config;
            _logger = logger;
        }

        // Override View resolution to use Views/Admin/ folder
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? unitFilter, string? roleFilter, bool showInactive = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Server-side validation: ignore unitFilter if it doesn't belong to selected sectionFilter
            if (!string.IsNullOrEmpty(unitFilter) && !string.IsNullOrEmpty(sectionFilter))
            {
                var validUnits = await _context.GetUnitsForSectionAsync(sectionFilter);
                if (!validUnits.Contains(unitFilter))
                    unitFilter = null;
            }
            else if (!string.IsNullOrEmpty(unitFilter) && string.IsNullOrEmpty(sectionFilter))
            {
                unitFilter = null;
            }

            var query = _context.Users.AsQueryable();

            // Search by name, email, or NIP
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s) ||
                    (u.NIP != null && u.NIP.Contains(s))
                );
            }

            // Filter by section
            if (!string.IsNullOrEmpty(sectionFilter))
            {
                query = query.Where(u => u.Section == sectionFilter);
            }

            // Filter by unit
            if (!string.IsNullOrEmpty(unitFilter))
            {
                query = query.Where(u => u.Unit == unitFilter);
            }

            // Filter by role level
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var roleLevel = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == roleLevel);
            }

            // Filter by IsActive
            if (!showInactive)
                query = query.Where(u => u.IsActive);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            // Get roles for each user
            var userRolesDict = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userRolesDict[u.Id] = roles.FirstOrDefault() ?? "No Role";
            }
            ViewBag.UserRoles = userRolesDict;

            // Stats
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.AdminCount = await _context.Users.CountAsync(u => u.RoleLevel == 1);
            ViewBag.HcCount = await _context.Users.CountAsync(u => u.RoleLevel == 2);
            ViewBag.WorkerCount = await _context.Users.CountAsync(u => u.RoleLevel >= 5);

            // Filters state
            ViewBag.Search = search;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.UnitFilter = unitFilter;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.ShowInactive = showInactive;
            ViewBag.AllSections = await _context.GetAllSectionsAsync();
            ViewBag.AllUnits = !string.IsNullOrEmpty(sectionFilter)
                ? await _context.GetUnitsForSectionAsync(sectionFilter)
                : new List<string>();

            return View(users);
        }

        // GET /Admin/ExportWorkers
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportWorkers(string? search, string? sectionFilter, string? unitFilter, string? roleFilter, bool showInactive = false)
        {
            // Server-side validation: ignore unitFilter if it doesn't belong to selected sectionFilter
            if (!string.IsNullOrEmpty(unitFilter) && !string.IsNullOrEmpty(sectionFilter))
            {
                var validUnits = await _context.GetUnitsForSectionAsync(sectionFilter);
                if (!validUnits.Contains(unitFilter))
                    unitFilter = null;
            }
            else if (!string.IsNullOrEmpty(unitFilter) && string.IsNullOrEmpty(sectionFilter))
            {
                unitFilter = null;
            }

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s) ||
                    (u.NIP != null && u.NIP.Contains(s))
                );
            }

            if (!string.IsNullOrEmpty(sectionFilter))
                query = query.Where(u => u.Section == sectionFilter);

            if (!string.IsNullOrEmpty(unitFilter))
                query = query.Where(u => u.Unit == unitFilter);

            if (!string.IsNullOrEmpty(roleFilter))
            {
                var roleLevel = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == roleLevel);
            }

            if (!showInactive)
                query = query.Where(u => u.IsActive);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Pekerja", new[] { "No", "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Status" });
            ws.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < users.Count; i++)
            {
                var u = users[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = u.FullName;
                ws.Cell(i + 2, 3).Value = u.Email;
                ws.Cell(i + 2, 4).Value = u.NIP ?? "-";
                ws.Cell(i + 2, 5).Value = u.Position ?? "-";
                ws.Cell(i + 2, 6).Value = u.Section ?? "-";
                ws.Cell(i + 2, 7).Value = u.Unit ?? "-";
                ws.Cell(i + 2, 8).Value = u.IsActive ? "Active" : "Inactive";
            }

            var fileName = $"Pekerja_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // GET /Admin/CreateWorker
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CreateWorker()
        {
            var model = new ManageUserViewModel
            {
                Role = "Coachee"
            };
            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            return View(model);
        }

        // POST /Admin/CreateWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorker(ManageUserViewModel model)
        {
            // Password required only in local mode; AD mode auto-generates
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            if (!useAD && string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password harus diisi untuk user baru");
            }

            // Validate Section/Unit against active OrganizationUnits in DB
            if (!string.IsNullOrEmpty(model.Section))
            {
                var validSections = await _context.GetAllSectionsAsync();
                if (!validSections.Contains(model.Section))
                {
                    ModelState.AddModelError("Section", $"Bagian '{model.Section}' tidak ditemukan di data organisasi");
                }
                else if (!string.IsNullOrEmpty(model.Unit))
                {
                    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
                    if (!validUnits.Contains(model.Unit))
                    {
                        ModelState.AddModelError("Unit", $"Unit '{model.Unit}' tidak valid untuk bagian '{model.Section}'");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("CreateWorker validation failed: {Errors}", errors);
                var sectionUnitsDictErr = await _context.GetSectionUnitsDictAsync();
                ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictErr);
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email sudah terdaftar di sistem");
                return View(model);
            }

            var roleLevel = UserRoles.GetRoleLevel(model.Role);

            // Determine default SelectedView based on role
            var selectedView = UserRoles.GetDefaultView(model.Role);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName,
                NIP = model.NIP,
                Position = model.Position,
                Section = model.Section,
                Unit = model.Unit,
                Directorate = model.Directorate,
                JoinDate = model.JoinDate,
                RoleLevel = roleLevel,
                SelectedView = selectedView
            };

            // AD mode auto-generates password; local mode uses form value
            var password = useAD ? GenerateRandomPassword() : model.Password!;
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                // Audit log
                try
                {
                    var actor = await _userManager.GetUserAsync(User);
                    var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                    await _auditLog.LogAsync(
                        actor?.Id ?? "",
                        actorName,
                        "CreateWorker",
                        $"Created user '{model.FullName}' ({model.Email}) with role '{model.Role}'",
                        null,
                        "ApplicationUser");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for CreateWorker (userId={UserId})", user.Id); }

                TempData["Success"] = $"User '{model.FullName}' berhasil ditambahkan dengan role '{model.Role}'.";
                return RedirectToAction("ManageWorkers");
            }

            // Identity errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // GET /Admin/EditWorker
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new ManageUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                NIP = user.NIP,
                Position = user.Position,
                Section = user.Section,
                Unit = user.Unit,
                Directorate = user.Directorate,
                JoinDate = user.JoinDate,
                Role = roles.FirstOrDefault() ?? "Coachee"
            };

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            return View(model);
        }

        // POST /Admin/EditWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorker(ManageUserViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return BadRequest();

            // Password is optional for edit — remove validation if blank
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            // Validate Section/Unit against active OrganizationUnits in DB
            if (!string.IsNullOrEmpty(model.Section))
            {
                var validSections = await _context.GetAllSectionsAsync();
                if (!validSections.Contains(model.Section))
                {
                    ModelState.AddModelError("Section", $"Bagian '{model.Section}' tidak ditemukan di data organisasi");
                }
                else if (!string.IsNullOrEmpty(model.Unit))
                {
                    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
                    if (!validUnits.Contains(model.Unit))
                    {
                        ModelState.AddModelError("Unit", $"Unit '{model.Unit}' tidak valid untuk bagian '{model.Section}'");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("EditWorker validation failed for user {UserId}: {Errors}", model.Id, errors);
                var sectionUnitsDictErr = await _context.GetSectionUnitsDictAsync();
                ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictErr);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Check if email changed and already in use by another user
            if (user.Email != model.Email)
            {
                var emailUser = await _userManager.FindByEmailAsync(model.Email);
                if (emailUser != null && emailUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Email sudah digunakan oleh user lain");
                    return View(model);
                }
                user.UserName = model.Email;
                user.Email = model.Email;
            }

            // Track changes for audit
            var changes = new List<string>();
            if (user.FullName != model.FullName) changes.Add($"Name: '{user.FullName}' → '{model.FullName}'");
            if (user.NIP != model.NIP) changes.Add($"NIP: '{user.NIP}' → '{model.NIP}'");
            if (user.Position != model.Position) changes.Add($"Position: '{user.Position}' → '{model.Position}'");
            if (user.Section != model.Section) changes.Add($"Section: '{user.Section}' → '{model.Section}'");
            if (user.Unit != model.Unit) changes.Add($"Unit: '{user.Unit}' → '{model.Unit}'");

            // Update fields
            user.FullName = model.FullName;
            user.NIP = model.NIP;
            user.Position = model.Position;
            user.Section = model.Section;
            user.Unit = model.Unit;
            user.Directorate = model.Directorate;
            user.JoinDate = model.JoinDate;

            // Update role if changed
            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();
            if (currentRole != model.Role)
            {
                if (currentRole != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, currentRole);
                }
                await _userManager.AddToRoleAsync(user, model.Role);

                var newRoleLevel = UserRoles.GetRoleLevel(model.Role);
                user.RoleLevel = newRoleLevel;

                // Update SelectedView based on new role
                user.SelectedView = UserRoles.GetDefaultView(model.Role);

                changes.Add($"Role: '{currentRole}' → '{model.Role}'");
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // AD mode: password managed via Pertamina portal — never change it here
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            if (!useAD && !string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError("Password", error.Description);
                    }
                    return View(model);
                }
                changes.Add("Password: reset");
            }

            // Audit log
            try
            {
                var actor = await _userManager.GetUserAsync(User);
                var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                await _auditLog.LogAsync(
                    actor?.Id ?? "",
                    actorName,
                    "EditWorker",
                    $"Updated user '{model.FullName}' ({model.Email}). Changes: {(changes.Any() ? string.Join("; ", changes) : "none")}",
                    null,
                    "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for EditWorker (userId={Id})", model.Id); }

            TempData["Success"] = $"Data user '{model.FullName}' berhasil diperbarui.";
            return RedirectToAction("ManageWorkers");
        }

        // POST /Admin/DeleteWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Prevent self-deletion
            if (currentUser.Id == id)
            {
                TempData["Error"] = "Anda tidak dapat menghapus akun Anda sendiri!";
                return RedirectToAction("ManageWorkers");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User tidak ditemukan.";
                return RedirectToAction("ManageWorkers");
            }

            var userName = user.FullName;
            var userEmail = user.Email;

            // Delete related data that uses Restrict delete behavior
            // UserResponses (Restrict on AssessmentSession)
            var userAssessmentIds = await _context.AssessmentSessions
                .Where(a => a.UserId == id)
                .Select(a => a.Id)
                .ToListAsync();

            if (userAssessmentIds.Any())
            {
                var packageUserResponses = await _context.PackageUserResponses
                    .Where(r => userAssessmentIds.Contains(r.AssessmentSessionId))
                    .ToListAsync();
                if (packageUserResponses.Any())
                    _context.PackageUserResponses.RemoveRange(packageUserResponses);

                var packageAssignments = await _context.UserPackageAssignments
                    .Where(a => userAssessmentIds.Contains(a.AssessmentSessionId))
                    .ToListAsync();
                if (packageAssignments.Any())
                    _context.UserPackageAssignments.RemoveRange(packageAssignments);
            }

            // UserCompetencyLevels removed (Phase 227 CLEN-03 — orphan table dropped)

            // ProtonDeliverableProgress (references CoacheeId as string)
            var protonProgress = await _context.ProtonDeliverableProgresses
                .Where(p => p.CoacheeId == id)
                .ToListAsync();
            if (protonProgress.Any())
                _context.ProtonDeliverableProgresses.RemoveRange(protonProgress);

            // ProtonFinalAssessments (Restrict on ProtonTrackAssignment — must be deleted before assignments)
            var protonFinalAssessments = await _context.ProtonFinalAssessments
                .Where(fa => fa.CoacheeId == id)
                .ToListAsync();
            if (protonFinalAssessments.Any())
                _context.ProtonFinalAssessments.RemoveRange(protonFinalAssessments);

            // ProtonTrackAssignments
            var protonAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == id)
                .ToListAsync();
            if (protonAssignments.Any())
                _context.ProtonTrackAssignments.RemoveRange(protonAssignments);

            // ProtonNotifications
            var protonNotifs = await _context.ProtonNotifications
                .Where(n => n.RecipientId == id || n.CoacheeId == id)
                .ToListAsync();
            if (protonNotifs.Any())
                _context.ProtonNotifications.RemoveRange(protonNotifs);

            // CoachCoacheeMappings
            var coachMappings = await _context.CoachCoacheeMappings
                .Where(m => m.CoachId == id || m.CoacheeId == id)
                .ToListAsync();
            if (coachMappings.Any())
                _context.CoachCoacheeMappings.RemoveRange(coachMappings);

            // CoachingSessions
            var coachSessions = await _context.CoachingSessions
                .Where(s => s.CoachId == id || s.CoacheeId == id)
                .ToListAsync();
            if (coachSessions.Any())
                _context.CoachingSessions.RemoveRange(coachSessions);

            // CoachingLogs
            var coachLogs = await _context.CoachingLogs
                .Where(l => l.CoachId == id || l.CoacheeId == id)
                .ToListAsync();
            if (coachLogs.Any())
                _context.CoachingLogs.RemoveRange(coachLogs);

            await _context.SaveChangesAsync();

            // Now delete the user (cascade will handle TrainingRecords, AssessmentSessions, IdpItems)
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                // Audit log
                try
                {
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser.Id,
                        actorName,
                        "DeleteWorker",
                        $"Deleted user '{userName}' ({userEmail})",
                        null,
                        "ApplicationUser");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteWorker (userId={Id})", id); }

                TempData["Success"] = $"User '{userName}' berhasil dihapus dari sistem.";
            }
            else
            {
                TempData["Error"] = $"Gagal menghapus user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction("ManageWorkers");
        }

        // POST /Admin/DeactivateWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (currentUser.Id == id)
            {
                TempData["Error"] = "Anda tidak dapat menonaktifkan akun Anda sendiri!";
                return RedirectToAction("ManageWorkers");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["Error"] = "User tidak ditemukan."; return RedirectToAction("ManageWorkers"); }
            if (!user.IsActive) { TempData["Error"] = $"User '{user.FullName}' sudah tidak aktif."; return RedirectToAction("ManageWorkers"); }

            // Count active coaching and assessments for confirmation message
            var activeCoachingCount = await _context.CoachCoacheeMappings
                .CountAsync(m => (m.CoachId == id || m.CoacheeId == id) && m.IsActive);
            var activeAssessmentCount = await _context.AssessmentSessions
                .CountAsync(a => a.UserId == id && (a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress"));

            // Auto-close active coaching mappings
            var activeMappings = await _context.CoachCoacheeMappings
                .Where(m => (m.CoachId == id || m.CoacheeId == id) && m.IsActive)
                .ToListAsync();
            foreach (var m in activeMappings) { m.IsActive = false; m.EndDate = DateTime.Today; }

            // Cascade: deactivate ProtonTrackAssignments for all deactivated mappings
            var coacheeIds = activeMappings.Where(m => m.CoacheeId == id).Select(m => m.CoacheeId)
                .Union(activeMappings.Where(m => m.CoachId == id).Select(m => m.CoacheeId))
                .Distinct().ToList();
            var activeTrackAssignments = await _context.ProtonTrackAssignments
                .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();
            foreach (var a in activeTrackAssignments) { a.IsActive = false; }
            var trackAssignmentCount = activeTrackAssignments.Count;

            // Auto-cancel active assessment sessions
            var activeSessions = await _context.AssessmentSessions
                .Where(a => a.UserId == id && (a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress"))
                .ToListAsync();
            foreach (var s in activeSessions) { s.Status = "Closed"; }

            // Soft delete: set IsActive = false
            user.IsActive = false;
            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var actorName = string.IsNullOrWhiteSpace(currentUser.NIP) ? currentUser.FullName : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(currentUser.Id, actorName, "DeactivateWorker",
                    $"Nonaktifkan user '{user.FullName}' ({user.Email}). {activeCoachingCount} coaching ditutup, {activeAssessmentCount} assessment dibatalkan, {trackAssignmentCount} track assignment dinonaktifkan. UserId={id}",
                    null, "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeactivateWorker (userId={Id})", id); }

            var detail = "";
            if (activeCoachingCount > 0) detail += $" {activeCoachingCount} coaching aktif ditutup.";
            if (activeAssessmentCount > 0) detail += $" {activeAssessmentCount} assessment dibatalkan.";
            if (trackAssignmentCount > 0) detail += $" {trackAssignmentCount} track assignment dinonaktifkan.";
            TempData["Success"] = $"User '{user.FullName}' berhasil dinonaktifkan.{detail}";
            return RedirectToAction("ManageWorkers", new { showInactive = true });
        }

        // POST /Admin/ReactivateWorker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateWorker(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["Error"] = "User tidak ditemukan."; return RedirectToAction("ManageWorkers", new { showInactive = true }); }
            if (user.IsActive) { TempData["Error"] = $"User '{user.FullName}' sudah aktif."; return RedirectToAction("ManageWorkers"); }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            try
            {
                var actorName = string.IsNullOrWhiteSpace(currentUser.NIP) ? currentUser.FullName : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(currentUser.Id, actorName, "ReactivateWorker",
                    $"Aktifkan kembali user '{user.FullName}' ({user.Email}). UserId={id}",
                    null, "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ReactivateWorker (userId={Id})", id); }

            TempData["Success"] = $"User '{user.FullName}' berhasil diaktifkan kembali.";
            return RedirectToAction("ManageWorkers", new { showInactive = true });
        }


        // GET /Admin/WorkerDetail
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> WorkerDetail(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.FirstOrDefault() ?? "No Role";

            return View(user);
        }

        // GET /Admin/ImportWorkers
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ImportWorkers()
        {
            ViewBag.AllSections = await _context.GetAllSectionsAsync();
            return View();
        }

        // GET /Admin/DownloadImportTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import Workers");

            // Dynamic headers based on auth mode
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
            var headers = new List<string>
            {
                "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung (YYYY-MM-DD)"
            };
            if (!useAD)
            {
                headers.Add("Password");
            }
            for (int i = 0; i < headers.Count; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var example = new List<object>
            {
                "Ahmad Fauzi", "ahmad.fauzi@pertamina.com", "123456", "Operator",
                "RFCC", "RFCC LPG Treating Unit (062)", "CSU Process", "Coachee", "2024-01-15"
            };
            if (!useAD) { example.Add("Password123!"); }
            for (int i = 0; i < example.Count; i++)
            {
                ws.Cell(2, i + 1).Value = example[i]?.ToString();
                ws.Cell(2, i + 1).Style.Font.Italic = true;
                ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray;
            }

            var sections = await _context.GetAllSectionsAsync();
            ws.Cell(3, 1).Value = $"Kolom Bagian: {string.Join(" / ", sections)}";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;
            ws.Cell(4, 1).Value = $"Kolom Role: {string.Join(" / ", UserRoles.AllRoles)}";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;
            if (useAD)
            {
                ws.Cell(5, 1).Value = "Mode AD aktif: Kolom Password tidak diperlukan. Sistem akan membuat password acak.";
                ws.Cell(5, 1).Style.Font.Italic = true;
                ws.Cell(5, 1).Style.Font.FontColor = XLColor.DarkBlue;
            }

            return ExcelExportHelper.ToFileResult(workbook, "workers_import_template.xlsx", this);
        }

        // POST /Admin/ImportWorkers
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportWorkers(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Pilih file Excel terlebih dahulu.";
                return View();
            }

            var allowedImportExtensions = new[] { ".xlsx", ".xls" };
            var importExt = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedImportExtensions.Contains(importExt))
            {
                TempData["Error"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                return View();
            }

            const long maxImportSize = 10 * 1024 * 1024; // 10MB
            if (excelFile.Length > maxImportSize)
            {
                TempData["Error"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return View();
            }

            var results = new List<ImportWorkerResult>();
            var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();

                var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nama = (row.Cell(1).GetString() ?? "").Trim();
                    var email = (row.Cell(2).GetString() ?? "").Trim();
                    var nip = (row.Cell(3).GetString() ?? "").Trim();
                    var jabatan = (row.Cell(4).GetString() ?? "").Trim();
                    var bagian = (row.Cell(5).GetString() ?? "").Trim();
                    var unit = (row.Cell(6).GetString() ?? "").Trim();
                    var directorate = (row.Cell(7).GetString() ?? "").Trim();
                    var role = (row.Cell(8).GetString() ?? "").Trim();
                    var tglStr = (row.Cell(9).GetString() ?? "").Trim();

                    // Skip blank rows (e.g. notes/example rows)
                    if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(email)) continue;

                    var result = new ImportWorkerResult { Nama = nama, Email = email, Role = role };

                    // AD mode generates password; local mode reads from column 10
                    string password;
                    if (useAD)
                    {
                        password = GenerateRandomPassword();
                    }
                    else
                    {
                        password = (row.Cell(10).GetString() ?? "").Trim();
                    }

                    var errors = new List<string>();
                    if (string.IsNullOrWhiteSpace(nama)) errors.Add("Nama kosong");
                    if (string.IsNullOrWhiteSpace(email)) errors.Add("Email kosong");
                    if (!useAD && string.IsNullOrWhiteSpace(password)) errors.Add("Password kosong");
                    if (string.IsNullOrWhiteSpace(role) || !UserRoles.AllRoles.Contains(role))
                        errors.Add($"Role tidak valid");

                    // Validasi Section terhadap OrganizationUnit database
                    if (!string.IsNullOrWhiteSpace(bagian) && !sectionUnitsDict.ContainsKey(bagian))
                        errors.Add($"Section '{bagian}' tidak ditemukan di database");

                    // Validasi Unit: harus child dari Section yang dipilih
                    if (!string.IsNullOrWhiteSpace(unit))
                    {
                        if (string.IsNullOrWhiteSpace(bagian))
                            errors.Add("Unit tidak boleh diisi tanpa Section");
                        else if (sectionUnitsDict.TryGetValue(bagian, out var validUnits) && !validUnits.Contains(unit))
                            errors.Add($"Unit '{unit}' bukan child dari Section '{bagian}'");
                    }

                    if (errors.Any())
                    {
                        result.Status = "Error";
                        result.Message = string.Join("; ", errors);
                        results.Add(result);
                        continue;
                    }

                    var existing = await _userManager.FindByEmailAsync(email);
                    if (existing != null)
                    {
                        if (!existing.IsActive)
                        {
                            result.Status = "PerluReview";
                            result.Message = "Email terdaftar tapi tidak aktif — dapat diaktifkan kembali";
                            result.ExistingUserId = existing.Id;
                        }
                        else
                        {
                            result.Status = "Skip";
                            result.Message = "Email sudah terdaftar, dilewati";
                        }
                        results.Add(result);
                        continue;
                    }

                    DateTime? joinDate = null;
                    if (!string.IsNullOrWhiteSpace(tglStr) && DateTime.TryParse(tglStr, out var parsedDate))
                        joinDate = parsedDate;

                    var roleLevel = UserRoles.GetRoleLevel(role);
                    var selectedView = UserRoles.GetDefaultView(role);

                    var newUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = nama,
                        NIP = string.IsNullOrWhiteSpace(nip) ? null : nip,
                        Position = string.IsNullOrWhiteSpace(jabatan) ? null : jabatan,
                        Section = string.IsNullOrWhiteSpace(bagian) ? null : bagian,
                        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                        Directorate = string.IsNullOrWhiteSpace(directorate) ? null : directorate,
                        JoinDate = joinDate,
                        RoleLevel = roleLevel,
                        SelectedView = selectedView
                    };

                    var createResult = await _userManager.CreateAsync(newUser, password);
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, role);
                        result.Status = "Success";
                        result.Message = "Berhasil dibuat";
                    }
                    else
                    {
                        result.Status = "Error";
                        result.Message = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    }

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read Excel import file");
                TempData["Error"] = "Gagal membaca file Excel. Pastikan format file benar.";
                return View();
            }

            // Audit log
            try
            {
                var actor = await _userManager.GetUserAsync(User);
                var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                var successCount = results.Count(r => r.Status == "Success");
                var reviewCount = results.Count(r => r.Status == "PerluReview");
                await _auditLog.LogAsync(actor?.Id ?? "", actorName, "ImportWorkers",
                    $"Bulk import: {successCount} berhasil, {results.Count(r => r.Status == "Error")} error, {results.Count(r => r.Status == "Skip")} dilewati, {reviewCount} perlu review",
                    null, "ApplicationUser");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ImportWorkers"); }

            ViewBag.ImportResults = results;
            return View();
        }

        // Helper: Crypto-random 16-char password for AD mode auto-generation
        private static string GenerateRandomPassword()
        {
            var bytes = new byte[12];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            // Base64 ensures uppercase + lowercase + digits, no special chars that break Identity validation
            return Convert.ToBase64String(bytes);
        }
    }
}
