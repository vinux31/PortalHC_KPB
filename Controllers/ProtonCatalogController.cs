using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;

namespace HcPortal.Controllers
{
    [Authorize]
    public class ProtonCatalogController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProtonCatalogController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /ProtonCatalog
        public async Task<IActionResult> Index(int? trackId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.RoleLevel > 2) return Forbid();

            var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
            ViewBag.AllTracks = tracks;
            ViewBag.SelectedTrackId = trackId;

            if (trackId.HasValue)
            {
                var kompetensiList = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .Where(k => k.ProtonTrackId == trackId.Value)
                    .OrderBy(k => k.Urutan)
                    .ToListAsync();
                ViewBag.KompetensiList = kompetensiList;
            }

            return View();
        }

        // GET: /ProtonCatalog/GetCatalogTree?trackId=1
        public async Task<IActionResult> GetCatalogTree(int trackId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (user.RoleLevel > 2) return Forbid();

            if (trackId <= 0)
                return PartialView("_CatalogTree", new List<ProtonKompetensi>());

            var kompetensiList = await _context.ProtonKompetensiList
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .Where(k => k.ProtonTrackId == trackId)
                .OrderBy(k => k.Urutan)
                .ToListAsync();

            return PartialView("_CatalogTree", kompetensiList);
        }

        // POST: /ProtonCatalog/AddTrack
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrack(string trackType, string tahunKe)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.RoleLevel > 2)
                return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(trackType) || string.IsNullOrWhiteSpace(tahunKe))
                return Json(new { success = false, error = "Input tidak valid." });

            var allowedTypes = new[] { "Panelman", "Operator" };
            var allowedYears = new[] { "Tahun 1", "Tahun 2", "Tahun 3" };

            if (!allowedTypes.Contains(trackType) || !allowedYears.Contains(tahunKe))
                return Json(new { success = false, error = "Pilihan tidak valid." });

            var existing = await _context.ProtonTracks
                .FirstOrDefaultAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe);

            if (existing != null)
            {
                var existingDisplayName = $"{trackType} - {tahunKe}";
                return Json(new { success = false, error = $"{existingDisplayName} already exists" });
            }

            var displayName = $"{trackType} - {tahunKe}";
            var maxUrutan = await _context.ProtonTracks.AnyAsync()
                ? await _context.ProtonTracks.MaxAsync(t => t.Urutan)
                : 0;

            var newTrack = new ProtonTrack
            {
                TrackType = trackType,
                TahunKe = tahunKe,
                DisplayName = displayName,
                Urutan = maxUrutan + 1
            };

            _context.ProtonTracks.Add(newTrack);
            await _context.SaveChangesAsync();

            return Json(new { success = true, trackId = newTrack.Id, displayName = displayName });
        }
    }
}
