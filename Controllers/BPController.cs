using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HcPortal.Controllers
{
    [Authorize]
    public class BPController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
