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
        public string Bagian { get; set; } = "";
        public string Unit { get; set; } = "";
        public int TrackId { get; set; }
    }

    public class SilabusDeleteRequest
    {
        public int DeliverableId { get; set; }
    }

    [Authorize(Roles = "Admin,HC")]
    public class ProtonDataController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLog;
        private readonly IWebHostEnvironment _env;

        public ProtonDataController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _env = env;
        }

        // GET: /ProtonData
        public async Task<IActionResult> Index(string? bagian, string? unit, int? trackId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.AllTracks = tracks;
            ViewBag.Bagian = bagian;
            ViewBag.Unit = unit;
            ViewBag.TrackId = trackId;

            // Build flat silabus rows for JSON serialization to JS
            var silabusRows = new List<object>();
            if (!string.IsNullOrEmpty(bagian) && !string.IsNullOrEmpty(unit) && trackId.HasValue)
            {
                var kompetensiList = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value)
                    .OrderBy(k => k.Urutan)
                    .ToListAsync();

                foreach (var k in kompetensiList)
                {
                    foreach (var s in k.SubKompetensiList.OrderBy(s => s.Urutan))
                    {
                        foreach (var d in s.Deliverables.OrderBy(d => d.Urutan))
                        {
                            silabusRows.Add(new
                            {
                                KompetensiId = k.Id,
                                Kompetensi = k.NamaKompetensi,
                                SubKompetensiId = s.Id,
                                SubKompetensi = s.NamaSubKompetensi,
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

        // POST: /ProtonData/SilabusSave — batch upsert all silabus rows
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SilabusSave([FromBody] List<SilabusRowDto> rows)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (rows == null || !rows.Any())
                return Json(new { success = false, message = "Tidak ada data." });

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
                        // ID not found — treat as new
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

                // 3. Upsert Deliverable
                if (row.DeliverableId > 0)
                {
                    var deliv = await _context.ProtonDeliverableList.FindAsync(row.DeliverableId);
                    if (deliv != null)
                    {
                        deliv.NamaDeliverable = row.Deliverable.Trim();
                        deliv.ProtonSubKompetensiId = subKomp!.Id;
                        deliv.Urutan = urutan;
                        updated++;
                    }
                }
                else
                {
                    var deliv = new ProtonDeliverable
                    {
                        ProtonSubKompetensiId = subKomp!.Id,
                        NamaDeliverable = row.Deliverable.Trim(),
                        Urutan = urutan
                    };
                    _context.ProtonDeliverableList.Add(deliv);
                    created++;
                }
                urutan++;
            }

            await _context.SaveChangesAsync();

            // Delete orphaned entities: any Kompetensi/SubKompetensi/Deliverable for this Bagian+Unit+Track that are NOT in the saved rows
            var savedKompIds = rows.Where(r => r.KompetensiId > 0).Select(r => r.KompetensiId).Distinct().ToList();
            var savedSubIds = rows.Where(r => r.SubKompetensiId > 0).Select(r => r.SubKompetensiId).Distinct().ToList();
            var savedDelivIds = rows.Where(r => r.DeliverableId > 0).Select(r => r.DeliverableId).Distinct().ToList();

            // Also include newly-created IDs from kompDict/subKompDict
            savedKompIds.AddRange(kompDict.Values.Select(k => k.Id));
            savedSubIds.AddRange(subKompDict.Values.Select(s => s.Id));

            // Find orphaned deliverables for this scope
            var scopeKomps = await _context.ProtonKompetensiList
                .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId)
                .Include(k => k.SubKompetensiList).ThenInclude(s => s.Deliverables)
                .ToListAsync();

            int deleted = 0;
            foreach (var k in scopeKomps)
            {
                foreach (var s in k.SubKompetensiList)
                {
                    var orphanDelivs = s.Deliverables.Where(d => !savedDelivIds.Contains(d.Id) && d.Id > 0).ToList();
                    _context.ProtonDeliverableList.RemoveRange(orphanDelivs);
                    deleted += orphanDelivs.Count;
                }
                var orphanSubs = k.SubKompetensiList.Where(s => !savedSubIds.Contains(s.Id) && s.Id > 0 && !s.Deliverables.Any()).ToList();
                _context.ProtonSubKompetensiList.RemoveRange(orphanSubs);
                deleted += orphanSubs.Count;
            }
            var orphanKomps = scopeKomps.Where(k => !savedKompIds.Contains(k.Id) && !k.SubKompetensiList.Any()).ToList();
            _context.ProtonKompetensiList.RemoveRange(orphanKomps);
            deleted += orphanKomps.Count;

            await _context.SaveChangesAsync();

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
    }
}
