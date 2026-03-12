using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    public class SilabusRowDto
    {
        public int KompetensiId { get; set; }     // 0 = new kompetensi
        public string Kompetensi { get; set; } = "";
        public int SubKompetensiId { get; set; }  // 0 = new subkompetensi
        public string SubKompetensi { get; set; } = "";
        public int DeliverableId { get; set; }    // 0 = new deliverable
        public string Deliverable { get; set; } = "";
        public string No { get; set; } = "";      // Manual row number (text)
        public string Target { get; set; } = "";
        public string Bagian { get; set; } = "";
        public string Unit { get; set; } = "";
        public int TrackId { get; set; }
    }

    public class SilabusDeleteRequest
    {
        public int DeliverableId { get; set; }
    }

    public class SilabusKompetensiRequest
    {
        public int KompetensiId { get; set; }
    }

    public class KompetensiDeleteRequest
    {
        public int KompetensiId { get; set; }
    }

    public class GuidanceDeleteRequest
    {
        public int Id { get; set; }
    }

    public class OverrideSaveRequest
    {
        public int ProgressId { get; set; }
        public string NewStatus { get; set; } = "";
        public string NewHCStatus { get; set; } = "";
        public string? NewRejectionReason { get; set; }
        public string OverrideReason { get; set; } = "";
    }

    [Authorize(Roles = "Admin,HC")]
    public class ProtonDataController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLog;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProtonDataController> _logger;

        public ProtonDataController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<ProtonDataController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _env = env;
            _logger = logger;
        }

        // GET: /ProtonData/StatusData
        public async Task<IActionResult> StatusData()
        {
            // 1. Silabus completeness per (Bagian, Unit, TrackId)
            var kompetensiData = await _context.ProtonKompetensiList
                .Where(k => k.IsActive)
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .ToListAsync();

            var silabusStatus = kompetensiData
                .GroupBy(k => new { k.Bagian, k.Unit, k.ProtonTrackId })
                .ToDictionary(
                    g => g.Key,
                    g => g.All(k => k.SubKompetensiList.Any()
                        && k.SubKompetensiList.All(s => s.Deliverables.Any()))
                );

            // 2. Guidance existence per (Bagian, Unit, TrackId)
            var guidanceKeys = await _context.CoachingGuidanceFiles
                .Select(f => new { f.Bagian, f.Unit, f.ProtonTrackId })
                .Distinct()
                .ToListAsync();
            var guidanceSet = new HashSet<string>(
                guidanceKeys.Select(g => $"{g.Bagian}|{g.Unit}|{g.ProtonTrackId}"));

            // 3. Build response for all combos
            var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            var result = new List<object>();

            foreach (var section in OrganizationStructure.SectionUnits)
            {
                foreach (var unit in section.Value)
                {
                    foreach (var track in tracks)
                    {
                        var key = new { Bagian = section.Key, Unit = unit, ProtonTrackId = track.Id };
                        var silabusOk = silabusStatus.TryGetValue(key, out var ok) && ok;
                        var guidanceOk = guidanceSet.Contains($"{section.Key}|{unit}|{track.Id}");

                        result.Add(new { bagian = section.Key, unit, trackId = track.Id,
                            trackName = track.DisplayName, silabusOk, guidanceOk });
                    }
                }
            }

            return Json(result);
        }

        // GET: /ProtonData
        public async Task<IActionResult> Index(string? bagian, string? unit, int? trackId, bool showInactive = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.AllTracks = tracks;
            ViewBag.Bagian = bagian;
            ViewBag.Unit = unit;
            ViewBag.TrackId = trackId;
            ViewBag.ShowInactive = showInactive;

            // Build flat silabus rows for JSON serialization to JS
            var silabusRows = new List<object>();
            if (!string.IsNullOrEmpty(bagian) && !string.IsNullOrEmpty(unit) && trackId.HasValue)
            {
                var kompetensiList = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value && (showInactive || k.IsActive))
                    .OrderBy(k => k.NamaKompetensi)
                    .ToListAsync();

                foreach (var k in kompetensiList)
                {
                    foreach (var s in k.SubKompetensiList.OrderBy(s => s.NamaSubKompetensi))
                    {
                        foreach (var d in s.Deliverables.OrderBy(d => d.NamaDeliverable))
                        {
                            silabusRows.Add(new
                            {
                                KompetensiId = k.Id,
                                Kompetensi = k.NamaKompetensi,
                                SubKompetensiId = s.Id,
                                SubKompetensi = s.NamaSubKompetensi,
                                Target = d.Target ?? "-",
                                DeliverableId = d.Id,
                                Deliverable = d.NamaDeliverable,
                                No = d.Urutan.ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.SilabusRowsJson = System.Text.Json.JsonSerializer.Serialize(silabusRows,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });

            return View();
        }

        // GET: /ProtonData/Override
        public async Task<IActionResult> Override()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.AllTracks = tracks;

            return View();
        }

        // POST: /ProtonData/SilabusSave — batch upsert all silabus rows
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SilabusSave([FromBody] List<SilabusRowDto> rows)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (rows == null || !rows.Any())
                return Json(new { success = false, message = "Tidak ada data." });

            // Validate Target not empty
            if (rows.Any(r => string.IsNullOrWhiteSpace(r.Target)))
                return Json(new { success = false, message = "Target tidak boleh kosong." });

            var bagian = rows[0].Bagian;
            var unit = rows[0].Unit;
            var trackId = rows[0].TrackId;

            // Track counts for audit log
            int created = 0, updated = 0;
            int urutan = 1;

            // Group rows by Kompetensi (text) to handle the nested structure
            // Keep a dictionary of seen Kompetensi text → entity to avoid duplicates within same save
            var kompDict = new Dictionary<string, ProtonKompetensi>();
            var subKompDict = new Dictionary<string, ProtonSubKompetensi>();
            var newDelivIds = new List<int>(); // Track newly created deliverable IDs for orphan cleanup
            // Track IDs of newly-created Kompetensi/SubKompetensi that came from stale-ID path (FindAsync returned null)
            var staleFallbackKompIds = new List<int>();
            var staleFallbackSubIds = new List<int>();

            foreach (var row in rows)
            {
                // 1. Upsert Kompetensi
                ProtonKompetensi? komp;
                var kompKey = row.Kompetensi.Trim();
                if (row.KompetensiId > 0)
                {
                    komp = await _context.ProtonKompetensiList.FindAsync(row.KompetensiId);
                    if (komp != null)
                    {
                        komp.NamaKompetensi = kompKey;
                        komp.Bagian = bagian;
                        komp.Unit = unit;
                        komp.ProtonTrackId = trackId;
                    }
                    else
                    {
                        // ID not found — treat as new; track new ID for orphan-safe inclusion
                        komp = new ProtonKompetensi { Bagian = bagian, Unit = unit, ProtonTrackId = trackId, NamaKompetensi = kompKey, Urutan = urutan };
                        _context.ProtonKompetensiList.Add(komp);
                        created++;
                    }
                }
                else if (kompDict.TryGetValue(kompKey, out var existing))
                {
                    komp = existing;
                }
                else
                {
                    komp = new ProtonKompetensi { Bagian = bagian, Unit = unit, ProtonTrackId = trackId, NamaKompetensi = kompKey, Urutan = urutan };
                    _context.ProtonKompetensiList.Add(komp);
                    kompDict[kompKey] = komp;
                    created++;
                }
                await _context.SaveChangesAsync(); // Flush to get Id for FK
                // If this was a stale-ID fallback, record the new Id so orphan cleanup won't delete it
                if (row.KompetensiId > 0 && komp!.Id != row.KompetensiId)
                    staleFallbackKompIds.Add(komp.Id);

                // 2. Upsert SubKompetensi
                ProtonSubKompetensi? subKomp;
                var subKey = $"{komp!.Id}|{row.SubKompetensi.Trim()}";
                if (row.SubKompetensiId > 0)
                {
                    subKomp = await _context.ProtonSubKompetensiList.FindAsync(row.SubKompetensiId);
                    if (subKomp != null)
                    {
                        subKomp.NamaSubKompetensi = row.SubKompetensi.Trim();
                        subKomp.ProtonKompetensiId = komp.Id;
                    }
                    else
                    {
                        subKomp = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = row.SubKompetensi.Trim(), Urutan = urutan };
                        _context.ProtonSubKompetensiList.Add(subKomp);
                        created++;
                    }
                }
                else if (subKompDict.TryGetValue(subKey, out var existingSub))
                {
                    subKomp = existingSub;
                }
                else
                {
                    subKomp = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = row.SubKompetensi.Trim(), Urutan = urutan };
                    _context.ProtonSubKompetensiList.Add(subKomp);
                    subKompDict[subKey] = subKomp;
                    created++;
                }
                await _context.SaveChangesAsync();
                // If this was a stale-ID fallback, record the new Id
                if (row.SubKompetensiId > 0 && subKomp!.Id != row.SubKompetensiId)
                    staleFallbackSubIds.Add(subKomp.Id);

                // 3. Upsert Deliverable
                if (row.DeliverableId > 0)
                {
                    var deliv = await _context.ProtonDeliverableList.FindAsync(row.DeliverableId);
                    if (deliv != null)
                    {
                        deliv.NamaDeliverable = row.Deliverable.Trim();
                        deliv.ProtonSubKompetensiId = subKomp!.Id;
                        deliv.Target = string.IsNullOrWhiteSpace(row.Target) ? null : row.Target.Trim();
                        deliv.Urutan = urutan;
                        updated++;
                    }
                    else
                    {
                        // Stale deliverable ID — create new
                        var deliv2 = new ProtonDeliverable
                        {
                            ProtonSubKompetensiId = subKomp!.Id,
                            NamaDeliverable = row.Deliverable.Trim(),
                            Target = string.IsNullOrWhiteSpace(row.Target) ? null : row.Target.Trim(),
                            Urutan = urutan
                        };
                        _context.ProtonDeliverableList.Add(deliv2);
                        await _context.SaveChangesAsync();
                        newDelivIds.Add(deliv2.Id);
                        created++;
                    }
                }
                else
                {
                    var deliv = new ProtonDeliverable
                    {
                        ProtonSubKompetensiId = subKomp!.Id,
                        NamaDeliverable = row.Deliverable.Trim(),
                        Target = string.IsNullOrWhiteSpace(row.Target) ? null : row.Target.Trim(),
                        Urutan = urutan
                    };
                    _context.ProtonDeliverableList.Add(deliv);
                    await _context.SaveChangesAsync(); // Flush to get Id for orphan tracking
                    newDelivIds.Add(deliv.Id);
                    created++;
                }
                urutan++;
            }

            await _context.SaveChangesAsync();

            // Delete orphaned entities: any Kompetensi/SubKompetensi/Deliverable for this Bagian+Unit+Track that are NOT in the saved rows
            var savedKompIds = rows.Where(r => r.KompetensiId > 0).Select(r => r.KompetensiId).Distinct().ToList();
            var savedSubIds = rows.Where(r => r.SubKompetensiId > 0).Select(r => r.SubKompetensiId).Distinct().ToList();
            var savedDelivIds = rows.Where(r => r.DeliverableId > 0).Select(r => r.DeliverableId).Distinct().ToList();

            // Also include newly-created IDs from kompDict/subKompDict/newDelivIds and stale-ID fallbacks
            savedKompIds.AddRange(kompDict.Values.Select(k => k.Id));
            savedKompIds.AddRange(staleFallbackKompIds);
            savedSubIds.AddRange(subKompDict.Values.Select(s => s.Id));
            savedSubIds.AddRange(staleFallbackSubIds);
            savedDelivIds.AddRange(newDelivIds);

            // Find all scope records fresh from DB for orphan evaluation
            var scopeKomps = await _context.ProtonKompetensiList
                .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId)
                .Include(k => k.SubKompetensiList).ThenInclude(s => s.Deliverables)
                .ToListAsync();

            int deleted = 0;
            // Collect orphan IDs first, then remove — avoids in-memory nav property staleness
            var orphanDelivIdSet = new HashSet<int>();
            var orphanSubIdSet = new HashSet<int>();

            foreach (var k in scopeKomps)
            {
                foreach (var s in k.SubKompetensiList)
                {
                    var orphanDelivs = s.Deliverables.Where(d => !savedDelivIds.Contains(d.Id) && d.Id > 0).ToList();
                    if (orphanDelivs.Any())
                    {
                        // Cascade delete progress records for orphaned deliverables (FK is Restrict)
                        var orphanDIds = orphanDelivs.Select(d => d.Id).ToList();
                        var orphanProgressIds = await _context.ProtonDeliverableProgresses
                            .Where(p => orphanDIds.Contains(p.ProtonDeliverableId))
                            .Select(p => p.Id)
                            .ToListAsync();
                        if (orphanProgressIds.Any())
                        {
                            var orphanSessions = await _context.CoachingSessions
                                .Include(cs => cs.ActionItems)
                                .Where(cs => cs.ProtonDeliverableProgressId != null && orphanProgressIds.Contains(cs.ProtonDeliverableProgressId!.Value))
                                .ToListAsync();
                            _context.CoachingSessions.RemoveRange(orphanSessions);
                            var orphanProgresses = await _context.ProtonDeliverableProgresses
                                .Where(p => orphanDIds.Contains(p.ProtonDeliverableId))
                                .ToListAsync();
                            _context.ProtonDeliverableProgresses.RemoveRange(orphanProgresses);
                        }
                        _context.ProtonDeliverableList.RemoveRange(orphanDelivs);
                    }
                    deleted += orphanDelivs.Count;
                    foreach (var od in orphanDelivs) orphanDelivIdSet.Add(od.Id);
                }
                // A SubKompetensi is orphaned if it's not in savedSubIds AND all its deliverables are being deleted
                var orphanSubs = k.SubKompetensiList.Where(s =>
                    !savedSubIds.Contains(s.Id) && s.Id > 0 &&
                    s.Deliverables.All(d => orphanDelivIdSet.Contains(d.Id))).ToList();
                _context.ProtonSubKompetensiList.RemoveRange(orphanSubs);
                deleted += orphanSubs.Count;
                foreach (var os in orphanSubs) orphanSubIdSet.Add(os.Id);
            }
            // A Kompetensi is orphaned if it's not in savedKompIds AND all its SubKompetensi are being deleted
            var orphanKomps = scopeKomps.Where(k =>
                !savedKompIds.Contains(k.Id) &&
                k.SubKompetensiList.All(s => orphanSubIdSet.Contains(s.Id))).ToList();
            _context.ProtonKompetensiList.RemoveRange(orphanKomps);
            deleted += orphanKomps.Count;

            await _context.SaveChangesAsync();

            // Phase 129: Auto-sync new deliverables to matching-unit assignments only
            if (newDelivIds.Any())
            {
                // Remove any orphan-deleted IDs from newDelivIds
                var survivingNewDelivIds = newDelivIds.Where(id => !orphanDelivIdSet.Contains(id)).ToList();
                if (survivingNewDelivIds.Any())
                {
                    // Find active assignments for this track, resolve each assignment's unit
                    var activeAssignments = await _context.ProtonTrackAssignments
                        .Where(a => a.ProtonTrackId == trackId && a.IsActive)
                        .Select(a => new { a.Id, a.CoacheeId })
                        .ToListAsync();

                    // Resolve unit per assignment: AssignmentUnit from active mapping, fallback to User.Unit
                    var assignmentCoacheeIds = activeAssignments.Select(a => a.CoacheeId).Distinct().ToList();
                    var mappingUnits = await _context.CoachCoacheeMappings
                        .Where(m => m.IsActive && assignmentCoacheeIds.Contains(m.CoacheeId))
                        .Select(m => new { m.CoacheeId, m.AssignmentUnit })
                        .ToListAsync();
                    var userUnits = await _context.Users
                        .Where(u => assignmentCoacheeIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.Unit })
                        .ToDictionaryAsync(u => u.Id, u => u.Unit);

                    // Filter to assignments whose resolved unit matches the deliverable's unit
                    var matchingAssignments = activeAssignments.Where(a =>
                    {
                        var resolvedUnit = (mappingUnits.FirstOrDefault(m => m.CoacheeId == a.CoacheeId)?.AssignmentUnit
                                            ?? userUnits.GetValueOrDefault(a.CoacheeId))?.Trim() ?? "";
                        return resolvedUnit == unit.Trim();
                    }).ToList();

                    if (matchingAssignments.Any())
                    {
                        var matchingIds = matchingAssignments.Select(a => a.Id).ToList();
                        var existingPairs = await _context.ProtonDeliverableProgresses
                            .Where(p => matchingIds.Contains(p.ProtonTrackAssignmentId) && survivingNewDelivIds.Contains(p.ProtonDeliverableId))
                            .Select(p => new { p.ProtonTrackAssignmentId, p.ProtonDeliverableId })
                            .ToListAsync();
                        var existingSet = new HashSet<string>(existingPairs.Select(p => $"{p.ProtonTrackAssignmentId}|{p.ProtonDeliverableId}"));

                        foreach (var assignment in matchingAssignments)
                        {
                            foreach (var deliverableId in survivingNewDelivIds)
                            {
                                if (!existingSet.Contains($"{assignment.Id}|{deliverableId}"))
                                {
                                    _context.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress
                                    {
                                        CoacheeId = assignment.CoacheeId,
                                        ProtonDeliverableId = deliverableId,
                                        ProtonTrackAssignmentId = assignment.Id,
                                        Status = "Belum Mulai",
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }

            await _auditLog.LogAsync(user.Id, user.FullName, "Update",
                $"Silabus saved for {bagian}/{unit}/Track {trackId}: {created} created, {updated} updated, {deleted} orphans removed ({rows.Count} rows total)",
                targetType: "ProtonSilabus");

            return Json(new { success = true, message = $"Data silabus berhasil disimpan ({rows.Count} baris)." });
        }

        // POST: /ProtonData/SilabusDelete — delete a single deliverable row (for inline delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SilabusDelete([FromBody] SilabusDeleteRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (req.DeliverableId <= 0)
                return Json(new { success = true }); // Row not yet saved — JS just removes from array

            var deliv = await _context.ProtonDeliverableList
                .Include(d => d.ProtonSubKompetensi)
                    .ThenInclude(s => s!.ProtonKompetensi)
                .FirstOrDefaultAsync(d => d.Id == req.DeliverableId);

            if (deliv == null) return Json(new { success = false, message = "Data tidak ditemukan." });

            var delivName = deliv.NamaDeliverable;
            var subKomp = deliv.ProtonSubKompetensi;
            var komp = subKomp?.ProtonKompetensi;

            // Cascade delete progress records for this deliverable (FK is Restrict)
            var progressIds = await _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonDeliverableId == deliv.Id)
                .Select(p => p.Id)
                .ToListAsync();
            if (progressIds.Any())
            {
                var sessions = await _context.CoachingSessions
                    .Include(cs => cs.ActionItems)
                    .Where(cs => cs.ProtonDeliverableProgressId != null && progressIds.Contains(cs.ProtonDeliverableProgressId!.Value))
                    .ToListAsync();
                _context.CoachingSessions.RemoveRange(sessions);

                var progresses = await _context.ProtonDeliverableProgresses
                    .Where(p => p.ProtonDeliverableId == deliv.Id)
                    .ToListAsync();
                _context.ProtonDeliverableProgresses.RemoveRange(progresses);
            }

            _context.ProtonDeliverableList.Remove(deliv);
            await _context.SaveChangesAsync();

            // Clean up empty parents
            if (subKomp != null && !await _context.ProtonDeliverableList.AnyAsync(d => d.ProtonSubKompetensiId == subKomp.Id))
            {
                _context.ProtonSubKompetensiList.Remove(subKomp);
                await _context.SaveChangesAsync();

                if (komp != null && !await _context.ProtonSubKompetensiList.AnyAsync(s => s.ProtonKompetensiId == komp.Id))
                {
                    _context.ProtonKompetensiList.Remove(komp);
                    await _context.SaveChangesAsync();
                }
            }

            await _auditLog.LogAsync(user.Id, user.FullName, "Delete",
                $"Deleted silabus deliverable '{delivName}' (ID {req.DeliverableId})",
                targetId: req.DeliverableId, targetType: "ProtonDeliverable");

            return Json(new { success = true });
        }

        // POST: /ProtonData/SilabusDeactivate — soft delete: set IsActive=false
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SilabusDeactivate([FromBody] SilabusKompetensiRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Sesi tidak valid." });

            var komp = await _context.ProtonKompetensiList.FindAsync(req.KompetensiId);
            if (komp == null) return Json(new { success = false, message = "Silabus tidak ditemukan." });
            if (!komp.IsActive) return Json(new { success = false, message = "Silabus sudah tidak aktif." });

            komp.IsActive = false;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "Deactivate",
                $"Deactivated silabus kompetensi '{komp.NamaKompetensi}' (ID {komp.Id})",
                targetId: komp.Id, targetType: "ProtonKompetensi");

            return Json(new { success = true, message = $"Silabus '{komp.NamaKompetensi}' berhasil dinonaktifkan." });
        }

        // POST: /ProtonData/SilabusReactivate — restore soft-deleted silabus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SilabusReactivate([FromBody] SilabusKompetensiRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Sesi tidak valid." });

            var komp = await _context.ProtonKompetensiList.FindAsync(req.KompetensiId);
            if (komp == null) return Json(new { success = false, message = "Silabus tidak ditemukan." });
            if (komp.IsActive) return Json(new { success = false, message = "Silabus sudah aktif." });

            komp.IsActive = true;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "Reactivate",
                $"Reactivated silabus kompetensi '{komp.NamaKompetensi}' (ID {komp.Id})",
                targetId: komp.Id, targetType: "ProtonKompetensi");

            return Json(new { success = true, message = $"Silabus '{komp.NamaKompetensi}' berhasil diaktifkan kembali." });
        }

        // GET: /ProtonData/GuidanceList?bagian=X&unit=Y&trackId=Z
        public async Task<IActionResult> GuidanceList(string bagian, string unit, int trackId)
        {
            var files = await _context.CoachingGuidanceFiles
                .Where(f => f.Bagian == bagian && f.Unit == unit && f.ProtonTrackId == trackId)
                .OrderByDescending(f => f.UploadedAt)
                .Select(f => new {
                    f.Id,
                    f.FileName,
                    f.FileSize,
                    UploadedAt = f.UploadedAt.ToString("dd MMM yyyy HH:mm")
                })
                .ToListAsync();

            return Json(files);
        }

        // POST: /ProtonData/GuidanceUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuidanceUpload(string bagian, string unit, int trackId, IFormFile? file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (file == null || file.Length == 0)
                return Json(new { success = false, error = "File tidak boleh kosong." });

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Json(new { success = false, error = "Tipe file tidak diperbolehkan. Gunakan PDF, Word, Excel, atau PowerPoint." });

            if (file.Length > 10 * 1024 * 1024)
                return Json(new { success = false, error = "Ukuran file maksimal 10 MB." });

            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "guidance");
            Directory.CreateDirectory(uploadDir);

            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, safeFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var record = new CoachingGuidanceFile
            {
                Bagian = bagian,
                Unit = unit,
                ProtonTrackId = trackId,
                FileName = file.FileName,
                FilePath = $"/uploads/guidance/{safeFileName}",
                FileSize = file.Length,
                UploadedById = user.Id
            };
            _context.CoachingGuidanceFiles.Add(record);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(user.Id, user.FullName, "Upload",
                $"Uploaded guidance file '{file.FileName}' ({FormatFileSize(file.Length)}) for {bagian}/{unit}/Track {trackId}",
                targetId: record.Id, targetType: "CoachingGuidanceFile");

            return Json(new { success = true });
        }

        // GET: /ProtonData/GuidanceDownload?id=X
        // NOTE (Phase 86): This action inherits class-level [Authorize(Roles = "Admin,HC")].
        // Plan IDP (Phase 86) requires coachees to download guidance files.
        // Recommended approach: add a separate GuidanceDownload action in CDPController
        // with [Authorize] (any authenticated user) that reuses the same DB lookup + file serve logic,
        // keeping the Admin/HC-restricted version here for the manage-data context.
        public async Task<IActionResult> GuidanceDownload(int id)
        {
            var record = await _context.CoachingGuidanceFiles.FindAsync(id);
            if (record == null) return NotFound();

            var physicalPath = Path.Combine(_env.WebRootPath, record.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            var contentType = GetContentType(Path.GetExtension(record.FilePath));
            return PhysicalFile(physicalPath, contentType, record.FileName);
        }

        // POST: /ProtonData/GuidanceReplace
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuidanceReplace(int id, IFormFile? file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var record = await _context.CoachingGuidanceFiles.FindAsync(id);
            if (record == null) return Json(new { success = false, error = "Record tidak ditemukan." });

            if (file == null || file.Length == 0)
                return Json(new { success = false, error = "File tidak boleh kosong." });

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Json(new { success = false, error = "Tipe file tidak diperbolehkan." });

            if (file.Length > 10 * 1024 * 1024)
                return Json(new { success = false, error = "Ukuran file maksimal 10 MB." });

            // Delete old physical file
            var oldPath = Path.Combine(_env.WebRootPath, record.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }

            // Save new file
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "guidance");
            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var newFilePath = Path.Combine(uploadDir, safeFileName);

            using (var stream = new FileStream(newFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var oldFileName = record.FileName;
            record.FileName = file.FileName;
            record.FilePath = $"/uploads/guidance/{safeFileName}";
            record.FileSize = file.Length;
            record.UploadedAt = DateTime.UtcNow;
            record.UploadedById = user.Id;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(user.Id, user.FullName, "Update",
                $"Replaced guidance file '{oldFileName}' with '{file.FileName}' ({FormatFileSize(file.Length)})",
                targetId: record.Id, targetType: "CoachingGuidanceFile");

            return Json(new { success = true });
        }

        // POST: /ProtonData/GuidanceDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuidanceDelete([FromBody] GuidanceDeleteRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var record = await _context.CoachingGuidanceFiles.FindAsync(req.Id);
            if (record == null) return Json(new { success = false, error = "Record tidak ditemukan." });

            var fileName = record.FileName;

            // Delete DB record first
            _context.CoachingGuidanceFiles.Remove(record);
            await _context.SaveChangesAsync();

            // Then delete physical file (non-critical if fails)
            var physicalPath = Path.Combine(_env.WebRootPath, record.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete physical guidance file for record {Id}, path {Path}", req.Id, physicalPath); }

            await _auditLog.LogAsync(user.Id, user.FullName, "Delete",
                $"Deleted guidance file '{fileName}'",
                targetId: req.Id, targetType: "CoachingGuidanceFile");

            return Json(new { success = true });
        }

        // GET: /ProtonData/OverrideList?bagian=X&unit=Y&trackId=Z&statusFilter=rejected|pendingHC
        public async Task<IActionResult> OverrideList(string bagian, string unit, int trackId, string? statusFilter)
        {
            if (string.IsNullOrEmpty(bagian) || string.IsNullOrEmpty(unit) || trackId == 0)
                return Json(new { success = true, coachees = new List<object>(), deliverableHeaders = new List<object>() });

            // 1. Load all deliverables for this Bagian+Unit+Track scope
            var deliverables = await _context.ProtonDeliverableList
                .Include(d => d.ProtonSubKompetensi)
                    .ThenInclude(s => s.ProtonKompetensi)
                .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.Bagian == bagian
                         && d.ProtonSubKompetensi.ProtonKompetensi.Unit == unit
                         && d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == trackId)
                .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
                    .ThenBy(d => d.ProtonSubKompetensi.Urutan)
                    .ThenBy(d => d.Urutan)
                .ToListAsync();

            var deliverableIds = deliverables.Select(d => d.Id).ToList();

            // 2. Load all progress records for these deliverables
            var allProgresses = await _context.ProtonDeliverableProgresses
                .Where(p => deliverableIds.Contains(p.ProtonDeliverableId))
                .ToListAsync();

            // 3. Get coachee names as dictionary
            var coacheeIds = allProgresses.Select(p => p.CoacheeId).Distinct().ToList();
            var coacheeNames = await _context.Users
                .Where(u => coacheeIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);

            // 4. Group progresses by coachee
            var progressesByCoachee = allProgresses.GroupBy(p => p.CoacheeId).ToList();

            // 5. Apply status filter in memory
            var filteredGroups = progressesByCoachee.AsEnumerable();
            if (statusFilter == "rejected")
                filteredGroups = filteredGroups.Where(g => g.Any(p => p.Status == "Rejected"));
            else if (statusFilter == "pendingHC")
                filteredGroups = filteredGroups.Where(g => g.Any(p => p.HCApprovalStatus == "Pending" && p.Status == "Approved"));

            // 6. Build response per coachee
            var coachees = filteredGroups.Select(g =>
            {
                var coacheeId = g.Key;
                var coacheeName = coacheeNames.TryGetValue(coacheeId, out var name) ? name : coacheeId;
                var progressMap = g.ToDictionary(p => p.ProtonDeliverableId);

                var badges = deliverables.Select(d =>
                {
                    if (progressMap.TryGetValue(d.Id, out var prog))
                        return (object)new { progressId = prog.Id, deliverableId = d.Id, deliverableName = d.NamaDeliverable, status = prog.Status };
                    else
                        return (object)new { progressId = (int?)null, deliverableId = d.Id, deliverableName = d.NamaDeliverable, status = "—" };
                }).ToList();

                return (object)new { coacheeId, coacheeName, badges };
            }).ToList();

            // 7. Build deliverable headers for table columns
            var deliverableHeaders = deliverables.Select(d => new { id = d.Id, name = d.NamaDeliverable }).ToList();

            return Json(new { success = true, coachees, deliverableHeaders });
        }

        // GET: /ProtonData/OverrideDetail?id=X
        public async Task<IActionResult> OverrideDetail(int id)
        {
            var progress = await _context.ProtonDeliverableProgresses
                .Include(p => p.ProtonDeliverable)
                    .ThenInclude(d => d!.ProtonSubKompetensi)
                        .ThenInclude(s => s!.ProtonKompetensi)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (progress == null) return Json(new { success = false });

            string? approverName = null;
            if (!string.IsNullOrEmpty(progress.ApprovedById))
            {
                approverName = await _context.Users
                    .Where(u => u.Id == progress.ApprovedById)
                    .Select(u => u.FullName ?? u.UserName)
                    .FirstOrDefaultAsync();
            }

            string? hcReviewerName = null;
            if (!string.IsNullOrEmpty(progress.HCReviewedById))
            {
                hcReviewerName = await _context.Users
                    .Where(u => u.Id == progress.HCReviewedById)
                    .Select(u => u.FullName ?? u.UserName)
                    .FirstOrDefaultAsync();
            }

            var deliverable = progress.ProtonDeliverable;
            var subKomp = deliverable?.ProtonSubKompetensi;
            var komp = subKomp?.ProtonKompetensi;

            return Json(new
            {
                success = true,
                id = progress.Id,
                deliverableName = deliverable?.NamaDeliverable,
                kompetensiName = komp?.NamaKompetensi,
                subKompetensiName = subKomp?.NamaSubKompetensi,
                coacheeId = progress.CoacheeId,
                status = progress.Status,
                hcApprovalStatus = progress.HCApprovalStatus,
                evidenceFileName = progress.EvidenceFileName,
                evidencePath = progress.EvidencePath,
                submittedAt = progress.SubmittedAt?.ToString("dd MMM yyyy HH:mm"),
                approvedAt = progress.ApprovedAt?.ToString("dd MMM yyyy HH:mm"),
                rejectedAt = progress.RejectedAt?.ToString("dd MMM yyyy HH:mm"),
                rejectionReason = progress.RejectionReason,
                approvedByName = approverName,
                hcReviewedAt = progress.HCReviewedAt?.ToString("dd MMM yyyy HH:mm"),
                hcReviewedByName = hcReviewerName
            });
        }

        // POST: /ProtonData/OverrideSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OverrideSave([FromBody] OverrideSaveRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(req.OverrideReason))
                return Json(new { success = false, message = "Alasan override wajib diisi." });

            var validStatuses = new[] { "Pending", "Submitted", "Approved", "Rejected" };
            if (!validStatuses.Contains(req.NewStatus))
                return Json(new { success = false, message = "Status tidak valid." });

            var validHCStatuses = new[] { "Pending", "Reviewed" };
            if (!validHCStatuses.Contains(req.NewHCStatus))
                return Json(new { success = false, message = "HC Status tidak valid." });

            var progress = await _context.ProtonDeliverableProgresses.FindAsync(req.ProgressId);
            if (progress == null)
                return Json(new { success = false, message = "Record tidak ditemukan." });

            var oldStatus = progress.Status;

            // Auto-fill timestamps based on new status
            switch (req.NewStatus)
            {
                case "Approved":
                    progress.ApprovedAt = DateTime.UtcNow;
                    progress.ApprovedById = user.Id;
                    break;
                case "Rejected":
                    progress.RejectedAt = DateTime.UtcNow;
                    break;
                case "Submitted":
                    progress.SubmittedAt = DateTime.UtcNow;
                    break;
                case "Pending":
                    progress.ApprovedAt = null;
                    progress.RejectedAt = null;
                    progress.SubmittedAt = null;
                    break;
            }

            progress.Status = req.NewStatus;
            progress.HCApprovalStatus = req.NewHCStatus;
            progress.RejectionReason = req.NewRejectionReason;

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "Override",
                $"Override deliverable progress #{progress.Id}: {oldStatus} → {req.NewStatus}. Alasan: {req.OverrideReason}",
                targetId: progress.Id, targetType: "ProtonDeliverableProgress");

            return Json(new { success = true });
        }

        // GET: /ProtonData/GetKompetensiCascadeInfo?id=X
        [HttpGet]
        public async Task<IActionResult> GetKompetensiCascadeInfo(int id)
        {
            var komp = await _context.ProtonKompetensiList
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (komp == null)
                return Json(new { success = false, message = "Kompetensi tidak ditemukan." });

            var deliverableIds = komp.SubKompetensiList
                .SelectMany(s => s.Deliverables)
                .Select(d => d.Id)
                .ToList();

            var progressIds = await _context.ProtonDeliverableProgresses
                .Where(p => deliverableIds.Contains(p.ProtonDeliverableId))
                .Select(p => p.Id)
                .ToListAsync();

            var sessionCount = await _context.CoachingSessions
                .Where(cs => cs.ProtonDeliverableProgressId != null && progressIds.Contains(cs.ProtonDeliverableProgressId!.Value))
                .CountAsync();

            return Json(new
            {
                success = true,
                nama = komp.NamaKompetensi,
                subKompetensiCount = komp.SubKompetensiList.Count,
                deliverableCount = deliverableIds.Count,
                progressCount = progressIds.Count,
                sessionCount
            });
        }

        // POST: /ProtonData/DeleteKompetensi — hard delete with full cascade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKompetensi([FromBody] KompetensiDeleteRequest req)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var komp = await _context.ProtonKompetensiList
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .FirstOrDefaultAsync(k => k.Id == req.KompetensiId);

            if (komp == null)
                return Json(new { success = false, message = "Kompetensi tidak ditemukan." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var deliverableIds = komp.SubKompetensiList
                    .SelectMany(s => s.Deliverables)
                    .Select(d => d.Id)
                    .ToList();

                var progressIds = await _context.ProtonDeliverableProgresses
                    .Where(p => deliverableIds.Contains(p.ProtonDeliverableId))
                    .Select(p => p.Id)
                    .ToListAsync();

                // 1. Delete CoachingSessions (no FK constraint, must be explicit; ActionItems cascade automatically)
                if (progressIds.Any())
                {
                    var sessions = await _context.CoachingSessions
                        .Include(cs => cs.ActionItems)
                        .Where(cs => cs.ProtonDeliverableProgressId != null && progressIds.Contains(cs.ProtonDeliverableProgressId!.Value))
                        .ToListAsync();
                    _context.CoachingSessions.RemoveRange(sessions);
                    await _context.SaveChangesAsync();
                }

                // 2. Delete ProtonDeliverableProgresses
                if (progressIds.Any())
                {
                    var progresses = await _context.ProtonDeliverableProgresses
                        .Where(p => progressIds.Contains(p.Id))
                        .ToListAsync();
                    _context.ProtonDeliverableProgresses.RemoveRange(progresses);
                    await _context.SaveChangesAsync();
                }

                // 3. Delete Deliverables
                var deliverables = komp.SubKompetensiList.SelectMany(s => s.Deliverables).ToList();
                _context.ProtonDeliverableList.RemoveRange(deliverables);
                await _context.SaveChangesAsync();

                // 4. Delete SubKompetensi
                _context.ProtonSubKompetensiList.RemoveRange(komp.SubKompetensiList);
                await _context.SaveChangesAsync();

                // 5. Delete Kompetensi
                _context.ProtonKompetensiList.Remove(komp);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "Delete",
                    $"Hard-deleted kompetensi '{komp.NamaKompetensi}' (ID {komp.Id}) with all descendants",
                    targetId: komp.Id, targetType: "ProtonKompetensi");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to delete Kompetensi {Id}", req.KompetensiId);
                return Json(new { success = false, message = "Gagal menghapus: " + ex.Message });
            }
        }

        private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024L) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}
