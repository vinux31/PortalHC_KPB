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
    [Route("Admin/[action]")]
    public class CoachMappingController : AdminBaseController
    {
        private readonly ILogger<CoachMappingController> _logger;
        private readonly INotificationService _notificationService;

        public CoachMappingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<CoachMappingController> logger,
            INotificationService notificationService)
            : base(context, userManager, auditLog, env)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        // Override View resolution to use Views/Admin/ folder
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        public async Task<IActionResult> CoachCoacheeMapping(
            string? search, string? section, bool showAll = false, int page = 1)
        {
            const int pageSize = 20;

            // 1. Load all users once (avoid N+1); use all for mapping display dict, active-only for modal dropdowns
            var allUsers = await _context.Users
                .Select(u => new { u.Id, u.FullName, u.NIP, u.Section, u.Unit, u.Position, u.RoleLevel, u.IsActive })
                .ToListAsync();
            var userDict = allUsers.ToDictionary(u => u.Id);
            var activeUsers = allUsers.Where(u => u.IsActive).ToList();

            // 2. Load mappings
            var query = _context.CoachCoacheeMappings.AsQueryable();
            if (!showAll)
                query = query.Where(m => m.IsActive);
            var mappings = await query.ToListAsync();

            // 3. Join with user data + apply filters (including parent.IsActive to prevent orphans)
            var rows = mappings.Select(m => new {
                Mapping = m,
                Coach = userDict.GetValueOrDefault(m.CoachId),
                Coachee = userDict.GetValueOrDefault(m.CoacheeId)
            }).Where(r => r.Coach?.IsActive == true && r.Coachee?.IsActive == true).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                rows = rows.Where(r =>
                    (r.Coach?.FullName?.ToLower().Contains(lower) ?? false) ||
                    (r.Coachee?.FullName?.ToLower().Contains(lower) ?? false) ||
                    (r.Coachee?.NIP?.ToLower().Contains(lower) ?? false))
                    .ToList();
            }
            if (!string.IsNullOrEmpty(section))
            {
                rows = rows.Where(r =>
                    r.Coach?.Section == section ||
                    r.Coachee?.Section == section)
                    .ToList();
            }

            // 4. Load active ProtonTrack assignments keyed by CoacheeId (filter out assignments with inactive ProtonKompetensi)
            var activeTrackAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.IsActive)
                .Include(a => a.ProtonTrack)
                    .ThenInclude(t => t.KompetensiList)
                .ToListAsync();
            var trackByCoachee = activeTrackAssignments
                .Where(a => a.ProtonTrack?.KompetensiList?.Any(k => k.IsActive) == true)
                .GroupBy(a => a.CoacheeId)
                .ToDictionary(g => g.Key, g => g.First().ProtonTrack?.DisplayName ?? "");

            // 5. Group by Coach, paginate over coach groups
            var grouped = rows
                .GroupBy(r => r.Mapping.CoachId)
                .Select(g => new {
                    CoachId = g.Key,
                    CoachName = g.First().Coach?.FullName ?? g.Key,
                    CoachSection = g.First().Coach?.Section ?? "",
                    ActiveCount = g.Count(r => r.Mapping.IsActive),
                    Coachees = g.Select(r => new {
                        r.Mapping.Id,
                        r.Mapping.IsActive,
                        r.Mapping.StartDate,
                        r.Mapping.EndDate,
                        r.Mapping.CoachId,
                        CoacheeName = r.Coachee?.FullName ?? r.Mapping.CoacheeId,
                        CoacheeNIP = r.Coachee?.NIP ?? "",
                        CoacheeSection = r.Coachee?.Section ?? "",
                        CoacheePosition = r.Coachee?.Position ?? "",
                        r.Mapping.CoacheeId,
                        ProtonTrack = trackByCoachee.GetValueOrDefault(r.Mapping.CoacheeId, ""),
                        AssignmentSection = r.Mapping.AssignmentSection ?? "",
                        AssignmentUnit = r.Mapping.AssignmentUnit ?? "",
                        IsCompleted = r.Mapping.IsCompleted
                    }).OrderBy(c => c.CoacheeName).ToList()
                })
                .OrderBy(g => g.CoachName)
                .ToList();

            var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);
            var paged = grouped.Skip(paging.Skip).Take(paging.Take).ToList();

            // 6. Modal data: eligible coaches, eligible coachees, proton tracks
            var activeCoacheeIds = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive)
                .Select(m => m.CoacheeId)
                .Distinct()
                .ToListAsync();

            ViewBag.GroupedCoaches = paged;
            ViewBag.CurrentPage = paging.CurrentPage;
            ViewBag.TotalPages = paging.TotalPages;
            ViewBag.TotalCount = paging.TotalCount;
            ViewBag.ShowAll = showAll;
            ViewBag.SearchTerm = search;
            ViewBag.SectionFilter = section;
            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            ViewBag.Sections = sectionUnitsDict.Keys.ToList();
            ViewBag.SectionUnits = sectionUnitsDict;
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
            // Phase 74: Coach role only — not level (Supervisor is level 5 but never a coach)
            // Filter to active users only so deactivated workers don't appear in assignment modals
            var coachRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);
            ViewBag.EligibleCoaches = coachRoleUsers
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName).ToList();
            var coacheeRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coachee);
            ViewBag.EligibleCoachees = coacheeRoleUsers
                .Where(u => u.IsActive && !activeCoacheeIds.Contains(u.Id))
                .OrderBy(u => u.FullName).ToList();
            ViewBag.AllUsers = activeUsers.OrderBy(u => u.FullName).ToList();
            ViewBag.ProtonTracks = await _context.ProtonTracks
                .OrderBy(t => t.Urutan).ToListAsync();

            return View();
        }

        // GET /Admin/DownloadMappingImportTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadMappingImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import CoachCoachee");

            // Row 1: Headers
            ws.Cell(1, 1).Value = "NIP Coach";
            ws.Cell(1, 2).Value = "NIP Coachee";
            for (int i = 1; i <= 2; i++)
            {
                ws.Cell(1, i).Style.Font.Bold = true;
                ws.Cell(1, i).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i).Style.Font.FontColor = XLColor.White;
            }

            // Row 2: Example data
            ws.Cell(2, 1).Value = "123456";
            ws.Cell(2, 2).Value = "789012";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
            ws.Cell(2, 2).Style.Font.Italic = true;
            ws.Cell(2, 2).Style.Font.FontColor = XLColor.Gray;

            // Row 3: Note
            ws.Cell(3, 1).Value = "Isi NIP Coach dan NIP Coachee. StartDate otomatis hari ini, IsActive otomatis true.";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

            return ExcelExportHelper.ToFileResult(workbook, "coach_coachee_import_template.xlsx", this);
        }

        // POST /Admin/ImportCoachCoacheeMapping
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportCoachCoacheeMapping(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ImportError"] = "Pilih file Excel terlebih dahulu.";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ImportError"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            const long maxSize = 10 * 1024 * 1024; // 10MB
            if (excelFile.Length > maxSize)
            {
                TempData["ImportError"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            // Load users keyed by NIP.
            // MED-01 fix: deteksi NIP duplikat di tabel Users dan surface warning ke admin,
            // sebelumnya GroupBy.First() silent memilih satu user secara non-deterministik.
            var allUsers = await _context.Users
                .Where(u => u.NIP != null)
                .Select(u => new { u.Id, u.NIP, u.Section, u.Unit })
                .ToListAsync();
            var duplicateNips = allUsers
                .GroupBy(u => u.NIP!)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            var usersByNip = allUsers
                .GroupBy(u => u.NIP!)
                .ToDictionary(g => g.Key, g => g.First());

            // Load all existing mappings
            var allMappings = await _context.CoachCoacheeMappings.ToListAsync();

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

            var results = new List<ImportMappingResult>();
            var newMappings = new List<CoachCoacheeMapping>();
            var reactivatedMappings = new List<CoachCoacheeMapping>();

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                if (!workbook.Worksheets.Any())
                {
                    TempData["ImportError"] = "File Excel tidak memiliki worksheet.";
                    return RedirectToAction(nameof(ImportCoachCoacheeMapping));
                }
                var ws = workbook.Worksheets.First();

                // D-16: Header validation
                var expectedHeaders = new[] { "NIP Coach", "NIP Coachee" };
                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var actual = ws.Cell(1, i + 1).GetString().Trim();
                    if (!actual.Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ImportError"] = $"Header kolom {i + 1} tidak cocok. Diharapkan: '{expectedHeaders[i]}', ditemukan: '{actual}'. Pastikan menggunakan template yang benar.";
                        return RedirectToAction(nameof(CoachCoacheeMapping));
                    }
                }

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nipCoach = (row.Cell(1).GetString() ?? "").Trim();
                    var nipCoachee = (row.Cell(2).GetString() ?? "").Trim();

                    // Skip completely blank rows
                    if (string.IsNullOrWhiteSpace(nipCoach) && string.IsNullOrWhiteSpace(nipCoachee))
                        continue;

                    var result = new ImportMappingResult
                    {
                        RowNum = row.RowNumber(),
                        NipCoach = nipCoach,
                        NipCoachee = nipCoachee
                    };

                    if (string.IsNullOrWhiteSpace(nipCoach) || string.IsNullOrWhiteSpace(nipCoachee))
                    {
                        result.Status = "Error";
                        result.Message = "NIP Coach atau NIP Coachee kosong";
                        results.Add(result);
                        continue;
                    }

                    if (!usersByNip.TryGetValue(nipCoach, out var coachUser))
                    {
                        result.Status = "Error";
                        result.Message = $"NIP Coach '{nipCoach}' tidak ditemukan";
                        results.Add(result);
                        continue;
                    }

                    if (!usersByNip.TryGetValue(nipCoachee, out var coacheeUser))
                    {
                        result.Status = "Error";
                        result.Message = $"NIP Coachee '{nipCoachee}' tidak ditemukan";
                        results.Add(result);
                        continue;
                    }

                    if (coachUser.Id == coacheeUser.Id)
                    {
                        result.Status = "Error";
                        result.Message = "Coach tidak dapat menjadi coachee dirinya sendiri";
                        results.Add(result);
                        continue;
                    }

                    // Phase 261: Validate coachee Section/Unit against OrganizationUnit
                    if (string.IsNullOrEmpty(coacheeUser.Section) || string.IsNullOrEmpty(coacheeUser.Unit)
                        || !sectionUnitsDict.TryGetValue(coacheeUser.Section.Trim(), out var vuImport)
                        || !vuImport.Contains(coacheeUser.Unit.Trim()))
                    {
                        result.Status = "Error";
                        result.Message = $"Section/Unit coachee ('{coacheeUser.Section}'/'{coacheeUser.Unit}') tidak valid di OrganizationUnit aktif";
                        results.Add(result);
                        continue;
                    }

                    // Check for existing active mapping
                    var activeMapping = allMappings.FirstOrDefault(m =>
                        m.CoachId == coachUser.Id && m.CoacheeId == coacheeUser.Id && m.IsActive);
                    if (activeMapping != null)
                    {
                        result.Status = "Skip";
                        result.Message = "Mapping sudah aktif";
                        results.Add(result);
                        continue;
                    }

                    // Check for existing inactive mapping (reactivate)
                    var inactiveMapping = allMappings.FirstOrDefault(m =>
                        m.CoachId == coachUser.Id && m.CoacheeId == coacheeUser.Id && !m.IsActive);
                    if (inactiveMapping != null)
                    {
                        inactiveMapping.IsActive = true;
                        inactiveMapping.StartDate = DateTime.Today;
                        inactiveMapping.EndDate = null;
                        inactiveMapping.AssignmentSection = coacheeUser.Section.Trim();
                        inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim();
                        reactivatedMappings.Add(inactiveMapping);
                        result.Status = "Reactivated";
                        result.Message = "Mapping diaktifkan kembali";
                        results.Add(result);
                        continue;
                    }

                    // Create new mapping
                    var newMapping = new CoachCoacheeMapping
                    {
                        CoachId = coachUser.Id,
                        CoacheeId = coacheeUser.Id,
                        IsActive = true,
                        StartDate = DateTime.Today,
                        AssignmentSection = coacheeUser.Section,
                        AssignmentUnit = coacheeUser.Unit
                    };
                    newMappings.Add(newMapping);
                    result.Status = "Success";
                    result.Message = "Berhasil dibuat";
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = $"Gagal membaca file Excel: {ex.Message}";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            // HIGH-04: track berapa ProtonTrackAssignment yang ikut direaktivasi (untuk audit log)
            var reactivatedAssignmentCount = 0;

            // D-13: Wrap insert phase dalam transaction untuk atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (newMappings.Any())
                    await _context.CoachCoacheeMappings.AddRangeAsync(newMappings);
                // reactivated mappings sudah di-track oleh EF (IsActive diubah di-memory)

                // HIGH-04: Untuk setiap mapping yang direaktivasi, reaktivasi juga ProtonTrackAssignment
                // terakhir milik coachee itu (jika ada dan inactive). Tidak membuat assignment baru —
                // hanya reuse state lama agar CoachingProton view kembali memunculkan coachee.
                if (reactivatedMappings.Any())
                {
                    var reactCoacheeIds = reactivatedMappings.Select(m => m.CoacheeId).Distinct().ToList();
                    var lastAssignments = await _context.ProtonTrackAssignments
                        .Where(a => reactCoacheeIds.Contains(a.CoacheeId))
                        .GroupBy(a => a.CoacheeId)
                        .Select(g => g.OrderByDescending(a => a.Id).First())
                        .ToListAsync();
                    foreach (var asg in lastAssignments)
                    {
                        if (!asg.IsActive)
                        {
                            asg.IsActive = true;
                            asg.DeactivatedAt = null;
                            reactivatedAssignmentCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "ImportCoachCoacheeMapping transaction failed");
                TempData["ImportError"] = "Import gagal. Semua perubahan dibatalkan.";
                return RedirectToAction(nameof(CoachCoacheeMapping));
            }

            var successCount = results.Count(r => r.Status == "Success");
            var reactivatedCount = results.Count(r => r.Status == "Reactivated");
            var skipCount = results.Count(r => r.Status == "Skip");
            var errorCount = results.Count(r => r.Status == "Error");

            var actor = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actor?.Id ?? "system",
                ActorName = actor?.FullName ?? "system",
                ActionType = "ImportCoachCoacheeMapping",
                Description = $"Import {successCount} mapping baru, {reactivatedCount} diaktifkan kembali ({reactivatedAssignmentCount} ProtonTrackAssignment ikut direaktivasi), {skipCount} dilewati, {errorCount} error",
                TargetType = "CoachCoacheeMapping",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["ImportResults"] = System.Text.Json.JsonSerializer.Serialize(results);
            // MED-01 fix: laporkan NIP duplikat di tabel Users ke admin (non-blocking warning).
            if (duplicateNips.Any())
            {
                TempData["ImportWarnings"] = $"Terdeteksi NIP duplikat di tabel Users: {string.Join(", ", duplicateNips)}. Mapping untuk NIP ini dipasang ke user pertama secara non-deterministik — harap bersihkan duplikat di master Users.";
            }
            return RedirectToAction(nameof(CoachCoacheeMapping));
        }

        // POST /Admin/CoachCoacheeMappingAssign
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingAssign([FromBody] CoachAssignRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.CoachId) || req.CoacheeIds == null || req.CoacheeIds.Count == 0)
                return Json(new { success = false, message = "Data tidak lengkap." });

            if (req.CoacheeIds.Contains(req.CoachId))
                return Json(new { success = false, message = "Coach tidak dapat menjadi coachee dirinya sendiri." });

            if (string.IsNullOrWhiteSpace(req.AssignmentSection) || string.IsNullOrWhiteSpace(req.AssignmentUnit))
                return Json(new { success = false, message = "Assignment Section dan Unit wajib diisi." });

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            if (!sectionUnitsDict.TryGetValue(req.AssignmentSection!.Trim(), out var validUnits)
                || !validUnits.Contains(req.AssignmentUnit!.Trim()))
                return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });

            // Check for duplicate active mappings
            var existingMappings = await _context.CoachCoacheeMappings
                .Where(m => req.CoacheeIds.Contains(m.CoacheeId) && m.IsActive)
                .ToListAsync();

            if (existingMappings.Any())
            {
                var allUsers = await _context.Users
                    .Select(u => new { u.Id, FullName = u.FullName ?? u.Id })
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);

                var names = existingMappings
                    .Select(m => allUsers.GetValueOrDefault(m.CoacheeId, m.CoacheeId))
                    .Distinct()
                    .ToList();

                return Json(new { success = false, message = $"Coachee sudah memiliki coach aktif: {string.Join(", ", names)}" });
            }

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            // D-09/D-10/D-11/D-12: Progression warning check for Tahun 2/3 assignment
            if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
            {
                var requestedTrack = await _context.ProtonTracks.FindAsync(req.ProtonTrackId.Value);
                if (requestedTrack != null)
                {
                    // Find previous track in the same TrackType (e.g. Panelman Tahun 1 before Panelman Tahun 2)
                    var prevTrack = await _context.ProtonTracks
                        .Where(t => t.TrackType == requestedTrack.TrackType
                                 && t.Urutan == requestedTrack.Urutan - 1)
                        .FirstOrDefaultAsync();
                    if (prevTrack != null)
                    {
                        var incompleteCoachees = new List<string>();
                        foreach (var coacheeId in req.CoacheeIds)
                        {
                            // D-11: Skip warning if coachee already has an assignment for this track (reactivated scenario)
                            var hasExistingAssignment = await _context.ProtonTrackAssignments
                                .AnyAsync(a => a.CoacheeId == coacheeId
                                           && a.ProtonTrackId == req.ProtonTrackId.Value);
                            if (hasExistingAssignment) continue;

                            // Check if previous track assignment exists and all progress is Approved
                            var prevAssignment = await _context.ProtonTrackAssignments
                                .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId
                                                       && a.ProtonTrackId == prevTrack.Id);
                            if (prevAssignment == null)
                            {
                                incompleteCoachees.Add(coacheeId);
                                continue;
                            }

                            var prevProgressCount = await _context.ProtonDeliverableProgresses
                                .CountAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id);
                            var allApproved = prevProgressCount > 0 && !await _context.ProtonDeliverableProgresses
                                .AnyAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id
                                           && p.Status != "Approved");
                            if (!allApproved)
                                incompleteCoachees.Add(coacheeId);
                        }

                        // D-09: Warning only — return warning response if incomplete and user hasn't confirmed
                        if (incompleteCoachees.Any() && !req.ConfirmProgressionWarning)
                        {
                            return Json(new { success = false, warning = true,
                                message = $"{incompleteCoachees.Count} coachee belum menyelesaikan {prevTrack.DisplayName}. Tetap lanjutkan?",
                                incompleteCount = incompleteCoachees.Count });
                        }
                    }
                }
            }

            var startDate = req.StartDate ?? DateTime.Today;

            var newMappings = req.CoacheeIds.Select(id => new CoachCoacheeMapping
            {
                CoachId = req.CoachId,
                CoacheeId = id,
                IsActive = true,
                StartDate = startDate,
                AssignmentSection = req.AssignmentSection!.Trim(),
                AssignmentUnit = req.AssignmentUnit!.Trim()
            }).ToList();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
            _context.CoachCoacheeMappings.AddRange(newMappings);

            // ProtonTrack side-effect
            if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
            {
                // Deactivate any currently active assignments for a different track
                var existingTracks = await _context.ProtonTrackAssignments
                    .Where(a => req.CoacheeIds.Contains(a.CoacheeId) && a.IsActive && a.ProtonTrackId != req.ProtonTrackId.Value)
                    .ToListAsync();
                foreach (var t in existingTracks)
                {
                    t.IsActive = false;
                    t.DeactivatedAt = DateTime.UtcNow;
                }

                // FIX-02: For each coachee, reuse an existing inactive assignment for this track instead of creating a duplicate.
                var allWarnings = new List<string>();
                foreach (var coacheeId in req.CoacheeIds)
                {
                    // Check if already active for this track (no-op)
                    var alreadyActive = await _context.ProtonTrackAssignments
                        .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == req.ProtonTrackId.Value && a.IsActive);
                    if (alreadyActive) continue;

                    // Check for an existing inactive assignment for this coachee+track
                    var existing = await _context.ProtonTrackAssignments
                        .Where(a => a.CoacheeId == coacheeId && a.ProtonTrackId == req.ProtonTrackId.Value && !a.IsActive)
                        .OrderByDescending(a => a.Id)
                        .FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        // Reuse it — existing ProtonDeliverableProgress rows are already linked
                        existing.IsActive = true;
                        existing.DeactivatedAt = null;
                    }
                    else
                    {
                        // Create a new assignment and auto-create progress rows
                        var newAssignment = new ProtonTrackAssignment
                        {
                            CoacheeId = coacheeId,
                            AssignedById = actor.Id,
                            ProtonTrackId = req.ProtonTrackId.Value,
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.ProtonTrackAssignments.Add(newAssignment);
                        await _context.SaveChangesAsync(); // flush to get assignment ID
                        var w = await AutoCreateProgressForAssignment(newAssignment.Id, newAssignment.ProtonTrackId, coacheeId);
                        allWarnings.AddRange(w);
                    }
                }
                if (allWarnings.Any())
                    TempData["Warning"] = string.Join("\n", allWarnings);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CoachCoacheeMappingAssign failed (CoachId={CoachId}, Coachees={Count})",
                    req.CoachId, req.CoacheeIds?.Count ?? 0);
                await tx.RollbackAsync();
                return Json(new { success = false, message = "Gagal menyimpan assignment. Operasi dibatalkan." });
            }

            var count = newMappings.Count;
            await _auditLog.LogAsync(actor.Id, actor.FullName, "Assign",
                $"Assigned coach to {count} coachee(s) [Section: {req.AssignmentSection}, Unit: {req.AssignmentUnit}]",
                targetType: "CoachCoacheeMapping");

            // COACH-01: Notify coach for each coachee assigned
            try
            {
                var coacheeUsers = await _context.Users
                    .Where(u => req.CoacheeIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);
                foreach (var coacheeId in req.CoacheeIds)
                {
                    var coacheeName = coacheeUsers.GetValueOrDefault(coacheeId, coacheeId);
                    await _notificationService.SendAsync(req.CoachId, "COACH_ASSIGNED",
                        "Coach Ditunjuk",
                        $"Anda ditunjuk sebagai coach untuk {coacheeName}",
                        "/CDP/CoachingProton");
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }

            return Json(new { success = true, message = $"{count} mapping berhasil dibuat." });
        }

        // POST /Admin/CoachCoacheeMappingEdit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingEdit([FromBody] CoachEditRequest req)
        {
            if (req == null)
                return Json(new { success = false, message = "Data tidak lengkap." });

            var mapping = await _context.CoachCoacheeMappings.FindAsync(req.MappingId);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });

            if (req.CoachId == mapping.CoacheeId)
                return Json(new { success = false, message = "Coach tidak dapat menjadi coachee dirinya sendiri." });

            // Check for duplicate: if changing coach, ensure no other active mapping exists for coachee with new coach
            if (req.CoachId != mapping.CoachId)
            {
                var duplicate = await _context.CoachCoacheeMappings
                    .AnyAsync(m => m.CoacheeId == mapping.CoacheeId && m.CoachId == req.CoachId && m.IsActive && m.Id != req.MappingId);
                if (duplicate)
                    return Json(new { success = false, message = "Sudah ada mapping aktif antara coach dan coachee ini." });
            }

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            var secEdit = req.AssignmentSection?.Trim();
            var unitEdit = req.AssignmentUnit?.Trim();
            if (!string.IsNullOrEmpty(secEdit) && !string.IsNullOrEmpty(unitEdit))
            {
                if (!sectionUnitsDict.TryGetValue(secEdit, out var validUnitsEdit) || !validUnitsEdit.Contains(unitEdit))
                    return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });
            }

            // CRIT-02 fix: wrap ALL mutations (mapping fields, ProtonTrack rebuild, Phase 129
            // unit-change rebuild, audit log) in ONE transaction. Disk file deletes are deferred
            // until after CommitAsync so a rollback never leaves DB rows pointing at missing files.
            var foldersToDelete = new List<string>();
            int deletedCount = 0, createdCount = 0;
            string? oldUnit;
            string? newUnit;
            bool unitChanged;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                mapping.CoachId = req.CoachId;
                if (req.StartDate.HasValue)
                    mapping.StartDate = req.StartDate.Value;

                // Phase 129: Detect AssignmentUnit change for progress rebuild
                oldUnit = mapping.AssignmentUnit;
                mapping.AssignmentSection = req.AssignmentSection?.Trim();
                mapping.AssignmentUnit = req.AssignmentUnit?.Trim();
                newUnit = mapping.AssignmentUnit;
                unitChanged = (oldUnit?.Trim() ?? "") != (newUnit?.Trim() ?? "");

                // ProtonTrack side-effect
                if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
                {
                    var existingTracks = await _context.ProtonTrackAssignments
                        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                        .ToListAsync();
                    foreach (var t in existingTracks)
                    {
                        t.IsActive = false;
                        foldersToDelete.AddRange(await CleanupProgressForAssignment(t.Id));
                    }

                    var newAssignment = new ProtonTrackAssignment
                    {
                        CoacheeId = mapping.CoacheeId,
                        AssignedById = actor.Id,
                        ProtonTrackId = req.ProtonTrackId.Value,
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.ProtonTrackAssignments.Add(newAssignment);
                    await _context.SaveChangesAsync(); // flush to get assignment ID (still inside tx)

                    var editWarnings = await AutoCreateProgressForAssignment(newAssignment.Id, newAssignment.ProtonTrackId, mapping.CoacheeId);
                    if (editWarnings.Any())
                        TempData["Warning"] = string.Join("\n", editWarnings);
                }

                await _context.SaveChangesAsync();

                // Phase 129: If unit changed and ProtonTrack wasn't already rebuilt, rebuild progress for new unit
                if (unitChanged && !(req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0))
                {
                    var activeAssignments = await _context.ProtonTrackAssignments
                        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                        .ToListAsync();

                    foreach (var a in activeAssignments)
                    {
                        // Count existing progress before cleanup
                        deletedCount += await _context.ProtonDeliverableProgresses
                            .CountAsync(p => p.ProtonTrackAssignmentId == a.Id);
                        foldersToDelete.AddRange(await CleanupProgressForAssignment(a.Id));
                    }
                    await _context.SaveChangesAsync(); // flush deletes before recreate

                    foreach (var a in activeAssignments)
                    {
                        var warnings = await AutoCreateProgressForAssignment(a.Id, a.ProtonTrackId, mapping.CoacheeId);
                        createdCount += await _context.ProtonDeliverableProgresses
                            .CountAsync(p => p.ProtonTrackAssignmentId == a.Id);
                        if (warnings.Any())
                            TempData["Warning"] = string.Join("\n", warnings);
                    }

                    TempData["Info"] = $"Unit berubah dari '{oldUnit}' ke '{newUnit}' → {deletedCount} progress dihapus, {createdCount} progress baru dibuat untuk unit {newUnit}";
                }

                // AuditLogService uses the same scoped ApplicationDbContext, so it must be
                // called inside the transaction so it commits atomically with the mapping change.
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Edit",
                    $"Edited coach-coachee mapping #{mapping.Id}", targetId: mapping.Id, targetType: "CoachCoacheeMapping");

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "CoachCoacheeMappingEdit failed for mapping {MappingId}; rolled back", req.MappingId);
                return Json(new { success = false, message = $"Gagal menyimpan perubahan: {ex.Message}" });
            }

            // Post-commit: now that the DB change is durable, delete evidence folders on disk.
            // Failures here must not fail the request — log and continue.
            foreach (var folder in foldersToDelete)
            {
                try
                {
                    if (Directory.Exists(folder))
                        Directory.Delete(folder, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete evidence folder {Folder} after mapping edit commit", folder);
                }
            }

            // COACH-02: Notify both coach and coachee about mapping edit
            try
            {
                var coachUser = await _context.Users.FindAsync(mapping.CoachId);
                var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
                var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
                var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

                await _notificationService.SendAsync(mapping.CoachId, "COACH_MAPPING_EDITED",
                    "Mapping Coaching Diubah",
                    $"Mapping coaching Anda dengan {coacheeName} telah diubah",
                    "/CDP/CoachingProton");
                await _notificationService.SendAsync(mapping.CoacheeId, "COACH_MAPPING_EDITED",
                    "Mapping Coaching Diubah",
                    $"Mapping coaching Anda dengan {coachName} telah diubah",
                    "/CDP/CoachingProton");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }

            return Json(new { success = true, message = "Mapping berhasil diperbarui." });
        }

        // POST /Admin/CleanupCoachCoacheeMappingOrg
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupCoachCoacheeMappingOrg()
        {
            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

            var activeMappings = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive)
                .ToListAsync();

            var userDict = await _context.Users
                .Select(u => new { u.Id, u.Section, u.Unit })
                .ToDictionaryAsync(u => u.Id, u => new { u.Section, u.Unit });

            int autoFixed = 0;
            var unfixable = new List<object>();

            foreach (var m in activeMappings)
            {
                var sec = m.AssignmentSection?.Trim();
                var unit = m.AssignmentUnit?.Trim();
                bool isValid = !string.IsNullOrEmpty(sec) && !string.IsNullOrEmpty(unit)
                    && sectionUnitsDict.TryGetValue(sec, out var vu) && vu.Contains(unit);

                if (isValid) continue;

                // Try fix from coachee user record
                if (userDict.TryGetValue(m.CoacheeId, out var coacheeInfo))
                {
                    var userSec = coacheeInfo.Section?.Trim();
                    var userUnit = coacheeInfo.Unit?.Trim();
                    bool userValid = !string.IsNullOrEmpty(userSec) && !string.IsNullOrEmpty(userUnit)
                        && sectionUnitsDict.TryGetValue(userSec, out var vuUser) && vuUser.Contains(userUnit);

                    if (userValid)
                    {
                        m.AssignmentSection = userSec;
                        m.AssignmentUnit = userUnit;
                        autoFixed++;
                        continue;
                    }
                }

                unfixable.Add(new { m.Id, m.CoacheeId, m.AssignmentSection, m.AssignmentUnit });
            }

            await _context.SaveChangesAsync();

            TempData["CleanupReport"] = System.Text.Json.JsonSerializer.Serialize(new { autoFixed, unfixable });
            return RedirectToAction(nameof(CoachCoacheeMapping));
        }

        // POST /Admin/CoachCoacheeMappingGetSessionCount
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingGetSessionCount(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });

            var activeSessionCount = await _context.CoachingSessions
                .CountAsync(s => s.CoachId == mapping.CoachId && s.CoacheeId == mapping.CoacheeId && s.Status == "Draft");

            return Json(new { success = true, count = activeSessionCount });
        }

        // GET /Admin/CoachCoacheeMappingActiveAssignmentCount
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMappingActiveAssignmentCount(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null) return Json(new { count = 0 });
            var count = await _context.ProtonTrackAssignments
                .CountAsync(a => a.CoacheeId == mapping.CoacheeId && a.IsActive);
            return Json(new { count });
        }

        // POST /Admin/CoachCoacheeMappingDeactivate
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingDeactivate(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });
            if (!mapping.IsActive)
                return Json(new { success = false, message = "Mapping sudah tidak aktif." });

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
            mapping.IsActive = false;
            mapping.EndDate = DateTime.UtcNow;

            // Cascade: deactivate all ProtonTrackAssignments for this coachee
            // FIX-01: stamp DeactivatedAt so reactivation can correlate assignments back to this event
            var deactivationTime = mapping.EndDate.Value;
            var activeAssignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                .ToListAsync();
            foreach (var a in activeAssignments)
            {
                a.IsActive = false;
                a.DeactivatedAt = deactivationTime;
            }
            int cascadeCount = activeAssignments.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogAsync(actor.Id, actor.FullName, "Deactivate",
                $"Deactivated coach-coachee mapping #{id} — {cascadeCount} ProtonTrackAssignment(s) also deactivated", targetId: id, targetType: "CoachCoacheeMapping");

            // COACH-03: Notify both coach and coachee about deactivation
            try
            {
                var coachUser = await _context.Users.FindAsync(mapping.CoachId);
                var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
                var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
                var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

                await _notificationService.SendAsync(mapping.CoachId, "COACH_MAPPING_DEACTIVATED",
                    "Mapping Coaching Dinonaktifkan",
                    $"Mapping coaching Anda dengan {coacheeName} telah dinonaktifkan",
                    "/CDP/CoachingProton");
                await _notificationService.SendAsync(mapping.CoacheeId, "COACH_MAPPING_DEACTIVATED",
                    "Mapping Coaching Dinonaktifkan",
                    $"Mapping coaching Anda dengan {coachName} telah dinonaktifkan",
                    "/CDP/CoachingProton");
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }

            return Json(new { success = true, message = $"Mapping berhasil dinonaktifkan. {cascadeCount} track assignment juga dinonaktifkan." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "CoachCoacheeMappingDeactivate transaction failed for mapping {Id}", id);
                return Json(new { success = false, message = "Operasi gagal. Semua perubahan dibatalkan." });
            }
        }

        // POST /Admin/CoachCoacheeMappingReactivate
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingReactivate(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });
            if (mapping.IsActive)
                return Json(new { success = false, message = "Mapping sudah aktif." });

            // Validate: no other active mapping for the same coachee
            var duplicateActive = await _context.CoachCoacheeMappings
                .AnyAsync(m => m.CoacheeId == mapping.CoacheeId && m.IsActive && m.Id != id);
            if (duplicateActive)
                return Json(new { success = false, message = "Coachee sudah memiliki coach aktif lain. Nonaktifkan dulu sebelum mengaktifkan mapping ini." });

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            // D-08: Capture originalEndDate BEFORE modifying mapping (avoid fragile OriginalValues API)
            var originalEndDate = mapping.EndDate;
            mapping.IsActive = true;
            mapping.EndDate = null;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
            // FIX-01: Only reactivate assignments that were deactivated as part of this mapping's deactivation event.
            // We correlate by DeactivatedAt timestamp (within 5 seconds of originalEndDate) to avoid restoring
            // assignments that were independently deactivated for other reasons.
            List<ProtonTrackAssignment> inactiveAssignments;
            if (originalEndDate.HasValue)
            {
                inactiveAssignments = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == mapping.CoacheeId
                        && !a.IsActive
                        && a.DeactivatedAt != null
                        && EF.Functions.DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) >= -5
                        && EF.Functions.DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) <= 5)
                    .ToListAsync();
            }
            else
            {
                // Mapping was deactivated before DeactivatedAt existed — fall back to all inactive assignments
                inactiveAssignments = await _context.ProtonTrackAssignments
                    .Where(a => a.CoacheeId == mapping.CoacheeId && !a.IsActive && a.DeactivatedAt == null)
                    .ToListAsync();
            }

            foreach (var a in inactiveAssignments)
            {
                a.IsActive = true;
                a.DeactivatedAt = null;
            }
            int reactivatedCount = inactiveAssignments.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogAsync(actor.Id, actor.FullName, "Reactivate",
                $"Reactivated coach-coachee mapping #{id} — {reactivatedCount} ProtonTrackAssignment(s) also reactivated", targetId: id, targetType: "CoachCoacheeMapping");

            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            return Json(new { success = true,
                message = $"Mapping berhasil diaktifkan kembali. {reactivatedCount} track assignment juga diaktifkan kembali.",
                showAssignPrompt = reactivatedCount == 0,
                coacheeName = coacheeUser?.FullName ?? "",
                assignUrl = Url.Action("CoachCoacheeMapping", "Admin") });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "CoachCoacheeMappingReactivate transaction failed for mapping {Id}", id);
                return Json(new { success = false, message = "Operasi gagal. Semua perubahan dibatalkan." });
            }
        }

        // Phase 236 COMP-04: completion criteria helper per D-13
        private async Task<bool> IsYearCompletedAsync(int assignmentId)
        {
            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == assignmentId)
                .ToListAsync();
            if (!progresses.Any()) return false;
            bool allApproved = progresses.All(p => p.Status == "Approved");
            bool hasFinalAssessment = await _context.ProtonFinalAssessments
                .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
            return allApproved && hasFinalAssessment;
        }

        // Phase 236 COMP-04: Mark mapping as completed/graduated per D-15
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> MarkMappingCompleted(int mappingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var mapping = await _context.CoachCoacheeMappings.FindAsync(mappingId);
            if (mapping == null) return NotFound();
            // Validate: coachee harus punya semua tahun completed
            var assignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
                .ToListAsync();
            var tahun3Assignment = assignments
                .FirstOrDefault(a => a.ProtonTrack != null && a.ProtonTrack.TahunKe == "Tahun 3");
            if (tahun3Assignment == null)
            {
                TempData["Error"] = "Coachee belum memiliki assignment Tahun 3.";
                return RedirectToAction("CoachCoacheeMapping");
            }
            bool tahun3Complete = await IsYearCompletedAsync(tahun3Assignment.Id);
            if (!tahun3Complete)
            {
                TempData["Error"] = "Tahun 3 belum selesai — semua deliverable harus Approved dan final assessment harus ada.";
                return RedirectToAction("CoachCoacheeMapping");
            }
            mapping.IsCompleted = true;
            mapping.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var actorName = string.IsNullOrWhiteSpace(user.NIP)
                ? (user.FullName ?? "Unknown")
                : $"{user.NIP} - {user.FullName}";
            await _auditLog.LogAsync(user.Id, actorName, "MarkMappingCompleted",
                $"Mapping ID={mappingId} ditandai graduated. CoacheeId={mapping.CoacheeId}", mappingId, "CoachCoacheeMapping");
            TempData["Success"] = "Coachee berhasil ditandai sebagai graduated.";
            return RedirectToAction("CoachCoacheeMapping");
        }

        // GET /Admin/CoachCoacheeMappingDeletePreview
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMappingDeletePreview(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return NotFound(new { success = false, message = "Mapping tidak ditemukan." });
            if (mapping.IsActive)
                return BadRequest(new { success = false, message = "Hanya mapping nonaktif yang dapat dihapus." });

            var coachUser = await _context.Users.FindAsync(mapping.CoachId);
            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
            var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

            var assignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == mapping.CoacheeId)
                .Select(a => a.Id)
                .ToListAsync();

            var progressCount = await _context.ProtonDeliverableProgresses
                .CountAsync(p => assignments.Contains(p.ProtonTrackAssignmentId));

            return Json(new
            {
                coachName,
                coacheeName,
                assignmentCount = assignments.Count,
                progressCount
            });
        }

        // POST /Admin/CoachCoacheeMappingDelete
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachCoacheeMappingDelete(int id)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
            if (mapping == null)
                return Json(new { success = false, message = "Mapping tidak ditemukan." });
            if (mapping.IsActive)
                return Json(new { success = false, message = "Hanya mapping nonaktif yang dapat dihapus." });

            var coachUser = await _context.Users.FindAsync(mapping.CoachId);
            var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
            var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
            var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Json(new { success = false, message = "Sesi tidak valid." });

            var assignments = await _context.ProtonTrackAssignments
                .Where(a => a.CoacheeId == mapping.CoacheeId)
                .ToListAsync();

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => assignmentIds.Contains(p.ProtonTrackAssignmentId))
                .ToListAsync();

            var finalAssessments = await _context.ProtonFinalAssessments
                .Where(fa => assignmentIds.Contains(fa.ProtonTrackAssignmentId))
                .ToListAsync();

            int assignmentCount = assignments.Count;
            int progressCount = progresses.Count;

            _context.ProtonFinalAssessments.RemoveRange(finalAssessments);
            _context.ProtonDeliverableProgresses.RemoveRange(progresses);
            _context.ProtonTrackAssignments.RemoveRange(assignments);
            _context.CoachCoacheeMappings.Remove(mapping);

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(actor.Id, actor.FullName, "DeleteMapping",
                $"Hapus mapping: Coach {coachName} -> Coachee {coacheeName}, {assignmentCount} track assignments, {progressCount} progress records deleted",
                targetId: id, targetType: "CoachCoacheeMapping");

            return Json(new { success = true, message = "Mapping berhasil dihapus." });
        }

        // GET /Admin/CoachCoacheeMappingExport
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachCoacheeMappingExport()
        {
            var mappings = await _context.CoachCoacheeMappings
                .OrderBy(m => m.CoachId)
                .ThenBy(m => m.StartDate)
                .ToListAsync();

            var allUserIds = mappings.SelectMany(m => new[] { m.CoachId, m.CoacheeId }).Distinct().ToList();
            var allUsers = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => new {
                    u.Id,
                    FullName = u.FullName ?? "",
                    NIP = u.NIP ?? "",
                    Section = u.Section ?? "",
                    Position = u.Position ?? ""
                })
                .ToDictionaryAsync(u => u.Id);

            var coacheeIds = mappings.Select(m => m.CoacheeId).Distinct().ToList();
            var activeTrackAssignments = await _context.ProtonTrackAssignments
                .Include(a => a.ProtonTrack)
                .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
                .ToListAsync();
            var trackByCoachee = activeTrackAssignments
                .GroupBy(a => a.CoacheeId)
                .ToDictionary(g => g.Key, g => g.First().ProtonTrack?.DisplayName ?? "");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Coach-Coachee Mapping");

            // Header row
            var headers = new[] {
                "Coach Name", "Coach Section", "Coachee Name", "Coachee NIP",
                "Coachee Section", "Coachee Position", "Bagian Penugasan", "Unit Penugasan",
                "Current Track", "Status", "Start Date", "End Date"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.DarkGray;
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data rows
            int row = 2;
            foreach (var m in mappings)
            {
                var coach = allUsers.GetValueOrDefault(m.CoachId);
                var coachee = allUsers.GetValueOrDefault(m.CoacheeId);
                var track = trackByCoachee.GetValueOrDefault(m.CoacheeId, "");
                var status = m.IsActive ? "Active" : "Inactive";

                ws.Cell(row, 1).Value = coach?.FullName ?? m.CoachId;
                ws.Cell(row, 2).Value = coach?.Section ?? "";
                ws.Cell(row, 3).Value = coachee?.FullName ?? m.CoacheeId;
                ws.Cell(row, 4).Value = coachee?.NIP ?? "";
                ws.Cell(row, 5).Value = coachee?.Section ?? "";
                ws.Cell(row, 6).Value = coachee?.Position ?? "";
                ws.Cell(row, 7).Value = string.IsNullOrEmpty(m.AssignmentSection) ? "\u2014" : m.AssignmentSection;
                ws.Cell(row, 8).Value = string.IsNullOrEmpty(m.AssignmentUnit) ? "\u2014" : m.AssignmentUnit;
                ws.Cell(row, 9).Value = track;
                ws.Cell(row, 10).Value = status;
                ws.Cell(row, 11).Value = m.StartDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 12).Value = m.EndDate.HasValue ? m.EndDate.Value.ToString("yyyy-MM-dd") : "";
                row++;
            }

            return ExcelExportHelper.ToFileResult(workbook, "CoachCoacheeMapping.xlsx", this);
        }

        /// <summary>
        /// AJAX: Returns coachees eligible for a Proton exam — assigned to the track + 100% deliverables Approved.
        /// Called from CreateAssessment form JS when category=Assessment Proton and track is selected.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetEligibleCoachees(int protonTrackId)
        {
            if (protonTrackId <= 0) return Json(new List<object>());

            // Coachees with active assignment to this track
            var assignedCoacheeIds = await _context.ProtonTrackAssignments
                .Where(a => a.ProtonTrackId == protonTrackId && a.IsActive)
                .Select(a => a.CoacheeId)
                .Distinct()
                .ToListAsync();

            if (!assignedCoacheeIds.Any()) return Json(new List<object>());

            // All deliverable IDs for this track (via Kompetensi → SubKompetensi → Deliverable)
            var trackDeliverableIds = await _context.ProtonKompetensiList
                .Where(k => k.ProtonTrackId == protonTrackId)
                .SelectMany(k => k.SubKompetensiList)
                .SelectMany(s => s.Deliverables)
                .Select(d => d.Id)
                .ToListAsync();

            // Tahun 3 (interview) tracks have no deliverables — all assigned coachees are eligible
            if (!trackDeliverableIds.Any())
            {
                var allAssigned = await _context.Users
                    .Where(u => assignedCoacheeIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName, u.Email, u.NIP, u.Section })
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
                return Json(allAssigned);
            }

            // Batch-load progress records for all assigned coachees on this track's deliverables
            var progressRecords = await _context.ProtonDeliverableProgresses
                .Where(p => assignedCoacheeIds.Contains(p.CoacheeId)
                         && trackDeliverableIds.Contains(p.ProtonDeliverableId))
                .Select(p => new { p.CoacheeId, p.ProtonDeliverableId, p.Status })
                .ToListAsync();

            // Eligible = has exactly trackDeliverableIds.Count Approved progress records
            var eligibleCoacheeIds = assignedCoacheeIds
                .Where(id =>
                {
                    var mine = progressRecords.Where(p => p.CoacheeId == id).ToList();
                    return mine.Count == trackDeliverableIds.Count && mine.All(p => p.Status == "Approved");
                })
                .ToList();

            if (!eligibleCoacheeIds.Any()) return Json(new List<object>());

            var users = await _context.Users
                .Where(u => eligibleCoacheeIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email, u.NIP, u.Section })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Json(users);
        }

        #region Proton Progress Helpers

        private async Task<List<string>> AutoCreateProgressForAssignment(int assignmentId, int protonTrackId, string coacheeId)
        {
            var warnings = new List<string>();

            // Resolve unit: AssignmentUnit from active mapping, fallback to User.Unit
            var assignmentUnit = await _context.CoachCoacheeMappings
                .Where(m => m.CoacheeId == coacheeId && m.IsActive)
                .Select(m => m.AssignmentUnit)
                .FirstOrDefaultAsync();

            var resolvedUnit = assignmentUnit;
            if (string.IsNullOrWhiteSpace(resolvedUnit))
            {
                resolvedUnit = await _context.Users
                    .Where(u => u.Id == coacheeId)
                    .Select(u => u.Unit)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(resolvedUnit))
            {
                warnings.Add($"Coachee {coacheeId} tidak memiliki AssignmentUnit maupun Unit — progress tidak dibuat.");
                return warnings;
            }

            var deliverableIds = await _context.ProtonDeliverableList
                .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
                         && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())
                .Select(d => d.Id)
                .ToListAsync();

            if (!deliverableIds.Any())
            {
                var trackName = await _context.ProtonTracks
                    .Where(t => t.Id == protonTrackId)
                    .Select(t => t.DisplayName)
                    .FirstOrDefaultAsync() ?? protonTrackId.ToString();
                warnings.Add($"Tidak ada deliverable untuk unit {resolvedUnit} di track {trackName}.");
                return warnings;
            }

            var progresses = deliverableIds.Select(dId => new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = dId,
                ProtonTrackAssignmentId = assignmentId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.ProtonDeliverableProgresses.AddRange(progresses);
            await _context.SaveChangesAsync(); // flush to get IDs for StatusHistory

            // D-17: Insert initial "Pending" StatusHistory for each new progress
            foreach (var p in progresses)
            {
                _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
                {
                    ProtonDeliverableProgressId = p.Id,
                    StatusType = "Pending",
                    ActorId = "system",
                    ActorName = "System",
                    ActorRole = "System",
                    Timestamp = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            return warnings;
        }

        /// <summary>
        /// CRIT-02 fix: deletes DB rows (sessions, histories, progresses) for an assignment
        /// and RETURNS the list of evidence folder paths that should be deleted on disk
        /// AFTER the surrounding transaction commits successfully. The caller is responsible
        /// for performing the actual <see cref="Directory.Delete(string, bool)"/> post-commit,
        /// so a rollback never leaves DB rows pointing at missing files.
        /// </summary>
        private async Task<List<string>> CleanupProgressForAssignment(int assignmentId)
        {
            var foldersToDelete = new List<string>();

            var progressIds = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == assignmentId)
                .Select(p => p.Id)
                .ToListAsync();

            if (!progressIds.Any()) return foldersToDelete;

            var histories = await _context.DeliverableStatusHistories
                .Where(h => progressIds.Contains(h.ProtonDeliverableProgressId))
                .ToListAsync();
            _context.DeliverableStatusHistories.RemoveRange(histories);

            var sessions = await _context.CoachingSessions
                .Where(s => s.ProtonDeliverableProgressId.HasValue && progressIds.Contains(s.ProtonDeliverableProgressId.Value))
                .ToListAsync();
            _context.CoachingSessions.RemoveRange(sessions);

            var progresses = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonTrackAssignmentId == assignmentId)
                .ToListAsync();
            _context.ProtonDeliverableProgresses.RemoveRange(progresses);

            foreach (var pid in progressIds)
            {
                foldersToDelete.Add(Path.Combine(_env.WebRootPath, "uploads", "evidence", pid.ToString()));
            }

            return foldersToDelete;
        }

        #endregion

        #region Coach Workload

        private record CoachWorkloadRow(string CoachId, string CoachName, string CoachSection, int CoacheeCount, string Status);
        private record ReassignSuggestion(int MappingId, string CoacheeName, string CoacheeSection, string FromCoachName, string ToCoachId, string ToCoachName);

        private async Task<(List<CoachWorkloadRow> Rows, CoachWorkloadThreshold Threshold, int TotalCoachees, List<string> Sections)> GetWorkloadDataAsync(string? section)
        {
            var threshold = await _context.CoachWorkloadThresholds.FirstOrDefaultAsync()
                ?? new CoachWorkloadThreshold { MaxCoacheesPerCoach = 5, WarningThreshold = 4 };

            var allUsers = await _context.Users
                .Select(u => new { u.Id, u.FullName, u.Section, u.IsActive })
                .ToListAsync();
            var userDict = allUsers.ToDictionary(u => u.Id);

            var activeMappings = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive && !m.IsCompleted)
                .ToListAsync();

            var coachRoleUsers = await _userManager.GetUsersInRoleAsync("Coach");
            var activeCoachIds = coachRoleUsers.Where(u => u.IsActive).Select(u => u.Id).ToHashSet();

            var mappingsByCoach = activeMappings.GroupBy(m => m.CoachId)
                .ToDictionary(g => g.Key, g => g.Count());

            var rows = new List<CoachWorkloadRow>();
            foreach (var coachId in activeCoachIds)
            {
                var count = mappingsByCoach.GetValueOrDefault(coachId, 0);
                var user = userDict.GetValueOrDefault(coachId);
                if (user == null || !user.IsActive) continue;

                var status = count >= threshold.MaxCoacheesPerCoach ? "Overloaded"
                    : count >= threshold.WarningThreshold ? "Warning"
                    : "OK";

                rows.Add(new CoachWorkloadRow(coachId, user.FullName ?? coachId, user.Section ?? "", count, status));
            }

            if (!string.IsNullOrEmpty(section))
                rows = rows.Where(r => r.CoachSection == section).ToList();

            rows = rows.OrderByDescending(r => r.CoacheeCount).ToList();

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            var sections = sectionUnitsDict.Keys.ToList();

            var totalCoachees = rows.Sum(r => r.CoacheeCount);

            return (rows, threshold, totalCoachees, sections);
        }

        private List<ReassignSuggestion> GenerateReassignSuggestions(
            List<CoachWorkloadRow> rows, List<CoachCoacheeMapping> activeMappings,
            Dictionary<string, (string FullName, string Section)> userDict, CoachWorkloadThreshold threshold)
        {
            var suggestions = new List<ReassignSuggestion>();
            var overloaded = rows.Where(r => r.CoacheeCount > threshold.MaxCoacheesPerCoach).ToList();
            var underloaded = rows.Where(r => r.CoacheeCount < threshold.MaxCoacheesPerCoach)
                .OrderBy(r => r.CoacheeCount).ToList();

            if (!underloaded.Any()) return suggestions;

            foreach (var coach in overloaded)
            {
                var coacheeMappings = activeMappings
                    .Where(m => m.CoachId == coach.CoachId)
                    .ToList();

                foreach (var mapping in coacheeMappings)
                {
                    if (suggestions.Count >= 20) return suggestions;

                    var coacheeInfo = userDict.GetValueOrDefault(mapping.CoacheeId);
                    var coacheeName = coacheeInfo.FullName ?? mapping.CoacheeId;
                    var coacheeSection = coacheeInfo.Section ?? "";

                    // Prefer same-section target
                    var target = underloaded.FirstOrDefault(r => r.CoachSection == coacheeSection)
                        ?? underloaded.FirstOrDefault();

                    if (target == null) break;

                    suggestions.Add(new ReassignSuggestion(
                        mapping.Id, coacheeName, coacheeSection,
                        coach.CoachName, target.CoachId, target.CoachName));
                }
            }

            return suggestions;
        }

        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CoachWorkload(string? section)
        {
            var (rows, threshold, totalCoachees, sections) = await GetWorkloadDataAsync(section);

            var activeMappings = await _context.CoachCoacheeMappings
                .Where(m => m.IsActive && !m.IsCompleted)
                .ToListAsync();

            var allUsers = await _context.Users
                .Select(u => new { u.Id, u.FullName, u.Section })
                .ToListAsync();
            var userDict = allUsers.ToDictionary(u => u.Id, u => (FullName: u.FullName ?? u.Id, Section: u.Section ?? ""));

            ViewBag.TotalActiveCoaches = rows.Count;
            ViewBag.TotalActiveCoachees = totalCoachees;
            ViewBag.AvgRatio = rows.Count > 0 ? Math.Round((double)totalCoachees / rows.Count, 1) : 0.0;
            ViewBag.OverloadedCount = rows.Count(r => r.Status == "Overloaded");
            ViewBag.Threshold = threshold;
            ViewBag.WorkloadRows = rows;
            ViewBag.Sections = sections;
            ViewBag.SectionFilter = section;
            ViewBag.ReassignSuggestions = GenerateReassignSuggestions(rows, activeMappings, userDict, threshold);

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetWorkloadThreshold(int maxCoachees, int warningThreshold)
        {
            if (maxCoachees < 1 || warningThreshold < 1 || warningThreshold > maxCoachees)
                return Json(new { success = false, message = "Nilai threshold tidak valid." });

            var user = await _userManager.GetUserAsync(User);
            var row = await _context.CoachWorkloadThresholds.FirstOrDefaultAsync();
            if (row == null)
            {
                row = new CoachWorkloadThreshold();
                _context.CoachWorkloadThresholds.Add(row);
            }

            row.MaxCoacheesPerCoach = maxCoachees;
            row.WarningThreshold = warningThreshold;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedById = user?.Id ?? "system";

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user?.Id ?? "system",
                ActorName = user?.FullName ?? "system",
                ActionType = "SetWorkloadThreshold",
                Description = $"Set threshold: Max={maxCoachees}, Warning={warningThreshold}",
                TargetType = "CoachWorkloadThreshold",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReassignSuggestion(int mappingId, string newCoachId)
        {
            var mapping = await _context.CoachCoacheeMappings.FindAsync(mappingId);
            if (mapping == null) return NotFound();
            if (!mapping.IsActive || mapping.IsCompleted)
                return Json(new { success = false, message = "Mapping sudah tidak aktif atau sudah selesai." });

            var oldCoachId = mapping.CoachId;
            mapping.CoachId = newCoachId;

            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user?.Id ?? "system",
                ActorName = user?.FullName ?? "system",
                ActionType = "ApproveReassignSuggestion",
                Description = $"Reassign mapping {mappingId} from coach {oldCoachId} to {newCoachId}",
                TargetType = "CoachCoacheeMapping",
                TargetId = mappingId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SkipReassignSuggestion(int mappingId)
        {
            return Json(new { success = true });
        }

        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ExportCoachWorkload(string? section)
        {
            var (rows, threshold, totalCoachees, sections) = await GetWorkloadDataAsync(section);

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Coach Workload",
                new[] { "Nama Coach", "Section", "Jumlah Coachee", "Status" });

            int rowNum = 2;
            foreach (var r in rows)
            {
                ws.Cell(rowNum, 1).Value = r.CoachName;
                ws.Cell(rowNum, 2).Value = r.CoachSection;
                ws.Cell(rowNum, 3).Value = r.CoacheeCount;
                ws.Cell(rowNum, 4).Value = r.Status;
                rowNum++;
            }

            return ExcelExportHelper.ToFileResult(workbook, "coach_workload.xlsx", this);
        }

        #endregion
    }
}

public class CoachAssignRequest
{
    public string CoachId { get; set; } = "";
    public List<string> CoacheeIds { get; set; } = new();
    public int? ProtonTrackId { get; set; }
    public DateTime? StartDate { get; set; }
    public string? AssignmentSection { get; set; }
    public string? AssignmentUnit { get; set; }
    /// <summary>D-09: If true, user confirmed to proceed despite incomplete progression warning.</summary>
    public bool ConfirmProgressionWarning { get; set; }
}

public class CoachEditRequest
{
    public int MappingId { get; set; }
    public string CoachId { get; set; } = "";
    public int? ProtonTrackId { get; set; }
    public DateTime? StartDate { get; set; }
    public string? AssignmentSection { get; set; }
    public string? AssignmentUnit { get; set; }
}
