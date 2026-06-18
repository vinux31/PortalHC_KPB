using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using HcPortal.ViewModels;

namespace HcPortal.Controllers
{
    // Phase 394 (Inject Assessment Manual "Seakan Online") — page wizard /Admin/InjectAssessment.
    // RBAC Admin,HC (server-authoritative). Reuse mesin existing (authoring/grading/cert) via Phase 393
    // InjectAssessmentService — commit batch aktual BARU dipanggil Phase 395 (D-07: 0 DB write di 394).
    [Route("Admin/[action]")]
    public class InjectAssessmentController : AdminBaseController
    {
        private readonly InjectAssessmentService _injectService;

        public InjectAssessmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            InjectAssessmentService injectService)
            : base(context, userManager, auditLog, env)
        {
            _injectService = injectService;   // dipakai Plan 04 POST commit (Phase 395); stored di sini OK.
        }

        // Override View resolution → Views/Admin/ folder (controller InjectAssessment, view stays in Admin/).
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        // GET /Admin/InjectAssessment — render wizard 6-langkah (INJ-03).
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> InjectAssessment()
        {
            await PopulateFeedAsync();
            return View(new InjectAssessmentViewModel());
        }

        // POST /Admin/InjectAssessment — Plan 04 mengisi pemetaan VM→InjectRequest + UserId→NIP.
        // STUB di Plan 01: re-render view + notice; TIDAK memanggil service commit batch (commit = Phase 395).
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InjectAssessment(InjectAssessmentViewModel vm)
        {
            // D-07: belum ada commit di 394 (jawaban per-pekerja diisi Phase 395).
            TempData["Info"] = "Lengkapi jawaban dulu (tahap berikutnya) sebelum meng-inject.";
            await PopulateFeedAsync();
            return View(vm);
        }

        // Feed dropdown/picker — mirror AssessmentAdminController.CreateAssessment GET (sans Proton/renewal/parent-cat).
        private async Task PopulateFeedAsync()
        {
            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
                .ToListAsync();
            ViewBag.SelectedUserIds = new List<string>();
            ViewBag.Sections = await _context.GetAllSectionsAsync();
            ViewBag.Categories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }
    }
}
