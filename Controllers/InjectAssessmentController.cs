using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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

        // POST /Admin/InjectAssessment — petakan VM→InjectRequest + UserId→NIP.
        // D-07: TIDAK memanggil service commit batch (jawaban per-pekerja + commit = Phase 395).
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InjectAssessment(InjectAssessmentViewModel vm)
        {
            // UserId→NIP (single query) — picker checkbox value = user.Id, service keys on NIP (Pitfall 2).
            var userIds = vm.UserIds ?? new List<string>();
            var userIdToNip = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.NIP ?? "");

            var req = MapToRequest(vm, userIdToNip);

            // D-07: belum commit di 394 — jawaban per-pekerja diisi Phase 395.
            TempData["Info"] = $"Setup tersimpan: {req.Workers.Count} pekerja, {req.Questions.Count} soal. Lengkapi jawaban pada tahap berikutnya untuk meng-inject.";
            await PopulateFeedAsync();
            return View(vm);
        }

        // Pure mapping VM→InjectRequest (testable; no DB). Questions diambil dari QuestionsJson (Plan 03)
        // bila ada, jika tidak fallback ke vm.Questions (bound list). Workers di-resolve UserId→NIP via dict.
        // Answers SELALU kosong di 394 (jawaban = Phase 395). CertPermanent → CertValidUntil null (D-10).
        public static InjectRequest MapToRequest(
            InjectAssessmentViewModel vm,
            IReadOnlyDictionary<string, string> userIdToNip)
        {
            var questionVms = ParseQuestionVms(vm);

            var req = new InjectRequest
            {
                Title = vm.Title ?? "",
                Category = vm.Category ?? "",
                AssessmentType = vm.AssessmentType ?? "Standard",
                CompletedAt = vm.CompletedAt,
                DurationMinutes = vm.DurationMinutes,
                PassPercentage = vm.PassPercentage,
                AllowAnswerReview = vm.AllowAnswerReview,
                CertMode = vm.CertMode,
                Questions = questionVms.Select((q, i) => new InjectQuestionSpec
                {
                    QuestionText = q.QuestionText ?? "",
                    QuestionType = q.QuestionType ?? "MultipleChoice",
                    ScoreValue = q.ScoreValue,
                    Order = i,
                    ElemenTeknis = q.ElemenTeknis,
                    Rubrik = q.Rubrik,
                    TempId = q.TempId,
                    Options = (q.Options ?? new()).Select(o => new InjectOptionSpec
                    {
                        OptionText = o.OptionText ?? "",
                        IsCorrect = o.IsCorrect,
                        TempId = o.TempId
                    }).ToList()
                }).ToList()
            };

            var certValidUntil = vm.CertPermanent
                ? (DateOnly?)null
                : (vm.CertValidUntil.HasValue ? DateOnly.FromDateTime(vm.CertValidUntil.Value) : (DateOnly?)null);

            foreach (var userId in (vm.UserIds ?? new List<string>()))
            {
                if (!userIdToNip.TryGetValue(userId, out var nip) || string.IsNullOrWhiteSpace(nip))
                    continue;   // null-NIP user diabaikan (surfaced saat commit 395)
                req.Workers.Add(new InjectWorkerSpec
                {
                    Nip = nip,
                    Answers = new(),   // kosong di 394
                    ManualCertNumber = vm.CertMode == InjectCertMode.Manual ? vm.ManualCertNumber : null,
                    CertValidUntil = certValidUntil
                });
            }

            return req;
        }

        // Sumber soal: QuestionsJson (Plan 03) prioritas; fallback vm.Questions (bound list).
        private static List<InjectAssessmentViewModel.InjectQuestionVM> ParseQuestionVms(InjectAssessmentViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.QuestionsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<InjectAssessmentViewModel.InjectQuestionVM>>(
                        vm.QuestionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) return parsed;
                }
                catch (JsonException) { /* malformed → fallback */ }
            }
            return vm.Questions ?? new();
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
