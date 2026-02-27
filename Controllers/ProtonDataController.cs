using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
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
    }
}
