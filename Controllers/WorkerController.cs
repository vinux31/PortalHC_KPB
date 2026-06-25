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

        // ============================================================
        // Phase 399 (MU-01/02/04/05/07) — multi-unit write-through helpers (single-source, testable)
        // ============================================================

        /// <summary>Phase 399 (MU-03) — proyeksi unit pekerja untuk display list (semua unit primary-first + primary).</summary>
        public record WorkerUnitsView(List<string> Units, string? PrimaryUnit);

        /// <summary>
        /// Phase 399 (MU-04) — parse 1 sel Unit Excel/import jadi daftar unit.
        /// Pipe-delimited "UnitA|UnitB|UnitC" → split('|') + trim + dedup (OrdinalIgnoreCase); first=primary (D-04).
        /// Backward-compat: "UnitA" (tanpa pipe) → ["UnitA"] (D-05). Empty/"|"/whitespace → [].
        /// </summary>
        public static List<string> ParseUnitCell(string? cell)
        {
            return (cell ?? "")
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Phase 399 (MU-05) — validasi server-side: tiap unit ∈ unit-Bagian + primary ∈ checked-set.
        /// Returns daftar pesan error (kosong = valid). JANGAN trust client checkbox-list (mass-assignment guard).
        /// </summary>
        public static List<string> ValidateUnitsInSection(
            List<string> validUnits, List<string> units, string? primaryUnit, string? sectionName)
        {
            var errors = new List<string>();
            var invalid = (units ?? new()).Where(u => !validUnits.Contains(u)).ToList();
            if (invalid.Any())
                errors.Add($"Unit tidak valid untuk '{sectionName}': {string.Join(", ", invalid)}");
            // primary harus ∈ checked-set (server-side; jangan percaya client)
            if ((units?.Any() ?? false) && !string.IsNullOrEmpty(primaryUnit) && !units.Contains(primaryUnit))
                errors.Add("Pilih salah satu Unit sebagai Unit Utama.");
            return errors;
        }

        /// <summary>
        /// Phase 399 (MU-01/02) — WRITE-THROUGH terpusat: replace-set baris UserUnits + mirror ApplicationUser.Unit.
        /// Strategy replace-set: RemoveRange lama + Add baru dalam 1 SaveChanges → EF emit DELETE sebelum INSERT
        /// (tidak ada window 2-primary vs filtered-unique). Helper TIDAK SaveChanges — caller yang commit.
        /// Mengembalikan set-diff (D-12) untuk audit: "Unit +'X'" / "Unit -'Y'" / "Primary: 'A' → 'B'".
        /// </summary>
        public static async Task<List<string>> SyncUserUnitsAsync(
            ApplicationDbContext context, ApplicationUser user, List<string> units, string? primaryUnit)
        {
            var changes = new List<string>();
            var existing = await context.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
            var oldSet = existing.Select(e => e.Unit).ToHashSet();
            var cleaned = (units ?? new()).Distinct().ToList();
            var newSet = cleaned.ToHashSet();

            foreach (var added in newSet.Except(oldSet)) changes.Add($"Unit +'{added}'");
            foreach (var removed in oldSet.Except(newSet)) changes.Add($"Unit -'{removed}'");

            // D-02 deterministik: primary = arg bila valid (∈ set), else unit tercentang pertama; null bila 0 unit.
            var primary = (primaryUnit != null && newSet.Contains(primaryUnit))
                ? primaryUnit : cleaned.FirstOrDefault();
            var oldPrimary = existing.FirstOrDefault(e => e.IsPrimary)?.Unit;
            if (oldPrimary != primary) changes.Add($"Primary: '{oldPrimary}' → '{primary}'");

            context.UserUnits.RemoveRange(existing);
            foreach (var u in newSet)
                context.UserUnits.Add(new UserUnit
                {
                    UserId = user.Id,
                    Unit = u,
                    IsPrimary = (u == primary),
                    IsActive = true
                });

            user.Unit = primary;   // MIRROR (invariant #3); null bila 0 unit
            return changes;
        }

        /// <summary>Hasil evaluasi guard hapus-unit (MU-07).</summary>
        public enum RemoveUnitOutcome { Allowed, Blocked, NeedConfirm, Deactivated }

        public record RemoveUnitGuardResult(RemoveUnitOutcome Outcome, string? Message, CoachCoacheeMapping? MappingToDeactivate);

        /// <summary>
        /// Phase 399 (MU-07) — guard hapus-unit ASIMETRIS (D-10/D-11), dievaluasi sebelum SyncUserUnitsAsync.
        /// - PTA aktif + unit-PROTON-teresolusi ∈ removed → HARD-BLOCK (D-11). Resolusi unit PROTON (Pitfall 4 — PTA
        ///   tak punya kolom Unit): activeMapping.AssignmentUnit ?? oldPrimary.
        /// - Mapping coach aktif (AssignmentUnit ∈ removed) tanpa PTA terkait → confirm→auto-deactivate (D-10):
        ///   ConfirmedDeactivate=false → NeedConfirm (re-prompt); true → Deactivated (caller set IsActive/EndDate).
        /// Caller TIDAK mutasi DB di sini (kecuali set IsActive saat Deactivated, dalam tx-nya).
        /// </summary>
        public static async Task<RemoveUnitGuardResult> EvaluateRemoveUnitGuardAsync(
            ApplicationDbContext context, string userId, string? oldPrimary,
            List<string> removed, bool confirmedDeactivate)
        {
            if (removed == null || !removed.Any())
                return new RemoveUnitGuardResult(RemoveUnitOutcome.Allowed, null, null);

            var activeMapping = await context.CoachCoacheeMappings
                .FirstOrDefaultAsync(m => m.CoacheeId == userId && m.IsActive);
            var hasActivePta = await context.ProtonTrackAssignments
                .AnyAsync(a => a.CoacheeId == userId && a.IsActive);

            // Resolusi unit PROTON (Pitfall 4): AssignmentUnit ?? oldPrimary
            var protonUnit = activeMapping?.AssignmentUnit ?? oldPrimary;

            // HARD-BLOCK (D-11) — Open Q1: (a) AssignmentUnit∈removed; ATAU (b) AssignmentUnit==null && oldPrimary∈removed.
            // Juga menutup Open Q2 (kosongkan SEMUA unit saat PTA aktif → protonUnit pasti ∈ removed → block).
            if (hasActivePta && protonUnit != null && removed.Contains(protonUnit))
            {
                return new RemoveUnitGuardResult(RemoveUnitOutcome.Blocked,
                    $"Tidak bisa menghapus Unit '{protonUnit}': masih ada PROTON tahun-berjalan aktif. " +
                    "Tutup atau bypass PROTON terlebih dahulu melalui halaman PROTON.", null);
            }

            // AUTO-DEACTIVATE-after-confirm (D-10) — mapping coach aktif AssignmentUnit∈removed, tanpa PTA terkait.
            if (activeMapping?.AssignmentUnit != null && removed.Contains(activeMapping.AssignmentUnit))
            {
                if (!confirmedDeactivate)
                    return new RemoveUnitGuardResult(RemoveUnitOutcome.NeedConfirm,
                        $"Mapping coach aktif unit '{activeMapping.AssignmentUnit}' akan dinonaktifkan", activeMapping);

                return new RemoveUnitGuardResult(RemoveUnitOutcome.Deactivated,
                    $"Mapping coach unit '{activeMapping.AssignmentUnit}' dinonaktifkan (hapus unit)", activeMapping);
            }

            return new RemoveUnitGuardResult(RemoveUnitOutcome.Allowed, null, null);
        }

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
            // Phase 400 (MU-06 D-01/D-03/D-06): predikat SET-AWARE active-only (display badge TIDAK diubah).
            // Correlated subquery → SQL EXISTS; PITFALL #1 (_context.UserUnits, bukan nav prop):
            // lihat rujukan kanonik di WorkerDataService.cs GetWorkersInSection.
            if (!string.IsNullOrEmpty(unitFilter))
            {
                query = query.Where(u =>
                    _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive));
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

            // Phase 399 (MU-03): batch-load UserUnits per pekerja (hindari N+1) → ViewBag dict utk display semua unit
            // (primary-first). Plan 04 Task 2 BACA dict ini di view (no edit WorkerController). Filter unitFilter
            // TETAP scalar di Phase 399 (set-aware = Phase 400 MU-06).
            var listUserIds = users.Select(u => u.Id).ToList();
            // IN-01: filter && uu.IsActive supaya badge "semua unit" hanya menampilkan unit aktif,
            // konsisten dengan GetWorkersInSection (unitsByUser) dan predikat filter unit aktif.
            var userUnitsDict = (await _context.UserUnits
                    .Where(uu => listUserIds.Contains(uu.UserId) && uu.IsActive)
                    .ToListAsync())
                .GroupBy(uu => uu.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => new WorkerUnitsView(
                        g.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Unit).Select(x => x.Unit).ToList(),
                        g.FirstOrDefault(x => x.IsPrimary)?.Unit));
            ViewBag.UserUnitsDict = userUnitsDict;

            // Get roles for all users in single query (avoid N+1)
            var userRolesDict = (await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id,
                      (ur, r) => new { ur.UserId, r.Name })
                .ToListAsync())
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First().Name ?? "No Role");
            // Fill missing users
            foreach (var u in users)
                if (!userRolesDict.ContainsKey(u.Id))
                    userRolesDict[u.Id] = "No Role";
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

            // Phase 400 (MU-06 D-01/D-03): predikat unit SET-AWARE active-only (correlated subquery → SQL EXISTS).
            // PITFALL #1 (_context.UserUnits, bukan nav prop): lihat rujukan kanonik di WorkerDataService.cs.
            if (!string.IsNullOrEmpty(unitFilter))
                query = query.Where(u =>
                    _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive));

            if (!string.IsNullOrEmpty(roleFilter))
            {
                var roleLevel = UserRoles.GetRoleLevel(roleFilter);
                query = query.Where(u => u.RoleLevel == roleLevel);
            }

            if (!showInactive)
                query = query.Where(u => u.IsActive);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            // Phase 399 (MU-03, D-08): batch-load UserUnits (hindari N+1) → kolom Unit = semua unit primary-first comma-join.
            var exportUserIds = users.Select(u => u.Id).ToList();
            var exportUnitsByUser = (await _context.UserUnits
                    .Where(uu => exportUserIds.Contains(uu.UserId) && uu.IsActive)  // WR-02: active-only, konsisten dgn predikat filter + GetWorkersInSection
                    .ToListAsync())
                .GroupBy(uu => uu.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

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

                // Multi-unit primary-first comma-join (ordering satu-satunya sinyal primary di Excel — D-08).
                exportUnitsByUser.TryGetValue(u.Id, out var uUnits);
                string unitsText;
                if (uUnits != null && uUnits.Any())
                {
                    var primaryUnit = uUnits.FirstOrDefault(x => x.IsPrimary)?.Unit;
                    unitsText = string.Join(", ", uUnits
                        .OrderByDescending(x => x.Unit == primaryUnit)
                        .ThenBy(x => x.Unit)
                        .Select(x => x.Unit));
                }
                else
                {
                    unitsText = u.Unit ?? "-";   // fallback mirror (pekerja tanpa baris UserUnits)
                }
                ws.Cell(i + 2, 7).Value = unitsText;
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
            // Phase 399 (MU-05): validasi multi-unit server-side (jangan trust client checkbox-list).
            if (!string.IsNullOrEmpty(model.Section))
            {
                var validSections = await _context.GetAllSectionsAsync();
                if (!validSections.Contains(model.Section))
                {
                    ModelState.AddModelError("Section", $"Bagian '{model.Section}' tidak ditemukan di data organisasi");
                }
                else if (model.Units != null && model.Units.Any())
                {
                    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
                    foreach (var err in ValidateUnitsInSection(validUnits, model.Units, model.PrimaryUnit, model.Section))
                        ModelState.AddModelError("Units", err);
                }
            }
            else if (model.Units != null && model.Units.Any())
            {
                ModelState.AddModelError("Units", "Unit tidak boleh diisi tanpa Bagian (Section).");
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
                // Unit (mirror) di-set via SyncUserUnitsAsync setelah CreateAsync (write-through)
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

                // Phase 399 (MU-01/02, WR-01 atomicity): bungkus write-through junction + mirror + UpdateAsync
                // dalam SATU transaksi → baris UserUnits & mirror ApplicationUser.Unit commit bersama (no desync,
                // konsisten dengan EditWorker yang sudah hardened — jaga Invariant #3).
                using (var uuTx = await _context.Database.BeginTransactionAsync())
                {
                    await SyncUserUnitsAsync(_context, user, model.Units ?? new(), model.PrimaryUnit);
                    await _context.SaveChangesAsync();
                    await _userManager.UpdateAsync(user);   // persist mirror Unit ke Identity store
                    await uuTx.CommitAsync();
                }

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

            // Phase 399 (MU-01/02) — pre-fill multi-unit dari junction supaya widget pre-check boxes + primary radio (round-trip).
            var userUnits = await _context.UserUnits
                .Where(uu => uu.UserId == user.Id)
                .ToListAsync();
            model.Units = userUnits.Select(uu => uu.Unit).ToList();
            model.PrimaryUnit = userUnits.FirstOrDefault(uu => uu.IsPrimary)?.Unit;

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
            // Phase 399 (MU-05): validasi multi-unit server-side (jangan trust client checkbox-list).
            if (!string.IsNullOrEmpty(model.Section))
            {
                var validSections = await _context.GetAllSectionsAsync();
                if (!validSections.Contains(model.Section))
                {
                    ModelState.AddModelError("Section", $"Bagian '{model.Section}' tidak ditemukan di data organisasi");
                }
                else if (model.Units != null && model.Units.Any())
                {
                    var validUnits = await _context.GetUnitsForSectionAsync(model.Section);
                    foreach (var err in ValidateUnitsInSection(validUnits, model.Units, model.PrimaryUnit, model.Section))
                        ModelState.AddModelError("Units", err);
                }
            }
            else if (model.Units != null && model.Units.Any())
            {
                ModelState.AddModelError("Units", "Unit tidak boleh diisi tanpa Bagian (Section).");
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
            // Phase 399 (D-12): unit di-audit via set-diff dari SyncUserUnitsAsync (BUKAN scalar if user.Unit != model.Unit).
            var changes = new List<string>();
            if (user.FullName != model.FullName) changes.Add($"Name: '{user.FullName}' → '{model.FullName}'");
            if (user.NIP != model.NIP) changes.Add($"NIP: '{user.NIP}' → '{model.NIP}'");
            if (user.Position != model.Position) changes.Add($"Position: '{user.Position}' → '{model.Position}'");
            if (user.Section != model.Section) changes.Add($"Section: '{user.Section}' → '{model.Section}'");

            // Phase 399 (MU-07): guard hapus-unit SEBELUM mutasi (PTA aktif → hard-block; mapping aktif → confirm→deactivate).
            var oldUnits = await _context.UserUnits.Where(uu => uu.UserId == user.Id).Select(uu => uu.Unit).ToListAsync();
            var oldPrimary = (await _context.UserUnits.FirstOrDefaultAsync(uu => uu.UserId == user.Id && uu.IsPrimary))?.Unit;
            var removedUnits = oldUnits.Except(model.Units ?? new()).ToList();
            CoachCoacheeMapping? mappingToDeactivate = null;
            if (removedUnits.Any())
            {
                var guard = await EvaluateRemoveUnitGuardAsync(
                    _context, user.Id, oldPrimary, removedUnits, model.ConfirmedDeactivate);
                if (guard.Outcome == RemoveUnitOutcome.Blocked)
                {
                    ModelState.AddModelError("", guard.Message!);
                    var sectionUnitsDictBlk = await _context.GetSectionUnitsDictAsync();
                    ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictBlk);
                    return View(model);
                }
                if (guard.Outcome == RemoveUnitOutcome.NeedConfirm)
                {
                    model.ImpactedMappings = new() { guard.Message! };
                    ViewBag.NeedConfirm = true;
                    var sectionUnitsDictCfm = await _context.GetSectionUnitsDictAsync();
                    ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictCfm);
                    return View(model);
                }
                if (guard.Outcome == RemoveUnitOutcome.Deactivated)
                    mappingToDeactivate = guard.MappingToDeactivate;
            }

            // Update fields (Unit/mirror di-set via SyncUserUnitsAsync dalam tx di bawah)
            user.FullName = model.FullName;
            user.NIP = model.NIP;
            user.Position = model.Position;
            user.Section = model.Section;
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

            // Phase 399 (MU-01/02, Open Q3 atomicity): bungkus write-through junction + mirror + UpdateAsync +
            // (MU-07 auto-deactivate bila confirmed) dalam SATU transaksi → mirror Unit & baris UserUnits commit
            // bersama (no desync). SyncUserUnitsAsync set user.Unit SEBELUM UpdateAsync persist mirror.
            using var uuTx = await _context.Database.BeginTransactionAsync();
            var unitDiff = await SyncUserUnitsAsync(_context, user, model.Units ?? new(), model.PrimaryUnit);
            changes.AddRange(unitDiff);

            if (mappingToDeactivate != null)
            {
                // confirmed → reuse pola CoachCoacheeMappingDeactivate (IsActive=false + EndDate) DALAM tx yang sama (D-10)
                mappingToDeactivate.IsActive = false;
                mappingToDeactivate.EndDate = DateTime.UtcNow;
                changes.Add($"Mapping coach unit '{mappingToDeactivate.AssignmentUnit}' dinonaktifkan (hapus unit)");
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

            await _context.SaveChangesAsync();
            await uuTx.CommitAsync();

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

            // Phase 335 D-02: pre-check cross-user renewal SEBELUM tx scope (early return TempData friendly)
            var userTrainingIds = await _context.TrainingRecords
                .Where(t => t.UserId == id)
                .Select(t => t.Id)
                .ToListAsync();
            var userAssessmentIds = await _context.AssessmentSessions
                .Where(a => a.UserId == id)
                .Select(a => a.Id)
                .ToListAsync();

            var crossUserTrReferences = 0;
            var crossUserAsReferences = 0;
            if (userTrainingIds.Any() || userAssessmentIds.Any())
            {
                crossUserTrReferences = await _context.TrainingRecords
                    .CountAsync(t => t.UserId != id && (
                        (t.RenewsTrainingId.HasValue && userTrainingIds.Contains(t.RenewsTrainingId.Value)) ||
                        (t.RenewsSessionId.HasValue && userAssessmentIds.Contains(t.RenewsSessionId.Value))
                    ));
                crossUserAsReferences = await _context.AssessmentSessions
                    .CountAsync(a => a.UserId != id && (
                        (a.RenewsTrainingId.HasValue && userTrainingIds.Contains(a.RenewsTrainingId.Value)) ||
                        (a.RenewsSessionId.HasValue && userAssessmentIds.Contains(a.RenewsSessionId.Value))
                    ));
            }

            var totalCrossRefs = crossUserTrReferences + crossUserAsReferences;
            if (totalCrossRefs > 0)
            {
                TempData["Error"] = $"Tidak bisa hapus pekerja '{userName}': {totalCrossRefs} sertifikat milik pekerja lain menggunakan sertifikat pekerja ini sebagai sumber renewal. Hapus atau update sertifikat pemakai terlebih dulu.";
                return RedirectToAction("ManageWorkers");
            }

            // Phase 335 D-03: collect file paths SEBELUM tx + cascade (TR + AS will detach via UserManager.DeleteAsync cascade)
            var userTrainingRecords = await _context.TrainingRecords
                .Where(t => t.UserId == id)
                .ToListAsync();
            var userAssessmentSessions = await _context.AssessmentSessions
                .Where(a => a.UserId == id)
                .ToListAsync();

            var allFilePaths = new List<string>();
            foreach (var tr in userTrainingRecords)
            {
                if (!string.IsNullOrEmpty(tr.SertifikatUrl)) allFilePaths.Add(tr.SertifikatUrl);
            }
            foreach (var asSession in userAssessmentSessions)
            {
                if (!string.IsNullOrEmpty(asSession.ManualSertifikatUrl)) allFilePaths.Add(asSession.ManualSertifikatUrl);
            }

            // Phase 335 D-04: tx wrap 9-step RemoveRange + SaveChanges + UserManager.DeleteAsync + audit log + CommitAsync
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Delete related data that uses Restrict delete behavior
                // UserResponses (Restrict on AssessmentSession)
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
                // Phase 335 D-03: collect EvidencePath + JSON history SEBELUM RemoveRange
                foreach (var p in protonProgress)
                {
                    if (!string.IsNullOrEmpty(p.EvidencePath)) allFilePaths.Add(p.EvidencePath);
                    if (!string.IsNullOrEmpty(p.EvidencePathHistory))
                    {
                        try
                        {
                            var history = System.Text.Json.JsonSerializer
                                .Deserialize<List<string>>(p.EvidencePathHistory) ?? new List<string>();
                            allFilePaths.AddRange(history);
                        }
                        catch (Exception jex)
                        {
                            _logger.LogWarning(jex, "Failed to parse EvidencePathHistory for progress {Pid} (DeleteWorker)", p.Id);
                        }
                    }
                }
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

                // UserManager.DeleteAsync INSIDE tx — uses same ApplicationDbContext via AddEntityFrameworkStores (Program.cs L47)
                // Cascade akan handle TrainingRecords, AssessmentSessions, IdpItems
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    // Phase 335 D-04: Identity error — early return INSIDE try, tx disposal auto-rollback
                    TempData["Error"] = $"Gagal menghapus user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    return RedirectToAction("ManageWorkers");
                }

                // Audit log INSIDE tx
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

                await tx.CommitAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Phase 335 D-06: using var disposal auto-rollback
                _logger.LogWarning(dbEx, "DbUpdate failed for DeleteWorker {Id}", id);
                TempData["Error"] = "Gagal hapus pekerja: ada constraint database yang dilanggar. Pastikan tidak ada data dependen yang belum dibersihkan.";
                return RedirectToAction("ManageWorkers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete worker {Id}", id);
                TempData["Error"] = "Gagal hapus pekerja: terjadi kesalahan internal. Hubungi admin.";
                return RedirectToAction("ManageWorkers");
            }

            // Phase 335 D-05: File.Delete loop POST CommitAsync — inner try/catch warn-only per file
            foreach (var relUrl in allFilePaths)
            {
                try
                {
                    var physical = Path.Combine(_env.WebRootPath,
                        relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                }
                catch (Exception fex)
                {
                    _logger.LogWarning(fex, "File.Delete post-commit failed (Worker file): {Path}", relUrl);
                }
            }

            TempData["Success"] = $"User '{userName}' berhasil dihapus dari sistem.";
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

            // MU-03 (Plan 04): muat SEMUA unit pekerja untuk display (primary-first). Read-only view-binding.
            var detailUnits = await _context.UserUnits
                .Where(uu => uu.UserId == user.Id)
                .ToListAsync();
            ViewBag.WorkerPrimaryUnit = detailUnits.FirstOrDefault(x => x.IsPrimary)?.Unit;
            ViewBag.WorkerUnits = detailUnits.Select(x => x.Unit).ToList();

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
                // Phase 399 (D-06): contoh Unit ganda pipe-delimited (unit pertama = Utama)
                "RFCC", "RFCC LPG Treating Unit (062)|RFCC Gas Concentration Unit (063)", "CSU Process", "Coachee", "2024-01-15"
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
            // Phase 399 (D-06): help-text format Unit ganda (pipe-delimited, unit pertama = primary).
            ws.Cell(5, 1).Value = "Kolom Unit: Untuk Unit ganda, pisahkan dengan tanda pipa | (contoh: UnitA|UnitB|UnitC). Unit pertama menjadi Unit Utama. Satu Unit tetap valid tanpa pipa.";
            ws.Cell(5, 1).Style.Font.Italic = true;
            ws.Cell(5, 1).Style.Font.FontColor = XLColor.DarkRed;
            if (useAD)
            {
                ws.Cell(6, 1).Value = "Mode AD aktif: Kolom Password tidak diperlukan. Sistem akan membuat password acak.";
                ws.Cell(6, 1).Style.Font.Italic = true;
                ws.Cell(6, 1).Style.Font.FontColor = XLColor.DarkBlue;
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
                if (!workbook.Worksheets.Any())
                {
                    TempData["Error"] = "File Excel tidak memiliki worksheet.";
                    return View();
                }
                var ws = workbook.Worksheets.First();

                var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var nama = (row.Cell(1).GetString() ?? "").Trim();
                    var email = (row.Cell(2).GetString() ?? "").Trim();
                    var nip = (row.Cell(3).GetString() ?? "").Trim();
                    var jabatan = (row.Cell(4).GetString() ?? "").Trim();
                    var bagian = (row.Cell(5).GetString() ?? "").Trim();
                    // Phase 399 (MU-04, D-04/05): Cell(6) posisi TETAP — parse pipe "UnitA|UnitB" (first=primary, dedup).
                    var unitCellRaw = (row.Cell(6).GetString() ?? "").Trim();   // posisi Cell(6) TIDAK bergeser
                    var unitList = ParseUnitCell(unitCellRaw);
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

                    // Validasi Unit (MU-05): tiap unit dalam pipe-list harus child dari Section yang dipilih.
                    if (unitList.Any())
                    {
                        if (string.IsNullOrWhiteSpace(bagian))
                            errors.Add("Unit tidak boleh diisi tanpa Section");
                        else if (sectionUnitsDict.TryGetValue(bagian, out var validUnits))
                        {
                            var invalid = unitList.Where(u => !validUnits.Contains(u)).ToList();   // validasi PER unit
                            if (invalid.Any())
                                errors.Add($"Unit tidak valid untuk '{bagian}': {string.Join(", ", invalid)}");
                        }
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
                        // Unit (mirror) di-set via SyncUserUnitsAsync setelah CreateAsync (write-through MU-04)
                        Directorate = string.IsNullOrWhiteSpace(directorate) ? null : directorate,
                        JoinDate = joinDate,
                        RoleLevel = roleLevel,
                        SelectedView = selectedView
                    };

                    var createResult = await _userManager.CreateAsync(newUser, password);
                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, role);

                        // Phase 399 (MU-04, WR-01 atomicity): write-through junction + mirror + UpdateAsync per-row
                        // dalam SATU transaksi → baris UserUnits & mirror Unit commit bersama (no desync, jaga Invariant #3).
                        using (var uuTx = await _context.Database.BeginTransactionAsync())
                        {
                            await SyncUserUnitsAsync(_context, newUser, unitList, unitList.FirstOrDefault());
                            await _context.SaveChangesAsync();
                            await _userManager.UpdateAsync(newUser);   // persist mirror Unit
                            await uuTx.CommitAsync();
                        }

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

        // Helper: Crypto-random 16-char password for AD mode auto-generation.
        // IN-03 — Asumsi PasswordOptions (lihat Program.cs AddIdentity):
        //   RequireDigit=false, RequireLowercase=false, RequireUppercase=false,
        //   RequireNonAlphanumeric=false, RequiredLength=6.
        // Base64 dari 12 byte = 16 karakter [A-Za-z0-9+/=] ⇒ selalu ≥ panjang minimum & TIDAK terikat
        // syarat komposisi karakter apa pun, sehingga password ini DIJAMIN lolos validasi Identity.
        // ⚠️ Jika Program.cs kelak mengetatkan PasswordOptions (mis. RequireNonAlphanumeric=true atau
        //    mensyaratkan kelas karakter spesifik), generator ini harus ditinjau ulang — Base64 tidak
        //    menjamin kehadiran tiap kelas karakter secara deterministik.
        private static string GenerateRandomPassword()
        {
            var bytes = new byte[12];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            // Base64 menghasilkan campuran huruf besar/kecil + digit, tanpa karakter yang memecah validasi Identity.
            return Convert.ToBase64String(bytes);
        }
    }
}
