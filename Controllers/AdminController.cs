using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Models;
using HcPortal.Models.Competency;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Services;
using ClosedXML.Excel;

namespace HcPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLog;
        private readonly IMemoryCache _cache;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _cache = cache;
        }

        // GET /Admin/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET /Admin/KkjMatrix
        public async Task<IActionResult> KkjMatrix()
        {
            ViewData["Title"] = "Kelola KKJ Matrix";

            // Seed default bagians if none exist yet
            if (!await _context.KkjBagians.AnyAsync())
            {
                var defaults = new[]
                {
                    new KkjBagian { Name = "RFCC",    DisplayOrder = 1 },
                    new KkjBagian { Name = "GAST",    DisplayOrder = 2 },
                    new KkjBagian { Name = "NGP",     DisplayOrder = 3 },
                    new KkjBagian { Name = "DHT/HMU", DisplayOrder = 4 },
                };
                _context.KkjBagians.AddRange(defaults);
                await _context.SaveChangesAsync();
            }

            var bagians = await _context.KkjBagians.OrderBy(b => b.DisplayOrder).ToListAsync();
            var items   = await _context.KkjMatrices.OrderBy(k => k.No).ToListAsync();

            ViewBag.Bagians = bagians;
            return View(items);
        }

        // POST /Admin/KkjMatrixSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjMatrixSave([FromBody] List<KkjMatrixItem> rows)
        {
            if (rows == null || !rows.Any())
                return Json(new { success = false, message = "Tidak ada data yang diterima." });

            try
            {
                foreach (var row in rows)
                {
                    if (row.Id == 0)
                    {
                        _context.KkjMatrices.Add(row);
                    }
                    else
                    {
                        var existing = await _context.KkjMatrices.FindAsync(row.Id);
                        if (existing != null)
                        {
                            existing.No = row.No;
                            existing.SkillGroup = row.SkillGroup ?? "";
                            existing.SubSkillGroup = row.SubSkillGroup ?? "";
                            existing.Indeks = row.Indeks ?? "";
                            existing.Kompetensi = row.Kompetensi ?? "";
                            existing.Bagian = row.Bagian ?? "";
                            existing.Target_SectionHead = row.Target_SectionHead ?? "-";
                            existing.Target_SrSpv_GSH = row.Target_SrSpv_GSH ?? "-";
                            existing.Target_ShiftSpv_GSH = row.Target_ShiftSpv_GSH ?? "-";
                            existing.Target_Panelman_GSH_12_13 = row.Target_Panelman_GSH_12_13 ?? "-";
                            existing.Target_Panelman_GSH_14 = row.Target_Panelman_GSH_14 ?? "-";
                            existing.Target_Operator_GSH_8_11 = row.Target_Operator_GSH_8_11 ?? "-";
                            existing.Target_Operator_GSH_12_13 = row.Target_Operator_GSH_12_13 ?? "-";
                            existing.Target_ShiftSpv_ARU = row.Target_ShiftSpv_ARU ?? "-";
                            existing.Target_Panelman_ARU_12_13 = row.Target_Panelman_ARU_12_13 ?? "-";
                            existing.Target_Panelman_ARU_14 = row.Target_Panelman_ARU_14 ?? "-";
                            existing.Target_Operator_ARU_8_11 = row.Target_Operator_ARU_8_11 ?? "-";
                            existing.Target_Operator_ARU_12_13 = row.Target_Operator_ARU_12_13 ?? "-";
                            existing.Target_SrSpv_Facility = row.Target_SrSpv_Facility ?? "-";
                            existing.Target_JrAnalyst = row.Target_JrAnalyst ?? "-";
                            existing.Target_HSE = row.Target_HSE ?? "-";
                        }
                    }
                }
                await _context.SaveChangesAsync();

                var actor = await _userManager.GetUserAsync(User);
                if (actor != null)
                    await _auditLog.LogAsync(actor.Id, actor.FullName, "BulkUpdate",
                        $"KKJ Matrix bulk-save: {rows.Count} rows", targetType: "KkjMatrixItem");

                return Json(new { success = true, message = $"{rows.Count} baris berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/KkjBagianSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianSave([FromBody] List<KkjBagian> bagians)
        {
            if (bagians == null || !bagians.Any())
                return Json(new { success = false, message = "Tidak ada data bagian." });

            try
            {
                foreach (var b in bagians)
                {
                    if (b.Id == 0)
                    {
                        _context.KkjBagians.Add(b);
                    }
                    else
                    {
                        var existing = await _context.KkjBagians.FindAsync(b.Id);
                        if (existing != null)
                        {
                            existing.Name         = b.Name;
                            existing.DisplayOrder = b.DisplayOrder;
                            existing.Label_SectionHead        = b.Label_SectionHead        ?? "Section Head";
                            existing.Label_SrSpv_GSH          = b.Label_SrSpv_GSH          ?? "Sr Spv GSH";
                            existing.Label_ShiftSpv_GSH       = b.Label_ShiftSpv_GSH       ?? "Shift Spv GSH";
                            existing.Label_Panelman_GSH_12_13 = b.Label_Panelman_GSH_12_13 ?? "Panelman GSH 12-13";
                            existing.Label_Panelman_GSH_14    = b.Label_Panelman_GSH_14    ?? "Panelman GSH 14";
                            existing.Label_Operator_GSH_8_11  = b.Label_Operator_GSH_8_11  ?? "Op GSH 8-11";
                            existing.Label_Operator_GSH_12_13 = b.Label_Operator_GSH_12_13 ?? "Op GSH 12-13";
                            existing.Label_ShiftSpv_ARU       = b.Label_ShiftSpv_ARU       ?? "Shift Spv ARU";
                            existing.Label_Panelman_ARU_12_13 = b.Label_Panelman_ARU_12_13 ?? "Panelman ARU 12-13";
                            existing.Label_Panelman_ARU_14    = b.Label_Panelman_ARU_14    ?? "Panelman ARU 14";
                            existing.Label_Operator_ARU_8_11  = b.Label_Operator_ARU_8_11  ?? "Op ARU 8-11";
                            existing.Label_Operator_ARU_12_13 = b.Label_Operator_ARU_12_13 ?? "Op ARU 12-13";
                            existing.Label_SrSpv_Facility     = b.Label_SrSpv_Facility     ?? "Sr Spv Facility";
                            existing.Label_JrAnalyst          = b.Label_JrAnalyst          ?? "Jr Analyst";
                            existing.Label_HSE                = b.Label_HSE                ?? "HSE";
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/KkjBagianAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianAdd()
        {
            var maxOrder = await _context.KkjBagians.MaxAsync(b => (int?)b.DisplayOrder) ?? 0;
            var newBagian = new KkjBagian
            {
                Name         = "Bagian Baru",
                DisplayOrder = maxOrder + 1
            };
            _context.KkjBagians.Add(newBagian);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success      = true,
                id           = newBagian.Id,
                name         = newBagian.Name,
                displayOrder = newBagian.DisplayOrder
            });
        }

        // POST /Admin/KkjBagianDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjBagianDelete(int id)
        {
            var bagian = await _context.KkjBagians.FindAsync(id);
            if (bagian == null)
                return Json(new { success = false, message = "Bagian tidak ditemukan." });

            var assignedCount = await _context.KkjMatrices
                .CountAsync(k => k.Bagian == bagian.Name);

            if (assignedCount > 0)
                return Json(new { success = false, blocked = true,
                    message = $"Tidak dapat dihapus — masih ada {assignedCount} item yang di-assign ke bagian ini." });

            _context.KkjBagians.Remove(bagian);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST /Admin/KkjMatrixDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KkjMatrixDelete(int id)
        {
            var item = await _context.KkjMatrices.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Item tidak ditemukan." });

            var usageCount = await _context.UserCompetencyLevels
                .CountAsync(u => u.KkjMatrixItemId == id);

            if (usageCount > 0)
                return Json(new { success = false, blocked = true,
                    message = $"Tidak dapat dihapus — digunakan oleh {usageCount} pekerja." });

            _context.KkjMatrices.Remove(item);
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                    $"Deleted KkjMatrixItem Id={id} ({item.Kompetensi})",
                    targetId: id, targetType: "KkjMatrixItem");

            return Json(new { success = true });
        }

        // GET /Admin/ManageAssessment
        [HttpGet]
        public async Task<IActionResult> ManageAssessment(string? search, int page = 1, int pageSize = 20)
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var managementQuery = _context.AssessmentSessions
                .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
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

            var allSessions = await managementQuery
                .Include(a => a.User)
                .OrderByDescending(a => a.Schedule)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    a.ExamWindowCloseDate,
                    a.DurationMinutes,
                    a.Status,
                    a.IsTokenRequired,
                    a.AccessToken,
                    a.PassPercentage,
                    a.AllowAnswerReview,
                    a.CreatedAt,
                    UserFullName = a.User != null ? a.User.FullName : "Unknown",
                    UserEmail = a.User != null ? a.User.Email : "",
                    UserId = a.User != null ? a.User.Id : ""
                })
                .ToListAsync();

            // Group by (Title, Category, Schedule.Date) — identical to CMPController manage branch
            var grouped = allSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g =>
                {
                    var rep = g.OrderBy(a => a.CreatedAt).First();
                    return new
                    {
                        rep.Title,
                        rep.Category,
                        rep.Schedule,
                        rep.ExamWindowCloseDate,
                        rep.DurationMinutes,
                        rep.Status,
                        rep.IsTokenRequired,
                        rep.AccessToken,
                        rep.PassPercentage,
                        rep.AllowAnswerReview,
                        RepresentativeId = rep.Id,
                        Users = g.Select(a => new { a.UserFullName, a.UserEmail, a.UserId }).ToList(),
                        AllIds = g.Select(a => a.Id).ToList(),
                        UserCount = g.Count()
                    };
                })
                .OrderByDescending(g => g.Schedule)
                .ToList();

            var totalCount = grouped.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            ViewBag.ManagementData = grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.SearchTerm = search;

            return View();
        }

        // GET /Admin/CpdpItems
        public async Task<IActionResult> CpdpItems()
        {
            ViewData["Title"] = "KKJ-IDP Mapping Editor";
            var items = await _context.CpdpItems
                .OrderBy(c => c.No)
                .ThenBy(c => c.Id)
                .ToListAsync();
            return View(items);
        }

        // POST /Admin/CpdpItemsSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CpdpItemsSave([FromBody] List<CpdpItem> rows)
        {
            if (rows == null || !rows.Any())
                return Json(new { success = false, message = "Tidak ada data yang diterima." });

            try
            {
                foreach (var row in rows)
                {
                    if (row.Id == 0)
                    {
                        _context.CpdpItems.Add(row);
                    }
                    else
                    {
                        var existing = await _context.CpdpItems.FindAsync(row.Id);
                        if (existing != null)
                        {
                            // Warn if NamaKompetensi changed and IdpItems reference the old name
                            if (existing.NamaKompetensi != row.NamaKompetensi)
                            {
                                var refCount = await _context.IdpItems
                                    .CountAsync(i => i.Kompetensi == existing.NamaKompetensi);
                                if (refCount > 0)
                                    return Json(new { success = false,
                                        message = $"Tidak bisa ubah NamaKompetensi '{existing.NamaKompetensi}' — {refCount} IDP record masih mereferensi nama ini." });
                            }

                            existing.No                 = row.No ?? "";
                            existing.NamaKompetensi     = row.NamaKompetensi ?? "";
                            existing.IndikatorPerilaku  = row.IndikatorPerilaku ?? "";
                            existing.DetailIndikator    = row.DetailIndikator ?? "";
                            existing.Silabus            = row.Silabus ?? "";
                            existing.TargetDeliverable  = row.TargetDeliverable ?? "";
                            existing.Status             = row.Status ?? "";
                            existing.Section            = row.Section ?? "";
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var actor = await _userManager.GetUserAsync(User);
                if (actor != null)
                    await _auditLog.LogAsync(actor.Id, actor.FullName, "BulkUpdate",
                        $"CPDP Items bulk-save: {rows.Count} rows", targetType: "CpdpItem");

                return Json(new { success = true, message = $"{rows.Count} baris berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST /Admin/CpdpItemDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CpdpItemDelete(int id)
        {
            var item = await _context.CpdpItems.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "CPDP item tidak ditemukan." });

            _context.CpdpItems.Remove(item);
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                    $"Deleted CpdpItem Id={id} ({item.NamaKompetensi})",
                    targetId: id, targetType: "CpdpItem");

            return Json(new { success = true });
        }

        // --- CREATE ASSESSMENT ---
        // GET: Show create assessment form
        [HttpGet]
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
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
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

            return RedirectToAction("ManageAssessment");
        }

        // --- EDIT ASSESSMENT ---
        // GET: Show edit form
        [HttpGet]
        public async Task<IActionResult> EditAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("ManageAssessment");
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

            // Count packages attached to this assessment's sibling group (for schedule-change warning)
            var siblingIds = siblings.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            ViewBag.PackageCount = packageCount;
            ViewBag.OriginalSchedule = assessment.Schedule.ToString("yyyy-MM-dd");

            return View(assessment);
        }

        // POST: Update assessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, AssessmentSession model, List<string> NewUserIds)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("ManageAssessment");
            }

            // Prevent editing completed assessments (optional - you can remove this if needed)
            if (assessment.Status == "Completed")
            {
                TempData["Error"] = "Cannot edit completed assessments.";
                return RedirectToAction("ManageAssessment");
            }

            // Rate limit: guard before any DB work
            if (NewUserIds != null && NewUserIds.Count > 50)
            {
                TempData["Error"] = "Cannot assign more than 50 users at once. Please split into multiple batches.";
                return RedirectToAction("ManageAssessment");
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
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error updating assessment");
                TempData["Error"] = $"Failed to update assessment: {ex.Message}";
                return RedirectToAction("ManageAssessment");
            }

            // ===== BULK ASSIGN: create new sessions for selected users =====
            if (NewUserIds != null && NewUserIds.Count > 0)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
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
                                return RedirectToAction("ManageAssessment");
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
                    var logger2 = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                    logger2.LogError(ex, "Error bulk-assigning users to assessment {Id}", id);
                    TempData["Error"] = $"Assessment updated but bulk assign failed: {ex.Message}";
                }
            }

            return RedirectToAction("ManageAssessment");
        }

        // --- DELETE ASSESSMENT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessment(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentGroup(int representativeId)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

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
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error regenerating token");
                return Json(new { success = false, message = $"Failed to regenerate token: {ex.Message}" });
            }
        }

        // GET /Admin/CpdpItemsExport?section=RFCC
        public async Task<IActionResult> CpdpItemsExport(string? section)
        {
            var query = _context.CpdpItems.OrderBy(c => c.No).ThenBy(c => c.Id).AsQueryable();

            if (!string.IsNullOrEmpty(section))
                query = query.Where(c => c.Section == section);

            var items = await query.ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("CPDP Items");

            // Header row
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Nama Kompetensi";
            ws.Cell(1, 3).Value = "Indikator Perilaku";
            ws.Cell(1, 4).Value = "Detail Indikator";
            ws.Cell(1, 5).Value = "Silabus / IDP";
            ws.Cell(1, 6).Value = "Target Deliverable";
            ws.Cell(1, 7).Value = "Status";
            ws.Cell(1, 8).Value = "Section";

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#343a40");
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            // Data rows
            for (int i = 0; i < items.Count; i++)
            {
                var row = items[i];
                var r = i + 2;
                ws.Cell(r, 1).Value = row.No;
                ws.Cell(r, 2).Value = row.NamaKompetensi;
                ws.Cell(r, 3).Value = row.IndikatorPerilaku;
                ws.Cell(r, 4).Value = row.DetailIndikator;
                ws.Cell(r, 5).Value = row.Silabus;
                ws.Cell(r, 6).Value = row.TargetDeliverable;
                ws.Cell(r, 7).Value = row.Status;
                ws.Cell(r, 8).Value = row.Section;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = string.IsNullOrEmpty(section)
                ? "CPDP_Items_All.xlsx"
                : $"CPDP_Items_{section}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // --- PRIVATE HELPERS ---
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

        // --- ASSESSMENT MONITORING DETAIL ---
        [HttpGet]
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
                return RedirectToAction("ManageAssessment");
            }

            // Detect package mode: check if any sibling session has packages attached
            var siblingIds = sessions.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Build question count map per session
            Dictionary<int, int> questionCountMap = new();
            if (isPackageMode)
            {
                // Package mode: count PackageQuestion rows via UserPackageAssignment -> AssessmentPackage
                questionCountMap = await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .Join(_context.AssessmentPackages.Include(p => p.Questions),
                        a => a.AssessmentPackageId,
                        p => p.Id,
                        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
                    .ToDictionaryAsync(
                        x => x.AssessmentSessionId,
                        x => x.QuestionCount);
            }
            else
            {
                // Legacy mode: count AssessmentQuestion rows per session
                questionCountMap = await _context.AssessmentQuestions
                    .Where(q => siblingIds.Contains(q.AssessmentSessionId))
                    .GroupBy(q => q.AssessmentSessionId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Count());
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
                    StartedAt    = a.StartedAt,
                    QuestionCount = questionCountMap.ContainsKey(a.Id) ? questionCountMap[a.Id] : 0
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
                               : sessions.Any(a => a.Status == "Upcoming") ? "Upcoming" : "Closed",
                IsPackageMode  = isPackageMode,
                PendingCount   = sessionViewModels.Count(s => s.UserStatus == "Not started")
            };

            ViewBag.BackUrl = Url.Action("ManageAssessment", "Admin");
            return View(model);
        }

        // --- GET MONITORING PROGRESS (polling endpoint for real-time monitoring) ---
        [HttpGet]
        public async Task<IActionResult> GetMonitoringProgress(string title, string category, DateTime scheduleDate)
        {
            // Step 1: load sessions (same filter as AssessmentMonitoringDetail)
            var sessions = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
                return Json(Array.Empty<object>());

            var siblingIds = sessions.Select(s => s.Id).ToList();

            // Step 2: detect package mode
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Step 3: build total question count map per session (reuse pattern from AssessmentMonitoringDetail)
            Dictionary<int, int> questionCountMap;
            if (isPackageMode)
            {
                questionCountMap = await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .Join(_context.AssessmentPackages.Include(p => p.Questions),
                        a => a.AssessmentPackageId,
                        p => p.Id,
                        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
                    .ToDictionaryAsync(
                        x => x.AssessmentSessionId,
                        x => x.QuestionCount);
            }
            else
            {
                questionCountMap = await _context.AssessmentQuestions
                    .Where(q => siblingIds.Contains(q.AssessmentSessionId))
                    .GroupBy(q => q.AssessmentSessionId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }

            // Step 4: build answered count map (single GROUP BY query, not N+1)
            Dictionary<int, int> answeredCountMap;
            if (isPackageMode)
            {
                answeredCountMap = await _context.PackageUserResponses
                    .Where(p => siblingIds.Contains(p.AssessmentSessionId))
                    .GroupBy(p => p.AssessmentSessionId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            else
            {
                answeredCountMap = await _context.UserResponses
                    .Where(r => siblingIds.Contains(r.AssessmentSessionId))
                    .GroupBy(r => r.AssessmentSessionId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }

            // Step 5: project to DTOs
            var dtos = sessions.Select(a =>
            {
                string status;
                if (a.CompletedAt != null || a.Score != null)
                    status = "Completed";
                else if (a.Status == "Abandoned")
                    status = "Abandoned";
                else if (a.StartedAt != null)
                    status = "InProgress";
                else
                    status = "Not started";

                int? remainingSeconds = null;
                if (status == "InProgress")
                    remainingSeconds = Math.Max(0, (a.DurationMinutes * 60) - a.ElapsedSeconds);

                string? result = a.IsPassed == true ? "Pass" : a.IsPassed == false ? "Fail" : null;

                return new
                {
                    sessionId      = a.Id,
                    status,
                    progress       = answeredCountMap.TryGetValue(a.Id, out var ans) ? ans : 0,
                    totalQuestions = questionCountMap.TryGetValue(a.Id, out var total) ? total : 0,
                    score          = a.Score,
                    result,
                    remainingSeconds,
                    completedAt    = a.CompletedAt
                };
            }).ToList();

            return Json(dtos);
        }

        // --- RESET ASSESSMENT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // Reset is valid for any active status (Open, InProgress, Completed, Abandoned)
            if (assessment.Status != "Open" && assessment.Status != "InProgress" && assessment.Status != "Completed" && assessment.Status != "Abandoned")
            {
                TempData["Error"] = "Status sesi tidak valid untuk direset.";
                return RedirectToAction("AssessmentMonitoringDetail", new
                {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule
                });
            }

            // Phase 46: Archive attempt data if session was Completed
            if (assessment.Status == "Completed")
            {
                int existingAttempts = await _context.AssessmentAttemptHistory
                    .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title)
                    .CountAsync();

                var attemptHistory = new AssessmentAttemptHistory
                {
                    SessionId    = assessment.Id,
                    UserId       = assessment.UserId,
                    Title        = assessment.Title ?? "",
                    Category     = assessment.Category ?? "",
                    Score        = assessment.Score,
                    IsPassed     = assessment.IsPassed,
                    StartedAt    = assessment.StartedAt,
                    CompletedAt  = assessment.CompletedAt,
                    AttemptNumber = existingAttempts + 1,
                    ArchivedAt   = DateTime.UtcNow
                };
                _context.AssessmentAttemptHistory.Add(attemptHistory);
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
            assessment.ElapsedSeconds = 0;
            assessment.LastActivePage = null;
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

        // --- FORCE CLOSE ALL SESSIONS IN GROUP ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceCloseAll(string title, string category, DateTime scheduleDate)
        {
            // Find all Open or InProgress sessions in this assessment group
            var sessionsToClose = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date
                         && (a.Status == "Open" || a.Status == "InProgress"))
                .ToListAsync();

            if (!sessionsToClose.Any())
            {
                TempData["Error"] = "No Open or InProgress sessions to close.";
                return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
            }

            // Bulk-transition to Abandoned (session period ended -- no score recorded)
            foreach (var session in sessionsToClose)
            {
                session.Status    = "Abandoned";
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Audit log -- one summary entry for the bulk action (AuditLogService saves immediately)
            var actor = await _userManager.GetUserAsync(User);
            var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
            await _auditLog.LogAsync(
                actor?.Id ?? "",
                actorName,
                "ForceCloseAll",
                $"Force-closed all Open/InProgress sessions for '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) -- {sessionsToClose.Count} session(s) closed",
                null,
                "AssessmentSession");

            TempData["Success"] = $"Berhasil menutup {sessionsToClose.Count} sesi ujian.";
            return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
        }

        // --- EXPORT ASSESSMENT RESULTS ---
        [HttpGet]
        public async Task<IActionResult> ExportAssessmentResults(string title, string category, DateTime scheduleDate)
        {
            // Query all sessions in this group (all workers assigned, regardless of completion status)
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
            {
                TempData["Error"] = "No sessions found for this assessment group.";
                return RedirectToAction("ManageAssessment");
            }

            // Detect package mode and load question counts (same pattern as AssessmentMonitoringDetail)
            var siblingIds = sessions.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Build question count map per session
            Dictionary<int, int> questionCountMap = new();
            if (isPackageMode)
            {
                questionCountMap = await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .Join(_context.AssessmentPackages.Include(p => p.Questions),
                        a => a.AssessmentPackageId,
                        p => p.Id,
                        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
                    .ToDictionaryAsync(
                        x => x.AssessmentSessionId,
                        x => x.QuestionCount);
            }
            else
            {
                questionCountMap = await _context.AssessmentQuestions
                    .Where(q => siblingIds.Contains(q.AssessmentSessionId))
                    .GroupBy(q => q.AssessmentSessionId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Count());
            }

            // Build row data: one row per session, include all statuses
            var rows = sessions.Select(a =>
            {
                string userStatus;
                if (a.CompletedAt != null || a.Score != null)
                    userStatus = "Completed";
                else if (a.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (a.StartedAt != null)
                    userStatus = "In Progress";
                else
                    userStatus = "Not Started";

                string resultText = a.IsPassed == true ? "Pass"
                                  : a.IsPassed == false ? "Fail"
                                  : "\u2014";

                return new
                {
                    UserFullName  = a.User?.FullName ?? "Unknown",
                    UserNIP       = a.User?.NIP ?? "",
                    QuestionCount = questionCountMap.ContainsKey(a.Id) ? questionCountMap[a.Id] : 0,
                    UserStatus    = userStatus,
                    Score         = a.Score.HasValue ? (object)a.Score.Value : "\u2014",
                    Result        = resultText,
                    CompletedAt   = a.CompletedAt.HasValue
                                    ? a.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm")
                                    : ""
                };
            })
            .OrderBy(r => r.UserStatus)
            .ThenBy(r => r.UserFullName)
            .ToList();

            // Generate workbook (ClosedXML)
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Results");

            // Header row
            int col = 1;
            worksheet.Cell(1, col++).Value = "Name";
            worksheet.Cell(1, col++).Value = "NIP";
            worksheet.Cell(1, col++).Value = "Jumlah Soal";
            worksheet.Cell(1, col++).Value = "Status";
            worksheet.Cell(1, col++).Value = "Score";
            worksheet.Cell(1, col++).Value = "Result";
            worksheet.Cell(1, col).Value   = "Completed At";

            int totalCols = 7;
            var headerRange = worksheet.Range(1, 1, 1, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var row = i + 2;
                int c = 1;
                worksheet.Cell(row, c++).Value = r.UserFullName;
                worksheet.Cell(row, c++).Value = r.UserNIP;
                worksheet.Cell(row, c++).Value = r.QuestionCount;
                worksheet.Cell(row, c++).Value = r.UserStatus;
                worksheet.Cell(row, c++).Value = r.Score?.ToString() ?? "\u2014";
                worksheet.Cell(row, c++).Value = r.Result;
                worksheet.Cell(row, c).Value   = r.CompletedAt;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            // Sanitize title for filename: replace non-alphanumeric with _
            var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
            var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Results.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // --- USER ASSESSMENT HISTORY ---
        [HttpGet]
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

        // GET /Admin/AuditLog
        [HttpGet]
        public async Task<IActionResult> AuditLog(int page = 1)
        {
            const int pageSize = 25;

            var totalCount = await _context.AuditLogs.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Clamp page to valid range
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(logs);
        }

        // POST /Admin/CloseEarly — score InProgress sessions from submitted answers, lock all
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseEarly(string title, string category, DateTime scheduleDate)
        {
            // Step 1 — Load all sibling sessions
            var allSessions = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!allSessions.Any())
            {
                TempData["Error"] = "Assessment group not found.";
                return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
            }

            // Step 2 — Detect package mode
            var siblingIds = allSessions.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Step 3 — For package mode: preload all packages + questions + options + assignments in bulk
            Dictionary<int, UserPackageAssignment> sessionAssignmentMap = new();
            Dictionary<int, PackageQuestion> allQuestionLookup = new();

            if (isPackageMode)
            {
                var assignments = await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .ToListAsync();
                foreach (var a in assignments)
                    sessionAssignmentMap[a.AssessmentSessionId] = a;

                var allSiblingPackages = await _context.AssessmentPackages
                    .Include(p => p.Questions)
                        .ThenInclude(q => q.Options)
                    .Where(p => siblingIds.Contains(p.AssessmentSessionId))
                    .ToListAsync();
                allQuestionLookup = allSiblingPackages
                    .SelectMany(p => p.Questions)
                    .ToDictionary(q => q.Id);
            }

            // Step 4 — For legacy mode: preload questions + options
            List<AssessmentQuestion> legacyQuestions = new();
            if (!isPackageMode)
            {
                var siblingWithQuestions = await _context.AssessmentSessions
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Where(a => siblingIds.Contains(a.Id) && a.Questions.Any())
                    .FirstOrDefaultAsync();
                legacyQuestions = siblingWithQuestions?.Questions?.ToList() ?? new();
            }

            // Step 5 — Loop over all sessions, set ExamWindowCloseDate, score InProgress sessions
            int inProgressCount = 0;

            foreach (var session in allSessions)
            {
                session.ExamWindowCloseDate = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;

                bool isInProgress = session.StartedAt != null && session.CompletedAt == null && session.Score == null;
                if (!isInProgress) continue;

                inProgressCount++;

                if (isPackageMode)
                {
                    if (!sessionAssignmentMap.TryGetValue(session.Id, out var assignment)) continue;
                    var sessionShuffledIds = assignment.GetShuffledQuestionIds();
                    if (!sessionShuffledIds.Any()) continue;

                    var responses = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == session.Id)
                        .ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId);

                    int totalScore = 0;
                    int maxScore = 0;

                    foreach (var qId in sessionShuffledIds)
                    {
                        if (!allQuestionLookup.TryGetValue(qId, out var q)) continue;
                        maxScore += q.ScoreValue;
                        if (responses.TryGetValue(q.Id, out var optId) && optId.HasValue)
                        {
                            var selectedOption = q.Options.FirstOrDefault(o => o.Id == optId.Value);
                            if (selectedOption != null && selectedOption.IsCorrect)
                                totalScore += q.ScoreValue;
                        }
                    }

                    int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                    session.Score = finalPercentage;
                    session.Status = "Completed";
                    session.Progress = 100;
                    session.IsPassed = finalPercentage >= session.PassPercentage;
                    session.CompletedAt = DateTime.UtcNow;
                    assignment.IsCompleted = true;

                    if (session.IsPassed == true)
                    {
                        var mappedCompetencies = await _context.AssessmentCompetencyMaps
                            .Include(m => m.KkjMatrixItem)
                            .Where(m => m.AssessmentCategory == session.Category &&
                                        (m.TitlePattern == null || session.Title.Contains(m.TitlePattern)))
                            .ToListAsync();

                        if (mappedCompetencies.Any())
                        {
                            var sessionUser = await _context.Users.FindAsync(session.UserId);
                            foreach (var mapping in mappedCompetencies)
                            {
                                if (mapping.MinimumScoreRequired.HasValue && session.Score < mapping.MinimumScoreRequired.Value)
                                    continue;

                                var existingLevel = await _context.UserCompetencyLevels
                                    .FirstOrDefaultAsync(c => c.UserId == session.UserId &&
                                                              c.KkjMatrixItemId == mapping.KkjMatrixItemId);
                                if (existingLevel == null)
                                {
                                    int targetLevel = PositionTargetHelper.GetTargetLevel(mapping.KkjMatrixItem!, sessionUser?.Position);
                                    _context.UserCompetencyLevels.Add(new UserCompetencyLevel
                                    {
                                        UserId = session.UserId,
                                        KkjMatrixItemId = mapping.KkjMatrixItemId,
                                        CurrentLevel = mapping.LevelGranted,
                                        TargetLevel = targetLevel,
                                        Source = "Assessment",
                                        AssessmentSessionId = session.Id,
                                        AchievedAt = DateTime.UtcNow
                                    });
                                }
                                else if (mapping.LevelGranted > existingLevel.CurrentLevel)
                                {
                                    existingLevel.CurrentLevel = mapping.LevelGranted;
                                    existingLevel.Source = "Assessment";
                                    existingLevel.AssessmentSessionId = session.Id;
                                    existingLevel.UpdatedAt = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                }
                else
                {
                    var userResponses = await _context.UserResponses
                        .Where(r => r.AssessmentSessionId == session.Id)
                        .ToDictionaryAsync(r => r.AssessmentQuestionId, r => r.SelectedOptionId);

                    int totalScore = 0;
                    int maxScore = 0;

                    foreach (var question in legacyQuestions)
                    {
                        maxScore += question.ScoreValue;
                        if (userResponses.TryGetValue(question.Id, out var selectedOptionId) && selectedOptionId.HasValue)
                        {
                            var selectedOption = question.Options.FirstOrDefault(o => o.Id == selectedOptionId.Value);
                            if (selectedOption != null && selectedOption.IsCorrect)
                                totalScore += question.ScoreValue;
                        }
                    }

                    int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

                    session.Score = finalPercentage;
                    session.Status = "Completed";
                    session.Progress = 100;
                    session.IsPassed = finalPercentage >= session.PassPercentage;
                    session.CompletedAt = DateTime.UtcNow;

                    if (session.IsPassed == true)
                    {
                        var mappedCompetencies = await _context.AssessmentCompetencyMaps
                            .Include(m => m.KkjMatrixItem)
                            .Where(m => m.AssessmentCategory == session.Category &&
                                        (m.TitlePattern == null || session.Title.Contains(m.TitlePattern)))
                            .ToListAsync();

                        if (mappedCompetencies.Any())
                        {
                            var sessionUser = await _context.Users.FindAsync(session.UserId);
                            foreach (var mapping in mappedCompetencies)
                            {
                                if (mapping.MinimumScoreRequired.HasValue && session.Score < mapping.MinimumScoreRequired.Value)
                                    continue;

                                var existingLevel = await _context.UserCompetencyLevels
                                    .FirstOrDefaultAsync(c => c.UserId == session.UserId &&
                                                              c.KkjMatrixItemId == mapping.KkjMatrixItemId);
                                if (existingLevel == null)
                                {
                                    int targetLevel = PositionTargetHelper.GetTargetLevel(mapping.KkjMatrixItem!, sessionUser?.Position);
                                    _context.UserCompetencyLevels.Add(new UserCompetencyLevel
                                    {
                                        UserId = session.UserId,
                                        KkjMatrixItemId = mapping.KkjMatrixItemId,
                                        CurrentLevel = mapping.LevelGranted,
                                        TargetLevel = targetLevel,
                                        Source = "Assessment",
                                        AssessmentSessionId = session.Id,
                                        AchievedAt = DateTime.UtcNow
                                    });
                                }
                                else if (mapping.LevelGranted > existingLevel.CurrentLevel)
                                {
                                    existingLevel.CurrentLevel = mapping.LevelGranted;
                                    existingLevel.Source = "Assessment";
                                    existingLevel.AssessmentSessionId = session.Id;
                                    existingLevel.UpdatedAt = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                }
            }

            // Step 6 — SaveChangesAsync + cache invalidation + audit log + redirect
            await _context.SaveChangesAsync();

            foreach (var s in allSessions)
                _cache.Remove($"exam-status-{s.Id}");

            var actor = await _userManager.GetUserAsync(User);
            var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
            await _auditLog.LogAsync(
                actor?.Id ?? "",
                actorName,
                "CloseEarly",
                $"Closed early assessment group '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) — {inProgressCount} session(s) scored from answers, {allSessions.Count} total session(s) locked",
                null,
                "AssessmentSession");

            TempData["Success"] = $"Assessment group ditutup lebih awal. {inProgressCount} sesi diberi skor berdasarkan jawaban yang sudah dikerjakan.";
            return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
        }

        // POST /Admin/ReshufflePackage — reshuffle package for single worker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReshufflePackage(int sessionId)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == sessionId);

            if (assessment == null)
                return Json(new { success = false, message = "Session not found." });

            string userStatus;
            if (assessment.CompletedAt != null || assessment.Score != null)
                userStatus = "Completed";
            else if (assessment.Status == "Abandoned")
                userStatus = "Abandoned";
            else if (assessment.StartedAt != null)
                userStatus = "InProgress";
            else
                userStatus = "Not started";

            if (userStatus != "Not started" && userStatus != "Abandoned")
                return Json(new { success = false, message = "Hanya peserta yang belum mulai atau sesi yang ditinggalkan yang dapat di-reshuffle." });

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

            if (!packages.Any())
                return Json(new { success = false, message = "Assessment ini tidak menggunakan mode paket." });

            var currentAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);

            if (currentAssignment != null)
                _context.UserPackageAssignments.Remove(currentAssignment);

            var rng = new Random();
            var shuffledIds = BuildCrossPackageAssignment(packages, rng);
            var sentinelPackage = packages.First();

            var newAssignment = new UserPackageAssignment
            {
                AssessmentSessionId = sessionId,
                AssessmentPackageId = sentinelPackage.Id,
                UserId = assessment.UserId,
                ShuffledQuestionIds = System.Text.Json.JsonSerializer.Serialize(shuffledIds),
                ShuffledOptionIdsPerQuestion = "{}"
            };
            _context.UserPackageAssignments.Add(newAssignment);

            await _context.SaveChangesAsync();

            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = $"{hcUser?.NIP ?? "?"} - {hcUser?.FullName ?? "Unknown"}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ReshufflePackage",
                    $"Reshuffled package (cross-package) for user {assessment.UserId} on assessment '{assessment.Title}' [SessionID={sessionId}]: {shuffledIds.Count} questions from {packages.Count} packages",
                    sessionId,
                    "AssessmentSession");
            }
            catch { /* audit failure must not roll back the successful reshuffle */ }

            return Json(new { success = true, packageName = $"Cross-package ({packages.Count} paket)", assignmentId = newAssignment.Id });
        }

        // POST /Admin/ReshuffleAll — bulk reshuffle for all workers in assessment group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReshuffleAll(string title, string category, DateTime scheduleDate)
        {
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title &&
                            a.Category == category &&
                            a.Schedule.Date == scheduleDate.Date)
                .ToListAsync();

            if (!sessions.Any())
                return Json(new { success = false, message = "Assessment group not found." });

            var siblingSessionIds = sessions.Select(s => s.Id).ToList();
            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            if (!packages.Any())
                return Json(new { success = false, message = "Assessment ini tidak menggunakan mode paket." });

            var existingAssignments = await _context.UserPackageAssignments
                .Where(a => siblingSessionIds.Contains(a.AssessmentSessionId))
                .ToDictionaryAsync(a => a.AssessmentSessionId);

            var rng = new Random();
            var results = new List<object>();
            int reshuffledCount = 0;

            foreach (var session in sessions)
            {
                string userName = session.User?.FullName ?? "Unknown";

                string userStatus;
                if (session.CompletedAt != null || session.Score != null)
                    userStatus = "Completed";
                else if (session.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (session.StartedAt != null)
                    userStatus = "InProgress";
                else
                    userStatus = "Not started";

                if (userStatus != "Not started")
                {
                    string reason = userStatus == "InProgress" ? "sedang mengerjakan"
                                  : userStatus == "Completed" ? "sudah selesai"
                                  : "dibatalkan";
                    results.Add(new { name = userName, status = $"Dilewati — {reason}" });
                    continue;
                }

                existingAssignments.TryGetValue(session.Id, out var existingAssignment);
                var sessionShuffledIds = BuildCrossPackageAssignment(packages, rng);
                var sentinelPackage = packages.First();

                if (existingAssignment != null)
                    _context.UserPackageAssignments.Remove(existingAssignment);

                _context.UserPackageAssignments.Add(new UserPackageAssignment
                {
                    AssessmentSessionId = session.Id,
                    AssessmentPackageId = sentinelPackage.Id,
                    UserId = session.UserId,
                    ShuffledQuestionIds = System.Text.Json.JsonSerializer.Serialize(sessionShuffledIds),
                    ShuffledOptionIdsPerQuestion = "{}"
                });

                results.Add(new { name = userName, status = $"Reshuffled (cross-package, {packages.Count} paket)" });
                reshuffledCount++;
            }

            await _context.SaveChangesAsync();

            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = $"{hcUser?.NIP ?? "?"} - {hcUser?.FullName ?? "Unknown"}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ReshuffleAll",
                    $"Bulk reshuffled {reshuffledCount} worker(s) on assessment '{title}' [{category}] scheduled {scheduleDate:yyyy-MM-dd}",
                    null,
                    "AssessmentSession");
            }
            catch { /* audit failure must not roll back successful reshuffles */ }

            return Json(new { success = true, results, reshuffledCount });
        }

        #region Helper Methods

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<int> BuildCrossPackageAssignment(List<AssessmentPackage> packages, Random rng)
        {
            if (packages.Count == 0)
                return new List<int>();

            if (packages.Count == 1)
                return packages[0].Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();

            int K = packages.Min(p => p.Questions.Count);
            if (K == 0)
                return new List<int>();

            int N = packages.Count;
            int baseCount = K / N;
            int remainder = K % N;

            var remainderIndices = Enumerable.Range(0, N)
                .OrderBy(_ => rng.Next())
                .Take(remainder)
                .ToHashSet();

            var slots = new List<int>();
            for (int i = 0; i < N; i++)
            {
                int count = baseCount + (remainderIndices.Contains(i) ? 1 : 0);
                for (int j = 0; j < count; j++)
                    slots.Add(i);
            }

            Shuffle(slots, rng);

            var pkgCounter = new int[N];
            var shuffledIds = new List<int>();
            var orderedQuestions = packages.Select(p => p.Questions.OrderBy(q => q.Order).ToList()).ToList();

            for (int pos = 0; pos < K; pos++)
            {
                int pkgIdx = slots[pos];
                var question = orderedQuestions[pkgIdx][pkgCounter[pkgIdx]];
                pkgCounter[pkgIdx]++;
                shuffledIds.Add(question.Id);
            }

            return shuffledIds;
        }

        #endregion
    }
}
