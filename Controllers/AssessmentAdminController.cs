using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using HcPortal.Hubs;
using Microsoft.AspNetCore.SignalR;
using ClosedXML.Excel;
using System.Globalization;
using HcPortal.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace HcPortal.Controllers
{
    [Route("Admin")]
    [Route("Admin/[action]")]
    public class AssessmentAdminController : AdminBaseController
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<AssessmentAdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<AssessmentHub> _hubContext;
        private readonly IWorkerDataService _workerDataService;
        private readonly GradingService _gradingService;

        public AssessmentAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            IMemoryCache cache,
            ILogger<AssessmentAdminController> logger,
            INotificationService notificationService,
            IHubContext<AssessmentHub> hubContext,
            IWorkerDataService workerDataService,
            GradingService gradingService)
            : base(context, userManager, auditLog, env)
        {
            _cache = cache;
            _logger = logger;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _workerDataService = workerDataService;
            _gradingService = gradingService;
        }

        // Override View resolution to use Views/Admin/ folder (controller name is AssessmentAdmin, but views stay in Admin/)
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        // GET /Admin/ManageAssessment
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageAssessment(string? search, int page = 1, int pageSize = 20,
            string? tab = null, string? section = null, string? unit = null,
            string? category = null, string? statusFilter = null, string? isFiltered = null)
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

            // Category filter — applied before DB fetch for efficiency
            if (!string.IsNullOrEmpty(category))
                managementQuery = managementQuery.Where(a => a.Category == category);

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
                    a.AssessmentType,
                    a.LinkedGroupId,
                    UserFullName = a.User != null ? a.User.FullName : "Unknown",
                    UserEmail = a.User != null ? a.User.Email : "",
                    UserId = a.User != null ? a.User.Id : ""
                })
                .ToListAsync();

            var mgPrePostSessions = allSessions.Where(a => a.LinkedGroupId != null).ToList();
            var mgStandardSessions = allSessions.Where(a => a.LinkedGroupId == null).ToList();

            // Pre-Post: group by LinkedGroupId
            var prePostGrouped = mgPrePostSessions
                .GroupBy(a => a.LinkedGroupId)
                .Select(g => {
                    var rep = g.Where(a => a.AssessmentType == "PreTest").OrderBy(a => a.CreatedAt).FirstOrDefault() ?? g.OrderBy(a => a.CreatedAt).First();
                    string groupStatus;
                    if (g.Any(a => a.Status == "Open" || a.Status == "InProgress")) groupStatus = "Open";
                    else if (g.Any(a => a.Status == "Upcoming")) groupStatus = "Upcoming";
                    else groupStatus = "Closed";
                    return new {
                        rep.Title, rep.Category, rep.Schedule, rep.ExamWindowCloseDate, rep.DurationMinutes,
                        rep.Status, rep.IsTokenRequired, rep.AccessToken, rep.PassPercentage, rep.AllowAnswerReview,
                        RepresentativeId = rep.Id,
                        Users = g.Where(a => a.AssessmentType == "PreTest").Select(a => new { a.UserFullName, a.UserEmail, a.UserId }).ToList<dynamic>(),
                        AllIds = g.Select(a => a.Id).ToList(),
                        UserCount = g.Where(a => a.AssessmentType == "PreTest").Count(),
                        GroupStatus = groupStatus,
                        IsPrePostGroup = true,
                        LinkedGroupId = g.Key
                    };
                }).ToList<dynamic>();

            // Standard: existing logic
            var standardGrouped = mgStandardSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g => {
                    var rep = g.OrderBy(a => a.CreatedAt).First();
                    string groupStatus;
                    if (g.Any(a => a.Status == "Open" || a.Status == "InProgress")) groupStatus = "Open";
                    else if (g.Any(a => a.Status == "Upcoming")) groupStatus = "Upcoming";
                    else groupStatus = "Closed";
                    return new {
                        rep.Title, rep.Category, rep.Schedule, rep.ExamWindowCloseDate, rep.DurationMinutes,
                        rep.Status, rep.IsTokenRequired, rep.AccessToken, rep.PassPercentage, rep.AllowAnswerReview,
                        RepresentativeId = rep.Id,
                        Users = g.Select(a => new { a.UserFullName, a.UserEmail, a.UserId }).ToList<dynamic>(),
                        AllIds = g.Select(a => a.Id).ToList(),
                        UserCount = g.Count(),
                        GroupStatus = groupStatus,
                        IsPrePostGroup = false,
                        LinkedGroupId = (int?)null
                    };
                }).ToList<dynamic>();

            var grouped = prePostGrouped.Concat(standardGrouped)
                .OrderByDescending(g => g.Schedule)
                .ToList();

            // Status filter — applied AFTER grouping (GroupStatus computed from sessions)
            // Default: show Open + Upcoming only (exclude Closed) unless statusFilter param is provided
            if (string.IsNullOrEmpty(statusFilter))
                grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
            else if (statusFilter == "Open" || statusFilter == "Upcoming" || statusFilter == "Closed")
                grouped = grouped.Where(g => g.GroupStatus == statusFilter).ToList();
            // statusFilter == "All" → no filter applied

            // Fetch distinct categories for dropdown
            ViewBag.Categories = await _context.AssessmentSessions
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            ViewBag.SelectedCategory = category ?? "";
            ViewBag.SelectedStatus = statusFilter ?? "";

            var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);

            ViewBag.ManagementData = grouped
                .Skip(paging.Skip)
                .Take(paging.Take)
                .ToList();
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.SearchTerm = search;

            // Tab routing — default to "assessment"
            var activeTab = tab switch { "training" => "training", "history" => "history", _ => "assessment" };
            ViewBag.ActiveTab = activeTab;

            // Training tab data (lazy — only fetch when tab=training or tab=history)
            if (activeTab == "training" || activeTab == "history")
            {
                bool isInitialState = string.IsNullOrEmpty(isFiltered);
                ViewBag.IsInitialState = isInitialState;
                ViewBag.SelectedSection = section;
                ViewBag.SelectedUnit = unit;
                ViewBag.SelectedCategory = category;
                ViewBag.SelectedStatus = statusFilter;

                List<WorkerTrainingStatus> workers;
                if (isInitialState)
                    workers = new List<WorkerTrainingStatus>();
                else
                    workers = await _workerDataService.GetWorkersInSection(section, unit, category, search, statusFilter);

                var (assessmentHistory, trainingHistory) = await _workerDataService.GetAllWorkersHistory();
                ViewBag.Workers = workers;
                ViewBag.AssessmentHistory = assessmentHistory;
                ViewBag.TrainingHistory = trainingHistory;
                ViewBag.AssessmentTitles = assessmentHistory
                    .Select(r => r.Title)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct().OrderBy(t => t).ToList();
                ViewBag.TrainingSections = await _context.GetAllSectionsAsync();
            }

            return View();
        }

        // --- CATEGORY MANAGEMENT ---

        private async Task SetCategoriesViewBag()
        {
            // Hierarchical tree for ManageCategories table + optgroup dropdowns
            var parentCategories = await _context.AssessmentCategories
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
                    .ThenInclude(ch => ch.Children.OrderBy(gc => gc.SortOrder).ThenBy(gc => gc.Name))
                .Include(c => c.Children)
                    .ThenInclude(ch => ch.Signatory)
                .Include(c => c.Children)
                    .ThenInclude(ch => ch.Children)
                        .ThenInclude(gc => gc.Signatory)
                .Include(c => c.Signatory)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.ParentCategories = parentCategories;

            // All users for signatory dropdown
            var allUsers = await _userManager.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.NIP, u.Position })
                .ToListAsync();
            ViewBag.AllUsers = allUsers;

            // Potential parents for Parent Category dropdown (depth 0 or 1 only)
            var potentialParents = await _context.AssessmentCategories
                .Include(c => c.Parent)
                .Where(c => c.ParentId == null || (c.Parent != null && c.Parent.ParentId == null))
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.PotentialParents = potentialParents;
        }

        private async Task SetTrainingCategoryViewBag()
        {
            var allCats = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            ViewBag.KategoriOptions = allCats
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToList();

            ViewBag.SubKategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId != null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageCategories()
        {
            await SetCategoriesViewBag();
            var parentCategories = (IEnumerable<AssessmentCategory>)ViewBag.ParentCategories;
            return View(parentCategories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, int defaultPassPercentage, int sortOrder, int? parentId, string? signatoryUserId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Nama kategori tidak boleh kosong.";
                return RedirectToAction("ManageCategories");
            }

            if (await _context.AssessmentCategories.AnyAsync(c => c.Name == name))
            {
                TempData["Error"] = "Nama kategori sudah digunakan. Gunakan nama yang berbeda.";
                return RedirectToAction("ManageCategories");
            }

            var category = new AssessmentCategory
            {
                Name = name.Trim(),
                DefaultPassPercentage = defaultPassPercentage,
                SortOrder = sortOrder,
                IsActive = true,
                ParentId = parentId,
                SignatoryUserId = string.IsNullOrWhiteSpace(signatoryUserId) ? null : signatoryUserId
            };
            _context.AssessmentCategories.Add(category);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "AddCategory",
                $"Added assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Kategori berhasil ditambahkan.";
            return RedirectToAction("ManageCategories");
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            await SetCategoriesViewBag();
            ViewBag.EditCategory = category;
            var parentCategories = (IEnumerable<AssessmentCategory>)ViewBag.ParentCategories;
            return View("ManageCategories", parentCategories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, int defaultPassPercentage, int sortOrder, int? parentId, string? signatoryUserId)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Nama kategori tidak boleh kosong.";
                return RedirectToAction("ManageCategories");
            }

            if (await _context.AssessmentCategories.AnyAsync(c => c.Name == name && c.Id != id))
            {
                TempData["Error"] = "Nama kategori sudah digunakan. Gunakan nama yang berbeda.";
                return RedirectToAction("ManageCategories");
            }

            category.Name = name.Trim();
            category.DefaultPassPercentage = defaultPassPercentage;
            category.SortOrder = sortOrder;
            category.ParentId = parentId;
            category.SignatoryUserId = string.IsNullOrWhiteSpace(signatoryUserId) ? null : signatoryUserId;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "EditCategory",
                $"Updated assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Kategori berhasil diperbarui.";
            return RedirectToAction("ManageCategories");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (await _context.AssessmentCategories.AnyAsync(c => c.ParentId == id))
            {
                TempData["Error"] = "Hapus sub-kategori terlebih dahulu.";
                return RedirectToAction("ManageCategories");
            }

            _context.AssessmentCategories.Remove(category);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteCategory",
                $"Deleted assessment category '{category.Name}'",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Kategori berhasil dihapus.";
            return RedirectToAction("ManageCategories");
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var category = await _context.AssessmentCategories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "ToggleCategoryActive",
                $"Toggled category '{category.Name}' to {(category.IsActive ? "Active" : "Inactive")}",
                category.Id, "AssessmentCategory");

            TempData["Success"] = "Status kategori berhasil diubah.";
            return RedirectToAction("ManageCategories");
        }

        // GET /Admin/ExportCategoriesExcel
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportCategoriesExcel()
        {
            var categories = await _context.AssessmentCategories
                .Include(c => c.Parent)
                .Include(c => c.Signatory)
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                    .ThenBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Kategori Assessment",
                new[] { "No", "Nama Kategori", "Kategori Induk", "Passing Grade (%)", "Penandatangan", "Urutan", "Status" });
            ws.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < categories.Count; i++)
            {
                var c = categories[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = c.Name;
                ws.Cell(i + 2, 3).Value = c.Parent?.Name ?? "";
                ws.Cell(i + 2, 4).Value = c.DefaultPassPercentage;
                ws.Cell(i + 2, 5).Value = c.Signatory?.FullName ?? "—";
                ws.Cell(i + 2, 6).Value = c.SortOrder;
                ws.Cell(i + 2, 7).Value = c.IsActive ? "Aktif" : "Nonaktif";
            }

            var fileName = $"KategoriAssessment_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // GET /Admin/ExportCategoriesPdf
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportCategoriesPdf()
        {
            var categories = await _context.AssessmentCategories
                .Include(c => c.Parent)
                .Include(c => c.Signatory)
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                    .ThenBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Kategori Assessment").FontSize(14).Bold();
                        col.Item().Text($"Tanggal export: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(9);
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(30);    // No
                                cols.RelativeColumn(3);     // Nama
                                cols.RelativeColumn(2);     // Kategori Induk
                                cols.ConstantColumn(50);    // Passing Grade
                                cols.RelativeColumn(2);     // Penandatangan
                                cols.ConstantColumn(40);    // Urutan
                                cols.ConstantColumn(50);    // Status
                            });

                            foreach (var header in new[] { "No", "Nama Kategori", "Kategori Induk", "PG (%)", "Penandatangan", "Urutan", "Status" })
                            {
                                table.Cell().Border(0.5f).Padding(3).Text(header).FontSize(8).Bold();
                            }

                            for (int i = 0; i < categories.Count; i++)
                            {
                                var c = categories[i];
                                table.Cell().Border(0.5f).Padding(2).Text($"{i + 1}").FontSize(7);
                                table.Cell().Border(0.5f).Padding(2).Text(c.Name).FontSize(7);
                                table.Cell().Border(0.5f).Padding(2).Text(c.Parent?.Name ?? "").FontSize(7);
                                table.Cell().Border(0.5f).Padding(2).Text($"{c.DefaultPassPercentage}").FontSize(7);
                                table.Cell().Border(0.5f).Padding(2).Text(c.Signatory?.FullName ?? "—").FontSize(7);
                                table.Cell().Border(0.5f).Padding(2).Text($"{c.SortOrder}").FontSize(7);
                                table.Cell().Border(0.5f).Padding(2).Text(c.IsActive ? "Aktif" : "Nonaktif").FontSize(7);
                            }
                        });
                    });
                });
            });

            var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;
            var fileName = $"KategoriAssessment_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }

        // --- CREATE ASSESSMENT ---
        // GET: Show create assessment form
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CreateAssessment([FromQuery] List<int>? renewSessionId = null, [FromQuery] List<int>? renewTrainingId = null)
        {
            // Get list of users for dropdown
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.SelectedUserIds = new List<string>();
            ViewBag.Sections = await _context.GetAllSectionsAsync();
            ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.Categories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var parentCats = await _context.AssessmentCategories
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.ParentCategories = parentCats;

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

            // ===== Phase 201 / 210: Renewal mode pre-fill =====
            bool isRenewalMode = false;

            if (renewSessionId != null && renewSessionId.Count > 0)
            {
                if (renewSessionId.Count == 1)
                {
                    // Single renew — backward-compatible path
                    var sourceSession = await _context.AssessmentSessions
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == renewSessionId[0]);

                    if (sourceSession == null)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    isRenewalMode = true;
                    model.Title = sourceSession.Title;
                    model.Category = sourceSession.Category;
                    model.GenerateCertificate = true;
                    if (sourceSession.ValidUntil.HasValue)
                        model.ValidUntil = sourceSession.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    ViewBag.SelectedUserIds = new List<string> { sourceSession.UserId };
                    ViewBag.RenewalSourceTitle = sourceSession.Title;
                    ViewBag.RenewalSourceUserName = sourceSession.User?.FullName ?? "";
                    ViewBag.RenewsSessionId = renewSessionId[0];
                }
                else
                {
                    // Bulk renew — build per-user FK mapping
                    var sourceSessions = await _context.AssessmentSessions
                        .Include(s => s.User)
                        .Where(s => renewSessionId.Contains(s.Id))
                        .ToListAsync();

                    if (sourceSessions.Count == 0)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    var firstSession = sourceSessions[0];
                    isRenewalMode = true;
                    model.Title = firstSession.Title;
                    model.Category = firstSession.Category;
                    model.GenerateCertificate = true;
                    if (firstSession.ValidUntil.HasValue)
                        model.ValidUntil = firstSession.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    // Build {UserId → SessionId} map (GroupBy to handle duplicate UserId safely)
                    var fkMap = sourceSessions
                        .GroupBy(s => s.UserId)
                        .ToDictionary(g => g.Key, g => g.First().Id);
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(fkMap);
                    ViewBag.RenewalFkMapType = "session";

                    ViewBag.SelectedUserIds = sourceSessions.Select(s => s.UserId).ToList();
                    ViewBag.RenewalSourceTitle = firstSession.Title;
                    ViewBag.RenewalSourceUserName = string.Join(", ", sourceSessions.Select(s => s.User?.FullName ?? ""));
                    // model.RenewsSessionId = null intentionally — resolved per-user at POST
                }
            }
            else if (renewTrainingId != null && renewTrainingId.Count > 0)
            {
                // Build category lookup for MapKategori DB lookup (LDAT-05)
                var catsForRenewal = (await _context.AssessmentCategories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .ToListAsync())
                    .GroupBy(c => c.Name.ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Name);
                if (!catsForRenewal.ContainsKey("MANDATORY")) catsForRenewal["MANDATORY"] = "Mandatory HSSE Training";
                if (!catsForRenewal.ContainsKey("PROTON")) catsForRenewal["PROTON"] = "Assessment Proton";

                if (renewTrainingId.Count == 1)
                {
                    // Single renew — backward-compatible path
                    var sourceTraining = await _context.TrainingRecords
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == renewTrainingId[0]);

                    if (sourceTraining == null)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    isRenewalMode = true;
                    model.Title = sourceTraining.Judul ?? "";
                    model.Category = MapKategori(sourceTraining.Kategori, catsForRenewal);
                    model.GenerateCertificate = true;
                    if (sourceTraining.ValidUntil.HasValue)
                        model.ValidUntil = sourceTraining.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    ViewBag.RenewalSourceTitle = sourceTraining.Judul ?? "";
                    ViewBag.RenewalSourceUserName = sourceTraining.User?.FullName ?? "";
                    ViewBag.RenewsTrainingId = renewTrainingId[0];
                }
                else
                {
                    // Bulk renew — build per-user FK mapping
                    var sourceTrainings = await _context.TrainingRecords
                        .Include(t => t.User)
                        .Where(t => renewTrainingId.Contains(t.Id))
                        .ToListAsync();

                    if (sourceTrainings.Count == 0)
                    {
                        TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
                        return RedirectToAction("CreateAssessment");
                    }

                    var firstTraining = sourceTrainings[0];
                    isRenewalMode = true;
                    model.Title = firstTraining.Judul ?? "";
                    model.Category = MapKategori(firstTraining.Kategori, catsForRenewal);
                    model.GenerateCertificate = true;
                    if (firstTraining.ValidUntil.HasValue)
                        model.ValidUntil = firstTraining.ValidUntil.Value.AddYears(1);
                    else
                        ViewBag.RenewalValidUntilWarning = "Tanggal expired sertifikat asal kosong. Silakan isi ValidUntil secara manual.";

                    // Build {UserId → TrainingRecordId} map (GroupBy to handle duplicate UserId safely)
                    var fkMap = sourceTrainings
                        .GroupBy(t => t.UserId ?? "")
                        .ToDictionary(g => g.Key, g => g.First().Id);
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(fkMap);
                    ViewBag.RenewalFkMapType = "training";

                    ViewBag.SelectedUserIds = sourceTrainings.Select(t => t.UserId).Where(id => id != null).ToList();
                    ViewBag.RenewalSourceTitle = firstTraining.Judul ?? "";
                    ViewBag.RenewalSourceUserName = string.Join(", ", sourceTrainings.Select(t => t.User?.FullName ?? ""));
                    // model.RenewsTrainingId = null intentionally — resolved per-user at POST
                }
            }

            ViewBag.IsRenewalMode = isRenewalMode;
            ViewBag.AssessmentTypeInput = "";

            return View(model);
        }

        // POST: Process form submission (multi-user)
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssessment(
            AssessmentSession model, List<string> UserIds,
            string? RenewalFkMap = null, string? RenewalFkMapType = null,
            string? AssessmentTypeInput = null,
            DateTime? PreSchedule = null, int? PreDurationMinutes = null, DateTime? PreExamWindowCloseDate = null,
            DateTime? PostSchedule = null, int? PostDurationMinutes = null, DateTime? PostExamWindowCloseDate = null,
            bool SamePackage = false)
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

            // Validate category is selected
            if (string.IsNullOrWhiteSpace(model.Category))
            {
                ModelState.AddModelError("Category", "Kategori wajib dipilih.");
            }

            // Rate limiting: max 50 users per request
            if (UserIds != null && UserIds.Count > 50)
            {
                ModelState.AddModelError("UserIds", "Cannot assign to more than 50 users at once. Please split into multiple batches.");
            }

            // Early Pre-Post mode determination (needed before standard field validation)
            bool isPrePostMode = AssessmentTypeInput == "PrePostTest";

            // Validate schedule date (skip for Pre-Post — uses PreSchedule/PostSchedule instead)
            if (!isPrePostMode)
            {
                if (model.Schedule < DateTime.Today)
                {
                    ModelState.AddModelError("Schedule", "Schedule date cannot be in the past.");
                }

                if (model.Schedule > DateTime.Today.AddYears(2))
                {
                    ModelState.AddModelError("Schedule", "Schedule date too far in future (maximum 2 years).");
                }
            }

            // Validate duration (skip for Pre-Post and Assessment Proton Tahun 3)
            if (!isPrePostMode)
            {
                bool isProtonYear3Check = model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue;
                // We'll resolve TahunKe after ModelState check below; for now use DurationMinutes=0 sentinel
                if (!isProtonYear3Check || model.DurationMinutes != 0)
                {
                    if (model.DurationMinutes <= 0)
                    {
                        ModelState.AddModelError("DurationMinutes", "Duration must be greater than 0.");
                    }

                    if (model.DurationMinutes > 480)
                    {
                        ModelState.AddModelError("DurationMinutes", "Duration cannot exceed 480 minutes (8 hours).");
                    }
                }
            }

            // Validate PassPercentage
            if (model.PassPercentage < 0 || model.PassPercentage > 100)
            {
                ModelState.AddModelError("PassPercentage", "Pass Percentage must be between 0 and 100.");
            }

            // ExamWindowCloseDate is optional — remove from ModelState to prevent accidental validation failure
            ModelState.Remove("ExamWindowCloseDate");
            // ValidUntil: opsional di normal mode, wajib di renewal mode
            bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue || !string.IsNullOrEmpty(RenewalFkMap);
            ModelState.Remove("ValidUntil");
            if (isRenewalModePost && !model.ValidUntil.HasValue)
            {
                ModelState.AddModelError("ValidUntil", "Tanggal expired sertifikat wajib diisi untuk renewal.");
            }

            // XOR validation: hanya satu renewal FK yang boleh diisi
            if (model.RenewsSessionId.HasValue && model.RenewsTrainingId.HasValue)
            {
                ModelState.AddModelError("", "Hanya satu renewal FK yang boleh diisi.");
            }
            // Double renewal prevention (per D-10): check if source cert already renewed
            if (model.RenewsSessionId.HasValue)
            {
                var srcAlreadyRenewed = await _context.AssessmentSessions.AnyAsync(a => a.RenewsSessionId == model.RenewsSessionId && a.IsPassed == true)
                    || await _context.TrainingRecords.AnyAsync(t => t.RenewsSessionId == model.RenewsSessionId);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            if (model.RenewsTrainingId.HasValue)
            {
                var srcAlreadyRenewed = await _context.AssessmentSessions.AnyAsync(a => a.RenewsTrainingId == model.RenewsTrainingId && a.IsPassed == true)
                    || await _context.TrainingRecords.AnyAsync(t => t.RenewsTrainingId == model.RenewsTrainingId);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            // Mixed-type bulk validation (per D-11, EDGE-01)
            if (!string.IsNullOrEmpty(RenewalFkMap) && UserIds != null && UserIds.Count > 1)
            {
                if (string.IsNullOrEmpty(RenewalFkMapType) || (RenewalFkMapType != "session" && RenewalFkMapType != "training"))
                {
                    ModelState.AddModelError("", "Bulk renewal tidak dapat mencampur tipe Assessment dan Training. Renew per tipe secara terpisah.");
                }
            }
            // NomorSertifikat is server-generated — remove from ModelState to prevent validation failure
            ModelState.Remove("NomorSertifikat");

            // T-297-01: Validate AssessmentTypeInput hanya "Standard" atau "PrePostTest"
            if (!string.IsNullOrEmpty(AssessmentTypeInput) && AssessmentTypeInput != "Standard" && AssessmentTypeInput != "PrePostTest")
            {
                ModelState.AddModelError("AssessmentTypeInput", "Tipe assessment tidak valid.");
            }

            // isPrePostMode already determined above (before Schedule/Duration validation)

            if (isPrePostMode)
            {
                // Validasi field Pre wajib
                if (!PreSchedule.HasValue)
                    ModelState.AddModelError("PreSchedule", "Jadwal Pre-Test wajib diisi.");
                if (!PreDurationMinutes.HasValue || PreDurationMinutes <= 0)
                    ModelState.AddModelError("PreDurationMinutes", "Durasi Pre-Test harus lebih dari 0.");
                if (PreDurationMinutes > 480)
                    ModelState.AddModelError("PreDurationMinutes", "Durasi Pre-Test tidak boleh lebih dari 480 menit.");

                // Validasi field Post wajib
                if (!PostSchedule.HasValue)
                    ModelState.AddModelError("PostSchedule", "Jadwal Post-Test wajib diisi.");
                if (!PostDurationMinutes.HasValue || PostDurationMinutes <= 0)
                    ModelState.AddModelError("PostDurationMinutes", "Durasi Post-Test harus lebih dari 0.");
                if (PostDurationMinutes > 480)
                    ModelState.AddModelError("PostDurationMinutes", "Durasi Post-Test tidak boleh lebih dari 480 menit.");

                // D-06: Schedule Post harus setelah Pre (T-297-02)
                if (PreSchedule.HasValue && PostSchedule.HasValue && PostSchedule <= PreSchedule)
                    ModelState.AddModelError("PostSchedule", "Jadwal Post-Test harus setelah jadwal Pre-Test.");

                // Override model fields agar validasi standar Schedule/Duration tidak gagal
                if (PreSchedule.HasValue) model.Schedule = PreSchedule.Value;
                if (PreDurationMinutes.HasValue) model.DurationMinutes = PreDurationMinutes.Value;
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                // Reload users for validation error (must match GET structure)
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                    .ToListAsync();

                ViewBag.Users = users;
                ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                ViewBag.Sections = await _context.GetAllSectionsAsync();
                ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
                ViewBag.Categories = await _context.AssessmentCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
                {
                    ViewBag.IsRenewalMode = true;
                    ViewBag.RenewsSessionId = model.RenewsSessionId;
                    ViewBag.RenewsTrainingId = model.RenewsTrainingId;
                    ViewBag.RenewalSourceTitle = model.Title ?? "";
                    ViewBag.RenewalSourceUserName = "";
                }
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
                        .Where(u => u.IsActive)
                        .OrderBy(u => u.FullName)
                        .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                        .ToListAsync();
                    ViewBag.Users = users;
                    ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                    ViewBag.Sections = await _context.GetAllSectionsAsync();
                    ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
                    ViewBag.Categories = await _context.AssessmentCategories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.SortOrder)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
                    {
                        ViewBag.IsRenewalMode = true;
                        ViewBag.RenewsSessionId = model.RenewsSessionId;
                        ViewBag.RenewsTrainingId = model.RenewsTrainingId;
                        ViewBag.RenewalSourceTitle = model.Title ?? "";
                        ViewBag.RenewalSourceUserName = "";
                    }
                    return View(model);
                }

                // Proton exam metadata — look up TahunKe from ProtonTrack
                string? protonTahunKe = null;
                if (model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue)
                {
                    var protonTrack = await _context.ProtonTracks.FindAsync(model.ProtonTrackId.Value);
                    if (protonTrack == null)
                    {
                        TempData["Error"] = "Proton Track tidak ditemukan. Silakan pilih track yang valid.";
                        var users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
                        ViewBag.Users = users;
                        ViewBag.Categories = await _context.AssessmentCategories
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.SortOrder)
                            .ThenBy(c => c.Name)
                            .ToListAsync();
                        if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
                        {
                            ViewBag.IsRenewalMode = true;
                            ViewBag.RenewsSessionId = model.RenewsSessionId;
                            ViewBag.RenewsTrainingId = model.RenewsTrainingId;
                            ViewBag.RenewalSourceTitle = model.Title ?? "";
                            ViewBag.RenewalSourceUserName = "";
                        }
                        return View("CreateAssessment", model);
                    }
                    protonTahunKe = protonTrack.TahunKe;
                }

                // Phase 227 CLEN-04: NomorSertifikat is now generated in SubmitExam (when IsPassed=true).
                // Pre-computation block removed — sessions start with NomorSertifikat = null.

                // Phase 210: Deserialize per-user FK map for bulk renew
                Dictionary<string, int>? fkMap = null;
                bool isSessionMap = RenewalFkMapType == "session";
                if (!string.IsNullOrEmpty(RenewalFkMap))
                {
                    try { fkMap = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(RenewalFkMap); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize RenewalFkMap");
                    }
                }

                // Pre-Post Test mode: buat 2 session per user (Pre + Post) secara transaksional
                if (isPrePostMode)
                {
                    // FASE 1: Buat Pre sessions
                    var preSessions = new List<AssessmentSession>();
                    foreach (var userId in UserIds)
                    {
                        var preSession = new AssessmentSession
                        {
                            Title = model.Title,
                            Category = model.Category,
                            Schedule = PreSchedule!.Value,
                            DurationMinutes = PreDurationMinutes!.Value,
                            ExamWindowCloseDate = PreExamWindowCloseDate,
                            Status = "Upcoming",
                            PassPercentage = model.PassPercentage,
                            AllowAnswerReview = model.AllowAnswerReview,
                            IsTokenRequired = model.IsTokenRequired,
                            AccessToken = model.AccessToken,
                            GenerateCertificate = false,  // D-20: Pre TIDAK generate sertifikat
                            ValidUntil = null,
                            UserId = userId,
                            AssessmentType = "PreTest",
                            CreatedBy = currentUser?.Id,
                            BannerColor = model.BannerColor
                        };
                        preSessions.Add(preSession);
                    }

                    using var pptTransaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.AssessmentSessions.AddRange(preSessions);
                        await _context.SaveChangesAsync(); // Pre sessions get Ids

                        int linkedGroupId = preSessions[0].Id;

                        // FASE 2: Buat Post sessions
                        var postSessions = new List<AssessmentSession>();
                        for (int i = 0; i < UserIds.Count; i++)
                        {
                            var postSession = new AssessmentSession
                            {
                                Title = model.Title,
                                Category = model.Category,
                                Schedule = PostSchedule!.Value,
                                DurationMinutes = PostDurationMinutes!.Value,
                                ExamWindowCloseDate = PostExamWindowCloseDate,
                                Status = "Upcoming",
                                PassPercentage = model.PassPercentage,
                                AllowAnswerReview = model.AllowAnswerReview,
                                IsTokenRequired = model.IsTokenRequired,
                                AccessToken = model.AccessToken,
                                GenerateCertificate = model.GenerateCertificate, // D-21: pilihan HC
                                ValidUntil = model.ValidUntil,
                                UserId = UserIds[i],
                                AssessmentType = "PostTest",
                                LinkedGroupId = linkedGroupId,
                                CreatedBy = currentUser?.Id,
                                BannerColor = model.BannerColor,
                                RenewsSessionId = model.RenewsSessionId, // D-24: renewal FK hanya di Post
                                RenewsTrainingId = model.RenewsTrainingId
                            };
                            postSessions.Add(postSession);
                        }

                        _context.AssessmentSessions.AddRange(postSessions);
                        await _context.SaveChangesAsync(); // Post sessions get Ids

                        // FASE 3: Cross-link LinkedSessionId dan set LinkedGroupId pada Pre
                        for (int i = 0; i < preSessions.Count; i++)
                        {
                            preSessions[i].LinkedGroupId = linkedGroupId;
                            preSessions[i].LinkedSessionId = postSessions[i].Id;
                            postSessions[i].LinkedSessionId = preSessions[i].Id;
                        }
                        await _context.SaveChangesAsync();
                        await pptTransaction.CommitAsync();

                        TempData["Success"] = $"Assessment Pre-Post Test '{model.Title}' berhasil dibuat untuk {UserIds.Count} peserta ({preSessions.Count + postSessions.Count} sesi).";
                        return RedirectToAction("ManageAssessment");
                    }
                    catch (Exception ex)
                    {
                        await pptTransaction.RollbackAsync();
                        _logger.LogError(ex, "Error creating Pre-Post Test assessment");
                        TempData["Error"] = "Gagal membuat assessment Pre-Post Test. Silakan coba lagi.";
                        await SetCategoriesViewBag();
                        await SetTrainingCategoryViewBag();
                        return View(model);
                    }
                }
                // else: flow standard existing continues below...

                // Create all sessions in memory first
                var sessions = new List<AssessmentSession>();

                for (int i = 0; i < UserIds.Count; i++)
                {
                    var userId = UserIds[i];
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
                        GenerateCertificate = model.GenerateCertificate,
                        ExamWindowCloseDate = model.ExamWindowCloseDate,
                        ValidUntil = model.ValidUntil,
                        NomorSertifikat = null, // Phase 227 CLEN-04: generated in SubmitExam when IsPassed=true
                        Progress = 0,
                        UserId = userId,
                        CreatedBy = currentUser?.Id,
                        RenewsSessionId = fkMap != null && isSessionMap && fkMap.TryGetValue(userId, out int sessionFk) ? sessionFk : model.RenewsSessionId,
                        RenewsTrainingId = fkMap != null && !isSessionMap && fkMap.TryGetValue(userId, out int trainingFk) ? trainingFk : model.RenewsTrainingId
                    };

                    // Set Proton-specific fields (nullable — null for non-Proton sessions)
                    if (model.Category == "Assessment Proton")
                    {
                        session.ProtonTrackId = model.ProtonTrackId;
                        session.TahunKe = protonTahunKe;
                        // Tahun 3 = interview only; no DurationMinutes required
                        if (protonTahunKe == "Tahun 3")
                            session.DurationMinutes = 0;
                    }

                    sessions.Add(session);
                }

                // Add all sessions
                _context.AssessmentSessions.AddRange(sessions);

                // Single SaveChanges with transaction (atomicity); retry up to 3 times on UNIQUE violation
                var transaction = await _context.Database.BeginTransactionAsync();
                int attempt = 0;
                const int maxAttempts = 3;
                bool saved = false;
                try
                {
                while (!saved && attempt < maxAttempts)
                {
                    attempt++;
                    try
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (attempt < maxAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
                    {
                        // Phase 227 CLEN-04: NomorSertifikat no longer generated here.
                        // This catch is kept for any other unique constraint violations.
                        foreach (var s in sessions) _context.Entry(s).State = EntityState.Detached;
                        await transaction.RollbackAsync();
                        await transaction.DisposeAsync();
                        transaction = await _context.Database.BeginTransactionAsync();

                        // Re-add sessions for retry (no cert re-assignment needed)
                        for (int j = 0; j < sessions.Count; j++)
                        {
                            sessions[j].Id = 0; // reset for re-insert
                        }
                        _context.AssessmentSessions.AddRange(sessions);
                    }
                }

                if (!saved)
                    throw new InvalidOperationException($"Failed to save assessment after {maxAttempts} attempts due to repeated unique constraint violations");

                    // ASMT-01: Notify each assigned worker
                    foreach (var session in sessions)
                    {
                        try
                        {
                            await _notificationService.SendAsync(
                                session.UserId,
                                "ASMT_ASSIGNED",
                                "Assessment Baru",
                                $"Anda telah di-assign assessment \"{session.Title}\"",
                                $"/CMP/StartExam/{session.Id}"
                            );
                        }
                        catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
                    }

                    // Audit log
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
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
                finally
                {
                    await transaction.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error creating assessment sessions");

                // Audit log for failed creation attempt
                try
                {
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        actorName,
                        "CreateAssessment_Failed",
                        $"Failed to create assessment '{model.Title}' ({model.Category}): {ex.Message}",
                        null,
                        "AssessmentSession");
                }
                catch (Exception auditEx) { _logger.LogWarning(auditEx, "Audit logging failed during CreateAssessment error handling"); }

                // Show error to user
                TempData["Error"] = "Gagal membuat assessment. Silakan coba lagi.";

                // Reload form
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                    .ToListAsync();
                ViewBag.Users = users;
                ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                ViewBag.Sections = await _context.GetAllSectionsAsync();
                ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
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

            return RedirectToAction("CreateAssessment");
        }

        // --- EDIT ASSESSMENT ---
        // GET: Show edit form
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
                return RedirectToAction("ManageAssessment");
            }

            // Detect Pre-Post group
            bool isPrePost = assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest";

            if (isPrePost && assessment.LinkedGroupId.HasValue)
            {
                // Load semua sessions dalam grup Pre-Post
                var groupSessions = await _context.AssessmentSessions
                    .Include(a => a.User)
                    .Where(a => a.LinkedGroupId == assessment.LinkedGroupId)
                    .ToListAsync();

                var preSessions = groupSessions.Where(a => a.AssessmentType == "PreTest").ToList();
                var postSessions = groupSessions.Where(a => a.AssessmentType == "PostTest").ToList();

                ViewBag.IsPrePostGroup = true;
                ViewBag.PreSession = preSessions.FirstOrDefault();
                ViewBag.PostSession = postSessions.FirstOrDefault();
                ViewBag.PreSessionIds = preSessions.Select(s => s.Id).ToList();
                ViewBag.PostSessionIds = postSessions.Select(s => s.Id).ToList();

                // Override siblings: semua users dalam grup (Pre sessions saja untuk avoid duplicates)
                var groupUserIds = preSessions.Where(a => a.User != null).Select(a => a.UserId).Distinct().ToList();
                ViewBag.AssignedUsers = preSessions.Where(a => a.User != null)
                    .Select(a => new
                    {
                        Id = a.Id,
                        FullName = a.User!.FullName ?? "",
                        Email = a.User!.Email ?? "",
                        Section = a.User!.Section ?? "",
                        Status = a.Status ?? "-",
                        CanDelete = a.Status != "InProgress" && a.Status != "Completed"
                    }).ToList<dynamic>();
                ViewBag.AssignedUserIds = groupUserIds;

                // Package count per phase
                var preIds = preSessions.Select(s => s.Id).ToList();
                var postIds = postSessions.Select(s => s.Id).ToList();
                ViewBag.PrePackageCount = await _context.AssessmentPackages.CountAsync(p => preIds.Contains(p.AssessmentSessionId));
                ViewBag.PostPackageCount = await _context.AssessmentPackages.CountAsync(p => postIds.Contains(p.AssessmentSessionId));

                // PackageCount for schedule-change warning (total)
                ViewBag.PackageCount = (int)ViewBag.PrePackageCount + (int)ViewBag.PostPackageCount;
            }
            else
            {
                ViewBag.IsPrePostGroup = false;

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
                        Section = a.User!.Section ?? "",
                        Status = a.Status ?? "-",
                        CanDelete = false
                    }).ToList<dynamic>();

                // Store assigned user IDs so the picker can exclude them
                ViewBag.AssignedUserIds = siblingUserIds;

                // Count packages attached to this assessment's sibling group (for schedule-change warning)
                var siblingIds = siblings.Select(s => s.Id).ToList();
                var packageCount = await _context.AssessmentPackages
                    .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
                ViewBag.PackageCount = packageCount;
            }

            // Get list of all users for the picker
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Sections = await _context.GetAllSectionsAsync();
            ViewBag.OriginalSchedule = assessment.Schedule.ToString("yyyy-MM-dd");
            ViewBag.Categories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var editParentCats = await _context.AssessmentCategories
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.ParentCategories = editParentCats;

            return View(assessment);
        }

        // POST: Update assessment
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssessment(int id, AssessmentSession model, List<string> NewUserIds,
            DateTime? PreSchedule, int? PreDurationMinutes, DateTime? PreExamWindowCloseDate,
            DateTime? PostSchedule, int? PostDurationMinutes, DateTime? PostExamWindowCloseDate,
            List<string>? UserIds)
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("ManageAssessment");
            }

            // --- Pre-Post branch: handle Pre-Post group edit separately ---
            if ((assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest")
                && assessment.LinkedGroupId.HasValue)
            {
                // Validate PostSchedule > PreSchedule (T-297-07)
                if (PreSchedule.HasValue && PostSchedule.HasValue && PostSchedule.Value <= PreSchedule.Value)
                {
                    TempData["Error"] = "Jadwal Post-Test harus setelah jadwal Pre-Test.";
                    return RedirectToAction("EditAssessment", new { id });
                }

                var allGroupSessions = await _context.AssessmentSessions
                    .Where(a => a.LinkedGroupId == assessment.LinkedGroupId)
                    .ToListAsync();

                var preGroup = allGroupSessions.Where(a => a.AssessmentType == "PreTest").ToList();
                var postGroup = allGroupSessions.Where(a => a.AssessmentType == "PostTest").ToList();

                // Update shared fields pada semua sessions
                foreach (var s in allGroupSessions)
                {
                    s.Title = model.Title;
                    s.Category = model.Category;
                    s.PassPercentage = model.PassPercentage;
                    s.AllowAnswerReview = model.AllowAnswerReview;
                    s.IsTokenRequired = model.IsTokenRequired;
                    s.AccessToken = model.IsTokenRequired ? (model.AccessToken ?? s.AccessToken ?? "") : "";
                    s.UpdatedAt = DateTime.UtcNow;
                }

                // Update per-phase fields
                if (PreSchedule.HasValue)
                {
                    foreach (var s in preGroup)
                    {
                        s.Schedule = PreSchedule.Value;
                        if (PreDurationMinutes.HasValue) s.DurationMinutes = PreDurationMinutes.Value;
                        s.ExamWindowCloseDate = PreExamWindowCloseDate;
                    }
                }
                if (PostSchedule.HasValue)
                {
                    foreach (var s in postGroup)
                    {
                        s.Schedule = PostSchedule.Value;
                        if (PostDurationMinutes.HasValue) s.DurationMinutes = PostDurationMinutes.Value;
                        s.ExamWindowCloseDate = PostExamWindowCloseDate;
                        s.GenerateCertificate = model.GenerateCertificate;
                        s.ValidUntil = model.ValidUntil;
                    }
                }

                // D-31: Tambah peserta baru = buat Pre+Post session
                if (UserIds != null && UserIds.Any())
                {
                    var existingUserIds = preGroup.Select(s => s.UserId).ToHashSet();
                    var newUserIds = UserIds.Where(u => !existingUserIds.Contains(u)).ToList();

                    // D-32: Hapus peserta — hapus kedua session Pre+Post untuk user yang dihapus
                    var removedUserIds = existingUserIds.Where(u => !UserIds.Contains(u)).ToList();
                    foreach (var removedUserId in removedUserIds)
                    {
                        var userPreSession = preGroup.FirstOrDefault(s => s.UserId == removedUserId);
                        var userPostSession = postGroup.FirstOrDefault(s => s.UserId == removedUserId);

                        // D-32 validasi: tidak bisa hapus jika Pre atau Post sudah InProgress/Completed (T-297-08)
                        if (userPreSession != null && (userPreSession.Status == "InProgress" || userPreSession.Status == "Completed"))
                        {
                            TempData["Error"] = $"Tidak dapat menghapus peserta — sesi Pre-Test sudah {userPreSession.Status}.";
                            continue;
                        }
                        if (userPostSession != null && (userPostSession.Status == "InProgress" || userPostSession.Status == "Completed"))
                        {
                            TempData["Error"] = $"Tidak dapat menghapus peserta — sesi Post-Test sudah {userPostSession.Status}.";
                            continue;
                        }

                        var sessionsToRemove = new List<AssessmentSession>();
                        if (userPreSession != null) sessionsToRemove.Add(userPreSession);
                        if (userPostSession != null) sessionsToRemove.Add(userPostSession);

                        var sessionIdsToRemove = sessionsToRemove.Select(s => s.Id).ToList();

                        // Cascade delete data terkait
                        var responses = await _context.PackageUserResponses
                            .Where(r => sessionIdsToRemove.Contains(r.AssessmentSessionId))
                            .ToListAsync();
                        if (responses.Any()) _context.PackageUserResponses.RemoveRange(responses);

                        var attempts = await _context.AssessmentAttemptHistory
                            .Where(h => sessionIdsToRemove.Contains(h.SessionId))
                            .ToListAsync();
                        if (attempts.Any()) _context.AssessmentAttemptHistory.RemoveRange(attempts);

                        var packages = await _context.AssessmentPackages
                            .Include(p => p.Questions).ThenInclude(q => q.Options)
                            .Where(p => sessionIdsToRemove.Contains(p.AssessmentSessionId))
                            .ToListAsync();
                        foreach (var pkg in packages)
                        {
                            foreach (var q in pkg.Questions)
                                _context.PackageOptions.RemoveRange(q.Options);
                            _context.PackageQuestions.RemoveRange(pkg.Questions);
                        }
                        if (packages.Any()) _context.AssessmentPackages.RemoveRange(packages);

                        _context.AssessmentSessions.RemoveRange(sessionsToRemove);
                    }

                    // Tambah peserta baru
                    foreach (var newUserId in newUserIds)
                    {
                        var repPre = preGroup.First();
                        var repPost = postGroup.First();
                        var linkedGroupId = assessment.LinkedGroupId!.Value;

                        var currentUser = await _userManager.GetUserAsync(User);
                        var newPre = new AssessmentSession
                        {
                            Title = model.Title,
                            Category = model.Category,
                            Schedule = PreSchedule ?? repPre.Schedule,
                            DurationMinutes = PreDurationMinutes ?? repPre.DurationMinutes,
                            ExamWindowCloseDate = PreExamWindowCloseDate ?? repPre.ExamWindowCloseDate,
                            Status = "Upcoming",
                            PassPercentage = model.PassPercentage,
                            AllowAnswerReview = model.AllowAnswerReview,
                            IsTokenRequired = model.IsTokenRequired,
                            AccessToken = model.IsTokenRequired ? (model.AccessToken ?? "") : "",
                            GenerateCertificate = false,
                            UserId = newUserId,
                            AssessmentType = "PreTest",
                            LinkedGroupId = linkedGroupId,
                            CreatedBy = currentUser?.Id,
                            BannerColor = repPre.BannerColor
                        };
                        var newPost = new AssessmentSession
                        {
                            Title = model.Title,
                            Category = model.Category,
                            Schedule = PostSchedule ?? repPost.Schedule,
                            DurationMinutes = PostDurationMinutes ?? repPost.DurationMinutes,
                            ExamWindowCloseDate = PostExamWindowCloseDate ?? repPost.ExamWindowCloseDate,
                            Status = "Upcoming",
                            PassPercentage = model.PassPercentage,
                            AllowAnswerReview = model.AllowAnswerReview,
                            IsTokenRequired = model.IsTokenRequired,
                            AccessToken = model.IsTokenRequired ? (model.AccessToken ?? "") : "",
                            GenerateCertificate = model.GenerateCertificate,
                            ValidUntil = model.ValidUntil,
                            UserId = newUserId,
                            AssessmentType = "PostTest",
                            LinkedGroupId = linkedGroupId,
                            CreatedBy = currentUser?.Id,
                            BannerColor = repPost.BannerColor
                        };

                        _context.AssessmentSessions.AddRange(newPre, newPost);
                        await _context.SaveChangesAsync();

                        newPre.LinkedSessionId = newPost.Id;
                        newPost.LinkedSessionId = newPre.Id;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Assessment Pre-Post Test berhasil diperbarui.";
                return RedirectToAction("ManageAssessment");
            }
            // --- End Pre-Post branch ---

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

            // Validate editable fields (mirrors CreateAssessment POST validation)
            var editErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Title))
                editErrors.Add("Title is required.");

            if (model.Schedule > DateTime.Today.AddYears(2))
                editErrors.Add("Schedule date too far in future (maximum 2 years).");

            bool editIsProtonYear3 = model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue && model.DurationMinutes == 0;
            if (!editIsProtonYear3)
            {
                if (model.DurationMinutes <= 0)
                    editErrors.Add("Duration must be greater than 0.");
                if (model.DurationMinutes > 480)
                    editErrors.Add("Duration cannot exceed 480 minutes (8 hours).");
            }

            if (model.PassPercentage < 0 || model.PassPercentage > 100)
                editErrors.Add("Pass Percentage must be between 0 and 100.");

            if (model.IsTokenRequired && string.IsNullOrWhiteSpace(model.AccessToken))
                editErrors.Add("Access Token is required when token security is enabled.");

            if (editErrors.Any())
            {
                TempData["Error"] = string.Join(" ", editErrors);
                return RedirectToAction("EditAssessment", new { id });
            }

            // Capture original group key before updating (needed to find siblings)
            var origTitle = assessment.Title;
            var origCategory = assessment.Category;
            var origScheduleDate = assessment.Schedule.Date;

            // Resolve new token value
            string newToken;
            if (model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken))
                newToken = model.AccessToken.ToUpper();
            else if (!model.IsTokenRequired)
                newToken = "";
            else
                newToken = assessment.AccessToken ?? "";

            // Fetch all sibling sessions (same group key) to propagate shared field changes
            var siblings = await _context.AssessmentSessions
                .Where(a => a.Title == origTitle
                         && a.Category == origCategory
                         && a.Schedule.Date == origScheduleDate)
                .ToListAsync();

            // Update shared fields on ALL siblings (including the current session)
            var now = DateTime.UtcNow;
            foreach (var sibling in siblings)
            {
                sibling.Title = model.Title;
                sibling.Category = model.Category;
                sibling.Schedule = model.Schedule;
                sibling.DurationMinutes = model.DurationMinutes;
                sibling.Status = model.Status;
                sibling.BannerColor = model.BannerColor;
                sibling.IsTokenRequired = model.IsTokenRequired;
                sibling.AccessToken = newToken;
                sibling.PassPercentage = model.PassPercentage;
                sibling.AllowAnswerReview = model.AllowAnswerReview;
                sibling.GenerateCertificate = model.GenerateCertificate;
                sibling.ExamWindowCloseDate = model.ExamWindowCloseDate;
                sibling.UpdatedAt = now;
            }

            // InProgress warning: notify if any sibling session is currently in progress
            var hasInProgress = await _context.AssessmentSessions
                .AnyAsync(s => s.Title == origTitle && s.Category == origCategory
                    && s.Schedule.Date == origScheduleDate
                    && s.StartedAt != null && s.CompletedAt == null);
            if (hasInProgress)
            {
                TempData["Warning"] = "Perhatian: Ada peserta yang sedang mengerjakan ujian. Perubahan Title/Category/Schedule tidak akan berlaku untuk sesi yang sedang berjalan.";
            }

            // Fetch actor info before try block so it is available for both edit and bulk-assign audit calls
            var editUser = await _userManager.GetUserAsync(User);
            var editActorName = string.IsNullOrWhiteSpace(editUser?.NIP) ? (editUser?.FullName ?? "Unknown") : $"{editUser.NIP} - {editUser.FullName}";

            try
            {
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
                TempData["Error"] = "Gagal memperbarui assessment. Silakan coba lagi.";
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
                                GenerateCertificate = savedAssessment.GenerateCertificate,
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

                                // ASMT-01: Notify each newly assigned worker
                                foreach (var ns in newSessions)
                                {
                                    try
                                    {
                                        await _notificationService.SendAsync(
                                            ns.UserId,
                                            "ASMT_ASSIGNED",
                                            "Assessment Baru",
                                            $"Anda telah di-assign assessment \"{ns.Title}\"",
                                            $"/CMP/StartExam/{ns.Id}"
                                        );
                                    }
                                    catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
                                }

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
                    TempData["Error"] = "Assessment berhasil diperbarui, tetapi gagal menambahkan user. Silakan coba lagi.";
                }
            }

            return RedirectToAction("ManageAssessment");
        }

        // --- DELETE ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessment(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            try
            {
                var assessment = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assessment == null)
                {
                    logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
                    TempData["Error"] = "Assessment not found.";
                    return RedirectToAction("ManageAssessment");
                }

                var assessmentTitle = assessment.Title;
                logger.LogInformation($"Attempting to delete assessment {id}: {assessmentTitle}");

                // D-19: Block delete individual jika bagian Pre-Post group
                if (assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest")
                {
                    TempData["Error"] = "Sesi ini bagian dari grup Pre-Post Test. Gunakan 'Hapus Grup' untuk menghapus keduanya.";
                    return RedirectToAction("ManageAssessment");
                }

                // Delete PackageUserResponses (Restrict FK — must be removed before session)
                var pkgResponses = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == id)
                    .ToListAsync();
                if (pkgResponses.Any())
                {
                    logger.LogInformation($"Deleting {pkgResponses.Count} package user responses");
                    _context.PackageUserResponses.RemoveRange(pkgResponses);
                }

                // Delete AssessmentAttemptHistory rows (no FK — orphaned if not removed)
                var attemptHistory = await _context.AssessmentAttemptHistory
                    .Where(h => h.SessionId == id)
                    .ToListAsync();
                if (attemptHistory.Any())
                {
                    logger.LogInformation($"Deleting {attemptHistory.Count} attempt history records");
                    _context.AssessmentAttemptHistory.RemoveRange(attemptHistory);
                }

                // Explicit cleanup: AssessmentPackages + nested Questions + Options
                // (DB may cascade, but explicit removal prevents ordering issues)
                var packages = await _context.AssessmentPackages
                    .Include(p => p.Questions).ThenInclude(q => q.Options)
                    .Where(p => p.AssessmentSessionId == id)
                    .ToListAsync();
                if (packages.Any())
                {
                    foreach (var pkg in packages)
                    {
                        foreach (var q in pkg.Questions)
                            _context.PackageOptions.RemoveRange(q.Options);
                        _context.PackageQuestions.RemoveRange(pkg.Questions);
                    }
                    _context.AssessmentPackages.RemoveRange(packages);
                    logger.LogInformation($"Deleting {packages.Count} packages with their questions/options");
                }

                // Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)

                // Finally delete the assessment itself
                _context.AssessmentSessions.Remove(assessment);

                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var deleteUser = await _userManager.GetUserAsync(User);
                    var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP) ? (deleteUser?.FullName ?? "Unknown") : $"{deleteUser.NIP} - {deleteUser.FullName}";
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
                TempData["Success"] = $"Assessment '{assessmentTitle}' has been deleted successfully.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting assessment {Id}", id);
                TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }
        }

        // --- DELETE ASSESSMENT GROUP ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentGroup(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            try
            {
                // Load representative to get grouping key
                var rep = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (rep == null)
                {
                    logger.LogWarning($"DeleteAssessmentGroup: representative session {id} not found");
                    TempData["Error"] = "Assessment not found.";
                    return RedirectToAction("ManageAssessment");
                }

                var scheduleDate = rep.Schedule.Date;

                // Find all siblings (same Title + Category + Schedule.Date)
                var siblings = await _context.AssessmentSessions
                    .Where(a =>
                        a.Title == rep.Title &&
                        a.Category == rep.Category &&
                        a.Schedule.Date == scheduleDate)
                    .ToListAsync();

                logger.LogInformation($"DeleteAssessmentGroup: deleting {siblings.Count} sessions for '{rep.Title}'");

                var siblingIds = siblings.Select(s => s.Id).ToList();

                // Delete PackageUserResponses for all siblings (Restrict FK — must be removed before sessions)
                var allPkgResponses = await _context.PackageUserResponses
                    .Where(r => siblingIds.Contains(r.AssessmentSessionId))
                    .ToListAsync();
                if (allPkgResponses.Any())
                    _context.PackageUserResponses.RemoveRange(allPkgResponses);

                // Delete AssessmentAttemptHistory for all siblings (no FK — orphaned if not removed)
                var allAttemptHistory = await _context.AssessmentAttemptHistory
                    .Where(h => siblingIds.Contains(h.SessionId))
                    .ToListAsync();
                if (allAttemptHistory.Any())
                    _context.AssessmentAttemptHistory.RemoveRange(allAttemptHistory);

                // Explicit cleanup: AssessmentPackages + nested Questions + Options for all siblings
                var allPackages = await _context.AssessmentPackages
                    .Include(p => p.Questions).ThenInclude(q => q.Options)
                    .Where(p => siblingIds.Contains(p.AssessmentSessionId))
                    .ToListAsync();
                if (allPackages.Any())
                {
                    foreach (var pkg in allPackages)
                    {
                        foreach (var q in pkg.Questions)
                            _context.PackageOptions.RemoveRange(q.Options);
                        _context.PackageQuestions.RemoveRange(pkg.Questions);
                    }
                    _context.AssessmentPackages.RemoveRange(allPackages);
                    logger.LogInformation($"DeleteAssessmentGroup: deleting {allPackages.Count} packages with their questions/options");
                }

                // Note: UserPackageAssignments are cascade-deleted by DB (Cascade FK on AssessmentSessionId)

                foreach (var session in siblings)
                {
                    _context.AssessmentSessions.Remove(session);
                }

                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var dgUser = await _userManager.GetUserAsync(User);
                    var dgActorName = string.IsNullOrWhiteSpace(dgUser?.NIP) ? (dgUser?.FullName ?? "Unknown") : $"{dgUser.NIP} - {dgUser.FullName}";
                    await _auditLog.LogAsync(
                        dgUser?.Id ?? "",
                        dgActorName,
                        "DeleteAssessmentGroup",
                        $"Deleted assessment group '{rep.Title}' ({rep.Category}) — {siblings.Count} session(s)",
                        id,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessmentGroup {Id}", id);
                }

                logger.LogInformation($"DeleteAssessmentGroup: successfully deleted group '{rep.Title}'");
                TempData["Success"] = $"Assessment '{rep.Title}' and all {siblings.Count} assignment(s) deleted.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeleteAssessmentGroup error for representative {Id}", id);
                TempData["Error"] = "Gagal menghapus grup assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }
        }

        // --- DELETE PRE-POST GROUP (D-18, D-19) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrePostGroup(int linkedGroupId)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            try
            {
                // Find semua sessions dalam Pre-Post group
                var groupSessions = await _context.AssessmentSessions
                    .Where(a => a.LinkedGroupId == linkedGroupId)
                    .ToListAsync();

                if (!groupSessions.Any())
                {
                    TempData["Error"] = "Grup Pre-Post Test tidak ditemukan.";
                    return RedirectToAction("ManageAssessment");
                }

                var groupTitle = groupSessions.First().Title;
                var groupIds = groupSessions.Select(s => s.Id).ToList();

                logger.LogInformation($"DeletePrePostGroup: deleting {groupSessions.Count} sessions for '{groupTitle}' (LinkedGroupId={linkedGroupId})");

                // Cascade delete — ikuti pola DeleteAssessmentGroup:
                // 1. PackageUserResponses
                var allPkgResponses = await _context.PackageUserResponses
                    .Where(r => groupIds.Contains(r.AssessmentSessionId))
                    .ToListAsync();
                if (allPkgResponses.Any())
                    _context.PackageUserResponses.RemoveRange(allPkgResponses);

                // 2. AssessmentAttemptHistory
                var allAttemptHistory = await _context.AssessmentAttemptHistory
                    .Where(h => groupIds.Contains(h.SessionId))
                    .ToListAsync();
                if (allAttemptHistory.Any())
                    _context.AssessmentAttemptHistory.RemoveRange(allAttemptHistory);

                // 3. Packages + Questions + Options
                var allPackages = await _context.AssessmentPackages
                    .Include(p => p.Questions).ThenInclude(q => q.Options)
                    .Where(p => groupIds.Contains(p.AssessmentSessionId))
                    .ToListAsync();
                if (allPackages.Any())
                {
                    foreach (var pkg in allPackages)
                    {
                        foreach (var q in pkg.Questions)
                            _context.PackageOptions.RemoveRange(q.Options);
                        _context.PackageQuestions.RemoveRange(pkg.Questions);
                    }
                    _context.AssessmentPackages.RemoveRange(allPackages);
                }

                // 4. Sessions
                _context.AssessmentSessions.RemoveRange(groupSessions);

                await _context.SaveChangesAsync();

                // Audit log
                try
                {
                    var dpgUser = await _userManager.GetUserAsync(User);
                    var dpgActorName = string.IsNullOrWhiteSpace(dpgUser?.NIP) ? (dpgUser?.FullName ?? "Unknown") : $"{dpgUser.NIP} - {dpgUser.FullName}";
                    await _auditLog.LogAsync(
                        dpgUser?.Id ?? "",
                        dpgActorName,
                        "DeletePrePostGroup",
                        $"Deleted Pre-Post group '{groupTitle}' — {groupSessions.Count} session(s) (LinkedGroupId={linkedGroupId})",
                        linkedGroupId,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeletePrePostGroup {LinkedGroupId}", linkedGroupId);
                }

                TempData["Success"] = $"Grup Pre-Post Test '{groupTitle}' dan semua {groupSessions.Count} sesi berhasil dihapus.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeletePrePostGroup error for LinkedGroupId {LinkedGroupId}", linkedGroupId);
                TempData["Error"] = "Gagal menghapus grup Pre-Post Test. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
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
                var newToken = GenerateSecureToken();
                // Update ALL sibling sessions in the same group (same Title + Category + Schedule.Date)
                var siblings = await _context.AssessmentSessions
                    .Where(a => a.Title == assessment.Title
                             && a.Category == assessment.Category
                             && a.Schedule.Date == assessment.Schedule.Date)
                    .ToListAsync();
                foreach (var sibling in siblings)
                {
                    sibling.AccessToken = newToken;
                    sibling.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                // Audit log
                var regenUser = await _userManager.GetUserAsync(User);
                var regenActorName = string.IsNullOrWhiteSpace(regenUser?.NIP) ? (regenUser?.FullName ?? "Unknown") : $"{regenUser.NIP} - {regenUser.FullName}";
                await _auditLog.LogAsync(
                    regenUser?.Id ?? "",
                    regenActorName,
                    "RegenerateToken",
                    $"Regenerated access token for '{assessment.Title}' ({assessment.Category}, {assessment.Schedule:yyyy-MM-dd}) — {siblings.Count} sibling(s) updated",
                    id,
                    "AssessmentSession");

                return Json(new { success = true, token = newToken, message = "Token regenerated successfully." });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, "Error regenerating token");
                return Json(new { success = false, message = "Gagal regenerate token. Silakan coba lagi." });
            }
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

        // --- ASSESSMENT MONITORING GROUP LIST ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AssessmentMonitoring(
            string? search,
            string? status,
            string? category)
        {
            // 7-day window — same as ManageAssessment
            // 90-review: 7-day window is intentional for monitoring view; Abandoned sessions with no ExamWindowCloseDate
            // fall back to Schedule for the window check and naturally age out after 7 days (expected behavior).
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var query = _context.AssessmentSessions
                .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
                .AsQueryable();

            // Text search by title
            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(lower));
            }

            // Category filter
            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            var allSessions = await query
                .OrderByDescending(a => a.Schedule)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Category,
                    a.Schedule,
                    a.ExamWindowCloseDate,
                    a.Status,
                    a.IsTokenRequired,
                    a.AccessToken,
                    a.CreatedAt,
                    a.AssessmentType,
                    a.LinkedGroupId,
                    a.DurationMinutes,
                    IsCompleted = a.CompletedAt != null,
                    IsPassed = a.IsPassed ?? false,
                    IsStarted = a.StartedAt != null,
                    IsMenungguPenilaian = a.Status == "Menunggu Penilaian",
                    a.HasManualGrading
                })
                .ToListAsync();

            // Pisahkan Pre-Post dari Standard (D-33)
            var prePostSessions = allSessions.Where(a => a.LinkedGroupId != null).ToList();
            var standardSessions = allSessions.Where(a => a.LinkedGroupId == null).ToList();

            // Group Pre-Post by LinkedGroupId
            var prePostGroups = prePostSessions
                .GroupBy(a => a.LinkedGroupId)
                .Select(g =>
                {
                    var preSubs = g.Where(a => a.AssessmentType == "PreTest").ToList();
                    var postSubs = g.Where(a => a.AssessmentType == "PostTest").ToList();
                    var rep = preSubs.OrderBy(a => a.CreatedAt).FirstOrDefault() ?? g.OrderBy(a => a.CreatedAt).First();

                    // D-29: status derived
                    string groupStatus;
                    if (g.Any(a => a.Status == "Open" || a.Status == "InProgress"))
                        groupStatus = "Open";
                    else if (g.Any(a => a.Status == "Upcoming"))
                        groupStatus = "Upcoming";
                    else
                        groupStatus = "Closed";

                    // Helper untuk sub-row status
                    string SubRowStatus(IEnumerable<dynamic> subs)
                    {
                        if (subs.Any(a => (string)a.Status == "Open" || (string)a.Status == "InProgress")) return "Open";
                        if (subs.Any(a => (string)a.Status == "Upcoming")) return "Upcoming";
                        return "Closed";
                    }

                    return new MonitoringGroupViewModel
                    {
                        RepresentativeId = rep.Id,
                        Title = rep.Title,
                        Category = rep.Category,
                        Schedule = rep.Schedule,
                        GroupStatus = groupStatus,
                        // D-11: stat gabungan dari Post
                        TotalCount = postSubs.Count > 0 ? postSubs.Count : preSubs.Count,
                        CompletedCount = postSubs.Count(a => a.IsCompleted),
                        PassedCount = postSubs.Count(a => a.IsPassed),
                        PendingCount = postSubs.Count(a => !a.IsCompleted && !a.IsStarted),
                        InProgressCount = postSubs.Count(a => a.IsStarted && !a.IsCompleted),
                        CancelledCount = g.Count(a => a.Status == "Cancelled"),
                        IsTokenRequired = rep.IsTokenRequired,
                        AccessToken = rep.AccessToken ?? "",
                        IsPrePostGroup = true,
                        LinkedGroupId = g.Key,
                        PreSubRow = preSubs.Any() ? new MonitoringSubRowViewModel
                        {
                            RepresentativeId = preSubs.OrderBy(a => a.CreatedAt).First().Id,
                            Phase = "PreTest",
                            Schedule = preSubs.First().Schedule,
                            DurationMinutes = preSubs.First().DurationMinutes,
                            TotalCount = preSubs.Count,
                            CompletedCount = preSubs.Count(a => a.IsCompleted),
                            PassedCount = preSubs.Count(a => a.IsPassed),
                            PendingCount = preSubs.Count(a => !a.IsCompleted && !a.IsStarted),
                            InProgressCount = preSubs.Count(a => a.IsStarted && !a.IsCompleted),
                            CancelledCount = preSubs.Count(a => a.Status == "Cancelled"),
                            GroupStatus = SubRowStatus(preSubs.Cast<dynamic>())
                        } : null,
                        PostSubRow = postSubs.Any() ? new MonitoringSubRowViewModel
                        {
                            RepresentativeId = postSubs.OrderBy(a => a.CreatedAt).First().Id,
                            Phase = "PostTest",
                            Schedule = postSubs.First().Schedule,
                            DurationMinutes = postSubs.First().DurationMinutes,
                            TotalCount = postSubs.Count,
                            CompletedCount = postSubs.Count(a => a.IsCompleted),
                            PassedCount = postSubs.Count(a => a.IsPassed),
                            PendingCount = postSubs.Count(a => !a.IsCompleted && !a.IsStarted),
                            InProgressCount = postSubs.Count(a => a.IsStarted && !a.IsCompleted),
                            CancelledCount = postSubs.Count(a => a.Status == "Cancelled"),
                            GroupStatus = SubRowStatus(postSubs.Cast<dynamic>())
                        } : null
                    };
                }).ToList();

            // Group Standard by (Title, Category, Schedule.Date) — existing logic
            var standardGroups = standardSessions
                .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
                .Select(g =>
                {
                    var rep = g.OrderBy(a => a.CreatedAt).First();
                    string groupStatus;
                    if (g.Any(a => a.Status == "Open" || a.Status == "InProgress"))
                        groupStatus = "Open";
                    else if (g.Any(a => a.Status == "Upcoming"))
                        groupStatus = "Upcoming";
                    else
                        groupStatus = "Closed";

                    return new MonitoringGroupViewModel
                    {
                        RepresentativeId = rep.Id,
                        Title = rep.Title,
                        Category = rep.Category,
                        Schedule = rep.Schedule,
                        GroupStatus = groupStatus,
                        TotalCount = g.Count(),
                        CompletedCount = g.Count(a => a.IsCompleted),
                        PassedCount = g.Count(a => a.IsPassed),
                        PendingCount = g.Count(a => !a.IsCompleted && !a.IsStarted),
                        IsTokenRequired = rep.IsTokenRequired,
                        AccessToken = rep.AccessToken ?? "",
                        MenungguPenilaianCount = g.Count(a => a.IsMenungguPenilaian)
                    };
                }).ToList();

            // Gabungkan dan sort
            var grouped = prePostGroups.Concat(standardGroups)
                .OrderByDescending(g => g.Schedule)
                .ToList();

            // Status filter — applied AFTER grouping (GroupStatus computed from sessions)
            // Default: show Open + Upcoming only (exclude Closed) unless status param is provided
            if (string.IsNullOrEmpty(status))
            {
                grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
                status = "active"; // signal to view that default active filter is on
            }
            else if (status == "Open" || status == "Upcoming" || status == "Closed")
            {
                grouped = grouped.Where(g => g.GroupStatus == status).ToList();
            }
            // status == "All" → no filter applied

            ViewBag.SearchTerm = search ?? "";
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = category ?? "";

            return View(grouped);
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
                return RedirectToAction("ManageAssessment");
            }

            // Detect package mode: check if any sibling session has packages attached
            var siblingIds = sessions.Select(s => s.Id).ToList();
            var packageCount = await _context.AssessmentPackages
                .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
            var isPackageMode = packageCount > 0;

            // Essay grading: build pending count map per session (Phase 298-05)
            // Only relevant for sessions with HasManualGrading == true
            Dictionary<int, int> essayPendingCountMap = new();
            var manualGradingSessionIds = sessions
                .Where(s => s.HasManualGrading)
                .Select(s => s.Id)
                .ToList();
            if (manualGradingSessionIds.Any())
            {
                var essayPendingRaw = await _context.PackageUserResponses
                    .Where(r => manualGradingSessionIds.Contains(r.AssessmentSessionId) && r.EssayScore == null)
                    .Join(_context.PackageQuestions.Where(q => q.QuestionType == "Essay"),
                        r => r.PackageQuestionId, q => q.Id, (r, q) => r.AssessmentSessionId)
                    .GroupBy(sid => sid)
                    .Select(g => new { SessionId = g.Key, Count = g.Count() })
                    .ToListAsync();
                foreach (var item in essayPendingRaw)
                    essayPendingCountMap[item.SessionId] = item.Count;
            }

            // Build question count map per session
            Dictionary<int, int> questionCountMap = new();
            if (isPackageMode)
            {
                // Package mode: count PackageQuestion rows via UserPackageAssignment -> AssessmentPackage
                // GroupBy handles users with multiple package assignments per session
                questionCountMap = (await _context.UserPackageAssignments
                    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
                    .Join(_context.AssessmentPackages.Include(p => p.Questions),
                        a => a.AssessmentPackageId,
                        p => p.Id,
                        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
                    .ToListAsync())
                    .GroupBy(x => x.AssessmentSessionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.QuestionCount));
            }
            var sessionViewModels = sessions.Select(a =>
            {
                string userStatus;
                if (a.CompletedAt != null)
                    userStatus = "Completed";
                else if (a.Status == "Cancelled")
                    userStatus = "Dibatalkan";
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
                    QuestionCount = questionCountMap.TryGetValue(a.Id, out var qc) ? qc : 0,
                    DurationMinutes = a.DurationMinutes,
                    HasManualGrading = a.HasManualGrading,
                    EssayPendingCount = essayPendingCountMap.TryGetValue(a.Id, out var ep) ? ep : 0
                };
            })
            .OrderBy(s => s.UserStatus)   // Not started before Completed
            .ThenBy(s => s.UserFullName)
            .ToList();

            var firstSession = sessions.First();
            var model = new MonitoringGroupViewModel
            {
                RepresentativeId = firstSession.Id,
                Title    = title,
                Category = category,
                Schedule = firstSession.Schedule,
                Sessions = sessionViewModels,
                TotalCount     = sessionViewModels.Count,
                CompletedCount = sessionViewModels.Count(s => s.UserStatus == "Completed"),
                PassedCount    = sessionViewModels.Count(s => s.IsPassed == true),
                GroupStatus    = sessions.Any(a => a.Status == "Open" || a.Status == "InProgress") ? "Open"
                               : sessions.Any(a => a.Status == "Upcoming") ? "Upcoming" : "Closed",
                IsPackageMode  = isPackageMode,
                PendingCount   = sessionViewModels.Count(s => s.UserStatus == "Not started"),
                CancelledCount = sessionViewModels.Count(s => s.UserStatus == "Dibatalkan"),
                InProgressCount = sessionViewModels.Count(s => s.UserStatus == "InProgress")
            };

            model.IsTokenRequired = firstSession.IsTokenRequired;
            model.AccessToken = firstSession.AccessToken ?? "";

            ViewBag.BackUrl = Url.Action("AssessmentMonitoring", "Admin");

            // Proton Tahun 3 interview form support
            // 90-review: For non-Proton categories, ViewBag.GroupTahunKe is not set. Views that access it via
            // (ViewBag.GroupTahunKe as string) ?? "" handle the null safely — no change needed.
            if (model.Category == "Assessment Proton")
            {
                var repSession = await _context.AssessmentSessions.FindAsync(model.RepresentativeId);
                ViewBag.GroupTahunKe = repSession?.TahunKe ?? "";

                if (repSession?.TahunKe == "Tahun 3")
                {
                    var siblingIds2 = model.Sessions.Select(s => s.Id).ToList();
                    ViewBag.SessionObjects = await _context.AssessmentSessions
                        .Where(s => siblingIds2.Contains(s.Id))
                        .ToListAsync();
                }
            }

            ViewBag.AssessmentBatchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";

            // Essay grading items per session (Phase 298-05)
            // Build map: sessionId -> List<EssayGradingItemViewModel>
            var essayGradingMap = new Dictionary<int, List<EssayGradingItemViewModel>>();
            var manualGradingSessions = model.Sessions.Where(s => s.HasManualGrading).ToList();
            if (manualGradingSessions.Any())
            {
                foreach (var sess in manualGradingSessions)
                {
                    var assignment = await _context.UserPackageAssignments
                        .FirstOrDefaultAsync(a => a.AssessmentSessionId == sess.Id);
                    if (assignment == null) continue;

                    var shuffled = assignment.GetShuffledQuestionIds();
                    var essayQs = await _context.PackageQuestions
                        .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay")
                        .ToListAsync();

                    var essayRespMap = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == sess.Id &&
                               essayQs.Select(q => q.Id).Contains(r.PackageQuestionId))
                        .ToDictionaryAsync(r => r.PackageQuestionId);

                    var items = essayQs.Select((q, idx) => new EssayGradingItemViewModel
                    {
                        QuestionId    = q.Id,
                        DisplayNumber = idx + 1,
                        QuestionText  = q.QuestionText ?? "",
                        Rubrik        = q.Rubrik,
                        TextAnswer    = essayRespMap.TryGetValue(q.Id, out var resp) ? resp.TextAnswer : null,
                        EssayScore    = essayRespMap.TryGetValue(q.Id, out var resp2) ? resp2.EssayScore : null,
                        ScoreValue    = q.ScoreValue
                    }).ToList();

                    essayGradingMap[sess.Id] = items;
                }
            }
            ViewBag.EssayGradingMap = essayGradingMap;

            return View(model);
        }

        // --- SUBMIT ESSAY SCORE (Phase 298-05, D-15/D-16) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEssayScore(int sessionId, int questionId, int score)
        {
            // 1. Load response
            var response = await _context.PackageUserResponses
                .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
            if (response == null)
                return Json(new { success = false, message = "Jawaban tidak ditemukan" });

            // 2. Load question untuk validasi ScoreValue
            var question = await _context.PackageQuestions.FindAsync(questionId);
            if (question == null)
                return Json(new { success = false, message = "Soal tidak ditemukan" });

            // 3. Validasi skor range (T-298-13)
            if (score < 0 || score > question.ScoreValue)
                return Json(new { success = false, message = $"Skor harus antara 0 dan {question.ScoreValue}" });

            // 4. Save EssayScore
            response.EssayScore = score;
            await _context.SaveChangesAsync();

            // 5. Cek berapa Essay masih pending
            var pendingCount = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId)
                .Join(_context.PackageQuestions.Where(q => q.QuestionType == "Essay"),
                    r => r.PackageQuestionId, q => q.Id, (r, q) => r)
                .CountAsync(r => r.EssayScore == null);

            return Json(new { success = true, pendingCount, allGraded = pendingCount == 0 });
        }

        // --- FINALIZE ESSAY GRADING (Phase 298-05, D-17/D-18) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeEssayGrading(int sessionId)
        {
            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null || session.Status != "Menunggu Penilaian")
                return Json(new { success = false, message = "Session tidak dalam status Menunggu Penilaian" });

            // 1. Cek semua Essay sudah dinilai (T-298-14)
            var packageAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
            if (packageAssignment == null)
                return Json(new { success = false, message = "Assignment tidak ditemukan" });

            var shuffledIds = packageAssignment.GetShuffledQuestionIds();

            var essayQuestions = await _context.PackageQuestions
                .Where(q => shuffledIds.Contains(q.Id) && q.QuestionType == "Essay")
                .ToListAsync();

            var essayResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId &&
                       essayQuestions.Select(q => q.Id).Contains(r.PackageQuestionId))
                .ToListAsync();

            if (essayResponses.Any(r => r.EssayScore == null))
                return Json(new { success = false, message = "Masih ada Essay yang belum dinilai" });

            // 2. Recalculate total score (MC + MA auto + Essay manual)
            var allQuestions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => shuffledIds.Contains(q.Id))
                .ToListAsync();
            var allResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId)
                .ToListAsync();

            int totalScore = 0;
            int maxScore = 0;
            foreach (var q in allQuestions)
            {
                maxScore += q.ScoreValue;
                switch (q.QuestionType ?? "MultipleChoice")
                {
                    case "MultipleChoice":
                        var mcResp = allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue);
                        if (mcResp != null)
                        {
                            var opt = q.Options.FirstOrDefault(o => o.Id == mcResp.PackageOptionId!.Value);
                            if (opt != null && opt.IsCorrect) totalScore += q.ScoreValue;
                        }
                        break;
                    case "MultipleAnswer":
                        var maSelected = allResponses
                            .Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue)
                            .Select(r => r.PackageOptionId!.Value).ToHashSet();
                        var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                        if (maSelected.SetEquals(maCorrect)) totalScore += q.ScoreValue;
                        break;
                    case "Essay":
                        var essayResp = allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                        if (essayResp?.EssayScore.HasValue == true) totalScore += essayResp.EssayScore.Value;
                        break;
                }
            }

            int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
            bool isPassed = finalPercentage >= session.PassPercentage;

            // 3. Update session: Completed + final score + IsPassed (T-298-16 replay guard via WHERE clause)
            await _context.AssessmentSessions
                .Where(s => s.Id == sessionId && s.Status == "Menunggu Penilaian")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Score, finalPercentage)
                    .SetProperty(r => r.Status, "Completed")
                    .SetProperty(r => r.IsPassed, isPassed)
                    .SetProperty(r => r.CompletedAt, DateTime.UtcNow));

            // 4. Generate TrainingRecord (duplicate guard — same as GradingService)
            var judul = $"Assessment: {session.Title}";
            bool trExists = await _context.TrainingRecords.AnyAsync(t =>
                t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule);
            if (!trExists)
            {
                _context.TrainingRecords.Add(new TrainingRecord
                {
                    UserId = session.UserId,
                    Judul = judul,
                    Kategori = session.Category ?? "Assessment",
                    Tanggal = session.Schedule,
                    TanggalSelesai = DateTime.UtcNow,
                    Penyelenggara = "Internal",
                    Status = isPassed ? "Passed" : "Failed"
                });
                await _context.SaveChangesAsync();
            }

            // 5. Generate sertifikat jika applicable (same pattern as GradingService)
            if (session.GenerateCertificate && isPassed)
            {
                var certNow = DateTime.Now;
                int certYear = certNow.Year;
                try
                {
                    var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
                    await _context.AssessmentSessions
                        .Where(s => s.Id == sessionId && s.NomorSertifikat == null)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
                }
                catch (DbUpdateException) { /* race-condition: cert number sudah diambil thread lain — skip */ }
            }

            // 6. Reload session untuk NotifyIfGroupCompleted
            var updatedSession = await _context.AssessmentSessions.FindAsync(sessionId);
            if (updatedSession != null)
                await _workerDataService.NotifyIfGroupCompleted(updatedSession);

            return Json(new { success = true, score = finalPercentage, isPassed });
        }

        // --- SUBMIT INTERVIEW RESULTS (Assessment Proton Tahun 3) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> SubmitInterviewResults(
            int sessionId,
            string? judges,
            string? notes,
            bool isPassed,
            IFormFile? supportingDoc,
            string? returnTitle,
            string? returnCategory,
            string? returnDate)
        {
            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null)
            {
                TempData["Error"] = "Session tidak ditemukan.";
                return RedirectToAction("ManageAssessment");
            }
            if (session.Category != "Assessment Proton" || session.TahunKe != "Tahun 3")
            {
                TempData["Error"] = "Aksi ini hanya untuk Assessment Proton Tahun 3.";
                return RedirectToAction("ManageAssessment");
            }

            // Collect aspect scores from form fields (name=aspect_{AspectName_Underscored})
            var aspects = new List<string>
            {
                "Pengetahuan Teknis", "Kemampuan Operasional", "Keselamatan Kerja",
                "Komunikasi & Kerjasama", "Sikap Profesional"
            };
            var aspectScores = new Dictionary<string, int>();
            foreach (var aspect in aspects)
            {
                var formKey = "aspect_" + aspect.Replace(" ", "_").Replace("&", "and").Replace(",", "");
                if (int.TryParse(Request.Form[formKey], out int score))
                    aspectScores[aspect] = Math.Clamp(score, 1, 5);
                else
                    aspectScores[aspect] = 3;
            }

            // File upload (optional, max 10MB)
            string? supportingDocPath = null;
            if (supportingDoc != null && supportingDoc.Length > 0 && supportingDoc.Length <= 10 * 1024 * 1024)
            {
                var ext = Path.GetExtension(supportingDoc.FileName).ToLowerInvariant();
                var allowed = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                if (allowed.Contains(ext))
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "interviews");
                    Directory.CreateDirectory(dir);
                    var safeName = $"{sessionId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
                    using var stream = new FileStream(Path.Combine(dir, safeName), FileMode.Create);
                    await supportingDoc.CopyToAsync(stream);
                    supportingDocPath = $"/uploads/interviews/{safeName}";
                }
            }
            // Preserve existing doc path when no new upload
            if (supportingDocPath == null && !string.IsNullOrEmpty(session.InterviewResultsJson))
            {
                try
                {
                    var old = System.Text.Json.JsonSerializer.Deserialize<InterviewResultsDto>(session.InterviewResultsJson);
                    supportingDocPath = old?.SupportingDocPath;
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse scheduled date field in AssessmentMonitoring"); }
            }

            var dto = new InterviewResultsDto
            {
                Judges = judges?.Trim() ?? "",
                AspectScores = aspectScores,
                Notes = notes?.Trim() ?? "",
                SupportingDocPath = supportingDocPath,
                IsPassed = isPassed
            };

            session.InterviewResultsJson = System.Text.Json.JsonSerializer.Serialize(dto);
            session.IsPassed = isPassed;
            session.Status = "Completed";
            session.CompletedAt = DateTime.UtcNow;

            // [PROTON-06 FIX] Create ProtonFinalAssessment when interview passes so that
            // HistoriProton and CoachingProton dashboard correctly reflect completion status.
            var actorForFix = await _userManager.GetUserAsync(User);
            if (isPassed && session.ProtonTrackId.HasValue)
            {
                var assignment = await _context.ProtonTrackAssignments
                    .FirstOrDefaultAsync(a => a.CoacheeId == session.UserId
                                           && a.ProtonTrackId == session.ProtonTrackId.Value
                                           && a.IsActive);
                if (assignment != null)
                {
                    // Avoid duplicate: only create if none exists for this assignment
                    var alreadyExists = await _context.ProtonFinalAssessments
                        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
                    if (!alreadyExists)
                    {
                        _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
                        {
                            CoacheeId = session.UserId,
                            CreatedById = actorForFix?.Id ?? "",
                            ProtonTrackAssignmentId = assignment.Id,
                            Status = "Completed",
                            CompetencyLevelGranted = 0, // Interview track does not grant a numeric level
                            Notes = $"Interview Tahun 3 lulus. Assessor: {dto.Judges}",
                            CreatedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Audit log
            var user = actorForFix;
            if (user != null)
            {
                var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
                await _auditLog.LogAsync(
                    user.Id,
                    actorName,
                    "SubmitInterviewResults",
                    $"Interview results saved for session ID={sessionId} ({session.Title}), IsPassed={isPassed}",
                    sessionId,
                    "AssessmentSession");
            }

            TempData["Success"] = "Hasil interview berhasil disimpan.";
            return RedirectToAction("AssessmentMonitoringDetail", new
            {
                title = returnTitle ?? session.Title,
                category = returnCategory ?? session.Category,
                scheduleDate = returnDate ?? session.Schedule.Date.ToString("yyyy-MM-dd")
            });
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

            // D-17: Block reset Pre jika Post sudah Completed
            if (assessment.AssessmentType == "PreTest" && assessment.LinkedSessionId.HasValue)
            {
                var linkedPost = await _context.AssessmentSessions
                    .FirstOrDefaultAsync(a => a.Id == assessment.LinkedSessionId);

                if (linkedPost != null && linkedPost.Status == "Completed")
                {
                    TempData["Error"] = "Post-Test sudah selesai. Reset Post-Test terlebih dahulu sebelum mereset Pre-Test.";
                    return RedirectToAction("AssessmentMonitoringDetail", new {
                        title = assessment.Title,
                        category = assessment.Category,
                        scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
                    });
                }
            }

            // Reset is valid for any active status (Open, InProgress, Completed, Abandoned) — Cancelled is final and NOT resettable
            if (assessment.Status != "Open" && assessment.Status != "InProgress" && assessment.Status != "Completed" && assessment.Status != "Abandoned")
            {
                TempData["Error"] = "Status sesi tidak valid untuk direset.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
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

            // Delete PackageUserResponse records for this session (package path answers)
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

            // 3. Reset session state to Open via status-guarded ExecuteUpdateAsync
            // (Cancelled is the only status that is NOT resettable — guard prevents double-reset race)
            await _context.SaveChangesAsync(); // flush archive + delete operations first

            var rsRowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == id && s.Status != "Cancelled")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Open")
                    .SetProperty(r => r.Score, (int?)null)
                    .SetProperty(r => r.IsPassed, (bool?)null)
                    .SetProperty(r => r.Progress, 0)
                    .SetProperty(r => r.StartedAt, (DateTime?)null)
                    .SetProperty(r => r.CompletedAt, (DateTime?)null)
                    .SetProperty(r => r.ElapsedSeconds, (int)0)
                    .SetProperty(r => r.LastActivePage, (int?)null)
                    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
                );

            if (rsRowsAffected == 0)
            {
                TempData["Error"] = "Sesi tidak dapat direset (mungkin sudah dibatalkan).";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            // Audit log
            var rsUser = await _userManager.GetUserAsync(User);
            var rsActorName = string.IsNullOrWhiteSpace(rsUser?.NIP) ? (rsUser?.FullName ?? "Unknown") : $"{rsUser.NIP} - {rsUser.FullName}";
            await _auditLog.LogAsync(
                rsUser?.Id ?? "",
                rsActorName,
                "ResetAssessment",
                $"Reset assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
                id,
                "AssessmentSession");

            await _hubContext.Clients.User(assessment.UserId).SendAsync("sessionReset", new { reason = "hc_reset" });

            TempData["Success"] = "Sesi ujian telah direset. Peserta dapat mengikuti ujian kembali.";
            return RedirectToAction("AssessmentMonitoringDetail", new {
                title = assessment.Title,
                category = assessment.Category,
                scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
            });
        }

        // --- AKHIRI UJIAN (individual: auto-grade from saved answers) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AkhiriUjian(int id)
        {
            var session = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (session == null) return NotFound();

            // Only InProgress sessions can be ended — use same logic as monitoring view:
            // StartedAt set + not yet completed/scored, and not Cancelled/Abandoned
            var isInProgress = session.StartedAt != null
                && session.CompletedAt == null
                && session.Score == null
                && session.Status != "Cancelled"
                && session.Status != "Abandoned";
            if (!isInProgress)
            {
                TempData["Error"] = "Akhiri Ujian hanya dapat dilakukan pada sesi yang berstatus InProgress.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = session.Title,
                    category = session.Category,
                    scheduleDate = session.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            // GradingService: grade dari DB, handle race-condition, TrainingRecord, NomorSertifikat, notifikasi grup
            bool graded = await _gradingService.GradeAndCompleteAsync(session);

            if (!graded)
            {
                // Race: session sudah di-complete oleh SubmitExam atau AkhiriUjian lain — silent skip
                TempData["Info"] = "Sesi sudah selesai atau dibatalkan.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = session.Title,
                    category = session.Category,
                    scheduleDate = session.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            _cache.Remove($"exam-status-{id}");

            // Audit log
            var auUser = await _userManager.GetUserAsync(User);
            var auActorName = string.IsNullOrWhiteSpace(auUser?.NIP) ? (auUser?.FullName ?? "Unknown") : $"{auUser.NIP} - {auUser.FullName}";
            await _auditLog.LogAsync(
                auUser?.Id ?? "",
                auActorName,
                "AkhiriUjian",
                $"Ended exam '{session.Title}' for user {session.UserId} [ID={id}], auto-graded score: {session.Score}%",
                id,
                "AssessmentSession");

            await _hubContext.Clients.User(session.UserId).SendAsync("examClosed", new { reason = "hc_closed" });

            TempData["Success"] = "Ujian telah diakhiri dan dinilai dari jawaban tersimpan.";
            return RedirectToAction("AssessmentMonitoringDetail", new {
                title = session.Title,
                category = session.Category,
                scheduleDate = session.Schedule.Date.ToString("yyyy-MM-dd")
            });
        }

        // --- AKHIRI SEMUA UJIAN (bulk: auto-grade InProgress + cancel not-started) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AkhiriSemuaUjian(string title, string category, DateTime scheduleDate)
        {
            // Find all Open or InProgress sessions in this assessment group
            var sessionsToEnd = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date
                         && (a.Status == "Open" || a.Status == "InProgress"))
                .ToListAsync();

            if (!sessionsToEnd.Any())
            {
                TempData["Error"] = "Tidak ada sesi Open atau InProgress untuk diakhiri.";
                return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate = scheduleDate.Date.ToString("yyyy-MM-dd") });
            }

            int gradedCount = 0;
            int cancelledCount = 0;

            // Cancelled sessions: update status via EF change tracking + SaveChanges
            var cancelledSessions = sessionsToEnd
                .Where(s => !(s.StartedAt != null && s.CompletedAt == null && s.Score == null))
                .ToList();
            foreach (var session in cancelledSessions)
            {
                session.Status = "Cancelled";
                session.UpdatedAt = DateTime.UtcNow;
                cancelledCount++;
            }
            if (cancelledSessions.Any())
                await _context.SaveChangesAsync();

            // InProgress sessions: grade via GradingService (handles race-condition, cert, TrainingRecord, notifikasi)
            var inProgressSessions = sessionsToEnd
                .Where(s => s.StartedAt != null && s.CompletedAt == null && s.Score == null)
                .ToList();
            foreach (var session in inProgressSessions)
            {
                try
                {
                    await _gradingService.GradeAndCompleteAsync(session);
                    gradedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "AkhiriSemuaUjian: gagal grade session {SessionId} untuk user {UserId} — lanjut ke session berikutnya.",
                        session.Id, session.UserId);
                }
            }

            // Invalidate cache for all affected sessions
            foreach (var s in sessionsToEnd)
                _cache.Remove($"exam-status-{s.Id}");

            // Audit log
            var actor = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
            await _auditLog.LogAsync(
                actor?.Id ?? "",
                actorName,
                "AkhiriSemuaUjian",
                $"Ended all exams for '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) — {gradedCount} graded, {cancelledCount} cancelled",
                null,
                "AssessmentSession");

            var batchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";
            await _hubContext.Clients.Group($"batch-{batchKey}").SendAsync("examClosed", new { reason = "hc_closed" });

            TempData["Success"] = $"Berhasil mengakhiri ujian: {gradedCount} peserta dinilai dari jawaban tersimpan, {cancelledCount} peserta dibatalkan.";
            return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate = scheduleDate.Date.ToString("yyyy-MM-dd") });
        }

        // --- GET AKHIRI SEMUA COUNTS (for confirmation modal) ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetAkhiriSemuaCounts(string title, string category, DateTime scheduleDate)
        {
            var sessions = await _context.AssessmentSessions
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date
                         && (a.Status == "Open" || a.Status == "InProgress"))
                .ToListAsync();

            int inProgressCount = sessions.Count(s => s.StartedAt != null && s.CompletedAt == null && s.Score == null);
            int notStartedCount = sessions.Count(s => s.StartedAt == null);

            return Json(new { inProgressCount, notStartedCount });
        }

        // --- EXPORT ASSESSMENT RESULTS ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
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
                    .GroupBy(x => x.AssessmentSessionId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Sum(x => x.QuestionCount));
            }
            // Build row data: one row per session, include all statuses
            var rows = sessions.Select(a =>
            {
                string userStatus;
                if (a.Status == "Cancelled")
                    userStatus = "Dibatalkan";
                else if (a.CompletedAt != null || a.Score != null)
                    userStatus = "Completed";
                else if (a.Status == "Abandoned")
                    userStatus = "Abandoned";
                else if (a.StartedAt != null)
                    userStatus = "In Progress";
                else
                    userStatus = "Not Started";

                string resultText = a.Status == "Cancelled" ? "\u2014"
                                  : a.IsPassed == true ? "Pass"
                                  : a.IsPassed == false ? "Fail"
                                  : "\u2014";

                return new
                {
                    UserFullName  = a.User?.FullName ?? "Unknown",
                    UserNIP       = a.User?.NIP ?? "",
                    QuestionCount = questionCountMap.TryGetValue(a.Id, out var qcnt) ? qcnt : 0,
                    UserStatus    = userStatus,
                    Score         = a.Status == "Cancelled" ? (object)"\u2014" : (a.Score.HasValue ? (object)a.Score.Value : "\u2014"),
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

            var firstSession = sessions.First();
            int totalCols = 7;

            // Assessment info header (rows 1-6)
            worksheet.Cell(1, 1).Value = "Laporan Assessment";
            worksheet.Range(1, 1, 1, totalCols).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            worksheet.Cell(2, 1).Value = "Judul";
            worksheet.Cell(2, 2).Value = title;
            worksheet.Range(2, 2, 2, totalCols).Merge();

            worksheet.Cell(3, 1).Value = "Kategori";
            worksheet.Cell(3, 2).Value = category;
            worksheet.Range(3, 2, 3, totalCols).Merge();

            worksheet.Cell(4, 1).Value = "Jadwal";
            worksheet.Cell(4, 2).Value = firstSession.Schedule.ToString("dd MMM yyyy HH:mm");
            worksheet.Range(4, 2, 4, totalCols).Merge();

            worksheet.Cell(5, 1).Value = "Durasi";
            worksheet.Cell(5, 2).Value = $"{firstSession.DurationMinutes} menit";
            worksheet.Range(5, 2, 5, totalCols).Merge();

            worksheet.Cell(6, 1).Value = "Batas Kelulusan";
            worksheet.Cell(6, 2).Value = $"{firstSession.PassPercentage}%";
            worksheet.Range(6, 2, 6, totalCols).Merge();

            // Bold the labels
            worksheet.Range(2, 1, 6, 1).Style.Font.Bold = true;

            // Row 7 is blank separator, column headers start at row 8
            int headerRow = 8;
            int col = 1;
            worksheet.Cell(headerRow, col++).Value = "Name";
            worksheet.Cell(headerRow, col++).Value = "NIP";
            worksheet.Cell(headerRow, col++).Value = "Jumlah Soal";
            worksheet.Cell(headerRow, col++).Value = "Status";
            worksheet.Cell(headerRow, col++).Value = "Score";
            worksheet.Cell(headerRow, col++).Value = "Result";
            worksheet.Cell(headerRow, col).Value   = "Completed At";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var row = i + headerRow + 1;
                int c = 1;
                worksheet.Cell(row, c++).Value = r.UserFullName;
                worksheet.Cell(row, c++).Value = r.UserNIP;
                worksheet.Cell(row, c++).Value = r.QuestionCount;
                worksheet.Cell(row, c++).Value = r.UserStatus;
                worksheet.Cell(row, c++).Value = r.Score?.ToString() ?? "\u2014";
                worksheet.Cell(row, c++).Value = r.Result;
                worksheet.Cell(row, c).Value   = r.CompletedAt;
            }

            // Sanitize title for filename: replace non-alphanumeric with _
            var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
            var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Results.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // --- USER ASSESSMENT HISTORY ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> UserAssessmentHistory(string userId)
        {
            // Load the target user
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (string.IsNullOrEmpty(userId) || targetUser == null)
            {
                TempData["Error"] = "User not found or invalid userId.";
                return RedirectToAction("ManageAssessment");
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
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AuditLog(int page = 1, DateTime? startDate = null, DateTime? endDate = null)
        {
            const int pageSize = 25;

            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt < endDate.Value.AddDays(1));

            var paging = PaginationHelper.Calculate(await query.CountAsync(), page, pageSize);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(paging.Skip)
                .Take(paging.Take)
                .ToListAsync();

            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }

        // GET /Admin/ExportAuditLog
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportAuditLog(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt < endDate.Value.AddDays(1));

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "AuditLog", new[] { "Waktu", "Aktor", "Aksi", "Detail" });
            ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                ws.Cell(i + 2, 1).Value = log.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");
                ws.Cell(i + 2, 2).Value = log.ActorName;
                ws.Cell(i + 2, 3).Value = log.ActionType;
                ws.Cell(i + 2, 4).Value = log.Description;
            }

            var fileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // CloseEarly removed in Phase 162 — replaced by AkhiriSemuaUjian with auto-grading

        // POST /Admin/ReshufflePackage — reshuffle package for single worker
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
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

            var rng = Random.Shared;
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
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ReshufflePackage",
                    $"Reshuffled package (cross-package) for user {assessment.UserId} on assessment '{assessment.Title}' [SessionID={sessionId}]: {shuffledIds.Count} questions from {packages.Count} packages",
                    sessionId,
                    "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ReshufflePackage (sessionId={Id})", sessionId); }

            return Json(new { success = true, packageName = $"Cross-package ({packages.Count} paket)", assignmentId = newAssignment.Id });
        }

        // POST /Admin/ReshuffleAll — bulk reshuffle for all workers in assessment group
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
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

            var rng = Random.Shared;
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
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ReshuffleAll",
                    $"Bulk reshuffled {reshuffledCount} worker(s) on assessment '{title}' [{category}] scheduled {scheduleDate:yyyy-MM-dd}",
                    null,
                    "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ReshuffleAll (groupTitle={Title})", title); }

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

            // Single package: shuffle question order so each worker sees a unique sequence
            if (packages.Count == 1)
            {
                var singlePackageQuestions = packages[0].Questions;
                if (singlePackageQuestions == null || !singlePackageQuestions.Any())
                    return new List<int>();
                var singlePackageIds = singlePackageQuestions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
                Shuffle(singlePackageIds, rng);
                return singlePackageIds;
            }

            // Safety fallback: use minimum question count across packages (edge case per user decision)
            int K = packages.Min(p => p.Questions.Count);
            if (K == 0)
                return new List<int>();

            // Collect all questions across all packages with their package index
            var allQuestions = packages.SelectMany((p, pIdx) =>
                p.Questions.Select(q => new { Question = q, PackageIndex = pIdx })).ToList();

            // Identify distinct ET groups (non-null ElemenTeknis values across all packages)
            var etGroups = allQuestions
                .Where(x => !string.IsNullOrWhiteSpace(x.Question.ElemenTeknis))
                .Select(x => x.Question.ElemenTeknis!)
                .Distinct()
                .ToList();

            // Fallback: if no questions have ElemenTeknis, use original slot-list algorithm
            if (etGroups.Count == 0)
            {
                // No ElemenTeknis data — fall back to original slot-list distribution
                int N0 = packages.Count;
                int baseCount0 = K / N0;
                int remainder0 = K % N0;
                var remainderIndices0 = Enumerable.Range(0, N0)
                    .OrderBy(_ => rng.Next())
                    .Take(remainder0)
                    .ToHashSet();
                var slots0 = new List<int>();
                for (int i = 0; i < N0; i++)
                {
                    int count = baseCount0 + (remainderIndices0.Contains(i) ? 1 : 0);
                    for (int j = 0; j < count; j++)
                        slots0.Add(i);
                }
                Shuffle(slots0, rng);
                var pkgCounter0 = new int[N0];
                var fallbackIds = new List<int>();
                var orderedQuestions0 = packages.Select(p => p.Questions.OrderBy(q => q.Order).ToList()).ToList();
                for (int pos = 0; pos < K; pos++)
                {
                    int pkgIdx = slots0[pos];
                    var question = orderedQuestions0[pkgIdx][pkgCounter0[pkgIdx]];
                    pkgCounter0[pkgIdx]++;
                    fallbackIds.Add(question.Id);
                }
                return fallbackIds;
            }

            // ET-aware distribution
            var selectedIds = new HashSet<int>();
            var selectedList = new List<int>();

            // Phase 1 — Guarantee one question per ET group (best-effort, capped at K)
            // NULL ElemenTeknis questions are excluded from Phase 1 (they participate in Phase 2 only)
            foreach (var etGroup in etGroups)
            {
                if (selectedIds.Count >= K) break;

                var candidates = allQuestions
                    .Where(x => x.Question.ElemenTeknis == etGroup && !selectedIds.Contains(x.Question.Id))
                    .Select(x => x.Question.Id)
                    .ToList();

                Shuffle(candidates, rng);
                if (candidates.Count > 0)
                {
                    int picked = candidates[0];
                    selectedIds.Add(picked);
                    selectedList.Add(picked);
                }
            }

            // Phase 2 — Fill remaining quota with balanced package distribution
            int remaining = K - selectedIds.Count;
            if (remaining > 0)
            {
                int N = packages.Count;
                var orderedByPackage = packages
                    .Select(p => p.Questions.OrderBy(q => q.Order)
                        .Where(q => !selectedIds.Contains(q.Id))
                        .ToList())
                    .ToList();

                // Build slot list for remaining slots using balanced distribution
                int baseCount = remaining / N;
                int remainder = remaining % N;
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
                var pkgAvailable = orderedByPackage.Select(q => q.Count).ToArray();

                foreach (int pkgIdx in slots)
                {
                    // Find a package with available unselected questions
                    int targetPkg = pkgIdx;
                    if (pkgCounter[targetPkg] >= pkgAvailable[targetPkg])
                    {
                        // Redistribute: find any package with remaining questions
                        targetPkg = -1;
                        for (int i = 0; i < N; i++)
                        {
                            if (pkgCounter[i] < pkgAvailable[i]) { targetPkg = i; break; }
                        }
                        if (targetPkg == -1) break; // All packages exhausted
                    }
                    var q = orderedByPackage[targetPkg][pkgCounter[targetPkg]];
                    pkgCounter[targetPkg]++;
                    selectedIds.Add(q.Id);
                    selectedList.Add(q.Id);
                }
            }

            // Phase 3 — Fisher-Yates shuffle the combined list
            Shuffle(selectedList, rng);
            return selectedList;
        }

        #endregion

        // Question Management (Admin) region removed in Phase 227 (CLEN-02) — ManageQuestions/AddQuestion/DeleteQuestion
        // were legacy-only actions. Assessment questions are now managed via Package Management.

        #region Package Management (Admin)

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

            var packageIds = packages.Select(p => p.Id).ToList();
            var assignmentCounts = await _context.UserPackageAssignments
                .Where(a => packageIds.Contains(a.AssessmentPackageId))
                .GroupBy(a => a.AssessmentPackageId)
                .Select(g => new { PackageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PackageId, x => x.Count);
            ViewBag.AssignmentCounts = assignmentCounts;

            // ET coverage: rows = ET groups, columns = per package
            var allEtGroups = packages
                .SelectMany(p => p.Questions)
                .Select(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "(Tanpa ET)" : q.ElemenTeknis!.Trim())
                .Distinct()
                .OrderBy(g => g == "(Tanpa ET)" ? "zzz" : g) // Tanpa ET last
                .ToList();

            // Dictionary: etGroup -> Dictionary<packageId, questionCount>
            var etCoverage = new Dictionary<string, Dictionary<int, int>>();
            foreach (var et in allEtGroups)
            {
                etCoverage[et] = new Dictionary<int, int>();
                foreach (var pkg in packages)
                {
                    var count = pkg.Questions.Count(q =>
                        (string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "(Tanpa ET)" : q.ElemenTeknis!.Trim()) == et);
                    etCoverage[et][pkg.Id] = count;
                }
            }
            ViewBag.EtCoverage = etCoverage;
            ViewBag.EtGroups = allEtGroups;

            ViewBag.Packages = packages;
            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.AssessmentId = assessmentId;

            // Detect Post session for CopyPackagesFromPre button
            bool isPostSession = assessment.AssessmentType == "PostTest";
            ViewBag.IsPostSession = isPostSession;
            if (isPostSession && assessment.LinkedSessionId.HasValue)
            {
                ViewBag.PreSessionId = assessment.LinkedSessionId.Value;
            }

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyPackagesFromPre(int postSessionId)
        {
            var postSession = await _context.AssessmentSessions.FindAsync(postSessionId);
            if (postSession == null || postSession.AssessmentType != "PostTest" || !postSession.LinkedSessionId.HasValue)
            {
                TempData["Error"] = "Sesi Post-Test tidak valid.";
                return RedirectToAction("ManagePackages", new { assessmentId = postSessionId });
            }

            int preSessionId = postSession.LinkedSessionId.Value;

            // Hapus paket Post yang ada
            var existingPostPkgs = await _context.AssessmentPackages
                .Include(p => p.Questions).ThenInclude(q => q.Options)
                .Where(p => p.AssessmentSessionId == postSessionId)
                .ToListAsync();

            foreach (var pkg in existingPostPkgs)
            {
                foreach (var q in pkg.Questions)
                    _context.PackageOptions.RemoveRange(q.Options);
                _context.PackageQuestions.RemoveRange(pkg.Questions);
            }
            _context.AssessmentPackages.RemoveRange(existingPostPkgs);

            // Deep clone Pre packages ke Post
            var prePkgs = await _context.AssessmentPackages
                .Include(p => p.Questions).ThenInclude(q => q.Options)
                .Where(p => p.AssessmentSessionId == preSessionId)
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

            foreach (var prePkg in prePkgs)
            {
                var newPkg = new AssessmentPackage
                {
                    AssessmentSessionId = postSessionId,
                    PackageName = prePkg.PackageName,
                    PackageNumber = prePkg.PackageNumber
                };
                foreach (var q in prePkg.Questions)
                {
                    var newQ = new PackageQuestion
                    {
                        QuestionText = q.QuestionText,
                        Order = q.Order,
                        ScoreValue = q.ScoreValue,
                        QuestionType = q.QuestionType,
                        ElemenTeknis = q.ElemenTeknis,
                        Options = q.Options.Select(o => new PackageOption
                        {
                            OptionText = o.OptionText,
                            IsCorrect = o.IsCorrect
                        }).ToList()
                    };
                    newPkg.Questions.Add(newQ);
                }
                _context.AssessmentPackages.Add(newPkg);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Berhasil menyalin {prePkgs.Count} paket soal dari Pre-Test.";
            return RedirectToAction("ManagePackages", new { assessmentId = postSessionId });
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

            var questionIds = pkg.Questions.Select(q => q.Id).ToList();
            if (questionIds.Any())
            {
                var pkgResponses = await _context.PackageUserResponses
                    .Where(r => questionIds.Contains(r.PackageQuestionId))
                    .ToListAsync();
                if (pkgResponses.Any())
                    _context.PackageUserResponses.RemoveRange(pkgResponses);
            }

            var assignments = await _context.UserPackageAssignments
                .Where(a => a.AssessmentPackageId == packageId)
                .ToListAsync();
            if (assignments.Any())
                _context.UserPackageAssignments.RemoveRange(assignments);

            foreach (var q in pkg.Questions)
                _context.PackageOptions.RemoveRange(q.Options);
            _context.PackageQuestions.RemoveRange(pkg.Questions);
            _context.AssessmentPackages.Remove(pkg);

            await _context.SaveChangesAsync();

            try
            {
                var delUser = await _userManager.GetUserAsync(User);
                var delActorName = string.IsNullOrWhiteSpace(delUser?.NIP) ? (delUser?.FullName ?? "Unknown") : $"{delUser.NIP} - {delUser.FullName}";
                await _auditLog.LogAsync(
                    delUser?.Id ?? "",
                    delActorName,
                    "DeletePackage",
                    $"Deleted package '{pkg.PackageName}' from assessment [ID={assessmentId}]" +
                        (assignments.Any() ? $" ({assignments.Count} assignment(s) removed)" : ""),
                    assessmentId,
                    "AssessmentPackage");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeletePackage (packageId={Id})", packageId); }

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

        // GET /Admin/DownloadQuestionTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadQuestionTemplate(string type = "MC")
        {
            // Normalize type — whitelist only
            var validTypes = new[] { "MC", "MA", "Essay", "Universal" };
            if (!validTypes.Contains(type)) type = "MC";

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Question Import");

            // 9-column header (same for all variants)
            var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Jawaban Benar", "Elemen Teknis", "QuestionType", "Rubrik" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int nextRow = 2;

            void AddExampleRow(int row, string[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = values[i];
                    ws.Cell(row, i + 1).Style.Font.Italic = true;
                    ws.Cell(row, i + 1).Style.Font.FontColor = XLColor.Gray;
                }
            }

            // Example rows per type
            var mcExample  = new[] { "Contoh soal MC?", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "A", "K3 Dasar", "MultipleChoice", "" };
            var maExample  = new[] { "Contoh soal MA?", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "A,C", "K3 Dasar", "MultipleAnswer", "" };
            var essayExample = new[] { "Contoh soal Essay?", "", "", "", "", "", "K3 Dasar", "Essay", "Rubrik: Jawaban harus mencakup..." };

            if (type == "MC")
            {
                AddExampleRow(nextRow++, mcExample);
            }
            else if (type == "MA")
            {
                AddExampleRow(nextRow++, maExample);
            }
            else if (type == "Essay")
            {
                AddExampleRow(nextRow++, essayExample);
            }
            else // Universal
            {
                AddExampleRow(nextRow++, mcExample);
                AddExampleRow(nextRow++, maExample);
                AddExampleRow(nextRow++, essayExample);
            }

            // Instruction rows
            void AddInstruction(int row, string text)
            {
                ws.Cell(row, 1).Value = text;
                ws.Cell(row, 1).Style.Font.Italic = true;
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkRed;
            }

            AddInstruction(nextRow++, "QuestionType: MultipleChoice (default jika kosong), MultipleAnswer, atau Essay");
            AddInstruction(nextRow++, "Jawaban Benar MA: isi huruf dipisah koma, contoh: A,C atau A,B,D");
            AddInstruction(nextRow++, "Essay: Opsi A-D dan Jawaban Benar dikosongkan. Rubrik wajib diisi");
            AddInstruction(nextRow++, "Kolom Elemen Teknis: opsional, isi nama elemen teknis. Kosongkan jika tidak ada.");

            var fileName = $"Template_Soal_{type}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
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
            // File type guard: only allow Excel files
            if (excelFile != null && excelFile.Length > 0)
            {
                var allowedQuestionsExtensions = new[] { ".xlsx", ".xls" };
                var questionsExt = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
                if (!allowedQuestionsExtensions.Contains(questionsExt))
                {
                    TempData["Error"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                    return RedirectToAction("ImportPackageQuestions", new { packageId });
                }
            }

            // File size guard: reject files larger than 5 MB to avoid memory pressure
            if (excelFile != null && excelFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File terlalu besar. Maksimal ukuran file adalah 5 MB.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            var existingFingerprints = pkg.Questions.Select(q =>
            {
                var opts = q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList();
                return MakePackageFingerprint(
                    q.QuestionText,
                    opts.ElementAtOrDefault(0) ?? "",
                    opts.ElementAtOrDefault(1) ?? "",
                    opts.ElementAtOrDefault(2) ?? "",
                    opts.ElementAtOrDefault(3) ?? "");
            }).ToHashSet();
            var seenInBatch = new HashSet<string>();

            List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct, string? ElemenTeknis, string QuestionType, string? Rubrik)> rows;
            var errors = new List<string>();

            var validQuestionTypes = new[] { "MultipleChoice", "MultipleAnswer", "Essay" };

            string NormalizeQuestionType(string raw)
            {
                var t = raw?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(t)) return "MultipleChoice"; // D-12 backward compat
                if (!validQuestionTypes.Contains(t)) return "MultipleChoice";
                return t;
            }

            if (excelFile != null && excelFile.Length > 0)
            {
                rows = new List<(string, string, string, string, string, string, string?, string, string?)>();
                try
                {
                    using var stream = excelFile.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    if (!workbook.Worksheets.Any())
                    {
                        errors.Add("File Excel tidak memiliki worksheet.");
                        return Json(new { success = false, errors });
                    }
                    var ws = workbook.Worksheets.First();
                    int rowNum = 1;
                    foreach (var row in ws.RowsUsed().Skip(1))
                    {
                        rowNum++;
                        var q   = (row.Cell(1).GetString() ?? "").Trim();
                        var a   = (row.Cell(2).GetString() ?? "").Trim();
                        var b   = (row.Cell(3).GetString() ?? "").Trim();
                        var c   = (row.Cell(4).GetString() ?? "").Trim();
                        var d   = (row.Cell(5).GetString() ?? "").Trim();
                        var cor = (row.Cell(6).GetString() ?? "").Trim().ToUpper();
                        var cell7 = (row.Cell(7).GetString() ?? "").Trim();
                        string? subComp = string.IsNullOrWhiteSpace(cell7) ? null : cell7;
                        var questionType = NormalizeQuestionType(row.Cell(8).GetString() ?? "");
                        var rubrik = row.Cell(9).GetString()?.Trim();
                        if (string.IsNullOrWhiteSpace(rubrik)) rubrik = null;
                        rows.Add((q, a, b, c, d, cor, subComp, questionType, rubrik));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read Excel file for package {PackageId}", packageId);
                    TempData["Error"] = "Gagal membaca file Excel. Pastikan format file benar.";
                    return RedirectToAction("ImportPackageQuestions", new { packageId });
                }
            }
            else if (!string.IsNullOrWhiteSpace(pasteText))
            {
                rows = new List<(string, string, string, string, string, string, string?, string, string?)>();
                var lines = pasteText.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

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
                        errors.Add($"Row {i + 1}: expected at least 6 columns, got {cells.Length}.");
                        continue;
                    }
                    string? subComp = cells.Length >= 7 ? cells[6].Trim() : null;
                    if (string.IsNullOrWhiteSpace(subComp)) subComp = null;
                    var questionType = NormalizeQuestionType(cells.Length >= 8 ? cells[7] : "");
                    string? rubrik = cells.Length >= 9 ? cells[8].Trim() : null;
                    if (string.IsNullOrWhiteSpace(rubrik)) rubrik = null;
                    rows.Add((
                        cells[0].Trim(), cells[1].Trim(), cells[2].Trim(),
                        cells[3].Trim(), cells[4].Trim(), cells[5].Trim().ToUpper(),
                        subComp, questionType, rubrik
                    ));
                }
            }
            else
            {
                TempData["Error"] = "Please upload an Excel file or paste question data.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Cross-package count validation
            var targetSession = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (targetSession != null)
            {
                var siblingSessionIds = await _context.AssessmentSessions
                    .Where(s => s.Title == targetSession.Title &&
                                s.Category == targetSession.Category &&
                                s.Schedule.Date == targetSession.Schedule.Date)
                    .Select(s => s.Id)
                    .ToListAsync();

                var siblingPackagesWithQuestions = await _context.AssessmentPackages
                    .Include(p => p.Questions)
                    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId)
                             && p.Id != packageId
                             && p.Questions.Any())
                    .ToListAsync();

                if (siblingPackagesWithQuestions.Any())
                {
                    var validRowCount = rows.Count(r =>
                    {
                        var (rq, ra, rb, rc, rd, rcor, _, rqtype, _) = r;
                        if (string.IsNullOrWhiteSpace(rq)) return false;
                        if (rqtype == "Essay") return true; // Essay: hanya butuh teks soal
                        var normalizedCor = ExtractPackageCorrectLetter(rcor);
                        return !string.IsNullOrWhiteSpace(ra) && !string.IsNullOrWhiteSpace(rb) &&
                               !string.IsNullOrWhiteSpace(rc) && !string.IsNullOrWhiteSpace(rd) &&
                               (new[] { "A", "B", "C", "D" }.Contains(normalizedCor) || rcor.Contains(','));
                    });

                    var referencePackage = siblingPackagesWithQuestions.First();
                    int expectedCount = referencePackage.Questions.Count;

                    if (validRowCount != expectedCount)
                    {
                        TempData["Error"] = $"Jumlah soal tidak sama dengan paket lain. {referencePackage.PackageName}: {expectedCount} soal. Harap masukkan {expectedCount} soal.";
                        return RedirectToAction("ImportPackageQuestions", new { packageId });
                    }
                }
            }

            int order = pkg.Questions.Count + 1;
            int added = 0;
            int skipped = 0;
            // Collect all new questions (with options embedded) before saving — avoids N+1 SaveChangesAsync
            var newQuestions = new List<PackageQuestion>();
            for (int i = 0; i < rows.Count; i++)
            {
                var (q, a, b, c, d, cor, rawSubComp, questionType, rubrik) = rows[i];

                if (string.IsNullOrWhiteSpace(q))
                {
                    errors.Add($"Row {i + 1}: Question text is empty. Skipped.");
                    continue;
                }

                // Essay: skip opsi/jawaban validation, only need question text
                if (questionType != "Essay")
                {
                    if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) ||
                        string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(d))
                    {
                        errors.Add($"Row {i + 1}: One or more options are empty. Skipped.");
                        continue;
                    }
                }

                // Parse correct answers — support multi for MA (T-298-05: hanya huruf A-D)
                List<string> correctLetters;
                if (questionType == "MultipleAnswer")
                {
                    correctLetters = cor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToUpper())
                        .Where(s => new[] { "A", "B", "C", "D" }.Contains(s))
                        .Distinct()
                        .ToList();
                    if (correctLetters.Count == 0)
                    {
                        errors.Add($"Row {i + 1}: MA soal harus memiliki minimal 1 jawaban benar valid (A-D). Skipped.");
                        continue;
                    }
                }
                else if (questionType == "Essay")
                {
                    correctLetters = new List<string>(); // Essay tidak butuh jawaban benar
                }
                else // MultipleChoice
                {
                    var normalizedCor = ExtractPackageCorrectLetter(cor);
                    if (!new[] { "A", "B", "C", "D" }.Contains(normalizedCor))
                    {
                        errors.Add($"Row {i + 1}: 'Correct' column must be A, B, C, or D. Got '{cor}'. Skipped.");
                        continue;
                    }
                    correctLetters = new List<string> { normalizedCor };
                }

                // Dedup fingerprint (essay uses empty options)
                var fp = MakePackageFingerprint(q, a, b, c, d);
                if (existingFingerprints.Contains(fp) || seenInBatch.Contains(fp))
                {
                    skipped++;
                    continue;
                }
                seenInBatch.Add(fp);

                var newQ = new PackageQuestion
                {
                    AssessmentPackageId = packageId,
                    QuestionText = q,
                    QuestionType = questionType,
                    Rubrik = questionType == "Essay" ? rubrik : null,
                    MaxCharacters = 2000,
                    Order = order++,
                    ScoreValue = 10,
                    ElemenTeknis = NormalizeElemenTeknis(rawSubComp),
                };

                // Build options (skip for Essay per D-04)
                if (questionType != "Essay")
                {
                    var optionTexts = new[] { a, b, c, d };
                    var optionLetters = new[] { "A", "B", "C", "D" };
                    newQ.Options = optionTexts.Select((optText, idx) => new PackageOption
                    {
                        OptionText = optText,
                        IsCorrect = correctLetters.Contains(optionLetters[idx])
                    }).ToList();
                }
                else
                {
                    newQ.Options = new List<PackageOption>();
                }

                newQuestions.Add(newQ);
                added++;
            }

            // Persist all new questions + options in a single transaction (single SaveChangesAsync)
            if (newQuestions.Count > 0)
            {
                _context.PackageQuestions.AddRange(newQuestions);
                using var importTx = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await importTx.CommitAsync();
                }
                catch
                {
                    await importTx.RollbackAsync();
                    throw;
                }
            }

            if (added == 0 && skipped == 0)
            {
                TempData["Warning"] = "No valid questions found in the import. Check the format and try again.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }
            if (added == 0 && skipped > 0)
            {
                TempData["Warning"] = "All questions were already in the package. Nothing was added.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }

            // Audit log
            try
            {
                var importUser = await _userManager.GetUserAsync(User);
                var importActorName = string.IsNullOrWhiteSpace(importUser?.NIP) ? (importUser?.FullName ?? "Unknown") : $"{importUser.NIP} - {importUser.FullName}";
                string source = excelFile != null && excelFile.Length > 0 ? $"file '{excelFile.FileName}'" : "pasted text";
                await _auditLog.LogAsync(
                    importUser?.Id ?? "",
                    importActorName,
                    "ImportQuestions",
                    $"Imported {added} questions from {source} to package {pkg.PackageName} [ID={packageId}] in assessment {pkg.AssessmentSessionId}",
                    packageId,
                    "AssessmentPackage");
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx, "Audit log write failed for ImportPackageQuestions {PackageId}", packageId);
            }

            // Cross-package ElemenTeknis distribution warning
            if (added > 0 && targetSession != null)
            {
                // Gather all ET groups across ALL packages in this assessment (including current)
                var allPackagesForSession = await _context.AssessmentPackages
                    .Include(p => p.Questions)
                    .Where(p => p.AssessmentSessionId == pkg.AssessmentSessionId)
                    .ToListAsync();

                if (allPackagesForSession.Count > 1)
                {
                    var allEtGroups = allPackagesForSession
                        .SelectMany(p => p.Questions)
                        .Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                        .Select(q => q.ElemenTeknis!.Trim())
                        .Distinct()
                        .ToHashSet();

                    if (allEtGroups.Any())
                    {
                        var missingPerPackage = new List<string>();
                        foreach (var p in allPackagesForSession)
                        {
                            var pkgEtGroups = p.Questions
                                .Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                                .Select(q => q.ElemenTeknis!.Trim())
                                .Distinct()
                                .ToHashSet();
                            var missing = allEtGroups.Except(pkgEtGroups).ToList();
                            if (missing.Any())
                                missingPerPackage.Add($"{p.PackageName}: tidak ada soal untuk {string.Join(", ", missing)}");
                        }

                        if (missingPerPackage.Any())
                        {
                            TempData["Warning"] = "Distribusi Elemen Teknis tidak lengkap — " + string.Join("; ", missingPerPackage) +
                                ". Pastikan setiap paket memiliki minimal 1 soal per Elemen Teknis untuk hasil assessment yang optimal.";
                        }
                    }
                }
            }

            if (excelFile != null && excelFile.Length > 0)
                TempData["Success"] = $"Imported from file: {added} added, {skipped} skipped.";
            else
                TempData["Success"] = $"{added} added, {skipped} skipped.";

            return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
        }

        // Package import helpers (named with "Package" prefix to avoid collision)
        private static string ExtractPackageCorrectLetter(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            if (raw.Length == 1) return raw;
            if ("ABCD".Contains(raw[0]) && !char.IsLetterOrDigit(raw[1]))
                return raw[0].ToString();
            if (raw.StartsWith("OPTION ") && raw.Length > 7 && "ABCD".Contains(raw[7]))
                return raw[7].ToString();
            return raw;
        }

        private static string? NormalizeElemenTeknis(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }

        private static string NormalizePackageText(string s)
            => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();

        private static string MakePackageFingerprint(string q, string a, string b, string c, string d)
            => string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizePackageText));

        #endregion

        #region Question Management per Package (Phase 298)

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManagePackageQuestions(int packageId)
        {
            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            var assessment = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (assessment == null) return NotFound();

            ViewBag.PackageId = packageId;
            ViewBag.PackageName = pkg.PackageName;
            ViewBag.AssessmentId = pkg.AssessmentSessionId;
            ViewBag.AssessmentTitle = assessment.Title;
            ViewBag.Questions = pkg.Questions.OrderBy(q => q.Order).ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(
            int packageId,
            string questionText,
            string questionType,
            int scoreValue,
            string? elemenTeknis,
            string? rubrik,
            int maxCharacters,
            string? optionA, string? optionB, string? optionC, string? optionD,
            bool correctA, bool correctB, bool correctC, bool correctD)
        {
            // Validate QuestionType whitelist (T-298-01)
            var validTypes = new[] { "MultipleChoice", "MultipleAnswer", "Essay" };
            if (!validTypes.Contains(questionType))
            {
                TempData["Error"] = "Tipe soal tidak valid.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            // Server-side: MC/MA force ScoreValue=10 (T-298-03)
            if (questionType != "Essay") scoreValue = 10;
            if (scoreValue <= 0) scoreValue = 10;

            // Validate per type (D-07)
            var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
            if (questionType == "MultipleChoice" && correctCount != 1)
            {
                TempData["Error"] = "Pilihan Ganda hanya boleh memiliki 1 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "MultipleAnswer" && correctCount < 2)
            {
                TempData["Error"] = "Multiple Answer membutuhkan minimal 2 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "Essay" && string.IsNullOrWhiteSpace(rubrik))
            {
                TempData["Error"] = "Rubrik wajib diisi untuk soal Essay.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            int nextOrder = pkg.Questions.Any() ? pkg.Questions.Max(q => q.Order) + 1 : 1;

            var newQ = new PackageQuestion
            {
                AssessmentPackageId = packageId,
                QuestionText = questionText.Trim(),
                QuestionType = questionType,
                ScoreValue = scoreValue,
                Order = nextOrder,
                ElemenTeknis = string.IsNullOrWhiteSpace(elemenTeknis) ? null : elemenTeknis.Trim(),
                Rubrik = questionType == "Essay" ? rubrik?.Trim() : null,
                MaxCharacters = questionType == "Essay" ? (maxCharacters > 0 ? maxCharacters : 2000) : 2000
            };

            if (questionType != "Essay")
            {
                var options = new[]
                {
                    (optionA, correctA),
                    (optionB, correctB),
                    (optionC, correctC),
                    (optionD, correctD)
                };
                foreach (var (text, isCorrect) in options)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        newQ.Options.Add(new PackageOption { OptionText = text!.Trim(), IsCorrect = isCorrect });
                    }
                }
            }

            _context.PackageQuestions.Add(newQ);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Soal berhasil ditambahkan.";
            return RedirectToAction("ManagePackageQuestions", new { packageId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditQuestion(int questionId)
        {
            var q = await _context.PackageQuestions
                .Include(q => q.Options)
                .Include(q => q.AssessmentPackage)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            if (q == null) return NotFound();

            // AJAX: return JSON for inline form population
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    id = q.Id,
                    order = q.Order,
                    questionText = q.QuestionText,
                    questionType = q.QuestionType ?? "MultipleChoice",
                    scoreValue = q.ScoreValue,
                    elemenTeknis = q.ElemenTeknis,
                    rubrik = q.Rubrik,
                    maxCharacters = q.MaxCharacters,
                    options = q.Options.OrderBy(o => o.Id).Select(o => new
                    {
                        optionText = o.OptionText,
                        isCorrect = o.IsCorrect
                    }).ToList()
                });
            }

            ViewBag.PackageId = q.AssessmentPackageId;
            ViewBag.AssessmentId = q.AssessmentPackage.AssessmentSessionId;
            return RedirectToAction("ManagePackageQuestions", new { packageId = q.AssessmentPackageId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(
            int questionId,
            int packageId,
            string questionText,
            string questionType,
            int scoreValue,
            string? elemenTeknis,
            string? rubrik,
            int maxCharacters,
            string? optionA, string? optionB, string? optionC, string? optionD,
            bool correctA, bool correctB, bool correctC, bool correctD)
        {
            // Validate QuestionType whitelist (T-298-01)
            var validTypes = new[] { "MultipleChoice", "MultipleAnswer", "Essay" };
            if (!validTypes.Contains(questionType))
            {
                TempData["Error"] = "Tipe soal tidak valid.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            var q = await _context.PackageQuestions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            if (q == null) return NotFound();

            // Server-side: MC/MA force ScoreValue=10 (T-298-03)
            if (questionType != "Essay") scoreValue = 10;
            if (scoreValue <= 0) scoreValue = 10;

            // Validate per type (D-07)
            var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
            if (questionType == "MultipleChoice" && correctCount != 1)
            {
                TempData["Error"] = "Pilihan Ganda hanya boleh memiliki 1 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "MultipleAnswer" && correctCount < 2)
            {
                TempData["Error"] = "Multiple Answer membutuhkan minimal 2 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "Essay" && string.IsNullOrWhiteSpace(rubrik))
            {
                TempData["Error"] = "Rubrik wajib diisi untuk soal Essay.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            q.QuestionText = questionText.Trim();
            q.QuestionType = questionType;
            q.ScoreValue = scoreValue;
            q.ElemenTeknis = string.IsNullOrWhiteSpace(elemenTeknis) ? null : elemenTeknis.Trim();
            q.Rubrik = questionType == "Essay" ? rubrik?.Trim() : null;
            q.MaxCharacters = questionType == "Essay" ? (maxCharacters > 0 ? maxCharacters : 2000) : 2000;

            // Replace options
            _context.PackageOptions.RemoveRange(q.Options);
            q.Options.Clear();

            if (questionType != "Essay")
            {
                var options = new[]
                {
                    (optionA, correctA),
                    (optionB, correctB),
                    (optionC, correctC),
                    (optionD, correctD)
                };
                foreach (var (text, isCorrect) in options)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        q.Options.Add(new PackageOption { OptionText = text!.Trim(), IsCorrect = isCorrect });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Soal berhasil diperbarui.";
            return RedirectToAction("ManagePackageQuestions", new { packageId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId, int packageId)
        {
            var q = await _context.PackageQuestions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            if (q == null) return NotFound();

            // Remove any responses for this question
            var responses = await _context.PackageUserResponses
                .Where(r => r.PackageQuestionId == questionId)
                .ToListAsync();
            if (responses.Any()) _context.PackageUserResponses.RemoveRange(responses);

            _context.PackageOptions.RemoveRange(q.Options);
            _context.PackageQuestions.Remove(q);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Soal berhasil dihapus.";
            return RedirectToAction("ManagePackageQuestions", new { packageId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> PreviewQuestion(int questionId)
        {
            var q = await _context.PackageQuestions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            if (q == null) return NotFound();

            return PartialView("_PreviewQuestion", q);
        }

        #endregion

        #region Activity Log (Phase 166)

        /// <summary>
        /// Returns the activity log for a given exam session as JSON.
        /// Used by HC to audit worker behaviour during the exam.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetActivityLog(int sessionId)
        {
            var session = await _context.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return NotFound(new { error = "Session not found." });

            var wib = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var events = await _context.ExamActivityLogs
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.Timestamp)
                .Select(l => new
                {
                    l.EventType,
                    l.Detail,
                    TimestampUtc = l.Timestamp
                })
                .ToListAsync();

            var eventsFormatted = events.Select(e => new
            {
                e.EventType,
                e.Detail,
                Timestamp = TimeZoneInfo.ConvertTimeFromUtc(e.TimestampUtc, wib).ToString("HH:mm:ss")
            }).ToList();

            var totalAnswered = await _context.PackageUserResponses
                .CountAsync(r => r.AssessmentSessionId == sessionId);

            var lastEventTime = await _context.ExamActivityLogs
                .Where(l => l.SessionId == sessionId)
                .MaxAsync(l => (DateTime?)l.Timestamp);

            int? timeSpentSeconds = null;
            if (session.StartedAt.HasValue)
            {
                var endTime = session.CompletedAt ?? lastEventTime ?? DateTime.UtcNow;
                timeSpentSeconds = (int)(endTime - session.StartedAt.Value).TotalSeconds;
            }

            var disconnectCount = eventsFormatted.Count(e => e.EventType == "disconnected");

            var summary = new
            {
                answeredCount = totalAnswered,
                disconnectCount,
                timeSpentSeconds,
                startedAt = session.StartedAt.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(session.StartedAt.Value, wib).ToString("HH:mm:ss")
                    : null,
                completedAt = session.CompletedAt.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(session.CompletedAt.Value, wib).ToString("HH:mm:ss")
                    : null
            };

            return Json(new { summary, events = eventsFormatted });
        }

        #endregion

    }
}
