using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using ClosedXML.Excel;
using HcPortal.Helpers;

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

    public class ImportSilabusResult
    {
        public int RowNumber { get; set; }
        public string Kompetensi { get; set; } = "";
        public string SubKompetensi { get; set; } = "";
        public string Deliverable { get; set; } = "";
        public string Status { get; set; } = ""; // "Created", "Updated", "Error", "Skipped"
        public string Message { get; set; } = "";
    }

    public class ParsedSilabusRow
    {
        public int RowNumber { get; set; }
        public string Bagian { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Track { get; set; } = "";
        public string Kompetensi { get; set; } = "";
        public string SubKompetensi { get; set; } = "";
        public string Deliverable { get; set; } = "";
        public string Target { get; set; } = "";
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

            var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
            foreach (var section in sectionUnitsDict)
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

            var sectionUnitsDictIndex = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictIndex);
            ViewBag.Sections = sectionUnitsDictIndex.Keys.ToList();

            return View();
        }

        // GET: /ProtonData/Override
        public async Task<IActionResult> Override()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.AllTracks = tracks;

            var sectionUnitsDictOverride = await _context.GetSectionUnitsDictAsync();
            ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictOverride);

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
                        // D-02: Block hard delete if any orphan deliverable has active (non-Approved) progress
                        var orphanDIds = orphanDelivs.Select(d => d.Id).ToList();
                        var hasActiveProgress = await _context.ProtonDeliverableProgresses
                            .AnyAsync(p => orphanDIds.Contains(p.ProtonDeliverableId) && p.Status != "Approved");
                        if (hasActiveProgress)
                        {
                            // Skip deletion of deliverables with active progress — keep them in DB
                            // Filter out deliverables that have active progress
                            var activeDelivIds = await _context.ProtonDeliverableProgresses
                                .Where(p => orphanDIds.Contains(p.ProtonDeliverableId) && p.Status != "Approved")
                                .Select(p => p.ProtonDeliverableId)
                                .Distinct()
                                .ToListAsync();
                            var safeOrphanDelivs = orphanDelivs.Where(d => !activeDelivIds.Contains(d.Id)).ToList();
                            orphanDelivs = safeOrphanDelivs;
                            orphanDIds = orphanDelivs.Select(d => d.Id).ToList();
                        }

                        if (orphanDelivs.Any())
                        {
                            // Cascade delete progress records for orphaned deliverables (FK is Restrict)
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

                        var newProgresses = new List<ProtonDeliverableProgress>();
                        foreach (var assignment in matchingAssignments)
                        {
                            foreach (var deliverableId in survivingNewDelivIds)
                            {
                                if (!existingSet.Contains($"{assignment.Id}|{deliverableId}"))
                                {
                                    var newProgress = new ProtonDeliverableProgress
                                    {
                                        CoacheeId = assignment.CoacheeId,
                                        ProtonDeliverableId = deliverableId,
                                        ProtonTrackAssignmentId = assignment.Id,
                                        Status = "Belum Mulai",
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    _context.ProtonDeliverableProgresses.Add(newProgress);
                                    newProgresses.Add(newProgress);
                                }
                            }
                        }
                        await _context.SaveChangesAsync(); // flush to get IDs for StatusHistory

                        // D-17: Insert initial "Pending" StatusHistory for each new progress
                        var actorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";
                        foreach (var p in newProgresses)
                        {
                            _context.DeliverableStatusHistories.Add(new DeliverableStatusHistory
                            {
                                ProtonDeliverableProgressId = p.Id,
                                StatusType = "Pending",
                                ActorId = actorId,
                                ActorName = "System",
                                ActorRole = "System",
                                Timestamp = DateTime.UtcNow
                            });
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

        // GET: /ProtonData/SilabusDeletePreview — check impact before hard delete (deliverable level)
        [HttpGet]
        public async Task<IActionResult> SilabusDeletePreview(int deliverableId)
        {
            var progressQuery = _context.ProtonDeliverableProgresses
                .Where(p => p.ProtonDeliverableId == deliverableId);
            var hasActiveProgress = await progressQuery.AnyAsync(p => p.Status != "Approved");
            var totalProgress = await progressQuery.CountAsync();
            var coacheeCount = await progressQuery.Select(p => p.CoacheeId).Distinct().CountAsync();
            return Json(new { hasActiveProgress, totalProgress, coacheeCount });
        }

        // GET: /ProtonData/SubKompetensiDeletePreview — check impact before hard delete (sub-kompetensi level)
        [HttpGet]
        public async Task<IActionResult> SubKompetensiDeletePreview(int subKompetensiId)
        {
            var deliverableIds = await _context.ProtonDeliverableList
                .Where(d => d.ProtonSubKompetensiId == subKompetensiId)
                .Select(d => d.Id).ToListAsync();
            var progressQuery = _context.ProtonDeliverableProgresses
                .Where(p => deliverableIds.Contains(p.ProtonDeliverableId));
            var hasActiveProgress = await progressQuery.AnyAsync(p => p.Status != "Approved");
            var totalProgress = await progressQuery.CountAsync();
            var coacheeCount = await progressQuery.Select(p => p.CoacheeId).Distinct().CountAsync();
            return Json(new { hasActiveProgress, totalProgress, coacheeCount });
        }

        // GET: /ProtonData/KompetensiDeletePreview — check impact before hard delete (kompetensi level)
        [HttpGet]
        public async Task<IActionResult> KompetensiDeletePreview(int kompetensiId)
        {
            var subIds = await _context.ProtonSubKompetensiList
                .Where(s => s.ProtonKompetensiId == kompetensiId)
                .Select(s => s.Id).ToListAsync();
            var deliverableIds = await _context.ProtonDeliverableList
                .Where(d => subIds.Contains(d.ProtonSubKompetensiId))
                .Select(d => d.Id).ToListAsync();
            var progressQuery = _context.ProtonDeliverableProgresses
                .Where(p => deliverableIds.Contains(p.ProtonDeliverableId));
            var hasActiveProgress = await progressQuery.AnyAsync(p => p.Status != "Approved");
            var totalProgress = await progressQuery.CountAsync();
            var coacheeCount = await progressQuery.Select(p => p.CoacheeId).Distinct().CountAsync();
            return Json(new { hasActiveProgress, totalProgress, coacheeCount });
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

            // D-01: Block hard delete if there is any active (non-Approved) progress
            var hasActiveProgress = await _context.ProtonDeliverableProgresses
                .AnyAsync(p => p.ProtonDeliverableId == req.DeliverableId && p.Status != "Approved");
            if (hasActiveProgress)
                return Json(new { success = false, hasActiveProgress = true,
                    message = "Deliverable ini memiliki progress aktif. Gunakan Deactivate untuk menonaktifkan silabus." });

            var delivName = deliv.NamaDeliverable;
            var subKomp = deliv.ProtonSubKompetensi;
            var komp = subKomp?.ProtonKompetensi;

            // D-06: Wrap all cascade delete in a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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

                // D-04: Orphan cleanup — remove SubKompetensi if no other deliverables remain
                if (subKomp != null)
                {
                    var remainingDeliverables = await _context.ProtonDeliverableList
                        .CountAsync(d => d.ProtonSubKompetensiId == subKomp.Id);
                    if (remainingDeliverables == 0)
                    {
                        _context.ProtonSubKompetensiList.Remove(subKomp);
                        await _context.SaveChangesAsync();

                        // D-04: Orphan cleanup — remove Kompetensi if no other sub-kompetensi remain
                        if (komp != null)
                        {
                            var remainingSubs = await _context.ProtonSubKompetensiList
                                .CountAsync(s => s.ProtonKompetensiId == komp.Id);
                            if (remainingSubs == 0)
                            {
                                _context.ProtonKompetensiList.Remove(komp);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "SilabusDelete transaction failed for deliverable {Id}", req.DeliverableId);
                return Json(new { success = false, message = "Operasi gagal. Semua perubahan dibatalkan." });
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

        // GET: /ProtonData/ExportSilabus
        [HttpGet]
        public async Task<IActionResult> ExportSilabus(string? bagian, string? unit, int? trackId)
        {
            if (string.IsNullOrEmpty(bagian) || string.IsNullOrEmpty(unit) || !trackId.HasValue)
                return BadRequest("Parameter bagian, unit, dan trackId wajib diisi.");

            var kompetensiList = await _context.ProtonKompetensiList
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value && k.IsActive)
                .OrderBy(k => k.NamaKompetensi)
                .ToListAsync();

            var track = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == trackId.Value);
            var trackName = track?.DisplayName ?? trackId.Value.ToString();

            using var workbook = new XLWorkbook();
            var ws = ExcelExportHelper.CreateSheet(workbook, "Silabus Proton", new[] { "Bagian", "Unit", "Track", "Kompetensi", "SubKompetensi", "Deliverable", "Target" });
            ws.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 2;
            foreach (var k in kompetensiList)
            {
                foreach (var s in k.SubKompetensiList.OrderBy(x => x.Urutan))
                {
                    foreach (var d in s.Deliverables.OrderBy(x => x.Urutan))
                    {
                        ws.Cell(row, 1).Value = k.Bagian;
                        ws.Cell(row, 2).Value = k.Unit;
                        ws.Cell(row, 3).Value = trackName;
                        ws.Cell(row, 4).Value = k.NamaKompetensi;
                        ws.Cell(row, 5).Value = s.NamaSubKompetensi;
                        ws.Cell(row, 6).Value = d.NamaDeliverable;
                        ws.Cell(row, 7).Value = d.Target ?? "";
                        row++;
                    }
                }
            }

            var fileName = $"SilabusProton_{bagian}_{unit}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // GET: /ProtonData/DownloadSilabusTemplate
        [HttpGet]
        public async Task<IActionResult> DownloadSilabusTemplate(string? bagian, string? unit, int? trackId)
        {
            if (string.IsNullOrEmpty(bagian) || string.IsNullOrEmpty(unit) || !trackId.HasValue)
                return BadRequest("Parameter bagian, unit, dan trackId wajib diisi.");

            var kompetensiList = await _context.ProtonKompetensiList
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value && k.IsActive)
                .OrderBy(k => k.NamaKompetensi)
                .ToListAsync();

            var track = await _context.ProtonTracks.FirstOrDefaultAsync(t => t.Id == trackId.Value);
            var trackName = track?.DisplayName ?? trackId.Value.ToString();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Silabus Proton");

            string[] headers = { "Bagian", "Unit", "Track", "Kompetensi", "SubKompetensi", "Deliverable", "Target" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var k in kompetensiList)
            {
                foreach (var s in k.SubKompetensiList.OrderBy(x => x.Urutan))
                {
                    foreach (var d in s.Deliverables.OrderBy(x => x.Urutan))
                    {
                        ws.Cell(row, 1).Value = k.Bagian;
                        ws.Cell(row, 2).Value = k.Unit;
                        ws.Cell(row, 3).Value = trackName;
                        ws.Cell(row, 4).Value = k.NamaKompetensi;
                        ws.Cell(row, 5).Value = s.NamaSubKompetensi;
                        ws.Cell(row, 6).Value = d.NamaDeliverable;
                        ws.Cell(row, 7).Value = d.Target ?? "";
                        row++;
                    }
                }
            }

            // Append 10 empty rows
            for (int i = 0; i < 10; i++) row++;

            var fileName = $"SilabusProton_Template_{bagian}_{unit}_{DateTime.Now:yyyyMMdd}.xlsx";
            return ExcelExportHelper.ToFileResult(workbook, fileName, this);
        }

        // GET: /ProtonData/ImportSilabus
        [HttpGet]
        public IActionResult ImportSilabus()
        {
            return View();
        }

        // POST: /ProtonData/ImportSilabus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportSilabus(IFormFile? excelFile, string? bagian, string? unit, int? trackId)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "File tidak boleh kosong.";
                return View();
            }

            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] = "Hanya file .xlsx atau .xls yang didukung.";
                return View();
            }

            if (excelFile.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "Ukuran file maksimal 10 MB.";
                return View();
            }

            var importResults = new List<ImportSilabusResult>();
            var validRows = new List<ParsedSilabusRow>();
            bool hasErrors = false;

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

                // D-16: Header validation — cek kolom header di baris 1
                var expectedHeaders = new[] { "Bagian", "Unit", "Track", "Kompetensi", "SubKompetensi", "Deliverable", "Target" };
                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var actual = ws.Cell(1, i + 1).GetString().Trim();
                    if (!actual.Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ImportError"] = $"Header kolom {i + 1} tidak cocok. Diharapkan: '{expectedHeaders[i]}', ditemukan: '{actual}'. Pastikan menggunakan template yang benar.";
                        return RedirectToAction("ImportSilabus");
                    }
                }

                // D-13: Pass 1 — Validasi semua baris tanpa DB write
                for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                {
                    var colBagian   = ws.Cell(rowNum, 1).GetString().Trim();
                    var colUnit     = ws.Cell(rowNum, 2).GetString().Trim();
                    var colTrack    = ws.Cell(rowNum, 3).GetString().Trim();
                    var colKomp     = ws.Cell(rowNum, 4).GetString().Trim();
                    var colSub      = ws.Cell(rowNum, 5).GetString().Trim();
                    var colDel      = ws.Cell(rowNum, 6).GetString().Trim();
                    var colTarget   = ws.Cell(rowNum, 7).GetString().Trim();

                    // Skip blank rows
                    if (string.IsNullOrEmpty(colKomp) && string.IsNullOrEmpty(colSub) &&
                        string.IsNullOrEmpty(colDel) && string.IsNullOrEmpty(colTarget) &&
                        string.IsNullOrEmpty(colBagian) && string.IsNullOrEmpty(colUnit))
                        continue;

                    var result = new ImportSilabusResult
                    {
                        RowNumber = rowNum,
                        Kompetensi = colKomp,
                        SubKompetensi = colSub,
                        Deliverable = colDel
                    };

                    // Validate required fields
                    if (string.IsNullOrEmpty(colKomp) || string.IsNullOrEmpty(colSub) || string.IsNullOrEmpty(colDel))
                    {
                        result.Status = "Error";
                        result.Message = "Kompetensi, SubKompetensi, dan Deliverable wajib diisi.";
                        importResults.Add(result);
                        hasErrors = true;
                        continue;
                    }

                    if (string.IsNullOrEmpty(bagian) || string.IsNullOrEmpty(unit) || !trackId.HasValue)
                    {
                        result.Status = "Error";
                        result.Message = "Parameter bagian, unit, dan trackId tidak valid.";
                        importResults.Add(result);
                        hasErrors = true;
                        continue;
                    }

                    // D-15: Duplikasi detection — cek apakah deliverable sudah ada di DB (active)
                    var existingDeliverable = await _context.ProtonDeliverableList
                        .AnyAsync(d => d.NamaDeliverable == colDel
                                    && d.ProtonSubKompetensi.NamaSubKompetensi == colSub
                                    && d.ProtonSubKompetensi.ProtonKompetensi.NamaKompetensi == colKomp
                                    && d.ProtonSubKompetensi.ProtonKompetensi.Bagian == bagian
                                    && d.ProtonSubKompetensi.ProtonKompetensi.Unit == unit
                                    && d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == trackId.Value
                                    && d.ProtonSubKompetensi.ProtonKompetensi.IsActive);
                    if (existingDeliverable)
                    {
                        result.Status = "Skipped";
                        result.Message = "Sudah ada (aktif)";
                        importResults.Add(result);
                        continue;
                    }

                    validRows.Add(new ParsedSilabusRow
                    {
                        RowNumber = rowNum,
                        Bagian = colBagian,
                        Unit = colUnit,
                        Track = colTrack,
                        Kompetensi = colKomp,
                        SubKompetensi = colSub,
                        Deliverable = colDel,
                        Target = colTarget
                    });
                    result.Status = "Valid";
                    result.Message = "OK";
                    importResults.Add(result);
                }

                // D-13: Jika ada error, rollback semua — tidak ada data yang masuk
                if (hasErrors)
                {
                    TempData["ImportResults"] = System.Text.Json.JsonSerializer.Serialize(importResults);
                    TempData["ImportError"] = "Import dibatalkan karena ada baris error. Perbaiki data dan coba lagi.";
                    return RedirectToAction("ImportSilabus", new { bagian, unit, trackId });
                }

                // Pass 2 — Insert semua valid rows dalam transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // In-memory dictionaries untuk FK resolution tanpa SaveChanges per baris
                    var kompDict = new Dictionary<string, ProtonKompetensi>();
                    var subDict = new Dictionary<string, ProtonSubKompetensi>();

                    // Pre-load existing Kompetensi dan SubKompetensi yang mungkin sudah ada
                    var existingKomps = await _context.ProtonKompetensiList
                        .Include(k => k.SubKompetensiList)
                        .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId!.Value)
                        .ToListAsync();
                    foreach (var k in existingKomps)
                    {
                        kompDict[$"{k.NamaKompetensi}"] = k;
                        foreach (var s in k.SubKompetensiList)
                            subDict[$"{k.NamaKompetensi}|{s.NamaSubKompetensi}"] = s;
                    }

                    var maxKompUrutan = existingKomps.Any() ? existingKomps.Max(k => k.Urutan) : 0;

                    foreach (var row in validRows)
                    {
                        // Upsert Kompetensi
                        if (!kompDict.TryGetValue(row.Kompetensi, out var komp))
                        {
                            komp = new ProtonKompetensi
                            {
                                Bagian = bagian!,
                                Unit = unit!,
                                ProtonTrackId = trackId!.Value,
                                NamaKompetensi = row.Kompetensi,
                                Urutan = ++maxKompUrutan,
                                IsActive = true
                            };
                            _context.ProtonKompetensiList.Add(komp);
                            kompDict[row.Kompetensi] = komp;
                        }

                        // Upsert SubKompetensi
                        var subKey = $"{row.Kompetensi}|{row.SubKompetensi}";
                        if (!subDict.TryGetValue(subKey, out var sub))
                        {
                            var maxSubUrutan = kompDict[row.Kompetensi].SubKompetensiList.Any()
                                ? kompDict[row.Kompetensi].SubKompetensiList.Max(s => s.Urutan)
                                : 0;
                            sub = new ProtonSubKompetensi
                            {
                                ProtonKompetensi = komp,
                                NamaSubKompetensi = row.SubKompetensi,
                                Urutan = maxSubUrutan + 1
                            };
                            _context.ProtonSubKompetensiList.Add(sub);
                            subDict[subKey] = sub;
                        }

                        // Insert Deliverable (baru — duplikasi sudah discarded di Pass 1)
                        var maxDelUrutan = sub.Deliverables?.Any() == true ? sub.Deliverables.Max(d => d.Urutan) : 0;
                        var del = new ProtonDeliverable
                        {
                            ProtonSubKompetensi = sub,
                            NamaDeliverable = row.Deliverable,
                            Target = string.IsNullOrEmpty(row.Target) ? null : row.Target,
                            Urutan = maxDelUrutan + 1
                        };
                        _context.ProtonDeliverableList.Add(del);
                    }

                    await _context.SaveChangesAsync(); // satu kali — EF insert dalam urutan dependency
                    await transaction.CommitAsync();

                    // Update results — semua valid rows menjadi "Created"
                    foreach (var r in importResults.Where(r => r.Status == "Valid"))
                    {
                        r.Status = "Created";
                        r.Message = "Berhasil dibuat.";
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "ImportSilabus transaction failed");
                    TempData["ImportError"] = "Import gagal. Semua perubahan dibatalkan.";
                    TempData["ImportResults"] = System.Text.Json.JsonSerializer.Serialize(importResults);
                    return RedirectToAction("ImportSilabus", new { bagian, unit, trackId });
                }
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = $"Gagal memproses file: {ex.Message}";
                return RedirectToAction("ImportSilabus", new { bagian, unit, trackId });
            }

            int createdCount = importResults.Count(r => r.Status == "Created");
            int skippedCount = importResults.Count(r => r.Status == "Skipped");

            TempData["ImportResults"] = System.Text.Json.JsonSerializer.Serialize(importResults);
            TempData["ImportSuccess"] = $"Import selesai: {createdCount} dibuat, {skippedCount} dilewati (sudah ada).";

            if (!string.IsNullOrEmpty(bagian) && !string.IsNullOrEmpty(unit) && trackId.HasValue && createdCount > 0)
                return RedirectToAction("Index", new { bagian, unit, trackId, tab = "silabus" });

            return RedirectToAction("ImportSilabus", new { bagian, unit, trackId });
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
            // Validate path stays within wwwroot to prevent path traversal
            var fullPath = Path.GetFullPath(physicalPath);
            if (!fullPath.StartsWith(_env.WebRootPath, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file path.");
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var contentType = GetContentType(Path.GetExtension(record.FilePath));
            return PhysicalFile(fullPath, contentType, record.FileName);
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

            // 1. Upload file baru DULU — sebelum hapus file lama
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "guidance");
            var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
            var newFilePath = Path.Combine(uploadDir, safeFileName);

            using (var stream = new FileStream(newFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. Capture path file lama SEBELUM update DB
            var oldPath = Path.Combine(_env.WebRootPath, record.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            // 3. Update DB record
            var oldFileName = record.FileName;
            record.FileName = file.FileName;
            record.FilePath = $"/uploads/guidance/{safeFileName}";
            record.FileSize = file.Length;
            record.UploadedAt = DateTime.UtcNow;
            record.UploadedById = user.Id;
            await _context.SaveChangesAsync();

            // 4. BARU hapus file lama (non-critical — wrapped in try-catch)
            try
            {
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old guidance file: {Path}", oldPath);
            }

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

            // Illegal status transition validation (D-14)
            // Prevents regressing Approved back to Pending directly — must go through Submitted or Rejected first.
            var illegalTransitions = new Dictionary<string, HashSet<string>>
            {
                { "Approved", new HashSet<string> { "Pending" } },
            };
            if (illegalTransitions.TryGetValue(progress.Status, out var blockedTargets)
                && blockedTargets.Contains(req.NewStatus))
            {
                return Json(new { success = false,
                    message = $"Transisi dari '{progress.Status}' ke '{req.NewStatus}' tidak diizinkan. " +
                              $"Status Approved tidak dapat langsung dikembalikan ke Pending. " +
                              $"Gunakan status Rejected atau Submitted terlebih dahulu." });
            }

            // HCApprovalStatus consistency: if overriding to non-Approved, reset HC status to Pending
            if (req.NewStatus != "Approved" && req.NewHCStatus == "Reviewed")
            {
                req.NewHCStatus = "Pending";
            }

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
