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

        // POST: /ProtonCatalog/AddKompetensi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddKompetensi(int trackId, string nama)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.RoleLevel > 2)
                return Json(new { success = false, error = "Unauthorized" });

            if (trackId <= 0 || string.IsNullOrWhiteSpace(nama))
                return Json(new { success = false, error = "Input tidak valid." });

            var trackExists = await _context.ProtonTracks.AnyAsync(t => t.Id == trackId);
            if (!trackExists)
                return Json(new { success = false, error = "Track tidak ditemukan." });

            var maxUrutan = await _context.ProtonKompetensiList.AnyAsync(k => k.ProtonTrackId == trackId)
                ? await _context.ProtonKompetensiList.Where(k => k.ProtonTrackId == trackId).MaxAsync(k => k.Urutan)
                : 0;

            var item = new ProtonKompetensi
            {
                ProtonTrackId = trackId,
                NamaKompetensi = nama.Trim(),
                Urutan = maxUrutan + 1
            };
            _context.ProtonKompetensiList.Add(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = item.Id, nama = item.NamaKompetensi, urutan = item.Urutan });
        }

        // POST: /ProtonCatalog/AddSubKompetensi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubKompetensi(int kompetensiId, string nama)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.RoleLevel > 2)
                return Json(new { success = false, error = "Unauthorized" });

            if (kompetensiId <= 0 || string.IsNullOrWhiteSpace(nama))
                return Json(new { success = false, error = "Input tidak valid." });

            var parentExists = await _context.ProtonKompetensiList.AnyAsync(k => k.Id == kompetensiId);
            if (!parentExists)
                return Json(new { success = false, error = "Kompetensi tidak ditemukan." });

            var maxUrutan = await _context.ProtonSubKompetensiList.AnyAsync(s => s.ProtonKompetensiId == kompetensiId)
                ? await _context.ProtonSubKompetensiList.Where(s => s.ProtonKompetensiId == kompetensiId).MaxAsync(s => s.Urutan)
                : 0;

            var item = new ProtonSubKompetensi
            {
                ProtonKompetensiId = kompetensiId,
                NamaSubKompetensi = nama.Trim(),
                Urutan = maxUrutan + 1
            };
            _context.ProtonSubKompetensiList.Add(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = item.Id, nama = item.NamaSubKompetensi, urutan = item.Urutan });
        }

        // POST: /ProtonCatalog/AddDeliverable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDeliverable(int subKompetensiId, string nama)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.RoleLevel > 2)
                return Json(new { success = false, error = "Unauthorized" });

            if (subKompetensiId <= 0 || string.IsNullOrWhiteSpace(nama))
                return Json(new { success = false, error = "Input tidak valid." });

            var parentExists = await _context.ProtonSubKompetensiList.AnyAsync(s => s.Id == subKompetensiId);
            if (!parentExists)
                return Json(new { success = false, error = "SubKompetensi tidak ditemukan." });

            var maxUrutan = await _context.ProtonDeliverableList.AnyAsync(d => d.ProtonSubKompetensiId == subKompetensiId)
                ? await _context.ProtonDeliverableList.Where(d => d.ProtonSubKompetensiId == subKompetensiId).MaxAsync(d => d.Urutan)
                : 0;

            var item = new ProtonDeliverable
            {
                ProtonSubKompetensiId = subKompetensiId,
                NamaDeliverable = nama.Trim(),
                Urutan = maxUrutan + 1
            };
            _context.ProtonDeliverableList.Add(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = item.Id, nama = item.NamaDeliverable, urutan = item.Urutan });
        }

        // POST: /ProtonCatalog/EditCatalogItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCatalogItem(string level, int itemId, string nama)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.RoleLevel > 2)
                return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(level) || itemId <= 0 || string.IsNullOrWhiteSpace(nama))
                return Json(new { success = false, error = "Input tidak valid." });

            switch (level)
            {
                case "Kompetensi":
                    var k = await _context.ProtonKompetensiList.FindAsync(itemId);
                    if (k == null) return Json(new { success = false, error = "Item tidak ditemukan." });
                    k.NamaKompetensi = nama.Trim();
                    break;
                case "SubKompetensi":
                    var s = await _context.ProtonSubKompetensiList.FindAsync(itemId);
                    if (s == null) return Json(new { success = false, error = "Item tidak ditemukan." });
                    s.NamaSubKompetensi = nama.Trim();
                    break;
                case "Deliverable":
                    var d = await _context.ProtonDeliverableList.FindAsync(itemId);
                    if (d == null) return Json(new { success = false, error = "Item tidak ditemukan." });
                    d.NamaDeliverable = nama.Trim();
                    break;
                default:
                    return Json(new { success = false, error = "Level tidak valid." });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
