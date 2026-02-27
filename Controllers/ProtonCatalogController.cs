using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HcPortal.Controllers
{
    [Authorize]
    public class ProtonCatalogController : Controller
    {
        // All ProtonCatalog functionality replaced by /ProtonData (Phase 51)
        // Redirect all actions to preserve bookmarked URLs

        public IActionResult Index() => RedirectToAction("Index", "ProtonData");
        public IActionResult GetCatalogTree() => RedirectToAction("Index", "ProtonData");
        public IActionResult AddKompetensi() => RedirectToAction("Index", "ProtonData");
        public IActionResult AddSubKompetensi() => RedirectToAction("Index", "ProtonData");
        public IActionResult AddDeliverable() => RedirectToAction("Index", "ProtonData");
        public IActionResult UpdateKompetensi() => RedirectToAction("Index", "ProtonData");
        public IActionResult UpdateSubKompetensi() => RedirectToAction("Index", "ProtonData");
        public IActionResult UpdateDeliverable() => RedirectToAction("Index", "ProtonData");
        public IActionResult DeleteKompetensi() => RedirectToAction("Index", "ProtonData");
        public IActionResult DeleteSubKompetensi() => RedirectToAction("Index", "ProtonData");
        public IActionResult DeleteDeliverable() => RedirectToAction("Index", "ProtonData");
    }
}
