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

    public class GuidanceDeleteRequest
    {
        public int Id { get; set; }
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
            catch { /* log but don't fail */ }

            await _auditLog.LogAsync(user.Id, user.FullName, "Delete",
                $"Deleted guidance file '{fileName}'",
                targetId: req.Id, targetType: "CoachingGuidanceFile");

            return Json(new { success = true });
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
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}
