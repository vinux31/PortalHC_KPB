using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    /// <summary>
    /// Phase 340 endpoint untuk JS consumer (Phase 342+343 view integration).
    /// Phase 341 nanti tambah page CRUD actions (Index/Add/Update/Delete).
    /// </summary>
    [Authorize]
    [Route("Admin/[action]")]
    public class OrgLabelController : Controller
    {
        private readonly IOrgLabelService _orgLabels;

        public OrgLabelController(IOrgLabelService orgLabels)
        {
            _orgLabels = orgLabels;
        }

        // GET /Admin/GetLevelLabels
        // Response example: { "0": "Bagian", "1": "Unit", "2": "Sub-unit" }
        [HttpGet]
        public IActionResult GetLevelLabels()
        {
            var dict = _orgLabels.GetAll();
            var jsonDict = dict.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            return Json(jsonDict);
        }
    }
}
