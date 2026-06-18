using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Helpers;
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

        // POST /Admin/InjectAssessment — petakan VM→InjectRequest + UserId→NIP + COMMIT aktual (Phase 395).
        // Commit PERTAMA milestone: 394 berhenti sebelum commit (D-07); 395 wire #btnInject → InjectBatchAsync.
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

            var workerAnswers = ParseAnswerVms(vm);
            var req = MapToRequest(vm, userIdToNip, workerAnswers);

            // BLOCKING guard (D-08.3): worker auto-gen dengan target>ceiling MC/MA = TIDAK boleh ter-commit
            // (integritas sertifikasi — JANGAN diam-diam cap). Re-derive ceiling server-authoritative.
            var blockedNips = FindBlockedAutoGenNips(req, workerAnswers, userIdToNip);
            if (blockedNips.Count > 0)
            {
                TempData["Error"] = $"Ada pekerja auto-generate dengan target melebihi ceiling (NIP {string.Join(", ", blockedNips)}) — beralih ke input asli dan naikkan skor essay secara manual.";
                await PopulateFeedAsync();
                return View(vm);
            }

            // Commit aktual (D-07 dibuka): grade + persist + cert + audit via service teruji 393.
            var actorUserId = _userManager.GetUserId(User) ?? "";
            var actorName = User?.Identity?.Name ?? actorUserId;
            var injResult = await _injectService.InjectBatchAsync(req, actorUserId, actorName);

            if (injResult.Rejected)
            {
                var detail = injResult.PerRowErrors.Count > 0
                    ? " " + string.Join(" ", injResult.PerRowErrors.Select(e => $"[{e.Nip}] {e.Message}"))
                    : "";
                TempData["Error"] = (injResult.Message ?? "Inject ditolak.") + detail;
            }
            else if (injResult.Success)
            {
                var skipNote = injResult.SkippedNips.Count > 0 ? $" Dilewati (duplikat): {injResult.SkippedNips.Count}." : "";
                TempData["Success"] = $"Inject berhasil: {injResult.SuccessSessionIds.Count} sesi ter-commit.{skipNote}";
            }
            else
            {
                TempData["Error"] = injResult.Message ?? "Inject tidak menghasilkan sesi.";
            }

            await PopulateFeedAsync();
            return View(vm);
        }

        // POST /Admin/PreviewInjectScore — dry-run skor final 1 worker (D-09): NO write DB, NO cert#.
        // Engine = AssessmentScoreAggregator.Compute (identik commit) atas pola usulan → preview == commit.
        // Return Json → tidak terpengaruh override View() ~/Views/Admin/ (yang hanya untuk ViewResult).
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public IActionResult PreviewInjectScore([FromBody] InjectPreviewRequest preq)
        {
            preq ??= new InjectPreviewRequest();
            var questions = preq.Questions ?? new List<InjectQuestionSpec>();

            // 1) Tentukan pola jawaban final (server-authoritative).
            List<InjectAnswerSpec> answers;
            InjectAssessmentService.AutoGenResult? ag = null;
            if (string.Equals(preq.Mode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                var seed = InjectAssessmentService.ComputeAutoGenSeed(
                    preq.Nip ?? "", preq.Title ?? "", preq.Category ?? "", preq.CompletedAt, preq.TargetScore);
                ag = InjectAssessmentService.BuildAutoGenAnswers(questions, preq.TargetScore, seed);
                // Auto-gen MC/MA + gabung essay manual dari request (HYBRID D-08.1: HC ketik skor essay).
                answers = new List<InjectAnswerSpec>(ag.Answers);
                var essayManual = (preq.Answers ?? new List<InjectAnswerSpec>())
                    .Where(a => IsEssayTempId(questions, a.QuestionTempId));
                answers.AddRange(essayManual);
            }
            else
            {
                answers = preq.Answers ?? new List<InjectAnswerSpec>();
            }

            // 2) Map pola → in-memory PackageQuestion/Response (TempId = Id sintetis; Aggregator match by Id, EF-free).
            var (qInMem, respInMem) = MapToInMemory(questions, answers);

            // 3) Engine identik commit (preview == commit).
            var agg = AssessmentScoreAggregator.Compute(qInMem, respInMem, preq.PassPercentage);

            // 4) Susun hasil — NO CertNumberHelper (D-09), NO SaveChanges.
            var result = new InjectPreviewResult
            {
                Percentage = agg.Percentage,
                IsPassed = agg.IsPassed,
                TotalScore = agg.TotalScore,
                MaxScore = agg.MaxScore
            };
            if (ag != null)
            {
                result.CeilingPercent = ag.CeilingPercent;
                result.TargetReachable = ag.TargetReachable;
                result.Blocked = !ag.TargetReachable;
                result.Overshoot = Math.Max(0, agg.Percentage - preq.TargetScore);
                result.BlockingMessage = result.Blocked
                    ? $"Target {preq.TargetScore}% tidak tercapai: bobot essay dikecualikan dari auto-generate, maksimum {ag.CeilingPercent}%. Beralih ke input asli dan naikkan skor essay secara manual."
                    : null;
            }

            return Json(result);
        }

        // Pure mapping VM→InjectRequest (testable; no DB). Questions diambil dari QuestionsJson (Plan 03)
        // bila ada, jika tidak fallback ke vm.Questions (bound list). Workers di-resolve UserId→NIP via dict.
        // Phase 395: Answers diisi per-worker dari workerAnswers — manual = copy (skip=omit, D-05);
        // auto = BuildAutoGenAnswers(seed) server-authoritative + gabung essay manual (HYBRID, D-08.1).
        // CertPermanent → CertValidUntil null (D-10).
        public static InjectRequest MapToRequest(
            InjectAssessmentViewModel vm,
            IReadOnlyDictionary<string, string> userIdToNip,
            IReadOnlyList<InjectAssessmentViewModel.InjectWorkerAnswersVM> workerAnswers)
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

                var wa = workerAnswers.FirstOrDefault(x => x.UserId == userId);
                var answers = ResolveWorkerAnswers(wa, req.Questions, nip, vm);

                req.Workers.Add(new InjectWorkerSpec
                {
                    Nip = nip,
                    Answers = answers,
                    ManualCertNumber = vm.CertMode == InjectCertMode.Manual ? vm.ManualCertNumber : null,
                    CertValidUntil = certValidUntil
                });
            }

            return req;
        }

        // Resolve Answers 1 worker: manual = copy spec yang dikirim (skip=omit sudah ditangani client, D-05);
        // auto = BuildAutoGenAnswers(seed deterministik) MC/MA + gabung essay manual (HYBRID D-08.1).
        // Worker tanpa entry workerAnswers → kosong (grade 0; pre-flight MC kosong tak terkirim → tak reject-all).
        private static List<InjectAnswerSpec> ResolveWorkerAnswers(
            InjectAssessmentViewModel.InjectWorkerAnswersVM? wa,
            IReadOnlyList<InjectQuestionSpec> questions,
            string nip,
            InjectAssessmentViewModel vm)
        {
            if (wa == null) return new List<InjectAnswerSpec>();

            if (string.Equals(wa.Mode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                var seed = InjectAssessmentService.ComputeAutoGenSeed(
                    nip, vm.Title ?? "", vm.Category ?? "", vm.CompletedAt, wa.TargetScore);
                var ag = InjectAssessmentService.BuildAutoGenAnswers(questions, wa.TargetScore, seed);
                var answers = new List<InjectAnswerSpec>(ag.Answers);
                // essay manual (HYBRID): hanya soal Essay dari payload worker.
                answers.AddRange((wa.Answers ?? new())
                    .Where(a => IsEssayTempId(questions, a.QuestionTempId))
                    .Select(ToAnswerSpec));
                return answers;
            }

            // manual — copy apa adanya (client sudah omit soal di-skip).
            return (wa.Answers ?? new()).Select(ToAnswerSpec).ToList();
        }

        private static InjectAnswerSpec ToAnswerSpec(InjectAssessmentViewModel.InjectAnswerVM a) => new InjectAnswerSpec
        {
            QuestionTempId = a.QuestionTempId,
            SelectedOptionTempIds = a.SelectedOptionTempIds ?? new(),
            TextAnswer = a.TextAnswer,
            EssayScore = a.EssayScore
        };

        private static bool IsEssayTempId(IReadOnlyList<InjectQuestionSpec> questions, int tempId)
            => (questions.FirstOrDefault(q => q.TempId == tempId)?.QuestionType ?? "MultipleChoice") == "Essay";

        // Re-derive ceiling auto-gen per worker server-authoritative (D-08.3 BLOCKING): worker mode=auto
        // dengan target>ceiling MC/MA (TargetReachable=false) TIDAK boleh ter-commit. Return daftar NIP terblokir.
        private static List<string> FindBlockedAutoGenNips(
            InjectRequest req,
            IReadOnlyList<InjectAssessmentViewModel.InjectWorkerAnswersVM> workerAnswers,
            IReadOnlyDictionary<string, string> userIdToNip)
        {
            var blocked = new List<string>();
            foreach (var wa in workerAnswers)
            {
                if (!string.Equals(wa.Mode, "auto", StringComparison.OrdinalIgnoreCase)) continue;
                if (!userIdToNip.TryGetValue(wa.UserId, out var nip) || string.IsNullOrWhiteSpace(nip)) continue;
                var seed = InjectAssessmentService.ComputeAutoGenSeed(
                    nip, req.Title, req.Category, req.CompletedAt, wa.TargetScore);
                var ag = InjectAssessmentService.BuildAutoGenAnswers(req.Questions, wa.TargetScore, seed);
                if (!ag.TargetReachable) blocked.Add(nip);
            }
            return blocked;
        }

        // Map pola usulan → in-memory POCO untuk preview (preview == commit). TempId = Id sintetis
        // (Aggregator match by Id, EF-free, tak persist). RESEARCH §Code Examples.
        private static (List<PackageQuestion> questions, List<PackageUserResponse> responses) MapToInMemory(
            IReadOnlyList<InjectQuestionSpec> questions,
            IReadOnlyList<InjectAnswerSpec> answers)
        {
            var qInMem = questions.Select(q => new PackageQuestion
            {
                Id = q.TempId,
                QuestionType = q.QuestionType ?? "MultipleChoice",
                ScoreValue = q.ScoreValue,
                Options = (q.Options ?? new()).Select(o => new PackageOption { Id = o.TempId, IsCorrect = o.IsCorrect }).ToList()
            }).ToList();

            var respInMem = new List<PackageUserResponse>();
            foreach (var a in answers)
            {
                var q = questions.FirstOrDefault(x => x.TempId == a.QuestionTempId);
                if (q == null) continue;   // TempId dangling → skip (sejajar service :191 continue)
                if ((q.QuestionType ?? "MultipleChoice") == "Essay")
                    respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, EssayScore = a.EssayScore, TextAnswer = a.TextAnswer });
                else
                    foreach (var optTemp in (a.SelectedOptionTempIds ?? new()))
                        respInMem.Add(new PackageUserResponse { PackageQuestionId = q.TempId, PackageOptionId = optTemp });
            }
            return (qInMem, respInMem);
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

        // Phase 395 — deserialize #AnswersJson (per-worker payload) paralel ParseQuestionVms; try/catch
        // JsonException → fallback new() (Security V5: malformed → bukan 500).
        private static List<InjectAssessmentViewModel.InjectWorkerAnswersVM> ParseAnswerVms(InjectAssessmentViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.AnswersJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<InjectAssessmentViewModel.InjectWorkerAnswersVM>>(
                        vm.AnswersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) return parsed;
                }
                catch (JsonException) { /* malformed → fallback */ }
            }
            return new();
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
