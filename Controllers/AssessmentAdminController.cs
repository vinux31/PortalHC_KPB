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
    [Route("Admin/[action]")]
    public class AssessmentAdminController : AdminBaseController
    {
        private readonly IMemoryCache _cache;
        // Phase 311 Plan 03: cache key untuk distinct Categories list (D-04)
        private const string CategoriesCacheKey = "assessment_categories_distinct";
        private readonly ILogger<AssessmentAdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<AssessmentHub> _hubContext;
        private readonly IWorkerDataService _workerDataService;
        private readonly GradingService _gradingService;
        private readonly ProtonCompletionService _protonCompletionService;
        private readonly ProtonBypassService _protonBypassService;
        private readonly HcPortal.Services.RetakeService _retakeService;

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
            GradingService gradingService,
            ProtonCompletionService protonCompletionService,
            ProtonBypassService protonBypassService,
            HcPortal.Services.RetakeService retakeService)
            : base(context, userManager, auditLog, env)
        {
            _cache = cache;
            _logger = logger;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _workerDataService = workerDataService;
            _gradingService = gradingService;
            _protonCompletionService = protonCompletionService;
            _protonBypassService = protonBypassService;
            _retakeService = retakeService;
        }

        // Override View resolution to use Views/Admin/ folder (controller name is AssessmentAdmin, but views stay in Admin/)
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);
        protected new PartialViewResult PartialView(string viewName, object? model) => base.PartialView(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        // GET /Admin/ManageAssessment
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageAssessment(string? search, int page = 1, int pageSize = 20,
            string? tab = null, string? section = null, string? unit = null,
            string? category = null, string? statusFilter = null, string? isFiltered = null)
        {
            // Phase 322: shared filter shell removed (rollback Phase 311 Plan 02).
            // Per-tab native filter di partial views — shell hanya routing + lazy-load HTMX trigger.
            // Filter values di-set ViewBag untuk wrapper hx-vals (URL bookmark backward compat).

            var swShell = System.Diagnostics.Stopwatch.StartNew();

            // Tab routing — default to "assessment" (preserved dari logika lama)
            var activeTab = tab switch { "training" => "training", "history" => "history", _ => "assessment" };
            ViewBag.ActiveTab = activeTab;

            // Filter values preserve untuk pre-populate wrapper hx-vals (D-21 Strategy D)
            // dan partial actions yang baca via param (URL bookmark backward compat).
            // Phase 322 fix: coalesce semua string null ke "" supaya @Json.Serialize tidak
            // produce JSON null (yang HTMX URL-encode jadi literal string "null").
            ViewBag.SearchTerm = search ?? "";
            ViewBag.SelectedCategory = category ?? "";
            ViewBag.SelectedStatus = statusFilter ?? "";
            ViewBag.SelectedSection = section ?? "";
            ViewBag.SelectedUnit = unit ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            swShell.Stop();
            _logger.LogInformation(
                "ManageAssessment perf [tab=shell]: elapsed={Ms}ms search_present={SearchPresent} page={Page}",
                swShell.ElapsedMilliseconds, !string.IsNullOrEmpty(search), page);

            return View();
        }

        // =====================================================================
        // Phase 311 Plan 02: Partial actions untuk HTMX lazy load (D-01..D-10).
        // Setiap partial action mirror parameter signature shell + return PartialView.
        // ResponseCache NoStore (D-06) — browser tidak cache, HTMX manage tab cache JS-side.
        // Stopwatch per-action (D-09 — preserve a4ce556e telemetry pattern).
        // =====================================================================

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ManageAssessmentTab_Assessment(string? search, int page = 1, int pageSize = 20,
            string? category = null, string? statusFilter = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Phase 370 (URG-02): window 7-hari dihapus — tampilan default tanpa batas umur.
            // Phase 311 Plan 03: AsNoTracking di chain start read-only partial action.
            var managementQuery = _context.AssessmentSessions
                .AsNoTracking()
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

            if (!string.IsNullOrEmpty(category))
                managementQuery = managementQuery.Where(a => a.Category == category);

            // Phase 311 Plan 03: redundant Include navigation dihapus.
            // Projection sudah punya null guard a.User != null, EF Core 8 auto-emit LEFT JOIN AspNetUsers dari projection.
            var allSessions = await managementQuery
                .OrderByDescending(a => a.Schedule)
                .Select(a => new
                {
                    a.Id, a.Title, a.Category, a.Schedule, a.ExamWindowCloseDate, a.DurationMinutes,
                    a.Status, a.IsTokenRequired, a.AccessToken, a.PassPercentage, a.AllowAnswerReview,
                    a.CreatedAt, a.AssessmentType, a.LinkedGroupId,
                    UserFullName = a.User != null ? a.User.FullName : "Unknown",
                    UserEmail = a.User != null ? a.User.Email : "",
                    UserId = a.User != null ? a.User.Id : ""
                })
                .ToListAsync();

            var mgPrePostSessions = allSessions.Where(a => a.LinkedGroupId != null).ToList();
            var mgStandardSessions = allSessions.Where(a => a.LinkedGroupId == null).ToList();

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

            // Phase 338 CIL-01: aggregate counter sebelum filter apply (untuk badge UI)
            ViewBag.OpenCount     = grouped.Count(g => g.GroupStatus == "Open");
            ViewBag.UpcomingCount = grouped.Count(g => g.GroupStatus == "Upcoming");
            ViewBag.ClosedCount   = grouped.Count(g => g.GroupStatus == "Closed");

            // Phase 338 CIL-02: hanya hide Closed default ketika BOTH statusFilter kosong DAN search kosong
            // (sebelumnya search "Cilacap" return 0 karena Closed di-filter walau user search spesifik)
            if (string.IsNullOrEmpty(statusFilter) && string.IsNullOrEmpty(search))
                grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
            else if (statusFilter == "Open" || statusFilter == "Upcoming" || statusFilter == "Closed")
                grouped = grouped.Where(g => g.GroupStatus == statusFilter).ToList();

            var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);

            ViewBag.ManagementData = grouped.Skip(paging.Skip).Take(paging.Take).ToList();
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.PageSize = paging.Take;   // MAP-21: expose paging.Take (drop magic-number 20 di view)
            ViewBag.SearchTerm = search;
            ViewBag.SelectedCategory = category ?? "";
            ViewBag.SelectedStatus = statusFilter ?? "";

            // Categories juga dibutuhkan oleh partial view (filter dropdown render).
            // Phase 311 Plan 03 (D-04): wrap dengan IMemoryCache.GetOrCreateAsync TTL 5 menit absolute expiration.
            // Cache invalidation di Add/Edit/DeleteCategory setelah SaveChangesAsync.
            ViewBag.Categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.AssessmentSessions
                    .AsNoTracking()
                    .Select(a => a.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            });

            sw.Stop();
            _logger.LogInformation(
                "ManageAssessment perf [tab=assessment]: elapsed={Ms}ms search_present={SearchPresent} page={Page}",
                sw.ElapsedMilliseconds, !string.IsNullOrEmpty(search), page);

            return PartialView("Shared/_AssessmentGroupsTab", null);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ManageAssessmentTab_Training(string? search, int page = 1, int pageSize = 20,
            string? section = null, string? unit = null, string? category = null, string? statusFilter = null,
            string? isFiltered = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // MAM-06: isInitialState turun dari absennya filter (bukan hardcode) → empty-state hidup, skip full-roster.
            bool isInitialState = IsTrainingInitialState(isFiltered, section, unit, category, statusFilter, search);
            ViewBag.IsInitialState = isInitialState;
            ViewBag.SelectedSection = section;
            ViewBag.SelectedUnit = unit;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedStatus = statusFilter;
            ViewBag.SearchTerm = search;

            // MAM-07: paginate di caller (pola CMPController.cs:776) — JANGAN ubah GetWorkersInSection signature.
            List<WorkerTrainingStatus> workers;
            if (isInitialState)
            {
                workers = new List<WorkerTrainingStatus>();
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalCount = 0;
                ViewBag.PageSize = pageSize;
            }
            else
            {
                var fullList = await _workerDataService.GetWorkersInSection(section, unit, category, search, statusFilter);
                var pageSizeValidated = (pageSize == 20 || pageSize == 50 || pageSize == 100) ? pageSize : 20;
                var paging = HcPortal.Helpers.PaginationHelper.Calculate(fullList.Count, page, pageSizeValidated);
                workers = fullList.Skip(paging.Skip).Take(paging.Take).ToList();
                ViewBag.CurrentPage = paging.CurrentPage;
                ViewBag.TotalPages = paging.TotalPages;
                ViewBag.TotalCount = paging.TotalCount;
                ViewBag.PageSize = paging.Take;
            }

            ViewBag.Workers = workers;

            // T4 (Sections + Units) — required untuk dropdown filter render
            ViewBag.TrainingSections = await _context.GetAllSectionsAsync();
            ViewBag.TrainingUnits = !string.IsNullOrEmpty(section)
                ? await _context.GetUnitsForSectionAsync(section)
                : new List<string>();

            sw.Stop();
            _logger.LogInformation(
                "ManageAssessment perf [tab=training]: elapsed={Ms}ms search_present={SearchPresent} page={Page}",
                sw.ElapsedMilliseconds, !string.IsNullOrEmpty(search), page);

            return PartialView("Shared/_TrainingRecordsTab", null);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        // MAP-22: drop param mati page/pageSize/statusFilter (History pakai client-filter, tak paginate server-side).
        // CATATAN Pitfall 8: ManageAssessmentTab_Training page/pageSize SEKARANG DIPAKAI (MAM-07) — JANGAN drop di sana.
        public async Task<IActionResult> ManageAssessmentTab_History(string? search)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var (assessmentHistory, trainingHistory) = await _workerDataService.GetAllWorkersHistory();

            ViewBag.AssessmentHistory = assessmentHistory;
            ViewBag.TrainingHistory = trainingHistory;
            ViewBag.AssessmentTitles = assessmentHistory
                .Select(r => r.Title)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct().OrderBy(t => t).ToList();
            ViewBag.SearchTerm = search;

            sw.Stop();
            _logger.LogInformation(
                "ManageAssessment perf [tab=history]: elapsed={Ms}ms search_present={SearchPresent}",
                sw.ElapsedMilliseconds, !string.IsNullOrEmpty(search));

            return PartialView("Shared/_HistoryTab", null);
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
            _cache.Remove(CategoriesCacheKey);  // Phase 311 Plan 03 (D-04 invalidation)

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
            _cache.Remove(CategoriesCacheKey);  // Phase 311 Plan 03 (D-04 invalidation)

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

            try
            {
                _context.AssessmentCategories.Remove(category);
                await _context.SaveChangesAsync();
                _cache.Remove(CategoriesCacheKey);  // Phase 311 Plan 03 (D-04 invalidation)
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Tidak bisa hapus kategori: masih ada data yang berelasi.";
                return RedirectToAction("ManageCategories");
            }

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
            ViewBag.CreationMode = "";

            return View(model);
        }

        // GET /Admin/CheckTitleAvailability?title=... — cek judul assessment sudah dipakai (cegah double sertifikat).
        // judul-fleksibel-cek-duplikat (2026-06-15). Read-only → tanpa antiforgery.
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CheckTitleAvailability(string title)
        {
            var matches = await FindTitleDuplicatesAsync(_context, title);
            return Json(new
            {
                exists = matches.Count > 0,
                groupCount = matches.Count,
                matches = matches.Select(m => new { category = m.Category, tanggal = m.Tanggal, peserta = m.Peserta })
            });
        }

        // POST: Process form submission (multi-user)
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssessment(
            AssessmentSession model, List<string> UserIds,
            string? RenewalFkMap = null, string? RenewalFkMapType = null,
            string? CreationMode = null,
            DateTime? PreSchedule = null, int? PreDurationMinutes = null, DateTime? PreExamWindowCloseDate = null,
            DateTime? PostSchedule = null, int? PostDurationMinutes = null, DateTime? PostExamWindowCloseDate = null,
            bool SamePackage = false,
            bool ConfirmDuplicateTitle = false)
        {
            // Remove single UserId from validation since we use UserIds list
            ModelState.Remove("UserId");

            // Phase 338 REST-06 (336-NAMING-CONVENTION-SPEC): Auto-pair LinkedGroupId via title pattern
            // Hanya untuk standard mode (non PrePost). PrePost mode generate own GroupId di logic existing.
            if (CreationMode != "PrePostTest"
                && model.LinkedGroupId == null
                && !string.IsNullOrEmpty(model.Title))
            {
                var counterpartId = await TryAutoDetectCounterpartGroup(model.Title, model.Category);
                if (counterpartId.HasValue)
                {
                    model.LinkedGroupId = counterpartId.Value;
                    TempData["Info"] = $"Auto-paired LinkedGroupId={counterpartId.Value} berdasarkan title pattern '{model.Title}' (336-NAMING-CONVENTION).";
                }
            }

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
            bool isPrePostMode = CreationMode == "PrePostTest";

            // Phase 308 D-04: Status field hidden in PrePost mode — JS sets default 'Upcoming', server skips [Required] validation
            if (isPrePostMode)
            {
                ModelState.Remove("Status");
            }

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

            // ExamWindowCloseDate validation
            ModelState.Remove("ExamWindowCloseDate"); // Remove model binding error first
            if (!isPrePostMode)
            {
                if (!model.ExamWindowCloseDate.HasValue)
                {
                    ModelState.AddModelError("ExamWindowCloseDate", "Tanggal tutup ujian wajib diisi.");
                }
                else if (model.ExamWindowCloseDate < model.Schedule)
                {
                    ModelState.AddModelError("ExamWindowCloseDate", "Tanggal tutup ujian tidak boleh sebelum tanggal jadwal.");
                }
            }
            // ValidUntil: opsional di normal mode, wajib di renewal mode
            bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue || !string.IsNullOrEmpty(RenewalFkMap);
            ModelState.Remove("ValidUntil");
            if (isRenewalModePost && !model.ValidUntil.HasValue)
            {
                ModelState.AddModelError("ValidUntil", "Tanggal expired sertifikat wajib diisi untuk renewal.");
            }

            // judul-fleksibel-cek-duplikat (2026-06-15): soft-block judul kembar (cegah double sertifikat).
            // Berlaku standard + PrePost. Skip renewal (judul sengaja reuse sertifikat asal). Override via konfirmasi.
            if (!string.IsNullOrWhiteSpace(model.Title)
                && !isRenewalModePost
                && !ConfirmDuplicateTitle)
            {
                var dupMatches = await FindTitleDuplicatesAsync(_context, model.Title);
                if (dupMatches.Count > 0)
                {
                    ModelState.AddModelError("Title",
                        $"Judul '{model.Title}' sudah dipakai di {dupMatches.Count} assessment. " +
                        "Centang konfirmasi di bawah untuk tetap membuat dengan judul sama.");
                    ViewBag.DuplicateTitleWarning = true;
                }
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

            // Phase 423 CERT-05/VAL-04 (D-07): anti double-cert guard UNCONDITIONAL.
            // Cegah penerbitan cert kedua yang AKTIF untuk (peserta, judul) sama. Renewal (RenewsSessionId/RenewsTrainingId)
            // DIKECUALIKAN — perpanjangan resmi. Guard ini SETARA pola double-renewal di atas (di LUAR cabang !ConfirmDuplicateTitle),
            // jadi TIDAK bisa di-bypass via ConfirmDuplicateTitle (Pitfall 3). Hanya relevan saat cert akan terbit (GenerateCertificate).
            if (model.GenerateCertificate && !isRenewalModePost
                && !string.IsNullOrWhiteSpace(model.Title) && UserIds != null && UserIds.Count > 0)
            {
                var blocked = new List<string>();
                foreach (var uid in UserIds.Distinct())
                {
                    if (await HasActiveCertForTitleAsync(uid, model.Title, null))
                        blocked.Add(uid);
                }
                if (blocked.Count > 0)
                {
                    ModelState.AddModelError("",
                        $"{blocked.Count} peserta sudah memiliki sertifikat aktif untuk judul ini. " +
                        "Cert tidak bisa diterbitkan ganda (gunakan mode renewal bila ingin perpanjang).");
                }
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

            // T-297-01: Validate CreationMode hanya "Standard" atau "PrePostTest"
            // FORM-10: penanda mode internal (Standard/PrePostTest) — TIDAK rancu dgn kolom DB AssessmentType (PreTest/PostTest/Standard/Manual).
            if (!string.IsNullOrEmpty(CreationMode) && CreationMode != "Standard" && CreationMode != "PrePostTest")
            {
                ModelState.AddModelError("CreationMode", "Tipe assessment tidak valid.");
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

                // EWCD wajib untuk Pre-Post
                if (!PreExamWindowCloseDate.HasValue)
                    ModelState.AddModelError("PreExamWindowCloseDate", "Batas waktu pengerjaan Pre-Test wajib diisi.");
                if (!PostExamWindowCloseDate.HasValue)
                    ModelState.AddModelError("PostExamWindowCloseDate", "Batas waktu pengerjaan Post-Test wajib diisi.");

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
                string? protonTrackType = null;
                int protonUrutan = 0;
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
                    protonTrackType = protonTrack.TrackType;
                    protonUrutan = protonTrack.Urutan;
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
                            ShuffleQuestions = model.ShuffleQuestions,
                            ShuffleOptions = model.ShuffleOptions,
                            AllowRetake = false,                                            // FORM-02 / D-03: Pre baseline murni — retake OFF eksplisit
                            MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),             // disalin untuk konsistensi grup (perilaku OFF)
                            RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
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
                                ShuffleQuestions = model.ShuffleQuestions,
                                ShuffleOptions = model.ShuffleOptions,
                                AllowRetake = model.AllowRetake,                                // FORM-02 Post: retake relevan → salin penuh dari model
                                MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),             // FORM-02 + clamp V5
                                RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
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
                                RenewsTrainingId = model.RenewsTrainingId,
                                SamePackage = SamePackage
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

                        TempData["SuccessMessage"] = $"Assessment Pre-Post Test '{model.Title}' berhasil dibuat untuk {UserIds.Count} peserta ({preSessions.Count + postSessions.Count} sesi).";

                        var pptCreatedSessions = new List<object>();
                        for (int i = 0; i < preSessions.Count; i++)
                        {
                            var assignedUser = userDictionary[UserIds[i]];
                            pptCreatedSessions.Add(new
                            {
                                PreId = preSessions[i].Id,
                                PostId = postSessions[i].Id,
                                UserId = UserIds[i],
                                UserName = assignedUser.FullName ?? UserIds[i],
                                UserEmail = assignedUser.Email ?? ""
                            });
                        }

                        TempData["CreatedAssessment"] = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Count = UserIds.Count,
                            Title = model.Title,
                            Category = model.Category,
                            IsPrePostTest = true,
                            PreSchedule = PreSchedule!.Value.ToString("dd MMMM yyyy HH:mm"),
                            PostSchedule = PostSchedule!.Value.ToString("dd MMMM yyyy HH:mm"),
                            PreDurationMinutes = PreDurationMinutes!.Value,
                            PostDurationMinutes = PostDurationMinutes!.Value,
                            Status = "Upcoming",
                            IsTokenRequired = model.IsTokenRequired,
                            AccessToken = model.AccessToken,
                            SamePackage = SamePackage,
                            Sessions = pptCreatedSessions
                        });

                        return RedirectToAction("CreateAssessment");
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

                // Phase 359 (PCOMP-06/07/08) — gate eligibility server-side untuk Assessment Proton.
                // BUKAN all-or-nothing (D-01): worker tak-eligible di-SKIP, eligible tetap dapat session.
                var eligibleUserIds = new List<string>(UserIds);
                int gateSkippedNotHundred = 0, gateSkippedPrevYear = 0;
                if (model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue && model.ProtonTrackId.Value > 0)
                {
                    int protonTrackId = model.ProtonTrackId.Value;
                    string trackType = protonTrackType ?? "";
                    // prevTahunKe = TahunKe track dgn TrackType sama & Urutan-1; null jika Urutan<=1 (Tahun 1).
                    string? prevTahunKe = protonUrutan > 1
                        ? await _context.ProtonTracks
                            .Where(t => t.TrackType == trackType && t.Urutan == protonUrutan - 1)
                            .Select(t => t.TahunKe).FirstOrDefaultAsync()
                        : null;
                    // T9/D-12: log-only — Urutan<=1 prevTahunKe==null itu normal (Tahun 1, tanpa prasyarat)
                    if (protonUrutan > 1 && prevTahunKe == null)
                        _logger.LogWarning("CreateAssessment gate: prevTahunKe null padahal protonUrutan={Urutan} > 1 (TrackType={TrackType}) — Urutan tidak kontigu. Cross-year gate dilewati untuk track ini.", protonUrutan, trackType);

                    // Deliverable track (D-08 fallback): jika kosong → skip cek 100% (interview-only/transisi).
                    var trackDeliverableIds = await _context.ProtonKompetensiList
                        .Where(k => k.ProtonTrackId == protonTrackId)
                        .SelectMany(k => k.SubKompetensiList)
                        .SelectMany(s => s.Deliverables)
                        .Select(d => d.Id)
                        .ToListAsync();
                    bool trackHasDeliverables = trackDeliverableIds.Any();

                    // Renewal: tetap WAJIB lewat gate 100% (D-07), TAPI cross-year prereq di-exempt
                    // (renewal = perpanjangan tahun yang SUDAH dilewati).
                    bool isRenewal = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue;

                    // Phase 401 (D-03): actor untuk gate-block AuditLog (resolve sekali sebelum loop).
                    var actor = await _userManager.GetUserAsync(User);
                    var filtered = new List<string>();
                    foreach (var uid in UserIds)
                    {
                        // (a) Cross-year gate (D-03/D-07): penanda Tahun N-1 (kecuali renewal).
                        // Phase 360 (D-06a/A-M4): assignment ber-Origin="Bypass" exempt cek cross-year prereq.
                        bool isBypassAssignment = await _context.ProtonTrackAssignments
                            .AnyAsync(a => a.CoacheeId == uid && a.ProtonTrackId == protonTrackId
                                        && a.IsActive && a.Origin == "Bypass");
                        if (!isRenewal && !isBypassAssignment
                            && !await _protonCompletionService.IsPrevYearPassedAsync(uid, trackType, prevTahunKe))
                        {
                            gateSkippedPrevYear++; continue;
                        }
                        // (b) Deliverable 100% per-unit (D-02). Fallback: track 0 deliverable → eligible (D-08).
                        if (trackHasDeliverables)
                        {
                            // Phase 401 (PSU-01): resolusi unit PROTON HANYA dari AssignmentUnit (fallback User.Unit DIBUANG — ambigu multi-unit).
                            var resolvedUnit = await _context.CoachCoacheeMappings
                                .Where(m => m.CoacheeId == uid && m.IsActive).Select(m => m.AssignmentUnit).FirstOrDefaultAsync();
                            if (string.IsNullOrWhiteSpace(resolvedUnit))
                            {
                                // PSU-05 / D-02: gate penerbitan session/cert — BLOCK (tak boleh terbit dgn unit ter-resolve dari primary).
                                // D-03 channel = AuditLog persisted (event langka & signifikan) + LogWarning.
                                await _auditLog.LogAsync(actor?.Id ?? "system", actor?.FullName ?? actor?.UserName ?? "system",
                                    "ProtonUnitUnresolved",
                                    $"Coachee {uid} di-skip dari gate eligibility exam (penerbitan session/cert): AssignmentUnit kosong — tak boleh resolve dari Unit primary.",
                                    targetType: "CoachCoacheeMapping");
                                _logger.LogWarning("Cert-gate skip: coachee {Uid} AssignmentUnit kosong (BLOCK penerbitan).", uid);
                                gateSkippedNotHundred++; continue;
                            }
                            var unitDeliverableIds = await _context.ProtonDeliverableList
                                .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
                                         && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())
                                .Select(d => d.Id).ToListAsync();
                            var myStatuses = await _context.ProtonDeliverableProgresses
                                .Where(p => p.CoacheeId == uid && unitDeliverableIds.Contains(p.ProtonDeliverableId))
                                .Select(p => p.Status).ToListAsync();
                            if (!CoacheeEligibilityCalculator.IsEligiblePerUnit(myStatuses, unitDeliverableIds.Count))
                            { gateSkippedNotHundred++; continue; }
                        }
                        filtered.Add(uid);
                    }
                    eligibleUserIds = filtered;
                }

                // Phase 359 (D-01) — empty-result guard: semua pekerja di-skip → JANGAN buka transaksi.
                if (model.Category == "Assessment Proton" && eligibleUserIds.Count == 0)
                {
                    TempData["Warning"] = $"0 session dibuat. Semua {UserIds.Count} pekerja di-skip. Alasan: {gateSkippedNotHundred} belum 100% deliverable, {gateSkippedPrevYear} Tahun sebelumnya belum lulus.";
                    var usersReload = await _context.Users
                        .Where(u => u.IsActive)
                        .OrderBy(u => u.FullName)
                        .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                        .ToListAsync();
                    ViewBag.Users = usersReload;
                    ViewBag.SelectedUserIds = UserIds ?? new List<string>();
                    ViewBag.Sections = await _context.GetAllSectionsAsync();
                    ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
                    return View("CreateAssessment", model);
                }

                // Create all sessions in memory first
                var sessions = new List<AssessmentSession>();

                for (int i = 0; i < eligibleUserIds.Count; i++)
                {
                    var userId = eligibleUserIds[i];
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
                        ShuffleQuestions = model.ShuffleQuestions,
                        ShuffleOptions = model.ShuffleOptions,
                        AllowRetake = model.AllowRetake,                                  // FORM-02 std: retake disalin eksplisit (bukan EF-default)
                        MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),               // FORM-02 + clamp V5
                        RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
                        GenerateCertificate = model.GenerateCertificate,
                        ExamWindowCloseDate = model.ExamWindowCloseDate,
                        ValidUntil = model.ValidUntil,
                        NomorSertifikat = null, // Phase 227 CLEN-04: generated in SubmitExam when IsPassed=true
                        Progress = 0,
                        UserId = userId,
                        AssessmentType = "Standard", // ISS-04 fix: DB column NOT NULL, must set explicit value (EF tidak memakai DB default)
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

                    // Phase 359 (S1) — skip-summary Proton banner (_Layout), setelah commit sukses.
                    // Pisah dari popup TempData["CreatedAssessment"]; tidak menggandakan pesan non-Proton.
                    if (model.Category == "Assessment Proton")
                    {
                        int gateSkippedTotal = gateSkippedNotHundred + gateSkippedPrevYear;
                        if (gateSkippedTotal == 0)
                            TempData["Success"] = $"{eligibleUserIds.Count} session berhasil dibuat.";
                        else
                            TempData["Warning"] = $"{eligibleUserIds.Count} session dibuat, {gateSkippedTotal} di-skip. Alasan: {gateSkippedNotHundred} belum 100% deliverable, {gateSkippedPrevYear} Tahun sebelumnya belum lulus.";

                        // Audit warn-only bila ada skip (TIDAK boleh memutus operasi).
                        if (gateSkippedTotal > 0)
                        {
                            try
                            {
                                var gateActor = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
                                await _auditLog.LogAsync(
                                    currentUser?.Id ?? "",
                                    gateActor,
                                    "CreateAssessment_GateSkip",
                                    $"Gate Proton '{model.Title}': {eligibleUserIds.Count} dibuat, {gateSkippedNotHundred} belum 100% deliverable, {gateSkippedPrevYear} Tahun sebelumnya belum lulus.",
                                    sessions.FirstOrDefault()?.Id,
                                    "AssessmentSession");
                            }
                            catch (Exception auditEx) { _logger.LogWarning(auditEx, "Audit gate-skip logging failed (non-blocking)"); }
                        }
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
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
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
                IsPrePostTest = false,
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

            // FORM-06 (E-08): sesi entry-manual diarahkan ke form edit manual (bukan form online).
            // Mirror filter IsManualEntry di TrainingAdminController EditManualAssessment GET.
            if (assessment.IsManualEntry)
                return RedirectToAction("EditManualAssessment", "TrainingAdmin", new { id });

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
            List<string>? UserIds, bool confirmRemoveWithHistory = false)   // v32.7 RTH-04/D-06: server round-trip flag
        {
            var assessment = await _context.AssessmentSessions.FindAsync(id);
            if (assessment == null)
            {
                TempData["Error"] = "Assessment not found.";
                return RedirectToAction("ManageAssessment");
            }

            // FORM-05 (E-04): lock metadata sesi/grup Completed — diangkat ke ATAS cabang Pre-Post.
            // Bug asal: cabang Pre-Post return ke ManageAssessment SEBELUM guard lama (single-mode) di
            // bawah cabang ini tercapai → guard tak pernah jalan untuk Pre-Post. Group-aware (Open Q#1):
            // blokir bila ADA SATU sesi dalam grup (LinkedGroupId sama) berstatus Completed. Standard
            // (LinkedGroupId null) → cek sesi itu sendiri (identik guard lama). JANGAN pakai
            // AssessmentEditEligibility.IsEditableAsync (semantik TERBALIK). Guard lama :2006 dibiarkan
            // (defense-in-depth jalur standard di bawah).
            bool isCompleted = assessment.Status == "Completed";
            if (assessment.LinkedGroupId.HasValue)
            {
                isCompleted = await _context.AssessmentSessions
                    .AnyAsync(a => a.LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed");
            }
            if (isCompleted)
            {
                TempData["Error"] = "Tidak dapat mengubah assessment yang sudah Completed.";
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

                // WSE-02 (D-01b): normalize token uppercase for Pre/Post writes (mirror CreateAssessment :1104-1108).
                // This branch returns at :ManageAssessment before the single-mode uppercase, so it is the only
                // normalization the Pre/Post path gets. Defensive compare (D-01a) heals existing rows; this keeps new writes clean.
                string normalizedToken = (model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken))
                    ? model.AccessToken.ToUpper()
                    : "";

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
                    s.ShuffleQuestions = model.ShuffleQuestions;
                    s.ShuffleOptions = model.ShuffleOptions;
                    s.IsTokenRequired = model.IsTokenRequired;
                    // D-01b: store uppercase; preserve fallback-to-existing when model supplies no token (do NOT wipe).
                    s.AccessToken = model.IsTokenRequired ? (normalizedToken != "" ? normalizedToken : (s.AccessToken ?? "")) : "";
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

                // v32.7 RTH-04 (PA-06, D-06): peserta ber-riwayat yang dibatalkan penghapusannya (butuh konfirmasi flag).
                // Dideklarasi DI LUAR blok agar terlihat di cek redirect pasca-loop.
                var pendingHistoryRemovals = new List<string>();
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

                        // v32.7 RTH-04 (PA-06, D-06): soft-confirm — sesi Abandoned / sudah-dimulai / ber-AttemptHistory
                        // (riwayat percobaan tercatat walau status kembali Open via reset) = HAPUS RIWAYAT.
                        // Server-authoritative: flag confirmRemoveWithHistory HANYA menekan peringatan; guard
                        // dievaluasi ulang tiap POST (forged flag tetap melewati guard hard-block di atas).
                        bool preHasHistory = userPreSession != null &&
                            (userPreSession.Status == "Abandoned" || userPreSession.StartedAt != null);
                        bool postHasHistory = userPostSession != null &&
                            (userPostSession.Status == "Abandoned" || userPostSession.StartedAt != null);
                        var sessIdsForHistCheck = new List<int>();
                        if (userPreSession != null) sessIdsForHistCheck.Add(userPreSession.Id);
                        if (userPostSession != null) sessIdsForHistCheck.Add(userPostSession.Id);
                        bool hasAttemptHistory = sessIdsForHistCheck.Count > 0 && await _context.AssessmentAttemptHistory
                            .AnyAsync(h => sessIdsForHistCheck.Contains(h.SessionId));
                        if ((preHasHistory || postHasHistory || hasAttemptHistory) && !confirmRemoveWithHistory)
                        {
                            pendingHistoryRemovals.Add(removedUserId);
                            continue;   // BATALKAN penghapusan peserta INI; edit metadata + peserta lain tetap jalan
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
                            ShuffleQuestions = model.ShuffleQuestions,
                            ShuffleOptions = model.ShuffleOptions,
                            IsTokenRequired = model.IsTokenRequired,
                            AccessToken = model.IsTokenRequired ? normalizedToken : "",
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
                            ShuffleQuestions = model.ShuffleQuestions,
                            ShuffleOptions = model.ShuffleOptions,
                            IsTokenRequired = model.IsTokenRequired,
                            AccessToken = model.IsTokenRequired ? normalizedToken : "",
                            GenerateCertificate = model.GenerateCertificate,
                            ValidUntil = model.ValidUntil,
                            UserId = newUserId,
                            AssessmentType = "PostTest",
                            LinkedGroupId = linkedGroupId,
                            CreatedBy = currentUser?.Id,
                            BannerColor = repPost.BannerColor,
                            SamePackage = repPost.SamePackage   // SHFX-04/PA-02: peserta baru warisi SamePackage grup (bukan default false)
                        };

                        _context.AssessmentSessions.AddRange(newPre, newPost);
                        await _context.SaveChangesAsync();

                        newPre.LinkedSessionId = newPost.Id;
                        newPost.LinkedSessionId = newPre.Id;
                    }
                }

                await _context.SaveChangesAsync();
                // v32.7 RTH-04 (D-06): bila ada peserta ber-riwayat yang penghapusannya dibatalkan, kembali ke
                // form Edit dengan peringatan + keep-list (UserIds) tersimpan → tombol "Tetap Hapus" me-replay
                // UserIds + confirmRemoveWithHistory=true (server round-trip, BUKAN JS-only). Metadata + peserta
                // tanpa-riwayat SUDAH tersimpan (SaveChanges di atas). Non-history → flow ManageAssessment normal.
                if (pendingHistoryRemovals.Any())
                {
                    TempData["PendingRemoveCount"] = pendingHistoryRemovals.Count;
                    TempData["PendingKeepUserIds"] = string.Join(",", UserIds ?? new List<string>());
                    TempData["Warning"] = $"{pendingHistoryRemovals.Count} peserta memiliki riwayat ujian (sesi sudah dimulai/ditinggalkan/percobaan tercatat). Menghapusnya akan menghapus riwayat percobaan terkait. Klik 'Tetap Hapus' untuk melanjutkan.";
                    return RedirectToAction("EditAssessment", new { id });
                }
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
                sibling.ShuffleQuestions = model.ShuffleQuestions;
                sibling.ShuffleOptions = model.ShuffleOptions;
                sibling.GenerateCertificate = model.GenerateCertificate;
                sibling.ExamWindowCloseDate = model.ExamWindowCloseDate;
                sibling.ValidUntil = model.ValidUntil;                                       // FORM-04 (E-05): ValidUntil tersimpan jalur std
                sibling.AllowRetake = model.AllowRetake;                                     // FORM-03 (E-03): retake bukan no-op
                sibling.MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5);                   // FORM-03 + clamp V5
                sibling.RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168); // FORM-03 + clamp V5
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
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
                logger.LogError(ex, "Error updating assessment");
                TempData["Error"] = "Gagal memperbarui assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }

            // ===== BULK ASSIGN: create new sessions for selected users =====
            if (NewUserIds != null && NewUserIds.Count > 0)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
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
                                ShuffleQuestions = savedAssessment.ShuffleQuestions,
                                ShuffleOptions = savedAssessment.ShuffleOptions,
                                // v32.4 RTK-01: pekerja baru mewarisi policy retake dari sibling existing (bukan EF-default diam-diam).
                                AllowRetake = savedAssessment.AllowRetake,
                                MaxAttempts = savedAssessment.MaxAttempts,
                                RetakeCooldownHours = savedAssessment.RetakeCooldownHours,
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
                    var logger2 = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
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
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
            var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();

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

                // D-19: Block delete individual jika bagian Pre-Post group (shared IsPrePostSession — single-source tab-1/tab-2)
                if (IsPrePostSession(assessment))
                {
                    TempData["Error"] = "Sesi ini bagian dari grup Pre-Post Test. Gunakan 'Hapus Grup' untuk menghapus keduanya.";
                    return RedirectToAction("ManageAssessment");
                }

                // Phase 367 L-03: pre-check renewal BLOKIR (fase 325) DIBALIK → cascade penuh via engine
                // (turunan renewal IKUT terhapus, parity tab 2 — fix kasus Rino #3). Engine hapus DB
                // (root + turunan) + artefak per node + cert per node + audit (1-tx, preview==execute).
                // Image SOAL (Opsi B) + #19 cert = ranah endpoint: collect SEBELUM engine, hapus file POST engine (warn-only).

                // Cascade node ids (root + turunan renewal) = SAMA dgn yg dihapus engine (CollectCascadeIds).
                var cascadeNodes = await cascade.CollectCascadeIds("session", id);
                var cascadeSessionIds = cascadeNodes.Where(n => n.Type == "session").Select(n => n.Id).ToList();

                // Image SOAL SEMUA session node (engine Opsi B tak sentuh image SOAL → cegah orphan turunan).
                var imagePaths = await CollectQuestionImagePathsAsync(cascadeSessionIds);

                // Load SELURUH session cascade (root + turunan) utk: (a) gate izin HC atas SEMUA node, (b) cert.
                var cascadeSessions = await _context.AssessmentSessions
                    .Where(a => cascadeSessionIds.Contains(a.Id)).ToListAsync();

                // #19: file sertifikat manual semua session node (engine juga hapus — idempotent File.Exists).
                var certPaths = cascadeSessions
                    .Where(s => !string.IsNullOrEmpty(s.ManualSertifikatUrl))
                    .Select(s => s.ManualSertifikatUrl!)
                    .ToList();

                // Snapshot audit
                string preDeleteStatus = assessment.Status;
                int preDeleteResponseCount = await _context.PackageUserResponses
                    .CountAsync(r => cascadeSessionIds.Contains(r.AssessmentSessionId));

                // Actor untuk audit (engine + endpoint)
                var actorUser = await _userManager.GetUserAsync(User);
                var actorId = actorUser?.Id ?? "";
                var actorName = string.IsNullOrWhiteSpace(actorUser?.NIP) ? (actorUser?.FullName ?? "Unknown") : $"{actorUser.NIP} - {actorUser.FullName}";

                // PHASE 312 role-tier guard (D-04, T-312-01) — gate izin HC atas SELURUH set cascade (root + turunan),
                // BUKAN cuma root. Fix temuan kritis 367-05: cegah HC menghapus turunan Completed/ber-jawaban via
                // ancestor (engine tak punya role guard). EnsureCanDeleteAsync read-only; Admin override tetap lewat.
                // Diposisikan paling akhir SEBELUM engine → window TOCTOU minimal (no in-tx re-check; residual diterima).
                var blockResult = await EnsureCanDeleteAsync(
                    "DeleteAssessment",
                    id,
                    "AssessmentSession",
                    cascadeSessions);
                if (blockResult != null) return blockResult;

                var result = await cascade.ExecuteAsync("session", id, Enumerable.Empty<int>(), actorId, actorName);
                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Gagal menghapus assessment. Silakan coba lagi.";
                    return RedirectToAction("ManageAssessment");
                }

                // Phase 366: hapus file gambar SOAL orphan POST cascade. Post-commit AnyAsync auto-sadar batch +
                // shared Pre/Post selamat (D-05). logger lokal (BUKAN _logger).
                await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, logger, imagePaths, "DeleteAssessment image");

                // #19: hapus file sertifikat manual fisik POST-commit, warn-only (confined webroot V12).
                DeleteCertFiles(certPaths, logger);

                // Audit log endpoint (konteks aksi user-facing; engine juga catat CascadeDelete detail)
                try
                {
                    await _auditLog.LogAsync(
                        actorId,
                        actorName,
                        "DeleteAssessment",
                        $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} CascadeDeleted={result.DeletedCount}",
                        id,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id);
                }

                logger.LogInformation($"Successfully deleted assessment {id}: {assessmentTitle} (cascade {result.DeletedCount} record)");
                TempData["Success"] = $"Assessment '{assessmentTitle}' has been deleted successfully.";
                return RedirectToAction("ManageAssessment");
            }
            catch (DbUpdateException ex)
            {
                // Phase 325 P05 D-05: safety net jika race TOCTOU antara pre-check dan tx commit (concurrent insert renewal child).
                logger.LogWarning(ex, "Delete failed for AssessmentSession {Id}: FK constraint", id);
                TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting assessment {Id}", id);
                TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }
        }

        // #18 Phase 367: predikat sibling grup standard online — single-source dipakai query EF DeleteAssessmentGroup
        // + diuji SiblingFilterTests. Samakan scope dgn mgStandardSessions (LinkedGroupId==null): bukan Pre/Post group,
        // bukan PreTest/PostTest, bukan assessment manual (cegah over-deletion ke luar scope tampilan tab 1).
        public static System.Linq.Expressions.Expression<Func<AssessmentSession, bool>> StandardGroupSiblingPredicate(
            string title, string category, DateTime scheduleDate)
            => a => a.Title == title
                    && a.Category == category
                    && a.Schedule.Date == scheduleDate
                    && a.LinkedGroupId == null
                    && a.AssessmentType != "PreTest"
                    && a.AssessmentType != "PostTest"
                    && !a.IsManualEntry;

        // --- DELETE ASSESSMENT GROUP ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssessmentGroup(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
            var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();

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

                // Find all siblings (same Title + Category + Schedule.Date) — #18 Phase 367: filter scope grup standard
                // (LinkedGroupId==null && bukan Pre/Post && !manual) supaya tidak menyapu sesi di luar tampilan tab 1.
                var siblings = await _context.AssessmentSessions
                    .Where(StandardGroupSiblingPredicate(rep.Title, rep.Category, scheduleDate))
                    .ToListAsync();

                logger.LogInformation($"DeleteAssessmentGroup: deleting {siblings.Count} sessions for '{rep.Title}'");

                // Phase 367 L-03: pre-check renewal BLOKIR (fase 329) DIBALIK → cascade penuh PER SIBLING via engine
                // (turunan renewal IKUT terhapus, parity tab 2). Engine = 1-tx per sibling (atomik per-sibling,
                // BUKAN 1-tx semua sibling — trade-off engine-route). Image SOAL (Opsi B) + #19 cert = ranah endpoint.

                // Cascade node ids SEMUA sibling (root + turunan renewal). CollectCascadeIds = traversal SAMA dgn ExecuteAsync.
                var allCascadeSessionIds = new HashSet<int>();
                foreach (var sib in siblings)
                    foreach (var node in await cascade.CollectCascadeIds("session", sib.Id))
                        if (node.Type == "session") allCascadeSessionIds.Add(node.Id);

                // Image SOAL Distinct semua node (engine Opsi B tak sentuh image SOAL → cegah orphan turunan).
                var imagePaths = await CollectQuestionImagePathsAsync(allCascadeSessionIds);

                // Load SELURUH session cascade (sibling + turunan) utk: (a) gate izin HC atas SEMUA node, (b) cert.
                var cascadeSessions = await _context.AssessmentSessions
                    .Where(a => allCascadeSessionIds.Contains(a.Id)).ToListAsync();

                // #19: file sertifikat manual per session node (engine juga hapus — idempotent). Map id→url supaya
                // saat partial failure cert HANYA dihapus utk sesi yg BENAR-BENAR ter-commit (deletedSet) — cegah
                // hapus cert sesi yg masih ada (DeleteCertFiles tak punya ref-check, beda dgn ImageFileCleanup).
                var certById = cascadeSessions
                    .Where(s => !string.IsNullOrEmpty(s.ManualSertifikatUrl))
                    .ToDictionary(s => s.Id, s => s.ManualSertifikatUrl!);

                // Snapshot audit agregat
                string preDeleteStatus = string.Join(" / ", siblings.GroupBy(s => s.Status).Select(g => $"{g.Count()} {g.Key}"));
                int preDeleteResponseCount = await _context.PackageUserResponses
                    .CountAsync(r => allCascadeSessionIds.Contains(r.AssessmentSessionId));
                int preDeleteSessionCount = siblings.Count;

                // Actor untuk audit (engine + endpoint)
                var actorUser = await _userManager.GetUserAsync(User);
                var actorId = actorUser?.Id ?? "";
                var actorName = string.IsNullOrWhiteSpace(actorUser?.NIP) ? (actorUser?.FullName ?? "Unknown") : $"{actorUser.NIP} - {actorUser.FullName}";

                // PHASE 312 role-tier guard — gate izin HC atas SELURUH set cascade (sibling + turunan), BUKAN cuma
                // sibling (fix temuan kritis 367-05: cegah HC hapus turunan Completed/ber-jawaban via engine). Last
                // SEBELUM loop → window TOCTOU minimal. Admin override tetap lewat.
                var blockResult = await EnsureCanDeleteAsync(
                    "DeleteAssessmentGroup",
                    id,
                    "AssessmentSession",
                    cascadeSessions);
                if (blockResult != null) return blockResult;

                // Cascade per sibling UNIK (skip yg sudah ikut cascade sibling lain via deletedSet — cegah dobel).
                // Engine 1-tx per sibling → bila sibling ke-N gagal, sibling 1..N-1 SUDAH commit. JANGAN early-return
                // sebelum cleanup: file gambar/cert sibling yg sudah commit harus dibersihkan (cegah orphan, fix MED).
                int totalDeleted = 0;
                var deletedSet = new HashSet<int>();
                bool partialFailure = false;
                int? failedSiblingId = null;
                foreach (var sib in siblings)
                {
                    if (deletedSet.Contains(sib.Id)) continue;
                    var r = await cascade.ExecuteAsync("session", sib.Id, Enumerable.Empty<int>(), actorId, actorName);
                    if (!r.Success)
                    {
                        partialFailure = true;
                        failedSiblingId = sib.Id;
                        break;
                    }
                    totalDeleted += r.DeletedCount;
                    foreach (var sid in r.DeletedSessionIds) deletedSet.Add(sid);
                }

                // Cleanup file SELALU jalan utk sibling yg SUDAH commit (post-commit AnyAsync self-correct: hanya file
                // tak-direferensikan baris tersisa yg dihapus). Aman walau partialFailure.
                await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, logger, imagePaths, "DeleteAssessmentGroup image");
                DeleteCertFiles(deletedSet.Where(certById.ContainsKey).Select(d => certById[d]), logger);

                // Audit endpoint SELALU ditulis (refleksi hasil AKTUAL totalDeleted, partial-aware — fix MED repudiation).
                try
                {
                    await _auditLog.LogAsync(
                        actorId,
                        actorName,
                        partialFailure ? "DeleteAssessmentGroupPartial" : "DeleteAssessmentGroup",
                        $"Deleted assessment group '{rep.Title}' ({rep.Category}) [RepId={id}] SessionCount={preDeleteSessionCount} Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} CascadeDeleted={totalDeleted}{(partialFailure ? $" PARTIAL FailedAt={failedSiblingId}" : "")}",
                        id,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessmentGroup {Id}", id);
                }

                if (partialFailure)
                {
                    logger.LogWarning("DeleteAssessmentGroup partial: deleted {Count} record before failing at sibling {SibId}", totalDeleted, failedSiblingId);
                    TempData["Error"] = $"Sebagian sesi sudah dihapus ({totalDeleted} record), proses berhenti pada salah satu sesi. Silakan refresh halaman dan ulangi untuk sisa.";
                    return RedirectToAction("ManageAssessment");
                }

                logger.LogInformation($"DeleteAssessmentGroup: successfully deleted group '{rep.Title}' (cascade {totalDeleted} record)");
                TempData["Success"] = $"Assessment '{rep.Title}' and all {siblings.Count} assignment(s) deleted.";
                return RedirectToAction("ManageAssessment");
            }
            catch (DbUpdateException dbEx)
            {
                // Phase 329 D-04: safety net TOCTOU race antara pre-check Task 1 dan tx commit
                // (concurrent insert renewal child setelah pre-check passed). Paralel L2180.
                logger.LogWarning(dbEx, "DeleteAssessmentGroup FK violation for representative {Id}", id);
                TempData["Error"] = "Gagal hapus grup: ada constraint database yang dilanggar.";
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
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
            var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();

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

                logger.LogInformation($"DeletePrePostGroup: deleting {groupSessions.Count} sessions for '{groupTitle}' (LinkedGroupId={linkedGroupId})");

                // Phase 367 L-03: pre-check renewal BLOKIR (fase 329) DIBALIK → cascade penuh PER SESI via engine
                // (turunan renewal IKUT terhapus). Engine handle #8 LinkedSessionId null-clear pasangan Pre/Post.
                // Atomik per-sesi (trade-off engine-route). Image SOAL (Opsi B) + #19 cert = ranah endpoint.

                // Cascade node ids SEMUA sesi group (root + turunan renewal). CollectCascadeIds = traversal SAMA dgn ExecuteAsync.
                var allCascadeSessionIds = new HashSet<int>();
                foreach (var gs in groupSessions)
                    foreach (var node in await cascade.CollectCascadeIds("session", gs.Id))
                        if (node.Type == "session") allCascadeSessionIds.Add(node.Id);

                // Image SOAL Distinct semua node (engine Opsi B tak sentuh image SOAL → cegah orphan turunan).
                var imagePaths = await CollectQuestionImagePathsAsync(allCascadeSessionIds);

                // Load SELURUH session cascade (sesi group + turunan) utk: (a) gate izin HC atas SEMUA node, (b) cert.
                var cascadeSessions = await _context.AssessmentSessions
                    .Where(a => allCascadeSessionIds.Contains(a.Id)).ToListAsync();

                // #19: file sertifikat manual per session node (map id→url; partial failure hapus cert hanya sesi commit).
                var certById = cascadeSessions
                    .Where(s => !string.IsNullOrEmpty(s.ManualSertifikatUrl))
                    .ToDictionary(s => s.Id, s => s.ManualSertifikatUrl!);

                // Snapshot audit (breakdown Pre/Post) — AssessmentType per Models/AssessmentSession.cs
                var preSession = groupSessions.FirstOrDefault(s => s.AssessmentType == "PreTest");
                var postSession = groupSessions.FirstOrDefault(s => s.AssessmentType == "PostTest");
                string preDeleteStatus = $"PreTest:{preSession?.Status ?? "-"},PostTest:{postSession?.Status ?? "-"}";
                int preDeleteResponseCount = await _context.PackageUserResponses
                    .CountAsync(r => allCascadeSessionIds.Contains(r.AssessmentSessionId));

                // Actor
                var actorUser = await _userManager.GetUserAsync(User);
                var actorId = actorUser?.Id ?? "";
                var actorName = string.IsNullOrWhiteSpace(actorUser?.NIP) ? (actorUser?.FullName ?? "Unknown") : $"{actorUser.NIP} - {actorUser.FullName}";

                // PHASE 312 role-tier guard — gate izin HC atas SELURUH set cascade (sesi group + turunan), BUKAN cuma
                // sesi group (fix temuan kritis 367-05). Last SEBELUM loop → window TOCTOU minimal. Admin override lewat.
                var blockResult = await EnsureCanDeleteAsync(
                    "DeletePrePostGroup",
                    linkedGroupId,
                    "AssessmentSession",
                    cascadeSessions);
                if (blockResult != null) return blockResult;

                // Cascade per sesi UNIK (engine handle #8 null-clear pasangan; skip yg sudah ikut cascade lain).
                // 1-tx per sesi → bila gagal di tengah, sesi sebelumnya SUDAH commit: JANGAN early-return sebelum cleanup.
                int totalDeleted = 0;
                var deletedSet = new HashSet<int>();
                bool partialFailure = false;
                int? failedSessionId = null;
                foreach (var gs in groupSessions)
                {
                    if (deletedSet.Contains(gs.Id)) continue;
                    var r = await cascade.ExecuteAsync("session", gs.Id, Enumerable.Empty<int>(), actorId, actorName);
                    if (!r.Success)
                    {
                        partialFailure = true;
                        failedSessionId = gs.Id;
                        break;
                    }
                    totalDeleted += r.DeletedCount;
                    foreach (var sid in r.DeletedSessionIds) deletedSet.Add(sid);
                }

                // Cleanup file SELALU jalan utk sesi yg SUDAH commit (post-commit AnyAsync self-correct). Aman walau partial.
                await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, logger, imagePaths, "DeletePrePostGroup image");
                DeleteCertFiles(deletedSet.Where(certById.ContainsKey).Select(d => certById[d]), logger);

                // Audit endpoint SELALU ditulis (refleksi hasil AKTUAL, partial-aware — fix MED repudiation).
                try
                {
                    await _auditLog.LogAsync(
                        actorId,
                        actorName,
                        partialFailure ? "DeletePrePostGroupPartial" : "DeletePrePostGroup",
                        $"Deleted Pre-Post group '{groupTitle}' [LinkedGroupId={linkedGroupId}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} CascadeDeleted={totalDeleted}{(partialFailure ? $" PARTIAL FailedAt={failedSessionId}" : "")}",
                        linkedGroupId,
                        "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    logger.LogWarning(auditEx, "Audit log write failed for DeletePrePostGroup {LinkedGroupId}", linkedGroupId);
                }

                if (partialFailure)
                {
                    logger.LogWarning("DeletePrePostGroup partial: deleted {Count} record before failing at session {SibId}", totalDeleted, failedSessionId);
                    TempData["Error"] = $"Sebagian sesi sudah dihapus ({totalDeleted} record), proses berhenti. Silakan refresh halaman dan ulangi untuk sisa.";
                    return RedirectToAction("ManageAssessment");
                }

                TempData["Success"] = $"Grup Pre-Post Test '{groupTitle}' dan semua {groupSessions.Count} sesi berhasil dihapus.";
                return RedirectToAction("ManageAssessment");
            }
            catch (DbUpdateException dbEx)
            {
                // Phase 329 D-04: safety net TOCTOU race antara pre-check Task 2 dan tx commit
                // (concurrent insert renewal child setelah pre-check passed). Paralel L2180.
                logger.LogWarning(dbEx, "DeletePrePostGroup FK violation for LinkedGroupId {LinkedGroupId}", linkedGroupId);
                TempData["Error"] = "Gagal hapus grup Pre-Post: ada constraint database yang dilanggar.";
                return RedirectToAction("ManageAssessment");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeletePrePostGroup error for LinkedGroupId {LinkedGroupId}", linkedGroupId);
                TempData["Error"] = "Gagal menghapus grup Pre-Post Test. Silakan coba lagi.";
                return RedirectToAction("ManageAssessment");
            }
        }

        // Phase 367: helper cascade (CollectQuestionImagePathsAsync, DeleteCertFiles) dipindah ke
        // AdminBaseController (single-source tab-1 + tab-2; lihat AdminBaseController.cs).

        // --- REGENERATE TOKEN ---
        [HttpPost("{id:int}")]
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
                // MAM-01: Pre-Post route by LinkedGroupId (PostTest bisa beda tanggal — validasi cuma enforce PostSchedule > PreSchedule).
                // Fallback ke Title+Category+Schedule.Date untuk grup standard.
                var siblings = assessment.LinkedGroupId != null
                    ? await _context.AssessmentSessions
                        .Where(a => a.LinkedGroupId == assessment.LinkedGroupId)
                        .ToListAsync()
                    : await _context.AssessmentSessions
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
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
                logger.LogError(ex, "Error regenerating token");
                return Json(new { success = false, message = "Gagal regenerate token. Silakan coba lagi." });
            }
        }

        // --- PRIVATE HELPERS ---

        // MAM-04: derivasi UserStatus untuk Monitoring Detail. PendingGrading WAJIB dicek pertama —
        // session ber-essay punya Status="Menunggu Penilaian" + CompletedAt terisi BERSAMAAN,
        // jadi cek CompletedAt duluan akan salah-map "Completed". Static + pure → testable (xUnit).
        public static string DeriveUserStatus(string? status, DateTime? completedAt, DateTime? startedAt)
        {
            if (status == AssessmentConstants.AssessmentStatus.PendingGrading)
                return "Menunggu Penilaian";
            if (completedAt != null)
                return "Completed";
            if (status == "Cancelled")
                return "Dibatalkan";
            if (status == "Abandoned")
                return "Abandoned";
            if (startedAt != null)
                return "InProgress";
            return "Not started";
        }

        // MAM-06: Tab2 Input Records initial-state = TIDAK ada filter aktif. isFiltered hidden field di-post
        // saat user interaksi filter. Bila initial → skip full-roster query (empty-state "Pilih filter").
        public static bool IsTrainingInitialState(string? isFiltered, string? section, string? unit,
            string? category, string? statusFilter, string? search)
        {
            return string.IsNullOrEmpty(isFiltered)
                && string.IsNullOrEmpty(section)
                && string.IsNullOrEmpty(unit)
                && string.IsNullOrEmpty(category)
                && string.IsNullOrEmpty(statusFilter)
                && string.IsNullOrEmpty(search);
        }

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
            // Phase 370 (URG-02): window 7-hari dihapus — tampilan default tanpa batas umur.
            // D-05: .AsNoTracking() — read-only method (no SaveChanges), selaras Tab Assessment Phase 311.
            var query = _context.AssessmentSessions
                .AsNoTracking()
                .AsQueryable();

            // Text search by title
            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(lower)
                                      || a.Category.ToLower().Contains(lower));  // MAP-23: extend search ke Category (Nama/NIP TIDAK — list aggregate)
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
                        // D-11: stat gabungan dari Post. MAP-13: exclude Cancelled (apply ke postSubs/preSubs, bukan g) -> progress bisa 100%.
                        TotalCount = (postSubs.Count > 0 ? postSubs : preSubs).Count(a => a.Status != AssessmentConstants.AssessmentStatus.Cancelled),
                        CompletedCount = postSubs.Count(a => a.IsCompleted),
                        PassedCount = postSubs.Count(a => a.IsPassed),
                        PendingCount = postSubs.Count(a => !a.IsCompleted && !a.IsStarted),
                        InProgressCount = postSubs.Count(a => a.IsStarted && !a.IsCompleted),
                        CancelledCount = g.Count(a => a.Status == AssessmentConstants.AssessmentStatus.Cancelled),
                        // MAM-03: parity standardGroups (L2825). Grading terjadi di Post half — pakai postSubs.
                        MenungguPenilaianCount = postSubs.Count(a => a.IsMenungguPenilaian),
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
                        // MAP-13: exclude Cancelled -> progress bar bisa 100%; + CancelledCount parity (sebelumnya MISSING di standardGroups).
                        TotalCount = g.Count(a => a.Status != AssessmentConstants.AssessmentStatus.Cancelled),
                        CompletedCount = g.Count(a => a.IsCompleted),
                        PassedCount = g.Count(a => a.IsPassed),
                        PendingCount = g.Count(a => !a.IsCompleted && !a.IsStarted),
                        CancelledCount = g.Count(a => a.Status == AssessmentConstants.AssessmentStatus.Cancelled),
                        IsTokenRequired = rep.IsTokenRequired,
                        AccessToken = rep.AccessToken ?? "",
                        MenungguPenilaianCount = g.Count(a => a.IsMenungguPenilaian)
                    };
                }).ToList();

            // Gabungkan dan sort
            var grouped = prePostGroups.Concat(standardGroups)
                .OrderByDescending(g => g.Schedule)
                .ToList();

            // Phase 338 CIL-01: aggregate counter sebelum filter apply (untuk badge UI)
            ViewBag.OpenCount     = grouped.Count(g => g.GroupStatus == "Open");
            ViewBag.UpcomingCount = grouped.Count(g => g.GroupStatus == "Upcoming");
            ViewBag.ClosedCount   = grouped.Count(g => g.GroupStatus == "Closed");

            // Status filter — applied AFTER grouping (GroupStatus computed from sessions)
            // Default: show Open + Upcoming only (exclude Closed) unless status param is provided
            // Phase 338 CIL-02: hanya hide Closed default ketika BOTH status kosong DAN search kosong
            if (string.IsNullOrEmpty(status) && string.IsNullOrEmpty(search))
            {
                grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
                status = "active"; // signal to view that default active filter is on
            }
            else if (status == "Open" || status == "Upcoming" || status == "Closed")
            {
                grouped = grouped.Where(g => g.GroupStatus == status).ToList();
            }
            else if (string.IsNullOrEmpty(status) && !string.IsNullOrEmpty(search))
            {
                // MAP-15: search broaden scope (Closed ikut muncul) -> dropdown jujur "Semua Status".
                // Tanpa ini view default selStatus="active" (Open+Upcoming) padahal Closed tampil. Tak ubah hasil filter (CIL-02 preserved).
                status = "All";
            }
            // status == "All" → no filter applied

            ViewBag.SearchTerm = search ?? "";
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = category ?? "";

            // MAM-11: dropdown Kategori data-driven dari AssessmentCategories aktif (buang hardcode "Proton" phantom).
            ViewBag.MonitoringCategories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();

            return View(grouped);
        }

        // --- EDIT PESERTA ANSWERS (Phase 321) ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditPesertaAnswers(int id)
        {
            var session = await _context.AssessmentSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
            {
                TempData["Error"] = "Sesi tidak ditemukan.";
                return RedirectToAction("ManageAssessment");
            }

            if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
            {
                TempData["Error"] = "Sesi ini tidak dapat diedit (status bukan Completed, atau IsManualEntry, atau Assessment Proton Tahun 3).";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = session.Title, category = session.Category, scheduleDate = session.Schedule
                });
            }

            var assignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
            if (assignment == null) return NotFound();

            var shuffledIds = assignment.GetShuffledQuestionIds();
            var questions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => shuffledIds.Contains(q.Id))
                .ToListAsync();
            var responses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == id)
                .ToListAsync();

            var rows = new List<HcPortal.Models.EditQuestionRow>();
            foreach (var qId in shuffledIds)
            {
                var q = questions.FirstOrDefault(x => x.Id == qId);
                if (q == null) continue;
                var selectedIds = responses
                    .Where(r => r.PackageQuestionId == qId && r.PackageOptionId.HasValue)
                    .Select(r => r.PackageOptionId!.Value)
                    .ToList();
                var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
                bool isCorrect = (q.QuestionType ?? "MultipleChoice") switch
                {
                    "MultipleChoice" => selectedIds.Count == 1 && correctIds.Contains(selectedIds[0]),
                    "MultipleAnswer" => selectedIds.ToHashSet().SetEquals(correctIds.ToHashSet()),
                    _ => false
                };
                rows.Add(new HcPortal.Models.EditQuestionRow
                {
                    PackageQuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType ?? "MultipleChoice",
                    ImagePath = q.ImagePath,
                    ImageAlt = q.ImageAlt,
                    Options = q.Options.Select(o => new HcPortal.Models.EditOptionRow
                    {
                        Id = o.Id, OptionText = o.OptionText, IsCorrect = o.IsCorrect,
                        ImagePath = o.ImagePath, ImageAlt = o.ImageAlt
                    }).ToList(),
                    SelectedOptionIds = selectedIds,
                    CorrectOptionIds = correctIds,
                    IsCurrentCorrect = isCorrect
                });
            }

            var vm = new HcPortal.Models.EditPesertaAnswersViewModel
            {
                Session = session,
                FullName = session.User?.FullName ?? "Unknown",
                NIP = session.User?.NIP ?? "—",
                UpdatedAt = session.UpdatedAt ?? session.CreatedAt,
                Questions = rows
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEditAnswers(HcPortal.Models.EditAnswersSubmission form)
        {
            var session = await _context.AssessmentSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == form.SessionId);
            if (session == null) { TempData["Error"] = "Sesi tidak ditemukan."; return RedirectToAction("ManageAssessment"); }

            var redirectBack = RedirectToAction("AssessmentMonitoringDetail", new {
                title = session.Title, category = session.Category, scheduleDate = session.Schedule
            });

            if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
            {
                TempData["Error"] = "Sesi tidak dapat di-edit.";
                return redirectBack;
            }

            var currentUpdatedAt = session.UpdatedAt ?? session.CreatedAt;
            if (Math.Abs((currentUpdatedAt - form.UpdatedAt).TotalSeconds) > 1)
            {
                TempData["Error"] = "Sesi sudah diubah admin lain. Refresh halaman.";
                return redirectBack;
            }

            var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var actor = await _context.Users.FirstOrDefaultAsync(u => u.Id == actorId);
            var actorRole = User.IsInRole("Admin") ? "Admin" : "HC";
            var actorName = $"{actor?.NIP} - {actor?.FullName}";

            var qIds = form.Answers.Keys.ToList();
            var questions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => qIds.Contains(q.Id))
                .ToListAsync();
            var existingResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == session.Id && qIds.Contains(r.PackageQuestionId))
                .ToListAsync();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int editCount = 0;
                foreach (var (qId, newOptionIds) in form.Answers)
                {
                    var q = questions.FirstOrDefault(x => x.Id == qId);
                    if (q == null) continue;
                    if ((q.QuestionType ?? "MultipleChoice") == "Essay") continue;

                    var validOptionIds = q.Options.Select(o => o.Id).ToHashSet();
                    var sanitizedNewSet = newOptionIds.Where(id => validOptionIds.Contains(id)).ToHashSet();

                    var oldResponses = existingResponses
                        .Where(r => r.PackageQuestionId == qId && r.PackageOptionId.HasValue).ToList();
                    var oldOptionIdsSet = oldResponses.Select(r => r.PackageOptionId!.Value).ToHashSet();

                    // Skip if answer unchanged (no edit log, no DB write, no reason required)
                    if (oldOptionIdsSet.SetEquals(sanitizedNewSet)) continue;

                    // CHANGED — require reason
                    if (!form.Reasons.TryGetValue(qId, out var reason) || string.IsNullOrWhiteSpace(reason.Code))
                    {
                        await tx.RollbackAsync();
                        TempData["Error"] = $"Alasan wajib untuk soal {qId} (jawaban berubah).";
                        return redirectBack;
                    }
                    if (!new[] { "SoalSalah", "KunciSalah", "BugSistem", "PermintaanPeserta", "Lainnya" }.Contains(reason.Code))
                    {
                        await tx.RollbackAsync();
                        TempData["Error"] = "ReasonCode tidak valid.";
                        return redirectBack;
                    }
                    if (reason.Code == "Lainnya" && string.IsNullOrWhiteSpace(reason.Text))
                    {
                        await tx.RollbackAsync();
                        TempData["Error"] = "ReasonText wajib kalau ReasonCode == Lainnya.";
                        return redirectBack;
                    }

                    var sanitizedNew = sanitizedNewSet.ToList();
                    var oldOptionIds = oldOptionIdsSet.ToList();
                    var oldTextSnapshot = string.Join(", ",
                        q.Options.Where(o => oldOptionIds.Contains(o.Id)).Select(o => o.OptionText));

                    _context.PackageUserResponses.RemoveRange(oldResponses);
                    foreach (var newOid in sanitizedNew)
                    {
                        _context.PackageUserResponses.Add(new PackageUserResponse
                        {
                            AssessmentSessionId = session.Id,
                            PackageQuestionId = qId,
                            PackageOptionId = newOid,
                            TextAnswer = null,
                            SubmittedAt = DateTime.UtcNow
                        });
                    }

                    var newTextSnapshot = string.Join(", ",
                        q.Options.Where(o => sanitizedNew.Contains(o.Id)).Select(o => o.OptionText));

                    _context.AssessmentEditLogs.Add(new AssessmentEditLog
                    {
                        AssessmentSessionId = session.Id,
                        PackageQuestionId = qId,
                        QuestionTextSnapshot = q.QuestionText ?? "",
                        OldAnswerJson = System.Text.Json.JsonSerializer.Serialize(oldOptionIds),
                        OldAnswerTextSnapshot = oldTextSnapshot,
                        NewAnswerJson = System.Text.Json.JsonSerializer.Serialize(sanitizedNew),
                        NewAnswerTextSnapshot = newTextSnapshot,
                        OldScore = session.Score,
                        OldIsPassed = session.IsPassed,
                        ActorUserId = actorId,
                        ActorName = actorName,
                        ActorRole = actorRole,
                        ReasonCode = reason.Code,
                        ReasonText = reason.Text,
                        EditedAt = DateTime.UtcNow
                    });
                    editCount++;
                }

                if (editCount == 0)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = "Tidak ada perubahan jawaban untuk disimpan.";
                    return redirectBack;
                }

                await _context.SaveChangesAsync();

                var (newScore, newIsPassed, oldScore, oldIsPassed) = await _gradingService.RegradeAfterEditAsync(session);

                await _context.AssessmentEditLogs
                    .Where(l => l.AssessmentSessionId == session.Id && l.NewScore == null && l.ActorUserId == actorId)
                    .ExecuteUpdateAsync(l => l
                        .SetProperty(x => x.NewScore, newScore)
                        .SetProperty(x => x.NewIsPassed, newIsPassed));

                _context.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = actorId,
                    ActorName = actorName,
                    ActionType = "EditAssessmentAnswer",
                    Description = $"Edit {editCount} jawaban session #{session.Id} ({session.User?.FullName}), score {oldScore?.ToString() ?? "—"} → {newScore}",
                    TargetType = "AssessmentSession",
                    TargetId = session.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                _cache.Remove($"exam-status-{session.Id}");

                var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
                await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("workerAnswerEdited", new
                {
                    sessionId = session.Id,
                    workerName = session.User?.FullName ?? "Unknown",
                    oldScore, newScore, oldIsPassed, newIsPassed,
                    actorName, actorRole
                });

                string flip = (oldIsPassed == true, newIsPassed) switch
                {
                    (true, false) => "Pass→Fail",
                    (false, true) => "Fail→Pass",
                    (true, true) => "Pass→Pass",
                    _ => "Fail→Fail"
                };
                TempData["Success"] = $"Edit {editCount} jawaban berhasil. Score: {oldScore?.ToString() ?? "—"} → {newScore}, {flip}";
                return redirectBack;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Edit jawaban gagal untuk session {SessionId}", session.Id);
                TempData["Error"] = "Terjadi kesalahan saat menyimpan. Coba lagi atau hubungi administrator.";
                return redirectBack;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditHistoryPartial(int sessionId)
        {
            var logs = await _context.AssessmentEditLogs
                .Where(l => l.AssessmentSessionId == sessionId)
                .OrderByDescending(l => l.EditedAt)
                .ToListAsync();
            return PartialView("_EditHistoryPartial", logs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewEditScore(int sessionId,
            HcPortal.Models.EditDraftSubmission form)
        {
            var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return NotFound();
            if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
                return Forbid();

            var overrideAnswers = form.Drafts.ToDictionary(d => d.QuestionId, d => d.Options);

            var (newScore, newIsPassed) = await _gradingService.PreviewScoreAsync(session, overrideAnswers);

            return Json(new
            {
                oldScore = session.Score,
                oldIsPassed = session.IsPassed,
                newScore,
                newIsPassed,
                hasCert = !string.IsNullOrEmpty(session.NomorSertifikat),
                nomorSertifikat = session.NomorSertifikat,
                willGenerateCert = session.GenerateCertificate && session.AssessmentType != "PreTest"
            });
        }

        // --- ASSESSMENT MONITORING DETAIL ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AssessmentMonitoringDetail(string title, string category, DateTime scheduleDate, string? assessmentType = null)
        {
            var query = _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Title == title
                         && a.Category == category
                         && a.Schedule.Date == scheduleDate.Date);

            if (!string.IsNullOrEmpty(assessmentType))
                query = query.Where(a => a.AssessmentType == assessmentType);

            var sessions = await query.ToListAsync();
            ViewBag.AssessmentType = assessmentType;

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
                // Phase 386 PXF-04 D-06 — single pending predicate (byte-identical 4 sites)
                // Whitespace dievaluasi in-memory (IsNullOrWhiteSpace) agar identik dgn .NET: SQL Server
                // LTRIM/RTRIM hanya trim spasi (bukan tab/newline) → parity 4 surface terjamin (D-06a).
                var essayUngradedRows = await _context.PackageUserResponses
                    .Where(r => manualGradingSessionIds.Contains(r.AssessmentSessionId) && r.EssayScore == null)
                    .Join(_context.PackageQuestions.Where(q => q.QuestionType == "Essay"),
                        r => r.PackageQuestionId, q => q.Id, (r, q) => new { r.AssessmentSessionId, r.TextAnswer })
                    .ToListAsync();
                var essayPendingRaw = essayUngradedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.TextAnswer))   // EssayScore==null sudah difilter server-side
                    .GroupBy(x => x.AssessmentSessionId)
                    .Select(g => new { SessionId = g.Key, Count = g.Count() });
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
                // MAM-04: PendingGrading dicek SEBELUM CompletedAt (essay-pending punya CompletedAt terisi).
                string userStatus = DeriveUserStatus(a.Status, a.CompletedAt, a.StartedAt);

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
                    EssayPendingCount = essayPendingCountMap.TryGetValue(a.Id, out var ep) ? ep : 0,
                    // Phase 310 D-02 — append untuk button gate
                    Status          = a.Status ?? "",
                    NomorSertifikat = a.NomorSertifikat
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
                InProgressCount = sessionViewModels.Count(s => s.UserStatus == "InProgress"),
                // MAP-10: assign AbandonedCount + MenungguPenilaianCount agar Total = jumlah 7 kartu (D-03)
                AbandonedCount = sessionViewModels.Count(s => s.UserStatus == "Abandoned"),
                MenungguPenilaianCount = sessionViewModels.Count(s => s.UserStatus == AssessmentConstants.AssessmentStatus.PendingGrading)
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
                        ScoreValue    = q.ScoreValue,
                        ImagePath     = q.ImagePath,
                        ImageAlt      = q.ImageAlt
                    }).ToList();

                    essayGradingMap[sess.Id] = items;
                }
            }
            ViewBag.EssayGradingMap = essayGradingMap;

            return View(model);
        }

        // --- RIWAYAT PERCOBAAN per-worker (v32.4 Phase 406 RTK-08) ---
        // GET lazy-AJAX (mirror EditHistoryPartial :3252): kembalikan PartialView @-encoded berisi
        // accordion per-attempt + tabel per-soal. Arsip di-query by AttemptHistoryId; attempt LIVE
        // saat ini dibangun via RetakeArchiveBuilder.Build(0,...) HANYA bila session.Status=="Completed"
        // (Pitfall 2/5 — anti partial-answer leak in-progress). RBAC identik AssessmentMonitoringDetail
        // (:3290) — T-406-01 IDOR/answer-leak guard. Read-only GET → tanpa [ValidateAntiForgeryToken].
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> RiwayatPercobaan(int sessionId)
        {
            var session = await _context.AssessmentSessions.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return NotFound();

            // Arsip: history by (UserId, Title, Category) anti-konflasi Pre/Post (Pitfall 3),
            // baris arsip by AttemptHistoryId.
            var histories = await _context.AssessmentAttemptHistory
                .Where(h => h.UserId == session.UserId
                         && h.Title == session.Title && h.Category == session.Category)
                .OrderByDescending(h => h.AttemptNumber)
                .ToListAsync();
            var histIds = histories.Select(h => h.Id).ToList();
            var archiveRows = await _context.AssessmentAttemptResponseArchives
                .Where(a => histIds.Contains(a.AttemptHistoryId))
                .ToListAsync();

            // Attempt LIVE saat ini — sumber data persis RetakeService (:128-139), sentinel id=0.
            var currentRows = new List<HcPortal.Models.AssessmentAttemptResponseArchive>();
            if (session.Status == "Completed")
            {
                var assign = await _context.UserPackageAssignments
                    .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
                var qids = assign?.GetShuffledQuestionIds() ?? new List<int>();
                if (qids.Count > 0)
                {
                    var qs = await _context.PackageQuestions.Include(q => q.Options)
                        .Where(q => qids.Contains(q.Id)).ToListAsync();
                    var resp = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == sessionId).ToListAsync();
                    if (qs.Count > 0)
                        currentRows = HcPortal.Helpers.RetakeArchiveBuilder.Build(0, qs, resp);
                }
            }

            ViewBag.WorkerName = session.User?.FullName ?? "";
            var vm = HcPortal.Helpers.RiwayatUnifier.Build(session, histories, archiveRows, currentRows);
            return PartialView("_RiwayatPercobaan", vm);
        }

        // --- ESSAY GRADING PAGE per-worker (Phase 384 UIG-02/03) ---
        // GET action BARU (append sebelah AssessmentMonitoringDetail). Clone single-session essay
        // loader dari builder "Essay grading items per session". Backend POST endpoint TAK diubah.
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EssayGrading(
            int sessionId, string title, string category, DateTime scheduleDate, string? assessmentType = null)
        {
            var session = await _context.AssessmentSessions
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == sessionId);
            if (session == null || !session.HasManualGrading)
            {
                TempData["Error"] = "Sesi penilaian essay tidak ditemukan.";
                return RedirectToAction("AssessmentMonitoringDetail",
                    new { title, category, scheduleDate, assessmentType });
            }

            // CLONE single-session essay loader (dari builder "Essay grading items per session" di AssessmentMonitoringDetail)
            var assignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);
            var items = new List<EssayGradingItemViewModel>();
            if (assignment != null)
            {
                var shuffled = assignment.GetShuffledQuestionIds();
                var essayQs = await _context.PackageQuestions
                    .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay")
                    .ToListAsync();
                var essayRespMap = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == session.Id &&
                           essayQs.Select(q => q.Id).Contains(r.PackageQuestionId))
                    .ToDictionaryAsync(r => r.PackageQuestionId);
                items = essayQs.Select((q, idx) => new EssayGradingItemViewModel
                {
                    QuestionId    = q.Id,
                    DisplayNumber = idx + 1,
                    QuestionText  = q.QuestionText ?? "",
                    Rubrik        = q.Rubrik,
                    TextAnswer    = essayRespMap.TryGetValue(q.Id, out var resp) ? resp.TextAnswer : null,
                    EssayScore    = essayRespMap.TryGetValue(q.Id, out var resp2) ? resp2.EssayScore : null,
                    ScoreValue    = q.ScoreValue,
                    ImagePath     = q.ImagePath,
                    ImageAlt      = q.ImageAlt
                }).ToList();
            }

            // Phase 386 PXF-04 D-06 — single pending predicate (byte-identical 4 sites)
            var essayPendingCount = items.Count(i => !string.IsNullOrWhiteSpace(i.TextAnswer) && i.EssayScore == null);
            var isFinalized = session.Status == AssessmentConstants.AssessmentStatus.Completed
                              && !string.IsNullOrEmpty(session.NomorSertifikat);

            var model = new EssayGradingPageViewModel
            {
                SessionId         = session.Id,
                UserFullName      = session.User?.FullName ?? "(Nama tidak tersedia)",
                UserNIP           = session.User?.NIP ?? "",
                EssayPendingCount = essayPendingCount,
                IsFinalized       = isFinalized,
                CompletedAt       = session.CompletedAt,
                EssayItems        = items,
                Title             = title,
                Category          = category,
                ScheduleDate      = scheduleDate.ToString("yyyy-MM-dd"),
                AssessmentType    = assessmentType
            };
            return View(model);
        }

        // --- SUBMIT ESSAY SCORE (Phase 298-05, D-15/D-16) ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEssayScore(int sessionId, int questionId, int score)
        {
            // 1. STATUS-GUARD (Phase 386 PXF-04 D-08, T-386-AUTHZ HIGH) — penilaian hanya saat PendingGrading.
            //    Tanpa guard ini, upsert akan MEMPERLEBAR F-03: HC bisa membuat+menilai baris pada sesi
            //    Completed → divergen Score/IsPassed yang memberi PDF lisensi resmi. Cermin FinalizeEssayGrading:3591.
            var session = await _context.AssessmentSessions.FindAsync(sessionId);
            if (session == null)
                return Json(new { success = false, message = "Session tidak ditemukan" });
            if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
                return Json(new { success = false, message = "Penilaian hanya bisa dilakukan saat status Menunggu Penilaian." });

            // 2. Load question + validasi skor range (T-298-13) — WAJIB sebelum upsert agar skor invalid tak pernah membuat baris.
            var question = await _context.PackageQuestions.FindAsync(questionId);
            if (question == null)
                return Json(new { success = false, message = "Soal tidak ditemukan" });
            if (score < 0 || score > question.ScoreValue)
                return Json(new { success = false, message = $"Skor harus antara 0 dan {question.ScoreValue}" });

            // 2a. PXF-06 anti-tamper: edit skor essay pasca-finalize SUDAH ditolak oleh status-guard Phase 386 D-08
            //     di atas (`Status != PendingGrading` → reject), sehingga guard `Status == Completed` eksplisit
            //     redundant. Yang ditutup di sini adalah 2 celah hardening tertunda (386-REVIEW WR-01 + WR-02)
            //     yang diperkenalkan oleh upsert D-08 (penciptaan baris baru tanpa validasi tipe/kepemilikan).
            // 2b. WR-01 (386-REVIEW) — questionId WAJIB tipe Essay. Tanpa ini upsert bisa membuat baris EssayScore
            //     pada soal MC/MA (korupsi skor: aggregator menjumlah EssayScore via case Essay).
            if (question.QuestionType != "Essay")
                return Json(new { success = false, message = "Soal ini bukan tipe Essay." });
            // 2c. WR-02 (386-REVIEW) — questionId WAJIB milik sesi ini (cross-session tampering guard). Rantai nav
            //     terverifikasi: PackageQuestion.AssessmentPackage.AssessmentSessionId == sessionId.
            var ownsQuestion = await _context.PackageQuestions
                .AnyAsync(q => q.Id == questionId && q.AssessmentPackage.AssessmentSessionId == sessionId);
            if (!ownsQuestion)
                return Json(new { success = false, message = "Soal bukan milik sesi ini." });

            // 3. UPSERT (Phase 386 PXF-04 D-08) — baris essay kosong tak ada → buat baru (TextAnswer null) lalu skor;
            //    mengganti dead-end "Jawaban tidak ditemukan" agar HC tetap bisa menilai essay yang dikosongkan peserta.
            var response = await _context.PackageUserResponses
                .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
            if (response == null)
            {
                response = new PackageUserResponse
                {
                    AssessmentSessionId = sessionId,
                    PackageQuestionId = questionId,
                    PackageOptionId = null,
                    TextAnswer = null,
                    EssayScore = score
                };
                _context.PackageUserResponses.Add(response);
            }
            else
            {
                response.EssayScore = score;
            }
            await _context.SaveChangesAsync();

            // 5. Cek berapa Essay masih pending
            // Phase 386 PXF-04 D-06 — single pending predicate (byte-identical 4 sites)
            // Whitespace dievaluasi in-memory (IsNullOrWhiteSpace) agar identik dgn .NET: SQL Server
            // LTRIM/RTRIM hanya trim spasi (bukan tab/newline) → parity 4 surface terjamin (D-06a).
            var pendingTextAnswers = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId && r.EssayScore == null)
                .Join(_context.PackageQuestions.Where(q => q.QuestionType == "Essay"),
                    r => r.PackageQuestionId, q => q.Id, (r, q) => r.TextAnswer)
                .ToListAsync();
            var pendingCount = pendingTextAnswers.Count(t => !string.IsNullOrWhiteSpace(t));   // EssayScore==null sudah difilter server-side

            return Json(new { success = true, pendingCount, allGraded = pendingCount == 0 });
        }

        // --- FINALIZE ESSAY GRADING (Phase 298-05, D-17/D-18; Phase 310 D-03/D-04/D-06/D-07 idempotency) ---
        /// <summary>
        /// Action "FinalizeEssayGrading" — finalize Essay assessment session secara idempotent.
        /// Phase 310 D-06/D-07: capture rowsAffected dari ExecuteUpdateAsync, gate audit/cert/notif side-effects
        /// dengan rowsAffected > 0. D-03: friendly no-op response saat Status=Completed (alreadyFinalized:true).
        /// D-04: pesan spesifik per status non-PendingGrading (Open/InProgress/Cancelled).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeEssayGrading(int sessionId)
        {
            // Phase 387 PXF-10 (REVIEW WR-01): eager-load User agar broadcast workerSubmitted
            // mengirim nama peserta asli (bukan "Unknown"). Cermin analog workerAnswerEdited (Include User).
            var session = await _context.AssessmentSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
                return Json(new { success = false, message = "Session tidak ditemukan." });

            // D-03 LOCKED — friendly no-op kalau sudah Completed (alreadyFinalized response)
            if (session.Status == AssessmentConstants.AssessmentStatus.Completed)
            {
                // Phase 310 WR-01 — guard CompletedAt null agar tidak render "pada  WIB" double-space
                var completedAtText = session.CompletedAt.HasValue
                    ? $" pada {session.CompletedAt.Value:dd MMM yyyy HH:mm} WIB"
                    : "";
                return Json(new
                {
                    success = true,
                    alreadyFinalized = true,
                    message = $"Penilaian sudah diselesaikan sebelumnya{completedAtText}",
                    score = session.Score,
                    isPassed = session.IsPassed,
                    nomorSertifikat = session.NomorSertifikat
                });
            }

            // D-04 LOCKED — pesan spesifik per status non-PendingGrading (Bahasa Indonesia, copy literal dari CONTEXT.md D-04)
            if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
            {
                var statusMsg = session.Status switch
                {
                    AssessmentConstants.AssessmentStatus.Open        => "Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian.",
                    AssessmentConstants.AssessmentStatus.InProgress  => "Belum bisa di-finalize. Peserta sedang mengerjakan ujian.",
                    AssessmentConstants.AssessmentStatus.Cancelled   => "Tidak bisa di-finalize. Session sudah dibatalkan.",
                    _                                                => $"Tidak bisa di-finalize. Status saat ini: {session.Status}."
                };
                return Json(new { success = false, message = statusMsg });
            }

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

            // Phase 386 PXF-04 D-06 — single pending predicate (byte-identical 4 sites)
            if (essayResponses.Any(r => !string.IsNullOrWhiteSpace(r.TextAnswer) && r.EssayScore == null))
                return Json(new { success = false, message = "Masih ada Essay yang belum dinilai" });

            // 2. Recalculate total score (MC + MA auto + Essay manual)
            // Phase 376 D-06: derivasi question-set ROBUST. shuffledIds biasanya terisi (fixed v27.0 Phase 373),
            // tapi bila kosong (root-cause historis H1 — ShuffledQuestionIds malformed/empty pra-v27) → fallback
            // derive dari PackageUserResponses session supaya agregasi tidak collapse ke maxScore=0 → Score=0.
            List<PackageQuestion> allQuestions;
            if (shuffledIds.Count > 0)
            {
                allQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => shuffledIds.Contains(q.Id))
                    .ToListAsync();
            }
            else
            {
                _logger.LogWarning("FinalizeEssayGrading: shuffledIds kosong session {SessionId} — fallback derive question-set dari PackageUserResponses (D-06).", sessionId);
                var respQIds = await _context.PackageUserResponses
                    .Where(r => r.AssessmentSessionId == sessionId)
                    .Select(r => r.PackageQuestionId).Distinct().ToListAsync();
                allQuestions = await _context.PackageQuestions
                    .Include(q => q.Options)
                    .Where(q => respQIds.Contains(q.Id))
                    .ToListAsync();
            }
            var allResponses = await _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId)
                .ToListAsync();

            // Phase 376 D-02/D-04: agregasi via helper murni AssessmentScoreAggregator (kill-drift —
            // sama persis dengan RecomputeEssayScores Plan 03). Formula D-04 di-port verbatim di helper.
            var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
            if (agg.MaxScore == 0)
                _logger.LogWarning("FinalizeEssayGrading: maxScore=0 session {SessionId} — anomali data, Score fallback 0 (D-05).", sessionId);
            int finalPercentage = agg.Percentage;
            bool isPassed = agg.IsPassed;

            // 3. Update session: Completed + final score + IsPassed (T-298-16 replay guard via WHERE clause)
            // D-06 / D-07 — capture rowsAffected, gate semua side-effect (audit + cert + notif)
            var rowsAffected = await _context.AssessmentSessions
                .Where(s => s.Id == sessionId && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Score, finalPercentage)
                    .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
                    .SetProperty(r => r.IsPassed, isPassed)
                    .SetProperty(r => r.CompletedAt, DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                // Race lost — thread lain sudah finalize duluan. Read current state, return friendly no-op.
                var current = await _context.AssessmentSessions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                _logger.LogInformation(
                    "FinalizeEssayGrading: race condition session {SessionId} — skip side-effects (already finalized).",
                    sessionId);

                // Phase 310 WR-01 — guard CompletedAt null agar tidak render "pada  WIB" double-space
                var raceCompletedAtText = current?.CompletedAt.HasValue == true
                    ? $" pada {current.CompletedAt.Value:dd MMM yyyy HH:mm} WIB"
                    : "";
                return Json(new
                {
                    success = true,
                    alreadyFinalized = true,
                    message = $"Penilaian sudah diselesaikan sebelumnya{raceCompletedAtText}",
                    score = current?.Score,
                    isPassed = current?.IsPassed,
                    nomorSertifikat = current?.NomorSertifikat
                });
            }

            // Phase 324 D-02: TrainingRecord auto-create removed dari FinalizeEssayGrading path.
            // Konsisten dengan GradingService.GradeAndCompleteAsync (D-01).
            // AssessmentSession sole source-of-truth untuk Records page display.

            // 5. Generate sertifikat jika applicable (same pattern as GradingService)
            // Phase 423 CERT-01 (SITE 3): gate kelayakan cert TUNGGAL via CertIssuanceRules.ShouldIssueCertificate
            // (tolak PreTest — sebelumnya gate ini TANPA cek PreTest). Loop seq -> TryAssignNextSeqAsync (CERT-03).
            // PXF-08: surface kegagalan cert ke HC tetap dipertahankan di bawah (certError).
            if (CertIssuanceRules.ShouldIssueCertificate(session))
            {
                var certNow = DateTime.Now;
                bool certSaved = await CertNumberHelper.TryAssignNextSeqAsync(_context, session.Id, certNow);
                if (certSaved)
                {
                    // CERT-02/06: derive ValidUntil dari CompletedAt utk CertificateType kanonik (paritas GradingService SITE 1).
                    var validUntil = CertIssuanceRules.DeriveValidUntil(session.CertificateType, session.CompletedAt);
                    if (validUntil != null)
                    {
                        await _context.AssessmentSessions
                            .Where(s => s.Id == session.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ValidUntil, (DateOnly?)validUntil));
                    }
                }
                else
                {
                    // CERT-03 non-destruktif: sesi tetap final, NomorSertifikat==null -> certError (PXF-08) surface ke HC + stamp UpdatedAt.
                    _logger.LogError("FinalizeEssayGrading: cert gagal terbit SessionId={SessionId} setelah retry — tandai HC (non-destruktif).", session.Id);
                    await _context.AssessmentSessions
                        .Where(s => s.Id == session.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
                }
            }

            // 5b. Audit log (Phase 310 D-07) — gated by rowsAffected > 0 (di-skip otomatis kalau race lost karena early return di atas)
            var currentUser = await _userManager.GetUserAsync(User);
            var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                ? (currentUser?.FullName ?? "Unknown")
                : $"{currentUser.NIP} - {currentUser.FullName}";
            try
            {
                await _auditLog.LogAsync(
                    currentUser?.Id ?? "",
                    actorName,
                    "FinalizeEssayGrading",
                    $"Session {sessionId} ({session.Title}) finalized: score={finalPercentage}%, isPassed={isPassed}",
                    sessionId,
                    "AssessmentSession");
            }
            catch (Exception ex)
            {
                // Audit failure tidak boleh break primary flow (precedent Phase 306 D-10)
                _logger.LogWarning(ex, "FinalizeEssayGrading: audit log failed for session {SessionId}", sessionId);
            }

            // 5c. PCOMP-01 D-05a (defensive): Proton essay lulus → penanda Origin="Exam".
            // Praktis idle (Proton tak ada essay di data sekarang) tapi nutup celah hasEssay early-return
            // di GradingService.GradeAndCompleteAsync (RESEARCH Pitfall 1) yang lewati hook penanda.
            // EnsureAsync idempotent → aman walau Hook A juga terbit.
            if (session.Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue)
            {
                await _protonCompletionService.EnsureAsync(
                    session.UserId, session.ProtonTrackId.Value, currentUser?.Id ?? "",
                    "Exam", $"Essay Proton finalisasi lulus (skor {finalPercentage}%).");
                // §7 titik 4 (Pitfall 2): essay early-return tak lewat hook GradeAndCompleteAsync →
                // flip pending CL-B(b) Menunggu→Siap + notif HC di sini. Idempotent (guard rowsAffected).
                await _protonBypassService.MarkPendingReadyIfAnyAsync(session.Id);
            }

            // 6. Reload session untuk NotifyIfGroupCompleted
            var updatedSession = await _context.AssessmentSessions.FindAsync(sessionId);
            if (updatedSession != null)
                await _workerDataService.NotifyIfGroupCompleted(updatedSession);

            // PXF-10: broadcast completion ke grup monitor agar tab Monitoring update tanpa refresh.
            // session.Schedule scalar DateTime (no Include); session.User nav → null-safe via FindAsync.
            // Fire-and-forget — tidak boleh break primary finalize flow.
            var fbatchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
            await _hubContext.Clients.Group($"monitor-{fbatchKey}").SendAsync("workerSubmitted", new
            {
                sessionId,
                workerName = session.User?.FullName ?? "Unknown",
                score = finalPercentage,
                result = isPassed ? "Pass" : "Fail",
                status = AssessmentConstants.AssessmentStatus.Completed,
                nomorSertifikat = updatedSession?.NomorSertifikat
            });

            // PXF-08: surface kegagalan cert ke HC (layak terbit tapi NomorSertifikat masih kosong).
            // Phase 423 CERT-01: konsisten-kan kondisi ke gate tunggal ShouldIssueCertificate (bukan literal divergen).
            var certError = (CertIssuanceRules.ShouldIssueCertificate(session) && string.IsNullOrEmpty(updatedSession?.NomorSertifikat))
                ? "Nomor sertifikat gagal dibuat, coba lagi." : null;
            return Json(new
            {
                success = true,
                score = finalPercentage,
                isPassed,
                nomorSertifikat = updatedSession?.NomorSertifikat,
                certError
            });
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
            // PCOMP-03 (D-07): refactor inline-create → single-source helper ProtonCompletionService.
            // EnsureAsync share _context scoped yang sama (SaveChanges internal) → idempotent, tidak dobel.
            if (isPassed && session.ProtonTrackId.HasValue)
            {
                await _protonCompletionService.EnsureAsync(
                    session.UserId, session.ProtonTrackId.Value, actorForFix?.Id ?? "",
                    "Interview", $"Interview Tahun 3 lulus. Assessor: {dto.Judges}");
            }

            // Simpan perubahan session (InterviewResultsJson/IsPassed/Status di-set di memory di atas).
            // EnsureAsync sudah flush bila penanda dibuat; SaveChanges ini jaga session tetap tersimpan
            // walau EnsureAsync skip/return false (Pitfall 2 — urutan terjaga).
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

        // --- BACKFILL PENANDA PROTON (PCOMP-05) ---
        // Maintenance admin-only 1x idempotent: terbitkan penanda Origin="Exam" untuk exam Proton
        // Tahun 1/2 LAMA yang lulus + deliverable 100% (A-M10 resolve, D-08 enforce 100%).
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackfillProtonPenanda()
        {
            int created = 0, alreadyExists = 0, notEligible = 0, skipped = 0;
            try
            {
                // 1. Semua exam Proton Tahun 1/2 yang lulus + selesai.
                var exams = await _context.AssessmentSessions
                    .Where(s => s.Category == "Assessment Proton"
                             && s.IsPassed == true
                             && s.ProtonTrackId.HasValue
                             && (s.TahunKe == "Tahun 1" || s.TahunKe == "Tahun 2")
                             && s.CompletedAt != null)
                    .ToListAsync();

                foreach (var exam in exams)
                {
                    // 2. Resolve assignment A-M10 (BUKAN EnsureAsync — Pitfall 3): tanpa filter IsActive
                    //    (bisa inactive & >1), AssignedAt <= exam.CompletedAt (Pitfall 4: pakai exam.CompletedAt).
                    var assignment = await _context.ProtonTrackAssignments
                        .Where(a => a.CoacheeId == exam.UserId
                                 && a.ProtonTrackId == exam.ProtonTrackId!.Value
                                 && a.AssignedAt <= exam.CompletedAt!.Value)
                        .OrderByDescending(a => a.AssignedAt)
                        .FirstOrDefaultAsync();
                    if (assignment == null)
                    {
                        skipped++;
                        _logger.LogInformation("BackfillProtonPenanda: skip exam {Id} — tidak ada assignment match.", exam.Id);
                        continue;
                    }

                    // 3. Idempotent: lewati bila penanda untuk assignment ini sudah ada.
                    if (await _context.ProtonFinalAssessments.AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id))
                    {
                        alreadyExists++;
                        continue;
                    }

                    // 4. ENFORCE deliverable 100% (D-08): count>0 + semua Approved (setara IsEligiblePerUnit assignment-scoped).
                    //    T10/D-13 (by-design): Backfill SENGAJA tanpa year-gate — menambal data historis
                    //    pre-Phase 358 yang lulus exam sungguhan; year-gate baru bermakna setelah penanda
                    //    lengkap. Enforce 100% deliverable Approved tetap berlaku.
                    var statuses = await _context.ProtonDeliverableProgresses
                        .Where(p => p.ProtonTrackAssignmentId == assignment.Id)
                        .Select(p => p.Status)
                        .ToListAsync();
                    if (statuses.Count == 0 || !statuses.All(s => s == "Approved"))
                    {
                        notEligible++;
                        continue;
                    }

                    // 5. Create penanda manual (Origin="Exam", CompletedAt=exam.CompletedAt).
                    _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
                    {
                        CoacheeId = exam.UserId,
                        CreatedById = (await _userManager.GetUserAsync(User))?.Id ?? "",
                        ProtonTrackAssignmentId = assignment.Id,
                        Status = "Completed",
                        CompetencyLevelGranted = 0,
                        Origin = "Exam",
                        Notes = $"Backfill exam {exam.TahunKe} lulus.",
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = exam.CompletedAt
                    });
                    created++;
                }

                if (created > 0)
                    await _context.SaveChangesAsync();

                // 7. Audit (warn-only — kegagalan audit tidak boleh break).
                try
                {
                    var actor = await _userManager.GetUserAsync(User);
                    var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                    await _auditLog.LogAsync(actor?.Id ?? "", actorName, "BackfillProtonPenanda",
                        $"Backfill: {created} dibuat, {alreadyExists} sudah ada, {notEligible} belum 100%, {skipped} tanpa assignment",
                        0, "ProtonFinalAssessment");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "BackfillProtonPenanda: audit log gagal.");
                }

                TempData["Success"] = $"Backfill selesai: {created} penanda dibuat, {alreadyExists} dilewati, {notEligible} belum 100%, {skipped} tanpa assignment.";
            }
            catch (Exception ex)
            {
                // No info-leak (Phase 334 D6): detail ke log, pesan generik ke user.
                _logger.LogError(ex, "BackfillProtonPenanda gagal.");
                TempData["Error"] = "Backfill penanda Proton gagal. Cek log untuk detail.";
            }

            return RedirectToAction("ManageAssessment");
        }

        // --- RECOMPUTE ESSAY SCORES (GRADE-01 — repair baris historis) ---
        // Phase 376: maintenance admin-only idempotent. Repair sesi essay-only yang di-finalize saat bug
        // pra-v27 aktif (Score=0 walau dinilai+finalize — lihat 376-DIAGNOSE.md). Reuse AssessmentScoreAggregator
        // (D-02 kill-drift, math identik forward path) — set Score + IsPassed ONLY (D-03: NO cert/Proton/notif/
        // TrainingRecord retroaktif). Eksekusi di DB Dev/Prod = tanggung jawab IT (CLAUDE.md); developer verifikasi lokal.
        [HttpPost]
        [Authorize(Roles = "Admin")]              // mass-repair → Admin-only (lebih ketat dari "Admin, HC", BulkBackfill precedent)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecomputeEssayScores()
        {
            int repaired = 0, skipped = 0, alreadyOk = 0;
            try
            {
                // Kandidat: Completed + manual grading + Score belum terisi benar (0/null). Predicate 376-DIAGNOSE.md.
                var candidateIds = await _context.AssessmentSessions
                    .Where(s => s.Status == AssessmentConstants.AssessmentStatus.Completed
                             && s.HasManualGrading
                             && (s.Score == null || s.Score == 0))
                    .Select(s => s.Id)
                    .ToListAsync();

                foreach (var candId in candidateIds)
                {
                    var session = await _context.AssessmentSessions.FindAsync(candId);
                    if (session == null) { skipped++; continue; }

                    var packageAssignment = await _context.UserPackageAssignments
                        .FirstOrDefaultAsync(a => a.AssessmentSessionId == candId);
                    if (packageAssignment == null) { skipped++; continue; }

                    var shuffledIds = packageAssignment.GetShuffledQuestionIds();

                    // Derivasi question-set ROBUST (sama persis FinalizeEssayGrading, D-06).
                    List<PackageQuestion> allQuestions;
                    if (shuffledIds.Count > 0)
                    {
                        allQuestions = await _context.PackageQuestions.Include(q => q.Options)
                            .Where(q => shuffledIds.Contains(q.Id)).ToListAsync();
                    }
                    else
                    {
                        var respQIds = await _context.PackageUserResponses
                            .Where(r => r.AssessmentSessionId == candId)
                            .Select(r => r.PackageQuestionId).Distinct().ToListAsync();
                        allQuestions = await _context.PackageQuestions.Include(q => q.Options)
                            .Where(q => respQIds.Contains(q.Id)).ToListAsync();
                    }

                    var allResponses = await _context.PackageUserResponses
                        .Where(r => r.AssessmentSessionId == candId).ToListAsync();

                    // Skip bila ada essay belum dinilai (EssayScore null) — jangan tulis skor parsial.
                    var essayQIds = allQuestions
                        .Where(q => (q.QuestionType ?? "MultipleChoice") == "Essay")
                        .Select(q => q.Id).ToHashSet();
                    if (essayQIds.Count > 0 && allResponses.Any(r => essayQIds.Contains(r.PackageQuestionId) && r.EssayScore == null))
                    {
                        skipped++; continue;
                    }

                    var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
                    if (agg.MaxScore == 0)
                    {
                        _logger.LogWarning("RecomputeEssayScores: maxScore=0 session {SessionId} — skip (anomali data, D-05).", candId);
                        skipped++; continue;
                    }

                    // Idempotent: Score + IsPassed ONLY (D-03). WHERE-guard Score 0/null → run kedua = no-op.
                    var rows = await _context.AssessmentSessions
                        .Where(s => s.Id == candId && (s.Score == null || s.Score == 0))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.Score, agg.Percentage)
                            .SetProperty(r => r.IsPassed, agg.IsPassed));
                    if (rows > 0) repaired++; else alreadyOk++;
                }

                // Audit warn-only (kegagalan audit tidak boleh break).
                try
                {
                    var actor = await _userManager.GetUserAsync(User);
                    var actorName = string.IsNullOrWhiteSpace(actor?.NIP) ? (actor?.FullName ?? "Unknown") : $"{actor.NIP} - {actor.FullName}";
                    await _auditLog.LogAsync(actor?.Id ?? "", actorName, "RecomputeEssayScores",
                        $"Recompute essay-only: {repaired} diperbaiki, {skipped} dilewati, {alreadyOk} sudah benar", 0, "AssessmentSession");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "RecomputeEssayScores: audit log gagal.");
                }

                TempData["Success"] = $"Recompute selesai: {repaired} skor diperbaiki, {skipped} dilewati, {alreadyOk} sudah benar.";
            }
            catch (Exception ex)
            {
                // No info-leak (Phase 334 D6): detail ke log, pesan generik ke user.
                _logger.LogError(ex, "RecomputeEssayScores gagal.");
                TempData["Error"] = "Recompute skor essay gagal. Cek log untuk detail.";
            }

            return RedirectToAction("ManageAssessment");
        }

        // #20 Phase 367: record manual tak punya jawaban untuk di-reset (reset hanya untuk sesi online).
        // Single-source: dipakai guard ResetAssessment di bawah + diuji ResetGuardTests.
        public static bool IsResettable(AssessmentSession assessment) => !assessment.IsManualEntry;

        // --- RESET ASSESSMENT ---
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAssessment(int id)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            // #20 Phase 367: tolak reset assessment manual — gunakan Edit untuk ubah atau Hapus untuk menghapus record.
            if (!IsResettable(assessment))
            {
                TempData["Error"] = "Assessment manual tidak dapat di-reset. Gunakan Edit untuk mengubah, atau Hapus untuk menghapus record.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

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

            // v32.4 RTK-06: delegasi ke RetakeService bersama (claim-atomik DULU → snapshot per-soal
            // SEBELUM delete → archive → reset → audit → SignalR reason). Guard HC (IsResettable/Pre-Post/
            // status) TETAP di controller (di atas). Logika inline archive→delete→reset dipindah ke service.
            var rsUser = await _userManager.GetUserAsync(User);
            var rsActorName = string.IsNullOrWhiteSpace(rsUser?.NIP) ? (rsUser?.FullName ?? "Unknown") : $"{rsUser.NIP} - {rsUser.FullName}";

            var rsResult = await _retakeService.ExecuteAsync(
                sessionId: id,
                actorUserId: rsUser?.Id ?? "",
                actorName: rsActorName,
                actionType: "ResetAssessment",
                reason: "hc_reset");

            if (!rsResult.Success)
            {
                TempData["Error"] = rsResult.Error ?? "Sesi tidak dapat direset.";
                return RedirectToAction("AssessmentMonitoringDetail", new {
                    title = assessment.Title,
                    category = assessment.Category,
                    scheduleDate = assessment.Schedule.Date.ToString("yyyy-MM-dd")
                });
            }

            // HC reset re-arms token verification on next entry (parity dgn worker retake) — must-fix #1.
            // StartExam pakai TempData.Peek (non-consume), jadi token stale WAJIB di-clear oleh caller.
            TempData.Remove($"TokenVerified_{id}");

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

        // --- PHASE 312: GET DELETE IMPACT (AJAX impact preview untuk modal delete) ---
        // D-02 modal step 1 fetch — Authorize Admin/HC sesuai T-312-04 (HC accept disclosure of aggregated counts)
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetDeleteImpact(int id, string type)
        {
            if (string.IsNullOrEmpty(type) || (type != "single" && type != "group" && type != "prepost"))
                return BadRequest(new { error = "Invalid type. Must be 'single', 'group', or 'prepost'." });

            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();

            try
            {
                List<AssessmentSession> sessions;

                if (type == "single")
                {
                    var sess = await _context.AssessmentSessions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == id);
                    if (sess == null) return NotFound(new { error = "Assessment not found." });
                    sessions = new List<AssessmentSession> { sess };
                }
                else if (type == "group")
                {
                    // Load representative + siblings by Title+Category+Schedule.Date (existing pattern dari DeleteAssessmentGroup)
                    var rep = await _context.AssessmentSessions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == id);
                    if (rep == null) return NotFound(new { error = "Group representative not found." });
                    var repScheduleDate = rep.Schedule.Date;
                    sessions = await _context.AssessmentSessions
                        .AsNoTracking()
                        .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == repScheduleDate)
                        .ToListAsync();
                }
                else // prepost
                {
                    sessions = await _context.AssessmentSessions
                        .AsNoTracking()
                        .Where(a => a.LinkedGroupId == id)
                        .ToListAsync();
                    if (sessions.Count == 0) return NotFound(new { error = "PrePost group not found." });
                }

                var sessionIds = sessions.Select(s => s.Id).ToList();
                int responseCount = await _context.PackageUserResponses
                    .AsNoTracking()
                    .CountAsync(r => sessionIds.Contains(r.AssessmentSessionId));
                // NOTE: AssessmentAttemptHistory FK column adalah `SessionId`, bukan `AssessmentSessionId`
                // (Models/AssessmentAttemptHistory.cs:9 — consistent dengan cascade pattern di line 2060, 2167, 2264)
                int attemptCount = await _context.AssessmentAttemptHistory
                    .AsNoTracking()
                    .CountAsync(h => sessionIds.Contains(h.SessionId));
                int packageCount = await _context.AssessmentPackages
                    .AsNoTracking()
                    .CountAsync(p => sessionIds.Contains(p.AssessmentSessionId));
                // Cert proxy: NomorSertifikat populated (Models/AssessmentSession.cs:72 — Phase 192 cert number field)
                int certCount = sessions.Count(s => !string.IsNullOrEmpty(s.NomorSertifikat));

                string statusSummary;
                object? prePostBreakdown = null;
                if (type == "prepost")
                {
                    // Per Q3 RESOLVED — per-session breakdown (field: AssessmentType per Models/AssessmentSession.cs:154)
                    var pre = sessions.FirstOrDefault(s => s.AssessmentType == "PreTest");
                    var post = sessions.FirstOrDefault(s => s.AssessmentType == "PostTest");
                    int preResp = pre != null
                        ? await _context.PackageUserResponses.AsNoTracking().CountAsync(r => r.AssessmentSessionId == pre.Id)
                        : 0;
                    int postResp = post != null
                        ? await _context.PackageUserResponses.AsNoTracking().CountAsync(r => r.AssessmentSessionId == post.Id)
                        : 0;
                    statusSummary = $"PreTest:{pre?.Status ?? "-"},PostTest:{post?.Status ?? "-"}";
                    prePostBreakdown = new
                    {
                        pre = new { status = pre?.Status ?? "-", responseCount = preResp },
                        post = new { status = post?.Status ?? "-", responseCount = postResp }
                    };
                }
                else if (sessions.Count == 1)
                {
                    statusSummary = sessions[0].Status;
                }
                else
                {
                    statusSummary = string.Join(" / ", sessions.GroupBy(s => s.Status).Select(g => $"{g.Count()} {g.Key}"));
                }

                return Json(new
                {
                    status = statusSummary,
                    responseCount,
                    certCount,
                    packageCount,
                    attemptCount,
                    sessionCount = sessions.Count,
                    prePostBreakdown
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetDeleteImpact failed for type={Type} id={Id}", type, id);
                return StatusCode(500, new { error = "Gagal mengambil data dampak penghapusan." });
            }
        }

        // --- EXPORT ASSESSMENT RESULTS ---
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportAssessmentResults(string title, string category, DateTime scheduleDate, int? linkedGroupId = null)
        {
            // Query all sessions in this group (all workers assigned, regardless of completion status)
            // MAM-02: Pre-Post both-half via linkedGroupId (PostTest bisa beda tanggal); fallback Schedule.Date untuk grup standard.
            var sessions = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => linkedGroupId != null
                    ? a.LinkedGroupId == linkedGroupId
                    : (a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate.Date))
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
            var worksheet = workbook.Worksheets.Add("Summary");

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

            // === Per-Peserta Sheets (v17.0 Phase 320) ===
            // REQ EXP-05 — Filter peserta eligible: UI-match logic (Cancelled excluded;
            // Completed via CompletedAt/Score OR explicit Status="Abandoned").
            // Deviation dari plan literal `Status == "Completed" || "Abandoned"` — captures
            // edge-case session dengan Status="Open" + CompletedAt!=null (1 row di DB Dev)
            // yang UI tampilkan sebagai "Completed" tapi raw filter literal skip.
            // D-01 — OrderBy(FullName) ascending konsisten UI ManageAssessment.
            var eligibleSessions = sessions
                .Where(s => s.Status != "Cancelled"
                         && ((s.CompletedAt != null || s.Score != null) || s.Status == "Abandoned"))
                .OrderBy(s => s.User?.FullName ?? "")
                .ToList();

            // Sheet name registry untuk collision guard (case-insensitive).
            // Pre-populate dengan "Summary" supaya peserta tidak collide dengan tab utama.
            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Summary" };

            // Pre-load all per-session data in single query (avoid N+1 — T-320-02-07).
            var eligibleSessionIds = eligibleSessions.Select(s => s.Id).ToList();
            var allResponses = await _context.PackageUserResponses
                .Where(r => eligibleSessionIds.Contains(r.AssessmentSessionId))
                .ToListAsync();
            var allEtScores = await _context.SessionElemenTeknisScores
                .Where(et => eligibleSessionIds.Contains(et.AssessmentSessionId))
                .ToListAsync();

            // Load all questions+options for involved packages.
            var sessionPackageMap = await _context.UserPackageAssignments
                .Where(a => eligibleSessionIds.Contains(a.AssessmentSessionId))
                .Select(a => new { a.AssessmentSessionId, a.AssessmentPackageId })
                .ToListAsync();
            var packageIds = sessionPackageMap.Select(x => x.AssessmentPackageId).Distinct().ToList();
            var allQuestions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => packageIds.Contains(q.AssessmentPackageId))
                .ToListAsync();

            // === Phase 338 CIL-05 (D-03 AMENDED + D-08 ordering): 2 aggregate sheet ADDITIVE ===
            // Inserted AFTER Summary (sheet 1), BEFORE per-peserta sheets — sheet 2+3.
            // Reuse pre-loaded data (allResponses, allEtScores, allQuestions) — NO new DB query.
            // Per-peserta sheets EXISTING preserve (T-338-01 mitigation: tool external tidak break).
            HcPortal.Helpers.ExcelExportHelper.AddDetailPerSoalSheet(workbook, eligibleSessions, allResponses, allQuestions);
            HcPortal.Helpers.ExcelExportHelper.AddElemenTeknisSheet(workbook, eligibleSessions, allEtScores);

            // === Parallel PNG pre-compute (REQ EXP-08 — <30s untuk 50 peserta) ===
            // CPU-bound rendering paralel; cap concurrency = ProcessorCount supaya tidak starvation thread pool.
            // CONTEXT D-08: ConcurrentDictionary upgrade dari plan literal Dictionary+lock (Claude's Discretion).
            var pngCache = new System.Collections.Concurrent.ConcurrentDictionary<int, byte[]>();
            var pngTasks = eligibleSessions
                .Where(s => !s.IsManualEntry)
                .Select(s => new
                {
                    SessionId = s.Id,
                    EtData = allEtScores
                        .Where(et => et.AssessmentSessionId == s.Id)
                        .OrderBy(e => e.ElemenTeknis)
                        .Select(e => (e.ElemenTeknis, e.QuestionCount > 0 ? (double)e.CorrectCount / e.QuestionCount * 100 : 0d))
                        .ToList()
                })
                .Where(x => x.EtData.Count >= 3)
                .ToList();

            await Parallel.ForEachAsync(
                pngTasks,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                (item, ct) =>
                {
                    var png = HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(item.EtData);
                    pngCache[item.SessionId] = png;
                    return ValueTask.CompletedTask;
                });

            foreach (var session in eligibleSessions)
            {
                string sheetName = HcPortal.Helpers.SheetNameSanitizer.Sanitize(
                    session.User?.NIP ?? "NA",
                    session.User?.FullName ?? "Unknown",
                    usedSheetNames);
                var ws = workbook.Worksheets.Add(sheetName);

                // === Header (REQ EXP-03 Variant A + REQ EXP-04 Variant B sama) ===
                ws.Cell(1, 1).Value = $"{session.User?.FullName ?? "Unknown"} (NIP {session.User?.NIP ?? "—"})";
                ws.Range(1, 1, 1, 4).Merge();
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 13;

                ws.Cell(2, 1).Value = "Started At";
                ws.Cell(2, 2).Value = session.StartedAt?.ToString("dd MMM yyyy HH:mm") ?? "—";
                ws.Cell(3, 1).Value = "Completed At";
                ws.Cell(3, 2).Value = session.CompletedAt?.ToString("dd MMM yyyy HH:mm") ?? "—";
                ws.Cell(4, 1).Value = "Durasi Aktual";
                int durasi = session.ElapsedSeconds / 60;
                ws.Cell(4, 2).Value = durasi > 0 ? $"{durasi} menit" : "—";
                ws.Cell(5, 1).Value = "Tipe Assessment";
                ws.Cell(5, 2).Value = session.AssessmentType ?? "—";

                ws.Range(2, 1, 5, 1).Style.Font.Bold = true;

                int currentRow = 7;

                if (session.IsManualEntry)
                {
                    // Variant B: Manual Entry — body diisi di Task 10
                    currentRow = WriteManualEntrySection(ws, session, currentRow);
                    continue;
                }

                // === Variant A: Online — Section Analisis Elemen Teknis (REQ EXP-03) ===
                var sessionEt = allEtScores.Where(et => et.AssessmentSessionId == session.Id).ToList();
                if (sessionEt.Any())
                {
                    ws.Cell(currentRow, 1).Value = "Analisis Elemen Teknis";
                    ws.Cell(currentRow, 1).Style.Font.Bold = true;
                    ws.Range(currentRow, 1, currentRow, 4).Merge();
                    currentRow++;

                    // Table header
                    ws.Cell(currentRow, 1).Value = "Elemen Teknis";
                    ws.Cell(currentRow, 2).Value = "Benar";
                    ws.Cell(currentRow, 3).Value = "Total";
                    ws.Cell(currentRow, 4).Value = "Persentase";
                    ws.Range(currentRow, 1, currentRow, 4).Style.Font.Bold = true;
                    ws.Range(currentRow, 1, currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    currentRow++;

                    foreach (var et in sessionEt.OrderBy(e => e.ElemenTeknis))
                    {
                        ws.Cell(currentRow, 1).Value = et.ElemenTeknis;
                        ws.Cell(currentRow, 2).Value = et.CorrectCount;
                        ws.Cell(currentRow, 3).Value = et.QuestionCount;
                        double pct = et.QuestionCount > 0 ? (double)et.CorrectCount / et.QuestionCount * 100 : 0;
                        ws.Cell(currentRow, 4).Value = $"{pct:F1}%";
                        currentRow++;
                    }
                    currentRow++; // blank separator
                }
                // Skip section kalau sessionEt kosong (Abandoned tanpa ET, atau Essay-only)

                // === Spider Chart PNG embed via cache lookup (REQ EXP-08, refactored Plan 03 Task 11) ===
                if (pngCache.TryGetValue(session.Id, out var png) && png.Length > 0)
                {
                    using var ms = new MemoryStream(png);
                    var pic = ws.AddPicture(ms, $"spider-{session.Id}")
                        .MoveTo(ws.Cell(currentRow, 1))
                        .WithSize(400, 400);
                    currentRow += 22; // approx rows occupied by 400px image
                }

                // === Detail Jawaban (REQ EXP-03 — MC/MA per soal, Essay skip, "Tidak dijawab" untuk Abandoned) ===
                var sessionPackage = sessionPackageMap.FirstOrDefault(x => x.AssessmentSessionId == session.Id);
                if (sessionPackage != null)
                {
                    var sessionQuestions = allQuestions
                        .Where(q => q.AssessmentPackageId == sessionPackage.AssessmentPackageId)
                        .OrderBy(q => q.Id)
                        .ToList();
                    var sessionResp = allResponses.Where(r => r.AssessmentSessionId == session.Id).ToList();

                    ws.Cell(currentRow, 1).Value = "Detail Jawaban";
                    ws.Cell(currentRow, 1).Style.Font.Bold = true;
                    ws.Range(currentRow, 1, currentRow, 6).Merge();
                    currentRow++;

                    // Table header
                    ws.Cell(currentRow, 1).Value = "No";
                    ws.Cell(currentRow, 2).Value = "Soal";
                    ws.Cell(currentRow, 3).Value = "Tipe";
                    ws.Cell(currentRow, 4).Value = "Jawaban Peserta";
                    ws.Cell(currentRow, 5).Value = "Jawaban Benar";
                    ws.Cell(currentRow, 6).Value = "Status";
                    ws.Range(currentRow, 1, currentRow, 6).Style.Font.Bold = true;
                    ws.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    currentRow++;

                    int no = 1;
                    foreach (var q in sessionQuestions)
                    {
                        string tipe = q.QuestionType ?? "MultipleChoice";

                        if (tipe == "Essay")
                        {
                            // PXF-09: tampilkan jawaban teks peserta + skor essay (bukan placeholder "—").
                            var essayResp = sessionResp.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                            ws.Cell(currentRow, 1).Value = no++;
                            ws.Cell(currentRow, 2).Value = q.QuestionText;
                            ws.Cell(currentRow, 3).Value = "Essay";
                            ws.Cell(currentRow, 4).Value = string.IsNullOrWhiteSpace(essayResp?.TextAnswer) ? "Tidak dijawab" : essayResp.TextAnswer;
                            ws.Cell(currentRow, 5).Value = "—"; // essay: tidak ada "jawaban benar" deterministik
                            ws.Cell(currentRow, 6).Value = essayResp?.EssayScore.HasValue == true
                                ? $"Skor: {essayResp.EssayScore}/{q.ScoreValue}"
                                : "Belum dinilai";
                            currentRow++;
                            continue;
                        }

                        var responses = sessionResp.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue).ToList();
                        string jawabanText;
                        bool correct;

                        if (!responses.Any())
                        {
                            // Soal tanpa response (Abandoned skip soal) — REQ EXP-03
                            jawabanText = "Tidak dijawab";
                            correct = false;
                        }
                        else if (tipe == "MultipleChoice")
                        {
                            var optId = responses.First().PackageOptionId!.Value;
                            var opt = q.Options.FirstOrDefault(o => o.Id == optId);
                            jawabanText = opt?.OptionText ?? "—";
                            correct = opt?.IsCorrect == true;
                        }
                        else // MultipleAnswer
                        {
                            var selectedIds = responses.Select(r => r.PackageOptionId!.Value).ToHashSet();
                            jawabanText = string.Join(", ",
                                q.Options.Where(o => selectedIds.Contains(o.Id)).Select(o => o.OptionText));
                            var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                            correct = selectedIds.SetEquals(correctIds);
                        }

                        string correctText = string.Join(", ", q.Options.Where(o => o.IsCorrect).Select(o => o.OptionText));

                        ws.Cell(currentRow, 1).Value = no++;
                        ws.Cell(currentRow, 2).Value = q.QuestionText;
                        ws.Cell(currentRow, 3).Value = tipe == "MultipleChoice" ? "SA" : "MA";
                        ws.Cell(currentRow, 4).Value = jawabanText;
                        ws.Cell(currentRow, 5).Value = correctText;
                        ws.Cell(currentRow, 6).Value = correct ? "✓" : "✗";
                        currentRow++;
                    }
                }
            }

            // Sanitize title for filename: replace non-alphanumeric with _
            var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
            var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Summary.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // =====================================================================
        // Phase 338 CIL-06 (D-05): Bulk export per-peserta PDF dalam ZIP.
        // Reuse SpiderChartRenderer (Phase 320 SkiaSharp) untuk radar chart PNG.
        // QuestPDF 2026.2.2 generate multi-page PDF per peserta.
        // T-338-02 DoS mitigation: max 50 peserta per batch.
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> BulkExportPdf(string title, string category, DateTime scheduleDate, int? linkedGroupId = null)
        {
            // MAM-02: Pre-Post both-half via linkedGroupId (PostTest bisa beda tanggal); fallback Schedule.Date untuk grup standard.
            var sessions = await _context.AssessmentSessions
                .Include(s => s.User)
                .Where(s => linkedGroupId != null
                    ? s.LinkedGroupId == linkedGroupId
                    : (s.Title == title && s.Category == category && s.Schedule.Date == scheduleDate.Date))
                .ToListAsync();

            var eligibleSessions = sessions
                .Where(s => s.Status != "Cancelled" && (s.CompletedAt != null || s.Score != null))
                .OrderBy(s => s.User != null ? s.User.FullName : "")
                .ToList();

            if (eligibleSessions.Count == 0)
                return NotFound("Tidak ada peserta untuk export PDF.");

            // T-338-02: DoS guard
            if (eligibleSessions.Count > 50)
                return BadRequest($"Max 50 peserta per batch. Ditemukan {eligibleSessions.Count}. Filter lebih spesifik via title/category/scheduleDate.");

            var sessionIds = eligibleSessions.Select(s => s.Id).ToList();
            var allResponses = await _context.PackageUserResponses
                .Where(r => sessionIds.Contains(r.AssessmentSessionId)).ToListAsync();
            var allEtScores = await _context.SessionElemenTeknisScores
                .Where(et => sessionIds.Contains(et.AssessmentSessionId)).ToListAsync();
            var sessionPackageMap = await _context.UserPackageAssignments
                .Where(a => sessionIds.Contains(a.AssessmentSessionId))
                .Select(a => new { a.AssessmentSessionId, a.AssessmentPackageId })
                .ToListAsync();
            var packageIds = sessionPackageMap.Select(x => x.AssessmentPackageId).Distinct().ToList();
            var allQuestions = await _context.PackageQuestions
                .Include(q => q.Options)
                .Where(q => packageIds.Contains(q.AssessmentPackageId))
                .ToListAsync();

            var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
            var fileName = $"{safeTitle}_{category}_{scheduleDate:yyyyMMdd}_Bundle.zip";

            byte[] zipBytes;
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var session in eligibleSessions)
                    {
                        var pdfBytes = GeneratePerPesertaPdf(session, allResponses, allEtScores, allQuestions, sessionPackageMap, title);
                        var nameSlug = System.Text.RegularExpressions.Regex.Replace(session.User?.FullName ?? session.UserId, @"[^\w]", "_");
                        var nip = session.User?.NIP ?? "noNIP";
                        var entryName = $"{nip}_{nameSlug}_{safeTitle}.pdf";
                        var entry = zipArchive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                    }
                }
                zipBytes = memoryStream.ToArray();
            }

            return File(zipBytes, "application/zip", fileName);
        }

        private byte[] GeneratePerPesertaPdf(
            AssessmentSession session,
            List<PackageUserResponse> allResponses,
            List<SessionElemenTeknisScore> allEtScores,
            List<PackageQuestion> allQuestions,
            IEnumerable<dynamic> sessionPackageMap,
            string title)
        {
            var sessionResponses = allResponses.Where(r => r.AssessmentSessionId == session.Id).ToList();
            var sessionEt = allEtScores
                .Where(et => et.AssessmentSessionId == session.Id)
                .OrderBy(et => et.ElemenTeknis)
                .ToList();

            // Spider chart PNG via Phase 320 SpiderChartRenderer (OQ-338-1 reuse)
            byte[]? spiderPng = null;
            if (sessionEt.Count >= 3)
            {
                var etData = sessionEt
                    .Select(e => (e.ElemenTeknis, e.QuestionCount > 0 ? (double)e.CorrectCount / e.QuestionCount * 100 : 0d))
                    .ToList<(string label, double percentage)>();
                spiderPng = HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(etData);
            }

            // Resolve session questions via package mapping
            var sessionPkgIds = sessionPackageMap
                .Where(x => (int)x.AssessmentSessionId == session.Id)
                .Select(x => (int)x.AssessmentPackageId)
                .ToList();
            var sessionQuestions = allQuestions
                .Where(q => sessionPkgIds.Contains(q.AssessmentPackageId))
                .OrderBy(q => q.Order).ThenBy(q => q.Id)
                .ToList();

            return QuestPDF.Fluent.Document.Create(document =>
            {
                // Page 1: Cover + Spider Chart
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(t => t.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Laporan Hasil Assessment").Bold().FontSize(18).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                        col.Item().Text(title).FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                        col.Item().LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(10);

                        // Peserta info card
                        col.Item().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Informasi Peserta").Bold().FontSize(13);
                            c.Item().Text(t => { t.Span("Nama: ").Bold(); t.Span(session.User?.FullName ?? "Unknown"); });
                            c.Item().Text(t => { t.Span("NIP: ").Bold(); t.Span(session.User?.NIP ?? "—"); });
                            c.Item().Text(t => { t.Span("Tanggal: ").Bold(); t.Span((session.CompletedAt ?? session.Schedule).ToString("dd MMM yyyy")); });
                            // Phase 345 CMP06R-03: 3-way status (null -> Menunggu Penilaian + Orange.Darken2 amber netral)
                            var statusText = session.IsPassed == true ? "Lulus"
                                : session.IsPassed == false ? "Tidak Lulus"
                                : AssessmentConstants.AssessmentStatus.PendingGrading;
                            var statusColor = session.IsPassed == true ? QuestPDF.Helpers.Colors.Green.Darken2
                                : session.IsPassed == false ? QuestPDF.Helpers.Colors.Red.Darken2
                                : QuestPDF.Helpers.Colors.Orange.Darken2;
                            c.Item().Text(t => { t.Span("Skor: ").Bold(); t.Span(session.Score?.ToString() ?? "—").Bold().FontColor(statusColor); });
                            c.Item().Text(t => { t.Span("Status: ").Bold(); t.Span(statusText).Bold().FontColor(statusColor); });
                        });

                        // Spider chart (kalau tersedia)
                        if (spiderPng != null && spiderPng.Length > 0)
                        {
                            col.Item().PaddingTop(10).Text("Distribusi Skor per Elemen Teknis").Bold().FontSize(13);
                            col.Item().AlignCenter().Width(350).Image(spiderPng);
                        }
                        else if (sessionEt.Any())
                        {
                            col.Item().PaddingTop(10).Text("Elemen Teknis (radar membutuhkan ≥3 elemen)").Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            foreach (var et in sessionEt)
                            {
                                var pct = et.QuestionCount > 0 ? (double)et.CorrectCount / et.QuestionCount * 100 : 0;
                                col.Item().Text($"• {et.ElemenTeknis}: {Math.Round(pct, 1)}% ({et.CorrectCount}/{et.QuestionCount})");
                            }
                        }
                    });

                    page.Footer().AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
                });

                // Page 2+: Detail Jawaban per Soal
                if (sessionQuestions.Count > 0)
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.DefaultTextStyle(t => t.FontSize(10));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("Detail Jawaban per Soal").Bold().FontSize(14).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                            col.Item().Text($"{session.User?.FullName ?? "Unknown"} — {session.User?.NIP ?? "—"}").FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            col.Item().LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        });

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Spacing(8);
                            int qNum = 1;
                            foreach (var q in sessionQuestions)
                            {
                                // Phase 386 PXF-05 (F-17 D-09/D-10) — label + answer cell via shared display helpers
                                // untuk SEMUA tipe soal. MA kini di-label all-or-nothing (SetEquals via IsQuestionCorrect)
                                // dan kolom "Jawaban" me-list SEMUA opsi terpilih (BuildAnswerCell join ", "). MC tetap
                                // byte-identik (single OptionText / IsCorrect). Essay tetap pakai IsQuestionCorrect (>0).
                                // Compute (scoring engine) TIDAK disentuh — display-path saja (D-11).
                                var responsesForQ = sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList();
                                bool? correct = AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ);   // D-09 — ALL types
                                string jawaban = AssessmentScoreAggregator.BuildAnswerCell(q, responsesForQ);    // D-10 — MA joins all selected

                                var statusColor = correct == true ? QuestPDF.Helpers.Colors.Green.Darken1
                                                : correct == false ? QuestPDF.Helpers.Colors.Red.Darken1
                                                : QuestPDF.Helpers.Colors.Grey.Darken1;
                                var statusText = correct == true ? "✓ Benar" : (correct == false ? "✗ Salah" : "— Pending");

                                col.Item().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(8).Column(c =>
                                {
                                    c.Item().Text(t =>
                                    {
                                        t.Span($"Soal {qNum}: ").Bold();
                                        t.Span(statusText).FontColor(statusColor).Bold();
                                    });
                                    c.Item().PaddingTop(3).Text(q.QuestionText).FontSize(10);
                                    c.Item().PaddingTop(5).Text(t => { t.Span("Jawaban: ").Bold(); t.Span(jawaban); });
                                });
                                qNum++;
                            }
                        });

                        page.Footer().AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
                    });
                }
            }).GeneratePdf();
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
            // REC-07 fold-in (Phase 346 UAT finding T-346-UAT-01): include sesi PendingGrading MURNI
            // (Status="Menunggu Penilaian") supaya muncul di riwayat berlabel "Menunggu Penilaian"
            // (konsisten GetUnifiedRecords/GetAllWorkersHistory). VM/stats sudah exclude-pending (Phase 345).
            var assessments = await _context.AssessmentSessions
                .Where(a => a.UserId == userId && (a.Status == "Completed" || a.Status == AssessmentConstants.AssessmentStatus.PendingGrading))
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
                    Score = a.Score ?? 0, // IN-02: sesi pending organik bisa punya interim score; averageScore exclude pending via ComputeHistoryStats (D-07), jadi ?? 0 di sini tak menyeret rata-rata
                    PassPercentage = a.PassPercentage,
                    IsPassed = a.IsPassed, // Phase 345 CMP06R-02: drop ?? false (AssessmentReportItem.IsPassed = bool?, preserve pending)
                    CompletedAt = a.CompletedAt
                })
                .ToListAsync();

            // Phase 345 CMP06R-02: stats exclude-pending via helper (D-04 passRate graded denom, D-07 avgScore exclude)
            var (totalAssessments, gradedCount, pendingCount, passedCount, passRate, averageScore) = ComputeHistoryStats(assessments);

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
                GradedCount = gradedCount,
                PendingCount = pendingCount,
                Assessments = assessments
            };

            return View(viewModel);
        }

        // Phase 345 CMP06R-02: stats exclude-pending (D-04 passRate graded denom, D-07 avgScore exclude pending).
        // Static + public untuk unit test (HcPortal.Tests) tanpa instantiate controller (10-dep ctor).
        public static (int total, int graded, int pending, int passed, double passRate, double averageScore)
            ComputeHistoryStats(List<AssessmentReportItem> items)
        {
            var total = items.Count;
            var graded = items.Count(a => a.IsPassed != null);
            var pending = items.Count(a => a.IsPassed == null);
            var passed = items.Count(a => a.IsPassed == true);
            var passRate = graded > 0 ? passed * 100.0 / graded : 0;
            var gradedItems = items.Where(a => a.IsPassed != null).ToList();
            var averageScore = gradedItems.Count > 0 ? gradedItems.Average(a => (double)a.Score) : 0;
            return (total, graded, pending, passed, passRate, averageScore);
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

            // WSE-04 (D-01/D-09): type-aware sibling isolation via shared helper — paritas dgn StartExam.
            // siblingSessionIds kini type-filtered → packages query (di bawah) ikut type-aware otomatis.
            var siblingSessionIds = await _context.AssessmentSessions
                .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(
                    assessment.Title, assessment.Category, assessment.Schedule.Date, assessment.AssessmentType))
                .Select(s => s.Id)
                .ToListAsync();

            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
                .OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)
                .ToListAsync();

            if (!packages.Any())
                return Json(new { success = false, message = "Assessment ini tidak menggunakan mode paket." });

            var currentAssignment = await _context.UserPackageAssignments
                .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);

            if (currentAssignment != null)
                _context.UserPackageAssignments.Remove(currentAssignment);

            var rng = Random.Shared;
            // Phase 373: rebuild via ShuffleEngine respecting BOTH flags. Worker index from the SAME
            // sibling set/order as StartExam (Title+Category+Schedule.Date, sorted — SQL has no guaranteed
            // order), so OFF≥2 keeps each worker on a stable package. ON = canonical core algorithm.
            var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();
            int workerIndex = sortedSiblingIds.IndexOf(sessionId);
            var shuffledIds = ShuffleEngine.BuildQuestionAssignment(packages, assessment.ShuffleQuestions, workerIndex, rng);
            var assignedQuestions = packages.SelectMany(p => p.Questions).Where(q => shuffledIds.Contains(q.Id));
            var optDict = ShuffleEngine.BuildOptionShuffle(assignedQuestions, assessment.ShuffleOptions, rng);
            var sentinelPackage = packages.First();

            var newAssignment = new UserPackageAssignment
            {
                AssessmentSessionId = sessionId,
                AssessmentPackageId = sentinelPackage.Id,
                UserId = assessment.UserId,
                ShuffledQuestionIds = System.Text.Json.JsonSerializer.Serialize(shuffledIds),
                ShuffledOptionIdsPerQuestion = System.Text.Json.JsonSerializer.Serialize(optDict)   // Phase 373: fix bug (was hard-coded "{}")
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
            // WSE-04 (D-08/D-09): determinisme + isolasi type-aware. workerIndex tiap session dihitung
            // terhadap sibling-set TYPE-nya sendiri (Pre↔Pre, Post↔Post, non-PrePost satu grup) — IDENTIK
            // dengan StartExam yang kini type-filtered (Phase 373 parity). Group sekali in-memory, no extra query.
            static string SiblingKey(string? t) => (t == "PreTest" || t == "PostTest") ? t : "__NONPREPOST__";
            var sortedByKey = sessions
                .GroupBy(s => SiblingKey(s.AssessmentType))
                .ToDictionary(g => g.Key, g => g.Select(s => s.Id).OrderBy(x => x).ToList());
            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
                .OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)
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
                // Phase 373 + WSE-04: rebuild via ShuffleEngine respecting BOTH flags (per-session). workerIndex
                // DAN packages dihitung type-aware (Pre/Post tak campur) — IDENTIK dengan StartExam.
                var siblingKey = SiblingKey(session.AssessmentType);
                var typeSiblingIds = sortedByKey[siblingKey];
                int workerIndex = typeSiblingIds.IndexOf(session.Id);
                var sessionPackages = packages
                    .Where(p => typeSiblingIds.Contains(p.AssessmentSessionId))
                    .OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)
                    .ToList();
                if (!sessionPackages.Any())
                {
                    results.Add(new { name = userName, status = "Dilewati — belum ada paket untuk tipe ujian ini" });
                    continue;
                }
                var sessionShuffledIds = ShuffleEngine.BuildQuestionAssignment(sessionPackages, session.ShuffleQuestions, workerIndex, rng);
                var assignedQuestions = sessionPackages.SelectMany(p => p.Questions).Where(q => sessionShuffledIds.Contains(q.Id));
                var optDict = ShuffleEngine.BuildOptionShuffle(assignedQuestions, session.ShuffleOptions, rng);
                var sentinelPackage = sessionPackages.First();

                if (existingAssignment != null)
                    _context.UserPackageAssignments.Remove(existingAssignment);

                _context.UserPackageAssignments.Add(new UserPackageAssignment
                {
                    AssessmentSessionId = session.Id,
                    AssessmentPackageId = sentinelPackage.Id,
                    UserId = session.UserId,
                    ShuffledQuestionIds = System.Text.Json.JsonSerializer.Serialize(sessionShuffledIds),
                    ShuffledOptionIdsPerQuestion = System.Text.Json.JsonSerializer.Serialize(optDict)   // Phase 373: fix bug (was hard-coded "{}")
                });

                results.Add(new { name = userName, status = $"Reshuffled (cross-package, {sessionPackages.Count} paket)" });
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

        // Phase 374 SHUF-10/11: explicit-save shuffle toggle endpoint (PRG + server lock guard + propagate sibling + audit).
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShuffleSettings(int assessmentId, bool shuffleQuestions, bool shuffleOptions)
        {
            var assessment = await _context.AssessmentSessions
                .FirstOrDefaultAsync(a => a.Id == assessmentId);
            if (assessment == null) return NotFound();

            // PROPAGATION scope (type-AGNOSTIC, by design): shuffle Pre↔Post SENGAJA berbagi setting
            // (cross-type). JANGAN ubah scope ini (Pitfall 4 / RESEARCH A3 — type-aware di sini = regresi
            // propagation shuffle). Dipakai HANYA untuk foreach write di bawah.
            var siblingSessionIds = await _context.AssessmentSessions
                .Where(s => s.Title == assessment.Title &&
                            s.Category == assessment.Category &&
                            s.Schedule.Date == assessment.Schedule.Date)
                .Select(s => s.Id)
                .ToListAsync();

            // SHFX-06/SHUF-ISS-01 (LOCK-DETECTION SAJA): kunci sibling TYPE-AWARE — Pre mulai TIDAK
            // mengunci Post (dan sebaliknya). Predicate kanonik selaras StartExam/Reshuffle. TERPISAH dari
            // propagationSiblingIds di atas (cross-type) supaya over-lock hilang tanpa merusak propagation.
            var lockSiblingIds = await _context.AssessmentSessions
                .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(
                    assessment.Title, assessment.Category, assessment.Schedule.Date, assessment.AssessmentType))
                .Select(s => s.Id)
                .ToListAsync();

            // Defense-in-depth (D-04a / SHUF-11): re-cek lock server-side (type-aware sibling).
            bool anyStarted = await _context.AssessmentSessions
                .AnyAsync(s => lockSiblingIds.Contains(s.Id) && s.StartedAt != null);
            bool anyAssignment = await _context.UserPackageAssignments
                .AnyAsync(a => lockSiblingIds.Contains(a.AssessmentSessionId));

            if (ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment))
            {
                TempData["Error"] = "Pengaturan pengacakan tidak dapat diubah karena sudah ada peserta yang memulai ujian.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            // Propagate ke SEMUA sibling grup cross-type (Pre↔Post share shuffle by design — JANGAN ubah scope).
            var siblings = await _context.AssessmentSessions
                .Where(s => siblingSessionIds.Contains(s.Id))
                .ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var sibling in siblings)
            {
                sibling.ShuffleQuestions = shuffleQuestions;
                sibling.ShuffleOptions   = shuffleOptions;
                sibling.UpdatedAt = now;
            }
            await _context.SaveChangesAsync();

            // Audit try/catch warn-only (pola ReshufflePackage/ReshuffleAll).
            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "UpdateShuffleSettings",
                    $"Set Acak Soal={shuffleQuestions}, Acak Pilihan={shuffleOptions} for assessment '{assessment.Title}' [grup {siblingSessionIds.Count} sesi]",
                    assessmentId,
                    "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for UpdateShuffleSettings (assessmentId={Id})", assessmentId); }

            TempData["Success"] = "Pengaturan pengacakan berhasil disimpan.";
            return RedirectToAction("ManagePackages", new { assessmentId });
        }

        // v32.4 RTK-04: explicit-save retake config (PRG + sibling propagation + audit), mirror UpdateShuffleSettings.
        // Beda dari shuffle: TIDAK ada lock-saat-mulai (retake config bisa diubah kapan saja); hanya guard
        // ShouldHideRetakeToggle (PreTest/Manual tak retakeable). Sibling key identik (Title/Category/Schedule.Date).
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRetakeSettings(int assessmentId, bool allowRetake, int maxAttempts, int retakeCooldownHours)
        {
            var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == assessmentId);
            if (assessment == null) return NotFound();

            // Guard: retake hanya untuk graded (bukan Pre-Test) & bukan manual entry.
            if (HcPortal.Helpers.RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry))
            {
                TempData["Error"] = "Ujian ulang tidak berlaku untuk Pre-Test atau assessment manual.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            // Clamp ke range model (defense-in-depth; [Range] hanya client/model-validation, bypass form → clamp server).
            maxAttempts = Math.Clamp(maxAttempts, 1, 5);
            retakeCooldownHours = Math.Clamp(retakeCooldownHours, 0, 168);

            // SHFX-06/SHUF-ISS-01: sibling key TYPE-AWARE (selaras StartExam/Reshuffle). Retake config
            // bersifat per-type (PreTest sudah ditolak ShouldHideRetakeToggle di atas; Post retake tak
            // berbagi setting dgn Pre by design) — propagate hanya ke sibling same-type, bukan cross-type.
            var siblingSessionIds = await _context.AssessmentSessions
                .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(
                    assessment.Title, assessment.Category, assessment.Schedule.Date, assessment.AssessmentType))
                .Select(s => s.Id).ToListAsync();
            var siblings = await _context.AssessmentSessions.Where(s => siblingSessionIds.Contains(s.Id)).ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var sibling in siblings)
            {
                // MaxAttempts < attempt terpakai = warning non-blocking (tetap simpan — tak hard-lock; D-02 retroaktif).
                sibling.AllowRetake = allowRetake;
                sibling.MaxAttempts = maxAttempts;
                sibling.RetakeCooldownHours = retakeCooldownHours;
                sibling.UpdatedAt = now;
            }
            await _context.SaveChangesAsync();

            // v32.7 RTH-01/D-02: peringatan NON-BLOCKING — cooldown bisa mendorong eligibility lewat batas tutup ujian?
            // Predikat hidup di RetakeRules.CooldownMayExceedWindow (pure, +7h WIB SATU tempat) — controller & test panggil
            // method yang SAMA (kill-drift). Setelan SUDAH tersimpan di atas; warning ko-eksis dgn Success, JANGAN return early.
            if (HcPortal.Helpers.RetakeRules.CooldownMayExceedWindow(DateTime.UtcNow, assessment.ExamWindowCloseDate, retakeCooldownHours))
            {
                TempData["Warning"] = "Masa jeda ujian ulang yang Anda atur bisa melewati batas tutup ujian. Peserta yang gagal mungkin tidak sempat mengulang sebelum ujian ditutup. Pengaturan tetap bisa disimpan.";
            }

            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(hcUser?.Id ?? "", actorNameStr, "UpdateRetakeSettings",
                    $"Set AllowRetake={allowRetake}, MaxAttempts={maxAttempts}, Cooldown={retakeCooldownHours}h for '{assessment.Title}' [grup {siblingSessionIds.Count} sesi]",
                    assessmentId, "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for UpdateRetakeSettings (assessmentId={Id})", assessmentId); }

            // D-07: angka "terpakai" untuk modal pra-simpan TIDAK dihitung di sini — disuplai oleh GET ManagePackages
            //       via RetakeCountingRules.MaxInGroupAsync (helper D-05, wired 421-02). POST = PRG redirect; no recompute.
            TempData["Success"] = "Pengaturan ujian ulang berhasil disimpan.";
            return RedirectToAction("ManagePackages", new { assessmentId });
        }

        // Phase 373: the local Shuffle + cross-package distribution helpers moved to
        // Helpers/ShuffleEngine.cs (single source of truth — the canonical CMPController algorithm).
        // Both reshuffle endpoints now delegate to the shared ShuffleEngine core.

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
                .OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)
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

            // SamePackage lock: jika Post-Test dengan SamePackage=true, lock editing
            ViewBag.IsSamePackageLocked = isPostSession && assessment.SamePackage;

            // === Phase 374: Shuffle toggle state ===
            // SHFX-06/SHUF-ISS-01: lock-detection TYPE-AWARE (Pre mulai → Post TIDAK terkunci). Site GET
            // ini MURNI lock-detection (tak ada propagation), jadi sibling key boleh sepenuhnya type-aware.
            var shufSiblingIds = await _context.AssessmentSessions
                .Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(
                    assessment.Title, assessment.Category, assessment.Schedule.Date, assessment.AssessmentType))
                .Select(s => s.Id)
                .ToListAsync();

            bool shufAnyStarted = await _context.AssessmentSessions
                .AnyAsync(s => shufSiblingIds.Contains(s.Id) && s.StartedAt != null);
            bool shufAnyAssignment = await _context.UserPackageAssignments
                .AnyAsync(a => shufSiblingIds.Contains(a.AssessmentSessionId));

            ViewBag.ShuffleQuestions = assessment.ShuffleQuestions;   // saved state -> render checked
            ViewBag.ShuffleOptions   = assessment.ShuffleOptions;
            ViewBag.IsShuffleLocked = ShuffleToggleRules.IsShuffleLocked(shufAnyStarted, shufAnyAssignment);
            ViewBag.HideShuffleToggle = ShuffleToggleRules.ShouldHideShuffleToggle(
                assessment.Category, assessment.TahunKe, assessment.IsManualEntry);

            // === v32.4: Retake config state (RTK-05 — card di-render fase 406) ===
            ViewBag.AllowRetake = assessment.AllowRetake;
            ViewBag.MaxAttempts = assessment.MaxAttempts;
            ViewBag.RetakeCooldownHours = assessment.RetakeCooldownHours;
            ViewBag.HideRetakeToggle = HcPortal.Helpers.RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry);
            // Warning non-blocking: MaxAttempts < attempt terpakai peserta mana pun di grup (Title/Category).
            // v32.7 RTH-03 (D-05): satu sumber counting — kini SNAPSHOT-AWARE (legacy HC-reset pre-v32.4 excluded);
            // GroupBy-max semantics dipertahankan (MaxInGroupAsync), HANYA filter snapshot-presence yang ditambahkan.
            int retakeMaxArchivedForGroup = await HcPortal.Helpers.RetakeCountingRules.MaxInGroupAsync(
                _context, assessment.Title, assessment.Category);
            ViewBag.RetakeMaxAttemptsUsedInGroup = retakeMaxArchivedForGroup + 1;

            // v32.7 Phase 422 D-05/SHFX-07: mismatch SATU SUMBER via PackageSizeAnalysis.Compute (kill-drift —
            // gantikan compute inline lama DI SINI + re-derive view ManagePackages.cshtml:72-78 yang DIHAPUS).
            var sizeAnalysis = PackageSizeAnalysis.Compute(packages);
            ViewBag.PackagesWithQuestions = sizeAnalysis.PackagesWithQuestions;
            ViewBag.HasSizeMismatch = sizeAnalysis.HasMismatch;
            ViewBag.ReferenceCount = sizeAnalysis.ReferenceCount;
            ViewBag.ShowSizeMismatchWarning = ShuffleToggleRules.ShouldShowSizeMismatchWarning(
                sizeAnalysis.PackagesWithQuestions, assessment.ShuffleQuestions, sizeAnalysis.HasMismatch);
            // D-04: warning K=min truncation ON-path (Acak ON + ukuran paket beda → soal dipangkas ke K=min).
            ViewBag.ShowKMinWarning = ShuffleToggleRules.ShouldShowKMinTruncationWarning(
                sizeAnalysis.PackagesWithQuestions, assessment.ShuffleQuestions, sizeAnalysis.HasMismatch);
            // D-03: warning Acak ON pada Post SamePackage (pengacakan kaburkan komparasi item-level Pre/Post).
            ViewBag.ShowAcakOnSamePackageWarning =
                assessment.AssessmentType == "PostTest" && assessment.SamePackage && assessment.ShuffleQuestions;

            // SHFX-02/D-01: disable toggle SamePackage di view bila ADA peserta sudah-mulai di GRUP (Pre+Post
            // pasangan). IN-03: SATU sumber via AnyStartedInPairAsync — endpoint ToggleSamePackage POST pakai
            // helper YANG SAMA untuk hard-reject (server-authoritative); ini friendly UX layer (tak boleh divergen).
            bool anyStartedInGroup = isPostSession
                ? await AnyStartedInPairAsync(assessment.Id, assessment.LinkedSessionId)
                : false;
            ViewBag.AnyStartedInGroup = anyStartedInGroup;

            // Reminder Pre/Post (opsi Z, SHUF-13): Post page only, saved-state Pre via LinkedSessionId.
            if (isPostSession && assessment.LinkedSessionId.HasValue)
            {
                var preShuffle = await _context.AssessmentSessions
                    .Where(s => s.Id == assessment.LinkedSessionId.Value)
                    .Select(s => (bool?)s.ShuffleQuestions)
                    .FirstOrDefaultAsync();
                ViewBag.PreShuffleQuestions = preShuffle;   // null bila Pre tak ada
            }

            return View();
        }

        /// <summary>
        /// v32.7 Phase 422 IN-03 (kill-drift) — SATU sumber kebenaran cek "ada peserta sudah-mulai di
        /// pasangan Pre+Post". Dipakai DUA tempat: GET ManagePackages (ViewBag.AnyStartedInGroup, UX-disable)
        /// + POST ToggleSamePackage (hard-reject server-authoritative). Keduanya WAJIB selalu sinkron —
        /// UX-disable dan server-reject tak boleh divergen. Mirror pola SessionEditLockRules/ShuffleToggleRules.
        /// "Sudah-mulai" = StartedAt != null || Status InProgress || Status Completed, atas {postId, preId}.
        /// preId null (tak berpasangan) → false (tak ada pasangan untuk dicek).
        /// </summary>
        private async Task<bool> AnyStartedInPairAsync(int postId, int? preId)
        {
            if (!preId.HasValue) return false;
            var pairIds = new[] { postId, preId.Value };
            return await _context.AssessmentSessions
                .AnyAsync(s => pairIds.Contains(s.Id) &&
                               (s.StartedAt != null || s.Status == "InProgress" || s.Status == "Completed"));
        }

        // Phase 423 CERT-05 (D-07) — ada cert AKTIF (ValidUntil null OR >=today) utk (peserta, judul-ternormalisasi)?
        // Renewal (RenewsSessionId terisi) dikecualikan. UNCONDITIONAL — tak boleh dibungkus ConfirmDuplicateTitle.
        private async Task<bool> HasActiveCertForTitleAsync(string userId, string? title, int? excludeSessionId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var norm = AdminBaseController.NormalizeTitleForDup(title);
            var candidates = await _context.AssessmentSessions
                .Where(s => s.UserId == userId
                         && s.NomorSertifikat != null
                         && s.IsPassed == true
                         && s.RenewsSessionId == null
                         && (excludeSessionId == null || s.Id != excludeSessionId)
                         && (s.ValidUntil == null || s.ValidUntil >= today))
                .Select(s => s.Title)
                .ToListAsync();
            return candidates.Any(t => AdminBaseController.NormalizeTitleForDup(t) == norm);
        }

        /// <summary>
        /// Deep-clones all packages+questions+options from Pre-Test session to Post-Test session.
        /// Deletes existing Post packages first.
        /// </summary>
        private async Task SyncPackagesToPost(int preSessionId, int postSessionId)
        {
            // Hapus paket Post yang ada
            var existingPostPkgs = await _context.AssessmentPackages
                .Include(p => p.Questions).ThenInclude(q => q.Options)
                .Where(p => p.AssessmentSessionId == postSessionId)
                .ToListAsync();

            // v32.7 Phase 422 WR-02 (kill-drift): bersihkan UserPackageAssignment Post yang menunjuk paket
            // yang akan DIHAPUS, SEBELUM RemoveRange. UserPackageAssignment.AssessmentPackageId ber-FK
            // OnDelete(Restrict) (ApplicationDbContext.cs:543-546) → menghapus paket Post sementara ada UPA
            // yang merujuknya melempar DbUpdateException. Dipusatkan DI SINI agar SEMUA 7 caller (toggle +
            // CreatePackage/DeletePackage/CreateQuestion/EditQuestion/DeleteQuestion/Import) ter-cover
            // (defense-in-depth: legacy data / jalur out-of-band). Lingkup by AssessmentPackageId (FK Restrict).
            var postPkgIds = existingPostPkgs.Select(p => p.Id).ToList();
            if (postPkgIds.Count > 0)
            {
                var staleUpa = await _context.UserPackageAssignments
                    .Where(a => postPkgIds.Contains(a.AssessmentPackageId))
                    .ToListAsync();
                if (staleUpa.Count > 0)
                    _context.UserPackageAssignments.RemoveRange(staleUpa);
            }

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
                .OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)
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
                        Rubrik = q.Rubrik,
                        MaxCharacters = q.MaxCharacters,
                        ImagePath = q.ImagePath,   // SYN-01: shared-file string copy (Pre→Post), no file op
                        ImageAlt = q.ImageAlt,
                        Options = q.Options.Select(o => new PackageOption
                        {
                            OptionText = o.OptionText,
                            IsCorrect = o.IsCorrect,
                            ImagePath = o.ImagePath, // SYN-01: shared-file string copy
                            ImageAlt = o.ImageAlt
                        }).ToList()
                    };
                    newPkg.Questions.Add(newQ);
                }
                _context.AssessmentPackages.Add(newPkg);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// v32.7 Phase 422 SHFX-01/D-06 — SATU sumber kebenaran auto-sync Pre→Post (kill-drift).
        /// Mengganti 5 blok copy-paste identik (CreatePackage/DeletePackage/CreateQuestion/EditQuestion/
        /// DeleteQuestion) + menutup jalur Import yang BOCOR (SHUF-ISS-03 HIGH). Membungkus
        /// <see cref="SyncPackagesToPost"/> (deep-clone, sudah benar — JANGAN ubah).
        /// No-op aman bila: sesi bukan PreTest, tak punya LinkedSessionId, linkedPost null, atau !SamePackage.
        /// </summary>
        private async Task SyncToLinkedPostIfSamePackageAsync(int preSessionId)
        {
            var pre = await _context.AssessmentSessions.FindAsync(preSessionId);
            if (pre?.AssessmentType == "PreTest" && pre.LinkedSessionId.HasValue)
            {
                var post = await _context.AssessmentSessions.FindAsync(pre.LinkedSessionId.Value);
                if (post != null && post.SamePackage)
                    await SyncPackagesToPost(pre.Id, post.Id);   // existing :5875-5933 deep-clone, sudah benar
            }
        }

        // v32.7 Phase 422 SHFX-02/D-01 (FLOW-07, keputusan bisnis b) — toggle SamePackage editable
        // pasca-create. Mirror UpdateShuffleSettings (RBAC + antiforgery + PRG + guard anyStarted + audit).
        //   ON  → (transaksi WR-01) set SamePackage=true + SaveChanges (SEBELUM helper) →
        //         SyncToLinkedPostIfSamePackageAsync (sync Pre→Post + lock implisit). Stale Post UPA
        //         dibersihkan terpusat di SyncPackagesToPost (WR-02 — bukan lagi inline di sini).
        //   OFF → set SamePackage=false SAJA (lepas lock). PERTAHANKAN paket clone (Pitfall 5: JANGAN sync/delete).
        //   GUARD → tolak (TempData Error non-blocking) bila ADA peserta sudah-mulai di GRUP (Pre+Post pasangan);
        //           belum-mulai = boleh. Server-authoritative (view disable hanya UX layer).
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSamePackage(int assessmentId, bool samePackage)
        {
            var post = await _context.AssessmentSessions.FindAsync(assessmentId);
            if (post == null) return NotFound();

            // Hanya berlaku untuk Post-Test berpasangan (backward-compat: Standard/Pre tak tersentuh).
            if (post.AssessmentType != "PostTest" || !post.LinkedSessionId.HasValue)
            {
                TempData["Error"] = "Pengaturan paket-sama hanya berlaku untuk Post-Test berpasangan.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            // GUARD D-01: grup = pasangan Pre+Post (LinkedSessionId). Tolak bila ADA peserta sudah-mulai.
            // IN-03: SATU sumber via AnyStartedInPairAsync — helper YANG SAMA dipakai GET ViewBag
            // (UX-disable). Server-authoritative; UX-disable & server-reject tak boleh divergen.
            bool anyStarted = await AnyStartedInPairAsync(post.Id, post.LinkedSessionId);
            if (anyStarted)
            {
                TempData["Error"] = "Gagal mengubah pengaturan paket-sama: sudah ada peserta yang memulai ujian di grup ini.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            if (samePackage)
            {
                // ON → sync Pre→Post (overwrite paket Post dgn clone Pre) + lock implisit (SamePackage=true).
                // v32.7 Phase 422 WR-01: bungkus mutasi multi-langkah (set flag + sync paket) dalam transaksi
                // eksplisit (mirror pola Import :6617) agar flag-set + package-sync commit ATOMIK. Kegagalan
                // di tengah me-rollback (bukan meninggalkan SamePackage=true + paket Post belum ter-sync).
                // v32.7 WR-02: cleanup stale Post UPA kini DIPUSATKAN di SyncPackagesToPost, jadi inline
                // UPA-clear lama (Open Q2) DIHAPUS di sini — satu sumber kebenaran, hindari double-work.
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    post.SamePackage = true;
                    post.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();   // SamePackage tersimpan SEBELUM helper (helper baca post.SamePackage==true)

                    await SyncToLinkedPostIfSamePackageAsync(post.LinkedSessionId.Value);   // pre→post deep-clone (+ UPA cleanup terpusat)
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
                TempData["Success"] = "Pengaturan paket-sama diaktifkan. Paket Post-Test telah disinkronkan dari Pre-Test dan dikunci.";
            }
            else
            {
                // OFF → KEEP paket clone (Pitfall 5: lepas lock SAJA, JANGAN sync/delete). Paket Post jadi editable.
                // Single SaveChanges, no destructive multi-step → tak butuh transaksi eksplisit.
                post.SamePackage = false;
                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pengaturan paket-sama dinonaktifkan. Kunci dilepas; paket salinan dipertahankan untuk diedit.";
            }

            // Audit try/catch warn-only (pola UpdateShuffleSettings :5675-5687).
            try
            {
                var hcUser = await _userManager.GetUserAsync(User);
                var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
                await _auditLog.LogAsync(
                    hcUser?.Id ?? "",
                    actorNameStr,
                    "ToggleSamePackage",
                    $"Set SamePackage={samePackage} for Post-Test '{post.Title}' [ID={post.Id}, linkedPre={post.LinkedSessionId.Value}]" +
                        (samePackage ? " (synced Pre→Post + locked)" : " (unlocked, clone kept)"),
                    assessmentId,
                    "AssessmentSession");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ToggleSamePackage (assessmentId={Id})", assessmentId); }

            return RedirectToAction("ManagePackages", new { assessmentId });
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
            await SyncPackagesToPost(preSessionId, postSessionId);

            var preCount = await _context.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == preSessionId);
            TempData["Success"] = $"Berhasil menyalin {preCount} paket soal dari Pre-Test.";
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

            // SHFX-03/D-07: tolak-keras server-side bila Post-Test terkunci (paket-sama). Defense-in-depth
            // terhadap root-cause SHUF-ISS-02 (lock view-only bisa di-bypass via POST langsung).
            if (SessionEditLockRules.IsSessionEditLocked(assessment))
            {
                TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
                return RedirectToAction("ManagePackages", new { assessmentId = assessment.Id });
            }

            // SHFX-05/D-02: MAX(PackageNumber)+1 per session (BUKAN count-based existingCount+1 yang
            // turun setelah hapus paket tengah -> bentrok). Gap nomor dibiarkan; unique index = jaring DB-level.
            var maxNumber = await _context.AssessmentPackages
                .Where(p => p.AssessmentSessionId == assessmentId)
                .Select(p => (int?)p.PackageNumber)
                .MaxAsync();   // null bila belum ada paket

            var pkg = new AssessmentPackage
            {
                AssessmentSessionId = assessmentId,
                PackageName = packageName.Trim(),
                PackageNumber = (maxNumber ?? 0) + 1
            };
            _context.AssessmentPackages.Add(pkg);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Package '{packageName}' created.";

            // SHFX-01/D-06: auto-sync Pre→Post via helper tunggal (kill-drift).
            await SyncToLinkedPostIfSamePackageAsync(assessment.Id);

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

            // SHFX-03/D-07: tolak-keras server-side bila Post-Test terkunci (paket-sama).
            var delLockSession = await _context.AssessmentSessions.FindAsync(assessmentId);
            if (delLockSession != null && SessionEditLockRules.IsSessionEditLocked(delLockSession))
            {
                TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

            // D-11 / SYN-02: path-collect SEBELUM cascade RemoveRange (union semua gambar soal+opsi).
            var imagePathsToDelete = new List<string>();
            foreach (var qImg in pkg.Questions)
            {
                if (!string.IsNullOrEmpty(qImg.ImagePath)) imagePathsToDelete.Add(qImg.ImagePath);
                foreach (var oImg in qImg.Options)
                    if (!string.IsNullOrEmpty(oImg.ImagePath)) imagePathsToDelete.Add(oImg.ImagePath);
            }

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Tidak bisa hapus paket: masih ada data yang berelasi.";
                return RedirectToAction("ManagePackages", new { assessmentId });
            }

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

            // SHFX-01/D-06: auto-sync Pre→Post via helper tunggal (kill-drift).
            await SyncToLinkedPostIfSamePackageAsync(assessmentId);

            // D-11 / D-10 / OQ2: ref-count + File.Delete SETELAH auto-sync (Post mungkin di-rebuild share path).
            await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, _logger, imagePathsToDelete, "DeletePackage image");

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

            // SHFX-03/D-07 + Pitfall 3 (guard-ordering): tolak Import LANGSUNG ke paket Post terkunci
            // di AWAL endpoint. Import ke Pre (lock false) lolos → sync Pre→Post di AKHIR success-path.
            var importLockSession = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (importLockSession != null && SessionEditLockRules.IsSessionEditLocked(importLockSession))
            {
                TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Impor soal harus dilakukan di sesi Pre-Test.";
                return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
            }

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

            // SHFX-01/D-06 (BOCOR SHUF-ISS-03 HIGH ditutup): Import beroperasi pada paket Pre →
            // sisip auto-sync Pre→Post di AKHIR success-path (Pitfall 3: lock-guard di AWAL endpoint,
            // sync di AKHIR). Import langsung ke Post terkunci sudah ditolak guard di awal (SHFX-03).
            await SyncToLinkedPostIfSamePackageAsync(pkg.AssessmentSessionId);

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
            // v32.7 Phase 422 D-07/SHFX-03: friendly-disable UX layer — server SUDAH hard-reject edit soal
            // saat Post-Test terkunci (paket-sama) via guard 5 endpoint (Wave 2). View pakai ini untuk banner
            // + disable tombol; bukan satu-satunya pengaman (server-authoritative).
            ViewBag.IsSamePackageLocked = assessment.AssessmentType == "PostTest" && assessment.SamePackage;
            ViewBag.PreSessionId = assessment.LinkedSessionId;
            return View();
        }

        // Truncate alt text ke MaxLength kolom (255) sebelum assign — hindari DbUpdateException (Pitfall 6).
        // IN-01: TruncateAlt dipindah KE ATAS agar trio attribute [HttpPost]/[Authorize]/[ValidateAntiForgeryToken]
        // mengikat ke CreateQuestion (sebelumnya salah-ikat ke helper static ini, di mana attribute inert →
        // CreateQuestion kehilangan CSRF/role/verb di level method).
        private static string? TruncateAlt(string? s, int max)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));

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
            bool correctA, bool correctB, bool correctC, bool correctD,
            IFormFile? questionImage, string? questionImageAlt,
            IFormFile? optionAImage, IFormFile? optionBImage, IFormFile? optionCImage, IFormFile? optionDImage,
            string? optionAImageAlt, string? optionBImageAlt, string? optionCImageAlt, string? optionDImageAlt)
        {
            // Validate QuestionType whitelist (T-298-01)
            var validTypes = new[] { "MultipleChoice", "MultipleAnswer", "Essay" };
            if (!validTypes.Contains(questionType))
            {
                TempData["Error"] = "Tipe soal tidak valid.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Validate-all image uploads fail-fast (SHARED-3 / D-08). Null file → (true,null).
            foreach (var f in new[] { questionImage, optionAImage, optionBImage, optionCImage, optionDImage })
            {
                var (okImg, errImg) = FileUploadHelper.ValidateImageFile(f);
                if (!okImg)
                {
                    TempData["Error"] = errImg ?? "File harus berupa gambar JPG atau PNG.";
                    return RedirectToAction("ManagePackageQuestions", new { packageId });
                }
            }

            var pkg = await _context.AssessmentPackages
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == packageId);
            if (pkg == null) return NotFound();

            // SHFX-03/D-07: tolak-keras server-side bila Post-Test terkunci (paket-sama).
            var cqLockSession = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
            if (cqLockSession != null && SessionEditLockRules.IsSessionEditLocked(cqLockSession))
            {
                TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Range validation 1-100 (D-12, D-13) - replaces force-override removed per D-14 (Phase 306)
            if (scoreValue < 1 || scoreValue > 100)
            {
                TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Validate per type (D-07)
            var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
            if (questionType == "MultipleChoice" && correctCount != 1)
            {
                TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "MultipleAnswer" && correctCount < 2)
            {
                TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "Essay" && string.IsNullOrWhiteSpace(rubrik))
            {
                TempData["Error"] = "Rubrik wajib diisi untuk soal Essay.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Phase 386 PXF-02 (F-DEV-01) — option-presence validation (≥2 ber-teks + checked-correct must be ber-teks).
            // Shared helper (kill-drift with EditQuestion). correctCount gate above is UNCHANGED.
            var (optOk, optErr) = QuestionOptionValidator.ValidateQuestionOptions(
                questionType,
                new[] { optionA, optionB, optionC, optionD },
                new[] { correctA, correctB, correctC, correctD });
            if (!optOk)
            {
                TempData["Error"] = optErr;
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            int nextOrder = pkg.Questions.Any() ? pkg.Questions.Max(q => q.Order) + 1 : 1;

            // Simpan gambar soal (IMG-01/03). SaveFileAsync null-safe → null bila tak ada file.
            var qImgUrl = await FileUploadHelper.SaveFileAsync(questionImage, _env.WebRootPath, $"uploads/questions/{packageId}", _logger);

            var newQ = new PackageQuestion
            {
                AssessmentPackageId = packageId,
                QuestionText = questionText.Trim(),
                QuestionType = questionType,
                ScoreValue = scoreValue,
                Order = nextOrder,
                ElemenTeknis = string.IsNullOrWhiteSpace(elemenTeknis) ? null : elemenTeknis.Trim(),
                Rubrik = questionType == "Essay" ? rubrik?.Trim() : null,
                MaxCharacters = questionType == "Essay" ? (maxCharacters > 0 ? maxCharacters : 2000) : 2000,
                ImagePath = qImgUrl,
                ImageAlt = TruncateAlt(questionImageAlt, 255)
            };

            if (questionType != "Essay")
            {
                var options = new[]
                {
                    (optionA, correctA, optionAImage, optionAImageAlt),
                    (optionB, correctB, optionBImage, optionBImageAlt),
                    (optionC, correctC, optionCImage, optionCImageAlt),
                    (optionD, correctD, optionDImage, optionDImageAlt)
                };
                foreach (var (text, isCorrect, oImg, oAlt) in options)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // IMG-02/03: simpan gambar opsi (null-safe).
                        var oImgUrl = await FileUploadHelper.SaveFileAsync(oImg, _env.WebRootPath, $"uploads/questions/{packageId}", _logger);
                        newQ.Options.Add(new PackageOption
                        {
                            OptionText = text!.Trim(),
                            IsCorrect = isCorrect,
                            ImagePath = oImgUrl,
                            ImageAlt = TruncateAlt(oAlt, 255)
                        });
                    }
                }
            }

            _context.PackageQuestions.Add(newQ);
            await _context.SaveChangesAsync();

            // Audit log: non-default score creation (D-11, CD-05)
            if (scoreValue != 10)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                        ? (currentUser?.FullName ?? "Unknown")
                        : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        actorName,
                        "CreateQuestion-CustomScore",
                        $"CreateQuestion: Question added with custom ScoreValue={scoreValue} (default 10) for Package #{packageId}",
                        newQ.Id,
                        "PackageQuestion");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit logging failed during CreateQuestion-CustomScore for Package {PackageId}", packageId);
                }
            }

            TempData["Success"] = "Soal berhasil ditambahkan.";

            // SHFX-01/D-06: auto-sync Pre→Post via helper tunggal (kill-drift).
            var parentPkgCQ = await _context.AssessmentPackages.FindAsync(packageId);
            if (parentPkgCQ != null)
                await SyncToLinkedPostIfSamePackageAsync(parentPkgCQ.AssessmentSessionId);

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
                // Compute affected sessions count for client-side modal trigger (D-09)
                var affectedSessions = await _context.PackageUserResponses
                    .Where(r => r.PackageQuestionId == q.Id)
                    .Select(r => r.AssessmentSessionId)
                    .Distinct()
                    .CountAsync();

                return Json(new
                {
                    id = q.Id,
                    order = q.Order,
                    questionText = q.QuestionText,
                    questionType = q.QuestionType ?? "MultipleChoice",
                    scoreValue = q.ScoreValue,
                    affectedSessions = affectedSessions,   // NEW field (D-09)
                    elemenTeknis = q.ElemenTeknis,
                    rubrik = q.Rubrik,
                    maxCharacters = q.MaxCharacters,
                    imagePath = q.ImagePath,   // D-06 / IMG-07: prefill thumbnail soal
                    imageAlt = q.ImageAlt,
                    options = q.Options.OrderBy(o => o.Id).Select(o => new
                    {
                        optionText = o.OptionText,
                        isCorrect = o.IsCorrect,
                        imagePath = o.ImagePath, // D-06 / IMG-07: prefill thumbnail opsi
                        imageAlt = o.ImageAlt
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
            bool correctA, bool correctB, bool correctC, bool correctD,
            IFormFile? questionImage, string? questionImageAlt, bool removeQuestionImage,
            IFormFile? optionAImage, IFormFile? optionBImage, IFormFile? optionCImage, IFormFile? optionDImage,
            string? optionAImageAlt, string? optionBImageAlt, string? optionCImageAlt, string? optionDImageAlt,
            bool removeOptionAImage, bool removeOptionBImage, bool removeOptionCImage, bool removeOptionDImage)
        {
            // Validate QuestionType whitelist (T-298-01)
            var validTypes = new[] { "MultipleChoice", "MultipleAnswer", "Essay" };
            if (!validTypes.Contains(questionType))
            {
                TempData["Error"] = "Tipe soal tidak valid.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Validate-all image uploads fail-fast (SHARED-3 / D-08). Null file → (true,null).
            foreach (var f in new[] { questionImage, optionAImage, optionBImage, optionCImage, optionDImage })
            {
                var (okImg, errImg) = FileUploadHelper.ValidateImageFile(f);
                if (!okImg)
                {
                    TempData["Error"] = errImg ?? "File harus berupa gambar JPG atau PNG.";
                    return RedirectToAction("ManagePackageQuestions", new { packageId });
                }
            }

            var q = await _context.PackageQuestions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            if (q == null) return NotFound();

            // SHFX-03/D-07: tolak-keras server-side bila Post-Test terkunci (paket-sama).
            var eqLockPkg = await _context.AssessmentPackages.FindAsync(packageId);
            if (eqLockPkg != null)
            {
                var eqLockSession = await _context.AssessmentSessions.FindAsync(eqLockPkg.AssessmentSessionId);
                if (eqLockSession != null && SessionEditLockRules.IsSessionEditLocked(eqLockSession))
                {
                    TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
                    return RedirectToAction("ManagePackageQuestions", new { packageId });
                }
            }

            // Range validation 1-100 (D-12, D-13) - replaces force-override removed per D-14 (Phase 306)
            if (scoreValue < 1 || scoreValue > 100)
            {
                TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Validate per type (D-07)
            var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
            if (questionType == "MultipleChoice" && correctCount != 1)
            {
                TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "MultipleAnswer" && correctCount < 2)
            {
                TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }
            if (questionType == "Essay" && string.IsNullOrWhiteSpace(rubrik))
            {
                TempData["Error"] = "Rubrik wajib diisi untuk soal Essay.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Phase 386 PXF-02 (F-DEV-01) — option-presence validation (≥2 ber-teks + checked-correct must be ber-teks).
            // Shared helper (kill-drift with CreateQuestion). correctCount gate above is UNCHANGED.
            var (optOk, optErr) = QuestionOptionValidator.ValidateQuestionOptions(
                questionType,
                new[] { optionA, optionB, optionC, optionD },
                new[] { correctA, correctB, correctC, correctD });
            if (!optOk)
            {
                TempData["Error"] = optErr;
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            // Capture old score for audit log delta detection (D-10)
            var oldScore = q.ScoreValue;

            q.QuestionText = questionText.Trim();
            q.QuestionType = questionType;
            q.ScoreValue = scoreValue;
            q.ElemenTeknis = string.IsNullOrWhiteSpace(elemenTeknis) ? null : elemenTeknis.Trim();
            q.Rubrik = questionType == "Essay" ? rubrik?.Trim() : null;
            q.MaxCharacters = questionType == "Essay" ? (maxCharacters > 0 ? maxCharacters : 2000) : 2000;

            // SYN-02 / D-10: kumpul path gambar lama yang harus dihapus fisik SETELAH auto-sync + ref-count.
            var imagePathsToDelete = new List<string>();

            // Resolusi niat gambar SOAL (D-05 file-baru-menang).
            if (questionImage != null)
            {
                var savedQImg = await FileUploadHelper.SaveFileAsync(questionImage, _env.WebRootPath, $"uploads/questions/{packageId}", _logger);
                if (!string.IsNullOrEmpty(q.ImagePath)) imagePathsToDelete.Add(q.ImagePath!);
                q.ImagePath = savedQImg;
                q.ImageAlt = TruncateAlt(questionImageAlt, 255);
            }
            else if (removeQuestionImage)
            {
                if (!string.IsNullOrEmpty(q.ImagePath)) imagePathsToDelete.Add(q.ImagePath!);
                q.ImagePath = null;
                q.ImageAlt = null;
            }
            else
            {
                q.ImageAlt = TruncateAlt(questionImageAlt, 255);
            }

            // OQ1: UPDATE-IN-PLACE opsi (jangan RemoveRange membabi-buta → preserve Id + ImagePath).
            if (questionType == "Essay")
            {
                // Essay tidak punya opsi: hapus semua opsi existing + delete-candidate gambarnya.
                foreach (var oldOpt in q.Options.ToList())
                {
                    if (!string.IsNullOrEmpty(oldOpt.ImagePath)) imagePathsToDelete.Add(oldOpt.ImagePath!);
                }
                _context.PackageOptions.RemoveRange(q.Options);
                q.Options.Clear();
            }
            else
            {
                var optTexts = new[] { optionA, optionB, optionC, optionD };
                var optCorrects = new[] { correctA, correctB, correctC, correctD };
                var optImages = new[] { optionAImage, optionBImage, optionCImage, optionDImage };
                var optAlts = new[] { optionAImageAlt, optionBImageAlt, optionCImageAlt, optionDImageAlt };
                var optRemoves = new[] { removeOptionAImage, removeOptionBImage, removeOptionCImage, removeOptionDImage };

                // Urutan SAMA dengan GET JSON (OrderBy o.Id == urutan pembuatan A-D).
                var existing = q.Options.OrderBy(o => o.Id).ToList();

                for (int i = 0; i < 4; i++)
                {
                    var hasText = !string.IsNullOrWhiteSpace(optTexts[i]);
                    var slot = i < existing.Count ? existing[i] : null;

                    if (slot != null && hasText)
                    {
                        // UPDATE in-place: text + correct; gambar preserve kecuali image-intent.
                        slot.OptionText = optTexts[i]!.Trim();
                        slot.IsCorrect = optCorrects[i];
                        await ApplyOptionImageIntent(slot, optImages[i], optAlts[i], optRemoves[i], packageId, imagePathsToDelete);
                    }
                    else if (slot != null && !hasText)
                    {
                        // Opsi dihapus (mis. 4→3): remove + delete-candidate gambar.
                        if (!string.IsNullOrEmpty(slot.ImagePath)) imagePathsToDelete.Add(slot.ImagePath!);
                        _context.PackageOptions.Remove(slot);
                        q.Options.Remove(slot);
                    }
                    else if (slot == null && hasText)
                    {
                        // Opsi baru di posisi i: ADD (oldPath null → hanya save bila ada file).
                        var newOpt = new PackageOption { OptionText = optTexts[i]!.Trim(), IsCorrect = optCorrects[i] };
                        await ApplyOptionImageIntent(newOpt, optImages[i], optAlts[i], optRemoves[i], packageId, imagePathsToDelete);
                        q.Options.Add(newOpt);
                    }
                    // slot == null && !hasText → skip (opsi tidak ada & tidak diisi)
                }
            }

            await _context.SaveChangesAsync();

            // Audit log: score change with affected sessions count (D-10)
            if (scoreValue != oldScore)
            {
                var affectedSessionsCount = await _context.PackageUserResponses
                    .Where(r => r.PackageQuestionId == questionId)
                    .Select(r => r.AssessmentSessionId)
                    .Distinct()
                    .CountAsync();

                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                        ? (currentUser?.FullName ?? "Unknown")
                        : $"{currentUser.NIP} - {currentUser.FullName}";
                    await _auditLog.LogAsync(
                        currentUser?.Id ?? "",
                        actorName,
                        "EditQuestion-ScoreChange",
                        $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)",
                        q.Id,
                        "PackageQuestion");
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Audit logging failed during EditQuestion-ScoreChange for Question {Id}", q.Id);
                }
            }

            TempData["Success"] = "Soal berhasil diperbarui.";

            // SHFX-01/D-06: auto-sync Pre→Post via helper tunggal (kill-drift).
            var parentPkgEQ = await _context.AssessmentPackages.FindAsync(packageId);
            if (parentPkgEQ != null)
                await SyncToLinkedPostIfSamePackageAsync(parentPkgEQ.AssessmentSessionId);

            // Ref-count + File.Delete SETELAH auto-sync (SYN-02 / D-10 / OQ2 ordering).
            // KRITIS: harus setelah SaveChanges DAN auto-sync — agar Post yang share path
            // sudah ter-update sebelum dicek; ref-count melindungi shared-file (Pre+Post).
            await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, _logger, imagePathsToDelete, "question image");

            return RedirectToAction("ManagePackageQuestions", new { packageId });
        }

        // Resolusi niat gambar OPSI (D-05 file-baru-menang). Menambah oldPath ke deleteList bila replace/remove.
        private async Task ApplyOptionImageIntent(PackageOption target, IFormFile? newFile, string? alt, bool removeChecked, int packageId, List<string> deleteList)
        {
            if (newFile != null)
            {
                var saved = await FileUploadHelper.SaveFileAsync(newFile, _env.WebRootPath, $"uploads/questions/{packageId}", _logger);
                if (!string.IsNullOrEmpty(target.ImagePath)) deleteList.Add(target.ImagePath!);
                target.ImagePath = saved;
                target.ImageAlt = TruncateAlt(alt, 255);   // IGNORE checkbox (file baru menang)
            }
            else if (removeChecked)
            {
                if (!string.IsNullOrEmpty(target.ImagePath)) deleteList.Add(target.ImagePath!);
                target.ImagePath = null;
                target.ImageAlt = null;
            }
            else
            {
                target.ImageAlt = TruncateAlt(alt, 255);    // keep gambar, alt boleh update
            }
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

            // SHFX-03/D-07: tolak-keras server-side bila Post-Test terkunci (paket-sama).
            var dqLockPkg = await _context.AssessmentPackages.FindAsync(packageId);
            if (dqLockPkg != null)
            {
                var dqLockSession = await _context.AssessmentSessions.FindAsync(dqLockPkg.AssessmentSessionId);
                if (dqLockSession != null && SessionEditLockRules.IsSessionEditLocked(dqLockSession))
                {
                    TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
                    return RedirectToAction("ManagePackageQuestions", new { packageId });
                }
            }

            // SYN-02 / D-10: path-collect SEBELUM RemoveRange (gambar soal + opsi).
            var imagePathsToDelete = new List<string>();
            if (!string.IsNullOrEmpty(q.ImagePath)) imagePathsToDelete.Add(q.ImagePath);
            foreach (var o in q.Options)
                if (!string.IsNullOrEmpty(o.ImagePath)) imagePathsToDelete.Add(o.ImagePath);

            // Remove any responses for this question
            var responses = await _context.PackageUserResponses
                .Where(r => r.PackageQuestionId == questionId)
                .ToListAsync();
            if (responses.Any()) _context.PackageUserResponses.RemoveRange(responses);

            _context.PackageOptions.RemoveRange(q.Options);
            _context.PackageQuestions.Remove(q);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Tidak bisa hapus soal: masih ada data yang berelasi.";
                return RedirectToAction("ManagePackageQuestions", new { packageId });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                    ? (currentUser?.FullName ?? "Unknown")
                    : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteQuestion",
                    $"Deleted question [ID={q.Id}] from package [ID={packageId}]",
                    packageId, "PackageQuestion");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteQuestion (questionId={Id})", q.Id); }

            TempData["Success"] = "Soal berhasil dihapus.";

            // SHFX-01/D-06: auto-sync Pre→Post via helper tunggal (kill-drift).
            var parentPkgDQ = await _context.AssessmentPackages.FindAsync(packageId);
            if (parentPkgDQ != null)
                await SyncToLinkedPostIfSamePackageAsync(parentPkgDQ.AssessmentSessionId);

            // SYN-02 / D-10 / OQ2: ref-count + File.Delete SETELAH auto-sync (warn-only per file).
            await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, _logger, imagePathsToDelete, "DeleteQuestion image");

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

        #region Extra Time (Phase 302)

        // WSE-03 (RST-04 / D-03): per-session cap — total extra time tak boleh melebihi durasi asli ujian.
        // Pure predicate (single source for the AddExtraTime cap gate) → unit-testable tanpa controller.
        public static bool ExtraTimeWithinCap(int currentExtra, int requestMinutes, int durationMinutes)
            => currentExtra + requestMinutes <= durationMinutes;

        [HttpPost]
        [Authorize(Roles = "Admin, HC")]      // WSE-03 / RST-01 — exact sibling string (ResetAssessment :3998), "Admin, HC" WITH space
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExtraTime(int assessmentId, int minutes)
        {
            if (minutes < 5 || minutes > 120 || minutes % 5 != 0)
                return Json(new { success = false, message = "Waktu harus antara 5-120 menit, kelipatan 5." });

            // Cari representative session dulu untuk mendapat identifier batch (title|category|date)
            var repSession = await _context.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == assessmentId);
            if (repSession == null)
                return Json(new { success = false, message = "Assessment tidak ditemukan." });

            // FIX ISS-08: scope batch via (Title, Category, Schedule.Date) — sama dengan
            // format SignalR batchKey di CMPController:1108 ("{Title}|{Category}|{Date:yyyy-MM-dd}").
            // Sebelumnya kode ini pakai AccessToken sebagai kunci — gagal untuk sesi legacy
            // (AccessToken=NULL) DAN tidak pernah match SignalR group Worker.
            var repTitle = repSession.Title;
            var repCategory = repSession.Category;
            var repDate = repSession.Schedule.Date;

            var sessions = await _context.AssessmentSessions
                .Where(s => s.Title == repTitle
                         && s.Category == repCategory
                         && s.Schedule.Date == repDate
                         && s.Status == "InProgress")
                .ToListAsync();

            if (!sessions.Any())
                return Json(new { success = false, message = "Tidak ada peserta aktif." });

            // RST-04 cap (D-03): total extra time per-sesi ≤ durasi asli. Reject-whole-batch (atomic, JSON contract — Pitfall 5).
            foreach (var session in sessions)
            {
                var currentExtra = session.ExtraTimeMinutes ?? 0;
                if (!ExtraTimeWithinCap(currentExtra, minutes, session.DurationMinutes))
                    return Json(new { success = false,
                        message = $"Total tambahan waktu tidak boleh melebihi durasi ujian ({session.DurationMinutes} menit). Saat ini sudah +{currentExtra} menit." });
            }

            foreach (var session in sessions)
            {
                session.ExtraTimeMinutes = (session.ExtraTimeMinutes ?? 0) + minutes;
            }
            await _context.SaveChangesAsync();

            // Broadcast ke semua peserta aktif via SignalR menggunakan composite batchKey
            // yang match dengan format di StartExam.cshtml + CMPController.
            var batchKey = $"{repTitle}|{repCategory}|{repDate:yyyy-MM-dd}";
            await _hubContext.Clients.Group($"batch-{batchKey}")
                .SendAsync("ExtraTimeAdded", minutes * 60);

            return Json(new { success = true, message = $"Waktu ujian berhasil ditambahkan. Peserta mendapat {minutes} menit tambahan." });
        }

        #endregion

        // ============================================================
        // PHASE 312: Role-tier guard helper untuk 3 delete methods
        // (DEL-01, T-312-01 PRIMARY mitigation, D-04 scope = Admin override + HC reject)
        // ============================================================
        private async Task<IActionResult?> EnsureCanDeleteAsync(
            string actionPrefix,                       // "DeleteAssessment" | "DeleteAssessmentGroup" | "DeletePrePostGroup"
            int targetId,
            string entityType,                         // "AssessmentSession"
            IList<AssessmentSession> sessions)
        {
            // PHASE 312 WR-05: self-defend kalau caller lupa load atau race delete-by-other-admin
            // Caller bug — fail loud via log + redirect dengan error generik (jangan silent pass)
            if (sessions == null || sessions.Count == 0)
            {
                var emptyLogger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
                emptyLogger.LogWarning(
                    "EnsureCanDeleteAsync called with empty sessions for {Action} TargetId={TargetId} — caller bug or race condition",
                    actionPrefix, targetId);
                TempData["Error"] = "Data assessment tidak ditemukan atau sudah dihapus. Silakan refresh halaman.";
                return RedirectToAction("ManageAssessment");
            }

            // Admin override: lewati semua cek (D-04 Admin tier)
            if (User.IsInRole("Admin")) return null;

            // HC tier: cek Status==Completed atau ada response peserta
            var sessionIds = sessions.Select(s => s.Id).ToList();
            int responseCount = await _context.PackageUserResponses
                .CountAsync(r => sessionIds.Contains(r.AssessmentSessionId));
            bool anyCompleted = sessions.Any(s => s.Status == "Completed");

            if (anyCompleted || responseCount > 0)
            {
                // AuditLog blocked entry (D-03)
                try
                {
                    var blockUser = await _userManager.GetUserAsync(User);
                    var blockActor = string.IsNullOrWhiteSpace(blockUser?.NIP)
                        ? (blockUser?.FullName ?? "Unknown")
                        : $"{blockUser.NIP} - {blockUser.FullName}";

                    // Per UI-SPEC line 170-172: format description per action type
                    string statusSummary;
                    if (sessions.Count == 1)
                    {
                        statusSummary = sessions[0].Status;
                    }
                    else if (actionPrefix == "DeletePrePostGroup")
                    {
                        // Per Q3 RESOLVED — explicit per-session "PreTest:X,PostTest:Y"
                        // Field: AssessmentSession.AssessmentType (Models/AssessmentSession.cs:154)
                        var pre = sessions.FirstOrDefault(s => s.AssessmentType == "PreTest");
                        var post = sessions.FirstOrDefault(s => s.AssessmentType == "PostTest");
                        statusSummary = $"PreTest:{pre?.Status ?? "?"},PostTest:{post?.Status ?? "?"}";
                    }
                    else
                    {
                        // Group: aggregate distinct status counts
                        statusSummary = string.Join("/", sessions.GroupBy(s => s.Status)
                            .Select(g => $"{g.Count()} {g.Key}"));
                    }

                    await _auditLog.LogAsync(
                        blockUser?.Id ?? "",
                        blockActor,
                        $"{actionPrefix}Blocked",
                        $"HC role blocked from {actionPrefix} [TargetId={targetId}]: Status={statusSummary}, ResponseCount={responseCount}",
                        targetId,
                        entityType);
                }
                catch (Exception auditEx)
                {
                    var blockLogger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
                    blockLogger.LogWarning(auditEx, "Audit log write failed for {Action}Blocked TargetId={TargetId}", actionPrefix, targetId);
                }

                TempData["Error"] = "Anda tidak memiliki izin untuk menghapus assessment yang sudah Completed atau memiliki jawaban peserta.";
                return RedirectToAction("ManageAssessment");
            }

            return null;  // pass — caller lanjut cascade
        }

        // === Variant B: Manual Entry — Info Sertifikasi Manual (REQ EXP-04) ===
        private int WriteManualEntrySection(ClosedXML.Excel.IXLWorksheet ws, AssessmentSession session, int startRow)
        {
            int row = startRow;
            ws.Cell(row, 1).Value = "Info Sertifikasi Manual";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Range(row, 1, row, 2).Merge();
            row++;

            var fields = new (string Label, string? Value)[]
            {
                ("Penyelenggara",    session.Penyelenggara),
                ("Kota",             session.Kota),
                ("Sub Kategori",     session.SubKategori),
                ("Tipe Sertifikat",  session.CertificateType),
            };
            foreach (var (label, value) in fields)
            {
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Value = value ?? "—";
                row++;
            }

            // Hyperlink ManualSertifikatUrl (REQ EXP-04 clickable link)
            ws.Cell(row, 1).Value = "Link Sertifikat";
            ws.Cell(row, 1).Style.Font.Bold = true;
            if (!string.IsNullOrWhiteSpace(session.ManualSertifikatUrl))
            {
                ws.Cell(row, 2).Value = session.ManualSertifikatUrl;
                ws.Cell(row, 2).SetHyperlink(new ClosedXML.Excel.XLHyperlink(session.ManualSertifikatUrl));
            }
            else
            {
                ws.Cell(row, 2).Value = "—";
            }
            row++;

            return row;
        }

        /// <summary>
        /// Phase 338 REST-06 (336-NAMING-CONVENTION-SPEC.md): Auto-detect counterpart Pre/Post group
        /// berdasarkan title pattern `{Pre|Post}Test {rest}`. Return LinkedGroupId existing counterpart bila ada.
        /// </summary>
        private async Task<int?> TryAutoDetectCounterpartGroup(string title, string? category)
        {
            // Pattern: "PreTest OJT GAST GTO SRU di Unit RU IV Cilacap" atau "Pre Test OJT GAST..."
            var match = System.Text.RegularExpressions.Regex.Match(
                title.Trim(),
                @"^(?<stage>Pre|Post)\s*Test\s+(?<rest>.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!match.Success) return null;

            var stage = match.Groups["stage"].Value;
            var rest = match.Groups["rest"].Value.Trim();
            var oppositeStage = stage.Equals("Pre", System.StringComparison.OrdinalIgnoreCase) ? "Post" : "Pre";

            // Search variants: "PreTest" (no space) + "Pre Test" (with space)
            var counterpartTitleA = $"{oppositeStage}Test {rest}";
            var counterpartTitleB = $"{oppositeStage} Test {rest}";

            var counterpart = await _context.AssessmentSessions
                .Where(s => s.Title == counterpartTitleA || s.Title == counterpartTitleB)
                .Where(s => category == null || s.Category == category)
                .Where(s => s.LinkedGroupId != null)
                .Select(s => s.LinkedGroupId)
                .FirstOrDefaultAsync();

            return counterpart;
        }

    }
}
